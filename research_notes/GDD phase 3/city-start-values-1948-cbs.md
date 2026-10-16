# City start values, 1948: the CBS figures for the MVP cities (research note, 2026-10-15)

Status: research and a proposal, for issue #383. It builds on the two earlier notes, `city-start-values-1948-51.md`
(#491) and its follow-up (#504, open), and does not repeat their 1945 anchors or the ma'abarot discussion. Nothing here
is in `content/`. The proposed data are in `cities-1948.proposed.yaml`, next to this note. Which cities the MVP uses, and
every balance value, are the owner's calls (D-D1 to D-D4).

## Owner decision (15 October 2026)

"Census values + defaults", recorded on PR #569:

- The 8 November 1948 census counts are the start values, labelled with their date. They are not scaled back to May
  1948.
- All residents are counted (the census basis), not Jews only.
- Tel Aviv-Yafo is one city (`tel_aviv`, the CBS Tel Aviv-Yafo row). Jaffa stays a separate place on the map.
- The Hebrew Wikipedia figures for Zikhron Ya'akov (1,900) and Rosh Pinna (about 350) are accepted. P66 (15 October
  2026) then found Zikhron Ya'akov's census count in CBS SA No. 3 (1,935, p. 133), so it follows the census rule
  above. Rosh Pinna stays on the Wikipedia figure.
- Rishon LeZion is 10,433 (CBS SA 14).
- Labelled design values fill the gaps where no source exists.

The decision does not name the defaults. The ones used (section "Design defaults") are the recommendation this note
made, and are marked `status: design` in `cities-1948.proposed.yaml`.

## How to read this note

- **[V]** read on the page named, on the date given. **[S]** a figure from a Wikipedia article, not checked against
  its own source. **[D]** derived from [V] figures by the method stated. **[C]** two sources disagree. **[G]** gap: no
  source reached.
- The CBS Statistical Abstracts No. 1 (1949/50), No. 2 (1950/51), No. 3 (1951/52) and No. 14 (1963) are on the CBS
  website as scanned PDFs, at `https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/` (files
  `shnaton1950_num1.pdf`, `shnaton1951_num2.pdf`, `shnaton1952_num3.pdf`, `shnaton1963_num14.pdf`). The earlier notes
  list No. 1 to No. 3 as not reached; they are reachable. Pages were read from the scans on 2026-10-15.

## Which cities

`content/map/settlements_1948.yaml` has 25 places. The twelve of type `city` or `town` are the MVP's Jewish cities:
Jerusalem, Tel Aviv, Haifa, Tiberias, Safed, Petah Tikva, Rishon LeZion, Zikhron Ya'akov, Rosh Pinna, Rehovot, Hadera
and Netanya. The six kibbutzim and moshavim are not cities and are not covered. The seven places typed `arab_town` are
not covered either.

## Summary

1. **Population: 11 of 12 from the CBS [V], 1 from Wikipedia [S].** CBS Statistical Abstract No. 14, Table 11,
   gives each town's population on 8 November 1948 (the census day) and at the end of 1949, 1950 and 1951. Zikhron
   Ya'akov and Rosh Pinna are too small for that table. Zikhron Ya'akov's census count is in SA No. 3's list of
   settlements (1,935, p. 133; P66, 2026-10-15); Rosh Pinna's row there is blank, so it stays Hebrew Wikipedia only.
2. **Jobs: 5 of 12 [D].** Statistical Abstract No. 1, Table 9, gives the working population as a share of the Jewish
   population on 8 November 1948 for Jerusalem, Tel Aviv, Haifa, Tiberias and Safed. Employed = that share times the
   city's population. The other seven have only the national share [G].
3. **Housing: 0 of 12 [G].** No per-city housing stock for 1948 was found. What exists: a national room count for May
   1948 (a 1951 secondary source) and per-city rooms completed in 1948-50 for the three big cities (CBS).
4. **The values are for 8 November 1948, not 15 May 1948.** No per-city count for May 1948 was reached. In those six
   months the Jewish population grew from about 650,000 to 716,678, so a May value would be lower. The owner chose the
   census-day values as the start values, labelled with their date (15 October 2026).

## Population [V]

CBS, Statistical Abstract of Israel No. 14 (1963), Table 11 "Population in settlements numbering above 5,000
inhabitants on 31 XII 1962 (1948-1962)", pp. 30-31, read 2026-10-15:
https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1963_num14.pdf . The first column is headed
"8 XI 1948" (the census). Its footnote: "Estimates up to 1960 have been adjusted in accordance with the 1961 Census
results"; that applies to the end-of-year estimates.

