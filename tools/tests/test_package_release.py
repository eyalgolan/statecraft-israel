"""tools/package_release.py: the itch.io zip is made only after the owner's sensitivity review of every sensitive event
(spec 5.5; M3 rereview). The gate is the sim's own (Statecraft.Cli --release-gate), run on the content inside the build."""
import json
import re
import shutil
import zipfile
from pathlib import Path

import pytest

import package_release

REPO = Path(package_release.REPO)
SENSITIVE_PHOTOS = ["altalena", "bernadotte_assassination", "burma_road_complete", "degania_defence", "operation_hiram", "operation_horev", "operation_yoav", "reparations_vote", "ten_days"]
ALTERNATE_FIXTURE_CARDS = ("jerusalem_demilitarisation_guard", "rafah_force_pressure")
SENSITIVE = ["altalena", "bernadotte_assassination", "burma_road_complete", "degania_defence", "deport_acre", "deport_nazareth", "operation_hiram", "operation_horev", "operation_yoav", "reparations_claim", "reparations_vote", "ten_days"]


def photo_manifest(build, reviewed=True, shareable=True):
    """The event-photos.json Build.Web writes (Build.PhotoManifestJson): the 8 sensitive photographs and one other."""
    photos = [{"event_id": e, "sensitive": True, "sensitivity_reviewed": reviewed} for e in SENSITIVE_PHOTOS]
    photos.append({"event_id": "maabarot_policy", "sensitive": False, "sensitivity_reviewed": False})
    (build / package_release.PHOTO_MANIFEST).write_text(json.dumps({"version": 1, "shareable": shareable, "photos": photos}))


def fake_build(tmp_path, reviewed, photos_reviewed=True, forces_checked=True, alternates_reviewed=True):
    """A build folder with index.html, the real content copied into StreamingAssets/content and the photo manifest.
    forces_checked: the starting forces' rows in units_1948.yaml are fact-checked, as the owner's person check of
    21 October 2026 left them (R-UNIT-006, Q-START-3; #1240); False sets every row back to draft, as before it.
    alternates_reviewed: TEST-ONLY fictional review of the two #939 copies. No real content file is changed."""
    build = tmp_path / "Web-brotli"
    (build / "StreamingAssets").mkdir(parents=True)
    (build / "Build").mkdir()
    (build / "Build/game.loader.js").write_bytes(b"\x00")
    (build / "index.html").write_text('<script src="Build/game.loader.js"></script>')
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
    # TEST-ONLY fixture data: these two files are copies below tmp_path, never the source content.
    # Keep positive packaging tests independent of real human sign-off and exercise unsigned copies below.
    for event_id in ALTERNATE_FIXTURE_CARDS:
        f = content / "events" / f"{event_id}.yaml"
        text = f.read_text(encoding="utf-8")
        text = re.sub(r"(?m)^reviewed_(?:by|on):[^\n]*(?:\n|$)", "", text)
        alternate_status = "fact-checked" if alternates_reviewed else "draft"
        text = re.sub(r"(?m)^status: .*$", f"status: {alternate_status}", text)
        if alternates_reviewed:
            text += ("\n# TEST-ONLY fictional review, not a person's historical or language approval.\n"
                     "reviewed_by: package-fixture-reviewer\nreviewed_on: 2026-10-24\n")
        f.write_text(text, encoding="utf-8")
    photo_manifest(build, reviewed=photos_reviewed)
    ship_translations(build)
    return build


def ship_translations(build):
    """The translations as ContentSync ships them: the files beside the content, listed in manifest.json and packed into
    bundle.json (only the fields package_release reads)."""
    folder = build / "StreamingAssets" / "translations"
    shutil.copytree(REPO / "translations", folder, dirs_exist_ok=True)
    files = package_release._shipped_translations(folder)
    content = build / "StreamingAssets" / "content"
    (content / "manifest.json").write_text(json.dumps({"files": [], "translations": files}))
    (content / "bundle.json").write_text(json.dumps(
        {"translations": files, "translationTexts": [(folder / f).read_text(encoding="utf-8") for f in files]}, ensure_ascii=False))
    return files


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


def test_packaging_refuses_each_unsigned_consequence_copy(tmp_path, capsys):
    # R-ALT-011: every other fake-build gate is clear; these two copied cards alone remain unsigned.
    build = fake_build(tmp_path, reviewed=True, alternates_reviewed=False)
    zip_path = tmp_path / "out.zip"
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 1
    assert not zip_path.exists()
    out = capsys.readouterr().out
    assert "PACKAGE REFUSED" in out
    for event_id in ALTERNATE_FIXTURE_CARDS:
        assert event_id in out


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
    (build / "Build").mkdir(exist_ok=True)
    for name in HASHED:
        (build / "Build" / name).write_bytes(b"\x00")
    return build


def test_the_build_files_are_read_from_index_html(tmp_path):
    assert package_release.build_files(hashed_build(tmp_path)) == sorted(HASHED)
    assert package_release.build_files(fake_build(tmp_path / "plain", reviewed=True)) == ["game.loader.js"]


@pytest.mark.parametrize("page", ["<html></html>", "<script src='Build/game.loader.js'></script>"])
def test_a_build_whose_index_names_no_recognised_build_file_is_refused(tmp_path, capsys, page):
    build = fake_build(tmp_path, reviewed=True)
    (build / "index.html").write_text(page)
    assert "index.html names no Build/ files" in refused(build, tmp_path, capsys)


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


