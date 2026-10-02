import ast
import os
import runpy
import unittest
from pathlib import Path
from unittest.mock import patch


class RenderStartupChecks(unittest.TestCase):
    def test_core_addons_remain_accessible(self):
        script = Path(__file__).resolve().parents[1] / "render_start.py"
        functions = runpy.run_path(str(script))
        with patch.dict(os.environ, {
            "DATABASE_URL": "postgresql://odoo:p%40ss@localhost:5432/neondb",
            "ODOO_MASTER_PASSWORD": "test-only-password",
            "PORT": "10000",
        }):
            options = functions["configuration"]()
        self.assertIn("/usr/lib/python3/dist-packages/odoo/addons", options["addons_path"])
        self.assertIn("/mnt/extra-addons", options["addons_path"])
        self.assertEqual(options["db_password"], "p@ss")
        self.assertEqual(options["http_port"], "10000")

    def test_viewer_requires_web_interface(self):
        manifest = Path(__file__).resolve().parents[1] / "addons" / "cvplatform_position_viewer" / "__manifest__.py"
        self.assertIn("web", ast.literal_eval(manifest.read_text(encoding="utf-8"))["depends"])


if __name__ == "__main__":
    unittest.main()
