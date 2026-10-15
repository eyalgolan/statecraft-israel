# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pyyaml==6.0.3"]
# ///
"""True sub-district lines -> the per-hex region layer the map draws (MapBorders, docs/visual-v1.md "Region borders").

Usage: uv run tools/borders_bake.py   (reads content/map/subdistricts_1948.geojson, content/map/hexes.json and the
heightmap; writes content/map/subdistrict_hexes_1948.yaml)

The GeoJSON keeps the true, digitised lines (tools/borders_digitise.py). The game shows them snapped to hex edges, as the
promo does, so each hex takes one region by its centre (deterministic: float64 arithmetic in a fixed order, features in
file order; libm only where stated below):
  1 a sea or lake hex (hexes.json land: false) is in no region (0): no band is drawn along the coast;
  2 a land hex whose centre lies inside a sub-district polygon takes that sub-district (the first in file order if a
    centre sat exactly on a shared edge);
  3 a land hex whose centre lies in water (a shore hex: the DEM at its centre is sea, 0 m, or below the Dead Sea's 1948
    level) takes the region of the nearest land to its centre (the nearest DEM land pixel within one hex size, 5 km): the
    sub-district containing that point, or within 0.5 km of it (the lines are simplified at about 120 m), else outside.
    So a shore hex follows its own coast: the coast is not a boundary, and a hex off the Sinai coast at Taba stays
    outside even though the Beersheba Sub-District is near;
  4 any other land hex is outside Mandatory Palestine (Lebanon, Syria, Transjordan, Sinai): the "outside" region, style
    none, so the sub-districts' bands run along the frontier but nothing is drawn beyond it;
  5 override (controller ruling, owner-delegated, 2026-10-11): the hex that holds a Gazette-listed place (GAZETTE_PLACES,
    GAZETTE_SETTLEMENTS; placed as the game places settlements, hexgrid.settlement_hex) always takes that place's
    sub-district, never outside, so a frontier town the snap would clip (Rafah, Metulla) stays in its sub-district. When
    two listed places share a hex, the region of rule 2-4 stands if it is one of theirs; otherwise the bake fails.
The sim never reads the output (display only: the content hash and the goldens do not move). Determinism: float64
arithmetic in a fixed order; libm (cos for the km-per-degree scale, hypot for distances) only in the shore rule, whose
result is an integer id and whose ties would need two regions at the same distance to the last bit; the committed output
is rechecked by the tools tests on macOS and on Linux CI.
Display window (owner ruling 2026-10-11): the layer shows from the game's start (15 May 1948) until the 1949 armistice
lines replace it, each front on its agreement's date once that data exists, labelled "Mandate sub-districts, 1947"
(LABEL). `fronts` gives each front's end (`until`): the agreement's date where content/map/armistice_lines_1949.geojson
has its line, null where it has none. The layer as a whole ends (`shown_until`) only when every front has a date and
the armistice lines enclose each side's area (the GeoJSON's `partition.closed`): the last date; until then (the Egypt line
stops at Gaza, the Jordan line leaves out Jerusalem) it stays as the base under the armistice lines.
"""
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import heightmap_codec  # noqa: E402
import hexgrid  # noqa: E402
from geo import KM_PER_DEG_LAT, KM_PER_DEG_LON_EQ  # noqa: E402
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "content" / "map"
SOURCE = MAP / "subdistricts_1948.geojson"
ARMISTICE = MAP / "armistice_lines_1949.geojson"
OUT = MAP / "subdistrict_hexes_1948.yaml"
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
DEAD_SEA_1948_M = -392.0
OUTSIDE = "outside"
LABEL = "Mandate sub-districts, 1947"
# Places as listed in the 1947 Schedule (Palestine Gazette No. 1552, Supplement No. 2; pages as printed, 125-133; read
# 2026-10-11, the page images where the text layer was unclear). Coordinates: settlements_1948.yaml for its rows; the
# Wikipedia API (prop=coordinates, 2026-10-11) for the rest. They drive rule 5 and the tests (tools/tests/test_borders.py).
GAZETTE_SETTLEMENTS = {   # settlements_1948.yaml id: (sub-district, Gazette page)
    "jaffa": ("jaffa", "p. 132"), "petah_tikva": ("jaffa", "p. 132"), "haifa": ("haifa", "p. 128"),
    "hadera": ("haifa", "p. 128"), "zikhron_yaakov": ("haifa", "p. 128"), "netanya": ("tulkarm", "p. 130"),
    "lod": ("ramle", "p. 132"), "rishon_lezion": ("ramle", "p. 133"), "rehovot": ("ramle", "p. 133"),
    "rosh_pinna": ("safad", "p. 127"), "degania_alef": ("tiberias", "p. 127"), "nahalal": ("nazareth", "p. 126"),
    "ein_harod": ("beisan", "p. 126"),
}
GAZETTE_PLACES = {   # name: (sub-district, lon, lat, Gazette page)
    "Kefar Sava": ("jaffa", 34.90833, 32.17139, "p. 132"), "Kafr Saba": ("tulkarm", 34.93722, 32.18111, "p. 130"),
    "Herzliya": ("jaffa", 34.84583, 32.16528, "p. 132"), "Ra'anana": ("jaffa", 34.86667, 32.18333, "p. 132"),
    "Holon": ("jaffa", 34.76667, 32.01667, "p. 132"), "Bat Yam": ("jaffa", 34.75000, 32.01667, "p. 132"),
    "Kafr Qasim": ("tulkarm", 34.97510, 32.11510, "p. 130"), "Jaljuliya": ("tulkarm", 34.95180, 32.15353, "p. 130"),
    "Qalqiliya": ("tulkarm", 34.96861, 32.19028, "p. 130"), "Yibna": ("ramle", 34.74639, 31.86611, "p. 133"),
    "Latrun": ("ramle", 34.98139, 31.83194, "p. 132"), "Gedera": ("ramle", 34.77722, 31.81194, "p. 132"),
    "Faluja": ("gaza", 34.74778, 31.62472, "p. 133"), "Isdud": ("gaza", 34.66167, 31.75361, "p. 133"),
    "Negba": ("gaza", 34.68306, 31.66083, "p. 133"), "Be'er Tuvya": ("gaza", 34.72389, 31.73611, "p. 133"),
    "Qastina": ("gaza", 34.76222, 31.73917, "p. 133"), "Rafah": ("gaza", 34.25861, 31.27250, "p. 133"),
    "Ruhama": ("beersheba", 34.70556, 31.49667, "p. 133"),
    "Beit Jibrin": ("hebron", 34.89833, 31.60528, "p. 130"), "Umm el Fahm": ("jenin", 35.15361, 32.51944, "p. 129"),
    "Afula": ("nazareth", 35.28806, 32.60639, "p. 126"), "Kafr Kanna": ("nazareth", 35.35000, 32.75000, "p. 126"),
    "Nahariya": ("acre", 35.09889, 33.00583, "p. 125"), "Kafr Bir'im": ("safad", 35.41417, 33.04361, "p. 127"),
    "Metulla": ("safad", 35.57444, 33.27889, "p. 127"), "Ein Gev": ("tiberias", 35.63972, 32.78278, "p. 127"),
    "Bir Zeit": ("ramallah", 35.19556, 31.97222, "p. 131"), "Bethlehem": ("jerusalem", 35.20639, 31.70444, "p. 131"),
}
SHOWN_FROM = "1948-05-15"
NEAR_LAND_KM = 0.5
HEADER = ("# Generated by tools/borders_bake.py from subdistricts_1948.geojson (the true lines) and hexes.json. Do not edit by\n"
          "# hand: rerun the tool. Display only (MapBorders): the sim never reads this file. Each land hex takes the\n"
          "# sub-district containing its centre (a shore hex the side of its nearest land), the hex of a Gazette-listed place\n"
          "# that place's sub-district; land outside them all is 'outside'.\n")


