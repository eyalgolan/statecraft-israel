"""The region files' runtime forms (content/map/{subdistrict_hexes_1948,armistice_hexes_1949,control_hexes_1948_49}.tsv,
issue #321) are the bakes of the shipped YAML.

The player reads the runtime files at boot instead of parsing the YAML, and refuses one (reading its YAML, with a
warning: slower, never wrong) when its version or its check of the YAML does not match, or when it is cut short or
malformed (RegionsRuntime.Read). CI runs no Unity, so this checks here:
- each committed file is a fresh bake of its YAML (tools/regions_bake.py --check). The region YAML is generated data
  (tools/borders_bake.py, tools/control_bake.py) and may change again: the failure names the stale file and the command
  that re-bakes it;
- the bake's VERSION is RegionsRuntime.Version;
- each file ends with its body_check over its data rows, and a digit cut off changes it;
- a fingerprint of the C# that reads the runtime files (RegionsRuntime, and each class's FromRuntime, ParseRuntime and
  ReadRuntimeOrYaml; comments and whitespace left out), so a change to the reader fails here until someone checks the
  classes still give the YAML's objects (Unity's RegionsRuntimeTests compares them field by field), bumps the version if
  the layout changed, re-bakes and re-pins. The bake checks the files' data; it cannot check the C# that reads them.
"""
import hashlib
import re
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import regions_bake as bake  # noqa: E402

MAP_SCRIPTS = ROOT / "unity/Assets/Scripts/Map"
REBAKE = "run uv run tools/regions_bake.py and commit the .tsv"

# The fingerprint of the readers (code_fingerprint). Re-pin only after RegionsRuntimeTests passes in Unity, with
# RegionsRuntime.Version and regions_bake.VERSION bumped together if the runtime layout changed.
PINNED_CODE = "688bfe4efcf6459b5b5de21812c92c1b1e5a906204e0145ac531d55b5f3d35d2"


def member(text: str, signature: str) -> str:
    """The text of the member starting at signature, through its closing brace, or its ';' for an expression body."""
    i = text.index(signature)
    depth = 0
    for j in range(i, len(text)):
        c = text[j]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0 and text[j + 1:j + 2] != ";":
                return text[i:j + 1]
            if depth == 0:
                return text[i:j + 2]
        elif c == ";" and depth == 0:
            return text[i:j + 1]
    raise AssertionError(f"{signature}: no end")


def code_fingerprint() -> str:
    parts = [(MAP_SCRIPTS / "RegionsRuntime.cs").read_text()]
    for name, cls in (("Subdistricts1948.cs", "Subdistricts1948"), ("Armistice1949.cs", "Armistice1949"), ("Control1948.cs", "Control1948")):
        text = (MAP_SCRIPTS / name).read_text()
        parts += [
            member(text, "static FileDoc FromRuntime("),
            member(text, f"public static {cls} ParseRuntime("),
            member(text, f"public static {cls} ReadRuntimeOrYaml("),
        ]
    parts.append(member((MAP_SCRIPTS / "Routes/RouteRibbonFile.cs").read_text(), "public static int Check("))
    code = "\n".join(re.sub(r"\s+", " ", re.sub(r"//[^\n]*", "", p)).strip() for p in parts)
    return hashlib.sha256(code.encode()).hexdigest()


@pytest.mark.parametrize("name", bake.FILES)
def test_each_runtime_file_is_the_bake_of_its_yaml(name):
    runtime = bake.MAP / f"{name}.tsv"
    assert runtime.exists(), f"content/map/{name}.tsv is missing: {REBAKE}"
    fresh = bake.bake(name, (bake.MAP / f"{name}.yaml").read_bytes())
    assert runtime.read_text(encoding="utf-8") == fresh, (
        f"content/map/{name}.tsv is stale: content/map/{name}.yaml changed after it was baked. {REBAKE}. "
        "(The game still works with a stale file, reading the YAML with a warning, but boot is slower.)")


