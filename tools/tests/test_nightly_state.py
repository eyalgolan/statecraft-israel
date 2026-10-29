import json
from datetime import date, datetime, timezone
from pathlib import Path

import pytest

# nightly_state locks with flock; fcntl does not exist on Windows, so the module skips there.
fcntl = pytest.importorskip("fcntl", reason="fcntl is POSIX-only (nightly_state uses flock); not on Windows")

import nightly_state as ns  # noqa: E402
from conftest import FakeRunner  # noqa: E402


def test_version_and_tag_follow_the_spec():
    assert ns.next_version([{"tag_name": "v0.3.1", "draft": False}]) == "0.3.2"
    assert ns.tag_for("0.3.2") == "v0.3.2"


@pytest.mark.parametrize("tags,expected", [
    (["v0.3.9", "v0.3.1"], "0.3.10"),
    (["v0.9.9", "v0.10.0"], "0.10.1"),
    (["v0.03.1"], "0.3.2"),
    (["v0.03.1", "v0.3.1"], "0.3.2"),
    (["v0.3.1", "v0.03.1"], "0.3.2"),
    (["v9.99.99", "v10.12.19"], "10.12.20"),
    (["v0.3.1", "v99.0.0-nightly.20261028", "0.9.9", "v1.2.3-extra", "v9.9.9\n"], "0.3.2"),
])
def test_next_version_compares_only_exact_release_tags_numerically(tags, expected):
    rows = [{"tag_name": tag, "draft": False} for tag in tags]
    rows.append({"tag_name": "v99.99.99", "draft": True})
    assert ns.next_version(rows) == expected


@pytest.mark.parametrize("rows", [[], [{"tag_name": "v0.3.1-nightly.20261028", "draft": False}],
                                   [{"tag_name": "v0.3.1", "draft": True}]])
def test_next_version_refuses_without_a_published_exact_tag(rows):
    with pytest.raises(ns.Refused, match="no published vX.Y.Z release"):
        ns.next_version(rows)


@pytest.mark.parametrize("row", [{}, {"draft": False}, {"tag_name": 3, "draft": False},
                                 {"tag_name": "v0.3.1", "draft": "false"}, None])
def test_next_version_refuses_malformed_release_rows(row):
    with pytest.raises(ns.Refused) as e:
        ns.next_version([row])
    assert e.value.gate == "version"


def test_the_night_is_the_jerusalem_date():
    assert ns.night_of(datetime(2026, 10, 28, 0, 30, tzinfo=timezone.utc)) == date(2026, 10, 28)   # 02:30 IST
    assert ns.night_of(datetime(2026, 10, 27, 21, 59, tzinfo=timezone.utc)) == date(2026, 10, 27)


def test_a_start_after_0330_is_late():
    il = ns.TZ
    assert not ns.late_start(datetime(2026, 10, 28, 3, 0, tzinfo=il), "03:30")
    assert not ns.late_start(datetime(2026, 10, 28, 3, 30, tzinfo=il), "03:30")
    assert ns.late_start(datetime(2026, 10, 28, 3, 31, tzinfo=il), "03:30")


def test_missed_days_are_the_dates_between_runs():
    assert ns.missed_days("2026-10-25", date(2026, 10, 28)) == [date(2026, 10, 26), date(2026, 10, 27)]
    assert ns.missed_days(None, date(2026, 10, 28)) == []


def test_log_line_names_outcome_tag_sha_gate_and_log(tmp_path):
    line = ns.log_line(datetime(2026, 10, 28, 3, 0, tzinfo=ns.TZ), "REFUSED", tag="v0.3.1-nightly.20261028",
                       sha="0123456789abcdef0123", gate="editmode", log=tmp_path / "runs/x")
    assert line == f"2026-10-28 03:00 REFUSED tag=v0.3.1-nightly.20261028 sha=0123456789ab gate=editmode log={tmp_path}/runs/x"
    with pytest.raises(ValueError):
        ns.log_line(datetime.now(ns.TZ), "OK", tag=None, sha=None, gate="-", log=None)


def test_record_appends_to_log_and_handoff(tmp_path):
    ns.record(tmp_path / "LOG.md", tmp_path / "HANDOFF.md", "L1")
    ns.record(tmp_path / "LOG.md", tmp_path / "HANDOFF.md", "L2")
    assert (tmp_path / "LOG.md").read_text().splitlines()[-2:] == ["- L1", "- L2"]
    assert (tmp_path / "HANDOFF.md").read_text().splitlines()[-1] == "- NIGHTLY: L2"


