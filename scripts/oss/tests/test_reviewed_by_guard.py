"""scripts/oss/reviewed_by_guard.py (#375, "Yes, humans only"): no commit an AI agent co-wrote adds, changes or removes a
`reviewed_by` / `reviewed_on` line in content/, and a person's `reviewed_on` is not after their commit. It fails closed.
Runs on throwaway git repositories."""
import os
import subprocess

import reviewed_by_guard as guard
from conftest import git

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
    assert len(guard.check(root, base, "HEAD")) == 1
    root2, base2 = repo(tmp_path / "two", SIGNED)
    edit(root2, ROW)
    commit(root2, "content: drop the sign-off" + AI)
    assert len(guard.check(root2, base2, "HEAD")) == 1


def test_an_agent_commit_may_touch_other_lines_comments_and_renames(tmp_path):
    root, base = repo(tmp_path, SIGNED)
    edit(root, "# reviewed_by: only a person writes it (#375)\n" + SIGNED.replace("name: A", "name: A2"))
    commit(root, "content: rename A, a comment" + AI)
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
