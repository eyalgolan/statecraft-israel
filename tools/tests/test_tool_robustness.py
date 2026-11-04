"""#2532: a malformed YAML file fails with a one-line error naming the file, in both tools; and the review queue's
QueueError is what main() already prints."""
import importlib.util
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent.parent
for name in ("review_queue", "translate"):
    spec = importlib.util.spec_from_file_location(name, TOOLS / f"{name}.py")
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)

rq = sys.modules["review_queue"]
tr = sys.modules["translate"]


def test_review_queue_scan_names_the_file_on_bad_yaml():
    try:
        rq.scan("events:\n  - id: [unclosed\n", "content/events/bad.yaml")
        raise AssertionError("expected QueueError")
    except rq.QueueError as e:
        assert "content/events/bad.yaml" in str(e)
        assert "line" in str(e)
        assert "Traceback" not in str(e)


def test_review_queue_signed_rows_names_the_file():
    try:
        rq.signed_rows("a: [unclosed\n", "content/governments.yaml")
        raise AssertionError("expected QueueError")
    except rq.QueueError as e:
        assert "content/governments.yaml" in str(e)


def test_review_queue_scan_translations_names_the_file(tmp_path):
    bad = tmp_path / "translations" / "he" / "bad.yaml"
    bad.parent.mkdir(parents=True)
    bad.write_text("flags:\n  - key: [unclosed\n", encoding="utf-8")
    try:
        rq.scan_translations(tmp_path, "translations/he/bad.yaml")
        raise AssertionError("expected QueueError")
    except rq.QueueError as e:
        assert "translations/he/bad.yaml" in str(e)


def test_translate_parse_names_the_file():
    bad = Path("/tmp/some-file-name.yaml")
    bad.write_text("a: [unclosed\n", encoding="utf-8")
    try:
        tr._parse(bad)
        raise AssertionError("expected Refused")
    except tr.Refused as e:
        assert "some-file-name.yaml" in str(e)
    finally:
        bad.unlink()
