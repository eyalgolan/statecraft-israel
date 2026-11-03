"""Makes the keffiyeh-free copy of Tal Heuberger's C07 veteran fighter (#507's figure-c07-veteran-fighter-v2), the figure the
Israeli tokens draw for the knitted stocking cap (UnitFigures.ByHeadgear), under the owner's ruling of 3 November 2026: "no
israeli soldir should wear a khafia on their neck". The original files are never written: the copy is a new model,
figure-c07-veteran-fighter-v2-nokeffiyeh, beside them, with Tal's colour map byte for byte.

What the original is: one 31,742-vertex mesh in 2,352 UV-chart pieces (every chart on its own vertices), one 67-bone
armature, nine takes. The scarf (two wraps round the neck, a knot, two tails down the chest, fringe) is modelled on top of a
complete neck and sweater: under the upper wrap the neck is whole, under the lower wrap and the knot the sweater has a
hole (the collar ring and a V down the chest), and at the nape and the neck's sides the upper wrap left small gaps.

How (all in Blender's bpy module, no paid tool):
  1. The chart pieces are welded into one surface (coincident vertices only, 1e-5), so holes can be found and closed.
  2. The scarf's faces are found by their texels (bright, unsaturated: the scarf is white with a black pattern, the sweater
     dark olive, the skin tan) inside the neck box, below the chin and beard, which are never taken; the pattern's dark dots
     are the small non-white patches enclosed by the selection. Specks smaller than 40 faces are not scarf.
  3. The scarf is deleted; what it leaves that is not body is removed too: pieces cut loose (the fringe's dark tips), faces
     that stand more than 8 mm out of the surrounding body surface (the knot's underside, flaps), single spikes on the
     hole's rim, and the rim's pinch points, so every hole is a simple loop.
  4. Each hole is closed with Tal's own topology around it: its loop is unwrapped to (angle, height) about the neck's axis,
     triangulated there (the collar ring and chest V are one C-shaped loop; the nape is a crescent), the triangles are
     split twice, and the new vertices are relaxed with the rim fixed and drawn onto the plane of the body faces around them,
     so the V becomes chest and the ring a collar sloping from the neck to the shoulders. No hole is left and no kept
     vertex moves.
  5. Each new face is textured from Tal's map: it takes the texel at the centre of the nearest kept face of its kind (skin
     next to skin, sweater next to sweater), so the map itself is unchanged.
  6. New vertices are skinned from their neighbours: the four nearest of the deleted scarf's vertices and of the rim's,
     weighted by distance, four influences at most. The armature and the nine takes are untouched and exported with the
     mesh, every frame keyed, no keyframe reduction.

The result is verified by re-importing the FBX in a second process (--check), which records bones, takes, weights, the
closed neck and that no face in the neck box carries the scarf's texels, writes provenance.json beside the model and
renders stills for review.

    uv run --no-project --python 3.13 --with bpy==5.2.2 python -I tools/c07_nokeffiyeh.py --make       # writes the FBX and the map copy
    uv run --no-project --python 3.13 --with bpy==5.2.2 python -I tools/c07_nokeffiyeh.py --check      # re-imports; writes provenance.json and stills
    python3 tools/c07_nokeffiyeh.py --verify                                                            # stdlib: hashes against provenance.json

One bpy import per process: --make and --check run separately. Tal's original folder is only read.
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
SOURCE = "figure-c07-veteran-fighter-v2"
OUT = "figure-c07-veteran-fighter-v2-nokeffiyeh"
BONES = 67
TAKES = ("box_01", "defeat_02", "defeat_03", "fire", "hit_to_body_01", "idle", "run", "shoot", "walk")
WELD = 1e-5
# The neck box, in the model's metres (the figure is 1 m tall): the scarf lies between the sweater's chest and the chin.
BOX_Z = (0.58, 0.905)
BOX_X = 0.26
CHIN_Z, CHIN_Y = 0.872, 0.0     # above this height in front of the neck's axis is the chin and beard, never scarf
WHITE_BRIGHTNESS, WHITE_SATURATION = 0.36, 0.13   # a scarf texel: bright and unsaturated
SPECK = 40                      # a selected piece smaller than this is a beard hair or a texel bleed, not the scarf
STICKS_OUT_M, STICKS_OUT_DEG = 0.005, 35   # a leftover stands this far out of the body, turned this far from it
STICKS_OUT_FLAT_M = 0.008       # or this far out at any angle (the fringe's tags lie along the back)
TEXELS_PER_M = 1000             # about the map's density on the figure, for the new faces' texel patches
PATCH_CELL, PATCH_VARIANCE = 4, 0.006   # the plain patches: cells of this many texels, colour variance under this
NECK_R = 0.07                   # the unwrap's radius: an angle step and a height step of the same metre count
V_FRONT_DEG, V_Z = 40, 0.785     # the chest V: the collar rim's run this close to the front and below this height
SUBDIVISIONS = 2
RELAX = 25
COLLAR_RIB_M = 0.008            # the crew neck's rolled rib, raised along the middle of the closed collar ring
COLLAR_RIB_WIDTH_M = (0.02, 0.035)   # full rib where the ring is this narrow, none where it is this wide
RIM_EASE = 3                    # passes easing the sweater's cut rim along the surface
SKIRT_INSET_M = 0.004           # how far inside the neck an open rim's skirt is tucked
SKIRT_WELD_M = 0.012            # and how far its inner edge may reach to weld onto the neck's own vertices
# Open edges in the neck box that the original does not have, allowed after the fill: the skirts' tucked inner edges and
# slivers where the head's rim meets the neck at the jaw, all under a centimetre and inside or behind the surface in the
# review stills (--check renders them); a scarf-sized hole has hundreds.
OPEN_EDGES_ALLOWED = 60


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def check(condition: bool, message: str) -> None:
    """A verification failure; unlike a bare assert it also holds under python -O."""
    if not condition:
        raise SystemExit("c07_nokeffiyeh: " + message)


def load(bpy, path: Path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    check(len(meshes) == 1 and len(arms) == 1, f"{path.name}: expected one mesh and one armature, found {len(meshes)} and {len(arms)}")
    return meshes[0], arms[0]


class Map:
    """Tal's colour map, sampled at a UV."""

    def __init__(self, bpy, path: Path):
        img = bpy.data.images.load(str(path))
        self.w, self.h = img.size
        self.px = img.pixels[:]

    def at(self, u: float, v: float):
        x = min(self.w - 1, max(0, int((u % 1.0) * self.w)))
        y = min(self.h - 1, max(0, int((v % 1.0) * self.h)))
        i = 4 * (y * self.w + x)
        return self.px[i], self.px[i + 1], self.px[i + 2]


def face_colour(f, uvl, cmap):
    """The mean texel at a face's UV centre and its corners pulled a quarter in (away from the chart's gutter)."""
    from mathutils import Vector
    uvs = [l[uvl].uv for l in f.loops]
    c = sum(uvs, Vector((0, 0))) / len(uvs)
    cols = [cmap.at(p.x, p.y) for p in [c] + [c + (u - c) * 0.75 for u in uvs]]
    return [sum(k[i] for k in cols) / len(cols) for i in range(3)]


def kind(colour) -> str:
    r, g, b = colour
    brightness, saturation = (r + g + b) / 3, max(colour) - min(colour)
    if brightness > WHITE_BRIGHTNESS and saturation < WHITE_SATURATION:
        return "white"
    if saturation > 0.18 and r > g > b:
        return "skin"
    return "cloth"


