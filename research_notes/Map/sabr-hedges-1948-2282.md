# Prickly-pear (sabr) hedges at Arab villages, 1948 and earlier (#2282, research part)

Status: **draft, needs a person to check**. Research only. An AI agent opened every source marked "agent read" below on
3 November 2026 (the rows dated 2026-11-11 on 11 November 2026) and recorded the day. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check every claim
against its source before use. This note does not change `content/`, `sim/` or `unity/`, and it does not choose where a hedge goes:
any placement change follows in a separate PR after the owner reads this note.

## Question and result

#2282 asks four things: (1) were prickly-pear (Opuntia, "sabr") hedges a common feature of Arab villages in Mandate Palestine up to
1948, and where did they stand; (2) does it vary by region or village type; (3) what did they look like (height, rows, continuous or
clumps); (4) which named villages have a dated source for their hedges in 1948 or earlier. The game side is the `arab_village` hex tile
(`unity/Assets/Scripts/Map/HexTiles/Prototypes/ArabVillage.cs:11`) and #2266.

**What closed.** Two printed sources from before 1949 were opened and their pages viewed: the Palestine Exploration Fund's *Survey of
Western Palestine*, *Memoirs*, vol. 2 (Samaria and the plain, London 1882), and *Cook's Handbook for Palestine and Syria*
(archive.org metadata year 1907). Between them they name hedges ("cactus hedges", "hedges of prickly pear") round the gardens, round
the village, or both, at villages in the Jezreel valley and Samaria, the coastal plain (Ramleh, Jaffa, Ashdod) and the southern plain around Esdud and Dhenebbeh (the page prints the district as "Nâhiet el Mejdel", Sheet XVI; the grouping is the note's own placement).
See the table.

**What did not close, and stays [GAP]:** every source found is from 1882 or 1907. No source from the Mandate years (1920-1948) was
reached, so "common in 1948" is not shown by a source dated near 1948. The six text-layer rows (Tannuk, Lidd, Tireh, Abu Shusheh,
Nablus, Surafend) were opened on their page images on 11 November 2026 and are now in the table; they come from the same two books, so
they do not close this gap. Height, number of rows, and continuous or clumped form are not
given by any source opened. The Galilee, the hills of Judaea and the Negev edge have no page opened by the agent that describes a village
hedge (see the open gaps).

## Answer table

Tags: V (always written in full, as in the rows) means opened and read, page image viewed; [S] secondary; [C] conflicting; [GAP] still
open. The page numbers are the printed ones; the archive.org leaf (`nNNN`, counted from 0) is given so the page can be found again.

| Claim | Source and page | Printed text | Tag | Day read |
|---|---|---|---|---|
| Jenin (Samaria; placement is the agent's, from the sheet) had gardens walled with cactus | PEF *Survey of Western Palestine*, *Memoirs* vol. 2, 1882 (archive.org `surveyofwesternp0002unse`), Sheet VIII, p. 45 (leaf n64), page image viewed | "Jenin is remarkable for its fine gardens north of the town. They are walled with cactus, and contain palms, oranges, tamarisks, and vegetables." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Shutta (Jezreel valley area): village surrounded by hedges of prickly pear and plough-land | same book, Sheet IX, p. 86 (leaf n105), page image viewed | "Shutta (O j).-A small village of mud hovels on rising ground, surrounded by hedges of prickly pear and by plough-land." (the page prints the names with diacritics and a dash after the grid reference; normalised here) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Sirin (same valley): a mud village with hedges of prickly pear | same page, next entry | "Sirin (P i).-A mud village of moderate size, on flat ground, with hedges of prickly pear, and a spring on the north and another on the east." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Ramleh (inner coastal plain): orchards and olive groves enclosed in prickly-pear hedges | same book, Sheet XIII, p. 253 (leaf n296), page image viewed | "The town is surrounded by fine orchards and olive-groves, enclosed in hedges of prickly pear." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Jaffa (coast): gardens surrounded by stone walls and cactus hedges; extent about 1.5 miles inland, over 2 miles north and south | same book, Sheet XIII, p. 255 (leaf n298), page image viewed | "The gardens of Jaffa, surrounded with stone walls and cactus hedges, stretch inland about 1 1/2 miles, and are over 2 miles in extent north and south." (the page prints the fraction as a half) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Esdud (Ashdod, southern coastal plain): houses on a low hill covered with gardens walled in with cactus hedges, on sandy soil | same book, Sheet XVI, p. 409 (leaf n462), page image viewed | "...with walls and enclosures also of mud. The houses occupy the eastern side of a low hill, which rises considerably above them, and is covered with gardens, walled in with cactus hedges; the soil a semi-consolidated sand." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Beshshit (southern plain; placement is the agent's, from the sheet): gardens with cactus hedges and a masonry well | same page (p. 409), entry 2 | "It has gardens with cactus hedges, and a masonry well to the south." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Dhenebbeh (same page): cactus hedges round the village itself | same page (p. 409), entry 7 | "It is built of mud and stone, with cactus hedges round it, and a garden of small fig-trees on the south." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Esdud, the citadel hill north-west of the village (Guérin, quoted in the SWP; "it" is the hill, not a village; this row is not Yebna): cultivated with fig and olive trees, a wall of cactus round it | same book, Sheet XVI, "Archaeology", p. 421 (leaf n474), under the heading "Esdûd (F t)", page image viewed | "This place, which rises on the slopes of a low hill, is itself commanded on the north-west by a higher hill, which was formerly the citadel. It is now cultivated and planted with fig and olive trees ; a wall of cactus surrounds it." (quoted in the SWP from "Guérin, 'Judée,' ii. 71") | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Esdud citadel hill, form of the hedge: cactus said to grow over a thick wall of dressed blocks (a local tradition, as reported by Guérin; the page does not call the wall ancient) | same page (p. 421), next sentences of the same Guérin passage | "This natural hedge grows over a thick wall built of blocks regularly cut and well dressed. This is the tradition among the people" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Jaffa orchards were protected by hedges of "the prickly cactus"; more than 500 gardens, 3-4 to 10-12 acres each | *Cook's Handbook for Palestine and Syria*, Thos. Cook & Son, London, revised by Hanauer and Masterman (archive.org `CooksHandbookForPalestineAndSyria`; archive.org metadata year 1907, no printed year read), "Jaffa", p. 41 (leaf n57), page image viewed | "These orchards, or gardens, are protected by hedges of the prickly cactus. There are, in the vicinity, over 500 of these gardens, varying in size from three or four acres to ten or twelve acres" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Sulem (Shunem, Jezreel valley): village "surrounded with a thick hedge of the prickly pear" | same book, "Beisan - Sulem", p. 269 (leaf n291), page image viewed | "...the little village of Sulem, ... The village is a great contrast to many the traveller will have seen in Palestine. ... which is surrounded with a thick hedge of the prickly pear" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Tannuk ("Jezreel valley edge" is the note's placement; the page says "at the edge of the plain"): village surrounded with cactus hedges | same book (SWP vol. 2), p. 46 (leaf n65, https://archive.org/details/surveyofwesternp0002unse/page/n65), page image viewed | "Tannuk (M k).-A small village ... at the edge of the plain. ... and is surrounded with cactus hedges." (diacritics and the dash normalised, as above) | [V, agent read 2026-11-11; a person's check pending] | 2026-11-11 |
| Lidd (Lydda; "inner plain" is the note's placement): a small town among enclosures of prickly pear | same book, p. 252 (leaf n295, https://archive.org/details/surveyofwesternp0002unse/page/n295), page image viewed | "Ludd (I r).-A small town, standing among enclosures of prickly pear, and having fine olive groves round it" (the page prints the name with a diacritic) | [V, agent read 2026-11-11; a person's check pending] | 2026-11-11 |
| Tireh (edge of the plain, hills behind): a mud village with cactus hedges | same book, p. 298 (leaf n341, https://archive.org/details/surveyofwesternp0002unse/page/n341), page image viewed | "Tireh (J q).-A mud village of moderate size, with cactus hedges, situate at the edge of the plain, the hills rising behind" | [V, agent read 2026-11-11; a person's check pending] | 2026-11-11 |
| Abu Shusheh: village surrounded with cactus hedges. "Gezer area" was the note's own placement word; the page lists the village under "I.-Kada Yafa" and does not name Gezer | same book, p. 407 (leaf n460, https://archive.org/details/surveyofwesternp0002unse/page/n460), page image viewed | "Abu Shusheh (I s).-A village of mud and stone, surrounded with cactus hedges, the water supply from wells beneath." | [V, agent read 2026-11-11; a person's check pending] | 2026-11-11 |
| Nablus (Samaria): cactus hedges round the gardens of the town | same book, p. 167 (leaf n192, https://archive.org/details/surveyofwesternp0002unse/page/n192), page image viewed | "The town is surrounded with beautiful orchards and vegetable gardens ... Cactus hedges surround the gardens." | [V, agent read 2026-11-11; a person's check pending] | 2026-11-11 |
| Mount Ebal above Nablus: cactus gardens on the lower slopes. Gardens, not hedges | same book, p. 148 (leaf n173, https://archive.org/details/surveyofwesternp0002unse/page/n173), page image viewed | "...and extensive cactus gardens on the lower slopes of the mountain near the City of Shechem." | [V, agent read 2026-11-11; a person's check pending]; does not show a hedge | 2026-11-11 |
| Mount Ebal: cactus gardens in terraces. Gardens, not hedges; the passage is Wilson quoted in the SWP, not the surveyors' own words | same book, p. 187 (leaf n214, https://archive.org/details/surveyofwesternp0002unse/page/n214), page image viewed, quoting Colonel Sir Charles Wilson, PEF *Quarterly Statement* 1873, p. 66 | "for a considerable height it is clothed with luxuriant cacti gardens carefully cultivated in terraces" (the page splits "carefully" over a line and ends the passage "Colonel Sir Charles Wilson, 'Quarterly Statement,' 1873, p. 66.") | [V, agent read 2026-11-11; a person's check pending]; does not show a hedge | 2026-11-11 |
| Surafend (near Ramleh): village surrounded by cactus hedges | *Cook's Handbook*, p. 49 (leaf n65, https://archive.org/details/CooksHandbookForPalestineAndSyria/page/n65), page image viewed | "Half an hour before reaching Ramleh the modern village of Surafend will be seen on the right hand, surrounded by cactus hedges (Cactus opuntia)" | [V, agent read 2026-11-11; a person's check pending] | 2026-11-11 |
| The Jaffa row gives stone walls and cactus hedges together round the gardens; the Esdud, Ramleh and Dhenebbeh rows give a garden or village enclosure. No height or row count appears on any page viewed | the pages above | see the rows above | [GAP] for height, rows, continuous or clumped | 2026-11-03 |

### Text-layer hits, page image not opened

None left. The six rows that stood here (Tannuk, Lidd, Tireh, Abu Shusheh, Nablus, Surafend) were opened on their page images on
11 November 2026; every page number was right and every page prints the wording the row gave. They are now in the answer table,
dated 2026-11-11.

## Disagreements

None found: no two opened sources disagree about the hedges. The two sources are about 25 years apart and one is a survey, the other a
travel guide, so they cannot confirm each other for a single village except Jaffa (both name hedges round its gardens).

## Search log

All runs on 3 November 2026 from the statecraft box, except the last row (11 November 2026).

| Search | Tool and exact query | Result |
|---|---|---|
| archive.org item search | `https://archive.org/advancedsearch.php`, q = `prickly pear sabr hedge village Palestine AND mediatype:texts`, and q = `cactus hedge Palestine village 1930s AND mediatype:texts` | numFound 0 for both (the search is on item metadata, not on page text, so it says nothing about page text) |
| archive.org full texts grepped for prickly / cactus / cacti / sabr / opuntia | `handbookofpalest00luke` (1922 *Handbook of Palestine*) | 0 hits in the text layer |
| same | `surveyofwesternp0002unse` | about 35 line hits; the lines on pp. 45, 86, 253, 255 and 409 are the rows above; pp. 46, 148, 167, 187, 252, 298 and 407 are the rows dated 2026-11-11 |
| same | `studiesingalilee00mast` (Masterman 1909) | 2 relevant lines ("prickly pears" among crops; "a beautiful lane shaded by fruit-trees and cacti"), page images not opened, not used |
| same | `FolkLoreOfTheHolyLand` (Hanauer 1907) | 0 hits |
| same | `surveyofwesternp00trisuoft`, `surveyofwesternp0000hbtr`, `ahandbookfortra00portgoog`, `biblicalresearc01robigoog`, `bub_gb_5nNKAAAAYAAJ` | hits counted (8, 8, 13, 4 and 4 lines in that order) but not read or used |
| Dalman, *Arbeit und Sitte in Palaestina* | archive.org items `arbeitundsittein0001gust` to `0003gust`, text download | each returned a 172-byte stub, not a text: not readable from this box |
| Palestine Exploration Fund *Quarterly Statement* 1928 to 1934 | archive.org `dli.ministry.18345`, `18347` | each returned a 146-byte stub: not readable |
| JTA archive | `https://www.jta.org/wp-json/wp/v2/archive?search=<word>&after=..&before=..`, words cactus, prickly, sabra, for 1920-1935, 1936-1939, 1940-1949 | x-wp-total: cactus 7,983 / 1,156 / 2,988; prickly 2,030 / 240 / 584; sabra 149 / 43 / 55. The totals are relevance matches: the first 15 titles of each run were unrelated (German and Polish news), so no item was opened and none is used |
| Library of Congress Matson photograph collection | `https://www.loc.gov/collections/matson-collection/?q=cactus+hedge&fo=json&c=40`, by curl and by the fetch tool | HTTP 429 and a "Just a moment" page both times. Not retried with changed headers. No Matson photograph was seen |
| Web search (the built-in WebSearch tool; no Tavily key on this box) | two queries on prickly pear hedges at Mandate villages | pointed to unopened modern essays and to Wikipedia village pages; none opened, none cited. A search summary is not a source |
| archive.org page images for the six text-layer rows (11 November 2026) | printed page mapped to leaf with `surveyofwesternp0002unse_page_numbers.json` and the `_djvu.xml` page objects of both books; page images `https://archive.org/download/surveyofwesternp0002unse/page/nNNN_w1400.jpg` for n65, n173, n192, n214, n295, n341, n460, and `https://archive.org/download/CooksHandbookForPalestineAndSyria/page/n65_w1400.jpg`, read by eye | all six page numbers right; rows dated 2026-11-11 above |

## Open gaps

- **[GAP] A source from 1920-1948.** Not found. Not reached: Granqvist's *Birth and Childhood among the Arabs* (1947; archive.org lists it, no full text was opened), Dalman (stub), *Quarterly Statement* volumes (stub), Library of Congress photographs (429), Survey of Palestine map legends (not checked), the Israeli archives (geo-block).
- **[GAP] A dated photograph or description from before 1949 showing hedge height, rows or clumping.**
- **[GAP] Galilee, Judaean hills and Negev edge village hedges.** No page for them was opened and viewed.
- **[GAP] Any named village with a source dated 1948 itself.** The 1882 volume's descriptions are survey descriptions; the pages viewed show 1872 population dates, and no fieldwork year is printed on the hedge lines; no per-village year is printed on the lines in the table.
- Out of scope per the issue: the plant's later history and symbolism. The agent recorded no citation about them.

## Short answers to the four questions

1. **Common in Mandate Palestine up to 1948?** [GAP] for 1948. Sources of 1882 and about 1907 describe cactus or prickly-pear hedges round gardens, orchards or villages at 9 named places in Samaria, the Jezreel valley and the plain (rows above), and six more rows read on 11 November 2026 add Tannuk,
Lidd, Tireh, Abu Shusheh, Nablus and Surafend. They stood on garden and orchard boundaries (Jenin, Ramleh, Jaffa, Esdud, Beshshit) and round the village (Shutta, Sirin, Dhenebbeh, Sulem). Along paths: [GAP]; no page viewed says so, and the one lead, Masterman's "a beautiful lane shaded by fruit-trees and cacti" (search-log row for `studiesingalilee00mast`), was not opened.
2. **By region or village type?** Seen in the Jezreel valley and Samaria, the coastal plain, and the southern plain around Esdud (p. 409 prints the district as "Nâhiet el Mejdel"; the grouping is the note's own placement). Not shown by a viewed page for Galilee, the Judaean hills or the Negev edge: [GAP]. Village type: the villages named are mostly mud-built; three are towns (Jaffa, Ramleh, and Jenin, which p. 45 calls a town), and of the 11 November rows Lidd (p. 252, "A small town") and Nablus (p. 167, "The town") are towns too; no source opened separates the types.
3. **What they looked like?** "A thick hedge" (Cook's, Sulem); stone walls and hedges together round the gardens (Jaffa); at Esdud, a cactus wall round the citadel hill, said by local tradition to grow over a thick wall of dressed blocks (SWP p. 421, quoting Guérin); height, rows and continuity [GAP].
4. **Named villages with a dated source?** Nine are named in the 3 November rows and six more in the 11 November rows, all in 1882 or 1907 sources. None has a source dated 1948 or the Mandate years: [GAP].