| City | 8 Nov 1948 | End 1949 | End 1950 | End 1951 | Source |
|---|---|---|---|---|---|
| Jerusalem | 83,984 | 103,900 | 123,000 | 140,700 | [V] SA 14 |
| Tel Aviv (CBS row "Tel Aviv-Yafo") | 248,459 | 311,000 | 335,000 | 345,600 | [V] SA 14 |
| Haifa | 98,618 | 131,500 | 140,000 | 147,500 | [V] SA 14 |
| Tiberias | 5,555 | 7,700 | 12,200 | 16,000 | [V] SA 14 |
| Safed ("Zefat") | 2,317 | 4,000 | 5,500 | 7,000 | [V] SA 14 |
| Petah Tikva | 21,879 | 24,800 | 31,800 | 40,000 | [V] SA 14 |
| Rishon LeZion | 10,433 | 15,000 | 18,000 | 20,000 | [V] SA 14; see conflict 1 |
| Rehovot | 12,522 | 14,800 | 18,200 | 21,200 | [V] SA 14 |
| Hadera | 11,811 | 12,000 | 15,000 | 19,000 | [V] SA 14 |
| Netanya | 11,589 | 15,200 | 23,000 | 26,000 | [V] SA 14 |
| Zikhron Ya'akov | 1,935 | - | - | 4,200 | [V] SA 3, Settlements in Israel, p. 133 (P66, 2026-10-15); was 1,900 [S] Hebrew Wikipedia |
| Rosh Pinna | about 350 (May 1948) | - | - | - | [S] Hebrew Wikipedia text: about 150 more by 1949; SA 3 p. 144 lists it with no population |

Checks:
- Statistical Abstract No. 2 (1950/51), Population Table 7 (PDF p. 39), gives the end-1949 and end-1950 values before the 1961
  adjustment. They agree within a few per cent (for example Petah Tikva 24,500 and 31,500; Jerusalem 103,900 and
  121,000; Tiberias 7,700 and 12,200; Hadera 12,000 and 15,000) [V].
- The SA 14 census-day values match the Wikipedia 1948 figures #491 used for Rehovot, Hadera and Netanya (rounded),
  and the Hebrew Wikipedia timelines for Petah Tikva (21,900) and Tiberias (5,500). Wikidata cites the same table for
  these towns but labels the column 31 December 1948; the table says 8 XI 1948.
- Safed: the English Wikipedia says about 1,700 Jews in 1948 (citing Vilnai); the Hebrew Wikipedia says about 2,300
  at the founding of the state. The CBS census figure is 2,317 [V].
- The twelve towns sum to 509,452 people on 8 November 1948, below the 716,700 Jews in `content/population.yaml` (the
  town figures also include non-Jews, mainly in Haifa and Jaffa).

Composition: the census counted residents of all groups. The Tel Aviv-Yafo row includes Jaffa, which
`settlements_1948.yaml` keeps as its own place; the table does not say whether its 1948 value uses the 1948 or a later
boundary. Haifa includes the Arab residents who stayed.

## Jobs [D]

CBS, Statistical Abstract No. 1 (1949/50), section "Registration of Population (8 XI 1948)", Table 9 "Jewish
population, by occupational status and sex (%)", p. 26 (PDF p. 49), read 2026-10-15:
https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1950_num1.pdf . The section note says the figures
for Jerusalem, Tel Aviv and Haifa come from a sample of about 10% of their Jewish population, and that all data are
provisional.

Working population = the sum of the six rows under "Working Population" (proprietors and own account; members of
co-operatives and kibbutzim; managerial and academic; foremen and semi-professionals; unpaid family members; clerks and
labourers), both sexes. Employed = that share times the SA 14 census-day population, rounded to 100.

| City | Working share | Unemployed share | Employed (derived) | Unemployed (derived) |
|---|---|---|---|---|
| Jerusalem | 34.2% | 0.9% | 28,700 | 760 |
| Tel Aviv | 42.6% | 0.8% | 105,800 | 1,990 |
| Haifa | 42.9% | 0.3% | 42,300 | 300 |
| Tiberias | 33.4% | 0.8% | 1,900 | 40 |
| Safed | 34.5% | 1.3% | 800 | 30 |

