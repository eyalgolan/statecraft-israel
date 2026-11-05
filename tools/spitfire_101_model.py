"""Builds the 101 Squadron Supermarine Spitfire LF Mk IXe (as flown from October 1948) as an authored unit model, in
Blender's bpy module (no paid tool, no third-party model): a smooth lofted body and a separate four-blade propeller, with
the squadron's 1948 markings PAINTED INTO ONE COLOUR MAP (no decal geometry), for UnitShowcase's art hook
(Resources/Units/Authored/spitfire-mk9e-101sqn-1948, UnitShowcase.AuthoredFolder + UnitShowcase.SpitfireType).

Owner, 5 November 2026, of the code-built Spitfire (UnitModels.Spitfire): "still doesnt look good. thr air force signs on
the sides are not on the airplanes fueoladge but appear as their own 3d obects almost seperate from the plane, and the
entire model is very low quality"; then "start the blender built 101 squadron in parralel to everything else".

How:
  1. Every part is lofted from cross-sections in pure Python (the game's frame: x right, y up, z forward, metres, the
     centre of gravity at the origin): the fuselage from 14 sections interpolated to a smooth Catmull-Rom loft, the wing
     from a true elliptical planform clipped at the clipped-wing span with a NACA 22xx section thinning from 13% to 9.4%,
     6 degrees of dihedral and the washout, the elliptical tailplane, fin and rudder, the two underwing radiators, the
     carburettor intake under the nose, the canopy and the exhaust stubs. The propeller (spinner and four blades) is a
     separate object whose pivot is the hub, turning about its local +z.
  2. The two objects are UV-unwrapped together (Smart UV Project) into one 0..1 space, and the colour map is painted by
     evaluating, for every texel, a colour function of the texel's own 3D point on the skin: camouflage, demarcation,
     roundels, rudder stripes, panel lines, exhaust soot. So each roundel lies on the skin wherever its island is.
  3. Exported as one FBX (two top-level nodes, "Body" and "Propeller", the axis conversion baked into the vertices so
     both nodes have identity rotation and scale) with no texture inside; the colour map is a PNG beside it, and the
     FBX itself sits in Resources (FBX_DIR), its importer remapping the material to the .mat beside the colour map.

    uv run --no-project --python 3.13 --with bpy==5.2.2 python -I tools/spitfire_101_model.py --build     # FBX + PNG
    uv run --no-project --python 3.13 --with bpy==5.2.2 python -I tools/spitfire_101_model.py --check --stills DIR
    python3 tools/spitfire_101_model.py --verify                                                          # stdlib hashes

--check re-imports the FBX in a fresh process (one bpy import per process), measures it, renders stills and writes
provenance.json. --verify re-hashes the files against provenance.json.

Sources (each number below carries its own tag):
  [V]  en.wikipedia "Supermarine Spitfire variants: specifications, performance and armament", LF Mk IXe entry, read
       5 November 2026: length 31 ft 1 in (9.47 m), wing area 231 sq ft (21.46 m2) (its span, 32 ft 6 in (9.9 m), is the
       page's general clipped figure, "this could vary slightly").
  [S]  en.wikipedia "Supermarine Spitfire", read 5 November 2026: span 36 ft 10 in (11.23 m) (also SeaAirCollection's
       cite); clipped tips "reduced the span by 3 ft 6 in (1.07 m)"; "A dihedral of 6° was adopted"; root NACA 2213,
       tip NACA 2209.4; incidence "+2° at its root to -½° at its tip"; a semi-elliptical wing.
  [E]  Yoav Efrati, Colors & Markings of the Israeli Air Force (IAF, 2005), as UnitModels.Spitfire and docs/units.md cite
       it: clipped wing on every 1948 Spitfire (p. 14); Ocean Grey and Dark Green over Medium Sea Grey (p. 14); red
       spinner and DIAGONAL red and white rudder stripes from early October 1948 (p. 12); filled Star of David roundels,
       no ring, on the fuselage sides and both wing surfaces (pp. 6, 14).
  [U]  docs/units.md: a four-blade propeller (the IAF Museum Mk IX at Hatzerim, Commons, CC BY 2.5).
  [P]  unity/Assets/Scripts/Map/Units/UnitPalette.cs: the colours (the same sRGB values the code-built model paints).
  DESIGN VALUE marks every number no source above gives (sections, positions, roundel sizes, stripe width ...).
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import sys
import zlib

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

TYPE = "spitfire-mk9e-101sqn-1948"                       # UnitShowcase.SpitfireType
MODEL_DIR = Path("unity/Assets/Art/Units") / TYPE         # the colour map, the material, provenance.json
# The FBX itself is what UnitShowcase.AuthoredModel loads (Resources.Load of AuthoredFolder + the type): no prefab variant.
FBX_DIR = Path("unity/Assets/Resources/Units/Authored")
FBX = f"{TYPE}.fbx"
PNG = f"{TYPE}_basecolor.png"
MATERIAL_NAME = f"{TYPE}-paint"                           # the FBX's material name, remapped to the .mat by the .fbx.meta
TEX = 1024                                                # DESIGN VALUE: colour map size (px), about 1.4 cm a texel
FIXED_TIME = (2026, 11, 5)                                # the FBX header's creation stamp, fixed so builds repeat

# ---- dimensions ----
LENGTH = 9.47                     # [V] spinner tip to rudder trailing edge
SPAN_FULL = 11.23                 # [S] the standard (unclipped) wing
CLIP = 1.07                       # [S] clipped tips cut 3 ft 6 in
SPAN = SPAN_FULL - CLIP           # 10.16 m, the clipped span (UnitModels: "about 10.2 m")
B0 = SPAN_FULL / 2                # the ellipse's semi-span
SEMI = SPAN / 2                   # the clipped semi-span
WING_AREA_FULL = 22.49            # [S] Mk Vb (standard wing) 242.1 sq ft
C0 = 4 * WING_AREA_FULL / (math.pi * SPAN_FULL)   # root chord of an elliptical wing of that area and span: 2.55 m (derived)
DIHEDRAL = 6.0                    # [S]
TC_ROOT, TC_TIP = 0.13, 0.094     # [S] NACA 2213 / 2209.4
INC_ROOT, INC_TIP = 2.0, -0.5     # [S] incidence, degrees
CAMBER, CAMBER_POS = 0.02, 0.2    # [S] NACA 22xx mean line
NOSE_Z = 3.65                     # DESIGN VALUE: spinner tip; the tail is NOSE_Z - LENGTH
TAIL_Z = NOSE_Z - LENGTH          # -5.82
HUB = (0.0, 0.02, 3.30)           # DESIGN VALUE: the propeller's pivot (game frame); UnitModels' hub is (0, 0.02, 3.55)
PROP_R = 1.635                    # DESIGN VALUE: blade tip radius (3.27 m diameter, not checked against a source)
BLADE_ANGLES = [20.0 + 90.0 * k for k in range(4)]   # [U] four blades; at 20 + 90k degrees from +y about +z (the code's)
WING_LE_SHARE = 0.42              # DESIGN VALUE: share of the chord ahead of the straight reference line (code: 1.05/2.5)
WING_ROOT_Y = -0.45               # DESIGN VALUE: the root chord line, a low wing
# The wing's reference line puts the root quarter chord (about the centre of gravity) at z = 0.
WING_ZREF = -(WING_LE_SHARE - 0.25) * C0

# Fuselage sections (z, half width, y of the widest line, top y, bottom y): DESIGN VALUES fitted to Mk IX proportions.
FUSELAGE = [
    (3.10, 0.300, 0.02, 0.32, -0.28),
    (2.80, 0.360, 0.02, 0.40, -0.36),
    (2.20, 0.400, 0.02, 0.45, -0.44),
    (1.40, 0.420, 0.02, 0.48, -0.52),
    (0.80, 0.430, 0.00, 0.52, -0.60),
    (0.00, 0.420, 0.00, 0.53, -0.64),
    (-0.80, 0.400, 0.00, 0.52, -0.62),
    (-1.60, 0.360, 0.00, 0.49, -0.56),
    (-2.50, 0.300, 0.02, 0.44, -0.45),
    (-3.40, 0.230, 0.04, 0.39, -0.33),
    (-4.30, 0.160, 0.06, 0.34, -0.21),
    (-5.00, 0.100, 0.08, 0.30, -0.11),
    (-5.40, 0.060, 0.10, 0.27, -0.05),
    (-5.66, 0.020, 0.12, 0.25, 0.00),
]
SECTION_EXP = 2.3                 # DESIGN VALUE: superellipse exponent of the fuselage sections (2 = ellipse)
RING = 28                         # points round a fuselage section

# Markings (DESIGN VALUES where [E] gives no position or size).
FUS_ROUNDEL = (-2.30, 0.00, 0.38)          # z, y, radius on both fuselage sides
WING_ROUNDEL_X, WING_ROUNDEL_R = 3.25, 0.50
STAR_SHARE = 0.74                           # UnitModels.RoundelStarShare (FOTW / Efrati pp. 11-14)
RUDDER_HINGE_Z = -5.18
STRIPE_PERIOD = 0.30

# Colours: UnitPalette.Base, sRGB [P].
COL = {
    "CamoGrey": (0.47, 0.50, 0.50), "CamoGreen": (0.27, 0.32, 0.22), "Underside": (0.68, 0.71, 0.72),
    "RoundelWhite": (0.95, 0.95, 0.93), "RoundelBlue": (0.07, 0.17, 0.45), "PropBlack": (0.08, 0.08, 0.08),
    "Glass": (0.20, 0.26, 0.30), "MarkingRed": (0.72, 0.12, 0.10),
}

PARTS = ["fuselage", "wing", "tail", "fin", "radiator", "canopy", "intake", "exhaust", "spinner", "blade"]


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


# ---- geometry (pure Python, game frame) ----

class Part:
    """A closed mesh in the game frame: vertices, quads/triangles and one part tag."""

    def __init__(self, name: str):
        self.name = name
        self.v: list[tuple[float, float, float]] = []
        self.f: list[tuple[int, ...]] = []

    def loft(self, rings: list[list[tuple[float, float, float]]], cap_start=True, cap_end=True) -> None:
        base = len(self.v)
        m = len(rings[0])
        for r in rings:
            check(len(r) == m, "rings differ in size")
            self.v.extend(r)
        for i in range(len(rings) - 1):
            a, b = base + i * m, base + (i + 1) * m
            for j in range(m):
                k = (j + 1) % m
                self.f.append((a + j, a + k, b + k, b + j))
        for ring_index, cap in ((0, cap_start), (len(rings) - 1, cap_end)):
            if not cap:
                continue
            r = rings[ring_index]
            c = tuple(sum(p[t] for p in r) / m for t in range(3))
            ci = len(self.v)
            self.v.append(c)
            o = base + ring_index * m
            for j in range(m):
                self.f.append((o + j, o + (j + 1) % m, ci))


def catmull(points: list[tuple[float, ...]], samples: int) -> list[tuple[float, ...]]:
    """Catmull-Rom through the rows (centripetal-free, uniform), `samples` points from the first row to the last."""
    n = len(points)
    out = []
    for s in range(samples):
        t = s * (n - 1) / (samples - 1)
        i = min(int(t), n - 2)
        u = t - i
        p0, p1, p2, p3 = points[max(i - 1, 0)], points[i], points[i + 1], points[min(i + 2, n - 1)]
        out.append(tuple(0.5 * (2 * b + (-a + c) * u + (2 * a - 5 * b + 4 * c - d) * u * u + (-a + 3 * b - 3 * c + d) * u ** 3)
                         for a, b, c, d in zip(p0, p1, p2, p3)))
    return out


def superellipse_ring(z, hw, ym, top, bot, n=RING, exp=SECTION_EXP):
    pts = []
    for j in range(n):
        phi = 2 * math.pi * j / n          # 0 at the top, clockwise seen from behind... direction is fixed by recalc later
        s, c = math.sin(phi), math.cos(phi)
        x = hw * math.copysign(abs(s) ** (2 / exp), s)
        r = (top - ym) if c >= 0 else (ym - bot)
        y = ym + r * math.copysign(abs(c) ** (2 / exp), c)
        pts.append((x, y, z))
    return pts


def fuselage_rows(samples=30):
    return catmull(FUSELAGE, samples)


def fuselage_at(z):
    """The interpolated section (hw, ym, top, bot) at z (for placing the canopy, intake and exhausts)."""
    rows = fuselage_rows(200)
    best = min(rows, key=lambda r: abs(r[0] - z))
    return best[1:]


def build_fuselage() -> Part:
    p = Part("fuselage")
    rows = fuselage_rows(30)
    p.loft([superellipse_ring(*r) for r in rows])
    return p


def naca(x, tc, camber=CAMBER, pos=CAMBER_POS):
    """Upper and lower (x, y) of a NACA 4-digit section at chord share x (closed trailing edge)."""
    yt = 5 * tc * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4)
    if camber == 0:
        return (x, yt), (x, -yt)
    if x < pos:
        yc = camber / pos ** 2 * (2 * pos * x - x * x)
        dy = 2 * camber / pos ** 2 * (pos - x)
    else:
        yc = camber / (1 - pos) ** 2 * ((1 - 2 * pos) + 2 * pos * x - x * x)
        dy = 2 * camber / (1 - pos) ** 2 * (pos - x)
    th = math.atan(dy)
    return (x - yt * math.sin(th), yc + yt * math.cos(th)), (x + yt * math.sin(th), yc - yt * math.cos(th))


def section(tc, k=13, camber=CAMBER):
    """A closed airfoil ring (chord 0..1 from the leading edge, thickness up), starting at the trailing edge."""
    xs = [(1 - math.cos(math.pi * i / (k - 1))) / 2 for i in range(k)]
    upper = [naca(x, tc, camber)[0] for x in xs]
    lower = [naca(x, tc, camber)[1] for x in xs]
    ring = [upper[i] for i in range(k - 1, -1, -1)]          # TE -> LE along the top
    ring += [lower[i] for i in range(1, k - 1)]              # LE -> TE along the bottom, TE shared
    return ring


def surface(name, span_stations, chord_at, le_at, tc_at, place, camber=CAMBER, tip_round=True) -> Part:
    """A lifting surface: for each span position s, a section of chord chord_at(s) whose leading edge is at le_at(s)
    (a z), thickness tc_at(s), and `place(s, xc, yt, z)` -> the game point (xc: chord share, yt: thickness offset in metres
    perpendicular to the chord, z: chordwise z before twist). The last station is followed by a rounded tip."""
    p = Part(name)
    rings = []
    stations = list(span_stations)
    for s in stations:
        c, le, tc = chord_at(s), le_at(s), tc_at(s)
        rings.append([place(s, x, y * c, le - x * c) for x, y in section(tc, camber=camber)])
    if tip_round:
        s = stations[-1]
        c, le, tc = chord_at(s) * 0.88, le_at(s) - chord_at(s) * 0.05, tc_at(s) * 0.55
        rings.append([place(s + 0.05, x, y * c, le - x * c) for x, y in section(tc, camber=camber)])
    p.loft(rings, cap_start=True, cap_end=True)
    return p


def wing_chord(s):
    return C0 * math.sqrt(max(0.0, 1 - (s / B0) ** 2))


def wing_le(s):
    return WING_ZREF + WING_LE_SHARE * wing_chord(s)


def wing_tc(s):
    return TC_ROOT + (TC_TIP - TC_ROOT) * s / B0


def build_wing(side: float) -> Part:
    # The rounded tip fairing (surface's last ring) adds 0.05 m, so the span ends at the clipped span.
    stations = [(SEMI - 0.05) * math.sin(0.5 * math.pi * i / 15) for i in range(16)]

    def place(s, xc, yt, z):
        c = wing_chord(s)
        inc = math.radians(INC_ROOT + (INC_TIP - INC_ROOT) * s / B0)
        qz = wing_le(s) - 0.25 * c                          # twist about the quarter chord
        dz, dy = z - qz, yt
        z2 = qz + dz * math.cos(inc) - dy * math.sin(inc)   # positive incidence lifts the leading edge
        y2 = dy * math.cos(inc) + dz * math.sin(inc)
        y = WING_ROOT_Y + s * math.tan(math.radians(DIHEDRAL)) + y2
        return (side * s, y, z2)

    return surface("wing", stations, wing_chord, wing_le, wing_tc, place)


TAIL_SEMI, TAIL_C0, TAIL_ZREF, TAIL_Y = 1.60, 1.05, -5.05, 0.14     # DESIGN VALUES
FIN_H, FIN_C0, FIN_Y0 = 1.25, 1.50, 0.18                           # DESIGN VALUES
FIN_ZREF = TAIL_Z + 0.55 * FIN_C0                                  # the rudder's root trailing edge is the tail


def build_tailplane(side: float) -> Part:
    b = TAIL_SEMI + 0.25
    chord = lambda s: TAIL_C0 * math.sqrt(max(0.0, 1 - (s / b) ** 2))
    le = lambda s: TAIL_ZREF + 0.40 * chord(s)
    stations = [TAIL_SEMI * math.sin(0.5 * math.pi * i / 8) for i in range(9)]
    return surface("tail", stations, chord, le, lambda s: 0.10,
                   lambda s, xc, yt, z: (side * s, TAIL_Y + yt, z), camber=0.0)


def build_fin() -> Part:
    b = FIN_H + 0.12
    chord = lambda s: FIN_C0 * math.sqrt(max(0.0, 1 - (s / b) ** 2))
    le = lambda s: FIN_ZREF + 0.45 * chord(s)
    stations = [FIN_H * math.sin(0.5 * math.pi * i / 10) for i in range(11)]
    # Span along +y; the section's thickness along x.
    return surface("fin", stations, chord, le, lambda s: 0.10,
                   lambda s, xc, yt, z: (yt, FIN_Y0 + s, z), camber=0.0)


def build_radiator(side: float) -> Part:
    """The Mk IX's radiator under each wing (DESIGN VALUES for size and place): a rounded duct hung under the wing."""
    p = Part("radiator")
    x0, z_front, z_back, half_w, depth = side * 1.70, 0.20, -1.15, 0.24, 0.28
    s = abs(x0)
    def wing_bottom(z):   # the wing's lower surface there, near enough: the chord line less 40% of the thickness
        return WING_ROOT_Y + s * math.tan(math.radians(DIHEDRAL)) - 0.4 * wing_tc(s) * wing_chord(s)
    rows = []
    for i in range(9):
        t = i / 8
        z = z_front + (z_back - z_front) * t
        shape = math.sin(math.pi * (0.08 + 0.84 * t)) ** 0.35        # a lip at the front, a taper at the back
        top = wing_bottom(z) + 0.10
        bot = wing_bottom(z) - depth * shape
        rows.append(superellipse_ring(z, half_w * (0.75 + 0.25 * shape), (top + bot) / 2, top, bot, n=16, exp=4.0))
    for r in rows:
        r[:] = [(x + x0, y, z) for x, y, z in r]
    p.loft(rows)
    return p


