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
# Tal Heuberger agreed on 12 Nov 2026 to move their files to the exclusive licence (#3159, decision X1); Afik Faerman's
# music stays CC BY-NC-ND 4.0 until Afik agrees (owner, 12 Nov 2026: "Switch Tal's now (Recommended)").
EXCLUSIVE = "LicenseRef-Statecraft-Exclusive-Asset-1.0"
NC_ND = "CC-BY-NC-ND-4.0"
EXCLUSIVE_LINK = f"[Statecraft: Israel Exclusive Asset Licence 1.0](LICENSES/{EXCLUSIVE}.txt)"
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
        assert sum(r["glob"] == path and r["licence"] == EXCLUSIVE for r in rules) == 1, path
    independent = [r for r in rules if r["licence"] in {EXCLUSIVE, NC_ND} and r["glob"] not in originals]
    # Tal Heuberger's #2082-#2085 files (owner, 2 November 2026: "Yes, #1177 covers them"): the 24 exact source files and,
    # for the four #2085 towns in the game, each town's compatibility FBX and colour map. Every other grant stays as it was.
    sources = {
        "asset-sources/prepared-towns-20261031/" + p for p in (
            "netanya/statecraft-netanya-approved-textured-4350a137.glb", "netanya/statecraft-netanya-approved-textured-4350a137_basecolor.jpg",
            "ramla/statecraft-ramla-approved-textured-aaf2619f.glb", "ramla/statecraft-ramla-approved-textured-aaf2619f_basecolor.jpg",
            "safed/statecraft-safed-approved-textured-1e2b8e59.glb", "safed/statecraft-safed-approved-textured-1e2b8e59_basecolor.jpg",
            "tiberias/statecraft-tiberias-smartmesh-original-086ad55f.glb", "tiberias/statecraft-tiberias-smartmesh-textured-086ad55f.glb",
            "tiberias/statecraft-tiberias-smartmesh-textured-086ad55f_basecolor.jpg")
    } | {
        "asset-sources/recovered-vegetation-20261031/" + p for p in (
            "g25_papyrus/albedo.jpg", "g25_papyrus/g25_papyrus.obj", "g25_papyrus/normal.jpg",
            "hf_tamarisk/albedo.jpg", "hf_tamarisk/hf_tamarisk.obj", "hf_tamarisk/normal.jpg")
    } | {
        "asset-sources/prepared-weapons-20261031/" + p for p in (
            "k98k/statecraft-k98k-approved-textured-b48a435f.glb", "k98k/statecraft-k98k-approved-textured-b48a435f_basecolor.jpg",
            "mg34/statecraft-mg34-approved-textured-3a384995.glb", "mg34/statecraft-mg34-approved-textured-3a384995_basecolor.jpg",
            "sten/statecraft-sten-approved-textured-bc6b06c8.glb", "sten/statecraft-sten-approved-textured-bc6b06c8_basecolor.jpg")
    } | {
        "asset-sources/c06-point-fire-reference-20261031/" + p for p in (
            "C06-POINT-FIRE-FULL-REVIEW.mp4", "C06-POINT-FIRE-PROVISIONAL-PUBLIC.fbx", "C06-POINT-FIRE-REVIEW-CONTACT-SHEET.png")
    }
    towns = {f"unity/Assets/Art/Collection/{t}-town-v1/{t}-town-v1{x}"
             for t in ("tiberias", "safed", "netanya", "ramla") for x in (".fbx", "_basecolor.jpg")}
    # #2082's papyrus and tamarisk in the game (owner, 2 November 2026: "Hula + Arava, map and boards (Recommended)"): the
    # byte-identical Unity copies of the six recovered sources.
    plants = {f"unity/Assets/Art/HexTiles/Props/{d}/{f}" for d in ("hula_swamp_papyrus", "arava_oasis_tamarisk")
              for f in (d + ".obj", "albedo.jpg", "normal.jpg")}
    # #2083 and #2084 in the game (owner, 2 November 2026): the K98k's and the Sten's compatibility FBX and colour map, and
    # the C06 point-fire export byte for byte.
    weapons = {f"unity/Assets/Art/Collection/weapon-{w}-v1/weapon-{w}-v1{x}" for w in ("k98k", "sten") for x in (".fbx", "_basecolor.jpg")} | {
        "unity/Assets/Art/Collection/c06-point-fire-v1/C06-POINT-FIRE-PROVISIONAL-PUBLIC.fbx"}
    # #2384 in the game (owner, 3 November 2026): the 1948 soldier's rigged copy, Tal's #336 mesh on their C07's skeleton, and
    # its colour map, Tal's byte for byte.
    rigged = {f"unity/Assets/Art/Collection/figure-soldier-1948-v1-rigged/figure-soldier-1948-v1-rigged{x}" for x in (".fbx", "_basecolor.jpg")}
    added = sources | towns | plants | weapons | rigged
    assert len(sources) == 24 and len(towns) == 8 and len(plants) == 6 and len(weapons) == 5 and len(rigged) == 2
    added_rows = [r for r in independent if r["glob"] in added]
    assert {r["glob"] for r in added_rows} == added and len(added_rows) == 45
    assert all("Hula + Arava, map and boards" in r["note"] for r in added_rows if r["glob"] in plants)
    assert all(r["artist"] == "Tal Heuberger" and r["ai_generated"] is True for r in added_rows)
    assert all("#1177" in r["note"] and "2 November 2026" in r["note"] for r in added_rows if r["glob"] not in rigged)
    assert all("#2384" in r["note"] and "3 November 2026" in r["note"] for r in added_rows if r["glob"] in rigged)
    import hashlib
    for r in added_rows:  # each row records its file's SHA-256
        assert hashlib.sha256((ROOT / r["glob"]).read_bytes()).hexdigest() in r["source"], r["glob"]
    # Afik Faerman's main theme (#39, #2240; owner, 2 November 2026: CC BY-NC-ND 4.0, credited to Afik): one exact-file row.
    music = [r for r in independent if r["glob"] == "unity/Assets/StreamingAssets/audio/music/Main_1.3.ogg"]
    assert len(music) == 1 and music[0]["artist"] == "Afik Faerman" and music[0]["ai_generated"] is False
    assert music[0]["licence"] == NC_ND  # until Afik agrees (X1)
    assert all(r["licence"] == EXCLUSIVE and r["artist"] == "Tal Heuberger" for r in independent if r not in music)
    # The fire take baked for C04, which has none of its own (#2375; owner, 3 November 2026: Tal Heuberger's permission for
    # this adapted version): one exact-file row, a derived clip of a take inside Tal's own FBX. The enemy infantry get none
    # yet (owner, 3 November 2026: "C04 only; enemies wait (Recommended)"; #2399).
    fire = {"unity/Assets/Art/Collection/figure-c04-sabra-pioneer-v3/fire.anim"}
    fire_rows = [r for r in independent if r["glob"] in fire]
    assert {r["glob"] for r in fire_rows} == fire and len(fire_rows) == 1
    assert all(r["artist"] == "Tal Heuberger" and r["ai_generated"] is True for r in fire_rows)
    assert all("owner confirms Tal's permission, 3 Nov 2026 (#2375)" in r["note"] and "FireTakeRetarget" in r["note"] for r in fire_rows)
    assert all((ROOT / r["glob"]).is_file() for r in fire_rows)
    assert not [r["glob"] for r in rules if r["glob"].endswith("/fire.anim") and r["glob"] not in fire]  # no enemy fire take yet (#2399)
    far_globs = {
        "unity/Assets/Art/Settlements/Prototype/Models/*/*-far.asset",
        "unity/Assets/Art/Settlements/Prototype/Models/*/*-far.png",
    }
    far_rows = [r for r in independent if r["glob"] in far_globs]
    assert len(far_rows) == 2 and {r["glob"] for r in far_rows} == far_globs
    assert all(r["licence"] == EXCLUSIVE and r["artist"] == "Tal Heuberger" for r in far_rows)
    licensing = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    far_licensing_rows = [next(line for line in licensing.splitlines() if line.startswith(prefix)) for prefix in (
        "| New 22 October buildings, tents and props ", "| Original 3D model files by Tal Heuberger |")]
    assert all("`*-far.asset`" in row and "`*-far.png`" in row for row in far_licensing_rows)
    assert all(EXCLUSIVE_LINK in row and "Tal Heuberger" in row for row in far_licensing_rows)
    # The hex-tile props row, the #1901 -light meshes and the two #2378 far-off copy rows (*-far.asset, *-far.png; owner,
    # 3 November 2026, confirms Tal's permission) among them.
    assert len([r for r in independent if r["glob"] not in added and r not in music and r["glob"] not in fire]) == 52
    assert len(independent) == 99  # 146 originals + 99 independent grants = 245 total
    for row in selected:
        assert row["licence"] == EXCLUSIVE
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


