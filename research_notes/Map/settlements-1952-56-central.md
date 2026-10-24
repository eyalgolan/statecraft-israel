# Settlements founded in 1952-1956: the Central District (research note, 2026-10-22)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file and the
tool change (#61) stay paused until after the MVP. Refs #27, its 1952-56 rows, one region. It follows the format of
[`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md) (#485), which covers 1949-1951; those rows are
not repeated here. Sibling notes for other regions: the Negev (#1366), the Ashkelon subdistrict (#1369), the Northern
District (#1380) and the Jerusalem District (#1381).

The question: which Jewish localities were founded in the Central District between 1 January 1952 and 31 December
1956, and for each: founding date, coordinates, type, founding group, renames and sources.

## Which "Central District"

**Boundary used: the Central District (מחוז המרכז, code 4) of the Central Bureau of Statistics**, as given in the CBS
localities file 2024 (columns "סמל מחוז", "שם מחוז", "סמל נפה" and "שם נפה"; source below). District 4 has 281 rows in
that file, in four subdistricts: Sharon (41, "השרון", 95 rows), Petah Tikva (42, "פתח תקווה", 62), Ramla (43, "רמלה",
65) and Rehovot (44, "רחובות", 59). It runs from Hefer Valley and the Netanya area in the north, through the southern
Sharon, the Petah Tikva and Lod-Ramla areas and the Ayalon Valley, down to the coastal plain around Yavne and Gedera.
It leaves out the Tel Aviv District (5), the Haifa District (3, which holds Hadera and Menashe), the Jerusalem District
(1, see #1381) and the Ashkelon subdistrict (61, see #1369). This line was chosen because it is the one the official
source applies to every row. **Other regions of #27 stay open to other claimants, and this region could be split
differently if the owner prefers another line.**

## How it was done

- Every page was fetched on 22 October 2026 with `curl` (User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`) or read through the MediaWiki
  API. No request carried a personal identifier. No web search result was used as a source.
- **The candidate pool** is the union of three lists: (1) every row of the CBS localities file 2024 in district 4 whose
  "שנת ייסוד" (year of founding) is 1952 to 1956: 21 rows; (2) every row of Haim Gvati's list of rural settlements whose
  "תאריך הקמה" (date of founding) falls in 1952-1956 and whose name matches a district-4 row of the CBS file: 15 rows,
  14 of them already in list 1, adding Einat; (3) the Hebrew Wikipedia categories "יישובים שהוקמו ב-1952" to
  "יישובים שהוקמו ב-1956" intersected with "יישובים במחוז המרכז" (articles only). Its five type subcategories (moshavim, kibbutzim, cities, local councils and regional councils of the
  Central District; the regional-councils one has 0 articles) and 93 per-locality subcategories were also swept;
  the 24 pages exclude Mevo'ot Yam, which was found only through a locality subcategory and is already row 22. Three of
  them are regional councils (Gan Raveh, Sdot Dan, Gederot), not localities, and are left out; the other 21 include 16
  already in list 1 and add 5 that the CBS file dates outside the window or does not list (Givat Haim (Ihud), Nehalim,
  Neve Monosson, Ganot Hadar, Yeshivat Kerem B'Yavneh), each kept with **[C]**. That gives **27 rows**.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024 ("קובץ יישובים
  2024"), https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22), columns
  "שנת ייסוד" and "קואורדינטות". As in `settlements-1949-51.md`, the twelve-digit coordinate field is read as an Israel
  Transverse Mercator easting and northing (six digits each) and converted with `pyproj` (EPSG:2039 to EPSG:4326).
  That reading is ours, not stated in the file; the 1949-51 note checked it against Wikipedia for 29 rows (median
  about 40 m). Coordinates below are the CBS values, rounded to five decimals. They are where the locality is
  **today**; where a source says the first camp stood elsewhere, the row says so.
- **Founding dates (day).** Gvati, *100 Years of Settlement*, vol. 2, appendix "רשימת כל הישובים הכפריים היהודים
  בישראל" (list of all Jewish rural settlements in Israel), Ben-Yehuda Project edition, https://benyehuda.org/read/14684
  (checked 2026-10-22). Its columns are type ("סוג התיישבות"), name ("שם הישוב"), region ("אזור גיאוגרפי"), date of
  founding ("תאריך הקמה"), affiliation ("השתייכות ארגונית") and origin of the name ("מקור השם"). The Gvati rows used
  here are in its sections "קיבוצים" (Bahan, Horeshim, Einat, Givat Haim (Ihud)) and "מושבים" (the rest). The column
  does not say whether the date is the day on the land or a later recognition. It is one secondary compilation. It
  lists rural settlements only, so towns, community settlements founded as suburbs and institutions are mostly not in
  it.
- **Wikipedia** (Hebrew and English) was read as wikitext of the current revision, cited by `oldid`. It is a pointer
  and a cross-check: a fact resting on Wikipedia alone is tagged **[S, Wikipedia]**, and where it cites a newspaper or
  book the citation is given as Wikipedia gives it and not counted as read. Quotes from Wikipedia are from the
  wikitext with link, footnote and bold/italic markup removed (a piped link shows its display text); nothing else is
  changed except that Hebrew vowel points are normalised (NFC). Where a sentence is reworded, it says "paraphrase".
  Where a quote stops before the end of the sentence, it ends at a word of the original.
- **Founding group** in Gvati is the affiliation (the settlement movement), not the people. The people (country of
  origin, garin) come from Wikipedia and are tagged as such.
- Hebrew dates were converted with the `convertdate` library.
- **Site notes.** Twelve rows sit, by the Wikipedia articles, on or next to the lands of an Arab village depopulated in
  1948, or on land the articles say was declared absentee property. Each such row has a one-line **Site note** in a
  neutral paraphrase, naming the village where the article does, for the owner's sensitivity review. They are
  recorded, not weighed. Rows without a site note are rows where neither article says so; the articles were searched
  for it, other sources were not.

Tags, one per fact, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. Both values and the sources on each side are given, and which we prefer.
- **[GAP]**: not found in any source read.

CBS year plus Gvati day agreeing on the year counts as [V] for the year; the day stays [S] unless a second source
gives the same day.

### Sources not reached

- **Regional council sites** (Hefer Valley, Drom HaSharon, Lev HaSharon, Gezer, Hevel Modi'in, Gederot, Hevel Yavne,
  Nahal Sorek and Sdot Dan) were not tried. The Wikipedia footnotes that point to them were not followed.
- **Not tried, known blocked** (`settlements-1949-51.md`): the NLI historical press (JPress) and the Israel State
  Archives. So **no contemporaneous newspaper was read for any row**. Hebrew Wikipedia cites papers of the time for
  many rows (Davar, HaTzofe, HaBoker, Haaretz, Al HaMishmar); they are given as cited.
- **Localities founded 1952-56 that no longer exist** are not in the CBS file and not in Gvati. They were not searched
  for beyond the Wikipedia categories above. **[GAP]**
- **District-4 rows with no founding year.** 47 rows of district 4 have an empty "שנת ייסוד". Most are statistical
  areas, industrial zones, army camps and the airport; the others are Arab towns and villages (Jaljulia, Kafr Bara,
  Kafr Qasim, Qalansawe, Taibe, Tira, Zemer) and the institution Tzofiya (code 1111). None was researched further; if
  any of them was founded in 1952-56 it is missing here. **[GAP]**

### Near misses, not rows

- **Givat Haim (Meuhad)** (CBS code 173): CBS 1932, Hebrew Wikipedia category 1932, Gvati 14.7.32; only English
  Wikipedia's category says 1952 ("Populated places in Israel established in 1952",
  https://en.wikipedia.org/w/index.php?oldid=1374530848 ; Hebrew https://he.wikipedia.org/w/index.php?oldid=43696761 ;
  checked 2026-10-22). It is the original kibbutz that stayed after the 1952 split; the new one is row 2.
- **Sdei Trumot**: Gvati gives "שְדֵי-תְּרוּמוֹת", region "השרון הדרומי", 16.9.52, but the CBS file has it in district
  2 (code 2057, subdistrict 23 (Jezreel), founding year 1951), in the Emek HaMa'ayanot Regional Council (CBS). It is outside this boundary and was not
  researched further.
- **CBS district-4 rows just outside the window**, not researched further: 1951, Ahituv, Beit Arif, Elishama, Azriel,
  Ga'ash, Harutzim, Kfar Aviv, Neve Yarak, Nitzanei Oz, Pedaya, Petahya, Savyon, Sha'alvim and Shoshanat HaAmakim;
  1958, Bnei Ayish, Ganei Hadar, Givat Shapira and Kerem Ben Shemen.

## Summary table

Coordinates: CBS 2024, lat N, lon E, today's site. "Founded" is the best-supported date; see the row for the sources.

| # | Name | Type | Founded | Lat, lon (CBS) | Renames |
|---|---|---|---|---|---|
| 1 | Einat / עינת | Kibbutz | 1952 (Gvati 1 May 1952, both Wikipedias) or 1925 (CBS) **[C]** | 32.08285, 34.93755 | First "גבעת השלושה של איחוד הקבוצות והקיבוצים" |
| 2 | Givat Haim (Ihud) / גבעת חיים (איחוד) | Kibbutz | 1951 or 1952 (Hebrew Wikipedia), 1952 (English Wikipedia) or 1932 (CBS, Gvati) **[C]** | 32.39941, 34.93216 | None found |
| 3 | Nehalim / נחלים | Moshav, moved from Upper Galilee | 1948 (CBS; Gvati 9 Sep 1948), 1952 (Hebrew Wikipedia) or 1953 (English Wikipedia) at today's site **[C]** | 32.05907, 34.91230 | First "מצודת אוסישקין ד'" (in Galilee) |
| 4 | Tenuvot / תנובות | Moshav | 10 Sep 1952 **[S]**; 1952 **[V]** | 32.30556, 34.96264 | None found |
| 5 | Kfar Bin Nun / כפר בן נון | Moshav | 11 Sep 1952 **[S]**; 1952 **[V]** | 31.86177, 34.94732 | First "Mishmar Ayalon" A or B **[C]** |
| 6 | Sdei Hemed / שדי חמד | Moshav | 16 Sep 1952 **[S]**; Sep 1952 **[V]** | 32.15951, 34.94319 | None found |
| 7 | Bareket / ברקת | Moshav | 20 Nov 1952 **[S]**; 1952 **[V]** | 32.01612, 34.94445 | First "כפר חוצבים" or "Kfar Halutzim" **[C]**, then "טירת יהודה ב'" |
| 8 | Ben Shemen (moshav) / בן שמן (מושב) | Moshav (new, beside the 1905 moshav) | Dec 1952 **[S, Wikipedia]**; 1952 **[V]** | 31.95266, 34.92285 | None found |
| 9 | Ganot Hadar / גנות הדר | Rural locality | 1952 first houses (Hebrew Wikipedia), 1954 (English Wikipedia) or 1964 (CBS) **[C]** | 32.32067, 34.90057 | None found |
| 10 | Beit Hilkia / בית חלקיה | Moshav | 6 Jan 1953 **[V]** | 31.79229, 34.81166 | None found |
| 11 | Gan HaDarom / גן הדרום | Moshav | Jan 1953 (Gvati; CBS 1953) or 1951 (both Wikipedias) **[C]** | 31.80433, 34.70085 | None; the authorities' proposed "משולם" was rejected (Hebrew Wikipedia) |
| 12 | Bahan / בחן | Nahal outpost (1953), kibbutz | Apr 1953 (CBS 1953; Gvati 23 Apr; Hebrew Wikipedia 15 Apr) or 1954 (English Wikipedia) **[C]** | 32.35209, 35.02005 | None found |
| 13 | Gannot / גנות | Moshav | 1950 (failed), 15 May 1953 (Gvati; CBS 1953) or 26 Oct 1955 (Hebrew Wikipedia) **[C]** | 32.01794, 34.82853 | None found |
| 14 | Sha'ar Efraim / שער אפרים | Moshav | 25 Aug 1953 **[S]**; Aug 1953 **[V]** | 32.28900, 34.99717 | None found |
| 15 | Ganei Tikva / גני תקווה | Immigrant locality, local council 1953 (a town today) | 1949 (both Wikipedias) or 1953 (CBS and Hebrew Wikipedia's 70th anniversary) **[C]** | 32.06012, 34.87503 | First "Shikun Yovel" |
| 16 | Hadassah Neurim / הדסה נעורים | Youth village | 1948 (both Wikipedias) or 1953 (CBS) **[C]** | 32.37407, 34.86204 | CBS name "נעורים" |
| 17 | Neve Monosson / נווה מונוסון | Cooperative society, later local council | 1953 **[S, Wikipedia]**; no own CBS row | 32.01667, 34.86667 (English Wikipedia, minute precision) | Also Neve Efraim Monosson; earlier "Neve Efraim" |
| 18 | Yeshivat Kerem B'Yavneh / ישיבת כרם ביבנה | Yeshiva (institution) | Oct/Nov 1953 (Hebrew Wikipedia), 1954 (English Wikipedia) or 1963 (CBS) **[C]** | 31.81840, 34.72236 | CBS name "כרם ביבנה (ישיבה)" |
| 19 | Nir Zvi / ניר צבי | Moshav | 2 May 1954 **[S]**; 1954 **[V]** | 31.95247, 34.86297 | First "כפר ארגנטינה", renamed 1955 **[S, Wikipedia]** |
| 20 | Aseret / עשרת | Rural centre of Gederot, community settlement | 1954 **[V]**; day **[GAP]** | 31.82464, 34.74726 | Also "מרכז גדרות" |
| 21 | Shedema / שדמה | Moshav | Oct 1954 **[S]**; 1954 **[V]** | 31.83205, 34.74141 | Earlier "יפה ענף", "בשיט 3"; English "Yefe Nof" **[C]** |
| 22 | Mevo'ot Yam / מבואות ים | Youth village (institution) | 1951 (English Wikipedia), 1954 (CBS) or 1955 (Hebrew Wikipedia) **[C]** | 32.40326, 34.86824 | None found |
| 23 | Horeshim / חורשים | Kibbutz | Jan 1955 **[V]**; 6 Jan (Gvati) or 13 Jan (Hebrew Wikipedia) **[C]** | 32.13765, 34.97109 | First "החורשים" |
| 24 | Hofit / חופית | Community settlement | 1952 first families; founded 1953 (Hebrew Wikipedia); recognised 1955 **[C]** | 32.38499, 34.87412 | None found |
| 25 | Yad Rambam / יד רמב"ם | Moshav | 30 Oct 1955 (Gvati, English Wikipedia) or 6 Nov 1955 (Hebrew Wikipedia) **[C]**; 1955 **[V]** | 31.90013, 34.90066 | None found |
| 26 | Beit Uziel / בית עוזיאל | Moshav | 14 Oct 1956 **[V]** | 31.87052, 34.90523 | None found |
| 27 | Tzukei Yam / צוקי ים | Community settlement | 1956 (CBS, English Wikipedia) or 1968 (Hebrew Wikipedia) **[C]** | 32.36133, 34.85892 | None found |

Rows: 27. By type: 14 moshavim (Nehalim, Tenuvot, Kfar Bin Nun, Sdei Hemed, Bareket, Ben Shemen, Beit Hilkia, Gan
HaDarom, Gannot, Sha'ar Efraim, Nir Zvi, Shedema, Yad Rambam, Beit Uziel), 4 kibbutzim (Einat, Givat Haim (Ihud),
Bahan, Horeshim), 5 other civil localities (Ganot Hadar, Neve Monosson, Aseret, Hofit, Tzukei Yam), 1 town (Ganei
Tikva, founded 1949 by the Wikipedias), 3 institutions (Hadassah Neurim, Yeshivat Kerem B'Yavneh, Mevo'ot Yam). By the
best-supported date, 2 rows fall before the window (Ganei Tikva, Hadassah Neurim) and 5 are open between a date inside
it and one outside (Nehalim, Ganot Hadar, Gannot, Mevo'ot Yam, Tzukei Yam; Gan HaDarom and Yeshivat Kerem B'Yavneh also conflict but lean
inside the window); they are kept so the owner can see the disagreement. Rows are ordered roughly by date.

## Rows

Common sources, cited by short name in every row:
- **CBS**: https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-22).
- **Gvati**: https://benyehuda.org/read/14684 , appendix table (checked 2026-10-22).

### 1. Einat (עינת), kibbutz

- **Founded: [C].** CBS: 1925 (row "עינת", code 871). Gvati, section "קיבוצים": "עֵינַת", region "השרון הדרומי",
  1.5.52, affiliation "האיחוד". Hebrew Wikipedia: "הקיבוץ הוקם בשנת 1952 על ידי חברי הקיבוצים גבעת השלושה ורמת
  הכובש, שפרשו מהם בעקבות הפילוג בקיבוץ המאוחד", and "בתחילת אפריל 1952 הונחה אבן הפינה לקיבוץ החדש" (cornerstone
  early April 1952). English Wikipedia: "The kibbutz was founded in 1952 by residents of Givat HaShlosha and Ramat
  HaKovesh who had left the HaKibbutz HaMeuhad after its ideological split." Hebrew Wikipedia's category says 1925,
  English Wikipedia's 1952. **We prefer 1952** (Gvati and both article texts). The CBS 1925 matches the founding year
  of Givat HaShlosha, which Gvati dates 1.5.25, so the CBS may carry the parent kibbutz's year; that is our reading,
  the file does not say. Gvati's day for Einat (1.5.52) and for Givat HaShlosha (1.5.25) differ only by the order of
  the year digits; whether that is a coincidence or a misprint is not known (our observation, no source says so).
  **Day [S]** (Gvati only).
