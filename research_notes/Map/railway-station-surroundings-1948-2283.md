# What stood beside the 1948 railway stations: first pass, six stations on the Lod to Haifa line (#2283, research part)

Status: **draft, needs a person to check**. This is research only. An AI agent opened every source marked "agent read" below on
3 November 2026 and recorded the day it opened each. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to
check every claim against its source before use. This note does not change `content/`, `sim/` or `unity/`, and it does not
choose where a prop goes: placement follows in a separate PR, and the owner decides it.

## Question and result

#2283 asks, for each station that `content/map/routes_1948.yaml` names, whether in 1948 a citrus packing house, a well or water
point, eucalyptus trees or crated freight stood at or beside it, with a source for each. #2283 allows a first PR for one line
and says the note can grow. This pass covers the Lod to Haifa line: Lod, Rosh HaAyin (Ras al-Ein), Hadera, Binyamina, Atlit
and Rehovot (Rehovot is on the Lod to Rafah line and was read because its tiles were already at hand). The other stations
(Jaffa, Ramla, Beit Shemesh, Battir, Jerusalem, Tel Aviv South, Yibna, Isdud, Dir Snid, Beit Hanoun, Gaza, Deir al-Balah, Khan Yunis,
Rafah, Qaqun, Haifa East, Acre, Nahariya, Afula and the rest) were not read: they stay [GAP] by omission, not by a negative search.

**What closed**

- **Water.** Two stations print a water installation on the 1:20,000 sheet: Ras al-Ein prints "Pumping Sta." (about 350 to 400 m north-west of the station label at this scale) and "W.T." (about 400 m north, by "KM.91"), and Rehovot prints small "w" marks on the plots around the station. The printed labels are read; their legend meaning was not opened
  (see "Open gaps").
- **Planted trees.** Atlit and Rehovot print tree symbols in plots beside the station. The sheet does not name the species, so no
  plot is a eucalyptus windbreak on this evidence.
- **Packing house.** No sheet label reads "packing house" at any of the six stations.

**What did not close:** every eucalyptus, every packing house and every crate-freight row stays [GAP]. A map symbol is not a species
and a missing label is not a negative, so no row says "none stood".

## Answer table

Tags: V (always written in full, as in the rows) means opened and read by the agent; [S] secondary; [C] conflicting, both values
kept; [GAP] still open. The map tile set is `https://palopenmaps.org/tiles/pal20k-1940s/{z}/{x}/{y}.jpg` at zoom 16 (the zoom 17 tiles tried for two stations failed to download); it does not name its edition, and the sheet list (`https://palopenmaps.org/api/sheets`) gives several versions of each sheet number: 14-21 Caesarea, 14-20 Hadera and
13-15 Beit Dajan undated and 1942; 14-23 Atlit undated, 1942 and "45-52"; 13-14 Ramle "1941" and "42-48";
14-16 Majdal Yaba undated, 1941 and 1948. Which version the tile set shows is not stated, so a 1945-52 edition is possible for
Atlit, Rehovot and Rosh HaAyin. The Atlit tiles carry grey hand-lettered overprint ("FaP", "FaB", "WR", "Salt Pans"), so that tile
may not be the plain survey sheet. Station coordinates are the ones in `content/map/routes_1948.yaml` (a Wikipedia or Wikidata
point), used here only to pick the tiles.

