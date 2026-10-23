# Jewish localities of Jerusalem and its corridor founded before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`.
Refs #97, its Jerusalem and its corridor region (claimed in
https://github.com/eyalgolan/statecraft-israel/issues/97#issuecomment-6361897868). The dated
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
  #1429 and #1435 use. The file does not state that reading, so every lat/lon below is **computed
  here** from the CBS string, not printed by CBS.
- **Founding dates.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix, Ben-Yehuda Project
  edition, https://benyehuda.org/read/14684 (checked 2026-10-23). Each row gives name, geographic
  region, date of founding, affiliation (a movement for kibbutzim and moshavim, `מועצה מקומית` for the Moza rows) and the origin of the name.
  The column does not say whether a date is the day of going onto the land or a later
  recognition. Dates are quoted exactly as printed, for example `19.5.26` or `4.20`. The narrative
  chapter on the War of Independence in the same edition is used for the dates of 12 to 21 May
  1948 and is cited as "Gvati, narrative".
- **Village Statistics 1945** (Government of Palestine), the archive.org scan
  https://archive.org/details/palestine-village-statistics-1945 and its text layer
  (checked 2026-10-23), Table I "Population and total land
  areas" for the **Jerusalem sub-district**, which in 1945 also covered Bethlehem and Jericho. It is
  used only as a **presence check**: a Jewish locality named in a 1945 land unit existed by then. It
  is not a founding source, and it lists land units, so several localities are not named at all;
  "not listed by name" therefore proves nothing. The text layer drops blank columns, so a lone
  population figure cannot be assigned to the Arab or Jewish column; no figure is given here.
- **Census of Palestine 1931**: not used. #1429 found that the archive.org scan has no usable text
  layer. [GAP]
