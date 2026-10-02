from odoo import api, fields, models, Command


class ImportedPosition(models.Model):
    _name = "cvplatform.position"
    _description = "Imported CV Platform position"
    _order = "imported_at desc, id desc"

    name = fields.Char(required=True, readonly=True)
    external_id = fields.Char(required=True, readonly=True, index=True)
    source_url = fields.Char(required=True, readonly=True)
    company = fields.Char(readonly=True)
    published_cv_count = fields.Integer(readonly=True)
    source_generated_at = fields.Datetime(readonly=True)
    imported_at = fields.Datetime(readonly=True)
    attribute_ids = fields.One2many("cvplatform.position.attribute", "position_id", readonly=True)
    _source_unique = models.Constraint("UNIQUE(source_url, external_id)", "This position has already been imported from this source.")

    @api.model
    def _import_payload(self, source_url, payload):
        self.env.cr.execute("SELECT pg_advisory_xact_lock(hashtext(%s), hashtext(%s))", (source_url, payload["external_id"]))
        positions = self.sudo()
        current = positions.search([("source_url", "=", source_url), ("external_id", "=", payload["external_id"])], limit=1)
        values = {key: value for key, value in payload.items() if key != "attributes"}
        values.update(source_url=source_url, imported_at=fields.Datetime.now(),
                      attribute_ids=[Command.clear()] + [Command.create(line) for line in payload["attributes"]])
        if current:
            current.write(values)
            return current
        return positions.create(values)


class ImportedPositionAttribute(models.Model):
    _name = "cvplatform.position.attribute"
    _description = "Aggregated position attribute"
    _order = "sequence, id"

    position_id = fields.Many2one("cvplatform.position", required=True, ondelete="cascade", readonly=True)
    sequence = fields.Integer(readonly=True)
    external_id = fields.Char(required=True, readonly=True)
    name = fields.Char(required=True, readonly=True)
    data_type = fields.Selection([(kind, kind) for kind in ("String", "Text", "Image", "Numeric", "Date", "Period", "Boolean", "Dropdown")], required=True, readonly=True)
    value_count = fields.Integer(readonly=True)
    missing_count = fields.Integer(readonly=True)
    values_withheld = fields.Boolean(readonly=True)
    has_numeric = fields.Boolean(readonly=True)
    minimum = fields.Float(readonly=True, digits=(20, 4))
    maximum = fields.Float(readonly=True, digits=(20, 4))
    average = fields.Float(readonly=True, digits=(20, 4))
    popular_values = fields.Text(readonly=True)
    earliest_date = fields.Date(readonly=True)
    latest_date = fields.Date(readonly=True)
    earliest_start = fields.Date(readonly=True)
    latest_end = fields.Date(readonly=True)
    has_average_days = fields.Boolean(readonly=True)
    average_days = fields.Float(readonly=True, digits=(20, 4))
    open_ended_count = fields.Integer(readonly=True)
    summary = fields.Text(readonly=True)