def components(faces, member):
    """Edge-connected groups of the faces for which member holds."""
    seen, groups = set(), []
    for f in faces:
        if not member(f) or f.index in seen:
            continue
        stack, group = [f], []
        while stack:
            g = stack.pop()
            if g.index in seen or not member(g):
                continue
            seen.add(g.index)
            group.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen and member(h):
                        stack.append(h)
        groups.append(group)
    return groups


def edge_key(e):
    return frozenset((tuple(round(c, 6) for c in e.verts[0].co), tuple(round(c, 6) for c in e.verts[1].co)))


def walk_loops(edges):
    """The boundary edges chained into loops of vertex indices (each loop closed, every vertex on two edges)."""
    from collections import defaultdict
    adj = defaultdict(list)
    for e in edges:
        for v in e.verts:
            adj[v.index].append(e)
    seen, loops = set(), []
    for e in edges:
        if e.index in seen:
            continue
        loop, cur, v = [], e, e.verts[0]
        while cur is not None and cur.index not in seen:
            seen.add(cur.index)
            loop.append(v)
            v = cur.other_vert(v)
            nxt = [x for x in adj[v.index] if x.index not in seen]
            cur = nxt[0] if nxt else None
        loops.append(loop)
    return loops


def plain_patch(bm, uvl, cmap, k: int) -> dict:
    """The largest rectangle of the map covered only by kept faces of kind k (0 cloth, 1 skin) whose texels are plain
    (variance under PATCH_VARIANCE per PATCH_CELL cell, the kind's own tone), inset a cell on each side; as UV (u0, v0, w, h)
    and texels."""
    C = PATCH_CELL
    gw, gh = cmap.w // C, cmap.h // C
    cover = {}
    for f in bm.faces:
        if f[bm.faces.layers.int.get("kind")] not in (0, 1):
            continue
        us = [l[uvl].uv for l in f.loops]
        x0, x1 = int(min(u.x for u in us) * cmap.w) // C, int(max(u.x for u in us) * cmap.w) // C
        y0, y1 = int(min(u.y for u in us) * cmap.h) // C, int(max(u.y for u in us) * cmap.h) // C
        if x1 - x0 > gw / 4 or y1 - y0 > gh / 4:
            continue
        for gx in range(x0, x1 + 1):
            for gy in range(y0, y1 + 1):
                cover.setdefault((gx, gy), set()).add(f[bm.faces.layers.int.get("kind")])
    good = [[False] * gw for _ in range(gh)]
    for (gx, gy), kinds in cover.items():
        if kinds != {k} or gx >= gw or gy >= gh:
            continue
        n, s1, s2 = 0, [0.0, 0.0, 0.0], [0.0, 0.0, 0.0]
        for y in range(gy * C, min(cmap.h, gy * C + C)):
            for x in range(gx * C, min(cmap.w, gx * C + C)):
                i = 4 * (y * cmap.w + x)
                for c in range(3):
                    s1[c] += cmap.px[i + c]
                    s2[c] += cmap.px[i + c] ** 2
                n += 1
        mean = [v / n for v in s1]
        variance = sum(s2[c] / n - mean[c] ** 2 for c in range(3))
        if variance < PATCH_VARIANCE and kind(mean) == ("cloth", "skin")[k]:
            good[gy][gx] = True
    best, heights = (0, None), [0] * gw
    for y in range(gh):
        for x in range(gw):
            heights[x] = heights[x] + 1 if good[y][x] else 0
        stack = []
        for x in range(gw + 1):
            cur = heights[x] if x < gw else 0
            start = x
            while stack and stack[-1][1] >= cur:
                sx, sh = stack.pop()
                if sh * (x - sx) > best[0]:
                    best = (sh * (x - sx), (sx, x, y - sh + 1, y))
                start = sx
            stack.append((start, cur))
    check(best[1] is not None, f"no plain patch of kind {k} in the map")
    x0, x1, y0, y1 = best[1]
    px = ((x0 + 1) * C, (x1 - 1) * C, (y0 + 1) * C, (y1) * C)   # inset by a cell
    check(px[1] - px[0] >= 2 * C and px[3] - px[2] >= 2 * C, f"the plain patch of kind {k} is too small: {best}")
    return {"texels": [px[0], px[1], px[2], px[3]], "uv": (px[0] / cmap.w, px[2] / cmap.h, (px[1] - px[0]) / cmap.w, (px[3] - px[2]) / cmap.h)}


def skirt(bm, edges, axis, kept, new_layer):
    """Quads from an open rim chain to a copy of it moved toward the neck's axis to SKIRT_INSET_M inside the nearest body
    face below the rim, one quad per edge, oriented from the kept rim."""
    import bmesh
    from mathutils import Vector
    from collections import defaultdict
    tree, body = kept
    adjacency = defaultdict(list)
    by_index = {}
    for e in edges:
        for v in e.verts:
            adjacency[v.index].append(e)
            by_index[v.index] = v
    ends = [v for v, es in adjacency.items() if len(es) == 1]
    start = by_index[ends[0]] if ends else edges[0].verts[0]
    chain, seen, v = [start], set(), start
    while True:
        nxt = next((e for e in adjacency[v.index] if e.index not in seen), None)
        if nxt is None:
            break
        seen.add(nxt.index)
        v = nxt.other_vert(v)
        chain.append(v)
    copies = {}
    for v in chain:
        out = Vector((v.co.x - axis.x, v.co.y - axis.y, 0.0))
        radius = out.length
        inward = out.normalized()
        depth = None
        for co, i, d in tree.find_n(v.co, 12):
            g = body[i]
            if co.z < v.co.z - 0.002 and (Vector((co.x - axis.x, co.y - axis.y, 0.0)).length < radius):
                depth = Vector((co.x - axis.x, co.y - axis.y, 0.0)).length
                break
        target = (depth if depth is not None else radius - 0.012) - SKIRT_INSET_M
        copies[v.index] = bm.verts.new(Vector((axis.x, axis.y, v.co.z - 0.003)) + inward * max(target, 0.01))
    faces = []
    for p, q in zip(chain, chain[1:]):
        quad = (p, q, copies[q.index], copies[p.index])
        if len({id(w) for w in quad}) == 4 and not bm.faces.get(quad):
            faces.append(bm.faces.new(quad))
    orient(bm, faces, new_layer)
    # The skirt's inner edge is welded onto the nearest body vertices (within SKIRT_WELD_M), so the mesh closes there.
    chain_ids = {v.index for v in chain}
    targets = {}
    for w in copies.values():
        best = None
        for co, i, d in tree.find_n(w.co, 8):
            for v in body[i].verts:
                if v.index in chain_ids or v in copies.values():
                    continue
                dist = (v.co - w.co).length
                if dist < SKIRT_WELD_M and (best is None or dist < best[0]):
                    best = (dist, v)
        if best is not None:
            targets[w] = best[1]
    if targets:
        bmesh.ops.weld_verts(bm, targetmap=targets)
    return [f for f in faces if f.is_valid]


