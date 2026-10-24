# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "scipy==1.17.1", "pillow==12.3.0", "shapely==2.2.0", "rasterio==1.5.2", "pyproj==3.8.0"]
# ///
"""Digitises the Mandate sub-districts in force on 15 May 1948 from a scan of the Survey of Palestine's map
"Administrative Divisions" (1:750,000, drawn and printed April 1946, red boundary overprint of May 1947; sheet 6 of
"Maps of Palestine prepared for the information of the United Nations Special Committee", June 1947). Writes
content/map/subdistricts_1948.geojson: one polygon per sub-district, the true digitised lines (not hex-snapped).

Usage (the scans are downloads, kept outside the repository and checked by SHA-256):
  uv run tools/borders_digitise.py --scan <dir>/06_Palestine-_Administrative_divisions.jpg \
      --cross-check <dir>/Survey_of_Palestine,_Maps_of_Palestine_06-Adm-Divisions_(FL147392105_2367029).jpg

Method (docs/visual-v1.md, "Region borders"; numbers in the output's "statecraft" block):
1. Georeference: the eight graticule intersections (35 and 36 E x 30..33 N) are found on the scan (dark-ink profiles
   around each expected point) and an affine lon/lat -> pixel fit is made (the map's graticule is rectilinear; residuals
   are written out).
2. Boundary ink: the red overprint (international, district and sub-district boundaries) plus the black dots that run
   between the dashes of every boundary (small compact dark blobs within 40 px of red ink). The district names' bold red
   lettering and the sub-district names' red underlines are erased first (boxes below); a few breaks where a boundary
   runs under a black label, or stops a few pixels short of the coast, are joined by connector segments (PATCHES).
   These are the "uncertain segments" of the output.
3. The ink is closed (a disk of CLOSE_R px at half resolution bridges the dash gaps), water is taken from the game's own
   Copernicus DEM (sea at 0 m; the Dead Sea at its 1948 level of about -392 m), and the free land is labelled by
   connected components seeded at each sub-district's chief town. Every seed must land in its own component, apart
   from the land outside Palestine (OUTSIDE seeds): any leak fails the run.
4. Boundary ink and small unseeded pockets go to the nearest labelled region (the boundary is the ink's medial line);
   the label raster is vectorised, simplified as one coverage (shared edges stay shared) and written in lon/lat.
The cross-check digitises a second, independent print of the same map with the same rules and reports, per
sub-district, the area, the IoU and the mean and maximum boundary offset between the two.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "content" / "map" / "subdistricts_1948.geojson"
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
CHECKED = "2026-10-11"
# Sensitivity review done by the controller on the owner's behalf (owner-delegated, 2026-10-11).
STATUS = "sensitivity-reviewed"
NAMES_SOURCE = ("name_he / district_he: The Palestine Gazette (Hebrew edition) No. 1415, Supplement No. 2, 7 June 1945, "
                "the schedule's headings, pp. 493-499 (owner ruling, 2026-10-23, #196; research_notes/Map borders/"
                "subdistrict-names-and-amendments-1947-48.md section 2). name_ar / district_ar: Wikipedia (secondary), "
                "accepted for now (owner-delegated ruling, 2026-10-11); the Gazette's Arabic edition was not reachable")
SAMARIA_AR_NOTE = ("'لواء السامرة' renders the official English 'Samaria District' directly; 'لواء نابلس' is the common "
                   "usage. No official Arabic Mandate text (al-Waqa'i' al-Filastiniyya) was reachable online (searched "
                   "2026-10-11): unverified")
F = 2           # processing at half the scan's resolution (about 66 m a pixel)
CLOSE_R = 20    # closing radius, processing pixels
FINE_R = 6      # the second, fine closing that gives back the land the coarse one fills in narrow salients (Metulla)
DEAD_SEA_1948_M = -392.0
SIMPLIFY_DEG = 0.0012   # coverage simplification tolerance (about 120 m), well inside the stated accuracy
MIN_PART_KM2 = 0.5      # coastal slivers of the DEM shore below this are dropped

SCANS = {
    "93c4bded807015b10804d158f6f3f39d3a1a171ebb0f0680b6987a50a0bdfc18": {
        "label": "UNSCOP compendium copy, National Library of Israel (accession per Commons: 'Pal 1143; Pal 1143b; Pal 1196?'); Commons tag {{PD-UK-Gov}} in the Map template's permission field",
        "url": "https://commons.wikimedia.org/wiki/File:06_Palestine-_Administrative_divisions.jpg",
        # expected graticule intersections, scan pixels (lon, lat, x, y); found within about 40 px
        "graticule": [[35, 33, 3594, 1858], [35, 32, 3603, 5355], [35, 31, 3612, 8843], [35, 30, 3621, 12327],
                      [36, 33, 6519, 1849], [36, 32, 6528, 5353], [36, 31, 6536, 8841], [36, 30, 6545, 12329]]},
    "84f2bdac9d39cfc39063d04f2a7e440d2a4786f548ce539837cc919202912792": {
        "label": "Eran Laor Cartographic Collection copy, National Library of Israel (FL147392105, published 1947-06 per Commons); Commons tags {{PD-UKGov}}{{PD-IsraelGov}}",
        "url": "https://commons.wikimedia.org/wiki/File:Survey_of_Palestine,_Maps_of_Palestine_06-Adm-Divisions_(FL147392105_2367029).jpg",
        "graticule": [[35, 33, 3398, 1707], [35, 32, 3412, 5205], [35, 31, 3420, 8696], [35, 30, 3425, 12182],
                      [36, 33, 6343, 1691], [36, 32, 6348, 5198], [36, 31, 6354, 8694], [36, 30, 6359, 12186]]},
}

# Each sub-district: (id, Gazette name, district id, a seed point well inside it: its chief town, lon/lat).
SUBDISTRICTS = [
    ("acre", "Acre Sub-District", "galilee", (35.0817, 32.9278)),
    ("beisan", "Beisan Sub-District", "galilee", (35.4986, 32.4975)),
    ("nazareth", "Nazareth Sub-District", "galilee", (35.3033, 32.7019)),
    ("safad", "Safad Sub-District", "galilee", (35.4983, 32.9658)),
    ("tiberias", "Tiberias Sub-District", "galilee", (35.5333, 32.7944)),
    ("haifa", "Haifa Sub-District", "haifa", (35.0500, 32.7800)),
    ("jenin", "Jenin Sub-District", "samaria", (35.3008, 32.4611)),
    ("nablus", "Nablus Sub-District", "samaria", (35.2544, 32.2211)),
    ("tulkarm", "Tulkarm Sub-District", "samaria", (35.0286, 32.3104)),
    ("hebron", "Hebron Sub-District", "jerusalem", (35.0998, 31.5326)),
    ("jerusalem", "Jerusalem Sub-District", "jerusalem", (35.2256, 31.7789)),
    ("ramallah", "Ramallah Sub-District", "jerusalem", (35.2034, 31.9038)),
    ("jaffa", "Jaffa Sub-District", "lydda", (34.7800, 32.0800)),
    ("ramle", "Ramle Sub-District", "lydda", (34.8625, 31.9275)),
    ("gaza", "Gaza Sub-District", "gaza", (34.4597, 31.5075)),
    ("beersheba", "Beersheba Sub-District", "gaza", (34.7867, 31.2522)),
]
DISTRICTS = {"galilee": "Galilee District", "haifa": "Haifa District", "samaria": "Samaria District",
             "jerusalem": "Jerusalem District", "lydda": "Lydda District", "gaza": "Gaza District"}
# Points in Lebanon, Syria, Transjordan and Sinai: the land outside Palestine, which no seed may reach.
OUTSIDE = [(35.30, 33.30), (35.80, 33.20), (35.80, 32.60), (35.80, 31.60), (35.70, 30.40), (34.30, 30.40),
           (34.25, 29.70), (35.85, 29.60)]

# Hebrew names: the Palestine Gazette's 1945 schedule headings (NAMES_SOURCE); the Hebrew Wikipedia page stays as a
# secondary source. Arabic names: secondary (no Mandate-era Arabic list was reachable), owner review.
NAMES_HE_URL = "https://he.wikipedia.org/wiki/%D7%9E%D7%97%D7%95%D7%96%D7%95%D7%AA_%D7%94%D7%9E%D7%A0%D7%93%D7%98_%D7%94%D7%91%D7%A8%D7%99%D7%98%D7%99"
NAMES_AR_URL = "https://ar.wikipedia.org/wiki/%D8%A7%D9%84%D8%AA%D9%82%D8%B3%D9%8A%D9%85_%D8%A7%D9%84%D8%A5%D8%AF%D8%A7%D8%B1%D9%8A_%D9%84%D9%81%D9%84%D8%B3%D8%B7%D9%8A%D9%86_(1920-1948)"
NAMES_HE = {"acre": "נפת עכו", "beisan": "נפת בית־שאן", "nazareth": "נפת נצרת", "safad": "נפת צפת", "tiberias": "נפת טבריה",
            "haifa": "נפת חיפה", "jenin": "נפת ג'נין", "nablus": "נפת שכם", "tulkarm": "נפת טול כרם", "hebron": "נפת חברון",
            "jerusalem": "נפת ירושלים", "ramallah": "נפת ראם־אללה", "jaffa": "נפת יפו", "ramle": "נפת רמלה", "gaza": "נפת עזה",
            "beersheba": "נפת באר־שבע",
            "galilee": "מחוז הגליל", "haifa_d": "מחוז חיפה", "samaria": "מחוז שומרון", "jerusalem_d": "מחוז ירושלים",
            "lydda": "מחוז לוד", "gaza_d": "מחוז עזה"}
NAMES_AR = {"acre": "قضاء عكا", "beisan": "قضاء بيسان", "nazareth": "قضاء الناصرة", "safad": "قضاء صفد", "tiberias": "قضاء طبريا",
            "haifa": "قضاء حيفا", "jenin": "قضاء جنين", "nablus": "قضاء نابلس", "tulkarm": "قضاء طولكرم", "hebron": "قضاء الخليل",
            "jerusalem": "قضاء القدس", "ramallah": "قضاء رام الله", "jaffa": "قضاء يافا", "ramle": "قضاء الرملة", "gaza": "قضاء غزة",
            "beersheba": "قضاء بئر السبع",
            "galilee": "لواء الجليل", "haifa_d": "لواء حيفا", "samaria": "لواء السامرة", "jerusalem_d": "لواء القدس",
            "lydda": "لواء اللد", "gaza_d": "لواء غزة"}
# Land area (dunams) of each sub-district in Village Statistics 1945 (Government of Palestine, position at 1.4.45; the
# "Grand Total" of each sub-district's last page): the independent check of the digitised areas. Tiberias excludes the lake.
VS1945_DUNAMS = {"acre": 799663, "beisan": 367087, "nazareth": 497533, "safad": 696131, "tiberias": 440969,
                 "haifa": 1031755, "jenin": 835214, "nablus": 1591718, "tulkarm": 835360, "hebron": 2076185,
                 "jerusalem": 1570785, "ramallah": 686564, "jaffa": 335366, "ramle": 870192, "gaza": 1111501,
                 "beersheba": 12577000}
LAKE_TIBERIAS_KM2 = 166.0   # the lake lies inside the Tiberias sub-district but not in its Village Statistics land area

# Edits to the boundary ink, lon/lat (so they apply to any scan of the map). Every one is an uncertain segment.
PATCHES = {
    "draw": [  # connectors: [points], reason
        ([(35.10486, 33.09855), (35.08434, 33.09856)], "Ras en Naqura: the Lebanese frontier extended a few pixels to the DEM sea"),
        ([(35.63109, 32.98353), (35.62969, 32.97351), (35.62829, 32.96205)], "Rosh Pinna: the Syrian frontier runs under the 'Rosh Pinna' label"),
        ([(35.24187, 32.61147), (35.25038, 32.60000), (35.26061, 32.58997)], "Afula: the Galilee/Samaria district boundary runs under the 'Afula' label"),
        ([(34.67350, 31.56925), (34.66182, 31.55122)], "Bureir: the Gaza/Beersheba sub-district boundary runs under the 'Bureir' label"),
        ([(34.99361, 29.55667), (34.99290, 29.54808), (34.99148, 29.53089)], "Gulf of Aqaba: the Transjordan frontier extended a few pixels to the DEM sea"),
        ([(34.74761, 31.99228), (34.73566, 31.99745), (34.71857, 32.00176)], "Jaffa/Ramle boundary: its coastal end extended a few pixels to the DEM sea"),
    ],
    # Gazette-listed villages at a frontier tip that the 1:750,000 line clips (the map draws the village on the line): land
    # labelled outside within radius_km of the village goes to its sub-district. (sub-district id, village, lon, lat,
    # radius km, Gazette page, coordinate source)
    "villages": [
        ("safad", "Metulla", 35.5744, 33.2789, 0.8, "p. 127 (Safad Sub-District: 'Metulla (includes Deir Mamas, Hura and Kafr Kila)')",
         "https://en.wikipedia.org/wiki/Metula"),
    ],
    # red sub-district name underlines (the map's convention, not boundaries): lon0, lat0, lon1, lat1
    "erase": [[35.45522, 32.94759, 35.51709, 32.94066], [35.01132, 32.92102, 35.06806, 32.91468],
              [35.45535, 32.76454, 35.54150, 32.75731], [35.30604, 32.68962, 35.40621, 32.68237],
              [35.30704, 32.45415, 35.37028, 32.44694], [35.19308, 32.19472, 35.27033, 32.18721],
              [34.65646, 32.04134, 34.73987, 32.03411], [34.81744, 31.90514, 34.88547, 31.89821],
              [35.09576, 31.90004, 35.20208, 31.89222], [35.23446, 31.74611, 35.39070, 31.73824],
              [35.10932, 31.52249, 35.18999, 31.51498], [34.47120, 31.49724, 34.52965, 31.48974],
              [34.79831, 31.23429, 34.90668, 31.22589], [35.43982, 32.48956, 35.49963, 32.48149],
              [35.02378, 32.29369, 35.11608, 32.28588]],
    # district names in bold red capitals (GALILEE, HAIFA, SAMARIA, LYDDA, JERUSALEM, GAZA): bold strokes erased inside
    "labels": [[35.23761, 32.89935, 35.44952, 32.87053], [34.94630, 32.68046, 35.13769, 32.65022],
               [34.95879, 32.28801, 35.44766, 32.25753], [34.71838, 31.93874, 34.99185, 31.91130],
               [34.90216, 31.65214, 35.37393, 31.62167], [34.65455, 31.18399, 34.96218, 31.14937]],
}

SOURCES = [
    {"cite": "The Palestine Gazette No. 1552, Supplement No. 2, 30 January 1947, pp. 125-133: Administrative Divisions "
             "(Amendment) Proclamation, 1947 (in force 1 January 1947). Revokes and replaces the whole Schedule of the "
             "Administrative Divisions Proclamation, 1939: the six districts and sixteen sub-districts by their villages.",
     "url": "https://commons.wikimedia.org/wiki/File:PalestineDistricts1947.pdf", "checked": CHECKED,
     "licence": "Public domain (Government of Palestine, 1947; see the licence note)"},
    {"cite": "Survey of Palestine, 'Administrative Divisions', 1:750,000, drawn and printed April 1946, overprinted May "
             "1947; map 6 of 'Maps of Palestine prepared for the information of the United Nations Special Committee', "
             "June 1947. The digitised source (two prints).",
     "url": "https://commons.wikimedia.org/wiki/File:06_Palestine-_Administrative_divisions.jpg", "checked": CHECKED,
     "licence": "Public domain (Government of Palestine, 1946/47; see the licence note)"},
    {"cite": "Village Statistics 1945 (Government of Palestine, April 1945): sub-district land areas, the area check.",
     "url": "https://commons.wikimedia.org/wiki/File:Village_Statistics_1945_full.pdf", "checked": CHECKED,
     "licence": "Public domain (Government of Palestine, 1945; see the licence note)"},
    {"cite": "Earlier changes, superseded by the 1947 Schedule: Supplement No. 2 to The Palestine Gazette No. 1163, 15 "
             "January 1942, pp. 119-132 (Jerusalem District reduced to three sub-districts: Bethlehem and Jericho merged "
             "into Jerusalem) and No. 1415, 7 June 1945, pp. 621-629 (minor adjustments; the edition Village Statistics "
             "1945 follows). Not read directly: cited via https://en.wikipedia.org/wiki/Districts_of_Mandatory_Palestine",
     "url": "https://en.wikipedia.org/wiki/Districts_of_Mandatory_Palestine", "checked": CHECKED, "licence": "n/a (citation only)"},
]
LICENCE_NOTE = (
    "Government of Palestine works of 1945-47. Copyright: the Copyright Act 1911, extended to Palestine in 1924, gave "
    "government works 50 years from publication, so these ran out by the end of 1997 in Palestine's successor law (Israel "
    "kept the 1911 Act until its 2007 Copyright Act took effect in 2008) and in the UK (Crown copyright, CDPA 1988 s.163 and its "
    "transition). Commons tags scan (a) {{PD-UK-Gov}} and scan (b) {{PD-UKGov}}{{PD-IsraelGov}}. Caveat: like the 1948-60 Government Press Office photos (docs/oss/DECISIONS.md "
    "D-P) they were still protected on 1 January 1996, so the URAA may have restored a US copyright in the map's drawing "
    "until about 2041-42 (95 years from 1946-47). What this file takes is factual: where legally defined administrative lines run (the Proclamation is "
    "a legislative text), redrawn by our own method; no image is copied, and US copyright does not protect facts (Feist), so "
    "a possible URAA copyright in the drawing does not reach this data. Decision D-B (owner-delegated, 2026-10-11): allowed; "
    "facts redrawn, no image copied. This "
    "file, like content/, is CC BY-SA 4.0; its coast follows the Copernicus DEM (NOTICE).")


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def fit_affine(points: list) -> tuple[np.ndarray, np.ndarray, float]:
    """Least-squares lon/lat -> pixel affine through graticule points [(lon, lat, x, y)]: (cx, cy, rms px), where
    x = cx[0] + cx[1] lon + cx[2] lat (and y alike)."""
    p = np.asarray(points, dtype=float)
    a = np.c_[np.ones(len(p)), p[:, 0], p[:, 1]]
    cx = np.linalg.lstsq(a, p[:, 2], rcond=None)[0]
    cy = np.linalg.lstsq(a, p[:, 3], rcond=None)[0]
    res = np.hypot(a @ cx - p[:, 2], a @ cy - p[:, 3])
    return cx, cy, float(np.sqrt(np.mean(res ** 2)))


def to_px(cx: np.ndarray, cy: np.ndarray, lon, lat):
    return cx[0] + cx[1] * np.asarray(lon) + cx[2] * np.asarray(lat), cy[0] + cy[1] * np.asarray(lon) + cy[2] * np.asarray(lat)


def to_lonlat(cx: np.ndarray, cy: np.ndarray, x, y):
    m = np.array([[cx[1], cx[2]], [cy[1], cy[2]]])
    d = np.stack([np.asarray(x, float) - cx[0], np.asarray(y, float) - cy[0]])
    lon, lat = np.tensordot(np.linalg.inv(m), d, 1)
    return lon, lat


def find_graticule(img, guesses: list) -> list:
    """Each expected intersection refined on the scan: the darkest column and row of a 240-px window (the crossing's own
    +-25 px left out, so the other line does not count), centroid of the peak."""
    out = []
    for lon, lat, gx, gy in guesses:
        r = 120
        a = np.asarray(img.crop((gx - r, gy - r, gx + r, gy + r))).astype(np.int16)
        dark = a.max(axis=2) < 130
        keep = np.r_[0:r - 25, r + 25:2 * r]
        def peak(p):
            k = int(np.argmax(p)); lo, hi = max(k - 4, 0), min(k + 5, len(p)); w = p[lo:hi]
            return lo + float((w * np.arange(len(w))).sum() / w.sum())
        out.append((lon, lat, gx - r + peak(dark[keep].sum(axis=0).astype(float)), gy - r + peak(dark[:, keep].sum(axis=1).astype(float))))
    return out


def label_scan(scan: Path, log=print) -> dict:
    """Steps 1-4 on one scan: the georeference and the sub-district label raster (processing pixels)."""
    from PIL import Image, ImageDraw
    from scipy import ndimage as ndi
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import heightmap_codec
    Image.MAX_IMAGE_PIXELS = None
    digest = sha256(scan)
    if digest not in SCANS:
        raise SystemExit(f"{scan}: unknown scan (sha256 {digest}); expected one of the two Commons files in SCANS")
    info = SCANS[digest]
    img = Image.open(scan).convert("RGB")
    grat = find_graticule(img, info["graticule"])
    cx, cy, rms = fit_affine(grat)
    log(f"{info['label']}: graticule affine rms {rms:.2f} px")
    a = np.asarray(img).astype(np.int16)
    w, h = img.size
    red = (a[..., 0] > 140) & (a[..., 0] - a[..., 1] > 70) & (a[..., 0] - a[..., 2] > 60)
    dark = (a[..., 0] < 125) & (a[..., 1] < 125) & (a[..., 2] < 125)
    del a

    def box(b):
        (xa, ya), (xb, yb) = [tuple(int(round(float(v))) for v in to_px(cx, cy, lo, la)) for lo, la in ((b[0], b[1]), (b[2], b[3]))]
        return min(xa, xb), min(ya, yb), max(xa, xb), max(ya, yb)
    for b in PATCHES["erase"]:
        x0, y0, x1, y1 = box(b); red[y0:y1, x0:x1] = False
    yy, xx = np.mgrid[-8:9, -8:9]
    for b in PATCHES["labels"]:
        x0, y0, x1, y1 = box(b)
        sub = red[y0:y1, x0:x1]
        sub &= ~ndi.binary_dilation(ndi.binary_erosion(sub, structure=xx ** 2 + yy ** 2 <= 64), iterations=14)
    dl, nd = ndi.label(dark)
    area = ndi.sum_labels(dark, dl, index=np.arange(1, nd + 1))
    keep = np.zeros(nd + 1, bool)
    for i, sl in enumerate(ndi.find_objects(dl), start=1):
        hh, ww = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        keep[i] = 6 <= area[i - 1] <= 160 and max(hh, ww) <= 18 and area[i - 1] >= 0.35 * hh * ww
    dots = keep[dl]
    del dl, dark
    near = ndi.binary_dilation(red[::4, ::4], iterations=10)
    dots &= np.repeat(np.repeat(near, 4, axis=0), 4, axis=1)[:h, :w]
    ink = red | dots
    h2, w2 = h // F, w // F
    ink2 = ink[:h2 * F, :w2 * F].reshape(h2, F, w2, F).any(axis=(1, 3))
    pim = Image.fromarray(ink2.astype(np.uint8) * 255); d = ImageDraw.Draw(pim)
    for seg, _ in PATCHES["draw"]:
        d.line([tuple(float(v) / F for v in to_px(cx, cy, lo, la)) for lo, la in seg], fill=255, width=3)
    ink2 = np.asarray(pim) > 0
    r = CLOSE_R
    yy, xx = np.mgrid[-r:r + 1, -r:r + 1]
    closed = ndi.binary_closing(np.pad(ink2, r), structure=xx ** 2 + yy ** 2 <= r * r)[r:-r, r:-r] | ink2
    # water from the game's DEM: the open sea (0 m) and the Dead Sea at its 1948 level
    meta = json.loads((MAP_DIR / "heightmap.json").read_text())
    raw = heightmap_codec.read(MAP_DIR, meta)
    elev = meta["elev_min_m"] + raw.astype("float64") / 65535 * (meta["elev_max_m"] - meta["elev_min_m"])
    ys, xs = np.mgrid[0:h2, 0:w2]
    lon, lat = to_lonlat(cx, cy, (xs + 0.5) * F, (ys + 0.5) * F)
    col = np.round((lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * (meta["width"] - 1)).astype(int)
    row = np.round((meta["lat1"] - lat) / (meta["lat1"] - meta["lat0"]) * (meta["height"] - 1)).astype(int)
    inside = (col >= 0) & (col < meta["width"]) & (row >= 0) & (row < meta["height"])
    e = np.where(inside, elev[np.clip(row, 0, meta["height"] - 1), np.clip(col, 0, meta["width"] - 1)], 0.0)
    sea = ~inside | (np.abs(e) < 0.5)
    sl, sn = ndi.label(sea); ss = ndi.sum_labels(sea, sl, index=np.arange(sn + 1)); ss[0] = 0
    sea = ss[sl] > 20000
    dead = ndi.binary_fill_holes(ndi.binary_closing(inside & (e <= DEAD_SEA_1948_M) & (lat < 31.9), iterations=4))
    dl2, dn = ndi.label(dead); ds = ndi.sum_labels(dead, dl2, index=np.arange(dn + 1)); ds[0] = 0
    water = sea | (ds[dl2] == ds.max())
    free = ~closed & ~ndi.binary_dilation(water, iterations=3)
    lab, _ = ndi.label(free)

    def comp(lo, la):
        x, y = to_px(cx, cy, lo, la); rr, cc = int(y / F), int(x / F)
        if lab[rr, cc]:
            return int(lab[rr, cc])
        win = lab[rr - 15:rr + 16, cc - 15:cc + 16]; ri, ci = np.nonzero(win)
        if len(ri) == 0:
            return 0
        k = np.argmin((ri - 15) ** 2 + (ci - 15) ** 2)
        return int(win[ri[k], ci[k]])
    seeds = {sid: comp(*pt) for sid, _, _, pt in SUBDISTRICTS}
    outside = {comp(*pt) for pt in OUTSIDE}
    bad = [k for k, c in seeds.items() if c == 0 or c in outside or list(seeds.values()).count(c) > 1]
    if bad:
        raise SystemExit(f"{scan.name}: boundary leak or unplaced seed for {bad}; the ink does not close these sub-districts")
    lut = np.zeros(lab.max() + 1, np.int32)
    for i, (sid, *_rest) in enumerate(SUBDISTRICTS, start=1):
        lut[seeds[sid]] = i
    for c in outside:
        lut[c] = 99
    labels = lut[lab]
    # Fine pass: the coarse closing also fills narrow salients of a solid line (the Metulla finger of the frontier) and the
    # nearest-label step then gives them to whichever side is nearer. With a fine closing those salients stay open and
    # join their own region; a fine component whose coarse-labelled pixels all carry one label passes it to its filled
    # pixels. A component with two labels (a dash gap the fine closing leaves open) changes nothing, so this cannot leak.
    yf, xf = np.mgrid[-FINE_R:FINE_R + 1, -FINE_R:FINE_R + 1]
    fine = ndi.binary_closing(np.pad(ink2, FINE_R), structure=xf ** 2 + yf ** 2 <= FINE_R * FINE_R)[FINE_R:-FINE_R, FINE_R:-FINE_R] | ink2
    fl, fn = ndi.label(~fine & ~ndi.binary_dilation(water, iterations=3))
    lo_lab = np.zeros(fn + 1, np.int64); hi_lab = np.zeros(fn + 1, np.int64)
    has = (fl > 0) & (labels > 0)
    np.maximum.at(hi_lab, fl[has], labels[has]); lo_lab[:] = 1 << 30; np.minimum.at(lo_lab, fl[has], labels[has])
    one = np.where((hi_lab == lo_lab) & (hi_lab > 0), hi_lab, 0)
    give = (labels == 0) & (fl > 0) & (one[fl] > 0)
    labels[give] = one[fl[give]]
    log(f"fine pass: {int(give.sum())} pixels given back")
    _, (ir, ic) = ndi.distance_transform_edt(labels == 0, return_indices=True)
    labels = labels[ir, ic]
    ys2, xs2 = np.mgrid[0:h2, 0:w2]
    for sid, name, vlon, vlat, rad, _page, _src in PATCHES["villages"]:
        vx, vy = to_px(cx, cy, vlon, vlat)
        near_v = (np.hypot(xs2 - vx / F, ys2 - vy / F) * F * 0.033 <= rad) & (labels == 99)
        labels[near_v] = 1 + [s_[0] for s_ in SUBDISTRICTS].index(sid)
        log(f"{name}: {int(near_v.sum())} pixels moved from outside to {sid}")
    labels[water] = 0
    return {"labels": labels, "cx": cx, "cy": cy, "rms": rms, "graticule": grat, "info": info, "sha256": digest,
            "ink": ink2, "closed": closed, "water": water}


def polygons(res: dict) -> dict:
    """Label raster -> {id: shapely geometry} in lon/lat, simplified as one coverage, slivers dropped."""
    import shapely
    from rasterio import features
    from rasterio.transform import Affine
    from shapely.geometry import MultiPolygon, shape
    cx, cy = res["cx"], res["cy"]
    mi = np.linalg.inv(np.array([[cx[1], cx[2]], [cy[1], cy[2]]]))
    t = Affine(mi[0, 0] * F, mi[0, 1] * F, -(mi[0, 0] * cx[0] + mi[0, 1] * cy[0]),
               mi[1, 0] * F, mi[1, 1] * F, -(mi[1, 0] * cx[0] + mi[1, 1] * cy[0]))
    lab = res["labels"]
    parts = {i: [] for i in range(1, len(SUBDISTRICTS) + 1)}
    for geom, v in features.shapes(lab.astype(np.int32), mask=(lab > 0) & (lab < 99), transform=t, connectivity=4):
        parts[int(v)].append(shape(geom))
    ids = [s[0] for s in SUBDISTRICTS]
    merged = [shapely.union_all(parts[i]) for i in range(1, len(ids) + 1)]
    simple = shapely.coverage_simplify(np.array(merged, dtype=object), SIMPLIFY_DEG)
    out = {}
    for sid, g in zip(ids, simple):
        keep = [p for p in (g.geoms if isinstance(g, MultiPolygon) else [g]) if km2(p) >= MIN_PART_KM2]
        out[sid] = shapely.orient_polygons(shapely.set_precision(MultiPolygon(keep) if len(keep) > 1 else keep[0], 1e-5))   # RFC 7946 winding
    return out


def km2(geom) -> float:
    from pyproj import Geod
    return abs(Geod(ellps="WGS84").geometry_area_perimeter(geom)[0]) / 1e6


def compare(a: dict, b: dict) -> dict:
    """Per sub-district: IoU and the mean (symmetric difference / perimeter) and maximum (Hausdorff) boundary offset, km."""
    import shapely
    from pyproj import Transformer
    t = Transformer.from_crs("EPSG:4326", "EPSG:32636", always_xy=True)
    utm = lambda g: shapely.transform(g, lambda c: np.c_[t.transform(c[:, 0], c[:, 1])])
    out = {}
    for k in a:
        ga, gb = utm(a[k]), utm(b[k])
        out[k] = {"iou": round(ga.intersection(gb).area / ga.union(gb).area, 3),
                  "mean_offset_km": round(ga.symmetric_difference(gb).area / ((ga.length + gb.length) / 2) / 1e3, 3),
                  "max_offset_km": round(ga.hausdorff_distance(gb) / 1e3, 2)}
    return out


def accuracy(main: dict, feats: list, cross: dict | None) -> str:
    dev = [100 * (f["properties"]["area_km2"] / f["properties"]["vs1945_area_km2"] - 1) for f in feats]
    text = (f"about +-1 km. Source map 1:750,000 (a 0.3 mm line is 225 m); georeference rms {main['rms'] * 0.033:.2f} km; "
            f"areas {min(dev):+.1f}% to {max(dev):+.1f}% against Village Statistics 1945")
    if cross:
        means = [v["mean_offset_km"] for k, v in cross.items() if not k.startswith("_")]
        maxes = [v["max_offset_km"] for k, v in cross.items() if not k.startswith("_")]
        text += f"; a second print digitised alike agrees to a mean {min(means):.2f}-{max(means):.2f} km, at most {max(maxes):.2f} km"
    return text + (". Caveats: the Village Statistics areas follow the 1945 Schedule (Gazette No. 1415), not the 1947 one, so 1947 "
                   "adjustments count as error; Beersheba's figure (12,577,000 dunams) is a rounded estimate, so its check is nominal; "
                   "Gaza low and Beersheba high point at the Gaza/Beersheba line (the Bureir connector). Uncertain segments (under "
                   "labels, connectors, the Metulla tip) to about +-1.5 km.")


def geojson(main: dict, geoms: dict, cross: dict | None) -> dict:
    from shapely.geometry import mapping
    feats = []
    for sid, name, district, seed in SUBDISTRICTS:
        g = geoms[sid]
        area = km2(g)
        vs = VS1945_DUNAMS[sid] / 1000 + (LAKE_TIBERIAS_KM2 if sid == "tiberias" else 0.0)
        dkey = district + "_d" if district in ("haifa", "jerusalem", "gaza") else district
        props = {"id": sid, "name": name, "name_he": NAMES_HE[sid], "name_ar": NAMES_AR[sid],
                 "district": DISTRICTS[district], "district_he": NAMES_HE[dkey], "district_ar": NAMES_AR[dkey],
                 "in_force_from": "1947-01-01", "area_km2": round(area, 1), "vs1945_area_km2": round(vs, 1),
                 "seed": list(seed), "sources": [s["url"] for s in SOURCES[:3]] + [NAMES_HE_URL, NAMES_AR_URL],
                 "names_source": NAMES_SOURCE, "checked": CHECKED, "status": STATUS}
        if district == "samaria":
            props["district_ar_note"] = SAMARIA_AR_NOTE
        if cross:
            props["cross_check"] = cross[sid]
        feats.append({"type": "Feature", "properties": props, "geometry": mapping(g)})
    meta = {
        "title": "Sub-districts of Mandatory Palestine in force on 15 May 1948 (true lines, not hex-snapped)",
        "status": STATUS, "checked": CHECKED, "generated_by": "tools/borders_digitise.py",
        "review": ("Sensitivity review by the controller on the owner's behalf (owner-delegated, 2026-10-11): accepted, with the "
                   "Mandate frontier outlined and the near-frontier shore hexes (Taba; the shore hex west of Rafah's frontier; "
                   "the Lisan) as baked. Display rule (controller ruling, owner-delegated, 2026-10-11): the hex that holds a "
                   "Gazette-listed place always takes that place's sub-district, never outside (tools/borders_bake.py rule 5; "
                   "it puts Rafah town's hex (-15, 31) in Gaza and Metulla's hex (15, 1) in Safad, which the centre rule "
                   "had left outside). These true lines are not changed by it."),
        "provenance": ("Hebrew name_he follows the Palestine Gazette headings in research_notes/Map borders/"
                       "subdistrict-names-and-amendments-1947-48.md section 2; the 1 January 1947 to 28 April 1948 "
                       "amendment check is closed (owner ruling, 23 October 2026, #196 comment 6359825082). Remaining "
                       "gaps from the note's section 3.3: 29 April to 14 May 1948 was not checked; the 1948 index is a "
                       "typescript of unknown origin and was only partly read; the 1947 Hebrew schedule was not found; "
                       "the Schedule's village lists were not re-checked."),
        "licence": "CC-BY-SA-4.0 (content/); coast from the Copernicus DEM (NOTICE)", "licence_note": LICENCE_NOTE,
        "sources": SOURCES,
        "scan": {"label": main["info"]["label"], "url": main["info"]["url"], "sha256": main["sha256"],
                 "graticule": [[lo, la, round(x, 1), round(y, 1)] for lo, la, x, y in main["graticule"]],
                 "graticule_rms_px": round(main["rms"], 2), "px_m_approx": 33},
        "method": "tools/borders_digitise.py docstring: red overprint + boundary dots, closed, flood-labelled from each "
                  "sub-district's chief town, medial line, coverage-simplified at about 120 m.",
        "accuracy": accuracy(main, feats, cross),
        "coast": "Seaward edges follow the game's Copernicus DEM coastline (sea at 0 m) and the Dead Sea at its 1948 level "
                 "(-392 m): coast, not a boundary. Lake Tiberias is inside the Tiberias sub-district.",
        "uncertain_segments": [{"points": [list(p) for p in seg], "why": why} for seg, why in PATCHES["draw"]]
                              + [{"point": [vlon, vlat], "radius_km": rad, "why": f"{name}: listed under {sid} in the 1947 Schedule ({page}) but drawn on the frontier's tip at 1:750,000; land within {rad} km kept in {sid} (coordinates: {src})"}
                                 for sid, name, vlon, vlat, rad, page, src in PATCHES["villages"]]
                              + [{"why": f"narrow salients of solid lines (the frontier): the coarse closing fills them; a fine closing ({FINE_R} px) gives each back to the region on its side where that side is unambiguous"}]
                              + [{"box": b, "why": "boundary under a district name's red lettering, joined by the closing"} for b in PATCHES["labels"]]
                              + [{"box": b, "why": "boundary crossing a sub-district name's red underline, joined by the closing"} for b in PATCHES["erase"]],
        "open_questions": ["Arabic names come from Wikipedia (secondary), accepted for now (owner-delegated ruling, "
                           "2026-10-11); the Gazette's Arabic edition was not reachable. Samaria "
                           "District in Arabic stays 'لواء السامرة' (see district_ar_note), unverified.",
                           "Known near-line place: Ra'anana (Jaffa Sub-District in the 1947 Schedule, p. 132) falls 0.1 km "
                           "inside Tulkarm on these lines, within the stated accuracy."],
    }
    if cross:
        meta["cross_check"] = {"scan": cross["_scan"], "sha256": cross["_sha256"]}
    meta.update(json.loads((ROOT / "content" / "map" / "region_names_he.json").read_text(encoding="utf-8"))["subdistricts"]["metadata"])
    return {"type": "FeatureCollection", "statecraft": meta, "features": feats}


def write(doc: dict, path: Path) -> None:
    """Deterministic, diff-friendly text: the metadata indented, then per feature one line of properties and one line per
    vertex (5-decimal coordinates), so a moved stretch of line shows as changed vertices in a diff."""
    def ring(r) -> str:   # one vertex per line
        return "[" + ",\n    ".join(json.dumps(v, separators=(",", ":")) for v in r) + "]"
    def ring_lines(geom: dict) -> str:
        if geom["type"] == "Polygon":
            inner = ",\n   ".join(ring(r) for r in geom["coordinates"])
        else:
            inner = ",\n   ".join("[" + ",\n    ".join(ring(r) for r in poly) + "]" for poly in geom["coordinates"])
        return '{"type":"' + geom["type"] + '","coordinates":[\n   ' + inner + "]}"
    meta = json.dumps({k: v for k, v in doc.items() if k != "features"}, ensure_ascii=False, indent=1)
    feats = ",\n".join('{"type":"Feature",\n  "properties":' + json.dumps(f["properties"], ensure_ascii=False, separators=(",", ":"))
                       + ',\n  "geometry":' + ring_lines(f["geometry"]) + "}" for f in doc["features"])
    text = meta[:-2] + ',\n "features":[\n' + feats + "\n]}\n"
    assert json.loads(text) == json.loads(json.dumps(doc)), "write() must round-trip"
    path.write_text(text, encoding="utf-8")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--scan", required=True, type=Path)
    ap.add_argument("--cross-check", type=Path)
    ap.add_argument("--out", type=Path, default=OUT)
    args = ap.parse_args()
    main_res = label_scan(args.scan)
    geoms = polygons(main_res)
    cross = None
    if args.cross_check:
        other = label_scan(args.cross_check)
        cross = compare(geoms, polygons(other))
        cross["_scan"] = other["info"]["label"]; cross["_sha256"] = other["sha256"]
    doc = geojson(main_res, geoms, cross)
    write(doc, args.out)
    for f in doc["features"]:
        p = f["properties"]
        print(f"{p['id']:10s} {p['area_km2']:8.1f} km2 (Village Statistics {p['vs1945_area_km2']:8.1f})"
              + (f"  cross-check iou {p['cross_check']['iou']} mean {p['cross_check']['mean_offset_km']} km max {p['cross_check']['max_offset_km']} km" if cross else ""))
    print(f"wrote {args.out.relative_to(ROOT) if args.out.is_relative_to(ROOT) else args.out}")


if __name__ == "__main__":
    main()
