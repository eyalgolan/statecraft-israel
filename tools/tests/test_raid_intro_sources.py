"""The New game raid card's sources and clip record (owner, 8 Nov 2026, "15 May dawn raid (Recommended)"): every source
has its url, the claim it supports and the date a person checked it; the Hebrew is complete or absent; the clip names a
file in StreamingAssets/video whose ASSETS.yaml row marks it ai_generated, carries the photographs' licence and credit, is
verified and is listed in docs/ai-disclosure.md; NOTICE credits the photographs. The draft is not shareable: RaidCardContent
(unity/Assets/Scripts/UI/RaidCardContent.cs) shows it only in the Editor until every review below is done.
"""
import copy
from datetime import date
from pathlib import Path

import yaml12

REPO = Path(__file__).resolve().parents[2]
CARD = REPO / "unity/Assets/Resources/RaidIntro/raid-intro.yaml"
VIDEO = REPO / "unity/Assets/StreamingAssets/video"


def actual():
    return yaml12.load(CARD.read_text(encoding="utf-8"))


def assets_rules():
    return yaml12.load((REPO / "ASSETS.yaml").read_text(encoding="utf-8"))["rules"]


def source_problems(doc):
    result = []
    if not doc.get("sources"):
        result.append("sources required")
    for i, source in enumerate(doc.get("sources", []), 1):
        if not source.get("url") or not source.get("supports"):
            result.append(f"source {i}: URL and supported claim required")
        checked = str(source.get("checked", ""))
        try:
            if date.fromisoformat(checked).isoformat() != checked:
                raise ValueError(checked)
        except ValueError:
            result.append(f"source {i}: a person must add checked: YYYY-MM-DD")
    fields = [doc.get("title_he"), doc.get("text_he"), doc.get("he_review")]
    if any(f is not None for f in fields) and not all(isinstance(f, str) and f.strip() for f in fields):
        result.append("the Hebrew comes complete or not at all (title_he, text_he, he_review)")
    return result


def shareable_problems(doc):
    """What a player build still needs (the C# Reviewed rule, restated for the Python checks)."""
    result = []
    if doc.get("status") != "sensitivity-reviewed":
        result.append("the card needs the owner's sensitivity review")
    if doc.get("he_review") != "approved":
        result.append("the Hebrew needs a native read (he_review: approved)")
    if doc.get("clip", {}).get("sensitivity_reviewed") is not True:
        result.append("the clip needs the owner's sensitivity review")
    return result


def clip_problems(doc, rules, disclosure, notice):
    clip = doc.get("clip", {})
    name = clip.get("file", "")
    path = f"unity/Assets/StreamingAssets/video/{name}"
    if not (VIDEO / name).is_file():
        return [f"{name} is not in StreamingAssets/video"]
    row = next((r for r in rules if r.get("glob") == path), None)
    if row is None:
        return [f"{path} has no ASSETS.yaml row"]
    result = []
    if row.get("ai_generated") is not True or row.get("status") != "verified" or row.get("licence") != "CC-BY-4.0":
        result.append(f"{path}: its row needs ai_generated: true, licence: CC-BY-4.0 (the photographs') and status: verified")
    if row.get("credit") != clip.get("credit"):
        result.append(f"{path}: the card's credit line differs from the ASSETS.yaml row's")
    if f"| {path} | ASSETS.yaml |" not in disclosure:
        result.append(f"docs/ai-disclosure.md does not list {path}")
    if f"- {name}:" not in notice:
        result.append(f"NOTICE does not credit {name}")
    return result


def clip_inputs():
    return (assets_rules(), (REPO / "docs/ai-disclosure.md").read_text(encoding="utf-8"),
            (REPO / "NOTICE").read_text(encoding="utf-8"))


def test_the_real_card_has_its_sources_and_hebrew():
    assert source_problems(actual()) == []


def test_the_real_card_is_a_draft_that_is_not_shareable():
    assert actual()["status"] == "draft"
    assert set(shareable_problems(actual())) == {
        "the card needs the owner's sensitivity review",
        "the Hebrew needs a native read (he_review: approved)",
        "the clip needs the owner's sensitivity review"}


def test_a_reviewed_fixture_is_shareable():
    """Only an in-memory fixture: this test never writes review markers to disk."""
    doc = copy.deepcopy(actual())
    doc["status"], doc["he_review"] = "sensitivity-reviewed", "approved"
    doc["clip"]["sensitivity_reviewed"] = True
    assert shareable_problems(doc) == []


def test_missing_impossible_and_short_dates_are_not_checks():
    for bad in (None, "", "2026-02-30", "2026-11-8", "yesterday"):
        doc = copy.deepcopy(actual())
        if bad is None:
            del doc["sources"][0]["checked"]
        else:
            doc["sources"][0]["checked"] = bad
        assert "source 1: a person must add checked: YYYY-MM-DD" in source_problems(doc), bad


def test_a_source_without_its_claim_and_partial_hebrew_fail():
    doc = copy.deepcopy(actual())
    del doc["sources"][1]["supports"]
    del doc["text_he"]
    assert source_problems(doc) == ["source 2: URL and supported claim required",
                                    "the Hebrew comes complete or not at all (title_he, text_he, he_review)"]


def test_the_real_clip_has_its_provenance():
    assert clip_problems(actual(), *clip_inputs()) == []


def test_a_clip_without_its_row_disclosure_or_notice_fails():
    rules, disclosure, notice = clip_inputs()
    path = "unity/Assets/StreamingAssets/video/tlv_raid_intro.mp4"
    assert clip_problems(actual(), [r for r in rules if r.get("glob") != path], disclosure, notice) == \
        [f"{path} has no ASSETS.yaml row"]
    assert clip_problems(actual(), rules, disclosure.replace(f"| {path} | ASSETS.yaml |", ""), notice) == \
        [f"docs/ai-disclosure.md does not list {path}"]
    assert clip_problems(actual(), rules, disclosure, notice.replace("- tlv_raid_intro.mp4:", "")) == \
        ["NOTICE does not credit tlv_raid_intro.mp4"]
    doc = copy.deepcopy(actual())
    doc["clip"]["file"] = "no_such_clip.mp4"
    assert clip_problems(doc, rules, disclosure, notice) == ["no_such_clip.mp4 is not in StreamingAssets/video"]
