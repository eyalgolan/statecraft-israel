import os
from pathlib import Path

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
    assert ac.GUARDED_MATERIALS == ("sand", "sea") and ac.INFO_MATERIALS == ("wood",)   # wood is not like-for-like
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
    assert set(g) == {"sand", "sea"} and not g["sand"]["ok"] and g["sea"]["ok"]
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


def material_shots(tmp_path, sand=PATCH["sand"], sea=PATCH["sea"]):
    promo = tmp_path / "promo.png"; stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]], (480, 270)).save(promo)
    shots = tmp_path / "shots"; shots.mkdir()
    stripes([sea, sand, PATCH["wood"]], (480, 270)).save(shots / "diorama02.png")
    return ac.main(["art_compare.py", str(shots), "t", "--log", str(tmp_path / "art-log.tsv")], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo})


def test_main_passes_when_every_gating_material_is_in_band(tmp_path, capsys):
    assert material_shots(tmp_path) == 0
    out = capsys.readouterr().out
    assert "MATERIAL diorama02.png sand" in out and "PASS" in out and "GUARD FAIL" not in out
    assert "whole-frame warmth (information only)" in out and "MATERIAL diorama02.png wood" in out and "information only" in out


def test_a_material_guard_fail_exits_3_and_names_the_material_its_value_and_band(tmp_path, capsys):
    assert material_shots(tmp_path, sand=(229, 173, 119)) == ac.EXIT_GUARD_FAIL == 3
    fails = [l for l in capsys.readouterr().out.splitlines() if l.startswith("GUARD FAIL: ")]
    assert len(fails) == 1 and fails[0].startswith("GUARD FAIL: sand 0.4314 vs [0.2824, 0.4235]")


def test_a_gating_material_missing_from_the_capture_exits_4_not_3(tmp_path, capsys):
    assert material_shots(tmp_path, sea=PATCH["land"]) == ac.EXIT_UNMEASURABLE == 4
    assert "cannot measure" in capsys.readouterr().out


STUB_UV = r"""#!/usr/bin/env bash
# A stand-in for uv: Unity (through machine_lock) and the browser do nothing; web_check writes each pose's PNG;
# art_compare writes a compare image, prints what STUB_ART_OUT holds and exits STUB_ART_EXIT.
shift
case "$1" in
  tools/machine_lock.py) exit 0 ;;
  tools/web_check.py) while [[ $# -gt 0 && "$1" != --shots ]]; do shift; done
    for p in gameplay03 diorama02 hero06judea hero06negev; do echo x > "$2/$p.png"; done ;;
  tools/art_compare.py) mkdir -p "$2/editor"; echo x > "$2/gameplay03-compare.png"; echo x > "$2/editor/a-compare.png"
    printf '%b' "$STUB_ART_OUT"; exit "$STUB_ART_EXIT" ;;
esac
"""


def run_checkpoint(tmp_path, art_exit, art_out=""):
    """tools/art_checkpoint.sh in a scratch git repository holding only what it reads, with uv stubbed (no Unity)."""
    import shutil
    import subprocess
    repo, tools = tmp_path / "repo", Path(__file__).resolve().parents[1]
    for rel, text in {"unity/ProjectSettings/QualitySettings.asset": "q\n", "unity/Builds/Web-disabled/index.html": "x\n",
                      "sim/Statecraft.Sim.Tests/golden/manifest.txt": "hash1950 aa\nhash1952 bb\n"}.items():
        (repo / rel).parent.mkdir(parents=True, exist_ok=True); (repo / rel).write_text(text)
    (repo / "tools").mkdir(); shutil.copy(tools / "art_checkpoint.sh", repo / "tools")
    (repo / "unity/Assets/Scripts/Map").mkdir(parents=True)
    shutil.copy(tools.parent / "unity/Assets/Scripts/Map/PromoPose.cs", repo / "unity/Assets/Scripts/Map")
    git = ["git", "-C", str(repo), "-c", "user.name=t", "-c", "user.email=t@example.com"]
    subprocess.run(git + ["init", "-q"], check=True); subprocess.run(git + ["add", "-A"], check=True)
    subprocess.run(git + ["commit", "-qm", "t"], check=True)
    stub = tmp_path / "bin"; stub.mkdir(); (stub / "uv").write_text(STUB_UV); (stub / "uv").chmod(0o755)
    env = {**os.environ, "PATH": f"{stub}:{os.environ['PATH']}", "UNITY": "/bin/true",
           "STUB_ART_EXIT": str(art_exit), "STUB_ART_OUT": art_out}
    return subprocess.run(["bash", "tools/art_checkpoint.sh", "--no-build", "cp"], cwd=repo, env=env, capture_output=True, text=True)


