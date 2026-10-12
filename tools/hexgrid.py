# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3"]
# ///
"""Heightmap -> hex grid (Red Blob Games axial, pointy-top) with elevation and biome per hex.

Usage: uv run tools/hexgrid.py   (reads unity/Assets/StreamingAssets/map/heightmap.* and content/map/precip_bio12.* +
precip_stations.json,
writes content/map/hexes.json, whose "attribution" list repeats the Copernicus DEM, CHELSA and IMS credits of its inputs)
Projection: equirectangular km, x east from lon0, y south from lat1 (so r grows southward).
Biome rules, first match wins (per hex, over the heightmap pixels / precipitation cells inside it):
  1 sea           >= 50% of pixels within 0.5 m of 0 m (Copernicus sea is exactly 0 m)
  2 lake          >= 40% of pixels are flat water: below -150 m and within 1.5 m of the hex's most common
                  sub -150 m level (Sea of Galilee ~-210 m, Dead Sea ~-430 m, evaporation ponds)
  3 desert/valley median < 0 m: "desert" south of the Dead Sea's north tip (lat < 31.77: the hyper-arid Dead Sea
                  basin and northern Arava), otherwise "valley" (Jordan rift, Beit She'an, Hula)
  4 mountains     median >= 700 m
  5 desert        mean annual precipitation < 200 mm (the standard arid boundary): the mean, over the clip cells
                  whose centres fall inside the hex and on land (the heightmap there is not sea, as in rule 1; CHELSA
                  is extrapolated over the sea), of CHELSA V2.1 bio12 (1981-2010, content/map/precip_bio12.*)
                  bias-corrected with IMS rain-gauge normals (1981-2010, content/map/precip_stations.json): IDW
                  (power 2) of log(station / CHELSA) applied as a multiplicative correction. A hex with no valid land
                  cell uses the nearest valid cell to its centre. Inputs are written by tools/precip_clip.py and
                  tools/ims_normals.py; this build reads only the committed files
  6 hills         median >= 200 m
  7 coastal_plain everything else within COASTAL_PLAIN_MAX_KM of the sea (see below)
  8 lowland       everything else: land of 0-200 m that is further inland (slopes above the Sea of Galilee, the
                  Jezreel and Hula valley margins)
The distance is `coast_km`: from the hex centre to the nearest shore pixel (a sea pixel with a non-sea neighbour)
that lies in a hex classed "sea" by rule 1. It is a pure function of the heightmap; a map with no sea gives infinity.
Current counts (1102 hexes): coastal_plain 79, desert 332, hills 147, lake 16, lowland 30, mountains 251, sea 216, valley 31
"""
import json
import math
from pathlib import Path

import numpy as np

import heightmap_codec
from geo import KM_PER_DEG_LAT, KM_PER_DEG_LON_EQ

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
OUT = ROOT / "content" / "map" / "hexes.json"
PRECIP_DIR = ROOT / "content" / "map"
PRECIP_NAME = "precip_bio12"
DEAD_SEA_NORTH_TIP_LAT = 31.77
COASTAL_PLAIN_MAX_KM = 25.0  # land of 0-200 m further than this from the shore is "lowland", not "coastal_plain"
ARID_MM = 200.0  # mean annual precipitation below which land is desert (arid)
PRECIP_NODATA = 65535
STATIONS_NAME = "precip_stations.json"
SEA_EPS_M = 0.5  # Copernicus sea is exactly 0 m
IDW_MIN_D2_KM2 = 1e-6  # a station on a cell centre gets (almost) all the weight there
HEX_SIZE_KM = 5.0  # circumradius (centre to corner)
SQRT3 = math.sqrt(3)
NEIGHBOURS = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)]  # axial directions


def km_per_deg_lon(meta: dict) -> float:
    return KM_PER_DEG_LON_EQ * math.cos(math.radians((meta["lat0"] + meta["lat1"]) / 2))


