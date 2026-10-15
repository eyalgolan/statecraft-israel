"""scripts/oss/reviewed_by_guard.py (#375, "Yes, humans only"): no commit an AI agent co-wrote adds, changes or removes a
`reviewed_by` / `reviewed_on` line in content/, and a person's `reviewed_on` is not after their commit. It fails closed.
Runs on throwaway git repositories."""
import os
import subprocess

import pytest

import reviewed_by_guard as guard
from conftest import git

@pytest.fixture(autouse=True)
def an_explicit_git_identity(monkeypatch):
    """Every git call here (commits, merges, the guard itself) gets an explicit identity and default branch, so the tests
    do not lean on a guessed name and email: CI's runner refuses to guess one (#629, Repo policy run 38736960399)."""
    for k, v in {"GIT_AUTHOR_NAME": "t", "GIT_AUTHOR_EMAIL": "t@users.noreply.github.com",
                 "GIT_COMMITTER_NAME": "t", "GIT_COMMITTER_EMAIL": "t@users.noreply.github.com"}.items():
        monkeypatch.setenv(k, v)


AI = "\n\nCo-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>"
ROW = "people:\n  - id: a\n    name: A\n    status: fact-checked\n"
SIGNED = ROW + "    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n"


def commit(root, msg, date="2026-10-15T12:00:00+00:00"):
    git(root, "add", "-A")
    subprocess.run(["git", "-C", str(root), "-c", "user.name=t", "-c", "user.email=t@users.noreply.github.com", "commit", "-qm", msg],
                   check=True, capture_output=True, env={**os.environ, "GIT_AUTHOR_DATE": date, "GIT_COMMITTER_DATE": date})
    return git(root, "rev-parse", "HEAD").strip()


def repo(tmp_path, text=ROW):
    root = tmp_path / "r"
    (root / "content").mkdir(parents=True)
    git(root, "init", "-q", "-b", "main")
    (root / "content" / "people.yaml").write_text(text, encoding="utf-8")
    base = commit(root, "content: people" + AI)
    return root, base


def edit(root, text, rel="content/people.yaml"):
    (root / rel).write_text(text, encoding="utf-8")


def test_a_persons_sign_off_passes(tmp_path):
    root, base = repo(tmp_path)
    edit(root, SIGNED)
    commit(root, "content: sign off A")
    assert guard.check(root, base, "HEAD") == []


def test_an_agent_commit_that_adds_a_sign_off_fails(tmp_path, capsys):
    root, base = repo(tmp_path)
    edit(root, SIGNED)
    sha = commit(root, "content: sign off A" + AI)
    problems = guard.check(root, base, "HEAD")
    assert len(problems) == 1 and sha[:10] in problems[0] and "content/people.yaml" in problems[0]
    assert guard.main(["--repo", str(root), "--base", base, "--head", "HEAD"]) == 1
    assert "AI co-author" in capsys.readouterr().out


def test_an_agent_commit_that_changes_or_removes_a_sign_off_fails(tmp_path):
    root, base = repo(tmp_path, SIGNED)
    edit(root, SIGNED.replace("2026-10-15", "2026-10-14"))
    commit(root, "content: fix the day" + AI)
    assert any("changes a reviewed_by / reviewed_on line" in p for p in guard.check(root, base, "HEAD"))
    root2, base2 = repo(tmp_path / "two", SIGNED)
    edit(root2, ROW)
    commit(root2, "content: drop the sign-off" + AI)
    assert any("changes a reviewed_by / reviewed_on line" in p for p in guard.check(root2, base2, "HEAD"))


def test_an_agent_commit_may_touch_other_lines_comments_and_renames(tmp_path):
    """Outside a signed row an agent may edit: a comment naming the keys above it, and a pure move of the file. (An edit
    inside a signed row is the owner's "Block it in CI" rule, tested below.)"""
    root, base = repo(tmp_path, SIGNED)
    edit(root, "# reviewed_by: only a person writes it (#375)\n" + SIGNED)
    commit(root, "content: a comment" + AI)
    git(root, "mv", "content/people.yaml", "content/people2.yaml")
    commit(root, "content: move the file" + AI)
    assert guard.check(root, base, "HEAD") == []


