# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3", "numpy==2.5.3"]
# ///
"""Makes the decision cards' silent clips (unity/Assets/StreamingAssets/video/<file>) from the promo shots listed in
unity/Assets/Resources/EventVideos/event-videos.yaml (decision videos, owner ruling 2026-10-13; docs/event-videos.md):
the cards' illustration loops (<event>.mp4, the mapping's clips) and the End Turn outcome clips (<event>-outcome.mp4, its
outcomes, each cut to its recorded 3.5 s window).

The promo shots are not in the repository. Each row names its source (a path under promo/ in the checkout given by
--root, default this one; the shots are copied, never changed in place) and the source's
SHA-256, and the tool refuses a file that does not match, so it reproduces exactly what was reviewed. The encode is
pinned, and the shot is not otherwise changed:
  - scaled to cover 960x540 (Lanczos) and centre-cropped (the Dreamina shots are 1920x1088), 24 fps, H.264 High, CRF 28, preset slow, yuv420p, a keyframe every 2 s, faststart;
  - no audio stream (-an; the Dreamina shots carry a generated AAC track, which is dropped);
  - no burned-in caption. Owner ruling of 14 October 2026 (relayed by the coordinator): "the 3 ilustration clips are
    great but no need to say there were created by ai". The provenance stays in the data (ai_generated: true in the
    mapping and in ASSETS.yaml), and the card still shows "AI-generated illustration" while a clip plays. The Dreamina
    shots keep Dreamina's own small "AI" badge, which is part of the shot.
Every output is checked with ffprobe: one H.264 960x540 video stream, no audio, the moov box before mdat.

  uv run tools/encode_event_videos.py [--root CHECKOUT_WITH_PROMO] [--only EVENT | EVENT-outcome ...]
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

import yaml12

REPO = Path(__file__).resolve().parents[1]
MAPPING = REPO / "unity/Assets/Resources/EventVideos/event-videos.yaml"
OUT = REPO / "unity/Assets/StreamingAssets/video"
WIDTH, HEIGHT, FPS = 960, 540, 24
FFMPEG_VIDEO = ["-c:v", "libx264", "-profile:v", "high", "-preset", "slow", "-crf", "28", "-pix_fmt", "yuv420p",
                "-g", str(2 * FPS), "-keyint_min", str(2 * FPS), "-sc_threshold", "0", "-an", "-movflags", "+faststart",
                "-map_metadata", "-1"]


def load_rows(mapping: Path = MAPPING) -> list[dict]:
    """The card loops (clips) and the End Turn outcome clips (outcomes), in that order."""
    doc = yaml12.load(mapping.read_text(encoding="utf-8"))
    return doc["clips"] + doc.get("outcomes", [])


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def filter_graph(row: dict | None = None) -> str:
    """Scale and crop the shot (nothing is laid over it). An outcome row may also ask for:
    decimate: drop the repeated frames of a 60 fps Dreamina shot first, so its 24 unique frames a second play evenly;
    start_frame and frames: keep only that window, in frames at 24 fps (the steadiest 3.5 s, chosen by
    tools/encode_event_videos.py's steadiest(); the row records it so the cut reproduces)."""
    row = row or {}
    pre = "mpdecimate=hi=64:lo=32:frac=0.1,setpts=N/24/TB," if row.get("decimate") else ""
    cut = ""
    if "start_frame" in row:
        s, n = int(row["start_frame"]), int(row["frames"])
        cut = f",trim=start_frame={s}:end_frame={s + n},setpts=PTS-STARTPTS"
    return f"[0:v]{pre}scale={WIDTH}:{HEIGHT}:force_original_aspect_ratio=increase:flags=lanczos,crop={WIDTH}:{HEIGHT},fps={FPS}{cut},setsar=1[out]"


def steadiest(src: Path, seconds: float = 3.5, decimate: bool = False) -> tuple[int, dict]:
    """The start frame (at 24 fps) of the steadiest window of the shot: the least frame-to-frame change and above all
    no sudden jump in it (a morph, a warp or a cut is a jump in the change), as score = mean change + 3 x largest jump +
    spread. Returns the frame and the window's numbers."""
    import numpy as np
    w, h = 192, 108
    pre = "mpdecimate=hi=64:lo=32:frac=0.1,setpts=N/24/TB," if decimate else ""
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(src), "-vf", f"{pre}fps={FPS},scale={w}:{h},format=gray",
                          "-f", "rawvideo", "-"], capture_output=True, check=True).stdout
    fr = np.frombuffer(raw, np.uint8).reshape(-1, h, w).astype(np.float32)
    d = np.abs(np.diff(fr, axis=0)).mean(axis=(1, 2))
    jumps = np.abs(np.diff(d))
    n = int(round(seconds * FPS))
    best = None
    for s in range(0, len(fr) - n + 1):
        seg, j = d[s:s + n - 1], jumps[s:s + n - 2]
        score = float(seg.mean() + 3 * j.max() + seg.std())
        if best is None or score < best[0]:
            best = (score, s, float(seg.mean()), float(j.max()))
    return best[1], {"score": round(best[0], 2), "mean_change": round(best[2], 2), "max_jump": round(best[3], 2)}


