# Settlements founded in 1952-1956: the Haifa and Tel Aviv Districts (research note, 2026-10-23)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file and the
tool change (#61) stay paused until after the MVP. Refs #27, its 1952-56 rows, two regions in one note. It follows the
format of [`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) (#485), which covers 1949-1951; those
rows are not repeated here. Sibling notes for other regions: the Negev (#1366), the Ashkelon subdistrict (#1369), the
Northern District (#1380) and the Jerusalem District (#1381). The Central District is claimed separately on #27.

The question: which Jewish localities were founded in the Haifa District and the Tel Aviv District between 1 January
1952 and 31 December 1956, and for each: founding date, coordinates, type, founding group, renames and sources.

## Which "Haifa District" and "Tel Aviv District"

**Boundary used: the Haifa District (מחוז חיפה, code 3) and the Tel Aviv District (מחוז תל אביב, code 5) of the
Central Bureau of Statistics**, as given in the CBS localities file 2024 (columns "סמל מחוז", "שם מחוז", "סמל נפה" and
"שם נפה"; source below).

- **District 3** has two subdistricts in that file: 31, Haifa (27 rows: the city, the Krayot, Tirat Carmel, Nesher, the
  Zevulun valley, the Carmel Druze towns) and 32, Hadera (82 rows: Hadera, the Carmel coast, Zikhron Ya'akov, the
  Menashe and Alona areas and Wadi Ara). **All 9 CBS rows in the window are in subdistrict 32.**
- **District 5** has 18 rows in the file, under subdistrict codes 51, 52 and 53, all three named "תל אביב". This note
  takes all of district 5, not only code 51. **No district-5 row has a founding year in the window, and no other
  source read adds one** (see "Tel Aviv District: no rows" below).

It leaves out the Northern District (#1380) and the Central District (the Sharon, Petah Tikva, Ramla and Rehovot
subdistricts), so moshavim and kibbutzim of the southern Sharon that Gvati lists in the window (for example Einat,
Ganot or Tnuvot) are not here. This line was chosen because it is the one the official source applies to every row.
**Other regions of #27 stay open to other claimants, and these regions could be split differently if the owner
prefers another line.**

Haifa (code 4000, founding year "ותיק", old) and Tel Aviv-Yafo (code 5000, 1909) are single rows in the CBS file and
are not candidates; neighbourhoods founded inside the cities in 1952-56 are not localities in the CBS sense and were
not searched for.

## How it was done

- Every page was fetched on 23 October 2026 with `curl` or Python `urllib` (User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`), Wikipedia and Wikidata through
  the MediaWiki API. No request carried a personal identifier. No web search result was used as a source.
- **The candidate pool** is the union of three lists:
  1. every row of the CBS localities file 2024 in district 3 or 5 whose "שנת ייסוד" (year of founding) is 1952 to
     1956: **9 rows**, all in district 3;
  2. every row of Haim Gvati's list of rural settlements whose "תאריך הקמה" (date of founding) falls in 1952-1956 and
     whose name matches a district-3 or district-5 row of the CBS file: **5 rows**, all of them already in list 1;
  3. the Hebrew Wikipedia categories "יישובים שהוקמו ב-1952" to "יישובים שהוקמו ב-1956" intersected with "יישובים
     במחוז חיפה" and with "יישובים במחוז תל אביב". A page counts as in a category if it is a direct member, or if the
     category has a subcategory of the same name (for example "קטגוריה:עין הוד" inside "יישובים שהוקמו ב-1953"). The
     pages in the district's type subcategories ("מושבים במחוז חיפה", "קיבוצים במחוז חיפה", "ערים במחוז חיפה",
     "ישראל: מועצות מקומיות במחוז חיפה", and the same for Tel Aviv) were added too; that step added no new page.
     Result: **12 pages for the Haifa District, none for the Tel Aviv District.** One of the 12 is a regional council,
     not a locality (Alona, near misses below). The other 11 cover all 9 CBS rows and add 2 (HaHotrim, Yemin Orde),
     each kept with **[C]** or as an institution with no CBS row.

  That gives **11 rows**.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024 ("קובץ יישובים
  2024"), https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23), columns
  "שנת ייסוד" and "קואורדינטות". As in `settlements-1949-51.md`, the twelve-digit coordinate field is read as an Israel
  Transverse Mercator easting and northing (six digits each) and converted with `pyproj` (EPSG:2039 to EPSG:4326).
  That reading is ours, not stated in the file; the 1949-51 note checked it against Wikipedia for 29 rows (median
  about 40 m). Coordinates below are the CBS values, rounded to five decimals. They are where the locality is
  **today**; where a source says the first camp stood elsewhere, the row says so. One row (Yemin Orde) has no CBS row;
  its coordinates come from Wikidata and are tagged so.
- **Founding dates (day).** Gvati, *100 Years of Settlement*, vol. 2, appendix "רשימת כל הישובים הכפריים היהודים
  בישראל" (list of all Jewish rural settlements in Israel), Ben-Yehuda Project edition, https://benyehuda.org/read/14684
  (checked 2026-10-23). Its columns are name, region ("אזור גיאוגרפי"), date of founding ("תאריך הקמה"), affiliation
  ("השתייכות ארגונית") and origin of the name ("מקור השם"). The rows used here are in its sections "קיבוצים" (Maggal,
  Mezer, HaHotrim) and "מושבים" (Ma'or, Givat Nili, Sde Yitzhak, Talmei Elazar). The column does not say whether the
  date is the day on the land or a later recognition. It is one secondary compilation. Institutions (youth villages,
  schools) and the artists' village Ein Hod are not in it.
- **Wikipedia** (Hebrew and English) was read as wikitext of the current revision, cited by `oldid`. It is a pointer
  and a cross-check: a fact resting on Wikipedia alone is tagged **[S, Wikipedia]**, and where it cites a newspaper or
  book the citation is given as Wikipedia gives it and not counted as read. Quotes from Wikipedia are from the
  wikitext with link, footnote and bold/italic markup removed (a piped link shows its display text); nothing else is
  changed. Where a sentence is reworded, it says "paraphrase".
- **Founding group** in Gvati is the affiliation (the settlement movement), not the people. The people (country of
  origin, garin) come from Wikipedia and are tagged as such.
- **Site notes.** Four rows sit, by the Wikipedia articles, on, near or next to the lands of an Arab village depopulated
  in 1948 (Ein Hod, Givat Nili, Talmei Elazar, HaHotrim). Two more rows have a note on land the article ties to Arab
  neighbours (Maggal, Mezer). Each such row has a one-line **Site note** in a neutral paraphrase, for the owner's
  sensitivity review. They are recorded, not weighed.

Tags, one per fact, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. Both values and the sources on each side are given, and which we prefer.
- **[GAP]**: not found in any source read.

CBS year plus Gvati day agreeing on the year counts as [V] for the year; the day stays [S] unless a second source
gives the same day.

### Sources not reached

- **Not tried, known blocked** (`settlements-1949-51.md`): the NLI historical press (JPress) and the Israel State
  Archives. So **no contemporaneous newspaper was read for any row**. The regional council sites (Hof HaCarmel,
  Menashe, Alona) were not tried. Books cited by Wikipedia and not read: Emanuel Hareuveni, *Lexicon of the Land of
  Israel* (2010) (for Kfar Tzvi Sitrin); Menachem and Ephraim Talmi, *Kol ha-Aretz* (1966), p. 387 (for Maggal);
  Walid Khalidi, *All That Remains* (1992), pp. 189, 198 and 199 (for Talmei Elazar, HaHotrim and Givat Nili). The
  1952 newsreel "טקס ההקמה של כפר סיטרין" (Carmel newsreel, linked from Hebrew Wikipedia) was not watched.
- **Localities founded 1952-56 that no longer exist** are not in the CBS file and not in Gvati. They were not searched
  for beyond the Wikipedia categories above. **[GAP]**

### Near misses, not rows

- **Or Akiva** (CBS code 1020): CBS 1951. It is row 21 of `settlements-1949-51.md` (founded 17 April 1951 there) and is
  out of scope.
- **Alona Regional Council**: in "יישובים שהוקמו ב-1953" on Hebrew Wikipedia ("המועצה קיבלה מעמד מוניציפלי בשנת
  1953"), but it is a regional council over three moshavim (Amikam, Aviel, Givat Nili), not a locality. Its one
  1953 moshav is row 5. Source: https://he.wikipedia.org/w/index.php?oldid=43946688 (checked 2026-10-23).
- **CBS district-3 and district-5 rows just outside the window**, not researched further: 1950, Or Yehuda (code 2400,
  the only district-5 row founded after 1948), Beit Hananya, Kfar Hasidim B, Nir Etzion, Ofer and Amikam; 1951, Or
  Akiva and Oranim; 1957, Rekhasim (code 922). Sde Yitzhak (row 2) is in the window by the CBS year but not by Gvati's.

## Summary table

Coordinates: CBS 2024, lat N, lon E, today's site (Yemin Orde: Wikidata). "Founded" is the best-supported date; see the
row for the sources.

| # | Name | Type | Founded | Lat, lon (CBS) | Renames |
|---|---|---|---|---|---|
| 1 | Talmei Elazar / תלמי אלעזר | Moshav | 16 Sep 1952 **[S]**; 1952 **[V]** | 32.44527, 34.97743 | None found |
| 2 | Sde Yitzhak / שדה יצחק | Moshav | 1952 (CBS, both Wikipedias) or 22 Oct 1962 (Gvati) **[C]** | 32.40513, 34.99339 | None found |
| 3 | Kfar Galim / כפר גלים | Youth village, agricultural school | 1952 (CBS, English Wikipedia) or autumn 1954 school opening (Hebrew Wikipedia text) **[C]** | 32.76622, 34.95891 | None found |
| 4 | Ma'or / מאור | Moshav | 2 Jun 1953 **[S]**; 1953 **[V]**; refounded 1957 **[S, Wikipedia]** | 32.42452, 35.00606 | First "ארבע ארצות" |
| 5 | Givat Nili / גבעת ניל"י | Moshav | 13 Jul 1953 **[S]**; 1953 **[V]** | 32.54776, 35.04141 | None found |
| 6 | Mezer / מצר | Kibbutz | 8 Sep 1953 **[V]** | 32.44005, 35.04749 | None found |
| 7 | Maggal / מגל | Kibbutz (Nahal outpost) | 2 Nov 1953 **[V]** | 32.38573, 35.03325 | None found |
| 8 | Kfar Tzvi Sitrin / כפר צבי סיטרין (CBS "בית צבי") | Religious vocational school, youth village | 1953 (CBS, English Wikipedia) or 1952 (Hebrew Wikipedia text) **[C]** | 32.71972, 34.96968 | Also called "בית צבי" |
| 9 | Ein Hod / עין הוד | Artists' village | 5 Aug 1953 (Hebrew Wikipedia) or 1954 (CBS) **[C]** | 32.69979, 34.98309 | None in the window |
| 10 | HaHotrim / החותרים | Kibbutz | 1948 (CBS, Gvati 7 Jun 1948) at a first site; present site 1952 (both Wikipedias) **[C]** | 32.75244, 34.95679 | None found |
| 11 | Yemin Orde / ימין אורד | Youth village | 1953 **[S, Wikipedia]**; no CBS row | 32.70239, 34.98762 (Wikidata) | First "כפר וינגייט" |

Rows: 11, all in the Haifa District (subdistrict 32 for the 10 with a CBS row; Yemin Orde has none). By type: 4
moshavim (Talmei Elazar, Sde Yitzhak, Ma'or, Givat Nili), 3 kibbutzim (Mezer, Maggal, HaHotrim), 1 artists' village
(Ein Hod), 3 institutions (Kfar Galim, Kfar Tzvi Sitrin, Yemin Orde). No town was founded in either district in the
window by any source read. Rows are ordered roughly by date, CBS-dated rows first.

## Tel Aviv District: no rows

- **CBS.** The 18 district-5 rows have founding years from 1870 (Mikveh Israel) to 1950 (Or Yehuda), or none (the four
  area rows, codes 1905, 1907, 1908 and 1909). None is 1952-1956.
- **Gvati.** The district-5 names it lists (Gelil Yam 7.1.43, Ramat HaSharon 1923, Kfar Shmaryahu 1937) are all before
  the window.
- **Hebrew Wikipedia.** The intersection of "יישובים במחוז תל אביב" with the five year categories is empty.

So the district gets no row. Housing estates and ma'abarot inside the cities of the district (Tel Aviv-Yafo, Ramat Gan,
Holon, Bat Yam and others) were not localities in the CBS sense and were not searched for. **[GAP]** for anything
below locality level.

## Rows

Common sources, cited by short name in every row:
- **CBS**: https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23).
- **Gvati**: https://benyehuda.org/read/14684 , appendix table (checked 2026-10-23).

### 1. Talmei Elazar (תלמי אלעזר), moshav

- **Founded 16 September 1952. [S]** Gvati: "תַּלְמֵי אֶלְעָזָר", region "השרון הצפוני", 16.9.52, affiliation
  "התאחדות האיכרים" (the Farmers' Federation). **Year [V]**: CBS 1952 (code 2003). Hebrew Wikipedia: "אבן פינה ליישוב
  הונחה בספטמבר 1952 ובמאי 1953 נחנכו 30 בתים" (a cornerstone in September 1952, 30 houses dedicated in May 1953,
  citing Davar, 15 May 1953, not read). English Wikipedia: "The village was established in 1952".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: set up for the middle class by PICA with the Jewish Agency's
  settlement department and the Farmers' Federation; the settlers were sons of farmers from Zikhron Ya'akov and other
  moshavot and new immigrants from Europe, Iran and North Africa (paraphrase, citing Davar, 17 Sep 1952, not read).
  English Wikipedia: residents of Zikhron Ya'akov and immigrants from Eastern Europe and Iran.
- **Site note. [S, Wikipedia]** English Wikipedia: on land that had belonged to the depopulated village of Khirbat
  al-Sarkas, on a site east of the village (paraphrase, citing Khalidi, p. 189, not read). Hebrew Wikipedia does not
  mention it. For the owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: named after one of PICA's directors ("ממנהלי פיק"א"; both
  Wikipedias say the same).
- **Coordinates.** CBS 32.44527, 34.97743 (code 2003).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42494434 ;
  https://en.wikipedia.org/w/index.php?oldid=1374537354 . All checked 2026-10-23.

### 2. Sde Yitzhak (שדה יצחק), moshav

- **Founded: [C], year.** CBS: 1952 (code 2008). Hebrew Wikipedia: "המושב הוקם בשנת 1952 על ידי עולים מפולין, פרס
  ותימן". English Wikipedia: "The village was established in 1952 on the former site of Lahavot Haviva, which had moved
  to its present location three kilometres east the previous year." Gvati: "שְדֵה-יִצְחָק", region "השרון הצפוני",
  **22.10.62**, affiliation Tnuat HaMoshavim. **We prefer 1952** (CBS and both Wikipedias), which puts it inside the
  window. What Gvati's 1962 date marks is not known; that it could be a later event is our reading, no source says so.
  The day is **[GAP]** for 1952.
- **Founders. [S, Wikipedia]** Immigrants from Poland, Persia and Yemen (Hebrew Wikipedia); English Wikipedia names only
  Poland.
- **Site.** Both Wikipedias: on the first site of kibbutz Lahavot Haviva, which moved about 3 km east (paraphrase).
- **Renames.** None found. Named after Yitzhak Sadeh, a founder of the Palmach (Gvati, English Wikipedia;
  Hebrew Wikipedia: its founder and first commander).
- **Coordinates.** CBS 32.40513, 34.99339 (code 2008).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43807298 ;
  https://en.wikipedia.org/w/index.php?oldid=1374536663 . All checked 2026-10-23.

### 3. Kfar Galim (כפר גלים), youth village

- **Founded: [C].** CBS: 1952 (code 427). English Wikipedia: "The school was established in 1952 as a partnership
  between the Haifa municipal council and the Ministry of Immigrant Absorption." Hebrew Wikipedia is in the category
  "יישובים שהוקמו ב-1952", but its text dates the building and opening later: land near Tirat Carmel was allotted in
  the early 1950s, "הכפר היה בשלבי הקמה בתחילת 1954", "ומאפריל 1954 הוחל ברישום תלמידים", and "את שנת הלימודים
  הראשונה בבית הספר החקלאי שנפתח בסתיו 1954 פתחו כ-80 תלמידים" (citing HaTzofe, 24 Feb 1954, HaBoker, 26 Apr 1954,
  Maariv, 14 May 1954, and Zmanim, 9 Nov 1954, not read). **No preference**: 1952 and autumn 1954 may date different things (the decision
  or land allotment, and the school's opening), but neither article says so; that is our reading. Both years are in
  the window. Not in Gvati.
- **Type.** An educational youth village with a boarding school (both Wikipedias). Not a residential settlement.
- **Founders. [S, Wikipedia]** The Haifa municipality and a government ministry (English Wikipedia, quote above); no
  founding group of residents named.
- **Renames.** None found.
- **Coordinates.** CBS 32.76622, 34.95891 (code 427).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43761689 ;
  https://en.wikipedia.org/w/index.php?oldid=1374532457 . All checked 2026-10-23.

### 4. Ma'or (מאור), moshav

- **Founded 2 June 1953. [S]** Gvati: "מָאוֹר", region "השרון הצפוני", 2.6.53, affiliation Tnuat HaMoshavim. **Year
  [V]**: CBS 1953 (code 2055). Hebrew Wikipedia: "המושב הוקם באמצע שנת 1953 במסגרת תנועת "מן העיר אל הכפר" בשם
  "ארבע ארצות" או "ועד ארבע ארצות", על ידי פועלים מנמל חיפה, רובם עולים מרומניה ופולין". English Wikipedia: "The
  moshav was established in 1953 by Jewish immigrants from Romania and Poland" (that sentence is tagged "citation
  needed" on the page).
- **Abandoned and refounded. [S, Wikipedia]** Hebrew Wikipedia: "בשל סיבות אלה נעזב היישוב לאחר מספר שנים" and
  "המושב יושב מחדש בשנת 1957 על ידי עולים מתימן" (abandoned after some years, resettled in 1957 by immigrants from
  Yemen). English Wikipedia says the same. The 1957 resettlement is after the window; the first founding is in it.
- **Founders. [S, Wikipedia]** Haifa port workers, mostly immigrants from Romania and Poland (quote above).
- **Renames. [S, Wikipedia]** First "ארבע ארצות" or "ועד ארבע ארצות", soon renamed Ma'or (Hebrew Wikipedia: "במהרה
  נקבע למושב השם "מאור""). No date for the change. Name origin, Gvati: "סמלי".
- **Coordinates.** CBS 32.42452, 35.00606 (code 2055).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42748668 ;
  https://en.wikipedia.org/w/index.php?oldid=1374533722 . All checked 2026-10-23.

### 5. Givat Nili (גבעת ניל"י), moshav

- **Founded 13 July 1953. [S]** Gvati: "גִבְעַת נִילִ"י", region "רמת מנשה", 13.7.53, affiliation "מושבי “חירות”"
  (the Herut moshavim). **Year [V]**: CBS 1953 (code 360). Hebrew Wikipedia: "היישוב הוקם בשנת 1953 על ידי עולים
  מעיראק, טורקיה, מרוקו ותוניסיה". English Wikipedia: "The moshav was established in 1953 by Jewish immigrants from
  Iraq, Turkey and Tunisia".
- **Founders. [S, Wikipedia]** Immigrants from Iraq, Turkey, Morocco and Tunisia (Hebrew Wikipedia); English Wikipedia
  leaves out Morocco.
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: on the lands of the abandoned village of Umm ash-Shauf, taken by the
  Irgun in 1948 (paraphrase); English Wikipedia: on land near the depopulated village of Umm ash Shauf (paraphrase,
  citing Khalidi, p. 199, not read). For the owner's sensitivity review.
- **Renames.** None found. Named after the Nili underground (Gvati: "ע"ש ארגון המחתרת ניל"י במלחמת העולם הראשונה";
  both Wikipedias).
- **Coordinates.** CBS 32.54776, 35.04141 (code 360).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43789567 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530865 . All checked 2026-10-23.

### 6. Mezer (מצר), kibbutz

- **Founded 8 September 1953. [V]** Gvati: "מֶצֶר", region "השרון הצפוני", 8.9.53, affiliation HaKibbutz HaArtzi.
  Hebrew Wikipedia: "התאריך הרשמי של ייסוד הקיבוץ נקבע ל-8 בספטמבר 1953." English Wikipedia: "The kibbutz was founded
  on 8 September 1953". CBS: 1953 (code 648). Hebrew Wikipedia adds that part of the group went onto the land in May
  1953 ("בחודש מאי 1953, חלק מחברי שכבה זו עלו להתיישב על שטח של כ-5,000 דונם"); the 8 September date is the official
  one by its own wording.
- **Founders. [S, Wikipedia]** Graduates of Hashomer Hatzair from Argentina (both Wikipedias; English Wikipedia: a
  garin of 128 immigrants), trained at Merhavia, Ga'aton, Gat, Sa'ar and HaMa'apil.
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: the land lay between the Arab villages of Meiser and Baqa al-Gharbiyye,
  with the border with Jordan 500 m from the dining hall, and Meiser supplied the first water (paraphrase). Both villages are CBS rows
  today (codes 649 and 6000), so they are not depopulated villages. Recorded for the owner's sensitivity review because
  of the border setting.
- **Renames.** None found. Name origin, Gvati: "סמלי. לפי הכפר הסמוך מיסר"; both Wikipedias: Psalms 118:5 and the
  border.
- **Coordinates.** CBS 32.44005, 35.04749 (code 648).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44019282 ;
  https://en.wikipedia.org/w/index.php?oldid=1374534208 . All checked 2026-10-23.

### 7. Maggal (מגל), kibbutz

- **Founded 2 November 1953. [V]** Gvati: "מַגָּל", region "השרון הצפוני", 2.11.53, affiliation "האיחוד" (Ihud
  HaKvutzot veHaKibbutzim, our reading of the short form). Hebrew Wikipedia: "קיבוץ מגל נוסד ב-2 בנובמבר 1953 כהיאחזות נח"ל, נחלאים י"א" (citing Al
  HaMishmar, 3 Nov 1953, not read). CBS: 1953 (code 375; CBS spelling "Maggal"). English Wikipedia: "Kibbutz Magal was
  founded in 1953 as a Nahal settlement".
- **Type.** A Nahal outpost that became a kibbutz (both Wikipedias).
- **Founders. [S, Wikipedia]** A Scouts battalion ("גדוד צופים"; the outpost was a Nahal one), first meant for the Lower Galilee and sent here at the last
  moment after attacks from across the Jordanian line (Hebrew Wikipedia, paraphrase).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "לקיבוץ הוקצו 3,600 דונם לעיבוד חקלאי שכללו עצי זית, שקד ומשמש אשר
  נטעו על ידי ערבים" (3,600 dunams were allotted, with olive, almond and apricot trees planted by Arabs). It does not
  name a village. For the owner's sensitivity review.
- **Renames.** None found. Named after the sickle on the Nahal badge (both Wikipedias); Gvati: "סמלי".
- **Coordinates.** CBS 32.38573, 35.03325 (code 375).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43773937 ;
  https://en.wikipedia.org/w/index.php?oldid=1374533578 . All checked 2026-10-23.

### 8. Kfar Tzvi Sitrin (כפר צבי סיטרין), religious vocational school

- **Which CBS row.** The CBS row is "בית צבי" (code 212, English "Bet Zevi"). Hebrew Wikipedia's article "כפר צבי
  סיטרין" carries the CBS bot template for code 0212 and opens "כפר צבי סיטרין (גם: בית צבי)"; English Wikipedia
  "Kfar Tzvi Sitrin" says "also known as Beit Tzvi". The Hebrew page "בית צבי" is a different place, an acting school in
  Ramat Gan founded in 1950, and is not this row.
- **Founded: [C].** CBS: 1953. English Wikipedia: "The village was established in 1953 as a professional religious
  school". Hebrew Wikipedia text: "כפר סיטרין נוסד בשנת 1952 כבית ספר מקצועי דתי" (founded 1952), citing Hareuveni,
  not read, and HaTzofe, 23 April 1952, not read; its category is "יישובים שהוקמו ב-1953". **No preference**; both
  years are in the window. Not in Gvati.
- **Type.** A religious vocational school and boarding school, later a technological college, today Chabad
  institutions (Hebrew Wikipedia). Not a residential settlement.
- **Founders. [C, minor]** Named after the son of a HaPoel HaMizrachi leader in the United States. Hebrew Wikipedia: "נוסד בשנת 1952 ונקרא
  על שם [הבן], בנו של [האב], ממנהיגי הפועל המזרחי בארצות הברית אשר יסד את הכפר לזכרו" (his father, a HaPoel
  HaMizrachi leader in the United States, founded the village in his son's memory). English Wikipedia instead says
  the namesake himself was "a leader of Hapoel HaMizrachi in the United States". No founding group of residents named.
- **Renames. [S, Wikipedia]** Also called "בית צבי" (the CBS name).
- **Coordinates.** CBS 32.71972, 34.96968 (code 212).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43957068 ;
  https://en.wikipedia.org/w/index.php?oldid=1374532861 ; the other "בית צבי",
  https://he.wikipedia.org/w/index.php?oldid=43901856 . All checked 2026-10-23.

### 9. Ein Hod (עין הוד), artists' village

- **Founded: [C].** CBS: 1954 (code 74). Hebrew Wikipedia: "ב-1953 התיישבו בבתים קבוצת אמנים, ובהם מרסל ינקו
  והקימו כפר אמנים" and "ב-5 באוגוסט 1953 נחגגה העלייה לקרקע של קבוצת האמנים הראשונה" (the first artists' group went
  onto the land on 5 August 1953); its article category is "יישובים שהוקמו ב-1954", while a subcategory "קטגוריה:עין
  הוד" sits in "יישובים שהוקמו ב-1953". English Wikipedia: "After a failed attempt to create a moshav on the site, Ein
  Hod became an artists' colony in 1953." **We prefer 5 August 1953** (the one dated statement, and the year in both
  article texts); CBS 1954 noted. Both years are in the window. Not in Gvati.
