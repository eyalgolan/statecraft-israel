# Historian pilot: three draft review packets

**Agent-prepared inventory, not a historian review.** No volunteer, qualification, consent, verdict or signature is invented. No content, game rules, schema, review tooling or release gate changes. Refs #29; does not complete it.

These packets follow the [owner's 19 October 2026 ruling](https://github.com/eyalgolan/statecraft-israel/issues/29#issuecomment-6266095198) and the existing [review guide](../../../HISTORIAN_REVIEW.md). The ruling releases a draft-preparation slice now; it requires a refreshed final packet after pending edits settle. The old proposal's unapproved tooling/schema is not implemented here.

## Snapshot and reconciled merges

Source commit: `c8f4bc027e32db3147fc64a919e538f3aea5e3f6`. Prepared and refreshed 20 October 2026. [Manifest](snapshot-manifest.json) pins event, people, office, model, resolver and accepted alternative-research byte hashes. The packet commit is a later docs-only commit; it is not the content snapshot or a runtime test of that source.

| Packet | Merged changes reconciled at this fixed source commit |
| --- | --- |
| [Elections 1949](elections_1949.md) | [#1103](https://github.com/eyalgolan/statecraft-israel/pull/1103) Hebrew title/body/option, still agent-checked |
| [Clothing rationing 1950](clothing_rationing_1950.md) | [#1114](https://github.com/eyalgolan/statecraft-israel/pull/1114) debated alternative and nested source/date; #1103 Hebrew text, still agent-checked |
| [Transit camps](maabarot_policy.md) | #1114 design alternative; [#1104](https://github.com/eyalgolan/statecraft-israel/pull/1104) Hebrew text, still agent-checked |

Their predecessor [#1091](https://github.com/eyalgolan/statecraft-israel/pull/1091) is also merged and included. The packets show all current English/Hebrew text and options, all original comments, 20 event-level source entries and the additional nested alternative entry (same URL as S15, distinct checked date). Native Hebrew review remains pending. Snapshot metadata does not confer historian acceptance.

The earlier preparation retrieved the 20 event-level cited URLs: 19 with urllib, one official Gazette PDF with curl after a Python issuer-chain error; full TLS verification stayed on. Its notes distinguish read pages from unreviewed passages and are preserved, not represented as new source reads at this re-pin. The alternative's JTA HTML was reopened/read by the agent on 20 October 2026; it supports a modified ration, not the exact imports wording. Two earlier original JTA bulletin attempts timed out via HTTPS; their datelines remain an exact-day gap. Source PDFs/HTML, notices and extracted texts are kept locally as evidence, not published or relicensed here.

## What the packet does and does not establish

Each event has its exact source link/hash, window with exclusive end, historical-reference body/options, game-design effects, all current source entries and original checked dates, all YAML comments, claim/source/locator rows with blank human verdicts, and known gaps. Existing `fact-checked` status is copied metadata, not a new attestation. None of these events gets `historian-reviewed`, reviewed_by/reviewed_on or a changed sensitivity/native flag.

Historical fallback rendering comes from the pinned people.yaml terms and existing resolver. Actual saves may appoint another holder. The three windows have no historical-reference holder change in the pinned records; the packet does not newly certify the primary office sources. English factual review and native Hebrew review are distinct.

Useful review flags include a Harpaz page-locator discrepancy, an existing election-record date/turnout discrepancy, CET's bibliography 1986 versus the current source-comment 1987, and original JTA dateline access gaps. These are questions for source checking, not edits to the game's values or a claim that every source is wrong.

## Reviewer-owned next actions

The owner recruits one independent qualified volunteer **through #29**, with no honorarium promised or direct outreach by this agent. The owner confirms fit/independence. The actual reviewer chooses scope and supplies per-claim verdicts; unresolved dissent blocks positive historian acceptance. Public identity and CC BY 4.0 review-text consent require that person's express agreement. Their credit is limited to reviewed events, not endorsement. No private contact details belong here.

The requested merged edits are reconciled at the fixed source above. Before actual handoff, the owner should confirm the snapshot scope and review the path-specific LICENSING.md row, which remains pending owner approval. Any later source change requires a new refresh. Follow the existing PR-comment process; only the reviewer writes their own future sign-off in their own commit. Keep sensitivity review separate. The pilot adds no new release blocker; a later sensitive batch needs two independent qualified reviewers under the owner ruling. This packet contains no reviewer's text to which consent could be inferred.

## Attribution

These packets reproduce the project's CC BY-SA 4.0 game text/comments, credit **Statecraft: Israel contributors**, and are themselves under [CC BY-SA 4.0](../../../../LICENSES/CC-BY-SA-4.0.txt). The path-specific row in LICENSING.md records this instead of incorrectly treating copied content as generic CC BY 4.0 documentation. It does not change the separate consent/licence requirement for any future human review record. Third-party source pages are linked, not reproduced wholesale.
