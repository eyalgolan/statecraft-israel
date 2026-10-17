"""Packages a WebGL release build as the itch.io zip, but only once the owner's sensitivity review is recorded for every
event with a sensitive tag (spec 5.5; docs/m3-release.md). The gate is the sim's own (Statecraft.Cli --release-gate),
run on the content INSIDE the build folder, so it checks exactly what would ship. Stdlib only (plus the .NET SDK).

  python3 tools/package_release.py [--build unity/Builds/Web-gzip] [--zip unity/Builds/statecraft-m3-web.zip]

Exit 0 and "PACKAGED <zip> <bytes>" on success; exit 1 and "PACKAGE REFUSED ..." when the gate refuses or the build is
not a WebGL build folder.

The event-card photographs are packed into Unity's data files, where this script cannot see them, so the build writes
what it carries into event-photos.json (Build.PhotoManifestName, review I1). The zip is refused when that file is
missing or unreadable, when the build was not made with -shareable, or when any photograph in it is sensitive and not
sensitivity-reviewed (docs/event-images.md).

Build.Web names the files under Build/ by their content hash (issue #437), so their names change from build to build and
index.html is what says which ones belong to this build. The zip carries exactly the Build/ files index.html names: it
is refused when one is missing, and a file it does not name (an earlier build's, left in the folder) is left out with a
"LEFT OUT Build/<name>" line.
"""
import argparse
import json
import re
import subprocess
import sys
import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]


def gate(content_dir: Path):
    """Runs the sim's release gate on a content folder: (passed, message)."""
    r = subprocess.run(["dotnet", "run", "--project", str(REPO / "sim/Statecraft.Cli"), "-v", "q", "--",
                        "--release-gate", "--content", str(content_dir)], capture_output=True, text=True)
    lines = [l for l in (r.stdout + r.stderr).splitlines() if "RELEASE GATE" in l or "error" in l.lower()]
    return r.returncode == 0, "\n".join(lines) or f"release gate exited {r.returncode}"


PHOTO_MANIFEST = "event-photos.json"   # Build.PhotoManifestName (unity/Assets/Editor/Build.cs)


def photo_refusal(build: Path):
    """Why the build's event photographs may not be shared, or None (see the module docstring)."""
    f = build / PHOTO_MANIFEST
    if not f.is_file():
        return f"{build} has no {PHOTO_MANIFEST}: build it with Build.Web -shareable, which writes it"
    try:
        m = json.loads(f.read_text(encoding="utf-8"))
        photos = m["photos"]
        shareable = m["shareable"]
        if not isinstance(photos, list) or not isinstance(shareable, bool):
            raise TypeError("photos must be a list and shareable a boolean")
        open_ = sorted(p["event_id"] for p in photos if p["sensitive"] is not False and p["sensitivity_reviewed"] is not True)
    except (ValueError, KeyError, TypeError) as e:
        return f"{f} is not a valid photo manifest ({e})"
    if shareable is not True:
        return f"{build} was not built with -shareable ({PHOTO_MANIFEST} says shareable: false)"
    if open_:
        return ("the event photographs need the owner's sensitivity review first: " + ", ".join(open_)
                + " (sensitivity_reviewed: true in unity/Assets/UI/EventImages/event-images.yaml; docs/event-images.md)")
    return None


BUILD_FILE = re.compile(r'"Build/([^"/]+)"')   # as the WebGL template writes them (src="Build/..", dataUrl: "Build/..")


def build_files(build: Path) -> list[str]:
    """The names of the files under Build/ that the build's index.html loads, sorted."""
    return sorted(set(BUILD_FILE.findall((build / "index.html").read_text(encoding="utf-8"))))


def package(build: Path, zip_path: Path) -> int:
    if not (build / "index.html").is_file():
        print(f"PACKAGE REFUSED: {build} has no index.html (not a WebGL build folder)")
        return 1
    content = build / "StreamingAssets" / "content"
    if not content.is_dir():
        print(f"PACKAGE REFUSED: {build} has no StreamingAssets/content")
        return 1
    named = build_files(build)
    if missing := [n for n in named if not (build / "Build" / n).is_file()]:
        print("PACKAGE REFUSED: index.html names files that are not in the build: " + ", ".join("Build/" + n for n in missing))
        return 1
    stale = {p for p in (build / "Build").glob("*") if p.is_file() and p.name not in named}
    if (photos := photo_refusal(build)) is not None:
        print(f"PACKAGE REFUSED: {photos}")
        return 1
    ok, message = gate(content)
    print(message)
    if not ok:
        print("PACKAGE REFUSED: the build may not be shared until the sensitivity review is recorded")
        return 1
    zip_path.unlink(missing_ok=True)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:   # index.html at the zip root, as itch.io needs
        for f in sorted(p for p in build.rglob("*") if p.is_file() and p not in stale):
            z.write(f, f.relative_to(build).as_posix())
    for f in sorted(stale):
        print(f"LEFT OUT Build/{f.name} (index.html does not name it: a file of an earlier build)")
    print(f"PACKAGED {zip_path} {zip_path.stat().st_size} bytes")
    return 0


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--build", default=str(REPO / "unity/Builds/Web-gzip"))
    p.add_argument("--zip", default=str(REPO / "unity/Builds/statecraft-m3-web.zip"))
    a = p.parse_args(argv)
    return package(Path(a.build), Path(a.zip))


if __name__ == "__main__":
    sys.exit(main())
