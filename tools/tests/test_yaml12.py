"""tools/yaml12.py: a YAML 1.2 core-schema loader for the Python readers, so they read a plain scalar the way the game's
YamlDotNet does, not the YAML 1.1 way PyYAML's safe_load does (yes -> True, 1_0 -> 10, 1:30 -> 90, 2026-10-15 -> a
date). Refs #601."""
import math
from datetime import date

import pytest
import yaml

import yaml12


# (plain scalar, what PyYAML's safe_load gives, what the YAML 1.2 core schema gives)
HAZARDS = [
    ("yes", True, "yes"),
    ("no", False, "no"),
    ("Off", False, "Off"),
    ("on", True, "on"),
    ("y", "y", "y"),            # PyYAML leaves y/n alone (its bool resolver omits them), listed so that stays pinned
    ("1_0", 10, "1_0"),
    ("1:30", 90, "1:30"),
    ("017", 15, 17),            # 1.1 reads a leading 0 as octal; 1.2 core is decimal
    ("0o17", "0o17", 15),       # 1.2 core's octal; PyYAML does not know it
    ("2026-10-15", date(2026, 10, 15), "2026-10-15"),
    ("1e3", "1e3", 1000.0),     # 1.2 core needs no dot in a float; PyYAML does
    ("0b101", 5, "0b101"),
]


@pytest.mark.parametrize("raw, pyyaml, core", HAZARDS)
def test_core_schema_reads_the_1_1_hazards_as_1_2_does(raw, pyyaml, core):
    assert yaml.safe_load(f"k: {raw}")["k"] == pyyaml       # the hazard is real today
    got = yaml12.load(f"k: {raw}")["k"]
    assert got == core and type(got) is type(core)


@pytest.mark.parametrize("raw, value", [
    ("true", True), ("False", False), ("TRUE", True), ("null", None), ("~", None), ("", None),
    ("12", 12), ("-3", -3), ("+4", 4), ("0x0F", 15), ("1.5", 1.5), ("-0.25", -0.25), (".5", 0.5),
    ("1.0e+3", 1000.0), (".inf", math.inf), ("-.Inf", -math.inf), ("text", "text"), ("'yes'", "yes"),
    ('"1:30"', "1:30"),
])
def test_core_schema_agrees_where_1_1_and_1_2_agree(raw, value):
    got = yaml12.load(f"k: {raw}")["k"]
    assert got == value and type(got) is type(value)


def test_nan():
    assert math.isnan(yaml12.load("k: .nan")["k"])


def test_keys_are_resolved_too():
    assert yaml12.load("on: 1\nyes: 2\n2026-10-15: 3") == {"on": 1, "yes": 2, "2026-10-15": 3}


def test_merge_key_is_a_plain_key():
    # YamlDotNet does not merge '<<' unless asked to (MergingParser); 1.2 has no merge key.
    assert yaml12.load("a: &x {b: 1}\nc:\n  <<: *x") == {"a": {"b": 1}, "c": {"<<": {"b": 1}}}


def test_explicit_tags_still_work():
    assert yaml12.load("k: !!str 12")["k"] == "12"
    assert yaml12.load("k: !!int '7'")["k"] == 7


def test_safe_load_is_untouched():
    yaml12.load("k: yes")
    assert yaml.safe_load("k: yes")["k"] is True


@pytest.mark.parametrize("raw", ["yes", "Off", "1_0", "1:30", "0x0F", "0o17", "2026-10-15", "~", "12", "1.5", "true",
                                 "null"])
def test_raw_keeps_every_scalar_as_its_text(raw):
    assert yaml12.load_raw(f"k: {raw}")["k"] == raw


def test_raw_keeps_structure_and_quoted_text():
    assert yaml12.load_raw("a: [1, 'x', {b: no}]\nc: |\n  two\n") == {"a": ["1", "x", {"b": "no"}], "c": "two\n"}


def test_load_path(tmp_path):
    p = tmp_path / "a.yaml"
    p.write_text("k: 1:30\n", encoding="utf-8")
    assert yaml12.load_path(p) == {"k": "1:30"}


def test_hazards_lists_every_plain_scalar_1_1_reads_differently():
    text = "a: yes\nb:\n  - 1:30\n  - 'no'\n  - 12\nc: {d: 2026-10-15}\non: 1\n"
    got = [(h.line, h.path, h.raw, h.pyyaml, h.core) for h in yaml12.hazards(text)]
    assert got == [
        (1, "a", "yes", True, "yes"),
        (3, "b[0]", "1:30", 90, "1:30"),
        (6, "c.d", "2026-10-15", date(2026, 10, 15), "2026-10-15"),
        (7, "<key>", "on", True, "on"),
    ]


def test_hazards_of_clean_text_is_empty():
    assert yaml12.hazards("a: 'yes'\nb: 12\nc: true\nd: 1.5\ne: ~\n") == []
