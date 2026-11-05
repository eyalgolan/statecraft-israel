"""compose.py <S> <main-sha> <head-sha>: captioned #2649 stills, centre zooms of the air shots, main|head side-by-sides into out/."""
import re
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

S = Path(sys.argv[1]); SHA = {"main": sys.argv[2][:9], "head": sys.argv[3][:9]}
WHO = {"main": "origin/main", "head": "PR #2649 head"}
OUT = S / "out"; OUT.mkdir(exist_ok=True)
font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 24)
small = font.font_variant(size=20)
SHOT = re.compile(r"E21PLAY (en|he) SHOT (\S+) what=(.*?) t=(\S+) month=(\S+) date=(\S+) top=\[(.*?)\] d=(\d+) lookAt=\S+ \|\| (.*) \|\| shown=\[(.*)\]")

def parse(tag):
    d = {}
    p = S / f"logs-{tag}" / "lines.txt"
    if not p.exists(): return d
    for line in p.read_text(encoding="utf-8", errors="replace").splitlines():
        if m := SHOT.search(line):
            parts = m[9].split(" || ")
            d[(m[2], m[1])] = {"what": m[3], "t": float(m[4]), "date": m[6], "d": int(m[8]), "parts": parts, "shown": [s for s in m[10].split(" ; ") if s]}
    return d

def inframe(vp):
    x, y = (float(v) for v in vp.strip("()").split(","))
    return 0 <= x <= 1 and 0 <= y <= 1

def summary(g):
    dak, ftr, pools, other = [], [], [], []
    for p in g["parts"]:
        if p.startswith("DAK "):
            m = re.match(r"DAK (\S+) u=(\S+) vp=(\(\S+\)) plane=\[(.*)\]", p)
            nm = m[1].replace("tel-aviv-dakota-", "Dakota ")
            dak.append(f"{nm} {'hidden' if m[4] == 'hidden' else ('in frame' if inframe(m[3]) else 'off frame')}")
        elif p.startswith("FTR "):
            m = re.match(r"FTR (\S+) vp=(\(\S+\))", p); ftr.append(f"{m[1]} {'in frame' if inframe(m[2]) else 'off frame'}")
        elif p.startswith("POOL "):
            m = re.match(r"POOL (\S+)Renderers on=(\d+)", p); pools.append(f"{m[1].lower()} {m[2]}")
        elif p.startswith("CRASH "):
            m = re.match(r"CRASH on=(\S+) .*wingShown=(\S+)", p); other.append(f"crash blast {'ON' if m[1] == 'True' else 'off'}, wing {'drawn' if m[2] == 'True' else 'GONE'}")
        elif p.startswith("DEFENCE none"):
            other.append("no air defence")
    a = "; ".join(dak) or "no Dakotas"
    b = ("S-199s: " + "; ".join(ftr) + " | " + ", ".join(pools) + " | " + "; ".join(o for o in other if o != "no air defence")) if ftr else "; ".join(other)
    return a, b

def bar(img, lines):
    h = 12 + 30 * len(lines)
    c = Image.new("RGB", (img.width, img.height + h), "white"); d = ImageDraw.Draw(c)
    for i, t in enumerate(lines): d.text((12, 8 + 30 * i), t, fill="black", font=font if i == 0 else small)
    c.paste(img, (0, h)); return c

data = {t: parse(t) for t in ("main", "head")}
made = {}
for tag in ("main", "head"):
    for (name, lang), g in sorted(data[tag].items()):
        img = Image.open(S / f"raw-{tag}" / f"{name}-{lang}.png").convert("RGB")
        a, b = summary(g)
        day = "15 May 1948" if g["date"].endswith("05-15") else "15 July 1948"
        cap = [f"{WHO[tag]} {SHA[tag]} | {lang.upper()} | in play, {day} | loop t = {g['t']:.1f} s (lead; wing's own {g['t'] - 26.4:.1f}) | camera {g['d'] / 1000:.0f} km, 50 deg down",
               g["what"], f"{a} | {b}"]
        bar(img, cap).save(OUT / f"{tag}-{name}-{lang}.png", optimize=True)
        made[(tag, name, lang)] = (img, g, cap)
        if "-air-" in name and tag == "head":
            W, H = img.size
            crop = img.crop((W // 4, H // 4, 3 * W // 4, 3 * H // 4)).resize((W, H), Image.LANCZOS)
            bar(crop, [f"ZOOM x2 (centre) | {cap[0]}", cap[1], cap[2]]).save(OUT / f"{tag}-{name}-{lang}-zoom.png", optimize=True)

gap = 20
SBS = {"may-air-45km-t000.0", "may-tlv-31km-t000.0", "jul-air-45km-t000.0", "jul-tlv-31km-t000.0", "jul-air-45km-t038.7",
       "jul-air-45km-t045.0", "jul-air-45km-t057.0", "jul-air-45km-t060.0", "jul-air-45km-t107.0", "may-air-28km-t000.0", "jul-air-28km-t000.0", "jul-air-28km-t038.7", "jul-air-28km-t057.0"}
for (tag, name, lang), (a, g, cap) in list(made.items()):
    if tag != "main" or name not in SBS or ("head", name, lang) not in made: continue
    b = made[("head", name, lang)][0]
    a2 = a.resize((a.width // 2, a.height // 2), Image.LANCZOS); b2 = b.resize((b.width // 2, b.height // 2), Image.LANCZOS)
    c = Image.new("RGB", (a2.width + gap + b2.width, a2.height + 80), (40, 40, 40)); d = ImageDraw.Draw(c)
    day = "15 May 1948" if name.startswith("may") else "15 July 1948"
    d.text((12, 8), f"LEFT: origin/main {SHA['main']} (before) | {lang.upper()} | {day}, loop t = {g['t']:.1f} s | {name}", fill="white", font=small)
    d.text((a2.width + gap + 12, 8), f"RIGHT: PR #2649 head {SHA['head']} (after) | same date, clock and camera", fill="white", font=small)
    d.text((12, 40), f"main: {summary(g)[0]} {summary(g)[1]}"[:110], fill="white", font=small)
    d.text((a2.width + gap + 12, 40), f"head: {summary(made[('head', name, lang)][1])[0]}"[:110], fill="white", font=small)
    c.paste(a2, (0, 80)); c.paste(b2, (a2.width + gap, 80))
    c.save(OUT / f"sbs-{name}-{lang}.png", optimize=True)

# pixel diff for 15 May main vs head
from PIL import ImageChops
for name in ("may-air-45km-t000.0", "may-air-28km-t000.0", "may-tlv-31km-t000.0"):
    k1, k2 = ("main", name, "en"), ("head", name, "en")
    if k1 in made and k2 in made:
        df = ImageChops.difference(made[k1][0], made[k2][0]).convert("L")
        hist = df.histogram(); n = sum(hist); over = sum(hist[16:])
        print(f"PIXDIFF {name}: max={max(i for i, v in enumerate(hist) if v)} pixels>15={over} of {n} ({100 * over / n:.3f}%) bbox={df.point(lambda v: 255 if v > 15 else 0).getbbox()}")
print("\n".join(sorted(p.name for p in OUT.glob("*.png"))))
