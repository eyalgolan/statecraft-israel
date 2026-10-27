# Rationing Clothes and Shoes - historian pilot draft

**AI-PREPARED DRAFT. No historian verdict, consent or sign-off. Re-pinned after merged #1091/#1114/#1103/#1104. Owner licensing review, qualified historian review and native Hebrew review remain pending.**

Snapshot: `c8f4bc027e32db3147fc64a919e538f3aea5e3f6`; prepared 20 October 2026. Event: `clothing_rationing_1950`. [Exact YAML](https://github.com/eyalgolan/statecraft-israel/blob/c8f4bc027e32db3147fc64a919e538f3aea5e3f6/content/events/clothing_rationing_1950.yaml). SHA-256: `2e33baac62ea7d12e36b9a5fd5237a128139671b8729c4841c967afdfa6dba60`.

Existing status: `fact-checked` (copied metadata, not a new check). Tags: none recorded in this YAML. No reviewed_by/reviewed_on keys are added or changed. Existing sensitivity/release rules remain independent.

## Dates and historical-reference rendering

Shown from **1 July 1950**; unavailable from **1 September 1950** (exclusive end). This is the game window, not proof every narrated act occurred on its first day. The responsible office is `rationing_minister`; pinned historical-reference holder: **Dov Yosef** at the start and through the window, using people.yaml terms and the [existing Offices.Holder](https://github.com/eyalgolan/statecraft-israel/blob/c8f4bc027e32db3147fc64a919e538f3aea5e3f6/sim/Statecraft.Sim/Offices.cs). This is not a fresh primary-source check of that person's terms. Actual `Cabinet.Fill` prefers player appointments; this static packet shows the historical fallback, not every possible save.

Original placeholders: `{rationing_minister}`. The merged Hebrew translation is copied below with `text_he_review: agent-checked`; its placeholder names also retain their separate agent-checked flags in people.yaml. English factual review and native-Hebrew review remain separate. No translation is newly certified here.

## Player-facing text at this snapshot

**Rationing Clothes and Shoes**

Prices on the black market are climbing. Dov Yosef proposes extending rationing to clothing and footwear and fighting the black market.

### Options

- **Ration clothing and footwear** (`extend`): historical flag=true, recommended flag=true. These are current metadata, not the reviewer's verdict.
- **Ease rationing and let imports fill the shops** (`relax`): historical flag=false, recommended flag=false. These are current metadata, not the reviewer's verdict.

### Hebrew draft at this snapshot

**קיצוב הלבוש וההנעלה**

המחירים בשוק השחור עולים. הצעת דב יוסף היא להרחיב את הקיצוב ללבוש ולהנעלה ולהיאבק בשוק השחור.

- **לקצב לבוש והנעלה** (`extend`).
- **להקל את הקיצוב ולאפשר ליבוא למלא את החנויות** (`relax`).

Copied state: `text_he_review: agent-checked`. The English and Hebrew text/options above are source inventories, not a historian or native-speaker verdict.

### Alternative metadata at this snapshot

`relax` (copied metadata, not a new source-support verdict):

```yaml
basis: debated
by: the Knesset
date: 1950-08-08
sources:
- url: https://www.jta.org/archive/strike-of-clothing-and-shoe-storekeepers-in-israel-ends-modified-rationing-seen
  checked: 2026-10-17
```

The nested source is the same URL as S15 with its own unchanged `checked: 2026-10-17` (3 days old at preparation). The agent reopened and read that HTML on 20 October 2026: it reports a modified clothing ration and a confidence vote, not the exact imports option. The 8 August date and `debated` basis are copied from the accepted YAML and [road 8](https://github.com/eyalgolan/statecraft-israel/blob/c8f4bc027e32db3147fc64a919e538f3aea5e3f6/research_notes/Alternate%20timelines/roads-checked.md), which records the owner ruling. This is not a new certification of the exact historical proposal or dateline.

## Design ledger - excluded from historical certification

All amounts below are authored game values from this event and model.yaml, not measured historical outcomes. No mechanics, state, score or balance value changes. Indicators run 0..1.

| Option | Existing effects |
| --- | --- |
| Ration clothing and footwear | Social cohesion -5 percentage points (raw -0.05 on 0..1) |
| Ease rationing and let imports fill the shops | Social cohesion +3 percentage points (raw 0.03 on 0..1); Foreign currency $-8.0m (raw -8 million) |

## Requirements and dispatch

`anchor: hard`; no `requires` field in this snapshot. The date window, normal event dispatch and office resolution still apply; absence of an explicit requires block is not a statement that every arbitrary save displays the card.

## Source inventory and agent-access notes

Existing checked dates are copied verbatim and **not restamped**. Age is calculated as of 20 October 2026. The access/read notes below were recorded during the earlier agent preparation on 20 October 2026; they are preserved evidence from that preparation, not new reads or a person's historical verification. Successful retrieval or extracted OCR alone is not a support verdict. PDF page numbers are 1-based; printed-page locators are stated separately. Original PDFs/HTML are not redistributed with this packet.

| ID | Existing source | Existing checked; age | Agent access/read scope; remaining locator work |
| --- | --- | --- | --- |
| S10 | [source](https://en.wikipedia.org/wiki/Austerity_in_Israel) | 2026-10-10; 10 days | Agent read the black-market passage in the existing Wikipedia article. That describes a black market but does not by itself measure prices climbing in the precise event window. |
| S11 | [source](https://fs.knesset.gov.il/1/Plenum/1_ptm_250056.pdf) | 2026-10-14; 6 days | Agent viewed PDF pp. 1/8 (printed 2418/2425), 2 August 1950: the rationing announcement section starts on 2425. A speech heading is not proof of the exact prior announcement day. |
| S12 | [source](https://fs.knesset.gov.il/1/Plenum/1_ptm_250241.pdf) | 2026-10-14; 6 days | PDF retrieved and text extracted; opening page identifies 7 August 1950 discussion. Detailed contribution-by-contribution verification remains for the reviewer. |
| S13 | [source](https://fs.knesset.gov.il/1/Plenum/1_ptm_250242.pdf) | 2026-10-14; 6 days | Agent viewed PDF p. 15 (printed 2505), 8 August 1950: confidence framing and coalition motion 57 for/36 against. The tally is not inferred from scrambled OCR digits. |
| S14 | [source](https://www.jta.org/archive/tel-aviv-merchants-strike-against-rationing-of-clothing-and-shoe-keep-stores-closed) | 2026-10-14; 6 days | Agent read HTML article dated 2 August 1950: stores closed today after announcement yesterday. Original bulletin link identified; HTTPS curl timed out (exit 28). Dateline/publishing-day distinction unresolved; do not infer exact 31 July/1 August dates from the web header alone. |
| S15 | [source](https://www.jta.org/archive/strike-of-clothing-and-shoe-storekeepers-in-israel-ends-modified-rationing-seen) | 2026-10-14; 6 days | Agent read HTML article dated 14 August 1950: two-week strike expected to end tomorrow, confidence vote 57-36. Original bulletin HTTPS curl timed out (exit 28). Compare original dateline before treating this as proof of 13/14 August in the YAML note. |

## Claims for the human reviewer

This is a draft claim inventory, including assertions in YAML comments. Rows may be split or extended by the reviewer. No source-support verdict or historian acceptance is prefilled. A note saying an agent saw a passage is not the human verdict.

| ID | Field | Existing assertion to check | Source/locator to inspect | Limits/questions | Human verdict |
| --- | --- | --- | --- | --- | --- |
| C01 | title/text/extend | Rationing was extended to clothes and footwear. | S11 announcement section printed 2425 onward; S14 contemporary report. | Verify exact scope; title alone is not evidence. |  |
| C02 | text | Black-market prices are climbing during this window. | S10 black-market passage; S11/S12 discussions need precise price passage. | Black-market existence does not establish a measured price trend; exact support remains open. |  |
| C03 | text; office | The responsible minister proposes extending rationing and fighting the black market. | Pinned rationing office term; S11 announcement section; S14 report. | Historical-reference Dov Yosef substitution is not a claim that every player still appointed him. |  |
| C04 | comments | Extension 31 July 1950; retailers strike 1-14 August. | S14/S15 HTML; original bulletins linked but HTTPS attempts timed out. | Archive headers say 2/14 August, while text says today/tomorrow; dateline must be checked before certifying exact days. |  |
| C05 | comments | 8 August 1950 coalition motion, made a confidence issue, passed 57-36. | S13 PDF p. 15/printed 2505. | Agent visually read the tally; human verdict still blank. |  |
| C06 | relax option | Ease rationing and substitute imports is an offered alternative. | Pinned YAML alternative record; S15 and accepted road 8. | `basis: debated`, by the Knesset, dated 8 August 1950, is copied metadata; S15 supports a modified ration, not outright relaxation through imports. Human passage/date verdict remains blank. |  |
| C07 | window | The card is available from 1 July until 1 September 1950 (exclusive). | Pinned YAML from/to. | Game scheduling is distinct from a dated announcement. |  |
| C08 | effects | Social-cohesion and foreign-currency changes. | Pinned YAML and model.yaml. | Design values only; excluded from factual certification. |  |

## Known gaps and contradictions

- Exact announcement and strike endpoints require original JTA datelines: both linked original HTTPS PDFs timed out (exit 28), while readable HTML dates are publication headers. No exact-day correction is made.
- The card says prices are climbing; a general black-market paragraph alone does not support that precise time-bounded trend.
- The merged alternative record has `basis: debated`; the exact imports wording is broader than the modified ration described by S15. Preserve this scope limit and the original dateline gap for the historian.

## Existing YAML comments - unchanged assertions under review

These are copied from the pinned file for checking, not newly endorsed claims or fresh source dates.

```yaml
# yaml-language-server: $schema=../schema/event.schema.json
# For the native reviewer (#564): Hebrew terminology follows he.wiki "תקופת הצנע" (קיצוב, לבוש והנעלה, השוק השחור):
# https://he.wikipedia.org/wiki/תקופת_הצנע
# Extended on 31 July 1950; retailers' strike 1-14 August. On 8 August the Knesset passed the coalition parties' motion,
# which Ben-Gurion made a question of confidence, 57-36; the opposition motions failed (plenum, 1_ptm_250242).
    # Who proposed it: research_notes/Alternate timelines/roads-checked.md (checked by the owner, 19 October 2026).
      # The day of the confidence vote, from this file; accepted by the owner on 18 October 2026 (#949).
  # JTA: the clothing and shoe stores shut on 1 August, the day after the announcement; on 13 August the two-week strike
  # was to end the next day.
```

## Human review and refresh checklist

- [ ] Owner confirms the actual volunteer's qualifications, independence and scope; no candidate is assumed.
- [ ] Reviewer supplies their own claim-by-claim verdicts and precise passages; keep unresolved dissent.
- [ ] Reviewer expressly consents to any public name/handle and to CC BY 4.0 for their future review text before it is committed. No consent is inferred from this draft.
- [ ] Before human handoff, confirm this fixed source commit remains the agreed scope. Merged #1091/#1114/#1103/#1104 are now reconciled; any later YAML/text/term change requires a fresh comparison. Earlier acceptance never silently covers changed text.
- [ ] Native Hebrew review is separately recorded when applicable; no flag is removed by an agent.
- [ ] Follow [HISTORIAN_REVIEW.md](../../../HISTORIAN_REVIEW.md): actual review in a PR comment; only the reviewer writes any future YAML sign-off, in their own commit. No status or release-gate transition is implemented by these docs.

Packet/reproduced content: CC BY-SA 4.0, credit Statecraft: Israel contributors; [licence](../../../../LICENSES/CC-BY-SA-4.0.txt). This does not license or include a future reviewer's text before their separate consent.
