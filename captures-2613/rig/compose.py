"""compose.py <S> <main-sha> <head-sha>: labelled #2613 Dakota stills, zooms, main|head side-by-sides and timed strips into out/."""
import re
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

S = Path(sys.argv[1]); SHA = {"main": sys.argv[2][:9], "head": sys.argv[3][:9]}
WHO = {"main": "origin/main", "head": "PR #2613 head"}
OUT = S / "out"; OUT.mkdir(exist_ok=True)
font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 24)
small = font.font_variant(size=20)
SHOT = re.compile(r"E21PLAY (en|he) SHOT (\S+) what=(.*?) clock=(\S+) d=(\d+) .*? \|\| (.*) \|\| shown=\[(.*)\]")
PLANE = re.compile(r"(\S+) u=(\S+) pos=\S+ vp=\((\S+),(\S+)\) keep=\[([^\]]*)\] keepSize=(\S+) plane=\[([^\]]*)\] flag=\[([^\]]*)\]")
COL = {"keep": (255, 220, 0), "plane": (0, 230, 255), "flag": (255, 0, 200)}

def parse(tag):
    d = {}
    for line in (S / f"logs-{tag}" / "lines.txt").read_text(encoding="utf-8", errors="replace").splitlines():
        if m := SHOT.search(line):
            planes = [PLANE.search(p) for p in m[6].split(" || ")]
            d[(m[2], m[1])] = {"what": m[3], "clock": float(m[4]), "d": int(m[5]), "planes": [p for p in planes if p], "shown": [s for s in m[7].split(" ; ") if s]}
    return d

def rect(s):
    try:
        v = [float(x) for x in s.split(",")]
        return v if len(v) == 4 else None
    except ValueError:
        return None

def bar(img, lines):
    h = 12 + 30 * len(lines)
    c = Image.new("RGB", (img.width, img.height + h), "white"); d = ImageDraw.Draw(c)
    for i, t in enumerate(lines): d.text((12, 8 + 30 * i), t, fill="black", font=font if i == 0 else small)
    c.paste(img, (0, h)); return c

def overlays(name):
    return name.startswith(("plane-", "wide-", "opening-"))

data = {t: parse(t) for t in ("main", "head")}
diff = []
made = {}
for tag in ("main", "head"):
    for (name, lang), g in sorted(data[tag].items()):
        img = Image.open(S / f"raw-{tag}" / f"{name}-{lang}.png").convert("RGB")
        clean = img.copy()
        d = ImageDraw.Draw(img)
        inframe = []
        for p in g["planes"]:
            vx, vy = float(p[3]), float(p[4]); inf = 0 <= vx <= 1 and 0 <= vy <= 1
            inframe.append(f"{p[1].replace('tel-aviv-dakota-', '')} loop u={float(p[2]) % 120:.1f} s {'IN frame' if inf else 'off frame'}")
            if not overlays(name): continue
            for key, idx in (("keep", 5), ("plane", 7), ("flag", 8)):
                r = rect(p[idx])
                if r: d.rectangle((r[0], r[1], r[2], r[3]), outline=COL[key], width=3 if key == "keep" else 2)
        cap = [f"{WHO[tag]} {SHA[tag]} | {lang.upper()} | {name} | in play, 15 May 1948, clock {g['clock']:.1f} s | camera {g['d'] / 1000:.0f} km, 50 deg down",
               f"{g['what']} | Dakotas: {'; '.join(inframe)}",
               f"Names shown: {len(g['shown'])}" + (" | yellow = Dakota keep-clear box (screen), cyan = aircraft, magenta = flag" if overlays(name) else "")]
        bar(img, cap).save(OUT / f"{tag}-{name}-{lang}.png", optimize=True)
        made[(tag, name, lang)] = (img, clean, g, cap)

# zooms
def zoom_box(name, g, img):
    W, H = img.size
    if name.startswith("plane-"):
        rs = [rect(p[5]) for p in g["planes"][:1]] + [rect(p[7]) for p in g["planes"][:1]]
        rs = [r for r in rs if r]
        if rs:
            x0 = min(r[0] for r in rs); y0 = min(r[1] for r in rs); x1 = max(r[2] for r in rs); y1 = max(r[3] for r in rs)
            cx, cy = (x0 + x1) / 2, (y0 + y1) / 2; w = max(640, (x1 - x0) * 1.6); h = w * 9 / 16
            h = max(h, (y1 - y0) * 1.4); w = h * 16 / 9
            return (max(0, cx - w / 2), max(0, cy - h / 2), min(W, cx + w / 2), min(H, cy + h / 2))
    if name.startswith(("wide-", "opening-")):
        return (W * 0.2, 0, W * 0.8, H * 0.6)
    return (W * 0.25, H * 0.2, W * 0.75, H * 0.8)