- **Founders. [S, Wikipedia]** Members who left Givat HaShlosha and Ramat HaKovesh in the split of HaKibbutz HaMeuhad
  (quotes above).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "בתחילה נקרא הקיבוץ גבעת השלושה של איחוד הקבוצות והקיבוצים" (first
  called Givat HaShlosha of the Ihud), or "גבעת השלושה-רמת הכובש". Name origin, Gvati: "משום קירבתו למקורות הירקון".
- **Coordinates.** CBS 32.08285, 34.93755 (code 871).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43774561 ;
  https://en.wikipedia.org/w/index.php?oldid=1374529751 . All checked 2026-10-22.

### 2. Givat Haim (Ihud) (גבעת חיים (איחוד)), kibbutz

- **Founded: [C].** CBS: 1932 (row "גבעת חיים (איחוד)", code 2018). Gvati, section "קיבוצים": "גִבְעַת-חַיִים
  (איחוד)", region "עמק חפר", 14.7.32, affiliation "האיחוד", the same date as its row for Givat Haim (Meuhad). Hebrew
  Wikipedia: "הקיבוץ הוקם בשנת 1952 בעקבות הפילוג שחל בתנועת הקיבוץ המאוחד" (quote ends before the years of the
  split), but the same revision also says "בשנת 1951 עזבה קבוצת חברי מפא"י את הקיבוץ והקימה מעבר לכביש קיבוץ חדש".
  English Wikipedia: "It was formed in 1952 by an ideological split in kibbutz Givat Haim (founded 1932)".
  **We prefer 1952 for this kibbutz as a separate settlement** (both Wikipedias); CBS and Gvati give the 1932 founding
  of the undivided kibbutz (our reading). Day **[GAP]**. Hebrew Wikipedia has a photograph captioned "טקס הנחת אבן
  הפינה ב-1952" (cornerstone ceremony in 1952).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: members who left the old kibbutz (Mapai members and non-party members)
  and Mapai members from Kfar Szold (paraphrase). English Wikipedia: Mapai-supporting members.
