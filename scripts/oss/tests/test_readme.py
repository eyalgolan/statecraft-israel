import re
from pathlib import Path
from urllib.parse import urlparse

ROOT = Path(__file__).resolve().parents[3]
CONCEPT = "docs/images/hero-concept-1936.jpg"
CLIP = "docs/images/build-gameplay.webp"
GRID = ["docs/images/build-m3-map.jpg", "docs/images/build-m3-start.jpg", "docs/images/build-m3-city.jpg",
        "docs/images/build-m3-cabinet.jpg", "docs/images/build-m3-why.jpg"]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def images(text: str) -> list[str]:
    return [a or b for a, b in re.findall(r'<img src="([^"]+)"|!\[[^\]]*\]\(([^)]+)\)', text)
            if urlparse(a or b).hostname != "img.shields.io"]


def section(text: str, heading: str) -> str:
    return text.split(heading, 1)[1].split("\n## ", 1)[0]


def test_language_bar_comes_first():
    first = read("README.md").lstrip().splitlines()[0]
    assert "English" in first and "README.he.md" in first


def test_hero_is_the_trailer_in_githubs_player_disclosed_as_ai():
    text = read("README.md")
    head = text.split("# Statecraft: Israel", 1)[0]
    assert re.search(r"^https://github\.com/user-attachments/assets/[0-9a-f-]{36}$", head, re.M)
    assert "AI video tools" in head and "not in-game footage" in head


def test_concept_art_opens_what_is_this_captioned_as_concept_art():
    what = section(read("README.md"), "## What is this?")
    assert images(what)[0] == CONCEPT
    tag = re.search(rf'<img src="{re.escape(CONCEPT)}"[^>]*>', what, re.S).group(0)
    assert "AI-generated concept art" in tag and "not the current build" in tag
    after = what.split(tag, 1)[1][:600]
    assert "Concept art (AI-generated)" in after and "set in 1936" in after and "not the current build" in after


def test_real_dated_screenshots_in_a_one_plus_two_plus_two_grid():
    text = read("README.md")
    assert images(text)[1:7] == [CLIP] + GRID
    assert re.search(r"Real gameplay, main at [0-9a-f]{7}, \d{4}-\d{2}-\d{2}\.", text)
    for path, width in zip(GRID, ("820", "405", "405", "405", "405")):
        assert re.search(rf'<img src="{re.escape(path)}"[^>]*width="{width}"', text), path
    assert re.search(r"Real screenshots, main at [0-9a-f]{7}, \d{4}-\d{2}-\d{2}[.;]", text)


def test_note_says_independent_free_and_non_commercial_before_what_is_this():
    text = read("README.md")
    note = text.index("> [!NOTE]")
    assert note < text.index("## What is this?")
    block = text[note:note + 600]
    assert "not affiliated with or endorsed by" in block and "any person depicted" in block
    assert "free and non-commercial" in block and "commercial edition" not in text


def test_link_bar_routes_errors_to_the_form():
    text = read("README.md")
    assert "template=historical_accuracy.yml" in text.split("## What is this?", 1)[0]


def test_history_section_is_accurate():
    hist = section(read("README.md"), "## History, sources and accuracy")
    assert "historical_accuracy.yml" in hist and "docs/CONTENT_POLICY.md" in hist and "not neutral" in hist
    assert "Wikipedia" in hist and "Historian review is planned for M4" in hist
    assert "DRAFT" not in read("README.md")  # review I3: no such stamp exists


# The live tracker (#374) and one issue per GDD phase, 0 to 7 (docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md, §6).
ROADMAP_ISSUES = (374, 375, 382, 383, 384, 385, 386, 387)


def test_roadmap_links_the_tracker_and_every_phase_in_both_readmes():
    for rel, heading, anchor in (("README.md", "## Roadmap", "#roadmap"), ("README.he.md", "## מפת דרכים", "#מפת-דרכים")):
        road = section(read(rel), heading)
        assert "```mermaid" in road, rel
        for n in ROADMAP_ISSUES:
            assert f"https://github.com/eyalgolan/statecraft-israel/issues/{n})" in road, (rel, n)
        assert f"]({anchor})" in read(rel).split(heading, 1)[0], (rel, "the link bar points at the roadmap")


def test_roadmap_phase_1_progress_matches_in_both_readmes():
    # The status is updated by hand (PR template); this catches one README moving without the other.
    en = re.search(r"(\d+) of (\d+) PRs merged", section(read("README.md"), "## Roadmap"))
    he = re.search(r"(\d+) מתוך (\d+) PRs", section(read("README.he.md"), "## מפת דרכים"))
    assert en and he and en.groups() == he.groups()


def test_play_links_the_itch_build_and_running_locally():
    play = section(read("README.md"), "## Play")
    assert "https://eyalgolan.itch.io/statecraft-israel" in play and "BUILDING.md" in play and "TODO" not in read("README.md")


