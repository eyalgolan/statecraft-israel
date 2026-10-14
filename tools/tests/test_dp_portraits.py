"""D-P, 14 October 2026 (refs #30): Maimon and Bentov (Knesset Archives, CC BY-SA 4.0) and Shapira (Meitar Collection,
CC BY 4.0) replace their Government Press Office portraits. Source, runtime crop, credit and licence record stay together,
and each crop box is recorded and was only ever downscaled."""
from pathlib import Path
import re

from PIL import Image
import yaml

ROOT = Path(__file__).resolve().parents[2]
# person id: (Commons file, promo/ref source crop, focus_y, artist, people.yaml licence, ASSETS licence, Commons template,
#             crop box (x, y, w, h) in the Commons original)
BATCH = {
    "maimon": ("Yehuda_Leib_Maimon_(00320507).jpg", "promo/ref/people/Yehuda_Leib_Maimon_CCBYSA_Knesset.jpg", 0.6,
               "Knesset photographer", "CC BY-SA 4.0", "CC-BY-SA-4.0", "Cc-by-sa-4.0", (32, 60, 956, 1290)),
    "bentov": ("Mordechai_Bentov_(00330019).jpg", "promo/ref/people/Mordechai_Bentov_CCBYSA_Knesset.jpg", 0.5,
               "Knesset photographer", "CC BY-SA 4.0", "CC-BY-SA-4.0", "Cc-by-sa-4.0", (30, 26, 684, 918)),
    "shapira": ("Haim-Moshe_Shapira_(997008137213005171).jpg", "promo/ref/people/Haim-Moshe_Shapira_CCBY_1952.jpg", 0.37,
                "Benno Rothenberg", "CC BY 4.0", "CC-BY-4.0", "Cc-by-4.0", (730, 900, 540, 684)),
}
OLD_GPO = {"promo/ref/people/Yehuda_Leib_Maimon.jpg", "promo/ref/people/Mordechai_Bentov.jpg",
           "promo/ref/people/Haim-Moshe_Shapira.jpg"}
PEOPLE = {p["id"]: p for p in yaml.safe_load((ROOT / "content/people.yaml").read_text(encoding="utf-8"))["people"]}
RULES = yaml.safe_load((ROOT / "ASSETS.portraits.yaml").read_text(encoding="utf-8"))["rules"]


def source(name):
    return "https://commons.wikimedia.org/wiki/File:" + name


def test_people_record_the_new_source_file_licence_and_focus():
    for pid, (name, file, focus, _artist, licence, *_rest) in BATCH.items():
        p = PEOPLE[pid]["portrait"]
        assert (p["license"], p["source"], p["file"], p["focus_y"]) == (licence, source(name), file, focus)


def test_licence_rows_are_verified_for_source_and_runtime():
    for pid, (name, file, _focus, artist, _licence, asset_licence, template, _box) in BATCH.items():
        for path in (file, f"unity/Assets/UI/Portraits/{pid}.jpg"):
            r = next(r for r in RULES if r["glob"] == path)
            assert r["licence"] == asset_licence and r["status"] == "verified" and r["source"] == source(name)
            assert r["artist"] == artist and template in r["commons"].split("; ", 1)[1].split(", ")


def test_share_alike_rows_say_the_crop_is_itself_cc_by_sa():
    for pid, (_name, file, *_rest) in BATCH.items():
        if BATCH[pid][5] != "CC-BY-SA-4.0":
            continue
        for path in (file, f"unity/Assets/UI/Portraits/{pid}.jpg"):
            note = next(r for r in RULES if r["glob"] == path)["note"]
            assert "share-alike" in note and "CC BY-SA 4.0" in note


def test_the_old_government_press_office_files_are_gone():
    assert not OLD_GPO & {r["glob"] for r in RULES}
    assert not any((ROOT / g).exists() for g in OLD_GPO)


def test_runtime_is_the_source_crop_at_512_and_never_upscaled():
    credits = (ROOT / "promo/CREDITS.md").read_text(encoding="utf-8")
    for pid, (_name, file, *_rest, box) in BATCH.items():
        runtime = ROOT / f"unity/Assets/UI/Portraits/{pid}.jpg"
        assert runtime.read_bytes() == (ROOT / file).read_bytes()
        x, y, w, h = box
        assert f"crop box x={x}, y={y}, w={w}, h={h}" in credits, pid
        assert h >= 512, f"{pid}: a {h} px tall crop would be upscaled"
        with Image.open(runtime) as image:
            assert image.format == "JPEG" and image.height == 512 and image.width == round(w * 512 / h)
            assert 370 <= image.width <= 420


def test_notice_credits_each_file_and_listing_and_catalog_match():
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    catalog = (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text()
    listing = (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text()
    for pid, (name, _file, focus, artist, licence, *_rest) in BATCH.items():
        entry = re.search(rf"^- {pid}\.jpg: (.*?)\n  (\S+)$", notice, re.M | re.S)
        assert entry and entry.group(2) == source(name), pid
        assert artist in entry.group(1) and licence in entry.group(1), pid
        assert f"{pid} | {licence} | {source(name)}\n" in listing
        block = re.search(rf"  - personId: {pid}\n(.*?)(?=  - personId:|\Z)", catalog, re.S).group(1)
        assert f"focusY: {focus}" in block


def test_licensing_and_importer_accept_cc_by_sa_portraits():
    licensing = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    row = next(l for l in licensing.splitlines() if l.startswith("| Portraits |"))
    assert "CC BY-SA 4.0" in row and "share-alike" in row
    importer = (ROOT / "unity/Assets/Editor/PortraitImport.cs").read_text(encoding="utf-8")
    assert 'p.Portrait.License != "CC BY-SA 4.0"' in importer
    tests = (ROOT / "unity/Assets/Tests/EditMode/PortraitImportTests.cs").read_text(encoding="utf-8")
    assert 'p.Portrait.License == "CC BY-SA 4.0"' in tests


def test_notice_share_alike_block_gives_the_by_sa_licence_link_and_the_share_alike_statement():
    # The header and sentence give the licence and URI for the adapted crops, which the per-file lines do not.
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    header = "CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/)"
    first = min(notice.index("\n- maimon.jpg:"), notice.index("\n- bentov.jpg:"))
    start = notice.rindex("\nCC BY", 0, first) + 1      # the licence header that governs these entries
    end = notice.find("\n\n", first)
    block = notice[start:end if end != -1 else len(notice)]
    assert "- maimon.jpg:" in block and "- bentov.jpg:" in block, "maimon.jpg and bentov.jpg sit under one header"
    assert block.startswith(header)
    statement = re.sub(r"\s+", " ", block)
    assert ("Share-alike: these cropped portraits, and the matching files in promo/ref/people/, are adaptations and are "
            "released under CC BY-SA 4.0") in statement
