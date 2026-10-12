"""tools/package_release.py: the itch.io zip is made only after the owner's sensitivity review of every sensitive event
(spec 5.5; M3 rereview). The gate is the sim's own (Statecraft.Cli --release-gate), run on the content inside the build."""
import json
import re
import shutil
import zipfile
from pathlib import Path

import package_release

REPO = Path(package_release.REPO)
SENSITIVE_PHOTOS = ["altalena", "bernadotte_assassination", "operation_hiram", "operation_horev", "operation_yoav", "ten_days"]
SENSITIVE = ["altalena", "bernadotte_assassination", "operation_hiram", "operation_horev", "operation_yoav", "reparations_claim", "ten_days"]


def photo_manifest(build, reviewed=True, shareable=True):
    """The event-photos.json Build.Web writes (Build.PhotoManifestJson): the 7 sensitive photographs and one other."""
    photos = [{"event_id": e, "sensitive": True, "sensitivity_reviewed": reviewed} for e in SENSITIVE_PHOTOS]
    photos.append({"event_id": "maabarot_policy", "sensitive": False, "sensitivity_reviewed": False})
    (build / package_release.PHOTO_MANIFEST).write_text(json.dumps({"version": 1, "shareable": shareable, "photos": photos}))


def fake_build(tmp_path, reviewed, photos_reviewed=True):
    """A build folder with index.html, the real content copied into StreamingAssets/content and the photo manifest."""
    build = tmp_path / "Web-brotli"
    (build / "StreamingAssets").mkdir(parents=True)
    (build / "index.html").write_text("<html></html>")
    content = build / "StreamingAssets" / "content"
    shutil.copytree(REPO / "content", content)
    status = "sensitivity-reviewed" if reviewed else "fact-checked"   # the owner's sign-off (D11), or its absence
    for e in SENSITIVE:
        f = content / "events" / f"{e}.yaml"
        f.write_text(re.sub(r"(?m)^status: .*$", f"status: {status}", f.read_text()))
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
