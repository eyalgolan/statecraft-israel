# Settlements founded in 1952-1956: the Negev (research note, 2026-10-22)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file and the
tool change (#61) stay paused until after the MVP. Refs #27, its 1952-56 rows, one region. It follows the format of
[`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) (#485), which covers 1949-1951; those rows are
not repeated here.

The question: which Jewish localities were founded in the Negev between 1 January 1952 and 31 December 1956, and for
each: founding date, coordinates, type, founding group, renames and sources.

## Which "Negev"

**Boundary used: the Be'er Sheva subdistrict (נפת באר שבע, code 62) of the Central Bureau of Statistics**, as given in
the CBS localities file 2024 (columns "סמל נפה" and "שם נפה"; source below). It is an official, machine-checkable line
and every locality in the file carries it. It includes the Dead Sea shore (Ein Gedi) and the Arava, and it leaves out
the Ashkelon subdistrict (61), so the Lachish area and the northern Shafir and Yoav areas are not here, even where some
sources call them "Negev" (Sde Yoav, founded 1956, is one: CBS subdistrict 61). Other boundaries are possible (the
Negev in a geographer's sense starts further south than some of these moshavim); this one was chosen because it is
the one the official source applies to every row. **Other regions of #27 (the Lachish area, the north, the centre)
are open to other claimants, and this region could also be split differently if the owner prefers another line.**

## How it was done

- Every page was fetched on 22 October 2026 with `curl` (User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`) or read through the MediaWiki
  API. No request carried a personal identifier. No web search result was used as a source.
- **The candidate pool** is the union of two lists: (1) every row of the CBS localities file 2024 in subdistrict 62
  whose "שנת ייסוד" (year of founding) is 1952 to 1956: 24 rows; (2) every row of Haim Gvati's list of rural
  settlements whose "תאריך הקמה" (date of founding) falls in 1952-1956 and whose name matches a subdistrict-62 row of
  the CBS file: 17 rows, all of them already in list 1. A third
  check, the Hebrew Wikipedia categories "יישובים שהוקמו ב-1952" to "יישובים שהוקמו ב-1956" intersected with
  "הנגב: יישובים", "הנגב המערבי: יישובים" and "הערבה: יישובים", added one row, Ami'oz (CBS 1957, Gvati 1957,
  both Wikipedias 1956), kept as row 25 with **[C]**. That gives **25 rows**.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024 ("קובץ יישובים
  2024"), https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22), columns
  "שנת ייסוד" and "קואורדינטות". As in `settlements-1949-51.md`, I read the twelve-digit coordinate field as an Israel
  Transverse Mercator easting and northing (six digits each) and converted it with `pyproj` (EPSG:2039 to EPSG:4326).
  That reading is mine, not stated in the file; the 1949-51 note checked it against Wikipedia for 29 rows (median
  about 40 m). Coordinates below are the CBS values, rounded to five decimals. They are where the locality is
  **today**; where a source says the first camp stood elsewhere, the row says so.
- **Founding dates (day).** Gvati, *100 Years of Settlement*, vol. 2, appendix "List of all Jewish rural settlements
  in Israel", Ben-Yehuda Project edition, https://benyehuda.org/read/14684 (checked 2026-10-22). Its sections
  ("קיבוצים", "מושבים", "מושבים שיתופיים") give the type used below for rural rows; its columns are name, region
  ("אזור גיאוגרפי"), date of founding, affiliation ("השתייכות ארגונית") and origin of the name. The column does not say
  whether the date is the day on the land or a later recognition. It is one secondary compilation. Its section of
  urban settlements ("ישובים עירוניים שנוסדו כמושבות") lists no development towns.
- **Wikipedia** (Hebrew and English) was read as wikitext of the current revision, cited by `oldid`. It is a pointer
  and a cross-check: a fact resting on Wikipedia alone is tagged **[S, Wikipedia]**, and where it cites a newspaper or
  book I give that citation as Wikipedia gives it and do not count it as read. Quotes from Wikipedia are from the
  wikitext with link markup removed (a piped link shows its display text); nothing else is changed.
- **Founding group** in Gvati is the affiliation (the settlement movement), not the people. The people (country of
  origin, garin) come from Wikipedia or the locality's own site and are tagged as such.
- Hebrew dates were converted with the `convertdate` library.

Tags, one per fact, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. Both values and the sources on each side are given, and which I prefer.
- **[GAP]**: not found in any source read.

CBS year plus Gvati day agreeing on the year counts as [V] for the year; the day stays [S] unless a second source
gives the same day.

### Sources not reached

- **Regional council and municipal sites**, HTTP 403 to both `curl` and the WebFetch tool: Merhavim
  (`https://www.merchavim.org.il/קלחים/` and six sibling pages), Sdot Negev (`https://www.sdotnegev.org.il/21/`),
  Bnei Shimon (`https://www.bns.org.il/`), Eshkol (`https://www.eshkol.info/`), Dimona
  (`https://www.dimona.muni.il/אודות-העיר/`), Netivot (`https://www.netivot.muni.il/`).
- **Eshel HaNasi "about" page** cited by Hebrew Wikipedia, http://www.eshelnet.com/page/1610/123: HTTP 404.
- **Official gazette (Yalkut HaPirsumim) PDFs on Nevo** cited by Hebrew Wikipedia for names (Yalkut 326, 31 Dec 1953;
  Yalkut 617, 10 Jun 1956; Kovetz HaTakanot 763): Nevo returns a captcha page, not the PDF.
- **Kibbutz Ein Gedi** page cited by Hebrew Wikipedia: redirects to a page that returned no content. **Sde Boker**
  site: reached, but the page read carries no founding text.
- **Not tried, known blocked** (`settlements-1949-51.md`): the NLI historical press (JPress) and the Israel State
  Archives. So **no contemporaneous newspaper was read for any row**. Books cited by Wikipedia and not read: Ze'ev
  Vilnai, *Encyclopaedia of the Land* (Hebrew), vol. 3 (1956), pp. 1043 and 1084; Amikam Osem, *To Dream with Open
  Eyes: Sde Boker, the First 50 Years* (2007).
- **Localities founded 1952-56 that no longer exist** are not in the CBS file and not in Gvati. They were not searched
  for. **[GAP]**

## Summary table

Coordinates: CBS 2024, lat N, lon E, today's site. "Founded" is the best-supported date; see the row for the sources.

| # | Name | Type | Founded | Lat, lon (CBS) | Renames |
|---|---|---|---|---|---|
| 1 | Shibbolim / שיבולים | Moshav | 17 Feb 1952 (Gvati) or 22 Feb 1952 (en Wikipedia) **[C]** | 31.39571, 34.60758 | First "Sharsheret B" |
| 2 | Sde Boker / שדה בוקר | Cooperative farm, later kibbutz | 15 May 1952 **[V]** | 30.87327, 34.79317 | None found |
| 3 | Lahav / להב | Nahal group, kibbutz | 7 Aug 1952 **[S]**; 1952 **[V]** | 31.37933, 34.86865 | First "Tziklag" |
| 4 | Giv'olim / גבעולים | Moshav | 24 Sep 1952 **[S]**; 1952 **[V]** | 31.39682, 34.59153 | First "Sharsheret" (see row) |
| 5 | Eshel HaNasi / אשל הנשיא | Agricultural school and youth village | 1952 **[V]**; school 1951 **[S, Wikipedia]** | 31.32592, 34.69744 | None found |
| 6 | Melilot / מלילות | Moshav | 4 May 1953 **[S]**; 1953 **[V]** | 31.38951, 34.59522 | First "Sharsheret D" |
| 7 | Nir Akiva / ניר עקיבא | Moshav | 1 Jun 1953 **[V]** | 31.46987, 34.6466 | "Shoval 2", "Sdot Akiva" (May 1954), then Nir Akiva |
| 8 | Nir Moshe / ניר משה | Moshav | 3 Jun 1953 **[S]**; 1953 **[V]** | 31.47747, 34.63091 | First "Shoval Mizrahi 1" |
| 9 | Pa'amei Tashaz / פעמי תש"ז | Moshav | 4 Jun or 11 Jun 1953 **[C]** | 31.43913, 34.6928 | None found |
| 10 | Berosh / ברוש | Moshav | 25 Jun 1953 **[S]**; 1953 **[V]** | 31.37051, 34.63301 | None found |
| 11 | Talmei Bilu / תלמי ביל"ו | Moshav | 25 Jun 1953 **[S]**; 1953 **[V]** | 31.4378, 34.64514 | First "Shoval 8" |
| 12 | Tidhar / תדהר | Moshav | 9 Jul 1953 **[S]**; 1953 **[V]** | 31.37919, 34.62735 | None found |
| 13 | Zeru'a / זרועה | Moshav | 13 Aug 1953 **[S]**; 1953 **[V]** | 31.45849, 34.62411 | First "Shoval 11" |
| 14 | Te'ashur / תאשור | Moshav | 17 Aug 1953 **[S]**; 1953 **[V]** | 31.37182, 34.64322 | None found |
| 15 | Sde Tzvi / שדה צבי | Moshav | 1953 (CBS, both Wikipedias) or 3 Jun 1950 (Gvati) **[C]** | 31.44883, 34.71224 | First "Shoval 5" |
| 16 | Ein Gedi / עין גדי | Nahal outpost, then kibbutz | 1953 outpost, 1956 kibbutz; Gvati 26 Feb 1950 **[C]** | 31.45264, 35.3847 | None found |
| 17 | Mitzpe Ramon / מצפה רמון | Road workers' camp, then town | 1951, 1953, 1954 or 1956 **[C]** | 30.6229, 34.80162 | None found |
| 18 | Kelahim / קלחים | Moshav | 30 Aug 1954 **[V]** | 31.4494, 34.67791 | "Shoval 4" ("Shoval Daled"), "Shadma" |
| 19 | Eshbol / אשבול | Moshav | 20 Mar 1955 (Gvati) **[S]**; 1955 or 1954 **[C]** | 31.44754, 34.66601 | First "Shoval G" |
| 20 | Ofakim / אופקים | Development town | 19 Apr 1955 **[S, Wikipedia]**; 1955 **[V]** | 31.30652, 34.62082 | None found |
| 21 | Dimona / דימונה | Development town | 19 Sep 1955 **[S, Wikipedia]**; 1955 **[V]** | 31.07034, 35.03609 | None found |
| 22 | Nir Oz / ניר עוז | Nahal outpost, kibbutz | 22 Sep, 29 Sep or 1 Oct 1955 **[C]** | 31.31079, 34.40175 | None found |
| 23 | Kerem Shalom / כרם שלום | Nahal outpost (1956), kibbutz (1967 or 1968) | 13 Jun 1956 **[V]** (outpost) | 31.2277, 34.28432 | First "Kerem Avshalom" |
| 24 | Netivot / נתיבות | Transit camp (ma'abara), then town | 1956 **[V]** | 31.42234, 34.58307 | First "Azata", renamed 1957 |
| 25 | Ami'oz / עמיעוז | Moshav | 1956 (both Wikipedias) or 1957 (CBS; Gvati 10 May 1957) **[C]** | 31.24846, 34.41295 | None found |

Rows: 25. By type: 15 moshavim, 4 kibbutzim (Sde Boker, Lahav, Ein Gedi, Nir Oz) plus Kerem Shalom as an outpost, 4
towns (Mitzpe Ramon, Ofakim, Dimona, Netivot), 1 school village (Eshel HaNasi).

## Rows

Common sources, cited by short name in every row:
- **CBS**: https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22).
- **Gvati**: https://benyehuda.org/read/14684 , appendix table (checked 2026-10-22).

### 1. Shibbolim (שיבולים), moshav

- **Founded: [C], day.** Gvati: "שִבָּלִים", region "הנגב המערבי", 17.2.52, affiliation HaPoel HaMizrachi, section
  "מושבים". English Wikipedia: "established on 22 February 1952". **Year 1952 [V]**: CBS 1952, Hebrew Wikipedia 1952.
  I prefer Gvati's 17 February only because it is not Wikipedia; neither day is confirmed by a second source.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "עולים מאיראן ומכורדיסטן" (immigrants from Iran and Kurdistan);
  English Wikipedia the same, adding they "had previously been living in Yakhini".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "ונקרא בתחילה "שרשרת ב'"" ("first called Sharsheret B"); English
  Wikipedia: "Sharsheret Bet".
- **Coordinates.** CBS 31.39571, 34.60758 (CBS code 865).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43649484 ;
  https://en.wikipedia.org/w/index.php?oldid=1376017231 . All checked 2026-10-22.

### 2. Sde Boker (שדה בוקר), cooperative farm, later kibbutz

- **Founded 15 May 1952. [V]** Gvati: 15.5.52, region "הר הנגב", affiliation HaIhud, section "קיבוצים". English
  Wikipedia: "established on 15 May 1952". Hebrew Wikipedia: "בהמשך נקבע יום העלייה לקרקע ל-15 במאי 1952" (the day
  on the land was later set as 15 May 1952), after a founders' meeting on 10 January 1952. CBS: 1952. The Hebrew Wikipedia infobox
  gives "תאריך ייסוד הגרעין" (the garin's founding) as 1949; not used for the settlement.
- **Type. [S, Wikipedia]** Hebrew Wikipedia: registered first as an agricultural cooperative, an association named "שדה בוקר" with the
  description "חווה לגידול מקנה" (a livestock farm; the full name is paraphrased here, not quoted); the members decided in October 1955 to be a kibbutz, and it joined Ihud HaKvutzot
  VeHaKibbutzim after a vote in April 1962. Gvati lists it under kibbutzim.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia names ten founders from the founding meeting's minutes (citing Osem,
  not read). English Wikipedia: "former soldiers".
- **Renames.** None found. Name origin, Gvati: "מן השם הערבי של ההר הסמוך: שג’רת אל-בקר" (from the Arabic name of the
  nearby hill).
- **Site note.** Hebrew Wikipedia: "בעת הקמת שדה בוקר הרחיק הממשל הצבאי על ערביי ישראל את שבטי הבדואים מהאזור". Given
  as the source words it, for the owner's sensitivity review.
- **Coordinates.** CBS 30.87327, 34.79317 (code 885).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43911013 ;
  https://en.wikipedia.org/w/index.php?oldid=1376043964 . All checked 2026-10-22.

### 3. Lahav (להב), kibbutz

- **Founded 7 August 1952. [S]** Gvati: 7.8.52, region "הנגב הצפוני", affiliation HaKibbutz HaArtzi. **Year [V]**: CBS
  1952; both Wikipedias 1952.
- **Founders. [V]** A Nahal garin: Gvati's name origin "על-פי שם הגרעין המייסד" (after the founding garin); Hebrew
  Wikipedia "על שם גרעין הנח"ל שהקים אותו"; English Wikipedia "in honour of the Nahal group which established it".
- **Renames. [S, Wikipedia]** Both Wikipedias: first named "Tziklag" (צקלג), after biblical Ziklag, then Lahav. No
  date for the change is given.
- **Coordinates.** CBS 31.37933, 34.86865 (code 2023).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43333992 ;
  https://en.wikipedia.org/w/index.php?oldid=1375256607 . All checked 2026-10-22.

### 4. Giv'olim (גבעולים), moshav

- **Founded 24 September 1952. [S]** Gvati: 24.9.52, region "הנגב הצפוני", affiliation HaPoel HaMizrachi. **Year [V]**:
  CBS 1952; both Wikipedias 1952.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "עולים מעיראק ומכורדיסטן"; English Wikipedia: immigrants from Iraq and
  Kurdistan.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia, verbatim: "ונקרא בתחילה "שרשרת דג'"". The form "דג'" looks garbled
  (my reading); it may be meant as "Sharsheret D", which Hebrew Wikipedia also gives for Melilot (row 6). Not
  resolved. **[GAP]** for the exact temporary name.
- **Coordinates.** CBS 31.39682, 34.59153 (code 2014).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44038960 ;
  https://en.wikipedia.org/w/index.php?oldid=1376015871 . All checked 2026-10-22.

### 5. Eshel HaNasi (אשל הנשיא), agricultural school and youth village

- **Founded 1952. [V]** CBS: 1952. Hebrew Wikipedia: the agricultural school and the youth village "הוקמו בשנת 1952",
  opening ceremony at the end of November 1952 (citing HaTzofe, 24 Nov 1952, not read). English Wikipedia: "The
  following year an agricultural school and youth village were founded on the site". **[S, Wikipedia]** An earlier
  school on the site in 1951: English Wikipedia, and Hebrew Wikipedia's history section ("בית הספר "אשל הנשיא הוקם
  ב-1951"); Hebrew Wikipedia's opening section instead says school and youth village were both founded in 1952, so
  that page differs between its two sections. Not in Gvati.
- **Type.** Agricultural school and youth village (both Wikipedias). It is not a moshav or kibbutz.
- **Founders. [GAP]** No founding group found; Hebrew Wikipedia names the village's first director.
- **Renames.** None found. Name: after the tamarisk (eshel) and President Chaim Weizmann (both Wikipedias).
- **Coordinates.** CBS 31.32592, 34.69744 (code 2021).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44014217 ;
  https://en.wikipedia.org/w/index.php?oldid=1380198390 . All checked 2026-10-22.

### 6. Melilot (מלילות), moshav

- **Founded 4 May 1953. [S]** Gvati: 4.5.53, region "נגב מערבי", affiliation HaPoel HaMizrachi. **Year [V]**: CBS 1953;
  Hebrew Wikipedia 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "עולים מאיראן ומכורדיסטן".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "ונקרא בתחילה "שרשרת ד'"" (first Sharsheret D).
- **Coordinates.** CBS 31.38951, 34.59522 (code 2044).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44038961 . No English Wikipedia article was
  found under "Melilot" (it redirects to the plant genus). Checked 2026-10-22.

### 7. Nir Akiva (ניר עקיבא), moshav

- **Founded 1 June 1953. [V]** Gvati: 1.6.53, region "הנגב המערבי", affiliation Tnuat HaMoshavim. Hebrew Wikipedia:
  "הוקם ב-1 ביוני 1953". CBS: 1953. English Wikipedia: 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Romania, joined by a garin of young Moroccan
  immigrants.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: temporarily "שובל 2"; in May 1954 named "שדות עקיבא" (citing Al
  HaMishmar, 16 May 1954, not read); about a year later "ניר עקיבא" (citing Yalkut HaPirsumim 617, 10 June 1956, not
  reached). Named after Akiva Ettinger (Gvati and both Wikipedias).
- **Site note.** Hebrew Wikipedia: "על אדמות הכפר הערבי כופח'ה"; English Wikipedia: "on the lands of the Palestinian
  village of Kawfakha that was depopulated in 1948". Given as the sources word it, for the owner's sensitivity review.
- **Coordinates.** CBS 31.46987, 34.6466 (code 2048).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42995636 ;
  https://en.wikipedia.org/w/index.php?oldid=1376062452 . All checked 2026-10-22.

### 8. Nir Moshe (ניר משה), moshav

- **Founded 3 June 1953. [S]** Gvati: 3.6.53, region "הנגב המערבי", affiliation Tnuat HaMoshavim. **Year [V]**: CBS
  1953; both Wikipedias 1953.
- **Founders. [C]** Hebrew Wikipedia: "על ידי עולים ממרוקו" (Moroccan immigrants), later joined by Algerian
  immigrants. English Wikipedia: "by former city dwellers from Jerusalem, Haifa and Herzliya". No third source read;
  no preference.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "ונקרא בתחילה "שובל מזרחי 1""; English Wikipedia: "initially
  named Shoval Mizrachi 1". Named after Moshe Smilansky (Gvati and both Wikipedias).
- **Coordinates.** CBS 31.47747, 34.63091 (code 2047).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43794284 ;
  https://en.wikipedia.org/w/index.php?oldid=1376022270 . All checked 2026-10-22.

### 9. Pa'amei Tashaz (פעמי תש"ז), moshav

- **Founded: [C], day.** Gvati: 11.6.53, region "הנגב הצפוני", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "הוקם
  ביום כ"א סיון תשי"ג, 4 ביוני 1953", citing Vilnai's encyclopedia, vol. 3, p. 1043 (not read); 21 Sivan 5713 is 4
  June 1953 (`convertdate`). **Year [V]**: CBS 1953; English Wikipedia 1953. I prefer neither day: Gvati is the one
  source read directly; the 4 June date rests on a book I could not read.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "עולי יהדות כורדיסטן מאיראן"; English Wikipedia: immigrants from Iran.
- **Renames.** None found. Named after the 11 points of 1946 (Gvati and both Wikipedias).
- **Coordinates.** CBS 31.43913, 34.6928 (code 2059).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44079066 ;
  https://en.wikipedia.org/w/index.php?oldid=1376022545 . All checked 2026-10-22.

### 10. Berosh (ברוש), moshav

- **Founded 25 June 1953. [S]** Gvati: 25.6.53, region "הנגב המערבי", affiliation Tnuat HaMoshavim. **Year [V]**: CBS
  1953; both Wikipedias 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "מרבית תושביו הם יוצאי מרוקו" (most residents are of Moroccan
  origin; this describes residents, not only founders); English Wikipedia: "Moroccan immigrants making up most of the
  founders". One of the three "יחדיו" (Yahdav) moshavim with Tidhar and Te'ashur (Hebrew Wikipedia, "שלושת מושבי
  "יחדיו"").
- **Renames.** None found.
- **Coordinates.** CBS 31.37051, 34.63301 (code 2060).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43596485 ;
  https://en.wikipedia.org/w/index.php?oldid=1375254978 . All checked 2026-10-22.

### 11. Talmei Bilu (תלמי ביל"ו), moshav

- **Founded 25 June 1953. [S]** Gvati: 25.6.53, region "הנגב הצפוני", affiliation Hit'ahdut HaIkarim. **Year [V]**: CBS
  1953; both Wikipedias 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Romania and Iran; the Romanians left and the main
  community is of Kurdish origin. English Wikipedia: "immigrants from Kurdistan and Romania".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "כונה באופן זמני שובל 8" (temporarily Shoval 8); the permanent name
  marks 70 years since the Bilu immigration (citing Yalkut HaPirsumim 326, 31 Dec 1953, not reached).
- **Coordinates.** CBS 31.4378, 34.64514 (code 2050).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42995692 ;
  https://en.wikipedia.org/w/index.php?oldid=1376024606 . All checked 2026-10-22.

### 12. Tidhar (תדהר), moshav

- **Founded 9 July 1953. [S]** Gvati: 9.7.53, region "הנגב המערבי", affiliation Tnuat HaMoshavim. **Year [V]**: CBS
  1953; both Wikipedias 1953.
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Morocco.
- **Renames.** None found.
- **Coordinates.** CBS 31.37919, 34.62735 (code 2061).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43790203 ;
  https://en.wikipedia.org/w/index.php?oldid=1375582165 . All checked 2026-10-22.

### 13. Zeru'a (זרועה), moshav

- **Founded 13 August 1953. [S]** Gvati: 13.8.53, region "הנגב הצפוני", affiliation HaPoel HaMizrachi. **Year [V]**: CBS
  1953; both Wikipedias 1953.
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Morocco.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "ונקרא בתחילה "שובל 11"" (citing HaTzofe, 24 Aug 1954, not read).
  Name from Jeremiah 2:2 (Gvati and Hebrew Wikipedia).
- **Coordinates.** CBS 31.45849, 34.62411 (code 2064).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42996432 ;
  https://en.wikipedia.org/w/index.php?oldid=1376019034 . All checked 2026-10-22.

### 14. Te'ashur (תאשור), moshav

- **Founded 17 August 1953. [S]** Gvati: 17.8.53, region "הנגב המערבי", affiliation Tnuat HaMoshavim. **Year [V]**: CBS
  1953; both Wikipedias 1953.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "נוסד ב-1953 על ידי עולים ממרוקו", later joined mainly by
  Iraqi-born settlers. English Wikipedia: Moroccan immigrants.
- **Renames.** None found.
- **Coordinates.** CBS 31.37182, 34.64322 (code 2062).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42335662 ;
  https://en.wikipedia.org/w/index.php?oldid=1375581844 . All checked 2026-10-22.

### 15. Sde Tzvi (שדה צבי), moshav

- **Founded: [C], year.** CBS: 1953. Hebrew Wikipedia: "הוקם בשנת 1953"; English Wikipedia: 1953. Gvati: 3.6.50,
  region "הנגב הצפוני", affiliation HaIhud HaHaklai. **I prefer 1953** (three sources, one of them official, and the
  temporary name "Shoval 5" fits the 1953 Shoval moshavim of rows 7 to 13); the Gvati date may be an error, but
  that is my inference.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Morocco and a group of families from Jerusalem;
  English Wikipedia: immigrants from North Africa.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "וכונה בתחילה "שובל 5"", the permanent name after Zvi Hirschfeld of
  Ruhama (citing Zmanim, 13 May 1954, not read). Gvati gives the same namesake.
- **Coordinates.** CBS 31.44883, 34.71224 (code 2049).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43794291 ;
  https://en.wikipedia.org/w/index.php?oldid=1376024026 . All checked 2026-10-22.

### 16. Ein Gedi (עין גדי), Nahal outpost, then kibbutz

- **Founded: [C].** CBS: 1953. Hebrew Wikipedia: "בשנת 1953 הוקמה היאחזות נח"ל" (a Nahal outpost in 1953), and "היישוב
  אוזרח והפך לקיבוץ בשנת 1956" (civilianised as a kibbutz in 1956). English Wikipedia: "The kibbutz was founded in
  1953." Gvati: 26.2.50, region "ככר ים המלח", affiliation HaIhud. **I prefer 1953 for the first settlement and 1956
  for the kibbutz**, following CBS and Hebrew Wikipedia; Gvati's 1950 is not supported by any other source read.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "רוב מייסדיו הם בוגרי תנועות הנוער וגרעיני נח"ל" (mostly graduates of
  youth movements and Nahal garinim).
