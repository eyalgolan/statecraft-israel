"""tools/package_release.py: the itch.io zip is made only after the owner's sensitivity review of every sensitive event
(spec 5.5; M3 rereview). The gate is the sim's own (Statecraft.Cli --release-gate), run on the content inside the build."""
import json
import re
import shutil
import zipfile
from pathlib import Path

import package_release

REPO = Path(package_release.REPO)
SENSITIVE_PHOTOS = ["altalena", "bernadotte_assassination", "burma_road_complete", "operation_hiram", "operation_horev", "operation_yoav", "ten_days"]
SENSITIVE = ["altalena", "bernadotte_assassination", "burma_road_complete", "operation_hiram", "operation_horev", "operation_yoav", "reparations_claim", "ten_days"]


def photo_manifest(build, reviewed=True, shareable=True):
    """The event-photos.json Build.Web writes (Build.PhotoManifestJson): the 7 sensitive photographs and one other."""
    photos = [{"event_id": e, "sensitive": True, "sensitivity_reviewed": reviewed} for e in SENSITIVE_PHOTOS]
    photos.append({"event_id": "maabarot_policy", "sensitive": False, "sensitivity_reviewed": False})
    (build / package_release.PHOTO_MANIFEST).write_text(json.dumps({"version": 1, "shareable": shareable, "photos": photos}))


def fake_build(tmp_path, reviewed, photos_reviewed=True, forces_checked=True):
    """A build folder with index.html, the real content copied into StreamingAssets/content and the photo manifest.
    forces_checked: the starting forces' rows in units_1948.yaml are fact-checked, as the owner's person check of
    21 October 2026 left them (R-UNIT-006, Q-START-3; #1240); False sets every row back to draft, as before it."""
    build = tmp_path / "Web-brotli"
    (build / "StreamingAssets").mkdir(parents=True)
    (build / "index.html").write_text("<html></html>")
    content = build / "StreamingAssets" / "content"
    shutil.copytree(REPO / "content", content)
    status = "sensitivity-reviewed" if reviewed else "fact-checked"   # the owner's sign-off (D11), or its absence
    for e in SENSITIVE:
        f = content / "events" / f"{e}.yaml"
        f.write_text(re.sub(r"(?m)^status: .*$", f"status: {status}", f.read_text()))
    units = content / "map" / "units_1948.yaml"
    forces = "fact-checked" if forces_checked else "draft"
    units.write_text(re.sub(r"(?m)^  status: .*$", f"  status: {forces}", units.read_text(encoding="utf-8")), encoding="utf-8")
    targets = content / "settlement_targets.yaml"   # #793's rows, as the owner's review would leave them (Q-TGT-3 (b))
    targets.write_text(re.sub(r"(?m)^    status: .*$", f"    status: {status}", targets.read_text(encoding="utf-8")), encoding="utf-8")
    photo_manifest(build, reviewed=photos_reviewed)
    return build


def test_packaging_is_refused_until_every_sensitive_event_is_reviewed(tmp_path, capsys):
    build, zip_path = fake_build(tmp_path, reviewed=False), tmp_path / "out.zip"
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 1
    assert not zip_path.exists()
    out = capsys.readouterr().out
    assert "PACKAGE REFUSED" in out
    for e in SENSITIVE:
        assert e in out


def test_packaging_is_refused_until_every_starting_row_is_fact_checked(tmp_path, capsys):
    # R-UNIT-006, Q-START-3 (#743): the rows the sim starts a unit from are game state, so they are checked first.
    build, zip_path = fake_build(tmp_path, reviewed=True, forces_checked=False), tmp_path / "out.zip"
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 1
    assert not zip_path.exists()
    out = capsys.readouterr().out
    assert "PACKAGE REFUSED" in out
    for row in ("golani", "egyptian_expeditionary_force", "arab_liberation_army"):
        assert row in out


def test_packaging_writes_the_zip_once_all_are_reviewed(tmp_path, capsys):
    build, zip_path = fake_build(tmp_path, reviewed=True), tmp_path / "out.zip"
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 0
    with zipfile.ZipFile(zip_path) as z:
        names = z.namelist()
    assert "index.html" in names                                       # at the zip root, as itch.io needs
    assert "StreamingAssets/content/events/altalena.yaml" in names
    assert "PACKAGED" in capsys.readouterr().out


