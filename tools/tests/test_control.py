"""Who held which areas, 1948-49: the true polygons (content/map/control_1948_49.geojson, tools/control_digitise.py) and
their per-hex, per-period layer (content/map/control_hexes_1948_49.yaml, tools/control_bake.py). Owner rulings of
2026-10-14: sides only from sourced data; grey only for the days an operation ran there; the 11 June snapshot may use the
line the archive map dates 9 July; the localities stay as of 1948 (no per-village dates). Owner decision of 16 October
2026 (#674, option b): from 15 May to 10 June 1948 the 9 July lines, adjusted (research note control-15-may-1948.md §9)."""
import json
import math
import re
from pathlib import Path
from urllib.parse import urlparse

import pytest
import yaml12

import borders_bake as bb
import control_bake as cb
import control_digitise as cd
import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
SOURCE = json.loads((MAP / "control_1948_49.geojson").read_text(encoding="utf-8"))
BAKED = yaml12.load((MAP / "control_hexes_1948_49.yaml").read_text(encoding="utf-8"))
HEXES = json.loads((MAP / "hexes.json").read_text())
META = SOURCE["statecraft"]
PERIODS = [p["id"] for p in BAKED["periods"]]
CODES = {(q, r): c for q, r, c in BAKED["hexes"]}
SETTLEMENTS = yaml12.load((MAP / "settlements_1948.yaml").read_text(encoding="utf-8"))


# The hexes of cb.CONTESTED_SHARED_HEXES (owner on #1796, 26 October 2026, "Contested + move target"): grey in every period
# where their settlements' points disagree, pinned exactly by test_ramla_and_rehovots_shared_hex_is_contested_where_their_
# points_disagree. At 4.4 km (#1183) that is (-4, 24), Ramla's and Rehovot's: grey from 15 May until Operation Danny took
# Ramla (12 July), Israeli after. The older tests below name it where it meets their rule, and nowhere else.
SHARED_CONTESTED = {hg.settlement_hex(s["lon"], s["lat"], HEXES) for s in SETTLEMENTS["settlements"]
                    if any(s["id"] in pair for pair in cb.CONTESTED_SHARED_HEXES)}


def code(lon: float, lat: float, period: str) -> str:
    return CODES[hg.settlement_hex(lon, lat, HEXES)][PERIODS.index(period)]


def test_the_bake_is_up_to_date_and_deterministic():
    text = cb.build()
    assert text == (MAP / "control_hexes_1948_49.yaml").read_text(encoding="utf-8")
    assert cb.build() == text


def test_periods_run_on_from_15_may_1948_without_gaps():
    """#674 (b): coloured from the game's start, 15 May 1948; nothing before it."""
    ps = BAKED["periods"]
    assert str(ps[0]["from"]) == "1948-05-15", "the 9 July lines, adjusted, from the game's start"
    for a, b in zip(ps, ps[1:]):
        assert str(a["until"]) == str(b["from"]), (a["id"], b["id"])
    assert ps[-1]["until"] is None
    assert str(BAKED["shown_from"]) == "1948-05-15"


def test_the_legend_turns_to_the_armistice_wording_front_by_front():
    by = {p["id"]: p for p in BAKED["periods"]}
    assert by["opening"]["legend"] == "adjusted" and by["opening"]["fronts"] == []
    assert all(by[p]["legend"] == "held" and by[p]["fronts"] == [] for p in PERIODS if not p.startswith("arm_") and p != "opening")
    assert by["arm_1949-02-24"]["fronts"] == ["egypt"] and by["arm_1949-02-24"]["legend"] == "mixed"
    assert by["arm_1949-03-23"]["fronts"] == ["egypt", "lebanon"]
    assert by["arm_1949-04-03"]["fronts"] == ["egypt", "jordan", "lebanon"]
    assert by["arm_1949-07-20"]["fronts"] == ["egypt", "jordan", "lebanon", "syria"] and by["arm_1949-07-20"]["legend"] == "armistice"


def test_every_land_hex_of_mandatory_palestine_has_one_valid_code_per_period():
    # Plus a mapped settlement's hex whose centre is off the land: Hanita's (10, 4) at 4.4 km (owner on #1796, "Give it a row").
    assert set(CODES) == set(cb.mandate_hexes()) | {hg.settlement_hex(s["lon"], s["lat"], HEXES) for s in SETTLEMENTS["settlements"]}
    valid = set(cb.HOLDER_CODE.values()) | set(cb.KIND_CODE.values())
    for qr, c in CODES.items():
        assert len(c) == len(PERIODS) and set(c) <= valid, qr


