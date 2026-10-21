---
name: statecraft-ux-writing
description: 'Draft or review Statecraft: Israel interface labels, warnings, tooltips, help and onboarding against the implemented player flow. Use for new UI copy or improvements to existing strings, including English/Hebrew review handoffs.'
---

# Statecraft UX writing

Use this workflow for interface copy. For historical events, use the historical-content and writing-voice skills and their sourcing rules.

## Establish the player need

1. Read [CONTRIBUTING](../../../CONTRIBUTING.md), the [writing guide](../../../docs/WRITING_GUIDE.md) and [content policy](../../../docs/CONTENT_POLICY.md). Historical claims also follow [sourcing guidance](../../../docs/SOURCING_FOR_CONTRIBUTORS.md).
2. Find the actual string, its caller and the simulation condition behind it. Read the surrounding screen and the next available action. A label alone cannot establish how a feature works.
3. State what the player needs to understand or do. Separate a wording problem from missing functionality or an unresolved design decision.
4. Check for current UX guidance. [PR #970](https://github.com/eyalgolan/statecraft-israel/pull/970) proposes a UI guide and glossary; treat them as proposals until accepted. Use accepted repository guidance when available, and identify terminology choices that still need the owner's decision.

## Draft from the implemented behavior

- Use concrete words for the action, consequence and available recovery step. Explain why an action is unavailable when the player can act on that reason.
- Do not promise a recovery action, outcome, undo option or safety guarantee the code does not provide.
- Read warnings as part of the full flow: what happened, what the player can still do, and whether continuing has consequences.
- Keep each message proportionate to its screen. Remove technical detail unless it changes the player's decision; do not expose internal identifiers or exceptions.
- Preserve localization keys, formatting tokens such as `{0}`, office placeholders and supported markup. Do not replace a dynamic value with a fixed one.
- Use the same term for the same concept. Flag conflicting existing usage rather than silently expanding a small copy change into a glossary migration.
- Keep mechanics in the simulation. A prose change cannot repair a simulation or presenter bug.
- Source historical claims before adding them. Never invent quotes or mark AI-assisted drafts as human reviewed.

## Produce a reviewable change

For each string, give the player need, current wording, candidate wording, implementation evidence and review status. See the [worked examples](references/worked-examples.md).

Apply changes to the real localization file when the task authorizes it. A proposal document is useful for unresolved choices, but does not replace implementing agreed copy.

English and Hebrew need to convey the same behavior and preserve tokens. Identify whether each language has had a native review. Do not treat a machine translation as approved Hebrew, change human review fields, or claim paired review when only English was edited. Keep a PR in draft if a required language or runtime gate is outstanding.

## Validate and hand off

1. Inspect the diff for keys, token parity and scope. Read the complete message in its screen context, including disabled states and recovery paths.
2. Run the repository's applicable validation commands. Changes under `unity/` require an Unreleased changelog entry and local Unity EditMode results under CONTRIBUTING. Record an unavailable or failing check accurately; passing YAML checks does not prove the UI fits or the Hebrew reads naturally.
3. Include before/after wording, evidence and outstanding owner/native decisions in the PR. Disclose AI assistance and leave personal contributor declarations for the contributor.
4. After acceptance, update examples or terminology references to the accepted wording. An unmerged example remains a proposal.
