"""The opening's sources and image records (#530, plan PR 10): every paragraph, the closing line too, has sources a person
has checked (a `checked:` date on each), and every photograph has its alt text, credit and the owner's sensitivity review.
The Hebrew (owner, 1 Nov 2026, "Translate to Hebrew (Recommended)") is the same rule for a shareable build: every panel and
the closing carry their Hebrew, all of it or none per panel, and `he_review: approved` from a native read; `pending` blocks.
An AI reading and an in-memory fixture are never a person's sign-off.
"""
import copy
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
    for name, paragraph in paragraphs:
        result += hebrew_problems(name, paragraph)
    return result


def hebrew_problems(name, paragraph):
    """A paragraph's Hebrew: present in full (a panel's title_he, text_he and each arrival's country_he; the closing's
    text_he) with he_review, and approved by a native read. Pending, missing or partial Hebrew is not shareable."""
    fields = [paragraph.get("text_he"), paragraph.get("he_review")]
    if name != "closing":
        fields += [paragraph.get("title_he")] + [a.get("country_he") for a in paragraph.get("arrivals", [])]
    if any(f is not None for f in fields) and not all(isinstance(f, str) and f.strip() for f in fields):
        return [f"{name}: the Hebrew comes complete or not at all (title_he, text_he, he_review, each country_he)"]
    review = paragraph.get("he_review")
    if review not in (None, "pending", "approved"):
        return [f"{name}: he_review must be pending or approved"]
    if review != "approved":
        return [f"{name}: the Hebrew needs a native read (he_review: approved)"]
    return []


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
    for paragraph in [*doc["panels"], doc["closing"]]:
        paragraph["he_review"] = "approved"
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


def test_pending_hebrew_blocks_a_shareable_build():
    for i, name in [(0, "europe"), (2, "yemen")]:
        doc = reviewed_fixture()
        doc["panels"][i]["he_review"] = "pending"
        assert problems(doc) == [f"{name}: the Hebrew needs a native read (he_review: approved)"]
    doc = reviewed_fixture()
    doc["closing"]["he_review"] = "pending"
    assert problems(doc) == ["closing: the Hebrew needs a native read (he_review: approved)"]


def test_every_panel_and_the_closing_need_their_hebrew():
    doc = reviewed_fixture()
    for key in ["title_he", "text_he", "he_review"]:
        del doc["panels"][1][key]
    for arrival in doc["panels"][1]["arrivals"]:
        del arrival["country_he"]
    assert problems(doc) == ["arab_countries: the Hebrew needs a native read (he_review: approved)"]
    doc = reviewed_fixture()
    del doc["closing"]["text_he"], doc["closing"]["he_review"]
    assert problems(doc) == ["closing: the Hebrew needs a native read (he_review: approved)"]


def test_partial_hebrew_is_refused():
    partial = "the Hebrew comes complete or not at all (title_he, text_he, he_review, each country_he)"
    for change in [lambda d: d["panels"][0].pop("title_he"), lambda d: d["panels"][2].update(text_he=" "),
                   lambda d: d["panels"][1]["arrivals"][1].pop("country_he"), lambda d: d["panels"][2].pop("he_review")]:
        doc = reviewed_fixture()
        change(doc)
        assert len(problems(doc)) == 1 and problems(doc)[0].endswith(partial), problems(doc)
    doc = reviewed_fixture()
    del doc["closing"]["text_he"]
    assert problems(doc) == ["closing: " + partial]


def test_an_unknown_review_value_is_not_a_review():
    for value in ["reviewed", "agent-checked", "Approved", True]:
        doc = reviewed_fixture()
        doc["panels"][0]["he_review"] = value
        assert problems(doc) != [], value


def test_the_real_hebrew_is_complete_and_has_no_latin_letters():
    """The shipped draft: complete on every panel and the closing (only its review may wait), with no Latin letter."""
    import re
    doc = actual()
    for name, paragraph in [(p["id"], p) for p in doc["panels"]] + [("closing", doc["closing"])]:
        assert hebrew_problems(name, paragraph) in ([], [f"{name}: the Hebrew needs a native read (he_review: approved)"])
        texts = [paragraph["text_he"]] + ([paragraph["title_he"]] + [a["country_he"] for a in paragraph["arrivals"]]
                                          if name != "closing" else [])
        for text in texts:
            assert not re.search("[A-Za-z]", text), (name, text)
            assert re.search("[\u05d0-\u05ea]", text), (name, text)
