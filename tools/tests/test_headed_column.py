"""tools/headed.py derives the left icon column's click points (HUD round 2, #602) from Statecraft.uss's numbers; this
fails when the USS moves the column and headed.py does not follow (review of #602: Why? was still clicked at the top bar)."""
import importlib.util
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
USS = (ROOT / "unity/Assets/UI/Statecraft.uss").read_text()


def headed():
    spec = importlib.util.spec_from_file_location("headed", ROOT / "tools/headed.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def rule(selector: str) -> str:
    m = re.search(re.escape(selector) + r"\s*\{([^}]*)\}", USS)
    assert m, selector
    return m.group(1)


def px(body: str, prop: str) -> float:
    m = re.search(rf"(?<![-\w]){prop}:\s*([0-9.]+)px", body)
    assert m, prop
    return float(m.group(1))


def test_the_column_click_points_follow_the_uss():
    col, button = rule(".left-column"), rule(".left-column .rail-button")
    left, top, size = px(col, "left"), px(col, "top"), px(button, "height")
    gap = float(re.search(r"margin:\s*0 0 ([0-9.]+)px 0", button).group(1))
    h = headed()
    w, hh = h.WINDOW
    scale = ((w / 1920) * (hh / 1080)) ** 0.5
    for i in range(4):
        assert h.left_column_button(i) == (round((left + size / 2) * scale), round((top + (size + gap) * i + size / 2) * scale))
    assert h.WHY == h.left_column_button(3)
