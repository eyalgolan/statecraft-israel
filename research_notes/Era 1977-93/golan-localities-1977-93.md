# Israeli localities founded on the Golan Heights, 1977-1993 (research note, 2026-10-25)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file stays paused until after the MVP. Refs #165 (1977-93 21), the "Settlements" and caravan-site items of sections 7.3 and 7.4 of [the 1977-1993 design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md), as the owner's "Where to start" comment of 24 Oct 2026 scopes them. One topic of #165 only: **the Golan subdistrict of the CBS localities file.** The West Bank, Gaza, Sinai, the localities inside the pre-1967 lines (PR #1647), caravan sites and lines are not in this note. Which rows enter the game, and their names, wait for the owner's sensitivity list (#150); this note makes no include or omit choice and no naming choice, and it takes no position on status or legality beyond what a source says, quoted and tagged.

**Who checked this, under `docs/CONTENT_POLICY.md` section 2, item 6.** Claude Code wrote this note and read every source below on **2026-10-25**; **no person has read it against the sources yet**. Every verified item is written in the form the owner set on #1601.

It follows the format of [`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) and the conventions of the two localities notes (PR #1646, #1647). Every page below was opened on **25 October 2026** (UTC) unless a row says otherwise.

The question: which localities in today's CBS Golan subdistrict have a CBS year of founding from 1977 to 1993, and for each: the founding date (and what it marks), coordinates from a named source, and the sources.

## Short answer

- **The list.** The CBS localities file 2024 has 43 rows in the Golan subdistrict ("גולן"). 11 of them have a year of founding of 1977 to 1993, and that is the whole list: Qazrin (1977), Avne Etan, Ortal, Ani'am (1978), Natur (1980), Allone HaBashan, Mezar (1981), Qela (1984), Qidmat Zevi (1985), Had-Nes (1989), Kanaf (1991). They are the 11 Golan rows that PR #1647 left out. No other row in the file has a year in the window and a Golan subdistrict code. The other 32 rows are outside the window: 21 have a year from 1967 to 1976, one has 2022, and 10 have no year (six localities and four regional-council area rows with no usable coordinates).
- **10 of the 11 rows carry a disagreement [C]** on the year or the day, mostly because a locality had a temporary site, a Nahal outpost or a first group before its permanent site, and the sources date different steps. One row (Allone HaBashan) agrees on the year across the sources read ([S]); its day rests on one source.
- **Days a source gives:** Qazrin's dedication (23 Jan 1978 by the JTA dateline, checked on the page image; 24 Jan 1978 by Hebrew Wikipedia), Ani'am (28 Aug 1978, the Golan Regional Council page only), Natur (8 Feb 1980, in both Wikipedia articles, which probably share one source), Qela/Bruchim civilian settling (4 Nov 1991, a JTA item and a UN report) and Kanaf (1 Apr 1985 temporary site, council page and Hebrew Wikipedia; a New York Times item dated 22 May 1991 for the permanent site, day not resolved). Allone HaBashan's Hebrew date (Lag BaOmer 5741) converts to 22 May 1981 (derived).
- **Contemporaneous sources read: 5 items for 3 rows** (JTA 18 Aug 1977, JTA 24 Jan 1978, JTA 5 Nov 1991, JTA 6 Nov 1991, New York Times 22 May 1991); the UN report of 1992 is press-derived. **For 8 rows no contemporaneous record was opened.** That is the main [GAP] of the note ("What was not done").
- **Other notes.** `research_notes/Era 1977-93/localities-1977-93-pre67.md` (PR #1647) leaves out these 11 Golan rows, and `../Era 1957-77/golan-localities-1967-77.md` (PR #1655, merged) covers the Golan rows of 1967 to 1977. Qazrin (CBS 1977) is in both this note and #1655 (its row 22). #1655 lists Ani'am, Avne Etan and Ortal (CBS 1978) under its edge rows, "Rows at the edge of the window", and cites Gvati for their earlier dates; this note carries the same three rows with the CBS year as the window and points to #1655 for Gvati.

## Conventions and tags

- **[V, agent read 2026-10-25; a person's check pending]**: what a CBS file prints, read today. It says nothing about whether the printed value is right.
- **[S]** secondary or press: one source, or several that plainly copy one another. The Golan Regional Council pages, the Hebrew and English Wikipedia articles, JTA bulletins, the New York Times and the UN report (which cites press) are all [S]. Where Wikipedia cites a newspaper or a book I did not open the citation and do not count it as read.
- **[C]** conflicting: sources disagree; every value is given and what each marks, as far as the source says.
- **[GAP]** not closed, with what was tried.
- **Derived** marks a number or a date this note computed; the arithmetic or the library is named. A derived value is never tagged as verified.
- Quotes are copied from the page read; Hebrew quotes carry a translation. Translations from Hebrew are mine.
- JTA dates: the JTA web page shows the bulletin date; the event date is read from the dateline on the item in the original bulletin PDF (the owner's rule on #1596). Where an item says "today" or "Monday", it is resolved from that dateline.

## How it was done

- **The list, the CBS year and the coordinates.** CBS, localities file 2024 ("קובץ יישובים 2024"), https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (downloaded and opened 2026-10-25 08:00 UTC; 1,490 locality rows, one sheet; the same file, byte for byte, that PR #1646 used). Filter: column "שם נפה" (subdistrict) equal to "גולן", then column "שנת ייסוד" (year of founding) from 1977 to 1993. 43 rows have the subdistrict "גולן"; 11 have a year in the window; 10 rows have no year (six localities and four regional-council area rows). The CBS year is its "year of Jewish settlement or founding" (intro, section 12, as the #1646 note read it: the last settling year after a long break, the original year after a short one, the older community's year when localities merged), which is why it can differ from the first group's arrival. Intro: https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/intro.pdf (read for #1646; not reopened for this note).
- **What the codes mean.** The 2024 file does not list code meanings. I use the cross-tab of #1646 (2024 codes against the form printed in the CBS publication "Local authorities in Israel 2009", list 03): 310 moshav (printed in the 2009 list as "מושב (לרבות כפרי שיתופי)", moshav including kfar shitufi, as in #1655), 320 moshav shitufi, 330 kibbutz, 370 community settlement. I did not repeat that cross-tab here. Qazrin's code is 180; the file's municipal-status column for it reads "מועצה מקומית" (local council), and I give that text, not a meaning for 180. The file's current form can differ from the founding form (Natur and Mezar began as kibbutzim).
- **Coordinates.** The CBS field "קואורדינטות" has twelve digits; as in `settlements-1949-51.md` and #1646 I read it as an Israel Transverse Mercator easting and northing in metres (six digits each) and converted it with `pyproj` (EPSG:2039 to EPSG:4326), rounded to five decimals. That reading is mine, not stated in the file. I did not cross-check the 11 points against another source (see "What was not done"). Raw CBS fields are in the table.
- **Golan Regional Council pages.** The council's website (golan.org.il, run by the "עמותת יישובי הגולן", copyright 2007 on each page) now redirects away from the old paths, so each page was read from the Internet Archive, at the URL named in the row (https://web.archive.org/web/<timestamp>/http://www.golan.org.il/...; the Archive serves the capture nearest the timestamp, which I did not read back). The pages are undated except for the page date they print (March 2008); the capture dates in the Archive differ from the URL timestamps (some are 2008-2010) and are not given per row. They carry a "year established" field and a short history. They are a regional body's own text, not a contemporaneous record: [S].
- **JTA.** Searched through the JTA archive's own search (see "Searches"); the four bulletin PDFs cited (18 Aug 1977, 24 Jan 1978, 5 and 6 Nov 1991) were downloaded from http://pdfs.jta.org, OCR'd locally, and the dateline and quoted lines checked on the rendered page image.
- **Wikipedia** (Hebrew and English) was fetched through the MediaWiki API as wikitext of the current revision; each revision is in the last table. Always [S]; a lead and a cross-check.
- Hebrew dates were converted with the `convertdate` library (Python; month numbers as the library numbers them, Nisan = 1).
- All requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

## Summary table

Order: by CBS year, then name. "CBS year", "CBS code" and the raw field are **[V, agent read 2026-10-25; a person's check pending]** as to what the file prints. Coordinates are **derived** from the raw field (method above). Type is the CBS code read by the #1646 cross-tab, with the municipal-status text for Qazrin. "Tag" is the note's own tag for the founding date.

| # | Locality (CBS spelling) | Hebrew | Type | CBS year | CBS code | Lat, lon (derived) | CBS raw field | Tag |
|---|---|---|---|---|---|---|---|---|
| 1 | Qazrin | קצרין | local council (CBS code 180) | 1977 | 180 | 32.99246, 35.68681 | 264540766483 | [C] |
| 2 | Avne Etan | אבני איתן | moshav (310) | 1978 | 310 | 32.82322, 35.76520 | 271966747749 | [C] |
| 3 | Ortal | אורטל | kibbutz (330) | 1978 | 330 | 33.08693, 35.75869 | 271203776993 | [C] |
| 4 | Ani'am | אניעם | moshav (310) | 1978 | 310 | 32.95728, 35.74014 | 269544762605 | [C] |
| 5 | Natur | נטור | moshav (310; a kibbutz until 2008) | 1980 | 310 | 32.85315, 35.75312 | 270818751063 | [C] |
| 6 | Allone HaBashan | אלוני הבשן | moshav shitufi (320) | 1981 | 320 | 33.04403, 35.83872 | 278703772277 | [S] |
| 7 | Mezar | מיצר | moshav (310; a kibbutz until 2015) | 1981 | 310 | 32.76708, 35.73597 | 269260741509 | [C] |
| 8 | Qela | קלע | community settlement (370) | 1984 | 370 | 33.13172, 35.68396 | 264203781926 | [C] |
| 9 | Qidmat Zevi | קדמת צבי | moshav (310) | 1985 | 310 | 33.02999, 35.69767 | 265535770650 | [C] |
| 10 | Had-Nes | חד-נס | community settlement (370) | 1989 | 370 | 32.92740, 35.64237 | 260416759249 | [C] |
| 11 | Kanaf | כנף | moshav (310) | 1991 | 310 | 32.86963, 35.69991 | 265828752866 | [C] |

## The rows

Each row: what the sources say about the founding, in the order CBS, the Golan Regional Council page (GRC), JTA and other press, Hebrew Wikipedia (he.wp), English Wikipedia (en.wp), and what each date marks where a source says. "Temporary site" and "permanent site" are the sources' words. Revisions are in the last table.

1. **Qazrin** (CBS 1977; local council) [C]. CBS 1977. he.wp: "קצרין הוקמה ביולי 1977 על ידי גרעין שהתארגן בשנת 1975" (Qazrin was founded in July 1977 by a core group organised in 1975); the decision to set up a town in the Golan was taken by the Meir government in 1973; "נחנכה ב-24 בינואר 1978" (it was inaugurated on 24 January 1978), a date he.wp attributes to a Haim Yavin book that I did not open. en.wp: infobox founded 1977; its text: "Katzrin was established in 1977 as a planned urban center for the Golan Heights based on a decision of the Israeli government in November 1973. Construction began in 1976." **JTA, bulletin of 24 Jan 1978** (`http://pdfs.jta.org/1978/1978-01-24_016.pdf`, PDF page 3, item "Israel Will Not Withdraw from Golan" by a JTA correspondent): dateline "KATZERIN, Golan Heights, Jan. 23 (JTA)" and "Residents of this new town in the central Golan Heights which was dedicated today on Tu B'Shvat, the Jewish Arbor Day, were pleased to hear from an official government spokesman that Israel will not withdraw from the Golan." It also says Construction Minister Gideon Patt told "the 100 families already settled in Katzerin". **Derived:** Tu B'Shvat (15 Shvat) 5738 is 23 January 1978 (`convertdate`), a Monday, so the dateline day and the holiday agree. The dedication therefore falls on **23 Jan 1978** by the JTA dateline and **24 Jan 1978** by he.wp; the bulletin date is 24 Jan, which may be the source of the later date (my guess; he.wp's own source was not opened). **JTA, bulletin of 18 Aug 1977** (`http://pdfs.jta.org/1977/1977-08-18_158.pdf`, PDF page 3, item "Golan Tenth Anniversary", checked on the page image; the same item is cited in #1655): dateline "TEL AVIV, Aug. 17 (JTA)" and "This month also marks the beginning of the settlement of the Kazarin Township, the urban center of the Golan Heights." So the start of settling is **July 1977 (he.wp) against August 1977 (JTA)**. Gvati, as quoted in #1655 (not re-opened here): "ב-1977 הוחל, באזור המרכזי, באכלוס המרכז העירוני קצרין" (in 1977 settling of the urban centre Qazrin began in the central area), a year without a month. GRC has no page for Qazrin (it is a separate local council).

2. **Avne Etan** (moshav; CBS 1978) [C]. CBS 1978. GRC (`http://www.golan.org.il/555/434.htm`): "שנת הקמה: 1974"; "ההתחלה היתה בנקודה זמנית במושב נוב ביולי 1974" (the start was at a temporary site in Moshav Nov in July 1974); "ביוני 1987 עברו החברים לנקודת הקבע" (in June 1987 the members moved to the permanent site). he.wp: the founding core group "עלה בשנת 1974 למחנה זמני בנוב"; "ב-1978 לאחר תום בניית יישוב הקבע עברו חברי היישוב ליישובם" (in 1978, after the permanent settlement was built, the members moved to it). en.wp: founded in 1978, families living temporarily in Nov first. So 1974 marks the first group, 1978 is the CBS and Wikipedia year, and GRC alone dates the move to the permanent site to June 1987, which contradicts he.wp's 1978. Which step the CBS year marks is not stated; I did not resolve it. JTA 22 Sep 1992 names a farmer of "Moshav Avnei Eitan" and says nothing about its founding. Gvati dates Avne Etan to 1975 at Tel Zayit (a Nahal gar'in) and its civilianising to 1977; see #1655, "Rows at the edge of the window" (not re-opened here).

