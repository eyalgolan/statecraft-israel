import numpy as np
import pytest
from PIL import Image

import art_compare as ac


def solid(rgb, size=(64, 36)):
    return Image.new("RGB", size, rgb)


def noisy(size=(192, 108), seed=1):
    return Image.fromarray(np.random.default_rng(seed).integers(0, 255, (size[1], size[0], 3), dtype=np.uint8), "RGB")


def test_metrics_read_saturation_contrast_and_warmth():
    grey = ac.metrics(solid((128, 128, 128)))
    assert grey["saturation"] == 0 and grey["contrast"] == 0 and grey["warmth"] == 0
    red = ac.metrics(solid((255, 0, 0)))
    assert red["saturation"] > 0.99 and red["warmth"] > 0.99


def test_palette_distance_is_zero_for_the_same_image_and_one_for_disjoint_palettes():
    a = solid((200, 150, 60))
    assert ac.histogram_distance(a, a) == 0
    assert ac.histogram_distance(solid((0, 0, 0)), solid((255, 255, 255))) == 1


def test_compose_puts_both_images_at_the_same_height_under_a_caption():
    out = ac.compose(solid((10, 20, 30), (1920, 1080)), solid((200, 100, 50), (2752, 1536)), "t")
    assert out.height == ac.CAPTION_PX + ac.HEIGHT_PX
    assert out.width == round(1920 * ac.HEIGHT_PX / 1080) + ac.GAP_PX + round(2752 * ac.HEIGHT_PX / 1536)


def test_colour_blind_simulation_keeps_greys_and_merges_red_with_green():
    grey = np.asarray(ac.simulate_cvd(solid((128, 128, 128)), "deuteranopia"), dtype=int)
    assert np.abs(grey - 128).max() <= 1
    red = np.asarray(ac.simulate_cvd(solid((200, 30, 30)), "protanopia"), dtype=int)[0, 0]
    green = np.asarray(ac.simulate_cvd(solid((30, 160, 30)), "protanopia"), dtype=int)[0, 0]
    assert abs(int(red[0]) - int(green[0])) < 100      # 170 apart before simulation


def test_main_pairs_each_pose_with_its_promo_logs_a_row_and_writes_cvd_copies(tmp_path):
    promo = tmp_path / "promo.png"; noisy((275, 153), 2).save(promo)
    shots = tmp_path / "shots"; (shots / "editor").mkdir(parents=True)
    noisy().save(shots / "gameplay03.png")
    noisy(seed=3).save(shots / "editor" / "PC-diorama02.png")
    noisy(seed=4).save(shots / "unrelated.png")
    log = tmp_path / "elsewhere" / "art-log.tsv"
    code = ac.main(["art_compare.py", str(shots), "task-x", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo})
    assert code == 0
    for stem in ("gameplay03", "editor/PC-diorama02"):
        for suffix in ("-compare.png", "-compare-deuteranopia.png", "-compare-protanopia.png"):
            assert (shots / (stem + suffix)).exists(), stem + suffix
    rows = log.read_text().splitlines()
    assert len(rows) == 2 and all(r.startswith("task-x-hudmask\t") for r in rows)
    assert ac.main(["art_compare.py", str(shots), "again", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo}) == 0
    assert not (shots / "gameplay03-compare-compare.png").exists()


def test_a_uniform_screenshot_fails(tmp_path):
    promo = tmp_path / "promo.png"; noisy().save(promo)
    shots = tmp_path / "shots"; shots.mkdir()
    solid((0, 0, 0), (192, 108)).save(shots / "gameplay03.png")       # a black capture (the WebGL readback failed)
    assert ac.main(["art_compare.py", str(shots), "t"], root=tmp_path, promos={"gameplay03": promo, "diorama02": promo}) == 2


def test_art_log_rows_name_the_branch_the_commit_and_the_absolute_image(tmp_path):
    """Review M5: the art log is shared by every worktree, so a row says which branch and commit made it and where its
    image is (absolute path), not a worktree-relative path."""
    promo = tmp_path / "promo.png"; noisy().save(promo)
    shots = tmp_path / "shots"; shots.mkdir()
    noisy(seed=5).save(shots / "gameplay03.png")
    log = tmp_path / "art-log.tsv"
    assert ac.main(["art_compare.py", str(shots), "t", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo}) == 0
    name, branch, commit, image = log.read_text().splitlines()[0].split("\t")[:4]
    assert (name, branch, commit) == ("t-hudmask", "-", "-")             # tmp_path is not a git checkout
    assert image == str((shots / "gameplay03.png").resolve())
    from pathlib import Path
    branch, commit = ac.source_identity(Path(ac.__file__).resolve().parents[1])
    assert branch != "-" and len(commit) >= 7