def orient(bm, faces, new_layer) -> None:
    """Turns the given new faces to agree with their kept neighbours (across a shared edge the two faces run it opposite
    ways), spreading from the kept rim inward; every face must be reached."""
    oriented = set()
    changed = True
    while changed:
        changed = False
        for f in faces:
            if id(f) in oriented:
                continue
            for l in f.loops:
                e = l.edge
                other = next((g for g in e.link_faces if g is not f and (g[new_layer] == 0 or id(g) in oriented)), None)
                if other is None:
                    continue
                ol = next(x for x in other.loops if x.edge is e)
                if ol.vert is l.vert:
                    f.normal_flip()
                oriented.add(id(f))
                changed = True
                break
    check(len(oriented) == len(faces), f"{len(faces) - len(oriented)} new faces could not be oriented")
    for f in faces:
        f.normal_update()


def make(repo: Path, out_dir: Path | None = None, blend: Path | None = None) -> dict:
    import bpy
    import bmesh
    from collections import Counter
    from mathutils import Vector
    from mathutils.kdtree import KDTree
    import slim_collection as privacy   # its scrub blanks machine paths Blender's FBX writer leaves in a file

    src_dir = repo / COLLECTION / SOURCE
    dest = out_dir or repo / COLLECTION / OUT
    dest.mkdir(parents=True, exist_ok=True)
    report: dict = {"inputs": {f"{SOURCE}.fbx": sha((src_dir / f"{SOURCE}.fbx").read_bytes()),
                               f"{SOURCE}_basecolor.jpg": sha((src_dir / f"{SOURCE}_basecolor.jpg").read_bytes())}}
    mesh, arm = load(bpy, src_dir / f"{SOURCE}.fbx")
    arm.data.pose_position = "REST"
    cmap = Map(bpy, src_dir / f"{SOURCE}_basecolor.jpg")
    me = mesh.data
    report["source"] = {"vertices": len(me.vertices), "faces": len(me.polygons), "uv_pieces": None}

    # 1. One surface.
    bm = bmesh.new()
    bm.from_mesh(me)
    uvl = bm.loops.layers.uv.active
    deform = bm.verts.layers.deform.verify()
    kind_layer = bm.faces.layers.int.new("kind")     # 0 kept cloth, 1 kept skin, 2 new
    report["source"]["uv_pieces"] = len(components(bm.faces, lambda f: True))
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=WELD)

    def reindex():
        for seq in (bm.verts, bm.edges, bm.faces):
            seq.ensure_lookup_table()
            seq.index_update()

    reindex()
    old_boundary = {edge_key(e) for e in bm.edges if e.is_boundary}
    report["welded"] = {"vertices": len(bm.verts), "faces": len(bm.faces), "boundary_edges": len(old_boundary),
                        "pieces": len(components(bm.faces, lambda f: True))}

    # 2. The scarf's faces.
    kinds = {}
    for f in bm.faces:
        kinds[f.index] = kind(face_colour(f, uvl, cmap))
        f[kind_layer] = 1 if kinds[f.index] == "skin" else 0

    def in_box(f) -> bool:
        c = f.calc_center_median()
        return BOX_Z[0] < c.z < BOX_Z[1] and abs(c.x) < BOX_X and not (c.z > CHIN_Z and c.y < CHIN_Y)

    selected = {f.index for f in bm.faces if kinds[f.index] == "white" and in_box(f)}
    report["white_in_box"] = len(selected)
    for _ in range(2):   # the pattern's dark dots: small patches the selection encloses
        for group in components(bm.faces, lambda f: f.index not in selected):
            if len(group) > 150:
                continue
            ids = {g.index for g in group}
            to_selection = border = 0
            for g in group:
                for e in g.edges:
                    if e.is_boundary:
                        border += 1
                        continue
                    for h in e.link_faces:
                        if h is g:
                            continue
                        if h.index in selected:
                            to_selection += 1
                        elif h.index not in ids:
                            border += 1
            share = to_selection / max(1, to_selection + border)
            if all(in_box(g) for g in group) and (share >= 0.7 or (len(group) <= 12 and share >= 0.4)):
                selected |= ids
    for group in components(bm.faces, lambda f: f.index in selected):
        if len(group) < SPECK:
            selected -= {g.index for g in group}
    report["scarf_faces"] = len(selected)
    report["scarf_pieces"] = sorted((len(g) for g in components(bm.faces, lambda f: f.index in selected)), reverse=True)
    scarf_donors = {}
    for i in selected:
        for v in bm.faces[i].verts:
            scarf_donors[v.index] = (v.co.copy(), dict(v[deform]))

    def delete(faces) -> None:
        bmesh.ops.delete(bm, geom=list(faces), context="FACES")
        reindex()

    def new_boundary():
        return [e for e in bm.edges if e.is_boundary and edge_key(e) not in old_boundary]

    delete([bm.faces[i] for i in selected])

    # 3. What the scarf leaves that is not body.
    rim = {v.index for e in new_boundary() for v in e.verts}
    loose = [g for g in components(bm.faces, lambda f: True) if len(g) < 400 and any(v.index in rim for f in g for v in f.verts)]
    report["loose_pieces"] = [len(g) for g in loose]
    if loose:
        delete([f for g in loose for f in g])
    scarf_tree = KDTree(len(scarf_donors))
    for i, (co, _) in enumerate(scarf_donors.values()):
        scarf_tree.insert(co, i)
    scarf_tree.balance()

    def near_scarf(co, d=0.006) -> bool:
        return bool(scarf_tree.find_range(co, d))

    def zone(f) -> bool:
        c = f.calc_center_median()
        return 0.60 < c.z < BOX_Z[1] and abs(c.x) < 0.22

    def rim_tree():
        pts = [bm.verts[i].co for i in {v.index for e in new_boundary() for v in e.verts}]
        tree = KDTree(len(pts))
        for i, p in enumerate(pts):
            tree.insert(p, i)
        tree.balance()
        return tree

    def reference_faces(tree, clear=0.015):
        return [f for f in bm.faces if zone(f) and tree.find(f.calc_center_median())[2] >= clear]

    report["sticks_out"] = []
    for _ in range(5):
        tree = rim_tree()
        ref = reference_faces(tree)
        ref_tree = KDTree(len(ref))
        for i, f in enumerate(ref):
            ref_tree.insert(f.calc_center_median(), i)
        ref_tree.balance()
        out = []
        for f in bm.faces:
            if not zone(f):
                continue
            c = f.calc_center_median()
            if tree.find(c)[2] >= 0.015 or not near_scarf(c) or f.normal.length == 0:
                continue
            heights, angles, normals = [], [], Vector()
            for co, i, _ in ref_tree.find_n(c, 6):
                g = ref[i]
                if g.normal.length == 0:
                    continue
                heights.append((c - co).dot(g.normal))
                angles.append(math.degrees(f.normal.angle(g.normal)))
                normals += g.normal
            if heights:
                height, angle = sum(heights) / len(heights), sum(angles) / len(angles)
                flat_body = normals.length / len(heights) > 0.95   # the body faces around agree: a flat stretch, as the back
                if (height > STICKS_OUT_FLAT_M and flat_body and angle < 25) or (height > STICKS_OUT_M and angle > STICKS_OUT_DEG):
                    out.append(f)
        report["sticks_out"].append(len(out))
        if not out:
            break
        delete(out)
    report["spikes"] = []
    for _ in range(2):
        rim_edges = {e.index for e in new_boundary()}
        spikes = [f for f in bm.faces if sum(1 for e in f.edges if e.index in rim_edges) >= 2 and near_scarf(f.calc_center_median(), 0.004)]
        report["spikes"].append(len(spikes))
        if not spikes:
            break
        delete(spikes)
    report["pinches"] = []
    for _ in range(10):
        count = Counter(v.index for e in new_boundary() for v in e.verts)
        pinched = [v for v, n in count.items() if n > 2]
        report["pinches"].append(len(pinched))
        if not pinched:
            break
        delete({f for v in pinched for f in bm.verts[v].link_faces})
    check(report["pinches"][-1] == 0, "the hole rims still pinch")

    # 4. Close the holes. Their rims are the new open edges and the open edges Tal's mesh had there already (the neck is
    # a tube open at both ends under the scarf). About the neck's axis, the collar's rim and the neck's bottom wind once
    # each: that hole is a ring, bridged, after the V down the chest is cut off it by a chord and closed on its own. The
    # rest (the crescent at the nape, the gaps on the neck's sides) are closed by an edge-net fill of all the open edges
    # the scarf touched, done in the unwrap (angle, height), where every such rim is a flat polygon.
    in_box = lambda c: BOX_Z[0] < c.z < BOX_Z[1] and abs(c.x) < BOX_X
    open_edges = [e for e in bm.edges if e.is_boundary and in_box((e.verts[0].co + e.verts[1].co) / 2)]
    loops = [lp for lp in walk_loops(open_edges) if len(lp) >= 3]
    axis = Vector((0.0, 0.0))
    for lp in loops:
        if len(lp) > len(max(loops, key=len)) - 1:
            axis = Vector((sum(v.co.x for v in lp) / len(lp), sum(v.co.y for v in lp) / len(lp)))
    report["axis_xy"] = [round(axis.x, 4), round(axis.y, 4)]

    def unwrap(loop, cut=None):
        """Each vertex's angle about the axis, continuous along the loop (or from the cut), and the loop's winding."""
        theta, last = [], None
        for v in loop:
            a = math.atan2(v.co.y - axis.y, v.co.x - axis.x)
            if cut is not None:
                a = cut + (a - cut) % (2 * math.pi)
            elif last is not None:
                a = last + (a - last + math.pi) % (2 * math.pi) - math.pi
            theta.append(a)
            last = a
        closing = (theta[0] - theta[-1] + math.pi) % (2 * math.pi) - math.pi
        return theta, (theta[-1] + closing - theta[0]) / (2 * math.pi)

    rings = []
    for lp in loops:
        theta, winding = unwrap(lp)
        new = any(edge_key(e) not in old_boundary for v in lp for e in v.link_edges if e.is_boundary and e.other_vert(v) in lp)
        if abs(winding) > 0.9 and (lp[0].co - lp[-1].co).length < 0.04:   # the V bottom has a long edge
            rings.append((sum(v.co.z for v in lp) / len(lp), lp, theta, new))
    rings.sort(key=lambda r: r[0])
    report["rims"] = [{"n": len(lp), "winding": round(unwrap(lp)[1], 2), "closed": round((lp[0].co - lp[-1].co).length, 4),
                       "z": [round(min(v.co.z for v in lp), 3), round(max(v.co.z for v in lp), 3)]} for lp in loops]
    check(len(rings) == 2 and rings[0][3] and not rings[1][3], f"expected the collar's rim and the neck's bottom, found {[(round(r[0], 3), len(r[1]), r[3]) for r in rings]}; rims {report['rims']}")
    _, outer, outer_theta, _ = rings[0]
    _, inner, _, _ = rings[1]
    report["holes"] = [{"what": "collar ring and chest V", "rim_vertices": len(outer)}, {"what": "neck bottom", "rim_vertices": len(inner)}]
    new_layer = bm.faces.layers.int.new("new")   # a new layer invalidates every face reference: both are made here, before any fill
    part = bm.faces.layers.int.new("part")       # 1 the V, 2 the collar ring, 3 the rest

    def loop_edges(loop):
        members = {v.index for v in loop}
        return list({e.index: e for v in loop for e in v.link_edges if e.is_boundary and e.other_vert(v).index in members}.values())

    # The V: the run of the collar's rim within V_FRONT_DEG of the front (the face looks toward -y) and below V_Z.
    front = math.atan2(-1.0, 0.0)
    in_v = [abs((t - front + math.pi) % (2 * math.pi) - math.pi) < math.radians(V_FRONT_DEG) and v.co.z < V_Z for v, t in zip(outer, outer_theta)]
    check(any(in_v) and not all(in_v), "no V found on the collar's rim")
    start = next(i for i in range(len(outer)) if in_v[i] and not in_v[i - 1])
    run = []
    i = start
    while in_v[i % len(outer)]:
        run.append(outer[i % len(outer)])
        i += 1
    check(sum(in_v) == len(run), "the V is not one run of the rim")
    a, b = outer[(start - 1) % len(outer)], outer[i % len(outer)]
    chord = bm.edges.new((a, b))
    v_loop = [a] + run + [b]
    kept_co = [v.co.copy() for v in v_loop]
    theta, _ = unwrap(v_loop)
    for v, t in zip(v_loop, theta):
        v.co = Vector((NECK_R * t, v.co.z, 0.0))
    v_edges = [e for e in loop_edges(v_loop) if e is not chord] + [chord]
    filled = bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=v_edges)
    v_faces = [g for g in filled["geom"] if isinstance(g, bmesh.types.BMFace)]
    for v, co in zip(v_loop, kept_co):
        v.co = co
    check(v_faces, "the V did not fill")
    for f in v_faces:
        f[new_layer] = 1
        f[kind_layer] = 2
        f[part] = 1
    report["holes"][0]["v_rim_vertices"] = len(v_loop)
    report["holes"][0]["v_triangles"] = len(v_faces)
    # The ring: the collar's rim without the V, closed by the chord, zipped to the neck's bottom: both rims are walked in
    # angle order about the neck's axis and a triangle is laid to whichever rim's next vertex comes first, so the
    # collar is a clean band of Tal's rim vertices with no new vertex of its own before the subdivision.
    run_ids = {v.index for v in run}
    ring = [v for v in outer if v.index not in run_ids]
    ring = ring[ring.index(b):] + ring[:ring.index(b)]
    check(ring[-1] is a, "the chord's ends are not adjacent on the collar's rim")
    ring_theta, _ = unwrap(ring)
    if ring_theta[-1] < ring_theta[0]:
        ring, ring_theta = [ring[0]] + ring[1:][::-1], None
        ring_theta, _ = unwrap(ring)
    inner_theta, _ = unwrap(inner)
    if inner_theta[-1] < inner_theta[0]:
        inner = inner[::-1]
        inner_theta, _ = unwrap(inner)
    k = min(range(len(inner)), key=lambda i: abs((inner_theta[i] - ring_theta[0] + math.pi) % (2 * math.pi) - math.pi))
    inner = inner[k:] + inner[:k]
    inner_theta, _ = unwrap(inner)
    shift = ring_theta[0] - inner_theta[0]
    inner_theta = [t + round(shift / (2 * math.pi)) * 2 * math.pi for t in inner_theta]
    ring_edges = loop_edges(ring)   # the rim's own edges and the chord, for the collar's rib below
    ring_faces = []
    i = j = 0
    turn = 2 * math.pi
    while i < len(ring) or j < len(inner):
        next_ring = ring_theta[i + 1] if i + 1 < len(ring) else ring_theta[0] + turn
        next_inner = inner_theta[j + 1] if j + 1 < len(inner) else inner_theta[0] + turn
        r0, n0 = ring[i % len(ring)], inner[j % len(inner)]
        if i < len(ring) and (j >= len(inner) or next_ring <= next_inner):
            tri = (r0, ring[(i + 1) % len(ring)], n0)
            i += 1
        else:
            tri = (r0, inner[(j + 1) % len(inner)], n0)
            j += 1
        if len({id(v) for v in tri}) == 3 and not bm.faces.get(tri):
            ring_faces.append(bm.faces.new(tri))
    check(len(ring_faces) >= len(ring) + len(inner) - 2, f"the collar zipped into {len(ring_faces)} triangles")
    orient(bm, ring_faces, new_layer)
    for f in ring_faces:
        f.normal_update()
        f[new_layer] = 1
        f[kind_layer] = 2
        f[part] = 2
    report["holes"][0]["ring_faces"] = len(ring_faces)
    inner_pts = [v.co.copy() for v in inner]              # the rims' positions, taken now: the later operators
    ring_rim_pts = [v.co.copy() for v in ring]            # invalidate element references
    reindex()
    # The rest: every open edge the scarf touched that is still open, each connected net on its own, as an edge net in
    # the unwrap cut through the widest angle its vertices leave free.
    rest = [e for e in bm.edges if e.is_boundary and in_box((e.verts[0].co + e.verts[1].co) / 2)]
    parent = {}

    def find(i):
        while parent.setdefault(i, i) != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for e in rest:
        parent[find(e.verts[0].index)] = find(e.verts[1].index)
    nets = {}
    for e in rest:
        nets.setdefault(find(e.verts[0].index), []).append(e)
    nets = {k: v for k, v in nets.items() if any(near_scarf((e.verts[0].co + e.verts[1].co) / 2, 0.010) for e in v)}
    body = [f for f in bm.faces if f[new_layer] == 0 and zone(f)]
    kept_faces_tree = (KDTree(len(body)), body)
    for i, f in enumerate(body):
        kept_faces_tree[0].insert(f.calc_center_median(), i)
    kept_faces_tree[0].balance()
    rest_faces = []
    report["holes"].append({"what": "nape crescent and neck-side gaps", "open_edges": sum(len(v) for v in nets.values()), "nets": []})
    for net_edges in nets.values():
        verts = {v.index: v for e in net_edges for v in e.verts}
        angles = sorted(math.atan2(v.co.y - axis.y, v.co.x - axis.x) for v in verts.values())
        gaps = [((angles[(k + 1) % len(angles)] - angles[k]) % (2 * math.pi), angles[k]) for k in range(len(angles))]
        cut = max(gaps)[1] + max(gaps)[0] / 2
        kept_co = {i: v.co.copy() for i, v in verts.items()}
        for v in verts.values():
            t = cut + (math.atan2(v.co.y - axis.y, v.co.x - axis.x) - cut) % (2 * math.pi)
            v.co = Vector((NECK_R * t, v.co.z, 0.0))
        net = bmesh.ops.holes_fill(bm, edges=net_edges, sides=0)
        tri = bmesh.ops.triangulate(bm, faces=list(net["faces"]), quad_method="BEAUTY", ngon_method="BEAUTY")
        for i, v in verts.items():
            v.co = kept_co[i]
        faces = list(tri["faces"])
        how = "net"
        if not faces:
            # Not a closed rim: the head's base hangs over the neck, whose surface runs on into the head. The chain is
            # extruded inward to just inside the neck (a skirt); the relax below settles it onto the neck's surface.
            faces = skirt(bm, net_edges, axis, kept_faces_tree, new_layer)
            how = "skirt"
        rest_faces += faces
        report["holes"][-1]["nets"].append({"open_edges": len(net_edges), "free_arc_deg": round(math.degrees(max(gaps)[0]), 1), "triangles": len(faces), "how": how})
    reindex()
    for f in rest_faces:
        f[new_layer] = 1
        f[kind_layer] = 2
        f[part] = 3
    new_faces = v_faces + ring_faces + rest_faces
    # The sweater's rim is a sawtooth of cut faces: it is eased, each rim vertex drawn halfway to the mean of its two rim
    # neighbours within the plane of its kept faces (RIM_EASE passes), so the collar and the V meet the sweater on a
    # smooth line. These are the only kept vertices that move, and only along the surface.
    sweater_rim = list({v.index: v for v in run + [a, b]}.values()) + [v for v in outer if v not in run]
    rim_index = {v.index: v for v in sweater_rim}
    for _ in range(RIM_EASE):
        moves = {}
        for v in sweater_rim:
            around = [e.other_vert(v) for e in v.link_edges if e.other_vert(v).index in rim_index]
            kept_faces = [f for f in v.link_faces if f[new_layer] == 0 and f.normal.length > 0]
            if len(around) != 2 or not kept_faces:
                continue
            n = sum((f.normal for f in kept_faces), Vector()).normalized()
            target = (around[0].co + around[1].co) / 2
            step = (target - v.co) * 0.5
            moves[v.index] = v.co + step - n * step.dot(n)
        for v in sweater_rim:
            if v.index in moves:
                v.co = moves[v.index]
    report["rim_vertices_eased"] = len(sweater_rim)
    reindex()
    for _ in range(SUBDIVISIONS):
        split_edges = [e for e in bm.edges if e.link_faces and all(f[new_layer] == 1 for f in e.link_faces) and not e.is_boundary]
        bmesh.ops.subdivide_edges(bm, edges=split_edges, cuts=1, use_grid_fill=True)
        reindex()
        fresh = [f for f in bm.faces if f[new_layer] == 1]
        bmesh.ops.triangulate(bm, faces=fresh)
        reindex()
    new_faces = [f for f in bm.faces if f[new_layer] == 1]
    rim = {v.index for e in new_boundary() for v in e.verts} | {v.index for f in new_faces for v in f.verts if any(g[new_layer] == 0 for g in v.link_faces)}
    interior = [v for v in {v.index: v for f in new_faces for v in f.verts}.values() if v.index not in rim]
    report["new_faces"] = len(new_faces)
    report["new_vertices"] = len(interior)

    def relax(times: int, strength: float = 0.5) -> None:
        for _ in range(times):
            moves = {}
            for v in interior:
                around = [e.other_vert(v).co for e in v.link_edges]
                moves[v.index] = v.co.lerp(sum(around, Vector()) / len(around), strength)
            for v in interior:
                v.co = moves[v.index]

    rim_pts = [bm.verts[i].co for i in rim]
    tree = KDTree(len(rim_pts))
    for i, co in enumerate(rim_pts):
        tree.insert(co, i)
    tree.balance()
    ref = reference_faces(tree, clear=0.012)
    ref_tree = KDTree(len(ref))
    for i, f in enumerate(ref):
        ref_tree.insert(f.calc_center_median(), i)
    ref_tree.balance()

    def project(strength: float) -> None:
        """Draws each new vertex toward the plane of the nearest body faces, more the farther it is from the rim."""
        for v in interior:
            weight = min(1.0, max(0.0, (tree.find(v.co)[2] - 0.002) / 0.006)) * strength
            if weight <= 0:
                continue
            height, normal, total = 0.0, Vector(), 0.0
            for co, i, d in ref_tree.find_n(v.co, 6):
                g = ref[i]
                if g.normal.length == 0:
                    continue
                w = 1.0 / max(d, 1e-4)
                height += w * (v.co - co).dot(g.normal)
                normal += w * g.normal
                total += w
            if total > 0 and normal.length > 0:
                v.co -= normal.normalized() * (height / total) * weight

    relax(RELAX)
    for _ in range(6):   # the membrane settles onto the body's surface, the rim's own faces aside
        project(1.0)
        relax(4, 0.3)
    project(1.0)
    # The collar: the ring between the neck's bottom and the sweater's rim lies flat on the shoulders; a rolled rib along
    # its middle (COLLAR_RIB_M high, zero at both rims) makes it read as the sweater's crew neck.
    inner_tree = KDTree(len(inner_pts))
    for i, co in enumerate(inner_pts):
        inner_tree.insert(co, i)
    inner_tree.balance()
    outer_tree = KDTree(len(ring_rim_pts))
    for i, co in enumerate(ring_rim_pts):
        outer_tree.insert(co, i)
    outer_tree.balance()
    ring_verts = {v.index: v for f in bm.faces if f[part] == 2 for v in f.verts if v.index not in rim}
    for v in ring_verts.values():
        d_in, d_out = inner_tree.find(v.co)[2], outer_tree.find(v.co)[2]
        width = d_in + d_out
        t = d_in / max(width, 1e-6)
        narrow = min(1.0, max(0.0, (COLLAR_RIB_WIDTH_M[1] - width) / (COLLAR_RIB_WIDTH_M[1] - COLLAR_RIB_WIDTH_M[0])))
        v.co.z += COLLAR_RIB_M * math.sin(math.pi * t) * narrow   # no rib down the wide notch behind the neck
    relax(2, 0.2)

    # 5. Texels: each new face is the sweater (the collar ring and the V) or skin (the gaps at the nape and the neck's
    # sides), drawn from a plain patch of Tal's map of that kind, found below, tiled over the face by position with a
    # mirrored wave so the texels run on without a seam. The map itself is unchanged.
    patches = {k: plain_patch(bm, uvl, cmap, k) for k in (0, 1)}
    report["texel_patches"] = {("cloth", "skin")[k]: v for k, v in patches.items()}

    def wave(t: float) -> float:
        t = t % 2.0
        return t if t < 1.0 else 2.0 - t

    report["texel_kinds"] = {"skin": 0, "cloth": 0}
    for f in new_faces:
        k = 1 if f[part] == 3 else 0
        report["texel_kinds"]["skin" if k else "cloth"] += 1
        u0, v0, w, h = patches[k]["uv"]
        for l in f.loops:
            co = l.vert.co
            along = math.atan2(co.y - axis.y, co.x - axis.x) * NECK_R if k else co.x
            l[uvl].uv = Vector((u0 + w * wave(along * TEXELS_PER_M / (w * cmap.w)), v0 + h * wave(co.z * TEXELS_PER_M / (h * cmap.h))))

    # 6. Skin weights from the neighbours.
    def weighted(v) -> bool:
        return any(w > 0.001 for w in v[deform].values())

    donors = [d for d in scarf_donors.values() if any(w > 0.001 for w in d[1].values())]
    donors += [(bm.verts[i].co.copy(), dict(bm.verts[i][deform])) for i in rim if weighted(bm.verts[i])]
    # Tal's rim vertices that had no weight (the neck tube's open bottom, inside the sweater before) now carry visible
    # faces, so they are weighted like the new vertices, from the weighted neighbours.
    bare_rim = [bm.verts[i] for i in rim if not weighted(bm.verts[i])]
    report["rim_vertices_weighted"] = len(bare_rim)
    donor_tree = KDTree(len(donors))
    for i, (co, _) in enumerate(donors):
        donor_tree.insert(co, i)
    donor_tree.balance()
    influences = []
    for v in interior + bare_rim:
        weights: dict = {}
        total = 0.0
        for co, i, d in donor_tree.find_n(v.co, 4):
            w = 1.0 / max(d, 1e-4)
            total += w
            for group, value in donors[i][1].items():
                weights[group] = weights.get(group, 0.0) + w * value
        top = sorted(weights.items(), key=lambda kv: -kv[1])[:4]
        norm = sum(value for _, value in top)
        check(norm > 0, "a new vertex found no weighted neighbour")
        v[deform].clear()
        for group, value in top:
            v[deform][group] = value / norm
        influences.append(len(top))
    report["new_vertex_influences_max"] = max(influences) if influences else 0
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
        reindex()
    report["loose_vertices_dropped"] = len(loose)
    unweighted = [v for v in bm.verts if not any(w > 0.001 for w in v[deform].values())]
    check(not unweighted, f"{len(unweighted)} vertices without weight, around z {sorted(round(v.co.z, 3) for v in unweighted)[:6]}")
    for f in bm.faces:
        f.smooth = True
    bm.faces.layers.int.remove(new_layer)
    bm.faces.layers.int.remove(kind_layer)
    bm.to_mesh(me)   # with the "part" tags, for the working .blend; they are dropped before the export
    bm.free()
    me.update()
    report["result"] = {"vertices": len(me.vertices), "triangles": sum(len(p.vertices) - 2 for p in me.polygons)}
    report["actions"] = sorted(a.name for a in bpy.data.actions)
    check({a.split("|")[-1] for a in report["actions"]} == set(TAKES), f"takes: {report['actions']}")
    if blend is not None:
        bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    me.attributes.remove(me.attributes["part"])

    # Export: the armature posed again (the exporter bakes the takes), no material or image (the prefab uses the .mat beside
    # the model), every bone, every frame keyed. The units go into the data as C07's own export has them (FBX_SCALE_ALL):
    # the bones at scale 1, as UnitWeapons' hand socket needs.
    arm.data.pose_position = "POSE"
    arm.animation_data_create()
    for act in bpy.data.actions:   # exported as "Armature|<take>", as Tal's file names them (Blender prefixes the armature)
        act.name = act.name.split("|")[-1]
    idle = next(a for a in bpy.data.actions if a.name == "idle")
    arm.animation_data.action = idle
    arm.animation_data.action_slot = idle.slots[0]
    me.materials.clear()
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)
    for im in list(bpy.data.images):
        bpy.data.images.remove(im)
    fbx = dest / f"{OUT}.fbx"
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    arm.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(fbx), use_selection=True, object_types={"MESH", "ARMATURE"}, add_leaf_bones=False,
        mesh_smooth_type="FACE", use_mesh_modifiers=True, use_tspace=False, use_custom_props=False,
        path_mode="STRIP", embed_textures=False, use_armature_deform_only=False, apply_scale_options="FBX_SCALE_ALL",
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_step=1.0, bake_anim_simplify_factor=0.0, bake_anim_force_startend_keying=True)
    privacy.scrub(fbx, repo)
    shutil.copyfile(src_dir / f"{SOURCE}_basecolor.jpg", dest / f"{OUT}_basecolor.jpg")
    report["fbx_sha256"] = sha(fbx.read_bytes())
    (dest / "make-report.json").write_text(json.dumps(report, indent=1) + "\n", encoding="utf-8")
    print("MADE", fbx, report["fbx_sha256"])
    return report