3. **Ortal** (kibbutz; CBS 1978) [C]. CBS 1978. GRC (`/553/396.htm`): "קיבוץ אורטל נוסד בשנת 1978, כקיבוץ הרביעי בשרשרת קיבוצי צפון רמת הגולן" (founded in 1978 as the fourth kibbutz in the chain of northern Golan kibbutzim). he.wp: the core group formed in 1976; "בספטמבר 1977 עלו החברים כקיבוץ עצמאי למרום הגולן" (in September 1977 the members went up to Merom Golan as an independent kibbutz); in early June 1978 they moved to the permanent site; officially inaugurated in September 1978. en.wp: founded 1978. 1977 (first group on a neighbour's site), June 1978 (permanent site) and September 1978 (inauguration) are three steps; no day is given for the 1977 arrival. Gvati, as quoted in #1655 (not re-opened here), has Ortal ".76" in his appendix and, in the narrative, among two kibbutz settlements "in the stages of setting up in 1977" (בשלבי הקמה); see #1655, "Rows at the edge of the window".

4. **Ani'am** (moshav; CBS 1978) [C]. CBS 1978. GRC (`/554/420.htm`): "שנת הקמה: 1978"; "הקבוצה התארגנה בשנת 1971 אולם התפלגה ורק ב- 28 באוגוסט 1978 נוסד היישוב" (the group organised in 1971 but split, and only on 28 August 1978 was the settlement founded). he.wp: "המושב עלה ליישוב הקבע בשנת 1978" (the moshav moved to the permanent settlement in 1978). en.wp: built in 1978. The year 1978 agrees across CBS, GRC and Wikipedia; the day, **28 Aug 1978 (a Monday, derived)**, rests on GRC alone and GRC does not say what it marks. Gvati, as quoted in #1655 (I did not re-open that page here): "ב-1974 עלה במרכז הרמה המושב השיתופי אניעם" (in 1974 the moshav shitufi Ani'am went up in the centre of the Heights). So 1974 (Gvati) against 1978 (CBS, GRC, Wikipedia); #1655 lists the row at its edge with the same two dates.