- **Site. [S, Wikipedia]** Hebrew Wikipedia: "קיבוץ גבעת חיים איחוד נמצא צפונית לקיבוץ גבעת חיים מאוחד" (north of
  the old kibbutz). The CBS points of the two kibbutzim are about 900 m apart (our computation from the CBS
  coordinates).
- **Renames.** None found.
- **Coordinates.** CBS 32.39941, 34.93216 (code 2018).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44069165 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530839 . All checked 2026-10-22.

### 3. Nehalim (נחלים), moshav, moved from Upper Galilee

- **Founded: [C], at today's site.** CBS: 1948 (code 449). Gvati: "נְחָלִים", region "שפלת יהודה", 9.9.48,
  affiliation "הפועל המזרחי", name origin "ע"ש מושב נחלים בעמק החולה שחבריו עקרו משם והקימו את הישוב בדרום". Hebrew
  Wikipedia: "המושב נחלים הוקם מחדש בשנת 1952 עם שמונים ותשע נחלות של 20 דונם כל אחת." English Wikipedia: "The new
  moshav, in its present location south of Petah Tikva, was founded in 1953". **No preference**: Gvati's region
  "שפלת יהודה" with a 1948 date suggests the move south was dated 1948, and the Wikipedias may date the permanent
  moshav, but no source says so; that is our reading.
- **Earlier site. [S, Wikipedia]** Hebrew Wikipedia: "באוקטובר 1943 עלו אנשי ארגון ירושלים ליישוב", in the eastern
  Galilee panhandle; after the 1948 war the settlers were offered empty houses in the former German Templer colony of
  Wilhelma near Yehud, and moved on to the present site near Petah Tikva (paraphrase). English Wikipedia: moved in 1944
  to land south-east of today's HaGoshrim, and after the war to Wilhelma (paraphrase).
- **Founders. [S, Wikipedia]** Members of "ארגון ירושלים" (Hebrew Wikipedia; English Wikipedia: a religious Bnei
  Akiva group from Jerusalem); at the present site
  joined by refugees from Neve Ya'akov and by Holocaust survivors who immigrated in the mass immigration years
  (paraphrase).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "היישוב נקרא בתחילה "מצודת אוסישקין ד'"" (at the Galilee site), then
  Nehalim.
- **Coordinates.** CBS 32.05907, 34.91230 (code 449), today's site.
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44066589 ;
  https://en.wikipedia.org/w/index.php?oldid=1374534869 . All checked 2026-10-22.

### 4. Tenuvot (תנובות), moshav

