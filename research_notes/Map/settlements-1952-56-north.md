# Settlements founded in 1952-1956: the Northern District (research note, 2026-10-22)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file and the
tool change (#61) stay paused until after the MVP. Refs #27, its 1952-56 rows, one region. It follows the format of
[`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) (#485), which covers 1949-1951; those rows are
not repeated here. Sibling note: the Negev, `settlements-1952-56-negev.md` (#1366), whose method this note copies.

The question: which Jewish localities were founded in the Northern District between 1 January 1952 and 31 December
1956, and for each: founding date, coordinates, type, founding group, renames and sources.

## Which "north"

**Boundary used: the Northern District (מחוז הצפון, code 2) of the Central Bureau of Statistics**, as given in the
CBS localities file 2024 (columns "סמל מחוז" and "סמל נפה"; source below). In that file the district has six
subdistricts: Safed (צפת, 21), Kinneret (כנרת, 22), Yizre'el (יזרעאל, 23), Akko (עכו, 24), a second subdistrict named
Yizre'el (code 25, the Nazareth area; its only Jewish locality is Nof HaGalil) and Golan (גולן, 29). All six are in
scope. It is an official, machine-checkable line and every locality in the file carries it. It leaves out the Haifa
District (3), so the Carmel coast, the Haifa bay and Wadi Ara are not here, even where some sources call them "the
north". The Golan subdistrict has no Jewish locality founded before 1967 in the CBS file, so it adds no rows. **Other
regions of #27 (the Haifa District, the centre, Jerusalem) are open to other claimants, and this region could also be
split differently if the owner prefers another line.**

## How it was done

- Every page was fetched on 22 October 2026 with `curl` (User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`) or read through the MediaWiki
  API. No request carried a personal identifier. No web search result was used as a source.
- **The candidate pool** is the union of four lists:
  1. every row of the CBS localities file 2024 in district 2 whose "שנת ייסוד" (year of founding) is 1952 to 1956:
     22 rows;
  2. every row of Haim Gvati's list of rural settlements whose "תאריך הקמה" (date of founding) falls in 1952-1956 and
     whose name matches a district-2 row of the CBS file: 21 rows, 17 of them already in list 1; the other four (Sde
     Terumot, Yif'at, Lehavot HaBashan, Ma'ale Gilboa) are kept with **[C]**;
  3. the Hebrew Wikipedia categories "יישובים שהוקמו ב-1952" to "יישובים שהוקמו ב-1957", intersected with the
     district-2 rows of the CBS file: no new row except Nof HaGalil (category 1957; kept with **[C]** because Hebrew
     Wikipedia puts the start of building in December 1956);
  4. the English Wikipedia categories "Populated places in Israel established in 1952" to "... in 1956", matched to
     the CBS file through each page's Hebrew interlanguage link: one new row, Ram-On (English Wikipedia 1953, CBS 1960),
     kept with **[C]**, and four kibbutz halves from the 1951-1954 split of the kibbutz movement, which are listed
     separately below and not counted as rows.

  That gives **28 rows**: 22 with a CBS year in the window and 6 kept with **[C]**.
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
- **Wikipedia** (Hebrew and English) was read as wikitext of the current revision, cited by `oldid` with the page
  title. It is a pointer and a cross-check: a fact resting on Wikipedia alone is tagged **[S, Wikipedia]**, and where
  it cites a newspaper or book I give that citation as Wikipedia gives it and do not count it as read. Quotes from
  Wikipedia are from the wikitext with link markup, bold markup and footnotes removed (a piped link shows its display
  text); nothing else is changed.
- **Founding group** in Gvati is the affiliation (the settlement movement), not the people. The people (country of
  origin, garin) come from Wikipedia and are tagged as such.
- Hebrew dates were converted with the `convertdate` library.

Tags, one per fact, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. Both values and the sources on each side are given, and which I prefer.
- **[GAP]**: not found in any source read.

CBS year plus Gvati day agreeing on the year counts as [V] for the year; the day stays [S] unless a second source
gives the same day.

### Sources not reached

- **Regional council, municipal and moshav sites** were not read for this note. Two that Wikipedia cites were tried:
  the Megiddo Regional Council page on Midrakh Oz (`http://www.megido.org.il/info/yesh/midrach-oz.htm`, marked dead by
  English Wikipedia; not fetched again) and the Mele'a moshav site (`http://www.homee.co.il/מלאה/`, cited by Hebrew
  Wikipedia; not fetched). **[GAP]**
- **Not tried, known blocked** (`settlements-1949-51.md`): the NLI historical press (JPress) and the Israel State
  Archives. So **no contemporaneous newspaper was read for any row**; every newspaper date below is as Wikipedia
  cites it. Books cited by Wikipedia and not read: Yehuda Grinker, *The Immigration of the Atlas Jews* (Hebrew, 1973),
  pp. 57-58 (row 21).
- **Localities founded 1952-56 that no longer exist, or that the CBS file does not list**, were not searched for
  systematically. **[GAP]** One turned up in the Wikipedia categories and is noted below (Hemdat Yamim).

## Summary table

Coordinates: CBS 2024, lat N, lon E, today's site. "Founded" is the best-supported date; see the row for the sources.
"Sub" is the CBS subdistrict code.

