#!/usr/bin/env python3
"""Check a Statecraft video script written in the shot-table template.

Usage: python check_script.py path/to/script.md [--wpm 150] [--max-words 18] [--max-gap 5]

Errors (exit code 1): a dash in the text, a quotation with no Q id, a line with a fact and no source id,
an empty row (no voice-over and no on-screen text).
Warnings: generic phrasing, vague phrasing that assumes the viewer already knows the situation,
a definite reference at first mention ("the lake", "the camps") that a cold viewer cannot place,
a voice-over line with no concrete detail (no date, number, place or name), long spoken sentences,
a voice-over that does not fit its time window, a silent gap between rows, a sound-off hook with no
on-screen text, more than one question, a call to action that may not exist yet.
"""
import argparse
import re
import sys

GENERIC = [
    r"in a world", r"pivotal", r"unprecedented", r"destiny", r"forever changed", r"against all odds",
    r"hang(s|ing)? in the balance", r"like never before", r"\bepic\b", r"untold story", r"\bjourney\b",
    r"tapestry", r"rapidly evolving", r"it is important to note", r"game-?changer", r"buckle up",
    r"dive into", r"little did", r"the fate of a nation", r"the world watched", r"changed the course of history",
    r"a nation (was )?born", r"hope and courage", r"courage, sacrifice",
]
# Phrases that sound like information and carry none, or that assume the viewer already knows the story.
VAGUE = [
    r"faced many (problems|challenges)", r"one of the (most|biggest|greatest)", r"was everywhere",
    r"everywhere else", r"became a basis (for|of)", r"a basis for", r"countless", r"a lot of",
    r"so much", r"many (problems|challenges|difficulties)", r"in many ways", r"the rest is history",
    r"and so it (began|went)", r"changed everything", r"nothing would be the same",
]
# Definite references that a viewer with no background cannot place without being told which one.
AMBIGUOUS = [
    "the lake", "the camps", "the sea", "the north", "the south", "the war", "the state", "the army",
    "the river", "the city", "the border", "the capital", "the leader", "the government", "the people",
    "the enemy", "the land",
]
MONTHS = "January February March April May June July August September October November December".split()
DASHES = ("\u2014", "\u2013")
# Hebrew gershayim (U+05F4) marks acronyms and is not a quotation delimiter.
QUOTES = ('"', "\u201c", "\u201d")
CTA_UNSAFE = re.compile(r"coming soon|wish-?list|pre-?order|available now|out now", re.I)


def parse_table(text):
    rows = []
    for no, line in enumerate(text.splitlines(), 1):
        if not line.lstrip().startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 6 or not re.fullmatch(r"\d+", cells[0]):
            continue
        rows.append({"line": no, "n": cells[0], "time": cells[1], "pic": cells[2], "screen": cells[3], "vo": cells[4], "src": cells[5]})
    return rows


def to_seconds(s):
    s = s.strip()
    m = re.fullmatch(r"(\d+):(\d+(?:\.\d+)?)", s)
    if m:
        return int(m.group(1)) * 60 + float(m.group(2))
    return float(s)


def span(t):
    m = re.fullmatch(r"\s*([\d:.]+)\s*(?:-|->|to)\s*([\d:.]+)\s*", t)
    if not m:
        return None
    try:
        return to_seconds(m.group(1)), to_seconds(m.group(2))
    except ValueError:
        return None


def has_concrete(s):
    if re.search(r"\d", s):
        return True
    if any(m in s for m in MONTHS):
        return True
    words = re.findall(r"[A-Za-z][A-Za-z'\-]*", s)
    return any(i > 0 and w[0].isupper() and w not in ("I", "You", "Why") for i, w in enumerate(words))


