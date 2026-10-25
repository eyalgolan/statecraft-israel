# Monthly calendar and event windows

## Player story

I plan against a consistent monthly calendar and receive historical decisions in time to act.

## Rules

Only [GDD scope](../GDD.md#mvp-scope) defines campaign start/end/count. Advance one calendar month on the start-date day. Final resolution reaches the boundary exactly, then no further events/turns. Security changes do not change time length.

Align decision windows to the grid boundary on/before their historical decision. Preserve sourced exceptions and during/after-the-fact choices rather than inventing earlier decisions. Hard anchors remain fixed; soft cards retain eligibility. [Balance](balance.md#campaign-and-indicators) owns maximum card gaps.

The loader and sim share calendar/window validation. [Loop-switch](loop-switch.md) owns sequence; [objectives](objective-card.md) owns historical calendar-month differences. Art/presentation dates cannot change the clock.

## Data touched

- Era turn lengths, campaign boundary, event windows and date helpers.
- Historical reference dates remain sourced content, not balance guesses.

## Done when

- Turns reach exact monthly boundaries and stop at the documented end.
- Security changes cannot add weekly/quarterly or partial final turns.
- Historical anchors/exceptions survive migration.

## Out of scope

A second duration definition, variable turn speed by security rung and later playable calendar expansion.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, decision D-B1, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md

## Open questions

See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
