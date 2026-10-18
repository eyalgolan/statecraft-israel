"""content/map/units_1948.yaml (docs/units.md, "Units in play"): every position is sourced and every window ends for a
stated reason (review C1). The C# side (UnitData.Parse, UnitsInPlayTests) enforces the same rules in the game; this
keeps them in CI's Python job too."""
import re
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
    for v in u.get('vehicles') or []:
        out += v['sources']
    out += (u.get('disbanded') or {}).get('sources') or []
    out += (u.get('strength') or {}).get('sources') or []
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
    assert {u['id'] for u in UNITS if u.get('hidden') and u['side'] == 'israel'} == {'brigade_8', 'oded'}
    for u in UNITS:
        if u.get('hidden'):
            assert u['anchors'] == []
        if 'headgear' in u:
            assert u['service'] == 'army' and len(u['headgear']) == 3 and u['headgear_sources'], u['id']


def test_a_vehicle_is_a_known_type_from_a_sourced_day_on_an_army_unit():
    # Issue #665: the 65 mm gun and the jeep stand on a token only where a source puts them with the unit, from that day.
    placed = {}
    for u in UNITS:
        for v in u.get('vehicles') or []:
            assert u['service'] == 'army' and u['side'] == 'israel', u['id']
            assert v['type'] in ('gun_65mm', 'jeep'), (u['id'], v)
            assert day(v['from']) >= GAME_START and v['note'].strip() and v['sources'], (u['id'], v)
            placed.setdefault(v['type'], set()).add(u['id'])
        types = [v['type'] for v in u.get('vehicles') or []]
        assert len(types) == len(set(types)), u['id']
    assert placed == {'gun_65mm': {'carmeli', 'givati', 'harel', 'yiftach', 'negev'}, 'jeep': {'harel', 'negev'}}


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

# The sides a unit can belong to, as the loader knows them (UnitData.Sides in the C# file the game reads).
UNIT_DATA_CS = ROOT / 'unity/Assets/Scripts/Map/Units/UnitData.cs'


def loader_sides():
    text = UNIT_DATA_CS.read_text(encoding='utf-8')
    m = re.search(r'Sides\s*=\s*new\[\]\s*\{([^}]*)\}', text)
    assert m, 'UnitData.Sides not found'
    israel = re.search(r'const string Israel = "([a-z_]+)"', text).group(1)
    return {israel if x.strip() == 'Israel' else x.strip().strip('"') for x in m.group(1).split(',')}


SIDES = loader_sides()
# Q-START-4 (owner, 15 October 2026, "Armies + ALA + local forces"; #375): Israel, the five Arab states whose armies
# entered the war, the Arab Liberation Army and the local Arab forces, each a side of its own (impact plan section 4.2).
# The Saudi, Sudanese and Yemeni contingents are not sides of their own.
EXPECTED_SIDES = {'israel', 'egypt', 'jordan', 'syria', 'iraq', 'lebanon', 'ala', 'local_arab_forces'}
ECHELONS = {'force', 'division', 'brigade', 'battalion', 'company'}


def test_every_unit_names_a_known_side():
    for u in UNITS:
        assert u.get('side') in SIDES, f"{u['id']}: side {u.get('side')!r} is not one of {sorted(SIDES)}"


def test_every_side_is_known():
    assert SIDES == EXPECTED_SIDES, f'the loader knows {sorted(SIDES)}; Q-START-4 names {sorted(EXPECTED_SIDES)}'
    for u in UNITS:
        assert u['side'] in EXPECTED_SIDES, u['id']


def books(srcs):
    """The distinct sources that are not Wikipedia or Wikidata: an anchor's coordinates page never counts as a second
    source (review of #860 at 00d3ba15, must-fix 2)."""
    return {s['url'] for s in srcs if not wiki(s)}


def arab_rows():
    return [u for u in UNITS if u['side'] != 'israel']


# A copied range ("6,000 to 12,000", "2,000-3,000"): the research note keeps the conflicts; the file keeps one value.
RANGE = re.compile(r'\d[\d,]*\s*(?:to|-|\u2013|\u2014)\s*\d')


def test_strength_has_one_value_and_a_source():
    """The owner's #570 decision: the Arab forces' rows carry `strength` and `echelon`. CONTENT_POLICY section 1: one
    value per fact, never a range; Q-START-3: from an Israeli source where one gives it, else the value two of the note's
    sources agree on. Every strength has its day (`as_of`) and its sources."""
    rows = arab_rows()
    assert rows, 'the Arab forces have rows (#740)'
    for u in rows:
        assert u.get('echelon') in ECHELONS, f"{u['id']}: echelon {u.get('echelon')!r} is not one of {sorted(ECHELONS)}"
        st = u.get('strength')
        assert isinstance(st, dict), f"{u['id']}: no strength"
        assert set(st) <= {'value', 'as_of', 'note', 'sources'}, (u['id'], sorted(st))
        v = st.get('value')
        assert isinstance(v, int) and not isinstance(v, bool) and v > 0, f"{u['id']}: strength {v!r} is not one number"
        assert GAME_START <= day(st['as_of']) <= date(1948, 6, 11), f"{u['id']}: strength as_of {st['as_of']}"
        assert len(books(st.get('sources') or [])) >= 2, f"{u['id']}: strength rests on fewer than two sources (Q-START-3)"
        assert not RANGE.search(str(st.get('note', ''))), f"{u['id']}: the strength note copies a range"
    # Every row the file has today gives the 15 May strength only: no source in the note dates a strength on 11 June
    # (arab-forces-1948.md, section 9; owner, 17 October 2026, #740: "15 May strength only ... Nothing is invented").
    assert all(u['strength']['as_of'] == '1948-05-15' for u in rows)


