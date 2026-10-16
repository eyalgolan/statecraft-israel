"""The YAML the game reads (content/, unity/Assets, the sim's test scenarios) holds no plain scalar that the readers could
take differently (refs #601, review of #606):
- no YAML 1.1 hazard (PyYAML's safe_load against the 1.2 core schema) except dates. A date-only YYYY-MM-DD is read as
  text by YamlDotNet's string fields and by every Python reader here (through tools/yaml12.py or str()), and str(date)
  is that same text;
- no scalar a YamlDotNet bool or int field accepts beyond YAML 1.2 (y, yes, off, 017, 1_000, 1:30, 0b101, 0o17): quote
  it if it is text, write true/false or a plain decimal if it is not;
- no duplicate key.
The scenarios' `on:` step key is allowed: the sim reads it as the key "on" (a string), and no Python reader loads them.
"""
import datetime
import subprocess
from pathlib import Path

import pytest

import yaml12

ROOT = Path(__file__).resolve().parents[2]
SCENARIOS = "sim/Statecraft.Sim.Tests/scenarios"
# Tracked files only: a local WebGL build copies content/ into the gitignored unity/Assets/StreamingAssets/content/,
# and a glob over the working tree would test those copies too (54 extra cases in one run, review of #686).
GAME_YAML = sorted(ROOT / rel for rel in subprocess.run(
    ["git", "ls-files", "-z", "--", "content/*.yaml", "unity/Assets/*.yaml", f"{SCENARIOS}/*.yaml"],
    cwd=ROOT, check=True, capture_output=True, text=True).stdout.split("\0") if rel)


def allowed(rel: str, h: yaml12.Hazard) -> bool:
    return rel.startswith(SCENARIOS + "/") and h.path.endswith("<key>") and h.raw == "on"


def test_the_game_yaml_is_found():
    rels = {p.relative_to(ROOT).as_posix() for p in GAME_YAML}
    assert len(rels) > 50 and "content/people.yaml" in rels and "unity/Assets/UI/strings.en.yaml" in rels
    assert any(r.startswith(SCENARIOS + "/") for r in rels)
    assert not any(r.startswith("unity/Assets/StreamingAssets/content/") for r in rels)  # gitignored build copies


@pytest.mark.parametrize("path", GAME_YAML, ids=lambda p: p.relative_to(ROOT).as_posix())
def test_no_scalar_the_readers_take_differently(path):
    rel, text = path.relative_to(ROOT).as_posix(), path.read_text(encoding="utf-8")
    bad = [f"{rel}:{h.line} {h.path} {h.raw!r}: safe_load {h.pyyaml!r}, 1.2 core {h.core!r}"
           for h in yaml12.hazards(text)
           if not (type(h.pyyaml) is datetime.date and h.core == str(h.pyyaml)) and not allowed(rel, h)]
    bad += [f"{rel}:{h.line} {h.path} {h.raw!r}: a YamlDotNet bool or int field reads it beyond YAML 1.2"
            for h in yaml12.ambiguous(text) if not allowed(rel, h)]
    assert not bad, "\n".join(bad)
    yaml12.load(text)  # and no duplicate key
