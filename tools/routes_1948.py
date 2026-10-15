# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0", "pyyaml==6.0.3", "scikit-image==0.26.0"]
# ///
"""The railways and main roads of May 1948 (issue #35): traced on the Survey of Palestine 1:250,000 map of 1946.

Usage: uv run tools/routes_1948.py [--fetch] [--only ID ...] [--preview DIR]
Reads content/map/routes_1948.yaml (hand-written: each route's kind, dates, sources and the named waypoints it passes,
in order) and writes content/map/route_paths_1948.yaml (generated: each route as a lon/lat polyline). Between two
waypoints the line is the least-cost path over the scanned map, where the cost is low on the route's own map symbol:
  rail   the black railway line (a thick dark stroke; the map's white dashes inside it are closed first);
  road   the red main road;
  track  any dark line (cart tracks, footpaths, the thin narrow-gauge railway symbol): the 1948 Burma Road, which the
         1946 map cannot show as a road, and the 1,050 mm lines (a route's `trace` names the symbol when it is not its
         kind's).
Each waypoint first snaps to the nearest pixel of its symbol within SNAP_M. The traced pixel chain is simplified
(a SMOOTH_PX moving average, then Douglas-Peucker at SIMPLIFY_M) and written with 5 decimals (about 1 m).

The map: Survey of Palestine 1:250,000 (1946), public domain (Crown copyright, published 1946, expired), scanned and
georeferenced by Palestine Open Maps, read as web-mercator tiles at zoom TILE_ZOOM (about 16 m a pixel). --fetch
downloads the tiles each route needs into tools/.cache/pal250k-1946/ (about 20 MB, not committed; one request at a
time).
Without --fetch a missing tile is an error. The sim never reads either routes file (map-only, like
settlements_1948.yaml's coordinates), so neither moves the content hash.

--preview DIR writes one PNG per route: the map under the window with the traced line, for checking by eye.
"""
from __future__ import annotations

import argparse
import math
import re
from datetime import date
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))  # under python -I the script's folder is not on sys.path
import review_record  # noqa: E402
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
ROUTES = ROOT / "content" / "map" / "routes_1948.yaml"
PATHS = ROOT / "content" / "map" / "route_paths_1948.yaml"
HEXES = ROOT / "content" / "map" / "hexes.json"

TILE_URL = "https://palopenmaps.org/tiles/pal250k-1946/{z}/{x}/{y}.jpg"
TILE_ZOOM = 13
TILE_CACHE = Path(os.environ.get("STATECRAFT_TILE_CACHE", ROOT / "tools" / ".cache" / "pal250k-1946"))
USER_AGENT = "statecraft-israel-routes/1.0 (+https://github.com/eyalgolan/statecraft-israel)"

KINDS = ("rail", "road", "track")
SNAP_M = 1500.0           # a waypoint (a town's or a station's coordinates) snaps to its symbol this close
WINDOW_MARGIN_M = 3000.0  # the search window around two waypoints: their box grown by this (or 35% of their distance)
SPUR_M = 130.0            # a trace that comes back this close to where it was cuts the loop (a waypoint off the line)
SMOOTH_PX = 15            # moving average over the traced pixels (ends kept): the 16 m pixel staircase and the trace
                          # hopping between the two edges of a thick symbol drew a 100-200 m wobble at close zoom
SMOOTH_SIGMA_M = 250.0    # Gaussian along a straight stretch (gaussian_smooth): removes the 100-300 m wobble (review B14)
BEND_SIGMA_M = 60.0       # ... and along a real bend, so the Sorek canyon's meanders keep their shape
BEND_WINDOW_M = 320.0     # a bend is a net heading change, over +- this, of BEND_FROM_DEG (straight) to BEND_TO_DEG (bend)
BEND_FROM_DEG, BEND_TO_DEG = 30.0, 90.0
RESAMPLE_M = 40.0         # spacing the chain is resampled to before the Gaussian
SIMPLIFY_M = 35.0         # Douglas-Peucker tolerance of the written line: about two map pixels, a tenth of a road's width
OFF_SYMBOL_COST = 40.0    # cost of a pixel off the symbol (on it: 1), so a gap in the scan of up to ~40 px is bridged
THIN_RAIL_COST = 4.0      # a rail trace's cost on a thin dark line (cost_raster)
STATUSES = ("draft", "fact-checked")


