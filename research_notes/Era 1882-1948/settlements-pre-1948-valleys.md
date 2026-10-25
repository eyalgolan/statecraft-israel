# Jewish localities founded in the valleys before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`.
Refs #97, its valleys region (claimed in
https://github.com/eyalgolan/statecraft-israel/issues/97#issuecomment-6365918565). The dated
settlements file and `tools/settlement_hexes.py` stay with #61, and rows with a rename or an
abandonment date need the owner's sensitivity review before they enter the game; none is proposed
here. Method, sources and tags are those of the Galilee note (#1429), the Haifa and Carmel note
(#1435), the coastal plain and Sharon note (#1477) and the Negev note (#1403).

Update 2026-10-24: the [GAP] rows were worked again by an agent (Claude Code) for #97 (owner's 24 Oct
"Research part open" comment). Anything opened that day is marked "(24 Oct)". A person's check is
still pending, and no row below was moved to [V] by this update; everything added is [S], [C] or
[GAP], and press items cited by Wikipedia were not opened (nli.org.il returns 403 from this box).

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
  (24 Oct) The page images of the archive.org PDF `Village Statistics 1945.pdf` were rendered
  (Ghostscript, `-sDEVICE=png16m`, 110 to 130 dpi; the pdf page equals the printed page) and read
  for the rows named in the 24 Oct additions below: Table I printed p. 43 and p. 44 (Beisan), p. 47
  (Haifa), p. 62 and p. 63 (Nazareth). For those rows all columns are read from the image.
- **Census of Palestine 1931**, *Population of villages, towns and administrative areas*,
  https://archive.org/details/CensusOfPalestine1931.PopulationOfVillagesTownsAndAdministrativeAreas
  (opened 2026-10-24). #1429 found that the scan has no usable text
  layer, and nothing in the first pass of this note rested on it. (24 Oct) The PDF
  `PalestineCensus1931.pdf` of that item (124 pages) was downloaded and rendered as images
  (Ghostscript, `-sDEVICE=png16m`, 100 dpi, and 200 dpi for the rows whose digits are quoted with
  their houses), and the printed tables were read by eye. The pdf page is the printed page plus 11
  (printed p. 73 is pdf page 84). Read in full: printed pp. 73 to 76 (Nazareth sub-district), 77 to
  81 (Beisan), 83 and 84 (Tiberias, the page of Menahemiya) and 87, 91 to 98 (Haifa). Not read:
  printed pp. 82, 85, 86 (Tiberias) and 88 to 90 (Haifa). The figures are a **presence check** in
  the census year, with no founding date; they are in the table in part 2 (24 Oct). The census day
  is not on the pages read. [GAP] for the pages not read.
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
- (24 Oct) he.wiki `מרחביה (קיבוץ)`, oldid **43794936**: "משנת 1918 עת התפרקה הקואופרציה ועד 1921
  התחלפו במקום כמה קבוצות של מתיישבים. בשנת 1924 הוקם מושב העובדים מרחביה. בשנת 1929 הוקם בסמוך
  הקיבוץ". [S, Wikipedia] So the two conversions the first pass could not date are 1924 (the colony
  becomes a workers' moshav) and 1929 (a kibbutz is founded beside the moshav, not a conversion of
  it); neither article gives a day. The 1945 Nazareth table image (printed p. 62) names "Merhavya
  Settlement" (population Jews 270; land Jews 13,979, public 546, total 14,525) and "Merhavya Group"
  (population Jews 350; land Jews 3,014, public 109, total 3,123). The 1931 census prints "Merhavya"
  and a separate "Merhavya (Qevutsa)", Hebrew `קבוצת מרחביה` (table below). **Our reading:** the Group
  is the kibbutz and the Settlement the moshav, by the Qevutsa entry of 1931; the 1945 table does not
  say so.

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
- (24 Oct) Census of Palestine 1931, printed pp. 73 to 76 (Nazareth sub-district, all four pages
  read): no row named Ginegar or Ginnegar, but printed p. 73 (pdf p. 84) has the row "Jinuj'ar"
  (Hebrew column ג'נג'ר, pointed), the row just before "Gvat (Qevutsa)": 17 houses, 109 persons
  (57 male, 52 female), Jews 57 + 52, no other religion (read on the 150 dpi page image). The he.wiki
  article above gives ג'ינג'אר as the Arabic name of the place. **Our reading:** this row is the
  kibbutz (an all-Jewish unit on the land of the Arabic name Jinjar); the census does not say so.
  The 1931 run of names there goes "Jinuj'ar", "Gvat (Qevutsa)", "Gvat (Experimental Station)",
  "Hash Sharon", "Iksal". An all-Jewish unit of 109 persons in 1931 bears on the 1922 against 1927
  [C] (it fits 1922 and does not fit a first settling in 1927), but it does not date the founding.
  The 1945 table lists "Ginneigar" with population Jews 330 (printed p. 62). [GAP]

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
- (24 Oct) Census of Palestine 1931, Haifa sub-district, printed p. 92: a unit "Jeida" (29 houses,
  115 persons: Moslems 20 + 13 = 33, Jews 52 + 25 = 77, Christians 1 + 1 = 2, no religion 1 + 2 = 3;
  33 + 77 + 2 + 3 = 115, read at 200 dpi) with the sub-entries "Beit She'arim" and "Stock Farm".
  The 1945 table (Haifa, printed p. 47) has "Beit She'arim", Jews 330, land Jews 4,045, public 159,
  total 4,204; part 1 gives "Ramat Yuissai (previously Jeida)" for Ramat Yishay. So a place called Beit
  She'arim stood in the census year, which fits Gvati's `27.3.31` and he.wiki's 1926 and does not
  rule out CBS's 1936. **Our reading:** the 77 Jews of the Jeida unit are the people of Beit
  She'arim and of Ramat Yishay together; the census prints no Ramat Yishay row (none on printed p.
  95, where "Raml, Arab er" and "Rihaniya, Er" run). Presence only, no founding date.

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
- (24 Oct) Census of Palestine 1931, Nazareth sub-district, printed p. 73: "Gvat (Qevutsa)", 76
  persons, Jews 36 + 38 = 74, and "Gvat (Experimental Station)", 30 persons, and "Hash Sharon",
  56 persons. **Our reading:** these are the two predecessors that he.wiki names for Yifat
  (Gvat and the Sharon group), so by 1931 both predecessor groups were on the ground; the census
  does not mention Yifat, named in 1954. Presence only.

