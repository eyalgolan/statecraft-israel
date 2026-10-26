# Economy research, 1977–1993: prices, stabilization and financing

Researched 2026-10-23. **Research only; agent-checked draft, not human fact-checked.** Refs #148. Second pass **2026-10-25** (an AI agent, for Beri): the [GAP] rows named in the owner's 24 October comment on #148 were researched again; each addition carries the day the agent opened its source, and a person's check is still pending. The four first-pass cells of the status table (Bank crisis, US aid, Loan guarantees, Defence share) were re-tagged on 25 October only to add the date and the "a person's check pending" wording, in line with the note's own statement that its sources were checked on 2026-10-23 by an AI agent. This note does not choose D11, change content, implement era rules, calibrate effects or approve a price display. The [owner's research ruling](https://github.com/eyalgolan/statecraft-israel/issues/148#issuecomment-6340404323) keeps those implementation tasks paused.

**Reading key.** [V] marks a claim checked on its cited page, opened and read by an AI agent; it is not human sign-off. [S] identifies single-source material or a summary not directly opened. [C] preserves conflicting accounts with their sources. [R] identifies retrospective evidence. [GAP] marks incomplete evidence. Sources were checked on 2026-10-23 by an AI agent only. Quoted fragments reproduce the cited sources.

## 1. Scope and risks

The evidence must distinguish monthly changes, twelve-month changes and changes in annual averages; an index's statistical base from currency units; a currency's issue date from the date prices were quoted in it; and an aid request, appropriation, payment and loan guarantee. GDP and GNP are different denominators. None of those distinctions supplies a simulation rule.