def build_intake() -> Part:
    """The carburettor air intake under the nose (DESIGN VALUES)."""
    p = Part("intake")
    rows = []
    for i in range(8):
        t = i / 7
        z = 2.35 - 1.50 * t
        _, _, _, bot = fuselage_at(z)
        shape = math.sin(math.pi * (0.10 + 0.85 * t)) ** 0.5
        rows.append(superellipse_ring(z, 0.15 * (0.6 + 0.4 * shape), bot - 0.02, bot + 0.10, bot - 0.14 * shape, n=14, exp=3.0))
    p.loft(rows)
    return p


def build_canopy() -> Part:
    """The canopy and windscreen: upper half-sections standing on the fuselage top (DESIGN VALUES)."""
    p = Part("canopy")
    prof = [(0.55, 0.20, 0.00), (0.38, 0.24, 0.20), (0.15, 0.27, 0.31), (-0.30, 0.27, 0.34), (-0.75, 0.24, 0.27),
            (-1.05, 0.20, 0.12), (-1.30, 0.17, 0.01)]
    rows = catmull(prof, 12)
    rings = []
    for z, hw, h in rows:
        _, _, top, _ = fuselage_at(z)
        base = top - 0.05
        ring = []
        n = 16
        for j in range(n + 1):
            a = math.pi * j / n                      # 0..pi over the top
            x = hw * math.cos(a)
            x = math.copysign(abs(x) ** (2 / 2.4) * hw ** (1 - 2 / 2.4), x) if hw > 0 else 0.0
            y = base + max(h, 0.01) * math.sin(a) ** (2 / 2.4)
            ring.append((x, y, z))
        ring.append((0.0, base - 0.02, z))           # close the section under the glass, inside the fuselage
        rings.append(ring)
    p.loft(rings)
    return p


