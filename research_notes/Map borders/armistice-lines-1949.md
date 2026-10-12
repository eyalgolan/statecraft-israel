# The 1949 armistice lines: sources and how to digitise them (research note, 2026-10-11)

Status: built for all four fronts (below, status draft); Jordan at low accuracy from a low-resolution scan. The lines are dated and sensitive: before a shared build shows them they
need #61-style dating (below) and the owner's sensitivity review (chapters design §13; `lines.yaml`, chapters design
§10). Owner rulings for this work (2026-10-11): the real lines, from the four General Armistice Agreements and their
annexed maps; never a later or simplified "Green Line" drawing as the source (one may be used as a cross-check, and is
then named as such); true lines stored, hex-snapped only for display; no invented boundaries.

Companion: the 1948 sub-districts (`content/map/subdistricts_1948.geojson`, `tools/borders_digitise.py`), done this
round from Mandate sources. Mandate maps are *not* a source for any armistice line.

## Built (2026-10-11, branch armistice-lines, issue #196)

`tools/armistice_digitise.py` -> `content/map/armistice_lines_1949.geojson` (true lines, status draft; per-feature
sources, check dates, accuracy, method, diagnostics); `tools/armistice_bake.py` -> `content/map/armistice_hexes_1949.yaml`
(hex-snapped); `tools/borders_bake.py` writes each front's end into the sub-district layer. In the game, `MapRegions`
draws the zones over the sub-districts from each agreement's date and the info panel names the line and agreement.

