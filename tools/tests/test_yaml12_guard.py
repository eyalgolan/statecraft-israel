"""Guard (refs #601): no new PyYAML YAML 1.1 load in tools/, scripts/ or .github/. Read YAML with tools/yaml12.py
(load, load_path or load_raw), which resolves plain scalars by the YAML 1.2 core schema and rejects duplicate keys. A
file that must keep a PyYAML load goes in ALLOWED with the reason.

The scan parses each file (ast): it follows `import yaml as y`, `from yaml import (safe_load as parse)` and `from yaml
import *`, and flags any call through those names whose yaml-side name contains "load" (safe_load, load, full_load,
*_all, and the SafeLoader classes)."""
import ast
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCANNED = ["tools", "scripts", ".github"]
SKIP_DIRS = {".cache", ".venv", "__pycache__", "node_modules"}
LOADISH = re.compile("load", re.I)


def _calls(tree: ast.AST) -> list[int]:
    """Line numbers of the calls that reach a PyYAML load function or loader class."""
    modules: set[str] = set()      # local names bound to the yaml package (or a yaml.* submodule's root)
    functions: set[str] = set()    # local names bound to a load-ish name imported from yaml
    star = False
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                if alias.name == "yaml" or alias.name.startswith("yaml."):
                    modules.add(alias.asname or "yaml")
        elif isinstance(node, ast.ImportFrom) and node.level == 0 and node.module and (
                node.module == "yaml" or node.module.startswith("yaml.")):
            for alias in node.names:
                if alias.name == "*":
                    star = True
                elif LOADISH.search(alias.name):
                    functions.add(alias.asname or alias.name)
    lines = []
    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        f = node.func
        if isinstance(f, ast.Attribute) and LOADISH.search(f.attr):
            root = f.value
            while isinstance(root, ast.Attribute):
                root = root.value
            if isinstance(root, ast.Name) and root.id in modules:
                lines.append(node.lineno)
        elif isinstance(f, ast.Name) and (f.id in functions or (star and LOADISH.search(f.id))):
            lines.append(node.lineno)
    return sorted(lines)


def offenders(root: Path) -> dict[str, list[int]]:
    """{repo-relative path: [line numbers]} of every PyYAML load call in the scanned trees."""
    found: dict[str, list[int]] = {}
    for top in SCANNED:
        for p in sorted((root / top).rglob("*.py")):
            if SKIP_DIRS & set(p.relative_to(root).parts):
                continue
            lines = _calls(ast.parse(p.read_text(encoding="utf-8"), filename=str(p)))
            if lines:
                found[p.relative_to(root).as_posix()] = lines
    return found


ALLOWED = {
    "tools/yaml12.py": "the helper itself (yaml.load with its own SafeLoader subclasses)",
    "tools/tests/test_yaml12.py": "pins safe_load's YAML 1.1 readings on purpose, to show the hazard the helper removes",
    "scripts/oss/tests/test_policies.py": "reads .github issue forms and workflows, which GitHub parses, not the game",
    "scripts/oss/tests/test_github_config.py": "reads .github workflows, which GitHub parses; it expects PyYAML's True "
                                               "for the bare 'on' key",
}


def test_no_yaml_1_1_load_outside_the_helper_and_the_allowlist():
    new = {f: lines for f, lines in offenders(ROOT).items() if f not in ALLOWED}
    assert not new, ("PyYAML loads (YAML 1.1: yes -> True, 1:30 -> 90, 2026-10-15 -> a date); use tools/yaml12.py, or "
                     f"add the file to ALLOWED with the reason: {new}")


def test_allowlist_is_not_stale():
    found = offenders(ROOT)
    stale = [f for f in ALLOWED if (ROOT / f).exists() and f not in found]
    assert not stale, f"no longer loads YAML the 1.1 way; drop it from ALLOWED: {stale}"


def test_every_allowance_has_a_reason():
    assert all(len(reason) > 20 for reason in ALLOWED.values())


EVASIONS = {
    "plain": "import yaml\nx = yaml.safe_load(t)\n",
    "loader_kwarg": "import yaml\nx = yaml.load(t, Loader=yaml.SafeLoader)\n",
    "full_load": "import yaml\nx = yaml.full_load(t)\n",
    "load_all": "import yaml\nx = list(yaml.safe_load_all(t))\n",
    "spaced": "import yaml\nx = yaml . safe_load (t)\n",
    "module_alias": "import yaml as y\nx = y.safe_load(t)\n",
    "module_alias_load": "import yaml as y\nx = y.load(t, Loader=y.SafeLoader)\n",
    "from_import": "from yaml import safe_load\nx = safe_load(t)\n",
    "from_import_alias": "from yaml import load as parse\nx = parse(t)\n",
    "from_import_parenthesised": "from yaml import (\n    dump,\n    safe_load,\n)\nx = safe_load(t)\n",
    "from_import_star": "from yaml import *\nx = safe_load(t)\n",
    "nested_import": "def f(t):\n    import yaml as yy\n    return yy.safe_load(t)\n",
    "submodule_import": "import yaml.constructor\nx = yaml.safe_load(t)\n",
}
CLEAN = {
    "helper": "import yaml12\nx = yaml12.load(t)\n",
    "dump": "import yaml\ns = yaml.safe_dump(d)\n",
    "other_load": "import json\nfrom pickle import load\nx = json.load(f)\ny = load(f)\n",
    "yaml12_alias": "import yaml12 as yaml\nx = yaml.load(t)\n",
}


def _tree(tmp_path, files):
    for top in SCANNED:
        (tmp_path / top).mkdir(parents=True, exist_ok=True)
    for name, text in files.items():
        (tmp_path / "tools" / f"{name}.py").write_text(text, encoding="utf-8")
    return tmp_path


def test_the_scan_catches_each_form(tmp_path):
    found = offenders(_tree(tmp_path, EVASIONS))
    assert sorted(found) == sorted(f"tools/{name}.py" for name in EVASIONS)


def test_the_scan_passes_clean_code(tmp_path):
    assert offenders(_tree(tmp_path, CLEAN)) == {}


def test_the_scan_skips_caches(tmp_path):
    _tree(tmp_path, {})
    (tmp_path / "tools" / ".cache").mkdir()
    (tmp_path / "tools" / ".cache" / "x.py").write_text("import yaml\nyaml.safe_load(t)\n", encoding="utf-8")
    assert offenders(tmp_path) == {}


def test_the_scanned_directories_exist():
    assert all((ROOT / top).is_dir() for top in SCANNED)
