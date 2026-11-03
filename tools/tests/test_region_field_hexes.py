"""#1691 Q4: the 20 opening field hexes in content/regions.yaml (owner, 2 November 2026: "Approve as drafted
(Recommended)"), by the per-region split of 27 October 2026 ("Approve as proposed"). The sim checks that each is a land
hex of its own region; the sim never reads the control layer, so the Israeli-held-on-15-May check is here, with the
workable-biome check (coastal_plain, lowland, valley). A person's check of each row is still pending."""
import json
from pathlib import Path

import yaml12

ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT / "content"
SPLIT = {"galilee": 2, "valleys": 6, "haifa_sharon": 6, "tel_aviv_jaffa": 2, "central_plain": 2,
         "jerusalem_hills": 0, "northern_hills": 0, "southern_coast": 1, "negev": 1}
WORKABLE = {"coastal_plain", "lowland", "valley"}


def regions():
    return yaml12.load((CONTENT / "regions.yaml").read_text(encoding="utf-8"))["player_regions"]


def fields(region):
    return [(h["q"], h["r"]) for h in region.get("field_hexes", [])]


def test_twenty_field_hexes_by_the_approved_split():
    got = {r["id"]: len(fields(r)) for r in regions()}
    assert got == SPLIT
    every = [h for r in regions() for h in fields(r)]
    assert len(every) == 20 and len(set(every)) == 20


def test_each_field_hex_passes_the_four_checks():
    hexes = {(h["q"], h["r"]): h for h in json.loads((CONTENT / "map" / "hexes.json").read_text())["hexes"]}
    control = yaml12.load((CONTENT / "map" / "control_hexes_1948_49.yaml").read_text(encoding="utf-8"))
    assert control["periods"][0]["id"] == "opening" and str(control["periods"][0]["from"]) == "1948-05-15"
    opening = {(q, r): codes[0] for q, r, codes in control["hexes"]}
    for region in regions():
        own = {(h["q"], h["r"]) for h in region["member_hexes"]}
        for hex_ in fields(region):
            assert hexes[hex_]["land"], (region["id"], hex_)
            assert hexes[hex_]["biome"] in WORKABLE, (region["id"], hex_)
            assert opening[hex_] == "I", (region["id"], hex_)
            assert hex_ in own, (region["id"], hex_)
