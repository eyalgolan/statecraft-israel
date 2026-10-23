# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "scipy==1.17.1", "pillow==12.3.0", "shapely==2.2.0", "rasterio==1.5.2", "pyproj==3.8.0",
#                 "scikit-image==0.26.0", "pypdf==6.21.1"]
# ///
"""Digitises the 1949 armistice lines that the primary sources allow, as true lines (not hex-snapped), into
content/map/armistice_lines_1949.geojson. Owner rulings (2026-10-11): the lines come from the four General Armistice
Agreements and their annexed maps, never from a later "Green Line" drawing; no invented boundaries; the game snaps them
to hex edges only for display (tools/armistice_bake.py). Research note: research_notes/Map borders/armistice-lines-1949.md.

Usage (the scans are downloads kept outside the repository and checked by SHA-256; SOURCES has the URLs):
  uv run tools/armistice_digitise.py --syria-pdf <dir>/S_1353_Rev_1.pdf --syria-tif <dir>/s1353rev1.tif \
      --gaza-sheet "<dir>/Survey_of_Palestine,_Palestine_09-gaza_(FL33122277_2367645).jpg" \
      --s1471-sheet1 <dir>/s1471.tif --s1471-sheet2 <dir>/khan_yunis.pdf --jordan-map <dir>/Arm_1949.jpg

Fronts, each a dated feature set (status draft):
- Syria (signed 20 Jul 1949): the Armistice Demarcation Line through the 24 items of Annex I (Palestine-grid MR points,
  typed below), traced between them on the annexed 1:50,000 map, where the line is printed in green (the UN ODS colour
  scan of S/1353/Rev.1); the Demilitarized Zone, three parts, from the map's hatching (the UNISPAL 1-bit scan of the
  same map, georeferenced on its 1-km Palestine grid).
- Lebanon (23 Mar 1949): Art. V.1 adopts the international boundary; its geometry is the Mandate frontier already
  digitised for the 1948 sub-districts (content/map/subdistricts_1948.geojson), from the coast at Ras en Naqura to the
  Lebanon-Syria-Palestine tripoint.
- Egypt (24 Feb 1949): the Gaza-Rafah line of Art. VI.1 (the words of para. 2.B(i) of the Memorandum of 13 November
  1948), built from the Survey of Palestine 1:100,000 sheet 9 "Gaza" (the Wadi Hasi mouth, Deir Suneid and the Gaza-Al
  Majdal highway), from the Wadi Hasi to Gaza (the stretch on to the frontier is a gap: EGYPT_SOUTH_GAP); the El Auja Demilitarized Zone from the Art. VIII.2 MR points,
  closed along the 1906 frontier (the Mandate frontier above).
- Jordan (3 Apr 1949): low accuracy (owner ruling 2026-10-11). Traced from the UNISPAL copy of the signed map 1 (3119 x
  8630 px), georeferenced on town symbols; line A staged by Art. VI.3; Jerusalem, the no-man's-lands and Mount Scopus
  not legible, recorded as gaps. Every feature `source_quality: low_res_scan`, to be replaced from a full-resolution scan.
- Egypt, 1950: the modus vivendi's signed map (S/1471) as its own dated feature (owner ruling 2026-10-11).
Control points (JORDAN_CONTROL): town coordinates from Wikidata (CC0) and the Wikipedia API / settlements_1948.yaml; facts
used only to georeference the Jordan scan, never drawn. Names: the agreements' own titles are "Egyptian-Israeli",
"Lebanese-Israeli", "Hashemite Jordan Kingdom-Israel" and "Israeli-Syrian" General Armistice Agreement; the game's
"Egypt-Israel" style is a paraphrase (a choice recorded in D-A).
Conversion: Palestine Grid, EPSG:28191 (Palestine 1923 / Palestine Grid, Cassini-Soldner; MR values are km), to WGS84
with the EPSG Helmert transformation (stated accuracy 2 m), pinned as PG_PIPELINE and tested against pyproj's EPSG pair.
"""
import argparse
import hashlib
import io
import json
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "content" / "map" / "armistice_lines_1949.geojson"
SUBDISTRICTS = ROOT / "content" / "map" / "subdistricts_1948.geojson"
CHECKED = "2026-10-11"
# The owner's sign-off (CONTENT_POLICY 2.7, in the owner's own commit): the 11 features the owner approved on 2026-10-23 (#196), the
# owner's review standing as their fact check too (owner ruling, PR #1444: "My review counts"). The 5 flagged stay draft.
SIGNED_OFF = {"status": "sensitivity-reviewed", "reviewed_by": "eyalgolan", "reviewed_on": "2026-10-23"}
SIGNED_OFF_IDS = ("egypt_el_auja_dmz", "lebanon_adl", "jordan_adl_bc_south", "jordan_adl_d_arava",
                  "jordan_adl_iraqi_a_triangle", "jordan_adl_iraqi_b_wadi_ara", "jordan_adl_iraqi_c_east",
                  "jordan_adl_iraqi_c_south", "syria_dmz_central", "syria_dmz_north", "syria_dmz_south")
PG_PIPELINE = ("+proj=pipeline +step +inv +proj=cass +lat_0=31.7340969444444 +lon_0=35.2120805555556 +x_0=170251.555 "
               "+y_0=126867.909 +a=6378300.789 +b=6356566.435 +step +inv +proj=longlat +a=6378300.789 +b=6356566.435 "
               "+step +proj=push +v_3 +step +proj=cart +a=6378300.789 +b=6356566.435 +step +proj=helmert +x=-275.7224 "
               "+y=94.7824 +z=340.8944 +rx=-8.001 +ry=-4.42 +rz=-11.821 +s=1 +convention=position_vector +step +inv "
               "+proj=cart +ellps=WGS84 +step +proj=pop +v_3 +step +proj=unitconvert +xy_in=rad +xy_out=deg")
_PG = None


def mr_to_lonlat(e_km, n_km):
    """Palestine Grid MR (km easting, km northing; EPSG:28191, no false-northing offset) -> WGS84 lon, lat."""
    global _PG
    from pyproj import Transformer
    if _PG is None:
        _PG = Transformer.from_pipeline(PG_PIPELINE)
    lon, lat = _PG.transform(np.asarray(e_km, float) * 1000.0, np.asarray(n_km, float) * 1000.0)
    return lon, lat


def lonlat_to_mr(lon, lat):
    global _PG
    from pyproj import Transformer
    if _PG is None:
        _PG = Transformer.from_pipeline(PG_PIPELINE)
    from pyproj.enums import TransformDirection
    e, n = _PG.transform(np.asarray(lon, float), np.asarray(lat, float), direction=TransformDirection.INVERSE)
    return np.asarray(e) / 1000.0, np.asarray(n) / 1000.0


# --- sources --------------------------------------------------------------------------------------------------------

UNTS = {"cite": "United Nations Treaty Series vol. 42 (1949): No. 654 Egypt-Israel (p. 251), No. 655 Lebanon-Israel (p. 287), "
                "No. 656 Jordan-Israel (p. 303), No. 657 Syria-Israel (p. 327), English and French texts.",
        "url": "https://treaties.un.org/doc/publication/UNTS/Volume%2042/v42.pdf",
        "sha256": "1aacfde1ac4f9c2407fc3a3ed6d07db9a84deb52f95f91aade0bc5f1a6e2e4c1", "checked": CHECKED,
        "licence": "Public domain (UN treaty publication; UN document, ST/AI/189/Add.9/Rev.2 para. 2)"}
SYRIA_PDF = {"cite": "S/1353/Rev.1, 'Map referred to in Annexes I, II and III' of the Syria-Israel General Armistice Agreement, "
                     "1:50,000 sheet 'Houle' (Service Geographique des FFL base), colour scan in the UN Official Document System "
                     "(PDF page 14); the Armistice Demarcation Line is printed in green.",
             "url": "https://documents.un.org/api/symbol/access?s=S/1353/Rev.1&l=en&t=pdf",
             "sha256": "0e5a406f8f749af9c73a3d6b70e92a23e6eb7b69468ca81c80ba8d6e0f2baed8", "checked": CHECKED,
             "licence": "Public domain (UN document with a symbol, ST/AI/189/Add.9/Rev.2 para. 2; Commons {{PD-UN-doc}}/{{PD-UN-map}})"}
SYRIA_TIF = {"cite": "The same map (S/1353/Rev.1), 1-bit 200 dpi scan on UNISPAL: the Demilitarized Zone's hatching and the "
                     "1-km Palestine grid.",
             "url": "https://www.un.org/unispal/wp-content/uploads/1949/07/794884bef66f46f38525705200598b87_s1353rev1.tif",
             "sha256": "181ee3335f22d79e3daf684c06c7fc8cc14d92d203a9c1ce2582bbd817aa7694", "checked": CHECKED,
             "licence": SYRIA_PDF["licence"]}
GAZA_SHEET = {"cite": "Survey of Palestine, 1:100,000 series sheet 9 'Gaza' (compiled, drawn and printed under F.J. Salmon), "
                      "National Library of Israel copy on Wikimedia Commons: the Gaza-Al Majdal highway (first-class road), "
                      "the Wadi Hasi mouth and Deir Suneid. Only these features' positions are taken.",
              "url": "https://commons.wikimedia.org/wiki/File:Survey_of_Palestine,_Palestine_09-gaza_(FL33122277_2367645).jpg",
              "sha256": "3c955f06ed31daa4a5dba6736ef420716039c6d7582b24f73f8f483e2716c9e6", "checked": CHECKED,
              "licence": "Public domain (Government of Palestine work; Commons 'Public domain'); same status as decision D-B"}
EGYPT_TEXT = {"cite": "S/1264/Corr.1 (the Egypt-Israel agreement as corrected), Art. VI and VIII", "checked": CHECKED,
              "url": "https://documents.un.org/api/symbol/access?s=S/1264/Corr.1&l=en&t=pdf",
              "sha256": "67da96ca34141edd9cb9b33b13b7778dd73586efb5d3ee25b36baedecbb1c28d", "licence": SYRIA_PDF["licence"]}
MEMO = {"cite": "Memorandum of 13 November 1948 on the implementation of the Security Council resolution of 4 November 1948 "
                "(Acting Mediator). Not read: its para. 2.B(i) is quoted verbatim in Art. VI.1; S/1086 (20 Nov 1948) refers "
                "to it; the UN Archives hold it as 'Memorandum of 13 November and Note of Transmittal (Part III) (re Negev)' "
                "(fonds AG-022, United Nations Mediator for Palestine), not digitised as text; the catalogue page needs a "
                "JavaScript check. No map of it was found.",
        "url": "https://search.archives.un.org/memorandum-of-13-november-and-note-of-transmittal-part-iii-re-negev",
        "checked": CHECKED, "licence": "n/a (not used)"}
MANDATE_FRONTIER = {"cite": "The Mandate frontier of content/map/subdistricts_1948.geojson (Survey of Palestine 1:750,000, "
                            "'Administrative Divisions', 1946/47; tools/borders_digitise.py): about +-1 km.",
                    "url": "https://commons.wikimedia.org/wiki/File:06_Palestine-_Administrative_divisions.jpg",
                    "checked": CHECKED, "licence": "Public domain (Government of Palestine; decision D-B)"}

