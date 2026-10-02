import configparser
import os
import re
import subprocess
from pathlib import Path
from urllib.parse import unquote, urlsplit


def required(name):
    value = os.environ.get(name, "").strip()
    if not value:
        raise ValueError(f"Set {name} in Render environment variables")
    return value


def configuration():
    address = urlsplit(required("DATABASE_URL"))
    if address.scheme not in ("postgres", "postgresql") or not address.hostname:
        raise ValueError("DATABASE_URL must be the Render Internal Database URL")
    database = unquote(address.path.lstrip("/"))
    if not database or not address.username or not address.password:
        raise ValueError("DATABASE_URL must include a database, username and password")
    options = {
        "admin_passwd": required("ODOO_MASTER_PASSWORD"),
        "db_host": address.hostname,
        "db_port": str(address.port or 5432),
        "db_user": unquote(address.username),
        "db_password": unquote(address.password),
        "db_name": database,
        "dbfilter": "^" + re.escape(database) + "$",
        "db_sslmode": "prefer",
        "list_db": "False",
        "proxy_mode": "True",
        "http_interface": "0.0.0.0",
        "http_port": str(int(os.environ.get("PORT", "10000"))),
        "addons_path": "/mnt/extra-addons",
        "data_dir": "/var/lib/odoo",
        "workers": "0",
        "max_cron_threads": "0",
        "limit_time_real": "180",
        "db_maxconn": "8",
        "log_level": "info",
    }
    return options


def main():
    import psycopg2

    options = configuration()
    required("ODOO_ADMIN_PASSWORD")
    parser = configparser.ConfigParser(interpolation=None)
    parser["options"] = options
    path = Path("/tmp/cvplatform-odoo.conf")
    path.touch(mode=0o600, exist_ok=True)
    path.chmod(0o600)
    with path.open("w", encoding="utf-8") as stream:
        parser.write(stream)

    with psycopg2.connect(
        host=options["db_host"],
        port=options["db_port"],
        user=options["db_user"],
        password=options["db_password"],
        dbname=options["db_name"],
        sslmode="prefer",
        connect_timeout=30,
    ) as connection:
        with connection.cursor() as cursor:
            cursor.execute("SELECT to_regclass('public.ir_module_module')")
            initialized = cursor.fetchone()[0] is not None
            installed = False
            if initialized:
                cursor.execute(
                    "SELECT state FROM ir_module_module WHERE name = %s",
                    ("cvplatform_position_viewer",),
                )
                row = cursor.fetchone()
                installed = row is not None and row[0] == "installed"

    if not installed:
        subprocess.run(
            ["odoo", "-c", str(path), "-i", "base,cvplatform_position_viewer",
             "--without-demo=all", "--stop-after-init", "--no-http"],
            check=True,
        )

    setup = """
import os
parameters = env['ir.config_parameter'].sudo()
parameters.set_param('ir_attachment.location', 'db')
env['ir.attachment'].sudo().force_storage()
origin = os.environ.get('RENDER_EXTERNAL_URL', '').strip()
if origin:
    parameters.set_param('web.base.url', origin)
    parameters.set_param('web.base.url.freeze', 'True')
if not parameters.get_param('cvplatform.render_admin_initialized'):
    administrator = env.ref('base.user_admin').sudo()
    administrator.write({
        'login': os.environ.get('ODOO_ADMIN_LOGIN', 'admin'),
        'password': os.environ['ODOO_ADMIN_PASSWORD'],
    })
    parameters.set_param('cvplatform.render_admin_initialized', 'True')
env.cr.commit()
"""
    subprocess.run(
        ["odoo", "shell", "-c", str(path), "--no-http"],
        input=setup,
        text=True,
        check=True,
    )
    print("Odoo initialization complete; starting HTTP server", flush=True)
    os.execvp("odoo", ["odoo", "-c", str(path)])


if __name__ == "__main__":
    main()
