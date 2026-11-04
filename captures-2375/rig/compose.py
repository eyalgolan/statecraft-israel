import sys, shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
# compose.py <e21-c04 dir> <main short sha>: copies the en_US stills to out/ and builds a 2x3 sheet (before/after x 25/50/75 %).
S = Path(sys.argv[1]); main = sys.argv[2]
out = S / "out"; out.mkdir(exist_ok=True)
stills = ["1-standing", "2-moving", "3-firing-25", "3-firing-50", "3-firing-75"]
for side in ("before", "after"):
    src = S / f"raw-{side}-en_US" / "tokens"
    for s in stills:
        shutil.copy(src / f"israel-c04-{s}.jpg", out / f"{side}-{s}.jpg")
    shutil.copy(src / "README.txt", out / f"{side}-README.txt")
im0 = Image.open(out / "after-3-firing-50.jpg"); W, H = im0.size
bar = 60
canvas = Image.new("RGB", (W * 3, (H + bar) * 2), "white")
d = ImageDraw.Draw(canvas)
try:
    f = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 40)
except OSError:
    f = ImageFont.load_default()
labels = {"before": "BEFORE: b2269c3d7 (parent of #2397)", "after": f"AFTER: main {main}"}
for row, side in enumerate(("before", "after")):
    for col, q in enumerate(("25", "50", "75")):
        x, y = col * W, row * (H + bar)
        d.text((x + 20, y + 8), f"{labels[side]}, C04 firing at {q} %", fill="black", font=f)
        canvas.paste(Image.open(out / f"{side}-3-firing-{q}.jpg").convert("RGB"), (x, y + bar))
canvas = canvas.resize((canvas.width // 2, canvas.height // 2), Image.LANCZOS)
canvas.save(out / "compare-firing-2x3.png", optimize=True)
print(W, H, canvas.size)
