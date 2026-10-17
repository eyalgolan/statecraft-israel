"""unity/Assets/Resources/Map/DuneCoasts.json (#690 option C): the dune coasts' one definition, read by DuneCoasts.cs
(the kits and the water pass) and by the tools."""
import json
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
FILE = ROOT / "unity" / "Assets" / "Resources" / "Map" / "DuneCoasts.json"


def test_each_band_is_a_lon_lat_box_with_a_ramp_inside_it():
    bands = json.loads(FILE.read_text())["bands"]
    assert [b["name"] for b in bands] == ["Jaffa south to the map's southern coast", "Haifa Bay"]
    for b in bands:
        assert b["lon0"] < b["lon1"] and b["lat0"] < b["lat1"]
        assert 0 < 2 * b["ramp"] <= min(b["lon1"] - b["lon0"], b["lat1"] - b["lat0"])


def test_jaffa_south_ends_between_32_03_and_32_07_n():
    """Full south of 32.03 N and gone by 32.07 N: the edge #685's beach and #690's surf share."""
    jaffa = json.loads(FILE.read_text())["bands"][0]
    assert jaffa["lat1"] == 32.07 and jaffa["lat1"] - jaffa["ramp"] == pytest.approx(32.03)