### Kfar HaHoresh (CBS 192)
- CBS **1933**; Gvati `16.6.33`. The 1945 Nazareth table names "Kefar ha Horesh".
- he.wiki `כפר החורש`, oldid **43555965**: "היישוב הוקם ב-30 בנובמבר 1930 על ידי קבוצת חברים מארגון
  הכפר"; "בשנת 1933 עזבה קבוצה זו את המקום על מנת לעבור למקום קבע משלה, ובמקומם הגיעו 40 מאנשי
  גורדוניה". [S, Wikipedia]
- **[C]** 30 November 1930 (he.wiki, first group) against 1933 (CBS, Gvati). **Our reading:** keep
  both; the first group left in 1933 and the Gordonia group came then (**our reading:** they are the later kibbutz).
- (24 Oct) Census of Palestine 1931, Nazareth sub-district, printed p. 74: "Kefar Hahoresh", 15
  persons, Jews 13 + 2 = 15. A place of that name existed in the census year, which fits the first
  group of 30 November 1930 in he.wiki and does not decide between that and CBS's 1933 (the move of
  the Gordonia group, per the article). Presence only.

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
- (24 Oct) he.wiki `חומה ומגדל`, oldid **43882052**, the table of tower-and-stockade settlements, row
  47: "חמדיה || 23 ביוני 1939". [S, Wikipedia] The first Hamadya is therefore dated 23 June 1939 there
  (the first pass had the year only). The same table marks other abandoned entries in brackets (row
  41: "שורשים (ננטש ב-1939)") and prints no such mark for Hamadya. English Wikipedia `Hamadia`, oldid
  1374531275: "The kibbutz was founded in 1939 as part of the Tower and stockade movement. It was
  re-established in 1942 by the 'Hermonim' pioneers". [S, Wikipedia] The 1945 Beisan table image
  (printed p. 43) reads "Hamidiya, El and Hermonim (Hermonim was previously Irgun Deror)": population
  Arabs 220, Jews 100, total 320; land Arabs 4,814, Jews 1,386, public 4,702, total 10,902.
  **Our reading:** the 100 Jews are Hermonim, since El Hamidiya was an Arab village; the table does
  not split the unit.
