# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "scipy==1.17.1", "pillow==12.3.0", "shapely==2.2.0", "pyproj==3.8.0",
#                 "scikit-image==0.26.0", "pypdf==5.4.0"]
# ///
"""Digitises who held which areas of Mandatory Palestine in 1948-49, by date, as true polygons, into
content/map/control_1948_49.geojson (status draft; owner-verify). Owner rulings of 2026-10-14: the map colours areas by
side like promo 03 (blue the Israeli side, orange-red the other side, grey contested, no-man's land or not held), from
sourced data only; the colours claim military control on a date, nothing else; the 1948 localities stay as of the game's
start. Research note: research_notes/Map borders/control-areas-1948-49.md.

Usage (the scans are downloads kept outside the repository and checked by SHA-256; ISA_MAP and CIA_MAP have the URLs):
  uv run tools/control_digitise.py --isa-map <dir>/R0005czg.pdf --cia-map <dir>/CIA-RDP78-01617A004700010003-6.pdf

Source: Israel State Archives, Prime Minister's Office, map 6158/1 ("Palestine: the partition lines and the front lines",
1949, a Survey of Palestine 1:250,000 sheet with the lines drawn by hand), the best public copy found (ISA_MAP: the NYU
Faculty Digital Archive's PDF, 2368 x 4078 px, about 64 m a pixel). Checked for higher-resolution public copies on
2026-10-14 (ISA_SEARCH): none reachable. Its legend (read on the scan):
  red   the front line at the end of the first truce, 9 July 1948
  blue  the front line at the start of the second truce, 18 July 1948
  green the front line at the opening of the road to the Negev and the taking of the Galilee (no date on the map; by its
        wording and by what it shows, end of October 1948: after Operations Yoav and Hiram)
  teal  the 29 November 1947 partition boundaries; yellow the withdrawal line the Acting Mediator demanded (November
        1948): neither is a control line, and neither is traced.
Method:
  1 The scan is georeferenced on its own printed 10-km Palestine grid (a cubic fit of E and N to the pixel, from every grid
    line crossing found in 120-px bands; the inset and the legend left out), then converted to WGS84 with the EPSG
    Helmert pipeline (tools/armistice_digitise.py, PG_PIPELINE).
  2 Each drawn line is traced between waypoints read off the scan (STROKES), routed along its own colour (red, blue, the
    green's light turquoise; the partition line's dark teal is excluded by colour, and where the two meet the waypoints
    follow the legend's colours and Resolution 181's own wording of the partition line, whose named points the map
    labels in teal: Kh. Khuweilifa, Wadi Sab', 'Ein Geddi).
  3 A later line is drawn only where the front had moved: where it is not drawn, the earlier line holds (the map's
    convention, read from where the three lines run together). Each snapshot is therefore the earlier lines with the
    stretches a later line replaces taken out (DROPS, each named by the stroke and a point on it), plus the later lines;
    enclaves the sources date as taken (Tira, 16 July; Ijzim, Jaba and Ein Ghazal, Operation Shoter, 24-26 July; the
    Galilee pocket, Operation Hiram) leave the snapshot from that date.
  4 The snapshot's lines and the Mandate outline (the 1948 sub-districts' union, D-B) are polygonised; every face takes a
    side by parity from Tel Aviv: crossing a front line changes side. A face reached by two parities fails the run
    (slivers under 1 km2, where two drawn lines run side by side, take their largest neighbour's side). Ends of lines
    within 3 km of another line or of the outline are joined to it (the map's pen lines stop short of the coast).
  5 The scan stops at the 60-km northing (Beersheba's latitude): south of it no 1948 line is sourced, and nothing is
    coloured there until the Jordan agreement's line d (Dead Sea to Aqaba) closes the south on 3 April 1949.
  6 Closure check (research note section 3): every face has a side, and every anchor place (ANCHORS: towns and
    settlements whose holder on the date is sourced) lands on its side, unless it lies within 2 km of a line (the
    accuracy). A failed anchor fails the run.
  7 Contested (grey) only for the days an operation ran there (owner ruling 2026-10-14): where two snapshots differ,
    during the Ten Days (8-18 July 1948; Lydda and Ramle, Nazareth, the Jenin and Latrun fronts, Tira), Operation
    Shoter (24-26 July), Operations Yoav and Ha-Har and the actions that followed them to the end of October in the south
    (15-31 October), Operation Hiram in the north (29-31 October). Operation Horev is not drawn (no source gives a line
    for January 1949, and its fighting lay south of the scan or beyond the frontier): the October line stands until the
    Egypt agreement. Towns that the sources place on one side but the lines, within their accuracy, put on the other are
    pinned (PINNED): the bake gives the town's hex the sourced side.
  8 From each armistice agreement's date its front takes the agreement's lines (content/map/armistice_lines_1949.geojson,
    D-A): the 1948 lines of that front leave as the agreement's lines come in (each 1948 piece leaves on the date of the
    nearest line of its front, so line A in the Iraqi sector comes in by Art. VI.3's stages, the earlier lines held until
    then), the demilitarized zones are neutral, and the gaps D-A records are closed as listed in CLOSURES.
  9 15 May - 10 June 1948 (owner decision of 16 October 2026 on #674, option (b), "9 July lines, adjusted"; research
    note research_notes/Map borders/control-15-may-1948.md section 9): no line for 15 May can be traced honestly (the 18
    May CIA scan's strongly-held border cannot be told from the partition line, roads and rail), so the opening period
    shows the 9 July snapshot's polygons, the other side not named, with: grey where the CIA "Military Situation - 18 May
    1948" map's dashed (tenuously held) border is legible and closes on a sourced line (CIA_STROKES: the Safad-Hula loop,
    traced on the CIA scan, georeferenced on its own graticule ticks); grey, cut by the 1947 sub-districts (D-B) so no
    new line is drawn, where a source dates a change of hands between 15 May and 11 June inside a 9 July Israeli area
    (OPENING_CUTS: Lajjun-Zir'in) or where the CIA's dashed border is legible but closes on nothing sourced (the
    corridor); in the Ramle and Acre sub-districts grey only the hex of the place that changed hands (OPENING_CONTESTED,
    the GeoJSON's contested_places: Yibna, Acre; owner ruling 2 of 16 October 2026 on #686), so Rehovot, Rishon,
    Nahariya and the CIA's Rishon-Sarafand area keep the 9 July side; the Gaza and Beersheba sub-districts without colour and their Jewish
    settlements' hexes blue (pinned_towns; owner ruling Q3); Jerusalem no-man's land (the period's `jerusalem` rule, in
    tools/control_bake.py).
Accuracy: about +-1-2 km (a 1:250,000 base, pen lines 0.5-0.7 km wide, a low-resolution scan): source_quality
low_res_scan. Status draft, owner-verify (sensitivity review) for every feature.
"""
import argparse
import io
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import armistice_digitise as ad  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "content" / "map" / "control_1948_49.geojson"
ARMISTICE = ROOT / "content" / "map" / "armistice_lines_1949.geojson"
CHECKED = "2026-10-14"

ISA_MAP = {"cite": "Israel State Archives, Prime Minister's Office, Government Secretariat, map 6158/1, file 'פלשתינה - קווי "
                   "החלוקה וקווי החזית' (Palestine: the partition lines and the front lines), 1949, marked secret; a Survey of "
                   "Palestine 1:250,000 'Palestine: Index to Villages & Settlements' sheet with the lines drawn by hand. ISA id "
                   "ISA-PMO-GovernmentSecretary-R0005czg. The copy used: NYU Faculty Digital Archive, Jewish Settlements Archival "
                   "Project, PDF page 2 (2368 x 4078 px).",
           "url": "https://archive.nyu.edu/bitstream/2451/72921/1/R0005czg.pdf",
           "page": "http://hdl.handle.net/2451/72921",
           "isa": "https://www.archives.gov.il/archives/Archive/0b071706800171a1/File/0b07170687cca62a",
           "sha256": "e6f753fcab2f18aeec78fa9878a27dc79bbd1dc81ffc4d03e5ee04b6be412b80", "checked": CHECKED,
           "licence": "A 1949 Israeli state work: out of copyright in Israel (Copyright Act 2007 s. 42); a possible URAA US term "
                      "in its drawing does not reach the facts taken (where a front line ran on a date); its base is a "
                      "Survey of Palestine sheet (D-B). Facts only, redrawn by our own method; no image is copied."}
ISA_SEARCH = [
    "Israel State Archives catalogue page (the ISA link above) and catalog.archives.gov.il: HTTP 403 (CloudFront) to a plain "
    "client on 2026-10-14; not reached.",
    "Wikimedia Commons (API file searches '6158/1', 'קווי החזית', 'קווי החלוקה וקווי החזית', 'R0005czg', 'front lines 1948 "
    "Israel State Archives map', 'הפוגה הראשונה מפה'): no copy.",
    "NYU Faculty Digital Archive record 2451/72921 (full record): one file, the PDF used (the map at 2368 x 4078 px).",
]
RES181 = {"cite": "UN General Assembly Resolution 181 (II), 29 November 1947, Part II, Boundaries (the Avalon Project text): "
                  "used only to tell the partition line (not traced) from the green front line where they run close.",
          "url": "https://avalon.law.yale.edu/20th_century/res181.asp", "checked": CHECKED}
S1025 = {"cite": "S/1025, the Mediator's report on the observation of the truce (11 June - 9 July 1948): the Jerusalem agreement "
                 "of 16 June, an agreed line with a no-man's land between the two lines.",
         "url": "https://www.un.org/unispal/document/auto-insert-184863/", "checked": CHECKED}
WIKI = "https://en.wikipedia.org/wiki/"
OPERATIONS = {   # operation: (first day, last day + 1, the sources of the dates)
    # the Ten Days: renewed fighting from 8 July (the Egyptian offensive) to the second truce on 18 July
    "ten_days": ("1948-07-08", "1948-07-18", [WIKI + "1948_Arab%E2%80%93Israeli_War"]),
    "shoter": ("1948-07-24", "1948-07-26", [WIKI + "Operation_Shoter"]),
    # Yoav (15-22 October), then Ha-Har south-west of Jerusalem (end of October) and the capture of Beit Jibrin (27 Oct)
    # and al-Dawayima (29 Oct) that the October line already shows
    "yoav": ("1948-10-15", "1948-10-31", [WIKI + "Operation_Yoav", WIKI + "Operation_Ha-Har", WIKI + "Bayt_Jibrin", WIKI + "Al-Dawayima"]),
    "hiram": ("1948-10-29", "1948-10-31", [WIKI + "Operation_Hiram"]),
}

