# Settlements founded in 1952-1956: the Jerusalem District (research note, 2026-10-22)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file and the
tool change (#61) stay paused until after the MVP. Refs #27, its 1952-56 rows, one region. It follows the format of
[`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) (#485), which covers 1949-1951; those rows are
not repeated here. Sibling notes for other regions: the Negev (#1366), the Ashkelon subdistrict (#1369) and the Northern District
(#1380).

The question: which Jewish localities were founded in the Jerusalem District between 1 January 1952 and 31 December
1956, and for each: founding date, coordinates, type, founding group, renames and sources.

## Which "Jerusalem District"

**Boundary used: the Jerusalem District (מחוז ירושלים, code 1) of the Central Bureau of Statistics**, as given in the
CBS localities file 2024 (columns "סמל מחוז", "שם מחוז", "סמל נפה" and "שם נפה"; source below). Every row of district 1
in that file has subdistrict 11 (Jerusalem), so district and subdistrict are the same area here. It is the city of
Jerusalem inside today's municipal line plus the Mateh Yehuda area: the Jerusalem corridor, the hills around Beit
Shemesh and the Adullam area down to Nehusha. It leaves out the Ashkelon subdistrict (61; the Lachish area, see #1369)
and everything in the Central District, so moshavim that some sources place in the Judean Shephelah, such as Yad
Natan, Kfar Ben Nun or Beit Uziel, are not here. This line was chosen because it is the one the official source applies
to every row. **Other regions of #27 stay open to other claimants, and this region could be split differently if the
owner prefers another line.**

Jerusalem itself is one row in the CBS file (code 3000, founding year "ותיק", old) and is not a candidate; neighbourhoods
founded inside the city in 1952-56 are not localities in the CBS sense and were not searched for.

## How it was done

- Every page was fetched on 22 October 2026 with `curl` (User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`) or read through the MediaWiki
  API. No request carried a personal identifier. No web search result was used as a source.
- **The candidate pool** is the union of three lists: (1) every row of the CBS localities file 2024 in district 1 whose
  "שנת ייסוד" (year of founding) is 1952 to 1956: 7 rows; (2) every row of Haim Gvati's list of rural settlements whose
  "תאריך הקמה" (date of founding) falls in 1952-1956 and whose name matches a district-1 row of the CBS file: 5 rows,
  all of them already in list 1; (3) the Hebrew Wikipedia categories "יישובים שהוקמו ב-1952" to "יישובים שהוקמו ב-1956"
  intersected with "יישובים במחוז ירושלים": 10 pages, which added 4 rows that the CBS file dates outside the window or
  not at all (Beko'a, the Ein Kerem agricultural school, Givat Shemesh, Nehusha), each kept with **[C]**. That gives
  **11 rows**.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024 ("קובץ יישובים
  2024"), https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22), columns
  "שנת ייסוד" and "קואורדינטות". As in `settlements-1949-51.md`, the twelve-digit coordinate field is read as an Israel
  Transverse Mercator easting and northing (six digits each) and converted with `pyproj` (EPSG:2039 to EPSG:4326).
  That reading is ours, not stated in the file; the 1949-51 note checked it against Wikipedia for 29 rows (median
  about 40 m). Coordinates below are the CBS values, rounded to five decimals. They are where the locality is
  **today**; where a source says the first camp stood elsewhere, the row says so.
- **Founding dates (day).** Gvati, *100 Years of Settlement*, vol. 2, appendix "List of all Jewish rural settlements
  in Israel", Ben-Yehuda Project edition, https://benyehuda.org/read/14684 (checked 2026-10-22). Every Gvati row used
  here is in its section "מושבים"; its columns are name, region ("אזור גיאוגרפי"), date of founding, affiliation
  ("השתייכות ארגונית") and origin of the name. The column does not say whether the date is the day on the land or a
  later recognition. It is one secondary compilation. Institutions (hospital, schools, youth village) are not in it.
- **Wikipedia** (Hebrew and English) was read as wikitext of the current revision, cited by `oldid`. It is a pointer
  and a cross-check: a fact resting on Wikipedia alone is tagged **[S, Wikipedia]**, and where it cites a newspaper or
  book the citation is given as Wikipedia gives it and not counted as read. Quotes from Wikipedia are from the
  wikitext with link, footnote and bold/italic markup removed (a piped link shows its display text); nothing else is
  changed. Where a sentence is reworded, it says "paraphrase".
- **Founding group** in Gvati is the affiliation (the settlement movement), not the people. The people (country of
  origin, garin) come from Wikipedia and are tagged as such.
- Hebrew dates were converted with the `convertdate` library.
- **Site notes.** Ten rows sit, by the Wikipedia articles, on or next to the lands of an Arab village depopulated in
  1948. Each such row has a **Site note** that names the village where the article does (both articles for row 4 name
  Ein Kerem), for the owner's sensitivity review. They are recorded, not weighed.

Tags, one per fact, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. Both values and the sources on each side are given, and which we prefer.
- **[GAP]**: not found in any source read.

CBS year plus Gvati day agreeing on the year counts as [V] for the year; the day stays [S] unless a second source
gives the same day.

### Sources not reached

- **Mateh Yehuda regional council site**, `https://www.m-yehuda.org.il/`: HTTP 403 to `curl`. Two archived council
  pages that Hebrew Wikipedia links (Sdot Micha,
  https://web.archive.org/web/20130922090741/http://www.m-yehuda.org.il/sdotmicha/ ; Tirosh,
  https://web.archive.org/web/20120328061537/http://www.m-yehuda.org.il/tirush/) were reached, but the archived pages
  carry no founding text.
- **Not tried, known blocked** (`settlements-1949-51.md`): the NLI historical press (JPress) and the Israel State
  Archives. So **no contemporaneous newspaper was read for any row**. Books cited by Wikipedia and not read: Emanuel
  Hareuveni, *Lexicon of the Land of Israel* (2010), p. 483, and Ze'ev Ener, *Kol Makom ve-Atar* (2000), p. 172 (both
  for Kesalon).
- **Localities founded 1952-56 that no longer exist** are not in the CBS file and not in Gvati. They were not searched
  for beyond the Wikipedia categories above. **[GAP]**

### Near misses, not rows

- **Beit Shemesh** (CBS code 2610): CBS 1950. Hebrew Wikipedia: "העיר הוקמה בכ"ז בכסלו ה'תשי"א (6 בדצמבר 1950)", first
  as "מעברת הר־טוב"; "בשנת 1953 שונה שמה של "מעברת הר־טוב" לשם "בית שמש"" (renamed Beit Shemesh in 1953); "בשנת 1952
  עברה המעברה למבני קבע" (moved to permanent buildings in 1952). English Wikipedia: "On 6 December 1950, the Hartuv
  displaced persons camp "Ma'abarat Har-Tuv" was established". So it was **founded in 1950 and is out of scope**; the
  1952 move to permanent houses and the 1953 rename fall inside the window and are noted here only. It is row 16 of
  `settlements-1949-51.md`, and the owner gave its sensitivity sign-off on 17 Oct 2026 (it joins Phase 4's founding-site list)
  (https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6233776625). Sources:
  https://he.wikipedia.org/w/index.php?oldid=43925345 ; https://en.wikipedia.org/w/index.php?oldid=1380233679 . Checked
  2026-10-22.
- **CBS district-1 rows just outside the window**, not researched further: 1951, Mevaseret Zion (code 1015) and Beko'a
  (row 5 below, kept as a conflict); 1958, Aviezer, Givat Yeshayahu, Neve Michael and Tzafririm (Gvati gives 1958 for
  Aviezer, Givat Yeshayahu and Tzafririm too).

## Summary table

Coordinates: CBS 2024, lat N, lon E, today's site. "Founded" is the best-supported date; see the row for the sources.

| # | Name | Type | Founded | Lat, lon (CBS) | Renames |
|---|---|---|---|---|---|
| 1 | Eitanim / איתנים | Tuberculosis hospital, later psychiatric hospital | Oct 1950 (Hebrew Wikipedia), 1950 (English Wikipedia) or 1952 (CBS) **[C]** | 31.77852, 35.09574 | "בית חולים איתנים", now "המרכז לבריאות הנפש איתנים" |
| 2 | Kesalon / כסלון | Transit camp (ma'abara), then moshav | 1952 moshav **[V]**, 4 Dec 1952 **[S]**; ma'abara 1950 **[S, Wikipedia]** | 31.77408, 35.04930 | Earlier "Shufa" or "Khirbat Shufa" |
| 3 | Kiryat Ye'arim Youth Village / כפר הנוער קריית יערים | Youth village | 1952 **[V]** | 31.81161, 35.10412 | CBS name "קריית יערים (מוסד)" |
| 4 | Ein Kerem agricultural school / בית ספר אזורי עין כרם | Agricultural school | Oct 1952 (Hebrew Wikipedia) or 1948 (English Wikipedia); no CBS year **[C]** | 31.77819, 35.15305 | First "בית הספר החקלאי עין כרם" |
| 5 | Beko'a / בקוע | Moshav | 1951 (CBS; Gvati 4 Dec 1951) or 1952 (both Wikipedias) **[C]** | 31.82879, 34.92570 | None found |
| 6 | Givat Shemesh / גבעת שמש | Educational institution | Oct 1953 (Hebrew Wikipedia text) or 1954 (English Wikipedia, both category lists); no CBS year **[C]** | 31.77489, 34.95165 | None found |
| 7 | Gefen / גפן | Moshav | 8 Sep 1955 **[V]** | 31.74075, 34.87853 | None found |
| 8 | Sdot Micha / שדות מיכה | Moshav | 29 Sep 1955 **[S]**; 1955 **[V]** | 31.72116, 34.92045 | None found |
| 9 | Luzit / לוזית | Moshav | 27 Oct 1955 **[S]**; 1955 **[V]** | 31.68694, 34.88653 | First "Dir Duban B" |
| 10 | Tirosh / תירוש | Moshav | 1955 **[V]**; day **[GAP]** | 31.75066, 34.88598 | None found |
| 11 | Nehusha / נחושה | Nahal outpost (1955), cooperative moshav (1957), refounded 1981 | Sep 1955 **[S, Wikipedia]** or 1982 (CBS) **[C]** | 31.62740, 34.95233 | None found |

Rows: 11. By type: 6 moshavim (Kesalon, Beko'a, Gefen, Sdot Micha, Luzit, Tirosh), 1 Nahal outpost that became a
moshav (Nehusha), 4 institutions (Eitanim hospital, Kiryat Ye'arim Youth Village, the Ein Kerem agricultural school,
Givat Shemesh). No town and no kibbutz was founded in the district in the window by any source read. Rows are ordered roughly
by date, CBS-dated rows first.

## Rows

Common sources, cited by short name in every row:
- **CBS**: https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22).
- **Gvati**: https://benyehuda.org/read/14684 , appendix table (checked 2026-10-22).

### 1. Eitanim (איתנים), hospital

- **Founded: [C].** CBS: 1952 (row "איתנים", code 886). Hebrew Wikipedia: "בית החולים הוקם באוקטובר 1950 על הר
  איתנים" (founded October 1950), and "באפריל 1952 התקיים טקס חנוכה חגיגי של בית החולים" (a dedication ceremony in
  April 1952, citing Davar, 2 Apr 1952, not read). English Wikipedia: "In 1950 a hospital for people with tuberculosis
  was founded on the site." **We prefer October 1950 for the hospital** (two sources); the CBS 1952 may follow the
  dedication, but that is our reading. If the window is taken by the CBS year it is in; by the Wikipedia year it is
  out. Not in Gvati.
- **Type. [S, Wikipedia]** Hebrew Wikipedia: founded as a tuberculosis hospital for children; turned into a psychiatric
  hospital in 1958; part of the Jerusalem Mental Health Center since 2005. It is a hospital, not a residential
  settlement.
- **Founders. [GAP]** No founding group; Hebrew Wikipedia names the director from 1951, Maurizio Mendes.
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: on Mount Eitanim, where the village "דיר עמר" stood until 1948;
  English Wikipedia: "Until 1948, there was a village in the area by the name of Dayr 'Amr." Given as the sources word
  it, for the owner's sensitivity review.
- **Renames. [S, Wikipedia]** Formerly "בית חולים איתנים"; now "המרכז לבריאות הנפש איתנים" (Hebrew Wikipedia title).
- **Coordinates.** CBS 31.77852, 35.09574 (code 886).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44096756 ;
  https://en.wikipedia.org/w/index.php?oldid=1374529777 . All checked 2026-10-22.

### 2. Kesalon (כסלון), transit camp, then moshav

- **Founded 1952 as a moshav. [V]** CBS: 1952. Gvati: "כְּסָלוֹן", region "הרי יהודה", 4.12.52, affiliation HaIhud
  HaHaklai. Hebrew Wikipedia: "בשנת 1952 הפכה המעברה למושב בה יושבו עולי תימן שעזבו את עגור" (in 1952 the ma'abara
  became a moshav, settled by Yemenite immigrants who had left Agur). English Wikipedia: category "Populated places in
  Israel established in 1952". **Day 4 December 1952 [S]** (Gvati only).
- **Earlier camp. [S, Wikipedia]** A ma'abara on the site first. English Wikipedia: "The first transit camp
  (ma'abara) for new Jewish immigrants was set up in 1950"; Hebrew Wikipedia places it during Operation On Eagles'
  Wings (1949-1950), citing press of 1950 (Maariv, 28 Jul 1950; 9 Jun 1950), not read. The 1950 camp is before the window; the moshav is in it.
- **Founders. [S, Wikipedia]** Yemenite immigrants (both Wikipedias). Hebrew Wikipedia: they later left, 37 families
  from Romania came, the place was abandoned again, and "והוא אוכלס מחדש בשנת 1955 על ידי עולים מתוניסיה ומרוקו"
  (resettled in 1955 by immigrants from Tunisia and Morocco). English Wikipedia names only Moroccans as the
  newcomers.
- **Site note. [S, Wikipedia]** Both Wikipedias: on the lands of the Arab village of Kasla, depopulated in 1948
  (paraphrase; Hebrew Wikipedia adds it was taken in Operation Dani). Hebrew Wikipedia: "שם היישוב הקדום השתמר בשם
  הכפר הערבי "כסלא", שתושביו נמלטו ממנו בזמן מלחמת העצמאות, ולא הורשו לחזור אליו לאחריה". For the owner's
  sensitivity review.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "הועבר השם למושב כסלון, שנקרא קודם "שופה" או "חורבת שופה""
  (citing Yalkut HaPirsumim 277). Name origin, Gvati: "יישוב קדום בנחלת שבט יהודה (יהושע ט"ו, י')".
- **Coordinates.** CBS 31.77408, 35.04930 (code 859).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43982218 ;
  https://en.wikipedia.org/w/index.php?oldid=1374533185 . All checked 2026-10-22.

### 3. Kiryat Ye'arim Youth Village (כפר הנוער קריית יערים), youth village

- **Founded 1952. [V]** CBS: 1952 (row "קריית יערים (מוסד)", code 2039). Hebrew Wikipedia: "כפר הנוער נוסד ב-1952
  בתרומת יהודי שווייץ ובסיוע הסוכנות היהודית". English Wikipedia: "The village was established in 1952 with a donation
  from Swiss Jews." The day is **[GAP]**. Not in Gvati.
- **Type.** A youth village, not a moshav or kibbutz (both Wikipedias; Hebrew Wikipedia adds a boarding house).
- **Founders. [S, Wikipedia]** Funded by Swiss Jews with the Jewish Agency (quote above); no founding group of
  residents named. Run in 1954-1959 by Yisrael Katz, later a minister (both Wikipedias).
- **Renames.** None found. It is a separate CBS row from the later locality Kiryat Ye'arim (code 1137, CBS 1975).
- **Coordinates.** CBS 31.81161, 35.10412 (code 2039).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=41157563 ;
  https://en.wikipedia.org/w/index.php?oldid=1374533061 . All checked 2026-10-22.

### 4. Ein Kerem agricultural school (בית ספר אזורי עין כרם), agricultural school

- **Founded: [C].** CBS: the row "עין כרם-בי"ס חקלאי" (code 1056) is in district 1 with no founding year. Hebrew
  Wikipedia: "בית הספר החקלאי נחנך באוקטובר 1952 במשכנו הזמני" (opened October 1952 in temporary premises, citing
  HaTzofe, 30 Oct 1952, not read), in buildings of Rachel Yanait Ben-Zvi's training farm, moved to Ein Kerem from the
  demilitarised zone east of Talpiot after the war. English Wikipedia: "Ein Kerem school opened in 1948", infobox
  "founded = 1948". **No preference**: the two articles may be dating different things (the farm's move and the
  school's opening), but neither says so; that is our reading.
- **Type.** An agricultural boarding school, today a regional school of Mateh Yehuda (Hebrew Wikipedia). Not a
  residential settlement.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia names Nissim Mashiah among the founders.
- **First site. [S, Wikipedia]** Hebrew Wikipedia: "הוקם בשנת 1952 במבנים אשר שימשו את חוות הלימוד שייסדה רחל ינאית
  בן-צבי, בשכונת עין כרם שבירושלים ... בשנת 1960 עבר בית הספר אל משכנו הנוכחי מחוץ לעיר". The CBS point is the
  present campus occupied from 1960, not the first site.
- **Site note. [S, Wikipedia]** Hebrew Wikipedia calls the Ein Kerem buildings "שננטשו על ידי בעליהם הערבים". English
  Wikipedia says the school was in "abandoned buildings in Ein Kerem that belonged to Palestinians after they fled
  during the events of the Nakba of 1948". For the owner's sensitivity review.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: earlier name "בית הספר החקלאי עין כרם".
- **Coordinates.** CBS 31.77819, 35.15305 (code 1056).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43479684 ;
  https://en.wikipedia.org/w/index.php?oldid=1374529643 . All checked 2026-10-22.

### 5. Beko'a (בקוע), moshav

- **Founded: [C], year.** CBS: 1951. Gvati: "בְּקוֹעַ", region "שפלת יהודה", 4.12.51, affiliation Tnuat HaMoshavim.
  Hebrew Wikipedia: "היישוב הוקם בשנת 1952 על ידי עולים מתימן". English Wikipedia: "Beko'a was established in 1952 by
  immigrants from Yemen". **We prefer 1951** (CBS and Gvati, the two non-wiki sources), which puts it outside the
  window; kept so the owner can see the disagreement. Gvati gives the same day and month, 4 December, to Kesalon (row
  2) one year later; whether either is a misprint is not known (our observation, no source says so).
- **Founders. [S, Wikipedia]** Immigrants from Yemen (both Wikipedias).
- **Site note. [S, Wikipedia]** English Wikipedia: on land of the Arab village of Dayr Muhaysin, depopulated in April
  1948 during Operation Nachshon (paraphrase); in its words, "which was completely depopulated by Jewish Haganah
  forces on April 6, 1948 ... The surviving inhabitants had fled in fear of further Haganah attacks." Hebrew Wikipedia
  does not mention it. For the owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: "סמלי"; Hebrew Wikipedia: for the breaking of the siege of Jerusalem by
  the Burma Road, which began near the moshav.
- **Coordinates.** CBS 31.82879, 34.92570 (code 864).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43347486 ;
  https://en.wikipedia.org/w/index.php?oldid=1374528888 . All checked 2026-10-22.

### 6. Givat Shemesh (גבעת שמש), educational institution

- **Founded: [C].** CBS: the row "גבעת שמש" (code 566) is in district 1 with no founding year. Hebrew Wikipedia text:
  "המוסד נפתח באוקטובר 1953" (opened October 1953), for young people with intellectual disabilities, on the initiative
  of the AKIM association with the Welfare Ministry, in a building rented from the Deir Rafat monastery (paraphrase
  after the quoted words; citing Davar, 7 Sep 1953, not read). Hebrew Wikipedia's category and English Wikipedia
  (infobox "founded = 1954", category "Populated places established in 1954") give 1954. **We prefer October 1953**,
  the one dated statement; 1954 is given with no event. Not in Gvati.
- **Type. [S, Wikipedia]** An institution, not a residential settlement: a home for young people with intellectual
  disabilities, later run privately (Hebrew Wikipedia); today the Retorno rehabilitation centre (English Wikipedia,
  and the body text of Hebrew Wikipedia).
- **Founders. [S, Wikipedia]** AKIM and the Welfare Ministry (Hebrew Wikipedia); 45 residents at the opening.
- **Site note. [S, Wikipedia]** English Wikipedia: on the land of the Arab village of Dayr Rafat, depopulated in July
  1948, just west of the village site (paraphrase); in its words, "most of the inhabitants had already left the area
  and the few who remained at each village were expelled." For the owner's sensitivity review.
- **Renames.** None found.
- **Coordinates.** CBS 31.77489, 34.95165 (code 566).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=40909012 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530898 . All checked 2026-10-22.

### 7. Gefen (גפן), moshav

- **Founded 8 September 1955. [V]** Gvati: "גֶפֶן", region "שפלת יהודה", 8.9.55, affiliation HaPoel HaMizrachi. Hebrew
  Wikipedia: "שהוקם ב-8 בספטמבר 1955 (כ"א באלול ה'תשט"ו), על ידי עולים ממרוקו"; 21 Elul 5715 is 8 September 1955
  (`convertdate`). CBS: 1955. English Wikipedia: 1955.
- **Founders. [S, Wikipedia]** Immigrants from Morocco (both Wikipedias); Hebrew Wikipedia: mostly from villages in the
  High Atlas, arriving in 1955-1956, some straight to the moshav, some from nearby ma'abarot.
- **Site note. [S, Wikipedia]** English Wikipedia: on land that had belonged to the Arab village of Mughallis, depopulated
  in 1948 (paraphrase). For the owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: "סמלי"; Hebrew Wikipedia: Psalms 80:15 and the vineyards of the area.
- **Coordinates.** CBS 31.74075, 34.87853 (code 39).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43761613 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530436 . All checked 2026-10-22.

### 8. Sdot Micha (שדות מיכה), moshav

- **Founded 29 September 1955. [S]** Gvati: "שְדוֹת מִיכָה", region "שפלת יהודה", 29.9.55, affiliation Tnuat HaMoshavim.
  **Year [V]**: CBS 1955; Hebrew Wikipedia "היישוב הוקם בשנת 1955"; English Wikipedia 1955.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "כחמישים משפחות עולים ממרוקו" (about fifty families of Moroccan
  immigrants); English Wikipedia: immigrants from Morocco.
- **Site note. [S, Wikipedia]** English Wikipedia: on the former land of the Arab village of Al-Burayj, depopulated in
  1948 (paraphrase). For the owner's sensitivity review.
- **Renames.** None found. Named after the writer Micha Josef Berdyczewski (Gvati and both Wikipedias).
- **Coordinates.** CBS 31.72116, 34.92045 (code 27).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42331096 ;
  https://en.wikipedia.org/w/index.php?oldid=1374536720 . All checked 2026-10-22.

### 9. Luzit (לוזית), moshav

- **Founded 27 October 1955. [S]** Gvati: "לוּזִית", region "שפלת יהודה", 27.10.55, affiliation Tnuat HaMoshavim.
  **Year [V]**: CBS 1955; Hebrew Wikipedia "היישוב הוקם בשנת 1955 על ידי עולים מצפון אפריקה"; English Wikipedia 1955.
- **Founders. [S, Wikipedia]** Immigrants from North Africa, later joined by Algerian immigrants (Hebrew Wikipedia);
  English Wikipedia names the founding group's leader.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "ונקרא תחילה בשם "דיר דובן ב'"" (first called Dir Duban B); English
  Wikipedia: "It was initially named Dir Duban Bet". Then Luzit, after the almond (luz) groves (Gvati, both
  Wikipedias). No date for the change is given.
- **Site note. [S, Wikipedia]** Both Wikipedias: the first name came from the Arab village of Deir al-Dubban that stood
  on the site (English Wikipedia: depopulated) (paraphrase). For the owner's sensitivity review.
- **Coordinates.** CBS 31.68694, 34.88653 (code 52).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42331899 ;
  https://en.wikipedia.org/w/index.php?oldid=1374533379 . All checked 2026-10-22.

### 10. Tirosh (תירוש), moshav

- **Founded 1955. [V]** CBS: 1955. Hebrew Wikipedia: "המושב תירוש הוקם בשנת 1955 על ידי עולים מצפון אפריקה". English
  Wikipedia: "The village was established in 1955". Gvati: "תִּירוֹש", region "שפלת יהודה", affiliation HaPoel
  HaMizrachi, date printed "67.55" (so in the page's HTML). That is not a valid day and month; it may be 6.7.55 with a
  dot missing, but that is our reading. **Day [GAP].**
- **Founders. [S, Wikipedia]** Immigrants from North Africa (both Wikipedias).
- **Site note. [S, Wikipedia]** English Wikipedia: on land of the Arab village of 'Ajjur, depopulated in 1948
  (paraphrase). Hebrew Wikipedia: "המושב הוקם על אדמות הכפר הערבי מוע'ליס, שתושביו עזבוהו במהלך מלחמת העצמאות ולא
  הורשו לחזור אליו לאחריה"; its link to the village page "עג'ור" displays the name "מוע'ליס" (the village English
  Wikipedia gives for Gefen, row 7), so the Hebrew page is internally unclear. Not resolved. For the owner's
  sensitivity review.
- **Renames.** None found. Name origin, Gvati: "סמלי"; Hebrew Wikipedia: the area's vineyards.
- **Coordinates.** CBS 31.75066, 34.88598 (code 10).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43546231 ;
  https://en.wikipedia.org/w/index.php?oldid=1374537584 . All checked 2026-10-22.

### 11. Nehusha (נחושה), Nahal outpost, then moshav

- **Founded: [C].** CBS: 1982. Hebrew Wikipedia: "נחושה נוסדה בספטמבר 1955 כהיאחזות נח"ל על ידי גרעין של הנוער העובד
  הדתי" (founded September 1955 as a Nahal outpost by a garin of HaNoar HaOved HaDati, citing Haaretz, 7 Sep 1955, not
  read); civilianised as a cooperative moshav on 18 November 1957; the settlers later left; "בשנת 1981 הוקמה נחושה
  מחדש כמושב עובדים" (refounded in 1981). English Wikipedia: "The moshav was established in 1955", "Although the
  village collapsed in 1968, it was re-established in 1981." Not in Gvati. **We prefer September 1955 for the first
  settlement** (both Wikipedias, with a dated newspaper citation); the CBS 1982 fits the refounding (our reading, the
  file does not say).
- **Founders. [S, Wikipedia]** A garin of HaNoar HaOved HaDati (Hebrew Wikipedia). Hebrew Wikipedia quotes HaTzofe of
  18 Nov 1957 (not read) for members from Yemen, Tripoli, Morocco, France, Poland, Romania and Israel.
- **Site note. [S, Wikipedia]** English Wikipedia: on land of the Arab village of Umm Burj, depopulated in 1948
  (paraphrase). Hebrew Wikipedia instead links the name to a small Arab village, Deir Nakhkhas, whose remains are in the
  nearby forest. For the owner's sensitivity review.
- **Renames.** None found. Name from Psalms 18:35 and possibly Ir Nahash (Hebrew Wikipedia).
- **Coordinates.** CBS 31.62740, 34.95233 (code 59). This is today's locality; whether the 1955 outpost stood on the
  same spot is **[GAP]**.
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44011711 ;
  https://en.wikipedia.org/w/index.php?oldid=1379795818 . All checked 2026-10-22.

## Gaps and notes for the owner

- **Small region.** By the CBS year only 7 localities qualify, 2 of them institutions; with the Wikipedia conflicts,
  11. The district's big wave of moshavim came in 1949-1950 (`settlements-1949-51.md`) and the next in 1958 (near
  misses above).
- **Institutions.** Four rows are a hospital, two schools and a youth village. They are CBS localities, but whether
  the map wants them is the owner's call.
- **Defunct localities** founded 1952-56 in the district were not searched for beyond the Wikipedia categories.
- **No newspaper of the time was read**; every newspaper date above is as Wikipedia cites it.
- **Days** confirmed by two sources only for Gefen; Kesalon, Sdot Micha and Luzit have Gvati's day only; Beko'a has
  Gvati's day inside a year conflict; Tirosh's Gvati day is unreadable; the rest have a month or a year. Gefen's two
  sources (Gvati, Hebrew Wikipedia) were not checked for independence.
- **Site notes** are recorded for ten rows (Eitanim, Kesalon, the Ein Kerem school, Beko'a, Givat Shemesh, Gefen,
  Sdot Micha, Luzit, Tirosh, Nehusha; Eitanim, Kesalon, the Ein Kerem school and Luzit from both Wikipedias, the
  others mostly from English Wikipedia citing Khalidi, *All That Remains*, 1992, not read). Hexes and armistice-line flags were not computed;
  they belong to the paused data-file step.
