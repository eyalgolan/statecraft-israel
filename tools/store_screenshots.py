# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow==12.3.0"]
# ///
"""#45: check a proposed Steam screenshot set's count/dimensions, not its contents.

Usage: uv run tools/store_screenshots.py path/to/shot1.png ... path/to/shot5.png
At least five different paths, minimum 1920 x 1080 and exact 16:9.
This cannot establish real gameplay, rights, age suitability or store approval.
Steam source checked 2026-10-11: https://partner.steamgames.com/doc/store/assets/standard
"""
import argparse
from pathlib import Path
from PIL import Image, UnidentifiedImageError


def validate(paths):
    errors, unique = [], set()
    for path in paths:
        p = Path(path)
        try:
            resolved = p.resolve()
        except (OSError, RuntimeError) as exc:
            errors.append(f'{p}: cannot read image ({exc})')
            continue
        if resolved in unique:
            errors.append(f'{p}: duplicate path (does not count as another screenshot)')
            continue
        unique.add(resolved)
        try:
            with Image.open(p) as image:
                width, height = image.size
                image.verify()
        except (OSError, ValueError, UnidentifiedImageError, Image.DecompressionBombError) as exc:
            errors.append(f'{p}: cannot read image ({exc})')
            continue
        if width < 1920 or height < 1080:
            errors.append(f'{p}: {width} x {height}, minimum 1920 x 1080 required')
        if width * 9 != height * 16:
            errors.append(f'{p}: {width} x {height}, 16:9 required')
    if len(unique) < 5:
        errors.append(f'at least 5 distinct screenshot paths required; got {len(unique)}')
    return errors


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('images', nargs='*', type=Path)
    args = parser.parse_args(argv)
    errors = validate(args.images)
    if errors:
        for error in errors:
            print('DIMENSIONS REFUSED: ' + error)
        return 1
    print('DIMENSIONS OK: count and image dimensions only, not gameplay provenance, rights or age suitability')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