| # | Name | Sub | Type | Founded | Lat, lon (CBS) | Renames |
|---|---|---|---|---|---|---|
| 1 | Sde Eliezer / שדה אליעזר | 21 | Moshav | 10 Feb 1952 **[S]**; 1952 **[V]**, English Wikipedia 1950 **[C]** | 33.04538, 35.56346 | None found |
| 2 | Avdon / עבדון | 24 | Moshav | 21 Apr 1952 **[S]**; 1952 **[V]** | 33.04806, 35.18002 | First "Kfar Avdon" |
| 3 | Yuval / יובל | 21 | Moshav | 25 Sep 1952 **[S]**; 1952 **[V]**, English Wikipedia 1953 **[C]** | 33.24668, 35.59795 | None found |
| 4 | Rewaya / רוויה | 23 | Moshav | 14 Oct 1952 **[S]**; 1952 **[V]** | 32.44874, 35.47341 | First "Farwana B" (both Wikipedias) |
| 5 | Midrakh Oz / מדרך עוז | 23 | Moshav (after a 1951 ma'abara) | 12 Nov 1952 (Gvati) or 12 Dec 1952 (both Wikipedias) **[C]** | 32.59507, 35.15863 | First "Mansi" |
| 6 | Yardena / ירדנה | 23 | Moshav | 4 Dec 1952 **[S]**; 1952 **[V]** | 32.56527, 35.56468 | None found |
| 7 | Migdal HaEmek / מגדל העמק | 23 | Ma'abara, then development town | 1952 (CBS) or 1953 (both Wikipedias) **[C]** | 32.67799, 35.24282 | First "Mujdal" |
| 8 | Avital / אביטל | 23 | Moshav | 2 Jun 1953 **[S]**; 1953 **[V]** | 32.55750, 35.30711 | Earlier "Ta'anakh A" |
| 9 | Perazon / פרזון | 23 | Moshav | 25 Jun 1953 **[S]**; 1953 **[V]** | 32.54577, 35.31077 | Earlier "Ta'anakh B" |
| 10 | Dishon / דישון | 21 | Moshav | 6 Jul 1953 **[S]**; 1953 **[V]** | 33.08216, 35.51898 | None found |
| 11 | Atsmon Segev / עצמון שגב | 24 | Nahal outpost (1953), village, re-founded 1979 | Aug 1953 **[S, Wikipedia]**; 1953 **[V]** | 32.86576, 35.25177 | "Nahala'im H Mi'ar", "Segev" (Oct 1953), later Atzmon |
| 12 | Shezor / שזור | 24 | Nahal outpost, then moshav | 31 Aug 1953 **[S]**; 1953 **[V]** | 32.93317, 35.35273 | None found |
| 13 | Hazor HaGelilit / חצור הגלילית | 21 | Ma'abarot, then development town | 1953 **[V]**; English Wikipedia 1952 to 1953 | 32.98321, 35.54393 | None found |
| 14 | Metav / מיטב | 23 | Moshav | 21 Jul 1954 **[S]**; 1954 **[V]** | 32.54586, 35.30081 | Earlier "Ta'anakh C" |
| 15 | Peqi'in HaHadasha / פקיעין החדשה | 24 | Moshav (first cooperative) | 6 Feb 1955 **[S]**; Feb 1955 **[V]** | 32.98395, 35.32256 | None found |
| 16 | Poriyya Illit / פוריה עילית | 22 | Re-settled 1955; separate locality 1991 | 1955 **[V]** | 32.73048, 35.54659 | Earlier "Poria-Alumot" |
| 17 | Mele'a / מלאה | 23 | Moshav | 1 Jan 1956 **[S]**; 1956 **[V]** | 32.56354, 35.23756 | None found |
| 18 | Nir Yafe / ניר יפה | 23 | Moshav | 27 Jan 1956 **[S]**; 1956 **[V]** | 32.56911, 35.24519 | None found |
| 19 | Addirim / אדירים | 23 | Moshav | 20 Feb 1956 **[V]**; Gvati year 1950 **[C]** | 32.54901, 35.27197 | None found |
| 20 | Gadish / גדיש | 23 | Moshav | 24 Feb 1956 **[S]**; 1956 **[V]** | 32.55890, 35.24482 | None found |
| 21 | Devora / דבורה | 23 | Moshav | 14 Mar 1956 **[S]**; 1956 **[V]** | 32.55280, 35.26401 | None found |
| 22 | Baraq / ברק | 23 | Moshav | 19 Aug 1956 **[S]**; 1956 **[V]** | 32.54280, 35.26485 | None found |
| 23 | Sde Terumot / שדי תרומות | 23 | Moshav | 1951 (CBS, English Wikipedia) or spring / 16 Sep 1952 (Hebrew Wikipedia, Gvati) **[C]** | 32.44050, 35.48588 | First "Farwana C" (Hebrew Wikipedia) |
| 24 | Yif'at / יפעת | 23 | Kibbutz (on a new site) | 15 or 18 Jul 1954 **[C]**; CBS 1926 **[C]** | 32.67681, 35.22479 | Temporary "Ihud HaSharon-Gevat" |
| 25 | Lehavot HaBashan / להבות הבשן | 21 | Kibbutz | 2 Nov 1945 (preferred) or 7 Aug 1952 (Gvati) **[C]** | 33.14149, 35.64734 | First "Lehavot"; "HaBashan" added later |
| 26 | Ma'ale Gilboa / מעלה גלבוע | 23 | Nahal outpost, kibbutz | 29 Nov 1962 (preferred) or 25 Jan 1952 (Gvati) **[C]** | 32.47695, 35.41875 | Nahal name "Mitzpor HaGilboa" |
| 27 | Ram-On / רם און | 23 | Moshav | 1960 (preferred) or 1953 (English Wikipedia) **[C]** | 32.52690, 35.25971 | None found |
| 28 | Nof HaGalil / נוף הגליל | 25 | Development town | 1957 (preferred), Sep 1956 (first dwellings) or Dec 1956 (start of building) **[C]** | 32.70860, 35.32337 | "Kiryat Natzrat", "Natzrat Illit" (1959), Nof HaGalil |

Rows: 28. Rows 1 to 22 have a CBS year in 1952-1956. Rows 23 to 28 are kept because a source puts them in the window
and another does not; on my reading, rows 23 and 24 belong in the window and rows 25 to 28 do not (each row says why).
By type in rows 1 to 22: 18 moshavim (Shezor and Peqi'in HaHadasha among them), 2 towns (Migdal HaEmek, Hazor
HaGelilit), Atsmon Segev (an outpost that became a village) and Poriyya Illit (a re-settled site). 13 of the 22 are in
subdistrict 23, 9 of them in the Ta'anakh area (rows 8, 9, 14, 17 to 22).

## Rows

Common sources, cited by short name in every row:
- **CBS**: https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22).
- **Gvati**: https://benyehuda.org/read/14684 , appendix table (checked 2026-10-22).

### 1. Sde Eliezer (שדה אליעזר), moshav

- **Founded 10 February 1952. [S]** Gvati: 10.2.52, region "הגליל העליון", affiliation HaOved HaTzioni, section
  "מושבים". **Year: [V] 1952, with one [C].** CBS 1952; Hebrew Wikipedia: "היישוב הוקם בשנת 1952". English Wikipedia:
  "Sde Eliezer was founded in 1950 by residents of Yesod HaMa'ala and Kiryat Shmona." I prefer 1952 (CBS, Gvati,
  Hebrew Wikipedia). Hebrew Wikipedia cites Ma'ariv, 11 July 1951, for the plan of the moshav (not read).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: residents of Yesod HaMa'ala and Kiryat Shmona, joined by immigrants
  from Iraq, Czechoslovakia, Yugoslavia, Poland and Romania. English Wikipedia lists the same towns and four of the
  countries (not Iraq).
- **Renames.** None found. Named after Robert (Eliezer) Rothschild (Gvati, English Wikipedia).
- **Site note. [S, Wikipedia]** English Wikipedia gives two versions: land bought by PICA ("According to some sources", in its words),
  or "according to Walid Khalidi, it was established on land located near the former depopulated Palestinian village
  of al-Husayniyya". Given as worded, for the owner's sensitivity review.
- **Coordinates.** CBS 33.04538, 35.56346 (code 861).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43561133 ("שדה אליעזר");
  https://en.wikipedia.org/w/index.php?oldid=1378980967 ("Sde Eliezer"). All checked 2026-10-22.