def test_a_flow_style_sign_off_by_an_agent_fails(tmp_path):
    root, base = repo(tmp_path, "parties:\n  - { id: a, name: A, status: fact-checked }\n")
    edit(root, "parties:\n  - { id: a, name: A, status: fact-checked, reviewed_by: eyalgolan, reviewed_on: 2026-10-15 }\n")
    commit(root, "content: sign off" + AI)
    assert len(guard.check(root, base, "HEAD")) == 1


def test_a_sign_off_dated_after_its_commit_fails(tmp_path):
    root, base = repo(tmp_path)
    edit(root, SIGNED.replace("2026-10-15", "2026-10-16"))
    commit(root, "content: sign off A", date="2026-10-15T12:00:00+00:00")
    problems = guard.check(root, base, "HEAD")
    assert len(problems) == 1 and "after" in problems[0]


def test_it_fails_closed_when_it_cannot_read_the_range(tmp_path, capsys):
    root, base = repo(tmp_path)
    assert guard.main(["--repo", str(root), "--base", "0" * 40, "--head", "HEAD"]) == 1
    assert guard.main(["--repo", str(root), "--base", "no-such-ref", "--head", "HEAD"]) == 1
    edit(root, SIGNED)
    commit(root, "content: sign off A")
    shallow = tmp_path / "shallow"
    subprocess.run(["git", "clone", "-q", "--depth", "1", root.as_uri(), str(shallow)], check=True, capture_output=True)
    assert guard.main(["--repo", str(shallow), "--base", "HEAD~1", "--head", "HEAD"]) == 1
    assert "cannot" in capsys.readouterr().out


def test_a_commit_whose_message_cannot_be_read_fails_closed(tmp_path, monkeypatch):
    root, base = repo(tmp_path)
    edit(root, SIGNED)
    commit(root, "content: sign off A")
    real = guard._git

    def broken(args, cwd):
        if args[:2] == ["log", "-1"]:
            raise subprocess.CalledProcessError(128, ["git", *args])
        return real(args, cwd)

    monkeypatch.setattr(guard, "_git", broken)
    assert guard.main(["--repo", str(root), "--base", base, "--head", "HEAD"]) == 1


# ---- second review of #629: the guard's diff reading must not fail open ------------------------------------------------

def test_a_quoted_key_is_still_a_sign_off(tmp_path):
    for i, text in enumerate([ROW + '    "reviewed_by": eyalgolan\n    "reviewed_on": 2026-10-15\n',
                              ROW + "    'reviewed_by': eyalgolan\n    'reviewed_on': 2026-10-15\n"]):
        root, base = repo(tmp_path / str(i))
        edit(root, text)
        commit(root, "content: sign off A" + AI)
        assert len(guard.check(root, base, "HEAD")) == 1, text


def test_paths_with_a_space_or_hebrew_are_read(tmp_path):
    for i, rel in enumerate(["content/my people.yaml", "content/אנשים.yaml", "content/sub dir/p.yml"]):
        root, base = repo(tmp_path / str(i))
        (root / rel).parent.mkdir(parents=True, exist_ok=True)
        edit(root, SIGNED, rel)
        commit(root, "content: sign off A" + AI)
        problems = guard.check(root, base, "HEAD")
        assert len(problems) == 1 and rel in problems[0], (rel, problems)


def test_a_binary_attribute_does_not_hide_the_lines(tmp_path):
    root, base = repo(tmp_path)
    (root / ".gitattributes").write_text("content/*.yaml -diff\n", encoding="utf-8")
    edit(root, SIGNED)
    commit(root, "content: sign off A" + AI)
    assert len(guard.check(root, base, "HEAD")) == 1


