# Statecraft: Israel — Game Design Document

**Version 0.3 · consolidated design revision, 25 October 2026.** This revision combines the inspected main-branch documentation, the location and map-editor work on `gdd-v0.2`, the comments on PR #856, and Amir's subsequent design decisions. It describes the intended game; it does not claim the existing simulation implements every change. Numerical defaults supplied under Amir's delegation are labelled separately from his explicit decisions. The v0.3 designation follows Amir's version decision on 25 October 2026; the recorded mechanics and original source snapshots are unchanged by this version-label update.

## MVP scope

The campaign runs from **15 May 1948 to 15 January 1952: 44 monthly turns**. The calendar boundary follows the [owner's latest PR ruling](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233776804) (checked 2026-10-17). The real-time play target is in [balance.md](gdd/balance.md#campaign-and-indicators).
  **Then decided (owner, 18 Oct 2026, "60-90 minutes"; [#813](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6249771314)):** a full session targets 60 to 90 minutes, about 2 minutes a turn.
  **Then changed (owner, 30 Oct 2026, "120 (data wins)"; [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164)):** a full session targets 120 minutes, matching `gdd03-balance.yaml`.

The player manages the establishment of the state through population, work, settlement, infrastructure, immigration and war. The map supports direct hex-to-hex orders and regional civilian management. Historical events explain the period and react to the player's situation; they must follow the existing [content policy](CONTENT_POLICY.md), [writing guide](WRITING_GUIDE.md) and review gates.

The core objectives are to establish the designated new settlements, complete the designated transport corridors, absorb immigration, and reach the armistices. Their exact completion tests, grouping and score ownership are in [objective-card.md](gdd/objective-card.md). A campaign can reach its end without completing every objective. Losing a region does not itself end the campaign.

## Document authority

This document owns scope, the authority map and deferred decisions. The linked feature file owns the corresponding rules. [balance.md](gdd/balance.md) is the only active catalogue of numerical design values; feature files reference its keys rather than maintaining their own cost tables. Historical dates and factual evidence remain with the relevant feature or content record.

|Subject|Authoritative document|
|-|-|
|Numerical values, prices, rates, capacity, starting state|[Balance](gdd/balance.md)|
|Population identity, professions, reception, courses, military conversion|[Training and population](gdd/training.md)|
|Regions, civilian assignments, housing, construction, workshops|[Cities and regions](gdd/cities.md)|
|Money, Food, Arms, imports, immigration, upkeep settlement|[Resource layer](gdd/cutover-resource-layer.md)|
|Movement, combat, military XP, forts, enemy AI, truces and prisoners|[Military and combat](gdd/military-combat.md)|
|Region loss, evacuation, occupation, annexation and withdrawal|[Settlement control](gdd/settlement-control.md)|
|Designated site projects and field preparation|[Build yishuv](gdd/build-yishuv.md)|
|Road and railway construction|[Corridors](gdd/corridors.md)|
|Objective tests, completion cards and scoring|[Objective card](gdd/objective-card.md)|
|Resolver sequence, saves and migration|[Game loop](gdd/loop-switch.md)|
|Calendar implementation and event windows|[Monthly turns](gdd/monthly-turns.md)|
|Events, government integration and campaign ending|[Ministers, events and end](gdd/ministers-events-end.md)|
|Fixed camera and internal map authoring|[Map editor](gdd/map-editor.md)|
|Named places, aliases, inclusion priority, facility roster and source notes|[Locations](locations.md)|
|Route geometry and evidence|[Routes](routes.md)|
|Unit artwork and historical placement anchors|[Units](units.md)|

[The feature index](gdd/README.md) explains the file structure. [The rule-ID index](rules/mvp.md) preserves links from existing tests and identifies implementation contracts requiring migration. Old plans are implementation history, not additional rule owners. The original [GDD v0.1 PDF](GDD-v0.1.pdf) is a historical reference; it is not a second active GDD.

## Population and management direction

The player selects and assigns individual population units through regional capitals, rather than managing each kibbutz separately. The same population unit retains its identity across civilian work, reception, training, military service and demobilisation. Civilian profession and military experience are separate attributes. Map anchors for historical formations do not create additional population units.

Player regions pool civilian housing and employment. Farming capacity comes from actual prepared, workable tiles in the region. Enemy towns and cities use individual control and tax relationships; their residents do not join the player's assignable workforce. The linked feature owners define these systems.

Attention is removed entirely from the MVP: no resource pool, action prices, replenishment, refusal condition or UI counter. Money and worker availability provide the construction constraints. There is no automatic borrowing or automatic Food refill.

## Map, locations and presentation

The complete must-have and optional roster is maintained only in [locations.md](locations.md). Inclusion does not automatically create a managed city, a capturable target, a new population unit, or an operating facility. Military camps are distinct selectable locations, highlighted and enlarged for readability; reception camps are a separate facility category.

The camera has a fixed viewing direction and pitch, supports pan and **dolly in/out**, and does not orbit. Unit movement is turn-based between hexes. The internal editor changes tiles and moves locations while preserving identifiers, source coordinates and regeneration-safe overrides; see [map-editor.md](gdd/map-editor.md).

### Terrain / tile / hex types

The MVP terrain catalogue is frozen at the existing eight biomes, with no new types or terrain conversions. Editable properties beyond the existing biome fields and final tile/hex presentation remain **TBD**. Workable land, prepared fields, roads, forts and authored location placement can be represented as explicit properties without inventing a replacement terrain taxonomy.

## Decision provenance and implementation status

* The base is the inspected `main` at `e213d960188608f7edb590c8cb364f13e794b623`. Existing factual evidence and content-review status are retained; no human review or owner approval is fabricated.
* The location/editor source is `gdd-v0.2` at `36443ec81e9f22d0edf793996fd07002a1d40936`. The original final refresh found that branch at `c9cb11990acfc413a853ae6d365af01ac0d56516`; its upload changes reintroduced older scope/priority text. This revision resolves those overlaps using the later owner rulings and interview decisions rather than treating the upload as a new design decision. The owner's [PR review](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6230916598), [location approvals](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6230935027), [further approvals](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233524779), and [calendar/founding-site ruling](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233776804) were checked 2026-10-17.
* The [Arms request](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231351946) and [real-time play request](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231565826), checked 2026-10-17, are addressed by the balance catalogue and population/training rules.
* **Session target history:** the owner first set the play-time target after the v0.2 review, then superseded it after the balance data was available.
   **Then decided (owner, 18 Oct 2026, "60-90 minutes"; [#813](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6249771314)):** a full session targets 60 to 90 minutes, about 2 minutes a turn.
   **Then changed (owner, 30 Oct 2026, "120 (data wins)"; [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164)):** a full session targets 120 minutes, matching `gdd03-balance.yaml`.
* Amir's design interview on 17 October 2026 supplies the later mechanics in this revision. His latest correction supersedes earlier answers: only housing deteriorates from unpaid upkeep; farmer/pioneer advancement uses actual working turns; refugee reception precedes civilian work; Attention and combat Arms expenditure are removed.
* **Agreed** in the balance catalogue means an explicit interview decision or a retained approved baseline. **Default** means an initial balance choice supplied under Amir's instruction to fill numerical blanks. Neither label means a new historical claim or an already implemented feature.
* Existing `content/` values, tests, saves and code still describe the earlier implementation where they conflict. Implementation work must migrate them together, update rule-ID coverage and deliberately review hash changes. This documentation revision does not silently change that code or its data.

## Deferred decisions

These are recorded once here. Feature files link here instead of maintaining competing open-question lists.

|Decision|Status / boundary|
|-|-|
|Starting army size|**Deferred explicitly by Amir.** It remains the parameter `A`; no previously proposed count is accepted.|
|Regional boundaries and capital roster|Authoring work pending. Bind real prepared fields and existing places to regions; do not invent historical administrative boundaries as facts.|
|Founding-date verification|Keep the approved required-site membership; resolve Kiryat Shmona's conflicting day and Beit Shemesh's second-source requirement before releasing their content. Evidence stays with the objective owner.|
|Facility availability and starting training-centre host|The existing owner ruling already starts Schneller and Tel HaShomer as military facilities; the other military camps are approved build sites. Verify reception-camp availability and choose the starting reception/centre host without assuming every roster entry operates on the opening turn. Centre and teacher counts are decided.|
|Final defeat conditions|A single lost region, an unpaid Money bill, and coalition collapse do not independently end the campaign. The remaining terminal defeat rule is **TBD**.|
|Minister-specific gameplay|The existing office-competence slice (`R-MIN-001` to `R-MIN-003` and `Cabinet.Appoint`) is the complete MVP minister mechanic. Historical offices and event references remain; Attention is not restored through cabinet mechanics.|
|Terrain presentation|**TBD**. The MVP catalogue itself remains frozen at the existing eight biomes.|
|Novice profession lock-in|Trained professions are permanent and professions cannot be held simultaneously. The precise lock-in point before formal qualification is **TBD**; do not add a retraining UI.|
|Sensitive capture outcomes|Raid/deport/raze outcomes require the existing content-policy and review decisions before implementation. Do not infer displacement, rewards or civilian labour from occupation or tax rules.|
|Specialised military equipment and operation-card orders|Specific gun/vehicle models and unsourced operation orders remain evidence/review work; ordinary recruitment and reusable Arms rules are settled.|

Reception-camp growth into permanent towns, an individually managed economy for every rural settlement, and a detailed enemy supply/reinforcement economy are outside the MVP. They are not unresolved numerical values.
