# Jewish localities founded in the coastal plain and the Sharon before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`.
Refs #97, its coastal plain and Sharon region (claimed in
https://github.com/eyalgolan/statecraft-israel/issues/97#issuecomment-6361617595). The dated
settlements file and `tools/settlement_hexes.py` stay with #61, and rows with a rename or an
abandonment date need the owner's sensitivity review before they enter the game; none is proposed
here. Method, sources and tags are those of the Galilee note (#1429), the Haifa and Carmel note
(#1435) and the Negev note (#1403).

## How it was done

- Every page was fetched on 23 October 2026 with the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024,
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23),
  columns `שם נפה`, `אזור טבעי`, `דת יישוב`, `שנת ייסוד`, `שם יישוב באנגלית` and `קואורדינטות`. The
  twelve-digit coordinate is read as an Israel Transverse Mercator easting and northing of six
  digits each and converted with `pyproj` (EPSG:2039 to EPSG:4326), the reading
  `settlements-1949-51.md`, #1403, #1429 and #1435 use. The file does not state that reading, so
  every lat/lon below is **computed here** from the CBS string, not printed by CBS.
- **Founding dates.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix, Ben-Yehuda Project
  edition, https://benyehuda.org/read/14684 (checked 2026-10-23). Each row gives name, geographic
  region, date of founding, affiliation (kibbutzim and moshavim only) and the origin of the name.
  The section "מושבות וישובים כפריים" has no affiliation column, and the section "ישובים עירוניים
  שנוסדו כמושבות" prints a status (`עיר`, `מועצה מקומית`) in that place; the tables say which. The
  column does not say whether a date is the day of going onto the land or a later recognition.
  Dates are quoted exactly as printed, for example `2.12.1907` or `12.32`.
- **Village Statistics 1945** (Government of Palestine), the archive.org scan
  https://archive.org/details/palestine-village-statistics-1945 and its text layer
  `Village Statistics 1945_djvu.txt` (checked 2026-10-23), Table I "Population and total land
  areas" for the **Jaffa, Ramle, Tulkarm, Gaza and Haifa sub-districts** (in 1945 the Hadera area
  was in the Haifa sub-district, Netanya and the Hefer valley in Tulkarm, and Gan Yavne in Gaza).
  It is used only as a **presence check**: a Jewish locality named in a 1945 land unit existed by
  then. It is not a founding source, and it lists land units, so several kibbutzim and moshavim
  are not named at all; "not listed by name" therefore proves nothing. On the first page of the
  Jaffa, Ramle and Tulkarm tables the text layer prints the names and the figures in separate
  runs, so no figure is read from those pages. Figures are given only where all three population
  columns (Arabs, Jews, Total) survive on one line. Land unit names are quoted as the text layer
  prints them, with [sic] where it plainly misreads the scan.
- **Census of Palestine 1931**: not used. #1429 found that the archive.org scan has no usable text
  layer. [GAP]