def test_local_git_config_does_not_change_what_the_guard_reads(tmp_path):
    for i, (key, value) in enumerate([("color.ui", "always"), ("diff.noprefix", "true"), ("diff.mnemonicPrefix", "true"),
                                      ("core.quotePath", "true"), ("diff.relative", "true"), ("diff.external", "false")]):
        root, base = repo(tmp_path / str(i))
        git(root, "config", key, value)
        edit(root, SIGNED)
        commit(root, "content: sign off A" + AI)
        assert len(guard.check(root, base, "HEAD")) == 1, key


def branch_with_main_signed(tmp_path, main_text):
    """main: the row signed off (or changed) by a person; branch: an agent's own commit; then the agent merges main in."""
    root, base = repo(tmp_path, ROW + "  - id: b\n    name: B\n    status: fact-checked\n")
    git(root, "checkout", "-q", "-b", "agent")
    (root / "content" / "other.yaml").write_text("rows: []\n", encoding="utf-8")
    commit(root, "content: other" + AI)
    git(root, "checkout", "-q", "main")
    edit(root, main_text)
    commit(root, "content: sign off A")
    main_tip = git(root, "rev-parse", "HEAD").strip()
    git(root, "checkout", "-q", "agent")
    subprocess.run(["git", "-C", str(root), "-c", "user.name=t", "-c", "user.email=t@users.noreply.github.com", "merge", "-q",
                    "--no-ff", "-m", "Merge main" + AI, "main"], check=True, capture_output=True)
    return root, main_tip


def test_merging_main_with_a_persons_sign_off_is_not_a_false_failure(tmp_path):
    signed = ROW + "    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n  - id: b\n    name: B\n    status: fact-checked\n"
    root, main_tip = branch_with_main_signed(tmp_path, signed)
    assert guard.check(root, main_tip, "HEAD") == []


def test_an_agent_merge_that_adds_a_sign_off_itself_fails(tmp_path):
    root, base = repo(tmp_path)
    git(root, "checkout", "-q", "-b", "agent")
    (root / "content" / "other.yaml").write_text("rows: []\n", encoding="utf-8")
    commit(root, "content: other" + AI)
    git(root, "checkout", "-q", "main")
    (root / "content" / "third.yaml").write_text("rows: []\n", encoding="utf-8")
    commit(root, "content: third")
    main_tip = git(root, "rev-parse", "HEAD").strip()
    git(root, "checkout", "-q", "agent")
    subprocess.run(["git", "-C", str(root), "merge", "-q", "--no-ff", "--no-commit", "main"], check=True, capture_output=True)
    edit(root, SIGNED)   # an "evil merge": the sign-off comes from the merge commit itself, in no parent
    commit(root, "Merge main" + AI)
    assert len(guard.check(root, main_tip, "HEAD")) == 1


# ---- owner ruling, 15 October 2026 ("Block it in CI"): an agent may not edit a row a person signed off ------------------

TWO = SIGNED + "  - id: b\n    name: B\n    status: fact-checked\n"


def test_an_agent_edit_to_a_signed_row_fails(tmp_path):
    for i, change in enumerate([("name: A", "name: A2"), ("id: a", "id: a2"), ("status: fact-checked\n    reviewed", "status: draft\n    reviewed")]):
        root, base = repo(tmp_path / str(i), TWO)
        edit(root, TWO.replace(*change, 1))
        commit(root, "content: edit A" + AI)
        problems = guard.check(root, base, "HEAD")
        assert len(problems) == 1 and "signed off" in problems[0] and "'a'" in problems[0], (change, problems)


def test_an_agent_key_added_inside_a_signed_row_fails(tmp_path):
    root, base = repo(tmp_path, TWO)
    edit(root, TWO.replace("    name: A\n", "    name: A\n    party: x\n"))
    commit(root, "content: add a key to A" + AI)
    assert len(guard.check(root, base, "HEAD")) == 1


def test_an_agent_edit_to_an_unsigned_row_or_a_new_row_passes(tmp_path):
    root, base = repo(tmp_path, TWO)
    edit(root, TWO.replace("name: B", "name: B2") + "  - id: c\n    name: C\n    status: draft\n")
    commit(root, "content: edit B, add C" + AI)
    assert guard.check(root, base, "HEAD") == []


