"""Preserve delivered vehicle archives and make an explicitly labelled Unity FBX compatibility export.

Blender --background --factory-startup --python VehicleReviewSources.py -- --batch BATCH --repo REPO
python3 VehicleReviewSources.py --verify --repo REPO
No decimation, welding, pivot rewrite, image re-encoding, or animation reduction.
"""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import struct
import sys
import zipfile

IDS = {13: "generated-1948-13-egyptian-spitfire", 14: "generated-1948-14-syrian-renault-r35", 16: "generated-1948-16-egyptian-dakota-c47"}
ART = Path("unity/Assets/Editor/Generated1948VehicleReview")
SOURCES = Path("asset-sources/generated-1948/vehicles")
sha = lambda data: hashlib.sha256(data).hexdigest()


def glb(blob):
    assert blob[:4] == b"glTF" and struct.unpack_from("<I", blob, 4)[0] == 2
    length, kind = struct.unpack_from("<II", blob, 12)
    assert kind == 0x4E4F534A
    data = json.loads(blob[20:20 + length])
    size, kind = struct.unpack_from("<II", blob, 20 + length)
    assert kind == 0x004E4942
    binary = blob[28 + length:28 + length + size]
    assert len(data["images"]) == 1 and data["images"][0]["mimeType"] == "image/jpeg"
    view = data["bufferViews"][data["images"][0]["bufferView"]]
    start = view.get("byteOffset", 0)
    return data, binary[start:start + view["byteLength"]]


def jpeg_size(blob):
    assert blob[:2] == b"\xff\xd8"
    at = 2
    while at < len(blob):
        assert blob[at] == 255
        marker = blob[at + 1]; at += 2
        size = struct.unpack_from(">H", blob, at)[0]
        if marker in (0xC0, 0xC1, 0xC2):
            height, width = struct.unpack_from(">HH", blob, at + 3)
            return [width, height]
        at += size
    raise ValueError("JPEG dimensions missing")


def verify(repo):
    manifest = json.loads((repo / SOURCES / "sources.json").read_text())
    for item in manifest["items"]:
        archive = repo / SOURCES / item["archive"]
        assert sha(archive.read_bytes()) == item["archive_sha256"]
        with zipfile.ZipFile(archive) as bundle:
            source = bundle.read(item["member"])
        assert sha(source) == item["source_glb_sha256"]
        data, colour = glb(source)
        assert len(data.get("animations", [])) == item["source_animation_count"] == 0
        assert jpeg_size(colour) == item["texture_dimensions"] == [4096, 4096]
        dest = repo / ART / item["id"]
        assert (dest / (item["id"] + "_basecolor.jpg")).read_bytes() == colour
        assert sha(colour) == item["texture_sha256"]
        assert sha((dest / (item["id"] + "-compatibility.fbx")).read_bytes()) == item["compatibility_fbx_sha256"]
        assert not list(dest.glob("*-near.*")) and not list(dest.glob("*-far.*"))
        print("SOURCE VERIFIED", item["id"], item["triangles"], "triangles, exact 4096px map, zero source clips")
    assert len(manifest["items"]) == 3
    print("PASS: all three delivered archive members and colour maps are byte-identical; compatibility exports match provenance")