- (24 Oct) Searched again and not found: the year or day the 1939 site was abandoned, and its
  coordinate. Read: the he.wiki `חמדיה` wikitext (oldid 43555833), whose coordinate parameter is the
  present kibbutz (the API gives 32.52036847, 35.51990715; the en.wiki API gives 32.52027778,
  35.51972222; **computed here:** the he.wiki point is 7 m from CBS's point, 2 m north-south and 6 m
  east-west, and the en.wiki point is 17 m, 12 m and 11 m); the
  tower-and-stockade table above; the he.wiki chronology `כרונולוגיית ההתיישבות היהודית בארץ ישראל
  בעת החדשה` (oldid 43572242), which names Hamadya three times (under 1939, under 1942, and in the 1949 kibbutzim row "... שעלבים, חמדיה, לביא ..."), with no year of leaving; the Gvati
  page, where `חמדיה` occurs only in the name-origin cell of the `1.9.42` row. he.wiki has no
  article `חרמונים` (API: missing page, 2026-10-24). [GAP]

### Moledet, Benei Berit and Ramat Zevi (CBS 269 and 339)
- CBS Moledet **1937**; Gvati `4.7.37` (moshav shitufi section). The 1945 Beisan table names
  "Taiyiba, Et and Benei Berit" (printed: Benei Berit was previously Moledet).
- he.wiki `מולדת (יישוב)`, oldid **43881029**: "היישוב הוקם ב-4 ביולי 1937 במסגרת חומה ומגדל בשם
  מולדת על שם ארגונם של המייסדים"; "נקראו אנשי המקום ביולי 1940 לבירור אחרון לפני נקיטת צעדים ואנשי
  המקום נאלצו להסכים לשם 'בני ברית'". [S, Wikipedia] Moledet is the CBS name again; the year it was
  restored is not in the passages read. [GAP]
- he.wiki `רמת צבי`, oldid **43685519**: "בינואר 1942 עלו חברי הארגון להתיישב באופן זמני במולדת".
  Gvati Ramat Zevi `22.1.42`. [V] for the year, [S, Wikipedia] for the temporary stay in Moledet.
- (24 Oct) The year Moledet took its name back: he.wiki `מולדת (יישוב)`, oldid **43881029**, section
  on the name: "השם לא תפס ולרוב הופיע בשילוב עם 'מולדת', שם הארגון. עד ראשית שנות ה-50 הופיע השם בני
  ברית עם מולדת בסוגריים ולאחר מכן כבר נקרא המקום 'מולדת – כפר בני ברית' וצירופים דומים". [S,
  Wikipedia] The article gives no year for a restoration; the name Benei Berit was written beside
  Moledet by 1942 (the footnote of that sentence is a *Davar* item of 26 June 1942, not opened) and
  the written form shifts in the early 1950s. The last footnote of that
  passage cites *Davar* of 23 February 1951, not opened here. **Our reading:** there was no single
  restoration event to date. The year stays a [GAP] in the sense that no source read gives one.

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
- (24 Oct) he.wiki has a separate article for the place, `גבעות זייד`, oldid **43158155**: "גבעות זייד
  היה קיבוץ באזור עמק יזרעאל, ליד בית זייד. הקיבוץ הוקם ב-1940 ונקרא על שם אלכסנדר זייד. הוא פורק סופית
  ב-1957."; "בשנת 1957 הוחלט ב'איחוד הקבוצות והקיבוצים' לפרק את הקיבוץ. גבעות זייד נעזב וחלק מהחברים
  עברו להקים קיבוץ חדש בנגב"; and in its links list: "מפת Carmel East שיצאה לאור ב-1945, ועליה סימון
  'משק זייד' - מקומו של קיבוץ גבעות זייד משנת 1943" (the map was not opened here). [S, Wikipedia] The
  he.wiki chronology (oldid 43572242) lists `גבעות זייד` under 1940. Gvati's running text (the page
  https://benyehuda.org/read/14684, fetched 2026-10-24): "בסוף תשי״ז עברו חברי גבעות־זַיד לנגב והקימו
  את קיבוץ אור־הנר בשכנות לברור־חיל"; his appendix row for אור-הנר is `1.10.57`, `האיחוד`. [S] The 1945
  Haifa table image (printed p. 47): "Giv'ot Zeid (previously part of Esh Sheikh Bureik)", population
  Jews 110, total 110; land Jews 1,581, public 8, total 1,589.