| Owner's anchor | Coverage here | Remaining work |
|---|---|---|
| Monthly CPI, 1979–1985 | [V] All 84 months, source S1; catalogue S2 | Published 1985 scale link now documented in S15; resolve API metadata and verify any later cross-base use |
| CPI outside 1979–1985 | [V, agent read 2026-10-25; a person's check pending] Monthly 1977–1978 and 1986–1993, S17; annual averages, S4 | Years before 1977 and after 1993 not retrieved; levels across base-label changes not linked |
| Inflation series / D11 | [V] CBS monthly data; BoI annual and annual-average comparisons, S1–S4 | Owner chooses the measure and its eventual use |
| Bank crisis, 1983 | [V, agent read 2026-10-23; a person's check pending] Contemporary BoI account, S5; explicitly retrospective study, S6 | Arrangement outlined by S6 and S22; closure and report dates from dated reporting, S18–S19; the inquiry report and the arrangement's decision text not read |
| Stabilization, 1985 | [V] BoI's contemporary prices chapter, S3 | Cabinet decision and full fiscal implementation records not read |
| US aid | [V, agent read 2026-10-23; a person's check pending] Dated request and signing statement, S9–S10 | Enacted 1985 ESF amount in S16; FY1977–1993 schedule in section 5 from a secondary compilation (S20); payments not established |
| Loan guarantees | [V, agent read 2026-10-23; a person's check pending] Official codified authority and GAO summary, S11–S12 | Actual issues and deductions under the 1992 program still not established (section 5) |
| Defence share of GDP | [V, R, agent read 2026-10-23; a person's check pending] CBS 1579, Table 4: both GDP columns for 1977–1993, S14 | BoI 1995 vintage compared in section 6; later vintages and any gameplay measure stay open |
| Sheqel dates | [V] BoI currency history and dated appendix, S7–S8 | No display-date decision is made |
| 1957–1977 / 1977 price-base bridge | [V] S15's published link coefficients, section 2 | Which base the 1957–1977 era uses stays with #24/#116 |

## 2. CPI: the monthly evidence

[V] The CBS catalogue calls code **120010** “Consumer Price Index - General” and marks it monthly (S2). The date-bounded S1 response contains 84 unique year/month records, January 1979 through December 1985; its paging metadata reports 84 total items, one page and no next page. These are historical observations retrieved on the check date, not a claim to have opened every original monthly release.

The following cells reproduce S1's `percent` field, in percent. Selected month-to-month readings are independently consistent with BoI's account: December 1984 3.7%, January 1985 5.3% (S3, p. 36). This note has not annualized the returned percentages; seasonal-adjustment metadata was not established in this research.

| Year | Jan | Feb | Mar | Apr | May | Jun | Jul | Aug | Sep | Oct | Nov | Dec |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1979 | 4.9 | 2.4 | 5.6 | 8.7 | 4.8 | 3.5 | 5.5 | 8.4 | 8.0 | 7.8 | 9.7 | 8.1 |
| 1980 | 7.3 | 4.9 | 5.1 | 10.2 | 9.5 | 4.6 | 4.4 | 8.2 | 7.3 | 11.0 | 9.4 | 6.0 |
| 1981 | 7.3 | 5.5 | 4.8 | 10.7 | 3.3 | 2.7 | 6.1 | 3.9 | 8.1 | 9.0 | 5.8 | 5.2 |
| 1982 | 8.3 | 5.7 | 5.1 | 10.7 | 6.2 | 6.0 | 9.2 | 7.9 | 7.6 | 8.4 | 6.5 | 5.5 |
| 1983 | 8.5 | 6.1 | 5.6 | 13.3 | 5.5 | 3.6 | 6.3 | 7.2 | 9.0 | 21.1 | 15.2 | 11.6 |
| 1984 | 14.9 | 12.0 | 10.7 | 20.6 | 14.3 | 13.4 | 12.4 | 16.5 | 21.4 | 24.3 | 19.5 | 3.7 |
| 1985 | 5.3 | 13.5 | 12.1 | 19.4 | 6.8 | 14.9 | 27.5 | 3.9 | 3.0 | 4.7 | 0.5 | 1.3 |

**Index-level warning.** In S1, December 1984 has `currBase.value: 10754.8`, and January 1985 has
`113.2`, while both carry `baseDesc: Average 1980`; `prevBase` is null. A direct ratio of those levels
would contradict the reported January change. S1's monthly percentages above are transcribed without repair.

### The 1985 index scale: an explicit published link

[V, R] CBS Table 3.5, whose header says it was last produced for Price Statistics Monthly No. 8/2008, shows
December 1984 as **10,754.8** and January 1985 as **113.2**. Its scale band specifies a **100-fold scale
reduction** from January 1985; footnote (2) explicitly instructs multiplying the **1985** indices by
**100** to link them to earlier years (S15, p. 1). This is a published instruction, not a factor calculated
from the two monthly values or a currency-conversion rule. It is limited to the stated 1985 comparison;
footnote (3) gives separate linking instructions for 1986 onward.

This accounts for the scale boundary in the published level table. The API's unchanged `Average 1980`
label remains unexplained **[GAP]**. A separate two-month retrieval with `coef=true` returns coefficient
arrays but does not include the factor 100 in those arrays (S2); it does not establish the cause or meaning
of the API metadata. No values are altered here, no API defect is diagnosed, and no D11 display base or
cross-era gameplay ratio is selected.

### CPI outside 1979–1985: second pass (2026-10-25)

[V, agent read 2026-10-25; a person's check pending] The same CBS code **120010** (S2) was retrieved a second time on 2026-10-25 for two date-bounded ranges (S17). The January 1977–December 1978 response holds 24 unique year/month records (paging metadata: 24 total items, one page, no next page). The January 1986–December 1993 response holds 96 unique year/month records (96 total items, one page, no next page). Cells below reproduce the `percent` field, in percent, unrepaired, like the 1979–1985 table. The note's era starts in 1977, so January 1977–December 1978 and 1986–1993 close the "prices outside 1979–1985" coverage gap for the era; 1994 onward and years before 1977 were not retrieved.

| Year | Jan | Feb | Mar | Apr | May | Jun | Jul | Aug | Sep | Oct | Nov | Dec |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1977 | -0.2 | 0.9 | 2.0 | 2.4 | 2.4 | 2.4 | 1.5 | 3.9 | 3.8 | 3.6 | 11.8 | 2.0 |
| 1978 | 2.3 | 1.6 | 3.7 | 5.5 | 1.8 | 2.0 | 2.4 | 2.4 | 3.0 | 5.7 | 6.2 | 3.4 |

| Year | Jan | Feb | Mar | Apr | May | Jun | Jul | Aug | Sep | Oct | Nov | Dec |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1986 | -1.3 | 1.6 | 1.5 | 3.3 | 1.6 | 1.6 | 0.0 | 1.1 | 1.9 | 2.4 | 2.9 | 1.5 |
| 1987 | 2.1 | 1.0 | 1.3 | 2.2 | 0.6 | 1.0 | 0.2 | 1.2 | 0.9 | 1.5 | 1.6 | 1.3 |
| 1988 | 1.6 | 0.8 | 1.6 | 2.7 | 1.2 | 0.3 | 0.1 | 0.8 | 1.7 | 2.4 | 1.6 | 0.5 |
| 1989 | 4.7 | 1.7 | 0.5 | 2.6 | 0.9 | 1.2 | 0.6 | 1.2 | 1.6 | 1.8 | 1.3 | 1.1 |
| 1990 | 1.1 | 0.5 | 1.2 | 2.5 | 1.6 | 0.7 | 1.6 | 1.6 | 2.1 | 1.7 | 1.3 | 0.4 |
| 1991 | 1.3 | 0.6 | 1.2 | 2.2 | 1.9 | 2.0 | 3.0 | 2.1 | 1.6 | 0.4 | 0.1 | 0.3 |
| 1992 | 0.0 | 1.0 | 1.5 | 1.8 | -0.4 | 0.1 | 0.7 | 0.8 | 1.3 | 0.5 | 0.6 | 1.1 |
| 1993 | 1.3 | 1.2 | 1.3 | 1.4 | 0.3 | 0.2 | 0.1 | 1.0 | 1.0 | 1.4 | 0.8 | 0.8 |

**Base labels.** S17 labels all of 1977–1978 `Average 1976`; all of 1986 and 1987 `Average 1985`; and all of 1988–1993 `Average 1987`. `prevBase` is null in every record. December 1987 carries `189.4` on the `Average 1985` label and January 1988 carries `108.4` on the `Average 1987` label. Index levels across a label change are therefore not comparable, and this note does not link them: S15 (section 2) documents links only up to the 1985 scale and its footnote (3) for 1986 onward was not re-read in this pass. The `percent` field is used here as returned.

Two annual measures for the same years (S17 December `percentYear`, S4 Table A6 total CPI average annual change, read as a rendered image of printed p. 236):

| Year | S17 December `percentYear` (December over previous December) | S4 Table A6, annual-average change (CPI, total) | `baseDesc` on all twelve months in S17 |
|---|---:|---:|---|
| 1977 | 42.5% | 34.6% | Average 1976 |
| 1978 | 48.1% | 50.6% | Average 1976 |
| 1986 | 19.7% | 48.1% | Average 1985 |
| 1987 | 16.1% | 19.9% | Average 1985 |
| 1988 | 16.4% | 16.3% | Average 1987 |
| 1989 | 20.7% | 20.2% | Average 1987 |
| 1990 | 17.6% | 17.2% | Average 1987 |
| 1991 | 18.0% | 19.0% | Average 1987 |
| 1992 | 9.4% | 11.9% | Average 1987 |
| 1993 | 11.2% | 10.9% | Average 1987 |

[V, agent read 2026-10-25; a person's check pending] The two columns differ in the way the 1982–1985 columns above do (December over December against annual average) and are not rival estimates of one statistic. For 1977–1978 the S4 column is one vintage (BoI 1995 report); S17 is a 2026 retrieval, so their vintage equivalence is not established.

**Contemporary cross-check from a 1983 wire report.** [V, agent read 2026-10-25; a person's check pending] The JTA bulletin of 17 October 1983 (S18), item dated Jerusalem, 16 October, says: "The consumer price index for September, published Friday, showed a nine percent increase, a record high for that month." It also says "Prices rose by 86.8 percent during the first nine months of the year." S1 gives September 1983 as 9.0%. Compounding S1's January–September 1983 monthly values: 1.085 × 1.061 × 1.056 × 1.133 × 1.055 × 1.036 × 1.063 × 1.072 × 1.090 = 1.8698, i.e. 87.0% (derived, not [V]), against the bulletin's 86.8%, a 0.2 percentage point gap that this note does not explain (compounding rounded one-decimal monthly values was not tested as the cause). The same item says "Inflation in October is expected to soar to a rate of 16-20 percent"; S1 gives October 1983 as 21.1%. That is a forecast, not a measurement.

### Two annual measures, not interchangeable values

| Year | S1 December `percentYear` (December over previous December) | S4 Table A7, annual-average change |
|---|---:|---:|
| 1982 | 131.5% | 120.4% |
| 1983 | 190.7% | 145.6% |
| 1984 | 444.9% | 373.8% |
| 1985 | 185.2% | 304.6% |

[V] S4's Table A7 is explicitly headed “average annual rate of change”; S3's Table III-1 rounds the period increases for 1982–1985 to 132%, 191%, 445% and 185%. Thus 373.8% and 444.9% for 1984 need not be rival estimates of the same statistic. Likewise 304.6% and 185.2% for 1985 describe different comparisons. The original IMF series underlying the era plan's comparison was **not** opened here; this note does not attribute these BoI values to IMF or establish IMF/CBS vintage equivalence.

**Proposal for D11, not a decision:** use CBS general CPI 120010 monthly percentage changes as the research anchor for the 1979–1985 path, with December/December checks clearly labelled. It preserves the within-year break that an annual average hides and comes directly from the national statistical publisher. Costs: this is a current historical-data retrieval, not a frozen contemporary release; the published scale link does not explain the API metadata; rounded monthly percentages do not establish exact index levels; and the remaining years still need collection. Annual averages remain useful for annual comparisons, but should not be substituted for monthly changes. Any later chaining, turn mapping or calibration needs separate owner-approved work.

### Price bases across the eras

[V] S15 lists successive CPI bases and their published link coefficients (p. 1). Footnote (1) instructs
readers to multiply the updated indices by the coefficient printed above them to express those indices
on the preceding base. The relevant bands give **September 1951 = 100**; **January 1959 = 100**, coefficient
**2.753**; **average 1964 = 100**, **1.347**; **average 1969 = 100**, **1.237**; **average 1976 = 100**,
**4.112**; and **average 1980 = 100**, **8.349**. From January 1985 the indices are reduced 100-fold,
as footnote (2) explains. **June 1977 is 128.8 on the average-1976 base**. The choice of base for the
1957–1977 era remains with #24 and #116. These published links support later conversion once that base
is chosen; this note selects no single cross-era ratio or gameplay rule.

## 3. Bank crisis and stabilization

[V] BoI's **1983** prices chapter, p. 38 (S5), describes October measures including ending support for bank-share prices, a large devaluation, subsidy cuts and a decision to reduce the budget. This is a contemporary institutional account of the policy sequence, not the text of the share arrangement.

[V, R] Blass and Grossman's **May 1996** discussion paper, introduction p. 3 (S6), dates the stock-exchange shutdown to **6 October 1983** and says it lasted eighteen days after heavy bank-share selling and bank purchases of their own shares strained liquidity. The introduction says “On October 6, 1983” and “shut down for eighteen days”. This is later research hosted by BoI, not a primary exchange notice or an inquiry finding. Its cover says the authors' views do not necessarily represent BoI. The original arrangement, exact legal ownership transitions and the inquiry report remain [GAP]; no legal conclusion about individual bankers is drawn here.

**Second pass (2026-10-25): dated contemporaneous items on the 1983 closure.** The JTA reproduces each bulletin as a PDF ("See Original Daily Bulletin"); the event dates below come from the datelines printed on the items in those PDFs, which were opened and read as rendered images (S18).

- [V, agent read 2026-10-25; a person's check pending] Bulletin of **17 October 1983**, item datelined **Jerusalem, Oct. 16 (JTA)**: "The Tel Aviv Stock Exchange remained closed for the seventh consecutive day and there was no indication when trading would be resumed." This is the item Ella Sharakanski (EllaSharakanski) found and posted on #148 on 24 October; the owner replied the same day that it is a good source to cite beside S6. The 24 October reopening (below) is the reported end of the same closure. It does not by itself fix the start date: the count depends on which days the exchange traded, and the trading week of the exchange in 1983 was not established in this pass **[GAP]**.
- [V, agent read 2026-10-25; a person's check pending] Bulletin of **24 October 1983**, item datelined **Tel Aviv, Oct. 23 (JTA)**, headed "Stock Exchange Prepared to Reopen Monday After a Closure of Two Weeks": "The Tel Aviv stock exchange prepared to reopen tomorrow after a two-week suspension of trading" and "The stock exchange closed down on October 9 in order to end the panic sale of commercial bank shares in the face of a rapidly weakening Shekel." It adds "A government scheme to sustain the shares at a realistic price is still under negotiation."
- [V, agent read 2026-10-25; a person's check pending] Bulletin of **25 October 1983**, item datelined **Jerusalem, Oct. 24 (JTA)**, headed "Tel Aviv Stock Exchange Reopens": "when the Tel Aviv stock exchange reopened for trading this morning after an 18-day suspension". The reopening day is therefore **Monday 24 October 1983**. It reports bank share prices "down 17 percent in Shekel value and 40 percent in Dollar value compared to the levels before the exchange closed" and other shares down "10-15 percent".

**[C] Start date of the closure.** S6 (p. 3) says the exchange "was shut down" on **6 October 1983** "for eighteen days"; the JTA item datelined 23 October says it "closed down on October 9". Both values stay. Arithmetic: counting 6 October as day 1, the 18th calendar day is 23 October, so a 6 October start gives reopening on 24 October, which matches the JTA 24 October dateline; counting from 9 October, 9 through 23 October is 15 calendar days. 6 October 1983 was a Thursday and 9 October a Sunday. In the Knesset record (S19) one member refers (agent's translation) to a concern present "from 5 October" and another to an "earthquake" lasting from 5 October to 10 October 1983; neither speaker is dating the exchange closure in the passages read, so those are not counted as a third value for it. The exchange's own notice of closure was not searched for **[GAP]**.

**The bank-share arrangement, as described by S6.** [V, R, agent read 2026-10-25; a person's check pending] S6 (p. 3) says that during the closure "the government devalued the Shekel and took control of five of the banks, converting their shares into government guaranteed zero coupon bonds maturing within 5 to 6 years at face values of 85 to 117 percent of pre-closure dollar market values". This is a later description. The government's original decision or contract text was not found, and in the Knesset record (S19) a member says (agent's translation) that there is an issue the commission did not deal with, the question of the arrangement (original: "יש נושא שהוועדה לא עסקה בו והוא: שאלת ההסדר."). Same record, a member asks (agent's translation) whether the cost to the state treasury is 2 billion dollars or 2.5 billion dollars, calling it a matter of dispute; this is a spoken estimate by one member, not a finding.

**The inquiry report.** The commission of inquiry on the regulation (manipulation) of bank shares is called the Bejsky commission in S6's reference list ("Commission of Inquiry (Bejsky Commission) Report, April 1986", printed p. 30) and "Beisky" in the JTA. The report itself was not opened **[GAP]**. Dated items opened:

- [V, agent read 2026-10-25; a person's check pending] JTA bulletin of **22 April 1986**, item datelined **Jerusalem, April 21 (JTA)**: "a report released Sunday night by the special commission set up to investigate the collapse of bank shares and other stocks that touched off a financial panic three years ago" and "a 550-page document representing 13 months of labor by the commission". Sunday before 21 April 1986 (a Monday) is **20 April 1986**. It says the commission, "headed by Supreme Court Justice Chaim Beisky", recommended "that the Governor of the Bank of Israel and the heads of the five largest banks be forced to resign within 30 days".
- [V, agent read 2026-10-25; a person's check pending] JTA bulletin of **23 April 1986**, item datelined **Jerusalem, April 22 (JTA)**: Moshe Mandelbaum, Governor of the Bank of Israel, "resigned Monday", that is 21 April 1986.
- [V, R, agent read 2026-10-25; a person's check pending] Knesset State Control Committee (11th Knesset) protocol no. 138, **Monday 21 April 1986**, agenda item, in the agent's translation, the report of the commission of inquiry on the regulation of bank shares (S19). The chairman, D. Libai, says: "אתמול קיבלנו לידינו את הדו"ח" (agent's translation: "Yesterday we received the report into our hands"), which is consistent with 20 April. Another member says (agent's translation) that the commission worked a whole year and that behind the report stand 50 thousand pages of documents. That page count differs from the JTA's "550-page document" and both are kept: they may count different things (report against supporting records); this was not established.

Other dates for the report seen only in search results (not opened) are not used here.

**The arrangement, as described by the State Comptroller in 1993.** [V, agent read 2026-10-25; a person's check pending] State Comptroller of Israel, special report "Preparation for the sale of the bank shares under the arrangement" (Hebrew title: ההיערכות למכירת מניות הבנקים שבהסדר), Jerusalem, April 1993, PDF pp. 8–11 (S22), read as extracted text (agent's translation follows, not quotation). It says the government gave the banks a right to "redeem" under the arrangement the shares the banks' own corporations had bought for the regulation (the "nostro" shares); that the arrangement fixed the nostro shares to be "redeemed" at the end of five years (October 1988) and not traded before then without the prior agreement of the Finance Ministry and the Bank of Israel; that "interested parties" as defined were not given the right to redeem; and that the interim period under the arrangement was meant to last until the end of October 1993, with companies set up by each bank's controlling owners holding the shares, financed by loans from the state budget. It also records that discussions chaired by the Deputy Prime Minister in October 1983 moved some shares from the interested-party list to the nostro category. Set beside S6's description (bonds "maturing within 5 to 6 years"), the dates are of the same order; the two accounts were not reconciled further, and the arrangement's original decision text is still not read **[GAP]**. The report refers to the Comptroller's earlier report on the bank-share crisis (chapter 8, p. 65 as cited in S22), which was not opened.


[V] S3 (pp. 36–38) dates the stabilization program's launch to **1 July 1985** and describes exchange-rate stabilization, price controls, partial suspension of wage indexation, fiscal restraint and monetary restraint. Its wording on p. 36 is “stabilization program launched on July 1”. It reports about 12% average monthly inflation in the first half of 1985 and 2.7% in the final five months. That latter interval excludes July; S1 retains July's 27.5% rise. These are historical observations, not event-effect magnitudes.

## 4. Currency chronology

| Date | What the opened source actually dates | Source |
|---|---|---|
| 22 February 1980 | Sheqel declared legal tender | S7, introductory history |
| 24 February 1980 | First sheqel notes entered circulation; one sheqel equalled ten pounds | S7; S8, p. 64 |
| 30 September 1980 | Prices and exchange rates quoted in sheqalim | S8, p. 64 |
| 31 March 1984 | Pound ceased to be legal tender | S8, p. 64 |
| 4 September 1985 | New sheqel entered circulation; one new sheqel equalled 1,000 sheqalim | S8, p. 64 |
| 1 January 1986 | Prices and exchange rates quoted in new sheqalim | S8, p. 64 |
| 4 September 1986 | Sheqel ceased to be legal tender | S8, p. 64 |

[V, R] S8 is an official **2000** retrospective appendix. Its date phrases are “February 24, 1980”,
“September 30, 1980”, “March 31, 1984”, “September 4, 1985”, “January 1, 1986” and “September 4, 1986”;
it distinguishes circulation from quotation of “prices and exchange rates” (p. 64). Circulation on 4 September 1985 and price quotation from 1 January 1986 answer different questions. Neither this note nor the denomination ratios select the era's display date under D11 or define its real-price base.

## 5. US financing: distinguish authority from payments

[V] The White House public-papers entry for **15 May 1985** records a presidential request for $1.5 billion for Israel in FY1985 and $1.2 billion in FY1986, linked to economic reforms (S9, dated entry). The dated entry says “This request would provide $1.5 billion to Israel” and “$1.2 billion in fiscal year 1986 to help Israel”. These are requested appropriations, not verified disbursements. The signing statement published **16 August 1985** says H.R. 2577 appropriated funds for Israel, Egypt and Jordan; its editorial note dates approval to **15 August** and identifies Public Law 99-88 (S10). That statement does not give Israel's amount. The enacted ESF amount is documented below in S16; actual payment dates and other accounts remain [GAP]. Neither the request nor the statutory allocation establishes a payment on either August date.

**Enacted 1985 ESF allocation [V].** Public Law 99-88, dated **15 August 1985**, appropriates an additional
**$2,258,000,000** for the Economic Support Fund. Its continuation specifies **$1,500,000,000 for Israel**
and provides for the Israel and Egypt funds as cash grant transfers. The amount clause reads
“$1,500,000,000 shall be available for Israel”. The paragraph states availability for
obligation until **30 September 1986** (99 Stat. 323–324; S16). This supplies the enacted ESF amount
alongside the earlier presidential request, rather than inferring it from that request.

This is not evidence of a disbursement's date or amount, a military-financing allocation or an annual
all-aid total. The Jordan-specific timing limits on the same page are not assigned to Israel. Other law
pages, cross-referenced laws, later amendments and payment records were not reviewed here. The full
1977–1993 aid schedule remains [GAP]; no historical game text or calibration is supplied.

### US aid by fiscal year, 1977–1993: second pass (2026-10-25)

[S, agent read 2026-10-25; a person's check pending] CRS report RL33222, Jeremy M. Sharp, "U.S. Foreign Aid to Israel", **16 September 2010**, Table A-2 "U.S. Assistance to Israel, FY1949-FY1996", printed pp. 25–27 (S20), read as rendered images. Millions of dollars, as printed (the table states no inflation adjustment). Tagged [S] because it is a congressional-staff compilation: the underlying USAID "Greenbook" table was not opened, and the pages read print no source line under the table. The report's own note on the table says funds such as the Arrow missile research money and loan guarantees are not included.

The table also prints columns for Food for Peace loans and grants, Export-Import Bank loans, Jewish refugee resettlement grants, American schools and hospitals grants, other loans, cooperative development grants and other grants; the last column below sums them. Check: for 1977, 500.0 + 500.0 + 245.0 + 490.0 + 27.5 = 1,762.5, the printed total; for 1983, 950.0 + 750.0 + 785.0 + 20.6 = 2,505.6, the printed total. The same sum was run for every year shown and matched the printed total.

| Fiscal year | Total | Military loan | Military grant | Economic loan | Economic grant | All other columns, summed |
|---|---:|---:|---:|---:|---:|---:|
| 1977 | 1,762.5 | 500.0 | 500.0 | 245.0 | 490.0 | 27.5 |
| 1978 | 1,822.6 | 500.0 | 500.0 | 260.0 | 525.0 | 37.6 |
| 1979 | 4,888.0 | 2,700.0 | 1,300.0 | 260.0 | 525.0 | 103.0 |
| 1980 | 2,121.0 | 500.0 | 500.0 | 260.0 | 525.0 | 336.0 |
| 1981 | 2,413.4 | 900.0 | 500.0 | - | 764.0 | 249.4 |
| 1982 | 2,250.5 | 850.0 | 550.0 | - | 806.0 | 44.5 |
| 1983 | 2,505.6 | 950.0 | 750.0 | - | 785.0 | 20.6 |
| 1984 | 2,631.6 | 850.0 | 850.0 | - | 910.0 | 21.6 |
| 1985 | 3,376.7 | - | 1,400.0 | - | 1,950.0 | 26.7 |
| 1986 | 3,663.5 | - | 1,722.6 | - | 1,898.4 | 42.5 |
| 1987 | 3,040.2 | - | 1,800.0 | - | 1,200.0 | 40.2 |
| 1988 | 3,043.4 | - | 1,800.0 | - | 1,200.0 | 43.4 |
| 1989 | 3,045.6 | - | 1,800.0 | - | 1,200.0 | 45.6 |
| 1990 | 3,034.9 | - | 1,792.3 | - | 1,194.8 | 47.8 |
| 1991 | 3,712.3 | - | 1,800.0 | - | 1,850.0 | 62.3 |
| 1992 | 3,100.0 | - | 1,800.0 | - | 1,200.0 | 100.0 |
| 1993 | 3,103.4 | - | 1,800.0 | - | 1,200.0 | 103.4 |

Reading notes. A "-" is the source's "None". Fiscal-year basis follows the source, which also prints a "TQ" transition-quarter row after FY1976 (the US fiscal year changed). The Public Law 99-88 allocation in the section above ($1,500,000,000 for Israel, 15 August 1985, S16), this table's FY1985 economic grant of 1,950.0 and the 15 May 1985 request of $1.5 billion for FY1985 (S9) are three different measures; this note does not reconcile them. These are fiscal-year amounts, not payment dates; payment dates remain **[GAP]**. Defense Department missile-defence funding is not in these columns. A direct comparison with the Greenbook is the open check for this row **[GAP]**.

[V] In the official **2023 codification**, 22 USC §2186(a)–(b) authorizes loan guarantees for immigrant absorption beginning **1 October 1992**, with up to **$10 billion** principal overall and up to **$2 billion** in FY1993, followed by annual authorizations for FY1994–1997. Subsections (c)–(f) contain geographical/use conditions, reductions, fees and suspension provisions (S11). The source note attributes the section to Public Law 102-391 §601, **6 October 1992**. These are statutory ceilings and conditions, not evidence that $10 billion was handed over in cash or all guarantees were issued. The later codification is identified explicitly; the original 1992 statutory pages were not obtained.

[V] GAO's NSIAD-92-119 page, report dated **12 February 1992**, describes its review of an earlier **$400 million housing loan-guaranty program**; the page's subsequently updated recommendation status discusses congressional authorization of $10 billion (S12). The later status text must not be presented as a decision already made on the report date. It is not a disbursement ledger.

**Guarantee issues and deductions: what was found (second pass, 2026-10-25).**

- [V, agent read 2026-10-25; a person's check pending] Federal Register, 27 January 1994, USAID notice "Loan Guarantees to Israel Notice of Investment Opportunity" (S21): the Government of Israel "wishes to select a financial advisor in connection with the issuance of U.S. Agency for International Development (“USAID”)-guaranteed loans" under Public Law 102-391, for "up to $10 billion principal amount of loans over the next five-year period, with a maximum of $2 billion in loans, offered in one or more tranches, to be guaranteed in each of the five fiscal years". It calls the offering "contemplated" and sets 4 February 1994 for proposals. It confirms the statutory ceilings and says nothing about amounts issued or deducted.
- [S, agent read 2026-10-25; a person's check pending] The CRS table notes (S20, printed p. 24) say "The $600 million in housing loan guarantees, $5.5 billion in military debt reduction loan guarantees, $9.2 billion in Soviet Jew resettlement loan guarantees, and $9 billion in economic recovery loan guarantees are not included in the tables because the United States government did not transfer funds to Israel." The report text gives the 1990s guarantee subsidy as 4.1%. The $9.2 billion is the only figure found for the resettlement program, against the $10 billion authority in S11; the report does not say in which years it was issued, or whether it is a total of issues or of authority net of deductions. Its $600 million housing figure differs from the **$400 million** housing program in S12 and S13, whose scope and dates were not compared. The same report's closing note (printed p. 28) mentions "the $7.9 billion in loan guarantees for housing or settling Soviet Jews" as also not included in the table, and gives the Arrow missile research figure as $180 million. The figures $400 million, $600 million, $7.9 billion and $9.2 billion are not reconciled and all stay **[C]**.
- The same CRS text says the United States reduced loan guarantees "by an amount equal to Israel's estimated spending on settlement construction" in "the mid-1990s and then again in 2003 and 2005", but the table it points to covers FY2003 onward only. No amount or year for the 1990s deductions was found in an opened source **[GAP]**.
- (25 Oct) Searched again and not found, for the issues and deductions of the 1992 program. Read: S21; S20; and three editions of CRS RL33222 as text, re-fetched and searched again on 25 October: the 2008 edition (https://www.everycrsreport.com/files/20080102_RL33222_ccba3778cd7b80bf36f0a6f852b90e110c8c11b6.html), the 2013 edition (https://www.everycrsreport.com/files/20130411_RL33222_3831ef6fa0005618eed693ec05f19c58f979c1fd.html) and the 2023 edition (https://www.everycrsreport.com/files/2023-03-01_RL33222_7ba4408ea8cf4a56fc01dbcb3d6b1154f8868dc3.html). Hits per term in the 2008, 2013 and 2023 editions (tags stripped, case-insensitive substring count, measured 25 October): "guarantee" 36, 44, 32; "10 billion" 1, 1, 1 (in 2023 the one hit is an unrelated arms-deal title); "deduct" 1, 2, 2; "9.2" 2, 3, 0, which includes unrelated table cells (a 1959 table cell in 2008 and 2013, and the digits of "3,029.22" in the 2013 table), so only the "$9.2 billion" phrase counts, once each in 2008 and 2013 and not in 2023. Findings differ by edition. The 2008 edition has the sentence "In the mid- 1990s and then again in 2003, the United States reduced loan guarantees to Israel" and the "Congress approved $10 billion in loan guarantees" sentence, but no 1990s issue or deduction amounts. The 2013 edition has the "Congress approved $10 billion in loan guarantees" sentence and the "$9.2 billion in Soviet Jew resettlement loan guarantees" table note, but no statement that guarantees were reduced in the mid-1990s; its "Deductions for" table (Table 4) covers FY2003 to FY2013 only. The 2023 edition had no "loan guarantee" passage within 400 characters of a 1990s year or of the $10 billion. Web search (the built-in WebSearch tool, no Tavily key on this machine), run 25 October 2026, query: GAO Israel loan guarantees immigrant absorption $10 billion issued 1993 1997 report. It returned 9 links (govinfo uscode and crec pages, Cornell and FindLaw copies of 22 USC §2186, two Jewish Virtual Library pages, a budgetcounsel.com copy of Public Law 102-391 section 601, the Federal Register notice above, and a palestine-studies.org page); none was a GAO page by title or address, and only the Federal Register notice was opened. The annual State Department reports under 22 USC §2186 were not located.

### Earlier housing guarantee: the report's dated milestones

[V] GAO's report of **12 February 1992**, Appendix I, printed pp. 12–14 (S13), attributes authorization of the earlier **$400 million housing-loan-guaranty program** to **25 May 1990**. It records authorization to release the guarantee on **20 February 1991**, AID's release in **March 1991**, completion of financing with underwriters on **28 March 1991**, and transfer of loan proceeds to Israel's Ministry of Finance on **9 April 1991**. This is the auditor's account, not an independently inspected bank-transfer record. The guarantee ceiling is neither a $400 million US cash grant nor proof of the exact net proceeds. These milestones do not establish an annual aid series or issues under the later 1992 program.

## 6. Defence consumption and GDP

[V, R] CBS Publication 1579, *Defence Expenditure in Israel 1950–2012* (October 2014), Table 4, printed p. 28 (PDF p. 30), reports the following percentages at current prices. Both columns are retained; neither is selected for gameplay. Footnote 3 assigns 1950–1994 to SNA 1968. This is a later official statistical publication, not a budget record (S14).

| Year | General government defence consumption / GDP | Domestic defence consumption / GDP |
|---|---:|---:|
| 1977 | 21.0% | 13.1% |
| 1978 | 22.7% | 12.5% |
| 1979 | 18.7% | 13.0% |
| 1980 | 21.6% | 13.3% |
| 1981 | 23.1% | 13.5% |
| 1982 | 19.7% | 13.9% |
| 1983 | 16.9% | 13.0% |
| 1984 | 19.4% | 13.0% |
| 1985 | 19.0% | 11.4% |
| 1986 | 14.2% | 10.2% |
| 1987 | 17.1% | 10.2% |
| 1988 | 14.5% | 9.9% |
| 1989 | 12.3% | 10.0% |
| 1990 | 12.6% | 9.7% |
| 1991 | 12.0% | 8.8% |
| 1992 | 10.7% | 8.5% |
| 1993 | 10.9% | 7.5% |

Domestic consumption excludes defence imports (p. 13); it is not the broader total defence-cost measure discussed separately (pp. 9, 15). Imports are recorded using equipment supply and certain payments, differing from budget payment-flow accounting (p. 17). Do not relabel either column as GNP, gross military budget or total economic cost; do not splice it to the earlier GNP table.

**Second pass (2026-10-25): the same years in a Bank of Israel vintage.** [V, agent read 2026-10-25; a person's check pending] S4's Tables A11b, A11c and A11d (BoI Annual Report 1995, printed pp. 244, 246 and 248), general-government expenditure as a percent of **GNP**, were read as rendered images. The rows "Defense, net" (under "Internal") and "Direct defense imports" (under "External") are tabulated next to S14's two GDP columns. The columns are different measures on different denominators and are not merged; the arithmetic columns are only subtraction and addition of printed values.

| Year | S4 A11 "Defense, net", internal (% of GNP) | S4 A11 "Direct defense imports" (% of GNP) | S14 domestic defence consumption (% of GDP) | A11 net minus S14 domestic (points, derived) | A11 net plus direct imports (% of GNP, derived) | S14 general-government defence consumption (% of GDP) |
|---|---:|---:|---:|---:|---:|---:|
| 1977 | 14.1 | 9.2 | 13.1 | 1.0 | 23.3 | 21.0 |
| 1978 | 13.4 | 13.0 | 12.5 | 0.9 | 26.4 | 22.7 |
| 1979 | 14.0 | 7.8 | 13.0 | 1.0 | 21.8 | 18.7 |
| 1980 | 14.1 (old SNA, A11b) / 13.9 (new SNA, A11c) | 9.9 (A11b) / 8.0 (A11c) | 13.3 | 0.8 (A11b) / 0.6 (A11c) | 24.0 (A11b) / 21.9 (A11c) | 21.6 |
| 1981 | 14.1 | 9.4 | 13.5 | 0.6 | 23.5 | 23.1 |
| 1982 | 15.0 | 5.6 | 13.9 | 1.1 | 20.6 | 19.7 |
| 1983 | 14.0 | 3.7 | 13.0 | 1.0 | 17.7 | 16.9 |
| 1984 | 14.1 | 6.0 | 13.0 | 1.1 | 20.1 | 19.4 |
| 1985 | 12.5 | 7.1 | 11.4 | 1.1 | 19.6 | 19.0 |
| 1986 | 11.2 | 3.8 | 10.2 | 1.0 | 15.0 | 14.2 |
| 1987 | 11.3 | 6.7 | 10.2 | 1.1 | 18.0 | 17.1 |
| 1988 | 10.8 | 4.4 | 9.9 | 0.9 | 15.2 | 14.5 |
| 1989 | 10.6 | 2.3 | 10.0 | 0.6 | 12.9 | 12.3 |
| 1990 | 10.4 | 2.7 | 9.7 | 0.7 | 13.1 | 12.6 |
| 1991 | 9.5 | 3.0 | 8.8 | 0.7 | 12.5 | 12.0 |
| 1992 | 9.1 | 2.1 | 8.5 | 0.6 | 11.2 | 10.7 |
| 1993 | 8.1 | 3.1 | 7.5 | 0.6 | 11.2 | 10.9 |

As arithmetic only: A11 "Defense, net" minus S14 domestic defence consumption is between 0.6 and 1.1 points in every year shown (for 1980, 0.8 from A11b or 0.6 from A11c). Both series fall from 1977 to 1993: A11 from 14.1 to 8.1 (6.0 points) and S14 domestic from 13.1 to 7.5 (5.6 points). A11 "Direct defense imports" has no counterpart in S14's domestic column, which excludes defence imports (S14 p. 13, section above). Footnotes on S4's tables: A11b is "According to old SNA", A11c and A11d are "According to new SNA", which is why 1980 has two printed values; S14's footnote 3 assigns 1950–1994 to SNA 1968. What "net" means in A11 was not established, and whether A11 and S14 draw on one national-accounts vintage was not established **[GAP]**. No series is selected for gameplay.

## 7. Outstanding evidence and coordination

- **Negative-search record (25 October 2026, by the agent).** (a) JTA archive site search, https://www.jta.org/?s=Beisky+commission, returned 9 archive links; the two read as original bulletin PDFs are in S18, the other seven were not opened. https://www.jta.org/?s=Tel+Aviv+Stock+Exchange+reopen returned 6 links; the two dated October 1983 are in S18, the others were not opened (no date range filter exists on that search). (b) WebSearch (built-in tool, no Tavily key), query "Beisky Commission 1986 report bank shares crisis 1983 Israel inquiry committee", returned 9 links; none was opened except through the items above. WebSearch, query in Hebrew for the commission's report as a PDF (the query text was not recorded in the session notes and is not reconstructed here), returned 9 links, of which 4 were Knesset State Control Committee protocols (the one for 21 April 1986 read in S19; the headers only of those of 23 June, 5 August and 15 October 1986), 2 were State Comptroller files (S22 read, the .docx copy not), and 3 were not opened; none of the opened files is the commission's report itself. Two Detroit Jewish News page-text addresses from a search result (the addresses were not recorded) returned a browser-challenge page (curl) and HTTP 403 (WebFetch) and were not read. (c) The 1983 closure notice and the Knesset debates of October 1983 were not searched.
- **Defence-series comparability [GAP].** S14 supplies GDP ratios with two defined consumption measures. S4 Table A11c remains a separate **GNP** table; the measures are not spliced. Section 6 now compares S4's A11 series; newer revision vintages, original budget ledgers and any eventual gameplay choice remain unexamined. No calibration is supplied.
- **Price-base bridge.** S15's published coefficients (section 2) link the successive CPI bases from September 1951 through 1985. The 1957–1977 era's base choice, and therefore the single ratio to use, stay with #24 and #116. Their research notes are on main; the 24 October follow-up passes landed in PR #1558 for #24 and PR #1574 for #116. The implementation choice remains paused. [PR #1387](https://github.com/eyalgolan/statecraft-israel/pull/1387) is coordination context for #116, not a primary source read or adopted by this note.
- **Coverage [GAP], after the 25 October pass.** Added: CPI for 1977–1978 and 1986–1993 (S17), an aid schedule by fiscal year (S20, secondary), the arrangement's outline (S22) and dated reporting on the closure and the report (S18, S19). Still open: aid payment dates and a Greenbook check, guarantee issues and deductions, the inquiry report and the arrangement's decision text, the exchange's own closure notice, the meaning of "net" in S4 A11, and gameplay selection. D11 is still an owner decision. Research is partial; no claim that #148 is complete or that this note has been reviewed by a human.

## 8. Sources opened and read

All checked **2026-10-23 by an AI agent only**. PDF page locators distinguish printed pages where available. Raw responses and selected rendered pages are retained privately for review; third-party PDFs are not added to the repository.

- **S1 — CBS data API**, [general CPI 120010, January 1979–December 1985](https://api.cbs.gov.il/index/data/price?id=120010&format=json&download=false&startPeriod=01-1979&endPeriod=12-1985&lang=en). Read all 84 records and paging. Field names and the anomalous levels above refer to this exact response.
- **S2 — CBS catalogue and API documentation**, [subject 37](https://api.cbs.gov.il/index/catalog/subject?id=37&format=json&download=false&lang=en), record 120010; [API Indices](https://www.cbs.gov.il/en/cbsNewBrand/Pages/Api-Indices.aspx), data retrieval parameters `id`, `startPeriod`, `endPeriod`, `coef`. A separate [December 1984–January 1985 retrieval with `coef=true`](https://api.cbs.gov.il/index/data/price?id=120010&format=json&download=false&startPeriod=12-1984&endPeriod=01-1985&coef=true&lang=en) was also read, including both monthly records, coefficient arrays and paging. The documentation and returned arrays do not explain the unchanged `Average 1980` label or supply the 1985 factor 100 shown explicitly in S15.
- **S3 — Bank of Israel, Annual Report 1985**, [Chapter III, Prices](https://www.boi.org.il/media/zi1injbo/pe_3.pdf), printed pp. 36–38, Table III-1 p. 37 (PDF pp. 1–3). Pages 36–37 also read as rendered images; p. 38 read as extracted text.
- **S4 — Bank of Israel, Annual Report 1995**, [Appendix tables](https://www.boi.org.il/media/bymmrj4d/nispe.pdf), Table A6 p. 236 and Table A7 p. 237 (PDF pp. 16–17), read as rendered images; Table A11c (PDF p. 26), heading and defence/import rows read in extracted text. A6 and A7 differ slightly for some other historical totals, and on 2026-10-25 the agent also read A6's total CPI column (p. 236) and A11b, A11c and A11d (printed pp. 244, 246, 248; PDF pp. 24, 26, 28) as rendered images, [V, agent read 2026-10-25; a person's check pending], so the comparison above uses A7 consistently rather than merging vintages/definitions.
- **S5 — Bank of Israel, Annual Report 1983**, [Chapter III, Prices](https://www.boi.org.il/media/5w2cx22q/pe_3.pdf), printed p. 38 (PDF p. 6), also read as a rendered image.
- **S6 — Asher A. Blass and Richard S. Grossman**, [A Harmful Guarantee? The 1983 Israel Bank Shares Crisis Revisited](https://boiwebrepec.azurefd.net/RePEc/boi/wpaper/WP_1996.03.pdf), Discussion Paper 96.03, May 1996, cover disclaimer and introduction p. 3 (PDF p. 3; rendered image read), and the reference list on printed p. 30 (PDF p. 30; rendered image read). Retrospective research, not the original arrangement.
- **S7 — Bank of Israel**, [Sheqel Series](https://www.boi.org.il/en/economic-roles/banknotes-and-coins/sheqel-series/), introductory history. Official retrospective currency account, read through the web reader. A separate direct GET returned HTTP 200 with a browser-verification challenge, not the historical page; that raw response does not support the dates.
- **S8 — Bank of Israel, Foreign Currency Department, Annual Report 2000**, [Part 2, Appendix 2.4](https://boi.org.il/boi_files/OldBankIsraelAnnualReport/Bank%20of%20Israel%20Annual%20Report%20-%202000/3/Part%202%20Developments%20in%20the%20Foreign%20Currency%20Market%20in%202000/ch-e2.pdf), printed p. 64 (PDF p. 16), also read as a rendered image.
- **S9 — Ronald Reagan Presidential Library / Public Papers**, [1985 Appendix A](https://www.reaganlibrary.gov/1985-public-papers-president-appendix), entry **15 May 1985**, appropriation request paragraph. Primary presidential record as reproduced by the official archive.
- **S10 — Ronald Reagan Presidential Library**, [Statement on Signing the Supplemental Appropriations Act, 1985](https://www.reaganlibrary.gov/archives/speech/statement-signing-supplemental-appropriations-act-1985), **16 August 1985**, opening two paragraphs and final editorial note. Statement date and law-approval date differ.
- **S11 — US Government Publishing Office**, [22 USC §2186, 2023 edition](https://www.govinfo.gov/content/pkg/USCODE-2023-title22/html/USCODE-2023-title22-chap32.htm), subsections (a)–(f) and source note after (m). Official later codification; original statutory text/vintage comparison remains open. Two attempted 1994 section URLs redirected to GovInfo's error page despite HTTP 200 and were not used as evidence.
- **S12 — US GAO**, [Israel: U.S. Loan Guaranties for Immigrant Absorption, NSIAD-92-119](https://www.gao.gov/products/nsiad-92-119), report metadata, highlights and recommendation-status table. Web-rendered text was opened and read; a separate direct GET returned 403. The full 44-page report was not read; S13 covers only the newly read Appendix I passages.
- **S13 — US GAO**, [NSIAD-92-119, official report PDF](https://www.gao.gov/assets/nsiad-92-119.pdf), Appendix I, printed pp. 12–14 (PDF pp. 14–16), authorization and financing passages. Extracted PDF text read from the archived web-reader response; no full local PDF or verified page image. Checked **2026-10-23 by an AI agent**. The original statutes cited by GAO were not read.
- **S14 — Central Bureau of Statistics**, [Publication 1579, Defence Expenditure in Israel 1950–2012](https://www.cbs.gov.il/he/publications/doclib/2014/1579_defence_2012/pdf/e_print.pdf), October 2014. Table 4, printed p. 28 (PDF p. 30), both GDP columns and footnote 3 read as a rendered image; definitions pp. 13–15 and methods p. 17 read as text, with pp. 15 and 17 also rendered. These are retrospective current-price national-account estimates; the source is not a ledger of budget payments. Checked **2026-10-23 by an AI agent only**.
- **S15 — Central Bureau of Statistics**, [Table 3.5, Consumer Price Index by different bases since September 1951](https://www.cbs.gov.il/he/publications/doclib/2008/price08a/a3_5_h.pdf), whose header says it was last produced for Price Statistics Monthly No. 8/2008. Page 1 header, published base/link bands, June 1977 cell, December 1984/January 1985 rows, scale bands and footnotes 1–3 read as a rendered image. Official retrospective table, not the original 1985 release. Checked **2026-10-23 by an AI agent only**.
- **S16 — US Government Publishing Office / Statutes at Large**, [Public Law 99-88, Supplemental Appropriations Act, 1985](https://www.govinfo.gov/content/pkg/STATUTE-99/pdf/STATUTE-99-Pg293.pdf), **15 August 1985**, H.R. 2577. Law header and enacting clause at 99 Stat. 293 (PDF p. 1); Economic Support Fund paragraph at 99 Stat. 323–324 (PDF pp. 31–32). These three pages were opened and read as rendered images. The remaining 83 pages and cross-referenced laws were not reviewed. Checked **2026-10-23 by an AI agent only**. An OCR search snippet omitted three zeroes in the Israel amount; the scanned paragraph, not that snippet, supports the amount recorded here.

Second pass, all opened **2026-10-25 by an AI agent only**:

- **S17: CBS data API, second retrieval**, [general CPI 120010, January 1977–December 1978](https://api.cbs.gov.il/index/data/price?id=120010&format=json&download=false&startPeriod=01-1977&endPeriod=12-1978&lang=en) (24 records) and [January 1986–December 1993](https://api.cbs.gov.il/index/data/price?id=120010&format=json&download=false&startPeriod=01-1986&endPeriod=12-1993&lang=en) (96 records). Paging metadata read for both.
- **S18: Jewish Telegraphic Agency Daily News Bulletin**, original PDFs at pdfs.jta.org, items read as rendered images with their datelines: [17 October 1983, No. 195](http://pdfs.jta.org/1983/1983-10-17_195.pdf) (item "1 Million Israeli Workers Stage 2-Hour Strike", Jerusalem Oct. 16); [24 October 1983, No. 200](http://pdfs.jta.org/1983/1983-10-24_200.pdf) (Tel Aviv, Oct. 23), page 3; [25 October 1983, No. 201](http://pdfs.jta.org/1983/1983-10-25_201.pdf) (Jerusalem, Oct. 24), page 2; [22 April 1986, No. 77](http://pdfs.jta.org/1986/1986-04-22_077.pdf) (Jerusalem, April 21), page 1; [23 April 1986, No. 78](http://pdfs.jta.org/1986/1986-04-23_078.pdf) (Jerusalem, April 22), page 1. Archive pages: [17 Oct](https://www.jta.org/archive/1-million-israeli-workers-stage-2-hour-strike-to-protest-against-the-governments-economic-policies), [24 Oct](https://www.jta.org/archive/stock-exchange-prepared-to-reopen-monday-after-a-closure-of-two-weeks), [25 Oct](https://www.jta.org/archive/tel-aviv-stock-exchange-reopens), [22 Apr 1986](https://www.jta.org/archive/israels-financial-institutions-reeling-following-scathing-criticism), [23 Apr 1986](https://www.jta.org/archive/bank-of-israel-governor-resigns-in-wake-of-indictment-of-banking-system). The 17 October item was first found and posted by Ella Sharakanski on #148 on 24 October 2026; the agent re-read it in the original bulletin.
- **S19: Knesset State Control Committee, 11th Knesset, protocol no. 138**, Monday 21 April 1986, "uncorrected text" (the file's own heading), [Word file on oknesset.org](https://production.oknesset.org/pipelines/data/committees/download_document_committee_session/files/23/4/5/454141.DOC). Text read from the document body (extracted); a third-party mirror of the Knesset record, not the Knesset's own site. Hebrew passages quoted verbatim; English renderings are the agent's.
- **S20: Congressional Research Service**, RL33222, Jeremy M. Sharp, [U.S. Foreign Aid to Israel, 16 September 2010](https://www.everycrsreport.com/files/20100916_RL33222_c1f917458b1b58dd0fa3ea177e655817c8cad13a.pdf), Table A-1 notes (printed p. 24) and Table A-2 (printed pp. 25–27) read as rendered images; text on loan guarantees read as extracted text. Secondary compilation [S]; Greenbook not opened.
- **S21: Federal Register**, [Federal Register, 59 FR No. 18, 27 January 1994, FR Doc. 94-1918](https://www.govinfo.gov/content/pkg/FR-1994-01-27/html/94-1918.htm), USAID notice, whole notice read.
- **S22: State Comptroller of Israel**, [Preparation for the sale of the bank shares under the arrangement, April 1993](https://library.mevaker.gov.il/sites/DigitalLibrary/Documents/special/special_2004-1990/1993-Menayot/1993-Menayot.pdf), PDF pp. 8–11 read as extracted text (102 pages in total; the rest not read).