- **Earlier settlement. [S, Wikipedia]** English Wikipedia: "In July 1949 the Moshavim Movement settled immigrants from
  Tunisia and Algeria in the depopulated village, renaming the village Ein Hod." Hebrew Wikipedia: the government
  brought immigrants from Algeria and Tunisia, who later left and founded Tzrufa (paraphrase). That attempt is before
  the window.
- **Type.** An artists' village registered as a cooperative settlement society (Hebrew Wikipedia: "כפר אמנים הרשום
  כאגודה שיתופית להתיישבות"). A residential village, not an institution.
- **Founders. [S, Wikipedia]** A group of artists led by Marcel Janco (both Wikipedias); Itche Mambush the second
  resident (Hebrew Wikipedia).
- **Site note. [S, Wikipedia]** Both Wikipedias: the village stands on the site and in the houses of the Arab village of
  Ein Hawd (עין חוד), whose inhabitants left in the 1948 war (Hebrew Wikipedia: fled; English Wikipedia: most were
  expelled); some stayed nearby and founded the present village of the same name (paraphrase). That village is a CBS
  row today, "עין חוד" (code 1320, no founding year). Hebrew Wikipedia: "ממנו ברחו תושביו במהלך מלחמת העצמאות. לאחר
  המלחמה הופקעו האדמות והבתים"; "רוב תושבי הכפר הערבי שלא נמנו עם בני אותו השבט אכן עזבוהו, חלקם לאזורי אירביד
  שבירדן וג'נין שביהודה ושומרון". English Wikipedia: "Most of the 700 ... Arab villagers of Ein Hod from before the
  1948 ... War resettled in the West Bank, many in the refugee camp in Jenin"; "Attempts to dispossess them by legal
  means did not succeed." English Wikipedia cites Khalidi, *All That Remains*, not read. For the owner's sensitivity
  review.
