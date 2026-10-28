import subprocess
import sys
import urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import pytest

from nightly_state import Refused, Runner


def pytest_addoption(parser):
    parser.addoption("--headed", action="store_true", help="also run the headed-browser checks (opens Chrome for Testing)")


def pytest_configure(config):
    config.addinivalue_line("markers", "headed: needs a real headed browser; runs only with --headed")


def pytest_collection_modifyitems(config, items):
    if config.getoption("--headed"):
        return
    skip = pytest.mark.skip(reason="headed browser check: run with --headed")
    for item in items:
        if "headed" in item.keywords:
            item.add_marker(skip)


class FakeRunner(Runner):
    """A nightly Runner that never starts a process. `add(prefix, code, out, effect)` answers any command that starts
    with `prefix` (first rule added wins): `run` writes `out` to the log, calls `effect(cmd)` and returns `code`."""

    def __init__(self):
        self.rules: list[tuple[tuple, int, str, object]] = []
        self.calls: list[list[str]] = []

    def add(self, prefix, code=0, out="", effect=None):
        self.rules.append((tuple(prefix), code, out, effect))

    def _match(self, cmd):
        self.calls.append(list(cmd))
        for prefix, code, out, effect in self.rules:
            if tuple(cmd[:len(prefix)]) == prefix:
                return code, out, effect
        raise AssertionError(f"unexpected command {cmd}")

    def run(self, cmd, *, cwd, log, deadline, env=None, holder=None):
        code, out, effect = self._match(cmd)
        Path(log).parent.mkdir(parents=True, exist_ok=True)
        Path(log).write_text(out, encoding="utf-8")
        if effect:
            effect(cmd)
        return code

    def capture(self, cmd, *, cwd, timeout=300):
        code, out, effect = self._match(cmd)
        if effect:
            effect(cmd)
        if code != 0:
            raise Refused("command", f"{' '.join(cmd)} exited {code}")
        return out


@pytest.fixture
def no_processes(monkeypatch):
    """Any real process or network call in the test fails it."""
    def boom(*a, **k):
        raise AssertionError("a test started a real process or network call")
    monkeypatch.setattr(subprocess, "Popen", boom)
    monkeypatch.setattr(subprocess, "run", boom)
    monkeypatch.setattr(urllib.request, "urlopen", boom)