def build_exhausts() -> Part:
    """Six exhaust stubs a side (DESIGN VALUES): small fishtail boxes along the cowling."""
    p = Part("exhaust")
    for side in (1, -1):
        for i in range(6):
            z = 2.55 - 0.21 * i
            hw, ym, top, bot = fuselage_at(z)
            x = side * (hw * 0.93)
            y = 0.20
            rows = []
            for zz, w in ((z + 0.05, 0.035), (z - 0.06, 0.05)):
                rows.append([(x + side * dx, y + dy, zz) for dx, dy in ((-0.02, -w), (0.07, -w), (0.07, w), (-0.02, w))])
            p.loft(rows)
    return p


def build_propeller() -> tuple[Part, Part]:
    """The spinner and four blades about the hub (the propeller's own frame: the hub at the origin, turning about +z)."""
    spin = Part("spinner")
    rows = []
    length, radius = NOSE_Z - (HUB[2] - 0.25), 0.30
    for i in range(14):
        t = i / 13
        z = NOSE_Z - HUB[2] - length * t                     # tip to back
        r = radius * math.sqrt(max(0.0, 1 - (1 - t) ** 2)) if i else 0.0
        rows.append([(max(r, 0.004) * math.sin(2 * math.pi * j / 20), max(r, 0.004) * math.cos(2 * math.pi * j / 20), z) for j in range(20)])
    spin.loft(rows)
    blades = Part("blade")
    for ang in BLADE_ANGLES:
        a = math.radians(ang)
        d = (-math.sin(a), math.cos(a), 0.0)                  # Quaternion.Euler(0, 0, ang) * up, as UnitModels turns them
        t = (-d[1], d[0], 0.0)                                 # in the disc, at right angles to the blade
        rings = []
        for i in range(9):
            u = i / 8
            r = 0.18 + (PROP_R - 0.18) * u
            chord = 0.20 + 0.10 * math.sin(math.pi * min(u / 0.8, 1.0)) - 0.10 * max(0.0, (u - 0.8) / 0.2) ** 2
            pitch = math.radians(48 - 32 * u)                  # DESIGN VALUE: blade angle root to tip
            tc = 0.20 - 0.12 * u
            ring = []
            for x, y in section(tc, k=7, camber=0.0):
                cx, ty = (x - 0.35) * chord, y * chord
                # chord along the disc tangent, turned by the pitch toward +z; thickness along the other axis
                ct = cx * math.cos(pitch) - ty * math.sin(pitch)
                cz = cx * math.sin(pitch) + ty * math.cos(pitch)
                ring.append((d[0] * r + t[0] * ct, d[1] * r + t[1] * ct, -cz))
            rings.append(ring)
        blades.loft(rings)
    return spin, blades


