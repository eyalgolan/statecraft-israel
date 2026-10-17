# Plow fields and dry swamps

## Player story

As a player of the MVP, I want to send a group of halutzim to plow the fields of a yishuv I have built, so that the land adds a little Food to the national stock every turn (GDD "Pioneer work", "Prepare agricultural land" [S1]; decision D-D10 [S2]).

## Rules

- Each of the four yishuv sites has one field site in its hex: Mefalsim, Beit Kama, Re'im and Gilat fields. They are design placements, not historical fields (`Q-FIELD-1`, the owner took the plan's recommendations on #782 [S3]).
- `PioneerWork(unit, plow_fields, site)` sends an idle halutz unit to plow a field site. It follows the build rules: one project per unit, one unit per site, paid in full at once with borrowing, and no Attention (R-FIELD-001, R-SITE-001).
- A field is ready only once the yishuv in its hex is built. Before that the order is refused with the state unchanged (R-FIELD-001).
- Plowing takes 3 turns, counting the turn of the order, and costs IL 0.5m. Both are design values the owner may override (`Q-FIELD-3` [S3]).
- When the plowing finishes, the field site is plowed on the turn's end date and the unit is idle again (R-FIELD-001).
- A plowed field needs no workers (`Q-FIELD-2` [S3]). Each turn, in the turn's sixth step, after the cities' production, it adds 1 unit of Food to the national stock, one population unit's food for a month (R-FIELD-002, `Q-FIELD-3` [S3]).
- The effect is small on purpose: the nation eats 650 to 1,400 units of Food a turn over the MVP, so one field is about 0.1% of a month's need (`Q-FIELD-3` [S3]). In a turn with imports the import only tops the stock up by less; the field shows in the stock when foreign currency is exhausted and nothing is imported.
- The reference player of the goldens plows the lowest-id ready field once every yishuv site is taken (D-A7).

## Data touched

- `rules.yaml` `mvp.plow_fields_turns`, `mvp.plow_fields_cost` and `mvp.field_food_per_turn`: labelled design values, required in the GDD loop.
- `content/sites.yaml`: the four field rows of #785, unchanged. The loader now refuses a field site with another kind.
- `GameState.Sites` (a plowed field is a built field site, so the hash format is unchanged), `GameState.Food`, `Treasury` and debt.
- No save or scenario format change: `pioneer UNIT plow_fields SITE` uses the existing line.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `FieldsTests`, `ProjectsTests` and `ReferencePlayerTests`.
- `hash1952` and `mvp-end` move and are re-pinned with history rows in `docs/determinism.md`; `hash1950`, `smoke5` and the three scenarios do not move.

## Out of scope

- The swamp half: the Hula swamp site and `dry_swamp` (#789). The site waits for the owner on #785, and a drained swamp becomes a field site there (`Q-SWAMP-1`).
- A plow button and plowed fields on the map, and fields at places other than the yishuv sites. Unity's order check (`SimBridge`) still accepts only `build_yishuv`, so the game's UI cannot send `plow_fields` until that PR adds it.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD Phase 4 plan, PR 4 and the owner questions table, docs/superpowers/plans/2026-10-15-gdd-phase-4.md
- [S3] GDD Phase 4 owner answers, https://github.com/eyalgolan/statecraft-israel/issues/782, checked 2026-10-17

## Open questions

- The design values (1 Food a turn, 3 turns, IL 0.5m) stand until the owner overrides them.
- The swamp half of this section waits for the owner's answer on the Hula site (#785).