def rings(geometry: dict) -> list[list[np.ndarray]]:
    """GeoJSON Polygon / MultiPolygon -> [[exterior, hole, ...], ...] as (n, 2) lon/lat arrays."""
    if geometry["type"] == "Polygon":
        polys = [geometry["coordinates"]]
    elif geometry["type"] == "MultiPolygon":
        polys = geometry["coordinates"]
    else:
        raise ValueError(f"unsupported geometry {geometry['type']}")
    return [[np.asarray(r, dtype=np.float64) for r in poly] for poly in polys]


def ring_contains(ring: np.ndarray, x: float, y: float) -> bool:
    """Even-odd ray cast (horizontal ray to +x); a point exactly on a vertex row is counted by the half-open rule."""
    x0, y0 = ring[:-1, 0], ring[:-1, 1]
    x1, y1 = ring[1:, 0], ring[1:, 1]
    crosses = (y0 > y) != (y1 > y)
    with np.errstate(divide="ignore", invalid="ignore"):
        xi = x0 + (y - y0) * (x1 - x0) / (y1 - y0)
    return bool(np.count_nonzero(crosses & (x < xi)) % 2)


def contains(polys: list[list[np.ndarray]], x: float, y: float) -> bool:
    return any(ring_contains(p[0], x, y) and not any(ring_contains(h, x, y) for h in p[1:]) for p in polys)


