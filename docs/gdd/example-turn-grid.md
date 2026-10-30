# Example: the turn grid reaches an era boundary

<!-- A filled-in example of the template, built from a rule the chapters design already sets (§4.1). Not a new design. -->
# Example: one monthly turn

## Player story

As a player in a chapter, I want each turn to end exactly on the date the next chapter begins, so that I never get a turn of a few days with a full turn of attention.
I understand planning/resolution without learning a second ruleset.

## Rules

- The founding chapter begins on 15 May 1948 [S1].
- A turn that would cross the next chapter's start date ends on that date.
- A turn that would leave under 14 days before the start date is lengthened to end on it, so a weekly turn lasts between 7 and 21 days.
- Rates that scale with time use the turn's real days divided by the days in a 365 day year.
This is illustrative; [loop-switch](loop-switch.md#turn-boundaries-and-resolution) owns sequence and [monthly-turns](monthly-turns.md) owns calendar.

- Turn opening makes due purchase shipments available and shows people, jobs, facilities and existing work.
- Planning changes standing orders, workers, affordable purchases and permitted project starts, with sim previews.
- Enemy actions/combat/control precede courses/work/production. Food settles for that month, immigrants arrive, Money/conditions settle once, occupation advances and objectives are checked.
- Arrivals do not retrospectively work/eat for the preceding month; specifically defined transit/queue costs still apply at turn end.
- Next planning shows completion cards and new capacity; the boundary turn reaches its ending after score.

Prices, dates, scale and durations refer to their owners rather than another example balance table.

## Data touched

- `content/eras.yaml`: each era's `from` and `to`.
- `GameState.Date` and `GameState.Turn`.
- No additional schema or game state.
- Walkthrough UI uses real previews/results.

## Done when

- A two-era test lands on the boundary date and the game goes on.
- An adjusted weekly turn is never shorter than 7 days or longer than 21 days.
- The golden hashes do not move.
- The example follows the authoritative resolver and adds no alternative values or timings.

## Out of scope

- What carries from one chapter into the next.
Another turn order, hard-coded sample prices and claiming planned mechanics run in the current build.

## Sources

- [S1] Chapters architecture, decision D3, docs/superpowers/specs/2026-10-10-chapters-architecture-design.md
- [S1] Resolver, docs/gdd/loop-switch.md.
- [S2] Calendar, docs/gdd/monthly-turns.md.

## Open questions

- None.
See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
