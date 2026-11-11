# Village Statistics 1945: the 25 town populations checked against the scan (#2450, research note, 2026-11-11)

Status: **draft, needs a person to check**. AI-assisted reading: an AI agent read the scan images by eye on 11 November
2026. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check every claim against its source
before use. This note does not change `content/`: the owner clears the `owner-verify` flags, and a follow-up PR can
change figures or notes once the owner rules.

File checked: [`content/map/settlement_population_1945.yaml`](../../content/map/settlement_population_1945.yaml) at
`main` 5711144 (the file last changed on 11 October 2026). Its header says every figure was read from the Wikipedia
table of the publication because no scan was reachable on 11 October 2026.

## Result

- Match: 23.
- Differs: 0.
- Not found: 2 (Revivim, Yad Mordechai). Neither is in the 1945 book; the file already has `population: null` for
  both, so it agrees with the source.

Beersheba, the row the file calls garbled: the scan prints the total as "5.570", with a full stop, not a comma. The
parts add up: Moslems 5,360 + Jews none + Christians 200 + Others 10 = 5,570. So 5,570 is the scan's figure. [V]

## Source

Government of Palestine, *Village Statistics 1945* ("as at 1.4.45"), the original 1945 printing (not Hadawi's 1970
reprint). The scan is hosted by Brendan McKay (ANU), who says the National Library of Israel copy is no longer on
public view.

- Index page: https://users.cecs.anu.edu.au/~bdm/yabber/yabber_census.html (opened 11 November 2026).
- Full PDF: https://users.cecs.anu.edu.au/~bdm/yabber/census/VillageStatistics1945orig.pdf (opened 11 November 2026;
  34 PDF pages, 52,198,759 bytes, SHA-256 `b265a1dab532486945f3840bb435bc9f2465188abd41173bf4af37de95ece51e`).
- Population-column crops of the same scan, one per sub-district:
  `https://users.cecs.anu.edu.au/~bdm/yabber/census/VillageStatistics1945/<SubDistrict>.jpg` (Acre, Beersheba, Beisan,
  Gaza, Haifa, Jaffa, Jerusalem, Nazareth, Ramle, Safad, Tiberias, Tulkarm; opened 11 November 2026).

## Conventions

- **Page** is the printed page number in the top right corner of each scanned page. PDF page = printed page + 1. A
  row's page was set by the first serial number printed on each "(continued)" page (for example, Jaffa continued,
  p. 28, starts at serial 42).
- **Total** is the last population column. The book's columns are Moslems, Jews, Christians, Others, Total.
- **[V]**: the scan's cell was read directly; page and serial are given.
- **[GAP]**: searched and not found.
- Line numbers are for `settlement_population_1945.yaml` at 5711144.

## Rows