def welded_boundary_in_box(bpy, bmesh, mesh) -> list:
    """The midpoints of the open edges in the neck box once the chart pieces are welded: the holes a figure has there."""
    bm = bmesh.new()
    bm.from_mesh(mesh.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=WELD)
    mids = []
    for e in bm.edges:
        if not e.is_boundary:
            continue
        c = (e.verts[0].co + e.verts[1].co) / 2
        if BOX_Z[0] < c.z < BOX_Z[1] and abs(c.x) < BOX_X:
            mids.append(c.copy())
    bm.free()
    return mids


def scarf_texel_faces(bpy, bmesh, mesh, cmap) -> dict:
    """Faces in the neck box carrying the scarf's texels (bright, unsaturated), the chin and beard aside: how many, and how
    many in connected patches of SPECK faces or more once the chart pieces are welded (a patch that size is scarf; the
    smaller ones are grey beard hairs and texel bleed at chart edges, which the original has too)."""
    bm = bmesh.new()
    bm.from_mesh(mesh.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=WELD)
    bm.faces.ensure_lookup_table()
    bm.faces.index_update()
    uvl = bm.loops.layers.uv.active
    white = set()
    for f in bm.faces:
        c = f.calc_center_median()
        if BOX_Z[0] < c.z < BOX_Z[1] and abs(c.x) < BOX_X and not (c.z > CHIN_Z and c.y < CHIN_Y) and kind(face_colour(f, uvl, cmap)) == "white":
            white.add(f.index)
    patches = sorted((len(g) for g in components(bm.faces, lambda f: f.index in white)), reverse=True)
    bm.free()
    return {"faces": len(white), "patches": patches[:8], "scarf_patches": sum(1 for n in patches if n >= SPECK)}


