# Settlements founded in 1952-1956: the Ashkelon subdistrict and the Lachish area (research note, 2026-10-22)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file and the
tool change (#61) stay paused until after the MVP. Refs #27, its 1952-56 rows, one region. It follows the format of
[`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) (#485), which covers 1949-1951; those rows are
not repeated here. It is the sibling of the Negev note (`settlements-1952-56-negev.md`, PR #1366) and uses the same
method and sources.

The question: which Jewish localities were founded in the Ashkelon subdistrict between 1 January 1952 and 31 December
1956, and for each: founding date, coordinates, type, founding group, renames and sources.

## Which region

**Boundary used: the Ashkelon subdistrict (נפת אשקלון, code 61) of the Central Bureau of Statistics**, as given in the
CBS localities file 2024 (columns "סמל נפה" and "שם נפה"; source below). It is the line the Negev note left out: that
note takes subdistrict 62 and names the Lachish area as outside it. Subdistrict 61 holds the Lachish area moshavim,
Kiryat Gat, Ashdod and Sde Yoav, and also the strip along the Gaza line further south (Nahal Oz and Kefar Azza are in
it; both have year 1951 in the CBS file, and Nahal Oz is in the 1949-51 note). As in the Negev note, this line was chosen because the official source
applies it to every row. **Other regions of #27 (the north, the centre, Jerusalem) are open to other claimants, and the
owner can draw this line differently.**

## How it was done

- Every page was fetched on 22 October 2026 with `curl` (User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`) or read through the MediaWiki
  API. No request carried a personal identifier. No web search result was used as a source.
- **The candidate pool** is the union of these lists:
  1. every row of the CBS localities file 2024 in subdistrict 61 whose "שנת ייסוד" (year of founding) is 1952 to 1956:
     **29 rows**;
  2. every row of Haim Gvati's list of rural settlements whose "תאריך הקמה" (date of founding) falls in 1952-1956 and
     whose name matches a subdistrict-61 row of the CBS file: **25 rows** (six of them matched by hand, where the two
     spell the name differently: Zohar, Noga, No'am, Otzem, Revaha, Shalva). 24 are already in list 1. The one new
     row is Hatzor-Ashdod (Gvati "1953", CBS 1937), kept as row 10 with **[C]**;
  3. the Hebrew Wikipedia categories "יישובים שהוקמו ב-1952" to "יישובים שהוקמו ב-1956", intersected with every
     subdistrict-61 name in the CBS file: 27 matches, all already in list 1 (three of them under another year than
     the CBS one: Nehora, Ashdod, Nir Hen). The two CBS rows of list 1 not in these categories are Timorim (category
     1948) and Beit Nir (category 1957);
  4. a manual look at the two subdistrict-61 rows with CBS year 1957: Kfar Silver's Hebrew Wikipedia dates its opening
     to 1956, so it is kept as row 27 with **[C]**. Or HaNer (CBS 1957, Gvati 1.10.57) agrees with itself and was not
     kept; its Wikipedia pages were not read.

  That gives **31 rows**.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024 ("קובץ יישובים
  2024"), https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22), columns
  "שנת ייסוד" and "קואורדינטות". As in `settlements-1949-51.md`, I read the twelve-digit coordinate field as an Israel
  Transverse Mercator easting and northing (six digits each) and converted it with `pyproj` (EPSG:2039 to EPSG:4326).
  That reading is mine, not stated in the file; the 1949-51 note checked it against Wikipedia for 29 rows (median
  about 40 m). Coordinates below are the CBS values, rounded to five decimals. They are where the locality is
  **today**; where a source says the first camp stood elsewhere, the row says so.
- **Founding dates (day).** Gvati, *100 Years of Settlement*, vol. 2, appendix "List of all Jewish rural settlements
  in Israel", Ben-Yehuda Project edition, https://benyehuda.org/read/14684 (checked 2026-10-22). This is a web edition
  with no page numbers. Its sections ("קיבוצים", "מושבים", "מושבים שיתופיים") give the type used below for rural rows;
  its columns are name, region ("אזור גיאוגרפי"), date of founding, affiliation ("השתייכות ארגונית") and origin of the
  name. The column does not say whether the date is the day on the land or a later recognition. It is one secondary
  compilation. Its section of urban settlements ("ישובים עירוניים שנוסדו כמושבות") lists no development towns, and it
  has no row for Kannot, Kfar Silver, Nehora or Even Shmuel.
- **Wikipedia** (Hebrew and English) was read as wikitext of the current revision, cited by `oldid`. It is a pointer
  and a cross-check: a fact resting on Wikipedia alone is tagged **[S, Wikipedia]**, and where it cites a newspaper or
  book I give that citation as Wikipedia gives it and do not count it as read. Quotes from Wikipedia are from the
  wikitext with link markup, bold marks and footnotes removed (a piped link shows its display text); nothing else is changed. The
  Hebrew infoboxes of these pages are filled by a bot template ("בוט יישובים") and carry no founding year, so the Hebrew
  year used below is the one in the article text.
- **Founding group** in Gvati is the affiliation (the settlement movement), not the people. The people (country of
  origin, garin) come from Wikipedia and are tagged as such.
- **Site notes.** Where a Wikipedia article says the locality was built on or near the land of a village depopulated
  in 1948, the row gives that in one paraphrased line, for the owner's sensitivity review, as the Negev note did for
  Nir Akiva. Nothing is added beyond what the article says.
- Hebrew dates were converted with the `convertdate` library.

Tags, one per fact, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. Both values and the sources on each side are given, and which I prefer.
- **[GAP]**: not found in any source read.

CBS year plus Gvati day agreeing on the year counts as [V] for the year; the day stays [S] unless a second source
gives the same day.

### Sources not reached

- **Regional council and municipal sites.** Yoav regional council settlement pages cited by Hebrew Wikipedia
  (`https://www.yoav.org.il/objDoc.asp?PID=358245&OID=475532&DivID=1&docMode=settMap` for Sde Yoav and `...OID=475654...`
  for Kfar HaRif): HTTP 403. `https://www.shafir.org.il/`, `https://www.kiryat-gat.muni.il/` and
  `https://www.ashdod.muni.il/`: no response within 25 seconds. The Lachish regional council's settlements page
  (`https://lachish.org.il/ישובי-המועצה/`) was reached; it gives the council's own founding ("הוכרזה רשמית ב 1956")
  but no founding date for any locality.
- **Not tried, known blocked** (`settlements-1949-51.md`, `settlements-1952-56-negev.md`): the NLI historical press
  (JPress) and the Israel State Archives. So **no contemporaneous newspaper was read for any row**; Hebrew Wikipedia
  cites many (Davar, Al HaMishmar, HaTzofe and others), listed in the rows as not read.
- **Localities founded 1952-56 that no longer exist** are not in the CBS file and not in Gvati. They were not searched
  for. One turned up by chance: Hebrew Wikipedia's "מעברת חרובית" (Harubit transit camp, near Kfar Menahem, 1955 to
  1959, `oldid=44051899`), with a second camp, Masua. Not given rows. **[GAP]**

## Summary table

Coordinates: CBS 2024, lat N, lon E, today's site. "Founded" is the best-supported date; see the row for the sources.

| # | Name | Type | Founded | Lat, lon (CBS) | Renames |
|---|---|---|---|---|---|
| 1 | Yinon / ינון | Moshav | 4 Sep 1952 **[S]**; 1952 **[V]** | 31.74257, 34.78149 | None found |
| 2 | Orot / אורות | Moshav | 2 Oct 1952 (Gvati) or Sep 1952 (he Wikipedia) **[C]**; 1952 **[V]** | 31.74219, 34.73451 | Site first known as "Be'er Tuvia B" |
| 3 | Kannot / כנות | Agricultural school (youth village) | Oct 1952 **[S, Wikipedia]**; 1952 **[V]** | 31.80196, 34.75213 | None found |
| 4 | Shalva / שלווה | Moshav | 25 Dec 1952 **[S]**; 1952 **[V]** | 31.56371, 34.76713 | None found |
| 5 | Menuha / מנוחה | Moshav | 21 Jan 1953 **[S]**; Jan 1953 **[V]** | 31.65772, 34.77647 | None found |
| 6 | Sgula / סגולה | Moshav | 18 May 1953 **[S]**; May 1953 **[V]** | 31.66969, 34.78037 | None found |
| 7 | Nahala / נחלה | Moshav | 25 May 1953 (Gvati) or 26 Jun 1953 (he Wikipedia) **[C]**; 1953 **[V]** | 31.6582, 34.79463 | None found |
| 8 | Revaha / רווחה | Moshav | 9 Jun 1953 **[S]**; 1953 **[V]** | 31.64939, 34.73339 | First "Zavdiel B" |
| 9 | Yad Natan / יד נתן | Moshav | 29 Jul 1953 **[S]**; 1953 **[V]** | 31.65238, 34.70557 | First "Zikhron Natan" |
| 10 | Hatzor-Ashdod / חצור-אשדוד | Kibbutz | 1937 (CBS), 1946 (en Wikipedia) or 1953 (Gvati) **[C]** | 31.77259, 34.72033 | None found |
| 11 | Nir Banim / ניר בנים | Moshav | Oct 1953, 1954 or 1955 **[C]** | 31.6718, 34.75489 | First "Gat 2" |
| 12 | Timorim / תימורים | Moshav shitufi | Moved here 1954 **[V]**; founded 1948 elsewhere **[S, Wikipedia]** | 31.71549, 34.75861 | None found |
| 13 | Kiryat Gat / קריית גת | Transit camp (ma'abara), then town | 1954 **[V]** | 31.61118, 34.76615 | None found |
| 14 | Amatzia / אמציה | Nahal outpost, then moshav | 8 Mar 1955 (Gvati) or Jun 1955 (he Wikipedia) **[C]**; 1955 **[V]** | 31.53242, 34.91373 | None found |
| 15 | Otzem / עוצם | Moshav | 22 May (he Wikipedia) or 24 May 1955 (Gvati) **[C]**; 1955 **[V]** | 31.63682, 34.7038 | First "Lachish 1" |
| 16 | Shahar / שחר | Moshav | 26 May (he Wikipedia) or 26 Jun 1955 (Gvati) **[C]**; 1955 **[V]** | 31.61972, 34.72551 | First "Lachish 2" |
| 17 | Sde David / שדה דוד | Moshav | 1 Jun 1955 **[S]**; 1955 **[V]** | 31.57619, 34.68403 | None found |
| 18 | Lachish / לכיש | Nahal outpost, then moshav | 8 Jun 1955 **[S]**; 1955 **[V]** | 31.56122, 34.8421 | None found |
| 19 | Eitan / איתן | Moshav | 18 Jun 1955 **[S]**; 1955 **[V]** | 31.57232, 34.7482 | First "Uza G" and "Lachish 9" |
| 20 | Noga / נוגה | Moshav | 10 Jul 1955 **[S]**; 1955 **[V]** | 31.62442, 34.69475 | First "Lachish 3" |
| 21 | Beit Nir / בית ניר | Moshav shitufi, then kibbutz | 20 Jul 1955 **[S]**; Jul 1955 **[V]**; kibbutz 1957 **[C]** | 31.64845, 34.87278 | None found |
| 22 | No'am / נועם | Moshav | 7 Aug 1955 **[S]**; 1955 **[V]** | 31.56802, 34.78887 | None found |
| 23 | Nir Hen / ניר ח"ן | Moshav shitufi | 21 Sep 1955 **[V]** (cornerstone); year 1955 or 1956 **[C]** | 31.60875, 34.71465 | Planned as "Hila" |
| 24 | Nehora / נהורה | Rural centre | 1955 (both Wikipedias) or 1956 (CBS) **[C]** | 31.62298, 34.70558 | None found |
| 25 | Ashdod / אשדוד | Transit camp, then town | 1955 (CBS) or 1956 (both Wikipedias) **[C]**; first settlers 25 Nov 1956 **[S, Wikipedia]** | 31.79538, 34.64939 | First "Ashdod Yam" |
| 26 | Zohar / זוהר | Moshav | 20 Feb 1956 **[S]**; 1956 **[V]** | 31.5954, 34.69262 | None found |
| 27 | Kfar Silver / כפר סילבר | Youth village | Dedicated 29 Mar 1956 (he Wikipedia) or 1957 (CBS, en Wikipedia) **[C]** | 31.67315, 34.61176 | First "Abba Hillel" |
| 28 | Sde Yoav / שדה יואב | Kibbutz | 10 May 1956 (Gvati) or 2 Jun 1966 (he Wikipedia) **[C]** | 31.64513, 34.67719 | None found |
| 29 | Sde Moshe / שדה משה | Moshav | 17 Jun 1956 **[S]**; 1956 **[V]** | 31.61133, 34.80146 | "Sde Yeshayahu" or "Sde Eliyahu" **[C]** |
| 30 | Even Shmuel / אבן שמואל | Rural centre | 1956 **[V]** | 31.5749, 34.76544 | First "Oman" |
| 31 | Kfar HaRif / כפר הרי"ף | Moshav | Nov 1956 **[S]**; 1956 **[V]** | 31.74546, 34.79289 | None found |

Rows: 31. By type (first form): 22 moshavim or moshavim shitufim (Amatzia and Lachish began as Nahal outposts), 2
kibbutzim (Hatzor-Ashdod, Sde Yoav), 2 youth villages or schools (Kannot, Kfar Silver), 2 rural centres (Nehora, Even
Shmuel), 2 towns (Kiryat Gat, Ashdod), and Timorim, which moved here. Rows 10, 25, 27 and 28 may fall outside
the window; they are kept so the owner can see the disagreement.

## Rows

Common sources, cited by short name in every row:
- **CBS**: https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22).
- **Gvati**: https://benyehuda.org/read/14684 , appendix table (checked 2026-10-22).

### 1. Yinon (ינון), moshav

- **Founded 4 September 1952. [S]** Gvati: 4.9.52, region "שפלת יהודה", affiliation Tnuat HaMoshavim, section
  "מושבים". **Year [V]**: CBS 1952; Hebrew Wikipedia "המושב הוקם בשנת 1952"; English Wikipedia "founded in 1952".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים מתימן" (immigrants from Yemen); English Wikipedia the
  same.
- **Renames.** None found. Name from Psalm 72:17 (Gvati).
- **Site note. [S, Wikipedia]** Paraphrased: Hebrew Wikipedia places it on the land of al-Masmiyya al-Kabira, English
  Wikipedia south of it.
- **Coordinates.** CBS 31.74257, 34.78149 (CBS code 2011).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43801269 ;
  https://en.wikipedia.org/w/index.php?oldid=1375047214 . All checked 2026-10-22.

### 2. Orot (אורות), moshav

- **Founded: [C], day.** Gvati: 2.10.52, region "דרום", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "ארבעת
  המתיישבים הראשונים עלו למושב בספטמבר 1952" (the first four settlers came in September 1952), citing Davar, 29 Sep
  1953 and 11 Jan 1955, not read. **Year 1952 [V]**: CBS 1952; English Wikipedia "founded in 1952". I prefer neither
  day; our reading: the two may date different steps (the first four settlers in September, Gvati's 2 October).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: a moshav for immigrants from the United States, set up on the
  initiative of the "האיכר העובד" movement; later families mainly from New Jersey. English Wikipedia: immigrants from
  the United States, Canada and South Africa.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: huts were put up on the site from 1951, "שנודע אז כבאר טוביה ב'" (then
  known as Be'er Tuvia B).
- **Coordinates.** CBS 31.74219, 34.73451 (code 2012).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43119652 ;
  https://en.wikipedia.org/w/index.php?oldid=1375043825 . All checked 2026-10-22.

### 3. Kannot (כנות), agricultural school

- **Founded October 1952. [S, Wikipedia]** Hebrew Wikipedia: "המוסד נפתח באוקטובר 1952 עם כ-100 תלמידים" (opened in
  October 1952 with about 100 pupils), official opening ceremony on 3 December 1952; foundation stone August 1951
  (citing Al HaMishmar and Davar, not read). **Year [V]**: CBS 1952; English Wikipedia "founded in 1952". Not in
  Gvati.
- **Type and founders. [S, Wikipedia]** Hebrew Wikipedia: opened by the Histadrut's Working Women's Council
  ("מועצת הפועלות") with Youth Aliyah classes. English Wikipedia: "founded in 1952 by the Workers' Council of the
  Histadrut".
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it on the land of Bashshit, depopulated in 1948.
- **Coordinates.** CBS 31.80196, 34.75213 (code 2006).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43797438 ;
  https://en.wikipedia.org/w/index.php?oldid=1375176901 . All checked 2026-10-22.

### 4. Shalva (שלווה), moshav

- **Founded 25 December 1952. [S]** Gvati: 25.12.52, region "לכיש", affiliation HaPoel HaMizrachi. **Year [V]**: CBS
  1952; English Wikipedia "founded in 1952". The Hebrew Wikipedia article gives no year in its text (only its
  category, 1952).
- **Founders. [S, Wikipedia]** English Wikipedia: "Jewish immigrants and refugees mostly from Libya", with some from
  Tunisia. Hebrew Wikipedia: most residents came from Libya, a few from Tunisia (this describes residents, not only
  founders).
- **Renames.** None found.
- **Coordinates.** CBS 31.56371, 34.76713 (code 873).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43801229 ;
  https://en.wikipedia.org/w/index.php?oldid=1375238995 . All checked 2026-10-22.

### 5. Menuha (מנוחה), moshav

- **Founded 21 January 1953. [S]**; **January 1953 [V].** Gvati: 21.1.53, region "לכיש", affiliation Tnuat
  HaMoshavim. Hebrew Wikipedia: "המושב הוקם בינואר 1953" (citing Davar, 22 Jan 1953, not read). CBS: 1953. English
  Wikipedia: 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים מכורדיסטן ממעברת קסטינה, שהיוו את הרוב, ומעולים מפרס"
  (immigrants from Kurdistan from the Kastina transit camp, the majority, and from Persia). English Wikipedia: refugees
  "from Kurdistan region of Iraq".
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it next to the ruins of Jusayr, depopulated in
  1948.
- **Coordinates.** CBS 31.65772, 34.77647 (code 2030).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43801197 ;
  https://en.wikipedia.org/w/index.php?oldid=1375205534 . All checked 2026-10-22.

### 6. Sgula (סגולה), moshav

- **Founded 18 May 1953. [S]**; **May 1953 [V].** Gvati: 18.5.53, region "לכיש", affiliation Tnuat HaMoshavim. Hebrew
  Wikipedia: "המושב הוקם במאי 1953". CBS: 1953. English Wikipedia: 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי ילידי הארץ ועולים חדשים, במסגרת התנועה "מן העיר אל הכפר""
  (Israeli-born and new immigrants, in the "from the city to the village" movement). English Wikipedia: "by native
  Israelis".
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Paraphrased: both Wikipedias place it on the land of Summeil (Hebrew: סומייל
  אל-ח'ליל), depopulated in 1948.
- **Coordinates.** CBS 31.66969, 34.78037 (code 2046).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42995602 ;
  https://en.wikipedia.org/w/index.php?oldid=1375251612 . All checked 2026-10-22.

### 7. Nahala (נחלה), moshav

- **Founded: [C], day.** Gvati: 25.5.53, region "לכיש", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "היישוב הוקם
  בי"ג בתמוז ה'תשי"ג (26 ביוני 1953)"; 13 Tammuz 5713 is 26 June 1953 (`convertdate`). **Year 1953 [V]**: CBS 1953;
  English Wikipedia 1953. I prefer neither day: Gvati is the one source read directly; Hebrew Wikipedia gives no
  footnote for its date.
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Yemen. Hebrew Wikipedia: in 1950 they had been brought
  from the Ein Shemer transit camp to found moshav Agur near Beit Guvrin; English Wikipedia: "The founders had
  originally established moshav Agur in 1950."
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Paraphrased: both Wikipedias place it on or near the land of Summeil.
- **Coordinates.** CBS 31.6582, 34.79463 (code 2045).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42995601 ;
  https://en.wikipedia.org/w/index.php?oldid=1375251941 . All checked 2026-10-22.

### 8. Revaha (רווחה), moshav

- **Founded 9 June 1953. [S]** Gvati: 9.6.53, region "לכיש", affiliation HaPoel HaMizrachi. Hebrew Wikipedia: "היישוב
  הוקם באמצע 1953" (mid-1953, citing Davar, 5 Jul 1953, not read). **Year [V]**: CBS 1953; English Wikipedia 1953.
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Kurdistan.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "קודם לכן נקרא היישוב זבדיאל ב', בשל קרבתו לזבדיאל" (first called
  Zavdiel B, being near Zavdiel), citing Al HaMishmar, 21 Oct 1953, not read.
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it on the land of Karatiyya and Hatta,
  depopulated in 1948.
- **Coordinates.** CBS 31.64939, 34.73339 (code 2051).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42996545 ;
  https://en.wikipedia.org/w/index.php?oldid=1375252150 . All checked 2026-10-22.

### 9. Yad Natan (יד נתן), moshav

- **Founded 29 July 1953. [S]** Gvati: 29.7.53, region "שפלת יהודה", affiliation HaOved HaTzioni. **Year [V]**: CBS
  1953; both Wikipedias 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים מהונגריה"; English Wikipedia: immigrants from the
  Hungarian youth movement HaNoar HaTzioni "who survived the Holocaust".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "ונקרא בשנותיו הראשונות "זיכרון נתן"" (called Zikhron Natan in its
  first years). Named after Ottó Komoly (Natan) (Gvati and both Wikipedias).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "היישוב הוקם במקום בו שכן הכפר הערבי בית עפה"; "תושביו, שנמלטו
  במהלך הקרבות, לא הורשו לחזור אליו לאחריהם, ורכושם הוכרז נכסי נפקדים". English Wikipedia
  (paraphrased): built 800 m south of Bayt 'Affa and 1 km north-east of Iraq Suwaydan, both depopulated in 1948.
- **Coordinates.** CBS 31.65238, 34.70557 (code 775).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43771659 ;
  https://en.wikipedia.org/w/index.php?oldid=1375075621 . All checked 2026-10-22.

### 10. Hatzor-Ashdod (חצור-אשדוד), kibbutz

- **Founded: [C].** Gvati: "1953" (year only), region "דרום", affiliation HaKibbutz HaArtzi, section "קיבוצים". CBS:
  1937. English Wikipedia: infobox "founded = 17 June 1946"; "On 17 June 1946, some of the members arrived at the
  point and established the kibbutz". Hebrew Wikipedia: the founding garin formed in 1936 (category 1936). **I prefer
  1946 for the settlement on this site** (English Wikipedia's dated account), with 1936 or 1937 for the group; no
  source read explains Gvati's 1953. Kept only because Gvati's year falls in the window; it is most likely not a
  1952-56 founding (my reading).
- **Founders. [S, Wikipedia]** Both Wikipedias: a garin of HaShomer HaTzair graduates.
- **Renames.** None found for the locality. English Wikipedia: the group was first "Kibbutz Eretz Israeli Gimel".
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "במבצע ברק נכבשו הכפרים הערביים הסמוכים, ותושביהם הפכו לפליטים
  ברצועת עזה. חלק מאדמותיהם, שהוכרזו כאדמות מדינה לפי חוק נכסי נפקדים, הוחכרו על ידי מינהל מקרקעי ישראל לקיבוץ";
  English Wikipedia: "their residents became refugees in the Gaza Strip. Part of the land in the area was leased to
  the kibbutz". Given as the sources word it, for the owner's sensitivity review.
- **Coordinates.** CBS 31.77259, 34.72033 (code 406).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43555841 ;
  https://en.wikipedia.org/w/index.php?oldid=1375038981 . All checked 2026-10-22.

### 11. Nir Banim (ניר בנים), moshav

- **Founded: [C], year.** Gvati: "10.53" (October 1953), region "לכיש", affiliation Tnuat HaMoshavim. CBS: 1954.
  English Wikipedia: infobox "founded = 1953", text "founded in 1954". Hebrew Wikipedia: "היישוב הוקם בשנת 1955"
  (category 1954), and the founding garin left Tzur Ma'on "בשלהי 1954" (at the end of 1954) for the permanent site "גת
  2". **I prefer 1954** (CBS, English Wikipedia's text, and the garin's move at the end of 1954); 1953 and 1955 stay
  as given.
- **Founders. [V]** Sons and daughters of older moshavim (Gvati's name origin "הוקם על-ידי בני המושבים"; Hebrew
  Wikipedia lists Be'er Tuvia, Kfar Vitkin, Nahalal, Kfar Yehezkel, Herut; English Wikipedia the same five).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: the permanent site "גת 2", "שהפך לניר בנים".
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it on the land of al-Sawafir al-Sharqiyya,
  depopulated in 1948.
- **Coordinates.** CBS 31.6718, 34.75489 (code 553).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42987817 ;
  https://en.wikipedia.org/w/index.php?oldid=1379234186 . All checked 2026-10-22.

### 12. Timorim (תימורים), moshav shitufi

- **Founded here 1954. [V]** CBS: 1954. Hebrew Wikipedia: "בשנת 1953 הוא הפך למושב שיתופי ועבר ב-1954, עקב מחסור
  בקרקעות, למיקומו הנוכחי" (became a moshav shitufi in 1953 and moved to its present site in 1954 for lack of land).
  English Wikipedia: "In 1954 the settlement moved to its current location". Gvati: "1954", region "דרום", affiliation
  HaOved HaTzioni, section "מושבים שיתופיים".
- **Earlier founding. [S, Wikipedia]** Hebrew Wikipedia: first founded on 21 June 1948 as a kibbutz near Tel Shimron in
  the Lower Galilee, where Timrat is today (citing HaMashkif, 1 Aug 1948, not read). English Wikipedia: established in
  1948 "as a kibbutz on Shimron Hill in the Lower Galilee". For #27 the 1954 arrival is the event inside the window.
- **Founders. [S, Wikipedia]** English Wikipedia: a garin of youth from South Africa, Romania and Egypt, of HaNoar
  HaTzioni.
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places the present site near Tall al-Turmus,
  depopulated in 1948.
- **Coordinates.** CBS 31.71549, 34.75861 (code 163). The 1948 site in the Galilee is not in this region.
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43704902 ;
  https://en.wikipedia.org/w/index.php?oldid=1375045979 . All checked 2026-10-22.

### 13. Kiryat Gat (קריית גת), transit camp, then town

- **Founded 1954. [V]** CBS: 1954. Hebrew Wikipedia: "נוסדה בשנת 1954 כעיירת עולים" and "בשנת 1954 הוקם בשטח העיר
  מעברה זמנית לקליטת עולים". English Wikipedia: "Kiryat Gat was founded in 1954, initially as a ma'abara." The day is
  **[GAP]**. Not in Gvati.
- **Later steps. [S, Wikipedia]** English Wikipedia: "The following year it was established as a development town by
  18 families from Morocco." Hebrew Wikipedia: foundation stone of the town centre in 1956, local council in 1958, city
  in 1972.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: the camp was the first home of immigrants from North Africa, mainly
  Morocco.
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "העיר שוכנת על אדמות הכפרים הערביים אל-פאלוג'ה ועיראק אל-מנשייה,
  שתושביהם נטשו לאחר הקמת מדינת ישראל". The same section: "בנספח של חילופי מכתבים שצורף להסכם, היא התחייבה להתיר
  לאזרחים להישאר בכפרים אל-פלוג'ה ועיראק אל-מנשייה"; "מספר ימים לאחר חתימת הסכם שביתת הנשק, נסו כ-2,000 עד 3,000
  תושבים משני הכפרים".
- **Coordinates.** CBS 31.61118, 34.76615 (code 2630).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44075661 ;
  https://en.wikipedia.org/w/index.php?oldid=1381329989 . All checked 2026-10-22.

### 14. Amatzia (אמציה), Nahal outpost, then moshav

- **Founded: [C], day.** Gvati: 8.3.55, region "לכיש", affiliation the Herut moshavim ("מושבי “חירות”"), section
  "מושבים שיתופיים". Hebrew Wikipedia: "היישוב הוקם כהיאחזות נח"ל ביוני 1955" (a Nahal outpost in June 1955), citing
  HaTzofe, 9 Jun 1955, p. 4, not read; set up together with Lachish (row 18) and Nehusha near the armistice line with
  Jordan. **Year 1955 [V]**: CBS 1955; English Wikipedia 1955. I prefer neither date: Gvati is read directly, the June
  date rests on a newspaper I could not read, and (our reading) they may date different steps.
- **Renames.** None found. Named for King Amaziah of Judah (Gvati and English Wikipedia).
- **Site note. [S, Wikipedia]** Paraphrased: both Wikipedias place it on the land of al-Dawayima.
- **Coordinates.** CBS 31.53242, 34.91373 (code 23).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43651220 ;
  https://en.wikipedia.org/w/index.php?oldid=1375067458 . All checked 2026-10-22.

### 15. Otzem (עוצם), moshav

- **Founded: [C], day.** Gvati: 24.5.55, region "לכיש", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "היישוב הוקם
  ב-22 במאי 1955". **Year 1955 [V]**: CBS 1955; English Wikipedia 1955. I prefer neither day; they are two days apart.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים מבוגמז שבהרי האטלס במרוקו" (immigrants from Bougmez in
  the Atlas mountains, Morocco); English Wikipedia: immigrants from Morocco. Hebrew Wikipedia's article on the Harubit
  transit camp (`oldid=44051899`) says the founders came from there in May 1955.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: the first moshav of the Lachish plan, "ולכן שמו הזמני היה "לכיש 1""
  (so its temporary name was Lachish 1).
- **Coordinates.** CBS 31.63682, 34.7038 (code 32).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43685476 ;
  https://en.wikipedia.org/w/index.php?oldid=1375072442 . All checked 2026-10-22.

### 16. Shahar (שחר), moshav

- **Founded: [C], day.** Gvati: 26.6.55, region "לכיש", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "המושב הוקם
  ב-26 במאי 1955". The same day number in different months; one of them may be a slip, but which is not shown by any
  source read (my reading). **Year 1955 [V]**: CBS 1955; English Wikipedia 1955.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from North Africa, joined by families from Cochin and later
  from Iraq; English Wikipedia: immigrants from North Africa and India.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "שמו הזמני היה לכיש 2".
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it near al-Faluja.
- **Coordinates.** CBS 31.61972, 34.72551 (code 7).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44095593 ;
  https://en.wikipedia.org/w/index.php?oldid=1375073672 . All checked 2026-10-22.

### 17. Sde David (שדה דוד), moshav

- **Founded 1 June 1955. [S]** Gvati: 1.6.55, region "לכיש", affiliation HaOved HaTzioni. Hebrew Wikipedia cites three
  newspapers of 2 June 1955 (Lamerhav, Davar, Zmanim; not read), two of whose titles, as Wikipedia gives them, report
  its settlement celebration. **Year [V]**:
  CBS 1955; both Wikipedias 1955.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים ממרוקו"; English Wikipedia: immigrants "from the
  Moroccan youth movement HaNoar HaTzioni".
- **Renames.** None found. Named after Zalman David Levontin (Gvati and Hebrew Wikipedia).
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it near Burayr, depopulated in 1948.
- **Coordinates.** CBS 31.57619, 34.68403 (code 36).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43685481 ;
  https://en.wikipedia.org/w/index.php?oldid=1375073018 . All checked 2026-10-22.

### 18. Lachish (לכיש), Nahal outpost, then moshav

- **Founded 8 June 1955. [S]** Gvati: 8.6.55, region "לכיש", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "המושב
  הוקם תחילה כהיאחזות נח"ל בשנת 1955" (first a Nahal outpost in 1955), later a moshav; the Amatzia article (row 14)
  dates both outposts to June 1955. **Year [V]**: CBS 1955; English Wikipedia "founded as a Nahal settlement in 1955".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: a garin of sons of moshavim and kibbutzim.
- **Renames.** None found. Named after the ancient city (Gvati, English Wikipedia).
- **Site note. [S, Wikipedia]** Paraphrased: both Wikipedias place it on or near the land of al-Qubayba.
- **Coordinates.** CBS 31.56122, 34.8421 (code 24).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43219056 ;
  https://en.wikipedia.org/w/index.php?oldid=1375069283 . All checked 2026-10-22.

### 19. Eitan (איתן), moshav

- **Founded 18 June 1955. [S]** Gvati: 18.6.55, region "לכיש", affiliation HaPoel HaMizrachi. Hebrew Wikipedia: "היישוב
  הוקם בשנת 1955", citing Davar, 1 Jul 1955, not read. **Year [V]**: CBS 1955; English Wikipedia 1955.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים מתוניסיה ומהאי ג'רבה" (immigrants from Tunisia and
  Djerba); English Wikipedia: immigrants from Tunisia.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia, verbatim: "קודם לשם איתן, נקרא היישובבמות הזמניים עוזה ג' ולכיש 9"
  (the run-together "היישובבמות" is in the source; it reads as "by the temporary names" Uza G and Lachish 9, my
  reading).
- **Coordinates.** CBS 31.57232, 34.7482 (code 37).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44032044 ;
  https://en.wikipedia.org/w/index.php?oldid=1375230679 . All checked 2026-10-22.

### 20. Noga (נוגה), moshav

- **Founded 10 July 1955. [S]** Gvati: 10.7.55, region "לכיש", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "היישוב
  הוקם בשנת 1955", citing Davar, 11 Jul 1955, not read. **Year [V]**: CBS 1955; English Wikipedia 1955.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים מעיראק ומאיראן, ברובם ממעברת אגרובנק" (immigrants from
  Iraq and Iran, mostly from the Agrobank transit camp); English Wikipedia: from Iraq and Iran.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "שמו הזמני היה לכיש 3", citing HaBoker, 11 Jul 1955, not read.
- **Site note. [S, Wikipedia]** Paraphrased: both Wikipedias place it on part of the land of al-Faluja or near it.
  Hebrew Wikipedia: "המושב הוקם על גבי חלק מאדמות העיירה אל-פאלוג'ה, שתושביה עזבו אותה במלחמת העצמאות ולא הורשו
  לחזור לבתיהם".
- **Coordinates.** CBS 31.62442, 34.69475 (code 55).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44031874 ;
  https://en.wikipedia.org/w/index.php?oldid=1375072178 . All checked 2026-10-22.

### 21. Beit Nir (בית ניר), moshav shitufi, then kibbutz

- **Founded 20 July 1955. [S]**; **July 1955 [V].** Gvati: 20.7.55, region "לכיש", affiliation HaKibbutz HaArtzi,
  section "קיבוצים". Hebrew Wikipedia: "היישוב הוקם לראשונה ביולי 1955 כמושב שיתופי" (first founded in July 1955 as a
  moshav shitufi), citing Davar, 21 Jul 1955, not read. CBS: 1955.
- **Refounding. [C]** Hebrew Wikipedia: the first group left (nine members gave notice on 10 October 1956); the site
  went to a HaShomer HaTzair garin, which "עלה על הקרקע ב-11 באוגוסט 1957" (category 1957). English Wikipedia: "Beit
  Nir was established in August 1957 by members of Hashomer Hatzair" (its infobox gives the range 1955 to 1957,
  paraphrased). I prefer **July 1955 for the first settlement** and August 1957 for the kibbutz; Gvati's affiliation is
  the later kibbutz's (my reading).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: 1955 group of Israeli-born, non-party farmers' sons from the Rishon
  LeZion and Be'er Ya'akov area.
- **Renames.** None found. Named after Max Bodenheimer (Gvati, Hebrew Wikipedia).
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it near Kudna, depopulated in 1948.
- **Coordinates.** CBS 31.64845, 34.87278 (code 16).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43555551 ;
  https://en.wikipedia.org/w/index.php?oldid=1375252245 . All checked 2026-10-22.

### 22. No'am (נועם), moshav

- **Founded 7 August 1955. [S]** Gvati: 7.8.55, region "לכיש", affiliation HaPoel HaMizrachi. **Year [V]**: CBS 1955;
  Hebrew Wikipedia "שהוקם בשנת 1955 על ידי הפועל המזרחי"; English Wikipedia "established in 1955 by Hapoel
  HaMizrachi".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "המתיישבים הראשונים היו עולים ממרוקו"; English Wikipedia: immigrants
  from the Maghreb, mostly Morocco, later joined by immigrants from Iran and Iraq.
- **Renames.** None found.
- **Coordinates.** CBS 31.56802, 34.78887 (code 15).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43801230 ;
  https://en.wikipedia.org/w/index.php?oldid=1375232285 . All checked 2026-10-22.

### 23. Nir Hen (ניר ח"ן), moshav shitufi

- **Cornerstone 21 September 1955. [V]** Gvati: 21.9.55, region "לכיש", affiliation Tnuat HaMoshavim. Hebrew
  Wikipedia: the cornerstone was laid "ב-21 בספטמבר 1955" (paraphrase around a short quote),
  citing HaBoker and Al HaMishmar of 22 Sep 1955, not read.
- **Year: [C].** CBS 1955; Gvati 1955. English Wikipedia: "The moshav was founded in 1956" (its infobox gives the range 1955 to 1956,
  paraphrased). Hebrew Wikipedia: a pioneer group trained at Ibim from early 1955 and the rest of
  the garin was due in spring 1956 (category 1956). **I prefer 21 September 1955** (two sources on the day, CBS on the
  year), with 1956 as the arrival of the main group.
- **Founders. [C]** Hebrew Wikipedia: a garin of about ninety young people from Haifa, of Ihud HaKvutzot
  VeHaKibbutzim, later filled with Nahal graduates. English Wikipedia: "originally settled by Jewish refugees from
  North Africa". No third source read.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: it was first meant to be called "הילה" (Hila); the final name, Nir Hen (ח"ן = 58), marks the 58
  passengers of the El Al plane shot down over Bulgaria on 27 July 1955 (also Gvati's name origin).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "היישוב הוקם על חלק מאדמות העיירה אל-פאלוג'ה, שתושביה נמלטו ממנה
  במהלך מלחמת העצמאות".
- **Coordinates.** CBS 31.60875, 34.71465 (code 11).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42997651 ;
  https://en.wikipedia.org/w/index.php?oldid=1375071871 . All checked 2026-10-22.

### 24. Nehora (נהורה), rural centre

- **Founded: [C], year.** CBS: 1956. Hebrew Wikipedia: "נהורה הוקמה בשנת 1955 כחלק מהקמת מועצה אזורית לכיש" (founded
  1955 as part of setting up the Lachish regional council). English Wikipedia: "Nehora was founded in 1955". Not in
  Gvati. **I prefer neither year**: CBS is the official file, the two Wikipedias give no source for 1955.
- **Type. [S, Wikipedia]** Both Wikipedias: a services centre for the surrounding moshavim (commercial centre,
  regional school, council offices).
- **Founders. [GAP]** No founding group found.
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it near Karatiyya and al-Faluja.
- **Coordinates.** CBS 31.62298, 34.70558 (code 309).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43895624 ;
  https://en.wikipedia.org/w/index.php?oldid=1375070291 . All checked 2026-10-22.

### 25. Ashdod (אשדוד), transit camp, then town

- **Founded: [C], year.** CBS: 1955. Hebrew Wikipedia: "נוסדה ב-1956", and "ב-25 בנובמבר 1956 הגיעו למקום המתיישבים
  הראשונים" (on 25 November 1956 the first settlers came); the sentence goes on (paraphrased) to say they were 22
  families of immigrants from Morocco who lived in a transit camp.
  English Wikipedia: "The modern city of Ashdod was founded in 1956", and on 1 May 1956 the finance minister Levi
  Eshkol approved the city's establishment; "The first residents, 22 families from Morocco, arrived in November 1956".
  **I prefer 25 November 1956** (first settlers) **[S, Wikipedia]**, year 1956 from both Wikipedias against CBS 1955.
  Not in Gvati.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "היישוב החדש שנקרא אז "אשדוד ים"" (then called Ashdod Yam).
- **Site note. [S, Wikipedia]** English Wikipedia: "Egyptians defending Isdud withdrew in late 1948, causing most of
  Isdud's roughly 5,000 residents to flee. The 300 townspeople who remained were driven southwards by the Israel
  Defense Forces (IDF)." Hebrew Wikipedia: "ביישוב נותרו כ-300 תושבים שהניפו דגלים לבנים, אך הם גורשו לרצועת עזה זמן
  קצר לאחר מכן". English Wikipedia places the new town 6 km north-west of Isdud. Given as the sources word it, for the
  owner's sensitivity review.
- **Coordinates.** CBS 31.79538, 34.64939 (code 70).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44075250 ;
  https://en.wikipedia.org/w/index.php?oldid=1380081863 . All checked 2026-10-22.

### 26. Zohar (זוהר), moshav

- **Founded 20 February 1956. [S]** Gvati: 20.2.56, region "לכיש", affiliation HaIhud HaHaklai. **Year [V]**: CBS 1956;
  Hebrew Wikipedia "היישוב הוקם בשנת 1956"; English Wikipedia "founded in 1956".
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Algeria and Tunisia.
- **Renames.** None found.
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it on the land of al-Faluja and Burayr.
- **Coordinates.** CBS 31.5954, 34.69262 (code 44).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44031846 ;
  https://en.wikipedia.org/w/index.php?oldid=1375328381 . All checked 2026-10-22.

### 27. Kfar Silver (כפר סילבר), youth village

- **Founded: [C].** CBS: 1957. English Wikipedia: "The village was founded in 1957 on the initiative of the Zionist
  Organization of America". Hebrew Wikipedia: foundation stone on 8 May 1951; "טקס "חנוכת הבית" של כפר סילבר נערך ביום
  ב' בחול המועד פסח תשט"ז (29 במרץ 1956)" (the house-warming ceremony on 29 March 1956), and the school opened in 1956
  (category 1957). Not in Gvati. **I prefer neither**: the 1956 opening is in the window, the CBS year is not. Kept so
  the owner can see the disagreement; drop it if the window is taken by the CBS year.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: the General Zionists in Israel and the Zionist Organization of America;
  named after Abba Hillel Silver (English Wikipedia).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "בשנותיו הראשונות היה בשימוש שם היישוב 'אבא הלל' ובהמשך התקבע 'כפר
  סילבר'".
- **Coordinates.** CBS 31.67315, 34.61176 (code 107).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43883004 ;
  https://en.wikipedia.org/w/index.php?oldid=1375052747 . All checked 2026-10-22.

### 28. Sde Yoav (שדה יואב), kibbutz

- **Founded: [C], year.** CBS: 1956. Gvati: 10.5.56, region "שפלת יהודה", affiliation HaKibbutz HaArtzi. Hebrew
  Wikipedia: "היישוב עלה על הקרקע ב-2 ביוני 1966 בידי בני גרעין נח"ל "אתגר"" (on the land on 2 June 1966, a Nahal
  garin "Etgar"), while its category is 1956. English Wikipedia: infobox "founded = 1956", text "Kibbutz Sde Yoav was
  established in 1966 by members of Hashomer Hatzair". **I prefer 10 May 1956** (Gvati and CBS, the two non-wiki
  sources, agree on the year), but both Wikipedia texts say 1966; no source read explains the gap (it may be two
  foundings, my inference). Kept as **[C]**.
- **Renames.** None found. Named after Yitzhak Dubno ("Yoav") (Gvati, Hebrew Wikipedia).
- **Site note. [S, Wikipedia]** Paraphrased: English Wikipedia places it on the land of Iraq Suwaydan; Hebrew
  Wikipedia says a village of that name stood near it.
- **Coordinates.** CBS 31.64513, 34.67719 (code 293).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43744291 ;
  https://en.wikipedia.org/w/index.php?oldid=1375247769 . All checked 2026-10-22.

### 29. Sde Moshe (שדה משה), moshav

- **Founded 17 June 1956. [S]** Gvati: 17.6.56, region "לכיש", affiliation Tnuat HaMoshavim. **Year [V]**: CBS 1956;
  Hebrew Wikipedia "המושב הוקם ב-1956 בסיועה של יק"א"; English Wikipedia "founded in 1956".
- **Founders. [S, Wikipedia]** English Wikipedia: "as part of a movement from the city to the village". Hebrew
  Wikipedia: founded with ICA's help; founders' origin "מעורב" (mixed, infobox).
- **Renames. [C]** Hebrew Wikipedia: "ונקרא תחילה "שדה ישעיהו" על שם ישעיהו פרס" (first Sde Yeshayahu, after
  Yeshayahu Press). English Wikipedia: "It was originally named "Sde Eliyahu."", citing Hareuveni (1974), not read. No
  preference. Named after Baron Maurice (Moshe) de Hirsch (Gvati and both Wikipedias).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "על אדמות הכפר הערבי הנטוש עיראק אל-מנשייה".
- **Coordinates.** CBS 31.61133, 34.80146 (code 18).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42335983 ;
  https://en.wikipedia.org/w/index.php?oldid=1375073271 . All checked 2026-10-22.

### 30. Even Shmuel (אבן שמואל), rural centre

- **Founded 1956. [V]** CBS: 1956. Hebrew Wikipedia: "היישוב הוקם בשנת 1956 תחת השם הזמני "אומן"". English Wikipedia:
  infobox "founded = 1956". The day is **[GAP]**. Not in Gvati.
- **Type. [S, Wikipedia]** Hebrew Wikipedia: meant as a rural centre for the religious localities of the Lachish area,
  with a tractor station and a regional clinic. English Wikipedia: "originally founded as an agricultural moshav".
- **Founders. [GAP]** No founding group found.
- **Renames. [S, Wikipedia]** First "Oman" (Hebrew Wikipedia, quoted above).
- **Coordinates.** CBS 31.5749, 34.76544 (code 400).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43376831 ;
  https://en.wikipedia.org/w/index.php?oldid=1375231017 . All checked 2026-10-22.

### 31. Kfar HaRif (כפר הרי"ף), moshav

- **Founded November 1956. [S]**; **1956 [V].** Gvati: "11.56", region "שפלת יהודה", affiliation HaIhud HaHaklai. English Wikipedia:
  "The moshav was founded at the end of 1956" (consistent with November, but not the same claim). CBS: 1956. Hebrew Wikipedia: "כפר הרי"ף נוסד בשנת 1956 כמושב". The day
  is **[GAP]**.
- **Founders. [C]** Hebrew Wikipedia: meant for well-off Zionists from Morocco, most of whom did not come or left; their
  places were taken by immigrants from Poland and Russia, mostly Holocaust survivors. English Wikipedia: "Jewish
  refugees and immigrants to Israel from Morocco", later joined by immigrants from Eastern Europe.
- **Renames.** None found. Named after Rabbi Isaac Alfasi (Gvati, both Wikipedias).
- **Site note. [S, Wikipedia]** Paraphrased: both Wikipedias place it on or near the land of al-Masmiyya al-Saghira.
- **Coordinates.** CBS 31.74546, 34.79289 (code 888).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43211221 ;
  https://en.wikipedia.org/w/index.php?oldid=1375252074 . All checked 2026-10-22.

## Gaps and notes for the owner

- **Defunct localities** founded 1952-56 in the subdistrict were not searched for (neither CBS nor Gvati lists them);
  the Harubit and Masua transit camps (1955) are one example found by chance.
- **No newspaper of the time was read**; every newspaper date above is as Wikipedia cites it.
- **Days** confirmed by two sources only for Nir Hen (cornerstone); months confirmed by two sources for Menuha, Sgula,
  Otzem and Beit Nir. Every other day is single-source, conflicting or missing.
- **Rows that may fall outside the window**: Hatzor-Ashdod (row 10; only Gvati says 1953), Ashdod (row 25; CBS 1955,
  both Wikipedias 1956, so inside either way), Kfar Silver (row 27; CBS 1957) and Sde Yoav (row 28; both Wikipedia
  texts say 1966).
- **Today's site vs the first camp**: Timorim moved here from the Galilee in 1954; Nahala's founders first settled at
  Agur (1950) and Nir Banim's garin first at Tzur Ma'on. The CBS point is today's site. For the others no source read
  says the site moved.
- **Hexes and sensitivity flags** (armistice lines, depopulated localities) were not computed here; they belong to the
  paused data-file step. The site notes record only what the Wikipedia articles state.
