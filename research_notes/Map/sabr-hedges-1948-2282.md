# Prickly-pear (sabr) hedges at Arab villages, 1948 and earlier (#2282, research part)

Status: **draft, needs a person to check**. Research only. An AI agent opened every source marked "agent read" below on
3 November 2026 and recorded the day. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check every claim
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
the village, or both, at villages in the Jezreel valley and Samaria, the coastal plain (Ramleh, Jaffa, Ashdod) and the Gaza plain.
See the table.

**What did not close, and stays [GAP]:** every source found is from 1882 or 1907. No source from the Mandate years (1920-1948) was
reached, so "common in 1948" is not shown by a source dated near 1948. Height, number of rows, and continuous or clumped form are not
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
| Jaffa orchards were protected by hedges of "the prickly cactus"; more than 500 gardens, 3-4 to 10-12 acres each | *Cook's Handbook for Palestine and Syria*, Thos. Cook & Son, London, revised by Hanauer and Masterman (archive.org `CooksHandbookForPalestineAndSyria`; archive.org metadata year 1907, no printed year read), "Jaffa", p. 41 (leaf n57), page image viewed | "These orchards, or gardens, are protected by hedges of the prickly cactus. There are, in the vicinity, over 500 of these gardens, varying in size from three or four acres to ten or twelve acres" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Sulem (Shunem, Jezreel valley): village "surrounded with a thick hedge of the prickly pear" | same book, "Beisan - Sulem", p. 269 (leaf n291), page image viewed | "...the little village of Sulem, ... The village is a great contrast to many the traveller will have seen in Palestine. ... which is surrounded with a thick hedge of the prickly pear" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The Jaffa row gives stone walls and cactus hedges together round the gardens; the Esdud, Ramleh and Dhenebbeh rows give a garden or village enclosure. No height or row count appears on any page viewed | the pages above | see the rows above | [GAP] for height, rows, continuous or clumped | 2026-11-03 |

### Text-layer hits, page image not opened

The agent read these lines in the archive.org text layer of the same two books but did **not** view the page images, so none is
tagged V and none is cited as read. A person can open the page named.

| Place and region | Where | Text-layer wording | Tag |
|---|---|---|---|
| Tannuk, Jezreel valley edge | SWP vol. 2, p. 46 | "surrounded with cactus hedges" | [GAP] page not opened |
| Lidd (Lydda), inner plain | SWP vol. 2, p. 252 | "standing among enclosures of prickly pear" | [GAP] page not opened |
| Tireh, edge of the plain (hills behind) | SWP vol. 2, p. 298 | "with cactus hedges" | [GAP] page not opened |
| Abu Shusheh, Gezer area | SWP vol. 2, p. 407 | "surrounded with cactus hedges" | [GAP] page not opened |
| Nablus: Mount Ebal terraces, gardens of Nablus | SWP vol. 2, pp. 148, 167, 187 | "cacti gardens carefully cultivated in terraces" (p. 187); "Cactus hedges surround the gardens" (p. 167) | [GAP] page not opened |
| Yebna (Ibelin) | SWP vol. 2, p. 421 | "a wall of cactus surrounds it" | [GAP] page not opened |
| Surafend, near Ramleh | Cook's Handbook, p. 49 | "surrounded by cactus hedges" | [GAP] page not opened |

## Disagreements

None found: no two opened sources disagree about the hedges. The two sources are about 25 years apart and one is a survey, the other a
travel guide, so they cannot confirm each other for a single village except Jaffa (both name hedges round its gardens).

## Search log

All runs on 3 November 2026 from the statecraft box.