- **Founded 10 September 1952. [S]** Gvati: "תְּנוּבוֹת", region "השרון הדרומי", 10.9.52, affiliation "תנועת
  המושבים". **Year [V]**: CBS 1952 (code 2002); Hebrew Wikipedia: "תנובות הוקם בשנת 1952 על ידי עולים מתימן שהגיעו
  ממעברת עמק חפר, מקריית שמונה ומתרביחה, וכלל 76 משקים"; English Wikipedia: "The village was founded in 1952, on land
  near Qalansawe, by Jewish immigrants from Yemen."
- **Founders. [S, Wikipedia]** Immigrants from Yemen (both Wikipedias), who came from the Hefer Valley ma'abara, Kiryat
  Shmona and Tarbikha (Hebrew Wikipedia, quote above).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: at the end of 1955 a dispute arose between the moshav and residents
  of Qalansawe over land declared absentee property and given to the moshav (paraphrase; citing Al HaMishmar, Dec
  1955, not read). For the owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: "סמלי".
- **Coordinates.** CBS 32.30556, 34.96264 (code 2002).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43328931 ;
  https://en.wikipedia.org/w/index.php?oldid=1374537620 . All checked 2026-10-22.

### 5. Kfar Bin Nun (כפר בן נון), moshav

- **Founded 11 September 1952. [S]** Gvati: "כְּפַר בִּן-נוּן", region "שפלת יהודה", 11.9.52, affiliation "האיחוד
  החקלאי". **Year [V]**: CBS 1952 (code 2010); Hebrew Wikipedia: "המושב נוסד בשנת 1952 על ידי תנועת העובד הציוני"
  (citing HaTzofe and HaBoker, 17 Sep 1952, not read); English Wikipedia: "The moshav was founded in 1952 by the
  Agricultural Union".
- **Affiliation. [C]** Gvati and English Wikipedia: HaIhud HaHaklai (the Agricultural Union); Hebrew Wikipedia: HaOved
  HaTzioni. One of the papers Hebrew Wikipedia cites is titled, as it gives it, "מושב עובדים של העובד הציוני ליד
  אל־קובאב" (not read).
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "עולים רומנים", other East European immigrants and a few families from
  North Africa, mainly Tunisia (paraphrase after the quoted words).
- **Site note. [S, Wikipedia]** Both Wikipedias: on or near the lands of the Arab village of al-Qubab, whose people
  fled in May 1948 and were not allowed back (Hebrew Wikipedia; English Wikipedia: depopulated) (paraphrase). For the
  owner's sensitivity review.
- **Renames. [C]** Hebrew Wikipedia: "הכפר נקרא בתחילה "משמר אילון א'"" (first called Mishmar Ayalon A); English
  Wikipedia: "It was initially  named Mishmar Ayalon Bet" (B; two spaces as in the wikitext). Then Kfar Bin Nun, after
  Operation Bin Nun B (both name the operation; 1948 is Hebrew Wikipedia). No date for the change.
- **Coordinates.** CBS 31.86177, 34.94732 (code 2010).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43688521 ;
  https://en.wikipedia.org/w/index.php?oldid=1374532363 . All checked 2026-10-22.

### 6. Sdei Hemed (שדי חמד), moshav

- **Founded 16 September 1952. [S]** Gvati: "שְדֵי-חֶמֶד", region "השרון הדרומי", 16.9.52, affiliation "תנועת
  המושבים". **Month [V]**: Hebrew Wikipedia: "היישוב הוקם בספטמבר 1952 על ידי פועלים ותיקים מפתח תקווה שעברו
  להתיישבות במסגרת תנועת מן העיר אל הכפר, והיה ראשון יישובי התנועה." CBS 1952 (code 2015); English Wikipedia: "The
  moshav was founded in 1952 as part of the 'From the city to the village' project."
- **Founders. [S, Wikipedia]** Veteran workers from Petah Tikva in the "From the city to the village" movement, its
  first settlement (Hebrew Wikipedia, quote above); half Ashkenazi and half Sephardi, with residents of Tel Aviv and
  Jerusalem too (Hebrew Wikipedia, paraphrase).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: each family's land included 12 dunams of orchard held by the
  Custodian of Absentee Property (paraphrase; citing Davar, 6 Nov 1953, not read). For the owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: Isaiah 32:12.
- **Coordinates.** CBS 32.15951, 34.94319 (code 2015).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42333165 ;
  https://en.wikipedia.org/w/index.php?oldid=1374536704 . All checked 2026-10-22.

### 7. Bareket (ברקת), moshav

- **Founded 20 November 1952. [S]** Gvati: "בַּרֶקֶת", region "השרון הצפוני", 20.11.52, affiliation "הפועל המזרחי".
  **Year [V]**: CBS 1952 (code 2038); Hebrew Wikipedia: "המושב נוסד בשנת 1952 בידי חברי "הפועל המזרחי" יוצאי העיר
  חבאן, שבדרום-מזרח תימן."; English Wikipedia: "The village of Bareket was established in 1952 by members of Hapoel
  HaMizrachi who had immigrated from Habban District in south-east Yemen." Gvati's region "השרון הצפוני" does not fit
  the site near Lod (our observation).
- **Founders. [S, Wikipedia]** Hapoel HaMizrachi members from Habban in Yemen (both Wikipedias).
- **Site note. [S, Wikipedia]** English Wikipedia: on the site of the Arab village of al-Tira, depopulated on 10 July
  1948, built on its ruins (paraphrase). In its words: "However, the village was depopulated on 10 July 1948 after a
  military assault by the Israeli army."; "On the same day, Operation Danny headquarters ordered the Yiftach Brigade
  to blow up most of Innaba and al-Tira". Hebrew Wikipedia does not say so. For the owner's sensitivity review.
