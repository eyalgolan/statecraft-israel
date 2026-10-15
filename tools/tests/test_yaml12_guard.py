"""Guard (refs #601): no new PyYAML YAML 1.1 load in tools/ or scripts/. Read YAML with tools/yaml12.py (load, load_path
or load_raw), which resolves plain scalars by the YAML 1.2 core schema as the game's YamlDotNet does. A file that must
keep yaml.safe_load goes in ALLOWED with the reason."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCANNED = ["tools", "scripts"]
SKIP_DIRS = {".cache", ".venv", "__pycache__", "node_modules"}

LOAD = r"(?:safe_|full_|unsafe_)?load(?:_all)?"
PATTERN = re.compile(rf"\byaml\s*\.\s*{LOAD}\s*\(|^\s*from\s+yaml\s+import\s+[^\n]*\b{LOAD}\b", re.M)

ALLOWED = {
    "tools/yaml12.py": "the helper itself (yaml.load with its own SafeLoader subclasses)",
    "tools/tests/test_yaml12.py": "pins safe_load's YAML 1.1 readings on purpose, to show the hazard the helper removes",
    "tools/route_paths_bake.py": "being reworked in #547; migrate it there or after",
    "scripts/pr_content_summary.py": "(c) a markdown report; reads event and model from/to through str(), and a date "
                                     "prints as the same text",
    "scripts/event_timeline.py": "(c) a printed report; reads from/to through str(), the same text either way",
    "scripts/source_report.py": "(c) a printed report; to_date() takes a date or its ISO text alike",
    "scripts/oss/commons_licences.py": "(c) writes ASSETS.portraits.yaml (a licence manifest the game never reads) from "
                                       "people.yaml's portrait file and source strings",
    "scripts/oss/ai_disclosure.py": "(c) reads ASSETS.yaml and ASSETS.portraits.yaml, licence manifests only Python reads",
    "scripts/oss/check_repo.py": "(c) reads the ASSETS manifests and scripts/oss/allow.yaml, policy files only Python reads",
    "scripts/oss/tests/test_policies.py": "(c) reads .github issue forms and workflows (GitHub's YAML, not the game's)",
    "scripts/oss/tests/test_github_config.py": "(c) reads .github workflows, whose bare 'on' key it expects as True",
}


def offenders(root: Path) -> dict[str, list[int]]:
    """{repo-relative path: [line numbers]} of every YAML 1.1 load call in the scanned trees."""
    found: dict[str, list[int]] = {}
    for top in SCANNED:
        for p in sorted((root / top).rglob("*.py")):
            if SKIP_DIRS & set(p.relative_to(root).parts):
                continue
            text = p.read_text(encoding="utf-8")
            lines = [text.count("\n", 0, m.start()) + 1 for m in PATTERN.finditer(text)]
            if lines:
                found[p.relative_to(root).as_posix()] = lines
    return found


def test_no_yaml_1_1_load_outside_the_helper_and_the_allowlist():
    new = {f: lines for f, lines in offenders(ROOT).items() if f not in ALLOWED and f != "tools/tests/test_yaml12_guard.py"}
    assert not new, ("YAML 1.1 loads (yes -> True, 1:30 -> 90, 2026-10-15 -> a date) where the game reads YAML 1.2; use "
                     f"tools/yaml12.py, or add the file to ALLOWED with the reason: {new}")


def test_allowlist_is_not_stale():
    found = offenders(ROOT)
    stale = [f for f in ALLOWED if (ROOT / f).exists() and f not in found]
    assert not stale, f"no longer loads YAML the 1.1 way; drop it from ALLOWED: {stale}"


def test_every_allowance_has_a_reason():
    assert all(len(reason) > 20 for reason in ALLOWED.values())


def test_the_scan_catches_each_form(tmp_path):
    forms = ["yaml.safe_load(t)", "yaml.load(t, Loader=yaml.SafeLoader)", "yaml.full_load(t)", "yaml.safe_load_all(t)",
             "yaml . safe_load (t)", "from yaml import safe_load", "from yaml import dump, load"]
    for i, form in enumerate(forms):
        (tmp_path / "tools").mkdir(exist_ok=True)
        (tmp_path / "tools" / f"f{i}.py").write_text(f"import yaml\nx = 1\n{form}\n", encoding="utf-8")
    (tmp_path / "scripts").mkdir()
    (tmp_path / "scripts" / "ok.py").write_text("import yaml12\nyaml12.load(t)\nyaml.safe_dump(d)\n", encoding="utf-8")
    (tmp_path / "tools" / ".cache").mkdir()
    (tmp_path / "tools" / ".cache" / "x.py").write_text("yaml.safe_load(t)\n", encoding="utf-8")
    assert offenders(tmp_path) == {f"tools/f{i}.py": [3] for i in range(len(forms))}
