# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0", "pyyaml==6.0.3"]
# ///
"""M2 geography check: shaded relief + biome hexes + settlements, north up, true km aspect.

Usage: uv run tools/preview_map.py [out.png]   (default: tools/.cache/map-preview.png)
Compare the result by eye against promo/ref/geo-reference.png before accepting map data."""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hexgrid as hg  # noqa: E402
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
BIOME_RGB = {"sea": (70, 130, 180), "lake": (40, 110, 200), "coastal_plain": (175, 215, 120),
             "hills": (80, 135, 55), "mountains": (150, 120, 90), "valley": (215, 160, 50), "lowland": (150, 200, 100),
             "desert": (220, 195, 140)}
DOT_RGB = (200, 20, 20)


def lonlat_to_px(lon: float, lat: float, meta: dict, size: tuple[int, int]) -> tuple[float, float]:
    return ((lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * (size[0] - 1),
            (meta["lat1"] - lat) / (meta["lat1"] - meta["lat0"]) * (size[1] - 1))


def hillshade(elev: np.ndarray) -> np.ndarray:
    gy, gx = np.gradient(elev)
    shade = (-gx - gy) / np.sqrt(gx ** 2 + gy ** 2 + 400.0)  # light from the north-west
    return np.clip(0.75 + 0.5 * shade, 0, 1)


def render(map_dir: Path, hexes: dict, settlements: list[dict], out: Path, width_px: int = 800) -> None:
    elev, meta = hg.load_elevation(map_dir)
    km_w = (meta["lon1"] - meta["lon0"]) * hg.km_per_deg_lon(meta)
    km_h = (meta["lat1"] - meta["lat0"]) * hg.KM_PER_DEG_LAT
    size = (width_px, round(width_px * km_h / km_w))
    shade = Image.fromarray((hillshade(elev) * 255).astype("uint8")).resize(size, Image.BILINEAR)
    img = Image.merge("RGB", [shade] * 3).convert("RGBA")
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    px_per_km = width_px / km_w
    for hx in hexes["hexes"]:
        cx, cy = hg.hex_center_km(hx["q"], hx["r"], hexes["hex_size_km"])
        corners = [((cx + hexes["hex_size_km"] * math.cos(math.radians(30 + 60 * i))) * px_per_km,
                    (cy + hexes["hex_size_km"] * math.sin(math.radians(30 + 60 * i))) * px_per_km) for i in range(6)]
        d.polygon(corners, fill=BIOME_RGB[hx["biome"]] + (110,), outline=(255, 255, 255, 60))
    img = Image.alpha_composite(img, layer).convert("RGB")
    d = ImageDraw.Draw(img)
    font = ImageFont.load_default(size=max(10, width_px // 60))
    for s in settlements:
        x, y = lonlat_to_px(s["lon"], s["lat"], meta, size)
        rad = max(3, width_px // 160)
        d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=DOT_RGB)
        d.text((x + rad + 2, y - rad - 2), s["name"], fill=(0, 0, 0), font=font)
    img.save(out)


def main() -> None:
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "tools" / ".cache" / "map-preview.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    hexes = json.loads((ROOT / "content" / "map" / "hexes.json").read_text())
    towns_file = ROOT / "content" / "map" / "settlements_1948.yaml"
    towns = yaml12.load(towns_file.read_text())["settlements"] if towns_file.exists() else []
    render(hg.MAP_DIR, hexes, towns, out)
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
