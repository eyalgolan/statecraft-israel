"""The hex-tile board generator's region recipes and overrides (tools/hex_tiles/make_board.py, regions.yaml)."""
import json
import re
from pathlib import Path

import pytest

from hex_tiles import make_board as mb

ROOT = Path(__file__).resolve().parents[2]
REGIONS = ROOT / "tools/hex_tiles/regions.yaml"
HEX_TILES = ROOT / "unity/Assets/Scripts/Map/HexTiles"
COUNTRY = ["--regions", str(REGIONS), "--id", "israel", "--all", "--env-ring", "1"]


def known_prototypes():
    ids = set()
    for f in (HEX_TILES / "Prototypes").glob("*.cs"):
        ids |= set(re.findall(r'\[HexPrototype\("([a-z0-9_]+)"[,)]', f.read_text(encoding="utf-8")))
    return ids


def known_grounds():
    text = (HEX_TILES / "Core/HexBoard.cs").read_text(encoding="utf-8")
    layers = text[text.index("Layers ="):text.index("public static IReadOnlyList<string> Ids")]
    return set(re.findall(r'\("([a-z_]+)", "', layers))


def known_units():
    ids = set()
    for f in (HEX_TILES / "Units").glob("*.cs"):
        ids |= set(re.findall(r'\[HexUnitType\("([a-z0-9_]+)"\)\]', f.read_text(encoding="utf-8")))
    return ids


@pytest.fixture(scope="module")
def country():
    board, _ = mb.build_board(mb.parse(COUNTRY))
    return board


def test_the_whole_country_is_deterministic(country):
    again, _ = mb.build_board(mb.parse(COUNTRY))
    assert json.dumps(again) == json.dumps(country)


def test_every_region_dresses_some_land(country):
    regions = [r["id"] for r in mb.Recipes.load(str(REGIONS)).regions]
    used = {h["region"] for h in country["hexes"] if h["terrain"] != "water"}
    assert set(regions) <= used
    assert [r["id"] for r in country["regions"]] == [r for r in regions if r in used]


def test_the_board_names_only_known_prototypes_grounds_units_and_regions(country):
    protos, grounds = known_prototypes(), known_grounds()
    assert "tel_aviv_city" in protos and "desert" in grounds
    regions = {r["id"] for r in country["regions"]}
    for h in country["hexes"]:
        assert h["terrain"] in ("flat", "hills", "mountain", "water")
        assert h["ground"] in grounds, h
        assert h["prototype"] == "" or h["prototype"] in protos, h
        assert h["unit"] == "" or h["unit"] in known_units(), h
        assert h["terrain"] == "water" or h["region"] in regions, h


def test_the_recipes_name_only_known_prototypes_grounds_and_shapes():
    protos, grounds = known_prototypes(), known_grounds()
    recipes = mb.Recipes.load(str(REGIONS))
    for region in recipes.regions + [recipes.defaults]:
        for rule in region.get("settlements", []) + region.get("nature", []):
            names = [rule["prototype"]] if "prototype" in rule else [p for p, _ in rule["pick"]]
            assert set(names) <= protos, rule
            assert rule.get("ground", "grass") in grounds, rule
        assert set(region.get("ground", {}).values()) <= grounds, region.get("id")
        flora = region.get("flora", {})
        for s in flora.get("flat", []) + flora.get("hills", []):
            assert s["shape"] in ("tree", "pine", "palm", "rock", "box"), s
            assert s["model"].startswith(("sandbox:", "tiles:", "proto:")), s
    assert {u["unit"] for u in recipes.defaults["units"]} <= known_units()


def test_environment_carries_no_tiles(country):
    env = [h for h in country["hexes"] if h["env"]]
    assert env, "an --env-ring board has environment hexes"
    assert all(h["prototype"] == "" and h["unit"] == "" and h["terrain"] != "mountain" for h in env)
    assert sum(not h["env"] for h in country["hexes"]) > 4 * len(env)