for (tag, name, lang), (img, clean, g, cap) in list(made.items()):
    if name.startswith("seq"): continue
    box = zoom_box(name, g, img)
    src = img if overlays(name) else clean
    crop = src.crop(tuple(round(v) for v in box))
    z = 1920 / crop.width
    crop = crop.resize((1920, round(crop.height * z)), Image.LANCZOS)
    zi = bar(crop, [f"ZOOM x{z:.1f} | {WHO[tag]} {SHA[tag]} | {lang.upper()} | {name}", cap[1]])
    zi.save(OUT / f"{tag}-{name}-{lang}-zoom.png", optimize=True)
    made[(tag, name, lang)] = (img, clean, g, cap, zi)

# side by sides (main left / head right at half size; zoom main top / head bottom)
gap = 20
for (tag, name, lang), v in list(made.items()):
    if tag != "main" or ("head", name, lang) not in made: continue
    a, b = v[0], made[("head", name, lang)][0]
    a = a.resize((a.width // 2, a.height // 2), Image.LANCZOS); b = b.resize((b.width // 2, b.height // 2), Image.LANCZOS)
    c = Image.new("RGB", (a.width + gap + b.width, a.height + 80), (40, 40, 40)); d = ImageDraw.Draw(c)
    d.text((12, 8), f"LEFT: origin/main {SHA['main']} (before) | {lang.upper()} {name} | clock {v[2]['clock']:.1f} s", fill="white", font=small)
    d.text((a.width + gap + 12, 8), f"RIGHT: PR #2613 head {SHA['head']} (after) | same clock", fill="white", font=small)
    d.text((12, 40), f"shown names: main {len(v[2]['shown'])}, head {len(made[('head', name, lang)][2]['shown'])}", fill="white", font=small)
    c.paste(a, (0, 80)); c.paste(b, (a.width + gap, 80))
    c.save(OUT / f"sbs-{name}-{lang}.png", optimize=True)
    if len(v) > 4 and len(made[("head", name, lang)]) > 4:
        za, zb = v[4], made[("head", name, lang)][4]
        c = Image.new("RGB", (max(za.width, zb.width), za.height + gap + zb.height + 50), (40, 40, 40)); d = ImageDraw.Draw(c)
        d.text((12, 10), f"TOP: origin/main {SHA['main']} (before)   |   BOTTOM: PR #2613 head {SHA['head']} (after)   |   {lang.upper()} {name}", fill="white", font=font)
        c.paste(za, (0, 50)); c.paste(zb, (0, 50 + za.height + gap))
        c.save(OUT / f"sbs-zoom-{name}-{lang}.png", optimize=True)
    sm = set(v[2]["shown"]); sh = set(made[("head", name, lang)][2]["shown"])
    diff.append(f"{lang} {name}: main {len(sm)}, head {len(sh)} | only main: {sorted(sm - sh)} | only head: {sorted(sh - sm)}")

# timed strips: rows main/head, columns = clock times (centre crops)
for series in ("close-20km", "column-9km", "growth-20km"):
    for lang in ("en",):
        names = sorted({n for (t, n, l) in made if n.startswith(series) and l == lang})
        if not names or not all(("main", n, lang) in made and ("head", n, lang) in made for n in names): continue
        tiles = {}
        for t in ("main", "head"):
            for n in names:
                cl = made[(t, n, lang)][1]; W, H = cl.size
                tile = cl.crop((round(W * 0.2), round(H * 0.1), round(W * 0.8), round(H * 0.9))).resize((576, 432), Image.LANCZOS)
                tiles[(t, n)] = tile
        tw, th = 576, 432
        c = Image.new("RGB", (190 + len(names) * (tw + 8), 50 + 2 * (th + 40)), (40, 40, 40)); d = ImageDraw.Draw(c)
        d.text((12, 10), f"{series} {lang.upper()} timed frames (centre crop 60% x 80%) | top: origin/main {SHA['main']}, bottom: PR #2613 head {SHA['head']}", fill="white", font=small)
        for j, n in enumerate(names):
            x = 190 + j * (tw + 8)
            for i, t in enumerate(("main", "head")):
                y = 50 + i * (th + 40)
                d.text((x, y), f"{n} clock {made[(t, n, lang)][2]['clock']:.1f}", fill="white", font=small)
                c.paste(tiles[(t, n)], (x, y + 30))
        for i, t in enumerate(("main", "head")):
            d.text((12, 50 + i * (th + 40) + 200), WHO[t], fill="white", font=small)
        c.save(OUT / f"strip-{series}-{lang}.png", optimize=True)

(OUT / "labels-diff.txt").write_text("\n".join(diff) + "\n", encoding="utf-8")
print("\n".join(sorted(p.name for p in OUT.glob("*.png"))))