def test_borrowed_fire_clip_resolves_to_explicit_tal_row():
    """C04's derived clip has an exact LICENSING.md row and does not fall back to the PolyForm code row."""
    import re
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    row = next(l for l in text.splitlines() if l.startswith("| Derived borrowed fire clip by Tal Heuberger |"))
    fire = {"unity/Assets/Art/Collection/figure-c04-sabra-pioneer-v3/fire.anim"}
    assert set(re.findall(r"`(unity/Assets/Art/Collection/[^`]+/fire\.anim)`", row)) == fire
    assert EXCLUSIVE_LINK in row
    assert "PolyForm" not in row


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


def test_tal_files_are_under_the_exclusive_licence_and_afik_music_is_not_yet():
    """#3159: Tal Heuberger agreed on 12 Nov 2026 (decision X1 ticked); Afik Faerman has not agreed yet."""
    text = (ROOT / f"LICENSES/{EXCLUSIVE}.txt").read_text(encoding="utf-8")
    assert text.startswith("Statecraft: Israel Exclusive Asset Licence 1.0") and "Tal Heuberger" in text
    rules = yaml12.load_path(ROOT / "ASSETS.yaml")["rules"]
    tal = [r for r in rules if r.get("artist") == "Tal Heuberger" and r["licence"] in {EXCLUSIVE, NC_ND}]
    assert len(tal) == 244 and all(r["licence"] == EXCLUSIVE for r in tal)
    assert [r["glob"] for r in rules if r["licence"] == NC_ND] == ["unity/Assets/StreamingAssets/audio/music/Main_1.3.ogg"]
    licensing = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    tal_rows = [l for l in licensing.splitlines() if l.startswith("|") and "Tal Heuberger" in l and "CC BY-NC-ND" in l]
    assert len(tal_rows) == 11 and all(EXCLUSIVE_LINK in l and "12 November 2026" in l for l in tal_rows)
    afik = next(l for l in licensing.splitlines() if l.startswith("| Music by Afik Faerman |"))
    assert EXCLUSIVE not in afik and "[CC BY-NC-ND 4.0](LICENSES/CC-BY-NC-ND-4.0.txt)" in afik
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    credit = " ".join(next(p for p in notice.split("\n\n") if "Tal Heuberger" in p).split())
    assert "Statecraft: Israel Exclusive Asset Licence 1.0" in credit and "other models, colour maps and clips" in credit
    decisions = (ROOT / "docs/oss/DECISIONS.md").read_text(encoding="utf-8")
    x1 = next(l for l in decisions.splitlines() if l.lstrip().startswith(("- [x] X1", "- [ ] X1")))
    assert x1.startswith("- [x] X1")