- **Renames.** None in the window; English Wikipedia says the 1949 settlers renamed the village Ein Hod.
- **Coordinates.** CBS 32.69979, 34.98309 (code 74).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43960470 ;
  https://en.wikipedia.org/w/index.php?oldid=1375811042 . All checked 2026-10-23.

### 10. HaHotrim (החותרים), kibbutz

- **Founded: [C].** CBS: 1948 (code 434). Gvati: "הַחוֹתְרִים", region "חוף הכרמל", 7.6.48, affiliation HaKibbutz
  HaMeuhad. Hebrew Wikipedia: "בשנת 1948 התיישבו חברי הקבוצה בבתי המושבה הגרמנית הנטושה נויהרדטהוף, כ-5 ק"מ צפונית
  ליישוב הקבע, אליו עברו בשנת 1952." (in 1948 the group settled in the houses of the abandoned German colony
  Neuhardthof, about 5 km north, and moved to the permanent site in 1952); its category is "יישובים שהוקמו ב-1952".
  English Wikipedia: "The kibbutz was established in 1952", infobox "founded = 1941/1952", category "Populated places
  in Israel established in 1952". **We prefer 1948 for the founding** (CBS and Gvati, the two non-wiki sources), which
  puts it outside the window; **the move to today's site in 1952 is inside it**. Kept so the owner can see the
  disagreement.
