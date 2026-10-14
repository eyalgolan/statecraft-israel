"""Who held which areas, 1948-49: the true polygons (content/map/control_1948_49.geojson, tools/control_digitise.py) and
their per-hex, per-period layer (content/map/control_hexes_1948_49.yaml, tools/control_bake.py). Owner rulings of
2026-10-14: sides only from sourced data; grey only for the days an operation ran there; the 11 June snapshot may use the
line the archive map dates 9 July; the localities stay as of 1948 (no per-village dates)."""
import json
import re
from pathlib import Path

import pytest
import yaml

import borders_bake as bb
import control_bake as cb
import control_digitise as cd
import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
SOURCE = json.loads((MAP / "control_1948_49.geojson").read_text(encoding="utf-8"))
BAKED = yaml.safe_load((MAP / "control_hexes_1948_49.yaml").read_text(encoding="utf-8"))
HEXES = json.loads((MAP / "hexes.json").read_text())
META = SOURCE["statecraft"]
PERIODS = [p["id"] for p in BAKED["periods"]]
CODES = {(q, r): c for q, r, c in BAKED["hexes"]}


def code(lon: float, lat: float, period: str) -> str:
    return CODES[hg.settlement_hex(lon, lat, HEXES)][PERIODS.index(period)]


def test_the_bake_is_up_to_date_and_deterministic():
    text = cb.build()
    assert text == (MAP / "control_hexes_1948_49.yaml").read_text(encoding="utf-8")
    assert cb.build() == text


def test_periods_run_on_from_11_june_1948_without_gaps():
    ps = BAKED["periods"]
    assert str(ps[0]["from"]) == "1948-06-11", "no sourced line for 15 May 1948: the sub-districts until the first truce"
    for a, b in zip(ps, ps[1:]):
        assert str(a["until"]) == str(b["from"]), (a["id"], b["id"])
    assert ps[-1]["until"] is None
    assert str(BAKED["shown_from"]) == "1948-06-11"


def test_the_legend_turns_to_the_armistice_wording_front_by_front():
    by = {p["id"]: p for p in BAKED["periods"]}
    assert all(by[p]["legend"] == "held" and by[p]["fronts"] == [] for p in PERIODS if not p.startswith("arm_"))
    assert by["arm_1949-02-24"]["fronts"] == ["egypt"] and by["arm_1949-02-24"]["legend"] == "mixed"
    assert by["arm_1949-03-23"]["fronts"] == ["egypt", "lebanon"]
    assert by["arm_1949-04-03"]["fronts"] == ["egypt", "jordan", "lebanon"]
    assert by["arm_1949-07-20"]["fronts"] == ["egypt", "jordan", "lebanon", "syria"] and by["arm_1949-07-20"]["legend"] == "armistice"


def test_every_land_hex_of_mandatory_palestine_has_one_valid_code_per_period():
    assert set(CODES) == set(cb.mandate_hexes())
    valid = set(cb.HOLDER_CODE.values()) | set(cb.KIND_CODE.values())
    for qr, c in CODES.items():
        assert len(c) == len(PERIODS) and set(c) <= valid, qr


@pytest.mark.parametrize("place, lon, lat", [("Tel Aviv", 34.78, 32.08), ("Haifa", 34.9992, 32.8192), ("Rehovot", 34.8167, 31.8969)])
def test_the_coastal_plain_is_israeli_throughout(place, lon, lat):
    assert set(CODES[hg.settlement_hex(lon, lat, HEXES)]) == {"I"}, place


@pytest.mark.parametrize("place, lon, lat", [("Nablus", 35.2608, 32.2211), ("Hebron", 35.0986, 31.535), ("Gaza", 34.4597, 31.5075)])
def test_the_other_sides_heartlands_are_never_israeli(place, lon, lat):
    c = CODES[hg.settlement_hex(lon, lat, HEXES)]
    assert "I" not in c, (place, c)


def test_dated_changes_follow_the_sources():
    # Nazareth: the other side's on the 9 July line, contested in the Ten Days, Israeli from 18 July (Operation Dekel)
    assert code(35.3033, 32.7019, "truce1") in "VAM" and code(35.3033, 32.7019, "ten_days") == "c"
    assert code(35.3033, 32.7019, "truce2") == "I"
    # Lydda: the other side's, then Israeli (Operation Danny)
    assert code(34.8881, 31.9519, "truce1") != "I" and code(34.8881, 31.9519, "truce2") == "I"
    # Beersheba: the other side's until Yoav, Israeli on the October line
    assert code(34.7867, 31.2522, "truce2b") != "I" and code(34.7867, 31.2522, "october") == "I"
    # Tarshiha, in the Galilee pocket: contested only during Hiram, Israeli after
    assert code(35.2683, 33.009, "yoav") != "I" and code(35.2683, 33.009, "yoav_hiram") == "c" and code(35.2683, 33.009, "october") == "I"


