from unittest.mock import patch

from odoo import Command
from odoo.exceptions import AccessError
from odoo.tests import TransactionCase, tagged

from ..helpers import validate_export


@tagged("post_install", "-at_install")
class TestPositionImport(TransactionCase):
    def setUp(self):
        super().setUp()
        self.payload = {
            "schemaVersion": 1, "generatedAt": "2026-10-02T12:00:00Z", "publishedCvCount": 2,
            "position": {"id": "11111111-1111-1111-1111-111111111111", "title": "Test position", "company": "Demo"},
            "attributes": [{"id": "22222222-2222-2222-2222-222222222222", "title": "Score", "type": "Numeric",
                            "valueCount": 2, "missingCount": 0, "valuesWithheld": False,
                            "numeric": {"minimum": 1, "maximum": 3, "average": 2}, "popularValues": [], "dates": None, "period": None}],
        }
        self.source_url = "https://example.com"

    def test_reimport_updates_and_removes_stale_attributes(self):
        positions = self.env["cvplatform.position"]
        first = positions._import_payload(self.source_url, validate_export(self.payload))
        self.assertEqual(first.attribute_ids.average, 2)
        self.payload["position"]["title"] = "Updated title"
        self.payload["attributes"] = []
        second = positions._import_payload(self.source_url, validate_export(self.payload))
        self.assertEqual(first.id, second.id)
        self.assertEqual(second.name, "Updated title")
        self.assertFalse(second.attribute_ids)
        self.assertEqual(positions.search_count([("external_id", "=", self.payload["position"]["id"])]), 1)

    def test_internal_user_can_import_but_cannot_edit(self):
        users = self.env["res.users"]
        group_field = "group_ids" if "group_ids" in users._fields else "groups_id"
        viewer = users.create({"name": "Position viewer", "login": "cvplatform-position-viewer-test",
                               group_field: [Command.set([self.env.ref("base.group_user").id])]})
        self.env["ir.config_parameter"].sudo().set_param("cvplatform_position_viewer.base_url", self.source_url)
        wizard = self.env["cvplatform.import.wizard"].with_user(viewer).create({"api_token": "cvp_" + "a" * 43})
        with patch("odoo.addons.cvplatform_position_viewer.models.import_wizard.fetch_export", return_value=validate_export(self.payload)):
            action = wizard.action_import()
        position = self.env["cvplatform.position"].with_user(viewer).browse(action["res_id"])
        self.assertEqual(position.name, "Test position")
        with self.assertRaises(AccessError):
            position.write({"name": "Changed directly"})
        with self.assertRaises(AccessError):
            position.unlink()
        self.assertFalse(wizard.exists())