# The drawn lines: (colour, waypoints in scan pixels, each snapped to the nearest pen ink of that colour within 12 px and
# routed along it). r_* red (9 July 1948), b_* blue (18 July 1948), g_* green (end of October 1948). The blue strokes
# under 1.5 km from the red in the Hula valley and at Ein Gev are not traced (below the data's accuracy).
STROKES = {
    "r_galilee": ("red", [(2026, 90), (2005, 135), (1993, 195), (1990, 245), (1990, 300), (1998, 360), (2002, 420),
        (2005, 480), (1980, 535), (1945, 565), (1905, 585), (1890, 620), (1898, 680), (1912, 735), (1903, 770),
        (1888, 820), (1895, 880), (1905, 930), (1900, 990), (1885, 1055), (1858, 1080), (1840, 1045), (1815, 1038),
        (1792, 1062), (1778, 1110), (1782, 1160), (1775, 1188), (1700, 1200), (1600, 1203), (1562, 1185), (1545, 1145),
        (1530, 1105), (1495, 1084), (1430, 1085), (1395, 1070), (1392, 1030), (1412, 998), (1398, 965), (1395, 910),
        (1408, 860), (1440, 815), (1455, 785), (1420, 745), (1375, 715), (1368, 665), (1388, 615), (1425, 560),
        (1470, 525), (1520, 505), (1543, 475), (1532, 447), (1480, 448), (1420, 447), (1362, 445)]),
    "r_east": ("red", [(2030, 92), (2060, 125), (2105, 155), (2150, 160), (2178, 162), (2185, 182), (2180, 212),
        (2175, 242), (2177, 290), (2182, 335), (2175, 360), (2162, 392), (2150, 440), (2140, 480), (2110, 505),
        (2088, 535), (2083, 580), (2095, 640), (2112, 720), (2128, 800), (2145, 860), (2152, 930), (2160, 975),
        (2155, 1020), (2125, 1065), (2095, 1100), (2068, 1145), (2048, 1185)]),
    "r_jenin": ("red", [(2030, 1678), (2000, 1684), (1960, 1680), (1920, 1660), (1880, 1620), (1840, 1580),
        (1800, 1548), (1760, 1537), (1700, 1536), (1640, 1530), (1610, 1508), (1590, 1460), (1555, 1418), (1500, 1398),
        (1430, 1402), (1380, 1420), (1340, 1445), (1300, 1480), (1270, 1530), (1255, 1575), (1220, 1615), (1190, 1650),
        (1168, 1690), (1172, 1720), (1190, 1760), (1210, 1800), (1185, 1835), (1175, 1880), (1170, 1930), (1150, 1965),
        (1145, 2000), (1135, 2050), (1125, 2100), (1110, 2150), (1100, 2200), (1095, 2250), (1080, 2300), (1050, 2340),
        (1020, 2380), (1008, 2420), (1003, 2460), (995, 2500), (988, 2540), (1000, 2570), (1050, 2600), (1110, 2620),
        (1150, 2645), (1170, 2685), (1185, 2708), (1230, 2717), (1300, 2717), (1380, 2712), (1430, 2716), (1470, 2732),
        (1510, 2750), (1530, 2762)]),
    "r_jerusalem": ("red", [(1530, 2762), (1535, 2800), (1535, 2830), (1531, 2850)]),
    "r_south_east": ("red", [(1530, 2850), (1500, 2838), (1470, 2815), (1400, 2792), (1300, 2777), (1200, 2776),
        (1100, 2786), (1050, 2803), (1000, 2840), (950, 2880), (900, 2918), (850, 2950), (800, 2962), (760, 2992),
        (735, 3030), (720, 3075), (707, 3105), (728, 3135), (760, 3152), (800, 3215), (840, 3290), (870, 3370),
        (905, 3440), (915, 3510), (895, 3580), (872, 3655), (865, 3730), (885, 3772), (940, 3765), (1050, 3760),
        (1095, 3768), (1105, 3820), (1080, 3865), (1040, 3910), (1005, 3950)]),
    "r_south_west": ("red", [(706, 2664), (740, 2705), (765, 2745), (760, 2790), (740, 2830), (712, 2875), (690, 2915),
        (668, 2960), (676, 2998), (668, 3040), (652, 3075), (620, 3113), (590, 3150), (565, 3195), (540, 3235),
        (505, 3280), (470, 3320), (440, 3355), (411, 3382), (385, 3430), (372, 3480), (398, 3515), (411, 3540),
        (394, 3591), (339, 3636), (290, 3655), (261, 3669), (230, 3700), (194, 3735), (195, 3780), (205, 3802),
        (240, 3840), (272, 3869), (310, 3905), (350, 3947)]),
    "r_carmel_tira": ("red", [(1170, 1012), (1192, 1018), (1215, 1045), (1208, 1070), (1172, 1076), (1163, 1042),
        (1170, 1012)]),
    "r_carmel_ijzim": ("red", [(1100, 1213), (1130, 1208), (1152, 1188), (1172, 1150), (1200, 1140), (1240, 1137),
        (1270, 1146), (1285, 1172), (1282, 1220), (1270, 1260), (1240, 1290), (1200, 1302), (1150, 1305), (1110, 1295),
        (1088, 1287)]),
    "b_galilee": ("blue", [(1540, 440), (1544, 480), (1533, 540), (1540, 600), (1540, 660), (1526, 720), (1532, 760),
        (1560, 768), (1640, 768), (1668, 795), (1667, 850), (1646, 874), (1583, 884), (1561, 905), (1583, 937),
        (1640, 937), (1750, 926), (1835, 895), (1867, 853), (1882, 800), (1895, 770), (1906, 752)]),
    "b_jenin": ("blue", [(1500, 1398), (1560, 1402), (1640, 1420), (1700, 1450), (1740, 1490), (1790, 1530),
        (1840, 1572), (1880, 1630), (1910, 1690), (1935, 1740), (1960, 1765), (1990, 1770), (2010, 1755), (2015, 1722),
        (2025, 1700)]),
    "b_wadiara": ("blue", [(1258, 1566), (1255, 1600), (1250, 1640), (1230, 1665), (1185, 1675), (1170, 1688)]),
    "b_danny": ("blue", [(1110, 2115), (1125, 2150), (1140, 2200), (1150, 2250), (1160, 2300), (1170, 2350),
        (1180, 2400), (1190, 2450), (1210, 2510), (1250, 2570), (1262, 2610), (1245, 2630), (1200, 2650), (1168, 2678),
        (1175, 2700), (1210, 2707), (1245, 2706), (1258, 2680), (1270, 2654), (1300, 2655), (1330, 2672), (1365, 2693),
        (1400, 2712)]),
    "b_corridor_south": ("blue", [(1515, 2848), (1490, 2850), (1450, 2860), (1400, 2870), (1350, 2865), (1300, 2860),
        (1280, 2842), (1250, 2832), (1200, 2840), (1150, 2846), (1100, 2862), (1085, 2892), (1098, 2930), (1080, 2962),
        (1050, 3000), (1030, 3040), (1000, 3065), (950, 3085), (900, 3092), (850, 3062), (810, 3059), (796, 3073),
        (779, 3101), (773, 3130), (770, 3160)]),
    "b_affa": ("blue", [(681, 2996), (720, 3012), (760, 3008), (786, 2992), (790, 3005), (781, 3021), (760, 3045),
        (735, 3068), (712, 3095), (690, 3110), (650, 3118), (621, 3127), (598, 3150), (572, 3214), (548, 3234)]),
    "g_hahar": ("green", [(1490, 2860), (1420, 2880), (1350, 2900), (1280, 2940), (1210, 2980), (1160, 3005),
        (1120, 3030), (1090, 3070), (1080, 3130), (1080, 3210), (1070, 3240), (1030, 3240), (970, 3200), (940, 3220),
        (930, 3270), (930, 3360), (951, 3452), (974, 3514), (1000, 3570), (1035, 3637), (1070, 3690), (1097, 3740),
        (1102, 3760)]),
    "g_faluja": ("green", [(884, 3074), (912, 3085), (920, 3110), (915, 3140), (880, 3165), (840, 3168), (806, 3153),
        (795, 3120), (791, 3089)]),
}
LEGEND = {"red": ("קו החזית עם גמר ההפוגה הראשונה, 9.7.1948", "the front line at the end of the first truce, 9 July 1948", "1948-07-09"),
          "blue": ("קו החזית עם התחלת ההפוגה השנייה, 18.7.1948", "the front line at the start of the second truce, 18 July 1948", "1948-07-18"),
          "green": ("קו החזית עם פתיחת הדרך לנגב ושחרור הגליל",
                    "the front line at the opening of the road to the Negev and the taking of the Galilee (no date on the map: "
                    "end of October 1948, after Operations Yoav and Hiram, by inference)", "1948-10")}
# Which front each 1948 stroke faces (it leaves on that front's armistice dates); the Faluja pocket's closing stretch of
# b_corridor_south (the piece through POCKET) is Egypt's, the rest of that stroke Jordan's.
SECTOR = {"r_south_west": "egypt", "b_affa": "egypt", "g_faluja": "egypt", "r_east": "syria"}
POCKET = (840, 3062)

# Each snapshot: the strokes drawn and the stretches a later line or a dated event takes out ((stroke, x, y): the piece of
# that stroke, between its crossings with the other strokes, nearest scan pixel (x, y)).
T2_DROPS = [("r_galilee", 1395, 1070, "replaced by b_galilee (Operation Dekel, 8-18 July: Nazareth and the lower Galilee)"),
            ("r_jenin", 1700, 1536, "replaced by b_jenin"), ("r_jenin", 1960, 1680, "replaced by b_jenin"),
            ("r_jenin", 1220, 1615, "replaced by b_wadiara"), ("r_jenin", 1050, 2340, "replaced by b_danny (Lydda, Ramle)"),
            ("r_south_east", 1300, 2777, "replaced by b_corridor_south"), ("r_south_east", 735, 3030, "replaced by b_corridor_south"),
            ("r_south_east", 728, 3135, "replaced by b_corridor_south"), ("r_south_west", 620, 3113, "replaced by b_affa"),
            ("r_south_west", 575, 3195, "replaced by b_affa"),
            ("r_carmel_tira", 1215, 1045, "Tira taken on 16 July 1948 (" + WIKI + "Al-Tira,_Haifa)")]
T2B_DROPS = T2_DROPS + [("r_carmel_ijzim", 1285, 1172, "Ijzim, Jaba and Ein Ghazal taken in Operation Shoter, 24-26 July 1948")]
OCT_DROPS = T2B_DROPS + [("r_galilee", 1990, 300, "the Galilee pocket taken in Operation Hiram, 29-31 October 1948"),
                         ("b_galilee", 1540, 600, "the Galilee pocket taken in Operation Hiram"),
                         ("b_corridor_south", 1050, 3000, "replaced by g_hahar"),
                         ("b_corridor_south", 779, 3101, "replaced by g_faluja"),
                         ("r_south_east", 905, 3440, "replaced by g_hahar (Beersheba, 21 October)")]
SNAPSHOTS = {   # id: (strokes, drops, the source line's date as drawn, shown from)
    "truce1": ([s for s in STROKES if s.startswith("r_")], [], "1948-07-09", "1948-06-11"),
    "truce2": ([s for s in STROKES if s[0] in "rb"], T2_DROPS, "1948-07-18", "1948-07-18"),
    "truce2b": ([s for s in STROKES if s[0] in "rb"], T2B_DROPS, "1948-07-18", "1948-07-26"),
    "october": (list(STROKES), OCT_DROPS, "1948-10", "1948-10-31"),
}
SOUTH_N = 60.0        # the scan's southern neatline (km northing): no 1948 line is sourced south of it
JERUSALEM = (35.2256, 31.7789)
JERUSALEM_KEEP_KM = 4.5   # 1948 pieces this near Jerusalem stay after 3 April 1949 (CLOSURES, Jerusalem)
EGYPT_FRONTIER_N = 71.0   # Art. VI.3 bounds where the 1949 Egypt line meets the frontier: N 70-72 (D-A); the middle
CLOSURE_UNCERTAIN_KM = 3.0
CLOSURES = {
    "jerusalem": "The Jordan line stops at the city's western edge on both sides (map 2, the 30 November 1948 cease-fire "
                 "lines, is not available: D-A). The 1948 lines within 4.5 km of Jerusalem (ISA 6158/1, July 1948) stay to close "
                 "it; the hexes that hold both sides there are grey (no-man's land: S/1025; Art. V.1(b)).",
    "egypt_south": "The 1949 Egypt line is drawn from the Wadi Hasi to Gaza only (D-A gap). It is closed straight from its "
                   "end to the frontier at the middle of the N 70-72 km stretch that Art. VI.3 bounds; the land within "
                   "3 km of that straight stretch has no colour (no sourced line).",
}
ARMISTICE_DATES = ["1949-02-24", "1949-03-23", "1949-04-03", "1949-05-08", "1949-05-22", "1949-07-17", "1949-07-20"]
# Operation Horev (22 December 1948 - 7 January 1949) is not drawn: its fighting lay south of the scan's 60-km northing or
# beyond the frontier, and no source gives a line for January 1949, so the October line stands until the Egypt agreement.

