"""content/map/settlement_hexes.yaml is the committed output of tools/settlement_hexes.py and must stay in sync."""
import json
from pathlib import Path

import yaml12

import hexgrid as hg
import settlement_hexes as sh

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
TOWNS = yaml12.load((MAP / "settlements_1948.yaml").read_text())["settlements"]
BY_QR = {(h["q"], h["r"]): h for h in HEXES["hexes"]}


def test_committed_file_is_up_to_date():
    assert (MAP / "settlement_hexes.yaml").read_text() == sh.render(HEXES, TOWNS), "rerun: uv run tools/settlement_hexes.py"


def test_every_settlement_is_on_its_rule_hex_and_that_hex_is_land():
    rows = yaml12.load((MAP / "settlement_hexes.yaml").read_text())["settlements"]
    assert [r["id"] for r in rows] == [t["id"] for t in TOWNS]
    for row, town in zip(rows, TOWNS):
        assert (row["q"], row["r"]) == hg.settlement_hex(town["lon"], town["lat"], HEXES), row["id"]
        assert BY_QR[(row["q"], row["r"])]["land"], row["id"]


def test_tiberias_is_moved_off_the_lake():
    rows = {r["id"]: r for r in yaml12.load((MAP / "settlement_hexes.yaml").read_text())["settlements"]}
    assert BY_QR[(rows["tiberias"]["q"], rows["tiberias"]["r"])]["biome"] != "lake"


def test_two_known_settlements_are_pinned_literally():
    rows = {r["id"]: (r["q"], r["r"]) for r in yaml12.load((MAP / "settlement_hexes.yaml").read_text())["settlements"]}
    assert rows["jerusalem"] == (0, 23)
    # jaffa and tel_aviv share this hex legitimately: Jaffa is the old port town beside Tel Aviv, inside one 5 km hex.
    assert rows["tel_aviv"] == (-3, 19)
    assert rows["jaffa"] == rows["tel_aviv"]