def build_body_parts() -> list[Part]:
    parts = [build_fuselage(), build_wing(1), build_wing(-1), build_tailplane(1), build_tailplane(-1), build_fin(),
             build_radiator(1), build_radiator(-1), build_intake(), build_canopy(), build_exhausts()]
    return parts


# ---- paint (numpy, game frame) ----

def smooth(edge, width):
    import numpy as np
    return np.clip(0.5 - edge / width, 0.0, 1.0)


def star_sd(u, v, radius, point_up=True):
    """Signed distance (m, <0 inside) of the filled Star of David of circumradius STAR_SHARE * radius, a point along +v."""
    import numpy as np
    R = STAR_SHARE * radius
    def tri(base_deg):
        d = None
        for k in range(3):
            a = math.radians(base_deg + 120 * k)
            e = u * math.cos(a) + v * math.sin(a) - R / 2
            d = e if d is None else np.maximum(d, e)
        return d
    return np.minimum(tri(270.0), tri(90.0))   # edge normals of the up- and the down-pointing triangle


def paint(pos, nrm, part, texel_m):
    """sRGB colours (N x 3) for texels at game points `pos` with face normals `nrm` and part indices `part`."""
    import numpy as np
    x, y, z = pos[:, 0], pos[:, 1], pos[:, 2]
    nx, ny, nz = nrm[:, 0], nrm[:, 1], nrm[:, 2]
    P = {n: part == i for i, n in enumerate(PARTS)}
    c = lambda k: np.array(COL[k])
    out = np.zeros((len(pos), 3))
    aa = 1.5 * texel_m

    # Upper camouflage: a smooth 3D field, thresholded into large curving patches (DESIGN VALUE pattern).
    f = (np.sin(0.95 * x + 0.55 * z + 0.3) + np.sin(-0.45 * x + 1.05 * z + 1.9 + 0.8 * y)
         + 0.6 * np.sin(1.7 * x - 0.9 * z + 2.0 * y + 4.1) + 0.35 * np.sin(2.6 * z + 1.3 * x - 0.7))
    green = smooth(-f, 0.08)[:, None]
    upper = green * c("CamoGreen") + (1 - green) * c("CamoGrey")

    # Demarcation: wings and tailplane by their face (upper surfaces face up); the fuselage below a line that runs from
    # under the nose, past the wing root, to mid-height at the tail (DESIGN VALUE line).
    ydem = np.interp(z, [TAIL_Z, -2.0, 0.0, 3.2], [0.06, -0.22, -0.34, -0.22])
    lower_fus = smooth(y - ydem, 0.02)
    lower_wing = smooth(ny, 0.15)
    lower = (P["wing"] | P["tail"]) * lower_wing + (P["fuselage"]) * lower_fus + (P["radiator"] | P["intake"]) * 1.0
    lower = lower[:, None]
    out = lower * c("Underside") + (1 - lower) * upper

    # Radiator faces (front and back openings): dark matrix.
    rad_face = P["radiator"] & (np.abs(nz) > 0.6)
    out[rad_face] = (0.12, 0.12, 0.12)
    out[P["exhaust"]] = (0.10, 0.09, 0.08)
    out[P["spinner"]] = c("MarkingRed")
    out[P["blade"]] = c("PropBlack")

    # Panel lines (DESIGN VALUES): a few frames and the control-surface hinges, a quarter darker.
    line = np.zeros(len(pos), bool)
    for zf in (2.75, 1.05, -1.35, -3.35):
        line |= P["fuselage"] & (np.abs(z - zf) < 0.012)
    s = np.abs(x)
    chord = C0 * np.sqrt(np.clip(1 - (s / B0) ** 2, 0, 1))
    te = WING_ZREF - (1 - WING_LE_SHARE) * chord
    aileron = P["wing"] & (s > 2.9) & (s < 4.95) & (np.abs(z - (te + 0.24 * chord)) < 0.012)
    flap = P["wing"] & (ny < 0) & (s > 0.45) & (s < 2.75) & (np.abs(z - (te + 0.20 * chord)) < 0.012)
    span_breaks = P["wing"] & (np.abs(z - te) < 0.25 * chord + 0.01) & ((np.abs(s - 2.9) < 0.012) | (np.abs(s - 4.95) < 0.012))
    rudder_hinge = P["fin"] & (np.abs(z - RUDDER_HINGE_Z) < 0.012)
    tail_chord = TAIL_C0 * np.sqrt(np.clip(1 - (s / (TAIL_SEMI + 0.25)) ** 2, 0, 1))
    elev = P["tail"] & (np.abs(z - (TAIL_ZREF - 0.60 * tail_chord + 0.35 * tail_chord)) < 0.012)
    line |= aileron | flap | span_breaks | rudder_hinge | elev
    out[line] *= 0.75

    # Exhaust soot along the cowling sides behind the stubs, fading aft.
    soot = P["fuselage"] & (np.abs(nx) > 0.3) & (z < 2.65) & (z > 0.4)
    k = np.where(soot, np.clip(1 - np.abs(y - 0.20) / 0.13, 0, 1) * np.clip((z - 0.4) / 2.2, 0, 1) * 0.45, 0)[:, None]
    out = out * (1 - k) + k * np.array((0.10, 0.09, 0.08))

    # Rudder: diagonal red and white stripes on both sides behind the hinge ([E] p. 12; angle, width DESIGN VALUES).
    rud = P["fin"] & (z < RUDDER_HINGE_Z)
    d = (y - z) / math.sqrt(2)
    red = (np.mod(d / STRIPE_PERIOD, 1.0) < 0.5)
    out[rud & red] = c("MarkingRed")
    out[rud & ~red] = c("RoundelWhite")

    # Roundels: white disc, filled blue star, no ring ([E] pp. 6, 14). Fuselage sides: a point straight up.
    def roundel(mask, u, v, radius):
        disc = np.sqrt(u * u + v * v) - radius
        w = smooth(disc, aa)[:, None] * mask[:, None]
        b = smooth(star_sd(u, v, radius), aa)[:, None] * mask[:, None]
        nonlocal out
        out = out * (1 - w) + w * c("RoundelWhite")
        out = out * (1 - b) + b * c("RoundelBlue")

    zc, yc, rr = FUS_ROUNDEL
    for side in (1, -1):
        m = P["fuselage"] & (side * nx > 0.25) & (side * x > 0)
        roundel(m, z - zc, y - yc, rr)
        # Wings: on both surfaces, a point toward the nose (DESIGN VALUE orientation), centred on the local mid chord.
        cz = WING_ZREF + (WING_LE_SHARE - 0.5) * wing_chord(WING_ROUNDEL_X)
        m = P["wing"] & (side * x > 0)
        roundel(m, side * x - WING_ROUNDEL_X, z - cz, WING_ROUNDEL_R)

    # Canopy glass with its frames (camouflage green) at the windscreen, the hood's rear and along the bottom rail.
    can = P["canopy"]
    frame = can & ((np.abs(z - 0.36) < 0.025) | (np.abs(z - (-0.95)) < 0.02) | (np.abs(z - 0.10) < 0.012))
    out[can] = c("Glass") * (0.85 + 0.35 * np.clip(ny[can], 0, 1))[:, None]
    out[frame] = c("CamoGreen")

    # A faint deterministic mottle (+-3%) so the paint is not flat.
    mott = 1 + 0.03 * np.sin(13.1 * x + 7.3 * y + 5.7 * z) * np.sin(4.9 * x - 11.3 * z + 2.1)
    return np.clip(out * mott[:, None], 0, 1)


