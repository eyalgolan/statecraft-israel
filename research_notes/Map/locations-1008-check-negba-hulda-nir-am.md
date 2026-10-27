# Source check of the `negba`, `hulda` and `nir_am` rows (#1008, first slice)

Status: **draft, needs a person to check**. Research only: an AI agent opened every source below on 26 October 2026.
`docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check each claim against its source. This note
changes nothing in `content/`, `sim/` or `unity/`: the three rows keep `status: draft` and `name_he_review: agent-checked`,
and no `reviewed_by` is written (the owner's 22 October rule on #1008: only the person who did the check does that).

Scope: the three rows `negba`, `hulda` and `nir_am` of [`content/map/locations_1948.yaml`](../../content/map/locations_1948.yaml)
as on `main` at `05e8890b`. Field meanings are from [`docs/locations.md`](../../docs/locations.md) (identity, period-appropriate
name, map position and hex binding, starting control, sources and review state).

## Conventions

- **[V, agent read 26 Oct 2026; a person's check pending]**: the printed or served text was opened by an agent on that day and
  says what the cell reports.
- **[S]**: Wikipedia or Wikidata (a compilation, never primary), read by revision id.
- **[C]**: sources conflict; both values are kept.
- **[GAP]**: not reached or not found; what was tried is written next to it.
- **Result** column: *match* (the source supports the row), *conflict* (the source says something different),
  *not covered* (the source is silent), *not a row field* (the row has no such field; the fact is recorded for the owner).
- Hex conversion is the repo's own: `tools/hexgrid.py` `settlement_hex(lon, lat, grid)` over `content/map/hexes.json`
  (bounds lon 34.2 to 35.9, lat 29.45 to 33.35, hex circumradius 5 km), run on 26 Oct 2026. The three rows' stored `q`/`r`
  equal what it returns for their own `lon`/`lat`: negba (-7, 25), hulda (-4, 23), nir_am (-9, 27).

## Summary

| Row | Fields checked | match | conflict | not covered / not a row field |
|---|---|---|---|---|
| `negba` | name, name_he, kind, lon/lat, hex (5 row fields); period name, dates, starting control (not row fields) | 5 | 0 | 3 not row fields |
| `hulda` | same | 5 | 0 | 3 not row fields (the founding dates conflict among the sources) |
| `nir_am` | same | 5 | 0 | 3 not row fields (the founding dates conflict among the sources; no primary source reached for control) |

No field of the three rows conflicts with a source. What the sources do show that the owner may want to know: (1) all three
places had an earlier or other name that the rows do not carry (Negba: Beit Affa / Givat Ganim; Nir Am: Beit Hanun, then
Shdemot; Hulda: the same name as the Arab village Khulda about 1 km to the east); (2) the sources disagree on founding years
(Hulda 1930 or 1931; Nir Am 21 January or 19 August 1943), and the rows carry no date; (3) the rows carry no
`starting_control`; (4) the Hulda point lies 0.27 km from a hex edge (computed below), so a source that puts Hulda a few
hundred metres elsewhere could change its hex. Every map-read position below stays in the row's hex.

## Sources opened

| Id | Source | Day opened | Used for |
|---|---|---|---|
| WD1, WD2, WD3 | Wikidata items Q1019154 (Negba), Q2916639 (Hulda), Q2891278 (Nir Am), `https://www.wikidata.org/wiki/Special:EntityData/<Q>.json`, revisions 2530835906, 2525235740, 2525233266 | 26 Oct 2026 | the row's own cited coordinate source [S] |
| HW1, HW2, HW3 | he.wikipedia, `https://he.wikipedia.org/w/index.php?oldid=43556119` (נגבה), `oldid=43882049` (חולדה (קיבוץ)), `oldid=44033578` (ניר עם), read as plain text through the MediaWiki API | 26 Oct 2026 | the row's cited `name_he` source [S] |
| EW1, EW2, EW3 | en.wikipedia, `oldid=1375743769` (Negba), `oldid=1377268762` (Hulda, Israel), `oldid=1380468384` (Nir Am) | 26 Oct 2026 | names, dates, coordinates [S] |
| MAP | Survey of Palestine 1:20,000 as served by Palestine Open Maps, tile layer `https://palopenmaps.org/tiles/pal20k-1940s/{z}/{x}/{y}.jpg`, zoom 15 and 16, 3 x 3 tiles around each row point, row point marked in red on the agent's copy | 26 Oct 2026 | printed names and positions [V, agent read 26 Oct 2026; a person's check pending] |
| SHEETS | Palestine Open Maps sheet index `raw-data/sheets.csv` at `PalOpenMaps/pom-data` commit `55fe58c45d53f7bce6045659e19286d30c544288` | 26 Oct 2026 | which sheets cover the points |
| VS | *Village Statistics, 1945*, page images `https://users.cecs.anu.edu.au/~bdm/yabber/census/VSpages/VS1945_p29.jpg` and `VS1945_p32.jpg`, page header "VILLAGE STATISTICS - 1945, as at 1.4.45" | 26 Oct 2026 | Jewish population rows [V, agent read 26 Oct 2026; a person's check pending] |
| JTA-1 | JTA Daily News Bulletin, Thursday 20 May 1948, vol. XV no. 117, `http://pdfs.jta.org/1948/1948-05-20_117.pdf`, page 2 (page image, 200 dpi, crop read) | 26 Oct 2026 | Negba on 19 May 1948 |
| JTA-2 | JTA Daily News Bulletin, 3 June 1948, `http://pdfs.jta.org/1948/1948-06-03_128.pdf`, page 2 (page image, crops read) | 26 Oct 2026 | Negba and Hulda on 2 June 1948 |

Not reached from this box: the National Library of Israel (`nli.org.il`), the Israel State Archives (`archives.gov.il`) and
the IDF Maarachot site all answered HTTP 403 on 26 Oct 2026 with the plain research User-Agent, and no retry with other
headers was made. `main.knesset.gov.il` answered 303 and was not pursued (no Knesset document is needed for these fields).

## `negba` (row: `Negba`, `נגבה`, kibbutz, lon 34.6830556, lat 31.6608333, q -7, r 25)

| Field | Row says | Source says | Result | Tag | Source | Read |
|---|---|---|---|---|---|---|
| name | `Negba` | Mandate map prints the label "Negba" beside a purple-outlined settlement boundary; JTA 19 May 1948: "The southern Jewish settlement of Negba was raided by planes this morning" (the OCR of the page drops the name; the page image crop shows "Negba was raided by planes this morning") | match | [V, agent read 26 Oct 2026; a person's check pending] | MAP, JTA-1 | 26 Oct 2026 |
| name_he | `נגבה` | Wikidata Hebrew label "נגבה"; he.wikipedia title "נגבה" | match | [S] | WD1, HW1 | 26 Oct 2026 |
| kind | `kibbutz` | Wikidata P31 = Q161387, whose English label is "kibbutz"; he.wiki: "קיבוץ מתנועת הקיבוץ הארצי השומר הצעיר" | match | [S] | WD1, HW1 | 26 Oct 2026 |
| lon/lat | 34.6830556, 31.6608333 | Wikidata P625 31.6609, 34.6831 (4 decimals, 8 m from the row); en.wiki 31.66083333, 34.68305556 (the row's digits exactly, 0 m); he.wiki 31.66154399, 34.68251252 (94 m from the row). Mandate map: the red row point is about 135 m south of the centre of the printed "Negba" boundary (about 33 px at 4.06 m/px, zoom 15), which is within the 1:20,000 map's own symbol precision | match (within 100 m of every coordinate source; about 135 m from the map symbol) | [S] for the three coordinates; [V, agent read 26 Oct 2026; a person's check pending] for the map reading | WD1, EW1, HW1, MAP | 26 Oct 2026 |
| hex | q -7, r 25 | `settlement_hex` on each source coordinate: row, Wikidata, en.wiki and he.wiki all (-7, 25). Map-read point (135 m north of the row point) also (-7, 25). Row point is 1.88 km from the hex centre and 2.6 km from the nearest edge | match | [V, agent read 26 Oct 2026; a person's check pending] (computation) | tools/hexgrid.py | 26 Oct 2026 |
| period / other names | none in the row | he.wiki: "בתחילה נקראה הנקודה בית עפה על שם הכפר הערבי הסמוך או \"גבעת גנים\" כשם הגרעין המייסד, ולאחר כשנה הוסב שמו ל\"נגבה\"" (at first the point was called Beit Affa after the nearby Arab village, or "Givat Ganim" after the founding group; about a year later it became Negba). The Mandate map (sheet 11-11 El Majdal, row point on its east edge at lon 34.6847) and JTA 1948 print "Negba" | not a row field; no conflict with the row's name | [S] | HW1, MAP | 26 Oct 2026 |
| dates | none in the row | Founded 12 July 1939: he.wiki "היישוב הוקם ב-12 ביולי 1939 כיישוב חומה ומגדל"; en.wiki "founded on 12 July 1939"; Wikidata P571 1939 (year). VS 1945 page 32 (Gaza Sub-District), serial 55 "Negba": Jews 280, total 280. | not a row field; the sources agree with each other | [S] for 1939; [V, agent read 26 Oct 2026; a person's check pending] for VS 1945 | WD1, HW1, EW1, VS | 26 Oct 2026 |
| starting control | no such field on this row | JTA bulletin of 20 May 1948, dateline "TEL AVIV, May 19. (JTA)": Negba "was raided by planes this morning, but no casualties or damage were reported" (the paragraph sits under that dateline in the item); JTA 3 June 1948, dateline "TEL AVIV, June 2. (JTA)": "The Egyptian blows this morning fell at Negba, in southern Palestine, where infantry supported by tanks and artillery opened an attack." So Jewish-held on 19 May and on 2 June 1948. The 15 May state itself is not printed in either item; he.wiki says the Egyptian army did not take the kibbutz ("לא עלה בידם לכבוש את הקיבוץ") | not a row field; Israeli-held on 19 May and 2 June is [V, agent read 26 Oct 2026; a person's check pending]; 15 May exactly is [S] | [V, agent read 26 Oct 2026; a person's check pending] for 19 May and 2 June | JTA-1, JTA-2, HW1 | 26 Oct 2026 |

## `hulda` (row: `Hulda`, `חולדה`, kibbutz, lon 34.8833333, lat 31.8322222, q -4, r 23)

| Field | Row says | Source says | Result | Tag | Source | Read |
|---|---|---|---|---|---|---|
| name | `Hulda` | Mandate map prints "Hulda Colony" inside a purple-outlined boundary, and "HULDA" (with "I" and "1852") across the green-shaded area to the south-east, which is the Arab village. JTA 2 June 1948: "At Hulda, on the Latrun front, Egyptian planes dropped bombs on Jewish positions." | match (the map's name for the Jewish settlement is "Hulda Colony") | [V, agent read 26 Oct 2026; a person's check pending] | MAP, JTA-2 | 26 Oct 2026 |
| name_he | `חולדה` | Wikidata Hebrew label "חולדה" (its native-label statement is vocalised, "חֻלְדָּה"); he.wikipedia title "חולדה (קיבוץ)", first line "חולדה (חֻלְדָּה) הוא קיבוץ בשפלה" | match | [S] | WD2, HW2 | 26 Oct 2026 |
| kind | `kibbutz` | Wikidata P31 = Q161387 ("kibbutz"); he.wiki "הוא קיבוץ בשפלה במועצה אזורית גזר" | match | [S] | WD2, HW2 | 26 Oct 2026 |
| lon/lat | 34.8833333, 31.8322222 | Wikidata P625 31.8323, 34.8834 (11 m from the row); en.wiki 31.83222222, 34.88333333 (0 m); he.wiki 31.83139184, 34.88257762 (116 m). Mandate map (sheet 13-13 Qazaza): the row point lies on the north-east edge of the "Hulda Colony" boundary, about 215 m from the boundary's centre (about 136 m east and 164 m north of it, zoom 16, 2.03 m/px) | match | [S] for the coordinates; [V, agent read 26 Oct 2026; a person's check pending] for the map reading | WD2, EW2, HW2, MAP | 26 Oct 2026 |
| hex | q -4, r 23 | `settlement_hex` on row, Wikidata, en.wiki and he.wiki points: all (-4, 23). Map-read boundary centre (136 m west, 164 m south of the row point) and (170 m west, 200 m south) also (-4, 23). **The row point is 4.68 km from the hex centre and only 0.27 km from the nearest hex edge**, so a position error above about 270 m in that direction would move the binding | match, with the edge-proximity note | [V, agent read 26 Oct 2026; a person's check pending] (computation) | tools/hexgrid.py | 26 Oct 2026 |
| period / other names | none in the row | he.wiki: "מקור השם חולדה מהכפר הערבי בעל אותו שם אשר שכן מזרחית לקיבוץ" (the name comes from the Arab village of the same name that lay east of the kibbutz); en.wiki: "takes its name from the Palestinian village of Khulda". VS 1945 page 29 (Ramle Sub-District) has both an Arab "Khulda" row (Moslems 280, total 280) and a separate row "Hulda" (Jews 260, total 260). The serial number of the "Hulda" row is small print: the agent could not read it reliably at first, and a review agent read it as 31 (a person should confirm). Correction 27 Oct 2026: on the page image (VS1945_p29.jpg, cropped and enlarged) the row reads "31 Hulda", Jews 260, total 260, so the serial is 31 (agent read 27 Oct 2026; a person's check pending) | not a row field; it is the distinct-from fact for the identity contract | [V, agent read 26 Oct 2026; a person's check pending] for VS 1945; [S] for the name origin | VS, HW2, EW2 | 26 Oct 2026 |
| dates | none in the row | Kibbutz founding: he.wiki lead "הוקם כקבוצה בשנת 1930 על ידי תנועת גורדוניה" but its history section "בסתיו 1931 עברה למקום קבוצת "גורדוניה" א' מחדרה והקימה את קיבוץ חולדה"; en.wiki "In 1931, the Gordonia pioneer group resettled Hulda"; Wikidata P571 1930. Farm: he.wiki "ב-1906", en.wiki "In 1909, the Hulda farm was established". The he.wiki text also says the members moved in 1938 to the current site "כקילומטר מערבית ליער", about 1 km west of the forest | not a row field; the sources conflict among themselves (1930 vs 1931; farm 1906 vs 1909) | [C], both values kept | WD2, HW2, EW2 | 26 Oct 2026 |
| starting control | no such field on this row | JTA 3 June 1948, dateline "TEL AVIV, June 2. (JTA)": "At Hulda, on the Latrun front, Egyptian planes dropped bombs on Jewish positions." Jewish-held on 2 June 1948. he.wiki: "במלחמת העצמאות הייתה חולדה הנקודה המזרחית ביותר אשר היה לה קשר גאוגרפי ישיר עם היישוב ומוסדותיו" | not a row field; Israeli-held on 2 June is [V, agent read 26 Oct 2026; a person's check pending]; 15 May exactly is [S] | [V, agent read 26 Oct 2026; a person's check pending] for 2 June | JTA-2, HW2 | 26 Oct 2026 |

## `nir_am` (row: `Nir Am`, `ניר עם`, kibbutz, lon 34.5808333, lat 31.5194444, q -9, r 27)

| Field | Row says | Source says | Result | Tag | Source | Read |
|---|---|---|---|---|---|---|
| name | `Nir Am` | Mandate map (sheet 11-10 Bureir, the row point lies at the sheet's west edge, lon 34.5804) prints "Nir Am" beside a rectangular boundary with building symbols. The sheet exists in 1931, 1942 and 1946 editions, and the name was adopted in 1946 (see period names), so the tile the agent read is a 1946 or later edition; this is this note's inference, the tile layer does not show its edition | match | [V, agent read 26 Oct 2026; a person's check pending] | MAP, SHEETS, HW3 | 26 Oct 2026 |
| name_he | `ניר עם` | Wikidata Hebrew label "ניר עם"; he.wikipedia title "ניר עם" | match | [S] | WD3, HW3 | 26 Oct 2026 |
| kind | `kibbutz` | Wikidata P31 = Q161387 ("kibbutz"); he.wiki "הוא קיבוץ בנגב הצפוני-מערבי" | match | [S] | WD3, HW3 | 26 Oct 2026 |
| lon/lat | 34.5808333, 31.5194444 | Wikidata P625 31.5194, 34.580897222222 (8 m); en.wiki 31.51944444, 34.58083333 (0 m); he.wiki 31.51985349, 34.57991759 (98 m). Mandate map: the red row point sits inside the printed boundary, about 70 m from its centre, south of the cluster of building symbols (zoom 16, 2.03 m/px; a review agent's check, 26 Oct 2026) | match | [S] for the coordinates; [V, agent read 26 Oct 2026; a person's check pending] for the map reading | WD3, EW3, HW3, MAP | 26 Oct 2026 |
| hex | q -9, r 27 | `settlement_hex` on row, Wikidata, en.wiki, he.wiki points: all (-9, 27). A point 150 m north of the row point also gives (-9, 27). The row point is 2.79 km from the hex centre and 1.54 km from the nearest edge | match | [V, agent read 26 Oct 2026; a person's check pending] (computation) | tools/hexgrid.py | 26 Oct 2026 |
| period / other names | none in the row | he.wiki: "בהקמתו נקרא היישוב בית חנון על שם בית חאנון שעל קרקעותיה הוא הוקם. בשנת 1943 נקבע שם המקום כשְׁדֵמוֹת. בשנת 1946 הוסב שם היישוב לניר עם" (at its founding it was called Beit Hanun after Beit Hanoun, on whose land it was built; in 1943 the name Shdemot was set; in 1946 it became Nir Am). The en.wiki article names no earlier name. VS 1945 (as at 1 April 1945) page 32, Gaza Sub-District (alphabetical, "Negba" is followed by "Ni'ilya"), has no "Nir Am" or Beit Hanun Jewish row; a text search of the OCR of pages 4 to 33 for "Nir Am" and "Hanun" found nothing, but OCR of those pages is unreliable, so the agent records the absence as [GAP] and not as a negative | not a row field; the row's name is the 1946-onward name | [S] for the names; [GAP] for any printed 1945 name | HW3, VS | 26 Oct 2026 |
| dates | none in the row | Founding: he.wiki "הקיבוץ הוקם בט\"ו בשבט ה'תש\"ג (21 בינואר 1943)"; en.wiki "established on 19 August 1943". Wikidata P571 1943 (year). The two Wikipedia dates differ by seven months | not a row field; **[C]**: 21 January 1943 (he.wiki) against 19 August 1943 (en.wiki), both kept; neither is a primary source and none was reached | [C] | WD3, HW3, EW3 | 26 Oct 2026 |
| starting control | no such field on this row | JTA searches ("Nir Am", 1 Jan to 31 Dec 1948, `wp-json/wp/v2/archive`, returned 16 items, none with the name in the page text the agent scanned; "Nir-Am", 1 Jan 1943 to 31 Dec 1948, returned 54 items, none with the name in the text the agent scanned; run 26 Oct 2026). en.wiki: "During the 1948 war it was the headquarters of the Negev Brigade." No printed 15 May 1948 statement was found | not a row field; [GAP] for a primary source | [GAP] (24 Oct rule) Searched again and not found. Read: the two JTA searches above, the en.wiki and he.wiki articles, the Mandate map tile, VS 1945 page 32 | HW3, EW3 | 26 Oct 2026 |

## Searches run (so others can repeat them)

All through `https://www.jta.org/wp-json/wp/v2/archive?search=<query>&after=<from>T00:00:00&before=<to>T23:59:59&per_page=100&page=1`,
run 26 Oct 2026 with a script:

| Query | Range | Items returned | Items whose page text contained the name |
|---|---|---|---|
| Negba | 1 Jan to 31 Dec 1948 | 9 | 9 (read: 20 May 1948 and 3 June 1948 PDFs; the other seven are not read beyond the match) |
| Hulda | 1 Jan to 31 Dec 1948 | 8 | 4 (read: 3 June 1948 PDF; the 7 April 1948 item matches twice and was not read) |
| Nir Am | 1 Jan to 31 Dec 1948 | 16 | 0 |
| Nir-Am | 1 Jan 1943 to 31 Dec 1948 | 54 | 0 |

The match counts are of the rendered page text by a case-insensitive pattern (for Nir Am, `Nir[ -]?Am` in the first search and `nir.am` in the second).

## What a person still has to do

1. Open the Mandate map sheets 11-11 (El Majdal), 11-10 (Bureir) and 13-13 (Qazaza) on `palopenmaps.org` and confirm the three
   readings above, including which edition shows "Nir Am".
2. Settle the two [C] founding dates (Hulda, Nir Am) in a primary source, if the owner wants a date on those rows; the three
   rows carry none today.
3. Decide whether the rows should gain a period name (Negba, Nir Am) or a `starting_control` (all three). This note gives the
   facts only.