- **Site. [S, Wikipedia]** Hebrew Wikipedia: the first camp was where the youth hostel is now; in July 1959 the
  kibbutz moved to a higher plateau nearby. The CBS point is today's site.
- **Renames.** None found. Name after the ancient settlement (Gvati, both Wikipedias).
- **Coordinates.** CBS 31.45264, 35.3847 (code 2042).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43556183 ;
  https://en.wikipedia.org/w/index.php?oldid=1376032724 . All checked 2026-10-22.

### 17. Mitzpe Ramon (מצפה רמון), road workers' camp, then town

- **Founded: [C].** Four dates, for different events:
  - CBS: 1954.
  - Hebrew Wikipedia: road building to Eilat began in 1951 and a camp ("מחנה העצמאות") was set up on the crater's edge;
    "ב-26 במאי 1956 הגיעו המתיישבים הראשונים למקום, בהנהגת חגי אבריאל" (first settlers on 26 May 1956, led by Haggai
    Avriel). English Wikipedia: "founded in 1951 as a camp for the workers building Highway 40".
  - The local council's own page, "הסיפור שלנו": heading "נוסד בשנת 1956" (founded 1956); in its text the road
    building began in 1953, and "בראשית אוגוסט 1953 החלה לפעול במקום אגודה שיתופית בראשות חגי אבריאל" (a cooperative
    led by Avriel started in early August 1953).
  - Hebrew Wikipedia's opening section: "היא הוקמה בשנת 1953 כמחנה עבודה לפועלים שסללו את הדרך לאילת, והפכה ליישוב
    קבע בשנת 1956" (founded 1953 as a workers' camp, a permanent settlement in 1956). In the wikitext the "1953" is a
    link whose target is the year page 1951 (`[[1951|1953]]`), so the page shows 1953 while its history section says
    1951.
  - So the camp's start is 1951 (Hebrew Wikipedia history section, English Wikipedia) or 1953 (Hebrew Wikipedia
    opening section, council page), and the town's founding 1954 (CBS) or 1956 (council,
    Hebrew Wikipedia). **I prefer 1956 for the settlement**, the date the locality itself uses; the 1951/1953 camp
    date stays as given.