def write_png(path: Path, rgb) -> None:
    """A deterministic 8-bit RGB PNG (stdlib zlib, level 9, filter Sub on every row)."""
    import numpy as np
    h, w, _ = rgb.shape
    a = (np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8)
    flipped = a[::-1]                                     # rows top-down in the file; v = 0 is the bottom row
    sub = flipped.astype(np.int16)
    sub[:, 1:] = (flipped[:, 1:].astype(np.int16) - flipped[:, :-1].astype(np.int16)) % 256
    raw = b"".join(b"\x01" + sub[r].astype(np.uint8).tobytes() for r in range(h))
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
    data += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    path.write_bytes(data)


# ---- Blender ----

def to_blender(p):
    """Game frame (x right, y up, z forward) to Blender's (the FBX export's -Z forward, Y up, and Unity's X mirror bring
    it back): Blender (-x, -z, y)."""
    return (-p[0], -p[2], p[1])


def make_object(bpy, bmesh, name, parts, origin=(0.0, 0.0, 0.0)):
    """One mesh object from the parts (each part's normals made outward on its own), its origin at `origin` (game)."""
    bm = bmesh.new()
    tag = bm.faces.layers.int.new("part")
    for part in parts:
        sub = bmesh.new()
        vs = [sub.verts.new(to_blender((v[0] - origin[0], v[1] - origin[1], v[2] - origin[2]))) for v in part.v]
        for f in part.f:
            try:
                sub.faces.new([vs[i] for i in f])
            except ValueError:
                pass
        bmesh.ops.remove_doubles(sub, verts=sub.verts, dist=1e-6)
        bmesh.ops.recalc_face_normals(sub, faces=sub.faces)
        me = bpy.data.meshes.new("tmp")
        sub.to_mesh(me)
        sub.free()
        before = len(bm.faces)
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        idx = PARTS.index(part.name)
        for i in range(before, len(bm.faces)):
            bm.faces[i][tag] = idx
        bpy.data.meshes.remove(me)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    for fc in bm.faces:
        fc.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > math.radians(48):
            e.smooth = False
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = to_blender(origin)
    return ob