JORDAN_SEARCH = [
    "UNISPAL, 'Israel-Jordan General Armistice Agreement - Map' (https://www.un.org/unispal/document/auto-insert-181069/): "
    "map 1 (Survey of Palestine 1:250,000 north and south sheets, signed); the page shows a 925 x 2560 px copy, and its "
    "original upload (f03d55e48f77ab698525643b00608d34_Arm_1949.jpg) is 3119 x 8630 px, about 51 m a pixel: the source used "
    "(owner ruling 2026-10-11), low-resolution. No map 2.",
    "UN Official Document System, S/1302/Rev.1 (https://documents.un.org/api/symbol/access?s=S/1302/Rev.1&l=en&t=pdf) and "
    "S/1302: the text only (12 and 9 pages); no map plates.",
    "UNTS vol. 42 (https://treaties.un.org/doc/publication/UNTS/Volume%2042/v42.pdf): the maps were an insert 'between pages "
    "326 and 327'; the online PDF has a blank page there.",
    "UN Archives (search.archives.un.org): the catalogue needs a JavaScript check (not passed with a plain HTTP client); no "
    "digitised map found through search engines.",
    "Wikimedia Commons (API searches: '1949 armistice map Jordan Israel', 'Jordan Israel armistice agreement map 1949', "
    "'Jerusalem armistice map 1949', 'Jerusalem cease-fire 30 November 1948 map', 'S/1302', 'Green Line 1949 original map'): "
    "'File:Green Line - Cease Fire Agreement 30 Nov 1948.jpg' is a 3119 x 8630 px colour copy of the signed map 1, the same "
    "size as UNISPAL's original but not the same file (SHA-1 3d5990f9... against 370af7aa...), uploaded as 'own work' under "
    "CC0 with no source: not used, not even as a cross-check (owner ruling); 'File:UN Map of Central Jerusalem November 1949.jpg' is a later UN cartographic map "
    "(November 1949), not map 2; the others are later drawings (H212/10/1955, user-made comparisons).",
    "National Library of Israel and ECF ('Original Green Line Map', https://ecf.org.il/media_items/338): not reached with a "
    "plain HTTP client in this pass (JavaScript catalogues); to request.",
]


# --- Syria: Annex I --------------------------------------------------------------------------------------------------

# (label, E km, N km) in the order of Annex I (UNTS vol. 42, No. 657, pp. 342-344).
SYRIA_POINTS = [("1", 208.7, 294.2), ("1b", 212.8, 294.7), ("5a", 212.7, 290.4), ("5b", 212.4, 290.2), ("5c", 211.0, 276.8),
                ("6", 210.9, 276.7), ("7", 210.3, 276.5), ("8", 209.9, 272.6), ("9", 209.7, 272.6), ("10", 209.2, 271.7),
                ("11", 208.5, 272.9), ("12", 205.2, 269.1), ("13", 208.8, 265.0), ("14", 208.7, 260.0), ("15", 208.5, 258.2),
                ("16", 207.0, 257.0), ("17", 207.4, 256.0), ("BP61", 210.6, 246.3), ("BP66", 211.7, 240.1), ("21", 209.6, 239.0),
                ("22", 206.3, 234.8), ("23", 206.3, 234.5), ("BH", 207.7, 233.4), ("24", 209.5, 232.2)]
# Each stretch between consecutive points: how Annex I defines it. "line": straight between the MR points (the text says
# "a line", or gives only the two points); "map": the text says "along" a wady, shoreline, river, border, trail or road,
# or passes points with no MR value: traced on the annexed map's green line.
SYRIA_STRETCHES = {
    ("1", "1b"): ("map", "Annex I.1: from the Lebanese-Syrian-Palestinian border point eastward to MR 212.8-294.7 (the map's line is not straight)"),
    ("1b", "5a"): ("map", "Annex I.2-4: along the Wady Assal to the Marabout Cheikh el Makhfi, a line to the Marabout Nabi Huda, a line to MR 212.7-290.4 (the marabouts have no MR values)"),
    ("5a", "5b"): ("line", "Annex I.5: MR 212.7-290.4 to MR 212.4-290.2"),
    ("5b", "5c"): ("map", "Annex I.5: southward along the Syrian-Palestinian border to MR 211.0-276.8"),
    ("5c", "6"): ("line", "Annex I.6: to a point on the Wady es Simadi"),
    ("6", "7"): ("map", "Annex I.7: westward along the Wady es Simadi to the trail"),
    ("7", "8"): ("map", "Annex I.8: southward along the trail, by-passing Dardara to the east"),
    ("8", "9"): ("line", "Annex I.9: westward to the shoreline of Lake Hula"),
    ("9", "10"): ("map", "Annex I.10: southward along the shoreline to the mouth of the Jordan River"),
    ("10", "11"): ("map", "Annex I.11: north-west along the western shoreline of Lake Hula"),
    ("11", "12"): ("line", "Annex I.12: a line"),
    ("12", "13"): ("line", "Annex I.13: a line to the Jordan River"),
    ("13", "14"): ("map", "Annex I.14: southward along the Jordan River"),
    ("14", "15"): ("line", "Annex I.15: a line"),
    ("15", "16"): ("line", "Annex I.16: a line"),
    ("16", "17"): ("line", "Annex I.17: a line"),
    ("17", "BP61"): ("map", "Annex I.18: southward along the Syrian-Palestinian border to BP 61"),
    ("BP61", "BP66"): ("map", "Annex I.19-20: along the Syrian-Palestinian border, BP 61 to BP 62 to BP 66 (BP 62 has no MR value)"),
    ("BP66", "21"): ("line", "Annex I.21: a line to the eastern shoreline of Lake Tiberias"),
    ("21", "22"): ("map", "Annex I.22: southward along the shoreline of Lake Tiberias"),
    ("22", "23"): ("map", "Annex I.23: southward to the bend in the road"),
    ("23", "BH"): ("map", "Annex I.23: south-east along the west bank of the railroad and the west side of the road to the Block House"),
    ("BH", "24"): ("map", "Annex I.24: along the road to the Yarmuk River, on the border"),
}
# Initial guesses for the grid georeferences (scan pixels of one line each way), refined from the drawn grid lines.
TIF_SHEETS = {"north": {"y": (990, 5315), "x": (780, 4620), "anchor": ((917, 1300, 198.0), (1300, 1140, 294.0))},
              "south": {"y": (5330, 11000), "x": (700, 4470), "anchor": ((2722, 8000, 210.0), (2722, 8055, 250.0))}}
PDF_SHEETS = {"north": {"y": (780, 4050), "x": (80, 3040), "anchor": ((1729, 850, 210.0), (1000, 880, 294.0))},
              "south": {"y": (4120, 8350), "x": (80, 3040), "anchor": ((1730, 7000, 210.0), (1000, 6050, 250.0))}}
SHEET_SPLIT_N = 267.3     # the two sheets of the map meet near 267.3 km N
PDF_LEGEND_BOX = (5800, 7400, 300, 1250)   # y0, y1, x0, x1 of the legend's green swatch on the colour scan


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def check(path: Path, src: dict) -> None:
    got = sha256(path)
    if got != src["sha256"]:
        raise SystemExit(f"{path}: sha256 {got}, expected {src['sha256']} ({src['url']})")


def jpeg_as_pypdf5(jpeg: bytes):
    """A JPEG stream decoded as pypdf 5.x decoded a /DCTDecode image (#1251): decoded by Pillow, saved again as a JPEG with
    Pillow's defaults, and that copy decoded. pypdf 6.3 and later return the first decode, which moves pixels on the ISA
    and khan_yunis scans; the committed lines were digitised on these pixels, so the tools keep them."""
    from PIL import Image
    buf = io.BytesIO()
    Image.open(io.BytesIO(jpeg), formats=("JPEG",)).save(buf, format="JPEG")
    return Image.open(io.BytesIO(buf.getvalue()), formats=("JPEG",))


def pdf_page_image(pdf: Path, page: int):
    """The largest image on a PDF page (0-based) as a Pillow image; a /DCTDecode image goes through jpeg_as_pypdf5, the
    others (CCITT, JPX: the same pixels on pypdf 5.4.0 and 6.21.1) through pypdf."""
    from pypdf import PdfReader
    im = max(PdfReader(pdf).pages[page].images, key=lambda im: im.image.size[0] * im.image.size[1])
    xobj = im.indirect_reference.get_object()
    if xobj.get("/Filter") == "/DCTDecode":
        return jpeg_as_pypdf5(xobj.get_data())
    return im.image


def grid_fit(img_v: np.ndarray, img_h: np.ndarray, sheet: dict, px_per_km, thr: float, along: int, detrend: bool,
             tols=(0.15, 0.08, 0.05, 0.05)) -> dict:
    """Fits E and N (km) as affine functions of scan pixels from the map's drawn 1-km grid lines: in bands across each
    sheet, local darkness peaks (the lines) are matched to the nearest whole kilometre of the current fit and the fit is
    redone, with a shrinking tolerance. img_v / img_h: darkness (higher = darker) for the vertical and horizontal lines;
    detrend: take each band's profile less its 41-px running mean (raw ink masks)."""
    from scipy import ndimage as ndi
    (xe, ye, e0), (xn, yn, n0) = sheet["anchor"]
    sx, sy = px_per_km if isinstance(px_per_km, tuple) else (px_per_km, px_per_km)   # a scan's pixels may not be square
    a = np.array([e0 - xe / sx, 1 / sx, 0.0]); b = np.array([n0 + yn / sy, 0.0, -1 / sy])

    def peaks(p):
        q = p - ndi.uniform_filter1d(p, 41) if detrend else p
        return [i for i in range(3, len(p) - 3) if q[i] > thr and q[i] == q[i - 3:i + 4].max()]
    for tol in tols:
        ve, vn = [], []
        for y0 in range(sheet["y"][0], sheet["y"][1] - along, along):
            for px in peaks(img_v[y0:y0 + along, sheet["x"][0]:sheet["x"][1]].mean(axis=0)):
                x, y = px + sheet["x"][0], y0 + along / 2
                e = a @ [1, x, y]
                if abs(e - round(e)) < tol:
                    ve.append((x, y, round(e)))
        for x0 in range(sheet["x"][0], sheet["x"][1] - along, along):
            for py in peaks(img_h[sheet["y"][0]:sheet["y"][1], x0:x0 + along].mean(axis=1)):
                x, y = x0 + along / 2, py + sheet["y"][0]
                n = b @ [1, x, y]
                if abs(n - round(n)) < tol:
                    vn.append((x, y, round(n)))
        ve, vn = np.array(ve, float), np.array(vn, float)
        a = np.linalg.lstsq(np.c_[np.ones(len(ve)), ve[:, :2]], ve[:, 2], rcond=None)[0]
        b = np.linalg.lstsq(np.c_[np.ones(len(vn)), vn[:, :2]], vn[:, 2], rcond=None)[0]
    re = np.c_[np.ones(len(ve)), ve[:, :2]] @ a - ve[:, 2]
    rn = np.c_[np.ones(len(vn)), vn[:, :2]] @ b - vn[:, 2]
    return {"E": a, "N": b, "obs": [len(ve), len(vn)], "rms_m": [float(np.sqrt((re ** 2).mean())) * 1000, float(np.sqrt((rn ** 2).mean())) * 1000]}


def px_to_en(fit: dict, x, y):
    return fit["E"][0] + fit["E"][1] * np.asarray(x) + fit["E"][2] * np.asarray(y), fit["N"][0] + fit["N"][1] * np.asarray(x) + fit["N"][2] * np.asarray(y)


def en_to_px(fit: dict, e, n):
    m = np.array([[fit["E"][1], fit["E"][2]], [fit["N"][1], fit["N"][2]]])
    return np.linalg.solve(m, [e - fit["E"][0], n - fit["N"][0]])


def hatch_strokes(dark: np.ndarray) -> np.ndarray:
    """Pixels of long '/' diagonal strokes (the map's Demilitarized Zone hatching), solid ink excluded."""
    from scipy import ndimage as ndi
    diag = dark.copy()
    for k in range(1, 8):
        up = np.zeros_like(dark); up[:-k, k:] = dark[k:, :-k]
        dn = np.zeros_like(dark); dn[k:, :-k] = dark[:-k, k:]
        diag &= up & dn
    return diag & ~ndi.binary_dilation(ndi.binary_erosion(dark, structure=np.ones((5, 5))), iterations=3)