# --- web mercator ------------------------------------------------------------------------------------------------
def lonlat_to_px(lon: float, lat: float, z: int = TILE_ZOOM) -> tuple[float, float]:
    """Global pixel coordinates (x east, y south) of a point on the zoom-z tile pyramid (256 px tiles)."""
    n = 256 * 2 ** z
    x = (lon + 180.0) / 360.0 * n
    y = (1.0 - math.asinh(math.tan(math.radians(lat))) / math.pi) / 2.0 * n
    return x, y


def px_to_lonlat(x: float, y: float, z: int = TILE_ZOOM) -> tuple[float, float]:
    n = 256 * 2 ** z
    lon = x / n * 360.0 - 180.0
    lat = math.degrees(math.atan(math.sinh(math.pi * (1.0 - 2.0 * y / n))))
    return lon, lat


def metres_per_px(lat: float, z: int = TILE_ZOOM) -> float:
    return 40075016.686 * math.cos(math.radians(lat)) / (256 * 2 ** z)


def km_between(a: tuple[float, float], b: tuple[float, float]) -> float:
    """Equirectangular distance (km) between two lon/lat points: plenty at this map's scale."""
    lat = math.radians((a[1] + b[1]) / 2)
    return math.hypot((b[0] - a[0]) * 111.32 * math.cos(lat), (b[1] - a[1]) * 110.57)


def length_km(points: list[tuple[float, float]]) -> float:
    return sum(km_between(a, b) for a, b in zip(points, points[1:]))


# --- the symbols -------------------------------------------------------------------------------------------------
def _shift_reduce(mask: np.ndarray, k: int, op) -> np.ndarray:
    """Binary dilation (op=np.logical_or) or erosion (np.logical_and) by a k x k square, by shifted copies."""
    r0, r1 = (k - 1) // 2, k // 2
    pad = np.pad(mask, ((r0, r1), (r0, r1)), constant_values=op is np.logical_and)
    out = None
    h, w = mask.shape
    for dy in range(k):
        for dx in range(k):
            s = pad[dy:dy + h, dx:dx + w]
            out = s.copy() if out is None else op(out, s)
    return out


def dilate(mask: np.ndarray, k: int) -> np.ndarray:
    return _shift_reduce(mask, k, np.logical_or)


def erode(mask: np.ndarray, k: int) -> np.ndarray:
    return _shift_reduce(mask, k, np.logical_and)


def symbol_mask(rgb: np.ndarray, kind: str) -> np.ndarray:
    """Where the map shows a route of this kind (RGB uint8 image -> bool). Tuned on the 1946 sheets:
    road: the red fill of a main road: red over green by more than 90 with green and blue within 50 of each other (the
          hill tint is orange, green well over blue, and passed a plain red-over-green test), red above 130; or, where
          the road is printed over the valleys' green tint and reads darker (about 146, 64, 42 by the Sea of
          Galilee), red over green by more than 65 with green and blue within 35 (the orange tint's are 60+ apart);
    rail: dark, not blue, strokes at least about 4 px thick once the white dashes inside the line are closed, which
          drops the thin double-dashed cart tracks, hairlines and road casings (lettering stays, but it is short);
    track: any dark, not blue, pixel, dilated by one so a dashed track reads as continuous."""
    if kind not in KINDS:
        raise ValueError(f"unknown kind {kind!r}")
    c = rgb.astype(np.int16)
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    if kind == "road":
        return (((r - g) > 90) & (np.abs(g - b) < 50) & (r > 130)) | (((r - g) > 65) & (np.abs(g - b) < 35) & (r > 125))
    dark = (np.maximum(np.maximum(r, g), b) < 110) & ((b - r) < 25)
    if kind == "track":
        return dilate(dark, 3)
    closed = erode(dilate(dark, 5), 5)
    return dilate(erode(closed, 4), 4)