def build(repo: Path, out: Path | None) -> dict:
    import bpy
    import bmesh
    import numpy as np

    bpy.ops.wm.read_factory_settings(use_empty=True)
    dest = out or repo / MODEL_DIR
    dest.mkdir(parents=True, exist_ok=True)

    body = make_object(bpy, bmesh, "Body", build_body_parts())
    spin, blades = build_propeller()
    prop = make_object(bpy, bmesh, "Propeller", [spin, blades])
    prop.location = to_blender(HUB)
    # (the parts were built in the propeller's own frame; the object sits at the hub)

    # The part tag per face -> kept for the painter; the parts were tagged in order of creation.
    # UVs: both objects unwrapped together into one space.
    for ob in (body, prop):
        ob.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.004, area_weight=0.0, correct_aspect=True,
                             scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")

    mat = bpy.data.materials.new(MATERIAL_NAME)
    for ob in (body, prop):
        ob.data.materials.append(mat)

    # Paint: rasterise every triangle into the map, evaluating the colour at its texels' 3D points.
    img = np.zeros((TEX, TEX, 3))
    filled = np.zeros((TEX, TEX), bool)
    positions, normals, parts, pix = [], [], [], []
    for ob, origin in ((body, (0, 0, 0)), (prop, HUB)):
        me = ob.data
        me.calc_loop_triangles()
        uv = me.uv_layers.active.data
        tags = me.attributes["part"].data
        for tri in me.loop_triangles:
            uvs = np.array([uv[l].uv[:] for l in tri.loops]) * TEX
            vb = [me.vertices[i].co for i in tri.vertices]
            g = np.array([(-v.x + origin[0], v.z + origin[1], -v.y + origin[2]) for v in vb])   # Blender -> game
            n = np.cross(g[1] - g[0], g[2] - g[0])
            nn = np.linalg.norm(n)
            if nn < 1e-12:
                continue
            n = n / nn
            # the game frame is mirrored from Blender's, so the cross product's sign flips; point it away from the part
            bl_n = tri.normal
            n_game = np.array((-bl_n.x, bl_n.z, -bl_n.y))
            x0, y0 = np.floor(uvs.min(0)).astype(int)
            x1, y1 = np.ceil(uvs.max(0)).astype(int)
            x0, y0 = max(x0 - 1, 0), max(y0 - 1, 0)
            x1, y1 = min(x1 + 1, TEX - 1), min(y1 + 1, TEX - 1)
            xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            a, b, c = uvs
            det = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
            if abs(det) < 1e-12:
                continue
            l1 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / det
            l2 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / det
            l3 = 1 - l1 - l2
            eps = -0.6 / max(1.0, math.sqrt(abs(det)))    # a little past the edges, so seams have no gaps
            inside = (l1 >= eps) & (l2 >= eps) & (l3 >= eps)
            if not inside.any():
                continue
            w = np.stack([l1[inside], l2[inside], l3[inside]], 1)
            positions.append(w @ g)
            normals.append(np.repeat(n_game[None], len(w), 0))
            parts.append(np.full(len(w), tags[tri.polygon_index].value))
            pix.append(np.stack([ys[inside].astype(int), xs[inside].astype(int)], 1))
    pos = np.concatenate(positions)
    nrm = np.concatenate(normals)
    prt = np.concatenate(parts)
    px = np.concatenate(pix)
    area = sum(p.area for p in body.data.polygons) + sum(p.area for p in prop.data.polygons)
    texel_m = math.sqrt(area / max(1, len(np.unique(px[:, 0] * TEX + px[:, 1]))))
    col = paint(pos, nrm, prt, texel_m)
    img[px[:, 0], px[:, 1]] = col
    filled[px[:, 0], px[:, 1]] = True
    # Dilate into the gutters, so mipmaps never pull in the background.
    for _ in range(12):
        if filled.all():
            break
        acc = np.zeros_like(img)
        cnt = np.zeros(filled.shape)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            sh = np.roll(np.roll(filled, dy, 0), dx, 1)
            acc += np.roll(np.roll(img, dy, 0), dx, 1) * sh[..., None]
            cnt += sh
        grow = (~filled) & (cnt > 0)
        img[grow] = acc[grow] / cnt[grow][:, None]
        filled |= grow
    img[~filled] = COL["Underside"]
    write_png(dest / PNG, img)

    # Export: two top-level nodes, the axis conversion baked into the vertices (identity node rotations), no texture.
    from io_scene_fbx import export_fbx_bin, fbx_utils
    import datetime
    # The exporter's object ids come from hash() of their names, which Python salts per process (and -I ignores
    # PYTHONHASHSEED): a stable hash instead, so the same build writes the same bytes.
    fbx_utils.hash = lambda key: int.from_bytes(hashlib.sha256(repr(key).encode("utf-8")).digest()[:8], "big")
    original = export_fbx_bin.fbx_header_elements
    export_fbx_bin.fbx_header_elements = lambda root, scene_data, time=None: original(root, scene_data, datetime.datetime(*FIXED_TIME))
    bpy.ops.object.select_all(action="DESELECT")
    for ob in (body, prop):
        ob.select_set(True)
    fbx = (out or repo / FBX_DIR) / FBX
    fbx.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(fbx), use_selection=True, object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
        bake_space_transform=True, apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="OFF", use_mesh_modifiers=False,
        use_tspace=False, use_custom_props=False, path_mode="STRIP", embed_textures=False, add_leaf_bones=False,
        bake_anim=False, use_triangles=False)   # already triangles; a triangulating copy would rename the meshes
    export_fbx_bin.fbx_header_elements = original
    import slim_collection as privacy
    privacy.scrub(fbx, repo)
    tris = sum(len(ob.data.polygons) for ob in (body, prop))
    report = {"triangles": tris, "body_triangles": len(body.data.polygons), "propeller_triangles": len(prop.data.polygons),
              "fbx_sha256": sha(fbx.read_bytes()), "png_sha256": sha((dest / PNG).read_bytes()), "texel_m": round(texel_m, 4)}
    print("BUILT", json.dumps(report))
    return report


# ---- the FBX as Unity reads it (stdlib) ----

def fbx_tree(path: Path) -> list:
    """The binary FBX's node tree: [(name, props, children)], read with the stdlib (FBX 7.x, 32-bit offsets)."""
    data = path.read_bytes()
    check(data[:21] == b"Kaydara FBX Binary  \x00", "not a binary FBX")
    check(struct.unpack_from("<I", data, 23)[0] < 7500, "64-bit-offset FBX not handled")

    def prop(o):
        t = chr(data[o]); o += 1
        scalar = {"Y": "<h", "C": "<?", "I": "<i", "F": "<f", "D": "<d", "L": "<q"}
        if t in scalar:
            v = struct.unpack_from(scalar[t], data, o)[0]
            return v, o + struct.calcsize(scalar[t])
        if t in "SR":
            n = struct.unpack_from("<I", data, o)[0]
            return data[o + 4:o + 4 + n], o + 4 + n
        n, enc, clen = struct.unpack_from("<III", data, o)
        raw = data[o + 12:o + 12 + clen]
        raw = zlib.decompress(raw) if enc else raw
        fmt = {"f": "f", "d": "d", "l": "q", "i": "i", "b": "?"}[t]
        return list(struct.unpack("<%d%s" % (n, fmt), raw)), o + 12 + clen

    def node(o):
        end, nprops, _plen, nlen = struct.unpack_from("<IIIB", data, o)
        if end == 0:
            return None, o + 13
        name = data[o + 13:o + 13 + nlen].decode()
        q = o + 13 + nlen
        props = []
        for _ in range(nprops):
            v, q = prop(q)
            props.append(v)
        kids = []
        while q < end:
            k, q = node(q)
            if k is None:
                break
            kids.append(k)
        return (name, props, kids), end

    out, o = [], 27
    while o < len(data) - 13:
        n, o = node(o)
        if n is None:
            break
        out.append(n)
    return out


