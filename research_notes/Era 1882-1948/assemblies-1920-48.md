# The Assemblies of Representatives, the Vaad Leumi and the People's Council, 1920 to 1948

Researched 22 October 2026. Every URL below was opened on that date, except those keyed JTA-yymmdd in the source
table and the passages marked "(24 Oct)": a second pass on the [GAP] rows, opened on 24 October 2026 (the owner's comment
of 24 October 2026 on #89 lists them; agent read 2026-10-24; a person's check pending). Each JTA bulletin was opened as a web page on that day, and the dateline dates used below were read on the original bulletin PDFs. Research note for issue #89 (Yishuv 10, the
research part only, opened by the owner's ruling of 22 October 2026 on #49). Research only: nothing here is drafted
for the game, and no file in `content/` is changed. `legislatures.yaml`, `parties.yaml`, `governments.yaml` and the
coalition test stay paused.

## Purpose

Fix, from the best sources reachable, the facts #89 asks for: the four Assemblies of Representatives (Asefat
ha-Nivharim: election dates, first sessions, sizes, seats by list, turnout), the People's Council of April and May
1948, the Vaad Leumi each Assembly elected and its parties, how the Vaad Leumi and Jewish Agency budgets were
approved, the boycotts of 1944 and the postponements of 1919 and 1920. It also checks the figures in §2 and §5.2 of
[the Yishuv design](../../docs/superpowers/specs/2026-10-10-era-yishuv-design.md).

