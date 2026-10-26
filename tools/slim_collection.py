"""Slims the 31 non-building models of The 1948 Collection (Tal Heuberger's Tripo models from PR #507, issue #664), and #507's
nine whole-town models (issue #663; owner ruling of 17 October 2026: "The nine whole-town models ship"), and the eight
figures offered only in #336 (issue #708; owner ruling of 18 October 2026: "the 8 figures in #708 can be merged without his
license line"), with #886's pioneer farmer and Jerusalem variant from #336 (owner, 19 October 2026: "Yes, both"), into
the files the game ships, so only reduced meshes and compressed colour maps enter the repository
(owner, 15 October 2026: "Plain git, slimmed first"; no masters, no Git LFS).
This converter owns only the original #507/#336 source IDs below. Independently generated infantry use their own
rigging/export pipeline and are not inputs here, even though the runtime catalog streams them in the same bundle.

  uv run --no-project --python 3.13 --with bpy==5.2.2 --with pillow==12.3.0 python -I tools/slim_collection.py \\
      --src <folder holding #507's Models/<id>/ folders> --out unity/Assets/Art/Collection

The source is #507 at 3ef0a7a5a36887aee7a24fd11ddf722507695f3c:
  git fetch origin +pull/507/head:refs/remotes/origin/pr-507
  git archive 3ef0a7a5a36887aee7a24fd11ddf722507695f3c unity/Assets/Art/Settlements/Prototype/Models | tar -x -C <dir>
The ten models of #336 are slimmed from #336 at 1a5482a5864e812c1f5afdf0b31464ac92cbc7fa, with their ids:
  git archive 1a5482a5864e812c1f5afdf0b31464ac92cbc7fa unity/Assets/Art/CommandModels/Models | tar -x -C <dir>

For each model it writes, into <out>/<id>/:
  <id>-far.fbx    the strategic zoom: one static mesh of at most FAR_TRIS triangles (FAR_TRIS_VEHICLE for vehicles, ships
                  and aircraft, FAR_TRIS_TOWN for a whole town), no rig, no animation (posed at the idle clip's first frame when the model has one,
                  or at the first frame of its POSE_CLIP);
  <id>-near.fbx   the close zoom, at most NEAR_TRIS triangles: for a model with an "idle" clip, the skinned mesh, its rig
                  and that one clip sampled every ANIM_STEP frames (every other clip is dropped); for any other model one
                  static mesh (#507 has those stand still too), posed as the far mesh;
  <id>_basecolor.jpg   the colour map at TEX_PX px (Unity imports it crunched).
A model's parts (a vehicle's wheels) are joined into one mesh, so it draws as one instanced mesh. Decimation is
Blender's collapse decimator, which keeps the vertex weights, so the cut mesh still follows its rig.

No path from the authoring machine may enter an FBX (PR #680 shipped the author's temp folder and account name in all
62). Blender's exporter writes a texture's absolute FileName even with path_mode="STRIP", and the export path itself as
the document URL, so the models are exported with no material or image at all (Unity imports none: the prefabs use the
.mat beside each model), every byte run naming the export path is overwritten with a same-length neutral string, and
the file is refused unless no home, temp or session path is left in it, in UTF-8 or UTF-16 (scripts/oss/oss_policy.py,
the same patterns CI's home-paths check uses). """
from __future__ import annotations

import argparse
import os
import re
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts" / "oss"))
import oss_policy  # noqa: E402  (stdlib only; the home, temp and session path patterns of CI's home-paths check)

FAR_TRIS = 1500          # people and animals at the strategic zoom
FAR_TRIS_VEHICLE = 4500  # hard-surface models lose their shape below this (the collapse stalls on the locomotive at ~4,150)
FAR_TRIS_TOWN = 4500     # a whole town is drawn only at the overview zoom, where it is a landmark some dozens of pixels across
NEAR_TRIS = 8000
ANIM_STEP = 2.0
TEX_PX = 512
JPEG_QUALITY = 88
WELD = 1e-5   # metres in the file's units: vertices this close are one

