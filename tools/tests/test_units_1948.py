"""content/map/units_1948.yaml (docs/units.md, "Units in play"): every position is sourced and every window ends for a
stated reason (review C1). The C# side (UnitData.Parse, UnitsInPlayTests) enforces the same rules in the game; this
keeps them in CI's Python job too."""
from datetime import date
from pathlib import Path
from urllib.parse import urlparse

import yaml12

ROOT = Path(__file__).resolve().parents[2]
UNITS = yaml12.load((ROOT / 'content/map/units_1948.yaml').read_text(encoding='utf-8'))['units']
# The signing day of each front's armistice (https://en.wikipedia.org/wiki/1949_Armistice_Agreements, checked 2026-10-11).
ARMISTICE = {'egypt': date(1949, 2, 24), 'lebanon': date(1949, 3, 23), 'jordan': date(1949, 4, 3), 'syria': date(1949, 7, 20)}
GAME_START = date(1948, 5, 15)


def day(s):
    return date.fromisoformat(s)


def sources(u):
    out = list(u.get('sources') or [])
    for a in u['anchors']:
        out += a['sources']
        out += (a.get('toward') or {}).get('sources') or []
    for t in u.get('aircraft') or []:
        out += t['sources']
    out += u.get('headgear_sources') or []
    out += (u.get('disbanded') or {}).get('sources') or []
    return out


def wiki(s):
    host = urlparse(s['url']).netloc
    return host.endswith('.wikipedia.org') or host == 'www.wikidata.org'


def test_every_window_ends_for_a_stated_reason():
    for u in UNITS:
        for a in u['anchors']:
            where = f"{u['id']} {a['place']}"
            assert a['sources'], where
            assert a.get('end_basis') in ('source', 'front_armistice'), where
            if a['end_basis'] == 'source':
                assert a.get('end_note', '').strip(), f'{where}: a source-based end says which source'
                assert 'front' not in a, where
            else:
                assert day(a['to']) == ARMISTICE[a['front']], f"{where}: ends on the {a['front']} armistice"
            if u.get('disbanded'):
                assert day(a['to']) <= day(u['disbanded']['date']), f'{where}: runs past its disbandment'
                assert u['disbanded']['sources'], u['id']
            assert GAME_START <= day(a['from']) < day(a['to']), where


def test_no_blanket_end_date():
    ends = [day(a['to']) for u in UNITS for a in u['anchors']]
    syria = [a for u in UNITS for a in u['anchors'] if day(a['to']) == ARMISTICE['syria']]
    assert all(a.get('front') == 'syria' for a in syria), 'only a Syrian-front window may end on 20 July 1949'
    assert max(ends) <= ARMISTICE['syria']


def test_since_is_never_the_game_start():
    for u in UNITS:
        for a in u['anchors']:
            assert '15 May 1948' not in str(a.get('since', '')), f"{u['id']} {a['place']}"


def test_every_source_has_a_url_and_a_check_date_and_wikipedia_only_matches_its_sources():
    for u in UNITS:
        srcs = sources(u)
        assert srcs, u['id']
        for s in srcs:
            assert s['url'].startswith('https://'), (u['id'], s)
            assert day(s['checked']) <= date.today(), (u['id'], s)
        assert u['wikipedia_only'] == all(wiki(s) for s in srcs), f"{u['id']}: wikipedia_only must match its sources"


def test_hidden_brigades_and_headgear():
    assert {u['id'] for u in UNITS if u.get('hidden')} == {'brigade_8', 'oded'}
    for u in UNITS:
        if u.get('hidden'):
            assert u['anchors'] == []
        if 'headgear' in u:
            assert u['service'] == 'army' and len(u['headgear']) == 3 and u['headgear_sources'], u['id']


def test_a_toward_point_is_sourced_and_says_why():
    for u in UNITS:
        for a in u['anchors']:
            t = a.get('toward')
            if t:
                assert t['note'].strip() and t['sources'], f"{u['id']} {a['place']}"


def test_a_window_ends_at_the_earliest_sourced_move():
    """Review M1 and S2 (#570): Carmeli's Brosh window ends with the second truce, and Givati's with its move to the centre."""
    def shown(uid, d):
        u = next(x for x in UNITS if x['id'] == uid)
        return any(day(a['from']) <= d < day(a['to']) for a in u['anchors'])
    assert shown('carmeli', date(1948, 7, 10)) and not shown('carmeli', date(1948, 9, 1))
    assert shown('givati', date(1948, 10, 1)) and not shown('givati', date(1948, 12, 15))

# The sides of 1948 a unit can belong to (UnitData.Sides): Israel and the Arab states whose armies entered the war. Every row
# names its side; the map flies that side's flag where one is sourced (UnitFlags).
SIDES = {'israel', 'egypt', 'jordan', 'syria', 'iraq', 'lebanon'}


def test_every_unit_names_a_known_side():
    for u in UNITS:
        assert u.get('side') in SIDES, f"{u['id']}: side {u.get('side')!r} is not one of {sorted(SIDES)}"
