"""tools/spitfire_101_model.py: the Blender-built 101 Squadron Spitfire ships as its record says, in the frame the streamed
model swap expects (StreamedAircraft: real metres, nose +z, y up, the propeller a top-level node at the hub with identity
rotation, its four blades at 20 + 90k degrees as the code-built propeller's), and its markings are painted where the sources
put them.
The FBX is read with the tool's own stdlib reader; no Blender is needed.
"""
import importlib.util
import math
import re
import subprocess
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]


def tool():
    spec = importlib.util.spec_from_file_location("spitfire_101_model", ROOT / "tools/spitfire_101_model.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def guid(path: Path) -> str:
    return re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(encoding="utf-8"), re.M).group(1)


def test_verify_passes_on_the_shipped_files():
    record = tool().verify(ROOT)
    assert record["licence"] == "CC-BY-4.0" and record["credit"] == "Statecraft: Israel contributors"
    assert record["triangles"] < 10000


def test_the_fbx_is_in_the_frame_the_streamed_swap_expects():
    mod = tool()
    fbx = ROOT / mod.FBX_DIR / mod.FBX
    s = mod.fbx_summary(fbx)
    assert (s["UpAxis"], s["UpAxisSign"], s["FrontAxis"], s["CoordAxis"], s["UnitScaleFactor"]) == ([1], [1], [2], [0], [100.0])
    assert set(s["models"]) == {"Body", "Propeller"}, "two top-level nodes: the model root's direct children"
    for m in s["models"].values():
        assert m["Lcl Rotation"] == [0, 0, 0] and m["PreRotation"] == [0, 0, 0] and m["Lcl Scaling"] == [1, 1, 1]
    assert s["models"]["Body"]["Lcl Translation"] == [0, 0, 0]
    assert s["models"]["Propeller"]["Lcl Translation"] == [0.0, 0.02, 3.3], "the pivot is the hub on the centre line"
    (lo, hi) = s["geometry_boxes"]["Body"]
    plo, phi = s["geometry_boxes"]["Propeller"]
    assert abs((hi[0] - lo[0]) - mod.SPAN) < 0.01, "the clipped span"
    assert abs((phi[2] + 3.3) - lo[2] - mod.LENGTH) < 0.01, "spinner tip to rudder trailing edge"
    assert hi[2] > 3.0 and lo[2] < -5.5, "the nose toward +z"
    assert abs(plo[2] + 0.25) < 1e-3 and abs(phi[2] - 0.35) < 1e-3, "the spinner on the propeller, about its own origin"
    assert mod.blade_angles(fbx) == [20.0, 110.0, 200.0, 290.0], "the code-built propeller's blade angles"


def test_the_dimensions_follow_the_cited_figures():
    mod = tool()
    # Owner, 8 November 2026: "9.9 m (Recommended)", the clipped LF Mk IXe's 32 ft 6 in (en.wiki variants page).
    assert abs(mod.SPAN - 9.9) < 1e-9 and mod.LENGTH == 9.47 and mod.DIHEDRAL == 6.0
    # The clipped elliptical planform's area against the LF Mk IXe's 21.46 m2 (en.wiki variants page).
    n = 4000
    area = 2 * sum(mod.wing_chord((i + 0.5) * mod.SEMI / n) * mod.SEMI / n for i in range(n))
    assert abs(area - 21.46) / 21.46 < 0.02, area


