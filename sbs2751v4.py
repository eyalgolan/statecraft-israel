"""sbs2751v4.py <S> <dest> <leftTag> <rightTag> <leftLabel> <rightLabel> (sbs2751.py with tags/labels as args): BEFORE | AFTER strips for #2751 (parked 101 Squadron Spitfire), outlines from the rig's logged rects.
Outlines: orange = the parked footprint (where trees and houses must stay off), cyan = the badge, magenta = the tail (rear 40%).
Pairs: before/<shot> with after/<shot> (camera on the plane) and before/sqn101-parked-* with after/sqn101-fixed-* (same world view)."""
import re
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

S = Path(sys.argv[1]); D = Path(sys.argv[2]); LT, RT, LL, RL = sys.argv[3:7]; D.mkdir(parents=True, exist_ok=True)
BIG = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 40)
MID = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 30)
SM = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 24)
SHOT = re.compile(r"E21PLAY (en|he) SHOT (\S+) what=(.*?) date=(\S+) top=\[(.*?)\] d=(\d+) depth=(-?\d+) lookAt=\S+ rect=\[(.*?)\] badge=\[(.*?)\] badgePos=\S+ badgeSize=\S+ tail=\[(.*?)\] foot=\[(.*?)\] span=\[(.*?)\]")
OUTL = [("foot", (255, 140, 0), "tree hole (plane's footprint)"), ("span", (255, 40, 40), "house clearance (11.23 m span x420)"), ("badge", (0, 220, 255), "badge"), ("tail", (255, 0, 200), "tail (rear 40%)")]


def shots(tag):
    out = {}
    for line in (S / f"logs-{tag}" / "lines.txt").read_text(encoding="utf-8", errors="replace").splitlines():
        m = SHOT.search(line)
        if m:
            lang, name, what, date, top, d, depth, rect, badge, tail, foot, span = m.groups()
            out[(name, lang)] = dict(what=what, date=date, d=int(d), depth=int(depth), foot=foot, span=span, badge=badge, tail=tail)
    return out


def box(s):
    if s in ("none", "behind", ""): return None
    return [float(v) for v in s.split(",")]


def panel(img, info):
    img = img.copy(); d = ImageDraw.Draw(img); off = 0
    for key, col, label in OUTL:
        b = box(info[key])
        if not b: continue
        if b[3] < 0 or b[1] > img.height or b[2] < 0 or b[0] > img.width:
            side = "above" if b[3] < 0 else "below" if b[1] > img.height else "left of" if b[2] < 0 else "right of"
            note = f"{label}: {side} the frame (not drawn)"
            d.text((img.width / 2 - d.textlength(note, font=MID) / 2, 120 + 40 * off), note, fill=col, font=MID, stroke_width=3, stroke_fill="black")
            off += 1; continue
        x0, y0, x1, y1 = (max(-5, min(v, lim + 5)) for v, lim in zip(b, (img.width, img.height) * 2))
        if key in ("foot", "span"): d.ellipse((x0, y0, x1, y1), outline=col, width=5)
        else: d.rectangle((x0, y0, x1, y1), outline=col, width=4)
        tw = d.textlength(label, font=MID)
        if key == "foot": tx, ty = (x0 + x1 - tw) / 2, y1 + 6
        elif key == "span": tx, ty = (x0 + x1 - tw) / 2, y1 - 42
        elif key == "tail": tx, ty = x0 + 8, y0 + 8
        else: tx, ty = x1 + 10, max(y0, 80)
        tx = min(max(tx, 8), img.width - tw - 8); ty = min(max(ty, 80), img.height - 40)
        d.text((tx, ty), label, fill=col, font=MID, stroke_width=3, stroke_fill="black")
    return img


B, A = shots(LT), shots(RT)
pairs = []
for (name, lang) in sorted(A):
    bname = name.replace("sqn101-fixed-", "sqn101-parked-") if LT == "before" else name
    if (bname, lang) in B: pairs.append((bname, name, lang))
for bname, aname, lang in pairs:
    bi, ai = B[(bname, lang)], A[(aname, lang)]
    ib = panel(Image.open(S / f"raw-{LT}" / f"{bname}-{lang}.png").convert("RGB"), bi)
    ia = panel(Image.open(S / f"raw-{RT}" / f"{aname}-{lang}.png").convert("RGB"), ai)
    gap, head = 16, 150
    c = Image.new("RGB", (ib.width + ia.width + gap, ib.height + head), "white"); d = ImageDraw.Draw(c)
    view = "camera on the plane" if aname == bname else ("SAME world view (camera on the BEFORE plane centre)" if LT == "before" else "both cameras on the BEFORE plane centre")
    d.text((12, 8), LL, fill=(160, 0, 0), font=BIG)
    d.text((ib.width + gap + 12, 8), RL, fill=(0, 110, 0), font=BIG)
    for x, info in ((12, bi), (ib.width + gap + 12, ai)):
        d.text((x, 58), f"{lang.upper()} | 101 Squadron Spitfire, parked (rung 1) | {info['date']} | camera {info['d'] / 1000:.1f} km, 50 deg down | {view}", fill="black", font=SM)
    d.text((12, 96), "Outlines (rig-logged geometry):", fill="black", font=SM)
    x = 360
    for key, col, label in OUTL:
        d.rectangle((x, 100, x + 26, 122), outline=col, width=4); d.text((x + 34, 96), label, fill="black", font=SM); x += 60 + int(d.textlength(label, font=SM))
    c.paste(ib, (0, head)); c.paste(ia, (ib.width + gap, head))
    c = c.resize((c.width * 2 // 3, c.height * 2 // 3), Image.LANCZOS)
    dest = D / f"{aname}-{lang}-sbs.png"; c.save(dest, optimize=True); print(dest.name)