- **Hebrew Wikipedia** was read through the MediaWiki API as the wikitext of the current revision,
  cited by `oldid`. It is a lead and a cross-check, not an authority; a fact resting on it alone is
  marked **[S, Wikipedia]**. Where an article cites a newspaper item that was not opened here, the
  note says so. Quotes keep the source's wording; double quote marks inside a Hebrew quote are
  printed as single quotes; gershayim in Hebrew dates and abbreviations (for example
  תרפ"ט) are kept as printed.

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

## What counts as Jerusalem and its corridor here

There is no single boundary, so this note states its rule.

Included: every locality that the CBS file codes as Jewish (`דת יישוב` = 1) with `שנת ייסוד` before
1948 in the **Jerusalem sub-district** other than Jerusalem itself (6 rows: natural region 111 for
five of them, region 112 for Kefar Uriyya). **Our reading** of the two region numbers: the file has
no name column for natural regions, and no statistical-area row was read to name them. Jerusalem is
coded `ותיק` (long established) instead of a year; it gets one line as a pre-1882 city and no
founding date is sought. Places that stood in the same area before 15 May 1948 and were later
abandoned or merged into another locality are in part 3, together with the Gush Etzion kibbutzim
south of the city, which no other regional note takes.

Excluded:
- Hulda, Gezer and the rest of the Ramla and Rehovot sub-districts, left for the coastal plain and
  Sharon note (#1477).
- The Jewish neighbourhoods of Jerusalem outside the walls. Neither CBS nor Gvati lists them as
  localities, and they are not listed one by one here.
- Settlements founded after 15 May 1948 on the corridor (for example Shoresh, Tsova, Beit Nekofa
  and Mevo Betar, all dated by Gvati after the cut-off).

## 1. The CBS localities, one row each

Read every row as: CBS name (CBS transliteration); CBS locality code; CBS founding year; the
coordinate computed from the CBS string; Gvati's row (region, date as printed, affiliation); the
1945 land unit that names it, if any; the tag. All CBS cells come from bycode2024.xlsx, all Gvati
cells from https://benyehuda.org/read/14684, all 1945 cells from the archive.org text layer cited
above, each checked 2026-10-23. Rows tagged "part 2" have an earlier founding, a move or a
rename written out below.

| Locality | CBS code | CBS year | lat, lon (computed here) | Gvati: region, date, affiliation | Village Statistics 1945 | Tag |
|---|---|---|---|---|---|---|
| קריית ענבים (Qiryat Anavim) | 78 | 1920 | 31.81055, 35.11946 | הרי יהודה, `4.20`, האיחוד | Jerusalem: "Qiryat ‘Anavim and" (the name continues on the next line, which begins "Ma‘ale") | [V] |
| רמת רחל (Ramat Rahel) | 127 | 1926 | 31.73954, 35.21865 | הרי יהודה, `19.5.26`, האיחוד | Jerusalem: "Ramat Rahel" | [V]; left 1929, part 2 |
| מוצא עילית (Moza Illit) | 208 | 1933 | 31.79381, 35.15762 | הרי ירושלים, `1933`, מועצה מקומית | not listed by name (see Moza, part 3) | [V]; evacuated 1948, part 2 |
| מעלה החמישה (Ma'ale HaHamisha) | 286 | 1938 | 31.81773, 35.11054 | הרי יהודה, `19.7.38`, האיחוד | not listed by name (our reading: possibly the Qiryat Anavim unit above) | [V]; part 2 |
| כפר אוריה (Kefar Uriyya) | 364 | 1944 | 31.79261, 34.94732 | שפלת יהודה, `2.8.44`, תנועת המושבים | Ramle: "Kefar Uriya" (a Ramle sub-district table, not Jerusalem) | [V]; founded 1913, left 1929, part 2 |
| נווה אילן (Newe Ilan) | 405 | 1946 | 31.80828, 35.08093 | הרי יהודה, `27.10.46`, האיחוד | not applicable, founded after 1945 | [V]; kibbutz 1946, present village 1971-72, part 2 |
| ירושלים (Jerusalem) | 3000 | ותיק | 31.78381, 35.22047 | [GAP] no row | Jerusalem: "Jerusalem (Urban)" | pre-1882 city; no founding date sought |

All six CBS rows have a Gvati row (6 of 6), and CBS and Gvati give the same year in every one.

## 2. Earlier foundings, moves and renames for CBS localities

Hebrew Wikipedia pages below were read at the `oldid` given, checked 2026-10-23.

### Qiryat Anavim (CBS 78)
- CBS **1920**; Gvati `4.20`. [V]
- he.wiki `קריית ענבים`, oldid **44006051**: "הקיבוץ נוסד בשנת 1920 על ידי עולים מאוקראינה"; "על
  חלקה זו הוקם בשנת 1920 קיבוץ קריית ענבים". [S, Wikipedia] Gvati's origin of the name:
  "בעקבות הכפר הערבי קריית אל-ענב".
- The 1945 unit named "Qiryat ‘Anavim and" plus a next line beginning "Ma‘ale". **Our reading:** the second
  name is Ma'ale HaHamisha, whose land came from Qiryat Anavim (below); the text layer does not
  show the rest of the name.

### Ma'ale HaHamisha (CBS 286)
- CBS **1938**; Gvati `19.7.38`. [V]
- he.wiki `מעלה החמישה`, oldid **44069073**: "הקיבוץ נוסד רשמית ב-19 ביולי 1938"; of the name,
  "באספת ועדת השמות של קק"ל מ-27 באוגוסט 1938 נקבע השם הרשמי 'מעלה החמישה'", after a temporary
  name "גבעת החמישה". [S, Wikipedia]
- he.wiki `קריית ענבים`, oldid **44006051**: the land "דילב ב" "שבתחילה היו שייכות לקיבוץ קריית
  ענבים אך ב-1938 הוקם עליהן קיבוץ מעלה החמישה". [S, Wikipedia]

### Ramat Rahel (CBS 127)
- CBS **1926**; Gvati `19.5.26`. [V]
- he.wiki `רמת רחל`, oldid **43964481**: "קיבוץ רמת רחל הוקם בשנת 1926 על ידי חברי גדוד העבודה על
  שם טרומפלדור"; "בפרעות תרפ"ט (1929) ניטש הקיבוץ"; "בראשית שנות ה-30 של המאה ה-20 שבו החברים ובנו
  הכול מחדש". [S, Wikipedia]
- **Left in 1929** (no single day in the passages read), **returned in the early 1930s**. [S, Wikipedia]
  For May 1948, Gvati, narrative: "ב־21.5 הם פתחו באש תותחים על רמת־רחל וכבשוה. המגינים נסוגו, אולם
  באותו לילה, כאשר הגיעה תגבורת, שבו וכבשו את המקום"; the place then changed hands several times and
  was taken finally "על ידינו" (Gvati's wording). Gvati's list of the nine localities lost in the war does not
  include it (see part 3).

### Moza Illit (CBS 208)
- CBS **1933**; Gvati `1933`. [V]
- he.wiki `מוצא עילית`, oldid **43944602**: "הוקמה בשנת 1933 כמושב עובדים על אדמות האיכר שמואל
  ברוזה, על ידי בני המושבה הוותיקה מוצא (תחתית) וקבוצת פועלים מירושלים"; "בינואר 1948 פונה המושב
  מתושביו"; "תושבי המקום חזרו למוצא עילית עם תום מלחמת העצמאות". [S, Wikipedia]
- **Evacuated January 1948, residents returned after the war**; no day given. [S, Wikipedia]

### Kefar Uriyya (CBS 364)
- CBS **1944**; Gvati `2.8.44`. [V] for the year of the resettlement.
- Gvati, narrative: "ישוב זה, שניטש במאורעות 1929, יוּשַב מחדש בסוף 1944 על־ידי מתיישבים בני
  העדה הכורדית". So CBS and Gvati date the **second** settlement.
- he.wiki `כפר אוריה`, oldid **43728110**: the land was bought in December 1912; "בתחילת שנת 1913
  הזמין מאיר רוטברג את 'חבורת סג'רה' ... לבוא וליישב את המקום", and "באמצע 1913 נמכר המקום לשלושים
  משפחות של יהודי ביאליסטוק". [S, Wikipedia]
- **[C]** CBS and Gvati 1944 against he.wiki 1913 for the first settlement. **Our reading:** two
  settlements on one site; use 1944 as the founding of the locality CBS lists and record 1913 as the
  earlier one. No source read here gives a day for the first arrival.
- **Left in 1929:** the same article has the residents leaving at the time of the 1929 riots, and the
  houses burned afterwards, with no single day in the passages read. **Rename:** "המקום נקרא במקור
  'קריית משה' ... אך שונה ל'כפר אוריה'"; the year of the rename is not given. [GAP]
  **Resettled 1944:** "המושב הוקם מחדש על חורבות המושב הישן ב־1944 על ידי יהודים דתיים יוצאי
  כורדיסטן". [S, Wikipedia]
- **Village Statistics 1945** names "Kefar Uriya" in the Ramle sub-district table, not the Jerusalem
  one, although CBS codes it today in the Jerusalem sub-district. For May 1948 Gvati, narrative,
  has Har Tuv's people moved to Kefar Uriyya (part 3).

### Newe Ilan (CBS 405)
- CBS **1946**; Gvati `27.10.46`. [V]
- he.wiki `נווה אילן`, oldid **43173897**: "נווה אילן הוקם בתחילת אוקטובר 1946"; "ב-27 באוקטובר
  1946 התקיים טקס חנוכה רשמי של הקיבוץ"; of the first site, "קיבוץ שהתפרק ב-1956"; "אבן פינה למושב
  הונחה ב-27 בספטמבר 1971". [S, Wikipedia]
- **Our reading:** CBS's 1946 and Gvati's day are the kibbutz that was dissolved in 1956; the locality
  on CBS's coordinate is the cooperative village of the 1970s on a neighbouring hill ("על גבעה בכניסה
  לנווה אילן" is how the article places the historic site). No coordinate for the 1946 site here.
  [GAP] For the later settlement the article gives the first members arriving in Israel in 1970, six families living on the site from 1972, and the cornerstone on 27 September 1971.

## 3. Places that stood before 15 May 1948 and are not CBS localities today

These are the rows the issue calls renames and abandonment. Each is recorded with its source; nothing
is proposed about how it appears in the game. None is a CBS row for its pre-1948 site.
[checked 2026-10-23: CBS has no row for `נווה יעקב`, `הר טוב`, `מוצא תחתית` or `מגדל עדר`. It has
rows with later years for `בית הערבה` (code 3645, 1980, Jordan-Jericho sub-district), `כפר עציון`
(3488, 1967, Bethlehem sub-district), `משואות יצחק` (620, 1949), `עין צורים` (622, 1949) and `רבדים`
(564, 1948), the last three in the Ashkelon sub-district; `בני עטרות` (448, 1948) and `אזור תעשייה
הר-טוב` (1701, no year) are other localities.]

### Moza (Moza Tahtit, annexed to Jerusalem)
- he.wiki `מוצא`, oldid **43885172**: infobox `תאריך ייסוד = 1894`; "המושבה נוסדה בשנת 1894 על ידי
  הארגון היהודי 'בני ברית' כיישוב חקלאי יהודי מבודד על הדרך מירושלים ליפו"; "החל משנת 1933, עם
  הקמתו של מושב העובדים מוצא עילית, שונה שם המקום מ'מוצא' ל'מוצא תחתית', ובשנת 1993 צורפה מוצא
  תחתית לירושלים". [S, Wikipedia]
- Gvati has a row `מוצא (תחתית)`, הרי ירושלים, `1984`, מועצה מקומית. **Our reading:** 1984 is a
  misprint of 1894, because he.wiki gives 1894 and the colony's own article says it was annexed to Jerusalem in
  1993; no source read here says it is a misprint. Use 1894 and label it "our reading".
- Village Statistics 1945: "Qaliniya and Motsa" in the Jerusalem sub-district table; the unit also
  includes the neighbouring Arab village (our reading). **Renamed 1933, annexed 1993.** [S, Wikipedia]

### Atarot
- he.wiki `עטרות`, oldid **43943172**: "היישוב הוקם ב-1912, ופונה והוחרב במלחמת העצמאות"; "התיישבה
  במקום בשנת 1914 קבוצה מבני העלייה השנייה שנקראה 'קבוצת קלנדיה'"; "בחודש דצמבר 1919 ... הגיע
  למקום גרעין של קבוצת עבודה שנייה"; "בשנת 1923 רכשה הקרן הקיימת לישראל 374 דונם ... והקבוצה
  החליטה להתארגן במסגרת מושב עובדים בשם 'עטרות'". [S, Wikipedia]
- he.wiki `נווה יעקב (מושבה)`, oldid **44067999**: "בשנת 1919 הוקמה עטרות". [S, Wikipedia]
- **[C]** inside Wikipedia: 1912 (land bought), 1914 (first group), 1919 (second group, said to have
  founded it) and 1923 (the moshav under the name Atarot). **Our reading:** no one date is the
  founding; the moshav of that name dates from 1923. Not in Gvati's appendix. [checked 2026-10-23:
  no row] Village Statistics 1945 names "‘Atarot" in the Jerusalem sub-district table.
- **1948:** he.wiki: "באפריל 1948 פונו מהיישוב הילדים והנשים"; Gvati, narrative: "ב־14.5 הותקפה
  עטרות. בלילה פונו תושביה לנוה־יעקב (שכונה חצי־חקלאית בצפון ירושלים)". **Evacuated the night of
  14 May 1948.** This rests on Gvati; he.wiki gives April for the women and children and puts the order to leave on the day of the High Commissioner's departure, 14 May, which is consistent with Gvati (our reading). Gvati has a row for Bnei Atarot, `7.8.48`,
  "ע"ש הישוב עטרות שננטש במלחמת השחרור ועבר לכאן", which is after the cut-off and in another
  region. Coordinate: [GAP].

### Neve Ya'akov (the colony, not the 1970 neighbourhood)
- he.wiki `נווה יעקב (מושבה)`, oldid **44067999**: infobox `תאריך ייסוד = 1925`, `תאריך נטישה =
  1948`; "בהתחלה 'כפר עברי' או 'הכפר העברי'"; "היישוב העברי השני שהוקם מצפון לירושלים". [S,
  Wikipedia] The neighbourhood of the same name was founded in 1970 slightly north-east of the
  colony (same article); it is not this row.
- Gvati, narrative: "ב־16.5 הותקפה נוה־יעקב על־ידי כוחות גדולים. ההתקפה נהדפה, אולם אחרי האבדות
  שנגרמו למגינים הוחלט לסגת בלילה מהמקום". **Left the night of 16 May 1948.** Gvati has no appendix row. [checked 2026-10-23: no row]
- Village Statistics 1945 names "Neve Ya‘aqov" in the Jerusalem sub-district table. **First name**
  "Kefar Ivri" (the Hebrew village); the year of the rename is not given. [GAP] Coordinate: [GAP].

### Har Tuv
- he.wiki `הר-טוב`, oldid **43696711**: infobox `תאריך ייסוד = 1883`, `תאריך נטישה = 16 במאי 1948`;
  "ב-1883 רכש המיסיון האנגלי בירושלים ... 5,950 דונמים"; "ולקראת 1886 התרוקן המקום מיושביו"; "בדצמבר
  1895 ... עלו למקום ראשוני המתיישבים והתיישבו בבית המיסיון"; "פונתה במלחמת העצמאות בליל 16 במאי
  1948". [S, Wikipedia]
- **Our reading:** 1883 is the purchase by a mission and 1886 the emptying of that farm; the Jewish
  colony dates from December 1895 (Bulgarian Jews, same article). Not in Gvati's appendix.
  [checked 2026-10-23: no row] Village Statistics 1945 names "Har Tuv" in the Jerusalem sub-district
  table.
- **[C]** on the evacuation: Gvati, narrative: "עוד ישוב יהודי אחד פונה ב־15 במאי: הרטוב" and "אנשיו הועברו
  לכפר־אוריה", against he.wiki "בליל 16 במאי 1948". **Our reading:** the two can describe one night
  that spans the date; keep both. Gvati lists it among seven places "פונו או נכבשו זמנית וחזרו
  לשליטתנו", and he.wiki says "מספר מתיישבים שבו, אך היישוב לא הוקם מחדש". Coordinate: [GAP].

### Beit HaArava
- he.wiki `בית הערבה`, oldid **43568559**: "בית הערבה הוקם לראשונה כקיבוץ בכ"ה בתשרי ה'ת"ש (8
  באוקטובר 1939)"; "ב-17 במאי 1948, החליט אמנון זעיר, ה'קונסול' של האזור המנותק, שאין סיכוי להגן
  על צפון ים המלח ויש לפנות את בית הערבה"; "עד ל-20 במאי ... פונו מצפון ים המלח בדרך הים".
  [S, Wikipedia]
- Gvati, narrative, names it among the nine places "שפונו מיושביהם ונכבשו על־ידי האויב". Not in the
  appendix. [checked 2026-10-23: no row] **Our reading:** the Jerusalem sub-district table also
  lists Jericho, but no source read here puts Beit HaArava's land unit in it, and the table does not
  name it. Sub-district [GAP]. **Non-combatants ordered out 15 May 1948** ("ב-15 במאי 1948 ניתנת הוראה להוציא את האוכלוסייה הבלתי-לוחמת מבית הערבה"); **evacuation decided 17 May and complete by 20 May 1948.** [S, Wikipedia] Coordinate: [GAP].

### Gush Etzion: Kefar Etzion, Masuot Yitzhak, Ein Tzurim and Revadim
- Four kibbutzim (Gvati, narrative: "ארבעה קיבוצים היו בגוש: כפר־עציון, משׂוּאוֹת, עין־צורים
  ורבדים ואוכלוסייתם" and then "260 נפש"). he.wiki `גוש עציון`, oldid **43981750**, dates three of them:
  Masuot Yitzhak "כ"ח בחשוון תש"ו (4 בנובמבר 1945)", Ein Tzurim "כ"ז בתשרי תש"ז (22 באוקטובר
  1946)", Revadim "כ"א בשבט תש"ז (11 בפברואר 1947)". [S, Wikipedia]
- **Kefar Etzion**: he.wiki `כפר עציון`, oldid **43846185**: "בשנת 1943 (תש"ג) נוסד קיבוץ כפר עציון,
  הקיבוץ הראשון בגוש עציון"; the earlier attempt, "ננטש בשנת 1929 עקב מאורעות תרפ"ט. בשנת 1935
  יושב האזור על ידי חברת 'אל ההר' ואז ניתן למקום שמו 'כפר עציון'". [S, Wikipedia] Gvati's row for
  Kefar Etzion is the 1967 refounding: `25.9.67`, הר חברון, הפועל המזרחי.
- **Masuot Yitzhak**: he.wiki `משואות יצחק`, oldid **44069163**: "הוקם באוגוסט 1945". **[C]** inside
  Wikipedia: August 1945 here, 4 November 1945 in the Gush article. **Our reading:** keep both; the
  month is not settled. Gvati's row `6.7.49` (region דרום) is, our reading, the refounding in the south.
- **Ein Tzurim**: he.wiki `עין צורים`, oldid **43279921**: "הוקם כנקודת היאחזות בגוש עציון" on
  "23 באוקטובר 1946", against 22 October in the Gush article. [C] by one day; keep both. Gvati's row
  `6.5.49` (region שפלת יהודה; name origin "כשם הקיבוץ בגוש עציון שנחרב במלחמת השחרור") is, our reading,
  the refounding.
- **Revadim**: he.wiki `רבדים`, oldid **44069086**: "הוקם ב-11 בפברואר 1947 בגוש עציון". [S,
  Wikipedia] Gvati's row `17.11.48` is the refounding, and the narrative says it went up "אחרי שחבריו
  שוחררו מהשבי".
- **Migdal Eder (predecessor)**: he.wiki `מגדל עדר`, oldid **42481770**: "היישוב נוסד בחורף שנת
  תרפ"ז, 1927"; "חרב בימי מאורעות תרפ"ט". [S, Wikipedia]
- **12 to 14 May 1948**, Gvati, narrative: "ב־12.5.1948 הסתער הלגיון הערבי ... על הישובים בגוש
  עציון", "ב־13 במאי פרצו הערבים לכפר־עציון" (the sentence goes on to the deaths of those found
  there; not repeated here), the other three surrendered the next day, and "גוש עציון נכנע ביום
  הכרזת המדינה: 14 במאי 1948". **Kefar Etzion fell 13 May; the other three on 14 May 1948.**
