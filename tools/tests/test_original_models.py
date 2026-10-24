"""#1273: pin Tal's committed bytes, with only the owner-approved path-string blanking."""
import hashlib
import json
from pathlib import Path
import pytest
REPO = Path(__file__).resolve().parents[2]
ROWS = json.loads((REPO / "docs/original-models.json").read_text())
def test_every_original_is_pinned_once():
    assert len(ROWS) == 146
    assert len({r["destination"] for r in ROWS}) == 146
    assert len({r["model"] for r in ROWS}) == 73
    assert sum(bool(r["blankedStringSpans"]) for r in ROWS) == 54
@pytest.mark.parametrize("row", ROWS, ids=[r["destination"] for r in ROWS])
def test_original_file_matches_the_approved_bytes(row):
    data = (REPO / row["destination"]).read_bytes()
    assert len(data) == row["sourceBytes"]
    assert hashlib.sha256(data).hexdigest() == row["spikeSHA256"]
    assert len(data) <= 10 * 1024 * 1024
    if not row["blankedStringSpans"]:
        assert row["sourceSHA256"] == row["spikeSHA256"]
    for span in row["blankedStringSpans"]:
        assert data[span["offset"]:span["offset"] + span["length"]] == b" " * span["length"]
