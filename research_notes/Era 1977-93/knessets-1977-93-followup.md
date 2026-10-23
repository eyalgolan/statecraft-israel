# Knessets, elections and parties, 1977–1993 — follow-up

Status: research only, 23 October 2026; AI agent checks, not human fact-checking or approval.
Follow-up to [the original research note](knessets-1977-93.md), for
[issue #146](https://github.com/eyalgolan/statecraft-israel/issues/146).
The [owner's research-only ruling](https://github.com/eyalgolan/statecraft-israel/issues/146#issuecomment-6340400128)
allows this note; it does not reopen implementation. No `content/`, simulation, tests or golden values are changed.

## Reading this note

- **[A]** an AI agent opened and read the named source on `checked: 2026-10-23`. This is not a person's sign-off.
- **[C]** conflicting values remain, each attributed to its source.
- **[GAP]** the required source or distinction has not been established. A search result, inaccessible page or
  unexamined citation does not close a gap.
- The original note's **[V]** and **[S]** labels describe its checks of 13 October 2026. They are not renewed here
  unless a row explicitly records a new reading. The original note is unchanged.
- Election lists, parliamentary factions, party founding, ministerial terms and coalition membership are different
  records. A date in one is not automatically the date of another.

This follow-up adds the readings below and retains the unresolved items in the coverage table. It is not a
completed implementation of #146 or permission to turn an unresolved value into game content.

## Election totals: an additional official source for C1

**[A, C]** The Central Bureau of Statistics' table 27.2, published **29 August 2023**, gives **2,236,293**
eligible voters for the **17 May 1977** election. Its source line attributes the data to the Central Elections
Committee [E1]. This is an official retrospective statistical publication, not the original results notice.

The original note's CEC Basic Facts value, **2,236,478**, remains recorded as a conflicting value. Its archived
source has not yet been rechecked here, and the CBS table does not explain that discrepancy. The new reading
supports 2,236,293 without establishing how the conflicting figure arose.

The same CBS page prints **79.8%** turnout for **23 July 1984**, whereas the original note records **78.8%**.
This additional discrepancy remains **[C]**; neither value is silently corrected or substituted. E1 does not
provide per-list results or votes for lists that won no seats.

### C2 and C5 — two archived official results tables

**[A]** The archived Knesset table for the **1 November 1988** election gives the Progressive List for Peace
**33,695 votes, 1.5% and one seat** [E2]. This rechecks the official one-seat figure. The original note's
attribution of two seats to a Wikipedia passage is retained as an older reported conflict; that passage was
not newly read here.

**[A, C]** The archived table for the **23 July 1984** election uses the list labels
**Morasha-Po'aeli Agudat Yisrael** and **Tehiya-Tsomet**, with **33,287 votes/two seats** and
**83,037 votes/five seats**, respectively [E3]. These are the official retrospective table's English labels.
They do not establish the legal ballot names in the original election notice or when a faction adopted a longer
name. The shorter Morasha and Tehiya labels recorded in the original note therefore remain attributed variants.
The Flatto-Sharon naming variants were not newly verified: the archived 1977 results page could not be reached
in two bounded attempts.

## Faction names, membership changes and dates

### C5 — Telem's split and the later Ometz name

**[A]** The current Knesset faction records distinguish the following [K1]:

| Record | Name as returned by the Knesset | Recorded dates |
|---|---|---|
| FactionID 469, Knesset 10 | תנועה להתחדשות ממלכתית — Telem | 20 July 1981 to 6 June 1983 |
| FactionID 1022, Knesset 10 | רפ"י - הרשימה הממלכתית — Rafi–National List | 6 June 1983 to 13 August 1984 |
| FactionID 1023, Knesset 10 | התנועה להתחדשות ציונית חברתית — Movement for Social Zionist Renewal | 6 June 1983 to 13 August 1984 |
| FactionID 489, Knesset 11 | אומץ — Ometz | 13 August 1984 to 27 September 1988 |

These are parliamentary record names and dates. They do not justify naming the 1983 faction Ometz or assigning
a party-founding date from a faction's first day. The original note's secondary description using Ometz for the
1983 split remains a differently named account, not evidence that the database used that name then.

### A checked example of member records versus faction records

**[A]** The member-term records show Moshe Dayan's Telem term ending and Yigal Hurvitz's Telem term beginning
on **16 October 1981**, both under FactionID 469. On **6 June 1983**, Hurvitz's record changes from 469 to 1022,
while Mordechai Ben-Porat's changes from 469 to 1023 [K2, K3]. The October records share the same faction ID;
the June records change faction IDs. The five records alone do not document the formal replacement procedure
or its cause. This is a checked example, **not** a complete audit
of replacements or faction moves throughout the five Knessets. The dates are the database's term fields;
no independent inference about announcements or the precise time of legal effect is made.

### The original secondary-only 13 May 1980 move

**[A]** A targeted identity query identifies David Golomb as PersonID 2244 and Meir Amit as 2527 [K9].
For both, the Knesset member-term records end faction 413, the Movement for Change and Initiative, on
**13 May 1980**, and begin faction 431, the Alignment, on the same day [K10]. Golomb's relevant rows are
27136–27137; Amit's are 27232–27233. This provides a primary register check for the date previously marked
secondary-only in the original note. It establishes the recorded faction-term boundary, without separately
dating an announcement, parliamentary vote or legal decision.

### The five party-founding gaps

These contemporary reports supply milestones outside the parliamentary register, with the limits stated below.
They are primary evidence of what was reported at the time, not party registration certificates.
All rows are **[A]**; an exact founding day remains **[GAP]** where the source does not give one.

| Party | Additional evidence read | What remains unproved |
|---|---|---|
| Shas | JTA's **30 July 1984** report describes Shas as a new party and reports four elected members [P1]. Separately, faction 503 starts **13 August 1984** [K1]. | Neither gives the date of the founding decision or registration. The report's publication and the faction's start are not substitutes for it. |
| Meretz | Faction 561 starts **9 March 1992**, when the Mapam 525, Ratz 533 and Shinui 535 faction records end [K1]. JTA's **16 June 1992** election guide describes their joint list [P2]. | The first alliance agreement's day and any later legal merger are not established. Ending faction records does not prove that the parties themselves ceased to exist. |
| Moledet | JTA's **25 August 1988** report describes a party launch at an inaugural meeting on Tuesday evening [P3]. Faction 549 starts **21 November 1988** [K1]. | The report does not print a civil date for that Tuesday or a registration date. No calculated day is promoted to an explicit founding date. |
| Degel Hatorah | Davar reports dated **25, 27 and 30 September 1988**, read as reproductions in the reporter's archive, describe an instruction to establish a party, a signature drive using its name, and campaign preparations [P4a–c]. Faction 547 starts **21 November 1988** [K1]. | The first report dates the instruction to Friday, not to its publication day. Original newspaper scans and a founding or registration instrument were not read. The archive's editorial annotation is separate from the report. |
| Tzomet | The Christian Science Monitor's **29 December 1983** report says Eitan had announced in October that he would head a new movement named Tzomet [P5]. The parliamentary record separately has an Eitan faction record, 1024, from **16 to 30 November 1987**, followed by Tzomet 507 on **30 November 1987** [K1]. | An exact founding day remains unverified. The 1987 faction milestone must not be described as the movement's first founding merely because it is the register's start. |

## API access and session coverage

**[A]** A direct request to the service root lists `KNS_KnessetDates` but no `KNS_Knesset`; a direct request to
`KNS_Knesset?$top=1` returned HTTP 404 with an unknown-segment message [K4]. This confirms the missing entity
on the route checked, rather than treating it as a usable source.

**[A]** A fully returned query for `KnessetNum ge 9 and KnessetNum le 10` produced **16 session-period rows**
and no next-page link [K5]. For Knesset 9 it includes both ID 51, a one-day row on **13 June 1977**, and ID 136,
from **13 June to 3 August 1977**, as well as later periods. ID 31 for Knesset 10 starts **20 July 1981**.
The original note's description of only a single one-day row for Knesset 9 is therefore **not the result of
the query read now**. No cause for the difference is established. Exhausting this query is not proof that every
historical session period is present: that still requires comparison with an independent complete calendar.

Direct OData requests succeeded, while one web-reader request to the service root was redirected to a location
restriction page. These are route-specific observations, not evidence that all Knesset sources are inaccessible.

## Legal dates: what the documents establish

### C3 — resignation notice, term fields and the older withdrawal date

**[A, C]** Section ב of sitting 374, indexed by the Knesset as **21 January 1992**, records Roni Milo's
government notice that Rehavam Ze'evi and Yuval Ne'eman resigned on **19 January 1992** [J4, K8].
Separately, the administrative ministerial records have `FinishDate` **21 January 1992** for Ze'evi's
minister-without-portfolio row and Ne'eman's two ministerial rows in government 24 [K6, K7].

The original note's **16 January 1992** withdrawal date remains a secondary claim not newly established here.
The new sources date a reported resignation and recorded ministerial terms. They do not establish when the
parties announced their withdrawal, or explain the legal mechanism connecting these dates. That distinction
remains **[GAP]**; the original suggestion about announcement versus effective date remains unconfirmed.
J4 was read as the official DOC transcription; its scanned counterpart was not opened.

### C4 — the committee decision concerning Kach

**[A, C]** The English translation of Election Appeal 1/88 on Cardozo's Versa site has a readable PDF.
Its paragraph 1, printed page 2, dates the Central Elections Committee's determination and the chairman's notice
to **5 October 1988** [J1]. The corresponding HTML passage was garbled in the original note; the PDF removes
that reading obstacle.

The original note records **4 October 1988** from the English Wikipedia election article. That secondary passage
and its underlying source have not been rechecked here. The two dates therefore remain attributed alternatives;
this note does not establish why they differ. J1 is an English translation of a judgment, not the original Hebrew
judgment or the committee's own decision. Those originals remain **[GAP]**. This finding supplies a readable
judicial account; it does not constitute a sensitivity review or authorize a game event.

### The 1984 dissolution law and the 22 March photograph

**[A]** The Knesset's copy of the Dissolution of the Tenth Knesset Law gives **23 July 1984** as the election day
in section 2. Section 3 makes the law effective on adoption. Its adoption footnote says **4 April 1984**;
the publication footer is *Sefer HaHukim* 1116, **12 April 1984**, printed page 132 [J2].

**[A]** The official transcript linked to sitting 301, indexed as **22 March 1984**, records a **61–58** vote
to send dissolution proposal שפ״ד for preliminary committee consideration and preparation for its first
reading [J5, K8]. That is the stage recorded for this proposal, separate from final adoption on 4 April.
The DOC's date header has a transcription error; the day-to-document link comes from the official session
and document metadata. The facsimile was not opened, so a scan check remains **[GAP]**. Adoption, publication
and the photograph's date must not be substituted for one another.

### The 1988 election day and the regular-term question

**[A]** Section 1(1) of the Twelfth Knesset Elections (Temporary Provisions) Law identifies
**1 November 1988** as election day while amending the electoral-register provisions for that election.
Its adoption footnote and publication footer both give **27 July 1988**, *Sefer HaHukim* 1260,
printed page 181 [J3].

This confirms the date named in that law. It does **not**, by itself, establish whether that date followed from
the then-applicable Basic Law's regular-term calculation. Nor does absence of a dissolution law in a database
query prove that calculation. That narrower question remains **[GAP]**.

## Coverage and remaining work

The C labels refer to section 6 of the original note. G labels below track its section 7 bullets;
they are research locators, not game-rule IDs or completion approvals.

| Original item | Result of this follow-up | Remaining work |
|---|---|---|
| C1: 1977 eligible voters | Additional official support for 2,236,293 [E1] | Origin of the older 2,236,478 value; original results notice. |
| C2: PLP seats in 1988 | Official retrospective table re-read: one seat [E2] | Original notice and provenance of the older secondary two-seat statement. |
| C3: January 1992 withdrawal | Government notice says 19 January; term fields end 21 January [J4, K6–K8] | Primary basis for 16 January, party announcement dates and any legal-effect explanation. |
| C4: Kach committee decision | Readable translated judgment states 5 October 1988 [J1] | Original Hebrew/committee record and the source of 4 October. |
| C5: Morasha and Tehiya names | Archived table labels re-read [E3] | Contemporaneous legal list names and timing of any rename. |
| C5: Flatto-Sharon name | No new successful reading | Original note's variants retain their old attribution; archive access failed. |
| C5: Telem/Rafi/Ometz | Dated parliamentary names distinguished [K1–K3] | Extra-parliamentary renaming and electoral-registration dates. |
| G1: original per-list election notices | None read for 1977, 1981, 1984, 1988 or 1992 | Obtain and read each notice. A discovered 1988 PDF link returned 404 live and timed out twice in the archive. The two retrospective tables are different sources. |
| G2: votes for zero-seat lists | No new verified figures | Original complete results for all five elections; the tables read here list winning lists only. |
| G3: live versus archived access | Live CBS and direct OData read; two archived Knesset tables read | Access failures are source-specific. Other old pages have no renewed check date. |
| G4: missing API entity | Service list and error response rechecked [K4] | Use an existing documented collection; do not invent a KNS_Knesset response. |
| G4: session coverage | More Knesset 9 periods than the original note describes are now returned [K5] | Compare with an independent calendar; neither full session completeness nor constitutional term ends are proved. |
| G5: member terms versus faction moves | Five Telem member records compared with faction records [K1–K3] | Full five-Knesset replacement/move audit and causes of membership changes. |
| Original §3 [S]: Golomb/Amit, 13 May 1980 | Primary identity and faction-term records now read [K9–K10] | Announcement or decision dates, if needed, require their own sources. |
| G6: founding of Shas, Meretz, Moledet, Degel Hatorah, Tzomet | Additional contemporary milestones and parliamentary dates [P1–P5, K1] | The exact founding/registration instruments; see the five-party table for each limit. |
| G7: 22 March 1984 reading | Official transcript records preliminary transfer/preparation for first reading [J5, K8] | Inspect the facsimile and resolve the DOC date-header error. |
| G7: Kach date | See C4 | Original-language cross-check remains open. |
| G8: 1 November 1988 fixed by law | Explicit election-day reference read [J3] | Then-applicable Basic Law and the regular-term calculation. |

Other secondary-only statements in the original note are not automatically upgraded by these targeted reads.
No missing figure is filled from a search snippet, an inaccessible source or a calendar inference.

An unreached lead for G1 is the [CEC-hosted yalkut_3591.pdf](https://bechirot.gov.il/election/about/Documents/yalkut_3591.pdf)
and its [archived route](https://web.archive.org/web/20180612141031/http://bechirot.gov.il/election/about/Documents/yalkut_3591.pdf).
Neither PDF was read in these attempts. Their contents, page references and publication date are not asserted here.

## Sources read for the rows above

All entries below: `checked: 2026-10-23`, opened and read by AI agents; human verification pending.
Only the passages named were used, not a claim to have reviewed each entire source.

| Key | Source and exact locator | What was read and its limit |
|---|---|---|
| E1 | CBS, [table 27.2: Elections to the Knesset, by eligible voters and voters](https://www.cbs.gov.il/he/publications/doclib/2023/shnatonelectionstheknessetandgovernment.27/st27_02.pdf), single printed p. 1, rows for Knessets 9 and 11; source line and publication footer | PDF rendered and read. Official retrospective totals attributed to the CEC, not original election notices or per-list results. |
| E2 | Knesset, [Twelfth Knesset results, archived 17 August 2016](https://web.archive.org/web/20160817093719/http://knesset.gov.il/description/eng/eng_mimshal_res12.htm), Progressive List for Peace row and column headings | Retrospective official table read from its archived HTML. Winning lists only. |
| E3 | Knesset, [Eleventh Knesset results, archived 17 August 2016](https://web.archive.org/web/20160817160844/http://knesset.gov.il/description/eng/eng_mimshal_res11.htm), Morasha and Tehiya rows and column headings | Retrospective official table read from its archived HTML. List labels do not establish naming chronology. |
| K1 | Knesset OData, [KNS_Faction, Knessets 10–12](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_Faction?%24filter=KnessetNum+ge+10+and+KnessetNum+le+12), IDs 469, 489, 1022, 1023; 503, 507, 525, 533, 535, 547, 549, 561, 1024 | 60 rows returned, no next-page link. Hebrew database names copied; English glosses are agent-checked, not native-speaker approval. |
| K2 | Knesset OData, [KNS_PersonToPosition, Knesset 10, factions 469/1022/1023](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition?%24filter=KnessetNum+eq+10+and+%28FactionID+eq+469+or+FactionID+eq+1022+or+FactionID+eq+1023%29), record IDs 27313, 27314, 27326, 27330, 27331 | All five returned member terms read; no next-page link. This is a local example, not all membership changes. |
| K3 | Knesset OData, [KNS_Person, IDs 2206/2277/2294](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_Person?%24filter=PersonID+eq+2206+or+PersonID+eq+2277+or+PersonID+eq+2294&%24select=PersonID%2CFirstName%2CLastName) | Only the three returned IDs and names were requested and read to identify the K2 records. |
| K4 | Knesset OData [service root](https://knesset.gov.il/Odata/ParliamentInfo.svc/) and [KNS_Knesset probe](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_Knesset?%24top=1) | Collection list read and 404 error body read; not an inference from a search snippet. |
| K5 | Knesset OData, [KNS_KnessetDates, Knessets 9–10](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_KnessetDates?%24filter=KnessetNum+ge+9+and+KnessetNum+le+10), IDs 31, 51 and 136 and returned periods | 16 rows returned, no next-page link. Not compared with an independent complete calendar. |
| K6 | Knesset OData, [government 24 ministerial records ending in January 1992](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition?%24filter=GovernmentNum%20eq%2024%20and%20%28FinishDate%20ge%20datetime%271992-01-01T00:00:00%27%20and%20FinishDate%20lt%20datetime%271992-02-01T00:00:00%27%29&%24format=json), PersonToPositionID 4102, 4312, 4313 | Three administrative FinishDate fields read; these are not a party-withdrawal notice or a legal explanation. |
| K7 | Knesset OData, [person identities 417 and 2475](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_Person?%24filter=PersonID+eq+417+or+PersonID+eq+2475&%24format=json) | Public names in the returned identity records used to identify the K6 terms. |
| K8 | Knesset OData: [22 March 1984 session](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PlenumSession?%24filter=KnessetNum+eq+10+and+StartDate+ge+datetime%271984-03-22T00%3A00%3A00%27+and+StartDate+lt+datetime%271984-03-23T00%3A00%3A00%27&%24format=json), [20–22 January 1992 sessions](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PlenumSession?%24filter=KnessetNum+eq+12+and+StartDate+ge+datetime%271992-01-20T00%3A00%3A00%27+and+StartDate+lt+datetime%271992-01-23T00%3A00%3A00%27&%24format=json), [document links](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_DocumentPlenumSession?%24filter=PlenumSessionID+eq+436732+or+PlenumSessionID+eq+159771+or+PlenumSessionID+eq+160495+or+PlenumSessionID+eq+437218+or+PlenumSessionID+eq+437219&%24format=json) | Session 436732/number 301 maps to DOC 236438; session 437218/number 374 maps to DOC 236738. This is the date-to-document attribution, not inspection of the linked scans. |
| K9 | Knesset OData, [Golomb and Amit name query](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_Person?%24filter=%28FirstName+eq+%27%D7%93%D7%95%D7%93%27+and+LastName+eq+%27%D7%92%D7%95%D7%9C%D7%95%D7%9E%D7%91%27%29+or+%28FirstName+eq+%27%D7%9E%D7%90%D7%99%D7%A8%27+and+LastName+eq+%27%D7%A2%D7%9E%D7%99%D7%AA%27%29&%24select=PersonID%2CFirstName%2CLastName&%24top=10) | Two returned identities read, no next-page link; IDs were looked up before querying terms. |
| K10 | Knesset OData, [Knesset 9 terms for persons 2244 and 2527](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition?%24filter=KnessetNum+eq+9+and+%28PersonID+eq+2244+or+PersonID+eq+2527%29&%24select=PersonToPositionID%2CPersonID%2CPositionID%2CKnessetNum%2CStartDate%2CFinishDate%2CFactionID%2CFactionName&%24top=100) | Ten returned records read, no next-page link; the four relevant faction-term rows have PositionID 54. Other kinds of term are not used as faction changes. |
| J1 | Cardozo Versa, English translation of EA 1/88, *Neiman v. Chairman of the Central Election Committee for the 12th Knesset*, [PDF](https://versa.cardozo.yu.edu/sites/default/files/upload/opinions/Neiman%20v.%20Chairman%20of%20the%20Central%20Election%20Committee%20for%20the%2012th%20Knesset.pdf), printed p. 2, paragraph 1 | Rendered page read. The translation dates the committee determination and notice to 5 October 1988. Original Hebrew decision not read. |
| J2 | Knesset file server, [Dissolution of the Tenth Knesset Law](https://fs.knesset.gov.il/10/law/10_lsr_215629.DOC), sections 2–3, adoption footnote and footer, printed p. 132 | Official DOC opened; text read using textutil. Adoption and publication are separate dates. No proof of the 22 March reading. |
| J3 | Knesset file server, [Twelfth Knesset Elections (Temporary Provisions) Law](https://fs.knesset.gov.il/11/law/11_lsr_214604.DOC), section 1(1), adoption footnote and footer, printed p. 181 | Official DOC opened; text read using textutil. It names election day for the temporary provisions; the regular-term formula remains unverified. |
| J4 | Knesset, [sitting 374 transcript](https://fs.knesset.gov.il/12/Plenum/12_ptm_236738.DOC), section ב, government notice by Roni Milo | Official DOC text read; notice gives 19 January for the two resignations. Original scan not inspected. |
| J5 | Knesset, [sitting 301 transcript](https://fs.knesset.gov.il/10/Plenum/10_ptm_236438.DOC), vote on dissolution proposal שפ״ד and the chair's following announcement | Official DOC text read; 61–58 and preparation for first reading. Date header has a transcription error; K8 supplies the session date. Scan check remains open. |
| P1 | JTA, [Shas coalition report, 30 July 1984](https://www.jta.org/archive/trying-to-form-a-government-shas-says-it-is-strongly-inclined-to-join-with-likud-in-a-coalition), publication line and opening two paragraphs | Contemporary report read; not a founding or registration document. |
| P2 | JTA, [election-list guide, 16 June 1992](https://www.jta.org/archive/the-israeli-elections-from-communists-to-cab-drivers-array-of-choices-awaits-voters), publication line and MERETZ, DEMOCRATIC ISRAEL entry | Joint list described; no founding-agreement date. |
| P3 | JTA, [Moledet launch report, 25 August 1988](https://www.jta.org/archive/new-political-party-formed-stresses-transfer-of-arabs), publication line and opening paragraph | Tuesday-evening inaugural meeting described; no founding instrument read. |
| P4a | Noach Zevuluni's reporter archive, [Davar, 25 September 1988, p. 7](https://ranaz.co.il/articles/article2963_19880925.asp), opening paragraphs | Reproduced report, not the original newspaper scan; Friday decision distinguished from Sunday publication. |
| P4b | Same reporter archive, [Davar, 27 September 1988, p. 3](https://ranaz.co.il/articles/article2965_19880927.asp), opening paragraphs | Reproduced report of the named list and signature drive, not proof of a registration date. |
| P4c | Same reporter archive, [Davar, 30 September 1988, p. 3](https://ranaz.co.il/articles/article2970_19880930.asp), opening paragraph | Reproduced campaign-preparation report; original scan not read. |
| P5 | The Christian Science Monitor, [report of 29 December 1983](https://www.csmonitor.com/1983/1229/122930.html), publication line and opening paragraph | Contemporary report of an October announcement concerning Tzomet; no exact founding day. |

## AI assistance and review status

AI agents researched and drafted this follow-up. A second agent read the rendered J1 page and the extracted
J2/J3 text against the proposed claims. No human source check, historian review, native-speaker review or
sensitivity approval is claimed. Nothing here changes a signed content record or a release gate.
