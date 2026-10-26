"""Source report (#330): nested sources found, oldest first, flags, the age filter and the real content."""
import importlib.util
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('source_report', ROOT / 'scripts/source_report.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)
TODAY = date(2026, 10, 13)


def repo(tmp_path):
    (tmp_path / 'content/map').mkdir(parents=True)
    (tmp_path / 'content/a.yaml').write_text(
        "x:\n  - id: 1\n    sources:\n      - { url: 'https://a.example/1', checked: 2026-10-10 }\n"
        "      - { url: 'http://old.example', checked: 2026-01-01 }\n")
    (tmp_path / 'content/map/b.yaml').write_text("y:\n  source: { url: 'https://b.example', checked: '2026-10-12' }\n  bad: { url: 'https://c.example', checked: soon }\n")
    return tmp_path


def test_collects_nested_sources_oldest_first_with_flags(tmp_path):
    rows = m.collect(repo(tmp_path), TODAY)
    assert [r['url'] for r in rows] == ['https://c.example', 'http://old.example', 'https://a.example/1', 'https://b.example']
    assert [r['age'] for r in rows] == [None, 285, 3, 1]
    out = m.render(rows)
    assert 'NO-DATE' in out.splitlines()[0] and out.splitlines()[1].endswith(' http')
    assert out.splitlines()[-1] == '4 sources'


def test_older_than_keeps_stale_and_undated(tmp_path, capsys):
    assert m.main(['--root', str(repo(tmp_path)), '--older-than', '30', '--today', '2026-10-13']) == 0
    out = capsys.readouterr().out
    assert 'old.example' in out and 'c.example' in out and 'b.example' not in out and out.splitlines()[-1] == '2 sources'


def test_real_content_has_sources_and_all_are_dated():
    rows = m.collect(ROOT, date.today())
    assert rows and all(r['checked'] is not None for r in rows)


def test_render_shows_the_date(tmp_path):
    assert '2026-10-10' in m.render(m.collect(repo(tmp_path), TODAY))


def test_alias_counts_once_and_plain_url_lists_are_read(tmp_path):
    (tmp_path / 'content/map').mkdir(parents=True)
    (tmp_path / 'content/p.yaml').write_text(
        "a:\n  name: &bio [{ url: 'https://p.example', checked: 2026-10-01 }]\n  born: *bio\n  died: *bio\n")
    (tmp_path / 'content/map/l.yaml').write_text(
        "- id: x\n  checked: '2026-10-11'\n  sources:\n  - https://l.example/1\n  - https://l.example/2\n")
    rows = m.collect(tmp_path, TODAY)
    assert sorted((r['url'], r['age']) for r in rows) == [('https://l.example/1', 2), ('https://l.example/2', 2),
                                                          ('https://p.example', 12)]


def test_root_without_content_is_an_error(tmp_path, capsys):
    # #1460: a mistyped --root must not look like a repository with no sources
    for root in (tmp_path, tmp_path / 'missing'):
        assert m.main(['--root', str(root), '--today', '2026-10-13']) != 0
        cap = capsys.readouterr()
        assert str(root / 'content') in cap.err and 'sources' not in cap.out
        assert not (root / 'content').exists()
    assert not (tmp_path / 'missing').exists()


def test_empty_content_is_still_a_report(tmp_path, capsys):
    (tmp_path / 'content').mkdir()
    assert m.main(['--root', str(tmp_path), '--today', '2026-10-13']) == 0
    assert capsys.readouterr().out.splitlines() == ['0 sources']