- (24 Oct) **[C]** for the kibbutz Giv'ot Zeid: founded 1940 (he.wiki, two articles) against 1943 (CBS
  for the row Bet Zeid, and the map remark above); dissolved 1957 (he.wiki; Gvati's sentence
  agrees on the move to the Negev at the end of תשי״ז). **Our reading:** the
  kibbutz stood by 1943 at the latest and is in the 1945 table with 110 people; CBS's 1943 for "Bet
  Zeid" most likely dates this kibbutz and not the moshav that he.wiki puts in 1951; the 1940 rests
  on he.wiki alone. The 1951 settling of Gordonia people in the same article is a second group on the
  same site. Coordinate: the he.wiki `גבעות זייד` coordinate is 32.70133333, 35.115 (API
  `prop=coordinates`, 2026-10-24); **computed here:** 1,399 m west of CBS's Bet Zeid point (4 m
  north-south, 1,399 m east-west, at 93.7 km per degree of longitude at 32.7 N). The he.wiki `בית
  זייד` coordinate, 32.702397, 35.131597, is 198 m from CBS's point (122 m and 156 m). [S, Wikipedia]
  Not a map reading; Survey of Palestine sheets and GovMap were not opened.

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

### Geva and Afula, no Gvati row (24 Oct)
- Geva (CBS 86, 1921): he.wiki `גבע`, oldid **43555602**: "הקבוצה הוקמה בשנת 1921 על ידי עולים מרוסיה
  ומפולין". [S, Wikipedia] Afula (CBS 7700, 1925): he.wiki `עפולה`, oldid **43690677**: "העיר העברית
  עפולה נוסדה בשנת 1925 על ידי חברה ציונית מיישבת בשם קהילת ציון אמריקאית". [S, Wikipedia] Both
  years stay [S] (CBS and he.wiki).
- Search log, Gvati page https://benyehuda.org/read/14684, fetched 2026-10-24, tags stripped with a
  python script. A name cell reading exactly `גבע`: 0 matches (other cells begin with
  `גבע`, for example `גבע הכרמל` and `גבעתי`, different places); `הגבעה`: 0; `עפולה`: 5 lines, all running
  text, none a name cell; `זייד` or `זיד` in any form: the one sentence on Or HaNer quoted above. The
  no-row finding stands for Geva, Afula and Bet Zeid. [GAP]
- Census of Palestine 1931, Beisan sub-district, printed p. 78: a Jewish unit "Giv'a, Hag" (Hebrew
  `הגבעה`, "the hill"), 108 persons, Jews 56 + 52 = 108. The 1945 table (printed p. 43) has "Geva'",
  population Jews 380, land Jews 3,114, public 70, total 3,184. **Our reading, not shown:** the 1931
  unit may be Geva. No source read says Geva was called HaGiv'a, and the he.wiki `גבע` article
  does not. [GAP]

