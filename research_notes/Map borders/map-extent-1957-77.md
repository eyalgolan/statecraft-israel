# The map's extent 1957 to 1977: Kilometre 101, the Hermon summit, the October 1973 enclave in Syria and Sharm el-Sheikh

Researched 24 October 2026. Every URL below was opened on that date by an agent. **Research only:** a note for issue #129
(1957-1977 item 17, part of #50), the research part the owner opened on 22 and 24 Oct. It gives coordinates, sources and
the distance to the map's edges for the four places the design lists as off today's map (design section 7.1). It does not
choose the map's edges, and it changes nothing under `content/`, `tools/`, `sim/` or `unity/`: the DEM and hex-grid
rebuild, `grid_version` and the save change stay paused, and the edges stay the owner's decision (1957-77 and 1977-93
design D9). A person's check of every source is still pending (CONTENT_POLICY section 2, item 6). The design is
[the 1957-1977 era design](../../docs/superpowers/specs/2026-10-10-era-1957-1977-design.md) (section 7.1, lines 481-498).

## Result in one table

Coordinates are WGS84 decimal degrees, north then east. "Margin" is the distance from the point to the nearest edge,
worked out in section 6 (negative = outside). Today's box is 34.20-35.90 E and 29.45-33.35 N (`content/map/hexes.json`
`bounds`, `tools/dem_to_heightmap.py` `BOUNDS`, both read 24 Oct); the design's proposed box is about 31.8-36.1 E and
27.7-33.5 N.

| Place | Coordinates | Source and tag | Margin |
|---|---|---|---|
| Kilometre 101, Cairo-Suez road | **Our reading** 30.095 N, 32.267 E (about +-0.01 degrees) | [S] Our reading on the IDF atlas map 72 (section 1.2); cross-check from a routing line 30.09 N, 32.22-32.32 E. [C] with the design's "about 31.9-32.0 E" (to verify). No source read prints a coordinate | 186 km west of today's west edge; 45 km inside the proposed west edge |
| Hermon summit | 33.41611 N, 35.8575 E | [S, Wikipedia] en.wiki Mount Hermon, oldid 1369568274. [C] with [S, Wikidata] Q215471, 33.4 N, 35.85 E (a point for the range) | 7.3 km north of today's north edge; 9.3 km inside a 33.5 N edge |
| Enclave of October 1973, farthest north and east | **Our reading** 33.4275 N, 35.9057 E (north) and 33.17 N, 36.14 E (east), +-3 km | [S] Our reading on a Wikimedia map of 24 Oct 1973 (section 3.3). Depth and size [C] across five secondary or primary statements (section 3.1) | East point is 22 km east of today's east edge and 3.7 km beyond a 36.1 E edge |
| Sharm el-Sheikh | 27.915 N, 34.3275 E | [S, Wikipedia] en.wiki Sharm El Sheikh, oldid 1380827798. [C] with [S, Wikidata] Q644, 27.8519 N, 34.305 E (7.3 km apart) | 170 km south of today's south edge; 24 km inside a 27.7 N edge (17 km on the Wikidata point) |

What the numbers say, in short: Km 101 is about 0.3 degrees east of where the design guessed, so the design's 31.8 E edge
leaves it 45 km of room, not a tight fit; the enclave's farthest east reading is the one place the proposed 36.1 E edge
may be short; Sharm el-Sheikh and Ras Muhammad fit inside 27.7 N with 17 to 24 km and 8 km of room.

## Conventions

The same as [procurement-1957-77.md](../Era%201957-77/procurement-1957-77.md) and
[forces-1967-73.md](../Era%201957-77/forces-1967-73.md):