def test_the_markings_are_painted_where_the_sources_put_them():
    mod = tool()
    P = mod.PARTS.index
    zc, yc, r = mod.FUS_ROUNDEL
    cz = mod.WING_ZREF + (mod.WING_LE_SHARE - 0.5) * mod.wing_chord(mod.WING_ROUNDEL_X)
    pts = [
        ((0.30, yc, zc), (1, 0, 0), "fuselage", "RoundelBlue"),                 # star centre, right side
        ((-0.30, yc + 0.85 * r, zc), (-1, 0, 0), "fuselage", "RoundelWhite"),   # disc, beyond the star's top point, left side
        ((mod.WING_ROUNDEL_X, -0.1, cz), (0, 1, 0), "wing", "RoundelBlue"),      # top of the right wing
        ((-mod.WING_ROUNDEL_X, -0.3, cz), (0, -1, 0), "wing", "RoundelBlue"),    # under the left wing
        # The star's orientation (points at 0.74 r, the notches between them at 0.43 r): a point toward the nose on the
        # wings, straight up on the fuselage; between two points across the span and fore and aft.
        ((-mod.WING_ROUNDEL_X, -0.3, cz + 0.65 * mod.WING_ROUNDEL_R), (0, -1, 0), "wing", "RoundelBlue"),
        ((-mod.WING_ROUNDEL_X - 0.65 * mod.WING_ROUNDEL_R, -0.3, cz), (0, -1, 0), "wing", "RoundelWhite"),
        ((0.30, yc + 0.65 * r, zc), (1, 0, 0), "fuselage", "RoundelBlue"),
        ((0.30, yc, zc + 0.65 * r), (1, 0, 0), "fuselage", "RoundelWhite"),
        ((0.0, 0.0, 3.5), (0, 0, 1), "spinner", "MarkingRed"),
        ((0.0, 0.0, 0.0), (0, 0, 1), "blade", "PropBlack"),
        ((2.0, -0.6, -0.8), (0, -1, 0), "wing", "Underside"),                    # Medium Sea Grey under the wing
    ]
    pos = np.array([p[0] for p in pts], float)
    nrm = np.array([p[1] for p in pts], float)
    part = np.array([P(p[2]) for p in pts])
    out = mod.paint(pos, nrm, part, texel_m=0.01)
    for (where, _, _, colour), got in zip(pts, out):
        want = np.array(mod.COL[colour])
        assert np.abs(got - want).max() < 0.05, f"{where}: {got} is not {colour}"
    # The rudder: red and white stripes behind the hinge, both colours present; the fin ahead of it camouflaged.
    ys = np.linspace(0.3, 1.2, 60)
    rud = mod.paint(np.array([(0.03, y, -5.5) for y in ys]), np.tile((1, 0, 0), (60, 1)), np.full(60, P("fin")), 0.01)
    reds = sum(np.abs(c - mod.COL["MarkingRed"]).max() < 0.05 for c in rud)
    whites = sum(np.abs(c - mod.COL["RoundelWhite"]).max() < 0.05 for c in rud)
    assert reds > 15 and whites > 15 and reds + whites == 60
    fin = mod.paint(np.array([(0.03, 0.8, -4.8)]), np.array([(1, 0, 0)]), np.array([P("fin")]), 0.01)[0]
    assert min(np.abs(fin - mod.COL[k]).max() for k in ("CamoGreen", "CamoGrey")) < 0.06


def test_the_unity_files_point_at_each_other_with_unique_guids():
    mod = tool()
    d = ROOT / mod.MODEL_DIR
    fbx_meta, png_meta, mat_meta = (ROOT / mod.FBX_DIR / f"{mod.FBX}.meta", d / f"{mod.PNG}.meta", d / f"{mod.TYPE}.mat.meta")
    fbx_g, png_g, mat_g = guid(fbx_meta), guid(png_meta), guid(mat_meta)
    meta = fbx_meta.read_text(encoding="utf-8")
    assert f"name: {mod.MATERIAL_NAME}\n    second: {{fileID: 2100000, guid: {mat_g}, type: 2}}" in meta, "the FBX's material is remapped to the .mat"
    mat = (d / f"{mod.TYPE}.mat").read_text(encoding="utf-8")
    assert mat.count(f"guid: {png_g}") == 2 and "guid: 933532a4fcc9baf4fa0491de14d08ed7" in mat, "URP Lit with the colour map"
    assert "- _Smoothness: 0.5\n" in mat and "- _Metallic: 0\n" in mat
    # The model streams from its own Collection folder (CollectionCatalog.IsraeliSpitfire, #2734), beside its colour map and
    # material; nothing of it is under a Resources folder (the WebGL player's first download).
    assert mod.FBX_DIR == mod.MODEL_DIR == Path("unity/Assets/Art/Collection/spitfire-mk9e-101sqn-1948") and mod.FBX == "spitfire-mk9e-101sqn-1948.fbx"
    tracked = subprocess.run(["git", "ls-files", "--", "unity/Assets"], cwd=ROOT, capture_output=True, text=True, check=True).stdout.split("\n")
    assert not [p for p in tracked if mod.TYPE in p and "/Resources/" in p], "a copy under Resources"
    assert "animationCompression: 0\n" in meta, "the FBX's import passes the Collection's lossless check (it has no takes)"
    for g in (fbx_g, png_g, mat_g):
        hits = subprocess.run(["git", "grep", "-l", "-e", f"^guid: {g}$", "--", "*.meta"], cwd=ROOT, capture_output=True, text=True).stdout.split()
        assert len(hits) <= 1, f"{g} is the guid of more than one asset: {hits}"


