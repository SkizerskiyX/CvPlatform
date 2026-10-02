import unittest
from pathlib import Path
from xml.etree import ElementTree


class ActionTargetChecks(unittest.TestCase):
    def test_window_actions_use_odoo_19_targets(self):
        views = Path(__file__).resolve().parents[1] / "addons" / "cvplatform_position_viewer" / "views"
        targets = {"current", "new", "fullscreen", "main"}
        checked = 0
        for path in views.glob("*.xml"):
            root = ElementTree.parse(path).getroot()
            for record in root.findall(".//record[@model='ir.actions.act_window']"):
                for field in record.findall("field[@name='target']"):
                    with self.subTest(file=path.name, action=record.attrib["id"]):
                        self.assertIn(field.text, targets)
                    checked += 1
        self.assertGreater(checked, 0)


if __name__ == "__main__":
    unittest.main()
