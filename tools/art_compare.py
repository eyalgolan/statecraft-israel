# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0"]
# ///
"""Owner art checkpoint: puts each screenshot next to the promo image its pose copies, prints rough numbers, and writes
colour-blind simulations of each comparison.

Usage: uv run tools/art_compare.py <shots-dir> <checkpoint-name> [--log PATH]
For every PNG under <shots-dir> whose name ends in a pose (gameplay03, diorama02), writes <name>-compare.png beside it
(screenshot left, promo right, a caption with the numbers), <name>-compare-deuteranopia.png and
<name>-compare-protanopia.png (Machado, Oliveira and Fernandes 2009, severity 1), and appends a row to the art log
(default captures/visual/art-log.tsv; art_checkpoint.sh points every worktree at the main checkout's): checkpoint, branch,
short commit (both "-" outside a git checkout), the image's absolute path, saturation, contrast (luma std), warmth
(mean R - B), and the palette distance to the promo (total variation between
8x8x8 RGB histograms; 0 = same palette, 1 = disjoint). Every number leaves out HUD_MASK's boxes (the HUD) of both
images (pass mask=() for the whole frame), and main() names its rows <checkpoint-name>-hudmask, so they cannot be
mistaken for the whole-frame rows written before the mask. For diorama02 it also prints a MATERIAL line per
gating material (sand, sea) of the WebGL shot with its warmth against its band from promo 02, PASS or FAIL (#695),
and wood, the whole-frame warmth and the editor renders as information. Exit codes (EXIT_GUARD_FAIL and its neighbours
below): 0 all passed, 1 no screenshot matched a pose, 2 a near-uniform screenshot (luma std < 0.01: a black or blank
WebGL capture), 3 a gating material out of its band, 4 a gating material too rare to measure."""
import argparse
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
PROMOS = {"gameplay03": ROOT / "promo" / "final" / "03-gameplay-1936.png",
          "diorama02": ROOT / "promo" / "final" / "02-map-diorama.png"}
CAPTION_PX, GAP_PX, HEIGHT_PX = 44, 16, 720
UNIFORM_STD = 0.01
MIN_KEPT = 0.5         # a mask must leave at least this share of the frame measured
MASKED_SUFFIX = "-hudmask"   # main() names its art-log rows <checkpoint-name>-hudmask: they leave out HUD_MASK, and
                             # rows written before it measured the whole frame (as art_checkpoint.sh's -units rows)
