import fnmatch
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import yaml12  # noqa: E402


# sha256 of the official plain text, https://polyformproject.org/licenses/noncommercial/1.0.0.txt (fetched 22 Oct 2026;
# word for word the same as PolyForm-Noncommercial-1.0.0.md in polyformproject/polyform-licenses at 9c032bcb).
POLYFORM_NC_SHA256 = "ffcca38841adb694b6f380647e15f17c446a4d1656fed51a1e2041d064c94cc8"
# sha256 of the official plain text, https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode.txt (fetched
# 23 Oct 2026).
CC_BY_NC_ND_4_0_SHA256 = "38762e3777f4ec00a6f769062a7c3f704fb78ce08303ecff88558da4c49cf9ea"
REQUIRED_NOTICE = "Required Notice: Copyright (c) 2026 Eyal Golan (https://github.com/eyalgolan/statecraft-israel)"


def test_code_licence_is_polyform_noncommercial_unmodified_with_the_required_notice():
    """Owner, 22 Oct 2026: the code is under PolyForm Noncommercial 1.0.0. LICENSE is the Required Notice line, a blank
    line, then the official text byte for byte."""
    import hashlib
    raw = (ROOT / "LICENSE").read_bytes()
    head, blank, body = raw.split(b"\n", 2)
    assert head.decode() == REQUIRED_NOTICE and blank == b""
    assert hashlib.sha256(body).hexdigest() == POLYFORM_NC_SHA256


def test_earlier_mit_licence_is_kept():
    """Code released up to v0.2.1 stays MIT, and LICENSING.md points at the text it was released under."""
    text = (ROOT / "LICENSES/MIT.txt").read_text(encoding="utf-8")
    assert text.startswith("MIT License\n\nCopyright (c) 2026 Eyal Golan\n") and "Permission is hereby granted" in text
    licensing = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    for s in ("(LICENSES/MIT.txt)", "v0.2.1", "c7a5fbce", "PolyForm Noncommercial License 1.0.0](LICENSE)",
              "Commercial use needs written permission from Eyal Golan", "checked by a lawyer"):
        assert s in licensing, s
    assert "@" not in licensing.split("**The code licence changed")[1].split("\n\n")[0]


def test_licence_texts_are_the_real_ones():
    for name, marker in (("CC-BY-4.0", "Attribution 4.0 International"),
                         ("CC-BY-NC-ND-4.0", "Attribution-NonCommercial-NoDerivatives 4.0 International"),
                         ("CC-BY-SA-4.0", "Attribution-ShareAlike 4.0 International"),
                         ("CC0-1.0", "CC0 1.0 Universal")):
        assert marker in (ROOT / f"LICENSES/{name}.txt").read_text(encoding="utf-8"), name
    import hashlib
    assert hashlib.sha256((ROOT / "LICENSES/CC-BY-NC-ND-4.0.txt").read_bytes()).hexdigest() == CC_BY_NC_ND_4_0_SHA256
    yaml_dot_net = (ROOT / "LICENSES/YamlDotNet-MIT.txt").read_text(encoding="utf-8")
    assert "Antoine Aubry" in yaml_dot_net and "Permission is hereby granted" in yaml_dot_net


def test_licensing_table_covers_every_top_level_area():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    for d in ("sim/", "tools/", "scripts/", ".github/", "unity/Assets/", "content/", "docs/", "reports/",
              "research_notes/", "promo/", "spikes/", "translations/", "ASSETS.yaml", "unity/Assets/Settings/", "unity/Packages/"):
        assert f"`{d}" in text, d


def test_licensing_puts_unity_template_files_under_the_companion_licence():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    row = next(l for l in text.splitlines() if l.startswith("| Unity template-derived files"))
    assert "Unity Companion License" in row and "spikes/m0/unity/Assets/Settings/" in row and "not the code licence" in row


def test_licensing_points_at_decisions_and_assets():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    assert "docs/oss/DECISIONS.md" in text and "ASSETS.yaml" in text and "published before the checks" in text


def test_notice_carries_the_required_attributions():
    text = (ROOT / "NOTICE").read_text(encoding="utf-8")
    assert "produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014" in text
    for s in ("CHELSA V2.1", "Israel Meteorological Service", "SIL Open Font License", "Wikipedia", "YamlDotNet"):
        assert s in text, s


def test_every_repo_path_licensing_names_exists():
    """Every backticked repo path in LICENSING.md names something tracked, so the table never licenses a file that is
    gone (the decision clips' old label, tools/event_videos/ai-bug.png, was deleted in #551).
    What counts as a repo path: a backticked token with a "/" whose first segment is a tracked top-level entry
    (`unity/...`, `promo/...`, `.github/`). Skipped as not repo paths: URLs, placeholders such as `<name>`, and tokens
    with no "/" or a relative first segment (`*.md`, `.meta`, `link.xml`, `FrankRuhlLibre/`, `08`), which name a kind of
    file or a file inside a folder the row names. A token with * or ? must match at least one tracked file; any other
    token must be a tracked file or the start of one's path (a folder, or `promo/final/01` for the file 01-key-art.png)."""
    import fnmatch
    import re
    import subprocess
    tracked = subprocess.run(["git", "ls-files"], cwd=ROOT, capture_output=True, text=True, check=True).stdout.splitlines()
    top = {p.split("/", 1)[0] for p in tracked}
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    missing = []
    for token in sorted(set(re.findall(r"`([^`\n]+)`", text))):
        if "://" in token or "<" in token or "/" not in token or token.split("/", 1)[0] not in top:
            continue
        if any(c in token for c in "*?"):
            ok = any(fnmatch.fnmatchcase(p, token) for p in tracked)
        else:
            ok = any(p == token or p.startswith(token) for p in tracked)
        if not ok:
            missing.append(token)
    assert missing == [], f"LICENSING.md names paths that are not in the repository: {missing}"



