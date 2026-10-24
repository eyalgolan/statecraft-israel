# The Fourth to Eighth Knessets: elections, results, mergers and budgets, 1957 to 1977

Researched 23 October 2026. Every URL below was opened or queried on that date. Research note for issue #114 (titled "1957-77
2", the research part only, opened by the owner's ruling of 22 October 2026 on #49). Research only: nothing here is
drafted for the game, and no file in `content/` is changed. The `legislatures.yaml` records stay paused.

## Purpose

Fix, from the best sources reachable, the facts #114 asks for: one entry per Knesset elected from 1959 to 1973 (the
Fourth to the Eighth; the Ninth, elected in 1977, is in `research_notes/Era 1977-93/knessets-1977-93.md`), with the
election date, the first sitting, the threshold and the seats by list; each merger the issue names (the Alignment,
Gahal, Labor, the Alignment with Mapam, Likud) and the other splits and mergers inside each term; the governments; and
how each budget of 1957 to 1977 was approved.

The format follows `research_notes/Era 1977-93/knessets-1977-93.md` and, for the budgets,
`research_notes/budget-approval-1948-56.md`, which also describes the budget procedure (first reading, the Finance
Committee, second and third readings) that this note does not repeat.

## Conventions

- [V] read on a primary source opened this session (the source key or URL is given). The Knesset's own pages, its
  OData tables and the Central Elections Committee count as primary here.
- [S] single secondary source, usually Wikipedia, not confirmed in a [V] source. A lead, to be confirmed before
  encoding.
- [C] conflict. Every value is given with its source, and the preferred one is named.
- [GAP] not found in any source read. No number, name or date is filled in by inference.
- "Computed here" marks arithmetic done for this note (a sum of seats, a percentage). It checks a source; it is not a
  source.
- "Our reading" marks a conclusion this note draws that no source states in those words.
- Dates are written 3 November 1959 style. Hebrew faction names are copied from the Knesset's faction table (OD-F) and
  glossed in English. Quotations are short and verbatim; a Hebrew quotation is followed by this note's translation.

## Sources and how they were read

Every request used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

Access from this machine: the Knesset's HTML pages are read as Internet Archive copies, as in the 1977-93 note. Unlike
that note, the Knesset's OData service (knesset.gov.il/Odata) and its file server (fs.knesset.gov.il) both answered
with a redirect to `https://www.knesset.gov.il/maintenance-page-geo` (blocked outside Israel). The OData tables were
therefore read as the copies the Open Knesset project publishes (production.oknesset.org/pipelines/data), which are
CSV exports of the same Knesset tables with the same ids and columns. They are marked "mirror" below. One law text
was reached through an Internet Archive copy (L-PP-8); no budget law, and no plenum protocol of a budget vote, was
archived (see §7).