National check [V]: Statistical Abstract No. 2, Table 9, p. 24 (PDF p. 54): of 716,678 Jews on 8 November 1948,
314,781 were in the working population (43.9%) and 4,111 were unemployed (0.6%). Its English text (PDF p. 169) says mobilised soldiers were classified by their occupation before mobilisation, and that most new immigrants
still in immigrant centres were classified as unemployed or in an unstated branch.

Caveats:
- The shares are of the Jewish population; the populations above include non-Jews. In Tel Aviv-Yafo and Haifa the
  derived counts are therefore slightly high.
- "Employed" is the count of working people, not of job slots. The game's `job_slots` (capacity) has no source; with
  unemployment near 1%, the sourced fact is only that slots were at least the employed count.
- The city shares differ by up to 9 points (Jerusalem 34.2%, Haifa 42.9%), so the national share is a weak stand-in for
  the other seven towns. By the owner decision it fills them as a labelled design value ("Design defaults").

## Housing [G]

No per-city housing stock (dwellings, rooms or capacity) for 1948 was found. What was found:

- National, May 1948: "when the Israeli population numbered 650,000, there were only 300,000 rooms (excluding
  kitchens)" [V]. Charles Abrams, "Israel Grapples with Its Housing Crisis", *Commentary*, April 1951, read 2026-10-15:
  https://www.commentary.org/articles/charles-abrams/israel-grapples-with-its-housing-crisisthe-new-states-number-one-problem/ .
  That is about 2.2 people per room. It is a contemporary secondary source, not the CBS. Applying it per city is a
  design choice, not a fact; it is the housing default in "Design defaults".
- Rooms completed in residential buildings, Jewish building in 17 named towns (CBS, Statistical Abstract No. 2,
  Table 2, p. 50, read 2026-10-15) [V]:

  | | 1948 | 1949 | 1950 |
  |---|---|---|---|
  | All 17 towns | 9,281 | 10,057 | 17,629 |
  | Jerusalem | 113 | 69 | 495 |
  | Tel Aviv | 4,186 | 2,893 | 5,374 |
  | Haifa | 2,196 | 1,651 | 3,220 |

  The 17 towns include Petah Tikva, Rishon LeZion, Rehovot, Hadera and Netanya, but only the three big cities are shown
  separately. From January 1950 Tiberias and Safed were added. These are flows, useful for the pace of city housing
  projects, not a start stock.
- Abandoned Arab housing became immigrant housing in 1948-49 (Haifa, Jaffa, Tiberias, Safed among others). One
  figure: "Between May 1948 and March 1949, about 24,000 immigrants ... were settled in the former Arab quarters of Wadi
  Salib" in Haifa [S] (https://en.wikipedia.org/wiki/Wadi_Salib , read 2026-10-14). Sensitivity: `docs/CONTENT_POLICY.md`
  and the plan §4 apply to any use of this.

## Conflicts