- **Founders. [S, per event]** Haggai Avriel led both events, as two sources describe them: the August 1953
  cooperative (council page) and the settlers of 26 May 1956 (Hebrew Wikipedia).
- **Renames.** None found.
- **Coordinates.** CBS 30.6229, 34.80162 (code 99).
- **Sources.** CBS; https://mitzpe-ramon.muni.il/מצפה-רמון/הסיפור-שלנו/ ;
  https://he.wikipedia.org/w/index.php?oldid=43718641 ; https://en.wikipedia.org/w/index.php?oldid=1381331905 . All
  checked 2026-10-22.

### 18. Kelahim (קלחים), moshav

- **Founded 30 August 1954. [V]** Gvati: 30.8.54, region "הנגב הצפוני", affiliation HaIhud HaHaklai. Hebrew Wikipedia:
  "נוסד ביום א' אלול תשי"ד, 30 באוגוסט 1954" (citing Vilnai, vol. 3, p. 1084, not read); 1 Elul 5714 is 30 August 1954
  (`convertdate`). CBS: 1954. English Wikipedia: 1954. Hebrew Wikipedia adds the houses were ready in early 1954 and
  stood empty for some months.
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Iran, Morocco and Tunisia. Hebrew
  Wikipedia names the settling movement ("התנועה המיישבת") of 1954 as HaOved HaTzioni and the present affiliation as
  HaIhud HaHaklai ("המשתייך לארגון יישובי האיחוד החקלאי"), which matches Gvati's affiliation.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "כונה באופן זמני שובל 4"; English Wikipedia: "initially called Shoval
  Daled and then Shadma, before adopting its current name". "Shadma" is in the English article only.