def test_grey_contested_is_only_for_the_days_an_operation_ran():
    contested = {p for p in PERIODS for c in CODES.values() if c[PERIODS.index(p)] == "c"}
    assert contested <= {"ten_days", "shoter", "yoav", "yoav_hiram"}, contested
    ops = META["operations"]
    for f in SOURCE["features"]:
        p = f["properties"]
        if p.get("neutral_kind") == "contested":
            op = {"contested_ten_days": "ten_days", "contested_shoter": "shoter", "contested_yoav": "yoav",
                  "contested_hiram": "hiram"}[p["id"]]
            assert (p["shown_from"], p["shown_until"]) == (ops[op]["from"], ops[op]["until"]), p["id"]


def test_jerusalem_is_split_between_its_two_hexes_or_no_mans_land():
    """The city's line (S/1025; Art. V.1(b)) falls between the hex of the Old City and the one west of it at this scale: the
    first is never Israeli, the second always is; a hex holding both sides, the smaller a fifth of it, would be grey (m)."""
    q, r = hg.settlement_hex(35.2256, 31.7789, HEXES)
    east, west = CODES[(q, r)], CODES[(q - 1, r)]
    assert "I" not in east and set(east) <= set("MJm"), east
    assert set(west) <= {"I", "m"}, west


def test_the_south_is_uncoloured_until_the_jordan_agreement_closes_it():
    eilat = CODES[hg.settlement_hex(34.95, 29.56, HEXES)]
    i = PERIODS.index("arm_1949-04-03")
    assert set(eilat[:i]) == {"-"}, "no 1948 line is sourced south of the scan's 60-km northing"
    assert set(eilat[i:]) == {"I"}


def test_demilitarized_zones_are_never_the_other_side():
    zones = [f for f in SOURCE["features"] if f["properties"].get("neutral_kind") == "dmz"]
    assert {f["properties"]["id"] for f in zones} >= {"dmz_egypt_el_auja_dmz", "dmz_syria_dmz_central"}
    assert all(f["properties"]["side"] == "neutral" for f in zones)


def test_every_feature_is_sourced_draft_and_awaits_the_owners_review():
    for f in SOURCE["features"]:
        p = f["properties"]
        assert p["status"] == "draft" and p["review"] == "owner-verify", p["id"]
        assert p["sources"] and p["checked"] == cd.CHECKED, p["id"]
        if p["kind"] == "area":
            assert p["side"] in ("israel", "arab", "neutral", "unsided"), p["id"]


def test_each_area_cites_the_source_it_comes_from_with_that_sources_accuracy():
    """Review M3: the 1948 snapshots and the operations' grey cite ISA 6158/1 (about +-1-2 km); the 1949 sides, the
    demilitarized zones and the Egypt closure cite the agreements' lines (D-A) and UNTS, with their own accuracy."""
    isa = cd.ISA_MAP["url"]
    for f in SOURCE["features"]:
        p = f["properties"]
        if p["kind"] != "area":
            continue
        if (p.get("snapshot") or "").startswith("arm_") or p.get("neutral_kind") in ("dmz", "not_sourced"):
            assert "armistice_lines_1949.geojson" in p["sources"], p["id"]
            assert any("treaties.un.org" in s for s in p["sources"]), p["id"]
            assert "ISA 6158/1: a 1:250,000" not in p["accuracy"], p["id"]
        else:
            assert p["sources"][0] == isa and "1-2 km" in p["accuracy"], p["id"]
        if p.get("neutral_kind") == "contested":
            assert any("wikipedia.org" in s for s in p["sources"]), p["id"]
    arm = [f["properties"] for f in SOURCE["features"] if (f["properties"].get("snapshot") or "").startswith("arm_")]
    assert all(p["low_accuracy"] == ("jordan" in p["snapshot"] or p["snapshot"] >= "arm_1949-04-03") for p in arm)


# The tests' own table, independent of the tool's anchors (review: mutation 7): the side each town's hex must show, from
# the sources. "E": Egypt's; "A": the other side, any holder; "I": Israel's. Periods by id; "*" every drawn period.
GAZA_STRIP = {"Gaza": (34.4597, 31.5075), "Deir al-Balah": (34.3508, 31.4181), "Khan Yunis": (34.3025, 31.3439), "Rafah": (34.2519, 31.2886)}


