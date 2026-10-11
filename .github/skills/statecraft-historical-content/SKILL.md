---
name: statecraft-historical-content
description: 'Research, draft, and review historical events and political writing for Statecraft: Israel. Use for source-checking claims, event drafting, content gaps, and 1948-1956 narrative work.'
argument-hint: 'event topic / date range / source question / historical gap'
user-invocable: true
disable-model-invocation: false
---

# Statecraft Historical Content

## When to use

- drafting or revising event text in `content/events/`
- finding historical gaps in the 1948–1956 timeline
- checking whether a claim is source-backed and suitable for the repo
- editing player-facing writing for historical clarity and tone

## Core repo rules

Before drafting, read these first:

- [README.md](../../README.md)
- [CONTRIBUTING.md](../../CONTRIBUTING.md)
- [docs/CONTENT_POLICY.md](../../docs/CONTENT_POLICY.md)
- [docs/WRITING_EVENTS.md](../../docs/WRITING_EVENTS.md)
- [docs/WRITING_GUIDE.md](../../docs/WRITING_GUIDE.md)

## Mandatory workflow

1. Identify the event, date range, or gap.
2. Check the existing content in `content/events/` and the relevant people/offices structure.
3. Verify the factual claim with a source before drafting.
4. Draft only in the repo’s historical framing and event schema.
5. Keep the text grounded, specific, and readable in a game UI.
6. Leave `status: draft` on anything an AI drafted. Only a person who has checked every claim against its source sets `fact-checked`.
7. Validate with `dotnet test sim/Statecraft.sln` when editing repo content.

## Historical writing standards

- No invented quotes or verbatim speeches without direct source support.
- Use office placeholders such as `{prime_minister}` rather than hard-coded names.
- Prefer concrete government, military, or social context over generic historical mood-setting.
- Keep war and atrocity language precise and sensitive.
- Favor the political and logistical realities of state-building over dramatic filler.

## Research checklist

- Is the claim connected to a real date and a real institutional decision?
- Is the wording supported by a cited source or a source packet?
- Does it fit the historical period and office holder?
- Does it avoid unsupported generalization or invented drama?
- Does it align with the project’s Israeli framing and source policy?

## Skill bundle contents

This skill package is intentionally structured like a real agent skill bundle, not a single markdown note.

- `scripts/` contains quick helper commands for checking source coverage and event completeness
- `templates/` contains YAML and checklist scaffolds for drafts
- `examples/` contains small examples of useful historical gaps and workflow patterns
- `references/` contains the supporting checklists and repo reference docs

## References

- [Historical writing guide](./references/historical-writing-checklist.md)
- [Content policy](../../docs/CONTENT_POLICY.md)
- [Event schema](../../docs/WRITING_EVENTS.md)
- [Event draft template](./templates/event-draft.yaml)
- [Source coverage script](./scripts/check_source_coverage.py)