def test_nested_files_of_an_earlier_build_are_left_out_of_the_zip(tmp_path, capsys):
    build, zip_path = hashed_build(tmp_path), tmp_path / "out.zip"
    stale = build / "Build" / "old" / "files" / "earlier.data.br"
    stale.parent.mkdir(parents=True)
    stale.write_bytes(b"old")
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 0
    with zipfile.ZipFile(zip_path) as z:
        assert "Build/old/files/earlier.data.br" not in z.namelist()
    assert "LEFT OUT Build/old/files/earlier.data.br" in capsys.readouterr().out


def test_a_nested_file_the_index_names_is_kept(tmp_path, capsys):
    """#2695 review: only unnamed files are stale; a nested path index.html names still ships."""
    build, zip_path = hashed_build(tmp_path), tmp_path / "out.zip"
    named = build / "Build" / "extra" / "module.js"
    named.parent.mkdir(parents=True)
    named.write_bytes(b"kept")
    index = build / "index.html"
    index.write_text(index.read_text(encoding="utf-8") + '<script src="Build/extra/module.js"></script>\n',
                     encoding="utf-8")
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 0
    with zipfile.ZipFile(zip_path) as z:
        assert "Build/extra/module.js" in z.namelist()
    assert "LEFT OUT Build/extra/module.js" not in capsys.readouterr().out


def test_an_index_that_is_not_utf8_is_refused_without_a_zip(tmp_path, capsys):
    build, zip_path = fake_build(tmp_path, reviewed=True), tmp_path / "out.zip"
    (build / "index.html").write_bytes(b"<html>\xff</html>")
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 1
    assert not zip_path.exists()
    out = capsys.readouterr().out
    assert "PACKAGE REFUSED" in out and "index.html" in out and "UTF-8" in out


def test_an_index_that_cannot_be_read_is_refused_without_a_zip(tmp_path, capsys, monkeypatch):
    build, zip_path = fake_build(tmp_path, reviewed=True), tmp_path / "out.zip"
    index = build / "index.html"
    read_text = Path.read_text

    def fail_index(path, *args, **kwargs):
        if path == index:
            raise OSError("read failed")
        return read_text(path, *args, **kwargs)

    monkeypatch.setattr(package_release.Path, "read_text", fail_index)
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 1
    assert not zip_path.exists()
    out = capsys.readouterr().out
    assert "PACKAGE REFUSED" in out and "index.html" in out and "UTF-8" in out


def test_a_build_missing_a_file_index_html_names_is_refused(tmp_path, capsys):
    build = hashed_build(tmp_path)
    (build / "Build" / HASHED[3]).unlink()
    assert "Build/" + HASHED[3] in refused(build, tmp_path, capsys)


def test_the_zip_carries_the_translations_the_web_bundle_packs(tmp_path, capsys):
    build, zip_path = fake_build(tmp_path, reviewed=True), tmp_path / "out.zip"
    assert package_release.translation_refusal(build) is None
    assert package_release.main(["--build", str(build), "--zip", str(zip_path)]) == 0
    with zipfile.ZipFile(zip_path) as z:
        assert "StreamingAssets/translations/ro/events.yaml" in z.namelist()


def test_losing_every_translation_is_refused(tmp_path, capsys):
    """#2641: a build with no translations folder and no translation lists, absent, null or empty, must not pass while
    the repository has translation files."""
    expected = package_release._shipped_translations(REPO / "translations")
    assert expected, "the repository ships at least one translation, or this test proves nothing"
    for case in ("missing", "null", "empty"):
        build = fake_build(tmp_path / case, reviewed=True)
        shutil.rmtree(build / "StreamingAssets" / "translations")
        content = build / "StreamingAssets" / "content"
        for name, fields in (("manifest.json", ("translations",)), ("bundle.json", ("translations", "translationTexts"))):
            data = json.loads((content / name).read_text(encoding="utf-8"))
            for field in fields:
                if case == "missing":
                    data.pop(field, None)
                else:
                    data[field] = None if case == "null" else []
            (content / name).write_text(json.dumps(data), encoding="utf-8")
        assert f"the repository's translations/ holds {expected}" in refused(build, tmp_path / case, capsys), case


def test_a_lost_changed_or_unlisted_translation_is_refused(tmp_path, capsys):
    """#1618: the gate reads StreamingAssets/translations and the web player bundle.json; they must be one set."""
    build = fake_build(tmp_path, reviewed=True)
    folder = build / "StreamingAssets" / "translations"
    (folder / "ro" / "ui.yaml").unlink()
    assert "do not match" in refused(build, tmp_path, capsys)
    ship_translations(build)
    (folder / "ro" / "ui.yaml").write_text("ui.lang: \"xx\"\n", encoding="utf-8")
    assert "differs from StreamingAssets/translations for ro/ui.yaml" in refused(build, tmp_path, capsys)
    ship_translations(build)
    (folder / "ro" / "content.yaml").write_text("{}\n", encoding="utf-8")
    assert "do not match" in refused(build, tmp_path, capsys)
    (folder / "ro" / "content.yaml").unlink()
    ship_translations(build)
    (folder / "ro" / "ui.yaml").write_bytes(b"ui.lang: \"\xff\"\n")   # not UTF-8: a refusal, not a crash
    assert "cannot be read as UTF-8 text" in refused(build, tmp_path, capsys)
    ship_translations(build)
    (folder / ".DS_Store").write_text("x")              # hidden and top-level names are never translations
    (folder / "README.md").write_text("x")
    assert package_release.translation_refusal(build) is None
    (build / "StreamingAssets" / "content" / "bundle.json").unlink()
    assert "no readable manifest.json and bundle.json" in refused(build, tmp_path, capsys)
