"""Rigs Tal Heuberger's 1948 soldier (#336's figure-soldier-1948-v1, a static A-pose mesh with no rig) to the 67-bone skeleton
their animated C-series Israeli figures share, so the Yiftach and Negev tokens' third figure walks, idles and fires as C06 and
C07 do (#2384; owner, 3 November 2026: "rig Tal's later"; Tal's OK for adapted versions confirmed by the owner the same day).
The original files are never written: the rigged copy is a new model, figure-soldier-1948-v1-rigged, beside them.

How (all in Blender's bpy module, no paid tool):
  1. The soldier's mesh is measured by horizontal cross-sections (one closed ring per limb every centimetre): the crotch,
     armpit, collar and neck heights, the fingertip, and each limb's centre line give the joint positions. The rules are the
     donor's own: C07's joints are read against C07's mesh the same way (where its hip sits above its crotch, where the knee
     divides the leg, where the elbow and wrist divide the arm), and the soldier's joints take the same ratios.
  2. A copy of C07's armature (the same 67 bones, names and hierarchy) is fitted to those joints. Each bone keeps C07's
     roll convention: its axes are C07's turned by the smallest rotation from C07's bone direction to the soldier's.
  3. The mesh is skinned to the fitted armature with Blender's bone-heat weights, in the soldier's own A-pose, so the
     weights are solved on the mesh as Tal made it.
  4. The fitted armature is then posed so every bone has exactly C07's rest orientation, the mesh is baked into that pose,
     and the pose is applied as the new rest pose. The soldier's rig now has C07's bone orientations with the soldier's
     own bone lengths, which is what lets C07's takes (bone-local rotations, plus the hips' small translation) play on
     it unchanged.
  5. C07's idle, walk and fire takes are assigned to the rig and exported with it as one FBX, every frame keyed, no
     keyframe reduction; the other six takes the game never plays are left out.

The result is verified by re-importing the FBX in a second process (--check), which records bones, takes and weight
coverage in provenance.json beside the model, and renders stills of the rest pose and of frames of each take for review.

    uv run --no-project --python 3.13 --with bpy==5.2.2 python -I tools/rig_soldier_1948.py --rig        # writes the FBX and the map copy
    uv run --no-project --python 3.13 --with bpy==5.2.2 python -I tools/rig_soldier_1948.py --check      # re-imports; writes provenance.json and stills
    python3 tools/rig_soldier_1948.py --verify                                                            # stdlib: hashes against provenance.json

One bpy import per process: --rig and --check run separately. Tal's original folders are only read.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

COLLECTION = Path("unity/Assets/Art/Collection")
SOURCE = "figure-soldier-1948-v1"          # Tal's static soldier (#336)
DONOR = "figure-c07-veteran-fighter-v2"    # the animated figure whose skeleton and takes the copy uses (#507)
OUT = "figure-soldier-1948-v1-rigged"
TAKES = ("idle", "walk", "fire")           # the three the token plays (UnitFigureMotion); C07's other six are left out
STEP = 0.01                                # metres between cross-sections
COLLAR_HALF_WIDTH = 0.09                   # the torso ring narrower than this above the shoulders is the neck's base
BONES = 67


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def check(condition: bool, message: str) -> None:
    """A verification failure; unlike a bare assert it also holds under python -O."""
    if not condition:
        raise AssertionError(message)


# ---- cross-sections (pure Python, so the rules are testable without Blender) ----

class Ring:
    """One closed cross-section of the mesh at a height: its extent and centre."""
    __slots__ = ("xmin", "xmax", "ymin", "ymax", "xc", "yc", "n")

    def __init__(self, points):
        xs = [p[0] for p in points]
        ys = [p[1] for p in points]
        self.xmin, self.xmax, self.ymin, self.ymax = min(xs), max(xs), min(ys), max(ys)
        self.xc, self.yc, self.n = sum(xs) / len(xs), sum(ys) / len(ys), len(points)

    @property
    def spans_centre(self) -> bool:
        return self.xmin < 0.0 < self.xmax

    @property
    def half_width(self) -> float:
        return max(-self.xmin, self.xmax)


def welded(verts, polys):
    """The polygons over one index per position: Tripo's meshes are split at every UV and normal seam, and rings chained
    over the split indices would break there."""
    canon: dict[tuple[float, float, float], int] = {}
    index = [canon.setdefault((round(v[0], 5), round(v[1], 5), round(v[2], 5)), i) for i, v in enumerate(verts)]
    return [tuple(index[i] for i in poly) for poly in polys]


def rings_at(verts, polys, z: float) -> list[Ring]:
    """The closed rings where the plane at height z cuts the mesh: every polygon edge crossing the plane gives a point, and
    the two crossings of one polygon are joined, so points chain into one ring per limb (union-find over the edges).
    polys are over welded indices (welded)."""
    points: dict[tuple[int, int], tuple[float, float]] = {}
    parent: dict[tuple[int, int], tuple[int, int]] = {}

    def find(k):
        while parent[k] != k:
            parent[k] = parent[parent[k]]
            k = parent[k]
        return k

    for poly in polys:
        crossing = []
        n = len(poly)
        for i in range(n):
            a, b = poly[i], poly[(i + 1) % n]
            za, zb = verts[a][2], verts[b][2]
            if (za - z) * (zb - z) >= 0.0:
                continue
            key = (a, b) if a < b else (b, a)
            if key not in points:
                t = (z - za) / (zb - za)
                points[key] = (verts[a][0] + t * (verts[b][0] - verts[a][0]), verts[a][1] + t * (verts[b][1] - verts[a][1]))
                parent[key] = key
            crossing.append(key)
        for k in crossing[1:]:
            ra, rb = find(crossing[0]), find(k)
            if ra != rb:
                parent[rb] = ra
    groups: dict[tuple[int, int], list[tuple[float, float]]] = {}
    for k, p in points.items():
        groups.setdefault(find(k), []).append(p)
    return [Ring(g) for g in groups.values()]


class Sections:
    """A standing figure's rings every STEP metres (z up, metres, the figure facing -y as Tal's FBX files import), and the
    landmarks read from them. The figure faces -y, so its left side is +x."""

    def __init__(self, verts, polys):
        self.top = max(v[2] for v in verts)
        self.rows: dict[float, list[Ring]] = {}
        polys = welded(verts, polys)
        z = STEP / 2
        while z < self.top:
            self.rows[round(z, 4)] = rings_at(verts, polys, z)
            z += STEP

    def key(self, z: float) -> float:
        return round(math.floor(z / STEP) * STEP + STEP / 2, 4)

    def torso(self, z: float) -> Ring | None:
        """The ring across the centre line (the torso, or the pelvis above the crotch), or none."""
        rings = [r for r in self.rows.get(self.key(z), []) if r.spans_centre]
        return max(rings, key=lambda r: r.n) if rings else None

    def right(self, z: float) -> list[Ring]:
        """The rings wholly on the +x side (the figure's left), innermost first."""
        return sorted((r for r in self.rows.get(self.key(z), []) if not r.spans_centre and r.xc > 0.0), key=lambda r: r.xc)

    @property
    def arm_x(self) -> float:
        """Below the crotch, a ring beyond this is the hand's: clear of the pelvis's width (the boot straps never reach it)."""
        return self.torso(self.crotch).half_width + 0.05

    def arm(self, z: float) -> Ring | None:
        """The left arm's ring (+x): beside the torso, the outermost ring wholly outside it (belt pouches sit inside its width);
        below the crotch, the outermost ring clear of the legs. None where the arm has merged into the shoulder."""
        torso = self.torso(z)
        def outside(r: Ring) -> bool:
            return r.xmin > torso.xmax - 0.01 if torso is not None else r.xc > self.arm_x
        rings = [r for r in self.right(z) if outside(r)]
        return rings[-1] if rings else None

    def leg(self, z: float) -> Ring | None:
        """The left leg's ring (+x) below the crotch (the largest ring inside the arm's line)."""
        rings = [r for r in self.right(z) if r.xc <= self.arm_x]
        return max(rings, key=lambda r: r.n) if rings and self.torso(z) is None else None

    def heights(self):
        return sorted(self.rows)

    @property
    def crotch(self) -> float:
        return next(z for z in self.heights() if z >= 0.2 and self.torso(z) is not None)

    @property
    def fingertip(self) -> float:
        return next(z for z in self.heights() if z >= 0.1 and self.arm(z) is not None)

    @property
    def armpit(self) -> float:
        return next(z for z in reversed(self.heights()) if self.arm(z) is not None)

    @property
    def collar(self) -> float:
        armpit = self.armpit
        return next(z for z in self.heights() if z > armpit and self.torso(z) is not None and self.torso(z).half_width < COLLAR_HALF_WIDTH)

    @property
    def neck(self) -> float:
        collar = self.collar
        rows = [z for z in self.heights() if collar <= z <= self.top - 0.06 and self.torso(z) is not None]
        return min(rows, key=lambda z: self.torso(z).half_width)

    def foot_length(self) -> float:
        ring = self.leg(0.02)
        return ring.ymax - ring.ymin


# ---- joints ----

def v3(a, b, op):
    return (op(a[0], b[0]), op(a[1], b[1]), op(a[2], b[2]))


def sub(a, b): return v3(a, b, lambda x, y: x - y)
def length(a): return math.sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2])