- **Coordinates.** CBS 31.4494, 34.67791 (code 414).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43448072 ;
  https://en.wikipedia.org/w/index.php?oldid=1376023475 . All checked 2026-10-22.

### 19. Eshbol (אשבול), moshav

- **Founded: 20 March 1955 [S], year [C].** Gvati: 20.3.55, region "הנגב הצפוני", affiliation Tnuat HaMoshavim. CBS:
  1955. Hebrew Wikipedia: "המושב הוקם בשנת 1955", next to a farm of the "Eshbol" company set up a year before (citing
  Haaretz, 21 Mar 1955, not read). English Wikipedia: "Eshbol was established in 1954 as a farm by members of the
  "Eshbol" organisation". **I prefer 1955 for the moshav** (Gvati, CBS, Hebrew Wikipedia); 1954 is the farm.
- **Founders. [C]** Hebrew Wikipedia: Kurdish-born immigrants (from Iraq and Persia) who had lived in the Hadar
  Ramatayim transit camp. English Wikipedia: "members of the "Eshbol" organisation who were immigrants from Iran".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "היישוב נקרא בתחילה "שובל ג'"" (first Shoval G).
- **Coordinates.** CBS 31.44754, 34.66601 (code 71).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43000015 ;
  https://en.wikipedia.org/w/index.php?oldid=1376020215 . All checked 2026-10-22.

