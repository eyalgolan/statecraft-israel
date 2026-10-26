# The Ninth to Thirteenth Knessets: elections, results and factions, 1977 to 1993

Researched 13 October 2026. Every URL below was opened or queried on that date. Research note for issue #146 (part of
#51). Nothing in `content/` is changed by this note.

## Purpose

Fix, from primary sources, the facts that the 1977–1993 design (§12 item 2) needs for one `legislatures.yaml` record
per Knesset (9th to 13th) and a new record at each sourced merger or split: election dates, first sittings, the
threshold, the seats per list, and the faction changes during each term, with Kach's disqualification in 1988.

`content/legislatures.yaml` currently stops at `third_knesset` (from 15 August 1955).

## Conventions

- [V] read on a primary source opened this session (source key or URL given).
- [S] single secondary source, usually Wikipedia, not confirmed in a primary source.
- [C] conflict. Every value is given with its source.
- [GAP] not found.
- Dates are written 17 May 1977 style. Hebrew faction names are copied from the Knesset's database and glossed in
  English. No quotation is used.

## Sources and how they were read

Every request used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

The Knesset's HTML pages (knesset.gov.il, main.knesset.gov.il, m.knesset.gov.il) return HTTP 247 with a bot challenge to
a script, so they were read as Internet Archive copies. The Knesset's OData service and its file server
(fs.knesset.gov.il) answered directly.