### Census of Palestine 1931, rows read for this note (24 Oct)
Each row is the unit as printed (names are the census's spellings), the printed page, the persons
and the Jews (male + female). Houses are given only where the row was read at 200 dpi. Presence in the
census year only; none of it dates a founding. The Jews column is as printed, and the persons total
also holds the few non-Jews of the unit.

| Locality in this note | Census unit as printed | Printed page | Persons | Jews (m + f) | Remark |
|---|---|---|---|---|---|
| Afula | 'Affula | 73 | 874 | 393 + 393 = 786 | 236 houses; Moslems 45 + 31, Christians 9, no religion 3 |
| Balfurya | Balfuriya | 73 | 238 | 114 + 124 = 238 | |
| Ginegar (our reading) | Jinuj'ar | 73 | 109 | 57 + 52 = 109 | 17 houses |
| Gevat | Gvat (Qevutsa) | 73 | 76 | 36 + 38 = 74 | and "Gvat (Experimental Station)", 30 persons |
| Yifat, the Sharon group (our reading) | Hash Sharon | 73 | 56 | not copied | |
| Kefar Barukh | Kefar Barukh | 74 | 175 | 84 + 90 = 174 | |
| Kefar Gid'on | Kefar Gid'on | 74 | 96 | 50 + 46 = 96 | |
| Kefar HaHoresh | Kefar Hahoresh | 74 | 15 | 13 + 2 = 15 | |
| Merhavya (moshav, our reading) | Merhavya | 74 | 218 | 118 + 100 = 218 | |
| Merhavya (kibbutz, our reading) | Merhavya (Qevutsa) | 75 | 84 | 52 + 32 = 84 | |
| Mizra | Mizra' | 75 | 81 | 43 + 38 = 81 | |
| Nahalal | Nahalal | 75 | 648 | 303 + 345 = 648 | |
| Sarid | Sarid | 76 | 68 | 38 + 27 = 65 | Moslems 2 + 1 |
| Tel Adashim | Tel 'Adashim | 76 | 251 | 142 + 109 = 251 | |
| Bet Alfa | Beit Alfa | 77 | 149 | 79 + 68 = 147 | no religion 1 + 1 |
| Ein Harod | Ein Harod | 78 | 483 | 253 + 225 = 478 | Moslems 2 + 3; sub-entry "Old Ein Harod" |
| Geva (our reading, see above) | Giv'a, Hag | 78 | 108 | 56 + 52 = 108 | |
| Hefzi-Bah | Heftsi Bah | 79 | 108 | 56 + 51 = 107 | no religion 1 |
| Kefar Yehezqel | Kefar Yehezkel | 79 | 431 | 217 + 214 = 431 | |
| Tel Yosef | Tel Yosef | 81 | 261 | 140 + 121 = 261 | |
| Menahemia | Menahemiya | 83 | 182 | 97 + 85 = 182 | 47 houses |
| Kefar Yehoshua | Kefar Yehoshua | 93 | 277 | 126 + 119 = 245 | 53 houses; Moslems 20 + 12 |
| Mishmar HaEmeq | Mishmar ha 'Emeq | 94 | 122 | 70 + 52 = 122 | 59 houses |
| Bet She'arim, Ramat Yishay (our reading) | Jeida, with sub-entries Beit She'arim and Stock Farm | 92 | 115 | 52 + 25 = 77 | 29 houses |
| Sede Ya'aqov, Giv'ot Zeid or El Ro-i (not shown) | Sheikh Bureik, Esh (sub-entry Sheikh Bureik Mizrahi) | 96 | 37 | 22 + 15 = 37 | 10 houses; the 1945 table calls all three "previously part of Esh Sheikh Bureik" |

Rows with no 1931 entry in the pages read: Yokne'am (Haifa p. 97), HaZorea
(between "Haifa Suburbs" and "Hosha", p. 91), Sede Ya'aqov by its own name (between "Sarkas" and
"Shafa 'Amr", p. 95), and on the Beisan pages 77 to 81 Gesher, Nir David, Tirat Zevi and Sede Nahum. "Mahane Yisrael" (Nazareth,
printed p. 74) is in the census with 38 persons, all Moslems (17 + 21). The Jezreel rows of the
Beisan and Nazareth sub-districts were read in full on the pages named; the Haifa pages 88 to 90
were not, so a missing Haifa name proves nothing.

## 3. Places that stood before 15 May 1948 and are not CBS localities today

These are the rows the issue calls renames and abandonment. Each is recorded with its source; nothing
is proposed about how it appears in the game. [checked 2026-10-23: no CBS row matches `גבעות זייד`,
`חרמונים`, `בני ברית` or `תל עמל` as a separate locality; Tel Amal is the former name of Nir David
(part 2).]

### Giv'ot Zeid (the place of Bet Zeid)
- See Bet Zeid in part 2: the Zeid family settled the hills in **1926**, the place Giv'ot Zeid is named for Alexander Zeid (killed 11 July 1938; the article gives no naming year) and is in the 1945 Haifa table as "Giv‘ot
  Zeid (previously part of Esh Sheikh Bureik)"; the family left in **1951** and founded Bet Zeid
  (he.wiki `בית זייד`, oldid **42326268**). [S, Wikipedia] (24 Oct) The place has its own he.wiki
  article, `גבעות זייד` (oldid 43158155): a kibbutz of 1940, dissolved 1957, with the coordinate
  32.70133333, 35.115 [S, Wikipedia], 1,399 m west of CBS's Bet Zeid point (**computed here**); details
  in part 2, Bet Zeid. The family left in 1951 is the he.wiki `בית זייד` account; the kibbutz's end in
  1957 is the other.

