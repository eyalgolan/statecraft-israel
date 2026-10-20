"""The one-asset experiment must not call negligible triangle cuts a useful LOD."""
import pytest

from building_lod_probe import assess_planar_reduction


def test_fragmented_house_is_rejected_instead_of_forcing_a_triangle_budget():
    result = assess_planar_reduction(6000, 5988)
    assert result["accepted"] is False
    assert result["reduction_fraction"] == pytest.approx(0.002)


def test_a_geometry_budget_is_only_a_candidate_not_a_quality_or_frame_time_verdict():
    result = assess_planar_reduction(6000, 3000)
    assert result["accepted"] is True
    assert result["visual_quality"] == "unverified"
    assert result["frame_time_benefit"] == "unmeasured"


@pytest.mark.parametrize("before,after", [(0, 0), (6000, 0), (6000, -1), (6000, 6001)])
def test_invalid_measurements_fail_closed(before, after):
    with pytest.raises(ValueError):
        assess_planar_reduction(before, after)