def inspect(repo: Path, fbx: Path, colour: Path, stills: Path | None) -> dict:
    """Re-imports the copy (a fresh process) and reports its rig, takes, weights and closed neck; stills when asked."""
    import bpy
    import bmesh
    from mathutils import Vector

    mesh, arm = load(bpy, fbx)
    cmap = Map(bpy, colour)
    takes = {a.name.split("|")[-1]: [int(f) for f in a.frame_range] for a in bpy.data.actions}
    groups = {g.name for g in mesh.vertex_groups}
    unweighted = sum(1 for v in mesh.data.vertices if not any(g.weight > 0.001 for g in v.groups))
    influences = [sum(1 for g in v.groups if g.weight > 0.001) for v in mesh.data.vertices]
    report = {
        "bones": len(arm.data.bones), "bone_names": sorted(b.name for b in arm.data.bones),
        "takes": takes, "take_names": sorted(a.name for a in bpy.data.actions), "fps": bpy.context.scene.render.fps,
        "vertices": len(mesh.data.vertices), "triangles": sum(len(p.vertices) - 2 for p in mesh.data.polygons),
        "vertex_groups": len(groups), "unweighted_vertices": unweighted, "max_influences": max(influences),
        "height_m": max((mesh.matrix_world @ Vector(c)).z for c in mesh.bound_box),
        "scarf_texels_in_box": scarf_texel_faces(bpy, bmesh, mesh, cmap),
    }
    copy_open = welded_boundary_in_box(bpy, bmesh, mesh)
    # The original's own boundary edges in the box (Tal's mesh has a few open edges of its own): the copy may keep those, no more.
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(repo / COLLECTION / SOURCE / f"{SOURCE}.fbx"))
    added = [o for o in bpy.data.objects if o not in before]
    original = next(o for o in added if o.type == "MESH")
    from mathutils.kdtree import KDTree
    original_open = welded_boundary_in_box(bpy, bmesh, original)
    tree = KDTree(len(original_open))
    for i, c in enumerate(original_open):
        tree.insert(c, i)
    tree.balance()
    new_open = [c for c in copy_open if tree.find(c)[2] > 1e-4]
    # A new open edge inside the body (a skirt's inner edge, tucked into the neck) is hidden; one outside is a hole.
    from mathutils.bvhtree import BVHTree
    bvh = BVHTree.FromObject(mesh, bpy.context.evaluated_depsgraph_get())

    def inside(co) -> bool:
        votes = 0
        for direction in (Vector((1, 0, 0)), Vector((-1, 0, 0)), Vector((0, 1, 0)), Vector((0, -1, 0))):
            hits, origin = 0, co.copy()
            for _ in range(64):
                hit = bvh.ray_cast(origin, direction)
                if hit[0] is None:
                    break
                hits += 1
                origin = hit[0] + direction * 1e-5
            votes += hits % 2
        return votes >= 3

    exposed = [c for c in new_open if not inside(c)]
    report["open_edges_in_box"] = {"copy": len(copy_open), "original": len(original_open), "new_in_copy": len(new_open),
                                   "new_hidden_inside": len(new_open) - len(exposed), "new_exposed": len(exposed),
                                   "exposed_sample": [[round(v, 3) for v in c] for c in exposed[:12]]}
    for o in added:
        bpy.data.objects.remove(o, do_unlink=True)
    if stills is not None:
        stills.mkdir(parents=True, exist_ok=True)
        render_stills(bpy, mesh, arm, stills, cmap)
    return report


