"""The opening's sources and image records (#530, plan PR 10): every paragraph, the closing line too, has sources a person
has checked (a `checked:` date on each), and every photograph has its alt text, credit and the owner's sensitivity review.
An AI reading and an in-memory fixture are never a person's sign-off.
"""
import copy
import fnmatch
from datetime import date
from pathlib import Path

import yaml12

REPO = Path(__file__).resolve().parents[2]
OPENING = REPO / "unity/Assets/UI/Opening/opening.yaml"


def problems(doc):
    """Every paragraph (closing included) has its own source checks; photos have separate reviews."""
    result = []
    if doc.get("status") != "sensitivity-reviewed":
        result.append("opening text needs the owner's sensitivity review")
    panels = doc.get("panels", [])
    if [p.get("id") for p in panels] != ["europe", "arab_countries", "yemen"]:
        result.append("the three approved panels are required in order")
    paragraphs = [(p.get("id", "?"), p) for p in panels] + [("closing", doc.get("closing", {}))]
    for name, paragraph in paragraphs:
        if not paragraph.get("text") or not paragraph.get("sources"):
            result.append(f"{name}: text and sources required")
        for i, source in enumerate(paragraph.get("sources", []), 1):
            if not source.get("url") or not source.get("supports"):
                result.append(f"{name} source {i}: URL and supported claim required")
            checked = str(source.get("checked", ""))
            try:
                if date.fromisoformat(checked).isoformat() != checked:
                    raise ValueError(checked)
            except ValueError:
                result.append(f"{name} source {i}: a person must add checked: YYYY-MM-DD")
    for panel in panels:
        image = panel.get("image", {})
        if not image.get("credit") or not image.get("alt") or not image.get("file"):
            result.append(f"{panel.get('id')}: photo, alt text and credit required")
        if image.get("sensitive") is not True or image.get("sensitivity_reviewed") is not True:
            result.append(f"{panel.get('id')}: photo needs the owner's sensitivity review")
    return result


def actual():
    return yaml12.load(OPENING.read_text(encoding="utf-8"))


def reviewed_fixture():
    """Only a deep in-memory fixture: this function never writes review markers to disk."""
    doc = copy.deepcopy(actual())
    doc["status"] = "sensitivity-reviewed"
    for paragraph in [*doc["panels"], doc["closing"]]:
        for source in paragraph["sources"]:
            source["checked"] = "2026-10-22"
    for panel in doc["panels"]:
        panel["image"]["sensitivity_reviewed"] = True
    return doc


def test_the_real_opening_has_every_check():
    assert problems(actual()) == [], "\n".join(problems(actual()))


def test_photo_approval_does_not_approve_draft_text():
    doc = reviewed_fixture()
    assert problems(doc) == []
    doc["status"] = "draft"
    assert "opening text needs the owner's sensitivity review" in problems(doc)
    doc["status"] = "sensitivity-reviewed"
    del doc["closing"]["sources"][0]["checked"]
    assert any("closing source 1" in p for p in problems(doc))


def test_missing_impossible_and_short_dates_are_not_checks():
    for value in ["", "2026-02-30", "20261022"]:
        doc = reviewed_fixture()
        doc["panels"][0]["sources"][0]["checked"] = value
        assert any("europe source 1" in p for p in problems(doc))


def test_photo_credit_and_review_are_checked_separately():
    doc = reviewed_fixture()
    doc["panels"][1]["image"]["credit"] = ""
    doc["panels"][2]["image"]["sensitivity_reviewed"] = False
    result = problems(doc)
    assert any("arab_countries: photo, alt text and credit required" in p for p in result)
    assert any("yemen: photo needs" in p for p in result)


# The panels' illustration clips (owner ruling of 2 November 2026, "B: A + one new Yemen shot (~2,424)"): each `clip:`
# names an existing file in StreamingAssets/video whose ASSETS.yaml row marks it ai_generated, with its licence, source and
# a verified status, and which docs/ai-disclosure.md lists; a clip that is a decision card's loop keeps that row's shot and
# alt text. The C# reader (OpeningContent.Parse) checks the row's own shape; the shareable build gates its review.
VIDEO = REPO / "unity/Assets/StreamingAssets/video"
EVENT_VIDEOS = REPO / "unity/Assets/Resources/EventVideos/event-videos.yaml"