def syria_dmz(tif: Path, log) -> tuple[list, dict, dict]:
    """The Demilitarized Zone's parts, from the hatching of the 1-bit scan, as polygons in Palestine-grid km."""
    import shapely
    from PIL import Image
    from rasterio import features
    from scipy import ndimage as ndi
    from shapely.geometry import shape
    Image.MAX_IMAGE_PIXELS = None
    dark = ~np.asarray(Image.open(tif).convert("1"))
    d32 = dark.astype(np.float32)
    fits = {k: grid_fit(d32, d32, s, 157.5, 0.3, 120, True) for k, s in TIF_SHEETS.items()}
    for k, f in fits.items():
        log(f"Syria map (1-bit), {k} sheet: {f['obs']} grid observations, rms {f['rms_m'][0]:.0f} / {f['rms_m'][1]:.0f} m")
    diag = hatch_strokes(dark)
    rough = ndi.binary_opening(ndi.uniform_filter(diag.astype(np.float32), 61) > 0.02, iterations=5)
    lab, n = ndi.label(rough)
    sizes = ndi.sum_labels(rough, lab, range(1, n + 1))
    r = 18; yy, xx = np.mgrid[-r:r + 1, -r:r + 1]
    parts = []
    for i, sl in enumerate(ndi.find_objects(lab), start=1):
        if sizes[i - 1] < 20000:
            continue
        y0, y1 = max(sl[0].start - 200, 0), sl[0].stop + 200
        x0, x1 = max(sl[1].start - 200, 0), sl[1].stop + 200
        m = ndi.binary_dilation(diag[y0:y1, x0:x1] & ndi.binary_dilation(lab[y0:y1, x0:x1] == i, iterations=40), structure=np.ones((3, 3)), iterations=6)
        m = ndi.binary_fill_holes(ndi.binary_closing(np.pad(m, r), structure=xx ** 2 + yy ** 2 <= r * r)[r:-r, r:-r])
        m = ndi.binary_erosion(m, iterations=4)
        l2, n2 = ndi.label(m)
        m = l2 == 1 + int(np.argmax(ndi.sum_labels(m, l2, range(1, n2 + 1))))
        poly = shapely.union_all([shape(g) for g, v in features.shapes(m.astype(np.uint8), mask=m) if v == 1]).simplify(2.0)
        poly = shapely.affinity.translate(poly, x0, y0)
        cy = poly.centroid.y
        fit = fits["north"] if cy < 5322 else fits["south"]
        parts.append(shapely.transform(poly, lambda c: np.c_[px_to_en(fit, c[:, 0], c[:, 1])]))
    return parts, fits, {"sheets_px": {k: [s["y"][0], s["y"][1]] for k, s in TIF_SHEETS.items()}}


def syria_line(pdf: Path, log) -> tuple[list, dict]:
    """The Armistice Demarcation Line: the colour scan's green line, registered on the Annex I points and traced between
    them; returns [(E, N)] vertices (Palestine-grid km) and diagnostics."""
    from PIL import Image
    from scipy import ndimage as ndi
    from skimage.graph import route_through_array
    Image.MAX_IMAGE_PIXELS = None
    img = pdf_page_image(pdf, 13).convert("RGB")
    rgb = np.asarray(img).astype(np.int16)
    gray = rgb.mean(axis=2).astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    green = (g - r >= 4) & (g < 140) & (g - b > 12)
    y0, y1, x0, x1 = PDF_LEGEND_BOX
    green[y0:y1, x0:x1] = False
    from scipy import ndimage as ndi2
    darkness = ndi2.uniform_filter1d(gray, 25, axis=1) - gray
    darkness_v = ndi2.uniform_filter1d(gray, 25, axis=0) - gray
    # the grid fit takes one darkness image per direction: vertical lines from the row-wise filter and vice versa
    fits = {}
    for k, s in PDF_SHEETS.items():
        fits[k] = grid_fit(darkness, darkness_v, s, 118.3, 10.0, 100, False)
    # registration on the Annex I points: each point's nearest green pixel, an affine per sheet, iterated (ICP)
    dist, (iy, ix) = ndi.distance_transform_edt(~green, return_indices=True)
    resid = {}
    for k in fits:
        pts = [p for p in SYRIA_POINTS if (p[2] > SHEET_SPLIT_N) == (k == "north")]
        a, bb = fits[k]["E"], fits[k]["N"]
        for _ in range(15):
            px = np.array([en_to_px({"E": a, "N": bb}, e, n) for _, e, n in pts])
            near = np.array([[ix[int(round(y)), int(round(x))], iy[int(round(y)), int(round(x))]] for x, y in px], float)
            xx = np.c_[np.ones(len(pts)), near]
            a = np.linalg.lstsq(xx, np.array([p[1] for p in pts]), rcond=None)[0]
            bb = np.linalg.lstsq(xx, np.array([p[2] for p in pts]), rcond=None)[0]
        res = np.hypot(xx @ a - [p[1] for p in pts], xx @ bb - [p[2] for p in pts]) * 1000
        fits[k] = {"E": a, "N": bb, "grid_rms_m": fits[k]["rms_m"]}
        resid.update({p[0]: round(float(v)) for p, v in zip(pts, res)})
        log(f"Syria map (colour), {k} sheet: Annex I points to the green line after registration: median {np.median(res):.0f} m, max {res.max():.0f} m")
    cost = np.where(green, 1.0, 40.0)
    verts, traced = [], {}
    for (u, eu, nu), (v, ev, nv) in zip(SYRIA_POINTS, SYRIA_POINTS[1:]):
        how, why = SYRIA_STRETCHES[(u, v)]
        if how == "line":
            seg = [(eu, nu), (ev, nv)]
        else:
            fu = fits["north"] if nu > SHEET_SPLIT_N else fits["south"]
            fv = fits["north"] if nv > SHEET_SPLIT_N else fits["south"]
            pu = en_to_px(fu, eu, nu); pv = en_to_px(fv, ev, nv)
            su = (iy[int(round(pu[1])), int(round(pu[0]))], ix[int(round(pu[1])), int(round(pu[0]))])
            sv = (iy[int(round(pv[1])), int(round(pv[0]))], ix[int(round(pv[1])), int(round(pv[0]))])
            m = 150
            by0, bx0 = max(min(su[0], sv[0]) - m, 0), max(min(su[1], sv[1]) - m, 0)
            by1, bx1 = max(su[0], sv[0]) + m, max(su[1], sv[1]) + m
            path, _ = route_through_array(cost[by0:by1, bx0:bx1], (su[0] - by0, su[1] - bx0), (sv[0] - by0, sv[1] - bx0),
                                          fully_connected=True, geometric=True)
            path = np.array(path, float) + [by0, bx0]
            from shapely.geometry import LineString
            ls = LineString(path[:, ::-1]).simplify(1.5)
            xy = np.array(ls.coords)
            on = float(green[path[:, 0].astype(int), path[:, 1].astype(int)].mean())
            mid = []
            for x, y in xy[1:-1]:
                f = fits["north"] if px_to_en(fits["north"], x, y)[1] > SHEET_SPLIT_N else fits["south"]
                mid.append(tuple(float(t) for t in px_to_en(f, x, y)))
            seg = [(eu, nu)] + mid + [(ev, nv)]   # the trace's ends replaced by the exact MR points
            traced[f"{u}-{v}"] = {"on_green": round(on, 3), "vertices": len(seg)}
        verts += seg if not verts else seg[1:]
    return verts, {"fits": {k: {"E": list(map(float, f["E"])), "N": list(map(float, f["N"])), "grid_rms_m": [round(x, 1) for x in f["grid_rms_m"]]} for k, f in fits.items()},
                   "annex_points_to_green_m": resid, "traced": traced}


# --- Egypt ----------------------------------------------------------------------------------------------------------

EL_AUJA = [("frontier 5 km NW of the Rafah-El Auja road crossing", 87.5, 46.8), ("Khashm El Mamdud", 96.5, 41.4),
           ("Hill 405", 107.8, 28.5), ("frontier 5 km SE of the old railway crossing", 99.5, 14.5)]
# Art. VI.3 Israeli outposts and evacuated points (MR, km), for the check of the Gaza-Rafah line.
GAZA_OUTPOSTS = [("Deir Suneid, north side of the Wadi", 107.5, 109.0, "kept"), ("700 SW of Sa'ad", 105.0, 98.2, "kept"),
                 ("Sulphur Quarries", 98.7, 92.4, "kept"), ("Tall-Jamma", 97.2, 88.7, "kept"), ("Kh al Ma'in", 93.2, 82.1, "kept"),
                 ("Cemetery (evacuated the day after the next)", 81.6, 72.3, "evacuated"),
                 ("Hill 79 (evacuated within four weeks)", 104.5, 101.7, "evacuated"), ("new outpost", 83.6, 70.0, "new")]
GAZA_GRID = {"v": {2567: 90, 4925: 100, 7285: 110, 9645: 120}, "h": {3712: 120, 6077: 110, 8435: 100, 10795: 90}}
# Points read off the georeferenced sheet (scan pixels): the Wadi Hasi's mouth at the coast; Deir Suneid (village
# symbol); a point on the highway at Deir Suneid and one in Gaza, between which the highway is traced.
GAZA_PX = {"wadi_hasi_mouth": (5527, 5416), "deir_suneid": (6782, 6240), "highway_deir_suneid": (6730, 6240),
           "highway_gaza": (4880, 8060)}
OFFSET_KM = 3.0
EGYPT_SOUTH_GAP = (
    "Gap: from Gaza (about MR 102.4-100.1) to the Egyptian frontier the line is not drawn. Art. VI.1 says only 'continuing "
    "thus to the Egyptian frontier' and no map of the Memorandum or of the agreement was found. Art. VI.3 bounds it: Israel "
    "evacuates its outpost at the Cemetery (MR 0816-0723), so that point is inside the line, and new Israeli outposts may "
    "be set up at MR 0836-0700, east of it, so the line reaches the frontier south-east of Rafah, between about N 70 and N "
    "72 km. Carrying the highway's bearing on from Gaza reaches the frontier at about MR 077.7-075.6, north of the Cemetery, "
    "and is therefore wrong; a 3-km offset of the coast road through Khan Yunis (MR 083.7-084.2) and Rafah (078.1-077.6), "
    "carried on its bearing, reaches the frontier near N 73, also north-west of the Cemetery. The Egyptian-held area is not drawn "
    "either, since it needs the whole line.")
S1471 = {"cite": "Cross-check of the 1949 line (this feature only): S/1471, sheet 1 of the signed map of the modus vivendi of 22 "
                 "February 1950 (Survey of Palestine 1:20,000 base, Jaffa 1931, revised June 1946, with a Survey of Israel reprint imprint), UNISPAL; the 1950 line itself is the separate feature egypt_mv1950_demarcation. The "
                 "map's dash-dot demarcation line lies on this 1949 line within about 0.1 km from MR 102.25-100.0 to 106.95-102.40 "
                 "(the sheet's south neatline to the text's point); north-east of that it shows only the zone A line.",
         "url": "https://www.un.org/unispal/wp-content/uploads/1950/03/02c98322cf78b99a8525705b004d7b73_s1471.tif",
         "sha256": "55083142da168abffccb2db7f0da5516b6a26c8d3b8a7f2f4c11a982ed822434", "checked": CHECKED,
         "licence": "Public domain (UN document with a symbol)"}
IBS46 = {"cite": "US Department of State, International Boundary Study No. 46, 'Israel - Egypt (United Arab Republic) Armistice "
                 "Line' (1965): the Gaza Strip sector of the line 'dates from February 22, 1950'; elsewhere the line follows the "
                 "1906 Egypt-Palestine boundary from 24 February 1949. Secondary; cross-check only.",
         "url": "https://library.law.fsu.edu/Digital-Collections/LimitsinSeas/pdf/ibs046.pdf", "checked": CHECKED,
         "licence": "Public domain (US Government work); cross-check only"}