def frac(value, lo, hi):
    return (value - lo) / (hi - lo)


def derive_joints(fig: Sections, donor: Sections, donor_bones: dict[str, tuple[tuple, tuple]]) -> dict[str, tuple]:
    """The left-side (+x: the figure faces -y) and centre joints of the figure, by the donor's own ratios (its bones read against its sections):
    the torso chain divides crotch-to-collar as the donor's does; the hip sits above the crotch in the donor's proportion
    of the leg; the knee and the ankle divide the leg as the donor's; the shoulder sits below the collar as the donor's
    does; the elbow and the wrist divide shoulder-to-fingertip as the donor's. Where a donor joint is off its
    ring's centre (its hip, knee and ankle, its spine against the torso's depth), the figure's joint keeps that offset.
    Returns armature-space points, metres."""
    d = {name: head for name, (head, _) in donor_bones.items()}
    dc, dk = donor.crotch, donor.collar
    fc, fk = fig.crotch, fig.collar
    out = {}

    def torso_point(name, z_d, z_f):
        """A centre-line joint: the donor's depth offset from its torso ring, at the mapped height."""
        yd = donor.torso(z_d).yc if donor.torso(z_d) else 0.0
        yf = fig.torso(z_f).yc if fig.torso(z_f) else 0.0
        out[name] = (0.0, yf + (d[name][1] - yd), z_f)

    for name in ("Hips", "Spine", "Chest", "UpperChest"):
        torso_point(name, d[name][2], fc + frac(d[name][2], dc, dk) * (fk - fc))
    torso_point("Neck", d["Neck"][2], fk + (d["Neck"][2] - dk))
    torso_point("Head", d["Head"][2], fig.neck + (d["Head"][2] - donor.neck))
    out["HeadTop"] = (0.0, out["Head"][1] + (donor_bones["Head"][1][1] - d["Head"][1]),
                      fig.neck + frac(donor_bones["Head"][1][2], donor.neck, donor.top) * (fig.top - fig.neck))
    # The clavicle root: the donor's distance below its collar, its own x, the torso's depth.
    z_sh = fk + (d["Left_Shoulder"][2] - dk)
    torso_point("Left_Shoulder", d["Left_Shoulder"][2], z_sh)
    out["Left_Shoulder"] = (d["Left_Shoulder"][0], out["Left_Shoulder"][1], z_sh)

    # The shoulder joint: the donor's depth below its collar (the armpit moves with the arm's angle, the collar does not),
    # its share of the shoulders' half-width (read at the ring just above the joint) and the torso's depth with the donor's
    # offset; then the elbow and the wrist divide the drop to the fingertip as the donor's do, each at the centre of the
    # arm's ring there.
    z_sd = d["Left_UpperArm"][2]
    z_sh = fk + (z_sd - dk)
    shoulder = (d["Left_UpperArm"][0] * fig.torso(z_sh + 0.02).half_width / donor.torso(z_sd + 0.02).half_width,
                fig.torso(z_sh).yc + (d["Left_UpperArm"][1] - donor.torso(z_sd).yc), z_sh)
    out["Left_UpperArm"] = shoulder
    tip_ring = fig.arm(fig.fingertip)
    tip = (tip_ring.xc, tip_ring.yc, fig.fingertip)
    donor_tip_z = donor.fingertip
    for name in ("Left_LowerArm", "Left_Hand"):
        t = frac(d[name][2], z_sd, donor_tip_z)
        z = shoulder[2] + t * (tip[2] - shoulder[2])
        ring = fig.arm(z)
        out[name] = (ring.xc, ring.yc, z)
    out["Fingertip"] = tip
    donor_tip_ring = donor.arm(donor_tip_z)
    out["DonorFingertip"] = (donor_tip_ring.xc, donor_tip_ring.yc, donor_tip_z)

    # The leg: the hip above the crotch in the donor's proportion of its crotch height, the knee and ankle dividing it.
    z_hip = fc + (d["Left_UpperLeg"][2] - dc) * (fc / dc)
    z_ankle = d["Left_Foot"][2]
    z_knee = z_ankle + frac(d["Left_LowerLeg"][2], z_ankle, d["Left_UpperLeg"][2]) * (z_hip - z_ankle)
    for name, z, zd in (("Left_UpperLeg", z_hip, dc - STEP), ("Left_LowerLeg", z_knee, d["Left_LowerLeg"][2]), ("Left_Foot", z_ankle, z_ankle)):
        rf = fig.leg(fc - STEP if name == "Left_UpperLeg" else z)
        rd = donor.leg(zd)
        out[name] = (rf.xc * (d[name][0] / rd.xc), rf.yc + (d[name][1] - rd.yc), z)
    return out


