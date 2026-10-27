# Source check of the Kiryat Shmona and Beit Shemesh rows of `content/sites.yaml`, slice 2 of #785 (research note, AI read, 2026-10-27)

Status: research only, an agent's read. Nothing here changes `content/`, `sim/` or `unity/`: no `checked:` date, `status`,
`reviewed_by` or attestation is touched. Refs #785, slice 2, the same method as slice 1 ([sites-draft-rows-check-785.md](sites-draft-rows-check-785.md),
PR #1783). Every read below that a non-Wikipedia source prints what the row claims is tagged
`[V, agent read 27 Oct 2026; a person's check pending]`: a person still has to open each source, write the result and add their own
date before any of these rows becomes `fact-checked`.

**The slice (claimed on #785 on 27 Oct 2026, after the owner's 26 Oct "nobody has claimed" comment):** the four `status: draft` rows
`kiryat_shmona`, `kiryat_shmona_field`, `beit_shemesh` and `beit_shemesh_field`. The two yishuv rows are "Part A" of the owner's 22 Oct
fact-check sheet; the two field rows are "Part B" (design placements, Q-FIELD-1). The founding days themselves are the owner's rulings of
17 Oct 2026 on #785 (18 July 1949 for Kiryat Shmona; the month December 1950, so the 15th, for Beit Shemesh). This note checks what each
cited source prints against them and does not re-open the rulings.

## Result in one table

| Row | Claims read | Match | Conflict | Not covered or not reached |
|---|---|---|---|---|
| `kiryat_shmona` | 13 | 9 | 2 (KS11: Hebrew Wikipedia dates the start in July 1949 and the ma'abara on 26 December 1949 in one article; KS13: the CBS year 1950 against 1949 in Yad Ben-Zvi, the Ministry sheet and both Wikipedias) | 2 (the municipality's two pages, KS6 and KS7: HTTP 403, and no archived copy holds the text; so the 18 July against 21 July conflict in the row is not re-read). **Correction 27 Oct 2026, after the owner's review:** both pages were read by a review agent on 27 Oct 2026 (a person's check pending); see KS6 and KS7, so these two are no longer "not reached". `[C]` first name: the name page prints "חלסה", the chronology prints "קריית יוסף"; both kept |
| `kiryat_shmona_field` | 2 | 2 | 0 | 0 |
| `beit_shemesh` | 10 | 7 | 1 (BS7: Hebrew Wikipedia prints 6 December 1950, which the row does not use; the National Library prints only the month) | 1 (BS8: no source read dates the first families separately, the row's own `[GAP]`); BS5 is the owner's monthly-turn rule, not a source claim |
| `beit_shemesh_field` | 2 | 2 | 0 | 0 |

What no source read gives is a day for Beit Shemesh other than Hebrew Wikipedia's 6 December 1950, and a day for Kiryat Shmona
other than the two Wikipedias' 18 July 1949 (one line of evidence, see KS3 to KS5). The row's second independent source for
18 July, the municipality's chronology (https://www.k-8.co.il/he/faq/3/), **was not reached** from this box (HTTP 403 to `curl`
with the project User-Agent, to the WebFetch tool, and no Internet Archive capture of it). It is the only open item the owner's
ruling rests on, and a person has to read it: see "What a person has to open".