def test_record_starts_a_new_line_after_a_handoff_without_a_final_newline(tmp_path):
    (tmp_path / "HANDOFF.md").write_text("existing")
    ns.record(tmp_path / "LOG.md", tmp_path / "HANDOFF.md", "L1")
    assert (tmp_path / "HANDOFF.md").read_text().splitlines() == ["existing", "- NIGHTLY: L1"]


def test_state_round_trips_and_a_missing_file_is_empty(tmp_path):
    assert ns.load_state(tmp_path / "state.json") == ns.State()
    s = ns.State(last_published=ns.Published("v0.3.1-nightly.20261028", "a" * 40, "0.3.1-nightly.20261028",
                                             {"statecraft-web.zip": 123}, 4200.5),
                 boot_baseline=[1.0, 2.5], last_run_day="2026-10-28", open_draft="v0.3.1-nightly.20261029",
                 site_pending="v0.3.1-nightly.20261028")
    ns.save_state(tmp_path / "state.json", s)
    assert ns.load_state(tmp_path / "state.json") == s
    assert not list(tmp_path.glob("*.tmp*"))


def test_a_corrupt_state_or_config_refuses(tmp_path):
    (tmp_path / "state.json").write_text("{bad")
    (tmp_path / "config.json").write_text('{"itch_enabled": "yes"}')
    with pytest.raises(ns.Refused) as e:
        ns.load_state(tmp_path / "state.json")
    assert e.value.gate == "state"
    with pytest.raises(ns.Refused) as e:
        ns.load_config(tmp_path / "config.json")
    assert e.value.gate == "config"
    (tmp_path / "config.json").write_text('{"surprise": 1}')
    with pytest.raises(ns.Refused):
        ns.load_config(tmp_path / "config.json")
    (tmp_path / "state.json").write_text('{"last_published": {"tag": "t"}}')
    with pytest.raises(ns.Refused):
        ns.load_state(tmp_path / "state.json")
    (tmp_path / "state.json").write_text('{"boot_baseline": "nope"}')
    with pytest.raises(ns.Refused):
        ns.load_state(tmp_path / "state.json")


def test_a_missing_config_gives_the_defaults_and_paths_expand(tmp_path):
    c = ns.load_config(tmp_path / "none.json")
    assert c.repo_slug == "eyalgolan/statecraft-israel" and c.itch_enabled is False and c.min_free_gb == 40
    (tmp_path / "config.json").write_text(json.dumps({"butler": "~/bin/butler", "deadline_hours": 5}))
    c = ns.load_config(tmp_path / "config.json")
    assert c.butler == Path.home() / "bin/butler" and c.deadline_hours == 5


def test_only_enable_itch_flips_the_flag_and_keeps_other_keys(tmp_path):
    p = tmp_path / "config.json"
    p.write_text('{"itch_page_url": "https://x.itch.io/y"}')
    assert ns.load_config(p).itch_enabled is False
    ns.enable_itch(p, tmp_path / "evidence.json")
    c = ns.load_config(p)
    assert c.itch_enabled and c.itch_page_url == "https://x.itch.io/y" and c.itch_evidence.endswith("evidence.json")


def test_notify_quotes_the_message_for_applescript():
    calls = []
    ns.notify("Statecraft nightly", 'gate "x" \\ y', run=lambda a, **k: calls.append(a))
    assert calls[0][:2] == ["osascript", "-e"] and '\\"x\\"' in calls[0][2] and "\\\\ y" in calls[0][2]


def test_the_run_lock_is_exclusive(tmp_path):
    with ns.run_lock(tmp_path) as first:
        with ns.run_lock(tmp_path) as second:
            assert first is True and second is False
    with ns.run_lock(tmp_path) as again:
        assert again is True


def test_stops_carry_outcome_gate_and_detail():
    r = ns.Refused("editmode", "3 failed")
    assert (r.outcome, r.gate, r.detail) == ("REFUSED", "editmode", "3 failed")
    assert (ns.Skipped("no change").outcome, ns.Skipped("no change").gate) == ("SKIPPED", "-")
    assert ns.Missed("asleep").outcome == "MISSED" and isinstance(ns.Missed("x"), ns.Stop)