5. **Natur** (CBS 1980; a kibbutz until 2008) [C]. CBS 1980. GRC (`/555/449.htm`): "ב-1978 החלו עבודות הבנייה, ובינואר 1980 הוקם היישוב רשמית" (construction began in 1978 and in January 1980 the settlement was officially founded). he.wp: a core group formed in summer 1977; "הקיבוץ עלה על הקרקע ב–8 בפברואר 1980" (the kibbutz went onto the land on 8 February 1980). en.wp: infobox 1980, and "officially founded on February 8, 1980"; it cites items of Davar dated 18 Jan 1980 and 4 Feb 1980 (the latter titled "Kibbutz Natur Established in the Golan" in en.wp's citation) and of Maariv dated 3 Feb 1980 (it also cites two Maariv items of 1977 and 1978); I did not open any of them (the National Library site refused this machine, see "What was not done"). January 1980 (GRC) and 8 February 1980 (he.wp and en.wp) differ; an item dated 4 Feb 1980 that en.wp cites with the word "Established" precedes the 8 Feb date, which I note and do not resolve.

6. **Allone HaBashan** (moshav shitufi; CBS 1981) [S]. CBS 1981. GRC (`/553/409.htm`): "שנת הקמה: 1981 תשמ"א"; "המושב של "הפועל המזרחי" עלה לקרקע ב-תשמ"א, 1981". he.wp: the core group "עלה למקום בל"ג בעומר ה'תשמ"א - מאי 1981" (went up on Lag BaOmer 5741, May 1981). **Derived:** Lag BaOmer (18 Iyar) 5741 is 22 May 1981, a Friday (`convertdate`). en.wp: founded 1981; it cites Davar of 25 May 1981, "מושב אלוני־הבשן עלה על הקרקע ברמת הגולן" (the moshav went onto the land); I did not read that article (see above). CBS, GRC, he.wp and en.wp agree on 1981; the day 22 May 1981 is derived from one source's holiday name.

7. **Mezar** (CBS 1981; a kibbutz until 2015) [C]. CBS 1981. GRC (`/555/442.htm`): "שנת הקמה: 1981"; the kibbutz "שעלה לקרקע כהיאחזות נח"ל ביוני 1981, אך לאחר אזרוחו התפרק" (went onto the land as a Nahal outpost in June 1981, but after it was made civilian it broke up); "הישוב חודש על ידי ועד ישובי הגולן ביום כינוס ועידת מדריד (1991)" (the settlement was renewed by the Golan settlements committee on the day the Madrid conference convened, 1991). he.wp: set up as a Nahal outpost in 1981; "ולאחר שנתיים הפך ליישוב קבע" (two years later it became a permanent settlement); in 1989 most members left and members of the Qela core group came in. en.wp: established 1981, "The 29th settlement on the Golan Heights". The year 1981 agrees; GRC's June 1981 is the only month; the civilian settling (about 1983 by he.wp, not dated by GRC) and the 1991 renewal are other steps. I did not find the Madrid date in the page; the conference date is not given here.

8. **Qela** (community settlement; CBS 1984) [C]. CBS 1984. he.wp (article "קלע אלון"): in May 1981 a few settlers went up to about 12 houses near Mubarakin, temporary, called "Qela"; around 1985 they moved to the permanent site that became Qidmat Zevi; "ב-16 בספטמבר 1984 הוקם קיבוץ קלע אלון של תנועת התק"ם בן 36 מתיישבים" (on 16 September 1984 the Qela Alon kibbutz of the TAKAM movement, 36 settlers, was founded), dissolved at the end of 1988; a Nahal outpost in 1990; at the end of 1991 a community settlement was set up by a core group of immigrants from Russia, called Bruchim. GRC (`/553/417.htm`, "קלע אלון - מצוק עורבים"): "שנת הקמה: 1981"; "ראשיתו של הישוב כהיאחזות נח"ל, שאוזרחה בשנת 1984 והיתה לקיבוץ. ב-1989 התפרק הקיבוץ והמקום חזר והיה להיאחזות נח"ל. הישוב הנוכחי הוקם ב-1991 בידי עולים מחבר המדינות ונקרא תחילה ברוכים" (it began as a Nahal outpost that was made civilian in 1984 and became a kibbutz; the kibbutz dissolved in 1989 and the place was again a Nahal outpost; the present settlement was founded in 1991 by immigrants from the CIS and was first called Bruchim). **JTA, bulletin of 5 Nov 1991** (`http://pdfs.jta.org/1991/1991-11-05_206.pdf`, PDF page 2, item "Soviet Jews Move into Golan Kibbutz, in Move Likely to Infuriate Syrians" by a JTA correspondent): dateline "TEL AVIV, Nov. 4 (JTA)"; "A group of Soviet immigrants moved into an abandoned kibbutz on the Golan Heights on Monday"; "Kibbutz Kelah has a checkered history. Established by the United Kibbutz Movement, its residents abandoned the place two years ago. It was taken over by Nahal"; the IDF "found 22 families, all new immigrants from the Soviet Union". **Derived:** 4 Nov 1991 is a Monday, so "Monday" is the dateline day. **JTA, bulletin of 6 Nov 1991** (`http://pdfs.jta.org/1991/1991-11-06_207.pdf`, PDF page 1, item "U.S. Consulting with Mideast Parties About Where to Resume Bilateral Talks", checked on the page image): dateline "WASHINGTON, Nov. 5 (JTA)"; "The settlement, at Kibbutz Kelah, was abandoned two years ago and taken over by Nahal" and "Housing Minister Ariel Sharon used the occasion of the settlement dedication ceremony Monday", that is Monday 4 Nov, the same day as the item above. **UN report A/47/294 (front page: 7 July 1992)**, paragraph 9, a document that cites Ha'aretz of 6 Oct 1991 and the Jerusalem Post of 5 Nov 1991: "On 4 November 1991, a new settlement (Bruchim) was inaugurated in the Golan Heights by the Ministers of Housing, Energy and Agriculture." (The mark after "Housing" is faint on the scan; I read it as a comma.) So: CBS 1984 (the kibbutz, 16 Sep 1984 by he.wp), 1981 (GRC's year and he.wp's temporary settling), and 4 Nov 1991 (JTA: immigrants "moved into" the place on Monday; the UN report: a settlement "inaugurated" that day; two acts on one date). The JTA item calls the place "Kibbutz Kelah" and the UN report "Bruchim"; that they are the same place rests on he.wp and GRC naming Bruchim as the first name of the present Qela, not on the JTA or UN text (neither names the other). en.wp: settled 1981 as a Nahal settlement, abandoned in 1988, the modern settlement established in 1991 as Bruchim, name Kela Alon adopted in 1997.

9. **Qidmat Zevi** (moshav; CBS 1985) [C]. CBS 1985. GRC (`/554/423.htm`): "שנת הקמה: 1985"; "ראשוני הישוב עלו לגולן ב-1981 והתיישבו בקלע-גולן שבצפון, עד המעבר לנקודת הקבע בקיץ 1985" (the first settlers went up to the Golan in 1981 and settled in Qela-Golan in the north, until the move to the permanent site in summer 1985). he.wp: "קדמת צבי הוקמה בשנת 1981" and "יישוב הקבע נוסד בשנת 1985". en.wp: infobox 1981, text "built in 1981". 1981 marks the first group on another site, summer 1985 the permanent site (CBS year). No day in any source.

10. **Had-Nes** (community settlement; CBS 1989) [C]. CBS 1989. GRC (`/556/427.htm`): "שנת הקמה: 1982"; the Betar movement began organising right after the announcement of the Sinai evacuation; "ב-1987 פנו החברים מעל דפי העיתון בקריאה למשפחות חדשות" (in 1987 the members appealed in the newspaper to new families); "ב-1989 עלו לנקודת הקבע בגולן רק המשפחות החדשות, ולא היתה ביניהם ולו משפחה אחת של מפוני סיני" (in 1989 only the new families went up to the permanent site, and there was not one Sinai evacuee family among them). he.wp: the movement published its call in 1987 and "ב-1989 הוקם היישוב". en.wp: founded 1989, by the Herut Beitar settlement movement. 1982 (GRC's year field, the organising) and 1989 (CBS, he.wp, en.wp, the settling) mark different steps. No day in any source.

11. **Kanaf** (moshav; CBS 1991) [C]. CBS 1991. GRC (`/556/431.htm`): "שנת הקמה: 1985"; "קבוצת חלוץ של המושב, בת חמישה חברים, עלתה בראשית 1985 לנקודה זמנית באליעד. ב-1.4.85 הצטרפו אליהם שאר 25 חברי הגרעין" (a pioneer group of five went up in early 1985 to a temporary site at Eli-Ad; on 1.4.85 the other 25 members of the core group joined them); they lived there until "המעבר לנקודת הקבע במאי 1991" (the move to the permanent site in May 1991). he.wp: the core group settled at a temporary site on 1 April 1985 and moved to the present site in 1991. en.wp: infobox 1985, and "began to be populated in 1991". **New York Times, 22 May 1991, page 5**, "Israel Opens New Golan Heights Settlement" (read through the Internet Archive copy `https://web.archive.org/web/2020/https://www.nytimes.com/1991/05/22/world/israel-opens-new-golan-heights-settlement.html`): "The Government opened a new Jewish settlement in the Golan Heights today" and "Thirty Jewish families moved into homes built at the new settlement of Kanaf, on a bluff overlooking the Sea of Galilee." It adds: "Mr. Sharon presided over the groundbreaking ceremony." The digitised text prints no dateline, so "today" cannot be resolved by the dateline rule; it is not later than 22 May 1991. **[GAP]** the exact day of the permanent-site opening; tried: the NYT page above (no dateline in the digitised text) and a Journal of Palestine Studies chronology page that I could not open (see "What was not done"). 1 April 1985 (GRC, he.wp: the temporary site) and May 1991 (permanent site, CBS year) are two steps.

## What was not done

- **No contemporaneous record for 8 rows.** For Avne Etan, Ortal, Ani'am, Natur, Allone HaBashan, Mezar, Qidmat Zevi and Had-Nes no press report, government decision or Official Gazette item of the day was opened. Their dates rest on the council's later web pages and Wikipedia. **[GAP]** The Wikipedia articles cite Davar and Maariv items (for Natur, Allone HaBashan, Qidmat Zevi, Avne Etan) at https://www.nli.org.il/ ; the National Library site answers this machine with a browser check (HTTP 403, 2026-10-25), and the Internet Archive copy of the Davar page of 25 May 1981 returned only the headline and page frame, not the article text. None of those items was read.
- **Knesset and government decisions.** `https://main.knesset.gov.il` and `https://knesset.gov.il/` answered HTTP 303 to `https://www.knesset.gov.il/maintenance-page-geo` on 2026-10-25 (08:06 UTC); `https://www.gov.il` answered HTTP 403. No government decision on any of the 11 settlements was read. The decision of 1973 on Qazrin is named by he.wp and not read. **[GAP]**
- **Other sources tried and not readable.** The Detroit Jewish News copy of the JTA Katzrin item (`https://digital.bentley.umich.edu/djnews/djn.1978.01.27.001/23/download_text`, HTTP 403, also not in the Internet Archive); `https://chronology.palestine-studies.org/node/11148` (HTTP 403; a Journal of Palestine Studies chronology page that a web search listed for Kanaf; its content was not read and no claim in this note rests on it); the CIA Reading Room page `https://www.cia.gov/readingroom/node/1940733` (a "System Maintenance" page); the UN digital library search (HTTP 202, empty). The Internet Archive CDX index timed out.
- **What each date marks.** A Nahal outpost, a temporary site, a first group, a permanent site and an inauguration are different steps; where a source does not say which it dates, the row says so.
- **Coordinates not cross-checked** against GeoNames, Wikipedia or a map for the 11 points; the ITM reading of the CBS field is mine.
- **Membership rests on the CBS year and the CBS subdistrict.** A Golan locality whose CBS year is outside 1977-1993 but whose first group arrived inside it (or the reverse) is not in the table, except where a row names such a case (Qela's 1981 first settling, Had-Nes's 1982 year field). The file has 21 Golan rows with years from 1967 to 1976 (19 kibbutz, moshav and moshav shitufi codes, the rows #1646 left out; Bene Yehuda and Haspin are code 370), none from 1994 to 2021, and one row of 2022. **[GAP]** the sub-window counts were not researched.

## Searches (every negative record)

All run 2026-10-25 (UTC). JTA search URL form: `https://www.jta.org/?s=<query, spaces as +>&post_type=archive&paged=<n>` (page 1 has no `paged`). Each page returned up to 10 archive items; I opened every item. "Dated 1977-93" counts items whose bulletin date is from 1977 to 1993; "name" counts, among those, items whose web text contains the settlement's name in one of the spellings tried for that row. The result lists are JTA's own relevance order, so the counts do not say what the archive as a whole holds.

| Row | Query | Pages read | Items returned | Dated 1977-93 | With name | Bulletin dates in the list |
|---|---|---|---|---|---|---|
| Qazrin | `Katzrin` | 3 | 30 | 10 | 7 | 1933-12-10 to 2008-05-28 |
| Qazrin | `Katzrin Golan` | 3 | 30 | 14 | 9 | 1970-11-04 to 2008-05-28 |
| Qazrin | `Qazrin` | 3 | 30 | 11 | 0 | 1929-04-08 to 1998-06-16 |
| Qazrin | `Katzrin city Golan` | 2 | 10 | 2 | 0 | 1973-09-12 to 2007-08-01 |
| Avne Etan | `Avne Eitan` | 3 | 30 | 21 | 0 | 1936-10-05 to 2007-07-26 |
| Avne Etan | `Avnei Eitan` | 3 | 30 | 19 | 1 | 1962-11-02 to 2007-06-01 |
| Avne Etan | `Avne Etan` | 3 | 30 | 12 | 0 | 1930-01-06 to 2008-05-20 |
| Avne Etan | `Avney Eitan` | 3 | 22 | 13 | 1 | 1971-01-29 to 2007-06-01 |
| Ortal | `Ortal Golan` | 3 | 30 | 8 | 0 | 1962-12-24 to 2000-02-28 |
| Ortal | `Ortal kibbutz` | 3 | 24 | 15 | 0 | 1967-10-04 to 2001-11-06 |
| Aniam | `Ani'am` | 3 | 30 | 3 | 0 | 1923-05-02 to 2004-12-08 |
| Aniam | `Aniam Golan` | 2 | 9 | 2 | 0 | 1970-04-27 to 2006-08-10 |
| Aniam | `Aniam moshav` | 3 | 16 | 5 | 0 | 1927-02-10 to 2006-08-10 |
| Natur | `Natur Golan` | 3 | 30 | 10 | 0 | 1962-03-05 to 2005-03-29 |
| Natur | `Natur kibbutz Golan` | 3 | 11 | 5 | 0 | 1973-10-10 to 2000-06-06 |
| Allone HaBashan | `Alonei Habashan` | 3 | 27 | 11 | 0 | 1948-04-08 to 2008-05-08 |
| Allone HaBashan | `Allone Habashan` | 3 | 30 | 12 | 0 | 1934-07-11 to 2008-01-31 |
| Allone HaBashan | `Alonei Bashan` | 3 | 30 | 8 | 0 | 1924-01-01 to 2008-10-29 |
| Allone HaBashan | `Allon Habashan` | 3 | 30 | 12 | 0 | 1934-07-11 to 2008-01-31 |
| Mezar | `Metzar Golan` | 3 | 15 | 8 | 0 | 1975-06-26 to 2008-06-17 |
| Mezar | `Mezar Golan` | 1 | 0 | 0 | 0 |  to  |
| Mezar | `Metzar settlement` | 3 | 30 | 8 | 0 | 1934-01-18 to 2005-08-23 |
| Mezar | `Mitzar Golan` | 3 | 20 | 7 | 0 | 1969-09-29 to 2007-03-09 |
| Qela | `Kela Golan` | 2 | 6 | 3 | 0 | 1973-10-17 to 2008-08-25 |
| Qela | `Keila Golan settlement` | 1 | 0 | 0 | 0 |  to  |
| Qela | `Kela kibbutz Golan` | 2 | 3 | 2 | 0 | 1973-10-17 to 1991-11-06 |
| Qela | `Kela Alon` | 3 | 30 | 4 | 0 | 1924-11-19 to 2008-10-21 |
| Qela | `Kela settlement Golan` | 2 | 3 | 2 | 0 | 1973-10-17 to 1991-11-06 |
| Qela | `Kelah Golan` | 2 | 3 | 2 | 2 | 1991-11-05 to 2006-08-03 |
| Qela | `Kelah kibbutz Golan` | 2 | 2 | 2 | 2 | 1991-11-05 to 1991-11-06 |
| Qela | `Kelah settlement Golan` | 2 | 2 | 2 | 2 | 1991-11-05 to 1991-11-06 |
| Qela | `Kelah` | 3 | 30 | 6 | 2 | 1929-03-26 to 2008-11-18 |
| Kidmat Zevi | `Kidmat Zvi` | 3 | 30 | 10 | 0 | 1936-11-03 to 2007-04-20 |
| Kidmat Zevi | `Kidmat Tzvi` | 3 | 30 | 12 | 0 | 1946-06-20 to 2007-04-20 |
| Kidmat Zevi | `Kidmat Zevi` | 3 | 30 | 10 | 0 | 1936-08-09 to 2007-06-28 |
| Kidmat Zevi | `Kidmat Zvi Golan` | 2 | 8 | 0 | 0 | 1955-10-20 to 2005-11-30 |
| Kidmat Zevi | `Kidmat Tsvi` | 3 | 13 | 4 | 0 | 1969-12-10 to 2006-09-27 |
| Had Nes | `Had Ness` | 3 | 30 | 9 | 0 | 1924-08-19 to 2007-02-07 |
| Had Nes | `Had-Nes` | 3 | 30 | 12 | 0 | 1923-08-14 to 2004-11-24 |
| Had Nes | `Had Nes Golan` | 3 | 30 | 13 | 0 | 1972-03-08 to 2008-05-28 |
| Had Nes | `Hadness Golan` | 3 | 30 | 14 | 0 | 1962-03-05 to 2008-05-28 |
| Had Nes | `Chad Nes` | 3 | 30 | 13 | 0 | 1931-04-25 to 2005-09-27 |
| Kanaf | `Kanaf Golan` | 2 | 4 | 1 | 0 | 1933-01-29 to 2006-08-01 |
| Kanaf | `Kanaf moshav` | 2 | 1 | 0 | 0 | 1999-09-22 to 1999-09-22 |
| Kanaf | `Kanaf settlement` | 2 | 8 | 3 | 0 | 1938-04-18 to 2007-03-06 |

The five older Qela rows did not try the spelling the two 1991 JTA items use, "Kelah" (found only after the owner's review); the four "Kelah" rows were run on 2026-10-25 (a Sunday) after it, same URL form. Each "Kelah" query returns the two items of 5 and 6 Nov 1991 and no other item of 1977-93 with the name, so the "With name 0" of the five older rows is a spelling gap, not an absence from the archive.

Items I read around the name that did not concern a founding: Qazrin on 27 Dec 1979, 11 Jun 1979, 11 Feb 1980, 18 Dec 1984, 18 Jul 1989, 19 Mar 1991, 24 Jul 1991, 25 Nov 1991, 31 Aug 1992 and 17 May 1993; Avne Etan on 22 Sep 1992; Ortal on 25 Jun 1993. "Read" here means the passage around the name (about 500 characters), not the whole bulletin; in those passages none gives a founding date. The items on Qazrin of 24 Jan 1978 and on Qela of 5 and 6 Nov 1991 (and on Qazrin of 18 Aug 1977) are the ones cited in the rows.

Second batch (spelling variants and descriptive queries, same URL form, pages 1-2):

| Query | Pages read | Items returned |
|---|---|---|
| `Katzerin` | 2 | 20 |
| `Kazrin` | 2 | 20 |
| `Katsrin` | 2 | 20 |
| `Qatzrin` | 2 | 20 |
| `Katzrin dedicated Gideon Patt` | 2 | 17 |
| `Katzerin dedication Golan 1978` | 2 | 0 |
| `Golan new town dedicated Patt` | 2 | 20 |
| `new settlement Golan Heights 1977 Katzrin first families` | 2 | 1 |
| `Kazerin Golan` | 2 | 20 |
| `Kuneitra Golan Jewish settlers new settlement 1981 Golan settlement` | 2 | 0 |
| `Golan settlers new settlement Hashomer Hatzair Natur` | 2 | 0 |
| `Golan new settlement 1981 Nahal Metzar` | 2 | 0 |
| `Mitzar Nahal Golan` | 2 | 2 |
| `Kibbutz Kela Golan` | 2 | 3 |
| `Golan new religious moshav 1981 Bnei Akiva Golan northernmost` | 2 | 0 |
| `Hadness Betar Golan Sinai evacuees` | 2 | 2 |
| `Golan Betar settlement Sinai evacuees` | 2 | 0 |
| `Ani'am Soviet immigrants Golan` | 2 | 0 |
| `Aniam Golan Soviet immigrant scientists moshav` | 2 | 0 |

Other searches: `https://web.archive.org/cdx/search/cdx?url=golan.org.il/*&matchType=prefix&limit=3&filter=statuscode:200` timed out (30 s, no result); `https://web.archive.org/web/2010/http://www.katzrin.muni.il/` and `/web/2012/` returned HTTP 404; `https://www.katzrin.muni.il` did not resolve. WebSearch (built-in, no Tavily key on this machine) was used only to find pages to open; results were not used as sources: query "New York Times 1977 Katzrin Golan Heights new town first families" (10 results, none from the Times); "nytimes.com 1991 Israel Golan Heights new settlement Kela OR Metzar OR \"Had Nes\" immigrants" (9 results; led to the UN report and the NYT item); "Israel 1981 Golan Heights new settlement Nahal ... Kela" (9 results, none opened).

## Sources and revisions

CBS localities file 2024 (https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx), opened 2026-10-25. Golan Regional Council pages, via the Internet Archive, opened 2026-10-25:

- https://web.archive.org/web/20091221081652/http://www.golan.org.il/555/434.htm (Avne Etan), `/555/442.htm` (Mezar) and `/555/449.htm` (Natur), same timestamp
- https://web.archive.org/web/20091219070603/http://www.golan.org.il/553/396.htm (Ortal), `/553/409.htm` (Allone HaBashan), `/553/417.htm` (Qela Alon)
- https://web.archive.org/web/20091126231937/http://www.golan.org.il/554/420.htm (Ani'am) and `/554/423.htm` (Qidmat Zevi)
- https://web.archive.org/web/20091217213527/http://www.golan.org.il/556/427.htm (Had-Nes) and `/556/431.htm` (Kanaf)
- https://web.archive.org/web/20091219175950/http://www.golan.org.il/411/ (history page: "חידוש ההתיישבות בגולן החל לאחר מלחמת ששת הימים", settlement in the Golan began again after the Six-Day War; it dates no settlement)

Press and UN: JTA `http://pdfs.jta.org/1978/1978-01-24_016.pdf` (PDF page 3), `http://pdfs.jta.org/1991/1991-11-05_206.pdf` (PDF page 2), `http://pdfs.jta.org/1991/1991-11-06_207.pdf` (PDF page 1), `http://pdfs.jta.org/1977/1977-08-18_158.pdf` (PDF page 3), and `https://www.jta.org/archive/israel-will-not-withdraw-from-golan`, `https://www.jta.org/archive/soviet-jews-move-into-golan-kibbutz-in-move-likely-to-infuriate-syrians`; the New York Times copy named in row 11; UN report A/47/294 (`https://unispal.un.org/pdfs/a47294e199284.pdf`, 9 pages, paragraph 9 on page 3, via https://www.un.org/unispal/?p=183659).

Wikipedia revisions (current at the fetch; URL form `https://he.wikipedia.org/w/index.php?oldid=<n>`, `https://en.wikipedia.org/w/index.php?oldid=<n>`):

| # | Locality | Hebrew article (oldid) | English article (oldid) |
|---|---|---|---|
| 1 | Qazrin | קצרין (44094064) | Katzrin (1380250661) |
| 2 | Avne Etan | אבני איתן (43749614) | Avnei Eitan (1379636185) |
| 3 | Ortal | אורטל (43128538) | Ortal (Israeli settlement) (1379635574) |
| 4 | Ani'am | אניעם (42883785) | Ani'am (1374625556) |
| 5 | Natur | נטור (43375761) | Natur (Israeli settlement) (1379635541) |
| 6 | Allone HaBashan | אלוני הבשן (43213803) | Alonei HaBashan (1379635946) |
| 7 | Mezar | מיצר (מושב) (43309326) | Metzar (1381494780) |
| 8 | Qela | קלע אלון (42505278) | Kela Alon (1379797332) |
| 9 | Qidmat Zevi | קדמת צבי (42191956) | Kidmat Tzvi (1379636713) |
| 10 | Had-Nes | חד נס (43789645) | Had Ness (1379635648) |
| 11 | Kanaf | כנף (מושב) (43843905) | Kanaf (1379636610) |
