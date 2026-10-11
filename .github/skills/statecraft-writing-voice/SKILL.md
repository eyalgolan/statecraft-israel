---
name: statecraft-writing-voice
description: 'Improve and review Statecraft: Israel prose for tone, clarity, and historical credibility. Use when drafting event text, checking voice, or making copy more grounded and game-readable.'
argument-hint: 'event text / tone review / rewrite / voice pass'
user-invocable: true
disable-model-invocation: false
---

# Statecraft Writing Voice

## When to use

- revising event card text
- tightening political or historical narration
- checking phrasing for clarity and tone
- making copy more grounded and less generic

## Voice goals

Write with these qualities:

- direct
- specific
- sober
- concrete
- readable under time pressure

## Tone standards

Prefer:

- plain historical language
- clarity over flourish
- consequence over abstraction
- strong institutions and dates
- calm, tense, credible prose

Avoid:

- inflated heroism
- generic AI filler
- invented quotes
- vague emotional summaries
- rhetorical over-claiming

## Revision checklist

Before accepting a draft, ask:

- Does it sound like a decision card, not a TED Talk or a film trailer?
- Is the phrasing clear in a UI context?
- Does it explain the stakes without padding?
- Does it fit the repo’s historical framing?
- Is it source-safe and not drifting into unsupported drama?

## Skill bundle contents

This skill package is intentionally structured as a reusable agent workflow with supporting assets.

- `scripts/` contains lightweight checks for generic AI phrasing and weak copy patterns
- `templates/` contains a quick tone pass checklist for event rewrites
- `references/` holds the supporting writing guidance and style material

## References

- [Writing guide](../../docs/WRITING_GUIDE.md)
- [Content policy](../../docs/CONTENT_POLICY.md)
- [Event writing rules](../../docs/WRITING_EVENTS.md)
- [Tone rules](./references/tone-rules.md)
- [Tone pass template](./templates/tone-pass-checklist.md)
- [Phrase scan script](./scripts/scan_for_generic_ai_phrases.py)