def lonlat_to_km(lon: float, lat: float, b: dict) -> tuple[float, float]:
    """Map projection used by every tool: x km east of lon0, y km south of lat1."""
    return (lon - b["lon0"]) * km_per_deg_lon(b), (b["lat1"] - lat) * KM_PER_DEG_LAT


def hex_center_km(q: int, r: int, size: float) -> tuple[float, float]:
    return size * (SQRT3 * q + SQRT3 / 2 * r), size * 1.5 * r


def point_to_hex(x: np.ndarray, y: np.ndarray, size: float) -> tuple[list[int], list[int]]:
    """Vectorised axial rounding (cube-coordinate round), Red Blob Games 'pixel to hex'."""
    qf = (SQRT3 / 3 * x - y / 3) / size
    rf = (2 / 3 * y) / size
    sf = -qf - rf
    q, r, s = np.round(qf), np.round(rf), np.round(sf)
    dq, dr, ds = np.abs(q - qf), np.abs(r - rf), np.abs(s - sf)
    fix_q = (dq > dr) & (dq > ds)
    fix_r = ~fix_q & (dr > ds)
    q = np.where(fix_q, -r - s, q)
    r = np.where(fix_r, -q - s, r)
    return q.astype(int).tolist(), r.astype(int).tolist()


def water_share(v: np.ndarray) -> float:
    low = np.round(v[v < -150])
    if low.size == 0:
        return 0.0
    vals, counts = np.unique(low, return_counts=True)
    level = vals[np.argmax(counts)]
    return float(np.mean((v < -150) & (np.abs(v - level) < 1.5)))


def settlement_hex(lon: float, lat: float, grid: dict) -> tuple[int, int]:
    """Placement rule: the containing hex if it is land, else the land neighbour with the nearest centre
    (shore towns such as Tiberias). Raises if neither exists, which the content tests turn into a failure."""
    size = grid["hex_size_km"]
    x, y = lonlat_to_km(lon, lat, grid["bounds"])
    (q,), (r,) = point_to_hex(np.array([x]), np.array([y]), size)
    land = {(h["q"], h["r"]) for h in grid["hexes"] if h["land"]}
    if (q, r) in land:
        return q, r
    options = [(q + dq, r + dr) for dq, dr in NEIGHBOURS if (q + dq, r + dr) in land]
    if not options:
        raise ValueError(f"no land hex at or next to ({lon}, {lat})")
    return min(options, key=lambda c: math.dist(hex_center_km(*c, size), (x, y)))


def classify(st: dict, lat: float) -> str:
    """Biome of one hex from its stats (sea_share, water_share, median, precip_mm, coast_km) and its latitude."""
    if st["sea_share"] >= 0.5:
        return "sea"
    if st["water_share"] >= 0.4:
        return "lake"
    if st["median"] < 0:
        return "desert" if lat < DEAD_SEA_NORTH_TIP_LAT else "valley"
    if st["median"] >= 700:
        return "mountains"
    if st["precip_mm"] < ARID_MM:
        return "desert"
    if st["median"] >= 200:
        return "hills"
    return "coastal_plain" if st["coast_km"] <= COASTAL_PLAIN_MAX_KM else "lowland"


def coast_points_km(sea: np.ndarray, hex_q: np.ndarray, hex_r: np.ndarray, sea_hexes: set,
                    width_km: float, length_km: float) -> np.ndarray:
    """Shore pixels, as an (n, 2) array of map km (x, y): sea pixels (2-D mask `sea`) with a non-sea 4-neighbour
    (the map edge counts as sea) whose hex, from the flat per-pixel `hex_q`, `hex_r`, is in `sea_hexes`.
    The hex filter drops the few 0 m pixels of inland no-data."""
    h, w = sea.shape
    p = np.pad(sea, 1, constant_values=True)
    shore = sea & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])
    rows, cols = np.nonzero(shore)
    flat = rows * w + cols
    keep = np.array([(int(hex_q[i]), int(hex_r[i])) in sea_hexes for i in flat], dtype=bool)
    rows, cols = rows[keep], cols[keep]
    return np.stack([cols / (w - 1) * width_km, rows / (h - 1) * length_km], axis=1)


