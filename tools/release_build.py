# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Build the desktop players on the owner's Mac and write a build record (refs #54; Phases 0-1 of the
plan in PR #482, spec
docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md, sections 6.4 and 6.5).

Usage: uv run tools/release_build.py [--platform mac|linux|windows|all] [--out DIR] [--tag vX.Y.Z] [--dev] [--dry-run]
                                    [--unity PATH]

- Refuses, before anything is built (spec 6.4 step 1):
  - a dirty working tree (tracked changes or untracked files);
  - a release (no --dev) unless HEAD is on main and contained in refs/remotes/origin/main (as last fetched; the script
    never fetches), and the tag (--tag, default v<bundleVersion>) is v<bundleVersion>[-suffix] and points at HEAD;
  - an editor whose version (its Info.plist) is not the one in unity/ProjectSettings/ProjectVersion.txt;
  - an --out inside any checkout of the repository (the main one and every worktree, ignored paths included, compared
    after resolving symlinks, .. and case), or an --out that exists and is not empty.
  A --dev build runs on any branch and commit; it takes no --tag, is not -shareable, and its archives are named
  v<bundleVersion>-dev.<commit8>, so they can never pass for a release's assets.
- Checks the Unity modules each selected platform needs and fails (exit 2) naming the ones to add in Unity Hub.
- Builds the sim DLLs (tools/build_sim_for_unity.py), then each player: macOS IL2CPP (Build.Mac), Linux and Windows on
  Mono (Build.LinuxMono, Build.WindowsMono; decisions O7 and O8). Every Unity run goes through
  `uv run tools/machine_lock.py --shared --wait-unity`, never with -nographics, and is followed by
  `git checkout -- unity/ProjectSettings unity/Assets/Settings` to drop the settings churn (also on an exception). Release
  builds are -shareable (the sensitivity gate).
- Runs the player golden check (docs/determinism.md) on the macOS player, the only one the Mac can run:
  `-batchmode -nographics -golden -logFile <absolute>`, under the shared lock. It passes only on exit 0 with both
  `GOLDEN OK` lines carrying the hashes pinned in sim/Statecraft.Sim.Tests/golden/manifest.txt. The Linux and Windows
  players are recorded as unchecked, for the CI player check (spec 6.3).
