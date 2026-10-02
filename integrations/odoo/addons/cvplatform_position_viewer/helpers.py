import json
import math
import re
from datetime import date, datetime, timezone
from urllib.parse import urlsplit
from urllib.error import HTTPError, URLError
from urllib.request import HTTPRedirectHandler, Request, build_opener
from uuid import UUID

ATTRIBUTE_TYPES = {"String", "Text", "Image", "Numeric", "Date", "Period", "Boolean", "Dropdown"}
MAX_RESPONSE_BYTES = 2 * 1024 * 1024


class _NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def normalize_base_url(value):
    value = (value or "").strip().rstrip("/")
    parts = urlsplit(value)
    local_hosts = {"localhost", "127.0.0.1", "host.docker.internal"}
    if not parts.hostname or parts.username or parts.password or parts.query or parts.fragment or parts.path:
        raise ValueError("Enter the CV Platform origin, without a path, query, or credentials.")
    if parts.scheme != "https" and not (parts.scheme == "http" and parts.hostname in local_hosts):
        raise ValueError("Use an HTTPS URL. HTTP is allowed only for local development.")
    return value


def fetch_export(base_url, token):
    base_url = normalize_base_url(base_url)
    token = (token or "").strip()
    if not re.fullmatch(r"cvp_[A-Za-z0-9_-]{43}", token):
        raise ValueError("Paste a position API token generated in CV Platform.")
    try:
        request = Request(base_url + "/api/integrations/odoo/position",
                          headers={"Authorization": "Bearer " + token, "Accept": "application/json"})
        with build_opener(_NoRedirect()).open(request, timeout=45) as response:
            if response.status != 200:
                raise ValueError("CV Platform returned HTTP %s. Check the URL and deployment." % response.status)
            body = response.read(MAX_RESPONSE_BYTES + 1)
            if len(body) > MAX_RESPONSE_BYTES:
                raise ValueError("The export is too large to import.")
            return validate_export(json.loads(body))
    except HTTPError as error:
        if error.code in (401, 403):
            raise ValueError("The token is invalid or revoked. Generate a new token in CV Platform.") from None
        raise ValueError("CV Platform returned HTTP %s. Check the URL and deployment." % error.code) from None
    except (URLError, TimeoutError, OSError):
        raise ValueError("Could not reach CV Platform. Check the URL and try again.") from None
    except (UnicodeDecodeError, json.JSONDecodeError):
        raise ValueError("CV Platform did not return valid JSON.") from None


def _count(value):
    if type(value) is not int or value < 0:
        raise ValueError("The export contains an invalid count.")
    return value


def _number(value):
    if type(value) not in (int, float) or not math.isfinite(value):
        raise ValueError("The export contains an invalid numeric result.")
    return float(value)


def _text(value, maximum=8000):
    if not isinstance(value, str) or len(value) > maximum:
        raise ValueError("The export contains an invalid text value.")
    return value


def _date(value):
    if value is None:
        return False
    return date.fromisoformat(_text(value, 10)).isoformat()


def validate_export(payload):
    try:
        if not isinstance(payload, dict) or type(payload.get("schemaVersion")) is not int or payload["schemaVersion"] != 1:
            raise ValueError("Unsupported export version. Update the Odoo module.")
        position = payload["position"]
        external_id = str(UUID(_text(position["id"], 36)))
        title = _text(position["title"], 200).strip()
        if not title:
            raise ValueError("The position title is empty.")
        count = _count(payload["publishedCvCount"])
        timestamp = datetime.fromisoformat(_text(payload["generatedAt"], 64).replace("Z", "+00:00"))
        if timestamp.tzinfo is None:
            raise ValueError("The export timestamp must contain a time zone.")
        attributes = payload["attributes"]
        if not isinstance(attributes, list) or len(attributes) > 1000:
            raise ValueError("The export contains too many attributes.")
        lines = []
        seen = set()
        for index, attribute in enumerate(attributes):
            attribute_id = str(UUID(_text(attribute["id"], 36)))
            if attribute_id in seen:
                raise ValueError("The export contains duplicate attributes.")
            seen.add(attribute_id)
            kind = attribute["type"]
            if kind not in ATTRIBUTE_TYPES:
                raise ValueError("The export contains an unsupported attribute type.")
            value_count = _count(attribute["valueCount"])
            missing_count = _count(attribute["missingCount"])
            if value_count + missing_count != count or type(attribute["valuesWithheld"]) is not bool:
                raise ValueError("The export contains inconsistent attribute counts.")
            line = {
                "external_id": attribute_id, "sequence": index, "name": _text(attribute["title"], 200),
                "data_type": kind, "value_count": value_count, "missing_count": missing_count,
                "values_withheld": attribute["valuesWithheld"],
                "has_numeric": False, "minimum": 0, "maximum": 0, "average": 0,
                "popular_values": False, "earliest_date": False, "latest_date": False,
                "earliest_start": False, "latest_end": False, "average_days": 0,
                "has_average_days": False, "open_ended_count": 0,
            }
            if line["values_withheld"]:
                summary = "Values are withheld; only completeness counts are exported."
            elif value_count == 0:
                summary = "No published values."
            elif kind == "Numeric":
                numeric = attribute["numeric"]
                line.update(has_numeric=True, minimum=_number(numeric["minimum"]),
                            maximum=_number(numeric["maximum"]), average=_number(numeric["average"]))
                if not line["minimum"] <= line["average"] <= line["maximum"]:
                    raise ValueError("The numeric results are inconsistent.")
                summary = "Minimum: %g\nAverage: %g\nMaximum: %g" % (line["minimum"], line["average"], line["maximum"])
            elif kind == "Date":
                line.update(earliest_date=_date(attribute["dates"]["earliest"]), latest_date=_date(attribute["dates"]["latest"]))
                summary = "Earliest: %s\nLatest: %s" % (line["earliest_date"], line["latest_date"])
            elif kind == "Period":
                period = attribute["period"]
                line.update(earliest_start=_date(period["earliestStart"]), latest_end=_date(period["latestEnd"]),
                            open_ended_count=_count(period["openEndedCount"]))
                if period["averageDays"] is not None:
                    line.update(has_average_days=True, average_days=_number(period["averageDays"]))
                summary = "Earliest start: %s\nLatest end: %s\nAverage days: %s\nOpen-ended: %s" % (
                    line["earliest_start"], line["latest_end"] or "—", line["average_days"] if line["has_average_days"] else "—", line["open_ended_count"])
            else:
                popular = attribute["popularValues"]
                if not isinstance(popular, list) or len(popular) > 5:
                    raise ValueError("The export contains an invalid list of popular values.")
                entries = []
                for value in popular:
                    frequency = _count(value["count"])
                    if not 0 < frequency <= value_count:
                        raise ValueError("The export contains an invalid value frequency.")
                    entries.append("%s (%s)" % (_text(value["value"]), frequency))
                line["popular_values"] = "\n".join(entries)
                summary = line["popular_values"] or "No grouped values."
            line["summary"] = summary
            lines.append(line)
        return {
            "external_id": external_id, "name": title, "company": _text(position.get("company") or "", 200),
            "published_cv_count": count, "source_generated_at": timestamp.astimezone(timezone.utc).replace(tzinfo=None),
            "attributes": lines,
        }
    except (KeyError, TypeError, AttributeError, OverflowError):
        raise ValueError("CV Platform returned an incomplete or invalid export.") from None
