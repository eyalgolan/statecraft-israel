# Economy research, 1977–1993: prices, stabilization and financing

Researched 2026-10-23. **Research only; agent-checked draft, not human fact-checked.** Refs #148. This note does not choose D11, change content, implement era rules, calibrate effects or approve a price display. The [owner's research ruling](https://github.com/eyalgolan/statecraft-israel/issues/148#issuecomment-6340404323) keeps those implementation tasks paused.

**Reading key.** [V] marks a claim checked on its cited page, opened and read by an AI agent; it is not human sign-off. [S] identifies single-source material or a summary not directly opened. [C] preserves conflicting accounts with their sources. [R] identifies retrospective evidence. [GAP] marks incomplete evidence. Sources were checked on 2026-10-23 by an AI agent only. Quoted fragments reproduce the cited sources.

## 1. Scope and risks

The evidence must distinguish monthly changes, twelve-month changes and changes in annual averages; an index's statistical base from currency units; a currency's issue date from the date prices were quoted in it; and an aid request, appropriation, payment and loan guarantee. GDP and GNP are different denominators. None of those distinctions supplies a simulation rule.

| Owner's anchor | Coverage here | Remaining work |
|---|---|---|
| Monthly CPI, 1979–1985 | [V] All 84 months, source S1; catalogue S2 | Published 1985 scale link now documented in S15; resolve API metadata and verify any later cross-base use |
| Inflation series / D11 | [V] CBS monthly data; BoI annual and annual-average comparisons, S1–S4 | Owner chooses the measure and its eventual use |
| Bank crisis, 1983 | [V] Contemporary BoI account, S5; explicitly retrospective study, S6 | Original bank-share arrangement and inquiry records not read |
| Stabilization, 1985 | [V] BoI's contemporary prices chapter, S3 | Cabinet decision and full fiscal implementation records not read |
| US aid | [V] Dated request and signing statement, S9–S10 | Enacted 1985 ESF amount documented in S16; annual 1977–1993 commitments/payments remain incomplete |
| Loan guarantees | [V] Official codified authority and GAO summary, S11–S12 | Actual issues and deductions under the 1992 program not established |
| Defence share of GDP | [V, R] CBS 1579, Table 4: both GDP columns for 1977–1993, S14 | Compare revision vintages and choose any later gameplay measure separately |
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

[V] In the official **2023 codification**, 22 USC §2186(a)–(b) authorizes loan guarantees for immigrant absorption beginning **1 October 1992**, with up to **$10 billion** principal overall and up to **$2 billion** in FY1993, followed by annual authorizations for FY1994–1997. Subsections (c)–(f) contain geographical/use conditions, reductions, fees and suspension provisions (S11). The source note attributes the section to Public Law 102-391 §601, **6 October 1992**. These are statutory ceilings and conditions, not evidence that $10 billion was handed over in cash or all guarantees were issued. The later codification is identified explicitly; the original 1992 statutory pages were not obtained.

[V] GAO's NSIAD-92-119 page, report dated **12 February 1992**, describes its review of an earlier **$400 million housing loan-guaranty program**; the page's subsequently updated recommendation status discusses congressional authorization of $10 billion (S12). The later status text must not be presented as a decision already made on the report date. It is not a disbursement ledger.

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

## 7. Outstanding evidence and coordination

- **Defence-series comparability [GAP].** S14 supplies GDP ratios with two defined consumption measures. S4 Table A11c remains a separate **GNP** table; the measures are not spliced. Newer revision vintages, original budget ledgers and any eventual gameplay choice remain unexamined. No calibration is supplied.
- **Price-base bridge.** S15's published coefficients (section 2) link the successive CPI bases from September 1951 through 1985. The 1957–1977 era's base choice, and therefore the single ratio to use, stay with #24 and #116. #24 and #116 have separate contributor claims. [PR #1387](https://github.com/eyalgolan/statecraft-israel/pull/1387) is coordination context for #116, not a primary source read or adopted by this note.
- **Coverage [GAP].** CPI outside 1979–1985, a full US aid schedule for 1977–1993, actual guarantee issues/deductions, defence-series comparability and gameplay selection, and original crisis records remain open. D11 is still an owner decision. Research is partial; no claim that #148 is complete or that this note has been reviewed by a human.

## 8. Sources opened and read

All checked **2026-10-23 by an AI agent only**. PDF page locators distinguish printed pages where available. Raw responses and selected rendered pages are retained privately for review; third-party PDFs are not added to the repository.

- **S1 — CBS data API**, [general CPI 120010, January 1979–December 1985](https://api.cbs.gov.il/index/data/price?id=120010&format=json&download=false&startPeriod=01-1979&endPeriod=12-1985&lang=en). Read all 84 records and paging. Field names and the anomalous levels above refer to this exact response.
- **S2 — CBS catalogue and API documentation**, [subject 37](https://api.cbs.gov.il/index/catalog/subject?id=37&format=json&download=false&lang=en), record 120010; [API Indices](https://www.cbs.gov.il/en/cbsNewBrand/Pages/Api-Indices.aspx), data retrieval parameters `id`, `startPeriod`, `endPeriod`, `coef`. A separate [December 1984–January 1985 retrieval with `coef=true`](https://api.cbs.gov.il/index/data/price?id=120010&format=json&download=false&startPeriod=12-1984&endPeriod=01-1985&coef=true&lang=en) was also read, including both monthly records, coefficient arrays and paging. The documentation and returned arrays do not explain the unchanged `Average 1980` label or supply the 1985 factor 100 shown explicitly in S15.
- **S3 — Bank of Israel, Annual Report 1985**, [Chapter III, Prices](https://www.boi.org.il/media/zi1injbo/pe_3.pdf), printed pp. 36–38, Table III-1 p. 37 (PDF pp. 1–3). Pages 36–37 also read as rendered images; p. 38 read as extracted text.
- **S4 — Bank of Israel, Annual Report 1995**, [Appendix tables](https://www.boi.org.il/media/bymmrj4d/nispe.pdf), Table A6 p. 236 and Table A7 p. 237 (PDF pp. 16–17), read as rendered images; Table A11c (PDF p. 26), heading and defence/import rows read in extracted text. A6 and A7 differ slightly for some other historical totals, so the comparison above uses A7 consistently rather than merging vintages/definitions.
- **S5 — Bank of Israel, Annual Report 1983**, [Chapter III, Prices](https://www.boi.org.il/media/5w2cx22q/pe_3.pdf), printed p. 38 (PDF p. 6), also read as a rendered image.
- **S6 — Asher A. Blass and Richard S. Grossman**, [A Harmful Guarantee? The 1983 Israel Bank Shares Crisis Revisited](https://boiwebrepec.azurefd.net/RePEc/boi/wpaper/WP_1996.03.pdf), Discussion Paper 96.03, May 1996, cover disclaimer and introduction p. 3 (PDF p. 3; rendered image read). Retrospective research, not the original arrangement.
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