### 2. Avdon (עבדון), moshav

- **Founded 21 April 1952. [S]** Gvati: 21.4.52, region "הגליל העליון", affiliation Tnuat HaMoshavim. **Year [V]**: CBS
  1952; Hebrew Wikipedia "המושב הוקם בשנת 1952"; English Wikipedia "The village was established in 1952".
- **Type. [S, Wikipedia]** Hebrew Wikipedia: in its first days it served as a work village ("ככפר עבודה").
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Tunisia and Iran.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "שקראו ליישוב תחילה "כפר עבדון"" (first called Kfar Avdon); English
  Wikipedia: "initially named Kfar Avdon". Name from Joshua 21:30 (Gvati).
- **Coordinates.** CBS 33.04806, 35.18002 (code 892).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43794334 ("עבדון");
  https://en.wikipedia.org/w/index.php?oldid=1374528082 ("Avdon"). All checked 2026-10-22.

### 3. Yuval (יובל), moshav

- **Founded 25 September 1952. [S]** Gvati: 25.9.52, region "הגליל העליון", affiliation Tnuat HaMoshavim. **Year: [V]
  1952, with one [C].** CBS 1952; Hebrew Wikipedia "המושב נוסד בשנת 1952". English Wikipedia: "Yuval was founded in
  1953 by evacuees from the Old City of Jerusalem". I prefer 1952 (CBS, Gvati, Hebrew Wikipedia).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "מקימי המושב היו פליטי הרובע היהודי בירושלים העתיקה, רובם ממוצא
  כורדי" (refugees from the Jewish Quarter of the Old City, most of Kurdish origin; citing Davar, 5 Jan 1955, not
  read). Most founders left in 1955 (Hebrew Wikipedia) or "In the early 1960s" (English Wikipedia); immigrants from
  Cochin followed (both).
- **Renames.** None found. English Wikipedia also uses the form "Kfar Yuval" (infobox and text).
- **Site note. [C, Wikipedia]** Hebrew Wikipedia: "חלקו על אדמות הכפר הערבי אבל אל-קמח" (partly on the lands of the
  Arab village Abil al-Qamh). English Wikipedia: "established on the land of the depopulated Palestinian village of
  al-Zūq al-Fauqānī". The two name different villages; not resolved.
- **Coordinates.** CBS 33.24668, 35.59795 (code 2009).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43790298 ("יובל (מושב)");
  https://en.wikipedia.org/w/index.php?oldid=1378480963 ("Yuval"). All checked 2026-10-22.

### 4. Rewaya (רוויה), moshav

- **Founded 14 October 1952. [S]** Gvati: "רְוָיָה", 14.10.52, region "בקעת בית-שאן", affiliation HaPoel HaMizrachi.
  **Year [V]**: CBS 1952; Hebrew Wikipedia "המושב הוקם בשנת 1952 על ידי ארגון "הפועל המזרחי""; English Wikipedia
  1952.
- **Founders. [C]** Hebrew Wikipedia: first about 20 immigrants from Kurdistan from the Zar'in ma'abara, who had to
  leave; after three months empty, immigrants from Morocco "שהובאו ישר מהאנייה" arrived in August 1954 (citing
  HaTzofe, Al HaMishmar and Haaretz of 1954 and 1955, not read). English Wikipedia: "settled by Jewish immigrants to
  Israel from Iraq and Morocco".
- **Renames. [S, Wikipedia]** English Wikipedia: "It was originally named "Farwana B"". Hebrew Wikipedia: "רוויה כונתה
  פרונה ב'" (spelled פרונה); the present name was given at the start of 1953 (citing HaTzofe, 16 Apr 1953, and Herut,
  10 Jun 1953, not read). Its Sde Terumot page calls Sde Terumot "כפרוונה ג'" (as Farwana C, row 23), so the letters
  agree.
- **Coordinates.** CBS 32.44874, 35.47341 (code 2016).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42595901 ("רוויה (מושב)");
  https://en.wikipedia.org/w/index.php?oldid=1374536313 ("Revaya"). All checked 2026-10-22.

### 5. Midrakh Oz (מדרך עוז), moshav

- **Founded: [C], day.** Gvati: 12.11.52, region "עמק יזרעאל", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: "וב-12
  בדצמבר 1952 הוקם המושב שתחילה נקרא מנסי ולאחר מכן שונה שמו למדרך עוז". English Wikipedia: "on 12 December 1952
  Moshav was established", citing the Megiddo Regional Council site (dead link, not read). **Year 1952 [V]**: CBS
  1952. Same day of the month, one month apart; I prefer neither.
- **Type. [S, Wikipedia]** Both Wikipedias: a ma'abara named Mansi was set up in 1951 and dismantled a year later,
  when the moshav was founded.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "המושב הוקם בשנת 1952 על ידי עולים מתימן" (immigrants from Yemen).
- **Renames. [S, Wikipedia]** First "Mansi", after the Arab village (both Wikipedias). Name from the Song of Deborah,
  "תדרכי נפשי עוז": Gvati cites "(שופטים ה', י"א)", Hebrew Wikipedia Judges 5:21 (כא).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: the ma'abara stood on the lands of the Arab village al-Mansi,
  "שננטש במלחמת העצמאות". English Wikipedia places it "near the former depopulated Palestinian villages of Al-Mansi
  and Al-Ghubayya al-Tahta". Given as worded.
- **Coordinates.** CBS 32.59507, 35.15863 (code 2029).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43128000 ("מדרך עוז");
  https://en.wikipedia.org/w/index.php?oldid=1374534278 ("Midrakh Oz"). All checked 2026-10-22.

### 6. Yardena (ירדנה), moshav

- **Founded 4 December 1952. [S]** Gvati: 4.12.52, region "בקעת בית-שאן", affiliation Tnuat HaMoshavim. **Year
  [V]**: CBS 1952; Hebrew Wikipedia "המושב הוקם בשנת 1952"; English Wikipedia "Yardena was founded in 1952".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Kurdistan from Jerusalem, Ramla and the Yokneam
  ma'abara; for over a year they lived in huts in Beit Yosef until their houses were built (citing Davar, 20 Apr
  1955, not read). English Wikipedia: "immigrants to Israel from Iraqi Kurdistan".
- **Renames.** None found. Named for the Jordan (Gvati, English Wikipedia).
- **Coordinates.** CBS 32.56527, 35.56468 (code 2026).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43789632 ("ירדנה");
  https://en.wikipedia.org/w/index.php?oldid=1374538132 ("Yardena"). All checked 2026-10-22.

### 7. Migdal HaEmek (מגדל העמק), ma'abara, then development town

