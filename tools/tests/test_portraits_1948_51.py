"""Issue #6, 15 October 2026 (owner: "Yes, source more"): Knesset Archives portraits (CC BY-SA 4.0) for eight 1948-1951
office holders who had none. Source, runtime crop, credit and licence record stay together; each crop box is recorded
and was only ever downscaled; and people.yaml cites, for each person, the Knesset member page whose code is typed in
BATCH below. That code was read from the Commons file by hand on 15 October 2026; the test does not read Commons."""
from pathlib import Path
import re

from PIL import Image
import yaml12

ROOT = Path(__file__).resolve().parents[2]
# person id: (Commons file, promo/ref source crop, focus_y, Knesset member code on the file, crop box (x, y, w, h) in the
#             Commons original)
BATCH = {
    "lavon": ("Pinchas_Lavon_(00010012).jpg", "promo/ref/people/Pinhas_Lavon_CCBYSA_Knesset.jpg", 0.58, 468,
              (628, 310, 2382, 3054)),
    "eshkol": ("Levi_Eshkol_(00330374).jpg", "promo/ref/people/Levi_Eshkol_CCBYSA_Knesset.jpg", 0.46, 683,
               (10, 12, 500, 640)),
    "dinur": ("Ben-Zion_Dinur_(00370241).jpg", "promo/ref/people/Ben-Zion_Dinur_CCBYSA_Knesset.jpg", 0.47, 363,
              (10, 0, 780, 1000)),
    "burg": ("Joseph_Burg_(00330274).jpg", "promo/ref/people/Yosef_Burg_CCBYSA_Knesset.jpg", 0.52, 288,
             (65, 14, 400, 512)),
    "naftali": ("Peretz_Naftali_(00340281).jpg", "promo/ref/people/Peretz_Naftali_CCBYSA_Knesset.jpg", 0.48, 529,
                (12, 14, 430, 560)),
    "efrati": ("Joseph_Ephrati_(00330292).jpg", "promo/ref/people/Yosef_Efrati_CCBYSA_Knesset.jpg", 0.47, 268,
               (0, 10, 400, 530)),
    "shari": ("Reuven_Sheri_(00320123).jpg", "promo/ref/people/Reuven_Shari_CCBYSA_Knesset.jpg", 0.55, 669,
              (10, 33, 405, 540)),
    "kahana": ("Kalman_Kahana_(00360304).jpg", "promo/ref/people/Kalman_Kahana_CCBYSA_Knesset.jpg", 0.43, 456,
               (12, 12, 415, 540)),
}
PEOPLE = {p["id"]: p for p in yaml12.load((ROOT / "content/people.yaml").read_text(encoding="utf-8"))["people"]}
RULES = yaml12.load((ROOT / "ASSETS.portraits.yaml").read_text(encoding="utf-8"))["rules"]


def source(name):
    return "https://commons.wikimedia.org/wiki/File:" + name


def test_people_record_the_source_file_licence_and_focus():
    for pid, (name, file, focus, *_rest) in BATCH.items():
        p = PEOPLE[pid]["portrait"]
        assert (p["license"], p["source"], p["file"], p["focus_y"]) == ("CC BY-SA 4.0", source(name), file, focus)


def test_the_file_is_the_knesset_member_people_yaml_cites():
    # people.yaml cites each person's Knesset member page (mk_individual_id_t); the Commons file carries the same code.
    text = (ROOT / "content/people.yaml").read_text(encoding="utf-8")
    for pid, (*_rest, code, _box) in BATCH.items():
        record = re.search(rf"^  - id: {pid}\n(.*?)(?=^  - id: |\Z)", text, re.M | re.S).group(1)
        assert f"mk_individual_id_t={code}\"" in record, pid


def test_licence_rows_are_verified_share_alike_for_source_and_runtime():
    for pid, (name, file, *_rest) in BATCH.items():
        for path in (file, f"unity/Assets/UI/Portraits/{pid}.jpg"):
            r = next(r for r in RULES if r["glob"] == path)
            assert r["licence"] == "CC-BY-SA-4.0" and r["status"] == "verified" and r["source"] == source(name)
            assert r["artist"] == "Knesset photographer"
            assert "Cc-by-sa-4.0" in r["commons"].split("; ", 1)[1].split(", ")
            assert "share-alike" in r["note"] and "CC BY-SA 4.0" in r["note"]


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


def test_notice_credits_each_file_under_the_share_alike_block_and_listing_and_catalog_match():
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    catalog = (ROOT / "unity/Assets/UI/Portraits/PortraitCatalog.asset").read_text()
    listing = (ROOT / "unity/Assets/UI/Portraits/PORTRAITS.txt").read_text()
    header = notice.index("CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/)")
    block = notice[header:notice.index("\n\n", header)]
    for pid, (name, _file, focus, *_rest) in BATCH.items():
        entry = re.search(rf"^- {pid}\.jpg: (.*?)\n  (\S+)$", block, re.M | re.S)
        assert entry and entry.group(2) == source(name), pid
        assert "Knesset photographer" in entry.group(1) and "CC BY-SA 4.0" in entry.group(1), pid
        assert f"{pid} | CC BY-SA 4.0 | {source(name)}\n" in listing
        found = re.search(rf"  - personId: {pid}\n(.*?)(?=  - personId:|\Z)", catalog, re.S)
        assert found and f"focusY: {focus}" in found.group(1), pid
