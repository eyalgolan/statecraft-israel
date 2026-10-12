"""#45: pin the post-merge review's image measurement and rule attribution."""
from pathlib import Path
import struct
ROOT = Path(__file__).resolve().parents[3]


def jpeg_size(path):
    # SOF dimensions, stdlib only. These committed README JPEGs use baseline/extended frames.
    data = path.read_bytes()
    assert data[:2] == b'\xff\xd8'
    i = 2
    while i < len(data):
        assert data[i] == 0xff
        while data[i] == 0xff:
            i += 1
        marker = data[i]
        i += 1
        if marker in (0xd8, 0xd9) or 0xd0 <= marker <= 0xd7:
            continue
        length = struct.unpack('>H', data[i:i + 2])[0]
        if marker in (0xc0, 0xc1, 0xc2):
            height, width = struct.unpack('>HH', data[i + 3:i + 7])
            return width, height
        i += length
    raise AssertionError('No JPEG dimensions')


def checklist():
    # Whitespace-normalised so the checks do not depend on where the Markdown wraps.
    text = (ROOT / 'docs/RELEASING.md').read_text().split('## Store-page checklist', 1)[1]
    return ' '.join(text.split())


def test_checklist_reports_measured_readme_screenshots():
    sizes = {jpeg_size(ROOT / 'docs/images' / name) for name in (
        'build-m3-map.jpg', 'build-m3-cabinet.jpg', 'build-m3-why.jpg')}
    text = checklist()
    for width, height in sizes:
        assert f'{width} x {height}' in text
    assert 'smaller than this minimum' in text


def test_trailer_recommendation_is_separate_from_project_rule():
    text = checklist()
    assert 'Steam requires a trailer' in text
    assert 'recommends' in text and 'mostly gameplay' in text
    assert 'real build' in text and 'stricter' in text
    assert 'https://partner.steamgames.com/doc/store/trailer' in text
    assert 'still encoding' in text