@pytest.mark.parametrize("town", sorted(GAZA_STRIP))
def test_the_gaza_strip_is_egypts_in_every_period(town):
    """Review M1, M2: Egypt held the Strip from 1948 to the end of the game; no overlay or box may take its towns away."""
    lon, lat = GAZA_STRIP[town]
    c = CODES[hg.settlement_hex(lon, lat, HEXES)]
    assert set(c) == {"E"}, (town, c)


def test_no_assumed_box_overrides_a_source():
    """Review M2: no feature is a game assumption; Operation Horev is not drawn."""
    assert "game_assumptions" not in META
    assert not any("horev" in f["properties"]["id"] for f in SOURCE["features"])
    assert "horev" not in META["operations"]


@pytest.mark.parametrize("town, lon, lat, side, first, last", [
    ("Qalqilya", 34.9685, 32.1904, "A", "truce1", "arm_1949-07-20"),
    ("Mishmar HaYarden", 35.5997, 33.0028, "S", "truce1", "arm_1949-07-17"),
    ("Tulkarm", 35.0308, 32.3111, "A", "truce1", "arm_1949-07-20"),
    ("Jenin", 35.3, 32.4611, "A", "truce1", "arm_1949-07-20"),
    ("Hebron", 35.0986, 31.535, "A", "truce1", "arm_1949-07-20"),
    ("Lydda", 34.8881, 31.9519, "I", "truce2", "arm_1949-07-20"),
    ("Beersheba", 34.7867, 31.2522, "I", "october", "arm_1949-07-20"),
])
def test_named_towns_show_their_sourced_side(town, lon, lat, side, first, last):
    """Pins (S2, S3) and the towns around them: Qalqilya never Israeli, Mishmar HaYarden Syria's until July 1949."""
    c = CODES[hg.settlement_hex(lon, lat, HEXES)]
    for p in PERIODS[PERIODS.index(first):PERIODS.index(last) + 1]:
        got = c[PERIODS.index(p)]
        if got == "c":
            continue
        assert (got == side) if side in "IS" else (got not in "I-"), (town, p, got)


def test_hexes_agree_with_the_true_polygons_at_the_towns():
    """Review M1: away from the lines, a town's hex shows the side its point has in the true polygons (base snapshot)."""
    snaps, overlays, periods = cb.load(SOURCE)
    drawn = [p for p in periods if p["snapshot"]]
    towns = {**GAZA_STRIP, "Tel Aviv": (34.78, 32.08), "Nablus": (35.2608, 32.2211), "Ramallah": (35.2017, 31.8969),
             "Haifa": (34.9992, 32.8192), "Bethlehem": (35.2061, 31.7044), "Nazareth": (35.3033, 32.7019), "Majdal": (34.5664, 31.6658)}
    for name, (lon, lat) in towns.items():
        c = CODES[hg.settlement_hex(lon, lat, HEXES)]
        for i, p in enumerate(drawn):
            truth = cb.code_at(lon, lat, snaps[p["snapshot"]], [])
            if truth is None or c[i] in "cmz":
                continue
            assert (c[i] == "I") == (truth == "I"), (name, p["id"], c[i], truth)


def test_the_staleness_and_pins_are_recorded_as_the_sources_say():
    stale = " ".join(META["known_staleness"])
    assert "24 February 1949" in stale and "Yad Mordechai" in stale and "Huleiqat" in stale
    pins = {p["place"]: p for p in META["pinned_towns"]}
    assert pins["Mishmar HaYarden"]["holder"] == "syria" and pins["Mishmar HaYarden"]["until"] == "1949-07-20"
    assert pins["Qalqilya"]["holder"] == "jordan"
    assert all(p["source"].startswith("https://") for p in pins.values())


def test_only_the_three_front_lines_are_traced_never_the_partition_or_the_demanded_line():
    lines = [f["properties"] for f in SOURCE["features"] if f["properties"]["kind"] == "line"]
    assert {l["colour"] for l in lines} == {"red", "blue", "green"}
    assert {k for k, (c, _) in cd.STROKES.items()} == {l["id"] for l in lines}
    assert all(l["on_ink"] >= 0.5 for l in lines), [(l["id"], l["on_ink"]) for l in lines]
    assert any("partition" in t for t in META["not_traced"])


def test_holders_name_states_after_the_armistice_and_no_holder_is_dead():
    """Neutrality (review N6): Jordan is named as the state from its agreement. Every holder code the bake can write appears
    in the layer, so no holder name is listed that players never see (review r2, SR3)."""
    holders = {(f["properties"]["snapshot"], f["properties"]["holder"]) for f in SOURCE["features"] if f["properties"].get("side") == "arab"}
    assert any(h == "jordan" for s, h in holders if s == "arm_1949-07-20")
    seen = {c for codes in CODES.values() for c in codes}
    for holder, code in cb.HOLDER_CODE.items():
        if holder != "unspecified":
            assert code in seen, f"holder {holder} ({code}) is never drawn"


