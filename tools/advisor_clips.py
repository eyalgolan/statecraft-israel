# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Brings an advisor clip into the game and checks the shipped ones (advisors spec 11 and 12; plan Task 26).

A clip is two files under unity/Assets/StreamingAssets/advisors/, named after the advisor message they belong to:
  <message>.mp4     the silent picture, held to the decision cards' encode (tools/encode_event_videos.py FFMPEG_VIDEO:
                    960x540 H.264, no audio stream, faststart); a delivered file that already passes problems() is copied
                    byte for byte, any other is encoded with that profile (scaled to cover and centre-cropped);
  <message>.en.wav  the voice, the exact 16-bit PCM WAV the lip-sync used, copied byte for byte (anything else is
                    refused: re-encoding it would move the lips off the voice).
The media row in advisor-messages.yaml, the cue strings and the ASSETS.yaml rows are written by hand; `check` holds
them together.

  uv run tools/advisor_clips.py ingest MESSAGE --picture CLIP.mp4 --voice VOICE.wav [--lang en]
  uv run tools/advisor_clips.py check
"""
from __future__ import annotations

import argparse
import hashlib
import shutil
import subprocess
import sys
import tempfile
import uuid
import wave
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))  # under python -I the script's folder is not on sys.path
import encode_event_videos as ev  # noqa: E402
import yaml12  # noqa: E402

REPO = ev.REPO
OUT = REPO / "unity/Assets/StreamingAssets/advisors"
MESSAGES = REPO / "unity/Assets/Resources/Advisors/advisor-messages.yaml"
ASSETS = REPO / "ASSETS.yaml"
META = ("fileFormatVersion: 2\nguid: {}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n"
        "  assetBundleVariant: \n")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def voice_problems(path: Path) -> list[str]:
    """Anything but an uncompressed 16-bit PCM WAV."""
    try:
        with wave.open(str(path), "rb") as w:
            if w.getcomptype() != "NONE" or w.getsampwidth() != 2:
                return [f"{path.name}: not 16-bit PCM ({w.getsampwidth() * 8}-bit, {w.getcomptype()})"]
    except (wave.Error, EOFError) as e:
        return [f"{path.name}: not a PCM WAV ({e})"]
    return []


def media_rows(messages: Path = MESSAGES) -> dict[str, dict]:
    """Each message's or page's media row, by its id."""
    doc = yaml12.load(messages.read_text(encoding="utf-8")) or {}
    return {r["id"]: r["media"] for r in (doc.get("messages") or []) + (doc.get("pages") or []) if r.get("media")}


def check(out: Path = OUT, messages: Path = MESSAGES, assets: Path = ASSETS) -> list[str]:
    """Every problem with the shipped clips, one line each."""
    found: list[str] = []
    rules = {r["glob"]: r for r in yaml12.load(assets.read_text(encoding="utf-8"))["rules"]}
    used: set[str] = set()
    for mid, media in media_rows(messages).items():
        files = ([media["picture"]] if media.get("picture") else []) + list((media.get("narration") or {}).values())
        for name in files:
            used.add(name)
            path = out / name
            if not path.is_file():
                found.append(f"{mid}: no {name}"); continue
            if name.endswith(".mp4"):
                found += [f"{name}: {p}" for p in ev.problems(path)]
            elif name.endswith(".wav"):
                found += voice_problems(path)
            if not (out / (name + ".meta")).is_file():
                found.append(f"{name}: no .meta")
            rule = rules.get(f"{out.relative_to(REPO).as_posix()}/{name}") if out.is_relative_to(REPO) else None
            if rule is None:
                found.append(f"{name}: no ASSETS.yaml row")
            elif rule.get("ai_generated") is not True or sha256(path) not in rule.get("source", ""):
                found.append(f"{name}: its ASSETS.yaml row must be ai_generated and record this file's sha256")
    if out.is_dir():
        for path in sorted(out.iterdir()):
            if path.suffix != ".meta" and path.name not in used:
                found.append(f"{path.name}: no media row uses it")
    return found


def ingest(message: str, picture: Path, voice: Path, lang: str = "en", out: Path = OUT) -> list[Path]:
    """Writes <message>.mp4 and <message>.<lang>.wav (and new .meta files) into out; returns the two files."""
    bad = voice_problems(voice)
    if bad:
        raise ValueError("; ".join(bad))
    out.mkdir(parents=True, exist_ok=True)
    pic, wav = out / f"{message}.mp4", out / f"{message}.{lang}.wav"
    if ev.problems(picture):
        with tempfile.TemporaryDirectory() as tmp:   # the delivered file is read from a copy, never in place
            src = Path(tmp) / "src.mp4"
            shutil.copyfile(picture, src)
            subprocess.run(ev.encode_command(src, pic), check=True)
    else:
        shutil.copyfile(picture, pic)
    bad = ev.problems(pic)
    if bad:
        raise ValueError(f"{pic.name}: {'; '.join(bad)}")
    shutil.copyfile(voice, wav)
    for f in (pic, wav):
        meta = f.with_name(f.name + ".meta")
        if not meta.exists():
            meta.write_text(META.format(uuid.uuid4().hex), encoding="utf-8")
    return [pic, wav]


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    i = sub.add_parser("ingest", help="copy or encode one clip into StreamingAssets/advisors")
    i.add_argument("message")
    i.add_argument("--picture", type=Path, required=True)
    i.add_argument("--voice", type=Path, required=True)
    i.add_argument("--lang", default="en", choices=["en", "he"])
    sub.add_parser("check", help="check every shipped clip against its media row and ASSETS.yaml row")
    a = ap.parse_args(argv)
    if shutil.which("ffmpeg") is None or shutil.which("ffprobe") is None:
        print("ffmpeg and ffprobe are needed"); return 2
    if a.cmd == "ingest":
        for f in ingest(a.message, a.picture, a.voice, a.lang):
            print(f"{f.relative_to(REPO) if f.is_relative_to(REPO) else f} {f.stat().st_size} bytes sha256 {sha256(f)}")
        return 0
    found = check()
    print("\n".join(found) if found else "advisor clips: OK")
    return 1 if found else 0


if __name__ == "__main__":
    sys.exit(main())
