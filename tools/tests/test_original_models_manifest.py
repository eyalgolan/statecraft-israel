"""The current originals' effective licence agrees with their exact asset rows."""
import copy
import fnmatch
import json
from pathlib import Path
import re
import unittest

import yaml12

ROOT = Path(__file__).resolve().parents[2]
ORIGINAL_ROW = re.compile(r'Tal Heuberger original offered in PR #(507|336) at [0-9a-f]{40}, ')


def verify(manifest, assets):
    originals = [x for x in manifest['files'] if x['source'] in ('507', '336')]
    assert originals, 'the manifest lists original FBX/JPEG entries'
    listed = {a['glob'] for a in assets['rules'] if ORIGINAL_ROW.match(a.get('source', ''))}
    assert {x['path'] for x in originals} == listed, 'manifest originals equal the #507/#336 source files in ASSETS.yaml'
    assert manifest['header'].get('licence') == 'CC-BY-NC-ND-4.0', 'original header licence'
    assert manifest['header'].get('generated_licence') == 'PolyForm-Noncommercial-1.0.0', 'generated code row in LICENSING.md'
    for row in originals:
        exact = [a for a in assets['rules'] if a['glob'] == row['path']]
        assert len(exact) == 1, row['path']
        assert row.get('licence', manifest['header']['licence']) == exact[0]['licence'], row['path']
    generated = [x for x in manifest['files'] if x['source'] == 'generated (ours)']
    assert len(generated) == 311, 'generated entries remain explicitly ours'
    assert all(x.get('licence', manifest['header']['generated_licence']) == 'PolyForm-Noncommercial-1.0.0' for x in generated)


def asset_rules_over_generated(manifest, assets):
    """(glob, path) pairs where an ASSETS.yaml rule covers a generated file; scripts/oss/check_repo.py uses fnmatch too."""
    generated = [x['path'] for x in manifest['files'] if x['source'] not in ('507', '336')]
    return [(a['glob'], p) for a in assets['rules'] for p in generated if fnmatch.fnmatch(p, a['glob'])]


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

    def test_a_dropped_manifest_original_fails(self):
        changed = copy.deepcopy(self.manifest)
        changed['files'].remove(next(x for x in changed['files'] if x['source'] in ('507', '336')))
        with self.assertRaisesRegex(AssertionError, 'equal the #507/#336'):
            verify(changed, self.assets)

    def test_a_dropped_asset_row_fails(self):
        changed = copy.deepcopy(self.assets)
        changed['rules'].remove(next(x for x in changed['rules'] if ORIGINAL_ROW.match(x.get('source', ''))))
        with self.assertRaisesRegex(AssertionError, 'equal the #507/#336'):
            verify(self.manifest, changed)

    def test_no_asset_rule_covers_a_generated_file(self):
        # Generated prefabs, controllers, materials and import metadata are under the code row of LICENSING.md.
        self.assertEqual([], asset_rules_over_generated(self.manifest, self.assets))

    def test_a_wide_historical_glob_over_a_generated_file_fails(self):
        changed = copy.deepcopy(self.assets)
        changed['rules'].append({'glob': 'unity/Assets/Art/Collection/*-near.*', 'licence': 'CC0-1.0'})
        self.assertTrue(asset_rules_over_generated(self.manifest, changed))
