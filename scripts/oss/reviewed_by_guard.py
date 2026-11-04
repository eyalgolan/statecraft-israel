# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Only a person signs a row off, or an AI-assisted contributor who discloses the AI help, and an agent may not edit a row
a person has signed off. For every commit in a pull request's range this fails when:

1. a commit that an AI agent co-wrote adds, changes or removes a `reviewed_by` or `reviewed_on` line in a content/ YAML
   file (owner, 15 October 2026, #375: "Yes, humans only"), unless the pull request discloses the AI help (see below);
2. a commit that an AI agent co-wrote changes any line, the id included, of a row that carries a person's whole sign-off
   in that commit's parent (so on the base, or signed earlier in the same pull request) (owner, 15 October 2026, on #629: "Block it in CI"), unless a later commit of the same pull
   request with no AI trailer changes that row's own reviewed_by / reviewed_on lines (signs it again or removes its
   sign-off; a merge commit releases nothing);
3. a person's added `reviewed_on` is after the day of their own commit.

A signed row's Hebrew counts as part of the row wherever it lives (owner, 25 October 2026, on #1618: "Yes, carry them
over"): inline (title_he, text_he, name_he_review ...) or as the row's entry in translations/he/events.yaml or
content.yaml (docs/translations.md). An AI-co-authored commit that only removes Hebrew field lines from a signed row and
leaves the row's Hebrew, read from both places, exactly as its parent had it, has moved the text byte for byte, and the
sign-off covers it. Any other change to that Hebrew, in either place (a moved value that differs, a lost review flag, a
dropped option, an edit after the move), is an edit of the signed row under (2). The translation files are read whole
with the game's YAML 1.2 core schema, not diffed; one that does not parse fails closed.

