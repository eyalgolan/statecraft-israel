"""compose_seq2.py <S> <after-sha>: the v2 bomb sequence strips and the in-pass Tel Aviv shots, labelled."""
import re
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

S = Path(sys.argv[1]); AFTER = sys.argv[2][:9]
OUT = S / "out"; OUT.mkdir(exist_ok=True)
W, H, BAR = 1280, 720, 52
font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 21)
lines = (S / "logs-seq2" / "seq2-lines.txt").read_text(encoding="utf-8").splitlines()
SHOT = re.compile(r"E21PLAY en SHOT (\S+) d=(\d+) lookAt=\(([^)]*)\) t=([\d.]+) sinceDakotas=([\d.]+)")
SEQ = re.compile(r"E21PLAY en SEQ2 (s2-\d-\w+)-(\d+)km u=([\d.]+) release\+([\d.]+)s")
shots = {m[1]: dict(d=int(m[2]), t=float(m[4]), since=float(m[5])) for m in map(SHOT.search, lines) if m}
seq = {(m[1], int(m[2])): (float(m[3]), float(m[4])) for m in map(SEQ.search, lines) if m}


def single(name, out, extra=""):
    s = shots[name]
    c = Image.new("RGB", (W, H + BAR), "white"); d = ImageDraw.Draw(c)
    d.text((12, 14), f"AFTER main+#2490 (5ca573246) = {AFTER} | EN | 15 May 1948, in play | d={s['d']/1000:.0f} km | play t={s['t']:.1f}s, {s['since']:.1f}s after Dakotas{extra}", fill="black", font=font)
    c.paste(Image.open(S / "raw-seq2" / f"{name}-en.png").convert("RGB"), (0, BAR)); c.save(OUT / out, optimize=True)


single("s2-wide-31km", "telaviv-wide-31km-en-after-inplay-bombing-pass.png", " | bombing pass")
single("s2-close-20km", "telaviv-close-20km-en-after-inplay-bombing-pass.png", " | bombing pass")
names = ["s2-1-midfall", "s2-2-impact", "s2-3-fireball"]
for km in (12, 20):
    strip = Image.new("RGB", (W * 3, H + BAR * 2), "white"); d = ImageDraw.Draw(strip)
    d.text((12, 12), f"AFTER main+#2490 (5ca573246) = {AFTER} | EN | in play | ONE bomb (lead Dakota), ONE fixed camera d={km} km | loop clock u = Time.time (lead lag 0); fall 2 s, flash 0.5 s, blast 2.4 s", fill="black", font=font)
    for i, n in enumerate(names):
        u, rel = seq[(n, km)]; s = shots[f"{n}-{km}km"]
        d.text((i * W + 12, BAR + 12), f"({i+1}) {n[5:]}: u={u:.3f}s = release+{rel:.3f}s | play t={s['t']:.2f}s, {s['since']:.1f}s after Dakotas", fill="black", font=font)
        strip.paste(Image.open(S / "raw-seq2" / f"{n}-{km}km-en.png").convert("RGB"), (i * W, BAR * 2))
        single(f"{n}-{km}km", f"bomb-seq-{i+1}-{n[5:]}-{km}km-en-after-inplay.png", f" | u={u:.3f}s = release+{rel:.3f}s")
    strip.save(OUT / f"bomb-sequence-one-drop-{km}km-en-after-inplay.png", optimize=True)
print("ok", sorted(shots))
