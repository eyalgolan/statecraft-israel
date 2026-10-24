"""Event timeline (#324): sorting, era lookup, open windows, status filter and the real content."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('event_timeline', ROOT / 'scripts/event_timeline.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)


def repo(tmp_path):
    (tmp_path / 'content/events').mkdir(parents=True)
    (tmp_path / 'content/eras.yaml').write_text(
        "eras:\n  - { id: a, from: 1948-05-15, to: 1950-01-01 }\n  - { id: b, from: 1950-01-01, to: 1957-01-01 }\n")
    (tmp_path / 'content/events/late.yaml').write_text("id: late\nfrom: 1951-01-01\nstatus: fact-checked\n")
    (tmp_path / 'content/events/early.yaml').write_text("id: early\nfrom: 1948-06-01\nto: 1948-07-01\ntags: [security]\nanchor: hard\n")
    # named so that id order is not date order; this one starts on the era boundary, so it belongs to era b
    (tmp_path / 'content/events/a_edge.yaml').write_text("id: a_edge\nfrom: 1950-01-01\nstatus: fact-checked\n")
    return tmp_path


def test_sorted_with_era_open_window_and_default_status(tmp_path):
    rows = m.load(repo(tmp_path))
    assert [r['id'] for r in rows] == ['early', 'a_edge', 'late']
    assert [r['era'] for r in rows] == ['a', 'b', 'b']
    assert rows[2]['to'] == 'open' and rows[0]['status'] == 'draft' and rows[0]['anchor'] == 'hard'
    out = m.render(rows)
    assert '[security]' in out and out.splitlines()[-1] == '3 events: 1 draft, 2 fact-checked'


def test_status_filter(tmp_path, capsys):
    assert m.main(['--root', str(repo(tmp_path)), '--status', 'draft']) == 0
    out = capsys.readouterr().out
    assert 'early' in out and 'late' not in out.split('\n')[0]
    assert m.main(['--root', str(tmp_path), '--status', 'historian-reviewed']) == 0
    assert capsys.readouterr().out.strip() == '0 events'


def test_defaults_match_the_sim():
    # a missing anchor or status must show as the sim reads it (GameEvent in Content.cs)
    cs = (ROOT / 'sim/Statecraft.Sim/Content.cs').read_text()
    game_event = cs[cs.index('class GameEvent'):]
    game_event = game_event[:game_event.index('\n    }')]
    assert f'Anchor {{ get; set; }} = "{m.DEFAULT_ANCHOR}";' in game_event
    assert f'Status {{ get; set; }} = "{m.DEFAULT_STATUS}";' in game_event


def test_real_content_every_event_lands_in_an_era():
    rows = m.load(ROOT)
    assert rows and all(r['era'] != '-' for r in rows)


def test_invalid_status_is_an_argparse_error_listing_schema_choices(tmp_path, capsys):
    import json
    import pytest
    schema = json.loads((ROOT / 'content/schema/event.schema.json').read_text(encoding='utf-8'))
    with pytest.raises(SystemExit) as error:
        m.main(['--root', str(repo(tmp_path)), '--status', 'drfat'])
    assert error.value.code == 2
    out, err = capsys.readouterr()
    assert out == ''
    assert 'invalid choice' in err and 'drfat' in err
    for status in schema['properties']['status']['enum']:
        assert status in err


def test_status_choices_match_the_schema_enum():
    import json
    schema = json.loads((ROOT / 'content/schema/event.schema.json').read_text(encoding='utf-8'))
    assert list(m.STATUS_CHOICES) == schema['properties']['status']['enum']