- **[V, agent read 2026-10-24; a person's check pending]** read this session on a primary source opened: FRUS and UN
  documents. A US document is [V] for what the US government was told or wrote, not for the fact behind it. Written in
  full each time (CONTENT_POLICY section 2, item 6).
- **[S]** single or secondary source: press (JTA, even where the original bulletin page was read), Wikipedia and
  Wikidata at the revision given (`[S, Wikipedia]`, `[S, Wikidata]`), a map drawn by a later author, a database.
  Agreement of secondary sources is [S].
- **Our reading** a coordinate we worked out ourselves from a map or from two sources, with the method and the check
  shown. It is never [V]. The map's own accuracy is not known and is not claimed.
- **[C]** sources conflict; every value is kept.
- **[GAP]** not found in a source that could be opened. Every [GAP] says what was read or searched.
- No figure, name, page or quote was filled from memory. Quotes are copied from the page.
- **JTA dates.** The date on a JTA archive page is the bulletin date. The event date is read from the dateline on the
  item in the original bulletin PDF (OCR of the page images). Where the item's dateline could not be read, the row says
  so and gives only the bulletin date.
- **Distances** use 110.57 km per degree of latitude and 111.32 x cos(latitude) km per degree of longitude, the constants
  of `tools/geo.py`, with the cosine taken at the point's own latitude.
- **Negative searches** are in the search log (section 8) with the exact query, URL, date range and the day run.

## Source caveat

- **FRUS** read on history.state.gov (https://history.state.gov/historicaldocuments/<volume>/d<n>) on 24 Oct 2026 and
  searched in the Office of the Historian's TEI files. Volumes: 1964-68 XIX, 1969-76 XXV and XXVI. Citations give volume
  and document number. FRUS prints no map coordinates for any of the four places.
- **UN S/11302/Add.1** (30 May 1974), the text of the Israel-Syria disengagement agreement as the UN Digital Library
  holds it: the attached map is not in the file (section 3.4).
- **No official map of 1973 with a coordinate grid was found for the Syrian front.** The Egyptian front has one in a
  secondary atlas (section 1.2). Israeli tier 1 sources (gov.il, idf.il, knesset.gov.il) were not retried; nothing here
  rests on them.
- The two maps read here are third-party works on Wikimedia Commons, used only to read positions off them, not copied
  into the repository (their licences are in section 9).

## 1. Kilometre 101 on the Cairo-Suez road

### 1.1 What the sources say

| Item | Status and source |
|---|---|
| The name and the road. Kissinger to Ismail: "at 2200 hours Cairo local time today at Kilometer 101 of the Cairo–Suez Road they would be met by Israeli representatives, and that the meeting place would be under the supervision of the United Nations peacekeeping force" | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d294 (Washington, undated; its footnotes refer to Sadat's message of 27 Oct) |
| The six-point text: "they will hold a meeting under the auspices of the United Nations Commander at the usual place (kilometer 101 on the Suez–Cairo road) to sign this agreement" | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d334 (Washington, 9 Nov 1973, a White House press release) |
| Where the talks were held: "At Kilometer 101 on the Cairo–Suez Road, Israeli and Egyptian military officials negotiated between October 28 and November 29, 1973" (editors' footnote) | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v26 d31, footnote (Washington, 8 Mar 1974); the same dates in d5, worded "the chief Israeli negotiator at Kilometer 101 on the Cairo-Suez Road, where Israeli and Egyptian military officials negotiated between October 28 and November 29, 1973" |
| The Israeli end of the road: Israel interprets the six points so that the road "remains under “Israeli control” from KM 102 to the entrance to town" | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d329 (Washington, 9 Nov 1973), for Israel's stated reading |
| A different number: Ismail's message proposes a meeting "at the kilometer 110 of the Cairo–Suez road" | [C] with the 101 above. [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d291 (Cairo, 27 Oct 1973). Nothing read explains the difference; the meeting that followed is called Kilometer 101 in d296 and later |
| "an improvised shed near the 101 kilometer marker on the Suez-Cairo road, an area held by Israeli forces on the west bank of the Suez Canal" | [S] JTA, bulletin of 29 Oct 1973, "Israelis, Egyptians Meet Face to Face for First Time Since 1956" (https://www.jta.org/archive/israelis-egyptians-meet-face-to-face-for-first-time-since-1956). The item's dateline in the bulletin PDF (http://pdfs.jta.org/1973/1973-10-29_205.pdf) reads "TEL AVIV, Oct. 28 (JTA)", so "shortly after midnight today" is the night of 27 to 28 Oct, which agrees with FRUS d31's "October 28" |
| "kilometer 101 on the Cairo-Suez road, which is the checkpoint through which the trucks, under UN aegis, cross from Egyptian to Israeli lines" | [S] JTA, bulletin of 6 Nov 1973, "Eye Witness Report: with Israeli Forces in Occupied Egypt" (https://www.jta.org/archive/eye-witness-report-with-israeli-forces-in-occupied-egypt); the item's dateline was not read |
| At the ceasefire the Israeli army "והתייצב על הקילומטר ה-101 מקהיר, על כביש קהיר-סואץ" (our translation: and took its stand at the 101st kilometre from Cairo, on the Cairo-Suez road) | [S, Wikipedia] he.wiki, "הסכם הפרדת הכוחות בין ישראל למצרים", oldid 43941653. The same encyclopaedia's page on Egypt's 7th Division says its bases are near the 101st kilometre, "101 ק"מ ממזרח לקהיר" (101 km east of Cairo), oldid 42908816 |
| "خيمةٍ نُصبت على مبعدة 101 كم من القاهرة على طريق القاهرة-السويس" (our translation: a tent set up 101 km from Cairo on the Cairo-Suez road) | [S, Wikipedia] ar.wiki, "حرب أكتوبر", oldid 76956104 |
| Dinitz to Kissinger: "It is 70 miles between the Canal and Cairo" | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d212 (Washington, 19 Oct 1973, 7:09 p.m.), for what the Israeli ambassador said; used only as the order of magnitude of the canal-to-Cairo distance (70 x 1.609344 = 112.7 km) |

All sources count 101 km **from Cairo**. None gives the zero point of the count, a latitude, a longitude or a village next
to the post. The design's "about 31.9-32.0 E" has no source in the design either (it is marked *to verify*).

Wikidata has an item for the talks (Q108427911, revision 1508526266, found by `wbsearchentities` on 24 Oct); its
`P625` (coordinate location) claim list is empty, so it is not a coordinate source.

### 1.2 Our reading, with the method

The Israeli army's atlas of the war prints Kilometre 101 on its map of the ceasefire lines in the south.

- **Map:** *Atlas of the Yom Kippur War*, map 72, "קו הפסקת האש בדרום" (the ceasefire line in the south), page -83-, as
  Wikimedia Commons holds it: 1775 x 2560 px, licence CC BY-SA 3.0, credit "IDF Spokesperson's Unit"
  (https://commons.wikimedia.org/wiki/File:%D7%9E%D7%A4%D7%94_72_-_%D7%A7%D7%95%D7%95%D7%99_%D7%94%D7%A4%D7%A1%D7%A7%D7%AA_%D7%94%D7%90%D7%A9_%D7%91%D7%93%D7%A8%D7%95%D7%9D_-_%D7%9E%D7%9C%D7%97%D7%9E%D7%AA_%D7%99%D7%95%D7%9D_%D7%94%D7%9B%D7%99%D7%A4%D7%95%D7%A8%D7%99%D7%9D.jpg,
  file revision 565730309, opened 24 Oct 2026). It is a secondary map: a later atlas redrawing the situation.
- **What it shows:** two lines side by side (red and blue) round the Israeli bridgehead; the label "ה-ק"מ ה-101" with a
  black leader line that ends on the road at the western edge of the bridgehead, where the road meets the pair of lines;
  the sheet's graticule, printed every 10 minutes, with the labels "32°30'", "33°00'" and "30°30'".
- **Graticule, measured on the file:** vertical lines at x = 225, 414, 601, 789, 976, 1163, 1350, 1537 px (187.5 px
  apart); the line at x = 789 carries the label 32°30' and the one at x = 1350 the label 33°00' (3 x 187.5 = 562 px for
  30', which fits). Horizontal lines at y = 1242, 1456, 1673, 1887, 2104 px (215.5 px apart); the line at y = 1242
  carries the label 30°30'. So 10' of longitude is 187.5 px and 10' of latitude 215.5 px; that is 16.05 km and 18.45 km
  at this latitude, 11.7 px per km in both directions. The printed scale bar (0-30 km), measured by eye, gives about 11.4
  to 11.5 px per km, 2 percent less.
- **The point:** the leader line's tip is at pixel (527, 1766).
  - Longitude: 32°30' - (789 - 527) / 187.5 x 10' = 32°30' - 13.97' = 32°16.03' = 32.267 E.
  - Latitude: 30°10' - (1766 - 1673) / 215.5 x 10' = 30°10' - 4.32' = 30°05.68' = 30.095 N.
- **Accuracy:** the tip's position is good to about 10 px, 0.9 km, so +-0.01 degrees. The map's own accuracy is not
  known. The graticule labels were read by eye on the file, not from a source table.
- **Cross-check from a routing line:** the OSRM public demo server (https://router.project-osrm.org, queried 24 Oct 2026,
  driving route from Cairo to the centre of Suez, 29.9668 N 32.5498 E, on OpenStreetMap roads of today) gives a road of 142.7, 136.9 and 134.0 km from three
  possible starting points in Cairo (Ramses Square 30.0626 N 31.2467 E, Abbassia 30.0733 N 31.2800 E, Heliopolis
  30.1000 N 31.3500 E; the starting points are our choice, nobody names the zero of the count). The point 101 km along
  each is 30.0858 N 32.2298 E, 30.0942 N 32.2896 E and 30.0877 N 32.3179 E. That is a range 30.09 N, 32.22-32.32 E, with
  today's road, not the 1973 one. It is [S] (a database) and only a cross-check of the map reading: they agree to about
  3 km in longitude at the middle of the range.
- **[C] with the design:** the design writes "about 31.9-32.0 E". Our reading is 0.27 to 0.37 degrees (25 to 34 km) east
  of that. Both are kept.
- **West edge of the bridgehead on the same map:** the blue line's leftmost point is at pixel (347, 1271): 32°30' -
  (789 - 347) / 187.5 x 10' = 32.107 E, and 30°30' - (1271 - 1242) / 215.5 x 10' = 30.478 N (**Our reading**, +-0.01
  degrees). This is the farthest west the ceasefire lines of 24 Oct reach on this map; Km 101 is not it.

## 2. The Hermon summit

| Item | Status and source |
|---|---|
| 33.41611 N, 35.8575 E; the summit "straddles the border between Syria and Lebanon and at the elevation of 2,814 m (9,232 ft), is the highest point in Syria and the Israeli-occupied Golan Heights" | [S, Wikipedia] en.wiki "Mount Hermon", oldid 1369568274 (page text and coordinate from the API; the design uses the same values, "checked 2026-10-10") |
| 33.4 N, 35.85 E, "mountain range in Syria and Lebanon" | [S, Wikidata] Q215471, revision 2529077990. [C] with the row above: 1.9 km apart (north-south (33.41611 - 33.4) x 110.57 = 1.8 km, east-west (35.8575 - 35.85) x 111.32 x cos 33.4 = 0.7 km, sqrt(1.8^2 + 0.7^2) = 1.9 km). Wikidata's point is for the range, Wikipedia's reads as the summit |
| Israeli side: an observation post at "about 2,224 m (7,297 ft)" near Mitzpe Shlagim, and "Its neighboring peak, at 2,236 m (7,336 ft), is the highest elevation in Israeli-controlled territory" | [S, Wikipedia] same oldid. Not a coordinate source |
| 24 Oct 1973: Colby, asked whether Israel now held the whole mountain: "Yes, they’re on the summit." Earlier: "They had a slice of it; it’s a long mountain." | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d259 (Washington, 24 Oct 1973, 10:21 a.m., WSAG), for what the CIA director told the group |
| The Hermon outposts were taken "on the night of October 21" | [S, Wikipedia] en.wiki "Bashan salient", oldid 1379638031. [C] with en.wiki "Yom Kippur War", oldid 1375181066: "On 22 October, the Golani Brigade and Sayeret Matkal commandos recaptured the outpost on Mount Hermon". One day apart; the FRUS date above (24 Oct) is only a latest date |
| 8 May 1974, Asad: "They keep points they occupied after October 22. For example, on Mount Hermon, where they had no positions." | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v26 d48 (Damascus, 8 May 1974), for Asad's statement as the memorandum records it. The same talk: Kissinger "They want all points on top of Mt. Hermon" |
| Bulletin of 24 Jun 1974 (event day not read from a dateline): "The Hermon will remain under UN control" after the Israeli withdrawal from "the western slopes of Mt. Hermon" | [S] JTA, bulletin of 24 Jun 1974, "Disengagement on Time Israel Leaves Last of Syrian Territory Captured in Oct, War" (https://www.jta.org/archive/disengagement-on-time-israel-leaves-last-of-syrian-territory-captured-in-oct-war). The page says "The schedule now calls for Israel to leave Kuneitra and the western slopes of Mt. Hermon on Tuesday afternoon" |

**Margin.** The summit is north of today's 33.35 N edge, the one edge the design names for it ("north of the 33.35 N
edge"): (33.41611 - 33.35) x 110.57 = 7.3 km. East, it is 35.90 - 35.8575 = 0.0425 degrees x 111.32 x cos 33.4 = 3.9 km
inside today's east edge. With the Wikidata point the figures are 5.5 km and 4.6 km. The north edge the owner decides on
(33.5 N in design 1957-77, about 34.0 N in design 1977-93) is 9.3 km and (34.0 - 33.41611) x 110.57 = 64.6 km from the
summit. Beirut, Sultan Yacoub and the 1982 road (owner's comment of 10 Oct) are not part of this note.

## 3. The October 1973 enclave in Syria

### 3.1 Size and depth: the sources disagree

| Statement | Status and source |
|---|---|
| "Israeli forces advanced 10 km beyond the 1967 cease- fire lines into Syrian territory" (the page text breaks "cease-fire" across a line) | [S] The Jewish Agency, "October War 1973" map page, archived copy https://web.archive.org/web/20220930021557/https://archive.jewishagency.org/maps/content/36191 (the archived page opened 24 Oct 2026; the live page is offline, as the design records) |
| The pocket "is 10–18 kilometers deep" | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v26 d29 (Damascus, 1 Mar 1974), Asad's words, for what he said to Kissinger |
| "The salient was about twenty kilometres wide and encompassed an area of approximately 400 km2, extending to a point 33 kilometres from the Syrian capital of Damascus." It "included the summit of Mount Hermon, the town of Beit Jinn, and many other Syrian villages." | [S, Wikipedia] en.wiki "Bashan salient", oldid 1379638031 |
| "The Israeli Army advanced to within 30 km of Damascus" | [S, Wikipedia] en.wiki "Yom Kippur War", oldid 1375181066 |
| On 14 Oct the Israelis "were about 22 miles from Damascus" (22 x 1.609344 = 35.4 km), "had, however, really moved only a few miles beyond their previous positions" | [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d181 (Washington, 14 Oct 1973, 9:16 a.m., WSAG), Colby's briefing, a mid-war figure |
| "the 72 sq. kilometer western edge of the Yom Kippur War enclave" handed over on 23 Jun 1974 | [S] JTA, bulletin of 24 Jun 1974 (the item above). The bulletin shows the dateline "TEL AVIV, June 23 (JTA)"; the item's own lead line was not legible in the OCR text, so the day is the bulletin's only Tel Aviv dateline, not a read one |

Five distance figures, none the same: 10 km (Jewish Agency), 10 to 18 km (Asad), about 20 km wide (Wikipedia), 30 km, 33
km and 35 km from Damascus (three sources). They measure different things (depth from the 1967 line, width, distance to
Damascus) and none says which edge of the pocket it means. **[C]; all kept.** The size figure (400 km2 against the JTA's
72 km2 for one stage) is not comparable.

### 3.2 Places named, with coordinates

| Place | Coordinates | Status and source |
|---|---|---|
| Beit Jinn, "included" in the salient | 33.31056 N, 35.91889 E | [S, Wikidata] Q15198160, revision 2435487053. 1.8 km east of today's east edge ((35.91889 - 35.90) x 111.32 x cos 33.31 = 1.8 km) |
| Mazraat Beit Jinn | 33.30986 N, 35.91920 E | [S, Wikidata] Q17068347, revision 2480719056. [C] with en.wiki "Mazraat Beit Jinn", oldid 1377748525: 33.3333 N, 35.9167 E (2.6 km further north). On the map of section 3.3 its label stands about 3.5 km east of Beit Jinn's (52 px at 0.0678 km per pixel on the reduced view), so Wikidata's point reads as Beit Jinn's rather than the hamlet's: our observation, not a source statement |
| Khan Arnabah, where the UN set up its advance headquarters in June 1974 | 33.18 N, 35.89 E | [S, Wikidata] Q15236107, revision 2366561383, for the place; the headquarters is [S] JTA, bulletin of 20 Jun 1974, "Syrian Troops, Followed by Villagers, Return to Yom Kippur War Enclave" (https://www.jta.org/archive/syrian-troops-followed-by-villagers-return-to-yom-kippur-war-enclave; the item's dateline in the PDF http://pdfs.jta.org/1974/1974-06-20_119.pdf reads "TEL AVIV, June 19 (JTA)") |
| Sa'sa', held by Syria, **not** in the enclave: "Additional movements of Israeli tanks and artillery were observed west of Syrian-held Sasa" | 33.28 N, 36.02 E | [S, Wikidata] Q15274187, revision 2290634479, for the coordinate. The quote is [V, agent read 2026-10-24; a person's check pending] FRUS 69-76 v25 d392 (Washington, 15 Dec 1973), for what UN observers reported on 12 and 13 Dec. Sa'sa' is 11.2 km east of today's east edge ((36.02 - 35.90) x 111.32 x cos 33.28 = 11.2 km) and 7.4 km inside a 36.1 E edge |
| Harfa and Hader, villages the Syrians returned to | not looked up | [S] JTA, bulletin of 20 Jun 1974 (above): "villagers returning to their abandoned homes in Harfa and Hader". The dateline above puts "today" at 19 Jun 1974 |
| Tel Shams, where Israeli tanks passed a reviewing stand and the UN entered "today" in that item | not looked up | [S] same JTA item. The Wikidata search for "Tell Shams" returned only Q15949835, "Tell Shamsine", a different name (33.74 N); no coordinate was taken |

### 3.3 Our reading of the pocket's outer edge

A Wikimedia Commons map draws "the approximate frontlines during late-October 1973" on an OpenStreetMap base with place
labels and a scale bar.

- **Map:** "Yom Kippur War - Golan Front (October 24, 1973).svg" by user Rr016, own work, CC BY-SA 2.0, file revision
  1021312321 of 15 Apr 2025 (https://commons.wikimedia.org/wiki/File:Yom_Kippur_War_-_Golan_Front_(October_24,_1973).svg),
  read as the 1920 x 2572 px render, opened 24 Oct 2026. It is a hobbyist map, not an official one; its lines are
  approximate by its own caption.
- **Georeferencing:** six labelled places as anchors (Quneitra, Majdal Shams, Beit Jinn, Khan Arnabah, Sa'sa', Damascus),
  with their Wikipedia or Wikidata coordinates, fitted by least squares as Web Mercator (longitude linear in x,
  Mercator-latitude linear in y). Scale of the fit: 0.0407 km per pixel; the printed 20 km bar is 492 px, 0.0407 km per
  pixel, the same. Anchor errors: Quneitra 0.5 km N and 0.8 km E, Majdal Shams 0.2 N and 0.5 W, Beit Jinn 0.3 S and 2.3 W,
  Khan Arnabah 0.2 N and 0.2 E, Sa'sa' 0.6 N and 2.4 E, Damascus 0.2 S and 0.6 W. So readings are good to about 3 km;
  that is the accuracy claimed.
- **What the map shows:** the pocket's thick outline, measured on the render as the thick dark-grey line. Its farthest
  east point is at pixel (1467, 1106): **36.14 E, 33.17 N**. Its farthest north point is at (931, 379): **33.4275 N, 35.9057
  E**. East of Quneitra town (35.82389 E) the east point is (36.1395 - 35.82389) x 111.32 x cos 33.17 = 29.4 km. The
  map puts Sa'sa' outside the outline, east of it, as FRUS d392 does.
- **Arithmetic check with the 33 km figure:** a point 33 km from Damascus (33.51306 N, 36.30944 E) at 33.30 N lies at
  36.061 E, at 33.35 N at 36.012 E and at 33.40 N at 35.981 E (circle of 33 km radius; for example at 33.35 N the
  north-south offset is (33.51306 - 33.35) x 110.57 = 18.0 km, the east-west offset sqrt(33^2 - 18.0^2) = 27.6 km, 27.6 /
  (111.32 x cos 33.35) = 0.297 degrees). That is, the Wikipedia figure puts the nearest Israeli point at about 36.0 to
  36.06 E. The map's east point (36.14 E at 33.17 N) is farther from Damascus than that: sqrt(((33.51306 - 33.1696) x
  110.57)^2 + ((36.30944 - 36.1395) x 111.32 x cos 33.17)^2) = sqrt(38.0^2 + 15.8^2) = 41.1 km. The two readings do not
  contradict each other (one is the nearest point, the other the farthest east), but 41 km is not 33 km: **[C]**.

### 3.4 The line of 1974

UN S/11302/Add.1 (30 May 1974, Annex A, read in the UN Digital Library's PDF, 3 pages) fixes the end of the enclave in
words: "All Israeli military forces will be west of the line designated as Line A on the Map attached hereto, except in the
Qneitra area, where they will be west of Line A-1." "All territory east of Line A will be under Syrian administration."
[V, agent read 2026-10-24; a person's check pending] https://digitallibrary.un.org/record/576578/files/S_11302_Add.1-EN.pdf
(record 576578). The text carries no coordinate and the map is not in this file (a note in it says the map is on page
884 of 13 I.L.M.): the line's geometry is a [GAP] (section 7).

**Margin.** The east point at 36.14 E is 22.3 km east of today's east edge ((36.1395 - 35.90) x 111.32 x cos 33.17 = 22.3
km) and 3.7 km east of a 36.1 E edge ((36.1395 - 36.1) x 93.2 = 3.7 km, with 93.2 km per degree at this latitude); the
north point, 33.4275 N, is 8.6 km north of today's north edge and 8.0 km inside a 33.5 N edge. The accuracy of the reading
is about 3 km, so it cannot say that 36.1 E is too short, only that the map read here is not on its safe side. Beit Jinn
needs 35.919 E (1.8 km east of today's edge) and Sa'sa' is the nearest Syrian-held place to the east.

## 4. Sharm el-Sheikh and the Straits of Tiran

| Item | Status and source |
|---|---|
| 27.915 N, 34.3275 E; "on a promontory overlooking the Straits of Tiran at the mouth of the Gulf of Aqaba"; "conquered by Israel during the Suez Crisis of 1956 and returned to Egypt in 1957" | [S, Wikipedia] en.wiki "Sharm El Sheikh", oldid 1380827798 |
| 27.8519 N, 34.305 E, "city in South Sinai, Egypt" | [S, Wikidata] Q644, revision 2555998922. [C] with Wikipedia: 7.3 km apart ((27.915 - 27.8519) x 110.57 = 7.0 km north; (34.3275 - 34.305) x 111.32 x cos 27.9 = 2.2 km east; sqrt(7.0^2 + 2.2^2) = 7.3 km) |
| Naama Bay 27.910528 N, 34.332422 E; Ofira 27.8675 N, 34.295278 E | [S, Wikipedia] en.wiki "Naama Bay", oldid 1366463805; "Ofira", oldid 1376952553. The Wikipedia city point is 0.6 km from Naama Bay; the Wikidata point is 1.8 km from Ofira. Which of the two is the 1957-67 and 1967 position is not stated in a source read |
| Ras Muhammad National Park, 27.7694 N, 34.2097 E (the southern tip of Sinai); Tiran Island 27.95667 N, 34.56194 E; Straits of Tiran 28.00389 N, 34.46528 E | [S, Wikipedia] en.wiki "Ras Muhammad National Park", oldid 1377914976; "Tiran Island", oldid 1376194810; "Straits of Tiran", oldid 1366771403. Wikidata gives the same places at 27.7694 N 34.2097 E (Q1028367), 27.9519 N 34.5661 E (Q598889) and 28.0039 N 34.4653 E (Q208157) |
| 2 Jun 1967, Cairo: "Following the withdrawal of the UNEF, it was only logical that the United Arab Republic armed forces should occupy their positions, among which was the area of Sharm el Sheikh overlooking the Straits of Tiran." | [V, agent read 2026-10-24; a person's check pending] FRUS 64-68 v19 d134 (Cairo, 2 Jun 1967, 2029Z), for what the Egyptian government said |
| 3 Jun 1967: "the withdrawal of Israeli forces from the Sinai, including Sharm el Sheikh" (as part of the 1957 settlement) | [V, agent read 2026-10-24; a person's check pending] FRUS 64-68 v19 d141 (Washington, 3 Jun 1967, 7:17 p.m.), for the US view of the 1957 settlement |
| 7 Jun 1967: Barbour thought Israel "would want to hold on to the areas of the Sinai, including Sharm el-Sheikh, from which they withdrew under pressure in 1957" | [V, agent read 2026-10-24; a person's check pending] FRUS 64-68 v19 d194 (Washington, 7 Jun 1967, 12:05 p.m.), for what the US meeting heard of the ambassador's estimate |

The distinction between Wikipedia's point (Naama Bay, the tourist centre) and Wikidata's (near Ofira, the Israeli
settlement of the 1970s, per the place names alone) is our observation from the coordinates, not a statement in either
source. A map of 1957-67 would settle which is the military site; none was opened. **[C]; both kept.**

**Margin.** Both points are inside today's longitude range (10 to 13 km east of the west edge, 155 to 157 km west of the east edge)
and 170 to 178 km south of today's south edge. Against the design's 27.7 N edge the Wikipedia point has (27.915 - 27.7)
x 110.57 = 23.8 km, the Wikidata point (27.8519 - 27.7) x 110.57 = 16.8 km, Ras Muhammad (27.7694 - 27.7) x 110.57 =
7.7 km, and Tiran Island (27.95667 - 27.7) x 110.57 = 28.4 km. Its east edge is far (Tiran Island is 131.6 km inside the
current east edge).

## 5. Why the four are needed

The need comes from the design (section 7.1, lines 481-498) and the owner's comments on #129 (22 and 24 Oct); this note
checked the places, not the need. The 1973 events are in [forces-1967-73.md](../Era%201957-77/forces-1967-73.md) and
[events-1957-67.md](../Era%201957-77/events-1957-67.md).

## 6. Distances to the edges (the arithmetic)

Positive: inside the box, distance to that edge in km. Negative: outside. Today's box W 34.20, E 35.90, S 29.45, N 33.35;
proposed box W 31.8, E 36.1, S 27.7, N 33.5. Longitude difference x 111.32 x cos(point's latitude); latitude difference x
110.57.

| Point | Today: W / E / S / N (km) | Proposed: W / E / S / N (km) |
|---|---|---|
| Km 101, Our reading (30.095 N, 32.267 E) | -186.2 / +349.9 / +71.3 / +359.9 | +45.0 / +369.2 / +264.8 / +376.5 |
| Km 101 at the design's 31.95 E (same latitude) | -216.7 / +380.4 / +71.3 / +359.9 | +14.4 / +399.7 / +264.8 / +376.5 |
| Km 101 routing cross-check, 32.32 E / 32.22 E (30.09 N) | -181.1 / +344.8 / +70.8 / +360.5 and -190.7 / +354.5 / +70.8 / +360.5 | +50.1 / +364.1 / +264.3 / +377.0 and +40.5 / +373.7 / +264.3 / +377.0 |
| Bridgehead west point, Our reading (30.478 N, 32.107 E) | -200.8 / +363.9 / +113.7 / +317.6 | +29.5 / +383.1 / +307.2 / +334.1 |
| Hermon summit, Wikipedia (33.41611 N, 35.8575 E) | +154.0 / +3.9 / +438.5 / -7.3 | +377.0 / +22.5 / +632.0 / +9.3 |
| Hermon, Wikidata (33.4 N, 35.85 E) | +153.3 / +4.6 / +436.8 / -5.5 | +376.4 / +23.2 / +630.2 / +11.1 |
| Enclave north point, Our reading (33.4275 N, 35.9057 E) | +158.5 / -0.5 / +439.8 / -8.6 | +381.4 / +18.1 / +633.3 / +8.0 |
| Enclave east point, Our reading (33.1696 N, 36.1395 E) | +180.7 / -22.3 / +411.3 / +19.9 | +404.4 / -3.7 / +604.8 / +36.5 |
| Beit Jinn, Wikidata (33.31056 N, 35.91889 E) | +159.9 / -1.8 / +426.9 / +4.4 | +383.2 / +16.8 / +620.4 / +20.9 |
| Sa'sa', Wikidata (33.28 N, 36.02 E), Syrian-held | +169.4 / -11.2 / +423.5 / +7.7 | +392.7 / +7.4 / +617.0 / +24.3 |
| Sharm el-Sheikh, Wikipedia (27.915 N, 34.3275 E) | +12.5 / +154.7 / -169.7 / +600.9 | +248.6 / +174.4 / +23.8 / +617.5 |
| Sharm el-Sheikh, Wikidata (27.8519 N, 34.305 E) | +10.3 / +157.0 / -176.7 / +607.9 | +246.6 / +176.7 / +16.8 / +624.5 |
| Ras Muhammad National Park (27.7694 N, 34.2097 E) | +1.0 / +166.5 / -185.8 / +617.0 | +237.4 / +186.2 / +7.7 / +633.6 |
| Tiran Island (27.95667 N, 34.56194 E) | +35.6 / +131.6 / -165.1 / +596.3 | +271.6 / +151.2 / +28.4 / +612.9 |

Worked example, Km 101 west edge today: (32.267 - 34.20) x 111.32 x cos 30.095 = -1.933 x 96.3 = -186.2 km. Km 101 west
edge proposed: (32.267 - 31.8) x 96.3 = +45.0 km.

What this says for the owner's decision, without making it: the proposed west edge (31.8 E) has room for Km 101 and for
the bridgehead's west point; the proposed east edge (36.1 E) has 7.4 km to Sa'sa' but is 3.7 km short of the east point
of the one map read for the pocket; the proposed south edge (27.7 N) has room for Sharm el-Sheikh and Ras Muhammad (7.7
km); the north edge needs at least 9.3 km beyond the Hermon summit at 33.5 N.

## 7. Still [GAP]

- **A coordinate for Km 101 in a primary source.** (24 Oct) Searched again and not found. Read: FRUS 69-76 v25 d291,
  d294, d296, d329, d334 and v26 d5, d16, d31 (all name the road and the number, none a coordinate); the JTA items listed
  in section 1.1; he.wiki, ar.wiki and en.wiki pages; the Wikidata item of the talks. Not read: the IDF archive's
  Km 101 pages (https://www.idf.il not retried), the Emory University page "שיחות הקילומטר ה-101" that he.wiki links
  (not opened), UN Archives records of UNEF II. The coordinate above is Our reading.
- **The 1973 enclave's outer line in a primary source.** (24 Oct) Searched again and not found. Read: UN S/11302/Add.1
  (text only; the map is not in the file), FRUS 69-76 v25 d181, d259, d392 and v26 d29, d48, d49 (distances and place
  names, no coordinates). The UN map of the 1974 agreement (13 I.L.M. 884) and the UNDOF line maps were not opened. The
  reading in section 3.3 comes from a hobbyist map.
- **The Hermon summit's coordinate in a primary or surveyed source.** (24 Oct) Searched again and not found. Read: en.wiki
  and Wikidata (they differ by 1.9 km). No survey map was opened.
- **Which Sharm el-Sheikh position the 1957-67 and 1967 events refer to** (Naama Bay or the old town near Ofira): not
  found. Read: FRUS v19 d134, d141, d194 (they name "the area of Sharm el Sheikh"), en.wiki "Sharm El Sheikh", "Naama
  Bay", "Ofira".
- **The day the Hermon outposts were taken** (21 or 22 Oct) is [C], see section 2; not needed for the extent.

## 8. Search log

All run on 24 Oct 2026. "Returned" is the number of results the call returned; "read" is how many I read.

| Search | URL pattern and range | Returned | Result |
|---|---|---|---|
| JTA `Sasa Syria Israeli` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sasa+Syria+Israeli&per_page=20&after=1973-10-01T00:00:00&before=1974-06-30T00:00:00` | 7 | read the 7 titles; none opened for Sa'sa' |
| JTA `Mt. Hermon Israeli forces Syria` | same pattern, same range | 20 (cap; 44 existed per a re-run) | read the first 12 titles; opened the 25 Jun, 20 Jun, 19 Jun and 24 Jun 1974 items; used 20 Jun and 24 Jun (the 25 Jun and 19 Jun items' datelines were not legible in the OCR text, so they are not used) |
| JTA `Kilometer 101` | same pattern, same range | 20 (cap; 55 existed per a re-run) | read the first 12 titles (two Kiryat Shemona and Galilee items of Apr and May 1974, ten Egypt-Israel items of Jan to Mar 1974); none opened |
| JTA `Sharm el-Sheikh` | same pattern, same range | 17 | read the first 12 titles (Nov 1973 to Mar 1974 items on the peace talks and Dayan); none opened |
| JTA `Tel Shams` | same pattern, same range | 20 (cap; 28 existed per a re-run) | read the first 12 titles; opened the 19 Jun (not used) and 20 Jun 1974 items |
| JTA `Mazrat Beit Jinn` | same pattern, same range | 0 | none returned |
| JTA `kilometer 101 Cairo-Suez road meeting` | same pattern, `after=1973-10-26`, `before=1973-11-16` | 5 | read the 5 titles; opened the 29 Oct and 12 Nov 1973 items (12 Nov not used: its lead dateline was not legible) and the 15 Nov item (not used) |
| JTA `Cairo-Suez road 101 kilometers` | same pattern, same range | 6 | read the 6 titles: the 5 above and the 6 Nov 1973 item, which was opened and used |
| Wikipedia search `"Kilometre 101" Cairo Suez road 1973` | `https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch=...`, no date range | 1 | "Yom Kippur War" only; no page for Km 101 |
| Wikipedia search `"Kilometer 101" Egypt Israel 1973 ceasefire` | same | 2 | "Yom Kippur War", "Aharon Yariv" |
| Wikipedia searches `"km 101" Suez Cairo road` and `"101st kilometer"` | same | 4 and 9 | none about the Suez road (distances of other places; the Soviet "101st kilometre" restriction) |
| ar.wikipedia search `الكيلو 101 القاهرة السويس مفاوضات` | `https://ar.wikipedia.org/w/api.php?...` | 34 (8 shown, 8 read) | the "حرب أكتوبر" page quoted in section 1.1 and a page on the talks; no coordinate |
| he.wikipedia search `"הקילומטר ה-101"` | `https://he.wikipedia.org/w/api.php?...` | 14 (8 shown, 8 read) | the two pages quoted in section 1.1 and the division page; no coordinate |
| Wikidata `wbsearchentities` `Kilometer 101 Cairo Suez` and four other forms | `https://www.wikidata.org/w/api.php?action=wbsearchentities&search=...&language=en` | 1 for the first; 0 for the others | only Q108427911 (the talks); no coordinate seen |
| Overpass (OpenStreetMap) names with "101" in 29.5-30.5 N, 31.5-32.6 E | `https://overpass-api.de/api/interpreter` | error 504 | not run to completion; no result is claimed |
| Commons search `אטלס מלחמת יום הכיפורים` and category "IDF atlas of the Yom Kippur War" | `https://commons.wikimedia.org/w/api.php?...` | 36 hits; 47 category members | the atlas files on Commons are maps 16-80 of the Sinai front; none of the northern front |
| Commons searches for a northern ceasefire-line map (two Hebrew and one English query) | same | 0 each | none |
| UN Digital Library `S/11302/Add.1` and `S/11302` | `https://digitallibrary.un.org/search?p=...&of=xm&rg=5` | 1 and 5 records | read S/11302/Add.1 (3 pages) |
| `https://peacemaker.un.org/sites/peacemaker.un.org/files/IL%20SY_740531_DisengagementAgreementIsraelSyria.pdf` | direct | HTTP 404 | not available at that address |
| `https://www.jewishvirtuallibrary.org/israel-syria-separation-of-forces-agreement` | direct | HTTP 200, 797 characters of text | the page renders by script; no agreement text |
| FRUS TEI text search `kilomet(er|re)s? ?101` and `km\.? ?101` (case-insensitive) | 1969-76 v25, v26 | 60 hits in 27 documents (v25) and 24 hits in 12 documents (v26) | I read the passages of d291, d294, d296, d298, d305, d324, d329, d334 and the v26 hits in d3 to d17, d28, d31; none gives a coordinate. Others (v25 d342 to d425) were not read |
| FRUS TEI text search `Sasa|Sa'sa|Mazraat|Beit Jinn|Tel Shams|Kafr Hawar` | 1969-76 v25, v26 | 1 document (v25 d392) and 0 | no coordinate |
| FRUS TEI text search `Hermon` | 1969-76 v25, v26 | 8 and 15 documents | read the printed passage of every hit shown: all 8 documents of v25 (d112, d115, d121, d259, d310, d312, d314, d401) and 14 of the 15 of v26 (my output was cut at 40 lines; d29, d32, d36, d39, d47, d48, d49, d51, d52, d53, d57, d67, d71, d74 shown); none gives a coordinate |
| Commons searches `Kilometer 101 Suez Cairo road map 1973`, `Yom Kippur War Sinai Suez Canal crossing map 1973 ceasefire line`, `UNEF II deployment map Suez 1974` | `https://commons.wikimedia.org/w/api.php?action=query&list=search&srnamespace=6&srsearch=...` | 1, 0 and 15 | read the titles: a book scan, and Ford Library files on the 1975 Sinai agreement; none opened |

## 9. Sources opened and when

All on 24 Oct 2026.

| Source | Used for |
|---|---|
| FRUS 1969-76 v25 (history.state.gov): d181, d212, d259, d291, d294, d296, d329, d334, d392 | Km 101 name and road, Hermon, Sa'sa', pocket depth |
| FRUS 1969-76 v26: d5, d16, d29, d31, d48, d49 | Km 101 dates, pocket depth, Hermon 1974 |
| FRUS 1964-68 v19: d134, d141, d194 | Sharm el-Sheikh 1957 and 1967 |
| UN S/11302/Add.1, https://digitallibrary.un.org/record/576578/files/S_11302_Add.1-EN.pdf | The 1974 line in words |
| JTA archive: 681999, 682073, 684331, 684359 (jta.org/archive), original PDFs 1973-10-29_205, 1974-06-20_119, 1974-06-24_121. Opened and not used: 682112, 682132, 683336, 684377, 681921 | Km 101 shed and checkpoint, the pocket's villages, the 72 km2 stage |
| The Jewish Agency map page, web.archive.org copy of 30 Sep 2022 | 10 km beyond the 1967 line |
| en.wiki oldids: Mount Hermon 1369568274; Sharm El Sheikh 1380827798; Bashan salient 1379638031; Yom Kippur War 1375181066; Mazraat Beit Jinn 1377748525; Naama Bay 1366463805; Ofira 1376952553; Ras Muhammad National Park 1377914976; Tiran Island 1376194810; Straits of Tiran 1366771403; Quneitra 1378912340; Damascus 1378355105; Majdal Shams 1381385435 | Coordinates and quotes [S, Wikipedia] |
| he.wiki oldids 43941653, 42908816; ar.wiki oldid 76956104 | Km 101 in the Hebrew and Arabic encyclopaedias |
| Wikidata revisions: Q644 2555998922; Q215471 2529077990; Q15198160 2435487053; Q17068347 2480719056; Q15236107 2366561383; Q15274187 2290634479; Q1028367 2550518756; Q598889 2555323899; Q208157 2559625898 | Coordinates [S, Wikidata] |
| IDF atlas map 72 (Commons, file revision 565730309, CC BY-SA 3.0) | Our reading of Km 101 and the bridgehead |
| "Yom Kippur War - Golan Front (October 24, 1973).svg" (Commons, file revision 1021312321, CC BY-SA 2.0, OpenStreetMap base) | Our reading of the pocket's outer edge |
| OSRM demo router (router.project-osrm.org) | Cross-check of Km 101 |
| `content/map/hexes.json`, `tools/dem_to_heightmap.py`, `tools/geo.py` (this repository) | Today's box and the distance constants |
