# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow==12.3.0", "pyyaml==6.0.3"]
# ///
"""Makes the event-card header photographs (unity/Assets/UI/EventImages/<event>.jpg) from the archival originals listed
in unity/Assets/UI/EventImages/event-images.yaml. Offline: it never downloads. Fetch each row's `original` URL once,
into an empty folder outside the repo, saved as <event>.jpg; the tool checks every file's SHA-1 against the row before
it reads it, so a wrong or tampered download is refused. Downloads are untrusted, so run it isolated:

  uv run --with pillow==12.3.0 --with pyyaml==6.0.3 python -I tools/event_images.py --originals DIR

The adaptation, recorded as "graded and cropped" in each credit (CC BY 4.0 asks for it), is exactly this and nothing
else: the picture is read as luminance (a colour slide becomes black and white), cropped to the row's 16:9 box, resized
to at most WIDTH pixels wide (a multiple of 64, so both sides are multiples of 4), its levels stretched between the 0.5
and 99.5 percentiles, and toned from navy shadows through sepia mid-tones to warm highlights. No retouching, no colourising, nothing added or removed."""
from __future__ import annotations

import argparse
import hashlib
import io
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))  # under python -I the script's folder is not on sys.path
import yaml12  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
FOLDER = REPO / "unity/Assets/UI/EventImages"
MAPPING = FOLDER / "event-images.yaml"
ASPECT = 16 / 9                    # the card's media header, width : height (EventCardPresenter.MediaAspect)
WIDTH = 960                        # the header's width at the 1080p reference (a 1040 px card less its padding)
MAX_BYTES = 450 * 1024            # 200 KB at the old 720x320, scaled with the pixel count (x2.25)
# The grade: luminance 0 -> navy, 128 -> sepia, 255 -> warm paper (the promo's navy and gold palette).
SHADOW, MID, HIGHLIGHT = (14, 18, 30), (126, 117, 104), (238, 232, 218)


def crop_box(size: tuple[int, int], crop: list[float]) -> tuple[int, int, int, int]:
    """The 16:9 box for a row's crop [centre x, centre y, width] (shares of the original), kept inside the picture.
    If the picture is too short for that width, the width shrinks to fit the full height."""
    w, h = size
    cx, cy, share = crop
    if not (0 < share <= 1 and 0 <= cx <= 1 and 0 <= cy <= 1):
        raise ValueError(f"crop {crop} out of range")
    bw = round(w * share)
    bh = round(bw / ASPECT)
    if bh > h:
        bh = h
        bw = round(bh * ASPECT)
    left = min(max(round(cx * w - bw / 2), 0), w - bw)
    top = min(max(round(cy * h - bh / 2), 0), h - bh)
    return left, top, left + bw, top + bh


def tone_lut() -> list[int]:
    """The per-channel lookup (R, G, B, 256 entries each) for the navy-sepia-paper grade."""
    lut: list[int] = []
    for channel in range(3):
        for v in range(256):
            a, b, t = (SHADOW, MID, v / 128) if v < 128 else (MID, HIGHLIGHT, (v - 128) / 127)
            lut.append(round(a[channel] + (b[channel] - a[channel]) * t))
    return lut


def stretch(im: Image.Image) -> Image.Image:
    """Levels between the 0.5 and 99.5 percentiles of a luminance picture."""
    hist, total = im.histogram(), im.width * im.height
    def pct(p: float) -> int:
        acc = 0
        for v, n in enumerate(hist):
            acc += n
            if acc >= total * p:
                return v
        return 255
    lo, hi = pct(0.005), pct(0.995)
    if hi <= lo:
        return im
    return im.point(lambda v: min(255, max(0, round((v - lo) * 255 / (hi - lo)))))


def grade(original: Image.Image, crop: list[float]) -> Image.Image:
    """The shipped picture for one row (see the module docstring for the full list of changes)."""
    gray = original.convert("L").crop(crop_box(original.size, crop))
    width = min(WIDTH, gray.width) // 64 * 64       # 16:9 in whole pixels, both sides multiples of 4 (block compression)
    gray = gray.resize((width, width * 9 // 16), Image.Resampling.LANCZOS)
    return stretch(gray).convert("RGB").point(tone_lut())


def encode(im: Image.Image) -> bytes:
    """Baseline JPEG without metadata; quality steps down until it is under MAX_BYTES."""
    for quality in (86, 82, 78, 74, 70):
        buf = io.BytesIO()
        im.save(buf, "JPEG", quality=quality, optimize=True, progressive=False)
        if buf.tell() <= MAX_BYTES:
            return buf.getvalue()
    raise ValueError("cannot fit the picture under MAX_BYTES")


def load_rows(mapping: Path = MAPPING) -> list[dict]:
    return yaml12.load(mapping.read_text(encoding="utf-8"))["images"]


def isolated() -> bool:
    """True when Python runs with -I (the tests stand in for it: pytest itself is not isolated)."""
    return bool(sys.flags.isolated)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--originals", type=Path, required=True, help="folder holding <event>.jpg downloads")
    ap.add_argument("--out", type=Path, default=FOLDER)
    ap.add_argument("--only", action="append", default=[], help="event id (repeatable); default all rows")
    a = ap.parse_args(argv)
    if not isolated():
        print("run with python -I: the originals are untrusted downloads (see the module docstring)"); return 2
    rows = [r for r in load_rows() if not a.only or r["event"] in a.only]
    for r in rows:   # check every download before reading or writing anything, so a bad row writes nothing
        src = a.originals / f"{r['event']}.jpg"
        if not src.is_file():
            print(f"MISSING {src}"); return 1
        if hashlib.sha1(src.read_bytes()).hexdigest() != r["sha1"]:
            print(f"SHA-1 MISMATCH {src} (expected {r['sha1']})"); return 1
        with Image.open(src) as im:   # reads the header only
            if list(im.size) != r["original_size"]:
                print(f"SIZE MISMATCH {r['event']}: {im.size} vs {r['original_size']}"); return 1
    for r in rows:
        with Image.open(a.originals / f"{r['event']}.jpg") as im:
            data = encode(grade(im, r["crop"]))
        (a.out / r["file"]).write_bytes(data)
        print(f"{r['file']} {len(data)} bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