**Correction 27 Oct 2026, after the owner's review:** the paragraph above is kept as first written. The owner reports that a review agent did read both municipality pages on 27 Oct 2026 (the chronology, 314,927 bytes; the name page, 131,500 bytes). The chronology prints 18.07.49 as a decision to make the abandoned Arab village Halsa a settlement named Kiryat Yosef, and prints arrivals of 18 Yemeni families on 08.07.49 and on 21.07.49; the name page prints 21.7.49 with the first name "חלסה". That is a review agent's check (agent read 27 Oct 2026; a person's check pending). It confirms the decision-versus-arrival point: 18.07.49 is a decision, not an arrival. A person's check is still pending. `[C]` first name: the name page prints "חלסה", the chronology prints "קריית יוסף"; both kept.

## Conventions

- Every source below was opened on **27 Oct 2026** (the day read). Result: **match** (the source prints what the row says),
  **conflict** (it prints something different, both given), **not covered** (nothing opened settles it), **not reached** (the
  source could not be opened, with the exact attempt).
- Quotation marks are verbatim from the page text read, so a person can find them with a search. They are not meant for event text.
- Tags: the full `[V, agent read 27 Oct 2026; a person's check pending]` when a source other than Wikipedia prints what the row
  claims; `[S]` for Wikipedia (read by `oldid`), its mirrors and a calculator; `[C]` for a conflict with both values kept;
  `[GAP]` for what is still open, with what was tried. Derived readings (a hex, a Hebrew date) are never `[V]`.
- **Page images.** The one PDF tagged `[V]` below (the Ministry of Education sheet) was also rendered to an image and viewed on
  27 Oct 2026: the two lines quoted for it (Beit Shemesh and Kiryat Shmona) read the same on the page image as in the text layer.
  No other source was viewed as an image: HTML pages and the CBS sheet were read as text.
- Requests used `curl` and the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
  One extra probe of the municipality's site with a generic browser User-Agent also answered 403 (it was a probe, no content was
  obtained from it). The built-in WebFetch tool was also used (no Tavily key on this box); no search result is cited as a source.
- **Coordinates and hexes.** The CBS localities file 2024 (https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx)
  has a twelve-digit field "קואורדינטות" (column U) that the settlements note reads as an Israel Transverse Mercator easting and
  northing (six digits each); the file does not say so. Converted here with `pyproj` (EPSG:2039 to EPSG:4326) and run through
  `tools/hexgrid.py` `settlement_hex(lon, lat, grid)` against `content/map/hexes.json` on `origin/main` `b02f82a0`, the same rule the
  YAML comment names. The reading as ITM is borne out below: the converted points lie 215 m (Kiryat Shmona) and 566 m (Beit Shemesh)
  from the Hebrew Wikipedia points. Distances are haversine on a 6,371,008.8 m sphere.
- **Hebrew dates.** Hebcal's converter (https://www.hebcal.com/converter, `cfg=json`, `g2h=1`) was run on each civil date. It is a
  calculator, not a source: `[S]`.

