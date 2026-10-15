"""Checks the committed 1948 routes (content/map/routes_1948.yaml and the traced route_paths_1948.yaml) and the pure parts
of tools/routes_1948.py. The trace itself needs the map tiles (network) and scikit-image, so it runs only where both are
present; CI checks the committed result."""
import json
import math
from pathlib import Path

import numpy as np
import pytest
import yaml12

import routes_1948 as rt

ROOT = Path(__file__).resolve().parents[2]
BOUNDS = json.loads((ROOT / "content" / "map" / "hexes.json").read_text())["bounds"]
ROUTES = yaml12.load((ROOT / "content" / "map" / "routes_1948.yaml").read_text(encoding="utf-8"))
PATHS = yaml12.load((ROOT / "content" / "map" / "route_paths_1948.yaml").read_text(encoding="utf-8"))["routes"]
BY_ID = {r["id"]: r for r in PATHS}


def test_routes_file_is_valid():
    rt.validate_routes(ROUTES, BOUNDS)
    assert ROUTES["map_source"]["licence"] == "Public-Domain" and ROUTES["map_source"]["checked"]


def test_the_issue_routes_are_there():
    """Issue #35 names the Jaffa-Jerusalem line, the coastal main line and the 1948 roads to Jerusalem."""
    ids = {r["id"] for r in ROUTES["routes"]}
    assert {"jaffa_jerusalem_railway", "kantara_main_line_south", "eastern_railway", "hadera_haifa_railway",
            "jaffa_jerusalem_road", "burma_road", "tel_aviv_haifa_road", "coastal_road_south"} <= ids


def test_paths_match_the_routes_file():
    assert [r["id"] for r in PATHS] == [r["id"] for r in ROUTES["routes"]]
    for src in ROUTES["routes"]:
        p = BY_ID[src["id"]]
        assert (p["kind"], p["status"]) == (src["kind"], src["status"]), src["id"]


@pytest.mark.parametrize("route", ROUTES["routes"], ids=lambda r: r["id"])
def test_each_path_runs_from_its_first_to_its_last_waypoint(route):
    pts = [tuple(p) for p in BY_ID[route["id"]]["points"]]
    assert len(pts) >= 2
    for lon, lat in pts:
        assert BOUNDS["lon0"] <= lon <= BOUNDS["lon1"] and BOUNDS["lat0"] <= lat <= BOUNDS["lat1"]
    first, last = route["via"][0], route["via"][-1]
    # A waypoint snaps to its symbol within SNAP_M (plus the spur cut at the ends).
    assert rt.km_between(pts[0], (first["lon"], first["lat"])) <= rt.SNAP_M / 1000 + 0.3, route["id"]
    assert rt.km_between(pts[-1], (last["lon"], last["lat"])) <= rt.SNAP_M / 1000 + 0.3, route["id"]
    # No wild detours: the line is at least the waypoints' chord and at most twice it (hill roads wind).
    chord = sum(rt.km_between((a["lon"], a["lat"]), (b["lon"], b["lat"])) for a, b in zip(route["via"], route["via"][1:]))
    length = rt.length_km(pts)
    assert 0.85 * chord <= length <= 2.0 * chord, (route["id"], chord, length)
    assert abs(BY_ID[route["id"]]["length_km"] - length) < 0.06


@pytest.mark.parametrize("route", ROUTES["routes"], ids=lambda r: r["id"])
def test_each_path_passes_near_every_waypoint(route):
    """Every waypoint is near the line (a trace that skipped one would be far from it). Up to 3.5 km: a waypoint at a
    modern town's point off the 1946 road (Kfar Yona, Ashkelon) is reached by a spur, which the tool cuts."""
    pts = np.asarray(BY_ID[route["id"]]["points"], float)
    for w in route["via"]:
        d = min(rt.km_between((w["lon"], w["lat"]), tuple(p)) for p in pts)
        assert d <= 3.5, (route["id"], w["name"], d)