def fbx_summary(path: Path) -> dict:
    """The FBX's axes and each model node's local transform and vertex box, in the file's own (Y-up) frame; Unity
    then mirrors x (its left-handed frame)."""
    tree = fbx_tree(path)
    top = {n[0]: n for n in tree}
    def props70(n):
        for k in n[2]:
            if k[0] == "Properties70":
                return {p[1][0].decode(): p[1][4:] for p in k[2] if p[0] == "P"}
        return {}
    gs = props70(top["GlobalSettings"])
    models, geoms = {}, {}
    for n in top["Objects"][2]:
        name = n[1][1].split(b"\x00")[0].decode() if len(n[1]) > 1 else ""
        if n[0] == "Model":
            p = props70(n)
            models[name] = {k: [round(v, 6) for v in p.get(k, [0.0, 0.0, 0.0] if k != "Lcl Scaling" else [1.0, 1.0, 1.0])]
                            for k in ("Lcl Translation", "Lcl Rotation", "Lcl Scaling", "PreRotation", "PostRotation")}
        elif n[0] == "Geometry":
            vs = next(k[1][0] for k in n[2] if k[0] == "Vertices")
            geoms[name] = [[round(min(vs[i::3]), 4) for i in range(3)], [round(max(vs[i::3]), 4) for i in range(3)]]
    return {"UpAxis": gs.get("UpAxis"), "UpAxisSign": gs.get("UpAxisSign"), "FrontAxis": gs.get("FrontAxis"),
            "FrontAxisSign": gs.get("FrontAxisSign"), "CoordAxis": gs.get("CoordAxis"), "CoordAxisSign": gs.get("CoordAxisSign"),
            "UnitScaleFactor": gs.get("UnitScaleFactor"), "models": models, "geometry_boxes": geoms}


def blade_angles(path: Path) -> list[float]:
    """The propeller's blade-tip directions in Unity's frame, as the z angle Quaternion.Euler(0, 0, a) * Vector3.up
    gives (degrees, sorted): from the FBX's Propeller vertices beyond 95% of the tip radius, x mirrored as Unity does."""
    top = {n[0]: n for n in fbx_tree(path)}
    for n in top["Objects"][2]:
        if n[0] == "Geometry" and n[1][1].split(b"\x00")[0] == b"Propeller":
            vs = next(k[1][0] for k in n[2] if k[0] == "Vertices")
    pts = [(-vs[i], vs[i + 1]) for i in range(0, len(vs), 3)]          # Unity x, y
    far = [(x, y) for x, y in pts if math.hypot(x, y) > 0.95 * PROP_R]
    angles = sorted({round(math.degrees(math.atan2(-x, y)) % 360.0 / 5.0) * 5.0 % 360.0 for x, y in far})
    return angles


# ---- check (fresh process): re-import, measure, stills ----

def inspect(repo: Path, folder: Path, fbx_file: Path, stills: Path | None) -> dict:
    import bpy
    from mathutils import Vector

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(fbx_file))
    obs = {o.name: o for o in bpy.data.objects if o.type == "MESH"}
    check(set(obs) == {"Body", "Propeller"}, f"objects: {sorted(obs)}")
    for o in obs.values():
        check(o.parent is None, f"{o.name} is not top level")

    def game_box(o):
        pts = [o.matrix_world @ v.co for v in o.data.vertices]
        g = [(-p.x, p.z, -p.y) for p in pts]          # Blender -> game
        return [min(p[i] for p in g) for i in range(3)], [max(p[i] for p in g) for i in range(3)]
    tris = {}
    for n, o in obs.items():
        o.data.calc_loop_triangles()
        tris[n] = len(o.data.loop_triangles)
    bmin, bmax = game_box(obs["Body"])
    pmin, pmax = game_box(obs["Propeller"])
    hub = obs["Propeller"].matrix_world.translation
    report = {
        "blender": bpy.app.version_string,
        "triangles": sum(tris.values()), "triangles_by_object": tris,
        "span_m": round(bmax[0] - bmin[0], 3), "length_m": round(max(bmax[2], pmax[2]) - bmin[2], 3),
        "height_m_body": round(bmax[1] - bmin[1], 3),
        "body_box_game_m": [[round(v, 3) for v in bmin], [round(v, 3) for v in bmax]],
        "propeller_box_game_m": [[round(v, 3) for v in pmin], [round(v, 3) for v in pmax]],
        "hub_game_m": [round(-hub.x, 3) + 0.0, round(hub.z, 3) + 0.0, round(-hub.y, 3) + 0.0],
        "uv_layers": {n: len(o.data.uv_layers) for n, o in obs.items()},
        "materials": sorted({m.name for o in obs.values() for m in o.data.materials if m}),
    }
    if stills:
        render_stills(bpy, obs, folder / PNG, stills)
    return report


def render_stills(bpy, obs, png: Path, stills: Path) -> None:
    from mathutils import Vector
    stills.mkdir(parents=True, exist_ok=True)
    img = bpy.data.images.load(str(png))
    mat = bpy.data.materials.new("preview")
    mat.use_nodes = True
    nt = mat.node_tree
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    bsdf = nt.nodes["Principled BSDF"]
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.5
    nt.nodes.active = tex
    for o in obs.values():
        o.data.materials.clear()
        o.data.materials.append(mat)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    scene.display.shading.show_shadows = False
    scene.display.shading.show_cavity = False
    scene.render.resolution_x, scene.render.resolution_y = 1400, 900
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("w")
    scene.world = world
    scene.display_settings.display_device = "sRGB"
    scene.view_settings.view_transform = "Standard"
    cam_data = bpy.data.cameras.new("cam")
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    def g2b(p):
        return Vector((-p[0], -p[2], p[1]))
    views = {
        "side-left": (-14, 0.4, -0.4),
        "side-right": (14, 0.4, -0.4),
        "top": (0, 18, -0.4),
        "bottom": (0, -18, -0.4),
        "three-quarter": (-9.5, 5.5, 9.0),
        "three-quarter-rear": (8.5, 4.5, -10.5),
        "close-roundel": (-5.5, 1.2, -1.2),
    }
    for name, eye in views.items():
        cam_data.type = "ORTHO" if name in ("side-left", "side-right", "top", "bottom") else "PERSP"
        cam_data.ortho_scale = 11.5
        cam_data.lens = 50 if name != "close-roundel" else 40
        cam.location = g2b(eye)
        target = g2b((0, 0, -0.8 if name != "close-roundel" else -2.0))
        d = target - cam.location
        if name == "top":
            cam.rotation_euler = (0.0, 0.0, math.pi)       # looking down, the nose up the image
        elif name == "bottom":
            cam.rotation_euler = (math.pi, 0.0, 0.0)       # looking up, the nose up the image
        else:
            cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(stills / f"spitfire101-{name}.png")
        bpy.ops.render.render(write_still=True)
        print("STILL", scene.render.filepath)


