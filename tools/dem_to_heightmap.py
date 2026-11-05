# /// script
# requires-python = ">=3.11"
# dependencies = ["rasterio==1.5.2", "numpy==2.5.3", "requests==2.32.5"]
# ///
"""Copernicus GLO-30 DEM -> Unity heightmap (16-bit grid, row 0 = north, packed by heightmap_codec) + metadata JSON.

Usage: uv run tools/dem_to_heightmap.py
Tiles are cached in tools/.cache/dem/ (not committed). A tile that returns 404 is open sea.
No tile data at all, or a flat mosaic, is refused rather than written (#2652)."""
import json
import math
from pathlib import Path

import numpy as np
import rasterio
import requests
from rasterio.transform import from_bounds
from rasterio.warp import Resampling, reproject

import heightmap_codec
from geo import KM_PER_DEG_LAT, KM_PER_DEG_LON_EQ

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "tools" / ".cache" / "dem"
OUT = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
BOUNDS = {"lon0": 34.20, "lon1": 35.90, "lat0": 29.45, "lat1": 33.35}
SIZE = 2049  # Unity Terrain heightmap resolution must be 2^n + 1
# Required notice for adapted/modified WorldDEM-30 data (Copernicus DEM licence; wording as quoted on
# developers.google.com/earth-engine/datasets/catalog/COPERNICUS_DEM_GLO30_2024_1, checked 2026-10-09).
ATTRIBUTION = ("produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space "
               "GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved")


def tile_url(lat: int, lon: int) -> str:
    name = f"Copernicus_DSM_COG_10_N{lat:02d}_00_E{lon:03d}_00_DEM"
    return f"https://copernicus-dem-30m.s3.amazonaws.com/{name}/{name}.tif"


def tiles_for_bounds(b: dict) -> list[tuple[int, int]]:
    return [(lat, lon) for lat in range(math.floor(b["lat0"]), math.ceil(b["lat1"]))
            for lon in range(math.floor(b["lon0"]), math.ceil(b["lon1"]))]


def fetch_tile(lat: int, lon: int, cache: Path = CACHE) -> Path | None:
    """Download one tile into the cache; None if it does not exist (404 = all sea)."""
    path = cache / f"N{lat:02d}E{lon:03d}.tif"
    if path.exists():
        return path
    r = requests.get(tile_url(lat, lon), timeout=600)
    if r.status_code == 404:
        return None
    r.raise_for_status()
    cache.mkdir(parents=True, exist_ok=True)
    path.write_bytes(r.content)
    return path


def mosaic(paths: list[Path], b: dict, size: int) -> np.ndarray:
    """Average-resample all tiles onto a size x size lon/lat grid; uncovered cells = 0 m (sea)."""
    dst = np.full((size, size), np.nan, dtype="float32")
    transform = from_bounds(b["lon0"], b["lat0"], b["lon1"], b["lat1"], size, size)
    for p in paths:
        with rasterio.open(p) as src:
            reproject(source=rasterio.band(src, 1), destination=dst, dst_transform=transform,
                      dst_crs="EPSG:4326", dst_nodata=np.nan, init_dest_nodata=False,
                      resampling=Resampling.average)
    return np.nan_to_num(dst, nan=0.0)


def write_heightmap(elev: np.ndarray, b: dict, out: Path) -> dict:
    lo, hi = float(elev.min()), float(elev.max())
    if hi == lo:
        raise ValueError(f"the DEM is flat at {lo:g} m; refusing to write a heightmap")
    raw = np.round((elev - lo) / (hi - lo) * 65535).astype("<u2")
    out.mkdir(parents=True, exist_ok=True)
    heightmap_codec.write(out, raw)   # packed, lossless (issue #44): the raw grid is never written
    meta = {"width": int(elev.shape[1]), "height": int(elev.shape[0]), **b,
            "elev_min_m": lo, "elev_max_m": hi,
            "width_km": (b["lon1"] - b["lon0"]) * KM_PER_DEG_LON_EQ
                        * math.cos(math.radians((b["lat0"] + b["lat1"]) / 2)),
            "length_km": (b["lat1"] - b["lat0"]) * KM_PER_DEG_LAT,
            "attribution": ATTRIBUTION}
    (out / "heightmap.json").write_text(json.dumps(meta, indent=2))
    return meta


def main() -> None:
    paths = []
    for lat, lon in tiles_for_bounds(BOUNDS):
        p = fetch_tile(lat, lon)
        print(f"N{lat} E{lon:03d}: {'sea (404)' if p is None else p.name}")
        if p:
            paths.append(p)
    if not paths:
        raise RuntimeError("no DEM tile has data (every tile answered 404); refusing to write an all-sea heightmap")
    meta = write_heightmap(mosaic(paths, BOUNDS, SIZE), BOUNDS, OUT)
    print(f"wrote {OUT / heightmap_codec.FILE}: elev {meta['elev_min_m']:.1f}..{meta['elev_max_m']:.1f} m, "
          f"{meta['width_km']:.1f} x {meta['length_km']:.1f} km")


if __name__ == "__main__":
    main()