Mask = tuple[tuple[float, float, float, float], ...]
# Screen regions the guards leave out of both the screenshot and the promo, as (x0, y0, x1, y1) fractions of the frame:
# the HUD covers the same places in both, and promo 03's HUD cannot be hidden. Measured on 1920x1080 ?shot captures
# after #624 and on promo 03 (2752x1536).
HUD_MASK: Mask = (
    (0.0, 0.0, 1.0, 0.07),     # the top bar: ours 6.4% of the height, promo 03's 6.5%
    (0.0, 0.0, 0.13, 0.10),    # the date plate hanging below it: ours 12.4% x 9.1%, promo 03's 12% x 8.6%
    (0.0, 0.10, 0.045, 0.44),  # the left column of round buttons: ours (four) to 34% of the height, promo 03's (five) to 43%
)
# diorama02's warmth is measured per material against promo 02 (owner's ruling on #695, "Measure each material"): the
# whole frame mixes materials in different shares (promo 02's board is about a quarter sea, ours an eighth, with a third
# table), so its warmth compares geography as much as colour. classify() puts each pixel in one class, in this order:
#   neutral  chroma (max - min) / max under NEUTRAL_SAT: greys, white salt and labels, which carry no hue;
#   sea      blue above red (B > R): the sea, the Dead Sea and the lakes (and promo 02's glass towers and solar fields);
#   green    green above red (and B <= R): fields and orchards;
#   wood     warm (R >= G, R >= B) and luma under WOOD_LUMA: the table, the frame's walnut and promo 02's dark backdrop;
#   land     warm, luma WOOD_LUMA to SAND_LUMA: the hills and the cut earth;
#   sand     warm, luma at or over SAND_LUMA: bright warm ground (the Negev, the beaches, sunlit hills and stone towns).
# The luma cuts sit between hand-picked region means on promo 02 and main's diorama02 (T77), not in gaps of the pixel
# histograms: the wood's mean luma is 0.13-0.26 (our table 0.18-0.20), the hills' 0.35-0.50 and the Negev sand's 0.70-0.71.
MATERIALS = ("neutral", "sea", "green", "wood", "land", "sand")
NEUTRAL_SAT, WOOD_LUMA, SAND_LUMA = 0.08, 0.30, 0.55
GUARDED_MATERIALS = ("sand", "sea")
# The material guard also leaves out the minimap (bottom right), the "Click the map" plate (bottom left) and the map-layer
# legend (bottom right, beside the minimap): their navy is B > R and would count as sea, about 16% of main's diorama02 sea
# pixels. Applied to promo 02 alike (there they cover open sand, a corner of the Dead Sea and some hills: no UI). HUD_MASK
# itself, and the guards that use it, are unchanged.
MATERIAL_MASK: Mask = HUD_MASK + ((0.86, 0.62, 1.0, 1.0), (0.0, 0.92, 0.17, 1.0), (0.76, 0.94, 0.87, 1.0))
# Printed, not gated: promo 02's dark backdrop is mostly under HUD_MASK's top-bar box, so its "wood" is mostly the frame's
# walnut and terrain shadow while ours is mostly the table, which is not like-for-like.
INFO_MATERIALS = ("wood",)
# main()'s exit codes: 0 every check passed; 1 no screenshot matched a pose (an uncaught crash also exits 1); 2 a blank
# capture; 3 a gating material outside its band (EXIT_GUARD_FAIL; each failure printed as "GUARD FAIL: ..."); 4 a gating
# material too rare to measure in the screenshot or the promo (EXIT_UNMEASURABLE). 1, 2 and 4 are errors in the capture
# or the setup, 3 is the art missing its target.
EXIT_GUARD_FAIL, EXIT_UNMEASURABLE = 3, 4
# Each guarded material's band is promo 02's mean warmth (R - B) for it, plus or minus MATERIAL_TOLERANCE of its size: the
# old whole-frame band [0.087, 0.145] was 40% of its promo anchor wide, here centred on the promo, so either way is a miss.
MATERIAL_TOLERANCE = 0.2
# A guarded material must cover at least this share of the measured pixels in both images (about 37,000 pixels of a
# 1920x1080 frame); below it the guard raises, so a capture missing a material cannot pass on a handful of pixels.
MIN_MATERIAL_SHARE = 0.02
MACHADO = {
    "deuteranopia": np.array([[0.367322, 0.860646, -0.227968], [0.280085, 0.672501, 0.047413], [-0.011820, 0.042940, 0.968881]]),
    "protanopia": np.array([[0.152286, 1.052583, -0.204868], [0.114503, 0.786281, 0.099216], [-0.003882, -0.048116, 1.051998]]),
}


def source_identity(root: Path) -> tuple[str, str]:
    """(branch, short commit) of the checkout at root, so rows in the shared art log say which worktree made them."""
    def git(*args: str) -> str:
        r = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True)
        return r.stdout.strip() if r.returncode == 0 and r.stdout.strip() else "-"
    if git("rev-parse", "--show-toplevel") == "-" or Path(git("rev-parse", "--show-toplevel")).resolve() != root.resolve():
        return "-", "-"
    return git("rev-parse", "--abbrev-ref", "HEAD"), git("rev-parse", "--short", "HEAD")


def keep(width: int, height: int, mask: Mask, pad: tuple[int, int] = (0, 0)) -> np.ndarray:
    """(height, width) booleans, True outside every box of mask (each grown by pad pixels). Raises ValueError on a box
    outside [0, 1] or inside out, and when the mask leaves less than MIN_KEPT of the frame: a guard must not pass on an
    empty or near-empty region."""
    k = np.ones((height, width), dtype=bool)
    for x0, y0, x1, y1 in mask:
        if not (0 <= x0 < x1 <= 1 and 0 <= y0 < y1 <= 1):
            raise ValueError(f"mask box {(x0, y0, x1, y1)} is not inside the frame")
        k[max(0, int(np.floor(y0 * height)) - pad[1]):int(np.ceil(y1 * height)) + pad[1],
          max(0, int(np.floor(x0 * width)) - pad[0]):int(np.ceil(x1 * width)) + pad[0]] = False
    if k.mean() < MIN_KEPT:
        raise ValueError(f"mask {mask} leaves {k.mean():.1%} of a {width}x{height} frame, under {MIN_KEPT:.0%}")
    return k


def metrics(img: Image.Image, mask: Mask = HUD_MASK) -> dict[str, float]:
    rgb = img.convert("RGB")
    k = keep(rgb.width, rgb.height, mask)
    a = (np.asarray(rgb, dtype=np.float32) / 255.0)[k]
    s = (np.asarray(rgb.convert("HSV"), dtype=np.float32)[..., 1] / 255.0)[k]
    luma = 0.2126 * a[:, 0] + 0.7152 * a[:, 1] + 0.0722 * a[:, 2]
    return {"saturation": float(s.mean()), "contrast": float(luma.std()), "warmth": float((a[:, 0] - a[:, 2]).mean())}