- **Founders. [S, Wikipedia]** A work company formed in 1941-42 near Kiryat Haim (both Wikipedias); English Wikipedia:
  Jewish refugees from Czechoslovakia and Germany.
- **Site note. [S, Wikipedia]** English Wikipedia: the 1952 site is on land near the depopulated village of al-Tira
  (paraphrase, citing Khalidi, p. 198, and Morris, not read). Hebrew Wikipedia does not mention it. For the owner's
  sensitivity review.
- **Renames.** None found. Name origin, Gvati: "סמלי"; both Wikipedias: the founders' wish to fish.
- **Coordinates.** CBS 32.75244, 34.95679 (code 434), today's site. The 1948 site at Neuhardthof is **[GAP]** for
  coordinates.
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43625867 ;
  https://en.wikipedia.org/w/index.php?oldid=1374531105 . All checked 2026-10-23.

### 11. Yemin Orde (ימין אורד), youth village

- **Founded 1953. [S, Wikipedia]** Hebrew Wikipedia: "המקום נוסד ב-1953 ונקרא תחילה "כפר וינגייט"." English
  Wikipedia: "Yemin Orde Youth Village was established in 1953 by the British Friends of Youth Aliyah." The two may
  copy one another; no non-wiki source was found. The day is **[GAP]**. Not in Gvati.