@pytest.mark.parametrize("place, lon, lat", [("Tel Aviv", 34.78, 32.08), ("Haifa", 34.9992, 32.8192), ("Rehovot", 34.8167, 31.8969)])
def test_the_coastal_plain_is_israeli_throughout(place, lon, lat):
    """From 11 June; in the opening period Rehovot's sub-district (Ramle) is grey (#674: Yibna and the CIA's tenuously
    held Rishon-Sarafand area), tested below. Rehovot's hex is Ramla's too at 4.4 km (SHARED_CONTESTED): grey while Ramla
    was the other side's (to the Ten Days), Israeli from truce2."""
    c = CODES[hg.settlement_hex(lon, lat, HEXES)]
    if hg.settlement_hex(lon, lat, HEXES) in SHARED_CONTESTED:
        assert c[T1:PERIODS.index("truce2")] == "cc" and set(c[PERIODS.index("truce2"):]) == {"I"}, (place, c)
        return
    assert set(c[T1:]) == {"I"}, place


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
    contested = {p for p in PERIODS for qr, c in CODES.items() if c[PERIODS.index(p)] == "c" and qr not in SHARED_CONTESTED}
    assert contested <= {"opening", "ten_days", "shoter", "yoav", "yoav_hiram"}, contested
    ops = META["operations"]
    for f in SOURCE["features"]:
        p = f["properties"]
        if p.get("neutral_kind") == "contested" and not p["id"].startswith("contested_opening_"):
            op = {"contested_ten_days": "ten_days", "contested_shoter": "shoter", "contested_yoav": "yoav",
                  "contested_hiram": "hiram"}[p["id"]]
            assert (p["shown_from"], p["shown_until"]) == (ops[op]["from"], ops[op]["until"]), p["id"]


def test_jerusalem_is_split_between_its_two_hexes_or_no_mans_land():
    """The city's line (S/1025; Art. V.1(b)) falls between the hex of the Old City and the one west of it at this scale: the
    first is never Israeli, the second always is; a hex holding both sides, the smaller a fifth of it, would be grey (m)."""
    q, r = hg.settlement_hex(35.2256, 31.7789, HEXES)
    east, west = CODES[(q, r)], CODES[(q - 1, r)]
    assert "I" not in east and set(east) <= set("MJm"), east
    assert set(west[T1:]) <= {"I", "m"}, west
    assert west[OPEN] == "c", "15 May - 10 June: the hex west of the Old City's is the corridor's end (Motza, El Qastal), grey"


def test_the_south_is_uncoloured_until_the_jordan_agreement_closes_it():
    eilat = CODES[hg.settlement_hex(34.95, 29.56, HEXES)]
    i = PERIODS.index("arm_1949-04-03")
    assert set(eilat[:i]) == {"-"}, "no 1948 line is sourced south of the scan's 60-km northing"
    revivim = CODES[hg.settlement_hex(34.7211, 31.0436, HEXES)]
    assert revivim[PERIODS.index("opening")] == "-", "south of the 60-km northing: no colour, as from 11 June (not pinned)"
    assert set(eilat[i:]) == {"I"}


def test_demilitarized_zones_are_never_the_other_side():
    zones = [f for f in SOURCE["features"] if f["properties"].get("neutral_kind") == "dmz"]
    assert {f["properties"]["id"] for f in zones} >= {"dmz_egypt_el_auja_dmz", "dmz_syria_dmz_central"}
    assert all(f["properties"]["side"] == "neutral" for f in zones)


def test_every_feature_is_sourced_draft_and_awaits_the_owners_review():
    for f in SOURCE["features"]:
        p = f["properties"]
        assert p["status"] == "draft" and p["review"] == "owner-verify", p["id"]
        assert p["sources"] and p["checked"] in (cd.CHECKED, cd.CHECKED_OPENING), p["id"]
        assert p["checked"] == cd.CHECKED or p["id"] in OPENING_FEATURES, p["id"]
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
        if p["id"] == "unsourced_south_opening":
            continue    # tested in test_the_south_is_uncoloured_in_the_opening_period_but_its_settlements
        if (p.get("snapshot") or "").startswith("arm_") or p.get("neutral_kind") in ("dmz", "not_sourced"):
            assert "armistice_lines_1949.geojson" in p["sources"], p["id"]
            assert any(host(s) == "treaties.un.org" for s in p["sources"]), p["id"]
            assert "ISA 6158/1: a 1:250,000" not in p["accuracy"], p["id"]
        else:
            assert p["sources"][0] == isa and "1-2 km" in p["accuracy"], p["id"]
        if p.get("neutral_kind") == "contested":
            assert any(host(s) == "wikipedia.org" or host(s).endswith(".wikipedia.org") for s in p["sources"]), p["id"]
    arm = [f["properties"] for f in SOURCE["features"] if (f["properties"].get("snapshot") or "").startswith("arm_")]
    for p in arm:   # D-A ruling 2: low accuracy only where the Jordan line bounds the area
        jordan_in = p["snapshot"] >= "arm_1949-04-03"
        if p["side"] == "arab":
            assert p["low_accuracy"] == (jordan_in and p["holder"] == "jordan"), p["id"]
        else:
            assert bool(p["low_accuracy"]) == jordan_in, p["id"]
    assert not next(p for p in arm if p["id"] == "arm_1949-07-20:arab:egypt")["low_accuracy"], "Egypt's Strip is not Jordan-derived"