- None of the four is a Gvati founding row for the pre-1948 site, and none is a CBS row for its pre-1948 site (CBS has rows for the refounded Kefar Etzion, Masuot
  Yitzhak, Ein Tzurim and Revadim, with the later years above). Village
  Statistics 1945: [GAP], see part 4. Coordinates: [GAP].

## 4. Gaps and what was not done

- [GAP] **Gush Etzion in Village Statistics 1945.** The 1945 sub-district of the four kibbutzim is
  not established. The Jerusalem sub-district table has no unit with their names, and a search of the
  Hebron sub-district table's text layer for Etzion spellings found no unit with their names. They may be
  unnamed land units; "not listed by name" proves nothing.
- [GAP] **Kalya.** Gvati has a row `קליה`, בקעת הירדן, `1.2.68`, האיחוד (a 1968 Nahal outpost, "קם
  לתחיה ים המוות"), and a narrative line that places it next to "מלון 'קליה' הזכור לרבים מתקופת
  המאנדט". Whether a Jewish settlement stood there before 15 May 1948 is not established by anything
  read here; he.wiki `קליה`, oldid **38683480**, is a short disambiguation page. No row.
- [GAP] **Completeness of part 3.** Part 3 lists the abandoned and merged places named in Gvati's
  list of nine, in the 1945 table and in the articles read. It is not shown to be complete: Gvati's
  other places lost in the war (Kefar Darom, Mishmar HaYarden) are outside this region.
- [GAP] **Census of Palestine 1931**: not used (no usable text layer, see #1429).
- [GAP] **Coordinates of abandoned, moved and merged places**: none has one here.
- [GAP] **Rename years** for Kefar Uriyya (from Qiryat Moshe) and Neve Ya'akov (from the Hebrew
  Village) are not in the passages read.
- **Gvati day versus CBS year.** Where they agree on the year the row is [V]; the day rests on Gvati
  alone, and as `settlements-1949-51.md` notes, it can be a day or more off other sources. All six
  CBS rows have a Gvati row; none of the places in part 3 has one for its pre-1948 site.
- **Not done:** no hex, no game date, no rule for which of two dates the game should use. Those
  belong to #61 and to the owner's sensitivity review.
