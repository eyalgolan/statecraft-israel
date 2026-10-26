"""Original building sources stay outside Unity's importer and retain exact bytes."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]


def test_original_building_archive_matches_every_admission_receipt():
    rows = json.loads((ROOT / 'docs/generated-buildings-props-admission.json').read_text())
    assert len(rows) == 11
    assert not list((ROOT / 'unity/Assets/Editor/Generated1948Review').rglob('*.glb'))
    for row in rows:
        path = ROOT / row['source_archive_path']
        assert path == ROOT / 'asset-sources/generated-1948' / row['id'] / (row['id'] + '.glb')
        data = path.read_bytes()
        assert len(data) == row['source_glb_bytes']
        assert hashlib.sha256(data).hexdigest() == row['source_glb_sha256']
        provenance = ROOT / 'unity/Assets/Editor/Generated1948Review' / row['id'] / 'provenance.json'
        assert json.loads(provenance.read_text())['source_archive_path'] == row['source_archive_path']
