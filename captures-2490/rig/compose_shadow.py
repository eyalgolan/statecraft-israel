import sys, re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
S = Path(sys.argv[1]); still = sys.argv[2]
tsv = (S / "raw-after" / "shadow-worst-relief.tsv").read_text()
m = re.search(r"# (max\(s\.y - g\) = .*)", tsv); line = m.group(1)
img = Image.open(S / "raw-after" / f"shadow-{still}.png").convert("RGB")
bar = 112; W, H = img.size
c = Image.new("RGB", (W, H + bar), "white"); c.paste(img, (0, bar)); d = ImageDraw.Draw(c)
# shadow centre: viewport (0.50, 0.44) from the rig log (E21SHADOW still ... shadow viewport)
cx, cy = int(0.50 * W), bar + int((1 - 0.44) * H)
for dx, dy in ((1, 0), (0, 1)): d.line([(cx - 18 * dx, cy - 18 * dy), (cx - 6 * dx, cy - 6 * dy)], fill="yellow", width=3); d.line([(cx + 6 * dx, cy + 6 * dy), (cx + 18 * dx, cy + 18 * dy)], fill="yellow", width=3)
f = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 26); g = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 18)
d.text((14, 8), line + "  (PR #2490, after tree 7294073ac, 15 May 1948)", fill="black", font=f)
d.text((14, 46), "Dakota shadow at worst relief. s = shadow centre, g = max(terrain SampleHeight + y0, sea level) under it;", fill="black", font=g)
d.text((14, 72), f"24 samples per plane (t = i * 120 s / 24). Still at that time, view: {still}, 20 km, yaw 0; yellow tick = shadow centre (disc scale 4971 x 4628 m)", fill="black", font=g)
(S / "out").mkdir(exist_ok=True)
c.save(S / "out" / "dakota-shadow-worst-relief.png", optimize=True)
print(line)
