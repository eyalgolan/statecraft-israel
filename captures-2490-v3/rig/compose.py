"""compose.py <S> <before-sha> <after-sha> <main-sha-short>: labelled images and the label diff for #2490 captures v3."""
import re
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

S = Path(sys.argv[1]); BEFORE, AFTER = sys.argv[2][:9], sys.argv[3][:9]
OUT = S / "out"; OUT.mkdir(exist_ok=True)
W, H, BAR = 1280, 720, 52
font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 21)
SHOT = re.compile(r"E21PLAY (en|he) SHOT (\S+) d=(\d+) lookAt=\(([^)]*)\) t=([\d.]+) sinceDakotas=([\d.]+)")
LABELS = re.compile(r"E21PLAY (en|he) (\S+) d=(\d+) lookAt=\(([^)]*)\) t=([\d.]+) sinceDakotas=([\d.]+) keepClear=(\d+) boxes=(.*?) shown=\[(.*)\]$")


def read(side):
    shots, labels, meta = {}, {}, []
    for line in (S / "logs" / f"{side}-lines.txt").read_text(encoding="utf-8").splitlines():
        m = SHOT.search(line)
        if m: shots[(m[1], m[2])] = dict(d=int(m[3]), at=m[4], t=float(m[5]), since=float(m[6]))
        m = LABELS.search(line)
        if m: labels[(m[1], m[2])] = dict(d=int(m[3]), at=m[4], t=float(m[5]), since=float(m[6]), keep=int(m[7]), boxes=m[8],
                                          shown=[x for x in m[9].split(" ; ") if x])
        if " begun " in line or " dakotas=" in line or " telaviv=" in line or " bomb runs=" in line or " blast " in line:
            meta.append(line[line.index("E21PLAY"):])
    return shots, labels, meta


sides = {"before": read("before"), "after": read("after")}
title = {"before": f"BEFORE main {BEFORE}", "after": f"AFTER main+#2490 (5ca573246) = {AFTER}"}


def caption(side, lang, what):
    s = sides[side][0][(lang, what)]
    return f"{title[side]} | {lang.upper()} | 15 May 1948, in play | d={s['d']/1000:.0f} km | play t={s['t']:.1f}s, {s['since']:.1f}s after Dakotas"


def labelled(side, lang, what, out):
    img = Image.open(S / f"raw-{side}" / f"{what}-{lang}.png").convert("RGB")
    c = Image.new("RGB", (W, H + BAR), "white"); d = ImageDraw.Draw(c)
    d.text((12, 12), caption(side, lang, what), fill="black", font=font)
    c.paste(img, (0, BAR)); c.save(OUT / out, optimize=True)


# 1. labels 2x2
canvas = Image.new("RGB", (W * 2, (H + BAR) * 2), "white"); d = ImageDraw.Draw(canvas)
for row, lang in enumerate(("en", "he")):
    for col, side in enumerate(("before", "after")):
        x, y = col * W, row * (H + BAR)
        d.text((x + 12, y + 12), caption(side, lang, "labels-turnmap"), fill="black", font=font)
        canvas.paste(Image.open(S / f"raw-{side}" / f"labels-turnmap-{lang}.png").convert("RGB"), (x, y + BAR))
d.line([(W, 0), (W, canvas.height)], fill="black", width=3); d.line([(0, H + BAR), (canvas.width, H + BAR)], fill="black", width=3)
canvas.save(OUT / "labels-15may-opening-31km-en-he-before-after-inplay.png", optimize=True)

# 2-4. after-tree singles
labelled("after", "en", "wide-31km", "telaviv-wide-31km-en-after-inplay.png")
labelled("after", "en", "close-20km", "telaviv-close-20km-en-after-inplay.png")
for (lang, what) in sides["after"][0]:
    if what.startswith("blast-"): labelled("after", lang, what, f"{what}-{lang}-after-inplay.png")