- **Founded: [C], year.** CBS: 1952. Hebrew Wikipedia, history section: "מגדל העמק נוסדה בשנת 1953"; its page is in
  the category "יישובים שהוקמו ב-1952", and its name section cites Sha'arim of 16 December 1952 on "יישובי עולים
  חדשים בגליל" for the first months under the name Mujdal (not read), which would put a settlement there in 1952 (my
  reading). English Wikipedia: "Migdal HaEmek was established in 1953 as a ma'abara for Jews from Arab countries". Not
  in Gvati. **I prefer 1952** (CBS, and the December 1952 citation), with low confidence; the day is **[GAP]**.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: the first residents came from the Shimron and Ramat Yishai
  ma'abarot, and people from Manchuria, Harbin, Magadan and Tianjin.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "בחודשיו הראשונים נקרא היישוב מוג'דל כשם הכפר הערבי שעל אדמותיו
  הוקם" (first called Mujdal, after the Arab village on whose lands it was built); "Migdal Haim" was proposed and
  turned down; the government names committee chose Migdal HaEmek (citing Davar, 21 Apr 1953, not read).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "בשטח הכפר הערבי הנטוש אל-מוג'ידל, שתושביו ברחו לנצרת" and "הכפר
  הוכרז כנכסי נפקדים ועבר לחזקת רשות הפיתוח. חלק מתושבי הכפר היו נוצרים קתולים ובעת שהוקם ביקש נציג הכס הקדוש
  שיותר להם לחזור לכפר, אך הוא נענה בסרוב". English Wikipedia: "In July 1948 al-Mujaydil was completely destroyed due
  to aerial bombing during the operations conducted by the Golani Brigade, when the villagers fled, resulting in its
  depopulation." Given as worded.
- **Coordinates.** CBS 32.67799, 35.24282 (code 874), today's site. English Wikipedia: "The original site of the
  ma'abara was west of the current site, at Shimron hill."
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44027576 ("מגדל העמק");
  https://en.wikipedia.org/w/index.php?oldid=1380255216 ("Migdal HaEmek"). All checked 2026-10-22.

### 8. Avital (אביטל), moshav

- **Founded 2 June 1953. [S]** Gvati: 2.6.53, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**: CBS 1953;
  Hebrew Wikipedia "המושב הוקם בשנת 1953"; English Wikipedia "The village was founded in 1953".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Persia and Turkey, joined by immigrants from
  Kurdistan from the Zar'in ma'abara. English Wikipedia: "immigrants from Iran and Turkey". Hebrew Wikipedia: "אביטל
  היה היישוב הראשון שהוקם בחבל תענך" (the first settlement of the Ta'anakh area).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "המושב נקרא בעבר "תענך א'"" (earlier Ta'anakh A).
- **Site note. [S, Wikipedia]** English Wikipedia: "Avital is located on land near the former Palestinian village of
  Zir'in." Given as worded.
- **Coordinates.** CBS 32.55750, 35.30711 (code 2052).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44026492 ("אביטל");
  https://en.wikipedia.org/w/index.php?oldid=1374528153 ("Avital (moshav)"). All checked 2026-10-22.

### 9. Perazon (פרזון), moshav

- **Founded 25 June 1953. [S]** Gvati: 25.6.53, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**: CBS 1953;
  Hebrew Wikipedia "היישוב הוקם בשנת 1953 (תשי"ג)"; English Wikipedia "Prazon was founded in 1953".
- **Founders. [S, Wikipedia]** Both Wikipedias: Jewish immigrants from Kurdistan. Hebrew Wikipedia: the second
  settlement of the Ta'anakh area after Avital; with Avital and Metav it formed "גוש יעל".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "נקרא "תענך ב'"" (called Ta'anakh B). Name from Judges 5:7 (Gvati).
- **Site note. [S, Wikipedia]** English Wikipedia: "established on the lands of the former Palestinian village of
  Al-Mazar, which was depopulated during the 1948 Arab-Israeli War". Given as worded.
- **Coordinates.** CBS 32.54577, 35.31077 (code 2053).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44026510 ("פרזון");
  https://en.wikipedia.org/w/index.php?oldid=1374535978 ("Prazon"). All checked 2026-10-22.

### 10. Dishon (דישון), moshav

- **Founded 6 July 1953. [S]** Gvati: 6.7.53, region "הגליל העליון", affiliation HaOved HaTzioni. **Year [V]**: CBS
  1953; Hebrew Wikipedia "היישוב הוקם בשנת 1953"; English Wikipedia "Dishon was established in 1953".
- **Founders. [C]** Hebrew Wikipedia: "על ידי יהודים ממרוקו וקבוצת רווקים שעלו ממזרח אירופה" (Jews from Morocco and a
  group of single men from Eastern Europe). English Wikipedia: "by Jewish immigrants from Libya". No third source read;
  no preference.
- **Renames.** None found. Gvati: the name echoes the nearby Arab village "דישום".
- **Site note. [S, Wikipedia]** English Wikipedia: "at the location of the former Palestinian village of Dayshum which
  was depopulated during the 1948 Arab-Israeli War". Hebrew Wikipedia: "שננטש במלחמת העצמאות ושחורבותיו סמוכות
  ליישוב". Given as worded.
- **Coordinates.** CBS 33.08216, 35.51898 (code 2063).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43055475 ("דישון (מושב)");
  https://en.wikipedia.org/w/index.php?oldid=1377971927 ("Dishon"). All checked 2026-10-22.

### 11. Atsmon Segev (עצמון שגב), Nahal outpost, then village, re-founded 1979

- **Founded August 1953 as an outpost. [S, Wikipedia]** Hebrew Wikipedia: "היאחזות מיאר הוקמה באוגוסט 1953 במסגרת
  מבצע "ייהוד הגליל"", quoting Haaretz of 1 September 1953 (not read). **Year 1953 [V]**: CBS 1953; English
  Wikipedia: "Atzmon was established (under the name of Segev) in 1953". Not in Gvati. **[C] inside English
  Wikipedia**: its history section says "Segev was founded in 1957 as one of these villages", against its own 1953
  and the Hebrew account of civilian settlers in 1956.
- **Later dates. [S, Wikipedia]** Hebrew Wikipedia: the Nahal was to leave in March 1954; discharged soldiers came in
  late June 1956 and North African immigrants in late July 1956 (citing newspapers of 1956, not read); the place was
  renewed by the Atzmon garin, with a cornerstone ceremony on 12 February 1979. For #27 the 1953 outpost is the
  founding inside the window; the 1956 civilian settlers are a second event in the window.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "ההיאחזות הוקמה על ידי גרעינים של "איחוד הקבוצות והקיבוצים"".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "בתחילה נקראה נחלאים ח' מיאר"; named "Segev" by the names committee
  in October 1953; later Atzmon (Atsmon Segev in CBS).
- **Site note. [S, Wikipedia]** English Wikipedia: "on land near the former Palestinian village of Mi'ar, which had
  been depopulated in the 1948 Arab-Israeli War"; "Atzmon/Segev was established east of the village site." Hebrew
  Wikipedia: "במקום בו נמצא היישוב עצמון שגב שכן לפני מלחמת העצמאות כפר ערבי בשם מִעאר". Given as worded.
- **Coordinates.** CBS 32.86576, 35.25177 (code 917), today's site. Hebrew Wikipedia says the Atzmon garin "עלה לאתר
  הקבוע בשגב בשנת 1983" (moved to the permanent Segev site in 1983).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43873569 ("עצמון שגב");
  https://en.wikipedia.org/w/index.php?oldid=1374528071 ("Atzmon"). All checked 2026-10-22.

### 12. Shezor (שזור), Nahal outpost, then moshav

- **Founded 31 August 1953. [S]** Gvati: 31.8.53, region "גליל מערבי", affiliation Tnuat HaMoshavim. **Year [V]**:
  CBS 1953; Hebrew Wikipedia "היישוב הוקם בשנת 1953 כהיאחזות נח"ל, שאוזרחה בתחילת 1954" (a Nahal outpost in 1953,
  civilianised at the start of 1954); English Wikipedia "settled in 1953 by Jewish immigrants to Israel from Morocco".