def gaza_sheet(path: Path, log) -> tuple[dict, list]:
    """Georeference sheet 9 on its 10-km grid and trace the first-class road from Deir Suneid to Gaza."""
    from PIL import Image
    from scipy import ndimage as ndi
    from skimage.graph import route_through_array
    Image.MAX_IMAGE_PIXELS = None
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.int16)
    gray = rgb.mean(axis=2).astype(np.float32)
    obs_e, obs_n = [], []
    for gx, e in GAZA_GRID["v"].items():
        for y0 in range(1500, 11800, 300):
            p = gray[y0:y0 + 150, gx - 80:gx + 81].mean(axis=0); q = ndi.uniform_filter1d(p, 31) - p
            k = int(np.argmax(q))
            if q[k] > 8:
                obs_e.append((gx - 80 + k, y0 + 75, e))
    for gy, n in GAZA_GRID["h"].items():
        for x0 in range(1500, 11900, 300):
            p = gray[gy - 80:gy + 81, x0:x0 + 150].mean(axis=1); q = ndi.uniform_filter1d(p, 31) - p
            k = int(np.argmax(q))
            if q[k] > 8:
                obs_n.append((x0 + 75, gy - 80 + k, n))
    oe, on = np.array(obs_e, float), np.array(obs_n, float)
    a = np.linalg.lstsq(np.c_[np.ones(len(oe)), oe[:, :2]], oe[:, 2], rcond=None)[0]
    b = np.linalg.lstsq(np.c_[np.ones(len(on)), on[:, :2]], on[:, 2], rcond=None)[0]
    rms = [float(np.sqrt(((np.c_[np.ones(len(oe)), oe[:, :2]] @ a - oe[:, 2]) ** 2).mean())) * 1000,
           float(np.sqrt(((np.c_[np.ones(len(on)), on[:, :2]] @ b - on[:, 2]) ** 2).mean())) * 1000]
    fit = {"E": a, "N": b, "rms_m": rms, "obs": [len(oe), len(on)]}
    log(f"Gaza sheet: {fit['obs']} grid observations, rms {rms[0]:.0f} / {rms[1]:.0f} m")
    r, g, bl = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    road = (r > 140) & (g < 100) & (bl < 90) & (r - g > 80)
    road &= ndi.binary_dilation((r < 100) & (g < 100) & (bl < 100), iterations=8)   # the red fill between black casings
    del rgb, gray
    (xa, ya), (xb, yb) = GAZA_PX["highway_deir_suneid"], GAZA_PX["highway_gaza"]
    m = 200
    by0, bx0 = min(ya, yb) - m, min(xa, xb) - m
    sub = np.where(road[by0:max(ya, yb) + m, bx0:max(xa, xb) + m], 1.0, 25.0)
    path, _ = route_through_array(sub, (ya - by0, xa - bx0), (yb - by0, xb - bx0), fully_connected=True, geometric=True)
    path = np.array(path, float) + [by0, bx0]
    on_road = float(road[path[:, 0].astype(int), path[:, 1].astype(int)].mean())
    from shapely.geometry import LineString
    xy = np.array(LineString(path[:, ::-1]).simplify(2.0).coords)
    highway = list(zip(*px_to_en(fit, xy[:, 0], xy[:, 1])))
    fit["highway_on_red_road"] = round(on_road, 3)
    log(f"Gaza sheet: highway traced Deir Suneid -> Gaza, {len(highway)} vertices, {on_road:.0%} on the red road")
    return fit, highway


def mandate_land():
    """The union of the 1948 sub-districts (Mandatory Palestine's land; coast from the DEM) in Palestine-grid km."""
    import shapely
    from shapely.geometry import shape
    doc = json.loads(SUBDISTRICTS.read_text(encoding="utf-8"))
    land = shapely.union_all([shape(f["geometry"]) for f in doc["features"]])
    return shapely.transform(land, lambda c: np.c_[lonlat_to_mr(c[:, 0], c[:, 1])])


def frontier_path(land, a, b):
    """The stretch of Mandatory Palestine's outline between the outline points nearest a and b (km), the shorter way
    round (both stretches used here are land frontiers, far shorter than the rest of the outline)."""
    from shapely.geometry import LineString, Point
    from shapely.ops import substring
    ring = max(land.geoms, key=lambda p: p.area).exterior if land.geom_type == "MultiPolygon" else land.exterior
    ring = LineString(ring.coords)
    da, db = ring.project(Point(a)), ring.project(Point(b))
    lo, hi = sorted((da, db))
    inner = substring(ring, lo, hi)
    outer_len = ring.length - inner.length
    if inner.length <= outer_len:
        seg = list(inner.coords)
    else:
        seg = list(substring(ring, hi, ring.length).coords) + list(substring(ring, 0, lo).coords)[1:]
    if Point(seg[0]).distance(Point(a)) > Point(seg[-1]).distance(Point(a)):
        seg = seg[::-1]
    return seg, ring.distance(Point(a)), ring.distance(Point(b))


def egypt(gaza_path: Path, log) -> dict:
    import shapely
    from shapely.geometry import LineString, Point, Polygon
    fit, highway = gaza_sheet(gaza_path, log)
    mouth = tuple(float(v) for v in px_to_en(fit, *GAZA_PX["wadi_hasi_mouth"]))
    ds = tuple(float(v) for v in px_to_en(fit, *GAZA_PX["deir_suneid"]))
    hw = LineString(highway)
    # the line 3 km east (inland, the side away from the sea) of the highway: offset to the left of the road as traced
    # from Deir Suneid (north-east) to Gaza (south-west) is the south-east side
    off = hw.offset_curve(OFFSET_KM, join_style="round", quad_segs=4)
    if off.distance(Point(mouth)) < hw.distance(Point(mouth)):
        off = hw.offset_curve(-OFFSET_KM, join_style="round", quad_segs=4)
    off = LineString(off.coords) if off.geom_type == "LineString" else max(off.geoms, key=lambda g: g.length)
    oc = np.array(off.coords)
    if np.hypot(*(oc[0] - np.array(ds))) > np.hypot(*(oc[-1] - np.array(ds))):
        oc = oc[::-1]
    # from the mouth "in an easterly direction through Deir Suneid and across the highway to a point 3 km east of the
    # highway": the mouth -> Deir Suneid direction, carried on to the offset line
    d = np.array(ds) - np.array(mouth); d /= np.hypot(*d)
    ray = LineString([ds, tuple(np.array(ds) + d * 10.0)])
    hit = ray.intersection(LineString(oc))
    start = (hit if hit.geom_type == "Point" else min(hit.geoms, key=lambda p: p.distance(Point(ds))))
    k = LineString(oc).project(start)
    from shapely.ops import substring
    along = list(substring(LineString(oc), k, LineString(oc).length).coords)
    # "then in a southerly direction parallel to the Gaza-Al Majdal Highway": the offset as far as the highway goes, to
    # Gaza. "And continuing thus to the Egyptian frontier" is not drawn: no map of it was found, and Art. VI.3 rules out
    # the plain readings (EGYPT_SOUTH_GAP).
    adl = [mouth, ds] + along
    adl_line = LineString(adl)
    land = mandate_land()
    # El Auja: the four Art. VIII.2 points, closed back along the frontier (P4 -> P1)
    p1, p4 = (EL_AUJA[0][1], EL_AUJA[0][2]), (EL_AUJA[3][1], EL_AUJA[3][2])
    seg, d1, d4 = frontier_path(land, p4, p1)
    auja = Polygon([(e, n) for _, e, n in EL_AUJA] + seg[1:-1])
    auja = shapely.make_valid(auja)
    checks = []
    for name, e, n, kind in GAZA_OUTPOSTS:
        p = Point(e, n)
        checks.append({"point": name, "mr": [e, n], "kind": kind, "km_from_drawn_line": round(adl_line.distance(p), 2)})
    return {"fit": fit, "mouth": mouth, "deir_suneid": ds, "highway": highway, "adl": adl, "auja": auja,
            "frontier_offsets_km": {"El Auja P1": round(d1, 2), "El Auja P4": round(d4, 2)}, "outposts": checks,
            "highway_km": round(hw.length, 1)}


# --- Egypt, 1950: the modus vivendi (S/1471, signed at El Auja on 22 February 1950) ---------------------------------

MV_SHEET1 = {"cite": "S/1471, signed map of the modus vivendi, sheet 1 'Ascalon' (1:20,000, Survey of Palestine 1:20,000 base, Jaffa 1931, revised June 1946, with a Survey of Israel reprint imprint), "
                     "1-bit scan on UNISPAL: the demarcation line as the parties drew it in 1950 (dash-dot) and the zone A line.",
             "url": "https://www.un.org/unispal/wp-content/uploads/1950/03/02c98322cf78b99a8525705b004d7b73_s1471.tif",
             "sha256": "55083142da168abffccb2db7f0da5516b6a26c8d3b8a7f2f4c11a982ed822434", "checked": CHECKED,
             "licence": "Public domain (UN document with a symbol, ST/AI/189/Add.9/Rev.2 para. 2)"}
MV_SHEET2 = {"cite": "The same map, sheet 2 'Khan Yunis' (1:20,000), the copy in the UN Treaty Collection (registered 14 August 1950): "
                     "the demarcation line south of Khan Yunis and the zone C line.",
             "url": "https://treaties.un.org/doc/Treaties/1950/08/19500814%2004-17%20PM/khan_yunis.pdf",
             "sha256": "530866817158a041bb60d94e437af30de7a19068540ef6910575c07aa8b3b333", "checked": CHECKED,
             "licence": MV_SHEET1["licence"]}
MV_TEXT = {"cite": "S/1471 (17 March 1950), 'Modus vivendi to the Egyptian-Israeli General Armistice Agreement signed at El Auja "
                   "on 22 February 1950', Art. I.1 (zones A, B, C by MR points), Art. III (it does not modify either party's "
                   "position on the Art. VI.1 demarcation line); the zones are those 'on the map attached' (Art. I.1).",
           "url": "https://documents.un.org/api/symbol/access?s=S/1471&l=en&t=pdf", "checked": CHECKED,
           "sha256": "3d1807500b9b573d754da382d2be8e44c0e2469a5703894f16369234be2f9ff7", "licence": MV_SHEET1["licence"]}
MV_DATE = "1950-02-22"
MV_AGREEMENT = ("Modus vivendi to the Egypt-Israel General Armistice Agreement", MV_DATE, "El Auja", "S/1471")
# The demarcation line as drawn (dash-dot) on the two signed sheets: waypoints (MR km) read off the georeferenced scans,
# snapped to the stroke and joined straight (no routing: review C-1). Points the text names on the line are exact (marked "text").
MV_PIECES = [   # (sheet, MR km waypoints along the drawn dash-dot, read off gridded crops of the georeferenced scans)
    ("sheet1", [(102.25, 100.00), (102.52, 100.22), (103.31, 100.70), (103.86, 101.06), (104.89, 101.37), (105.52, 101.73),
                (106.00, 102.05), (106.95, 102.40)]),
    ("sheet2", [(90.05, 87.11), (89.68, 86.83), (89.51, 86.56), (89.38, 86.19), (89.35, 85.90), (89.21, 85.65), (89.07, 85.25)]),
    ("sheet2", [(87.63, 77.31), (87.35, 76.70), (86.95, 75.82), (86.58, 75.32), (86.08, 74.70), (85.65, 74.26), (85.03, 73.76),
                (84.17, 73.39), (83.49, 73.07), (82.74, 72.61)]),
]
MV_TEXT_POINTS = {(106.95, 102.40), (89.35, 85.90), (87.35, 76.70)}   # Art. I.1(a) and (c): points on the demarcation line
# Art. I.1 zone lines, exactly as the text gives them (MR "10170 - 11160" = 101.70 km E, 111.60 km N)
MV_ZONE_A = [(101.70, 111.60), (106.90, 107.40), (107.95, 106.40), (109.00, 105.65), (108.80, 104.80), (108.10, 104.10), (106.95, 102.40)]
MV_ZONE_C = [(89.35, 85.90), (90.35, 79.70), (89.70, 77.90), (87.35, 76.70)]
MV_GAPS = ("The signed map draws the demarcation line only in pieces: on sheet 1 from its south neatline (N 100) to MR 106.95-102.40 "
           "(the text's point; north-east of it the dotted line is zone A's); on sheet 2 from MR 90.05-87.11 to 89.07-85.25, and from "
           "MR 87.63-77.31 to 82.74-72.61. North of N 102.6 the 1949 line "
           "is not redrawn on the map; between N 85.4 and 77.3 zone C lies against it but no demarcation line is drawn; the two "
           "sheets do not cover N 90-100; south-west of MR 82.8-72.6 nothing is drawn. Those stretches are not drawn here.")


