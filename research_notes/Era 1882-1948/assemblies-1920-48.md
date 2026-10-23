# The Assemblies of Representatives, the Vaad Leumi and the People's Council, 1920 to 1948

Researched 22 October 2026. Every URL below was opened on that date. Research note for issue #89 (Yishuv 10, the
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

Not reached: the National Library's historical newspapers (nli.org.il answers 403 to a script), so the *Doar
HaYom* report of 22 April 1920 that he.wiki cites for the 1920 postponement was not read; the Seventh Eye article
by Rafi Mann on the 1920 Assembly (read as an archive copy of 24 April 2019; it does not give the postponement);
Abramov's *Perpetual Dilemma*; Moshe Attias, *Knesset Israel be-Eretz Israel* (1944) and *Sefer ha-Te'udot shel
ha-Va'ad ha-Le'ummi* (1963), the standard document collections, which are not online in a readable form; the Mandate
reports for 1920 to 1928 (only 1929 and 1930 were read).

## 1. The four Assemblies at a glance

| Assembly | Election | First session | Members | Lists with seats | Voters | Electors | Turnout |
|---|---|---|---|---|---|---|---|
| First | 19 April 1920 [V] EJ-PL; Jerusalem later [C], see §2 | October 1920 [V] EJ-PL; 7 October 1920 [S] WP-EN | 314 [V] KN-AR, EJ-PL | 20 [V] KN-AR, EJ-PL | 22,200 [V] KN-AR | 28,765 [V] EJ-PL | 77% [V] EJ-PL |
| Second | 6 December 1925 [V] EJ-PL | [GAP] | 221 [V] KN-AR, EJ-PL, MR-1930 | 26 KN-AR; 25 EJ-PL [C] | 36,437 [V] KN-AR | 64,764 [V] EJ-PL | 57% [V] EJ-PL |
| Third | 5 January 1931 [V] MR-1930, EJ-PL | [GAP] | 71 [V] KN-AR, MR-1930, EJ-PL | 12 MR-1930, KN-AR; 14 EJ-PL [C] | 50,436 [V] MR-1930, KN-AR | 75,046 voters' cards MR-1930; 89,656 EJ-PL [C] | 65% MR-1930; 56% EJ-PL [C] |
| Fourth | 1 August 1944 [V] EJ-PL, UN-JA11; 2 August AJYB-46 [C] | September 1944 [V] AJYB-46; 12 or 13 September [C], see §5 | 171 [V] KN-AR, EJ-PL, UN-JA11 | 18 [V] KN-AR, EJ-PL | 202,448 [V] UN-NC84, KN-AR; others [C] | 300,018 [V] UN-NC84, UN-JA11 | 67% [V] EJ-PL |

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
5 Jan 1931" and "from Aug 1944" are election dates, not first sessions; the first sessions of the second and third
Assemblies are a [GAP]. The design's 1944 values (2 August, 173 seats) are not preferred: see §5.

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
"The Assembly of Representatives, 12.9.1944" [V]. [C] 12 or 13 September 1944. Neither is preferred: no source read states the day of the session in words.

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

How each year's budget was actually voted (dates, amounts, tallies): [GAP]. No session record was read.

### The Jewish Agency's budget

[GAP]. No source read says how the Jewish Agency's budget was approved before 1948. What was found: from August 1929 the
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
| Third, from 1932 | not given | The Revisionists "were given 5 seats in the following year, leaving Mapai with 10 seats and the three other main parties with 2 each" | EJ-PL [V]. Read literally this changes the whole council, not only adds 5; the new size is a [GAP] |
| Fourth (September 1944) | 42 | Mapai 15 and 8 "from the other two left-wing parties" (EJ-PL); "with all factions represented" (AJYB-46 p. 460). Ben-Zvi president, Remez chairman (EJ-PL; signed so in UN-JA11) | [V]. The other 19 seats by party: [GAP] |
| Fourth, in 1948 | Executive of 14 | "the 14 members of the Executive of the Va'ad Le'ummi" sat in the People's Council (EJ-PL) | [V]; their parties: [GAP] |

