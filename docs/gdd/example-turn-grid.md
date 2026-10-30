# Example: one monthly turn

## Player story

I understand planning/resolution without learning a second ruleset.

## Rules

This is illustrative; [loop-switch](loop-switch.md#turn-boundaries-and-resolution) owns sequence and [monthly-turns](monthly-turns.md) owns calendar.

- Turn opening makes due purchase shipments available and shows people, jobs, facilities and existing work.
- Planning changes standing orders, workers, affordable purchases and permitted project starts, with sim previews.
- Enemy actions/combat/control precede courses/work/production. Food settles for that month, immigrants arrive, Money/conditions settle once, occupation advances and objectives are checked.
- Arrivals do not retrospectively work/eat for the preceding month; specifically defined transit/queue costs still apply at turn end.
- Next planning shows completion cards and new capacity; the boundary turn reaches its ending after score.

Prices, dates, scale and durations refer to their owners rather than another example balance table.

## Data touched

- No additional schema or game state.
- Walkthrough UI uses real previews/results.

## Done when

- The example follows the authoritative resolver and adds no alternative values or timings.

## Out of scope

Another turn order, hard-coded sample prices and claiming planned mechanics run in the current build.

## Sources

- [S1] Resolver, docs/gdd/loop-switch.md.
- [S2] Calendar, docs/gdd/monthly-turns.md.

## Open questions

See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