### 20. Ofakim (אופקים), development town

- **Founded 19 April 1955. [S, Wikipedia]** Hebrew Wikipedia: "אופקים נוסדה ב־19 באפריל 1955" (citing Davar, 20 Apr
  1955, not read); English Wikipedia infobox "established_date = 19 April 1955". **Year [V]**: CBS 1955. Not in Gvati.
- **Type. [S, Wikipedia]** Hebrew Wikipedia: began as a tractor station for the Merhavim moshavim, meant to grow into a
  regional centre; English Wikipedia: "established as a development town in 1955". Local council from November 1957
  or 1958: Hebrew Wikipedia gives November 1957 and also "בשנת 1958 קיבל היישוב מעמד של מועצה מקומית"; the Kovetz
  HaTakanot 763 order it cites is dated 16 January 1958.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Morocco and Tunisia. English Wikipedia: families
  "from mainly Morocco (seventeen families) and four families from Tunisia and Egypt".
- **Renames.** None found.
- **Site note.** English Wikipedia: "The Bedouin families living there fled to Al-Muharraqa, and from there they were
  expelled to the Gaza Strip." Given as the source words it, for the owner's sensitivity review.
- **Coordinates.** CBS 31.30652, 34.62082 (code 31).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44000999 ;
  https://en.wikipedia.org/w/index.php?oldid=1381331711 . All checked 2026-10-22.

