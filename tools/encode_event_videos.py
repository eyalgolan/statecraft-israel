# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow==12.3.0", "pyyaml==6.0.3", "websockets==15.0.1"]
# ///
"""Makes the decision cards' silent illustration loops (unity/Assets/StreamingAssets/video/<file>) from the promo shots
listed in unity/Assets/Resources/EventVideos/event-videos.yaml (decision videos, owner ruling 2026-10-13; docs/event-videos.md).

The promo shots are not in the repository. Each row names its source (a path under promo/ in the checkout given by
--root, default this one; the shots are copied, never changed in place) and the source's
SHA-256, and the tool refuses a file that does not match, so it reproduces exactly what was reviewed. The encode is
pinned and is the only change besides the burned-in label:
  - scaled to cover 960x540 (Lanczos) and centre-cropped (the Dreamina shots are 1920x1088), 24 fps, H.264 High, CRF 28, preset slow, yuv420p, a keyframe every 2 s, faststart;
  - no audio stream (-an; the Dreamina shots carry a generated AAC track, which is dropped);
  - the small bilingual label "AI-generated · נוצר בבינה מלאכותית" (BUG, made by --make-bug in headless Chrome, so the
    Hebrew is laid out by the browser's bidi) over the top left corner, inside the 5% safe area, so the file says what
    it is even when it is downloaded on its own (the card shows "AI-generated illustration" too).
Every output is checked with ffprobe: one H.264 960x540 video stream, no audio, the moov box before mdat.

  uv run tools/encode_event_videos.py [--root CHECKOUT_WITH_PROMO] [--only EVENT ...]
  uv run tools/encode_event_videos.py --make-bug        # remakes tools/event_videos/ai-bug.png
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

import yaml

REPO = Path(__file__).resolve().parents[1]
MAPPING = REPO / "unity/Assets/Resources/EventVideos/event-videos.yaml"
OUT = REPO / "unity/Assets/StreamingAssets/video"
BUG = REPO / "tools/event_videos/ai-bug.png"
WIDTH, HEIGHT, FPS = 960, 540, 24
SAFE_X, SAFE_Y = 48, 27                       # 5% of 960 and 540: the label's top left corner
FFMPEG_VIDEO = ["-c:v", "libx264", "-profile:v", "high", "-preset", "slow", "-crf", "28", "-pix_fmt", "yuv420p",
                "-g", str(2 * FPS), "-keyint_min", str(2 * FPS), "-sc_threshold", "0", "-an", "-movflags", "+faststart",
                "-map_metadata", "-1"]
LABEL_EN, LABEL_HE = "AI-generated", "נוצר בבינה מלאכותית"


def load_rows(mapping: Path = MAPPING) -> list[dict]:
    return yaml.safe_load(mapping.read_text(encoding="utf-8"))["clips"]


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def filter_graph() -> str:
    """Scale the shot, then lay the label over its top left corner."""
    return (f"[0:v]scale={WIDTH}:{HEIGHT}:force_original_aspect_ratio=increase:flags=lanczos,crop={WIDTH}:{HEIGHT},fps={FPS},setsar=1[v];"
            f"[v][1:v]overlay={SAFE_X}:{SAFE_Y}:format=auto[out]")


def encode_command(src: Path, dst: Path, bug: Path = BUG) -> list[str]:
    return ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(src), "-i", str(bug),
            "-filter_complex", filter_graph(), "-map", "[out]", *FFMPEG_VIDEO, str(dst)]


def probe(path: Path) -> dict:
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "stream=codec_type,codec_name,width,height",
                          "-of", "json", str(path)], capture_output=True, text=True, check=True).stdout
    return json.loads(out)


def problems(path: Path) -> list[str]:
    """What is wrong with an encoded clip: anything but one 960x540 H.264 stream and no audio, or moov after mdat."""
    found = []
    streams = probe(path).get("streams", [])
    video = [s for s in streams if s.get("codec_type") == "video"]
    if any(s.get("codec_type") == "audio" for s in streams):
        found.append("has an audio stream")
    if len(video) != 1 or video[0].get("codec_name") != "h264" or (video[0].get("width"), video[0].get("height")) != (WIDTH, HEIGHT):
        found.append(f"not one {WIDTH}x{HEIGHT} H.264 stream: {video}")
    data = path.read_bytes()
    if data.find(b"moov") < 0 or data.find(b"moov") > data.find(b"mdat"):
        found.append("moov is not before mdat (no faststart)")
    return found


BUG_HTML = """<!doctype html><meta charset="utf-8"><style>
@font-face {{ font-family: "EB Garamond"; src: url("{en}"); }}
@font-face {{ font-family: "Frank Ruhl Libre"; src: url("{he}"); }}
html, body {{ margin: 0; background: transparent; }}
#bug {{ display: inline-block; padding: 2px 9px 3px 9px; border-radius: 9px; background: rgba(9, 14, 26, 0.62);
       color: rgba(243, 234, 216, 0.92); font: 15px "EB Garamond", serif; letter-spacing: 0.2px; white-space: nowrap; }}