# The tests' own table, independent of the tool's anchors (review: mutation 7): the side each town's hex must show, from
# the sources. "E": Egypt's; "A": the other side, any holder; "I": Israel's. Periods by id; "*" every drawn period.
GAZA_STRIP = {"Gaza": (34.4597, 31.5075), "Deir al-Balah": (34.3508, 31.4181), "Khan Yunis": (34.3025, 31.3439), "Rafah": (34.2519, 31.2886)}


@pytest.mark.parametrize("town", sorted(GAZA_STRIP))
def test_the_gaza_strip_is_egypts_in_every_period(town):
    """Review M1, M2: Egypt held the Strip from June 1948 to the end of the game; no overlay or box may take its towns away.
    Before 11 June the south has no colour (#674, owner ruling Q3)."""
    lon, lat = GAZA_STRIP[town]
    c = CODES[hg.settlement_hex(lon, lat, HEXES)]
    assert c[PERIODS.index("opening")] == "-", (town, c)
    assert set(c[PERIODS.index("truce1"):]) == {"E"}, (town, c)


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
    """Pins (S2, S3) and the towns around them: Qalqilya never Israeli, Mishmar HaYarden Syria's until July 1949. A pin is
    read on its own hex, which for Mishmar HaYarden is off Rosh Pinna's (owner on #1796, 27 October 2026, "Move the pin")."""
    pinned = {p["place"]: qr for qr, p in cb.pin_hexes(SOURCE, HEXES)}
    c = CODES[pinned.get(town) or hg.settlement_hex(lon, lat, HEXES)]
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
    assert {l["colour"] for l in lines} == {"red", "blue", "green", "cia_tenuous"}
    assert set(cd.STROKES) | set(cd.CIA_STROKES) == {l["id"] for l in lines}
    isa = [l for l in lines if l["colour"] in ("red", "blue", "green")]
    assert all(l["on_ink"] >= 0.5 for l in isa), [(l["id"], l["on_ink"]) for l in isa]
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
    # #1183 (4.4 km): in the Ten Days the hex is grey from the traced lines, which a pin does not override (rule 6).
    assert at("ten_days") == "c"
    for p in ("truce2", "shoter", "truce2b"):
        assert at(p) == "E", p
    assert at("october") == "I", "Israeli after 19 October"
    pin = {p["place"]: p for p in META["pinned_towns"]}["Huleiqat"]
    assert (pin["from"], pin["until"], pin["holder"]) == ("1948-07-08", "1948-10-19", "egypt")
    assert "disagree" in pin["why"]


def test_latrun_is_jordans_from_11_june_1948():
    """#1183 (owner, 26 October 2026): en.wiki (Latrun) has the British hand the fort to the Arab Legion on their withdrawal
    and the fort stay a salient under Jordanian control after the 1949 armistice; on the 4.4 km grid the traced lines put
    its hex on the Israeli side in every period, so the hex is pinned to Jordan, and the Legion's 11 June token stands on it."""
    c = CODES[hg.settlement_hex(34.98139, 31.83194, HEXES)]
    assert c[PERIODS.index("opening")] == "A", "the other side, unnamed, in the opening snapshot (#674)"
    assert set(c[PERIODS.index("truce1"):]) == {"J"}, c
    pin = {p["place"]: p for p in META["pinned_towns"]}["Latrun"]
    assert (pin["from"], pin["until"], pin["holder"]) == ("1948-06-11", None, "jordan")
    assert "salient under Jordanian control" in pin["why"]


def test_the_ten_days_start_on_8_july_1948():
    """Review N1, pinned against the source's date, not the tool's constant."""
    by = {p["id"]: p for p in BAKED["periods"]}
    assert str(by["ten_days"]["from"]) == "1948-07-08" and str(by["truce1"]["until"]) == "1948-07-08"
    assert META["operations"]["ten_days"]["from"] == str(by["ten_days"]["from"]), "the overlay's window and the period agree"


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
    assert set(CODES[gaza][PERIODS.index("truce1"):]) == {"E"}