def egypt_1950(sheet1: Path, sheet2: Path, log) -> dict:
    """The demarcation line as drawn on the two signed sheets of S/1471, traced between waypoints."""
    from PIL import Image
    from scipy import ndimage as ndi
    from shapely.geometry import LineString
    Image.MAX_IMAGE_PIXELS = None
    d1 = ~np.asarray(Image.open(sheet1).convert("1"))
    f1 = grid_fit(d1.astype(np.float32), d1.astype(np.float32),
                  {"y": (470, 9900), "x": (390, 4300), "anchor": ((772, 2000, 101.0), (2000, 850, 123.0))}, 390.0, 0.3, 150, True)
    g2 = np.asarray(pdf_page_image(sheet2, 0).convert("L")).astype(np.float32)
    f2 = grid_fit(ndi.uniform_filter1d(g2, 25, axis=1) - g2, ndi.uniform_filter1d(g2, 25, axis=0) - g2,
                  {"y": (470, 6700), "x": (210, 3100), "anchor": ((193, 2000, 80.0), (1500, 452, 90.0))}, (295.0, 295.0), 6.0, 120, False)
    d2 = g2 < 110
    for k, f in (("sheet 1", f1), ("sheet 2", f2)):
        log(f"S/1471 {k}: {f['obs']} grid observations, rms {f['rms_m'][0]:.0f} / {f['rms_m'][1]:.0f} m")
    # The pieces are the waypoints joined straight (the dash-dot is near-straight between them); no routing, which on a 1-bit
    # scan follows the neatline and grid lines as readily as the drawn line (review C-1, 2026-10-11). Diagnostic: the share
    # of points every 10 m along each piece with dark ink within 3 px (a dash-dot is about 60-70% ink).
    pieces, on_ink, snaps = [], [], []
    thick2 = ndi.binary_opening(g2 < 160, structure=np.ones((3, 3)))   # sheet 2's dash-dot is grey and about 4-6 px wide
    thick1 = ndi.binary_opening(d1, structure=np.ones((3, 3)))
    for sheet, wps in MV_PIECES:
        thick, fit, ppk = (thick1, f1, 390.0) if sheet == "sheet1" else (thick2, f2, 296.0)
        _, (iy, ix) = ndi.distance_transform_edt(~thick, return_indices=True)
        out = []
        for e, n in wps:                               # each read waypoint snapped to the stroke within 0.1 km
            x, y = (int(round(v)) for v in en_to_px(fit, e, n))
            if (e, n) not in MV_TEXT_POINTS and np.hypot(ix[y, x] - x, iy[y, x] - y) <= 0.1 * ppk:
                x, y = int(ix[y, x]), int(iy[y, x])
                snaps.append(round(float(np.hypot(*(np.array(px_to_en(fit, x, y)) - [e, n]))) * 1000))
                out.append(tuple(float(v) for v in px_to_en(fit, x, y)))
            else:
                out.append((e, n))
        near = ndi.binary_dilation(thick, iterations=3)
        line = LineString(out)
        hits = [bool(near[int(round(yy)), int(round(xx))]) for xx, yy in
                (en_to_px(fit, q.x, q.y) for q in (line.interpolate(d) for d in np.arange(0, line.length, 0.01)))]
        pieces.append(out); on_ink.append(round(float(np.mean(hits)), 3))
    log(f"S/1471: waypoints snapped by a median {np.median(snaps):.0f} m (max {max(snaps)} m); ink along the pieces {on_ink}")
    return {"pieces": pieces, "on_ink": on_ink, "rms_m": {"sheet1": [round(x, 1) for x in f1["rms_m"]], "sheet2": [round(x, 1) for x in f2["rms_m"]]}}


# --- Jordan: map 1 of Annex I, from the low-resolution UNISPAL copy (owner ruling 2026-10-11) -------------------------

JORDAN_MAP = {"cite": "Annex I, map 1 of the Jordan-Israel General Armistice Agreement (S/1302/Rev.1): the Survey of Palestine "
                      "1:250,000 'Palestine' north and south sheets with the lines drawn by hand and signed, as the UNISPAL page's "
                      "original upload (3119 x 8630 px, about 51 m a pixel). Low resolution: to be replaced from a full-resolution "
                      "scan (UN Archives, Israel State Archives, ECF).",
              "url": "https://www.un.org/unispal/wp-content/uploads/1949/04/f03d55e48f77ab698525643b00608d34_Arm_1949.jpg",
              "page": "https://www.un.org/unispal/document/auto-insert-181069/",
              "sha256": "87c374a3199d2e606278549b017c954f56271817f3be37b2f435192f5c9cb23e", "checked": CHECKED,
              "licence": "Public domain (UN document with a symbol, ST/AI/189/Add.9/Rev.2 para. 2); base map a Government of Palestine work (D-B)"}
JORDAN_TOWNS = {"cite": "Control-point coordinates: Wikidata (P625) and the Wikipedia API (prop=coordinates), and "
                        "content/map/settlements_1948.yaml, read 2026-10-11; used only to georeference the scan.",
                "url": "https://www.wikidata.org/", "checked": CHECKED, "licence": "CC0 (Wikidata) / CC BY-SA 4.0 (Wikipedia)"}
JORDAN_CONTROL = [   # (place, lon, lat (Wikidata/Wikipedia, 2026-10-11), scan x, y read off the town symbol)
    ('Ramallah', 35.2017, 31.8969, 1953, 3193), ('Jerusalem', 35.2256, 31.7789, 1975, 3478),
    ('Bethlehem', 35.2064, 31.7044, 1943, 3625), ('Lydda', 34.8881, 31.9519, 1384, 3096),
    ('Ramla', 34.8625, 31.9275, 1334, 3151), ('Jaffa', 34.7531, 32.0522, 1105, 2875),
    ('Nazareth', 35.3033, 32.7019, 2125, 1463), ('Afula', 35.2897, 32.6078, 2104, 1674),
    ('Jenin', 35.3, 32.4611, 2123, 1996), ('Haifa', 34.9992, 32.8192, 1568, 1227),
    ('Tiberias', 35.5333, 32.7944, 2563, 1279), ('Umm al-Fahm', 35.1536, 32.5194, 1851, 1874),
    ('Beit Shean', 35.5, 32.5, 2501, 1917), ('Hadera', 34.9167, 32.45, 1420, 2038),
    ('Tulkarm', 35.0308, 32.3111, 1619, 2318), ('Qalqilya', 34.9685, 32.1904, 1498, 2583),
    ('Kafr Qasim', 34.9772, 32.1142, 1524, 2749), ('Nablus', 35.2608, 32.2211, 2055, 2509),
    ('Jericho', 35.46, 31.8561, 2413, 3310), ('Hebron', 35.0969, 31.5286, 1750, 4025),
    ('Bayt Jibrin', 34.8983, 31.6053, 1366, 3846), ('Gaza', 34.4597, 31.5075, 562, 4068),
    ('Beersheba', 34.7867, 31.2522, 1168, 4647), ('Khan Yunis', 34.3031, 31.3444, 260, 4415),
    ('Rafah', 34.2519, 31.2886, 141, 4512), ('Masada', 35.3539, 31.3156, 2226, 4477),
    ('Ein Gedi', 35.3833, 31.45, 2305, 4155), ('El Auja', 34.4226, 30.886, 495, 5436),
    ('Ein Husb', 35.2467, 30.7981, 2020, 5589), ('Aqaba', 35.0061, 29.5322, 1559, 8377),
]
JORDAN_WAYPOINTS = {   # scan px, read off the hand-drawn lines (each snapped to the nearest pen ink, then routed along it)
    'A_iraqi': [(2600, 2150), (2556, 2137), (2512, 2125), (2462, 2112), (2406, 2094), (2369, 2062), (2344, 2006), (2325,
        1956), (2281, 1881), (2240, 1868), (2160, 1872), (2100, 1875), (2070, 1865), (2030, 1825), (2000, 1797),
        (1985, 1795), (1965, 1815), (1925, 1835), (1875, 1875), (1830, 1900), (1800, 1944), (1750, 1975), (1712,
        2006), (1687, 2037), (1669, 2087), (1662, 2119), (1656, 2150), (1644, 2194), (1631, 2237), (1606, 2270),
        (1590, 2330), (1585, 2370), (1600, 2410), (1610, 2440), (1600, 2470), (1590, 2510), (1570, 2545), (1555,
        2580), (1542, 2620), (1548, 2670), (1555, 2720), (1560, 2780), (1560, 2840), (1560, 2900), (1560, 2960),
        (1560, 3005)],
    'a_latrun': [(1560, 3005), (1566, 3047), (1556, 3062), (1575, 3129), (1600, 3154), (1619, 3185), (1635, 3220), (1635,
        3285), (1610, 3297), (1565, 3297), (1530, 3310), (1505, 3335), (1505, 3360), (1540, 3365), (1600, 3365),
        (1620, 3330), (1680, 3318), (1740, 3332), (1800, 3346), (1870, 3360), (1930, 3378), (1950, 3395)],
    'bc_south': [(1895, 3570), (1900, 3590), (1870, 3600), (1830, 3620), (1790, 3640), (1740, 3660), (1690, 3690), (1640,
        3720), (1580, 3760), (1540, 3790), (1500, 3830), (1464, 3951), (1457, 4001), (1450, 4073), (1421, 4130),
        (1364, 4230), (1330, 4283), (1383, 4390), (1423, 4397), (1520, 4393), (1620, 4388), (1720, 4382), (1810,
        4377), (1900, 4362), (1997, 4343), (2037, 4310), (2103, 4250), (2183, 4190), (2240, 4140), (2300, 4090)],
    'd_arava': [(2300, 4090), (2370, 4092), (2436, 4100), (2436, 4250), (2414, 4336), (2371, 4421), (2343, 4507), (2329,
        4607), (2343, 4679), (2386, 4750), (2414, 4850), (2421, 4921), (2400, 4993), (2357, 5029), (2230, 5330),
        (2114, 5600), (2035, 5870), (1960, 6140), (1900, 6433), (1842, 6833), (1783, 7200), (1733, 7550), (1683,
        7900), (1615, 8215), (1600, 8300)],
}
JORDAN_STAGES = (
    "Art. VI.3 stages for line A, as read here (the article names areas, not points): (a) 'In the area west of the road from "
    "Baqa to Jaljulia, and thence to the east of Kafr Qasim: within five weeks' = line A from Baqa south to Kafr Qasim, by 8 May "
    "1949; (b) 'In the area of Wadi Ara north of the line from Baqa to Zubeiba: within seven weeks' = line A from Zubeiba round "
    "Wadi Ara to Baqa, by 22 May 1949; (c) 'In all other areas of the Iraqi sector: within fifteen weeks' = line A east of "
    "Zubeiba to the Jordan and from Kafr Qasim to Kh Deir Arab (MR 1510-1574), by 17 July 1949. Split at the trace's points "
    "nearest Baqa al-Gharbiyye, Zubeiba (Wikidata 'Zububa') and Kafr Qasim. The dates are deadlines ('within ... weeks'; "
    "'pending which the existing military lines may be maintained'): each stretch is shown from its deadline, a convention; "
    "the actual handover dates (May-July 1949) are not researched.")
JORDAN_STAGE_PLACES = {"Zubeiba": (35.2236, 32.5455), "Baqa": (35.0422, 32.4203), "Kafr Qasim": (34.9772, 32.1142)}
# The text's "Zubeiba" is the village Wikidata calls Zububa (Q2911499), north-west of Jenin; the text's spelling is used.
JORDAN_KH_DEIR_ARAB = (151.0, 157.4)    # Art. V.1(a): Kh Deir Arab, MR 1510-1574
JORDAN_DEAD_SEA_POINT = (192.5, 95.8)   # Art. V.1(d): a point on the Dead Sea, MR 1925-0958
JORDAN_GAPS = (
    "Not drawn (legibility, low-resolution scan): the Jerusalem sector (Art. V.1(b): the lines of the 30 November 1948 "
    "cease-fire, map 2, not on UNISPAL; on map 1 the city's lines merge into one blot at 51 m a pixel), so the line stops at "
    "the city's western edge on both sides; the Israeli and Jordanian lines and the no-man's-land between them, in "
    "Jerusalem and at Latrun (map 1 shows a double line at Latrun, but the two cannot be told apart at this resolution; the "
    "trace follows the outer drawn loop); the Mount Scopus enclave. To do from map 2 and a full-resolution map 1.")


