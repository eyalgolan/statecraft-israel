# Cities and workable regions

## Player story

I send a population unit to a regional capital and assign work there, while the map explains which fields and facilities make it possible.

## Rules

### Regional management

Each player region has an authored capital and member tiles/locations. The capital is the civilian interface. Housing and Industry/workshop jobs are pooled regionally. A map marker does not automatically create another managed city. Small rural places retain map identity without separate population-management screens.

Farm assignments link a unit based at the capital to an available prepared field tile in that region. The tile must be workable, prepared, friendly controlled and have a free slot. [Site projects](build-yishuv.md) own preparation. Only staffed fields produce Food at [balance](balance.md#work-professions-and-training) tier rates; farming creates no Money. Farmers need not individually commute between each kibbutz and capital every turn.

Enemy control disables a field slot and makes its farmer idle at the capital. Retaking restores access without automatic rebuilding. [Settlement control](settlement-control.md) owns capital/region loss and evacuation. Enemy towns use its separate tax model, not this workforce pool.

### Assignments and income

A unit has one active assignment. Eligible free civilians may take Industry jobs or generic builder slots; trained farmers/pioneers may use generic assignments without learning another profession. Craftsmen modify Industry income and builder rates. Industry produces only when staffed; courses use [training](training.md) course income.

Farm work, Industry, building, teaching, courses and active service cannot run simultaneously for the same unit. Reassignment releases old capacity atomically. Invalid capacity/eligibility refuses before Money or state changes.

### Housing and condition

Ordinary regional capacity is limited. A civilian resident without dedicated reception, teacher or military-training housing uses ordinary housing. Overflow does not create beds, delete population or convert refugees. Free arrivals/evacuated free civilians may become overcrowded residents; incomplete-reception refugees use camp/port queues. Deployed active army units are not ordinary civilian residents.

Let `R_r` be ordinary civilian residents, `c_r` condition fraction, and `P` total own population. Project capacity/condition effects live in [balance](balance.md#project-catalogue), clamped to condition bounds. No separate household simulation is introduced.

Additional Money per turn is `R_r * max(0, housing.money_threshold - c_r) * housing.money_slope`. It rises smoothly; crossing the threshold never adds a flat fee to every resident.

Housing cohesion loss is `housing.max_cohesion_loss * sum((R_r / P) * max(0, (housing.cohesion_threshold - c_r) / housing.cohesion_threshold))`. With no own population it is zero. Camp residents do not receive a second ordinary-housing penalty. Food and port queue penalties have separate owners; do not also charge legacy effects for the same housing loss.

Overflow lowers condition by `housing.overflow_deterioration * max(0, R_r - capacity_r) / R_r` pp per turn, zero with no residents. Apply once alongside maintenance deterioration, then use the condition-to-cohesion rule without a second direct overflow penalty.

Unpaid upkeep lowers housing condition proportionally; [resources](cutover-resource-layer.md#money-and-upkeep) owns cash-shortfall allocation. Repairs pay the catalogue cost/work, improve condition and add no capacity. Only houses deteriorate from unpaid upkeep; no facility shutdown, army penalty or direct cash-shortage cohesion hit is added.

### City projects and builders

City projects include housing, Industry, reception camps, centres, military camps, workshops and repairs. Start requires full upfront Money and a national city-project start. The catalogue is the sole cost/work/upkeep definition.

Advance by assigned builder work rates up to `builder.slots_per_project`. No workers means pause, not timer completion. Ongoing projects may coexist while the nationwide new-start cap applies. Discard excess work on completion and release workers. Builders earn no simultaneous Industry income. Completed projects begin upkeep that turn but provide no second production pass.

Winery Industry uses these shared job/production rules; place/theme binding belongs to [locations](../locations.md). The maabara retains its existing reviewed ma'abarot-policy availability gate in 1950 [S4]; reception beds are separate, and the gate is not a fixed construction timer.

Stopping/moving workers pauses paid progress. Explicit cancellation discards unfinished work without refund. Field/road/rail/standalone pioneer work is outside the city-start cap.

### Arms workshops

A workshop has its own worker slot. Reserve the full cycle cost upfront at the worker's tier. Each staffed turn advances it; no worker means pause. Completion adds spare Arms nationally. All tiers share cycle duration; expertise lowers Money cost. Pause or worker change retains progress and paid cost; the next cycle uses the new tier. Refuse an unaffordable new cycle. The workshop pays no simultaneous Industry income.

### Initial authoring

[Starting inventory](balance.md#population-and-starting-state) binds population/facilities to authored friendly regions. Initial prepared fields must support the agreed staffed farmer mix across multiple regions. Census records remain evidence; do not round each small location into another whole game unit.

## Data touched

- Region id, capital, member hexes, housing/condition, facilities and jobs.
- Per-unit assignments reference regions and specific fields, projects, facilities and jobs.
- Legacy cities data and map classifications need migration rather than automatically instantiating regional economies.
- Commands and UI previews use shared sim validation.

## Done when

- A unit cannot occupy multiple jobs, builder slots, farm slots or training places simultaneously.
- Farm loss/retaking does not delete farmers or automatically destroy fields.
- Condition costs are continuous and national housing cohesion is population weighted.
- City construction requires actual work and the national start allowance.
- Workshops produce equipment without duplicate people or job income.

## Out of scope

Individual workforce screens per kibbutz, enemy resident workforce, mothballing, non-housing deterioration and automatically granting a city economy on founding completion.

## Sources

- [S1] Regional/housing decisions, docs/GDD.md.
- [S2] Numerical catalogue, docs/gdd/balance.md.
- [S3] Population eligibility, docs/gdd/training.md.
- [S4] Retained availability/winery decisions, docs/superpowers/plans/2026-10-15-gdd-phase-3.md, historical Q-HOUSE-3 and Q-SPEC-1/2.

## Open questions

No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