- **Type. [S, Wikipedia]** Hebrew Wikipedia: a work village of the JNF, handed to the Jewish Agency in 1956. A Davar
  article of 1978 cited by Hebrew Wikipedia gives 1960; Hebrew Wikipedia explains this by a later resettlement (not
  read).
- **Founders. [S, Wikipedia]** Both Wikipedias: immigrants from Morocco; after most left, immigrants from Iraq and
  Iran.
- **Renames.** None found. Named after an ancient town (Gvati, English Wikipedia).
- **Coordinates.** CBS 32.93317, 35.35273 (code 527).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43790238 ("שזור");
  https://en.wikipedia.org/w/index.php?oldid=1374537048 ("Shezor"). All checked 2026-10-22.

### 13. Hazor HaGelilit (חצור הגלילית), ma'abarot, then development town

- **Founded 1953. [V]** CBS: 1953. Hebrew Wikipedia: the town began with two ma'abarot, "Hatzor A" and "Hatzor B",
  "שהוקמו לצורך קליטת העולים החדשים שהגיעו לארץ בגל העלייה ההמונית בשנת 1953" (the sentence can be read as dating
  the ma'abarot or the wave of immigration to 1953; my reading). English Wikipedia (paraphrased, its dash not reproduced): founded
  in 1952 to 1953 as a transit camp; infobox 1953. Not in Gvati. The day is **[GAP]**.
- **Later dates. [V]** Local council in 1956 (both Wikipedias).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "מייסדי חצור הגלילית היו עולים מצפון אפריקה".
- **Renames.** None found. Named after Tel Hazor (both Wikipedias).
- **Site note. [S, Wikipedia]** English Wikipedia: "The new camp was located at a site near the former depopulated
  Palestinian village of Mughr al-Khayt". Given as worded.
- **Coordinates.** CBS 32.98321, 35.54393 (code 2034).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43678098 ("חצור הגלילית");
  https://en.wikipedia.org/w/index.php?oldid=1380256976 ("Hatzor HaGlilit"). All checked 2026-10-22.

### 14. Metav (מיטב), moshav

- **Founded 21 July 1954. [S]** Gvati: 21.7.54, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**: CBS 1954;
  Hebrew Wikipedia "מיטב הוקם בשנת 1954"; English Wikipedia "established in 1954".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Iraq and Iraqi Kurdistan; English Wikipedia:
  "immigrants from Kurdistan and Iraq".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "הוא נקרא "תענך ג'"" (called Ta'anakh C).
- **Site note. [S, Wikipedia]** English Wikipedia: "on the land of the Palestinian village of Al-Mazar, which had been
  depopulated in the 1948 Arab-Israeli War". Given as worded.
- **Coordinates.** CBS 32.54586, 35.30081 (code 2054).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42327771 ("מיטב (מושב)");
  https://en.wikipedia.org/w/index.php?oldid=1374534031 ("Meitav, Israel"). All checked 2026-10-22.

### 15. Peqi'in HaHadasha (פקיעין החדשה), moshav

- **Founded 6 February 1955. [S]** Gvati: "פְקִיעִין החדשה", 6.2.55, region "הגליל העליון", affiliation Tnuat
  HaMoshavim. **Month [V]**: Hebrew Wikipedia "הקבוצה החלוצית הראשונה הגיעה בפברואר 1955" (the first group came in
  February 1955), citing HaBoker of 16 February 1955 on the opening (not read). CBS 1955; English Wikipedia 1955.
- **Type. [S, Wikipedia]** Hebrew Wikipedia: "תחילה כמושב עובדים שיתופי" (first a cooperative moshav). Gvati lists it
  under moshavim.
- **Founders. [V]** Two groups of immigrants from Morocco (both Wikipedias), the second in October 1955, at the
  initiative of President Yitzhak Ben-Zvi (English Wikipedia; Hebrew Wikipedia in the same sense).
- **Renames.** None found. Named after the old village of Peqi'in (Gvati, Hebrew Wikipedia).
- **Coordinates.** CBS 32.98395, 35.32256 (code 281).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42329725 ("פקיעין החדשה");
  https://en.wikipedia.org/w/index.php?oldid=1374535878 ("Peki'in HaHadasha"). All checked 2026-10-22.

### 16. Poriyya Illit (פוריה עילית), re-settled 1955

- **Founded 1955. [V]** CBS: 1955. Hebrew Wikipedia: "היישוב הוקם מחדש ב־1955 ופוריה עילית הייתה ליישוב מבחינה
  רשמית בעת היפרדותה מפוריה - נווה עובד בשנת 1991" (re-founded 1955; a separate locality from 1991). English
  Wikipedia: "Poria Illit was established  in 1955." (two spaces as in the source). Not in Gvati. The day is
  **[GAP]**.
- **Earlier history. [S, Wikipedia]** Hebrew Wikipedia: first settled in 1912 as a farm; English Wikipedia's infobox
  gives "1912 (original)", "1940 (first re-establishment)" and "1955 (second re-establishment)".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: in 1955 "הגיעה קבוצת פועלים להתיישב במקום" (a group of workers came
  to settle).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "שם היישוב היה פוריה-אלומות"; English Wikipedia: "Poria Alumot".
- **Coordinates.** CBS 32.73048, 35.54659 (code 1313).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43177261 ("פוריה עילית");
  https://en.wikipedia.org/w/index.php?oldid=1374535939 ("Poria Illit"). All checked 2026-10-22.

### 17. Mele'a (מלאה), moshav