def jordan(path: Path, log) -> dict:
    """Georeference map 1 on control points (town symbols on the map, Wikidata coordinates; the printed grid on this copy
    is not usable: its fitted lines put the towns 5-8 km off) and trace each hand-drawn line between waypoints."""
    from PIL import Image
    from scipy import ndimage as ndi
    from shapely.geometry import LineString, Point
    from skimage.graph import route_through_array
    Image.MAX_IMAGE_PIXELS = None
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lum = (r + g + b) / 3.0
    del a
    ink = (lum < 70) & (r - g < 25)            # the pen lines are blackish green: dark, unlike the red-brown roads
    yy, xx = np.mgrid[-2:3, -2:3]
    thick = ndi.binary_opening((lum < 95) & (r - g < 30), structure=(xx ** 2 + yy ** 2) <= 4)
    cost = np.where(ink, 1.0, np.where(lum < 110, 6.0, 25.0))
    cost_thick = np.where(thick, 1.0, np.where(ink, 8.0, 40.0))
    del r, g, b, lum
    _, (iy, ix) = ndi.distance_transform_edt(~ink, return_indices=True)
    _, (ty, tx) = ndi.distance_transform_edt(~thick, return_indices=True)
    # control-point georeference, one affine for the north sheet and one for the south (they meet near y 4050)
    fits, diag = {}, {}
    for sheet in ("north", "south"):
        pts = [c for c in JORDAN_CONTROL if (c[4] < 4050) == (sheet == "north")]
        en = np.array([[float(v) for v in lonlat_to_mr(c[1], c[2])] for c in pts])
        px = np.array([[c[3], c[4]] for c in pts], float)
        def fit(idx):
            aa = np.c_[np.ones(len(idx)), px[idx]]
            return np.linalg.lstsq(aa, en[idx, 0], rcond=None)[0], np.linalg.lstsq(aa, en[idx, 1], rcond=None)[0]
        all_i = np.arange(len(pts)); fe, fn = fit(all_i)
        res = np.hypot(np.c_[np.ones(len(pts)), px] @ fe - en[:, 0], np.c_[np.ones(len(pts)), px] @ fn - en[:, 1])
        loo = []
        for i in all_i:
            ce, cn = fit(all_i[all_i != i]); q = np.array([1, *px[i]])
            loo.append(float(np.hypot(q @ ce - en[i, 0], q @ cn - en[i, 1])))
        fits[sheet] = (fe, fn)
        diag[sheet] = {"control_points": len(pts), "rms_km": round(float(np.sqrt((res ** 2).mean())), 2),
                       "leave_one_out_rms_km": round(float(np.sqrt(np.mean(np.square(loo)))), 2), "leave_one_out_max_km": round(max(loo), 2)}
        log(f"Jordan map 1, {sheet}: {len(pts)} control points, rms {diag[sheet]['rms_km']} km, leave-one-out {diag[sheet]['leave_one_out_rms_km']} km (max {diag[sheet]['leave_one_out_max_km']})")

    def to_en(x, y):
        fe, fn = fits["north" if y < 4050 else "south"]
        return float(fe @ [1, x, y]), float(fn @ [1, x, y])
    traces = {}
    for name, wps in JORDAN_WAYPOINTS.items():
        th = name == "d_arava"
        def snap(x, y):
            jx, jy, rad = (tx, ty, 40) if th else (ix, iy, 15)
            return (int(jx[y, x]), int(jy[y, x])) if np.hypot(jx[y, x] - x, jy[y, x] - y) <= rad else None
        pts = []
        for i, (x, y) in enumerate(wps):
            s = snap(x, y)
            if s is None and th and 0 < i < len(wps) - 1:
                continue                                  # an inner waypoint off the pen line: dropped
            pts.append(s or (x, y))
        cst = cost_thick if th else cost
        m = 60 if th else 25
        path, hits = [pts[0]], []
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            bx0, by0 = max(min(x0, x1) - m, 0), max(min(y0, y1) - m, 0)
            sub = cst[by0:max(y0, y1) + m, bx0:max(x0, x1) + m]
            p_, _ = route_through_array(sub, (y0 - by0, x0 - bx0), (y1 - by0, x1 - bx0), fully_connected=True, geometric=True)
            p_ = [(x + bx0, y + by0) for y, x in p_]
            hits += [bool(ink[y, x]) for x, y in p_]
            path += p_[1:]
        clean = []                                        # out-and-back spurs cut
        for q in path:
            for j in range(len(clean) - 8, max(len(clean) - 400, -1), -1):
                if j >= 0 and abs(clean[j][0] - q[0]) <= 2 and abs(clean[j][1] - q[1]) <= 2:
                    del clean[j + 1:]; break
            clean.append(q)
        xy = np.array(LineString(clean).simplify(2.0).coords)
        traces[name] = {"en": [to_en(x, y) for x, y in xy], "on_ink": round(float(np.mean(hits)), 3)}
        log(f"Jordan map 1: {name} traced, {len(xy)} vertices, {traces[name]['on_ink']:.0%} on the pen ink")
    # sector boundaries at the text's own MR points (review I-1): Art. V.1(a) starts at Kh Deir Arab (MR 1510-1574), so
    # line A ends there; Art. V.1(d) starts at the Dead Sea point (MR 1925-0958). The traces are joined and cut at the
    # trace point nearest each.
    from shapely.ops import substring
    def cut_at(a: list, b: list, mr: tuple) -> tuple:
        joined = LineString(a + b[1:])
        k = joined.project(Point(mr))
        return substring(joined, 0, k), substring(joined, k, joined.length), round(float(joined.distance(Point(mr))), 2)
    line_a, sector_a, d_kda = cut_at(traces["A_iraqi"]["en"], traces["a_latrun"]["en"], JORDAN_KH_DEIR_ARAB)
    line_b, sector_d, d_ds = cut_at(traces["bc_south"]["en"], traces["d_arava"]["en"], JORDAN_DEAD_SEA_POINT)
    sectors = {"a_latrun": sector_a, "bc_south": line_b, "d_arava": sector_d}
    cuts = {"kh_deir_arab_km": d_kda, "dead_sea_point_km": d_ds}
    log(f"Jordan map 1: sector cuts, the trace passes {d_kda} km from Kh Deir Arab and {d_ds} km from the Dead Sea point")
    # line A split into Art. VI.3's stages
    a_line = line_a
    cut = {k: a_line.project(Point(*[float(v) for v in lonlat_to_mr(*ll)])) for k, ll in JORDAN_STAGE_PLACES.items()}
    z, bq, kq = cut["Zubeiba"], cut["Baqa"], cut["Kafr Qasim"]
    if not 0 < z < bq < kq < a_line.length:
        raise SystemExit(f"line A: the stage places are not in order along the trace ({cut})")
    stages = {"c_east": substring(a_line, 0, z), "b_wadi_ara": substring(a_line, z, bq), "a_triangle": substring(a_line, bq, kq),
              "c_south": substring(a_line, kq, a_line.length)}
    return {"traces": traces, "stages": stages, "sectors": sectors, "cuts": cuts, "georeference": diag}


# --- Lebanon --------------------------------------------------------------------------------------------------------

NAQURA = (35.0843, 33.0986)     # where the 1:750,000 frontier meets the DEM sea at Ras en Naqura (tools/borders_digitise.py)
RAS_EN_NAQURA = (35.10486, 33.09855)   # the frontier's end on land there (borders_digitise.py's PATCHES connector start)


def lebanon() -> tuple[list, dict]:
    land = mandate_land()
    a = tuple(float(v) for v in lonlat_to_mr(*NAQURA))
    b = (SYRIA_POINTS[0][1], SYRIA_POINTS[0][2])
    seg, da, db = frontier_path(land, a, b)
    # the frontier's land end at Ras en Naqura (where tools/borders_digitise.py's connector to the DEM sea starts)
    from shapely.geometry import Point
    rn = tuple(float(v) for v in lonlat_to_mr(*RAS_EN_NAQURA))
    return seg, {"tripoint_offset_km": round(db, 2), "start_to_ras_en_naqura_km": round(Point(seg[0]).distance(Point(rn)), 2)}


# --- output ---------------------------------------------------------------------------------------------------------

AGREEMENTS = {
    "egypt": ("Egypt-Israel General Armistice Agreement", "1949-02-24", "Rhodes", "S/1264/Corr.1; UNTS vol. 42, No. 654"),
    "lebanon": ("Lebanon-Israel General Armistice Agreement", "1949-03-23", "Ras en Naqura", "S/1296; UNTS vol. 42, No. 655"),
    "jordan": ("Hashemite Jordan Kingdom-Israel General Armistice Agreement", "1949-04-03", "Rhodes", "S/1302/Rev.1; UNTS vol. 42, No. 656"),
    "syria": ("Syria-Israel General Armistice Agreement", "1949-07-20", "Hill 232 near Mahanayim", "S/1353; UNTS vol. 42, No. 657"),
}


def km_geom_to_lonlat(g):
    import shapely
    return shapely.transform(g, lambda c: np.c_[mr_to_lonlat(c[:, 0], c[:, 1])])


def feature(fid, front, kind, name, geom_km, accuracy, method, sources, extra=None, agreement=None):
    import shapely
    from shapely.geometry import mapping
    g = shapely.set_precision(km_geom_to_lonlat(geom_km), 1e-5)
    if kind == "zone":
        g = shapely.orient_polygons(g)
    agr, date, place, doc = agreement or AGREEMENTS[front]
    props = {"id": fid, "front": front, "kind": kind, "name": name, "agreement": agr, "signed": date, "place": place,
             "document": doc, "shown_from": date, "accuracy": accuracy, "method": method,
             "sources": [s["url"] for s in sources], "checked": CHECKED, "status": "draft"}
    props.update(extra or {})
    if fid in SIGNED_OFF_IDS:
        props.update(SIGNED_OFF)
    return {"type": "Feature", "properties": props, "geometry": mapping(g)}


def write(doc: dict, path: Path) -> None:
    """Deterministic, diff-friendly text: metadata indented, then per feature one line of properties and one line per
    vertex (5-decimal lon/lat)."""
    def ring(r) -> str:
        return "[" + ",\n    ".join(json.dumps(v, separators=(",", ":")) for v in r) + "]"

    def geom_text(g: dict) -> str:
        t, c = g["type"], g["coordinates"]
        if t == "LineString":
            inner = ring(c)
        elif t == "Polygon":
            inner = "[\n   " + ",\n   ".join(ring(r) for r in c) + "]"
        else:
            inner = "[\n   " + ",\n   ".join("[" + ",\n    ".join(ring(r) for r in poly) + "]" for poly in c) + "]"
        return '{"type":"' + t + '","coordinates":' + inner + "}"
    meta = json.dumps({k: v for k, v in doc.items() if k != "features"}, ensure_ascii=False, indent=1)
    feats = ",\n".join('{"type":"Feature",\n  "properties":' + json.dumps(f["properties"], ensure_ascii=False, separators=(",", ":"))
                       + ',\n  "geometry":' + geom_text(json.loads(json.dumps(f["geometry"]))) + "}" for f in doc["features"])
    text = meta[:-2] + ',\n "features":[\n' + feats + "\n]}\n"
    assert json.loads(text) == json.loads(json.dumps(doc)), "write() must round-trip"
    path.write_text(text, encoding="utf-8")