PEOPLE = [
    "figure-c01-arriving-man-v2", "figure-c02-townsman-v2", "figure-c03-smith-carpenter-v2", "figure-c04-sabra-pioneer-v3",
    "figure-c05-pioneer-veteran-v2", "figure-c06-young-recruit-v2", "figure-c07-veteran-fighter-v2",
    "figure-c08-arab-legion-v2", "figure-c09-irregular-v2", "figure-c10-woman-blue-dress-v2",
    "figure-c12f-palmach-scout-v2", "figure-c13-nurse-v2",
    # the eight figures of #336 (issue #708); the soldier has no rig, so both his meshes are static
    "figure-arab-villager-v1", "figure-builder-1950-v1", "figure-elder-immigrant-v1", "figure-immigrant-child-v1",
    "figure-immigrant-woman-v1", "figure-kibbutz-woman-v1", "figure-soldier-1948-v1", "figure-yemenite-immigrant-v1",
    # #886's pioneer farmer from #336: a T-pose at rest, so his static far mesh stands at his idle clip's first frame
    "figure-pioneer-farmer-v1-tpose",
]
VEHICLES = [
    "figure-vehicle-willys-jeep-1948-v2", "figure-vehicle-sandwich-convoy-1948-v2", "figure-vehicle-napoleonchik-65mm-1948-v2",
    "figure-vehicle-intercity-bus-1948-v2", "figure-vehicle-horse-wagon-1948-v2", "figure-vehicle-farm-tractor-1948-v2",
    "figure-vehicle-citrus-truck-1948-v2", "figure-vehicle-canvas-lorry-1948-v2", "vehicle-steam-locomotive-1948-v2",
    "vehicle-immigrant-steamer-1948-v2wl", "vehicle-fishing-boat-1948-v2wl", "vehicle-altalena-lst-1948-v2wl",
    "figure-vehicle-c54-skymaster-1948-v2",
]
ANIMALS = [
    "figure-donkey-1948-v1", "figure-draught-horse-idle-1948-v2", "figure-dromedary-camel-1948-v1",
    "figure-nubian-ibex-1948-v1", "figure-sheep-idle-1948-v1", "figure-striped-hyena-1948-v1",
]
# The nine whole-town models of #507 (the same files as #336's), each one static mesh, and #336's Jerusalem variant
# (#886), an extra beside #507's Jerusalem.
TOWNS = [
    "tel-aviv-white-city-v1", "jerusalem-stone-city-v1", "haifa-port-city-v1", "moshava-town-v1", "nahalal-moshav-v1",
    "kibbutz-compound-v2", "stone-village-v1", "maabara-camp-v1", "shikun-housing-v1",
    "jerusalem-stone-city-v1-3denhance",
]
IDS = PEOPLE + VEHICLES + ANIMALS + TOWNS
# A model with no "idle" clip whose rest pose is a T-pose has its static meshes posed at the first frame of this clip of its
# own instead (issue #665): C05's only clips are a walk and a run, and the walk's first frame stands with the arms down,
# mid-step. Without it he stood on the army tokens with his arms straight out.
POSE_CLIP = {"figure-c05-pioneer-veteran-v2": "idle_walk"}


def tris(mesh) -> int:
    return sum(len(p.vertices) - 2 for p in mesh.polygons)


def decimate(bpy, obj, target: int) -> None:
    """Cuts obj's mesh to at most target triangles with the collapse decimator (vertex weights interpolate)."""
    bpy.context.view_layer.objects.active = obj
    # Tripo's meshes are split at every UV and normal seam, and the collapse alone tears them apart there; welding the
    # coincident vertices first keeps the surface closed (the UVs and weights live on the loops and survive).
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=WELD)
    bpy.ops.object.mode_set(mode="OBJECT")
    for _ in range(12):   # the collapse ratio is approximate, and seams and thin parts resist it: repeat until the cap holds
        n = tris(obj.data)
        if n <= target:
            return
        mod = obj.modifiers.new("slim", "DECIMATE")
        mod.decimate_type = "COLLAPSE"
        mod.ratio = target / n * 0.9
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_move_to_index(modifier=mod.name, index=0)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    raise SystemExit(f"{obj.name}: {tris(obj.data)} triangles after decimation, cap {target}")


