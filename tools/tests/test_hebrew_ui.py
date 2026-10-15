"""Partial #12: translation data must keep UiText's format contract."""
from pathlib import Path
import re
from collections import Counter
import yaml12
ROOT = Path(__file__).resolve().parents[2]
UI = ROOT / 'unity/Assets/UI'


def strings(name):
    return yaml12.load((UI / name).read_text(encoding='utf-8'))


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


def test_an_order_refusal_shows_no_raw_id_or_exception_text():
    # #622 finding 1: the toast showed a content id ("Unknown policy 'curfew'") or the exception's message after
    # "Not done:". The kind word stays ({0}, #595); the id and the message go to the log only (SimBridge).
    for name in ('strings.en.yaml', 'strings.he.yaml'):
        s = strings(name)
        assert re.findall(r'\{[^{}]+\}', s['order.unknown']) == ['{0}'], name
        assert re.findall(r'\{[^{}]+\}', s['order.failed']) == [], name


def test_the_top_bar_and_start_labels_are_in_sentence_case():
    # #622 finding 2: the six Title Case labels match the file's sentence case, and so do the lines that name the two
    # buttons, written as the button shows them. Game.uxml keeps the same texts (HebrewUiTests).
    en = strings('strings.en.yaml')
    assert [en[k] for k in ('hud.caption.support', 'hud.caption.standing', 'hud.caption.fx', 'hud.new_game',
                            'hud.end_turn', 'start.new_game')] == [
        'Public support', 'International standing', 'Foreign currency', 'New game', 'End turn', 'New game']
    for key, value in en.items():
        assert 'End Turn' not in value and 'New Game' not in value, key
    uxml = (UI / 'Game.uxml').read_text(encoding='utf-8')
    for title in ('Public Support', 'International Standing', 'Foreign Currency', 'New Game', 'End Turn'):
        assert f'text="{title}"' not in uxml, title


HEBREW_ACRONYM_STRAIGHT_QUOTE = re.compile('[א-ת]"[א-ת]')


def test_hebrew_acronyms_use_the_gershayim_character():
    # #622 finding 4: the strings write gershayim as U+05F4 ("הרמטכ״ל"); the Hebrew names in content follow them, not a
    # straight quote. Only values are checked: comments may quote a source verbatim.
    for name, text in strings('strings.he.yaml').items():
        assert not HEBREW_ACRONYM_STRAIGHT_QUOTE.search(text), name
    content = ROOT / 'content'
    for file, key, field in (('offices.yaml', 'offices', 'title_he'), ('parties.yaml', 'parties', 'name_he'),
                             ('people.yaml', 'people', 'name_he')):
        rows = yaml12.load((content / file).read_text(encoding='utf-8'))[key]
        values = [r[f] for r in rows for f in (field, field + '_f') if r.get(f)]
        assert values, file
        for value in values:
            assert not HEBREW_ACRONYM_STRAIGHT_QUOTE.search(value), (file, value)