# 53's sequence: one drop, one camera, three loop-clock times, side by side (each frame keeps its own caption line).
SEQ = re.compile(r"E21PLAY en SEQ (seq\d-\S+) target u=([\d.]+) actual u=([\d.]+) \(release\+([\d.]+)s\)")
seqinfo = {}
for line in (S / "logs" / "after-lines.txt").read_text(encoding="utf-8").splitlines():
    m = SEQ.search(line)
    if m: seqinfo[m[1]] = (float(m[3]), float(m[4]))
names = [n for n in ("seq1-midfall", "seq2-impact", "seq3-fireball") if n in seqinfo]
if names:
    small = font.font_variant(size=22)
    strip = Image.new("RGB", (W * len(names), H + BAR * 2), "white"); d = ImageDraw.Draw(strip)
    s0 = sides["after"][0][("en", names[0])]
    d.text((12, 10), f"AFTER {AFTER} | EN | in play | ONE drop, same camera d={s0['d']/1000:.0f} km | lead Dakota loop clock u (release + s)", fill="black", font=font)
    for i, n in enumerate(names):
        u, rel = seqinfo[n]; s1 = sides["after"][0][("en", n)]
        d.text((i * W + 12, BAR + 12), f"({i+1}) {n[5:]}: u={u:.2f}s (release+{rel:.2f}s), play t={s1['t']:.1f}s, {s1['since']:.1f}s after Dakotas", fill="black", font=small)
        strip.paste(Image.open(S / "raw-after" / f"{n}-en.png").convert("RGB"), (i * W, BAR * 2))
        labelled("after", "en", n, f"{n}-en-after-inplay.png")
    strip.save(OUT / "bomb-sequence-midfall-impact-fireball-en-after-inplay.png", optimize=True)

# label diff
lines = [f"# PR #2490 captures v3 (IN PLAY): names shown on the 15 May 1948 turn, opening view d=31000 lookAt (56929.60, 0, 289262.20)",
         f"# before = main {sys.argv[2]}; after = {sys.argv[3]} (main + #2490 at 5ca573246)",
         "# Key: S<i> = settlement pill, L<i> = 1948 locality pill, M<i> = map location pill. Taken >= 62 s of play after the Dakotas appeared.", ""]
for side in ("before", "after"):
    lines += [f"{side} meta: {m}" for m in sides[side][2]]
lines.append("")
counts = []
for lang in ("en", "he"):
    for what in ("labels-turnmap", "wide-31km", "close-20km"):
        b = sides["before"][1].get((lang, what)); a = sides["after"][1].get((lang, what))
        if a is None: continue
        if b is None:
            lines.append(f"== {lang} {what} (after only) d={a['d']} t={a['t']:.1f}s keepClear={a['keep']} shown {len(a['shown'])}: {' ; '.join(a['shown'])}")
            continue
        bs, as_ = set(b["shown"]), set(a["shown"])
        counts.append(f"{lang.upper()} {what}: before {len(bs)}, after {len(as_)}")
        lines += [f"== {lang} {what}  before d={b['d']} t={b['t']:.1f}s ({b['since']:.1f}s after Dakotas) keepClear={b['keep']}; "
                  f"after d={a['d']} t={a['t']:.1f}s ({a['since']:.1f}s after Dakotas) keepClear={a['keep']}.  shown before {len(bs)}, after {len(as_)}",
                  f"   before boxes: {b['boxes']}", f"   after boxes: {a['boxes']}",
                  f"   shown before: {' ; '.join(sorted(bs))}", f"   shown after: {' ; '.join(sorted(as_))}",
                  f"   hidden after, shown before: {' ; '.join(sorted(bs - as_)) or '(none)'}",
                  f"   shown after, hidden before: {' ; '.join(sorted(as_ - bs)) or '(none)'}"]
(OUT / "labels-diff-inplay.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
(OUT / "counts.txt").write_text("\n".join(counts) + "\n", encoding="utf-8")
print("\n".join(counts))
