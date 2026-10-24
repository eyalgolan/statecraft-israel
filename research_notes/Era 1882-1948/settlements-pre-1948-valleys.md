# Jewish localities founded in the valleys before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`.
Refs #97, its valleys region (claimed in
https://github.com/eyalgolan/statecraft-israel/issues/97#issuecomment-6365918565). The dated
settlements file and `tools/settlement_hexes.py` stay with #61, and rows with a rename or an
abandonment date need the owner's sensitivity review before they enter the game; none is proposed
here. Method, sources and tags are those of the Galilee note (#1429), the Haifa and Carmel note
(#1435), the coastal plain and Sharon note (#1477) and the Negev note (#1403).

## How it was done

- Every page was fetched on 23 October 2026 with the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024,
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23),
  columns `שם נפה`, `אזור טבעי`, `דת יישוב`, `שנת ייסוד` and `קואורדינטות`. The twelve-digit
  coordinate is read as an Israel Transverse Mercator easting and northing of six digits each and
  converted with `pyproj` (EPSG:2039 to EPSG:4326), the reading `settlements-1949-51.md`, #1403,
  #1429, #1435 and #1477 use. The file does not state that reading, so every lat/lon below is
  **computed here** from the CBS string, not printed by CBS.
- **Founding dates.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix, Ben-Yehuda Project
  edition, https://benyehuda.org/read/14684 (checked 2026-10-23). Each row gives name, geographic
  region, date of founding, affiliation (kibbutzim and moshavim only) and the origin of the name.
  The column does not say whether a date is the day of going onto the land or a later
  recognition. Dates are quoted exactly as printed, for example `4.11.22` or `5.39`.
- **Village Statistics 1945** (Government of Palestine), the archive.org scan
  https://archive.org/details/palestine-village-statistics-1945 and its text layer
  `Village Statistics 1945_djvu.txt` (checked 2026-10-23), Table I "Population and total land
  areas". In 1945 the area of this note was spread over four sub-district tables: **Nazareth**
  (Jezreel valley), **Beisan** (Harod valley, Gilboa and the Beit She'an valley), **Haifa** (Ramot
  Menashe, Yokneam and the western valley) and **Tiberias** (Menahemia). The text layer prints page
  numbers only in places (45 after the Beisan table, 62 to 64 around the Nazareth table, 49 and 50
  in the Haifa table, 73 after the first Tiberias page), so the table title is the locator. It is
  used only as a **presence check**: a Jewish locality named in a 1945 land unit existed by then. It
  is not a founding source, and it lists land units, so several kibbutzim are not named at all; "not
  listed by name" therefore proves nothing. Several units join an Arab village with one or two
  Jewish localities, and the text layer drops blank columns, so a lone population figure cannot be
  assigned to the Arab or Jewish column; figures are given only where all three columns (Arabs,
  Jews, Total) survive. Spellings are as printed, OCR errors included, marked [sic].
- **Census of Palestine 1931**: not used. #1429 found that the archive.org scan has no usable text
  layer. [GAP]
- **Hebrew Wikipedia** was read through the MediaWiki API as the wikitext of the current revision,
  cited by `oldid`. It is a lead and a cross-check, not an authority; a fact resting on it alone is
  marked **[S, Wikipedia]**. Where an article cites a newspaper item that was not opened here, the
  note says so. Quotes keep the source's wording; double quote marks inside a Hebrew quote are
  printed as single quotes; gershayim in Hebrew dates and abbreviations (for example
  תרפ"ד) are kept as printed.

**Tags**, as in `settlements-1949-51.md`:
- **[V] verified**: two independent sources agree. For a founding year that is CBS and Gvati;
  CBS's coordinates are **computed here** (see above).
- **[S] single source**: one source, or several that plainly copy one another. **[S, Wikipedia]**
  when that source is Hebrew Wikipedia.
- **[C] conflicting**: sources disagree. Both values are given, each with its source, and
  **our reading** says which to use and why.
- **[GAP]** not found.
- **computed here** is arithmetic or a conversion done for this note, not a printed figure.
- **our reading** marks an interpretation and is not attributed to a source.
- A row with an earlier group or camp but one agreed CBS and Gvati year is [V] and the earlier date is
  written out in part 2; where he.wiki gives an earlier or later founding year for the same row it is [C].

## What counts as the valleys here

There is no single boundary, so this note states its rule.

Included: every locality that the CBS file codes as Jewish (`דת יישוב` = 1) with `שנת ייסוד` before
1948 in the **Jezreel sub-district** (`יזרעאל`), all seven of its natural regions (231 to 237;
53 rows), plus one row that CBS dates 1948 but Gvati dates before 15 May 1948 (Bet Lehem HaGelilit,
part 1b), 54 rows in all. This is the Jezreel valley, the Harod valley, the Gilboa, the Beit
She'an valley with the Jordan valley south of Lake Kinneret, Yokneam and the western valley, and
Ramot Menashe. The file has no name column for natural regions, so the headings below give the
code and, as **our reading**, the region that Gvati's own region column names for the rows in it.

Excluded:
- The Safed, Kinneret and Acre sub-districts, which the Galilee note (#1429) took, and the Haifa and
  Hadera sub-districts (#1435, #1477). The Galilee note left Menahemia, Gesher and the rest of the
  Jezreel sub-district for this note, so they are here; the Ramot Menashe kibbutzim that the
  Haifa note left for this note (for example Ein HaShofet and Ramat HaShofet) are here too.
- By date, the CBS 1948 rows of the sub-district whose Gvati rows are all dated after 15 May 1948:
  Shluhot `10.6.48`, Reshafim `10.6.48`, Ein Dor `16.6.48`, Alonei Abba `23.5.48`, Gazit `27.7.48`,
  Yizre'el `20.7.48` and Ramot Menashe `31.7.48`. Bet Lehem HaGelilit (Gvati `18.4.48`) is the one
  exception, part 1b.
- Beit She'an (CBS code 9200, year 1948). The 1945 Beisan table lists "Beisan (Urban)" with a
  population in the Arab column only, so it is not a Jewish locality before 1948.

Places that stood in the same area before 15 May 1948 and were later renamed, moved or merged are
in part 3.

## 1. The CBS localities, one row each

Read every row as: CBS name (CBS transliteration); CBS locality code; CBS founding year; the
coordinate computed from the CBS string; Gvati's row (region, date as printed, affiliation); the
1945 land unit that names it, if any; the tag. All CBS cells come from bycode2024.xlsx, all Gvati
cells from https://benyehuda.org/read/14684, all 1945 cells from the archive.org text layer cited
above (the table named in each cell), each checked 2026-10-23. Rows tagged "part 2" have a
disagreement, a rename, a move, a day that differs or an earlier founding written out below.

### Jezreel sub-district, natural region 231 (Gvati's region column: Beit She'an valley, Jordan valley)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| מנחמיה (Menahemya) | 48 | 1902 | 32.66605, 35.55501 | עמק הירדן, `23.12.1902`, (no affiliation column) | Tiberias: "Menahamiya" | [C] he.wiki 1901; renamed 1921, part 2 |
| ניר דוד (תל עמל) (Nir Dawid (Tel Amal)) | 256 | 1936 | 32.50376, 35.45585 | בקעת בית-שאן, `10.12.36`, הקיבוץ הארצי | Beisan: "Sakhina, Es and Nir David (Tel ‘Amal)" | [V]; first named Tel Amal, part 2 |
| בית יוסף (Bet Yosef) | 265 | 1937 | 32.55934, 35.55163 | בקעת בית-שאן, `9.4.37`, תנועת המושבים | Beisan: "Jabbéil and Beit Yosef" | [V] |
| טירת צבי (Tirat Zevi) | 268 | 1937 | 32.42074, 35.52863 | בקעת בית-שאן, `30.6.37`, הפועל המזרחי | Beisan: "Tirat Tsevi (Ez Zarra‘a)" | [V] |
| מעוז חיים (Ma'oz Hayyim) | 272 | 1937 | 32.49308, 35.55036 | בקעת בית-שאן, `6.7.37`, הקיבוץ המאוחד | Beisan: "Ghazawiya, El, Ma‘oz Hatyim [sic] and Neve Eitan" | [V]; three groups left Nov 1938, part 2 |
| שדה נחום (Sede Nahum) | 259 | 1937 | 32.52777, 35.48142 | בקעת בית-שאן, `5.1.37`, הקיבוץ המאוחד | Beisan: "Beisan (Rural), Sede Nahum and Messilot" | [V] |
| כפר רופין (Kefar Ruppin) | 295 | 1938 | 32.45777, 35.55663 | בקעת בית-שאן, `25.11.38`, האיחוד | Beisan: "Masil el Jizl and Kefar Ruppin (Massada)" | [V] |
| מסילות (Mesillot) | 298 | 1938 | 32.49559, 35.47413 | בקעת בית-שאן, `22.12.38`, הקיבוץ הארצי | Beisan: "Beisan (Rural), Sede Nahum and Messilot" | [V]; first near Tel Amal, part 2 |
| נווה איתן (Newe Etan) | 296 | 1938 | 32.49200, 35.53176 | בקעת בית-שאן, `25.11.38`, האיחוד | Beisan: same unit as Ma‘oz Hatyim | [V]; part 2 |
| גשר (Gesher) | 305 | 1939 | 32.62055, 35.55119 | עמק הירדן, `5.39`, הקיבוץ המאוחד | Beisan: "Gesher" | [V]; Gvati gives month and year only, part 2 |
| שדה אליהו (Sede Eliyyahu) | 304 | 1939 | 32.44041, 35.51368 | בקעת בית-שאן, `5.5.39`, הפועל המזרחי | Beisan: "‘Arida, El and Sede Eliyahu" | [V] |
| חמדיה (Hamadya) | 343 | 1942 | 32.52039, 35.51984 | בקעת בית-שאן, `1.9.42`, האיחוד | Beisan: "Hamidiya, El and Hermonim", see part 2 | [V]; founded 1939, abandoned, refounded 1942, part 2 |
| עין הנצי"ב (En HaNaziv) | 383 | 1946 | 32.47064, 35.50102 | בקעת בית-שאן, `17.1.46`, הפועל המזרחי | not listed by name | [V] |

### Jezreel sub-district, natural region 232 (Gvati: Jezreel valley)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| גבע (Geva) | 86 | 1921 | 32.56629, 35.37208 | [GAP] no row | Beisan: "Geva’" | [S] CBS only for the year; he.wiki 1921 |
| כפר יחזקאל (Kefar Yehezqel) | 85 | 1921 | 32.56714, 35.35970 | עמק יזרעאל, `16.12.21`, תנועת המושבים | Beisan: "Kefar Yehezael" [sic] | [V] |
| עין חרוד (איחוד) (En Harod (Ihud)) | 89 | 1921 | 32.56363, 35.39114 | עמק יזרעאל, `21.9.21`, האיחוד | Beisan: same unit as Ein Harod (Me'uhad) | [V]; Ein Harod split 1952, part 2 |
| עין חרוד (מאוחד) (En Harod(Me'uhad)) | 82 | 1921 | 32.55837, 35.39155 | עמק יזרעאל, `21.9.21`, הקיבוץ המאוחד | Beisan: "‘Ein Harod" (one unit for both kibbutzim) | [V]; Ein Harod split 1952, part 2 |
| תל יוסף (Tel Yosef) | 84 | 1921 | 32.55624, 35.39958 | עמק יזרעאל, `15.12.21`, האיחוד | Beisan: "Tel Yosef" | [V]; day differs, part 2 |
| בית אלפא (Bet Alfa) | 95 | 1922 | 32.51689, 35.43148 | עמק יזרעאל, `4.11.22`, הקיבוץ הארצי | Beisan: "Beit Alfa" | [V] |
| חפצי-בה (Hefzi-Bah) | 90 | 1922 | 32.51905, 35.42398 | עמק יזרעאל, `14.10.22`, הקיבוץ המאוחד | Beisan: "Heftsi Bah" | [V] |
| בית השיטה (Bet HaShitta) | 242 | 1935 | 32.55193, 35.43981 | עמק יזרעאל, `12.12.35`, הקיבוץ המאוחד | Beisan: "Beit hash Shitta (Shatta)" | [V]; first camp 1931, part 2 |

### Jezreel sub-district, natural region 233 (Gvati: Lower Galilee)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| מולדת (Moledet) | 269 | 1937 | 32.58635, 35.44047 | הגליל התחתון, `4.7.37`, תנועת המושבים | Beisan: "Taiyiba, Et and Benei Berit (Benei Berit was previously Moledet)" | [V]; renamed Benei Berit 1940, part 2 |
| רמת צבי (Ramat Zevi) | 339 | 1942 | 32.59152, 35.41389 | גליל תחתון, `22.1.42`, תנועת המושבים | not listed by name | [V]; first on Moledet land, part 2 |

### Jezreel sub-district, natural region 234 (Gvati: Jezreel valley, except Daverat: Lower Galilee)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| מרחביה (קיבוץ) (Merhavya(Qibbuz)) | 66 | 1911 | 32.60627, 35.30719 | עמק יזרעאל, `24.1.11`, הקיבוץ הארצי | Nazareth: "Merhavya Settlement" and "Methavya Group" [sic] (two units, not told apart in the text layer) | [C] he.wiki 1929 for the kibbutz; part 2 |
| נהלל (Nahalal) | 80 | 1921 | 32.69038, 35.19653 | עמק יזרעאל, `11.9.21`, תנועת המושבים | Nazareth: "Nahalal and Shimron" | [V] |
| בלפוריה (Balfurya) | 94 | 1922 | 32.63008, 35.29655 | עמק יזרעאל, `2.11.22`, תנועת המושבים | Nazareth: "Balfourya" | [V] |
| גיניגר (Ginnegar) | 92 | 1922 | 32.66401, 35.25722 | עמק יזרעאל, `21.10.27`, האיחוד | Nazareth: "Ginneigar" | [C] Gvati 1927, he.wiki 1922; part 2 |
| מרחביה (מושב) (Merhavya(Moshav)) | 97 | 1922 | 32.60381, 35.31641 | עמק יזרעאל, `1922`, תנועת המושבים | Nazareth: same two units as the kibbutz row | [C] he.wiki 1924 for the moshav; part 2 |
| כפר גדעון (Kefar Gid'on) | 106 | 1923 | 32.64406, 35.29130 | עמק יזרעאל, `16.11.28`, פועלי אגודת ישראל | Nazareth: "Kefar Gid‘on" | [C] Gvati 1928, he.wiki 1923; part 2 |
| מזרע (Mizra) | 104 | 1923 | 32.65093, 35.28639 | עמק יזרעאל, `24.10.23`, הקיבוץ הארצי | Nazareth: "Mizra’" | [V]; day differs, part 2 |
| תל עדשים (Tel Adashim) | 103 | 1923 | 32.65634, 35.30289 | עמק יזרעאל, `27.10.23`, תנועת המושבים | Nazareth: "Tel ‘Adashim" | [C] he.wiki 1913; part 2 |
| עפולה (Afula) | 7700 | 1925 | 32.61695, 35.30201 | [GAP] no row | Nazareth: "‘Afula (Urban)" (Arabs 10, Jews 2,300, Total 2,310) | [S] CBS only for the year; he.wiki 1925 |
| גבת (Gevat) | 133 | 1926 | 32.67479, 35.21248 | עמק יזרעאל, `4.12.26`, הקיבוץ המאוחד | Nazareth: "Gevat" | [V]; day differs, part 2 |
| יפעת (Yif'at) | 134 | 1926 | 32.67681, 35.22479 | עמק יזרעאל, `18.7.54`, האיחוד | not listed by name | [C] Gvati 1954, he.wiki 1954; CBS 1926 is the first house; part 2 |
| כפר ברוך (Kefar Barukh) | 132 | 1926 | 32.64595, 35.19090 | עמק יזרעאל, `8.11.26`, תנועת המושבים | Nazareth: "Kefar Barukh" | [V] |
| משמר העמק (Mishmar HaEmeq) | 130 | 1926 | 32.61023, 35.14239 | עמק יזרעאל, `2.11.26`, הקיבוץ הארצי | Haifa: "Mishmar ha ‘Emeq" | [V]; day differs, core group 1922, part 2 |
| רמת דוד (Ramat Dawid) | 135 | 1926 | 32.67823, 35.20178 | עמק יזעאל, `12.26`, האיחוד | Nazareth: "Ramat David, ‘Ayanot and Hash Sharon" [sic] | [V] |
| שריד (Sarid) | 126 | 1926 | 32.66328, 35.22520 | עמק יזרעאל, `15.5.26`, הקיבוץ הארצי | Nazareth: "Sarid" | [V]; day differs, part 2 |
| דברת (Daverat) | 407 | 1946 | 32.64785, 35.34953 | הגליל התחתון, `13.10.46`, האיחוד | not listed by name | [V]; day differs, part 2 |

### Jezreel sub-district, natural region 235 (Gvati: Jezreel valley)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| רמת ישי (Ramat Yishay) | 122 | 1925 | 32.70281, 35.16552 | עמק יזרעאל, `1925`, (no affiliation column) | Haifa: "Ramat Yuissai (previously Jeida)" [sic] | [V] |
| כפר יהושע (Kefar Yehoshua) | 140 | 1927 | 32.68265, 35.15239 | עמק יזעאל, `12.8.27`, תנועת המושבים | Haifa: "Kefar Yehoshua‘" | [V] |
| שדה יעקב (Sede Ya'aqov) | 142 | 1927 | 32.69823, 35.14137 | עמק יזרעאל, `19.9.27`, הפועל המזרחי | Haifa: "Sede Ya'agov (previously part of Esh Sheikh Bureik)" [sic] | [V] |
| יקנעם (מושבה) (Yoqne'am(Moshava)) | 241 | 1935 | 32.65473, 35.11494 | עמק יזרעאל, `1.12.35`, (no affiliation column) | Haifa: "Yoqne‘am (previously" (Arabs 410, Jews 280, Total 690) | [V] |
| בית שערים (Bet She'arim) | 248 | 1936 | 32.69662, 35.17824 | עמק יזרעאל, `27.3.31`, תנועת המושבים | Haifa: "Beit She‘arim" | [C] Gvati 1931, he.wiki 1926 and 1936; part 2 |
| הזורע (HaZorea) | 250 | 1936 | 32.64401, 35.11944 | עמק יזרעאל, `15.4.36`, הקיבוץ הארצי | Haifa: "Haz Zorea‘ (previously" | [V] |
| אלונים (Allonim) | 285 | 1938 | 32.71998, 35.14529 | עמק יזרעאל, `20.6.38`, הקיבוץ המאוחד | Haifa: "Tiv'on (Alonim) (previously Qusqus..." [sic, cut off] | [V]; earlier camps 1936 to 1937, part 2 |
| בית זיד (Bet Zeid) | 353 | 1943 | 32.70130, 35.12993 | [GAP] no row | Haifa: "Giv‘ot Zeid (previously part of Esh Sheikh Bureik)", see part 2 | [C] he.wiki: Zeid family from 1926, Giv'ot Zeid named for Zeid, Bet Zeid founded in 1951; part 2 |

### Jezreel sub-district, natural region 236 (Gvati: Ramat Menashe)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| עין השופט (En HaShofet) | 270 | 1937 | 32.59588, 35.10035 | רמת מנשה, `5.7.37`, הקיבוץ הארצי | Haifa: "‘Ein hash Shofet (previously Ji‘ara)" | [V] |
| דלייה (Daliyya) | 300 | 1939 | 32.58968, 35.07490 | רמת מנשה, `2.5.39`, הקיבוץ הארצי | Haifa: "Daliyat er Raha and Dalia" | [V]; day differs, part 2 |
| רמת השופט (Ramat HaShofet) | 335 | 1941 | 32.61072, 35.09411 | רמת מנשה, `18.12.41`, הקיבוץ הארצי | Haifa: "Ramat hash Shofet" | [V] |
| עין העמק (En HaEmeq) | 367 | 1944 | 32.62878, 35.08479 | רמת מנשה, `1944`, תנועת המושבים | not listed by name | [V] |
| גלעד (אבן יצחק) (Gal'ed (Even Yizhaq)) | 369 | 1945 | 32.55722, 35.07644 | רמות מנשה, `11.3.45`, האיחוד | not listed by name | [V] |

### Jezreel sub-district, natural region 237 (Gvati: Lower Galilee)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| כפר החורש (Kefar HaHoresh) | 192 | 1933 | 32.70092, 35.27301 | הגליל התחתון, `16.6.33`, האיחוד | Nazareth: "Kefar ha Horesh" | [C] he.wiki 1930; part 2 |

### 1b. One CBS 1948 row that Gvati dates before 15 May 1948

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| בית לחם הגלילית (Bet Lehem HaGelilit) | 430 | 1948 | 32.73508, 35.19102 | עמק יזרעאל, `18.4.48`, תנועת המושבים | Haifa: "Beit Lahm" (a land unit; the text layer does not say which column holds its 370) | [C] CBS 1948, he.wiki 1906; part 2 |

CBS natural region 237, Jezreel sub-district. See part 2.

## 2. Disagreements, renames, moves and days that differ

Hebrew Wikipedia pages below were read at the `oldid` given, checked 2026-10-23.

### Menahemia (CBS 48)
- CBS **1902**; Gvati `23.12.1902` (moshava section). The 1945 Tiberias table names "Menahamiya".
- he.wiki `מנחמיה`, oldid **44057960**: "היישוב נוסד ב-1901, והיה היישוב העברי הראשון בעמק הירדן";
  "התיישבו חמש המשפחות הראשונות על אדמות שנרכשו בדרום עמק הירדן" (23 to 26 December 1901); "בשנת 1921
  שונה שמה של המושבה למנחמיה על שם מנחם, אביו של הנציב העליון הרברט סמואל". [S, Wikipedia] for 1901
  and the 1921 rename.
- **[C]** 1902 (CBS and Gvati) against December 1901 (he.wiki, first five families). **Our reading:**
  both are real events of one founding; the article itself says the colony formed in three steps
  (the families of December 1901, the union with the Lahmiya lands on 25 December 1902, the
  colony proper in January 1903). Keep both years. Renamed **1921**; the article calls the colony
  "מלחמיה" before that ("בינואר 1903 התגבשה המושבה מלחמיה"). [S, Wikipedia]

### Merhavya, kibbutz and moshav (CBS 66 and 97)
- CBS **1911** for the kibbutz, **1922** for the moshav. Gvati: kibbutz `24.1.11`, moshav `1922`.
- he.wiki `מרחביה (קיבוץ)`, oldid **43794936**: "הקיבוץ, שנוסד בשנת 1929"; "התיישבות יהודית במרחביה
  הייתה כבר משנת 1911 (הקואופרציה של פרנץ אופנהיימר". he.wiki `מרחביה (מושב)`, oldid **43761665**:
  "בשנת 1924 הפכה המושבה מרחביה, אשר הוקמה בשנת 1911 בסמוך לקואופרציה במרחביה, למושב עובדים"; "בשנת
  1929 הוקם קיבוץ מרחביה בסמוך למושב". [S, Wikipedia]
- **[C]** kibbutz: 1911 (CBS, Gvati) against 1929 (he.wiki); moshav: 1922 (CBS, Gvati) against 1924
  (he.wiki). **Our reading:** 1911 is the first Jewish settling on the land (Oppenheimer's
  cooperative) and CBS and Gvati give it to the kibbutz row; the kibbutz as it is known today is of
  1929 and the moshav of 1922 to 1924. Keep all four numbers, since the rows share one name.

### Ein Harod, Ihud and Me'uhad (CBS 82 and 89)
- CBS **1921** both; Gvati `21.9.21` both. [V]
- he.wiki `עין חרוד`, oldid **43693275**: "עין חרוד הוא הקיבוץ הראשון ... שהוקם על ידי חברי גדוד
  העבודה בי"ח באלול תרפ"א (21 בספטמבר 1921) והתפלג בשנת 1952 לשני קיבוצים: עין חרוד איחוד ועין חרוד
  מאוחד". [S, Wikipedia] for the split. The 1945 Beisan table names one unit "‘Ein Harod".
- Both CBS rows therefore carry the 1921 founding of the one kibbutz; the two kibbutzim exist from
  **1952** (after this era). Tel Yosef's he.wiki date for its founding is in the day list below.

### Ginegar (CBS 92)
- CBS **1922**; Gvati `21.10.27` (kibbutz section, Ihud). The 1945 Nazareth table names "Ginneigar".
- he.wiki `גניגר`, oldid **43069232**: "הקיבוץ הוקם בשנת 1922, שנה בה הועברו אדמותיו רשמית על שם
  הקק"ל (הכשרת האדמות החלה כבר בשנת 1916)"; "בשנת 1922 הם עברו לגניגר". [S, Wikipedia]
- **[C]** 1922 (CBS, he.wiki) against 1927 (Gvati). **Our reading:** 1922, two sources against one;
  Gvati's `21.10.27` is not explained by anything read here.

### Kfar Gid'on (CBS 106)
- CBS **1923**; Gvati `16.11.28` (moshav section). The 1945 Nazareth table names "Kefar Gid‘on".
- he.wiki `כפר גדעון`, oldid **43740819**: "היישוב הוקם בשנת 1923 על ידי עולים מטרנסילבניה";
  "בהתחלה התיישבו במקום 6 משפחות ולאחר כשנה הגיעו שאר המשפחות". [S, Wikipedia]
- **[C]** 1923 (CBS, he.wiki) against 1928 (Gvati). **Our reading:** 1923, with the first six
  families; the rest followed about a year later, which does not reach 1928.

### Bet She'arim (CBS 248)
- CBS **1936**; Gvati `27.3.31` (moshav section). The 1945 Haifa table names "Beit She‘arim".
- he.wiki `בית שערים (מושב)`, oldid **43745017**: "בשנת 1926, לאחר שאדמות המושב נרכשו על ידי הקק"ל
  ביוגוסלביה ... התיישבו במקום חלוצים יוגוסלבים"; "בשנת 1936 הועברו הקרקעות לקבוצה החדשה והוקם מושב
  העובדים הנוכחי". [S, Wikipedia]
- **[C]** three dates: 1926 (first settling by Yugoslav pioneers), 1931 (Gvati) and 1936 (CBS, and
  the founding of the present moshav in he.wiki). **Our reading:** 1936 for the moshav that exists,
  1926 as the earlier founding on the same land; Gvati's 1931 has no support in the passages read.

### Yifat (CBS 134)
- CBS **1926**; Gvati `18.7.54` (kibbutz section, Ihud). The 1945 tables do not name Yifat.
- he.wiki `יפעת`, oldid **44079115**: "העלייה להתיישבות בגבת ובקבוצת השרון בדצמבר 1926"; "ב-15 ביולי 1954
  עברו כל החברים והילדים מקבוצת השרון וגבת ליפעת"; "ב-27 בדצמבר 1954 אישרה ועדת השמות הממשלתית את
  השם". [S, Wikipedia]
- **[C]** 1926 (CBS) against 1954 (Gvati, and the date the members moved in). **Our reading:** Yifat
  as a named kibbutz begins on **15 July 1954**; CBS's 1926 is the settling of its two predecessors,
  Gvat and the Sharon group (Kvutzat HaSharon, which he.wiki `רמת דוד`, oldid **44006598**, says joined
  Gvat's Mapai members in 1953: "ב-1953 התחברה קבוצת השרון ... והקימה עימם את קיבוץ יפעת").
  Gvat is its own row (CBS 133). A game date should not take 1926 for a place named in 1954.

### Kfar HaHoresh (CBS 192)
- CBS **1933**; Gvati `16.6.33`. The 1945 Nazareth table names "Kefar ha Horesh".
- he.wiki `כפר החורש`, oldid **43555965**: "היישוב הוקם ב-30 בנובמבר 1930 על ידי קבוצת חברים מארגון
  הכפר"; "בשנת 1933 עזבה קבוצה זו את המקום על מנת לעבור למקום קבע משלה, ובמקומם הגיעו 40 מאנשי
  גורדוניה". [S, Wikipedia]
- **[C]** 30 November 1930 (he.wiki, first group) against 1933 (CBS, Gvati). **Our reading:** keep
  both; the first group left in 1933 and the Gordonia group came then (**our reading:** they are the later kibbutz).

### Tel Adashim (CBS 103)
- CBS **1923**; Gvati `27.10.23`. The 1945 Nazareth table names "Tel ‘Adashim".
- he.wiki `תל עדשים`, oldid **43685309**: "היישוב הוקם בשנת 1913 כהתיישבות של חברי ארגון השומר";
  "בשנת 1923 הגיעו למקום 40 משפחות חדשות והוא הפך למושב עובדים". [S, Wikipedia]
- **[C]** 1913 (he.wiki, Hashomer settlement) against 1923 (CBS, Gvati, the moshav). **Our reading:**
  1923 for the present moshav, 1913 as the earlier settling on the site.

### Bet HaShitta (CBS 242)
- CBS **1935**; Gvati `12.12.35`. [V]
- he.wiki `בית השיטה`, oldid **44080374**: "ב-1931, לאחר תקופת מעבר במחנה עבודה בכפר יחזקאל, החליטה
  קבוצת החוגים להצטרף לקיבוץ המאוחד והתיישבה במחנה זמני ליד מעין חרוד"; "ב-1935 נרכשו אדמות שטה,
  והקיבוץ הקים עליהן את נקודת הקבע שלו, החל מט"ז כסלו תרצ"ז, 12 בדצמבר 1935". [S, Wikipedia]
- The civil day agrees with Gvati. The article's Hebrew date does not match it: ט"ז בכסלו תרצ"ז is 30 November 1936, and 12 December 1935 is ט"ז בכסלו תרצ"ו (**computed here**). The group existed from **1931** at a temporary camp; the place CBS and
  Gvati date is the permanent site of 12 December 1935.

### Mishmar HaEmeq (CBS 130)
- CBS **1926**; Gvati `2.11.26`. The 1945 Haifa table names "Mishmar ha ‘Emeq".
- he.wiki `משמר העמק`, oldid **43684754**: "גרעין הקיבוץ נוסד ב-1922 בשם 'קיבוץ השומר הצעיר ב'' בנווה
  שאנן שבחיפה"; "ב-26 בנובמבר, קבוצת-חלוץ של 12 חברים ושלוש חברות עזבה את עפולה, התיישבה בחאן על תל
  שוש". [S, Wikipedia]
- Year [V]. The day differs: Gvati 2 November 1926, he.wiki 26 November 1926. The group was formed in
  **1922** in Haifa and went onto the land in 1926. **Our reading:** 1926 is the place; keep 1922
  as the group's founding.

### Nir David (Tel Amal), Mesillot, Ma'oz Hayyim and Newe Etan (CBS 256, 298, 272, 296)
- he.wiki `ניר דוד (תל-עמל)`, oldid **44069100**: "ב-10 בדצמבר 1936 הוקם הקיבוץ כשהוא נושא את השם 'תל
  עמל'"; "במאי 1937 שונה שם היישוב ל'ניר דוד'"; "בראשית שנות ה-80 של המאה ה-20 הושב לשם הקיבוץ שמו
  הקודם, 'תל עמל'". Gvati `10.12.36`, same day. [V] for the founding; the rename years are [S,
  Wikipedia].
- he.wiki `מסילות`, oldid **42319034**: "באוגוסט 1937 עלו 16 חברים מהגרעין לעמק בית שאן והקימו יחד עם
  שלושה גרעינים נוספים את היישוב מעוז"; "בנובמבר 1937 עזבו חברי גרעין במסילה את מעוז ועברו להתיישבות
  זמנית ליד תל עמל"; "ביוני 1938 נקרא שם הקיבוץ מסילות". he.wiki `מעוז חיים`, oldid **43802246**:
  "בחודש נובמבר 1938 עזבו שלושת הגרעינים ויצאו להקים קיבוצים אחרים בנקודות סמוכות בעמק בית שאן".
  he.wiki `נווה איתן`, oldid **43556121**: "ב-1938 (25 בנובמבר, ב' בכסליו תרצ"ט) נעשתה העלייה
  לקרקע". [S, Wikipedia]
- Gvati: Mesillot `22.12.38`, Ma'oz Hayyim `6.7.37`, Newe Etan `25.11.38`. Newe Etan and Kefar Ruppin
  have the same day in Gvati, and the article agrees for Newe Etan. **Our reading:** Mesillot's CBS and
  Gvati year, 1938, is its permanent site; the group existed from August 1937 inside Ma'oz Hayyim and
  then at a temporary site by Tel Amal.

### Hamadya (CBS 343)
- CBS **1942**; Gvati `1.9.42`. The 1945 Beisan table names a unit "Hamidiya, El and Hermonim"
  (printed: Hermonim was previously Irgun Deror).
- he.wiki `חמדיה`, oldid **43555833**: "הקיבוץ הוקם לראשונה ב-1939, במסגרת מבצעי חומה ומגדל, וננטש";
  "בשנת 1942 הוקם בשנית על ידי גרעין 'חרמונים'". [S, Wikipedia]
- Founded **1939**, abandoned (the article gives no year or date), refounded **1942**. **Our reading:**
  the 1945 unit's "Hermonim" is that 1942 group ("חרמונים" in the article); the text layer does not
  say so and no other source was read. [S, Wikipedia] Coordinate for the 1939 site: [GAP].

### Moledet, Benei Berit and Ramat Zevi (CBS 269 and 339)
- CBS Moledet **1937**; Gvati `4.7.37` (moshav shitufi section). The 1945 Beisan table names
  "Taiyiba, Et and Benei Berit" (printed: Benei Berit was previously Moledet).
- he.wiki `מולדת (יישוב)`, oldid **43881029**: "היישוב הוקם ב-4 ביולי 1937 במסגרת חומה ומגדל בשם
  מולדת על שם ארגונם של המייסדים"; "נקראו אנשי המקום ביולי 1940 לבירור אחרון לפני נקיטת צעדים ואנשי
  המקום נאלצו להסכים לשם 'בני ברית'". [S, Wikipedia] Moledet is the CBS name again; the year it was
  restored is not in the passages read. [GAP]
- he.wiki `רמת צבי`, oldid **43685519**: "בינואר 1942 עלו חברי הארגון להתיישב באופן זמני במולדת".
  Gvati Ramat Zevi `22.1.42`. [V] for the year, [S, Wikipedia] for the temporary stay in Moledet.

### Bet Zeid (CBS 353)
- CBS **1943**; Gvati: no row. The 1945 Haifa table names "Giv‘ot Zeid (previously part of Esh Sheikh
  Bureik)".
- he.wiki `בית זייד`, oldid **42326268**: "ב-1926, לאחר שגורש עם משפחתו מכפר גלעדי, התיישב אלכסנדר
  זייד, מוותיקי ארגון השומר"; "על שמו הוקם היישוב גבעות זייד, בו חיו בני משפחתו עד שעזבו בשנת 1951
  והקימו את בית זייד במקום בו התגוררו לפני כן". [S, Wikipedia]
- he.wiki, same article: "ב-11 ביולי 1938, בדרכו לפגישה עם אנשי אלונים, נרצח אלכסנדר זייד"; the
  place Giv'ot Zeid is named for him.
- **[C]** 1943 (CBS) against a Zeid family settling in 1926, a Giv'ot Zeid named for Zeid (the article gives no year; it comes straight after his killing on 11 July 1938) and a Bet Zeid that the article puts in 1951. **Our reading:** the family lived on the hills from 1926; Giv'ot Zeid was most likely named after his death, and it is in the 1945 table; the
  year of Bet Zeid itself is not settled by what was read. The 1943 rests on CBS alone (the article's category "יישובים שהוקמו ב-1943" probably follows CBS). Coordinate for
  Giv'ot Zeid: [GAP].

### Alonim (CBS 285)
- CBS **1938**; Gvati `20.6.38`. [V] The 1945 Haifa table names "Tiv'on (Alonim)".
- he.wiki `אלונים`, oldid **43859675**: "חצי שנה לאחר מכן, בכ"א בסיוון ה'תרצ"ח, 20 ביוני 1938, החלו
  חברי הקיבוץ להתיישב בקוסקוס תחתית"; "בכ"ג בטבת תרצ"ו, 18 בינואר 1936 התאחדו הקבוצות בקריית חרושת";
  "הסתיים המעבר למחנה העצמאי בשייך אבריק". [S, Wikipedia]
- The day agrees with Gvati. The group existed from **1936** at Kiryat Haroshet and then at Sheikh
  Abreik before the move of **20 June 1938**.

### Gesher (CBS 305)
- CBS **1939**; Gvati `5.39` (month and year only). The 1945 Beisan table names "Gesher". [V]
- he.wiki `גשר (קיבוץ)`, oldid **43555674**: "הגרעין הראשון של אנשי קיבוץ גשר ... התיישבו בשנת 1921
  במנחמיה"; "אנשי הקבוצה עברו לקראת סוף שנות ה-30 לאשדות יעקב, ובמקומם הגיעו למקום ב־1939 אנשיי
  גרעין של הנוער העובד". [S, Wikipedia]
- A group of **1921** at Menahemia preceded the 1939 founding and moved to Ashdot Ya'akov. **Our
  reading:** 1939 is the kibbutz; the 1921 group is the same name on other land.

### Bet Lehem HaGelilit (CBS 430, part 1b)
- CBS **1948**; Gvati `18.4.48` (moshav section).
- he.wiki `בית לחם הגלילית`, oldid **43906638**: "היישוב המודרני הוקם במקור ב-1906 על ידי
  הטמפלרים"; "ב-17 באפריל 1948 החל המבצע לכיבוש היישובים הטמפלרים וולדהיים ובית לחם". [S,
  Wikipedia]
- **[C]** CBS 1948 and Gvati 18 April 1948 for the Jewish moshav, he.wiki 1906 for the Templer
  colony on the same site. **Our reading:** the Jewish locality counted here begins on the Gvati
  date, one day after the operation the article names; the 1906 colony was founded by Templers ("הטמפלרים"), not a Jewish
  locality. Whether and how this row enters the game is for the owner's sensitivity review.

### Days that differ, year the same
Each is [V] on the year (CBS and Gvati agree) and the day differs from he.wiki, which is [S,
Wikipedia] for its own day:
- Tel Yosef: Gvati `15.12.21`; he.wiki `תל יוסף`, oldid **44069321**: "הוקם בי"ב בכסלו ה'תרפ"ב (13
  בדצמבר 1921)".
- Gevat: Gvati `4.12.26`; he.wiki `גבת`, oldid **44077253**: "הקיבוץ נוסד ב-28 בנובמבר 1926".
- Mizra: Gvati `24.10.23`; he.wiki `מזרע`, oldid **43646058**: "הקיבוץ הוקם על גבעת
  'רובע-אל-נצרא' ב-21 בנובמבר 1923".
- Daverat: Gvati `13.10.46`; he.wiki `דברת`, oldid **43555687**: "הוקם הקיבוץ ב-30 באוקטובר 1946
  בנקודה זמנית בעמק".
- Dalia: Gvati `2.5.39`; he.wiki `דליה (קיבוץ)`, oldid **43555701**: "הקיבוץ עלה על הקרקע ב-1 במאי
  1939".
- Sarid: Gvati `15.5.26`; he.wiki `שריד`, oldid **43556390**: "שריד הוקם בתחילת שנת תרפ"ז (סוף שנת
  1926) על גבעת כניפס".

**Our reading** for all six: use Gvati's year with CBS, and keep the he.wiki day beside it. The
column of Gvati's dates does not say what event a date records (see above).

## 3. Places that stood before 15 May 1948 and are not CBS localities today

These are the rows the issue calls renames and abandonment. Each is recorded with its source; nothing
is proposed about how it appears in the game. [checked 2026-10-23: no CBS row matches `גבעות זייד`,
`חרמונים`, `בני ברית` or `תל עמל` as a separate locality; Tel Amal is the former name of Nir David
(part 2).]

### Giv'ot Zeid (the place of Bet Zeid)
- See Bet Zeid in part 2: the Zeid family settled the hills in **1926**, the place Giv'ot Zeid is named for Alexander Zeid (killed 11 July 1938; the article gives no naming year) and is in the 1945 Haifa table as "Giv‘ot
  Zeid (previously part of Esh Sheikh Bureik)"; the family left in **1951** and founded Bet Zeid
  (he.wiki `בית זייד`, oldid **42326268**). [S, Wikipedia] Coordinate: [GAP].

### Benei Berit (the name of Moledet in 1945)
- See Moledet in part 2: the 1945 Beisan table gives "Benei Berit" for the unit "previously Moledet";
  he.wiki gives a forced agreement on that name in July 1940. Not a separate place. [S, Wikipedia]

### Hermonim (the 1942 group of Hamadya)
- See Hamadya in part 2. The 1945 unit names "Hermonim" with "previously Irgun Deror" in the text
  layer; he.wiki names the 1942 group "חרמונים". Not followed further. [GAP]

## 4. Gaps and what was not done

- [GAP] **Completeness of part 3.** Part 3 lists only the places that the CBS rows' own histories
  and the 1945 land units name. It is not shown to be complete. The 1945 units "Irgun Borokhov"
  (in the Tira unit of the Beisan table), "Mahane Yisrael" and "Kefar Yeladim" (Nazareth table) and
  "Shimron" (with Nahalal) are leads that were not followed; the Galilee note may cover the second
  and third under the Kinneret sub-district.
- [GAP] **Census of Palestine 1931**: not used (no usable text layer, see #1429).
- [GAP] **Coordinates of moved, renamed and merged places**: none has one here.
- [GAP] **Rename and abandonment years**: the year Moledet took its name back, the year Hamadya's
  first site was abandoned and the dates of Merhavya's two conversions are not in the passages read.
- [GAP] **Gvati rows**: Afula, Geva and Bet Zeid have none (Afula is listed by CBS and he.wiki as of
  1925, Geva as of 1921), so their year rests on CBS with Wikipedia beside it.
- **Gvati day versus CBS year.** Where they agree on the year the row is [V]; the day rests on Gvati
  alone, and as `settlements-1949-51.md` notes, it can be a day or more off other sources, as the
  six rows in part 2 show.
- **Not done:** no hex, no game date, no rule for which of two dates the game should use. Those
  belong to #61 and to the owner's sensitivity review.
