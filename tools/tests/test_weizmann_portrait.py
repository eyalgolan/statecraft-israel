"""The one-photo #6 batch keeps source, runtime and explicit US status together."""
from pathlib import Path
import re

from PIL import Image
import yaml12

ROOT = Path(__file__).resolve().parents[2]
SOURCE = "https://commons.wikimedia.org/wiki/File:C._(Chaim)_Weizmann_LCCN2014712248.jpg"


def portrait():
    return next(p for p in yaml12.load((ROOT / "content/people.yaml").read_text())["people"] if p["id"] == "weizmann")["portrait"]


def test_weizmann_has_legal_portrait_with_explicit_us_provenance():
    p = portrait()
    assert p["license"] == "Public domain" and p["source"] == SOURCE
    assert p["focus_y"] == 0.32
    rules = yaml12.load((ROOT / "ASSETS.portraits.yaml").read_text())["rules"]
    for path in (p["file"], "unity/Assets/UI/Portraits/weizmann.jpg"):
        r = next(r for r in rules if r["glob"] == path)
        assert r["source"] == SOURCE and r["status"] == "verified"
        assert "PD-US-expired-text" in r["commons"]


def test_weizmann_source_and_runtime_are_identical_small_jpegs():
    p = portrait()
    source = ROOT / p["file"]
    runtime = ROOT / "unity/Assets/UI/Portraits/weizmann.jpg"
    assert source.read_bytes() == runtime.read_bytes()
    with Image.open(runtime) as image:
        assert image.format == "JPEG" and image.size == (444, 512)


def test_weizmann_catalog_guid_focus_and_credit_match():
    p = portrait()
    meta = (ROOT / "unity/Assets/UI/Portraits/weizmann.jpg.meta").read_text()
    guid = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.M).group(1)
    catalog = (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text()
    entry = re.search(r"  - personId: weizmann\n(.*?)(?=  - personId:|\Z)", catalog, re.S).group(1)
    assert f"guid: {guid}" in entry and "focusY: 0.32" in entry
    assert f"weizmann | Public domain | {SOURCE}\n" in (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text()
    assert SOURCE in (ROOT / "promo/CREDITS.md").read_text()