"Co-wrote" is the trailer test of tools/review_queue.py (AI_MARK: "Co-Authored-By: Claude / Codex / Copilot / GPT ...",
"Generated with [Claude Code]" or "Generated with Codex"), read from each commit's full message. For (1) any non-comment
line that names either key counts, quoted or not, block or flow style. A row's span is its mapping's lines
(tools/review_queue.py, signed_rows), plus the spans of nodes it reaches through an alias or a merge key; a block inserted
right after a block row belongs to it when its first line that is neither blank nor a comment is at the row's keys'
indent or deeper; a comment inside a signed row counts as part of it. Known limit: a row stored under a mapping key (not
in a list) has its id on the key line, outside its span, so renaming that key passes; no content file has such a status
row today. A commit is diffed against its parent with renames followed; a merge
commit by its combined diff (--cc), so lines it takes unchanged from one parent (a person's sign-off merged in from main)
are not its own.

The diff is read with every option that could change its shape pinned (no colour, no external diff or textconv, text even
where .gitattributes says binary, a/ and b/ prefixes, unquoted paths, no relative paths) and a pathspec for content/*.yaml
and *.yml, so a path with a space or a non-Latin letter, a -diff attribute or a local git setting cannot hide a line.

It fails closed: a base or head that does not resolve, a range git cannot list, a shallow clone, a commit whose message,
diff or files cannot be read, a content YAML file that does not parse, and a path git had to quote are failures, never
passes.

Disclosure (owner, 4 November 2026, asked whether an AI-assisted contributor's source check counts as the reviewed_by
sign-off: "Yes, it counts"): with --pr-event, the guard reads the pull request's description from the event file GitHub
writes (GITHUB_EVENT_PATH), never from a shell argument. The pull request discloses the AI help when its "AI assistance"
section (the template's heading, up to the next heading, HTML comments removed) does not start with no / none / not /
n/a / nothing and names a check (one of a short list of verbs: check, verified, compared, opened, read and the like).
CI checks only that much; the reviewer reads what was checked and how. Then an AI-co-authored commit may add or change reviewed_by /
reviewed_on lines, but never remove one: every key it drops must come back in the same commit. (3) applies to it, and the
sign-off lines it adds release a row under (2); its removals never release a row, and an edit to a signed row that the
commit does not sign again still fails. Without --pr-event (local runs, check.sh), with an event that has no pull
request, or with a missing, empty, negative or check-less section, nothing is disclosed: the 15 October rule applies
unchanged. An event file that cannot be read or parsed is a failure.

Usage: uv run scripts/oss/reviewed_by_guard.py --base <base-sha> [--head HEAD] [--repo .] [--pr-event <event.json>]"""
from __future__ import annotations

import argparse
import datetime
import json
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools"))
import yaml  # noqa: E402
from review_queue import AI_MARK, signed_rows  # noqa: E402  (one definition of the trailer test and of a signed row)
import yaml12  # noqa: E402  (translations/ is read as the game reads it: the YAML 1.2 core schema)

EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"
PATHSPEC = [":(glob)content/**/*.yaml", ":(glob)content/**/*.yml"]
CONFIG = ["-c", "core.quotePath=false", "-c", "color.ui=never", "-c", "diff.noprefix=false", "-c", "diff.mnemonicPrefix=false",
          "-c", "diff.relative=false", "-c", "diff.external=", "-c", "core.attributesFile=/dev/null"]
DIFF = ["--no-color", "--no-ext-diff", "--no-textconv", "--text", "--src-prefix=a/", "--dst-prefix=b/", "--unified=0"]
NAMES = re.compile(r"reviewed_(?:by|on)")
ON_VALUE = re.compile(r"""["']?reviewed_on["']?\s*:\s*["']?([^,}\s#"']*)""")
HUNK = re.compile(r"^(@+) (.*?) @+")
HEADING = re.compile(r"^ {0,3}#{1,6}(?:\s+(.*?))?\s*#*\s*$")
NEGATIVE = {"no", "none", "not", "n/a", "na", "nothing"}
FIRST_WORD = re.compile(r"[^\s.,;:!?*_`~>#()\[\]-]+")
CHECK = re.compile(r"\b(?:check|checks|checked|checking|verify|verified|compared|opened|read)\b", re.I)
LIST_MARK = re.compile(r"^\s*(?:(?:\d+[.)]|[-*+])\s+|\[[ xX]\]\s*)")
FENCE = re.compile(r"^ {0,3}(```|~~~)")
KEYS = ("reviewed_by", "reviewed_on")
HE_EVENTS, HE_CONTENT = "translations/he/events.yaml", "translations/he/content.yaml"
HE_FIELD = re.compile(r"^( *)[\"']?[a-z_]+_he(?:_f)?(?:_review)?[\"']?\s*:")
HE_KEY = re.compile(r"(.+)_he(_f)?")


class GuardError(Exception):
    """Something the guard cannot determine: a failure, never a pass."""


def _git(args: list[str], cwd: Path) -> str:
    return subprocess.run(["git", *CONFIG, *args], cwd=cwd, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout


def _resolve(repo: Path, ref: str) -> str:
    try:
        return _git(["rev-parse", "--verify", "--quiet", ref + "^{commit}"], repo).strip()
    except subprocess.CalledProcessError as e:
        raise GuardError(f"cannot resolve {ref!r} to a commit") from e


@dataclass
class Hunk:
    old: list[tuple[int, int]]          # per parent: (start, count) as in the hunk header
    new: tuple[int, int]
    lines: list[tuple[str, str]] = field(default_factory=list)   # (the parents' columns, the line's text)


def hunks(diff: str) -> list[Hunk]:
    """The hunks of a unified (one parent) or combined (--cc) diff of one file, with the -U0 ranges."""
    out: list[Hunk] = []
    cols = 1
    for line in diff.splitlines():
        m = HUNK.match(line)
        if m:
            cols = len(m.group(1)) - 1
            ranges = m.group(2).split()
            def rng(r):
                a, _, b = r[1:].partition(",")
                return int(a), int(b) if b else 1
            out.append(Hunk([rng(r) for r in ranges[:-1]], rng(ranges[-1])))
            continue
        if out and line[:cols].strip(" +-") == "" and len(line) >= cols and any(c in "+-" for c in line[:cols]):
            out[-1].lines.append((line[:cols], line[cols:]))
    return out


def ai_assistance(body: str) -> str:
    """The text of the description's first "AI assistance" section, HTML comments removed ("" when there is none). An
    unclosed comment hides the rest of the section; a "#" line inside a ``` or ~~~ code fence is not a heading."""
    lines, found, out, fence = (body or "").splitlines(), False, [], False
    for line in lines:
        if FENCE.match(line):
            fence = not fence     # a "#" line inside a code fence is not a heading
        m = None if fence else HEADING.match(line)
        if m:
            if found:
                break
            found = (m.group(1) or "").strip().lower() == "ai assistance"
            continue
        if found:
            out.append(line)
    text = re.sub(r"<!--.*?(?:-->|\Z)", "", "\n".join(out), flags=re.S)
    return text.strip()


def disclosed(body: str) -> bool:
    """True when the "AI assistance" section exists, is not a "no" and names a check. Its first word, after list markers
    ("1." "1)" "-" "*" "+" "[x]" "[ ]") and markdown punctuation are stripped, is not no / none / not / n/a / nothing, and it
    contains one of check / checks / checked / checking / verify / verified / compared / opened / read as a whole word.
    An empty or template-only section, "None used.", "- [x] None, I checked it" or "Claude Code drafted it." (no check
    named) is not a disclosure. This is all CI can see: what was checked, and how, is for the reviewer to read."""
    text = ai_assistance(body)
    while (stripped := LIST_MARK.sub("", text, count=1)) != text:
        text = stripped
    first = FIRST_WORD.search(text)
    if first is None or first.group(0).lower() in NEGATIVE:
        return False
    return CHECK.search(text) is not None


def _key_count(lines: list[tuple[str, str]], sign: str, key: str) -> int:
    """How many times the key is named on the diff lines with this sign (comments excluded)."""
    return sum(len(re.findall(rf"[\"']?{key}[\"']?\s*:", t)) for cols, t in lines if sign in cols and _named(t))


def event_body(path: str) -> str:
    """The pull request description in a GitHub event file; "" for an event with no pull request or no description."""
    try:
        event = json.loads(Path(path).read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        raise GuardError(f"cannot read the event file {path}") from e
    pr = event.get("pull_request") if isinstance(event, dict) else None
    body = pr.get("body") if isinstance(pr, dict) else None
    return body if isinstance(body, str) else ""


def _named(text: str) -> bool:
    return not text.lstrip().startswith("#") and NAMES.search(text) is not None


def _touches(row, start: int, count: int, inserted: list[str]) -> bool:
    """True when a change at old lines [start, start+count) (count 0: an insertion after line start) is inside the row, or
    inside a node the row reaches through an alias or a merge key."""
    for first, last in [(row.first, row.last), *row.extra]:
        if count > 0 and start <= last and start + count - 1 >= first:
            return True
        if count == 0 and first <= start < last:
            return True
    if count == 0 and start == row.last and not row.flow:
        # A line inserted right after the row belongs to it when the first line that says anything (not blank, not a
        # comment) is indented as the row's keys or deeper and does not start a new item.
        text = next((t for t in inserted if t.strip() and not t.lstrip().startswith("#")), None)
        if text is not None:
            indent = len(text) - len(text.lstrip(" "))
            return indent >= row.key_column and not text.lstrip().startswith("- ")
    return False


def _show(repo: Path, rev: str, path: str) -> str | None:
    try:
        return _git(["show", f"{rev}:{path}"], repo)
    except subprocess.CalledProcessError:
        return None


def _signed(repo: Path, rev: str, path: str, must_exist: bool):
    """The signed rows of a file at a revision. A file the diff says was there but git cannot show is a failure."""
    text = _show(repo, rev, path)
    if text is None:
        if must_exist:
            raise GuardError(f"cannot read {path} at {rev[:10]}")
        return []
    try:
        return signed_rows(text)
    except yaml.YAMLError as e:
        raise GuardError(f"cannot parse {path} at {rev[:10]}") from e


@dataclass
class Change:
    path: str                           # the file after the commit (or before, for a deletion)
    old_paths: list[str]                # per parent: the file before the commit ("" when it did not exist)
    hunks: list[Hunk]


def changes(repo: Path, sha: str, parents: list[str]) -> list[Change]:
    """The content YAML files a commit changes, with their hunks against each parent."""
    if len(parents) <= 1:
        parent = parents[0] if parents else EMPTY_TREE
        names = _git(["diff", "--name-status", "-z", "-M", "--no-ext-diff", parent, sha, "--", *PATHSPEC], repo).split("\0")
        out, i = [], 0
        while i < len(names) - 1:
            status = names[i]
            if status[:1] in "RC":
                old, new, i = names[i + 1], names[i + 2], i + 3
            else:
                old = new = names[i + 1]
                i += 2
                if status[:1] == "A":
                    old = ""
            spec = [f":(literal){p}" for p in {old, new} if p]
            diff = _git(["diff", *DIFF, "-M", parent, sha, "--", *spec], repo)
            out.append(Change(new, [old], hunks(diff)))
        return out
    diff = _git(["diff-tree", *DIFF, "--cc", sha, "--", *PATHSPEC], repo)
    out = []
    for block in re.split(r"(?m)^diff --cc ", diff)[1:]:
        path = block.split("\n", 1)[0]
        if path.startswith('"'):
            raise GuardError(f"cannot read the quoted path {path} in merge {sha[:10]}")
        out.append(Change(path, [path] * len(parents), hunks(block)))
    return out


def check(repo: Path, base: str, head: str, ai_disclosed: bool = False) -> list[str]:
    """The problems in base..head, one line each; raises GuardError for anything it cannot determine. ai_disclosed: the
    pull request discloses the AI help (owner, 4 November 2026, "Yes, it counts")."""
    repo = Path(repo)
    try:
        if _git(["rev-parse", "--is-shallow-repository"], repo).strip() != "false":
            raise GuardError("cannot judge a shallow clone (fetch the full history: fetch-depth: 0)")
    except subprocess.CalledProcessError as e:
        raise GuardError("cannot read the repository") from e
    b, h = _resolve(repo, base), _resolve(repo, head)
    try:
        shas = _git(["rev-list", "--reverse", "--topo-order", f"{b}..{h}"], repo).split()
    except subprocess.CalledProcessError as e:
        raise GuardError(f"cannot list the commits in {base}..{head}") from e
    problems: list[str] = []
    edited: dict[tuple[str, str], str] = {}   # (file, row) an agent edited while signed -> the commit, until a person re-signs
    for sha in shas:
        try:
            message = _git(["log", "-1", "--format=%B", sha], repo)
            day = datetime.date.fromisoformat(_git(["log", "-1", "--format=%as", sha], repo).strip())
            parents = _git(["rev-list", "--parents", "-n", "1", sha], repo).split()[1:]
            files = changes(repo, sha, parents)
        except (subprocess.CalledProcessError, ValueError) as e:
            raise GuardError(f"cannot read commit {sha[:10]}") from e
        agent = AI_MARK.search(message) is not None
        moved: set[tuple[str, str, str, str]] = set()   # (file, row, parent, the file at the parent): its Hebrew moved or changed
        if agent:
            moved |= _translated_rows(repo, sha, parents)
        for ch in files:
            named = [(cols, text) for hk in ch.hunks for cols, text in hk.lines if _named(text)]
            if agent:
                if named and ai_disclosed:
                    # Disclosed (owner, 4 November 2026, "Yes, it counts"): an AI-assisted check may add or change a sign-off,
                    # never remove one. Every key a commit drops must come back in the same commit.
                    lost = [k for k in KEYS if _key_count(named, "-", k) > _key_count(named, "+", k)]
                    if lost:
                        problems.append(f"{sha[:10]}: a commit with an AI co-author trailer removes a {' / '.join(lost)} line in "
                                        f"{ch.path}. Disclosed AI assistance lets a check count as a sign-off (owner, 4 November "
                                        f"2026, \"Yes, it counts\"); it does not let an AI-co-authored commit remove one. A "
                                        f"person removes a sign-off in a commit of their own.")
                if named and not ai_disclosed:
                    problems.append(f"{sha[:10]}: a commit with an AI co-author trailer changes a reviewed_by / reviewed_on line "
                                    f"in {ch.path}, and the pull request does not disclose AI assistance. Only a person signs a "
                                    f"row off (#375), or an AI-assisted contributor who discloses it (owner, 4 November 2026, "
                                    f"\"Yes, it counts\"): say in the \"AI assistance\" section what was checked and how, then "
                                    f"push again so CI reads the new description; or remove those lines from this commit.")
                for p, (parent, old) in enumerate(zip(parents or [EMPTY_TREE], ch.old_paths)):
                    if not old or parent == EMPTY_TREE:
                        continue
                    for row in _signed(repo, parent, old, must_exist=len(parents) == 1):
                        for hk in ch.hunks:
                            if p >= len(hk.old):
                                continue
                            inserted = [t for cols, t in hk.lines if "+" in cols]
                            if _touches(row, hk.old[p][0], hk.old[p][1], inserted):
                                if len(parents) == 1 and _moves_hebrew_out(hk):
                                    moved.add((ch.path, row.label, parent, old))   # judged by its Hebrew, below
                                    continue
                                edited.setdefault((ch.path, row.label), sha)
                                break
                if not ai_disclosed:
                    continue
            # A person's commit, or a disclosed AI-assisted one: the sign-off's day, and re-signing (or unsigning) a row an
            # agent edited.
            for cols, text in named:
                if "+" in cols:
                    m = ON_VALUE.search(text)
                    if m:
                        try:
                            signed_on = datetime.date.fromisoformat(m.group(1))
                        except ValueError:
                            problems.append(f"{sha[:10]}: {ch.path}: reviewed_on {m.group(1)!r} is not a day (YYYY-MM-DD)")
                            continue
                        if signed_on > day:
                            problems.append(f"{sha[:10]}: {ch.path}: reviewed_on {m.group(1)} is after the commit's own day, {day}")
            if named and len(parents) <= 1:
                # Release only the rows whose own lines gain or lose a sign-off line in this commit: a person re-signing (or
                # unsigning) row b does not release row a, even in the same hunk. A merge never releases (fail closed).
                added, removed = set(), set()
                for hk in ch.hunks:
                    old_no, new_no = hk.old[0][0] if hk.old else 0, hk.new[0]
                    for cols, text in hk.lines:
                        if "-" in cols:
                            if _named(text):
                                removed.add(old_no)
                            old_no += 1
                        elif "+" in cols:
                            if _named(text):
                                added.add(new_no)
                            new_no += 1
                released = {row.label for row in _signed(repo, sha, ch.path, must_exist=False)
                            if any(row.first <= n <= row.last for n in added)}
                old = ch.old_paths[0] if ch.old_paths else ""
                if old and parents and not agent:   # an AI-co-authored commit's removal never releases a row
                    released |= {row.label for row in _signed(repo, parents[0], old, must_exist=True)
                                 if any(row.first <= n <= row.last for n in removed)}
                for label in released:
                    edited.pop((ch.path, label), None)
        for path, label, parent, old in sorted(moved):
            # Owner, 25 October 2026 (#1618): a sign-off covers its row's Hebrew moved byte for byte into translations/he/;
            # any other change to that Hebrew, inline or there, is an edit of the signed row.
            before = _hebrew(repo, parent, old, label)
            if before is not None and before != _hebrew(repo, sha, path, label):
                edited.setdefault((path, label), sha)
    for (path, label), sha in edited.items():
        problems.append(f"{sha[:10]}: a commit with an AI co-author trailer edits {path} row {label!r}, which a person signed "
                        f"off (owner, 15 October 2026: \"Block it in CI\"). A person must read it again and sign it again "
                        f"(new reviewed_by / reviewed_on), or remove its sign-off, in a commit of their own.")
    return problems


def _moves_hebrew_out(hk: Hunk) -> bool:
    """True when a one-parent hunk only removes Hebrew text fields (title_he, text_he, name_he_review ...: a key with a
    text value on its line, or a quoted or block scalar with its lines): the Hebrew may have moved into translations/he/,
    which _hebrew judges. A key holding a list or a mapping (biography_sources.name_he, a source), an anchor or an alias
    is not Hebrew text, and neither is _he_sources or the title's form and date window: removing one is an edit. A
    flow-style row ({ ... } on one line) cannot lose a field without changing its line, so moving its Hebrew is an edit
    too, and a person signs it again."""
    indent = more = None
    for cols, text in hk.lines:
        if "+" in cols:
            return False
        m = HE_FIELD.match(text)
        if m and not text.lstrip().startswith("#"):
            value = text[m.end():].strip()
            if not value or value[0] in "[{&*-":
                return False
            indent, more = len(m.group(1)), value[0] in ">|\"'"
        elif indent is None or not more or (text.strip() and len(text) - len(text.lstrip(" ")) <= indent):
            return False
    return indent is not None


def _records(node, ids: tuple = ()):
    """Every mapping under node with its key path, as tools/translate.py keys content: a list item by its id, else by its
    index."""
    if isinstance(node, yaml.MappingNode):
        yield ids, node
        for k, v in node.value:
            yield from _records(v, ids + (_scalar(k),))
    elif isinstance(node, yaml.SequenceNode):
        for i, item in enumerate(node.value):
            ident = next((v.value for k, v in item.value if _scalar(k) == "id" and isinstance(v, yaml.ScalarNode)), None) \
                if isinstance(item, yaml.MappingNode) else None
            yield from _records(item, ids + (ident if ident is not None else str(i),))


def _scalar(node) -> str:
    return node.value if isinstance(node, yaml.ScalarNode) else ""


def _translations(repo: Path, rev: str, rel: str) -> dict:
    text = _show(repo, rev, rel)
    if text is None:
        return {}
    try:
        data = yaml12.load(text) or {}
    except yaml.YAMLError as e:
        raise GuardError(f"cannot parse {rel} at {rev[:10]}") from e
    if not isinstance(data, dict):
        raise GuardError(f"{rel} at {rev[:10]} is not a mapping")
    return data


def _hebrew(repo: Path, rev: str, path: str, label: str) -> dict[str, set] | None:
    """The Hebrew a signed row carries at a revision, inline and in translations/he/, by the translation key: the value
    (and its review flag) from each place that has one. Equal on both sides of a commit when it moved the Hebrew byte for
    byte; None when the row is not signed there."""
    text = _show(repo, rev, path)
    row = next((r for r in (_signed(repo, rev, path, must_exist=False) if text is not None else []) if r.label == label), None)
    if row is None:
        return None
    out: dict[str, set] = {}

    def put(key: str, value) -> None:   # a value that is not text never equals the inline Hebrew: it fails closed
        out.setdefault(key, set()).add(value if isinstance(value, str) else ("not text", repr(value)))

    rel = path.removeprefix("content/")
    root = yaml.compose(text)
    if rel.startswith("events/"):
        top = {_scalar(k): v for k, v in root.value} if isinstance(root, yaml.MappingNode) else {}
        for field, key in (("title_he", "title"), ("text_he", "text"), ("text_he_review", "review")):
            if isinstance(top.get(field), yaml.ScalarNode):
                put(key, top[field].value)
        for o in top["options"].value if isinstance(top.get("options"), yaml.SequenceNode) else []:
            kv = {_scalar(k): v for k, v in o.value} if isinstance(o, yaml.MappingNode) else {}
            if isinstance(kv.get("id"), yaml.ScalarNode) and isinstance(kv.get("text_he"), yaml.ScalarNode):
                put("option:" + kv["id"].value, kv["text_he"].value)
        entry = _translations(repo, rev, HE_EVENTS).get(label)
        if isinstance(entry, dict):
            for key in ("title", "text", "review"):
                if key in entry:
                    put(key, entry[key])
            options = entry.get("options")
            for oid, value in (options if isinstance(options, dict) else {"(options)": options} if options else {}).items():
                put(f"option:{oid}", value)
        return out
    inside = {".".join(ids): node for ids, node in _records(root) if row.first <= node.start_mark.line + 1 <= row.last}
    for where, node in inside.items():
        for k, v in node.value:
            m = HE_KEY.fullmatch(_scalar(k).removesuffix("_review"))
            if m and isinstance(v, yaml.ScalarNode):
                field = f"{rel}#{where}.{m.group(1)}{'_f' if m.group(2) else ''}"
                put(field + (":review" if _scalar(k).endswith("_review") else ""), v.value)
    for key, entry in _translations(repo, rev, HE_CONTENT).items():
        file, _, rest = str(key).partition("#")
        if file == rel and rest.rpartition(".")[0] in inside and isinstance(entry, dict):
            put(key, entry.get("text"))
            if "review" in entry:
                put(key + ":review", entry["review"])
    return out


def _translated_rows(repo: Path, sha: str, parents: list[str]) -> set[tuple[str, str, str, str]]:
    """The signed rows whose entry in translations/he/events.yaml or content.yaml this commit changes against a parent
    (a merge: against every parent), as (file, row, parent, file): the content file is named by the key."""
    out = set()
    for rel in (HE_EVENTS, HE_CONTENT):
        now = _translations(repo, sha, rel)
        befores = [(p, _translations(repo, p, rel)) for p in parents]
        changed = {k for k in set(now).union(*(b for _, b in befores)) if all(now.get(k) != b.get(k) for _, b in befores)}
        for parent, before in befores:
            for key in sorted(changed, key=str):
                if rel == HE_EVENTS:
                    path = _event_file(repo, parent, str(key))
                    label = str(key)
                else:
                    file, _, rest = str(key).partition("#")
                    path, label = "content/" + file, _row_of(repo, parent, "content/" + file, rest.rpartition(".")[0])
                if path and label:
                    out.add((path, label, parent, path))
    return out


def _event_file(repo: Path, rev: str, eid: str) -> str | None:
    """The content/events/ file whose id is eid at a revision (by name first, then by reading each)."""
    guess = f"content/events/{eid}.yaml"
    names = [n for n in _git(["ls-tree", "-r", "--name-only", rev, "--", "content/events/"], repo).splitlines()
             if n.endswith((".yaml", ".yml"))]
    for name in sorted(names, key=lambda n: n != guess):
        text = _show(repo, rev, name)
        try:
            root = yaml.compose(text) if text else None
        except yaml.YAMLError as e:
            raise GuardError(f"cannot parse {name} at {rev[:10]}") from e
        if isinstance(root, yaml.MappingNode) and any(_scalar(k) == "id" and _scalar(v) == eid for k, v in root.value):
            return name
    return None


def _row_of(repo: Path, rev: str, path: str, where: str) -> str | None:
    """The label of the signed row holding the record at this key path at a revision, if one does."""
    text = _show(repo, rev, path)
    if text is None:
        return None
    try:
        record = next((n for ids, n in _records(yaml.compose(text)) if ".".join(ids) == where), None)
    except yaml.YAMLError as e:
        raise GuardError(f"cannot parse {path} at {rev[:10]}") from e
    if record is None:
        return None
    line = record.start_mark.line + 1
    rows = [r for r in _signed(repo, rev, path, must_exist=True) if r.first <= line <= r.last]
    return min(rows, key=lambda r: r.last - r.first).label if rows else None


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--base", required=True)
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--repo", default=".")
    ap.add_argument("--pr-event", help="GitHub event file (GITHUB_EVENT_PATH) to read the PR's AI assistance section from")
    a = ap.parse_args(argv)
    try:
        ai_disclosed = a.pr_event is not None and disclosed(event_body(a.pr_event))
        print(f"reviewed_by guard: AI assistance {'disclosed' if ai_disclosed else 'not disclosed'} in the pull request.")
        problems = check(Path(a.repo), a.base, a.head, ai_disclosed)
    except GuardError as e:
        print(f"reviewed_by guard: {e}. Failing closed.")
        return 1
    for p in problems:
        print(p)
    if problems:
        return 1
    print("reviewed_by guard: no AI-co-authored commit signs a row off without disclosure or edits a signed-off row.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
