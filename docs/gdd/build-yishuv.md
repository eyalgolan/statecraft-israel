# Designated sites and pioneer work

## Player story

I choose sites to develop and send a pioneer there, with rural production managed through regional capitals.

## Rules

### Sites and field work

Sites have stable ids, authored hexes, allowed project kinds and source/availability metadata. The [location roster](../locations.md) owns names/aliases. Reference founding dates are not unlocks; approved designated build sites are available from scenario start.

An eligible pioneer must reach the target hex. Allow one active project per pioneer and one pioneer per target. Validate availability, kind, control, prior completion and full upfront cost; no remote work, borrowing or Attention.

Staffed turns add `pioneer.work_rate` toward the [catalogue](balance.md#project-catalogue) requirement. Movement/stopping pauses paid progress; explicit cancellation discards it without refund. Completion marks built and releases the worker. [Training](training.md#population-identity-and-civilian-professions) owns working-turn XP.

Prepare only authored workable land in friendly regions, paying `field.cost` and completing `field.work` for `field.slots`. It produces nothing until a farmer is assigned through the capital; [cities](cities.md#regional-management) owns staffing/tile loss/retaking. Field work is outside city-start limits.

### Founding and facility projects

A yishuv completion marks its site built, without spawning people, housing, jobs, capitals or extra cohesion. [Objectives](objective-card.md) owns required membership, grouping, dates and points. Preserve founding-only sensitivity framing without rewards from displacement.

Standalone reception/military camps and training centres use on-site pioneer work and their catalogue cost/work. Centres require operating parent camps. City variants use [builders](cities.md#city-projects-and-builders), the same catalogue and national city-start allowance. Never build the same facility id through both interfaces. [Training](training.md) owns operation/capacity.

[Corridors](corridors.md) owns road/rail projects. Retain the approved Hula drainage site and drainage-only sensitivity treatment at `hula_drainage` cost/work. Completion unlocks a field-preparation opportunity, not free Food; prepare and staff it under current rules. The existing event retains sources/window and fires only while works are in progress [S7]. No new terrain class or unsourced claims are introduced.

A built required yishuv also exposes a field-preparation opportunity in its hex, not an already staffed farm. Different site kinds can share a hex; duplicate yishuv sites on a hex are refused under the retained authoring rule [S7].

## Data touched

- Designated sites, allowed project kinds, paid work state and stable region/hex/facility bindings.
- Movement/project commands share validation and exact work arithmetic.
- Prepared fields expose capacity, not an automatic Food event effect.

## Done when

- Work requires physical arrival, correct eligibility and upfront affordability.
- Staffing pauses retain progress; explicit cancellation discards it.
- Fields produce only through staffed accessible regional slots.
- Founding creates no duplicate people/economy, and facilities cannot be built twice.

## Out of scope

Camp-to-town progression, per-rural-place workforce screens, automatic historical-date founding and civilian-displacement rewards.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 plan for PR 5 and the owner's decisions, https://github.com/eyalgolan/statecraft-israel/issues/378, checked 2026-10-14
- [S4] Haim Gvati, 100 Years of Settlement, vol. 2, list of all Jewish rural settlements, https://benyehuda.org/read/14684, checked 2026-10-14
- [S5] Yad Ben-Zvi, "Israel Revealed to the Eye", Kibbutz Mefalsim, https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%9E%D7%A4%D7%9C%D7%A1%D7%99%D7%9D/, checked 2026-10-14
- [S6] Settlements founded in 1949-1951, rows 22, 23 and 25, research_notes/GDD objectives/settlements-1949-51.md
- [S7] GDD Phase 4 owner answers, https://github.com/eyalgolan/statecraft-israel/issues/782, checked 2026-10-17
- [S8] Later work/regional decisions, docs/GDD.md.
- [S9] Catalogue, docs/gdd/balance.md.

## Open questions

Unresolved principle and authoring decisions are maintained only in [the GDD](../GDD.md#deferred-decisions).
