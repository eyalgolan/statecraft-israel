"""unity/Assets/Editor/VehicleReviewSources.py --verify checks the three delivered vehicle archives against the files the
repo ships. #2014 moved the Egyptian Spitfire (placed in the game, #1932) to the Collection folder and renamed its FBX to
the Collection's <id>.fbx. #2046 moved the Syrian R35 there too, under its delivered -compatibility.fbx name; the Dakota
stays review-only in the Editor folder. This fails when verify stops following
the move (it would then look for a model that is no longer there, or pass without checking the shipped one)."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "unity/Assets/Editor/VehicleReviewSources.py"
SPITFIRE = "generated-1948-13-egyptian-spitfire"
R35 = "generated-1948-14-syrian-renault-r35"
COLLECTION = Path("unity/Assets/Art/Collection")
REVIEW = Path("unity/Assets/Editor/Generated1948VehicleReview")


def sources():
    spec = importlib.util.spec_from_file_location("vehicle_review_sources", SCRIPT)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def test_the_placed_spitfire_is_verified_where_the_game_ships_it():
    mod = sources()
    assert mod.home(SPITFIRE) == COLLECTION / SPITFIRE, "the placed Spitfire is checked in the Collection folder (#2014)"
    assert mod.fbx_name(SPITFIRE) == SPITFIRE + ".fbx", "under the Collection's <id>.fbx name (#2014)"
    assert (ROOT / mod.home(SPITFIRE) / mod.fbx_name(SPITFIRE)).is_file()
    # #2046 placed the R35 too: in the Collection folder, under its delivered -compatibility.fbx name.
    assert mod.home(R35) == COLLECTION / R35, "the placed R35 is checked in the Collection folder (#2046)"
    for mid in set(mod.IDS.values()) - {SPITFIRE, R35}:
        assert mod.home(mid) == REVIEW / mid, f"{mid} stays review-only"
    for mid in set(mod.IDS.values()) - {SPITFIRE}:
        assert mod.fbx_name(mid) == mid + "-compatibility.fbx", mid
        assert (ROOT / mod.home(mid) / mod.fbx_name(mid)).is_file()


def test_verify_passes_on_the_shipped_files():
    # Every archive, GLB, colour map and FBX hash in sources.json against the files where they now are.
    sources().verify(ROOT)
