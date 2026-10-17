# City start values, 1948-51 - follow-up (research note, 2026-10-14)

Status: research only, for issue #383. This is the follow-up to `city-start-values-1948-51.md` (#491), filling two of
its named gaps: a sourced pre-war anchor for the eight towns that had no 1948 figure (three of them -
Tiberias, Safed and Petah Tikva - now have the census-day count on main, `content/cities.yaml`, CBS Statistical
Abstract No. 14, Table 11), and the ma'abarot 250,000 vs 26,039 conflict. Nothing here is in `content/`. Which cities exist in the MVP, and every balance value, are the owner's
calls (D-D1 to D-D4, D-D8 in #375).

## How to read this note

- **[V]** read on the page named, on 2026-10-14. **[S]** a figure from a Wikipedia article that I read, where the article
  cites the source named; not checked against that source's own text. **[C]** two sources disagree. **[G]** gap: no
  source reached.
- "Village Statistics" below is the Government of Palestine, Department of Statistics, *Village Statistics, April 1945*
  (quoted in Hadawi 1970): the last Mandate survey, three years before the window opens. It is a pre-war anchor, not a
  1948 value.

## Summary

1. **Five of the eight missing towns now have a 1945 Village Statistics anchor [S]** - Nazareth 14,200; Acre
   12,360; Tiberias 11,310; Beersheba 5,570; Afula 2,310 - **and Safed's 1945 anchor is now read from the Village
   Statistics scan itself: Safad (Urban) 11,930, of them 9,530 Muslims and 2,400 Jews [V, read 2026-10-17].** Since this note was written, three of the eight have
   their census-day count on main (`content/cities.yaml`, CBS Statistical Abstract No. 14, Table 11): Tiberias
   5,555, Safed 2,317 and Petah Tikva 21,879 - the 1945 figures stay as background for those three. Petah Tikva was
   about 22,000 after the municipal mergers; the article dates them only "after 14 May 1948", unreferenced, and
   the he.wiki village articles disagree for three of the six (see the table) [C]. Of the other five towns,
   Nazareth, Acre and Beersheba are `arab_town` in `content/map/settlements_1948.yaml`, and Afula and Holon are not
   on the map. Holon's 1945 anchor is now read from the Village Statistics scan itself: 3,280 (Urban) [V,
   read 2026-10-17]; for 1948 see the table.