def mirror(p):
    return (-p[0], p[1], p[2])


# ---- Blender ----

def mesh_arrays(bpy, obj):
    """The object's evaluated mesh (rest position) as plain vertex and polygon lists in world space."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    verts = [tuple(ev.matrix_world @ v.co) for v in me.vertices]
    polys = [tuple(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    return verts, polys


def load(bpy, path: Path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    return meshes, arms


def bone_table(arm) -> dict[str, tuple[tuple, tuple]]:
    """Each bone's rest head and tail in armature space."""
    return {b.name: (tuple(arm.matrix_world @ b.head_local), tuple(arm.matrix_world @ b.tail_local)) for b in arm.data.bones}


def depth(bone) -> int:
    n = 0
    while bone.parent is not None:
        bone = bone.parent
        n += 1
    return n


def rig(repo: Path, out_dir: Path | None = None, keep_blend: Path | None = None) -> dict:
    """Builds the rigged copy; writes <OUT>.fbx and the colour map into out_dir (the Collection folder by default)."""
    import bpy
    from mathutils import Matrix, Vector
    import slim_collection as privacy   # its scrub blanks machine paths Blender's FBX writer leaves in a file

    src_dir, donor_dir = repo / COLLECTION / SOURCE, repo / COLLECTION / DONOR
    dest = out_dir or repo / COLLECTION / OUT
    dest.mkdir(parents=True, exist_ok=True)

    # The donor: its rig, takes and mesh (for the joint rules); then the soldier into the same scene.
    (donor_mesh,), (donor_arm,) = load(bpy, donor_dir / f"{DONOR}.fbx")
    donor_arm.data.pose_position = "REST"
    bpy.context.view_layer.update()
    donor_sections = Sections(*mesh_arrays(bpy, donor_mesh))
    donor_bones = bone_table(donor_arm)
    donor_axes = {b.name: (donor_arm.matrix_world @ b.matrix_local).to_3x3() for b in donor_arm.data.bones}
    check(len(donor_bones) == BONES, f"{DONOR}: {len(donor_bones)} bones, expected {BONES}")
    bpy.ops.import_scene.fbx(filepath=str(src_dir / f"{SOURCE}.fbx"))
    soldier = next(o for o in bpy.data.objects if o.type == "MESH" and o is not donor_mesh)
    check(not soldier.vertex_groups and not soldier.modifiers and soldier.parent is None, f"{SOURCE}: expected a static mesh with no rig")
    sections = Sections(*mesh_arrays(bpy, soldier))
    report = {"source_height_m": sections.top, "donor_height_m": donor_sections.top,
              "landmarks": {k: getattr(sections, k) for k in ("crotch", "fingertip", "armpit", "collar", "neck")},
              "donor_landmarks": {k: getattr(donor_sections, k) for k in ("crotch", "fingertip", "armpit", "collar", "neck")}}
    check(abs(sections.top - donor_sections.top) < 0.02, "the soldier and C07 are not the same height; the K98k socket assumes they are")

    # 1. Joints. The rules read against C07's own mesh must give back C07's own joints (the pose-independent ones; its
    # shoulder and arm joints are placed by its wider A-pose), so that is logged as a check on the rules.
    own = derive_joints(donor_sections, donor_sections, donor_bones)
    report["donor_self_check_m"] = {k: round(length(sub(own[k], donor_bones[k][0])), 4) for k in own if k in donor_bones}
    joints = derive_joints(sections, donor_sections, donor_bones)
    for name in list(joints):
        if name.startswith("Left_"):
            joints["Right_" + name[5:]] = mirror(joints[name])
    report["joints"] = {k: [round(c, 4) for c in v] for k, v in joints.items()}

    # 2. The armature: a copy of C07's, fitted. Frames per bone: C07's axes turned by the least rotation between directions.
    arm = donor_arm.copy()
    arm.data = donor_arm.data.copy()
    arm.data.pose_position = "POSE"
    arm.animation_data_clear()
    bpy.context.scene.collection.objects.link(arm)
    for o in (donor_mesh, donor_arm):
        bpy.data.objects.remove(o, do_unlink=True)
    for block in (bpy.data.armatures, bpy.data.meshes):
        for item in list(block):
            if item.users == 0:
                block.remove(item)
    arm.name = arm.data.name = "Armature"   # the takes export as "Armature|idle", "Armature|walk", "Armature|fire", as C07's are named
    check(arm.name == "Armature", f"the armature is {arm.name!r}")
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm.data.edit_bones
    for b in eb:
        b.use_connect = False
    # Wrist-to-fingertip, the figure's against the donor's: the hand bone and the fingers scale by it.
    hand_ratio = {}
    for side in ("Right", "Left"):
        flip = (lambda p: p) if side == "Left" else mirror   # the fingertip landmarks are the left hand's
        hand_ratio[side] = (length(sub(flip(joints["Fingertip"]), joints[f"{side}_Hand"]))
                            / length(sub(flip(joints["DonorFingertip"]), donor_bones[f"{side}_Hand"][0])))
    report["hand_ratio"] = hand_ratio

    def place(name, head, tail):
        b = eb[name]
        d_head, d_tail = donor_bones[name]
        d_dir = (Vector(d_tail) - Vector(d_head)).normalized()
        n_dir = (Vector(tail) - Vector(head)).normalized()
        turn = d_dir.rotation_difference(n_dir)
        b.head, b.tail = Vector(head), Vector(tail)
        b.align_roll(turn @ (donor_axes[name] @ Vector((0.0, 0.0, 1.0))))

    def relative(name, parent, scale):
        """A bone placed as the donor's is in its parent's frame, scaled: the fingers in the hand, the eyes in the head."""
        frame = Matrix.Translation(eb[parent].head.copy()) @ eb[parent].matrix.to_3x3().to_4x4()
        d_frame = Matrix.Translation(Vector(donor_bones[parent][0])) @ donor_axes[parent].to_4x4()
        d_head, d_tail = (d_frame.inverted() @ Vector(donor_bones[name][0])) * scale, (d_frame.inverted() @ Vector(donor_bones[name][1])) * scale
        place(name, frame @ d_head, frame @ d_tail)

    chain = ("Hips", "Spine", "Chest", "UpperChest", "Neck", "Head", "HeadTop")
    for a, b in zip(chain, chain[1:]):
        place(a, joints[a], joints[b])
    for side in ("Right", "Left"):
        def S(n, side=side):
            return f"{side}_{n}"
        place(S("Shoulder"), joints[S("Shoulder")], joints[S("UpperArm")])
        place(S("UpperArm"), joints[S("UpperArm")], joints[S("LowerArm")])
        place(S("LowerArm"), joints[S("LowerArm")], joints[S("Hand")])
        wrist = Vector(joints[S("Hand")])
        tip = Vector(joints["Fingertip"] if side == "Left" else mirror(joints["Fingertip"]))
        d_hand = donor_bones[S("Hand")]
        place(S("Hand"), wrist, wrist + (tip - wrist).normalized() * (Vector(d_hand[1]) - Vector(d_hand[0])).length * hand_ratio[side])
        place(S("UpperLeg"), joints[S("UpperLeg")], joints[S("LowerLeg")])
        place(S("LowerLeg"), joints[S("LowerLeg")], joints[S("Foot")])
        foot_scale = sections.foot_length() / donor_sections.foot_length()
        ankle = Vector(joints[S("Foot")])
        d_ankle = Vector(donor_bones[S("Foot")][0])
        foot = {n: tuple(ankle + (Vector(p) - d_ankle) * foot_scale) for n, p in
                ((S("Foot"), donor_bones[S("Foot")][1]), (S("Toes"), donor_bones[S("Toes")][1]), (S("ToesEnd"), donor_bones[S("ToesEnd")][1]))}
        place(S("Foot"), ankle, foot[S("Foot")])
        place(S("Toes"), foot[S("Foot")], foot[S("Toes")])
        place(S("ToesEnd"), foot[S("Toes")], foot[S("ToesEnd")])
    # Bones the rules do not name follow their parent's frame as C07's do (the fingers, the eyes, the neck twist).
    head_scale = (Vector(joints["HeadTop"]) - Vector(joints["Head"])).length / (Vector(donor_bones["Head"][1]) - Vector(donor_bones["Head"][0])).length
    for b in sorted(eb, key=depth):
        if b.name in chain or any(b.name == f"{s}_{n}" for s in ("Right", "Left") for n in ("Shoulder", "UpperArm", "LowerArm", "Hand", "UpperLeg", "LowerLeg", "Foot", "Toes", "ToesEnd")):
            continue
        parent = b.parent.name
        relative(b.name, parent, hand_ratio[parent.split("_")[0]] if parent.endswith("Hand") or "Thumb" in b.name or "Index" in b.name or "Middle" in b.name or "Ring" in b.name or "Pinky" in b.name else head_scale if parent == "Head" else 1.0)
    bpy.ops.object.mode_set(mode="OBJECT")
    fitted = bone_table(arm)
    report["fitted_bones"] = {k: [[round(c, 4) for c in p] for p in v] for k, v in fitted.items()}

    # 3. Weights: bone heat on the mesh as Tal made it.
    bpy.ops.object.select_all(action="DESELECT")
    soldier.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    with bpy.context.temp_override(object=arm, active_object=arm, selected_objects=[soldier, arm], selected_editable_objects=[soldier, arm]):
        bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    check(soldier.parent is arm and any(m.type == "ARMATURE" for m in soldier.modifiers), "the heat skinning did not bind the mesh")
    # Bone heat sees no bone from the small separate shells (the belt pouches, boot straps and buttons are their own
    # closed pieces): each vertex it left without weight takes the weights of the nearest weighted vertex, so a pouch
    # follows the belt beside it. Logged, as a check on how much of the mesh that is.
    unweighted = [v for v in soldier.data.vertices if not any(g.weight > 0.0 for g in v.groups)]
    report["vertices"] = len(soldier.data.vertices)
    report["unweighted_vertices_after_heat"] = len(unweighted)
    check(len(unweighted) < 0.05 * len(soldier.data.vertices), f"{len(unweighted)} vertices got no weight from bone heat")
    if unweighted:
        from mathutils import kdtree
        tree = kdtree.KDTree(len(soldier.data.vertices) - len(unweighted))
        for v in soldier.data.vertices:
            if any(g.weight > 0.0 for g in v.groups):
                tree.insert(v.co, v.index)
        tree.balance()
        for v in unweighted:
            _, nearest, _ = tree.find(v.co)
            for g in soldier.data.vertices[nearest].groups:
                soldier.vertex_groups[g.group].add([v.index], g.weight, "REPLACE")
        report["unweighted_filled_from_nearest_m"] = round(max(tree.find(v.co)[2] for v in unweighted), 4)
    check(not any(not any(g.weight > 0.0 for g in v.groups) for v in soldier.data.vertices), "vertices still without weight")
    # At most four bones per vertex, normalised: what Unity's import keeps (maxBonesPerVertex 4), so the stills show what
    # the game shows.
    bpy.context.view_layer.objects.active = soldier
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    report["max_influences"] = max(sum(1 for g in v.groups if g.weight > 0.0) for v in soldier.data.vertices)

    # 4. Re-pose into C07's rest orientation, bake, apply as rest.
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    for pb in sorted(arm.pose.bones, key=lambda p: depth(p.bone)):
        bpy.context.view_layer.update()
        pb.matrix = Matrix.Translation(pb.head) @ donor_axes[pb.name].to_4x4()
    bpy.context.view_layer.update()
    def miss(pb) -> float:
        a = donor_axes[pb.name].to_quaternion().rotation_difference(pb.matrix.to_quaternion()).angle
        return min(a, 2 * math.pi - a)
    errors = {pb.name: math.degrees(miss(pb)) for pb in arm.pose.bones}
    worst = max(errors.values())
    report["rest_orientation_error_deg"] = worst
    check(worst < 0.01, "bones missed C07's orientation (degrees): " + ", ".join(f"{k} {v:.2f}" for k, v in errors.items() if v > 0.01))
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.objects.active = soldier
    bpy.ops.object.modifier_apply(modifier=next(m.name for m in soldier.modifiers if m.type == "ARMATURE"))
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    mod = soldier.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    rest = bone_table(arm)
    for name, (head, tail) in rest.items():
        d_head, d_tail = donor_bones[name]
        a = (Vector(tail) - Vector(head)).normalized().angle((Vector(d_tail) - Vector(d_head)).normalized())
        check(a < math.radians(0.05), f"{name}: rest direction differs from C07's by {math.degrees(a):.3f} degrees")
    report["rest_bones"] = {k: [[round(c, 4) for c in p] for p in v] for k, v in rest.items()}

    # 5. The takes: C07's idle, walk and fire, by name, on the new rig.
    keep = {}
    for act in list(bpy.data.actions):
        take = act.name.split("|")[-1]
        if take in TAKES and take not in keep:
            act.name = take
            keep[take] = act
        else:
            bpy.data.actions.remove(act)
    check(set(keep) == set(TAKES), f"takes found: {sorted(keep)}")
    arm.animation_data_create()
    arm.animation_data.action = keep["idle"]
    arm.animation_data.action_slot = keep["idle"].slots[0]
    report["takes"] = {t: [int(f) for f in keep[t].frame_range] for t in TAKES}
    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, max(int(a.frame_range[1]) for a in keep.values())
    if keep_blend is not None:
        bpy.ops.wm.save_as_mainfile(filepath=str(keep_blend))

    # Export: no material or image (the prefab uses the .mat beside the model), every bone, every frame keyed.
    soldier.data.materials.clear()
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)
    for im in list(bpy.data.images):
        bpy.data.images.remove(im)
    fbx = dest / f"{OUT}.fbx"
    bpy.ops.object.select_all(action="DESELECT")
    soldier.select_set(True)
    arm.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(fbx), use_selection=True, object_types={"MESH", "ARMATURE"}, add_leaf_bones=False,
        mesh_smooth_type="FACE", use_mesh_modifiers=True, use_tspace=False, use_custom_props=False,
        path_mode="STRIP", embed_textures=False, use_armature_deform_only=False,
        # The metres-to-centimetres unit factor goes into the data, not onto the nodes: with the default (FBX_SCALE_NONE)
        # Unity reads the bones at scale 100 under a root at 0.01, and a weapon parented under the hand bone (UnitWeapons)
        # comes out a hundred times too big and too far. C07's bones are at scale 1.
        apply_scale_options="FBX_SCALE_ALL",
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_step=1.0, bake_anim_simplify_factor=0.0, bake_anim_force_startend_keying=True)
    privacy.scrub(fbx, repo)
    shutil.copyfile(src_dir / f"{SOURCE}_basecolor.jpg", dest / f"{OUT}_basecolor.jpg")
    report["fbx_sha256"] = sha(fbx.read_bytes())
    (dest / "rig-report.json").write_text(json.dumps(report, indent=1) + "\n", encoding="utf-8")
    print("RIGGED", fbx, report["fbx_sha256"])
    return report


