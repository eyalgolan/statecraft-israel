# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""A local page for writing translations: every user-facing string (the events, the UI strings of unity/Assets/UI/ and
every content field that has a Hebrew form: names, office titles, unit sectors ...), the source language on the left and
the translation on the right.

Usage: uv run tools/translate.py [--port 8765] [--no-open]   (serves http://127.0.0.1:PORT; Ctrl+C stops it)

Hebrew is written where the game reads it: the events' title_he / text_he / options' text_he (#1064) and
unity/Assets/UI/strings.he.yaml (#12), changing only the edited lines so comments and layout stay. Hebrew content names
are shown but not edited here: they carry their own sources. Any other language is written to
translations/<lang>/{events,ui,content}.yaml, which the game does not read yet. Every language keeps the Hebrew rules
(docs/WRITING_EVENTS.md; the sim's EventRules): an event is all or none, placeholders match the English with no stray
'{' or '}', one line, no direction marks, and a Hebrew event field has Hebrew letters. Of tools/tests/test_hebrew_ui.py
it applies only the general rules (every key has a value, the same format tokens, no direction marks), not its checks of
particular keys' wording, so run the tests after saving. A translation is marked agent-checked until a native speaker
has read it, and goes back to agent-checked when its text changes (#564). Every write is read back first, and nothing
is written when any edited string is refused. It never touches status,
reviewed_by or reviewed_on, and it does not commit.
"""
from __future__ import annotations

import argparse
import copy
import json
import os
import re
import sys
import webbrowser
from collections import Counter
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

sys.path.insert(0, str(Path(__file__).resolve().parent))
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
EVENTS = ROOT / "content/events"
UI_EN = ROOT / "unity/Assets/UI/strings.en.yaml"
UI_HE = ROOT / "unity/Assets/UI/strings.he.yaml"
CONTENT = ROOT / "content"
TRANSLATIONS = ROOT / "translations"
HE_KEY = re.compile(r"(.+)_he(_f)?$")       # name_he, title_he_f, sector_he ...: a content field's Hebrew form
LANG = re.compile(r"[a-z]{2,3}(-[A-Za-z0-9]{2,8})*")
RTL = {"ar", "ckb", "dv", "fa", "he", "ps", "ur", "yi"}
# Kept in English and Hebrew only, for now: the advisors and How to play (owner, 24 October 2026, on #1604).
HE_EN_ONLY = ("ui:advisor.", "ui:how_to_play.")
TOKEN = re.compile(r"\{[^{}]+\}")
OFFICE = re.compile(r"\{[a-z0-9_]+\}")     # an event's {office_id} placeholder (Offices.Placeholder in the sim)
HEBREW = re.compile("[\u05d0-\u05ea]")
MAX_BODY = 4_000_000
BIDI = re.compile("[\u202a-\u202e\u2066-\u2069]")
REVIEW = "agent-checked"


class Refused(ValueError):
    """An edit the loader or the tests would refuse; nothing is written."""


def _check(name: str, value: str, english: str | None, hebrew: bool = True, event: bool = False) -> list[str]:
    """One value's rules; raises Refused, returns the warnings. event: an event field, read by the sim's EventRules."""
    if "\n" in value or "\r" in value:
        raise Refused(f"{name}: keep it on one line")
    if BIDI.search(value):
        raise Refused(f"{name}: no direction marks (U+202A-U+202E, U+2066-U+2069); keep logical order")
    if english is not None and Counter(TOKEN.findall(value)) != Counter(TOKEN.findall(english)):
        raise Refused(f"{name}: placeholders differ from the English ({', '.join(TOKEN.findall(english)) or 'none'})")
    if event and re.search("[{}]", OFFICE.sub("", value)):
        raise Refused(f"{name}: stray '{{' or '}}' outside an {{office_id}} placeholder")
    if event and hebrew and not HEBREW.search(value):
        raise Refused(f"{name}: must be Hebrew, in Hebrew letters")
    if hebrew and re.search("[A-Za-z]", TOKEN.sub("", value)):
        return [f"{name}: Latin letters; the Hebrew fields are written in Hebrew letters"]
    return []


def _read(path: Path) -> str:
    return path.read_bytes().decode("utf-8")


_PARSED: dict[Path, tuple[tuple[int, int], object]] = {}


def _parse(path: Path):
    """The file's YAML, parsed again only when the file changes (the page re-checks on every keystroke). Callers do not
    change what it returns."""
    stat = path.stat()
    stamp = (stat.st_mtime_ns, stat.st_size)
    if path not in _PARSED or _PARSED[path][0] != stamp:
        _PARSED[path] = (stamp, yaml12.load(_read(path)))
    return _PARSED[path][1]


def _write(path: Path, text: str) -> None:
    """Write beside the file, then replace it, so a crash never leaves half a file."""
    tmp = path.with_name(f".{path.name}.tmp")
    tmp.write_bytes(text.encode("utf-8"))
    os.replace(tmp, path)


def _indent(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def _span(lines: list[str], i: int) -> int:
    """The end (exclusive) of the scalar that starts on line i: its continuation lines are indented deeper."""
    j = i + 1
    while j < len(lines) and lines[j].strip() and not lines[j].lstrip().startswith("#") \
            and _indent(lines[j]) > _indent(lines[i]):
        j += 1
    return j


def _find(lines: list[str], key: str, indent: int, lo: int = 0, hi: int | None = None) -> int | None:
    pattern = re.compile(" " * indent + re.escape(key) + r":(\s|$)")
    return next((i for i in range(lo, len(lines) if hi is None else hi) if pattern.match(lines[i])), None)


def _scalar(value: str) -> str:
    """Plain when YAML reads it back as the same string and has no '"' (gershayim are quoted in content), else
    single-quoted."""
    try:
        if '"' not in value and yaml12.load(f"k: {value}") == {"k": value}:
            return value
    except Exception:  # noqa: BLE001 (any parse error means: quote it)
        pass
    return "'" + value.replace("'", "''") + "'"


def _options(lines: list[str]) -> dict[str, tuple[int, int, int]]:
    """Each option id -> (its '- id:' line, the end of its lines, the indent of its keys)."""
    start = next((i for i, line in enumerate(lines) if re.match(r"options:\s*$", line)), None)
    if start is None:
        return {}
    end = start + 1
    while end < len(lines) and not re.match(r"[A-Za-z_]", lines[end]):
        end += 1
    items = [(i, m) for i in range(start + 1, end)
             if (m := re.match(r"( *)-\s+id:\s*[\"']?([A-Za-z0-9_]+)", lines[i]))]
    if not items:
        return {}
    dash = min(len(m.group(1)) for _, m in items)
    items = [(i, m) for i, m in items if len(m.group(1)) == dash]
    return {m.group(2): (i, items[n + 1][0] if n + 1 < len(items) else end, dash + 2) for n, (i, m) in enumerate(items)}


def _put(lines: list[str], key: str, value: str, indent: int, after: int, lo: int = 0, hi: int | None = None) -> None:
    """Replace key's line(s) in lines[lo:hi], remove them when value is '', or add the key after the scalar on `after`."""
    i = _find(lines, key, indent, lo, hi)
    line = [f"{' ' * indent}{key}: {_scalar(value)}"] if value else []
    if i is not None:
        if (yaml12.load("\n".join(x[_indent(lines[i]):] for x in lines[i:_span(lines, i)])) or {}).get(key) != value:
            lines[i:_span(lines, i)] = line
    elif value:
        lines[_span(lines, after):_span(lines, after)] = line


def set_event(text: str, changes: dict) -> tuple[str, list[str]]:
    """changes: title_he, text_he, options {id: text_he}, native_read. Returns the new file text and its warnings."""
    data = yaml12.load(text)
    options = {o["id"]: o for o in data.get("options") or []}
    if set(changes["options"]) - set(options):
        raise Refused(f"unknown option {sorted(set(changes['options']) - set(options))}")
    fields = [("title", "title_he", changes["title_he"].strip(), data.get("title", "")),
              ("text", "text_he", changes["text_he"].strip(), data.get("text", ""))]
    fields += [(f"option {oid}", oid, changes["options"].get(oid, "").strip(), o.get("text", ""))
               for oid, o in options.items()]
    warnings = check_unit([(name, v, en) for name, _, v, en in fields], hebrew=True)
    filled = [v for _, _, v, _ in fields if v]

    expected = copy.deepcopy(data)
    lines = text.split("\n")
    title_he, text_he = fields[0][2], fields[1][2]
    review = REVIEW if filled and not changes.get("native_read") else ""
    for key, value in (("title_he", title_he), ("text_he", text_he), ("text_he_review", review)):
        prev = {"title_he": "text", "text_he": "title_he", "text_he_review": "text_he"}[key]
        after = _find(lines, prev, 0)
        _put(lines, key, value, 0, _find(lines, "text", 0) if after is None else after)
        expected.pop(key, None)
        if value:
            expected[key] = value
    for oid, o in zip(options, expected.get("options") or []):
        value = changes["options"].get(oid, "").strip()
        dash, end, indent = _options(lines)[oid]
        after = _find(lines, "text", indent, dash, end)
        _put(lines, "text_he", value, indent, dash if after is None else after, dash, end)
        o.pop("text_he", None)
        if value:
            o["text_he"] = value
    out = "\n".join(lines)
    if yaml12.load(out) != expected:
        raise Refused("the edit would change other fields too; nothing was written (edit this file by hand)")
    return out, warnings


def set_ui(text: str, order: list[str], key: str, value: str, english: str | None = None) -> str:
    """Set one Hebrew UI string, adding a missing key after the nearest earlier English key. Spaces at either end are
    kept: some strings are separators (" · ")."""
    if not value.strip():
        raise Refused(f"{key}: empty; every Hebrew UI string needs a value (test_hebrew_ui.py)")
    _check(key, value, english)
    lines = text.split("\n")
    line = f"{key}: {json.dumps(value, ensure_ascii=False)}"
    i = _find(lines, key, 0)
    if i is not None:
        if (yaml12.load(text) or {}).get(key) != value:
            lines[i:_span(lines, i)] = [line]
    else:
        earlier = [j for k in order[:order.index(key)] if (j := _find(lines, k, 0)) is not None]
        at = _span(lines, max(earlier)) if earlier else len(lines) - (lines[-1] == "")
        lines.insert(at, line)
    out = "\n".join(lines)
    if yaml12.load(out) != {**(yaml12.load(text) or {}), key: value}:
        raise Refused("the edit would change other strings too; nothing was written")
    return out


def check_unit(fields: list[tuple[str, str, str]], hebrew: bool) -> list[str]:
    """fields: (name, value, English) of one event. All or none, then each value's own checks; returns the warnings."""
    filled = [f for f in fields if f[1]]
    if filled and len(filled) != len(fields):
        raise Refused("all or none: the title, the body and every option's text (docs/WRITING_EVENTS.md)")
    return [w for name, value, english in filled for w in _check(name, value, english, hebrew, event=True)]


def _events() -> dict[str, tuple[Path, dict]]:
    found = {}
    for path in sorted(EVENTS.glob("*.yaml")):
        data = _parse(path)
        found[data["id"]] = (path, data)
    return dict(sorted(found.items(), key=lambda kv: (kv[1][1].get("from", ""), kv[0])))


def _content() -> list[dict]:
    """Every content field with a Hebrew form (name_he, title_he_f, sector_he ...): a row for each record of that shape
    with the English beside it, whether or not that record has its Hebrew yet."""
    out = []
    for path in sorted(CONTENT.glob("*.yaml")) + sorted((CONTENT / "map").glob("*.yaml")):
        rel, nodes = path.relative_to(CONTENT).as_posix(), []

        def walk(node, ids: list[str], shape: tuple) -> None:
            if isinstance(node, dict):
                nodes.append((shape, ids, node))
                for k, v in node.items():
                    walk(v, ids + [str(k)], shape + (str(k),))
            elif isinstance(node, list):
                for i, x in enumerate(node):
                    ident = x["id"] if isinstance(x, dict) and isinstance(x.get("id"), str) else str(i)
                    walk(x, ids + [ident], shape + ("*",))

        walk(_parse(path), [], ())
        fields = dict.fromkeys((shape, k) for shape, _, n in nodes for k, v in n.items()
                               if HE_KEY.match(k) and isinstance(v, str))
        for shape, ids, node in nodes:
            for s, he_key in fields:
                m = HE_KEY.match(he_key)
                if s != shape or not isinstance(node.get(m.group(1)), str):
                    continue
                field, where = m.group(1) + ("_f" if m.group(2) else ""), ".".join(ids)
                he = node.get(he_key)
                out.append({"key": f"content:{rel}#{where}.{field}", "label": f"{where} · {field}",
                            "group": f"content:{rel}", "group_label": rel, "unit": None, "en": node[m.group(1)],
                            "he": he if isinstance(he, str) else "", "he_review": bool(node.get(he_key + "_review"))})
    return out


def catalog() -> list[dict]:
    """Every user-facing string, by its key: the events, the UI strings, then the content fields."""
    rows = []
    for eid, (_, d) in _events().items():
        unit = f"event:{eid}"
        group = {"group": unit, "group_label": d.get("title", eid), "unit": unit}
        rows.append({"key": f"{unit}:title", "label": "Title", "en": d.get("title", ""), **group})
        rows.append({"key": f"{unit}:text", "label": "Body", "en": d.get("text", ""), **group})
        rows += [{"key": f"{unit}:option:{o['id']}", "label": f"Option · {o['id']}", "en": o.get("text", ""), **group}
                 for o in d.get("options") or []]
    for k, v in _parse(UI_EN).items():
        prefix = k.split(".")[0]
        rows.append({"key": f"ui:{k}", "label": k, "en": v, "group": f"ui:{prefix}", "group_label": prefix, "unit": None})
    rows += [{k: v for k, v in r.items() if k not in ("he", "he_review")} for r in _content()]
    return rows


def lang_code(code: str, target: bool = False) -> str:
    """A language code as a folder name may hold it (fr, ar, pt-BR); never a path. A target is not English."""
    if not isinstance(code, str) or not LANG.fullmatch(code) or (target and code == "en"):
        raise Refused(f"language code {code!r}: lowercase ISO 639 (fr, ar, pt-BR){', not en' if target else ''}")
    return code


def translatable(key: str, lang: str) -> bool:
    """Whether a catalog key is translated into lang: Hebrew takes every string, other languages all but HE_EN_ONLY."""
    return lang in ("en", "he") or not key.startswith(HE_EN_ONLY)


def _lang_file(lang: str, name: str) -> Path:
    return TRANSLATIONS / lang_code(lang) / f"{name}.yaml"


def _lang_data(lang: str, name: str) -> dict:
    path = _lang_file(lang, name)
    return (yaml12.load(_read(path)) or {}) if path.exists() else {}


def languages() -> list[str]:
    found = sorted(p.name for p in TRANSLATIONS.iterdir() if p.is_dir() and LANG.fullmatch(p.name)) \
        if TRANSLATIONS.exists() else []
    return ["en", "he"] + [code for code in found if code not in ("en", "he")]


def load(lang: str) -> tuple[dict[str, str], set[str]]:
    """A language's texts by catalog key, and the keys or event units still marked agent-checked."""
    texts, flagged = {}, set()
    if lang == "en":
        return {r["key"]: r["en"] for r in catalog()}, flagged
    if lang == "he":
        for eid, (_, d) in _events().items():
            unit = f"event:{eid}"
            if d.get("title_he"):
                texts[f"{unit}:title"], texts[f"{unit}:text"] = d["title_he"], d.get("text_he", "")
            texts.update({f"{unit}:option:{o['id']}": o["text_he"] for o in d.get("options") or [] if o.get("text_he")})
            if d.get("text_he_review"):
                flagged.add(unit)
        texts.update({f"ui:{k}": v for k, v in (_parse(UI_HE) or {}).items()})
        for r in _content():
            if r["he"]:
                texts[r["key"]] = r["he"]
            if r["he_review"]:
                flagged.add(r["key"])
        return texts, flagged
    for eid, e in _lang_data(lang, "events").items():
        unit = f"event:{eid}"
        texts.update({f"{unit}:{k}": e[k] for k in ("title", "text") if e.get(k)})
        texts.update({f"{unit}:option:{oid}": t for oid, t in (e.get("options") or {}).items() if t})
        if e.get("review"):
            flagged.add(unit)
    texts.update({f"ui:{k}": v for k, v in _lang_data(lang, "ui").items()})
    for k, e in _lang_data(lang, "content").items():
        texts[f"content:{k}"] = e.get("text", "")
        if e.get("review"):
            flagged.add(f"content:{k}")
    return texts, flagged


def _yaml_key(key: str) -> str:
    return key if re.fullmatch(r"[A-Za-z0-9_.\-]+", key) else json.dumps(key, ensure_ascii=False)


def _dump(header: str, data: dict) -> str:
    """Block mappings down to double-quoted strings; anything else (a list of sources) as one JSON flow value."""
    lines = [f"# {line}".rstrip() for line in header.split("\n")]

    def emit(mapping: dict, indent: int) -> None:
        for k, v in mapping.items():
            if isinstance(v, dict):
                lines.append(f"{' ' * indent}{_yaml_key(str(k))}:")
                emit(v, indent + 2)
            else:
                lines.append(f"{' ' * indent}{_yaml_key(str(k))}: {json.dumps(v, ensure_ascii=False)}")

    emit(data, 0)
    out = "\n".join(lines) + "\n"
    if (yaml12.load(out) or {}) != data:
        raise Refused("the translation file would not read back as written; nothing was written")
    return out


TITLES = {"events": "Events", "ui": "UI strings", "content": "Content fields"}
HEADER = """{name} ({lang}) for tools/translate.py. The English in content/ and unity/Assets/UI/ is the source of record.
The game does not read this file yet. Same rules as the Hebrew (docs/WRITING_EVENTS.md): an event is all or none,
placeholders match the English, one line each; review: agent-checked until a native speaker has read it (#564).
The tool rewrites this file: comments other than this header are not kept."""


def save(lang: str, edits: dict[str, str], native: dict[str, bool], dry: bool = False) -> dict:
    """Check every edited string, and write them all only when none is refused. native: event unit or content key ->
    whether a native speaker has read it (a missing one keeps its state; a new translation starts agent-checked)."""
    lang_code(lang, target=True)
    if not isinstance(edits, dict) or not isinstance(native, dict) or \
            not all(isinstance(v, str) for v in edits.values()) or not all(isinstance(v, bool) for v in native.values()):
        raise Refused("edits are {key: text} and native is {key: true or false}")
    rows = {r["key"]: r for r in catalog()}
    kept = sorted(k for k in edits if not translatable(k, lang))
    if kept:
        raise Refused(f"{kept[0]}: the advisors and How to play stay English or Hebrew only, for now (owner, #1604)")
    if set(edits) - set(rows):
        raise Refused(f"unknown strings {sorted(set(edits) - set(rows))[:3]}")
    reviewed = {r["unit"] for r in rows.values() if r["unit"]} | {k for k in rows if k.startswith("content:")}
    if set(native) - reviewed:
        raise Refused(f"unknown review keys {sorted(set(native) - reviewed)[:3]}")
    old, flagged = load(lang)
    new = {**old, **{k: v if k.startswith("ui:") else v.strip() for k, v in edits.items()}}
    hebrew = lang == "he"

    def changed(key: str) -> bool:
        keys = [r["key"] for r in rows.values() if r["unit"] == key] if key.startswith("event:") else [key]
        return any(new.get(k, "") != old.get(k, "") for k in keys)

    def read(key: str) -> bool:
        """Whether an event unit or content key counts as read by a native speaker after this save: a ticked box, or
        a text that was read and has not changed (a native read covers only the text that was read)."""
        if key in native:
            return native[key]
        return bool(old.get(key + ":title" if key.startswith("event:") else key)) and key not in flagged \
            and not changed(key)

    errors, warnings = {}, {}
    units = {rows[k]["unit"] for k in edits if rows[k]["unit"]} | {k for k in native if k.startswith("event:")}
    singles = [k for k in edits if not rows[k]["unit"]] + [k for k in native if k.startswith("content:")]
    for unit in units:
        fields = [(r["label"], new.get(r["key"], ""), r["en"]) for r in rows.values() if r["unit"] == unit]
        try:
            warnings[unit] = check_unit(fields, hebrew)
        except Refused as e:
            errors[unit] = str(e)
    for key in dict.fromkeys(singles):
        value = new.get(key, "")
        try:
            if hebrew and key.startswith("content:") and (value != old.get(key, "") or key in native):
                raise Refused("Hebrew content is edited by hand in content/, with its sources (docs/WRITING_EVENTS.md)")
            if hebrew and key.startswith("ui:") and not value.strip():
                raise Refused("empty; every Hebrew UI string needs a value (test_hebrew_ui.py)")
            warnings[key] = _check(rows[key]["label"], value, rows[key]["en"], hebrew) if value.strip() else []
        except Refused as e:
            errors[key] = str(e)

    writes: dict[Path, str] = {}
    if not errors and hebrew:
        events = _events()
        for unit in units:
            path, d = events[unit[6:]]
            changes = {"title_he": new.get(f"{unit}:title", ""), "text_he": new.get(f"{unit}:text", ""),
                       "native_read": read(unit),
                       "options": {o["id"]: new.get(f"{unit}:option:{o['id']}", "") for o in d.get("options") or []}}
            try:
                writes[path] = set_event(_read(path), changes)[0]
            except Refused as e:
                errors[unit] = str(e)
        ui_keys = [k[3:] for k in singles if k.startswith("ui:")]
        if ui_keys:
            text, order = _read(UI_HE), [r["key"][3:] for r in rows.values() if r["key"].startswith("ui:")]
            for k in ui_keys:
                text = set_ui(text, order, k, new[f"ui:{k}"], rows[f"ui:{k}"]["en"])
            writes[UI_HE] = text
    elif not errors:
        events, ui, content = _lang_data(lang, "events"), _lang_data(lang, "ui"), _lang_data(lang, "content")
        for unit in units:
            eid, keys = unit[6:], [r["key"] for r in rows.values() if r["unit"] == unit]
            entry = {k: v for k, v in events.get(eid, {}).items() if k not in ("title", "text", "options", "review")}
            if new.get(keys[0]):
                entry = {"title": new[keys[0]], "text": new[keys[1]],
                         "options": {k.split(":")[-1]: new[k] for k in keys[2:]}, **entry}
                if not read(unit):
                    entry["review"] = REVIEW
                events[eid] = entry
            else:
                events.pop(eid, None)
        for key in dict.fromkeys(singles):
            if key.startswith("ui:"):
                if new.get(key, "").strip():
                    ui[key[3:]] = new[key]
                else:
                    ui.pop(key[3:], None)
            else:
                k = key[len("content:"):]
                entry = {n: v for n, v in content.get(k, {}).items() if n not in ("text", "review")}
                if new.get(key):
                    content[k] = {"text": new[key], **({} if read(key) else {"review": REVIEW}), **entry}
                else:
                    content.pop(k, None)
        rank = {k: i for i, k in enumerate(rows)}
        for name, data, order in (("events", events, lambda eid: rank.get(f"event:{eid}:title", len(rank))),
                                  ("ui", ui, lambda k: rank.get(f"ui:{k}", len(rank))),
                                  ("content", content, lambda k: rank.get(f"content:{k}", len(rank)))):
            if data or _lang_file(lang, name).exists():
                ordered = {k: data[k] for k in sorted(data, key=lambda k: (order(k), k))}
                writes[_lang_file(lang, name)] = _dump(HEADER.format(name=TITLES[name], lang=lang), ordered)
    if not errors and not dry:
        for path, text in writes.items():
            if path.exists() and _read(path) == text:
                continue
            path.parent.mkdir(parents=True, exist_ok=True)
            _write(path, text)
    return {"errors": errors, "warnings": {k: v for k, v in warnings.items() if v}}


def catalog_for(src: str, lang: str) -> dict:
    """The rows with the source language's text on the left and the target's on the right."""
    lang_code(src)
    lang_code(lang)
    rows = [r for r in catalog() if translatable(r["key"], lang)]
    src_texts, _ = load(src)
    texts, flagged = load(lang) if lang != "en" else ({}, set())
    readonly = lang == "he"
    for r in rows:
        r["src"] = src_texts.get(r["key"], "")
        r["tgt"] = texts.get(r["key"], "")
        r["readonly"] = readonly and r["key"].startswith("content:")
    return {"rows": rows, "flagged": sorted(flagged)}


class Handler(BaseHTTPRequestHandler):
    def _send(self, code: int, body: bytes, kind: str = "application/json") -> None:
        self.send_response(code)
        self.send_header("Content-Type", kind + "; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _json(self, code: int, value) -> None:
        self._send(code, json.dumps(value, ensure_ascii=False).encode("utf-8"))

    def _hosts(self) -> tuple[str, str]:
        port = self.server.server_address[1]
        return f"127.0.0.1:{port}", f"localhost:{port}"

    def _local(self) -> bool:
        # Refuse a request for another name (DNS rebinding: another site's page that resolves to 127.0.0.1).
        return self.headers.get("Host") in self._hosts()

    def _route(self, routes: dict, arg) -> None:
        if not self._local():
            return self._json(403, {"error": "local only"})
        handler = routes.get(urlparse(self.path).path)
        if handler is None:
            return self._json(404, {"error": "not found"})
        try:
            self._json(200, handler(arg))
        except Refused as e:
            self._json(400, {"error": str(e)})
        except Exception as e:  # noqa: BLE001 (shown in the page, the files are untouched)
            self._json(500, {"error": f"{type(e).__name__}: {e}"})

    def do_GET(self) -> None:  # noqa: N802
        if urlparse(self.path).path == "/" and self._local():
            return self._send(200, PAGE.encode("utf-8"), "text/html")
        query = {k: v[0] for k, v in parse_qs(urlparse(self.path).query).items()}
        self._route({"/api/langs": lambda q: {"langs": languages(), "rtl": sorted(RTL)},
                     "/api/catalog": lambda q: catalog_for(q.get("src", "en"), q.get("lang"))}, query)

    def do_POST(self) -> None:  # noqa: N802
        # Checked before the body is read: the Host, then the Origin (only this page may post; a browser always sends
        # it on a POST, so a missing one is refused too), the content type and the size.
        if not self._local() or self.headers.get("Origin") not in tuple(f"http://{h}" for h in self._hosts()):
            return self._json(403, {"error": "local only"})
        if self.headers.get("Content-Type") != "application/json":
            return self._json(415, {"error": "JSON only"})
        try:
            length = int(self.headers.get("Content-Length", ""))
        except ValueError:
            return self._json(411, {"error": "Content-Length required"})
        if not 0 <= length <= MAX_BODY:
            return self._json(413, {"error": f"at most {MAX_BODY} bytes"})
        try:
            body = json.loads(self.rfile.read(length) or b"{}")
        except (ValueError, UnicodeDecodeError):
            return self._json(400, {"error": "bad JSON"})
        if not isinstance(body, dict):
            return self._json(400, {"error": "bad JSON: expected an object"})
        self._route({"/api/save": lambda b: save(b.get("lang"), b.get("edits", {}), b.get("native", {}),
                                                 bool(b.get("dry")))}, body)

    def log_message(self, *args) -> None:
        pass


PAGE = r"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Translations</title>
<style>
:root { --bg:#fafaf8; --panel:#fff; --ink:#1d1d1b; --muted:#6b6b66; --line:#e2e1dc; --accent:#1f5fa8;
  --bad:#b3261e; --warn:#8a5a00; --ok:#2e7d32; --chip:#eef2f8; }
@media (prefers-color-scheme: dark) { :root { --bg:#161615; --panel:#1f1f1d; --ink:#ecebe6; --muted:#9a9993;
  --line:#33332f; --accent:#7fb0ef; --bad:#f2867d; --warn:#e3b45c; --ok:#86c98a; --chip:#26303d; } }
* { box-sizing:border-box; }
body { margin:0; background:var(--bg); color:var(--ink); font:14px/1.5 system-ui, sans-serif; height:100vh; display:grid; grid-template-rows:auto 1fr; }
header { display:flex; flex-wrap:wrap; gap:10px; align-items:center; padding:10px 16px; border-bottom:1px solid var(--line); background:var(--panel); }
header label { display:flex; gap:6px; align-items:center; color:var(--muted); }
.layout { display:grid; grid-template-columns:280px 1fr; min-height:0; }
nav { border-right:1px solid var(--line); overflow:auto; background:var(--panel); padding-bottom:20px; }
nav h3 { font-size:11px; text-transform:uppercase; letter-spacing:.05em; color:var(--muted); margin:14px 12px 4px; }
nav a { display:flex; justify-content:space-between; gap:8px; padding:4px 12px; color:var(--ink); text-decoration:none; cursor:pointer; }
nav a:hover { background:var(--bg); } nav a.sel { background:var(--chip); }
nav a small { color:var(--muted); white-space:nowrap; } nav a small.done { color:var(--ok); }
main { overflow:auto; padding:0 24px 40px; }
button, select, input, textarea { font:inherit; color:var(--ink); border:1px solid var(--line); border-radius:6px; }
button { background:var(--panel); padding:5px 12px; cursor:pointer; } button.primary { background:var(--accent); color:var(--panel); border-color:var(--accent); }
button:disabled { opacity:.5; cursor:default; }
select, input { background:var(--bg); padding:5px 8px; } input[type=checkbox] { width:auto; }
#q { width:200px; }
.cols { display:grid; grid-template-columns:1fr 1fr; gap:16px; position:sticky; top:0; background:var(--bg); padding:10px 0 6px; border-bottom:1px solid var(--line); z-index:1;
  font-size:12px; font-weight:600; color:var(--muted); text-transform:uppercase; letter-spacing:.04em; }
section h2 { font-size:15px; margin:22px 0 4px; display:flex; gap:12px; align-items:baseline; flex-wrap:wrap; }
section h2 label { font-size:13px; font-weight:400; color:var(--muted); display:flex; gap:5px; align-items:center; }
.row { display:grid; grid-template-columns:1fr 1fr; gap:16px; padding:6px 0; border-bottom:1px solid var(--line); }
.key { grid-column:1/-1; font:11px ui-monospace, monospace; color:var(--muted); display:flex; gap:10px; align-items:center; margin-bottom:-4px; }
.key label { font:12px system-ui, sans-serif; display:flex; gap:4px; align-items:center; }
.src { padding:6px 8px; border:1px dashed var(--line); border-radius:6px; white-space:pre-wrap; }
.src.fallback { color:var(--muted); } .src.fallback::after { content:" (English)"; font-size:11px; }
textarea { width:100%; background:var(--panel); padding:6px 8px; resize:vertical; min-height:34px; }
textarea.dirty { border-color:var(--accent); } textarea[readonly] { background:var(--bg); color:var(--muted); }
.tok { background:var(--chip); border-radius:4px; padding:0 3px; font:12px ui-monospace, monospace; }
.chips { display:flex; flex-wrap:wrap; gap:4px; margin-top:3px; } .chips button { padding:0 6px; font-size:12px; }
.err { color:var(--bad); grid-column:1/-1; } .wrn { color:var(--warn); grid-column:1/-1; } .note { color:var(--muted); font-size:12px; }
#status { color:var(--muted); } .saved { color:var(--ok); }
@media (max-width: 800px) { .layout { grid-template-columns:1fr; } nav { display:none; } .row, .cols { grid-template-columns:1fr; } }
</style></head><body>
<header>
  <label>From <select id="src"></select></label>
  <label>To <select id="lang"></select></label>
  <input id="q" placeholder="Filter keys and text">
  <select id="show"><option value="">All strings</option><option value="todo">Not translated</option><option value="flag">Not read by a native speaker</option></select>
  <span id="progress" class="note"></span>
  <span style="flex:1"></span><span id="status"></span>
  <button class="primary" id="save" disabled>Save</button>
</header>
<div class="layout"><nav id="nav"></nav><main id="main"></main></div>
<script>
const $ = s => document.querySelector(s);
const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
const toks = s => String(s).match(/\{[^{}]+\}/g) || [];
const hl = s => esc(s).replace(/\{[^{}]+\}/g, t => `<span class="tok">${t}</span>`);
const api = async (path, body) => { const r = await fetch(path, body ? {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body)} : {});
  const j = await r.json(); if (!r.ok) throw new Error(j.error); return j; };
let langs = [], rtl = [], rows = [], byKey = {}, flagged = new Set(), edits = {}, native = {}, result = {errors:{}, warnings:{}}, group = '';
const dir = l => rtl.includes(l.split('-')[0]) ? 'rtl' : 'ltr';
const value = r => r.key in edits ? edits[r.key] : r.tgt;
const isDirty = () => Object.keys(edits).length + Object.keys(native).length > 0;
const reviewKey = r => r.unit || (r.key.startsWith('content:') ? r.key : null);
// A native read covers only the text that was read: an edited text is not read until its box is ticked again.
const isRead = k => k in native ? native[k] : !flagged.has(k) && rows.some(r => reviewKey(r) === k && r.tgt)
  && !rows.some(r => reviewKey(r) === k && r.key in edits);

async function init() {
  const l = await api('/api/langs'); langs = l.langs; rtl = l.rtl;
  const keep = (sel, v) => { sel.value = v; };
  $('#src').innerHTML = langs.map(c => `<option>${esc(c)}</option>`).join('');
  $('#lang').innerHTML = langs.filter(c => c !== 'en').map(c => `<option>${esc(c)}</option>`).join('') + '<option value="+">New language…</option>';
  keep($('#src'), localGet('src', 'en')); keep($('#lang'), localGet('lang', 'he'));
  if (!$('#src').value) $('#src').value = 'en'; if (!$('#lang').value || $('#lang').value === '+') $('#lang').value = 'he';
  await load();
}
function localGet(k, d) { try { return localStorage.getItem('translate.' + k) || d; } catch { return d; } }
function localSet(k, v) { try { localStorage.setItem('translate.' + k, v); } catch {} }

async function load() {
  const c = await api(`/api/catalog?src=${encodeURIComponent($('#src').value)}&lang=${encodeURIComponent($('#lang').value)}`);
  rows = c.rows; flagged = new Set(c.flagged); byKey = Object.fromEntries(rows.map(r => [r.key, r]));
  edits = {}; native = {}; result = {errors:{}, warnings:{}}; renderNav(); render(); updateBar();
}
function visible(r) {
  const q = $('#q').value.toLowerCase(), show = $('#show').value;
  if (group && r.group !== group) return false;
  if (show === 'todo' && value(r)) return false;
  if (show === 'flag' && !(value(r) && reviewKey(r) && !isRead(reviewKey(r)))) return false;
  return !q || (r.key + ' ' + r.en + ' ' + r.src + ' ' + value(r)).toLowerCase().includes(q);
}
function groups() {
  const g = new Map();
  for (const r of rows) { if (!g.has(r.group)) g.set(r.group, {id: r.group, label: r.group_label, unit: r.unit, rows: []}); g.get(r.group).rows.push(r); }
  return [...g.values()];
}
function renderNav() {
  const all = groups(), done = rs => rs.filter(r => value(r)).length;
  const link = (id, label, rs) => `<a data-g="${esc(id)}" class="${group === id ? 'sel' : ''}"><span>${esc(label)}</span>
    <small class="${done(rs) === rs.length ? 'done' : ''}">${done(rs)}/${rs.length}</small></a>`;
  const part = (title, prefix) => `<h3>${title}</h3>` + all.filter(g => g.id.startsWith(prefix)).map(g => link(g.id, g.label, g.rows)).join('');
  $('#nav').innerHTML = link('', 'All strings', rows) + part('Events', 'event:') + part('Interface', 'ui:') + part('Content', 'content:');
  $('#progress').textContent = `${done(rows)} of ${rows.length} translated`;
}
$('#nav').onclick = e => { const a = e.target.closest('a'); if (!a) return; group = a.dataset.g; renderNav(); render(); $('#main').scrollTop = 0; };

function render() {
  const src = $('#src').value, lang = $('#lang').value, he = lang === 'he';
  let html = `<div class="cols"><div>${esc(src)}</div><div>${esc(lang)}</div></div>`, shown = 0;
  for (const g of groups()) {
    const rs = g.rows.filter(visible); if (!rs.length) continue;
    if (shown + rs.length > 800 && shown) { html += `<p class="note">More strings below the first 800: pick a group on the left or filter.</p>`; break; }
    shown += rs.length;
    html += `<section><h2>${esc(g.label)}${g.unit ? `<label><input type="checkbox" data-native="${esc(g.unit)}" ${isRead(g.unit) ? 'checked' : ''}> read by a native speaker</label>` : ''}</h2>`;
    if (g.unit) html += msgs(g.unit);
    if (he && g.id.startsWith('content:')) html += `<p class="note">Hebrew content names are kept in content/ with their sources; edit them there by hand.</p>`;
    for (const r of rs) {
      const s = r.src || r.en, fallback = !r.src && src !== 'en', v = value(r), rk = reviewKey(r);
      const lines = Math.max(1, Math.ceil(Math.max(r.en.length, v.length) / 55));
      html += `<div class="row" data-key="${esc(r.key)}"><div class="key">${esc(r.label)}${!r.unit && rk && !r.readonly && v ?
        `<label><input type="checkbox" data-native="${esc(rk)}" ${isRead(rk) ? 'checked' : ''}> read by a native speaker</label>` : ''}</div>
        <div class="src ${fallback ? 'fallback' : ''}" dir="${dir(fallback ? 'en' : src)}">${hl(s)}</div>
        <div><textarea dir="${dir(lang)}" lang="${esc(lang)}" rows="${lines}" ${r.readonly ? 'readonly' : ''} class="${r.key in edits ? 'dirty' : ''}">${esc(v)}</textarea>
        ${toks(r.en).length && !r.readonly ? `<div class="chips">${[...new Set(toks(r.en))].map(t => `<button type="button" data-tok="${esc(t)}">${esc(t)}</button>`).join('')}</div>` : ''}</div>
        ${r.unit ? '' : msgs(r.key)}</div>`;
    }
    html += '</section>';
  }
  $('#main').innerHTML = shown ? html : html + '<p class="note">Nothing matches.</p>';
}
function msgs(k) {
  return (result.errors[k] ? `<div class="err">✕ ${esc(result.errors[k])}</div>` : '') +
    (result.warnings[k] || []).map(w => `<div class="wrn">⚠ ${esc(w)}</div>`).join('');
}
function updateBar() {
  const n = Object.keys(edits).length + Object.keys(native).length, bad = Object.keys(result.errors).length;
  $('#save').disabled = !n || bad > 0;
  $('#save').textContent = n ? `Save ${n} change${n > 1 ? 's' : ''}` : 'Save';
  if (n) $('#status').innerHTML = bad ? `<span style="color:var(--bad)">${bad} to fix</span>` : 'Unsaved';
}
let timer;
function changed() { updateBar(); clearTimeout(timer); timer = setTimeout(check, 350); }
async function check() {
  if (!isDirty()) { result = {errors:{}, warnings:{}}; return refreshMsgs(); }
  try { result = await api('/api/save', {lang: $('#lang').value, edits, native, dry: true}); }
  catch (e) { result = {errors: {'': e.message}, warnings: {}}; $('#status').innerHTML = `<span style="color:var(--bad)">${esc(e.message)}</span>`; }
  refreshMsgs();
}
function refreshMsgs() {
  document.querySelectorAll('.err, .wrn').forEach(e => e.remove());
  for (const [k, list, cls] of [...Object.entries(result.errors).map(([k, v]) => [k, [v], 'err']), ...Object.entries(result.warnings).map(([k, v]) => [k, v, 'wrn'])]) {
    const html = list.map(m => `<div class="${cls}">${cls === 'err' ? '✕' : '⚠'} ${esc(m)}</div>`).join('');
    const row = document.querySelector(`.row[data-key="${CSS.escape(k)}"]`);
    if (row) { row.insertAdjacentHTML('beforeend', html); continue; }
    const box = document.querySelector(`[data-native="${CSS.escape(k)}"]`)?.closest('h2');
    if (box) box.insertAdjacentHTML('afterend', html);
  }
  updateBar();
}
$('#main').addEventListener('input', e => {
  if (e.target.tagName !== 'TEXTAREA') return;
  const r = byKey[e.target.closest('.row').dataset.key], v = e.target.value;
  if (v === r.tgt) delete edits[r.key]; else edits[r.key] = v;
  e.target.classList.toggle('dirty', r.key in edits);
  // A tick covers the text as it was when ticked: editing it again needs a new tick.
  const rk = reviewKey(r), box = rk && document.querySelector(`[data-native="${CSS.escape(rk)}"]`);
  if (rk) delete native[rk];
  if (box) box.checked = isRead(rk);
  changed();
});
$('#main').addEventListener('change', e => {
  const k = e.target.dataset.native; if (!k) return;
  native[k] = e.target.checked; changed();
});
$('#main').addEventListener('click', e => {
  const t = e.target.dataset.tok; if (!t) return;
  const ta = e.target.closest('.row').querySelector('textarea'), s = ta.selectionStart;
  ta.value = ta.value.slice(0, s) + t + ta.value.slice(ta.selectionEnd); ta.focus(); ta.selectionStart = ta.selectionEnd = s + t.length;
  ta.dispatchEvent(new Event('input', {bubbles: true}));
});
async function save() {
  if ($('#save').disabled) return;
  try {
    result = await api('/api/save', {lang: $('#lang').value, edits, native});
    if (Object.keys(result.errors).length) return refreshMsgs();
    const lang = $('#lang').value; await init(); $('#lang').value = lang; await load();
    $('#status').innerHTML = '<span class="saved">Saved</span>';
  } catch (e) { $('#status').innerHTML = `<span style="color:var(--bad)">${esc(e.message)}</span>`; }
}
$('#save').onclick = save;
document.addEventListener('keydown', e => { if ((e.metaKey || e.ctrlKey) && e.key === 's') { e.preventDefault(); save(); } });
window.addEventListener('beforeunload', e => { if (isDirty()) e.preventDefault(); });
let last = {};
for (const id of ['src', 'lang']) {
  $('#' + id).addEventListener('focus', e => last[id] = e.target.value);
  $('#' + id).addEventListener('change', async e => {
    if (isDirty() && !confirm('Discard the unsaved translations?')) { e.target.value = last[id]; return; }
    if (e.target.value === '+') {
      const code = (prompt('Language code (ISO 639, for example fr, ar, ru, pt-BR):') || '').trim();
      if (!/^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$/.test(code) || code === 'en') { e.target.value = last[id] || 'he'; return; }
      if (!langs.includes(code)) { langs.push(code); e.target.insertAdjacentHTML('afterbegin', `<option>${esc(code)}</option>`); }
      e.target.value = code;
    }
    localSet(id, e.target.value); $('#status').textContent = ''; await load();
  });
}
$('#q').oninput = $('#show').onchange = () => render();
init();
</script></body></html>
"""


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--no-open", action="store_true", help="do not open the browser")
    args = parser.parse_args()
    server = HTTPServer(("127.0.0.1", args.port), Handler)
    url = f"http://127.0.0.1:{args.port}/"
    print(f"Hebrew translations: {url} (Ctrl+C stops it)")
    if not args.no_open:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