# Holders of the other side, for the info panel only: (place, lon, lat, holder, from, until, source).
# Every row must land on the other side on every snapshot in its window, or the run fails. Jordan is named from its
# agreement, as the state (as Egypt is named as the state throughout); in 1948 the West Bank's holders (the Arab Legion,
# the Iraqi army, Egyptian forces at Hebron) share one connected area that no sourced line divides, so it is "mixed".
HOLDERS = [
    ("Gaza", 34.4597, 31.5075, "egypt", "1948-06-11", None, WIKI + "Gaza_Strip"),
    ("Faluja", 34.7477, 31.6246, "egypt", "1948-06-11", "1949-02-24", WIKI + "Faluja_pocket"),
    ("Ramallah", 35.2017, 31.8969, "jordan", "1949-04-03", None, WIKI + "1949_Armistice_Agreements"),
    ("Nablus", 35.2608, 32.2211, "jordan", "1949-04-03", None, WIKI + "1949_Armistice_Agreements"),
    ("Hebron", 35.0986, 31.535, "mixed", "1948-06-11", "1949-04-03", WIKI + "1948_Arab%E2%80%93Israeli_War"),
    ("Tarshiha", 35.2683, 33.009, "ala", "1948-06-11", "1948-10-31", WIKI + "Arab_Liberation_Army"),
    ("Nazareth", 35.3033, 32.7019, "ala", "1948-06-11", "1948-07-18", WIKI + "Operation_Dekel"),
]
# Towns the sources place on one side that the traced lines, within their accuracy, put on the other: the hex holding the
# town takes the sourced side for the periods inside the window (tools/control_bake.py), as the hex of a Gazette-listed
# place takes its sub-district (D-B). (place, lon, lat, holder, from, until, source, why)
PINNED = [
    ("Mishmar HaYarden", 35.5997, 33.0028, "syria", "1948-06-11", "1949-07-20", WIKI + "Mishmar_HaYarden_(moshava)",
     "taken by the Syrian army in June 1948 and back in Israel's control on 20 July 1949 (en.wiki); the bridgehead is "
     "narrower than ISA 6158/1's accuracy, so its lines put the moshava on the Israeli side"),
    ("Qalqilya", 34.9685, 32.1904, "jordan", "1949-04-03", None, WIKI + "Qalqilya",
     "read 2026-10-14: en.wiki quotes Sami Hadawi, who argues that the 1949 armistice lines severed the town's orange groves "
     "in favour of Israel, leaving it its rocky land to the east, which implies the town stayed on the Jordanian side (an "
     "inference, not a statement of the source); on the signed map 1 of the "
     "Jordan-Israel agreement (the UNISPAL copy D-A traced, 51 m a pixel) the pen line runs along the town's western edge, its "
     "symbol on or just east of the line, which that resolution cannot settle; D-A's trace of it puts the town 2 km to the "
     "Israeli side from 8 May 1949"),
    ("Huleiqat", 34.6497, 31.6008, "egypt", "1948-07-08", "1948-10-19", WIKI + "Hulayqat",
     "retaken by the Egyptian army on 8 July 1948 and captured by the Giv'ati Brigade on 19 October 1948 in Operation Yoav "
     "(en.wiki); ISA 6158/1's 9 July line puts it on the Israeli side, about 1.3 km from the line, so the source and the "
     "archive map disagree for these weeks"),
]
# The closure check's anchors: (place, lon, lat, side, from, until, source). Windows from 15 May 1948 (#674): the places
# whose holder the source gives from before the game's start to past 11 June 1948 (no change of hands in between). Coordinates: settlements_1948.yaml and
# localities_1948.tsv; the Wikipedia API (prop=coordinates) for Negba, Nir Am, Dorot, Shoval, Mishmar HaNegev, Hatzerim,
# Nevatim (each at its 1946 or earlier site), Huleiqat-area places are left out (within 2 km of the lines).
ANCHORS = [
    ("Tel Aviv", 34.78, 32.08, "israel", "1948-05-15", None, WIKI + "Tel_Aviv"),
    ("Haifa", 34.9992, 32.8192, "israel", "1948-05-15", None, WIKI + "Battle_of_Haifa_(1948)"),
    ("Jaffa", 34.7531, 32.0522, "israel", "1948-05-15", None, WIKI + "Jaffa"),
    ("Acre", 35.0817, 32.9278, "israel", "1948-06-11", None, WIKI + "Acre,_Israel"),
    ("Safed", 35.4983, 32.9658, "israel", "1948-05-15", None, WIKI + "Battle_of_Safed"),
    ("Tiberias", 35.5333, 32.7944, "israel", "1948-05-15", None, WIKI + "Tiberias"),
    ("Nahariya", 35.0925, 33.0036, "israel", "1948-05-15", None, WIKI + "Nahariya"),
    ("Beisan", 35.4989, 32.4961, "israel", "1948-06-11", None, WIKI + "Beisan"),
    ("Rehovot", 34.8167, 31.8969, "israel", "1948-05-15", None, WIKI + "Rehovot"),
    ("Hadera", 34.9167, 32.45, "israel", "1948-05-15", None, WIKI + "Hadera"),
    ("Negba", 34.683, 31.6608, "israel", "1948-06-11", None, WIKI + "Negba"),
    ("Nir Am", 34.5808, 31.5194, "israel", "1948-06-11", None, WIKI + "Nir_Am"),
    ("Dorot", 34.6453, 31.5064, "israel", "1948-06-11", None, WIKI + "Dorot"),
    ("Shoval", 34.7428, 31.4128, "israel", "1948-06-11", None, WIKI + "Shoval"),
    ("Mishmar HaNegev", 34.7186, 31.3642, "israel", "1948-06-11", None, WIKI + "Mishmar_HaNegev"),
    ("Hatzerim", 34.7142, 31.2403, "israel", "1948-06-11", None, WIKI + "Hatzerim"),
    ("Nevatim", 34.8828, 31.2236, "israel", "1948-06-11", None, WIKI + "Nevatim"),
    ("Ein Harod", 35.3908, 32.5603, "israel", "1948-05-15", None, WIKI + "Ein_Harod"),
    ("Nahalal", 35.1967, 32.69, "israel", "1948-05-15", None, WIKI + "Nahalal"),
    ("Degania Alef", 35.5747, 32.7081, "israel", "1948-05-15", None, WIKI + "Battles_of_the_Kinarot_Valley"),
    ("Mishmar HaEmek", 35.1417, 32.6097, "israel", "1948-05-15", None, WIKI + "Battle_of_Mishmar_HaEmek"),
    ("Abu Ghosh", 35.1108, 31.8047, "israel", "1948-06-11", None, WIKI + "Abu_Ghosh"),
    ("Nazareth", 35.3033, 32.7019, "arab", "1948-05-15", "1948-07-16", WIKI + "Operation_Dekel"),
    ("Nazareth", 35.3033, 32.7019, "israel", "1948-07-18", None, WIKI + "Operation_Dekel"),
    ("Lydda", 34.8881, 31.9519, "arab", "1948-05-15", "1948-07-11", WIKI + "Operation_Danny"),
    ("Lydda", 34.8881, 31.9519, "israel", "1948-07-18", None, WIKI + "Operation_Danny"),
    ("Ramle", 34.8625, 31.9275, "arab", "1948-05-15", "1948-07-12", WIKI + "Operation_Danny"),
    ("Ramle", 34.8625, 31.9275, "israel", "1948-07-18", None, WIKI + "Operation_Danny"),
    ("Shefa-Amr", 35.1694, 32.8056, "arab", "1948-05-15", "1948-07-14", WIKI + "Operation_Dekel"),
    ("Shefa-Amr", 35.1694, 32.8056, "israel", "1948-07-18", None, WIKI + "Operation_Dekel"),
    ("Saffuriya", 35.2792, 32.7531, "arab", "1948-05-15", "1948-07-16", WIKI + "Saffuriyya"),
    ("Saffuriya", 35.2792, 32.7531, "israel", "1948-07-18", None, WIKI + "Saffuriyya"),
    ("Ijzim", 34.9881, 32.6447, "arab", "1948-05-15", "1948-07-24", WIKI + "Operation_Shoter"),
    ("Ijzim", 34.9881, 32.6447, "israel", "1948-07-26", None, WIKI + "Operation_Shoter"),
    ("Tira (Haifa)", 34.9752, 32.7618, "arab", "1948-05-15", "1948-07-16", WIKI + "Al-Tira,_Haifa"),
    ("Tira (Haifa)", 34.9752, 32.7618, "israel", "1948-07-18", None, WIKI + "Al-Tira,_Haifa"),
    ("Tarshiha", 35.2683, 33.009, "arab", "1948-05-15", "1948-10-29", WIKI + "Operation_Hiram"),
    ("Tarshiha", 35.2683, 33.009, "israel", "1948-10-31", None, WIKI + "Operation_Hiram"),
    ("Jish", 35.4447, 33.0258, "arab", "1948-05-15", "1948-10-29", WIKI + "Operation_Hiram"),
    ("Jish", 35.4447, 33.0258, "israel", "1948-10-31", None, WIKI + "Operation_Hiram"),
    ("Malkiya", 35.5111, 33.0983, "arab", "1948-06-11", "1948-10-29", WIKI + "Operation_Hiram"),
    ("Beersheba", 34.7867, 31.2522, "arab", "1948-06-11", "1948-10-21", WIKI + "Operation_Yoav"),
    ("Beersheba", 34.7867, 31.2522, "israel", "1948-10-31", None, WIKI + "Operation_Yoav"),
    ("Beit Jibrin", 34.8983, 31.6053, "arab", "1948-06-11", "1948-10-27", WIKI + "Bayt_Jibrin"),
    ("Beit Jibrin", 34.8983, 31.6053, "israel", "1948-10-31", None, WIKI + "Bayt_Jibrin"),
    ("Dawayima", 34.9119, 31.5361, "arab", "1948-06-11", "1948-10-29", WIKI + "Al-Dawayima"),
    ("Dawayima", 34.9119, 31.5361, "israel", "1948-10-31", None, WIKI + "Al-Dawayima"),
    ("Faluja", 34.7477, 31.6246, "arab", "1948-06-11", "1949-02-24", WIKI + "Faluja_pocket"),
    ("Majdal", 34.5664, 31.6658, "arab", "1948-06-11", "1948-11-05", WIKI + "Ashkelon"),
    ("Isdud", 34.6578, 31.7572, "arab", "1948-06-11", "1948-10-28", WIKI + "Isdud"),
    ("Yad Mordechai", 34.5583, 31.5886, "arab", "1948-06-11", "1948-11-05", WIKI + "Battle_of_Yad_Mordechai"),
    ("Gaza", 34.4597, 31.5075, "arab", "1948-06-11", None, WIKI + "Gaza_Strip"),
    ("Khan Yunis", 34.3025, 31.3439, "arab", "1948-06-11", None, WIKI + "Gaza_Strip"),
    ("Jenin", 35.3, 32.4611, "arab", "1948-05-15", None, WIKI + "Battle_of_Jenin_(1948)"),
    ("Tulkarm", 35.0308, 32.3111, "arab", "1948-05-15", None, WIKI + "Tulkarm"),
    ("Nablus", 35.2608, 32.2211, "arab", "1948-05-15", None, WIKI + "Nablus"),
    ("Qalqilya", 34.9685, 32.1904, "arab", "1948-05-15", None, WIKI + "Qalqilya"),   # pinned from 3 April 1949 (PINNED)
    ("Hebron", 35.0986, 31.535, "arab", "1948-05-15", None, WIKI + "Hebron"),
    ("Bethlehem", 35.2061, 31.7044, "arab", "1948-05-15", None, WIKI + "Bethlehem"),
    ("Ramallah", 35.2017, 31.8969, "arab", "1948-05-15", None, WIKI + "Ramallah"),
    ("Imwas", 34.9927, 31.842, "arab", "1948-06-11", None, WIKI + "Imwas"),
    ("Umm al-Fahm", 35.1536, 32.5194, "arab", "1948-05-15", "1949-05-22", WIKI + "Umm_al-Fahm"),
]