def cost_raster(rgb: np.ndarray, kind: str) -> np.ndarray:
    """1 on the route's symbol, OFF_SYMBOL_COST off it. For rail, any dark pixel costs THIN_RAIL_COST: the line is drawn
    thinner round tight curves (the Sorek canyon), where the thick-stroke mask breaks up and a cheaper chord cut across
    the meanders."""
    cost = np.where(symbol_mask(rgb, kind), 1.0, OFF_SYMBOL_COST)
    if kind == "rail":
        cost = np.where((cost > 1.0) & symbol_mask(rgb, "track"), THIN_RAIL_COST, cost)
    return cost


# --- tiles -------------------------------------------------------------------------------------------------------
def tile_path(x: int, y: int, z: int = TILE_ZOOM) -> Path:
    return TILE_CACHE / str(z) / str(x) / f"{y}.jpg"


def fetch_tile(x: int, y: int, z: int = TILE_ZOOM) -> bool:
    """Downloads one tile into the cache; False when the server has none (outside the sheets)."""
    path = tile_path(x, y, z)
    if path.exists():
        return True
    none = path.with_suffix(".none")       # remembers a tile the server does not have (outside the sheets)
    if none.exists():
        return False
    path.parent.mkdir(parents=True, exist_ok=True)
    req = urllib.request.Request(TILE_URL.format(z=z, x=x, y=y), headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            data = resp.read()
    except urllib.error.HTTPError as e:
        if e.code == 404:
            none.touch()
            return False
        raise
    tmp = path.with_suffix(".part")
    tmp.write_bytes(data)
    tmp.replace(path)
    return True


def window(x0: int, y0: int, x1: int, y1: int, fetch: bool) -> np.ndarray:
    """RGB pixels of the global pixel box [x0, x1) x [y0, y1) from cached tiles (white where no sheet covers it)."""
    out = np.full((y1 - y0, x1 - x0, 3), 255, np.uint8)
    for ty in range(y0 // 256, (y1 - 1) // 256 + 1):
        for tx in range(x0 // 256, (x1 - 1) // 256 + 1):
            path = tile_path(tx, ty)
            if path.with_suffix(".none").exists():
                continue
            if not path.exists():
                if not fetch:
                    raise FileNotFoundError(f"{path}: tile missing; run with --fetch")
                if not fetch_tile(tx, ty):
                    continue
            tile = np.asarray(Image.open(path).convert("RGB"))
            gx0, gy0 = tx * 256, ty * 256
            sx0, sy0 = max(x0, gx0), max(y0, gy0)
            sx1, sy1 = min(x1, gx0 + 256), min(y1, gy0 + 256)
            out[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = tile[sy0 - gy0:sy1 - gy0, sx0 - gx0:sx1 - gx0]
    return out


# --- tracing -----------------------------------------------------------------------------------------------------
def snap(cost: np.ndarray, p: tuple[int, int], radius_px: int) -> tuple[int, int]:
    """The on-symbol pixel (cost 1) nearest to p=(row, col) within radius_px, else p itself."""
    r, c = p
    r0, r1 = max(r - radius_px, 0), min(r + radius_px + 1, cost.shape[0])
    c0, c1 = max(c - radius_px, 0), min(c + radius_px + 1, cost.shape[1])
    rows, cols = np.nonzero(cost[r0:r1, c0:c1] <= 1.0)
    if rows.size == 0:
        return p
    d2 = (rows + r0 - r) ** 2 + (cols + c0 - c) ** 2
    i = int(np.argmin(d2))
    if d2[i] > radius_px ** 2:
        return p
    return int(rows[i] + r0), int(cols[i] + c0)


def least_cost_path(cost: np.ndarray, start: tuple[int, int], end: tuple[int, int]) -> list[tuple[int, int]]:
    from skimage.graph import route_through_array   # only the trace needs it (the tests and CI do not)
    path, _ = route_through_array(cost, start, end, fully_connected=True, geometric=True)
    return [(int(r), int(c)) for r, c in path]


def remove_spurs(chain: list[tuple[int, int]], radius_px: float) -> list[tuple[int, int]]:
    """Cuts every out-and-back excursion: from each kept point the walk jumps to the last point of the chain within
    radius_px of it, when that point is more than 3 radius_px steps further on. A waypoint off the line (a town circle,
    its name, the next road over) is reached by a spur from the line and left the same way, so the whole spur goes; a
    real line never comes back that close to itself (a hairpin would, which this map's railways and main roads do not
    have at that scale: two legs of a hairpin are drawn at least a millimetre, 250 m, apart)."""
    cell = max(int(math.ceil(radius_px)), 1)
    r2 = radius_px * radius_px
    grid: dict[tuple[int, int], list[int]] = {}
    for k, (x, y) in enumerate(chain):
        grid.setdefault((x // cell, y // cell), []).append(k)

    def last_near(i: int) -> int:
        x, y = chain[i]
        cx, cy = x // cell, y // cell
        return max((k for gx in (cx - 1, cx, cx + 1) for gy in (cy - 1, cy, cy + 1) for k in grid.get((gx, gy), ())
                    if (chain[k][0] - x) ** 2 + (chain[k][1] - y) ** 2 <= r2), default=i)

    out, i = [], 0
    while i < len(chain):
        out.append(chain[i])
        k = last_near(i)
        i = k + 1 if k > i + 3 * radius_px else i + 1
    if out[-1] != chain[-1]:
        out.append(chain[-1])
    return out


def trim_end_hooks(chain: list[tuple[int, int]], radius_px: float) -> list[tuple[int, int]]:
    """Cuts a hook at either end: a waypoint snapped a little off the route's end makes the line go out (to the town)
    and come back, within radius_px of its end, which remove_spurs's smaller radius does not catch. Near the start, if
    the distance from the first point rises and then falls again by more than a fifth of radius_px, the line starts at
    that fall's lowest point (where it turns back onto the route); likewise at the end. A line that only moves away
    from its ends is kept whole."""
    def cut(c: list[tuple[int, int]]) -> int:
        d = [math.dist(p, c[0]) for p in c]
        window = next((k for k, v in enumerate(d) if v > radius_px), len(c))
        if window < 2:
            return 0
        best, low, top = 0.0, 0, 0.0
        for k in range(window):                 # the largest fall below the farthest point so far
            top = max(top, d[k])
            if top - d[k] > best:
                best, low = top - d[k], k
        return low if best > radius_px / 5 else 0

    if len(chain) < 3:
        return chain
    chain = chain[cut(chain):]
    rev = chain[::-1]
    return rev[cut(rev):][::-1]


def smooth(points: list[tuple[float, float]], window: int) -> list[tuple[float, float]]:
    """Moving average over `window` points (odd), shrinking at the ends so both end points stay where they are."""
    if window < 3 or len(points) < 3:
        return list(points)
    pts = np.asarray(points, float)
    half = window // 2
    out = []
    for i in range(len(pts)):
        h = min(half, i, len(pts) - 1 - i)
        out.append(tuple(pts[i - h:i + h + 1].mean(axis=0)))
    return out


def resample(points: list[tuple[float, float]], step: float) -> list[tuple[float, float]]:
    """Points every `step` along a polyline (its own units), both ends kept."""
    pts = np.asarray(points, float)
    seg = np.hypot(*np.diff(pts, axis=0).T)
    s = np.concatenate([[0.0], np.cumsum(seg)])
    if s[-1] <= 0:
        return [tuple(pts[0]), tuple(pts[-1])]
    t = np.append(np.arange(0.0, s[-1], step), s[-1])
    return list(zip(np.interp(t, s, pts[:, 0]), np.interp(t, s, pts[:, 1])))


def gaussian_smooth(points: list[tuple[float, float]], spacing: float) -> list[tuple[float, float]]:
    """Curvature-adaptive Gaussian smoothing of evenly spaced points (`spacing` apart, own units = metres). The trace hops
    between the two edges of a thick symbol, a 100-300 m wobble that drew kinks every few hundred metres (review B14).
    A wobble's turns alternate, so over +-BEND_WINDOW_M its net heading change is small, while a real bend's adds up
    (at an S-bend's inflection the net turn is also small, so it is smoothed as a straight there: measured against the
    unsmoothed trace that moves no line more than 86 m, under a third of a road's width; review A-m4):
    each point takes a Gaussian of SMOOTH_SIGMA_M on a straight stretch, easing to BEND_SIGMA_M where the net turn grows
    from BEND_FROM_DEG to BEND_TO_DEG, so the wobble goes and the meanders stay. The window shrinks symmetrically at the
    ends, so both end points stay put. Deterministic: plain arithmetic on the input."""
    if len(points) < 3:
        return list(points)
    pts = np.asarray(points, float)
    head = np.unwrap(np.arctan2(*np.diff(pts, axis=0)[:, ::-1].T))
    head = np.append(head, head[-1])
    k = max(1, int(round(BEND_WINDOW_M / spacing)))
    n = len(pts)
    out = []
    for i in range(n):
        net = abs(head[min(n - 1, i + k)] - head[max(0, i - k)])
        t = np.clip((math.degrees(net) - BEND_FROM_DEG) / (BEND_TO_DEG - BEND_FROM_DEG), 0.0, 1.0)
        t = t * t * (3 - 2 * t)
        sigma = (SMOOTH_SIGMA_M + (BEND_SIGMA_M - SMOOTH_SIGMA_M) * t) / spacing
        half = min(int(math.ceil(3 * sigma)), i, n - 1 - i)
        w = np.exp(-0.5 * (np.arange(-half, half + 1) / sigma) ** 2)
        out.append(tuple((pts[i - half:i + half + 1] * w[:, None]).sum(axis=0) / w.sum()))
    return out


def simplify(points: list[tuple[float, float]], tol: float) -> list[tuple[float, float]]:
    """Douglas-Peucker in the points' own units (iterative; keeps both ends)."""
    if len(points) < 3:
        return list(points)
    pts = np.asarray(points, float)
    keep = np.zeros(len(pts), bool)
    keep[0] = keep[-1] = True
    stack = [(0, len(pts) - 1)]
    while stack:
        i, j = stack.pop()
        if j <= i + 1:
            continue
        a, b = pts[i], pts[j]
        ab = b - a
        n = np.hypot(*ab)
        seg = pts[i + 1:j] - a
        d = np.abs(ab[0] * seg[:, 1] - ab[1] * seg[:, 0]) / n if n > 0 else np.hypot(seg[:, 0], seg[:, 1])
        k = int(np.argmax(d))
        if d[k] > tol:
            m = i + 1 + k
            keep[m] = True
            stack += [(i, m), (m, j)]
    return [tuple(p) for p in pts[keep]]


def trace_route(route: dict, fetch: bool, preview: Path | None = None) -> list[tuple[float, float]]:
    """The route's lon/lat polyline: least-cost paths between its consecutive waypoints, joined and simplified."""
    via = [(float(w["lon"]), float(w["lat"])) for w in route["via"]]
    kind = route.get("trace", route["kind"])
    chain: list[tuple[int, int]] = []          # global pixel (x, y)
    on_symbol = total = 0
    off: set[tuple[int, int]] = set()           # global pixels traced off the symbol
    on_limit = THIN_RAIL_COST if kind == "rail" else 1.0   # a rail's thin stretches are still its symbol
    for a, b in zip(via, via[1:]):
        mpp = metres_per_px((a[1] + b[1]) / 2)
        ax, ay = lonlat_to_px(*a)
        bx, by = lonlat_to_px(*b)
        margin = max(WINDOW_MARGIN_M, 0.35 * km_between(a, b) * 1000) / mpp
        x0, x1 = int(min(ax, bx) - margin), int(max(ax, bx) + margin) + 1
        y0, y1 = int(min(ay, by) - margin), int(max(ay, by) + margin) + 1
        rgb = window(x0, y0, x1, y1, fetch)
        cost = cost_raster(rgb, kind)
        rad = int(SNAP_M / mpp)
        # A segment starts where the previous one ended, so the joins never jump.
        start = (chain[-1][1] - y0, chain[-1][0] - x0) if chain else snap(cost, (int(ay - y0), int(ax - x0)), rad)
        end = snap(cost, (int(by - y0), int(bx - x0)), rad)
        seg = least_cost_path(cost, start, end)
        for r, c in seg:
            if cost[r, c] <= on_limit:
                on_symbol += 1
            else:
                off.add((c + x0, r + y0))
        total += len(seg)
        glob = [(c + x0, r + y0) for r, c in seg]
        chain += glob if not chain else glob[1:]
        if preview is not None:
            img = Image.fromarray(rgb)
            from PIL import ImageDraw
            d = ImageDraw.Draw(img)
            d.line([(c, r) for r, c in seg], fill=(0, 160, 255), width=3)
            for p in (start, end):
                d.ellipse((p[1] - 6, p[0] - 6, p[1] + 6, p[0] + 6), outline=(0, 200, 0), width=3)
            preview.mkdir(parents=True, exist_ok=True)
            img.save(preview / f"{route['id']}_{len(list(preview.glob(route['id'] + '_*.png'))):02d}.png")
    route["_on_symbol"] = on_symbol / max(total, 1)
    chain = remove_spurs(chain, SPUR_M / metres_per_px(via[0][1]))
    chain = trim_end_hooks(chain, 6 * SPUR_M / metres_per_px(via[0][1]))   # a 400 m hook at the Burma Road's start
    # The longest run of the kept line off its symbol (a stretch the trace bridged: a name over the road, a town).
    gap = worst = 0
    at = chain[0]
    for p in chain:
        gap = gap + 1 if p in off else 0
        if gap > worst:
            worst, at = gap, p
    route["_max_gap_m"] = worst * metres_per_px(via[0][1])
    route["_gap_at"] = px_to_lonlat(*at)
    lonlat = [px_to_lonlat(x + 0.5, y + 0.5) for x, y in chain]
    # Simplify in metres (a local equirectangular frame), then back to lon/lat.
    lat0 = math.radians(sum(p[1] for p in lonlat) / len(lonlat))
    kx, ky = 111320.0 * math.cos(lat0), 110570.0
    even = resample(smooth([(lo * kx, la * ky) for lo, la in lonlat], SMOOTH_PX), RESAMPLE_M)
    simple = simplify(gaussian_smooth(even, RESAMPLE_M), SIMPLIFY_M)
    return [(x / kx, y / ky) for x, y in simple]


# --- files -------------------------------------------------------------------------------------------------------
def load_routes(path: Path = ROUTES) -> dict:
    doc = yaml12.load(path.read_text(encoding="utf-8"))
    validate_routes(doc, json_bounds())
    return doc


def json_bounds() -> dict:
    import json
    return json.loads(HEXES.read_text())["bounds"]


DATE_RULE = re.compile(r"\d{4}(-\d{2}-\d{2})?")


def _as_date(v: str) -> date:
    """A 'YYYY' (1 January) or 'YYYY-MM-DD' window bound."""
    return date(int(v), 1, 1) if len(v) == 4 else date.fromisoformat(v)


def validate_routes(doc: dict, bounds: dict) -> None:
    """Raises ValueError naming the route and field for anything malformed in routes_1948.yaml."""
    routes = doc.get("routes") if isinstance(doc, dict) else None
    if not routes:
        raise ValueError("routes_1948.yaml: no routes")
    seen = set()
    for r in routes:
        rid = r.get("id")
        if not isinstance(rid, str) or not rid or not rid.replace("_", "a").isalnum() or rid != rid.lower():
            raise ValueError(f"routes_1948.yaml: bad id {rid!r}")
        if rid in seen:
            raise ValueError(f"routes_1948.yaml: duplicate id {rid}")
        seen.add(rid)
        if r.get("kind") not in KINDS or r.get("trace", r["kind"]) not in KINDS:
            raise ValueError(f"{rid}: kind (and trace, when given) must be one of {KINDS}")
        if r.get("status") not in STATUSES:
            raise ValueError(f"{rid}: status must be one of {STATUSES}")
        signoff = review_record.problem(r)   # a person's sign-off beside the status (#375)
        if signoff:
            raise ValueError(f"{rid}: {signoff[0]} {signoff[1]}")
        if not r.get("name"):
            raise ValueError(f"{rid}: name required")
        if "opened" in r and (isinstance(r["opened"], bool) or not isinstance(r["opened"], int) or r["opened"] > 1948):
            raise ValueError(f"{rid}: opened, when given, must be a year <= 1948")
        window = []
        for key in ("from", "to"):
            if key not in r:
                continue
            v = r[key]
            if not isinstance(v, str) or not DATE_RULE.fullmatch(v):
                raise ValueError(f"{rid}: {key} must be a quoted 'YYYY' or 'YYYY-MM-DD'")
            window.append(_as_date(v))
        if "from" not in r:
            raise ValueError(f"{rid}: from is required (the date the map shows the route from)")
        if len(window) == 2 and window[1] <= window[0]:
            raise ValueError(f"{rid}: to must be after from")
        if "opened" in r and window[0].year < r["opened"]:
            raise ValueError(f"{rid}: from ({r['from']}) is before opened ({r['opened']})")
        via = r.get("via") or []
        if len(via) < 2:
            raise ValueError(f"{rid}: needs at least two waypoints")
        for w in via:
            if not w.get("name") or not str(w.get("url", "")).startswith("https://"):
                raise ValueError(f"{rid}: waypoint {w.get('name')!r} needs a name and an https url")
            lon, lat = w.get("lon"), w.get("lat")
            if not (isinstance(lon, (int, float)) and isinstance(lat, (int, float))
                    and bounds["lon0"] <= lon <= bounds["lon1"] and bounds["lat0"] <= lat <= bounds["lat1"]):
                raise ValueError(f"{rid}: waypoint {w['name']} outside the map ({lon}, {lat})")
        srcs = r.get("sources") or []
        if not srcs or not all(str(s.get("url", "")).startswith("https://") and s.get("checked") for s in srcs):
            raise ValueError(f"{rid}: every route needs sources with an https url and a checked date")


HEADER = """\
# GENERATED by tools/routes_1948.py from routes_1948.yaml and the Survey of Palestine 1:250,000 map of 1946 (public
# domain; tiles by Palestine Open Maps). Do not edit by hand: change the waypoints in routes_1948.yaml and re-run.
# Each route: its id, kind, status and display window (from, to) from routes_1948.yaml; the trace's quality (on_symbol:
# the share of traced pixels on the route's own map symbol; max_gap_m: the longest run off it, in metres); its length
# and its line as [lon, lat] pairs, 5 decimals.
# Map-only: the sim never reads this file, so it does not move the content hash.
"""


def dump_paths(routes: list[dict], lines: dict[str, list[tuple[float, float]]]) -> str:
    out = [HEADER, "routes:\n"]
    for r in routes:
        pts = lines[r["id"]]
        out.append(f"- id: {r['id']}\n  kind: {r['kind']}\n  status: {r['status']}\n  from: '{r['from']}'\n")
        if "to" in r:
            out.append(f"  to: '{r['to']}'\n")
        out.append(f"  on_symbol: {r['_on_symbol']:.2f}\n  max_gap_m: {round(r['_max_gap_m'])}\n")
        out.append(f"  length_km: {length_km(pts):.1f}\n  points:\n")
        for lon, lat in pts:
            out.append(f"  - [{lon:.5f}, {lat:.5f}]\n")
    return "".join(out)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--fetch", action="store_true", help="download missing map tiles into the cache")
    ap.add_argument("--only", nargs="*", help="trace only these route ids (the others keep their current lines)")
    ap.add_argument("--preview", type=Path, help="write a PNG per segment with the traced line into this directory")
    args = ap.parse_args(argv)
    doc = load_routes()
    routes = doc["routes"]
    old = {}
    if PATHS.exists():
        for r in (yaml12.load(PATHS.read_text(encoding="utf-8")) or {}).get("routes", []):
            old[r["id"]] = r
    lines = {}
    for r in routes:
        if args.only and r["id"] not in args.only and r["id"] in old:
            lines[r["id"]] = [tuple(p) for p in old[r["id"]]["points"]]
            r["_on_symbol"], r["_max_gap_m"] = old[r["id"]]["on_symbol"], old[r["id"]]["max_gap_m"]
            continue
        lines[r["id"]] = trace_route(r, args.fetch, args.preview)
        print(f"{r['id']:32} {r['kind']:5} {len(lines[r['id']]):5} points {length_km(lines[r['id']]):7.1f} km"
              f"  on its symbol {r['_on_symbol']:.0%}, longest gap {r['_max_gap_m']:.0f} m"
              + (" ending at %.4f, %.4f" % r["_gap_at"] if "_gap_at" in r else ""))
    PATHS.write_text(dump_paths(routes, lines), encoding="utf-8")
    print(f"wrote {PATHS.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
