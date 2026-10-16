"""The route ribbons' bake (unity/Assets/Resources/Routes/RouteRibbons.bytes, issue #321) is for the shipped map and routes.

The game refuses a bake whose header does not match what it loaded (RouteRibbonFile.TryRead: the heightmap's resolution and
HeightsCheck, FNV-1a checks of heightmap.json and route_paths_1948.yaml, the terrain, RouteLayer's geometry constants and
the bake's version) and then derives the ribbons at boot, which is slower. CI runs no Unity, so this checks the same
header here, with the constants read from the C# source: a change to any of them without a re-bake
(RouteRibbonBake.Bake) fails CI, as the water's bake does. It also walks the meshes and their route index (route id, vertex
and index range) for the GDD Phase 4 corridors (#384). Unity's RouteTests.TheBakedRibbonsMatchTheRoutes compares the
whole meshes.

The header holds the ribbons' data and constants, not the code that drapes them. A fingerprint of that code (RouteLayer.Plan,
RouteLayer.Derive and what they call; comments and whitespace left out) covers it (issue #535, as #529 did for the biome
colours): an edit to it fails here until the ribbons are re-baked, RouteRibbonFile.Version is bumped and the fingerprint
is re-pinned. There is no numpy port: the ribbons drape on Unity's terrain sampling (GetInterpolatedHeight and Normal)."""
import codecs
import json
import re
import struct
from pathlib import Path

import numpy as np
import pytest
import yaml12

from test_terrain_water_bake import MAP, csharp, fingerprint, heights_check, method, unity_heights

ROOT = Path(__file__).resolve().parents[2]
BAKE = ROOT / "unity/Assets/Resources/Routes/RouteRibbons.bytes"
ROUTES = ROOT / "content/map/route_paths_1948.yaml"
SCRIPTS = ROOT / "unity/Assets/Scripts/Map"
MAGIC = 0x31524353   # "SCR1"
REBAKE = "run Statecraft.Game.EditorTools.RouteRibbonBake.Bake in Unity (docs/routes.md, Baked ribbons)"

# RouteRibbonFile.Floats and .Ints, in the header's order, with the file that defines each.
FLOATS = [("Routes/RouteLayer.cs", n) for n in ("RailWidthM", "RoadWidthM", "TrackWidthM", "StepM", "ClearanceM",
          "FallToleranceM", "RiseToleranceM", "MinWidthShare", "HalfWidthStepShare", "RailSpanShare", "RailClearanceM")]
FLOATS += [("MapBuilder.cs", "VerticalExaggeration")]
INTS = [("Routes/RouteLayer.cs", n) for n in ("Across", "NarrowSamples", "LiftSamples")]


def fnv1a(data: bytes) -> int:
    """RouteRibbonFile.Check: FNV-1a over the bytes, as a signed int32."""
    h = 2166136261
    for b in data:
        h = ((h ^ b) * 16777619) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


def text_check(path: Path) -> int:
    """Check over the file as Unity reads it: File.ReadAllText drops a UTF-8 BOM."""
    return fnv1a(path.read_bytes().removeprefix(codecs.BOM_UTF8))


def csharp_const(rel: str, name: str) -> str:
    m = re.search(rf"\b{name}\s*=\s*([0-9]+(?:\.[0-9]+)?)f?\b", (SCRIPTS / rel).read_text())
    assert m, f"{name} not found in {rel}"
    return m.group(1)


def bake_version() -> int:
    return int(csharp_const("Routes/RouteRibbonFile.cs", "Version"))


class Reader:
    def __init__(self, data: bytes):
        self.data, self.at = data, 0

    def take(self, fmt: str):
        size = struct.calcsize(fmt)
        assert self.at + size <= len(self.data), "truncated: " + REBAKE
        values = struct.unpack_from(fmt, self.data, self.at)
        self.at += size
        return values

    def int(self) -> int:
        return self.take("<i")[0]

    def text(self) -> str:
        n = self.int()
        return self.take(f"<{n}s")[0].decode("utf-8")


def read_header(r: Reader) -> dict:
    head = dict(zip(("magic", "version", "res", "heights", "map", "routes"), r.take("<6i")))
    head["terrain"] = r.take("<6f")
    head["floats"] = r.take(f"<{len(FLOATS)}f")
    head["ints"] = r.take(f"<{len(INTS)}i")
    head["meshes"] = r.int()
    return head


def bake_bytes() -> bytes:
    assert BAKE.exists(), f"{BAKE.relative_to(ROOT)} is missing: {REBAKE}"
    return BAKE.read_bytes()