def convert(repo, batch):
    import bpy
    from mathutils.kdtree import KDTree
    spec = importlib.util.spec_from_file_location("vehicle_privacy_scrub", repo / "tools/slim_collection.py")
    privacy = importlib.util.module_from_spec(spec); spec.loader.exec_module(privacy)
    delivery = json.loads((batch / "delivery/delivery-manifest.json").read_text())
    items = {x["number"]: x for x in json.loads((batch / "FINAL-MANIFEST.json").read_text())["items"]}
    original = repo / SOURCES; original.mkdir(parents=True, exist_ok=True)
    reports = []
    for archive in delivery["archives"]:
        selected = [x for x in archive["files"] if x["number"] in IDS]
        if not selected:
            continue
        source_archive = batch / "delivery" / archive["archive"]
        assert sha(source_archive.read_bytes()) == archive["sha256"]
        shutil.copyfile(source_archive, original / source_archive.name)
        with zipfile.ZipFile(source_archive) as bundle:
            for record in selected:
                number = record["number"]; mid = IDS[number]; item = items[number]
                blob = bundle.read(record["member"])
                assert sha(blob) == record["sha256"] == item["sha256"]
                assert blob == (batch / record["source_relative"]).read_bytes()
                data, colour = glb(blob)
                assert not data.get("skins") and not data.get("animations")
                assert jpeg_size(colour) == [4096, 4096]
                dest = repo / ART / mid; dest.mkdir(parents=True, exist_ok=True)
                (dest / (mid + "_basecolor.jpg")).write_bytes(colour)
                bpy.ops.wm.read_factory_settings(use_empty=True)
                bpy.ops.import_scene.gltf(filepath=str(batch / record["source_relative"]), merge_vertices=False)
                meshes = [o for o in bpy.data.objects if o.type == "MESH"]
                assert len(meshes) == 1 and not bpy.data.actions and not meshes[0].modifiers
                obj = meshes[0]; obj.data.calc_loop_triangles()
                triangles = len(obj.data.loop_triangles)
                assert triangles == item["actual_triangles"]
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
                # External URP material uses the exact extracted map; stripping export material
                # paths prevents authoring-machine paths from entering the compatibility FBX.
                obj.data.materials.clear()
                bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True); bpy.context.view_layer.objects.active = obj
                fbx = dest / (mid + "-compatibility.fbx")
                bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={"MESH"},
                    use_mesh_modifiers=False, add_leaf_bones=False, apply_unit_scale=True,
                    apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                    bake_anim=True, bake_anim_simplify_factor=0.0, path_mode="STRIP", embed_textures=False)
                privacy.scrub(fbx, repo)
                bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=str(fbx))
                rt = [o for o in bpy.data.objects if o.type == "MESH"]
                assert len(rt) == 1 and not bpy.data.actions
                rt = rt[0]; rt.data.calc_loop_triangles(); nearest = []; max_error = 0.0
                for v in rt.data.vertices:
                    _, index, error = tree.find(rt.matrix_world @ v.co)
                    nearest.append(keys[index]); max_error = max(max_error, error)
                assert max_error < 1e-6
                assert Counter(tuple(sorted(nearest[v] for v in t.vertices)) for t in rt.data.loop_triangles) == faces
                uv_error = 0.0
                for t in rt.data.loop_triangles:
                    key = tuple(sorted(nearest[v] for v in t.vertices))
                    actual = sorted((nearest[rt.data.loops[i].vertex_index], tuple(rt.data.uv_layers.active.data[i].uv)) for i in t.loops)
                    options = uvs[key]
                    errors = [max(abs(actual[j][1][k] - candidate[j][1][k]) for j in range(3) for k in range(2)) for candidate in options]
                    best = min(range(len(errors)), key=errors.__getitem__); uv_error = max(uv_error, errors[best]); options.pop(best)
                assert uv_error < 1e-6 and not privacy.oss_policy.binary_home_path_kinds(fbx.read_bytes())
                report = dict(id=mid, number=number, archive=archive["archive"], archive_sha256=archive["sha256"],
                    member=record["member"], source_glb_sha256=sha(blob), geometry_model_id=item["geometry_model_id"],
                    source_animation_count=0, imported_animation_count=0, texture_dimensions=jpeg_size(colour), texture_sha256=sha(colour),
                    compatibility_fbx_sha256=sha(fbx.read_bytes()), triangles=triangles,
                    geometry_max_vertex_error=max_error, uv_max_error=uv_error, triangle_connectivity="identical", blender=bpy.app.version_string,
                    source_format="Delivered textured GLB; no provider FBX exists in these original archives", decimation=False, welding=False,
                    conversion="Standard glTF Y-up / FBX Y-up compatibility export; no pivot, mesh or UV rewrite; all zero source clips retained; map extracted byte-for-byte",
                    historical_status="Candidate paint/markings/subtype are unverified; no historical deployment represented")
                (dest / "provenance.json").write_text(json.dumps(report, indent=2) + "\n"); reports.append(report)
                print("LOSSLESS COMPATIBILITY", mid, triangles, max_error, uv_error, flush=True)
    (original / "sources.json").write_text(json.dumps({"items": sorted(reports, key=lambda x: x["number"])}, indent=2) + "\n")
    verify(repo)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--batch", type=Path)
    parser.add_argument("--verify", action="store_true")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else None)
    if args.verify:
        verify(args.repo)
    else:
        assert args.batch is not None
        convert(args.repo, args.batch)
