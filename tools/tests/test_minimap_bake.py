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


def test_the_minimap_sides_are_powers_of_two():
    """Unity block-compresses a mipmapped texture only with power-of-two sides (review of #584: an NPOT relief stayed
    uncompressed RGB, about 2.5 MB in the editor); the presenter sizes the picture to the frame's own aspect."""
    for side in (minimap_bake.WIDTH, minimap_bake.HEIGHT):
        assert side > 0 and side & (side - 1) == 0, side