def test_the_fake_runner_matches_prefixes_and_fails_on_unknown_commands(tmp_path, no_processes):
    fr = FakeRunner()
    fr.add(("git", "fetch"), out="fetched")
    fr.add(("git",), code=1, out="boom")
    log = tmp_path / "x.log"
    assert fr.run(["git", "fetch", "origin"], cwd=tmp_path, log=log, deadline=0.0) == 0
    assert log.read_text() == "fetched"
    assert fr.capture(["git", "fetch"], cwd=tmp_path) == "fetched"
    with pytest.raises(ns.Refused):
        fr.capture(["git", "status"], cwd=tmp_path)
    with pytest.raises(AssertionError, match="unexpected command"):
        fr.run(["gh", "x"], cwd=tmp_path, log=log, deadline=0.0)
    with pytest.raises(AssertionError):
        __import__("subprocess").run(["true"])


@pytest.mark.parametrize("body", [
    '{"last_run_day": "yesterday"}',
    '{"last_run_day": "2026-13-45"}',
    '{"boot_baseline": [1.0, NaN]}',
    '{"boot_baseline": [Infinity]}',
    '{"last_published": {"tag": "t", "sha": "s", "version": "v", "sizes": {}, "boot_ms": NaN}}',
    '{"last_published": {"tag": "t", "sha": "s", "version": "v", "sizes": {}, "boot_ms": -Infinity}}',
    '{"boot_baseline": [%s]}' % ("9" * 400),
    '{"last_published": {"tag": "t", "sha": "s", "version": "v", "sizes": {}, "boot_ms": %s}}' % ("9" * 400),
])
def test_a_state_value_that_would_fail_later_refuses_now(tmp_path, body):
    (tmp_path / "state.json").write_text(body)
    with pytest.raises(ns.Refused) as e:
        ns.load_state(tmp_path / "state.json")
    assert e.value.gate == "state"


def test_notify_without_run_uses_subprocess_resolved_at_call_time(no_processes):
    with pytest.raises(AssertionError):
        ns.notify("Statecraft nightly", "msg")


def test_notify_default_run_is_looked_up_when_called(monkeypatch):
    import subprocess
    calls = []
    monkeypatch.setattr(subprocess, "run", lambda a, **k: calls.append(a))
    ns.notify("Statecraft nightly", "msg")
    assert calls and calls[0][:2] == ["osascript", "-e"]


def test_old_state_without_ownership_or_recovery_fields_loads(tmp_path):
    # Mutation: require the new fields in state files written before this change.
    path = tmp_path / "state.json"
    path.write_text(json.dumps({"open_draft": "v0.3.2", "site_pending": "0.3.1", "boot_baseline": []}))
    state = ns.load_state(path)
    assert state.open_draft == "v0.3.2" and getattr(state, "open_draft_id", None) is None
    assert state.site_pending == "0.3.1" and getattr(state, "latest_recovery_pending", None) is None


def test_ownership_and_pending_recovery_round_trip(tmp_path):
    # Mutation: omit the ID or pending record from serialization, including a null previous Latest.
    state = ns.State(open_draft="v0.3.2", open_draft_id=101,
                     latest_recovery_pending=ns.LatestRecovery("v0.3.2", None))
    path = tmp_path / "state.json"
    ns.save_state(path, state)
    assert ns.load_state(path) == state


@pytest.mark.parametrize("extra", [{"open_draft_id": True}, {"open_draft_id": "101"}, {"open_draft_id": 0},
                                   {"latest_recovery_pending": {}},
                                   {"latest_recovery_pending": {"failed": 1, "previous": None}},
                                   {"latest_recovery_pending": {"failed": "v0.3.2", "previous": False}}])
def test_invalid_ownership_or_pending_record_refuses(tmp_path, extra):
    # Mutation: accept a malformed ownership ID or recovery target and guess a public action.
    # Value-validation guard: first prove the same keys are supported with valid values.
    path = tmp_path / "state.json"
    valid = {"open_draft_id": 101, "latest_recovery_pending": {"failed": "v0.3.2", "previous": None}}
    path.write_text(json.dumps(valid))
    assert ns.load_state(path).open_draft_id == 101
    path.write_text(json.dumps({**valid, **extra}))
    with pytest.raises(ns.Refused):
        ns.load_state(path)
