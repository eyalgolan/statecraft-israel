# Jewish localities founded in the Galilee before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Every source was read by an agent (Claude Code) on 23 October 2026; a person's check is pending. Nothing here is in `content/`, `sim/` or `unity/`.

Update 2026-10-24: the [GAP] rows were worked again by an agent (Claude Code) for #97 (owner's 24 Oct "Research part open" comment). Anything opened that day is marked "(24 Oct)". A person's check is still pending, and no row below was moved to [V] by this update.
Refs #97, its Galilee region (claimed in
https://github.com/eyalgolan/statecraft-israel/issues/97#issuecomment-6360680637). The dated
settlements file and `tools/settlement_hexes.py` stay with #61, and rows with a rename or an
abandonment date need the owner's sensitivity review before they enter the game; none is proposed
here.

## How it was done

- Every page was fetched on 23 October 2026 with the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024,
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23),
  columns `שם נפה`, `דת יישוב`, `שנת ייסוד` and `קואורדינטות`. The twelve-digit coordinate is read
  as an Israel Transverse Mercator easting and northing of six digits each and converted with
  `pyproj` (EPSG:2039 to EPSG:4326), the reading `settlements-1949-51.md` and the Negev note (#1403)
  use. The file does not state that reading, so every lat/lon below is **computed here** from the
  CBS string, not printed by CBS.
- **Founding dates.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix, Ben-Yehuda Project
  edition, https://benyehuda.org/read/14684 (checked 2026-10-23). Each row gives name, geographic
  region, date of founding, affiliation (kibbutzim and moshavim only) and the origin of the name.
  The column does not say whether a date is the day of going onto the land or a later
  recognition. Dates are quoted exactly as printed, for example `12.1882` or `23.5.39`.
- **Village Statistics 1945** (Government of Palestine), the archive.org scan
  https://archive.org/details/palestine-village-statistics-1945 and its text layer
  `Village Statistics 1945_djvu.txt` (checked 2026-10-23), Table I "Population and total land
  areas" for the Safad, Tiberias, Acre and Haifa sub-districts. Page numbers as printed in the text
  layer: Acre pp. 40 to 41, Safad pp. 69 to 71, Tiberias pp. 72 to 73; the Haifa table's page number
  is not legible there. It is used only as a **presence
  check**: a Jewish locality named in a 1945 land unit existed by then. It is not a founding source,
  and it lists land units, so many kibbutzim sit inside an Arab village's unit or are not named at
  all; "not listed by name" therefore proves nothing. The text layer drops blank columns, so a lone
  population figure cannot be assigned to the Arab or Jewish column; figures are given only where
  all three columns (Arabs, Jews, Total) survive.
- **Census of Palestine 1931**, *Population of villages, towns and administrative areas*,
  https://archive.org/details/CensusOfPalestine1931.PopulationOfVillagesTownsAndAdministrativeAreas
  (checked 2026-10-23). (24 Oct) The PDF `PalestineCensus1931.pdf` of that item (124 pages) was downloaded and read again; its embedded OCR is still unusable, but the pages render legibly as images (Ghostscript, `-sDEVICE=png16m -r90`, and `-r200` for one crop of printed p. 108), so the printed tables could be read by eye. Rows read (printed page numbers): Beit Gan and Bitanya (p. 82); Kefar Gil'adi (p. 107); Metullá and Mishmár hay Yardén (p. 108); Tel Hai (p. 111); and the Mahanayim position in the alphabetical run (pp. 108 to 109). No other row was read, so the rest of the Galilee rows have no 1931 check. [GAP] for those. Earlier: the PDF has no usable text layer (124 pages yield about 90 KB of
  broken OCR, the Safad pages give "Rosh PinnA", "Yesud ham Ma'alA", "MettulA" with unreadable
  figures), so nothing in this note rested on the OCR text; the 1931 figures added on 24 October come from the page images only.
- **Hebrew Wikipedia** was read through the MediaWiki API as the wikitext of the current revision,
  cited by `oldid`. It is a lead and a cross-check, not an authority; a fact resting on it alone is
  marked **[S, Wikipedia]**. Where an article cites a newspaper item that was not opened here, the
  note says so. Quotes keep the source's wording; double quote marks inside a Hebrew quote are
  printed as single quotes.

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

## What counts as the Galilee here

There is no single boundary, so this note states its rule.

Included: every locality that the CBS file codes as Jewish (`דת יישוב` = 1) with `שנת ייסוד` founded before 15 May 1948 (CBS year before 1948, or CBS 1948 with a Gvati date before 15 May)
in the **Safed, Kinneret and Acre sub-districts** (62 rows; plus Safed and Tiberias, coded
`ותיק`), and places that stood in the same area before 15 May 1948 and were later abandoned, moved
or merged into another locality (part 3).

Excluded, for a valleys note rather than this one: Menahemia (CBS 1902) and Gesher (CBS 1939), both in
the **Jezreel** sub-district, and the rest of that sub-district. Kefar Masaryk, Ein HaMifratz and
Afek are in the CBS Acre sub-district today. Kefar Masaryk and Ein HaMifratz appear in the Mandate
**Haifa** sub-district's 1945 table, and Afek is not named in either table. They are kept here because
the rule follows CBS, and flagged so a Haifa note does not count them twice. Bustan HaGalil (CBS 1948) is in, because Gvati dates it 1.2.48; the other CBS 1948 rows here are after 15 May in Gvati (Shamerat 28.5.48, Kefar HaNasi 2.7.48, Yiftah 19.8.48, Sa'ar 6.9.48, HaGosherim 26.9.48, Ga'ton 8.10.48), and later rows are out by date.

Safed and Tiberias are pre-1882 towns with old Jewish communities. CBS gives them `ותיק` (long
established) instead of a year, and no founding date is sought for them.

## 1. The CBS localities, one row each

Read every row as: CBS name (CBS transliteration); CBS locality code; CBS founding year; the
coordinate computed from the CBS string; Gvati's row (region, date as printed, affiliation); the
1945 land unit that names it, if any; the tag. All CBS cells come from bycode2024.xlsx, all Gvati
cells from https://benyehuda.org/read/14684, all 1945 cells from the archive.org text layer cited
above, each checked 2026-10-23. Rows tagged "part 2" or "part 3" have a disagreement, a rename or
an earlier founding written out below.