def test_the_bake_and_the_reader_share_a_version():
    m = re.search(r"\bVersion\s*=\s*(\d+)\b", (MAP_SCRIPTS / "RegionsRuntime.cs").read_text())
    assert m and int(m.group(1)) == bake.VERSION, "RegionsRuntime.Version and regions_bake.VERSION differ"


@pytest.mark.parametrize("name", bake.FILES)
def test_each_file_ends_with_its_body_check(name):
    lines = (bake.MAP / f"{name}.tsv").read_text(encoding="utf-8").rstrip("\n").split("\n")
    kind, key, value = lines[-1].split("\t")
    assert (kind, key) == ("meta", "body_check")
    rows = [line for line in lines if line and not line.startswith("#") and not line.startswith("meta\t")]
    body = lambda rs: bake.fnv1a("".join(r + "\n" for r in rs).encode("utf-8"))
    assert int(value) == body(rows)
    last = rows[-1]
    digit = max(i for i, c in enumerate(last) if c.isdigit())
    assert body(rows[:-1] + [last[:digit] + last[digit + 1:]]) != int(value), "a digit cut off changes body_check"


def test_the_readers_are_the_ones_the_runtime_files_were_checked_against():
    assert code_fingerprint() == PINNED_CODE, (
        f"the C# that reads the region runtime files changed (fingerprint {code_fingerprint()}): run RegionsRuntimeTests in "
        "Unity, bump the version on both sides if the layout changed, re-bake, then re-pin PINNED_CODE")


def test_the_yaml_check_is_the_ribbon_bakes():
    """The runtime files' yaml_check is the same FNV-1a the other bakes use for their source text."""
    from test_route_ribbon_bake import fnv1a
    for name in bake.FILES:
        data = (bake.MAP / f"{name}.yaml").read_bytes()
        assert bake.fnv1a(data) == fnv1a(data)


def _rows(tsv: str):
    return [line for line in tsv.split("\n") if line and not line.startswith("#") and not line.startswith("meta\t")]


# Review of #601, S1: PyYAML's YAML 1.1 resolver re-types plain scalars that YamlDotNet reads into the classes' string
# fields as their text. Each case is one edit to a shipped file; the bake must keep the scalar's own text, as the YAML path
# does (0x0F then stays "0x0F", which Control1948 refuses on both paths; a datetime keeps its space).
YAML_11_CASES = [
    ("subdistrict_hexes_1948", 'name: "Acre Sub-District"', "name: yes", "name=s:yes"),
    ("subdistrict_hexes_1948", 'district: "Galilee District", style: primary }\n  - { id: 2', 'district: Off, style: primary }\n  - { id: 2', "district=s:Off"),
    ("subdistrict_hexes_1948", "key: acre,", "key: 1_0,", "key=s:1_0"),
    ("armistice_hexes_1949", "signed: 1949-03-23, shown_from: 1949-03-23", "signed: 1949-03-23 00:00:00, shown_from: 1949-03-23", "signed=s:1949-03-23 00:00:00"),
    ("control_hexes_1948_49", '  - [15, 1, "cIIIIIIIIIIIIIII"]', '  - [0x0F, 1, "cIIIIIIIIIIIIIII"]', "0x0F 1 cIIIIIIIIIIIIIII"),
]


@pytest.mark.parametrize("name,old,new,expected", YAML_11_CASES)
def test_the_bake_keeps_plain_scalars_as_text(name, old, new, expected):
    text = (bake.MAP / f"{name}.yaml").read_text(encoding="utf-8")
    assert old in text, f"{name}: the case's anchor is gone"
    tsv = bake.bake(name, text.replace(old, new, 1).encode("utf-8"))
    assert expected in "\n".join(_rows(tsv))


def test_the_bake_keeps_a_sexagesimal_looking_note_as_text():
    text = (bake.MAP / "armistice_hexes_1949.yaml").read_text(encoding="utf-8")
    m = re.search(r'note: ("[^"]*"|[^,}\n]*)', text)
    assert m
    tsv = bake.bake("armistice_hexes_1949", (text[:m.start()] + "note: 1:30" + text[m.end():]).encode("utf-8"))
    assert "note=s:1:30" in "\n".join(_rows(tsv))