### 21. Dimona (דימונה), development town

- **Founded 19 September 1955. [S, Wikipedia]** Hebrew Wikipedia: "דימונה הוקמה ב-19 בספטמבר 1955 כאשר הובאו למקום 36
  משפחות של עולים מצפון אפריקה", citing the city's "about" page (HTTP 403, not reached). **Year [V]**: CBS 1955;
  English Wikipedia "It was established in 1955." Not in Gvati.
- **Founders. [S, Wikipedia]** 36 families of immigrants from North Africa (quote above).
- **Renames.** None found. Name from Joshua 15:22 (Hebrew Wikipedia).
- **Coordinates.** CBS 31.07034, 35.03609 (code 2200).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43939076 ;
  https://en.wikipedia.org/w/index.php?oldid=1381332086 . All checked 2026-10-22.

### 22. Nir Oz (ניר עוז), Nahal outpost, kibbutz

- **Founded: [C], day.** Gvati: 22.9.55, region "הנגב המערבי", affiliation HaKibbutz HaArtzi. Hebrew Wikipedia: "הוקם
  בספטמבר 1955", with the ceremony of going onto the land "ב-29 בספטמבר 1955" (citing Davar, 27 and 30 Sep 1955, not
  read). English Wikipedia: "founded on 1 October 1955". **Year 1955 [V]**: CBS 1955. I prefer **September 1955**
  (Gvati and Hebrew Wikipedia agree on the month), with the day open between 22 and 29 September.