def build(syria_pdf: Path, syria_tif: Path, gaza: Path, mv1: Path, mv2: Path, jordan_map: Path, log=print) -> dict:
    import shapely
    from shapely.geometry import LineString, MultiPolygon, Point
    check(syria_pdf, SYRIA_PDF); check(syria_tif, SYRIA_TIF); check(gaza, GAZA_SHEET); check(mv1, MV_SHEET1); check(mv2, MV_SHEET2); check(jordan_map, JORDAN_MAP)
    feats = []
    # Lebanon
    leb, leb_diag = lebanon()
    feats.append(feature("lebanon_adl", "lebanon", "line", "Armistice Demarcation Line (Lebanon-Israel): the international boundary",
                         LineString(leb),
                         f"about +-1 km: the Mandate frontier digitised from the Survey of Palestine 1:750,000 map for the 1948 sub-districts "
                         f"(its uncertain segments, Ras en Naqura and the Metulla tip, to about +-1.5 km); its east end lies "
                         f"{leb_diag['tripoint_offset_km']} km from the tripoint of the Syria agreement's Annex I (MR 208.7-294.2).",
                         "Art. V.1: 'The Armistice Demarcation Line shall follow the international boundary between the Lebanon and "
                         "Palestine.' The boundary is the 1923 line (boundary pillars), taken as the Mandate frontier already "
                         "digitised (content/map/subdistricts_1948.geojson), from the coast at Ras en Naqura to the Lebanon-Syria-Palestine tripoint.",
                         [UNTS, MANDATE_FRONTIER], {"diagnostics": leb_diag}))
    # Syria
    verts, diag = syria_line(syria_pdf, log)
    adl = LineString(verts)
    parts, tif_fits, _ = syria_dmz(syria_tif, log)
    near = lambda p: p.distance(adl) < 3.0 and p.area > 0.3   # the legend's swatch, in the lake far from the line, is not
    keep = [p for p in parts if near(p)]
    dropped = [round(p.area, 2) for p in parts if not near(p)]
    # parts within 1 km of each other are one part (the hatching is cut by the Yarmuk at El Hamma)
    groups = []
    for p in sorted(keep, key=lambda q: -q.centroid.y):
        for gp in groups:
            if any(p.distance(q) < 1.0 for q in gp):
                gp.append(p); break
        else:
            groups.append([p])
    names = {0: ("syria_dmz_north", "Demilitarized Zone (Syria-Israel), northern part: Banias and Tell el Qadi"),
             1: ("syria_dmz_central", "Demilitarized Zone (Syria-Israel), central part: Dardara, the Jordan and Mishmar HaYarden"),
             2: ("syria_dmz_south", "Demilitarized Zone (Syria-Israel), southern part: Ein Gev and El Hamma")}
    if len(groups) != 3:
        raise SystemExit(f"expected the Demilitarized Zone in three parts on the map, found {len(groups)}")
    diag["dmz_areas_km2"] = {}
    shore = {k: v["on_green"] for k, v in diag["traced"].items() if k in ("9-10", "10-11")}
    adl_text = ("the Annex I points exact; stretches the text calls 'a line' straight between them; the others traced on the "
                "annexed map's green line. Evidence, independent of the trace: the map's own 1-km grid fits each sheet at "
                f"{max(max(f['grid_rms_m']) for f in diag['fits'].values()):.0f} m rms before the points register the scan; where the "
                "line is the Demilitarized Zone's edge it agrees with the hatching of the other scan (separately georeferenced) "
                "to a median of 12-98 m per stretch. (The points' distance to the green ink after registration, median "
                f"{np.median(list(diag['annex_points_to_green_m'].values())):.0f} m, is small by construction and is not evidence.) "
                "About +-0.1 km (the MR precision) at the points and +-0.1-0.2 km along the traced stretches, except the two short "
                f"Lake Hula shoreline stretches, 9-10 and 10-11, only {shore['9-10']:.0%} and {shore['10-11']:.0%} on green ink "
                "(under 1.5 km each; error bounded by their length, about +-0.3 km). Lake Hula's shoreline is the 1949 one (the lake was "
                "drained in the 1950s).")
    stretches = [{"from": u, "to": v, "how": h, "text": w} for (u, v), (h, w) in SYRIA_STRETCHES.items()]
    feats.append(feature("syria_adl", "syria", "line", "Armistice Demarcation Line (Syria-Israel)", adl, adl_text,
                         "Annex I's 24 items (typed from UNTS vol. 42 pp. 342-344) converted from the Palestine Grid (EPSG:28191); "
                         "the stretches between points as below; Art. V.3: the line is as delineated on the map of Annex I.",
                         [UNTS, SYRIA_PDF], {"annex_points": [{"label": l, "mr": [e, n]} for l, e, n in SYRIA_POINTS], "stretches": stretches,
                                             "diagnostics": {"annex_points_to_green_m": diag["annex_points_to_green_m"], "traced": diag["traced"]}}))
    for i, gp in enumerate(groups):
        geom = shapely.union_all(gp)
        fid, name = names[i]
        diag["dmz_areas_km2"][fid] = round(geom.area, 2)
        feats.append(feature(fid, "syria", "zone", name, geom,
                             "about +-0.1-0.2 km: the edge of the hatching on the 1:50,000 map (hatch spacing about 0.17 km; the "
                             "zone's narrowest tips, under two hatch spacings wide, are under-drawn); georeference rms "
                             f"{max(max(f['rms_m']) for f in tif_fits.values()):.0f} m on the map's 1-km grid.",
                             "Art. V.5: where the line departs from the international boundary, the area between them is a "
                             "Demilitarized Zone, which also takes in the Ein Gev and Dardara sectors; its outline is the hatched "
                             "area of the annexed map ('Demilitarized zone' in the legend), vectorised from the hatching.",
                             [UNTS, SYRIA_TIF],
                             {"area_km2": round(geom.area, 2), "withdrawal": "Annex II: forces out of the zone within 12 weeks of signing (by 12 Oct 1949), in three sectors (3, 6 and 12 weeks)"}))
    total = round(sum(diag["dmz_areas_km2"].values()), 1)
    # cross-check of the two scans: where Art. V.5 makes the line the zone's edge, the line (colour scan) and the hatching's
    # edge (1-bit scan, its own georeference) should coincide; the median distance per stretch, metres
    edges = shapely.union_all([shapely.union_all(gp).boundary for gp in groups])
    pts = {l: (e, n) for l, e, n in SYRIA_POINTS}
    agree = {}
    for (u, v) in SYRIA_STRETCHES:
        seg = LineString([pts[u], pts[v]]) if SYRIA_STRETCHES[(u, v)][0] == "line" else None
        if seg is None:
            i0 = next(i for i, c in enumerate(verts) if abs(c[0] - pts[u][0]) < 1e-9 and abs(c[1] - pts[u][1]) < 1e-9)
            i1 = next(i for i, c in enumerate(verts) if i > i0 and abs(c[0] - pts[v][0]) < 1e-9 and abs(c[1] - pts[v][1]) < 1e-9)
            seg = LineString(verts[i0:i1 + 1])
        d = [edges.distance(seg.interpolate(f, normalized=True)) for f in np.linspace(0.05, 0.95, 19)]
        agree[f"{u}-{v}"] = round(float(np.median(d)) * 1000)
    diag["line_to_zone_edge_median_m"] = agree
    # Egypt
    eg = egypt(gaza, log)
    gaza_acc = ("about +-0.3 km: the Wadi Hasi mouth and Deir Suneid read off the 1:100,000 sheet (+-0.1 km; georeference rms "
                f"{max(eg['fit']['rms_m']):.0f} m), the highway traced on it ({eg['fit']['highway_on_red_road']:.0%} of the trace on its red "
                "fill) and offset 3 km; the turn at Deir Suneid ('in an easterly direction through Deir Suneid and across the highway') "
                "is read as the mouth-Deir Suneid direction carried on to the offset line. Cross-check: S/1471's demarcation line "
                "(1950) lies on it, by eye within about 0.1 km, between MR 103-100.2 and 107-102.8. The Art. VI.3 Deir Suneid outpost "
                "('on the north side of the Wadi', MR 1075-1090) lies 0.5 km from it.")
    feats.append(feature("egypt_adl_gaza", "egypt", "line", "Armistice Demarcation Line (Egypt-Israel) of 1949, Gaza-Rafah area: Wadi Hasi to Gaza",
                         LineString(eg["adl"]), gaza_acc,
                         "Art. VI.1 (para. 2.B(i) of the Memorandum of 13 November 1948): from the coast at the mouth of the Wadi "
                         "Hasi in an easterly direction through Deir Suneid and across the Gaza-Al Majdal Highway to a point 3 km "
                         "east of the highway, then southerly parallel to the highway. Built on the Survey of Palestine sheet "
                         "'Gaza' (georeferenced on its 10-km grid), as far as the highway runs (to Gaza).",
                         [EGYPT_TEXT, UNTS, MEMO, GAZA_SHEET, S1471, IBS46],
                         {"outposts_check": eg["outposts"], "gap": EGYPT_SOUTH_GAP}))
    feats.append(feature("egypt_el_auja_dmz", "egypt", "zone", "El Auja Demilitarized Zone (Egypt-Israel)", eg["auja"],
                         "+-0.1 km at the four Art. VIII.2 points; the closing stretch is the 1906 frontier from the Mandate data "
                         f"(about +-1 km; the two frontier points lie {eg['frontier_offsets_km']['El Auja P1']} and "
                         f"{eg['frontier_offsets_km']['El Auja P4']} km from it).",
                         "Art. VIII.2: from the frontier 5 km NW of the Rafah-El Auja road crossing (MR 0875-0468) to Khashm El "
                         "Mamdud (MR 0965-0414), Hill 405 (MR 1078-0285), the frontier 5 km SE of the old railway crossing "
                         "(MR 0995-0145), and back along the Egypt-Palestine frontier.",
                         [EGYPT_TEXT, UNTS, MANDATE_FRONTIER],
                         {"points": [{"label": l, "mr": [e, n]} for l, e, n in EL_AUJA], "area_km2": round(eg["auja"].area, 1)}))
    # Egypt, 1950: the modus vivendi (owner ruling 2026-10-11: its own dated feature; the 1949 stretch south of Gaza stays undrawn)
    from shapely.geometry import MultiLineString
    mv = egypt_1950(mv1, mv2, log)
    feats.append(feature("egypt_mv1950_demarcation", "egypt", "line",
                         "Demarcation line as drawn on the signed 1950 modus vivendi map (Gaza-Rafah area)",
                         MultiLineString(mv["pieces"]),
                         "about +-0.1 km: waypoints read on the drawn dash-dot of the signed 1:20,000 sheets every 0.5-1 km and joined "
                         f"straight (georeference rms {max(mv['rms_m']['sheet1'] + mv['rms_m']['sheet2']):.0f} m; the dash-dot is about 12 m "
                         f"wide; ink within 3 px along {', '.join(f'{x:.0%}' for x in mv['on_ink'])} of each piece); the three points "
                         "the text puts on the line are exact. " + MV_GAPS,
                         "The modus vivendi of 22 February 1950 (S/1471) sets local patrol zones A, B and C beside the demarcation line "
                         "and, by its Art. III, does not change either party's position on the Art. VI.1 line; its signed map draws that "
                         "line (dash-dot, 'Demarcation line'). Drawn here where the map draws it, through waypoints read off the "
                         "georeferenced sheets. A later drawing of the 1949 line, kept as its own feature and date.",
                         [MV_TEXT, MV_SHEET1, MV_SHEET2],
                         {"label_note": "1950 modus vivendi", "pieces_on_ink": mv["on_ink"], "gaps": MV_GAPS,
                          "text_points_on_line": [list(p) for p in sorted(MV_TEXT_POINTS)]}, agreement=MV_AGREEMENT))
    feats.append(feature("egypt_mv1950_zone_lines", "egypt", "line",
                         "Zone A and zone C lines of the 1950 modus vivendi (Gaza-Rafah area)",
                         MultiLineString([MV_ZONE_A, MV_ZONE_C]),
                         "+-0.1 km (the MR precision): straight lines between the text's points, as Art. I.1 describes them; on the "
                         "signed map they lie on the drawn zone lines to within its georeference (about 20 m).",
                         "Art. I.1(a): zone A between the demarcation line and the line MR 10170-11160, 10690-10740 (the railway "
                         "crossing), 10795-10640, 10900-10565, 10880-10480, 10810-10410, to the demarcation line at 10695-10240; "
                         "(c): zone C between the demarcation line and the line MR 08935-08590, 09035-07970, 08970-07790, to the "
                         "demarcation line at 08735-07670. Zone A was patrolled by Israel, zones B and C by Egypt (Art. II). Zone B "
                         "('between the line of (a) and the Egyptian fighting line north of Beit Lahaya') has no points in the text.",
                         [MV_TEXT, MV_SHEET1, MV_SHEET2],
                         {"label_note": "1950 modus vivendi", "zone_a": [list(p) for p in MV_ZONE_A], "zone_c": [list(p) for p in MV_ZONE_C]},
                         agreement=MV_AGREEMENT))
    # Jordan (owner ruling 2026-10-11): from the low-resolution UNISPAL copy of map 1, every feature marked low-accuracy
    jo = jordan(jordan_map, log)
    gn, gs = jo["georeference"]["north"], jo["georeference"]["south"]
    def jo_acc(sheet_note: str, on_ink: float) -> str:
        return ("LOW ACCURACY, about " + sheet_note + ": traced from a low-resolution copy of the signed map 1 (1:250,000; about "
                "51 m a pixel; the pen line is about 5 px, 250 m, wide), georeferenced on town symbols (north sheet leave-one-out "
                f"rms {gn['leave_one_out_rms_km']} km, max {gn['leave_one_out_max_km']}; south sheet {gs['leave_one_out_rms_km']} km, "
                f"max {gs['leave_one_out_max_km']}); {on_ink:.0%} of the trace on the pen ink. To be replaced from a full-resolution scan.")
    jq = {"source_quality": "low_res_scan",
          "source_quality_note": "Traced from the UNISPAL copy of map 1 (3119 x 8630 px); to be replaced from a full-resolution scan."}
    stage_dates = {"a_triangle": ("1949-05-08", "Art. VI.3(a), by 8 May 1949", "Baqa to Kafr Qasim"),
                   "b_wadi_ara": ("1949-05-22", "Art. VI.3(b), by 22 May 1949", "Wadi Ara, Zubeiba to Baqa"),
                   "c_east": ("1949-07-17", "Art. VI.3(c), by 17 July 1949", "Zubeiba to the Jordan"),
                   "c_south": ("1949-07-17", "Art. VI.3(c), by 17 July 1949", "Kafr Qasim to Kh Deir Arab")}
    for key, geom in jo["stages"].items():
        date, why, where = stage_dates[key]
        f = feature(f"jordan_adl_iraqi_{key}", "jordan", "line",
                    f"Armistice Demarcation Line (Jordan-Israel), line A, {where} ({why})",
                    geom, jo_acc("+-1-1.5 km", jo["traces"]["A_iraqi"]["on_ink"]),
                    "Art. VI.2: line A on map 1 (the sector held by Iraqi forces), traced from the map; dated by Art. VI.3's "
                    "stages, shown from each deadline (until then the existing military lines held; they are not drawn). " + JORDAN_STAGES,
                    [UNTS, JORDAN_MAP, JORDAN_TOWNS], {**jq, "stage": why, "sector_cuts_km": jo["cuts"]})
        f["properties"]["shown_from"] = date
        feats.append(f)
    for key, name, sheet_note, method in [
        ("a_latrun", "Armistice Demarcation Line (Jordan-Israel), Kh Deir Arab to Jerusalem (Latrun)", "+-1-1.5 km",
         "Art. V.1(a): from Kh Deir Arab (MR 1510-1574; line A cut at the trace point nearest it) to the northern end of the "
         "Jerusalem lines, the truce lines as certified by UNTSO, as drawn on map 1; at Latrun the trace follows the outer drawn loop (the two lines and the "
         "no-man's-land between them are not legible at this resolution); it stops at Jerusalem's western edge."),
        ("bc_south", "Armistice Demarcation Line (Jordan-Israel), Jerusalem to the Dead Sea (line B, Hebron-Dead Sea)", "+-1-2 km",
         "Art. V.1(b)-(c): from Jerusalem's southern edge west of Bethlehem, down past Beit Jibrin and round to the Dead Sea "
         "north of Ein Gedi and on to the Dead Sea point MR 1925-0958, where sector (d) begins (cut at the trace point nearest "
         "it), as drawn on map 1 (line B marked in the Hebron-Dead Sea sector); where the Jerusalem lines end and line B "
         "begins is not legible, so the stretch is one feature."),
        ("d_arava", "Armistice Demarcation Line (Jordan-Israel), the Dead Sea to the Gulf of Aqaba", "+-1.5-2.5 km, up to about 3 km at the Gulf of Aqaba",
         "Art. V.1(d): from the Dead Sea (MR 1925-0958) to the southernmost tip of Palestine, the positions surveyed by UN "
         "observers in March 1949, as drawn on map 1 (through the Dead Sea and down the Wadi Araba).")]:
        extra = dict(jq)
        if key == "d_arava":
            # independent check: south of the Dead Sea the 1949 line followed the Mandate frontier, which the sub-district
            # data has (about +-1 km); the trace's distance to it every 8 km of northing
            ring = LineString(max(mandate_land().geoms, key=lambda q: q.area).exterior.coords)
            arava = jo["sectors"][key]
            cs = list(arava.coords)
            ds = [round(ring.distance(Point(e, n)), 2) for e, n in cs[::max(1, len(cs) // 40)] if n < 66]
            extra["check_against_mandate_frontier_km"] = {"median": float(np.median(ds)), "max": float(max(ds)),
                                                          "note": "trace points south of N 66 (the Arava) to the Mandate frontier"}
        feats.append(feature(f"jordan_adl_{key}", "jordan", "line", name, jo["sectors"][key],
                             jo_acc(sheet_note, jo["traces"][key]["on_ink"]), method, [UNTS, JORDAN_MAP, JORDAN_TOWNS], extra))
    fronts = []
    for key, (agr, date, place, docsym) in AGREEMENTS.items():
        f = {"key": key, "agreement": agr, "signed": date, "document": docsym}
        if key == "jordan":
            f.update({"shown_from": date, "data": True, "source_quality": "low_res_scan",
                      "low_accuracy": "Traced from a low-resolution copy of the signed map 1 (about +-1-2.5 km); to be replaced from a full-resolution scan.",
                      "gap": JORDAN_GAPS, "searched": JORDAN_SEARCH,
                      "dating": "from 1949-04-03 for sectors (a), (b) (not drawn), (c), (d); line A in the Iraqi sector in stages "
                                "(Art. VI.3): 1949-05-08, 1949-05-22 and 1949-07-17; until each stage the earlier military line held (not drawn)."})
        else:
            f.update({"shown_from": date, "data": True})
        if key == "egypt":
            f["gap"] = (EGYPT_SOUTH_GAP + " From 22 Feb 1950 the line as drawn on the signed modus vivendi map (S/1471) is a "
                        "separate feature (egypt_mv1950_demarcation), with its zone A and C lines (egypt_mv1950_zone_lines). Art. VI.4: "
                        "in the Bethlehem-Hebron area no line was demarcated (left to a later agreement); nothing is drawn there.")
        fronts.append(f)
    meta = {
        "title": "The 1949 armistice lines that the primary sources allow (true lines, not hex-snapped)",
        "status": "draft", "checked": CHECKED, "generated_by": "tools/armistice_digitise.py",
        "licence": "CC-BY-SA-4.0 (content/); coast from the Copernicus DEM (NOTICE)",
        "licence_note": ("The agreements and their annexed maps are UN documents with symbols, not copyrighted (UN Administrative "
                         "Instruction ST/AI/189/Add.9/Rev.2, para. 2; Commons {{PD-UN-doc}}, {{PD-UN-map}}). The Gaza sheet and the "
                         "Mandate frontier are Government of Palestine works (decision D-B). Only the lines' positions, facts of "
                         "the agreements, are taken, redrawn by our own method; no image is copied."),
        "sources": [UNTS, SYRIA_PDF, SYRIA_TIF, EGYPT_TEXT, MEMO, GAZA_SHEET, MANDATE_FRONTIER, IBS46, MV_TEXT, MV_SHEET1, MV_SHEET2, JORDAN_MAP, JORDAN_TOWNS],
        "conversion": "Palestine Grid (EPSG:28191) to WGS84 by the EPSG Helmert transformation (2 m), pipeline in tools/armistice_digitise.py",
        "fronts": fronts,
        "partition": {"closed": False,
                      "why": "The lines do not yet enclose the areas each side held: the Egypt line stops at Gaza and the Jordan "
                             "line leaves out Jerusalem. Until they do, the Mandate sub-district layer stays as the base under them "
                             "(tools/borders_bake.py), each front's end recorded per front."},
        "naming": "The agreements' own terms ('Armistice Demarcation Line', 'Demilitarized Zone'); owner review.",
        "sensitivity": ("The owner's sensitivity review (eyalgolan, 2026-10-23, issue #196 question 1): on the "
                        "coordinator's review page of origin/main de78253e4c, decisions given in chat. Approved, "
                        "verbatim: 'egypt_el_auja_dmz | APPROVE'; 'lebanon_adl | APPROVE'; 'jordan_adl_bc_south | "
                        "APPROVE'; 'jordan_adl_d_arava | APPROVE'; 'jordan_adl_iraqi_a_triangle | APPROVE'; "
                        "'jordan_adl_iraqi_b_wadi_ara | APPROVE'; 'jordan_adl_iraqi_c_east | APPROVE'; "
                        "'jordan_adl_iraqi_c_south | APPROVE'; 'syria_dmz_central | APPROVE'; 'syria_dmz_north | "
                        "APPROVE'; 'syria_dmz_south | APPROVE'. Flagged, verbatim, to be checked and reviewed again: "
                        "'egypt_adl_gaza | FLAG | this is not where the international border is. the line is clear on the "
                        "map you drew on'; 'egypt_mv1950_demarcation | FLAG | this is not where the international border "
                        "is. the line is clear on the map you drew on'; 'egypt_mv1950_zone_lines | FLAG | this is not "
                        "where the international border is. the line is clear on the map you drew on'; "
                        "'jordan_adl_a_latrun | FLAG | check this again, the bottom part doesnt look right'; 'syria_adl | "
                        "FLAG | this is not where the international border is. the line is clear on the map you drew on'. "
                        "Owner ruling (2026-10-23, PR #1444): the owner's review also stands as the fact check of the 11 "
                        "approved features, so each is sensitivity-reviewed, signed reviewed_by eyalgolan, reviewed_on "
                        "2026-10-23, in the owner's own commit; the 5 flagged stay draft, and so does this file's status until "
                        "every feature is reviewed."),
        "diagnostics": {"syria": {"dmz_total_km2": total, "line_to_zone_edge_median_m": diag["line_to_zone_edge_median_m"], "dmz_areas_km2": diag["dmz_areas_km2"], "dropped_hatch_km2": dropped,
                                  "colour_registration": diag["fits"],
                                  "grid_georeference_1bit": {k: {"rms_m": [round(x, 1) for x in f["rms_m"]], "obs": f["obs"]} for k, f in tif_fits.items()}},
                        "egypt": {"gaza_sheet_rms_m": [round(x, 1) for x in eg["fit"]["rms_m"]],
                                  "highway_on_red_road": eg["fit"]["highway_on_red_road"], "highway_km": eg["highway_km"],
                                  "wadi_hasi_mouth_mr": [round(v, 2) for v in eg["mouth"]], "deir_suneid_mr": [round(v, 2) for v in eg["deir_suneid"]]}},
    }
    return {"type": "FeatureCollection", "statecraft": meta, "features": feats}


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--syria-pdf", required=True, type=Path)
    ap.add_argument("--syria-tif", required=True, type=Path)
    ap.add_argument("--gaza-sheet", required=True, type=Path)
    ap.add_argument("--s1471-sheet1", required=True, type=Path)
    ap.add_argument("--s1471-sheet2", required=True, type=Path)
    ap.add_argument("--jordan-map", required=True, type=Path)
    ap.add_argument("--out", type=Path, default=OUT)
    args = ap.parse_args()
    doc = build(args.syria_pdf, args.syria_tif, args.gaza_sheet, args.s1471_sheet1, args.s1471_sheet2, args.jordan_map)
    write(doc, args.out)
    for f in doc["features"]:
        p = f["properties"]
        print(f"{p['id']:24s} {p['kind']:5s} from {p['shown_from']}" + (f"  {p['area_km2']} km2" if "area_km2" in p else ""))
    print(json.dumps(doc["statecraft"]["diagnostics"], indent=1)[:3000])
    print(f"wrote {args.out}")


if __name__ == "__main__":
    main()
