"""tools/review_queue.py: the human-review queue (CONTENT_POLICY section 2, rule 6). Runs on a throwaway git repository, so
it needs no history of the real one (CI checks out shallow)."""
import hashlib
import os
import subprocess
from pathlib import Path

import review_queue as rq

AI_TRAILER = "\n\nCo-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>\n"

FLOW = """offices:
  - { id: pm, title: Prime Minister, title_he: "ראש הממשלה", title_he_review: agent-checked, title_he_sources: [{ url: "https://knesset.gov.il/x", checked: 2026-10-14 }] }
  - { id: fm, title: Minister of Finance, title_he: "שר האוצר", title_he_sources: [{ url: "https://knesset.gov.il/y", checked: 2026-10-14 }] }
"""
BLOCK = """governments:
  - id: g1
    name: First
    name_he: "ממשלה"
    name_he_review: agent-checked
    name_he_sources:
      - { url: "https://he.wikipedia.org/wiki/g", checked: 2026-10-14 }
    sources:
      - { url: "https://en.wikipedia.org/wiki/g", checked: 2026-10-09 }
    status: fact-checked
  - id: g2
    name: Second
    status: draft
"""
EVENT = """id: truce
title: A truce
sources:
  - { url: "https://example.org/truce", checked: 2026-10-10 }
status: sensitivity-reviewed
"""
PEOPLE = """people:
  - id: example
    name: Example Person
    name_he: איש לדוגמה
    name_he_review: agent-checked
    biography_sources:
      name_he: [{ url: "https://example.org/hebrew-name", checked: 2026-10-14 }]
"""


def git(repo: Path, *args: str, env: dict | None = None) -> str:
    base = {**os.environ, "GIT_AUTHOR_NAME": "A Person", "GIT_AUTHOR_EMAIL": "a@example.org",
            "GIT_COMMITTER_NAME": "A Person", "GIT_COMMITTER_EMAIL": "a@example.org"}
    return subprocess.run(["git", *args], cwd=repo, check=True, capture_output=True, text=True, env={**base, **(env or {})}).stdout


def make_repo(tmp_path: Path) -> Path:
    repo = tmp_path / "repo"
    (repo / "content" / "events").mkdir(parents=True)
    git(repo, "init", "-q")
    (repo / "content" / "offices.yaml").write_text(FLOW, encoding="utf-8")
    (repo / "content" / "governments.yaml").write_text(BLOCK, encoding="utf-8")
    git(repo, "add", "content")
    git(repo, "commit", "-q", "-m", "content: offices and governments (#10)" + AI_TRAILER)
    (repo / "content" / "events" / "truce.yaml").write_text(EVENT, encoding="utf-8")
    git(repo, "add", "content")
    git(repo, "commit", "-q", "-m", "content: the truce, reviewed by the owner (#11)")
    return repo


def test_scan_finds_flags_and_reviewed_statuses_with_their_lines():
    flags = rq.scan(FLOW, "content/offices.yaml")
    assert [(e.kind, e.line, e.row, e.field, e.value, e.sources) for e in flags] == [
        ("flag", 2, "pm", "title_he", "ראש הממשלה", ["https://knesset.gov.il/x"])]   # fm has no flag: reviewed
    block = rq.scan(BLOCK, "content/governments.yaml")
    assert [(e.kind, e.line, e.row, e.field) for e in block] == [("flag", 5, "g1", "name_he"), ("status", 10, "g1", "status")]
    assert block[1].sources == ["https://en.wikipedia.org/wiki/g"]   # the row's own sources, not the name's
    event = rq.scan(EVENT, "content/events/truce.yaml")
    assert [(e.kind, e.line, e.row, e.value) for e in event] == [("status", 5, "truce", "sensitivity-reviewed")]
    assert rq.scan("rows:\n  - { id: x, name_he_review: reviewed }\n", "c.yaml") == []   # only agent-checked is a flag


def test_people_name_uses_its_biography_source_in_the_page():
    entries = rq.scan(PEOPLE, "content/people.yaml")
    text = rq.page(entries, 0, "0123456789abcdef")
    assert "| example | name_he | איש לדוגמה | <https://example.org/hebrew-name> |" in text