def test_build_section_pins_unity_and_points_to_building():
    build = section(read("README.md"), "## Build from source")
    assert "6000.3.25f1" in build and "unity/ProjectSettings/ProjectVersion.txt" in build and "BUILDING.md" in build


def test_ai_use_uses_itch_categories():
    ai = section(read("README.md"), "## AI use")
    for cat in ("Code", "Graphics", "Text", "Sound"):
        assert f"**{cat}:**" in ai, cat


def test_no_counts_in_building_prose():
    assert "five commands" not in read("BUILDING.md") and "five commands" not in read("README.md")


def test_every_local_link_and_image_exists():
    for rel in ("README.md", "README.he.md", "BUILDING.md", "CONTRIBUTING.md"):
        text = read(rel)
        targets = re.findall(r'\]\(([^)#\s]+)', text) + re.findall(r'(?:src|href)="([^"#]+)"', text)
        local = [t for t in targets if not t.startswith(("http://", "https://", "mailto:"))]
        missing = [t for t in local if not (ROOT / t).exists()]
        assert not missing, (rel, missing)


def test_hebrew_readme_is_rtl_and_names_english_as_authoritative():
    he = read("README.he.md")
    assert he.lstrip().splitlines()[0].startswith("[English](README.md)")
    assert he.count('<div dir="rtl">') >= 2
    assert he.count("<div") == he.count("</div>")
    assert "הנוסח האנגלי" in he and images(he)[:6] == [CLIP] + GRID and CONCEPT in images(he)


def test_images_are_web_sized_and_the_original_stays():
    for p in (ROOT / "docs/images").glob("*.jpg"):
        assert p.stat().st_size < 1_000_000, p.name
    assert (ROOT / "promo/final/03-gameplay-1936.png").is_file()


def test_building_names_contributing_headings_that_exist():
    building, contributing = read("BUILDING.md"), read("CONTRIBUTING.md")
    for heading in ("Rules every change follows", "What CI runs"):
        assert f'"{heading}"' in building and f"## {heading}" in contributing, heading


def test_contributing_links_the_issue_filters_and_the_open_issues():
    text = section(read("README.md"), "## Contributing")
    for url in ("https://github.com/eyalgolan/statecraft-israel/labels/good%20first%20issue",
                "https://github.com/eyalgolan/statecraft-israel/labels/help%20wanted",
                "https://github.com/eyalgolan/statecraft-israel/issues"):
        assert url in text, url


def test_hebrew_readme_keeps_the_english_facts():
    he = read("README.he.md")
    shot_build, shot_date = re.search(r"Real screenshots, main at ([0-9a-f]{7}), (\d{4}-\d{2}-\d{2})[.;]",
                                      read("README.md")).groups()
    for fact in ("15 במאי 1948", "M3", shot_build, shot_date, "M4", "CC BY-SA 4.0", "CC0", "Firaxis", "2K", "1936",
                 "6000.3.25f1", "docs/CONTENT_POLICY.md", "historical_accuracy.yml", "SECURITY.md"):
        assert fact in he, fact
    assert "חינמי ולא מסחרי" in he and "גרסה מסחרית" not in he  # free and non-commercial
    assert "אינו ניטרלי" in he or "לא ניטרלי" in he  # "not neutral"


def test_social_preview_is_1280_by_640_and_small():
    import struct
    data = (ROOT / "docs/images/social-preview.jpg").read_bytes()
    assert len(data) < 1_000_000 and data[:2] == b"\xff\xd8"
    i = 2
    while i < len(data):  # walk JPEG segments to the start-of-frame marker
        marker, size = data[i + 1], struct.unpack(">H", data[i + 2:i + 4])[0]
        if marker in (0xC0, 0xC1, 0xC2):
            h, w = struct.unpack(">HH", data[i + 5:i + 9])
            assert (w, h) == (1280, 640)
            return
        i += 2 + size
    raise AssertionError("no frame header")


def test_readme_has_no_formatting_churn():
    text = read("README.md")
    assert "\n\n\n" not in text, "double blank line"
    assert not re.search(r"\n [A-Za-z<]", text), "continuation line indented by one space"
    for block in re.findall(r"(?:^- .*\n(?:\n)?)+", text, re.M):
        assert "\n\n- " not in block, "loose list: " + block[:40]
    assert "#1 to #13" not in text


def test_hebrew_readme_polish_and_alt_text():
    he = read("README.he.md")
    assert "`content/`" not in he  # the trailing slash jumps to the wrong side in RTL
    for bad in ("מורשה ב־CC", "מצוטטת עם מקור", "מאסיבי", "שערי סקירה", "בזמן אמת בזמן"):
        assert bad not in he, bad
    alts = re.findall(r'alt="([^"]+)"', he)
    assert len(alts) == 2 + len(GRID) and all(re.search(r"[א-ת]", a) for a in alts)