def test_the_scan_and_its_georeference_are_recorded():
    assert META["sources"][0]["sha256"] == cd.ISA_MAP["sha256"]
    assert max(META["georeference"]["rms_m"]) < 150, "the printed 10-km grid fits to about 0.1 km"
    assert META["searched_for_a_better_copy"]


def test_a_hex_takes_the_code_at_its_centre():
    snaps, overlays, periods = cb.load(SOURCE)
    drawn = [p for p in periods if p["snapshot"]]
    land = set(cb.mandate_hexes())
    pinned = {qr for qr, _ in cb.pin_hexes(SOURCE, HEXES)}
    # A mapped settlement's hex is the one exception: it takes the code at the settlement's own point (#1659), which
    # test_a_settlement_hex_takes_the_code_at_its_own_point covers instead.
    towns = {hg.settlement_hex(s["lon"], s["lat"], HEXES) for s in SETTLEMENTS["settlements"]}
    checked = 0
    for h in HEXES["hexes"]:
        qr = (h["q"], h["r"])
        if qr not in land or qr in pinned or qr in towns or abs(h["lon"] - 35.2256) < 0.1:
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


# --- #674, owner decision of 16 October 2026, option (b): 15 May - 10 June 1948, "lines of 9 July 1948, adjusted" ------

def host(url: str) -> str:
    """The URL's host name, so a source check matches the site, not a substring anywhere in the URL. A Wayback Machine copy
    (web.archive.org/web/<timestamp>/<url>) gives the archived page's host."""
    u = urlparse(url)
    if u.hostname == "web.archive.org":
        return host(u.path.split("/", 3)[3])
    return u.hostname or ""


SUBS = {(q, r): rid for q, r, rid in yaml12.load((MAP / "subdistrict_hexes_1948.yaml").read_text(encoding="utf-8"))["hexes"]}
SUB_KEY = {r["id"]: r["key"] for r in yaml12.load((MAP / "subdistrict_hexes_1948.yaml").read_text(encoding="utf-8"))["regions"]}
OPEN, T1 = PERIODS.index("opening") if "opening" in PERIODS else -1, PERIODS.index("truce1")
OPENING_OVERLAYS = {"unsourced_south_opening", "contested_opening_hula", "contested_opening_jenin", "contested_opening_jerusalem"}
OPENING_FEATURES = {"opening:israel", "opening:arab:unspecified", "cia_hula"} | OPENING_OVERLAYS
# Owner ruling 2 of 16 October 2026 on #686 (cut breadth): in the Ramle and Acre sub-districts grey only the hexes that
# changed hands between 15 May and 10 June 1948: Yibna's (occupied 5 June) and Acre's (surrendered on the night of 17/18
# May). Rehovot, Rishon and Nahariya, and every other hex of the two sub-districts, keep their 9 July side.
CUT_GREY = {"ramle": {(-6, 25)}, "acre": {(8, 7)}}   # Yibna's and Acre's hexes on the 4.4 km grid (#1183)
# The Jewish settlements of the Gaza sub-district and of the Beersheba sub-district north of the 60-km northing whose 1948
# site the repository already holds, sourced (settlements_1948.yaml; the closure check's anchors): Q3 "settlement hexes blue".
SOUTH_SETTLEMENTS = {"Yad Mordechai": (34.5583, 31.5886), "Negba": (34.683, 31.6608), "Nir Am": (34.5808, 31.5194),
                     "Dorot": (34.6453, 31.5064), "Shoval": (34.7428, 31.4128), "Mishmar HaNegev": (34.7186, 31.3642),
                     "Hatzerim": (34.7142, 31.2403), "Nevatim": (34.8828, 31.2236)}


def sub(lon, lat):
    return SUB_KEY[SUBS[hg.settlement_hex(lon, lat, HEXES)]]


def test_the_opening_period_runs_from_15_may_to_10_june_1948_on_the_9_july_line():
    by = {p["id"]: p for p in BAKED["periods"]}
    assert PERIODS[0] == "opening" and PERIODS[1] == "truce1"
    o = by["opening"]
    assert (str(o["from"]), str(o["until"])) == ("1948-05-15", "1948-06-11"), "nothing before 15 May; 11 June is the first truce"
    assert o["legend"] == "adjusted" and o["line_date"] == "1948-07-09", "the caption names the 9 July 1948 lines, adjusted"
    src = next(p for p in META["periods"] if p["id"] == "opening")
    assert src["snapshot"] == "opening" and set(src["overlays"]) == OPENING_OVERLAYS and src["jerusalem"] == "no_mans_land"