def load(bpy, fbx: Path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(fbx))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    return meshes, arms


def join_static(bpy, meshes, arms, idle):
    """Every mesh baked to one pose (the idle clip's first frame when there is one, else the rest pose), transforms
    applied, and joined into one object with no rig or animation."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in bpy.data.objects:
        if not (idle is not None and o in arms):
            o.animation_data_clear()
    if idle is not None:
        arms[0].animation_data_create()
        arms[0].animation_data.action = idle
        bpy.context.scene.frame_set(int(idle.frame_range[0]))
    else:
        for arm in arms:
            arm.data.pose_position = "REST"
    for m in meshes:
        bpy.context.view_layer.objects.active = m
        for mod in list(m.modifiers):
            if mod.type == "ARMATURE":
                bpy.ops.object.modifier_apply(modifier=mod.name)   # rest pose: no action is active
        m.vertex_groups.clear()
    for o in bpy.data.objects:
        o.animation_data_clear()
    for m in meshes:
        m.select_set(True)
        world = m.matrix_world.copy()
        m.parent = None
        m.matrix_world = world
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    for o in list(bpy.data.objects):
        if o is not obj:
            bpy.data.objects.remove(o, do_unlink=True)
    return obj


def strip_materials(bpy) -> None:
    """Drops every material and image, so the FBX names no texture file (Blender writes its absolute path)."""
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.data.materials.clear()
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)
    for im in list(bpy.data.images):
        bpy.data.images.remove(im)


ENCODINGS = ("utf-8", "utf-16-le", "utf-16-be")


def local_paths(path: Path, src: Path) -> list[str]:
    """Every machine-specific path an export could carry, longest first: the export file and its folder (as given and
    resolved), the source folder, the working folder, the home folder and the temp folder."""
    found = set()
    for p in (path, path.parent, src, Path.cwd(), Path.home(), Path(tempfile.gettempdir())):
        found |= {os.path.abspath(p), str(p.resolve())}
    return sorted((s for s in found if s != os.sep), key=len, reverse=True)


def scrub(path: Path, src: Path) -> int:
    """Overwrites every machine-specific path in the exported file with same-length underscores (FBX strings are
    length-prefixed, so the file stays valid), then refuses the file, deleting it, if any such path, the user name or
    any home, temp or session path is left, in UTF-8 or UTF-16. Returns the number of runs overwritten."""
    data = path.read_bytes()
    n = 0
    for s in local_paths(path, src):
        for enc in ENCODINGS:
            b = s.encode(enc)
            n += data.count(b)
            data = data.replace(b, "_".encode(enc) * len(s))
    user = os.path.basename(os.path.expanduser("~"))
    left = [s for s in local_paths(path, src) + ([user] if len(user) >= 4 else [])
            if any(s.encode(e) in data for e in ENCODINGS)]
    if left or oss_policy.binary_home_path_kinds(data):
        path.unlink()
        raise SystemExit(f"{path.name}: a local path is still inside after the scrub; the file was removed")
    path.write_bytes(data)
    return n


def export(bpy, path: Path, src: Path, objects, animated: bool) -> None:
    strip_materials(bpy)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True, object_types={"MESH", "ARMATURE"}, add_leaf_bones=False,
        mesh_smooth_type="FACE", use_mesh_modifiers=False, use_tspace=False, use_custom_props=False,
        path_mode="STRIP", embed_textures=False, bake_anim=animated, bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False, bake_anim_step=ANIM_STEP, bake_anim_simplify_factor=1.0,
        bake_anim_force_startend_keying=True, use_armature_deform_only=True)
    scrub(path, src)


def find_idle(bpy, arms):
    """The "idle" clip; #336's FBX files name it with Blender's numbered suffix ("Armature|idle.001")."""
    if not arms:
        return None
    return next((a for a in bpy.data.actions if re.fullmatch(r"idle(\.\d{3})?", a.name.split("|")[-1])), None)