- **Renames. [C], first name.** Hebrew Wikipedia: "תחילה נקרא המושב "כפר חוצבים" וכן "טירת יהודה ב'"" and "אך
  בשנת תשי"ג נבחר השם "ברקת"" (Bareket chosen in 5713, 1952-53). English Wikipedia: "It was initially named Kfar
  Halutzim (Pioneers' Village) and then Tirat Yehuda Bet". So the first name is Kfar Hotzvim or Kfar Halutzim.
- **Coordinates.** CBS 32.01612, 34.94445 (code 2038).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43906648 ;
  https://en.wikipedia.org/w/index.php?oldid=1374528313 . All checked 2026-10-22.

### 8. Ben Shemen (בן שמן (מושב)), moshav

- **Founded 1952 as a new moshav. [V]** CBS: 1952 (row "בן שמן (מושב)", code 2013). Hebrew Wikipedia: "המושב נוסד
  בדצמבר 1952 על ידי עולים מרומניה, מדרום לכפר הנוער בן שמן, על אדמות שהיו בעבר חלק משטחי החווה החקלאית במקום,
  ומשטחי מושב בן שמן הוותיק, שהפך לימים ליישוב כרם בן שמן" (citing Haaretz, 4 Dec 1952, not read). **Month December
  [S, Wikipedia].**
- **Framing. [C]** English Wikipedia treats Ben Shemen as one moshav: "The moshav was founded in 1905", and
  "Immigrants from Romania joined the moshav in 1952." Gvati has only the old moshav: "בֶּן-שֶמֶן", region "שפלת
  לוד", 11.9.22. The CBS file has Kerem Ben Shemen (code 88, 1958) as a separate row, and
  the youth village as another (code 1084, 1921). Hebrew Wikipedia says the old moshav "later became" Kerem Ben Shemen (paraphrase of "שהפך לימים");
  English Wikipedia says that in 1923 the moshav was split in two and part became Kerem Ben Shemen (paraphrase); the
  framings differ. **We follow CBS and Hebrew Wikipedia**: a new moshav in 1952.
- **Founders. [S, Wikipedia]** Immigrants from Romania (both Wikipedias).
- **Renames.** None found.
- **Coordinates.** CBS 31.95266, 34.92285 (code 2013).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43088381 ;
  https://en.wikipedia.org/w/index.php?oldid=1374528907 . All checked 2026-10-22.

### 9. Ganot Hadar (גנות הדר), rural locality

- **Founded: [C].** CBS: 1964 (code 549). Hebrew Wikipedia: "עשרים בתים ראשונים נבנו בשנת 1952." and "בשנת 1953
  שווקו הבתים בעיתונות הישראלית" (houses advertised in 1953); its category says 1954. English Wikipedia: "The village
  was founded in 1954 and was designated for immigrants from South Africa." Not in Gvati. **No preference**: 1952 is
  the first building, 1954 has no event attached, and the CBS 1964 is unexplained by any source read.
- **Type. [S, Wikipedia]** Hebrew Wikipedia: "יישוב כפרי"; farms set up by Yachin-Hakal, houses built by Yekutiel
  Federmann and Israel-Miami (paraphrase).
- **Founders. [C]** English Wikipedia: planned for immigrants from South Africa. Hebrew Wikipedia: "היישוב יועד
  בתחילת דרכו לעולים חדשים מאמריקה ואירופה, אך הגיעו רק שתי משפחות מאמריקה ... כן הגיעו ליישוב מספר משפחות מצפון
  אפריקה". No preference. Hebrew Wikipedia says 17 houses were sold by 1958.
- **Renames.** None found.
- **Coordinates.** CBS 32.32067, 34.90057 (code 549).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43801235 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530375 . All checked 2026-10-22.

### 10. Beit Hilkia (בית חלקיה), moshav

- **Founded 6 January 1953. [V]** Gvati: "בֵּית-חִלְקִיָה", region "דרום", 6.1.53, affiliation "פועלי אגודת
  ישראל". Hebrew Wikipedia: "היישוב הוקם ביום י"ט טבת בשנת ה'תשי"ג, (6 בינואר 1953) על ידי עולים מהונגריה
  ורומניה."; 19 Tevet 5713 is 6 January 1953 (`convertdate`). CBS 1953 (code 2033); English Wikipedia 1953. The two
  day sources were not checked for independence.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: immigrants from Hungary and Romania (quote above), of whom "חלקם
  התגוררו באופן זמני במעברות וחלקם מירושלים". English Wikipedia agrees they were "former residents of Jerusalem and
  ma'abarot who wanted to combine a Haredi and agricultural lifestyle".
- **Site note. [S, Wikipedia]** English Wikipedia: on land near the depopulated Arab village of al-Mukhayzin
  (paraphrase). For the owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: "סמלי. שם אביו של ירמיהו הנביא".
- **Coordinates.** CBS 31.79229, 34.81166 (code 2033).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43002119 ;
  https://en.wikipedia.org/w/index.php?oldid=1374528681 . All checked 2026-10-22.

### 11. Gan HaDarom (גן הדרום), moshav

- **Founded: [C].** CBS: 1953 (code 1072). Gvati: "גַן-הַדָרוֹם", region "דרום", "1.53" (January 1953), affiliation
  "האיחוד החקלאי". Hebrew Wikipedia: "המושב הוקם בשנת 1951 על ידי עולים מעיראק שהגיעו לישראל במסגרת מבצע עזרא
  ונחמיה." English Wikipedia: "The moshav was founded in 1951 by Jewish refugees from Iraq on Operation Ezra and
  Nehemiah." (its category says 1953; the Hebrew category says 1951). **We prefer January 1953** (CBS and Gvati, the
  two non-wiki sources), which puts it in the window; kept with the conflict.
- **Founders. [S, Wikipedia]** Immigrants from Iraq (both Wikipedias); 15 families from Poland joined in 1957-58
  (paraphrase).
- **Site note. [S, Wikipedia]** English Wikipedia: on land near the Arab village of Isdud, depopulated in 1948
  (paraphrase). For the owner's sensitivity review.
- **Renames. [S, Wikipedia]** No rename. Hebrew Wikipedia: representatives of the authorities proposed the name "משולם",
  the settlers objected and proposed the present one (paraphrase). Name origin, Gvati: "סמלי".
- **Coordinates.** CBS 31.80433, 34.70085 (code 1072).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43685494 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530183 . All checked 2026-10-22.

### 12. Bahan (בחן), Nahal outpost, then kibbutz

- **Founded: [C].** CBS: 1953 (code 2043). Gvati, section "קיבוצים": "בַּחַן", region "השרון הצפוני", 23.4.53,
  affiliation "האיחוד". Hebrew Wikipedia: "הקיבוץ הוקם כהיאחזות הנח"ל השישית, נחלאים ו', ב־15 באפריל 1953, על שטח של
  כ-4,000 דונם" (founded as the sixth Nahal outpost on 15 April 1953); the permanent garin settled in July 1954 and the
  civilianisation ceremony was on 10 October 1954 (paraphrase). English Wikipedia: "The kibbutz was established in 1954
  by a Nahal gar'in group made up of immigrants from South America." Both categories say 1954. **We prefer April 1953
  for the first settlement** (CBS, Gvati, Hebrew Wikipedia text); 1954 is the permanent kibbutz. **Day [C]**: 23 April
  (Gvati) or 15 April (Hebrew Wikipedia).
- **Founders. [S, Wikipedia]** A garin of the "Lamerhav" movement from Argentina and Chile (Hebrew Wikipedia); English
  Wikipedia: immigrants from South America.
- **Site note. [S, Wikipedia]** English Wikipedia: "Nearby is an archaeological site named Tel Bahan and ruins of the
  depopulated Palestinian village of Qaqun." Hebrew Wikipedia: the site was known as "משלט 82", a 1948 position. For
  the owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: symbolic, meaning a watchtower (paraphrase).
- **Coordinates.** CBS 32.35209, 35.02005 (code 2043).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43761649 ;
  https://en.wikipedia.org/w/index.php?oldid=1374528255 . All checked 2026-10-22.

### 13. Gannot (גנות), moshav

- **Founded: [C].** CBS: 1953 (code 836). Gvati: "גַנּוֹת", region "השרון הדרומי", 15.5.53, affiliation "האיחוד
  החקלאי". Hebrew Wikipedia: "היישוב הוקם לראשונה בשנת 1950 על ידי קבוצת חיילים משוחררים, אך ניסיון התיישבות זה
  נכשל." and "היישוב הוקם רשמית ב-י' בחשון תשט"ז, 1955 על ידי חברת רסקו, שקלטה ליישוב תושבים מכל רחבי הארץ"; 10
  Heshvan 5716 is 26 October 1955 (`convertdate`). English Wikipedia: "The village was first established in 1950 by
  demobilised soldiers, but was later abandoned." and "It was re-established in 1955 by Rassco". Hebrew Wikipedia's
  category says 1953, English Wikipedia's 1950. **No preference**: the May 1953 date of Gvati and CBS falls between the
  failed 1950 attempt and the 1955 refounding, and no source read explains it.
- **Founders. [S, Wikipedia]** Demobilised soldiers in 1950; Rassco settlers from all over the country in 1955 (both
  Wikipedias).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: on the land of al-Yahudiya (al-Abbasiya) (paraphrase). For the
  owner's sensitivity review.