def has_fact(s):
    return bool(s) and has_concrete(s)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("path")
    ap.add_argument("--wpm", type=float, default=150.0)
    ap.add_argument("--max-words", type=int, default=18)
    ap.add_argument("--max-gap", type=float, default=5.0)
    a = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    text = open(a.path, encoding="utf-8").read()
    lang = "en"
    m = re.search(r"^Language:\s*(\w+)", text, re.M)
    if m:
        lang = m.group(1).lower()
    errors, warns = [], []

    for no, line in enumerate(text.splitlines(), 1):
        if any(d in line for d in DASHES):
            errors.append(f"line {no}: a dash (em or en). Use a comma, a colon or a new sentence.")

    rows = parse_table(text)
    if not rows:
        errors.append("no shot table rows found (expected | # | Time | Picture | On-screen text | Voice-over | Sources |)")
    questions = 0
    seen_ambiguous = set()
    prev_end = None
    for r in rows:
        where = f"row {r['n']} (line {r['line']})"
        vo, screen = r["vo"].strip(), r["screen"].strip()
        spoken_and_screen = " ".join([screen, vo])
        sp = span(r["time"])
        if sp and prev_end is not None and sp[0] - prev_end > a.max_gap:
            warns.append(f"{where}: {sp[0] - prev_end:g}s with no row before it (gap over {a.max_gap:g}s). Say what happens there or close it.")
        if sp:
            prev_end = sp[1]
        if vo in ("", "¶") and screen in ("", "¶"):
            errors.append(f"{where}: empty row (no voice-over and no on-screen text).")
        if lang == "en":
            low = spoken_and_screen.lower()
            for pat in GENERIC:
                if re.search(pat, low):
                    warns.append(f"{where}: generic phrasing matches '{pat}'.")
            for pat in VAGUE:
                if re.search(pat, low):
                    warns.append(f"{where}: vague phrasing matches '{pat}'. Say what, how much, where, when.")
            for term in AMBIGUOUS:
                cased = term[0].upper() + term[1:]
                if term not in seen_ambiguous and (
                    re.search(r"\b" + re.escape(term) + r"\b", spoken_and_screen)
                    or re.search(r"\b" + re.escape(cased) + r"\b", spoken_and_screen)
                ):
                    seen_ambiguous.add(term)
                    warns.append(f"{where}: '{term}' at first mention. Does a viewer with no background know which one?")
            if vo and not has_concrete(vo):
                warns.append(f"{where}: the voice-over has no date, number, place or name. What does a cold viewer learn from it?")
            for sent in re.split(r"(?<=[.!?])\s+", vo):
                n = len(sent.split())
                if n > a.max_words:
                    warns.append(f"{where}: spoken sentence of {n} words (limit {a.max_words}): {sent[:60]}...")
            questions += vo.count("?") + screen.count("?")
            nvo = len(vo.split())
            if sp and nvo:
                dur = sp[1] - sp[0]
                cap = dur * a.wpm / 60.0
                if dur > 0 and nvo > cap:
                    warns.append(f"{where}: {nvo} spoken words in {dur:g}s, room for about {cap:.0f} at {a.wpm:g} wpm.")
            if CTA_UNSAFE.search(spoken_and_screen):
                warns.append(f"{where}: call to action may refer to something that does not exist yet. Check.")
        has_quote = any(q in spoken_and_screen for q in QUOTES)
        if has_quote and not re.search(r"\bQ\d+\b", r["src"]):
            errors.append(f"{where}: a quotation with no Q id in Sources.")
        if (has_fact(vo) or has_fact(screen)) and not r["src"].strip():
            errors.append(f"{where}: a line with a date, number or name and no source id.")
        for tok in [t.strip() for t in r["src"].split(",") if t.strip()]:
            if not re.fullmatch(r"(F\d+|Q\d+|game:.+)", tok):
                warns.append(f"{where}: source '{tok}' is not F#, Q# or game:...")

    if rows and lang == "en":
        first = rows[0]
        if first["vo"].strip() and not first["screen"].strip():
            warns.append("row 1: the hook has voice-over and no on-screen text, so it fails with the sound off.")
        if questions > 1:
            warns.append(f"{questions} questions in the script. Keep one, and only if it is the player's real decision.")

    for e in errors:
        print("ERROR", e)
    for w in warns:
        print("WARN ", w)
    print(f"{len(rows)} rows, {len(errors)} errors, {len(warns)} warnings")
    sys.exit(1 if errors else 0)


if __name__ == "__main__":
    main()
