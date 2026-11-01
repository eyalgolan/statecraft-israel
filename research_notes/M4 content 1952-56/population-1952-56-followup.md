# M4 content research: arrivals by country of origin, 1952 to 1956 (follow-up to population-1952-56.md)

Research only. Researched and every page below opened on 2026-10-31 (a second research pass, after the first pass of 2026-10-23 in `population-1952-56.md`). Nothing here is a decision about `content/aliyah.yaml`, `content/population.yaml`, the emigration code or the calibration test, which stay paused (#102). This note closes the one row `population-1952-56.md` section 7 left as [GAP]: per-year arrivals by country of origin, finer than continent. That note's section 1 said "No per-year country-of-origin table was found" and its access notes said no Statistical Abstract volume for these years was found online. Both statements are left as written; the volumes below did open from this box on 2026-10-31 (see "Sources opened").

**Conventions** (same letters as `population-1952-56.md`)
- **[V, agent read 2026-10-31; a person's check pending]** read on a rendered image of the cited page (`gs -sDEVICE=pnggray -r200`, cropped at up to 500 dpi), and every group of rows checked against the table's own totals.
- **[C]** sources conflict or a printed table is internally inconsistent. Both readings are given.
- **[GAP]** not found.
- **derived here** is plain arithmetic on printed figures. It is never tagged verified.
- **our reading** is an interpretation, not CBS's.
- Quotes are copied from the printed page. Nothing is invented.

**Two series, kept apart.** Each year's table prints two separate columns, and they are never added together here:
- **Series A, "Immigrants Entering" (1952, 1953) or "Immigrants Arriving" (1954 to 1956):** Jewish immigrants, from the table title "Jewish Immigrants".
- **Series B, "Travellers Settling":** Jewish travellers (tourists) who stayed and were counted as settling, a different column of the same table.

The two CBS whole-population series of `population-1952-56.md` section 2 (SA No. 14 table 2, SA 2010 table 4.2) are a different, later layer and are only compared in section 4 below.

**Reading caveat.** These are scans, and the typeface prints 3, 8, 6 and 0 almost alike. A cell is accepted only if its group sums to the printed continent total and, where the cumulative column "15 V 1948 to 31 XII <year>" was legible, this year's cumulative figure minus the year before's gives the same Series A figure. Where the first read failed a sum, the digit was fixed to the reading that satisfies it. Computed here. The units are persons; the tables print no unit label.

## Sources opened (all by plain HTTP GET, default headers, HTTP 200 on 2026-10-31)

| Source | Table and printed page | PDF page |
|---|---|---|
| SA of Israel no. 4 (1952/53), https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1953_num4.pdf | Table 5 "Jewish Immigrants, by Country of Origin (1919-1952)", p. 28 | 42 |
| SA no. 5, https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1954_num5.pdf | Table 5 "Jewish Immigrants Registered by Country of Birth (1919-1953)", p. 27 | 51 |
| SA no. 6, https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1955_num6.pdf | Table 5 "Jewish Immigrants Registered, by Country of Birth (1919-1954)", p. 33 | 62 |
| SA no. 7, https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1956_num7.pdf | Table 4 "Jewish Immigrants Registered, by Country of Birth (1919-1955)", p. 34 | 83 |
| SA no. 8, https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1957_num8.pdf | Table 4 "Jewish Immigrants Registered, by Country of Birth (1919-1956)", p. 33; Table 12 "Jewish Immigrants Arriving, by Country of Birth, Sex and Age (1956)" | 80; 85 |
| SA no. 9, https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1958_num9.pdf | Table 6 "Jewish Immigrants and Travellers Settling by Country of Birth (1919-1957)", p. 60 | 101 |
| SA No. 14 (1963), https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1963_num14.pdf | Table 3, p. 109; Table 4, p. 110 | 134; 135 |
| SA 2010 table 4.4, https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_04.pdf | pp. 233-234 | 1-2 |

Also fetched and found to exist under the same directory, not read for this note: `shnaton1959_num10.pdf` to `shnaton1962_num13.pdf` (all HTTP 200, application/pdf).
The starter list's `e_print.pdf` (Publication 1365) was fetched (HTTP 200, 80 pages) and not used: its tables cover 2002-2006 and the 1952-56 series above is more direct.
The CBS reuse notice in each volume was not read for this note, so a person's check of the reuse terms is pending; figures and short phrases are quoted with the source named.

## 1. Series A: Jewish immigrants arriving, by country of birth, per year

Source tables as in the list above, columns "Immigrants Entering" (1952, 1953) and "Immigrants Arriving" (1954 to 1956). [V, agent read 2026-10-31; a person's check pending]

Printed footnote (1), as in SA no. 4 to no. 6 (the SA no. 8 page words it "registered immigrants by countries in 1919-1934" but says the same): "The distribution of immigrants registered in 1919-1934 and of visa-less immigrants in 1946 is based on citizenship; in other years, on country of birth." Footnote (3) of the 1954 table, on the Tunisia, Algeria, Morocco row: "Including Tangier."
`n/p` means the country is not printed as its own row in that year's table, so it sits inside the "Other Countries" row of its continent. `..` and `-` are as printed.

| Country of birth | 1952 | 1953 | 1954 | 1955 | 1956 |
|---|---|---|---|---|---|
| All countries | 23,375 | 10,347 | 17,471 | 36,303 | 54,925 |
| **Asia** | 6,701 | 2,871 | 3,305 | 1,323 | 2,739 |
| Turkey | 353 | 270 | 138 | 223 | 1,293 |
| Iraq | 961 | 421 | 250 | n/p | n/p |
| Iran | 4,187 | 1,091 | 472 | 126 | 647 |
| Yemen | 87 | 27 | 584 | 11 | 7 |
| Aden | 53 | 58 | 40 | 4 | 9 |
| India | n/p | 367 | 1,506 | 515 | 253 |
| Other Asia | 1,060 | 637 | 315 | 444 | 530 |
| **Africa** | 10,024 | 4,889 | 12,188 | 32,413 | 44,878 |
| Tunisia, Algeria, Morocco (with Tangier from 1955) | 7,560 | 3,628 | 11,033 | 31,608 | 43,769 |
| Libya | 1,187 | 236 | 186 | 129 | 106 |
| Union of South Africa | 17 | 22 | 29 | 43 | 180 |
| Other Africa | 1,260 | 1,003 | 940 | 633 | 823 |
| **Europe** | 6,131 | 2,025 | 1,325 | 1,942 | 6,674 |
| U.S.S.R. | 156 | 173 | 151 | 178 | n/p |
| Lithuania, Latvia and Estonia | 42 | 43 | n/p | n/p | n/p |
| Poland | 615 | 398 | 268 | 394 | n/p |
| Rumania | 3,627 | 80 | 99 | 253 | n/p |
| Bulgaria | 450 | 338 | 185 | 187 | n/p |
| Yugoslavia | 93 | 16 | 11 | 14 | 22 |
| Greece | 67 | 93 | 40 | 27 | 29 |
| Germany | 193 | 136 | 94 | 112 | 85 |
| Austria | 67 | 49 | 35 | 41 | 31 |
| Czechoslovakia | 84 | 30 | 39 | 63 | n/p |
| Hungary | 218 | 224 | 55 | 260 | n/p |
| United Kingdom | 159 | 112 | 97 | 83 | 103 |
| Netherlands | 115 | 95 | 67 | 43 | 26 |
| France | 133 | 114 | 108 | 118 | 121 |
| Italy | 41 | 40 | 24 | 26 | 31 |
| Other Europe | 71 | 84 | 52 | 143 | 6,226 |
| **America and Oceania** | 516 | 549 | 641 | 620 | 631 |
| United States | 90 | 75 | 93 | 106 | 56 |
| Canada | 37 | 17 | 34 | 28 | 9 |
| Argentine | n/p | 290 | 296 | 249 | 362 |
| Other America | 382 | 152 | 207 | 222 | 201 |
| Australia and New Zealand | 7 | 15 | 11 | 15 | 3 |
| Not stated | 3 | 13 | 12 | 5 | 3 |

Rows sum to the printed continent totals, and the continents plus "Not stated" sum to the printed "All Countries" total, in every year. Computed here. SA No. 14 (1963) table 4, p. 110, "Immigrants and Tourists Settling, by Continent of Birth (Jews)", prints the same continent counts a second time for 1953 to 1956 ("All Continents" 10,347; 17,471; 36,303; 54,925; Africa 4,889; 12,188; 32,413; 44,878; Asia 2,871; 3,305; 1,323; 2,739). [V, agent read 2026-10-31; a person's check pending] That table has no row labelled 1952 on the page read.

**Changes of category between years** (printed, so a reader must not compare a row across years without them):
- Iraq is its own row 1952 to 1954 and inside "Other Countries" in 1955 and 1956. India is its own row from 1953; in 1952 it is inside "Other Countries" of Asia.
- Lithuania, Latvia and Estonia is its own row only in 1952 and 1953.
- Argentine is its own row from 1953.
- **1956: the SA no. 8 table prints no row for U.S.S.R., Poland, Rumania, Bulgaria, Czechoslovakia or Hungary.** They are inside "Other Countries" of Europe, 6,226 persons. The page also prints that row's cumulative figure for 15 V 1948 to 31 XII 1956 as 819,040 on the render read (a 3 and an 8 cannot be told apart in this scan). The six countries' printed 1955 cumulative figures plus the 1955 "Other Countries" cumulative figure (8,455 + 107,787 + 121,968 + 38,413 + 18,943 + 15,056 + 2,192 = 312,814) plus 6,226 give 319,040, computed here, so the cell is 319,040.
- The label "Tunisia, Algeria, Morocco" gains "& Tangier" in the 1955 table; in 1954 it carries footnote (3) "Including Tangier".

**[C] 1953 India and Other Asia.** The 1953 table prints India 153 travellers and 367 immigrants, and Other Countries 11 and 637, with cumulative columns 532 / 9,027 and 26 / 2,848. The 1954 table prints India 5 / 1,506 and cumulative 31 / 4,354, Other Countries 103 / 315 and cumulative 635 / 9,342. 1954 cumulative minus 1954 year gives India 1948-53 as 26 and 2,848, and Other as 532 and 9,027 (computed here). So in the 1953 table the cumulative columns sit against the opposite labels to the 1954 table. **Our reading:** the two row labels are swapped on that page, which would make India 1953 11 travellers and 637 immigrants and Other Asia 153 and 367. The table above and the one in section 2 keep the labels as printed. The Asia total (2,871 immigrants) is the same either way. The other reading, that the cumulative cells are swapped and the 1953 year cells are right as printed, fits the arithmetic equally; the neighbouring years favour the label swap only mildly. [C]

## 2. Series B: Jewish travellers settling, by country of birth, per year

Same tables, column "Travellers Settling". Kept apart from section 1 and never added to it. [V, agent read 2026-10-31; a person's check pending]

| Country of birth | 1952 | 1953 | 1954 | 1955 | 1956 |
|---|---|---|---|---|---|
| All countries | 994 | 979 | 899 | 1,175 | 1,309 |
| **Asia** | 331 | 248 | 175 | 283 | 571 |
| Turkey | 80 | 55 | 46 | 116 | 417 |
| Iraq | 143 | .. | .. | n/p | n/p |
| Iran | 49 | 23 | 16 | 18 | 26 |
| Yemen | - | - | - | 1 | - |
| Aden | 7 | 6 | 5 | 16 | 16 |
| India | n/p | 153 | 5 | 3 | 5 |
| Other Asia | 52 | 11 | 103 | 129 | 107 |
| **Africa** | 108 | 106 | 155 | 233 | 260 |
| Tunisia, Algeria, Morocco (with Tangier from 1955) | 38 | 35 | 71 | 162 | 184 |
| Libya | 8 | 14 | 13 | 7 | 4 |
| Union of South Africa | 24 | 7 | 27 | 15 | 25 |
| Other Africa | 38 | 50 | 44 | 49 | 47 |
| **Europe** | 466 | 539 | 491 | 564 | 374 |
| U.S.S.R. | 23 | 31 | 48 | 36 | n/p |
| Lithuania, Latvia and Estonia | 18 | 14 | n/p | n/p | n/p |
| Poland | 162 | 180 | 146 | 182 | n/p |
| Rumania | 43 | 53 | 40 | 37 | n/p |
| Bulgaria | 7 | 5 | 3 | 5 | n/p |
| Yugoslavia | 3 | 2 | 7 | 3 | 7 |
| Greece | 16 | 31 | 10 | 21 | 9 |
| Germany | 43 | 42 | 55 | 58 | 27 |
| Austria | 15 | 20 | 21 | 19 | 7 |
| Czechoslovakia | 16 | 22 | 23 | 18 | n/p |
| Hungary | 21 | 21 | 22 | 17 | n/p |
| United Kingdom | 14 | 22 | 33 | 35 | 30 |
| Netherlands | 11 | 11 | 18 | 31 | 8 |
| France | 34 | 33 | 26 | 35 | 26 |
| Italy | 7 | 7 | 7 | 2 | 10 |
| Other Europe | 33 | 45 | 32 | 65 | 250 |
| **America and Oceania** | 89 | 86 | 74 | 94 | 104 |
| United States | 72 | 37 | 61 | 50 | 42 |
| Canada | 5 | 3 | 3 | 9 | 2 |
| Argentine | n/p | 8 | 2 | 14 | 35 |
| Other America | 12 | 35 | 7 | 20 | 24 |
| Australia and New Zealand | - | 3 | 1 | 1 | 1 |
| Not stated | - | - | 4 | 1 | - |

The same sum checks hold for this column in every year. Computed here. The 1953 India and Other Asia rows carry the same label question as above [C]. The year totals 994, 979, 899, 1,175 and 1,309 are the "thereof: Tourists Settling" figures of SA No. 14 (1963) table 3, p. 109, which prints 994 (1952), 979, 899, 1,175 and 1,309 (1956). [V, agent read 2026-10-31; a person's check pending]

## 3. 1956 Morocco and Tunisia split, and a derived split of the 1956 European "Other"

**Maghreb 1956, printed.** SA no. 8 table 12, "Jewish Immigrants Arriving, by Country of Birth, Sex and Age (1956)", all immigrants row: Morocco & Tangier 36,281; Tunisia & Algeria 7,488; Africa total 44,878; Asia total 2,742; Europe 6,674; America & Oceania 631; all countries 54,925. [V, agent read 2026-10-31; a person's check pending] 36,281 plus 7,488 is 43,769, the table 4 figure for the combined row (computed here). Asia is 2,742 here against 2,739 in table 4. **Our reading:** the three "Not stated" persons of table 4 are inside Asia in table 12 (2,739 plus 3, computed here). SA no. 7's table 12 is headed "Summary 1954-1955" and its all-countries total reads 53,774 on the text layer only (read on the image in section 7.1 after this sentence was first written), which equals 17,471 plus 36,303 (computed here), so no single-year Morocco and Tunisia split was read for 1953 to 1955. [GAP]

**European countries inside the 1956 "Other Countries" (6,226), derived here, not printed.** SA no. 9 table 6, p. 60, prints for 1957 and for 15 V 1948 to 31 XII 1957. Cumulative to 1957 minus 1957 minus the printed cumulative to 1955 (SA no. 7 table 4, p. 34) leaves 1956. [V, agent read 2026-10-31; a person's check pending] for the printed inputs. The last column is derived here.

| Country | cumulative to 1957 (SA no. 9) | 1957 (SA no. 9) | cumulative to 1955 (SA no. 7) | 1956, derived here |
|---|---|---|---|---|
| U.S.S.R. | 11,994 | 3,048 | 8,455 | 491 |
| Poland | 136,820 | 25,497 | 107,787 | 3,536 |
| Rumania | 123,562 | 875 | 121,968 | 719 |
| Bulgaria | 38,599 | 69 | 38,413 | 117 |
| Czechoslovakia | 19,161 | 119 | 18,943 | 99 |
| Hungary | 23,263 | 7,203 | 15,056 | 1,004 |
| Other Europe | 3,611 | 1,164 | 2,192 | 255 |

The six named countries sum to 5,966 and with the 255 to 6,221, which is 5 below the printed 6,226. SA no. 9 prints Europe cumulative to 1957 as 388,458, and 349,569 (SA no. 8) plus 38,894 (SA no. 9, 1957) is 388,463, and its "Not stated" cumulative is 19,419 where SA no. 8's 18,810 plus 604 is 19,414 (all computed here). **Our reading:** CBS moved 5 persons from Europe to "Not stated" between the two volumes, so the derived split carries a revision of up to 5 in total and is a value for the research record only. These seven values come from three printings, not from a 1956 table, and are never tagged verified.

## 4. Where the per-year series sit against the whole-population series

Computed here from sections 1 and 2 and the first note (SA No. 14 (1963) table 2, SA 2010 table 4.2). Series A plus B is the "Immigrants Registered, including tourists settling" idea of SA No. 14 table 3 and is shown only to compare; it is not a CBS-labelled column in these country tables.

| Year | A: Jewish immigrants arriving | B: Jewish travellers settling | A plus B | SA No. 14 table 2, all population | SA 2010 table 4.2, all population |
|---|---|---|---|---|---|
| 1952 | 23,375 | 994 | 24,369 | 23,408 | 24,610 |
| 1953 | 10,347 | 979 | 11,326 | 10,388 | 11,575 |
| 1954 | 17,471 | 899 | 18,370 | 17,485 | 18,491 |
| 1955 | 36,303 | 1,175 | 37,478 | 36,327 | 37,528 |
| 1956 | 54,925 | 1,309 | 56,234 | 54,996 | 56,330 |

SA No. 14 (1963) table 3, p. 109, "Immigrants Registered (2)" with footnote "(2) Including tourists settling", reads on the render 24,869, 11,826, 18,370, 37,478 and 56,234 for 1952 to 1956. Series A plus B gives 24,369 and 11,326 for the first two, so the 8 in those two cells is the 3-or-8 glyph and the arithmetic reading is used. [C] on those two cells only. [V, agent read 2026-10-31; a person's check pending]

The 2010 whole-population figure is above A plus B by 241, 249, 121, 50 and 96 persons (1952 to 1956, computed here), and the 1963 table 2 figure is above A alone by 33, 41, 14, 24 and 71. **Our reading:** these small excesses are the non-Jewish and other immigrants and, for 2010, the "potential immigrants" its footnote 1 includes. CBS does not reconcile them. A reader who needs one series should say which, as `population-1952-56.md` section 2 already warns.

## 5. Series C: SA 2010 table 4.4, country of birth, but only as the period 1952 to 1960

SA 2010 table 4.4 "Immigrants (1), by Period of Immigration, Country of Birth and Last Country of Residence", pp. 233-234, column "1952-1960" (the neighbouring column is "15 V 1948-1951"). Footnote 1: "See note 1 to Table 4.2" (that note is quoted in `population-1952-56.md` section 2). [V, agent read 2026-10-31; a person's check pending]

| Country of birth | 1952-1960 |
|---|---|
| Grand total | 297,138 |
| Morocco | 95,945 |
| Tunisia | 23,569 |
| Algeria | 3,433 |
| Libya | 2,079 |
| Egypt, Sudan | 17,521 |
| Iran | 15,699 |
| Turkey | 6,871 |
| Iraq | 2,989 |
| Yemen (incl. South Yemen) | 1,170 |
| India (incl. Pakistan and Sri-Lanka) | 5,380 |
| Poland | 39,618 |
| Romania | 32,462 |
| Hungary | 9,819 |
| U.S.S.R. (former), European part | 13,743 |

This is a nine-year bucket, so it cannot be split into single years, and its country sets differ from Series A (Morocco, Tunisia and Algeria are separate here and joined there; Egypt and Sudan, Argentina and others are separate here). It is kept apart from Series A and B and is not a per-year source. The per-year tables of SA 2010 chapter 4 that were read are 4.2 and 4.4, both already cited in `population-1952-56.md`.

## 6. Open

- [GAP] Single-year Morocco / Tunisia-Algeria split for 1953 to 1955 on a CBS basis. Section 7 gives the printed 1954-1955 two-year split and a secondary, Jewish Agency based Morocco series.
- [GAP] Single-year Poland, Rumania, Hungary, Bulgaria, Czechoslovakia and U.S.S.R. for 1956 as printed. Section 3 gives a derived split only; section 7 records what the second pass searched.
- [GAP] Country detail for Series B in 1956 inside "Other Countries" of Europe (250 travellers).
- [C] 1953 India and Other Asia labels (section 1).
- A person's check of every page above is pending; the reuse terms of the older volumes are also pending a person's check.

## 7. Second pass, 2026-11-01: Morocco and Tunisia for 1953 to 1955, and the 1956 "Other Countries" rows

Same conventions as above. Pages opened on 2026-11-01 are listed in the table below.

| Source | What was opened | Day |
|---|---|---|
| SA no. 7 (`shnaton1956_num7.pdf`), Table 12 "Jewish Immigrants Arriving, by Country of Birth, Sex and Age", sub-title "(Summary 1954-1955)", printed p. 39, PDF page 88 | rendered at 200 dpi and read on the image | 2026-11-01 |
| SA no. 5, 6, 8 to 14 (same directory, no. 5 to 14) | text-layer search for "Tunisia", "Morocco" and "Tangier"; not every page read on an image | 2026-11-01 |
| moroccanjews.org (Visiting Jewish Morocco), page "Data on Immigration to Israel from Morocco", under its Jewish Emigration from Morocco section | page text | 2026-11-01 |
| English Wikipedia "Aliyah", oldid 1378651266, table "Immigration to Israel in the years following the May 1948 Israeli Declaration of Independence" | wikitext of that revision | 2026-11-01 |
| JTA, "55,000 Immigrants Entered Israel in 1956, Jewish Agency Reports", 2 January 1957, https://www.jta.org/archive/55000-immigrants-entered-israel-in-1956-jewish-agency-reports | article text | 2026-11-01 |

### 7.1 Printed: Morocco and Tunisia for 1954 and 1955 together

SA no. 7 Table 12, "All Immigrants" row, columns as printed. [V, agent read 2026-11-01; a person's check pending]

| Column | 1954-1955 together |
|---|---|
| Europe, America and Oceania | 4,528 |
| Africa: Other Countries | 1,960 |
| Africa: Morocco and Tangier | 33,231 |
| Africa: Tunisia and Algeria | 9,410 |
| Africa: Total | 44,601 |
| Asia: Other Countries | 2,607 |
| Asia: India | 2,021 |
| Asia: Total | 4,628 |
| All Countries (footnote 1: "Including 17 immigrants whose country of birth is not stated") | 53,774 |

Checks, computed here: 1,960 plus 33,231 plus 9,410 is 44,601; 2,607 plus 2,021 is 4,628; 4,528 plus 44,601 plus 4,628 plus 17 is 53,774. Section 1's Series A gives Africa 12,188 (1954) plus 32,413 (1955) = 44,601, Asia 3,305 plus 1,323 = 4,628, and "Tunisia, Algeria, Morocco" 11,033 plus 31,608 = 42,641, which is 33,231 plus 9,410. So this table splits the Series A Maghreb row into two parts for the two years together, and prints no split for either year alone. No other table of SA no. 5 to 14 that the text-layer search found prints Morocco or Tunisia as an immigrant column or row for a single year of 1953 to 1955. The Tunisia rows that did turn up in those volumes (for example "Tunisia 147 111 207 239") sit in emigrant and departing-resident tables. [GAP] for the single-year split on a CBS basis.

### 7.2 Secondary: Morocco by single year, and 1952-53 by country

These are not CBS tables and are tagged [S]. Neither source was checked against its own underlying source here.

| Year | Morocco, moroccanjews.org (printed "Source: Jewish Agency") | Morocco, Wikipedia oldid 1378651266 | Tunisia, Wikipedia | Algeria, Wikipedia |
|---|---|---|---|---|
| 1952 | 5,031 | 5,031 | 2,548 | 92 |
| 1953 | 2,996 | 2,990 | 606 | 84 |
| 1954 | 8,171 | not in table | not in table | not in table |
| 1955 | 24,994 | not in table | not in table | not in table |
| 1956 | 36,301 | not in table | not in table | not in table |

[S] for every cell. The two Morocco series differ by 6 in 1953 (2,996 against 2,990). The Wikipedia table's own citation is Dvora Hacohen, "Aliyah to Israel by Country of Origin and Year of Aliyah, 14 May 1948-31 December 1953", in Immigrants in Turmoil (2003), p. 267 (its 1952 and 1953 columns are single years); that book page was not opened here, so it is not cited as read. The table's all-countries cells for 1952 and 1953 are 23,375 and 10,347, the same as Series A in section 1. The moroccanjews.org page prints the same 1952 figure, 5,031.

### 7.3 Where the secondary figures meet the printed ones

Computed here from section 1 and 7.1 and the two secondary series. Nothing in this table is printed anywhere as a Tunisia or Algeria figure.

| Year | CBS Tunisia + Algeria + Morocco (section 1) | Morocco, moroccanjews.org | Difference, derived here (Tunisia + Algeria if the two series match) | Wikipedia Morocco + Tunisia + Algeria |
|---|---|---|---|---|
| 1952 | 7,560 | 5,031 | 2,529 | 7,671 |
| 1953 | 3,628 | 2,996 | 632 | 3,680 |
| 1954 | 11,033 | 8,171 | 2,862 | not in table |
| 1955 | 31,608 | 24,994 | 6,614 | not in table |
| 1956 | 43,769 | 36,301 | 7,468 | not in table |

- 1954 plus 1955: the derived Tunisia and Algeria parts are 2,862 plus 6,614, which is 9,476, against 9,410 printed in 7.1 (66 more). The moroccanjews.org Morocco figures for the two years are 8,171 plus 24,994, which is 33,165, against 33,231 printed (66 fewer). The two gaps are the same 66 by construction (the derived part is the total minus the Morocco series), so this is not independent corroboration.
- 1956: 7,468 derived against 7,488 printed in section 3 for Tunisia and Algeria (20 fewer), and 36,301 against 36,281 printed for Morocco and Tangier (20 more).
- 1952 and 1953: the Wikipedia three-country sums, 7,671 and 3,680, are above the CBS Series A row, 7,560 and 3,628, by 111 and 52.
- **Our reading:** the Jewish Agency based Morocco series and the CBS row differ by 20 in 1956 and by 66 over 1954 and 1955 together (the single years 1952 to 1955 are not comparable, because the CBS row is three countries), which fits a different cut-off or a different treatment of Tangier, but neither source says so. [C] The Morocco figures are usable as a second, secondary series for 1953 to 1955; the Tunisia and Algeria figures for those years exist here only as a derived difference and are never tagged verified.

### 7.4 1956 "Other Countries" rows: Poland, U.S.S.R., Rumania, Bulgaria, Czechoslovakia, Hungary

The text-layer search of SA no. 9 to 14 found no 1956 per-country column: SA no. 9 Table 6 (1919-1957) prints a 1957 column and the cumulative column, SA no. 10 Table 6 (1919-1958) prints 1958 and the cumulative column, SA no. 10 Table 8 is for 1958 only, and the immigrant tables of no. 11 to 14 found by title are by continent of birth. The derived split in section 3 therefore stays the only per-country 1956 figure found. [GAP] for a printed one.

Total only, as a cross-check of the 1956 all-countries figure: JTA, 2 January 1957, prints "A total of 55,000 immigrants came into Israel in 1956, marking a 53 percent increase as compared with 1955" ("made public here today by the Jewish Agency"), against 54,925 in Series A. [S]

A search of the JTA archive for 1956 Poland and Hungary arrivals turned up nothing usable; no bulletin other than the one above was read for this note.