- **Renames.** None found. Name origin, Gvati: Jeremiah 29:5.
- **Coordinates.** CBS 32.01794, 34.82853 (code 836).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44005624 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530361 . All checked 2026-10-22.

### 14. Sha'ar Efraim (שער אפרים), moshav

- **Founded 25 August 1953. [S]** Gvati: "שַׁעַר אֶפְרַיִם", region "השרון הצפוני", 25.8.53, affiliation "תנועת
  המושבים". **Month [V]**: Hebrew Wikipedia: "המושב יושב בסוף אוגוסט 1953 על ידי משפחות מעולי תימן שהגיעו מקריית
  שמונה." (citing Davar, 28 Aug 1953, not read). CBS 1953 (code 661); English Wikipedia: "The village was founded in
  1953".
- **Founders. [S, Wikipedia]** Families of Yemenite immigrants from Kiryat Shmona (Hebrew Wikipedia).
- **Renames.** None found. Name origin, Gvati: "משום מיקומו הגיאוגרפי המשקיף על הרי אפרים".
- **Coordinates.** CBS 32.28900, 34.99717 (code 661). Hebrew Wikipedia: on the Green Line (paraphrase).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43775042 ;
  https://en.wikipedia.org/w/index.php?oldid=1377958106 . All checked 2026-10-22.

### 15. Ganei Tikva (גני תקווה), immigrant locality, local council 1953

- **Founded: [C].** CBS: 1953 (code 229). Hebrew Wikipedia: "נוסדה כיישוב עולים בשנת 1949 והוכרזה כמועצה מקומית בשנת
  1953." English Wikipedia: "Ganei Tikva was formed in 1949" and "Ganei Tikva achieved local council status in 1953."
  Hebrew Wikipedia also says it became a city on 1 May 2023 "במלאת 70 שנה להקמתה", which dates the founding to 1953.
  **We prefer 1949**, which puts it outside the window; the CBS 1953 matches the local council (our reading, the file
  does not say). Hebrew Wikipedia also says "בשנת 1954, עם הקמת המועצה המקומית", so it gives 1953 and 1954 for the
  council. Not in Gvati.
- **Type. [S, Wikipedia]** Founded by the Jewish Agency as an immigrant locality on the edge of Petah Tikva, next to
  Moshav HaYovel (today Kfar Ma'as) (Hebrew Wikipedia, paraphrase). It is the only town-type row in this note.
- **Site note. [S, Wikipedia]** English Wikipedia: on land near the Arab village of al-'Abbasiyya, depopulated in 1948
  (paraphrase). Hebrew Wikipedia: on land partly of the JNF and partly private (paraphrase). For the owner's
  sensitivity review.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "היישוב הוכר בשם שיכון היובל" and "שנתיים לאחר מכן שונה שם היישוב
  לגני תקווה". English Wikipedia calls it "Shikun Yovel, built in 1949".
- **Coordinates.** CBS 32.06012, 34.87503 (code 229).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44092580 ;
  https://en.wikipedia.org/w/index.php?oldid=1380318762 . All checked 2026-10-22.

### 16. Hadassah Neurim (הדסה נעורים), youth village

- **Founded: [C].** CBS: 1953 (row "נעורים", code 186). Hebrew Wikipedia: "הכפר נוסד בשנת 1948 במטרה למצוא מקום
  תחליפי לילדים שפונו מכפר הנוער בן שמן במלחמת העצמאות". English Wikipedia: "The village was established in 1948."
  **We prefer 1948** (both Wikipedias), which puts it outside the window; the CBS 1953 is unexplained by any source
  read. Not in Gvati.
- **Type.** A youth village, an institution, not a residential settlement (both Wikipedias).
- **Founders. [S, Wikipedia]** A couple from the Ben Shemen youth village staff, who took over the
  British police building (Hebrew Wikipedia, paraphrase).
- **Renames.** None found; the CBS name is "נעורים".
- **Coordinates.** CBS 32.37407, 34.86204 (code 186).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43898019 ;
  https://en.wikipedia.org/w/index.php?oldid=1374531221 . All checked 2026-10-22.

### 17. Neve Monosson (נווה מונוסון), cooperative society, later local council

- **Founded 1953. [S, Wikipedia]** Hebrew Wikipedia: "היישוב נווה מונוסון, נוסד בשנת 1953, כאגודה שיתופית במסגרת
  מועצה אזורית.", "אבן הפינה של היישוב הונחה בדצמבר 1953" (cornerstone December 1953) and "המשפחות הראשונות נכנסו
  לבתיהן בתחילת 1955." (first families early 1955). English Wikipedia: "Neve Monosson was founded in 1953 by a group of
  families supported by Fred (Efraim) Monosson". The two articles may copy each other. **No own CBS row**: since 2003
  it is part of Yehud-Monosson (CBS code 9400, founding year 1948), so the CBS gives it no year (our reading). Not in Gvati.
- **Founders. [S, Wikipedia]** Fred (Efraim) Monosson, a Zionist donor from Boston, for workers of the Lod airport
  (Hebrew Wikipedia, paraphrase).
- **Site note. [S, Wikipedia]** English Wikipedia: on the land of the depopulated Arab village of Kafr 'Ana, "on or
  very near" the village site (paraphrase around the quoted words). For the owner's sensitivity review.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: also known as Neve Efraim Monosson (paraphrase; the lead gives it
  with vowel points). Earlier Neve Efraim: Hebrew Wikipedia "לאחר שהוחלף לבקשת התושבים מנוה אפרים"; English
  Wikipedia "also known as Neve Efraim".
- **Coordinates. [S, Wikipedia]** English Wikipedia infobox: 32°01′N 34°52′E, read as 32.01667, 34.86667 (rounded
  to the minute, so up to about 1 km off). The CBS point of Yehud-Monosson (32.03320, 34.88639) is the merged town, not this
  neighbourhood.
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=42387945 ;
  https://en.wikipedia.org/w/index.php?oldid=1376926180 . All checked 2026-10-22.

### 18. Yeshivat Kerem B'Yavneh (ישיבת כרם ביבנה), yeshiva