def test_the_hud_mask_boxes_are_pinned():
    """The top bar (6.4% of the frame since #624; promo 03's is 6.5%), the date plate hanging below it and the left column
    of round buttons, as (x0, y0, x1, y1) fractions of the frame, applied alike to the build frame and the promo."""
    assert ac.HUD_MASK == ((0.0, 0.0, 1.0, 0.07), (0.0, 0.0, 0.13, 0.10), (0.0, 0.10, 0.045, 0.44))


def paint_inside(img, mask, seed):
    """img with random pixels painted strictly inside every box of mask, and nowhere else."""
    a = np.asarray(img, dtype=np.uint8).copy()
    h, w = a.shape[:2]
    rng = np.random.default_rng(seed)
    for x0, y0, x1, y1 in mask:
        r0, r1, c0, c1 = int(np.ceil(y0 * h)), int(y1 * h), int(np.ceil(x0 * w)), int(x1 * w)
        a[r0:r1, c0:c1] = rng.integers(0, 255, (r1 - r0, c1 - c0, 3), dtype=np.uint8)
    return Image.fromarray(a, "RGB")


def test_pixels_inside_the_hud_mask_do_not_move_the_metrics():
    frame, promo = noisy((480, 270), 6), noisy((688, 384), 7)                # build 16:9, promo 03's 2752x1536 shape
    frame2, promo2 = paint_inside(frame, ac.HUD_MASK, 8), paint_inside(promo, ac.HUD_MASK, 9)
    assert ac.metrics(frame2) == ac.metrics(frame)
    assert ac.histogram_distance(frame2, promo2) == ac.histogram_distance(frame, promo)
    # not vacuous: the painted pixels do move the whole-frame numbers
    assert ac.metrics(frame2, mask=()) != ac.metrics(frame, mask=())
    assert ac.histogram_distance(frame2, promo2, mask=()) != ac.histogram_distance(frame, promo, mask=())


def test_a_mask_that_leaves_no_pixels_fails_instead_of_passing_vacuously():
    img = noisy()
    for bad in (((0.0, 0.0, 1.0, 1.0),), ((0.0, 0.0, 1.0, 0.5), (0.0, 0.5, 1.0, 1.0)), ((0.5, 0.0, 0.2, 1.0),),
                ((-0.1, 0.0, 0.5, 0.5),), ((0.0, 0.0, 1.0, 0.55),), ((0.0, 0.0, 0.999, 0.999),)):
        with pytest.raises(ValueError):
            ac.metrics(img, mask=bad)
        with pytest.raises(ValueError):
            ac.histogram_distance(img, img, mask=bad)


def test_the_hud_mask_keeps_most_of_the_build_frame_and_of_promo_03():
    for w, h in ((1920, 1080), (1280, 720), (2752, 1536)):
        assert ac.keep(w, h, ac.HUD_MASK).mean() > 0.85, (w, h)
    assert ac.MIN_KEPT == 0.5


def test_the_resize_pad_drops_every_pixel_blended_from_the_masked_pixels():
    """The masked pixels are zeroed before the 256x256 resize; without the pad the bicubic blend of that black with the
    image would land in other bins. A solid image therefore keeps exactly one bin, enlarged or shrunk."""
    for size in ((64, 36), (480, 270), (1920, 1080), (688, 384)):
        h = ac.histogram(solid((200, 150, 60), size))
        assert np.count_nonzero(h) == 1 and h.max() == 1.0, size


def test_masked_art_log_rows_and_titles_are_labelled_so_they_are_not_mistaken_for_whole_frame_rows(tmp_path, capsys):
    """Masking moves main's gameplay03 palette distance by about 0.027, more than the main + 0.02 tolerance, so a masked
    row must never read as one of the whole-frame rows already in the shared log (art_checkpoint.sh's -units precedent)."""
    promo = tmp_path / "promo.png"; noisy().save(promo)
    shots = tmp_path / "shots"; shots.mkdir()
    noisy(seed=5).save(shots / "gameplay03.png")
    log = tmp_path / "art-log.tsv"
    log.write_text("main6\tmain\t988ad417\t/x/gameplay03.png\t0.4\t0.2\t0.1\t0.3505\n")   # a whole-frame row from before
    assert ac.main(["art_compare.py", str(shots), "main6", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo}) == 0
    old, new = (r.split("\t")[0] for r in log.read_text().splitlines())
    assert old == "main6" and new == "main6" + ac.MASKED_SUFFIX == "main6-hudmask"
    assert "main6-hudmask (HUD masked)" in capsys.readouterr().out