def histogram(img: Image.Image, bins: int = 8, mask: Mask = HUD_MASK) -> np.ndarray:
    """The normalised bins x bins x bins RGB histogram of img at 256x256, outside mask."""
    px = np.asarray(img.convert("RGB")).copy()
    px[~keep(px.shape[1], px.shape[0], mask)] = 0             # the masked pixels cannot reach the resize below
    # and the 256x256 pixels the resize blended from them are dropped too (bicubic reaches 2 pixels of the
    # smaller grid: 2 here when shrinking, 2 * 256 / side when enlarging)
    pad = tuple(int(np.ceil(2 * max(1, 256 / side))) + 1 for side in (px.shape[1], px.shape[0]))
    small = np.asarray(Image.fromarray(px, "RGB").resize((256, 256)), dtype=np.int64)
    x = (small[keep(256, 256, mask, pad)] // (256 // bins)).reshape(-1, 3)
    h = np.bincount(x[:, 0] * bins * bins + x[:, 1] * bins + x[:, 2], minlength=bins ** 3).astype(np.float64)
    return h / h.sum()


def histogram_distance(a: Image.Image, b: Image.Image, bins: int = 8, mask: Mask = HUD_MASK) -> float:
    return float(0.5 * np.abs(histogram(a, bins, mask) - histogram(b, bins, mask)).sum())


def classify(img: Image.Image) -> np.ndarray:
    """(height, width) indices into MATERIALS, one per pixel (the rules are above MATERIALS)."""
    a = np.asarray(img.convert("RGB"), dtype=np.float32) / 255.0
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx, mn = a.max(-1), a.min(-1)
    chroma = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0.0)
    luma = 0.2126 * r + 0.7152 * g + 0.0722 * b
    out = np.full(r.shape, MATERIALS.index("sand"), dtype=np.int8)
    out[luma < SAND_LUMA] = MATERIALS.index("land")
    out[luma < WOOD_LUMA] = MATERIALS.index("wood")
    out[g > r] = MATERIALS.index("green")
    out[b > r] = MATERIALS.index("sea")
    out[chroma < NEUTRAL_SAT] = MATERIALS.index("neutral")
    return out


def material_warmth(img: Image.Image, mask: Mask = MATERIAL_MASK) -> dict[str, tuple[float, float | None]]:
    """Per material: (its share of the pixels outside mask, its mean warmth R - B, or None when it has no pixel)."""
    rgb = img.convert("RGB")
    k = keep(rgb.width, rgb.height, mask)
    a = np.asarray(rgb, dtype=np.float32) / 255.0
    w, labels = (a[..., 0] - a[..., 2])[k], classify(rgb)[k]
    return {name: (float((labels == i).mean()), float(w[labels == i].mean()) if (labels == i).any() else None)
            for i, name in enumerate(MATERIALS)}


def material_band(promo_warmth: float) -> tuple[float, float]:
    t = MATERIAL_TOLERANCE * abs(promo_warmth)
    return promo_warmth - t, promo_warmth + t


def material_guard(shot: Image.Image, promo: Image.Image, mask: Mask = MATERIAL_MASK) -> dict[str, dict[str, float | bool]]:
    """Each guarded material's mean warmth in shot against its band from promo. Raises ValueError when a guarded material
    covers less than MIN_MATERIAL_SHARE of either image, rather than pass on too few pixels."""
    ours, theirs = material_warmth(shot, mask), material_warmth(promo, mask)
    out: dict[str, dict[str, float | bool]] = {}
    for name in GUARDED_MATERIALS:
        for who, m in (("the screenshot", ours), ("the promo", theirs)):
            if m[name][0] < MIN_MATERIAL_SHARE:
                raise ValueError(f"{name} covers {m[name][0]:.2%} of {who}, under {MIN_MATERIAL_SHARE:.0%}: cannot measure it")
        o, p = ours[name][1], theirs[name][1]
        assert o is not None and p is not None
        lo, hi = material_band(p)
        out[name] = {"ours": o, "promo": p, "lo": lo, "hi": hi, "ok": lo <= o <= hi}
    return out


def simulate_cvd(img: Image.Image, kind: str) -> Image.Image:
    a = np.asarray(img.convert("RGB"), dtype=np.float64) / 255.0
    lin = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    out = np.clip(lin @ MACHADO[kind].T, 0.0, 1.0)
    srgb = np.where(out <= 0.0031308, out * 12.92, 1.055 * out ** (1 / 2.4) - 0.055)
    return Image.fromarray(np.round(srgb * 255).astype(np.uint8), "RGB")