def test_the_checkpoint_completes_then_lists_each_guard_fail_and_exits_3(tmp_path):
    r = run_checkpoint(tmp_path, 3, "GUARD FAIL: sand 0.2948 vs [0.3049, 0.4574] in diorama02\nGUARD FAIL: sea -0.1729 vs [-0.3117, -0.2078] in diorama02\n")
    assert r.returncode == 3, r.stderr
    assert "ART CHECKPOINT cp:" in r.stdout
    tail = r.stdout[r.stdout.index("ART CHECKPOINT cp:"):]
    assert "GUARD FAIL: sand 0.2948 vs [0.3049, 0.4574] in diorama02" in tail and "GUARD FAIL: sea -0.1729" in tail


def test_the_checkpoint_exits_3_even_without_a_guard_fail_line(tmp_path):
    r = run_checkpoint(tmp_path, 3)
    assert r.returncode == 3 and "ART CHECKPOINT cp:" in r.stdout, r.stderr


def test_the_checkpoint_passes_and_stops_on_genuine_errors_as_before(tmp_path):
    ok = run_checkpoint(tmp_path / "ok", 0)
    assert ok.returncode == 0 and "ART CHECKPOINT cp:" in ok.stdout and "GUARD FAIL" not in ok.stdout
    for code in (1, 2, 4):          # no pose matched or a crash, a blank capture, a material too rare to measure
        r = run_checkpoint(tmp_path / str(code), code)
        assert r.returncode == code and "ART CHECKPOINT" not in r.stdout, code


def test_the_material_mask_adds_the_minimap_and_the_bottom_plates_to_the_hud_mask():
    """Their navy is B > R, so unmasked they read as sea (about 16% of main's diorama02 "sea")."""
    assert ac.MATERIAL_MASK == ac.HUD_MASK + ((0.86, 0.62, 1.0, 1.0), (0.0, 0.92, 0.17, 1.0), (0.76, 0.94, 0.87, 1.0))
    navy = np.asarray(stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]], (480, 270))).copy()
    navy[int(0.62 * 270):, int(0.86 * 480):] = (14, 22, 38)          # a minimap-like navy panel bottom right
    with_ui = ac.material_warmth(Image.fromarray(navy, "RGB"))
    plain = ac.material_warmth(stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]], (480, 270)))
    assert with_ui["sea"] == plain["sea"]


def test_a_cooler_sand_fails_the_sand_guard_below_its_band():
    promo = stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]])
    g = ac.material_guard(stripes([PATCH["sea"], (209, 173, 139), PATCH["wood"]]), promo)   # 20/255 cooler
    assert not g["sand"]["ok"] and g["sand"]["ours"] < g["sand"]["lo"] and g["sea"]["ok"]


def test_only_the_webgl_shot_is_gated_and_every_material_line_names_its_file(tmp_path, capsys):
    promo = tmp_path / "promo.png"; stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]], (480, 270)).save(promo)
    shots = tmp_path / "shots"; (shots / "editor").mkdir(parents=True)
    stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]], (480, 270)).save(shots / "diorama02.png")
    stripes([PATCH["sea"], (229, 173, 119), PATCH["wood"]], (480, 270)).save(shots / "editor" / "PC-diorama02.png")
    assert ac.main(["art_compare.py", str(shots), "t", "--log", str(tmp_path / "log.tsv")], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo}) == 0
    out = capsys.readouterr().out
    lines = [l for l in out.splitlines() if l.startswith("MATERIAL ")]
    assert all(" diorama02.png " in l or " editor/PC-diorama02.png " in l for l in lines), lines
    assert any(l.startswith("MATERIAL editor/PC-diorama02.png sand") and "information only" in l and "FAIL" not in l
               for l in lines)
    assert "GUARD FAIL" not in out


def test_the_material_guard_and_main_leave_a_navy_minimap_out_of_the_sea(tmp_path, capsys):
    """material_guard's default mask (and what main() passes) must be MATERIAL_MASK, not HUD_MASK."""
    plain = stripes([PATCH["sea"], PATCH["sand"], PATCH["wood"]], (480, 270))
    a = np.asarray(plain).copy(); a[int(0.62 * 270):, int(0.86 * 480):] = (14, 22, 38)    # a navy minimap
    with_ui = Image.fromarray(a, "RGB")
    assert ac.material_guard(with_ui, plain)["sea"]["ours"] == ac.material_guard(plain, plain)["sea"]["ours"]
    promo = tmp_path / "promo.png"; plain.save(promo)
    for name, img in (("plain", plain), ("ui", with_ui)):
        (tmp_path / name).mkdir(); img.save(tmp_path / name / "diorama02.png")
        ac.main(["art_compare.py", str(tmp_path / name), "t", "--log", str(tmp_path / "log.tsv")], root=tmp_path,
                promos={"gameplay03": promo, "diorama02": promo})
    sea = [l.split(" vs ")[0] for l in capsys.readouterr().out.splitlines() if l.startswith("MATERIAL diorama02.png sea")]
    assert len(sea) == 2 and sea[0] == sea[1], sea
