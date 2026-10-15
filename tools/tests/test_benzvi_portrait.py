"""The one-photo #6 batch keeps source, runtime and CC0 status together."""
from pathlib import Path
import re

from PIL import Image
import yaml12

ROOT = Path(__file__).resolve().parents[2]
SOURCE = "https://commons.wikimedia.org/wiki/File:President_Isaac_Ben_Zwi_,_president_van_Israel_en_echtgenote_ontvangen_door_het_,_Bestanddeelnr_909-7131.jpg"


def portrait():
    return next(p for p in yaml12.load((ROOT / "content/people.yaml").read_text())["people"] if p["id"] == "ben_zvi")["portrait"]


def test_ben_zvi_has_legal_portrait_with_cc0_provenance():
    p = portrait()
    assert p["license"] == "CC0" and p["source"] == SOURCE
    assert p["focus_y"] == 0.32
    rules = yaml12.load((ROOT / "ASSETS.portraits.yaml").read_text())["rules"]
    for path in (p["file"], "unity/Assets/UI/Portraits/ben_zvi.jpg"):
        r = next(r for r in rules if r["glob"] == path)
        assert r["source"] == SOURCE and r["status"] == "verified"
        assert "Cc-zero" in r["commons"]


def test_ben_zvi_source_and_runtime_are_identical_small_jpegs():
    p = portrait()
    source = ROOT / p["file"]
    runtime = ROOT / "unity/Assets/UI/Portraits/ben_zvi.jpg"
    assert source.read_bytes() == runtime.read_bytes()
    with Image.open(runtime) as image:
        assert image.format == "JPEG" and image.size == (444, 512)


def test_ben_zvi_catalog_guid_focus_and_credit_match():
    p = portrait()
    meta = (ROOT / "unity/Assets/UI/Portraits/ben_zvi.jpg.meta").read_text()
    guid = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.M).group(1)
    catalog = (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text()
    entry = re.search(r"  - personId: ben_zvi\n(.*?)(?=  - personId:|\Z)", catalog, re.S).group(1)
    assert f"guid: {guid}" in entry and "focusY: 0.32" in entry
    assert f"ben_zvi | CC0 | {SOURCE}\n" in (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text()
    assert SOURCE in (ROOT / "promo/CREDITS.md").read_text()