- **Founded 1 January 1956. [S]** Gvati: 1.1.56, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**: CBS 1956;
  Hebrew Wikipedia "המושב הוקם בשנת 1956"; English Wikipedia "It was founded in 1956".
- **Founders. [C]** Hebrew Wikipedia: "על ידי עולים מפולין" (immigrants from Poland), most of whom left by the end of
  1957 and were replaced by immigrants from Morocco. English Wikipedia: "by Jewish immigrants and refugees to Israel
  from Morocco". English Wikipedia also gives the reverse order: "by the end of 1957 most of the original
  settlers moved out, with Jewish immigrants from Poland moving in to replace them". I prefer the Hebrew account
  (more detail), but that is my reading.
- **Renames.** None found. Gvati: from the verse "המלאה הזרע אשר תזרעו", which it cites as "(דברים י"ב, ט')".
- **Coordinates.** CBS 32.56354, 35.23756 (code 164).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43761711 ("מלאה");
  https://en.wikipedia.org/w/index.php?oldid=1374534604 ("Mle'a"). All checked 2026-10-22.

### 18. Nir Yafe (ניר יפה), moshav

- **Founded 27 January 1956. [S]** Gvati: 27.1.56, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**: CBS
  1956; Hebrew Wikipedia "המושב הוקם בשנת 1956"; English Wikipedia "It was founded in 1956".
- **Founders. [V]** Immigrants from Tunisia (both Wikipedias). Hebrew Wikipedia: organised for settlement while still
  in Tunisia, they skipped the ma'abara stage.
- **Renames.** None found. Gvati: named after a Jewish donor from South Africa.
- **Coordinates.** CBS 32.56911, 35.24519 (code 165).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43165996 ("ניר יפה");
  https://en.wikipedia.org/w/index.php?oldid=1374535456 ("Nir Yafeh"). All checked 2026-10-22.

### 19. Addirim (אדירים), moshav

- **Founded 20 February 1956. [V]** Hebrew Wikipedia: "אדירים נוסד ב-20 בפברואר 1956, ח' באדר ה'תשט"ז, ראשון
  למושבי גוש "חבר""; 8 Adar 5716 is 20 February 1956 (`convertdate`). English Wikipedia: "The moshav founded on 20
  February 1956". CBS: 1956. **[C] with Gvati**: 20.2.50, region "תענך", affiliation Tnuat HaMoshavim. Same day and
  month, year 1950; the Ta'anakh moshavim of rows 8 and 17 to 22 date from 1953 to 1956 in Gvati itself, so I read
  Gvati's 1950 as a misprint, but that is my inference.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים ממרוקו, מהרי האטלס" (immigrants from Morocco, from
  the Atlas mountains); English Wikipedia: "by immigrants from Morocco".
- **Renames.** None found. Gvati: from "מספל אדירים הקריבה חמאה", which it cites as "(שופטים ה', י"ג)".
- **Coordinates.** CBS 32.54901, 35.27197 (code 113).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42923099 ("אדירים");
  https://en.wikipedia.org/w/index.php?oldid=1374527455 ("Adirim"). All checked 2026-10-22.

### 20. Gadish (גדיש), moshav

- **Founded 24 February 1956. [S]** Gvati: 24.2.56, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**: CBS
  1956; Hebrew Wikipedia "בשנת 1956"; English Wikipedia "The moshav was founded in 1956".
- **Founders. [V]** Immigrants from Morocco (both Wikipedias).
- **Renames.** None found.
- **Coordinates.** CBS 32.55890, 35.24482 (code 145).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42326309 ("גדיש");
  https://en.wikipedia.org/w/index.php?oldid=1374530140 ("Gadish"). All checked 2026-10-22.

### 21. Devora (דבורה), moshav

- **Founded 14 March 1956. [S]** Gvati: "דְבוֹרָה", 14.3.56, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**:
  CBS 1956; Hebrew Wikipedia "היישוב הוקם בשנת 1956"; English Wikipedia "The village was founded in 1956".
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "על ידי עולים מגרעין "שובה" מרקש שבמרוקו" (citing Grinker, pp.
  57-58, not read); English Wikipedia: "by immigrants from Morocco".
- **Renames.** None found. Named after Deborah the prophetess (Gvati).
- **Coordinates.** CBS 32.55280, 35.26401 (code 146).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42326317 ("דבורה (מושב)");
  https://en.wikipedia.org/w/index.php?oldid=1374529407 ("Dvora, Israel"). All checked 2026-10-22.

### 22. Baraq (ברק), moshav

- **Founded 19 August 1956. [S]** Gvati: "בָּרָק", 19.8.56, region "תענך", affiliation Tnuat HaMoshavim. **Year [V]**:
  CBS 1956; Hebrew Wikipedia "יישובי גוש חבר, ובתוכם ברק, הוקמו בשנת 1956"; English Wikipedia "The moshav was founded
  in 1956".
- **Founders. [V]** Immigrants from Morocco (both Wikipedias).
- **Renames.** None found. Named after Barak son of Abinoam (Gvati, Hebrew Wikipedia); English Wikipedia: "Named after
  Barak".
- **Coordinates.** CBS 32.54280, 35.26485 (code 141).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43348675 ("ברק (מושב)");
  https://en.wikipedia.org/w/index.php?oldid=1374528299 ("Barak, Israel"). All checked 2026-10-22.

### 23. Sde Terumot (שדי תרומות), moshav

- **Founded: [C].** CBS: 1951. English Wikipedia: "The village founded in 1951". Hebrew Wikipedia: "בספטמבר 1951,
  הוקמה נקודת יישוב זמנית במקום על אדמת היישוב הערבי הנטוש אלסאמיריה" and, after a footnote, "נקודה זאת הייתה למושב
  רחוב" (a temporary point in September 1951, which became the moshav Rehov, not Sde Terumot; citing HaTzofe, 14 Sep
  1951, not read), and "המושב שדי תרומות הוקם באביב 1952" (the moshav in spring 1952, citing Haaretz, 15 Apr 1952, not read).
  Gvati: 16.9.52, affiliation HaPoel HaMizrachi, region "השרון הדרומי". **I prefer spring 1952 for the moshav**, following
  Hebrew Wikipedia, which gives the September 1951 point to Rehov; CBS and English Wikipedia's 1951 may then reflect
  that point (my reading). Gvati's region "השרון הדרומי"
  (the southern Sharon) does not fit a Beit She'an valley moshav and may be a misprint (my reading); its 16 September
  may be the anniversary of the 1951 point rather than a 1952 date (my inference, not stated anywhere).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Iraq and Kurdistan within HaPoel HaMizrachi. English
  Wikipedia: "Kurdish Jews immigrants from the village of Sandur in Iraq".
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "כפרוונה ג'" (as Farwana C). See row 4 for the English "Farwana B".
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "על אדמת היישוב הערבי הנטוש אלסאמיריה"; English Wikipedia: "on
  land near the former depopulated Palestinian village of Al-Samiriyya". Given as worded.