def test_the_opening_snapshot_is_the_9_july_geometry_with_no_holder_named():
    """No new line: the opening's areas are truce1's polygons; the other side is not named before the state armies and the
    ALA's re-entry (7 June) are sourced to an area (research note §4)."""
    feats = {f["properties"]["id"]: f for f in SOURCE["features"]}
    assert feats["opening:israel"]["geometry"] == feats["truce1:israel"]["geometry"]
    other = [f for f in SOURCE["features"] if f["properties"].get("snapshot") == "opening" and f["properties"]["side"] == "arab"]
    assert [f["properties"]["holder"] for f in other] == ["unspecified"]
    assert not set("EJSVM") & {c[OPEN] for c in CODES.values()}


def test_away_from_the_adjustments_each_hex_keeps_its_9_july_side():
    pinned = {hg.settlement_hex(x, y, HEXES) for x, y in SOUTH_SETTLEMENTS.values()}
    checked = 0
    for qr, c in CODES.items():
        if c[OPEN] in "cm-" or qr in pinned:
            continue
        assert (c[OPEN] == "I") == (c[T1] == "I"), (qr, c[OPEN], c[T1])
        checked += 1
    assert checked > 150


@pytest.mark.parametrize("place, lon, lat", [("Acre", 35.0817, 32.9278), ("Zir'in", 35.32, 32.555), ("Yibna", 34.7463, 31.866),
                                             ("Metulla", 35.578, 33.278), ("Mishmar HaYarden", 35.5997, 33.0028),
                                             ("Motza", 35.165, 31.79)])
def test_the_adjusted_areas_are_contested(place, lon, lat):
    """Acre (surrendered 17/18 May) and Yibna (5 June): the hex of a place that changed hands (owner ruling 2, #686);
    Lajjun-Zir'in (28 May), cut by its sub-district; the CIA 18 May tenuously held Safad-Hula loop (traced) and the corridor
    (cut by sub-district)."""
    assert CODES[hg.settlement_hex(lon, lat, HEXES)][OPEN] == "c", place


def test_the_ramle_and_acre_cuts_grey_only_the_hexes_that_changed_hands():
    """Owner ruling 2 of 16 October 2026 on #686: grey only Yibna's and the Acre coast's hexes; Rehovot, Rishon and
    Nahariya, and every other Israeli-held hex of the two sub-districts that did not change hands, stay blue."""
    for key, want in CUT_GREY.items():
        got = {qr for qr, c in CODES.items() if SUB_KEY[SUBS[qr]] == key and c[OPEN] == "c" and qr not in SHARED_CONTESTED}
        assert got == want, (key, sorted(got))
        for qr, c in CODES.items():
            if SUB_KEY[SUBS[qr]] == key and qr not in want | SHARED_CONTESTED:
                assert c[OPEN] == ("I" if c[T1] == "I" else "A"), (key, qr, c[OPEN], c[T1])
    for place, lon, lat in [("Rehovot", 34.8167, 31.8969), ("Rishon LeZion", 34.8, 31.95), ("Nahariya", 35.0925, 33.0036)]:
        # Rehovot's hex is Ramla's too at 4.4 km: grey by the shared-hex rule, not by a cut (SHARED_CONTESTED)
        assert code(lon, lat, "opening") == ("c" if hg.settlement_hex(lon, lat, HEXES) in SHARED_CONTESTED else "I"), place
    by = {p["place"]: p for p in META["contested_places"]}
    assert set(by) == {"Yibna", "Acre"}
    assert {hg.settlement_hex(p["lon"], p["lat"], HEXES) for p in by.values()} == set().union(*CUT_GREY.values())
    for p in by.values():
        assert (p["from"], p["until"]) == ("1948-05-15", "1948-06-11") and p["why"], p["place"]
        assert p["sources"] and all(host(s) == "en.wikipedia.org" for s in p["sources"]), p["place"]
    feats = {f["properties"]["id"] for f in SOURCE["features"]}
    assert not feats & {"contested_opening_acre", "contested_opening_ramle"}, "no sub-district cut is drawn for Ramle or Acre"


@pytest.mark.parametrize("place, lon, lat, want", [("Tel Aviv", 34.78, 32.08, "I"), ("Haifa", 34.9992, 32.8192, "I"),
                                                   ("Hadera", 34.9167, 32.45, "I"), ("Nahalal", 35.1967, 32.69, "I"),
                                                   ("Ein Harod", 35.3908, 32.5603, "I"), ("Nablus", 35.2608, 32.2211, "A"),
                                                   ("Nazareth", 35.3033, 32.7019, "A"), ("Lydda", 34.8881, 31.9519, "A"),
                                                   ("Hebron", 35.0986, 31.535, "A")])