# --- 15 May - 10 June 1948 (#674, option b) ---------------------------------------------------------------------------
CHECKED_OPENING = "2026-10-16"
CIA_MAP = {"cite": "CIA, Near East/Africa Branch, Intelligence Summary Vol. III No. 19, week ending 19 May 1948, page 2: "
                   "'Military Situation - 18 May 1948', map no. 10981 (Map Branch, CIA, 4-48), CONFIDENTIAL, declassified and "
                   "approved for release 2013-07-22. Legend: 'Border of strongly-held Jewish area' (heavy solid line), 'Border "
                   "of tenuously-held Jewish area' (heavy dashed line). Record CIA-RDP78-01617A004700010003-6; the copy used: "
                   "the Wayback Machine's of the reading room PDF, page 2 (1594 x 2108 px, 1 bit).",
           "url": "https://web.archive.org/web/20250302131758/https://www.cia.gov/readingroom/docs/CIA-RDP78-01617A004700010003-6.pdf",
           "record": "https://www.cia.gov/readingroom/document/cia-rdp78-01617a004700010003-6",
           "sha256": "56915d81ac0f6f69ecef301f12c71362abb34ba5d6fa7d377b9853bb0cf69cbf", "checked": CHECKED_OPENING,
           "licence": "A US federal government work, not protected by US copyright (17 USC 105); its 1:250,000 base is a UK "
                      "military sheet (facts only, as D-B). Facts only, redrawn by our own method; no image is copied."}
# The graticule ticks printed on the map's inner neatline: (degrees, side, the tick's pixel along that side, a first guess).
CIA_TICKS = [("lon", 35.0, "top", 743), ("lon", 35.5, "top", 1075), ("lon", 34.5, "bottom", 380), ("lon", 35.0, "bottom", 724),
             ("lon", 35.5, "bottom", 1068), ("lat", 33.0, "right", 441), ("lat", 32.5, "right", 837), ("lat", 32.0, "right", 1235),
             ("lat", 31.5, "right", 1632), ("lat", 33.0, "left", 433)]
CIA_NEATLINES = {"top": 154, "bottom": 1979, "left": 616, "right": 1308}   # first guesses (px) for the thin inner neatline
# The CIA map's dashed (tenuously held) borders that are legible and close on a sourced line, waypoints on its dashes in scan
# pixels (each snapped to the nearest ink within 4 px): the loop from the frontier near Nabi Yusha down to Safad and east
# to the Jordan, enclosing the Hula. Its ends join the 9 July line or the frontier (within 3 km).
CIA_STROKES = {
    "cia_hula": [(1113, 368), (1107, 384), (1101, 396), (1090, 412), (1077, 426), (1069, 440), (1066, 456), (1068, 470),
                 (1074, 475), (1086, 478), (1102, 479), (1118, 480), (1132, 478), (1146, 475), (1160, 474)],
}
# The faces of the 9 July snapshot cut by each CIA stroke that the dashed border encloses, each by a place inside it (lon,
# lat), and their sane total area (km2): a stroke that failed to close would merge a face with its neighbour. The Hula loop
# holds the 9 July Israeli face (Rosh Pinna: settlements_1948.yaml) and the strip west of the Jordan that ISA 6158/1's 9
# July line gives the other side (the Syrian bridgehead at Mishmar HaYarden, taken on 10 June; a point inside it read on
# the 9 July polygons), which the CIA loop encloses on 18 May.
CIA_FACES = {"cia_hula": ([("Rosh Pinna", 35.5422, 32.97), ("the 9 July line's strip west of the Jordan", 35.6192, 33.0343)],
                          (100.0, 700.0))}
WIKI_OPENING = {
    "acre": [WIKI + "Operation_Ben-Ami", WIKI + "Acre,_Israel"],
    "jenin": [WIKI + "Battle_of_Jenin_(1948)", WIKI + "Zir%27in"],
    "jerusalem": [WIKI + "Burma_Road_(Israel)", WIKI + "Battles_of_Latrun_(1948)", WIKI + "Operation_Pitchfork"],
    "hula": [WIKI + "Al-Nabi_Yusha%27", WIKI + "Al-Malkiyya", WIKI + "Mishmar_HaYarden_(moshava)"],
    "south": [WIKI + "Battle_of_Yad_Mordechai", WIKI + "Battle_of_Nitzanim", WIKI + "Isdud"],
}
# Grey, cut by a 1947 sub-district (D-B; no new line), inside the 9 July Israeli area: (sub-district, why, sources).
OPENING_CUTS = {
    "contested_opening_jenin": ("jenin", "Lajjun and Zir'in, taken on 28 May 1948, then the battle of Jenin (31 May - 4 June) "
                                "(en.wiki); the 9 July Israeli area of the Jenin sub-district", WIKI_OPENING["jenin"]),
    "contested_opening_jerusalem": ("jerusalem", "the Bab el Wad - El Qastal - Jerusalem corridor, a single dashed (tenuously "
                                    "held) border along the road on the CIA's 18 May map, which closes on no sourced line (the "
                                    "Legion blocked the Latrun road from 18 May; the Burma Road); the 9 July Israeli area of the "
                                    "Jerusalem sub-district (the city's own hexes are no-man's land: the period's jerusalem rule)",
                                    [CIA_MAP["url"]] + WIKI_OPENING["jerusalem"]),
}
# Owner ruling 2 of 16 October 2026 on #686 (cut breadth): in the Ramle and Acre sub-districts grey only the hexes that
# changed hands, Yibna's and the Acre coast's; Rehovot, Rishon and Nahariya stay blue. Each place's hex (placed as the game
# places settlements) is contested for the opening period (tools/control_bake.py rule 5): (place, lon, lat, sources, why).
# Coordinates: localities_1948.yaml (Yibna), settlements_1948.yaml (Acre).
OPENING_CONTESTED = [
    ("Yibna", 34.7463, 31.866, [WIKI + "Yibna"],
     "Yibna, occupied by the Givati Brigade on 5 June 1948 (en.wiki Yibna, revision 1375084946)"),
    ("Acre", 35.0817, 32.9278, WIKI_OPENING["acre"],
     "Acre: Operation Ben-Ami from 13 May; attacked on 16 May, the town surrendered on the night of 17/18 May 1948 (en.wiki "
     "Acre, Israel, revision 1380244094; Operation Ben-Ami, revision 1369980718)"),
]
OPENING_SOUTH = ("gaza", "beersheba")
# Q3 "settlement hexes blue": the Jewish settlements of the Gaza sub-district and of the Beersheba sub-district north of the
# 60-km northing whose 1948 site the repository already holds, sourced (settlements_1948.yaml; the ANCHORS above, each at its
# 1946 or earlier site). Others (Nitzanim, Kfar Darom, Nirim, Be'erot Yitzhak and more) are not pinned: their sites moved
# after 1948 and no 1948 site is sourced here yet (listed in known_staleness).
OPENING_SETTLEMENTS = [("Yad Mordechai", 34.5583, 31.5886, WIKI + "Battle_of_Yad_Mordechai", "held until it fell on 24 May 1948 (en.wiki): known staleness"),
                       ("Negba", 34.683, 31.6608, WIKI + "Negba", "held throughout (attacked on 2 June 1948)"),
                       ("Nir Am", 34.5808, 31.5194, WIKI + "Nir_Am", "held throughout"),
                       ("Dorot", 34.6453, 31.5064, WIKI + "Dorot", "held throughout"),
                       ("Shoval", 34.7428, 31.4128, WIKI + "Shoval", "held throughout"),
                       ("Mishmar HaNegev", 34.7186, 31.3642, WIKI + "Mishmar_HaNegev", "held throughout"),
                       ("Hatzerim", 34.7142, 31.2403, WIKI + "Hatzerim", "held throughout"),
                       ("Nevatim", 34.8828, 31.2236, WIKI + "Nevatim", "held throughout")]
for _n, _x, _y, _src, _held in OPENING_SETTLEMENTS:
    PINNED.append((_n, _x, _y, "israel", "1948-05-15", "1948-06-11", _src,
                   "a Jewish settlement in the uncoloured south (owner ruling Q3, #674: no colour, settlement hexes blue); " + _held))
CIA_ACCURACY = ("the 9 July lines: " + "about +-1-2 km (ISA 6158/1)" + "; the CIA's 18 May dashed border: about +-1.5 km on the scan "
                "(its graticule fit), on a map compiled at 1:250,000-1:500,000 and printed near 1:1,000,000 (about +-3-5 km)")

FACT_SOURCES = [ISA_MAP, RES181, S1025, CIA_MAP]


# --- the scan ---------------------------------------------------------------------------------------------------------

def load_scan(pdf: Path) -> np.ndarray:
    from PIL import Image
    from pypdf import PdfReader
    ad.check(pdf, ISA_MAP)
    page = PdfReader(pdf).pages[1]
    img = max(page.images, key=lambda im: im.image.size[0] * im.image.size[1]).image.convert("RGB")
    a = np.asarray(img).astype(np.int16)
    if a.shape[:2] != (4078, 2368):
        raise SystemExit(f"{pdf}: the map image is {a.shape[1]} x {a.shape[0]} px, expected 2368 x 4078")
    return a


INSET = (280, 680, 820, 1570)    # x0, y0, x1, y1: the "Southern Palestine" inset (its own grid)
LEGEND_BOX = (60, 1680, 900, 2060)
DEG = 3


def terms(x, y):
    x = np.asarray(x, float) / 1000; y = np.asarray(y, float) / 1000
    return np.stack([x ** i * y ** j for i in range(DEG + 1) for j in range(DEG + 1 - i)], -1)


def georeference(a: np.ndarray, log) -> dict:
    """E and N (km) as cubic functions of the scan pixel, fitted to the printed 10-km grid: in 120-px bands across the map,
    each grid line's darkness peak within 7-10 px of where the current fit puts it, iterated four times."""
    lum = a.mean(2); sat = a.max(2) - a.min(2)
    dark = ((lum < 185) & (sat < 50)).astype(np.float32)
    H, W = dark.shape
    E = lambda x, y: 80 + (np.asarray(x, float) - 104.0) / 15.71     # first guess: E 80 at the left neatline,
    N = lambda x, y: 60 + (3956.0 - np.asarray(y, float)) / 16.15    # N 60 at the bottom one
    inside = lambda x, y, b: b[0] <= x <= b[2] and b[1] <= y <= b[3]
    for it in range(4):
        obs_e, obs_n = [], []
        win, stride, tol = 120, 60, (7 if it else 10)
        for y0 in range(110, 3940 - win, stride):
            yc = y0 + win / 2; prof = None
            xs = np.arange(W); ev = E(xs, np.full(W, yc))
            for k in range(80, 230, 10):
                i = int(np.argmin(np.abs(ev - k)))
                if abs(ev[i] - k) > 0.2 or i < 60 or i > W - 60 or inside(i, yc, INSET) or inside(i, yc, LEGEND_BOX):
                    continue
                if prof is None:
                    prof = dark[y0:y0 + win].mean(0)
                seg = prof[i - tol:i + tol + 1]; j = int(np.argmax(seg))
                if seg[j] - np.median(prof[i - 40:i + 40]) > 0.35:
                    obs_e.append((i - tol + j, yc, k))
        for x0 in range(110, W - 60 - win, stride):
            xc = x0 + win / 2; prof = None
            ys = np.arange(H); nv = N(np.full(H, xc), ys)
            for k in range(60, 310, 10):
                i = int(np.argmin(np.abs(nv - k)))
                if abs(nv[i] - k) > 0.2 or i < 60 or i > H - 60 or inside(xc, i, INSET) or inside(xc, i, LEGEND_BOX):
                    continue
                if prof is None:
                    prof = dark[:, x0:x0 + win].mean(1)
                seg = prof[i - tol:i + tol + 1]; j = int(np.argmax(seg))
                if seg[j] - np.median(prof[i - 40:i + 40]) > 0.35:
                    obs_n.append((xc, i - tol + j, k))
        oe, on = np.array(obs_e, float), np.array(obs_n, float)
        fits = []
        for o in (oe, on):
            c = np.linalg.lstsq(terms(o[:, 0], o[:, 1]), o[:, 2], rcond=None)[0]
            keep = np.abs(terms(o[:, 0], o[:, 1]) @ c - o[:, 2]) < 0.25
            c = np.linalg.lstsq(terms(o[keep, 0], o[keep, 1]), o[keep, 2], rcond=None)[0]
            res = terms(o[keep, 0], o[keep, 1]) @ c - o[keep, 2]
            fits.append((c, res, int(keep.sum()), len(o)))
        (ce, re, ne, te), (cn, rn, nn, tn) = fits
        E = lambda x, y, c=ce: terms(x, y) @ c
        N = lambda x, y, c=cn: terms(x, y) @ c
    diag = {"grid_points": [ne, nn], "of": [te, tn], "rms_m": [round(float(np.sqrt((re ** 2).mean())) * 1000), round(float(np.sqrt((rn ** 2).mean())) * 1000)],
            "max_m": [round(float(np.abs(re).max()) * 1000), round(float(np.abs(rn).max()) * 1000)]}
    log(f"ISA 6158/1: georeferenced on {ne} + {nn} grid crossings, rms {diag['rms_m'][0]} / {diag['rms_m'][1]} m (E / N), "
        f"max {diag['max_m'][0]} / {diag['max_m'][1]} m")
    return {"E": ce, "N": cn, "diag": diag}


