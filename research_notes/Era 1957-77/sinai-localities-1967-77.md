# Israeli localities and Nahal outposts founded in Sinai and the Rafah approaches, June 1967 to 1977 (research note, 2026-10-25)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file (#61)
stays paused until after the MVP. Refs #130 (1957-77 18), the "Settlements" row of [the 1957-1977
design](../../docs/superpowers/specs/2026-10-10-era-1957-1977-design.md) section 7.3: "settlements in the territories".
One topic of #130 only: the localities and Nahal outposts founded in Sinai, including the Rafah approaches (Hebrew
Pithat Rafiah), between June 1967 and 31 December 1977. **The Golan (PR #1655), the West Bank, the Gaza Strip proper
(note `gaza-localities-1967-77.md`, claimed on #130), the localities inside the pre-1967 lines (PRs #1643, #1646, #1647) and the lines are not in this note.** Which rows
enter the game, and their names, wait for the owner's sensitivity list (#118); this note makes no include or omit
choice, no naming choice and no statement on the status of any place beyond what a source says, quoted and tagged.

It follows the format of [`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) and the conventions of
the sibling notes in this folder. Every page below was opened on **25 October 2026** (UTC) unless a row says otherwise.
The evacuation of 1982 is not redone here: see [`lines-1977-93.md`](../Era%201977-93/lines-1977-93.md) section 1.4
(Yamit region, April 1982) and [`events-1977-82.md`](../Era%201977-93/events-1977-82.md) B24.

The question: which Israeli localities and Nahal outposts were founded in Sinai between June 1967 and 31 December 1977,
and for each: the founding date (and what it marks), coordinates from a named source, and the sources.

## Short answer

- **The list cannot come from the CBS 2024 localities file.** These places were evacuated in 1982 and the file has no
    row for most of them. It does have seven rows with the same names as Sinai places (Avshalom, Holit, Netiv HaAsara,
  Sufa, Peri Gan, Talme Yosef, Nitzanei Sinai), but those are the later localities that took the names inside the
  pre-1967 lines (CBS years 1990, 1980, 1982, 1975, 1981, 1982, 1986; section "How it was done", "The CBS file"). So membership here is built from Haim
  Gvati's *100 Years of Settlement* (his narrative on the Rafah approaches and the Gulf coast, and his appendix of
  rural settlements) and cross-checked against Hebrew and English Wikipedia and the JTA Daily News Bulletin.
- **20 rows.** 14 in North Sinai and the Rafah approaches (Nahal Yam, an outpost that never became a locality; the 11
  rural localities of Gvati's count; Yamit; and the regional centre Avshalom) and 6 on the Gulf coast and elsewhere
  (Ofira, Neviot, Di Zahav, Zaharon, Shalhevet, Kadesh Barnea). Gvati's own count for the Rafah approaches in 1977 is "11 ישובים חקלאיים ומרכז
  עירוני אחד" (11 agricultural localities and one urban centre); the 11 names in his narrative are all rows below.
- **11 of the 20 rows carry a disagreement [C]** on the year, the month or on what the date marks (an outpost, a
  civil settlement, a cornerstone, a first attempt). 5 rows agree on the year across the sources read (rows 4, 7,
  12, 13, 17), and for those the day, where there is one, rests on Gvati or on one Wikipedia article. 3 rows rest on
  one Hebrew Wikipedia article only (Zaharon, Shalhevet, Kadesh Barnea) and 1 row has no founding day from any source
  (Ofira, [GAP]).
- **Contemporaneous press was read for 13 of the 20 rows** (JTA Daily News Bulletin; 16 items opened as PDFs, 13 with a
  dated dateline, 2 with a place only, 1 with none (29 Aug 1973); "JTA items and their datelines"). No government record, Knesset record, Official
  Gazette item or Statistical Abstract was opened. Every founding date below is [S], [C] or [GAP]; the only verified items
  are what the CBS 2024 file prints. That is the main [GAP] of the note ("What was not done").
- **Coordinates:** 5 rows have a coordinate from two or three sources that agree to under 2 km (rows 1, 3, 5, 7, 15); 2
  rows have two sources that do not agree (rows 6 and 10); 10 rows have one source only; 3 rows have none ([GAP]: rows
  12, 14, 20). No coordinate comes from a map I opened, and none is a survey of the founding site.

## Conventions and tags

- **[V, agent read 2026-10-25; a person's check pending]**: what the CBS 2024 file prints, read today. It says nothing
  about whether the printed value is right.
- **[S]** secondary: one source, or several that plainly copy one another. Gvati, Wikipedia and the JTA bulletins are
  [S] (JTA is press, not a record). Where Wikipedia cites a newspaper (Davar, Maariv), a book or a government
  publication, I give the citation as Wikipedia gives it and **do not count that source as read**; I opened none of them.
- **[C]** conflicting: sources disagree; every value is given and what each marks, as far as the source says.
- **[GAP]** not closed, with what was tried.
- **Derived** marks a number this note computed; the arithmetic or the tool is named.
- Hebrew quotes from Gvati and Wikipedia are given without vowel signs; translations are mine. English quotes from JTA
  are copied from the archive page text. Each quoted string was checked by script against the page text I fetched.

## How it was done

- **The list.** Gvati, *100 Years of Settlement*, vol. 2, Ben-Yehuda Project edition, https://benyehuda.org/read/14684
  (opened 2026-10-25 08:43 UTC, 1,348,249 bytes, the same size as the copy other lanes opened today). Two parts: the
  chapter "ההתיישבות מעבר ל״קו הירוק״" (sections "צפון סיני", "פיתחת רפיח", "מרחב שלמה") and the appendix table "List
    of all Jewish rural settlements" (869 parsed table rows (2 of them headers) from the 13 HTML tables on the page, parsed by script; columns name, region, date of
  founding, affiliation, origin of the name). The appendix region labels used for Sinai are "פיתחת רפיח" or "פתחת רפיח"
   (Rafah approaches), "צפון סיני" (North Sinai) and "חוף מפרץ אילת" (Gulf of Eilat coast). The appendix has misprints I can see (it prints "פתחת" and "פיתחת" for the same region). Its date is one
  secondary compilation and does not say whether it marks the day on the land or a ceremony.
- **The CBS file.** CBS, localities file 2024, https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx
  (downloaded and opened 2026-10-25 08:53 UTC, 370,654 bytes, SHA-256
    `a021c6a151863ded624d2f674f9bbcf90ba9f3660c8d55aebe6526ff7f5d2ad0`; 1,490 locality rows, 1,490 read). I matched 23
  Hebrew names (rows 1 to 19 as Hebrew Wikipedia spells them, plus Atzmona, Pri Gan, Hatzar Adar and Nahal Sinai)
  against the column "שם יישוב" with spaces and hyphens removed, and then searched the column for "ניצני סיני", "קדש",
  "סיני", "ימית" and "חבל". Seven matched: Avshalom (symbol 1311, year 1990, form 370, Eshkol), Holit (1239, 1980, form
  330), Netiv HaAsara (1242, 1982, form 310, Hof Ashkelon), Sufa (1238, 1975, form 330), Peri Gan (1231, 1981, form 310),
  Talme Yosef (1237, 1982, form 310) and Nitzanei Sinai (1280, 1986, form 310, Ramat Negev). **[V, agent read 2026-10-25; a person's check pending]** as to what the file prints. They are
    rows of the present localities in the Be'er Sheva and Ashkelon subdistricts, not of the Sinai places; only Sufa's year
  (1975) falls in the window (Nitzanei Sinai's 1986 matches Hebrew WP's category for the present moshav), and `kibbutzim-moshavim-1957-77.md` row 40 already records that this year follows the
  outpost in the Rafah approaches. No row has the name Yamit, Sadot, Dikla, Ugda, Ofira, Neviot, Di Zahav or any other
  of the rest, in the name search or in the "סיני" search.
- **Wikipedia** (Hebrew and English) was fetched through the MediaWiki API as wikitext of the current revision at the
  fetch; each revision is named in the last table. It is a lead and a cross-check, always [S]. The coordinates marked
  "he WP" come from the `אורך` and `רוחב` fields of the Hebrew infobox's location-map template; "en WP" from the first
  `coord` template of the English article, converted from degrees, minutes and seconds (my arithmetic: degrees +
  minutes/60 + seconds/3600).
- **GeoNames.** https://download.geonames.org/export/dump/EG.zip (downloaded 2026-10-25, 1,859,093 bytes, 35,027 rows,
  country Egypt). Searched for the Latin and Hebrew names of every row. Found: Yamit (geonameid 449757, class S, code
  RUIN, last modified 2012-01-20), Naẖal Diqla (345953, class P, code PPL, last modified 2020-06-11) and Ẕomet Avshalom
  (8068834, a road junction, last modified 2020-06-12). Nothing for the other names.
- **JTA Daily News Bulletin.** Searches: the archive's wp-json endpoint
  `https://www.jta.org/wp-json/wp/v2/archive?search=<term>&after=1967-06-01T00:00:00&before=1978-06-30T23:59:59&per_page=100&page=1`,
  page 1 only, run 2026-10-25. Every query with its count of results is in "Searches". The search is fuzzy: it returns
  items that do not contain the term, so I kept only returned items whose text contains the term, and for the
  large counts I read only the first 100 returned. For every item cited as dated I opened the bulletin PDF
  (`http://pdfs.jta.org/<year>/<date>_<no>.pdf`), read it as page images through PyMuPDF at 250 dpi and RapidOCR (the
  OCR drops spaces and mangles some words) and read the dateline. **Dates rule:** the archive page shows the bulletin
  date; the event date comes from the dateline in the PDF ("TEL AVIV, Aug. 15 (JTA)") and from "today", "yesterday" or
  "this week" read against it. Where the item has no dateline on the page I say so.
- **Internet Archive and the Official Gazette.** Looked for a primary list (Statistical Abstract of Israel, Israel
  Government Year-Book, Israel Year Book) and for the Official Gazette notice that named the places; see "What was not
  done" for the exact queries and the blocks.
- All requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

## Summary table

Order: North Sinai and the Rafah approaches by Gvati's date, then the Gulf coast and the rest. "Gvati appendix" is the
appendix row as printed (name, region, date), "-" where the appendix has no row; "Gvati text" is the year his
narrative gives. "Coordinates" are decimal degrees, latitude then longitude, with the source and, where two sources
exist, the distance between them (**derived**, haversine, my computation). "Tag" is the note's own tag for the founding
date: [C] sources disagree, [S] they agree on the year (the day, where given, is one source's), [GAP] no date closed.

| # | Locality (as the sources spell it) | Kind | Gvati appendix | Gvati text | Dates in the sources read | Coordinates | Tag |
|---|---|---|---|---|---|---|---|
| 1 | Nahal Yam | Nahal outpost, Bardawil lagoon | - | by end 1967 | 3 Oct 1967 (he WP); "1968" (JTA 1973) | 31.03100, 33.15200 (he WP); 31.03111, 33.15250 (en WP), 0.05 km | [C] |
| 2 | Nahal Sinai, later Neot Sinai | Nahal outpost, then moshav, E of El Arish | "נאות סיני", North Sinai, 1969 | by end 1967 | Dec 1967 outpost; 1969; Jun 1972 civil; Sep 1977 permanent site | 31.15200, 33.85350 (he WP) | [C] |
| 3 | Dikla (Nahal Dikla) | Nahal outpost, then moshav | "דקלה", Rafah approaches, 3.7.69 | late 1960s | 2 Jun 1969 (he WP); 3 Jul 1969 (Gvati); May 1969 (en WP); 17 May 1971 civil | 31.23333, 34.11667 (GeoNames); 31.21911, 34.12420 (en WP), 1.74 km | [C] |
| 4 | Sadot | moshav | "שדות", Rafah approaches, 3.1.71 | 1971 | Jan 1971 (he WP); 3 Jan 1971 (Gvati) | 31.21546, 34.20336 (he WP) | [S] |
| 5 | Yamit | urban centre | - | 1973 (as Merkaz Avshalom) | Feb 1973 decision; Aug 1973 first settlers; Dec 1975 cornerstone; "1975" (he WP infobox); construction began Jan 1975 (en WP) | 31.27544, 34.16790 (GeoNames); 31.27556, 34.16762 (he WP); 31.27556, 34.16722 (en WP), at most 0.07 km | [C] |
| 6 | Avshalom (Merkaz Avshalom) | regional centre | - | 1973 | 1973 (he WP); name fixed 1979 | 31.21546, 34.19268 (he WP; see row note) | [C] |
| 7 | Netiv HaAsara (Sinai) | moshav | "נתיב העשרה", Rafah approaches, 1.7.73 | 1973 | Jul 1973 (he WP); 1 Jul 1973 (Gvati); 1973 as "Minyan" (en WP) | 31.21232, 34.21800 (he WP); 31.21333, 34.21861 (en WP), 0.13 km | [S] |
| 8 | Ugda (Ogda) | moshav | "אוגדה", Rafah approaches, 3.6.75 | 1975 | 3 Jun 1975 (Gvati); Dec 1975 third moshav on the land, 5 families on "31 June" 1975, 3 more end Aug 1975 (he WP); land being prepared 16 Jan 1975 (JTA) | 31.18677, 34.21939 (he WP) | [C] |
| 9 | Sufa (Nahal Sukkot) | kibbutz, from a Nahal outpost | "סופה", Rafah approaches, 1975 | 1974 | 1974 (Gvati text, he WP, en WP); 1975 (appendix); 17 Jan 1977 civil | 31.23639, 34.18944 (en WP) | [C] |
| 10 | Holit (Nahal Holit) | kibbutz, from a Nahal outpost | "נחל חולית", Rafah approaches, 1977 | 1974 | 1974 (Gvati text, inferred from "the same"); Jul 1977 (he WP); 1978 (en WP, Nahal settlement); civilian turnover about 12 Jan 1978 (JTA via #1647, derived) or 17 Jan 1978 (he WP) | 31.22624, 34.16380 (he WP); 31.23000, 34.32667 (en WP), 15.49 km; the en WP point is 0.12 km from the CBS point of present Holit, 31.23068, 34.32573 (derived) | [C] |
| 11 | Haruvit (Tarsag) | Nahal outpost, then moshav | "תרסג (חרובית)", North Sinai, 1976 | 1975 | Dec 1975 outpost; 1976 (appendix); summer 1977 civil | 31.17000, 33.94600 (he WP) | [C] |
| 12 | Nir Avraham | moshav | "ניר אברהם", Rafah approaches, 10.77 | 1977 | 1977 (he WP); Oct 1977 (Gvati) | none: [GAP] | [S] |
| 13 | Talmei Yosef (Sinai) | moshav | - | 1977, in stages | 1977; cornerstone 27 Jul 1977 (he WP) | 31.21056, 34.24278 (en WP) | [S] |
| 14 | Pri'el | moshav | - | 1977, in stages | 1977 (Gvati text); Sep 1978 families in (he WP); early 1978 (en WP) | none: [GAP] | [C] |
| 15 | Ofira (Sharm el-Sheikh) | town | - | no date | 1969 (he WP, en WP); Apr 1971 housing being built; Aug 1971 name published (JTA) | 27.85380, 34.30816 (he WP); 27.86750, 34.29528 (en WP), 1.98 km | [GAP] |
| 16 | Neviot | moshav | "נביעות", Gulf coast, 10.5.71 | 1971 | Feb 1971 first attempt; Aug 1971 second; Jan 1972 cooperative (he WP); 10 May 1971 (Gvati) | 29.03333, 34.66667 (he WP) | [C] |
| 17 | Di Zahav | moshav shitufi | "די-זהב", Gulf coast, 13.9.71 | 1971 | 13 Sep 1971 (he WP and Gvati) | 28.48139, 34.49861 (he WP) | [S] |
| 18 | Zaharon | moshav | - | - | 1977 (he WP) | 28.99856, 34.67622 (he WP) | [S] |
| 19 | Shalhevet | settlement, Gulf of Suez | - | - | 1971-1975 (he WP) | 28.89158, 33.18420 (he WP) | [S] |
| 20 | Kadesh Barnea (Nitzanei Sinai) | Nahal outpost | - | - | 1977 (he WP) | none: [GAP] | [S] |

Rows 18 to 20 and the day of every row marked "he WP" only rest on one source. Rows 12 and 14 have no coordinate in any
source read; rows 15 and 19 have a coordinate but no date from a source that gives a day.

## Row notes

Each row: what the sources say, in the order Gvati, Hebrew Wikipedia, English Wikipedia, JTA, and what each date marks
where a source says. "Bulletin" is the JTA archive date.

1. **Nahal Yam (Nahal outpost on the Bardawil lagoon, North Sinai) [C].** Gvati's text: "בצפון־סיני הוקמו שתי היאחזויות
   של הנח׳׳ל: נח״ל־ים ונח״ל־סיני" and, after describing them, "אלה היו כל ההיאחזויות שהוקמו עד סוף 1967"; of Nahal Yam
   he says it "לא התקיימה אחרי מלחמת יום הכיפורים" (did not exist after the Yom Kippur War). No appendix row. Hebrew WP
   (oldid 44108692): "תאריך הקמתה הרשמי של ההיאחזות הוא ה-3 באוקטובר 1967", on the site of a small railway station;
      civilianised by decision of 16 May 1973; it cites Maariv and a Public Library of US Diplomacy cable (March 1973), not
   opened by me. English WP (oldid 1376881599): "established on 3 October 1967", slated on 9 March 1973 to be abandoned in
   favour of a fishing village. JTA: bulletin 1 Jul 1968, dateline "TEL AVIV, June 30 (JTA)": "a para-military settlement established
   on the northern Sinai coast near the Bardawil lagoon" (no date of founding); bulletin 23 Jul 1969, dateline "TEL AVIV,
   July 22 (JTA)": "a fishing settlement--Israel's first--on a lagoon of the Mediterranean" (Hebrew WP dates the same
   bazooka attack 21 July; I did not resolve the day); bulletin 9 Mar 1973, dateline "JERUSALEM, March 8 (JTA)": "Nahal
   Yam, established in 1968, was one of the first settlements outside Israel's borders after the Six-Day War", to be
   "abandoned in favor of a civilian fishing village". **[C]:** 1967 (Gvati's text, which counts it among the outposts up to the end of 1967; Hebrew WP and English WP
   3 Oct 1967) against "1968" (JTA, 1973, one unattributed clause). The English WP date may follow the Hebrew one; I
   did not establish it.

2. **Nahal Sinai, later Neot Sinai (North Sinai, east of El Arish) [C].** Gvati's text: Nahal Sinai "הוקמה כ־3 ק״מ
   צפונית־מזרחית לאל־עריש, על שטח תחנת־נסיונות מצרית", one of the outposts "עד סוף 1967"; later "היאחזויות הנח״ל שהוקמו
   בסוף שנות השישים: נחל־סיני ונחל דקלה – התאזרחו והפכו למושבים: נאות־סיני ודקלה, משל תנועת ״חירות״". Appendix row
   "נאות סיני", region "צפון סיני", date "1969", Herut moshavim, origin "סמלי". Hebrew WP (oldid 43737905): founded as
   Nahal Sinai "בדצמבר 1967" (cites Maariv, 28 Dec 1967, not opened), civilianised June 1972 with Begin and Sharon at the
   ceremony, new permanent site opened in September 1977 with Begin present. JTA: bulletin 8 Jan 1975, dateline
   "JERUSALEM, Jan. 7 (JTA)": the JNF is preparing the infrastructure of "Nahal Sinai at a site some five kilometers
   east of the Arab city of Al Arish in the Gaza Strip" (as printed; El Arish is in Sinai) for "the new moshav", 500
   dunams; bulletin 17 Jan 1975, dateline "OGDA, Near Rafiah (south of Gaza Strip), Jan. 16": "Nahal Sinai 40 kilometers down the coast near El
   Arish". **[C]:** December 1967 (Hebrew WP) and the end of 1967 (Gvati's text) against 1969 (Gvati's appendix row for the
   moshav name). The 1975 JTA items date the move to a permanent site, not the founding. English WP has no article. Gvati's appendix lists it in its section "מושבים שיתופיים" (moshav shitufi), as it does Dikla and Di Zahav.

3. **Dikla (Nahal Dikla) [C].** Gvati appendix: "דקלה", "פיתחת רפיח", "3.7.69", Herut moshavim, "עפ"י עצי הדקלים באזור".
   Hebrew WP (oldid 43737895): "הוקמה ב-2 ביוני 1969 ... על ידי גרעין של בני עקיבא",    civilianised 17 May 1971; it says the Government Names Committee first called the place "נח"ל דקלים" and
   cites Yalkut Hapirsumim 2633 (10 June 1980) for the naming (that PDF answered me with an HTML page, so I did not open
   it). English WP (oldid 1304841200): "established in May 1969 as a pioneer Nahal outpost known as Dekalim (palms)". JTA, existence only
   (bulletin dates, datelines not read): 28 Jan 1970 ("Several bazooka shells were fired today at Nahal Dikla in the
   northern Sinai"), 18 Feb, 15 Apr 1970; 6 Jan and 11 Mar 1971 ("Nahal Dikla, an Israeli settlement in the northern end
   of the peninsula"); bulletin 9 Oct 1975, dateline "JERUSALEM, Oct. 8 (JTA)", article "JNF tackling new project" (page 2, from the left column into the top of the right): JNF "developed 300 dunams at Dikla in northern Sinai". **[C]** on the month: May (English WP), 2 June (Hebrew WP) and 3 July (Gvati), all 1969. Gvati's appendix lists Dikla in its section "מושבים שיתופיים" (moshav shitufi), as it does Neot Sinai and Di Zahav.

4. **Sadot [S].** Gvati's text: "ב־1971 הוקם המושב הראשון שדות, כשמונה ק״מ דרומית־מערבית לרפיח". Appendix: "שדות",
   "פיתחת רפיח", "3.1.71", Moshavim movement. Hebrew WP (oldid 43738231): the first civilian settlement of the Yamit
   region, "בינואר 1971", first 18 families. JTA, bulletin 17 Jan 1975, dateline "OGDA, Near Rafiah (south of Gaza Strip), Jan. 16":
   "neighboring Sadot did in 1971". Existence by September 1973: bulletins 4 and 19 Sep 1973 name Sadot and Netiv Haasara
   (4 Sep dateline not read; 19 Sep dateline Sept. 18, see the datelines table). The year agrees across three sources; the day (3 January) rests on Gvati.

5. **Yamit [C].** Gvati's text: "ראשיתו כמרכז אזורי שנקרא - מרכז אבשלום, אולם מאוחר יותר הוחלט להקים במקום ישוב עירוני
   גדול", under "ב־1973 הישוב העירוני: ימית". No appendix row (it is not a rural settlement). Hebrew WP (oldid 43845355):
   infobox founding "1975"; text: the ministers' committee decided on the city in February 1973, the Labor Party
   secretariat endorsed the Galili document on 3 September 1973, preliminary works began in 1973, "טקס הנחת אבן הפינה
   ליישוב נערך בדצמבר 1975, כשארבעים המשפחות הראשונות התיישבו במקום"; planned for 250,000 residents, about 2,500 at the
   peak. JTA: bulletin 15 Dec 1972 (dateline not read): Dayan "acknowledged that preliminary plans had been made by the
   Defense Ministry for a new immigrant town in the southern Gaza Strip to be called Yamit"; bulletin 16 Aug 1973,
   dateline "TEL AVIV, Aug. 15 (JTA)": the Public Works Department "started this week construction of the road that will
   lead to the urban center at Pithat Raffiah which Defense Minister Moshe Dayan named 'Yamit.'"; bulletin 29 Aug 1973 (no
   dateline on the item): "A new urban center in the Pithat Rafah region ... got its first settlers this week", Soviet
   immigrants who work in Sadot and "Tel Asarah"; bulletin 17 Jan 1975, dateline Jan. 16: the regional centre "Merkaz
   Avshalom"-"better known as Yamit" is "now rising slowly"; bulletin 2 Jun 1975, dateline "JERUSALEM, June 1 (JTA)":
   the Housing Ministry "today invited the public to register for homes at Yamit"; bulletin 19 Mar 1976, dateline
   "JERUSALEM, March 18 (JTA)": "There are presently 58 families living in Yamit". **[C]:** the Hebrew WP infobox year
   (1975), the construction start of January 1975 (English WP), the 1973 of Gvati and of the first settlers (JTA, week of 29 Aug 1973), and a December 1975 cornerstone. They mark
   different things (decision, first residents, cornerstone), which this note does not rank. Gvati and JTA (Jan 1975)
      identify Yamit with Merkaz Avshalom; Hebrew WP treats Avshalom as a separate row (row 6). English WP (oldid
   1378290812): "Construction of Yamit began in January 1975. When the first fifty residents arrived there were no
   buildings, roads, electricity or water."; it gives no other founding date.

6. **Avshalom (Merkaz Avshalom) [C].** Hebrew WP (oldid 41169976): "אבשלום הוקם ב-1973, דרום מזרחית לימית. תחילה נקרא
   'מרכז שדות', ולאחר מכן 'יד און'"; 1975 rural centre for the neighbouring moshavim; the final name set in 1979 after
   Avshalom Feinberg; permanent settling began in 1979. Gvati and JTA (17 Jan 1975) say "Merkaz Avshalom" is Yamit
   (row 5). **[C]** on whether row 5 and row 6 are one place. Coordinates: the same revision gives two points, 31.21546, 34.19268 in the map template and 31.21232, 34.19268 in an infobox `coord` field (0.35 km apart, derived); the first latitude is the same number as Sadot's in the same fields and the second is the same as Netiv HaAsara's, so either may be a copy; GeoNames has a road junction "Ẕomet Avshalom" at 31.21319,
   34.27031, 7.39 km from the Hebrew WP point (derived). Not resolved.

7. **Netiv HaAsara, Sinai [S].** Gvati's text: "ב־1973 הוקם עוד מושב – נתיב העשרה, לזכר עשרת החיילים שניספו בתאונת
   מסוק צה״ל בחוף הים הסמוך". Appendix: "נתיב העשרה", "פיתחת רפיח", "1.7.73", Moshavim movement. Hebrew WP (oldid
   43738008): "ראשיתו של המושב היה בשנת 1973 ... ראשוני המתיישבים היו קבוצה של 30 משפחות שהגיעו בחודש יולי 1973";
      named for the ten killed in the helicopter accident of 1971. English WP (oldid 1318255352): "founded in 1973 under the
   name Minyan", later renamed for the ten soldiers killed in 1971. JTA: existence by September 1973 (bulletins 4 and 19
   Sep 1973; the 19 Sep item: "Two settlements in the Pitach Raffah region agreed yesterday ... Netiv Haasara and Sadot",
   dateline "TEL AVIV, Sept. 18 (JTA)" read on the PDF, so the hired-labour agreement of the two settlements with the Defense Ministry is 17 Sep 1973, derived from "yesterday"; it is not a founding date). The year agrees; the day rests on Gvati. **Not to be confused with** the present Netiv HaAsara
   (CBS 1982, Hof Ashkelon). Gvati's appendix also has Netzer Hazani (29.5.73) with the region label "פיתחת רפיח", but his
   narrative puts it in the Katif area of the Gaza Strip; it is outside this note's scope.

8. **Ugda (Ogda) [C].** Gvati's text: "ב־1975 הוקם מושב שלישי של תנועת המושבים: אוגדה, במתכונת שני המושבים הקודמים".
   Appendix: "אוגדה", "פתחת רפיח", "3.6.75", named for the armoured division of the Six-Day War. Hebrew WP (oldid
   43737869): infobox 1975; text: "אוגדה היה המושב השלישי שעלה על הקרקע בחבל ימית, בדצמבר 1975", and "ב-31 ביוני 1975 עלו חמש משפחות למרכז הגושי אבשלום" (as printed; June has no 31st), "בסוף אוגוסט 1975 הצטרפו עוד 3 משפחות". JTA, bulletin 17 Jan 1975, dateline "OGDA, Near Rafiah (south of Gaza Strip), Jan. 16": "Today,
   Ogda is a tract of churned up sandy loam", and "By summer of next year, the first 50 families should be moving in".
   So in January 1975 the land was being prepared and the first families were expected in 1976. **[C]:** 3 June 1975 (Gvati), a "31 June" 1975 arrival of five families at the Avshalom centre, end of August 1975, and December 1975 for the moshav on its land (Hebrew WP); each marks something different (a first group, a centre, the moshav site), which neither source ranks.

9. **Sufa (Nahal Sukkot) [C].** Gvati's text: "ב־1974 הוקמה היאחזות סופה של איחוד הקבוצות והקיבוצים". Appendix: "סופה",
   "פתחת רפיח", "1975", Ihud. Hebrew WP (oldid 42404279): began as the Nahal outpost "נח"ל סוכות" that "עלתה לקרקע ... בשנת
   1974", civilianised 17 Jan 1977. English WP (oldid 1302520256): "established in 1974 as a Nahal settlement"; "On 17 January 1977
   it was recognised as a kibbutz". JTA, bulletin 17 Jan 1975, dateline Jan. 16: work on "Ogda,
   on neighboring Sukkot which will be a kibbutz" had "speeded up"; the item does not say whether the outpost was already on the site. CBS 2024: Sufa (successor), year 1975. **[C]:** 1974 (Gvati text, Hebrew WP, English WP) against 1975
   (Gvati appendix, CBS). See `kibbutzim-moshavim-1957-77.md` row 40.

10. **Holit (Nahal Holit, first called Ahimar) [C].** Gvati's text: after Sufa, "הוא הדין לגבי היאחזות חולית"
    (the same; I read this as 1974, an inference from his sentence). Appendix: "נחל חולית", "פיתחת רפיח", "1977", Ihud. Hebrew WP (oldid 41170009): the outpost was
    first called "אחימר" (it cites a Maariv item of 10 January 1975, not opened); Ihud decided on the settlement in December 1975; "וגרעין הנח"ל התיישב בו ביולי אותה השנה" (July 1977); civilianised 17 Jan 1978. JTA, as read in #1647 (its item J3, bulletin of 16 Jan 1978, "TEL AVIV (JTA)", no day in the dateline; I did not open this PDF): Nahal Holit "was turned over to civilian control Thursday" and was re-named Kibbutz Holit. The bulletin is a Monday, so the Thursday is most likely 12 Jan 1978 (derived). English WP (oldid 1375608090): infobox "1978 (in
    Sinai)". CBS 2024: Holit (successor), year 1980. **[C]:** 1974 (Gvati's text), July 1977 (Hebrew WP: the Nahal group settled), 1978 (English WP; Hebrew WP: civilianised 17 Jan 1978); and for the civilian turnover, about 12 Jan 1978 (JTA through #1647, derived) against 17 Jan 1978 (Hebrew WP). The
    English WP coordinate is 15.49 km from the Hebrew WP one; the English infobox also lists a "1982 (current location)",
    which may be the reason: the English article describes the present kibbutz near Nir Yitzhak. The CBS 2024 point for present Holit, 31.23068, 34.32573 (row 27 of #1647's note), is 0.12 km from the English WP point (**derived**, haversine), so the English point is the present site inside Israel, not the Sinai site.

11. **Haruvit (Tarsag) [C].** Gvati's text: "ב־1975 הוקמה היאחזות חרובית, כ־16 ק״מ צפונית־מזרחית לאל־עריש. ב־1977 הפכה
    ההיאחזות למושב של תנועת ״חירות״". Appendix: "תרסג (חרובית)", "צפון סיני", "1976", Herut moshavim, origin (my translation) the year in which Herzl proposed his plan to settle the El Arish area. Hebrew WP (oldid 43737904): Nahal outpost "בדצמבר 1975",
    moshav "בקיץ 1977", expanded early 1978. **[C]:** 1975 against 1976 (Gvati's text and appendix disagree with each
    other).

12. **Nir Avraham [S].** Appendix: "ניר אברהם", "פיתחת רפיח", "10.77", Moshavim movement, named for Avraham Herzfeld.
    Gvati's text lists it among three localities "בשלבי הקמה שונים ב־1977". Hebrew WP (oldid 43737991): land preparation in
    the second half of 1976, founded 1977 (cites Davar, 31 May 1981, not opened). No coordinate in either Wikipedia, in
    GeoNames, or anywhere I read: [GAP].

13. **Talmei Yosef, Sinai [S].** Gvati's text: one of three localities in various stages in 1977; no appendix row
    (searched the page text, see Searches). Hebrew WP (oldid 43738234): "המושב הוקם בשנת 1977"; "ב-27 ביולי 1977 נערך טקס הנחה אבן פינה ליישוב". English WP (oldid 1379995738): infobox 1977. JTA, bulletin 2 Jun 1977, dateline "JERUSALEM
    (JTA)" with no day: "The Jewish National Fund is completing land reclamation works for the new settlement of Talmei
    Yosef in the Rafah salient." The cornerstone day (27 July 1977) rests on Hebrew WP. Present Talme Yosef: CBS 1982.

14. **Pri'el [C].** Gvati's text lists it with rows 12 and 13 as in various stages in 1977. Hebrew WP (oldid 43738223):
    works continued under a government decision of January 1978; families entered in September 1978; its category says
    1978. English WP (oldid 1303822827): "established in early 1978 in the Yamit region of Sinai by a group of Jewish immigrants
    from the Soviet Union". It has no coordinate in the fields I read. The English "Pri Gan" article (oldid 1376006385) says the present Peri Gan "was founded in 1981 by former residents of Pri'el, an Israeli settlement in Sinai" (see "Later localities of Sinai origin" below). **[C]:** 1977 (Gvati, a stage of building) against
    1978 (both Wikipedias). No coordinate: [GAP]. The date is at the edge of the window
    and the row is kept because Gvati counts it among the 11.

15. **Ofira (Sharm el-Sheikh) [GAP on the day].** Gvati's text: "בשארם־א־שייח הוקמה העיר אופירה ... העיר נבנתה על־ידי
    המינהל האזרחי של מרחב שלמה וכמה מאות משפחות מצפון הארץ עברו לגור בה" (no date). No appendix row. Hebrew WP (oldid
    43417313): infobox 1969; "אופירה יושבה בין השנים 1969 – 1975"; the 1956 Nahal outpost Nahal Tarshish on the same
    coast (founded 16 Dec 1956, evacuated in 1957) is before the window and is not a row. JTA: bulletin 20 Apr 1971 (dateline not
    read): "civilian housing is being built at Sharm el-Sheikh", with "a new settlement to be called Hitnahalut" on the
    Eilat road; bulletin 12 Aug 1971, dateline "JERUSALEM, Aug. 11 (JTA)": "The urban settlement at Sharm el-Sheikh has been named
    Ophira after Ophir", names "to be published tomorrow in the official law journal" (so 12 Aug 1971, derived); bulletin 27 Feb 1973, dateline "JERUSALEM, Feb. 26 (JTA)": "Sharm el-Sheikh now appears on Israeli maps as Ofira". No source read gives a founding day. The
    Official Gazette notice itself was not opened. English WP (oldid 1376952553): "Ofira was settled from 1969" and an airfield opened in 1968.

16. **Neviot [C].** Gvati's text: "ב־1971 הוקמו שני ישובים באזור: מושב נביעות – 68 ק״מ דרומה מאילת ודי־זהב". Appendix:
    "נביעות", "חוף מפרץ אילת", "10.5.71", Moshavim movement. Hebrew WP (oldid 42301992): first settlers came "באמצע
    פברואר" 1971 and left after about six weeks; "ניסיון שני ... באוגוסט 1971"; cooperative status in early January 1972.
    JTA, existence only: bulletin 27 Feb 1973 ("the existing settlements of Dahav and Neviot", dateline "JERUSALEM, Feb. 26 (JTA)") and
    bulletin 15 Jul 1976 ("around the moshav of Neviot"). **[C]:** 10 May 1971 (Gvati) against two attempts, February and
    August 1971 (Hebrew WP). English WP "Neviot" is a mineral-water company, not the moshav, and is not used.

17. **Di Zahav [S].** Gvati's text as in row 16; appendix "די-זהב", "חוף מפרץ אילת", "13.9.71", Ha'Oved HaZioni. Hebrew
    WP (oldid 43737890): "עלה על קרקע ב-13 בספטמבר 1971 על ידי גרעין של 13 צעירים". JTA: bulletin 15 Jul 1976 (palms
    around Di Zahav, "half-way between Sharm el-Sheikh and Eilat"). The date agrees between the two; the Hebrew WP
    article may draw on the same appendix, which I could not rule out.

18. **Zaharon [S, single source].** Hebrew WP (oldid 43737898): "בשנת 1977 הוקם המושב זהרון סמוך לנביעות", eight couples in
    caravans in 1979. No row in Gvati's appendix or text (searched the page for the name), none in the English WP I
    searched for, no JTA item read (see Searches).

19. **Shalhevet [S, single source].** Hebrew WP (oldid 42301995): a settlement of 1971-1975 on the Gulf of Suez coast at
    Abu Rudeis next to the oil fields, "מרחב שלמה". Gvati does not mention it (searched the page for "שלהבת" and "אבו רודס").
    The two JTA items my search returned for "Shalhevet" (bulletins 20 Jun and 9 Jul 1974) are about a scientist
    named Shalhevet Freier, not the place. I found no source that dates the founding or the end. [GAP] beyond the one
    article.

20. **Kadesh Barnea (Nitzanei Sinai) [S, single source].** Hebrew WP (oldid 43790225): "היישוב המקורי הוקם בשנת 1977
    כהיאחזות נח"ל"; after the peace treaty the residents were moved to the Nitzana area and in 1986 to the present site (CBS 2024 row Nitzanei Sinai: year 1986, form 310; see "The CBS file"). One article only; no day, no coordinate, not in Gvati's text or appendix (searched). The same
    article's category says 1986 for the present moshav.

## JTA items and their datelines

Bulletin PDFs opened 2026-10-25; the dateline is as read on the page image. "-" means no dateline printed on the item
or the OCR did not reach it, so no event date is claimed.

| Bulletin | PDF | Item | Dateline read | Event date |
|---|---|---|---|---|
| 1 Jul 1968 | pdfs.jta.org/1968/1968-07-01_125.pdf | Desalination plant for Nahal Yam | TEL AVIV, June 30 (JTA) | 30 Jun 1968 |
| 23 Jul 1969 | pdfs.jta.org/1969/1969-07-23_138.pdf | Nahal Yam soldiers injured | TEL AVIV, July 22 (JTA) | 22 Jul 1969 |
| 12 Aug 1971 | pdfs.jta.org/1971/1971-08-12_154.pdf | Settlements given Hebrew names (Ophira) | JERUSALEM, Aug. 11 (JTA) | 11 Aug 1971; the names were due in the law journal on 12 Aug 1971 ("tomorrow", derived) |
| 27 Feb 1973 | pdfs.jta.org/1973/1973-02-27_040.pdf | Sharm el-Sheikh plans (Ofira) | JERUSALEM, Feb. 26 (JTA) | 26 Feb 1973 |
| 9 Mar 1973 | pdfs.jta.org/1973/1973-03-09_048.pdf | Nahal Yam abandoned | JERUSALEM, March 8 (JTA) | 8 Mar 1973 |
| 16 Aug 1973 | pdfs.jta.org/1973/1973-08-16_157.pdf | Road to Yamit | TEL AVIV, Aug. 15 (JTA) | "this week" before 15 Aug 1973; the day is not given |
| 19 Sep 1973 | pdfs.jta.org/1973/1973-09-19_180.pdf | No hired labor in the Raffah region (Netiv Haasara, Sadot) | TEL AVIV, Sept. 18 (JTA) | 17 Sep 1973, the day the two settlements "agreed yesterday" with the Defense Ministry (derived) |
| 29 Aug 1973 | pdfs.jta.org/1973/1973-08-29_166.pdf | First settlers in the urban centre | - | "this week" before 29 Aug 1973 |
| 8 Jan 1975 | pdfs.jta.org/1975/1975-01-08_005.pdf | JNF at Nahal Sinai | JERUSALEM, Jan. 7 (JTA) | 7 Jan 1975 |
| 17 Jan 1975 | pdfs.jta.org/1975/1975-01-17_012.pdf | JNF in the Rafiah program (Ogda) | OGDA, Near Rafiah (south of Gaza Strip), Jan. 16 (JTA) | 16 Jan 1975 |
| 2 Jun 1975 | pdfs.jta.org/1975/1975-06-02_105.pdf | Yamit registration opens | JERUSALEM, June 1 (JTA) | 1 Jun 1975 |
| 19 Mar 1976 | pdfs.jta.org/1976/1976-03-19_055.pdf | JNF working on Yamit | JERUSALEM, March 18 (JTA) | 18 Mar 1976 |
| 26 Apr 1976 | pdfs.jta.org/1976/1976-04-26_080.pdf | 10 new settlements due (Rafiah approaches) | TEL AVIV, April 25 (JTA) | 25 Apr 1976 |
| 15 Jul 1976 | pdfs.jta.org/1976/1976-07-15_136.pdf | JNF palms (Neviot, Di Zahav) | JERUSALEM (JTA), no day | - |
| 2 Jun 1977 | pdfs.jta.org/1977/1977-06-02_105.pdf | JNF land for Talmei Yosef | JERUSALEM (JTA), no day | - |
| 9 Oct 1975 | pdfs.jta.org/1975/1975-10-09_193.pdf | JNF projects (Dikla), "JNF TACKLING NEW PROJECT", page 2 | JERUSALEM, Oct. 8 (JTA) | 8 Oct 1975 |

The 26 Apr 1976 item adds (not a founding date): ten new settlements, "eight moshavim ... and two kibbutzim", "in the
Raffah approaches in the Gaza Strip"; Dulzin said work is going on building large packing houses, and the item says they are being built at Dikla, Saddot, Kerem Shalom and Sdeh-Nitzha, "described as veteran settlements of the region" (the last two are inside the pre-1967 lines). That line is overstruck on the page; the PDF image reads "Dikla" and "Kerem Shalom" (the archive's text transcribes them "Dikia" and "Kerem Shelom"). The 17 Jan 1975 item also names "Dekel and
Eshel, sited near Ogda and at present only army camps"; neither name appears among Gvati's 11, so they are [GAP] and
not rows.

## Searches

All run on 2026-10-25 (UTC) with the endpoint described in "How it was done", date range 1 June 1967 to 30 June 1978 (`after=1967-06-01T00:00:00`, `before=1978-06-30T23:59:59`), `per_page=100`, `page=1`, with the extra parameter `_fields=id,date,link,title,content,slug` (left out of the URLs below). "Total" is the count the server reports (header `x-wp-total`); "returned" is what page 1 held; "contain the term" is how many of the returned items contain the query string (case-insensitive) in their text, as my script counted. The search is fuzzy, so a total can be far above the number of items that contain the term; **only returned items were read, so for every row with total above 100, items 101 onward were not read**, and "0 contain the term" means none of the first 100 returned.

| Query | Search URL | Total | Returned | Contain the term |
|---|---|---|---|---|
| `Yamit` | `https://www.jta.org/wp-json/wp/v2/archive?search=Yamit&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 76 | 76 | 32 |
| `Rafiah Salient` | `https://www.jta.org/wp-json/wp/v2/archive?search=Rafiah+Salient&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 99 | 99 | 0 |
| `Rafiah approaches` | `https://www.jta.org/wp-json/wp/v2/archive?search=Rafiah+approaches&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 51 | 51 | 1 |
| `Rafah approaches` | `https://www.jta.org/wp-json/wp/v2/archive?search=Rafah+approaches&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 20 | 20 | 1 |
| `Avshalom` | `https://www.jta.org/wp-json/wp/v2/archive?search=Avshalom&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 266 | 100 | 0 |
| `Ofira` | `https://www.jta.org/wp-json/wp/v2/archive?search=Ofira&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 15 | 15 | 4 |
| `Ophira` | `https://www.jta.org/wp-json/wp/v2/archive?search=Ophira&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 103 | 100 | 6 |
| `Sharm el-Sheikh settlement` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sharm+el-Sheikh+settlement&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 131 | 100 | 1 |
| `Neviot` | `https://www.jta.org/wp-json/wp/v2/archive?search=Neviot&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 1684 | 100 | 0 |
| `Nabiot` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nabiot&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 10000 | 100 | 0 |
| `Di Zahav` | `https://www.jta.org/wp-json/wp/v2/archive?search=Di+Zahav&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 3 | 3 | 1 |
| `Dizahav` | `https://www.jta.org/wp-json/wp/v2/archive?search=Dizahav&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 7 | 7 | 0 |
| `Dahab settlement` | `https://www.jta.org/wp-json/wp/v2/archive?search=Dahab+settlement&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 5 | 5 | 0 |
| `Shalhevet` | `https://www.jta.org/wp-json/wp/v2/archive?search=Shalhevet&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 2 | 2 | 2 |
| `Zaharon` | `https://www.jta.org/wp-json/wp/v2/archive?search=Zaharon&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 928 | 100 | 0 |
| `Nahal Sinai` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nahal+Sinai&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 110 | 100 | 4 |
| `Nahal Yam` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nahal+Yam&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 111 | 100 | 6 |
| `Nahal Dikla` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nahal+Dikla&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 6 | 6 | 5 |
| `Nahal Sufa` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nahal+Sufa&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 0 | 0 | 0 |
| `Nahal outpost Sinai` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nahal+outpost+Sinai&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 7 | 7 | 0 |
| `Nahal Haruvit` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nahal+Haruvit&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 0 | 0 | 0 |
| `Nahal Holit` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nahal+Holit&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 263 | 100 | 1 |
| `Sadot` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sadot&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 1487 | 100 | 0 |
| `Netiv Haasara` | `https://www.jta.org/wp-json/wp/v2/archive?search=Netiv+Haasara&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 2 | 2 | 1 |
| `Netiv Ha'asara` | `https://www.jta.org/wp-json/wp/v2/archive?search=Netiv+Ha%27asara&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 2 | 2 | 0 |
| `Ugda` | `https://www.jta.org/wp-json/wp/v2/archive?search=Ugda&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 5 | 5 | 0 |
| `Nir Avraham` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nir+Avraham&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 165 | 100 | 0 |
| `Talmei Yosef` | `https://www.jta.org/wp-json/wp/v2/archive?search=Talmei+Yosef&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 21 | 21 | 2 |
| `Priel` | `https://www.jta.org/wp-json/wp/v2/archive?search=Priel&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 195 | 100 | 0 |
| `Pri'el` | `https://www.jta.org/wp-json/wp/v2/archive?search=Pri%27el&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 6595 | 100 | 0 |
| `Neot Sinai` | `https://www.jta.org/wp-json/wp/v2/archive?search=Neot+Sinai&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 351 | 100 | 1 |
| `Dikla` | `https://www.jta.org/wp-json/wp/v2/archive?search=Dikla&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 9 | 9 | 8 |
| `Haruvit` | `https://www.jta.org/wp-json/wp/v2/archive?search=Haruvit&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 2 | 2 | 0 |
| `Holit` | `https://www.jta.org/wp-json/wp/v2/archive?search=Holit&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 6865 | 100 | 0 |
| `Sufa Sinai` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sufa+Sinai&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 0 | 0 | 0 |
| `Kadesh Barnea settlement` | `https://www.jta.org/wp-json/wp/v2/archive?search=Kadesh+Barnea+settlement&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 1 | 1 | 0 |
| `Nitzanei Sinai` | `https://www.jta.org/wp-json/wp/v2/archive?search=Nitzanei+Sinai&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 0 | 0 | 0 |
| `Merkaz Avshalom` | `https://www.jta.org/wp-json/wp/v2/archive?search=Merkaz+Avshalom&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 2 | 2 | 1 |
| `northern Sinai settlement` | `https://www.jta.org/wp-json/wp/v2/archive?search=northern+Sinai+settlement&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 123 | 100 | 0 |
| `Sinai settlements` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sinai+settlements&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 639 | 100 | 16 |
| `"Nahal Sinai"` | `https://www.jta.org/wp-json/wp/v2/archive?search=%22Nahal+Sinai%22&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 110 | 100 | 4 |
| `"Nahal Yam"` | `https://www.jta.org/wp-json/wp/v2/archive?search=%22Nahal+Yam%22&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 111 | 100 | 6 |
| `Rafiah` | `https://www.jta.org/wp-json/wp/v2/archive?search=Rafiah&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 887 | 100 | 2 |
| `Rafah salient` | `https://www.jta.org/wp-json/wp/v2/archive?search=Rafah+salient&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 58 | 58 | 42 |
| `Raffiah` | `https://www.jta.org/wp-json/wp/v2/archive?search=Raffiah&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 168 | 100 | 2 |
| `Pithat Rafiah` | `https://www.jta.org/wp-json/wp/v2/archive?search=Pithat+Rafiah&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 121 | 100 | 8 |
| `Pitchat Rafiah` | `https://www.jta.org/wp-json/wp/v2/archive?search=Pitchat+Rafiah&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 16 | 16 | 15 |
| `Sadot Sinai moshav` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sadot+Sinai+moshav&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 5 | 5 | 2 |
| `Gulf of Eilat settlement moshav` | `https://www.jta.org/wp-json/wp/v2/archive?search=Gulf+of+Eilat+settlement+moshav&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 0 | 0 | 0 |
| `Sharm el-Sheikh civilian settlement` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sharm+el-Sheikh+civilian+settlement&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 16 | 16 | 16 |
| `Ophira` | `https://www.jta.org/wp-json/wp/v2/archive?search=Ophira&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 103 | 100 | 10 |
| `moshav Sinai Nahal Dikla` | `https://www.jta.org/wp-json/wp/v2/archive?search=moshav+Sinai+Nahal+Dikla&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 2 | 2 | 2 |
| `Sinai settlement Nahal outpost` | `https://www.jta.org/wp-json/wp/v2/archive?search=Sinai+settlement+Nahal+outpost&after=1967-06-01T00%3A00%3A00&before=1978-06-30T23%3A59%3A59&per_page=100&page=1` | 3 | 3 | 2 |

The first 40 queries are the first run; the last 13 the second run (queries starting with a quote or with "Rafiah", "Rafah salient", "Raffiah", "Pithat Rafiah", "Pitchat Rafiah", "Sadot Sinai moshav", "Gulf of Eilat settlement moshav", "Sharm el-Sheikh civilian settlement", "Ophira", "moshav Sinai Nahal Dikla", "Sinai settlement Nahal outpost"). For the second run the string tested was a pattern, not the query: for example `Pitchat Rafiah` was tested with the pattern `Pit[a-z]*h?at Raf`, and "Gulf of Eilat settlement moshav" with `Neviot|Nabiot|Dahab|Di Zahav|Dizahav|Nabi`.

Other negative searches, all on 2026-10-25:

- **Gvati page** (https://benyehuda.org/read/14684, 1,348,249 bytes, tags and vowel signs removed): the names "זהרון", "שלהבת", "אבו רודס", "קדש ברנע" and "ניצני סיני" each returned 0 matches. The appendix tables (869 rows) have no row for Yamit, Talmei Yosef, Pri'el, Ofira, Nahal Yam, Zaharon, Shalhevet or Kadesh Barnea; I listed every row whose text contains "פיתחת", "פתחת", "סיני", "מפרץ אילת", "מרחב שלמה", "נחל ים", "נחל סיני" or "חירות" and read the Sinai ones. "תלמי־יוסף" (written without the hyphen once vowel signs and the maqaf are removed) and "פריאל" occur once each, in the Rafah approaches narrative (the same sentence). The appendix has a cross-reference row "חרובית (ראה תרס"ג)" (Haruvit, see Tarsag).
- **GeoNames Egypt dump** (EG.txt, 35,027 rows): case-insensitive search for `yamit|ofira|sadot|neviot|nabi|dahab|dikla|ugda|ophira|talmei|netiv|nahal|holit|sufa|haruvit|priel|nir avraham|avshalom|shalhevet` and for the Hebrew names of rows 1 to 19. The pattern matched 194 rows (58 canals, 34 populated places, 25 hotels, 21 wadis and others); the only Israeli settlements among them are the three in "How it was done". The rest are unrelated Egyptian names (for example Sidi Abd an Nabi, Nabi tin mine, Nahal Shani (a wadi)) and the Egyptian town Dahab (geonameid 358245, 28.48208, 34.49505), beside which Di Zahav was founded; it is not an Israeli locality and I did not use it as a coordinate for row 17.
- **English Wikipedia pages that do not exist under the title I tried** (MediaWiki API, `redirects=1`): "Neot Sinai", "Sadot, Sinai", "Ugda", "Haruvit", "Nir Avraham", "Di Zahav", "Atzmona, Sinai", "Hatzar Adar", "Zahron", "Kadesh Barnea (moshav)", "Avshalom (Sinai)". 11 of the 23 titles tried were missing. The title "Neviot" exists but is a mineral-water company; "Shalhevet" is a disambiguation page of personal names.

## Outside the window and outside the scope

- **After 1977 (Hebrew WP years, not researched beyond the infobox):** Pri Gan in Sinai (the Hebrew WP article for the Sinai moshav was not opened; the present Peri Gan has CBS year 1981), Atzmona (Sinai) 20 March 1979 (oldid 43279026), Hatzar Adar December 1981 (oldid 42805245), Maoz HaYam late 1981 or early 1982 (oldid 43365334; not in the Yamit-region list). Hebrew WP's list of the Yamit region has 16 localities (oldid 43082391); rows 5 to 14 here are 10 of them, and the other six are Atzmona, Pri Gan, Hatzar Adar and, from rows 2 to 4, Sadot, Dikla and Neot Sinai.
- **Later localities of Sinai origin** are in [`localities-1977-93-pre67.md`](../Era%201977-93/localities-1977-93-pre67.md)
  (#1647), which has the CBS rows of the later localities inside the pre-1967 lines. Those with the name of a row here, with
  the CBS spellings: Holit (its row 27, our row 10), Peri Gan (50, our row 14, Pri'el), Netiv HaAsara (67, our row 7),
  Talme Yosef (72, our row 13), Nizzane Sinay (96, our row 20) and Avshalom (108, our row 6). Amnun, En HaBesor, Deqel and Yated
  are later localities of Sinai origin that note covers; they have no row here because they were founded after 1977.
- **Before the window:** Nahal Tarshish (also called Nahal Ofir), a Nahal outpost at Sharm el-Sheikh founded 16 December
  1956 and evacuated in 1957 (Hebrew WP oldid 42404149, not researched).
- **The Gaza Strip proper**, which Gvati treats in the same section: Nahal outposts Kfar Darom (1970), Netzarim and Morag
  (1972), and the moshavim Netzer Hazani and Katif of the Katif area. Not in this note.
- **Where the line between this note and the Gaza Strip note falls (same line as in `gaza-localities-1967-77.md`,
  the Gaza Strip note of #130, `gaza-localities-1967-77.md`).** A locality is in the Gaza Strip note if the sources put it inside the Strip, the area Egypt
  held between 1949 and June 1967 north and west of the Egyptian frontier that the armistice line of 24 February 1949
  (Art. VI.1, "continuing thus to the Egyptian frontier"; `armistice-lines-1949.md`) stops at. It is in this note if
  the sources put it in the Rafah approaches (Hebrew Pithat Rafiah), which Gvati describes as "פיתחת רפיח, המשתרעת בין
  רפיח למבואות אלעריש לאורך חוף הים" and as "אזור חיץ בין רצועת עזה" ... "לבין מרחבי סיני" (a buffer zone between the Strip and the Sinai expanses) (Gvati, vol. 2,
  Ben-Yehuda edition, https://benyehuda.org/read/14684, section "רצועת עזה ופיתחת רפיח", read 2026-10-25). By that
  split the Gaza note has Kfar Darom, Netzarim, Morag, Netzer Hazani and Katif (Netzer Hazani's appendix row says "פיתחת רפיח"
  but Gvati's narrative puts it in the Katif area of the Strip, so it goes to the Gaza note; Katif has no appendix row); this note has the
  Rafah-approaches and Sinai rows of the table above. Nearest to the line: Morag (the southernmost
  locality of the Strip: "between Khan Yunis and Rafah" in Hebrew WP, "3 km south of Khan Yunis" in Gvati) and, on this side, Dikla, Sadot
  and Netiv HaAsara. No locality is on both lists. This is a reading of the sources named, not a surveyed boundary.
- **Bases and camps** (Etam air base, December 1976, Camp Nurit of the Yamit region; Hebrew WP oldid 43678026) are
  not localities in Gvati's list and are not rows.

## What was not done

- **No primary list.** The Statistical Abstract of Israel and the Israel Government Year-Book of 1977-1981 were looked
  for through the Internet Archive search API (`https://archive.org/advancedsearch.php`, 25 Oct 2026): "title:(Statistical
  Abstract of Israel) AND (year:[1978 TO 1981])" returned 0; "Statistical abstract of Israel 1979" returned 3 (all CIA
  Reading Room documents, none the Abstract); "Statistical abstract of Israel 1978 Central Bureau of Statistics" returned
  2 (CIA documents); "Israel Government Year Book" with years 1977 to 1981 returned 0; `title:("Israel Year Book") AND
  year:[1977 TO 1981]` returned 1 (`israelyearbook0000unse_r4t9`, 1977), whose full text answered HTTP 401. So no CBS
  publication, Government Year-Book or Year Book list was read.
- **No Official Gazette.** `https://www.nevo.co.il/Law_word/law10/yalkut-2633.pdf` (Yalkut Hapirsumim 2633, 10 June
  1980, cited by Hebrew WP for the official names of Dikla and Netiv HaAsara) answered HTTP 200 with an HTML page of 3,184
  bytes, not a PDF. The notice of August 1971 naming Ofira, mentioned by JTA, was not looked for further.
- **No Knesset record, no State Archives, no Jewish Agency or Zionist Archives record** was opened; the founding
  days therefore rest on Gvati and Wikipedia, and the state's own record of each founding is the main gap.
- **JTA coverage is thin before 1973** for the Rafah approaches, and the search is capped at 100 returned items per
  query (see Searches); items beyond the first 100 for "Sadot", "Neviot", "Holit", "Pri'el", "Nir Avraham" and "Zaharon"
  were not read. Of the 16 items in the datelines table, 13 have a dated dateline, 2 a place without a day and 1 none (29 Aug 1973).
- **JTA items returned but not opened:** "Nahal Holit" (bulletin 16 Jan 1978), "Ophira" (19 May 1975, 200 housing units), "Talmei Yosef" (10 Jan 1978) and "Neot Sinai" (10 Jan 1978) each came back containing the term and were read only as search snippets, which this note does not use as sources.
- **Maariv and Davar**, which Hebrew WP cites for several days (Nahal Sinai 28 Dec 1967, Ahimar 10 Jan 1975, Nir Avraham
  1981, Yamit and others), are not opened by me; the Historical Jewish Press and the National Library newspaper archive
  were not searched.
- **No map.** No coordinate comes from a map I opened. Wikipedia infobox coordinates and GeoNames points are not
  surveys, and for the Nahal outposts that later moved (Nahal Sinai, Holit, Sufa) the first site is not given by any
  source I read. Rows 12, 14 and 20 have no coordinate. Row 6's coordinate is doubtful (see its row).
- **No row was settled between [C] values.** The note does not choose one date.

## Sources and revisions

| Source | URL or identifier | Day read | Used for |
|---|---|---|---|
| Gvati, 100 Years of Settlement vol. 2 (Ben-Yehuda) | https://benyehuda.org/read/14684 | 2026-10-25 | list, narrative, appendix rows |
| CBS localities file 2024 | https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx | 2026-10-25 | the seven same-name rows |
| GeoNames Egypt dump | https://download.geonames.org/export/dump/EG.zip | 2026-10-25 | Yamit, Dikla, Avshalom junction |
| JTA Daily News Bulletin | https://www.jta.org/archive/ and http://pdfs.jta.org/ (items in the datelines table) | 2026-10-25 | press dates, datelines |
| Hebrew Wikipedia, oldids | חבל ימית 43082391; פתחת רפיח 41586408; מרחב שלמה 43791984; אבשלום (סיני) 41169976; אוגדה (יישוב) 43737869; דקלה (יישוב) 43737895; חולית (סיני) 41170009; חרובית (יישוב) 43737904; ימית 43845355; נאות סיני 43737905; ניר אברהם 43737991; נתיב העשרה (סיני) 43738008; פריאל (יישוב) 43738223; סופה (סיני) 42404279; שדות 43738231; תלמי יוסף (סיני) 43738234; אופירה 43417313; די זהב 43737890; נביעות (יישוב) 42301992; שלהבת (יישוב) 42301995; זהרון (יישוב) 43737898; ניצני סיני 43790225; נח"ל ים 44108692; נח"ל תרשיש 42404149; בסיס איתם 43678026; עצמונה (סיני) 43279026; חצר אדר 42805245; מעוז הים (סיני) 43365334 | 2026-10-25 | dates, civil dates, coordinates |
| English Wikipedia, oldids | Yamit 1378290812; Dikla 1304841200; Netiv HaAsara, Sinai 1318255352; Sufa, Sinai 1302520256; Holit 1375608090; Talmei Yosef, Sinai 1379995738; Pri'el 1303822827; Ofira 1376952553; Nahal Yam 1376881599; Pri Gan 1376006385 | 2026-10-25 | coordinates, Holit infobox |

URL form for a revision: `https://he.wikipedia.org/w/index.php?oldid=<n>` and `https://en.wikipedia.org/w/index.php?oldid=<n>`.
