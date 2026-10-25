# Jewish localities founded in Haifa and the Carmel before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`.
Refs #97, its Haifa and the Carmel region (claimed in
https://github.com/eyalgolan/statecraft-israel/issues/97#issuecomment-6361196386). The dated
settlements file and `tools/settlement_hexes.py` stay with #61, and rows with a rename or an
abandonment date need the owner's sensitivity review before they enter the game; none is proposed
here. Method, sources and tags are those of the Galilee note (#1429) and the Negev note (#1403).

Update 2026-10-24: the [GAP] rows were worked again by an agent (Claude Code) for #97 (owner's 24 Oct
"Research part open" comment). Anything opened that day is marked "(24 Oct)". A person's check is
still pending, and no row below was moved to [V] by this update. New tag used once for coordinates:
**[S, Wikidata]**, a single source that is a Wikidata statement read through its API.

## How it was done

- Every page was fetched on 23 October 2026 with the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024,
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23),
  columns `שם נפה`, `אזור טבעי`, `דת יישוב`, `שנת ייסוד` and `קואורדינטות`. The twelve-digit
  coordinate is read as an Israel Transverse Mercator easting and northing of six digits each and
  converted with `pyproj` (EPSG:2039 to EPSG:4326), the reading `settlements-1949-51.md`, #1403 and
  #1429 use. The file does not state that reading, so every lat/lon below is **computed here** from
  the CBS string, not printed by CBS.
- **Founding dates.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix, Ben-Yehuda Project
  edition, https://benyehuda.org/read/14684 (checked 2026-10-23). Each row gives name, geographic
  region, date of founding, affiliation (kibbutzim and moshavim only) and the origin of the name.
  The column does not say whether a date is the day of going onto the land or a later
  recognition. Dates are quoted exactly as printed, for example `6.12.1882` or `8.36`.
- **Village Statistics 1945** (Government of Palestine), the archive.org scan
  https://archive.org/details/palestine-village-statistics-1945 and its text layer
  `Village Statistics 1945_djvu.txt` (checked 2026-10-23), Table I "Population and total land
  areas" for the **Haifa sub-district**, which in 1945 also covered Zikhron Ya'akov, Atlit and the
  Hadera area. The text layer prints page numbers 49 and 50 at the foot of the table's second and
  third pages; the first page's number is not legible there. It is used only as a **presence
  check**: a Jewish locality named in a 1945 land unit existed by then. It is not a founding source,
  and it lists land units, so several kibbutzim and most of the Krayot are not named at all; "not
  listed by name" therefore proves nothing. The text layer drops blank columns, so a lone population
  figure cannot be assigned to the Arab or Jewish column; figures are given only where all three
  columns (Arabs, Jews, Total) survive. (24 Oct) The page images of the same item were opened
  (`https://archive.org/download/palestine-village-statistics-1945/page/n46_w1400.jpg`, `n47` and
  `n48`, printed pages 47 to 49 of the Haifa sub-district table), and the columns were read directly
  for the rows in section 1b. The editor's notes of the 1970 edition were not used.
- **Census of Palestine 1931**: (24 Oct) the PDF of the archive.org item (https://archive.org/details/CensusOfPalestine1931.PopulationOfVillagesTownsAndAdministrativeAreas, file `PalestineCensus1931.pdf`; 124 pages, 29.6 MB) was
  downloaded and read. Its embedded OCR is still unusable for the figures, but the pages render
  legibly as images (Ghostscript `-sDEVICE=pnggray -r220`), so the Haifa sub-district table
  ("Sub-District Haifa", PDF pages 98 to 109, printed pages 87 to 98; printed number = PDF page minus
  11) was read by eye. Only the rows in section 1b were read; every other row of that table is
  unread here. [GAP] for those. Earlier note: #1429 found the scan has no usable text layer.
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

## What counts as Haifa and the Carmel here

There is no single boundary, so this note states its rule.

Included: every locality that the CBS file codes as Jewish (`דת יישוב` = 1) with `שנת ייסוד` before
1948 in the **Haifa sub-district** (all of it is CBS natural region 311; 14 rows), plus the rows of
the **Hadera sub-district** in CBS natural regions **321** and **322** (8 rows). The file has no
name column for natural regions; **our reading** of their names comes from the file's own
statistical-area rows, `חוף הכרמל מ"א 15` (code 1864) in region 321 and `אזור זכרון יעקב מ"א 15`
and `אזור זכרון יעקב של"ש` (codes 1866, 1867) in region 322, so this note calls them the Carmel
coast and the Zikhron Ya'akov area. Haifa itself is coded `דת יישוב` = 4 and `ותיק` (long
established) instead of a year; it gets one line as a pre-1882 town and no founding date is sought.
Places that stood in the same area before 15 May 1948 and were later abandoned or merged into
another locality are in part 3.