def inspect(repo: Path, fbx: Path, stills: Path | None, colour: Path | None) -> dict:
    """Re-imports the rigged FBX (a fresh process) and reports its rig, takes and weights; renders stills when asked."""
    import bpy
    from mathutils import Vector

    (mesh,), (arm,) = load(bpy, fbx)
    bones = bone_table(arm)
    takes = {a.name.split("|")[-1]: [int(f) for f in a.frame_range] for a in bpy.data.actions}
    take_names = sorted(a.name for a in bpy.data.actions)
    groups = {g.name for g in mesh.vertex_groups}
    unweighted = sum(1 for v in mesh.data.vertices if not any(g.weight > 0.001 for g in v.groups))
    influences = [sum(1 for g in v.groups if g.weight > 0.001) for v in mesh.data.vertices]
    weighted_bones = sorted({mesh.vertex_groups[g.group].name for v in mesh.data.vertices for g in v.groups if g.weight > 0.001})
    report = {
        "bones": len(bones), "bone_names": sorted(bones), "hierarchy": {b.name: (b.parent.name if b.parent else None) for b in arm.data.bones},
        "takes": takes, "take_names": take_names, "fps": bpy.context.scene.render.fps, "vertices": len(mesh.data.vertices),
        "triangles": sum(len(p.vertices) - 2 for p in mesh.data.polygons), "vertex_groups": len(groups),
        "unweighted_vertices": unweighted, "max_influences": max(influences), "bones_with_weight": len(weighted_bones),
        "height_m": max((mesh.matrix_world @ Vector(c)).z for c in mesh.bound_box),
        "mesh_object": mesh.name, "armature_object": arm.name,
    }
    if stills is not None:
        stills.mkdir(parents=True, exist_ok=True)
        render_stills(bpy, mesh, arm, stills, colour)
    return report