def test_tal_original_licensing_matches_manifest_exactly():
    """Only the 146 pinned originals receive the 20 October 2026 original-file terms.

    Execution is PENDING (root, at APPLY); this is a static pre-draft.
    Selecting by manifest instead of artist permits independent newer grants and historical records.
    """
    manifest = json.loads((ROOT / "docs/original-models.json").read_text(encoding="utf-8"))
    originals = {f["path"] for f in manifest["files"] if f["source"] in {"336", "507"}}
    generated = {f["path"] for f in manifest["files"] if f["source"] == "generated (ours)"}
    assert len(originals) == 146 and len(generated) == 311
    assert originals.isdisjoint(generated)
    assert len(manifest["files"]) == 457  # reject unknown classifications or duplicate paths
    rules = yaml12.load_path(ROOT / "ASSETS.yaml")["rules"]
    selected = [r for r in rules if any(fnmatch.fnmatch(p, r["glob"]) for p in originals)]
    assert len(selected) == 146
    assert {r["glob"] for r in selected} == originals  # no wildcard or extra path
    for path in originals:
        assert sum(r["glob"] == path and r["licence"] == "CC-BY-NC-ND-4.0" for r in rules) == 1, path
    independent = [r for r in rules if r["licence"] == "CC-BY-NC-ND-4.0" and r["glob"] not in originals]
    assert len(independent) == 50  # 146 originals + 50 independent grants (with the hex-tile props row and the #1901 -light meshes) = 196 total
    for row in selected:
        assert row["licence"] == "CC-BY-NC-ND-4.0"
        assert row["artist"] == "Tal Heuberger"
        for required in ("from 2026-10-20", "credit 'Tal Heuberger'",
                         "https://creativecommons.org/licenses/by-nc-nd/4.0/",
                         "do not label the current originals as CC0"):
            assert required in row["note"], (row["glob"], required)
        assert "docs/original-models.json" in row["source"]


def test_no_asset_licence_rule_matches_generated_manifest_paths():
    manifest = json.loads((ROOT / "docs/original-models.json").read_text(encoding="utf-8"))
    generated = {f["path"] for f in manifest["files"] if f["source"] == "generated (ours)"}
    assert len(generated) == 311
    rules = yaml12.load_path(ROOT / "ASSETS.yaml")["rules"]
    captured = [(r["glob"], p) for r in rules for p in sorted(generated)
                if fnmatch.fnmatch(p, r["glob"])]
    assert captured == [], f"Asset-licence rows capture generated paths: {captured}"


def test_tal_history_excludes_current_originals_and_keeps_generated_code_terms():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    original = next(l for l in text.splitlines() if l.startswith("| Original 3D model files by Tal Heuberger |"))
    for required in ("146", "docs/original-models.json", "20 October 2026", 'credit "Tal Heuberger"',
                     "without their permission", "not slimmed", "311 generated"):
        assert required in original
    history = text.split("**Historical model copies and versions.**", 1)[1].split("\n\n", 1)[0]
    for required in ("2d3e13c098e147493ab034ab2ff20f0b04765fab", "valid CC0 grant before 20 October 2026",
                     "do not label the current originals as CC0", "same-path/same-blob"):
        assert required in history
    code = next(l for l in text.splitlines() if l.startswith("| Code |"))
    assert "[PolyForm Noncommercial 1.0.0](LICENSE)" in code
    assert "everything else under `unity/Assets/`" in code and "`.meta` files" in code
    assert "v0.2.1" in code  # the earlier MIT boundary is retained
    assert "any later model whose" not in text
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    credit = next(p for p in notice.split("\n\n") if "Tal Heuberger" in p)
    for required in ("146 original files", "docs/original-models.json", "CC BY-NC-ND 4.0",
                     "https://creativecommons.org/licenses/by-nc-nd/4.0/", "20 October 2026",
                     "do not label the current originals as CC0", "311 generated"):
        assert required in credit


def test_exact_licensing_code_row_is_unchanged():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    code = [line for line in text.splitlines(keepends=True) if line.startswith("| Code |")]
    assert code == ['| Code | `sim/`, `tools/` (Python), `scripts/`, `.github/`, `.gitignore`, `unity/Assets/Scripts/`, `unity/Assets/Editor/`, `unity/Assets/Tests/`, `promo/*.py`, `spikes/` code, and everything else under `unity/Assets/` not listed below (scenes such as `Map.unity`, `WebGLTemplates/`, `link.xml`, `*.inputactions`, `.meta` files) | [PolyForm Noncommercial 1.0.0](LICENSE). Code released up to and including v0.2.1 stays MIT; see below |\n']