def find_pose(bpy, arms, mid: str):
    """The clip whose first frame poses the static meshes: the idle clip, else the model's POSE_CLIP, else none (rest)."""
    idle = find_idle(bpy, arms)
    if idle is not None or mid not in POSE_CLIP:
        return idle
    pose = next((a for a in bpy.data.actions if a.name.split("|")[-1] == POSE_CLIP[mid]), None) if arms else None
    if pose is None:
        raise SystemExit(f"{mid}: no clip {POSE_CLIP[mid]!r} to pose it")
    return pose


def slim(bpy, src: Path, out: Path, mid: str) -> dict:
    fbx = src / mid / f"{mid}.fbx"
    dest = out / mid
    dest.mkdir(parents=True, exist_ok=True)
    report = {"id": mid}

    meshes, arms = load(bpy, fbx)
    idle = find_idle(bpy, arms)
    if idle is not None:
        if len(meshes) != 1 or len(arms) != 1:
            raise SystemExit(f"{mid}: expected one skinned mesh and one rig, got {len(meshes)} and {len(arms)}")
        for a in list(bpy.data.actions):
            if a is not idle:
                bpy.data.actions.remove(a)
        idle.name = "idle"
        arms[0].animation_data_create()
        arms[0].animation_data.action = idle
        bpy.context.scene.frame_start, bpy.context.scene.frame_end = (int(v) for v in idle.frame_range)
        decimate(bpy, meshes[0], NEAR_TRIS)
        meshes[0].name = mid
        report["near"] = f"{tris(meshes[0].data)}tris+idle"
        export(bpy, dest / f"{mid}-near.fbx", src, [meshes[0], arms[0]], animated=True)
    else:
        near = join_static(bpy, meshes, arms, find_pose(bpy, arms, mid))
        decimate(bpy, near, NEAR_TRIS)
        near.name = mid
        report["near"] = f"{tris(near.data)}tris"
        export(bpy, dest / f"{mid}-near.fbx", src, [near], animated=False)

    meshes, arms = load(bpy, fbx)   # the far mesh starts again from the full mesh
    far = join_static(bpy, meshes, arms, find_pose(bpy, arms, mid))
    decimate(bpy, far, FAR_TRIS_TOWN if mid in TOWNS else FAR_TRIS_VEHICLE if mid in VEHICLES else FAR_TRIS)
    far.name = mid
    report["far"] = f"{tris(far.data)}tris"
    export(bpy, dest / f"{mid}-far.fbx", src, [far], animated=False)
    return report


def colour_map(src: Path, out: Path, mid: str) -> None:
    from PIL import Image

    with Image.open(src / mid / f"{mid}_basecolor.jpg") as im:
        im = im.convert("RGB")
        if max(im.size) > TEX_PX:
            im = im.resize((TEX_PX, TEX_PX * im.height // im.width), Image.Resampling.LANCZOS)
        im.save(out / mid / f"{mid}_basecolor.jpg", quality=JPEG_QUALITY, optimize=True)


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--src", required=True, type=Path)
    p.add_argument("--out", required=True, type=Path)
    p.add_argument("ids", nargs="*", help="model ids (default: all 50)")
    a = p.parse_args(argv)
    import bpy

    for mid in a.ids or IDS:
        if mid not in IDS:
            raise SystemExit(f"unknown id {mid}")
        r = slim(bpy, a.src, a.out, mid)
        colour_map(a.src, a.out, mid)
        print("SLIM", " ".join(f"{k}={v}" for k, v in r.items()), flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