def assets_rules():
    return yaml12.load((REPO / "ASSETS.yaml").read_text(encoding="utf-8"))["rules"]


def clip_problems(doc, rules, disclosure, card_rows, exists=lambda name: (VIDEO / name).is_file()):
    """Every provenance problem with the panels' clips; the first ASSETS.yaml rule whose glob matches a file is its row."""
    result = []
    cards = {row["file"]: row for row in card_rows}
    for panel in doc.get("panels", []):
        clip = panel.get("clip")
        if clip is None:
            continue
        name, where = clip.get("file", ""), f"{panel.get('id')} clip"
        if not exists(name):
            result.append(f"{where}: {name} is not in unity/Assets/StreamingAssets/video")
            continue
        path = f"unity/Assets/StreamingAssets/video/{name}"
        row = next((r for r in rules if fnmatch.fnmatchcase(path, r.get("glob", ""))), None)
        if row is None:
            result.append(f"{where}: {path} has no ASSETS.yaml row")
        elif row.get("ai_generated") is not True or not row.get("licence") or not row.get("source") or row.get("status") != "verified":
            result.append(f"{where}: its ASSETS.yaml row needs ai_generated: true, licence, source and status: verified")
        elif f"| {row['glob']} | ASSETS.yaml |" not in disclosure:
            result.append(f"{where}: docs/ai-disclosure.md does not list {row['glob']}")
        card = cards.get(name)
        if card is not None and (card.get("shot") != clip.get("shot") or card.get("alt") != clip.get("alt")):
            result.append(f"{where}: shot and alt must match its event-videos.yaml row ({card.get('event')})")
    return result


def clip_inputs():
    return (assets_rules(), (REPO / "docs/ai-disclosure.md").read_text(encoding="utf-8"),
            yaml12.load(EVENT_VIDEOS.read_text(encoding="utf-8"))["clips"])


def test_the_real_clips_have_their_provenance():
    doc = actual()
    assert [p["id"] for p in doc["panels"] if p.get("clip")] == ["europe", "arab_countries", "yemen"], "the ruling's two card loops and the new Yemen shot"
    assert clip_problems(doc, *clip_inputs()) == []


def test_an_unknown_clip_file_fails():
    doc = actual()
    doc["panels"][0]["clip"]["file"] = "no_such_clip.mp4"
    assert any("europe clip: no_such_clip.mp4 is not in" in p for p in clip_problems(doc, *clip_inputs()))


def test_a_clip_without_an_ai_generated_row_fails():
    rules, disclosure, cards = clip_inputs()
    rules = [dict(r, ai_generated=False) if r.get("glob", "").endswith("/video/law_of_return.mp4") else r for r in rules]
    assert any("europe clip: its ASSETS.yaml row needs ai_generated" in p for p in clip_problems(actual(), rules, disclosure, cards))
    rules = [r for r in clip_inputs()[0] if not r.get("glob", "").endswith("/video/iraqi_exodus.mp4")]
    assert any(p.startswith("arab_countries clip:") for p in clip_problems(actual(), rules, disclosure, cards)), \
        "without its own row it falls to a broader glob or none"


def test_a_clip_not_in_the_disclosure_fails():
    rules, disclosure, cards = clip_inputs()
    disclosure = disclosure.replace("| unity/Assets/StreamingAssets/video/law_of_return.mp4 | ASSETS.yaml |", "")
    assert any("europe clip: docs/ai-disclosure.md does not list" in p for p in clip_problems(actual(), rules, disclosure, cards))


def test_a_reused_card_clip_keeps_its_shot_and_alt():
    doc = actual()
    doc["panels"][1]["clip"]["shot"] = "A08"
    assert any("arab_countries clip: shot and alt must match" in p for p in clip_problems(doc, *clip_inputs()))


def test_every_clip_has_its_owner_read_caption():
    """Owner, 2 Nov 2026: "Version 1, fixed (Recommended)": one caption line per panel, shown with its clip, its Hebrew
    read by the owner (he_review: approved)."""
    for panel in actual()["panels"]:
        caption = panel.get("caption") or {}
        assert caption.get("text") and caption.get("text_he") and caption.get("he_review") == "approved", panel["id"]
