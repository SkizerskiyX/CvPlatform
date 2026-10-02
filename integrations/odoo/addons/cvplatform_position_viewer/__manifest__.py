{
    "name": "CV Platform Position Viewer",
    "version": "19.0.1.0.0",
    "category": "Human Resources",
    "summary": "Import positions and aggregated CV results from CV Platform",
    "depends": ["base"],
    "data": [
        "security/ir.model.access.csv",
        "views/position_views.xml",
        "views/import_views.xml",
        "views/settings_views.xml",
        "views/menus.xml",
    ],
    "application": True,
    "installable": True,
    "license": "LGPL-3",
}