def test_the_route_ribbon_bake_is_for_the_shipped_map_and_routes():
    head = read_header(Reader(bake_bytes()))
    heights = unity_heights()
    assert head["magic"] == MAGIC
    assert head["version"] == bake_version(), "made by another RouteRibbonFile.Version: " + REBAKE
    assert head["res"] == heights.shape[1]
    assert head["heights"] == heights_check(heights), "stale (heightmap): " + REBAKE
    assert head["map"] == text_check(MAP / "heightmap.json"), "stale (heightmap.json): " + REBAKE
    assert head["routes"] == text_check(ROUTES), "stale (route_paths_1948.yaml): " + REBAKE
    want = [np.float32(float(csharp_const(f, n))) for f, n in FLOATS]
    got = [np.float32(v) for v in head["floats"]]
    changed = [n for (_, n), a, b in zip(FLOATS, want, got) if a != b]
    changed += [n for (f, n), v in zip(INTS, head["ints"]) if int(csharp_const(f, n)) != v]
    assert not changed, f"stale (constants {', '.join(changed)}): " + REBAKE


def test_every_route_is_in_the_index_once_and_its_ranges_tile_its_mesh():
    r = Reader(bake_bytes())
    head = read_header(r)
    ids = []
    for _ in range(head["meshes"]):
        name = r.text()
        nv, ni, nr = r.take("<3i")
        vertex = index = 0
        for _ in range(nr):
            rid = r.text()
            first_v, count_v, first_i, count_i = r.take("<4i")
            assert (first_v, first_i) == (vertex, index), f"{name}: {rid} does not follow the route before it"
            assert count_i % 3 == 0, f"{name}: {rid}"
            vertex, index = first_v + count_v, first_i + count_i
            ids.append(rid)
        assert (vertex, index) == (nv, ni), f"{name}: the routes do not cover the mesh"
        r.take(f"<{8 * nv}f")
        indices = np.array(r.take(f"<{ni}i"), dtype=np.int64)
        assert ni == 0 or (indices.min() >= 0 and indices.max() < nv), name
    assert r.at == len(r.data), "longer than its meshes: " + REBAKE
    shipped = [route["id"] for route in yaml12.load(ROUTES.read_text(encoding="utf-8"))["routes"]]
    assert sorted(ids) == sorted(shipped), "stale (route ids): " + REBAKE


def test_the_check_is_fnv1a():
    assert fnv1a(b"") == struct.unpack("<i", struct.pack("<I", 2166136261))[0]
    assert fnv1a(b"a") == struct.unpack("<i", struct.pack("<I", 0xE40C292C))[0]   # the FNV-1a 32-bit test vector
    assert fnv1a(b"ab") != fnv1a(b"ba")


# The fingerprint of the ribbons' C# code (code_fingerprint), and the RouteRibbonFile.Version it was pinned at. Re-pin both
# together, only after re-baking (RouteRibbonBake.Bake) with Version bumped.
PINNED_VERSION, PINNED_CODE = 3, "a60ad9441761c59d2dab361e68be89fb710cc59a5edc31e0c051a6ff14367328"


def code_fingerprint() -> str:
    """The code between the inputs and the bake: RouteRibbonFile.Bake (the terrain and frame it builds), the plans and
    their routes' windows, RouteLayer's geometry, and the Old City's walls the roads are cut at (#319). Its constants are left out: the header holds them (FLOATS, INTS)."""
    layer = csharp("Scripts/Map/Routes/RouteLayer.cs")
    paths = csharp("Scripts/Map/Routes/RoutePaths.cs")
    landmarks = csharp("Scripts/Map/" + LANDMARKS)
    return fingerprint([
        method(csharp("Scripts/Map/Routes/RouteRibbonFile.cs"), "public static byte[] Bake("),
        method(csharp("Scripts/Map/MapBuilder.cs"), "public static Vector3 TerrainSize("),
        method(csharp("Scripts/Map/GeoFrame.cs"), "public Vector3 ToWorld("),
        method(paths, "public static DateTime Bound("),
        method(paths, "public static RouteWindow WindowOf("),
        method(layer, "public static float WidthOf("),
        method(layer, "public sealed class RibbonPlan"),
        method(layer, "public static List<RibbonPlan> Plan("),
        method(layer, "public static RibbonGeometry Derive("),
        method(layer, "public static List<Vector2> World("),
        method(layer, "public static List<Vector2> Drawn("),
        method(layer, "public static List<Vector2> TrimEndsInside("),
        method(layer, "static Vector2 Exit("),
        method(landmarks, "public const double OldCityLon"),
        method(landmarks, "public const float OldCityHalfM"),
        method(landmarks, "public static (Vector2 centre, float half) OldCityWalls("),
        method(layer, "public static (List<Vector2> points, List<float> along) Resample("),
        method(layer, "public static float GradeHeight("),
        method(layer, "public static float FlatHalfWidth("),
        method(layer, "static bool OutOfTolerance("),
        method(layer, "public static float Ground("),
        method(layer, "public static float DrapeHeight("),
        method(layer, "public static float[] RailHalfWidths("),
        method(layer, "public static RibbonGeometry Geometry("),
    ])


