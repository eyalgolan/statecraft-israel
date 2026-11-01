# Era look 1977 to 1993: dated sources and free photo links

Researched 26 October 2026. Every URL in the sources table was opened on that day by an agent; a person's check of the sources is pending. **Research only:** a new note for issue #166 (the chapter on screen, 1977-93), the research part the owner opened on 24 Oct. It links to photographs and records their licence as the Commons page prints it; **it holds no image file** and changes nothing under `content/`. The chapter's screens and art stay paused until after the MVP. The design is [the 1977-1993 design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md), section 7.5 (lines 614 to 620 at commit 55dd6489), whose every item is "to verify". Its television item is not in the owner's list of 24 Oct and is not covered here.

## Conventions

- **[V, agent read 2026-10-26; a person's check pending]** a primary record opened on the day: a Bank of Israel (BoI) page, or a Commons file page read for what it prints (licence template, date, author). It is [V] for what the page says, not for any fact behind it.
- **[S]** secondary: press (JTA bulletins) and Wikipedia at the revision (oldid) given.
- **[C]** sources conflict; both values are kept.
- **[GAP]** not found in a record that could be opened. Each says what was tried.
- No figure, date, URL or quote was filled from memory or from a search snippet. Where an item's wording is quoted it was read in the item's full text (JTA's own archive API, `https://www.jta.org/wp-json/wp/v2/archive`) and the bulletin PDF was read through OCR of the page image.
- **JTA dates.** The date on a JTA archive page is the bulletin date. The event date is read from the dateline on the item in the original bulletin PDF ("See Original Daily Bulletin") and resolved from it. Datelines below were read in the OCR text of the PDF page image (OCR can garble a digit; where it did, the row says so).

- **Second pass, 27 Oct 2026.** A second pass on the [GAP] rows, added after the first-pass text, which is kept as it was. Where a first-pass line is overtaken, the new line follows it and starts "(second pass, 27 Oct)". The tag legend above is unchanged; the second pass uses it as written, with the day 2026-10-27 in the verified tag for pages opened on that day. JTA items are [S] as in the first pass; each JTA item added below was found through JTA's own archive API and its bulletin PDF was read through OCR of the page image on 27 Oct, for the dateline and for the key words of each quote (OCR can garble a word; a quoted sentence or part of one that the OCR did not show is the API text).
- **Photograph licences.** Only a Commons file page that prints a public-domain template or CC0 is listed as free. Public domain is never inferred from age. "US status" quotes what the page prints; where the page prints no US basis the cell says so and the status stays [GAP]. A licence read only from the Commons API field (file page not opened) is never listed as free.
- Quotes are copied from the page; a dash or a stray space inside a quote is normalised to the repo's no-dash rule. Cross-references are by repo path.

## Source caveat

- **Not reachable from this box:** the Israeli Air Force site (`https://www.iaf.org.il/` returned HTTP 403 "Access Denied" on 26 Oct 2026; an Internet Archive copy tried the same day returned 14 bytes), the Knesset site and the US Army Heritage Center file named on the 1979 Merkava page (`emu.usahec.org`, no file was returned). Delivery years of the aircraft therefore rest on JTA bulletins already held in [procurement-1977-93.md](procurement-1977-93.md), not on an IAF page.
- The BoI pages were read in Wayback copies (the live site answers scripts with a browser check).
- No Tavily key on this box: web lookups used `curl` against the Commons API, the English Wikipedia API and JTA's archive API. The built-in WebSearch was not used.

## 1. The shekel's banknotes from 1980 (design 7.5: "the shekel's banknotes from 1980 as a cue")

Dates of the change: 22 Feb 1980 (legal tender) and 24 Feb 1980 (first notes) are in [currencies-1948-93.md](../currencies-1948-93.md) row 10 and [economy-1977-93.md](economy-1977-93.md), not re-sourced. This section adds what the notes looked like.

All rows [V, agent read 2026-10-26; a person's check pending] on the BoI "Sheqel Series" page (BoI-SH). The page prints, for every note, "Cancellation date September 4, 1986" and the designers and signatures as shown.

| Note | Issue date as printed | Colour as printed | Front as printed | Designer as printed |
|---|---|---|---|---|
| 1 sheqel | 24 Feb 1980 | Purple | Portrait of Sir Moses Montefiore; the "Mishkanot Shaananim" quarter in Jerusalem with the windmill | Paul Kor, Adrian Senger |
| 5 sheqalim | 24 Feb 1980 | Green | Portrait of Chaim Weizmann; the Wix Library at the Weizmann Institute of Science | same |
| 10 sheqalim | 24 Feb 1980 | Blue | Portrait of Theodor Herzl; the entrance gate to Mt. Herzl in Jerusalem | same |
| 50 sheqalim | 24 Feb 1980 | Ivory-brown | Portrait of David Ben-Gurion; the library at Kibbutz Sde Boker | Dutch artists |
| 100 sheqalim | 11 Dec 1980 | Orange-brown | Portrait of Ze'ev Jabotinsky; the old inn "Shuni" near Binyamina | Dutch artists |
| 500 sheqalim | 1 Dec 1982 | Red | Portrait of Baron Edmond de Rothschild; a group of agricultural workers | Zvi Narkiss |
| 1,000 sheqalim | 17 Nov 1983 | Green | Portrait of Maimonides (Rambam); a passage from his manuscript of the "Mishneh Torah" | Zvi Narkiss, assisted by Arie Glazer |
| 5,000 sheqalim | 9 Aug 1984 | Blue | Portrait of Levi Eshkol; a panorama of united Jerusalem | Jacob Zim |
| 10,000 sheqalim | 27 Nov 1984 | Orange | Portrait of Golda Meir; a stylized tree against a seven-branched candelabrum | Asher Kalderon |

What the page says about the series (quotes copied):

- "The notes were prepared in the same colour and size and with the same portraits as the pound series in order to make it easier for the public to become familiar with the new denominations."
- "This was the largest series of banknotes in the history of the State of Israel. Initially there were four denominations (1,5, 10 and 50 sheqalim), but as inflation accelerated, another five were added between 1981 and 1985 - 100, 500, 1,000, 5,000 and 10,000 sheqalim." The table above gives 11 Dec 1980 for the 100, which is earlier than the page's "between 1981 and 1985" (**[C]** inside the page; both kept).
- "Beginning with the IS 500 denomination, the banknotes were printed in distinct colours and standard size (76x138 mm)".
- The page also prints a note on the 1,000: "There was a printing error on some of the banknotes: Instead of "Harav" in Hebrew, the word is misspelled "Rarav"" (the Hebrew words are not copied here).

The new sheqel (1 new sheqel = 1,000 sheqalim from 4 Sep 1985; see [currencies-1948-93.md](../currencies-1948-93.md) row 13) is inside the era, so its first-series notes to 1993 are added, from the BoI "First Series of the New Sheqel" page (BoI-NIS), [V, agent read 2026-10-26; a person's check pending]. The page's introduction says: "On September 4, 1985 the new sheqel, equal to 1,000 old sheqalim, became the currency of Israel." Each note's cancellation date is printed in Hebrew and reads 1 July 2000.

| Note | Issue date as printed | Colour | Front as printed |
|---|---|---|---|
| 1 new sheqel | 8 May 1986 | Green | Portrait of Maimonides |
| 5 new sheqalim | 4 Sep 1985 | Blue | Portrait of Levi Eshkol |
| 10 new sheqalim | 4 Sep 1985 | Orange | Portrait of Golda Meir |
| 20 new sheqalim | 2 Apr 1988 | Dark-grey | Portrait of Moshe Sharett |
| 50 new sheqalim | 4 Sep 1985 | Purple | Portrait of S.Y. Agnon |
| 100 new sheqalim | 19 Aug 1986 | Brown | Portrait of Itzhak Ben-Zvi |
| 200 new sheqalim | 16 Feb 1992 | Orange | Portrait of Zalman Shazar |

**[C]** on the 20: the page's introduction says "in 1986, for the first time since the establishment of the state, an intermediate denomination (NIS 20) was issued", and the note's own block prints "April 2, 1988". Both are kept; neither is explained by the page. The 1 new sheqel row (8 May 1986) is also later than the 4 Sep 1985 start the page gives for the series; the page does not say why.

Reading, for the art lane (derived, not a source claim): a purse in 1980 held the pound-series look (same colours, sizes and portraits) under sheqel numerals; from 1982 the new colours; from 1985 the same portraits again under "new sheqel" numerals. The design's logo and emblem rule still governs any use of a note's printed design.

**Free photographs of these notes: [GAP].** The Commons scans found carry the template "Use permitted by the BOI, Currency Department". `File:Israel 10000 NIS Bill 1984.jpg` was opened: it prints that template with the BoI notice "This design is copyrighted by the Bank of Israel, and its use is permitted by the bank under certain limitations", and **also** a separate "Public domain" template ("I, the copyright holder of this work, release this work into the public domain") from the uploader's own scan. The two conflict (**[C]**); the page does not settle whose rights govern the printed design, so it is not listed as free. The other scans (for example `File:Israel 1000 Sheqalim 1983 Obverse & Reverse.jpg`, `File:Israel 5000 Sheqalim 1984 Obverse & Reverse.jpg`, `File:Israel 50 Shkalim 1978 Obverse & Reverse.jpg`) print the BoI template in the API field; their pages were not opened and they are not listed.

(second pass, 27 Oct) Three scans whose pages were not opened on 26 Oct were opened on 27 Oct, [V, agent read 2026-10-27; a person's check pending]: `File:Israel 1000 Sheqalim 1983 Obverse & Reverse.jpg`, `File:Israel 5000 Sheqalim 1984 Obverse & Reverse.jpg` and `File:Israel 50 Shkalim 1978 Obverse & Reverse.jpg`. Each prints "Self-scanned" as the source and the permission template "Use permitted by the BOI, Currency Department", with the condition "subject to the conditions set forth in the directive entitled "Instructions concerning the use of photocopies and replicas of coins and banknotes"". The 1000 and the 50 pages print the date "20 May 2001" (added 2026-10-27: the 5000 page prints it too); the 1000 page prints the author as the Bank of Israel, "designed by Zvi Narkis", and the 50 page prints "Bank of Israel". None of the three prints a public-domain or CC0 template. Free photographs of the notes stay **[GAP]**; the conflict on `File:Israel 10000 NIS Bill 1984.jpg` stays **[C]**.

## 2. The Merkava (design 7.5)

Dates are not re-sourced. [procurement-1977-93.md](procurement-1977-93.md) section 2 holds them with their tags: the 15 May 1977 announcement of the production tank (1957-77 note), the Mark 1 "entered official service in 1979" ([S] en.wiki, no contemporaneous source, **[GAP]**), the Mark 2 as **[C]** between 4 Oct 1983 (JTA, public unveiling), 28 Oct 1984 (JTA) and April 1983 (en.wiki), and the Mark 3 "introduced in December 1989" ([S] en.wiki, **[GAP]** for a contemporaneous source).

Photograph: see section 8, row 1 (Mark 1, dated 9 Sep 1979 on the page). Mark 2 and Mark 3 free photographs: **[GAP]**. Found and excluded: the IPPA photographs of the first Merkava Mark 1s on the Golan, "Taken on 24 April 1980", and of the unveiling of the Mark II, "Taken on 4 October 1983", print CC BY 4.0 in the Commons API field (pages not opened); CC BY 4.0 is not public domain or CC0.

(second pass, 27 Oct) **Contemporaneous press for the Mark 1 and the Mark 3** (the first pass held only en.wiki for both). All [S] JTA:

| Event date | Claim | Status and source |
|---|---|---|
| 11 Dec 1979 | Of a former general: "In that capacity he was in charge of developing the Israel-made Merkava tank that was recently put into service." | [S] JTA, bulletin of 12 Dec 1979, https://www.jta.org/archive/weizman-depends-plan-to-reorganize-army-unit-under-a-single-command-and-tal-as-its-commanding-office. OCR of the bulletin page (PDF http://pdfs.jta.org/1979/1979-12-12_235.pdf) reads the item's dateline as "JERUSALEM, Dec: 11" (the agency tag is garbled) and the words "Merkava tank that wos recently put into service" (OCR error "wos"); the quote is the API text. It supports service by Dec 1979 without a day; the Mark 1 "entered official service in 1979" stays [S] for the year and **[GAP]** for the day |
| 21 Feb 1980 | "Premier Menachem Begin had lavish praise today for the Merkava tank, designed and manufactured in Israel, and the man who designed it, Gen. Israel Tol. Begin was the guest of the army ordnance corp at an installation where the tank is produced." ("Gen. Israel Tol" as printed, 2026-10-27) | [S] JTA, bulletin of 22 Feb 1980, https://www.jta.org/archive/israel-intelligence-confirms-plo-has-soviet-tanks. OCR dateline "TEL AVIV, Feb. 21 (JTA)" on the item; the OCR reads the sentence with errors, the quote is the API text |
| 3 Sep 1990 | "In ordinary conditions, Israel would not acquire these tanks [US surplus M-60s], because they would come at the expense of producing Israeli-made Merkava III tanks, which are far superior to the M-60s." | [S] JTA, bulletin of 4 Sep 1990, https://www.jta.org/archive/reported-u-s-arms-offer-to-israel-would-enhance-defenses-against-iraq. OCR dateline "TEL AVIV, Sept. 3" (agency tag garbled); PDF http://pdfs.jta.org/1990/1990-09-04_166.pdf. It shows the Mark III existed and was being produced by Sep 1990 (our reading of "producing"); it gives no introduction date, so "December 1989" stays [S] en.wiki and **[GAP]** for a contemporaneous source |

The JTA search for `Merkava`, 1989-09-01 to 1990-03-31, returned 5 items; 2 matched the word and both are the land-mine items of 29 and 30 Jan 1990 (Merkava tanks in the security zone, no mark named). Full search record in the second-pass search log below.

(second pass, 27 Oct) **Photographs, pages opened on 27 Oct, [V, agent read 2026-10-27; a person's check pending].** `File:Israeli Merkava Mk II Tank Unveiled in 1983.jpg` prints "Date 1983/10/04", the caption "The Mark II version of the locally produced tank, the Merkava, is unveiled at the Armoured Corps memorial day ceremony at Latrun Photo by Gil Hadani", the author "Israel Press and Photo Agency (I.P.P.A.) photographer" and the template Cc-by-4.0 ("licensed under the Creative Commons Attribution 4.0 International license"). `File:The first Israeli made Merkava 1 tanks in action on the Golan heights (FL62778349).jpg` prints "Taken in 1980" and Cc-by-4.0, credit "Dan Hadani collection / National Library of Israel". CC BY 4.0 is not public domain or CC0, so neither is listed as free. The source of the 1979 Mark 1 photograph (section 8, row 1), `https://emu.usahec.org/alma/multimedia/1444722/20183574PHBW1013047059F0000000163241I068.pdf`, was tried on 27 Oct: the host name did not resolve from this box; three Wayback attempts timed out. Row 1's caution stands. Commons search `filetype:bitmap Merkava` returned 50 files, 4 labelled public domain or CC0 in the API (a 2008 photograph, a 2025 CCTV frame, a Philippine Army photograph of 2022, and an undated `File:Merkava spring.jpg` whose page was not opened); none is dated to 1977-93. Free photographs of the Mark 2 and Mark 3: **[GAP]**.

## 3. The M113 (design 7.5)

| Claim | Status and source |
|---|---|
| A US sale of "700 M-13 armored personnel carriers" was proposed in June 1977 | [S] JTA, bulletin of 27 Jun 1977, "Begin Welcomes U.S. Move to Sell Israel $115 Million in New Arms", https://www.jta.org/archive/begin-welcomes-u-s-move-to-sell-israel-115-million-in-new-arms. Quote as printed: "700 M-13 armored personnel carriers". The type name is printed "M-13", not "M-113"; that this means the M113 is **our reading**; no item read prints "M113" for 1977. The OCR of the PDF page reads the dateline as "JERUSALEM, June 26 (JTA)" with garbling, so the report is of 26 Jun 1977 ("today" is the Times story's day); PDF http://pdfs.jta.org/1977/1977-06-27_122.pdf |
| The M113 in IDF service | [S] en.wiki "M113 armored personnel carrier" (oldid 1380603322, read 2026-10-26): "In 1970 Israel started to receive M113A1 to replace the antiquated half-tracks." and "In the 1982 Lebanon War, they saw heavy action." No primary found: **[GAP]** (see search log: JTA has no item that prints "M113" or "M-113" among those read) |
| Armoured personnel carriers in the 1982 war | [S] JTA, bulletin of 18 Jun 1982, https://www.jta.org/archive/correspondent-finds-beirut-peaceful ("Further back from the front, soldiers were repairing and maintaining their tanks, armored personnel carriers and other pieces of equipment"), type not named; found by the query `armored personnel carrier`, 1977 to 1993 (search log), not by the 1982 query |

Photograph: **[GAP]** for the period. Found and excluded: `File:IDF in M113 Patrolling in Lebanon (Operation Peace for Galilee), 1984.jpg` (IPPA, "1984-06-18") prints CC BY 4.0 in the API field, page not opened; `File:Destroyed M113 - Battle of Suez.jpg` was opened and prints PD-Egypt together with the tag `Not-PD-US-expired-min-year`, so it is not public domain in the US (and is of October 1973, before the era).

(second pass, 27 Oct) **The type name is now sourced.** [V, agent read 2026-10-27; a person's check pending] FRUS 1977-80, volume 8 (TEI file `https://raw.githubusercontent.com/HistoryAtState/frus/6170e4b32c1224002db2fe0c5d8d910597cddc2e/volumes/frus1977-80v08.xml`, document n=43, "Summary of Conclusions of a Policy Review Committee Meeting", Washington, 25 Jun 1977), under "Israeli Arms Requests": "The President's decision to provide Israel with 200 TOW launchers, 700 M-113 Armored Personnel Carriers and 15 M-728 Combat Engineering Vehicles was announced." (the volume prints the type with an en dash, "M–113", here normalised to a hyphen as the conventions say) The same document says "The 300 remaining APCs from the Israeli request might also be approved after the visit." This settles what the first pass called "our reading": the "700 M-13 armored personnel carriers" of the JTA bulletin of 27 Jun 1977 are the 700 M-113s of this record (same number, same week); the JTA spelling "M-13" is the bulletin's. Correction 2026-10-27 to "This settles": it should read "This supports what the first pass called "our reading" (derived: the same items in the same numbers, 200, 700 and 15, in the same week; not stated in either record)". It is a US decision to supply, not a delivery or an IDF service date. This is the same document the procurement note already cites for other 25 Jun 1977 rows ([procurement-1977-93.md](procurement-1977-93.md) section 3), where it is read for the F-16 line.

The M113 "in IDF service" (the 1970 start and the 1982 use): still [S] en.wiki and **[GAP]** for a primary. Searched on 27 Oct: FRUS 1969-76 volume 23 (Arab-Israeli Dispute 1969-1972, same raw.githubusercontent.com path, `frus1969-76v23.xml`): 0 matches for "M-113" or "M–113" (hyphen and en dash), 4 for "armored personnel carriers", none of which names an M113. A first run of the FRUS 1977-80 volume 8 search with only a hyphen in the pattern found 0 matches for the type; the volume prints the en dash, so a search must allow both.

(second pass, 27 Oct) Photograph: `File:IDF in M113 Patrolling in Lebanon (Operation Peace for Galilee), 1984.jpg` was opened, [V, agent read 2026-10-27; a person's check pending]: it prints the description "IDF patroling over the Lebanese town of." (as printed, incomplete), "Date 18 June 1984", author "Israel Press and Photo Agency (I.P.P.A.) photographer", the template Cc-by-4.0 and a National Library of Israel digital ID. CC BY 4.0 is not public domain or CC0; not listed as free. Photograph of the M113 for the period: **[GAP]**.

## 4. The F-15 and the F-16 (design 7.5: "from their sourced delivery years")

The delivery years are already sourced in [procurement-1977-93.md](procurement-1977-93.md) section 3 and are **not re-sourced**; they are repeated here only in the form the art lane needs, with each row's tag from that note.

| Aircraft | First year in Israel, per that note | Tag there |
|---|---|---|
| F-15 | The first three of 25 "landed ... Friday afternoon" 10 Dec 1976 (JTA dateline "TEL AVIV, Dec. 12 (JTA)") | [S]; **[C]** with en.wiki's "since 1977" |
| F-16 | "last summer" 1980, the first arrival, in a Nov 1980 report; the day is **[GAP]** | [S]; **[GAP]** on the day |
| F-16C | First three on Monday 9 Feb 1987 (JTA) | [S] |

The IAF's own page for either type is **[GAP]**: `iaf.org.il` returned HTTP 403 (see Source caveat).

Photographs: **no free period photograph found.** Opened, public domain in the US, but of the type and not of the era, so listed in section 8 as "type only": three US Air Force photographs dated 2004, 2009 and 2013 that show Israeli aircraft. Found and excluded: the IPPA "Another IDF Pilot Course ended with an outstanding airplanes display" series ("Taken in 1981") prints CC BY 4.0 in the API field; `File:Operation Opera. Israeli Air force history branch report. VIA.jpg` ("Taken on 16 June 1981") prints CC BY-SA 3.0 in the API field; pages not opened.

(second pass, 27 Oct) The first-pass line "`iaf.org.il` returned HTTP 403" was repeated once on 27 Oct (`curl https://www.iaf.org.il/`): HTTP 403 again; the IAF's own page stays **[GAP]**. The day of the first F-16's arrival in 1980 stays **[GAP]**: JTA `F-16`, 1980-06-01 to 1980-09-30, returned 3 items (2 name the F-16: the bulletin of 7 Aug 1980, "the speed with which it absorbed the American F-16 jets, almost 18 months ahead of schedule", and the year review of 11 Sep 1980, whose February entry is about the sale); JTA `F-16`, 1980-01-01 to 1980-12-31, returned 15 items, 1 of which pairs the F-16 with "arrived" (the Nov 1980 item already cited in section 4's table: "last summer"); JTA `F-16 jets arrive`, 1980-03-01 to 1980-08-31, returned 0. The en.wiki article "General Dynamics F-16 Fighting Falcon" (oldid 1382023348) was searched for the Israeli first-delivery date and none of its matching lines gave one. Photographs of the F-15 and F-16 of the era: the Commons query `filetype:bitmap Israel F-16 1981` returned 11 files and `filetype:bitmap Israeli F-15 1980s` returned 5; the only free-labelled file is a US White House contact sheet of 1976-12-08, not an Israeli aircraft; no free period photograph was added.

## 5. The security zone's outposts (design 7.5)

The zone and its dates are in [lines-1977-93.md](lines-1977-93.md) section 3.6 (the cabinet plan of 14 Jan 1985; the UN reports). This section adds dated press evidence that the zone was held by outposts, and what is known of their look.

| Event date | Claim | Status and source |
|---|---|---|
| 24 Apr 1985 | "The last Israeli soldier was lifted by helicopter from Jebel Barukh shortly after 11 a.m."; "The expensive electronic surveillance equipment had long since been removed from that outpost and what installations remained were blown up to prevent their falling into the hands of terrorists." | [S] JTA, bulletin of 25 Apr 1985, https://www.jta.org/archive/idf-completes-its-penultimate-stage-of-withdrawal-from-lebanon. OCR dateline "TELAVIV, April 24 (JTA)"; "today" is 24 Apr 1985. PDF http://pdfs.jta.org/1985/1985-04-25_079.pdf. The item also calls the Jebel Barukh point an "observation post"; it was left in the withdrawal and was outside the retained zone (our reading), so it shows what outposts were like, not the zone's final line |
| 3 Jul 1991 | "a successful hit-and-run attack on a well-defended IDF outpost on the slopes of Mount Hermon, at the eastern end of the security zone" | [S] JTA, bulletin of 8 Jul 1991, https://www.jta.org/archive/israel-not-yet-ready-to-give-up-security-zone-in-southern-lebanon. The item's own date "July 3" is in its text; OCR dateline "JERUSALEM, July 7 (JTA)". PDF http://pdfs.jta.org/1991/1991-07-08_128.pdf |
| 1 Mar 1992 | "Two mortar shells were fired at an Israel Defense Force outpost at the edge of the southern Lebanon security zone Sunday" | [S] JTA, bulletin of 2 Mar 1992, https://www.jta.org/archive/mortar-fired-at-idf-in-lebanon-breaks-nine-day-lull-in-combat. Dateline not read; "Sunday" before 2 Mar 1992 (a Monday, computed here) is 1 Mar |
| 17 Jul 1987 (bulletin) | A general: "The IDF can't fight in a routine manner within the framework of the war against terrorism-maintaining outposts and patrolling." | [S] JTA, bulletin of 17 Jul 1987, https://www.jta.org/archive/commander-concerned-that-idf-counter-terrorism-in-lebanon-detracts-from-deterring-syria; dateline not read |

The SLA's outposts (the Israeli-backed militia's) appear in the same press, for example 8 Jul 1985 (https://www.jta.org/archive/sla-hit-by-serious-defections, a report from Beirut that a Shiite force overran an SLA outpost at Kantara; [S], dateline not read), 9 Feb 1987 and 4 Mar 1993 (search log). They are not repeated.

What an outpost looked like (walls, towers, the standard layout): **[GAP]**. The JTA items name outposts and say nothing of their form.

Photographs: **[GAP]** for outposts. Opened: `File:Angry Shias burn an Israeli jeep.png` is a free period photograph (section 8, row 2) but shows a vehicle, not an outpost. Not found: any photograph of an outpost with a free licence in `Category:Israel Defense Forces in Southern Lebanon` (76 files scanned, none public domain or CC0; its 16 subcategories, among them the Brosh, Dlaat, Galagalit, Hamama and Karkom military posts, were not scanned).

(second pass, 27 Oct) **The form of an outpost: still [GAP].** JTA search `outpost`, 1985-01-01 to 1993-12-31, returned 100 items of 226 (`x-wp-total`; the API page cap is 100, so the first 100 by date were examined; read to 15 Dec 1988, so 1989-93 was not reached and later items were not read); one matched a form word (barbed wire, tower, bunker, concrete and the like near "outpost", "post" or "position"), the bulletin of 16 Jun 1988 on a UNIFIL "temporary post, enclosed with their own barbed wire", a UN post and not an IDF outpost. `fortified`, same range: 100 of 373 (read to 22 Sep 1986; later items not read), 2 matched, about Golan positions (5 Sep 1985) and Syrian works in the Bekaa (14 May 1986), neither about the zone. `bunker`, same range: 100 of 916 (read to 21 Apr 1986; later items not read), 0 matched. Dates added 2026-10-27 from a re-run of the three searches (same totals 226, 373, 916; last item read by the API's date field): none of the three reached 1989-93, so the [GAP] for those years rests on items not read. The en.wiki article "South Lebanon conflict (1985–2000)" (oldid 1381019033) names IDF and SLA outposts (Bra'shit 1987, Beit Yahoun, 14 outposts attacked in May 1999) and says nothing of their form.

(second pass, 27 Oct) **Photographs.** All 16 subcategories of `Category:Israel Defense Forces in Southern Lebanon` that the first pass named but did not scan were listed through the API on 27 Oct: Aishiya 11 files, Brosh 2, Dlaat 6, Galagalit 4, Hamama 0, Karkom 4, Kawkaba 2, Operation Accountability 7, Operation Grapes of Wrath 10, Operation Orech Ruach 32, Rayhan 5, Rotem 5, Shani 2, Shreife 8, Sujud 3, Tayibe 3 (104 files). One is labelled public domain in the API: a photograph of a mountain peak dated 06/01/2014, not an outpost and not of the era. None is public domain or CC0 for an outpost of 1977-93. One free file of an outpost of another date was opened, [V, agent read 2026-10-27; a person's check pending]: `File:Israel outpost.JPG` ("My own work, taken in August 2007", "A picture from the Lebanese-side of the Israeli-Lebanon border. This photo shows in the distance an Israeli army outpost."; author Eternalsleeper at English Wikipedia; templates PD-user-en and PD-user-at-project: "This work has been released into the public domain by its author ... This applies worldwide."). It is of 2007, after the withdrawal of 2000, so it shows a border outpost and not the security zone; type only. Free photograph of a security-zone outpost of the era: **[GAP]**.

## 6. The caravan sites of 1990-91 (design 7.5)

The 1978-93 settlements' own caravan years are in [binyamin-localities-1978-93.md](binyamin-localities-1978-93.md), [golan-localities-1977-93.md](golan-localities-1977-93.md) and [localities-1977-93-pre67.md](localities-1977-93-pre67.md) and are not repeated. This section covers the 1990-91 imports for the immigration wave, which the design's cue names.

| Event date | Claim | Status and source |
|---|---|---|
| 1 Jul 1990 | The Cabinet "gave Housing Minister Ariel Sharon emergency powers Sunday to immediately import 3,000 prefabricated houses"; "The houses will be installed at seven separate locations around the country." | [S] JTA, bulletin of 2 Jul 1990, https://www.jta.org/archive/sharon-given-emergency-powers-to-import-3000-prefab-houses. OCR dateline "JERUSALEM, July 1 (JTA)", a Sunday (computed here) |
| 12 Aug 1990 | "The first imported mobile homes authorized by the government will arrive within two months ... The first shipment of 1,000 units will be followed by another 4,000 by the end of the year." | [S] JTA, bulletin of 14 Aug 1990, https://www.jta.org/archive/first-imported-mobile-homes-to-arrive-within-2-months-sharon-tells-cabinet. OCR dateline "JERUSALEM, Aug 13 (JTA)"; "Sunday" is 12 Aug 1990 (computed here) |
| 23 Dec 1990 | "A container ship arrived at the port of Ashdod on Sunday with 441 American-built mobile homes, the first consignment of temporary housing to reach Israel from the United States. Each unit is 40 feet long and contains 144 square feet of living space." | [S] JTA, bulletin of 24 Dec 1990, https://www.jta.org/archive/jews-fleeing-chaos-in-soviet-union-arriving-in-israel-by-the-thousands. OCR dateline "TEL AVIV, Dec. 23 (JTA)"; 23 Dec 1990 was a Sunday (computed here). The item adds "The first shipment was three days late." and that the vessel was towed after an engine breakdown near Crete |
| to 31 Dec 1990 | "The government's program included plans for 49,500 temporary housing units for 1990 and 1991 ... By the end of December, only 680 mobile homes and 110 prefabricated ones had been set up." | [S] JTA, bulletin of 25 Jan 1991, https://www.jta.org/archive/serious-housing-shortage-expected-in-israel-because-of-building-lag. OCR dateline "JERUSALEM, Jan. 24 (JTA)", a Finance Ministry report "earlier this month" |
| May 1991 | "The Housing Ministry has imported several thousand mobile homes ..., but only a few of those located within Israel proper have been hooked up to water, electricity and sewage." | [S] JTA, bulletin of 17 May 1991, https://www.jta.org/archive/with-subsidies-over-and-jobs-scarce-soviet-olim-are-suddenly-in-a-pinch; dateline not read |

**[C]** on the number of units in the first shipment: 1,000 (the 14 Aug 1990 forecast) against 441 (the 23 Dec 1990 arrival, "the first consignment of temporary housing to reach Israel from the United States"). The forecast and the arrival are different statements and neither item reconciles them; both are kept.

Which sites held the 1990-91 caravans, and a date for each site: **[GAP]**. Only the count of seven sites for the 3,000 prefabricated houses (1 Jul 1990) and the later general statements were read.

Photographs: **[GAP]** for 1990-91. Found and excluded: the PikiWiki photographs of caravan neighbourhood setups of 1980 and 1982 (`File:PikiWiki Israel 1805 ...`, "1982-11"; the 1794 to 1798 series, "1982-06"; 1803, "1980-04") print CC BY 2.5 in the API field, pages not opened, and are earlier than 1990 in any case. No file in `Category:Caravans in Israel` (16 files, listed by the API) is a public-domain or CC0 photograph of the period: the two public-domain ones are of 1938 and 1943.

(second pass, 27 Oct) **Where the units went: narrowed, not closed.** The site counts of the first pass ("seven separate locations", 1 Jul 1990) stay as read. The items below name places. They separate two kinds of unit (prefabricated houses and mobile homes, the caravans) because the press does:

| Event date | Claim | Status and source |
|---|---|---|
| 11 Jul 1990 | "The Housing Ministry has begun work on the infrastructure for some 1,800 new housing units in Beersheba, Arad, Sderot, Netivot and Ofakim, all communities in the Negev. The plan is to erect prefabricated houses at those sites. The ministry estimates they will be ready in five months." | [S] JTA, bulletin of 12 Jul 1990, https://www.jta.org/archive/behind-the-headlines-tent-cities-spread-through-israel-becoming-more-than-a-place-to-live. OCR dateline "JERUSALEM, July 11 (JTA)"; PDF http://pdfs.jta.org/1990/1990-07-12_129.pdf. Prefabricated houses, not mobile homes |
| 25 Oct 1990 (Sharon's speech) | "The government is also ordering 15,000 prefabricated housing units, to be set up at 14 sites around the country, and 5,000 mobile homes."; "The Jewish Agency leaders inspected four prefab units in Kiryat Gat, a development town in the northern Negev. The four units, the first of 450 that will be built in the town, were put in place only the day before." "The first several hundred mobile homes have been set up in the last week or so." | [S] JTA, bulletin of 29 Oct 1990, https://www.jta.org/archive/labor-problem-wont-slow-building-of-immigrant-housing-sharon-vows. OCR dateline "ASHKELON, Israel, Oct. 28 (JTA)"; the item says Sharon spoke "last Thursday", which is 25 Oct (28 Oct 1990 was a Sunday, computed here); the day of the Kiryat Gat tour is not printed. PDF http://pdfs.jta.org/1990/1990-10-29_201.pdf. The 14 sites are not named; the 15,000 (Oct) and the 3,000 on seven sites (Jul) are different programme sizes, not a conflict (our reading) |
| report of 27 May 1991 | Of the Ethiopian immigrants: "About 3,200 olim were directed to the south, some to Ashkelon and Eilat, most to mobile homes set up by the Jewish National Fund at various sites in the Negev, including kibbutzim and moshavim." | [S] JTA, bulletin of 28 May 1991, https://www.jta.org/archive/now-that-the-eagle-has-landed-israel-must-absorb-the-ethiopians. OCR dateline "JERUSALEM, May 27 (JTA)"; PDF http://pdfs.jta.org/1991/1991-05-28_100.pdf. The sites are not named |
| 16 Jul 1991 | A group of Ethiopian immigrants "staged a protest march Tuesday demanding to be returned to the mobile homes near the Negev town of Netivot where they were originally quartered." | [S] JTA, bulletin of 18 Jul 1991, https://www.jta.org/archive/group-of-ethiopian-immigrants-protest-lodgings-at-kibbutz-hotel. OCR dateline "JERUSALEM, July 17 (JTA)"; "Tuesday" is 16 Jul 1991 (computed here). PDF http://pdfs.jta.org/1991/1991-07-18_136.pdf |
| report of 28 Aug 1991 | Ethiopians "will be resettled in mobile homes around the country, provided by the Jewish Agency and the Absorption Ministry in cooperation with the Amidar Housing Co."; "They will be invited in the next few days to inspect mobile home sites in Beersheba, Hatzor and Acre and make their choices." | [S] JTA, bulletin of 29 Aug 1991, https://www.jta.org/archive/following-absorption-center-fracas-center-closed-olim-to-be-separated. OCR dateline "JERUSALEM, Aug. 28 (JTA)"; PDF http://pdfs.jta.org/1991/1991-08-29_166.pdf. A plan, not an opening |

So the press names, for 1990-91: five Negev towns for 1,800 prefabricated houses (Jul 1990), Kiryat Gat for 450 prefabricated houses (Oct 1990), JNF mobile-home sites in the Negev and near Netivot for the Ethiopian arrivals (May to Jul 1991), and Beersheba, Hatzor and Acre as mobile-home sites to be inspected (Aug 1991). Not found: the names behind "seven" and "14" sites; a dated opening or occupancy for any single caravan site. Those stay **[GAP]**. The State Comptroller's audit lead that names Even Yehuda, Kfar Yona and Kadima is in [caravan-sites-1990-91-audit.md](caravan-sites-1990-91-audit.md) and is not re-read here. Items on mobile homes in the West Bank (for example the bulletins of 17 Apr and 6 Aug 1991) turned up in the same searches; they belong to the settlement notes and their datelines were not read.

(second pass, 27 Oct) Photographs of 1990-91 caravans: still **[GAP]**. Commons `filetype:bitmap Israel immigrants Soviet 1990` returned 4 files, `filetype:bitmap mobile home Israel immigrants` 0, `filetype:bitmap Israel tent city 1990` 0, `Israel caravans immigrants 1991 Soviet Jews` 0; none free.

## 7. The sealed room and the gas-mask kit (design 7.5: "Gulf War UI cues")

Dates of the mask decision and distribution are in [model-basis-1977-93.md](model-basis-1977-93.md) section 2.4 ([S] JTA bulletins of 6, 10 and 23 Aug and 2 Oct 1990) and the missile attacks from 17 Jan 1991 in [events-1987-93.md](events-1987-93.md) A8. This section adds the look: what was handed out and how a room was sealed.

| Event date | Claim | Status and source |
|---|---|---|
| 7 Oct 1990 | A test of the distribution: "Israel tested its gas mask distribution system Sunday in three small urban centers"; "Gas masks and poison gas antidote kits will be provided for Israel's 4.5 million inhabitants"; "the largest allocation of gas masks undertaken in any country since the end of World War II"; "about 25,000 specially designed gas masks have been ordered ... for use by ultra-Orthodox men who refuse to shave their beards" | [S] JTA, bulletin of 9 Oct 1990, https://www.jta.org/archive/gas-mask-distribution-begins. OCR dateline "TEL AVIV, Oct. 8 (JTA)"; "Sunday" is 7 Oct 1990 (computed here). PDF http://pdfs.jta.org/1990/1990-10-09_188.pdf. The item names the test towns Yokne'am, Ofakim and Kfar Yona |
| 15 Oct 1990 | "The distribution of gas masks to the Israeli public began in Tel Aviv and other large cities Monday afternoon"; "the nationwide distribution of gas masks and poison gas antidote kits ... will take six to eight weeks" | [S] JTA, bulletin of 16 Oct 1990, https://www.jta.org/archive/gas-masks-distributed-in-large-cities; dateline not read; "Monday" before 16 Oct 1990 (a Tuesday, computed here) is 15 Oct. This is the first day of the public distribution that any item read gives. [C] against [model-basis-1977-93.md](model-basis-1977-93.md), which quotes an earlier test in Shlomi and Ramat Hasharon and gives the public collection "from about 22 October" (computed there); the 7 Oct row above is also a test in three towns, not the public distribution |
| to 9 Jan 1991 | "About 80 percent of the population, which lives in densely populated urban and suburban regions, received their gas masks and poison gas antidote kits in October and November" | [S] JTA, bulletin of 9 Jan 1991, https://www.jta.org/archive/visitors-pour-out-of-israel-fearing-suspended-air-travel; dateline not read |
| Jan 1991 | The IDF's instructions: "seal windows with plastic strips and nylon sheets"; one room set aside "and stocked it with sweets, toys and a transistor radio" | [S] JTA, bulletin of 14 Jan 1991, https://www.jta.org/archive/supermarket-lines-are-a-bit-longer-but-israeli-spirits-are-mainly-high; dateline not read. The quote is a resident's account in the item |
| 11 Feb 1991 | "The IDF has broadcast detailed instructions on how to make rooms gas-proof."; "The military has faith in plastic sheeting and masking tape" | [S] JTA, bulletin of 12 Feb 1991, https://www.jta.org/archive/israelis-questioning-civil-defense-on-best-policy-of-scud-protection; dateline not read; the item says "Monday" for the row over shelters (11 Feb 1991, computed here) |
| 14 Feb 1991 bulletin | "Confined to a room sealed with tape and plastic sheets against poison gas, yet still wearing gas masks, thousands of Israelis ... depend on their ears" | [S] JTA, bulletin of 14 Feb 1991, https://www.jta.org/archive/israelis-becoming-sound-experts-identifying-scuds-by-their-whoosh; dateline not read |
| Jan 1991 (bulletin of 21 Jan) | A resident: "she just had time to take her three children into the safety of the gas-sealed room in the middle of the building"; some new immigrants "panicked and prematurely injected themselves with atrophine [sic], an antidote to nerve gas" | [S] JTA, bulletin of 21 Jan 1991, https://www.jta.org/archive/israelis-enjoy-a-day-of-calm-after-2-days-of-missile-attacks; dateline not read |

What the kit held: JTA names "masks and poison gas antidote kits" and the nerve-gas antidote (atropine, spelt "atrophine" in the item). A list of the kit's contents, the mask's make and the sealed-room materials beyond "plastic strips and nylon sheets" and "plastic sheeting and masking tape": **[GAP]** (no civil-defence publication was reached).

Photographs: **[GAP]** (free). Found and excluded, all with the licence in the API field and the pages not opened: the Government Press Office photographs `File:Flickr - Government Press Office (GPO) - An Israeli family in its sealed room.jpg` and `...Family in the sealed room.jpg` ("1991-01-18"), CC BY-SA 3.0; the Bamahane "Israel in the Gulf War" files, CC BY-SA 3.0; `File:Gulfwar 1991 in Israeli shelter.jpg`, CC BY 2.0. None is public domain or CC0.

(second pass, 27 Oct) **What the kit held: narrowed.** All [S] JTA:

| Event date | Claim | Status and source |
|---|---|---|
| 9 Aug 1990 | "The kits contain disposable syringes with atropine, an antidote against nerve gas." The civil-defence organizations "would not complete the distribution of gas masks and anti-chemical warfare kits until the end of the year." | [S] JTA, bulletin of 10 Aug 1990, https://www.jta.org/archive/idf-will-not-distribute-gas-masks-now-for-fear-of-provoking-hostile-action. The OCR datelines of that bulletin's items read "Aug. 9 (JTA)"; PDF http://pdfs.jta.org/1990/1990-08-10_150.pdf. This is the first item read that says what was in the kit |
| 12 Aug 1990 | "The gas masks retail for $150 to $300, including kits that contain a disposable syringe with atropine, a poison gas antidote." (masks sold on the market, our reading; not the state's issue) | [S] JTA, bulletin of 13 Aug 1990, https://www.jta.org/archive/iraq-not-capable-now-of-launching-a-poison-gas-attack-against-israel. OCR dateline "JERUSALEM, Aug. 12 (JTA)"; PDF http://pdfs.jta.org/1990/1990-08-13_151.pdf |
| 17 Mar 1991 | Ha'aretz, as relayed: "the masks supplied to the civilian population had a protective factor of 100, compared with a protective factor of 1,000 in the masks supplied to the IDF and those manufactured in the United States"; the Defense Ministry "admitted that gas masks manufactured between 1972 and 1986 offered "reasonable" protection if the user remained inside a gas-proof room and was equipped with atropine". The State Comptroller's claim, as relayed, that "about 1.4 million gas masks distributed to the public were unfit for use": "Arens and the Israel Defense Force chief of staff, Gen. Dan Shomron, flatly denied the charge Sunday at the weekly Cabinet meeting"; the Defense Ministry said "the 1.4 million figure was inaccurate, but conceded that as many as 860,000 gas masks could have been ineffective" | [S] JTA, bulletin of 18 Mar 1991, https://www.jta.org/archive/many-gas-masks-issued-to-israelis-were-ineffective-newspaper-charges. OCR dateline "JERUSALEM, March 17 (JTA)"; PDF http://pdfs.jta.org/1991/1991-03-18_053.pdf. The figures are claims and counter-claims as reported, not verified counts |

The kit's other contents, the mask's make and model, and sealed-room materials beyond the two phrases already quoted: still **[GAP]**. En.wiki "Merkhav Mugan" (oldid 1369253566), "Safe room" (oldid 1378289371) and "Home Front Command" (oldid 1373573164) were read for plastic sheeting, tape and 1991: no line matched. A first-pass sentence says no civil-defence publication was reached; that is unchanged.

(second pass, 27 Oct) **Photographs, pages opened on 27 Oct, [V, agent read 2026-10-27; a person's check pending].** `File:Flickr - Government Press Office (GPO) - An Israeli family in its sealed room.jpg`: "An Israeli family in its sealed room ( Merkhav Mugan ), with gas masks , during an alarm. Gulf War." (spacing note 2026-10-27: the extra spaces came from stripped link tags; the page's own wording is "(Merkhav Mugan), with gas masks, during an alarm") with "Photograph: ALPERT NATHAN, GPO. 18/01/1991", "Date 18 January 1991, 18:04:07", the National Photo Collection ID D343-067, and the template Cc-by-sa-3.0 (attribution "Government Press Office (Israel)"). `File:Gulfwar 1991 in Israeli shelter.jpg`: "Israeli civilians taking shelter from missiles", "Taken in 1991", Flickr, author Jane Fresco, template Cc-by-2.0. Neither is public domain or CC0; the Hebrew term the page uses for the sealed room, "Merkhav Mugan", is transliterated by the page. Commons `filetype:bitmap gas mask Israel` returned 50 files, 10 labelled free in the API, none an Israeli photograph of 1990-91 (they date from 1939, 1948-56, 2011, 2012 and 2016) (added 2026-10-27: the 10 free files include two dated 4 Apr 1941, British Army gas mask drill on the beach of Tel Aviv, and two of 21 Oct 1939; none is of 1977-93); `filetype:bitmap Israel sealed room` returned 50, 12 labelled free, none relevant (Jerusalem views of 1898 to 1946 and 2008); `filetype:bitmap Israel Scud 1991` returned 16, 6 labelled CC0, all pages of one 1985 Pioneer UAV document. Free photograph of a sealed room or the kit: **[GAP]**.

## 8. Period photographs: free licence, as the page prints it

Opened on 26 Oct 2026, file by file. Every row is [V, agent read 2026-10-26; a person's check pending] for what the page prints. Template names are from the Commons API template list of the page.

| Item | Commons file page | Date and author as printed | Licence template as printed | US status line |
|---|---|---|---|---|
| 1. Merkava Mark 1 | https://commons.wikimedia.org/wiki/File:IDF_Merkava_Mk_I_in_1979.jpg | "9 September 1979"; "Unknown author"; source printed as a PDF on `emu.usahec.org` (US Army Heritage and Education Center) | PD-USGov (also Cc-pd-mark-footer, the Public Domain Mark) | "This work is in the public domain in the United States because it is a work prepared by an officer or employee of the United States Federal Government as part of that person's official duties" |
| 2. Israeli jeep burning, southern Lebanon (a vehicle, not an outpost) | https://commons.wikimedia.org/wiki/File:Angry_Shias_burn_an_Israeli_jeep.png | "Published October 1, 1985"; Central Intelligence Agency; source printed as a CIA assessment of Oct 1985 on `cia.gov` | PD-USGov-CIA (also PD-CIA) | "As a Work of the United States Government, this image or media is in the public domain in the United States." |
| 3. F-15D, type only | https://commons.wikimedia.org/wiki/File:IAF_F-15D_At_Lajes.jpg | "27 June 2004"; SSGT Michelle Michaud, USAF | PD-USGov-Military-Air Force | "As a work of the U.S. federal government, the image or file is in the public domain in the United States." |
| 4. F-15A, type only | https://commons.wikimedia.org/wiki/File:Israeli_F-15A_takes_off_from_Ovda_in_November_2013.jpg | "26 November 2013"; Master Sgt. Lee Osberry, USAF | PD-USGov-Military-Air Force | "As a work of the U.S. federal government, the image or file is in the public domain in the United States." |
| 5. F-16, type only | https://commons.wikimedia.org/wiki/File:Israeli_F-16s_at_Red_Flag.jpg | "17 July 2009"; Master Sergeant Kevin J. Gruenwald, USAF | PD-USGov-Military-Air Force | "As a work of the U.S. federal government, the image or file is in the public domain in the United States." |

Cautions: row 1 prints "Unknown author" and the US-government basis is the uploader's, not stated by an author on the page; the page's description says a message from an Israeli brigadier general is written on the photograph, so who took it is not shown, and the source PDF could not be reached from this box. Treat row 1 as free only after someone opens the source. Row 2 needs the same caution: its photographer is unknown and the page dates the scene only "in the 1980s" (published in the CIA assessment of 1 October 1985), so treat it as free only after someone opens the source PDF. Rows 3 to 5 are not photographs of the era; they are listed so the art lane knows what free US Air Force images of the types exist.

## Search log

Every row: the tool, the exact query, the range, the count and the day run (26 Oct 2026). JTA queries are `https://www.jta.org/wp-json/wp/v2/archive?search=<query>&after=<from>T00:00:00&before=<to>T23:59:59&per_page=100&orderby=date&order=asc`; "returned" is the count of items the API returned (equal to its `x-wp-total` header unless stated); "regex matched" counts items whose title or full text matched the regex, case-insensitive. Only the items named in the sections above were read in full.

| Query | Range | Returned | Regex matched / read |
|---|---|---|---|
| `sealed room` | 1990-08-01 to 1991-03-31 | 50 | `sealed room`: 14 matched; the 14 snippets read, 6 of them quoted above or in section 7's exclusions |
| `atropine kit gas mask` | 1990-08-01 to 1991-01-31 | 3 | all 3 matched `sheeting\|atropine\|antidote\|sealed\|gas-proof\|gas proof\|kit`; read as snippets |
| `gas mask kit distribution` | 1990-08-01 to 1991-01-31 | 30 | same regex: 14 matched (one is an unrelated "kit" item); the 14 read as snippets |
| `plastic sheeting tape room gas` | 1990-08-01 to 1991-01-31 | 1 | 0 matched |
| `gas mask` and `gas masks` | 1990-12-01 to 1991-02-28 | 100 of 167 (`x-wp-total` 167; the API page cap is 100, so only the first 100 by date were examined; read to 24 Jan 1991, later items not read; same for both queries) | regex `adhesive tape\|plastic sheet\|plastic\|masking tape\|wet towel\|cardboard box\|filter`: 2 matched (one unrelated, "plastics manufacturer") |
| `civil defense instructions` | 1990-12-01 to 1991-02-28 | 9 | regex as above: 3 matched; read |
| `M-113` and `M113` | 1977-01-01 to 1993-12-31 | 49 and 47 | regex `M-?113`: 0 matched in either (the item that prints "M-13" was found by the next query) |
| `armored personnel carrier` | 1977-01-01 to 1993-12-31 | 66 | regex `armou?red personnel carrier\|APC`: 62 matched (loose: "APC" matches inside other words); the 12 earliest (5 May 1977 to 26 Jul 1982) read as snippets; one prints "M-13", none prints "M113" |
| `armored personnel carriers Israel army` | 1982-06-01 to 1982-07-31 | 3 | 3 matched; read as snippets |
| `caravans` | 1990-01-01 to 1991-12-31 | 19 | regex `caravan`: 3 matched, all read: a convoy in Egypt, Petra's trade and a Hebrew proverb; none about housing |
| `mobile homes` | 1990-01-01 to 1991-12-31 | 83 | regex `mobile home\|trailer\|prefab\|caravil\|caravan`: 44 matched; the 11 earliest (18 Apr to 29 Oct 1990) read as snippets |
| `prefabricated housing immigrants` | 1990-01-01 to 1991-12-31 | 97 | same regex: 30 matched; the 10 earliest (1 Mar to 18 Jul 1990) read as snippets |
| `trailers immigrants housing` | 1990-01-01 to 1991-12-31 | 7 | 0 matched |
| `first mobile homes arrive Israel` | 1990-09-01 to 1991-12-31 | 14 | regex `mobile home`: 9 matched; read as snippets, 5 quoted above |
| `security zone outposts Lebanon` | 1985-01-01 to 1993-12-31 | 32 | regex `outpost`: 22 matched; all 22 (4 Mar 1985 to 18 Aug 1993) read as snippets, 6 quoted above |

Commons API searches (`https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=<query>&gsrnamespace=6`, files only; "free" means the API's `LicenseShortName` contains public domain or CC0; each free hit that is used above was then opened):

| Query | Returned | Free hits |
|---|---|---|
| `Israel sheqel banknote 1980` | 2 | 0 (BoI template) |
| `Israel Sheqalim banknote` | 19 | 3 ("Public domain": two files of a 1968 5 sheqalim note, outside 1980, and the 10000 NIS file opened in section 1); all others BoI template or CC BY-SA |
| `Merkava Mark 1` | 25 | 3 (the 1979 file; a CC0 map of operators; a Philippine Army file of a later vehicle) |
| `M113 Israel` | 25 | 1 PD (Suez 1973, opened, excluded) |
| `Israeli tank Lebanon 1982` | 50 | 5 PD-labelled (four Battle of Sultan Yacoub files, 1982, "Unknown author", and a US Navy Sherman of 1984); one of the four opened and prints PD-Syria with no US basis, so none is listed |
| `Israeli Air Force F-16` / `F-15 Israeli Air Force` (several variants, also category listings) | 50 per query | the three US Air Force type-only photographs in section 8 |
| `Israel gas mask 1991`, `Gulf War Israel sealed room` | 15 and 5 | 0 free (GPO and Bamahane files are CC BY-SA) |
| `South Lebanon security zone`, `Israeli army outpost Lebanon`, `Beaufort Lebanon IDF` | 50, 50, 18 | 1 PD (`File:Israel outpost.JPG`, "12 October 2007", not of the era; not opened) |
| `caravan Israel 1990`, `caravans immigrants Israel`, `mobile homes immigrants Israel 1991` | 0, 50, 10 | 0 |
| Categories `Caravans in Israel`, `Israel Defense Forces in Southern Lebanon`, `South Lebanon conflict (1985-2000)`, `Israeli occupation of Southern Lebanon`, `Good Fence`, `F-15D Baz`, `F-15 Eagle of the Israeli Air Force in flight` | 16, 76, 9, 26, 9, 15, 117 files | see sections 4 to 6 |

## Second-pass search log (27 Oct 2026)

Same conventions as the log above. JTA queries use the same API URL pattern as above (`search=<query>`, `after`, `before`, `per_page=100`), run 27 Oct 2026 with a script that prints each returned item's regex hits. Commons queries use `https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=<query>&gsrnamespace=6&gsrlimit=50` with `prop=imageinfo` and the `LicenseShortName` field; "free" means that field contains public domain or CC0. Regexes are written with every pipe escaped.

| Query | Range | Returned | Regex matched / read |
|---|---|---|---|
| `mobile homes` | 1990-07-01 to 1991-12-31 | 77 (`x-wp-total` 77) | `kiryat\|caravan site\|site(s) (for\|of)\|located at\|will be (placed\|set up\|installed)`: 9 matched, all 9 read as snippets |
| `prefabricated` | 1990-06-01 to 1991-12-31 | 100 of 477 (read to 30 Aug 1990; later items not read) | `(prefab\|mobile home\|caravilla)` followed within 160 characters by "in", "at", "near" or "outside" and a capitalised word: 10 matched; the 10 read (25 Jun to 27 Aug 1990) |
| `prefab` | 1990-06-01 to 1991-12-31 | 100 of 490 (read to 30 Aug 1990; later items not read) | same regex: 10 matched, the same 10 |
| `caravilla` | 1990-06-01 to 1991-12-31 | 0 | 0 |
| `mobile home` | 1990-06-01 to 1991-12-31 | 79 | same regex; the listing was read as printed up to the item of 3 Sep 1991 and cut there, so matches after that date were not read; 4 quoted above |
| `Merkava` | 1979-01-01 to 1980-12-31 | 7 | regex `merkava`: 5 matched, all read as snippets, 2 quoted above (12 Dec 1979, 22 Feb 1980) |
| `Merkava` | 1988-01-01 to 1993-12-31 | 32 | `merkava`: 8 matched; titles and dates read; none states the mark except the next row |
| `Merkava III` | 1985-01-01 to 1993-12-31 | 5 (`x-wp-total` 5) | `merkava (III\|3\|Mark)\|Mark (III\|3)`: 1 matched (4 Sep 1990, quoted above) |
| `Merkava` | 1989-09-01 to 1990-03-31 | 5 | `merkava`: 2 matched (29 and 30 Jan 1990, land mines) |
| `F-16` | 1980-06-01 to 1980-09-30 | 3 | `F-?16`: 2 matched |
| `F-16` | 1980-01-01 to 1980-12-31 | 15 | F-16 within 200 characters of arriv/land/delivered/received: 1 matched (26 Nov 1980) |
| `F-16 jets arrive` | 1980-03-01 to 1980-08-31 | 0 | 0 |
| `atropine` | 1990-08-01 to 1991-03-31 | 4 (`x-wp-total` 4) | `syringe\|injector\|injection\|inject\|powder\|pill\|ampule\|tablet\|kit (contain\|includ)\|kit,? which\|decontaminat`: 4 matched, all read |
| `syringe` | 1990-08-01 to 1991-03-31 | 100 of 409 (read to 24 Sep 1990; later items not read) | same regex: 3 matched: 10 Aug and 13 Aug 1990, and the unrelated "injecting the Lavi" item of 9 Aug 1990 (the pattern matches "inject") |
| `gas mask` | 1990-08-01 to 1991-03-31 | 100 of 387 (read to 1 Oct 1990; later items not read) | same regex: 3 matched, the same items |
| `antidote` | 1990-08-01 to 1991-03-31 | 12 (`x-wp-total` 12) | same regex: 4 matched (10 Aug 1990, 13 Aug 1990, 21 Jan 1991, 18 Mar 1991) |
| `outpost` | 1985-01-01 to 1993-12-31 | 100 of 226 (read to 15 Dec 1988; later items not read) | form-word regex (see section 5 update): 1 matched |
| `fortified` | 1985-01-01 to 1993-12-31 | 100 of 373 (read to 22 Sep 1986; later items not read) | same: 2 matched |
| `bunker` | 1985-01-01 to 1993-12-31 | 100 of 916 (read to 21 Apr 1986; later items not read) | same: 0 matched |

Other searches, 27 Oct 2026:

- FRUS TEI files from `https://raw.githubusercontent.com/HistoryAtState/frus/6170e4b32c1224002db2fe0c5d8d910597cddc2e/volumes/`: `frus1977-80v08.xml` (7.4 MB) searched with markup stripped for "M-113", "M–113" and "armored personnel": the type is in document 43 (section 3 above); `frus1977-80v09.xml` at that path is a 12 KB file, not the volume, and was not searched; `frus1969-76v23.xml` as stated in section 3.
- English Wikipedia API, by revision: "Merkava" (oldid 1378577172, one line gives the Mark 3 as "introduced in December 1989", the claim the first pass already had), "General Dynamics F-16 Fighting Falcon" (oldid 1382023348), "Israeli Air Force" (oldid 1380532238, no Israeli F-16 delivery date found by the pattern used), "Merkhav Mugan", "Safe room", "Home Front Command" and "South Lebanon conflict (1985-2000)", oldids as given above. A search for the sealed-room terms returned 0 titles.
- `https://www.cia.gov/readingroom/docs/CIA-RDP86T00587R000400440002-6.pdf`, the source printed on the Commons page of the burning-jeep photograph: the request returned an HTML page and not a PDF (the bytes begin `<!DOCTYPE html>`), so the source was not opened; section 8 row 2's caution stands.
- Wayback copies of that CIA address and of the usahec address timed out three times each.
- Commons category listing through the API, 16 subcategories (section 5 update).

## Sources opened

| Id | Source and what it was used for | URL | Opened |
|---|---|---|---|
| BoI-SH | Bank of Israel, "Sheqel Series" (Wayback copy of 12 Jun 2026) | https://web.archive.org/web/20260612170121/https://www.boi.org.il/en/economic-roles/banknotes-and-coins/sheqel-series/ | 26 Oct 2026 |
| BoI-NIS | Bank of Israel, "First Series of the New Sheqel" (Wayback copy of 2 Sep 2026; page last updated 21/05/2023 as printed) | https://web.archive.org/web/20260902090932/https://boi.org.il/en/economic-roles/banknotes-and-coins/first-series-of-the-new-sheqel/ | 26 Oct 2026 |
| JTA | Jewish Telegraphic Agency archive items and their bulletin PDFs, named in the tables above | https://www.jta.org/archive/ and http://pdfs.jta.org/ | 26 Oct 2026 |
| WP-M113 | English Wikipedia, "M113 armored personnel carrier", oldid 1380603322 (the revision the API returned) | https://en.wikipedia.org/w/index.php?title=M113_armored_personnel_carrier&oldid=1380603322 | 26 Oct 2026 |
| Commons | The file pages named in section 8 and in sections 1, 3 and 5 as opened | https://commons.wikimedia.org/ | 26 Oct 2026 |
| JTA-2 | Second pass: the JTA items and bulletin PDFs named in the second-pass tables (12 Dec 1979, 22 Feb 1980, 4 Sep 1990, 12 Jul 1990, 29 Oct 1990, 28 May 1991, 18 Jul 1991, 29 Aug 1991, 10 Aug 1990, 13 Aug 1990, 18 Mar 1991) | https://www.jta.org/archive/ and http://pdfs.jta.org/ | 27 Oct 2026 |
| FRUS-v8 | FRUS 1977-80 volume 8, document 43 (TEI file); FRUS 1969-76 volume 23 (searched) | https://raw.githubusercontent.com/HistoryAtState/frus/6170e4b32c1224002db2fe0c5d8d910597cddc2e/volumes/frus1977-80v08.xml | 27 Oct 2026 |
| Commons-2 | The file pages named in the second-pass paragraphs of sections 1, 2, 3, 5 and 7, and the 16 category listings of section 5 | https://commons.wikimedia.org/ | 27 Oct 2026 |