def test_no_segment_jumps():
    """Consecutive points are at most 7 km apart (the longest real straight is 6.5 km, on the Tel Aviv-Haifa road across
    the Sharon plain); and the trace itself never bridged more than 800 m off its symbol (max_gap_m, measured by the
    tool on the kept line), so no invented chord hides inside a straight."""
    for r in PATHS:
        pts = [tuple(p) for p in r["points"]]
        worst = max(rt.km_between(a, b) for a, b in zip(pts, pts[1:]))
        assert worst < 7.0, (r["id"], worst)
        assert r["max_gap_m"] <= 800, (r["id"], r["max_gap_m"])


def test_each_trace_follows_its_symbol():
    """At least 85% of every route's traced pixels lie on its own map symbol (the rest: names over the road, towns)."""
    for r in PATHS:
        assert r["on_symbol"] >= 0.85, (r["id"], r["on_symbol"])


def test_display_windows():
    """Every route has a display window; the Burma Road shows from its completion on 14 June 1948 (C1), so it is not on
    the map at the 15 May 1948 start; the rest are on the 1946 map and show from 1946 at the latest."""
    for src in ROUTES["routes"]:
        assert BY_ID[src["id"]]["from"] == src["from"] and BY_ID[src["id"]].get("to") == src.get("to")
        if src["id"] != "burma_road":
            assert rt._as_date(src["from"]) <= rt._as_date("1946"), src["id"]
    assert BY_ID["burma_road"]["from"] == "1948-06-14"


@pytest.mark.parametrize("change, message", [
    ({"opened": True}, "opened"),
    ({"opened": 1950}, "opened"),
    ({"from": 1946}, "from must be a quoted"),
    ({"from": "1946-6-1"}, "from must be a quoted"),
    ({"from": "1890"}, "is before opened"),
    ({"to": "1900"}, "to must be after from"),
])
def test_validate_rejects_bad_dates(change, message):
    doc = yaml12.load((ROOT / "content" / "map" / "routes_1948.yaml").read_text(encoding="utf-8"))
    doc["routes"][0].update(change)
    with pytest.raises(ValueError, match=message):
        rt.validate_routes(doc, BOUNDS)


def test_validate_requires_from():
    doc = yaml12.load((ROOT / "content" / "map" / "routes_1948.yaml").read_text(encoding="utf-8"))
    del doc["routes"][0]["from"]
    with pytest.raises(ValueError, match="from is required"):
        rt.validate_routes(doc, BOUNDS)


def test_the_jaffa_jerusalem_railway_is_its_real_length():
    """Wikipedia: 86.6 km (Jaffa to Jerusalem). The trace starts at the Jaffa terminus and ends at the Jerusalem
    station, so it must be close (within 10%)."""
    assert 78.0 <= rt.length_km([tuple(p) for p in BY_ID["jaffa_jerusalem_railway"]["points"]]) <= 95.0


def test_mercator_round_trip():
    for lon, lat in [(34.2, 29.45), (35.9, 33.35), (34.78, 32.08)]:
        x, y = rt.lonlat_to_px(lon, lat)
        lo, la = rt.px_to_lonlat(x, y)
        assert abs(lo - lon) < 1e-9 and abs(la - lat) < 1e-9
    assert 15.0 < rt.metres_per_px(32.0) < 17.0          # zoom 13: about 16 m a pixel at 32 N


def test_simplify_keeps_ends_and_tolerance():
    line = [(float(x), 0.0 + (0.3 if x == 5 else 0.0)) for x in range(11)]
    assert rt.simplify(line, 1.0) == [(0.0, 0.0), (10.0, 0.0)]
    bent = [(0.0, 0.0), (5.0, 3.0), (10.0, 0.0)]
    assert rt.simplify(bent, 1.0) == bent


def test_smooth_keeps_the_ends():
    pts = [(0.0, 0.0), (1.0, 1.0), (2.0, 0.0), (3.0, 1.0), (4.0, 0.0)]
    out = rt.smooth(pts, 3)
    assert out[0] == pts[0] and out[-1] == pts[-1] and len(out) == len(pts)
    assert out[2] == pytest.approx((2.0, 2.0 / 3.0))