def distance_to_coast_km(cx: float, cy: float, coast: np.ndarray) -> float:
    if len(coast) == 0:
        return math.inf
    return float(np.sqrt(np.min((coast[:, 0] - cx) ** 2 + (coast[:, 1] - cy) ** 2)))


def write_precip(mm: np.ndarray, lon0: float, lat1: float, cell_deg: float, out_dir: Path, extra: dict) -> dict:
    """Precipitation clip: <name>.u16 (little-endian uint16 mm/year, row 0 = north, PRECIP_NODATA = no data)
    plus <name>.json (grid georeference: lon0/lat1 = west/north edges of the first cell, cell_deg, size; and `extra`)."""
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    raw = np.where(np.isnan(mm), PRECIP_NODATA, np.clip(np.round(np.nan_to_num(mm)), 0, PRECIP_NODATA - 1)).astype("<u2")
    raw.tofile(out_dir / f"{PRECIP_NAME}.u16")
    meta = {**extra, "units": "mm/year", "width": int(mm.shape[1]), "height": int(mm.shape[0]),
            "lon0": float(lon0), "lat1": float(lat1), "cell_deg": float(cell_deg), "nodata": PRECIP_NODATA,
            "format": "little-endian uint16, row 0 = north"}
    (out_dir / f"{PRECIP_NAME}.json").write_text(json.dumps(meta, indent=2) + "\n")
    return meta


def load_precip(d: Path) -> dict:
    meta = json.loads((Path(d) / f"{PRECIP_NAME}.json").read_text())
    raw = np.fromfile(Path(d) / f"{PRECIP_NAME}.u16", dtype="<u2").reshape(meta["height"], meta["width"])
    return {"meta": meta, "mm": np.where(raw == meta["nodata"], np.nan, raw.astype("float64"))}


def load_stations(d: Path) -> list[dict]:
    return json.loads((Path(d) / STATIONS_NAME).read_text())["stations"]


def station_log_ratio(i: int, s: dict, pr: dict) -> float:
    """log(station mm / CHELSA mm at the station). Raises ValueError rather than letting a NaN or infinite ratio
    spread through the IDW to every cell."""
    ids = s.get("ids") or ([s["id"]] if "id" in s else None)
    who = f"station {ids} ({s.get('name', '?')})" if ids else f"station index {i} ({s.get('name', '?')})"
    chelsa = precip_at(s["lon"], s["lat"], pr)
    if not chelsa > 0:  # NaN or <= 0
        raise ValueError(f"{who}: CHELSA value at the station is {chelsa}; need a finite value > 0")
    with np.errstate(divide="ignore", invalid="ignore"):
        lr = float(np.log(s["mm"] / chelsa))
    if not math.isfinite(lr):
        raise ValueError(f"{who}: station/CHELSA ratio {s['mm']}/{chelsa} has log {lr}; not finite")
    return lr


def correct_precip(pr: dict, stations: list[dict]) -> dict:
    """Bias-correct the CHELSA field with station normals: at each station ratio = station mm / CHELSA mm (bilinear);
    log(ratio) is interpolated to every cell by inverse-distance weighting (power 2, km on the map projection) and
    the field is multiplied by exp(interpolated log ratio). No stations = unchanged (a copy)."""
    if not stations:
        return {"meta": pr["meta"], "mm": pr["mm"].copy()}
    lons, lats = precip_cell_centres(pr)
    mid = pr["meta"]["lat1"] - pr["meta"]["height"] * pr["meta"]["cell_deg"] / 2
    kx = KM_PER_DEG_LON_EQ * math.cos(math.radians(mid))
    sx = np.array([s["lon"] for s in stations]); sy = np.array([s["lat"] for s in stations])
    log_ratio = np.array([station_log_ratio(i, s, pr) for i, s in enumerate(stations)])
    num = np.zeros(lons.shape); den = np.zeros(lons.shape)
    for x, y, lr in zip(sx, sy, log_ratio):              # station by station: fixed order, modest memory
        w = 1.0 / np.maximum(((lons - x) * kx) ** 2 + ((lats - y) * KM_PER_DEG_LAT) ** 2, IDW_MIN_D2_KM2)
        num += w * lr; den += w
    return {"meta": pr["meta"], "mm": pr["mm"] * np.exp(num / den)}