def render_stills(bpy, mesh, arm, out: Path, colour: Path | None) -> None:
    """Workbench stills: the rest pose and frames of each take, front and three-quarter views, textured when the map is given."""
    import math as m
    scene = bpy.context.scene
    if colour is not None:
        img = bpy.data.images.load(str(colour))
        mat = bpy.data.materials.new("map")
        mat.use_nodes = True
        tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = img
        mat.node_tree.links.new(tex.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
        mesh.data.materials.clear()
        mesh.data.materials.append(mat)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE" if colour is not None else "MATERIAL"
    scene.render.resolution_x, scene.render.resolution_y = 700, 1000
    scene.world = bpy.data.worlds.new("w")
    scene.world.color = (0.5, 0.5, 0.55)
    cam_data = bpy.data.cameras.new("cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 1.2
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    views = {"front": (0, -5, 0), "left": (-3.5, -3.5, -45), "right": (3.5, -3.5, 45), "side": (5, 0, 90)}

    def shoot(tag):
        for name, (x, y, rz) in views.items():
            cam.location = (x, y, 0.5)
            cam.rotation_euler = (m.radians(90), 0, m.radians(rz))
            scene.render.filepath = str(out / f"{tag}-{name}.png")
            bpy.ops.render.render(write_still=True)

    arm.data.pose_position = "REST"
    shoot("rest")
    arm.data.pose_position = "POSE"
    arm.animation_data_create()
    for act in bpy.data.actions:
        take = act.name.split("|")[-1]
        arm.animation_data.action = act
        arm.animation_data.action_slot = act.slots[0]
        first, last = (int(f) for f in act.frame_range)
        frames = {"idle": (first, (first + last) // 2), "walk": (first, first + (last - first) // 4, first + (last - first) // 2, first + 3 * (last - first) // 4),
                  "fire": (first, first + (last - first) // 3, first + 2 * (last - first) // 3, last)}.get(take, (first, last))
        for f in frames:
            scene.frame_set(f)
            shoot(f"{take}-f{f:03d}")


def verify(repo: Path) -> dict:
    """Stdlib: the shipped files are the ones provenance.json records, from the recorded originals."""
    dest = repo / COLLECTION / OUT
    record = json.loads((dest / "provenance.json").read_text(encoding="utf-8"))
    src, donor = repo / COLLECTION / SOURCE, repo / COLLECTION / DONOR
    check(record["inputs"][f"{SOURCE}.fbx"] == sha((src / f"{SOURCE}.fbx").read_bytes()), "the soldier's original FBX is not the recorded input")
    check(record["inputs"][f"{DONOR}.fbx"] == sha((donor / f"{DONOR}.fbx").read_bytes()), "C07's FBX is not the recorded input")
    check(record["files"][f"{OUT}.fbx"] == sha((dest / f"{OUT}.fbx").read_bytes()), "the rigged FBX is not the recorded one")
    check((dest / f"{OUT}_basecolor.jpg").read_bytes() == (src / f"{SOURCE}_basecolor.jpg").read_bytes(), "the colour map is not Tal's byte for byte")
    check(record["files"][f"{OUT}_basecolor.jpg"] == sha((src / f"{SOURCE}_basecolor.jpg").read_bytes()), "the colour map's recorded hash")
    check(record["bones"] == BONES and record["unweighted_vertices"] == 0 and set(record["takes"]) == set(TAKES), "the rig record")
    check(record["creator_credit"] == "Tal Heuberger" and record["licence"] == "CC-BY-NC-ND-4.0", "credit")
    return record


def write_provenance(repo: Path, inspected: dict) -> None:
    dest = repo / COLLECTION / OUT
    report = json.loads((dest / "rig-report.json").read_text(encoding="utf-8"))
    record = {
        "asset": OUT, "creator_credit": "Tal Heuberger", "licence": "CC-BY-NC-ND-4.0",
        "what": f"Tal Heuberger's {SOURCE} (#336, a static mesh) skinned to the 67-bone skeleton of their {DONOR} (#507) and given its "
                f"idle, walk and fire takes, by tools/rig_soldier_1948.py (Blender {inspected['blender']}); the mesh is Tal's, re-posed into C07's rest pose; the colour map is Tal's byte for byte. "
                "Adapted version by the owner's report of Tal's OK (#2384, 3 November 2026). The originals are unchanged.",
        "inputs": {f"{SOURCE}.fbx": sha((repo / COLLECTION / SOURCE / f"{SOURCE}.fbx").read_bytes()),
                   f"{DONOR}.fbx": sha((repo / COLLECTION / DONOR / f"{DONOR}.fbx").read_bytes())},
        "files": {f"{OUT}.fbx": sha((dest / f"{OUT}.fbx").read_bytes()),
                  f"{OUT}_basecolor.jpg": sha((dest / f"{OUT}_basecolor.jpg").read_bytes())},
        "bones": inspected["bones"], "takes": inspected["takes"], "take_names": inspected["take_names"], "fps": inspected["fps"],
        "vertices": inspected["vertices"], "triangles": inspected["triangles"], "height_m": round(inspected["height_m"], 4),
        "unweighted_vertices": inspected["unweighted_vertices"], "max_influences": inspected["max_influences"],
        "bones_with_weight": inspected["bones_with_weight"],
        "landmarks_m": report["landmarks"], "donor_landmarks_m": report["donor_landmarks"],
        "joints_m": {k: v for k, v in report["joints"].items() if not k.startswith("Right_")},
        "donor_self_check_m": report["donor_self_check_m"], "hand_ratio": report["hand_ratio"],
        "unweighted_vertices_after_heat": report["unweighted_vertices_after_heat"],
        "unweighted_filled_from_nearest_m": report.get("unweighted_filled_from_nearest_m", 0.0),
        "rest_orientation_error_deg": report["rest_orientation_error_deg"],
    }
    (dest / "provenance.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    (dest / "rig-report.json").unlink()   # its figures are in the record; the Collection folder holds only the runtime files


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--repo", type=Path, default=ROOT)
    p.add_argument("--rig", action="store_true", help="build the rigged FBX (needs bpy)")
    p.add_argument("--check", action="store_true", help="re-import the rigged FBX, write provenance.json (needs bpy)")
    p.add_argument("--verify", action="store_true", help="hashes against provenance.json (stdlib)")
    p.add_argument("--out", type=Path, help="--rig: write into this folder instead of the Collection; --check: read the FBX there")
    p.add_argument("--stills", type=Path, help="--check: also render stills into this folder")
    p.add_argument("--blend", type=Path, help="--rig: also save the working .blend here")
    a = p.parse_args(argv)
    repo = a.repo.resolve()
    if a.rig:
        rig(repo, a.out, a.blend)
    elif a.check:
        folder = a.out or repo / COLLECTION / OUT
        import bpy
        report = inspect(repo, folder / f"{OUT}.fbx", a.stills, folder / f"{OUT}_basecolor.jpg")
        report["blender"] = bpy.app.version_string
        check(report["bones"] == BONES, f"{report['bones']} bones, expected {BONES}")
        check(set(report["takes"]) == set(TAKES), f"takes: {report['takes']}")
        check(report["unweighted_vertices"] == 0, f"{report['unweighted_vertices']} unweighted vertices")
        print(json.dumps({k: v for k, v in report.items() if k not in ("bone_names", "hierarchy")}, indent=1))
        if a.out is None:
            write_provenance(repo, report)
    else:
        verify(repo)
        print("VERIFIED", OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