def test_the_rest_keeps_the_9_july_sides(place, lon, lat, want):
    assert CODES[hg.settlement_hex(lon, lat, HEXES)][OPEN] == want, place


def test_grey_in_the_opening_period_is_only_where_a_source_puts_it():
    allowed = {"acre", "jenin", "ramle", "jerusalem", "safad"}
    # #1183 (4.4 km): Afula's hex takes the Nazareth sub-district by borders_bake rule 5, but its centre is in the
    # Lajjun-Zir'in overlay (Jenin), so the hex is grey in a sub-district the list does not name.
    rule5 = {(7, 13): "jenin"}
    for qr, c in CODES.items():
        if c[OPEN] == "c":
            assert rule5.get(qr, SUB_KEY[SUBS[qr]]) in allowed, (qr, SUB_KEY[SUBS[qr]])
            if qr in SHARED_CONTESTED:   # grey by the shared-hex rule (Ramla's point), in the Ramle sub-district
                assert SUB_KEY[SUBS[qr]] == "ramle", qr
                continue
            assert c[T1] == "I" or SUB_KEY[SUBS[qr]] == "safad", ("grey only inside a 9 July Israeli area, or the CIA loop", qr, c[T1])
    for f in SOURCE["features"]:
        p = f["properties"]
        if p["id"] in OPENING_OVERLAYS:
            assert (p["shown_from"], p["shown_until"]) == ("1948-05-15", "1948-06-11"), p["id"]
            assert p["sources"] and p["why"], p["id"]
            if p["neutral_kind"] == "contested":
                assert any(host(s) in ("en.wikipedia.org", "www.cia.gov") for s in p["sources"]), p["id"]


def test_jerusalem_is_no_mans_land_in_the_opening_period():
    km_lon = hg.km_per_deg_lon(HEXES["bounds"])
    near = [(h["q"], h["r"]) for h in HEXES["hexes"] if (h["q"], h["r"]) in CODES
            and ((h["lon"] - 35.2256) * km_lon) ** 2 + ((h["lat"] - 31.7789) * 110.574) ** 2 <= cb.JERUSALEM_KM ** 2]
    assert len(near) >= 1, "the Old City's hex (the hex west of it is the corridor's end, tested above)"
    assert {CODES[qr][OPEN] for qr in near} == {"m"}
    assert "m" not in {c[OPEN] for qr, c in CODES.items() if qr not in near}


def test_the_south_is_uncoloured_in_the_opening_period_but_its_settlements():
    pinned = {hg.settlement_hex(x, y, HEXES): n for n, (x, y) in SOUTH_SETTLEMENTS.items()}
    for qr, c in CODES.items():
        if SUB_KEY[SUBS[qr]] in ("gaza", "beersheba"):
            assert c[OPEN] == ("I" if qr in pinned else "-"), (qr, pinned.get(qr), c[OPEN])
    pins = {p["place"]: p for p in META["pinned_towns"]}
    for n in SOUTH_SETTLEMENTS:
        assert (pins[n]["holder"], pins[n]["from"], pins[n]["until"]) == ("israel", "1948-05-15", "1948-06-11"), n
    assert CODES[hg.settlement_hex(34.5583, 31.5886, HEXES)][T1] != "I", "Yad Mordechai fell on 24 May: from 11 June the other side's"
    assert "Yad Mordechai" in " ".join(META["known_staleness"])
    f = next(f["properties"] for f in SOURCE["features"] if f["properties"]["id"] == "unsourced_south_opening")
    assert f["neutral_kind"] == "not_sourced" and f["side"] == "unsided"


def test_the_cia_map_is_recorded_georeferenced_and_its_trace_lies_on_its_ink():
    cia = next(s for s in META["sources"] if s.get("sha256") == cd.CIA_MAP["sha256"])
    assert "CIA-RDP78-01617A004700010003-6" in cia["url"] and "17 USC 105" in cia["licence"]
    g = META["georeference_cia"]
    assert g["ticks"] >= 9 and max(g["max_residual_deg"]) < 0.01
    hula = next(f["properties"] for f in SOURCE["features"] if f["properties"]["id"] == "cia_hula")
    assert hula["colour"] == "cia_tenuous" and hula["date"] == "1948-05-18" and hula["on_ink"] >= 0.8
    assert any("strongly-held" in t for t in META["not_traced"]), "the solid border cannot be told from the partition line"