def load_corrected_precip(d: Path) -> dict:
    return correct_precip(load_precip(d), load_stations(d))


def precip_cell_centres(pr: dict) -> tuple[np.ndarray, np.ndarray]:
    m = pr["meta"]
    rows, cols = np.mgrid[0:m["height"], 0:m["width"]]
    return m["lon0"] + (cols + 0.5) * m["cell_deg"], m["lat1"] - (rows + 0.5) * m["cell_deg"]


def precip_at(lon: float, lat: float, pr: dict) -> float:
    """Bilinear interpolation between cell centres (NaN cells are skipped and the rest renormalised)."""
    m = pr["meta"]
    fx = (lon - m["lon0"]) / m["cell_deg"] - 0.5
    fy = (m["lat1"] - lat) / m["cell_deg"] - 0.5
    x0, y0 = math.floor(fx), math.floor(fy)
    tx, ty = fx - x0, fy - y0
    acc = wsum = 0.0
    for dy, wy in ((0, 1 - ty), (1, ty)):
        for dx, wx in ((0, 1 - tx), (1, tx)):
            y = min(max(y0 + dy, 0), m["height"] - 1); x = min(max(x0 + dx, 0), m["width"] - 1)
            v = pr["mm"][y, x]
            if wx * wy > 0 and not np.isnan(v):
                acc += wx * wy * v; wsum += wx * wy
    return acc / wsum if wsum > 0 else float("nan")


def nearest_precip(lon: float, lat: float, pr: dict) -> float:
    lons, lats = precip_cell_centres(pr)
    ok = ~np.isnan(pr["mm"])
    d = (lons[ok] - lon) ** 2 + (lats[ok] - lat) ** 2
    return float(pr["mm"][ok][np.argmin(d)])


def is_sea(elev_m: np.ndarray) -> np.ndarray:
    """Copernicus sea is exactly 0 m: the sea test of biome rule 1."""
    return np.abs(elev_m) < SEA_EPS_M


def hex_precip_means(pr: dict, elev: np.ndarray, meta: dict, size: float) -> dict:
    """Mean precipitation of the valid clip cells whose centres fall inside each hex and on land, keyed (q, r).
    A cell is land when the nearest heightmap pixel to its centre (clamped to the map) is not sea: over the sea
    CHELSA is extrapolated, so sea cells would bias shore hexes."""
    lons, lats = precip_cell_centres(pr)
    h, w = elev.shape
    width_km = (meta["lon1"] - meta["lon0"]) * km_per_deg_lon(meta)
    length_km = (meta["lat1"] - meta["lat0"]) * KM_PER_DEG_LAT
    x = (lons - meta["lon0"]) * km_per_deg_lon(meta)
    y = (meta["lat1"] - lats) * KM_PER_DEG_LAT
    col = np.clip(np.round(x / width_km * (w - 1)), 0, w - 1).astype(int)
    row = np.clip(np.round(y / length_km * (h - 1)), 0, h - 1).astype(int)
    ok = ~np.isnan(pr["mm"]) & ~is_sea(elev[row, col])
    x, y = x[ok], y[ok]
    q, r = point_to_hex(x, y, size)
    sums, counts = {}, {}
    for k, v in zip(zip(q, r), pr["mm"][ok]):
        sums[k] = sums.get(k, 0.0) + float(v); counts[k] = counts.get(k, 0) + 1
    return {k: sums[k] / counts[k] for k in sums}


