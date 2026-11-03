"""Tal Heuberger's whole-town models of Tiberias, Safed, Netanya and Ramla (#2085, #1288) for the Unity Collection.

Unity has no glTF importer in this project, so each delivered textured GLB gets a labelled lossless FBX compatibility export,
the way the placed vehicles were made (unity/Assets/Editor/VehicleReviewSources.py): no decimation, welding, pivot, mesh, UV
or image rewrite. The base-colour map is Tal's delivered JPG copied byte for byte (it is the same image the GLB embeds).

    python3 tools/generated_towns.py --verify                      # stdlib only: every hash against provenance.json
    python generated_towns.py --convert   (with Blender's bpy importable; writes the FBX, the map copy and provenance.json)

Tal's files under asset-sources/prepared-towns-20261031/ are only read, never written.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCES = Path("asset-sources/prepared-towns-20261031")
COLLECTION = Path("unity/Assets/Art/Collection")
# Unity id -> Tal's delivered textured GLB (its _basecolor.jpg sits beside it), from #2085's provenance.json.
TOWNS = {
    "tiberias-town-v1": "tiberias/statecraft-tiberias-smartmesh-textured-086ad55f.glb",
    "safed-town-v1": "safed/statecraft-safed-approved-textured-1e2b8e59.glb",
    "netanya-town-v1": "netanya/statecraft-netanya-approved-textured-4350a137.glb",
    "ramla-town-v1": "ramla/statecraft-ramla-approved-textured-aaf2619f.glb",
}
sha = lambda data: hashlib.sha256(data).hexdigest()


def ensure(ok: bool, why: str) -> None:
    """An explicit check, so --verify still checks under python -O (which strips assert statements)."""
    if not ok:
        raise ValueError(why)


def glb(blob: bytes):
    """The glTF JSON and the embedded base-colour JPG of a one-image binary glTF."""
    ensure(blob[:4] == b"glTF" and struct.unpack_from("<I", blob, 4)[0] == 2, "not a version 2 binary glTF")
    length, kind = struct.unpack_from("<II", blob, 12)
    ensure(kind == 0x4E4F534A, "the glTF has no JSON chunk first")
    data = json.loads(blob[20:20 + length])
    size, kind = struct.unpack_from("<II", blob, 20 + length)
    ensure(kind == 0x004E4942, "the glTF has no binary chunk second")
    binary = blob[28 + length:28 + length + size]
    ensure(len(data["images"]) == 1 and data["images"][0]["mimeType"] == "image/jpeg", "the glTF does not embed exactly one JPEG")
    view = data["bufferViews"][data["images"][0]["bufferView"]]
    start = view.get("byteOffset", 0)
    return data, binary[start:start + view["byteLength"]]


def jpeg_size(blob: bytes) -> list[int]:
    ensure(blob[:2] == b"\xff\xd8", "not a JPEG")
    at = 2
    while at < len(blob):
        ensure(blob[at] == 255, "a malformed JPEG marker")
        marker = blob[at + 1]; at += 2
        size = struct.unpack_from(">H", blob, at)[0]
        if marker in (0xC0, 0xC1, 0xC2):
            height, width = struct.unpack_from(">HH", blob, at + 3)
            return [width, height]
        at += size
    raise ValueError("JPEG dimensions missing")


def source(repo: Path, mid: str):
    """Tal's GLB bytes, its glTF JSON, and the delivered JPG beside it, checked to be the image the GLB embeds."""
    path = repo / SOURCES / TOWNS[mid]
    blob = path.read_bytes()
    data, embedded = glb(blob)
    colour = path.with_name(path.stem + "_basecolor.jpg").read_bytes()
    ensure(colour == embedded, f"{mid}: the delivered JPG is not the image the GLB embeds")
    ensure(len(data["meshes"]) == 1 and len(data["meshes"][0]["primitives"]) == 1, f"{mid}: not one mesh with one primitive")
    ensure(not data.get("animations") and not data.get("skins"), f"{mid}: the GLB has clips or skins")
    return blob, data, colour


def verify(repo: Path) -> None:
    for mid, rel in TOWNS.items():
        blob, _, colour = source(repo, mid)
        dest = repo / COLLECTION / mid
        record = json.loads((dest / "provenance.json").read_text(encoding="utf-8"))
        ensure(record["id"] == mid and record["source"] == (SOURCES / rel).as_posix(), f"{mid}: provenance.json names another model or source")
        ensure(sha(blob) == record["source_glb_sha256"], f"{mid}: the GLB is not the one provenance.json records")
        ensure((dest / f"{mid}_basecolor.jpg").read_bytes() == colour, f"{mid}: the Unity map is not Tal's JPG byte for byte")
        ensure(sha(colour) == record["texture_sha256"] and jpeg_size(colour) == record["texture_dimensions"], f"{mid}: the colour map's hash or size differs from provenance.json")
        ensure(sha((dest / f"{mid}.fbx").read_bytes()) == record["compatibility_fbx_sha256"], f"{mid}: the FBX is not the recorded compatibility export")
        print("VERIFIED", mid, record["triangles"], "triangles", record["texture_dimensions"])
    print(f"PASS: {len(TOWNS)} town GLBs and maps byte-identical to #2085; compatibility exports match provenance")


