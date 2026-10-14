"""Partial #12: translation data must keep UiText's format contract."""
from pathlib import Path
import re
from collections import Counter
import yaml
ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / 'unity/Assets/UI'


def strings(name):
    return yaml.safe_load((UI / name).read_text(encoding='utf-8'))


def test_hebrew_keys_match_english_and_every_value_is_nonempty():
    en, he = strings('strings.en.yaml'), strings('strings.he.yaml')
    assert en.keys() == he.keys()
    assert all(isinstance(v, str) and v.strip() for v in he.values())


def test_hebrew_keeps_complete_format_tokens_and_logical_text_order():
    en, he = strings('strings.en.yaml'), strings('strings.he.yaml')
    for key in en:
        tokens = lambda v: Counter(re.findall(r'\{[^{}]+\}', v))
        assert tokens(en[key]) == tokens(he[key]), key
        assert not any(c in he[key] for c in '\u202a\u202b\u202c\u202d\u202e'), key
    assert 'תור' in he['hud.turn']
    assert 'תשומת לב' in he['hud.attention']


def test_hebrew_is_not_enabled_without_runtime_review():
    paths = (ROOT / 'unity/Assets/Scripts/UI/UiPaths.cs').read_text()
    assert 'public const string Strings = Folder + "/strings.en.yaml";' in paths
    # The translation is data only; no language-switch or RTL implementation in this PR.
    meta = (UI / 'strings.he.yaml.meta').read_text()
    english_meta = (UI / 'strings.en.yaml.meta').read_text()
    guid = re.search(r'^guid: ([0-9a-f]{32})$', meta, re.M).group(1)
    assert guid not in english_meta
    assert 'TextScriptImporter:' in meta


def test_hebrew_review_wording_is_count_neutral_and_uses_knesset_terms():
    he = strings('strings.he.yaml')
    assert he['event.more'] == 'החלטות ממתינות נוספות: {0}'
    # A seat count's unit is its own string, by the body (CabinetPresenter.SeatUnit): the Knesset's seats are
    # "מנדטים", the appointed Provisional State Council's members "חברים"; no template hard-codes either.
    assert he['cabinet.seats'] == 'מנדטים'
    assert he['cabinet.seats_appointed'] == 'חברים'
    for key in ('budget.vote_needed', 'budget.vote_advisory', 'cabinet.coalition', 'cabinet.opposition',
                'cabinet.majority', 'cabinet.largest'):
        assert 'מנדטים' not in he[key] and 'חברים' not in he[key], key
    assert he['order.no_attention'] == 'אין מספיק תשומת לב (נדרש {0:0.0}, יש {1:0.0})'
    assert he['defence.rung5'] == '5  מלחמה כוללת'
