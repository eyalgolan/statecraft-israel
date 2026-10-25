# Pioneer progression: compatibility handoff

## Player story

I see work experience and predict its effect on future projects.

## Rules

[Training](training.md#population-identity-and-civilian-professions) owns civilian advancement. [Balance](balance.md#work-professions-and-training) owns thresholds/rates. This existing path stays for compatibility/migration and defines no competing multiplier or threshold.

Replace founding/service shortcuts and veteran double-progress with actual work turns and current tier rate. Novices may become veteran through work without courses. Promotion creates no extra people, tool refunds, progress reset or military-XP change.

All pioneer projects use shared work/XP logic. Teaching, travel, idle time and courses do not count. [Sites](build-yishuv.md)/[corridors](corridors.md) own eligibility/outcomes.

## Data touched

- Legacy service/progress constants require migration.
- Profession/tier/work XP belongs to the same population unit.

## Done when

- Every pioneer project calls shared advancement.
- Existing links reach current rules instead of old thresholds.

## Out of scope

Separate progression rules, elapsed-service XP and exchanging military/civilian experience.

## Sources

- [S1] Progression owner, docs/gdd/training.md.
- [S2] Catalogue, docs/gdd/balance.md.

## Open questions

See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