The heads: EJ-GOV gives "David Yellin (1920-29), Pinhas Rutenberg (1929-31), Izhak Ben-Zvi (1931-44), and David Remez
(1944-48), with Ben-Zvi as president", which differs from EJ-VL's presidium of three from 1920 to 1925 [C, inside
EJ]. The offices note for #88 sets out both and prefers EJ-VL; this note does not change that.

The executive (ועד פועל, later הנהלה) of each period by party: [GAP]. he.wiki הוועד הלאומי lists the names of the
executive members for each Assembly, without parties and without dates [S]; it would let a later pass assign parties
from biographies, but that is inference and is not done here.

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

[C] Preferred: EJ-PL, which NAOR matches line by line. The Wikipedia figures may count members by later party, or
count Aliyah Hadashah members elected on the General Zionist side; that is not checked.

- The People's Administration (13): Mapai 4, General Zionists 2, Mapam 2, and one each for Mizrachi, Ha-Po'el
  ha-Mizrachi, Agudat Israel, the Sephardim and Aliyah Hadashah (NAOR p. 266) [V]; that is 13 (computed here). The
  Revisionists and Communists sat in the Council but not in the Administration (NAOR p. 266) [V].
- Sittings: NAOR p. 266 records Ben-Gurion addressing the People's Council on 4 May 1948 [V]. he.wiki dates its first
  sitting to 4 May 1948 and counts four sittings in all [S]. The full list of sittings is a [GAP].
- End: "On May 14, the People's Council and the People's Administration became respectively the Provisional Council
  of State and the Provisional Government" (EJ-PL; NAOR p. 266) [V]. The Provisional State Council's last meeting was on
  3 February 1949 (KN-PSC) [V]. Its budget role is in [budget-approval-1948-56.md](../budget-approval-1948-56.md).

Against the design (§2, §5.2): "People's Council (Moetzet HaAm), 37 members" from 12 April 1948 is confirmed by EJ-PL
[V]; CZA-30 confirms April. Whether the window should open on 12 April (appointment) or 4 May (first sitting) is a
design choice; both dates are given above.

## 9. Gaps and disagreements, in one list

Disagreements [C], with the preferred value: the 1944 election date (1 August over 2 August); the 1944 size (171 over
173); 1944 voters (202,448 of 300,018); Mapai's 1944 seats (63 over 64); Aliyah Hadashah's 1944 seats (18, against 16
and 19); the 1944 opening session (12 or 13 September, not settled); 1931 turnout and electors (MR-1930's 50,436 of
75,046, 65%, over EJ-PL's 89,656 and 56%); 1931 Revisionist and General Zionist seats (MR-1930); 1931 lists
represented (12 over 14); 1925 votes (36,437 or 35,987, not settled); 1925 women's seats (9 or 13, not settled); 1925
lists (26 or 25); the 1920 Orthodox seats (51 over 53); the 1920 Vaad Leumi size (36, though its parts add to 37);
the 1920 Jerusalem vote (eight days later, or 3 May, not settled); the People's Council's party seats (EJ-PL and NAOR
over Wikipedia).

Gaps [GAP]: the first sessions of the second and third Assemblies; the day of the 1920 Jerusalem and Orthodox polls;
the day of the 1925 Yemenite poll; the full 1925 list of seats; the seats of the small 1944 lists; whether the 9,219
soldiers' votes are in the 1944 total; whether the 1944 boycotters later joined the Vaad Leumi; the party make-up of
each Vaad Leumi executive (#89 asks for it; only the full Vaad Leumi's parties were found, §7); the 1932 and 1944 Vaad Leumi beyond the parties named; the 1928 Gazette wording of
regulation 13 and its 1934 amendment; the record of any annual budget vote in the Assembly; how the Jewish Agency's
budget was approved before 1948; the People's Council's sittings.

Where to look next: Attias, *Sefer ha-Te'udot shel ha-Va'ad ha-Le'ummi* (1963) and *Knesset Israel be-Eretz Israel*
(1944); the Mandate reports for 1920 to 1928 on UNISPAL; the Hebrew press of April 1920, December 1925, January 1931 and
August 1944 on the National Library's site, from a browser.
