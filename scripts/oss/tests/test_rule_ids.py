"""Rule ID report (#286): untested, dead and duplicate IDs."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('rule_ids', ROOT / 'scripts/oss/rule_ids.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)


def tree(tmp_path, rules='', test='', content=''):
    (tmp_path / 'docs/rules').mkdir(parents=True)
    (tmp_path / 'docs/rules/economy.md').write_text(rules)
    (tmp_path / 'sim/Statecraft.Sim.Tests').mkdir(parents=True)
    (tmp_path / 'sim/Statecraft.Sim.Tests/T.cs').write_text(test)
    (tmp_path / 'content').mkdir()
    (tmp_path / 'content/a.yaml').write_text(content)
    return tmp_path


def test_clean_tree(tmp_path):
    t = tree(tmp_path, 'R-ECON-014: tax is collected.\n- **R-ECON-015** spending.\n',
             '// R-ECON-014\n// R-ECON-015\n')
    assert m.check(t) == []


def test_defined_rule_with_no_test_is_untested(tmp_path):
    t = tree(tmp_path, 'R-ECON-014: tax is collected.\n', '// nothing here\n')
    assert [p.split(':')[0] for p in m.check(t)] == ['untested']


def test_a_citation_in_content_does_not_count_as_a_test(tmp_path):
    t = tree(tmp_path, 'R-ECON-014: tax.\n', '', '# R-ECON-014\n')
    assert any(p.startswith('untested: R-ECON-014') for p in m.check(t))


def test_citing_an_id_no_rule_defines_is_dead(tmp_path):
    t = tree(tmp_path, '', '// R-ECON-099\n', '# R-ECON-098\n')
    out = m.check(t)
    assert len(out) == 2 and all(p.startswith('dead: ') for p in out)


def test_the_same_id_defined_twice_is_reported(tmp_path):
    t = tree(tmp_path, 'R-ECON-014: a.\nR-ECON-014: b.\n', '// R-ECON-014\n')
    assert [p.split(':')[0] for p in m.check(t)] == ['duplicate']


def test_a_heading_that_starts_with_an_id_is_a_definition(tmp_path):
    t = tree(tmp_path, '## R-ECON-014 Tax\n', '// R-ECON-014\n')
    assert m.check(t) == []


def test_a_unity_file_citing_an_id_no_rule_defines_is_dead(tmp_path):
    t = tree(tmp_path)
    (t / 'unity/Assets/Tests/EditMode').mkdir(parents=True)
    (t / 'unity/Assets/Tests/EditMode/U.cs').write_text('// R-MAP-001\n')
    assert [p.split(':')[0] for p in m.check(t)] == ['dead']


def test_build_output_is_skipped_even_under_a_bin_folder(tmp_path):
    t = tree(tmp_path / 'bin' / 'repo', 'R-ECON-014: tax.\n', '// R-ECON-014\n')
    (t / 'sim/Statecraft.Sim/obj').mkdir(parents=True)
    (t / 'sim/Statecraft.Sim/obj/G.cs').write_text('// R-ECON-099\n')
    assert m.check(t) == []


def test_prose_mentioning_an_id_is_not_a_definition(tmp_path):
    t = tree(tmp_path, 'See R-ECON-014 for tax.\n', '')
    assert m.check(t) == []


def test_this_repository_has_no_untested_dead_or_duplicate_rule_ids():
    # The policy checker tests run in CI, so this is the CI gate for the real tree.
    assert m.check(ROOT) == []
