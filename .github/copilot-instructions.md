# GitHub Copilot instructions for Statecraft: Israel

## Purpose

This repository is a historical strategy game with a strict content policy. Copilot should help produce useful, accurate work without drifting into unsupported historical claims, generic AI language, or game design that ignores the project’s source-backed model.

## Non-negotiable rules

- Never invent a quote, date, minister, office, or fact.
- Every historical fact in `content/` must be sourced and checked before it is treated as valid.
- Do not add unsupported claims to event text, ministry descriptions, or narrative copy.
- Preserve the project’s Israeli framing as set in `docs/CONTENT_POLICY.md`, section 1. Do not reopen it, and do not soften facts to avoid it.
- Treat war, terror, atrocity, and the Holocaust as sensitive subjects with careful wording and clear sourcing.
- Stay aligned with the repo’s rules in `CONTRIBUTING.md`, `docs/CONTENT_POLICY.md`, and `docs/WRITING_EVENTS.md`.

## Writing expectations

- Prefer direct, grounded historical writing over dramatic filler.
- Use clear, concrete language. A good event card explains what happened, why it mattered, and what decision the player faces.
- Avoid generic AI phrasing and padded exposition.
- Keep the writing readable in a game UI: concise, legible, and easy to scan.
- If a player-facing sentence is not backed by sources, it should be treated as a draft requirement, not a finished line.

## Event content guidance

When editing or drafting event YAML in `content/events/`:

- use office placeholders like `{prime_minister}` rather than fixed names
- keep the text tied to a real historical window and not a vague, timeless summary
- include proper `sources:` entries with URLs and `checked:` dates
- respect event and option field rules from `docs/WRITING_EVENTS.md`
- avoid soft historical wording that blurs political responsibility or causes

## Tone

Write in a tone that is:

- direct
- sober
- concrete
- politically literate
- historically grounded

Avoid:

- slogan-like language
- exaggerated melodrama
- moral grandstanding
- vague “This was a pivotal moment in history” filler
- AI clichés like “it is important to note that” or “in today’s rapidly evolving landscape”

## Workflow

1. Read the relevant historical files before editing.
2. Check whether the event or content already exists in a similar form.
3. Find the source and confirm the claim.
4. Draft the content in the repo’s established style.
5. Validate with `dotnet test sim/Statecraft.sln` and `uv run scripts/oss/check_repo.py`.
6. Keep the scope tight and the change focused.
7. Never run `git commit` or `git push` unless the user asks for it in that message.

## Before finalizing a content change

Ask:

- Is every factual claim backed by a cited source?
- Is the wording clear enough for a game event card?
- Does it fit the period, office, and decision context?
- Does it avoid invented quotes, unsupported policy claims, or rhetorical drift?

If the answer is not clearly yes, keep researching.