def test_queue_lists_agent_set_statuses_and_every_flag_and_counts_the_rest(tmp_path):
    repo = make_repo(tmp_path)
    entries, human, _ = rq.queue(repo)
    assert [(e.kind, e.file, e.line, e.row) for e in entries] == [
        ("flag", "content/governments.yaml", 5, "g1"),
        ("status", "content/governments.yaml", 10, "g1"),
        ("flag", "content/offices.yaml", 2, "pm"),
    ]
    assert human == 1   # the truce's status came from a commit with no agent marker
    status = entries[1]
    assert status.checked_by.startswith("a commit with an AI co-author trailer (PR #10")
    assert entries[0].checked_by == "an AI agent (the `agent-checked` flag)"


def test_an_agent_marker_is_read_from_the_whole_message():
    for body, agent in [("x" + AI_TRAILER, True), ("x\n\n🤖 Generated with [Claude Code](https://claude.com/claude-code)", True),
                        ("x\n\nCo-Authored-By: Codex <noreply@example.org>", True), ("x\n\nCo-Authored-By: A Person <a@example.org>", False),
                        ("content: reviewed by the owner", False)]:
        assert bool(rq.AI_MARK.search(body)) is agent, body


def test_the_queue_changes_no_content(tmp_path):
    repo = make_repo(tmp_path)
    before = {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in (repo / "content").rglob("*.yaml")}
    entries, human, _ = rq.queue(repo)
    rq.page(entries, human, git(repo, "rev-parse", "HEAD").strip())
    assert {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in (repo / "content").rglob("*.yaml")} == before
    assert git(repo, "status", "--porcelain") == ""


def test_the_page_groups_by_file_and_links_each_line(tmp_path):
    repo = make_repo(tmp_path)
    entries, human, _ = rq.queue(repo)
    text = rq.page(entries, human, "0123456789abcdef")
    assert "at commit `01234567`" in text
    assert "- Hebrew names flagged `agent-checked`: **2**" in text
    assert "co-author trailer, or not committed yet: **1**" in text
    assert "### [content/offices.yaml](../content/offices.yaml) (1)" in text
    assert "| [2](../content/offices.yaml#L2) | pm | title_he | ראש הממשלה | <https://knesset.gov.il/x> |" in text
    assert "[10](../content/governments.yaml#L10) | g1 | fact-checked |" in text
    assert "truce" not in text   # human-set: counted, not listed


def test_deleting_a_flag_then_regenerating_keeps_every_line_right(tmp_path):
    """The reviewer's step is to delete a *_review line in the working tree; the queue then drops that flag and the lines
    below it move up, still blamed on the commits that wrote them (second review of #611: it blamed HEAD but read the
    working tree)."""
    repo = make_repo(tmp_path)
    gov = repo / "content" / "governments.yaml"
    gov.write_text(gov.read_text(encoding="utf-8").replace("    name_he_review: agent-checked\n", ""), encoding="utf-8")
    entries, human, _ = rq.queue(repo)
    assert [(e.kind, e.file, e.line, e.row) for e in entries] == [
        ("status", "content/governments.yaml", 9, "g1"), ("flag", "content/offices.yaml", 2, "pm")]
    assert entries[0].checked_by.startswith("a commit with an AI co-author trailer (PR #10")
    assert human == 1


def test_an_uncommitted_status_is_listed_as_not_committed_yet(tmp_path):
    """A reviewed status nobody has committed yet has no commit to vouch for it, so it is listed, as "not committed yet"
    (independent re-review of #611: it was counted with the human-set ones and hidden). The extra line above it must not
    crash the blame or shift the lines."""
    repo = make_repo(tmp_path)
    ev = repo / "content" / "events" / "truce.yaml"
    ev.write_text("# a new comment line\n" + ev.read_text(encoding="utf-8").replace("status: sensitivity-reviewed", "status: historian-reviewed"),
                  encoding="utf-8")
    entries, human, _ = rq.queue(repo)
    truce = [e for e in entries if e.row == "truce"]
    assert [(e.kind, e.line, e.value, e.checked_by) for e in truce] == [("status", 6, "historian-reviewed", "not committed yet")]
    assert human == 0
    gov = repo / "content" / "governments.yaml"
    gov.write_text(gov.read_text(encoding="utf-8").replace("status: draft", "status: fact-checked"), encoding="utf-8")
    g2 = [e for e in rq.queue(repo)[0] if e.row == "g2"]
    assert [(e.line, e.value, e.checked_by) for e in g2] == [(13, "fact-checked", "not committed yet")]


def test_historian_reviewed_counts_as_a_reviewed_status():
    assert [e.value for e in rq.scan(EVENT.replace("sensitivity-reviewed", "historian-reviewed"), "e.yaml")] == ["historian-reviewed"]


