"""Meitar CC BY 4.0 portraits (#30: Remez, Levin; #6: Dov Yosef) keep source, runtime, credit and licence together."""
from pathlib import Path
import re

from PIL import Image
import yaml12

ROOT = Path(__file__).resolve().parents[2]
BATCH = {
    "remez": ("David_Remez_(997009452176205171).jpg", "promo/ref/people/David_Remez_CCBY_1950.jpg", 0.38, "Boris Carmi"),
    "levin": ("Yitzhak-Meir_Levin_(997008137163805171).jpg", "promo/ref/people/Yitzhak-Meir_Levin_CCBY_1949.jpg", 0.41,
              "Benno Rothenberg"),
    "dov_yosef": ("Dov_Yosef_(997008137381905171).jpg", "promo/ref/people/Dov_Yosef_CCBY_1949.jpg", 0.36,
                  "Benno Rothenberg"),
}
PEOPLE = {p["id"]: p for p in yaml12.load((ROOT / "content/people.yaml").read_text(encoding="utf-8"))["people"]}
RULES = yaml12.load((ROOT / "ASSETS.portraits.yaml").read_text(encoding="utf-8"))["rules"]


def source(name):
    return "https://commons.wikimedia.org/wiki/File:" + name


def test_people_record_the_cc_by_source_file_and_focus():
    for pid, (name, file, focus, _artist) in BATCH.items():
        p = PEOPLE[pid]["portrait"]
        assert (p["license"], p["source"], p["file"], p["focus_y"]) == ("CC BY 4.0", source(name), file, focus)


def test_licence_rows_are_cc_by_and_verified_for_source_and_runtime():
    for pid, (name, file, _focus, artist) in BATCH.items():
        for path in (file, f"unity/Assets/UI/Portraits/{pid}.jpg"):
            r = next(r for r in RULES if r["glob"] == path)
            assert r["licence"] == "CC-BY-4.0" and r["status"] == "verified" and r["source"] == source(name)
            assert r["artist"] == artist and "Cc-by-4.0" in r["commons"]


def test_dov_yosef_catalog_guid_matches_the_meta_file():
    meta = (ROOT / "unity/Assets/UI/Portraits/dov_yosef.jpg.meta").read_text()
    guid = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.M).group(1)
    catalog = (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text()
    assert f"guid: {guid}, type: 3}}" in re.search(r"  - personId: dov_yosef\n(.*?)(?=  - personId:|\Z)", catalog, re.S).group(1)


def test_the_old_government_press_office_files_are_gone_from_the_rules():
    gone = {"promo/ref/people/David_Remez.jpg", "promo/ref/people/Yitzhak-Meir_Levin.jpg"}
    assert not gone & {r["glob"] for r in RULES}
    assert not any((ROOT / g).exists() for g in gone)


def test_runtime_is_the_source_crop_and_small():
    for pid, (_name, file, _focus, _artist) in BATCH.items():
        runtime = ROOT / f"unity/Assets/UI/Portraits/{pid}.jpg"
        assert runtime.read_bytes() == (ROOT / file).read_bytes()
        with Image.open(runtime) as image:
            assert image.format == "JPEG" and image.height == 512 and 380 <= image.width <= 420


def test_notice_credits_each_file_and_the_catalog_focus_matches():
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    catalog = (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text()
    listing = (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text()
    for pid, (name, _file, focus, artist) in BATCH.items():
        assert source(name) in notice and artist in notice
        assert f"{pid} | CC BY 4.0 | {source(name)}\n" in listing
        entry = re.search(rf"  - personId: {pid}\n(.*?)(?=  - personId:|\Z)", catalog, re.S).group(1)
        assert f"focusY: {focus}" in entry


def test_portraits_txt_header_matches_the_importer_and_order_matches_people_yaml():
    importer = (ROOT / "unity/Assets/Editor/PortraitImport.cs").read_text(encoding="utf-8")
    header = (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text(encoding="utf-8").splitlines()[0]
    assert f'new StringBuilder("{header}\\n")' in importer
    order = [pid for pid, p in PEOPLE.items() if p.get("portrait") and p["portrait"].get("file")]
    listed = [l.split(" | ")[0] for l in (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text(encoding="utf-8").splitlines()[1:]]
    catalog = re.findall(r"  - personId: (\w+)", (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text())
    assert listed == order and catalog == order