- Archives each player under the asset name the CI player check expects (PR #533's contract; ARCHIVES below):
  statecraft-israel-<tag>-macos-universal.zip (the .app zipped by ditto --norsrc), statecraft-israel-<tag>-linux-x64.tar.gz
  and statecraft-israel-<tag>-windows-x64.zip. It checks first that the folder holds exactly one player executable.
  Every archive carries the notices at its top level, beside the player (RELEASING.md step 7): LICENSE, NOTICE,
  THIRD-PARTY-NOTICES.md, OFL-EBGaramond.txt, OFL-FrankRuhlLibre.txt and LICENSES/, the web packager's list (REQUIRED_NOTICES). It refuses before
  building when one is missing from the repository, and lists each archive afterwards to prove they are in it. An
  archive leaves out Unity's *_DoNotShip folders and event-photos.json, the build's photo manifest (packaging
  evidence the player never reads), which is copied beside the record as event-photos-<platform>.json.
- Writes build-record.json, build-record.md and files-<platform>.sha256: one `shasum -a 256` line per file the archive
  holds, so `shasum -a 256 -c` run where the archive was unpacked checks every file. The record carries the SHA-256 and
  size of each archive and file list, and whether the tree was still clean after the builds.

The output folder defaults to ~/statecraft-release/v<version>-<commit>-<UTC stamp>/: outside every worktree, so removing
a worktree or cleaning unity/Builds cannot take a release with it, and outside git, so nothing in it can be committed
by accident. Build records live in the owner's
separate records repository (decision O12): this script never commits, pushes or uploads anything; it prints where the
record is, and the owner copies it.

Exit 0 when every build succeeded and every check that ran passed; 1 on a refusal, a failed build or a failed golden
(the record is still written for a failed golden); 2 when the editor or a module is missing.
"""
import argparse
import hashlib
import json
import os
import plistlib
import re
import subprocess
import sys
import tarfile
import time
import zipfile
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

import player_golden_check as pgc   # tools/: the one asset-name contract, tag rule and golden pass rule (issue #546)

REPO = Path(__file__).resolve().parents[1]
LOCK_TIMEOUT = 30 * 24 * 3600   # machine_lock's own default gives up after an hour; a release waits for the machine
HUB = "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
METHOD = "Statecraft.Game.EditorTools.Build."
# The licence and attribution files every archive carries beside the player (RELEASING.md step 7): the list
# scripts/oss/package_web.sh copies into the web zip and checks (test_the_notice_list_is_the_web_packagers keeps them
# one list). Archive name -> source in the repository; LICENSES/ is copied whole, as the web packager does.
REQUIRED_NOTICES = pgc.REQUIRED_NOTICES   # one list with the CI player check, which checks every archive has them
# Each font's own OFL text (its copyright line differs), under the names site/public/fonts uses. Every OFL.txt under
# unity/Assets/UI/Fonts must be here (test_every_shipped_fonts_licence_is_a_required_notice).
FONT_LICENCES = {"OFL-EBGaramond.txt": "unity/Assets/UI/Fonts/OFL.txt",
                 "OFL-FrankRuhlLibre.txt": "unity/Assets/UI/Fonts/FrankRuhlLibre/OFL.txt"}
NOTICE_TOPS = pgc.NOTICE_TOPS
PHOTOS = "event-photos.json"   # Build.PhotoManifestName: written beside every player, read by packaging, not the player
# The release asset names and the tag rule are the CI player golden check's (tools/player_golden_check.py), so the two
# cannot drift (issue #546). {tag} carries the v. The CI check names the Mac "macos".
ARCHIVES = {"mac": pgc.ASSETS["macos"], "linux": pgc.ASSETS["linux"], "windows": pgc.ASSETS["windows"]}
TAG = pgc.TAG


@dataclass(frozen=True)
class Platform:
    name: str
    method: str          # Build.<method>
    build_target: str    # Unity's -buildTarget
    backend: str
    module_id: str       # Unity Hub's module id
    module_name: str
    module_dir: str      # where the module lives, relative to the editor folder (modules.json's destination)
    output: str          # the player's path inside players/<name>/
    runs_here: bool      # whether the Mac can run this player's golden check


PLATFORMS = {
    "mac": Platform("mac", "Mac", "OSXUniversal", "IL2CPP", "mac-il2cpp", "Mac Build Support (IL2CPP)",
                    "Unity.app/Contents/PlaybackEngines/MacStandaloneSupport", "Statecraft.app", True),
    "linux": Platform("linux", "LinuxMono", "Linux64", "Mono", "linux-mono", "Linux Build Support (Mono)",
                      "PlaybackEngines/LinuxStandaloneSupport", "Statecraft.x86_64", False),
    "windows": Platform("windows", "WindowsMono", "Win64", "Mono", "windows-mono", "Windows Build Support (Mono)",
                        "PlaybackEngines/WindowsStandaloneSupport", "Statecraft.exe", False),
}


@dataclass
class Result:
    returncode: int
    stdout: str = ""


def run(cmd: list, **kw) -> Result:
    """The one place a process is started (tests replace it). Output is captured only when asked for."""
    capture = kw.pop("capture", False)
    p = subprocess.run([str(c) for c in cmd], text=True, capture_output=capture, **kw)
    return Result(p.returncode, p.stdout if capture else "")


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def parse(argv: list[str]) -> argparse.Namespace:
    ap = argparse.ArgumentParser(description="Build the desktop players and write a build record (never pushes).")
    ap.add_argument("--platform", choices=["mac", "linux", "windows", "all"], default="all")
    ap.add_argument("--out", type=Path, default=None, help="output folder (default ~/statecraft-release/<run>)")
    ap.add_argument("--dev", action="store_true", help="a development build: no -shareable, so not for sharing")
    ap.add_argument("--dry-run", action="store_true", help="check, then print the commands without running them")
    ap.add_argument("--tag", default=None, help="the release tag in the archive names, v included (default "
                                                "v<bundleVersion>)")
    ap.add_argument("--unity", type=Path, default=None, help="the Unity executable (default: the Hub path of the "
                                                             "version in ProjectVersion.txt)")
    return ap.parse_args(argv)


def selected(a: argparse.Namespace) -> list[str]:
    return list(PLATFORMS) if a.platform == "all" else [a.platform]


# --- what the repository says ------------------------------------------------------------------------------------------

def unity_version() -> tuple[str, str]:
    text = (REPO / "unity/ProjectSettings/ProjectVersion.txt").read_text()
    m = re.search(r"m_EditorVersionWithRevision:\s*(\S+)\s*\((\w+)\)", text)
    if not m:
        raise SystemExit("cannot read the Unity version from unity/ProjectSettings/ProjectVersion.txt")
    return m.group(1), m.group(2)


def versions() -> dict:
    bundle = re.search(r"^\s*bundleVersion:\s*(\S+)", (REPO / "unity/ProjectSettings/ProjectSettings.asset").read_text(), re.M)
    sim = re.search(r'Version\s*=\s*"([^"]+)"', (REPO / "sim/Statecraft.Sim/SimInfo.cs").read_text())
    return {"bundle_version": bundle.group(1) if bundle else None, "sim_version": sim.group(1) if sim else None}


def expected_goldens() -> dict[str, str]:
    pins = {}
    for line in (REPO / "sim/Statecraft.Sim.Tests/golden/manifest.txt").read_text().splitlines():
        name, _, value = line.partition(" ")
        if name and not name.startswith("#"):
            pins[name] = value.strip()
    return pins


def editor_version(unity: Path) -> tuple[str, str] | None:
    """(version, revision) from the editor's Info.plist ("Unity version 6000.3.25f1 (e1dba0a9aba4). ..."), or None."""
    try:
        with open(unity.parents[1] / "Info.plist", "rb") as f:
            info = plistlib.load(f).get("CFBundleGetInfoString", "")
    except (OSError, plistlib.InvalidFileException):
        return None
    m = re.search(r"Unity version (\S+) \((\w+)\)", info)
    return (m.group(1), m.group(2)) if m else None


def default_out(commit: str, version: str, stamp: str) -> Path:
    return Path.home() / "statecraft-release" / f"v{version}-{commit[:8]}-{stamp}"


# --- checks --------------------------------------------------------------------------------------------------------------

def git(*args: str) -> Result:
    return run(["git", *args], cwd=REPO, capture=True)


def dirty_lines() -> list[str]:
    return [l for l in git("status", "--porcelain", "--untracked-files=normal").stdout.splitlines() if l.strip()]


def missing_modules(names: list[str], editor_root: Path) -> list[Platform]:
    return [PLATFORMS[n] for n in names if not (editor_root / PLATFORMS[n].module_dir).is_dir()]


def modules_message(missing: list[Platform], version: str) -> str:
    rows = "\n".join(f"  - {p.module_name} (Unity Hub module id {p.module_id}), for --platform {p.name}" for p in missing)
    ids = " ".join(f"-m {p.module_id}" for p in missing)
    return (f"MISSING UNITY MODULES for Unity {version}:\n{rows}\n"
            f"Add them in Unity Hub (Installs > {version} > Manage > Add modules), or from the command line:\n"
            f'  "{HUB}" -- --headless install-modules -v {version} {ids}\n'
            "Nothing was built.")


def checkouts() -> list[Path]:
    """This checkout, the main one and every worktree of the repository."""
    listed = git("worktree", "list", "--porcelain").stdout.splitlines()
    return [REPO, *(Path(l[len("worktree "):]) for l in listed if l.startswith("worktree "))]


def canonical(path: Path) -> str:
    """The path with symlinks (dangling ones included), .. and, for APFS, which ignores case, case resolved."""
    return os.path.realpath(os.path.abspath(path)).casefold()


def inside_a_checkout(out: Path) -> Path | None:
    """The checkout that contains out, or None. Ignored paths count: ContentSync deletes
    unity/Assets/StreamingAssets/content on every build, and unity/Library and unity/Builds belong to Unity."""
    target = canonical(out)
    for root in checkouts():
        r = canonical(root)
        if target == r or target.startswith(r.rstrip(os.sep) + os.sep):
            return root
    return None


# --- commands ------------------------------------------------------------------------------------------------------------

def locked(cmd: list, wait_unity: bool) -> list[str]:
    head = ["uv", "run", str(REPO / "tools/machine_lock.py"), "--shared"]
    if wait_unity:
        head.append("--wait-unity")
    return [*head, "--timeout", str(LOCK_TIMEOUT), "--", *[str(c) for c in cmd]]


def build_command(unity: Path, p: Platform, target: Path, log: Path, shareable: bool) -> list[str]:
    cmd = [str(unity), "-batchmode", "-CI", "false", "-GITHUB_ACTIONS", "false", "-TF_BUILD", "false", "-quit",
           "-projectPath", str(REPO / "unity"), "-buildTarget", p.build_target, "-executeMethod", METHOD + p.method,
           "-out", str(target), "-logFile", str(log)]
    if shareable:
        cmd.append("-shareable")
    return locked(cmd, wait_unity=True)


def mac_executable(app: Path) -> Path:
    with open(app / "Contents/Info.plist", "rb") as f:
        return app / "Contents/MacOS" / plistlib.load(f)["CFBundleExecutable"]


def golden_command(player: Path, log: Path) -> list[str]:
    # A player, not the editor: docs/determinism.md runs it -batchmode -nographics, with an absolute -logFile.
    return locked([str(player), "-batchmode", "-nographics", "-golden", "-logFile", str(log.resolve())], wait_unity=False)


# --- results -------------------------------------------------------------------------------------------------------------

def golden_lines(log_text: str) -> list[str]:
    """The GOLDEN lines as the player printed them (stripped), for the build record."""
    return [l.strip() for l in log_text.splitlines() if l.strip().startswith(("GOLDEN OK ", "GOLDEN FAIL "))]


def golden_status(log_text: str, exit_code: int, expected: dict[str, str]) -> str:
    """pass only on exit 0, every manifest `GOLDEN OK <name> <pinned hash>` line exactly as the player prints it (bare, one per
    line), and no GOLDEN FAIL or BOOT FAIL line: player_golden_check.check_log, the rule CI applies (issue #546)."""
    if not expected or any(not re.fullmatch(r"[0-9a-f]{64}", value) for value in expected.values()): return "fail"
    wanted = [f"GOLDEN OK {name} {value}" for name, value in expected.items()]
    return "pass" if not pgc.check_log(log_text, wanted, exit_code) else "fail"


def archive_name(tag: str, platform: str) -> str:
    return ARCHIVES[platform].format(tag=tag)


def notice_sources() -> dict[str, Path]:
    """Archive name -> file in the repository: the three root files, each font's licence (FONT_LICENCES), and every
    LICENSES/*.txt (as package_web.sh copies them)."""
    sources = {name: REPO / name for name in ("LICENSE", "NOTICE", "THIRD-PARTY-NOTICES.md")}
    sources.update({name: REPO / src for name, src in FONT_LICENCES.items()})
    for f in sorted((REPO / "LICENSES").glob("*.txt")):
        sources[f"LICENSES/{f.name}"] = f
    for name in REQUIRED_NOTICES:
        sources.setdefault(name, REPO / name)   # a required licence text that is missing: reported by missing_sources
    return sources


def missing_sources() -> list[str]:
    return [str(src.relative_to(REPO)) for src in notice_sources().values() if not src.is_file()]


def add_notices(folder: Path) -> None:
    """Copies the notices into the player folder, beside the player, so the archive and its file list carry them."""
    for name, src in notice_sources().items():
        (folder / name).parent.mkdir(parents=True, exist_ok=True)
        (folder / name).write_bytes(src.read_bytes())


def archive_names(archive: Path) -> list[str]:
    if archive.name.endswith(".tar.gz"):
        with tarfile.open(archive) as t:
            return [m.name for m in t.getmembers() if not m.isdir()]
    with zipfile.ZipFile(archive) as z:
        return [n for n in z.namelist() if not n.endswith("/")]


def missing_notices(archive: Path) -> list[str]:
    """The required notices the archive itself lacks (the artefact is checked, as package_web.sh does)."""
    names = set(archive_names(archive))
    return sorted(n for n in REQUIRED_NOTICES if n not in names)


def make_archive(p: Platform, folder: Path, target: Path, dest: Path) -> None:
    """The release archive of one player folder, notices included. macOS: ditto keeps the bundle (modes, links) and
    zips the .app; the notices are then added at the zip's top level. Linux: tar keeps the executable bit. Windows: a
    zip of the folder."""
    if p.name == "mac":
        # --norsrc: no AppleDouble ._* entries, which unzip would leave as files the file list does not name.
        if run(["ditto", "-c", "-k", "--norsrc", "--keepParent", str(target), str(dest)]).returncode != 0:
            raise SystemExit(f"ditto failed for {target}")
        with zipfile.ZipFile(dest, "a", zipfile.ZIP_DEFLATED) as z:
            for path in shipped(folder, mac=True):
                if path.is_file() and path.relative_to(folder).parts[0] in NOTICE_TOPS:
                    z.write(path, path.relative_to(folder).as_posix())
    elif p.name == "linux":
        with tarfile.open(dest, "w:gz") as t:
            for path in shipped(folder):
                t.add(path, arcname=path.relative_to(folder).as_posix(), recursive=False)
    else:
        with zipfile.ZipFile(dest, "w", zipfile.ZIP_DEFLATED) as z:
            for path in shipped(folder):
                z.write(path, path.relative_to(folder).as_posix())


def player_problem(p: Platform, folder: Path) -> str | None:
    """Why the player folder breaks the archive contract, or None: the CI player check's own find_player (one player
    .exe, crash handler aside; one *.x86_64; or one .app with one file in Contents/MacOS), so the two cannot drift."""
    try:
        pgc.find_player(folder, "macos" if p.name == "mac" else p.name)
    except ValueError as e:
        return str(e)
    return None


def file_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def unity_seconds(log_text: str) -> int | None:
    m = re.search(r"BUILD Succeeded \S+ -> .*?, (\d+)s", log_text)
    return int(m.group(1)) if m else None


def shipped(folder: Path, mac: bool = False) -> list[Path]:
    """Every path under folder that the archive carries, sorted: Unity's own *_DoNotShip and
    *_ButDontShipItWithYourGame folders (IL2CPP sources, Burst debug data) and the photo manifest are left out. For the
    Mac (mac=True) only the .app and the notices are kept: the zip holds nothing else."""
    def skip(path: Path) -> bool:
        top = path.relative_to(folder).parts[0]
        if mac:
            return not (top.endswith(".app") or top in NOTICE_TOPS)
        return top.endswith("_DoNotShip") or top.endswith("_ButDontShipItWithYourGame") or top == PHOTOS
    return sorted(p for p in folder.rglob("*") if not skip(p))


def tree_manifest(folder: Path, mac: bool = False) -> tuple[str, int, int]:
    """`shasum -a 256` lines for every file under folder, sorted by path; a symlink is listed by its target, not
    followed (`shasum -c` skips such a line with a warning); the do-not-ship folders are left out. Returns the text, the number of entries and the bytes of the regular files."""
    rows, size = [], 0
    for path in shipped(folder, mac):
        rel = path.relative_to(folder).as_posix()
        if path.is_symlink():
            rows.append((rel, f"symlink  {rel} -> {os.readlink(path)}"))
        elif path.is_file():
            size += path.stat().st_size
            rows.append((rel, f"{file_sha256(path)}  {rel}"))
    rows.sort(key=lambda r: r[0])
    return "".join(line + "\n" for _, line in rows), len(rows), size


def markdown(r: dict) -> str:
    out = [f"# Build record: Statecraft: Israel {r['version']['bundle_version']} ({r['commit'][:8]})", "",
           f"- Commit: `{r['commit']}` ({r['describe']}, branch {r['branch']}); tree clean before the builds: "
           f"{r['tree_clean']}, after: {r['tree_clean_after_builds']}"
           + (f" ({'; '.join(r['dirty_after_builds'])})" if r["dirty_after_builds"] else ""),
           f"- {'Release' if r['release'] else 'Development build, not a release'}: tag `{r['tag']}`",
           f"- Date (UTC): {r['date_utc']}",
           f"- Unity: {r['unity']['version']} ({r['unity']['revision']})",
           f"- Version: bundleVersion {r['version']['bundle_version']}, SimInfo.Version {r['version']['sim_version']}",
           f"- Shareable build: {r['shareable']}" + ("" if r["shareable"] else " (a --dev build: not for sharing)"),
           f"- Signing: {r['signing']}", "",
           "| Platform | Backend | Architecture | Archive | Archive bytes | Archive SHA-256 | Player files | Player bytes "
           "| File list SHA-256 | Golden |",
           "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"]
    for a in r["artifacts"]:
        ar = a["archive"]
        out.append(f"| {a['platform']} | {a['backend']} | {a['architecture'] or '-'} | `{ar['name']}` | {ar['bytes']} | "
                   f"`{ar['sha256']}` | {a['files']} | {a['bytes']} | `{a['file_list_sha256']}` | {a['golden']['status']} |")
    out.append("")
    for a in r["artifacts"]:
        g = a["golden"]
        out.append(f"- {a['platform']}: {g['status']}" + (f", exit {g['exit_code']}" if g["exit_code"] is not None else "")
                   + (f"; {g['note']}" if g.get("note") else ""))
        out.extend(f"  - `{line}`" for line in g["lines"])
    out += ["", "The archive is the release asset. Each file list (`" + "`, `".join(a["file_list"] for a in r["artifacts"])
            + "`) names exactly the files its archive holds; `shasum -a 256 -c` on it, run where the archive was "
            "unpacked, checks every regular file (it skips `symlink` lines, which give a link's target instead). Each archive "
            "carries the licence and attribution files beside the player. The build's photo manifest is kept beside the "
            "record (`event-photos-<platform>.json`), not in the archive.",
            "Steam build ID: (added by the owner after an upload).", ""]
    return "\n".join(out)


# --- the run -------------------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    a = parse(sys.argv[1:] if argv is None else argv)
    names = selected(a)
    version, revision = unity_version()
    unity = a.unity or Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity")
    if not unity.is_file():
        print(f"NO UNITY EDITOR at {unity}: install Unity {version} ({revision}) through Unity Hub (docs/dev-setup.md)",
              file=sys.stderr)
        return 2
    found = editor_version(unity)
    if found != (version, revision):
        have = f"{found[0]} ({found[1]})" if found else "an unknown version"
        print(f"REFUSED: {unity} is Unity {have}, but unity/ProjectSettings/ProjectVersion.txt asks for {version} "
              f"({revision}); the record names the project's version, so build with that editor.", file=sys.stderr)
        return 1
    editor_root = unity.parents[3]   # <version>/Unity.app/Contents/MacOS/Unity
    missing = missing_modules(names, editor_root)
    if missing:
        print(modules_message(missing, version), file=sys.stderr)
        return 2
    dirty = dirty_lines()
    if dirty:
        print("REFUSED: the working tree is not clean. A build record names a commit, so build only a committed tree.\n"
              + "\n".join("  " + l for l in dirty)
              + "\nAfter a Unity run, `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset` "
                "drops the settings churn (and the UNITY_MCP_* defines).", file=sys.stderr)
        return 1
    if missing := missing_sources():
        print("REFUSED: notice files every archive must carry are missing from the repository: " + ", ".join(missing)
              + " (RELEASING.md step 7).", file=sys.stderr)
        return 1

    commit = git("rev-parse", "HEAD").stdout.strip()
    branch = git("rev-parse", "--abbrev-ref", "HEAD").stdout.strip()
    describe = git("describe", "--tags", "--always", "--dirty").stdout.strip()
    vers = versions()
    started = utc_now()
    release = not a.dev
    if release:
        tag = a.tag or f"v{vers['bundle_version']}"
        if not TAG.fullmatch(tag):
            print(f"REFUSED: {tag!r} is not a release tag (vX.Y.Z)", file=sys.stderr)
            return 1
        if tag != f"v{vers['bundle_version']}" and not tag.startswith(f"v{vers['bundle_version']}-"):
            print(f"REFUSED: the tag {tag} is not bundleVersion {vers['bundle_version']} (v{vers['bundle_version']}, "
                  "optionally with a -suffix); the archives would name a version the player does not carry.",
                  file=sys.stderr)
            return 1
        if branch != "main":
            where = "HEAD is detached; check out main" if branch == "HEAD" else f"not on {branch!r}"
            print(f"REFUSED: a release is built on main ({where}; spec 6.4 step 1). Use --dev for a test build.",
                  file=sys.stderr)
            return 1
        if git("merge-base", "--is-ancestor", "HEAD", "refs/remotes/origin/main").returncode != 0:
            print(f"REFUSED: HEAD ({commit[:8]}) is not on origin/main as this checkout last saw it; a release is a "
                  "reviewed commit on GitHub's main. Run `git fetch origin` (this script never does) and check that "
                  "your main matches origin/main.", file=sys.stderr)
            return 1
        if tag not in git("tag", "--points-at", "HEAD").stdout.split():
            print(f"REFUSED: the tag {tag} does not point at HEAD ({commit[:8]}); tag the release commit first, or use "
                  "--dev for a test build.", file=sys.stderr)
            return 1
    else:
        if a.tag:
            print("REFUSED: a --dev build takes no --tag; its archives are named v<bundleVersion>-dev.<commit>.",
                  file=sys.stderr)
            return 1
        tag = f"v{vers['bundle_version']}-dev.{commit[:8]}"
    out = (a.out or default_out(commit, vers["bundle_version"] or "0", started.strftime("%Y%m%dT%H%M%SZ"))).absolute()
    if (root := inside_a_checkout(out)) is not None:
        print(f"REFUSED: {out} is inside the checkout {root}; choose a folder outside every checkout of the repository "
              "(the default is ~/statecraft-release/).", file=sys.stderr)
        return 1
    if out.exists() and (not out.is_dir() or any(out.iterdir())):
        print(f"REFUSED: {out} exists and is not empty; a release folder starts empty.", file=sys.stderr)
        return 1
    shareable = release
    plan = [(PLATFORMS[n], out / "players" / n / PLATFORMS[n].output, out / "logs" / f"unity-build-{n}.log") for n in names]
    sim = [sys.executable, str(REPO / "tools/build_sim_for_unity.py")]
    if a.dry_run:
        print("DRY RUN: nothing is built or written.\n" + " ".join(sim))
        for p, target, log in plan:
            print(" ".join(f'"{c}"' if " " in c else c for c in build_command(unity, p, target, log, shareable)))
            print("git checkout -- unity/ProjectSettings unity/Assets/Settings")
            print(f"archive: {out / archive_name(tag, p.name)}")
            if p.runs_here:
                print(" ".join(golden_command(target / "Contents/MacOS/<CFBundleExecutable>", out / "logs/golden-mac.log")))
        print(f"record: {out / 'build-record.json'}")
        return 0

    (out / "logs").mkdir(parents=True, exist_ok=True)
    if run(sim, cwd=REPO).returncode != 0:
        print("FAILED: tools/build_sim_for_unity.py", file=sys.stderr)
        return 1
    expected = expected_goldens()
    artifacts, all_ok = [], True
    for p, target, log in plan:
        print(f"release_build: building {p.name} ({p.backend}) -> {target}", flush=True)
        t0 = time.monotonic()
        try:
            code = run(build_command(unity, p, target, log, shareable), cwd=REPO).returncode
        finally:
            run(["git", "checkout", "--", "unity/ProjectSettings", "unity/Assets/Settings"], cwd=REPO)
        wall = round(time.monotonic() - t0)
        log_text = log.read_text(errors="replace") if log.exists() else ""
        if code != 0 or not target.exists():
            print(f"FAILED: the {p.name} build (exit {code}); see {log}", file=sys.stderr)
            return 1
        folder = out / "players" / p.name
        add_notices(folder)
        text, files, size = tree_manifest(folder, mac=p.name == "mac")
        listing = out / f"files-{p.name}.sha256"
        listing.write_text(text)
        photos = None
        if (folder / PHOTOS).is_file():
            copy = out / f"event-photos-{p.name}.json"
            copy.write_bytes((folder / PHOTOS).read_bytes())
            photos = {"file": copy.name, "sha256": file_sha256(copy)}
        if problem := player_problem(p, folder):
            print(f"FAILED: {problem}", file=sys.stderr)
            return 1
        archive = out / archive_name(tag, p.name)
        make_archive(p, folder, target, archive)
        if lacking := missing_notices(archive):
            print(f"FAILED: {archive.name} lacks the notices {', '.join(lacking)}", file=sys.stderr)
            return 1
        arch = None
        if p.name == "mac":
            arch = run(["lipo", "-archs", str(mac_executable(target))], capture=True).stdout.strip() or None
        golden = {"status": "unchecked", "exit_code": None, "lines": [],
                  "note": "this player cannot run on the Mac; the CI player golden check runs it (spec 6.3)"}
        if p.runs_here:
            glog = out / "logs" / f"golden-{p.name}.log"
            gcode = run(golden_command(mac_executable(target), glog), cwd=REPO).returncode
            gtext = glog.read_text(errors="replace") if glog.exists() else ""
            golden = {"status": golden_status(gtext, gcode, expected), "exit_code": gcode, "lines": golden_lines(gtext),
                      "log": str(glog.relative_to(out))}
            all_ok &= golden["status"] == "pass"
        artifacts.append({"platform": p.name, "build_target": p.build_target, "backend": p.backend,
                          "architecture": arch, "build_method": METHOD + p.method,
                          "path": str(target.relative_to(out)), "files": files, "bytes": size,
                          "archive": {"name": archive.name, "bytes": archive.stat().st_size,
                                      "sha256": file_sha256(archive)},
                          "file_list_sha256": file_sha256(listing), "file_list": listing.name,
                          "photo_manifest": photos,
                          "unity_build_seconds": unity_seconds(log_text), "wall_seconds_with_lock_wait": wall,
                          "unity_log": str(log.relative_to(out)), "golden": golden})

    dirty_after = [l.strip() for l in dirty_lines()]
    record = {"schema": 1, "tool": "tools/release_build.py", "commit": commit,
              "describe": describe, "branch": branch, "release": release,
              "tree_clean": True,   # checked before the builds: a dirty tree is refused
              "tree_clean_after_builds": not dirty_after, "dirty_after_builds": dirty_after,
              "date_utc": started.strftime("%Y-%m-%dT%H:%M:%SZ"), "unity": {"version": version, "revision": revision},
              "version": vers, "tag": tag, "shareable": shareable, "signing": "none", "golden_expected": expected,
              "artifacts": artifacts, "steam_build_id": None}
    (out / "build-record.json").write_text(json.dumps(record, indent=2) + "\n")
    (out / "build-record.md").write_text(markdown(record))
    print(f"RECORD {out / 'build-record.json'}\nRECORD {out / 'build-record.md'}")
    for a_ in artifacts:
        print(f"{a_['archive']['name']}: sha256 {a_['archive']['sha256']}, {a_['archive']['bytes']} bytes, "
              f"golden {a_['golden']['status']}")
    print("Nothing was pushed or uploaded. Copy the record files to the build records repository.")
    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
