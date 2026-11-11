# Arava springs and oases with tamarisk, 1948-51 (#2287, research part)

Status: **draft, needs a person to check**. This is research only. An AI agent opened every source marked "agent read" below on
3 November 2026, and the row dated 2026-11-11 on 11 November 2026, and recorded the day it opened each. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to
check every claim against its source before use. This note does not change `content/`, `sim/` or `unity/`, and it does not
choose where the tile goes: placement follows in a separate PR, and the owner decides it.

## Question and result

#2287 asks: which springs or oases in the Arava (the polygon at `tools/hex_tiles/regions.yaml:378`) had tamarisk growing at them
between 1948 and 1951, with names, a point, whether a source records the tamarisk itself or only the spring, which side of the
1949 armistice line each falls on, and any change before 1951. The tile is `unity/Assets/Scripts/Map/HexTiles/Prototypes/AravaOasis.cs`.

**What closed**

- **Springs that existed, with names and an approximate point.** Four springs are printed by name on the 1946 and 1951 1:250,000
  sheets inside the game's Arava: Ein Ghadyan (1946) which is Ein Yotvata (1951), Ein Yahav with Ein Rahel beside it, Ein Hatseva
  (with Ein Ofarim, Ein Amatsyahu and Ein Aqrabim north of it), and Ain el Hufeira, whose label is printed east of the line.
  Points are read off the printed labels (see the table; about 300 m at best, not a surveyed point).
- **Side of the line.** The 1951 sheet prints "Undemarcated" along the Arava boundary. Ein Yotvata, Ein Yahav and Ein Hatseva are
  printed west of it (Israel's side); the label "Ain el Hufeira" is printed east of it (Jordan's side), with its spring symbol touching the line.
- **Changes before 1951.** Ein Yahav: an agricultural experiment station from 1950 [S]. Ein Yotvata (as Ein Radian): a Nahal
  settlement from 1951 [S].

**What did not close, and stays [GAP]:** every row that asks for the tamarisk itself. No source opened here names tamarisk at any
Arava spring in 1948-51. The maps print springs, palm signs at the spring and scrub signs along the road (by comparison with other Survey of Palestine legends), not a species. See "Open gaps".

## Answer table

Tags: V (always written in full, as in the rows) means opened and read by the agent; [S] secondary (Wikipedia by revision, or a
value the agent derived from a map); [C] conflicting, both values kept; [GAP] still open.