def encode_command(src: Path, dst: Path, row: dict | None = None) -> list[str]:
    return ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(src),
            "-filter_complex", filter_graph(row), "-map", "[out]", *FFMPEG_VIDEO, str(dst)]


def probe(path: Path) -> dict:
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "stream=codec_type,codec_name,width,height",
                          "-of", "json", str(path)], capture_output=True, text=True, check=True).stdout
    return json.loads(out)


def frame_count(path: Path) -> int:
    out = subprocess.run(["ffprobe", "-v", "error", "-select_streams", "v:0", "-count_frames", "-show_entries",
                          "stream=nb_read_frames", "-of", "csv=p=0", str(path)], capture_output=True, text=True, check=True).stdout
    return int(out.strip().split(",")[0])


def problems(path: Path, frames: int | None = None) -> list[str]:
    """What is wrong with an encoded clip: anything but one 960x540 H.264 stream and no audio, moov after mdat, or (an
    outcome clip) a frame count other than its row's."""
    found = []
    if frames is not None and (n := frame_count(path)) != frames:
        found.append(f"{n} frames, not {frames}")
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


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--only", action="append", default=[],
                    help="a card loop's event id, or EVENT-outcome for an outcome clip (repeatable); default all rows")
    ap.add_argument("--steadiest", type=Path, help="print the steadiest 3.5 s window of this shot and stop")
    ap.add_argument("--decimate", action="store_true", help="with --steadiest: the shot is a 60 fps Dreamina file")
    ap.add_argument("--out", type=Path, default=OUT)
    ap.add_argument("--root", type=Path, default=REPO, help="the checkout whose promo/ holds the source shots")
    a = ap.parse_args(argv)
    if a.steadiest:
        start, numbers = steadiest(a.steadiest, decimate=a.decimate)
        print(json.dumps({"start_frame": start, "start_s": round(start / FPS, 3), "frames": int(round(3.5 * FPS)), **numbers})); return 0
    if shutil.which("ffmpeg") is None or shutil.which("ffprobe") is None:
        print("ffmpeg and ffprobe are needed"); return 2
    rows = [r for r in load_rows() if not a.only or Path(r["file"]).stem in a.only]
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
            if sha256(src) != r["source_sha256"]:   # the copy is what is encoded, so check it too
                print(f"SHA-256 MISMATCH {src} (copy of {r['source']})"); return 1
            subprocess.run(encode_command(src, dst, row=r), check=True)
        bad = problems(dst, r.get("frames"))
        if bad:
            print(f"BAD {dst}: {'; '.join(bad)}"); return 1
        print(f"{r['file']} {dst.stat().st_size} bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
