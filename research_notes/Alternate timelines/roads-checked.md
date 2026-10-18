# The roads not taken, fact-checked, with the premise table (gate G2; AI DRAFT, research note, 2026-10-17)

> **AI DRAFT. No person has checked this note yet.** It is Task 3 of the alternate-timeline plan (merged in PR #943, commit `66c66b0`,
> `docs/superpowers/plans/2026-10-17-alternate-timelines.md`, issue #948). An AI pass (Claude, Anthropic) opened the
> sources and wrote every row. Under the plan's Step 3, a person checks the note against its sources and records that
> at the top in their own commit. Until then it is a draft, and no later task (4, 11 to 14) may use it. It carries no
> source check-date fields and no "checked by" line, because nobody has checked it yet.

Status: research only. Nothing here changes `content/`. Refs #948, #943, #939.

Corrected on 18 October 2026 after the independent G2 check (#949): road 29 follows the owner's ruling on the Sinai
option (#975, #939), road 11 row 3 names the right view-holder, road 13 is flagged borderline, and the Knesset PDFs
are noted as reachable. The sources of the corrected rows were opened again that day (dated 2026-10-18 where they
appear). Roads 1, 8, 19, 22 and 25 are unchanged and wait on the owner (a note in each road says what).

## Who did this and how

- **Independence (the plan's G2 requirement).** The §A.6 leads came from an OpenAI Codex pass (plan §A.6). This
  check was done by Claude, which did not write those leads. The work was split across three Claude agents
  (roads 1, 7, 10, 14, 18, 19, 28, 30; roads 2-5, 17, 23-25, 27, 29; roads 6, 8, 9, 11-13, 15, 16, 20-22, 26). A
  fourth Claude session merged their rows. A separate Claude reviewer spot-checked quotes against the saved pages
  before the PR was opened.
- **How the sources were read.** Every source quoted was fetched with a plain GET. The request carried no personal
  identifier, only a generic browser User-Agent with the words "research-fact-check". Each page was saved as text, and
  the excerpts were copied from that text. A script then matched every English excerpt and every Hebrew original
  against the saved pages, ignoring only whitespace and quote-mark style. A few PDFs are scanned images, and the AI pass
  could not read them (listed under "not reachable"). Hebrew excerpts are given in Hebrew. The English after them is
  Claude's translation, outside the quotation marks, and is not a quotation.
- **Opened.** Every URL below was opened on **2026-10-17**, the day of this pass (131 requests). That is the day
  the AI pass read the page. It is not a person's check date.
- **Search.** There is no Tavily key on the box this ran on, so the built-in web search was used to locate a few
  FRUS documents. Wikipedia was used only to find a primary record. Where a row still rests on Wikipedia, the row says
  so, and the basis does not count it as evidence.

## Conventions

- **Basis** (a proposal for Task 4's `alternative` record; the owner decides):
  - `proposed`: a named body or office is recorded at the time proposing the option.
  - `debated`: the option is recorded as argued or considered in a body, but no body is recorded proposing it.
  - `design`: nobody is recorded proposing or debating it (GDD "design assumption", `GDD.md:195`).
  - `By` names a body or an office, never a person. People are named only in source titles and descriptions.
- **Expected consequence:** only what a party publicly threatened or promised, or what officials recorded at the time
  that they feared or expected. Counterfactuals written by later historians do not count. **Strength:** "one source",
  or "several sources" when two different documents in the same road's table say the same thing.
- **Dropped:** a §A.6 lead (or a claim in an event file) that the opened sources do not support, with the reason.
- Quotation marks around a source excerpt mean it is verbatim and under 15 words. Quotation marks around an event
  file's comment, or around a §A.6 lead, quote the repo or the plan. They are not source excerpts.
- Rows name people where the sources name them. Card text would use office placeholders (`docs/WRITING_EVENTS.md`).

## Summary

Basis count: **proposed 2, debated 9, design 19** (30 roads: every non-historical option on `main` at the
branch point).

| # | Road | Basis | Qualifying expected consequences | Note |
|---|---|---|---|---|
| 1 | `altalena/compromise` | debated | 3 |  |
| 2 | `armistice_egypt/hold_out` | design | none |  |
| 3 | `armistice_jordan/hold_out` | design | none |  |
| 4 | `armistice_lebanon/hold_out` | design | none |  |
| 5 | `armistice_syria/hold_out` | design | none |  |
| 6 | `austerity_1949/decline` | design | none |  |
| 7 | `bernadotte_assassination/investigate_only` | design | none |  |
| 8 | `clothing_rationing_1950/relax` | debated | 2 | Borderline: only a modified ration is on record, not relaxing outright |
| 9 | `education_registration_1951/concede` | debated | none |  |
| 10 | `first_truce/refuse` | design | 4 | Consequences are on record but no Israeli advocate: no follow-up card under Q-ALT-3 (a) |
| 11 | `forex_crisis_1951/cut_imports` | design | 3 | The government did cut imports, but alongside the aid request; nobody is recorded proposing the cuts instead of it |
| 12 | `frumkin_commission/dismiss` | debated | 2 | Borderline; the owner may prefer `design` |
| 13 | `hula_drainage/halt` | debated | none | Borderline: the reply on record asks for delay; the owner may prefer `design` |
| 14 | `idf_founding/separate_units` | design | 1 |  |
| 15 | `iraqi_exodus/pace` | design | 2 |  |
| 16 | `israel_bonds/donations_only` | design | none |  |
| 17 | `jerusalem_ceasefire/hold_out` | design | 2 |  |
| 18 | `jerusalem_military_government/accept_demilitarisation` | debated | 5 | Q-ALT-13 (a) slice-1 road: holds as `debated` |
| 19 | `latrun_proposal/press_offensive` | proposed | 1 |  |
| 20 | `law_of_return/quota` | design | none |  |
| 21 | `maabarot_policy/keep_camps` | design | 2 |  |
| 22 | `ministry_supply_crisis/keep_ministry` | debated | none | Borderline; the owner may prefer `design` |
| 23 | `operation_hiram/advance_into_lebanon` | design | 3 | Q-ALT-13 (a) candidate, not in slice 1 (#939): `proposed` only via Wikipedia (Kurzman 1970, p. 684); design until a person checks it |
| 24 | `operation_horev/hold` | design | none |  |
| 25 | `operation_yoav/push_to_hebron` | debated | 1 | Q-ALT-13 (a) slice-1 road: `debated` on the IDF study (commanders who disagreed with the restraint); `proposed` waits on Peri 1983, pp. 58-59, or a diary entry |
| 26 | `reparations_claim/refuse` | design | none |  |
| 27 | `rhodes_talks/decline` | design | none |  |
| 28 | `second_truce/defy` | design | 3 | Q-ALT-13 (a) candidate, not in slice 1 (#939): design, so no follow-up card under Q-ALT-3 (a) |
| 29 | `sinai_withdrawal/hold_el_arish` | proposed | 4 | Q-ALT-13 (a) slice-1 road: `proposed` on the IDF-published Michelson study, for the option as reworded (owner, 18 Oct, #975) |
| 30 | `ten_days/southern_front` | debated | none | The weakest `debated`: one diary entry |

Key finding for the owner: slice 1 is three roads (#939): `operation_yoav/push_to_hebron` (`debated`),
`jerusalem_military_government/accept_demilitarisation` (`debated`) and `sinai_withdrawal/hold_el_arish` (`proposed`,
after the owner's ruling of 18 October 2026, "Reword + accept source", #975). All three pass the Q-ALT-3 (a) gate on
this check. Of the five roads first chosen under Q-ALT-13 (a), the other two are not in slice 1: `second_truce/defy`
has well-recorded consequences but no recorded Israeli advocate, and `operation_hiram/advance_into_lebanon` has an
advocate only in a secondary source. Q-ALT-13 was decided "subject to the fact-check (G2) and Q-ALT-3"; the owner
decides what follows.

## The roads

### 1. `altalena/compromise`

- Event: `content/events/altalena.yaml`, window 1948-06-15 to 1948-06-23, hard anchor; historical option `enforce`. Sensitive: `war-casualties`.
- Basis: **debated**. By: the Provisional Government's representatives in the talks with the Irgun command (the request itself came from the Irgun command, which is not the deciding party, so this is not `proposed`). When: between the 15 June meeting and the 20 June landing (the source gives no exact day).
  - **Waits on the owner (G2, 18 Oct):** the record needs a YYYY-MM-DD day and the source gives only 15-20 June 1948. Accept a day, or downgrade to `design`.
  - Evidence: "was rejected by the Government representatives, who interpreted the request as a demand" (The Altalena Affair, the Irgun's own history, https://daat.ac.il/etzel/english/ac20.htm, opened 2026-10-17)
  - Evidence: "לא היה שום הסכם, מר פנקס, למסור חלק מן הנשק לאצ"ל" (Claude's translation: *There was no agreement, Mr. Pinkas, to hand part of the arms to the Irgun*) (Ben-Gurion's reply in the Provisional State Council's Altalena debate, June 1948, as republished in his collected speeches, https://benyehuda.org/read/56639, opened 2026-10-17). This shows a council member put the arms split on the floor; the government's account denies any agreement, and the Irgun account above differs.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| Minister of Defence, statement to the Provisional State Council | June 1948 (debate of 23-24 June) | "שיפוצץ את צבאנו לשני צבאות" (Claude's translation: *[private arms for the Irgun are bound to] blow our army apart into two armies*) | https://benyehuda.org/read/56639, 2026-10-17 | several sources |
| Government representatives in the talks (reported in the Irgun's own history) | June 1948 | "a demand to reinforce an 'army within an army'" | https://daat.ac.il/etzel/english/ac20.htm, 2026-10-17 | several sources |
| Minister of Defence, statement to the Provisional State Council | June 1948 (debate of 23-24 June) | "ומכשיר את הקרקע למלחמת־אזרחים בקנה־מידה רחב" (Claude's translation: *and prepares the ground for civil war on a wide scale*) | https://benyehuda.org/read/56639, 2026-10-17 | one source |

  Rows 1 and 3 come from the same speech as the dropped breach charge below. They are kept because they are aimed at the Irgun holding arms of its own, which `compromise` grants; a person should confirm that reading.

- Dropped: "a breach of the truce's arms terms" (benyehuda 56639) — dropped as a consequence specific to `compromise`: Ben-Gurion's charge ("הפרת סידור בינלאומי של או"מ", Claude's translation: *a breach of a UN international arrangement*) is aimed at the import of the arms itself, and the Irgun account has the government consenting to the landing. It attaches to both options, so it cannot mark this road.
- Not reachable: https://fs.knesset.gov.il/0/Plenum/0_ptm_249974.pdf — HTTP 200 text/html geo-block page ("The site is temporarily inaccessible from your location"). The council's 24 June roll-call votes were not reached by another route.
- Not reachable: https://fs.knesset.gov.il/0/Plenum/0_ptm_249973.pdf — the same geo-block page. Other route: the Ben-Yehuda copy of the same statement and reply (https://benyehuda.org/read/56639) opened and is the copy used above. https://main.knesset.gov.il/About/History/Pages/default.aspx also returned the geo-block page.

### 2. `armistice_egypt/hold_out`

- Event: `content/events/armistice_egypt.yaml`, window 1949-02-24 to 1950-01-01, hard anchor; historical option `sign`.
- Basis: **design**. By: none found. When: n/a.
  - The event file names no advocate (its comment cites only the owner's rung decision of 17 October 2026, #937). Its two sources are the agreement text (Avalon arm01) and S/1264 (UNISPAL), both opened 2026-10-17; neither records an Israeli voice for refusing. The research notes (facts-1948.md, war-1948.md) name none. The agreement records only that the parties were "responding to the Security Council resolution of 16 November 1948" (Avalon arm01, https://avalon.law.yale.edu/20th_century/arm01.asp, opened 2026-10-17).
- Expected consequences:

  None found that qualifies. Resolution 62 decides "an armistice shall be established in an sectors of Palestine" [sic] (Avalon, https://avalon.law.yale.edu/20th_century/scres062.asp, opened 2026-10-17), but it states no consequence for a party that refuses, and the agreement texts say only what signing secured. This matches the §A.6 lead (weak, covered by R-WAR-006).
- Dropped: none (the §A.6 lead claims no consequence).
- Not reachable: none.

### 3. `armistice_jordan/hold_out`

- Event: `content/events/armistice_jordan.yaml`, window 1949-04-03 to 1950-01-01, hard anchor; historical option `sign`.
- Basis: **design**. By: none found. When: n/a.
  - The event file names no advocate. Its sources, the agreement text (Avalon arm03) and S/1302 (UNISPAL), both opened 2026-10-17, record no Israeli voice for refusing. The notes name none for April 1949. The nearest record is from December 1948, outside this window: on 22 December the Jerusalem commander "Implied PGI would resume hostilities if stalemate continues" (FRUS 1948 V/2 d844, https://history.state.gov/historicaldocuments/frus1948v05p2/d844, opened 2026-10-17). That is a negotiating signal reported by the US consul before the Rhodes talks, not a proposal to refuse a finished agreement.
- Expected consequences:

  None found that qualifies. The agreement and resolution 62 state no consequence of refusal (see road 2).
- Dropped: none.
- Not reachable: none.

### 4. `armistice_lebanon/hold_out`

- Event: `content/events/armistice_lebanon.yaml`, window 1949-03-23 to 1950-01-01, hard anchor; historical option `sign`.
- Basis: **design**. By: none found. When: n/a.
  - The event file names no advocate. Its sources, the agreement text (Avalon arm02) and S/1296 (UNISPAL), both opened 2026-10-17, record no Israeli voice for refusing. The notes name none.
- Expected consequences:

  None found that qualifies. The agreement and resolution 62 state no consequence of refusal (see road 2).
- Dropped: none.
- Not reachable: none.

### 5. `armistice_syria/hold_out`

- Event: `content/events/armistice_syria.yaml`, window 1949-07-20 to 1950-01-01, hard anchor; historical option `sign`.
- Basis: **design**. By: none found. When: n/a.
  - The event file names no advocate. Its sources, the agreement text (Avalon arm04) and S/1353 (UNISPAL), both opened 2026-10-17, record no Israeli voice for refusing. The notes name none.
- Expected consequences:

  None found that qualifies. The agreement and resolution 62 state no consequence of refusal (see road 2).
- Dropped: none.
- Not reachable: none.

### 6. `austerity_1949/decline`

- Event: `content/events/austerity_1949.yaml`, window 1949-04-26 to 1950-01-01, hard anchor; historical option `impose`.
- Basis: **design**. By: none found. When: n/a.
  - The event file names no advocate of free markets in April 1949; the research notes (`M3 content 1950-51/facts.md` §6) name none. JTA of 26 April and 15 May 1949 report the plan and its approval, and an attack on it by the Histadrut Agricultural Center for lacking incentives to agriculture, not a proposal to drop rationing. The Knesset's own review of the First Knesset says only that the General Zionists "protested in the Knesset plenum over the government’s economic policy" (https://web.archive.org/web/20120307095248/http://www.knesset.gov.il/review/ReviewPage.aspx?kns=1&lng=3, opened 2026-10-17): a general protest, not a recorded proposal of this option. The minutes of 26 April 1949, where an opposition speech could be, are unreachable (below).
- Expected consequences:

None found that qualifies. The only source on foreign currency that could be opened, JTA of 26 April 1949, gives the plan's aim as JTA's own understanding ("The ban on luxury imports would save foreign exchange", https://www.jta.org/archive/israeli-knesseth-to-debate-rationing-program-today-plan-to-be-presented-by-ben-gurion, opened 2026-10-17), not what an official expected if the plan were declined.

- Dropped: "Forgoing the foreign-currency saving the plan's presenters called necessary" — dropped: the presenters' words are in the Knesset minutes, which could not be opened; the JTA line is the agency's summary of the plan's aim, not a presenter's expectation about declining it.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250102.pdf — HTTP 200 text/html geo-block page ("The site is temporarily inaccessible from your location"). Other route: https://web.archive.org/web/2024id_/https://fs.knesset.gov.il/1/Plenum/1_ptm_250102.pdf — HTTP 404, "The Wayback Machine has not archived that URL."

### 7. `bernadotte_assassination/investigate_only`

- Event: `content/events/bernadotte_assassination.yaml`, window 1948-09-23 to 1948-10-16, hard anchor; historical option `outlaw`. Sensitive: `terror`.
- Basis: **design**. By: none found. When: n/a.
  - The event file says "the notes name no advocate for this option". Notes `facts-1948.md` section 6, JTA of 20 September 1948, S/1004, S/1315 and FRUS 1948 V-2 doc 609 (the cabinet decrees, the ultimatum and the arrests) name no Israeli body arguing to ease the crackdown. The council minutes of 23 September (0_ptm_249987) could not be opened.
- Expected consequences: None found that qualifies. Bunche's cable (S/1004, https://www.un.org/unispal/document/auto-insert-210428/, opened 2026-10-17) holds the government responsible for the killing itself ("BREACH OF THE TRUCE OF UTMOST GRAVITY"), which applies to both options. A State Department circular of 19 September, quoted in FRUS doc 609, says only that the US was "confident Arab and Jewish leaders will take every precaution to prevent further violence". No document opened says what would follow if the crackdown were eased.
- Dropped: the event file's extra `intl_standing -0.10` for easing the crackdown, as a sourced reaction — dropped: no source opened records anyone expecting it. It stays a design value.
- Not reachable: https://fs.knesset.gov.il/0/Plenum/0_ptm_249987.pdf — HTTP 200 text/html geo-block page. No other route found for the 23 September sitting; the main.knesset.gov.il route is geo-blocked too.

### 8. `clothing_rationing_1950/relax`

- Event: `content/events/clothing_rationing_1950.yaml`, window 1950-07-01 to 1950-09-01, hard anchor; historical option `extend`.
- Basis: **debated**. By: the Knesset (the plenum debate on the rationing strike, which ended in a confidence vote); the Minister of Supply then reportedly accepted a modified, lighter programme. When: August 1950 (vote of 8 August per the event file; JTA report 14 August).
  - **Waits on the owner (G2, 18 Oct):** the day 8 August 1950 comes from the event file only, not from an opened source. Accept it, or downgrade to `design`.
  - Evidence: "a heated Knesset session in which the government sustained a 57-36 vote" (JTA, 14 Aug 1950, https://www.jta.org/archive/strike-of-clothing-and-shoe-storekeepers-in-israel-ends-modified-rationing-seen, opened 2026-10-17)
  - Evidence: "Minister Joseph agreed to accept a more modified rationing program" (same JTA report; the report says this was "understood", the details were not published)
  - Note: the demand to modify the law came from the Tel Aviv Merchants Association (JTA, 2 Aug 1950), a trade body outside the government, so it does not count as `proposed`. The opposition motions themselves are in the minutes of 8 August, which could not be opened. Relaxing rationing outright, as the option's text has it ("let imports fill the shops"), is not on record; only a lighter clothing ration is.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| Tel Aviv Merchants Association (public notices in shop windows) | 1950-08-02 | "will remain shut down until a fair arrangement is concluded" | https://www.jta.org/archive/tel-aviv-merchants-strike-against-rationing-of-clothing-and-shoe-keep-stores-closed, 2026-10-17 | one source |
| JTA, giving the ordinance's purpose (not a named official) | 1950-08-14 | "drastic rationing of the scarce commodities in order to save on foreign currency" | https://www.jta.org/archive/strike-of-clothing-and-shoe-storekeepers-in-israel-ends-modified-rationing-seen, 2026-10-17 | one source |

- Dropped: "An end to the retailers' strike under a modified programme" — kept only as the merchants' promise above; that the strike did end after a modified programme was agreed is what happened under the historical option, not an expectation about `relax`.
- Dropped: "More foreign-currency spending" as an official's expectation — dropped as stated: the only opened source is JTA's description of the ordinance's aim (row 2); the minister's and the Knesset's words are in the unreachable minutes.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250241.pdf — HTTP 200 text/html geo-block page. Other route: Wayback copy (`web.archive.org/web/2024id_/…1_ptm_250241.pdf`) — HTTP 404, not archived.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250242.pdf — HTTP 200 text/html geo-block page. Other route: Wayback copy — HTTP 404, not archived.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250056.pdf — HTTP 200 text/html geo-block page.

### 9. `education_registration_1951/concede`

- Event: `content/events/education_registration_1951.yaml`, window 1950-11-29 to 1951-02-14, hard anchor; historical option `press_plan`.
- Basis: **debated**. By: the cabinet (a discussion on the immigrant camps, after which the Minister of Religion resigned). When: 4 January 1951 (JTA report of 5 January: "last night").
  - Evidence: "after a stormy Cabinet discussion on the situation in the immigrant camps" (JTA, 5 Jan 1951, https://www.jta.org/archive/new-crisis-in-israel-cabinet-orthodox-minister-resigns-two-more-may-quit-government, opened 2026-10-17)
  - Note: this records the dispute being argued in cabinet, not the religious parties' demand in their own words. The research notes (`M3 content 1950-51/facts.md` item 7) frame the option as the religious parties' demand, citing en/he Wikipedia only; the Knesset minutes of the registration debate could not be opened. If the owner wants `proposed` (by the United Religious Front, a coalition party), a primary record of its demand is still needed.
- Expected consequences:

None found that qualifies. The one recorded fear in the opened sources runs the other way: Religious Bloc circles warned of "the possible resignation of the other two Orthodox members of the Cabinet" (same JTA report), which bears on pressing the plan, not on conceding. No opened source records what the labour side threatened if the government conceded.

- Dropped: none (no §A.6 lead; §A.6 lists this road under "No qualifying lead", which the check confirms for consequences).
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250264.pdf — HTTP 200 text/html geo-block page.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250266.pdf — HTTP 200 text/html geo-block page.

### 10. `first_truce/refuse`

- Event: `content/events/first_truce.yaml`, window 1948-06-01 to 1948-06-11, hard anchor; historical option `accept`.
- Basis: **design**. By: none found. When: n/a.
  - The event file says "the notes found no Israeli voice for refusing". FRUS 1948 V-2 doc 231 (Rusk, 7-8 May 1948) mentions a "bitter debate going on within the Jewish Agency on the subject of the truce". That concerns the May truce articles, before the state existed and before Resolution 50, so it is not a debate on refusing this truce and is not used as a basis.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| UN Security Council (Resolution 50, para. 11) | 29 May 1948 | "the situation in Palestine will be reconsidered with a view to action" (followed by "under Chapter VII") | https://documents.un.org/api/symbol/access?s=S/RES/50(1948)&l=en&t=pdf, 2026-10-17 (same text: https://avalon.law.yale.edu/20th_century/scres050.asp, 2026-10-17) | several sources |
| US Department of State (Rusk's memorandum: points already made to the Jewish Agency's Shertok) | 7-8 May 1948 | "will urge the Security Council to take the necessary action" (if a truce is not agreed) | https://history.state.gov/historicaldocuments/frus1948v05p2/d231, 2026-10-17 | several sources |
| French representative at the UN, in consultations with the US, UK and Belgian delegations (as reported by the US Representative at the UN) | 6 May 1948 | "would be relatively easy apply sanctions against Jews, whether economic or military" | https://history.state.gov/historicaldocuments/frus1948v05p2/d227, 2026-10-17 | one source |
| US Department of State, Office of UN Affairs (McClintock memorandum, on the economic sanctions study) | 1 July 1948 | "Israel could be almost fatally injured by the application of economic sanctions" | https://history.state.gov/historicaldocuments/frus1948v05p2/d428, 2026-10-17 | one source |

  Note: docs 227 and 231 deal with the May truce talks before Resolution 50. Doc 428 is dated during the truce and covers a breach or a resumed war, not a refusal on 1 June.
- Dropped: none (both §A.6 leads hold: the Chapter VII clause is in Resolution 50, and doc 428 records the sanctions study, with the date caveat above).
- Not reachable: none.

### 11. `forex_crisis_1951/cut_imports`

- Event: `content/events/forex_crisis_1951.yaml`, window 1951-10-19 to 1952-01-01, soft anchor; historical option `request_aid`.
- Basis: **design**. By: none found. When: n/a. The government is on record imposing the cuts alongside the aid request (by 23 October 1951), not proposing them instead of it.
  - Record of the measure: "has imposed a “very brutal” limitation on import licenses" (JTA, 23 Oct 1951, citing the New York Times from Tel Aviv, https://www.jta.org/archive/u-s-credits-will-check-foreign-currency-crisis-in-israel, opened 2026-10-17)
  - Note for the owner: the record shows the government doing both things at once, so "cut imports further" as a substitute for asking Washington is the card's framing. A `proposed` record would put "Not adopted" (plan §A.4.7) on a measure that was in fact adopted, so the basis is `design`: nobody is recorded proposing the cuts as the alternative to aid.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| US Ambassador in Israel (telegram on the position without emergency aid) | 1951-10-21 | "threatens so to jeopardize credit standing as to endanger imminent collapse of financing structure" | https://history.state.gov/historicaldocuments/frus1951v05/d514, 2026-10-17 | one source |
| US Ambassador in Israel (recommending that Kaplan's request be met) | 1951-10-19 | "eventual embarrassment which cld not help but affect adversely their internatl reputation" | https://history.state.gov/historicaldocuments/frus1951v05/d513, 2026-10-17 | one source |
| The New York Times (Tel Aviv), via JTA (on the import cuts already made) | 1951-10-23 | "enabled the Government to fulfill its obligations to foreign creditors" | https://www.jta.org/archive/u-s-credits-will-check-foreign-currency-crisis-in-israel, 2026-10-17 | one source |

  Rows 1 and 2 are the same embassy, two telegrams: one source each, not "several". Row 3 is the newspaper's own sentence that the cuts worked, which cuts against rows 1-2; both are on record. The officials it cites are reported saying only "All obligations will continue to be fulfilled" (same JTA report). A newspaper is not an official, so a person decides whether row 3 qualifies under the conventions; if not, the count is 2.
- Dropped: none (§A.6 has no lead for this road).
- Not reachable: none.

### 12. `frumkin_commission/dismiss`

- Event: `content/events/frumkin_commission.yaml`, window 1950-01-01 to 1950-05-09, soft anchor; historical option `inquiry`.
- Basis: **debated**. By: the Ministry of Education and Culture (its own inquiry, published in Davar), and the Ministers of Police and the Interior (in the Knesset). When: 17 January 1950 (Davar); February 1950 (Knesset, after the arrests of 14 February).
  - Evidence: "חקירת משרד החינוך מפריכה את התלונות על רדיפות דת במחנות" (Claude's translation: *The Education Ministry's inquiry refutes the complaints of religious persecution in the camps*; the Davar headline of 17.1.50 as quoted in Levitan, "The First State Commission of Inquiry", Niv HaMidrasha 20-21, https://www.daat.ac.il/daat/kitveyet/niv/levitan-1.htm, opened 2026-10-17)
  - Evidence: "לא מצאו כל הצדקה לטענות נגד מנהלי ועובדי המחנות" (Claude's translation: *found no justification for the charges against the camps' managers and staff*; the Police Minister reporting his and the Interior Minister's camp visits, same Levitan article)
  - Note: both are offices on the government side rejecting the charges, which is the option's first half. Nobody is recorded proposing that the cabinet refuse an inquiry; the cabinet discussed the camps on 17.1.50 and, per Levitan, decided on a committee "a week later", though elsewhere the same article puts the decision on 17.1.50 (JTA in the event file puts the appointment on 19 January; the event file says 17 January; the sources conflict and a person must settle it). The source is historiography quoting contemporary press and Knesset remarks; the Knesset minutes themselves could not be opened. A borderline case: the owner may prefer `design`.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| The religious circles in the coalition (after the cabinet's decision for a five-member committee: about 24 January per Levitan, 19 January per JTA) | January 1950 | "והם חידשו את איומיהם לפרוש מהממשלה" (Claude's translation: *and they renewed their threats to leave the government*) | https://www.daat.ac.il/daat/kitveyet/niv/levitan-1.htm, 2026-10-17 | one source |
| Immigration Minister, a leader of the Religious Bloc | 1950-02-02 | "a stumbling bloc to formation of a wider coalition Cabinet" | https://www.jta.org/archive/government-inquiry-committee-opens-hearings-on-religious-education-in-israel-camps, 2026-10-17 | one source |

  Row 1 is a later historian's summary of the threat (Levitan, 1987-88), not the threat's own text. The two rows say different things (leaving the government; blocking a wider coalition), so neither is "several sources".
- Dropped: "URF threats to leave the coalition (March 1950)" (research notes, item 1, en/he Wikipedia) — dropped as dated: the opened source places renewed threats in January 1950, after the decision on a committee; no source for March was opened.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250034.pdf — HTTP 200 text/html geo-block page.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250129.pdf — HTTP 200 text/html geo-block page.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250126.pdf — HTTP 200 text/html geo-block page.

### 13. `hula_drainage/halt`

- Event: `content/events/hula_drainage.yaml`, window 1951-03-07 to 1951-05-18, soft anchor; historical option `continue_works`.
- Basis: **debated**. By: the Israeli government (as its delegation to the Mixed Armistice Commission reported). When: 8 March 1951; the work then stopped for some days before 23 March.
  - Evidence: "its Government must give the question further and careful consideration" (FRUS 1951 v.V doc 308, State Department summary of 6 April 1951, entry for 8 March, https://history.state.gov/historicaldocuments/frus1951v05/d308, opened 2026-10-17)
  - Evidence: "Apparently at some point during this period the operations had ceased" (same document, entry for 19 March)
  - Note: the halt was requested by the UN (the Chief of Staff of the Truce Supervision Organisation and the MAC chairman), not by an Israeli body, so it is not `proposed`. On 15 March the Israeli delegate "flatly refused" to stop (same document), and JTA of 27 March has the work resumed. No cabinet record was opened.
  - Borderline (G2, 18 Oct): "must give the question further and careful consideration" is a request for delay (the delegation sent it instead of appearing for the postponed vote), not a recorded consideration of halting the works. The label stays `debated`; the owner may prefer `design`.
- Expected consequences:

None found that qualifies. The recorded threats and fears concern continuing, not halting: the Syrian Minister of Defence said that "if Israel continued operations, the Syrian army was prepared to take punitive action" (FRUS d308, 18 March), and the UN Chief of Staff agreed that if the work "is continued without a mutual agreement" trouble may result (FRUS 1951 v.V doc 288, https://history.state.gov/historicaldocuments/frus1951v05/d288, opened 2026-10-17). These bear on `continue_works`.

- Dropped: none (§A.6 has no lead for this road).
- Not reachable: none.

### 14. `idf_founding/separate_units`

- Event: `content/events/idf_founding.yaml`, window 1948-05-15 to 1948-06-01, hard anchor; historical option `one_army`.
- Basis: **design**. By: none found. When: n/a.
  - The event file says "the notes name no one who argued for this; it is the status quo the order ended". The MoD archive page on the IDF's establishment and the JVL/IDF founding page name no body proposing to keep the units outside. Ben-Gurion's June statement (benyehuda 56639) has the Irgun itself undertaking on 1 June to join the IDF.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| Minister of Defence, to the Provisional State Council (after the window, in the Altalena debate) | June 1948 (debate of 23-24 June) | "משטר זה שאתם ממליצים עליו הוא מלחמת־אזרחים, ארגון צבאות נפרדים" (Claude's translation: *This regime you recommend is civil war, the organising of separate armies*) | https://benyehuda.org/read/56639, 2026-10-17 | one source |

- Dropped: none (no §A.6 lead for this road).
- Not reachable: https://fs.knesset.gov.il/0/Plenum/0_ptm_249973.pdf — geo-block page (see road 1). The Ben-Yehuda copy was used.

### 15. `iraqi_exodus/pace`

- Event: `content/events/iraqi_exodus.yaml`, window 1951-03-10 to 1951-07-01, soft anchor; historical option `airlift`.
- Basis: **design**. By: none found. When: n/a.
  - The event file names no advocate; the research notes (`M3 content 1950-51/facts.md` item 4) say the Zionist movement "initially tried to slow registration" in 1950 (citing Meir-Glitzenstein, whose PDF could not be opened, below) and warn that "No primary Israeli cabinet decision was seen". The opened sources for March 1951 (JTA 15 March; the Israeli letter to the Conciliation Commission of 19 March; FRUS d340) record the airlift being sped up, and nobody proposing to pace it. The Jewish Agency's selective-immigration rule of 19 November 1951 is outside the window and exempts "emergency immigration" when whole communities are endangered (https://www.jta.org/archive/selective-immigration-system-to-be-introduced-for-israel, opened 2026-10-17).
- Expected consequences (recorded fears for Jews left waiting in Iraq):

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| Delegation of the Iraqi Jewish community in Tel Aviv, to the Foreign Ministry | 1951-03-15 | "fear that the Iraqi Government will take even more drastic measures" | https://www.jta.org/archive/anti-jewish-terror-continues-in-baghdad-airlift-of-jews-from-iraq-is-speeded-up, 2026-10-17 | one source |
| Director General of the Israeli Ministry of Foreign Affairs (letter to the Conciliation Commission) | 1951-03-19 | "while waiting for departure calls for urgent measures of relief inside Iraq" | https://www.un.org/unispal/document/auto-insert-211728, 2026-10-17 | one source |

- Dropped: none (§A.6 lists this road under "No qualifying lead"; confirmed for the basis).
- Not reachable: https://in.bgu.ac.il/bgi/iyunim/40/Meir-Glitzenstein.pdf — HTTP 200 text/html, a 40-character anti-bot stub page (text "Powered and protected by" and "Privacy"), no PDF.

### 16. `israel_bonds/donations_only`

- Event: `content/events/israel_bonds.yaml`, window 1951-05-10 to 1951-05-11, hard anchor; historical option `launch`.
- Basis: **design**. By: none found. When: n/a.
  - The event file and research notes (item 8) name no advocate of relying on donations alone. The closest record is American, not Israeli: executives of the Council of Jewish Federations and Welfare Funds discussed "the implications of a probable Israel Government bond drive" on the UJA and asked that bond money be "over and above" welfare-fund giving (JTA, 12 Sep 1950, https://www.jta.org/archive/executives-of-jewish-federations-hold-five-day-conference-on-problems-facing-u-s-jewry, opened 2026-10-17). That is a concern about overlap, not a proposal to drop bonds. The Knesset passed the bonds law with one vote against (the event file's note on the minutes, which could not be opened).
- Expected consequences:

None found that qualifies.

- Dropped: none (§A.6 lists this road under "No qualifying lead"; confirmed).
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250158.pdf — HTTP 200 text/html geo-block page.

### 17. `jerusalem_ceasefire/hold_out`

- Event: `content/events/jerusalem_ceasefire.yaml`, window 1948-11-30 to 1948-12-16, hard anchor; historical option `keep`.
- Basis: **design**. By: none found. When: n/a.
  - The event file says "no one is recorded arguing for it". Its source, FRUS d800 (opened 2026-10-17), records only the signing of a "complete and sincere ceasefire". The notes (facts-1948.md section 13b) give "hold out for the Old City / Latrun road" as a design option with no advocate. FRUS d844 (23 December 1948, after the window) reports the Jerusalem commander implying Israel "would resume hostilities if stalemate continues": a threat about stalled armistice talks, not a proposal to break the 30 November cease-fire for the Old City or the Latrun road.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| The Security Council Truce Commission at Jerusalem (as reported by the US Consul General) | 23 Dec 1948 | "quickly terminate war by pushing to Jordan" | https://history.state.gov/historicaldocuments/frus1948v05p2/d844, 2026-10-17 | one source |
| The US Consul General at Jerusalem | 23 Dec 1948 | "risking decision by PGI resume hostilities and gradual termination ceasefire Jerusalem" | https://history.state.gov/historicaldocuments/frus1948v05p2/d844, 2026-10-17 | one source |

  Both rows are from one telegram dated a week after the window. They concern Israel resuming the war against Transjordan in general, not the Old City or the Latrun road. Weak: the owner may judge they do not fit this road.
- Dropped: none (§A.6 lists this road under "No qualifying lead"; the rows above are new and weak).
- Not reachable: none.

### 18. `jerusalem_military_government/accept_demilitarisation`

- Event: `content/events/jerusalem_military_government.yaml`, window 1948-07-15 to 1948-08-03, hard anchor; historical option `military_government`.
- Basis: **debated**. By: the Provisional Government of Israel, in its replies to the Mediator. When: before the end of the first truce (proposal of 3 July); rejection reaffirmed on 28 July 1948.
  - Evidence: "the Provisional Government of Israel was prepared to discuss the proposal under certain assumptions" (Mediator's progress report A/648, part one ch. IV, https://unispal.un.org/pdfs/AB14D4AAFC4E1BB985256204004F55FA.pdf, opened 2026-10-17)
  - Evidence: "the Provisional Government reaffirms its rejection of the Mediator's plan of demilitarization" (Shertok's letter of 28 July, quoted in the same report, opened 2026-10-17)
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| US Department of State, Office of UN Affairs (McClintock memorandum) | 1 July 1948 | "the forces of the Hagannah and the Arab Legion would retire from Jerusalem" | https://history.state.gov/historicaldocuments/frus1948v05p2/d428, 2026-10-17 | one source |
| UN Mediator's proposal, as reported by the Office of UN Affairs (same memorandum) | 1 July 1948 | "fill the vacuum by a 1,000 man United Nations armed guard" | https://history.state.gov/historicaldocuments/frus1948v05p2/d428, 2026-10-17 | several sources |
| UN Mediator (interim report S/979 of 19 August, quoted in A/648) | 19 Aug 1948 | "without a strong adequately armed United Nations force to be provided immediately" | https://unispal.un.org/pdfs/AB14D4AAFC4E1BB985256204004F55FA.pdf, 2026-10-17 | several sources |
| Provisional Government of Israel (Foreign Minister's letter of 28 July, quoted in A/648) | 28 Jul 1948 | "proposed to place Jerusalem ultimately under Arab rule" (why it feared the Mediator's plans) | https://unispal.un.org/pdfs/AB14D4AAFC4E1BB985256204004F55FA.pdf, 2026-10-17 | one source |
| Palestine Arab Higher Committee (statement of 25 July, quoted in A/648) | 25 Jul 1948 | "impossible actually to disarm the Jews, thus exposing the Holy City" | https://unispal.un.org/pdfs/AB14D4AAFC4E1BB985256204004F55FA.pdf, 2026-10-17 | one source |

- Dropped: "a UN regime for the city" (FRUS doc 428) — dropped: the memo gives an international regime as the long-range settlement the Department favoured, not as a result of demilitarising. A/648 quotes Resolution 54 asking for demilitarisation "without prejudice to the future political status of Jerusalem".
- Not reachable: https://fs.knesset.gov.il/0/Plenum/0_ptm_249980.pdf — HTTP 200 text/html geo-block page. Other route: A/648 quotes Shertok's 28 July letter (used above). The JTA report of 27 July (https://www.jta.org/archive/bernadotte-confers-with-shertok-on-armistice-demilitarizing-jerusalem-immigration) opened and only lists demilitarisation as an agenda item.

### 19. `latrun_proposal/press_offensive`

- Event: `content/events/latrun_proposal.yaml`, window 1948-09-15 to 1948-09-27, hard anchor; historical option `accept_vote`.
- Basis: **proposed**. By: the Prime Minister and Minister of Defence, in the cabinet. When: 26 September 1948.
  - **Waits on the owner (G2, 18 Oct):** the option reads "Press the attack on Latrun despite the vote", but the record shows a proposal to attack before the vote, not to override the cabinet, and the diary quote could not be re-checked (archive unreachable). Reword the option, or downgrade to `debated` or `design`.
  - Evidence: "about my proposal to attack and conquer Latrun" (Ben-Gurion's diary, 26 September 1948, Ben-Gurion Archive English translation, https://bengurionarchive.bgu.ac.il/en/node/6251, opened 2026-10-17)
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| UN Security Council (Resolution 54, para. 3, the standing cease-fire order Latrun would break) | 15 Jul 1948 | "a breach of the peace within the meaning of Article 39 of the Charter" (followed by "further action under Chapter VII") | https://documents.un.org/api/symbol/access?s=S/RES/54(1948)&l=en&t=pdf, 2026-10-17 | one source |

  The diary records the vote but not the ministers' reasons. No document opened records what the opposing ministers feared.
- Dropped: none (§A.6 lists this road as "no qualifying lead").
- Not reachable: https://in.bgu.ac.il/bgi/israelis/DocLib/Pages/2012/ליאור-לביד.pdf — HTTP 200 text/html bot-check page (a "bm-verify" redirect), not the PDF.

### 20. `law_of_return/quota`

- Event: `content/events/law_of_return.yaml`, window 1950-07-05 to 1950-07-06, hard anchor; historical option `enact`.
- Basis: **design**. By: none found. When: n/a.
  - The law passed unanimously (event file); the research notes (item 3) call it "Not a contested vote". A JTA archive search found no 1950 report of a quota being proposed. The Jewish Agency's selection rules come in November 1951 (JTA, https://www.jta.org/archive/selective-immigration-system-to-be-introduced-for-israel, opened 2026-10-17), sixteen months later, and are selection criteria, not a yearly quota. The minutes and the bill could not be opened.
- Expected consequences:

None found that qualifies.

- Dropped: none (§A.6 has no lead for this road).
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250144.pdf — HTTP 200 text/html geo-block page.
- Not reachable: https://fs.knesset.gov.il/1/law/1_lsr_209667.PDF — HTTP 200 text/html geo-block page.

### 21. `maabarot_policy/keep_camps`

- Event: `content/events/maabarot_policy.yaml`, window 1950-04-01 to 1950-06-01, hard anchor; historical option `maabarot`.
- Basis: **design**. By: none found. When: n/a.
  - The event file, the research notes (§5, item 5), the Central Zionist Archives page and Kachansky's chapter name Eshkol's proposal and no advocate of keeping the free-board camps. Kachansky says the opposite: everyone agreed the status quo could not hold (row 2).
- Expected consequences (recorded about keeping the camps as they were):

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| Treasurer of the Jewish Agency and head of its Settlement Department, at the Agency executive | 1950-03-27 | "והנה עומדים להתוסף השנה עוד למעלה ממאתיים אלף נפש" (Claude's translation: *and more than two hundred thousand more people are about to be added this year*) | https://www.lib.cet.ac.il/pages/item.asp?item=12939, 2026-10-17 | one source |
| Those concerned at the time, per the historian (Kachansky, Yad Ben-Zvi 1987) | early 1950 | "הכל הסכימו שהשארת המצב הקיים פירושו 'התפוצצות מוכרחת' קרובה" (Claude's translation: *all agreed that leaving things as they were meant an inevitable explosion soon*) | https://www.lib.cet.ac.il/pages/item.asp?item=12939, 2026-10-17 | one source |

  Row 2 is the historian's summary around a quoted phrase, not a dated document.
- Dropped: none (§A.6 has no lead for this road).
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250140.pdf — HTTP 200 text/html geo-block page.

### 22. `ministry_supply_crisis/keep_ministry`

- Event: `content/events/ministry_supply_crisis.yaml`, window 1950-10-01 to 1950-11-01, hard anchor; historical option `restructure`.
- Basis: **debated**. By: Mapai (the Prime Minister's party), internally. When: October 1950.
  - **Waits on the owner (G2, 18 Oct):** the record needs a YYYY-MM-DD day and the source gives only "October 1950". Accept a day, or downgrade to `design`.
  - Evidence: "an internal crisis in the Prime Minister’s party over the liquidation" (Knesset, review of the First Knesset, archived copy, https://web.archive.org/web/20120307095248/http://www.knesset.gov.il/review/ReviewPage.aspx?kns=1&lng=3, opened 2026-10-17)
  - Note: a party, not the cabinet or the Knesset, and the source is the Knesset's later review, not a minute. No opened source names the Mapai faction that wanted to keep the ministry. The Knesset minutes of the crisis could not be opened, and two JTA searches found no report naming the ministry. Borderline between `debated` and `design`.
- Expected consequences:

None found that qualifies. A Religious Bloc leader's JTA statement of 25 October 1950 (https://www.jta.org/archive/leader-of-israel-orthodox-bloc-favors-re-establishment-of-old-coalition-cabinet, opened 2026-10-17) blames the crisis on agreements "ignored or else changed unilaterally", not on the ministry.

- Dropped: none (§A.6 lists this road under "No qualifying lead"; confirmed for consequences).
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250243.pdf — HTTP 200 text/html geo-block page.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250245.pdf — HTTP 200 text/html geo-block page.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250246.pdf — HTTP 200 text/html geo-block page.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250247.pdf — HTTP 200 text/html geo-block page.

### 23. `operation_hiram/advance_into_lebanon`

- Event: `content/events/operation_hiram.yaml`, window 1948-10-31 to 1948-11-16, soft anchor; historical option `stop_at_ceasefire`. Sensitive: `war-casualties`.
- Basis: **design** for now (a lead points to `proposed`, but only through a secondary source; CONTENT_POLICY lets Wikipedia locate a record, not stand in for one). Lead: the deputy commander of the Northern Front, about 31 October 1948 (not dated in the source).
  - Evidence: "requested permission to advance and occupy Beirut" (English Wikipedia, "Operation Hiram", locator only, citing Kurzman, *Genesis 1948* (1970), p. 684; https://en.wikipedia.org/wiki/Operation_Hiram, opened 2026-10-17)
  - No record from the time was found. Ben-Gurion's diary for 31 October (https://bengurionarchive.bgu.ac.il/node/12338, opened 2026-10-17) has him at Northern Front headquarters in Nazareth, not Tiberias, and does not mention the request. The diary pages for 22, 24, 28 and 29 October listed in the event file (nodes 7062, 6631, 5661, 12460, opened 2026-10-17) do not mention it either. Until a person checks Kurzman p. 684 or a diary entry, the strict reading is **design**.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| The Mediator's political representative for Syria and Lebanon (reported by the US Minister in Syria) | 3 Nov 1948 | "especially as regards provisions for return to original truce lines" | https://history.state.gov/historicaldocuments/frus1948v05p2/d728, 2026-10-17 | one source |
| The same | 3 Nov 1948 | "Servoise does not believe Jews will withdraw regardless of SC action" | https://history.state.gov/historicaldocuments/frus1948v05p2/d728, 2026-10-17 | one source |
| The UN Department of Public Information (press release PAL/370) | 6 Nov 1948 | "Dr. Bunche's report will come before the Security Council at its next meeting" | https://www.un.org/unispal/document/auto-insert-193573/, 2026-10-17 | one source |

  These records concern the incursion that had already happened (about fifteen Lebanese villages), not a deeper advance. They show that UN officials expected the Council to take up the Lebanese incursion and press for a return to the truce lines.
- Dropped: "International condemnation" — dropped: the only source is Wikipedia's "Fearing international condemnation, Ben-Gurion refrained from taking action", citing Kurzman (1970). That is a later secondary account. No record from the time of that fear was found.
- Dropped: "UN pressure to withdraw behind the frontier" with S/1071 — partly kept: d728 confirms a recommendation to the Council for a return to the truce lines (one source). S/1071 itself could not be read (see below).
- Not reachable: https://documents.un.org/api/symbol/access?s=S/1071&l=en&t=pdf — HTTP 200 PDF, but pages 2 to 5 are a scanned image with unreadable OCR, and no PDF renderer is installed on this box for a visual read. Not used.
- Not reachable: https://m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf — HTTP 200 text/html, the Knesset geo-block page ("The site is temporarily inaccessible from your location"). Second route https://main.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf returned the same geo-block page. Not used for any claim.

### 24. `operation_horev/hold`

- Event: `content/events/operation_horev.yaml`, window 1948-12-15 to 1948-12-23, hard anchor; historical option `launch`. Sensitive: `war-casualties`.
- Basis: **design**. By: none found. When: n/a.
  - The event file says "the notes give no alternative to launching; this one names no advocate". Its sources (JVL Horev, the IDF Archives page 050115, resolution 66, S/1225), all opened 2026-10-17, record no Israeli voice for holding. The notes (facts-1948.md section 9, war-1948.md) name none. The only related record is the Prime Minister's later statement to the US: "We accepted the UN request for an armistice and peace. Egypt rejected it." (FRUS 1949 VI d341, 1 Jan 1949, https://history.state.gov/historicaldocuments/frus1949v06/d341, opened 2026-10-17). That is a justification given after the launch, not a proposal to wait.
- Expected consequences:

  None found that qualifies. No source records what anyone expected from not launching.
- Dropped: none (§A.6 lists this road under "No qualifying lead").
- Not reachable: none. (The JVL Horev page is reachable but is a short summary that cites only Wikipedia.)

### 25. `operation_yoav/push_to_hebron`

- Event: `content/events/operation_yoav.yaml`, window 1948-10-15 to 1948-10-23, hard anchor; historical option `stop_at_ceasefire`. Sensitive: `war-casualties`.
- Basis: **debated**. By: the Southern Front command, as recorded by the IDF History Department. When: between Yoav and Horev (October to December 1948); the study gives no date.
  - **Waits on the owner (G2, 18 Oct):** the record needs a YYYY-MM-DD day and the study gives none. Accept a day, or downgrade to `design`.
  - Evidence: "וההנהגה, מצידה, בלמה מצביאים, כמו אלון" (Claude's translation: *and the leadership, for its part, held back commanders such as Allon*) (Benny Gshur, "Mount Hebron in the War of Independence", IDF History Department, 2022, p. 24, https://www.idf.il/media/rccdyzpl/%D7%94%D7%A8-%D7%97%D7%91%D7%A8%D7%95%D7%9F-%D7%91%D7%9E%D7%9C%D7%97%D7%9E%D7%AA-%D7%94%D7%A2%D7%A6%D7%9E%D7%90%D7%95%D7%AA-%D7%90%D7%97%D7%A8%D7%99-%D7%A2%D7%99%D7%9E%D7%95%D7%93-1.pdf, opened 2026-10-17)
  - Claude's read of p. 24: the sentence names Allon and Yosef Tabenkin and ends "אשר חלקו עליה" (Claude's translation: *who disagreed with it*), citing Frankel, *Operation HaHar*, p. 117 (footnote 172). "It" is the policy of restraint toward the Legion, which the study traces from Yoav through Horev. The study records commanders disagreeing with that policy, not a dated request to push into Hebron. That is a record of debate, so `debated`, by the same standard as road 23: there too the request itself rests only on Wikipedia.
  - Locator for the request itself (not counted as evidence): "General Allon sought permission to launch an attack on the Hebron hills" (English Wikipedia, "Operation Yoav", citing Peri 1983, pp. 58-59; https://en.wikipedia.org/wiki/Operation_Yoav, opened 2026-10-17). The IDF study records no such request, no date for it and no Jericho valley. On the same front it records Harel's advance being stopped at Husan, "שם נעצרה בהוראת הדרג המדיני" (Claude's translation: *where it was stopped by order of the political level*) (same study, p. 8).
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| The Prime Minister and Minister of Defence (war diary, quoted by the IDF History Department study) | 1948, war diary vol. III, p. 819 (day not given in the study) | "מלחמה על חברון היא מלחמה על הלגיון, והוא ירחיב את המלחמה בכל מקום הימצאו" (Claude's translation: *a war on Hebron is a war on the Legion, and it will widen the war wherever it is*) | https://www.idf.il/media/rccdyzpl/%D7%94%D7%A8-%D7%97%D7%91%D7%A8%D7%95%D7%9F-%D7%91%D7%9E%D7%9C%D7%97%D7%9E%D7%AA-%D7%94%D7%A2%D7%A6%D7%9E%D7%90%D7%95%D7%AA-%D7%90%D7%97%D7%A8%D7%99-%D7%A2%D7%99%D7%9E%D7%95%D7%93-1.pdf, 2026-10-17 | one source |

- Dropped: "fear of British intervention" — dropped: the only source is Wikipedia's "mainly due to fears that this would lead to British involvement", citing Peri (1983). The IDF study, the IDF account on JVL, FRUS d705, S/1042 and the UN record (UNISPAL 210876), all opened 2026-10-17, do not tie Britain to the Hebron decision.
- Not reachable: https://m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf — Knesset geo-block page (HTTP 200 text/html); main.knesset.gov.il route returned the same. Used in the event file only for dates, not needed here.

### 26. `reparations_claim/refuse`

- Event: `content/events/reparations_claim.yaml`, window 1951-03-01 to 1951-03-13, hard anchor; historical option `claim`. Sensitive: `holocaust`.
- Basis: **design**. By: none found. When: n/a.
  - The event file, the research notes (item 10) and the opened sources (JTA of 14 March 1951; FRUS 1951 v.V docs 296 and 297) record the note to the four powers and no Israeli body proposing to refuse any claim in March 1951. The organised opposition to dealing with Germany is on record for the Knesset debate of January 1952, outside this window. The Knesset sitting of 13 March 1951, where an objection could have been raised, could not be opened. A JTA search for 1951 found no such report.
- Expected consequences:

None found that qualifies.

- Dropped: "Forgoing the four-power claim of $1.5 billion" — dropped: this restates the option (refusing means making no claim); FRUS d296 records no one's expectation about refusal. It records a US official's view that the powers were "extremely unlikely" to back the claim, which concerns the historical option.
- Not reachable: https://fs.knesset.gov.il/1/Plenum/1_ptm_250159.pdf — HTTP 200 text/html geo-block page.

### 27. `rhodes_talks/decline`

- Event: `content/events/rhodes_talks.yaml`, window 1949-01-09 to 1949-01-13, hard anchor; historical option `send_delegation`.
- Basis: **design**. By: none found. When: n/a.
  - The event file says "no one is recorded arguing for this". FRUS d362 (opened 2026-10-17) records that both governments notified "their unconditional acceptance of a proposal providing for a cease-fire" followed by direct talks. Once Israel had accepted unconditionally, no source shows anyone proposing to stay away. The other sources (d389, S/1225, Hansard of 11 May 1949, JTA of 19 January 1949) and the notes name no advocate.
- Expected consequences:

  None found that qualifies. No source records a threat or an expectation tied to Israel declining. A US instruction of 10 January (d371, footnote) only welcomes "present intention of both Egypt and Israel to meet in Rhodes".
- Dropped: none (§A.6 lists this road under "No qualifying lead").
- Not reachable: https://documents.un.org/api/symbol/access?s=S/1187&l=en&t=pdf — HTTP 200 PDF, scanned with unreadable OCR. Not needed: FRUS d362 quotes the same report.

### 28. `second_truce/defy`

- Event: `content/events/second_truce.yaml`, window 1948-07-15 to 1948-07-19, hard anchor; historical option `comply`.
- Basis: **design**. By: none found. When: n/a.
  - The event file says "the notes name no advocate for defying the resolution", and notes section 5 marks the internal debate as a gap. Resolution 54 itself (FRUS doc 468) records Israel's "acceptance in principle of a prolongation of the truce". Ben-Gurion's diary of 11 July (bga node 9706) plans to use the days before an expected truce, not to defy it.
- Expected consequences:

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| UN Security Council (Resolution 54, para. 3) | 15 Jul 1948 | "a breach of the peace within the meaning of Article 39 of the Charter" (followed by "further action under Chapter VII") | https://documents.un.org/api/symbol/access?s=S/RES/54(1948)&l=en&t=pdf, 2026-10-17 (same text: https://avalon.law.yale.edu/20th_century/scres054.asp, 2026-10-17) | several sources |
| US Department of State, Office of UN Affairs (the Bancroft-Meeker paper McClintock approved, if war resumes) | 1 Jul 1948 | "to move into action under Chapter VII of the Charter" | https://history.state.gov/historicaldocuments/frus1948v05p2/d428, 2026-10-17 | several sources |
| US Department of State, Office of UN Affairs (economic sanctions study) | 1 Jul 1948 | "Israel could be almost fatally injured by the application of economic sanctions" | https://history.state.gov/historicaldocuments/frus1948v05p2/d428, 2026-10-17 | one source |

- Dropped: "a US expectation that sanctions would run against Israel" (FRUS doc 735) — dropped: the memo is dated 5 November 1948 and concerns the resolution "passed last night" (4 November) on the Negev. It says nothing about defying Resolution 54 in July.
- Not reachable: none.

### 29. `sinai_withdrawal/hold_el_arish`

- Event: `content/events/sinai_withdrawal.yaml`, window 1949-01-02 to 1949-01-16, hard anchor; historical option `withdraw`. The option on `main` (since #975) reads "Refuse to pull back the force cutting off the Egyptian army at Rafah".
- Basis: **proposed**. By: the Southern Front command. When: about 7-10 January 1949 (the source places it after the block west of Rafah, set up on the day five British aircraft were shot down, 7 January per the Hansard statement below, and before the evacuation finished on 10 January; it gives no day for the proposal itself, so the range is an inference). As with roads 1, 8, 22 and 25, the record's `alternative.date` needs one day (YYYY-MM-DD) that the owner accepts.
  - Evidence: "אלון שוב ניסה לשכנע את בן-גוריון לדחות את התביעה" (Claude's translation: *Allon again tried to persuade Ben-Gurion to reject the demand*; the demand was that Israel pull all its forces back to the international border before the talks) (Benny Michelson, "מבצע 'חורב'", published by the IDF, PDF p. 7 / printed p. 53, https://www.idf.il/media/emxjtczk/%D7%A7%D7%95%D7%91%D7%A5-%D7%9E%D7%91%D7%A6%D7%A2-%D7%97%D7%95%D7%A8%D7%91.pdf, opened 2026-10-18)
  - Evidence: the force was "כוחות שניתקו בפועל את חיל-המשלוח המצרי" (Claude's translation: *forces that had in fact cut off the Egyptian expeditionary force*), placed "ממערב לרפיח" (Claude's translation: *west of Rafah*) (same page)
  - Note: the owner ruled on 18 October 2026, "Reword + accept source" (#939): the option was reworded to match this proposal (#975) and the IDF-published study is accepted as its record. The study is later Israeli official historiography, not a record made at the time. The same page records an earlier proposal by the same command not to withdraw from Sinai at all (after the Security Council's call of 29 December), which comes before the window and is not this option.
  - Correction (G2, 18 Oct): an earlier version of this note said FRUS d371 records Israeli units re-entering Egypt "on the Chief of Staff's orders". It records the opposite. The Chief of Staff told the US Air Attaché that units "entered road to Egypt in order to cut it as a phase of attack on Rafah", and that since the cease-fire hour all units had been "ordered out of Egypt", "on direct orders ICS" (FRUS 1949 VI d371, https://history.state.gov/historicaldocuments/frus1949v06/d371, opened 2026-10-18).
- Expected consequences (of keeping the Rafah force inside Egypt):

| View-holder | Date | Excerpt (verbatim) | Source, opened | Strength |
|---|---|---|---|---|
| The British Government (its note, as reported by the Israeli Prime Minister to the US Special Representative) | 1 Jan 1949 | "Great Britain threatens to take action against us under 1936 Treaty with Egypt" | https://history.state.gov/historicaldocuments/frus1949v06/d341, 2026-10-18 | one source |
| The British Government (statement issued in London, printed in Hansard) | 8 Jan 1949 | "instructed to regard as hostile any Jewish aircraft encountered over Egyptian territory" | https://api.parliament.uk/historic-hansard/commons/1949/may/11/israel-attacked-british-aircraft, 2026-10-18 | one source |
| The US Acting Secretary of State | 5 Jan 1949 | "incursion by Israeli forces into Egypt threatened a much more grave conflict" | https://history.state.gov/historicaldocuments/frus1949v06/d354, 2026-10-18 | one source |
| The same | 5 Jan 1949 | "we had been willing to make strong representations" | https://history.state.gov/historicaldocuments/frus1949v06/d354, 2026-10-18 | one source |

  Rows 1 and 2 are two separate documents recording British measures aimed at Israeli forces in Egyptian territory, but they record different measures (a threat under the 1936 treaty; orders to treat Israeli aircraft over Egypt as hostile), so neither is "several sources". The Hansard statement says the RAF flights were made "to ascertain the depth and scale of Jewish incursion into Egyptian territory", which is the force this road keeps there. Rows 3 and 4 are one document.
- Dropped: none. All three §A.6 leads still hold for the reworded option: British action under the 1936 treaty (d341), US pressure (d354, "strong representations"), and a wider conflict outside Palestine (d354).
- Dropped (G2, 18 Oct): British troops at Aqaba (d354, the British Ambassador, 5 January; d371, the Israeli Foreign Minister, 9 January), counted in an earlier version as sources for British action if Israeli forces return into Egypt. They do not support that. The Foreign Office communiqué quoted in d354's footnote sends the force to Aqaba at Transjordan's request, "under the terms of the Anglo-Transjordan treaty of March 1948", and the Foreign Minister read the troops as a threat to "Israel territory in Negev" (d371), not to a force in Egypt.
- Not reachable: none.

### 30. `ten_days/southern_front`

- Event: `content/events/ten_days.yaml`, window 1948-07-10 to 1948-07-18, hard anchor; historical option `central_front`. Sensitive: `war-casualties`.
- Basis: **debated**. By: the General Staff (its head of operations, raising it with the Minister of Defence). When: 11 July 1948.
  - Evidence: "to Ramallah-Jerusalem, [or] to Beit Jubrin or southward?" (Ben-Gurion's diary, 11 July 1948, Ben-Gurion Archive English translation, https://bengurionarchive.bgu.ac.il/en/node/9706, opened 2026-10-17). The same entry plans "to send two armored battalions, at least, to the south" alongside Latrun and Jerusalem. That is a partial shift, not a choice of the south as the main effort, so the basis stays at `debated` (no cabinet debate found; the event file and notes section 4 say the same).
- Expected consequences: None found that qualifies. The diary's one expectation ("Presumably a truce will be imposed on us") holds for both options.
- Dropped: none (§A.6 lists this road as "no qualifying lead").
- Not reachable: none.

## The premise table

For each road, the later cards whose `text` that road makes untrue, quoted from the file on `main` at the branch
point. The proposal is a proposal for the owner: no conflict / suppress / replace / variant (plan §A.4.4). "Already
requires" means the card is already gated on the historical option, so a player on this road never sees it.

| Road | Later card (file:line) | Text the road contradicts (verbatim from the file) | Proposal |
|---|---|---|---|
| `altalena/compromise` | (none) | (none). Checked `bernadotte_assassination.yaml:4` ("the Irgun battalions in Jerusalem were given 24 hours to disband into the IDF"): the Jerusalem battalion exists under both options | no conflict |
| `armistice_egypt/hold_out` | (none) | (none) | no conflict |
| `armistice_jordan/hold_out` | (none) | (none) | no conflict |
| `armistice_lebanon/hold_out` | (none) | (none) | no conflict |
| `armistice_syria/hold_out` | `content/events/hula_drainage.yaml:4` | "reach into the demilitarised zone with Syria. Syria has complained, and the chairman of the armistice commission asks that work stop pending agreement." | suppress: the zone and commission exist only under a signed Syrian armistice. |
| `austerity_1949/decline` | `content/events/clothing_rationing_1950.yaml:4` | "{rationing_minister} proposes extending rationing to clothing and footwear and fighting the black market." | variant: with no food rationing, "extending" has nothing to extend |
| `bernadotte_assassination/investigate_only` | (none) | (none) | no conflict |
| `clothing_rationing_1950/relax` | (none) | | no conflict |
| `education_registration_1951/concede` | (none) | | no conflict |
| `first_truce/refuse` | `content/events/jerusalem_military_government.yaml:4` | "Count Bernadotte's proposals would put Jerusalem under UN authority, and he has suggested demilitarising the city." | variant: his proposals and demilitarisation plan were made during the truce |
| `first_truce/refuse` | `content/events/ten_days.yaml:4` | "The first truce has ended and the fighting has resumed" | no conflict (already requires `first_truce/accept`) |
| `first_truce/refuse` | `content/events/latrun_proposal.yaml:4` | "break the truce to attack and take Latrun" | no conflict (already requires `second_truce/comply`) |
| `forex_crisis_1951/cut_imports` | (none) | | no conflict |
| `frumkin_commission/dismiss` | `content/events/education_registration_1951.yaml:4` | "the religious parties oppose a religious stream run by the labour movement." | variant: under dismiss the uniform camp system stays, so no labour religious stream |
| `hula_drainage/halt` | (none) | | no conflict |
| `idf_founding/separate_units` | `content/events/altalena.yaml:4` | "the rest to Irgun battalions inside the IDF" | variant: with units left outside, no Irgun battalions sit inside the IDF |
| `idf_founding/separate_units` | `content/events/bernadotte_assassination.yaml:4` | "the Irgun battalions in Jerusalem were given 24 hours to disband into the IDF" | no conflict: Jerusalem was excepted either way |
| `iraqi_exodus/pace` | `content/events/forex_crisis_1951.yaml:4` | "immigration in the first half of the year far exceeded the plan" | variant: FRUS d513 blames the overshoot mainly on Iraqi and Romanian arrivals |
| `israel_bonds/donations_only` | (none) | | no conflict |
| `jerusalem_ceasefire/hold_out` | (none) | (none) | no conflict |
| `jerusalem_military_government/accept_demilitarisation` | `content/events/bernadotte_assassination.yaml:4` | "the Irgun battalions in Jerusalem were given 24 hours to disband into the IDF" | variant: demilitarisation would have withdrawn armed forces, irregulars included, from Jerusalem |
| `jerusalem_military_government/accept_demilitarisation` | `content/events/jerusalem_ceasefire.yaml:4` | "signed a full cease-fire for the Jerusalem area at Government House on 30 November" | no conflict (already requires `jerusalem_military_government/military_government`) |
| `latrun_proposal/press_offensive` | `content/events/operation_yoav.yaml:4` | "the offensive the cabinet approved on 6 October is under way" | variant: assumes the truce held into October; fighting resumed in September |
| `latrun_proposal/press_offensive` | `content/events/operation_hiram.yaml:4` | "In breach of the truce, the Arab Liberation Army attacked" | variant: Israel would already have broken the truce |
| `law_of_return/quota` | `content/events/iraqi_exodus.yaml:4` | "{immigration_minister} asks whether to bring everyone at once." | variant: under a yearly quota, bringing everyone at once is not open |
| `law_of_return/quota` | `content/events/forex_crisis_1951.yaml:4` | "immigration in the first half of the year far exceeded the plan" | variant: a quota caps arrivals, so exceeding the plan reads differently |
| `maabarot_policy/keep_camps` | `content/events/education_registration_1951.yaml:4` | "schooling for immigrant children in the transit camps" | variant: no transit camps on this road; say "immigrant camps" |
| `maabarot_policy/keep_camps` | `content/events/iraqi_exodus.yaml:4` | "The transit camps are already crowded." | variant: say "immigrant camps"; the premise of crowding still holds |
| `ministry_supply_crisis/keep_ministry` | (none) | | no conflict |
| `operation_hiram/advance_into_lebanon` | `content/events/armistice_lebanon.yaml:4` | "Negotiators have reached an armistice agreement with Lebanon." | no conflict: a deeper advance does not make later talks untrue. |
| `operation_horev/hold` | `content/events/sinai_withdrawal.yaml:4` | "Israeli forces crossed into Sinai and reached the outskirts of El-Arish airfield." | no conflict (already requires `operation_horev/launch`) |
| `operation_horev/hold` | `content/events/rhodes_talks.yaml:4` | "By 6 January Egypt and Israel had both accepted a cease-fire followed by direct talks under UN chairmanship" | no conflict (already requires `operation_horev/launch`) |
| `operation_horev/hold` | `content/events/armistice_egypt.yaml:4` | "Talks in Rhodes have produced an armistice agreement with Egypt." | variant: the Rhodes talks followed the cease-fire that ended Horev. |
| `operation_yoav/push_to_hebron` | `content/events/jerusalem_ceasefire.yaml:4` | "Dayan and Abdullah el-Tell signed a full cease-fire for the Jerusalem area" | no conflict: the push does not make the signing untrue; a Legion war is a design consequence. |
| `reparations_claim/refuse` | (none) | | no conflict |
| `rhodes_talks/decline` | `content/events/armistice_egypt.yaml:4` | "Talks in Rhodes have produced an armistice agreement with Egypt." | variant: with no delegation sent, the Rhodes talks did not happen. |
| `second_truce/defy` | `content/events/operation_hiram.yaml:4` | "In breach of the truce, the Arab Liberation Army attacked" | replace: no second truce exists to breach |
| `second_truce/defy` | `content/events/operation_yoav.yaml:4` | "the offensive the cabinet approved on 6 October is under way" | variant: assumes a three-month lull before the October offensive |
| `second_truce/defy` | `content/events/latrun_proposal.yaml:4` | "break the truce to attack and take Latrun" | no conflict (already requires `second_truce/comply`) |
| `sinai_withdrawal/hold_el_arish` | `content/events/rhodes_talks.yaml:4` | "the cease-fire took effect on 7 January. The talks are to open at Rhodes." | variant: Israel was required to pull all its forces back to the border as a condition of the talks (Michelson, p. 53), so keeping the Rafah force puts the opening of the talks in doubt. |
| `sinai_withdrawal/hold_el_arish` | `content/events/armistice_egypt.yaml:4` | "Talks in Rhodes have produced an armistice agreement with Egypt." | variant: with the Rafah force kept in Egypt, the Rhodes talks are in doubt (see the row above). |
| `ten_days/southern_front` | (none) | (none). Checked `operation_yoav.yaml:4` ("passed the Egyptian positions"): the road does not make it untrue | no conflict |

## Sources the AI pass could not read

Each road's own "Not reachable" line gives the details. In summary:

- **Knesset and Provisional State Council minutes** (`fs.knesset.gov.il`): every PDF tried came back with status 200
  but as an HTML geo-block page ("The site is temporarily inaccessible from your location"). Files: Provisional State
  Council `0_ptm_` 249973, 249974, 249980, 249987; First Knesset `1_ptm_` 250034, 250056, 250102, 250126, 250129,
  250140, 250144, 250158, 250159, 250241, 250242, 250243, 250245, 250246, 250247, 250264, 250266; `1_lsr_209667`; and
  `m.knesset.gov.il` / `main.knesset.gov.il` pages. Other routes tried: Wayback copies of 250102, 250241 and 250242
  (404, never archived) and the Ben-Yehuda republication of the Altalena debate (it opened and is used for roads 1
  and 14). **This is the biggest gap.** Several roads are `design` or have no consequence partly because their sitting
  could not be read.
  **Update, 18 October 2026:** this is no longer true from the network the G2 check ran on. A plain GET with a generic
  User-Agent returned HTTP 200 `application/pdf` for `fs.knesset.gov.il/0/Plenum/0_ptm_249973.pdf` and
  `fs.knesset.gov.il/1/Plenum/1_ptm_250241.pdf` (real minutes, not the block page), and for
  `m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf`. The sittings have not been read yet, so the gap
  stays open and no road above has changed because of it.
- **Scanned UN documents** S/1071 and S/1187: the PDFs opened, but they are images and could not be read here. FRUS
  1949 VI doc 362 quotes the part of S/1187 that was needed. Nothing replaced S/1071.
- **Two Ben-Gurion University PDFs** (`in.bgu.ac.il/.../ליאור-לביד.pdf`, `in.bgu.ac.il/bgi/iyunim/40/Meir-Glitzenstein.pdf`):
  a bot-check page came back instead of the PDF.

## What a person must check

Before anyone writes "checked by" at the top (plan Task 3, Step 3):

1. **Every excerpt against its page.** Open each URL and confirm that the quoted words are there, are not taken out of
   context, and support the row they sit in (view-holder, date, what they expected). The script confirmed only that
   the words appear on the saved page, not that they mean what the row says.
2. **Every Hebrew excerpt and its translation** (roads 1, 12, 14, 21, 25, 29): a Hebrew reader confirms both.
3. **The bases at the edge.** Road 23 (Hiram: `proposed` rests only on Kurzman 1970, p. 684, through Wikipedia; read
   the book or a diary entry). Road 25 (Yoav: `debated` on the IDF study; `proposed` waits on Peri 1983,
   pp. 58-59, or a diary entry; also the day of the war diary entry, vol. III, p. 819, the study's footnote 171, which
   the consequence row needs). Roads 12, 13 and 22 (`debated` or `design`?). Road 30 (one diary entry). Road 8 (only a modified ration is on record). Road 1 rows 1 and 3 (do they attach to `compromise`, or to the arms import under both options?). Road 12 (the date of the cabinet's committee decision: 17, 19 or about 24 January 1950). Road 1 (the request came
   from the Irgun, the deciding side only answered it). Road 11 (`design`: a measure taken alongside the aid request;
   confirm no body proposed it instead of the aid request). Road 9 (a cabinet dispute, not the religious parties' own demand).
4. **The Knesset sittings that could not be opened** (list above; the PDFs are reachable again as of 18 October 2026),
   or the printed Divrei HaKnesset. Above all, the sittings behind roads 6, 8, 9, 12, 20, 22 and 26: an advocate or a recorded warning there
   could change a basis or add a consequence.
5. **The dropped leads** (one "Dropped" line per road): confirm that each was right to drop. Check above all the
   FRUS 1948 V-2 doc 735 date for road 28 and the "fear of British intervention" for road 25 (Peri 1983).
6. **The "several sources" rows**: confirm that the two documents are independent and say the same thing.
7. **The premise table**: confirm each quoted card line against `main` at the time of the check (card text may have
   changed since this branch point), and look for conflicts the pass missed, above all for the war roads, where one
   change (no second truce, Latrun taken in September) reaches many later cards.
8. **Policy fit**: no row may hand a card a fact that rests only on Wikipedia or on Claude's translation.
