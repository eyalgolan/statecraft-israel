# Jaffa's Sea Mosque and its minaret in 1948, for Tal's `sea_minaret` prop (#2268, research part)

Status: **draft, needs a person to check**. This is research only. An AI agent opened every source marked "agent read" below on
3 November 2026 and recorded the day it opened each. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to
check every claim against its source before use. This note does not change `content/`, `sim/` or `unity/`, and it does not
place the prop: placement follows in a separate PR, and the owner decides it.

## Question and result

#2268 asks, for the hex-tile prop `tiles:jaffa_old_city/sea_minaret` (`unity/Assets/Scripts/Map/HexTiles/Prototypes/JaffaOldCity.cs:41`):
(1) did Jaffa's Sea Mosque stand in 1948 and have a minaret; (2) where, as a WGS84 point with its precision; (3) what the minaret looked
like in 1948 if a photograph or description of the time exists; (4) any change between May 1948 and 1951.

**What closed**

- **The mosque.** The Sea Mosque (Arabic *Masjid al-Bahr*, also *Jami' al-Bahr*; Hebrew *Misgad HaYam*) was built in 1675 by the governor of the
  Gaza sanjak and stood on the old city's seafront by the port. A 2010 article lists it among the thirteen mosques of Jaffa "until the late Ottoman
  period" and calls it the oldest. No source read says it was gone before 1948.
- **A minaret by 1962.** A Tel Aviv municipal engineer's report of 9 December 1962, quoted in the same article, says the mosque holds "a large hall with
  two arches and a uniquely-shaped minaret".
- **Point.** 32.05555556 N, 34.75277778 E (= 32°03′20″N 34°45′10″E), a Wikidata/Wikipedia point at one arc-second precision [S]. Plotted on the 1:20,000 sheet
  it falls on the north seafront edge of the old city.

**What did not close, and stays [GAP]:** a 1948 (or earlier dated) photograph or description of the minaret; its height and material in 1948; any record of
damage, demolition or rebuilding between May 1948 and 1951 (none found, and "none found" is not a finding that nothing happened); a surveyed coordinate.

## Answer table

Tags: V (always written in full, as in the rows) means opened and read by the agent; [S] secondary; [C] conflicting, both values kept; [GAP] still open.

| Claim | Source and page or URL | Printed text | Tag | Day read |
|---|---|---|---|---|
| Built 1675, near the harbour, on HaAliya HaShniya Street; one minaret in the infobox | en.wikipedia "Al-Bahr Mosque", revision 1366870976 (`https://en.wikipedia.org/wiki/Al-Bahr_Mosque`); it cites Mirkin 2017, p. 152 n. 16, which this agent could not open (Google Books returned 403, see search log) | "Built in 1675 CE, the mosque is situated on the HaAliya HaShniya Street near the harbour." Infobox: "minaret_quantity = One" | [S] | 2026-11-03 |
| Built by Musa Pasha Radwan, governor of the Gaza sanjak, in 1675; "close to the coast, adjacent to the port"; the oldest of Jaffa's thirteen mosques | Mahmoud Yazbak, "The Islamic Waqf in Yaffa and the Urban Space", *Makan* 2 (Adalah, 2010), printed p. 32 (PDF page 32), `http://www.adalah.org/eng/publications/makan/eng2.pdf`; page image viewed | "The oldest of these mosques was the Al-Bahr (Sea) Mosque, which was established by Musa Pasha of the Radwan Emir al-Hajj family and the Emir of Gaza in 1675. As its name suggests, this mosque was located close to the coast, adjacent to the port." The list of thirteen is on pp. 31-32 ("until the late Ottoman period") | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The mosque had a minaret and was a warehouse in 1962 | same, p. 32, quoting Tel Aviv Municipality Archives, Group 4, File 2241, Doc. 877/62, engineer's report, 9 December 1962 (Hebrew; this agent read Yazbak's English quotation, not the file) | "One of the oldest mosques in Yaffa and was built approximately 300 years ago. The mosque contains a large hall with two arches and a uniquely-shaped minaret. Today, the mosque is used as a warehouse" | [V, agent read 2026-11-03; a person's check pending] for what the article prints | 2026-11-03 |
| Same fact, in Hebrew Wikipedia: the minaret stands at the building's south-west corner, with a muezzin's balcony; in 1962 Tel Aviv reported the building used as a store | he.wikipedia "מסגד הים (יפו)", revision 42466237 | (Hebrew, not quoted; the note reads the article's second and third paragraphs) | [S] | 2026-11-03 |
| The minaret is cylindrical and late Ottoman, on an earlier base | Andrew Petersen, *The Towns of Palestine under Muslim Rule, AD 600-1600* (BAR, 2005), list of plates, Plate 21; archive.org `TheTownsOfPalestineUnderUnderMuslimRule` (OCR text read, no page image opened, page number not read) | "21. Jaffa, Sea Mosque. Note cylindrical late Ottoman minaret stands on an earlier, medieval(?), base." The photograph is of the standing building, undated | [V, agent read 2026-11-03; a person's check pending] for the caption; the date of the photograph is [GAP] | 2026-11-03 |
| ArchNet: Jami' al-Bahr, 16th/17th c.; the date of the second floor and the upper part of the minaret "remains unclear" | `https://web.archive.org/web/20110629172815/http://archnet.org/library/sites/one-site.jsp?site_id=7225` (the current `archnet.org/sites/3537` answered 429, not read) | "The original date of construction remains unclear, as does the date of the addition of a second floor and the upper part of its minaret." Sources it lists include Petersen 2001, *Gazetteer of Buildings in Muslim Palestine*, p. 166 (not opened: academia.edu returned a 403 challenge page) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The prop's question (minaret in 1948) | no source read states it | | [GAP] | 2026-11-03 |
| Point: 32.05555556 N, 34.75277778 E, precision 0.000277778 deg | Wikidata Q2898907, property P625, revision 2485953131 (`https://www.wikidata.org/w/api.php?action=wbgetentities&ids=Q2898907&props=claims&format=json`); en.wikipedia infobox `{{coord\|32\|03\|20\|N\|34\|45\|10\|E\|...source:kolossus-hewiki}}` | one arc-second is 30.9 m north-south and 30.9 x cos(32.06 deg) = 26.2 m east-west | [S] | 2026-11-03 |
| That point on the 1:20,000 map | Palestine Open Maps, sheet 12-16 Jaffa, tiles `https://palopenmaps.org/tiles/pal20k-1940s/16/{39093..39095}/{26600..26602}.jpg` (edition not named by the tile set; the sheet list has 12-16 undated and `12-16.1943-Jaffa.jpg`); plotted by this agent at z16 | the point falls on the coast line along the north edge of the old city's built-up block; no mosque or minaret label is printed there at z16 | [V, agent read 2026-11-03; a person's check pending] for what the map shows; the placement is the agent's plot, so a person should repeat it | 2026-11-03 |
| An unnamed Jaffa mosque was damaged on 24 May 1948 | JTA Daily News Bulletin, 26 May 1948, `http://pdfs.jta.org/1948/1948-05-26_122.pdf`, page 3 (page image read; item under the page's only dateline, "TEL AVIV, May 25. (JTA)") | "A Haganah communique this morning reported that Egyptian planes yesterday damaged a mosque when they bombed Jaffa." Event date 24 May 1948 (dateline 25 May, "yesterday"). The item does not name the mosque | [V, agent read 2026-11-03; a person's check pending]. That it was the Sea Mosque is [GAP], not claimed | 2026-11-03 |
| Distinct from the Mahmoudiya Mosque (Jaffa's Great Mosque, 1812, 32.0549 N, 34.7555 E) | `docs/visual-v1.md:2443-2444` (repo); Yazbak p. 31 lists "the Al-Bahr Mosque" and "the Great Mosque" as two of the thirteen | see the repo lines | [S] for the Mahmoudiya point; list [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |

## Disagreements and look-alikes

No two sources disagree on a value the issue asks about. One disagreement outside the question is kept: the builder. Yazbak p. 32 names Musa Pasha of the Radwan family; en.wikipedia (revision 1366870976) says the 'Azza/Alazzeh family, citing an inscription over the entrance (not opened) [C]. The prop does not depend on it. Three look-alikes a reader could confuse with the Sea Mosque:

- **Tabiyah Mosque.** Yazbak p. 32 puts it "close to the port lighthouse" and quotes a 1950 Ministry of Religious Affairs report (Meir and Venkerfield, 1950, p. 28, not opened) that the State "has turned the mosque into a home inhabited by a Christian family". That is the Tabiyah, not the Sea Mosque; the article gives no 1950 line for the Sea Mosque. A Matson photograph of 1936 (Library of Congress `2019709019`, `LC-M33- 9240`) captions "House of Simon the Tanner is to the right of the distant minaret"; Yazbak links St Simeon's place to the Tabiyah, so that minaret is probably the Tabiyah's, and it is not used as evidence for the Sea Mosque.
- **Mahmoudiya Mosque.** An Islamic-heritage web page (`https://www.islamicarchitecturalheritage.com/?p=2013`, read 2026-11-03) says a mosque "remained neglected, abandoned and deserted until 1982"; its text is about the Great Mahmoudiya Mosque, not the Sea Mosque. Not used.
- **Hassan Bek Mosque** (Manshiya): JTA bulletins of 6 January and 25 March 1948 (API text only, page images not opened) name it as a Haganah-reported Arab strongpoint and as damaged by mortar fire; Yazbak's text says its minaret later collapsed. Unrelated to the Sea Mosque.

## Search log

Every row: tool, query or URL, day run 2026-11-03.

| Source or search | Exact query / URL | Result |
|---|---|---|
| JTA archive, wp-json | `https://www.jta.org/wp-json/wp/v2/archive?search=mosque+jaffa&after=1947-11-29T00:00:00&before=1951-12-31T23:59:59&per_page=20` | x-wp-total 9. Each hit's text was searched for "mosque": no item names the Sea Mosque; the 26 May 1948 item above is the only one on a damaged Jaffa mosque |
| JTA archive, wp-json | same URL with `search=minaret` | x-wp-total 7. No hit about Jaffa's Sea Mosque in the text read |
| JTA archive, wp-json | same URL with `search=jaffa+mosque` | x-wp-total 9, the same items as the first row |
| archive.org full text | `https://archive.org/services/search/beta/page_production/?service_backend=fts&user_query="Sea Mosque" Jaffa` | 59 books; open ones read: Petersen (above). Lending-only books (*The Glory of the Holy Land*, which lists a plate "The minaret of the Sea Mosque at Jaffa", *Palestine: a guide*, Lebovic's *City of Oranges*) were not opened: not cited |
| archive.org full text | same, query `"Jami el-Bahr" OR "Jama el-Bahr" OR "Jamia el-Bahr" Jaffa` | 0 hits |
| Library of Congress prints | `https://www.loc.gov/pictures/search/?q=jaffa%20harbor&co=matpc&fo=json`, also `jaffa port`, `jaffa mosque harbor` | 4, 8 and 0 hits. Six harbour photographs opened (items `2019703277`, `2019694992`, `2019698371`, `2019692331`, `2019709019`, `2019713094`): none identified by its caption as the Sea Mosque |
| Wikimedia Commons | `list=search`, namespace 6, queries "Jaffa Sea Mosque", "Jaffa port mosque minaret", and category listings | only modern photographs; no pre-1951 photograph found. File names only, no file opened |
| Google Books (Mirkin 2017, p. 152) | `https://books.google.com/books?id=c2mRDwAAQBAJ&pg=PA152&output=text` | 403 "unusual traffic" page. Not read; recorded, no header changed |
| academia.edu (Petersen 2001 gazetteer, pp. 166-167) | `https://www.academia.edu/21539664/Gazetteer_4_D_J` | 403 challenge page. Not read |
| Overpass (OpenStreetMap) | `https://overpass-api.de/api/interpreter` query for places of worship in the box 32.050-32.060 N, 34.748-34.758 E | HTTP 406. Not read; no header changed |
| ArchNet (current page) | `https://archnet.org/sites/3537` (WebFetch) | HTTP 429. Not read; the 2011 archived copy was read instead |
| WebSearch (built-in; no Tavily key on this box) | `Jaffa Sea Mosque al-Bahr minaret history 1948 damaged OR demolished OR "second floor" OR rebuilt` | links only, to Wikipedia mirrors, ArchNet and one heritage page; none opened beyond those named above |
| Israeli archives and press (State Archives, NLI historical press, the Israeli Ministry of Religious Affairs 1950 report) | not attempted: geo-blocked per the lane rules; the 1950 report is known here only through Yazbak's quotation | not read |

## Open gaps

1. A photograph or drawing of the Sea Mosque dated before 1951, with the minaret visible (the Matson, Bonfils and American Colony collections, the Survey of Palestine air photographs, IDF and Israel State Archives pictures). The second floor and the upper minaret have no known date (ArchNet).
2. The minaret's height, diameter and material in 1948. The only description is Petersen's "cylindrical late Ottoman minaret ... on an earlier ... base" and the 1962 "uniquely-shaped minaret".
3. Any record for 1948-51: the 1950 Ministry of Religious Affairs report (Meir and Venkerfield) and the Tel Aviv file 877/62 would show the mosque's state; neither was opened.
4. A surveyed coordinate. The only point is a Wikipedia/Wikidata arc-second point [S]; Mirkin (2017) and Petersen (2001) were not opened.
