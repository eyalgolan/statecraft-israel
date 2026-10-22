"""The WebGL template's icon is linked and served beside index.html (no Unity needed)."""
from html.parser import HTMLParser
from pathlib import Path
from urllib.request import urlopen

from PIL import Image
import web_check as wc

TEMPLATE = Path(__file__).resolve().parents[2] / "unity/Assets/WebGLTemplates/Statecraft"


class IconLinks(HTMLParser):
    def __init__(self):
        super().__init__()
        self.icons = []

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "link" and "icon" in attrs.get("rel", "").split():
            self.icons.append(attrs)


def test_webgl_template_links_a_local_multisize_icon():
    page = IconLinks()
    page.feed((TEMPLATE / "index.html").read_text())
    assert len(page.icons) == 1
    icon = page.icons[0]
    assert icon["href"] == "favicon.ico"
    assert icon["type"] == "image/x-icon"
    with Image.open(TEMPLATE / icon["href"]) as image:
        assert image.format == "ICO"
        assert image.ico.sizes() == {(16, 16), (32, 32), (48, 48)}


def test_favicon_is_served_at_template_relative_url():
    server = wc.serve(TEMPLATE)
    try:
        # serve()'s own address, not "localhost" (Windows tries ::1 first, where nothing listens), and a timeout.
        host, port = server.server_address[:2]
        with urlopen(f"http://{host}:{port}/favicon.ico", timeout=10) as response:
            assert response.status == 200
            assert response.read() == (TEMPLATE / "favicon.ico").read_bytes()
    finally:
        server.shutdown()
        server.server_close()
