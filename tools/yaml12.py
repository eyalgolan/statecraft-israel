# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""YAML 1.2 core-schema loading for the Python tools and scripts (refs #601).

PyYAML's safe_load follows YAML 1.1, so it coerces plain scalars the game never coerces: yes/no/on/off become bools,
1_0 becomes 10, 1:30 becomes 90 (base 60), 017 becomes 15 (octal), 0b101 becomes 5 and 2026-10-15 becomes a date. The
sim and Unity read the same files with YamlDotNet, which does not. A bake or a check that reads a file with safe_load can
then disagree with the game in silence.

- load(text) / load_path(path): the YAML 1.2 core schema. Bool is true/false only (any case form the spec lists), null
  is null/~/empty, ints are decimal, 0o octal or 0x hex, floats need no dot (1e3) and keep .inf/.nan. No sexagesimal, no
  underscores, no 0b, no implicit timestamps, no '<<' merge key. Everything else is a string.
- load_raw(text): every scalar as its text (quoted or not, '~' stays '~'), for a reader that wants strings only.
- hazards(text): the plain scalars that safe_load reads differently from the core schema, with line and key path.
  `uv run tools/yaml12.py FILE...` prints them.

Use these instead of yaml.safe_load in tools/ and scripts/; tools/tests/test_yaml12_guard.py fails on a new safe_load.
"""
from __future__ import annotations

import math
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import yaml

INT = "tag:yaml.org,2002:int"
FLOAT = "tag:yaml.org,2002:float"
BOOL = "tag:yaml.org,2002:bool"
NULL = "tag:yaml.org,2002:null"
STR = "tag:yaml.org,2002:str"

# YAML 1.2.2, section 10.3.2 (core schema tag resolution).
CORE_RESOLVERS = [
    (NULL, re.compile(r"^(?:null|Null|NULL|~|)$"), list("nN~") + [""]),
    (BOOL, re.compile(r"^(?:true|True|TRUE|false|False|FALSE)$"), list("tTfF")),
    (INT, re.compile(r"^(?:[-+]?[0-9]+|0o[0-7]+|0x[0-9a-fA-F]+)$"), list("-+0123456789")),
    (FLOAT, re.compile(r"^(?:[-+]?(?:\.[0-9]+|[0-9]+(?:\.[0-9]*)?)(?:[eE][-+]?[0-9]+)?"
                       r"|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))$"), list("-+.0123456789")),
]


class Core12Loader(yaml.SafeLoader):
    """SafeLoader with the YAML 1.2 core schema's implicit resolvers and int/float constructors."""
    yaml_implicit_resolvers: dict = {}


for _tag, _regexp, _first in CORE_RESOLVERS:
    Core12Loader.add_implicit_resolver(_tag, _regexp, _first)


def _construct_int(loader: yaml.SafeLoader, node: yaml.ScalarNode) -> int:
    text = loader.construct_scalar(node)
    if re.fullmatch(r"0o[0-7]+", text):
        return int(text[2:], 8)
    if re.fullmatch(r"0x[0-9a-fA-F]+", text):
        return int(text[2:], 16)
    if re.fullmatch(r"[-+]?[0-9]+", text):
        return int(text, 10)
    raise yaml.constructor.ConstructorError(None, None, f"not a YAML 1.2 core int: {text!r}", node.start_mark)


def _construct_float(loader: yaml.SafeLoader, node: yaml.ScalarNode) -> float:
    text = loader.construct_scalar(node)
    body = text.lstrip("+-")
    sign = -1.0 if text.startswith("-") else 1.0
    if body.lower() == ".inf":
        return sign * math.inf
    if body.lower() == ".nan":
        return math.nan
    try:
        return float(text)
    except ValueError:
        raise yaml.constructor.ConstructorError(None, None, f"not a YAML 1.2 core float: {text!r}",
                                                node.start_mark) from None


Core12Loader.add_constructor(INT, _construct_int)
Core12Loader.add_constructor(FLOAT, _construct_float)


class RawLoader(yaml.SafeLoader):
    """Every scalar is a string: its text, as written (no implicit resolution at all)."""
    yaml_implicit_resolvers: dict = {}


def _construct_raw(loader: yaml.SafeLoader, node: yaml.ScalarNode) -> str:
    return loader.construct_scalar(node)


for _tag in (INT, FLOAT, BOOL, NULL, "tag:yaml.org,2002:timestamp"):
    RawLoader.add_constructor(_tag, _construct_raw)


def load(text: str) -> Any:
    """Parse with the YAML 1.2 core schema (what YamlDotNet's typed reads agree with)."""
    return yaml.load(text, Loader=Core12Loader)  # noqa: S506 (a SafeLoader subclass)


def load_path(path: Path | str) -> Any:
    return load(Path(path).read_text(encoding="utf-8"))


def load_raw(text: str) -> Any:
    """Parse keeping every scalar as its text."""
    return yaml.load(text, Loader=RawLoader)  # noqa: S506 (a SafeLoader subclass)


@dataclass(frozen=True)
class Hazard:
    line: int     # 1-based
    path: str     # e.g. "settlements[3].checked"; "<key>" marks a mapping key
    raw: str
    pyyaml: Any   # what yaml.safe_load gives
    core: Any     # what load() gives


def _value(loader_cls, node: yaml.ScalarNode) -> Any:
    """The value loader_cls gives a plain scalar with this text."""
    loader = loader_cls("")
    try:
        tag = loader.resolve(yaml.ScalarNode, node.value, (True, False))
        return loader.construct_object(yaml.ScalarNode(tag, node.value, node.start_mark, node.end_mark), deep=True)
    finally:
        loader.dispose()


def hazards(text: str) -> list[Hazard]:
    """The plain scalars whose safe_load value differs (in value or type) from the core-schema value."""
    out: list[Hazard] = []
    resolver = yaml.SafeLoader("")

    def scalar(node: yaml.ScalarNode, path: str) -> None:
        if node.style is not None or node.tag != resolver.resolve(yaml.ScalarNode, node.value, (True, False)):
            return  # quoted or block scalars, and explicitly tagged ones, are read the same way by both
        old, new = _value(yaml.SafeLoader, node), _value(Core12Loader, node)
        same = type(old) is type(new) and (old == new or (isinstance(old, float) and math.isnan(old) and math.isnan(new)))
        if not same:
            out.append(Hazard(node.start_mark.line + 1, path, node.value, old, new))

    def walk(node: yaml.Node, path: str) -> None:
        if isinstance(node, yaml.ScalarNode):
            scalar(node, path)
        elif isinstance(node, yaml.SequenceNode):
            for i, item in enumerate(node.value):
                walk(item, f"{path}[{i}]")
        elif isinstance(node, yaml.MappingNode):
            for k, v in node.value:
                walk(k, f"{path}.<key>" if path else "<key>")
                name = k.value if isinstance(k, yaml.ScalarNode) else "?"
                walk(v, f"{path}.{name}" if path else name)

    for doc in yaml.compose_all(text, Loader=yaml.SafeLoader):
        if doc is not None:
            walk(doc, "")
    return out


def main(argv: list[str]) -> int:
    count = 0
    for name in argv:
        for h in hazards(Path(name).read_text(encoding="utf-8")):
            count += 1
            print(f"{name}:{h.line}\t{h.path}\t{h.raw!r}\tsafe_load={h.pyyaml!r}\tcore={h.core!r}")
    print(f"{count} hazards in {len(argv)} files", file=sys.stderr)
    return 1 if count else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