| Claim | Source and page or URL | Printed text | Tag | Day read |
|---|---|---|---|---|
| The 1951 Israeli 1:250,000 sheet 3 prints "Ein Yotvata" at a spring beside a mark for "Yotvata" and "Melehat Yotvata" (salt flat) | Palestine Open Maps tiles `https://palopenmaps.org/tiles/isr250k-1951/12/{x}/{y}.jpg` (z12, x 2445 to 2447, y 1690 to 1692); sheet list `https://palopenmaps.org/api/sheets` ("Sheet 3", layer `isr250k1951`, year 1951) | map labels "Ein Yotvata", "Yotvata", "Melehat Yotvata" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The 1946 Palestine 1:250,000 sheet 3 prints the same spring as "Ein Ghadyan" beside "Makhfar Ghadyan (Disused P.)" | tiles `https://palopenmaps.org/tiles/pal250k-1946/12/{x}/{y}.jpg` (same tile numbers); sheet list: "Sheet 3", layer `pal250k1946`, year 1946 | map labels "Ein Ghadyan", "Makhfar Ghadyan", "(Disused P.)" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Point for Ein Yotvata / Ein Ghadyan: about 29.879 N, 35.060 E (1951 label 29.8794 N, 35.0613 E; 1946 label 29.8776 N, 35.0555 E). The two labels are 0.6 km apart, so the spring is one place under two names | derived by the agent from the pixel position of each label in the z12 tiles (Web Mercator, 256 px tiles); not a printed coordinate | arithmetic: tile = x0 - 1 + px / 1000 x 3 on a 3 x 3 mosaic resized to 1000 px, then the inverse Web Mercator formula | [S] (agent-derived estimate, about 0.003 degrees) | 2026-11-03 |
| Wikipedia gives Yotvata as 29.89555556 N, 35.06 E, founded 1951 as a Nahal settlement named Ein Radian | en.wikipedia "Yotvata", oldid 1382656951 | "Yotvata was formed in 1951 as a Nahal settlement named Ein Radian." | [S] | 2026-11-03 |
| In the same article the fort over the spring is Iron Age and the spring is called a wellspring | same article | "The fort overlooks the wellspring" | [S] | 2026-11-03 |
| The 1951 sheet prints "Ein Yahav" (a settlement circle) with "Ein Rahel" and "Ein Yahav" as spring labels beside it, at about 30.62 N, 35.19 E | tiles `isr250k-1951/12` x 2447 to 2449, y 1680 to 1682 | map labels "Ein Yahav", "Ein Rahel", "Ein Yahav" (spring label) | [V, agent read 2026-11-03; a person's check pending] for the labels; [S] for the derived point (circle 30.6213 N, 35.1889 E; "Ein Rahel" 30.6334 N, 35.1844 E) | 2026-11-03 |
| The moshav today is at 30.65861111 N, 35.23666667 E, named for "the spring located southwest of the moshav"; "In 1950, an agricultural experimentation station was set up at Ein Yahav by members of Shahal, a movement to settle arid areas of Israel" | en.wikipedia "Ein Yahav", oldid 1376047171 | quoted | [S] | 2026-11-03 |
| The 1951 sheet prints "Ein Hatseva" beside the "Hatseva" mark, at about 30.803 N, 35.262 E (label), 30.811 N, 35.246 E (mark) | tiles `isr250k-1951/12` x 2448 to 2450, y 1678 to 1680 | map labels "Ein Hatseva", "Hatseva" | [V, agent read 2026-11-03; a person's check pending] for the labels; [S] for the derived points | 2026-11-03 |
| North of it the same sheet prints "Ein Ofarim" (about 30.872 N, 35.256 E), "Ein Amatsyahu" and "Ein Aqrabim" | same tiles | map labels as quoted | [V, agent read 2026-11-03; a person's check pending] for the labels; [S] for the point | 2026-11-03 |
| The 1951 sheet prints "Ain el Hufeira" and "J. EL HUFEIRA" with its label east of the boundary line (the spring symbol touches the line), at about 30.676 N, 35.289 E | tiles `isr250k-1951/12` x 2447 to 2449, y 1680 to 1682 | map labels "Ain el Hufeira", "J. EL HUFEIRA" | [V, agent read 2026-11-03; a person's check pending] for the labels; [S] for the derived point | 2026-11-03 |
| The 1951 sheet prints the line along the Arava as "Undemarcated" with a ticked border sign; the springs above sit west of it except Ain el Hufeira (label east of the line) | same tiles (every crop above) | map label "Undemarcated" along the line | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The Wadi Araba road dispute was agreed by 14 February 1951; "only one-third of the disputed stretch of the Beersheba-Eilate road became Jordan territory" | JTA Daily News Bulletin, 16 February 1951, page 3, item "Israel and Jordan Military Leaders Agree on Measures to Halt Border Violations" (`http://pdfs.jta.org/1951/1951-02-16_034.pdf`; web copy `https://www.jta.org/archive/israel-and-jordan-military-leaders-agree-on-measures-to-halt-border-violations`); dateline read on the page image | "JERUSALEM, Feb. 15. (JTA) -- Agreement between military representatives of Israel and Jordan ..." and "In a comment on the agreement reached yesterday on the Wadi Araba dispute" (so 14 Feb) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Five Israeli soldiers were wounded "between Sodom and Ein Hussub on Sept. 21" 1951 (Ein Hussub is not matched here to any spring printed on the maps) | JTA, bulletin of 24 September 1951, item "News Brief" (`https://www.jta.org/archive/five-israeli-soldiers-were-wounded-three-of-them`); the original PDF dateline was not opened | "Five Israeli soldiers were wounded, three of them seriously, in an exchange of fire with unknown attackers, between Sodom and Ein Hussub on Sept. 21." | [V, agent read 2026-11-03; a person's check pending] for the web text; the PDF dateline is [GAP] | 2026-11-03 |
| Palm oasis at a named Arava spring: "Date oasis in Ein Wiba in 1838. Drawing by Eduard Rubinson"; "At Ein Wiba, adjacent to Ein Yahav" | KKL-JNF, "Saving the Wild Date Palms of the Arava", 8 December 2021, `https://kkl-jnf.org/about-kkl-jnf/green-israel-news/press-releases-2021/date-palms-in-the-arava.aspx` | quoted; the page also says the palms "flourished around springs in the central Arava for thousands of years". It does not mention tamarisk | [V, agent read 2026-11-03; a person's check pending] for the palms; tamarisk [GAP] | 2026-11-03 |
| The tamarisk itself at any Arava spring in 1948-51 | none found | no source opened names tamarisk at an Arava spring | [GAP] | 2026-11-03 |
| What the tree signs on the 1946 and 1951 sheets mean, and so whether the groves along the Ein Yotvata road are palm, tamarisk or acacia | full scans from Palestine Open Maps (`https://palopenmaps.org/api/sheets`): Palestine 1:250,000 Sheet 3, 1946, `https://www.dropbox.com/s/z2oc3249byf3uwt/250k-46-Palestine-03.jpg?dl=1`; Israel 1:250,000 Sheet 3, May 1951, `https://www.dropbox.com/s/pybvxqhrfyisk8d/250k-51-Israel-03.jpg?dl=1`; for comparison, Palestine 1:20,000 Sheet 19-14 Jericho (1942 revision), `https://www.dropbox.com/sh/qos20k3m2np6a7y/AAAV0aGijnK7BQMN7KoY6fYwa/19-14.1942-Jericho.jpg?dl=1`, and Palestine 1:100,000 Sheet 16 Jebel Usdum (June 1945 reprint), `https://www.dropbox.com/sh/kmtqcjk1x2yjs4t/AAAKfpUkrMfML_3IcmztqNqla/100k-58-16-Jebel%20Usdum.jpg?dl=1` | Neither 250k legend has a tree or vegetation entry: the 1946 legend's only land-cover entries are Marsh, Qa'a (mudflat) and Sand dunes; the 1951 legend's are Marsh, Mudflat and Sand dunes. Both sheets use two signs: along the road, small round crowns on a short stem, mostly with a tail (a few three-lobed); at the spring, three palm-like signs (a stem with drooping fronds), beside "Ein Ghadyan" on 1946 and beside "Ein Yotvata" on 1951. The Jericho legend shows "Palm Trees" as the frond sign and "Scrub" as the round tailed sign ("Forest" and "Fruit Trees" as three-lobed crowns); the Jebel Usdum "SCRUB" box is drawn with the same round tailed signs. No legend names tamarisk or acacia | [V, agent read 2026-11-11; a person's check pending] for the legends and signs; [S] for the reading palms at the spring and scrub along the road (by comparison; the 1:100,000 series both sheets say they were compiled from was not opened); species [GAP] | 2026-11-11 |

## Disagreements

None between sources, because no two sources were found for the same claim. One naming difference, not a conflict: the spring is
"Ein Ghadyan" on the 1946 sheet and "Ein Yotvata" on the 1951 sheet, and Wikipedia gives the 1951 Nahal outpost the name "Ein
Radian"; the three names sit at the same place on the maps. The Wikipedia spelling "Ein Radian" and the map spelling "Ghadyan"
are both kept as printed.

## Search log

All run on 3 November 2026 from the statecraft box, no Tavily key, so the built-in WebSearch and the sites' own APIs.

| Search | Exact query and range | Returned | What was read |
|---|---|---|---|
| JTA archive, Arava | `https://www.jta.org/wp-json/wp/v2/archive?search=Arava&after=1948-05-01T00:00:00&before=1951-12-31T23:59:59&per_page=20` | x-wp-total 18 | the 8 titles listed on page 1; opened 2 (16 Feb 1951 and 1 Dec 1950) and the 7 Sep 1951 Eilat bus item; none mentions a spring or tamarisk |
| JTA, Arabah | same URL with `search=Arabah` | x-wp-total 1000 (50 pages); the search is fuzzy and the first titles are about "Arab" | not read beyond page 1 titles |
| JTA, Yotvata | same URL with `search=Yotvata` | x-wp-total 0 | none on this spelling; not tried: Ghadian spelled "Ein Radian", Hebrew spellings |
| JTA, Hazeva | same URL with `search=Hazeva` | x-wp-total 71 (4 pages) | page 1 titles only; none opened |
| JTA, Husub | same URL with `search=Husub` | x-wp-total 1 | opened it (24 Sep 1951, see table) |
| JTA, Ghadian | same URL with `search=Ghadian` | x-wp-total 42 (3 pages) | page 1 titles only; none about the Arava |
| JTA, Eilat | same URL with `search=Eilat` | x-wp-total 49 (3 pages) | opened 2: 7 Sep 1951 bus item and 16 Feb 1951 |
| Palestine Open Maps gazetteer | `https://palopenmaps.org/api/localities`, points with lon 34.8 to 35.7 and lat below 31.2 | all Palestinian villages and post-1948 Israeli settlements; no spring is listed | none is a spring |
| Handbook of Palestine 1922 (archive.org `handbookofpalest00luke`, full text) | text search for "tamarisk", "Tamarix", "Ghadian", "Husb", "Arabah" | "Tamarix" three times (a plant list, "Tamarix Jordanis" among species of the Jordan Valley; "Tamarix articulata" in a wattle sentence; "Tamarix tetragyna" on dunes); no Ghadian, Husb or Arabah hit | printed page numbers were not recorded, so no row cites it |
| Internet Archive full-text search | `https://archive.org/services/search/beta/page_production/` with `user_query` "Ghadian tamarisk" (108 hits), "Yotvata tamarisk" (403 hits), "Ain Husb tamarisk" (2173 hits) | fuzzy matches; titles include The Wilderness of Zin, Masters of the Desert, Israel's Southern Landscapes | none opened |
| Glueck, *Rivers in the Desert* (1968), `https://archive.org/details/riversindesert0000nels` | in-book search `fulltext/inside.php` | HTTP 403 (borrowable book) | not read |
| WebSearch (built-in) | "Ein Yotvata spring tamarisk date palms 1950 Arava before Nahal settlement Ein Radian" and "Glueck Ain Ghadian OR Ain Ghadyan tamarisk palms spring Arabah Rivers in the Desert" | summaries only, no page with tamarisk at an Arava spring in 1948-51 | the summary is not a source; nothing cited from it |

## Open gaps

- Tamarisk at any named spring in 1948-51: a botanical or travel source of the period (Glueck's *Rivers in the Desert*, the
  Palestine Exploration Fund or Survey of Israel reports, a Zohary or Feinbrun-Dothan flora with localities) was not reachable here.
- The South Levant 1:100,000 (1943-46) legend, to confirm the signs.
- Ein Wiba (Ein Weibeh) has a date oasis in 1838 per KKL but no point here; the 1951 sheet was not read for it.
- Ein Hussub (JTA spelling): not matched to a spring on the maps; the Hatseva labels are the only Hazeva-area springs read, and the original bulletin dateline of the JTA item is not read.
- Ein Evrona, Ein Netafim, Ein Tamar and Ein Husb under their Hebrew and Arabic names: not searched on the maps beyond the four areas above.
- The 1946 layer in Palestine Open Maps also prints "Undemarcated" along the Arava, odd for a 1946 sheet; a person should check whether that layer is a later printing before the sheet is called 1946.
