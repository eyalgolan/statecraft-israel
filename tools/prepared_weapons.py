"""Tal Heuberger's prepared weapon models (#2083) for the Israeli unit-token soldier figures (owner, 2 November 2026: "Yes, on the
soldier figures (Recommended)").

Unity has no glTF importer in this project, so each delivered textured GLB gets a labelled lossless FBX compatibility export,
with the recipe of tools/generated_towns.py (#2085): no decimation, welding, pivot, mesh, UV or image rewrite. The base-colour
map is Tal's delivered JPG copied byte for byte (it is the same image the GLB embeds).

    python3 tools/prepared_weapons.py --verify                     # stdlib only: every hash against provenance.json
    python prepared_weapons.py --convert <id>   (with Blender's bpy importable; one weapon per process; writes the FBX,
                                                 the map copy and provenance.json)

Tal's files under asset-sources/prepared-weapons-20261031/ are only read, never written.
"""
from __future__ import annotations

import argparse
from collections import Counter
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from generated_towns import glb, jpeg_size, sha  # noqa: E402  (one GLB reader for both of Tal's batches)

SOURCES = Path("asset-sources/prepared-weapons-20261031")
COLLECTION = Path("unity/Assets/Art/Collection")
# Unity id -> Tal's delivered textured GLB (its _basecolor.jpg sits beside it), from #2083's provenance.json. Only the weapons
# a figure carries are converted (UnitWeapons); the MG34 stays a source until a figure has a sourced use for it.
WEAPONS = {
    "weapon-k98k-v1": "k98k/statecraft-k98k-approved-textured-b48a435f.glb",
    "weapon-sten-v1": "sten/statecraft-sten-approved-textured-bc6b06c8.glb",
}


def check(condition: bool, message: str) -> None:
    """A verification failure; unlike a bare assert it also holds under python -O."""
    if not condition:
        raise AssertionError(message)


def source(repo: Path, mid: str):
    """Tal's GLB bytes, its glTF JSON, and the delivered JPG beside it, checked to be the image the GLB embeds."""
    path = repo / SOURCES / WEAPONS[mid]
    blob = path.read_bytes()
    data, embedded = glb(blob)
    colour = path.with_name(path.stem + "_basecolor.jpg").read_bytes()
    check(colour == embedded, f"{mid}: the delivered JPG is not the image the GLB embeds")
    check(len(data["meshes"]) == 1 and len(data["meshes"][0]["primitives"]) == 1, f"{mid}: not one mesh with one primitive")
    check(not data.get("animations") and not data.get("skins"), f"{mid}: the GLB has clips or a skin")
    return blob, data, colour


def verify(repo: Path) -> None:
    tal = {f["path"]: f["sha256"] for f in json.loads((repo / SOURCES / "provenance.json").read_text(encoding="utf-8"))["files"]}
    for mid, rel in WEAPONS.items():
        blob, _, colour = source(repo, mid)
        check(sha(blob) == tal[rel], f"{mid}: the GLB is not the file #2083's provenance.json records")
        dest = repo / COLLECTION / mid
        record = json.loads((dest / "provenance.json").read_text(encoding="utf-8"))
        check(record["id"] == mid and record["source"] == (SOURCES / rel).as_posix(), f"{mid}: provenance names another source")
        check(sha(blob) == record["source_glb_sha256"], f"{mid}: the GLB hash differs from provenance.json")
        check((dest / f"{mid}_basecolor.jpg").read_bytes() == colour, f"{mid}: the Unity map is not Tal's JPG byte for byte")
        check(sha(colour) == record["texture_sha256"] and jpeg_size(colour) == record["texture_dimensions"], f"{mid}: the map's hash or size differs")
        check(sha((dest / f"{mid}.fbx").read_bytes()) == record["compatibility_fbx_sha256"], f"{mid}: the FBX differs from its recorded export")
        print("VERIFIED", mid, record["triangles"], "triangles", record["texture_dimensions"])
    print(f"PASS: {len(WEAPONS)} weapon GLBs and maps byte-identical to #2083; compatibility exports match provenance")


def convert(repo: Path, mid: str) -> None:
    import bpy  # Blender as a module; never imported by --verify. One weapon per process.
    from mathutils.kdtree import KDTree
    import slim_collection as privacy  # its scrub blanks machine paths Blender's FBX writer leaves in a file

    rel = WEAPONS[mid]
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
        licence="CC-BY-NC-ND-4.0", pull_request=2083, texture_dimensions=jpeg_size(colour), texture_sha256=sha(colour),
        compatibility_fbx_sha256=sha(fbx.read_bytes()), triangles=triangles, geometry_max_vertex_error=max_error,
        uv_max_error=uv_error, triangle_connectivity="identical", blender=bpy.app.version_string, decimation=False, welding=False,
        conversion="Standard glTF Y-up / FBX Y-up compatibility export (Tal's permission of 31 October 2026 covers export "
            "conversions); no pivot, mesh or UV rewrite; no clips in the source; map copied byte for byte from the delivered JPG")
    (dest / "provenance.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print("LOSSLESS COMPATIBILITY", mid, triangles, max_error, uv_error, flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--verify", action="store_true")
    mode.add_argument("--convert", choices=sorted(WEAPONS))
    parser.add_argument("--repo", type=Path, default=ROOT)
    args = parser.parse_args()
    if args.verify:
        verify(args.repo.resolve())
    else:
        convert(args.repo.resolve(), args.convert)
