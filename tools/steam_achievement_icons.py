# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow==12.3.0"]
# ///
"""Steam achievement icons (#2156): the five icons the owner chose on 1 November 2026 ("All A (Recommended)"), each
as an unlocked colour PNG and a greyed locked PNG, 256x256, in docs/steam/achievements/.

Every shape is drawn here with Pillow primitives: no font, no photograph, no 3D model, no AI image, no flag. The
colours are the game's UI tokens (unity/Assets/UI/Statecraft.uss:5-17, CC BY 4.0, LICENSING.md). Each icon is drawn
at 1024x1024 and downsampled for smooth edges. The locked icon is the unlocked one in greyscale, darkened and
flattened. The API names are those in docs/steam/README.md, "Achievements".

Usage: uv run tools/steam_achievement_icons.py [OUT_DIR]   (default docs/steam/achievements)
"""
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageOps

S = 1024  # working size; the icons are 256x256
NAVY_DEEP = (9, 14, 26)
NAVY_SOLID = (14, 22, 38)
NAVY_RAISED = (20, 31, 52)
GOLD = (196, 160, 92)
GOLD_LIGHT = (232, 204, 142)
INK = (236, 228, 210)
INK_DIM = (170, 164, 150)
FOCUS = (120, 98, 58)


def frame():
    im = Image.new("RGB", (S, S), NAVY_DEEP)
    d = ImageDraw.Draw(im)
    # raised medallion with a double gold ring
    d.ellipse((40, 40, S - 40, S - 40), fill=NAVY_RAISED)
    d.ellipse((40, 40, S - 40, S - 40), outline=GOLD, width=28)
    d.ellipse((96, 96, S - 96, S - 96), outline=GOLD_LIGHT, width=8)
    return im, d


def poly_line(d, pts, fill, width):
    d.line(pts, fill=fill, width=width, joint="curve")
    r = width // 2
    for x, y in (pts[0], pts[-1]):
        d.ellipse((x - r, y - r, x + r, y + r), fill=fill)


def railway_a():
    """Track in perspective running to the horizon."""
    im, d = frame()
    hz = 330  # horizon
    vx = S // 2
    # sleepers: spaced by perspective
    for i in range(1, 12):
        t = (i / 11) ** 1.8
        y = hz + 40 + t * 520
        half = 30 + t * 270
        th = 6 + t * 26
        d.rectangle((vx - half, y - th / 2, vx + half, y + th / 2), fill=FOCUS)
    # rails converging to the vanishing point
    for side in (-1, 1):
        d.line([(vx + side * 14, hz), (vx + side * 230, 900)], fill=GOLD_LIGHT, width=30)
    # cut off below the medallion's inner ring with a navy mask outside a circle
    return clip(im)


def burma_a():
    """A switchback road climbing a ridge."""
    im, d = frame()
    d.polygon([(110, 820), (330, 380), (470, 520), (640, 280), (920, 820)], fill=NAVY_SOLID)
    d.polygon([(330, 380), (380, 470), (300, 520)], fill=FOCUS)
    d.polygon([(640, 280), (700, 390), (600, 420)], fill=FOCUS)
    road = [(240, 900), (360, 790), (660, 760), (720, 700), (400, 650), (360, 590), (600, 560), (650, 500), (560, 420)]
    poly_line(d, road, GOLD_LIGHT, 46)
    # dashed centre line
    dash_line(d, road, NAVY_RAISED, 10, 26, 22)
    return clip(im)


def dash_line(d, pts, fill, width, dash, gap):
    carry = 0.0
    on = True
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        seg = math.hypot(x1 - x0, y1 - y0)
        pos = 0.0
        while pos < seg:
            step = min((dash if on else gap) - carry, seg - pos)
            if on:
                t0, t1 = pos / seg, (pos + step) / seg
                d.line([(x0 + (x1 - x0) * t0, y0 + (y1 - y0) * t0), (x0 + (x1 - x0) * t1, y0 + (y1 - y0) * t1)], fill=fill, width=width)
            pos += step
            carry += step
            if carry >= (dash if on else gap) - 1e-6:
                carry = 0.0
                on = not on