def to_km(fit: dict, x, y):
    t = terms(x, y)
    return t @ fit["E"], t @ fit["N"]


def colour_masks(a: np.ndarray) -> dict:
    """Each pen colour as a mask: red (with its pink, lighter strokes), blue (with its darker, purple strokes), and the green
    line's light turquoise; the partition line's dark teal falls outside all three (it is darker and greyer)."""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx = a.max(2); mn = a.min(2); sat = mx - mn
    return {"red": ((r - g > 70) & (r - b > 45) & (r > 120)) | ((r > 200) & (r - g > 45) & (r - b > 35) & (g < 200)),
            "blue": ((b - r > 45) & (b > g - 5) & (mx < 200) & (sat > 50)) | ((b - r > 30) & (b - g > 25) & (mx < 170)),
            "green": (g > 150) & (g - r > 45) & (b > 115) & (g - b > -5) & (mx < 235)}


def trace_all(a: np.ndarray, log) -> dict:
    from scipy import ndimage as ndi
    from shapely.geometry import LineString
    from skimage.graph import route_through_array
    cost, snap = {}, {}
    for k, m in colour_masks(a).items():
        m = ndi.binary_opening(m, structure=np.ones((2, 2)))
        near = ndi.binary_dilation(m, iterations=5)
        cost[k] = np.where(m, 1.0, np.where(near, 5.0, 30.0)).astype(np.float32)
        snap[k] = ndi.distance_transform_edt(~m, return_indices=True)[1]

    def run(colour, wps):
        if tuple(wps[0]) == tuple(wps[-1]) and len(wps) > 4:   # a closed loop: two halves
            h = len(wps) // 2
            p1, o1 = run(colour, wps[:h + 1]); p2, o2 = run(colour, wps[h:])
            return p1 + p2[1:], (o1 + o2) / 2
        iy, ix = snap[colour]
        pts = []
        for x, y in wps:
            sx, sy = int(ix[y, x]), int(iy[y, x])
            pts.append((sx, sy) if np.hypot(sx - x, sy - y) <= 12 else (x, y))
        c = cost[colour]; path, hits = [pts[0]], []
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            bx0, by0 = max(min(x0, x1) - 40, 0), max(min(y0, y1) - 40, 0)
            sub = c[by0:max(y0, y1) + 40, bx0:max(x0, x1) + 40]
            p_, _ = route_through_array(sub, (y0 - by0, x0 - bx0), (y1 - by0, x1 - bx0), fully_connected=True, geometric=True)
            p_ = [(x + bx0, y + by0) for y, x in p_]
            hits += [bool(c[y, x] == 1.0) for x, y in p_]
            path += p_[1:]
        clean = []
        for q in path:                                        # out-and-back spurs cut
            for j in range(len(clean) - 8, max(len(clean) - 400, -1), -1):
                if j >= 0 and abs(clean[j][0] - q[0]) <= 2 and abs(clean[j][1] - q[1]) <= 2:
                    del clean[j + 1:]; break
            clean.append(q)
        return [tuple(map(float, v)) for v in LineString(clean).simplify(1.5).coords], float(np.mean(hits))
    out = {}
    for name, (colour, wps) in STROKES.items():
        xy, on = run(colour, wps)
        out[name] = {"colour": colour, "px": xy, "on_ink": round(on, 3)}
        log(f"  {name}: {len(xy)} vertices, {on:.0%} on its colour's ink")
    return out


# --- the CIA scan (#674) ----------------------------------------------------------------------------------------------

def load_cia(pdf: Path) -> np.ndarray:
    """The 18 May 1948 map (page 2) as an ink mask (True: dark)."""
    from pypdf import PdfReader
    ad.check(pdf, CIA_MAP)
    page = PdfReader(pdf).pages[1]
    img = max(page.images, key=lambda im: im.image.size[0] * im.image.size[1]).image.convert("L")
    a = np.asarray(img) < 128
    if a.shape != (2108, 1594):
        raise SystemExit(f"{pdf}: the map image is {a.shape[1]} x {a.shape[0]} px, expected 1594 x 2108")
    return a


def cia_georeference(ink: np.ndarray, log) -> dict:
    """Longitude and latitude as affine functions of the pixel, fitted to the graticule ticks printed on the map's thin
    inner neatline (CIA_TICKS): each neatline's row or column, then each tick's centre where it leaves it. Fails if a tick
    is not found or a residual is over 0.01 degree (about 1 km)."""
    def centre(profile, lo, guess, w):
        seg = profile[guess - w:guess + w + 1]
        idx = np.nonzero(seg >= 0.8)[0]
        if len(idx) == 0:
            raise SystemExit(f"CIA scan: no ink line near pixel {guess}")
        return float(lo + guess - w + idx.mean())
    nl = {"top": centre(ink[:, 800:1000].mean(1), 0, CIA_NEATLINES["top"], 6),
          "bottom": centre(ink[:, 800:1000].mean(1), 0, CIA_NEATLINES["bottom"], 6),
          "right": centre(ink[1000:1100, :].mean(0), 0, CIA_NEATLINES["right"], 6),
          "left": centre(ink[250:380, :].mean(0), 0, CIA_NEATLINES["left"], 6)}
    obs = {"lon": [], "lat": []}
    for kind, deg, side, guess in CIA_TICKS:
        if side in ("top", "bottom"):
            y = int(nl[side]); band = ink[y + 3:y + 9] if side == "top" else ink[y - 9:y - 3]
            obs[kind].append((centre(band.mean(0), 0, guess, 8), nl[side], deg))
        else:
            x = int(nl[side]); band = ink[:, x - 9:x - 3] if side == "right" else ink[:, x + 3:x + 9]
            obs[kind].append((nl[side], centre(band.mean(1), 0, guess, 8), deg))
    fit, res = {}, {}
    for k, o in obs.items():
        o = np.array(o); A = np.c_[np.ones(len(o)), o[:, 0], o[:, 1]]
        c = np.linalg.lstsq(A, o[:, 2], rcond=None)[0]
        fit[k] = c; res[k] = A @ c - o[:, 2]
    worst = [round(float(np.abs(res[k]).max()), 5) for k in ("lon", "lat")]
    if max(worst) > 0.01:
        raise SystemExit(f"CIA scan: a graticule residual is {max(worst)} degrees (over 0.01)")
    km_px = (float(fit["lon"][1]) * 94.6, -float(fit["lat"][2]) * 110.9)
    log(f"CIA 18 May 1948: georeferenced on {len(CIA_TICKS)} graticule ticks, max residual {worst[0]} / {worst[1]} deg "
        f"(lon / lat), about {km_px[0]:.3f} / {km_px[1]:.3f} km a pixel")
    return {"lon": fit["lon"], "lat": fit["lat"],
            "diag": {"ticks": len(CIA_TICKS), "max_residual_deg": worst, "neatlines_px": {k: round(v, 1) for k, v in nl.items()},
                     "km_per_px": [round(v, 4) for v in km_px], "method": "affine lon/lat of the pixel, least squares on the ticks"}}


def cia_trace(ink: np.ndarray, fit: dict, log) -> dict:
    """Each CIA stroke's waypoints, snapped to the nearest ink within 4 px, as a line in Palestine-grid km; on_ink is the
    share of waypoints that found ink."""
    from scipy import ndimage as ndi
    from shapely.geometry import LineString
    iy, ix = ndi.distance_transform_edt(~ink, return_indices=True)[1]
    out = {}
    for name, wps in CIA_STROKES.items():
        pts, hits = [], 0
        for x, y in wps:
            sx, sy = int(ix[y, x]), int(iy[y, x])
            if np.hypot(sx - x, sy - y) <= 4:
                pts.append((sx, sy)); hits += 1
            else:
                pts.append((x, y))
        p = np.array(pts, float)
        lon = fit["lon"][0] + fit["lon"][1] * p[:, 0] + fit["lon"][2] * p[:, 1]
        lat = fit["lat"][0] + fit["lat"][1] * p[:, 0] + fit["lat"][2] * p[:, 1]
        e, n = ad.lonlat_to_mr(lon, lat)
        out[name] = {"km": LineString(np.c_[e, n]), "on_ink": round(hits / len(wps), 3)}
        log(f"  {name}: {len(wps)} waypoints, {hits / len(wps):.0%} on the scan's ink")
    return out


def subdistrict_km(key: str):
    import shapely
    from shapely.geometry import shape
    doc = json.loads(ad.SUBDISTRICTS.read_text(encoding="utf-8"))
    g = next(shape(f["geometry"]) for f in doc["features"] if f["properties"]["id"] == key)
    return shapely.transform(g, lambda c: np.c_[ad.lonlat_to_mr(c[:, 0], c[:, 1])])


def cia_faces(snap: dict, cia: dict, land, log) -> dict:
    """{stroke: the area it encloses}: the 9 July snapshot's lines and the land outline, with the stroke joined to them
    (its ends within 3 km), polygonised; the face holding the stroke's named place (CIA_FACES). Fails when that face is
    outside its sane area, which an unclosed stroke would cause."""
    import shapely
    from shapely.geometry import Point
    from shapely.ops import polygonize, unary_union
    t1 = [s for s, _ in snap["pieces"]]
    out = {}
    for name, c in cia.items():
        joined, joins = join_ends({name: c["km"]}, unary_union(t1 + [land.boundary]))
        if any("free_km" in j for j in joins):
            raise SystemExit(f"{name}: an end is not within 3 km of the 9 July line or the frontier ({joins})")
        faces = [f for f in polygonize(unary_union(t1 + [joined[name], land.boundary])) if land.contains(f.representative_point())]
        seeds, (lo, hi) = CIA_FACES[name]
        got = []
        for place, lon, lat in seeds:
            e, n = ad.lonlat_to_mr(lon, lat)
            face = next((f for f in faces if f.contains(Point(float(e), float(n)))), None)
            if face is None:
                raise SystemExit(f"{name}: no face holds {place}")
            got.append(face)
        area = shapely.union_all(got)
        if not lo <= area.area <= hi:
            raise SystemExit(f"{name}: the faces holding {', '.join(p for p, _, _ in seeds)} are {area.area:.0f} km2, outside {lo}-{hi}")
        out[name] = {"area": area, "joins": joins}
        log(f"  {name}: encloses {area.area:.0f} km2 (the faces holding {', '.join(p for p, _, _ in seeds)})")
    return out


# --- snapshots --------------------------------------------------------------------------------------------------------

