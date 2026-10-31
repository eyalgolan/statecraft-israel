"""content/map/unit_anchor_hexes.yaml is the committed output of tools/unit_anchor_hexes.py and must stay in sync: the sim
places the starting forces (R-UNIT-006) on these hexes instead of projecting lon/lat itself (docs/determinism.md)."""
import json
from pathlib import Path

import yaml12

import unit_anchor_hexes as uah

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
UNITS = yaml12.load((MAP / "units_1948.yaml").read_text(encoding="utf-8"))["units"]


def test_committed_file_is_up_to_date():
    assert (MAP / "unit_anchor_hexes.yaml").read_text() == uah.render(HEXES, UNITS), "rerun: uv run tools/unit_anchor_hexes.py"


def test_two_known_anchors_are_pinned_literally():
    rows = {(r["id"], r["anchor"]): (r["q"], r["r"]) for r in yaml12.load((MAP / "unit_anchor_hexes.yaml").read_text())["anchors"]}
    assert rows[("carmeli", 0)] == (6, 9)   # Haifa (#743, owner, 20 October 2026), the hex settlement_hexes.yaml gives haifa
    assert rows[("kiryati", 0)] == (-3, 21)   # Tel Aviv, the hex settlement_hexes.yaml gives tel_aviv


def test_an_anchor_on_the_shore_takes_the_land_hex_a_settlement_would():
    """#1183 (4.4 km bake): Haifa's anchor point falls in a sea hex, which the sim refuses (StartingForces). An anchor is
    placed by the settlements' rule (hexgrid.settlement_hex): its own hex if land, else the nearest land neighbour."""
    land = {(h["q"], h["r"]) for h in HEXES["hexes"] if h["land"]}
    rows = yaml12.load(uah.render(HEXES, UNITS))["anchors"]
    assert all((r["q"], r["r"]) in land for r in rows), [r for r in rows if (r["q"], r["r"]) not in land]


def test_an_enemy_anchor_on_a_players_locality_takes_the_nearest_free_neighbour():
    """#1183 (owner, 22 October 2026, issuecomment-6350228026) and #1305 (issuecomment of 22 October): an enemy anchor
    whose hex holds a player's locality (a settlement not in mvp.enemy_held_localities) goes on the nearest free land
    neighbour, measured from the anchor's own point; free means no settlement and no other anchor. Otherwise the scripted
    enemy move could never leave it (Movement.NoEntry for the enemy)."""
    rows = yaml12.load(uah.render(HEXES, UNITS))["anchors"]
    player = uah.player_locality_hexes()
    enemy = {u["id"] for u in UNITS if u.get("side") != "israel"}
    assert not [r for r in rows if r["id"] in enemy and (r["q"], r["r"]) in player]
    pos = {(r["id"], r["anchor"]): (r["q"], r["r"]) for r in rows}
    assert pos[("syrian_army", 1)] == (15, 6)   # the 11 June bridgehead, next to Rosh Pinna's (14, 6), the owner's hex


def test_a_hex_shared_by_a_player_locality_and_an_enemy_held_one_is_the_players_in_any_row_order(monkeypatch):
    """#1796 (owner review, 31 October 2026): at 4.4 km Rehovot shares Ramla's hex (-4, 24). Ramla is enemy-held, Rehovot
    is not, so the hex is a player's locality whichever row comes last in settlement_hexes.yaml (Movement.NoEntry builds
    the enemy's set from the player's settlements' rows, not from one id per hex)."""
    assert (-4, 24) in uah.player_locality_hexes()
    load = uah.yaml12.load

    def reversed_rows(text):
        data = load(text)
        if isinstance(data, dict) and "settlements" in data:
            data["settlements"] = list(reversed(data["settlements"]))
        return data

    monkeypatch.setattr(uah.yaml12, "load", reversed_rows)
    assert (-4, 24) in uah.player_locality_hexes()
