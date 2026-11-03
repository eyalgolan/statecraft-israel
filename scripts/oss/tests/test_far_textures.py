import struct
import zlib
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def _paeth(left, up, upper_left):
    estimate = left + up - upper_left
    left_distance = abs(estimate - left)
    up_distance = abs(estimate - up)
    upper_left_distance = abs(estimate - upper_left)
    if left_distance <= up_distance and left_distance <= upper_left_distance:
        return left
    if up_distance <= upper_left_distance:
        return up
    return upper_left


def _decode_png(path):
    data = path.read_bytes()
    assert data.startswith(PNG_SIGNATURE), f"{path}: invalid PNG signature"
    offset = len(PNG_SIGNATURE)
    header = None
    compressed = []
    while offset < len(data):
        assert offset + 12 <= len(data), f"{path}: truncated PNG chunk"
        length = struct.unpack_from(">I", data, offset)[0]
        chunk_type = data[offset + 4:offset + 8]
        end = offset + 12 + length
        assert end <= len(data), f"{path}: truncated {chunk_type!r} chunk"
        chunk = data[offset + 8:offset + 8 + length]
        if chunk_type == b"IHDR":
            assert length == 13 and header is None, f"{path}: invalid IHDR"
            header = struct.unpack(">IIBBBBB", chunk)
        elif chunk_type == b"IDAT":
            compressed.append(chunk)
        elif chunk_type == b"IEND":
            break
        offset = end

    assert header is not None and compressed, f"{path}: missing IHDR or IDAT"
    width, height, bit_depth, colour_type, compression, png_filter, interlace = header
    assert bit_depth == 8 and colour_type in (2, 6), f"{path}: unsupported PNG format"
    assert compression == 0 and png_filter == 0 and interlace == 0, f"{path}: unsupported PNG encoding"
    channels = 3 if colour_type == 2 else 4
    stride = width * channels
    raw = zlib.decompress(b"".join(compressed))
    assert len(raw) == height * (stride + 1), f"{path}: unexpected decompressed size"

    rows = []
    previous = bytearray(stride)
    offset = 0
    for _ in range(height):
        filter_type = raw[offset]
        row = bytearray(raw[offset + 1:offset + 1 + stride])
        offset += stride + 1
        assert filter_type in range(5), f"{path}: unsupported PNG filter type {filter_type}"
        for index in range(stride):
            left = row[index - channels] if index >= channels else 0
            up = previous[index]
            upper_left = previous[index - channels] if index >= channels else 0
            if filter_type == 1:
                row[index] = (row[index] + left) & 0xFF
            elif filter_type == 2:
                row[index] = (row[index] + up) & 0xFF
            elif filter_type == 3:
                row[index] = (row[index] + ((left + up) // 2)) & 0xFF
            elif filter_type == 4:
                row[index] = (row[index] + _paeth(left, up, upper_left)) & 0xFF
        rows.append(row)
        previous = row
    return rows, channels


def test_far_textures_have_no_pure_black_texels():
    files = sorted((ROOT / "unity/Assets").glob("**/*-far.png"))
    assert files, "no committed *-far.png textures found"
    # This must fail on today's 23 committed PNGs: each contains a 512-texel pure-black block from the old atlas padding.
    failures = []
    for path in files:
        rows, channels = _decode_png(path)
        black = sum(pixel[0] == pixel[1] == pixel[2] == 0
                    for row in rows for pixel in (row[index:index + channels] for index in range(0, len(row), channels)))
        if black:
            failures.append(f"{path.relative_to(ROOT)}: {black} pure-black texels")
    assert failures == [], "\n".join(failures)