def stroke_km(fit, px):
    from shapely.geometry import LineString
    p = np.array(px, float)
    e, n = to_km(fit, p[:, 0], p[:, 1])
    return LineString(np.c_[e, n])


def px_km(fit, x, y):
    from shapely.geometry import Point
    e, n = to_km(fit, np.array([x]), np.array([y]))
    return Point(float(e[0]), float(n[0]))


def join_ends(lines: dict, outline, snap_km=3.0, log=print) -> tuple[dict, list]:
    """Each line's ends within snap_km of another line or the outline are carried onto it, 50 m past, so the noder makes
    a node; returns the lines and the joins over 0.3 km."""
    from shapely.geometry import LineString, Point
    from shapely.ops import nearest_points, unary_union
    out, joins = {}, []
    for n, l in lines.items():
        cs = list(l.coords)
        others = unary_union([v for k, v in lines.items() if k != n] + [outline])
        for i in (0, -1):
            p = Point(cs[i]); d = p.distance(others)
            if d > snap_km:
                joins.append({"line": n, "end": "start" if i == 0 else "end", "free_km": round(d, 2)})
                continue
            if d < 1e-3:
                a_ = np.array(cs[i]); b_ = np.array(cs[1] if i == 0 else cs[-2]); v = (a_ - b_) / max(np.linalg.norm(a_ - b_), 1e-9)
                q2 = tuple(a_ + 0.05 * v)
                cs.insert(0, q2) if i == 0 else cs.append(q2)
                continue
            q = nearest_points(p, others)[1]
            v = np.array([q.x - p.x, q.y - p.y]); v = v / max(np.linalg.norm(v), 1e-9)
            q2 = (q.x + 0.05 * v[0], q.y + 0.05 * v[1])
            if i == 0: cs[0:0] = [q2, (q.x, q.y)]
            else: cs += [(q.x, q.y), q2]
            if d > 0.3:
                joins.append({"line": n, "end": "start" if i == 0 else "end", "joined_km": round(d, 2)})
        out[n] = LineString(cs)
    return out, joins


def pieces(lines: dict) -> list:
    """The lines noded against each other: [(piece, owner line name)]."""
    from shapely.ops import unary_union
    noded = unary_union(list(lines.values()))
    segs = list(getattr(noded, "geoms", [noded]))
    return [(s, min(lines, key=lambda k: lines[k].distance(s.interpolate(0.5, normalized=True)))) for s in segs]


def faces_and_sides(segs: list, land, seed=(34.78, 32.08), tiny=1.0, strict=True):
    """Polygonises the front-line pieces with the land outline; side 0 (the seed's, Israel) or 1 per face by parity."""
    import shapely
    from shapely.geometry import Point
    from shapely.ops import polygonize, unary_union
    allb = unary_union(list(segs) + [land.boundary])
    faces = [f for f in polygonize(allb) if land.contains(f.representative_point()) and f.area > 1e-6]
    tree = shapely.STRtree(faces)
    adj = {i: set() for i in range(len(faces))}
    for i, f in enumerate(faces):
        for j in tree.query(f):
            j = int(j)
            if j > i and f.boundary.intersection(faces[j].boundary).length > 1e-4:
                adj[i].add(j); adj[j].add(i)
    big = {i for i, f in enumerate(faces) if f.area >= tiny}
    e, n = ad.lonlat_to_mr(*seed)
    s0 = next(i for i, f in enumerate(faces) if f.contains(Point(float(e), float(n))))
    side, todo, conflicts = {s0: 0}, [s0], []
    while todo:
        i = todo.pop()
        for j in adj[i]:
            if j not in big:
                continue
            if j not in side:
                side[j] = 1 - side[i]; todo.append(j)
            elif side[j] == side[i]:
                conflicts.append((i, j))
    for i in range(len(faces)):
        if i not in side:
            nb = [j for j in adj[i] if j in side]
            if nb:
                side[i] = side[max(nb, key=lambda j: faces[j].area)]
    for i in range(len(faces)):
        if i not in side:          # a land island with no drawn line: the nearest face's side
            side[i] = side[min(side, key=lambda k: faces[k].distance(faces[i]))]
    if conflicts and not strict:
        return faces, side, conflicts
    if conflicts:
        p = faces[conflicts[0][1]].representative_point()
        raise SystemExit(f"the lines do not separate the sides: {len(conflicts)} faces reached by both parities "
                         f"(first near E {p.x:.1f} N {p.y:.1f} km)")
    return faces, side


def sides_union(faces, side):
    import shapely
    return {k: shapely.union_all([f for i, f in enumerate(faces) if side[i] == v]) for k, v in (("israel", 0), ("arab", 1))}


def build_snapshot(fit, traced, land, names, drops, log):
    lines = {n: stroke_km(fit, traced[n]["px"]) for n in names}
    lines, joins = join_ends(lines, land.boundary)
    ps = pieces(lines)
    keep = [True] * len(ps)
    for stroke, x, y, why in drops:
        p = px_km(fit, x, y)
        cand = [i for i, (s, o) in enumerate(ps) if o == stroke and keep[i]]
        i = min(cand, key=lambda k: ps[k][0].distance(p)) if cand else None
        if i is None or ps[i][0].distance(p) > 1.0:
            raise SystemExit(f"drop {stroke} at ({x}, {y}): no piece of it within 1 km")
        keep[i] = False
    kept = [(s, o) for (s, o), k in zip(ps, keep) if k]
    faces, side = faces_and_sides([s for s, _ in kept], land)
    return {"faces": faces, "side": side, "pieces": kept, "joins": joins, "areas": sides_union(faces, side)}


def side_at(snap, lon, lat):
    from shapely.geometry import Point
    e, n = ad.lonlat_to_mr(lon, lat)
    p = Point(float(e), float(n))
    for k, a in snap["areas"].items():
        if a.contains(p):
            return k
    return None


def line_distance_km(snap, lon, lat):
    from shapely.geometry import Point
    from shapely.ops import unary_union
    e, n = ad.lonlat_to_mr(lon, lat)
    return unary_union([s for s, _ in snap["pieces"]]).distance(Point(float(e), float(n)))


def check_holders(snap, sid, start, until) -> None:
    """Every HOLDERS row whose window covers the snapshot's span lands on the other side (no tolerance: it names the area)."""
    for name, lon, lat, h, f, u, src in HOLDERS:
        if f <= start and (u is None or (until is not None and until <= u)):
            if side_at(snap, lon, lat) != "arab":
                raise SystemExit(f"holder check, {sid}: {name} ({h}) is not on the other side ({src})")


def check_anchors(snap, sid, start, until, log, tol_km=2.0) -> list:
    """Every anchor whose sourced window covers the snapshot's whole span lands on its side, unless within tol_km of a line
    (the accuracy: 2 km for ISA 6158/1, 3 km once the Jordan line, traced from a low-resolution scan, is in)."""
    out = []
    for name, lon, lat, want, a_from, a_until, src in ANCHORS:
        if a_from > start or (a_until is not None and a_until < (until or "9999")):
            continue
        got = side_at(snap, lon, lat)
        near = line_distance_km(snap, lon, lat)
        ok = got == want or near < tol_km
        out.append({"place": name, "want": want, "got": got, "line_km": round(near, 1), "ok": ok})
        if not ok:
            raise SystemExit(f"closure check, {sid}: {name} is on the {got} side, sources say {want} ({src})")
    log(f"  {sid}: {len(out)} anchors on their sides ({sum(1 for o in out if o['line_km'] < tol_km)} within {tol_km:g} km of a line)")
    return out


# --- the armistice fronts ---------------------------------------------------------------------------------------------

def armistice_features():
    import shapely
    from shapely.geometry import shape
    doc = json.loads(ARMISTICE.read_text(encoding="utf-8"))
    tr = lambda g: shapely.transform(g, lambda c: np.c_[ad.lonlat_to_mr(c[:, 0], c[:, 1])])
    fronts = {f["key"]: f["shown_from"] for f in doc["statecraft"]["fronts"]}
    lines, zones = [], []
    for f in doc["features"]:
        p = f["properties"]
        item = {"id": p["id"], "front": p["front"], "from": p["shown_from"], "geom": tr(shape(f["geometry"])), "name": p["name"],
                "accuracy": p.get("accuracy", ""), "source_quality": p.get("source_quality"), "sources": p.get("sources", [])}
        if p["kind"] == "line":
            if p["id"].startswith("egypt_mv1950"):
                continue     # the 1950 modus vivendi line does not change either side's position (S/1471 Art. III)
            if p["id"] == "jordan_adl_d_arava":
                continue     # it follows the Mandate frontier (D-A: a median 0.42 km from it), which bounds the land already
            lines.append(item)
        elif p["kind"] == "zone":
            zones.append(item)
    return fronts, lines, zones


def build_armistice(date, fit, oct_snap, fronts, arm_lines, land_all, land_n, log, strict=True):
    """One date's sides: the switched fronts' armistice lines in force, the 1948 pieces of the other fronts (and of a
    switched front, those whose nearest armistice line of that front is not yet in force), and the CLOSURES."""
    from shapely.geometry import LineString, Point
    from shapely.ops import substring
    switched = {k for k, d in fronts.items() if d and d <= date}
    in_force = [l for l in arm_lines if l["front"] in switched and l["from"] <= date]
    jer = Point(*[float(v) for v in ad.lonlat_to_mr(*JERUSALEM)])
    pocket = px_km(fit, *POCKET)
    kept, notes = [], []
    chunks = []
    for s, owner in oct_snap["pieces"]:      # each piece in stretches of at most 3 km, each judged by its own nearest line
        k = max(1, int(np.ceil(s.length / 3.0)))
        chunks += [(substring(s, i * s.length / k, (i + 1) * s.length / k), owner) for i in range(k)]
    for s, owner in chunks:
        front = SECTOR.get(owner, "jordan")
        if owner == "b_corridor_south" and s.distance(pocket) < 1.0:
            front = "egypt"
        if front not in switched:
            kept.append(s); continue
        if front == "jordan" and s.distance(jer) < JERUSALEM_KEEP_KM:
            kept.append(s); notes.append("jerusalem"); continue
        mine = [l for l in arm_lines if l["front"] == front]
        if mine:
            nearest = min(mine, key=lambda l: l["geom"].distance(s))
            if nearest["from"] > date:
                kept.append(s)
    lines = {f"p{i}": s for i, s in enumerate(kept)}
    for l in in_force:
        lines[l["id"]] = l["geom"]
    uncertain = []
    if "egypt" in switched:
        g = next(l for l in in_force if l["id"] == "egypt_adl_gaza")["geom"]
        end = min((Point(g.coords[0]), Point(g.coords[-1])), key=lambda p: p.y)
        ring = land_all.boundary
        cand = [ring.interpolate(t) for t in np.linspace(0, ring.length, 20000)]
        fr = min((p for p in cand if abs(p.y - EGYPT_FRONTIER_N) < 0.5 and p.x < end.x), key=lambda p: p.x)
        closing = LineString([end.coords[0], fr.coords[0]])
        lines["egypt_south_closure"] = closing
        uncertain.append(closing)
        notes.append("egypt_south")
    land = land_all if "jordan" in switched else land_n
    # the two sources (ISA 6158/1 and the agreements' maps, each about +-1-2.5 km) meet within 5 km
    lines, joins = join_ends(lines, land.boundary, snap_km=5.0)
    ps = pieces(lines)
    # where the two sources meet (a 1948 line kept until its stage, an agreement's line) they leave small faces between
    # them, and the Jordan trace loops on itself near Beit Surik: under 5 km2 these take their largest neighbour's side
    r = faces_and_sides([s for s, _ in ps], land, tiny=5.0, strict=strict)
    faces, side = r[0], r[1]
    conflicts = r[2] if len(r) > 2 else []
    return {"faces": faces, "side": side, "pieces": ps, "joins": joins, "areas": sides_union(faces, side), "conflicts": conflicts,
            "switched": sorted(switched), "closures": sorted(set(notes)), "uncertain": uncertain}


