"""The current originals' effective licence agrees with their exact asset rows."""
import copy
import json
from pathlib import Path
import unittest

import yaml12

ROOT = Path(__file__).resolve().parents[2]


def verify(manifest, assets):
    originals = [x for x in manifest['files'] if x['source'] in ('507', '336')]
    assert len(originals) == 146, '146 original FBX/JPEG entries'
    assert manifest['header'].get('licence') == 'CC-BY-NC-ND-4.0', 'original header licence'
    assert manifest['header'].get('generated_licence') == 'PolyForm-Noncommercial-1.0.0', 'generated code row in LICENSING.md'
    for row in originals:
        exact = [a for a in assets['rules'] if a['glob'] == row['path']]
        assert len(exact) == 1, row['path']
        assert row.get('licence', manifest['header']['licence']) == exact[0]['licence'], row['path']
    generated = [x for x in manifest['files'] if x['source'] == 'generated (ours)']
    assert len(generated) == 311, 'generated entries remain explicitly ours'
    assert all(x.get('licence', manifest['header']['generated_licence']) == 'PolyForm-Noncommercial-1.0.0' for x in generated)


class OriginalModelsManifestTests(unittest.TestCase):
    def setUp(self):
        self.manifest = json.loads((ROOT / 'docs/original-models.json').read_text())
        self.assets = yaml12.load((ROOT / 'ASSETS.yaml').read_text())

    def test_all_originals_match_exact_asset_rows(self):
        verify(self.manifest, self.assets)

    def test_a_changed_original_licence_fails(self):
        changed = copy.deepcopy(self.assets)
        path = next(x['path'] for x in self.manifest['files'] if x['source'] in ('507', '336'))
        next(x for x in changed['rules'] if x['glob'] == path)['licence'] = 'CC0-1.0'
        with self.assertRaisesRegex(AssertionError, path):
            verify(self.manifest, changed)
