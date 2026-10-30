# Objectives, completion cards and score

## Player story

I see clear objectives, receive sourced completion cards and earn a bounded bonus for beating their historical reference dates.

## Rules

### Core objective groups

There are three core groups (unitless): armistice survival, immigrant absorption, and infrastructure/yishuvim. Reaching the end does not require all groups. Region loss does not cancel awarded results. [GDD scope](../GDD.md) owns campaign boundary and deferred final defeat.

Infrastructure has `railway`, `burma_road` and `yishuvim` parts, each with a card/timing bonus. Grant `objective.base_points` only once when all parts complete. Tel Aviv–Netanya remains buildable without silently adding another required part/base-score group. Migrate `first_new_yishuv` into `yishuvim` without duplicate awards.

Required sites are **Mefalsim, Beit Kama, Re'im, Gilat, Kiryat Shmona and Beit Shemesh**, per the later owner ruling [S8]. Complete when all required site ids are built. This list defines objective membership; [locations](../locations.md) owns their full records. The retained reference for the last required founding is Beit Shemesh on 15 December 1950; its second-source gate and Kiryat Shmona's conflicting exact day must be resolved before release [S8][S9]. No fabricated day or upgraded verification is implied.

Railway completes after every authored buildable segment; reference is restored Haifa–Tel Aviv service on 20 September 1949 [S10]. Burma completes after all `burma_road` segments, with 7 December 1948 as the separate Road of Valour card reference [S11]. [Corridors](corridors.md) owns alignments.

Absorption needs represented own population above `absorption.population_target`, no refugee awaiting a legal bed in port/transit queues, ordinary-housing overflow no higher than `absorption.max_overflow`, and free-civilian employment no lower than `absorption.min_employment`. A legally housed reception refugee is not waiting for housing or eligible civilian work. Overflow denominator is ordinary resident demand; employment denominator is all free civilians. Empty denominators mean no overflow/full employment. Productive jobs, farming, pioneer work, building and teaching count; courses/idle/travel do not. This adapts the approved goal to reception without requiring every newly arriving refugee to have finished its wait. Reference remains 20 December 1949, the selected celebration date [S12].

War completes when the required armistices are accepted. Fronts are Egypt, Lebanon, Syria and Jordan including the Iraqi sector; ALA/local forces do not add a front [S13]. Reference is Syria's 20 July 1949 agreement [S13]. [Combat](military-combat.md#enemy-actions-and-truces) owns quiet-front/truce rules.

### Scoring and cards

Check completion once at resolved turn end and record its end date. Each core group pays `objective.base_points` once. Early calendar-month difference times `objective.early_per_month` is capped at `objective.early_cap` per dated group/part. Infrastructure parts each receive their own bounded bonus but share the single group base award. Same reference month means on time; late completion has no early bonus or late penalty.

Queue each completion card once for next planning phase. It has one option; unanswered cards autoresolve without a new RNG draw. Show completion/reference dates and months early/on time/late. Simultaneous cards use stable ids. Kills, suffering, merely capturing prisoners and a place marker alone do not score; [combat](military-combat.md#prisoners) owns the separate return bonus.

Retain cleared imagery only with correct licence/attribution/caption scope. Generic railway art is not labelled opening-day photography. Yishuvim may keep the cleared Mefalsim image with group text. Without approved imagery use text only; factual/sensitivity gates remain.

## Data touched

- Objective group/part predicates, reference/card ids, bonuses and one-time state.
- Migrate the old single-site objective/save result rather than parallel scoring.
- Verify references and licences in content metadata, without approval by this edit.

## Done when

- Membership matches the latest owner ruling and unverified founding references remain gated.
- Infrastructure has part cards/bonuses and only one base award.
- Legal reception housing does not block absorption, while a port bed queue does.
- Scoring/cards/save reload are deterministic and cannot double-pay.
- Campaign ending remains possible with incomplete objectives.

## Out of scope

Kill points, extra first-founding base awards, invented dates, date-triggered automatic completion and treating every marker as a scored target.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 6, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] Yad Ben-Zvi, "Israel Revealed to the Eye", Kibbutz Mefalsim, <https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%9E%D7%A4%D7%9C%D7%A1%D7%99%D7%9D/>, checked 2026-10-15
- [S5] Haim Gvati, 100 Years of Settlement, vol. 2, Ben-Yehuda Project edition, <https://benyehuda.org/read/14684>, checked 2026-10-15
- [S6] Owner decision on the card wording, <https://github.com/eyalgolan/statecraft-israel/issues/379>, checked 2026-10-15
- [S7] Owner decision on calendar months, <https://github.com/eyalgolan/statecraft-israel/pull/653#issuecomment-6197448493>, checked 2026-10-15
- [S8] Required sites/sensitivity approval, https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6233776625, checked 2026-10-17; recorded in docs/superpowers/plans/2026-10-15-gdd-phase-4.md.
- [S9] Founding evidence, research_notes/GDD objectives/settlements-1949-51.md.
- [S10] Railway evidence, docs/gdd/corridors.md, sources S3.
- [S11] Road of Valour evidence, docs/gdd/corridors.md, sources S4.
- [S12] JTA reference research/selection, research_notes/GDD objectives/objective-cards.md; docs/superpowers/plans/2026-10-15-gdd-phase-3.md, historical Q-ABS-1.
- [S13] Parties/dates, content/map/armistice_hexes_1949.yaml; research_notes/M4 war mode/war-1948.md.
- [S14] Catalogue and later decisions, docs/gdd/balance.md; docs/GDD.md.

## Open questions

Unresolved principle and authoring decisions are maintained only in [the GDD](../GDD.md#deferred-decisions).