## `kiryat_shmona` (Kiryat Shmona, קריית שמונה): row in `content/sites.yaml` lines 134-170

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| KS1 | Name `Kiryat Shmona`, `name_he` `קריית שמונה` | CBS localities file 2024, row 1292 (data row for the locality) | name column `קריית שמונה`, transcription `QIRYAT SHEMONA`, English name `Qiryat Shemona`; status "עירייה" | match (the row's transliteration is the common one; the CBS prints `Qiryat Shemona`) | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |
| KS2 | Hex (14, 2) from the CBS point 33.21316 N, 35.57461 E | CBS file, same row | coordinate field `253967790918`: easting 253967, northing 790918; converted: 33.21316 N, 35.57461 E; `settlement_hex` gives (14, 2), the same as the YAML's `q`/`r` | match | [V, agent read 27 Oct 2026; a person's check pending] for the printed coordinate field only; the converted point and the hex are derived, not [V] | 27 Oct 2026 |
| KS3 | "Hebrew Wikipedia 214 m away" | he.wikipedia `oldid=44043359`, geohack parameters of the rendered page | `33.2116311318334_N_35.573204923663_E`; distance to the CBS point 214.9 m, the same hex (14, 2) | match (214.9 m as computed here with the conversion above; the row's 214 is a cut of that, not a rounding). **Correction 27 Oct 2026, after the owner's review:** from the row's rounded point (33.21316, 35.57461) the distance is 214.4 m, so the row's 214 is consistent (derived; recomputed on 27 Oct 2026) | [S] | 27 Oct 2026 |
| KS4 | "the first families" 18 July 1949: 14 families of immigrants from Yemen (Hebrew Wikipedia, citing Goldstein, not read) | he.wikipedia `oldid=44043359`, section "המתיישבים הראשונים 1949 עד 1951" | "14 משפחות ראשונות מקרב עולי תימן הגיעו לבתים העזובים של חלסה ב-18 ביולי 1949 כדי לאכלס את היישוב החדש, ככפר חקלאי למחצה שנקרא תחילה "קריית יוסף" על שמו של יוסף טרומפלדור." The sentence carries no footnote of its own: the first footnotes after it are two Goldstein items (2017 and 2021, ResearchGate), placed on the later sentence about families leaving in 1952 to 1953. | match for the day and the 14 Yemeni families; the row's "citing Goldstein" is the page's nearest footnotes, not a footnote on this sentence. Goldstein itself was not opened | [S] | 27 Oct 2026 |
| KS5 | "English Wikipedia: 18 July 1949 (the same line of evidence as the Hebrew article)" | en.wikipedia `oldid=1376419614`, History | "The first residents were fourteen Yemenite Jews who arrived on July 18, 1949, and were followed by more Yemenis a month later." The footnote on that sentence is Goldstein 2016, *Journal of Israeli History* 35, pp. 17-37, "The kibbutz and the ma'abara ... 1949-1953" (not opened). The infobox says only "Founded 1949". | match; one line of evidence with KS4 only if both rest on Goldstein, which is the likely source of both and is unread, so the row's "same line" is a reading, not a read. **Correction 27 Oct 2026, after the owner's review:** the footnote placement above is wrong. In the en.wikipedia wikitext of `oldid=1376419614` the `<ref name=Goldstein />` follows the next sentence ("By July 1951, the population had grown to nearly 4,000."); the 18 July sentence has no footnote of its own, the same as KS4 (re-read in the wikitext on 27 Oct 2026) | [S] | 27 Oct 2026 |
| KS6 | Municipality's "Main dates and events" chronology: 18.07.49, the decision to make the place a settlement named Kiryat Yosef | https://www.k-8.co.il/he/faq/3/ | not reached: HTTP 403 to `curl` (a Cloudflare-style challenge page, 4,544 bytes), HTTP 403 to the WebFetch tool, and the Internet Archive has no capture of this URL (availability API `archived_snapshots: {}`; the CDX query returned nothing) | **not reached**, `[GAP]`: the second source for 18 July is unread, so a person has to open it. **Correction 27 Oct 2026, after the owner's review (supersedes the "not reached" above):** the owner reports that a review agent did get this page on 27 Oct 2026 (314,927 bytes) and that a saved copy reads, under "שנת 1949": "18.07.49 הוחלט להפוך את הכפר הערבי הנטוש חלסה לישוב בשם קריית יוסף.", "08.07.49 מגיעות 18 משפחות מעולי תימן "מרבד הקסמים" לחלסה." and "21.07.49 מגיעות 18 משפחות מעולי תימן "מרבד הקסמים" לחלסה". So 18.07.49 is a decision, and families arrive on 08.07.49 and 21.07.49. This is a review agent's check (agent read 27 Oct 2026; a person's check pending); this slice's own request to the page was HTTP 403 again on 27 Oct 2026, so the lines are taken from the owner's review, not from a read by this slice. `[C]` first name: the name page prints "חלסה", the chronology prints "קריית יוסף"; both kept | n/a (see correction) | 27 Oct 2026 |
| KS7 | Municipality's "The evolution of a name": established on 21.7.49 (the conflict above) | https://www.k-8.co.il/he/05-04-2020-%D7%92%D7%9C%D7%92%D7%95%D7%9C%D7%95-%D7%A9%D7%9C-%D7%A9%D7%9D/ | HTTP 403 to `curl` and to WebFetch. The Internet Archive has a capture of 9 Oct 2025 (HTTP 200 as served), but its text is only a verification loader page ("One moment, please... Please wait while your request is being verified..."), not the article | **not reached**, `[GAP]`: the row's 21 July and its "[Conflict]" rest on the earlier research note, not on a read here. **Correction 27 Oct 2026, after the owner's review (supersedes the "not reached" above):** the owner reports that the same review agent read this name page (131,500 bytes) and that it says: "ראשית ניתן לעיר עם הקמתה בתאריך 21.7.49 השם "חלסה" ע"ש הכפר הערבי ...". This is a review agent's check (agent read 27 Oct 2026; a person's check pending), taken from the owner's review. `[C]`: this page gives the first name as "חלסה", the chronology (KS6) gives "קריית יוסף" for the 18.07.49 decision; both kept | n/a (see correction) | 27 Oct 2026 |
| KS8 | Yad Ben-Zvi, "Israel Revealed to the Eye": established in July 1949, "on the remains of the Arab village Halsa" | https://israelalbum.org.il/mercazim/%D7%A7%D7%A8%D7%99%D7%99%D7%AA-%D7%A9%D7%9E%D7%95%D7%A0%D7%94/ | "קריית שמונה הוקמה ביולי 1949 על שרידי הכפר הערבי 'חלסה'. ראשוני העולים הגיעו מתימן ובשנת 1951 הצטרפו אליהם אלפי עולים שבאו מעיראק ואיראן במבצע "עזרא ונחמיה"." Month only, no day. | match (July 1949; the village name quoted in the row's comment as "Halsa"; the first settlers from Yemen) | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |
| KS9 | Founded "on the remains of the Arab village Halsa" (Yad Ben-Zvi), al-Khalisa, abandoned in 1948 (Hebrew Wikipedia) | he.wikipedia `oldid=44043359`, "היסטוריה" | "הוקמה קריית שמונה כמעברה על חורבות הכפר הערבי "אל-ח'אלצה" (חלסה) הנטוש, שתושביו נמלטו במלחמת העצמאות במעלה ההר לדרום לבנון." | match | [S] | 27 Oct 2026 |
| KS10 | "depopulated", English Wikipedia | en.wikipedia `oldid=1376419614`, History | "Kiryat Shmona was established in 1949 on the site of the former depopulated Palestinian Arab village of Al-Khalisa" (the wikilink text is "depopulated"; the footnote on the sentence is Vilnai, *Ariel Encyclopedia* 7, 1979, not opened). The following sentence, on why the inhabitants left, has a Morris 2004 footnote. **Correction 27 Oct 2026, after the owner's review:** the Morris footnote sits on the rest of the same sentence (its relative clause, after "whose inhabitants fled ..."), not on a following one (re-read in the wikitext of `oldid=1376419614` on 27 Oct 2026). | match | [S] | 27 Oct 2026 |
| KS11 | Hebrew Wikipedia dates the ma'abara to 26 December 1949 (citing Gelber, not read; one source) | he.wikipedia `oldid=44043359`, "היסטוריה" | "בו' בטבת ה'תש"י, 26 בדצמבר 1949, הוקמה קריית שמונה כמעברה על חורבות הכפר הערבי ..." with a footnote to Yoav Gelber, *Komemiyut ve-Nakba* (Dvir, 2004), p. 287, chapter "מניעת השיבה". Hebcal: 26 Dec 1949 = 6 Tevet 5710, as printed. | match for what the row says (one source, Gelber unread). It is also a conflict inside the one page with KS4: the same article dates the start in July 1949 and the ma'abara on 26 Dec 1949; the row keeps both and uses 18 July as the owner ruled | [S] | 27 Oct 2026 |
| KS12 | Ministry of Education sheet is not a source of this row, but prints the year (added read) | https://meyda.education.gov.il/files/Pop/0files/geography_adam_sviva/70-shana/ari-pitoah.pdf (the same sheet the Beit Shemesh row cites) | "קרית שמונה: ... הוקמה בשנת 1949 בתור מעברה לעולים מתימן ומרומניה, אך בהמשך הגיעו אליה עולים רבים גם מצפון אפריקה. בתחילה נקראה העיר "קריית יוסף" על שמו של יוסף טרומפלדור ..." Year only. | extra read, not in the row's sources: agrees with the year 1949 and the name Kiryat Yosef | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |
| KS13 | "The CBS file gives the year 1950" | CBS file, row 1292 | founding-year column `1950` | match for what the row says. **Conflict with the rest, kept:** the CBS year (1950) differs from the 1949 of Yad Ben-Zvi, the Ministry sheet and both Wikipedias (the CBS may date the town from its renaming or its first permanent houses; the file does not say). The row's `founded: 1949-07-18` follows the owner's ruling, not the CBS year | [V, agent read 27 Oct 2026; a person's check pending] for the 1950; [C] for the disagreement | 27 Oct 2026 |

Not checked here (repo map data or earlier notes, not a source claim in this slice): "No 1948 settlement in the hex", the hex's
Lebanon line (`lebanon_adl` in `map/armistice_hexes_1949.yaml`) and the settlements note's row 7.

### Independent dates searched for and not found

The owner's ruling rests on two independent sources for 18 July 1949; one of them (the municipality) was not reached. The search for a
period source for any day of the founding found nothing:

| What was tried | Result |
|---|---|
| JTA archive, WordPress JSON `https://www.jta.org/wp-json/wp/v2/archive`, single-word searches (`Kiryat`, `Qiryat`, `Kiriat`, `Halsa`, `Khalsa`, `Shmona`, `Shemona`, `Shemesh`, `Hartuv`, `Har Tov`, `Yemenite`, `Yemenites`, `Galilee`, `transit camp`, `maabara`, `Beth Shemesh`, `Kiryat Yosef`), items dated 1 Jun 1949 to 1 Mar 1951, 388 distinct items fetched and their text searched for `Shmona`, `Shemona`, `Kiryat Yosef`, `Halsa`, `Khalsa`, `Khalisa`, `Har Tov`, `Hartuv` and the Beit Shemesh spellings | no item reports the founding of Kiryat Shmona or of Ma'abarat Har-Tov. Two items name `Hartuv`: 8 Feb 1950 (a freight train "from Hartuv" in the snow) and 19 Jun 1950 ("the Hartuv-Beit Jibrin area a security zone"); both are the Hartuv railway station and area, not the ma'abara, and not a bulletin date for any founding. The bulletin dateline is not the event date in any case (owner's 24 Oct rule, #1596) |
| Internet Archive, *The Jerusalem Post* issues 20 Nov 1950 to 5 Jan 1951 (41 issues, `per_jerusalem-post_the-jerusalem-post_*`, full text `_djvu.txt`), searched for `Har Tuv`, `Har Tov`, `Hartuv`, `Beit Shemesh` and spellings, `Shemona`, `Shmona`, `Kiryat Yosef`, `Halsa` | no usable hit. `Hartuvy` (4 Dec 1950, "police at Hartuvy", OCR noise) and `Khalsa` (7 Dec 1950, a jumbled OCR column) were not read as evidence. The Kiryat Shmona founding (July 1949) is before the archive's run of this paper (April 1950 on), so it was not searched |
| CBS yearbooks `shnaton1950_num1.pdf` to `shnaton1955_num6.pdf` under `https://www.cbs.gov.il/he/publications/doclib/<year>/shnaton/` | each answers HTTP 200 with a 2,056-byte HTML "Oops, Something is wrong" page, not a PDF: not reached |

## `kiryat_shmona_field` (Kiryat Shmona fields, שדות קריית שמונה): lines 171-181

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| KF1 | Hex (14, 2) equals the parent's | `content/sites.yaml` on `origin/main` `b02f82a0` | `kiryat_shmona`: `q: 14`, `r: 2`; `kiryat_shmona_field`: `q: 14`, `r: 2` | match | repo check, no outside source | 27 Oct 2026 |
| KF2 | Source is the parent's | the field's one source is the CBS file; it is the parent's sixth source (KS1, KS2, KS13) | the CBS row 1292 prints the point and year used for the parent | match | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |

## `beit_shemesh` (Beit Shemesh, בית שמש): row in `content/sites.yaml` lines 182-208

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| BS1 | Name `Beit Shemesh`, `name_he` `בית שמש` | CBS file, row 292 | `בית שמש`, `BET SHEMESH`, `Bet Shemesh`; status "עירייה" | match (CBS prints `Bet Shemesh`) | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |
| BS2 | Hex (-3, 24) from the CBS point 31.73178 N, 34.98828 E | CBS file, same row | coordinate field `198971626597`: easting 198971, northing 626597; converted 31.73178 N, 34.98828 E; `settlement_hex` gives (-3, 24), the same as the YAML's `q`/`r` | match | [V, agent read 27 Oct 2026; a person's check pending] for the printed coordinate field only; the converted point and the hex are derived, not [V] | 27 Oct 2026 |
| BS3 | "Hebrew Wikipedia 562 m away, the same hex" | he.wikipedia `oldid=43925345`, geohack parameters of the rendered page | `31.7368335355725_N_34.9889586223226_E`; distance to the CBS point 565.5 m (haversine), the same hex (-3, 24) | match for the hex; the distance is 565.5 m here against the row's 562 m (a 3 m difference, probably the distance formula). **Correction 27 Oct 2026, after the owner's review:** the haversine gives 565.5 m and the WGS84 geodesic 564.0 m; neither gives 562, so the formula is not the cause. The cause of the row's 562 is not known, and the hex is unaffected | [S] | 27 Oct 2026 |
| BS4 | December 1950, when the transit camp Ma'abarat Har-Tuv was set up (National Library) | https://www.nli.org.il/he/discover/israel/zionism/zionism-history/maabarot, read in the Internet Archive copy of 12 Oct 2026 (`web.archive.org/web/20261012142858id_/...`, HTTP 200, 69,788 bytes gzip-coded; the live page was not opened again) | "מעברת הרטוב הוקמה בדצמבר 1950 בהרי ירושלים, סמוך למקום בו הייתה המושבה הר-טוב שפונתה במלחמת העצמאות. ב-1952 החלו תושבי המעברה לעבור למבני קבע שהוקמו על הגבעה הצמודה, ביישוב שהלך והתפתח, ושנה לאחר מכן שונה שמו לבית שמש." The page spells it `הרטוב` (one word); the row's "Har-Tuv" is the English form | match for the month; no day. The page also says the ma'abara was renamed Beit Shemesh a year after 1952, i.e. 1953 | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |
| BS5 | "under the monthly turn the founding is the 15th, 15 December 1950" (owner, #785) | the row's `founded:` only | | not a source claim: the 15th is the monthly-turn rule of the owner's ruling, not a printed day | n/a | 27 Oct 2026 |
| BS6 | "The Ministry of Education sheet gives 1950, as a ma'abara" | https://meyda.education.gov.il/files/Pop/0files/geography_adam_sviva/70-shana/ari-pitoah.pdf (one page; text layer and page image both read) | "בית שמש: עיר במרכז הארץ בין תל אביב לירושלים. העיר הוקמה ב 1950 על אדמות הר טוב (מושב נחם היום) בתור מעברה לעולים מאיראן, מעיראק, מצפון אפריקה, ומרומניה. ... בשנת 1952 עברה המעברה למבני קבע ובשנת 1953 שונה שמה מ"מעברת הר־טוב" ל"בית שמש"." | match (year 1950, as a ma'abara, on the Har-Tov land, renamed in 1953; no month or day) | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |
| BS7 | "Wikipedia's day, 6 December 1950" (not used); first called Ma'abarat Har-Tuv | he.wikipedia `oldid=43925345`, History | "העיר הוקמה בכ"ז בכסלו ה'תשי"א (6 בדצמבר 1950). תחילה נקראה מעברת הר־טוב על שם המושבה הר-טוב, שהוקמה על ידי עולים מבולגריה על האדמות שעליהן יושב היום המושב נחם." Hebcal: 6 Dec 1950 = 27 Kislev 5711, as printed. The sentence has no footnote. "בשנת 1953 שונה שמה של "מעברת הר־טוב" לשם "בית שמש"." | match for what the row says. **Conflict with the row's chosen day, kept:** this is the only source of a day; the National Library prints only the month. The row's "(two copies of its wording are not independent)" was not re-checked here | [S]; [C] for the day | 27 Oct 2026 |
| BS8 | "[GAP] No source dates the first families separately" | the sources above | none of the sources read (National Library, Ministry sheet, Hebrew Wikipedia, CBS) and none of the JTA and Jerusalem Post searches prints a date for the first families | **not covered**, the row's own `[GAP]` stands | n/a | 27 Oct 2026 |
| BS9 | CBS: founding year 1950 | CBS file, row 292 | founding-year column `1950` | match | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |
| BS10 | "No source read places it on a village (settlements note, row 16)" | he.wikipedia `oldid=43925345`, section "העיר כיום"; the Ministry sheet | Hebrew Wikipedia: "במלחמת העצמאות, הצבא המצרי פלש לאזור והקים משלט באזור שבו היום נמצאת בית שמש הוותיקה, סמוך לדיר-אבאן. במשלט זה, שנקרא "המשלט המשותף", ישבו במשך מספר חודשים חיילי חטיבת הראל ... על משלט זה הוקמה העיר בית שמש. המשלט המשותף נכבש במהלך מבצע ההר, שהחל בליל ה־19/20 באוקטובר 1948." The same article says the ma'abara took its name from the moshava Har-Tov, built by Bulgarian immigrants on the land of today's Moshav Nahem and abandoned in the War of Independence. The Ministry sheet: "העיר הוקמה ב 1950 על אדמות הר טוב (מושב נחם היום)". | match for "no village" in the sense that no page says the town stands on an Arab village. Two neighbours the row does not mention: the abandoned moshava Har-Tov (not an Arab village) and, per Hebrew Wikipedia, the Egyptian outpost "the Joint Outpost" built near Deir Aban; whether Deir Aban touches the D-F1 flag is a question for the owner, not settled here. "On Har-Tov lands" is the Ministry sheet's wording only | [S] for the Wikipedia reading; [V, agent read 27 Oct 2026; a person's check pending] for the Ministry sheet's "Har Tov lands" | 27 Oct 2026 |

Not checked here (repo map data): "next to the Jordan line hexes `jordan_adl_a_latrun` and `jordan_adl_bc_south`".

## `beit_shemesh_field` (Beit Shemesh fields, שדות בית שמש): lines 209-219

| # | Claim in the row | Source | Printed text | Result | Tag | Day read |
|---|---|---|---|---|---|---|
| BF1 | Hex (-3, 24) equals the parent's | `content/sites.yaml` on `origin/main` `b02f82a0` | `beit_shemesh`: `q: -3`, `r: 24`; `beit_shemesh_field`: `q: -3`, `r: 24` | match | repo check, no outside source | 27 Oct 2026 |
| BF2 | Source is the parent's | the field's one source is the CBS file; it is the parent's fourth source (BS1, BS2, BS9) | the CBS row 292 prints the point and year used for the parent | match | [V, agent read 27 Oct 2026; a person's check pending] | 27 Oct 2026 |

## What a person has to open

1. **`kiryat_shmona`:** the municipality's "Main dates and events" chronology (https://www.k-8.co.il/he/faq/3/): read the line
   for 18.07.49 and its exact wording (the row says "the decision to make it a settlement named Kiryat Yosef"; the owner's ruling
   counts it as the second source for the arrival of the first families, which a decision to make a settlement is not obviously the
   same event as). Then the name page (the 21.7.49 line) and the two 8 and 21 July "18 families" lines the row mentions. The site
   answers scripts with a challenge page; a browser may get through. **Correction 27 Oct 2026, after the owner's review:** a review agent read the chronology and the name page on 27 Oct 2026 (a review agent's check (agent read 27 Oct 2026; a person's check pending)); a person still has to open them. Keep the decision/arrival point: 18.07.49 is a decision, and the families arrive on 08.07.49 and 21.07.49. `[C]` first name: the name page prints "חלסה", the chronology prints "קריית יוסף"; both kept.
2. **`kiryat_shmona`:** Goldstein (2016, *Journal of Israeli History* 35, pp. 17-37, doi 10.1080/13531042.2016.1140878) for the
   18 July 1949 day that both Wikipedias attribute to it, and Gelber (2004), p. 287, for 26 December 1949.
3. **`beit_shemesh`:** a source that dates the first families or the ma'abara's day; none was found. The National Library page
   (live, https://www.nli.org.il/he/discover/israel/zionism/zionism-history/maabarot, answers scripts with 403) should be read in a
   browser once, as the text here is from the Internet Archive copy of 12 Oct 2026.

## Sources used and how far each was reached

| Source | How it was read | Day |
|---|---|---|
| CBS localities file 2024, `bycode2024.xlsx` | HTTP 200; rows 292 and 1292 read from the sheet XML | 27 Oct 2026 |
| he.wikipedia `oldid=44043359` (Kiryat Shmona), `oldid=43925345` (Beit Shemesh); en.wikipedia `oldid=1376419614` (Kiryat Shmona) | HTTP 200, wikitext (`action=raw`) and rendered page (geohack parameters) | 27 Oct 2026 |
| Yad Ben-Zvi (israelalbum.org.il) Kiryat Shmona | HTTP 200, page text | 27 Oct 2026 |
| Ministry of Education development-towns sheet (PDF, one page) | HTTP 200; text layer and page image | 27 Oct 2026 |
| National Library "Ma'abarot", Internet Archive copy of 12 Oct 2026 | HTTP 200 (gzip), page text | 27 Oct 2026 |
| JTA archive (`wp-json`), Internet Archive *Jerusalem Post* Nov 1950 to Jan 1951 | HTTP 200, full text searched, nothing relevant (table above) | 27 Oct 2026 |
| k-8.co.il (two pages) | HTTP 403; Internet Archive: no capture or a loader page only; not reached. **Correction 27 Oct 2026, after the owner's review:** read by a review agent on 27 Oct 2026 (chronology 314,927 bytes, name page 131,500 bytes; a person's check pending). `[C]` first name: the name page prints "חלסה", the chronology prints "קריית יוסף"; both kept | 27 Oct 2026 |
| CBS yearbooks 1950 to 1955 | HTML error page instead of a PDF; not reached | 27 Oct 2026 |
| Gvati, https://benyehuda.org/read/14684 (the rural-settlement list) | HTTP 200; neither town is in it (it lists rural settlements); not covered | 27 Oct 2026 |
| Hebcal converter | HTTP 200, JSON (calculator) | 27 Oct 2026 |
| `content/sites.yaml`, `content/map/hexes.json`, `tools/hexgrid.py` on `origin/main` | read in the worktree | 27 Oct 2026 |

## AI assistance

Claude Code opened the sources and wrote this note. A second, independent Claude (Sonnet) agent re-checks the quotes (see the PR). Every
read tagged `[V, agent read 27 Oct 2026; a person's check pending]` is an agent's read; a person's check is pending, and this note
changes no `checked:` date, `status`, `reviewed_by` or attestation in `content/sites.yaml`.