| Front (from) | What is stored | Accuracy |
|---|---|---|
| Syria (20 Jul 1949) | The line through Annex I's 24 MR points (typed from UNTS vol. 42 pp. 342-344); "a line" stretches straight, the "along ..." stretches traced on the annexed map's **green** line (the legend: green = ADL, red = defensive area, black = Israeli truce line, brown = Syrian truce line, purple hatch = DMZ) in the **UN ODS colour scan of S/1353/Rev.1** (PDF page 14, 3165 x 8759 px), registered on the Annex points (median 35 m, max 125 m). The DMZ in three parts (4.1, 27.3, 33.8 km2; 65.2 in all) from the hatching of the UNISPAL 1-bit scan, georeferenced on its 1-km grid (rms 11-14 m). | +-0.1 km at the points, +-0.1-0.2 km traced; where the zone edge is the line, the two scans agree to a median of 12-98 m. |
| Lebanon (23 Mar 1949) | Art. V.1's international boundary as the sub-district data's Mandate frontier, Ras en Naqura to the tripoint (its end is 0.55 km from Annex I.1's tripoint). | about +-1 km (1:750,000 source), +-1.5 km at the uncertain segments. |
| Egypt (24 Feb 1949) | Art. VI.1 from the Wadi Hasi mouth through Deir Suneid to 3 km east of the Gaza-Al Majdal highway and along the offset to Gaza, on the Survey of Palestine 1:100,000 sheet 9 "Gaza" (10-km grid, rms 6-8 m). The El Auja DMZ from Art. VIII.2's four MR points, closed along the Mandate frontier. | about +-0.3 km; cross-check: the signed 1950 modus vivendi map S/1471 draws its demarcation line on this one (within about 0.1 km, MR 103-107 / 100.2-102.8). El Auja +-0.1 km at the points, +-1 km on the frontier. |
| Egypt, 1950 (22 Feb 1950) | The demarcation line as drawn on the signed modus vivendi map (S/1471, 1:20,000, sheet 1 "Ascalon" on UNISPAL, sheet 2 "Khan Yunis" in the UN Treaty Collection), through waypoints read on the dash-dot every 0.5-1 km where the map draws it (not routed: on a 1-bit scan routing follows the neatline and grid, review C-1), and Art. I.1's zone A and C lines from their MR points. Its own feature (controller ruling 2026-10-11). | about +-0.1 km (georeference rms 12-20 m). The map draws the line only in pieces (gaps recorded). |
| Jordan (3 Apr 1949) | **Low accuracy** (owner ruling 2026-10-11): traced from UNISPAL's original upload of the signed map 1 (3119 x 8630 px, about 51 m a pixel), georeferenced on 30 town symbols (Wikidata coordinates; the printed grid on this copy put the towns 5-8 km off and is not used). Line A (the Iraqi sector) split into Art. VI.3's stages, each shown from its deadline (by 8 May, 22 May, 17 Jul 1949, a convention), and cut at Kh Deir Arab's MR 1510-1574; sector (a) Kh Deir Arab-Latrun-Jerusalem, Jerusalem-Dead Sea (line B, to MR 1925-0958), Dead Sea-Aqaba (d) from 3 Apr. `source_quality: low_res_scan`. | about +-1-1.5 km north (leave-one-out rms 0.77 km, max 1.44), +-1.5-2.5 km south (1.47, max 2.61), up to about 3 km at the Gulf of Aqaba; the pen line is 250 m wide. Check: south of the Dead Sea, where the line followed the Mandate frontier, the trace lies a median 0.42 km (max 3.1 km, at the Gulf) from it. |

Gaps and findings:
- **Jordan:** no full-resolution scan of the signed map 1 or map 2 was found. Searched (2026-10-11): UNISPAL (the page
  shows a 925 x 2560 px copy; its original upload, 3119 x 8630 px, was found and is the source, at low accuracy), UN ODS
  (S/1302, S/1302/Rev.1: text only), UNTS vol. 42 (blank insert page), the UN Archives catalogue (needs a JavaScript
  check), Wikimedia Commons (six API searches), NLI and ECF (JavaScript catalogues, to request). Commons has a 3119 x 8630
  px colour copy of map 1 ("File:Green Line - Cease Fire Agreement 30 Nov 1948.jpg", "own work", CC0, no source; not the
  same file as UNISPAL's): not used. Not legible on UNISPAL's copy, so left out: Jerusalem (map 2 is not on UNISPAL), the
  double lines and no-man's-land at Latrun (the trace takes the outer loop) and in Jerusalem, Mount Scopus. The full list
  is in the GeoJSON's `fronts`.
- **Egypt south of Gaza, 1949:** not drawn (from 22 Feb 1950 the modus vivendi line is). Art. VI.1 ends "continuing thus to the Egyptian frontier"; Art. VI.3 has Israel
  evacuate its Cemetery outpost (MR 0816-0723) and allows a new one at MR 0836-0700, so the line meets the frontier
  south-east of Rafah (about N 70-72), which neither the highway's bearing nor a coast-road offset reaches. The Egyptian-held
  area is not drawn for the same reason. IBS 46 (US State Department, 1965) says the Gaza Strip sector "dates from February
  22, 1950" (the modus vivendi); the 1949 line there needs the Memorandum's map.
- **The Memorandum of 13 November 1948** is not online: S/1086 refers to it, and the UN Archives hold it (fonds AG-022,
  "Memorandum of 13 November and Note of Transmittal (Part III) (re Negev)"). Its para. 2.B(i) is quoted in Art. VI.1.
- **The Syrian DMZ** is not only "between the line and the boundary": Art. V.5 adds the (Israeli-held) Ein Gev and Dardara
  sectors, so the zone lies on both sides of the line there; the map's hatching, not a construction, is the source.
- **Snapped at 5-km hexes**, the northern and southern Syrian DMZ parts contain no hex centre (listed as `no_hex`); they
  are in the true lines only. El Auja takes 5 hexes, the central Syrian part 1.

## The documents

All four agreements are registered in the **United Nations Treaty Series, vol. 42 (1949)**
(https://treaties.un.org/doc/publication/UNTS/Volume%2042/v42.pdf, read 2026-10-11; the volume's contents list gives
the numbers and pages below). Its text says the Jordan maps are an insert "between pages 326 and 327"; the online PDF
has a blank page there (checked): the map plates are not in it.

| Agreement | Signed | UN document (text) | UNTS | How the line is defined | Annexed map(s) |
|---|---|---|---|---|---|
| Egypt-Israel | 24 Feb 1949, Rhodes | S/1264/Corr.1 (S/1264/Rev.1 in later UN papers) | No. 654, vol. 42, p. 251 | **Text.** Art. VI.1: in the Gaza-Rafah area, the line of para. 2.B(i) of the Memorandum of 13 November 1948: from the coast at the mouth of the Wadi Hasi east through Deir Suneid, across the Gaza-Al Majdal highway to a point 3 km east of it, then south parallel to the highway to the Egyptian frontier. Art. VI.3 fixes Israeli outposts by military reference (MR) points (e.g. Deir Suneid MR 10751090). Art. VI.4 leaves the Bethlehem-Hebron area to a later agreement. **El Auja demilitarised zone**, Art. VIII.2, by MR points: frontier 5 km NW of the Rafah-El Auja road crossing (MR 08750468), Khashm El Mamdud (MR 09650414), Hill 405 (MR 10780285), frontier 5 km SE of the old railway crossing (MR 09950145), back along the frontier. | Annexes I-II are a withdrawal plan and force definitions; whether a line map was annexed is **to verify** in the S/1264 PDF on UNISPAL. A later **modus vivendi** (El Auja, 22 Feb 1950, S/1471) adds a map with zones A, B and C in the Gaza-Rafah area (local arrangement; "does not change either party's position on the line"). |
| Lebanon-Israel | 23 Mar 1949, Ras en Naqura | S/1296 | No. 655, vol. 42, p. 287 | **Text.** Art. V.1: "The Armistice Demarcation Line shall follow the international boundary between the Lebanon and Palestine." | None for the line (the annex defines defensive forces). The international boundary itself is the 1923 Mandate line, marked by boundary pillars; its geometry comes from Mandate-era survey sheets and the pillar list, which is a Mandate source for a line the 1949 text adopts by reference (state it so). |
| Jordan-Israel | 3 Apr 1949, Rhodes | S/1302/Rev.1 | No. 656, vol. 42, p. 303 | **Maps.** Art. V.1: "as delineated on the maps in annex I": (a) Kh Deir Arab (MR 1510-1574) to the north end of the Jerusalem lines: the truce lines certified by UNTSO (map 1, the red and green lines); (b) Jerusalem: the lines of the **30 November 1948 Cease-Fire Agreement** (map 2); (c) Hebron-Dead Sea: map 1, line "B"; (d) Dead Sea (MR 1925-0958) to the southern tip: positions surveyed by UN observers in March 1949, map 1 (the blue line). Art. VI: the **Iraqi sector**, map 1 line "A", established **in stages** (see dating). | Annex I: **map 1**, the Survey of Palestine 1:250,000 "Palestine" north and south sheets (layered; compiled 1944, roads revised to January 1946, reprinted April 1946, per the legend on the UNISPAL image), the lines drawn by hand in colour and signed by the delegations; **map 2**, a survey map of Jerusalem. UNISPAL holds an image of map 1: https://www.un.org/unispal/document/auto-insert-181069/ (the page shows a 925 x 2560 px copy; its original upload, 3119 x 8630 px, about 51 m a pixel, is the source used at low accuracy (2026-10-11); a full-resolution scan is still to find: the UN Archives, the Israel State Archives, ECF's "Original Green Line Map" item https://ecf.org.il/media_items/338). |
| Syria-Israel | 20 Jul 1949, Hill 232 near Mahanayim | S/1353 (map: S/1353/Rev.1; S/1353/Add.2 reportedly carries Annex I, to verify) | No. 657, vol. 42, p. 327 | **Text with grid references.** Annex I lists the line point by point in Palestine-grid MR coordinates to 0.1 km, e.g. "1. From the point where the Lebanese-Syrian-Palestinian border meets MR 208.7-294.2, eastward to MR 212.8-294.7 ... 9. Westward to MR 209.7-272.6, a point on shoreline of Lake Hula ... 18. ... southward along the Syrian-Palestinian border to BP 61, MR 210.6-246.3", with stretches "along the Wady", "along shoreline", "along the Syrian-Palestinian border". **Demilitarised zone** (Art. V.5): where the line departs from the international boundary, the area between them; the map shows three zones (around Banias/Dan, Mishmar HaYarden-Hula, and Ein Gev-El Hamma). | "Map referred to in Annexes I, II and III", scale 1:50,000 (sheet "Houlé", Palestine grid ticks every 2 km, DMZ hatched): UNISPAL TIF, https://www.un.org/unispal/document/auto-insert-183273/ (6176 x 11666 px, 1-bit; downloaded and viewed 2026-10-11, not committed). |

## Copyright and licence

- The agreement texts and their annexed maps were issued as **UN documents with symbols** (S/1264..., S/1296, S/1302/Rev.1,
  S/1353...). Under UN Administrative Instruction ST/AI/189/Add.9/Rev.2 (para. 2) such documents are not copyrighted;
  Wikimedia Commons treats them as public domain ({{PD-UN-doc}}, maps {{PD-UN-map}}). UNTS texts likewise.
- Jordan map 1 is drawn on a **Survey of Palestine** base map (1946): the base is a Government of Palestine work, the
  same status as the 1948 sub-districts' source (decision D-B: public domain in Israel and the UK; a URAA-restored US
  copyright in the drawing is possible). We take only the hand-drawn lines' positions (facts of an international
  agreement), not the base map.