| Search | Tool and exact query | Result |
|---|---|---|
| archive.org item search | `https://archive.org/advancedsearch.php`, q = `prickly pear sabr hedge village Palestine AND mediatype:texts`, and q = `cactus hedge Palestine village 1930s AND mediatype:texts` | numFound 0 for both (the search is on item metadata, not on page text, so it says nothing about page text) |
| archive.org full texts grepped for prickly / cactus / cacti / sabr / opuntia | `handbookofpalest00luke` (1922 *Handbook of Palestine*) | 0 hits in the text layer |
| same | `surveyofwesternp0002unse` | about 35 line hits; the lines on pp. 45, 86, 253, 255 and 409 are the rows above, the rest are in the text-layer table |
| same | `studiesingalilee00mast` (Masterman 1909) | 2 relevant lines ("prickly pears" among crops; "a beautiful lane shaded by fruit-trees and cacti"), page images not opened, not used |
| same | `FolkLoreOfTheHolyLand` (Hanauer 1907) | 0 hits |
| same | `surveyofwesternp00trisuoft`, `surveyofwesternp0000hbtr`, `ahandbookfortra00portgoog`, `biblicalresearc01robigoog`, `bub_gb_5nNKAAAAYAAJ` | hits counted (8, 8, 13, 4 and 4 lines in that order) but not read or used |
| Dalman, *Arbeit und Sitte in Palaestina* | archive.org items `arbeitundsittein0001gust` to `0003gust`, text download | each returned a 172-byte stub, not a text: not readable from this box |
| Palestine Exploration Fund *Quarterly Statement* 1928 to 1934 | archive.org `dli.ministry.18345`, `18347` | each returned a 146-byte stub: not readable |
| JTA archive | `https://www.jta.org/wp-json/wp/v2/archive?search=<word>&after=..&before=..`, words cactus, prickly, sabra, for 1920-1935, 1936-1939, 1940-1949 | x-wp-total: cactus 7,983 / 1,156 / 2,988; prickly 2,030 / 240 / 584; sabra 149 / 43 / 55. The totals are relevance matches: the first 15 titles of each run were unrelated (German and Polish news), so no item was opened and none is used |
| Library of Congress Matson photograph collection | `https://www.loc.gov/collections/matson-collection/?q=cactus+hedge&fo=json&c=40`, by curl and by the fetch tool | HTTP 429 and a "Just a moment" page both times. Not retried with changed headers. No Matson photograph was seen |
| Web search (the built-in WebSearch tool; no Tavily key on this box) | two queries on prickly pear hedges at Mandate villages | pointed to unopened modern essays and to Wikipedia village pages; none opened, none cited. A search summary is not a source |

## Open gaps

- **[GAP] A source from 1920-1948.** Not found. Not reached: Granqvist's *Birth and Childhood among the Arabs* (1947; archive.org lists it, no full text was opened), Dalman (stub), *Quarterly Statement* volumes (stub), Library of Congress photographs (429), Survey of Palestine map legends (not checked), the Israeli archives (geo-block).
- **[GAP] A dated photograph or description from before 1949 showing hedge height, rows or clumping.**
- **[GAP] Galilee, Judaean hills and Negev edge village hedges.** No page for them was opened and viewed.
- **[GAP] Any named village with a source dated 1948 itself.** The 1882 volume's descriptions are survey descriptions; the pages viewed show 1872 population dates, and no fieldwork year is printed on the hedge lines; no per-village year is printed on the lines in the table.
- Out of scope per the issue: the plant's later history and symbolism. The agent recorded no citation about them.

## Short answers to the four questions

1. **Common in Mandate Palestine up to 1948?** [GAP] for 1948. Sources of 1882 and about 1907 describe cactus or prickly-pear hedges round gardens, orchards or villages at 9 named places in Samaria, the Jezreel valley and the plain (rows above). They stood on garden and orchard boundaries (Jenin, Ramleh, Jaffa, Esdud, Beshshit) and round the village (Shutta, Sirin, Dhenebbeh, Sulem).
2. **By region or village type?** Seen in the Jezreel valley and Samaria, the coastal plain, and the Gaza plain. Not shown by a viewed page for Galilee, the Judaean hills or the Negev edge: [GAP]. Village type: the villages named are mostly mud-built; two are towns (Jaffa, Ramleh); no source opened separates the types.
3. **What they looked like?** "A thick hedge" (Cook's, Sulem); stone walls and hedges together round the gardens (Jaffa); height, rows and continuity [GAP].
4. **Named villages with a dated source?** Nine are named in the tables above, all in 1882 or 1907 sources. None has a source dated 1948 or the Mandate years: [GAP].