def compose(shot: Image.Image, promo: Image.Image, title: str) -> Image.Image:
    def fit(img: Image.Image) -> Image.Image:
        return img.convert("RGB").resize((round(img.width * HEIGHT_PX / img.height), HEIGHT_PX), Image.LANCZOS)
    left, right = fit(shot), fit(promo)
    out = Image.new("RGB", (left.width + GAP_PX + right.width, CAPTION_PX + HEIGHT_PX), (14, 22, 38))
    out.paste(left, (0, CAPTION_PX))
    out.paste(right, (left.width + GAP_PX, CAPTION_PX))
    ImageDraw.Draw(out).text((12, 14), title, fill=(230, 214, 170))
    return out


def main(argv: list[str], root: Path = ROOT, promos: dict[str, Path] = PROMOS) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("shots", type=Path)
    ap.add_argument("name")
    ap.add_argument("--log", type=Path)
    a = ap.parse_args(argv[1:])
    log = a.log or root / "captures" / "visual" / "art-log.tsv"
    log.parent.mkdir(parents=True, exist_ok=True)
    rows, uniform, unmeasurable, guard_fails = 0, [], [], []
    branch, commit = source_identity(root)
    for path in sorted(a.shots.rglob("*.png")):
        if "-compare" in path.stem:
            continue
        pose = next((p for p in promos if path.stem.endswith(p)), None)
        if pose is None:
            continue
        shot, promo = Image.open(path), Image.open(promos[pose])
        m, p, d = metrics(shot), metrics(promo), histogram_distance(shot, promo)
        if m["contrast"] < UNIFORM_STD:
            uniform.append(path)
            print(f"FAIL: {path} is near-uniform (luma std {m['contrast']:.4f}): the capture is blank")
            continue
        title = (f"{a.name}{MASKED_SUFFIX} (HUD masked) | {path.stem} (left) vs {promos[pose].name} (right) | saturation {m['saturation']:.2f}/{p['saturation']:.2f}"
                 f"  contrast {m['contrast']:.3f}/{p['contrast']:.3f}  warmth {m['warmth']:.3f}/{p['warmth']:.3f}  palette distance {d:.2f}")
        pair = compose(shot, promo, title)
        pair.save(path.with_name(path.stem + "-compare.png"))
        for kind in MACHADO:
            simulate_cvd(pair, kind).save(path.with_name(f"{path.stem}-compare-{kind}.png"))
        with log.open("a") as f:
            f.write("\t".join([a.name + MASKED_SUFFIX, branch, commit, str(path.resolve()), f"{m['saturation']:.4f}", f"{m['contrast']:.4f}", f"{m['warmth']:.4f}", f"{d:.4f}"]) + "\n")
        print("ART " + title)
        if pose == "diorama02":
            # Only the WebGL ?shot (directly under <shots-dir>) is gated; the editor renders (editor/) are printed as
            # information, since the WebGL player is what the owner sees.
            rel, gated = path.relative_to(a.shots).as_posix(), path.parent == a.shots
            tag = "" if gated else " (information only: editor render, not gated)"
            print(f"MATERIAL {rel} whole-frame warmth (information only) {m['warmth']:.4f} vs promo {p['warmth']:.4f}")
            try:
                guard = material_guard(shot, promo, mask=MATERIAL_MASK)
            except ValueError as e:
                print(f"{'FAIL' if gated else 'NOTE'}: {path}: {e}")
                if gated:
                    unmeasurable.append(path)
                rows += 1
                continue
            for name, g in guard.items():
                verdict = ('PASS' if g['ok'] else 'FAIL') if gated else ('in band' if g['ok'] else 'out of band')
                print(f"MATERIAL {rel} {name} warmth {g['ours']:.4f} vs promo {g['promo']:.4f} band "
                      f"[{g['lo']:.4f}, {g['hi']:.4f}] {verdict}{tag}")
                if gated and not g["ok"]:
                    guard_fails.append(f"GUARD FAIL: {name} {g['ours']:.4f} vs [{g['lo']:.4f}, {g['hi']:.4f}] in {path}")
            ours, theirs = material_warmth(shot, MATERIAL_MASK), material_warmth(promo, MATERIAL_MASK)
            for name in INFO_MATERIALS:
                o, t = ours[name][1], theirs[name][1]
                print(f"MATERIAL {rel} {name} warmth {'-' if o is None else f'{o:.4f}'} vs promo "
                      f"{'-' if t is None else f'{t:.4f}'} (information only: not like-for-like)")
        rows += 1
    for line in guard_fails:
        print(line)
    if uniform:
        return 2
    if unmeasurable:
        return EXIT_UNMEASURABLE
    if rows == 0:
        print(f"no screenshot named after a pose ({', '.join(promos)}) under {a.shots}")
        return 1
    return EXIT_GUARD_FAIL if guard_fails else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