- **Founders. [V]** A garin of HaKibbutz HaArtzi (Gvati affiliation; Hebrew Wikipedia), as a Nahal outpost (Hebrew
  Wikipedia "כהיאחזות הנח"ל ה-15"; English Wikipedia "one of the first Nahal settlements"). Hebrew Wikipedia: the
  permanent garin arrived in April 1957.
- **Renames.** None found. Hebrew Wikipedia places the first site at "ביר גרא" opposite Khan Yunis.
- **Site note.** English Wikipedia: "the land belonged to the Palestinian village of Ma'in Abu Sitta". Given as the
  source words it, for the owner's sensitivity review.
- **Coordinates.** CBS 31.31079, 34.40175 (code 69).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42997498 ;
  https://en.wikipedia.org/w/index.php?oldid=1376004736 . All checked 2026-10-22.

### 23. Kerem Shalom (כרם שלום), Nahal outpost, later kibbutz

- **Founded 13 June 1956 as an outpost. [V]** Gvati: 13.6.56, region "הנגב המערבי", affiliation HaKibbutz HaArtzi.
  Hebrew Wikipedia: "ההיאחזות הוקמה ב-13 ביוני 1956" (citing HaTzofe, 3 Aug 1956, not read). CBS: 1956.
- **Later dates. [C]** Hebrew Wikipedia: civilianised ("אוזרחה") in 1968 and moved to its permanent site in 1971;
  English Wikipedia: "The kibbutz was established in 1967 by members of Hashomer Hatzair". Rebuilt in 2001 (both
  Wikipedias). For #27 the 1956 outpost is the founding inside the window; the kibbutz itself dates from 1967 or
  1968.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: the 1956 outpost by garinim of the religious scouts, Bnei Akiva and
  youth immigrants from Sde Eliyahu. Gvati's affiliation (HaKibbutz HaArtzi) is the later kibbutz's; the 1956
  founders named by Wikipedia are religious movements (my reading of the two together, not stated by either).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: the outpost was called "כרם אבשלום" (Kerem Avshalom), after Avshalom
  Feinberg.
- **Coordinates.** CBS 31.2277, 34.28432 (code 1085). This is the permanent site of 1971; the 1956 outpost's position
  is **[GAP]**.
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43852688 ;
  https://en.wikipedia.org/w/index.php?oldid=1381101693 . All checked 2026-10-22.

### 24. Netivot (נתיבות), transit camp, then town

- **Founded 1956. [V]** CBS: 1956. Hebrew Wikipedia: "נתיבות הוקמה בשנת 1956 כמעברה בשם "עזתה"" (founded 1956 as a
  ma'abara named Azata). English Wikipedia: "Netivot was founded in 1956". The day is **[GAP]**. Not in Gvati.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from North Africa.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "בשנת 1957 שונה שמה ל"נתיבות"" (renamed Netivot in 1957); first name
  "Azata".
- **Coordinates.** CBS 31.42234, 34.58307 (code 246).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43786905 ;
  https://en.wikipedia.org/w/index.php?oldid=1381331798 . All checked 2026-10-22.

### 25. Ami'oz (עמיעוז), moshav

- **Founded: [C].** Hebrew Wikipedia: "המושב הוקם לראשונה ב־1956" (first founded 1956) by Jews from Egypt and Romania,
  who left except four families; a Vizhnitz Hasidim attempt in 1961 failed; Moroccan immigrants settled at the end of
  1962. English Wikipedia: "first established in 1956", "permanently established in 1962". CBS: 1957. Gvati: 10.5.57,
  region "הנגב המערבי", affiliation Tnuat HaMoshavim. **I prefer 1957** (CBS and Gvati, the two non-wiki sources).
  Hebrew Wikipedia says the name commemorates the IDF in Operation Kadesh (October to November 1956); a settlement of
  that name before late 1956 would be unlikely (my inference, not stated by any source). Kept here so the owner can
  see the disagreement; drop it if the window is taken by the CBS year.
- **Founders. [S, Wikipedia]** As above (Egypt and Romania; then Morocco, 1962).
- **Renames.** None found.
- **Coordinates.** CBS 31.24846, 34.41295 (code 318).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42987738 ;
  https://en.wikipedia.org/w/index.php?oldid=1375608038 . All checked 2026-10-22.

## Gaps and notes for the owner

- **Defunct localities** founded 1952-56 in the Negev were not searched for (neither CBS nor Gvati lists them).
- **No newspaper of the time was read**; every newspaper date above is as Wikipedia cites it.
- **Days** confirmed by two sources only for Sde Boker, Nir Akiva, Kelahim and Kerem Shalom (outpost); every other
  day is single-source, conflicting or missing.
- **Today's site vs the first camp**: Ein Gedi (moved 1959) and Kerem Shalom (moved 1971) are known to have moved; the
  CBS point is today's site. For the others no source read says the site moved.
- **Hexes and sensitivity flags** (armistice lines, depopulated localities) were not computed here; they belong to the
  paused data-file step. Four site notes are recorded where the sources state them (Sde Boker, row 2; Nir Akiva, row 7;
  Ofakim, row 20; Nir Oz, row 22).