#bug span[dir=rtl] {{ font-family: "Frank Ruhl Libre", serif; font-size: 14px; }}
</style><div id="bug"><span dir="ltr">{label_en}</span> · <span dir="rtl" lang="he">{label_he}</span></div>"""


def make_bug(out: Path = BUG) -> Path:
    """Renders the label in Chrome for Testing over the DevTools protocol (tools/headed.py's Browser; the browser lays
    out the Hebrew), on a transparent page, and crops the screenshot to its pixels."""
    import asyncio
    import base64
    import io
    sys.path.insert(0, str(REPO / "tools"))
    import headed  # noqa: E402
    from PIL import Image
    fonts = REPO / "unity/Assets/UI/Fonts"
    with tempfile.TemporaryDirectory() as tmp:
        for name, src in (("en.ttf", fonts / "EBGaramond-Regular.ttf"), ("he.ttf", fonts / "FrankRuhlLibre/FrankRuhlLibre-Medium.ttf")):
            shutil.copyfile(src, Path(tmp) / name)
        (Path(tmp) / "index.html").write_text(BUG_HTML.format(en="en.ttf", he="he.ttf", label_en=LABEL_EN, label_he=LABEL_HE), encoding="utf-8")

        async def shoot() -> bytes:
            async with headed.Browser(Path(tmp), port=8813, width=480, height=200, viewport=(480, 60), dpr=1) as b:
                sid = await b.open()
                await b.send("Emulation.setDefaultBackgroundColorOverride", {"color": {"r": 0, "g": 0, "b": 0, "a": 0}}, session=sid)
                await asyncio.sleep(2)   # the fonts load
                return base64.b64decode((await b.send("Page.captureScreenshot", {"format": "png"}, session=sid))["data"])

        im = Image.open(io.BytesIO(asyncio.run(shoot()))).convert("RGBA")
    box = im.getchannel("A").getbbox()
    if box is None:
        raise RuntimeError("the label rendered empty")
    out.parent.mkdir(parents=True, exist_ok=True)
    im.crop(box).save(out, optimize=True)
    return out


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--only", action="append", default=[], help="event id (repeatable); default all rows")
    ap.add_argument("--make-bug", action="store_true", help="remake the burned-in label and stop")
    ap.add_argument("--out", type=Path, default=OUT)
    ap.add_argument("--root", type=Path, default=REPO, help="the checkout whose promo/ holds the source shots")
    a = ap.parse_args(argv)
    if a.make_bug:
        print(make_bug()); return 0
    if shutil.which("ffmpeg") is None or shutil.which("ffprobe") is None:
        print("ffmpeg and ffprobe are needed"); return 2
    rows = [r for r in load_rows() if not a.only or r["event"] in a.only]
    for r in rows:   # check every source before writing anything
        src = a.root / r["source"]
        if not src.is_file():
            print(f"MISSING {src}"); return 1
        if sha256(src) != r["source_sha256"]:
            print(f"SHA-256 MISMATCH {src} (expected {r['source_sha256']})"); return 1
    a.out.mkdir(parents=True, exist_ok=True)
    for r in rows:
        dst = a.out / r["file"]
        with tempfile.TemporaryDirectory() as tmp:   # the promo shot is read from a copy, never in place
            src = Path(tmp) / "src.mp4"
            shutil.copyfile(a.root / r["source"], src)
            subprocess.run(encode_command(src, dst), check=True)
        bad = problems(dst)
        if bad:
            print(f"BAD {dst}: {'; '.join(bad)}"); return 1
        print(f"{r['file']} {dst.stat().st_size} bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