### Benei Berit (the name of Moledet in 1945)
- See Moledet in part 2: the 1945 Beisan table gives "Benei Berit" for the unit "previously Moledet";
  he.wiki gives a forced agreement on that name in July 1940. Not a separate place. [S, Wikipedia]

### Hermonim (the 1942 group of Hamadya)
- See Hamadya in part 2. The 1945 unit names "Hermonim" with "previously Irgun Deror" in the text
  layer; he.wiki names the 1942 group "חרמונים". Not followed further. [GAP]
- (24 Oct) The 1945 Beisan table image (printed p. 43) prints the whole parenthesis: "Hermonim was
  previously Irgun Deror", so the group had an earlier name in the table's own words; see Hamadya in
  part 2 for the figures. The first Hamadya, 23 June 1939, and its abandonment are in part 2. [GAP]
  for the year the first site was left.

### Kefar Yeladim (24 Oct)
- Census of Palestine 1931, Nazareth sub-district, printed p. 74: "Kefar Yeladim", 18 houses, 126
  persons, Jews 48 + 77 = 125 and one Moslem (houses read at 100 dpi); Hebrew `כפר ילדים`, with a sub-entry "Central
  Hospital". Village Statistics 1945, Nazareth, printed p. 62: no population; land Jews 784, public
  34, total 818.
- he.wiki `כפר ילדים`, oldid **43262452**: "מוסד חינוכי ליתומים יהודים שהובאו ממזרח אירופה ... שפעל
  ב'חוות בלפוריה', בשנים 1923-1931"; "בשנת 1931 נסגר כפר הילדים". Afterwards a workers' children's
  school, a wartime hospital and army base; "במאי 1948 ... הוקם במקום בסיס של חטיבת גולני". [S,
  Wikipedia] The API coordinate is 32.622731, 35.318901, **computed here:** 1,709 m from CBS's Afula
  point (643 m north-south, 1,584 m east-west). **Our reading:** an orphans' institution and school
  on the Balfurya farm, not a farming locality; the lead of the first pass is closed as "not a
  locality of this note". It would only matter if the owner counts institutions.

### Mahane Yisrael (24 Oct)
- Census of Palestine 1931, printed p. 74: "Mahane Yisrael", 5 houses (100 dpi), 38 persons, all
  Moslems (17 + 21). Village Statistics 1945, printed p. 62: no population; land Arabs 390, Jews
  3,497, public 140, total 4,027. **Our reading:** a land unit with Jewish-owned land and no Jewish
  residents in 1945, and Moslem residents in 1931; not a Jewish locality of this note. Both it and
  Kefar Yeladim are in the Nazareth table, so the Galilee note, which the first pass thought
  might cover them, does not need to.

### Irgun Borokhov (24 Oct)
- Village Statistics 1945, Beisan, printed p. 44 (pdf page 44): "Tira, Et and Irgun Borokhov",
  population Arabs 150, Jews 50, total 200; land Arabs 4,463, Jews 2,604, public 3,140, total 10,207.
  A Jewish group of about 50 people on Jewish-owned land in 1945; the Arab village Et Tira is the
  other part of the unit. Who they were and when they came is [GAP].
- Searched again and not found, 2026-10-24. Query `ארגון בורוכוב טירה עמק בית שאן` and query `בורוכוב
  מושב עמק בית שאן 1945 טירת צבי`, against the he.wiki search API
  `https://he.wikipedia.org/w/api.php?action=query&list=search&srsearch=<query>&srlimit=10&format=json&formatversion=2`
  (current revisions, no date range): 8 and 4 results. The first returned `ניר דוד (תל-עמל)`, `רחל
  ינאית בן-צבי`, `צבי הירש קלישר`, `מלחמת העצמאות`, two local-elections articles, `רעננה` and
  `כרונולוגיית ההתיישבות היהודית בארץ ישראל בעת החדשה`. Opened: the wikitext of `ניר דוד (תל-עמל)`
  (oldid 44069100) and of the chronology (oldid 43572242): `בורוכוב` occurs once in the first, in the founders
  paragraph ("בשנת 1932 הם הקימו בין שכונת בורוכוב בגבעתיים לשכונת נחלת יצחק את קיבוץ ארצישראלי א'"), a
  neighbourhood of Givatayim and not Irgun Borokhov, and not at all in the chronology;
  the `רחל ינאית בן-צבי` article (oldid 43739439) has the word in a biography line about Ber
  Borochov, not about a locality, and was not read further. [GAP]