def render_stills(bpy, mesh, arm, out: Path, cmap: Map) -> None:
    """Workbench stills with the map baked to corner colours (headless bpy has no GPU texture): rest and frames of the
    idle, walk and fire takes, head and body crops, front, three-quarter, side and back."""
    scene = bpy.context.scene
    me = mesh.data
    uv = me.uv_layers.active.data
    colours = me.color_attributes.new("map", "FLOAT_COLOR", "CORNER")
    for p in me.polygons:
        for li in p.loop_indices:
            r, g, b = cmap.at(uv[li].uv.x, uv[li].uv.y)
            colours.data[li].color = (r, g, b, 1.0)
    me.color_attributes.active_color = colours
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "VERTEX"
    scene.display.shading.show_shadows = False
    scene.render.resolution_x = scene.render.resolution_y = 900
    scene.world = bpy.data.worlds.new("w")
    scene.world.color = (0.5, 0.5, 0.55)
    cam_data = bpy.data.cameras.new("cam")
    cam_data.lens = 85
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    from mathutils import Vector

    def shoot(tag: str) -> None:
        dg = bpy.context.evaluated_depsgraph_get()
        ev = mesh.evaluated_get(dg)
        posed = ev.to_mesh()
        pts = [ev.matrix_world @ v.co for v in posed.vertices]
        ev.to_mesh_clear()
        zmin, zmax = min(p.z for p in pts), max(p.z for p in pts)
        head = [p for p in pts if p.z > zmin + 0.78 * (zmax - zmin)]
        cx, cy = sum(p.x for p in head) / len(head), sum(p.y for p in head) / len(head)
        for crop, zc, span in (("head", zmin + 0.84 * (zmax - zmin), 0.34 * (zmax - zmin)), ("body", (zmin + zmax) / 2, 1.15 * (zmax - zmin))):
            for name, deg in (("front", 0), ("q34l", 40), ("q34r", -40), ("side", 90), ("back", 180), ("backq", 140)):
                target = Vector((cx if crop == "head" else 0.0, cy if crop == "head" else 0.02, zc))
                dist = span * cam_data.lens / 36.0
                a = math.radians(deg)
                cam.location = target + Vector((math.sin(a) * dist, -math.cos(a) * dist, 0.0))
                cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
                scene.render.filepath = str(out / f"{tag}-{crop}-{name}.png")
                bpy.ops.render.render(write_still=True)

    arm.data.pose_position = "REST"
    shoot("rest")
    arm.data.pose_position = "POSE"
    arm.animation_data_create()
    for act in bpy.data.actions:
        take = act.name.split("|")[-1]
        if take not in ("idle", "walk", "fire"):
            continue
        arm.animation_data.action = act
        arm.animation_data.action_slot = act.slots[0]
        first, last = (int(f) for f in act.frame_range)
        for f in sorted({first, first + (last - first) // 3, first + 2 * (last - first) // 3}):
            scene.frame_set(f)
            shoot(f"{take}-f{f:03d}")


def verify(repo: Path) -> dict:
    """Stdlib: the shipped files are the ones provenance.json records, from the recorded original."""
    dest, src = repo / COLLECTION / OUT, repo / COLLECTION / SOURCE
    record = json.loads((dest / "provenance.json").read_text(encoding="utf-8"))
    check(record["inputs"][f"{SOURCE}.fbx"] == sha((src / f"{SOURCE}.fbx").read_bytes()), "C07's original FBX is not the recorded input")
    check(record["inputs"][f"{SOURCE}_basecolor.jpg"] == sha((src / f"{SOURCE}_basecolor.jpg").read_bytes()), "C07's colour map is not the recorded input")
    check(record["files"][f"{OUT}.fbx"] == sha((dest / f"{OUT}.fbx").read_bytes()), "the copy's FBX is not the recorded one")
    check((dest / f"{OUT}_basecolor.jpg").read_bytes() == (src / f"{SOURCE}_basecolor.jpg").read_bytes(), "the colour map is not Tal's byte for byte")
    check(record["files"][f"{OUT}_basecolor.jpg"] == sha((dest / f"{OUT}_basecolor.jpg").read_bytes()), "the colour map's recorded hash")
    check(record["bones"] == BONES and record["unweighted_vertices"] == 0 and set(record["takes"]) == set(TAKES), "the rig record")
    check(record["scarf_texels_in_box"]["scarf_patches"] == 0 and record["open_edges_in_box"]["new_in_copy"] <= OPEN_EDGES_ALLOWED, "the neck record")
    check(record["creator_credit"] == "Tal Heuberger" and record["licence"] == "CC-BY-NC-ND-4.0", "credit")
    return record


def write_provenance(repo: Path, inspected: dict) -> None:
    dest = repo / COLLECTION / OUT
    made = json.loads((dest / "make-report.json").read_text(encoding="utf-8"))
    record = {
        "asset": OUT, "creator_credit": "Tal Heuberger", "licence": "CC-BY-NC-ND-4.0",
        "what": f"Tal Heuberger's {SOURCE} (#507) with the keffiyeh removed, by tools/c07_nokeffiyeh.py (Blender {inspected['blender']}), under the "
                "owner's ruling of 3 November 2026 (\"no israeli soldir should wear a khafia on their neck\"); the owner confirms Tal's permission for "
                "adapted versions, 3 November 2026. The mesh is edited: the chart pieces are welded into one surface, the scarf's faces are deleted "
                "with the pieces and flaps it leaves, the holes it exposes (the sweater's collar ring and a V down the chest, a crescent at the nape, "
                "small gaps on the neck's sides) are closed with new triangles relaxed onto the surrounding body and textured from the texels of the "
                "kept faces beside them, and the new vertices are skinned from their neighbours. The kept vertices do not move; the armature, its "
                "67 bones and the nine takes are Tal's, re-exported with every frame keyed; the colour map is Tal's byte for byte. "
                "The original files are unchanged.",
        "inputs": made["inputs"],
        "files": {f"{OUT}.fbx": sha((dest / f"{OUT}.fbx").read_bytes()),
                  f"{OUT}_basecolor.jpg": sha((dest / f"{OUT}_basecolor.jpg").read_bytes())},
        "bones": inspected["bones"], "takes": inspected["takes"], "take_names": inspected["take_names"], "fps": inspected["fps"],
        "vertices": inspected["vertices"], "triangles": inspected["triangles"], "height_m": round(inspected["height_m"], 4),
        "unweighted_vertices": inspected["unweighted_vertices"], "max_influences": inspected["max_influences"],
        "scarf_texels_in_box": inspected["scarf_texels_in_box"],
        "open_edges_in_box": inspected["open_edges_in_box"],
        "source_mesh": made["source"], "welded": made["welded"],
        "scarf_faces_deleted": made["scarf_faces"], "scarf_pieces": made["scarf_pieces"], "loose_pieces_deleted": made["loose_pieces"],
        "sticks_out_deleted": made["sticks_out"], "spikes_deleted": made["spikes"], "pinches_cut": made["pinches"],
        "neck_axis_xy": made["axis_xy"], "holes_closed": made["holes"], "new_faces": made["new_faces"], "new_vertices": made["new_vertices"],
        "new_face_texels": made["texel_kinds"], "new_vertex_influences_max": made["new_vertex_influences_max"],
        "subdivisions": SUBDIVISIONS, "relax_iterations": RELAX, "collar_rib_m": COLLAR_RIB_M, "collar_rib_width_m": list(COLLAR_RIB_WIDTH_M),
        "rim_vertices_eased": made["rim_vertices_eased"], "rim_ease_passes": RIM_EASE, "rim_vertices_weighted": made["rim_vertices_weighted"],
        "loose_vertices_dropped": made["loose_vertices_dropped"], "texel_patches": made["texel_patches"],
    }
    (dest / "provenance.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    (dest / "make-report.json").unlink()   # its figures are in the record; the Collection folder holds only the runtime files


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--repo", type=Path, default=ROOT)
    p.add_argument("--make", action="store_true", help="build the keffiyeh-free FBX and the map copy (needs bpy)")
    p.add_argument("--check", action="store_true", help="re-import the copy, write provenance.json (needs bpy)")
    p.add_argument("--verify", action="store_true", help="hashes against provenance.json (stdlib)")
    p.add_argument("--out", type=Path, help="--make: write into this folder instead of the Collection; --check: read the FBX there")
    p.add_argument("--stills", type=Path, help="--check: also render stills into this folder")
    p.add_argument("--blend", type=Path, help="--make: also save the working .blend here")
    a = p.parse_args(argv)
    repo = a.repo.resolve()
    if a.make:
        make(repo, a.out, a.blend)
    elif a.check:
        folder = a.out or repo / COLLECTION / OUT
        import bpy
        report = inspect(repo, folder / f"{OUT}.fbx", folder / f"{OUT}_basecolor.jpg", a.stills)
        report["blender"] = bpy.app.version_string
        check(report["bones"] == BONES, f"{report['bones']} bones, expected {BONES}")
        check(set(report["takes"]) == set(TAKES), f"takes: {report['takes']}")
        check(report["unweighted_vertices"] == 0, f"{report['unweighted_vertices']} unweighted vertices")
        check(report["scarf_texels_in_box"]["scarf_patches"] == 0, f"the neck box still has scarf-sized patches of the scarf's texels: {report['scarf_texels_in_box']}")
        check(report["open_edges_in_box"]["new_in_copy"] <= OPEN_EDGES_ALLOWED, f"open edges in the neck box the original does not have: {report['open_edges_in_box']}")
        print(json.dumps({k: v for k, v in report.items() if k != "bone_names"}, indent=1))
        if a.out is None:
            write_provenance(repo, report)
    else:
        verify(repo)
        print("VERIFIED", OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
