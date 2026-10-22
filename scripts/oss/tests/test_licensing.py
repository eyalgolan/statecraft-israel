from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


# sha256 of the official plain text, https://polyformproject.org/licenses/noncommercial/1.0.0.txt (fetched 22 Oct 2026;
# word for word the same as PolyForm-Noncommercial-1.0.0.md in polyformproject/polyform-licenses at 9c032bcb).
POLYFORM_NC_SHA256 = "ffcca38841adb694b6f380647e15f17c446a4d1656fed51a1e2041d064c94cc8"
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
                         ("CC-BY-SA-4.0", "Attribution-ShareAlike 4.0 International"),
                         ("CC0-1.0", "CC0 1.0 Universal")):
        assert marker in (ROOT / f"LICENSES/{name}.txt").read_text(encoding="utf-8"), name
    yaml_dot_net = (ROOT / "LICENSES/YamlDotNet-MIT.txt").read_text(encoding="utf-8")
    assert "Antoine Aubry" in yaml_dot_net and "Permission is hereby granted" in yaml_dot_net


def test_licensing_table_covers_every_top_level_area():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    for d in ("sim/", "tools/", "scripts/", ".github/", "unity/Assets/", "content/", "docs/", "reports/",
              "research_notes/", "promo/", "spikes/", "ASSETS.yaml", "unity/Assets/Settings/", "unity/Packages/"):
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
