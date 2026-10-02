import ast
import copy
import csv
import importlib.util
import json
import unittest
from io import BytesIO
from pathlib import Path
from unittest.mock import MagicMock, patch
from urllib.error import HTTPError
from xml.etree import ElementTree

MODULE = Path(__file__).resolve().parents[1] / "addons" / "cvplatform_position_viewer"
SPEC = importlib.util.spec_from_file_location("cvplatform_helpers", MODULE / "helpers.py")
helpers = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(helpers)


def payload():
    return {
        "schemaVersion": 1,
        "generatedAt": "2026-10-02T12:00:00Z",
        "position": {"id": "11111111-1111-1111-1111-111111111111", "title": "Frontend developer", "company": "Demo"},
        "publishedCvCount": 3,
        "attributes": [
            {"id": "22222222-2222-2222-2222-222222222222", "title": "Experience", "type": "Numeric",
             "valueCount": 2, "missingCount": 1, "valuesWithheld": False,
             "numeric": {"minimum": 2, "maximum": 6, "average": 4}, "popularValues": [], "dates": None, "period": None},
            {"id": "33333333-3333-3333-3333-333333333333", "title": "Skill", "type": "String",
             "valueCount": 3, "missingCount": 0, "valuesWithheld": False,
             "numeric": None, "popularValues": [{"value": "React", "count": 2}, {"value": "Vue", "count": 1}], "dates": None, "period": None},
        ],
    }


class ImportChecks(unittest.TestCase):
    def test_numeric_and_popular_values(self):
        result = helpers.validate_export(payload())
        self.assertEqual(result["name"], "Frontend developer")
        self.assertEqual(result["attributes"][0]["average"], 4)
        self.assertEqual(result["attributes"][1]["popular_values"], "React (2)\nVue (1)")
        self.assertIsNone(result["source_generated_at"].tzinfo)

    def test_empty_numeric_is_not_a_zero_result(self):
        data = payload()
        data["publishedCvCount"] = 0
        for attribute in data["attributes"]:
            attribute.update(valueCount=0, missingCount=0, numeric=None, popularValues=[])
        result = helpers.validate_export(data)
        self.assertFalse(result["attributes"][0]["has_numeric"])
        self.assertEqual(result["attributes"][0]["summary"], "No published values.")

    def test_withheld_values_are_not_imported(self):
        data = payload()
        data["attributes"][1]["valuesWithheld"] = True
        result = helpers.validate_export(data)
        self.assertFalse(result["attributes"][1]["popular_values"])

    def test_date_and_period(self):
        data = payload()
        data["attributes"][0].update(type="Period", period={"earliestStart": "2025-01-01", "latestEnd": None, "averageDays": None, "openEndedCount": 2})
        data["attributes"][1].update(type="Date", dates={"earliest": "2025-01-01", "latest": "2026-01-01"})
        result = helpers.validate_export(data)
        self.assertFalse(result["attributes"][0]["has_average_days"])
        self.assertEqual(result["attributes"][0]["open_ended_count"], 2)
        self.assertEqual(result["attributes"][1]["latest_date"], "2026-01-01")

    def test_invalid_payloads(self):
        mutations = [
            lambda data: data.update(schemaVersion=2),
            lambda data: data.update(publishedCvCount=-1),
            lambda data: data["attributes"].append(copy.deepcopy(data["attributes"][0])),
            lambda data: data["attributes"][0].update(valueCount=99),
            lambda data: data["attributes"][0]["numeric"].update(average=float("nan")),
            lambda data: data["attributes"][1]["popularValues"][0].update(count=9),
            lambda data: data["position"].update(id="not-a-uuid"),
        ]
        for mutation in mutations:
            with self.subTest(mutation=mutation):
                data = payload()
                mutation(data)
                with self.assertRaises(ValueError):
                    helpers.validate_export(data)

    def test_urls_and_tokens(self):
        self.assertEqual(helpers.normalize_base_url("https://cvplatform-h1rj.onrender.com/"), "https://cvplatform-h1rj.onrender.com")
        self.assertEqual(helpers.normalize_base_url("http://host.docker.internal:5242"), "http://host.docker.internal:5242")
        for url in ("http://public.example.com", "https://user:secret@example.com", "https://example.com/path", "https://example.com/?token=x", "ftp://example.com"):
            with self.subTest(url=url), self.assertRaises(ValueError):
                helpers.normalize_base_url(url)
        with patch.object(helpers, "build_opener") as opener:
            with self.assertRaises(ValueError):
                helpers.fetch_export("https://example.com", "user-jwt")
            opener.assert_not_called()

    def test_http_import(self):
        response = MagicMock()
        response.status = 200
        response.read.return_value = json.dumps(payload()).encode()
        response.__enter__.return_value = response
        with patch.object(helpers, "build_opener") as factory:
            factory.return_value.open.return_value = response
            result = helpers.fetch_export("https://example.com", "cvp_" + "a" * 43)
            request = factory.return_value.open.call_args.args[0]
            self.assertEqual(request.full_url, "https://example.com/api/integrations/odoo/position")
            self.assertEqual(request.get_header("Authorization"), "Bearer cvp_" + "a" * 43)
            self.assertEqual(result["published_cv_count"], 3)
            response.read.assert_called_once_with(helpers.MAX_RESPONSE_BYTES + 1)

    def test_revocation_redirects_and_size_limit(self):
        for status in (401, 403, 302, 500):
            with self.subTest(status=status), patch.object(helpers, "build_opener") as factory:
                factory.return_value.open.side_effect = HTTPError("https://example.com", status, "error", {}, BytesIO())
                with self.assertRaises(ValueError):
                    helpers.fetch_export("https://example.com", "cvp_" + "a" * 43)
        self.assertIsNone(helpers._NoRedirect().redirect_request(None, None, 302, "redirect", {}, "https://other.example.com"))
        response = MagicMock()
        response.status = 200
        response.read.return_value = b"x" * (helpers.MAX_RESPONSE_BYTES + 1)
        response.__enter__.return_value = response
        with patch.object(helpers, "build_opener") as factory:
            factory.return_value.open.return_value = response
            with self.assertRaisesRegex(ValueError, "too large"):
                helpers.fetch_export("https://example.com", "cvp_" + "a" * 43)

    def test_module_structure(self):
        manifest = ast.literal_eval((MODULE / "__manifest__.py").read_text(encoding="utf-8"))
        for filename in manifest["data"]:
            self.assertTrue((MODULE / filename).is_file())
        for filename in MODULE.rglob("*.py"):
            ast.parse(filename.read_text(encoding="utf-8"), filename=str(filename))
        for filename in MODULE.rglob("*.xml"):
            ElementTree.parse(filename)
        with (MODULE / "security" / "ir.model.access.csv").open(encoding="utf-8", newline="") as source:
            permissions = list(csv.DictReader(source))
        for permission in permissions[:2]:
            self.assertEqual([permission[key] for key in ("perm_read", "perm_write", "perm_create", "perm_unlink")], ["1", "0", "0", "0"])


if __name__ == "__main__":
    unittest.main()