- **Hebrew Wikipedia** was read through the MediaWiki API as the wikitext of the current revision,
  cited by `oldid`. It is a lead and a cross-check, not an authority; a fact resting on it alone is
  marked **[S, Wikipedia]**. Quotes keep the source's wording with link and bold markup and niqqud removed;
  double quote marks inside a Hebrew quote are printed as single quotes; gershayim in Hebrew dates
  and abbreviations (for example תרפ"ט) are kept as printed. Where an article cites a newspaper
  item or a book that was not opened here, the note says so.

**Tags**, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source. For
  a founding year that is CBS and Gvati; CBS's coordinates are **computed here** (see above).
- **[S] single source**: one source, or several that plainly copy one another. A fact resting only
  on Hebrew Wikipedia is **[S, Wikipedia]**.
- **[C] conflicting**: sources disagree. Both values are given, each with its source, and
  **our reading** says which to use and why.
- **[GAP]** not found.
- **computed here** is arithmetic or a conversion done for this note, not a printed figure.
- **our reading** marks an interpretation and is not attributed to a source.

## What counts as the coastal plain and the Sharon here

There is no single boundary, so this note states its rule.

Included: every locality that the CBS file codes as Jewish (`דת יישוב` = 1) with `שנת ייסוד` before
1948 in the **Sharon, Petah Tikva, Ramla and Rehovot sub-districts** and the **Tel Aviv district**,
plus the rows of the **Hadera sub-district** in CBS natural region **324** (natural regions 321 and
322 are in the Haifa and Carmel note, #1435; region 323 has no row before 1948). That is 115 rows.
Tel Aviv-Yafo is coded `דת יישוב` = 4, with `שנת ייסוד` 1909; it is added as a 116th row, the year
being that of Tel Aviv, and Jaffa, the older town inside it, gets no founding date. Mikve Israel
(1870) and Petah Tikva (1878) are older than 1882 but are kept, since the issue asks for every
Jewish locality founded before 15 May 1948. Places that stood in the same area before 15 May 1948
and were later abandoned or merged into another locality are in part 3.

Excluded:
- The **Ashkelon sub-district** (the southern coastal plain), left for its own note; its rows in
  the Negev note (#1403) are not counted again.
- The Carmel coast and the Zikhron Ya'akov area of the Hadera sub-district, in #1435.
- By date: the CBS 1948 rows of these sub-districts. Gvati dates all of the following after
  15 May 1948: Nezer Sereni `20.6.48`, Regavim `8.7.48`, Yesodot `16.7.48`, Bene Atarot
  `7.8.48`, Udim `29.8.48`, Nehalim `9.9.48`, Nordiyya `9.10.48`, Ge'alya `4.12.48`, Mishmar David
  `7.12.48` and Bet El'azari `27.12.48`. The CBS 1948 rows with no Gvati row (Azor, Bet Dagan,
  Yehud-Monosson, Midreshet Ruppin, Newe Avot, Qiryat Eqron, and Lod and Ramla, coded
  `דת יישוב` = 4) were not examined further. Be'erot Yitzhak (CBS 1948) is in the Negev note.

## 1. The CBS localities, one row each

Read every row as: CBS name (CBS English name); CBS locality code; CBS founding year; the
coordinate computed from the CBS string; Gvati's row (region, date as printed, affiliation or
status); the 1945 land unit that names it, with its sub-district table, if any; the tag. All CBS
cells come from bycode2024.xlsx, all Gvati cells from https://benyehuda.org/read/14684, all 1945
cells from the archive.org text layer cited above, each checked 2026-10-23. Rows tagged "part 2"
have a disagreement, a rename, a move, an abandonment or a single source written out below.

### Sharon sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| נתניה (Netanya) | 7400 | 1929 | 32.31609, 34.86253 | [GAP] no row | Tulkarm: "Natanya (Urban)" | [S] CBS only for the year; part 2 |
| תל מונד (Tel Mond) | 154 | 1929 | 32.25279, 34.91737 | השרון הדרומי, `1929`, (no affiliation column) | Tulkarm: "Tel Mond" | [V] |
| חירות (Herut) | 162 | 1930 | 32.24028, 34.91465 | השרון הדרומי, `1930`, תנועת המושבים | Tulkarm: "Herut" | [V] |
| עין ורד (En Wered) | 157 | 1930 | 32.26467, 34.93182 | השרון הדרומי, `1.5.30`, תנועת המושבים | Tulkarm: "‘Ein Vared" | [V] |
| עין החורש (En HaHoresh) | 167 | 1931 | 32.38596, 34.93967 | עמק חפר, `11.31`, הקיבוץ הארצי | Tulkarm: "‘Ein ha Horesh" | [V] |
| אביחיל (Avihayil) | 175 | 1932 | 32.35084, 34.87119 | עמק חפר, `19.7.32`, תנועת המושבים | Tulkarm: "Avihayil" | [V]; merged with Ein HaOved 1946, part 2 |
| אבן יהודה (Even Yehuda) | 182 | 1932 | 32.27106, 34.88578 | השרון הדרומי, `12.32`, (no affiliation column) | Tulkarm: "Even Yehuda" | [V] |
| גבעת חיים (איחוד) (Giv'at Hayyim (Ihud)) | 2018 | 1932 | 32.39941, 34.93216 | עמק חפר, `14.7.32`, האיחוד | Tulkarm: "Giv‘at Haiyim" | [C] he.wiki 1952; part 2 |
| גבעת חיים (מאוחד) (Giv'at Hayyim(Me'uhad)) | 173 | 1932 | 32.39144, 34.93077 | עמק חפר, `14.7.32`, הקיבוץ המאוחד | Tulkarm: "Giv‘at Haiyim" | [V]; part 2 |
| כפר יונה (Kefar Yona) | 168 | 1932 | 32.31541, 34.93330 | השרון, `23.1.32`, (no affiliation column) | Tulkarm: "Kefar Yona" | [V] |
| כפר יעבץ (Kefar Ya'bez) | 170 | 1932 | 32.27456, 34.96550 | השרון הדרומי, `11.32`, הפועל המזרחי | Tulkarm: a unit ending "Khirbat Takla and Kefar Ya‘vetz" | [V]; evacuated 1948, part 2 |
| אלישיב (Elyashiv) | 204 | 1933 | 32.38087, 34.90967 | עמק חפר, `14.11.33`, התאחדות האכרים | Tulkarm: "Elyashiv" | [V] |
| בית חירות (Bet Herut) | 877 | 1933 | 32.37907, 34.86759 | עמק חפר, `1933`, תנועת המושבים | not listed by name | [V] |
| בית ינאי (Bet Yannay) | 200 | 1933 | 32.38133, 34.86346 | עמק חפר, `15.10.33`, האיחוד החקלאי | Tulkarm: "Bett Yannai" [sic] | [V] |
| הדר עם (Hadar Am) | 191 | 1933 | 32.34904, 34.90118 | עמק חפר, `15.3.33`, האיחוד החקלאי | not listed by name | [V] |
| חגלה (Hogla) | 205 | 1933 | 32.38703, 34.92624 | עמק חפר, `14.11.38`, תנועת המושבים | Tulkarm: "Hogla" | [C] Gvati 1938; part 2 |
| חיבת ציון (Hibbat Ziyyon) | 219 | 1933 | 32.39739, 34.91239 | עמק חפר, `3.33`, התאחדות האיכרים | Tulkarm: "Hibbat Zion" | [V] |
| כפר הס (Kefar Hess) | 187 | 1933 | 32.24629, 34.93387 | השרון הדרומי, `5.1.33`, תנועת המושבים | Tulkarm: "Kefar Hess" | [V] |
| כפר ויתקין (Kefar Vitkin) | 190 | 1933 | 32.38159, 34.87782 | עמק חפר, `7.4.33`, תנועת המושבים | Tulkarm: "Kefar Vitkin" | [V] |
| כפר חיים (Kefar Hayyim) | 193 | 1933 | 32.35419, 34.90030 | עמק חפר, `16.6.33`, תנועת המושבים | Tulkarm: "Kefar Haiyim" | [V] |
| מעברות (Ma'barot) | 197 | 1933 | 32.36364, 34.90491 | עמק חפר, `15.8.33`, הקיבוץ הארצי | Tulkarm: "Ma'‘barot" [sic] | [V] |
| משמר השרון (Mishmar HaSharon) | 194 | 1933 | 32.35661, 34.90349 | עמק חפר, `16.6.33`, האיחוד | Tulkarm: "Mishmar hash Sharon" | [V] |
| קדימה-צורן (Qadima- Zoran) | 195 | 1933 | 32.28038, 34.91979 | השרון הדרומי, `5.7.33`, (no affiliation column) | Tulkarm: "Qadima" | [V]; merged with Tzoran 2003, part 2 |
| כפר הרא"ה (Kefar HaRo'e) | 217 | 1934 | 32.39062, 34.91182 | עמק חפר, `5.1.34`, הפועל המזרחי | Tulkarm: "Kefar ha Ro-e" | [V] |
| חבצלת השרון (Havazzelet HaSharon) | 235 | 1935 | 32.36061, 34.86275 | עמק חפר, `4.35`, האיחוד החקלאי | Tulkarm: "Havatselet hash Sharon" | [V]; first named Nahalat Yehoshua, part 2 |
| כפר ידידיה (Kefar Yedidya) | 233 | 1935 | 32.34563, 34.89841 | עמק חפר, `26.4.35`, תנועת המושבים | Tulkarm: "Yedidya" | [V] |
| שפיים (Shefayim) | 232 | 1935 | 32.21744, 34.82487 | השרון הדרומי, `1.3.36`, הקיבוץ המאוחד | Jaffa: "Shefayim" | [C] Gvati 1936; part 2 |
| ביתן אהרן (Bitan Aharon) | 252 | 1936 | 32.36514, 34.87017 | עמק חפר, `7.36`, האיחוד החקלאי | Tulkarm: "Bitan" | [V] |
| רשפון (Rishpon) | 247 | 1936 | 32.20054, 34.82432 | השרון הדרומי, `11.2.36`, תנועת המושבים | Jaffa: "Rishpon" | [V]; first named Rishpona, part 2 |
| צור משה (Zur Moshe) | 276 | 1937 | 32.29772, 34.91386 | השרון הדרומי, `13.9.37`, תנועת המושבים | Tulkarm: "Qalansuwa and Tsur Moshe" | [V] |
| בית יהושע (Bet Yehoshua) | 288 | 1938 | 32.26011, 34.86558 | השרון הדרומי, `17.8.38`, העובד הציוני | Tulkarm: "Ghabat Kafr Sir, Beit Yehoshua‘, Kfar Netter and Tel Yitshaq" [sic] | [V] |
| תל יצחק (Tel Yizhaq) | 287 | 1938 | 32.25302, 34.86806 | השרון הדרומי, `25.7.38`, העובד הציוני | Tulkarm: same unit as Beit Yehoshua | [V] |
| כפר נטר (Kefar Netter) | 316 | 1939 | 32.27539, 34.87158 | השרון הדרומי, `29.7.39`, (affiliation cell blank) | Tulkarm: same unit as Beit Yehoshua | [V] |
| בית יצחק-שער חפר (Bet Yizhaq-Sh. Hefer) | 326 | 1940 | 32.33421, 34.89077 | עמק חפר, `28.3.40`, האיחוד החקלאי | Tulkarm: "Beit Yits-haq" | [V] |
| כפר עבודה (Kefar Avoda) | 875 | 1942 | 32.25646, 34.94084 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |
| פרדסייה (Pardesiyya) | 171 | 1942 | 32.30595, 34.91051 | [GAP] no row | not listed by name | [C] he.wiki 1938; part 2 |
| בית הלוי (Bet HaLewi) | 373 | 1945 | 32.35370, 34.93139 | עמק חפר, `12.8.45`, תנועת המושבים | not listed by name | [V] |
| גאולים (Ge'ulim) | 379 | 1945 | 32.29796, 34.94546 | השרון הדרומי, `2.11.45`, תנועת המושבים | not listed by name | [V] |
| המעפיל (HaMa'pil) | 377 | 1945 | 32.37834, 34.98270 | עמק חפר, `2.11.45`, הקיבוץ הארצי | not listed by name | [V] |
| מכמורת (Mikhmoret) | 382 | 1945 | 32.40629, 34.87337 | עמק חפר, `9.11.45`, תנועת המושבים | not listed by name | [V] |
| קריית שלמה (Qiryat Shelomo) | 412 | 1945 | 32.23423, 34.86147 | [GAP] no row | not listed by name | [C] he.wiki 1949; part 2 |
| בני דרור (Bene Deror) | 386 | 1946 | 32.26219, 34.89908 | השרון הדרומי, `12.5.46`, תנועת המושבים | not listed by name | [V] |
| בצרה (Bazra) | 389 | 1946 | 32.21289, 34.87813 | השרון הדרומי, `1.8.46`, האיחוד החקלאי | not listed by name | [V] |
| כפר מונש (Kefar Monash) | 387 | 1946 | 32.34744, 34.91613 | עמק חפר, `16.5.46`, תנועת המושבים | not listed by name | [V] |
| משמרת (Mishmeret) | 425 | 1946 | 32.22811, 34.92017 | השרון הדרומי, `2.8.46`, תנועת המושבים | not listed by name | [V] |
| בני ציון (Bene Ziyyon) | 418 | 1947 | 32.21897, 34.86842 | השרון הדרומי, `26.3.47`, האיחוד החקלאי | not listed by name | [V] |
| גאולי תימן (Ge'ule Teman) | 872 | 1947 | 32.39226, 34.90165 | עמק חפר, `27.8.47`, הפועל המזרחי | not listed by name | [C] he.wiki 1948; part 2 |
| העוגן (HaOgen) | 423 | 1947 | 32.36221, 34.92402 | עמק חפר, `27.8.47`, הקיבוץ הארצי | not listed by name | [V] |
| חרב לאת (Herev Le'Et) | 422 | 1947 | 32.40186, 34.91678 | השרון הצפוני, `3.7.47`, האיחוד החקלאי | not listed by name | [V] |
| יקום (Yaqum) | 417 | 1947 | 32.24889, 34.84203 | השרון הדרומי, `11.3.47`, הקיבוץ הארצי | not listed by name | [V] |

### Hadera sub-district, natural region 324

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| חדרה (Hadera) | 6500 | 1890 | 32.43555, 34.91834 | השרון הצפוני, `1890`, (status column: עיר) | Haifa: "Hadera (Urban)" (Arabs 20, Jews 7,810, Total 7,830) | [V] |
| בנימינה-גבעת עדה (Binyamina-Giv'at Ada) | 9800 | 1903 | 32.52151, 34.96205 | רמת מנשה, `25.11.1903`, (no affiliation column) | Haifa: "Binyamina" (Arabs 270, Jews 1,250, Total 1,520) and "Giv‘at ‘Ada (El Marah)" | [V] for Giv'at Ada; Binyamina 1922, part 2 |
| גן שמואל (Gan Shemu'el) | 72 | 1913 | 32.45218, 34.95056 | השרון הצפוני, `1913`, הקיבוץ הארצי | not listed by name | [V] |
| פרדס חנה-כרכור (Pardes Hanna-Karkur) | 7800 | 1913 | 32.47311, 34.97562 | השרון הצפוני, `1913`, (no affiliation column) | Haifa: "Pardes Hanna" (Arabs 670, Jews 2,300, Total 2,970) | [V] for Karkur; Pardes Hanna 1929, part 2 |
| עין שמר (En Shemer) | 139 | 1927 | 32.46370, 35.00713 | השרון הצפוני, `1.6.27`, הקיבוץ הארצי | not listed by name | [V] |
| כפר פינס (Kefar Pines) | 189 | 1933 | 32.48394, 35.00237 | השרון הצפוני, `19.4.33`, הפועל המזרחי | not listed by name | [V] |
| משמרות (Mishmarot) | 213 | 1933 | 32.48665, 34.98432 | השרון הצפוני, `1933`, האיחוד | not listed by name | [V] |
| גן השומרון (Gan HaShomeron) | 225 | 1934 | 32.46264, 34.99840 | השרון הצפוני, `11.11.34`, האיחוד החקלאי | not listed by name | [V] |
| עין עירון (En Iron) | 223 | 1934 | 32.48350, 35.00927 | השרון הצפוני, `9.10.34`, תנועת המושבים | not listed by name | [V] |
| כפר גליקסון (Kefar Glikson) | 310 | 1939 | 32.50554, 35.00435 | השרון הצפוני, `23.5.39`, העובד הציוני | not listed by name | [V]; first named Lamakor, part 2 |
| שדות ים (Sedot Yam) | 327 | 1940 | 32.49150, 34.89280 | השרון הצפוני, `2.5.40`, הקיבוץ המאוחד | not listed by name | [V] |
| מענית (Ma'anit) | 344 | 1942 | 32.45670, 35.02732 | השרון הצפוני, `6.9.42`, הקיבוץ הארצי | not listed by name | [V] |

### Petah Tikva sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| פתח תקווה (Petah Tiqwa) | 7900 | 1878 | 32.08505, 34.88632 | השרון הדרומי, `1878`, (status column: עיר) | Jaffa: "Petah Tiqva (Urban)" (Arabs 150, Jews 17,100, Total 17,250) | [V]; pre-1882, part 2 |
| כפר סבא (Kefar Sava) | 6900 | 1903 | 32.18119, 34.91280 | השרון הדרומי, `1905`, (status column: עיר) | Jaffa: "Kefar Sava (Urban)" | [C] Gvati 1905; part 2 |
| רעננה (Ra'annana) | 8700 | 1921 | 32.18562, 34.87002 | השרון הדרומי, `1921`, (status column: מועצה מקומית) | Jaffa: "Ra‘anana (Urban)" | [V] |
| כפר מל"ל (Kefar Malal) | 98 | 1922 | 32.16808, 34.89400 | השרון הדרומי, `1922`, תנועת המושבים | Jaffa: "Kefar Malal" | [V]; first founded 1912 as Ein Hai, part 2 |
| הוד השרון (Hod HaSharon) | 9700 | 1924 | 32.15219, 34.89413 | השרון הדרומי, `1924`, (no affiliation column) | Jaffa: "Magdiel", "Ramatayim" and "Hadar", see part 2 | [V]; four places merged 1964, part 2 |
| גבעת השלושה (Giv'at HaShelosha) | 870 | 1925 | 32.09842, 34.92159 | השרון הדרומי, `1.5.25`, הקיבוץ המאוחד | not listed by name | [V] |
| עינת (Enat) | 871 | 1925 | 32.08285, 34.93755 | השרון הדרומי, `1.5.52`, האיחוד | not listed by name | [C] Gvati and he.wiki 1952; part 2 |
| גת רימון (Gat Rimmon) | 128 | 1926 | 32.06843, 34.88024 | פתח-תקוה, `2.7.26`, (no affiliation column) | Jaffa: "Gat Rimmon" | [V]; first named HaTehiya, part 2 |
| ירקונה (Yarqona) | 183 | 1932 | 32.14520, 34.89913 | השרון הדרומי, `1932`, תנועת המושבים | Jaffa: "Yargona" [sic] | [V]; first named Irgun Jidru, part 2 |
| רמת הכובש (Ramat HaKovesh) | 184 | 1932 | 32.21731, 34.93796 | השרון הדרומי, `1932`, הקיבוץ המאוחד | Tulkarm: "Ramat hak Kovesh" | [V] |
| גבעת ח"ן (Giv'at Hen) | 207 | 1933 | 32.16764, 34.87441 | השרון הדרומי, `11.33`, תנועת המושבים | Jaffa: "Giv‘at Hen" | [V] |
| צופית (Zofit) | 198 | 1933 | 32.19287, 34.92106 | השרון הדרומי, `19.9.33`, תנועת המושבים | Tulkarm: "Tsofit" | [V] |
| רמות השבים (Ramot HaShavim) | 206 | 1933 | 32.16653, 34.88468 | השרון הדרומי, `19.11.33`, האיחוד החקלאי | not listed by name | [V] |
| גני עם (Ganne Am) | 218 | 1934 | 32.15136, 34.90151 | השרון הדרומי, `3.34`, (affiliation cell blank) | not listed by name | [V] |
| גן חיים (Gan Hayyim) | 239 | 1935 | 32.19507, 34.90564 | השרון הדרומי, `25.8.35`, תנועת המושבים | Tulkarm: "Gan Haiyim" | [V] |
| מעש (Ma'as) | 230 | 1935 | 32.06464, 34.88972 | השרון הדרומי, `1935`, תנועת המושבים | not listed by name | [V] |
| כפר סירקין (Kefar Sirkin) | 249 | 1936 | 32.07693, 34.92420 | השרון הדרומי, `1.4.46`, תנועת המושבים | Ramle: "Kefar Sirkin" | [C] Gvati 1946; part 2 |
| שדה ורבורג (Sede Warburg) | 284 | 1938 | 32.20755, 34.90306 | השרון הדרומי, `1.4.38`, האיחוד החקלאי | Tulkarm: "Miska and Sede ‘Warburg" [sic] | [V] |
| גבעת שמואל (Giv'at Shemu'el) | 681 | 1942 | 32.07687, 34.85030 | [GAP] no row | not listed by name | [C] he.wiki 1944 to 1945; part 2 |
| בית ברל (Bet Berl) | 1076 | 1947 | 32.20108, 34.92627 | [GAP] no row | not listed by name | [C] cornerstone 1946; part 2 |

### Tel Aviv district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| מקווה ישראל (Miqwe Yisra'el) | 22 | 1870 | 32.02920, 34.78135 | [GAP] no row | Jaffa: "Miqve Yisrael" | [S] CBS only for the year; pre-1882, part 2 |
| תל אביב -יפו (Tel Aviv - Yafo) | 5000 | 1909 | 32.07638, 34.78933 | [GAP] no row | Jaffa: "Tel Aviv (Urban)" (Arabs 660, Jews 166,000, Total 166,660) | [S] CBS only for the year; part 2 |
| רמת גן (Ramat Gan) | 8600 | 1921 | 32.06886, 34.82417 | [GAP] no row | Jaffa: "Ramat Gan (Urban)" | [S] CBS only for the year; part 2 |
| גבעתיים (Giv'atayim) | 6300 | 1922 | 32.07136, 34.80994 | [GAP] no row | Jaffa: "Giv'atayim" | [S] CBS only for the year; part 2 |
| רמת השרון (Ramat HaSharon) | 2650 | 1923 | 32.13939, 34.84139 | השרון הדרומי, `1923`, (no affiliation column) | Jaffa: "Ramat hash Sharon" | [V] |
| בני ברק (Bene Beraq) | 6100 | 1924 | 32.08670, 34.83478 | [GAP] no row | Jaffa: "Benei Beraq (Urban)" | [S] CBS only for the year; part 2 |
| הרצלייה (Herzliyya) | 6400 | 1924 | 32.16984, 34.83295 | השרון הדרומי, `1924`, (status column: עיר) | Jaffa: "Herzliya (Urban)" | [V] |
| בת ים (Bat Yam) | 6200 | 1926 | 32.01974, 34.74991 | [GAP] no row | Jaffa: "Bat Yam (Urban)" | [S] CBS only for the year; first named Bayit VaGan, part 2 |
| כפר שמריהו (Kefar Shemaryahu) | 267 | 1937 | 32.18609, 34.82110 | השרון הדרומי, `1937`, (status column: מועצה מקומית) | not listed by name | [V] |
| קריית אונו (Qiryat Ono) | 2620 | 1939 | 32.06036, 34.85716 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |
| חולון (Holon) | 6600 | 1940 | 32.01647, 34.77891 | [GAP] no row | Jaffa: "Holon (Urban)" | [S] CBS only for the year; part 2 |
| גליל ים (Gelil Yam) | 346 | 1943 | 32.15719, 34.83077 | השרון הדרומי, `7.1.43`, הקיבוץ המאוחד | not listed by name | [V] |

### Ramla sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| באר יעקב (Be'er Ya'aqov) | 2530 | 1907 | 31.94202, 34.83904 | שפלת יהודה, `2.12.1907`, (no affiliation column) | Ramle: "Beer Ya‘aqov" | [V]; first named Be'er Shalom, part 2 |
| בן שמן (כפר נוער) (Ben Shemen(K.No'ar)) | 1084 | 1921 | 31.95944, 34.92739 | [GAP] no row | Ramle: "Ben Shemen", see part 2 | [C] he.wiki 1927; part 2 |
| חולדה (Hulda) | 160 | 1930 | 31.83139, 34.88255 | שפלת יהודה, `1930`, האיחוד | Ramle: "Hulda" | [V] |
| נען (Na'an) | 158 | 1930 | 31.88392, 34.85774 | שפלת יהודה, `10.3`, הקיבוץ המאוחד | not listed by name | [S] CBS only for the year; Gvati prints no year, part 2 |
| גזר (Gezer) | 370 | 1945 | 31.87672, 34.91983 | שפלת יהודה, `13.3.45`, האיחוד | not listed by name | [V] |

### Rehovot sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| ראשון לציון (Rishon LeZiyyon) | 8300 | 1882 | 31.96899, 34.79975 | מישור החוף, `1882`, (status column: עיר) | Ramle: "Rishon le Zion (Urban)" | [V] |
| מזכרת בתיה (Mazkeret Batya) | 28 | 1883 | 31.85096, 34.84359 | דרום, `7.11.1889`, (no affiliation column) | Ramle: "Mazkeret Batyah (‘Eqron)" | [C] Gvati 1889; part 2 |
| נס ציונה (Nes Ziyyona) | 7200 | 1883 | 31.92631, 34.79784 | דרום, `1880`, (status column: מועצה מקומית) | Ramle: "Wadi Hunein (Nes Tsiyona)" | [C] Gvati 1880; part 2 |
| גדרה (Gedera) | 2550 | 1884 | 31.81204, 34.78022 | דרום, `1884`, (status column: מועצה מקומית) | Ramle: "Gedera" | [V] |
| רחובות (Rehovot) | 8400 | 1890 | 31.89168, 34.80789 | דרום, `1890`, (status column: עיר) | Ramle: "Rehovot (Urban)" | [V] |
| גן שלמה (Gan Shelomo) | 144 | 1927 | 31.87814, 34.79876 | דרום, `14.10.27`, האיחוד | Ramle: "Gan Shelomo" | [V] |
| גבעת ברנר (Giv'at Brenner) | 147 | 1928 | 31.86730, 34.80092 | דרום, `23.6.28`, הקיבוץ המאוחד | Ramle: "Giv‘at Brenner" | [V] |
| בית חנן (Bet Hanan) | 159 | 1930 | 31.93366, 34.77329 | דרום, `12.30`, תנועת המושבים | Ramle: "Beit Hanan and Neta‘im" | [V] |
| עיינות (Ayanot) | 156 | 1930 | 31.91558, 34.76785 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |
| גן יבנה (Gan Yavne) | 166 | 1931 | 31.78833, 34.70844 | דרום, `10.31`, (no affiliation column) | Gaza: "Gan Yavne and Kefar Bitsaron" | [V] |
| כפר ביל"ו (Kefar Bilu) | 177 | 1932 | 31.87279, 34.82562 | דרום, `22.10.32`, תנועת המושבים | Ramle: "Kefar Bilu" | [V] |
| נטעים (Neta'im) | 174 | 1932 | 31.94511, 34.77578 | מישור החוף, `15.7.32`, תנועת המושבים | Ramle: "Beit Hanan and Neta‘im" | [V]; first named Kefar Vatikim, part 2 |
| בית עובד (Bet Oved) | 202 | 1933 | 31.92183, 34.77355 | דרום, `15.10.33`, תנועת המושבים | Ramle: "Beit ‘Oved" | [V] |
| גיבתון (Gibbeton) | 196 | 1933 | 31.88887, 34.80000 | דרום, `1.8.33`, (affiliation cell blank) | Ramle: "Zarnuqa and Gibbeton" | [V] |
| קבוצת יבנה (Qevuzat Yavne) | 334 | 1941 | 31.81543, 34.71960 | מישור החוף, `29.10.41`, הפועל המזרחי | not listed by name | [V]; moved 1940 to 1941, part 2 |
| חפץ חיים (Hafez Hayyim) | 363 | 1944 | 31.78946, 34.80002 | דרום, `25.4.44`, פועלי אגודת ישראל | not listed by name | [V] |
| בית רבן (Bet Rabban) | 848 | 1946 | 31.81730, 34.72957 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |

## 2. Disagreements, renames, moves and single sources for CBS localities

Hebrew Wikipedia pages below were read at the `oldid` given, checked 2026-10-23. Rows are grouped
by sub-district, in the order of part 1.

### Sharon sub-district

#### Netanya (CBS 7400)
- CBS **1929**. Not in Gvati's appendix (a town). [checked 2026-10-23: no row]
- he.wiki `נתניה`, oldid **43931296**: "המקום נוסד על ידי קבוצת בני בנימין בשנת 1929"; "ב-18 בפברואר
  1929 הגיעו חמשת המתיישבים הראשונים". [S, Wikipedia]
- **[S]** year 1929, CBS, with he.wiki agreeing; first settlers 18 February 1929 [S, Wikipedia].

#### Avihayil and Ein HaOved (CBS 175)
- CBS **1932**; Gvati `19.7.32`. [V]
- he.wiki `אביחיל`, oldid **43881676**: "המושב הוקם בשנת 1932 וב-1946 אוחד עם המושב הסמוך 'עין
  העובד'". [S, Wikipedia] Village Statistics 1945 (Tulkarm) still names "Avihayil" and "‘Ein ha
  ‘Oved" as separate units.
- **Merger** with Ein HaOved in **1946** [S, Wikipedia]. Ein HaOved's founding date is a [GAP]; no
  Gvati or CBS row of that name.

#### Givat Haim Ihud and Givat Haim Meuhad (CBS 2018 and 173)
- CBS **1932** for both; Gvati `14.7.32` for both, `האיחוד` and `הקיבוץ המאוחד`.
- he.wiki `גבעת חיים מאוחד`, oldid **43696761**: "ב-14 ביולי 1932 עלו אנשי הקיבוץ לקרקע בעמק חפר".
  he.wiki `גבעת חיים איחוד`, oldid **44069165**: "הקיבוץ הוקם בשנת 1952 בעקבות הפילוג". [S,
  Wikipedia]
- **[C] Ihud:** CBS and Gvati 1932 against he.wiki 1952. **Our reading:** there was one kibbutz from
  14 July 1932, and the split into two came in 1952, after this era; Village Statistics 1945
  (Tulkarm) has one unit, "Giv‘at Haiyim". A pre-1948 list should carry one place, not two.

#### Kfar Yavetz (CBS 170)
- CBS **1932**; Gvati `11.32`. [V]
- he.wiki `כפר יעבץ`, oldid **42335605**: "תושבי המקום התימנים פונו מכפר יעבץ עם פרוץ הקרבות"; "ב-29
  ביוני 1948 עלה ליישב את כפר יעבץ גרעין 'מכורה' של הקיבוץ הדתי". [S, Wikipedia]
- **Evacuated** in the 1948 war; the day is not given in the passage [GAP]. Resettled by another
  group on 29 June 1948, after this era. A row for the owner's sensitivity review. The evacuees'
  new village is Geulei Teiman, below.

#### Hagla (CBS 205)
- CBS **1933**; Gvati `14.11.38`.
- he.wiki `חגלה (מושב)`, oldid **43512464**: "באביב של שנת 1933 הוקם צריף סככה ראשון על האדמה";
  "ב-14 בנובמבר 1933 הגיעו שלוש המשפחות הראשונות". [S, Wikipedia] Village Statistics 1945
  (Tulkarm) names "Hogla".
- **[C]** CBS and he.wiki 1933 against Gvati 1938. **Our reading:** use 14 November 1933; Gvati's
  day and month are the article's, so his year may be a misprint, but no source read here says so.

#### Kadima-Tzoran (CBS 195)
- CBS **1933**; Gvati `קָדִימָה`, `5.7.33`. [V] for Kadima.
- he.wiki `קדימה-צורן`, oldid **43894186**: the council "נוצרה בשנת 2003 בעקבות החלטה של הכנסת על
  איחודן של המועצות המקומיות של קדימה ושל צורן"; "צורן הוא יישוב קהילתי שהוקם בשנת 1992". [S,
  Wikipedia]
- **Merger** 2003, and Tzoran itself was founded in 1992, both after this era. The pre-1948 place is
  Kadima.

#### Havatzelet HaSharon, first named Nahalat Yehoshua (CBS 235)
- CBS **1935**; Gvati `4.35`. [V]
- he.wiki `חבצלת השרון (מושב)`, oldid **42335555**: "היישוב הוקם על ידי עולים מפולין בשנת 1935";
  "בתחילה נקרא המושב 'נחלת יהושע' על שם יהושע סופרסקי". [S, Wikipedia]
- **Rename** Nahalat Yehoshua to Havatzelet HaSharon; year not given. [GAP]

#### Shefayim (CBS 232)
- CBS **1935**; Gvati `1.3.36`.
- he.wiki `שפיים`, oldid **43556388**: "בשנת 1935 קבעו המוסדות המיישבים של התנועה הציונית את
  'אדמות אל-חראם' מצפון להרצליה כמקום הקבע של הפלוגה, וחבריה נשלחו לשם לעלות על הקרקע"; "בשנת
  1936, לאחר העלייה לקרקע, ועדת השמות של הקרן הקיימת החליטה לקרוא לנקודת ההתיישבות החדשה בשם
  שפיים". [S, Wikipedia]
- **[C]** CBS 1935 against Gvati 1 March 1936. **Our reading:** the article dates the decision to
  1935 and the naming to 1936, after the ascent, but gives no day for the ascent, so it does not
  settle which year that was. Keep both.

#### Rishpon (CBS 247)
- CBS **1936**; Gvati `11.2.36`. [V]
- he.wiki `רשפון`, oldid **43624312**: "המושב הוקם בשנת 1936"; "ועדת השמות קראה ליישוב תחילה
  'רשפונה'". [S, Wikipedia]
- **Rename** Rishpona to Rishpon; year not given. [GAP]

#### Kfar Avoda (CBS 875)
- CBS **1942**. Not in Gvati's appendix (an institution). [checked 2026-10-23: no row]
- he.wiki `כפר עבודה (תל מונד)`, oldid **44045940**: "יישוב מוסדי ... שנוסד בשנת 1942"; "המוסד נוסד
  במקור בשנת 1937 על אדמות הקרן הקיימת ליד קיבוץ גבת"; "המוסד פונה, עבר זמנית לחדרה, ובהמשך שנת
  1942 התמקם באופן קבוע באזור תל מונד סמוך לכפר הס". [S, Wikipedia]
- **[S]** year 1942, CBS, with he.wiki agreeing. **Move:** the institution began in 1937 near Gvat
  and came to its present site in 1942. **Our reading:** Gvat is in the Jezreel valley, so the 1937
  site is outside this region.

#### Pardesiya (CBS 171)
- CBS **1942**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `פרדסיה`, oldid **43161122**: "בשנת 1938 הוקם במקום יישוב של עולי תימן". [S, Wikipedia]
- **[C]** CBS 1942 against he.wiki 1938. **Our reading:** one source each, and neither says what the
  other's year marks. Keep both.

#### Kiryat Shlomo (CBS 412)
- CBS **1945**. Not in Gvati's appendix (an institution). [checked 2026-10-23: no row]
- he.wiki `קריית שלמה`, oldid **41801890**: "אדמות 'גן שלמה' נרכשו ב-1933"; "המוסד הוקם ב-1949
  כחלק מתוכנית מלב"ן". [S, Wikipedia]
- **[C]** CBS 1945 against he.wiki 1949. **Our reading:** the article puts the institution after
  15 May 1948; the row should not enter a pre-1948 list until a dated source settles it.

#### Geulei Teiman (CBS 872)
- CBS **1947**; Gvati `27.8.47`, `הפועל המזרחי`.
- he.wiki `גאולי תימן`, oldid **43486342**: "היישוב הוקם בשנת 1948 על ידי מפוני מושב כפר יעבץ שבאזור
  השרון אשר פונו כתוצאה מהקרבות באזור היישוב". [S, Wikipedia]
- **[C]** CBS and Gvati 1947 against he.wiki 1948. **Our reading:** two sources against one; use
  27 August 1947 and record that the article ties the place to the 1948 evacuation of Kfar Yavetz.

### Hadera sub-district, natural region 324

#### Binyamina-Giv'at Ada (CBS 9800)
- CBS **1903**; Gvati `גִבְעַת-עָדָה`, `25.11.1903`, and a separate row `בִּנְיָמִינָה`, `10.22`, both
  in the section with no affiliation column. [V] for Giv'at Ada.
- he.wiki `בנימינה`, oldid **43939072**: "מושבה שנוסדה דרומית לזכרון יעקב, מזרחית לאור עקיבא וצפונית
  לפרדס חנה ב־1922"; "בשנת 2003 אוחדו הרשויות המקומיות: בנימינה וגבעת עדה". [S, Wikipedia]
- **Our reading:** CBS's 1903 is Giv'at Ada's year. Binyamina is October 1922: Gvati `10.22`, with
  he.wiki agreeing on 1922 [S]. The two were merged in 2003, after this era. Village Statistics 1945
  (Haifa) has both,
  "Binyamina" (Arabs 270, Jews 1,250, Total 1,520) and "Giv‘at ‘Ada (El Marah)".

#### Kfar Glickson, first named Lamakor (CBS 310)
- CBS **1939**; Gvati `23.5.39`, `העובד הציוני`. [V]
- he.wiki `כפר גליקסון`, oldid **43721392**: "בתחילה נקרא 'קיבוץ למקור' ובשנת 1940 נקבע שייקרא על שם
  ד"ר משה יוסף גליקסון שנפטר ביום העלייה על הקרקע של היישוב". [S, Wikipedia]
- **Rename** Lamakor to Kfar Glickson in **1940** [S, Wikipedia].

#### Pardes Hanna-Karkur (CBS 7800)
- CBS **1913**; Gvati `1913`. [V] for Karkur.
- he.wiki `פרדס חנה-כרכור`, oldid **43840986**: "שנוצרה בשנת 1969 מאיחוד שתי המועצות המקומיות
  פרדס חנה (נוסדה ב-1929) וכרכור (נוסדה ב-1913)"; "בספטמבר 1913 נרכשו 4000 דונם אדמות
  'ע'ביה'". [S, Wikipedia]
- **Our reading:** CBS's and Gvati's 1913 is Karkur's; Pardes Hanna is 1929 [S, Wikipedia]; the
  merger, 1969, is after this era. Village Statistics 1945 (Haifa) names "Pardes Hanna" only.

### Petah Tikva sub-district

#### Kfar Sava (CBS 6900)
- CBS **1903**; Gvati `1905` (status column `עיר`).
- he.wiki `כפר סבא`, oldid **44060532**: "בתרס"ג (1903), הנחשבת כיום כשנת הקמת היישוב היהודי בכפר
  סבא, מכרה החברה את מקצת האדמות לאנשי ירושלים ואת רובן לאיכרי פתח תקווה"; "צורת יישוב ארעית זו
  נמשכה עד שנת 1912"; "בספטמבר 1918 כשכבשו האנגלים את כפר סבא, היא הייתה הרוסה לחלוטין"; and of
  May 1921, "כשחזרו התושבים לכפר סבא כעבור כמה שבועות מצאו כי כל הבתים נבזזו והועלו באש". [S,
  Wikipedia]
- he.wiki `כפר מל"ל`, oldid **43143351**: "במאי 1918 גירשו העות'מאנים את חברי המושב יחד עם תושבי כפר
  סבא לצפון הארץ"; "ב-3 במאי 1921 פרצו מאורעות תרפ"א וכל חברי כפר מל"ל וכפר-סבא פונו לפתח-תקווה
  הסמוכה". [S, Wikipedia]
- **[C]** CBS and he.wiki 1903 against Gvati 1905. **Our reading:** 1903 is the year of the land
  sale that the article says is counted as the founding; use 1903 and record Gvati's 1905, which no
  source read here explains. **Abandoned twice:** the residents were expelled in **May 1918** and
  evacuated on **3 May 1921**, returning a few weeks later. Rows for the owner's sensitivity review.

#### Kfar Malal, first founded as Ein Hai (CBS 98)
- CBS **1922**; Gvati `1922`. [V]
- he.wiki `כפר מל"ל`, oldid **43143351**: "כפר מל"ל נוסד ב-1912 כמושב פועלים"; "ושם היישוב הציוני
  הוסב לשם עין חי"; "בשנת 1914 הוחלט באספה הכללית של המושב לשנות את שמו לכפר מל"ל"; the expulsion of
  May 1918 and the evacuation of 3 May 1921 quoted under Kfar Sava; and "באוקטובר 1921 חזרו החברים
  לאזור והחלו לעבד את אדמותיהם". [S, Wikipedia]
- **Our reading:** 1922 is the re-founding. The place was first founded in **1912** as Ein Hai,
  **renamed** Kfar Malal in **1914**, **abandoned** in May 1918 and on 3 May 1921, and reoccupied from
  October 1921. All [S, Wikipedia] except the 1922 year. Rows for the owner's sensitivity review.

#### Hod HaSharon (CBS 9700)
- CBS **1924**; Gvati `1924`, in the section with no affiliation column. [V]
- he.wiki `הוד השרון`, oldid **43993434**: "היא נוסדה כמועצה מקומית ב-1 בדצמבר 1964 עם איחודן של
  ארבע מושבות שכנות: מגדיאל, רמתיים, כפר הדר ורמת־הדר"; "מגדיאל הוקמה בי"ב באב תרפ"ד, אוגוסט 1924".
  [S, Wikipedia]
- **Our reading:** CBS's and Gvati's 1924 is Magdiel's, the oldest of the four. The **merger** of
  1964 is after this era. The four places are in part 3.

#### Einat (CBS 871)
- CBS **1925**; Gvati `1.5.52`, `האיחוד`.
- he.wiki `עינת`, oldid **43774561**: "הקיבוץ הוקם בשנת 1952 על ידי חברי הקיבוצים גבעת השלושה ורמת
  הכובש"; "בתחילה נקרא הקיבוץ גבעת השלושה של איחוד הקבוצות והקיבוצים". [S, Wikipedia]
- **[C]** CBS 1925 against Gvati and he.wiki 1952. **Our reading:** two sources against one; use
  1952, after this era. CBS's 1925 is the year of Givat HaShlosha (CBS 870, Gvati `1.5.25`), the
  one of the two kibbutzim whose members founded Einat. The row should not enter a pre-1948 list as a
  place of its own.

#### Gat Rimon, first named HaTehiya (CBS 128)
- CBS **1926**; Gvati `2.7.26`. [V]
- he.wiki `גת רימון`, oldid **42332876**: "היישוב הוקם בשנת 1926 כמושב פועלים"; "בתחילה נקרא היישוב
  'התחייה' ומאוחר יותר הוחלף שמו לשם התנ"כי 'גת-רימון'". [S, Wikipedia]
- **Rename** HaTehiya to Gat Rimon; year not given. [GAP]

#### Yarkona, first named Irgun Jidru (CBS 183)
- CBS **1932**; Gvati `1932`. [V]
- he.wiki `ירקונה`, oldid **42877051**: "בתחילה נקרא המקום 'ארגון ג'ידרו', כשם הארגון שהתיישב
  במושב"; "בתחילת 1933 ניתן למושב השם 'ירקונה'". [S, Wikipedia]
- **Rename** Irgun Jidru to Yarkona in **early 1933** [S, Wikipedia].

#### Kfar Sirkin (CBS 249)
- CBS **1936**; Gvati `1.4.46`.
- he.wiki `כפר סירקין`, oldid **42332909**: "אדמותיו נקנו באוגוסט של שנת 1934"; "תושבי הכפר הראשונים
  עלו לקרקע בשנת 1936". [S, Wikipedia] Village Statistics 1945 (Ramle) already names "Kefar
  Sirkin".
- **[C]** CBS and he.wiki 1936 against Gvati 1946. **Our reading:** use 1936; two sources agree, and
  the 1945 table shows the place existed before Gvati's date. Gvati's `1.4.46` may be a re-founding or
  a misprint; no source read here says which.

#### Givat Shmuel (CBS 681)
- CBS **1942**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `גבעת שמואל`, oldid **43775216**: "בנובמבר 1944 החלו פעילים מרומניה בסיוע המשרד לשיכון
  עולים בהקמת היישוב שנקרא בפי מייסדיו גבעת שמואל"; "באפריל 1945 נכנסו לבתיהם ראשוני המתיישבים".
  [S, Wikipedia]
- **[C]** CBS 1942 against he.wiki November 1944 (building) and April 1945 (first residents).
  **Our reading:** one source each; the article's dates are specific and CBS's 1942 is not explained
  by any source read here. Keep both.

#### Beit Berl (CBS 1076)
- CBS **1947**. Not in Gvati's appendix (a college). [checked 2026-10-23: no row]
- he.wiki `בית ברל`, oldid **43764790**: "טקס הנחת אבן הפינה למוסד התקיים ביום השנה השני לפטירתו של
  ברל כצנלסון, 21 באוגוסט 1946". [S, Wikipedia]
- **[C]** CBS 1947 against a cornerstone of 21 August 1946. **Our reading:** a cornerstone is not a
  founding; CBS's 1947 may be the opening, but no source read here says so. Keep both.

#### Petah Tikva (CBS 7900)
- CBS **1878**; Gvati `1878`. [V] Older than 1882; kept under the rule above.
- he.wiki `פתח תקווה`, oldid **44024409**: "נוסדה כמושבה ב-1878 והוכרזה כעיר בשנת 1937". Its reading
  list includes a chapter titled "חידוש היישוב היהודי בפתח־תקווה על ידי עולי ביאליסטוק, 1883" (not
  opened here). [S, Wikipedia]
- **Our reading:** a renewal in 1883 implies the place was left before that; when, and for how
  long, is not in the passages read. [GAP] A row for the owner's sensitivity review.

### Tel Aviv district

#### Mikve Israel (CBS 22)
- CBS **1870**. Not in Gvati's appendix (an agricultural school). [checked 2026-10-23: no row]
- he.wiki `מקוה ישראל`, oldid **43915885**: "בית הספר הוקם לבסוף על ידי נטר בי"ד באדר א' תר"ל (15
  בפברואר 1870)"; "בעת מלחמת העולם הראשונה ננטש בית הספר". [S, Wikipedia]
- **[S]** year 1870, CBS, with he.wiki agreeing; day 15 February 1870 [S, Wikipedia]. Older than 1882;
  kept under the rule above. **Abandoned** during the First World War; no date given. [GAP]

#### Tel Aviv-Yafo (CBS 5000)
- CBS **1909**, `דת יישוב` = 4. Not in Gvati's appendix (a city). [checked 2026-10-23: no row]
- he.wiki `תל אביב-יפו`, oldid **44079227**: "תל אביב נוסדה רשמית ב-11 באפריל 1909 כשכונת אחוזת
  בית, כשם האגודה שייסדה אותה"; "ב-21 במאי 1910 שונה שם השכונה ל'תל אביב'"; and of 1949, "אוחדו
  מוניציפלית שתי הערים". [S, Wikipedia] Village
  Statistics 1945 (Jaffa) has "Tel Aviv (Urban)" (Arabs 660, Jews 166,000, Total 166,660) and Jaffa
  as a separate unit.
- **[S]** year 1909, CBS, with he.wiki agreeing; day 11 April 1909 [S, Wikipedia]. **Rename**
  Ahuzat Bayit to Tel Aviv on **21 May 1910** [S, Wikipedia]. The union with Jaffa (1949) is
  after this era.

#### Ramat Gan (CBS 8600)
- CBS **1921**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `רמת גן`, oldid **44017656**: "היא הוקמה ב-1921 כמושבה חקלאית, הוכרה כמועצה מקומית ב-1926".
  [S, Wikipedia]
- **[S]** year 1921, CBS, with he.wiki agreeing.

#### Givatayim (CBS 6300)
- CBS **1922**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `גבעתיים`, oldid **44022991**: "נוסדה ב-1922 והוכרזה כעיר בשנת 1959"; "השכונה נוסדה בשנת
  1922, ביוזמתו של דוד שניידרמן, איש העלייה השנייה, ונקראת על שמו של דב בורוכוב". [S, Wikipedia]
- **[S]** year 1922, CBS, with he.wiki agreeing. **Our reading:** the 1922 place is the Borochov
  neighbourhood; when the name Givatayim was adopted is not in the passages read. [GAP]

#### Bnei Brak (CBS 6100)
- CBS **1924**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `בני ברק`, oldid **44060376**: "בני ברק נוסדה בי"א בסיוון תרפ"ד (13 ביוני 1924) כמושבה
  חקלאית". [S, Wikipedia]
- **[S]** year 1924, CBS, with he.wiki agreeing; day 13 June 1924 [S, Wikipedia].

#### Bat Yam, first named Bayit VaGan (CBS 6200)
- CBS **1926**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `בת ים`, oldid **43939074**: "היא הוקמה בשנת 1926 כ'בית וגן' בחולות שמדרום ליפו"; "במאורעות
  תרפ"ט פונו התושבים על ידי הצבא הבריטי"; "במאי 1932 חזרו התושבים לבית וגן"; "במאי 1937 החליטה
  המועצה המקומית לשנות את שם המקום לבת ים". [S, Wikipedia]
- **[S]** year 1926, CBS, with he.wiki agreeing. **Abandoned** in the 1929 riots (no exact date in
  the passage) and **resettled May 1932**. **Rename** Bayit VaGan to Bat Yam, decided **May 1937** [S,
  Wikipedia].
  Rows for the owner's sensitivity review.

#### Kiryat Ono (CBS 2620)
- CBS **1939**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `קריית אונו`, oldid **43882524**: "היישוב הנוכחי הוקם בשנת 1939". [S, Wikipedia]
- **[S]** year 1939, CBS, with he.wiki agreeing.

#### Holon (CBS 6600)
- CBS **1940**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `חולון`, oldid **43987452**: "חולון כמועצה מקומית הוקמה לאחר איחוד חמש השכונות שהיו במרחב
  ב-26 ביוני 1940 (שכונת גרין, מולדת, אגרובנק, קריית עבודה ושכונת עם)". [S, Wikipedia]
- **[S]** year 1940, CBS, with he.wiki agreeing. **Our reading:** 1940 is the union of five older
  neighbourhoods. The article's section headings date them: שכונת גרין (1924), שכונת מולדת (1930),
  שכונת אגרובנק (1934), שכונת קריית עבודה (1936) and שכונת עם (1937). [S, Wikipedia] In the Moledet
  section it says the neighbourhood "נפגעה ... וננטשה כמעט לחלוטין" in 1936 (the Arab Revolt), its
  residents taking shelter at Mikve Israel: an **abandonment** in 1936, with no day given. A row for
  the owner's sensitivity review.

### Ramla sub-district

#### Be'er Yaakov, first named Be'er Shalom (CBS 2530)
- CBS **1907**; Gvati `2.12.1907`. [V]
- he.wiki `באר יעקב`, oldid **43939820**: "המושבה הוקמה על ידי שתי קבוצות אלה בשנת 1907"; "בתחילה
  נקראה המושבה 'באר שלום' (תרגום מהערבית 'ביר סאלם') אולם כבר עם הקמת המושבה נקראה 'באר יעקב'".
  [S, Wikipedia]
- **Rename** at or near the founding, by the article's own wording.

#### Ben Shemen youth village (CBS 1084)
- CBS **1921**. Gvati has no row for the youth village; his row `בֶּן-שֶמֶן`, `שפלת לוד`, `11.9.22`,
  `תנועת המושבים`, is the moshav (part 3). [checked 2026-10-23]
- he.wiki `כפר הנוער בן שמן`, oldid **44033796**: "כפר הנוער נוסד בשנת 1927 על אדמות בית החרושת
  חדיד". [S, Wikipedia] Village Statistics 1945 (Ramle) has one unit "Ben Shemen"; **our reading:**
  it may cover both the moshav and the youth village, no source read here says so.
- **[C]** CBS 1921 against he.wiki 1927. **Our reading:** one source each; no source read here says
  what CBS's 1921 marks. Keep both.

#### Na'an (CBS 158)
- CBS **1930**; Gvati `10.3` as printed, a day and month with no year, `הקיבוץ המאוחד`.
- he.wiki `נען`, oldid **43556152**: "הקיבוץ נוסד בסוכות, תרצ"א, אוקטובר 1930". [S, Wikipedia]
- **[S]** year 1930, CBS, with he.wiki agreeing. **Our reading:** Gvati's `10.3` may be October 1930 with the
  year cut off; no source read here says so.

### Rehovot sub-district

#### Mazkeret Batya, first named Ekron (CBS 28)
- CBS **1883**; Gvati `7.11.1889`, in the section with no affiliation column.
- he.wiki `מזכרת בתיה`, oldid **43685177**: "נוסדה כמושבה בשלהי שנת 1883, ביוזמתו של הרב שמואל
  מוהליבר"; "שמה המקורי היה 'עקרון'"; "לבקשת הברון, במהלך ביקורו במקום באפריל 1887, הוחלף שמה של
  המושבה ל'מזכרת בתיה'". [S, Wikipedia] Village Statistics 1945 (Ramle) still prints
  "Mazkeret Batyah (‘Eqron)".
- **[C]** CBS and he.wiki 1883 against Gvati 1889. **Our reading:** two sources against one; use
  late 1883 and record Gvati's 7 November 1889, which no source read here explains. **Rename** Ekron
  to Mazkeret Batya in **April 1887** [S, Wikipedia].

#### Ness Ziona, first named Nahalat Reuven (CBS 7200)
- CBS **1883**; Gvati `1880` (status column `מועצה מקומית`).
- he.wiki `נס ציונה`, oldid **44026923**: "נס ציונה הוקמה בשנת 1883 כמושבה יהודית"; "יש המחשיבים את
  שנת 1882 כשנת ההקמה, מפני שב-18 ביוני 1882 קיבל ראובן לרר, המייסד, קושאן על האדמה שרכש"; "רק
  ב-1883 נבנה בנחלה זו בניין המגורים הראשון"; "העיר נקראה בתחילה 'נחלת ראובן' ו'ואדי אל חנין'". [S,
  Wikipedia]
- **[C]** CBS and he.wiki 1883 against Gvati 1880, with 18 June 1882 (the title deed) as a third
  date. **Our reading:** use 1883, the first dwelling, which two sources give; record 1880 and 1882.
  **Rename** Nahalat Reuven and Wadi Hanin to Ness Ziona; year not given. [GAP] Village Statistics
  1945 (Ramle) prints "Wadi Hunein (Nes Tsiyona)".

#### Ayanot (CBS 156)
- CBS **1930**. Not in Gvati's appendix (a youth village). [checked 2026-10-23: no row]
- he.wiki `עיינות`, oldid **43783584**: "יום ייסוד המקום נקבע ל-30 במרץ 1930 בו ניטע לראשונה עץ
  הדרים"; "שמה הוענק לה בשנת 1934 - עד אז נקראה 'משק הפועלות הגדול ליד נס ציונה'". [S, Wikipedia]
- **[S]** year 1930, CBS, with he.wiki agreeing; day 30 March 1930 [S, Wikipedia]. **Rename** in
  **1934** [S, Wikipedia].

#### Netaim, first named Kfar Vatikim (CBS 174)
- CBS **1932**; Gvati `15.7.32`. [V]
- he.wiki `נטעים`, oldid **42990654**: "המושב נוסד ב־י"א בתמוז ה'תרצ"ב, ה־15 ביולי 1932"; "ונקרא
  בתחילה כפר ותיקים". [S, Wikipedia]
- **Rename** Kfar Vatikim to Netaim; year not given. [GAP]

#### Kvutzat Yavne (CBS 334)
- CBS **1941**; Gvati `29.10.41`. [V]
- he.wiki `קבוצת יבנה`, oldid **44039224**: "חברי הקבוצה עלו לארץ ישראל ב-1929 והקימו בשנת 1931 את
  'קבוצת רודגס' בכפר אברהם שליד פתח תקווה"; of the winter of 1940 to 1941, "עברה הקבוצה למקומה
  הנוכחי, המקום שיועד להקמת היישוב ביבנה". [S, Wikipedia]
- **Move:** the group's first point, Kvutzat Rodges (1931, near Petah Tikva), moved to Yavne in the
  winter of 1940 to 1941; the first point's coordinate is a [GAP].

#### Beit Raban (CBS 848)
- CBS **1946**. Not in Gvati's appendix (a youth village). [checked 2026-10-23: no row]
- he.wiki `בית רבן` redirects to `גבעת ושינגטון`, oldid **43891051**: "בשנת ה'תש"ו (1946) התקיים טקס
  הנחת אבן הפינה של המוסד, שיועד בתחילתו לשמש ככפר נוער לילדים יתומים ניצולי השואה". [S,
  Wikipedia]
- **[S]** year 1946, CBS, with he.wiki giving a cornerstone in the same year.

## 3. Places that stood before 15 May 1948 and are not CBS localities today

These are the rows the issue calls renames and abandonment. Each is recorded with its source; nothing
is proposed about how it appears in the game. None is a CBS locality under its own name, except
where a later CBS row of the same name is noted. Coordinates: [GAP] for all of them.

### The four places of Hod HaSharon
- Magdiel: he.wiki `מגדיאל`, oldid **43898704**: "המושבה עלתה על הקרקע ב-24 באוקטובר 1924"; the Hod
  HaSharon article dates the decision to found it to August 1924 (part 2). [S, Wikipedia]
- Ramatayim: he.wiki `רמתיים`, oldid **42746030**: "מ-1925 עלו למקום 14 אנשים". [S, Wikipedia]
- Kfar Hadar: he.wiki `כפר הדר`, oldid **43237547**: "בשנת 1927 רכשו חלוצים אדמות"; "רשימת המייסדים
  בשנת 1928". [S, Wikipedia]
- Ramat Hadar: he.wiki `רמת הדר`, oldid **43383533**: "רמת הדר נוסדה ב-26 במאי 1938 כיישוב חומה
  ומגדל". [S, Wikipedia]
- All four merged into Hod HaSharon on **1 December 1964** (part 2). Village Statistics 1945 (Jaffa)
  names "Magdiel", "Ramatayim" and "Hadar"; Ramat Hadar is not listed by name. Not in Gvati.
  [checked 2026-10-23: no row for any of the four]

### Ein Ganim (merged into Petah Tikva)
- he.wiki `עין גנים`, oldid **43737658**: "עין גנים היה מושב פועלים שהוקם בשנת 1908, ובשנת 1937 הפך
  לשכונה בעיר פתח תקווה". [S, Wikipedia] Not in Gvati. [checked 2026-10-23: no row]
- Founded **1908**, a neighbourhood of Petah Tikva from **1937**. [S, Wikipedia]

### Kfar Ganim (merged into Petah Tikva)
- he.wiki `כפר גנים`, oldid **44101489**: "היא נוסדה ב-1926 בידי חברה משכנת"; "בשנת 1962 אוחדה השכונה
  עם העיר". [S, Wikipedia] Village Statistics 1945 (Jaffa) names "Kefar Gannim". Not in Gvati.
  [checked 2026-10-23: no row]
- Founded **1926**, merged **1962**. [S, Wikipedia]

### Nahalat Yitzhak (merged into Tel Aviv)
- he.wiki `נחלת יצחק (מזרח תל אביב)`, oldid **44012388**: "שכונה במזרח תל אביב שהוקמה בשנת 1925
  מזרחית לנחל איילון". [S, Wikipedia] Village Statistics 1945 (Jaffa) names "Nahalat Yits-haq".
  Not in Gvati. [checked 2026-10-23: no row]
- he.wiki, same article: "בתחילת שנת 1946 סופחו 550 דונם מאדמות השכונה לתל אביב"; "סופחה השכונה על
  ידי הבריטים במרץ 1948 לתל אביב". [S, Wikipedia]
- Founded **1925**; part of its land annexed to Tel Aviv early **1946**, the whole neighbourhood in
  **March 1948**, before 15 May 1948. [S, Wikipedia]

### Tel Litwinsky (merged into Ramat Gan)
- he.wiki `תל ליטוינסקי`, oldid **43678012**: "תל ליטוינסקי הייתה מושבה שהוקמה ב־1934"; "ב-1961 סופחה
  המושבה לעיר רמת גן". [S, Wikipedia] Village Statistics 1945 (Jaffa) names "Tel Litwinsky". Not in
  Gvati. [checked 2026-10-23: no row]
- Founded **1934**, annexed **1961**. [S, Wikipedia]

### Nahalat Yehuda (merged into Rishon LeZion)
- he.wiki `נחלת יהודה (ראשון לציון)`, oldid **43737649**: "שהוקמה מצפון לעיר כמושב נפרד בשנת 1913
  ה'תרע"ג"; "בשנת 1988 ה'תשמ"ט צורף המושב לעיר כשכונה". Gvati `נַחָלַת-יְהוּדָה`, `דרום`, `1914`
  (status column `מועצה מקומית`). Village Statistics 1945 (Ramle) names "Nahalat Yehuda".
- **[C]** he.wiki 1913 against Gvati 1914. **Our reading:** one source each; keep both. Merged
  **1988** [S, Wikipedia].

### Tirat Shalom, Kfar Aharon and Kfar Marmorek (now neighbourhoods of Ness Ziona and Rehovot)
- Tirat Shalom: he.wiki `טירת שלום`, oldid **43738322**: "מושב עובדים של יוצאי תימן שהוקם בשנת 1930".
  "בשנת 1959 הפך המושב לשכונה חקלאית בתחומה של נס ציונה". Village Statistics 1945 (Ramle) names
  "Tirat Shalom". [S, Wikipedia]
- Kfar Aharon: he.wiki `כפר אהרון`, oldid **43845665**: "הכפר הוקם ליד נס ציונה ... ביום 2 ביולי שנת
  1926". Village Statistics 1945 (Ramle) names "Kefar Aharon". [S, Wikipedia]
- Kfar Marmorek: he.wiki `כפר מרמורק`, oldid **43737609**: "השכונה הוקמה בשנת 1929 כמושב העובדים
  הראשון של עולי תימן". Village Statistics 1945 (Ramle) names "Kefar Marmorek". [S, Wikipedia]
- None is in Gvati. [checked 2026-10-23: no row] Tirat Shalom joined Ness Ziona in **1959** [S,
  Wikipedia]; the year Kfar Aharon and Kfar Marmorek joined their towns is not in the passages read.
  [GAP]

### Kfar Brandeis (merged into Hadera)
- he.wiki `ברנדייס (שכונה)`, oldid **41706076**: "שכונה בעיר חדרה שהוקמה ככפר ברנדייס בשנת 1927
  וצורפה לעיר חדרה לאחר קום המדינה". Village Statistics 1945 (Haifa) names "Kefar Brandeis". Not
  in Gvati. [checked 2026-10-23: no row] [S, Wikipedia]
- Founded **1927**, joined Hadera after 1948 (no year in the passage). [GAP]

### Ben Shemen moshav (CBS 2013, founded 1952)
- Gvati `בֶּן-שֶמֶן`, `שפלת לוד`, `11.9.22`, `תנועת המושבים`. CBS has a row `בן שמן (מושב)`, code
  2013, Ramla sub-district, `שנת ייסוד` **1952**.
- he.wiki `בן שמן`, oldid **43088381**: "המושב נוסד בדצמבר 1952 על ידי עולים מרומניה, מדרום לכפר
  הנוער בן שמן". [S, Wikipedia]
- **Our reading:** a moshav stood at Ben Shemen from 11 September 1922 (Gvati) [S]; CBS's row is a
  moshav founded in December 1952. Whether and when the first moshav was left is not in the
  passages read. [GAP] A row for the owner's sensitivity review.

### Giv'at Shapira (CBS 1077, founded 1958)
- CBS `גבעת שפירא`, code 1077, Sharon sub-district, `שנת ייסוד` **1958**; Gvati `גִבְעַת שַפִירָא`,
  `עמק חפר`, `1958`. Village Statistics 1945 (Tulkarm) lists "Giv‘at Shappira" with land areas and no
  population figure.
- **Our reading:** the 1945 unit is land, not shown to be inhabited; this is not a pre-1948 place on
  the sources read, and is listed only so a later reader does not count it twice.

## 4. Gaps and what was not done

- [GAP] **Completeness of part 3.** Part 3 lists the abandoned and merged places found from the CBS
  rows' own histories and the 1945 land units. It is not shown to be complete. Village Statistics
  1945 also names "Qiryat Shaul" (Jaffa), and "Tel Tsur" and "Benei Binyamin" (Tulkarm); none was
  checked further. The five neighbourhoods that formed Holon (part 2) have no rows of their own.
- [GAP] **Census of Palestine 1931**: not used (no usable text layer, see #1429).
- [GAP] **Coordinates of abandoned, moved and merged places**: none has one here.
- [GAP] **Rename years** for Nahalat Yehoshua, Rishpona, HaTehiya, Kfar Vatikim, Nahalat Reuven
  and the Borochov neighbourhood are not in the passages read.
- [GAP] **Abandonment dates** for Petah Tikva before 1883, Mikve Israel in the First World War, Bat
  Yam in 1929, Holon's Moledet neighbourhood in 1936 and Kfar Yavetz in 1948 are not given to the day
  in the passages read.
- **Gvati day versus CBS year.** Where they agree on the year the row is [V]; the day rests on Gvati
  alone, and as `settlements-1949-51.md` notes, it can be a day or more off other sources. Seventeen
  of the 116 rows (Tel Aviv-Yafo, Ramat Gan, Givatayim, Bnei Brak, Bat Yam, Kiryat Ono, Holon, Givat
  Shmuel, Netanya, Mikve Israel, Ben Shemen youth village, Ayanot, Beit Raban, Beit Berl, Kfar
  Avoda, Pardesiya and Kiryat Shlomo) have no Gvati row, so their year rests on CBS with Wikipedia
  beside it.
- **Not done:** no hex, no game date, no rule for which of two dates the game should use. Those
  belong to #61 and to the owner's sensitivity review.