- **Founded: [C].** CBS: 1963 (row "כרם ביבנה (ישיבה)", code 1094). Hebrew Wikipedia: infobox "תאריך ייסוד" 1953
  (ה'תשי"ג), and "בחודש מרחשוון ה'תשי"ד (1953) פתח את הישיבה" (Rabbi Goldwicht opened the yeshiva in Heshvan 5714,
  which ran from 10 October to 7 November 1953, `convertdate`). English Wikipedia: "Founded in 1954, Kerem BeYavneh was
  the first Yeshivat Hesder." Both categories say 1954. **We prefer October/November 1953**, the one dated
  statement; the CBS 1963 is unexplained by any source read. Not in Gvati.
- **Type.** A yeshiva, an institution, not a residential settlement (both Wikipedias); next to Kvutzat Yavne. It is a
  CBS locality, but whether the map wants it is the owner's call.
- **Founders. [S, Wikipedia]** A group of 13 graduates of the Kfar HaRoeh yeshiva, organised by Haim Drukman and
  Ya'akov Drori (Hebrew Wikipedia, paraphrase).
- **Renames.** None found.
- **Coordinates.** CBS 31.81840, 34.72236 (code 1094).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=44072924 ;
  https://en.wikipedia.org/w/index.php?oldid=1377031721 . All checked 2026-10-22.

### 19. Nir Zvi (ניר צבי), moshav

- **Founded 2 May 1954. [S]** Gvati: "נִיר צְבִי", region "שפלת יהודה", 2.5.54, affiliation "האיחוד החקלאי". **Year
  [V]**: CBS 1954 (code 331); English Wikipedia: "The village was founded in 1954 by immigrants from Argentina and was
  initially called Kfar Argentina (Argentina Village)." Hebrew Wikipedia gives three steps: "ב-25 בספטמבר 1952 נערך טקס
  הנחת אבן הפינה" (cornerstone 25 September 1952), "ראשוני המתיישבים בכפר עלו לישראל בדצמבר 1953 והתיישבו בכפר בתחילת
  1954." and "הכפר נחנך בתחילת 1955 כשנה לאחר הקמתו."
- **Founders. [S, Wikipedia]** Middle-class Jews from Argentina, settled by Rassco (Hebrew Wikipedia, paraphrase).
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "היישוב נקרא בהתחלה כפר ארגנטינה, ובשנת 1955 השתנה שמו לניר צבי"; English
  Wikipedia: first Kfar Argentina. Named after Baron Maurice (Zvi) de Hirsch (Gvati and both Wikipedias). The two
  Wikipedias were not checked for independence.
- **Coordinates.** CBS 31.95247, 34.86297 (code 331).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43907248 ;
  https://en.wikipedia.org/w/index.php?oldid=1374535450 . All checked 2026-10-22.

### 20. Aseret (עשרת), rural centre and community settlement

- **Founded 1954. [V]** CBS: 1954 (code 591). Hebrew Wikipedia: "היישוב הוקם ב-1954 כמרכז כפרי ליישובי המועצה
  האזורית גדרות, ומתפקד כך גם כיום." English Wikipedia: "Aseret was founded in 1954 as the municipal center of
  Gederot Regional Council." Day **[GAP]**. Not in Gvati.
- **Type. [S, Wikipedia]** A community settlement and the service centre of Gederot Regional Council (Hebrew
  Wikipedia).
- **Site note. [S, Wikipedia]** Hebrew Wikipedia: built where the houses of the Arab village of Bashshit stood, whose
  people fled in Operation Barak and were not allowed back; their land was later declared absentee property
  (paraphrase). English Wikipedia: on lands close to Bashshit, depopulated in 1948 (paraphrase). For the owner's
  sensitivity review.
- **Renames. [S, Wikipedia]** Hebrew Wikipedia: "והיה נקרא גם "מרכז גדרות"" (also called Gederot Centre), and the
  place was at first also known by the village's name. Named after the ten Bilu members who founded Gedera (both
  Wikipedias).
- **Coordinates.** CBS 31.82464, 34.74726 (code 591).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=42989437 ;
  https://en.wikipedia.org/w/index.php?oldid=1374527996 . All checked 2026-10-22.

### 21. Shedema (שדמה), moshav

- **Founded October 1954. [S]** Gvati: "שְׂדֵמָה", region "מישור החוף", "10.54", affiliation "האיחוד החקלאי".
  **Year [V]**: CBS 1954 (code 555); Hebrew Wikipedia: "היישוב הוקם בשנת 1954 לקליטת עולים מאיראן."; English
  Wikipedia: "The moshav was founded in 1954 to absorb immigrants to Israel from Iran".
- **Founders. [S, Wikipedia]** Planned for immigrants from Iran (both); English Wikipedia: that group refused to live
  there, and urban residents of a "from city to village" movement settled instead (paraphrase).
- **Site note. [S, Wikipedia]** English Wikipedia: on lands near the depopulated Arab village of Bashshit
  (paraphrase), as for Aseret (row 20). Hebrew Wikipedia's earlier name "בשיט 3" also ties the moshav to Bashshit.
  For the owner's sensitivity review.
- **Renames. [C]** Hebrew Wikipedia: "שמו הראשון של המושב היה "יפה ענף". עם זאת, קיימים מקורות שמייחסים לו את
  השם "יפה נוף" או "שדה נוף"." (footnotes removed). Its footnote quoting *Herut*, 16 August 1955, gives "שמות קודמים: יפה ענף, בשיט 3".
  English Wikipedia: "The original name was "Yefe Nof"". The two articles disagree on the first name; no date for the
  change. Name origin, Gvati: "סמלי".
- **Coordinates.** CBS 31.83205, 34.74141 (code 555).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=42989473 ;
  https://en.wikipedia.org/w/index.php?oldid=1374536978 . All checked 2026-10-22.

### 22. Mevo'ot Yam (מבואות ים), youth village

- **Founded: [C].** CBS: 1954 (code 1318). Hebrew Wikipedia: "כפר הנוער "מבואות ים" הוקם בשנת תשט"ו 1955"; the
  Hebrew year 5715 ran from 28 September 1954 to 16 September 1955 (`convertdate`), so it fits 1954 or 1955 (our
  observation); its category says 1954. English Wikipedia: "The village was established in 1951", and its category
  says 1951. **No preference** between 1954 and 1955; 1951 is given with no event. Not in Gvati.
- **Type.** A youth village and nautical school, an institution (both Wikipedias). It is a CBS locality, but whether
  the map wants it is the owner's call.
- **Founders. [S, Wikipedia]** English Wikipedia: its school was founded by the Alliance Israelite Universelle and the
  government (paraphrase); no founding group named.
- **Renames.** None found. Name from Ezekiel 27:3 (both Wikipedias).
- **Coordinates.** CBS 32.40326, 34.86824 (code 1318).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=42147188 ;
  https://en.wikipedia.org/w/index.php?oldid=1374534258 . All checked 2026-10-22.

### 23. Horeshim (חורשים), kibbutz

- **Founded January 1955. [V]** Gvati, section "קיבוצים": "חוֹרְשִים", region "השרון הדרומי", 6.1.55, affiliation
  "הקיבוץ הארצי". Hebrew Wikipedia: "הקיבוץ עלה על הקרקע בינואר 1955 כקיבוץ ה-69 של הקיבוץ הארצי." CBS 1955 (code
  355); English Wikipedia infobox "founded = 1955". **Day [C]**: 6 January (Gvati) or 13 January; Hebrew Wikipedia:
  "על פי ההזמנה לטקס העלייה על הקרקע בדצמבר 1954 (בגלל גשם שוטף נדחה והתקיים ב-13.1.55), בתחילה נקרא הקיבוץ
  החורשים." (the ceremony, planned for December 1954, was put off by rain to 13 January 1955). **We prefer 13 January
  for the ceremony**, the dated event; Gvati's 6 January may be the planned or the first day, but that is our reading.
- **Founders. [S, Wikipedia]** Hebrew Wikipedia: "מקימיו היו גרעין של חברי השומר הצעיר ילידי ישראל, רובם מאזור חיפה".
- **Renames. [S, Wikipedia]** First "החורשים" (quote above). **Name origin [C]**: Gvati: after a biblical town, Ge-Harashim (paraphrase), and
  English Wikipedia: the biblical valley of Charashim; Hebrew Wikipedia: "מקור השם של
  הקיבוץ הוא בכפר ערבי שהיה בסמוך לקיבוץ בשם ח'רבת חריש." (from a nearby Arab village, Khirbet Harish).
- **Coordinates.** CBS 32.13765, 34.97109 (code 355).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43615003 ;
  https://en.wikipedia.org/w/index.php?oldid=1374531760 . All checked 2026-10-22.

### 24. Hofit (חופית), community settlement