def convert(repo: Path) -> None:
    import bpy  # Blender as a module; never imported by --verify
    from mathutils.kdtree import KDTree
    sys.path.insert(0, str(repo / "tools"))
    import slim_collection as privacy  # its scrub blanks machine paths Blender's FBX writer leaves in a file

    for mid, rel in TOWNS.items():
        blob, data, colour = source(repo, mid)
        dest = repo / COLLECTION / mid; dest.mkdir(parents=True, exist_ok=True)
        (dest / f"{mid}_basecolor.jpg").write_bytes(colour)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(repo / SOURCES / rel), merge_vertices=False)
        meshes = [o for o in bpy.data.objects if o.type == "MESH"]
        assert len(meshes) == 1 and not bpy.data.actions and not meshes[0].modifiers
        obj = meshes[0]; obj.data.calc_loop_triangles()
        triangles = len(obj.data.loop_triangles)
        accessor = data["accessors"][data["meshes"][0]["primitives"][0]["indices"]]
        assert triangles == accessor["count"] // 3, f"{mid}: {triangles} imported, {accessor['count'] // 3} in the GLB"
        points = [obj.matrix_world @ v.co for v in obj.data.vertices]
        tree = KDTree(len(points))
        for i, co in enumerate(points):
            tree.insert(co, i)
        tree.balance()
        keys = [tuple(round(float(c), 7) for c in p) for p in points]
        faces = Counter(tuple(sorted(keys[v] for v in t.vertices)) for t in obj.data.loop_triangles)
        uvs = {}
        for t in obj.data.loop_triangles:
            key = tuple(sorted(keys[v] for v in t.vertices))
            uvs.setdefault(key, []).append(sorted((keys[obj.data.loops[i].vertex_index], tuple(obj.data.uv_layers.active.data[i].uv)) for i in t.loops))
        # The Unity material uses the exact delivered map; no material in the FBX means no authoring-machine texture path in it.
        obj.data.materials.clear()
        bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True); bpy.context.view_layer.objects.active = obj
        fbx = dest / f"{mid}.fbx"
        bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={"MESH"},
            use_mesh_modifiers=False, add_leaf_bones=False, apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
            bake_anim=False, path_mode="STRIP", embed_textures=False)
        privacy.scrub(fbx, repo)
        # Round trip: the exported FBX read back must give the same vertices, triangles and UVs.
        bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=str(fbx))
        rt = [o for o in bpy.data.objects if o.type == "MESH"]
        assert len(rt) == 1 and not bpy.data.actions
        rt = rt[0]; rt.data.calc_loop_triangles(); nearest = []; max_error = 0.0
        for v in rt.data.vertices:
            _, index, error = tree.find(rt.matrix_world @ v.co)
            nearest.append(keys[index]); max_error = max(max_error, error)
        assert max_error < 1e-6, f"{mid}: vertex error {max_error}"
        assert Counter(tuple(sorted(nearest[v] for v in t.vertices)) for t in rt.data.loop_triangles) == faces, mid
        uv_error = 0.0
        for t in rt.data.loop_triangles:
            key = tuple(sorted(nearest[v] for v in t.vertices))
            actual = sorted((nearest[rt.data.loops[i].vertex_index], tuple(rt.data.uv_layers.active.data[i].uv)) for i in t.loops)
            options = uvs[key]
            errors = [max(abs(actual[j][1][k] - c[j][1][k]) for j in range(3) for k in range(2)) for c in options]
            best = min(range(len(errors)), key=errors.__getitem__); uv_error = max(uv_error, errors[best]); options.pop(best)
        assert uv_error < 1e-6, f"{mid}: UV error {uv_error}"
        record = dict(id=mid, source=(SOURCES / rel).as_posix(), source_glb_sha256=sha(blob), creator_credit="Tal Heuberger",
            licence="CC-BY-NC-ND-4.0", pull_request=2085, texture_dimensions=jpeg_size(colour), texture_sha256=sha(colour),
            compatibility_fbx_sha256=sha(fbx.read_bytes()), triangles=triangles, geometry_max_vertex_error=max_error,
            uv_max_error=uv_error, triangle_connectivity="identical", blender=bpy.app.version_string, decimation=False, welding=False,
            conversion="Standard glTF Y-up / FBX Y-up compatibility export (Tal's permission of 31 October 2026 covers export "
                "conversions); no pivot, mesh or UV rewrite; no clips in the source; map copied byte for byte from the delivered JPG")
        (dest / "provenance.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
        print("LOSSLESS COMPATIBILITY", mid, triangles, max_error, uv_error, flush=True)
    verify(repo)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--verify", action="store_true")
    mode.add_argument("--convert", action="store_true")
    parser.add_argument("--repo", type=Path, default=ROOT)
    args = parser.parse_args()
    (verify if args.verify else convert)(args.repo.resolve())
