"""tools/review_record.py: the shape of a person's sign-off (#375), the Python side of Statecraft.Sim.ReviewRecord."""
import datetime

import pytest

import review_record as rr


@pytest.mark.parametrize("handle, ok", [
    ("eyalgolan", True), ("a", True), ("guy-shoham", True), ("Jeremy-Hoover1", True), ("a" * 39, True),
    ("", False), ("a" * 40, False), ("eyal@example.org", False), ("@eyalgolan", False), ("eyal-", False), ("-eyal", False),
    ("eyal--golan", False), ("eyal_golan", False), ("eyal.golan", False), ("Eyal Golan", False), ("dependabot[bot]", False),
    (None, False), (123, False),
])
def test_the_handle_shape_is_githubs(handle, ok):
    assert rr.is_handle(handle) is ok


@pytest.mark.parametrize("row, field", [
    ({}, None),
    ({"reviewed_by": "eyalgolan", "reviewed_on": "2026-10-15"}, None),
    ({"reviewed_by": "eyalgolan", "reviewed_on": datetime.date(2026, 10, 15)}, None),
    ({"reviewed_by": "eyalgolan"}, "reviewed_on"),
    ({"reviewed_on": "2026-10-15"}, "reviewed_by"),
    ({"reviewed_by": None, "reviewed_on": "2026-10-15"}, "reviewed_by"),
    ({"reviewed_by": "eyalgolan", "reviewed_on": None}, "reviewed_on"),
    ({"reviewed_by": "eyalgolan", "reviewed_on": "2026-13-01"}, "reviewed_on"),
    ({"reviewed_by": "eyalgolan", "reviewed_on": "2026-10-15T10:00"}, "reviewed_on"),
])
def test_problem_names_the_field(row, field):
    got = rr.problem(row)
    assert (got[0] if got else None) == field
