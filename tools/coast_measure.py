# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow==12.3.0"]
# ///
"""Beach width, sand lightness and foam lace across a west-facing coast, per image row, in 1920-wide frame pixels
(issue #388, sea round 3: the plan's section 1 measured promo 03 and the gameplay03 shot with this).

Usage: uv run tools/coast_measure.py IMAGE [IMAGE ...] (--window south|north | --box Y0 Y1 X0 X1) [--promo] [--step N]
       [--check] [--rows]
Each image is scaled to 1920 wide first (box filter); Y/X are in that 1920 frame. --window takes the gameplay03 pose's
window (WINDOWS), or with --promo the same stretch of coast in promo 03 (PROMO_WINDOWS). With several images (three ?shot
captures: the surf moves and ?shot does not pin the water's clock) each metric is the median over them. --check compares
those medians with BANDS (set from promo 03 before any change) and exits 1 when one is outside.

Classes (sRGB 0-255): water: b > r + 15 (docs/visual-v1.md promo-sea's sea pixel); foam: luma >= 0.72 and max-min <= 30;
sand: luma >= 0.60 and r - b >= 25; crown: any other pixel under luma 0.40 (a tree crown or its shadow). Per row: the
shore is where the sea ends, the first run of 3 sand or crown pixels after >= 6 water/foam pixels: a crown stands on
land, so one over the waterline (main's citrus south of Jaffa) marks the shore as sand would, instead of moving it to
the sand past the crown. The beach is the sand run from the first 3 sand pixels at or after the shore, bridging gaps <=
12 px (a border band, a pebble); the foam band is the seaward stretch [shore-80, shore) from the farthest foam pixel in
it; lace lines are foam runs >= 2 px in that band; stripe_rows is the share of rows with 3 or more of them (#690: evenly
spaced white bands read as combed stripes; promo 03's frontage reads 0.05). Widths are corrected to across the coast
with a least-squares fit of the shore's slope over the rows."""
import argparse
import statistics as st
import sys
from pathlib import Path

from PIL import Image

FRAME_W = 1920
SEA_RUN, SAND_RUN, GAP_PX, FOAM_REACH_PX = 6, 3, 12, 80
# The gameplay03 pose's windows (y0, y1, x0, x1) and the same coast in promo 03 at 1920 wide.
WINDOWS = {"south": (690, 880, 380, 760), "north": (110, 330, 600, 900)}
PROMO_WINDOWS = {"south": (748, 862, 300, 700), "north": (80, 300, 560, 900)}
# The bands, from promo 03 (plan C1 section 1): (low, high), either None for open. Promo 03 itself measures south: beach
# 84, foam 42, 2 lines, share 0.56, sand p50 0.74 and p90 0.86; north: beach 24.3, foam 12.5. The north window is a guard:
# promo 03's northern frontage is narrow.
BANDS = {
    "south": {"beach_px": (40, 95), "foam_px": (25, 55), "lace_lines": (2, None), "foam_share": (0.35, 0.75),
              "sand_luma_p50": (0.72, 0.80), "sand_luma_p90": (0.82, None)},
    "north": {"beach_px": (15, 40), "foam_px": (6, 30)},
}


def load(path: Path) -> Image.Image:
    with Image.open(path) as im:
        im = im.convert("RGB")
        if im.width != FRAME_W:
            im = im.resize((FRAME_W, max(1, round(im.height * FRAME_W / im.width / 2) * 2)), Image.Resampling.BOX)
        return im


def luma(r: int, g: int, b: int) -> float:
    return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255


def classify(r: int, g: int, b: int) -> str:
    lum = luma(r, g, b)
    if lum >= 0.72 and max(r, g, b) - min(r, g, b) <= 30:
        return "F"
    if b > r + 15:
        return "W"
    if lum >= 0.60 and r - b >= 25:
        return "S"
    return "C" if lum < 0.40 else "L"


def quantile(v: list[float], p: float) -> float:
    s = sorted(v)
    return s[min(len(s) - 1, int(p * len(s)))]


def measure_row(px: list[tuple[int, int, int]]) -> tuple[int, int, list[float], int, int, float] | None:
    """(shore index, beach px, sand lumas, foam px, lace lines, foam share) of one row, or None with no shore."""
    c = [classify(*p) for p in px]
    seen, shore = 0, None
    for i in range(len(c) - SAND_RUN):
        if c[i] in "WF":
            seen += 1
        if seen >= SEA_RUN and all(v in "SC" for v in c[i:i + SAND_RUN]):
            shore = i
            break
    if shore is None:
        return None
    sand = next((i for i in range(shore, len(c) - SAND_RUN + 1) if c[i:i + SAND_RUN] == ["S"] * SAND_RUN), None)
    if sand is None:
        return None
    j, end, gap = sand, sand, 0
    while j < len(c):
        if c[j] == "S":
            end, gap = j, 0
        else:
            gap += 1
            if gap > GAP_PX:
                break
        j += 1
    lums = [luma(*px[k]) for k in range(sand, end + 1) if c[k] == "S"]
    band = c[max(0, shore - FOAM_REACH_PX):shore]
    far = next((k for k, v in enumerate(band) if v == "F"), None)
    seg = band[far:] if far is not None else []
    lines, run = 0, 0
    for v in seg + ["W"]:
        if v == "F":
            run += 1
        else:
            lines += run >= 2
            run = 0
    return shore, end - sand + 1, lums, len(seg), lines, (seg.count("F") / len(seg)) if seg else 0.0