- **No CBS row.** No row of the CBS file matches the name; the place may be counted inside another locality (our reading),
  but the file does not say. **[GAP]**
- **Type.** A boarding youth village for immigrant youth, owned by the religious Youth Aliyah committee (Hebrew
  Wikipedia); English Wikipedia: for at-risk youth. Not a residential settlement.
- **Founders. [S, Wikipedia]** The British Friends of Youth Aliyah (English Wikipedia, quote above).
- **Renames. [S, Wikipedia]** First "כפר וינגייט" (quote above); named after Orde Wingate (both Wikipedias).
- **Coordinates. [S]** Wikidata Q6589839 holds two P625 values, both of normal rank: 32.70239, 34.98762 and 33.1,
  35.55. We take the first because it fits Hebrew Wikipedia's "סמוך ליישובים ניר עציון, עין הוד ועין חוד" (our
  reading); the second matches the English Wikipedia value below. English Wikipedia's coordinate template gives 33°06'N 35°33'E (source "GNS"),
  which is in the Upper Galilee; we read it as an error on that page (our reading).
- **Sources.** https://he.wikipedia.org/w/index.php?oldid=43606368 ;
  https://en.wikipedia.org/w/index.php?oldid=1324160284 ; https://www.wikidata.org/wiki/Q6589839 . All checked
  2026-10-23.