PATCH = {"sand": (219, 173, 129), "sea": (83, 116, 146), "wood": (60, 44, 31), "land": (150, 115, 80),
         "green": (110, 140, 80), "neutral": (128, 128, 128)}


def stripes(colours, size=(300, 200)):
    """A frame of full-height vertical stripes, one per colour, equal widths."""
    a = np.zeros((size[1], size[0], 3), dtype=np.uint8)
    edges = np.linspace(0, size[0], len(colours) + 1).astype(int)
    for c, x0, x1 in zip(colours, edges, edges[1:]):
        a[:, x0:x1] = c
    return Image.fromarray(a, "RGB")


def test_the_material_classifier_labels_each_known_patch():
    for name, rgb in PATCH.items():
        labels = ac.classify(solid(rgb))
        assert set(np.unique(labels)) == {ac.MATERIALS.index(name)}, name
    edge = {(240, 238, 235): "neutral",       # white salt and labels: chroma under NEUTRAL_SAT
            (150, 120, 150): "land",          # R == B is not sea; warm-or-level hue at mid luma
            (120, 160, 170): "sea",           # the Dead Sea's pale cyan is water
            (70, 70, 63): "wood",             # dark, barely warm (chroma 0.10) is still dark wood
            (190, 150, 110): "sand"}          # luma 0.6
    for rgb, name in edge.items():
        assert ac.MATERIALS[int(ac.classify(solid(rgb))[0, 0])] == name, (rgb, name)


def test_the_wood_land_and_sand_luma_thresholds_are_pinned():
    assert (ac.NEUTRAL_SAT, ac.WOOD_LUMA, ac.SAND_LUMA) == (0.08, 0.30, 0.55)
    assert ac.GUARDED_MATERIALS == ("sand", "sea", "wood")
    assert (ac.MATERIAL_TOLERANCE, ac.MIN_MATERIAL_SHARE) == (0.2, 0.02)
    assert ac.material_band(0.381) == pytest.approx((0.3048, 0.4572))
    assert ac.material_band(-0.254) == pytest.approx((-0.3048, -0.2032))


def test_material_warmth_gives_each_class_its_share_and_mean_warmth():
    m = ac.material_warmth(stripes([PATCH["sea"], PATCH["sand"]]), mask=())
    assert m["sea"][0] == pytest.approx(0.5) and m["sand"][0] == pytest.approx(0.5)
    assert m["sand"][1] == pytest.approx((219 - 129) / 255) and m["sea"][1] == pytest.approx((83 - 146) / 255)
    assert m["wood"] == (0.0, None)


def test_a_warmer_sand_fails_the_sand_guard_although_the_whole_frame_warmth_is_unchanged():
    promo = stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]])
    assert all(r["ok"] for r in ac.material_guard(promo, promo).values())
    # sand 20/255 warmer, wood 20/255 cooler: the same whole-frame warmth, so the old metric cannot tell them apart
    shot = stripes([PATCH["sea"], (229, 173, 119), (50, 44, 41)])
    assert ac.metrics(shot)["warmth"] == pytest.approx(ac.metrics(promo)["warmth"], abs=1e-6)
    g = ac.material_guard(shot, promo)
    assert not g["sand"]["ok"] and not g["wood"]["ok"] and g["sea"]["ok"]
    assert g["sand"]["ours"] > g["sand"]["hi"]


def test_a_material_too_rare_in_either_image_raises_instead_of_passing():
    full = stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]])
    no_sea = stripes([PATCH["land"], PATCH["sand"], PATCH["wood"]])
    with pytest.raises(ValueError, match="sea"):
        ac.material_guard(no_sea, full)
    with pytest.raises(ValueError, match="sea"):
        ac.material_guard(full, no_sea)
    a = np.asarray(full).copy(); a[:, :99] = PATCH["land"]; a[:, 99] = PATCH["sea"]   # sea is 1 column of 300: 0.3%
    with pytest.raises(ValueError, match="sea"):
        ac.material_guard(Image.fromarray(a, "RGB"), full)


def test_main_prints_the_diorama02_material_verdicts_and_the_whole_frame_warmth_as_information(tmp_path, capsys):
    promo = tmp_path / "promo.png"; stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]], (480, 270)).save(promo)
    shots = tmp_path / "shots"; shots.mkdir()
    stripes([PATCH["sea"], (229, 173, 119), PATCH["wood"]], (480, 270)).save(shots / "diorama02.png")
    log = tmp_path / "art-log.tsv"
    assert ac.main(["art_compare.py", str(shots), "t", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo}) == 0
    out = capsys.readouterr().out
    assert "MATERIAL diorama02 sand" in out and "FAIL" in out and "MATERIAL diorama02 sea" in out
    assert "whole-frame warmth (information only)" in out
