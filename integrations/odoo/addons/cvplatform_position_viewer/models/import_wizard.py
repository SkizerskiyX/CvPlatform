from odoo import fields, models, _
from odoo.exceptions import AccessError, UserError

from ..helpers import fetch_export, normalize_base_url


class ImportPositionWizard(models.TransientModel):
    _name = "cvplatform.import.wizard"
    _description = "Import a CV Platform position"

    api_token = fields.Char(string="Position API token", required=True)

    def action_import(self):
        self.ensure_one()
        if not self.env.user.has_group("base.group_user"):
            raise AccessError(_("Only internal users can import positions."))
        source_url = self.env["ir.config_parameter"].sudo().get_param("cvplatform_position_viewer.base_url", "")
        if not source_url:
            raise UserError(_("Ask an administrator to configure the CV Platform URL in Settings."))
        try:
            source_url = normalize_base_url(source_url)
            payload = fetch_export(source_url, self.api_token)
        except ValueError as error:
            raise UserError(str(error)) from None
        position = self.env["cvplatform.position"]._import_payload(source_url, payload)
        self.unlink()
        return {
            "type": "ir.actions.act_window", "name": position.name,
            "res_model": "cvplatform.position", "res_id": position.id,
            "view_mode": "form", "target": "current",
        }
