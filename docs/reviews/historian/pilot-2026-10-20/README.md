# Historian pilot: three draft review packets

**Agent-prepared inventory, not a historian review.** No volunteer, qualification, consent, verdict or signature is invented. No content, game rules, schema, review tooling or release gate changes. Refs #29; does not complete it.

These packets follow the [owner's 19 October 2026 ruling](https://github.com/eyalgolan/statecraft-israel/issues/29#issuecomment-6266095198) and the existing [review guide](../../../HISTORIAN_REVIEW.md). The ruling releases a draft-preparation slice now; it requires a refreshed final packet after pending edits settle. The old proposal's unapproved tooling/schema is not implemented here.

## Snapshot and pending edits

Source commit: `2d3e13c098e147493ab034ab2ff20f0b04765fab`. Prepared 20 October 2026. [Manifest](snapshot-manifest.json) pins event, people, office, model and resolver byte hashes. The packet commit is a later docs-only commit; it is not the content snapshot. A reviewer should receive a stable final snapshot, not these drafts as if future edits had already landed.

| Packet | Pending work to reconcile before final handoff |
| --- | --- |
| [Elections 1949](elections_1949.md) | [#1103](https://github.com/eyalgolan/statecraft-israel/pull/1103) adds draft Hebrew via #1091; recheck the actual final diff |
| [Clothing rationing 1950](clothing_rationing_1950.md) | [#1114](https://github.com/eyalgolan/statecraft-israel/pull/1114) adds alternative metadata; #1103 adds draft Hebrew via #1091 |
| [Transit camps](maabarot_policy.md) | #1114 alternative metadata; [#1104](https://github.com/eyalgolan/statecraft-israel/pull/1104) draft Hebrew via #1091 |

Pending PR descriptions are coordination leads, not committed input to these packets. All currently cited event URLs were retrieved: 19 with urllib, one official Gazette PDF with curl after a Python issuer-chain error; full TLS verification stayed on. Actual reader pages and unreviewed passages are distinguished in each inventory. Two additional original JTA bulletin links timed out via HTTPS; their datelines remain an exact-day gap. Source PDFs/HTML, notices and extracted texts are kept locally as evidence, not published or relicensed here.

## What the packet does and does not establish

Each event has its exact source link/hash, window with exclusive end, historical-reference body/options, game-design effects, all current source entries and original checked dates, all YAML comments, claim/source/locator rows with blank human verdicts, and known gaps. Existing `fact-checked` status is copied metadata, not a new attestation. None of these events gets `historian-reviewed`, reviewed_by/reviewed_on or a changed sensitivity/native flag.

Historical fallback rendering comes from the pinned people.yaml terms and existing resolver. Actual saves may appoint another holder. The three windows have no historical-reference holder change in the pinned records; the packet does not newly certify the primary office sources. English factual review and native Hebrew review are distinct.

Useful review flags include a Harpaz page-locator discrepancy, an existing election-record date/turnout discrepancy, CET's bibliography 1986 versus the current source-comment 1987, and original JTA dateline access gaps. These are questions for source checking, not edits to the game's values or a claim that every source is wrong.

## Reviewer-owned next actions

The owner recruits one independent qualified volunteer **through #29**, with no honorarium promised or direct outreach by this agent. The owner confirms fit/independence. The actual reviewer chooses scope and supplies per-claim verdicts; unresolved dissent blocks positive historian acceptance. Public identity and CC BY 4.0 review-text consent require that person's express agreement. Their credit is limited to reviewed events, not endorsement. No private contact details belong here.

Refresh these drafts at one actual fixed commit after #1114/#1103/#1104 (and their predecessor) settle. Follow the existing PR-comment process; only the reviewer writes their own future sign-off in their own commit. Keep sensitivity review separate. The pilot adds no new release blocker; a later sensitive batch needs two independent qualified reviewers under the owner ruling. This packet contains no reviewer's text to which consent could be inferred.

## Attribution

These packets reproduce the project's CC BY-SA 4.0 game text/comments, credit **Statecraft: Israel contributors**, and are themselves under [CC BY-SA 4.0](../../../../LICENSES/CC-BY-SA-4.0.txt). The path-specific row in LICENSING.md records this instead of incorrectly treating copied content as generic CC BY 4.0 documentation. It does not change the separate consent/licence requirement for any future human review record. Third-party source pages are linked, not reproduced wholesale.
