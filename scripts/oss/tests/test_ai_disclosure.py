"""Manifest-driven AI disclosure, not an inferred licence or store-policy verdict."""
import importlib.util
from pathlib import Path
import pytest

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('ai_disclosure', ROOT / 'scripts/oss/ai_disclosure.py')


def module():
    m = importlib.util.module_from_spec(SPEC)
    SPEC.loader.exec_module(m)
    return m


def test_only_explicit_true_rows_from_both_manifests():
    rows = [
        ('ASSETS.yaml', {'glob': 'b.png', 'ai_generated': True, 'source': 'model', 'status': 'owner-verify'}),
        ('ASSETS.portraits.yaml', {'glob': 'a.jpg', 'ai_generated': True, 'source': 'portrait model', 'status': 'verified'}),
        ('ASSETS.yaml', {'glob': 'script.png', 'source': 'AI-assisted script'}),
        ('ASSETS.yaml', {'glob': 'false.png', 'ai_generated': False}),
    ]
    text = module().render(rows)
    assert 'a.jpg' in text and 'b.png' in text
    assert 'script.png' not in text and 'false.png' not in text
    assert 'ASSETS.portraits.yaml' in text and 'owner-verify' in text
    assert 'Nothing is generated live while you play.' in text
    assert 'not a licence clearance' in text


def test_grouped_by_kind_and_deterministic():
    rows = [('A', {'glob': x, 'ai_generated': True, 'source': 's', 'status': 'verified'})
            for x in ['z.mp4', 'a.wav', 'x.fbx', 'b.png', 'x.xyz']]
    m = module()
    assert m.render(rows) == m.render(list(reversed(rows)))
    for heading in ['Images', 'Video', 'Audio', '3D models', 'Other']:
        assert '## ' + heading in m.render(rows)


def test_invalid_ai_flag_fails_instead_of_silently_omitting():
    with pytest.raises(ValueError, match='ai_generated must be a boolean'):
        module().render([('A', {'glob': 'a.png', 'ai_generated': 'true'})])


def test_empty_disclosure_is_honest():
    assert 'No asset rows are marked' in module().render([])


def test_checked_in_output_matches_current_manifests():
    m = module()
    assert (ROOT / 'docs/ai-disclosure.md').read_text(encoding='utf-8') == m.render(m.read_manifests(ROOT)), \
        'docs/ai-disclosure.md is stale: run uv run scripts/oss/ai_disclosure.py --output docs/ai-disclosure.md'


def test_incomplete_flagged_rule_fails():
    with pytest.raises(ValueError, match='requires source'):
        module().render([('A', {'glob': 'a.png', 'ai_generated': True, 'status': 'verified'})])
