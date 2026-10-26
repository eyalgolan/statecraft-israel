# Source check of seven draft rows of `content/sites.yaml`, slice 1 of #785 (research note, AI read, 2026-10-26)

Status: research only, an agent's read. Nothing here changes `content/`, `sim/` or `unity/`: no `checked:` date, `status`,
`reviewed_by` or attestation is touched. Refs #785, slice 1 (the owner's 19 Oct "Available now, claim a named slice" comment,
the 22 Oct fact-check sheet, the 23 Oct Beit Kama addition and the 24 Oct pointer update). Every read below that a source
prints what the row claims is tagged `[V, agent read 26 Oct 2026; a person's check pending]`: a person still has to open each
source, write the result and add their own date before any of these rows becomes `fact-checked`.

**The slice (named in the claim comment on #785):** the first seven `status: draft` rows of `content/sites.yaml` in file
order that nobody else had claimed, leaving out Kiryat Shmona and Beit Shemesh (left out of this slice): `mefalsim_field`, `beit_kama`,
`beit_kama_field`, `reim`, `reim_field`, `gilat`, `gilat_field`. The 22 Oct sheet's "Part A" rows in the slice are
`beit_kama`, `reim` and `gilat`; the four field rows are "Part B" (design placements, Q-FIELD-1).

Background: [settlements-1949-51.md](settlements-1949-51.md) rows 22, 23 and 25 (the note the rows were built from) and
[designated-site-1.md](designated-site-1.md). This note re-opens the sources and does not repeat their text.

## Result in one table

| Row | Claims read | Match | Conflict | Not covered or not reached |
|---|---|---|---|---|
| `beit_kama` | 13 | 10 | 2 (founding day: 19 April in Hebrew Wikipedia against 18 April in Gvati and English Wikipedia; and the row's comment that the 19 April comes from an Al HaMishmar item: the page's footnote on that sentence is a 1950 map, the Al HaMishmar item is on the next sentence) | 1 (the spelling "Sefiah" printed nowhere). The Al HaMishmar item's own text was not reached. |
| `reim` | 9 | 8 | 0 | 1 (the English spelling "Tel Jemmeh"; three Hebrew spellings noted) |
| `gilat` | 7 | 6 | 1 (Hebrew Wikipedia's lead gives 12 Kislev, 3 December 1949, "eve of 13 Kislev, 4 December") | 0 |
| `mefalsim_field`, `beit_kama_field`, `reim_field`, `gilat_field` | 4 hex-vs-parent checks (repo-internal) and 4 source checks | 8 | 0 | 0 |

The founding days in the rows (18 April, 20 October, 4 December 1949) are each printed by Gvati's list. What the extra sources add is
in the per-row tables below; the one open question for the owner is the one the 23 Oct comment asked for: the Al HaMishmar item
of 19 April 1949 was **not reachable from this box** (see "The Al HaMishmar item").

## Conventions

- Every source below was opened on **26 Oct 2026** (the day read). Result: **match** (the source prints what the row says),
  **conflict** (it prints something different, both given), **not covered** (nothing opened settles it), **not reached** (the
  source could not be opened, with the exact attempt).
- Quotation marks are verbatim from the page text read, so a person can find them with a search. They are not meant for event text.
- Tags: the full `[V, agent read 26 Oct 2026; a person's check pending]` when a source other than Wikipedia prints what the row
  claims; `[S]` for Wikipedia (read by `oldid`), its mirrors and a calculator; `[C]` for a conflict with both values kept;
  `[GAP]` for what is still open, with what was tried.
- Requests used `curl` and the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`; no
  header was changed to get past a block. The built-in WebSearch and WebFetch tools were used (no Tavily key on this box); a
  search result was only ever used to find a page, and every page cited was then opened and read directly.
- **Coordinates and hexes.** The CBS localities file 2024 (https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx)
  has a twelve-digit field "קואורדינטות" (column U) that the settlements note reads as an Israel Transverse Mercator easting and northing (six digits each);
  the file does not say so. Converted here with `pyproj` (EPSG:2039 to EPSG:4326) and run through `tools/hexgrid.py`
  `settlement_hex(lon, lat, grid)` against `content/map/hexes.json` on `origin/main` `0d37e9f2`, the same rule the YAML comment
  names. The reading as ITM is borne out below: the converted point lies 3 m (Gilat) and 38 m (Beit Kama) from the Hebrew Wikipedia point.
- **Hebrew dates.** Hebcal's converter (https://www.hebcal.com/converter, `cfg=json`, `g2h=1`) was run on each civil date. It is a
  calculator, not a source: `[S]`. A Hebrew day starts at nightfall, so a civil date and "the eve of" the next Hebrew date can name the same night.

## `beit_kama` (Beit Kama, בית קמה): row in `content/sites.yaml` lines 61-87

Per the 23 Oct pointer the row is lines 61-87 of `origin/main` `c7cf42a2`; on `origin/main` `0d37e9f2` (read for this note) it starts at line 61 and its
`status: draft` is line 87.

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| BK1 | Name `Beit Kama`, `name_he` `בית קמה` | CBS localities file 2024, row 287 (data row for the locality) | name column `בית קמה`, transcription `BET QAMA`, English name `Bet Qama` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| BK2 | Hex (-8, 28) from the CBS point 31.44853 N, 34.76108 E | CBS file, same row | coordinate field `177312595255`: easting 177312, northing 595255; converted: 31.44853 N, 34.76108 E; `settlement_hex` gives (-8, 28), the same as the YAML's `q`/`r` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| BK3 | "Hebrew Wikipedia 39 m away" | he.wikipedia `oldid=43790184`, geohack parameters in the infobox | `31.4483720567021_N_34.7614409433851_E`; distance to the CBS point 38.5 m (haversine), the same hex (-8, 28) | match (38.5 m, rounds to the row's 39) | [S] | 26 Oct 2026 |
| BK4 | Founded 18 April 1949, Gvati | Gvati, *100 Years of Settlement* vol. 2, appendix list (https://benyehuda.org/read/14684); the table's columns are name, region, date of founding, affiliation, origin of the name | `בֵּית-קָמָה` , `צפון הנגב` , `18.4.49` , `הקיבוץ הארצי` , `סמלי` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| BK5 | Same list: month of the going-up | Gvati, vol. 2, narrative on 1949 (same page) | "אחריהם עלו בחודש ניסן (אפריל 1949) שני קיבוצים של הקיבוץ המאוחד: ... גרעין הקיבוץ הארצי ״ספיח״ עלה לבית־קמה בנגב." The month is Nisan, April 1949; no day. | match (month only) | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| BK6 | "Hebrew Wikipedia gives 19 April 1949 [conflict, one day]" | he.wikipedia `oldid=43790184`, "History" | "קיבוץ בית קמה עלה על הקרקע בתאריך 19 באפריל 1949 במסגרת "הקיבוץ הארצי" ונקרא תחילה בשם "ספיח"[4]." | **conflict, one day, as the row records**: the page prints 19 April, Gvati and English Wikipedia 18 April | [C] (19 April: Hebrew Wikipedia; 18 April: Gvati, English Wikipedia; both kept) | 26 Oct 2026 |
| BK13 | "citing an Al HaMishmar item of 19 April 1949 that no one has read yet" | he.wikipedia `oldid=43790184`, footnote list and the places the footnote marks sit in the text | The marker `[4]` stands on the 19 April sentence quoted in BK6; footnote 4 is "מפה בהוצאת מחלקת המדידות, 1950 עם סימון היישוב בשמו ההיסטורי 'ספיח', באתר הספרייה הלאומית, אוסף המפות ע"ש ערן לאור" (link https://www.nli.org.il/he/maps/NNL_ALEPH990023671980205171/NLI). The Al HaMishmar item is footnote 5, "שבתי קפלן, "ספיח ,"משרידי הונגריה, עלה בשערי הנגב, על המשמר, 19 באפריל 1949", and its marker stands on the NEXT sentence: "המייסדים עלו מהונגריה במסגרת תנועת "השומר הצעיר" וישבו, לפני עלייתם על הקרקע, במושבה הדר (כיום חלק מהוד השרון)[5]." | **conflict with the row's comment**: the page cites the Al HaMishmar item for the founders' origin and their stay at Moshava Hadar, not for the day; the day's own footnote is a 1950 map that shows the name Sefiah. That 1950 map was not opened (the National Library answers 403). So Hebrew Wikipedia's "19 April" has no source on the page that is known to print a day | [S] | 26 Oct 2026 |
| BK7 | "English Wikipedia agrees" (18 April) | en.wikipedia `oldid=1375255214` | infobox "Founded 18 April 1949"; History: "Beit Kama was founded on 18 April 1949, The founders of Beit Kama were immigrants from Hungary who belonged to Hashomer Hatzair movement. Most of them, holocaust survivors. [citation needed]." The only inline reference after the lead is the CBS population table; no source is given for the day. | match, but the day has no source on the page (the `[citation needed]` follows the founders sentence) | [S] | 26 Oct 2026 |
| BK8 | "on 19 Nisan 5709, 18 April 1949 ... Tel Maliha" (kibbutz mynet, the founding scroll) | https://kibbutz.mynet.co.il/local_news/article/m_154712, read in the Internet Archive copy of 19 Jul 2022 (http://web.archive.org/web/20220719161837id_/https://kibbutz.mynet.co.il/local_news/article/m_154712; HTTP 200, 189,954 bytes); the live page answers 403 (challenge page, 5,629 bytes) to `curl` with and without the project User-Agent, and the WebFetch tool also got 403. A review agent's check (agent read 26 Oct 2026; a person's check pending) read the live page and found the same text | "בית קמה: הסיפור שמאחורי התמונה ההיסטורית" (Yizhar Ben Nachum, updated 20.09.15): "מגילת היסוד של "קיבוץ השומר הצעיר ספיח", שעלה על הקרקע באדמות תל-מליחה בי"ט בניסן ה'תש"ט". 19 Nisan 5709 is 18 April 1949 by Hebcal (and 19 April 1949 = 20 Nisan). The page prints the Hebrew date only, no civil date | match (Hebrew date; the civil day is Hebcal's) | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| BK9 | Name `Sefiah`, renamed Beit Kama in June 1951 (israeliana.org, the archive page) | https://www.israeliana.org/post/%D7%90%D7%A8%D7%9B%D7%99%D7%95%D7%9F-%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%91%D7%99%D7%AA-%D7%A7%D7%9E%D7%94 (the URL in the row), page text | "הקיבוץ עלה על הקרקע באפריל 1949 במסגרת "הקיבוץ הארצי" ונקרא תחילה בשם "ספיח"." and "ביוני 1951 הוחלף שם הקיבוץ ל"בית קמה"." The page repeats Hebrew Wikipedia's History text almost word for word (including the scroll quotation), so it is not an independent reading of the 1951 date; Hebrew Wikipedia's source for June 1951 is "ספיח - בית קמה, הארץ, 21 ביוני 1951" (not opened). | match | [S] | 26 Oct 2026 |
| BK10 | Spelling `Sefiah` | en.wikipedia `oldid=1375255214`, Etymology | "The kibbutz was initially called "Safiach", but later became Beit Kama" | not covered: the row's transliteration differs from English Wikipedia's; the Hebrew is `ספיח` on every page | [S] | 26 Oct 2026 |
| BK11 | By the Hashomer Hatzair group, from Hungary | he.wikipedia `oldid=43790184`; Gvati list | he.wiki: "המייסדים עלו מהונגריה במסגרת תנועת "השומר הצעיר"". Gvati's affiliation column: `הקיבוץ הארצי`. | match | [S] | 26 Oct 2026 |
| BK12 | CBS: founding year 1949 | CBS file, row 287 | founding-year column `1949`; organisational affiliation code `15` (not decoded here) | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |

Extra read, not in the row's sources: https://www.246.co.il/daat/itmab.asp?fn=802032049-433 ("מאגר הידע על קצה המזלג", from the
kibbutz movement's databases, read as windows-1255) prints "שנת עלייה לקרקע : 1949 שם קודם: ספיח" and "מוצא המייסדים : הונגריה פולין". Year only: `[S]`.

### The Al HaMishmar item of 19 April 1949 (the owner's 23 Oct addition): not reached

The item is the one Hebrew Wikipedia footnotes (footnote 5, on the sentence about the founders' origin, see BK13): Shabtai Kaplan,
"ספיח, משרידי הונגריה, עלה בשערי הנגב", *Al HaMishmar*, 19 April 1949. The National Library's viewer page for it is
https://www.nli.org.il/he/newspapers/ahr/1949/04/19/01/article/13 (page 2, article 13, `oid=ahr19490419-01.2.13`). Whether the item
supports 18 or 19 April is **still open, `[GAP]`**: its text was not read. Searched and tried on 26 Oct 2026, nothing changed in any request:

| What was tried | Result |
|---|---|
| https://www.nli.org.il/he/newspapers/hmishmar/1949/04/19/01/ (National Library historical press, Al HaMishmar) | HTTP 403, a "Checking your browser..." challenge page (6,720 bytes) |
| the same with `/en/`, then `/he/newspapers/hmishmar/1949/04/19`, `/he/newspapers/hmishmar`, `/he/discover/newspapers` | all HTTP 403, the same challenge page (these paths use the code `hmishmar`; the library's own code for the paper, in the Wikipedia footnote, is `ahr`) |
| https://www.nli.org.il/he/newspapers/ahr/1949/04/19/01/article/13, and the same with the Wikipedia link's parameters; the issue PDF `?a=is&oid=ahr19490419-01&type=staticpdf`; the article PDF `oid=ahr19490419-01.2.13&type=logicalsectionpdf` | all HTTP 403, the same challenge page |
| Internet Archive CDX, `url=nli.org.il/he/newspapers/ahr/1949/04/19*` (https://web.archive.org/cdx/search/cdx), lines read (I read the first 12 lines it printed) | the captures of the viewer page of article 13 (18 Sep 2023, 16 Dec 2024, several URL variants) and of the issue page (13 Nov 2020) are HTTP 200 (others in the list are other articles of that issue, some of them HTTP 404). Opened: they hold the page furniture and the article's metadata only: title `"ספיח,"משר יד י הונגר יה . עלה בשערי הנגב` (the spacing is the library's OCR), paper `על המשמר`, date `19 אפריל 1949`, article id 2.13 (page 2). The article text and the page image are not in any capture (the viewer loads them by script). The title is therefore read; the text is not. |
| https://www.jpress.org.il/ and its Client.asp link for `Publication=HMS&Date=19490419` | both HTTP 403 |
| Internet Archive availability API for `nli.org.il/he/newspapers/hmishmar/1949/04/19/01/` (the wrong paper code) | `"archived_snapshots": {}` (no capture) |
| Internet Archive CDX query `url=nli.org.il/he/newspapers/hmishmar/1949/04/19*&limit=10` | empty body (0 captures) |
| WebSearch, query `"משרידי הונגריה" "ספיח" על המשמר 19 באפריל 1949 שערי הנגב` | 10 results, none the newspaper page; the only one naming the item was the Hebrew Wikipedia article's footnote (and its mirror hamichlol.org.il). The summary text a search tool wrote is not a source and is not used. |

What a person needs: open the item at the National Library's historical press link above (the site answers scripts with a challenge
page; a browser may get through), find the dateline or the words "today / yesterday / this week" for the going-up, and write down whether
the item says it happened on 18 or 19 April. The item's own headline is in the past tense ("עלה", went up) and carries the
paper's date; that alone does not say which day. Also open footnote 4's 1950 map (BK13) if the day's footnote matters. The bulletin date of a daily paper is not the event date (the owner's 24 Oct rule, #1596, for JTA; the same logic applies to any daily).
Nothing here assumes the answer.

## `beit_kama_field` (Beit Kama fields, שדות בית קמה): lines 88-98

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| BF1 | Hex (-8, 28) equals the parent's | `content/sites.yaml` on `origin/main` `0d37e9f2` | `beit_kama`: `q: -8`, `r: 28`; `beit_kama_field`: `q: -8`, `r: 28` | match | repo check, no outside source | 26 Oct 2026 |
| BF2 | Source is the parent's | the field's one source is the CBS file; it is the parent's sixth source (`beit_kama`, BK2 and BK12) | the CBS row 287 prints the point and year used for the parent | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |

## `reim` (Re'im, רעים): lines 99-122

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| R1 | Name, `name_he` `רעים` | CBS file, row 1354 | `רעים`, `RE'IM`, `Re'im` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R2 | Hex (-12, 29) from the CBS point 31.38552 N, 34.45932 E | CBS file, same row | coordinate field `148580588423`: easting 148580, northing 588423; converted 31.38552 N, 34.45932 E; `settlement_hex` (-12, 29) | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R3 | CBS founding year 1949 | CBS file, same row | founding-year column `1949` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R4 | Founded 20 October 1949, Gvati | Gvati vol. 2 list | `רֵעִים` , `הנגב המערבי` , `20.10.49` , `הקיבוץ המאוחד` , `לזכר חברי הגרעין שנפלו במלחמת השחרור` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R5 | "two Palmach database entries the same day" (7953) | https://palmach.org.il/history/database/?itemId=7953 | "קיבוץ של תנועת הקיבוץ המאוחד. נוסד ב- 20 באוקטובר 1949 בידי חברי הכשרת "הצופים", גרעין ו' - הכשרת נען – שילר." and "לאחר תקופת התארגנות והכשרה נוספת בגבת, הקימו ב- 20 באוקטובר 1949 יישוב משלט על תל ג'מעה בנגב - קיבוץ רעים." | match (date, the Scouts' garin Vav from Na'an-Schiller, "יישוב משלט" = lookout settlement, the tel) | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R6 | the second entry (5312, "no comment of its own in the file") | https://palmach.org.il/history/database/?itemId=5312 (the hachshara page "הכשרת נען - שילר 1948 - גרעין הצופים ו'") | "לאחר תקופת התארגנות והכשרה נוספת בגבת, הקימו ב- 20 באוקטובר 1949 יישוב משלט על תל ג'מעה בנגב - קיבוץ רעים ('הקיבוץ-המאוחד')." Same words as entry 7953, same publisher (the Palmach Generation Association): the two entries are one source, not two. | match; one line of evidence, not two | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R7 | founded by the Scouts garin Vav, first called Tel Re'im (Yad Ben-Zvi) | https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%A8%D7%A2%D7%99%D7%9D/ (Re'im page) | "הקיבוץ הוקם בשנת 1949 על ידי גרעין של תנועת הצופים ו' ששירת בפלמ"ח. ... בתחילה היישוב נקרא 'תל רעים' על שם תל גמה שליד היישוב." Year only, no day. | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R8 | the tel's name in English, "Tel Jemmeh" | Palmach: `תל ג'מעה`; Yad Ben-Zvi: `תל גמה`; the kibbutz's own page (https://www.reim.org.il/cgi-webaxy/item?152=): "על שם תל ג'מה שליד היישוב"; Yad Ben-Zvi also prints the Arabic-based name "תל אלג'אמע" | three Hebrew spellings of one tel; no page opened prints the English "Jemmeh" | not covered: the English spelling is the row's transliteration; the identity (Palmach and Yad Ben-Zvi both put it as the tel next to the settlement) is a match | [S] | 26 Oct 2026 |
| R9 | "Scouts garin Vav from the Na'an hachshara" | Palmach 5312 | "הכשרת נען - שילר 1948 - גרעין הצופים ו'"; "פרטים אודות ההכשרה"; the garin mustered 150 in 1948 ("מספר המתגייסים: 150") | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| R10 | the record "covers the founding only" (owner, #782, Q-SENS-1) | the row's `founded:` only, nothing read in a source | | not a source claim | n/a | 26 Oct 2026 |

Not checked here (repo map data, not a source claim): "Next to the Egypt line hexes shown from 22 February 1950 in `map/armistice_hexes_1949.yaml`".

## `reim_field` (Re'im fields, שדות רעים): lines 123-133

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| RF1 | Hex equals the parent's | `content/sites.yaml`, `origin/main` `0d37e9f2` | `reim`: `q: -12`, `r: 29`; `reim_field`: `q: -12`, `r: 29` | match | repo check, no outside source | 26 Oct 2026 |
| RF2 | Source is the parent's | the field's one source, the CBS file, is the parent's fifth source (R1 to R3) | | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |

## `gilat` (Gilat, גילת): lines 220-238

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| G1 | Name, `name_he` `גילת` | CBS file, row 401 | `גילת`, `GILAT`, `Gilat` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| G2 | Hex (-10, 30) from the CBS point 31.32798 N, 34.65203 E | CBS file, same row | coordinate field `166879581935`: easting 166879, northing 581935; converted 31.32798 N, 34.65203 E; `settlement_hex` (-10, 30) | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| G3 | "Hebrew Wikipedia 3 m away" | he.wikipedia `oldid=43560845`, geohack parameters | `31.3279962872173_N_34.6520018312485_E`; distance 3.2 m | match | [S] | 26 Oct 2026 |
| G4 | CBS founding year 1949 | CBS file, same row | founding-year column `1949` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| G5 | Gvati: 4 December 1949, a moshav of Tnuat HaMoshavim | Gvati vol. 2 list | `גִילַת` , `הנגב הצפוני` , `4.12.49` , `תנועת המושבים` , `“פרח תפרח ותגל אף גילת ורנן” (ישעיהו ל"ה, ב')` | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| G6 | "Going onto the land on 13 Kislev 5710, 4 December 1949" and "Hebrew Wikipedia the Hebrew date" | he.wikipedia `oldid=43560845` | Body: "גילת, מושב העולים השני אשר הוקם בנגב עלה על הקרקע ב-י"ג בכסלו ה'תש"י (1949) על ידי עולים מתוניסיה." Lead: "מושב גילת הוקם בי"ב בכסלו תש"י 3 בדצמבר 1949 (אור לי"ג בכסלו תש"י 4 בדצמבר 1949) והוא אחד מהמושבים הוותיקים ביותר." Neither sentence has a reference. Hebcal: 13 Kislev 5710 = 4 December 1949, 12 Kislev 5710 = 3 December 1949. | **conflict, one day, inside the one page**: the body says 13 Kislev (4 December); the lead says "established" on 12 Kislev, 3 December, and in brackets "the eve of 13 Kislev", 4 December. The row's comment records only the Hebrew date. The two readings fit one night (3 to 4 December), so the row's 4 December agrees with Gvati; the page does not say which civil day the going-up was. | [C] (4 December: Gvati and the page's body; 3 December: the page's lead; both kept) | 26 Oct 2026 |
| G7 | Immigrants from Tunisia | he.wikipedia `oldid=43560845` | "על ידי עולים מתוניסיה" (infobox: `תוניסיה`). Gvati's list has no origin of the settlers. | match | [S] | 26 Oct 2026 |

Extra read, not in the row's sources: "ההתיישבות החקלאית בנגב, 1959-1949" (https://www.lib.cet.ac.il/Pages/item.asp?item=13006&rel=1): "אחריו עלה מושב גילת בדצמבר 1949." Month only; the same page puts Tifrah in
September 1949 ("עלה על הקרקע בספטמבר 1949"). `[S]` (no author or source line on the page read).
Tried and not reached: https://www.merchavim.org.il/גילת/ (HTTP 403), https://www.news1.co.il/Archive/0024-D-114633-00.html (HTTP 403),
https://www.ravdori.co.il/stories/... (HTTP 403).

## `gilat_field` (Gilat fields, שדות גילת): lines 239-249

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| GF1 | Hex equals the parent's | `content/sites.yaml`, `origin/main` `0d37e9f2` | `gilat`: `q: -10`, `r: 30`; `gilat_field`: `q: -10`, `r: 30` | match | repo check, no outside source | 26 Oct 2026 |
| GF2 | Source is the parent's | the field's one source, the CBS file, is the parent's third source (G1, G2, G4) | | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |

## `mefalsim_field` (Mefalsim fields, שדות מפלסים): lines 49-60

The parent `mefalsim` is the one `fact-checked` row and is not part of the slice; its hex and its Wikidata source are read here only as far as the field row needs them.

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| MF1 | Hex (-10, 27) equals the parent's | `content/sites.yaml`, `origin/main` `0d37e9f2` | `mefalsim`: `q: -10`, `r: 27`; `mefalsim_field`: `q: -10`, `r: 27` | match | repo check, no outside source | 26 Oct 2026 |
| MF2 | Source is the parent's "hex" source: Wikidata Q2890190 (the YAML itself cites it for the hex) | https://www.wikidata.org/wiki/Q2890190 (revision 2525233179), `Special:EntityData` JSON | labels en `Mefalsim`, he `מפלסים`; "coordinate location 31°30'7"N, 34°33'52"E" (31.501897 N, 34.564397 E); `settlement_hex` on that point gives (-10, 27), as does the row's rounded 31.5019 N, 34.5644 E | match | [V, agent read 26 Oct 2026; a person's check pending] | 26 Oct 2026 |
| MF3 | Name `name_he` `שדות מפלסים` | the label is a design name ("the fields of Mefalsim", #564), not printed anywhere | | not a source claim | n/a | 26 Oct 2026 |

Aside on the parent, for whoever re-reads it (outside this slice, no change proposed): Gvati's narrative sentence that carries the month "בסיון (מאי 1949)" is about Moshav Omer; the next sentence, on the Mefalsim garin, has no month, and his table
prints `12.6.49` for Mefalsim (Hebcal: 15 Sivan 5709). No conflict; the table is the one the row uses. `[S]`. The CBS point for Mefalsim
(31.50204 N, 34.56138 E, 286 m from Wikidata's) is in the same hex.

## Sources used and how far each was reached

| Source | How it was read | Day |
|---|---|---|
| Gvati, https://benyehuda.org/read/14684 | HTTP 200; the appendix table and the 1949 narrative | 26 Oct 2026 |
| CBS localities file 2024, `bycode2024.xlsx` | HTTP 200; rows 287, 401, 902, 1354 read from the sheet XML | 26 Oct 2026 |
| he.wikipedia `oldid=43790184` (Beit Kama), `oldid=43560845` (Gilat); en.wikipedia `oldid=1375255214` (Beit Kama) | HTTP 200, page text | 26 Oct 2026 |
| israeliana.org Beit Kama archive page | HTTP 200 | 26 Oct 2026 |
| Palmach database 7953 and 5312; Yad Ben-Zvi (israelalbum.org.il) Re'im; reim.org.il; lib.cet.ac.il; 246.co.il | HTTP 200 | 26 Oct 2026 |
| Wikidata Q2890190 | HTTP 200, page and `Special:EntityData` JSON | 26 Oct 2026 |
| nli.org.il (Al HaMishmar), jpress.org.il, merchavim.org.il, news1.co.il, ravdori.co.il | HTTP 403 (challenge pages), not reached; nothing was spoofed | 26 Oct 2026 |
| Internet Archive copy of kibbutz.mynet.co.il (Beit Kama), snapshot 19 Jul 2022 | HTTP 200 (the live page answers 403) | 26 Oct 2026 |
| Hebcal converter | HTTP 200, JSON (calculator) | 26 Oct 2026 |
| `content/sites.yaml`, `content/map/hexes.json`, `tools/hexgrid.py` on `origin/main` | read in the worktree | 26 Oct 2026 |

## AI assistance

Claude Code opened the sources and wrote this note. A second, independent Claude (Sonnet) agent re-checks the quotes (see the PR). Every
read tagged `[V, agent read 26 Oct 2026; a person's check pending]` is an agent's read; a person's check is pending, and this note
changes no `checked:` date, `status`, `reviewed_by` or attestation in `content/sites.yaml`.
