"""#43: keep the owner-run upload test's control and evidence explicit."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def plan():
    text = (ROOT / "docs/m3-release.md").read_text()
    return text.split("## Saves on itch.io:", 1)[1].split("## Known limits", 1)[0]


def test_same_content_control_compares_full_hash_and_survives_url_change():
    text = plan()
    assert "unchanged game content" in text
    assert "date, turn and full state hash match" in text
    assert "different upload URL" in text
    assert "same actual game-frame origin" in text


def test_changed_content_is_separate_and_result_records_both_uploads():
    text = plan()
    assert "Separate changed-content replay check" in text
    assert "not evidence of exact state preservation" in text
    for field in ("Browser/version", "OS", "Upload A/build ID", "Upload B/build ID", "Game-frame origin", "State hash before", "State hash after"):
        assert field in text
    assert "Result: not run on itch.io" in text
