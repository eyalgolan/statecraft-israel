# Road and railway corridor projects

## Player story

I build transport links segment by segment, with visible map progress and sourced historical completion cards.

## Rules

The simulation registry owns corridor ids, kind and ordered segments; [routes](../routes.md) owns evidence, waypoints and drawing geometry. A line is not automatically buildable. Charge a segment between consecutive sourced waypoints, not per crossed hex.

Approved rail construction is the Haifa–Tel Aviv link via Rosh HaAyin: the Yarkon link and west-of-Tulkarm bypass only with sourced alignment. Existing track remains existing. The historical card describes restored service on 20 September 1949, not the later coastal line [S3]. Without bypass evidence, only the Yarkon link is buildable.

Approved roads are Burma Road and Tel Aviv–Netanya coastal road. Burma uses existing `burma_road` waypoints; its card describes the separate Road of Valour opening on 7 December 1948 without identifying them as the same road [S4]. Coastal-road dates remain in research. [Objectives](objective-card.md) alone defines required group membership.

On-site pioneers may start unbuilt segments in any order, one worker per segment and parallel work on separate segments. [Balance](balance.md#project-catalogue) owns cost/work/rate. [Common site rules](build-yishuv.md) own payment, pause/cancel, arrival and validation. No city-start allowance is consumed.

Complete a corridor when all segments are built. Completed segments reduce [movement cost](military-combat.md#movement-and-orders) before the whole link is finished, without directly granting resources. A route touching a locality does not capture it. Geometry and ids survive editor moves/regeneration.

## Data touched

- Intended simulation corridor registry plus existing route geometry/evidence.
- Segment assignment, paid work and completion state; built-hex effects reference the same ids.
- Editor dependency checks cover segment/route bindings.

## Done when

- Segment boundaries use sourced waypoints rather than per-hex cost multiplication.
- Parallel work does not double-assign pioneers.
- Partial completion affects movement while objectives wait for the required whole.
- Railway cards do not substitute an out-of-window coastal line.

## Out of scope

Later coastal railway, invented alignments, per-hex upkeep and revenue merely from drawing routes.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] Haboker, 14 September 1949: the Tel Aviv-Haifa line opens to passengers on the 20th of the month, "next Tuesday", https://www.nli.org.il/he/newspapers/hbkr/1949/09/14/01/article/70 (OCR), checked 2026-10-14; confirmed by Maariv, 22 September 1949, a report from the line's first days, https://www.nli.org.il/he/newspapers/mar/1949/09/22/01/article/21 (OCR), checked 2026-10-14
- [S4] Hayom (Jerusalem), 8 December 1948: the Road of Valour was opened "yesterday", 7 December 1948, https://www.nli.org.il/he/newspapers/hayomjlm/1948/12/08/01/article/2 (OCR), checked 2026-10-14
- [S5] Owner decisions, G1 (the Burma Road in the MVP, 14 October 2026), docs/oss/DECISIONS.md
- [S6] Corridor evidence, research_notes/GDD phase 4/corridors-1948-51.md.
- [S7] Approved historical answers, docs/superpowers/plans/2026-10-15-gdd-phase-4.md.
- [S8] Catalogue, docs/gdd/balance.md.

## Open questions

Unresolved principle and authoring decisions are maintained only in [the GDD](../GDD.md#deferred-decisions).
