# Model basis, 1977-93: the policies, indicators and actor windows of design section 5.1 and 5.5

Researched 24 October 2026. Every source below was opened by an agent on that date; a person's check of the sources is pending. **Research only:** a note for issue #153 (1977-1993 item 9, part of #51), the research part the owner opened on 24 Oct. It adds no `model.yaml` record, no effect and no test, and changes nothing under `content/`. The records themselves stay paused until after the MVP.

## Purpose

[The 1977-1993 design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md) section 5.1 lists the policies, indicators and the voter group that each "need a sourced historical basis before it lands", and marks two groups of dates "to verify" (the start of `direct_absorption` and the dates of `home_front_readiness`). Section 5.5 lists the actor windows, with the 1981 memorandum, the resumption of relations with the USSR, the Lebanese Forces' window, the date of the South Lebanon Army's name and Hezbollah's founding marked "to verify". This note gives, row by row, what a source says, in what words and on which day, where two sources differ, and what is still open. Where another note of this era already sources a fact, the row points to it and does not repeat it (the economy note for the price, bank-crisis and stabilization numbers in particular).

Rows of the design that are values and not history (the level a policy opens at, the hysteresis of a situation, the names under D11) have no source to find; the note says so instead of filling them.

## Conventions

The same as [economy-1977-93.md](economy-1977-93.md), [events-1987-93.md](events-1987-93.md) and the 1957-77 model-basis note (PR #1610, in review).

- **[V, agent read 2026-10-24; a person's check pending]** read on a primary source named in the row. Here the only [V] rows are UN Digital Library records (resolution number, date, vote and title as the record gives them; the resolution texts were not opened).
- **[S]** a single or secondary source. Press is [S] throughout: JTA bulletins opened on the page itself. A copy of a primary document hosted by a library (the Avalon Project) is [S] here, because the original was not opened. Wikipedia is [S, en.wiki oldid N, Q-id].
- **[C]** sources conflict; every value is kept.
- **[GAP]** still open, with what was tried. A search result list or snippet is never a source; a result I did not open is not "read".
- **JTA dates.** A JTA archive page shows the bulletin date. The event date comes from the dateline on the item in the original bulletin PDF ("See Original Daily Bulletin"), usually one day earlier. The PDFs are scans, so the datelines were read by OCR (RapidOCR) on 24 Oct. Where the OCR gave the item's own dateline the row says "dateline X"; where it gave only the datelines of other items in the bulletin, or none, the row says so and resolves a "Monday" or "Friday" from the calendar, marked "computed here".
- **Cross-references.** `procurement-1977-93.md` (PR #1622) and the 1957-77 model-basis note (PR #1610) are in review and not on main; rows that cite them say "not re-read here".
- **Negative searches** are in section 6, each with the query, the date range, the count the search returned and the day it was run.
- Quotes are short and copied from the page. Figures "computed here" show the arithmetic.

## Source caveat

- **Reached:** the JTA archive through its own search interface (https://www.jta.org/archive) and the original bulletin PDFs at pdfs.jta.org; the UN Digital Library (https://digitallibrary.un.org); the Avalon Project at Yale (https://avalon.law.yale.edu); the English Wikipedia API (https://en.wikipedia.org/w/api.php).
- **Not reached:**
  - **FRUS 1981-88.** The TEI files for volumes XVIII part 1, XIX and XXII in https://github.com/HistoryAtState/frus are title pages only (2,530, 2,476 and 2,518 bytes on 24 Oct), so no FRUS document from 1981 on was read. FRUS 1977-80 was not needed for these rows.
  - **Israeli primaries.** The Knesset site was blocked for the 1957-77 note on 24 Oct (a geo-block page) and was not retried. The Israel Ministry of Foreign Affairs page for the memorandum (https://mfa.gov.il/MFA/ForeignPolicy/MFADocuments/Yearbook1/Pages/53%20Memorandum%20of%20Understanding%20on%20Strategic%20Cooperation.aspx) answered 200 with an error page and no document. Cabinet and Knesset decision texts are therefore [GAP] throughout.
  - **The Taif Agreement text.** https://peacemaker.un.org/sites/peacemaker.un.org/files/LB_891022_TaifAgreement.pdf and https://peacemaker.un.org/lebanon-taifaccords89 both answered 404.
  - **The Pentagon copy of the 1981 memorandum.** https://www.esd.whs.mil/Portals/54/Documents/FOID/Reading%20Room/MDR_Releases/FY25/FY25_Q1/18-M-1176.pdf returned a 509-byte HTML page, not the PDF.
  - **Bank of Israel annual reports 1977-79.** The URLs by the pattern that works for later reports (`.../Annual%20Report%201977/part%20one/pe_1.pdf`, also 1978 and 1979) answered 404 on `www.boi.org.il`; 1977 and 1978 also on `boi.org.il`.
- A built-in web search tool was used only to find URLs (there is no Tavily key on this box). Nothing in this note rests on a search result; each page cited was opened.

## 1. Economic policies and indicators (design 5.1)

| Id | Design says | What the sources give | Status |
|---|---|---|---|
| `price_stability`, `hyperinflation` | indicator high when inflation is low; a flag situation with two thresholds; "from 1977" | The price numbers (CBS monthly index 1979-85, BoI annual changes, the October 1983 measures, the 1 July 1985 programme) are in economy-1977-93.md sections 2 and 3, tagged there. The thresholds are design values | cross-reference; [GAP] by nature for the thresholds |
| `deficit_spending` | the deficit's size, "the Aridor years against the 1985 cuts"; opens at its June 1977 level | Aridor was confirmed as Finance Minister on 19 January 1981; the 1985 cuts: 1.2 and 1.3. No deficit figure was read | [S]; [GAP] for the June 1977 level and the deficit series |
| `exchange_rate_regime` | crawling devaluation against a fixed rate; "from 1977" | The pound was freed on Friday 28 October 1977 (1.1) | [S]; the day is computed |
| `wage_price_agreement` | package deals with the Histadrut and employers | The three-month wage-price freeze of 2 November 1984 and the 1985 programme (1.2, 1.3) | [S] |

### 1.1 `exchange_rate_regime`: 28 October 1977

- JTA bulletin of 31 Oct 1977, "Abolition of Currency Regulations and Other Sweeping Measures Are Introduced to Create a Free Economy" (https://www.jta.org/archive/abolition-of-currency-regulations-and-other-sweeping-measures-are-introduced-to-create-a-free-econom), [S]. Dateline JERUSALEM, Oct. 30. "Israel was plunged into a new economic era Friday when the government announced the abolition of currency regulations and other sweeping measures"; "The first move was to establish a free-floating currency. The Israel Pound which stood at IL 10.36 to $1 last week is expected to drop to IL 15 to $1 when the banks reopen tomorrow." The plan was "adapted [sic] by Finance Minister Simcha Ehrlich", and the Cabinet approved it "at an unprecedented Friday meeting with one dissenting vote and one abstention". The bulletin of Monday 31 Oct 1977 and a dateline of Sunday 30 Oct make the Friday 28 October 1977 (computed here from the calendar).
- So the design's "from 1977" holds, and the day is 28 October, not the 20 June of the new government. The item calls the regime "free-floating"; the design's "crawling devaluation against a fixed rate" is a later description of what followed and is not sourced here. [GAP] the Bank of Israel's account of the regime in 1977-79 (the reports' URLs were not reached, see the caveat) and a source for the crawling-peg period.

### 1.2 `deficit_spending` and the Aridor years

- JTA bulletin of 20 Jan 1981, "Aridor is New Finance Minister" (https://www.jta.org/archive/aridor-is-new-finance-minister), [S]: the Cabinet met in special session "to confirm Premier Menachem Begin's choice of Yoram Aridor to be Finance Minister, replacing Yigal Hurwitz who resigned Jan. II." Dateline JERUSALEM, Jan. 19 (read by OCR on the original bulletin): "today" is 19 January 1981.
- JTA bulletin of 21 Jan 1981, "New Economic Program to Be Introduced by Aridor" (https://www.jta.org/archive/new-economic-program-to-be-introduced-by-aridor), [S]: the new minister "is expected to introduce an economic program to slow the steady devaluation of the Shekel, improve labor relations, encourage long-term investments and savings by the public". Dateline JERUSALEM, Jan. 20: "His appointment was approved by the Cabinet yesterday", so 19 January. The opened text gives intentions, not the deficit.
- **Open:** [GAP] a deficit figure for any year (the Bank of Israel annual reports of 1979-82 were not reached); [GAP] the level at which `deficit_spending` stands in June 1977, a design value. The 1985 cuts: 1.3.

### 1.3 `wage_price_agreement`: 1984 and 1985

- JTA bulletin of 26 Oct 1984, "Histadrut Rejects Government's Proposal for an Economic Package" (https://www.jta.org/archive/histadrut-rejects-governments-proposal-for-an-economic-package), [S]: the proposal asked "for a one-third cut in cost-of-living increments for the next four months, with workers compensated by a five percent cut in their income taxes and a price freeze for the next six weeks".
- JTA bulletin of 5 Nov 1984, "Cabinet Approves Wage-price Freeze Package Aimed to Bring Down Inflation" (https://www.jta.org/archive/cabinet-approves-wage-price-freeze-package-aimed-to-bring-down-inflation), [S]. The bulletin's datelines are Nov. 4 (the OCR did not match this item). It says the Cabinet "today approved a three-month wage-price freeze agreed to by the government, labor and management last Friday". The bulletin of 15 Nov 1984 (https://www.jta.org/archive/possible-large-scale-unemployment-in-israel-causes-political-furor) says the freeze "went into effect on November 2", which is that Friday (2 November 1984 was a Friday, computed here). So: agreed and in effect 2 Nov 1984; approved by the Cabinet on 4 Nov 1984 [S].
- JTA bulletin of 2 Jul 1985, "Cabinet Adopts Economic Program, Histadrut Calls 24-hour Strike" (https://www.jta.org/archive/cabinet-adopts-economic-program-histadrut-calls-24-hour-strike), [S]. Dateline JERUSALEM, July 1: the Cabinet "adopted an emergency economic program today" and Peres "kept his Cabinet in session all day yesterday and into the early hours of this morning until the program was adopted", so the vote fell on 1 July 1985 (the session ran from 30 June into 1 July). The Bank of Israel's "stabilization program launched on July 1" is in economy-1977-93.md section 3 (S3 there). The design's "hard event's `set_policy` on 1 July 1985" fits both the vote and the launch. The item adds: the programme was "essentially the work of Finance Minister Yitzhak Modai", it passed "by a majority but far from unanimous vote", seven Likud ministers voted against, and Defense Minister Rabin abstained.
- **Open:** [GAP] the Cabinet decision texts and the agreement texts; the Histadrut's side in its own words.

## 2. Security and absorption policies (design 5.1)

| Id | Design says | What the sources give | Status |
|---|---|---|---|
| `lebanon_posture` | from 1982-06-06, end open | The war's day: events-1982-87.md C0 (tagged V there). UN Security Council resolution 508 of 5 June 1982 (2.1). The security zone: section 3.2 | cross-reference; [V, agent read 2026-10-24; a person's check pending] for 508 |
| `intifada_response` | from 1987-12-09 | The first day: events-1987-93.md A1 ([S] there). The doctrine as it was announced in January 1988 (2.2) | cross-reference; [S] |
| `direct_absorption` | from 1989, "to verify" | The idea and the Jewish Agency's agreement with the Absorption Ministry are dated 1988, so "from 1989" is early by about a year [C] (2.3) | [S] |
| `home_front_readiness` | from 1990-08-02, "to verify" the dates | 2 August 1990 is the invasion, not a civil-defence act; the distribution of masks was decided in stages from August to October 1990 (2.4) | [S]; [V, agent read 2026-10-24; a person's check pending] for 2 Aug |

### 2.1 `lebanon_posture`

- The war began on 6 June 1982: events-1982-87.md C0 (tagged V there, from the commission's report), not re-read here.
- UN Digital Library record 278795 (https://digitallibrary.un.org/record/278795), "Security Council resolution 508 (1982) [on the situation along the Lebanese-Israeli border]", date 1982-06-05; record 30305 for the resolution, "adopted by the Security Council at its 2374th meeting, on 5 June 1982", "Adopted unanimously". [V, agent read 2026-10-24; a person's check pending] for the resolution's number, date and vote; the text was not opened.
- UN Digital Library record 280211, "Security Council resolution 425 (1978) [on establishment of a UN interim force for Southern Lebanon]", date 1978-03-19; record 71622, "adopted by the Security Council at its 2074th meeting, on 19 March 1978", "Adopted 12-0-2". [V, agent read 2026-10-24; a person's check pending] for the number, date and vote. This is the Litani-era baseline for the zone (forces-1977-93.md O1 gives the same day).
- The "end left open for #52" is the design's choice; the security zone is in 3.2.

### 2.2 `intifada_response`

- Start: events-1987-93.md A1 (8 to 9 December 1987, [S]), not re-read here.
- JTA bulletin of 20 Jan 1988, "Israel Takes Tough Steps in Gaza, but Restricts Use of Lethal Force" (https://www.jta.org/archive/israel-takes-tough-steps-in-gaza-but-restricts-use-of-lethal-force), [S]. Dateline JERUSALEM, Jan. 19, so the event day is 19 January 1988 (the item's "Tuesday" is that day). It reports orders on both sides of the doctrine: soldiers "have received orders to take the offensive against demonstrators and beat them vigorously when they are caught"; "the IDF has received explicit instructions limiting the use of live ammunition against demonstrators"; Gen. Amram Mitzna "issued orders two days ago that soldiers may use live ammunition only in emergency situations, must aim at the legs of the rioters and may not use expanding bullets"; "The new orders call for the use of rubber bullets". Rabin: "We will act with force, with strength, with blows to stop violent demonstrations." In the Gaza Strip the item reports electricity, water and telephone service cut off in camps under curfew, as Haaretz reported.
- JTA bulletin of 3 Feb 1988, "Unrest Continues, As Rabin Affirms Policy on Beatings" (https://www.jta.org/archive/unrest-continues-as-rabin-affirms-policy-on-beatings), [S]: Rabin "affirmed Tuesday that his policy of ordering IDF troops to pursue and severely beat Arab troublemakers has proven effective and will continue"; "beatings were preferable to shootings"; "the IDF is still using live ammunition". The dateline was not matched; the day is Tuesday 2 February 1988 (computed here).
- Protests: JTA bulletin of 25 Jan 1988, "Thousands of Israelis Protest Tough Policies in the Territories" (https://www.jta.org/archive/thousands-of-israelis-protest-tough-policies-in-the-territories), [S], demanding "that the government stop the killings, beatings and deportations".
- **Design note (section 10.3).** The design says the record offers policing and restraint against force and closures and "never civilian harm as an option". These items are the evidence for what the doctrine's range was in January and February 1988; they are listed so the record's options can be drawn from dated orders, and none is proposed as an option. [GAP] the orders in their own text (the IDF and the Defense Ministry records were not reached) and the day the use of plastic bullets began.

### 2.3 `direct_absorption` (the start the design marks "to verify")

All JTA items are [S]. The term and the plan come from 1988, not 1989.

| Bulletin | What it says | Slug |
|---|---|---|
| 24 Jun 1988 | A Jewish Agency investigation, "headed by academic Dr. Israel Katz, seeks to shut down several absorption centers ... and encourage 'direct absorption' into mainstream Israeli society instead"; immigrant associations protested | `behind-the-headlines-on-eve-of-assembly-jewish-agency-finds-itself-in-whirlwind-of-change` |
| 13 Dec 1988 | The Agency's Kaplan and Dinitz "recently signed a contract with the Absorption Ministry to transfer most absorption responsibility to the govenment [sic]"; "Under the new plan, 55 to 70 percent of all immigrants will be absorbed directly, meaning they will be settied [sic] in apartments rather than housed temporarily in absorption centers". The OCR matched the text but not the item's own dateline (the bulletin's datelines are Dec. 12) | `behind-the-headlines-jewish-agency-chairman-optimistic-on-improvemenes-for-soviets-jews` |
| 12 Jun 1989 | "Last November, the agency agreed with the Israeli government to hand over absorption responsibilities ... The government was to have closed down the bulk of the centers, replacing them with apartment housing, in what is known as 'direct absorption.'" The Agency was "seriously reconsidering the agreement" | `behind-the-headlines-soviet-emigration-challenge-to-be-key-issue-at-jewish-agency-assembly` |
| 27 Jun 1989 | The Council of Olim Organizations says a government programme for "direct absorption" has "failed miserably"; "the government gives cash grants for new immigrants" | `flaws-in-absorption-system-aired-during-agency-assembly` |
| 13 Dec 1989 | About 25 percent of newcomers in absorption centers, "the rest will be go through the 'direct absorption' system" | `israel-is-prep-aring-to-receive-up-to-1000-soviet-jews-a-day` |
| 1 Mar 1990 | "Of the 6,170 immigrants who arrived in January, 4,836 bypassed absorption centers, opting instead for 'direct absorption.'" 4,836 / 6,170 = 78.4 percent (computed here) | `new-data-show-230000-soviet-jews-could-arrive-in-israel-this-year` |

- **[C]** The design's start "from 1989" against the Jewish Agency's agreement of November 1988 with the Absorption Ministry (a bulletin of 12 Jun 1989 says "Last November"; the 13 Dec 1988 bulletin says "recently signed"). The cash grants and self-chosen housing the design describes are in place by June 1989. The flood of arrivals began in the second half of 1989 (population-1977-93.md, citing the CBS: "The low period in immigration to Israel ended suddenly during the second half of 1989"), so a record windowed from 1989 would still sit on the right years for the effect; the *plan* predates it.
- **Open:** [GAP] the contract's text and its day; the Cabinet decision, if any; the size of the cash grant in 1989-90 (a 23 Feb 1990 bulletin, `immigrant-absorption-is-priority-in-1990-budget-of-jewish-agency`, gives about "$11,000 a year" per immigrant in goods, not a grant schedule, and was read only for the line it quotes). The Absorption Ministry's own records were not reached.

### 2.4 `home_front_readiness` (the dates the design marks "to verify")

- 2 August 1990: UN Digital Library record 282068 (https://digitallibrary.un.org/record/282068), "Security Council resolution 660 (1990) [on Iraqi invasion of Kuwait]", date 1990-08-02; record 94220, "adopted by the Security Council at its 2932nd meeting, on 2 August 1990", "Adopted 14-0-0". [V, agent read 2026-10-24; a person's check pending] for the day of the invasion's condemnation. The invasion is the trigger, not an Israeli civil-defence act.
- JTA bulletin of 6 Aug 1990, "Israelis to Receive Gas Masks; Cabinet Unusually Quiet on Iraq" (https://www.jta.org/archive/israelis-to-receive-gas-masks-cabinet-unusually-quiet-on-iraq), [S]. The bulletin's datelines are Aug. 5 and the item speaks of "Sunday's Cabinet meeting" (5 August 1990 was a Sunday, computed here); "Israelis learned they will soon be issued gas masks"; "The gas masks and anti-chemical warfare kits will be issued first in the densely populated Tel Aviv area and in Safed". The item's own dateline was not matched.
- JTA bulletin of 10 Aug 1990, "IDF Will Not Distribute Gas Masks Now for Fear of Provoking Hostile Action" (https://www.jta.org/archive/idf-will-not-distribute-gas-masks-now-for-fear-of-provoking-hostile-action), [S]: Gen. Dan Shomron "announced Thursday night" (9 August; the bulletin's datelines are Aug. 9) that the IDF "has decided not to begin the general distribution of gas masks to civilians at this time", so as not to provoke Iraq.
- JTA bulletin of 23 Aug 1990, "Israeli Ministers Postpone Decision on Timing of Gas Mask Distribution" (https://www.jta.org/archive/israeli-ministers-postpone-decision-on-timing-of-gas-mask-distribution), [S]: "Prime Minister Yitzhak Shamir has again put off deciding whether to issue gas masks to the public now or stick to the original plan to wait until year's end."
- JTA bulletin of 2 Oct 1990, "Gas Mask Distribution to Begin, but Israel Stresses No Emergency" (https://www.jta.org/archive/gas-mask-distribution-to-begin-but-israel-stresses-no-emergency), [S]: "An Israel Defense Force spokesman announced Monday [1 October 1990, computed here; the OCR found no dateline] that the phased distribution of masks and poison gas antidote kits would begin shortly and should be completed in about two months." The public would be told "on or about Oct. 15" where to collect them, and distribution would begin "about a week later"; it "has already been tested in the northern town of Shlomi and in the Morasha neighborhood of Ramat Hasharon"; "The distribution of gas masks in no way whatsoever constitutes a change to an emergency status."
- So the design's start 1990-08-02 is the invasion, the decision to issue masks was reported on 5 August, deferred on 9 August, and the phased distribution was announced on 1 October with the public collection from about 22 October (computed here: 15 October plus "about a week"). No source read gives the first day masks were handed out [GAP]. The missile attacks from 17 January 1991 are events-1987-93.md A8 ([S] on the dates, [C] on the count), not re-read here.
- **Open:** [GAP] the Cabinet's decision text and the budget; the Civil Defense Law and any shelter rules (the statutes were not reached); the Home Front Command's founding date (no source opened). The Iraqi chemical-capability assessments are forces-1977-93.md O10 and I5 ([C], limited access).

## 3. Actors (design 5.5)

| Actor id | Design window | What the sources give | Status |
|---|---|---|---|
| `usa` | windowless; the 1981 memorandum "to verify" | Signed 30 November 1981, suspended after the Golan Law of 14 December (3.1) | [S] |
| `ussr` | windowless; relations resume 1991 "to verify" | Full relations restored on Friday 18 October 1991 (3.3) | [S] |
| `un` | windowless | Resolutions 425, 497, 508, 660, 799 (below), each read on the UN record for number, date and vote | [V, agent read 2026-10-24; a person's check pending] |
| `lebanese_forces` | 1977 to 1993 "to verify" | Formed 1976; the militia's era ends 1990 (en.wiki heading); the party was dissolved on 23 March 1994 (3.4) | [C]; [S] |
| `sla` | from 1978, the name's date "to verify" | A "south Lebanese army" was proclaimed by Haddad in March 1978; en.wiki says the name SLA dates to May 1980 (3.2) | [C] |
| `hezbollah` | from its founding "to verify" | 1982 per en.wiki; the name first appears in JTA on 4 March 1985 (3.5) | [S]; [GAP] for a primary |
| `hamas` | from 1987-12-10 | JTA's first named report is 19 September 1988; the Covenant is dated 18 August 1988 (3.6) | [S]; the design's day is [GAP] |
| `ethiopia` | from 1977, end open | Relations were broken in 1973 and restored in November 1989 (3.7) | [S] |
| `egypt`, `syria`, `jordan`, `iraq`, `lebanon`, `plo`, `territories_residents` | windowless or continuing | Not marked "to verify"; the events are in events-1977-82.md, events-1982-87.md and events-1987-93.md (for example B23, the Golan Heights Law of 14 December 1981 (tagged V there); A9, Madrid, 30 October to 1 November 1991 (tagged V there); the London Agreement of April 1987 is a [GAP] on the day there) | cross-reference, not re-read here |

### 3.1 `usa`: the 1981 memorandum

- The text. Avalon Project, "Memorandum of Understanding between the Government of the United States and the Government of Israel on Strategic Cooperation; November 30, 1981" (https://avalon.law.yale.edu/20th_century/pal03.asp), opened 24 Oct, [S] (a library's copy; the original was not opened). Article I: the cooperation "is designed against the threat to peace and security of the region caused by the Soviet Union or Soviet-controlled forces from outside the region introduced into the region"; "is not directed at any state or group of states within the region". Article III sets up a coordinating council chaired "whenever possible" by the Secretary of Defense and the Minister of Defence, with joint working groups. Article IV: it "shall enter into force upon exchange of notification that required procedures have been completed by each party", and either party may end it "by notifying the other party six months in advance".
- The signing. JTA bulletin of 3 Dec 1981, "Sharon Defends U.s.-israel Agreement As 'a Real Change' in Israel's International Standing" (https://www.jta.org/archive/sharon-defends-u-s-israel-agreement-as-a-real-change-in-israels-international-standing), [S]. Dateline JERUSALEM, Dec. 2 (read from the bulletin of 3 Dec; the OCR gave the dateline of the item's opening). It says the memorandum was signed "by Sharon and Secretary of Defense Caspar Weinberger in Washington Monday", which is 30 November 1981 (computed here), and that four no-confidence motions followed in the Knesset on 2 December. A second item of 3 Dec 1981 (https://www.jta.org/archive/state-dept-on-u-s-israel-pact) has the State Department saying it was "explicit" that the memorandum was not aimed at "any state in the region". JTA of 11 Jan 1982 (https://www.jta.org/archive/weinberger-charges-that-israel-not-the-u-s-cancelled-the-strategic-cooperation-agreement), [S], has Weinberger referring to "the memorandum of understanding on strategic cooperation signed November 30". The date is the same in all three.
- The suspension. JTA bulletin of 21 Dec 1981, "Israel Denounces the U.S. for Suspending Its Cooperation Accord" (https://www.jta.org/archive/israel-denounces-the-u-s-for-suspending-its-cooperation-accord), [S]. Dateline Dec. 20: "Premier Menachem Begin and Defense Minister Ariel Sharon unleashed a severe counter-offensive against the Reagan Administration for its suspension of the recently signed memorandum ... following Israel's surprise annexation of the Golan Heights last Monday." "Last Monday" is 14 December 1981, the day of the Golan Heights Law (events-1977-82.md B23, tagged V there from the Knesset record). The item gives the day: "On Friday, the State Department announced suspension of the memorandum of understanding signed November 30", and the Friday before Sunday 20 December is **18 December 1981** (computed here). JTA of 11 Jan 1982 records the dispute over who ended it: Weinberger said "our first knowledge that it was no longer in effect came from Israel".
- UN Digital Library record 280165 (https://digitallibrary.un.org/record/280165), "Security Council resolution 497 (1981) [on annexation of the Golan Heights]", date 1981-12-17; record 26751, "adopted by the Security Council at its 2319th meeting, on 17 December 1981", "Adopted unanimously". [V, agent read 2026-10-24; a person's check pending] for the number, date and vote.
- So the memorandum's window as a standing effect is 30 November to 18 December 1981 [S], then suspended. [GAP] whether it was formally terminated or later revived (the design's `usa` row is windowless; the later strategic cooperation of 1983 is not sourced here), the US instruments of suspension, and the exchange of notification under Article IV that would date its entry into force.
- The agent arrested in 1985: sensitivity-list-1977-93.md B19 gives 21 November 1985 ([S] there); not re-read here. The loan guarantees: economy-1977-93.md section 5 (S11 to S13).

### 3.2 `sla`: the name's date

- JTA bulletin of 2 Mar 1978, "751 Lebanese 'sabras'" (https://www.jta.org/archive/751-lebanese-sabras), [S]: "Francis Rezak, political advisor to the south Lebanese army commander, Maj. Saad Haddad". The wording is lower case and generic; the item's dateline was not matched.
- JTA bulletin of 31 Mar 1978, "Eye Witness Report New Center for Lebanon Christian Army" (https://www.jta.org/archive/eye-witness-report-new-center-for-lebanon-christian-army), [S]: Haddad told notables of villages under Israeli control "I declare hereby the establishment of the south Lebanese army"; "all men aged 18-25 will be conscripted"; "In actuality, it functions under the auspices of Israel's army which provides the weapons and other equipment." The OCR found no dateline on this by-lined item; the day of the speech is [GAP] (before 31 March 1978).
- JTA bulletin of 20 Apr 1979, "Begin Says Israel Will Take All Necessary Measures to Maintain Security of Its Northern Border" (title as the archive gives it, truncated; https://www.jta.org/archive/begin-says-israel-will-take-all-necessary-measures-to-maintain-security-of-its-northern-border), [S]: "Yesterday, Haddad declared the 350-square mile region of south Lebanon along the Israel border a 'free Lebanon.'"
- JTA bulletin of 10 Nov 1981, "Haddad Withdraws Resignation; Israelis Pleased" (https://www.jta.org/archive/haddad-withdraws-resignation-israelis-pleased), [S]: the area "will be known as 'Free Lebanon.'" JTA of 16 Feb 1983 (https://www.jta.org/archive/haddad-extends-the-area-controlled-by-his-militia), [S], calls it "the Israel-backed Christian Free Lebanon Forces". JTA of 6 Jun 1984 (https://www.jta.org/archive/s-lebanon-army-commander-says-it-will-be-18-24-months-before-his-troops-can-be-independent-of-idf) names "The South Lebanon Army (SLA)" and its commander "Gen. Antoine Lahad" (Haddad's successor, per en.wiki below, after Haddad's death in January 1984).
- en.wiki "South Lebanon Army", oldid 1380235365 (Q929554), [S, en.wiki oldid 1380235365, Q929554]: the "Free Lebanese Army" was "formed in March 1978"; on 18 April 1979 Haddad proclaimed "Independent Free Lebanon"; Haddad's part "was renamed the South Lebanon Army (SLA) in May 1980".
- **[C]** The press used "south Lebanese army" in March 1978 and "Free Lebanon" (area and forces) in 1979-83; en.wiki dates the name SLA to May 1980. The first JTA bulletin I read that prints "South Lebanon Army" capitalised is 6 Jun 1984 (17 Apr 1984, `rabin-outlines-plan-for-withdrawal-of-idf-from-lebanon-in-3-6-months`, has it lower case, "south Lebanon army", with the earlier lower-case uses above); the searches in section 6 found no use of the name in 1980-82, and that range is not proof of absence. **The window `from 1978` fits the March 1978 proclamation; the name's date stays [C] and [GAP] for a primary.** [GAP] the Lebanese presidential decree dismissing Haddad (en.wiki says No. 1924 of 19 April 1979, not opened) and Israeli decisions on the militia.
- The security zone and its extent: JTA of 16 Feb 1983 says Haddad "extended the area controlled by his militia to virtually all of south Lebanon from the Israel border to the Litani River", an area "far larger than the border strip he originally controlled" [S]. The zone after the 1985 withdrawal is in events-1982-87.md and not re-read here.

### 3.3 `ussr`: relations resume, 1991

- JTA bulletin of 21 Oct 1991, "Restoration of Soviet-israeli Ties Ends 24 Years of Abnormal Relations" (https://www.jta.org/archive/restoration-of-soviet-israeli-ties-ends-24-years-of-abnormal-relations), [S]: "the Soviet Union restored full diplomatic relations with the Jewish state on Friday"; "An agreement on the immediate renewal of relations was signed at a brief ceremony at the Israeli Foreign Ministry by Pankin and his Israeli counterpart, David Levy"; Moscow had "severed ties at the end of the 1967 Six-Day War". Friday before Monday 21 Oct 1991 is **18 October 1991** (computed here). The item has no dateline of its own (a news analysis); another item, JTA of 18 Oct 1991 (https://www.jta.org/archive/soviet-official-arrives-in-israel-with-no-news-yet-on-full-relations), is eve-of coverage: on Thursday 17 October Pankin "did not make the long-anticipated announcement that the Soviet Union was restoring full diplomatic relations" and scheduled "a second, unplanned working session with Levy for noon Friday". The 18th itself rests on the 21 Oct item.
- The steps before it: JTA bulletin of 7 Jan 1991 (https://www.jta.org/archive/the-israeli-flag-flies-in-moscow-for-first-time-in-nearly-24-years), [S]: Israel's consul general Arye Levin "was accredited as consul general on Dec. 25 by outgoing Soviet Foreign Minister Eduard Shevardnadze", Alexei Chistyakov "the first Soviet consul general in Israel since 1967" presented credentials to Levy "in a reciprocal ceremony", and the consulate's formal opening was "Jan. 3" 1991, when the flag was raised, with "Full diplomatic relations still" not restored. JTA of 30 Sep 1991 (https://www.jta.org/archive/soviet-official-says-ussr-to-restore-ties-with-israel-by-peace-conference), [S]: Pankin said on Friday (27 September 1991, computed here) that ties would be restored "surely before the peace conference".
- Afterwards: JTA of 28 Oct 1991, "Israel's Moscow Embassy Reopens After 24 Years" (https://www.jta.org/archive/israels-moscow-embassy-reopens-after-24-years), [S], opened: the Israeli flag was hoisted over the Moscow embassy "last Thursday" (24 October 1991, computed here), and Moscow "resumed full diplomatic relations with Israel on Oct. 18". JTA of 23 Dec 1991 (https://www.jta.org/archive/envoy-from-moscow-arrives-in-israel-unsure-what-country-he-represents), [S]: the first Soviet ambassador in nearly 25 years, Alexander Bubin, "arrived here Saturday" (21 December 1991, computed here) to present credentials to President Herzog on Monday, while "the Soviet Union as a political entity will apparently cease to exist as a nation on Dec. 31" and the Commonwealth of Independent States was formed. So the `ussr` actor's last real days are the end of December 1991; the design's window for the actor is "windowless", and the design's 1991 resumption is 18 October 1991 [S]. The Madrid Conference of 30 October to 1 November 1991 is events-1987-93.md A9 (tagged V there).
- The 1967 break: the 1957-77 notes give it (10 June 1967, announced in the Security Council; `events-1967-70.md` A10, in review there), not re-read here.
- **Open:** [GAP] the signed agreement's text and a Soviet or Israeli Foreign Ministry record; the Knesset's or the Cabinet's act, if any.

### 3.4 `lebanese_forces`: the window

- en.wiki "Lebanese Forces", oldid 1370259226 (Q1146346), [S, en.wiki oldid 1370259226, Q1146346]: "The organization was created in 1976 by Pierre and Bachir Gemayel, Camille Chamoun, and other party leaders"; the history section is headed "Lebanese Forces militia (1976-1990)"; "On 23 March 1994, the Lebanese government headed by Rafic Hariri ordered the dissolution of the Lebanese Forces". The same article says Samir Geagea took over and dissolved the Lebanese Front in the late 1980s, and that after the civil war he created the Lebanese Forces Party.
- JTA bulletin of 16 Oct 1990 (https://www.jta.org/archive/israel-keeping-eye-on-syrian-moves-to-dislodge-militiaman-in-lebanon), [S], quotes Olmert: "the Christian Lebanese forces ... are some of the remaining barriers to full Syrian control". It uses the lower-case phrase, in the sense of the Christian militia; no dateline matched.
- The Taif Agreement's militia clause: a web search result summary (not a source) pointed to the Jewish Virtual Library and the Notre Dame Peace Accords Matrix; neither was opened, and the UN copy answered 404 (see the caveat). Nothing is said here about its words or the 30 April 1991 deadline. [GAP]
- So: start 1976 (en.wiki), against the design's 1977 [C] (the design's window opens when the era does; the LF existed before it). End: the militia era is dated 1976-1990 by en.wiki's heading, the party was dissolved 23 March 1994, and the design's "to 1993" lies between them. **No primary was opened for any of the three dates [GAP].** JTA used "Phalangists" and "Christian militia" in 1977-82: the searches in section 6 found no item that names "Lebanese Forces" as a party in that range.
- Sabra and Shatila and the Lebanese Forces' role: events-1982-87.md (the Kahan commission's own wording on "a unit of the Lebanese forces", tagged there); not re-read here. The 1982 alliance: forces-1977-93.md I3b.

### 3.5 `hezbollah`: the founding

- en.wiki "Hezbollah", oldid 1381358801 (Q41053), [S, en.wiki oldid 1381358801, Q41053]: "Hezbollah was founded in 1982 by Lebanese Shiite clerics in close cooperation with Iran"; "Hezbollah's 1985 manifesto outlined its key objectives".
- JTA's first use of the name that I found: bulletin of 4 Mar 1985, "Cabinet Decides to Begin Second Stage IDF Withdrawal from South Lebanon" (https://www.jta.org/archive/cabinet-decides-to-begin-second-stage-idf-withdrawal-from-south-lebanon), [S]: "Shiite fundamentalists, who call themselves 'Hizbollah' (Party of God), have been responsible for the most serious attacks on the IDF". Three more items of 1985 name it (3 Apr, `background-report-the-kidnapping-of-lebanese-jewish-leaders`; 14 Jun, `rabin-reportedly-tells-un-official-israel-will-not-agree-to-unifil-deployment-along-israeli-borders`; 18 Jun 1985, `reagan-administration-stresses-u-s-will-make-no-deal-with-terrorists-will-not-ask-others-to-do-so`), one glossing it as "Army of God".
- Before the name: JTA bulletin of 7 Nov 1983 (https://www.jta.org/archive/cabinet-considering-increased-security-measures-in-south-lebanon-following-terrorist-attack-on-israe), [S]: "A group calling itself the 'Islamic Jihad' (Holy War) claimed responsibility for the attack in Tyre. It is the same group that took credit for the attacks on the multinational force in Beirut last month and on the U.S. Embassy there last April." Whether that group was Hezbollah is a later attribution, not in the item.
- 1982 [S, en.wiki], primary [GAP], first JTA mention 4 Mar 1985 (a first press mention is not a conflicting founding date); the manifesto ("Open Letter") of 1985 was not opened; its date, 16 February 1985, is given as "(officially)" beside "1982 (initially)" in the en.wiki article's infobox [S, en.wiki oldid 1381358801, Q41053] and in search summaries, so it is [GAP] for a primary. **The design's "from its founding" has no primary source in this note; the defensible window start is 1985 on the press evidence, or 1982 on the encyclopaedia's.**
- Later: Hezbollah's attacks on the IDF and on the SLA in 1991-93 are in the JTA bulletins found by the searches in section 6 (for example 30 Dec 1991, 1-2 Oct 1992, 8 Nov 1993) and were opened on 24 Oct: 30 Dec 1991 (https://www.jta.org/archive/stepped-up-activity-by-hezbollah-harming-security-on-northern-front), [S], Arens says conditions on the northern border "have worsened in recent months" and attributes this to "the growing strength of Hezbollah"; 1-2 Oct 1992 (https://www.jta.org/archive/hezbollah-mounts-heavy-attack-on-israeli-zone-in-south-lebanon, https://www.jta.org/archive/israeli-copters-strike-hezbollah-in-ongoing-battles-in-lebanon), [S], a Hezbollah bombardment of SLA positions over Rosh Hashanah left "at least nine dead"; 8 Nov 1993 (https://www.jta.org/archive/hezbollah-steps-up-attacks-on-idf), [S], "a recent escalation of attacks by the Islamic fundamentalist Hezbollah movement on Israeli positions". Operation Accountability (July 1993) is forces-1977-93.md and events-1987-93.md; not re-read here.

### 3.6 `hamas`: from 1987-12-10

- JTA bulletin of 19 Sep 1988, "Two Arabs Killed, As Authorities Get Tough on Radical Islamic Group" (https://www.jta.org/archive/two-arabs-killed-as-authorities-get-tough-on-radical-islamic-group), [S]: "the emergence of a new organization of Islamic fundamentalists in the territories, known as Hamas. The world [sic] means 'zeal' and is an acronym for the Movement of Islamic Resistance." It is the first item in the returned results that names the organization (the earlier hit, 11 Jul 1988, is a person's surname).
- JTA of 30 Nov 1988 (https://www.jta.org/archive/anger-and-hopelessness-pervade-hebron-on-partition-anniversary), [S]: Hebron "was on strike, by order of the Hamas movement". JTA of 9 Dec 1988 (https://www.jta.org/archive/shin-bet-exposes-93-terrorist-cells-in-israel-and-territories), [S]: five of the cells exposed belonged to Hamas.
- Avalon Project, "Hamas Covenant 1988", "The Covenant of the Islamic Resistance Movement, 18 August 1988" (https://avalon.law.yale.edu/20th_century/hamas.asp), opened 24 Oct, [S] (a library's copy). Only its title and date were used.
- en.wiki "Hamas", oldid 1381511583 (Q38799), [S, en.wiki oldid 1381511583, Q38799]: "In December 1987, the Brotherhood adopted a more nationalist and activist line under the name of Hamas."
- **The design's day, 10 December 1987, has no source here [GAP].** The intifada began on 9 December (events-1987-93.md A1), the encyclopaedia gives the month, and the press names the organization nine months later. The deportation of 1992: UN Digital Library record 282913 (https://digitallibrary.un.org/record/282913), "Security Council resolution 799 (1992) [on deportation of Palestinian civilians from territories occupied by Israel]", date 1992-12-18; record 156019, "adopted by the Security Council at its 3151st meeting, on 18 December 1992", "Adopted unanimously". [V, agent read 2026-10-24; a person's check pending] for the number, date and vote. The deportation itself (the design's section 10 event) is in events-1987-93.md and not re-read here.

### 3.7 `ethiopia`

- JTA bulletin of 29 Jul 1977, "Officials Say They 'know Nothing' of Israeli Presence in Ethiopia" (https://www.jta.org/archive/officials-say-they-know-nothing-of-israeli-presence-in-ethiopia), [S]. Dateline TEL AVIV, July 28 (the item's own). Government officials said they "know nothing" of "an alleged presence of Israeli defense experts in Ethiopia", reported "in recent weeks". So the 1977 window start is a press report of alleged involvement, not an act of state.
- JTA bulletin of 6 Nov 1989, "Restoration of Ties with Ethiopia Raises Hope of New Exodus of Jews" (https://www.jta.org/archive/restoration-of-ties-with-ethiopia-raises-hope-of-new-exodus-of-jews), [S]: "Ethiopia's sudden decision to restore diplomatic ties with Israel, which it broke 16 years ago", "over the weekend" (4-5 November 1989, computed here). JTA bulletin of 19 Dec 1989, "Israel Reopens Its Ethiopian Embassy, Formalizing Resumption of Relations" (https://www.jta.org/archive/israel-reopens-its-ethiopian-embassy-formalizing-resumption-of-relations), [S]: Israel "reopened its embassy in Addis Ababa on Monday" (18 December 1989, computed here). The 1973 break is in the 1957-77 note (section 3.4 there, in review).
- The operations: Moses and Solomon are in events-1987-93.md A10 (Operation Solomon, 24 to 25 May 1991, [S]) and population-1977-93.md (Operation Moses; the Jewish Agency and JTA items of 4 and 7 Jan 1985 and 26 Mar 1985), not re-read here. "The design's `ethiopia` end left open for #52" stands.
- **Open:** [GAP] a primary for the 1977 involvement (the arms supply of 1977-78 is not sourced in this note: a search for it found nothing, see section 6).

## 4. The voter group `new_immigrants_ussr` (design 5.1)

- "From 1989": Tolts 2019, Table 1, as read in population-1977-93.md (tagged V there): emigration from the former Soviet Union to Israel 2.2 thousand in 1988, 12.9 in 1989, 185.2 in 1990. The CBS quote there ("The low period in immigration to Israel ended suddenly during the second half of 1989") is the same turn. Not re-read here, tagged there.
- The arrivals' share of the electorate, their registration to vote and the 1992 election (Knesset 13) are in knessets-1977-93.md and its follow-up; this note read nothing on the voter group itself [GAP]. The name is D11's.

## 5. Sources opened, with the day

All on 24 October 2026.

- **UN Digital Library records** (https://digitallibrary.un.org/search?p=S/RES/<n>&of=xm): resolutions 425 (1978), 497 (1981), 508 (1982), 660 (1990) and 799 (1992), with their draft records.
- **Avalon Project:** https://avalon.law.yale.edu/20th_century/pal03.asp (the 1981 memorandum, read in full) and https://avalon.law.yale.edu/20th_century/hamas.asp (title and date).
- **Wikipedia (en), via the API:** "Lebanese Forces" oldid 1370259226; "South Lebanon Army" oldid 1380235365; "Hezbollah" oldid 1381358801; "Hamas" oldid 1381511583.
- **JTA** (https://www.jta.org/archive/<slug>; bulletin dates; original PDFs at pdfs.jta.org read by OCR): the slugs in sections 1 to 3.
- **Other notes of this era**, cited and not re-read: economy-1977-93.md, events-1977-82.md (B23), events-1982-87.md (C0, the London Agreement), events-1987-93.md (A1, A8, A9, A10), forces-1977-93.md (O1, O10, I3b, I5), population-1977-93.md, sensitivity-list-1977-93.md (B19).
- Looked at and not usable: the pages named in the source caveat.

## 6. Search log: what was run and found nothing, or little

Every query ran on 24 October 2026. JTA searches used the archive's own search interface (`https://www.jta.org/wp-json/wp/v2/archive?search=<query>&after=<date>T00:00:00&before=<date>T00:00:00&per_page=100`, up to 5 pages, so at most 500 results; the engine matches words loosely, not as a phrase, and orders by relevance). "Matched" is the number of returned items whose full text contains the regular expression named; "read" means I read the snippet or the item, and "titles" means I read the result title only.

**`lebanese_forces`, `sla`, `hezbollah`, `hamas`**

| Query | Range | Returned | Matched / read |
|---|---|---|---|
| `Lebanese Forces` (regex `Lebanese Forces`) | 1977-06-01 to 1982-06-30 | 300 on my run, 500 (the cap) on the reviewer's re-run | 0 matched on both |
| `Phalangists` (same regex) | 1977-06-01 to 1982-06-30 | 24 | 0 matched |
| `Lebanese Forces` (same regex, any case) | 1982-07-01 to 1983-12-31 | 300 | 8 matched; first 3 snippets read (20 Jun, 13 Jul, 25 Jul 1983): the Sabra and Shatila killings "by Lebanese forces", "non-Lebanese forces", and "pro-Syrian Lebanese forces", none naming the party |
| `Lebanese Forces` (same regex, any case) | 1990-01-01 to 1993-12-31 | 490 | 5 matched, all 5 snippets read (16 Oct 1990, 9 and 12 Jul 1991, 2 Oct 1992, 26 Aug 1993): "Christian Lebanese forces", "non-Lebanese forces", the Lebanese army, "Israeli-backed Lebanese forces" (the SLA), and the army again |
| `Haddad militia southern Lebanon` (regex `South Lebanon Army\|Free Lebanon...`) | 1977-06-01 to 1984-12-31 | 65 | 4 matched, all read (20 Apr 1979, 10 Nov 1981, 8 Feb 1983, 16 Feb 1983) |
| `South Lebanon Army` (regex `South Lebanon(ese)? Army`, any case) | 1977-06-01 to 1985-12-31 | 500 (the cap) | 104 matched on my run (93 case-sensitive on the reviewer's); the 5 earliest of the matched read, the earliest is 17 Apr 1984 (lower case), the earliest capitalised 6 Jun 1984 |
| `South Lebanon Army` (same) | 1978-01-01 to 1979-12-31 | 136 | 2 matched, both read (2 and 31 Mar 1978; the regex included "south Lebanese army") |
| `South Lebanon Army` (same) | 1980-01-01 to 1980-12-31 | 37 | 0 matched |
| `South Lebanon Army` (same) | 1981-01-01 to 1982-12-31 | 204 | 0 matched |
| `Haddad` (same regex) | 1980-01-01 to 1980-12-31 | 50 | 0 matched |
| `Haddad militia` (regex `Haddad`) | 1978-03-01 to 1978-12-31 | 13 | 13 matched; 3 read (23, 27, 31 Mar 1978) |
| `Hezbollah` (regex `Hezbollah\|Hizbollah\|Hizballah\|Hizb Allah\|Hizbullah\|Hezballah\|Party of God\|Islamic Jihad`) | 1982-06-01 to 1983-12-31 | 0 | - |
| `Hezbollah` (same) | 1984-01-01 to 1984-12-31 | 0 | - |
| `Hezbollah` (same) | 1985-01-01 to 1985-06-30 | 4 | 4 matched, all read (4 Mar, 3 Apr, 14 Jun, 18 Jun 1985) |
| `Hizbollah`, `Hizballah`, `Hizbullah` (one query each, same regex) | 1982-06-01 to 1985-02-28 | 0 each | - |
| `Party of God` (same regex) | 1982-06-01 to 1985-02-28 | 449 | 0 matched |
| `Islamic Jihad Lebanon` (same regex) | 1982-06-01 to 1985-02-28 | 7 | 7 matched; 2 read (7 Nov and 13 Dec 1983) |
| `Hamas` (regex `Hamas\|Islamic Resistance Movement`) | 1987-12-01 to 1988-12-31 | 17 | 4 matched, all 4 read (11 Jul, 19 Sep, 30 Nov, 9 Dec 1988) |
| `Moslem Brotherhood Gaza` (regex `Brotherhood\|Hamas`) | 1987-12-01 to 1988-03-31 | 0 | - |

**`ethiopia`**

| Query | Range | Returned | Matched / read |
|---|---|---|---|
| `Ethiopia Israel arms Mengistu` (regex `Ethiopia`) | 1977-06-01 to 1978-12-31 | 0 | - |
| `Ethiopia` (same) | 1977-06-01 to 1978-12-31 | 23 | 23 matched; 5 snippets read (15 Jun to 19 Sep 1977), only the 29 Jul 1977 item concerns an Israeli presence |
| `Ethiopia Israel diplomatic relations restored` (regex `diplomatic relations\|restor\|resum`) | 1989-10-01 to 1990-01-31 | 6 | 5 matched; 5 snippets read (6, 20, 23 Nov, 19 Dec 1989, 18 Jan 1990) |

**`direct_absorption`, `home_front_readiness`, economics, `intifada_response`**

| Query | Range | Returned | Matched / read |
|---|---|---|---|
| `direct absorption` (regex `direct absorption\|direct-absorption`) | 1989-01-01 to 1992-12-31 | 431 | 23 matched; the 10 earliest read as snippets (12 Jun 1989 to 1 Mar 1990) |
| `direct absorption immigrants` (regex `direct absorption\|direct-absorption\|cash grant\|absorption basket`) | 1987-01-01 to 1989-05-31 | 56 | 1 matched by the regex (24 Jun 1988); the 13 Dec 1988 item ("absorbed directly") was found in the same run by a looser regex and read |
| `absorption centers Jewish Agency government agreement closing` (regex `absorption centers?`) | 1988-06-01 to 1989-06-12 | 0 | - |
| `gas masks distribution Israel` (regex `gas mask`) | 1990-08-01 to 1991-01-31 | 38 | 22 matched on my run (21 on the reviewer's); the 12 earliest read as snippets (6 Aug to 3 Oct 1990) |
| `Israel pound floated devaluation economic policy Simcha Ehrlich` (regex `pound\|float\|devalu`) | 1977-10-15 to 1977-11-30 | 1 | 1 matched, read (31 Oct 1977) |
| `Aridor exchange rate devaluation crawling peg` | 1977-10-01 to 1977-12-31 | 0 | - |
| `Aridor` (regex `deficit\|Aridor`) | 1979-01-01 to 1983-12-31 | 500 (the cap) | 140 matched; the 6 earliest read as snippets (7 Jul 1980 to 21 Jan 1981), 2 opened for the notes above; none gave a deficit figure |
| `Aridor budget deficit finance minister` (regex `deficit`) | 1981-01-01 to 1981-12-31 | 0 | - |
| `package deal Histadrut wage price freeze` (regex `package deal\|price freeze\|wage`) | 1984-10-01 to 1985-08-31 | 19 | 19 matched; the 8 earliest read as snippets (24 Oct to 16 Nov 1984) |
| `Cabinet approves budget cuts Modai Peres 1985 plan` (regex `wage\|freeze\|budget`) | 1985-06-01 to 1985-07-31 | 0 | - |
| `economic stabilization plan` (same regex plus `devalu\|subsid`) | 1985-06-25 to 1985-07-20 | 3 | 3 matched, all read as snippets (1, 2, 5 Jul 1985) |
| `Rabin beatings force riots territories` (regex `beat\|might`) | 1988-01-01 to 1988-03-31 | 16 | 13 matched; 8 snippets read (20 Jan to 18 Feb 1988), 1 opened (20 Jan) |

**`ussr` and the 1981 memorandum** (searches through `jta.py`, which prints at most 20 results and lists titles)

| Query | Range | Returned | What I read |
|---|---|---|---|
| `Israel Soviet Union restore diplomatic relations` | 1991-09-01 to 1991-12-31 | 13 | titles; the 21 Oct, 30 Sep, 28 Oct and 23 Dec items read |
| `Soviet Israel diplomatic relations restored Moscow` | 1990-12-01 to 1991-08-31 | 18 | titles; the 7 Jan 1991 item read |
| `memorandum of understanding strategic cooperation` | 1981-11-01 to 1982-02-28 | 20 (the page cap) | titles; the items of 3 Dec 1981 (two), 21 Dec 1981 and 11 Jan 1982 read |

**UN Digital Library**, `https://digitallibrary.un.org/search?p=S/RES/<n>&of=xm&rg=5`: for 425, 497, 508, 660 and 799 each search returned three records (the resolution, a bibliographic record and the draft), read as metadata. The same query with the symbol in quotation marks and the symbol including the year, as the earlier tool had it, returned no records on this run for these symbols, so the unquoted form was used.

**FRUS 1981-88**: see the source caveat. **Tried for a primary and not opened:** the Taif text (two URLs, 404), the Hezbollah Open Letter (a web search returned commentary and library records only), the Pentagon copy of the memorandum and the Israeli Foreign Ministry's page.

## 7. Gaps

- The June 1977 level and the series of the deficit; the Bank of Israel's account of the exchange-rate regime 1977-79; Cabinet and agreement texts for 1981, 1984 and 1985 (1.1 to 1.3).
- The orders of January 1988 in their own text; the start of plastic bullets (2.2).
- The contract and the Cabinet decision behind direct absorption; the 1989-90 grant schedule (2.3).
- The first day gas masks were handed out; the Civil Defense Law and shelter rules; the Home Front Command's founding (2.4).
- Any formal termination or revival of the 1981 memorandum; the exchange of notification (3.1).
- The day of Haddad's proclamation of March 1978; the SLA name's date; the presidential decree of April 1979 (3.2).
- The signed Soviet-Israeli agreement of 18 October 1991 (3.3).
- Primaries for the Lebanese Forces' three dates; the Taif text (3.4).
- A primary for Hezbollah's founding; the Open Letter (3.5).
- The day Hamas was founded and the design's 10 December 1987 (3.6).
- A primary for Israel's involvement in Ethiopia in 1977 (3.7).
- Israeli primary sources for every row: the Knesset and the Foreign Ministry pages were not reachable from this box.