def test_the_rear_fuselage_band_is_white_blue_white_ahead_of_the_fin():
    """Owner, 8 November 2026: "Add the band"; Efrati p. 4: the 31 May 1948 order, "An additional white-blue-white
    recognition band was to be applied ahead of the vertical fin". Its width and place are DESIGN VALUES."""
    mod = tool()
    P = mod.PARTS.index
    zc, w = mod.BAND_Z, mod.BAND_STRIPE
    fin_le = mod.FIN_ZREF + 0.45 * mod.FIN_C0           # the fin's root leading edge
    roundel_back = mod.FUS_ROUNDEL[0] - mod.FUS_ROUNDEL[2]
    assert roundel_back > zc + 1.5 * w and zc - 1.5 * w > fin_le, "between the fuselage roundel and the fin"
    hw, ym, top, bot = mod.fuselage_at(zc)
    rings = [((hw, ym, None), (1, 0, 0)), ((-hw, ym, None), (-1, 0, 0)), ((0.0, top, None), (0, 1, 0)), ((0.0, bot, None), (0, -1, 0))]
    for dz, colour in ((0.0, "RoundelBlue"), (w, "RoundelWhite"), (-w, "RoundelWhite")):
        pos = np.array([(x, y, zc + dz) for (x, y, _), _n in rings])
        nrm = np.array([n for _p, n in rings], float)
        out = mod.paint(pos, nrm, np.full(len(pos), P("fuselage")), 0.01)
        for got in out:
            assert np.abs(got - np.array(mod.COL[colour])).max() < 0.05, f"{dz:+.2f} m: {got} is not {colour}"
    # Clear of the band, fore and aft, the fuselage keeps its paint.
    for dz in (2.5 * w, -2.5 * w):
        got = mod.paint(np.array([(hw, ym, zc + dz)]), np.array([(1.0, 0, 0)]), np.array([P("fuselage")]), 0.01)[0]
        assert min(np.abs(got - np.array(mod.COL[k])).max() for k in ("CamoGreen", "CamoGrey", "Underside")) < 0.06, got


def test_no_fuselage_number():
    """Owner, 8 November 2026: "No number (Recommended)": the fuselage sides carry the roundel and the band only."""
    mod = tool()
    assert mod.FUSELAGE_NUMBER is None
    P = mod.PARTS.index
    zc, yc, r = mod.FUS_ROUNDEL
    # Where Efrati p. 12's Spitfire 14 wears its number, just behind the roundel: camouflage on both sides.
    for side in (1, -1):
        z = zc - r - 0.25
        hw, ym, _, _ = mod.fuselage_at(z)
        got = mod.paint(np.array([(side * hw, 0.10, z)]), np.array([(side * 1.0, 0, 0)]), np.array([P("fuselage")]), 0.01)[0]
        assert min(np.abs(got - np.array(mod.COL[k])).max() for k in ("CamoGreen", "CamoGrey")) < 0.06, got
