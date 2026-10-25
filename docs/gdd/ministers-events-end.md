# Government, events, indicators and the campaign end

## Player story

I read historical choices/national conditions and reach an ending explaining my achievements without hidden failure rules.

## Rules

[GDD scope](../GDD.md#mvp-scope) owns the end boundary; final scoring precedes ending. Show score, achieved/pending objective groups, historical comparisons and national/resource state. All goals are not required merely to finish.

Region loss, Money shortfall and coalition instability are not standalone defeat triggers. Do not restore any-city loss or exhausted-resource countdown through event code. Final terminal defeat remains explicitly deferred; a missing principle cannot be filled by choosing a number.

Historical offices, people and event roles remain. Attention is removed from cabinet/events and map actions. Minister-specific mechanics remain deferred. Old budget labels cannot restore passive taxes/spending, borrowing or another action-budget resource.

Social cohesion retains `morale` compatibility; support retains `intl_standing`. [Balance](balance.md#campaign-and-indicators) owns scale/hysteresis. Keep the independent employment-to-cohesion link using the same regional employment aggregate as the objective helper. Disable legacy Food/housing links duplicating explicit new penalties. Low situations expose state to reviewed events without secret extra penalties or defeat.

Events may affect approved indicators or explicit stocks. Population/facility effects use sim validation/accounting rather than UI mutation. Costs cannot silently borrow and grants cannot duplicate arrivals. Keep hard/soft windows and sensitivity restrictions under [WRITING_EVENTS](../WRITING_EVENTS.md)/[CONTENT_POLICY](../CONTENT_POLICY.md).

[Objective-card](objective-card.md) owns completion/images/dates/points and [combat](military-combat.md) owns armistices/prisoner return. Do not duplicate their tables. Changed historical/sensitive content still requires human factual, licence and sensitivity review.

## Data touched

- Government/event adapters, indicator compatibility, low-state visibility and ending views.
- Reviewed content is not rewritten/re-approved by this documentation.
- Event/optional-spending adapters need ruleset-aware migration.

## Done when

- A normal ending is reachable with unfinished goals and reports actual results.
- Region loss/unpaid Money cannot call retired defeat triggers.
- Attention is absent across government and event interactions.
- Effects apply once under the existing release gates.

## Out of scope

Invented minister perks, a new final defeat principle, unreviewed event rewrites and old passive budget restoration.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] The 1949 armistice layer, content/map/armistice_hexes_1949.yaml (signing dates per agreement)
- [S4] The owner's phase answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15
- [S5] Later decisions/deferred principles, docs/GDD.md.

## Open questions

See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