- **Founded: [C].** CBS: 1955 (code 115). Hebrew Wikipedia: "חופית הוקם בשנת 1953 עבור עובדי ציבור, שנתנו שירותים
  ליישובים החקלאיים של עמק חפר." and, in the same article, "המשפחות הראשונות עלו על הקרקע בשנת 1952" and, in 1955, the
  locality was recognised as "חופית" (Hebrew: הוכר היישוב רשמית כ"חופית"; paraphrase). English Wikipedia: "The first residents moved in 1952, and the village was officially
  recognised in 1955." Hebrew Wikipedia's category says 1955, English Wikipedia's 1952. **We prefer 1952 for the first
  settlement** (both Wikipedias); the CBS 1955 fits the official recognition (our reading). Not in Gvati.
- **Founders. [S, Wikipedia]** Public servants working for the farm villages of Hefer Valley (Hebrew Wikipedia);
  English Wikipedia: workers who wanted an agricultural lifestyle.
- **Renames.** None found; recognised "as Hofit" in 1955 (Hebrew Wikipedia, quote above), with no earlier name given.
- **Coordinates.** CBS 32.38499, 34.87412 (code 115).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=42420310 ;
  https://en.wikipedia.org/w/index.php?oldid=1374531719 . All checked 2026-10-22.

### 25. Yad Rambam (יד רמב"ם), moshav

- **Founded 1955. [V]** CBS: 1955 (code 64). **Day [C]**: Gvati: "יַד רַמְבַ"ם", region "שפלת יהודה", 30.10.55,
  affiliation "הפועל המזרחי"; English Wikipedia: "The moshav was founded on 30 October 1955" (citing Tagger and Kerem,
  *Guidebook for Sephardic and Oriental Genealogical Sources in Israel*, 2006, p. 131, not read). Hebrew Wikipedia:
  "המושב הוקם ב-6 בנובמבר 1955 (כ"א חשוון, תשט"ז)."; 21 Heshvan 5716 is 6 November 1955 (`convertdate`), so the Hebrew
  article's two forms agree with each other. **We prefer 30 October** (Gvati and English Wikipedia), but whether
  English Wikipedia's source draws on Gvati is not known.
- **Founders. [S, Wikipedia]** English Wikipedia: "Most of the founders were Jewish immigrants from Fes in Morocco."
- **Renames.** None found. Named for the 750th anniversary of Maimonides' death (Gvati and Hebrew Wikipedia; English Wikipedia says only: named after Maimonides).
- **Coordinates.** CBS 31.90013, 34.90066 (code 64).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=44001045 ;
  https://en.wikipedia.org/w/index.php?oldid=1374538019 . All checked 2026-10-22.

### 26. Beit Uziel (בית עוזיאל), moshav

- **Founded 14 October 1956. [V]** Gvati: "בֵּית-עֻזִיאֵל", region "שפלת יהודה", 14.10.56, affiliation "הפועל
  המזרחי". Hebrew Wikipedia: "טקס העלייה על הקרקע התקיים ב-14 באוקטובר 1956." CBS 1956 (code 301); English Wikipedia:
  "The village was founded in 1956". The two day sources were not checked for independence.
- **Founders. [C]** Hebrew Wikipedia: "ההתיישבות היהודית במקום נוסדה בשנת 1956 על ידי עולים ממרוקו, בני מעמד
  הביניים שמימנו את תקומתה במשאביהם שלהם הם" (middle-class Moroccan immigrants who paid for it themselves). English
  Wikipedia: "Jews who had emigrated from Morocco and from India".
- **Renames.** None found. Named after Rabbi Ben-Zion Meir Hai Uziel (Gvati and Hebrew Wikipedia).
- **Coordinates.** CBS 31.87052, 34.90523 (code 301).
- **Sources.** CBS; Gvati; https://he.wikipedia.org/w/index.php?oldid=43120018 ;
  https://en.wikipedia.org/w/index.php?oldid=1374528795 . All checked 2026-10-22.

### 27. Tzukei Yam (צוקי ים), community settlement

- **Founded: [C].** CBS: 1956 (code 1102). English Wikipedia: "The village was established in 1956, and was named
  after the nearby cliffs." Hebrew Wikipedia: "ראשיתו של צוקי ים בשנת 1968 בשכונת וילות בנות שלושה חדרים, שהוקמה על
  ידי חברת רסקו בצמוד למושב חבצלת השרון" (began in 1968 as a Rassco villa neighbourhood next to Havatzelet HaSharon);
  its category says 1968. **No preference**: CBS and English Wikipedia agree on 1956 but give no event; Hebrew
  Wikipedia gives a dated event. Not in Gvati.
- **Founders. [S, Wikipedia]** Rassco (Hebrew Wikipedia); no founding group named.
- **Renames.** None found.
- **Coordinates.** CBS 32.36133, 34.85892 (code 1102).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=41757913 ;
  https://en.wikipedia.org/w/index.php?oldid=1374537787 . All checked 2026-10-22.

## Gaps and notes for the owner

- **Size.** By the CBS year 21 localities qualify, 2 of them institutions; with the Gvati and Wikipedia additions, 27.
  The district's big wave came in 1949-1951 (`settlements-1949-51.md` and the 42 CBS district-4 rows dated 1950-51);
  1952-56 is mostly moshavim filling gaps in the Sharon and the Lod-Ayalon area.
- **Many CBS years disagree with the other sources** (rows 1, 2, 3, 9, 11, 13, 15, 16, 18, 22, 24, 27). In several
  the CBS year matches a later step (local council, official recognition) or a parent settlement (our reading in rows 1, 2, 15
  and 24). The CBS year alone is not a safe founding date for this district.
- **Institutions.** Three rows are a youth village, a yeshiva and a nautical youth village. They are CBS localities,
  but whether the map wants them is the owner's call.
- **Relocated or split settlements.** Nehalim (row 3) came from Upper Galilee; Einat and Givat Haim (Ihud) (rows 1
  and 2) were split from older kibbutzim in 1952. Whether the map dates them by the new site or the old founding is the
  owner's call.
- **Defunct localities** founded 1952-56 in the district were not searched for beyond the Wikipedia categories, and
  district-4 CBS rows with no founding year were not checked (see "Sources not reached").
- **No newspaper of the time was read**; every newspaper date above is as Wikipedia cites it.
- **Days** confirmed by two sources for Beit Hilkia (6 Jan 1953) and Beit Uziel (14 Oct 1956); Tenuvot, Kfar Bin Nun,
  Sdei Hemed, Bareket, Sha'ar Efraim, Nir Zvi and Einat have Gvati's day only; Bahan, Horeshim and Yad Rambam have two
  different days; Gannot has three dates; the rest have a month or a year.
- **Site notes** are recorded for twelve rows (Tenuvot, Kfar Bin Nun, Sdei Hemed, Bareket, Beit Hilkia, Gan HaDarom,
  Bahan, Gannot, Ganei Tikva, Neve Monosson, Aseret, Shedema; Kfar Bin Nun and Aseret from both Wikipedias; Bahan and Ganei Tikva cite both, but the Hebrew side is not about
  depopulation; the others from one (Tenuvot, Sdei Hemed and Gannot Hebrew; Bareket, Beit Hilkia, Gan HaDarom, Neve
  Monosson and Shedema English), many of the English ones citing Khalidi, *All That Remains*, 1992, not read). Horeshim's name origin (row
  23) also names an Arab village. Hexes and armistice-line flags were not computed; they belong to the paused
  data-file step.