def test_huleiqat_is_egypts_from_8_july_to_19_october_1948():
    """Review r2, R1: en.wiki (Hulayqat) has Egypt retake it on 8 July and Giv'ati capture it on 19 October 1948; the
    archive's 9 July line disagrees, so its hex is pinned, both edges of the window tested."""
    c = CODES[hg.settlement_hex(34.6497, 31.6008, HEXES)]
    at = lambda p: c[PERIODS.index(p)]
    assert at("truce1") == "I", "Israeli before 8 July (Operation Barak, 13 May)"
    for p in ("ten_days", "truce2", "shoter", "truce2b"):
        assert at(p) == "E", p
    assert at("october") == "I", "Israeli after 19 October"
    pin = {p["place"]: p for p in META["pinned_towns"]}["Huleiqat"]
    assert (pin["from"], pin["until"], pin["holder"]) == ("1948-07-08", "1948-10-19", "egypt")
    assert "disagree" in pin["why"]


def test_the_ten_days_start_on_8_july_1948():
    """Review N1, pinned against the source's date, not the tool's constant."""
    by = {p["id"]: p for p in BAKED["periods"]}
    assert str(by["ten_days"]["from"]) == "1948-07-08" and str(by["truce1"]["until"]) == "1948-07-08"


def test_the_uncoloured_closure_strip_is_at_most_3_km_wide():
    """Review r2, mutant c: the band left without colour beside the straight Egypt closure is on its Israeli side only and
    at most 3 km wide: its area (119 km2 as built) stays under 3 km times the closure's length (about 40 km from Gaza to
    the frontier at N 71) plus the round ends; 8 km would make it about 330 km2."""
    f = next(f for f in SOURCE["features"] if f["properties"]["id"] == "unsourced_egypt_south")
    km_lon = hg.km_per_deg_lon(HEXES["bounds"])
    def area(ring):
        x = [lon * km_lon for lon, _ in ring]; y = [lat * 110.574 for _, lat in ring]
        return abs(sum(x[i] * y[i + 1] - x[i + 1] * y[i] for i in range(len(x) - 1))) / 2
    g = f["geometry"]
    polys = [g["coordinates"]] if g["type"] == "Polygon" else g["coordinates"]
    total = sum(area(p[0]) - sum(area(h) for h in p[1:]) for p in polys)
    assert 0 < total <= 3.0 * 40 + 3.14 * 3.0 ** 2, f"{total:.0f} km2"
    gaza = hg.settlement_hex(34.4597, 31.5075, HEXES)
    assert set(CODES[gaza]) == {"E"}


def test_the_scan_and_its_georeference_are_recorded():
    assert META["sources"][0]["sha256"] == cd.ISA_MAP["sha256"]
    assert max(META["georeference"]["rms_m"]) < 150, "the printed 10-km grid fits to about 0.1 km"
    assert META["searched_for_a_better_copy"]


def test_a_hex_takes_the_code_at_its_centre():
    snaps, overlays, periods = cb.load(SOURCE)
    drawn = [p for p in periods if p["snapshot"]]
    land = set(cb.mandate_hexes())
    pinned = {hg.settlement_hex(p["lon"], p["lat"], HEXES) for p in META["pinned_towns"]}
    checked = 0
    for h in HEXES["hexes"]:
        qr = (h["q"], h["r"])
        if qr not in land or qr in pinned or abs(h["lon"] - 35.2256) < 0.1:
            continue
        p = drawn[PERIODS.index("truce2")]
        got = cb.code_at(h["lon"], h["lat"], snaps[p["snapshot"]], [overlays[o] for o in p["overlays"]])
        if got is not None:
            assert CODES[qr][PERIODS.index("truce2")] == got, qr
            checked += 1
    assert checked > 150


def test_the_sim_never_reads_the_control_files():
    """Display only: the content hash covers only files the sim reads (ContentLoader)."""
    for cs in (ROOT / "sim" / "Statecraft.Sim").rglob("*.cs"):
        assert not re.search(r"control_(1948|hexes)", cs.read_text(encoding="utf-8"), re.IGNORECASE), cs


def test_the_localities_stay_as_of_1948():
    """Owner ruling (b), 2026-10-14: the colour claims military control only; no locality gets a date or a fate."""
    loc = (MAP / "localities_1948.yaml").read_text(encoding="utf-8")
    assert "control_1948" not in loc and "held_by" not in loc