- Any third-party digitisation (OpenHistoricalMap, CC0; academic GIS layers; ECF) is used **only as a cross-check**, and
  its licence is recorded where used; none is a source.

## How each line can be digitised, and the accuracy to expect

| Line | Method | Expected accuracy |
|---|---|---|
| Syria (and its DMZs) | Type the Annex I MR points; convert from the **Palestine Grid, EPSG:28191** (Palestine 1923 / Palestine Grid, Cassini-Soldner; MR values are km eastings and northings with no false-northing offset; EPSG:28193 is the Israeli CS Grid, which adds 1,000 km to northings, and is wrong here) to WGS84. Checked with pyproj 3.8.0 on 2026-10-11: Syria Annex I MR 208.7-294.2 -> 33.243 N 35.626 E (the Lebanon-Syria-Palestine tripoint); El Auja MR 0875-0468 -> 31.009 N 34.346 E; Jordan Art. V MR 1925-0958 -> 31.454 N 35.447 E (in the Dead Sea, about 5 km off its west shore: the text's "a point on the Dead Sea"); follow "along the Wady / shoreline / border" stretches on the annexed 1:50,000 map (georeferenced on its own grid ticks) and on the Mandate boundary for border stretches. DMZ polygons = between this line and the international boundary. | Points: +-0.1 km (the MR precision) plus datum shift (tens of metres). Traced stretches: +-0.1-0.2 km at 1:50,000. |
| Egypt: Gaza-Rafah line | Text: the Wadi Hasi mouth, Deir Suneid, the highway +3 km offset line, to the frontier; MR points for outposts. Needs the 13 Nov 1948 Memorandum and any annex map (to verify); the 1950 modus vivendi map for the later zones. | Points +-0.1 km; the "3 km east of the highway, parallel" line depends on which 1948 road alignment is meant: +-0.5 km until a map is found. |
| Egypt: El Auja DMZ | MR points in Art. VIII.2 plus the 1906 Egypt-Palestine frontier. | +-0.1 km (points); the frontier from Mandate sheets. |
| Lebanon | The 1923 international boundary (boundary pillars) from Mandate survey sheets (1:20,000/1:100,000), as Art. V.1 adopts it. | +-0.1-0.3 km. |
| Jordan (map 1, 1:250,000) | A full-resolution scan of the signed map; georeference on its Palestine grid (it is printed on the sheet); trace each coloured line (red/green truce lines, "A" Iraqi sector, "B" Hebron-Dead Sea, blue southern line). | The pen line on a 1:250,000 sheet is 1-2 mm wide: **+-0.25-0.5 km**; the scan and georeference add +-0.1 km. |
| Jordan: Jerusalem (map 2) | The 30 Nov 1948 cease-fire map at city scale; Israeli and Jordanian lines and the no-man's-land between them. The commanders drew it in thick wax pencil (Israel MFA, "Divided Jerusalem (1948-1967)", https://embassies.gov.il/MFA/AboutIsrael/Maps/Pages/Jerusalem%20Before%20the%20Six%20Day%20War%20-1949-1967-.aspx), so each line is a band tens of metres wide on the ground. | +-30-60 m (the pencil width), and the band itself is a fact to keep (two lines, not one). |

## Special zones (each a separate, dated feature, never folded into one line)

- **Jerusalem:** two lines (Israeli and Jordanian forward lines) with **no-man's-land** between them, from the 30 November
  1948 cease-fire as adopted by Art. V.1(b); the map is map 2 of the Jordan agreement.
- **Mount Scopus enclave:** the Agreement for the Demilitarization of the Mount Scopus Area, **7 July 1948** (UN-supervised;
  Hadassah Hospital, the Hebrew University, Augusta Victoria and Issawiya; its own map, not yet found), reportedly modified by
  an annex of 30 November 1948 (secondary sources only: **to verify** against the 30 November 1948 cease-fire texts). An Israeli-held enclave inside the Jordanian lines until 1967. Text:
  https://jewishvirtuallibrary.org/agreement-for-the-demilitarization-of-the-mount-scopus-area (to replace with the UN
  copy).
