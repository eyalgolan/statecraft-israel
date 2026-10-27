# Yiftach's strength on 15 May 1948 and the end of its window: the sources side by side

Status: draft; AI-read; a person must open every cited page and check every line.

Researched 2026-10-26 by an AI agent for issue #1241 (the Yiftach part). It follows the note
`research_notes/GDD phase 5/later-brigades-1241.md` (#1258, sections 2, 5.5 and 7), which found the conflict and left it
open. This note changes no content file and no code. It does **not** settle the figure or the end day: under
`docs/CONTENT_POLICY.md` section 1 that is a person's choice, and section 7 lists the facts the person needs.

## Purpose

The row `yiftach` in `content/map/units_1948.yaml` (line 655) carries a `strength` of 4,500 with `as_of: '1948-05-15'`
and `status: to_verify`, and its only anchor is Lydda from 11 July 1948 (to 20 July). R-UNIT-006
(`docs/rules/mvp.md:215`) starts a row on 15 May only with a strength dated 15 May and an anchor window covering that
day; R-UNIT-009 (`docs/rules/mvp.md:221`) covers a strength dated after the start. A person has to pick the figure
(the row's 4,500, Dupuy's about 2,000, or the diary's possible 1,200) and the day Yiftach's first window ends. This note
puts each figure and each candidate end day next to what its source prints.

## Conventions

- **Tags.** `[V, agent read <day>; a person's check pending]` is a primary record opened by the agent, quoted from the
  page it read. `[S]` is a secondary source (Wikipedia, a heritage site, a history book read through a scan or a
  quotation); `[C]` is a conflict with both values kept; `[GAP]` is open, with what was tried. The agent has not
  settled any conflict.
- **Dates.** Every page was opened on 2026-10-26 unless a row says otherwise ("checked: 2026-10-26").
- **Units.** `people_per_unit` is 1,000 (`content/rules.yaml:41`) and R-UNIT-006 rounds half up, never below 1. The
  unit counts are arithmetic, not a proposal: 4,500 is 5 units, 2,000 is 2, 1,200 is 1.
- **Quotes** are exact; a quote from a wiki keeps its `[[...]]` link markup as the wikitext has it. Hebrew stays in Hebrew. Text pulled from a PDF scan came out with stray spaces between letters
  ("i n"); the quotes below have those removed and nothing else changed.
- **Not opened** means not opened. Such a source is named as a lead and carries no claim.

## 1. Sources read

| Short name | What it is | URL opened | How it was read |
|---|---|---|---|
| BG diary 15 May (live) | David Ben-Gurion's diary, 15 May 1948, the Ben-Gurion Archive's page with its English transcription, item 221910 | https://bengurionarchive.bgu.ac.il/en/node/8733 | The live page answered on 26 October (on 21 October, for #1258, it refused connections). Text read in full. The page's translation differs in wording from the Wayback copy (next row) |
| BG diary 15 May (Wayback) | The same page as the Wayback Machine saved it on 3 February 2024 | https://web.archive.org/web/20240203024717/https://bengurionarchive.bgu.ac.il/en/node/8733 | Re-read on 26 October; the quote #1258 gave is the Wayback wording |
| BG diary 1 June, 4 June, 6 June | The same diary, entries of 1, 4 and 6 June 1948 (items 15269, 15206, 15001) | https://bengurionarchive.bgu.ac.il/en/node/15269, https://bengurionarchive.bgu.ac.il/en/node/15206, https://bengurionarchive.bgu.ac.il/en/node/15001 | Live pages, English transcription. The Hebrew original was not seen: https://bengurionarchive.bgu.ac.il/he/node/8733 returned 404 |
| Dupuy | Trevor N. Dupuy, *Elusive Victory* (1978), the scan the repo already cites | https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf | `pdftotext` of PDF pages 3 to 5 (printed pages 43 to 45); the page images were not viewed |
| en.wiki 1948 war | "1948 Arab-Israeli War", revision 1375001749 (15 September 2026), the brigade table | https://en.wikipedia.org/wiki/1948_Arab%E2%80%93Israeli_War | Wikitext (`action=raw`) |
| en.wiki Palmach | "Palmach", revision 1381789687 (24 October 2026) | https://en.wikipedia.org/wiki/Palmach | Wikitext |
| en.wiki Yiftach | "Yiftach Brigade", revision 1379577121 (10 October 2026) | https://en.wikipedia.org/wiki/Yiftach_Brigade | Wikitext |
| he.wiki Yiftach | חטיבת יפתח, revision 44113003 (24 October 2026) | https://he.wikipedia.org/wiki/%D7%97%D7%98%D7%99%D7%91%D7%AA_%D7%99%D7%A4%D7%AA%D7%97 | Wikitext |
| he.wiki Yoram | מבצע יורם, revision 41706509 (23 August 2025) | https://he.wikipedia.org/wiki/%D7%9E%D7%91%D7%A6%D7%A2_%D7%99%D7%95%D7%A8%D7%9D | Wikitext |
| he.wiki Op. Yiftach | מבצע יפתח, revision 41422724 (9 July 2025) | https://he.wikipedia.org/wiki/%D7%9E%D7%91%D7%A6%D7%A2_%D7%99%D7%A4%D7%AA%D7%97 | Wikitext |
| Palmach site, itemId 6191 | palmach.org.il (Palmach Generation Association), "Yiftach Brigade (Brigade 11)" | https://palmach.org.il/history/database/?itemId=6191 | Page text, full |
| Palmach site, itemId 5523 | palmach.org.il, "Units and organisational structure" (the page the row cites for the Hebrew name) | https://www.palmach.org.il/history/database/?itemId=5523 | Page text, full |

**Opened but with no Yiftach strength on it.** The Palmach site pages above give no number of fighters for the brigade.
The he.wiki and en.wiki Yiftach pages print no 1948 strength either (the agent searched their wikitext for a count of
fighters and found none).

## 2. Candidate figures for 15 May 1948, side by side

Every figure below was found in a source the agent opened. A "dated" column says what date the source ties the
number to.

| # | Figure | Units | What the source counts | Dated | Source and exact printed text | Tag |
|---|---|---|---|---|---|---|
| A | 4,500 | 5 | "inc. some Golani": a brigade table's size column | Not dated by the table; the row sets `as_of` 15 May from the owner's ruling (#743) | en.wiki 1948 war, brigade table, row Yiftach: `4,500 inc. some Golani`. The size column's footnote: "Ben Gurion, David ''War Diaries, 1947-1949''. Arabic edition translated by Samir Jabbour. Institute of Palestine Studies, Beirut, 1994. p. 303." The cited page was **not opened** (no copy reachable). In the same column Golani is `4,500`, Carmeli `2,000`, Alexandroni `5,200`, Kiryati `1,400`, Givati `5,000`, Harel `1,400`, Negev `2,400`; none of these equals the diary's 15 May figures in B (Golani 3,573 + 522, Carmeli 2,238 or 2,289, Alexandroni 3,588, Kiryati 2,504, Givati 3,229) | [S] |
| B | 1,200 "in the north" | 1 | The Palmach's men "in the north", one of four lines under "Palmach"; the diary does not name Yiftach | 15 May 1948 (the diary entry) | BG diary 15 May (live): "Palmach approximately 6000 troops, 1300 of them in the “Harel” Brigade [in Jerusalem and the Jerusalem Corridor], 1300 in the “Negev” Brigade, 1200 in the north, 1000 girls". Wayback wording: "Palmach – about 6,000, of whom 1,300 are in Harel [a brigade stationed in Jerusalem and the Jerusalem Corridor], 1,300 in the Negev, 1,200 in the north, 1,000 women" | [V, agent read 2026-10-26; a person's check pending] for the figure; that it is Yiftach is the reading of #1258, not the source's words |
| C | about 2,000 | 2 | "men in each" of "three Palmach brigades" | "about" the war's start and the Israeli forces list that goes on to the 15 May figures; the passage is not dated by day | Dupuy printed p. 43, last lines: "There were three Palmach brigades, about 2,000 men in each. The" and p. 44, first line: "Yiftach Brigade was in eastern Galilee, facing Syria to the east, and Lebanon to the northwest." Two lines later the same page gives the Negev Brigade "about 1500 strong", so the "2,000 each" is a rounding by Dupuy, not a count of Yiftach. Dupuy's footnote 1 on p. 43 sends the reader to Luttwak and Horowitz, p. 34, "for an objective discussion of the inconsistencies among the sources"; that book was **not opened** | [S, agent read 2026-10-26; a person's check pending] |
| D | 1,800 (Yiftach and Harel together) | not a Yiftach figure | "fighters", combined for the two brigades, 6 June 1948 | 6 June 1948 (three weeks after the 15 May day; see section 3) | BG diary 6 June 1948 (item 15001): "Yigael estimates that there are about 23,000 fighters, including a garrison force of about 5,000. In the Upper Galilee (Tel Hai Subdistrict) there are 900-1,000, in the rest of the north[ern front] 3,000. In the center 1,300; in Tel Aviv 1,000. In Jerusalem 1,200. Givati has 1,500, in the Negev 700, Yiftah and Harel have 1,800, Shlomo’s brigade [the 7th Brigade] has 800." The same entry defines the count: "in a battalion of 700, there are 400 with rifles. 100 with auxiliary weapons (cannons [mortars] and [heavy] machine [gun]s). 200 [in support] services" | [V, agent read 2026-10-26; a person's check pending] |
| E | "over 2,000 men and women in three fighting brigades" (the whole Palmach) | not a Yiftach figure | The whole Palmach at the outbreak of the 1948 war | "By the outbreak of the 1948 Arab-Israeli War" | en.wiki Palmach, lead: "By the outbreak of the [[1948 Arab-Israeli War]], it consisted of over 2,000 men and women in three fighting brigades and auxiliary aerial, naval and intelligence units." Further down: "In May 1948 the Palmach had 2,200 permanently mobilised members" (citing Morris p. 22, not opened), and, citing Kimche and Kimche, *A Clash of Destinies* (1960), p. 77 (not opened), "five battalions were formed by May 1948, consisting of 5,000 fighters of whom 1,200 were women" | [S] |
| F | 6,360 (the whole Palmach, not Yiftach) | not a Yiftach figure | "The manpower situation, June 4, 1948": Palmach as one line among the brigades | 4 June 1948 (20 days after 15 May; context only) | BG diary entry of 6 June 1948 (item 15001), the line headed "The manpower situation, June 4, 1948": "Palmach 6,360, Golani 4,659, Carmeli 2,911, Alexandroni 4,261, Kiriati 2,238, Etsiyyoni 3,352, Brigade Zayin [7th Brigade] 2,050, Het [8th Brigade] – 1,091, engineers 622, air force 1,525, navy 1,045 – total 35,368, with the above 40,825". Read on the live page on 2026-10-27; a review agent's check (agent read 2026-10-26 on the archived copy https://web.archive.org/web/20240214092535/https://bengurionarchive.bgu.ac.il/en/node/15001; a person's check pending) found the same line. The Wayback URL returned an empty body from this box on 2026-10-27, so the agent did not re-read the archived copy | [V, agent read 2026-10-27; a person's check pending] |

### 2.1 What the diary's own lines add up to (arithmetic, ours)

- Palmach, 15 May: 1,300 (Harel) + 1,300 (Negev) + 1,200 (north) + 1,000 (women) = 4,800. The same sentence says "approximately
  6000", so 1,200 of the 6,000 sit in no named line. Whether the unnamed 1,200 belong to Yiftach, to the staff or to the
  women's line is not stated.
- Brigades, 15 May (live wording): 3,573 + 522 (Golani) + 2,238 (Carmeli) + 2,504 (Kiryati) + 3,588 (Alexandroni) +
  3,229 (Givati) + 3,166 (Etzioni) = 18,820, and with the Palmach's "approximately 6000" 24,820; the diary prints
  "approximately 24,400". The two bodies do not reconcile to the printed total; the arithmetic cannot say which Palmach
  number was in it. (With Carmeli at 2,289, the Wayback wording, the first sum is 18,871.)
- Added 2026-10-27, the 4 June line: the 6 June entry also prints "The manpower situation, June 4, 1948" with "Palmach 6,360" (row F). It is the only other dated Palmach total found on the four diary pages read (items 221910, 15269, 15206, 15001), and it is the 4 June line, not the 15 May block of section 2.1 above. Against the 15 May "approximately 6000" it is 360 higher after 20 days; against the four named 15 May Palmach lines (4,800) it is 1,560 higher. The diary does not say whether the 4 June total counts women or the Yiftach battalions apart from the other lines, so the arithmetic does not decide whether the "approximately 6000" or the unnamed 1,200 of 15 May is right. (The printed list also does not sum to its printed total: the eleven lines from Palmach to navy add to 30,114, against the printed 35,368; the printed 35,368 plus the 5,085 and the 372 above it is the printed 40,825. Not resolved here.)
- Two transcriptions of one page: the live page prints "Carmeli” 2238" and the Wayback copy of 3 February 2024 prints
  "Carmeli 2,289". Dupuy (p. 44, via Luttwak and Horowitz) prints Carmeli 4,095 and Golani 2,238, and he.wiki Carmeli
  has 2,238 (as #1258 recorded). This does not touch Yiftach but is the same diary line a person will open.

### 2.2 What the sources say Yiftach was made of on 15 May

| Fact | Source and exact printed text | Tag |
|---|---|---|
| Two battalions at first (the 1st and the 3rd); the 2nd joined "only in late 1948" | Palmach site 6191: "החטיבה כללה בראשיתה שני גדודים: הגדוד הראשון [...] והגדוד השלישי [...] הגדוד השני, שחנה בנגב בראשית, המלחמה, השתייך לחטיבת הנגב והצטרף ליפתח רק בשלהי 1948." he.wiki Yiftach: "בתחילת מלחמת השחרור כללה החטיבה שני גדודים" | [S] |
| Formation date: late April 1948 or end of May 1948 | Palmach site 6191: "חטיבת פלמ"ח שהוקמה בגליל בסוף אפריל 1948." en.wiki Yiftach: "It was formed in late April 1948 from existing Palmach battalions". he.wiki Yiftach, in its lead: "הוקמה כחטיבת [[פלמ"ח]] ב[[הגליל|גליל]] בעיצומה של [[מלחמת העצמאות]], בסוף מאי [[1948]]" and, in its history: "קמה מתוך [[מבצע יפתח]] שהתנהל בחודשים אפריל-מאי 1948" | [C] late April (Palmach site, en.wiki) against end of May (he.wiki lead); both kept. Added 2026-10-27: he.wiki Yiftach (rev 44113003), lead: "ערב [[הכרזת המדינה]] והקמת [[צה"ל]], עת התארגנו [[גדוד]]י הפלמ"ח וחילות [[ההגנה]] מחדש ב[[חטיבה|חטיבות]], אוחדו שני הגדודים הצפוניים לחטיבה שנשאה את שם המבצע שבו השתתפה". That puts the union of the two northern battalions into the brigade on the eve of the Declaration (14 May 1948), which sits beside "end of May" and does not say which day; the agent has not reconciled the two. Location corrected 2026-10-27: that "ערב הכרזת המדינה ..." sentence is not in the lead; on rev 44113003 it is in the history section (מלחמת העצמאות). The lead has the "בסוף מאי" sentence |
| The brigade was a task force before it was a brigade | Palmach site 6191: "תחילה הייתה כוח משימה, בפיקודו של מפקד הפלמ"ח יגאל אלון, מאוחר יותר גובש הכוח לחטיבה" | [S] |
| Units from outside the Palmach attached in the north in the same weeks | he.wiki Op. Yiftach: "במבצע השתתפו הגדוד הראשון והגדוד השלישי של הפלמ"ח וכן גדוד [[חי"ש]] 11 של [[חטיבת גולני]]" (the operation ran 28 April to 29 May 1948). The 11th Battalion was Golani's. The row's note says the table "counts some Golani men"; no source read says whether this battalion is those men | [S] |
| One Yiftach company on the Syrian front after 15 May, and the Yiftach 1st Battalion at Malkiya | en.wiki 1948 war: "defended by the Yiftach Brigade's 1st Battalion" at Malkiya (the wikitext has no link on "Yiftach Brigade" in that sentence; the other quote, "a company of the [[Yiftach Brigade]]", is linked); "a company of the [[Yiftach Brigade]]" reinforced the Syrian front (not an order of battle; no counts) | [S] |

## 3. The end day of the window: what each source prints

The row has no first anchor yet. Its only anchor is Lydda from 11 July 1948 (`to: '1948-07-20'`). A first anchor from 15 May
would end when the brigade left the Galilee, or at a later move. The sources give these days and places; none gives
one "end day".

| Candidate end | What it is | Source and exact printed text | Tag |
|---|---|---|---|
| 1 June 1948 | A Yiftach battalion cannot mobilise that day; its second battalion is to arrive "tomorrow" (2 June); the plan is to send both to take Latrun | BG diary 1 June 1948 (item 15269, Tuesday, June 1, 1948): "Turns out that the battalion under Yigal [Allon, of the Yiftah Brigade] won't be able to mobilize today." and "In light of Yigal P. [Allon]'s incapacity to mobilize today, Yigael S. [Yadin] proposes that [we] summon Yigal's second battalion (which will arrive tomorrow) and order the two battalions to conquer Latrun" | [V, agent read 2026-10-26; a person's check pending] |
| 4 to 5 June 1948 | The decision that Yiftach goes to Latrun, not south; the battalions set out for the Jerusalem road "tomorrow" (5 June) | BG diary 4 June 1948 (item 15206, Friday, June 4, 1948): "Yigael decided that Yiftah won’t go south, but rather to Latrun." and "The Yiftah battalions are heading to [the] Jerusalem [road] tomorrow to conquer Latrun." | [V, agent read 2026-10-26; a person's check pending] |
| "Early June" (no day) | The 1st and 3rd battalions brought down to the Sarafand camps and began preparing | he.wiki Yoram: "בתחילת יוני הורדו הגדוד הראשון והגדוד השלישי של חטיבת יפתח לאזור מחנות [[סרפנד]] והחלו בהכנות למבצע." (the footnotes name pages on irgon-haagana.co.il; the agent tried `http://www.irgon-haagana.co.il/info/hi_show.aspx?id=21937` and `id=21938` on 26 October and both returned 404) | [S] |
| June 1948 (no day) | The brigade moved to the centre | Palmach site 6191: "ביוני 1948 הועברה החטיבה למרכז הארץ, השתתפה במבצע יורם, בניסיון לכבוש את לטרון ומאוחר יותר במבצעי דני, גי"ס ויואב." he.wiki Yiftach: the same sentence | [S] |
| 6 June, then 8 to 9 June 1948 | Operation Yoram (the diary line is dated 6 June and describes a first move toward Al-Qubab; the attack on the positions is 8 to 9 June), the attack on the Latrun positions, by Yiftach's 3rd Battalion and a Harel battalion | he.wiki Yoram quotes Elyakim Sela's *Sefer HaPalmach* entry: "9.6.48 מבצע "יורם". יחידות "הראל" ו"יפתח" תוקפות את משלטי לטרון". The war note (`research_notes/M4 war mode/war-1948.md`, section 8) has the operation at "8 to 9 Jun". BG diary 6 June 1948: "The operation in Latrun began tonight. They apparently went to conquer Al-Qubab [in preparation for a third attack on Latrun – Operation Yoram]." | [V, agent read 2026-10-26; a person's check pending] for the 6 June line; [S] for Sela through he.wiki |
| 11 July 1948 | The row's own next anchor: Lydda, from 11 July (Operation Dani) | `content/map/units_1948.yaml`, `yiftach` anchors; the Palmach site's Operation Dani page is cited there (itemId 5632) and was not re-opened in this pass | row, not re-read |

What the sources leave open: where the brigade, or each battalion, stood between about 5 June and 11 July, other than
Latrun (Yoram, 8 to 9 June) and the Sarafand camps. The diary and he.wiki name no day the last Yiftach man left the
Galilee. Oded Brigade replaced it in the east Galilee (he.wiki Yoram: "(כדי להחליפה הוקמה בחפזון [[חטיבת עודד]])",
[S]); the Oded Brigade's own start was not looked up.

## 4. Which sources are primary, and which were not reachable

| Source | Result | Day |
|---|---|---|
| Ben-Gurion Archive, English diary pages (items 221910, 15269, 15206, 15001) | Opened; transcription is the archive's | 2026-10-26 |
| Ben-Gurion Archive, Hebrew page `/he/node/8733` | 404 | 2026-10-26 |
| *War Diary*, ed. Rivlin and Oren (Ministry of Defense, 1982) and the Arabic edition (Jabbour, 1994) p. 303 | Not opened; no copy reachable. The 4,500 table cites the second | 2026-10-26 |
| Lorch, *The Edge of the Sword* (archive.org items `edgeofsword00lorc`, `edgeofsword0000unse`) and Luttwak and Horowitz, *The Israeli Army* (`israeliarmy0000lutt`, `israeliarmy194810000lutt`) | Found on archive.org's search; the full text returned HTTP 403 (`edgeofsword00lorc`) or 401 (the other three), "Item not available" (lending library). Not read | 2026-10-26 |
| IDF map "מערך חטיבות צה"ל ב-15.5.1948" (read for #1258) | Not re-opened in this pass; it prints a place, not a strength | |
| Israeli archives (IDF Archive, State Archives, Maarachot) | Not tried again; the box is geo-blocked (recorded in #1766's note), not spoofed | |
| JTA | See the search record below | 2026-10-26 |

**Search record (JTA).** Run 2026-10-26 with the built-in `curl` against the JTA site's search API, no date filter:
`https://www.jta.org/wp-json/wp/v2/search?search=Palmach+brigade+Galilee&per_page=20` returned 12 items and
`https://www.jta.org/wp-json/wp/v2/search?search=Yiftach&per_page=20` returned 20. Their titles were read; the
archive items among them are not 1948 strength reports by title and **none was opened**, so this is no negative record
about the bulletins. A date-filtered query on `https://www.jta.org/wp-json/wp/v2/posts` (searches "Palmach brigade",
"Palmach strength", "Yiftach brigade", "Safad" and "Haganah", 1 May to 20 June or 30 June 1948) returned 0 items every
time, including for the common terms "Safad" and "Haganah", so that endpoint does not reach the 1948 bulletins and the
zeros are not evidence. No JTA bulletin with a Yiftach strength was found; none was looked for in the PDFs on
`pdfs.jta.org` by date. [GAP]

No Tavily key on this box: the web searches were the built-in WebSearch (two queries: "Yiftach Brigade Palmach strength
May 1948 ..." and "Palmach 1948 three brigades ... Lorch ... archive.org full text"). Their result snippets are not
sources and are not used here; only pages the agent then opened are cited.

## 5. Conflicts in one place

1. **Figure for 15 May:** 4,500 (en.wiki table, undated, an Arabic edition p. 303 of the diaries, not opened), about
   2,000 (Dupuy, a round "each" for three brigades), 1,200 (the diary's "in the north" line, not naming Yiftach).
   [C] The 4,500 does not match the diary of 15 May in any other row it shares a table with (section 2, row A).
2. **What "Yiftach" is on 15 May:** two battalions (Palmach site, he.wiki) of the brigade's later three; a task force
   rather than a brigade (Palmach site); formed late April (Palmach site, en.wiki) or end of May (he.wiki lead). [C]
3. **What a "strength" counts:** the diary's 6 June count is "fighters" and defines them as 400 with rifles in a 700-man
   battalion; the 15 May lines count "troops"/people. No source says the two are the same measure.
4. **The diary's two transcriptions** disagree on Carmeli (2,238 live; 2,289 Wayback) and are worded differently
   throughout; the Hebrew original was not reachable. [C]
5. **End of the window:** 1 June, 4 to 5 June, "early June", 8 to 9 June (Yoram) and 11 July are different events; no
   source prints "the brigade left Galilee on day X".

## 6. Not found

- A figure for Yiftach alone, dated 15 May, that names Yiftach. Looked in: the 15 May diary page, Dupuy pp. 43 to 45,
  en.wiki and he.wiki Yiftach, the Palmach site pages 6191 and 5523. **None on Yiftach** in these pages.
- A Haganah lexicon or Palmach-site page with a Yiftach order of battle by battalion on 15 May: the two Palmach-site
  pages above print none; `hahagana.org.il` lexicon page 47926 (Etzioni) was fetched and not read for Yiftach.
- JTA, Lorch, Luttwak and Horowitz, Morris, the Arabic and Hebrew editions of the diary, Gelber, *Yiftach: Ahuzat
  HaSufa* (Helman, ed., 1970, named on he.wiki Yoram) and Ofer and Ofer, *Yiftach* (2013, named on he.wiki Yiftach):
  not opened. [GAP]

## 7. What a person needs to decide (facts only, CONTENT_POLICY section 1)

Section 1 asks for one value from Israeli official or mainstream sources, and for Wikipedia to be named where it is the
source. The facts the choice depends on:

1. **Which figure.** The row's 4,500 is a table in en.wiki; its source page (Arabic edition p. 303) was not opened here.
   The only Israeli primary record opened that is dated 15 May is the diary's, and it prints a Palmach line "in the
   north" (1,200), not "Yiftach". Dupuy's 2,000 is "about", "in each" of three brigades, and he gives the Negev
   Brigade 1,500 a few lines later. In units: 4,500 is 5, 2,000 is 2, 1,200 is 1.
2. **Whether 1,200 is the brigade.** The Palmach's two northern battalions (1st, 3rd) were the brigade on 15 May
   (Palmach site, he.wiki); the diary's Palmach lines add to 4,800 of the stated 6,000, so 1,200 are in no named line.
3. **Whether the date matters for the measure.** The diary's 6 June entry (Yiftah and Harel together: 1,800
   "fighters", a rifle-and-support count) is the only other dated number in the diary for the brigade. It cannot be
   split between the two brigades without a figure for Harel on that day; the 15 May diary has Harel at 1,300.
4. **Formation date.** Late April (Palmach site, en.wiki) or end of May (he.wiki lead). A 15 May start needs the
   brigade to exist on 15 May; the two Palmach battalions in the north existed on that day either way, and the sources
   disagree on when they became "the Yiftach Brigade".
5. **The end day of the first anchor window.** The candidates in section 3 run from 1 June to 9 June, and the row's next
   anchor starts on 11 July. Whether the unit would stand at Safed or Rosh Pinna to the end of May and move on 1, 5 or
   8 June, or stand there until 10 July, is a choice the sources do not make.
6. **The capture check** noted on #1241 (the first anchor shares a hex with Lod and Ramla, `content/map/unit_anchor_hexes.yaml:15`):
   a Safed start avoids it; the choice of start place is a person's.
7. **Source check.** Open the live diary page of 15 May against the Hebrew original if it can be reached, and the two
   transcriptions' differences (section 2.1), before the figure goes into `content/`.

## Sources

Every URL in "Sources read" above, each opened on 2026-10-26 (checked: 2026-10-26), except the pages named in section 4
as not reachable or not opened.

## AI assistance

Researched and drafted by an AI agent (Claude Code). The diary entries, Dupuy's pages and the wiki and Palmach-site
pages were opened by the agent; no person has checked any line yet.
