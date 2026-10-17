"""tools/coast_measure.py (issue #388, sea round 3): the beach, foam and lace numbers across a west-facing coast."""
from pathlib import Path

import pytest
from PIL import Image

import coast_measure as cm

ROOT = Path(__file__).resolve().parents[2]
SEA, FOAM, SAND, LAND = (60, 120, 150), (235, 238, 236), (225, 200, 150), (120, 130, 70)
CROWN = (46, 34, 16)   # a citrus crown in the gameplay03 shot


def coast(beach: int, foam: list[tuple[int, int]], shore: int = 200, h: int = 40, slope: float = 0.0) -> Image.Image:
    """Sea west of the shore (x = shore + slope * y), foam runs (start, width) counted seaward from it, then beach px of
    sand and land beyond."""
    im = Image.new("RGB", (cm.FRAME_W, h), LAND)
    px = im.load()
    for y in range(h):
        s = shore + round(slope * y)
        for x in range(cm.FRAME_W):
            px[x, y] = SEA if x < s else SAND if x < s + beach else LAND
        for start, width in foam:
            for x in range(s - start - width, s - start):
                px[x, y] = FOAM
    return im


def test_the_classes_follow_the_promo_sea_rules():
    assert [cm.classify(*c) for c in (SEA, FOAM, SAND, LAND)] == ["W", "F", "S", "L"]


def test_a_straight_coast_measures_its_beach_foam_and_lace_lines():
    im = coast(beach=60, foam=[(0, 4), (10, 3), (20, 3)])     # swash at the shore and two breaker lines out to 23 px
    s = cm.measure(im, 0, 40, 100, 400)
    assert len(s["rows"]) == 20                                # every other row
    assert s["beach_px"] == 60 and s["perp_factor"] == 1
    assert s["foam_px"] == 23 and s["lace_lines"] == 3
    assert s["foam_share"] == pytest.approx(10 / 23)
    assert s["sand_luma_p50"] == pytest.approx(cm.luma(*SAND))


def test_a_gap_of_twelve_px_is_bridged_and_a_wider_one_ends_the_beach():
    im = coast(beach=80, foam=[])
    px = im.load()
    for y in range(im.height):
        for x in range(230, 242):                              # 12 px of land 30 px into the beach: bridged
            px[x, y] = LAND
    assert cm.measure(im, 0, 40, 100, 400)["beach_px"] == 80
    for y in range(im.height):
        px[242, y] = LAND                                      # 13 px: the beach ends at 30
    assert cm.measure(im, 0, 40, 100, 400)["beach_px"] == 30


def test_a_crown_over_the_waterline_does_not_move_the_shore_or_the_foam():
    """Main's citrus crowns stand at the waterline in the gameplay03 south window; the shore must stay where the sea
    ends, not jump past the crown to the next sand."""
    plain = coast(beach=60, foam=[(0, 4), (10, 3), (20, 3)])
    crowned = plain.copy()
    px = crowned.load()
    for y in range(crowned.height):
        for x in range(200, 214):                              # a 14 px crown on the sand, from the waterline in
            px[x, y] = CROWN
    a, b = cm.measure(plain, 0, 40, 100, 400), cm.measure(crowned, 0, 40, 100, 400)
    assert [r["shore_x"] for r in b["rows"]] == [r["shore_x"] for r in a["rows"]] == [200] * 20
    assert (b["foam_px"], b["lace_lines"], b["foam_share"]) == (a["foam_px"], a["lace_lines"], a["foam_share"]) == (
        23, 3, pytest.approx(10 / 23))
    assert b["beach_px"] == 46                                 # the sand still reads from where it shows


def test_stripe_rows_is_the_share_of_rows_with_3_or_more_foam_lines():
    """#690: evenly spaced white bands on the northern frontage read as combed stripes; the share of rows with 3 or more
    lines in the foam band counts them (promo 03's frontage: 0.05)."""
    three = coast(beach=60, foam=[(0, 4), (10, 3), (20, 3)])
    two = coast(beach=60, foam=[(0, 4), (10, 3)])
    assert cm.measure(three, 0, 40, 100, 400)["stripe_rows"] == 1.0
    assert cm.measure(two, 0, 40, 100, 400)["stripe_rows"] == 0.0
    mixed = cm.combine([cm.measure(im, 0, 40, 100, 400) for im in (three, two, two)])
    assert mixed["stripe_rows"] == 0.0


def test_a_slanted_coast_is_measured_across_it():
    im = coast(beach=50, foam=[(0, 10)], h=200, slope=1.0)     # 45 degrees: rows cross the strip at sqrt(2) its width
    s = cm.measure(im, 0, 200, 100, 600)
    assert s["slope_k"] == pytest.approx(1.0, abs=0.01)
    assert s["beach_px"] == pytest.approx(50 / 2 ** 0.5, abs=0.5)
    assert s["foam_px"] == pytest.approx(10 / 2 ** 0.5, abs=0.5)


def test_no_shore_gives_no_rows_and_fails_every_band():
    s = cm.measure(Image.new("RGB", (cm.FRAME_W, 20), LAND), 0, 20, 0, 400)
    assert s["rows"] == [] and cm.combine([s]) == {}
    assert len(cm.check({}, cm.BANDS["north"])) == len(cm.BANDS["north"])


def test_several_shots_combine_by_median_and_the_check_names_each_miss():
    shots = [cm.measure(coast(b, [(0, 30)]), 0, 40, 100, 400) for b in (30, 50, 70)]
    med = cm.combine(shots)
    assert med["beach_px"] == 50 and med["foam_px"] == 30
    assert cm.check(med, {"beach_px": (40, 95)}) == []
    assert cm.check(med, {"beach_px": (60, None), "foam_px": (None, 20)}) == [
        "beach_px 50.0 outside [60, None]", "foam_px 30.0 outside [None, 20]"]


def test_a_frame_of_another_width_is_scaled_to_1920(tmp_path):
    p = tmp_path / "wide.png"
    Image.new("RGB", (3840, 2160), SEA).save(p)
    assert cm.load(p).size == (1920, 1080)


@pytest.mark.parametrize("window", sorted(cm.WINDOWS))
def test_promo_03_measures_inside_its_own_bands(window):
    """The bands were set from promo 03 (plan C1, section 1); the promo itself must pass them."""
    s = cm.measure(cm.load(ROOT / "promo" / "final" / "03-gameplay-1936.png"), *cm.PROMO_WINDOWS[window])
    assert cm.check(cm.combine([s]), cm.BANDS[window]) == []


def test_the_cli_checks_the_band_and_exits_1_outside_it(tmp_path, capsys):
    p = tmp_path / "shot.png"
    im = Image.new("RGB", (1920, 1080), LAND)
    im.paste(coast(beach=10, foam=[(0, 3)], shore=700, h=220), (0, 110))   # a 10 px beach in the north window
    im.save(p)
    assert cm.main([str(p), "--window", "north", "--check"]) == 1
    assert "OUTSIDE beach_px" in capsys.readouterr().out