- **Coordinates.** CBS 32.44050, 35.48588 (code 2057).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42518238 ("שדי תרומות");
  https://en.wikipedia.org/w/index.php?oldid=1374536713 ("Sdei Trumot"). All checked 2026-10-22.

### 24. Yif'at (יפעת), kibbutz on a new site

- **Founded: [C].** CBS: 1926. Gvati: 18.7.54, region "עמק יזרעאל", affiliation HaIhud, section "קיבוצים". Hebrew
  Wikipedia: infobox "תאריך ייסוד= 1954"; cornerstone on 25 April 1952; "ב-15 ביולי 1954 עברו כל החברים והילדים
  מקבוצת השרון וגבת ליפעת" (on 15 July 1954 all members and children of Kvutzat HaSharon and Gevat moved to Yif'at),
  citing the kibbutz site (not read); the name approved on 27 December 1954. English Wikipedia: "The kibbutz was
  established in 1954 by members of Kvutzat HaSharon who previously lived in Ramat David, as well as former residents
  of Gevat". **I prefer 1954 for this locality**; CBS's 1926 is the founding year of the group that moved
  there: Hebrew Wikipedia says "מאז היו ליפעת שני שעוני זמן: האחד ... לפי הקמת "בית ראשון", העלייה להתיישבות בגבת
  ובקבוצת השרון בדצמבר 1926". The day is [C] between 15 July (Hebrew Wikipedia) and 18 July (Gvati).
- **Founders. [V]** Members of Kvutzat HaSharon and of Gevat (Hebrew and English Wikipedia; Gvati's affiliation
  HaIhud fits the Ihud side of the 1951 split, my reading).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "נקרא תחילה הקיבוץ החדש בשם הזמני "איחוד השרון-גבת"".
- **Site note. [S, Wikipedia]** English Wikipedia: "It was located on land near the former depopulated Arab village
  of Al-Mujaydil." Given as worded.
- **Coordinates.** CBS 32.67681, 35.22479 (code 134).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44079115 ("יפעת");
  https://en.wikipedia.org/w/index.php?oldid=1374538263 ("Yifat, Israel"). All checked 2026-10-22.

### 25. Lehavot HaBashan (להבות הבשן), kibbutz: out of the window on my reading

- **Founded: [C].** Gvati: 7.8.52, region "עמק החולה", affiliation HaKibbutz HaArtzi. CBS: 1945. Hebrew Wikipedia:
  "הקיבוץ עלה על הקרקע ביום השנה להצהרת בלפור, ב-2 בנובמבר 1945" (on the land on the Balfour Declaration
  anniversary, 2 November 1945, citing Davar, 4 Nov 1945, not read); its page is in the category "יישובים שהוקמו
  ב-1947". English Wikipedia: "The village was established in 1945". **I prefer 2 November 1945**, so the row is out of
  the 1952-56 window. Gvati gives the same date, 7.8.52, to Lahav in the Negev (row 3 of the Negev note); a copying
  slip is possible, but that is my inference.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: Youth Aliyah immigrants from Germany and Poland and HaShomer HaTzair
  graduates, the "Lehavot" garin founded in 1940.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "נוספה לשם הקיבוץ המילה "הבשן"".
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: the kibbutz moved "למיקומו הנוכחי, על אדמות הכפר הערבי ח'יאם
  אל-וליד". Given as worded.
- **Coordinates.** CBS 33.14149, 35.64734 (code 380).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44054315 ("להבות הבשן");
  https://en.wikipedia.org/w/index.php?oldid=1374884806 ("Lehavot HaBashan"). All checked 2026-10-22.

### 26. Ma'ale Gilboa (מעלה גלבוע), Nahal outpost, kibbutz: out of the window on my reading

- **Founded: [C].** Gvati: 25.1.52, region "הר הגלבוע", affiliation HaPoel HaMizrachi. CBS: 1962. Hebrew Wikipedia:
  "קיבוץ מעלה גלבוע, הוקם כהיאחזות נח"ל ב-29 בנובמבר 1962" (citing Ma'ariv, 2 Dec 1962, not read). English Wikipedia:
  "The kibbutz was founded in 1962 as a Nahal settlement", infobox "founded = 1963". **I prefer 29 November 1962**
  (CBS and both Wikipedias), so the row is out of the window. Whether Gvati's 1952 date marks an earlier attempt on the
  site is **[GAP]**.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: the Nahal called it "מצפור הגלבוע".
- **Site note. [S, Wikipedia]** English Wikipedia: "on land near the former depopulated Palestinian village of
  Khirbat al-Jawfa". Given as worded.
- **Coordinates.** CBS 32.47695, 35.41875 (code 1127).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43417875 ("מעלה גלבוע");
  https://en.wikipedia.org/w/index.php?oldid=1374533439 ("Ma'ale Gilboa"). All checked 2026-10-22.

### 27. Ram-On (רם און), moshav: out of the window on my reading

- **Founded: [C].** English Wikipedia: "Ram-On was founded in 1953 by children of families from moshavim around
  Israel. They originally settled in Nurit on the Gilboa mountain, but in 1961, the community moved". CBS: 1960. Gvati:
  14.5.60, region "עמק יזרעאל", affiliation Tnuat HaMoshavim. Hebrew Wikipedia: the founding garin went up
  temporarily to Nurit "ביוני 1958" (June 1958, citing Davar, not read). English Wikipedia also names the moshav after
  a man "killed in battle near Kerem Shalom in 1957", which a 1953 founding name could not commemorate (my
  inference). **I prefer 1960** (CBS, Gvati), so the row is out of the window.
- **Coordinates.** CBS 32.52690, 35.25971 (code 1069).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43786935 ("רם און");
  https://en.wikipedia.org/w/index.php?oldid=1374536041 ("Ram-On"). All checked 2026-10-22.

### 28. Nof HaGalil (נוף הגליל), development town: out of the window on my reading

- **Founded: [C].** CBS: 1957. Hebrew Wikipedia: "הקמתה של העיר החלה בדצמבר 1956" (building began in December 1956,
  citing Haaretz, 25 Dec 1956, not read); its page is in the category "יישובים שהוקמו ב-1957". English Wikipedia:
  infobox "established_date = 1957", and "The first dwellings were completed in September 1956 and the first residents
  moved in later that year". **I prefer 1957** for the settlement; the September 1956 dwellings and the December 1956
  start of building are given so the owner can see them. Kept as the north's counterpart of Ami'oz in the Negev note.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "שנקראה בתחילה "קריית נצרת", והחל מ-1959 "נצרת עילית"" (first
  Kiryat Natzrat, from 1959 Natzrat Illit). **[C] on the year**: English Wikipedia: "The name Nazareth Illit (Upper
  Nazareth) was adopted in 1958." English Wikipedia also has "then as Kiryat Natzeret". The present name is Nof
  HaGalil (both Wikipedias).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: "העיר נבנתה ברובה על אדמות שהופקעו מערביי הסביבה, ביוזמתו של
  מרדכי שטנר שהיה אחראי על נכסי הנפקדים, דבר שגרם לחיכוכים." English Wikipedia describes the 1954 expropriation of
  1,200 dunams, "relying on a law that permitted expropriations for public purposes", and the Supreme Court case.
  Given as worded.
