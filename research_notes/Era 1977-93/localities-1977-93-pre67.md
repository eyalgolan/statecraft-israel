# New localities of 1977-1993 inside the pre-1967 lines (research note, 2026-10-25)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file stays
paused until after the MVP. Refs #165 (1977-93 21), the "settlements founded 1977-93" row of [the 1977-1993
design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md) section 7.4. One topic of #165 only: **new
localities founded 1977-1993 inside Israel's pre-1967 lines** (kibbutzim, moshavim, community settlements including
the Galilee lookouts (mitzpim), and towns). **The territories (Judea and Samaria, Gaza, the Golan, Sinai), caravan
sites and the lines are not in it** (the lines are in [`lines-1977-93.md`](lines-1977-93.md), PR #1640). Which rows enter the game, and their names, wait for the
owner's sensitivity list (#150); this note makes no include or omit choice and no naming choice.

It follows the format of [`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) and the conventions of
PR #1643 (development towns) and PR #1646 (kibbutzim and moshavim 1957-77). **Overlap:** four rows with a CBS year of
1977 (Bet Rimmon, Kefar Rut, Moran, Shilat) are also in the 1957-77 note, because 1977 is in both windows. Every page
below was opened on **25 October 2026** (UTC) unless a row says otherwise.

The question: which localities were founded between 1 January 1977 and 31 December 1993 inside Israel's pre-1967
lines, and for each: the founding date (and what the date marks), coordinates from a named source, and the sources.

## Short answer

- **The list.** The Central Bureau of Statistics (CBS) localities file 2024 has 215 rows with a year of founding of 1977
  to 1993. 92 are in the Judea and Samaria district and 11 in the Golan subdistrict and are outside this note's topic (other parts of #165). The file has no Gaza Strip or Sinai rows. **112 remain**, every one outside the CBS Judea and Samaria district and the Golan subdistrict (a CBS district test, not a Green Line test; see "Flags" for the rows whose sources place a site on or beyond the line): 40 community settlement, 25 moshav, 21 kibbutz, 13 urban-size band (codes 140-190), 7 other rural settlement, 4 moshav shitufi, 2 institutional settlement.
- **Founding dates.** Every row has the CBS year. For the year, 73 rows agree across the sources read and 36 rows
  disagree [C] (listed in "Rows where the sources disagree"); 3 rows have only the CBS year behind them. A day or a
  month is stated for 44 rows, always by Wikipedia ([S]); Hebrew Wikipedia gives the going-onto-the-land day, the
  dedication day or the cornerstone day without saying which one the CBS year follows.
- **Contemporaneous records.** I found and read 9 Jewish Telegraphic Agency (JTA) Daily News Bulletin items (scanned PDFs)
  that bear on 14 rows (section "Contemporaneous records read"). Two of them place a founding in time: the 10 October
  1977 report that Kefar Rut was "established recently" and that 25 families had earlier moved into temporary
  dwellings for Shilat, and the 18 January 1982 report that settlers had "already moved into" six western-Negev
  moshavim and two kibbutzim. No JTA item gave a founding day for any row. **All 112 rows keep a [GAP] for a founding day
  from a contemporaneous or official record**, except where the row's text says otherwise.
- **Flagged, not removed:** rows whose site or history touches the Latrun area, the Green Line, Sinai or an earlier
  settlement (section "Flags"). The note does not decide whether they belong.

## Conventions and tags

- **[V, agent read 2026-10-25; a person's check pending]**: what a CBS publication prints, read today. It says nothing about whether the printed value is right.
- **[S]** secondary: one source, or several that plainly copy one another. Wikipedia, Gvati and JTA bulletins are [S]. Where Wikipedia cites a newspaper, a book or a government publication, I do not count that source as read; I did not open any Wikipedia citation.
- **[C]** conflicting: sources disagree; every value is given and what each marks, as far as the source says.
- **[GAP]** not closed, with what was tried.
- **Derived** marks a number or a date this note computed; the arithmetic or the library is named.
- Quotes are copied from the page read; Hebrew quotes carry a translation. Translations from Hebrew are mine.
- In the summary table the tag is for the founding **year**: [S] the years in Wikipedia agree with the CBS year, [S] CBS only: no other source gave a year, [C] a Wikipedia year differs from the CBS year.

## How it was done

- **The list, the CBS year and the coordinates.** CBS, localities file 2024 ("קובץ יישובים 2024"),
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (downloaded and opened 2026-10-25; 1,490
  locality rows, one sheet, 398 with an empty year of founding). Filter: column "שנת ייסוד" (year of founding) from 1977
  to 1993, then excluded by the file's own columns: district "אזור יהודה ושומרון" (Judea and Samaria; 92 rows in the
  window) and subdistrict "גולן" (Golan; 11 rows). 215 - 92 - 11 = 112 (derived). No filter on the form-of-settlement
  column: every row of the window that survives the two exclusions is in the table, so towns and urban-size localities
  are in it too. **[V, agent read 2026-10-25; a person's check pending]** for what the file prints.
- **What the CBS year is.** The CBS intro (https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/intro.pdf,
  December 2025, opened 2026-10-25), section 12, "שנת ההתיישבות היהודית/שנת ייסוד": "לפי נסיבות המקרה, נרשמה שנת
  הייסוד של היישוב או שנת אכלוסו בידי יהודים" (as the case requires, the year of founding of the locality or the year it
  was settled by Jews is recorded); for a locality settled more than once, the last settling year after a long break and
  the original year after a short one; "במקרה של איחוד יישובים צוינה שנת הייסוד של הוותיק מביניהם" (in a merger of
  localities, the founding year of the oldest of them). The intro names Modi'in-Maccabim-Re'ut among the merged
  authorities. [V, agent read 2026-10-25; a person's check pending]
- **What the codes mean.** The 2024 file does not list the code meanings. As in PR #1646 I cross-tabulated the 2024
  codes against the form printed in the CBS publication "Local authorities in Israel 2009", list 03, settlements in
  regional councils (https://www.cbs.gov.il/he/publications/doclib/2011/local_authorities09/pdf/list03_h.pdf, opened
  2026-10-25), matching on the 4-digit locality symbol (981 of 984 table rows matched a 2024 row). **Derived** result:
  310 = moshav (392 of 398 matched rows print "מושב"), 320 = moshav shitufi (39 of 41), 330 = kibbutz (263 of 264),
  340 = institutional settlement (27 of 27), 350 = other rural, Jewish (51 of 52), 370 = community settlement (87 of
  88). Codes 140 to 190 are not in that list's rural table; in 2009 the rows with these codes that it does print are
  Jewish localities of 2,000 to 9,999 people or community settlements, so I call them "urban-size band" and give the
  municipal status the file prints. The mapping is mine; the CBS does not state it for the 2024 file.
- **Coordinates.** The CBS field "קואורדינטות" has twelve digits; as in `settlements-1949-51.md` I read it as an Israel
  Transverse Mercator easting and northing in metres (six digits each) and converted it with `pyproj` (EPSG:2039 to
  EPSG:4326), rounded to five decimals. That reading is mine, not stated in the file. **Derived** check against English
  Wikipedia's own coordinates for the 105 rows whose article has one (haversine, my computation): median 127 m, largest 1297 m.
  Raw CBS fields are in the table.
- **Wikipedia** (Hebrew and English) was fetched through the MediaWiki API as wikitext of the current revision; each
  revision is named in the last table (`oldid`). It is a lead and a cross-check, always [S]. **Identity of each Hebrew
  article:** the article carries the bot infobox template `{{בוט יישובים/<CBS symbol>}}` (the CBS locality symbol is in
  the template name); I accepted a Hebrew article only when the symbol in the template equals the CBS symbol. 109 of 112
  rows have such an article. The English article is the one the Hebrew article's language link points to (105 rows).
  The founding year in the table is read from the article's category "יישובים שהוקמו ב-YYYY" (Hebrew), the category
  "Populated places established in YYYY" and the infobox "founded"/"established" field (English).
- **Gvati.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix "List of all Jewish rural settlements in Israel",
  Ben-Yehuda Project edition, https://benyehuda.org/read/14684 (opened 2026-10-25; 711 appendix rows parsed by script
  from the page). The appendix has no row dated after 1977 (its latest dates are "1977"; "מוצא (תחתית) 1984" is a row
  of the section on urban localities that began as colonies, not in this list). It covers 4 of the 112 rows by name and site (Moran, Nahal
  Bet Rimmon, Shilat, each printed "1977"; Kefar Rut, printed "77."). It covers a fifth row by name only: "נחל חולית" (region "פיתחת רפיח", 1977), which is the Sinai site of Holit, not the present kibbutz. It is cited only in the rows where it matters.
- **JTA Daily News Bulletin.** Search: `https://www.jta.org/?s=<query>&post_type=archive` (the site's own archive
  search), run 2026-10-25 for the English name of every row, then the item page (`https://www.jta.org/archive/...`)
  opened for each result. The item page shows the bulletin date; the event date comes from the dateline in the original
  bulletin (the "See Original Daily Bulletin" PDF at `pdfs.jta.org`). The PDFs are scans with no text layer; I read
  them with OCR (`rapidocr-onnxruntime`) and checked the two key items (10 October 1977, pages 2 and 3) by looking at
  the page image; after the owner's review I also read the datelines of J5, J6, J8 and J9 and the hyphen in J2 on the page images. The OCR text of the other items is not a verbatim source and is quoted below only where it is
  unambiguous. Every negative search is recorded in "Searches".
- Hebrew dates were not converted; no row needed it.
- All requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.


## Summary table

Order: by CBS year, then name. "CBS year", "CBS code", "Subdistrict" and "Municipal status" are [V, agent read 2026-10-25; a person's check pending] as to what the file prints (regional councils are printed as the file has them, in Hebrew; "local council" and "municipality" are my English for the Hebrew values, and "none" is my English for "חסר מעמד מוניציפלי", no municipal status). Coordinates are **derived** from the CBS field (method above). "he.WP" and "en.WP" are the founding year in the Hebrew and English articles (- = none read). "Tag" is the note's tag for the year.

| # | Locality (CBS spelling) | Type | Subdistrict | Municipal status | CBS year | CBS code | Lat, lon (derived) | CBS raw field | he.WP | en.WP | Tag | Flag |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Bet Rimmon | kibbutz | Jezreel | מועצה אזורית הגליל התחתון | 1977 | 330 | 32.78203, 35.32813 | 231044743048 | 1977 | 1977 | [S] |  |
| 2 | Kefar Rut | moshav | Ramla | מועצה אזורית חבל מודיעין | 1977 | 310 | 31.90989, 35.03070 | 203023646340 | 1977 | 1977 | [S] | Latrun area, see Flags |
| 3 | Moran | kibbutz | Acre | מועצה אזורית משגב | 1977 | 330 | 32.92162, 35.39372 | 237161758538 | 1977 | 1977 | [S] |  |
| 4 | Qesaryya | urban-size band, code 180 | Hadera | מועצה אזורית חוף הכרמל | 1977 | 180 | 32.50429, 34.90822 | 191619712279 | 1977 | 1952 | [C] |  |
| 5 | Shilat | moshav | Ramla | מועצה אזורית חבל מודיעין | 1977 | 310 | 31.91883, 35.01890 | 201908647333 | 1977 | 1977 | [S] | Latrun area, see Flags |
| 6 | Lappidot | moshav | Acre | מועצה אזורית מעלה יוסף | 1978 | 310 | 32.95889, 35.26183 | 224822762657 | 1978 | 1978 | [S] |  |
| 7 | Lotem | kibbutz | Acre | מועצה אזורית משגב | 1978 | 330 | 32.88302, 35.35734 | 233764754252 | 1978 | 1978 | [S] |  |
| 8 | Yad HaShemona | moshav shitufi | Jerusalem | מועצה אזורית מטה יהודה | 1978 | 320 | 31.80925, 35.08980 | 208601635173 | 1971 | 1971 | [C] |  |
| 9 | Bar Yohay | community settlement | Safed | מועצה אזורית מרום הגליל | 1979 | 370 | 32.99732, 35.44905 | 242317766945 | 1977 | 1977 | [C] |  |
| 10 | Eshbal | kibbutz | Acre | מועצה אזורית משגב | 1979 | 330 | 32.87660, 35.30502 | 228869753534 | 1979 | 1979 | [S] |  |
| 11 | Kelil | community settlement | Acre | מועצה אזורית מטה אשר | 1979 | 370 | 32.98057, 35.19729 | 218788765061 | 1979 | 1979 | [S] |  |
| 12 | Mahane Yattir | other rural settlement | Beer Sheva | none | 1979 | 350 | 31.34743, 35.06014 | 205725583969 | 1979 | 1979 | [S] | see Flags |
| 13 | Mattat | community settlement | Acre | מועצה אזורית מעלה יוסף | 1979 | 370 | 33.04106, 35.35756 | 233759771780 | 1979 | 1979 | [S] |  |
| 14 | Mizpe Netofa | community settlement | Jezreel | מועצה אזורית הגליל התחתון | 1979 | 370 | 32.80253, 35.38577 | 236440745330 | 1981 | 1981 | [C] |  |
| 15 | Sappir | other rural settlement | Beer Sheva | מועצה אזורית הערבה התיכונה | 1979 | 350 | 30.61484, 35.18417 | 217513502737 | 1979 | 1979 | [S] |  |
| 16 | Zviyya | community settlement | Acre | מועצה אזורית משגב | 1979 | 370 | 32.88967, 35.33174 | 231368754986 | - | - | [S] CBS only |  |
| 17 | Abbirim | community settlement | Acre | מועצה אזורית מעלה יוסף | 1980 | 370 | 33.03853, 35.28687 | 227156771492 | 1980 | 1980 | [S] |  |
| 18 | Adi | community settlement | Jezreel | מועצה אזורית עמק יזרעאל | 1980 | 370 | 32.78222, 35.17387 | 216592743063 | 1980 | 1980 | [S] |  |
| 19 | Allon HaGalil | moshav | Jezreel | מועצה אזורית עמק יזרעאל | 1980 | 310 | 32.75686, 35.21932 | 220851740250 | 1980 | 1980 | [S] |  |
| 20 | Amuqqa | community settlement | Safed | מועצה אזורית מרום הגליל | 1980 | 370 | 32.99808, 35.52476 | 249392767048 | 1980 | 1980 | [S] |  |
| 21 | Gilon | community settlement | Acre | מועצה אזורית משגב | 1980 | 370 | 32.90297, 35.23704 | 222506756455 | 1980 | 1980 | [S] |  |
| 22 | Gitta | community settlement | Acre | מועצה אזורית מעלה יוסף | 1980 | 370 | 32.96709, 35.24897 | 223619763566 | 1980 | 1980 | [S] |  |
| 23 | Gornot HaGalil | other rural settlement | Acre | מועצה אזורית מעלה יוסף | 1980 | 350 | 33.05998, 35.24920 | 223636773868 | 1980 | 1980 | [S] |  |
| 24 | Hararit | community settlement | Acre | מועצה אזורית משגב | 1980 | 370 | 32.84568, 35.36661 | 234638750112 | 1982 | 1982 (infobox 1980) | [C] |  |
| 25 | Harashim | community settlement | Acre | מועצה אזורית משגב | 1980 | 370 | 32.95664, 35.32800 | 231009762413 | 1980 | 1980 | [S] |  |
| 26 | Hilla | community settlement | Acre | מועצה אזורית מעלה יוסף | 1980 | 370 | 33.03642, 35.24428 | 223178771255 | - | - | [S] CBS only |  |
| 27 | Holit | kibbutz | Beer Sheva | מועצה אזורית אשכול | 1980 | 330 | 31.23068, 34.32573 | 135736571348 | 1982 | 1978 | [C] | Sinai origin, see Flags |
| 28 | Iddan | moshav | Beer Sheva | מועצה אזורית הערבה התיכונה | 1980 | 310 | 30.80660, 35.30045 | 228644524001 | 1980 | 1980 | [S] |  |
| 29 | Kahal | moshav | Safed | מועצה אזורית מבואות החרמון | 1980 | 310 | 32.89073, 35.51071 | 248114755138 | 1980 | 1980 | [S] |  |
| 30 | Kammon | community settlement | Acre | מועצה אזורית משגב | 1980 | 370 | 32.91245, 35.35994 | 234003757516 | 1980 | 1980 | [S] |  |
| 31 | Keramim | kibbutz | Beer Sheva | מועצה אזורית בני שמעון | 1980 | 330 | 31.33381, 34.91808 | 192203582485 | 1980 | 1980 | [S] |  |
| 32 | Lavon | other rural settlement | Acre | מועצה אזורית משגב | 1980 | 350 | 32.94263, 35.28680 | 227158760856 | 1980 | 1980 | [S] |  |
| 33 | Manof | moshav | Acre | מועצה אזורית משגב | 1980 | 310 | 32.85329, 35.23660 | 222467750945 | 1980 | 1980 | [S] |  |
| 34 | Manot | moshav | Acre | מועצה אזורית מעלה יוסף | 1980 | 310 | 33.03854, 35.19470 | 218546771490 | 1980 | 1980 | [S] |  |
| 35 | Mikhmannim | community settlement | Acre | מועצה אזורית משגב | 1980 | 370 | 32.90675, 35.32638 | 230864756880 | 1980 | 1980 | [S] |  |
| 36 | Nizzana (Qehilat Hinuh) | institutional settlement | Beer Sheva | מועצה אזורית רמת נגב | 1980 | 340 | 30.88614, 34.42151 | 144592533078 | 1987 | 1987 | [C] |  |
| 37 | Pelekh | kibbutz | Acre | מועצה אזורית משגב | 1980 | 330 | 32.93382, 35.23335 | 222160759876 | 1982 | 1982 | [C] |  |
| 38 | Qaddarim | kibbutz | Safed | מועצה אזורית הגליל העליון | 1980 | 330 | 32.89803, 35.47425 | 244700755939 | 1980 | - | [S] |  |
| 39 | Shekhanya | community settlement | Acre | מועצה אזורית משגב | 1980 | 370 | 32.84892, 35.24682 | 223424750461 | 1980 | 1980 | [S] |  |
| 40 | Tal-El | community settlement | Acre | מועצה אזורית משגב | 1980 | 370 | 32.92597, 35.17837 | 217018759005 | 1980 | 1980 | [S] |  |
| 41 | Tefahot | moshav | Safed | מועצה אזורית מרום הגליל | 1980 | 310 | 32.86848, 35.42216 | 239833752650 | 1980 | 1980 | [S] |  |
| 42 | Telalim | kibbutz | Beer Sheva | מועצה אזורית רמת נגב | 1980 | 330 | 30.99211, 34.77005 | 177966544645 | 1980 | 1980 | [S] |  |
| 43 | Tuval | kibbutz | Acre | מועצה אזורית משגב | 1980 | 330 | 32.92893, 35.24472 | 223224759334 | 1980 | 1980 | [S] |  |
| 44 | Ziv'on | kibbutz | Acre | מועצה אזורית הגליל העליון | 1980 | 330 | 33.02496, 35.41618 | 239239770004 | 1980 | 1980 | [S] |  |
| 45 | Hosha'aya | urban-size band, code 190 | Jezreel | מועצה אזורית עמק יזרעאל | 1981 | 190 | 32.75850, 35.29162 | 227626740436 | 1980 | 1980 | [C] |  |
| 46 | Kallanit | moshav | Safed | מועצה אזורית מרום הגליל | 1981 | 310 | 32.87510, 35.45559 | 242960753391 | 1982 | 1982 | [C] |  |
| 47 | Kokhav Ya'ir | urban-size band, code 180 | Petah Tikva | local council | 1981 | 180 | 32.22158, 34.99575 | 199784680908 | 1986 | 1986 | [C] |  |
| 48 | Mizpe Aviv | community settlement | Acre | מועצה אזורית משגב | 1981 | 370 | 32.83679, 35.20033 | 219071749115 | 1981 | 1981 | [S] |  |
| 49 | Moreshet | community settlement | Acre | מועצה אזורית משגב | 1981 | 370 | 32.82601, 35.23288 | 222119747919 | 1996 | 1996 | [C] |  |
| 50 | Peri Gan | moshav | Beer Sheva | מועצה אזורית אשכול | 1981 | 310 | 31.22169, 34.35541 | 138556570329 | 1981 | 1981 | [S] | Sinai origin, see Flags |
| 51 | Raqqefet | community settlement | Acre | מועצה אזורית משגב | 1981 | 370 | 32.85494, 35.26486 | 225112751129 | 1981 | 1981 | [S] |  |
| 52 | Ravid | kibbutz | Kinneret | מועצה אזורית עמק הירדן | 1981 | 330 | 32.85084, 35.46434 | 243786750703 | 1981 | 1981 | [S] |  |
| 53 | Sede Avraham | moshav | Beer Sheva | מועצה אזורית אשכול | 1981 | 310 | 31.21139, 34.33702 | 136795569201 | 1981 | 1981 | [S] |  |
| 54 | Yated | moshav | Beer Sheva | מועצה אזורית אשכול | 1981 | 310 | 31.20635, 34.32694 | 135830568649 | 1980 | 1982 | [C] | Sinai origin, see Flags |
| 55 | Yevul | moshav | Beer Sheva | מועצה אזורית אשכול | 1981 | 310 | 31.18927, 34.31811 | 134973566762 | 1981 | 1981 | [S] |  |
| 56 | Zurit | community settlement | Acre | מועצה אזורית משגב | 1981 | 370 | 32.90260, 35.25146 | 223855756414 | 1981 | - | [S] |  |
| 57 | Deqel | moshav | Beer Sheva | מועצה אזורית אשכול | 1982 | 310 | 31.19551, 34.34613 | 137649567433 | 1982 | 1982 | [S] | Sinai origin, see Flags |
| 58 | Elifaz | kibbutz | Beer Sheva | מועצה אזורית חבל אילות | 1982 | 330 | 29.79502, 35.01130 | 200783411870 | 1983 | 1983 | [C] |  |
| 59 | En HaBesor | moshav | Beer Sheva | מועצה אזורית אשכול | 1982 | 310 | 31.28145, 34.44245 | 146896576895 | 1982 | 1982 | [S] | Sinai origin, see Flags |
| 60 | En Tamar | moshav | Beer Sheva | מועצה אזורית תמר | 1982 | 310 | 30.94322, 35.37414 | 235673539156 | 1982 | 1982 | [S] |  |
| 61 | Harduf | kibbutz | Jezreel | מועצה אזורית עמק יזרעאל | 1982 | 330 | 32.76369, 35.17310 | 216520741008 | 1982 | 1982 | [S] |  |
| 62 | Harish | urban-size band, code 160 | Hadera | municipality | 1982 | 160 | 32.46039, 35.04781 | 204730707383 | 1982 | 1982 (infobox 1980) | [C] |  |
| 63 | Livnim | moshav | Safed | מועצה אזורית מרום הגליל | 1982 | 310 | 32.86380, 35.49456 | 246611752147 | 1982 | 1982 | [S] |  |
| 64 | Nataf | community settlement | Jerusalem | מועצה אזורית מטה יהודה | 1982 | 370 | 31.83193, 35.06762 | 206504637690 | 1982 | 1982 | [S] |  |
| 65 | Ne'ot Smadar | kibbutz | Beer Sheva | מועצה אזורית חבל אילות | 1982 | 330 | 30.04973, 35.02665 | 202310440103 | 1989 | 1989 | [C] |  |
| 66 | Nehusha | moshav | Jerusalem | מועצה אזורית מטה יהודה | 1982 | 310 | 31.62740, 34.95233 | 195537615030 | 1955 | 1955 | [C] | re-founded site, see Flags |
| 67 | Netiv HaAsara | moshav | Ashkelon | מועצה אזורית חוף אשקלון | 1982 | 310 | 31.57215, 34.53960 | 156342609068 | 1982 | 1982 | [S] | Sinai origin, see Flags |
| 68 | Nirit | community settlement | Petah Tikva | מועצה אזורית דרום השרון | 1982 | 370 | 32.14685, 34.98501 | 198754672624 | 1981 | 1982 | [C] |  |
| 69 | Qazir | urban-size band, code 190 | Hadera | מועצה אזורית מנשה | 1982 | 190 | 32.48609, 35.10333 | 209953710227 | 1982 | 1982 | [S] |  |
| 70 | Qoranit | community settlement | Acre | מועצה אזורית משגב | 1982 | 370 | 32.84369, 35.25263 | 223968749881 | 1978 | 1978 | [C] |  |
| 71 | Sheqef | moshav | Ashkelon | מועצה אזורית לכיש | 1982 | 310 | 31.51491, 34.93596 | 193954602561 | 1981 | 1981 | [C] |  |
| 72 | Talme Yosef | moshav | Beer Sheva | מועצה אזורית אשכול | 1982 | 310 | 31.20137, 34.36203 | 139170568071 | 1982 | 1982 | [S] | Sinai origin, see Flags |
| 73 | Yuvallim | community settlement | Acre | מועצה אזורית משגב | 1982 | 370 | 32.87861, 35.27019 | 225609753755 | 1982 | 1982 | [S] |  |
| 74 | Amnun | moshav | Safed | מועצה אזורית מבואות החרמון | 1983 | 310 | 32.90464, 35.57264 | 253903756700 | 1983 | 1983 | [S] | Sinai origin, see Flags |
| 75 | Asherat | community settlement | Acre | מועצה אזורית מטה אשר | 1983 | 370 | 32.97172, 35.15686 | 215008764080 | 1983 | 1983 | [S] |  |
| 76 | Har Amasa | kibbutz | Beer Sheva | מועצה אזורית תמר | 1983 | 330 | 31.34347, 35.10203 | 209711583525 | 1983 | 1984 (infobox 1983) | [C] |  |
| 77 | Korazim | other rural settlement | Safed | מועצה אזורית מבואות החרמון | 1983 | 350 | 32.91075, 35.55091 | 251868757370 | 1983 | 1983 | [S] |  |
| 78 | Lotan | kibbutz | Beer Sheva | מועצה אזורית חבל אילות | 1983 | 330 | 29.98779, 35.08716 | 208139433229 | 1983 | 1983 | [S] |  |
| 79 | Massad | community settlement | Kinneret | מועצה אזורית הגליל התחתון | 1983 | 370 | 32.84387, 35.42310 | 239927749921 | 1983 | 1983 | [S] |  |
| 80 | Newe Shalom | other rural settlement | Jerusalem | מועצה אזורית מטה יהודה | 1983 | 350 | 31.81799, 34.97865 | 198078636158 | 1969 | 1969 | [C] | Latrun area, see Flags |
| 81 | Retamim | moshav shitufi | Beer Sheva | מועצה אזורית רמת נגב | 1983 | 320 | 31.05355, 34.68928 | 170283551490 | 1983 | 1983 | [S] |  |
| 82 | Timrat | community settlement | Jezreel | מועצה אזורית עמק יזרעאל | 1983 | 370 | 32.70349, 35.22522 | 221405734332 | 1981 | 1981 | [C] |  |
| 83 | Zuqim | community settlement | Beer Sheva | מועצה אזורית הערבה התיכונה | 1983 | 370 | 30.49126, 35.16633 | 215797489037 | 2004 | - | [C] |  |
| 84 | Ezuz | community settlement | Beer Sheva | מועצה אזורית רמת נגב | 1984 | 370 | 30.79238, 34.47272 | 149421522649 | 1985 | 1956 (infobox 1985) | [C] |  |
| 85 | Hannaton | kibbutz | Jezreel | מועצה אזורית עמק יזרעאל | 1984 | 330 | 32.78383, 35.24496 | 223252743242 | 1984 | 1984 | [S] |  |
| 86 | Karme Yosef | other rural settlement | Ramla | מועצה אזורית גזר | 1984 | 350 | 31.84768, 34.92029 | 192561639463 | 1984 | 1984 | [S] |  |
| 87 | Kefar Weradim | urban-size band, code 180 | Acre | local council | 1984 | 180 | 32.99579, 35.26904 | 225494766750 | 1984 | 1984 | [S] |  |
| 88 | Metar | urban-size band, code 170 | Beer Sheva | local council | 1984 | 170 | 31.32777, 34.93098 | 193429581812 | 1984 | 1984 | [S] |  |
| 89 | Shomeriyya | kibbutz | Beer Sheva | מועצה אזורית בני שמעון | 1984 | 330 | 31.43221, 34.88372 | 188965593404 | 1985 | 1985 | [C] |  |
| 90 | Haluz | community settlement | Acre | מועצה אזורית משגב | 1985 | 370 | 32.95024, 35.31237 | 229548761702 | - | - | [S] CBS only |  |
| 91 | Lehavim | urban-size band, code 180 | Beer Sheva | local council | 1985 | 180 | 31.36906, 34.81251 | 182170586424 | 1983 | - | [C] |  |
| 92 | Shaharut | community settlement | Beer Sheva | מועצה אזורית חבל אילות | 1985 | 370 | 29.90434, 34.99997 | 199709423990 | 1985 | 1985 | [S] |  |
| 93 | Shorashim | moshav shitufi | Acre | מועצה אזורית משגב | 1985 | 320 | 32.89314, 35.25956 | 224614755365 | 1985 | 1985 | [S] |  |
| 94 | Eshhar | community settlement | Acre | מועצה אזורית משגב | 1986 | 370 | 32.88583, 35.30070 | 228464754557 | 1986 | 1986 | [S] |  |
| 95 | Modi'in-Makkabbim-Re'ut | urban-size band, code 140 | Ramla | municipality | 1986 | 140 | 31.89609, 35.00942 | 201007644813 | 1994 | 1985,1987,1993 | [C] | see Flags |
| 96 | Nizzane Sinay | moshav | Beer Sheva | מועצה אזורית רמת נגב | 1986 | 310 | 30.90462, 34.39731 | 142293535143 | 1986 | 1980 (infobox 1977) | [C] | Sinai origin, see Flags |
| 97 | Avtalyon | community settlement | Acre | מועצה אזורית משגב | 1987 | 370 | 32.83642, 35.35175 | 233248749083 | 1987 | 1987 | [S] |  |
| 98 | Gan Ner | urban-size band, code 190 | Jezreel | מועצה אזורית הגלבוע | 1987 | 190 | 32.53193, 35.33812 | 232015715314 | 1987 | 1987 | [S] |  |
| 99 | Merav | kibbutz | Jezreel | מועצה אזורית עמק המעיינות | 1987 | 330 | 32.45283, 35.42212 | 239924706555 | 1987 | 1982 (infobox 1982,1987) | [C] |  |
| 100 | Newe Harif | kibbutz | Beer Sheva | מועצה אזורית חבל אילות | 1987 | 330 | 30.03801, 35.03724 | 203330438802 | 1987 | 1983 | [C] |  |
| 101 | Nofit | urban-size band, code 190 | Haifa | מועצה אזורית זבולון | 1987 | 190 | 32.75933, 35.14563 | 213945740526 | 1987 | 1987 | [S] |  |
| 102 | Tel Te'omim | community settlement | Jezreel | מועצה אזורית עמק המעיינות | 1987 | 370 | 32.44206, 35.49582 | 246857705377 | 1982 | 1982 | [C] |  |
| 103 | Giv'at Ela | community settlement | Jezreel | מועצה אזורית עמק יזרעאל | 1988 | 370 | 32.72148, 35.24540 | 223296736327 | 1988 | 1988 | [S] |  |
| 104 | Karkom | community settlement | Safed | מועצה אזורית מבואות החרמון | 1988 | 370 | 32.92977, 35.60794 | 257195759499 | 1986 | 1986 | [C] |  |
| 105 | Kemehin | moshav | Beer Sheva | מועצה אזורית רמת נגב | 1988 | 310 | 30.91074, 34.43145 | 145562535799 | 1988 | 1988 | [S] |  |
| 106 | Or HaGanuz | moshav shitufi | Safed | מועצה אזורית מרום הגליל | 1989 | 320 | 33.00535, 35.44699 | 242122767835 | 1989 | 1989 | [S] |  |
| 107 | Shani | community settlement | Beer Sheva | none | 1989 | 370 | 31.35590, 35.06994 | 206659584907 | 1982 | 1982 | [C] | see Flags |
| 108 | Avshalom | community settlement | Beer Sheva | מועצה אזורית אשכול | 1990 | 370 | 31.19546, 34.33105 | 136212567439 | 1990 | 1990 | [S] |  |
| 109 | Kefar Hananya | community settlement | Safed | מועצה אזורית מרום הגליל | 1990 | 370 | 32.91500, 35.42330 | 239929757810 | 1977 | 1977 | [C] |  |
| 110 | Giv'at Avni | urban-size band, code 190 | Kinneret | מועצה אזורית הגליל התחתון | 1991 | 190 | 32.77556, 35.43893 | 241426742348 | 1991 | 1991 | [S] |  |
| 111 | Ibbim | institutional settlement | Ashkelon | מועצה אזורית שער הנגב | 1991 | 340 | 31.53372, 34.60973 | 162977604769 | 1992 | 1992 | [C] |  |
| 112 | Shoham | urban-size band, code 160 | Ramla | local council | 1993 | 160 | 31.99766, 34.94521 | 194960656088 | 1993 | 1993 | [S] |  |

## Contemporaneous records read

All are JTA Daily News Bulletin items, opened as scanned PDFs on 2026-10-25 (UTC). "Bulletin date" is the date of the
bulletin; "dateline" is what the item itself prints. JTA is press, so [S]. None gives a founding day.

| # | Bulletin (PDF) | Dateline, page | What the item says (quoted) | Rows | Tag |
|---|---|---|---|---|---|
| J1 | 11 Oct 1977, no. 190, http://pdfs.jta.org/1977/1977-10-11_190.pdf | "TEL AVIV, Oct. 10 (JTA)", pages 2 to 3 | "A new settlement by members of the Labor Zionist Tnuat Hamoshavim was established recently in the Latrun area, a former no-man's-land on the main highway between Jerusalem and the coast. The settlement, named Ruth, established with no fanfare, is the second moshav in the Latrun region. Earlier, 25 families of the Independent Liberal Party moved into temporary dwellings in the region preparatory to establishing a moshav to be called Shilat." | Kefar Rut, Shilat | [S] page image read |
| J2 | 28 Jul 1978, no. 146, http://pdfs.jta.org/1978/1978-07-28_146.pdf | "JERUSALEM (JTA)", page 4 (no date in the dateline) | "The Jewish National Fund afforestation department is planning tree planting for landscaping for the settlements of Shilat and Rut near the Latrun enclave and the new Tel Aviv-Jerusalem highway." | Shilat, Kefar Rut | [S] OCR |
| J3 | 16 Jan 1978, no. 10, http://pdfs.jta.org/1978/1978-01-16_010.pdf | "TEL AVIV (JTA)", page 3 (no date in the dateline) | "Nahal Holit, a para-military settlement in the Rafah salient of northern Sinai, was turned over to civilian control Thursday. It has been re-named Kibbutz Holit ... Holit is the 13th settlement in the Rafah area." The bulletin is a Monday; "Thursday" is 12 January 1978 (derived). This is the Sinai site, not the present kibbutz. | Holit | [S] OCR |
| J4 | 16 Sep 1977, no. 176, http://pdfs.jta.org/1977/1977-09-16_176.pdf | "JERUSALEM (JTA)", page 4 (no date in the dateline) | "The Jewish National Fund is currently preparing the first phase of the infrastructure for the new settlement of Iddan to be completed this year in the Arava near Ein Hatzeva. The JNF will also prepare the land for the establishment of three other settlements in the Arava region to be completed between 1978-1980. They are: Tsofar, Samar II, and Neot Ha-Kikkar II." A plan, not a founding. | Iddan | [S] OCR |
| J5 | 22 Feb 1979, no. 37, http://pdfs.jta.org/1979/1979-02-22_037.pdf | "TEL AVIV, Feb. 21 (JTA)", page 2 (item "Moshav in the Negev", whose text runs on into the next column) | "Two new kibbutzim are also scheduled to be established in the Negev before the end of the year, Telalim (dew) and Retamim (broom bushes)." A plan, not a founding. | Telalim, Retamim | [S] dateline read on the page image; OCR, wording of the quoted sentence from the JTA web page of the same item |
| J6 | 17 Jan 1980, no. 12, http://pdfs.jta.org/1980/1980-01-17_012.pdf | "JERUSALEM, Jan. 16 (JTA)", page 3 | Headline "New Galilee Moshav to Be Established": "The proposed village of Gilon, one of seven villages slated to be built by the farmers' federation". A plan, not a founding. The page image prints "some 350 families" in the sentence "The first Galilee moshav since the State's establishment will house some 350 families in the Galilee's Segev area, Likud MK Pessach Grupper announced here."; the OCR had misread the number, and the JTA web page also gives 350. | Gilon | [S] dateline and number read on the page image; OCR for the quoted words |
| J7 | 18 Jan 1982 report in the bulletin of 19 Jan 1982, no. 12, http://pdfs.jta.org/1982/1982-01-19_012.pdf | "NEW YORK, Jan. 18 (JTA)", page 2 | Headline "Development Underway of at Least 20 New Settlements in Western Negev": "He said that the first settlers have already moved into six moshavim and two kibbutzim in the western Negev. He listed the moshavim as Yesodot Hadarom, Yated, Pri Gan, Yevul, Dekel and Talmai Yafa and the Kibbutzim as Suffa and Holit. These settlements have already 160 families." (Menahem Perlmutter, Jewish Agency Settlement Department.) This shows the settlers were in place by 18 January 1982; it gives no day. "Dekel" and "Talmai Yafa" are JTA's spellings; I read them as Deqel and Talme Yosef (CBS name), which the item does not state. | Yated, Peri Gan, Yevul, Deqel, Talme Yosef, Holit | [S] OCR, names as read |
| J8 | 21 Sep 1983, no. 179, http://pdfs.jta.org/1983/1983-09-21_179.pdf | "NEW YORK, Sept. 20 (JTA)", page 4 | Headline "First Conservative Kibbutz to Be Established in Israel": "The settlement group will spend a year in training at a nearby kibbutz before moving to their own 375-acre site, located in lower Galilee just northwest of Nazareth, to be called Kibbutz Hannaton." A plan; compare English Wikipedia's "On 1 September 1983, Gar'in Noam ... moved to Kfar HaHoresh for training" and "In September 1984, the founders moved into the new kibbutz". | Hannaton | [S] dateline read on the page image; OCR for the quote |
| J9 | 25 Sep 1986, no. 185, http://pdfs.jta.org/1986/1986-09-25_185.pdf | "JERUSALEM, Sept. 24 (JTA)", page 2; the text is under the subhead "New Moshav Dedicated In The Galilee", inside the item "1,500 American Jews Are in Israel on Five UJA Missions" by Yitzhak Rabi | Subhead "New Moshav Dedicated In The Galilee": "Stein and members of the President's Mission attended the dedication ceremony of Moshav Kahal in the mountains of lower Galilee, on the site overlooking the Sea of Galilee." A dedication, not a founding; compare Hebrew Wikipedia "נוסד באוקטובר 1980". | Kahal | [S] dateline, subhead and byline read on the page image; OCR for the quote |

Other items that name a locality of the table in 1977-1993 and say nothing about its founding (read as web text of the item page; not opened as PDF): Avtalyon (22 Jan 1986: "a new settlement, Avtalyon, is to be completely surrounded by greenery", JNF tree planting; compare Hebrew Wikipedia "הממשלה החליטה על הקמת היישוב בדצמבר 1983"), Elifaz (21 Jul 1986, a fish farm "began in 1984 to breed the sea bream"), Telalim and Retamim (27 Feb 1984, swimming pools "in kibbutzim Telalim and Retamim"), Newe Shalom (26 Dec 1984, a Ford Foundation grant; 7 Apr 1987), Lotan (10 Aug 1989, an infiltration "of Kibbutz Lotan"). These show a place existed on the date; they are not founding records.

Context only, not tied to a row: the JTA items of 16 Oct 1980 and 14 May 1980 describe the lookout (mitzpim) programme in general ("small settlements (mitzpim –lookout posts housing 15 to 25 families) ... were quickly established throughout the Galilee", 16 Oct 1980). Items of 11 and 17 July 1979 report a road to "a new lookout post settlement called Mitzpeh" near the village of Mailia; the item does not say which locality it is and I do not identify it. **[GAP]**


## Day and month statements in Wikipedia

These are the founding-step statements the script found (it searched the Hebrew and English articles of all 112 rows
for a month name next to a year of 1977-1993; rows whose only hit was an unrelated later event are left out). The
script missed some, so the list is not complete. All [S]: I did not open the sources Wikipedia cites. Each statement marks a different step (a decision, a
cornerstone, going onto the land, a ceremony, civilian status); the "what it marks" column is my reading of the
sentence, and **none of them is shown to be the day the CBS year follows**. The sentence is copied from the article text
with wiki link markup removed.

| Locality | Date as stated | What the sentence marks (mine) | Sentence | Article, oldid |
|---|---|---|---|---|
| Bet Rimmon | 8 Mar 1977 | going-onto-the-land ceremony | טקס העלייה על הקרקע התקיים ב-8 במרץ 1977, בהשתתפות אלוף פיקוד הצפון דאז רפאל איתן. | he.wikipedia oldid 43685462 |
| Bet Rimmon | 28 Jun 1979 | civilianisation of the Nahal outpost | בית רימון אוזרח ב-28 ביוני 1979. | he.wikipedia oldid 43685462 |
| Kefar Rut | Oct 1977 | settlers (about 20 families) moved into prefabricated houses | באוקטובר 1977, עלו המתיישבים, שכללו כ-20 משפחות מבני מושבי מועצה אזורית מטה יהודה, להתגורר בבתים טרומיים במושב. | he.wikipedia oldid 43968102 |
| Kefar Rut | Dec 1977 | going-onto-the-land ceremony | במקביל הוקמו באזור גם שילת ומתתיהו טקס עלייה על הקרקע התקיים בדצמבר 1977. | he.wikipedia oldid 43968102 |
| Shilat | 16 Aug 1977 | first settlers, 25 families, went onto the land | ראשוני המתיישבים, 25 משפחות, עלו על הקרקע במקום ב-16 באוגוסט 1977. | he.wikipedia oldid 43725343 |
| Shilat | 11 Oct 1977 | dedication ceremony | טקס חנוכת היישוב נערך ב-11 באוקטובר 1977, בנוכחות נציגי הממשלה, התנועה המיישבת, הסוכנות היהודית ומשפחות החברים. | he.wikipedia oldid 43725343 |
| Shilat | Jul 1977 | moshav established by 25 families (English); the Hebrew article gives 16 Aug 1977 for the first settlers | The moshav was established in July 1977 by 25 families on land located near the Palestinian village of Shilta, which was depopulated in 1948. | en.wikipedia oldid 1380467739 |
| Moran | Sep 1977 | a pioneer unit arrived to finish the houses for the other founders | בספטמבר 1977 הגיעה יחידת חלוץ לקיבוץ להשלים את הכנת הבתים לשאר המייסדים. | he.wikipedia oldid 43761740 |
| Lappidot | Jul 1978 | went up to the temporary site | לנקודה הזמנית הם עלו ביולי 1978. | he.wikipedia oldid 42326908 |
| Lappidot | Sep 1980; 1981 | the moshav stood built but unoccupied; they went up to the permanent site in 1981 | בספטמבר 1980 עמד המושב בנוי אך טרם אוכלס והם עלו ליישוב הקבע בשנת 1981. | he.wikipedia oldid 42326908 |
| Lotem | 9 Oct 1978 | going-onto-the-land ceremony | טקס עלייה על הקרקע התקיים ב-9 באוקטובר 1978. | he.wikipedia oldid 42977196 |
| Eshbal | 30 Apr 1979 | Nahal outpost (first called Eshhar) went onto the land | ראשיתה של אשבל בהיאחזות נח"ל, שנקראה בתחילה "אשחר", שעלתה על הקרקע על ידי "גרעין עמק" ב-30 באפריל 1979. | he.wikipedia oldid 42761131 |
| Eshbal | May 1979 | official going-onto-the-land ceremony | הטקס הרשמי של עלייה על הקרקע התקיים במאי 1979. | he.wikipedia oldid 42761131 |
| Mahane Yattir | Aug 1979 | a Gush Emunim core group went up to temporary settlement at the camp | באוגוסט 1979 עלה גרעין התיישבות של גוש אמונים שיועד להתיישב במשטרת לציפר להתיישבות זמנית במחנה יתיר, סמוך לאתר ההתיישבות הראשון שלו, מצודת היערנים. | he.wikipedia oldid 42799448 |
| Mahane Yattir | Oct 1979 | dedication of the moshav that took the name "Bet Yatir" | באוקטובר 1979 נערך טקס חנוכת המושב הרשמי, והוא קיבל את השם "בית יתיר", שמו עד היום. | he.wikipedia oldid 42799448 |
| Lavon | Sep 1980 | manned by its permanent core group | שמו הוסב ל"לבון" על שם פנחס לבון ובספטמבר 1980 הוא אויש על ידי גרעין הקבע שלו מאיחוד הקבוצות והקיבוצים שהתפרנס מטוויית צמר. | he.wikipedia oldid 43220738 |
| Mizpe Netofa | Jun 1979 and Oct 1983 | Nahal settlement (English); the Hebrew article says 1981 and 1983 | Mitzpe Netofa was established originally as a Nahal settlement in June 1979 and was civilianized later in October 1983. | en.wikipedia oldid 1374534534 |
| Gilon | Nov 1980 | went onto the land | היישוב עלה לקרקע בנובמבר 1980 במסגרת תוכנית המצפים בגליל, על ידי תנועת משקי חירות בית"ר, הסוכנות היהודית ושר החקלאות דאז אריאל שרון. | he.wikipedia oldid 35255108 |
| Kahal | Oct 1980 | founded, as part of the lookouts programme | הוא נוסד באוקטובר 1980, במסגרת תוכנית המצפים בגליל, וב-1990 הפך למושב קהילתי. | he.wikipedia oldid 42465117 |
| Kammon | Oct 1981 (land 1980) | founded as a lookout (mitzpe) | היישוב נוסד כ'מצפה' באוקטובר 1981 (עלה לקרקע ב-1980) כחלק מתוכנית המצפים, שמטרתה העיקרית הוגדרה למען "יהוד הגליל". | he.wikipedia oldid 44028385 |
| Tuval | Feb 1980 | populated by a Nahal core group | הוא אוכלס בפברואר 1980 על ידי גרעין נח"ל, בגלל קשיים בגיבוש הגרעין המיישב. | he.wikipedia oldid 43835589 |
| Tuval | Jan 1981 | civilianised | תובל אוזרח בינואר 1981 על ידי חברי תנועת הצופים ותנועת הבונים דרור מאנגליה שקיבלו הכשרה בבית העמק וקבוצה של הבונים מדרום אפריקה. | he.wikipedia oldid 43835589 |
| Ziv'on | late Feb 1980 | Nahal outpost settled after an agreement on removing trespassers | בסוף פברואר 1980 הושג הסדר עם הפולשים על הפינוי, והיאחזות הנח"ל התמקמה במקום. | he.wikipedia oldid 43845456 |
| Ziv'on | Jan 1980 | decision to set up a Nahal outpost at the site (a decision, not a settling) | היישוב יועד להיות קיבוץ של השומר הצעיר, ועד התגבשות גרעין מיישב הוחלט להקים במקום היאחזות נח"ל בינואר 1980. | he.wikipedia oldid 43845456 |
| Kallanit | 29 Oct 1981 | going-onto-the-land ceremony | טקס עלייה על הקרקע התקיים ב-29 באוקטובר 1981. | he.wikipedia oldid 43737594 |
| Kallanit | Dec 1981 | moshav established | היישוב הוקם בדצמבר 1981 כמושב על ידי "הפועל המזרחי" עבור בני המושבים עלמה ודלתון יוצאי העדה הטריפולטאית. | he.wikipedia oldid 43737594 |
| Kallanit | Jan 1978 | the founders settled temporarily, at Giv'at Hananya, from this month | מקימי המושב התיישבו באופן זמני החל מינואר 1978 בגבעת חנניה. | he.wikipedia oldid 43737594 |
| Kokhav Ya'ir | Sep 1981 | founding group of 15 families went onto the land | גרעין המייסדים עלה על הקרקע בספטמבר 1981, מנה 15 משפחות שגרו במגורים זמניים ב"מצפה" ללא כל תשתית מוקדמת של חשמל או מים. | he.wikipedia oldid 43723358 |
| Ravid | Jun 1981 | kibbutz founded | הקיבוץ הוקם על ידי התק"ם ביוני 1981 במסגרת תוכנית המצפים בגליל. | he.wikipedia oldid 43975664 |
| Ravid | Oct 1982 | going-onto-the-land ceremony | באוקטובר 1982, עת התגוררו במקום 10 משפחות, התקיים טקס העלייה על הקרקע. | he.wikipedia oldid 43975664 |
| Yated | 24 Apr 1980 | cornerstone ceremony | טקס הנחת אבן הפינה למושב נערך ב-24 באפריל 1980. | he.wikipedia oldid 42987730 |
| Yevul | 22 Oct 1981 | moshav went onto the land in Hevel Shalom | המושב עלה על הקרקע בחבל שלום ב-22 באוקטובר 1981. | he.wikipedia oldid 42987726 |
| Talme Yosef | May 1982 | after the evacuation of Sinai the new moshav in Hevel Shalom came into being | אחרי פינוי סיני במאי 1982 קם המושב החדש בחבל שלום, והחל בגידול עגבניות לייצוא. | he.wikipedia oldid 42987765 |
| Zurit | Apr 1981 | founded as a lookout by 13 families | מקימי היישוב התגבשו במרכז הארץ ב-1979 במסגרת "גרעין סחלב", ו-13 משפחות הקימו את היישוב כמצפה באפריל 1981. | he.wikipedia oldid 43113132 |
| Deqel | 25 Feb 1981 | cornerstone laid | ב-25 בפברואר 1981 הונחה אבן פינה ליישוב. | he.wikipedia oldid 44092373 |
| Deqel | Apr 1982 | families went onto the land | באפריל 1982 עלו המשפחות על הקרקע. | he.wikipedia oldid 44092373 |
| Deqel | Apr 1982 | moshav founded by the Aguda Shitufit with the Jewish Agency (English) | The moshav was founded in April 1982 by the eponymous Aguda Shitufit (Co-operative Union) with the help of the Jewish Agency. | en.wikipedia oldid 1375608060 |
| Elifaz | Apr 1982 and Jan 1983 | Nahal outpost; civilianised | באפריל 1982 הוקמה במקום היאחזות נח"ל בשם "אליפז" על ידי גרעין קוצן של השומר הצעיר, והיישוב אוזרח בינואר 1983. | he.wikipedia oldid 42418520 |
| Elifaz | Jan 1983 | founded by a group of Nahal members (English) | Elifaz was founded in January 1983 by a group of Nahal members from Jerusalem, Bat Yam, Haifa and Nahariya. | en.wikipedia oldid 1378095573 |
| En Tamar | Aug 1982 | founded by 24 families | היישוב הוקם באוגוסט 1982 על ידי 24 משפחות. | he.wikipedia oldid 42621616 |
| En Tamar | Aug 1982 | established by 24 families (English) | Ein Tamar was established in August 1982 by 24 families. | en.wikipedia oldid 1376033485 |
| Nirit | 19 Jan 1982 | founded | Nirit was founded on January 19, 1982 and was named after an indigenous plant, Ridolfia segetum, growing nearby. | en.wikipedia oldid 1374535486 |
| Qazir | May 1982 | founded as a community settlement | היישוב קציר הוקם במאי 1982 על קרקע של הסוכנות היהודית כיישוב קהילתי שתושביו מאוגדים באגודה שיתופית. | he.wikipedia oldid 42586436 |
| Qoranit | Sep 1978 | founded by a core group | היישוב נוסד בספטמבר 1978 על ידי גרעין של ישראלים מרחבי ישראל שאורגן על ידי האיחוד החקלאי. | he.wikipedia oldid 43714099 |
| Yuvallim | Oct 1982 | going-onto-the-land ceremony | טקס העלייה על הקרקע התקיים באוקטובר 1982. | he.wikipedia oldid 43649498 |
| Har Amasa | 30 Jun 1983 | founded as a kibbutz (English); the Hebrew article says it went onto the land in Aug 1983 | The village was founded as a kibbutz of the United Kibbutz Movement on June 30, 1983. | en.wikipedia oldid 1376033751 |
| Har Amasa | Aug 1983 | went onto the land as a kibbutz of the United Kibbutz Movement (Hebrew) | היישוב עלה על הקרקע באוגוסט 1983 כקיבוץ של התק"ם, ונקרא על שם הר עמשא עליו הוא ממוקם. | he.wikipedia oldid 43901942 |
| Retamim | Apr 1978; early 1980 | ministers' committee decision; core group moved to a temporary caravan camp | על הקמת היישוב הוחלט על ידי ועדת השרים להתיישבות באפריל 1978, ובעקבות זאת עלה גרעין מתיישבים שכלל עולים מאנגליה וישראלים בתחילת 1980 למחנה קרוואנים זמני ליד ביר עסלוג'. | he.wikipedia oldid 42605403 |
| Telalim | Dec 1979 | Telalim moved to its permanent site (stated in the English article of Retamim, not Telalim) | The two groups merged in mid-1979 and moved to a caravan site opposite Park Golda after Tlalim had moved to its permanent site in December 1979. | en.wikipedia oldid 1376043086 |
| Karme Yosef | 28 Jul 1980 | cornerstone ceremony | ב-28 ביולי 1980 הונחה אבן הפינה "לישוב נוסף במערכת המושבות בארץ ישראל", במעמד נציגי ממשלת ישראל והתאחדות האיכרים, נציגי הסוכנות היהודית וההנהלה הציונית, נציגי עיריית רחובות והמועצה האזורית גזר. | he.wikipedia oldid 43762471 |
| Metar | 27 Aug 1979 | founding document | במסמך הייסוד מ-27 באוגוסט 1979 נאמר כי המטרה היא הקמת יישוב עירוני בנחל חברון בשם "עומר ב'". | he.wikipedia oldid 43793779 |
| Metar | 7 Oct 1980 | cornerstone laid | אבן פינה ליישוב הונח בנוכחות השר אריאל שרון ב-7 באוקטובר 1980. | he.wikipedia oldid 43793779 |
| Shomeriyya | Oct 1985 | kibbutz founded | שומריה הוקם באוקטובר 1985. | he.wikipedia oldid 42560622 |
| Shorashim | Nov 1985 | core group went onto the land | בנובמבר 1985 עלה הגרעין על הקרקע באתר שורשים. | he.wikipedia oldid 43737857 |
| Eshhar | 30 Apr 1979 | Nahal outpost "Eshhar" at the site of today's Eshbal | היאחזות נח"ל בשם אשחר, הוקמה באתר בו נמצא היום קיבוץ אשבל, ב־30 באפריל 1979. | he.wikipedia oldid 43255437 |
| Avtalyon | Dec 1983 | government decision to found it | הממשלה החליטה על הקמת היישוב בדצמבר 1983, במסגרת תוכנית המצפים לייהוד הגליל. | he.wikipedia oldid 43528362 |
| Nofit | 9 Nov 1987 | occupancy ceremony for the first 179 families | בשנת 1984 החלו עבודות התשתית: פריצת דרכים והכנת תשתיות תת-קרקעיות (חשמל, מים, טלפון, טלוויזיה מרכזית), באפריל 1986 החלה בניית בתי המגורים וטקס האכלוס ל־179 המשפחות הראשונות התקיים ב־9 בנובמבר 1987. | he.wikipedia oldid 42088842 |
| Tel Te'omim | May 1984 | going-onto-the-land ceremony | טקס העלייה על הקרקע התקיים במאי 1984 בנוכחות שר השיכון, דוד לוי ושר החקלאות, פסח גרופר. | he.wikipedia oldid 43771321 |
| Giv'at Ela | Dec 1983 | government decision to found it | הממשלה החליטה על הקמת היישוב בדצמבר 1983, כיישוב קהילתי ביוזמה פרטית. | he.wikipedia oldid 43107117 |
| Giv'at Ela | Sep 1986 | cornerstone ceremony | טקס הנחת אבן פינה בהשתתפות שר השיכון, דוד לוי, התקיים בספטמבר 1986. | he.wikipedia oldid 43107117 |
| Ibbim | Aug 1979 | a youth village of the Ministry of Education opened at the site (the CBS year is 1991) | באוגוסט 1979 נחנך במקום כפר נוער של משרד החינוך לנוער נושר. | he.wikipedia oldid 43639438 |
| Shoham | Jul 1991 | first lottery for 300 building plots | ביולי 1991 נערכה הגרלה ראשונה לרכישת 300 מגרשים ראשונים לבנייה ביישוב, בה השתתפו 3,000 איש. | he.wikipedia oldid 43920000 |
| Kefar Weradim | 17 Aug 1977 | joint government and Zionist Organization committee decided to set up a team to examine the plan | ב-17 באוגוסט 1977 החליטה ועדה משותפת לממשלה ולהסתדרות הציונית על הקמת צוות לבדיקת התכנית ובספטמבר 1977 אושרה התכנית תוך קביעה שהיישוב יהיה תחת ועד מקומי בתוך מועצת מעלות-תרשיחא. | he.wikipedia oldid 43906840 |

## Rows where the sources disagree [C]

The CBS year against the Hebrew (he) and English (en) Wikipedia year. Each value is as the article prints it in its
category or infobox; the article text, quoted in the previous sections where it dates a step, usually shows what the
other year marks (a Nahal outpost year, a civilian-settling year, a re-founding year). The CBS rule (intro, section
12) takes the last settling year after a long break and the original year after a short one, which explains some of
the gaps; I did not check which case applies row by row. **No row is resolved here.**

| Locality | CBS | he.WP | en.WP | What the sources read say |
|---|---|---|---|---|
| Qesaryya | 1977 | 1977 | 1952 | Hebrew category 1977; the English article "Caesarea (modern town)" has category 1952. CBS 1977. |
| Yad HaShemona | 1978 | 1971 | 1971 | Both articles: founded 1971 by Finnish Christians (Hebrew also: the moshav was set up in 1974). CBS 1978. |
| Bar Yohay | 1979 | 1977 | 1977 | Both articles: 1977. CBS 1979. |
| Mizpe Netofa | 1979 | 1981 | 1981 | Hebrew: Nahal outpost 1981, civilianised 1983; English text: Nahal settlement "in June 1979", civilianised October 1983, category 1981. CBS 1979. |
| Hararit | 1980 | 1982 | 1982 (infobox 1980) | Hebrew and English text: 1980; both categories 1982. CBS 1980. |
| Holit | 1980 | 1982 | 1978 | JTA J3: Nahal Holit became "Kibbutz Holit" in the Rafah salient of northern Sinai on a Thursday before 16 Jan 1978. English Wikipedia: established 1978 in Sinai, "In 1982, the kibbutz was re-established in its current location." The CBS year is 1980. Gvati has a row "נחל חולית", region "פיתחת רפיח", 1977. The present site is a later one. |
| Nizzana (Qehilat Hinuh) | 1980 | 1987 | 1987 | Both articles: the youth village was founded 1986 (Hebrew) or 1987 (English) by Aryeh Eliav. CBS 1980. |
| Pelekh | 1980 | 1982 | 1982 | Hebrew: Nahal core group 1980, went onto the land 1983; English: 1982. CBS 1980. |
| Hosha'aya | 1981 | 1980 | 1980 | Hebrew Wikipedia: a Nahal outpost in 1980, civilianised three years later; the first 10 permanent families in 1983. English: "founded in 1981", category 1980. CBS 1981. |
| Kallanit | 1981 | 1982 | 1982 | Both categories 1982; Hebrew text: ceremony 29 Oct 1981, "הוקם בדצמבר 1981". CBS 1981. |
| Kokhav Ya'ir | 1981 | 1986 | 1986 | Hebrew Wikipedia: founding group went onto the land in September 1981. English Wikipedia: "established in 1981 by 15 families living in temporary quarters. Two years later, work began on infrastructure for a permanent town. In 1986, 550 families moved into permanent housing and the town was officially founded." CBS 1981; the English category year is 1986. |
| Moreshet | 1981 | 1996 | 1996 | English: "established in 1996 with support from the Amana movement"; Hebrew: a core group of the 1970s, and in March 1980 a Ministry of the Interior official was quoted that it had 90 families and 30 prefabricated units. CBS 1981. |
| Yated | 1981 | 1980 | 1982 | Hebrew Wikipedia: "המושב הוקם בשנת 1980" and a cornerstone on 24 April 1980; English: "founded in 1982 by a Gar'in who intended to settle in Sinai". CBS 1981. JTA J7 lists Yated among settlements with settlers on 18 Jan 1982. |
| Elifaz | 1982 | 1983 | 1983 | Hebrew: Nahal outpost in April 1982, civilianised January 1983; English: "founded in January 1983". CBS 1982. |
| Harish | 1982 | 1982 | 1982 (infobox 1980) | Hebrew Wikipedia: "נוסדה בשנת 1982 כחלק מתוכנית יישובי הכוכבים" and a Nahal-based kibbutz of 1982 that dwindled to 1993; English Wikipedia infobox year 1980, text 1982. CBS 1982. Town status 2022 (Hebrew). |
| Ne'ot Smadar | 1982 | 1989 | 1989 | Hebrew and English Wikipedia: the Shizafon Nahal outpost went onto the land in 1980 (Hebrew: September 1980), was nearly abandoned by 1988, and Ne'ot Smadar settled in 1989. CBS year 1982. |
| Nehusha | 1982 | 1955 | 1955 | English: "Although the village collapsed in 1968, it was re-established in 1981"; infobox 1955. Hebrew: re-founded in 1981 as a workers' moshav. CBS 1982. |
| Nirit | 1982 | 1981 | 1982 | Hebrew 1981; English "founded on January 19, 1982" (the English year equals the CBS year). CBS 1982. |
| Qoranit | 1982 | 1978 | 1978 | Hebrew: founded in September 1978 by a core group organised by the Agricultural Union; English: 1978 as a moshav shitufi. CBS 1982. |
| Sheqef | 1982 | 1981 | 1981 | Both articles: 1981. CBS 1982. |
| Har Amasa | 1983 | 1983 | 1984 (infobox 1983) | English text: "June 30, 1983"; English category 1984; Hebrew: went onto the land in August 1983. CBS 1983. |
| Newe Shalom | 1983 | 1969 | 1969 | Both articles: founded 1969 by a Dominican priest; English: "The first five families, four Jewish and one Palestinian settled there in 1978"; early members from November 1980. CBS 1983. |
| Timrat | 1983 | 1981 | 1981 | Both: founded 1981 (Hebrew: "בהתאם להחלטת ועדת השרים להתיישבות ממרץ 1980", per the ministers' settlement committee decision of March 1980). CBS 1983. |
| Zuqim | 1983 | 2004 | - | Hebrew category 2004; no English article read. CBS 1983. |
| Ezuz | 1984 | 1985 | 1956 (infobox 1985) | English infobox 1985 with category 1956: "re-established as Ezuz in 1985". CBS 1984. |
| Shomeriyya | 1984 | 1985 | 1985 | Both categories 1985; Hebrew text: "הוקם באוקטובר 1985". CBS 1984. |
| Lehavim | 1985 | 1983 | - | Hebrew category 1983; English article not matched. Hebrew: declared a local council in 1988. CBS 1985. |
| Modi'in-Makkabbim-Re'ut | 1986 | 1994 | 1985,1987,1993 | English infobox: 1985 (Maccabim), 1987 (Re'ut), 1993 (Modi'in); Hebrew category 1994. CBS 1986 follows the merger rule of the CBS intro, section 12 (oldest of the merged localities), which I did not trace to Maccabim or Re'ut. |
| Nizzane Sinay | 1986 | 1986 | 1980 (infobox 1977) | Hebrew: "היישוב המקורי הוקם בשנת 1977 כהיאחזות נח"ל" (the original settlement was founded in 1977 as a Nahal outpost) and category 1986; English category 1980, infobox 1977; English text: "moved to its present site in 1986". CBS 1986. |
| Merav | 1987 | 1987 | 1982 (infobox 1982,1987) | Both: Nahal settlement on Mount Malkishua in 1982, moved to its present place in 1987. CBS 1987. |
| Newe Harif | 1987 | 1987 | 1983 | English: founded 1983 by city dwellers; Hebrew: 1987. CBS 1987. |
| Tel Te'omim | 1987 | 1982 | 1982 | Both 1982; Hebrew text: going-onto-the-land ceremony May 1984. CBS 1987. |
| Karkom | 1988 | 1986 | 1986 | Both categories 1986. CBS 1988. |
| Shani | 1989 | 1982 | 1982 | Hebrew category 1982; English article "Livne" (same place, 9 m from the CBS point) 1982; English text: "residents moving into permanent housing in 1989" (the English text's 1989 equals the CBS year). CBS 1989. |
| Kefar Hananya | 1990 | 1977 | 1977 | Both articles: founded 1977 "as a moshav of the movement Hapoel HaMizrachi". CBS 1990. |
| Ibbim | 1991 | 1992 | 1992 | Both articles: the student village 1992; Hebrew: a youth village opened in August 1979. CBS 1991. |

## Flags

Recorded factually, for the owner's sensitivity review. No judgement is made here.

- **Latrun area.** JTA J1 (10 October 1977) calls the area of Kefar Rut and Shilat "a former no-man's-land on the main
  highway between Jerusalem and the coast". Hebrew Wikipedia says Shilat was meant, by a ministers' settlement
  committee decision after the General Assembly resolution of December 1975, "לעבות ביישובים את הקו הירוק" (to thicken
  settlement along the Green Line), and stands where the Arab village of Shilta stood until 1948; English Wikipedia
  puts Kefar Rut in "an area that had become a no-man's land between Israel and Jordanian-controlled West Bank at the end of
  the 1948 Arab–Israeli War". I read no map source that says which side of the 1949 line the Kefar Rut or Shilat
  site was on. **[GAP]** Four of the Wikipedia revisions this note already cites say more, all [S]. Hebrew Wikipedia on
  Kefar Rut (oldid 43968102): "המושב הוקם על גבול הקו הירוק, במה שנחשב לשטח ההפקר בין ישראל לירדן עד 1967" (the moshav was set up on the Green Line border, in what was considered the no-man's-land
  between Israel and Jordan until 1967). Hebrew Wikipedia on Shilat (oldid 43725343): "ב-2005 טענו חברי המושב שאזור התעשייה נמצא מעבר לקו הירוק, בקרקע שהייתה חלק משטח ההפקר של מובלעת לטרון עד 1967, ולכן חוקי הבנייה של מדינת ישראל לא חלים עליו." (in 2005 the moshav's members claimed
  that the industrial zone is beyond the Green Line, on land that was part of the Latrun enclave's no-man's-land until 1967, so
  Israeli building law does not apply). English Wikipedia on Shilat (oldid 1380467739): "According to the ARIJ, after the Six-Day War in 1967, Israel expropriated 781 dunams of land from the Palestinian village of Saffa for the construction of Shilat." Newe Shalom (Wahat al-Salam) is in the
  same part of the Jerusalem sub-district in the CBS file; Hebrew Wikipedia (oldid 44104284), section "סטטוס טריטוריאלי": "נווה שלום ממוקם ב"שטח הפקר", שהיה אזור מפורז שלא בשליטתן של מדינת ישראל או ירדן טרם 1967. ישראל מעולם לא סיפחה רשמית שטח זה."
  (Newe Shalom is located in "no-man's-land", a demilitarised area not under the control of Israel or Jordan before 1967;
  Israel never formally annexed it). These are claims in Wikipedia text, not a map check, so the position of each site against
  the 1949 line stays open. **[GAP]**
- **Shani.** Hebrew Wikipedia: "בהחלטת ממשלה מאפריל 1984 נקבע כי היישוב יקבל שירותים ממועצה אזורית הר חברון, למרות שרק
  כ־100 דונם מתוך 430 עליהם הוא משתרע נמצאים מעבר לקו הירוק" (by a government decision of April 1984 the locality gets
  its services from the Mount Hebron regional council, although only about 100 of the 430 dunams on which it extends
  are beyond the Green Line). English Wikipedia on the same place, article "Livne" (oldid 1376045741): "Located in the southern Judaean Mountains, straddling the Green Line and therefore partly in Israel and partly in the West Bank". The CBS file lists it in the Beer Sheva subdistrict. [S]
- **Mahane Yattir.** Hebrew Wikipedia ties its history to a Gush Emunim settlement core of August 1979 for the Mount
  Hebron area ("גרעיני הר חברון"); the CBS file lists the camp in the Beer Sheva subdistrict. [S]
- **Modi'in-Maccabim-Re'ut.** Its CBS row (Ramla subdistrict, municipality) merges localities (CBS intro, section 12).
  English Wikipedia (oldid 1381718361): "A small part of the city (the Maccabim neighborhood) is not recognized by the European Union (EU) as being in Israel, as it lies in what the 1949 Armistice Agreement with Jordan left as a no man's land". [S] I did not check where Maccabim and Re'ut lie against the Green Line on a map. **[GAP]**
- **Sinai origin (CBS year may follow a re-settling inside the lines).** Holit (JTA J3 and J7; Gvati row "נחל חולית",
  region "פיתחת רפיח"); Netiv HaAsara (Hebrew: set up "כהתיישבות בחבל ימית" and rebuilt in 1982 at its present place);
  Talme Yosef (English: "established in 1982 by former residents of Talmei Yosef, an Israeli settlement in Sinai");
  Peri Gan (English: "former residents of Pri'el, an Israeli settlement in Sinai"); En HaBesor (Hebrew: founded 1982
  by "מפונים מחבל ימית שבסיני", evacuees from the Yamit area in Sinai, from named moshavim); Nizzane Sinay (Hebrew: the
  original settlement was a Nahal outpost of 1977); Amnun (English: "for evacuees of former Israeli settlements in
  Sinai ... and residents of neighboring moshavim"); Yated (English: a core group "who intended to settle in Sinai");
  Deqel (Hebrew: after the peace treaty its core group asked to settle in Hevel Shalom). All [S]. Every row of the table, these
  included, is outside the CBS Judea and Samaria district and the Golan subdistrict; that is a CBS district test, not a Green
  Line test (see the Latrun area, Shani, Mahane Yattir and Modi'in-Maccabim-Re'ut flags above).
- **Re-founded site.** Nehusha: English "re-established in 1981" after a 1955 founding (infobox 1955), Hebrew "הוקמה
  נחושה מחדש" in 1981; CBS 1982. [S]
- **Arab locality named as a neighbour.** Several articles name a depopulated Arab village near the site (for example
  Hebrew Wikipedia for Shilat, "במקום בו שכן עד 1948 הכפר הערבי שילתא", and English for Allon HaGalil, Tefahot, Kallanit,
  Nataf, Livnim, Gan Ner, Manof). They are reported here only as what the article says; this note did not check
  any of them against another source.

## Rows left out of this note

- **Territories and the Golan** (92 + 11 rows with a CBS year of 1977-1993): outside this note's topic (the CBS district
  "אזור יהודה ושומרון" and subdistrict "גולן"). Counts from the CBS file, opened 2026-10-25; no judgement is made here.
  The file has no Gaza Strip or Sinai rows.
- **Localities with an empty CBS year of founding** (398 rows). Among them are Hura, Kuseife, Laqye, Ar'ara and Rahat
  (all code 250 to 270 in the file) and Tel Sheva. Whether any of them was founded between 1977 and 1993 is not
  stated by the file, so none is in the table and I make no claim about any of them. **[GAP]**
- **Caravan sites and lookout outposts that never became a locality of the file** are outside the topic.

## What was not done

- **No founding day from a contemporaneous or official record for any row.** The JTA items read give the year or a
  period (J1, J7). The Knesset (fs.knesset.gov.il answers from this box), the Israel Government Year Book, the Official
  Gazette (Reshumot), Davar, Maariv, Hamashkif, the Jewish Agency and Settlement Division records and the minutes of
  the ministers' settlement committee were not searched or not reached: the Knesset site has no search by locality
  and I did not browse its 1977-93 plenum minutes, and the National Library, State Archives, gov.il and mfa.gov.il
  returned HTTP 403 to `curl` from this box on 2026-10-25. **[GAP]**
- **The day of going onto the land versus other steps** is stated only where a Wikipedia article says so (see "Day and
  month statements"). Where it says nothing, the row's date has no stated meaning. **[GAP]**
- **Wikipedia citations unopened.** The Hebrew articles cite Davar, Maariv, Nahal publications and Gush Emunim
  histories; none was opened.
- **Coordinates are not checked against a second official source** (Survey of Israel or GeoNames). The English
  Wikipedia cross-check is a consistency test of my reading of the CBS field, not a second source. **[GAP]**
- **Membership rests on the CBS year.** A locality whose CBS year is outside 1977-1993 but whose first outpost falls
  inside the window (or the reverse) is not in the table, except where a row's sources named one. Examples in the
  table: Holit, Nizzane Sinay, Eshhar (Hebrew: a Nahal outpost of 30 April 1979 at the site of today's Eshbal).
  **[GAP]**
- **Town-level history (Harish, Shoham, Metar, Lehavim, Kefar Weradim, Kokhav Ya'ir, Qesaryya, Modi'in-Maccabim-Re'ut)**
  was read only as far as the Wikipedia statements above; no municipal, CBS-history or Knesset record was opened. **[GAP]**


## Searches (every negative record)

All run 2026-10-25 UTC.

| Row | Query / method | Where | Result |
|---|---|---|---|
| S1 | CBS file, column "שנת ייסוד" 1977 to 1993 | bycode2024.xlsx | 215 rows; minus district "אזור יהודה ושומרון" (92) and subdistrict "גולן" (11) = 112 |
| S2 | Name of each of the 112 rows, nikud stripped, against the name column of the 711 appendix rows parsed from https://benyehuda.org/read/14684; and every appendix row with a date ending 77 to 93 | Gvati appendix | 4 of the 112 matched by name and site (Moran, Nahal Bet Rimmon, Shilat, each printed "1977"; Kefar Rut, printed "77."), a fifth (Nahal Holit) by name only. Appendix rows dated 1977 that are not in the table: Migdal Oz, Nahal Almog, Naaran (Judea and Samaria rows), Nir Avraham (Rafah Approaches) and Nahal Holit (name match only, the Sinai site). No appendix row is dated 1978 to 1993 except the urban-colony row "מוצא (תחתית)" 1984; Degania Alef is printed "1.11.90", which I read as a misprint, not a row of this list. |
| S3 | `titles=<Hebrew name>` with `redirects=1`, then the same with the suffixes "(מושב)", "(קיבוץ)", "(יישוב)", "(יישוב קהילתי)", "(עיר)" until the article carried the template for the CBS symbol | he.wikipedia.org MediaWiki API | 109 of 112 rows have an article carrying the CBS symbol (Shomeriyya was added by hand as "שומריה (קיבוץ)", the title the first search missed). No Hebrew article was found for Hilla, Haluz and Zviyya; none is read for them. |
| S4 | `langlinks` to English from each Hebrew article | en.wikipedia.org | 105 of 112 rows have an English article by that link; the rest have none read. |
| S5 | For each of the 112 rows, the JTA archive search with the English name (the CBS spelling, apostrophes removed) and, as a second query, a spelling variant (Kefar to Kfar, Newe to Neve, Q to K, Z to Tz); first results page each; every result dated 1977 to 1993 opened and its text searched for the name or a variant as a whole word | https://www.jta.org/?s=<query>&post_type=archive | 125 queries, 1139 result links, 261 of them dated 1977 to 1993, 51 of those containing a searched name. The containing items were read (section "Contemporaneous records read"); most were about a person or word with the same name (for example the Attorney General Yosef Harish, the Modiin research centre, the Tefahot mortgage bank). Per-row counts follow. |
| S6 | Concept queries, first two results pages each: "Yamit evacuees new moshavim Negev", "Galilee lookout settlement first families moved", "Nahal settlement turned over to civilians", "new kibbutz established Negev Nahal", "Neve Shalom Arab Jewish village Latrun", "new settlement dedicated Galilee Misgav", "Harish new town Wadi Ara", "Caesarea new settlement", "new community settlement established Galilee mitzpe", "Misgav regional council new settlements" | JTA archive search, same URL form | Results dated 1977 to 1993: 0, 2, 3, 0, 0, 4, 6, 2, 0, 7. Read for the table: the 16 Jan 1978 Holit item (J3) and the 25 Sep 1986 Kahal item (J9). The rest were not about a locality of the table or named it without a founding. |
| S7 | Reachability from this box with `curl` | fs.knesset.gov.il, knesset.gov.il, archive.org | HTTP 200; no locality search used. |
| S8 | Reachability from this box with `curl` | gov.il, mfa.gov.il, nli.org.il, jpress.org.il, misgav.org.il | HTTP 403 for each. |

S5 per row (query 1 / query 2: results returned, of which dated 1977-1993, of which containing the name). Search URL form: `https://www.jta.org/?s=<query with + for spaces>&post_type=archive`.

| Locality | Query 1 | Query 2 |
|---|---|---|
| Bet Rimmon | "Bet Rimmon": 10 / 0 / 0 | - |
| Kefar Rut | "Kefar Rut": 10 / 5 / 0 | "Kfar Rut": 10 / 2 / 0 |
| Moran | "Moran": 10 / 1 / 1 | - |
| Qesaryya | "Qesaryya": 0 / 0 / 0 | "Kesaryya": 1 / 0 / 0 |
| Shilat | "Shilat": 10 / 2 / 2 | - |
| Lappidot | "Lappidot": 10 / 4 / 0 | - |
| Lotem | "Lotem": 10 / 2 / 1 | - |
| Yad HaShemona | "Yad HaShemona": 10 / 2 / 0 | - |
| Bar Yohay | "Bar Yohay": 10 / 2 / 0 | - |
| Eshbal | "Eshbal": 10 / 0 / 0 | - |
| Kelil | "Kelil": 10 / 1 / 0 | - |
| Mahane Yattir | "Mahane Yattir": 10 / 5 / 0 | - |
| Mattat | "Mattat": 10 / 4 / 0 | - |
| Mizpe Netofa | "Mizpe Netofa": 0 / 0 / 0 | - |
| Sappir | "Sappir": 10 / 0 / 0 | - |
| Zviyya | "Zviyya": 10 / 7 / 0 | "Tzviyya": 9 / 3 / 0 |
| Abbirim | "Abbirim": 9 / 2 / 0 | - |
| Adi | "Adi": 10 / 1 / 1 | - |
| Allon HaGalil | "Allon HaGalil": 10 / 2 / 0 | - |
| Amuqqa | "Amuqqa": 6 / 0 / 0 | - |
| Gilon | "Gilon": 10 / 1 / 1 | - |
| Gitta | "Gitta": 10 / 1 / 1 | - |
| Gornot HaGalil | "Gornot HaGalil": 3 / 2 / 0 | - |
| Hararit | "Hararit": 10 / 1 / 0 | - |
| Harashim | "Harashim": 10 / 2 / 0 | - |
| Hilla | "Hilla": 10 / 2 / 2 | - |
| Holit | "Holit": 10 / 3 / 2 | - |
| Iddan | "Iddan": 10 / 1 / 1 | - |
| Kahal | "Kahal": 10 / 2 / 1 | - |
| Kammon | "Kammon": 10 / 0 / 0 | - |
| Keramim | "Keramim": 10 / 4 / 0 | - |
| Lavon | "Lavon": 10 / 0 / 0 | - |
| Manof | "Manof": 10 / 0 / 0 | - |
| Manot | "Manot": 10 / 0 / 0 | - |
| Mikhmannim | "Mikhmannim": 1 / 1 / 0 | - |
| Nizzana (Qehilat Hinuh) | "Nizzana": 10 / 0 / 0 | - |
| Pelekh | "Pelekh": 10 / 1 / 0 | - |
| Qaddarim | "Qaddarim": 10 / 8 / 0 | "Kaddarim": 10 / 0 / 0 |
| Shekhanya | "Shekhanya": 7 / 1 / 0 | - |
| Tal-El | "Tal-El": 10 / 9 / 0 | - |
| Tefahot | "Tefahot": 10 / 2 / 1 | - |
| Telalim | "Telalim": 10 / 5 / 2 | - |
| Tuval | "Tuval": 10 / 0 / 0 | - |
| Ziv'on | "Zivon": 10 / 0 / 0 | "Tzivon": 10 / 4 / 0 |
| Hosha'aya | "Hoshaaya": 10 / 3 / 0 | - |
| Kallanit | "Kallanit": 10 / 1 / 0 | - |
| Kokhav Ya'ir | "Kokhav Yair": 10 / 6 / 0 | - |
| Mizpe Aviv | "Mizpe Aviv": 10 / 2 / 0 | - |
| Moreshet | "Moreshet": 10 / 2 / 2 | - |
| Peri Gan | "Peri Gan": 10 / 1 / 0 | - |
| Raqqefet | "Raqqefet": 2 / 0 / 0 | - |
| Ravid | "Ravid": 10 / 3 / 3 | - |
| Sede Avraham | "Sede Avraham": 10 / 4 / 0 | - |
| Yated | "Yated": 10 / 3 / 1 | - |
| Yevul | "Yevul": 5 / 5 / 1 | - |
| Zurit | "Zurit": 10 / 1 / 0 | "Tzurit": 10 / 6 / 0 |
| Deqel | "Deqel": 10 / 8 / 0 | - |
| Elifaz | "Elifaz": 10 / 2 / 1 | - |
| En HaBesor | "En HaBesor": 10 / 0 / 0 | - |
| En Tamar | "En Tamar": 10 / 6 / 0 | - |
| Harduf | "Harduf": 10 / 2 / 0 | - |
| Harish | "Harish": 10 / 9 / 9 | - |
| Livnim | "Livnim": 10 / 0 / 0 | - |
| Nataf | "Nataf": 10 / 1 / 0 | - |
| Ne'ot Smadar | "Neot Smadar": 10 / 6 / 0 | - |
| Nehusha | "Nehusha": 10 / 0 / 0 | - |
| Netiv HaAsara | "Netiv HaAsara": 10 / 1 / 0 | - |
| Nirit | "Nirit": 10 / 1 / 0 | - |
| Qazir | "Qazir": 10 / 9 / 0 | "Kazir": 10 / 3 / 0 |
| Qoranit | "Qoranit": 10 / 0 / 0 | "Koranit": 10 / 0 / 0 |
| Sheqef | "Sheqef": 10 / 5 / 0 | - |
| Talme Yosef | "Talme Yosef": 10 / 6 / 0 | - |
| Yuvallim | "Yuvallim": 4 / 2 / 0 | - |
| Amnun | "Amnun": 10 / 2 / 1 | - |
| Asherat | "Asherat": 10 / 0 / 0 | - |
| Har Amasa | "Har Amasa": 10 / 2 / 0 | - |
| Korazim | "Korazim": 10 / 1 / 1 | - |
| Lotan | "Lotan": 10 / 5 / 5 | - |
| Massad | "Massad": 10 / 1 / 0 | - |
| Newe Shalom | "Newe Shalom": 10 / 3 / 0 | "Neve Shalom": 10 / 1 / 1 |
| Retamim | "Retamim": 10 / 4 / 2 | - |
| Timrat | "Timrat": 10 / 0 / 0 | - |
| Zuqim | "Zuqim": 2 / 0 / 0 | "Tzuqim": 10 / 1 / 0 |
| Ezuz | "Ezuz": 4 / 0 / 0 | - |
| Hannaton | "Hannaton": 10 / 1 / 1 | - |
| Karme Yosef | "Karme Yosef": 10 / 2 / 0 | - |
| Kefar Weradim | "Kefar Weradim": 10 / 0 / 0 | "Kfar Weradim": 10 / 0 / 0 |
| Metar | "Metar": 10 / 1 / 0 | - |
| Shomeriyya | "Shomeriyya": 0 / 0 / 0 | - |
| Haluz | "Haluz": 10 / 2 / 0 | - |
| Lehavim | "Lehavim": 10 / 0 / 0 | - |
| Shaharut | "Shaharut": 10 / 4 / 0 | - |
| Shorashim | "Shorashim": 10 / 5 / 5 | - |
| Eshhar | "Eshhar": 10 / 1 / 0 | - |
| Modi'in-Makkabbim-Re'ut | "Modiin-Makkabbim-Reut": 0 / 0 / 0 | - |
| Nizzane Sinay | "Nizzane Sinay": 10 / 3 / 0 | - |
| Avtalyon | "Avtalyon": 9 / 2 / 1 | - |
| Gan Ner | "Gan Ner": 10 / 2 / 0 | - |
| Merav | "Merav": 10 / 1 / 0 | - |
| Newe Harif | "Newe Harif": 10 / 3 / 0 | "Neve Harif": 10 / 4 / 0 |
| Nofit | "Nofit": 10 / 1 / 0 | - |
| Tel Te'omim | "Tel Teomim": 10 / 4 / 0 | - |
| Giv'at Ela | "Givat Ela": 10 / 5 / 0 | - |
| Karkom | "Karkom": 10 / 2 / 0 | - |
| Kemehin | "Kemehin": 10 / 0 / 0 | - |
| Or HaGanuz | "Or HaGanuz": 10 / 1 / 0 | - |
| Shani | "Shani": 10 / 1 / 1 | - |
| Avshalom | "Avshalom": 10 / 0 / 0 | - |
| Kefar Hananya | "Kefar Hananya": 10 / 0 / 0 | "Kfar Hananya": 10 / 0 / 0 |
| Giv'at Avni | "Givat Avni": 10 / 2 / 0 | - |
| Ibbim | "Ibbim": 7 / 1 / 0 | - |
| Shoham | "Shoham": 10 / 0 / 0 | - |

## Sources and revisions

CBS localities file 2024; CBS intro (December 2025); CBS "Local authorities in Israel 2009" list 03; Gvati vol. 2
appendix; JTA bulletins J1 to J9. All opened 2026-10-25 at the URLs above. Wikipedia revisions (current at the fetch; URL form
`https://he.wikipedia.org/w/index.php?oldid=<n>`, `https://en.wikipedia.org/w/index.php?oldid=<n>`):

| # | Locality | Hebrew article (oldid) | English article (oldid) |
|---|---|---|---|
| 1 | Bet Rimmon | בית רימון (43685462) | Beit Rimon (1374528763) |
| 2 | Kefar Rut | כפר רות (43968102) | Kfar Ruth (1380204185) |
| 3 | Moran | מורן (קיבוץ) (43761740) | Moran, Israel (1374534639) |
| 4 | Qesaryya | קיסריה (יישוב) (43685589) | Caesarea (modern town) (1380466986) |
| 5 | Shilat | שילת (43725343) | Shilat (1380467739) |
| 6 | Lappidot | לפידות (42326908) | Lapidot (1374533265) |
| 7 | Lotem | לוטם (יישוב) (42977196) | Lotem (1374533368) |
| 8 | Yad HaShemona | יד השמונה (43348179) | Yad HaShmona (1374537979) |
| 9 | Bar Yohay | בר יוחאי (יישוב) (43176672) | Bar Yohai (1374899279) |
| 10 | Eshbal | אשבל (קיבוץ) (42761131) | Eshbal (1374529961) |
| 11 | Kelil | כליל (43280452) | Klil (1374533102) |
| 12 | Mahane Yattir | מחנה יתיר (42799448) | Mahane Yatir (1376045195) |
| 13 | Mattat | מתת (44077136) | Mattat (1374533861) |
| 14 | Mizpe Netofa | מצפה נטופה (43319372) | Mitzpe Netofa (1374534534) |
| 15 | Sappir | ספיר (יישוב) (43879313) | Sapir, Israel (1376048724) |
| 16 | Zviyya | none read | none read |
| 17 | Abbirim | אבירים (41159917) | Abirim (1374527192) |
| 18 | Adi | עדי (44038971) | Adi, Israel (1374527448) |
| 19 | Allon HaGalil | אלון הגליל (43737576) | Alon HaGalil (1374527719) |
| 20 | Amuqqa | עמוקה (43790236) | Amuka, Israel (1374898534) |
| 21 | Gilon | גילון (35255108) | Gilon (1374530590) |
| 22 | Gitta | גיתה (יישוב) (41157437) | Gita, Israel (1374530660) |
| 23 | Gornot HaGalil | גורנות הגליל (41157435) | Gornot HaGalil (1374531014) |
| 24 | Hararit | הררית (43337844) | Hararit (1374531387) |
| 25 | Harashim | חרשים (43761736) | Harashim (1374531402) |
| 26 | Hilla | none read | none read |
| 27 | Holit | חולית (קיבוץ) (43143139) | Holit (1375608090) |
| 28 | Iddan | עידן (מושב) (44062398) | Idan, Israel (1378137910) |
| 29 | Kahal | כחל (42465117) | Kahal (1375021920) |
| 30 | Kammon | כמון (יישוב) (44028385) | Kamon, Israel (1374532064) |
| 31 | Keramim | כרמים (43790194) | Kramim (1375256359) |
| 32 | Lavon | לבון (43220738) | Lavon, Israel (1374533282) |
| 33 | Manof | מנוף (יישוב) (41299852) | Manof (1374533691) |
| 34 | Manot | מנות (42367009) | Manot, Israel (1374533699) |
| 35 | Mikhmannim | מכמנים (41157635) | Mikhmanim (1374534329) |
| 36 | Nizzana (Qehilat Hinuh) | ניצנה (43944606) | Nitzana, Israel (1376039933) |
| 37 | Pelekh | פלך (קיבוץ) (44031478) | Pelekh (1374535889) |
| 38 | Qaddarim | קדרים (43461387) | none read |
| 39 | Shekhanya | שכניה (44045413) | Shekhanya (1374537046) |
| 40 | Tal-El | טל-אל (43761741) | Tal-El (1374537338) |
| 41 | Tefahot | טפחות (מושב) (42326360) | Tefahot (1375327795) |
| 42 | Telalim | טללים (43700382) | Tlalim (1376041425) |
| 43 | Tuval | תובל (43835589) | Tuval (1374537655) |
| 44 | Ziv'on | צבעון (43845456) | Tziv'on (1374537714) |
| 45 | Hosha'aya | הושעיה (42595162) | Hoshaya (1374531784) |
| 46 | Kallanit | כלנית (יישוב) (43737594) | Kalanit (1375015131) |
| 47 | Kokhav Ya'ir | כוכב יאיר צור יגאל (43723358) | Kokhav Ya'ir–Tsur Yig'al (1380763516) |
| 48 | Mizpe Aviv | מצפה אבי"ב (41796266) | Mitzpe Aviv (1374534478) |
| 49 | Moreshet | מורשת (יישוב קהילתי) (44083399) | Moreshet (1374534653) |
| 50 | Peri Gan | פרי גן (43834611) | Pri Gan (1376006385) |
| 51 | Raqqefet | רקפת (יישוב) (43737856) | Rakefet (1374536030) |
| 52 | Ravid | רביד (43975664) | Ravid (1374536213) |
| 53 | Sede Avraham | שדי אברהם (42987758) | Sdei Avraham (1376007543) |
| 54 | Yated | יתד (42987730) | Yated, Israel (1376011081) |
| 55 | Yevul | יבול (מושב) (42987726) | Yevul (1376013950) |
| 56 | Zurit | צורית (43113132) | none read |
| 57 | Deqel | דקל (מושב) (44092373) | Dekel (1375608060) |
| 58 | Elifaz | אליפז (קיבוץ) (42418520) | Elifaz, Israel (1378095573) |
| 59 | En HaBesor | עין הבשור (43000816) | Ein HaBesor (1375755828) |
| 60 | En Tamar | עין תמר (42621616) | Ein Tamar (1376033485) |
| 61 | Harduf | הרדוף (קיבוץ) (44038973) | Harduf (1374531412) |
| 62 | Harish | חריש (עיר) (43930681) | Harish, Israel (1380230949) |
| 63 | Livnim | ליבנים (43790231) | Livnim (1375016931) |
| 64 | Nataf | נטף (43761607) | Nataf (1378111313) |
| 65 | Ne'ot Smadar | נאות סמדר (43995612) | Neot Smadar (1376814811) |
| 66 | Nehusha | נחושה (44011711) | Nehusha (1379795818) |
| 67 | Netiv HaAsara | נתיב העשרה (43725345) | Netiv HaAsara (1379168185) |
| 68 | Nirit | נירית (43685472) | Nirit (1374535486) |
| 69 | Qazir | קציר (יישוב) (42586436) | Katzir (1374532186) |
| 70 | Qoranit | קורנית (יישוב) (43714099) | Koranit (1374533161) |
| 71 | Sheqef | שקף (43801200) | Shekef (1375074141) |
| 72 | Talme Yosef | תלמי יוסף (42987765) | Talmei Yosef (1376009753) |
| 73 | Yuvallim | יובלים (43649498) | Yuvalim (1374586018) |
| 74 | Amnun | אמנון (מושב) (43843920) | Amnun (1375019661) |
| 75 | Asherat | אשרת (יישוב) (41238620) | Oshrat (1374535768) |
| 76 | Har Amasa | הר עמשא (יישוב) (43901942) | Har Amasa (1376033751) |
| 77 | Korazim | כורזים (יישוב) (39654195) | Korazim (1379291697) |
| 78 | Lotan | לוטן (43739405) | Lotan, Israel (1376329697) |
| 79 | Massad | מסד (יישוב קהילתי) (37967463) | Masad, Israel (1374533756) |
| 80 | Newe Shalom | נווה שלום (44104284) | Neve Shalom (1374535238) |
| 81 | Retamim | רתמים (42605403) | Retamim (1376043086) |
| 82 | Timrat | תמרת (41592980) | Timrat (1374537550) |
| 83 | Zuqim | צוקים (43287399) | none read |
| 84 | Ezuz | עזוז (42909117) | Ezuz (1376040325) |
| 85 | Hannaton | חנתון (43570074) | Hanaton (1374531304) |
| 86 | Karme Yosef | כרמי יוסף (43762471) | Karmei Yosef (1374532139) |
| 87 | Kefar Weradim | כפר ורדים (43906840) | Kfar Vradim (1380249038) |
| 88 | Metar | מיתר (יישוב) (43793779) | Meitar (1381330909) |
| 89 | Shomeriyya | שומריה (קיבוץ) (42560622) | Shomria (1375581150) |
| 90 | Haluz | none read | none read |
| 91 | Lehavim | להבים (44048649) | none read |
| 92 | Shaharut | שחרות (43799989) | Shaharut (1378460167) |
| 93 | Shorashim | שורשים (יישוב קהילתי) (43737857) | Shorashim (1375599465) |
| 94 | Eshhar | אשחר (יישוב) (43255437) | Eshhar (1374529988) |
| 95 | Modi'in-Makkabbim-Re'ut | מודיעין-מכבים-רעות (43847013) | Modi'in-Maccabim-Re'ut (1381718361) |
| 96 | Nizzane Sinay | ניצני סיני (43790225) | Nitzanei Sinai (1376037609) |
| 97 | Avtalyon | אבטליון (יישוב) (43528362) | Avtalion (1374528189) |
| 98 | Gan Ner | גן נר (43922664) | Gan Ner (1374530211) |
| 99 | Merav | מירב (42319036) | Meirav (1374533980) |
| 100 | Newe Harif | נווה חריף (42948911) | Neve Harif (1376329219) |
| 101 | Nofit | נופית (42088842) | Nofit (1374535615) |
| 102 | Tel Te'omim | תל תאומים (43771321) | Tel Te'omim (1374537469) |
| 103 | Giv'at Ela | גבעת אלה (43107117) | Givat Ela (1374530822) |
| 104 | Karkom | כרכום (יישוב) (43133209) | Karkom (1375022917) |
| 105 | Kemehin | כמהין (מושב) (43176971) | Kmehin (1376039717) |
| 106 | Or HaGanuz | אור הגנוז (43285591) | Or HaGanuz (1375017541) |
| 107 | Shani | שני (יישוב) (43668983) | Livne (1376045741) |
| 108 | Avshalom | אבשלום (יישוב) (42987702) | Avshalom, Israel (1375584989) |
| 109 | Kefar Hananya | כפר חנניה (43737604) | Kfar Hananya (1375015585) |
| 110 | Giv'at Avni | גבעת אבני (43685512) | Giv'at Avni (1374530692) |
| 111 | Ibbim | איבים (43639438) | Ibim (1375608100) |
| 112 | Shoham | שוהם (יישוב) (43920000) | Shoham (1380223440) |