def load_elevation(map_dir: Path) -> tuple[np.ndarray, dict]:
    meta = json.loads((map_dir / "heightmap.json").read_text())
    raw = heightmap_codec.read(map_dir, meta)
    elev = meta["elev_min_m"] + raw.astype("float64") / 65535 * (meta["elev_max_m"] - meta["elev_min_m"])
    return elev, meta


def credit(doc: dict, name: str) -> str:
    """The input's `attribution` text; hexes.json repeats it because its biomes are derived from that input."""
    if not doc.get("attribution"):
        raise ValueError(f"{name}: no attribution; hexes.json must credit every input it is derived from")
    return doc["attribution"]


def build(map_dir: Path, size: float = HEX_SIZE_KM, precip_dir: Path = PRECIP_DIR) -> dict:
    elev, meta = load_elevation(map_dir)
    pr = load_corrected_precip(precip_dir)
    attribution = [credit(meta, "heightmap.json"), credit(pr["meta"], f"{PRECIP_NAME}.json"),
                   credit(json.loads((Path(precip_dir) / STATIONS_NAME).read_text()), STATIONS_NAME)]
    precip = hex_precip_means(pr, elev, meta, size)
    h, w = elev.shape
    width_km = (meta["lon1"] - meta["lon0"]) * km_per_deg_lon(meta)
    length_km = (meta["lat1"] - meta["lat0"]) * KM_PER_DEG_LAT
    ys, xs = np.mgrid[0:h, 0:w]
    q, r = point_to_hex(xs.ravel() / (w - 1) * width_km, ys.ravel() / (h - 1) * length_km, size)
    q, r = np.array(q), np.array(r)
    values = elev.ravel()
    order = np.lexsort((q, r))
    keys = np.stack([r[order], q[order]], axis=1)
    starts = np.flatnonzero(np.r_[True, np.any(keys[1:] != keys[:-1], axis=1)])
    cells = []
    for a, b in zip(starts, np.r_[starts[1:], len(order)]):
        hr, hq = (int(v) for v in keys[a])
        cx, cy = hex_center_km(hq, hr, size)
        if not (0 <= cx <= width_km and 0 <= cy <= length_km):
            continue  # partial hex on the map edge: centre outside the bounds
        v = values[order[a:b]]
        lon = meta["lon0"] + cx / km_per_deg_lon(meta)
        lat = meta["lat1"] - cy / KM_PER_DEG_LAT
        mm = precip.get((hq, hr))
        st = {"sea_share": float(np.mean(is_sea(v))), "water_share": water_share(v),
              "median": float(np.median(v)), "precip_mm": mm if mm is not None else nearest_precip(lon, lat, pr)}
        cells.append((hq, hr, cx, cy, lon, lat, st))
    sea_hexes = {(hq, hr) for hq, hr, _, _, _, _, st in cells if st["sea_share"] >= 0.5}
    coast = coast_points_km(is_sea(elev), q, r, sea_hexes, width_km, length_km)
    hexes = []
    for hq, hr, cx, cy, lon, lat, st in cells:
        st["coast_km"] = distance_to_coast_km(cx, cy, coast)
        biome = classify(st, lat)
        hexes.append({"q": hq, "r": hr, "lon": round(lon, 5), "lat": round(lat, 5), "elev_m": round(st["median"], 1),
                      "land": biome not in ("sea", "lake"), "biome": biome})
    bounds = {k: meta[k] for k in ("lon0", "lon1", "lat0", "lat1")}
    return {"attribution": attribution, "bounds": bounds, "hex_size_km": size, "layout": "pointy", "hexes": hexes}


def main() -> None:
    out = build(MAP_DIR)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(out, separators=(",", ":")))
    counts = {}
    for hx in out["hexes"]:
        counts[hx["biome"]] = counts.get(hx["biome"], 0) + 1
    print(f"wrote {OUT}: {len(out['hexes'])} hexes {dict(sorted(counts.items()))}")


if __name__ == "__main__":
    main()