- **Latrun no-man's-land:** the area between the two lines of map 1 around Latrun (held by neither party); its outline is
  the two lines, so it comes from map 1, not from a later drawing.
- **Syrian DMZs:** three zones between the line and the international boundary (Art. V.5), on the annexed 1:50,000 map.
- **El Auja DMZ:** Art. VIII.2 points (above); later violations are history, not geometry.
- **Gaza-Rafah zones A, B, C:** modus vivendi of 22 February 1950 (S/1471), local and later than the agreement.

## What #61-style dating needs (`lines.yaml`, chapters design §10)

Every feature carries `from`, optional `until`, sources with check dates, and `status: fact-checked` (the owner sets
`sensitivity-reviewed`). Dates the texts already fix:

- Egypt line: from **24 Feb 1949**; the El Auja DMZ from the same date; the 1950 zones from **22 Feb 1950**.
- Lebanon: from **23 Mar 1949** (withdrawal to the line and reduction to defensive strength "within ten days of the signing", Art. V.3, read in UNTS vol. 42 No. 655 on 2026-10-11: a separate dated note).
- Jordan: from **3 Apr 1949** for sectors (a), (b), (d); the **Iraqi sector "A"** comes into force in stages (Art. VI.3):
  west of the Baqa-Jaljulia road and east of Kafr Qasim within five weeks (by **8 May 1949**), Wadi Ara north of the
  Baqa-Zubeiba line within seven weeks (by **22 May 1949**), the rest within fifteen weeks (by **17 July 1949**); until
  each stage the existing military line holds, so each stretch needs the earlier line until its date. Line "B"
  (Hebron-Dead Sea) is the stated offset for "A" (Art. VI.4).
- Syria: from **20 Jul 1949**; DMZ arrangements dated by their withdrawals (Annex: schedule of withdrawal).
- Later **Mixed Armistice Commission** adjustments (Jordan especially: local line changes and agreements recorded by the
  MACs in the 1950s) each need their own dated row; collecting them is a separate research task (UN MAC records,
  UNTSO reports in the S/ series).
- `until`: June 1967 for the Jordan, Syria and Egypt lines as game-relevant end dates; the owner decides how the game
  shows them after that (sensitivity).

## Open items for the owner

1. Approve this source plan, and the rule that a later Green Line drawing is a cross-check only.
2. Where to get full-resolution scans of the Jordan maps 1 and 2 (UN Archives request, Israel State Archives, ECF).
3. Names and terms for each line and zone (Israeli-perspective naming rule; the agreements' own terms: "Armistice
   Demarcation Line", "no-man's-land", "Demilitarized Zone").
4. Sensitivity review of each feature, and how lines after 1967 are shown.