1. **Rishon LeZion, 8 November 1948 [C]:** 10,433 (CBS, Statistical Abstract No. 14, 1963) against 11,100 (CBS media
   release, 2006, cited in #491). Same day, same office. The 2006 release may use later municipal boundaries; neither
   says. The other three cities in the 2006 release agree with SA 14 after rounding (Jerusalem 84,000, Tel Aviv-Yafo
   248,500, Haifa 98,600).
2. **Safed, 1948 [C]:** about 1,700 Jews (English Wikipedia, citing Vilnai) against 2,317 (CBS, census day). The
   figures are for different dates (before the battle and on 8 November 1948); the CBS figure is proposed.
3. **Dates [C]:** Wikidata gives the SA 14 census column as 31 December 1948; the table header is 8 XI 1948.

## For #504 (the ma'abarot conflict)

Not used here, recorded for the follow-up note: CBS Statistical Abstract No. 3 (1951/52), Table 5 "Population, by
type of settlement (31 XII 1951)", p. 7, gives 40 "Independent Ma'abaroth" with 68,345 Jews, and 10 immigrant
reception centres with 26,039 [V, read 2026-10-15]. Ma'abarot attached to existing towns are counted in those towns, so
68,345 is a lower bound for all ma'abarot residents at the end of 1951, not the total. SA 14 Table 11 also
puts Holon at 9,561 on 8 November 1948 [V], one of #504's gaps.

## Design defaults (not historical facts)

These fill the gaps under the owner decision. Each is a national ratio applied to one city; none is a fact about that
city.

- **Jobs, seven towns:** employed = 43.9% of the population (314,781 working of 716,678 Jews on 8 November 1948, SA
  No. 2, Table 9); unemployed = 0.57% (4,111 of 716,678). The five cities with a CBS city share keep it (derived).
- **Housing, all twelve:** rooms (excluding kitchens) = population x 300,000 / 650,000, the national May 1948 ratio
  (Abrams, 1951), about 2.2 people per room. At that density each city's housing capacity equals its start population,
  so every city starts full.

| City | Employed | Unemployed | Rooms (design) |
|---|---|---|---|
| Jerusalem | 28,700 (derived) | 760 (derived) | 38,800 |
| Tel Aviv-Yafo | 105,800 (derived) | 1,990 (derived) | 114,700 |
| Haifa | 42,300 (derived) | 300 (derived) | 45,500 |
| Tiberias | 1,900 (derived) | 40 (derived) | 2,600 |
| Safed | 800 (derived) | 30 (derived) | 1,070 |
| Petah Tikva | 9,600 (design) | 130 (design) | 10,100 |
| Rishon LeZion | 4,600 (design) | 60 (design) | 4,800 |
| Zikhron Ya'akov | 850 (design) | 10 (design) | 890 |
| Rosh Pinna | 150 (design) | 2 (design) | 160 |
| Rehovot | 5,500 (design) | 70 (design) | 5,800 |
| Hadera | 5,200 (design) | 70 (design) | 5,500 |
| Netanya | 5,100 (design) | 70 (design) | 5,300 |

Still for Phase 3, not settled here: converting people to population units (D-A1), and setting `job_slots` and
`housing_capacity` (D-D1 to D-D4). The sources only show that job slots were at least the employed count.

## Not reached

- The 1948 census volume by locality ("Towns, Villages, Regions"): the CBS locality tables above cover places over
  5,000 people in 1962, so Zikhron Ya'akov and Rosh Pinna are not in them. P66 (2026-10-15) found the list of
  settlements in SA No. 3 (pp. 126-147), which gives the 8 November 1948 count per settlement: Zikhron Ya'akov 1,935
  (p. 133). Its Rosh Pinna row (p. 144) has both population columns blank, so Rosh Pinna stays on Hebrew Wikipedia.
- Any per-city housing stock, dwelling count or persons-per-room figure for 1948.
- Working-population shares for the seven towns other than the five above.
- Per-city values for 15 May 1948.

## Sources

- CBS, Statistical Abstract of Israel No. 14 (1963), Table 11, pp. 30-31:
  https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1963_num14.pdf [V, 2026-10-15]
- CBS, Statistical Abstract of Israel No. 1 (1949/50), Table 9, p. 26:
  https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1950_num1.pdf [V, 2026-10-15]
- CBS, Statistical Abstract of Israel No. 2 (1950/51), Population Table 7 (PDF p. 39) and Table 9 p. 24; Building Table 2
  p. 50: https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1951_num2.pdf [V, 2026-10-15]
- CBS, Statistical Abstract of Israel No. 3 (1951/52), Population Table 5, p. 7, and Settlements in Israel, pp. 126-147
  (Zikhron Ya'akov p. 133, Rosh Pinna p. 144):
  https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1952_num3.pdf [V, 2026-10-15]
- CBS media release, "On the eve of Israel's 58th Independence Day" (2006):
  https://www.cbs.gov.il/he/mediarelease/DocLib/2006/092/11_06_092e.pdf [V, 2026-10-15]
- Charles Abrams, "Israel Grapples with Its Housing Crisis", *Commentary*, April 1951:
  https://www.commentary.org/articles/charles-abrams/israel-grapples-with-its-housing-crisisthe-new-states-number-one-problem/ [V, 2026-10-15]
- Hebrew Wikipedia, Zikhron Ya'akov and Rosh Pinna; English Wikipedia, Safed and Wadi Salib [S, 2026-10-14]

## AI assistance

Researched and drafted by an AI agent (Claude Code, for personal2). It read the CBS tables named above from the scanned
PDFs, page by page, on 2026-10-15, and the Wikipedia pages tagged [S] on 2026-10-14. A human must check each figure
against the scans before any value becomes content. That check is the owner gate for Phase 3 PR 1, which copies the
values into `content/`; it does not block merging this note.
