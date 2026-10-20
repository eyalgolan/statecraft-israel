# Electing the Constituent Assembly - historian pilot draft

**AI-PREPARED DRAFT. No historian verdict, consent or sign-off. Refresh after pending #1114/#1103/#1104 settles; do not present this snapshot as their future merged text.**

Snapshot: `2d3e13c098e147493ab034ab2ff20f0b04765fab`; prepared 20 October 2026. Event: `elections_1949`. [Exact YAML](https://github.com/eyalgolan/statecraft-israel/blob/2d3e13c098e147493ab034ab2ff20f0b04765fab/content/events/elections_1949.yaml). SHA-256: `75d5f16ed26d7562d5c13fb0795bf3b236d06c48607ed10554b7c459860a662e`.

Existing status: `fact-checked` (copied metadata, not a new check). Tags: none recorded in this YAML. No reviewed_by/reviewed_on keys are added or changed. Existing sensitivity/release rules remain independent.

## Dates and historical-reference rendering

Shown from **15 January 1949**; unavailable from **26 January 1949** (exclusive end). This is the game window, not proof every narrated act occurred on its first day. The responsible office is `prime_minister`; pinned historical-reference holder: **David Ben-Gurion** at the start and through the window, using people.yaml terms and the [existing Offices.Holder](https://github.com/eyalgolan/statecraft-israel/blob/2d3e13c098e147493ab034ab2ff20f0b04765fab/sim/Statecraft.Sim/Offices.cs). This is not a fresh primary-source check of that person's terms. Actual `Cabinet.Fill` prefers player appointments; this static packet shows the historical fallback, not every possible save.

Original placeholders: none in body. No new Hebrew translation is supplied. English factual review and native-Hebrew review remain separate; pending translation PRs are not imported.

## Player-facing text at this snapshot

**Electing the Constituent Assembly**

The Provisional State Council has set the Constituent Assembly at 120 seats, elected nationwide by proportional representation, and a national registration taken under curfew on 8 November built the voters' roll. Postponed because of the war, the vote is set for 25 January.

### Options

- **Hold the election on 25 January** (`hold`): historical flag=true, recommended flag=true. These are current metadata, not the reviewer's verdict.

## Design ledger - excluded from historical certification

All amounts below are authored game values from this event and model.yaml, not measured historical outcomes. No mechanics, state, score or balance value changes. Indicators run 0..1.

| Option | Existing effects |
| --- | --- |
| Hold the election on 25 January | Social cohesion +3 percentage points (raw 0.03 on 0..1); International support +3 percentage points (raw 0.03 on 0..1) |

## Requirements and dispatch

`anchor: hard`; no `requires` field in this snapshot. The date window, normal event dispatch and office resolution still apply; absence of an explicit requires block is not a statement that every arbitrary save displays the card.

## Source inventory and agent-access notes

Existing checked dates are copied verbatim and **not restamped**. Age is calculated as of 20 October 2026. The new access/read notes below are by the agent on 20 October 2026, not a person's historical verification. Successful retrieval or extracted OCR alone is not a support verdict. PDF page numbers are 1-based; printed-page locators are stated separately. Original PDFs/HTML are not redistributed with this packet.

| ID | Existing source | Existing checked; age | Agent access/read scope; remaining locator work |
| --- | --- | --- | --- |
| S01 | [source](https://dacenter.tau.ac.il/sites/abraham.tau.ac.il/files/media_server/daniel%20abraham/kesher/61/08_Kesher61_Harpaz.pdf) | 2026-10-14; 6 days | Agent read rendered PDF pp.2-3 (printed 187-188): electoral arrangements, registration/curfew and postponement. Article text is secondary scholarship, not the cited council record itself. |
| S02 | [source](https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf) | 2026-10-14; 6 days | Agent read table 2.1 first row and footnote 2: population-registration date. This population table alone does not establish the electoral-roll mechanism or curfew. |
| S03 | [source](https://en.idi.org.il/israeli-elections-and-parties/elections/1949/) | 2026-10-14; 6 days | Agent read election summary/table:25.1.1949, eligible 506,567, valid 434,684, turnout 87.0%. Existing disagreement with HE/CBS retained. |
| S04 | [source](https://www.idi.org.il/policy/parties-and-elections/elections/1949/) | 2026-10-14; 6 days | Agent read election summary/table:25.1.1949, eligible 506,567, valid 434,684, turnout 86.9%. |
| S05 | [source](https://www.gov.il/BlobFolder/guide/election-committee-history/en/history_election-dates_knesset1-results.pdf) | 2026-10-14; 6 days | Python TLS issuer verification failed; curl with full TLS verification succeeded, PDF magic verified. Agent viewed cover: Official Gazette 49,7 February 1949. Results notice pages/tallies still need page-level human checking. |
| S06 | [source](https://fs.knesset.gov.il/0/Plenum/0_ptm_249993.pdf) | 2026-10-14; 6 days | Agent viewed cover dated 4.11.1948 and PDF/printed p. 20:120-seat motion 16 for/14 against. The date disagreement with OData remains recorded, not silently harmonized. |
| S07 | [source](https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PlenumSession(489501)) | 2026-10-14; 6 days | Agent read retrieved Atom/XML record 489501: sitting date 1948-11-06. This differs from the scanned minutes. Treat metadata as one conflicting record. |
| S08 | [source](https://fs.knesset.gov.il/0/Plenum/0_ptm_249995.pdf) | 2026-10-14; 6 days | Agent viewed cover 18.11.1948 and PDF/printed pp. 22-23, ordinance sections 2/3/9:one counting district; general/direct/equal/secret/proportional; 120 members. Final adoption passage beyond this excerpt not independently read here. |
| S09 | [source](https://www.cbs.gov.il/he/publications/DocLib/2023/ShnatonElectionsTheKnessetandGovernment.27/st27_02.pdf) | 2026-10-14; 6 days | Agent viewed table 27.2, PDF p. 1, row Knesset 1:25.1.1949; 506,567 eligible; 440,095 voters; 86.9%; 434,684 valid. |

## Claims for the human reviewer

This is a draft claim inventory, including assertions in YAML comments. Rows may be split or extended by the reviewer. No source-support verdict or historian acceptance is prefilled. A note saying an agent saw a passage is not the human verdict.

| ID | Field | Existing assertion to check | Source/locator to inspect | Limits/questions | Human verdict |
| --- | --- | --- | --- | --- | --- |
| E01 | title/text/option | The body is the Constituent Assembly, with an election on 25 January 1949. | S09 table 27.2; S03/S04 election summaries. | Use the calendar day, not the 15 January game-window start. |  |
| E02 | text; comments | The Provisional State Council fixed 120 seats; its 25 th sitting voted 16-14. | S06 cover andp.20; S08 sections 2/3/9. | Council-date discrepancy is retained below. |  |
| E03 | text; comments | The system is nationwide proportional representation. | S08 p.22 sections 2/3; S01 printed 187. | One counting district and the ballot method are separate propositions. |  |
| E04 | text; comments | Registration on 8 November 1948 occurred under curfew and built the voters roll. | S01 printed 188; S02 first row+footnote 2. | Population-registration date alone does not prove the whole electoral claim. |  |
| E05 | text; comments | War postponed the election; 25 January was fixed by the time the card opens. | S01 printed 188. | Check both postponement decisions and their dates, not a general inference from the war. |  |
| E06 | comments | Declaration deadline 1 October 1948; two postponements. | S01 printed 187-188; cited Declaration not independently opened in this packet. | Exact decision dates were already flagged as a source gap. |  |
| E07 | comments | Minutes dated 4 November 1948 versus OData 6 November 1948. | S06 cover; S07 record 489501. | Preserve the existing chosen 4 November and both records; no new ruling inferred. |  |
| E08 | comments | Ordinance final text adopted 18 November 1948. | S08 cover/ordinancepp.22-23; final adoption passage needs locating. | Reading the printed ordinance is not automatically reading the adoption vote. |  |
| E09 | comments | Turnout 86.9%; 506,567 eligible; 440,095 voters; 434,684 valid; 5,411 invalid. | S09 row 1; S05 results notice not yet page-checked. | S03 displays 87.0%; do not average the discrepancy or infer the Gazette tally from arithmetic alone. |  |
| E10 | comments | The 25 January date was fixed 25 November 1948. | S01 printed 188. | Current YAML comment points to 187; packet flags a locator discrepancy, without editing content. |  |
| E11 | office | Historical reference office holder throughout this window. | Pinned people.yaml term and Offices.Holder behavior. | Source terms are an input to inspect, not fresh primary-source certification. |  |

## Known gaps and contradictions

- Current card is a hard anchor with one historical choice. This packet cannot certify that no other historical alternative existed.
- The existing 4 November scanned-minutes/6 NovemberOData mismatch remains explicit; do not reopen its chosen value without new evidence.
- Source-note locator 187 for the 25 November decision appears to be 188 in the opened article. Reviewer should confirm before a focused correction.
- The original Declaration and Gazette results tally pages are not certified here; final adoption vote passage also remains to be located.
- Do not treat valid-vote count as turnout denominator. Retain the existing EN 87.0% versus HE/CBS 86.9% discrepancy.

## Existing YAML comments - unchanged assertions under review

These are copied from the pinned file for checking, not newly endorsed claims or fresh source dates.

```yaml
# yaml-language-server: $schema=../schema/event.schema.json
# The Declaration of Independence required elected institutions by 1 October 1948 (quoted by Harpaz, p. 187); the vote
# was postponed twice because of the war (the dates of those decisions are a gap). The Provisional State Council voted
# 16 to 14 to set the assembly at 120 seats at its 25th sitting on 4 November 1948 (the minutes' heading: 4.11.1948 and
# 2 Heshvan 5709, which agree; [C] the Knesset's OData has 6 November, a Saturday, the only Saturday among its
# council sittings from September 1948 to February 1949) and adopted the election ordinance's final text at its 27th
# sitting on 18 November 1948, with 120 seats, general, direct, equal, secret and proportional elections, and the whole
# area counted as one district (the minutes of both sittings; Harpaz, Kesher 61, p. 187, who also quotes the election
# committee's chairman on a nationwide proportional system). Registration date
# [C]: 8 November 1948 (CBS, population.yaml; Harpaz, p. 188) vs "8 or 11" (en.wiki "1948 in Israel"); it ran under a
# curfew from 5 pm to midnight, seven hours (Harpaz, p. 188). The vote: 25 January 1949, turnout 86.9% (CBS, from the
# Central Elections Committee: 440,095 voters of 506,567, which is 86.88%; the Official Gazette's notice of the results
# gives 5,411 invalid votes, which with CBS's 434,684 valid votes makes the same 440,095; the IDI's Hebrew page also
# gives 86.9%; [C] its English page 87.0%, a rounding the figures do not support). 25 January was fixed on
# 25 November 1948 (Harpaz, p. 187); the card comes on 15 January, by when the date was set. Research notes sections
# 10 and 11.
# One option (review I3): legislatures.yaml fixes the election and the First Knesset's sitting, so a "postpone" option
# could not change anything. The window opens on 15 January, the monthly turn date before the vote.
  # Harpaz, Kesher 61: p. 187, the Declaration's 1 October 1948 deadline, the 120 seats and the choice of a nationwide
  # proportional system; p. 188, the registration of 8 November 1948 under a curfew from 5 pm to midnight.
  # IDI results page: 506,567 eligible voters, 434,684 valid votes, turnout 87.0% (the Hebrew page below gives 86.9%).
  # Official Gazette (Iton Rishmi) No. 49, 7 February 1949, special issue, the Central Elections Committee's copy: the
  # notice of the results of the elections to the Constituent Assembly, with the valid votes and seats of each list,
  # the members elected and 5,411 votes found invalid.
  # Provisional State Council, 25th sitting (minutes dated 4 November 1948): Warhaftig proposed 120 as a compromise
  # between 101 and 171, after the Great Assembly; the council voted 16 to 14 to set the Constituent Assembly at 120.
  # The minutes are headed 4.11.1948, 2 Heshvan 5709.
  # The Knesset's OData record of the council's 25th sitting: start date 6 November 1948. The minutes above give
  # 4 November, and the 24th and 26th sittings are on 28 October and 11 November in the same table.
  # Provisional State Council, 27th sitting, 18 November 1948: the end of the debate on the election ordinance and the
  # vote adopting it as a whole. The text read in: elections general, direct, equal, secret and proportional (section
  # 3); the whole area one district for counting the votes (section 2); 120 members (section 9).
  # CBS Statistical Abstract 2023, table 27.2 (source: the Central Elections Committee): 25 January 1949, 506,567
  # eligible voters, 440,095 voters, turnout 86.9%, 434,684 valid votes.
```

## Human review and refresh checklist

- [ ] Owner confirms the actual volunteer's qualifications, independence and scope; no candidate is assumed.
- [ ] Reviewer supplies their own claim-by-claim verdicts and precise passages; keep unresolved dissent.
- [ ] Reviewer expressly consents to any public name/handle and to CC BY 4.0 for their future review text before it is committed. No consent is inferred from this draft.
- [ ] Recheck at a new fixed commit after the pending edits settle; compare all YAML fields/comments, sources, placeholders and supporting terms. Earlier acceptance never silently covers changed text.
- [ ] Native Hebrew review is separately recorded when applicable; no flag is removed by an agent.
- [ ] Follow [HISTORIAN_REVIEW.md](../../../HISTORIAN_REVIEW.md): actual review in a PR comment; only the reviewer writes any future YAML sign-off, in their own commit. No status or release-gate transition is implemented by these docs.

Packet/reproduced content: CC BY-SA 4.0, credit Statecraft: Israel contributors; [licence](../../../../LICENSES/CC-BY-SA-4.0.txt). This does not license or include a future reviewer's text before their separate consent.