def house(d, cx, cy, w, fill, roof):
    h = w * 0.7
    d.rectangle((cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2), fill=fill)
    d.polygon([(cx - w / 2 - w * 0.12, cy - h / 2), (cx, cy - h / 2 - w * 0.55), (cx + w / 2 + w * 0.12, cy - h / 2)], fill=roof)
    d.rectangle((cx - w * 0.12, cy, cx + w * 0.12, cy + h / 2), fill=NAVY_RAISED)


def yishuvim_a():
    """Six small houses in two rows of three."""
    im, d = frame()
    for row, y in enumerate((440, 690)):
        for col, x in enumerate((300, 512, 724)):
            house(d, x, y, 150, INK, GOLD_LIGHT)
    d.line([(190, 800), (834, 800)], fill=GOLD, width=14)
    return clip(im)


def immigrants_a():
    """A passenger ship on the waves (no flag, no marking)."""
    im, d = frame()
    # hull
    d.polygon([(200, 560), (830, 560), (760, 690), (270, 690)], fill=INK)
    # superstructure and funnel
    d.rectangle((330, 470, 690, 560), fill=INK_DIM)
    for x in range(370, 680, 70):
        d.ellipse((x, 498, x + 30, 528), fill=NAVY_RAISED)
    d.rectangle((560, 370, 640, 470), fill=GOLD_LIGHT)
    d.rectangle((560, 370, 640, 400), fill=NAVY_SOLID)
    # waves
    for i, y in enumerate((730, 800)):
        pts = [(x, y + 18 * math.sin((x + i * 60) / 45)) for x in range(160, 870, 8)]
        d.line(pts, fill=GOLD, width=16, joint="curve")
    return clip(im)


def done_a():
    """A calendar page with a check mark."""
    im, d = frame()
    d.rounded_rectangle((270, 270, 754, 760), radius=36, fill=INK)
    d.rounded_rectangle((270, 270, 754, 380), radius=36, fill=GOLD)
    d.rectangle((270, 340, 754, 380), fill=GOLD)
    for x in (360, 664):
        d.rounded_rectangle((x - 18, 220, x + 18, 320), radius=16, fill=GOLD_LIGHT)
    # grid of small day squares
    for r in range(3):
        for c in range(5):
            x, y = 315 + c * 84, 420 + r * 96
            d.rectangle((x, y, x + 54, y + 54), fill=(205, 198, 182))
    # check mark over the grid
    poly_line(d, [(370, 560), (480, 670), (690, 430)], NAVY_DEEP, 90)
    poly_line(d, [(370, 560), (480, 670), (690, 430)], FOCUS, 56)
    return clip(im)


def clip(im):
    """Keep the motif inside the medallion's inner ring and redraw the rings on top."""
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).ellipse((104, 104, S - 104, S - 104), fill=255)
    base, d = frame()
    base.paste(im, (0, 0), mask)
    d = ImageDraw.Draw(base)
    d.ellipse((40, 40, S - 40, S - 40), outline=GOLD, width=28)
    d.ellipse((96, 96, S - 96, S - 96), outline=GOLD_LIGHT, width=8)
    return base


def locked(im):
    g = ImageOps.grayscale(im)
    g = ImageEnhance.Contrast(g).enhance(0.55)
    g = ImageEnhance.Brightness(g).enhance(0.62)
    return g.convert("RGB")


ICONS = {
    "OBJ_RAILWAY": railway_a,
    "OBJ_BURMA_ROAD": burma_a,
    "OBJ_YISHUVIM": yishuvim_a,
    "OBJ_ABSORB_IMMIGRANTS": immigrants_a,
    "CAMPAIGN_DONE": done_a,
}


def main(argv: list[str]) -> None:
    out = Path(argv[1]) if len(argv) > 1 else Path(__file__).resolve().parents[1] / "docs" / "steam" / "achievements"
    out.mkdir(parents=True, exist_ok=True)
    for api, draw in ICONS.items():
        unlocked = draw().resize((256, 256), Image.LANCZOS)
        unlocked.save(out / f"{api}_unlocked.png", optimize=True)
        locked(unlocked).save(out / f"{api}_locked.png", optimize=True)
        print(f"{api}: {out / (api + '_unlocked.png')}, {out / (api + '_locked.png')}")


if __name__ == "__main__":
    main(sys.argv)