def test_a_contested_place_that_greys_no_hex_fails_the_bake():
    """Fail closed: a contested place outside Mandatory Palestine's land, or with no period inside its window, must stop the
    bake, not grey nothing (a place at sea already fails in hexgrid.settlement_hex)."""
    elev, meta = bb.load_elevation()
    water = bb.water_mask(elev, meta)
    first = SOURCE["statecraft"]["contested_places"][0]
    for bad in (dict(first, place="Irbid", lon=35.85, lat=32.556), dict(first, place="no window", until="1948-05-20")):
        src = json.loads(json.dumps(SOURCE))
        src["statecraft"]["contested_places"] = [bad]
        with pytest.raises(SystemExit, match=bad["place"]):
            cb.bake(src, HEXES, water, meta)


def test_a_settlement_hex_takes_the_code_at_its_own_point():
    """#1659, owner's ruling of 25 October 2026 (a). A hex's centre can sit the other side of the traced line from the
    settlement placed in it, and a shore town such as Tiberias is placed on a substitute hex whose own hex is lake, so
    its centre says nothing about the town. Before this rule Tiberias and Safed read "Held by Arab forces" at the start
    and Hanita, held throughout, read Arab then ALA until July. The deliberate overrides still win: the contested
    places, the pins and the Jerusalem area are excluded here and covered by their own tests."""
    snaps, overlays, periods = cb.load(SOURCE)
    drawn = [p for p in periods if p["snapshot"]]
    pinned = {qr for qr, _ in cb.pin_hexes(SOURCE, HEXES)}
    greyed = {hg.settlement_hex(p["lon"], p["lat"], HEXES) for p in META.get("contested_places", [])}
    checked = 0
    for s in SETTLEMENTS["settlements"]:
        qr = hg.settlement_hex(s["lon"], s["lat"], HEXES)
        # Jerusalem only: a longitude band on its own also skips Nazareth, Nahalal and Hanita, and Hanita is one of
        # the three places this rule is for (review of #1765).
        if qr in pinned or qr in greyed or (abs(s["lon"] - 35.2256) < 0.1 and abs(s["lat"] - 31.7789) < 0.1):
            continue
        if any(s["id"] in pair for pair in cb.CONTESTED_SHARED_HEXES):   # their own test below
            continue
        for i, p in enumerate(drawn):
            ovs = [overlays[o] for o in p["overlays"] if o in overlays]
            got = cb.code_at(s["lon"], s["lat"], snaps[p["snapshot"]], ovs)
            if got is None:
                continue
            assert CODES[qr][i] == got, (s["id"], qr, p["id"])
            checked += 1
    assert checked > 200, checked


def test_hex_with_a_codeless_settlement_fails_closed_naming_hex_and_ids(monkeypatch):
    """#2028: a settlement outside every snapshot and overlay codes as None; sorting the disagreeing keys must not
    compare it with a code. test-in sits in a 200 m snapshot square, test-out 150 m east of it in the same hex."""
    lon_a, lat_a = 34.336715993, 30.975938320
    lon_b = 34.338287596
    square = [
        [34.335668258, 30.975033915],
        [34.337763728, 30.975033915],
        [34.337763728, 30.976842724],
        [34.335668258, 30.976842724],
        [34.335668258, 30.975033915],
    ]
    source = {
        "features": [
            {
                "type": "Feature",
                "properties": {"kind": "area", "snapshot": "s1", "holder": "israel"},
                "geometry": {"type": "Polygon", "coordinates": [square]},
            }
        ],
        "statecraft": {
            "periods": [{"id": "p1", "snapshot": "s1", "overlays": []}],
            "pinned_towns": [],
            "contested_places": [],
        },
    }
    settlements = {
        "settlements": [
            {"id": "test-in", "lon": lon_a, "lat": lat_a},
            {"id": "test-out", "lon": lon_b, "lat": lat_a},
        ]
    }
    real_load_path = yaml12.load_path
    monkeypatch.setattr(cb.yaml12, "load_path", lambda p: settlements if p == cb.SETTLEMENTS else real_load_path(p))
    with pytest.raises(SystemExit) as exc:
        cb.bake(source, HEXES, None, None)
    text = str(exc.value)
    assert text.startswith("settlements in hex (-16, 35) disagree about its control ("), text
    assert "test-in" in text and "test-out" in text, text


