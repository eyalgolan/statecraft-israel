# Transit Camps - historian pilot draft

**AI-PREPARED DRAFT. No historian verdict, consent or sign-off. Refresh after pending #1114/#1103/#1104 settles; do not present this snapshot as their future merged text.**

Snapshot: `2d3e13c098e147493ab034ab2ff20f0b04765fab`; prepared 20 October 2026. Event: `maabarot_policy`. [Exact YAML](https://github.com/eyalgolan/statecraft-israel/blob/2d3e13c098e147493ab034ab2ff20f0b04765fab/content/events/maabarot_policy.yaml). SHA-256: `6ef7e84f434f2cc6fbe00e1f90081133625575bd813af1e7f3db6ba5b8f12b93`.

Existing status: `fact-checked` (copied metadata, not a new check). Tags: none recorded in this YAML. No reviewed_by/reviewed_on keys are added or changed. Existing sensitivity/release rules remain independent.

## Dates and historical-reference rendering

Shown from **1 April 1950**; unavailable from **1 June 1950** (exclusive end). This is the game window, not proof every narrated act occurred on its first day. The responsible office is `prime_minister`; pinned historical-reference holder: **David Ben-Gurion** at the start and through the window, using people.yaml terms and the [existing Offices.Holder](https://github.com/eyalgolan/statecraft-israel/blob/2d3e13c098e147493ab034ab2ff20f0b04765fab/sim/Statecraft.Sim/Offices.cs). This is not a fresh primary-source check of that person's terms. Actual `Cabinet.Fill` prefers player appointments; this static packet shows the historical fallback, not every possible save.

Original placeholders: `{prime_minister}`. No new Hebrew translation is supplied. English factual review and native-Hebrew review remain separate; pending translation PRs are not imported.

## Player-facing text at this snapshot

**Transit Camps**

The immigrant camps, which house and feed new arrivals free of charge, are overflowing. Levi Eshkol of the Jewish Agency Settlement Department proposes transit camps near towns and work, where residents earn their own living. David Ben-Gurion must decide.

### Options

- **Build transit camps where residents work for their living** (`maabarot`): historical flag=true, recommended flag=true. These are current metadata, not the reviewer's verdict.
- **Keep the free-board immigrant camps** (`keep_camps`): historical flag=false, recommended flag=false. These are current metadata, not the reviewer's verdict.

## Design ledger - excluded from historical certification

All amounts below are authored game values from this event and model.yaml, not measured historical outcomes. No mechanics, state, score or balance value changes. Indicators run 0..1.

| Option | Existing effects |
| --- | --- |
| Build transit camps where residents work for their living | Housing +4 percentage points (raw 0.04 on 0..1); Social cohesion -2 percentage points (raw -0.02 on 0..1); Money IL +2.0m (raw 2 million); Set Transit camps (ma'abarot) to 1.0 (design policy level) |
| Keep the free-board immigrant camps | Housing -3 percentage points (raw -0.03 on 0..1); Money IL -4.0m (raw -4 million) |

## Requirements and dispatch

`anchor: hard`; no `requires` field in this snapshot. The date window, normal event dispatch and office resolution still apply; absence of an explicit requires block is not a statement that every arbitrary save displays the card.

## Source inventory and agent-access notes

Existing checked dates are copied verbatim and **not restamped**. Age is calculated as of 20 October 2026. The new access/read notes below are by the agent on 20 October 2026, not a person's historical verification. Successful retrieval or extracted OCR alone is not a support verdict. PDF page numbers are 1-based; printed-page locators are stated separately. Original PDFs/HTML are not redistributed with this packet.

| ID | Existing source | Existing checked; age | Agent access/read scope; remaining locator work |
| --- | --- | --- | --- |
| S16 | [source](https://en.wikipedia.org/wiki/Ma'abarot) | 2026-10-10; 10 days | Agent read proposal paragraph: English Wikipedia locates Eshkol proposal in March 1950. This existing discrepancy with CET relative chronology remains explicit. |
| S17 | [source](https://he.wikipedia.org/wiki/מעברה) | 2026-10-10; 10 days | Agent read opening/history passage: first use 21 April 1950 letter and first campMay 1950, with CZA reference. Original letter S15/4175 not opened; the article is not that primary document. |
| S18 | [source](https://fs.knesset.gov.il/1/Plenum/1_ptm_250140.pdf) | 2026-10-14; 6 days | PDF retrieved and text extracted; no claim of full passage/source-support review. Reviewer must locate which propositions this sitting supports. |
| S19 | [source](https://web.archive.org/web/20260419055518/http://www.zionistarchives.org.il/Pages/Maabara.aspx) | 2026-10-14; 6 days | Agent read archived CZA article body: Eshkol/Settlement Department, camps near established communities, work and reduced Agency dependence; first camp in Jerusalem hills 1950. It does not supply every exact day in the YAML notes. |
| S20 | [source](https://www.lib.cet.ac.il/pages/item.asp?item=12939) | 2026-10-14; 6 days | Agent read CET sections on the 27 March 1950 meeting, proposal two weeks later and camps near work, plus bibliography. Secondary text reportsDavar 23 May 1950; original cited archival/newspaper documents not opened. Bibliography says 1986, while current YAML comment says 1987. Do not resolve that discrepancy by assumption. |

## Claims for the human reviewer

This is a draft claim inventory, including assertions in YAML comments. Rows may be split or extended by the reviewer. No source-support verdict or historian acceptance is prefilled. A note saying an agent saw a passage is not the human verdict.

| ID | Field | Existing assertion to check | Source/locator to inspect | Limits/questions | Human verdict |
| --- | --- | --- | --- | --- | --- |
| M01 | text | Immigrant camps house/feed arrivals free of charge and are overflowing. | S20 opening/background and 27 March account. | Check which clauses are direct support and which compress conditions; no exact crowding measure claimed. |  |
| M02 | text | Levi Eshkol belongs to the Jewish Agency Settlement Department and proposes transit camps. | S19 archived CZA body; S20 proposal section. | Office/agency role is a factual claim, not a game rating. |  |
| M03 | text; maabarot option | Camps near towns/work permit residents to earn their living rather than depend on Agency support. | S19 body; S20 proposal/work sections. | Intended self-support must not imply that work was consistently available; sources describe hardship. |  |
| M04 | comments | Executive meeting 27 March 1950, proposal about two weeks later. | S20 section on the temporary solution, notes 4/5. | English WikipediaS 16 instead saysMarch; underlying archival record is not opened here. |  |
| M05 | comments | First occurrence of maabarot in 21 April 1950 letter, CZA S15/4175. | S17 article referencing the letter. | Original letter not retrieved/read; citation remains secondary support. |  |
| M06 | comments | First campKesalon in May 1950; Davar report 23 May 1950. | S20 camp-establishment section reportingDavar; S19 only year/region. | Original newspaper item not opened; secondary quotation is not fresh primary verification. |  |
| M07 | window | Window opens 1 April 1950 and ends 1 June 1950 (exclusive). | Pinned YAML. | Monthly game boundary is not an exact proposal date. |  |
| M08 | text; office | PrimeMinister is the decision office; historical-reference holder resolves within window. | Pinned people.yaml and Offices.Holder. | Player appointments can change actual runtime text. |  |
| M09 | keep_camps option | Keeping free-board camps is a playable alternative. | Current YAML option; pending PR 1114 metadata. | Not marked historical; do not invent a named real advocate/date. |  |
| M10 | sources comment | CET bibliography publication year versus YAML source note. | S20 Bibliography field 1986; YAML comment 1987. | Unresolved bibliographic mismatch; no source/comment correction included. |  |
| M11 | effects/set_policy | Housing/cohesion/money changes and maabarot policy 1.0. | Pinned YAML/model. | Design values, not historical quantities. |  |

## Known gaps and contradictions

- CET bibliography says 1986, while the current source comment says 1987. This is a bibliographic question, not permission to edit a content record.
- English WikipediaMarch versus CET 27 March meeting+two weeks later chronology stays explicit. Do not manufacture an exact proposal day.
- Original 21 April letter and 23 MayDavar item were not opened. The packet distinguishes secondary citations from original archival verification.
- Intention to work/self-support is not a claim that camps provided adequate jobs or conditions. The CZA/CET articles describe hardship; keep that context visible.
- The exact relevance of the cited Knesset 250140 sitting needs passage-level checking; a downloaded PDF is not automatically support.

## Existing YAML comments - unchanged assertions under review

These are copied from the pinned file for checking, not newly endorsed claims or fresh source dates.

```yaml
# yaml-language-server: $schema=../schema/event.schema.json
# Eshkol raised the camps' crisis at the Jewish Agency executive on 27 March 1950 and proposed transit housing two weeks
# later, in April (Kachansky, below), so the card opens on 1 April 1950. [C] en.wiki has him propose it in March 1950.
# The word ma'abarot first appears in his letter of 21 April 1950 (he.wiki, citing CZA S15/4175). The first transit camp, Kesalon, opened in May 1950 (Kachansky,
# quoting Davar of 23 May 1950).
  # Central Zionist Archives (archived copy; the live page is http only): Eshkol, head of the Jewish Agency Settlement
  # Department, devised the transit camps, near existing towns so that immigrants could work and not depend on the
  # Agency; the first was set up in the Jerusalem hills in 1950.
  # Miriam Kachansky, "The Ma'abarot", from Olim and Ma'abarot 1948-1952 (Yad Ben-Zvi, 1987), on CET: 100,000 people in
  # the camps living on the Agency; Eshkol's proposal after the executive meeting of 27 March 1950; Davar of 23 May 1950
  # reports the first ma'abara on the slopes of Kesalon.
```

## Human review and refresh checklist

- [ ] Owner confirms the actual volunteer's qualifications, independence and scope; no candidate is assumed.
- [ ] Reviewer supplies their own claim-by-claim verdicts and precise passages; keep unresolved dissent.
- [ ] Reviewer expressly consents to any public name/handle and to CC BY 4.0 for their future review text before it is committed. No consent is inferred from this draft.
- [ ] Recheck at a new fixed commit after the pending edits settle; compare all YAML fields/comments, sources, placeholders and supporting terms. Earlier acceptance never silently covers changed text.
- [ ] Native Hebrew review is separately recorded when applicable; no flag is removed by an agent.
- [ ] Follow [HISTORIAN_REVIEW.md](../../../HISTORIAN_REVIEW.md): actual review in a PR comment; only the reviewer writes any future YAML sign-off, in their own commit. No status or release-gate transition is implemented by these docs.

Packet/reproduced content: CC BY-SA 4.0, credit Statecraft: Israel contributors; [licence](../../../../LICENSES/CC-BY-SA-4.0.txt). This does not license or include a future reviewer's text before their separate consent.
