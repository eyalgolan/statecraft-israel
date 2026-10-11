"""The one-photo #6 batch keeps source, runtime and explicit US status together."""
from pathlib import Path
import re

from PIL import Image
import yaml

ROOT = Path(__file__).resolve().parents[2]
SOURCE = "https://commons.wikimedia.org/wiki/File:President_Lyndon_B._Johnson_with_President_Zalman_Shazar_of_Israel.jpg"


def portrait():
    return next(p for p in yaml.safe_load((ROOT / "content/people.yaml").read_text())["people"] if p["id"] == "shazar")["portrait"]


def test_shazar_has_legal_portrait_with_explicit_us_provenance():
    p = portrait()
    assert p["license"] == "Public domain" and p["source"] == SOURCE
    assert p["focus_y"] == 0.35
    rules = yaml.safe_load((ROOT / "ASSETS.portraits.yaml").read_text())["rules"]
    for path in (p["file"], "unity/Assets/UI/Portraits/shazar.jpg"):
        r = next(r for r in rules if r["glob"] == path)
        assert r["source"] == SOURCE and r["status"] == "verified"
        assert "PD-USGov" in r["commons"]


def test_shazar_source_and_runtime_are_identical_small_jpegs():
    p = portrait()
    source = ROOT / p["file"]
    runtime = ROOT / "unity/Assets/UI/Portraits/shazar.jpg"
    assert source.read_bytes() == runtime.read_bytes()
    with Image.open(runtime) as image:
        assert image.format == "JPEG" and image.size == (444, 512)


def test_shazar_catalog_guid_focus_and_credit_match():
    p = portrait()
    meta = (ROOT / "unity/Assets/UI/Portraits/shazar.jpg.meta").read_text()
    guid = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.M).group(1)
    catalog = (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text()
    entry = re.search(r"  - personId: shazar\n(.*?)(?=  - personId:|\Z)", catalog, re.S).group(1)
    assert f"guid: {guid}" in entry and "focusY: 0.35" in entry
    assert f"shazar | Public domain | {SOURCE}\n" in (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text()
    assert SOURCE in (ROOT / "promo/CREDITS.md").read_text()