def test_a_person_re_signing_or_unsigning_after_the_agent_edit_passes(tmp_path):
    for i, after in enumerate([TWO.replace("name: A", "name: A2").replace("2026-10-15", "2026-10-16"),
                               TWO.replace("name: A", "name: A2").replace("    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n", "")]):
        root, base = repo(tmp_path / str(i), TWO)
        edit(root, TWO.replace("name: A", "name: A2"))
        commit(root, "content: edit A" + AI)
        assert len(guard.check(root, base, "HEAD")) == 1
        edit(root, after)
        commit(root, "content: re-read A and sign again", date="2026-10-16T12:00:00+00:00")
        assert guard.check(root, base, "HEAD") == [], i


def test_merging_main_where_a_person_edited_and_signed_a_row_is_not_a_false_failure(tmp_path):
    signed = TWO.replace("name: A", "name: A (corrected)")
    root, main_tip = branch_with_main_signed(tmp_path, signed)
    assert guard.check(root, main_tip, "HEAD") == []


# ---- scoped re-review of #629: three ways round the signed-row rule ---------------------------------------------------

def test_a_key_inserted_after_a_comment_or_a_blank_line_is_still_inside_the_row(tmp_path):
    for i, lead in enumerate(["    # note\n", "\n", "    # one\n\n"]):
        root, base = repo(tmp_path / str(i), TWO)
        edit(root, TWO.replace("    reviewed_on: 2026-10-15\n", "    reviewed_on: 2026-10-15\n" + lead + "    born: 1900\n"))
        commit(root, "content: add born to A" + AI)
        assert len(guard.check(root, base, "HEAD")) == 1, repr(lead)
    root, base = repo(tmp_path / "eof", SIGNED)
    edit(root, SIGNED + "    # note\n    born: 1900\n")
    commit(root, "content: add born to A at the end" + AI)
    assert len(guard.check(root, base, "HEAD")) == 1


def test_a_comment_or_blank_line_alone_after_a_signed_row_passes(tmp_path):
    root, base = repo(tmp_path, TWO)
    edit(root, TWO.replace("    reviewed_on: 2026-10-15\n", "    reviewed_on: 2026-10-15\n\n    # a note between rows\n"))
    commit(root, "content: a note" + AI)
    assert guard.check(root, base, "HEAD") == []


def test_a_person_re_signing_another_row_does_not_release_the_edited_one(tmp_path):
    both = SIGNED + "  - id: b\n    name: B\n    status: fact-checked\n    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n"
    root, base = repo(tmp_path, both)
    edit(root, both.replace("name: A", "name: A2"))
    commit(root, "content: edit A" + AI)
    edit(root, both.replace("name: A", "name: A3").replace("    name: B\n    status: fact-checked\n    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n",
                                                          "    name: B\n    status: fact-checked\n    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-16\n"))
    commit(root, "content: fix A's typo, re-sign B", date="2026-10-16T12:00:00+00:00")
    problems = guard.check(root, base, "HEAD")
    assert len(problems) == 1 and "'a'" in problems[0]


def test_an_anchor_a_signed_row_uses_is_part_of_it(tmp_path):
    text = ("people:\n  - id: x\n    name: X\n    sources: [&src { url: 'https://example.org/1', checked: 2026-10-14 }]\n"
            "    status: draft\n"
            "  - id: a\n    name: A\n    sources: [*src]\n    status: fact-checked\n    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n")
    root, base = repo(tmp_path, text)
    edit(root, text.replace("https://example.org/1", "https://example.org/2"))
    commit(root, "content: change X's source" + AI)
    problems = guard.check(root, base, "HEAD")
    assert len(problems) == 1 and "'a'" in problems[0]
    merge = ("defaults: &d\n  status: fact-checked\n  note: n\n"
             "people:\n  - id: a\n    <<: *d\n    name: A\n    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n")
    root2, base2 = repo(tmp_path / "merge", merge)
    edit(root2, merge.replace("note: n", "note: m"))
    commit(root2, "content: change the defaults" + AI)
    assert len(guard.check(root2, base2, "HEAD")) == 1