def test_remove_spurs_drops_an_out_and_back():
    line = [(x, 0) for x in range(30)]
    spur = [(15, y) for y in range(1, 20)] + [(15, y) for y in range(18, 0, -1)]
    chain = line[:16] + spur + line[15:]
    out = rt.remove_spurs(chain, 2.0)
    assert all(y == 0 for _, y in out) and out[0] == (0, 0) and out[-1] == (29, 0)
    assert rt.remove_spurs(line, 2.0) == line


def test_symbol_masks_on_the_map_colours():
    def px(*rgb):
        return np.array([[rgb]], np.uint8)
    road, orange, cream, ink = px(205, 45, 50), px(235, 155, 70), px(252, 232, 182), px(30, 28, 25)
    assert rt.symbol_mask(road, "road")[0, 0]
    assert rt.symbol_mask(px(146, 64, 42), "road")[0, 0]          # a road over the valleys' green tint
    assert not rt.symbol_mask(px(200, 120, 40), "road")[0, 0]     # darker orange tint
    assert not rt.symbol_mask(orange, "road")[0, 0]          # the hill tint is not a road
    assert not rt.symbol_mask(cream, "road")[0, 0]
    assert rt.symbol_mask(ink, "track")[0, 0]
    # rail: a 6 px dark stroke survives the opening, a 2 px one (a cart track, a road's casing) does not
    img = np.full((40, 60, 3), 250, np.uint8)
    img[10:16, :] = 25
    img[30:32, :] = 25
    m = rt.symbol_mask(img, "rail")
    assert m[12, 30] and not m[31, 30]


def test_a_trace_follows_a_drawn_road():
    pytest.importorskip("skimage")
    img = np.full((120, 200, 3), (250, 230, 180), np.uint8)
    rr = (60 + 30 * np.sin(np.linspace(0, 3, 200))).astype(int)
    for c, r in enumerate(rr):
        img[r - 2:r + 3, c] = (205, 45, 50)
    cost = rt.cost_raster(img, "road")
    path = rt.least_cost_path(cost, (rr[0], 0), (rr[-1], 199))
    assert max(abs(r - rr[c]) for r, c in path) <= 3


def test_trim_end_hooks_drops_a_hook_at_either_end():
    hook = [(0, 0), (-3, 0), (-3, 1), (0, 1)]
    line = [(x, 1) for x in range(1, 30)]
    out = rt.trim_end_hooks(hook + line, 4.0)
    assert out[0][0] >= 0 and out[-1] == (29, 1)
    assert rt.trim_end_hooks(line, 4.0) == line, "a line that only moves away from its ends is kept whole"
    back = rt.trim_end_hooks((hook + line)[::-1], 4.0)
    assert back[0] == (29, 1) and back[-1] == (0, 1), "the same hook at the end"


def test_accuracy_script_pure_parts():
    """tools/routes_accuracy.py (the docs' accuracy table): densifying keeps the ends and the step; the 1948 filter."""
    ra = pytest.importorskip("routes_accuracy")
    d = ra.densify([(34.5, 31.5), (34.6, 31.5)], 20)
    assert np.allclose(d[0], ra.xy(34.5, 31.5)) and np.allclose(d[-1], ra.xy(34.6, 31.5))
    assert np.all(np.linalg.norm(np.diff(d, axis=0), axis=1) <= 20.01)
    assert ra.valid_1948({"start_date": "1937-05"}) and ra.valid_1948({})
    assert not ra.valid_1948({"start_date": "1975"}) and not ra.valid_1948({"end_date": "1948-01"})