### El Ro-i (24 Oct, a new lead)
- Village Statistics 1945, Haifa, printed p. 47: "El Ro-i (previously part of Esh Sheikh Bureik)",
  population Jews 360, total 360; land Jews 569, public 85, total 654. A Jewish unit of 360 people
  that is not a CBS row under that name, in the same Esh Sheikh Bureik area as Giv'ot Zeid and
  Sede Ya'aqov. Hebrew name, founder and dates are [GAP]; it is outside the Jezreel sub-district
  by the 1945 table (Haifa) and is listed so that the Haifa and Carmel note can pick it up.

### Shimron and Timorim (24 Oct)
- The 1945 unit "Nahalal and Shimron" (Nazareth, printed p. 62) has population Jews 1,090 and land
  Jews 8,023, public 302, total 8,325. Gvati's running text says a kibbutz Timorim went up "על גבעת
  שִמרוֹן בקרבת מקום לנהלל" in the year to the end of תש״ח and later moved south; he.wiki `תימורים`,
  oldid **43704902**: "מושב תימורים הוקם במקור ב-21 ביוני 1948 כקיבוץ על גבעה ליד תל שימרון". [S,
  Wikipedia] 21 June 1948 is after 15 May 1948, so Timorim is left out by the date rule of this
  note. Shimron as a pre-1948 locality is not shown; the 1945 unit is the land of Nahalal.

## 4. Gaps and what was not done

- [GAP] **Completeness of part 3.** Part 3 lists only the places that the CBS rows' own histories
  and the 1945 land units name. It is not shown to be complete. (24 Oct) The leads of the first pass
  were followed: Kefar Yeladim (an institution, 1923 to 1931), Mahane Yisrael (no Jewish residents in
  1931 or 1945) and Shimron (Timorim, 21 June 1948, after the cut-off) are closed as not localities
  of this note; Irgun Borokhov (a Jewish group of 50 in 1945) stays open, and "El Ro-i" is a new lead
  in the Haifa sub-district. The Haifa pages 88 to 90 and Tiberias pages 82, 85 and 86 of the 1931
  census were not read, so other 1931 names may be missing.
- [GAP] **Census of Palestine 1931**: (24 Oct) read as page images for the rows of this note (see
  How it was done and the table in part 2); the pages not read are named there.
- [GAP] **Coordinates of moved, renamed and merged places**: (24 Oct) Giv'ot Zeid now has one (he.wiki,
  32.70133333, 35.115) and Kefar Yeladim one (he.wiki, 32.622731, 35.318901), both from Wikipedia and
  none from a map sheet (Survey of Palestine sheets and GovMap were not opened). Moledet and Hamadya
  (first site) have none apart from the present village. The 1939 Hamadya site, the early Merhavya
  sites and Irgun Borokhov have none.
- [GAP] **Rename and abandonment years**: (24 Oct) Merhavya's conversions are 1924 (moshav) and 1929
  (kibbutz beside it), he.wiki; Hamadya's first site is dated 23 June 1939 and its abandonment year is
  not found; Moledet has no single restoration year, the written form shifts in the early 1950s per
  he.wiki, and the *Davar* item it cites was not opened; Giv'ot Zeid was dissolved in 1957 per he.wiki
  and Gvati's sentence.
- [GAP] **Gvati rows**: Afula, Geva and Bet Zeid have none (searched again 2026-10-24, see "Geva and
  Afula, no Gvati row"), so their year rests on CBS with Wikipedia beside it.
- **Gvati day versus CBS year.** Where they agree on the year the row is [V]; the day rests on Gvati
  alone, and as `settlements-1949-51.md` notes, it can be a day or more off other sources, as the
  six rows in part 2 show.
- **Not done:** no hex, no game date, no rule for which of two dates the game should use. Those
  belong to #61 and to the owner's sensitivity review.