def test_the_play_area_is_the_mandate_clipped_to_land(country):
    """defaults.clip mandate: every playing land hex is inside a Mandate sub-district; the land outside is a one-hex
    environment fringe round it, and the rest of the game map (Lebanon, the Golan, Transjordan, Sinai) is dropped."""
    inside = mb.mandate_hexes()
    land = [h for h in country["hexes"] if h["terrain"] != "water"]
    play = {(h["q"], h["r"]) for h in land if not h["env"]}
    assert play and play <= inside
    outside = [(h["q"], h["r"]) for h in land if (h["q"], h["r"]) not in inside]
    assert outside, "the fringe keeps some outside land as landscape"
    assert all(any(mb.hex_distance(k, i) <= 1 for i in inside) for k in outside)
    assert all(h["env"] for h in land if (h["q"], h["r"]) not in inside)
    # Every Mandate land hex the game map has is on the board: none of it is lost to the clip.
    hexes = json.loads((ROOT / "content/map/hexes.json").read_text())["hexes"]
    mandate_land = {(h["q"], h["r"]) for h in hexes if h["land"] and (h["q"], h["r"]) in inside}
    assert mandate_land <= {(h["q"], h["r"]) for h in land}
    # Water stays only next to playing land.
    for h in country["hexes"]:
        if h["terrain"] == "water":
            assert any(mb.hex_distance((h["q"], h["r"]), k) <= 1 for k in play), h


def test_clip_none_keeps_the_whole_game_map():
    whole, _ = mb.build_board(mb.parse(COUNTRY + ["--clip", "none"]))
    clipped, _ = mb.build_board(mb.parse(COUNTRY))
    assert len(whole["hexes"]) > len(clipped["hexes"])
    assert {(h["q"], h["r"]) for h in clipped["hexes"]} <= {(h["q"], h["r"]) for h in whole["hexes"]}


def test_a_rule_can_set_the_presentation_terrain():
    rules = [{"biome": "lowland", "prototype": "hula_swamp", "set_terrain": "flat"}, {"prototype": "b"}]
    assert mb.apply_rule(rules, 0, 0, biome="lowland", terrain="hills") == ("hula_swamp", None, "flat")
    assert mb.apply_rule(rules, 0, 0, biome="hills", terrain="hills") == ("b", None, None)


def test_settlements_take_their_tier_and_type_tiles(country):
    by_note = {h["note"]: h["prototype"] for h in country["hexes"] if h["note"]}
    assert by_note["tel_aviv"] == "tel_aviv_city"
    assert by_note["jerusalem"] == "jerusalem_old_city"
    assert by_note["jaffa"] == "jaffa_old_city"
    assert by_note["haifa"] == "haifa_port"   # its own tile, by id, ahead of the city-tier rule
    assert by_note["kiryat_ata"] == "haifa_bay"   # Haifa's edge: the bay's suburbs and industry
    assert by_note["rosh_pinna"] == "moshava_village"
    assert by_note["lod"] == "lydda_town" and by_note["ramla"] == "ramla_town"
    # A kibbutz takes its region's kibbutz: Galilee green in the north, a Negev outpost in the south.
    regions = {h["note"]: h["region"] for h in country["hexes"] if h["note"]}
    for note, proto in by_note.items():
        if proto in ("kibbutz_galilee", "kibbutz_negev"):
            assert regions[note] in (("hula", "upper_galilee", "lower_galilee", "jezreel", "jordan_valley") if proto == "kibbutz_galilee" else ("negev", "negev_loess")), note
    assert "maabara_camp" not in by_note.values()   # the camps date from 1950: a painter tile, not on the 1948 board
    # One place per hex: no hex note repeats.
    notes = [h["note"] for h in country["hexes"] if h["note"]]
    assert len(notes) == len(set(notes))


def test_region_lookup_lists_beat_polygons_and_file_order_breaks_ties():
    recipes = mb.Recipes({
        "defaults": {"region": "rest"},
        "regions": [
            {"id": "small", "polygon": [[0, 0], [1, 0], [1, 1], [0, 1]]},
            {"id": "big", "polygon": [[-5, -5], [5, -5], [5, 5], [-5, 5]], "hexes": [7, 7]},
            {"id": "rest"},
        ],
    })
    assert recipes.region_of({"q": 0, "r": 0, "lon": 0.5, "lat": 0.5}) == "small"
    assert recipes.region_of({"q": 0, "r": 0, "lon": 3, "lat": 3}) == "big"
    assert recipes.region_of({"q": 7, "r": 7, "lon": 0.5, "lat": 0.5}) == "big"
    assert recipes.region_of({"q": 0, "r": 0, "lon": 9, "lat": 9}) == "rest"


def test_rules_match_every_key_they_name():
    rules = [{"type": "kibbutz", "prototype": "kibbutz"}, {"terrain": ["hills", "mountain"], "tier": "city", "prototype": "a"},
             {"pick": [["b", 1.0], ["c", 0.0]], "ground": "beach"}]
    assert mb.apply_rules(rules, 0, 0, type="kibbutz", terrain="flat") == ("kibbutz", None)
    assert mb.apply_rules(rules, 0, 0, type="town", terrain="hills", tier="city") == ("a", None)
    assert mb.apply_rules(rules, 0, 0, type="town", terrain="hills", tier="town") == ("b", "beach")


