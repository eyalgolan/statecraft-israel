import re, sys
from pathlib import Path
S = Path(sys.argv[1])
pat = re.compile(r"E21LABELS (\w+) (\S+) d=(\S+) lookAt=(\(.*?\)) shown=\[(.*)\]\s*$")
def load(side):
    out, meta = {}, {}
    for line in (S / "logs" / f"{side}-lines.txt").read_text(encoding="utf-8", errors="replace").splitlines():
        m = pat.search(line)
        if m:
            items = [x.strip() for x in m.group(5).split(" ; ") if x.strip()]
            out[(m.group(1), m.group(2))] = (set(items), m.group(3), m.group(4))
        elif "E21LABELS" in line and " meta " in line:
            meta[line.split("E21LABELS ")[1].split()[0]] = line.split("E21LABELS ", 1)[1].strip()
    return out, meta
b, bm = load("before"); a, am = load("after")
lines = ["# PR #2490: map names shown on the 15 May 1948 turn, before (main 1957078d3) vs after (H6' tree 7294073ac = main + #2490 at 15e146332)",
         "# Key: S<i> = game settlement pill (settlements_1948.yaml order), L<i> = 1948 locality pill, M<i> = map location pill.",
         "# The labels also keep clear of unit tokens, which are the same on both trees, so the difference is the Dakota loop's keep-clear box.", ""]
for side, m in (("before", bm), ("after", am)):
    for lang in sorted(m): lines.append(f"{side} meta: {m[lang]}")
lines.append("")
for key in sorted(set(b) | set(a)):
    bs, bd, bl = b.get(key, (set(), "?", "?")); as_, ad, al = a.get(key, (set(), "?", "?"))
    lines.append(f"== {key[0]} {key[1]}  (before d={bd} lookAt={bl}; after d={ad} lookAt={al})  shown before {len(bs)}, after {len(as_)}")
    lines.append("   hidden after, shown before: " + (" ; ".join(sorted(bs - as_)) or "(none)"))
    lines.append("   shown after, hidden before: " + (" ; ".join(sorted(as_ - bs)) or "(none)"))
(S / "out").mkdir(exist_ok=True)
(S / "out" / "labels-diff.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
print("\n".join(lines))
