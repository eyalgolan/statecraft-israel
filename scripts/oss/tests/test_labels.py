from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
LABELS = ROOT / "scripts/oss/github/labels.tsv"
# The labels that exist on github.com/eyalgolan/statecraft-israel (read with `gh label list`, 2026-10-11), with colours.
# "ready for a contributor" is added with the Task form (#260); it exists after apply_github_settings.sh runs.
LIVE = {
    "accessibility": "f143ab", "bug": "d73a4a", "documentation": "0075ca", "duplicate": "cfd3d7",
    "enhancement": "a2eeef", "good first issue": "7057ff", "help wanted": "008672", "invalid": "e4e669",
    "question": "d876e3", "wontfix": "ffffff", "content": "0e8a16", "historical-accuracy": "1d76db",
    "no-changelog": "cfd3d7", "spam": "000000", "area: sim": "5319e7", "area: tools": "bfd4f2",
    "area: unity": "222222", "area: art": "f9d0c4", "area: design": "c5def5", "research": "0e8a16",
    "translation": "fbca04", "M4": "b60205", "in progress (maintainer)": "ededed", "roadmap": "3e4b9e",
    "ready for a contributor": "2da44e",
}


def rows():
    return [l.split("\t") for l in LABELS.read_text(encoding="utf-8").splitlines() if l.strip()]


def test_labels_tsv_holds_every_live_label_with_its_colour():
    got = {r[0]: r[1] for r in rows()}
    assert got == LIVE


def test_every_row_has_a_name_a_colour_and_a_short_description():
    for r in rows():
        assert len(r) == 3, r
        name, colour, desc = r
        assert len(colour) == 6 and int(colour, 16) >= 0 and 0 < len(desc) < 100, r