- **Coordinates.** CBS 32.70860, 35.32337 (code 1061, subdistrict 25).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43945199 ("נוף הגליל");
  https://en.wikipedia.org/w/index.php?oldid=1380255559 ("Nof HaGalil"). All checked 2026-10-22.

## Considered and left out

- **Kiryat Shmona** (קריית שמונה, code 2800): CBS 1950. Hebrew Wikipedia: "בו' בטבת ה'תש"י, 26 בדצמבר 1949, הוקמה
  קריית שמונה כמעברה" (founded as a ma'abara on 26 December 1949). Both dates are before 1952, so it is not a row here.
  It is already signed off for the MVP (#782); note only that CBS gives 1950, not 1949. Sources: CBS;
  https://he.wikipedia.org/w/index.php?oldid=44095568 ("קריית שמונה"). Checked 2026-10-22.
- **Ma'alot** (מעלות-תרשיחא, code 1063): CBS 1957. Hebrew Wikipedia: the names committee chose the name on 9 March
  1952 for the future town; "ידיעה ראשונה בעיתונות על הקמת היישוב היהודי מעלות פורסמה בעיתון דבר ב-3 ביולי 1957"
  (the first press report of the Jewish locality, Davar, 3 July 1957, not read). Out of the window. Jewish settlers in
  Tarshiha from 1949 (Hebrew Wikipedia) are before it. Sources: CBS;
  https://he.wikipedia.org/w/index.php?oldid=43704402 ("מעלות-תרשיחא"). Checked 2026-10-22.
- **Ilaniya** (אילנייה, code 49): CBS 1902. Gvati: "1902, חודשה ב-1.7.53" (renewed 1 July 1953). Hebrew Wikipedia:
  "בשנת 1957 הפכה אילניה למושב". A renewal of a 1902 colony, not a founding; left out. Sources: CBS; Gvati;
  https://he.wikipedia.org/w/index.php?oldid=43952364 ("אילניה"). Checked 2026-10-22.
- **Hemdat Yamim** (חמדת ימים), on Mount Meron: Hebrew Wikipedia: "היישוב הוקם ב-1954 ונקרא "טירת יעל"" (founded 1954
  as "Tirat Yael"), today one family; the page is in the category "יישובים בלתי מוכרים" (unrecognised localities). Not
  in the CBS file, so outside this note's scope. Source: https://he.wikipedia.org/w/index.php?oldid=41157505 ("חמדת
  ימים (נקודת יישוב)"). Checked 2026-10-22.
- **Kibbutz halves from the split of the kibbutz movement.** English Wikipedia puts four northern kibbutzim in its
  1952 and 1953 categories; CBS gives the year of the original kibbutz. They are splits of existing communities, not
  new settlements, so they are not rows. Two of them moved, which a map might want:
  - Ein Harod (Ihud) (code 89) and Ein Harod (Meuhad) (code 82): CBS 1921 for both; Gvati 21.9.21 for both. Hebrew
    Wikipedia (Ihud page): split in 1952, and "בשנת 1954 עברה הקבוצה המזוהה עם מפא"י מעט צפונה" (in 1954 the Mapai
    group moved a little north). English Wikipedia: both "formed in 1952". Sources:
    https://he.wikipedia.org/w/index.php?oldid=43724656 ("עין חרוד איחוד");
    https://he.wikipedia.org/w/index.php?oldid=43725252 ("עין חרוד מאוחד");
    https://en.wikipedia.org/w/index.php?oldid=1374529572 ("Ein Harod (Ihud)");
    https://en.wikipedia.org/w/index.php?oldid=1374529583 ("Ein Harod (Meuhad)").
  - Ashdot Ya'akov Ihud (code 199) and Ashdot Ya'akov Meuhad (code 188): CBS 1933 for both; Gvati 13.10.32 for both.
    Hebrew Wikipedia: the split began in 1951 and ended in 1954, the Meuhad half "צפונית אליו" (north of the
    original site). English Wikipedia: both "established in 1953". Sources:
    https://he.wikipedia.org/w/index.php?oldid=43555474 ("אשדות יעקב איחוד");
    https://he.wikipedia.org/w/index.php?oldid=43751600 ("אשדות יעקב מאוחד");
    https://en.wikipedia.org/w/index.php?oldid=1374528028 ("Ashdot Ya'akov Ihud");
    https://en.wikipedia.org/w/index.php?oldid=1374528038 ("Ashdot Ya'akov Meuhad"). All checked 2026-10-22.

## Gaps and notes for the owner

- **Defunct localities** founded 1952-56 in the north were not searched for systematically (neither CBS nor Gvati
  lists them); Hemdat Yamim is the one that turned up.
- **No newspaper of the time was read**; every newspaper date above is as Wikipedia cites it.
- **Days** confirmed by two sources only for Addirim (Hebrew and English Wikipedia; Gvati agrees on day and month but
  not year) and Midrakh Oz (both Wikipedias give 12 December, Gvati 12 November, so it stays [C]). Every other day is single-source, conflicting or missing; Migdal HaEmek, Hazor HaGelilit and Poriyya
  Illit have no day at all.
- **Gvati year slips.** Three Gvati dates disagree with every other source read by six to ten years (Addirim 1950,
  Lehavot HaBashan 1952, Ma'ale Gilboa 1952). The Negev note found the same kind of disagreement (Sde Tzvi, Ein
  Gedi). For the dated file, Gvati is best used for the day only where CBS agrees on the year.
- **Today's site vs the first camp**: Migdal HaEmek (the original ma'abara was west at Shimron hill), Yif'at (moved
  from Ramat David and Gevat in 1954), Ram-On (from Nurit in 1961),
  Atsmon Segev (re-founded 1979) and Lehavot HaBashan (Hebrew Wikipedia: "בשנת 1947 העתיק את מיקומו מגבעה שכנה למיקומו
  הנוכחי", moved in 1947 from a nearby hill) are known to differ from the first settlement; the CBS point is today's
  site. For the others no source read says the site moved.
- **Hexes and sensitivity flags** (armistice lines, depopulated localities) were not computed here; they belong to the
  paused data-file step. Site notes are recorded where the sources state them (rows 1, 3, 5, 7 to 11, 13, 14, 23 to
  26, and 28), as worded, for the owner's sensitivity review.
