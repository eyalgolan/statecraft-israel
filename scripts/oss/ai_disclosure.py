#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Generate review text only from explicit AI flags in the two asset manifests."""
from __future__ import annotations
import argparse
from pathlib import Path
import yaml

KINDS = {
    'Images': {'.png', '.jpg', '.jpeg', '.webp', '.gif', '.ico', '.svg', '.tif', '.tiff'},
    'Video': {'.mp4', '.mov', '.webm'},
    'Audio': {'.wav', '.mp3', '.ogg', '.flac'},
    '3D models': {'.fbx', '.obj', '.blend', '.gltf', '.glb'},
}


def read_manifests(root: Path):
    rows = []
    for name in ('ASSETS.yaml', 'ASSETS.portraits.yaml'):
        data = yaml.safe_load((root / name).read_text(encoding='utf-8'))
        if not isinstance(data, dict) or not isinstance(data.get('rules'), list):
            raise ValueError(f'{name}: rules must be a list')
        rows.extend((name, row) for row in data['rules'])
    return rows


def plain(value):
    return str(value).replace('|', '\\|').replace('\n', ' ')


def render(rows):
    groups = {key: [] for key in [*KINDS, 'Other']}
    for manifest, row in rows:
        if not isinstance(row, dict):
            raise ValueError(f'{manifest}: each rule must be a mapping')
        if 'ai_generated' in row and not isinstance(row['ai_generated'], bool):
            raise ValueError(f'{manifest}: ai_generated must be a boolean')
        if row.get('ai_generated') is not True:
            continue
        for field in ('glob', 'source', 'status'):
            if not isinstance(row.get(field), str) or not row[field].strip():
                raise ValueError(f'{manifest}: flagged rule requires {field}')
        suffix = Path(row['glob']).suffix.lower()
        kind = next((k for k, extensions in KINDS.items() if suffix in extensions), 'Other')
        groups[kind].append((row['glob'], manifest, row['source'], row['status']))
    lines = ['# AI-content disclosure (manifest review text)', '',
             'Generated from ASSETS.yaml and ASSETS.portraits.yaml. Only rows explicitly marked ai_generated: true are listed.', '',
             'Nothing is generated live while you play.', '',
             'This is not a licence clearance or a completed store content survey. owner-verify rows still need their recorded review. It does not decide whether AI-assisted script drawings should be flagged, expand globs into files, or infer flags from source wording.', '']
    if not any(groups.values()):
        lines += ['No asset rows are marked ai_generated: true.', '']
    for kind, entries in groups.items():
        if not entries:
            continue
        lines += [f'## {kind}', '', '| Asset pattern | Manifest | Recorded source | Review status |',
                  '| --- | --- | --- | --- |']
        lines += ['| ' + ' | '.join(plain(x) for x in entry) + ' |' for entry in sorted(entries)]
        lines.append('')
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    text = render(read_manifests(args.root))
    if args.output:
        args.output.write_text(text, encoding='utf-8')
    else:
        print(text, end='')


if __name__ == '__main__':
    main()
