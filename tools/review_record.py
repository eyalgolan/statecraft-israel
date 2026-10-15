"""A person's sign-off beside a status (owner, 15 October 2026, #375: "Yes, humans only"): `reviewed_by`, the reviewer's
GitHub handle, and `reviewed_on`, the day, both or neither. The Python side of Statecraft.Sim.ReviewRecord (the same
rules): the tools that read status rows check it, and tools/review_queue.py leaves a signed-off row out of the queue.
Only a person writes these keys; scripts/oss/reviewed_by_guard.py refuses an AI-co-authored commit that touches them."""
from __future__ import annotations

import datetime
import re

FIELDS = ("reviewed_by", "reviewed_on")
# Letters and digits, single hyphens between them, at most 39 characters: never an email, an @-name, a display name or a
# bot account ("name[bot]").
HANDLE = re.compile(r"[A-Za-z0-9](?:[A-Za-z0-9]|-(?=[A-Za-z0-9])){0,38}")
DAY = re.compile(r"\d{4}-\d{2}-\d{2}")


def is_handle(value) -> bool:
    return isinstance(value, str) and HANDLE.fullmatch(value) is not None


def is_day(value) -> bool:
    if isinstance(value, datetime.datetime):
        return False
    if isinstance(value, datetime.date):
        return True
    if not isinstance(value, str) or not DAY.fullmatch(value):
        return False
    try:
        datetime.date.fromisoformat(value)
    except ValueError:
        return False
    return True


def problem(row: dict) -> tuple[str, str] | None:
    """(field, message) for what is wrong with a row's sign-off, or None when it has none or a whole one. A key that is
    present but blank counts as present."""
    by, on = "reviewed_by" in row, "reviewed_on" in row
    if not by and not on:
        return None
    if not on:
        return "reviewed_on", "required with reviewed_by: the day the person signed off (YYYY-MM-DD)"
    if not by:
        return "reviewed_by", "required with reviewed_on: the reviewer's GitHub handle"
    if not is_handle(row["reviewed_by"]):
        return "reviewed_by", "must be the reviewer's GitHub handle (letters, digits and single hyphens; no @, email or display name)"
    if not is_day(row["reviewed_on"]):
        return "reviewed_on", "must be the day of the sign-off, YYYY-MM-DD"
    return None


def signed(row: dict) -> bool:
    """True when the row carries a whole sign-off."""
    return "reviewed_by" in row and problem(row) is None