def test_the_three_places_of_1659_are_held_by_israel_from_the_start():
    """The player-visible claim of #1659, pinned by name so a bake change cannot quietly undo it: the map and the city
    panel's "Held by" line. Tiberias was taken on 18 April 1948 and Safed on 10-11 May, both before the game's start on
    15 May, and Hanita was held throughout the war."""
    for sid in ("tiberias", "hanita"):
        s = next(x for x in SETTLEMENTS["settlements"] if x["id"] == sid)
        qr = hg.settlement_hex(s["lon"], s["lat"], HEXES)
        codes = CODES[qr]
        assert set(codes) == {"I"}, (sid, codes)
    safed = next(x for x in SETTLEMENTS["settlements"] if x["id"] == "safed")
    codes = CODES[hg.settlement_hex(safed["lon"], safed["lat"], HEXES)]
    assert "A" not in codes and "V" not in codes, codes
    assert codes[PERIODS.index("truce1")] == "I", codes


def test_ramla_and_rehovots_shared_hex_is_contested_where_their_points_disagree():
    """Owner on #1796, 26 October 2026, "Contested + move target": at 4.4 km (#1183) Ramla's and Rehovot's points both fall
    in hex (-4, 24) and disagree about its control. The hex reads "c" in every period where their codes differ and their
    common code otherwise. Any other pair that disagrees still fails the bake (the named list is the only exception)."""
    snaps, overlays, periods = cb.load(SOURCE)
    drawn = [p for p in periods if p["snapshot"]]
    pts = {s["id"]: s for s in SETTLEMENTS["settlements"] if s["id"] in ("ramla", "rehovot")}
    qr = hg.settlement_hex(pts["ramla"]["lon"], pts["ramla"]["lat"], HEXES)
    assert qr == hg.settlement_hex(pts["rehovot"]["lon"], pts["rehovot"]["lat"], HEXES) == (-4, 24)
    want = []
    for p in drawn:
        ovs = [overlays[o] for o in p["overlays"] if o in overlays]
        a, b = (cb.code_at(pts[i]["lon"], pts[i]["lat"], snaps[p["snapshot"]], ovs) for i in ("ramla", "rehovot"))
        want.append(a if a == b else "c")
    assert "c" in want and CODES[qr] == "".join(want), (CODES[qr], want)
    assert cb.CONTESTED_SHARED_HEXES == [frozenset({"ramla", "rehovot"})]


def test_the_mishmar_hayarden_pin_moves_off_rosh_pinnas_hex():
    """Owner on #1796, 27 October 2026, "Move the pin": at 4.4 km (#1183) the Mishmar HaYarden pin (Syria, 11 June 1948 -
    20 July 1949) falls in Rosh Pinna's hex (14, 6) and painted a player city Syrian. A non-Israeli pin whose hex holds a
    player's locality goes on the free land neighbour nearest its own point, the rule the owner gave the Syrian bridgehead
    token (tools/unit_anchor_hexes.py), so Rosh Pinna's hex reads its own point and the pin's hex reads Syria."""
    pin = {p["place"]: p for p in META["pinned_towns"]}["Mishmar HaYarden"]
    assert hg.settlement_hex(pin["lon"], pin["lat"], HEXES) == (14, 6)
    moved = dict((p["place"], qr) for qr, p in cb.pin_hexes(SOURCE, HEXES))["Mishmar HaYarden"]
    towns = {hg.settlement_hex(s["lon"], s["lat"], HEXES) for s in SETTLEMENTS["settlements"]}
    land = set(cb.mandate_hexes())
    free = [(14 + dq, 6 + dr) for dq, dr in hg.NEIGHBOURS if (14 + dq, 6 + dr) in land and (14 + dq, 6 + dr) not in towns]
    point = hg.lonlat_to_km(pin["lon"], pin["lat"], HEXES["bounds"])
    assert moved == min(free, key=lambda c: math.dist(hg.hex_center_km(*c, HEXES["hex_size_km"]), point)), moved
    # (15, 5), 5.4 km from the point. The bridgehead token's (15, 6) is 4.35 km but outside Mandatory Palestine on the
    # sub-district layer, so the control layer has no row there and a pin on it would colour nothing.
    assert moved == (15, 5) and (15, 6) not in land, moved
    rp = next(s for s in SETTLEMENTS["settlements"] if s["id"] == "rosh_pinna")
    assert hg.settlement_hex(rp["lon"], rp["lat"], HEXES) == (14, 6)
    assert "S" not in CODES[(14, 6)], CODES[(14, 6)]
    assert CODES[moved][PERIODS.index("truce1")] == "S", CODES[moved]
    # Only this pin moves: every other pin keeps the hex holding its point.
    for qr, p in cb.pin_hexes(SOURCE, HEXES):
        if p["place"] != "Mishmar HaYarden":
            assert qr == hg.settlement_hex(p["lon"], p["lat"], HEXES), p["place"]
