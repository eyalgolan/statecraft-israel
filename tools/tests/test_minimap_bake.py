"""The minimap's painted relief (unity/Assets/Resources/Map/MinimapRelief.png, HUD round 2, #398) is the bake of today's
inputs and code.

tools/minimap_bake.py makes it from the heightmap, the biome colour bake, the land-use and coast textures and the
sub-district hexes, all committed. CI runs no Unity, so this re-bakes it here and compares the pixels with the committed
PNG: a change to any input or to the bake's code without a re-bake (uv run tools/minimap_bake.py) fails. The bake is
float arithmetic in numpy and Pillow, so a different toolchain may round a channel the other way: one step is allowed."""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
import minimap_bake  # noqa: E402

REBAKE = "re-bake it: uv run tools/minimap_bake.py"


def test_the_committed_minimap_is_the_bake_of_its_inputs():
    committed = np.asarray(Image.open(minimap_bake.OUT).convert("RGB"), np.int16)
    fresh = minimap_bake.bake().astype(np.int16)
    assert committed.shape == fresh.shape, f"{minimap_bake.OUT.name} is {committed.shape}, the bake makes {fresh.shape}: {REBAKE}"
    diff = np.abs(committed - fresh)
    off = int((diff > 1).any(axis=-1).sum())
    assert off == 0, f"{off} pixels differ by more than one step (max {int(diff.max())}): {REBAKE}"


def test_the_minimap_has_the_maps_aspect_and_sides_a_multiple_of_four():
    import json
    meta = json.loads((minimap_bake.MAP_DIR / "heightmap.json").read_text())
    assert minimap_bake.WIDTH % 4 == 0 and minimap_bake.HEIGHT % 4 == 0
    assert abs(minimap_bake.WIDTH / minimap_bake.HEIGHT - meta["width_km"] / meta["length_km"]) < 0.005