| Station and prop | Source and page or URL | Printed text or what the image shows | Tag | Day read |
|---|---|---|---|---|
| Lod (Lydda Junction), station | 1:20,000 sheet 13-15 "Beit Dajan" (tiles z16 x 39115 to 39117, y 26624 to 26626; page image viewed) | label "Lydda Junction Sta." beside a wide fan of tracks | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Lod, packing house, well, eucalyptus, crates | same tiles | no label for any of the four in the 3 by 3 tiles read; a label-free map does not show they were absent | [GAP] | 2026-11-03 |
| Rosh HaAyin (Ras al-Ein), station name | 1:20,000 sheet 14-16 "Majdal Yaba" (tiles z16 x 39126 to 39128, y 26590 to 26592; page image viewed) | label "RAS AL 'EIN STA." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Rosh HaAyin, water point | same tiles | labels "Pumping Sta." (north-west of the station, by the springs' fort "Qal'at Ras al 'Ein") and "W.T." by the track about 400 m north of the station label | [V, agent read 2026-11-03; a person's check pending] for the labels; [GAP] for what "W.T." stands for (the legend was not opened) | 2026-11-03 |
| Rosh HaAyin, packing house, eucalyptus, crates | same tiles | no label; fields marked "c." around the station | [GAP] | 2026-11-03 |
| Hadera, station name | 1:20,000 sheet 14-20 "Hadera" (tiles z16 x 39129 to 39131, y 26516 to 26518; page image viewed) | label "STA." at the south end of the station yard, east of the "Hadera" (wadi) label | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Hadera, trees beside the line | same tiles | a large grove of tree symbols lies west and north-west of the track around "KM.49" and "Gan Shemuel", and a narrow belt east of the track south-east of the "(Stone)" mark; no tree symbols beside the station yard; the species is not printed | [V, agent read 2026-11-03; a person's check pending] for the symbols; [GAP] for species | 2026-11-03 |
| Hadera, packing house, well, crates | same tiles | no label | [GAP] | 2026-11-03 |
| Binyamina, station | 1:20,000 sheet 14-21 "Caesarea" (tiles z16 x 39129 to 39131, y 26502 to 26504; page image viewed) | label "Binyamina STA." with a few small building marks beside the track, on land marked "Ard el Burj" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Binyamina, trees | same tiles | a row of small tree symbols follows both sides of the track running south from the station; a separate tree-symbol strip lies to the north-east; species not printed | [V, agent read 2026-11-03; a person's check pending] for the symbols; [GAP] for species | 2026-11-03 |
| Binyamina, packing house, well, crates | same tiles | no label | [GAP] | 2026-11-03 |
| Atlit, station name | 1:20,000 sheet 14-23 "Atlit" (tiles z16 x 39127 to 39129, y 26463 to 26465; page image viewed) | label "STA." beside the track, "Atlit" village label to the south-west, "Sch" (school) and "T.F." marks near | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Atlit, industry beside the station | same tiles | label "Salt Factory" west of the station (a salt works, not a packing house) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Atlit, trees | same tiles | green plots with tree symbols immediately north-east and east of the station; species not printed | [V, agent read 2026-11-03; a person's check pending] for the symbols; [GAP] for species | 2026-11-03 |
| Atlit, packing house, well, crates | same tiles | no label | [GAP] | 2026-11-03 |
| Rehovot, station name | 1:20,000 sheet 13-14 "Ramle" (tiles z16 x 39103 to 39105, y 26632 to 26634; page image viewed) | label "STA." at the road and railway crossing | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Rehovot, water or well marks | same tiles | small "w" and "W" marks on the plots around the station (the closest is a "w" a short way west of it) | [V, agent read 2026-11-03; a person's check pending] for the marks; [GAP] for their legend meaning | 2026-11-03 |
| Rehovot, trees | same tiles | a clump of round tree symbols immediately west of the station and a short vertical row east of it; species not printed | [V, agent read 2026-11-03; a person's check pending] for the symbols; [GAP] for species | 2026-11-03 |
| Rehovot, packing house, crates | same tiles | no label | [GAP] | 2026-11-03 |
| Station lists in 1922 (context) | *The Handbook of Palestine*, Macmillan, London, 1922 (archive.org `handbookofpalest00luke`, djvu text layer lines 6494 to 6502, section "Inland communications"; the page number did not read cleanly, so not given) | "From Kantara East to Haifa : ... Rehoboth, Bir Salem, Ludd, Kafr Jinnis, Ras al-Ain, Kalkilieh, Tulkeram, Khedera, Zicron Jacob, Athlit, Kafr al-Semir, Haifa." The list has no Binyamina. Whether "Zicron Jacob" is the later Binyamina site is not stated in this book | [V, agent read 2026-11-03; a person's check pending]; the identity is [GAP] | 2026-11-03 |
| Eucalyptus on Jewish colony land, 1922 (context, not a station row) | same book, the djvu text layer line 3360 and line 3415 | "planted eucalyptus and pines" (colonies, general) and "4,566 donums (about 1,141 acres) eucalyptus plantations" (a total for the colonies, no place) | [V, agent read 2026-11-03; a person's check pending] for the wording; places: [GAP] | 2026-11-03 |

## Disagreements

None found in this pass. The undated and 1942 versions of each sheet were not compared with each other, so a difference between
them would not have been seen. See "Open gaps".

## Search log

All searches below were run on 3 November 2026 with `curl` (this box has no Tavily key). A count is the `x-wp-total` header of
the response, not a count of items opened.

| Query | URL | Range | Count returned | Opened |
|---|---|---|---|---|
| JTA archive, "Binyamina" | `https://www.jta.org/wp-json/wp/v2/archive?search=Binyamina&after=1945-01-01T00:00:00&before=1948-12-31T23:59:59&per_page=20` | 1945-01-01 to 1948-12-31 | 17 | none opened; titles (train blast, rail sabotage) are about attacks, not station surroundings |
| JTA archive, "Benjamina" | same, `search=Benjamina` | same | 146 | none opened; the search is fuzzy and the first ten titles are unrelated |
| JTA archive, "packing house" | same, `search=packing%20house` | same | 326 | none opened; the first ten titles are unrelated, so the search does not isolate the phrase |
| JTA archive, "Rehovoth station" | same, `search=Rehovoth%20station` | same | 55 | none opened; the first ten titles are unrelated |

No JTA item was opened, so none is cited. The search does not match phrases, so a high count says nothing about a hit.

## Open gaps

- **Uninterpreted marks.** The "c." printed on fields around every station is not interpreted here; Atlit's plots use conifer-shaped
  symbols and Rehovot's rounded ones, which may mean different things (legend not opened).
- **Legend.** The printed meaning of "w", "W", "W.T.", "T.F.", "Pumping Sta." and the tree symbols on the 1:20,000 sheets was not
  opened (no legend sheet found in the tile set). Until it is, the water rows are labels only, and no tree symbol is a species.
- **Edition.** The tile set's edition is not named; three of the sheets also have 1945-52 versions (see the table's opening paragraph). The rows say so.
- **Packing houses, crates.** No map label, no text. Candidates not yet searched: the Palestine Railways annual reports, the
  Citrus Marketing Board lists of packing houses, and dated photographs.
- **Stations not read.** All stations named in the "Question and result" paragraph.
- **Photographs.** No photograph of any station was opened. Any photograph found later and not opened counts as a gap, not a claim.
