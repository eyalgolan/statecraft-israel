# Era look 1957 to 1977: dated sources and free photo links

Researched 25 October 2026. Every URL in the sources table was opened on that day by an agent; a person's check of the sources is pending. **Research only:** a note for issue #131 (the chapter on screen, 1957-77 item 19), the research part the owner opened on 24 Oct. It links to photographs and records their licence as the Commons page prints it; **it holds no image file** and changes nothing under `content/`. The chapter's screens and art stay paused until after the MVP. The design is [the 1957-1977 era design](../../docs/superpowers/specs/2026-10-10-era-1957-1977-design.md), section 7.4 (lines 562 to 576 at commit 55dd6489), whose every item is "to verify".

## Conventions

- **[V, agent read 2026-10-25; a person's check pending]** a primary record opened on the day: a Bank of Israel page, a printed page of the Sefer HaChukim, or a Commons file page read for what it prints (licence template, date, author). It is [V] for what the page says, not for any fact behind it.
- **[S]** secondary: press (JTA bulletins, even where the original bulletin page was read) and Wikipedia at the revision (oldid) given.
- **[C]** sources conflict; both values are kept.
- **[GAP]** not found in a record that could be opened. Each says what was tried.
- No figure, date, URL or quote was filled from memory or from a search snippet. A web-search result summary is a lead only and is never used as a source here.
- **JTA dates.** The date on a JTA archive page is the bulletin date. The event date is read from the dateline on the item in the original bulletin PDF ("See Original Daily Bulletin") and resolved from it. Datelines below were read on the page image.
- **Photograph licences.** Only a Commons file page that prints a public-domain template (for example PD-Israel) or CC0 is listed as free. Public domain is never inferred from age. The "US status" column quotes what the page prints; where the page prints no US tag, the cell says so and the US status stays [GAP] (see "What the licence lines do and do not say").
- Quotes are copied from the page. Cross-references are by repo path.

## Source caveat

- **Not reachable:** the Knesset site (HTTP 303 on `main.knesset.gov.il`; the 1959 law was read from a Wayback copy of `fs.knesset.gov.il`); the Eilat-Ashkelon Pipeline Company site (`eapc.co.il` returned a page with no text, script only). No primary was found for television, the pipeline or the Water Carrier, so those rows are press [S].
- The Bank of Israel pages were read in Wayback copies (the live site was not used).
- No Tavily key on this box: web lookups used the built-in WebSearch (leads only) and `curl` for pages, the Commons API and the JTA archive's own search (`/wp-json/wp/v2/archive`).

## 1. Banknotes (design 7.4, UI: "designs and dates to verify")

All rows [V, agent read 2026-10-25; a person's check pending] on the Bank of Israel's four lira series pages (BoI-A to BoI-D). Dates are the "Issue date" each page prints; the pages also print "Cancellation date March 31, 1984" for every note. The pound was divided into agorot from 1 Jan 1960 ([currencies-1948-93.md](../currencies-1948-93.md), row 8), but the 1959-60 Series B notes already print "Israeli Lira".

| Series (BoI name) | Note | Issue date as printed | Front motif as printed | Designer as printed |
|---|---|---|---|---|
| A, Landscapes | 500 pruta | 4 Aug 1955 | Ruins of an ancient synagogue at Bir'am in Upper Galilee | Thomas de la Rue and Co., London |
| A | 1 lira | 27 Oct 1955 | View of Upper Galilee | same |
| A | 5 lirot | 27 Oct 1955 | Negev landscape with a settlement and farm equipment | same |
| A | 10 lirot | 4 Aug 1955 | View of the Jezreel Valley depicting settlements and cultivated fields | same |
| A | 50 lirot | 19 Sep 1957 | The road to Jerusalem | same |
| B, Representative Figures | 1/2 lira | 15 Oct 1959 | Pioneer-woman soldier holding a basket of oranges against a background of fields | Shamir Brothers, Jacob Zim |
| B | 1 lira | 15 Oct 1959 | A fisherman carrying fishing equipment against a background of a bay | same |
| B | 5 lirot | 15 Oct 1959 | A laborer holding a sledge-hammer against a background of an industrial plant | same |
| B | 10 lirot | 15 Oct 1959 | A scientist in a laboratory | same |
| B | 50 lirot | 9 Dec 1960 | Two young pioneers against a background of an agricultural settlement in the Negev | same |
| C, Notable Personalities | 100 lirot | 27 Feb 1969 | Portrait of Theodor Herzl | Prof. Masino Bessi, Italy and H.J. Bard, Britain |
| C | 10 lirot | 6 Aug 1970 | Portrait of Haim Nahman Bialik | Prof. Masino Besi and Prof. M. Baiardi, Italy |
| C | 5 lirot | 13 Jan 1972 | Portrait of Albert Einstein | Prof. Masino Besi, Italy and Sam Hertz, Holland |
| C | 50 lirot | 13 Jan 1972 | Portrait of Chaim Weizmann | Prof. Masino Besi and Prof. M. Baiardi, Italy |
| D, Gates of Jerusalem | 10 lirot | 30 Jan 1975 | Portrait of Sir Moses Montefiore; the "Mishkanot Shaananim" quarter in Jerusalem with the windmill | Paul Kor, Adrian Senger |
| D | 100 lirot | 14 Mar 1975 | Portrait of Theodor Herzl; the entrance gate to Mt. Herzl in Jerusalem | same |
| D | 5 lirot | 11 Mar 1976 | Portrait of Henrietta Szold; Hadassah Hospital on Mt. Scopus in Jerusalem | same |
| D | 500 lirot | 26 May 1977 | Portrait of David Ben-Gurion; the library at Kibbutz Sde Boker | "Dutch artists" |

Not in the table: the Series D 50 lirot, issue date "January 26, 1978" (BoI-D), which falls outside the 1957-77 window.

What the pages say about the series as a whole (quotes copied):

- A: "Four notes were gradually put into circulation in August-October 1955. The fifth was issued in 1957." The back of each note is "An abstract design".
- B: "The abstract patterns of the first series did not meet with public approval." "The series was put into circulation in 1959-60, and gradually replaced the previous series."
- C: "a IL 100 note, bearing the portrait of Theodor Herzl, was introduced in February 1969." "The other notes in this series were put into circulation in 1970-72."
- D: "put into circulation in 1975-78."

Reading, for the art lane (derived, not a source claim): in 1957 the notes in a purse were Series A, abstract backs and landscape fronts, plus the first Series B notes from 15 Oct 1959; from 1969 the portrait notes. The 1957 to 1968 span has no portrait of a named person on any note listed above. Series C and D carry portraits of named persons and the State emblem (100 lirot, C, back: "Emblem of the State of Israel surrounded by the emblems of the Twelve Tribes"); the design's rule on logos and emblems (spec section 5) still governs any use. The 1948 to 1952 notes are in [currencies-1948-93.md](../currencies-1948-93.md). The table lists the notes whose motif or date this note's sources gave; the Series C 100 lirot variants and the later colour-of-numbering reprints on the pages are not repeated.

Free photographs of these notes: [GAP]. The Commons scans found (`File:Israel Lira 1958 Obverse & Reverse.jpg`, `File:Israel 5 Lira 1958 Obverse & Reverse.jpg`, `File:Israel 10 Lirot 1968 Obverse & Reverse.jpg`) print the licence "Use permitted by the BOI, Currency Department" in the Commons API field (file pages not opened), which is not public domain or CC0. They are not listed as free.

## 2. Television and radio (design 7.4, UI: "1968, to verify")

| Claim | Status and source |
|---|---|
| Educational television of local origin began on **24 March 1966**: "Pupils in 32 schools in Tel Aviv received today the first television broadcast of local origin", from a station "financed by the Rothschild family" | [S] JTA, bulletin of 25 Mar 1966. The dateline on the page image (200 dpi, re-read 2026-10-26) reads "TEL AVIV, March 24, (JTA)" (24 March 1966 was a Thursday). en.wiki "Television in Israel" (oldid 1379929651) also says "inaugurated on 24 March 1966". The original PDF is http://pdfs.jta.org/1966/1966-03-25_059.pdf |
| 11 Sep 1967 (Maariv's report): the Rothschild family would not let the donated installations be used for general programmes | [S] JTA, bulletin of 12 Sep 1967 (no. 176), p. 2, dateline on the page image "JERUSALEM, Sept. 11. (JTA)", citing Maariv "said today" |
| 17 Sep 1967: the Cabinet decided on "Three hours daily of television broadcasting in Arabic, and one in Hebrew ... as soon as it is 'technically feasible'" | [S] JTA, bulletin of 18 Sep 1967 (no. 180), p. 2, dateline on the page image "JERUSALEM, Sept. 17. (JTA)"; the Cabinet "decided today" |
| **2 May 1968**: the Independence Day military parade in Jerusalem was "televised ... live in Israel's first national TV broadcast" | [S] JTA, bulletin of 6 May 1968. Dateline on the page image: "JERUSALEM, May 5 (JTA)"; the item says "Thursday's Independence Day military parade", and 5 May 1968 was a Sunday, so Thursday is 2 May 1968. en.wiki (oldid 1379929651): "regular public transmissions on 2 May 1968". PDF: http://pdfs.jta.org/1968/1968-05-06_087.pdf |
| By Nov 1969 "Israel's only TV channel" ran | [S] JTA, bulletin of 28 Nov 1969, "Israel's infant television services"; dateline not read |
| Design 7.4's statement that radio is the news medium until the first broadcasts | [GAP]. No source was opened for radio's share of news before 1968. The one dated example seen is in section 5: a Kol Israel radio interview with Foreign Minister Eban, reported in the bulletin of 5 May 1969 [S] |

The design's "1968, to verify" is therefore supported at the level of press: the first live national broadcast was the Independence Day parade of 2 May 1968 [S], two years after educational television began [S]. A primary (Broadcasting Authority records, the Broadcasting Authority Law of 1965, Knesset or IBA publications) was not opened: **[GAP]**, Knesset unreachable.

## 3. Eilat-Ashkelon pipeline (design 7.4, Infrastructure: "its dates to verify")

| Date | Claim | Status and source |
|---|---|---|
| Announced 18 Jun 1968 | "Work will begin next week on a $32,287,000 pipeline that will carry oil from Eilat on the Gulf of Aqaba to Ashkelon ... The 42-inch pipeline will be over 200 miles long." | [S] JTA, bulletin of 19 Jun 1968. Dateline on the page image: "TEL AVIV, June 18 (JTA)"; "announced here today". PDF: http://pdfs.jta.org/1968/1968-06-19_117.pdf |
| In operation since **1 Feb 1970** (disclosed 16 Feb 1970) | "its new 42 inch oil pipeline has been in operation since Feb. 1 ... some 160 miles away"; "$150 million project"; "annual capacity of 20 million tons", to grow to "60 million tons"; "A British tanker presently at Ashkelon is the first vessel to load oil passing through the new pipeline" | [S] JTA, bulletin of 17 Feb 1970. Dateline on the page image: "TEL AVIV, Feb. 16 (JTA)". PDF: http://pdfs.jta.org/1970/1970-02-17_032.pdf |

[C] inside the press: the 1968 item gives the cost as $32,287,000 and the length as "over 200 miles"; the 1970 item gives "$150 million" and "some 160 miles". Both are kept; the first is a plan announced on the eve of construction, the second a report on the finished line, and the difference is not explained by either. The Cabinet or company record of the start of pumping is [GAP] (company site unreadable). The pipeline sits in a game year of 1970 and later; it does not appear before 1968.

## 4. National Water Carrier (design 7.4, Infrastructure: "open canal sections")

- [S] JTA, bulletin of 15 Jun 1964: "Tests continued today all along the 70-mile section of the National Water Carrier project from Lake Tiberias to the southern terminal of the northern section at Rosh Haayin"; "water to was flowing through the southern section to settlements in the Negev" [sic, as JTA prints it]. Dateline not read.
- [S] JTA, bulletin of 27 Aug 1964: the Carrier "was reported here today to be in continuous test operation"; the third pump "may take several more years". Dateline not read.
- The design's "June 1964" for the Carrier is therefore consistent with the June 1964 test reports [S]; an inauguration date and any statement of which sections were open canal and which were pipe: **[GAP]**. A JTA search for the inauguration (see search log) returned no item.
- Photograph of the open canal: [GAP]. The nearest free image found is `File:Bevloeiingswerken bij de kibboets Hakuk ...` (CC0, "1960-01-01" as the API prints), which shows irrigation works near Kibbutz Hakuk, not the Carrier. Not listed in the photo table.

## 5. Military markers (design 7.4: Centurion, Patton, AMX-13, Mirage III, Skyhawk, Phantom, Bar Lev Line)

"First year each is shown in service". Where [procurement-1957-77.md](procurement-1957-77.md) section 5 or [forces-1967-73.md](forces-1967-73.md) section 6 already holds the dated evidence, it is cross-referenced with its tag and **not re-opened here**.

| Type | First year the sources show it in service | Status and source |
|---|---|---|
| Centurion | Purchase agreed Oct to Nov 1958; "in Israeli use by Sep 1963". Wikipedia adds "first delivered in 1959" | The 1958 and 1963 evidence: see [procurement-1957-77.md](procurement-1957-77.md) section 5 (FRUS order dates, JTA for 1963, tags there). [S] en.wiki "Centurion (tank)" oldid 1381665962: "Israel's formerly British Centurions were first delivered in 1959" (its footnote cites a book, not opened). First delivery from a primary: [GAP] (as in procurement-1957-77.md). Seen in service in a photograph dated 8 Oct 1973 (section 6, Shot) |
| Patton (M48) | 40 delivered by 29 Jul 1965; the first arrival day is a gap | See [procurement-1957-77.md](procurement-1957-77.md) section 5, M48 row (tags there). Not re-opened |
| AMX-13 | [GAP] | en.wiki "AMX-13" oldid 1379391446: "By 1967, Israel had less than 100 AMX-13s and formed three AMX-13 battalions, all of which fought actively on all fronts during the Six-Day War" [S], and Israel "phased out all of its AMX-13s following the Six-Day War" [S]. No year of entry found. JTA search (log) gave no AMX item on its first page of results. The design lists the AMX-13 only for 1967, and the oldid above covers "in service by 1967" as [S]; the [GAP] is for the first-service year only |
| Mirage III | Order "signed ... in June 1960"; "more than 50 were believed to have already been delivered" by 21 Feb 1963 | [S] JTA, bulletin of 21 Feb 1963, which reports the London Daily Express, itself unconfirmed ("French sources had never officially confirmed a Mirage pact"). Dateline not read. A first delivery date from the Israeli or French record: [GAP], as in procurement-1957-77.md section 1. A Commons photograph of IAF Mirage IIIs dated "before June 1967" is in section 6 |
| Skyhawk | Deliveries began the last week of Dec 1967 | See [procurement-1957-77.md](procurement-1957-77.md) section 5 (JTA bulletin of 31 Jan 1968, tagged [S] there; FRUS counts for 1970). Not re-opened. Seen with Centurions in a photograph dated 8 Oct 1973 (section 6) |
| Phantom | First consignment reported delivered on Friday 5 Sep 1969 | See [procurement-1957-77.md](procurement-1957-77.md) section 5, Phantom row ([S]: Al Ahram as JTA reported it). Not re-opened |
| Bar Lev Line (strongpoints on the canal) | Named in the JTA bulletin of 5 May 1969 (dateline May 4) | [S] JTA, bulletin of 5 May 1969: "Israelis meanwhile ridiculed Col. Nasser's claim that 60 percent of the Bar Lev Line, Israel's fortifications on the Suez Canal, had been destroyed by Egyptian shelling." Dateline on the page image: "JERUSALEM, May 4 (JTA)"; "said yesterday" for the speech reaction, so the report is of 3 May 1969, and "Thursday" in it is 1 May 1969. PDF: http://pdfs.jta.org/1969/1969-05-05_085.pdf. When the line was built and in what stages: [GAP] |

One free photograph of a Bar Lev fort exists only under non-free-in-the-US terms: `File:Bar Lev line fort.jpg` prints PD-Egypt and the tag `Not-PD-US-expired-min-year` (template names from the Commons API, page not read in full), and `File:Bar Lev Line, November 1970.jpg` (opened; "Taken on 20 November 1970", IDF Spokesperson's Unit) is CC BY-SA 3.0. Neither is public domain or CC0 in the sense the issue asks for; neither is in the photo table.

## 6. Period photographs: CC0 (five) and PD-Israel (four, US status [GAP])

Opened on 25 Oct 2026, file by file; every row is [V, agent read 2026-10-25; a person's check pending] for what the page prints. "Licence template" is the Commons template name; "Page prints" is what the file page shows. **All CC0 rows are the Dutch National Archives' Van de Poll collection**, whose page credit line reads "Fotocollectie Van de Poll"; the page print of date is what the archive record says, not checked against another source.

| Design 7.4 item | Commons file page | Date and author as printed | Licence template as printed | US status line |
|---|---|---|---|---|
| Towns: development-town housing blocks | https://commons.wikimedia.org/wiki/File:Straat_met_nieuwbouwwoningen_-_flats_-_in_de_stad_Dimona%2C_Bestanddeelnr_255-3555.jpg (Dimona, "street with new housing, flats") | "Datum: 1 januari 1963" (a day-and-month that may be a placeholder; year 1963 as printed); Willem van de Poll | Cc-zero (CC0 1.0 Universal Public Domain Dedication) | None printed |
| Towns | https://commons.wikimedia.org/wiki/File:Dimona_Stadsgezicht%2C_Bestanddeelnr_255-3553.jpg (Dimona townscape) | "Datum: 1 januari 1963"; Willem van de Poll | Cc-zero | None printed |
| Towns: new town in the Galilee (Nazareth new quarter) | https://commons.wikimedia.org/wiki/File:Nazareth_Nieuwbouwwijk_met_flatgebouwen_en_wegen%2C_Bestanddeelnr_255-3299.jpg | "Datum: ongedateerd"; Willem van de Poll | Cc-zero | None printed. Undated, so it cannot be tied to 1957-77 from the page |
| Towns: Ashdod, general view | https://commons.wikimedia.org/wiki/File:General_view_of_Ashdod%2C_1968.png | 1968; Dov Dafnai; Jewish National Fund photo archive, digital ID D3015-016 | PD-Israel | The PD-Israel text on the page: "You must also include a United States public domain tag"; none is on the page |
| Rural: kibbutz dining hall | https://commons.wikimedia.org/wiki/File:Kibboets_Nir_Elyahu_Gemeenschappelijke_eetzaal_van_de_kibboets_met_meest_jonge_%2C_Bestanddeelnr_255-3703.jpg (Nir Elyahu) | "Datum: 1 januari 1963"; Willem van de Poll | Cc-zero | None printed |
| Rural: kibbutz dining hall | https://commons.wikimedia.org/wiki/File:Eetzaal_in_de_kibboets_Revivim_in_de_Negevwoestijn%2C_Bestanddeelnr_255-3958.jpg (Revivim) | "Unknown date"; Willem van de Poll | Cc-zero | None printed. Undated |
| Military: Mirage III | https://commons.wikimedia.org/wiki/File:Dassault_Mirage_III_of_Israeli_Air_Force.jpg | "before June 1967"; author "Israeli Air Force"; source printed as an IAF magazine of 1967 | PD-Israel | Same as Ashdod row: the page asks for a US tag and carries none |
| Military: Centurion ("Shot") in the Golan | https://commons.wikimedia.org/wiki/File:Shot_tanks_in_Golan_Heights.jpg | 08/10/1973; Uzi Keren; source printed as the Ministry of Defence's Yom Kippur War site (`yomkipurwar.mod.gov.il`, item 9159) | PD-Israel | Same |
| Military: Centurion and Skyhawk | https://commons.wikimedia.org/wiki/File:Shot_tanks_and_Skyhawk_plane.jpg | 08/10/1973; Uzi Keren; same MoD site (item 9143) | PD-Israel | Same |

**Item by item, what is still without a free photograph ([GAP]):** Nahal outposts; kibbutz water towers of the 1960s (a search for `watertoren` found Dutch-archive files of towers, but none was opened); the Golan's new settlements; the Water Carrier's open canal; the Patton, the AMX-13 and the Phantom; Bar Lev Line strongpoints; Israeli government print (a page, see section 7, is a record not a photograph). What was found and excluded:

- Boris Carmi "Kalya" (May 1969, Nahal-related search) and "Arad" (1966), and the Dan Hadani collection "Karmiel" (1969): licence field CC BY 4.0 in the Commons API listing; file pages not opened. Not public domain or CC0.
- "PikiWiki 3802, 3619, 3620 Settlements in the Golan Heights" (1967-68): licence field CC BY 2.5; not opened.
- The `Lehakat ha-Nahal.jpg` file (PD-Israel, 1960, opened) is a Nahal entertainment troupe with Ben-Gurion, not an outpost; not used.

### What the licence lines do and do not say

- A **CC0** page prints a dedication and no US line; CC0 is the licence, so no US tag is needed on the page. The Dutch archive's own record of who holds the rights was not opened.
- A **PD-Israel** page prints Israel's term rules and then "You must also include a United States public domain tag to indicate why this work is in the public domain in the United States." None of the four PD-Israel pages above carries one. **The US status of these four is therefore [GAP]**, and they should be treated as not cleared for US use until a US-status basis is found and read. ([derived] my own reasoning, not a read source: Israeli photographs taken from 1946 onward may have been protected in Israel on 1 January 1996, which would matter for the US; this was not researched and is not tagged.)
- The Commons template that carries a US tag for a US-government work (`PD-USGov`) appeared on scanned US Navy and other theses and reports in the search results; no US-government photograph of an Israeli aircraft or tank of the period was found.

## 7. UI: government print of the 1960s and 1970s

- [V, agent read 2026-10-25; a person's check pending] **Sefer HaChukim 289, 10 Av 5719 (14 Aug 1959), page 193, the Law to Amend the Currency Ordinance, 5719-1959.** One page, read as an image from a Wayback copy of the Knesset's own file: http://web.archive.org/web/20250413225814id_/https://fs.knesset.gov.il/3/law/3_lsr_208936.pdf (original path `fs.knesset.gov.il/3/law/3_lsr_208936.pdf`). What the printed page shows: the title in bold at the top with an asterisk footnote; numbered sections in bold ("1.", "2." ...); a narrow column of marginal side-heads in small type to the left of each section; justified Hebrew body text in a single column; the signatories' names in bold letterspaced type at the foot (Ben-Gurion, Eshkol, Ben-Zvi); footnotes under a rule; a footer line reading "ספר החוקים 289, י' באב תשי"ט, 14.8.1959" and the page number 193 at the lower left. The paper tone in the scan is plain white; the Wayback copy is a clean PDF, so **paper texture is [GAP]**.
- The design's "typography and paper textures" for the 1970s: **[GAP].** One page of 1959 was opened. No page of the 1970s, no Reshumot (Official Gazette) page and no ministry form was opened.
- The 1949 Flag and Emblem Law: **[GAP]**. The Knesset page was unreachable (HTTP 303); the design's own section 5 is the reference for what the game may show.

## 8. Rural and towns: dated sources for the types

- "Shikun" housing blocks of the development towns: the photographs above (Dimona 1963, Ashdod 1968, both as the pages print) show multi-storey flat blocks in new towns. A dated primary source that **names the building type** (a Ministry of Housing publication, a census of dwellings) was not opened: **[GAP]**. Founding dates of the development towns are in [development-towns-1957-77.md](development-towns-1957-77.md) and not repeated.
- Nahal outposts, 1960s kibbutz dining halls and water towers, the Golan's new settlements: the settlement dates are in [kibbutzim-moshavim-1957-77.md](kibbutzim-moshavim-1957-77.md), [golan-localities-1967-77.md](golan-localities-1967-77.md) and the other locality notes of this folder. A source for the **look** of a Nahal outpost: [GAP]. The kibbutz dining hall: the two CC0 photographs of section 6 (one dated 1963, one undated).

## Sources opened

| Id | Source and what it was used for | URL | Opened |
|---|---|---|---|
| BoI-A | Bank of Israel, "Series A of the Lira - Landscapes Series" (Wayback copy of 11 Jun 2026; page last updated 25/12/2025 as printed) | https://web.archive.org/web/20260611131823/https://www.boi.org.il/en/economic-roles/banknotes-and-coins/series-a-of-the-lira-landscapes-series/ | 25 Oct 2026 |
| BoI-B | Bank of Israel, "Series B of the Lira - Representative Figures Series" (Wayback copy of 7 Jun 2026) | https://web.archive.org/web/20260607053707/https://www.boi.org.il/en/economic-roles/banknotes-and-coins/series-b-of-the-lira-representative-figures-series/ | 25 Oct 2026 |
| BoI-C | Bank of Israel, "Series C of the Lira - Notable Personalities Series" (Wayback copy of 2 Sep 2026) | https://web.archive.org/web/20260902163107/https://boi.org.il/en/economic-roles/banknotes-and-coins/series-c-of-the-lira-notable-personalities-series/ | 25 Oct 2026 |
| BoI-D | Bank of Israel, "Series D of the Lira - Gates of Jerusalem Series" (Wayback copy of 15 Jun 2026) | https://web.archive.org/web/20260615045510/https://www.boi.org.il/en/economic-roles/banknotes-and-coins/series-d-of-the-lira-gates-of-jerusalem-series | 25 Oct 2026 |
| SH289 | Sefer HaChukim 289, p. 193 (Wayback copy of the Knesset file, 13 Apr 2025) | http://web.archive.org/web/20250413225814id_/https://fs.knesset.gov.il/3/law/3_lsr_208936.pdf | 25 Oct 2026 |
| JTA | JTA archive items named in sections 2 to 5, each opened through `https://www.jta.org/archive/<slug>` (a pattern, not a link: the slug is the item title); page images of the bulletins read for the datelines of 24 Mar 1966, 2 May 1968 (bulletin of 6 May), 18 Jun 1968, 16 Feb 1970, 4 May 1969 | slugs: `israel-launches-first-experimental-educational-tv-of-local-origin`, `tv-equipment-donated-by-rothschilds-must-not-be-used-for-general-broadcasts`, `israel-cabinet-agrees-on-limited-general-television-as-soon-as-feasible`, `eshkol-dayan-praise-army-police-for-success-of-jerusalem-parade`, `israels-television-service-runs-into-controversy-over-question-of-personal-opinion`, `construction-will-begin-next-week-on-eilat-to-ashkelon-pipeline-for-oil`, `new-oil-pipeline-linking-eilat-with-ashkelon-in-operation-since-february-1`, `tests-on-water-carrier-project-from-lake-tiberias-to-negev-continue`, `israel-continues-to-test-system-bringing-water-from-jordan`, `israel-reported-increasing-orders-with-france-for-jet-planes`, `eban-says-nassers-may-day-speech-strengthens-resolve-to-fortify-suez-line` | 25 Oct 2026 |
| enwiki | English Wikipedia, read through the API at the revisions: "Television in Israel" oldid 1379929651; "Centurion (tank)" oldid 1381665962; "AMX-13" oldid 1379391446. Leads only | https://en.wikipedia.org/w/index.php?oldid=1379929651 , https://en.wikipedia.org/w/index.php?oldid=1381665962 , https://en.wikipedia.org/w/index.php?oldid=1379391446 | 25 Oct 2026 |
| Commons | The nine Commons file pages of section 6 and `File:Bar_Lev_Line,_November_1970.jpg` (opened, for the licence only) | URLs in section 6 and https://commons.wikimedia.org/wiki/File:Bar_Lev_Line%2C_November_1970.jpg | 25 Oct 2026 |

## Search log

Every negative or partial search, the day run (25 Oct 2026), the tool and the count it returned. "First page" means the ten results shown; titles were read, the items were not opened unless named above.

| Query and tool | Date range | Returned | Result |
|---|---|---|---|
| JTA archive search, `https://www.jta.org/wp-json/wp/v2/archive?search=Israel+television+first+broadcast&per_page=10&after=1966-01-01T00%3A00%3A00&before=1969-12-31T00%3A00%3A00` | 1966-01-01 to 1969-12-31 | 22 hits | First page held the 28 Nov 1969 item (opened) and nine unrelated titles |
| JTA, `search=Israel+television+Independence+Day+broadcast`, same URL pattern | 1968-01-01 to 1968-12-31 | 5 hits | Two parade items; the 6 May item opened |
| JTA, `search=television` | 1968-04-25 to 1968-05-15 | 5 hits | None about Israeli television except the 6 May item |
| JTA, `search=Israel+television+service+begins` | 1968-03-01 to 1968-06-30 | 0 hits | none |
| JTA, `search=Israel+TV+first+telecast` | 1968-01-01 to 1968-12-31 | 0 hits | none |
| JTA, `search=Israel+television+broadcasts+begin` | 1968-01-01 to 1968-12-31 | 4 hits | none about Israeli television (Jordan TV, Poland, a Beisan mortar item, the parade plans) |
| JTA, `search=Water+Carrier` | 1964-04-01 to 1964-08-31 | 31 hits | First 15 titles read; two Carrier items (15 Jun, 27 Aug) opened; no inauguration item among them |
| JTA, `search=National+Water+Carrier+inaugurated+Eshkol` and `search=water+carrier+Jordan+Negev+open+canal+pipeline+completed` | 1964-05-01 to 1964-07-31; 1964-01-01 to 1964-12-31 | 0 hits each | none |
| JTA, `search=Eilat-Ashkelon+pipeline` | 1960-01-01 to 1972-12-31 | 17 hits | First page: 17 Feb 1970 item (opened) and items of 1970 to 1972 |
| JTA, `search=pipeline+Eilat+Ashkelon+construction` | 1968-01-01 to 1969-06-30 | 5 hits | 19 Jun 1968 item (opened) |
| JTA, `search=Israel+pipeline+Eilat+Ashkelon+Iran+oil` | 1967-06-01 to 1969-12-31 | 0 hits | none |
| JTA, `search=AMX+tanks+Israel` | 1955-01-01 to 1967-06-01 | 223 hits | First ten titles read: none about tanks; not opened. JTA's ranking is loose |
| JTA, `search=AMX` | 1956-01-01 to 1958-12-31 | 234 hits | First ten titles read: none about tanks; not opened |
| JTA, `search=Sherman+tanks+Israel+French+AMX+light+tanks` | 1956-01-01 to 1957-12-31 | 0 hits | none |
| JTA, `search=Mirage+Israel+Air+Force+delivered` | 1962-01-01 to 1963-12-31 | 1 hit | the 21 Feb 1963 item (opened) |
| JTA, `search=Bar-Lev+Line` | 1969-01-01 to 1971-12-31 | 175 hits | First ten titles read (Nov to Dec 1971); the 5 May 1969 item came from the next search |
| JTA, `search=Bar-Lev+line+fortifications+Suez+Canal` | 1969-03-01 to 1970-06-30 | 8 hits | the 5 May 1969 item (opened) among them |
| Commons API, `https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrnamespace=6&gsrsearch=<query>` (pattern; queries follow): `Nahal settlement 1960s Israel`; `Nahal soldiers outpost` | all dates | 8 files each (limit 8) | All CC BY 4.0 or CC BY 2.5 (Kalya 1969; photographs of 2024 to 2025); none public domain or CC0 |
| Commons, `Golan Heights settlement 1968` | all dates | 8 files | Three PikiWiki files of 1967-68 (CC BY 2.5); five unrelated PDFs |
| Commons, `hastemplate:PD-Israel Nahal 1960` | all dates | 1 file | `Lehakat ha-Nahal.jpg` (a troupe, not an outpost) |
| Commons, `hastemplate:PD-Israel Phantom`; `hastemplate:PD-Israel Golan 1969 settlement` | all dates | 0 files each | none |
| Commons, `Israeli AMX-13 tank` | all dates | 8 files | All museum photographs taken 2006 to 2015, CC BY or CC BY-SA |
| Commons, `M48 Patton Israel 1967`; `Centurion Sho't 1967 Sinai`; `Skyhawk Israeli Air Force 1970s` | all dates | 2 files; 0 files; 2 files | A CC BY-SA 4.0 museum photograph and PDFs; none of the period |
| Commons, `Israeli pound 5 lirot 1968` | all dates | 0 files | none; `Israel 10 Lirot 1968 Obverse & Reverse.jpg` came from another query |
| Commons, `Bestanddeelnr 255 Israël Nahal`; `... Kirjat Sjmona`; `... woningbouw nieuwe stad`; `... waterleiding Jordaan` | all dates | 0 files each | none |
| WebSearch (built-in), two queries on the IAF's types and the Water Carrier | all dates | 9 and 10 results | Leads only (no IAF or Mekorot page among them); no fact from them is used |
| `curl` of the Knesset law page `main.knesset.gov.il/en/about/lawsandregulations/pages/lawflag.aspx` and `fs.knesset.gov.il/3/law/3_lsr_208936.pdf` | n/a | HTTP 303 (48 bytes) | blocked; the Wayback copy of the second was used |
| `curl` of `https://www.eapc.co.il/en/` | n/a | HTTP 200, no page text | script only |
