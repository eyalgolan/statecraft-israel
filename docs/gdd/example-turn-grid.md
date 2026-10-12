# Example: the turn grid reaches an era boundary

<!-- A filled-in example of the template, built from a rule the chapters design already sets (§4.1). Not a new design. -->

## Player story

As a player in a chapter, I want each turn to end exactly on the date the next chapter begins, so that I never get a turn of a few days with a full turn of attention.

## Rules

- The founding chapter begins on 15 May 1948 [S1].
- A turn that would cross the next chapter's start date ends on that date.
- A turn that would leave under 14 days before the start date is lengthened to end on it, so a weekly turn lasts between 7 and 21 days.
- Rates that scale with time use the turn's real days divided by the days in a 365 day year.

## Data touched

- `content/eras.yaml`: each era's `from` and `to`.
- `GameState.Date` and `GameState.Turn`.

## Done when

- A two-era test lands on the boundary date and the game goes on.
- An adjusted weekly turn is never shorter than 7 days or longer than 21 days.
- The golden hashes do not move.

## Out of scope

- What carries from one chapter into the next.

## Sources

- [S1] Chapters architecture, decision D3, docs/superpowers/specs/2026-10-10-chapters-architecture-design.md

## Open questions

- None.
