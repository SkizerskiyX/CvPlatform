from odoo import api, fields, models
from odoo.exceptions import ValidationError

from ..helpers import normalize_base_url


class ResConfigSettings(models.TransientModel):
    _inherit = "res.config.settings"

    cvplatform_base_url = fields.Char(
        string="CV Platform URL", config_parameter="cvplatform_position_viewer.base_url",
        default="https://cvplatform-h1rj.onrender.com",
    )

    @api.constrains("cvplatform_base_url")
    def _check_cvplatform_base_url(self):
        for record in self:
            if record.cvplatform_base_url:
                try:
                    normalize_base_url(record.cvplatform_base_url)
                except ValueError as error:
                    raise ValidationError(str(error)) from None
