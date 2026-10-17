import os
import re
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
NOTICES = ["LICENSE", "NOTICE", "THIRD-PARTY-NOTICES.md", "LICENSES/CC-BY-4.0.txt", "LICENSES/CC-BY-SA-4.0.txt",
           "LICENSES/CC0-1.0.txt", "LICENSES/YamlDotNet-MIT.txt", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt"]
# Stands in for tools/package_release.py (which needs dotnet and real content): zips the folder, index.html at the root.
STUB = """import argparse, zipfile
from pathlib import Path
a = argparse.ArgumentParser(); a.add_argument("--build"); a.add_argument("--zip"); a = a.parse_args()
b = Path(a.build)
with zipfile.ZipFile(a.zip, "w") as z:
    for f in sorted(p for p in b.rglob("*") if p.is_file()):
        z.write(f, f.relative_to(b).as_posix())
print("PACKAGED", a.zip)
"""


def run(tmp_path, build, out, stub=STUB):
    s = tmp_path / "stub.py"
    s.write_text(stub)
    return subprocess.run(["bash", str(ROOT / "scripts/oss/package_web.sh"), str(build), str(out)], cwd=ROOT,
                          capture_output=True, text=True, env=dict(os.environ, PACKAGE_RELEASE=str(s)))


def fake_build(tmp_path):
    build = tmp_path / "Web-brotli"
    (build / "Build").mkdir(parents=True)
    (build / "index.html").write_text("<html></html>")
    (build / "Build" / "x.data.br").write_bytes(b"\x00")
    return build


def test_the_gate_zip_carries_every_notice(tmp_path):
    out = tmp_path / "game.zip"
    r = run(tmp_path, fake_build(tmp_path), out)
    assert r.returncode == 0, r.stdout + r.stderr
    names = set(zipfile.ZipFile(out).namelist())
    assert "index.html" in names and "Build/x.data.br" in names
    for n in NOTICES:
        assert n in names, n


def test_a_refusing_gate_stops_the_package(tmp_path):
    out = tmp_path / "game.zip"
    r = run(tmp_path, fake_build(tmp_path), out, stub="import sys\nprint('PACKAGE REFUSED: review')\nsys.exit(1)\n")
    assert r.returncode != 0 and "PACKAGE REFUSED" in r.stdout and not out.exists()


def test_package_refuses_a_missing_build(tmp_path):
    r = run(tmp_path, tmp_path / "nope", tmp_path / "z.zip")
    assert r.returncode != 0 and not (tmp_path / "z.zip").exists()


def test_versions_agree():
    sim = re.search(r'Version = "([^"]+)"', (ROOT / "sim/Statecraft.Sim/SimInfo.cs").read_text()).group(1)
    unity = re.search(r"bundleVersion: (\S+)", (ROOT / "unity/ProjectSettings/ProjectSettings.asset").read_text()).group(1)
    assert sim == unity
    released = re.findall(r"^## \[(\d+\.\d+\.\d+)\]", (ROOT / "CHANGELOG.md").read_text(), re.M)
    if released:
        assert released[0] == sim


def test_a_stale_zip_does_not_survive_a_refusal(tmp_path):
    out = tmp_path / "game.zip"
    out.write_bytes(b"stale")
    r = run(tmp_path, fake_build(tmp_path), out, stub="import sys\nprint('PACKAGE REFUSED: review')\nsys.exit(1)\n")
    assert r.returncode != 0 and not out.exists()


def test_release_gate_uses_a_temp_file_not_a_fixed_path():
    text = (ROOT / "scripts/oss/release_gate.sh").read_text()
    assert "mktemp" in text and "/tmp/oss-release-scan.txt" not in text


def test_the_release_web_build_is_gzip_end_to_end():
    """Owner decision on #879 (17 Oct 2026, "gzip for both"): one gzip zip for GitHub Releases and itch.io. itch.io's
    CDN passes a Brotli `.data` to the player uncompressed but keeps a gzip `.data` compressed. The release recipe in
    RELEASING.md step 5 must build gzip and check and package that same folder, and the packager's default must agree."""
    text = (ROOT / "docs/RELEASING.md").read_text()
    step5 = text[text.index("5. **Build and package**"):text.index("6. **Tag and publish")]
    build = re.search(r"^.*Build\.Web -compression (\S+).*$", step5, re.M)   # the first Build.Web line: the release build
    assert build and build.group(1) == "gzip", "the release build must use -compression gzip"
    assert "-decompressionFallback" not in build.group(0)   # itch.io sends Content-Encoding: no fallback (Build.Web)
    assert "uv run tools/web_check.py unity/Builds/Web-gzip " in step5
    assert "bash scripts/oss/package_web.sh unity/Builds/Web-gzip " in step5
    assert "Web-brotli" not in step5
    packager = (ROOT / "tools/package_release.py").read_text()
    assert 'default=str(REPO / "unity/Builds/Web-gzip")' in packager
