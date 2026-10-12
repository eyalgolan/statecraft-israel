# AGENTS.md

This repository is the Statecraft: Israel project. It is a historical strategy game and a content-heavy civic/history project with strict sourcing and review rules.

## Mission

Help the project stay historically grounded, readable, and technically consistent. Do not treat this as a generic game repo or a casual writing sandbox. The bar is higher because the game presents real people, dates, institutions, war, and political stakes.

## Core rules

- Every historical fact must be sourced. If a fact is not supported by a cited source, do not add it.
- No invented quotes. Paraphrase, and never put words in a real person’s mouth.
- Use the Israeli framing set in `docs/CONTENT_POLICY.md`, section 1. It is a stated design decision and is not reopened in content work.
- Sensitive events must remain tagged and reviewed appropriately. Do not bypass the project’s release and review gates.
- Preserve the project’s deterministic simulation rules. Game mechanics belong in the sim, not in presentation-only code.
- If a change touches `content/`, `sim/`, or `unity/`, align with the repo’s tests, changelog, and review conventions.

## What to read first

Before writing or editing content, review:

- `README.md`
- `CONTRIBUTING.md`
- `docs/CONTENT_POLICY.md`
- `docs/WRITING_EVENTS.md`
- `docs/WRITING_GUIDE.md`

## Content-writing rules

- Use office placeholders like `{prime_minister}` rather than hard-coded names in dynamic event text.
- Keep wording concrete and credible. Prefer clear historical description over melodrama.
- Show dates as `15 May 1948` style, not shorthand or invented formatting.
- Prefer short, grounded event text over rhetorical filler.
- Treat AI-generated prose as a draft, not a source of truth. A human must verify factual claims.

## Tone and style

- Direct, specific, and historically grounded.
- Calm, not sensational.
- Explain consequence and trade-off, not just crisis.
- Write for the player as a decision-maker, not as a spectator of a cinematic script.
- Favor clean factual precision over generic historical voiceover language.

## Typical workflow for a task

1. Understand the exact historical question or gap.
2. Check the relevant files in `content/` and existing event patterns.
3. Gather the source or citation for the fact before drafting.
4. Add or edit the event or narrative text in the repo’s expected format.
5. Run the validation commands below.
6. Keep the PR and change focused.

## Validation commands

- `dotnet test sim/Statecraft.sln` loads all of `content/` and must pass with 0 warnings.
- `uv run scripts/oss/check_repo.py` runs the repository policy checks that CI runs.
- If you change `sim/`, `content/` or `unity/`, add a line under `## [Unreleased]` in `CHANGELOG.md`.
- `scripts/check.sh` runs all of the above and the tools tests in one command. It stops at the first failure.

## Git

- Never run `git commit` or `git push` unless the user asks for it in that message.
- Stage and commit only the files the user names or the task clearly covers.

## Red flags

- Unattributed claims or vague historical language
- Fabricated quotes or supposedly verbatim speeches
- Stating a number, date or claim without a source that supports it
- Softening historical pain or atrocity into game flavor text
- Generic AI filler like “in today’s rapidly evolving landscape” or “it is important to note that”

## Files that define quality in this repo

- `docs/CONTENT_POLICY.md`: historical framing and factual standards
- `docs/WRITING_EVENTS.md`: required YAML structure for content
- `docs/WRITING_GUIDE.md`: tone, style and writing expectations
- `CONTRIBUTING.md`: repo workflow and validation standards

Keep the work in scope, sourced, and readable.
