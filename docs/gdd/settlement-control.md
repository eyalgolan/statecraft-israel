# Settlement control, region loss and governance

## Player story

I can recover from losing a region and understand controlling an enemy town without making its residents assignable workers.

## Rules

### Player regions and automatic evacuation

Capturing a player capital transfers the whole region and removes access to its housing, jobs, production and projects. Administration does not relocate within it. Losing a field alone disables only that slot under [cities](cities.md#regional-management).

Civilians automatically evacuate to a friendly region even without an open route, an explicit MVP simplification. Preserve ids, profession, work XP, reception and military experience. Select nearest friendly capital by hex distance, stable region-id tie break. Without a friendly capital, keep civilians in an evacuation transit pool rather than deleting/capturing them. Terminal defeat remains in [the GDD](../GDD.md#deferred-decisions).

Process evacuation during control resolution. Free civilians with a destination join it under its housing/overflow rules. Refugees seek reception beds, otherwise a friendly port queue, or evacuation transit if no port exists. Dedicated teaching/training assignments end when their facility is lost. Remaining transit follows [resource costs](cutover-resource-layer.md); Food continues normally.

Military units retain their map orders; [combat](military-combat.md) owns retreat/surrender. Facilities and unfinished progress remain attached to map ids but inaccessible until retaken. Retaking restores surviving authored facilities and fields; individual hostile tile occupation can still block slots. Civilians are not automatically casualties, captives or enemy workers, and losing a region does not itself end the campaign.

### Enemy towns and cities

Each enemy town/city has individual control. Capturing it does not transfer a surrounding region or every nearby locality. Preserve the curated capture-target list separately from the [map roster](../locations.md); inclusion grants no capture permission. Shared-hex markers keep separate ids rather than merged resident/administration records.

Capture requires a curated target, friendly ground unit at its hex and no defending enemy unit. Combat may clear defenders without automatically executing a civilian-control action. Change only the selected locality. Preview eligibility and control consequences before confirmation; refuse unavailable/review-gated outcomes.

### Occupation and annexation

Occupation requires [balance](balance.md#military-and-control) garrison and advances through `occupation.turns`. Withdrawing before the timer ends restores enemy control and clears occupation progress. Occupation pays `occupation.tax`.

After occupation the player may annex or withdraw. The MVP has no puppet regime. Apply annexation's one-time International Support cost, clamped. Taxes use the catalogue town/city classification, not population-based income.

Annexation retains direct control/full taxes after garrison withdrawal. It creates no local military AI, free units or player workforce. Repeat-clicking an annexed settlement has no effect. Enemy recapture clears annexation and taxes; a later capture starts a new occupation.

Residents remain separate from player civilians, training candidates and reception refugees. At settlement pay only the current controlled regime's taxes, never occupation plus another regime twice.

### Civilian protection and review boundaries

Old references to Raid, Occupy and Deport do not approve every sensitive outcome. Only occupation/regime rules above are completed here. Do not infer displacement, razing, civilian capture, forced labour or rewards from suffering. [CONTENT_POLICY](../CONTENT_POLICY.md) and curator/sensitivity gates remain. Prisoners of war are combatants under a separate system.

## Data touched

- Region/capital membership, per-locality control, target allow-list, occupation progress, annexation and one-time cost state.
- Evacuation preserves people and releases assignments; tile control remains distinct from locality administration.
- Capture commands/events require migration and review; this edit does not add capture targets or change review status.

## Done when

- Capital capture disables a region; field capture disables only that slot.
- Route-blocked evacuation preserves people/state without duplication.
- Pre-annexation withdrawal loses control; after annexation a garrison withdrawal retains control.
- Residents never enter the player workforce/reception pipeline by annexation.
- Unreviewed outcomes remain unavailable and shared-hex identities stay distinct.

## Out of scope

All historical civilian fates, capture eligibility for every marker, civilian labour and final terminal-defeat conditions.

## Sources

- [S1] Region/control interview decisions, docs/GDD.md.
- [S2] Costs and taxes, docs/gdd/balance.md.
- [S3] Sensitive-content constraints, docs/CONTENT_POLICY.md.

## Open questions

Unresolved principle and authoring decisions are maintained only in [the GDD](../GDD.md#deferred-decisions).
