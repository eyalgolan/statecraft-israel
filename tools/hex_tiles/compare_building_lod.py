"""Compare matched one-building captures; run from the repository root.

Requires Pillow 12.3; writes only ignored captures/building-lod evidence.
"""

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageStat


def main():
    folder = Path.cwd() / "captures/building-lod"
    rows, images = [], []
    for label in ("gameplay", "close"):
        before = Image.open(folder / f"{label}-before.png").convert("RGB")
        after = Image.open(folder / f"{label}-after.png").convert("RGB")
        if before.size != after.size or before.size != (1920, 1080):
            raise ValueError("Expected matched 1920x1080 captures")
        diff = ImageChops.difference(before, after)
        box = diff.getbbox()
        row = {
            "camera": label,
            "resolution": [1920, 1080],
            "identical_pixels": box is None,
            "full_frame_mean_absolute_rgb": sum(ImageStat.Stat(diff).mean) / 3,
            "changed_pixel_bbox": box,
            "changed_pixels": sum(pixel != (0, 0, 0) for pixel in diff.get_flattened_data()),
        }
        if box:
            row["changed_region_mean_absolute_rgb"] = sum(ImageStat.Stat(diff.crop(box)).mean) / 3
        rows.append(row)
        images.extend([before, after])

    canvas = Image.new("RGB", (3840, 2240), "#20242b")
    draw = ImageDraw.Draw(canvas)
    for index, image in enumerate(images):
        x, y = index % 2 * 1920, index // 2 * 1120
        label = ("GAMEPLAY" if index < 2 else "CLOSE") + (
            " — BEFORE: original" if index % 2 == 0 else " — AFTER: experiment enabled"
        )
        draw.text((x + 20, y + 12), label, fill="white")
        canvas.paste(image, (x, y + 40))
    canvas.save(folder / "Statecraft-building-LOD-matched-comparison.png")
    report = {
        "comparison": rows,
        "note": "Pixel deltas quantify these two fixed Editor cameras only; no human acceptance or performance verdict.",
        "sha256": {
            path.name: hashlib.sha256(path.read_bytes()).hexdigest()
            for path in sorted(folder.glob("*.png"))
        },
    }
    (folder / "pixel-comparison.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