def test_a_long_subject_is_cut_before_its_pr_number():
    c = rq.Commit("a" * 40, "A Person", "content: " + "x" * 120 + " (#123)", True)
    text = c.describe()
    assert text.startswith("a commit with an AI co-author trailer (PR #123: content: ")
    assert "(#" not in text and text.endswith("…)")


def test_a_shallow_clone_is_refused(tmp_path, capsys):
    repo = make_repo(tmp_path)
    shallow = tmp_path / "shallow"
    subprocess.run(["git", "clone", "-q", "--depth", "1", repo.as_uri(), str(shallow)], check=True, capture_output=True)
    assert rq.run(shallow, write=False) == 2
    assert "shallow clone" in capsys.readouterr().err
    assert rq.run(repo, write=False) == 0
    out = capsys.readouterr().out
    assert out.startswith("# Human-review queue")
    assert "over-inclusive" in out


def test_an_untracked_content_file_is_scanned_and_listed_as_not_committed_yet(tmp_path):
    """Independent re-review of #611 (A): git ls-files missed a new, untracked file, so its rows were never scanned."""
    repo = make_repo(tmp_path)
    (repo / "content" / "events" / "new_event.yaml").write_text(EVENT.replace("id: truce", "id: new_event"), encoding="utf-8")
    entries, human, _ = rq.queue(repo)
    new = [e for e in entries if e.row == "new_event"]
    assert [(e.file, e.line, e.value, e.checked_by) for e in new] == [
        ("content/events/new_event.yaml", 5, "sensitivity-reviewed", "not committed yet")]
    assert human == 1


def test_a_whitespace_only_human_edit_does_not_hide_an_agent_set_status(tmp_path):
    """Independent re-review of #611 (B): a person re-spacing or re-indenting a line is not a review of it, so blame ignores
    whitespace (-w) and follows moved lines (-M)."""
    repo = make_repo(tmp_path)
    gov = repo / "content" / "governments.yaml"
    gov.write_text(gov.read_text(encoding="utf-8").replace("    status: fact-checked\n", "    status:   fact-checked   \n"), encoding="utf-8")
    git(repo, "commit", "-q", "-am", "content: tidy spacing (#12)")
    entries, human, _ = rq.queue(repo)
    g1 = [e for e in entries if e.row == "g1" and e.kind == "status"]
    assert len(g1) == 1 and g1[0].checked_by.startswith("a commit with an AI co-author trailer (PR #10")
    assert human == 1


def test_a_moved_tracked_file_is_a_clear_error(tmp_path, capsys):
    """A plain mv of a tracked file, not yet committed, left the listing pointing at a missing file (FileNotFoundError)."""
    repo = make_repo(tmp_path)
    (repo / "content" / "offices.yaml").rename(repo / "content" / "offices_moved.yaml")
    assert rq.run(repo, write=False) == 2
    err = capsys.readouterr().err
    assert "content/offices.yaml" in err and "missing from the working tree" in err


def test_a_row_a_person_signed_off_leaves_the_queue_and_is_counted(tmp_path):
    """#375 ("Yes, humans only"): a whole reviewed_by / reviewed_on beside the status takes the row out of the queue, even
    when an agent-co-written commit last wrote the status line; a malformed sign-off does not (fail closed)."""
    repo = make_repo(tmp_path)
    gov = repo / "content" / "governments.yaml"
    gov.write_text(gov.read_text(encoding="utf-8").replace("    status: fact-checked\n",
                                                           "    status: fact-checked\n    reviewed_by: eyalgolan\n    reviewed_on: 2026-10-15\n"),
                   encoding="utf-8")
    git(repo, "commit", "-q", "-am", "content: sign off g1 (#13)")
    entries, human, signed = rq.queue(repo)
    assert [e.row for e in entries if e.kind == "status"] == []
    assert (human, signed) == (1, 1)
    text = rq.page(entries, human, "0123456789abcdef", signed)
    assert "- signed off by a person (`reviewed_by` and `reviewed_on`; not listed): 1" in text

    gov.write_text(gov.read_text(encoding="utf-8").replace("reviewed_by: eyalgolan", "reviewed_by: eyal@example.org"), encoding="utf-8")
    entries, human, signed = rq.queue(repo)
    assert [e.row for e in entries if e.kind == "status"] == ["g1"] and signed == 0