def edit(q, r, value):
    return {"hex": {"q": q, "r": r}, "value": value}


def test_overrides_are_applied_last_and_survive_regeneration(tmp_path, capsys):
    out = tmp_path / "israel.json"
    mb.main(COUNTRY + ["--out", str(out)])
    first = json.loads(out.read_text())
    tel_aviv = next(h for h in first["hexes"] if h["note"] == "tel_aviv")
    squad = next(h for h in first["hexes"] if h["unit"] == "palmach_squad")
    ring = next(h for h in first["hexes"] if h["env"] and h["terrain"] != "water")
    ta, sq = (tel_aviv["q"], tel_aviv["r"]), (squad["q"], squad["r"])
    # The shape of content/map/overrides.yaml (#1099): one list per field of {hex: {q, r}, value}.
    (tmp_path / "israel.overrides.json").write_text(json.dumps({
        "board": "israel",
        "prototype": [edit(*ta, "kibbutz"), edit(999, 999, "kibbutz")],
        "ground": [edit(*ta, "plains")],
        "unit": [edit(*sq, "")],
        "facing": [edit(*sq, "0")],
        "region": [edit(ring["q"], ring["r"], "upper_galilee")],
    }))
    mb.main(COUNTRY + ["--out", str(out)])
    assert "(999,999)" in capsys.readouterr().out
    after = {(h["q"], h["r"]): h for h in json.loads(out.read_text())["hexes"]}
    assert (after[ta]["prototype"], after[ta]["ground"]) == ("kibbutz", "plains")
    assert (after[sq]["unit"], after[sq]["facing"]) == ("", 0.0)
    assert after[(ring["q"], ring["r"])]["region"] == "upper_galilee"
    # Only the edited fields move.
    assert after[ta]["terrain"] == tel_aviv["terrain"] and after[sq]["prototype"] == squad["prototype"]
    edited = {ta, sq, (ring["q"], ring["r"])}
    assert all(after[(h["q"], h["r"])] == h for h in first["hexes"] if (h["q"], h["r"]) not in edited)


def test_an_override_of_an_unknown_field_or_a_hex_twice_fails():
    board = {"hexes": [{"q": 0, "r": 0, "terrain": "flat"}]}
    with pytest.raises(ValueError):
        mb.apply_overrides(board, {"terrain": [edit(0, 0, "water")]})
    with pytest.raises(ValueError):
        mb.apply_overrides(board, {"ground": [edit(0, 0, "desert"), edit(0, 0, "grass")]})


def test_boards_follow_the_map_editors_terrain_and_location_moves(tmp_path):
    base, _ = mb.build_board(mb.parse(COUNTRY))
    hexes = {(h["q"], h["r"]): h for h in base["hexes"]}
    tel_aviv = next(k for k, h in hexes.items() if h["note"] == "tel_aviv")
    # A flat central-hills hex with no settlement, made desert by the map editor; Rosh Pinna moved to a free land hex.
    flat = next(k for k, h in hexes.items() if h["region"] == "central_hills" and h["terrain"] == "flat" and not h["note"] and not h["env"])
    free = next(k for k, h in hexes.items() if h["region"] == "upper_galilee" and h["terrain"] != "water" and not h["note"] and not h["env"])
    ov = tmp_path / "overrides.yaml"
    ov.write_text(f"terrain:\n- hex: {{q: {flat[0]}, r: {flat[1]}}}\n  value: desert\n"
                  f"control: []\nlocations:\n- id: rosh_pinna\n  hex: {{q: {free[0]}, r: {free[1]}}}\n")
    moved, _ = mb.build_board(mb.parse(COUNTRY + ["--map-overrides", str(ov)]))
    after = {(h["q"], h["r"]): h for h in moved["hexes"]}
    assert (after[flat]["ground"], after[flat]["prototype"]) == ("desert", "desert_edge")
    assert after[free]["note"] == "rosh_pinna"
    assert after[tel_aviv]["note"] == "tel_aviv"


def test_the_repos_map_overrides_file_reads():
    terrain, locations = mb.map_overrides()
    assert isinstance(terrain, dict) and isinstance(locations, dict)


def test_the_built_in_mode_writes_no_regions(tmp_path):
    out = tmp_path / "central.json"
    mb.main(["--id", "central_israel", "--out", str(out)])
    board = json.loads(out.read_text())
    assert "regions" not in board and all("region" not in h for h in board["hexes"])
