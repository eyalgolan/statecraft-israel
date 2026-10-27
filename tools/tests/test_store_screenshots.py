"""#45: dimensions/count checks are not a gameplay-provenance verdict."""
import importlib.util
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[2]


def module():
    spec = importlib.util.spec_from_file_location('store_screenshots', ROOT / 'tools/store_screenshots.py')
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def shots(tmp_path, count=5, size=(1920, 1080)):
    paths = []
    for i in range(count):
        p = tmp_path / f'{i}.png'
        Image.new('RGB', size).save(p)
        paths.append(p)
    return paths


def test_five_widescreen_images_pass_and_four_fail(tmp_path):
    m = module()
    paths = shots(tmp_path)
    assert m.validate(paths) == []
    assert any('at least 5' in e for e in m.validate(paths[:4]))


def test_smaller_readme_shot_and_wrong_ratio_fail(tmp_path):
    m = module()
    paths = shots(tmp_path)
    Image.new('RGB', (1568, 752)).save(paths[0])  # the README build shots (docs/images/build-m3-*.jpg)
    errors = m.validate(paths)
    assert any('1920 x 1080' in e and '0.png' in e for e in errors)
    Image.new('RGB', (1920, 1200)).save(paths[0])
    assert any('16:9' in e for e in m.validate(paths))


def test_duplicate_paths_do_not_meet_count_and_bad_file_is_named(tmp_path):
    m = module()
    paths = shots(tmp_path)
    assert any('duplicate' in e for e in m.validate([paths[0]] * 5))
    paths[0].write_text('not an image')
    assert any('cannot read' in e and '0.png' in e for e in m.validate(paths))


def test_cli_pass_is_explicitly_limited(tmp_path, capsys):
    m = module()
    assert m.main([str(p) for p in shots(tmp_path)]) == 0
    out = capsys.readouterr().out
    assert 'DIMENSIONS OK' in out
    assert 'not gameplay provenance' in out
    assert m.main([]) == 1


def bomb_png(path):
    """A small PNG whose header claims 32000 x 18000 (576 Mpx), over twice Pillow's MAX_IMAGE_PIXELS, so Image.open
    raises DecompressionBombError (not an OSError) before any pixel is decoded."""
    import struct
    import zlib

    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)
    path.write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 32000, 18000, 8, 2, 0, 0, 0))
                     + chunk(b'IDAT', zlib.compress(b'\0')) + chunk(b'IEND', b''))


def test_a_decompression_bomb_is_a_named_refusal_with_the_protection_kept_on(tmp_path):
    m = module()
    paths = shots(tmp_path)
    bomb_png(paths[0])
    errors = m.validate(paths)
    assert len(errors) == 1 and errors[0].startswith(f'{paths[0]}: cannot read image (') and 'exceeds limit' in errors[0]
    assert Image.MAX_IMAGE_PIXELS == 1024 * 1024 * 1024 // 4 // 3   # Pillow's default: refused, the limit is not lifted
    assert m.main([str(p) for p in paths]) == 1


def test_a_symlink_loop_is_a_named_refusal(tmp_path, monkeypatch):
    m = module()
    paths = shots(tmp_path)
    loop = tmp_path / 'loop.png'
    loop.symlink_to(loop.name)
    errors = m.validate(paths + [loop])
    assert len(errors) == 1 and errors[0].startswith(f'{loop}: cannot read image (')
    # Python 3.11 and 3.12 (CI runs 3.12) raise RuntimeError("Symlink loop ...") from Path.resolve() itself; 3.13 and
    # later return the path and Image.open then fails with OSError. Stand in for the older behaviour on any Python.
    real_resolve = Path.resolve

    def resolve(self, strict=False):
        if self == loop:
            raise RuntimeError(f'Symlink loop from {str(loop)!r}')
        return real_resolve(self, strict)
    monkeypatch.setattr(Path, 'resolve', resolve)
    errors = m.validate(paths + [loop])
    assert errors == [f"{loop}: cannot read image (Symlink loop from {str(loop)!r})"]
