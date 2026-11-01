"""The opening's sources and image records (#530, plan PR 10): every paragraph, the closing line too, has sources a person
has checked (a `checked:` date on each), and every photograph has its alt text, credit and the owner's sensitivity review.
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