def meta_record(repo: Path) -> dict:
    files = {}
    for rel in (FBX_DIR / FBX, MODEL_DIR / PNG, MODEL_DIR / f"{TYPE}.mat"):
        files[str(rel)] = sha((repo / rel).read_bytes())
    return files


def write_provenance(repo: Path, inspected: dict) -> None:
    dest = repo / MODEL_DIR
    record = {
        "asset": TYPE,
        "what": "The 101 Squadron IAF Supermarine Spitfire LF Mk IXe as flown from October 1948, built from primitives "
                "(lofted sections, a NACA 22xx wing on an elliptical clipped planform) by tools/spitfire_101_model.py in "
                f"Blender {inspected['blender']} (pip bpy); markings painted into the colour map by a colour function of "
                "each texel's 3D point. No third-party model, image or scan; no AI.",
        "licence": "CC-BY-4.0", "credit": "Statecraft: Israel contributors",
        "build": "uv run --no-project --python 3.13 --with bpy==5.2.2 python -I tools/spitfire_101_model.py --build",
        "sources": {
            "length_9.47_m": "en.wikipedia 'Supermarine Spitfire variants: specifications, performance and armament', LF Mk IXe, read 2026-11-05",
            "span_11.23_m_minus_clip_1.07_m": "en.wikipedia 'Supermarine Spitfire', read 2026-11-05 (span, clipped tips, dihedral 6 deg, NACA 2213/2209.4, incidence +2/-0.5 deg)",
            "markings": "Yoav Efrati, Colors & Markings of the Israeli Air Force (IAF, 2005) pp. 6, 12, 14, as cited in UnitModels.Spitfire and docs/units.md",
            "colours": "UnitPalette.Base (sRGB), the code-built model's",
            "four_blades": "docs/units.md (IAF Museum Mk IX, Hatzerim, Commons CC BY 2.5)",
        },
        "design_values": "fuselage sections, canopy, radiators, intake, exhausts, tailplane and fin sizes, hub position, "
                         "propeller diameter and blade shape, roundel sizes and places, wing-roundel star orientation, "
                         "rudder stripe angle and width, camouflage pattern, demarcation line, panel lines",
        "files": meta_record(repo),
        "triangles": inspected["triangles"], "triangles_by_object": inspected["triangles_by_object"],
        "span_m": inspected["span_m"], "length_m": inspected["length_m"], "hub_game_m": inspected["hub_game_m"],
        "texture_px": TEX,
    }
    (dest / "provenance.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")


def verify(repo: Path) -> dict:
    record = json.loads((repo / MODEL_DIR / "provenance.json").read_text(encoding="utf-8"))
    for rel, h in record["files"].items():
        check(sha((repo / rel).read_bytes()) == h, f"{rel} differs from its recorded SHA-256")
    check(record["licence"] == "CC-BY-4.0", "licence")
    return record


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--repo", type=Path, default=ROOT)
    p.add_argument("--build", action="store_true", help="build the FBX and the colour map (needs bpy)")
    p.add_argument("--check", action="store_true", help="re-import the FBX, measure it, write provenance.json (needs bpy)")
    p.add_argument("--verify", action="store_true", help="hashes against provenance.json (stdlib)")
    p.add_argument("--out", type=Path, help="--build/--check: use this folder instead of the model folder (no provenance)")
    p.add_argument("--stills", type=Path, help="--check: also render preview stills into this folder")
    a = p.parse_args(argv)
    repo = a.repo.resolve()
    if a.build:
        build(repo, a.out)
    elif a.check:
        folder = a.out or repo / MODEL_DIR
        fbx_file = (a.out or repo / FBX_DIR) / FBX
        report = inspect(repo, folder, fbx_file, a.stills)
        report["fbx"] = fbx = fbx_summary(fbx_file)
        print(json.dumps(report, indent=1))
        check(set(fbx["models"]) == {"Body", "Propeller"}, f"FBX models: {sorted(fbx['models'])}")
        check((fbx["UpAxis"], fbx["UpAxisSign"], fbx["FrontAxis"], fbx["CoordAxis"], fbx["UnitScaleFactor"]) == ([1], [1], [2], [0], [100.0]),
              "the FBX is not Y-up, metres")
        for name, m in fbx["models"].items():
            check(m["Lcl Rotation"] == [0, 0, 0] and m["PreRotation"] == [0, 0, 0] and m["Lcl Scaling"] == [1, 1, 1],
                  f"{name}: not identity rotation and scale")
        check(fbx["models"]["Body"]["Lcl Translation"] == [0, 0, 0], "Body is not at the origin")
        # The file is right-handed; Unity mirrors x, so (x, y, z) here is (-x, y, z) there: the hub is on the centre line.
        check(fbx["models"]["Propeller"]["Lcl Translation"] == [round(-HUB[0], 6), HUB[1], HUB[2]], "the propeller's pivot is not the hub")
        report["blade_tip_angles_deg"] = tips = blade_angles(fbx_file)
        check(tips == sorted(a % 360.0 for a in BLADE_ANGLES), f"blade tips at {tips}, expected {BLADE_ANGLES}")
        box = fbx["geometry_boxes"]["Body"]
        check(box[1][2] > 3.0 and box[0][2] < -5.5, "the nose is not toward +z")
        check(report["triangles"] < 10000, f"{report['triangles']} triangles: over the WebGL budget")
        check(abs(report["span_m"] - SPAN) < 0.08, f"span {report['span_m']} m, expected {SPAN:.2f}")
        check(abs(report["length_m"] - LENGTH) < 0.05, f"length {report['length_m']} m, expected {LENGTH}")
        if a.out is None:
            write_provenance(repo, report)
    else:
        verify(repo)
        print("VERIFIED", TYPE)
    return 0


if __name__ == "__main__":
    sys.exit(main())
