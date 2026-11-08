"""unity/Assets/Editor/VehicleReviewSources.py --verify checks the three delivered vehicle archives against the files the
repo ships. #2014 moved the Egyptian Spitfire (placed in the game, #1932) to the Collection folder and renamed its FBX to
the Collection's <id>.fbx. #2046 moved the Syrian R35 there too, under its delivered -compatibility.fbx name, and the
Dakota followed (owner, 2 November 2026: "Over Tel Aviv, 15 May-11 Jun"; on #2274: "Off the coast is fine"), also keeping
its -compatibility.fbx name. This fails when verify stops following
the move (it would then look for a model that is no longer there, or pass without checking the shipped one)."""
import importlib.util
import json
import shutil
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "unity/Assets/Editor/VehicleReviewSources.py"
SPITFIRE = "generated-1948-13-egyptian-spitfire"
R35 = "generated-1948-14-syrian-renault-r35"
DAKOTA = "generated-1948-16-egyptian-dakota-c47"
COLLECTION = Path("unity/Assets/Art/Collection")
REVIEW = Path("unity/Assets/Editor/Generated1948VehicleReview")


def sources():
    spec = importlib.util.spec_from_file_location("vehicle_review_sources", SCRIPT)
    mod = importlib.util.module_from_spec(spec)
    keep = sys.dont_write_bytecode
    sys.dont_write_bytecode = True  # the script is in unity/Assets: Unity gives any __pycache__ there a .meta
    try:
        spec.loader.exec_module(mod)
    finally:
        sys.dont_write_bytecode = keep
    return mod


def test_the_placed_spitfire_is_verified_where_the_game_ships_it():
    mod = sources()
    assert mod.home(SPITFIRE) == COLLECTION / SPITFIRE, "the placed Spitfire is checked in the Collection folder (#2014)"
    assert mod.fbx_name(SPITFIRE) == SPITFIRE + ".fbx", "under the Collection's <id>.fbx name (#2014)"
    assert (ROOT / mod.home(SPITFIRE) / mod.fbx_name(SPITFIRE)).is_file()
    # #2046 placed the R35 too: in the Collection folder, under its delivered -compatibility.fbx name.
    assert mod.home(R35) == COLLECTION / R35, "the placed R35 is checked in the Collection folder (#2046)"
    assert mod.home(DAKOTA) == COLLECTION / DAKOTA, "the placed Dakota is checked in the Collection folder"
    assert set(mod.IDS.values()) == {SPITFIRE, R35, DAKOTA}, "all three candidates are placed; none is review-only"
    assert not (ROOT / REVIEW / DAKOTA).exists(), "the Dakota left the review folder"
    for mid in set(mod.IDS.values()) - {SPITFIRE}:
        assert mod.fbx_name(mid) == mid + "-compatibility.fbx", mid
        assert (ROOT / mod.home(mid) / mod.fbx_name(mid)).is_file()


def test_verify_passes_on_the_shipped_files():
    # Every archive, GLB, colour map and FBX hash in sources.json against the files where they now are.
    sources().verify(ROOT)


def shipped_copy(mod, dest):
    # Copy exactly what verify() reads (sources.json, the archives, each model's colour map and FBX) into dest.
    manifest = json.loads((ROOT / mod.SOURCES / "sources.json").read_text())
    (dest / mod.SOURCES).mkdir(parents=True)
    shutil.copyfile(ROOT / mod.SOURCES / "sources.json", dest / mod.SOURCES / "sources.json")
    for item in manifest["items"]:
        if not (dest / mod.SOURCES / item["archive"]).exists():  # two items share one archive; copy it once
            shutil.copyfile(ROOT / mod.SOURCES / item["archive"], dest / mod.SOURCES / item["archive"])
        home = mod.home(item["id"])
        (dest / home).mkdir(parents=True, exist_ok=True)
        for name in (item["id"] + "_basecolor.jpg", mod.fbx_name(item["id"])):
            shutil.copyfile(ROOT / home / name, dest / home / name)


@pytest.mark.parametrize("target", [
    "archive ZIP",
    "base-colour JPEG",
    "compatibility FBX",
])
def test_verify_fails_on_one_changed_byte(tmp_path, target):
    # #2737: test_verify_passes_on_the_shipped_files stays green if verify() is a no-op. This one fails then.
    mod = sources()
    shipped_copy(mod, tmp_path)
    mod.verify(tmp_path)  # the untouched copy passes, so a broken copy can't make the raise below pass for the wrong reason
    path = {
        "archive ZIP": tmp_path / mod.SOURCES / "statecraft-vehicles-aircraft-01.zip",
        "base-colour JPEG": tmp_path / mod.home(SPITFIRE) / (SPITFIRE + "_basecolor.jpg"),
        "compatibility FBX": tmp_path / mod.home(R35) / mod.fbx_name(R35),
    }[target]
    assert path.is_relative_to(tmp_path)  # never flip a byte in a committed file
    blob = bytearray(path.read_bytes())
    blob[len(blob) // 2] ^= 0xFF
    path.write_bytes(bytes(blob))
    with pytest.raises(AssertionError):
        mod.verify(tmp_path)


def test_loading_the_script_writes_no_bytecode_under_assets():
    # The script lives in unity/Assets/Editor. A __pycache__ written there gets a .meta from the next Unity run, which leaves
    # the tree dirty and stops a gate that needs a clean one.
    cache = Path(importlib.util.cache_from_source(str(SCRIPT)))
    cache.unlink(missing_ok=True)  # a valid cache from an older run is never rewritten, so the check needs it gone
    sources()
    assert not cache.exists(), f"loading the script wrote {cache.relative_to(ROOT)}"
