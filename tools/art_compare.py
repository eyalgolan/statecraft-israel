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
images; pass mask=() for the whole frame. The numbers inform the owner's review. Exit code 2 when a
screenshot is near-uniform (luma std < 0.01: a black or blank WebGL capture), 1 when no screenshot matched a pose."""
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
Mask = tuple[tuple[float, float, float, float], ...]
# Screen regions the guards leave out of both the screenshot and the promo, as (x0, y0, x1, y1) fractions of the frame:
# the HUD covers the same places in both, and promo 03's HUD cannot be hidden. Measured on 1920x1080 ?shot captures
# after #624 and on promo 03 (2752x1536).
HUD_MASK: Mask = (
    (0.0, 0.0, 1.0, 0.07),     # the top bar: ours 6.4% of the height, promo 03's 6.5%
    (0.0, 0.0, 0.13, 0.10),    # the date plate hanging below it: ours 12.4% x 9.1%, promo 03's 12% x 8.6%
    (0.0, 0.10, 0.045, 0.44),  # the left column of round buttons: ours (four) to 34% of the height, promo 03's (five) to 43%
)
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
    outside [0, 1] or inside out, and when the mask leaves no pixel: a guard must not pass on an empty region."""
    k = np.ones((height, width), dtype=bool)
    for x0, y0, x1, y1 in mask:
        if not (0 <= x0 < x1 <= 1 and 0 <= y0 < y1 <= 1):
            raise ValueError(f"mask box {(x0, y0, x1, y1)} is not inside the frame")
        k[max(0, int(np.floor(y0 * height)) - pad[1]):int(np.ceil(y1 * height)) + pad[1],
          max(0, int(np.floor(x0 * width)) - pad[0]):int(np.ceil(x1 * width)) + pad[0]] = False
    if not k.any():
        raise ValueError(f"mask {mask} leaves no pixel of a {width}x{height} frame")
    return k


def metrics(img: Image.Image, mask: Mask = HUD_MASK) -> dict[str, float]:
    rgb = img.convert("RGB")
    k = keep(rgb.width, rgb.height, mask)
    a = (np.asarray(rgb, dtype=np.float32) / 255.0)[k]
    s = (np.asarray(rgb.convert("HSV"), dtype=np.float32)[..., 1] / 255.0)[k]
    luma = 0.2126 * a[:, 0] + 0.7152 * a[:, 1] + 0.0722 * a[:, 2]
    return {"saturation": float(s.mean()), "contrast": float(luma.std()), "warmth": float((a[:, 0] - a[:, 2]).mean())}


def histogram_distance(a: Image.Image, b: Image.Image, bins: int = 8, mask: Mask = HUD_MASK) -> float:
    def hist(img: Image.Image) -> np.ndarray:
        px = np.asarray(img.convert("RGB")).copy()
        px[~keep(px.shape[1], px.shape[0], mask)] = 0             # the masked pixels cannot reach the resize below
        # and the 256x256 pixels the resize blended from them are dropped too (bicubic reaches 2 pixels of the
        # smaller grid: 2 here when shrinking, 2 * 256 / side when enlarging)
        pad = tuple(int(np.ceil(2 * max(1, 256 / side))) + 1 for side in (px.shape[1], px.shape[0]))
        small = np.asarray(Image.fromarray(px, "RGB").resize((256, 256)), dtype=np.int64)
        x = (small[keep(256, 256, mask, pad)] // (256 // bins)).reshape(-1, 3)
        h = np.bincount(x[:, 0] * bins * bins + x[:, 1] * bins + x[:, 2], minlength=bins ** 3).astype(np.float64)
        return h / h.sum()
    return float(0.5 * np.abs(hist(a) - hist(b)).sum())


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
    rows, uniform = 0, []
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
        title = (f"{a.name} | {path.stem} (left) vs {promos[pose].name} (right) | saturation {m['saturation']:.2f}/{p['saturation']:.2f}"
                 f"  contrast {m['contrast']:.3f}/{p['contrast']:.3f}  warmth {m['warmth']:.3f}/{p['warmth']:.3f}  palette distance {d:.2f}")
        pair = compose(shot, promo, title)
        pair.save(path.with_name(path.stem + "-compare.png"))
        for kind in MACHADO:
            simulate_cvd(pair, kind).save(path.with_name(f"{path.stem}-compare-{kind}.png"))
        with log.open("a") as f:
            f.write("\t".join([a.name, branch, commit, str(path.resolve()), f"{m['saturation']:.4f}", f"{m['contrast']:.4f}", f"{m['warmth']:.4f}", f"{d:.4f}"]) + "\n")
        print("ART " + title)
        rows += 1
    if uniform:
        return 2
    if rows == 0:
        print(f"no screenshot named after a pose ({', '.join(promos)}) under {a.shots}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