| Key | Source | Note |
|---|---|---|
| K-RES-9 … K-RES-13 | Knesset, "Knesset Elections Results", one page per Knesset. Live: `https://www.knesset.gov.il/description/eng/eng_mimshal_res9.htm` (…res10 … res13). Read as archive copies: [9](https://web.archive.org/web/20160817093422/http://knesset.gov.il/description/eng/eng_mimshal_res9.htm), [10](https://web.archive.org/web/20160817111402/http://knesset.gov.il/description/eng/eng_mimshal_res10.htm), [11](https://web.archive.org/web/20160817160844/http://knesset.gov.il/description/eng/eng_mimshal_res11.htm), [12](https://web.archive.org/web/20160817093719/http://knesset.gov.il/description/eng/eng_mimshal_res12.htm), [13](https://web.archive.org/web/20160817100011/http://knesset.gov.il/description/eng/eng_mimshal_res13.htm) | Eligible voters, valid votes, threshold, votes per seat, and votes, percent and seats for each list that won seats. Lists without seats are not shown |
| K-HIST-9 … K-HIST-13 | Knesset, "Factional and Government Make-Up of the … Knesset". Archive copies: [9](https://web.archive.org/web/20160823201631/http://knesset.gov.il/history/eng/eng_hist9_s.htm), [10](https://web.archive.org/web/20160823201148/http://knesset.gov.il/history/eng/eng_hist10_s.htm), [11](https://web.archive.org/web/20131110113800/http://www.knesset.gov.il/history/eng/eng_hist11_s.htm), [12](https://web.archive.org/web/20160823162117/http://knesset.gov.il/history/eng/eng_hist12_s.htm), [13](https://web.archive.org/web/20160819062110/http://knesset.gov.il/history/eng/eng_hist13_s.htm) | Seats at election and "before next elections", footnotes on each move, and the governments with their formation dates. The 2016 copy of the 11th page is the bot challenge; the 2013 copy is used |
| OD-F | Knesset OData, factions: `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_Faction?$filter=KnessetNum%20ge%208%20and%20KnessetNum%20le%2014` (two pages, 173 rows) | Every faction with its start and finish date |
| OD-P | Knesset OData, members' faction terms: `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition?$filter=KnessetNum%20ge%209%20and%20KnessetNum%20le%2013%20and%20FactionID%20ne%20null` (821 rows, all PositionID 54, member of Knesset) | Counted here: members sitting in each faction on a given day |
| OD-D | Knesset OData, session dates: `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_KnessetDates` | First day of each Knesset's sessions |
| OD-B | Knesset OData, laws passed: `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_Bill?$filter=KnessetNum%20eq%209%20and%20StatusID%20eq%20118` (and 8, 10–13). StatusID 118 is "passed third reading" (KNS_Status) | Used to find the dissolution and election laws; their texts are on fs.knesset.gov.il (law keys below) |
| CEC-PCT | Central Elections Committee, "Voting Percentages in Past Elections", [archive copy of 12 January 2013](https://web.archive.org/web/20130112021104/http://www.bechirot.gov.il/elections19/eng/history/PercentVotes_eng.aspx) | Eligible voters and turnout per Knesset |
| CEC-BF | Central Elections Committee, "Comparative data from elections to the Sixth Knesset through today", [archive copy of 12 January 2013](https://web.archive.org/web/20130112021057/http://www.bechirot.gov.il/elections19/eng/history/BasicFacts_eng.aspx) | Eligible voters, polling stations, committee budget |
| EA-1-88 | Supreme Court, Election Appeal 1/88, *Neiman and Kach v. Chairman of the Central Elections Committee for the Twelfth Knesset*, English translation on Versa (Cardozo Law School): https://versa.cardozo.yu.edu/opinions/kach-v-central-election-committee-twelfth-knesset | Kach's disqualification |
| WP-1977 … WP-1992 | English Wikipedia, "1977 Israeli legislative election" … "1992 Israeli legislative election" (raw wikitext) | Comparison only. Their results tables cite the Israel Democracy Institute and Nohlen |
| HE-9 … HE-13 | Hebrew Wikipedia, "הכנסת התשיעית" … "הכנסת השלוש עשרה" | Comparison only |

The Central Elections Committee's current site (bechirot.gov.il) serves only recent elections; `www.bechirot.gov.il/`
returns 404 and the results site found is votes25.bechirot.gov.il (25th Knesset). No CEC per-list results for 1977 to
1992 were found online [GAP]. The per-list results below are the Knesset's own tables, checked against OD-P (who sat in
which faction on the first day) and against Wikipedia.

Law texts (Reshumot, Sefer HaHukim, as scanned and typed on fs.knesset.gov.il):

| Key | Law | Passed | Published | Text |
|---|---|---|---|---|
| L-DISS-8 | Dissolution of the Eighth Knesset Law, 5737-1977. Sets the Ninth Knesset election for Tuesday 17 May 1977 | 5 Jan 1977 | Sefer HaHukim 838, 10 Jan 1977, p. 50 | https://fs.knesset.gov.il//8/law/8_lsr_212032.DOC |
| L-DISS-9 | Dissolution of the Ninth Knesset Law, 5741-1981. Sets the Tenth Knesset election for Tuesday 30 June 1981 | 10 Feb 1981 | Sefer HaHukim 1006, 19 Feb 1981, p. 114 | https://fs.knesset.gov.il//9/law/9_lsr_215745.DOC |
| L-DISS-10 | Dissolution of the Tenth Knesset Law, 5744-1984. Sets the Eleventh Knesset election for Monday 23 July 1984 | 4 Apr 1984 | Sefer HaHukim 1116, 12 Apr 1984, p. 132 | https://fs.knesset.gov.il//10/law/10_lsr_215629.DOC |
| L-EL-12 | Twelfth Knesset Elections (Temporary Provisions) Law, 5748-1988. Names 1 November 1988 as election day | 27 Jul 1988 | Sefer HaHukim 1260, 27 Jul 1988, p. 181 | https://fs.knesset.gov.il//11/law/11_lsr_214604.DOC |
| L-DISS-12 | Dissolution of the Twelfth Knesset Law, 5752-1992. Sets the Thirteenth Knesset election for Tuesday 23 June 1992 | 4 Feb 1992 | Sefer HaHukim 1382, 11 Feb 1992, p. 66 | https://fs.knesset.gov.il//12/law/12_lsr_214878.DOC |
| L-BL-9 | Basic Law: The Knesset (Amendment No. 9): adds section 7A, barring a list whose aims or acts include denying Israel's existence as the state of the Jewish people, denying its democratic character, or incitement to racism | 31 Jul 1985 | Sefer HaHukim 1155, 7 Aug 1985, p. 196 | https://fs.knesset.gov.il//11/law/11_lsr_215616.DOC |
| L-THR | Knesset Elections Law (Amendment No. 23), 5752-1991: in section 81(a), "1%" becomes "1.5%" | 14 Oct 1991 | Sefer HaHukim 1369, 23 Oct 1991, p. 2 | https://fs.knesset.gov.il//12/law/12_lsr_214766.DOC ; bill and explanatory note (Hatza'ot Hok 2065, 8 Jul 1991, p. 270): https://fs.knesset.gov.il//12/law/12_ls1_287873.DOC |

OD-B lists no dissolution law passed by the Eleventh Knesset, so the 1988 election was not brought forward by a
dissolution law. Whether 1 November 1988 was the date the Basic Law fixed for the end of the term: second pass, 25 Oct 2026, sections 8, 9 and 36 of the Basic Law as printed on the Knesset's page, with the calendar arithmetic, give it, as a derived value [derived from sections 8, 9 and 36 as read; a person's check pending]. The sections themselves are [V, agent read 25 Oct 2026; a person's check pending]. See the follow-up, "Second pass, 25 October 2026".

## 1. The five Knessets at a glance

| Knesset | Election | Set by | First sitting | Term ended | Threshold | Eligible voters | Valid votes | Votes per seat | Turnout |
|---|---|---|---|---|---|---|---|---|---|
| 9th | 17 May 1977 | L-DISS-8 | 13 Jun 1977 | 20 Jul 1981 | 1% (17,478 votes) | 2,236,293 | 1,747,820 | 14,173 | 79.2% |
| 10th | 30 Jun 1981 | L-DISS-9 | 20 Jul 1981 | 13 Aug 1984 | 1% (19,373) | 2,490,014 | 1,937,366 | 15,312 | 78.5% |
| 11th | 23 Jul 1984 | L-DISS-10 | 13 Aug 1984 | 21 Nov 1988 | 1% (20,733) | 2,654,613 | 2,073,321 | 16,786 | 78.8% |
| 12th | 1 Nov 1988 | L-EL-12 | 21 Nov 1988 | 13 Jul 1992 | 1% (22,831) | 2,894,267 | 2,283,123 | 18,563 | 79.7% |
| 13th | 23 Jun 1992 | L-DISS-12 | 13 Jul 1992 | 17 Jun 1996 | 1.5% (39,253) | 3,409,015 | 2,616,841 | 20,715 | 77.4% |

Sources, all [V]:

- Election dates: the laws named, and K-RES-n and K-HIST-n ("Elected on …").
- First sitting: OD-D (first PlenumStart: 13 Jun 1977, 20 Jul 1981, 13 Aug 1984, 21 Nov 1988, 13 Jul 1992) and OD-F
  (the factions of each Knesset start on that day).
- Term end: the date the next Knesset's factions start in OD-F, which is the day every faction of the outgoing Knesset
  finishes.
- Threshold, eligible voters, valid votes and votes per seat: K-RES-n.
- Turnout and eligible voters: CEC-PCT. The eligible-voter figures agree with K-RES-n, except for one CEC-BF figure (see
  Conflicts, C1).
- The threshold change: L-THR (the 1.5% applies first in 1992). WP-1992 says "In October 1991, the electoral
  threshold was raised from 1% to 1.5%", which agrees [S, consistent]. WP-1992 also says it was the first change
  since 1949; that was not checked [S].

Wikipedia's turnout figures (79.23%, 78.50%, 78.8%, 79.66%, 77.38%) match the CEC to one decimal place, and the valid
votes in each WP results table add up exactly to the K-RES valid-vote total.

OD-D has only one row for the 9th Knesset (13 June 1977, start and finish the same) and no session rows for 1977–81;
the 13th has a one-day row for 13 July 1992 and then sessions from 26 October 1992. The table is incomplete for these
years, so it is used only for the first day.

Governments formed in these Knessets, as K-HIST-n gives them [V]: 18th, Begin, 20 Jun 1977; 19th, Begin, 5 Aug 1981;
20th, Shamir, 10 Oct 1983; 21st, Peres, 13 Sep 1984; 22nd, Shamir, 20 Oct 1986; 23rd, Shamir, 22 Dec 1988; 24th,
Shamir, 11 Jun 1990; 25th, Rabin, 13 Jul 1992. These match the design's §2.1 dates. The offices note for #145 owns
the governments; they are listed here only because faction changes refer to them.

## 2. Results per list

Each table is K-RES-n [V]. The "OD-P" column is the number of members sitting in that list's faction on the first
sitting day, counted from OD-P [V]. "WP" is the seat count in WP-year. Where every value in a row agrees, the row
says "agree". The Knesset page's spelling of list names is kept, with the Hebrew faction name from OD-F.

### 9th Knesset, election of 17 May 1977

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 13 Jun 1977 | WP |
|---|---|---|---|---|---|---|
| Likud | הליכוד | 583,968 | 33.4 | 43 | 43 | agree |
| Alignment | מערך | 430,023 | 24.6 | 32 | 32 | agree |
| Democratic Movement for Change (Dash) | התנועה הדמוקרטית לשינוי | 202,265 | 11.6 | 15 | 15 | agree |
| National Religious Party | מפלגה דתית לאומית | 160,787 | 9.2 | 12 | 12 | agree |
| Hadash | חזית דמוקרטית לשלום ושוויון | 80,118 | 4.6 | 5 | 5 | agree |
| Agudat Yisrael | אגודת ישראל | 58,652 | 3.3 | 4 | 4 | agree |
| Plato Sharon (Flatto-Sharon) | פלאטו שרון | 35,049 | 2.0 | 1 | 1 | agree |
| Shlomzion | שלומציון | 33,947 | 1.9 | 2 | 2 | agree |
| Mahaneh Sheli (Left Camp) | מחנה של"י | 27,281 | 1.6 | 2 | 2 | agree |
| United Arab List | הרשימה הערבית המאוחדת | 24,185 | 1.4 | 1 | 1 | agree |
| Po'alei Agudat Yisrael | פועלי אגודת ישראל | 23,571 | 1.3 | 1 | 1 | agree |
| Ratz | ר"ץ | 20,621 | 1.2 | 1 | 1 | agree |
| Independent Liberals | ליברלים עצמאיים | 20,384 | 1.2 | 1 | 1 | agree |

K-RES-9 notes that Flatto-Sharon's list won enough votes for two seats but had one candidate, and that the United
Arab List was a minority list associated with the Alignment. Kach ran and won no seat (4,396 votes) [S, WP-1977].

### 10th Knesset, election of 30 June 1981

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 20 Jul 1981 | WP |
|---|---|---|---|---|---|---|
| Likud | הליכוד | 718,941 | 37.1 | 48 | 48 | agree |
| Alignment | מערך | 708,536 | 36.6 | 47 | 47 | agree |
| National Religious Party | חזית דתית לאומית | 95,232 | 4.9 | 6 | 6 | agree |
| Agudat Yisrael | אגודת ישראל | 72,312 | 3.7 | 4 | 4 | agree |
| Hadash | חזית דמוקרטית לשלום ושוויון | 64,918 | 3.4 | 4 | 4 | agree |
| Tehiya | תחייה | 44,700 | 2.3 | 3 | 3 | agree |
| Tami (Movement for the Heritage of Israel) | תנועה למסורת ישראל | 44,466 | 2.3 | 3 | 3 | agree |
| Telem (Movement for National Renewal) | תנועה להתחדשות ממלכתית | 30,600 | 1.6 | 2 | 2 | agree |
| Shinui | שינוי | 29,837 | 1.5 | 2 | 2 | agree |
| Ratz | ר"ץ | 27,921 | 1.4 | 1 | 1 | agree |

K-RES-10 prints Tami above Tehiya although Tehiya has more votes; the numbers are as above. Kach won no seat (5,128
votes) [S, WP-1981].

### 11th Knesset, election of 23 July 1984

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 13 Aug 1984 | WP |
|---|---|---|---|---|---|---|
| Alignment | מערך | 724,074 | 34.9 | 44 | 44 | agree |
| Likud | הליכוד | 661,302 | 31.9 | 41 | 41 | agree |
| Tehiya-Tsomet | תחייה | 83,037 | 4.0 | 5 | 5 | agree |
| National Religious Party | מפלגה דתית לאומית | 73,530 | 3.5 | 4 | 4 | agree |
| Hadash | חזית דמוקרטית לשלום ושוויון | 69,815 | 3.4 | 4 | 4 | agree |
| Shas | התאחדות הספרדים שומרי תורה - תנועת ש"ס | 63,605 | 3.1 | 4 | 4 | agree |
| Shinui | שינוי | 54,747 | 2.7 | 3 | 3 | agree |
| Ratz | ר"ץ | 49,698 | 2.4 | 3 | 3 | agree |
| Yahad | יחד | 46,302 | 2.2 | 3 | 3 | agree |
| Progressive List for Peace | רשימה מתקדמת לשלום | 38,012 | 1.8 | 2 | 2 | agree |
| Agudat Yisrael | אגודת ישראל | 36,079 | 1.7 | 2 | 2 | agree |
| Morasha-Po'alei Agudat Yisrael | מורשה | 33,287 | 1.6 | 2 | 2 | agree (WP: "Morasha") |
| Tami | תנועה למסורת ישראל | 31,103 | 1.5 | 1 | 1 | agree |
| Kach | כך | 25,907 | 1.2 | 1 | 1 | agree |
| Ometz | אומץ | 23,845 | 1.2 | 1 | 1 | agree |

K-HIST-11 calls the Tehiya list "Tehiya" with a footnote that it was called "Tehiya-Tzomet" until Tzomet broke off,
and calls the Hadash list "Hadash-Black Panthers".

### 12th Knesset, election of 1 November 1988

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 21 Nov 1988 | WP |
|---|---|---|---|---|---|---|
| Likud | הליכוד | 709,305 | 31.1 | 40 | 40 | agree |
| Alignment | מערך | 685,363 | 30.0 | 39 | 39 | agree |
| Shas | התאחדות הספרדים שומרי תורה - תנועת ש"ס | 107,709 | 4.7 | 6 | 6 | agree |
| Agudat Yisrael | אגודת ישראל | 102,714 | 4.5 | 5 | 5 | agree |
| Ratz | ר"ץ | 97,513 | 4.3 | 5 | 5 | agree |
| National Religious Party | מפלגה דתית לאומית | 89,720 | 3.9 | 5 | 5 | agree |
| Hadash | חזית דמוקרטית לשלום ושוויון | 84,032 | 3.7 | 4 | 4 | agree |
| Tehiya | תחייה | 70,730 | 3.1 | 3 | 3 | agree |
| Mapam | מפלגת פועלים מאוחדת | 56,345 | 2.5 | 3 | 3 | agree |
| Tsomet | צומת | 45,489 | 2.0 | 2 | 2 | agree |
| Moledet | מולדת | 44,174 | 1.9 | 2 | 2 | agree |
| Shinui | שינוי | 39,538 | 1.7 | 2 | 2 | agree |
| Degel Hatorah | דגל התורה | 34,279 | 1.5 | 2 | 2 | agree |
| Progressive List for Peace | רשימה מתקדמת לשלום | 33,695 | 1.5 | 1 | 1 | agree in WP-1988; [C] WP-1984 (C2) |
| Arab Democratic Party | מד"ע - מפלגה דמוקרטית ערבית | 27,012 | 1.2 | 1 | 1 | agree |

Kach did not run; see §4.

### 13th Knesset, election of 23 June 1992

| List | Faction name (OD-F) | Votes | % | Seats | OD-P, 13 Jul 1992 | WP |
|---|---|---|---|---|---|---|
| Labor | מפלגת העבודה הישראלית | 906,810 | 34.7 | 44 | 44 | agree |
| Likud | הליכוד | 651,229 | 24.9 | 32 | 32 | agree |
| Meretz | מרצ | 250,667 | 9.6 | 12 | 12 | agree |
| Tsomet | צומת | 166,366 | 6.4 | 8 | 8 | agree |
| National Religious Party | מפלגה דתית לאומית | 129,663 | 5.0 | 6 | 6 | agree |
| Shas | התאחדות הספרדים שומרי תורה - תנועת ש"ס | 129,347 | 4.9 | 6 | 6 | agree |
| Yahadut Hatorah (United Torah Judaism) | יהדות התורה | 86,167 | 3.3 | 4 | 4 | agree |
| Hadash | חזית דמוקרטית לשלום ושוויון | 62,546 | 2.4 | 3 | 3 | agree |
| Moledet | מולדת | 62,269 | 2.4 | 3 | 3 | agree |
| Arab Democratic Party | מד"ע - מפלגה דמוקרטית ערבית | 40,788 | 1.6 | 2 | 2 | agree |

Tehiya ran in 1992 and did not pass the 1.5% threshold [S, WP-1992; vote count not taken].

## 3. Changes during each term

Each row gives the date from OD-F (the day a faction starts or finishes) unless another source is named. "Seats
after" is counted from OD-P on that day. K-HIST-n footnotes describe the same moves without dates; where they confirm a
row, the row says so.

### 9th Knesset (13 June 1977 to 20 July 1981)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 5 Jul 1977 | Shlomzion's faction ends; its two members join the Likud faction | Likud 45 | [V] OD-F, OD-P (6 Jul 1977); K-HIST-9 note 1 |
| 5 Jul 1977 | Moshe Dayan, elected on the Alignment list, sits as a one-member faction (to 19 May 1981). He is in Begin's government as "an Independent member" | Alignment 31 | [V] OD-F, OD-P; K-HIST-9 |
| 24 Oct 1977 | Dash joins the government; ministers go from 13 to 19 and the coalition from 61 to 76 members | — | [V] K-HIST-9 |
| 28 Feb 1978 | Flatto-Sharon's faction renamed Development and Peace (פיתוח ושלום) | — | [V] OD-F; K-HIST-9 note 4 |
| 14 Sep 1978 | Dash's faction ends. It splits into the Democratic Movement (7), the Movement for Change and Initiative (7, תנועה לשינוי ויוזמה) and Ya'ad (1) | DM 7, MCI 7, Ya'ad 1 | [V] OD-F, OD-P (14 Sep 1978). WP-1977 and HE-9 give the same three parts and sizes |
| 15 May 1979 | Moshe Shamir (Likud) sits alone; from 17 Jul 1979 as Banai (בנא"י), from 20 Nov 1979 as Tehiya–Banai | Tehiya–Banai 2 at the end of the term | [V] OD-F; K-HIST-9 ("Tehiya-Bnai split off of the Likud"). Tehiya's founding in 1979 is [S] |
| 5 Feb 1980 | Mordechai Elgrabli leaves the Democratic Movement and sits alone (to 19 May 1981) | — | [V] OD-F; HE-9 gives the same date |
| 13 May 1980 | David Golomb and Meir Amit leave the Movement for Change and Initiative for the Alignment | — | [S] HE-9; K-HIST-9 note 2 confirms "two members moved to the Alignment from the Democratic Movement for Change", without a date |
| 8 Jul 1980 | Ahva (אחווה) faction from the Democratic Movement | — | [V] OD-F; HE-9 same date |
| 29 Jul 1980 | The Movement for Change and Initiative becomes Shinui – Center Party | Shinui 5 at the end | [V] OD-F; K-HIST-9 note 3 |
| 11 Nov 1980 | One Israel (ישראל אחת) faction: Yitzhak Yitzhaki, ex-Shlomzion, who had sat alone from 14 Oct 1980 | — | [V] OD-F; K-HIST-9 note 7 |
| 11 Nov 1980 – 19 May 1981 | Saadia Marciano (ex-Sheli) sits alone, from 30 Dec 1980 as Equality in Israel – Panthers; on 19 May 1981 he and Elgrabli form the Unity Party (מפלגת האיחוד לקידום ולחינוך החברה בישראל) | Unity Party 2 | [V] OD-F; K-HIST-9 note 8 |
| 26 Jan 1981 | Rafi – National List (רפ"י - הרשימה הממלכתית) faction of Likud members (to 19 May 1981) | — | [V] OD-F |
| 10 Feb 1981 | The Knesset passes its dissolution law; election 30 Jun 1981 | — | [V] L-DISS-9 |
| 10 Mar 1981 | The Democratic Movement's faction ends; Yigael Yadin and Binyamin Halevy sit alone; Shmuel Tamir from 17 Mar, Yosef Tamir from 11 May 1981 | — | [V] OD-F; HE-9 same date for Halevy and Tamir |
| 19 May 1981 | Telem – Movement for National Renewal (תל"ם) faction: Dayan, two ex-Likud and one ex-Dash member | Telem 4 | [V] OD-F, OD-P; K-HIST-9 note 6 |
| 20 Jul 1981 | End of term | Likud 40, Alignment 33, NRP 12, Hadash 5, Shinui 5, Agudat 4, Telem 4, Tehiya–Banai 2, Unity 2, thirteen factions of 1 | [V] OD-P (19 Jul 1981); K-HIST-9 last column agrees |

### 10th Knesset (20 July 1981 to 13 August 1984)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 5 Aug 1981 | Begin forms the 19th government: Likud, NRP, Agudat Yisrael, Tami and Tehiya; 61 members at formation, later 66 with Tehiya and Telem | — | [V] K-HIST-10 |
| 16 Nov 1981 | Ratz's faction ends; its one member sits in the Alignment faction | Alignment 48, Likud 48 | [V] OD-F, OD-P; K-HIST-10 note 4 |
| 6 Jun 1983 | Telem's faction ends and splits into Rafi – National List (1) and the Movement for the Renewal of Social Zionism (1) | — | [V] OD-F, OD-P; K-HIST-10 note 3 |
| 10 Oct 1983 | Shamir forms the 20th government with the same parties (66 members). Haim Druckman leaves the NRP and sits alone | NRP 5 | [V] K-HIST-10; OD-F (Druckman from 10 Oct 1983) |
| 4 Apr 1984 | The Knesset passes its dissolution law; election 23 Jul 1984 | — | [V] L-DISS-10. K-HIST-10 has a photograph captioned "Opposition MKs before the vote on the early dissolution of the 10th Knesset, 22.3.1984"; which reading that was is [GAP] |
| 14 May 1984 | Ratz sits as its own faction again | Ratz 1 | [V] OD-F; K-HIST-10 note 4 |
| 29 May – 12 Jun 1984 | Gesher – Religious Zionist Centre (גשר - מרכז ציוני דתי) faction | — | [V] OD-F. WP-1981: two NRP members who returned two weeks later [S] |
| 26 Oct 1982 | Amnon Lin and Yitzhak Peretz (Likud; not the Shas member of the same name) move to the Alignment faction | Alignment 50, Likud 46 (6 Jun 1983) | [V] OD-P (their faction terms, PersonID 2437 and 2558); K-HIST-10 note 1 |
| 13 Aug 1984 | End of term | Alignment 49, Likud 46, NRP 5, Agudat 4, Hadash 4, Tami 3, Tehiya 3, Shinui 2, Ratz 1, Rafi 1, Social Zionism 1, Druckman 1 | [V] OD-P (12 Aug 1984); K-HIST-10 notes 1 and last column |

### 11th Knesset (13 August 1984 to 21 November 1988)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 13 Sep 1984 | Peres forms the 21st government (rotation): Alignment, Likud, NRP, Agudat Yisrael, Shas, Morasha, Shinui and Ometz; 97 members | — | [V] K-HIST-11 |
| 22 Oct 1984 | Yahad's faction ends and its 3 members join the Alignment; Mapam (מפלגת פועלים מאוחדת) leaves the Alignment as its own faction of 6. Yossi Sarid moves from the Alignment to Ratz the same day (OD-P, PersonID 512) | Likud 41, Alignment 40, Mapam 6, Ratz 4 | [V] OD-F, OD-P (22 Oct 1984); K-HIST-11 note 1. WP-1984 says six members re-established Mapam, which agrees |
| 31 Jul 1985 | Basic Law: The Knesset (Amendment No. 9) passed, adding section 7A | — | [V] L-BL-9 |
| 20 Oct 1986 | Shamir forms the 22nd government (rotation); 96 members | — | [V] K-HIST-11 |
| 26 May 1987 | Shinui leaves the coalition (94 members) | — | [V] K-HIST-11 |
| 16 Nov 1987 | Rafael Eitan leaves Tehiya and sits alone; from 30 Nov 1987 as Tzomet | Tehiya 4, Tzomet 1 | [V] OD-F, OD-P; K-HIST-11 note 3 |
| 15 Feb 1988 | Abdulwahab Darawshe leaves the Alignment and sits alone; from 7 Mar 1988 as the Arab Democratic Party (Mada) | Mada 1 | [V] OD-F. WP gives 15 Feb 1988 as the party's founding, which agrees |
| 27 Jul 1988 | The election law for the 12th Knesset names 1 Nov 1988 as election day | — | [V] L-EL-12 |
| 29 Aug 1988 | Tami's faction ends; it joins the Likud | — | [V] OD-F; K-HIST-11 note 2 |
| 27 Sep 1988 | Ometz's faction ends; it joins the Likud. Shimon Ben-Shlomo leaves Shas and sits alone | Shas 3 | [V] OD-F; K-HIST-11 notes 2 and 5 |
| 29 Jul 1986 | Haim Druckman moves from Morasha to the NRP; Morasha is then called Morasha–Po'alei Agudat Yisrael | NRP 5, Morasha 1 | [V] OD-P (PersonID 523); K-HIST-11 note 4 |
| 5 Aug 1987 | Mordechai Virshubski moves from Shinui to Ratz | Ratz 5, Shinui 2 | [V] OD-P (PersonID 2321); K-HIST-11 note 6 |
| 15 Mar 1988 | Yitzhak Artzi moves from the Alignment to Shinui | Shinui 3 | [V] OD-P (PersonID 2172); K-HIST-11 notes 1 and 6 |
| 12 Jul 1988 | Muhammed Wattad moves from Mapam to Hadash | Mapam 5, Hadash 5 | [V] OD-P (PersonID 2328); K-HIST-11 last column; WP-1984 names him |
| 21 Nov 1988 | End of term | Likud 43, Alignment 38, Ratz 5, NRP 5, Hadash 5, Mapam 5, Tehiya 4, Shinui 3, Shas 3, PLP 2, Agudat 2, Tzomet 1, Mada 1, Morasha 1, Kach 1, Ben-Shlomo 1 | [V] OD-P (20 Nov 1988); K-HIST-11 last column agrees |

Kach's single member sat for the whole term (faction כך, 13 Aug 1984 – 21 Nov 1988) [V] OD-F.

### 12th Knesset (21 November 1988 to 13 July 1992)

| Date | Change | Seats after | Source |
|---|---|---|---|
| 22 Dec 1988 | Shamir forms the 23rd government: Likud, Alignment, NRP, Shas, Agudat Yisrael and Degel Hatorah; 25 ministers, 97 members | — | [V] K-HIST-12 |
| 15 Mar 1990 | The government falls on a no-confidence motion and stays as a transition government without Labor. This settles the design's "to verify" on 15 March 1990 | — | [V] K-HIST-12 |
| 15 Mar 1990 | Party for the Advancement of the Zionist Idea (מפלגה לקידום הרעיון הציוני) faction from the Likud; renamed New Liberal Party on 3 Mar 1992 | 3 members (25 Dec 1990) | [V] OD-F, OD-P; K-HIST-12 note 1 (five left the Likud, two returned) |
| 11 Jun 1990 | Shamir forms the 24th government: Likud, NRP, Shas, Agudat Yisrael, Degel Hatorah, the Party for Advancing the Zionist Idea, Tehiya, Tzomet, Moledet and the one-member factions of Efraim Gur and Yitzhak Peretz; 19 ministers, 62 members | — | [V] K-HIST-12 |
| 25 Dec 1990 | Three one-member factions: Moriah (Yitzhak Peretz, ex-Shas), Unity for Peace and Aliyah (Efraim Gur, ex-Alignment) and Geulat Yisrael (Eliezer Mizrahi, ex-Agudat Yisrael). Charlie Biton leaves Hadash, sitting alone to 1 Jan 1991 and then as Black Panthers | Shas 5, Agudat 4, Hadash 3 | [V] OD-F, OD-P; K-HIST-12 notes 2, 3, 5. Names from OD-F |
| 7 Oct 1991 | The Alignment faction becomes the Israeli Labor Party faction (מפלגת העבודה הישראלית) | Labor 38 | [V] OD-F, OD-P |
| 14 Oct 1991 | Threshold raised to 1.5% | — | [V] L-THR |
| 31 Dec 1991 | Tzomet leaves the government | — | [V] K-HIST-12 |
| 21 Jan 1992 | Tehiya and Moledet leave the government | — | [V] K-HIST-12; [C] HE-12 gives 16 Jan 1992 (C3) |
| 4 Feb 1992 | The Knesset passes its dissolution law; election 23 Jun 1992 | — | [V] L-DISS-12 |
| 9 Mar 1992 | Ratz, Mapam and Shinui factions end; the Meretz faction starts | Meretz 10 | [V] OD-F, OD-P; K-HIST-12 note 4 |
| 17 May 1992 | Unity for Peace and Aliyah ends; Gur joins the Likud | Likud 38 | [V] OD-F, OD-P; K-HIST-12 note 1 ("One member moved from the Alignment to the Likud") |
| 13 Jul 1992 | End of term | Likud 38, Labor 38, Meretz 10, NRP 5, Shas 5, Agudat 4, Hadash 3, New Liberal 3, Tehiya 3, Tzomet 2, Degel 2, Moledet 2, five factions of 1 | [V] OD-P (12 Jul 1992); K-HIST-12 last column agrees |

### 13th Knesset, to the end of 1993

| Date | Change | Source |
|---|---|---|
| 13 Jul 1992 | First sitting; Rabin forms the 25th government the same day: Labor, Meretz and Shas, 17 ministers, 62 coalition members and five more from outside the coalition | [V] OD-D; K-HIST-13. HE-13 gives the confidence vote as 67 to 53 [S] |
| 14 Sep 1993 | Shas leaves the government | [V] K-HIST-13. WP-1992 "September 1993" agrees |
| to 31 Dec 1993 | No faction change. The first new faction of the 13th Knesset is Yi'ud on 7 Feb 1994, after this era | [V] OD-F; OD-P on 31 Dec 1993 equals the election result |

The 13th Knesset ran to 17 Jun 1996 [V] OD-F; its later splits (Yi'ud, Atid, Gesher, the Third Way, the end of United
Torah Judaism's joint faction) are outside this chapter.

## 4. Kach and the 1988 election

- Section 7A, added to Basic Law: The Knesset on 31 Jul 1985, bars a list whose aims or acts include denying the
  existence of Israel as the state of the Jewish people, denying its democratic character, or incitement to racism.
  It also lets the Attorney General, the committee chairman or a quarter of the committee appeal a list's approval to
  the Supreme Court [V] L-BL-9.
- The Central Elections Committee for the 12th Knesset barred the Kach list under section 7A. The appeal, EA 1/88
  (Moshe Neiman and the Kach party against the committee's chairman), was argued on 11 and 13 Oct 1988 and decided on
  18 Oct 1988 by President Shamgar, Deputy President Elon and Justices Bejski, S. Levin and D. Levin. The court held
  that the list was lawfully barred for incitement to racism and for undermining the state's democratic character [V]
  EA-1-88 (abstract and holdings K.1–2).
- The date of the committee's decision is 5 Oct 1988 [V, agent read 25 Oct 2026; a person's check pending] (JTA bulletin of 6 Oct 1988, dateline "Oct. 5", "voted overwhelmingly Wednesday"; see the follow-up, "Second pass"). The Versa page's first paragraph is garbled in the source text; the legible part ends "…5, 1988". WP-1988's poll table puts "The Central Elections Committee forbids Kach from running in the election" on 4 Oct 1988 [S], which the bulletin does not support. See C4.
- The committee also barred the Progressive List for Peace, and the Supreme Court reversed that decision [S] WP-1984.
  The PLP ran and won one seat [V] K-RES-12.
- Kach was later barred from the 1992 election and banned outright in 1994 [S] WP "Kach". Both are outside the
  1977–1993 seat tables and need their own check if an event uses them.

## 5. Party origins, as far as the primary sources show them

The Knesset sources give the first election or faction date; founding dates of parties outside the Knesset come only
from Wikipedia [S], and their primary founding dates are [GAP].

| Party | First seen in a primary source | Founding, Wikipedia [S] |
|---|---|---|
| Dash (Democratic Movement for Change) | 1977 list, 15 seats [V] K-RES-9; faction ends 14 Sep 1978 [V] OD-F | founded 2 Nov 1976, dissolved 14 Sep 1978 |
| Shlomzion | 1977 list, 2 seats [V]; into Likud 5 Jul 1977 [V] | 1976 |
| Tehiya | faction from 17 Jul 1979 (as Banai) [V] OD-F; 1981 list [V] | 1979 |
| Tami | 1981 list, 3 seats [V]; into Likud 29 Aug 1988 [V] | split from the NRP; date not given |
| Telem | faction 19 May 1981 [V]; 1981 list, 2 seats [V] | 1981 |
| Shas | 1984 list, 4 seats; faction 13 Aug 1984 [V] | 1984, split from Agudat Yisrael |
| Morasha, Yahad, Ometz, Progressive List for Peace, Kach (seat) | 1984 lists [V] K-RES-11 | Morasha 1984; PLP 1984; Ometz traced to Rafi – National List (26 Jan 1981) and Rafi (6 Jun 1983) |
| Mapam (separate again) | faction 22 Oct 1984 [V] OD-F | — |
| Tzomet (separate) | faction 30 Nov 1987 [V] OD-F | 1987 (second founding) |
| Arab Democratic Party | faction 7 Mar 1988 [V] OD-F | 15 Feb 1988 |
| Moledet, Degel Hatorah | 1988 lists [V] K-RES-12 | both 1988 |
| Labor (faction name) | 7 Oct 1991 [V] OD-F | — |
| Meretz | faction 9 Mar 1992 [V] OD-F; 1992 list, 12 seats [V] | 1992 as an alliance; exact date [GAP] |
| United Torah Judaism | 1992 list, 4 seats; faction 13 Jul 1992 [V] | 1992 |

## 6. Conflicts

- **C1. Eligible voters, 1977.** CEC-BF gives 2,236,478. K-RES-9, CEC-PCT and WP-1977 give 2,236,293. Two pages of the
  same committee disagree; the Knesset table, the CEC's turnout table and the turnout arithmetic (79.2%) all fit
  2,236,293.
- **C2. Progressive List for Peace seats, 1988.** WP-1984 says the PLP competed in 1988 "winning two seats". K-RES-12,
  K-HIST-12, OD-P and WP-1988 give one seat.
- **C3. Tehiya and Moledet leave the 24th government.** K-HIST-12: 21 Jan 1992 (Tzomet 31 Dec 1991). HE-12: Moledet
  and Tehiya left on 16 Jan 1992, a few days after Tzomet. WP-1988: "late 1991/early 1992". The 16 January date may
  be the announcement and the 21st the day the resignations took effect; that is not confirmed [GAP].
- **C4. Date of the committee's decision against Kach.** Second pass, 25 Oct 2026: 5 Oct 1988 in the JTA bulletin of 6 Oct 1988 and in the Versa PDF (see the follow-up). Values as first recorded: WP-1988: 4 Oct 1988. EA-1-88 (Versa text): unreadable,
  ending "…5, 1988". Dated 5 Oct 1988 by the JTA bulletin [V, agent read 25 Oct 2026; a person's check pending]; the committee's own record is still [GAP].
- **C5. Naming only, no numbers at stake.**
  - The 1984 Morasha list: K-RES-11 "Morasha-Po'aeli Agudat Yisrael"; K-HIST-11 "Morasha", with a note that it took the
    longer name later; WP-1984 "Morasha".
  - The 1984 Tehiya list: K-RES-11 "Tehiya-Tsomet"; K-HIST-11 "Tehiya".
  - The 1977 Flatto-Sharon list: K-RES-9 "Plato Sharon", K-HIST-9 "Platto-Sharon", OD-F "פלאטו שרון", WP
    "Flatto-Sharon".
  - The 1983 Telem split: WP-1981 says Telem split into "Ometz" and the Movement for the Renewal of Social Zionism; OD-F
    names the first faction Rafi – National List (6 Jun 1983). Ometz is the name of the 1984 list.

Checked against the design (§2.1) and found consistent: the five election dates, the near-tie seat counts (48–47,
44–41, 40–39), Labor 44, Likud 32, Meretz 12 and Shas 6 in 1992, the government formation dates, and Dash's entry into
the government in October 1977 (24 Oct 1977, K-HIST-9).

## 7. Gaps

- CEC official per-list results for 1977–1992: not online at bechirot.gov.il; the Reshumot results notices were not
  searched. The Knesset's own tables, checked against OD-P, stand in.
- Votes for lists that won no seat: only from Wikipedia [S].
- The Knesset's HTML pages could be read only as Internet Archive copies (2013 and 2016).
- `KNS_Knesset`, named in the task, does not exist in the OData service (404 "Resource not found"). KNS_KnessetDates is
  incomplete for 1977–1981 (see §1).
- Moves inside a term are dated from OD-P, the Knesset's record of each member's faction terms. Names were looked up
  in KNS_Person. Members who left the Knesset and were replaced are not listed as moves.
- Founding dates of Shas, Meretz (as an alliance), Moledet, Degel Hatorah and Tzomet in a primary source.
- The date of the committee's decision against Kach (C4): dated 5 Oct 1988 by the JTA bulletin on the second pass, 25 Oct 2026; the committee's own record is still [GAP]. The reading voted on 22 Mar 1984 (10th Knesset) remains [GAP].
- Whether 1 Nov 1988 was the end-of-term date fixed by law: answered provisionally on the second pass, 25 Oct 2026 (derived from the archived Basic Law text and calendar arithmetic, not printed in any source; amendments between 27 May 1987 and 1 Nov 1988 not checked, so still open; see the paragraph after the law table above and the follow-up).

## 8. What the records could look like

For #146's `legislatures.yaml` work, not written here. A record per Knesset from its first sitting, with the seats in
§2, and a new record at each change the issue calls a merger or split. The sourced break points with a seat effect
are: 5 Jul 1977 (Shlomzion into the Likud; Dayan alone), 14 Sep 1978 (Dash splits), 19 May 1981 (Telem), 16 Nov 1981
(Ratz into the Alignment), 22 Oct 1984 (Yahad in, Mapam out), 30 Nov 1987 (Tzomet), 7 Mar 1988 (Mada), 29 Aug and 27
Sep 1988 (Tami and Ometz into the Likud), 15 Mar 1990 (Modai's faction), 25 Dec 1990 (three one-member factions and
Biton), 7 Oct 1991 (Labor), 9 Mar 1992 (Meretz) and 17 May 1992 (Gur to the Likud). Which of these deserve their own
record is a content decision for the issue, and `parties.yaml` has no ids yet for any party first seen after 1955.
