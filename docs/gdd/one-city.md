# Early city slice: implementation handoff

## Player story

I inspect a regional capital and assign people under the same rules as the complete design.

## Rules

This is a handoff, not another city rulebook. [Cities](cities.md) owns assignments, housing/builders/workshops; [training](training.md) owns people/reception/conversion; [resources](cutover-resource-layer.md) owns stocks/upkeep; [balance](balance.md) owns amounts.

A fixture can use a single region, but the playable start distributes farmers across multiple regions. Do not independently round every town or create an urban-only population total. Explicit fixture overrides are not production balance.

The old city recruit counts, housing-only refugee conversion, passive city tax and Attention refunds need migration. Existing tests describe older code until deliberately updated, not authority to keep obsolete mechanics.

## Data touched

- Regional fixtures, assignment commands, capacity summaries and migration tests.
- No independent constants belong here.

## Done when

- The slice uses canonical owners and shared sim helpers.
- Assignment/housing/conversion cannot create or lose people.

## Out of scope

A second city ruleset, hidden fixture balance and another population ledger.

## Sources

- [S1] Authority, docs/GDD.md.
- [S2] City owner, docs/gdd/cities.md.

## Open questions

See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