# Review I1: the event photographs are packed into Unity's data files, so the build's own manifest is what is checked.
def refused(build, tmp_path, capsys):
    zip_path = tmp_path / "out.zip"
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 1
    assert not zip_path.exists()
    out = capsys.readouterr().out
    assert "PACKAGE REFUSED" in out
    return out


def test_a_build_without_the_photo_manifest_is_refused(tmp_path, capsys):
    build = fake_build(tmp_path, reviewed=True)
    (build / package_release.PHOTO_MANIFEST).unlink()
    assert "event-photos.json" in refused(build, tmp_path, capsys)


def test_unreviewed_sensitive_photographs_are_refused(tmp_path, capsys):
    out = refused(fake_build(tmp_path, reviewed=True, photos_reviewed=False), tmp_path, capsys)
    for e in SENSITIVE_PHOTOS:
        assert e in out
    assert "maabarot_policy" not in out                                # not sensitive: nothing to review


def test_a_dev_build_is_refused_even_with_reviewed_photographs(tmp_path, capsys):
    build = fake_build(tmp_path, reviewed=True)
    photo_manifest(build, reviewed=True, shareable=False)
    assert "-shareable" in refused(build, tmp_path, capsys)


def test_a_malformed_photo_manifest_is_refused(tmp_path, capsys):
    build = fake_build(tmp_path, reviewed=True)
    for bad in ["{", '{"shareable": true}', '{"shareable": "yes", "photos": []}', '{"shareable": true, "photos": [{"event_id": "x"}]}']:
        (build / package_release.PHOTO_MANIFEST).write_text(bad)
        assert "not a valid photo manifest" in refused(build, tmp_path, capsys)


# Issue #437: Build.Web names the files under Build/ by their content hash, so the names differ from build to build and
# index.html is the only place that says which files belong to this build.
HASHED = ["0a1b2c3d4e5f60718293a4b5c6d7e8f9.loader.js", "11112222333344445555666677778888.framework.js.br",
          "9999aaaabbbbccccddddeeeeffff0000.data.br", "abcdefabcdefabcdefabcdefabcdefab.wasm.br"]


def hashed_build(tmp_path):
    """fake_build with a Build/ folder of hashed names and an index.html that names them, as the template writes it."""
    build = fake_build(tmp_path, reviewed=True)
    loader, framework, data, code = HASHED
    (build / "index.html").write_text(
        f'<html><script src="Build/{loader}"></script><script>var config = {{\n  dataUrl: "Build/{data}",\n'
        f'  frameworkUrl: "Build/{framework}",\n  codeUrl: "Build/{code}",\n  streamingAssetsUrl: "StreamingAssets",\n}};'
        "</script></html>")
    (build / "Build").mkdir()
    for name in HASHED:
        (build / "Build" / name).write_bytes(b"\x00")
    return build


def test_the_build_files_are_read_from_index_html(tmp_path):
    assert package_release.build_files(hashed_build(tmp_path)) == sorted(HASHED)
    assert package_release.build_files(fake_build(tmp_path / "plain", reviewed=True)) == []


def test_the_zip_carries_the_hashed_files_index_html_names(tmp_path, capsys):
    build, zip_path = hashed_build(tmp_path), tmp_path / "out.zip"
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 0
    with zipfile.ZipFile(zip_path) as z:
        assert sorted(n for n in z.namelist() if n.startswith("Build/")) == sorted("Build/" + n for n in HASHED)


def test_files_of_an_earlier_build_are_left_out_of_the_zip(tmp_path, capsys):
    """A second build into the same folder leaves the earlier build's files beside the new ones under other names."""
    build, zip_path = hashed_build(tmp_path), tmp_path / "out.zip"
    (build / "Build" / "ffffffffffffffffffffffffffffffff.data.br").write_bytes(b"old")
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 0
    with zipfile.ZipFile(zip_path) as z:
        assert sorted(n for n in z.namelist() if n.startswith("Build/")) == sorted("Build/" + n for n in HASHED)
    assert "LEFT OUT Build/ffffffffffffffffffffffffffffffff.data.br" in capsys.readouterr().out


def test_a_build_missing_a_file_index_html_names_is_refused(tmp_path, capsys):
    build = hashed_build(tmp_path)
    (build / "Build" / HASHED[3]).unlink()
    assert "Build/" + HASHED[3] in refused(build, tmp_path, capsys)
