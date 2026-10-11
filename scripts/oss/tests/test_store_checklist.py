"""#45: store prep is reviewable, not a claimed store submission."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]


def checklist():
    return (ROOT / 'docs/RELEASING.md').read_text().split('## Store-page checklist', 1)[1]


def test_current_store_assets_have_official_source_and_real_build_control():
    text = checklist()
    for size in ('920 x 430', '462 x 174', '1232 x 706', '748 x 896', '1920 x 1080'):
        assert size in text
    assert 'https://partner.steamgames.com/doc/store/assets/standard' in text
    assert 'at least five' in text
    assert 'not concept art' in text
    assert 'build commit and zip SHA-256' in text


def test_owner_decisions_and_manifest_scope_are_not_claimed_complete():
    text = checklist()
    for phrase in ('OWNER STEP', 'D8', 'war, terror and the Holocaust', 'all adult content',
                   'docs/ai-disclosure.md', 'does not answer the survey', 'localization'):
        assert phrase in text
    assert 'https://partner.steamgames.com/doc/gettingstarted/contentsurvey' in text
