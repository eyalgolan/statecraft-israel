"""Audit ONE shipped building; run with Blender --background --python tools/building_lod_probe.py.

No FBX, material, UV or licence is written. Output is a local technical report only.
The planar candidate preserves UV/material/normal/sharp delimiters and boundary edges.
"""
from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path

ASSET = "b05a-moshava-house-v2"


def assess_planar_reduction(before: int, after: int) -> dict:
    if before <= 0 or after <= 0 or after > before:
        raise ValueError("invalid triangle counts")
    reduction = (before - after) / before
    return {"accepted": reduction >= 0.2, "reduction_fraction": reduction,
            "visual_quality": "unverified", "frame_time_benefit": "unmeasured"}


def main() -> None:
    import bmesh
    import bpy

    repo = Path(__file__).resolve().parents[1]
    source = repo / f"unity/Assets/Art/Settlements/Prototype/Models/{ASSET}/{ASSET}.fbx"
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source))
    obj, = [o for o in bpy.data.objects if o.type == "MESH"]
    mesh = obj.data
    before = sum(len(p.vertices) - 2 for p in mesh.polygons)
    bm = bmesh.new()
    try:
        bm.from_mesh(mesh)
        # Inspection only: do not weld the actual input to the planar experiment.
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-6)
        uv = bm.loops.layers.uv.active
        locked, boundary, seams = set(), 0, 0
        for edge in bm.edges:
            is_boundary = len(edge.link_faces) != 2
            seam = False
            if not is_boundary:
                a, b = edge.link_loops
                seam = any((next(l for l in a.face.loops if l.vert == v)[uv].uv
                            - next(l for l in b.face.loops if l.vert == v)[uv].uv).length > 1e-6
                           for v in edge.verts)
            boundary += is_boundary
            seams += seam
            if is_boundary or seam:
                locked.update(edge.verts)
        report = {"asset": ASSET, "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
                  "blender": bpy.app.version_string, "vertices": len(mesh.vertices),
                  "triangles": before, "uv_layers": len(mesh.uv_layers),
                  "material_slots": len(mesh.materials), "custom_normals": mesh.has_custom_normals,
                  "welded_vertices": len(bm.verts), "boundary_edges": boundary,
                  "uv_seam_edges": seams, "locked_boundary_or_seam_vertices": len(locked)}
    finally:
        bm.free()
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new("boundary_uv_planar_probe", "DECIMATE")
    mod.decimate_type = "DISSOLVE"
    mod.angle_limit = math.radians(5)
    mod.delimit = {"NORMAL", "MATERIAL", "SEAM", "SHARP", "UV"}
    mod.use_dissolve_boundaries = False
    bpy.ops.object.modifier_apply(modifier=mod.name)
    after = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    report["planar_5_degrees"] = {"triangles": after, **assess_planar_reduction(before, after)}
    output = repo / "captures/building-lod/topology.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
