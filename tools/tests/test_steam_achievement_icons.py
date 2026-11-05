"""The Steam achievement icons (#2449): the script's ICONS keys, the API names in docs/steam/README.md, the names in
AchievementRules.cs and the committed PNGs must all be the same five names, and the generated icons must have the
size and colour mode Steam gets."""
import re
from pathlib import Path

from PIL import Image, ImageChops

import steam_achievement_icons as icons

ROOT = Path(__file__).resolve().parents[2]
COMMITTED = ROOT / "docs" / "steam" / "achievements"


def readme_api_names():
    # Table rows of "Achievements" in docs/steam/README.md start with the API name in backticks.
    text = (ROOT / "docs" / "steam" / "README.md").read_text(encoding="utf-8")
    return set(re.findall(r"^\| `([A-Z][A-Z0-9_]+)` \|", text, re.M))


def rules_api_names():
    # ForObjective values and CampaignDone are the only upper-case string assignments in the file.
    text = (ROOT / "unity" / "Assets" / "Scripts" / "Platform" / "AchievementRules.cs").read_text(encoding="utf-8")
    return set(re.findall(r'=\s*"([A-Z][A-Z0-9_]+)"', text))


def test_icon_keys_match_readme_table_and_achievement_rules():
    keys = set(icons.ICONS)
    assert len(keys) == 5
    assert readme_api_names() == keys
    assert rules_api_names() == keys


def test_every_api_name_has_both_committed_pngs():
    for api in icons.ICONS:
        for state in ("unlocked", "locked"):
            assert (COMMITTED / f"{api}_{state}.png").is_file(), f"{api}_{state}.png is missing"


def test_generated_icons_are_256_rgb_and_locked_ones_are_grey(tmp_path):
    icons.main(["steam_achievement_icons.py", str(tmp_path)])
    for api in icons.ICONS:
        unlocked = Image.open(tmp_path / f"{api}_unlocked.png")
        locked = Image.open(tmp_path / f"{api}_locked.png")
        assert unlocked.size == locked.size == (256, 256)
        assert unlocked.mode == locked.mode == "RGB"
        r, g, b = locked.split()
        assert ImageChops.difference(r, g).getbbox() is None and ImageChops.difference(g, b).getbbox() is None, f"{api}_locked.png is not greyscale"