def distance_km(polys: list[list[np.ndarray]], lon: float, lat: float, km_lon: float) -> float:
    """Distance from a point to the nearest polygon edge, km on the tools' equirectangular projection."""
    best = np.inf
    for p in polys:
        for r in p:
            ax, ay = (r[:-1, 0] - lon) * km_lon, (r[:-1, 1] - lat) * KM_PER_DEG_LAT
            bx, by = (r[1:, 0] - lon) * km_lon, (r[1:, 1] - lat) * KM_PER_DEG_LAT
            dx, dy = bx - ax, by - ay
            ll = dx * dx + dy * dy
            t = np.clip(np.where(ll > 0, -(ax * dx + ay * dy) / np.where(ll > 0, ll, 1), 0), 0, 1)
            best = min(best, float(np.min(np.hypot(ax + t * dx, ay + t * dy))))
    return best


def water_mask(elev: np.ndarray, meta: dict) -> np.ndarray:
    """Water as tools/borders_digitise.py takes it: the sea (within 0.5 m of 0 m) and the 1948 Dead Sea (at or below its
    level, north of nothing past 31.9 N), closed over the modern evaporation-pond dikes and with the land it encloses
    filled, worked inside the Dead Sea's bounding box (numpy and a plain flood fill: no scipy in the bake)."""
    water = np.abs(elev) < 0.5
    h, w = elev.shape
    lat = meta["lat1"] - np.arange(h) * (meta["lat1"] - meta["lat0"]) / (h - 1)
    lon = meta["lon0"] + np.arange(w) * (meta["lon1"] - meta["lon0"]) / (w - 1)
    rows = np.flatnonzero((lat >= 30.85) & (lat < 31.9)); cols = np.flatnonzero((lon >= 35.25) & (lon <= 35.7))
    box = (elev[np.ix_(rows, cols)] <= DEAD_SEA_1948_M)
    box = close8(box, 4)
    # fill the land the Dead Sea encloses: what the box's border cannot reach through land is water
    seen = np.zeros_like(box); stack = [(i, j) for i in range(box.shape[0]) for j in (0, box.shape[1] - 1)] + \
        [(i, j) for j in range(box.shape[1]) for i in (0, box.shape[0] - 1)]
    while stack:
        i, j = stack.pop()
        if seen[i, j] or box[i, j]:
            continue
        seen[i, j] = True
        for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            a, b = i + di, j + dj
            if 0 <= a < box.shape[0] and 0 <= b < box.shape[1] and not seen[a, b] and not box[a, b]:
                stack.append((a, b))
    water[np.ix_(rows, cols)] |= ~seen
    return water