def measure(im: Image.Image, y0: int, y1: int, x0: int, x1: int, step: int = 2) -> dict:
    """The window's per-row numbers and their summary (widths across the coast)."""
    data = im.load()
    rows, pool = [], []
    for y in range(y0, y1, step):
        r = measure_row([data[x, y] for x in range(x0, x1)])
        if r is None:
            continue
        shore, beach, lums, foam, lines, share = r
        pool.extend(lums)
        rows.append({"y": y, "shore_x": x0 + shore, "beach_px": beach, "sand_luma": st.median(lums) if lums else 0.0,
                     "foam_px": foam, "lace_lines": lines, "foam_share": share})
    if not rows:
        return {"rows": []}
    ys, xs = [r["y"] for r in rows], [r["shore_x"] for r in rows]
    my, mx = st.mean(ys), st.mean(xs)
    k = sum((y - my) * (x - mx) for y, x in zip(ys, xs)) / max(sum((y - my) ** 2 for y in ys), 1e-9)
    f = 1 / (1 + k * k) ** 0.5
    return {"rows": rows, "slope_k": k, "perp_factor": f,
            "beach_px": st.median(r["beach_px"] * f for r in rows),
            "beach_p25": quantile([r["beach_px"] * f for r in rows], 0.25),
            "beach_p75": quantile([r["beach_px"] * f for r in rows], 0.75),
            "foam_px": st.median(r["foam_px"] * f for r in rows),
            "lace_lines": st.median(r["lace_lines"] for r in rows),
            "foam_share": st.median(r["foam_share"] for r in rows),
            "stripe_rows": sum(r["lace_lines"] >= 3 for r in rows) / len(rows),
            "sand_luma_p50": quantile(pool, 0.5) if pool else 0.0,
            "sand_luma_p90": quantile(pool, 0.9) if pool else 0.0}


METRICS = ("beach_px", "foam_px", "lace_lines", "foam_share", "stripe_rows", "sand_luma_p50", "sand_luma_p90")


def combine(summaries: list[dict]) -> dict:
    """Each metric's median over the shots that found a shore."""
    found = [s for s in summaries if s["rows"]]
    return {m: st.median(s[m] for s in found) for m in METRICS} if found else {}


def check(summary: dict, bands: dict) -> list[str]:
    """The metrics outside their band (all of them when nothing was measured)."""
    out = []
    for m, (lo, hi) in bands.items():
        v = summary.get(m)
        if v is None or (lo is not None and v < lo) or (hi is not None and v > hi):
            out.append(f"{m} {v if v is None else round(v, 3)} outside [{lo}, {hi}]")
    return out


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("images", nargs="+", type=Path)
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--window", choices=sorted(WINDOWS))
    g.add_argument("--box", nargs=4, type=int, metavar=("Y0", "Y1", "X0", "X1"))
    ap.add_argument("--promo", action="store_true", help="the window's stretch of coast in promo 03, not gameplay03")
    ap.add_argument("--step", type=int, default=2)
    ap.add_argument("--check", action="store_true", help="exit 1 when a median is outside the window's band")
    ap.add_argument("--rows", action="store_true", help="print every row")
    a = ap.parse_args(argv)
    box = tuple(a.box) if a.box else (PROMO_WINDOWS if a.promo else WINDOWS)[a.window]
    sums = []
    for path in a.images:
        s = measure(load(path), *box, step=a.step)
        sums.append(s)
        if not s["rows"]:
            print(f"{path}: no shore in {box}")
            continue
        if a.rows:
            for r in s["rows"]:
                print("  y=%(y)d shore_x=%(shore_x)d beach_px=%(beach_px)d sand_luma=%(sand_luma).2f foam_px=%(foam_px)d "
                      "lace_lines=%(lace_lines)d foam_share=%(foam_share).2f" % r)
        print(f"{path}: rows={len(s['rows'])} perp_factor={s['perp_factor']:.2f} beach_px={s['beach_px']:.1f} "
              f"(p25 {s['beach_p25']:.1f}, p75 {s['beach_p75']:.1f}) foam_px={s['foam_px']:.1f} "
              f"lace_lines={s['lace_lines']} foam_share={s['foam_share']:.2f} stripe_rows={s['stripe_rows']:.2f} "
              f"sand_luma p50={s['sand_luma_p50']:.2f} p90={s['sand_luma_p90']:.2f}")
    med = combine(sums)
    print("MEDIAN " + " ".join(f"{m}={v:.2f}" for m, v in med.items()) if med else "MEDIAN no shore")
    if not a.check:
        return 0 if med else 1
    if not a.window:
        ap.error("--check needs --window")
    bad = check(med, BANDS[a.window])
    for b in bad:
        print("OUTSIDE " + b)
    print("BANDS " + ("PASS" if not bad else "FAIL"))
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