| # | Town (our id) | Our value (line) | Scan value | Page, sub-district, serial | Scan cell (short quote) | Verdict |
|---|---|---|---|---|---|---|
| 1 | jerusalem | 157080 (11) | 157,080 | p. 24, Jerusalem, no. 51 | "Jerusalem (Urban) ... 157,080" | Match [V] |
| 2 | tel_aviv | 166660 (18) | 166,660 | p. 28, Jaffa (continued), no. 55 | "Tel Aviv (Urban) ... 166,660" | Match [V] (note d) |
| 3 | jaffa | 94310 (25) | 94,310 | p. 27, Jaffa, no. 17 | "Jaffa (Urban) ... 94,310" | Match [V] |
| 4 | haifa | 138300 (32) | 138,300 | p. 13, Haifa, no. 29 | "Haifa (includes Ahuzzat Sir Herbert Samuel) (Urban) ... 138,300" | Match [V] |
| 5 | acre | 12360 (39) | 12,360 | p. 4, Acre, no. 2 | "Acre (Urban) ... 12,360" | Match [V] |
| 6 | tiberias | 11310 (46) | 11,310 | p. 12, Tiberias, no. 40 | "Tiberias (Urban) ... 11,310" | Match [V] |
| 7 | safed | 11930 (53) | 11,930 | p. 11, Safad (continued), no. 68 | "Safad (Urban) ... 11,930" | Match [V] |
| 8 | nazareth | 14200 (60) | 14,200 | p. 8, Nazareth, no. 32 | "Nazareth (Urban) ... 14,200" | Match [V] |
| 9 | lod | 16780 (67) | 16,780 | p. 30, Ramle (continued), no. 58 | "Lydda (Urban) ... 16,780" | Match [V] |
| 10 | ramla | 15160 (74) | 15,160 | p. 30, Ramle (continued), no. 77 | "Ramle, Er (Urban) ... 15,160" | Match [V] |
| 11 | gaza | 34250 (81) | 34,250 | p. 31, Gaza, no. 23 | "Gaza (Urban) ... 34,250" | Match [V] |
| 12 | beersheba | 5570 (88) | 5,570 (printed "5.570") | p. 33, Beersheba (town list, no serial) | "Beersheba 5,360 - 200 10 5.570" | Match [V] |
| 13 | petah_tikva | 17250 (96) | 17,250 | p. 27, Jaffa, no. 36 | "Petah Tiqva (Urban) ... 17,250" | Match [V] |
| 14 | rishon_lezion | 8100 (103) | 8,100 | p. 30, Ramle (continued), no. 82 | "Rishon le Zion (Urban) ... 8,100" | Match [V] |
| 15 | zikhron_yaakov | 1740 (110) | 1,740 | p. 15, Haifa (continued), no. 84 | "Zikhron Ya'aqov ... 1,740 (n)" | Match [V] (note a) |
| 16 | rosh_pinna | 340 (116) | 340 | p. 10, Safad (continued), no. 66 | "Rosh Pinna ... 340" | Match [V] |
| 17 | rehovot | 10020 (122) | 10,020 | p. 30, Ramle (continued), no. 80 | "Rehovot (Urban) ... 10,020" | Match [V] |
| 18 | hadera | 7830 (129) | 7,830 | p. 13, Haifa, no. 27 | "Hadera (Urban) ... 7,830" | Match [V] (note b) |
| 19 | netanya | 5070 (136) | 5,070 | p. 21, Tulkarm (continued), no. 70 | "Natanya (Urban) ... 5,070" | Match [V] (note c) |
| 20 | degania_alef | 290 (143) | 290 | p. 12, Tiberias, no. 7 | "Deganiya "A" ... 290" (no. 8, Deganiya "B", is 380) | Match [V] |
| 21 | ein_harod | 1060 (151) | 1,060 | p. 6, Beisan, no. 12 | "'Ein Harod ... 1,060" | Match [V] |
| 22 | nahalal | 1090 (157) | 1,090 | p. 8, Nazareth, no. 30 | "Nahalal and Shimron ... 1,090" | Match [V] (combined row, as the file says) |
| 23 | hanita | 240 (165) | 240 | p. 4, Acre, no. 16 | "Hanita ... 240" | Match [V] |
| 24 | revivim | null (171) | not listed | p. 33, Beersheba | Town list: 'Auja El, Beersheba, Beit Eshel, Gevulot, Jammama, Ruhama, Tel Tsofim; no Revivim | Not found [GAP] |
| 25 | yad_mordechai | null (178) | not listed | pp. 31-32, Gaza, nos. 1-68 | No Yad Mordechai row; no. 19, Deir Suneid, is 730 Moslems, no Jews | Not found [GAP] |

## What a row covers

Some totals include nearby places, per the scan's own footnotes. This matters before anyone sizes a town's kit by
them.

- a. Zikhron Ya'aqov, footnote (n), p. 15: "Includes Zikhron Ya'aqov-1,510, Maayan Tzevi-130, Tel Tzur-100". The town
  alone is 1,510.
- b. Hadera, footnote (c), p. 13: "Includes Hadera-7,500 Jews, Gan Ssmuel-310 Jews" (spelling as printed).
- c. Natanya, footnote (b), Tulkarm: "Includes: Nathanya-4,900, Ramat Tiomkin-170".
- d. Tel Aviv's Jews cell is printed "166000" (no comma); the total cell is "166,660".

## Still open

- Revivim and Yad Mordechai (rows 24-25): both were founded in 1943, and the 1945 book does not list them. A figure
  would need a later source; none was looked for here. [GAP]
- Hadawi's 1970 reprint was not opened (the ANU page says it merges some columns). The Wikimedia Commons copy
  (`File:Village_Statistics_1945_full.pdf`) was found in a search but not opened.
- The crops were the main read. The full PDF was used to confirm page headers and spot values (Lydda 16,780 on p. 30,
  Beersheba "5.570" on p. 33, the Gaza Sub-District on p. 31); the crops and the PDF were not compared row by row.