def pixel(lon: float, lat: float, meta: dict) -> tuple[int, int]:
    col = int(round((lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * (meta["width"] - 1)))
    row = int(round((meta["lat1"] - lat) / (meta["lat1"] - meta["lat0"]) * (meta["height"] - 1)))
    return min(max(row, 0), meta["height"] - 1), min(max(col, 0), meta["width"] - 1)


def water_at(lon: float, lat: float, water: np.ndarray, meta: dict) -> bool:
    """The water mask at a point (nearest DEM pixel)."""
    return bool(water[pixel(lon, lat, meta)])


def close8(m: np.ndarray, n: int) -> np.ndarray:
    """Binary closing by n steps of the 8-neighbourhood (dilate n times, then erode n times), numpy only; the border is
    padded with the edge values."""
    def step(a: np.ndarray, grow: bool) -> np.ndarray:
        p = np.pad(a, 1, mode="edge")
        views = [p[1 + dy:p.shape[0] - 1 + dy, 1 + dx:p.shape[1] - 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1)]
        return np.logical_or.reduce(views) if grow else np.logical_and.reduce(views)
    for _ in range(n):
        m = step(m, True)
    for _ in range(n):
        m = step(m, False)
    return m


def nearest_land(lon: float, lat: float, water: np.ndarray, meta: dict, km_lon: float, reach_km: float) -> tuple[float, float] | None:
    """The nearest DEM pixel to a point that is not water (water_mask), within reach_km, as lon/lat (None: none)."""
    w, h = meta["width"], meta["height"]
    dlon, dlat = (meta["lon1"] - meta["lon0"]) / (w - 1), (meta["lat1"] - meta["lat0"]) / (h - 1)
    c0 = int(round((lon - meta["lon0"]) / dlon)); r0 = int(round((meta["lat1"] - lat) / dlat))
    rc, rr = int(reach_km / (dlon * km_lon)) + 1, int(reach_km / (dlat * KM_PER_DEG_LAT)) + 1
    rows = np.arange(max(r0 - rr, 0), min(r0 + rr, h - 1) + 1); cols = np.arange(max(c0 - rc, 0), min(c0 + rc, w - 1) + 1)
    land = ~water[np.ix_(rows, cols)]
    if not land.any():
        return None
    plon = meta["lon0"] + cols * dlon; plat = meta["lat1"] - rows * dlat
    d = np.hypot((plon[None, :] - lon) * km_lon, (plat[:, None] - lat) * KM_PER_DEG_LAT)
    d = np.where(land, d, np.inf)
    k = int(np.argmin(d))   # first minimum in row-major order: deterministic
    if d.flat[k] > reach_km:
        return None
    i, j = divmod(k, d.shape[1])
    return float(plon[j]), float(plat[i])


def load_elevation() -> tuple[np.ndarray, dict]:
    meta = json.loads((MAP_DIR / "heightmap.json").read_text())
    raw = heightmap_codec.read(MAP_DIR, meta)
    return meta["elev_min_m"] + raw.astype("float64") / 65535 * (meta["elev_max_m"] - meta["elev_min_m"]), meta


def bake(source: dict, hexes: dict, water: np.ndarray, meta: dict, places: list) -> tuple[list[dict], dict]:
    """(regions, {(q, r): region id}). Region ids: the sub-districts 1..n in file order, then 'outside'."""
    feats = source["features"]
    regions = [{"id": i, "key": f["properties"]["id"], "name": f["properties"]["name"], "district": f["properties"]["district"],
                "style": "primary"} for i, f in enumerate(feats, start=1)]
    outside_id = len(feats) + 1
    regions.append({"id": outside_id, "key": OUTSIDE, "name": "Outside Mandatory Palestine", "district": "", "style": "none"})
    shapes = [rings(f["geometry"]) for f in feats]
    b = hexes["bounds"]
    km_lon = KM_PER_DEG_LON_EQ * np.cos(np.radians((b["lat0"] + b["lat1"]) / 2))
    reach = hexes["hex_size_km"]
    out = {}
    for h in hexes["hexes"]:
        if not h["land"]:
            continue
        lon, lat = h["lon"], h["lat"]
        rid = next((i for i, s in enumerate(shapes, start=1) if contains(s, lon, lat)), 0)
        if rid == 0 and water_at(lon, lat, water, meta):
            land = nearest_land(lon, lat, water, meta, km_lon, reach)
            if land is not None:
                rid = next((i for i, s in enumerate(shapes, start=1) if contains(s, *land)), 0)
                if rid == 0:
                    d = [distance_km(s, land[0], land[1], km_lon) for s in shapes]
                    k = int(np.argmin(d))
                    rid = k + 1 if d[k] <= NEAR_LAND_KM else 0
        out[(h["q"], h["r"])] = rid or outside_id
    apply_places(out, places, hexes, {r["key"]: r["id"] for r in regions})
    return regions, out


def listed_places(settlements: list[dict]) -> list[tuple[str, str, float, float]]:
    """(name, sub-district, lon, lat) of every Gazette-listed place, settlements first, each list in its own order."""
    rows = {s["id"]: s for s in settlements}
    return ([(sid, sub, rows[sid]["lon"], rows[sid]["lat"]) for sid, (sub, _page) in GAZETTE_SETTLEMENTS.items()]
            + [(name, sub, lon, lat) for name, (sub, lon, lat, _page) in GAZETTE_PLACES.items()])


def place_hexes(places: list, hexes: dict) -> dict:
    """{(q, r): {sub-district: [place names]}} with each place on the hex the game puts it (hexgrid.settlement_hex)."""
    claims: dict = {}
    for name, sub, lon, lat in places:
        claims.setdefault(hexgrid.settlement_hex(lon, lat, hexes), {}).setdefault(sub, []).append(name)
    return claims


def apply_places(cells: dict, places: list, hexes: dict, ids: dict) -> None:
    """Rule 5, in place: a listed place's hex takes its sub-district."""
    for qr, subs in sorted(place_hexes(places, hexes).items()):
        if len(subs) > 1:
            raise ValueError(f"hex {qr}: listed places of different sub-districts {subs}")
        cells[qr] = ids[next(iter(subs))]


def front_ends(armistice: dict | None) -> list[tuple[str, str | None]]:
    """(front, the date that front switches: its armistice features are drawn over this layer from then (MapRegions), or
    None: no line data)."""
    if armistice is None:
        return []
    return [(f["key"], f["shown_from"] if f.get("data") else None) for f in armistice["statecraft"]["fronts"]]


def render(source: dict, regions: list[dict], cells: dict, armistice: dict | None = None) -> str:
    meta = source["statecraft"]
    ends = front_ends(armistice)
    closed = bool(armistice and armistice["statecraft"].get("partition", {}).get("closed"))
    until = max(d for _, d in ends) if ends and all(d for _, d in ends) and closed else None
    lines = [HEADER,
             "layer: subdistricts_1948\n",
             f"title: {json.dumps(meta['title'])}\n",
             f"line: {json.dumps('The true lines are the Survey of Palestine map of the 1947 Administrative Divisions Schedule (content/map/subdistricts_1948.geojson, status ' + meta['status'] + '); shown here to the nearest hex edge, up to half a hex (about 4 km) from the true line.')}\n",
             f"label: {json.dumps(LABEL)}   # shown on screen while the layer is drawn (owner ruling 2026-10-11)\n",
             f"shown_from: {SHOWN_FROM}   # the game's start\n",
             f"shown_until: {until or 'null'}   # the last front's end, once the armistice lines enclose each side's area; null until then (the base under them)\n",
             "fronts:   # until: the date that front switches, its 1949 armistice features drawn over this layer from then (null: no line data)\n"]
    lines += [f"  - {{ key: {k}, until: {d or 'null'} }}\n" for k, d in ends]
    lines += ["regions:\n"]
    for r in regions:
        lines.append(f"  - {{ id: {r['id']}, key: {r['key']}, name: {json.dumps(r['name'])}, district: {json.dumps(r['district'])}, style: {r['style']} }}\n")
    lines.append("hexes:   # [q, r, region id], ordered by r then q\n")
    for (q, r), rid in sorted(cells.items(), key=lambda kv: (kv[0][1], kv[0][0])):
        lines.append(f"  - [{q}, {r}, {rid}]\n")
    return "".join(lines)


def build() -> str:
    source = json.loads(SOURCE.read_text(encoding="utf-8"))
    hexes = json.loads((MAP / "hexes.json").read_text())
    elev, meta = load_elevation()
    settlements = yaml12.load((MAP / "settlements_1948.yaml").read_text(encoding="utf-8"))["settlements"]
    regions, cells = bake(source, hexes, water_mask(elev, meta), meta, listed_places(settlements))
    armistice = json.loads(ARMISTICE.read_text(encoding="utf-8")) if ARMISTICE.exists() else None
    return render(source, regions, cells, armistice)


def main() -> None:
    text = build()
    OUT.write_text(text, encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({text.count(chr(10) + '  - [')} hexes)")


if __name__ == "__main__":
    main()
