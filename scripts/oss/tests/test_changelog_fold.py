import pytest

import changelog_fold as cf

LOG = """# Changelog

Intro.

## [Unreleased]

### Added
- An entry already here.

### Fixed
- A fix already here.

## [0.1.2] - 2026-10-19

### Added
- Released.
"""


def test_entries_go_to_the_top_of_their_section_in_filename_order():
    out = cf.fold(LOG, [("b.md", "- B, added.\n  Wrapped.\n"),
                        ("a.md", "### Fixed\n- A, fixed.\n\n### Added\n- A, added.\n")])
    assert out == LOG.replace("### Added\n- An entry", "### Added\n- A, added.\n- B, added.\n  Wrapped.\n- An entry") \
                     .replace("### Fixed\n- A fix", "### Fixed\n- A, fixed.\n- A fix")


def test_a_section_unreleased_lacks_is_added_at_its_end():
    out = cf.fold(LOG, [("a.md", "### Changed\n- A, changed.\n")])
    assert out == LOG.replace("- A fix already here.\n", "- A fix already here.\n\n### Changed\n- A, changed.\n")


def test_an_empty_unreleased_after_a_release():
    log = "# Changelog\n\n## [Unreleased]\n\n## [0.1.2] - 2026-10-19\n\n### Added\n- Released.\n"
    out = cf.fold(log, [("a.md", "- A.\n")])
    assert out == log.replace("## [Unreleased]\n", "## [Unreleased]\n\n### Added\n- A.\n")


def test_it_refuses_a_fragment_with_no_entry_or_a_changelog_with_no_unreleased():
    with pytest.raises(ValueError, match="a.md"):
        cf.fold(LOG, [("a.md", "### Added\n\n")])
    with pytest.raises(ValueError, match="Unreleased"):
        cf.fold("# Changelog\n", [("a.md", "- A.\n")])


def test_main_folds_and_deletes_the_fragments_but_keeps_the_readme(tmp_path, capsys):
    (tmp_path / "CHANGELOG.md").write_text(LOG, encoding="utf-8")
    d = tmp_path / "changelog.d"
    d.mkdir()
    (d / "README.md").write_text("- Not an entry.\n", encoding="utf-8")
    (d / "z.md").write_text("- Z.\n", encoding="utf-8")
    (d / "a.md").write_text("- A.\n", encoding="utf-8")
    assert cf.main(["--root", str(tmp_path)]) == 0
    assert "- A.\n- Z.\n- An entry already here." in (tmp_path / "CHANGELOG.md").read_text(encoding="utf-8")
    assert sorted(p.name for p in d.iterdir()) == ["README.md"]
    assert "2 fragments" in capsys.readouterr().out


def test_main_changes_nothing_when_a_fragment_is_refused(tmp_path):
    (tmp_path / "CHANGELOG.md").write_text(LOG, encoding="utf-8")
    d = tmp_path / "changelog.d"
    d.mkdir()
    (d / "a.md").write_text("- A.\n", encoding="utf-8")
    (d / "b.md").write_text("\n", encoding="utf-8")
    assert cf.main(["--root", str(tmp_path)]) == 1
    assert (tmp_path / "CHANGELOG.md").read_text(encoding="utf-8") == LOG
    assert sorted(p.name for p in d.iterdir()) == ["a.md", "b.md"]