Excluded:
- The rest of the Hadera sub-district (natural regions 323 and 324: Hadera, Binyamina-Giv'at Ada,
  Pardes Hanna-Karkur, Gan Shemu'el, Ein Shemer and others), left for a coastal plain and Sharon
  note.
- Kefar Masaryk, Ein HaMifratz and Afek, which CBS places in the Acre sub-district. The 1945 Haifa
  table names the first two (in the unit "‘Emeq Zevulun (Jidru Ghawarina), Kefar Masaryk and ‘Ein
  ham Mifrats"); Afek is not named in it. All three are in the Galilee note (#1429) and are not
  counted again.
- The Ramot Menashe kibbutzim that Gvati files under `רמת מנשה` (for example Ein HaShofet, 1937,
  and Ramat HaShofet, 1941), which CBS places in the **Jezreel** sub-district; they are for a valleys
  note.
- By date: HaHotrim and Nahsholim, the two CBS 1948 rows of region 321. Gvati dates them `7.6.48`
  and `14.6.48`, after 15 May 1948.

## 1. The CBS localities, one row each

Read every row as: CBS name (CBS transliteration); CBS locality code; CBS founding year; the
coordinate computed from the CBS string; Gvati's row (region, date as printed, affiliation); the
1945 land unit that names it, if any; the tag. All CBS cells come from bycode2024.xlsx, all Gvati
cells from https://benyehuda.org/read/14684, all 1945 cells from the archive.org text layer cited
above (all in the Haifa sub-district table), each checked 2026-10-23. Rows tagged "part 2" have a
disagreement, a rename, a move or an earlier founding written out below.

### Haifa sub-district

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| יגור (Yagur) | 96 | 1922 | 32.74204, 35.07720 | עמק זבולון, `30.12.22`, הקיבוץ המאוחד | Haifa: "Mesheq Yagur (includes Mesheq)" | [V]; earlier camp 1920 to 1921, part 2 |
| כפר חסידים א' (Kefar Hasidim Alef) | 112 | 1924 | 32.75051, 35.09274 | עמק זבולון, `10.24`, (affiliation cell blank) | Haifa: "Kefar Hasidim" | [V]; part 2 |
| נשר (Nesher) | 2500 | 1925 | 32.76851, 35.04167 | [GAP] no row | Haifa: "Nesher" (Arabs 1,430, Jews 1,400) | [C] he.wiki 1923; part 2 |
| קריית אתא (Qiryat Atta) | 6800 | 1925 | 32.80423, 35.10871 | [GAP] no row | Haifa: "Kefar ‘Atta (Kufritta)" | [S] CBS only for the year; part 2 |
| רמת יוחנן (Ramat Yohanan) | 178 | 1932 | 32.79349, 35.12215 | עמק זבולון, `7.11.32`, האיחוד | Haifa: "Ramat Yohanan" | [C] he.wiki 1931; part 2 |
| כפר ביאליק (Kefar Bialik) | 220 | 1934 | 32.82131, 35.08654 | עמק זבולון, `9.7.34`, האיחוד החקלאי | not listed by name | [V] |
| קריית ביאליק (Qiryat Bialik) | 9500 | 1934 | 32.83853, 35.09019 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |
| קריית מוצקין (Qiryat Motzkin) | 8200 | 1934 | 32.83620, 35.08012 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |
| שער העמקים (Sha'ar HaAmaqim) | 237 | 1935 | 32.72308, 35.11270 | עמק יזרעאל, `2.6.35`, הקיבוץ הארצי | Haifa: "Sh’ar ha ‘Amaqim (previously part of Esh Sheikh Bureik)" | [V] |
| כפר המכבי (Kefar HaMakkabbi) | 254 | 1936 | 32.79007, 35.11595 | עמק זבולון, `25.10.36`, האיחוד | Haifa: "Kefar ham Maccabi" | [V] |
| אושה (Usha) | 278 | 1937 | 32.79625, 35.11462 | עמק זבולון, `8.36`, האיחוד | Haifa: "Usha" | [C] CBS 1937, Gvati 1936; part 2 |
| כפר הנוער הדתי (Kefar HaNo'ar Hadati) | 890 | 1937 | 32.74303, 35.09828 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |
| קריית טבעון (Qiryat Tiv'on) | 2300 | 1937 | 32.71523, 35.12580 | [GAP] no row | Haifa: "Qiryat ‘Amal (previously part of Esh Sheikh Bureik)", "EI] Ro-i" [sic] and "Qiryat Haroshet", see part 2 | [S] CBS only for the year; parts founded 1935 to 1947, part 2 |
| קריית ים (Qiryat Yam) | 9600 | 1941 | 32.84880, 35.07206 | [GAP] no row | not listed by name | [S] CBS only for the year; part 2 |
| חיפה (Haifa) | 4000 | ותיק | 32.80375, 35.00929 | [GAP] no row | Haifa: "Haifa (Urban) (includes Ahuzzat Sit Herbert Samuel)" [sic] (Arabs 62,800, Jews 75,500) | pre-1882 town; no founding date sought |

### Hadera sub-district, Carmel coast (natural region 321)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| עתלית (Atlit) | 53 | 1904 | 32.69205, 34.94336 | חוף הכרמל, `1904`, (no affiliation column) | Haifa: "‘Atlit" (Arabs 150, Jews 510) | [C] he.wiki 1903; part 2 |
| נווה ים (Newe Yam) | 312 | 1939 | 32.67917, 34.93064 | חוף הכרמל, `23.5.39`, האיחוד | not listed by name | [V]; first named Ma'apilim, part 2 |
| עין כרמל (En Karmel) | 426 | 1947 | 32.67728, 34.95279 | חוף הכרמל, `1947`, הקיבוץ המאוחד | not listed by name | [C] he.wiki 1948 or 1950; part 2 |

### Hadera sub-district, Zikhron Ya'akov area (natural region 322)

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| זכרון יעקב (Zikhron Ya'aqov) | 9300 | 1882 | 32.57045, 34.95265 | השומרון, `6.12.1882`, (no affiliation column) | Haifa: "Zikhron Ya‘aqov" | [V]; first named Zamarin, part 2 |
| בת שלמה (Bat Shelomo) | 33 | 1889 | 32.60043, 35.00355 | רמת מנשה, `6.1889`, (no affiliation column) | Haifa: "Bat Shelomo" | [V] |
| מאיר שפיה (Me'ir Shefeya) | 102 | 1923 | 32.59123, 34.97131 | [GAP] no row | Haifa: "Meir Shefeiya" | [C] he.wiki 1891; part 2 |
| מעיין צבי (Ma'yan Zevi) | 290 | 1938 | 32.56817, 34.94065 | חוף הכרמל, `30.8.38`, האיחוד | not listed by name | [V]; moved 1942, part 2 |
| בית אורן (Bet Oren) | 317 | 1939 | 32.73091, 35.00490 | הר הכרמל, `1.10.39`, הקיבוץ המאוחד | not listed by name | [V]; first site 1939, moved 1940, part 2 |

### 1b. Presence checks read from page images (24 Oct)

Both sources are used as presence checks only, as above. 1931 columns: occupied houses, persons,
and Jews (male + female); each row's religion columns add up to its persons column (checked here).
1945 columns: Jewish population, and Jewish-owned land in dunums, from the same row.

| Locality | Census 1931 (printed page; houses; persons; Jews) | Village Statistics 1945 (printed page; Jews; Jewish land) |
|---|---|---|
| Atlit | p. 87; 193; 948; 287 + 209 = 496 | p. 47; 510 (Arabs 150); 5,262 |
| Ahuzat Sir Herbert Samuel | p. 87; 42; 97; 55 + 42 = 97 | inside "Haifa (Urban)" |
| Bat Shelomo | p. 88; 19; 86; 26 + 25 = 51 | p. 47; 90; 7,501 |
| Haifa | p. 91; 12,351; 50,403; 8,232 + 7,691 = 15,923 | p. 48; 75,500 (Arabs 62,800); 27,623 |
| Kefar Hassidim | p. 93; 104; 420; 212 + 208 = 420 | p. 48; 980; 16,408 |
| Kufritta (Kefar 'Atta) | p. 94; 13; 33; 17 + 12 = 29 | p. 48; 1,690; 5,194 |
| Shefeiya | p. 96; 18; 208; 92 + 76 = 168 | p. 48 "Meir Shefeiya"; 330; 2,497 |
| Yajur, with the sub-entries Nesher Factory, Nesher Labourers Group, Nesher Station, Mesheq Yajur | p. 97; 291; 1,449; 471 + 387 = 858 | p. 48 "Mesheq Yagur"; 1,220; 4,084. p. 48 "Nesher"; 1,400 (Arabs 1,430); 2,748 |
| Zikhron Ya'aqov | p. 98; 191; 1,429; 601 + 591 = 1,192 | p. 49; 1,740; 11,860 |

Rows read in 1945 only: Kefar ham Maccabi (p. 48; 210; 1,660), Usha (p. 49; 180; 894), Ramat Yohanan
(p. 49; 420; 3,536), Sh'ar ha 'Amaqim (p. 49; 360; 2,676), Qiryat 'Amal (p. 48; 530; 2,832), "El Ro-i"
(p. 47; 360; 569), Qiryat Haroshet (p. 49; 240; 715), "Tiv'on (Alonim) (previously Qusqus-Tab'un)"
(p. 49; Jews 320, Arabs 370; 5,771) and "Ya'arot hak Karmel (previously Khirbat Shallala)" (p. 49; 360;
6,213). Each row's land columns add up to its total (for example Zikhron Ya'aqov 13 + 11,860 + 698 =
12,571; Ya'arot hak Karmel 64 + 6,213 = 6,277).

Our reading of the 1931 table: in the pages read, Nesher is not a row of its own; its factory, labourers' group
and station sit under Yajur. Kufritta had 13 houses and 29 Jews in 1931, against 1,690 Jews in
1945; that fits the near-abandonment of 1929 and the resettlement of 1930 that he.wiki describes (part
2), but the table does not say why. The 1931 row "Shefeiya" is read as the Meir Shefeya area because
its Jewish columns are large (168), but the row does not name the colony.

## 2. Disagreements, renames and moves for CBS localities

Hebrew Wikipedia pages below were read at the `oldid` given, checked 2026-10-23.

### Zikhron Ya'akov, first named Zamarin (CBS 9300)
- CBS **1882**; Gvati `6.12.1882`. [V]
- he.wiki `זכרון יעקב`, oldid **43995281**: "היא נוסדה כמושבה ב-6 בדצמבר 1882 בידי עולים חובבי ציון
  מרומניה"; "בהתחלה, נקרא שמה של המושבה 'זמרין'"; "הברון רוטשילד נתן למקום את השם 'זכרון יעקב'
  על שם אביו ג'יימס (יעקב) מאיר רוטשילד, בעת שחנך את בית הכנסת שנבנה בלב המושבה". [S, Wikipedia]
- **Rename** Zamarin to Zikhron Ya'akov; the year of the rename is not given in the passages read.
  [GAP] (24 Oct) The same article, in its account of 1883, quotes a *HaMagid* item of 28 Elul 5643
  (1883) under the headline "נדיב בלתי-ידוע מציל את זכרון יעקב", so by that article the name
  Zikhron Ya'akov was already in print in 1883. *HaMagid* itself was not opened. [S, Wikipedia]
  The year the name was given stays [GAP].

### Atlit (CBS 53)
- CBS **1904**; Gvati `1904`.
- he.wiki `עתלית`, oldid **43769429**: "עתלית המודרנית נוסדה בשנת 1903 כמושבה לאחר שהברון רוטשילד
  רכש את אדמות האזור בשנת 1899"; "בשנת 1903 נתן הברון רוטשילד הוראה ליישב את עתלית בכל תנאי".
  [S, Wikipedia]
- **[C]** CBS and Gvati 1904 against he.wiki 1903. **Our reading:** two sources against one; use 1904,
  record he.wiki's 1903 (the year of the Baron's order) and the land purchase of 1899.

### Meir Shefeya (CBS 102)
- CBS **1923**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `מאיר שפיה`, oldid **42501044**: "הוא הוקם בשנת 1891 כמושבת בת של זכרון יעקב הקרובה, בשם
  'מאיר שפיה'"; "בשנת 1923 הועבר המעון לשפיה"; and "בין השנים 1939–1945 נטשו רוב איכרי המושבה את
  בתיהם ועברו לנחלות חלופיות ב-זכרון יעקב, ושטח המושבה כולו הפך לחלק מכפר הנוער". [S, Wikipedia]
- **[C]** CBS 1923 against he.wiki 1891. **Our reading:** two different things. 1891 is the colony,
  1923 the children's home that became the present youth village; CBS most likely dates the
  youth village, but no source read here says so.
  Most of the colony's farmers left **between 1939 and 1945** (no single date). A row for the owner's
  sensitivity review.

### Ma'yan Zvi (CBS 290)
- CBS **1938**; Gvati `30.8.38`. [V]
- he.wiki `מעיין צבי`, oldid **43795823**: "היישוב הוקם ב-30 באוגוסט 1938 כיישוב חומה ומגדל";
  "בשנת 1942 עברו למקום הנוכחי בגלל מחלת הקדחת שנבעה מקרבתם לביצות". [S, Wikipedia]
- **Move date** 1942. CBS's coordinate is the present site; the 1938 site has no coordinate here.
  [GAP]
- (24 Oct) Same article, of the first site: "היישוב הוקם בתחילתו בצומת כביש 4 עם הכביש המוליך לתחנת
  הרכבת של זכרון יעקב, בקרבת האזור בו היו שאריות ביצות כבארה כישוב חומה ומגדל". That is a description,
  not a coordinate; the coordinate stays [GAP]. [S, Wikipedia]
- **Rename** (24 Oct): "בתחילה נקרא הקיבוץ 'מעין', על שם מעין הנמצא באזור בנימינה, ליד מבצר שוני.
  בשנת 1945 הוסף לשמו 'צבי'". So Ma'yan became Ma'yan Zevi in **1945**. [S, Wikipedia] Gvati's
  name-origin cell for the row reads "ע"ש צבי (הארי) פרנק שהיה מנהל פיק"א בארץ" (named for Zevi
  (Harry) Frank, who was PICA's director in the country), which agrees on the person but gives no
  year.

### Bet Oren (CBS 317)
- CBS **1939**; Gvati `1.10.39`. [V]
- he.wiki `בית אורן`, oldid **43555492**: "ב-1 באוקטובר 1939 עלה הגרעין המייסד לח'ירבת שלאלה כישוב
  הלפני אחרון ביישובי 'חומה ומגדל'"; "הקבוצה עברה לבית אורן באוקטובר 1940". [S, Wikipedia]
- **Move date** October 1940. Village Statistics 1945 has a land unit "Ya‘arot hak Karmel (previously
  Khirbat Shallala)". **Our reading:** its former name is the site of the 1939 point, so it may be
  the land unit of the first site; that link is not shown by any source read here.
- (24 Oct) The link is now shown by two sources read together. The he.wiki Bet Oren passage above
  puts the 1939 ascent at "לח'ירבת שלאלה", and Village Statistics 1945 (printed p. 49, read as an
  image) lists "Ya‘arot hak Karmel (previously Khirbat Shallala)" as a Jewish land unit (Jews 360;
  Arab land 64, Jewish land 6,213 dunums, total 6,277). he.wiki `חוות משמר הכרמל`, oldid **42420180**,
  opens "(בעבר נקראה יערות הכרמל, וכן ידועה גם בשם ח'ירבת שלאלה)" and says of the 1939 group: "הם
  התיישבו במבני החווה שעל הגבעה ביחד עם משפחת ברגר שנותרה במקום"; "הקבוצה עברה לבית אורן באוקטובר
  1940"; and that the farm lies "כקילומטר מקיבוץ בית אורן". [S, Wikipedia] The same article says the
  hill was bought in 1934 by a group of Haifa Jews for a planned settlement named Ya'arot HaCarmel,
  and that a religious pioneer group, Mishmar HaCarmel, went up on 4 October 1935 ("בערב שבת שובה
  תחילת שנת תרצ"ו (4 באוקטובר 1935)"), so Ya'arot HaCarmel is an earlier Jewish settlement on the
  same hill, not Bet Oren itself. [S, Wikipedia] Its end, two he.wiki passages that
  differ. The farm article (oldid **42420180**) says in its lead: "היישוב נעזב אחרי מאורעות
  תרצ"ו-תרצ"ט בעקבות החלטת המוסדות המיישבים" (the settlement was abandoned after the 1936 to 1939
  events, following a decision of the settling bodies). The disambiguation page `יערות הכרמל`, oldid
  **42620318**, says it was a Jewish settlement "שפונה בתחילת מלחמת העצמאות" (evacuated at the start of
  the War of Independence). **[C]** after 1936 to 1939 against the start of 1948; no day in either.
  Village Statistics 1945, which post-dates the first end, still lists 360 Jews at "Ya‘arot hak Karmel".
  [S, Wikipedia] The farm's
  coordinate is Wikidata Q16132058, 32.72594722, 35.01347222 (read 2026-10-24, [S, Wikidata]).
  **Computed here:** against CBS's Bet Oren point (32.73091, 35.00490) that is 0.55 km south and 0.80 km
  east (0.00496 degrees of latitude at 111.2 km, 0.00857 degrees of longitude at 93.6 km, 32.7 N),
  square root of the sum of squares 0.97 km, which fits the article's "about a kilometre".

### Newe Yam, first named Ma'apilim (CBS 312)
- CBS **1939**; Gvati `23.5.39`. [V]
- he.wiki `נווה ים`, oldid **44083397**: "הקיבוץ נקרא בתחילה מעפילים, והתמקם על הגבעה שעליה נמצא כיום
  פארק המים, ולאחר כמה שנים עבר למיקומו הנוכחי". [S, Wikipedia]
- **Rename** Ma'apilim to Newe Yam and a **move**, neither dated in the passage above. [GAP]
  (24 Oct) The same article, further on: "ב-1947 קיבל הקיבוץ את שמו הנוכחי". So the rename is dated
  **1947**. [S, Wikipedia] The move "after a few years" has no year; the first site is "the hill
  where the water park now is", with no coordinate. [GAP]

### Ein Carmel (CBS 426)
- CBS **1947**; Gvati `1947`.
- he.wiki `עין כרמל`, oldid **43556192**: "קיבוץ עין כרמל נוסד על ידי מתיישבים מעין הים ומרמת רחל";
  "מגילת היסוד של היישוב נחתמה בשלהי מלחמת העצמאות", and the caption of its founding charter,
  "נחתמה בדצמבר 1948". he.wiki `עין הים`, oldid **41769189**: "בשנת 1950 הקימו חברי עין הים ביחד עם
  חברים יוצאי רמת רחל את קיבוץ עין כרמל". [S, Wikipedia]
- **[C]** CBS and Gvati 1947 against he.wiki December 1948 (charter) or 1950 (founding). **Our
  reading:** the two Wikipedia pages disagree with each other as well, and both put the kibbutz after
  15 May 1948. CBS and Gvati are two sources for 1947, but neither says what happened in 1947. The
  row should not enter a pre-1948 list until a dated source settles it; its predecessor Ein HaYam is
  in part 3.

### Usha (CBS 278) and Ramat Yohanan (CBS 178)
- Usha: CBS **1937**; Gvati `8.36`. he.wiki `אושה`, oldid **42228936**: "קיבוץ אושה נוסד בקיץ 1937
  בשם אושא ..."; "באוגוסט 1937 נמצאה פשרה ... ושני הפלגים עלו להתיישבות במרחק של כמה מאות מטרים זה
  מזה"; and of the name, "תחילה, בראשית שנות השלושים, ניתן השם לקיבוץ הנקרא היום רמת יוחנן".
  [S, Wikipedia]
- **[C] Usha:** Gvati August 1936 (`8.36` read as month.year, like `6.1889` and `10.24`) against CBS 1937 and he.wiki August 1937. **Our reading:** use
  August 1937; CBS and the article agree on the year, and Gvati's month matches the article's month
  a year earlier, which may be a misprint, but no source read here says so.
- Ramat Yohanan: CBS **1932**; Gvati `7.11.32`. he.wiki `רמת יוחנן`, oldid **42229041**: "בשמחת תורה
  ה'תרצ"ב (1931) עלו ראשוני קבוצת הצפון המאוחדת להתיישבות על אדמות אל-מג'דל"; "המקום נקרא 'רמת
  הצפון' ובמשך שנתיים נתנה הקרן הקיימת למקום גם את השם 'אושא'". [S, Wikipedia]
- **[C] Ramat Yohanan:** CBS and Gvati 1932 against he.wiki Simhat Torah 5692 (1931; the day in the
  civil calendar is not given in the article). **Our reading:** two sources against one; use
  7 November 1932 and record the 1931 ascent as the group's first arrival. **Renames:** רמת הצפון and
  אושא before רמת יוחנן; years not given. [GAP]

### Yagur (CBS 96)
- CBS **1922**; Gvati `30.12.22`. [V]
- he.wiki `יגור`, oldid **43790258**: "בטרם נוסד הקיבוץ חנו ועבדו בקרקעותיו למשך כשנה (1920–1921) אנשי
  'השומר הצעיר' חברי גדוד שומריה"; "מתחילת שנת תרפ"ג (סוף 1922) התגוררה הקבוצה בחושות של 'סלמיה'";
  "כשנגשו להקמת המפעל בקיץ 1923 הם נתבקשו לעזוב את המקום ועברו לנקודת הקבע לקראת תחילת תרפ"ד".
  [S, Wikipedia]
- **Our reading:** 30 December 1922 is the founding; the 1920 to 1921 camp is an earlier stage, and the
  move to the permanent site came in 1923 (around the start of the Hebrew year 5684, autumn 1923).

### Kfar Hasidim (CBS 112)
- CBS **1924**; Gvati `10.24` (the affiliation cell is blank). [V]
- he.wiki `כפר חסידים`, oldid **43319822**: "תחילתו של כפר חסידים בה'תרפ"ד (1924) ... בשתי התארגנויות
  של חסידים בפולין"; "באפריל 1925, עוד טרם הרישום של רכישת הקרקעות בטאבו, עלתה קבוצה ראשונה של
  חסידים ... על הקרקע בנקודה שנקראה 'נחלת יעקב'"; and the caption "נחלת יעקב ב-1926, אחד היישובים
  שאוחדו לאחר מכן לכפר חסידים". [S, Wikipedia]
- **Our reading:** 1924 is the year the groups organised and bought land; the first group went onto
  the land in April 1925. Nahalat Ya'akov is in part 3.

### Nesher (CBS 2500)
- CBS **1925**. Not in Gvati's appendix (a town). [checked 2026-10-23: no row]
- he.wiki `נשר (עיר)`, oldid **43995563**: "הצריפים הראשונים של שכונת הפועלים, המסמלים את ייסוד
  היישוב העברי במקום, נבנו בסוף שנת 1923 ובתחילת שנת 1924"; "ב־18 באוקטובר 1925 הופעל הכבשן הראשון
  בבית החרושת". [S, Wikipedia]
- **[C]** CBS 1925 against he.wiki end of 1923. **Our reading:** 1923 is the first workers' huts, 1925
  the year the cement works started; no source read here says which one CBS dates. Keep both.

### Kiryat Ata, first named Kfar Ata (CBS 6800)
- CBS **1925**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `קריית אתא`, oldid **44062796**: "קריית אתא הוקמה בשנת 1925, על ידי חברת 'עבודת ישראל',
  כמושבה בשם 'כפר עטא'"; "במאורעות תרפ"ט נפגעה המושבה קשות על ידי פורעים ערבים וננטשה כמעט כליל.
  בשנת 1930 חזרו אליה התושבים וחידשו את היישוב במקום"; "ולכן שונה שם המושבה ל'כפר אתא'"; "בשנת
  1965 הוחלט על איחודה של קריית אתא עם המועצה המקומית קריית בנימין הסמוכה. היישוב המאוחד נקרא
  'קריית אתא'". [S, Wikipedia] Village Statistics 1945 names the unit "Kefar ‘Atta (Kufritta)".
- **[S]** year 1925: CBS, with he.wiki agreeing. **Almost entirely abandoned** in the 1929 riots (no exact date in
  the passage) and **resettled 1930**. **Renames:** כפר עטא to כפר אתא (after the textile works of
  1934; year of the rename not given) [GAP], and to קריית אתא at the merger decided in 1965. Rows for
  the owner's sensitivity review.
- (24 Oct) Searched again for the year of the rename Kfar Atta to Kfar Ata and not found. Read: he.wiki
  `קריית אתא`, oldid **44062796**, again; it says the colony was renamed after the textile works built
  in 1934 and prints no year for the rename. Its footnote for the 1941 local council cites *HaTzofe*,
  27 June 1941, "פתיחת המועצה המקומית בכפר עטה" (spelled with a final ה), which is a contemporaneous
  spelling and a date for the council, not for the rename. That item sits on the National Library of
  Israel newspaper site, and `https://www.nli.org.il` and `https://www.nli.org.il/en/newspapers`
  answered HTTP 403 to `curl` on 24 Oct, so it was not opened. Census 1931 (above) prints "Kufritta";
  Village Statistics 1945 prints "Kefar ‘Atta (Kufritta)". [GAP]

### Kiryat Bialik (CBS 9500)
- CBS **1934**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `קריית ביאליק`, oldid **43437798**: "קריית ביאליק נוסדה ב-18 ביולי 1934 כשכונה בסמיכות
  לחיפה"; "שמה הראשון היה 'שכונת עולי גרמניה'". [S, Wikipedia]
- **[S]** year 1934, day 18 July 1934 [S, Wikipedia]. **Rename** from שכונת עולי גרמניה; year not
  given. [GAP]
- (24 Oct) he.wiki, same revision: "משנפטר חיים נחמן ביאליק הוחלט לקרוא על שמו את שני היישובים
  הסמוכים, קריית ביאליק וכפר ביאליק". Bialik died on 4 July 1934 (he.wiki `חיים נחמן ביאליק`, oldid **43995182**, read
  2026-10-24; the same revision says "ביאליק נפטר בווינה ב-4 ביולי 1934"; infobox `תאריך פטירה = 4 ביולי 1934`). So the name Bialik dates from after 4 July
  1934, and the article's founding day, 18 July 1934, is 14 days later (18 minus 4); which name the
  neighbourhood carried on the day it was founded is not stated. [S, Wikipedia] **Our reading:** CBS's
  1934 holds under either name. The year of the first name's replacement stays [GAP].

### Kiryat Motzkin (CBS 8200)
- CBS **1934**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `קריית מוצקין`, oldid **43507011**: "ב-9 באוקטובר 1934 הונחה אבן הפינה לשכונה"; "ב-1935
  התיישבו המשפחות הראשונות בבתיהן שבשכונה". [S, Wikipedia]
- **Our reading:** CBS's 1934 is the cornerstone; the first families moved in during 1935.

### Kiryat Yam (CBS 9600)
- CBS **1941**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `קריית ים`, oldid **44022937**: "נוסדה בשנת 1941"; "גרעין המתיישבים הראשון כלל עשר משפחות,
  שהגיעו למקום החל מינואר 1941". [S, Wikipedia]
- **[S]** year 1941, with he.wiki agreeing; first families from January 1941.

### Kfar HaNo'ar HaDati (CBS 890)
- CBS **1937**. Not in Gvati's appendix (a youth village). [checked 2026-10-23: no row]
- he.wiki `כפר הנוער הדתי`, oldid **43034530**: "כפר הנוער הוקם בשנת 1937", with a footnote: "לפי
  אתר הבית של כפר הנוער, הנחת אבן הפינה הייתה ביום א' בתמוז תרצ"ז, 10 ביוני 1937". The village's
  own site was not opened here. [S, Wikipedia]

### Kiryat Tiv'on (CBS 2300)
- CBS **1937**. Not in Gvati's appendix. [checked 2026-10-23: no row]
- he.wiki `קריית טבעון`, oldid **44001013**: "ההתיישבות החדשה באזור החלה במחצית השנייה של שנות ה-30,
  כאשר נוסדו 'קריית-חרושת', 'אלרואי' ו'קריית-עמל' על ידי שיכון עובדים. כעשר שנים לאחר מכן, בשנת
  1947, נוסדה טבעון"; "בשנת 1957, סופחו למועצה של קריית עמל המעברה הסמוכה אליה, שיכון עמלים
  והיישוב אלרואי"; "בשנת 1958, אוחדו קריית-עמל וטבעון ליחידה מוניציפלית אחת בשם קריית טבעון".
  Of Elroi it says "היישוב נוסד בשנת 1935"; of Kiryat Haroshet, "קריית-חרושת נוסדה בשנת 1935".
  [S, Wikipedia]
- he.wiki `קריית עמל`, oldid **43691566**: "קריית עמל עלתה לקרקע ב-9 בספטמבר 1937". [S, Wikipedia]
- **[S]** CBS 1937. **Our reading:** CBS's 1937 matches the ascent of Kiryat Amal, the older of the
  two places whose names make up Kiryat Tiv'on; no source read here says that is what CBS dates. The
  parts are in part 3. Village Statistics 1945 lists "Qiryat ‘Amal (previously part of Esh Sheikh
  Bureik)", "EI] Ro-i (previously par of Esh Sheikh Bu-reik)" [sic, text layer] and "Qiryat
  Haroshet (previously part of Esh Sheikh Bureik)" as separate units, and a unit "Tiv'on (Alonim)",
  which is a land unit name and is not read here as the 1947 garden suburb.

## 3. Places that stood before 15 May 1948 and are not CBS localities today

These are the rows the issue calls renames and abandonment. Each is recorded with its source; nothing
is proposed about how it appears in the game. None is a CBS locality under its own name.
[checked 2026-10-23: no CBS row matches `קריית חיים`, `קרית חיים`, `קריית שמואל`, `קריית בנימין`,
`קריית עמל`, `אלרואי`, `קריית חרושת`, `עין הים` or `נחלת יעקב`.]

### Ein HaYam
- he.wiki `עין הים`, oldid **41769189**: "עין הים היה קיבוץ של הקיבוץ המאוחד שהתקיים בין השנים 1936
  ו-1950, ליד המושבה עתלית"; infobox `סוג יישוב = יישוב לשעבר`, `תאריך ייסוד = 1936`, `סיבת נטישה =
  הקמת קיבוץ חלופי עין כרמל`, `תאריך נטישה = 1950`. The article lists a *Davar* item, "חמש שנים לעין
  הים", 25 July 1941, not opened here. [S, Wikipedia]
- Founded **1936**, left **1950** for Ein Carmel, after this era. [S, Wikipedia] Not in Gvati.
  [checked 2026-10-23: no row] Coordinate: (24 Oct) Wikidata Q6766525, 32.690592, 34.94652 (read through
  `wbgetentities`, 2026-10-24; the item has no reference on the coordinate). [S, Wikidata] **Computed
  here:** 0.34 km from CBS's Atlit point (0.16 km south, 0.30 km east) and 1.59 km from CBS's Ein Carmel
  point; it is a point for the site as Wikidata gives it, not a boundary or a map reading.

### Nahalat Ya'akov (merged into Kfar Hasidim)
- he.wiki `כפר חסידים`, oldid **43319822**: the first group went onto the land "באפריל 1925 ... בנקודה
  שנקראה 'נחלת יעקב'", "אחד היישובים שאוחדו לאחר מכן לכפר חסידים". [S, Wikipedia]
- he.wiki `קריית טבעון`, oldid **44001013**, of the lands of Sheikh Abreik and Harithiya in the area of the present town: "במקום התיישבו בשנת 1924 קבוצות של
  חסידים (נחלת יעקב, עבודת ישראל), אך לא הצליחו להיאחז בקרקע, נטשו והקימו את כפר חסידים". [S,
  Wikipedia]
- **[C]** 1924 against April 1925 for the first settling, both Wikipedia. **Our reading:** use April
  1925, the dated statement. The date the point was left or merged is a [GAP]. Coordinate: [GAP]. (24 Oct) Searched again and not
  found. Read: Wikidata `wbsearchentities`, `search=נחלת יעקב`, `language=he`, `limit=5`, URL
  `https://www.wikidata.org/w/api.php?action=wbsearchentities&search=נחלת+יעקב&language=he&limit=5&format=json`,
  run 2026-10-24, which returned 2 items: Q12018804 (Downtown quarter of Haifa, a different place) and
  Q6511901 (a disambiguation page). Neither is the Nahalat Ya'akov of Kfar Hasidim.

### Kiryat Haim (merged into Haifa)
- he.wiki `קריית חיים`, oldid **44097134**: infobox `תאריך ייסוד = 1933`; "160 המתיישבים הראשונים ...
  נכנסו לבתיהם בתחילת 1933"; "ב-1950 סופחה קריית חיים לחיפה, יחד עם שכנתה קריית שמואל". [S,
  Wikipedia]
- Founded **early 1933**, annexed to Haifa **1950**. [S, Wikipedia] Coordinate: (24 Oct) two present-day quarter points, which
  differ: Wikidata Q2655674, 32.828333, 35.067222 [S, Wikidata], and the he.wiki infobox, oldid **44097134**,
  `{{coord|32|49|26.39|N|35|4|12|E}}`, which **computed here** is 32.82400, 35.07000 (49/60 + 26.39/3600 =
  0.82400; 4/60 + 12/3600 = 0.07000). They are about 0.55 km apart (0.48 km north-south, 0.26 km
  east-west). **Our reading:** both are centre points of the quarter, not the 1933 site.

### Kiryat Shmuel (merged into Haifa)
- he.wiki `קריית שמואל (חיפה)`, oldid **43128203**: "בניית התשתית לשכונה החלה בט"ו בתמוז תרצ"ה (16
  ביולי 1935)"; "נחנכו הבתים הראשונים בל"ג בעומר תרצ"ח (19 במאי 1938)"; "בשנת 1952 צורפה קריית שמואל
  כשכונה לעיר חיפה". [S, Wikipedia]
- **[C]** on the merger: 1952 in its own article against 1950 in the Kiryat Haim article, both
  Wikipedia. **Our reading:** both are after this era; keep both. Founding: infrastructure **16 July
  1935**, first houses **19 May 1938**. Coordinate: (24 Oct) Wikidata Q185687, 32.83401944, 35.06784167 [S, Wikidata], a point
  of the present neighbourhood.

### Kiryat Binyamin (merged into Kiryat Ata)
- he.wiki `קריית בנימין`, oldid **40121015**: "בינואר שנת 1937 (14.1.37) עלו על הקרקע ארבע המשפחות
  הראשונות"; "שבעבר הייתה יישוב נפרד עד שבשנת 1965 אוחדה עם כפר אתא וביחד היו לקריית אתא". [S,
  Wikipedia]
- Founded **14 January 1937**, merged **1965**. [S, Wikipedia] Coordinate: (24 Oct) Wikidata Q12411132, 32.79916667, 35.09388889
  [S, Wikidata], a point of the present neighbourhood.

### Kiryat Amal, Elroi and Kiryat Haroshet (merged into Kiryat Tiv'on)
- Kiryat Amal: "עלתה לקרקע ב-9 בספטמבר 1937" (he.wiki `קריית עמל`, oldid **43691566**); joined with
  Tiv'on as Kiryat Tiv'on in **1958** (he.wiki `קריית טבעון`, oldid **44001013**). [S, Wikipedia]
- Elroi: founded **1935**, attached to the Kiryat Amal council in **1957** (he.wiki `קריית טבעון`,
  oldid **44001013**). [S, Wikipedia]
- Kiryat Haroshet: he.wiki `קריית חרושת`, oldid **43792388**: "היא נוסדה ב-1935 ... עד לסיפוחה לקריית
  טבעון בשנת 1979"; "בשנת 1934 הניח טאוב את אבן הפינה". The Kiryat Tiv'on article says only that
  agreement was reached in April 1978. [S, Wikipedia]
- All three stood before 1948 (Village Statistics 1945 names each as a land unit, see part 2). (24 Oct)
  Coordinates, all [S, Wikidata] (statements read 2026-10-24; those of Kiryat Amal and Elroi are
  referenced "imported from" a Wikipedia edition, that of Kiryat Haroshet has no reference) and all
  points of today's neighbourhoods, not of the 1948 land units: Kiryat Amal Q12029237,
  32.70752778, 35.13075; Elroi Q11878352, 32.71066667, 35.10472222; Kiryat Haroshet Q139304838, 32.6925,
  35.1095. Village Statistics 1945 figures for the three units are in section 1b.

## 4. Gaps and what was not done

- (24 Oct) What this update closed: the 1931 and 1945 presence figures of section 1b; two rename years
  (Newe Yam 1947, Ma'yan Zevi 1945); seven coordinates (part 3, [S, Wikidata], neighbourhood points) and
  the first-site coordinate of Bet Oren (part 2); and the question whether "Ya‘arot hak Karmel" is Bet
  Oren's first site (it is the same hill, part 2). What it did not close is listed below.
- [GAP] **Completeness of part 3.** Part 3 lists the abandoned and merged places found from the CBS
  rows' own histories and the 1945 land units. It is not shown to be complete. Village Statistics
  1945 also names "Ya‘arot hak Karmel (previously Khirbat Shallala)" and "Giv‘ot Zeid (previously
  part of Esh Sheikh Bureik)"; the first may be Bet Oren's first site (part 2), for the second, CBS has no
  row of that name; neither was checked further. (24 Oct) Both were read in the 1945 page images (printed pp. 47 and 49,
  section 1b). "Giv‘ot Zeid" (Jews 110; Jewish land 1,581 dunums): he.wiki `גבעות זייד`, oldid **43158155**,
  calls it a kibbutz "באזור עמק יזרעאל, ליד בית זייד" that "הוקם ב-1940" and "פורק סופית ב-1957", so it
  belongs to the valleys note, not here (already in `settlements-pre-1948-valleys.md`, under Bet Zeid) [S, Wikipedia]
  (Wikidata Q5565575, property P571, gives 1940, the same year, read 2026-10-24 [S, Wikidata]). "Ya‘arot hak Karmel" is in part 2 under Bet Oren. The part 3 list is
  still not shown to be complete.
- [GAP] **Census of Palestine 1931**: (24 Oct) read for nine rows only (section 1b); the other rows of
  its Haifa sub-district table, and the Census as a source of founding dates, are not used.
- [GAP] **Coordinates of abandoned, moved and merged places**: (24 Oct) seven now have a Wikidata
  point (part 3) and the first site of Bet Oren has one (part 2). Still none: Nahalat Ya'akov, the first
  sites of Ma'yan Zevi and Newe Yam. No map sheet of the Survey of Palestine was read, so there is no
  coordinate with a sheet reference.
- [GAP] **Rename years**: (24 Oct) Ma'apilim to Newe Yam (1947) and Ma'yan to Ma'yan Zevi (1945) are now
  dated (part 2). Still open: Zamarin to Zikhron Ya'akov (in print by 1883, part 2), Kfar Atta to Kfar Ata,
  Ramat HaTzafon and Usha to Ramat Yohanan, and the first name of Kiryat Bialik (after 4 July 1934).
- **Gvati day versus CBS year.** Where they agree on the year the row is [V]; the day rests on Gvati
  alone, and as `settlements-1949-51.md` notes, it can be a day or more off other sources. Eight of
  the 22 rows (Nesher, the Krayot, Kiryat Tiv'on, Meir Shefeya and Kfar HaNo'ar HaDati) have no Gvati
  row, so their year rests on CBS with Wikipedia beside it. Haifa, outside the 22, has none either.
- **Not done:** no hex, no game date, no rule for which of two dates the game should use. Those
  belong to #61 and to the owner's sensitivity review.