The heads of the Vaad Leumi (presidium, chairman, president) and its departments are in the offices note for #88,
`research_notes/Era 1882-1948/offices-1882-1948.md`, in [PR #1358](https://github.com/eyalgolan/statecraft-israel/pull/1358)
(not yet merged when this was written). They are not repeated here.

## Conventions

- [V] read this session in a primary source (a Mandate report to the League of Nations, a United Nations document,
  the text of the Jewish Community Regulations, a Knesset page) or in an authoritative secondary one (Encyclopaedia
  Judaica, the American Jewish Year Book, a dated Jewish Telegraphic Agency bulletin, Mordecai Naor's book). The source
  key is given.
- [S] single secondary source, usually Wikipedia, not confirmed in a [V] source. A lead, to be confirmed before
  encoding.
- [C] conflict. Every value is given with its source, and the preferred one is named.
- [GAP] not found in any source read. A gap stays a gap: no number, name or date is filled in by inference.
- Arithmetic done for this note (a sum of seats, a percentage) is marked "computed here". It is a check on a source,
  not a source.
- Dates are written 19 April 1920 style. Quotes from Hebrew sources are this note's translation.

## Sources

| Key | Source | Note |
|---|---|---|
| EJ-PL | Encyclopaedia Judaica, 2nd ed. (2007), "Land of Israel: Political Life and Parties" (Misha Louvish for the Mandate part), https://www.encyclopedia.com/religion/encyclopedias-almanacs-transcripts-and-maps/land-israel-political-life-and-parties | Prose on each election and Vaad Leumi, and the "Parties of Elected Assemblies" table. On the web page the table is flattened into one line of text, so only its unambiguous rows (dates, electors, percentage voting, lists represented, totals, footnotes) are used here |
| EJ-GOV | Encyclopaedia Judaica, "Land of Israel: Governance", section "Jewish communal organization (1880-1948)", https://www.encyclopedia.com/religion/encyclopedias-almanacs-transcripts-and-maps/land-israel-governance | Sessions, the Regulations, the Vaad Leumi's size and Executive |
| EJ-VL | Encyclopaedia Judaica, "Va'ad Le'ummi", https://www.encyclopedia.com/religion/encyclopedias-almanacs-transcripts-and-maps/vaad-leummi | 1944 boycott; recognition |
| EJ-JA | Encyclopaedia Judaica, "Jewish Agency", https://www.encyclopedia.com/religion/encyclopedias-almanacs-transcripts-and-maps/jewish-agency | The 1929 constitution |
| KN-AR | Knesset, Lexicon of Terms, "Assembly of Representatives (Assefat Hanivharim)", archive copy of 9 December 2003: https://web.archive.org/web/20031209034746/http://www.knesset.gov.il/lexicon/eng/asefat_niv_eng.htm | Members, lists and voters for each Assembly; budget powers; boycotts. Later copies are the Knesset's bot challenge |
| KN-PSC | Knesset, Lexicon of Terms, "The Provisional State Council", archive copy of 28 September 2012: https://web.archive.org/web/20120928204140/http://www.knesset.gov.il/lexicon/eng/moatza_eng.htm | Last meeting |
| MR-1930 | Report by His Majesty's Government to the Council of the League of Nations on the Administration of Palestine and Trans-Jordan for the year 1930, answer to Question 1 (the National Home), paragraphs 17 to 24; UNISPAL, archive copy of 15 March 2020: https://web.archive.org/web/20200315124608/https://unispal.un.org/UNISPAL.NSF/0/C2FEFF7B90A24815052565E6004E5630 | The 1931 election: date, lists, votes, seats, the Vaad Leumi it elected, and the parties of the previous (1925) Assembly |
| MR-1929 | The same report for the year 1929, paragraph 41, https://www.un.org/unispal/document/auto-insert-206576/ | The Regulations for the Jewish Community, 1927 |
| UN-NC84 | UN Special Committee on Palestine, A/AC.13/NC/84, 21 August 1947: letter of the General Council (Vaad Leumi), 18 July 1947, "relating to the strength of the parties", https://www.un.org/unispal/wp-content/uploads/2022/12/A.AC_.13.NC_.84_21081947.pdf | Votes for every list in August 1944. Read on the page image of page 2, because the PDF's text layer misreads several figures (see §5) |
| UN-JA11 | UN Palestine Commission, A/AC.21/JA/11, 10 February 1948: Moshe Shertok's letter of 6 February 1948 with a cable from the Vaad Leumi received 5 February 1948, https://www.un.org/unispal/document/auto-insert-211353/ | 1944 date, voters and seats per list, signed "Benzvi President Remez Chairman" |
| AJYB-46 | American Jewish Year Book 47 (1945-46), "Review of the Year 5705: Palestine", pp. 460-461, archive copy: https://web.archive.org/web/20160304200131/http://www.ajcarchives.org/AJC_DATA/Files/1945_1946_8_Palestine.pdf | 1944 election and first session |
| AJYB-21 | American Jewish Year Book 22 (1920-21), "Events in 5680: Palestine", pp. 236-238, archive copy: https://web.archive.org/web/20100613161502/http://www.ajcarchives.org/AJC_DATA/Files/1920_1921_4_YearReview.pdf | The postponements of 1919 and the call of March 1920 |
| JTA-301230 | Jewish Telegraphic Agency, "Ancient Sanhedrin Paralleled by Palestine Jewish Assembly", 30 December 1930, https://www.jta.org/archive/ancient-sanhedrin-paralleled-by-palestine-jewish-assembly | 1931 rules |
| JTA-440810 | JTA, "Final Results of Palestine Elections Announced; Laborite Groups Form Majority", 10 August 1944, https://www.jta.org/archive/final-results-of-palestine-elections-announced-laborite-groups-form-majority | 1944 results and boycotts |
| JTA-440913 | JTA, "Simple Ceremony Marks Opening Session of Newly-elected Palestine Jewish Assembly", 13 September 1944, https://www.jta.org/archive/simple-ceremony-marks-opening-session-of-newly-elected-palestine-jewish-assembly | 1944 first session |
| REG | Regulations of Knesset Israel (תקנות כנסת ישראל), Laws of Palestine vol. 3, ch. 126, p. 2221, with the amendments of 1934, 1937, 1938, 1943 and 1948, Hebrew text on Wikisource: https://he.wikisource.org/wiki/תקנות_כנסת_ישראל | Regulations 11 to 15. This is the consolidated text as amended up to 1948, not the 1928 Gazette text (see §6) |
| NAOR | Mordecai Naor (ed.), *Friday That Changed Destiny: 5th Iyar 5708 (May 14, 1948)*, English edition hosted by idekel.co.il, pp. 265-266 of the flipbook, which are printed pages 263-264; the People's Council passage on p. 266 (printed 264) is credited there to Pinhas Medding, "The Institutions of Government in the First Year of Independence" (Hebrew), in *Shana Rishona LaAtzma'ut* (Yitzhak Ben-Zvi Institute, 1988), not to Naor; p. 265 (printed 263) carries no credit for that part: https://idekel.co.il/Books/Shishi_Hagadol/files/basic-html/page265.html and https://idekel.co.il/Books/Shishi_Hagadol/files/basic-html/page266.html | The People's Council and People's Administration |
| CZA-30 | *30 Zionist Landmarks: Thirty Documents from the Central Zionist Archives* (Jerusalem, 1978), as reproduced by the Center for Online Judaic Studies: https://cojs.org/?p=6132 | The Zionist General Council's announcement of April 1948 |
| WP-EN | English Wikipedia, raw wikitext of "Assembly of Representatives (Mandatory Palestine)", "1920 Assembly of Representatives election", "1925 ...", "1931 ...", "1944 ...", "Provisional State Council" | [S], comparison only. The 1920 and 1925 articles cite Zalman Abramov, *Perpetual Dilemma* (1976), pp. 87-90 and 108-109, which was not read |
| WP-HE | Hebrew Wikipedia, raw wikitext of אספת הנבחרים, הבחירות לאספת הנבחרים הראשונה (and השנייה), הוועד הלאומי, מועצת העם | [S], comparison only |
| JTA-260113 | JTA, "National Assembly of Palestine Opens New Session", 13 January 1926, https://www.jta.org/archive/national-assembly-of-palestine-opens-new-session | 2nd Assembly first session (opened 24 Oct) |
| JTA-260114 | JTA, "Communist Speaker in Palestine National Assembly Hissed by Delegates", 14 January 1926, https://www.jta.org/archive/communist-speaker-in-palestine-national-assembly-hissed-by-delegates | 2nd Assembly, second sitting, presidium (opened 24 Oct) |
| JTA-260118 | JTA, "Palestine Jewry Asks for Land Grant, Communities Ordinance and Education Allotment", 18 January 1926, https://www.jta.org/archive/palestine-jewry-asks-for-land-grant-communities-ordinance-and-education-allotment | 2nd Assembly closing day (opened 24 Oct) |
| JTA-310105 | JTA, "Elections to Palestine Jewish Assembly Not to Be Postponed: Polling on Monday", 5 January 1931, https://www.jta.org/archive/elections-to-palestine-jewish-assembly-not-to-be-postponed-polling-on-monday-hope-revisionists-wil | 1931 polling day (opened 24 Oct) |
| JTA-310106 | JTA, "Palestine Jewish Assembly Elections: Bright Sunshine in Jerusalem for Polls", 6 January 1931, https://www.jta.org/archive/palestine-jewish-assembly-elections-bright-sunshine-in-jerusalem-for-polls-streets-crowded-with-el | 1931 polling day, ballot cards (opened 24 Oct) |
| JTA-310113 | JTA, "New Palestine Jewish Assembly to Meet February 4th. Vaad Leumi Decides", 13 January 1931, https://www.jta.org/archive/new-palestine-jewish-assembly-to-meet-february-4th-vaad-leumi-decides | 3rd Assembly planned first meeting (opened 24 Oct) |
| JTA-310211 | JTA, "New Palestine Jewish Assembly Holds First Meeting", 11 February 1931, https://www.jta.org/archive/new-palestine-jewish-assembly-holds-first-meeting-only-three-absentees-miss-henrietta-szold-mr-u | 3rd Assembly first meeting (opened 24 Oct) |
| JTA-310212 | JTA, "Arab-jewish Round Table Conference and Equal Arab and Jewish Participation in Palestine Government M...", 12 February 1931, https://www.jta.org/archive/arab-jewish-round-table-conference-and-equal-arab-and-jewish-participation-in-palestine-government-m | 3rd Assembly seat recount (opened 24 Oct) |
| JTA-310214 | JTA, "Trouble over Negotiations with Government at Palestine Jewish Assembly Meeting", 14 February 1931, https://www.jta.org/archive/trouble-over-negotiations-with-government-at-palestine-jewish-assembly-meeting-no-decision-taken-bu | 3rd Assembly: walk-out, Vaad Leumi of 23, tax scheme (opened 24 Oct) |
| JTA-320305 | JTA, "Unification of Palestine Jewry Effected at Closing Session of Jewish Elected Assembly", 5 March 1932, https://www.jta.org/archive/unification-of-palestine-jewry-effected-at-closing-session-of-jewish-elected-assembly-after-all-nigh | 1932 session, Vaad Leumi reshuffle, tax and budget (opened 24 Oct) |
| JTA-391019 | JTA, "Vaad Leumi Achieves Unity; Revisionists, Farmers Enter Executive After Two-day Talks", 19 October 1939, https://www.jta.org/archive/vaad-leumi-achieves-unity-revisionists-farmers-enter-executive-after-two-day-talks | 1939 executive by party (opened 24 Oct) |
| JTA-400926 | JTA, "Palestine Jewish Council Reaffirms Support of Britain; Adopts Large Budget", 26 September 1940, https://www.jta.org/archive/palestine-jewish-council-reaffirms-support-of-britain-adopts-large-budget | 1940 budget vote (opened 24 Oct) |
| JTA-420310 | JTA, "Jewish Constituent Assembly in Palestine Opens Session in Jerusalem", 10 March 1942, https://www.jta.org/archive/jewish-constituent-assembly-in-palestine-opens-session-in-jerusalem | 1942 budget on the agenda (opened 24 Oct) |
| JTA-430328 | JTA, "$7,400,000 Budget is Asked for Palestine Jews", 28 March 1943, https://www.jta.org/archive/7400000-budget-is-asked-for-palestine-jews-children-reported-starving | 1943 budget presented (opened 24 Oct) |
| JTA-430331 | JTA, "Adopt $680,000 Budget for Jewish National Council of Palestine", 31 March 1943, https://www.jta.org/archive/adopt-680000-budget-for-jewish-national-council-of-palestine | 1943 budget vote (opened 24 Oct) |
| JTA-440910 | JTA, "Palestine Jewish Assembly to Avoid Political Discussions at Opening Session", 10 September 1944, https://www.jta.org/archive/palestine-jewish-assembly-to-avoid-political-discussions-at-opening-session | 4th Assembly first session day (opened 24 Oct) |
| JTA-470525 | JTA, "Palestine Jewish Assembly Chooses New National Council with Labor Majority", 25 May 1947, https://www.jta.org/archive/palestine-jewish-assembly-chooses-new-national-council-with-labor-majority | 1947 Vaad Leumi by party (opened 24 Oct) |
| JTA-480309 | JTA, "Jewish Agency Names 13-member Cabinet; Forms Provisional Govt. Council for Jewish State", 9 March 1948, https://www.jta.org/archive/jewish-agency-names-13-member-cabinet-forms-provisional-govt-council-for-jewish-state | March 1948 plan, seats by party (opened 24 Oct) |
| JTA-480414 | JTA, "Decision to from a Jewish Cabinet Announced in Palestine; Will Function After May 1", 14 April 1948, https://www.jta.org/archive/decision-to-from-a-jewish-cabinet-announced-in-palestine-will-function-after-may-1 | 37-member decision (opened 24 Oct) |
| JTA-480506 | JTA, "Jewish State Council Meets in Tel Aviv; Ben Gurion Warns Arabs; Sees Jewish Victory", 6 May 1948, https://www.jta.org/archive/jewish-state-council-meets-in-tel-aviv-ben-gurion-warns-arabs-sees-jewish-victory | People's Council sitting (opened 24 Oct) |
| JTA-290814 | JTA, "Agency Council by Unanimous Vote Approves $3,750,000 Palestine Budget for First Year", 14 August 1929, https://www.jta.org/archive/agency-council-by-unanimous-vote-approves-3750000-palestine-budget-for-first-year | Jewish Agency budget 1929 (opened 24 Oct) |
| JTA-300903 | JTA, "Jewish Agency Likely to Cut Actions Committee's Budget to $2,000,000; Separate Budget of $750,000 Fo...", 3 September 1930, https://www.jta.org/archive/jewish-agency-likely-to-cut-actions-committees-budget-to-2000000-separate-budget-of-750000-fo | Jewish Agency budget 1930 (opened 24 Oct) |
| JTA-300910 | JTA, "Laborites to Back Agency in Spite of Disapproval of Budget, Non-representation on Executive", 10 September 1930, https://www.jta.org/archive/laborites-to-back-agency-in-spite-of-disapproval-of-budget-non-representation-on-executive | Jewish Agency budget 1930 (opened 24 Oct) |
| JTA-321207 | JTA, "Vaad Leumi Formally Takes over Education", 7 December 1932, https://www.jta.org/archive/vaad-leumi-formally-takes-over-education | Agency grant to the Vaad Leumi (opened 24 Oct) |
| JTA-351205 | JTA, "Vaad Leumi Given Wide Taxing Powers", 5 December 1935, https://www.jta.org/archive/vaad-leumi-given-wide-taxing-powers | Vaad Leumi tax powers (opened 24 Oct) |

(24 Oct) A JTA archive page is dated by its bulletin, and the dispatch inside it is datelined earlier: usually one day (the bulletin of 26 September 1940 opens "JERUSALEM, Sept. 25. (JTA)"), two days in the Sunday bulletins and in some 1931 London items (the bulletin of 11 February 1931 opens "Jerusalem, Feb. 9th."). The dateline is what counts. "Today", "yesterday" and "last night" count from the dateline, not from the bulletin date. Not reached: the National Library's historical newspapers (nli.org.il answers 403 to a script), so the *Doar
HaYom* report of 22 April 1920 that he.wiki cites for the 1920 postponement was not read; the Seventh Eye article
by Rafi Mann on the 1920 Assembly (read as an archive copy of 24 April 2019; it does not give the postponement);
Abramov's *Perpetual Dilemma*; Moshe Attias, *Knesset Israel be-Eretz Israel* (1944) and *Sefer ha-Te'udot shel
ha-Va'ad ha-Le'ummi* (1963), the standard document collections, which are not online in a readable form; the Mandate
reports for 1920 to 1928 (only 1929 and 1930 were read). (24 Oct) The Internet Archive's copy of the Interim Report on the Civil
Administration of Palestine, 1921 (https://archive.org/details/civil-administration-of-palestine) was found, but its download links
answered HTTP 500 on 24 Oct 2026 for the text and the PDF, so it was not read; the JTA archive starts in 1923, so there is no JTA
report of the 1920 vote.

## 1. The four Assemblies at a glance

| Assembly | Election | First session | Members | Lists with seats | Voters | Electors | Turnout |
|---|---|---|---|---|---|---|---|
| First | 19 April 1920 [V] EJ-PL; Jerusalem later [C], see §2 | October 1920 [V] EJ-PL; 7 October 1920 [S] WP-EN | 314 [V] KN-AR, EJ-PL | 20 [V] KN-AR, EJ-PL | 22,200 [V] KN-AR | 28,765 [V] EJ-PL | 77% [V] EJ-PL |
| Second | 6 December 1925 [V] EJ-PL | 11 January 1926 [V] JTA-260113, see §3 | 221 [V] KN-AR, EJ-PL, MR-1930 | 26 KN-AR; 25 EJ-PL [C] | 36,437 [V] KN-AR | 64,764 [V] EJ-PL | 57% [V] EJ-PL |
| Third | 5 January 1931 [V] MR-1930, EJ-PL, JTA-310105 | 9 February 1931 [V] JTA-310211, see §4 | 71 [V] KN-AR, MR-1930, EJ-PL | 12 MR-1930, KN-AR; 14 EJ-PL [C] | 50,436 [V] MR-1930, KN-AR | 75,046 voters' cards MR-1930; 89,656 EJ-PL [C] | 65% MR-1930; 56% EJ-PL [C] |
| Fourth | 1 August 1944 [V] EJ-PL, UN-JA11; 2 August AJYB-46 [C] | September 1944 [V] AJYB-46; 12 September [V] JTA-440910, see §5 | 171 [V] KN-AR, EJ-PL, UN-JA11 | 18 [V] KN-AR, EJ-PL | 202,448 [V] UN-NC84, KN-AR; others [C] | 300,018 [V] UN-NC84, UN-JA11 | 67% [V] EJ-PL |

KN-AR gives, for each Assembly in turn, its members, the lists represented and the voters, as in the table: "The
first had 314 members, representing 20 lists elected by 22,200 voters", and so on to "171 members, representing 18
lists and elected by 202,448 voters".

EJ-GOV on how often each Assembly met: "The first met three times and the second twice; the third held 18 sessions
and the fourth seven. It met for its last working session in October 1947, and the concluding session took place after
the establishment of the State of Israel, shortly before the first meeting of the Knesset in February 1949." [V]
WP-EN gives 13 February 1949 as the last day [S]. EJ-PL dates the first Assembly's sessions: the first in October
1920, the second "scheduled for May 1921, did not take place until the following March", the third in June 1925 [V].
The third Assembly "lasted for over 13 years, as elections were repeatedly postponed because of recurrent Arab
violence, continuous political tension, and then the outbreak of World War ii" (EJ-PL) [V].

Terms in law: the consolidated text of the Regulations, as amended up to 1948, sets a four-year term for the Assembly
(REG 12(1)), a one-year term for the Vaad Leumi (REG 14) and a session at least once in twelve months (REG 15(1)) [V].
Regulation 12 is marked as amended in 1937 and 1948, and 14 and 15 in 1948, so which of these terms were in the 1928
text is a [GAP]. In practice the third Assembly sat for over 13 years (below).

Against the design (§2): the sizes 314, 221, 71 and 171 are confirmed. The design's dates "from 6 Dec 1925", "from
5 Jan 1931" and "from Aug 1944" are election dates, not first sessions. (24 Oct) The first sessions are now found: 11 January 1926
for the second Assembly (§3) and 9 February 1931 for the third (§4). The design's 1944 values (2 August, 173 seats) are not preferred: see §5.

## 2. The first Assembly, 1920

### The postponements before the vote

- 27 July 1919: the Provisional Council (Va'ad Zemani) was "compelled to postpone elections to constituent assembly
  till October because Mizrahi refused to participate if women are permitted to be candidates" (AJYB-21, p. 236) [V].
- About 24 October 1919 (the day is marked with an asterisk in the source): "As result of conflict on equal franchise
  for women, the Provisional Council postpones, for an indefinite time, calling the National Assembly. The orthodox
  Jews and the Mizrahi persist in their refusal to participate in election if women are permitted to vote" (AJYB-21,
  p. 237) [V].
- 19 March 1920: the Va'ad Zemani "decides to call the Asefat Nibharim (Constituent Assembly) in Jerusalem, on Lag
  be-Omer (May 6)" (AJYB-21, p. 238) [V]. This is the date for the Assembly to meet, not the election day.
- WP-EN (citing Abramov, p. 90): the elections were fixed for 26 October 1919, postponed again because Weizmann feared
  that internal divisions would weaken the Jewish negotiating position, and finally set for 19 April 1920 [S].

### The 1920 postponement of the vote itself

[C], not settled from a primary source. he.wiki says the riots of 1920 (מאורעות תר"פ) postponed the vote in Jerusalem
and Jaffa by eight days, citing *Doar HaYom*, 22 April 1920, p. 14, which was not read [S]. WP-EN says voting was on
19 April 1920 "except in Jerusalem where voting took place on 3 May" (citing Tariq Ali (2022), p. 370), and that the
Orthodox "held separate elections in Jerusalem on 3 May, from which women were banned" (citing Abramov, p. 90) [S]. EJ-PL gives only 19 April 1920 for the
election and says the 51 Orthodox delegates were "chosen at separate polls by Orthodox men" [V], without a date for
those polls. Neither value is preferred. The day Jerusalem voted is a [GAP] until the 1920 press is read.

(24 Oct) Searched again and not found. Read: AJYB-21 pp. 236-238 again (its chronology has the call of 19 March and the 6 May
date for the Assembly, and nothing on the polling day or on the Orthodox polls); the JTA archive (no bulletins before 1923); the
Internet Archive's copy of the 1921 Interim Report (download failed, see "Not reached"); web searches whose only hits were the
same en.wiki and he.wiki articles already read. The Jerusalem and Orthodox poll days stay [C] between 3 May and "eight days
after 19 April" (27 April, computed here: 19 April plus 8 days), both [S], with no value preferred.

### Result

EJ-PL [V] (its prose says 19 lists stood, its table and KN-AR say 20 were represented [C, not settled]):
"nineteen lists of candidates were submitted. Each list received one delegate for every 80 votes polled;
77% of the electors voted and 314 delegates were elected." Ahdut ha-Avodah 70, Sephardi Union 54, Ha-Po'el ha-Za'ir
41, Farmers' Union 16, Progressive Party (Mitkaddemim) 13, Yemenites 12, two Mizrachi lists 11, "11 other groups with
a total of 46 places", and "an additional 51 delegates were chosen at separate polls by Orthodox men, who refused to
participate in elections in which women had the franchise." These add up to 314 (computed here). At the first session
"the 20 factions combined into three wings".

WP-EN and WP-HE list the small lists by name [S]. Their totals also reach 314 (computed here), but they give the
Orthodox ("Haredi Party") 53, not 51 [C] (WP-EN disagrees with itself: its prose says "another 51 by Orthodox voters", and the 53 is only in its infobox and table), and WP-EN's infobox gives Mizrahi Youth 3 where its own results table gives 2.
Preferred: EJ-PL for the lists it names; the names and seats of the 11 "other groups" are [S] only.

Turnout: EJ-PL gives 28,765 electors and 77% [V]; KN-AR gives 22,200 voters [V]. 77% of 28,765 is about 22,150
(computed here), so the two agree. WP-EN, citing Abramov, gives "around 22,000 of the 26,000 registered voters" [S].

### Recognition

EJ-VL: the Vaad Leumi was "elected in 1920 by the first Asefat ha-Nivharim and recognized immediately as a
representative body in a letter from the high commissioner, Sir Herbert Samuel", but "achieved formal legal status only
on Jan. 1, 1928" [V]. he.wiki says the British opposed convening the Assembly until Samuel approved it in October 1920
[S]. The Vaad Leumi "functioned from Oct. 10, 1920" (EJ-VL) [V]; the offices note for #88 records the 7 or 10 October
question.

## 3. The second Assembly, 1925

Election 6 December 1925 (EJ-PL) [V]. Electors 64,764 and 57% voting (EJ-PL) [V]; 36,437 voters (KN-AR) [V]. WP-EN
gives 35,987 votes cast, citing Mackie and Rose, *The International Almanac of Electoral History* (1991), p. 243 [S]
[C]: the gap between 36,437 and 35,987 is not explained by any source read.

Size: 221 (KN-AR, EJ-PL table, MR-1930 para. 22) [V]. EJ-PL's prose says the Revisionists gained "15 seats out of
201" and that "The Yemenites, alleging discrimination, boycotted the elections, but were later permitted to elect 20
additional delegates of their own"; its table notes "Including 20 Yemenites elected at separate polls". So 201 were
elected on 6 December and 20 later (EJ-PL) [V]; the day of the Yemenite poll is a [GAP]. Agudat Israel kept its
boycott (EJ-PL) [V].

(24 Oct) First session [V]: JTA-260113, a bulletin dated Wednesday 13 January 1926: "The first session of the newly elected
Assefath Ha'nivcharim, the National Assembly of Palestine Jews, was opened here Monday night in the Theatre Zion in the presence
of 201 deputies". Monday before its dateline of 12 January is 11 January 1926 (computed here). The 201 present match the 201 elected on
6 December and not the 221 with the Yemenite 20 (EJ-PL, above); that is a reading, not a statement of the bulletin. JTA-260114
(dateline 13 January) reports the second sitting "yesterday", that is on 12 January, and that "One Yemenite Jew was also elected to the praesidium", so the Yemenites were in
the Assembly by 13 January; the day of their separate poll is still a [GAP]. JTA-260118: the Assembly "sat in session four
days" and closed on Friday morning (15 January 1926, computed here; dateline 16 January) after an all-night sitting.
JTA-260113 also gives the turnout as "Thirty-three thousand eight hundred and forty-five persons, representing 52.3 per cent of the
total number of electors", a third value besides 36,437 (KN-AR) and 35,987 (WP-EN) [C]; 33,845 is 52.3% of about 64,700 (computed
here), close to EJ-PL's 64,764 electors, but EJ-PL gives 57% voting. Not settled. The same bulletin says the two labour parties
"together secured 84 seats" and the Revisionists "hold 15", matching 54 + 30 and 15 in the table below [V].

Seats by list. No source read gives the full list with a primary record. Two partial lists:

| List | MR-1930 para. 22 ("principal parties in the previous Elected Assembly") [V] | EJ-PL prose [V] |
|---|---|---|
| Ahdut ha-Avodah | 54 | 54 |
| Ha-Po'el ha-Za'ir | 30 | 30 |
| Sephardim | 19 | 19 ("Sephardim and Oriental groups") |
| Revisionists | 15 | 15 |
| Women | 9 ("Women's Association") | 13 ("Women's Equal Rights Association") [C] |
| Democrats | 8 | not given |
| Mizrachi | 7 | 19 ("five Mizrachi lists together") |
| Proletariat / "working-class" list | 6 | 6 |
| Torah Veavoda (Orthodox Labour) | 6 | not given separately |
| Gush Merkazi (Centre Party) | 6 | not given |
| Ezrahim Leumiyim (Palestine National Party) | 6 | not given |
| Agricultural Bloc | not given | 9 |

MR-1930 lists 166 of the 221 seats (computed here). WP-EN's results table names 25 lists plus "Others 20" [S], with
Women's Association 13, Agricultural Bloc 9 and Democrats 9; its own infobox gives Agricultural Bloc 13 and Democrats
12 [C]. Preferred: MR-1930 where it gives a figure, as the Mandatory's contemporary report, but the women's seats are
[C] (9 or 13) and the rest of the 221 is [GAP] in any [V] source. Lists represented: 26 (KN-AR) or 25 (EJ-PL) [C].

## 4. The third Assembly, 1931

The rules (MR-1930 paras. 18 to 20; JTA-301230; EJ-PL) [V]: held on 5 January 1931 under an electoral regulation
(our reading: made under the 1927 Regulations; MR-1930 para. 18 says only "under the Electoral Regulation of which the text is
given in Appendix I to this Report"); 71 seats; voters 20 or over on the community register; candidates 25 or over with
Hebrew (JTA-301230). Electors were split by community: "Ashkenazim (Western Jews) could only vote for an Ashkenazi list,
Sephardim (Eastern Jews) for a Sephardi list and Yemenites for a Yemenite list" (MR-1930 para. 20); EJ-PL and EJ-GOV
give the seats per college as 53 Ashkenazi, 15 Sephardi and 3 Yemenite. Agudat Israel called a boycott, with "little
response" (MR-1930 para. 19). "The Farmers refused to participate in the elections when their demand for three
guaranteed seats was refused" (EJ-PL); MR-1930 shows the Farmers' Association list with no votes.

Turnout: "75,046 voters' cards were delivered and 50,436 votes (representing 65 per cent. of the number entitled to
vote) were cast" (MR-1930 para. 20) [V]. EJ-PL's table gives 89,656 electors and 56% [C]. Preferred: MR-1930, the
contemporary official count; KN-AR's 50,436 voters agrees with it.

Seats and votes (MR-1930 paras. 20 and 21) [V]. Eighteen lists stood; twelve won seats:

| List (MR-1930's name) | Votes | Seats |
|---|---|---|
| Labour (Mapai, a label added here from EJ-PL) | 21,497 | 27 |
| Revisionists | 8,069 | 10 |
| Sephardic "Bloc" | 2,301 | 6 |
| Mizrahi and Hapoel Hamizrahi | 4,107 | 5 |
| Sephardic Revisionists | 2,121 | 5 |
| General Zionists | 2,841 | 5 |
| Sephardic Workers | 1,653 | 4 |
| Women's Association | 1,861 | 3 |
| Yemenites | 1,515 | 3 |
| Poale Zion (Left Wing Labour) | 902 | 1 |
| Boruchov Workers | 891 | 1 |
| Hashomer Hatzair | 812 | 1 |
| Proletarian List (Communists) | 524 | 0 |
| Merkaz Baale Melakha (Artisans Group) | 352 | 0 |
| Sephardic Poale Zion | 28 | 0 |
| Sephardic Boruchov Workers | 1 | 0 |
| Farmers' Association | none shown | 0 |
| Middle Class Association | none shown | 0 |

The seats add up to 71 (computed here). The list votes add up to 49,475 (computed here), 961 fewer than the 50,436
votes cast; MR-1930 shows no figure ("--") for the Farmers' and Middle Class lists and gives no count of invalid votes,
so the 961 are not explained. [C] EJ-PL: Revisionists "16, including 5 Sephardim" (MR-1930: 10 + 5 = 15)
and General Zionists 4 (MR-1930: 5). he.wiki אספת הנבחרים gives the Revisionists 11 and the General Zionists 4 [S]; he.wiki הבחירות לאספת הנבחרים השלישית
gives the Revisionists 10 and the General Zionists 5 in its summary table but 4 in its results table [S]. Preferred:
MR-1930. Lists represented: 12 (MR-1930, KN-AR) or 14 (EJ-PL) [C]; preferred 12.

(24 Oct) What the JTA bulletins add for 1931 [V as dated bulletins]:

- Polling day: JTA-310105 (Monday 5 January 1931 bulletin, dateline 4 January) says the elections "will take place on Monday", and the Vaad Leumi
had declined to postpone them; JTA-310106 (bulletin of 6 January, dateline 5 January) reports polling "to-day". 5 January 1931 was a Monday (computed
here), so the poll was 5 January. JTA-310106: 85,000 ballot cards distributed,
about 10,000 entitled voters not traced, 114 constituencies. The 85,000 cards differ from MR-1930's 75,046 cards delivered [C]; not settled.
- First meeting: the Vaad Leumi fixed 4 February for it (JTA-310113, bulletin of 13 January, dispatch datelined 11 January: "held last night", so the night of 10 January), but the first meeting
was held at the Straus Health Centre on the afternoon of 9 February 1931: JTA-310211 is the bulletin of 11 February, and its dispatch is
datelined "Jerusalem, Feb. 9th." and says "first meeting held this afternoon" (read on the original bulletin). Ben-Gurion
took the chair; the presidium had six members, among them a Yemenite rabbi (the bulletin names all six);
three of the 71 were abroad (the bulletin names Szold, Ussishkin and Jabotinsky); the Revisionists refused places in the presidium.
- Seats: JTA-310211 gives Labour 32 of 71 and the Revisionists 15. JTA-310212 (bulletin of 12 February, dispatch datelined 10 February, so the recount was on 10 February) says the Revisionists
"to-day incidentally increased its representation from 15 to 16 seats" by successfully contesting a fifth General Zionist seat
"claiming in their favour a balance of 178 votes"; the General Zionists were left with four. The bulletin states only the move from 15 to 16 and from 5 to 4. The following is a reading, not a statement of the bulletin: this reconciles the [C] above, MR-1930's
10 + 5 = 15 Revisionists and 5 General Zionists are the first count, and EJ-PL's 16 and 4 are the count after the recount [V].
Preferred for the final seats: Revisionists 16, General Zionists 4. "Labour 32" (JTA) against Mapai 27 (MR-1930's "Labour") is
not reconciled: JTA's Labour may include the smaller labour lists, but no source read says so [C].
- The Vaad Leumi: JTA-310214 (bulletin of 14 February): after the Revisionists walked out on a 37 to 29 roll-call, a new Vaad
Leumi of 23 was elected, "eleven Labourites, and twelve others, four Sephardim, three Mizrachists, three General Zionists, one
Yemenite, and one woman representative", with five seats "reserved" for the Revisionists; the same session approved "the
proposals for the taxation scheme, the organisation of the Jewish Community and the Rabbinical Office" [V], which matches
EJ-PL and MR-1930 in §7.

## 5. The fourth Assembly, 1944, and the boycotts

### Date

[C] 1 August 1944: EJ-PL ("The fourth Asefat ha-Nivharim was elected on August 1, 1944") and UN-JA11 (the Vaad
Leumi's own cable of February 1948: "4th ASSEFAT NIVHARIM 1st AUGUST 1944"); he.wiki אספת הנבחרים also gives 1 August [S].
2 August 1944: he.wiki הבחירות לאספת הנבחרים הרביעית [S]; AJYB-46 p. 460 ("On August 2, 1944, general elections to the Assefath Hanivcharim ... took place") and
en.wiki [S]. Preferred: 1 August, the date the Vaad Leumi itself gave. UN-NC84 says only "August 1944".

### Who boycotted

All [V], and they agree except on the farmers and the size of the General Zionist group:

- EJ-PL: "The Revisionists, General Zionists b, and Sephardim boycotted the elections because their demands for
  changes in the electoral system were refused, while Agudat Israel maintained its ban."
- EJ-VL: "from 1944 the Sephardi list, the Revisionists, the General Zionists, and the Farmers Union were not
  represented, because of their boycott of the elections to the fourth Asefat ha-Nivharim when their demands for a
  reform of the electoral system were rejected." Agudat Israel and the ultra-Orthodox of the old yishuv boycotted the
  Vaad Leumi throughout.
- JTA-440810: "The Zionist-Revisionist Party, the Sephardic Jews, the General Zionist Group 'B' and the Jewish farmers
  Association will not be represented ... since they boycotted the elections."
- AJYB-46 p. 460: "Several Rightist groups (Sephardim, Revisionists, Landowners, and a General Zionists fraction)
  boycotted the elections", calling the proportional system "super-democratic".
- UN-NC84: "Certain Right-Wing groups abstained from voting."
- KN-AR: "After 1944, the Revisionists and several other political groups also boycotted it due to their opposition to
  the official policy of the elected leadership."
- JTA-440913: at the opening session Ben-Zvi "regretted the fact that some groups, especially the Sephardic Jews,
  boycotted the elections".
- AJYB-46 p. 461: the opposition later offered to take part in new elections "in a year or two under a slightly
  modified electoral system"; talks on their temporary entry into the Vaad Leumi "on the basis of their representation
  in the previous Assefath Hanivcharim" had "not yet advanced sufficiently". Whether they joined is a [GAP]. A
  lead, not proof: in February 1948 Shertok wrote that Agudath Israel was "the only Jewish party or group" outside the
  framework of both the Jewish Agency and the Vaad Leumi (UN-JA11) [V]. The same letter lists the Revisionists among
  the parties in the 1946 Congress elections, and being inside the Agency's framework is not the same as sitting in
  the Vaad Leumi.

EJ-VL's "the General Zionists" is wider than the other sources' "General Zionists b" or "a General Zionists fraction":
General Zionist lists did stand and win seats (below). Preferred: General Zionists B only.

### Voters

| Value | Source |
|---|---|
| 199,867 valid votes, 2,581 "disqualified and blank", total 202,448 "out of 300,018 electors" | UN-NC84 page 2 [V] |
| 202,448 voters | KN-AR [V] |
| "202404 VOTERS PARTICIPATED OUT OF 300018 ENTITLED TO VOTE", plus "9219 ENLISTED MEN AND WOMEN WHO VOTED IN THEIR UNITS IN PALESTINE AND ABROAD"; 274 settlements took part | UN-JA11 [V] |
| 200,881 voters, "more than seventy percent of all those eligible" | JTA-440810 [V] |
| "a total of 200,881 among 280,000 eligible voters"; 24 parties and groups, 1,694 candidates | AJYB-46 p. 460 [V] |
| 67% of "the vastly increased electorate of 300,000" | EJ-PL [V] |

[C] Preferred: 202,448 of 300,018, the figure in the Vaad Leumi's letter to UNSCOP, whose list totals add up (below),
and in the Knesset lexicon (which may have taken it from the same record). The Vaad Leumi's own cable of February 1948
(UN-JA11) gives 202,404, so the Vaad Leumi itself gave two values. Whether the 9,219 soldiers' votes are inside that figure is a
[GAP]. The 1944 percentage is 67.5% on the preferred figures (computed here), matching EJ-PL's 67%.

### Votes by list (UN-NC84 page 2, the Vaad Leumi's table) [V]

The PDF's text layer misreads several figures (for example 18027 for 18827 and 33328 for 38328); the values here are
read from the page image, and every group subtotal and the grand total add up (computed here).

| Group | List (UN-NC84's name) | Votes |
|---|---|---|
| Labour | Jewish Labour Party in Palestine (Mapai), and non-party members | 73,667 |
| Labour | Left-Wing Front | 24,764 |
| Labour | United Workers Movement (Ahdut ha-Avodah) | 18,168 |
| Labour | Popular Democratic List (Communists) | 3,948 |
| Labour | People's Movement for Jewish State | 1,407 |
| Labour | National Workers Front | 119 |
| General Zionists | Democratic Bloc | 4,729 |
| General Zionists | Zionist Workers | 3,687 |
| Religious | Mizrachi and Orthodox, Mizrachi Women and non-party orthodox women | 7,752 |
| Religious | Torah VeAvoda, Hapoel Hamizrachi, Women's Torah VeAvoda | 18,827 |
| Religious | Non-party Orthodox List | 3,128 |
| Religious | Oriental Communities, Torah VeAvoda | 542 |
| Religious | Orthodox Workers | 801 |
| Others | Yemenite Group | 959 |
| Others | Independent Yemenite Group | 5,053 |
| Others | Women's International Zionist Organization (W.I.Z.O.) | 4,338 |
| Others | New Settlers Movement (Aliyah Hadashah) | 21,403 |
| Others | Palestine Jewish Merchants | 1,673 |
| Others | "Maccabi" National Unity Movement | 3,723 |
| Others | United Community Group | 241 |
| Others | Ex-Servicemen and their Families | 674 |
| Others | Aden Youth Group | 264 |

Subtotals: Labour 122,073; General Zionists 8,416; Religious 31,050; Others 38,328; total 199,867. The names in
brackets are identifications made here from the vote counts AJYB-46 gives (Mapai 73,367, Ahdut ha-Avodah 18,168,
Aliyah Hadashah 21,403) and from UN-JA11's "Communists Popular Democracy". AJYB-46 gives Mapai 73,367 where UN-NC84
gives 73,667, the Left Bloc 24,773 (UN-NC84 24,764) and Hapoel Hamizrachi 19,346 (UN-NC84 18,827) [C]. Preferred:
UN-NC84, whose totals add up.

### Seats by list

No source read gives a seat for every list that adds up to 171:

| List | UN-JA11 (Vaad Leumi cable, Feb. 1948) | JTA-440810 | AJYB-46 p. 460 |
|---|---|---|---|
| Mapai | 63 | 64 | 63 |
| Hashomer Hatzair and Left Poale Zion (Left Front) | 37, together with Ahdut ha-Avoda | 21 | 21 |
| Ahdut ha-Avodah | in the 37 above | 16 | 16 |
| Aliyah Hadashah | 16 | 19 | 18 |
| Hapoel Hamizrahi (with Poel Mizrahi Women) | 17 | 17 | 17 |
| Mizrahi, Orthodox and Mizrahi Women | 7 | 7 ("Mizrachi") | 7 ("Mizrachi") |
| General Zionists (and Zionist Worker) | 7 | 7 ("group A") | 14, "divided into four different electoral groups" |
| Yemenites | 6 | not given | 6 |
| Merchants and Independent Orthodox | 5 | not given | not given |
| WIZO | 4 | 4 | not given |
| Communists (Popular Democracy) | 3 | 3 | 3 |
| Maccabi | 3 | 3 | not given |
| Popular Movement for Jewish State (Lubotzky) | 1 | not given | not given |
| Sum of the figures given | 169 (computed here), against the cable's own "171 members" | 161 (computed here), "the remainder of seats are scattered between smaller groups" | the "big five" 135 plus "ten smaller groups received 36 mandates" = 171 (computed here) |

[C] Preferred for the total: 171 (KN-AR, EJ-PL, UN-JA11, and AJYB-46's own arithmetic). For single lists: Mapai 63
(UN-JA11, AJYB-46), Left Front 21, Ahdut ha-Avodah 16 and Communists 3 (EJ-PL
footnote, adapted: the page's dashes and diacritics are removed per repo policy: "Ha-Shomer ha-Za'ir 21; Le-Ahdut ha-Avodah 16; Popular Democrats (Communists) 3"), Hapoel Hamizrahi 17 and
Mizrahi 7 (all three sources). Aliyah Hadashah is 16, 18 or 19 [C]; EJ-PL's table footnote reads "Including 18 Aliyah"
[V], which with AJYB-46 makes 18 the better-supported value. The seats of the other small lists are [GAP]: the cable's
list misses 2 seats. en.wiki's results table, from the *Palestine Post* of 9 August 1944 (not read), gives Mapai 64,
Aliyah Hadashah 19 and 173 seats in all [S] [C]; not preferred.

### First session

AJYB-46 p. 460: "In September 1944, the Assefath Hanivcharim held its first session", which "closed with the election of
a permanent body, the Vaad Leumi (National Council) of 42 members, with all factions represented" [V]. JTA-440913 is a
bulletin dated 13 September 1944 describing "today's opening session" [V]; the KN-AR page shows a photograph captioned
"The Assembly of Representatives, 12.9.1944" [V]. (24 Oct) 12 September 1944 [V]: JTA-440910, a bulletin dated Sunday 10 September 1944 (dispatch datelined 8 September), says the first session "will open here
next Tuesday", and 12 September 1944 was a Tuesday (computed here). That agrees with the Knesset photograph caption 12.9.1944
and with JTA-440913, whose dispatch is datelined 12 September (bulletin of 13 September) and says "today's opening session". 13 September is not the day. The same JTA-440910 bulletin says political issues would wait for "the second session which
will take place after the High Holidays".

## 6. Budget approval

### The Vaad Leumi's budget

KN-AR [V]: "The Assembly held one annual session, at which the Va'ad Le'umi ... was elected. ... The Assembly of
Representatives was authorized to take decisions and to vote on the budgets of the National Council and the Rabbinical
Council." EJ-GOV [V]: the Assembly "was convened infrequently, for sessions lasting from one to four days, to deal with
internal and political issues, organizational matters, and the approval of budgets."

Legal basis. EJ-GOV [V]: on 1 January 1928 the Official Gazette published the Regulations of the Jewish Community,
whose "central organs were granted judicial powers and the right to levy taxes"; membership was automatic for Jews
after three months' residence, and anyone could have his name struck off once a year. MR-1929 para. 41 [V] calls them
"the Regulations for the Jewish Community, 1927" and the Vaad Leumi "the body acting in the capacity of the
constitutional General Council". The text, REG [V]:

- Regulation 13(1)(a): every year the Assembly elects a Vaad Leumi from among its members and may pass resolutions
  that bind it; the Vaad Leumi stays in office after its term until new elections. 13(1)(b): the Vaad Leumi appoints
  an executive committee (ועד פועל) from among its members, responsible to it.
- Regulation 13(2): "After the Assembly of Representatives has examined the estimate of expenditure and revenue
  submitted by the Vaad Leumi, it shall approve for the Vaad Leumi an annual budget" sufficient for its functions,
  including the cost of elections and of convening the Assembly, and any sum needed to cover a deficit of the
  Rabbinical Council.
- Regulation 13(3): the Assembly may require a local community (קהלה) to levy taxes, or authorise it to, for
  education, poor relief, care of the sick, orphans, and contributions to the rabbinical offices, the Vaad Leumi and
  the community, "according to their budgets"; and taxes on ritual slaughter, matzah licences and certificates.
- Regulation 13(4): the Assembly fixes the method of collection and the maximum of each tax "in an order submitted
  every year" for the authority's approval.
- Regulation 13(6): "The budget adopted by the Assembly of Representatives shall be submitted ... for approval and
  shall not take effect until approved"; each local community's budget goes to the District Commissioner.
- Regulation 11(1): the Rabbinical Council's budget is fixed jointly by the Vaad Leumi and the Rabbinical Council;
  11(4): the Assembly may levy a special tax to cover a deficit in its special account.

Caveat on REG: the Wikisource text is the consolidated version as amended in 1934, 1937, 1938, 1943 and 1948. Where
it says "Minister of Religions" (שר הדתות), its editorial note says the High Commissioner's powers under the law passed
to the government in 1948 and were assigned to the Minister of Religions (Official Gazette 5708, p. 22). So under the
Mandate the budget in 13(6) went to the High Commissioner for approval; the 1928 Gazette wording itself was not read
[GAP], and which clauses the 1934 amendment to regulation 13 changed is a [GAP].

Finance, from he.wiki הוועד הלאומי [S]: the finance committee set up by the Assembly set the Vaad Leumi's budget, with
the Assembly's approval (the page gives no date for this). Before its constitution was approved the Vaad Leumi had two
levies: the first, the "Sela ha-Yishuv", was voluntary; the second was an assessment tax ("מס הערכה") and was
compulsory. The page dates the constitution's approval to 1925. EJ-PL [V]: the second session of the first Assembly was delayed partly by "the Sephardim's and Farmers'
objections to the proposed self-taxation system".

How each year's budget was actually voted (dates, amounts, tallies): partly closed (24 Oct). The Assembly's own minutes were
not read, so no tally and no complete yearly series. Dated JTA bulletins [V] show the practice:

- 1931 and 1932: the 1931 session (bulletin of 14 February 1931) approved "the proposals for the taxation scheme" (JTA-310214);
  the closing bulletin of the 1932 session reports the Vaad Leumi leaders telling JTA that it "decided on the taxation system and the budget" (JTA-320305). No amount.
- December 1932: the Jewish Agency was to "contribute the sum of £40,000 annually to the Vaad Leumi for the support of the
  schools" (the bulletin prints "Â£40,000"), and the Vaad Leumi became "responsible for the entire budget and control of the school
  system" (JTA-321207).
- December 1935: the Government gave the Vaad Leumi power to levy a tax of ten percent of the rental value of Jewish-owned
  property (JTA-351205).
- 25 September 1940 (dispatch datelined 25 September, bulletin of 26 September): the Assembly, "holding its eighth extraordinary session, today adopted a budget for the coming year"
  (JTA-400926). The bulletin's figures are garbled by the scan ("1480,000 income and 1170,000 expenditures", with "L130,000" in
  Government grants in the same text), so the amounts are not used.
- 9 March 1942 (dispatch datelined 9 March, bulletin of 10 March): the three-day session, opened "today", "will also vote on the budget for the Vaad Leumi" (JTA-420310).
- 26 and 30 March 1943: on 26 March (JTA-430328, bulletin of 28 March) the Assembly "was asked today to approve a $7,400,000 budget to meet the needs of Palestine's
Jews", of which the Government would give $400,000 (JTA-430328); on 30 March (JTA-430331, bulletin of 31 March) it "today approved a budget of $680,000 to finance
the activities of the Jewish National Council for the 1943-1944 fiscal period" (JTA-430331). The two figures differ by a factor of
about ten (680,000 / 7,400,000 = 9.2%, computed here) and the bulletins print dollars and do not state the currency actually used;
whether the first is the whole community's need and the second the Vaad Leumi's own budget is not stated [C]. Neither is preferred.

The Assembly's approval is [V] for 1932, 1940 and 1943, in addition to the legal powers above. Dates of the
votes in the other years, the amounts, and the tallies stay [GAP].

### The Jewish Agency's budget

(24 Oct) Partly closed for 1929 and 1930, [GAP] for 1931 to 1948. JTA-290814 [V]: on 13 August 1929 (dispatch datelined Zurich, 13 August; bulletin of 14 August) "By a unanimous vote the Council of
the Jewish Agency in a session which lasted one hour today approved the budget for the Palestine work for the coming year in the
amount of $3,750,000", a budget "previously decided upon at the Sixteenth Zionist Biennial Congress", recommended by the budget
committee of the Council. In September 1930 the budget went the other way round [V]: JTA-300903 says the Zionist Actions Committee had
adopted a budget for general work, that a cut to $2,000,000 with a separate $750,000 for colonization was "considered by the budget
committee of the Jewish Agency" and was likely to be adopted by the Administrative Committee; JTA-300910 says "the Administrative
Committee of the Jewish Agency adopted a budget for which it did not vote" ("it" is the Labour fraction speaking), so the Administrative
Committee, not the Council, voted the 1930 budget (a reading of that phrase, not a stated fact). The sequence is Actions Committee, budget committee, Administrative or Council vote;
the amounts adopted in 1930 are not stated. For the years after 1930 no source read says which body approved it. Earlier finding: from August 1929 the
enlarged Agency's constitution "provided for parity between Zionists and non-Zionists on its three governing bodies:
the 224-member council, the administrative committee, and the executive" (EJ-JA) [V], but EJ-JA does not say which body
approved the budget. AJYB-46 p. 461 gives the Agency's income and expenditure for 5704 (1943-44), £P2,867,751 and
£P3,185,932 [V], with no approval procedure. The Assembly did not elect the Agency's Executive: "The Executive of the
Jewish Agency for Palestine was elected by the World Zionist Congress in December 1946" (UN-JA11, Shertok's letter)
[V]. The design's note that the Assembly "elected the Vaad Leumi, not the Jewish Agency Executive" is confirmed.

## 7. The Vaad Leumi each Assembly elected, and its parties

The design (§5.2, §4.2) takes the coalition of each period to be the parties of the Vaad Leumi executive. The sources
read give the party make-up of the full Vaad Leumi (the General Council) after each election, not of its smaller
executive. EJ-GOV [V]: "The membership of the Va'ad Le'ummi during 1920-48 varied from 23 to 42, representing almost
all parties in the larger body. It elected an Executive of 6 to 14 members." EJ-PL [V] for the first Assembly: "The
Va'ad Le'ummi was headed by a presidium of three, assisted by an executive council whose membership varied from 7 to
14."

| Assembly | Vaad Leumi size | Parties | Source |
|---|---|---|---|
| First (October 1920) | 36 | Ahdut ha-Avodah 8, Sephardim 6, Ha-Po'el ha-Za'ir 5, Orthodox 5, Progressives 3, Farmers 2, Yemenites 2, Mizrachi 1, Clerks 1; Dizengoff, Jabotinsky, Margolis-Kalvaryski and Yellin "elected on a personal basis" | EJ-PL [V]. The figures add up to 37 with the four personal members (computed here), one more than 36 [C, internal]; not resolved |
| Second (1926) | 38 | "18 representing the United Bloc (Mizrachi, Sephardim, Yemenites, Farmers, and others), 9 for Ahdut ha-Avodah, 5 for Ha-Po'el ha-Za'ir, and 2 each for Women, Revisionists, and the Democratic group" | EJ-PL [V]; adds up to 38 (computed here). The year of the session is not given; the second Assembly was elected in December 1925 |
| Third (1931) | 23 | Mapai 11, Sephardim 4, Mizrachi 3, General Zionists 3, Women 1, Yemenites 1 (EJ-PL); "11 were Labour, four Sephardim, three Mizrahi, three General Zionists, and two were members representing the smaller factions" (MR-1930 para. 23). The Revisionists declined to join after the Assembly rejected their three resolutions (MR-1930 para. 23) | [V], the two agree. MR-1930 para. 24: the General Council "will, in due course, form from amongst its members an Executive Committee, of about seven members" |
| Third, from 1932 | 22 (computed here) | JTA-320305 (bulletin of 5 March 1932, dispatch datelined 4 March): the Revisionists re-entered, so the Vaad Leumi "will now consist of 10 Labour representatives, 5 Revisionists, 2 General Zionists, 2 Mizrachists, 1 Sephardi, 1 Yemenite, and one Women's representative". EJ-PL: the Revisionists "were given 5 seats in the following year, leaving Mapai with 10 seats and the three other main parties with 2 each" | [V]. The JTA and EJ-PL agree on Mapai 10 and Revisionists 5; they differ on the Sephardim (1, against "2 each") [C]; preferred JTA, the dated bulletin. The size 22 is the sum of the figures (10 + 5 + 2 + 2 + 1 + 1 + 1), not a stated total |
| Fourth (September 1944) | 42 | Mapai 15 and 8 "from the other two left-wing parties" (EJ-PL); "with all factions represented" (AJYB-46 p. 460). Ben-Zvi president, Remez chairman (EJ-PL; signed so in UN-JA11) | [V]. The other 19 seats by party: [GAP] |
| Fourth, in 1948 | Executive of 14 | "the 14 members of the Executive of the Va'ad Le'ummi" sat in the People's Council (EJ-PL) | [V]; their parties: [GAP] |
| Executive, 18 October 1939 (not tied to an Assembly's first year) | 18 | JTA-391019 (bulletin of 19 October 1939, dispatch datelined 18 October, "announced today"): "The 18-member Executive includes six representatives of the Mapai, Zionist labor party; two Revisionist, two of the Sephardic group and one each of the Mizrachi, the Poel Hamizrachi, the German imigrants' organization [sic], the General Zionists 'A', General Zionists 'B' and the farmers" | [V]. The parties named add up to 16 (6 + 2 + 2 + 6, computed here), not 18 [C, internal]; the same bulletin says Rutenberg (president) and Szold held "the balance of voting power" but does not say they are the other two of the 18, so that is not filled in. The only Vaad Leumi executive with its parties found |
| Fifth Vaad Leumi, May 1947 (elected by the closing session of the fourth Assembly) | not given | JTA-470525 (bulletin of 25 May 1947, dispatch datelined 23 May: "elected last night", so the night of 22 May 1947): "15 from the Mapai; five from the Tenua L'Achdut Avoda; four from the Hashomer Hatzair; six from the Mizrachi; four Aliys Hadasha; two General Zionists and several representing minority parties" | [V]. The named seats add up to 36 (computed here); the "several" others are not counted. This is the Vaad Leumi of 1947, not the one elected in September 1944 in the row above |

The heads: EJ-GOV gives "David Yellin (1920-29), Pinhas Rutenberg (1929-31), Izhak Ben-Zvi (1931-44), and David Remez
(1944-48), with Ben-Zvi as president", which differs from EJ-VL's presidium of three from 1920 to 1925 [C, inside
EJ]. The offices note for #88 sets out both and prefers EJ-VL; this note does not change that.

The executive (ועד פועל, later הנהלה) of each period by party: [GAP]. he.wiki הוועד הלאומי lists the names of the
executive members for each Assembly, without parties and without dates [S]; it would let a later pass assign parties
from biographies, but that is inference and is not done here.

(24 Oct) Searched again. Closed for one executive only: the 18-member Executive of October 1939 by party (table above, JTA-391019). The
executives of the other periods by party stay [GAP]. Read: the JTA bulletins listed in the source table and the searches "Vaad Leumi
executive elected", "Vaad Leumi new executive" and "National Council Palestine executive committee elected" in the JTA archive, whose
hits give the full council's parties (1931, 1932, 1947), the presidium of the 1931 Assembly (named in the §4 notes above) and the
1939 executive, but no other executive by party. MR-1930 para. 24 (seen above) says the 1931 General Council would form an Executive
Committee "of about seven members" without naming it.

Who the Vaad Leumi said it represented, in February 1948: the cable in UN-JA11 lists the parties that took part in the
1944 election with their seats (§5), and Shertok's letter says "The only Jewish party or group which is outside the
organisational framework both of the Jewish Agency and of the Vaad Leumi is the organisation known as Agudath Israel"
[V].

## 8. The People's Council (Mo'etzet ha-Am), April and May 1948

NAOR page numbers below are the flipbook's (265 = printed 263, 266 = printed 264); the passage is Medding's (1988), reprinted in Naor's book (see the source table).

- Set up by the Zionist General Council: "On April 12, 1948, the Zionist General Council laid the foundations for the
  self-governing institutions of the Jewish state by appointing a provisional legislature, called Mo'ezet ha-Am
  (People's Council), and an executive called Minhelet ha-Am (People's Administration)" (EJ-PL) [V]. CZA-30: the
  Zionist General Council, "in session in April 1948, announced the setting up of a 'National Council' of 37 members,
  and the 'establishment and appointment of a National Administration of 13 members, which would be responsible in its
  actions to the National Council'" [V, to the month]. Before that, on 1 March 1948, the Vaad Leumi decided that its
  executive and the Jewish Agency's would form the core of the council, with power to co-opt groups not represented in
  either (NAOR p. 265) [V].
- Size: 37 (EJ-PL, CZA-30, NAOR) [V]. Built from "the 14 members of the Executive of the Va'ad Le'ummi ..., the 11
  members of the Jewish Agency Executive from the yishuv, and 12 delegates from parties not represented on either"
  (EJ-PL) [V]. "Chaim Weizmann was the president of the Council and its 38th member" (EJ-PL) [V].
- (24 Oct) JTA-480414 (bulletin of 14 April 1948, dispatch datelined 13 April) [V]: the Jewish Agency announced that a 13-man cabinet would be formed "in accordance
with the decision adopted yesterday in Tel Aviv by the Zionist Actions Committee", which "decided that the central Jewish authority
would he [sic] composed of 37 members, including the 13 Cabinet members", to be joined by Arab delegates "who recognize the Jewish State".
Counted from the dateline, "yesterday" is 12 April, the same as EJ-PL; the body is called the Zionist Actions Committee here and the
Zionist General Council in EJ-PL and CZA-30, and no source read says whether they are one body. The
bulletin counts the 13 ministers inside the 37, while EJ-PL says 37 plus Weizmann as the 38th member; not reconciled.
- (24 Oct) An earlier plan, JTA-480309 (bulletin of 9 March 1948, dispatch datelined 8 March) [V]: a "31-member Provisional Council of Government and cabinet of 13"
  was named late the night before the dateline, that is on 7 March, with seats "ten to the Palestine Labor Party, [th]ree to the Mizrachi Organization, two to the
  Mizrachi laborites, four to the General [Zi]onists, two to the United Labor Party, two to the Revisionists, and one each to the
  [Ali]yah Hadasha, the Yemenite community, the Sephardic community, the Agudas Israel, the Agudah laborites and the Communists", and
  the Jewish National Council to be represented by two members. The named seats add up to 29 (computed here), and the two for the
  Jewish National Council make 31, as stated; several letters of the bulletin are lost in the scan (restored in square brackets above). It is a plan of March, not the 37 of April, and it is not used below.
- Basis: seats were proportionate to the 1944 election, and the groups that had boycotted it (Agudat Israel, the
  Revisionists, the Sephardim and the Communist Party, in NAOR's list) were added in April 1948 (NAOR p. 265) [V].

Seats by party:

| Party | EJ-PL [V] | NAOR p. 265-266 [V] | he.wiki מועצת העם [S] | en.wiki "Provisional State Council" [S] |
|---|---|---|---|---|
| Mapai | 10 | 10 | 10 | 10 |
| General Zionists | 6 | 6 | 4 | 3 |
| Mapam | 5 | 5 | 5 | 5 (Mapam 3, Ahdut ha-Avodah-Poalei Zion 1, Left Poale Zion 1) |
| Mizrachi and Ha-Po'el ha-Mizrachi | 5 | 5 | 5 (3 + 2) | 5 (3 + 2) |
| Agudat Israel and Po'alei Agudat Israel | 3 (EJ-PL: "three from Agudat Israel", Po'alei not named) | 3 (NAOR names both) | 3 (2 + 1) | 3 (2 + 1) |
| Revisionists | 3 | 3 | 3 | 3 |
| Aliyah Hadashah | 1 | 1 | 3 (as the Progressive Party) | 3 |
| WIZO | 1 | 1 | 1 | 1 |
| Sephardim | 1 | 1 | 1 | 1 |
| Yemenites | 1 | 1 | 1 | 1 |
| Communists | 1 | 1 | 1 | 1 |
| Independent | none | none | none | 1 (Gruenbaum) |
| Total | 37 (computed here) | 37 (computed here) | 37 (computed here) | 37 (computed here) |

(24 Oct) Searched again and not found. Read: JTA-480309, JTA-480414 and JTA-480506 (above) and the JTA hits for "People's Council",
"Provisional Council of Government" and "Zionist General Council" for March to May 1948; none gives the 37 seats by party, so the
Wikipedia columns stay [S] and the seats by party stay [C] between them and EJ-PL and NAOR.

[C] Preferred: EJ-PL, which NAOR matches line by line. The Wikipedia figures may count members by later party, or
count Aliyah Hadashah members elected on the General Zionist side; that is not checked.

- The People's Administration (13): Mapai 4, General Zionists 2, Mapam 2, and one each for Mizrachi, Ha-Po'el
  ha-Mizrachi, Agudat Israel, the Sephardim and Aliyah Hadashah (NAOR p. 266) [V]; that is 13 (computed here). The
  Revisionists and Communists sat in the Council but not in the Administration (NAOR p. 266) [V].
- Sittings: NAOR p. 266 records Ben-Gurion addressing the People's Council on 4 May 1948 [V]. he.wiki dates its first
  sitting to 4 May 1948 and counts four sittings in all [S]. The full list of sittings is a [GAP].
  (24 Oct) JTA-480506, a bulletin dated 6 May 1948 (dispatch datelined 5 May), reports Ben-Gurion "addressing a session of the 37-member National Council of the
  Jewish State" "today" and that the session "adjourned for two days"; "today" is 5 May, one day after the 4 May of NAOR and he.wiki, and the
  bulletin does not say it is the same sitting [C]. It also says "its budget will be adopted this week".
- End: "On May 14, the People's Council and the People's Administration became respectively the Provisional Council
  of State and the Provisional Government" (EJ-PL; NAOR p. 266) [V]. The Provisional State Council's last meeting was on
  3 February 1949 (KN-PSC) [V]. Its budget role is in [budget-approval-1948-56.md](../budget-approval-1948-56.md).

Against the design (§2, §5.2): "People's Council (Moetzet HaAm), 37 members" from 12 April 1948 is confirmed by EJ-PL
[V]; CZA-30 confirms April. Whether the window should open on 12 April (appointment) or 4 May (first sitting) is a
design choice; both dates are given above.

## 9. Gaps and disagreements, in one list

Disagreements [C], with the preferred value: the 1944 election date (1 August over 2 August); the 1944 size (171 over
173); 1944 voters (202,448 of 300,018); Mapai's 1944 seats (63 over 64); Aliyah Hadashah's 1944 seats (18, against 16
and 19); 1931 turnout and electors (MR-1930's 50,436 of
75,046, 65%, over EJ-PL's 89,656 and 56%); 1931 Revisionist and General Zionist seats (MR-1930); 1931 lists
represented (12 over 14); 1925 votes (36,437 or 35,987, not settled); 1925 women's seats (9 or 13, not settled); 1925
lists (26 or 25); the 1920 Orthodox seats (51 over 53); the 1920 Vaad Leumi size (36, though its parts add to 37);
the 1920 Jerusalem vote (eight days later, or 3 May, not settled); the People's Council's party seats (EJ-PL and NAOR
over Wikipedia). (24 Oct) New: 1925 turnout has a third value, 33,845 (JTA-260113); the 1943 budget, $7,400,000 asked against
$680,000 approved; the date of a sitting reported in the bulletin of 6 May (5 May, against 4 May in NAOR and he.wiki); the 1932 Sephardic seats on the Vaad Leumi (1 in JTA-320305, "2 each" in EJ-PL); the 1939 executive's
party count (16 named of 18). Closed by a dated bulletin: the 1944 opening session (12 September); the 1931 Revisionist and General
Zionist seats (final count 16 and 4).

Gaps [GAP]: the day of the 1920 Jerusalem and Orthodox polls;
the day of the 1925 Yemenite poll; the full 1925 list of seats; the seats of the small 1944 lists; whether the 9,219
soldiers' votes are in the 1944 total; whether the 1944 boycotters later joined the Vaad Leumi; the party make-up of
each Vaad Leumi executive (#89 asks for it; only the 1939 executive and the full Vaad Leumi's parties were found, §7); the 1944 Vaad Leumi beyond the parties named; the 1928 Gazette wording of
regulation 13 and its 1934 amendment; the dates, amounts and tallies of the annual budget votes in the Assembly (only 1932, 1940
and 1943 are dated, §6); which body approved the Jewish Agency's budget after 1930; the People's Council's seats by party in a source
beyond EJ-PL and NAOR, and its full list of sittings.

Where to look next: Attias, *Sefer ha-Te'udot shel ha-Va'ad ha-Le'ummi* (1963) and *Knesset Israel be-Eretz Israel*
(1944); the Mandate reports for 1920 to 1928 on UNISPAL; the Hebrew press of April 1920, December 1925, January 1931 and
August 1944 on the National Library's site, from a browser.