# --- periods ----------------------------------------------------------------------------------------------------------

def split_holders(area, date):
    """{holder: MultiPolygon} for the other side's area on a date, for the info panel only: each connected part takes the
    holder of the HOLDERS places inside it when they all name one; several holders make it "mixed" (no line between
    them is drawn or claimed), none "unspecified"."""
    import shapely
    from shapely.geometry import Point
    out = {}
    for part in getattr(area, "geoms", [area]):
        found = set()
        for name, x, y, hh, f, u, _ in HOLDERS:
            if f <= date and (u is None or date < u):
                e, n = ad.lonlat_to_mr(x, y)
                if part.contains(Point(float(e), float(n))):
                    found.add(hh)
        h = next(iter(found)) if len(found) == 1 else ("mixed" if found else "unspecified")
        out.setdefault(h, []).append(part)
    return {k: shapely.union_all(v) for k, v in out.items()}


def xor_israel(a, b):
    return a["areas"]["israel"].symmetric_difference(b["areas"]["israel"])


def km_box(e0, n0, e1, n1):
    from shapely.geometry import box
    return box(e0, n0, e1, n1)


# --- output -----------------------------------------------------------------------------------------------------------

ISA_ACCURACY = "about +-1-2 km (ISA 6158/1: a 1:250,000 base, pen lines 0.5-0.7 km wide, a 2368 x 4078 px scan)"
ARMISTICE_ACCURACY = ("the agreements' lines (content/map/armistice_lines_1949.geojson, D-A): about +-0.1-0.3 km for Syria and "
                      "the Gaza line, the Mandate frontier for Lebanon (about +-1 km), Jordan about +-1-2.5 km (low accuracy, from "
                      "a low-resolution scan of map 1); where a front has not yet switched, ISA 6158/1's lines (about +-1-2 km)")


def area_feature(fid, snapshot, side, geom, holder=None, kind=None, extra=None, sources=None, accuracy=ISA_ACCURACY,
                 source_quality="low_res_scan"):
    import shapely
    from shapely.geometry import mapping
    g = shapely.set_precision(ad.km_geom_to_lonlat(geom.simplify(0.03)), 1e-5)
    g = shapely.orient_polygons(shapely.make_valid(g))
    if g.geom_type == "GeometryCollection":
        g = shapely.union_all([x for x in g.geoms if x.geom_type in ("Polygon", "MultiPolygon")])
    props = {"id": fid, "kind": "area", "snapshot": snapshot, "side": side, "holder": holder, "neutral_kind": kind,
             "accuracy": accuracy, "source_quality": source_quality, "sources": sources or [ISA_MAP["url"]], "checked": CHECKED, "status": "draft",
             "review": "owner-verify"}
    props.update(extra or {})
    return {"type": "Feature", "properties": props, "geometry": mapping(g)}