### Safed sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| ראש פינה (Rosh Pinna) | 26 | 1882 | 32.96810, 35.54253 | הגליל העליון, `12.1882`, (no affiliation column) | Safad: "Rosh Pinna" | [V]; Gei Oni 1878, part 3 |
| יסוד המעלה (Yesud HaMa'ala) | 29 | 1883 | 33.05696, 35.60290 | הגליל העליון, `9.1883`, (no affiliation column) | Safad: "Yesud ham Ma‘ala (includes El Kharrar)" | [V] |
| מטולה (Metula) | 43 | 1896 | 33.27861, 35.57707 | הגליל העליון, `19.5.1896`, (no affiliation column) | Safad: "Metulla (includes Deir Mamas, Hira and Kafr Kila)" | [V] |
| כפר גלעדי (Kefar Gil'adi) | 76 | 1916 | 33.24266, 35.57445 | הגליל העליון, `21.10.16`, האיחוד | Safad: "Kefar Gil‘adi (Tel Hat)" [sic, text layer] | [V]; Tel Hai merged into it, part 3 |
| איילת השחר (Ayyelet HaShahar) | 77 | 1918 | 33.02368, 35.57618 | עמק החולה, `30.6.18`, האיחוד | Safad: "Aiyelet hash Shahar and Yarda" | [V] |
| חולתה (Hulata) | 253 | 1937 | 33.05160, 35.60988 | עמק החולה, `9.36`, הקיבוץ המאוחד | not listed by name | [C] CBS 1937, Gvati 1936; part 2 |
| דן (Dan) | 303 | 1939 | 33.24014, 35.65281 | עמק החולה, `4.5.39`, הקיבוץ הארצי | Safad: "Dan (previously Khan ed Duweir)" | [V] |
| דפנה (Dafna) | 302 | 1939 | 33.22995, 35.63852 | עמק החולה, `3.5.39`, הקיבוץ המאוחד | Safad: "Dafna" | [V] |
| מחניים (Mahanayim) | 308 | 1939 | 32.98906, 35.57012 | הגליל העליון, `23.5.39`, הקיבוץ המאוחד | Safad: "Mahanayim" (text layer: "Neahanayinn") | [V] for the 1939 kibbutz; earlier foundings in part 3 |
| עמיר (Amir) | 319 | 1939 | 33.17818, 35.62025 | עמק החולה, `29.10.39`, הקיבוץ הארצי | Safad: "Dawwara, ‘Amir and Kefar Nehemya" | [V] |
| בית הלל (Bet Hillel) | 322 | 1940 | 33.20843, 35.60577 | עמק החולה, `4.1.40`, תנועת המושבים | Safad: "Lazvaza and Beit Hillel" [sic] (Arabs 230, Jews 100) | [V] |
| שאר ישוב (She'ar Yashuv) | 324 | 1940 | 33.22647, 35.64649 | עמק החולה, `7.1.40`, העובד הציוני | Safad: "She-ar Yashuv" | [V] |
| שדה נחמיה (Sede Nehemya) | 329 | 1940 | 33.18738, 35.62365 | עמק החולה, `1940`, האיחוד | Safad: "Dawwara, ‘Amir and Kefar Nehemya" | [V] |
| כפר סאלד (Kefar Szold) | 345 | 1942 | 33.19563, 35.65802 | הגליל העליון, `13.11.42`, הקיבוץ המאוחד | Safad: "‘Abisiya, El and Kefar Szold" (Arabs 1,220, Jews 290) | [V] |
| כפר בלום (Kefar Blum) | 357 | 1943 | 33.17330, 35.60874 | עמק החולה, `10.11.43`, האיחוד | not listed by name | [V] |
| מנרה (Menara) | 347 | 1943 | 33.19624, 35.54454 | הגליל העליון, `18.1.43`, הקיבוץ המאוחד | Safad: "Manara, El" | [V] |
| שמיר (Shamir) | 366 | 1944 | 33.16669, 35.65998 | עמק החולה, `28.11.44`, הקיבוץ הארצי | not listed by name | [V] |
| בירייה (Biriyya) | 368 | 1945 | 32.97793, 35.50019 | [GAP] no row | not listed by name | [S] CBS only; part 2 |
| להבות הבשן (Lahavot HaBashan) | 380 | 1945 | 33.14149, 35.64734 | עמק החולה, `7.8.52`, הקיבוץ הארצי | not listed by name | [C] CBS 1945, Gvati 1952; part 2 |
| משגב עם (Misgav Am) | 378 | 1945 | 33.24775, 35.54848 | הגליל העליון, `2.11.45`, הקיבוץ המאוחד | not listed by name | [V] |
| רמות נפתלי (Ramot Naftali) | 372 | 1945 | 33.10155, 35.55330 | הגליל העליון, `25.4.45`, תנועת המושבים | not listed by name | [V] |
| נאות מרדכי (Ne'ot Mordekhay) | 408 | 1946 | 33.16002, 35.59679 | עמק החולה, `3.11.46`, האיחוד | not listed by name | [V] |
| עמיעד (Ammi'ad) | 385 | 1946 | 32.92810, 35.54101 | גליל עליון, `17.1.46`, האיחוד | not listed by name | [V] |
| מעיין ברוך (Ma'yan Barukh) | 416 | 1947 | 33.23980, 35.60722 | הגליל העליון, `11.8.47`, האיחוד | not listed by name | [V] |
| צפת (Zefat) | 8000 | ותיק | 32.96419, 35.50532 | [GAP] no row | Safad: "Safad (Urban)" (Jews 2,400) | pre-1882 town; no founding date sought |

### Acre sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| נהרייה (Nahariyya) | 9100 | 1934 | 33.00844, 35.09766 | [GAP] no row | Acre: "Nahariya" (figures not legible in the text layer) | [S] CBS only; part 2 |
| אילון (Elon) | 294 | 1938 | 33.06328, 35.21685 | הגליל המערבי, `14.11.38`, הקיבוץ הארצי | Acre: "Khirbat Samah (Eilon)" | [V] |
| חניתה (Hanita) | 280 | 1938 | 33.08839, 35.17537 | הגליל המערבי, `18.11.38`, האיחוד | Acre: "Hanita" | [V] year; [C] day, part 2 |
| כפר מסריק (Kefar Masaryk) | 297 | 1938 | 32.89035, 35.10026 | עמק זבולון, `29.11.38`, הקיבוץ הארצי | Haifa: "‘Emeq Zevulun (Jidru Ghawarina), Kefar Masaryk and ‘Ein ham Mifrats" | [V] |
| עין המפרץ (En HaMifraz) | 289 | 1938 | 32.90314, 35.09652 | עמק זבולון, `25.8.38`, הקיבוץ הארצי | Haifa: "‘Emeq Zevulun (Jidru Ghawarina), Kefar Masaryk and ‘Ein ham Mifrats" | [V] |
| שבי ציון (Shave Ziyyon) | 282 | 1938 | 32.98178, 35.08405 | הגליל המערבי, `13.4.38`, האיחוד החקלאי | Acre: "Mazra‘a, El, Shavei Zion, ‘Ein Sara and Ga‘aton" | [V] |
| אפק (Afeq) | 313 | 1939 | 32.83854, 35.12798 | עמק זבולון, `28.5.39`, הקיבוץ המאוחד | not listed by name | [V] year; founded as משמר הים, part 2 |
| מצובה (Mazzuva) | 325 | 1940 | 33.06281, 35.15681 | הגליל המערבי, `14.2.40`, האיחוד | not listed by name | [V] |
| עברון (Evron) | 376 | 1945 | 32.99187, 35.10049 | גליל מערבי, `8.10.45`, הקיבוץ הארצי | not listed by name | [V] year; group from 1936, part 2 |
| יחיעם (Yehi'am) | 409 | 1946 | 32.99759, 35.22004 | הגליל המערבי, `27.11.46`, הקיבוץ הארצי | not listed by name | [V] |
| רגבה (Regba) | 390 | 1946 | 32.97778, 35.09961 | הגליל המערבי, `15.8.46`, האיחוד | not listed by name | [V] |
| בוסתן הגליל (Bustan HaGalil) | 559 | 1948 | 32.95134, 35.08214 | הגליל המערבי, `1.2.48`, האיחוד החקלאי | not listed by name | [V] year; Gvati day before 15 May 1948 |

### Kinneret sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| יבנאל (Yavne'el) | 46 | 1901 | 32.70849, 35.50345 | הגליל התחתון, `3.10.1901`, (no affiliation column) | Tiberias: "Yavneel and Mishmar hash Shelosha" | [V]; Beit Gan and Mishmar HaShlosha merged in, part 3 |
| כפר תבור (Kefar Tavor) | 47 | 1901 | 32.68704, 35.42045 | הגליל התחתון, `11.1901.`, (no affiliation column) | Tiberias: "Kefar Tavor (Mas-ha)" | [V] |
| אילנייה (Ilaniyya) | 49 | 1902 | 32.75429, 35.40733 | הגליל התחתון, `1902, חודשה ב-1.7.53`, האיחוד החקלאי | Tiberias: "Sejera (Ilanya)" | [V] year; renamed from Sejera, part 2 |
| כנרת (קבוצה) (Kinneret(Qevuza)) | 57 | 1908 | 32.71355, 35.56160 | עמק הירדן, `7.6.08`, האיחוד | Tiberias: "Kinneret Group" | [C] he.wiki 1913; part 2 |
| מצפה (Mizpa) | 58 | 1908 | 32.79007, 35.50964 | הגליל התחתון, `10.1908`, (no affiliation column) | Tiberias: "Mitspa" | [V] |
| דגניה א' (Deganya Alef) | 62 | 1909 | 32.70800, 35.57478 | עמק הירדן, `1.11.90`, האיחוד | Tiberias: "Deganiya ‘A’" | [C] Gvati prints `1.11.90`; part 2 |
| כנרת (מושבה) (Kinneret(Moshava)) | 63 | 1909 | 32.72455, 35.56456 | עמק הירדן, `1909`, (no affiliation column) | Tiberias: "Kinneret" | [C] Gvati 1909, he.wiki 1908; part 2 |
| מגדל (Migdal) | 65 | 1910 | 32.83861, 35.50335 | הגליל התחתון, `1.1910`, (no affiliation column) | Tiberias: "Migdal" | [V] |
| דגניה ב' (Deganya Bet) | 79 | 1920 | 32.70031, 35.57510 | עמק הירדן, `21.9.20`, האיחוד | Tiberias: "Deganiya ‘B’" | [V] |
| בית זרע (Bet Zera) | 143 | 1926 | 32.68891, 35.57341 | עמק הירדן, `20.9.27`, הקיבוץ הארצי | Tiberias: "Beit Zera‘" (line split in the text layer) | [C] CBS 1926, Gvati 1927; part 2 |
| כדורי (Kadoorie) | 371 | 1931 | 32.70292, 35.40578 | [GAP] no row | not listed by name | [C] he.wiki 20 June 1933; part 2 |
| אפיקים (Afiqim) | 176 | 1932 | 32.68110, 35.57803 | עמק הירדן, `8.32`, האיחוד | Tiberias: "Afiqim" | [V] |
| אשדות יעקב (איחוד) (Ashdot Ya'aqov(Ihud)) | 199 | 1933 | 32.65814, 35.58029 | עמק הירדן, `13.10.32`, האיחוד | Tiberias: "Ashdot Ya‘aqov" | [C] CBS 1933, Gvati 1932; part 2 |
| אשדות יעקב (מאוחד) (Ashdot Ya'aqov(Me'uhad)) | 188 | 1933 | 32.66182, 35.58349 | עמק הירדן, `13.10.32`, הקיבוץ המאוחד | Tiberias: "Ashdot Ya‘aqov" | [C] CBS 1933, Gvati 1932; part 2 |
| כפר חיטים (Kefar Hittim) | 255 | 1936 | 32.80110, 35.50274 | הגליל התחתון, `3.12.36`, תנועת המושבים | Tiberias: "Kefar Hittim" | [V] |
| גינוסר (Ginnosar) | 262 | 1937 | 32.84754, 35.52325 | עמק הירדן, `25.2.37`, הקיבוץ המאוחד | Tiberias: "Ghuweir Abi Shisha and Genossar" | [V] |
| מסדה (Massada) | 263 | 1937 | 32.68365, 35.59832 | עמק הירדן, `31.3.37`, האיחוד | Tiberias: "Samakh, Massada and Sha‘ar hag Golan" | [V] |
| עין גב (En Gev) | 273 | 1937 | 32.78281, 35.63896 | עמק הירדן, `6.7.37`, האיחוד | Tiberias: "‘Ein Gev (Nugeib)" (Arabs 320, Jews 420) | [V] |
| שער הגולן (Sha'ar HaGolan) | 264 | 1937 | 32.68708, 35.60408 | עמק הירדן, `21.3.37`, הקיבוץ הארצי | Tiberias: "Samakh, Massada and Sha‘ar hag Golan" | [V] |
| שרונה (Sharona) | 292 | 1938 | 32.72616, 35.46643 | הגליל התחתון, `16.8.38`, תנועת המושבים | Tiberias: "Sharona" | [V] |
| הזורעים (HaZore'im) | 307 | 1939 | 32.74600, 35.50243 | הגליל התחתון, `23.5.39`, הפועל המזרחי | Tiberias: "Tiberias (Rural) and Haz Zor‘im" | [V] |
| שדמות דבורה (Shadmot Devora) | 306 | 1939 | 32.69628, 35.43604 | הגליל התחתון, `23.5.39`, תנועת המושבים | not listed by name | [V] |
| אלומות (Alummot) | 330 | 1941 | 32.70685, 35.54640 | הגליל התחתון, `20.3.41`, האיחוד | not listed by name | [V] year; first at the Poria estate, part 2 |
| בית קשת (Bet Qeshet) | 365 | 1944 | 32.71862, 35.39406 | הגליל התחתון, `15.8.44`, הקיבוץ המאוחד | not listed by name | [V] |
| חוקוק (Huqoq) | 374 | 1945 | 32.88061, 35.49529 | הגליל התחתון, `5.10.45`, הקיבוץ המאוחד | not listed by name | [V] |
| כפר קיש (Kefar Kisch) | 388 | 1946 | 32.66702, 35.44908 | הגליל התחתון, `18.7.46`, תנועת המושבים | not listed by name | [V] |
| טבריה (Tiberias) | 6700 | ותיק | 32.78752, 35.52353 | [GAP] no row | Tiberias: "Tiberias (Urban)" | pre-1882 town; no founding date sought |

## 2. Disagreements, renames and moves for CBS localities

Hebrew Wikipedia pages below were read at the `oldid` given, checked 2026-10-23.

### Degania A (CBS 62)
- CBS `שנת ייסוד` **1909**. Gvati prints the date as **`1.11.90`** (kibbutzim section, `דְגַנְיָה א' |
  עמק הירדן | 1.11.90 | האיחוד`), which cannot be read as a year of the colony era.
- he.wiki `דגניה א'`, oldid **44010154**: "נוסדה בשנת 1910 על חלק משטח של כ-3,000 דונם מאדמות הכפר
  אום ג'וני", and its infobox `תאריך ייסוד הגרעין= 1909`. It also says the group moved "ב-4 ביוני 1912
  ... מאום ג'וני לנקודת הקבע". [S, Wikipedia]
- **[C]** CBS 1909 (the year the group formed, per the he.wiki infobox) against he.wiki 1910 (the
  settlement at Umm Juni). **Our reading:** Gvati's `1.11.90` is most likely a misprint of
  1.11.1910, which would agree with he.wiki's 1910, but that is a guess and is not used. Record CBS
  1909 and he.wiki 1910 side by side, and the move to the present site on 4 June 1912 as a move
  date, not a founding date.

### Kinneret, the kvutza (CBS 57) and the moshava (CBS 63)
- CBS: kvutza **1908**, moshava **1909**.
- Gvati has two rows named `כִּנֶּרֶת`: in the kibbutzim section `עמק הירדן | 7.6.08 | האיחוד`, and
  in the moshavot section `עמק הירדן | 1909`.
- he.wiki `קבוצת כנרת`, oldid **43845309**: "הקבוצה הוקמה בב' בחשוון ה'תרע"ד, 2 בנובמבר 1913".
  he.wiki `חוות כנרת`, oldid **43844892**: the training farm "הוקמה ב-1908". he.wiki
  `כנרת (מושבה)`, oldid **43812492**: "נוסד כמושבה בשנת 1908 על ידי חברת יק"א". [S, Wikipedia]
- **[C] kvutza:** CBS 1908 and Gvati 7 June 1908 against he.wiki 2 November 1913. **Our reading:**
  1908 is the date of the Kinneret training farm, from which the kvutza grew; 1913 is the founding
  of the kvutza as a group. Both are kept; the game should say which one it means.
- **[C] moshava:** CBS and Gvati 1909 against he.wiki 1908. **Our reading:** two sources against one;
  use 1909 and record he.wiki's 1908.

### Beit Zera (CBS 143)
- CBS **1926**; Gvati `20.9.27`; he.wiki `בית זרע`, oldid **44026936**: the Markenhof group "עלתה על
  הקרקע בספטמבר 1927". [S, Wikipedia]
- **[C]** CBS 1926 against Gvati and he.wiki September 1927. **Our reading:** use 20 September 1927;
  two sources give it to the day or month, and the article places the group elsewhere (Ein Ganim,
  then Ruba al-Nasra) before 1927.

### Ashdot Ya'akov, both kibbutzim (CBS 199 and 188)
- CBS **1933** for each; Gvati `13.10.32` for each (`אשדות-יעקב ... האיחוד` and `אשדות יעקב ...
  הקיבוץ המאוחד`).
- he.wiki `אשדות יעקב`, oldid **44069166**: "בספטמבר 1924 הקימה חבורת 'אחדות' ... קבוצה באתר 'גשר
  הישנה'"; it moved "בין השנים 1932–1935 לאדמות דלהמיה"; and "קיבל הקיבוץ בשנת 1938 את שמו החדש,
  אשדות יעקב". The kibbutz split into the two present localities in 1951. [S, Wikipedia]
- **[C]** three dates: 1924 (the group at old Gesher), 13 October 1932 (Gvati) and 1933 (CBS).
  **Our reading:** Gvati's 1932 and CBS's 1933 both describe the move to the present site, which
  he.wiki spreads over 1932 to 1935; use 13 October 1932 as the move date and 1924 as the founding of
  the group. **Rename** in 1938; the earlier name of the kibbutz is not stated in the passage read.
  (24 Oct) The same article, in its account of the 1924 to 1926 beginning, says of the group that stayed after the failed Bashkirim settlement of 1920 to 1921: "היישוב הקיבוצי החדש נקרא 'גשר'" (the new kibbutz settlement was called "Gesher"). **Our reading:** Gesher is the name before the 1938 rename, but the article does not say so in one sentence, and the present kibbutz Gesher is a different locality (per the article, the 1924 site lay east of it). [S, Wikipedia] The sentence is not a source for the date the name Gesher began or ended.

### Hulata (CBS 253)
- CBS **1937**; Gvati `9.36`.
- he.wiki `חולתה`, oldid **43227564**: "בקיץ 1936 הגיעו מספר מועט מחברי הקבוצה ליסוד המעלה";
  "במרץ 1937 ניתן להם להקים משק קטן" in the Hula concession yard; "בסוף 1945 החלה בניית יישוב הקבע,
  וב-1946 עבר הקיבוץ למקום". [S, Wikipedia]
- **[C]** Gvati September 1936 against CBS 1937. **Our reading:** 1936 is the group's arrival at
  Yesud HaMa'ala, March 1937 its first farm of its own; use March 1937 and record the 1946 move to the
  permanent site.

### Lahavot HaBashan (CBS 380)
- CBS **1945**; Gvati **`7.8.52`**.
- he.wiki `להבות הבשן`, oldid **44054315**: "הקיבוץ עלה על הקרקע ביום השנה להצהרת בלפור, ב-2 בנובמבר
  1945"; the name was first להבות, and "מספר שנים לאחר עלייתו לקרקע נוספה לשם הקיבוץ המילה 'הבשן'".
  The article also lists a *Davar* item, "חג העשור ללהבות הבשן", 30 November 1955, not opened here.
  [S, Wikipedia]
- **[C]** CBS and he.wiki 1945 against Gvati 1952. **Our reading:** use 2 November 1945; Gvati's
  1952 does not match any event in the article read, and the reason for it is a [GAP]. (24 Oct) In the Gvati page read again on 24 October, the row directly above, Lahav (northern Negev, Kibbutz Artzi), carries the identical date `7.8.52`, so a carry-over into the Lahavot HaBashan row is possible; **our reading**, not shown, and the day is not used. The row's own name-origin cell reads "לפי מיקומו הגיאוגרפי (הבשן) ושם הגרעין המֵקים (להבות)", which supports the he.wiki account of a name built from the group's name, but gives no year. **Rename**
  להבות to להבות הבשן, year not given. [GAP]

### Hanita (CBS 280)
- CBS **1938**; Gvati `18.11.38`.
- he.wiki `חניתה`, oldid **43338150**, caption of its founding photograph: "העלייה לקרקע של קיבוץ
  חניתה, 21 במרץ 1938"; and "זהו היישוב ה-18 בין יישובי חומה ומגדל". [S, Wikipedia]
- **[C] on the day.** Gvati 18 November 1938 against he.wiki 21 March 1938. **Our reading:** use 21
  March 1938 for the tower-and-stockade ascent; Gvati's November date may be a later stage, but no
  source read here says so.

### Kadoorie (CBS 371)
- CBS **1931**. Not in Gvati's appendix (it is a school, not a settlement). [checked 2026-10-23: no row]
- he.wiki `בית הספר החקלאי כדורי`, oldid **43963826**: "בית הספר נוסד ב-20 ביוני 1933 למרגלות הר
  תבור". [S, Wikipedia]
- **[C]** CBS 1931 against he.wiki 20 June 1933. The article's only 1931 is its category `יישובים שהוקמו ב-1931`, a Wikipedia category, not a dated
  statement. **Our reading:** no source here explains CBS's 1931;
  use 1933 for the opening of the school and keep CBS's year beside it.

### Nahariya (CBS 9100)
- CBS **1934**. Not in Gvati's appendix (a town). [checked 2026-10-23: no row]
- he.wiki `נהריה`, oldid **44103186**: "בשנת 1934 התאפשר לקנות אדמה בבעלות ערבית, שטח של 2,400
  דונמים צפונית לעכו". [S, Wikipedia] That is the land purchase, not the arrival of the first
  settlers. The same article: "ב-10 בפברואר 1935 התיישבו במקום וכך נקבע תאריך זה כתאריך הרשמי לייסודה של נהריה". [S, Wikipedia] **[C]** CBS 1934 against the official founding date of 10 February 1935. **Our reading:** 1934 is the land purchase; use 10 February 1935 and keep CBS's year beside it.

### Biriya (CBS 368)
- CBS **1945**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `מצודת ביריה`, oldid **41257964**: "את ביריה יישב ב-8 בינואר 1945 גרעין של המחלקה הדתית של
  הפלמ"ח"; on 5 March 1946 "הודיע הצבא הבריטי כי הוא כובש את ביריה, גירש את הנוטרים, ואסר על
  המגורים במקום"; on 14 March 1946 "עלו אלפי בני נוער ... והקימו נקודת יישוב חדשה בסמוך לביריה א'";
  the settling group, "גרעין ה' של בני עקיבא", stayed "עד שעברו לעין צורים בפברואר 1947". [S, Wikipedia]
- he.wiki `העלייה לביריה`, oldid **43838312**: the affair "החלה בסוף חודש פברואר 1946 ... והגיעה
  לשיאה בין 14 במרץ ל-17 במרץ 1946". he.wiki `ביריה`, oldid **43619175**: "במלחמת העצמאות נפגע
  היישוב קשות ונהרס. בשנת 1949 הוקם במקום מחנה עולים". [S, Wikipedia]
- **[S]** year 1945: CBS, with he.wiki's 8 January 1945 agreeing; not [V], which this note keeps for
  CBS plus Gvati. he.wiki `ביריה` also says "בשנת 1949 הוקם במקום מחנה עולים. באוקטובר 1959 הוחלט
  להפוך את היישוב לכפר יערנים"; **our reading:** the present community grew from that 1949 camp,
  and CBS keeps the 1945 year. The eviction (5 March 1946), the second
  point (14 March 1946) and the 1948 destruction are each a separate dated event, and these are
  rows for the owner's sensitivity review.

### Ilaniya, formerly Sejera (CBS 49)
- CBS **1902**; Gvati `1902, חודשה ב-1.7.53` (re-established 1 July 1953). [V] for 1902.
- he.wiki `אילניה`, oldid **43952364**: it began as the Sejera farm, bought "בשנת 1899", "ובשנת 1902
  הפכה למושבה"; "בשנת 1957 הפכה אילניה למושב". Its category `יישובים שהוקמו ב-1899` follows the
  purchase year. [S, Wikipedia] Village Statistics 1945 names the land
  unit "Sejera (Ilanya)". **Rename:** Sejera to Ilaniya; the year of the official name is a [GAP].
  The article also states that the neighbouring Arab village al-Shajara was captured on 6 May 1948 and
  that its residents fled; that is outside this note's subject and is noted only as a pointer for the
  owner's sensitivity review.

### Alumot (CBS 330)
- CBS **1941**; Gvati `20.3.41`. [V]
- he.wiki `אלומות`, oldid **42318917**: the group formed in 1937, and "בסתיו 1940, הקבוצה עלתה לפוריה
  והתמקמה בבניינים הישנים וההרוסים של האחוזה", a site the article calls temporary. [S, Wikipedia]
  **Our reading:** 20 March 1941 is the founding; autumn 1940 at the Poria estate is a preliminary
  stage. The group moved to its permanent site near Bitaniya Illit in **November 1946** [S, Wikipedia, citing Davar 14 November 1946]; the day is a [GAP].

### Afek, founded as Mishmar HaYam (CBS 313)
- CBS **1939**; Gvati `28.5.39`. [V]
- he.wiki `אפק (קיבוץ)`, oldid **42985726**: "במאי 1939 הקימו חברי הקבוצה יישוב חומה ומגדל בשם
  'משמר הים', כחמישה קילומטרים דרומית לעכו"; "רוב חברי הקיבוץ עברו לאתר הקבע החדש ב-18 בספטמבר
  1946 ולקראת סוף שנת 1947 הושלם המעבר. לאחר המעבר, שינה הקיבוץ ... את שמו ל'אפק'". [S, Wikipedia]
- **Rename** משמר הים to אפק, after the move (end of 1947). **Move date** 18 September 1946. CBS's
  coordinate is the present site, not the 1939 one. [GAP] for a coordinate of Mishmar HaYam.

### Evron (CBS 376)
- CBS **1945**; Gvati `8.10.45`. [V]
- he.wiki `עברון`, oldid **43900430**: the group was founded "ב-1936 על מגרש בן דונם ורבע בנהריה";
  "באוקטובר 1945 עלתה הקבוצה ... לנקודת הקבע". [S, Wikipedia] **Our reading:** 1945 is the founding
  of the locality; 1936 is the founding of the group, which lived inside Nahariya.

## 3. Places that stood before 15 May 1948 and are not CBS localities today

These are the rows the issue calls renames and abandonment. Each is recorded with its source; nothing
is proposed about how it appears in the game. None is a CBS locality under its own name.
[checked 2026-10-23: no CBS row matches `גיא אוני`, `חמארה`, `עין זיתים`, `בית גן`, `משמר השלושה`, `ביתניה` or
`משמר הים`; the only `תל חי` row is `תל חי (מכללה)`, code 1707, a college with no founding year; the
only `חמרה` row, code 3609, 1971, is a different place in the Jordan Valley.]

### Gei Oni (predecessor of Rosh Pinna)
- he.wiki `ראש פינה`, oldid **43998079**: "ב-1878 רכשה חבורה של 17 משפחות מבני צפת מחצית מאדמות הכפר
  הערבי ג'עוני"; "המתיישבים קראו ליישוב החדש בשם גיא אוני"; on 24 July 1882 David Shub bought the
  Gei Oni lands, and "המושבה נוסדה לקראת סוף 1882". [S, Wikipedia] Gvati `12.1882` and CBS 1882 date
  Rosh Pinna, not Gei Oni.
- **Gei Oni 1878 [S, Wikipedia]; Rosh Pinna 1882 [V] (CBS and Gvati), December 1882 [S] (Gvati only); both date Rosh Pinna, not
  the rename).** Abandonment:
  the article says only a few families stayed after two years; no date. [GAP]

### Mahanayim, the first two attempts
- he.wiki `מחניים`, oldid **42319503**: land bought in 1891; "בשנת 1898 התיישבו במקום מספר משפחות
  מגליציה"; "בשנת 1903 נמכרה אדמת מחנים לחקלאי ראש פינה ומתיישבי מחנים פונו"; Caucasian Jews then
  living there "נטשו בשנת 1912"; "בשנת 1922, מחניים הפך למושב עובדים וכעבור שש שנים נעזב שוב"; the
  present kibbutz on "23 במאי 1939", which matches Gvati `23.5.39`. [S, Wikipedia]
- First colony **1898 to 1903** (vacated, families compensated), a later group left **1912**, a moshav
  **1922 to about 1928** (computed here: 1922 plus "six years"). [S, Wikipedia] No coordinate of the
  old sites. [GAP] (24 Oct) Searched again and not found. Read: the he.wiki `מחניים` wikitext, oldid 42319503, whose coordinate source is the template for the present kibbutz; the English Wikipedia "Mahanayim" coordinate, 32.98916667, 35.57027778 (last revision id 1374885288), which is the present kibbutz (CBS 32.98906, 35.57012). Census of Palestine 1931: no row named Mahanayim sits between "Madahil" (end of printed p. 108) and "Muftakhira" (start of p. 109) in the Safad run; other spellings were not searched, so this is not shown to be absent.

### Tel Hai
- he.wiki `תל חי`, oldid **43882634**: "ב-1916 נאחזה במקום קבוצה מאנשי 'הרועה' לזמן קצר"; "בספטמבר
  1918 התיישבה במקום קבוצת פועלים"; the battle of "1 במרץ 1920"; "היישוב תל חי אוחד ב-1926 עם כפר
  גלעדי, ואנשיו עברו לשם". [S, Wikipedia] Village Statistics 1945 names the land unit "Kefar Gil‘adi
  (Tel Hai)" (text layer "Tel Hat"). (24 Oct) Census of Palestine 1931, Safad sub-district, printed p. 111: "Tel Hai", 4 occupied houses, 14 persons (9 male, 5 female), all 14 in the Jewish columns (9 + 5). Presence in 1931 only; it dates nothing.
- The same article: after quiet was restored "בחודש יולי 1920", "ב-5 באוקטובר חזרו המתיישבים היהודיים
  אל שלושת היישובים שננטשו ויישבו אותם מחדש". [S, Wikipedia] **Our reading:** the three are Kfar
  Giladi, Metula and Tel Hai, since the article says Hamara was not re-founded; the year 1920 is
  from context.
- Founded **September 1918** (after a short stay in 1916), abandoned after **1 March 1920** (exact day
  [GAP]), resettled **5 October 1920**, merged into Kfar Giladi **1926**. [S, Wikipedia] Coordinate:
  33.235, 35.57833333, the coordinate of the English Wikipedia article "Tel Hai" (API `prop=coordinates`, last revision id 1361942946, read 2026-10-24). [S, Wikipedia] **Computed here:** CBS's Kefar Gil'adi point (33.24266, 35.57445) is about 0.9 km from it (852 m north-south, 362 m east-west, square root of the sum of squares 926 m, at 111.2 km per degree of latitude and 93.2 km per degree of longitude at 33.2 N). Not a map reading of the courtyard.
- (24 Oct) The he.wiki article gives the end of the battle and then: "החליטו מגיני החצר לנטוש את תל חי ולשורפה באש" and "לאחר הטיפול בפצועים עלו כולם לכפר גלעדי". [S, Wikipedia] So the article places the abandonment after the battle of 1 March 1920 and prints no separate day for leaving; the exact day stays a [GAP].

### Hamara
- he.wiki `חמארה (כפר)`, oldid **43174476**: "נוסד באוקטובר 1919 ... באתר ממזרח לכפר גלעדי, ונעזב בשל
  קשיי המחיה והתקפות פורעים ב-1 בינואר 1920"; "חמארה לא הוקמה מחדש". The Tel Hai article says the
  same ("שננטשה על ידי מתיישביה בעקבות הפרעות באחד בינואר 1920"), but both are Wikipedia. [S, Wikipedia]
- Founded **October 1919**, abandoned **1 January 1920**. [S, Wikipedia] Coordinate: [GAP]. (24 Oct) Searched again and not found. Read: the he.wiki `חמארה (כפר)` wikitext, oldid 43174476 (no coordinate parameter in it); the English Wikipedia API (`prop=coordinates`, `redirects=1`) for the titles "Hamara, Israel" and "Hamara (Safed)", which returned a missing page for both.

### Ein Zeitim
- he.wiki `עין זיתים`, oldid **42229027**: founded "בשנת 1891"; "ב-1925 שבה והוקמה המושבה"; "ב-1932 שבה
  ונוסדה המושבה אך שוב נעזבה במאורעות תרצ"ו-תרצ"ט"; a kibbutz set up there "בט"ו בשבט 1946"; attacked
  from December 1947 and on 2 February 1948, and the settlers returned after the fighting; "לקראת סוף
  1951 עזבו רוב החברים". Its map template gives **32.9972, 35.485904**, the article's own figure.
  [S, Wikipedia]
- Foundings **1891, 1925, 1932, 17 January 1946 (Tu BiShvat 5706, computed here)**; abandonments in the First World War (Ottoman expulsion, no exact date), in **1929** (attacked and burned on **4 September 1929**), in the 1936 to 1939 revolt (no exact date) and at the **end of 1951**, after this era. The 1929 event is another row for the owner's sensitivity review. [S, Wikipedia] Not in Gvati. [checked 2026-10-23: no row] Village Statistics 1945
  has a unit "‘Ein Zeitim" separate from the Arab village's unit, printed "‘Ein ez Zeitin" in the
  text layer; the figures of the "‘Ein Zeitim" line are not legible there.

### Mishmar HaYarden, the colony
- he.wiki `משמר הירדן (מושבה)`, oldid **43882330**: infobox `סוג יישוב=יישוב לשעבר`, `תאריך
  ייסוד=1890`, `סיבת נטישה=כיבוש והרס על ידי כוחות סוריה`, `תאריך נטישה=10 ביוני 1948`, successors
  `גדות` and `משמר הירדן (מושב)`, and **33.01380, 35.62084** in the infobox. Body: "המושבה משמר הירדן
  נוסדה בתחיל [sic] ה'תרנ"א (1890)". [S, Wikipedia] Village Statistics 1945 names it "Mishmar hay Yarden". (24 Oct) Census of Palestine 1931, Safad sub-district, printed p. 108: "Mishmár hay Yardén", 23 occupied houses, 95 persons (48 male, 47 female); Muslims 16 + 14 = 30; Jews 32 + 33 = 65 (the two Jewish figures are read from a 200 dpi crop and are slightly blurred; 30 + 65 = 95 agrees with the total). Presence in 1931 of a Jewish community of 65, not a founding date.
- CBS row 732 `משמר הירדן` is the moshav, year **1949**, and Gvati's `6.12.49` row is the same moshav;
  neither dates the colony. Founded **1890** [S, Wikipedia; presence in 1945 from Village Statistics];
  abandoned **10 June 1948**, after 15 May. A row for the owner's sensitivity review.

### Beit Gan (merged into Yavne'el)
- he.wiki `בית-גן`, oldid **43838436**: land bought "בשנת 1903"; "בשנת 1905 נבנה הרחוב העליון של
  המושבה"; one of seven ICA colonies of 1902 to 1908; "צורפה למועצה המקומית יבנאל בשנת 1953". Village
  Statistics 1945 lists "Beit Gan" as its own land unit in Tiberias. [S, Wikipedia]
- **Founding year: [GAP]**, between 1903 (land) and 1905 (first street). Merged **1953**. [S, Wikipedia]
- (24 Oct) The he.wiki article's footnotes to the land purchase give "בשנת 1903" and, citing Ze'ev Vilnai, "או באלול תרס"ד - ספטמבר 1904" (Madrich Eretz Israel, Galilee); a second footnote quotes Vilnai's Encyclopaedia (1956) as "נוסד באלול תרס"ד (נובמבר 1903)" and adds the Wikipedia editor's remark that Elul 5664 fell in August or September, not November. Neither Vilnai work was opened here. **[C, secondary]** land bought 1903 against Elul 5664 (August to September 1904); the founding year stays a [GAP] until a source dates the first settlers rather than the land. Census of Palestine 1931, Tiberias sub-district, printed p. 82: "Beit Gan", 27 occupied houses, 134 persons (72 + 62), all in the Jewish columns, and a separate entry "Bitanya" (11 houses, 35 persons, 29 + 6 Jews) that was not matched to Bitaniya Illit or Bitaniya Tahta and is not used. The town's own history book (`yavneel.org.il/site/files/beitgan_book.pdf`, cited by the article) returned HTTP 404 on 2026-10-24. Coordinate: no coordinate in the he.wiki article (searched its wikitext, 2026-10-24); the English Wikipedia API returned a missing page for "Beit Gan". [GAP]

### Mishmar HaShlosha (merged into Yavne'el)
- he.wiki `משמר השלושה`, oldid **43737620**: "המושב הוקם ביום 13 באפריל 1937 על אדמת פיק"א כיישוב
  חומה ומגדל", named for three Yavne'el residents killed on 14 March 1937, and attached to the Yavne'el
  local council in 1953. Village Statistics 1945: "Yavneel and Mishmar hash Shelosha". [S, Wikipedia;
  presence in 1945 from Village Statistics]
- Founded **13 April 1937**, merged **1953**. [S, Wikipedia]
- (24 Oct) Coordinate: 32.7123854, 35.503006, the `מיקום מפורט בישראל` template and `{{coord}}` of the he.wiki article (oldid 43737620, read 2026-10-24); the English Wikipedia "Mishmar HaShlosha" gives 32.7125, 35.50305556 (API, last revision id 1377764788). [S, Wikipedia] **Computed here:** about 0.4 km from CBS's Yavne'el point (32.70849, 35.50345): 433 m north-south, 42 m east-west, 435 m together. The article also says that in 1961 part of the members left, which "הביא לפירוק האגודה השיתופית ולחיסול הסופי של המושב", and that "כיום משמר השלושה הוא חלק אינטגרלי מהמושבה יבנאל"; its cited newspapers (Al HaMishmar 30 October 1961, HaBoker 7 May 1965) were not opened. [S, Wikipedia]

### Bitaniya Illit
- he.wiki `ביתניה עילית`, oldid **43555562**: "בא' באלול תר"ף (15 באוגוסט 1920) התיישבה על הרכס מעל
  חוות ביתניה ... קבוצה של בוגרי תנועת השומר הצעיר"; "בפסח תרפ"א (אפריל 1921) הם עזבו את המקום".
  [S, Wikipedia]
- Founded **15 August 1920**, left **April 1921**. [S, Wikipedia] A work camp more than a locality; included because
  the issue asks for places abandoned before 1948.
- (24 Oct) Coordinate: 32.7145656, 35.5481945, the template and `{{coord}}` of the he.wiki article (oldid 43555562, read 2026-10-24). [S, Wikipedia] The English Wikipedia API returned a missing page for "Bitaniya Illit".

## 4. Gaps and what was not done

- [GAP] **Completeness of part 3.** Part 3 lists the abandoned and merged places found from the CBS
  rows' own histories and the 1945 land units. It is not shown to be complete. Village Statistics
  1945 also names "Hatsor" (Safad), "Shorashim" and "Poriya" (Tiberias), land units that may hold a
  Jewish locality; (24 Oct) their land units were read in the Village Statistics 1945 page images (archive.org, pages n68, n71 and n72 of the scan, printed Table I, columns Population and Total Land Areas, checked 2026-10-24) and none of the three is shown to be a locality in the sense of this note except Poriya: **Hatsor** (Safad, printed p. 69) has no population entry and land of Arabs 2, Jews 2,104, public 113, total 2,219 dunums, Jewish-owned land with nobody counted; **Shorashim** (Tiberias) has no population entry and land of Arabs 1,297, Jews 2,253, public 115, total 3,665; **Poriya** (Tiberias) has Jews 130 and total 130 population (Arabs blank) and land of Jews 2,909, public 285, total 3,194, a Jewish settlement of 130 people in 1945 that is not a CBS row under that name (the Poria estate, where Alumot lived from autumn 1940 per he.wiki above; **our reading:** it is the Poria estate, not shown by the table). Poriya is the lead for a follow-up; whether its people were a founded locality, a workers' camp or the Alumot group is a [GAP], and Hatsor and Shorashim stay out. The text layer had printed "Poriya 130 130" without saying which column it was; the page image settled it.
- [GAP] **Census of Palestine 1931**: the archive.org scan has no usable OCR text. (24 Oct) A few rows were read from page images (Beit Gan, Bitanya, Kefar Gil'adi, Metullá, Mishmár hay Yardén, Tel Hai; see How it was done); every other Galilee row has no 1931 check.
- [GAP] **Coordinates of abandoned places**: (24 Oct) Ein Zeitim, the Mishmar HaYarden colony, Tel Hai, Mishmar HaShlosha and Bitaniya Illit now have one each, all from Wikipedia and none from a map sheet (Survey of Palestine sheets and GovMap were not opened). Gei Oni (the he.wiki title `גיא אוני` is a redirect page of 28 characters and carries no coordinate), the first Mahanayim, Hamara, Beit Gan and Mishmar HaYam have none.
- **Gvati day versus CBS year.** Where they agree on the year the row is [V]; the day rests on Gvati
  alone, and as `settlements-1949-51.md` notes, it can be a day or more off other sources.
- **Not done:** no hex, no game date, no rule for which of two dates the game should use. Those
  belong to #61 and to the owner's sensitivity review.
