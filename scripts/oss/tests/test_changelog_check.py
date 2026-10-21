import changelog_check as cc
from conftest import commit_all


def test_user_visible_change_without_changelog_fails_and_names_the_paths():
    ok, msg = cc.verdict(["content/events/x.yaml", "README.md"], skip_label=False)
    assert not ok and "content/events/x.yaml" in msg and "## [Unreleased]" in msg and "no-changelog" in msg


def test_changelog_entry_satisfies_it():
    assert cc.verdict(["sim/Statecraft.Sim/Rng.cs", "CHANGELOG.md"], skip_label=False)[0]


def test_label_satisfies_it():
    assert cc.verdict(["unity/Assets/Scripts/UI/GameRoot.cs"], skip_label=True)[0]


def test_tests_docs_tools_and_ci_need_no_entry():
    for p in ("unity/Assets/Tests/EditMode/MapTests.cs", "sim/Statecraft.Sim.Tests/CoreTests.cs", "docs/x.md",
              "tools/hexgrid.py", ".github/workflows/ci.yml", "scripts/oss/check_repo.py"):
        assert cc.verdict([p], skip_label=False)[0], p


def test_every_user_visible_root_is_named():
    for p in ("content/people.yaml", "sim/Statecraft.Sim/Rng.cs", "sim/Statecraft.Cli/Program.cs",
              "unity/Assets/UI/Game.uxml", "unity/ProjectSettings/ProjectSettings.asset", "unity/Packages/manifest.json"):
        assert cc.user_visible([p]) == [p], p


def test_an_added_fragment_satisfies_it_and_the_failure_names_that_way():
    ok, msg = cc.verdict(["content/people.yaml", "changelog.d/1180.md"], skip_label=False,
                         fragments=["changelog.d/1180.md"])
    assert ok and "changelog.d/1180.md" in msg
    assert "changelog.d/" in cc.verdict(["content/people.yaml"], skip_label=False)[1]


def test_only_a_changelog_d_markdown_file_other_than_the_readme_is_a_fragment():
    assert cc.is_fragment("changelog.d/fix-zoom.md")
    for p in ("changelog.d/README.md", "changelog.d/x.txt", "changelog.d/sub/x.md", "docs/changelog.d/x.md",
              "CHANGELOG.md"):
        assert not cc.is_fragment(p), p


def test_a_fragment_needs_a_bullet_that_says_something():
    assert cc.has_entry("- The map zooms from the keyboard.\n")
    assert cc.has_entry("### Fixed\n- Money negatives put the sign next to the number.\n  It wraps.\n")
    for text in ("", "  \n\t\n", "### Added\n", "- \n-\n", "The map zooms.\n"):
        assert not cc.has_entry(text), repr(text)


def _pr(make_repo, base_files, head_changes):
    """Commits base_files, then head_changes (None deletes the file) as the PR's commit. Returns (root, base, head)."""
    root = make_repo({"README.md": "x\n", **base_files})
    base = commit_all(root, "base")
    for rel, data in head_changes.items():
        p = root / rel
        if data is None:
            p.unlink()
        else:
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(data, encoding="utf-8")
    return root, base, commit_all(root, "pr")


def _run(root, base, head, capsys, monkeypatch):
    monkeypatch.chdir(root)
    code = cc.main(["--base", base, "--head", head])
    return code, capsys.readouterr().out


def test_a_pr_adding_a_fragment_passes(make_repo, capsys, monkeypatch):
    root, base, head = _pr(make_repo, {"content/a.yaml": "a: 1\n"},
                           {"content/a.yaml": "a: 2\n", "changelog.d/a.md": "- Content: a is 2.\n"})
    assert _run(root, base, head, capsys, monkeypatch) == (0, "A changelog fragment is added: changelog.d/a.md\n")


def test_an_empty_blank_readme_or_non_markdown_fragment_fails_closed(make_repo, capsys, monkeypatch):
    root, base, head = _pr(make_repo, {"content/a.yaml": "a: 1\n"},
                           {"content/a.yaml": "a: 2\n", "changelog.d/a.md": "", "changelog.d/b.md": " \n\n",
                            "changelog.d/README.md": "- Not an entry.\n", "changelog.d/c.txt": "- Not an entry.\n"})
    code, out = _run(root, base, head, capsys, monkeypatch)
    assert code == 1 and "content/a.yaml" in out


def test_editing_an_existing_fragment_fails_closed(make_repo, capsys, monkeypatch):
    root, base, head = _pr(make_repo, {"content/a.yaml": "a: 1\n", "changelog.d/old.md": "- Old entry.\n"},
                           {"content/a.yaml": "a: 2\n", "changelog.d/old.md": "- Old entry.\n- A second one.\n"})
    code, out = _run(root, base, head, capsys, monkeypatch)
    assert code == 1 and "content/a.yaml" in out


def test_deleting_or_renaming_an_existing_fragment_fails_closed(make_repo, capsys, monkeypatch):
    root, base, head = _pr(make_repo, {"content/a.yaml": "a: 1\n", "changelog.d/old.md": "- Old entry.\n",
                                       "changelog.d/older.md": "- An older entry, long enough to match a rename.\n"},
                           {"content/a.yaml": "a: 2\n", "changelog.d/old.md": None, "changelog.d/older.md": None,
                            "changelog.d/new.md": "- An older entry, long enough to match a rename.\n"})
    code, out = _run(root, base, head, capsys, monkeypatch)
    assert code == 1 and "content/a.yaml" in out


def test_a_changelog_line_still_passes_end_to_end(make_repo, capsys, monkeypatch):
    root, base, head = _pr(make_repo, {"content/a.yaml": "a: 1\n", "CHANGELOG.md": "## [Unreleased]\n"},
                           {"content/a.yaml": "a: 2\n", "CHANGELOG.md": "## [Unreleased]\n- Content: a is 2.\n"})
    assert _run(root, base, head, capsys, monkeypatch) == (0, "CHANGELOG.md is updated.\n")