def test_the_ribbon_code_is_the_one_the_bake_was_made_with():
    assert (bake_version(), code_fingerprint()) == (PINNED_VERSION, PINNED_CODE), (
        f"the C# that makes the route ribbons changed (version {bake_version()}, fingerprint {code_fingerprint()}): "
        f"{BAKE.relative_to(ROOT)} is stale until you {REBAKE} after bumping RouteRibbonFile.Version, then re-pin PINNED_VERSION and PINNED_CODE in "
        "tools/tests/test_route_ribbon_bake.py")


# Jerusalem's symbolic Old City (SettlementKits.OldCityWalls): its walled square is 4 km across, so the real 1948 roads by
# New Gate and the Jaffa-Jerusalem railway's station, all outside the real walls, fall inside it; the map cuts each road
# and the railway at the walls (RouteLayer.Drawn, #319; the railway on the owner's ruling, "Cut it at the walls").
LANDMARKS = "Kits/SettlementKits.Landmarks.cs"
OLD_CITY_ROADS = ("jaffa_jerusalem_road", "jerusalem_hebron_beersheba_road", "jerusalem_nablus_nazareth_road",
                  "jerusalem_jericho_road")
OLD_CITY_RAIL = ("jaffa_jerusalem_railway",)


def ribbon_vertices_by_route(look: str) -> dict[str, np.ndarray]:
    """Every ribbon vertex (world x, z) of one look in the bake ("Roads": the roads and tracks; "Rail"), by route id."""
    r = Reader(bake_bytes())
    head = read_header(r)
    out: dict[str, list] = {}
    for _ in range(head["meshes"]):
        name = r.text()
        nv, ni, nr = r.take("<3i")
        spans = []
        for _ in range(nr):
            rid = r.text()
            first_v, count_v, _, _ = r.take("<4i")
            spans.append((rid, first_v, count_v))
        xyz = np.array(r.take(f"<{3 * nv}f"), dtype=np.float64).reshape(-1, 3) if nv else np.zeros((0, 3))
        r.take(f"<{5 * nv}f")
        r.take(f"<{ni}i")
        if name.startswith(f"Route{look} "):
            for rid, first, count in spans:
                out.setdefault(rid, []).append(xyz[first:first + count][:, [0, 2]])
    return {k: np.concatenate(v) for k, v in out.items()}


def old_city_square() -> tuple[np.ndarray, float]:
    """The walls' centre (world x, z) and half side, from the C# constants and the bake's terrain size (GeoFrame.ToWorld)."""
    head = read_header(Reader(bake_bytes()))
    size_x, _, size_z = head["terrain"][:3]
    meta = json.loads((MAP / "heightmap.json").read_text())
    lon, lat = (float(csharp_const(LANDMARKS, n)) for n in ("OldCityLon", "OldCityLat"))
    centre = np.array([(lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * size_x,
                       (lat - meta["lat0"]) / (meta["lat1"] - meta["lat0"]) * size_z])
    return centre, float(csharp_const(LANDMARKS, "OldCityHalfM"))


@pytest.mark.parametrize("look, width, reaching", [("Roads", "RoadWidthM", OLD_CITY_ROADS), ("Rail", "RailWidthM", OLD_CITY_RAIL)])
def test_no_road_or_railway_runs_inside_the_old_city_walls(look, width, reaching):
    """Each road and the railway end at the walls: no ribbon vertex lies inside the square by more than half the ribbon's
    width (the end section is square to the line, so a line meeting the wall at a slant reaches that far in under the
    wall). Not vacuous: the square is where the real Old City is, and the four roads that start there and the railway
    that ends there each reach to within 300 m of it."""
    centre, half = old_city_square()
    assert 1000 < half < 5000, half
    depth_allowed = float(csharp_const("Routes/RouteLayer.cs", width)) / 2
    ribbons = ribbon_vertices_by_route(look)
    assert ribbons, f"no {look} ribbons in the bake"
    inside, near = {}, {}
    for rid, xz in ribbons.items():
        d = np.abs(xz - centre).max(axis=1)   # Chebyshev distance: inside the square where it is under half
        if (deep := int((d < half - depth_allowed).sum())):
            inside[rid] = deep
        near[rid] = int(((d >= half - depth_allowed) & (d < half + 300)).sum())
    assert not inside, f"{look} vertices inside the Old City's walls: {inside}"
    missing = [rid for rid in reaching if near.get(rid, 0) == 0]
    assert not missing, f"these should reach the Old City's walls: {missing}"
