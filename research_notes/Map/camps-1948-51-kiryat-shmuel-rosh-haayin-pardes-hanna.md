# Camps left out of #2169: Kiryat Shmuel (Haifa), Rosh HaAyin, Pardes Hanna (#1719, research only)

Status: **draft, needs a person to check**. An AI agent opened every source below on 1 November 2026 and wrote this
note. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check each claim against its source before
use. This note changes nothing in `content/`, `sim/` or `unity/`, and proposes no record. It adds to
`research_notes/Map/camps-1948-51.md` (Beri, #1766, #1797) for the three places that PR #2169 left out; it does not
restate that note's rows, and it uses that note's tags: **[V]** the agent read the record itself; **[S]** secondary, or a
page that is not the record; **[C]** two sources disagree, both kept; **[GAP]** still open, with what was tried.

## Result in one table

| Place | Location point | Camp in 1948-51 | Operating dates | Still open |
|---|---|---|---|---|
| Kiryat Shmuel, Haifa | **Found** for the neighbourhood: Wikidata Q185687 P625, 32.83401944 N, 35.06784167 E, matches the 1:20,000 Survey of Palestine label "Qiryat Shemuel" to about 0.2 km. Hebrew name קריית שמואל. | **Immigrant camp** (מחנה העולים בקריית שמואל), in the south part of the neighbourhood (Hebrew Wikipedia only). **No military or training camp found** for 1948-51. | 1947 to January 1948 (Hebrew Wikipedia, citing Davar and a Jewish Agency document); "Kiryat Shmuel Immigrant Camp" named in *The Jerusalem Post*, 3 July 1950 (OCR); Sept and Dec 1949 counts and "still in 1959" (Hebrew Wikipedia citing Davar). | The camp's own point; any day in May 1948; any 1948-51 military use; every Davar primary. |
| Rosh HaAyin | Town point only (as in the earlier note). | Immigrant camp in a former British army camp: **found**, primary (GPO photo caption) and reliable secondary (Yad Ben-Zvi, Shvarts, Encyclopaedia Judaica). | Seen operating **2 February 1950** (GPO caption, new; earlier than the earlier note's 17 May 1950); ma'abara in 1950 (EJ, Shvarts p. 208). | Opening day; the camp's own point. |
| Pardes Hanna | Town point only (as in the earlier note). | Immigrant camp: **found**, reliable secondary (Yad Ben-Zvi, Shvarts) and a dated GPO photo of December 1950. | IMS children's hospital there in March 1949 (Shvarts p. 168, new); camp doctor March 1950 (Shvarts p. 204); ma'abara photographed 1 December 1950 (GPO). | Opening day; the camp's own point. |

## Sources

| Id | Source | Type | How read | Accessed |
|---|---|---|---|---|
| WD1 | Wikidata, item Q185687 "Kiryat Shmuel" / "קריית שמואל", P625, https://www.wikidata.org/wiki/Q185687 (API `wbgetentities`) | tertiary (gazetteer data) | API JSON | 2026-11-01 |
| HW1 | Hebrew Wikipedia, "קריית שמואל (חיפה)", revision 43128203 (22 Apr 2026), `prop=coordinates` and wikitext | tertiary | API | 2026-11-01 |
| EW1 | English Wikipedia, "Kiryat Shmuel, Haifa", revision 1378284954 (3 Oct 2026), `{{coord}}` and `prop=coordinates` | tertiary | API | 2026-11-01 |
| HW2 | Hebrew Wikipedia, "מחנה העולים בקריית שמואל", revision 39577890 (7 Sep 2024), wikitext (Wikidata Q6953874, which has no P625) | tertiary (cites Davar and *Political Documents of the Jewish Agency 1947*) | API | 2026-11-01 |
| HW3 | Hebrew Wikipedia, "מחנה עולים", revision 42053537 (23 Oct 2025), wikitext | tertiary (cites Kachinsky, KC1) | API | 2026-11-01 |
| HW4 | Hebrew Wikipedia, "מחנה העולים בפרדס חנה", revision 39602644 (11 Sep 2024), the same revision as the earlier note's W9 (Wikidata Q6979962, no P625) | tertiary | API | 2026-11-01 |
| KC1 | Miriam Kachinsky (מרים קצ'נסקי), "המעברות", in Mordechai Naor (ed.), *עולים ומעברות, 1948-1952: מקורות, סיכומים, פרשיות נבחרות וחומר עזר*, Yad Izhak Ben-Zvi, 1986 (date as printed on the page), online text at Mateh/CET: http://www.lib.cet.ac.il/pages/item.asp?item=12939. No page numbers on the web text. | reliable secondary (Yad Ben-Zvi) | page text | 2026-11-01 |
| S1 | Shifra Shvarts, *Health and Zionism: The Israeli Health Care System, 1948-1960* (University of Rochester Press, 2008), open-access copy https://archive.org/details/oapen-20.500.12657-85979, text file `9781580467414_djvu.txt`; page numbers from the printed running heads in the OCR | reliable secondary (academic press; cites Lavon Institute archive files) | OCR text | 2026-11-01 |
| EJ1 | *Encyclopaedia Judaica*, 2nd ed. (2008, Gale), "Rosh ha-Ayin" (Shlomo Hasson / Shaked Gilboa), as reproduced at https://www.jewishvirtuallibrary.org/jsource/judaica/ejud_0002_0017_0_17029.html | reliable tertiary (edited encyclopedia) | page text | 2026-11-01 |
| GPO1 | Government Press Office, National Photo Collection, D826-082, Zoltan Kluger, dated 2 February 1950, caption as transcribed on Wikimedia Commons: https://commons.wikimedia.org/wiki/File:Zoltan_Kluger._Rosh_Ha-Ayin_1950.jpg | primary (dated government photo record), read through a tertiary transcription | Commons `extmetadata` | 2026-11-01 |
| GPO2 | Government Press Office, National Photo Collection, D199-084, David Eldan, dated 5 February 1950: https://commons.wikimedia.org/wiki/File:ROSH_HA%27AYIN,_YEMENITE_IMMIGRANTS.jpg | as GPO1 | Commons `extmetadata` | 2026-11-01 |
| GPO3 | Government Press Office, National Photo Collection, D200-045 and D200-046, Teddy Brauner, dated 1 December 1950: https://commons.wikimedia.org/wiki/File:Pardes_Hanna_Camp_1.jpg and https://commons.wikimedia.org/wiki/File:Pardes_Hanna_Camp_2.jpg | as GPO1 | Commons `extmetadata` | 2026-11-01 |
| JNF1 | JNF photo archive, Lazar Dinar, dated 1 February 1947, caption as transcribed on Commons: https://commons.wikimedia.org/wiki/File:פליטים_שהגיעו_מקפריסין_ארצה_באונית_סרינה_מועברים_למחנה_הפליטים_בקרית_שמואל-JNF025342.jpeg | primary (photo record) via Commons | Commons `extmetadata` | 2026-11-01 |
| JP6 | *The Jerusalem Post*, issue dated 3 July 1950, archive.org `per_jerusalem-post_the-jerusalem-post_1950-07-03_26_7296`, OCR file `_djvu.txt`; page image not opened | primary (newspaper), OCR only | OCR text | 2026-11-01 |
| JP7 | *The Jerusalem Post*, issue dated 21 May 1950, archive.org `per_jerusalem-post_the-jerusalem-post_1950-05-21_26_7260`, OCR; page image not opened | primary, OCR only | OCR text | 2026-11-01 |
| JTA1 | JTA archive page text, 14 September 1948: https://www.jta.org/archive/arabs-continue-attacks-on-jewish-positions-capture-hills-fire-upon-u-n-observers | primary (news agency), page text | page text | 2026-11-01 |
| P1, P2, P5 | Palestine Open Maps tile layers, as in the earlier note: `pal20k-1940s` (sheets 14-24 / 15-24 "Haifa" cover the point, per P3 `sheets.csv`), `pal100k-1950s` (sheet 02 "Haifa", catalogued 1958), `isr250k-1951`; tiles 15/19574-19576/13215-13217 and 13/4892-4894/3303-3305 | primary (maps), georeferenced by the site | tiles stitched, read by eye | 2026-11-01 |

All requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

## 1. Kiryat Shmuel, Haifa (the owner's starting training-centre host)

The owner's ruling (`docs/GDD.md` O8, 27M-25) is a design value: "the training centre is an ordinary facility in Kiryat
Shmuel, Haifa". Nothing below establishes a historical training centre there; the location record needs only the place.

| Claim | Exact quote | Source | Type | Tag | Confidence |
|---|---|---|---|---|---|
| Coordinates of the neighbourhood | P625 = latitude 32.83401944, longitude 35.06784167 (two statements, same value; one is referenced only "imported from Wikimedia project" English Wikipedia, the other has no reference) | WD1 | tertiary | [S] | high that this is the point Wikidata holds; it is a present-day centre point, not a 1948 site |
| Same point in both Wikipedias | he: `coordinates` lat 32.83401944, lon 35.06784167 (primary); en: `{{coord\|32\|50\|2.47\|N\|35\|4\|4.23\|E}}` = 32.834019, 35.067842 (computed) | HW1, EW1 | tertiary | [S] | high (same value; the three are not independent) |
| The point sits on the 1940s Survey of Palestine label | The 1:20,000 tiles print "Qiryat Shemuel"; the label's centre reads at about 32.8325 N, 35.0663 E, about 0.17 km south and 0.14 km west of the Wikidata point (eye reading, about 50 m). The 1958 1:100,000 sheet prints "Qiryat Shemuel" next to the point; the 1951 1:250,000 layer prints "Qir. Shemuel" beside a circle about 0.37 km west of it. None of the three prints a camp label in the windows read. | P1, P2, P5 | primary (maps) | [V, agent read 2026-11-01; a person's check pending] | medium (the site's georeferencing was not checked; edition of the 1:20,000 mosaic not shown) |
| Hex on the current grid | `tools/hexgrid.py settlement_hex(35.06784167, 32.83401944)` with main's `content/map/hexes.json` returns (6, 8); Haifa is (5, 8) in `content/map/settlement_hexes.yaml`. A rebake (#1796) would move it. | repo | computed | n/a | high for main at 32e7d9cf8 |
| Hebrew name | Wikidata he label "קריית שמואל"; the article title "קריית שמואל (חיפה)"; the period maps spell it "Qiryat Shemuel" (English) | WD1, HW1, P1 | tertiary, primary (map) | [S] | high |
| Inside Haifa's area in 1945 | already in `research_notes/Map/regions-1948-opening-control-p1.md` (gdd03-0730): the 1945 village statistics count "Qiryat Shmuel—160" inside Haifa (Urban) | repo | primary via repo | (as there) | (as there) |
| Founded 1935-38, joined Haifa 1952 | "בניית התשתית לשכונה החלה בט"ו בתמוז תרצ"ה (16 ביולי 1935)"; "בשנת 1952 צורפה קריית שמואל כשכונה לעיר חיפה" (the Kiryat Haim article says 1950: [C], already recorded in `research_notes/Era 1882-1948/settlements-pre-1948-haifa-carmel.md`) | HW1 | tertiary | [S] / [C] | medium |
| An immigrant camp stood in its south part | "בתקופת העלייה ההמונית שכנה בחלקה הדרומי של השכונה מעברה, בשם מחנה העולים בקריית שמואל"; the present quarter "המעברה" is "על שטח מעברה לשעבר, מדרום לקריית שמואל א'" | HW1 | tertiary, uncited there | [S] | medium; **rests on Wikipedia alone** for the position |
| Camp operating in 1947 under the Mandate | "המחנה עצמו הוקם לקראת ראשית 1947 ופעל עד הקמת מדינת ישראל"; capacity "כ-700 נפש" (cites Davar, 7 Feb 1947, and the Yassky memorandum of 18 July 1947, *Political Documents of the Jewish Agency 1947*, pp. 468-469) | HW2, HW3 | tertiary citing primary | [S] | medium; primaries not opened (NLI 403) |
| Camp in use, February 1947 | JNF photo caption: "פליטים שהגיעו מקפריסין ארצה באונית סרינה מועברים למחנה הפליטים בקרית שמואל", dated 1 February 1947 | JNF1 | primary (photo record) via Commons | [S] | medium (caption and date as transcribed on Commons; the JNF record itself not opened) |
| Camp in use, January 1948 | "בינואר 1948 הובלו למחנה העולים בקריית שמואל עולים בלתי לגליים" (cites Davar, 4 Jan 1948) | HW2 | tertiary | [S] | medium |
| Camp in use, 1949 | "בספטמבר 1949 היו במחנה 268 משפחות" (Davar, 9 Sep 1949) and "בדצמבר אותה שנה היו בה 950 נפש" (Davar, 6 Dec 1949); by late 1949 one of the first converted to a work camp | HW2 | tertiary | [S] | medium |
| Camp in use, July 1950 | "A newcomer from Germany, who still lives in the Kiryat Shmuel Immigrant Camp, is carving character heads for them" (OCR prints "Shmuel!" and "Lm-migrant"). The item does not name the city; the camp of that name in the sources read is Haifa's. | JP6 | primary, OCR | [S] | medium (page image not opened; city inferred) |
| Still a ma'abara in 1959 | "המחנה המשיך להתקיים כמעברה ... ועדיין התקיים בשנת 1959" | HW2 | tertiary | [S] | medium |
| Military or training camp, 1948-51 | **Not found.** HW1 says the neighbourhood was built "סמוך למחנות צבאיים בריטים גדולים" and that a later quarter "נבנתה בשטח מחנה צבאי לשעבר מזרחית לקריית שמואל ב'", with no dates and no IDF use. | HW1 | tertiary | [GAP] | n/a |
| Not in the Yad Ben-Zvi list of camps | KC1 lists the first immigrant camps as "בעתלית, פרדס-חנה, רעננה, בית-ליד, בנימינה, חדרה וראש-העין" and the second wave as "בבאר-יעקב, קרית-אליהו (חיפה), קרית-מוצקין, רחובות וירושלים"; Kiryat Shmuel is in neither. Not a contradiction (HW2 dates its camp from 1947, before this list), but the one reliable secondary read here does not name it. | KC1 | reliable secondary | [S] | high that the list omits it |

**Distinct from (names that collide):** Kiryat Shmuel, **Jerusalem** (the JTA bulletin of 14 September 1948 reports
Egyptian shelling of "Katamon, Kiryath Shmuel, ... Rehavia", JTA1: every JTA hit for the name in 1948-51 is this one);
Kiryat Shmuel, **Tiberias** (*Jerusalem Post* 21 May 1950, OCR: "Tiberias Recruiting Station, Kiryat Shmuel", JP7, a
Ministry of Defence recruiting notice, so not Haifa); Kiryat Shmona; the neighbouring Kiryat Motzkin immigrant camp (KC1's
second wave, "מחנה צבאי מול תחנת הרכבת" in HW3) and Kiryat Haim. A record's sources must not mix these.

**Searched, not found (Kiryat Shmuel):**
- JTA archive API (`wp-json/wp/v2/archive`, 1 Jan 1948 to 31 Dec 1951): "Kiryat Shmuel" 1 item, "Kiryat Samuel" 1,
  "Kiriat Shmuel" 2, "Kiryath Shmuel" 1, "Kiryat Shemuel" 1, "Kfar Shmuel" 22, "Haifa immigrant camp" 56. A script scan of
  every item's text for the name found only the Jerusalem item (JTA1) and a 1950 item on Kfar Shmuel, a new settlement in
  the Jerusalem corridor. No Haifa camp item.
- *The Jerusalem Post* OCR: only **61 of the 512** archive.org issues were downloaded (24 April to 13 July 1950) before
  the download was stopped for time; the scan found the two items above (JP6, JP7). The other 451 issues were **not scanned**
  for Kiryat Shmuel (the earlier note scanned all 501 for the other camps' names only).
- *Jewish Frontier* (archive.org `sim_jewish-frontier_*`, 47 issues listed for 1948-51; see "Jewish Frontier" below).
- S1 (Shvarts): no "Kiryat Shmuel" in the text (only Kiryat Shmona, Kiryat Ono, Kiryat Malachi).
- Hebrew Wikipedia search: "מחנה קריית שמואל" 0, "מחנה קרית שמואל" 0, "קרית שמואל" טירונים 0; "קריית שמואל" בסיס צה"ל and
  "בקריית שמואל" מחנה צבאי returned no article about a 1948-51 military camp in the Haifa neighbourhood.
- Wikimedia Commons file search: "Kiryat Shmuel" camp 3 files (JNF1 and the two 1947 HW2 photos); "Kiryat Shmuel" Haifa
  immigrants 0; "Kiryat Shmuel" ma'abara 0.
- A web search (Hebrew, camp + 1948-49 + ma'abara) returned only encyclopedia pages, nothing primary.

## 2. Rosh HaAyin camp

What the earlier note already has (not repeated): JTA 17-19 May 1950 [V], *Jerusalem Post* 27 July and 11 September 1950
[V], S1 p. 210 (transfer of medical equipment "Rosh Haayin (transferred on August 6, 1950)"), and the town point only.

| Claim | Exact quote | Source | Type | Tag | Confidence |
|---|---|---|---|---|---|
| An immigrant camp existed at Rosh HaAyin, among the first wave of camps | "מחנות עולים הוקמו תחילה בעתלית, פרדס-חנה, רעננה, בית-ליד, בנימינה, חדרה וראש-העין" | KC1 | reliable secondary | [S] | high that the camp existed; no date in the sentence |
| It was a former British army camp | "A large British army camp erected during World War II was converted in 1950 into a ma'barah that provided shelter for immigrants from Yemen." | EJ1 | reliable tertiary | [S] | medium-high |
| **Operating on 2 February 1950 (new, earlier than 17 May 1950)** | "Immigrants from Yemen celebrating their first "Tu Bishvat" in Israel, at the "Rosh Ha'Ayin" immigrants camp." dated 1950-02-02 (the agent did not check the Hebrew-calendar date of Tu BiShvat 1950 against it) | GPO1 | primary via Commons | [S] (record not opened at the GPO; see blocked list) | medium-high |
| Yemenite immigrants there, 5 February 1950 | "Gathering wood for fires to dispel the bitter cold, Rosh Ha'Ayin, Yemenite immigrants." dated 1950-02-05 | GPO2 | primary via Commons | [S] | medium-high |
| Became a ma'abara in 1950 | "(Rosh Haain camp became Rosh Haain Maabara)", in a passage that begins "In mid-1950, the government started to transfer immigrants from the camps to newly-constructed maabarot or to change the status of some of the camps" (p. 208) | S1 | reliable secondary | [S] | high; agrees with the earlier note's JP1 (about 11 August 1950, derived) and EJ1 |
| Archive file on the Rosh HaAyin ma'abarot | S1's note 41 cites "Ishpuz Holim me-Maabarot be-Rosh ha-Ayin ... letter from Dr. Steinberg to the Kupat Holim directorate, October 15, 1950, LMA, IV 243-3-138" | S1 | pointer to a primary (Lavon archive), not opened | [S] | pointer only |
| Permanent town 1951 | "In 1951, Rosh ha-Ayin was transformed into a permanent settlement" | EJ1 | reliable tertiary | [S] | medium |
| Camp's own position | **[GAP].** No source read gives a point for the camp; EJ1 places the town "3 mi. (5 km.) east of Petaḥ Tikvah, near the Yarkon". | EJ1 | | [GAP] | n/a |
| Opening day | **[GAP].** KC1 gives the order, not a date; GPO1 is a sighting, not an opening. A PikiWiki photo on Commons ("126874 mabara in rosh haayin", Teddy Brauner) carries "דצמבר 1949" in its metadata, but that is a user-contributed collection and is not used. | | | [GAP] | n/a |

## 3. Pardes Hanna camp

What the earlier note already has (not repeated): HW4/W9 (British camps turned into four immigrant camps when the British
withdrew; about 4,500 in October 1948), S1 p. 204 (a doctor "in the Pardes Hanna Immigrant Camp", March 1950), *Jerusalem
Post* 4 December 1950 [V], and the town point only.

| Claim | Exact quote | Source | Type | Tag | Confidence |
|---|---|---|---|---|---|
| An immigrant camp existed at Pardes Hanna, among the first wave | "מחנות עולים הוקמו תחילה בעתלית, פרדס-חנה, ..." (same sentence as above) | KC1 | reliable secondary | [S] | high that it existed; no date |
| Set up in vacated British camps after the partition resolution | "התגבר זרם העולים ... והם שוכנו ... במחנות עולים נוספים שהוקמו במחנות צבא שפונו על ידי הצבא הבריטי ... בשלב זה הוקם מחנה העולים בפרדס חנה" (cites KC1) | HW3 | tertiary citing KC1 | [S] | medium (KC1's own text, read above, gives the list but not the "vacated British camps" wording) |
| **Immigrant Medical Service children's hospital at Pardes Hanna, early 1949 (new)** | "At the same time, the IMS operated four hospitals on an independent footing that provided 128 beds: A children's hospital in Pardes Hana in the Sharon (forty beds)", in a paragraph that opens "At the outset of 1949" and dates the figures before it "In March 1949" (p. 168) | S1 | reliable secondary | [S] | medium: the passage ties the hospital to Pardes Hanna but does not say it stood inside the camp |
| Ma'abara photographed, 1 December 1950 | "TENTS AT PARDES HANNA MA'ABARA (Jewish refugees camp in Israel)." and "PARDES HANNA MA'ABARA." dated 1950-12-01 | GPO3 | primary via Commons | [S] | medium-high |
| Camp's own position | **[GAP].** HW4 places the British camps "בין פרדס חנה לבנימינה" (cites *Jewish Frontier*, 1949, p. 40, not found, see below); no point. | HW4 | tertiary | [GAP] | n/a |
| Opening day | **[GAP].** | | | [GAP] | n/a |

## Jewish Frontier (the source HW4 cites)

archive.org lists 47 issues of *Jewish Frontier* for 1948 to 1951 (`sim_jewish-frontier_1948-01_15_1` to
`..._1951-12_18_12`), with open OCR files. 42 were downloaded and scanned for Pardes(s) Hanna, Rosh Ha'ayin (and
spellings), Kiryat/Qiryat Shmuel/Shemuel and "20,000 soldiers/British camps". Not scanned: 1949-12, 1950-10 and 1951-09
(HTTP 500 from archive.org) and 1948-12 and 1949-05 (no text file saved). The only camp hit: October 1951, a writer quotes
a friend on "the Jews from Yemen whom I saw at Rosh Ha'ayin" (no date for the visit; not used). No issue scanned has HW4's
cited sentence about the British camps between Pardes Hanna and Binyamina, so its page "40" of 1949 is probably in
one of the two 1949 issues not scanned. **Searched, not found.**

## Blocked or not retrieved

- **National Library of Israel** (`nli.org.il`): HTTP 403 to one plain request each for a Davar article page
  (`/he/newspapers/dvr/1947/02/07/01/article/124.1`) and an image record (`/en/images/NNL_ALEPH997008137290805171/NLI`).
  Not retried. So every Davar citation in HW2 and HW4 stays unread.
- **GPO photo site**: `https://gpophoto.gov.il/haetonot/Pages/default.aspx` returned HTTP 200 with a "page not found" page;
  the GPO records D826-082, D199-084 and D200-045/046 were read only as transcribed on Wikimedia Commons.
- Not tried in this pass: the Israel State Archives, IDF archive and Knesset sites (all blocked to the earlier passes, see
  `camps-1948-51.md`), and Yad Ben-Zvi's own site (KC1 was read on the Mateh/CET mirror of the book chapter).

## Open rows for a person

1. Kiryat Shmuel: the location record can use WD1's point and the name קריית שמואל, but the **camp** position (the south
   part, HW1 only) and any May 1948 date need Davar (NLI) or the State Archives. No source supports a military or training
   camp there in 1948-51; the training centre stays an owner design value.
2. Rosh HaAyin and Pardes Hanna: existence is now on a reliable secondary (KC1, a Yad Ben-Zvi book) as well as the
   earlier primaries; neither camp has its own point, so a record would still have to use the town point or wait.
3. Open the GPO records (D826-082, D199-084, D200-045) at the GPO itself to confirm the captions and dates.
4. Scan the remaining 451 *Jerusalem Post* issues (13 July 1950 to 30 November 1951) for Kiryat Shmuel.
