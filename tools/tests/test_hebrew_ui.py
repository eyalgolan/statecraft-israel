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


def test_map_action_reasons_are_hebrew_and_their_english_action_names_are_the_rules_names():
    """#881: the info panel builds the refusal from these strings; English must keep the sim's wording, which names
    each action by its rules.yaml name."""
    en, he = strings('strings.en.yaml'), strings('strings.he.yaml')
    keys = [key for key in en if key.startswith('info.map_action')]
    assert len(keys) == 14             # 2 action names and 12 reasons
    assert all(not re.search('[A-Za-z]', he[key]) for key in keys)
    rules = yaml12.load((ROOT / 'content/rules.yaml').read_text(encoding='utf-8'))
    actions = rules['map_actions']
    assert {k.split('.')[-1] for k in keys if k.startswith('info.map_action.')} == set(actions)
    for action, rule in actions.items():
        assert en['info.map_action.' + action] == rule['name']


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


def test_a_failed_turn_gives_the_next_step_in_both_languages():
    # #622 (maintainer, 15 October 2026): SimBridge.EndTurn clears the queue when the turn fails, so the refusal says so
    # and tells the player to give the orders again. The Hebrew waits for the native reviewer (#564).
    en, he = strings('strings.en.yaml'), strings('strings.he.yaml')
    assert en['order.failed'] == 'The turn could not run. Your orders were cleared. Give them again.'
    assert he['order.failed'].count('.') == 3, he['order.failed']
    assert not re.search('[A-Za-z]', he['order.failed']), he['order.failed']

def test_the_top_bar_and_start_labels_are_in_sentence_case():
    # #622 finding 2: the six Title Case labels match the file's sentence case, and so do the lines that name the two
    # buttons, written as the button shows them. Game.uxml keeps the same texts (HebrewUiTests).
    en = strings('strings.en.yaml')
    assert [en[k] for k in ('hud.caption.support', 'hud.caption.standing', 'hud.caption.fx', 'hud.new_game',
                            'hud.end_turn', 'start.new_game')] == [
        'Public support', 'International support', 'Foreign currency', 'New game', 'End turn', 'New game']
    for key, value in en.items():
        assert 'End Turn' not in value and 'New Game' not in value, key
    uxml = (UI / 'Game.uxml').read_text(encoding='utf-8')
    for title in ('Public Support', 'International Standing', 'International standing', 'International Support',
                  'Foreign Currency', 'New Game', 'End Turn'):
        assert f'text="{title}"' not in uxml, title


def test_the_international_meter_is_named_support_in_both_languages():
    # #622 decision 3 (maintainer, 15 October 2026): "International support". The Hebrew is the owner's word for
    # International Support, תמיכה בינלאומית (Q-HUD-3, decided 15 October 2026, #375), replacing מעמד בינלאומי.
    en, he = strings('strings.en.yaml'), strings('strings.he.yaml')
    assert en['hud.caption.standing'] == 'International support'
    assert he['hud.caption.standing'] == 'תמיכה בינלאומית'
    assert 'text="International support"' in (UI / 'Game.uxml').read_text(encoding='utf-8')


HEBREW_ACRONYM_STRAIGHT_QUOTE = re.compile('[א-ת]"[א-ת]')


def test_hebrew_acronyms_use_the_gershayim_character():
    # #622 finding 4: the strings write gershayim as U+05F4 ("הרמטכ״ל"); the Hebrew names in content follow them, not a
    # straight quote. Only values are checked: comments may quote a source verbatim.
    for name, text in strings('strings.he.yaml').items():
        assert not HEBREW_ACRONYM_STRAIGHT_QUOTE.search(text), name
    content = ROOT / 'content'
    for file, key, field in (('offices.yaml', 'offices', 'title_he'), ('parties.yaml', 'parties', 'name_he'),
                             ('people.yaml', 'people', 'name_he'), ('map/units_1948.yaml', 'units', 'name_he')):
        rows = yaml12.load((content / file).read_text(encoding='utf-8'))[key]
        values = [r[f] for r in rows for f in (field, field + '_f') if r.get(f)]
        assert values, file
        for value in values:
            assert not HEBREW_ACRONYM_STRAIGHT_QUOTE.search(value), (file, value)


def test_every_cabinet_refusal_has_both_strings_and_the_english_is_the_sims_wording():
    """#901: every return path of Cabinet.CheckReason names a kind; the Cabinet screen formats it from
    cabinet.reason.<kind>. Each kind needs a key in both files, and the English key must be the sim's own English text
    (its interpolations as {0}, {1}, ... in order), so the English player sees what the sim says."""
    en, he = strings('strings.en.yaml'), strings('strings.he.yaml')
    src = (ROOT / 'sim/Statecraft.Sim/Cabinet.cs').read_text(encoding='utf-8')
    paths = re.findall(r'new CabinetReason\("(\w+)",\s*(\$?)"((?:[^"\\]|\\.)*)"', src)
    assert len(paths) == 15
    assert len({kind for kind, _, _ in paths}) == len(paths)   # one kind per return path
    for kind, interpolated, english in paths:
        key = 'cabinet.reason.' + kind
        assert key in en and key in he, key
        n = iter(range(10))
        template = re.sub(r'\{[^{}]+\}', lambda _: '{%d}' % next(n), english) if interpolated else english
        assert en[key] == template, key
        assert not re.search('[A-Za-z]', he[key]), key
    assert sorted(k for k in en if k.startswith('cabinet.reason.')) == sorted('cabinet.reason.' + k for k, _, _ in paths)