2. **The ma'abarot conflict is resolved as two different categories and two different dates.** The 26,039 (end of 1951)
   counts immigrant camps and reception centres; the 250,000 is the ma'abarot scale, dated by the National Library of
   Israel to 1953 (about 130 ma'abarot), not 1951. One residual conflict: the English Wikipedia dates "127 ma'abarot
   housing 250,000" to 1951 [C]. No direct figure for ma'abarot residents at the end of 1951 was found [G].
3. **The 8 November 1948 census method is now sourced [V]** (CBS census history page): two stages, a seven-hour home
   curfew on census day, run to build the Population Register ahead of the first Knesset elections. The per-locality
   results are now reached and in content: `research_notes/GDD phase 3/city-start-values-1948-cbs.md` (CBS
   Statistical Abstract Nos. 1, 2, 3 and 14, read 2026-10-15).

## The 1945 anchors, town by town [S]

Read from the English Wikipedia articles on 2026-10-14; each cites *Village Statistics, April 1945* unless noted
(Beersheba is the exception: the article attributes it to the 1945 village survey but cites the UN
Conciliation Commission, April 1949).

| Town | 1945 (Village Statistics) | Composition | Earlier point |
|---|---|---|---|
| Nazareth | 14,200 | 8,600 Christians, 5,600 Muslims | 1938 survey: 9,900 (100 Jews) |
| Acre | 12,360 | 9,890 Muslims, 2,330 Christians, 50 Jews, 90 others | 1931 census: 7,897 |
| Tiberias | 11,310 (sum of groups) | 6,000 Jews, 4,540 Muslims, 760 Christians, 10 others | - |
| Safed | Safad (Urban) 11,930 (9,530 Muslims, 2,400 Jews; the volume spells it "Safad"), Safad Sub-District Table I (Continued), Village Statistics 1945 [V, scan read 2026-10-17, djvu.xml coordinates; the en.wiki 13,700 "by 1948" stays unreferenced] | - | 1922 census: 8,761 (5,431 Muslims, 2,986 Jews, 343 Christians) |
| Beersheba | 5,570 (the article attributes it to the 1945 village survey but cites the UN Conciliation Commission, April 1949) | 5,360 Muslims, 200 Christians, 10 others | 1931 census: 2,959 |
| Afula | 2,310 (sum of groups) | 2,300 Jews, 10 Muslims | - |
| Petah Tikva | not in the article for 1945; about 22,000 after the adjoining villages (Amishav, Ein Ganim, Kiryat Matalon, Kfar Ganim, Mahaneh Yehuda, Kfar Avraham) merged into its boundaries. The en.wiki sentence dates the mergers only "after 14 May 1948" and carries no footnote [S]; the he.wiki village articles date three of them differently: Mahaneh Yehuda joined in 1937 (the year Petah Tikva was declared a city), Ein Ganim was formally annexed in 1939, Kfar Ganim unified with the city in 1962 [C]. The merger dates for Kiryat Matalon, Kfar Avraham and Amishav were not found [G] | Jewish | 1931 census: 6,880 |
| Holon | 3,280 (Urban), Village Statistics 1945, Jaffa Sub-District Table I [V, scan read 2026-10-17: the archive.org OCR splits the numerals from the names, so the figure was read off the djvu.xml word coordinates of the Holon (Urban) row; the row's group columns did not OCR readably] | - | [S] he.wiki: 9,600 in 1948 (population chart, no footnote); 28,000 at the 1940 founding |

These are the pre-war baselines. Lod, Ramla, Ashkelon (Majdal) and Beersheba changed their populations in 1948-49, and
Tiberias, Safed and Acre saw the same in 1948; a 1945 figure is therefore a "before" value, not the game start value.
Sensitivity: `docs/CONTENT_POLICY.md` and the plan §4 apply to any card about these towns.

Pages read: https://en.wikipedia.org/wiki/Nazareth , https://en.wikipedia.org/wiki/Acre%2C_Israel ,
https://en.wikipedia.org/wiki/Tiberias , https://en.wikipedia.org/wiki/Safed , https://en.wikipedia.org/wiki/Beersheba ,
https://en.wikipedia.org/wiki/Afula , https://en.wikipedia.org/wiki/Petah_Tikva , https://en.wikipedia.org/wiki/Holon .
Village Statistics 1945 scan, Jaffa Sub-District Table I and Safad Sub-District Table I (Continued):
https://archive.org/details/palestine-village-statistics-1945 (djvu.xml coordinates, read 2026-10-17).
Petah Tikva merger dates: https://he.wikipedia.org/wiki/%D7%9E%D7%97%D7%A0%D7%94_%D7%99%D7%94%D7%95%D7%93%D7%94_(%D7%A4%D7%AA%D7%97_%D7%AA%D7%A7%D7%95%D7%95%D7%94) ,
https://he.wikipedia.org/wiki/%D7%A2%D7%99%D7%9F_%D7%92%D7%A0%D7%99%D7%9D , https://he.wikipedia.org/wiki/%D7%9B%D7%A4%D7%A8_%D7%92%D7%A0%D7%99%D7%9D ,
https://en.wikipedia.org/wiki/Petah_Tikva (all read 2026-10-17).

## Ma'abarot: the conflict resolved

Read from the National Library of Israel's ma'abarot article on 2026-10-14 [V]:
https://www.nli.org.il/en/discover/israel/zionism/zionism-history/maabarot

- At the beginning of 1950 about 100,000 people lived in the temporary absorption camps.
- In March 1950 Levi Eshkol, head of the Jewish Agency settlement department, initiated the ma'abarot to relieve the
  camps; unlike the camps, ma'abara residents had to support themselves.
- "In 1953, about 130 ma'abarot were established to house about 250,000 persons."
- The ma'abarot gradually began to close from the end of 1952 (the page's "That same year", in the ma'abarot
  paragraph), a process that lasted two decades.

So the two figures in the first note were never measuring the same thing:

- **26,039** (US Census Bureau 1952 report, end of 1951) [V] = residents of immigrant camps and reception centres.
- **250,000** [V, NLI] = the ma'abarot scale in 1953, after the MVP window closes (1 January 1952).

Residual conflict [C]: the English Wikipedia's ma'abarot article dates "127 Ma'abarot housing 250,000 Jews" to 1951
(resting on one footnote, Swirski 2002, p. 114).
The NLI dates the same scale to 1953. For the MVP window the sourced statements are: ma'abarot exist from May 1950
(first note, [S]) and grow through 1951, but no source reached gives their population at the end of 1951 [G].

For game use this keeps supporting D-D3 (a transit camp from May 1950, a cheap low-condition type), with the scale at
the window's end still the owner's value.

## The 1948 census method [V]

CBS, "The Census - A Historical Perspective", read 2026-10-14:
https://www.cbs.gov.il/en/Surveys/Pages/The-Census-Historical-Perspective.aspx

The first enumeration ran in November 1948 in two stages: enumerators visited homes beforehand; on census day,
8 November 1948, a seven-hour home curfew was imposed and the enumerators returned to verify the records. Its purpose
was to build the Population Register and to issue identity certificates ahead of the first Knesset elections. Censuses
followed in 1961, 1972, 1983 and 1995.

## Open questions for the owner (updated from the first note)

1. ANSWERED (owner, 15 October 2026, on #569; `docs/gdd/cities.md:10`): the 8 November 1948 census count is the
   start value ("Census values + defaults"); the 1945 column is background.
2. ANSWERED (the same ruling): each city starts at its census population of 8 November 1948.
3. Ma'abarot: with the categories now separated, which end-1951 scale (if any) should the transit-camp value reflect?
   STILL OPEN.
4. ANSWERED (15 October 2026): all residents (#569; Q-START-5); housing capacity equals the start population
   (Q-START-1).

## Not reached

- The per-locality results of the 8 November 1948 census: REACHED after this note was written - CBS Statistical
  Abstract Nos. 1, 2, 3 and 14 on the CBS site, read 2026-10-15; see
  `research_notes/GDD phase 3/city-start-values-1948-cbs.md`.
- Holon 1945: not in the English or Hebrew articles read. (For 1948 the Hebrew article's population chart gives 9,600,
  unfootnoted [S]; its text gives 28,000 at the 1940 founding.) The Village Statistics scanned PDF (mirrors at
  archive.org and elsewhere) is image-only and its tables could not be read here.
- Per-city housing capacity, and ma'abarot residents at end of 1951: no source found.
- The Hebrew press (National Library, JPress): not tried; it blocks scripted access (see #384).

## Sources

- National Library of Israel, "The Ma'abarot": https://www.nli.org.il/en/discover/israel/zionism/zionism-history/maabarot [V]
- CBS, "The Census - A Historical Perspective": https://www.cbs.gov.il/en/Surveys/Pages/The-Census-Historical-Perspective.aspx [V]
- Wikipedia articles listed above, read 2026-10-14 [S]; each cites Government of Palestine, Department of Statistics,
  *Village Statistics, April, 1945* (quoted in Hadawi, *Village Statistics of 1945*, 1970).
- US Bureau of the Census, "Israel: Jewish population and immigration" (1952):
  https://digitalcommons.fau.edu/cgi/viewcontent.cgi?article=1307&context=prism [V]

## AI assistance

This note was researched and drafted by an AI agent (agamrafaeli-instinct). It read the pages tagged [V] on
2026-10-14 and the Wikipedia articles tagged [S] the same day. It did not read the scanned Village Statistics tables,
the 1948 census volume, the CBS Statistical Abstracts of 1949-52, or the Hebrew press. The [S] figures are one hop from
their cited sources and should be re-checked against the Village Statistics tables before they become content values.