def near(a, lon, lat, km=3.0):
    # Close enough to name the same place (degrees at this latitude: about 111 km north-south, 95 km east-west).
    return ((a['lon'] - lon) * 95) ** 2 + ((a['lat'] - lat) * 111) ** 2 <= km ** 2


def covers(a, d):
    return day(a['from']) <= d < day(a['to'])


LATRUN = (34.98139, 31.83194)
MALIKIYYA = (35.50806, 33.10528)
JERUSALEM_OLD_CITY = (35.2342, 31.7767)


def test_no_anchor_rests_on_one_source_where_the_note_says_contested():
    """arab-forces-1948.md section 8 and section 10. Latrun on 15 May rests on al-Tall alone (item 1), so no Legion
    anchor holds Latrun before 17 May; the Legion in Jerusalem on 15 May is contested (item 2), so none stands there.
    al-Malikiyya on 15 May is contested and ruled the ALA's (item 12; #226 ruling 2): an ALA anchor stands there and no
    Lebanese one does; who held it on 11 June is contested, so no row stands there then. Every other anchor of an Arab
    row cites at least two sources."""
    rows = arab_rows()
    anchors = [(u, a) for u in rows for a in u['anchors']]
    assert anchors, 'the Arab forces have anchors (#740)'
    for u, a in anchors:
        where = f"{u['id']} {a['place']}"
        if u['side'] == 'jordan':
            assert not (near(a, *LATRUN) and day(a['from']) < date(1948, 5, 17)), f'{where}: Latrun on 15 May rests on al-Tall alone'
            assert not (near(a, *JERUSALEM_OLD_CITY, km=5) and covers(a, GAME_START)), f'{where}: the Legion in Jerusalem on 15 May is contested'
        if near(a, *MALIKIYYA):
            assert u['side'] == 'ala' and covers(a, GAME_START) and not covers(a, date(1948, 6, 11)), \
                f'{where}: al-Malikiyya is the ALA\'s on 15 May (ruling) and nobody\'s row on 11 June (contested)'
        else:
            assert len(books(a['sources'])) >= 2, f'{where}: rests on one source (a Wikipedia or Wikidata page for the coordinates does not count)'
    assert any(u['side'] == 'ala' and near(a, *MALIKIYYA) and covers(a, GAME_START) for u, a in anchors), 'the ALA at al-Malikiyya on 15 May'
    assert any(u['side'] == 'jordan' and near(a, *LATRUN) and covers(a, date(1948, 6, 11)) for u, a in anchors), 'the Legion at Latrun on 11 June'


# The places the research note does not describe (arab-forces-1948.md, "Conventions"): the Arab rows say nothing of them.
SENSITIVE = ('Kfar Etzion', 'Gush Etzion', 'Jewish Quarter', 'Nitzanim', 'Nitzanin', 'ניצנים', 'Lydda', 'Lod', 'Ramla', 'Ramle')


def texts(x):
    if isinstance(x, dict):
        for v in x.values():
            yield from texts(v)
    elif isinstance(x, list):
        for v in x:
            yield from texts(v)
    elif isinstance(x, str):
        yield x


def test_the_arab_rows_name_no_sensitive_place():
    for u in arab_rows():
        for t in texts(u):
            for name in SENSITIVE:
                assert not re.search(rf'\b{name}\b', t), f"{u['id']}: {name!r} in {t[:80]!r}"


ISDUD = (34.66167, 31.75361)
MAJDAL = (34.56667, 31.66667)


def test_the_egyptian_force_has_its_forward_and_its_rear_base_at_the_truce():
    """Owner, 17 October 2026, on #860 (question 4): "both places. Isdud is the forward base and Majdal the rear base."
    At the first truce the Egyptian row stands at both, each anchor with at least two sources. The two may overlap in
    time only on a row the map does not draw (UnitRoster.OnMap)."""
    egypt = next(u for u in UNITS if u['id'] == 'egyptian_expeditionary_force')
    truce = [a for a in egypt['anchors'] if covers(a, date(1948, 6, 11))]
    assert any(near(a, *ISDUD) for a in truce), 'Isdud, the forward base, at the truce'
    assert any(near(a, *MAJDAL) for a in truce), 'Majdal, the rear base, at the truce'
    for a in truce:
        assert len(books(a['sources'])) >= 2, a['place']
    for u in UNITS:
        if u['side'] == 'israel':
            ends = [(day(a['from']), day(a['to'])) for a in u['anchors']]
            assert all(ends[i - 1][1] <= ends[i][0] for i in range(1, len(ends))), f"{u['id']}: an Israeli row is at one place at a time"