def sawtooth(points: list, deg: float = 15.0, short_m: float = 600.0) -> int:
    """Kinks that zigzag: consecutive turns over `deg` in opposite directions, with a segment under `short_m` between
    them (review B14's measure; a real S-bend has longer legs)."""
    k = math.cos(math.radians(31.8))
    p = [(lon * 111320 * k, lat * 110570) for lon, lat in points]
    heads = [math.atan2(b[1] - a[1], b[0] - a[0]) for a, b in zip(p, p[1:])]
    lens = [math.dist(a, b) for a, b in zip(p, p[1:])]
    turns = [(h2 - h1 + math.pi) % (2 * math.pi) - math.pi for h1, h2 in zip(heads, heads[1:])]
    lim = math.radians(deg)
    return sum(1 for i in range(len(turns) - 1)
               if abs(turns[i]) > lim and abs(turns[i + 1]) > lim and turns[i] * turns[i + 1] < 0 and lens[i + 1] < short_m)


def test_no_sawtooth_on_straight_lines():
    """Review B14: the trace's 100-300 m wobble drew kinks every few hundred metres (Hadera-Haifa had 20 zigzags in 92
    points, the main line south 54 in 185). Now the near-straight plain railways have at most 3, and no route more than
    15% of its points (the rest are real bends: the Sorek canyon, the hill roads, the Jericho descent)."""
    for r in PATHS:
        n = sawtooth(r["points"])
        assert n <= 0.15 * len(r["points"]), (r["id"], n, len(r["points"]))
        if r["id"] in ("hadera_haifa_railway", "kantara_main_line_south", "eastern_railway", "haifa_beirut_railway"):
            assert n <= 3, (r["id"], n)


def test_gaussian_smooth_flattens_a_wobble_and_keeps_a_bend():
    rng = np.random.default_rng(7)
    xs = np.arange(0, 6000, rt.RESAMPLE_M)
    wobble = [(x, 40.0 * math.sin(x / 120.0) + rng.normal(0, 5)) for x in xs]   # a 750 m wobble on a straight line
    out = np.asarray(rt.gaussian_smooth(wobble, rt.RESAMPLE_M))
    assert np.abs(out[10:-10, 1]).max() < 15.0                                   # 40 m swings down to under 15 m
    assert out[0] == pytest.approx(wobble[0]) and out[-1] == pytest.approx(wobble[-1])
    r = 400.0                                                                    # a tight bend, like the Sorek meanders
    arc = [(r * math.cos(a), r * math.sin(a)) for a in np.arange(0, math.pi, rt.RESAMPLE_M / r)]
    out = np.asarray(rt.gaussian_smooth(arc, rt.RESAMPLE_M))
    radii = np.hypot(out[5:-5, 0], out[5:-5, 1])
    assert radii.min() > 0.9 * r, radii.min()                                    # the bend keeps its shape
    assert rt.gaussian_smooth(arc, rt.RESAMPLE_M) == rt.gaussian_smooth(arc, rt.RESAMPLE_M)   # deterministic


@pytest.mark.parametrize("change, message", [
    ({"reviewed_by": "eyalgolan"}, "reviewed_on"),
    ({"reviewed_on": "2026-10-15"}, "reviewed_by"),
    ({"reviewed_by": "someone@example.org", "reviewed_on": "2026-10-15"}, "reviewed_by"),
    ({"reviewed_by": None, "reviewed_on": "2026-10-15"}, "reviewed_by"),
    ({"reviewed_by": "eyalgolan", "reviewed_on": "15 October 2026"}, "reviewed_on"),
])
def test_validate_rejects_a_bad_sign_off(change, message):
    """A person's sign-off beside the status (#375): both or neither, a GitHub handle, a day."""
    doc = yaml12.load((ROOT / "content" / "map" / "routes_1948.yaml").read_text(encoding="utf-8"))
    doc["routes"][0].update(change)
    with pytest.raises(ValueError, match=message):
        rt.validate_routes(doc, BOUNDS)


def test_validate_accepts_a_whole_sign_off():
    doc = yaml12.load((ROOT / "content" / "map" / "routes_1948.yaml").read_text(encoding="utf-8"))
    doc["routes"][0].update({"reviewed_by": "eyalgolan", "reviewed_on": "2026-10-15"})
    rt.validate_routes(doc, BOUNDS)