def build(isa_pdf: Path, cia_pdf: Path, log=print) -> dict:
    import shapely
    from shapely.geometry import LineString, mapping
    a = load_scan(isa_pdf)
    fit = georeference(a, log)
    log("tracing the drawn lines:")
    traced = trace_all(a, log)
    land_all = ad.mandate_land()
    land_n = land_all.intersection(km_box(0, SOUTH_N, 400, 400))
    feats, snaps, anchors = [], {}, {}
    # the drawn lines, as traced (true lines); the CIA's after the snapshots
    for name, t in traced.items():
        he, en, d = LEGEND[t["colour"]]
        g = shapely.set_precision(ad.km_geom_to_lonlat(stroke_km(fit, t["px"]).simplify(0.02)), 1e-5)
        feats.append({"type": "Feature", "properties": {"id": name, "kind": "line", "colour": t["colour"], "legend_he": he,
                      "legend": en, "date": d, "on_ink": t["on_ink"], "sources": [ISA_MAP["url"]], "checked": CHECKED,
                      "status": "draft", "review": "owner-verify"}, "geometry": mapping(g)})
    spans = {"opening": ("1948-05-15", "1948-06-11"), "truce1": ("1948-06-11", "1948-07-08"), "truce2": ("1948-07-18", "1948-07-24"),
             "truce2b": ("1948-07-26", "1948-10-15"), "october": ("1948-10-31", "1949-02-24")}
    for sid, (names, drops, drawn, shown) in SNAPSHOTS.items():
        log(f"snapshot {sid} (lines as drawn {drawn}, shown from {shown}):")
        snaps[sid] = build_snapshot(fit, traced, land_n, names, drops, log)
        anchors[sid] = check_anchors(snaps[sid], sid, *spans[sid], log)
        check_holders(snaps[sid], sid, *spans[sid])
    # 15 May - 10 June 1948 (#674, option b): the 9 July polygons themselves (no line of their own)
    log("snapshot opening (the 9 July lines, adjusted; shown from 1948-05-15):")
    snaps["opening"] = snaps["truce1"]
    anchors["opening"] = check_anchors(snaps["opening"], "opening", *spans["opening"], log)
    if len(anchors["opening"]) < 20:
        raise SystemExit(f"opening: only {len(anchors['opening'])} anchors are sourced for 15 May - 10 June (20 wanted)")
    check_holders(snaps["opening"], "opening", *spans["opening"])
    log("the CIA's 18 May 1948 map:")
    ink = load_cia(cia_pdf)
    cfit = cia_georeference(ink, log)
    cia = cia_trace(ink, cfit, log)
    cia_area = cia_faces(snaps["truce1"], cia, land_n, log)
    fronts, arm_lines, arm_zones = armistice_features()
    prev_key = None; arm_ids = {}
    for d in ARMISTICE_DATES:
        s = build_armistice(d, fit, snaps["october"], fronts, arm_lines, land_all, land_n, log)
        key = shapely.to_wkb(shapely.normalize(s["areas"]["israel"].simplify(0.01)))
        if prev_key is not None and key == prev_key:
            arm_ids[d] = arm_ids[ARMISTICE_DATES[ARMISTICE_DATES.index(d) - 1]]
            log(f"armistice {d}: the same sides as the date before (fronts switched: {', '.join(s['switched'])})")
            continue
        sid = "arm_" + d
        arm_ids[d] = sid; snaps[sid] = s; prev_key = key
        nxt = ARMISTICE_DATES[ARMISTICE_DATES.index(d) + 1] if d != ARMISTICE_DATES[-1] else None
        log(f"armistice {d}: fronts switched {', '.join(s['switched'])}; closures {', '.join(s['closures']) or 'none'}")
        anchors[sid] = check_anchors(s, sid, d, nxt, log, 3.0 if "jordan" in s["switched"] else 2.0)
        check_holders(s, sid, d, nxt)
    for name, c in cia.items():
        g = shapely.set_precision(ad.km_geom_to_lonlat(c["km"]), 1e-5)
        feats.append({"type": "Feature", "properties": {"id": name, "kind": "line", "colour": "cia_tenuous",
                      "legend": "Border of tenuously-held Jewish area", "date": "1948-05-18", "on_ink": c["on_ink"],
                      "sources": [CIA_MAP["url"]], "checked": CHECKED_OPENING, "status": "draft", "review": "owner-verify"},
                      "geometry": mapping(g)})
    # overlays: contested during operations, no colour at the uncertain Egypt closure, demilitarized zones
    south = km_box(0, 0, 400, 160); north = km_box(0, 200, 400, 400)
    ten = xor_israel(snaps["truce1"], snaps["truce2"])
    shoter = xor_israel(snaps["truce2"], snaps["truce2b"])
    octx = xor_israel(snaps["truce2b"], snaps["october"])
    mid = octx.difference(south).difference(north)
    if mid.area > 0.5:
        raise SystemExit(f"the October changes include {mid.area:.1f} km2 outside the Yoav (south) and Hiram (north) areas")
    ops = OPERATIONS
    overlays = {   # id: (geometry, kind, (from, until), sources, accuracy, source_quality, why)
        "contested_ten_days": (ten, "contested", ops["ten_days"][:2], [ISA_MAP["url"]] + ops["ten_days"][2], ISA_ACCURACY, "low_res_scan",
                               "the Ten Days: where the 9 July and 18 July lines differ"),
        "contested_shoter": (shoter, "contested", ops["shoter"][:2], [ISA_MAP["url"]] + ops["shoter"][2], ISA_ACCURACY, "low_res_scan",
                             "Operation Shoter: Ijzim, Jaba and Ein Ghazal"),
        "contested_yoav": (octx.intersection(south), "contested", ops["yoav"][:2], [ISA_MAP["url"]] + ops["yoav"][2], ISA_ACCURACY, "low_res_scan",
                           "Operations Yoav and Ha-Har and the actions that followed them to the end of October: where the July and October lines differ in the south"),
        "contested_hiram": (octx.intersection(north), "contested", ops["hiram"][:2], [ISA_MAP["url"]] + ops["hiram"][2], ISA_ACCURACY, "low_res_scan",
                            "Operation Hiram: the Galilee pocket"),
    }
    # 15 May - 10 June 1948 (#674): grey where the CIA's dashed border closes (traced) or a source dates a change inside a 9
    # July Israeli area (cut by its sub-district), the south without colour (cut by its sub-districts)
    win = ("1948-05-15", "1948-06-11")
    t1_israel = snaps["truce1"]["areas"]["israel"]
    opening = {"contested_opening_hula": (cia_area["cia_hula"]["area"], "contested", win,
                                          [ISA_MAP["url"], CIA_MAP["url"]] + WIKI_OPENING["hula"], CIA_ACCURACY, "low_res_scan",
                                          "the Safad-Hula loop that the CIA's 18 May map draws as a dashed (tenuously held) border, "
                                          "from the frontier near Nabi Yusha down to Safad and east to the Jordan (traced on the "
                                          "scan); its other edges are the 9 July line and the frontier")}
    for oid, (key, why, srcs) in OPENING_CUTS.items():
        opening[oid] = (subdistrict_km(key).intersection(t1_israel), "contested", win, [ISA_MAP["url"]] + srcs,
                        ISA_ACCURACY, "low_res_scan", why)
    south = shapely.union_all([subdistrict_km(k) for k in OPENING_SOUTH]).intersection(land_all)
    opening["unsourced_south_opening"] = (south, "not_sourced", win, [ISA_MAP["url"], CIA_MAP["url"]] + WIKI_OPENING["south"],
                                          "the Gaza and Beersheba sub-districts (D-B)", None,
                                          "the south: the CIA's 18 and 25 May maps draw no Jewish-held area beyond the partition "
                                          "boundary and its 11 June map labels the Negev 'Arab controlled', while ISA 6158/1's 9 "
                                          "July line draws an Israeli block; the settlements were islands among Arab localities, with "
                                          "the Egyptian army moving north from 15 May. The sources disagree: no colour, the Jewish "
                                          "settlements' hexes blue (pinned_towns; owner ruling Q3, #674)")
    for oid, o in opening.items():
        if o[0].is_empty:
            raise SystemExit(f"{oid}: empty")
    overlays = {**opening, **overlays}
    first_egypt = next(sid for d, sid in arm_ids.items() if "egypt" in snaps[sid]["switched"])
    # the uncertain band is the Israeli side's only: the area the Egypt line and the frontier already give Egypt stays Egypt's
    unc = shapely.union_all([shapely.buffer(u, CLOSURE_UNCERTAIN_KM) for u in snaps[first_egypt]["uncertain"]]).intersection(land_all)
    unc = unc.difference(snaps[first_egypt]["areas"]["arab"])
    overlays["unsourced_egypt_south"] = (unc, "not_sourced", ("1949-02-24", None), [ARMISTICE.name, ad.UNTS["url"], ad.EGYPT_TEXT["url"]],
                                         "the closing stretch is a straight line within Art. VI.3's bounds (about +-3 km); this band, "
                                         "on the Israeli side of it, is left without colour", None, CLOSURES["egypt_south"])
    for z in arm_zones:
        overlays["dmz_" + z["id"]] = (z["geom"].intersection(land_all), "dmz", (z["from"], None), [ARMISTICE.name, ad.UNTS["url"]],
                                      z["accuracy"] or ARMISTICE_ACCURACY, z["source_quality"], z["name"])
    # features
    for sid, s in sorted(snaps.items(), key=lambda kv: kv[0] != "opening"):
        date = spans.get(sid, (sid[4:], None))[0]
        arm = sid.startswith("arm_")
        legend = "armistice" if arm else "held"
        kw = {"sources": [ARMISTICE.name, ad.UNTS["url"], ISA_MAP["url"]], "accuracy": ARMISTICE_ACCURACY} if arm else {}
        # D-A ruling 2: only what the Jordan line bounds is low accuracy: Jordan's side, and the Israeli side along it
        jordan_in = arm and "jordan" in s["switched"]
        israel_flag = {"low_accuracy": "partly: its boundary with Jordan's side" if jordan_in else False} if arm else {}
        feats.append(area_feature(f"{sid}:israel", sid, "israel", s["areas"]["israel"], "israel", extra={"legend": legend, **israel_flag}, **kw))
        for h, g in sorted(split_holders(s["areas"]["arab"], date).items()):
            flag = {"low_accuracy": jordan_in and h == "jordan"} if arm else {}
            feats.append(area_feature(f"{sid}:arab:{h}", sid, "arab", g, h, extra={"legend": legend, **flag}, **kw))
        if sid == "opening":
            for f in feats[-(1 + len(split_holders(s["areas"]["arab"], date))):]:
                f["properties"]["checked"] = CHECKED_OPENING
    for oid, (g, kind, (f, u), srcs, acc, sq, why) in overlays.items():
        if g.is_empty:
            continue
        side = "unsided" if kind == "not_sourced" else "neutral"
        feat = area_feature(oid, None, side, g, None, kind, {"shown_from": f, "shown_until": u, "why": why}, sources=srcs,
                            accuracy=acc, source_quality=sq)
        if oid in opening:
            feat["properties"]["checked"] = CHECKED_OPENING
        feats.append(feat)
    periods = [
        {"id": "opening", "from": "1948-05-15", "until": "1948-06-11", "snapshot": "opening",
         "overlays": ["unsourced_south_opening", "contested_opening_hula", "contested_opening_jenin", "contested_opening_jerusalem"],
         "legend": "adjusted", "line_date": "1948-07-09", "jerusalem": "no_mans_land",
         "why": "owner decision of 16 October 2026 (#674, option b, '9 July lines, adjusted'): no line for 15 May can be traced "
                "honestly, so the 9 July line with grey where the front moved before 11 June or the CIA's 18 May map shows a "
                "tenuous hold, the south without colour (its settlements blue), Jerusalem no-man's land (research note "
                "control-15-may-1948.md section 9)"},
        {"id": "truce1", "from": "1948-06-11", "until": "1948-07-08", "snapshot": "truce1", "overlays": [], "legend": "held",
         "line_date": "1948-07-09", "why": "the first truce: the line ISA 6158/1 dates 9 July 1948, the closest sourced line (owner ruling 2026-10-14)"},
        {"id": "ten_days", "from": "1948-07-08", "until": "1948-07-18", "snapshot": "truce1", "overlays": ["contested_ten_days"], "legend": "held", "line_date": "1948-07-09"},
        {"id": "truce2", "from": "1948-07-18", "until": "1948-07-24", "snapshot": "truce2", "overlays": [], "legend": "held", "line_date": "1948-07-18"},
        {"id": "shoter", "from": "1948-07-24", "until": "1948-07-26", "snapshot": "truce2", "overlays": ["contested_shoter"], "legend": "held", "line_date": "1948-07-18"},
        {"id": "truce2b", "from": "1948-07-26", "until": "1948-10-15", "snapshot": "truce2b", "overlays": [], "legend": "held", "line_date": "1948-07-18"},
        {"id": "yoav", "from": "1948-10-15", "until": "1948-10-29", "snapshot": "truce2b", "overlays": ["contested_yoav"], "legend": "held", "line_date": "1948-07-18"},
        {"id": "yoav_hiram", "from": "1948-10-29", "until": "1948-10-31", "snapshot": "truce2b", "overlays": ["contested_yoav", "contested_hiram"], "legend": "held", "line_date": "1948-07-18"},
        {"id": "october", "from": "1948-10-31", "until": "1949-02-24", "snapshot": "october", "overlays": [], "legend": "held", "line_date": "1948-10",
         "why": "the October line stands until the Egypt agreement: no source gives a later line (Operation Horev is not drawn)"},
    ]
    for i, d in enumerate(ARMISTICE_DATES):
        sid = arm_ids[d]; s = snaps[sid]
        ovs = ["unsourced_egypt_south"] + [o for o in overlays if o.startswith("dmz_") and overlays[o][2][0] <= d]
        periods.append({"id": "arm_" + d, "from": d, "until": ARMISTICE_DATES[i + 1] if i + 1 < len(ARMISTICE_DATES) else None,
                        "snapshot": sid, "overlays": ovs, "legend": "armistice" if all(fd and fd <= d for fd in fronts.values()) else "mixed",
                        "fronts": sorted(k for k, fd in fronts.items() if fd and fd <= d), "line_date": "1948-10"})
    meta = {
        "title": "Who held which areas, 1948-49: the front lines of ISA 6158/1 and, from each agreement's date, the sides of the 1949 armistice lines (true polygons, not hex-snapped)",
        "status": "draft", "review": "owner-verify (sensitivity review before a shareable build)", "checked": CHECKED,
        "generated_by": "tools/control_digitise.py",
        "licence": "CC-BY-SA-4.0 (content/); facts redrawn by our own method from the sources below, no image copied",
        "meaning": "Military control on a date (owner ruling 2026-10-14), nothing else: not sovereignty, ownership or population. From each armistice the side of the agreement's line, which the agreements themselves say is not a political or territorial boundary (Egypt Art. V.2; Jordan Art. VI.9).",
        "sources": FACT_SOURCES + [{"cite": "content/map/armistice_lines_1949.geojson (D-A): the 1949 lines and zones", "url": ARMISTICE.name, "checked": CHECKED}],
        "searched_for_a_better_copy": ISA_SEARCH,
        "legend_of_the_source": {k: {"he": v[0], "en": v[1], "date": v[2]} for k, v in LEGEND.items()},
        "not_traced": ["the partition line (teal) and the Acting Mediator's demanded withdrawal line (yellow): neither is a control line",
                       "the small circles the map draws in red (Kfar Darom, 'Iraq Suweidan and its neighbour): not in the map's legend",
                       "the blue strokes under 1.5 km from the red in the Hula valley and at Ein Gev (below the accuracy)",
                       "the Syrian and Israeli truce lines of S/1353/Rev.1: on its colour scan the Israeli line is legible only in parts and the Syrian line cannot be told apart from the zone's outline and the river at that resolution; ISA 6158/1's red and blue cover that front",
                       "the 1950 modus vivendi line: it does not change either side's position (S/1471 Art. III)",
                       "the CIA 18 May 1948 map's strongly-held (solid) border: on the greyscale scan it cannot be told from the "
                       "partition boundary, the roads and the railway (research note control-15-may-1948.md section 9)",
                       "the CIA 18 May map's dashed (tenuously held) border along the Jerusalem corridor: legible, but a single line "
                       "along the road that closes on no sourced line; its area is cut by the Jerusalem sub-district instead",
                       "the CIA 18 May map's dashed (tenuously held) border by Rishon-Sarafand: legible, but it runs into the "
                       "lettering by Jaffa and closes on no sourced line; not drawn (owner ruling 2 of 16 October 2026 on #686: in "
                       "the Ramle sub-district only Yibna's hex is grey, Rehovot and Rishon stay blue)",
                       "the CIA 18 May map's dashed loop by the north Dead Sea works: not legible on this copy (one dash, then the "
                       "'Palestine Potash Ltd' lettering); the area keeps the 9 July line's side",
                       "the CIA 18 May map's dashed border from Ramle to Latrun: within about 1.5 km of the 9 July line all the way "
                       "(below both maps' accuracy)"],
        "south_limit_km_n": SOUTH_N,
        "known_staleness": ["the October 1948 line is shown until the Egypt agreement (24 February 1949), since no source gives a later line: Isdud (taken 28 October), Majdal (4-5 November) and Yad Mordechai stay on the other side until then; the caption names the source line's date",
                            "15 May - 10 June 1948 is one period on the 9 July lines (owner ruling Q4, #674): changes inside it are not "
                            "shown by day: Atarot (17 May) and Neve Yaakov (18 May) evacuated, the Kinarot valley fighting (18-22 "
                            "May), Kalia and Beit HaArava abandoned (20 May), Yad Mordechai fallen (24 May; its hex stays blue), RAF "
                            "Ramat David handed over (26 May), the Jewish Quarter surrendered (28 May), Isdud taken by the Egyptian "
                            "army (29 May), Nitzanim fallen (7 June), Mishmar HaYarden taken by the Syrian army (10 June); and the "
                            "British evacuation enclave at Haifa (to 30 June 1948) is not drawn (owner ruling Q5: an info-panel note)",
                            "the south in 15 May - 10 June 1948: only the Jewish settlements whose 1948 site the repository holds are "
                            "pinned blue (Yad Mordechai, Negba, Nir Am, Dorot, Shoval, Mishmar HaNegev, Hatzerim, Nevatim); Nitzanim, "
                            "Kfar Darom, Nirim, Be'erot Yitzhak and others are not, their sites having moved after 1948 with no 1948 "
                            "site sourced here; Revivim, south of the 60-km northing, stays without colour as in every 1948 period",
                            "Huleiqat: ISA 6158/1's lines put it on the Israeli side from 9 July 1948, but en.wiki has Egypt retake it on 8 July and Israel capture it on 19 October 1948 (Operation Yoav); its hex is pinned to Egypt for 8 July - 19 October (pinned_towns). A known limit of the periods: the pin covers whole periods inside its window, so the Yoav period (15-29 October) shows the hex Israeli from 15 October, four days before its capture"],
        "contested_places": [{"place": n, "lon": x, "lat": y, "from": "1948-05-15", "until": "1948-06-11", "sources": src, "why": w}
                             for n, x, y, src, w in OPENING_CONTESTED],
        "pinned_towns": [{"place": n, "lon": x, "lat": y, "holder": h, "from": f, "until": u, "source": src, "why": w} for n, x, y, h, f, u, src, w in PINNED],
        "georeference": fit["diag"],
        "georeference_cia": cfit["diag"],
        "conversion": "Palestine Grid (EPSG:28191) to WGS84 by the EPSG Helmert transformation (tools/armistice_digitise.py, PG_PIPELINE)",
        "drops": {sid: [{"stroke": s, "px": [x, y], "why": w} for s, x, y, w in d] for sid, (_, d, _, _) in SNAPSHOTS.items()},
        "closures": CLOSURES,
        "joins": {**{sid: s["joins"] for sid, s in snaps.items()}, **{n: c["joins"] for n, c in cia_area.items()}},
        "operations": {k: {"from": f, "until": u, "sources": src} for k, (f, u, src) in OPERATIONS.items()},
        "anchors": anchors,
        "periods": periods,
    }
    return {"type": "FeatureCollection", "statecraft": meta, "features": feats}


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--isa-map", type=Path, required=True, help="the NYU copy of ISA 6158/1 (R0005czg.pdf)")
    ap.add_argument("--cia-map", type=Path, required=True, help="the CIA 18 May 1948 summary (CIA-RDP78-01617A004700010003-6.pdf)")
    ap.add_argument("--out", type=Path, default=OUT)
    args = ap.parse_args()
    doc = build(args.isa_map, args.cia_map)
    ad.write(doc, args.out)
    print(f"wrote {args.out} ({len(doc['features'])} features)")


if __name__ == "__main__":
    main()