| Key | Source | Note |
|---|---|---|
| K-RES-4 ... K-RES-8 | Knesset, "Knesset Elections Results", one page per Knesset, read as archive copies: [4](https://web.archive.org/web/20160817101553/http://knesset.gov.il/description/eng/eng_mimshal_res4.htm), [5](https://web.archive.org/web/20160815235430/http://knesset.gov.il/description/eng/eng_mimshal_res5.htm), [6](https://web.archive.org/web/20160817100637/http://knesset.gov.il/description/eng/eng_mimshal_res6.htm), [7](https://web.archive.org/web/20160817091405/http://knesset.gov.il/description/eng/eng_mimshal_res7.htm), [8](https://web.archive.org/web/20160817084111/http://knesset.gov.il/description/eng/eng_mimshal_res8.htm) | Election date, eligible voters, valid votes, threshold, votes per seat, and votes, percent and seats for each list that won seats. Lists without seats are not shown. A December 2016 copy of page 5 is an empty script page; the August 2016 copy is used |
| K-HIST-3 ... K-HIST-8 | Knesset, "Factional and Government Make-Up of the ... Knesset", archive copies: [3](https://web.archive.org/web/20160304030116/http://www.knesset.gov.il/history/eng/eng_hist3_s.htm), [4](https://web.archive.org/web/20160522174906/https://www.knesset.gov.il/history/eng/eng_hist4_s.htm), [5](https://web.archive.org/web/20160815201505/http://knesset.gov.il/history/eng/eng_hist5_s.htm), [6](https://web.archive.org/web/20160303223458/http://www.knesset.gov.il/history/eng/eng_hist6_s.htm), [7](https://web.archive.org/web/20160304085532/https://www.knesset.gov.il/history/eng/eng_hist7_s.htm), [8](https://web.archive.org/web/20160303200104/http://www.knesset.gov.il/history/eng/eng_hist8_s.htm) | Seats at election and "Number of Seats Before Next Elections", footnotes on each move, and each government with its formation date and coalition. The 2017 and later copies of pages 6 and 8 are the bot challenge; the March 2016 copies are used |
| K-MS | Knesset, "Mergers and Splits Among Parliamentary Groups", [archive copy of 3 March 2016](https://web.archive.org/web/20160303233309/http://www.knesset.gov.il/faction/eng/FactionHistoryAll_eng.asp) | Every faction merger, split and renaming, dated, Knesset by Knesset. Some Hebrew words on the page are mojibake in the copy (noted where it matters) |
| OD-F | Knesset OData `KNS_Faction`, mirror: https://production.oknesset.org/pipelines/data/knesset/kns_faction/kns_faction.csv | Every faction with its start and finish date. Rows for Knessets 3 to 9 used |
| OD-D | Knesset OData `KNS_KnessetDates`, mirror: https://production.oknesset.org/pipelines/data/knesset/kns_knessetdates/kns_knessetdates.csv | First day of each Knesset's sessions |
| OD-P | Members' faction terms (derived, not a Knesset OData table; its counts are tagged [V] in this note with that caveat), mirror: https://production.oknesset.org/pipelines/data/members/mk_individual/mk_individual_factions.csv | Open Knesset's table of each member's faction terms (member id, faction id, start, finish), built from the Knesset's `KNS_PersonToPosition`. Counted here: members sitting in each faction on a given day |
| OD-B | Knesset OData `KNS_Bill`, mirror: https://production.oknesset.org/pipelines/data/bills/kns_bill/kns_bill.csv | Laws passed (StatusID 118, "התקבלה בקריאה שלישית", passed third reading, per `KNS_Status`), with publication date, series, issue and page |
| OD-I | Knesset OData `KNS_PlmSessionItem` and `KNS_PlenumSession`, mirrors: https://production.oknesset.org/pipelines/data/plenum/kns_plmsessionitem/kns_plmsessionitem.csv and https://production.oknesset.org/pipelines/data/plenum/kns_plenumsession/kns_plenumsession.csv | Each sitting at which a bill was on the agenda, with the bill's status there: 141 laid on the table for first reading, 111 for plenum debate towards first reading, 114 for debate towards second-third reading, 117 for debate towards third reading (`KNS_Status`, mirror https://production.oknesset.org/pipelines/data/knesset/kns_status/kns_status.csv) |
| OD-DOC | Knesset OData `KNS_DocumentBill` and `KNS_DocumentPlenumSession`, mirrors under https://production.oknesset.org/pipelines/data/bills/kns_documentbill/ and https://production.oknesset.org/pipelines/data/plenum/kns_documentplenumsession/ | The fs.knesset.gov.il file of each law and protocol (listed in §5 so they can be read from inside Israel) |
| L-PP-8 | Eighth Knesset and Local Authorities Elections (Temporary Provision) Law, 5734-1973 (חוק הבחירות לכנסת השמינית ולרשויות המקומיות (הוראת שעה), התשל"ד-1973), Sefer HaHukim 718, p. 10, [archive copy of the Knesset file](https://web.archive.org/web/20160304223913/http://fs.knesset.gov.il/7/law/7_lsr_208004.PDF), read in its text layer | Postponement of the 1973 election |
| CEC-PCT | Central Elections Committee, "Voting Percentages in Past Elections", [archive copy of 12 January 2013](https://web.archive.org/web/20130112021104/http://www.bechirot.gov.il/elections19/eng/history/PercentVotes_eng.aspx) | Eligible voters and turnout per Knesset |
| CEC-BF | Central Elections Committee, "Comparative data from elections to the Sixth Knesset through today", [archive copy of 12 January 2013](https://web.archive.org/web/20130112021057/http://www.bechirot.gov.il/elections19/eng/history/BasicFacts_eng.aspx) | Eligible voters (rounded), polling stations and the committee's budget, from 1965 |
| JTA-660112 | Jewish Telegraphic Agency, "Eshol to Present Cabinet Today to Knesset; Composed of 18 Members", 12 January 1966, https://jta.org/1966/01/12/archive/eshol-to-present-cabinet-today-to-knesset-composed-of-18-members | The 1965 election date (C1) |
| WP-1959 ... WP-1973 | English Wikipedia, raw wikitext: "1959 Israeli legislative election" (oldid 1375053307), "1961 ..." (oldid 1335062269), "1965 ..." (oldid 1375053652), "1969 ..." (oldid 1333315559), "1973 ..." (oldid 1379863385) | [S], comparison only. Their results tables cite the Israel Democracy Institute and Nohlen *et al.*; they add the lists that won no seat and the invalid votes |
| WP-PARTY | English Wikipedia, raw wikitext: "Alignment (Israel)" (oldid 1377159825), "Gahal" (oldid 1363140906), "Israeli Labor Party" (oldid 1375841534), "Likud" (oldid 1380058620), "Israeli Liberal Party" (oldid 1350957487), "Independent Liberals (Israel)" (oldid 1350104342), "Rafi (political party)" (oldid 1367367974) | [S], founding dates and components |

Not used: the Israel Government Year Book and the Encyclopaedia Judaica were not needed for any figure here, because
the Knesset's own tables cover every election and faction; they were not opened. The Central Elections Committee's
current site serves only recent elections (as the 1977-93 note found), so its per-list results for 1959 to 1973 are
[GAP]; the Knesset tables stand in, checked against OD-P.

## 1. The five Knessets at a glance

| Knesset | Election | Set by | First sitting | Term ended | Threshold | Eligible voters (K-RES) | Valid votes | Votes per seat | Turnout (CEC-PCT) |
|---|---|---|---|---|---|---|---|---|---|
| 4th | 3 Nov 1959 | end of term (our reading; §1.1) | 30 Nov 1959 | 4 Sep 1961 | 1% (9,693 votes) | 1,218,724 | 969,337 | 7,800 | 81.6% |
| 5th | 15 Aug 1961 | Dissolution of the Fourth Knesset Law, 1961 | 4 Sep 1961 | 22 Nov 1965 | 1% (10,070) | 1,274,280 | 1,006,964 | 8,332 | 83.0% (CEC-PCT) or 81.6% (computed) [C] C3 |
| 6th | 1 Nov 1965 (Knesset) or 2 Nov 1965 [C] C1 | end of term (our reading) | 22 Nov 1965 | 17 Nov 1969 | 1% (12,067) | 1,499,988 | 1,206,728 | 9,881 | 83.0% |
| 7th | 28 Oct 1969 | end of term (our reading) | 17 Nov 1969 | 21 Jan 1974 | 1% (13,677) | 1,758,685 | 1,367,743 | 11,274 | 81.7% |
| 8th | 31 Dec 1973, postponed from 30 Oct 1973 | L-PP-8 | 21 Jan 1974 | 13 Jun 1977 | 1% (15,668) | 2,037,478 | 1,566,855 | 12,424 | 78.6% |

Sources, all [V] unless marked:

- Election dates: K-RES-n ("Elections to the Fourth Knesset (3 November, 1959)" and so on) and K-HIST-n ("Elected on 3
  November, 1959."). For 1965 both Knesset pages say 1 November; see C1.
- First sitting: OD-D (first PlenumStart: 30 Nov 1959, 4 Sep 1961, 22 Nov 1965, 17 Nov 1969, 21 Jan 1974) and OD-F
  (every faction of each Knesset starts that day).
- Term end: the day the next Knesset's factions start in OD-F, which is the day the outgoing factions finish.
- Threshold, valid votes and votes per seat: K-RES-n. Each page heads the threshold "Qualifying threshold (1%)".
- Turnout: CEC-PCT. Eligible voters in CEC-PCT differ from K-RES for 1959 to 1969; see C2.

Arithmetic, computed here from K-RES-n:

- Seats sum to 120 in every election: 1959 (12 lists), 1961 (11), 1965 (13), 1969 (13), 1973 (10).
- The threshold is 1% of valid votes, rounded down: 9,693.37, 10,069.64 (printed 10,070, rounded up), 12,067.28,
  13,677.43 and 15,668.55.
- "Votes per seat" is the votes of the seat-winning lists divided by 120, rounded down: 936,077, 999,887, 1,185,823,
  1,352,898 and 1,490,948 votes give 7,800.6, 8,332.4, 9,881.9, 11,274.1 and 12,424.6.
- Turnout: WP-n's valid plus invalid votes divided by CEC-PCT's eligible voters gives 81.60%, 81.57%, 83.00%, 81.66% and
  78.58%. Four agree with CEC-PCT; 1961 does not (C3). The invalid-vote counts are [S], from WP-n only.

### 1.1 What set each election date

- 1959: OD-B lists no dissolution law for the Third Knesset. Basic Law: The Knesset was passed on 12 Feb 1958 (third
  reading at sitting 412, OD-I; Sefer HaHukim 244, p. 69, OD-B) [V]; its text was not read, so that the 1959 date was
  the end of the term it fixed is our reading.
- 1961: Dissolution of the Fourth Knesset Law, 5721-1961 (חוק התפזרות הכנסת הרביעית), on the agenda for second-third
  and third reading at sitting 265, 28 Mar 1961 (OD-I), published in Sefer HaHukim 335, p. 82, on 4 Apr 1961 (OD-B)
  [V]. The text was not reachable, so the date it set is [GAP] in the law; 15 Aug 1961 is from K-RES-5. K-HIST-4 links
  the fall of the Ninth Government to "the Lavon Affair": the Prime Minister "resigned on 31 January, 1961, against the
  background of a motion of no-confidence tabled by Herut and the General Zionists".
- 1965 and 1969: OD-B lists no dissolution law for the Fifth or Sixth Knesset, so neither election was brought forward
  by a dissolution law (our reading). OD-B does list two temporary-provision election laws whose texts were not read
  [GAP]: Knesset Elections (Temporary Provisions) Law, 5730-1969 (חוק הבחירות לכנסת (הוראות שעה), התש"ל-1969), Sefer
  HaHukim 576, p. 2, 5 Oct 1969, and Knesset and Local Authorities Elections (Temporary Provision) Law, 5733-1973
  (חוק הבחירות לכנסת ולרשויות המקומיות (הוראת שעה), התשל"ג-1973), Sefer HaHukim 678, p. 41, 18 Jan 1973. Whether
  either fixed an election date is open.
- 1973: L-PP-8, section 1: the elections to the Eighth Knesset set for "(30 באוקטובר 1973)" (30 October 1973) "נדחות"
  (are postponed) and are held "(31 בדצמבר 1973)" [V]. Section 2 extends the Seventh Knesset's term to the Eighth's
  first sitting. Section 3: for the length of the Eighth Knesset's term and the Ninth Knesset's election, the Eighth
  Knesset is treated "כאילו נבחרה" (as if it had been elected) on 30 Oct 1973 [V]. The law's footnote: "נתקבל בכנסת
  ביום כ"ט בתשרי תשל"ד (25 באוקטובר 1973)" (passed by the Knesset on 25 October 1973) [V], which matches OD-I (sitting
  464, 25 Oct 1973). WP-1973: "The election was initially scheduled for 30 October" and was postponed because of the
  Yom Kippur War [S, consistent].

## 2. Results per list

Each table is K-RES-n [V]. "OD-P" is the number of members sitting in that list's faction on the first sitting day,
counted here from OD-P [V]. "WP" compares votes and seats with WP-year: for every election the set of (votes, seats)
pairs of the seat-winning lists in WP is identical to K-RES (computed here), so the column says "agree". The Knesset
page's spelling of list names is kept. An asterisk on K-RES marks "Minority lists associated with Mapai" (1959, 1961)
or "with the Alignment" (1965 to 1973).

### 4th Knesset, election of 3 November 1959

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 30 Nov 1959 | WP |
|---|---|---|---|---|---|---|
| Mapai | מפלגת פועלי ארץ-ישראל (Workers' Party of the Land of Israel) | 370,585 | 38.2 | 47 | 47 | agree |
| Herut Movement | תנועת החרות | 130,515 | 13.5 | 17 | 17 | agree |
| National Religious Party | מפלגה דתית לאומית | 95,581 | 9.9 | 12 | 12 | agree |
| Mapam | מפלגת פועלים מאוחדת (United Workers' Party) | 69,468 | 7.2 | 9 | 9 | agree |
| General Zionists | ציונים כלליים | 59,700 | 6.2 | 8 | 8 | agree |
| Ahdut Ha'avodah | אחדות העבודה - פועלי ציון | 58,043 | 6.0 | 7 | 7 | agree |
| Religious Torah Front | חזית דתית תורתית | 45,569 | 4.7 | 6 | 6 | agree |
| Progressive Party | מפלגה פרוגרסיבית | 44,889 | 4.6 | 6 | 6 | agree |
| Maki | מפלגה קומוניסטית ישראלית (Israel Communist Party) | 27,374 | 2.8 | 3 | 3 | agree |
| Kidmah Ufituah* | קידמה ופיתוח (Progress and Development) | 12,347 | 1.3 | 2 | 2 | agree |
| Shituf Ve`ahvah* | שיתוף ואחווה (Cooperation and Brotherhood) | 11,104 | 1.1 | 2 | 2 | agree |
| Hakla`ut Ufituah* | חקלאות ופיתוח (Agriculture and Development) | 10,902 | 1.1 | 1 | 1 | agree |

K-HIST-4 calls the Torah list "United Torah Front" and the last list "Agriculture and Development" (C6).

### 5th Knesset, election of 15 August 1961

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 4 Sep 1961 | WP |
|---|---|---|---|---|---|---|
| Mapai | מפלגת פועלי ארץ-ישראל | 349,330 | 34.7 | 42 | 42 | agree |
| Herut Movement | תנועת החרות | 138,599 | 13.8 | 17 | 17 | agree |
| Liberal Party | מפלגה ליברלית ישראלית (Liberal Party of Israel) | 137,255 | 13.6 | 17 | 17 | agree |
| National Religious Party | מפלגה דתית לאומית | 98,786 | 9.8 | 12 | 12 | agree |
| Mapam | מפלגת פועלים מאוחדת | 75,654 | 7.5 | 9 | 9 | agree |
| Ahdut Ha'avodah | אחדות העבודה - פועלי ציון | 66,170 | 6.6 | 8 | 8 | agree |
| Maki | מפלגה קומוניסטית ישראלית | 42,111 | 4.2 | 5 | 5 | agree |
| Agudat Yisrael | אגודת ישראל | 37,178 | 3.7 | 4 | 4 | agree |
| Po'alei Agudat Yisrael | פועלי אגודת ישראל | 19,428 | 1.9 | 2 | 2 | agree |
| Shituf Ve`ahvah* | שיתוף ואחווה | 19,342 | 1.9 | 2 | 2 | agree |
| Kidmah Ufituah* | קידמה ופיתוח | 16,034 | 1.6 | 2 | 2 | agree |

### 6th Knesset, election of 1 or 2 November 1965 (C1)

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 22 Nov 1965 | WP |
|---|---|---|---|---|---|---|
| Alignment | מערך | 443,379 | 36.7 | 45 | 45 | agree |
| Gahal | גוש חרות ליברלים (Herut-Liberal Bloc) | 256,957 | 21.3 | 26 | 26 | agree |
| National Religious Party | מפלגה דתית לאומית | 107,966 | 8.9 | 11 | 11 | agree |
| Rafi | רשימת פועלים ישראלית (Israel Workers' List) | 95,328 | 7.9 | 10 | 10 | agree |
| Mapam | מפלגת פועלים מאוחדת | 79,985 | 6.6 | 8 | 8 | agree |
| Independent Liberals | ליברלים עצמאיים | 45,299 | 3.8 | 5 | 5 | agree |
| Agudat Yisrael | אגודת ישראל | 39,795 | 3.3 | 4 | 4 | agree |
| Rakah | רשימה קומוניסטית חדשה (New Communist List) | 27,413 | 2.3 | 3 | 3 | agree |
| Kidmah Ufituah* | קידמה ופיתוח | 23,430 | 1.9 | 2 | 2 | agree |
| Po'alei Agudat Yisrael | פועלי אגודת ישראל | 22,066 | 1.8 | 2 | 2 | agree |
| Shituf Ve`ahvah* | שיתוף ואחווה | 16,464 | 1.3 | 2 | 2 | agree |
| Ha'olam Hazeh-Koah Hadash | העולם הזה - כוח חדש | 14,124 | 1.2 | 1 | 1 | agree |
| Maki | מפלגה קומוניסטית ישראלית | 13,617 | 1.1 | 1 | 1 | agree |

K-HIST-6 lists the Alignment with the footnote "The first Alignment of Mapai and Ahdut Avoda". Shituf Ve`ahvah's 16,464
votes are 1.36% of valid votes (computed here); K-RES-6 prints 1.3.

### 7th Knesset, election of 28 October 1969

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 17 Nov 1969 | WP |
|---|---|---|---|---|---|---|
| Alignment | מערך | 632,035 | 46.2 | 56 | 56 | agree |
| Gahal | גוש חרות ליברלים | 296,294 | 21.7 | 26 | 26 | agree |
| National Religious Party | מפלגה דתית לאומית | 133,238 | 9.7 | 12 | 12 | agree |
| Agudat Yisrael | אגודת ישראל | 44,002 | 3.2 | 4 | 4 | agree |
| Independent Liberals | ליברלים עצמאיים | 43,933 | 3.2 | 4 | 4 | agree |
| State List | הרשימה הממלכתית | 42,654 | 3.1 | 4 | 4 | agree (WP: "National List") |
| Rakah | רשימה קומוניסטית חדשה | 38,827 | 2.8 | 3 | 3 | agree |
| Kidmah Ufituah* | קידמה ופיתוח | 28,046 | 2.1 | 2 | 2 | agree |
| Po'alei Agudat Yisrael | פועלי אגודת ישראל | 24,968 | 1.9 | 2 | 2 | agree |
| Shituf Ve`ahvah* | שיתוף ואחווה | 19,943 | 1.4 | 2 | 2 | agree |
| Ha'olam Hazeh-Koah Hadash | העולם הזה - כוח חדש | 16,853 | 1.2 | 2 | 2 | agree |
| Hamerkaz Hahofshi | מרכז חופשי (Free Center) | 16,393 | 1.2 | 2 | 2 | agree |
| Maki | מפלגה קומוניסטית ישראלית | 15,712 | 1.1 | 1 | 1 | agree |

Computed here: Po'alei Agudat Yisrael's 24,968 votes are 1.83% (printed 1.9) and Shituf Ve`ahvah's 19,943 are 1.46%
(printed 1.4). The seats are not affected.

### 8th Knesset, election of 31 December 1973

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 21 Jan 1974 | WP |
|---|---|---|---|---|---|---|
| Alignment | מערך | 621,183 | 39.6 | 51 | 51 | agree |
| Likud | הליכוד | 473,309 | 30.2 | 39 | 39 | agree |
| National Religious Party | מפלגה דתית לאומית | 130,349 | 8.3 | 10 | 10 | agree |
| United Torah Front | חזית דתית תורתית | 60,012 | 3.8 | 5 | 5 | agree (WP: "Religious Torah Front") |
| Independent Liberals | ליברלים עצמאיים | 56,560 | 3.6 | 4 | 4 | agree |
| Rakah | הרשימה הקומוניסטית החדשה | 53,353 | 3.4 | 4 | 4 | agree |
| Ratz | ר"ץ (Civil Rights Movement) | 35,023 | 2.2 | 3 | 3 | agree |
| Kidmah Ufituah* | קידמה ופיתוח | 22,604 | 1.4 | 2 | 2 | agree |
| Moked | מוקד | 22,147 | 1.4 | 1 | 1 | agree |
| Arab list for Bedouins and Villagers* | רשימה ערבית לבדואים וכפריים | 16,408 | 1.0 | 1 | 1 | agree |

## 3. The mergers #114 names

Each row is dated from K-MS and OD-F, which agree on every date below [V]. "Seats" is counted here from OD-P on that
day.

| Merger | Date | What the Knesset sources say | Seats | Founding dates in Wikipedia [S] |
|---|---|---|---|---|
| The Liberal Party (General Zionists and Progressives) | 8 May 1961 (4th Knesset) | K-MS: "The General Zionists and Progressive Party united and formed the Liberal Party of Israel." OD-F: faction מפלגה ליברלית ישראלית from 8 May 1961; the two factions end that day | 14 (8 + 6) | 8 May 1961 |
| The first Alignment (Mapai and Ahdut HaAvoda) | ran as one list in 1965; faction מערך from 22 Nov 1965 | K-RES-6 list "Alignment"; K-HIST-6 note 1 "The first Alignment of Mapai and Ahdut Avoda". In the Fifth Knesset the two factions stayed separate to the end of the term (OD-F: both to 22 Nov 1965), so K-MS records no Fifth Knesset merger | 45 at the first sitting | "1965", "formed to contest the 1965 Knesset election" (WP-PARTY, Alignment). The date of the agreement is [GAP] |
| Gahal (Herut and the Liberals) | 25 May 1965 (5th Knesset) | K-MS: "The Herut Movement and the Liberal Party of Israel merged and formed the Herut-Liberal Bloc (Gahal)." OD-F: גוש חרות ליברלים from 25 May 1965; Herut and the Liberal faction end that day | 27 (17 + 10) | 25 May 1965 |
| The Israel Labor Party (the Alignment and Rafi) | 23 Jan 1968 (6th Knesset) | K-MS: "Rafi (all members except David Ben Gurion) and Alignment merged and formed the Israel Labor Party." OD-F: מפלגת העבודה הישראלית from 23 Jan 1968; Ben-Gurion sits alone from the same day | 54 (45 + 9); Ben-Gurion 1 | 23 Jan 1968 |
| The second Alignment (Labor and Mapam) | 28 Jan 1969 (6th Knesset) | K-MS: "Israel Labor Party and United Worker's Party (Mapam) merged and formed a new party called the Alignment." OD-F: מערך from 28 Jan 1969; Labor and Mapam factions end | 63 (Labor 55 with Yizhar Harari, who moved from the Independent Liberals on 27 May 1968, plus Mapam 8) | "an alliance with Mapam on 28 January 1969"; the Alignment "had 63 of 120 Knesset seats" (WP-PARTY, Alignment) |
| Likud | list of 1973; faction הליכוד from 21 Jan 1974 | K-RES-8 list "Likud". In the Seventh Knesset, Gahal, the Free Center and the State List stayed separate factions to the end of the term (OD-F), so K-MS records no Seventh Knesset merger | 39 at the first sitting | "formed on 13 September 1973" by Herut, the Liberal Party, the Free Centre, the National List and the Movement for Greater Israel (WP-PARTY, Likud, citing Rabinovich and Reinharz, *Israel in the Middle East*, p. 462, and *Davar* of 14 Sep 1973). Not read in a primary source [GAP] |

Arithmetic, computed here from OD-P: Labor's 54 on 23 Jan 1968 equals the Alignment's 45 plus 9 of Rafi's 10. The
second Alignment's 63 on 28 Jan 1969 equals 55 plus 8; K-HIST-6 also ends the term with the Alignment at 63.

The other splits the issue mentions: the Independent Liberals left the Liberal Party on 16 Mar 1965, before Gahal was
formed (K-MS: the seven "who were in the Progressive Party in the past ... left and formed the Independent Liberals")
[V], so the Liberal faction went into Gahal with 10 (K-HIST-5 note 2: "10 members formed Gahal with the Herut
Movement, the other 7 formed the Independent Liberals") [V]. Rafi left Mapai on 14 Jul 1965 with eight members, whom
K-MS names, Ben-Gurion, Dayan and Peres among them [V]. Rafi ran separately in 1965 and won 10 seats [V] K-RES-6.

## 4. Changes during each term

Dates from K-MS and OD-F, which agree unless a row says otherwise. "Seats after" is counted here from OD-P on that day.
Moves by single members that did not change a faction are not listed; members who left the Knesset and were replaced
are not moves.

### 3rd Knesset, 1957 to 1959 (the part inside this era)

| Date | Change | Source |
|---|---|---|
| 22 Jan 1957 | Poel Mizrahi - Mizrahi renamed the National Religious Party | [V] K-MS; OD-F (מפלגה דתית לאומית from 22 Jan 1957) |
| 31 Dec 1957 | Ben-Gurion resigns "against the background of the leaking of information from government meetings" | [V] K-HIST-3 |
| 7 Jan 1958 | Ben-Gurion forms the 8th government: Mapai, NRP, Mapam, Ahdut Ha'avoda, the Progressive Party and the minority lists; 16 ministers | [V] K-HIST-3 |
| 5 Jul 1959 | Ben-Gurion resigns after "the members of Ahdut Ha'avoda and Mapam" vote against the government on arms sales to West Germany and refuse to resign | [V] K-HIST-3 (paraphrased) |

### 4th Knesset (30 November 1959 to 4 September 1961)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 17 Dec 1959 | Ben-Gurion forms the 9th government: Mapai, NRP, Mapam, Ahdut Ha'avoda, the Progressive Party, the minority lists "and one non-partisan member"; 16 ministers | - | [V] K-HIST-4 |
| 9 Aug 1960 | The Religious Torah Front splits into Agudat Yisrael and Po'alei Agudat Yisrael | Agudat 3, PAI 3 | [V] K-MS, OD-F, OD-P. K-HIST-4 gives 4 and 2 at the end of the term; OD-P shows one PAI member's term ending on 30 May 1961 and an Agudat member starting that day |
| 31 Jan 1961 | Ben-Gurion resigns (the Lavon Affair; §1.1) | - | [V] K-HIST-4 |
| 28 Mar 1961 | Dissolution law on the agenda for third reading | - | [V] OD-I |
| 8 May 1961 | The Liberal Party (see §3) | Liberal 14 | [V] |
| end of term | K-HIST-4 "Before Next Elections": Mapai 47, Herut 17, Liberal 14, NRP 12, Mapam 9, Ahdut Ha'avoda 7, Agudat 4, Maki 3, PAI 2, the three minority lists 2, 2 and 1 | 120 (computed here) | [V] K-HIST-4 |

### 5th Knesset (4 September 1961 to 22 November 1965)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 2 Nov 1961 | Ben-Gurion forms the 10th government: Mapai, NRP, Ahdut Ha'avoda, Po'alei Agudat Yisrael and the minority lists; 15 ministers | - | [V] K-HIST-5. WP-1961 says 13 ministers [S, C7] |
| 16 Jun 1963 | Ben-Gurion resigns "for 'personal reasons'" | - | [V] K-HIST-5 |
| 26 Jun 1963 | Eshkol forms the 11th government, same coalition; 16 ministers | - | [V] K-HIST-5 |
| 15 Dec 1964 | The government resigns over Ben-Gurion's demand for a judicial inquiry into the Lavon Affair | - | [V] K-HIST-5. WP-1961 gives 10 Dec 1964 [S, C7] |
| 22 Dec 1964 | Eshkol forms the 12th government, same coalition; 16 ministers. Yona Kesse leaves Mapai and sits alone; K-MS: the House Committee "did not approve his becoming a single member parliamentary group" by the name "Min Hayesod" | Mapai 41 | [V] K-HIST-5, K-MS, OD-F, OD-P |
| 16 Mar 1965 | The Independent Liberals leave the Liberal Party (§3) | IL 7, Liberal 10 | [V] |
| 25 May 1965 | Gahal (§3) | Gahal 27 | [V] |
| 14 Jul 1965 | Rafi leaves Mapai (§3) | Rafi 8, Mapai 33 | [V] |
| 1 Sep 1965 | Emil Habibi and Tawfik Toubi leave Maki and form the New Communist List (Rakah) | Rakah 2, Maki 3 | [V] K-MS, OD-F, OD-P |
| end of term | K-HIST-5: Mapai 34, Gahal 27, NRP 12, Mapam 9, Ahdut Ha'avoda 8, Rafi 8, IL 7, Agudat 4, Maki 3, PAI 2, Rakah 2, the two minority lists 2 each | 120 (computed here) | [V] K-HIST-5; OD-P gives Mapai 32 plus Kesse alone (C4) |

### 6th Knesset (22 November 1965 to 17 November 1969)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 12 Jan 1966 | Eshkol forms the 13th government: the Alignment, NRP, Mapam, the Independent Liberals, Po'alei Agudat Yisrael and the minority lists; 18 ministers | - | [V] K-HIST-6 |
| 5 Jul 1966 | The two minority lists form Shituf Ve-Pituach (שיתוף ופיתוח); it splits back on 1 Jan 1967 | 4, then 2 and 2 | [V] K-MS, OD-F |
| 29 Mar 1967 | The Free Center (Shostak, Tamir, Tiar) leaves Gahal | Free Center 3, Gahal 23 | [V] K-MS, OD-F, OD-P |
| 11 Apr 1967 | The Druze Party (Jabr Moade) leaves Shituf Ve-achva | 1 | [V] K-MS, OD-F |
| 5 Jun 1967 | "Gahal and Rafi joined the government"; 21 ministers in the national unity government | - | [V] K-HIST-6 ("On June 5, 1967 (the day of the outbreak of the Six Day War)"). The design's 1 Jun 1967 is checked in §8 |
| 23 Jan 1968 | The Israel Labor Party (§3); Ben-Gurion sits alone | Labor 54 | [V] |
| 27 May 1968 | Yizhar Harari leaves the Independent Liberals for Labor | Labor 55, IL 4 | [V] K-MS, OD-P |
| 22 Oct 1968 | The Jewish-Arab Brotherhood (Elias Nahale) leaves Kidma Vepituah. The name it split from is mojibake in K-MS's copy; OD-P shows Nahale in קידמה ופיתוח until that day | 1 | [V] K-MS, OD-F, OD-P |
| 28 Jan 1969 | The second Alignment (§3) | Alignment 63 | [V] |
| 11 Feb 1969 | Shlomo Cohen-Tsiddon leaves Gahal for the Free Center | Free Center 4, Gahal 22 | [V] K-MS, OD-P |
| 26 Feb 1969 | Eshkol dies; the government ends | - | [V] K-HIST-6 |
| 17 Mar 1969 | Golda Meir forms the 14th government: the Alignment, Gahal, NRP, the Independent Liberals and the minority lists; 21 ministers | - | [V] K-HIST-6 |
| end of term | K-HIST-6: Alignment 63, Gahal 22, NRP 11, Free Center 4, IL 4, Agudat 4, Rakah 3, PAI 2, Kidma Vepituah 1, Shituf Ve-achva 1, Ha-olam Hazeh 1, Maki 1, Jewish-Arab Brotherhood 1, Druze Party 1, Ben-Gurion 1 | 120 (computed here) | [V] K-HIST-6; OD-P on 15 Nov 1969 agrees |

### 7th Knesset (17 November 1969 to 21 January 1974)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 15 Dec 1969 | Meir forms the 15th government, "as in the fourteenth government"; 24 ministers | - | [V] K-HIST-7 |
| 6 Aug 1970 | Gahal resigns over the government's acceptance of the Rogers Plan; ministers go down to 22 | - | [V] K-HIST-7 |
| 4 Jan 1972 | Shalom Cohen leaves Ha-olam Hazeh - Koah Hadash and sits alone | 1 | [V] K-MS, OD-F |
| 25 Apr 1972 | Meir Avizohar leaves the State List (K-MS: "National List"; C6) and sits alone; he joins the Alignment on 17 Jul 1973 | State List 3; Alignment 57 from 17 Jul 1973 | [V] K-MS, OD-F, OD-P |
| 22 May 1973 | Avner-Hai Shaki leaves the NRP and sits alone | NRP 11 | [V] K-MS, OD-F |
| 3 Jul 1973 | Ha-olam Hazeh - Koah Hadash renamed the Radical Camp of Israel (מחנה רדיקלי ישראלי) | 1 | [V] K-MS, OD-F; K-HIST-7 note 4 gives the new name as "Meri" (C5) |
| 25 Jul 1973 | The Israel Communist Party renamed Moked | 1 | [V] K-MS, OD-F |
| 25 Oct 1973 | The election is postponed to 31 Dec 1973 | - | [V] L-PP-8 |
| end of term | K-HIST-7: Alignment 57, Gahal 26, NRP 11, Agudat 4, IL 4, State List 3, Rakah 3, PAI 2, Free Center 2, the two minority lists 2 each, Maki 1, Shaki 1, Shalom Cohen 1, Ha-olam Hazeh 0 | 119 (computed here; C5) | [V] K-HIST-7 |

### 8th Knesset (21 January 1974 to 13 June 1977)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 26 Feb 1974 | Kidma Vepituah and the Arab List for Bedouin and Villagers join the Alignment | Alignment 54 | [V] K-MS, OD-F |
| 10 Mar 1974 | Meir forms the 16th government: the Alignment, NRP and the Independent Liberals; 22 ministers. She resigns on 11 April after the Agranat Commission's interim report | - | [V] K-HIST-8 |
| 3 Jun 1974 | Rabin forms the 17th government: the Alignment, Ratz, the Independent Liberals and the minority lists; 19 ministers. The NRP joins on 30 October and Ratz leaves on 6 November 1974 (21 ministers) | - | [V] K-HIST-8 |
| 18 Mar 1975 | Benjamin Halevi leaves the Likud and sits alone | Likud 38 | [V] K-MS, OD-F |
| 29 Apr 1975 | Arie Lova Eliav leaves the Alignment and sits alone | - | [V] K-MS |
| 3 Jun 1975 | Ratz and Eliav form Ya'ad - Movement for Civil Rights | Ya'ad 4, Alignment 53 | [V] K-MS, OD-F, OD-P |
| 21 Oct 1975 | Moked - Maki and Tchelet Adom renamed Moked - for Peace and Social Compensation | 1 | [V] K-MS |
| 27 Jan 1976 | Eliav and Marcia Freedman leave Ya'ad as the Social-Democratic Faction, renamed the Independent Socialist Faction on 3 Feb 1976; Aloni and Moav go back to the name Ratz | 2 and 2 | [V] K-MS, OD-F, OD-P |
| 8 Jun 1976 | Seif-El-Din El-Zubi and Jabr Moade leave the Alignment as Kidma Vepituah | Kidma 2 | [V] K-MS, OD-F |
| 26 Oct 1976 | Shmuel Tamir and Akiva Nof leave the Likud as the Free Center; K-MS: "The Free Center resigned from the Knesset" on 25 Jan 1977 | Free Center 2, Likud 36 | [V] K-MS, OD-F, OD-P |
| 22 Dec 1976 | The 17th government resigns, after the NRP abstains on a no-confidence motion brought by Agudat Yisrael over a Sabbath ceremony at an air force base and its ministers are removed | - | [V] K-HIST-8 (paraphrased) |
| 4 Jan 1977 | Hamad Abu-Rabiah leaves the Alignment and re-forms the Arab List for Bedouin and Villagers | - | [V] K-MS, OD-F |
| 5 Jan 1977 | Dissolution of the Eighth Knesset Law on the agenda for third reading (published 10 Jan 1977, Sefer HaHukim 838, p. 50); election 17 May 1977 | - | [V] OD-I, OD-B; the 1977-93 note's L-DISS-8 gives the law's text |
| 15 Feb 1977 | Hillel Seidel leaves the Independent Liberals for the Likud | IL 3 | [V] K-MS, OD-P |
| 8 Mar 1977 | The two Arab lists form the United Arab List | 3 | [V] K-MS, OD-F, OD-P |
| 15 Mar 1977 | Rakah's faction becomes the Democratic Front for Peace and Equality (Hadash); the Torah Front splits into Agudat Yisrael and Po'alei Agudat Yisrael; Mordechai Ben-Porat leaves the Alignment and sits alone | Hadash 4, Agudat 3, PAI 2 | [V] K-MS, OD-F, OD-P |
| 10 to 12 Apr 1977 | Six Mapam members leave the Alignment as Mapam and return two days later | - | [V] K-MS, OD-F |
| end of term | K-HIST-8: Alignment 49, Likud 40, NRP 10, Rakah/Hadash 4, IL 3, United Arab List 3, Agudat 3, Ratz 2, Social-Democratic Faction 2, PAI 2, Moked 1, Ben-Porat 1 | 120 (computed here) | [V] K-HIST-8; OD-P on 12 Jun 1977 gives Alignment 48 and 119 members, our reading one vacant seat |

## 5. How each budget of 1957 to 1977 was approved

Every annual budget of the era was a Budget Law passed by the Knesset plenum: OD-B lists each as passed third reading
(StatusID 118), and OD-I lists each on the agenda for first-reading debate, then for second-third and third reading,
the procedure `budget-approval-1948-56.md` read in the protocols for 1949 to 1957 [V, single source: OData, read through
the mirror]. Our reading from the laws' names: the fiscal year ran April to March up to 1971/72 (for example "חוק התקציב לשנת 1960/61").
From 1972 the laws are named for one year ("חוק התקציב לשנת הכספים 1972", Budget Law for the fiscal year 1972); an
interim law passed on 21 Mar 1972 suggests the 1972 year still began on 1 April (our reading; the law's section 1 was
not read [GAP]).

What is open for every year: the vote tallies, and the adoption footnote in each law. The law texts and the protocols
are on fs.knesset.gov.il, which this machine could not reach, and none is in the Internet Archive (checked for every
file listed below) [GAP]. The "passed" date below is the sitting at which OD-I lists the bill for third reading (status
117); that this is the day of adoption is our reading. For the one law of this era whose text was read (L-PP-8, an
election law) the same method gives the day in the law's own footnote.

| Fiscal year | Annual Budget Law (Hebrew name as in OD-B) | Passed (OD-I, third reading) | Knesset, sitting | Published (OD-B: *Hukei Taktziv* issue, page, date) | Interim and supplementary laws in that year (OD-B, OD-I: third-reading date) |
|---|---|---|---|---|---|
| 1957/58 | חוק התקציב לשנת 1957/58 | 11 Apr 1957 | 3rd, 290 | 18, p. 4, 10 Jun 1957 | Interim law for 1957/58, 28 Mar 1957; (No. 2) 14 Jan 1958; (No. 3) 4 Feb 1958; Development Authority budget law for 1957/58, 31 Jul 1957 |
| 1958/59 | חוק התקציב לשנת 1958/59 | 31 Mar 1958 | 3rd, 452 | 22, p. 2, 29 May 1958 | (No. 2) 26 Jan 1959 |
| 1959/60 | חוק התקציב לשנת 1959/60 | 1 Apr 1959 | 3rd, 627 | 24, p. 2, 31 May 1959 | Amendment, 9 Feb 1960 |
| 1960/61 | חוק התקציב לשנת 1960/61 | 29 Jun 1960 | 4th, 125 | 28, p. 209, 10 Aug 1960 | Budget Law (April-June 1960), 28 Mar 1960 |
| 1961/62 | חוק התקציב לשנת 1961/62 | 28 Mar 1961 | 4th, 265 | 29, p. 3, 29 May 1961 | (No. 2) 7 Feb 1962; (No. 3) 28 Mar 1962 (5th Knesset) |
| 1962/63 | חוק התקציב לשנת 1962/63 | 1 Aug 1962 | 5th, 167 | 34, p. 229, 11 Sep 1962 | Budget Law Adar II-Tammuz 5722 (April-July 1962), 27 Mar 1962, amended 9 Apr 1962; amendment, 27 Nov 1962 |
| 1963/64 | חוק התקציב לשנת 1963/64 | 27 Mar 1963 | 5th, 238 | 36, p. 3, 30 Apr 1963 | (No. 2) 17 Dec 1963 |
| 1964/65 | חוק התקציב לשנת 1964/65 | 23 Mar 1964 | 5th, 348 | 38, p. 3, 30 Apr 1964 | (No. 2) 7 Dec 1964 |
| 1965/66 | חוק התקציב לשנת 1965/66 | 30 Mar 1965 | 5th, 466 | 40, p. 3, 28 May 1965 | none in OD-B |
| 1966/67 | חוק התקציב לשנת 1966/67 | 30 Mar 1966 | 6th, 55 | 41, p. 3, 27 May 1966 | none in OD-B |
| 1967/68 | חוק התקציב לשנת 1967/68 | 29 Mar 1967 | 6th, 167 | 42, p. 3, 25 May 1967 | Amendment and (No. 2), 10 Jul 1967; (No. 3) 25 Sep 1967. OD-I also lists the annual law for second-third reading on 20 Jun 1967, after its publication; not explained [GAP] |
| 1968/69 | חוק התקציב לשנת 1968/69 | 27 Mar 1968 | 6th, 274 | 46, p. 3, 23 May 1968 | (No. 2) 17 Jun 1968; (No. 3) 30 Jul 1968 |
| 1969/70 | חוק התקציב לשנת 1969/70 | 25 Mar 1969 | 6th, 394 | 49, p. 3, 21 May 1969 | (No. 2) 16 Dec 1969 and (No. 3) 26 Mar 1970 (7th Knesset) |
| 1970/71 | חוק התקציב לשנת 1970/71 | 14 Apr 1970 | 7th, 65 | 52, p. 3, 1 Jun 1970 | Interim law for 1970/71, 26 Mar 1970; (No. 2) and (No. 3), 1 Sep 1970 |
| 1971/72 | חוק התקציב לשנת 1971/72 | 25 Mar 1971 | 7th, 172 | 54, p. 3, 16 May 1971 | (No. 2) 8 Feb 1972 |
| 1972 | חוק התקציב לשנת הכספים 1972 | 28 Jun 1972 | 7th, 322 | 58, p. 3, 10 Aug 1972 | Interim law for 1972, 21 Mar 1972; (No. 2) 2 Jan 1973 |
| 1973 | חוק התקציב לשנת הכספים 1973 | 28 Mar 1973 | 7th, 411 | 60, p. 3, 24 May 1973 | (No. 2) 16 Oct 1973, during the war; (No. 3) 18 Mar 1974 (8th Knesset) |
| 1974 | חוק התקציב לשנת הכספים 1974 | 26 Jun 1974 (status 117); OD-I lists it again for second-third reading on 1 Jul 1974 [GAP] | 8th, 64 | 63, p. 3, 25 Aug 1974 | Interim law for 1974, 27 Mar 1974; (No. 2) 18 Feb 1975 |
| 1975 | חוק התקציב לשנת הכספים 1975 | 30 Jun 1975 | 8th, 192 | 66, p. 5, 27 Aug 1975 | Interim law for 1975, 20 Mar 1975; (No. 2) 23 Feb 1976 |
| 1976 | חוק התקציב לשנת הכספים 1976 | 29 Jun 1976 | 8th, 311 | 69, p. 3, 9 Aug 1976 | Interim law for 1976, 30 Mar 1976 |
| 1977 | חוק התקציב לשנת הכספים 1977 | 24 Aug 1977 | 9th, 23 | 72, p. 3, 10 Oct 1977 | Budget Law for the transition period April-July 1977, 16 Mar 1977 (8th Knesset, after its dissolution law), amended 18 Jul 1977 (9th Knesset); (No. 2) 14 Mar 1978 |

Our reading of the table (counted here): in 11 of the 21 years the annual law passed before 1 April, and in 1959/60 on
1 April itself. In the other nine (1957/58, 1960/61, 1962/63, 1970/71, 1972, 1974, 1975, 1976 and 1977) the year began
on an interim law and the annual law passed between April and August.
The 1977 budget was passed by the Ninth Knesset, so it belongs to the next chapter's government; the outgoing Eighth
Knesset passed only the April-July transition law.

The first-reading stage is also in OD-I, for the record: for example the 1957/58 bill was laid on the table on 7 Jan
1957 (sitting 217) and listed for first-reading debate at 36 sittings from 14 Jan to 9 Apr 1957. The second-third
reading ran over many sittings in the early years (28 sittings from 26 Feb to 31 Mar 1958 for 1958/59; 38 from 15 Mar
to 29 Jun 1960 for 1960/61) and over one sitting from 1975 (computed here from OD-I). That the long runs are ministry
by ministry debates is our reading, from the 1948-56 note's description of the same procedure.

Files that would close the gaps (OD-DOC; all on fs.knesset.gov.il, none archived): the annual laws are
`3/law/3_lsr_562410.pdf` (1957/58), `3_lsr_562443` (1958/59), `3_lsr_562444` (1959/60), `4/law/4_lsr_562447` (1960/61),
`4_lsr_562464` (1961/62), `5/law/5_lsr_562501` (1962/63), `5_lsr_562515` (1963/64), `5_lsr_562548` (1964/65),
`5_lsr_562549` (1965/66), `6/law/6_lsr_562754` (1966/67), `6_lsr_562757` (1967/68), `6_lsr_562756` (1968/69),
`6_lsr_562755` (1969/70), `7/law/7_lsr_562758` (1970/71), `7_lsr_562761` (1971/72), `7_lsr_562760` (1972),
`7_lsr_562759` (1973), `8/law/8_lsr_562762` (1974), `8_lsr_562763` (1975), `8_lsr_562685` (1976) and
`9/law/9_lsr_562689` (1977), each `.pdf`. The protocols of the third-reading sittings are `3/Plenum/3_ptm_251080.pdf`
(1957/58), `3_ptm_250925` (1958/59), `3_ptm_251189` (1959/60), `4/Plenum/4_ptm_251604` (1960/61), `4_ptm_251735`
(1961/62), `5/Plenum/5_ptm_252268` (1962/63), `5_ptm_252049` (1963/64), `5_ptm_252083` (1964/65), `5_ptm_251987`
(1965/66), `6/Plenum/6_ptm_252493` (1966/67), `6_ptm_252524` (1967/68), `6_ptm_252437` (1968/69), `6_ptm_252589`
(1969/70), `7/Plenum/7_ptm_252847` (1970/71), `7_ptm_252996` (1971/72), `7_ptm_253039` (1972), `7_ptm_253080` (1973),
`8/Plenum/8_ptm_253457` (1974), `8_ptm_253506` (1975), `8_ptm_253555` (1976) and `9/Plenum/9_ptm_253922` (1977).

## 6. Conflicts

- **C1. The 1965 election date.** K-RES-6 ("1 November, 1965") and K-HIST-6 ("Elected on 1 November, 1965.") give 1
  November. JTA-660112, written two months later, refers to "the November 2 general elections", and WP-1965 gives 2
  November 1965 [S]. Computed here: 2 November 1965 was a Tuesday, the weekday of the 1959, 1961 and 1969 elections, and
  1 November was a Monday. Preferred: 2 November 1965, from the contemporary report, with the Knesset's 1 November
  kept as the conflicting value. A primary record (the Central Elections Committee's results notice in Reshumot) was
  not reached [GAP].
- **C2. Eligible voters, 1959 to 1969.** K-RES-n: 1,218,724; 1,274,280; 1,499,988; 1,758,685. CEC-PCT: 1,218,483;
  1,271,285; 1,499,709; 1,748,710. CEC-BF (from 1965, rounded): 1,500,000 and 1,758,000, which fits the Knesset's 1969
  figure, not CEC-PCT's. 1973 agrees in all (2,037,478). WP-n use the CEC-PCT figures, except WP-1965, which gives
  1,449,709 and a turnout of 85.86%; computed here, 1,244,706 votes over 1,449,709 is 85.86% and over 1,499,709 is
  83.00%, so WP-1965's electorate looks like a typing slip. No preference between the two Knesset and CEC figures;
  both are given.
- **C3. Turnout, 1961.** CEC-PCT: 83.0%. Computed here from WP-1961's valid and invalid votes (1,037,030) over
  CEC-PCT's own electorate: 81.57%; WP-1961 also gives 81.57%. CEC-PCT prints 83.0 for 1965 as well. Preferred: 81.6%,
  because it is the only figure consistent with the votes, with 83.0% kept as CEC-PCT's printed value. The
  preference rests on WP-1961's invalid-vote count, which is [S].
- **C4. Mapai at the end of the Fifth Knesset.** K-HIST-5: 34. OD-P on 20 Nov 1965: Mapai 32, Yona Kesse alone, and
  119 members in all. Our reading: K-HIST-5 still counts Kesse, whose single-member group the House Committee did not
  approve (K-MS), and one vacant seat under Mapai.
- **C5. The Seventh Knesset's end column.** K-HIST-7's "Before Next Elections" column sums to 119 (computed here):
  Ha-olam Hazeh - Koah Hadash goes from 2 to 0, with note 4 "Changed its name to "Meri". MK Shalom Cohen split off and remained a single MK.",
  but no row for the renamed one-member faction. K-MS and OD-F name it the Radical Camp of Israel from 3 Jul 1973, with
  one member (OD-P). The 120th seat is that faction; the "Meri" name is in K-HIST-7 only.
- **C6. Naming only.** The Torah list: K-RES-4 "Religious Torah Front", K-HIST-4 "United Torah Front", K-RES-8 and
  K-HIST-8 "United Torah Front", OD-F חזית דתית תורתית, WP "Religious Torah Front". The 1969 list: K-RES-7 "State List",
  K-HIST-7 "National List", OD-F הרשימה הממלכתית. The 1976 faction: K-HIST-8 "Social-Democratic Faction"; K-MS and OD-F
  rename it the Independent Socialist Faction on 3 Feb 1976.
- **C7. Two government details, Wikipedia against the Knesset.** WP-1961: the 10th government "had 13 ministers"
  and the 11th "resigned on 10 December 1964"; K-HIST-5: 15 ministers and 15 December 1964. Preferred: K-HIST-5. The
  offices work owns the governments; they are here only because faction changes refer to them.

## 7. Gaps

- The Central Elections Committee's per-list results for 1959 to 1973, and votes for the lists that won no seat (only
  [S], WP-n).
- The text of Basic Law: The Knesset (1958) and of the Dissolution of the Fourth Knesset Law (1961): which date each
  fixed.
- A primary date for the 1965 election (C1), for the founding of the first Alignment as a list, and for the founding of
  the Likud (13 Sep 1973 is [S]).
- Every budget vote tally and every budget law's adoption footnote, 1957 to 1977 (§5): fs.knesset.gov.il is blocked
  from this machine and the files are not archived. A contributor inside Israel can read them from the list in §5.
- What the 1972 to 1977 fiscal years covered, from the laws' section 1.
- Why OD-I lists the 1967/68 and 1974 laws for second-third reading after their third-reading sitting.
- The OData tables were read through the Open Knesset mirror, not the Knesset's own service; the counts in OD-P are
  from Open Knesset's member table built from the Knesset's.

## 8. Checked against the 1957-1977 design

Checked against `docs/superpowers/specs/2026-10-10-era-1957-1977-design.md` §2.1 and §5.2:

- Agree: the 1959, 1961, 1969 and 1973 election dates; the 1973 postponement from 30 October; the seat counts given
  there (Mapai 47 and 42; Alignment 45, Gahal 26, Rafi 10; Alignment 56, Gahal 26; Alignment 51, Likud 39); Labor on
  23 January 1968 and the Alignment with Mapam on 28 January 1969; Gahal's exit on 6 August 1970 (K-HIST-7).
- Differs: the design dates the 1965 election 2 November 1965 from Wikipedia; the Knesset says 1 November (C1, which
  prefers 2 November). The design dates the national unity government 1 June 1967 and marks Gahal's and Rafi's entry on
  5 June *to verify*; K-HIST-6 gives 5 June 1967 for their joining. The 1 June date is not in any Knesset source read.
- The design's §5.2 lists "Gahal in 1965" and "Likud in 1973" as mergers inside a Knesset. The sources put Gahal's
  merger inside the Fifth Knesset on 25 May 1965, but the Likud has no faction before the Eighth Knesset: in the
  Seventh Knesset Gahal, the Free Center and the State List stayed separate factions to its end (OD-F). Whether the
  Likud needs a record before 21 January 1974 is a content decision for the paused `legislatures.yaml` part.
