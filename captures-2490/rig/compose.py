import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
S = Path(sys.argv[1]); view = sys.argv[2]
W, H, bar = 1280, 720, 52
f = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 30)
title = {"before": "BEFORE: main 1957078d3", "after": "AFTER: main + PR #2490 (15e146332), tree 7294073ac"}
canvas = Image.new("RGB", (W * 2, (H + bar) * 2), "white"); d = ImageDraw.Draw(canvas)
for row, lang in enumerate(("en", "he")):
    for col, side in enumerate(("before", "after")):
        x, y = col * W, row * (H + bar)
        d.text((x + 14, y + 10), f"{title[side]} | {lang.upper()} | 15 May 1948 | view: {view}", fill="black", font=f)
        canvas.paste(Image.open(S / f"raw-{side}" / f"labels-{view}-{lang}.png").convert("RGB"), (x, y + bar))
d.line([(W, 0), (W, canvas.height)], fill="black", width=3); d.line([(0, H + bar), (canvas.width, H + bar)], fill="black", width=3)
(S / "out").mkdir(exist_ok=True)
canvas.save(S / "out" / f"labels-15may-{view}-en-he-before-after.png", optimize=True)