## Gaps and notes for the owner

- **Tel Aviv District: no rows** by any of the three lists. The district was already built up; its last new locality
  in the CBS file is Or Yehuda (1950).
- **Haifa District: 11 rows**, the 10 with a CBS row all in the Hadera subdistrict (32); the Haifa subdistrict (31)
  has none in the window (its nearest are Oranim 1951 and Rekhasim 1957).
- **Institutions.** Three rows are youth villages or schools (Kfar Galim, Kfar Tzvi Sitrin, Yemin Orde). Two of them
  are CBS localities, Yemin Orde is not; whether the map wants them is the owner's call.
- **Out by the preferred date but kept:** HaHotrim (founded 1948, moved 1952). Sde Yitzhak is in by our preference
  but out by Gvati.
- **Defunct localities** founded 1952-56 in either district were not searched for beyond the Wikipedia categories.
- **No newspaper of the time was read**; every newspaper date above is as Wikipedia cites it.
- **Days** confirmed by two sources for Mezer and Maggal (Gvati and Hebrew Wikipedia, not checked for independence;
  English Wikipedia gives Mezer's day too); Talmei Elazar, Ma'or and Givat Nili have Gvati's day only; Ein Hod has
  Hebrew Wikipedia's day only; the rest have a year.
- **Site notes** are recorded for six rows: four on, near or next to the lands of a village depopulated in 1948 (Ein
  Hod from both Wikipedias, including the postwar expropriation and where residents went; Givat Nili from both; Talmei
  Elazar and HaHotrim from English Wikipedia citing Khalidi, not read), and two on land tied to Arab neighbours
  (Maggal, Mezer, Hebrew Wikipedia). Hexes and armistice-line flags were not computed; they belong to the paused
  data-file step.