def test_first_open_in_unity_is_in_all_three_files():
    for rel in ("README.md", "BUILDING.md", "README.he.md"):
        text = read(rel)
        for fact in ("Assets/Map.unity", "Packages/manifest.json", "com.ivanmurzak.unity.mcp"):
            assert fact in text, (rel, fact)
    assert "First open in Unity" in read("README.md") and "First open in Unity" in read("BUILDING.md")


def test_hebrew_file_names_in_link_text_are_direction_isolated():
    # GitHub strips <bdi> but keeps <span dir="ltr">.
    he = read("README.he.md")
    assert '[<span dir="ltr">BUILDING.md</span>]' in he and '[<span dir="ltr">CONTRIBUTING.md</span>]' in he
    assert "<bdi>" not in he


MCP_HEADING = "## Working with an AI agent in Unity (Unity-MCP)"


def test_unity_mcp_section_is_in_contributing_and_building_with_the_same_facts():
    for rel in ("CONTRIBUTING.md", "BUILDING.md"):
        text = section(read(rel), MCP_HEADING)
        for fact in ("https://github.com/IvanMurzak/Unity-MCP", "0.93.3", "Packages/manifest.json",
                     "Window → AI Game Developer", "Custom", "held by the project", "Cloud", "sign in",
                     "UNITY_MCP_CONNECTION_MODE", "UserSettings/AI-Game-Developer-Config.json", "Server URL",
                     "derived from your project's path", "http", "token", "Bearer", "Account sign-in",
                     "Keep server running", "Configure", "Generate", "unity/.mcp.json",
                     "no game rules in Unity", "tests first", "sources", "AI assistance",
                     "editor actions", "never expose", "batch mode", "UnityMcpGuard", "player build", "-CI false -GITHUB_ACTIONS false -TF_BUILD false",
                     "GITHUB_ACTIONS", "TF_BUILD", "AI-Game-Developer-UpdateSettings.asset", "UnityMcpGuardTests",
                     "reopen the window and click Start",
                     "ProjectSettings.asset", "UNITY_MCP_READY", "nuget.org", "Not connecting?", "gamedev-mcp-server",
                     "PID", "Unity-MCP.MainWindow.Initialized", "Unity_MCP_SelectedAiAgent"):
            assert fact in text, (rel, fact)
    for rel in ("CONTRIBUTING.md", "BUILDING.md", "README.he.md", "README.md"):
        assert "27766" not in read(rel), rel  # the port is per project; a shared fixed port made clones share a server


def test_hebrew_readme_carries_the_unity_mcp_facts():
    he = section(read("README.he.md"), "## איך משתתפים")
    for fact in ("Unity-MCP", "0.93.3", "Window → AI Game Developer", "Custom", "Cloud", "UNITY_MCP_CONNECTION_MODE",
                 "UserSettings/AI-Game-Developer-Config.json", "http", "token", "Bearer", "Account sign-in",
                 "Keep server running", "Configure", "Generate", "unity/.mcp.json", "ProjectSettings.asset",
                 "UNITY_MCP_READY", "nuget.org", "gamedev-mcp-server", "PID", "batch", "UnityMcpGuard", "-CI false -GITHUB_ACTIONS false -TF_BUILD false",
                 "GITHUB_ACTIONS", "TF_BUILD", "AI-Game-Developer-UpdateSettings.asset", "git checkout -- unity/Assets/Settings",
                 "פתחו את החלון מחדש",
                 "Unity-MCP.MainWindow.Initialized", "Unity_MCP_SelectedAiAgent"):
        assert fact in he, fact
    for idea in ("כללי המשחק", "בדיקות", "מקור", "בינה מלאכותית", "פורט", "נתיב"):
        assert idea in he, idea


def test_damaged_editor_dialog_troubleshooting_is_in_building_and_hebrew():
    for rel in ("BUILDING.md", "README.he.md"):
        text = read(rel)
        for fact in ("Unity is damaged and can't be opened", "Move to Trash", "Cancel", "Unity Hub",
                     "Mac Build Support"):
            assert fact in text, (rel, fact)


def test_playerprefsbool_error_points_at_the_openupm_registry():
    for rel in ("README.md", "BUILDING.md", "README.he.md"):
        text = read(rel)
        for fact in ("PlayerPrefsBool", "https://package.openupm.com"):
            assert fact in text, (rel, fact)


def test_ci_true_troubleshooting_is_in_building_and_hebrew():
    for rel in ("BUILDING.md", "README.he.md"):
        text = read(rel)
        for fact in ("error CS0234", "CI=true", "GITHUB_ACTIONS=true", "-CI false -GITHUB_ACTIONS false -TF_BUILD false", "git checkout -- unity/ProjectSettings/ProjectSettings.asset"):
            assert fact in text, (rel, fact)
