# Plow fields and dry swamps

## Player story

As a player of the MVP, I want to send a group of halutzim to plow the fields of a yishuv I have built, so that the land adds a little Food to the national stock every turn (GDD "Pioneer work", "Prepare agricultural land" [S1]; decision D-D10 [S2]). I also want to send them to drain the Hula swamp, so that its land becomes a field I can plow, and so that the 1951 card about the drainage works comes only when my works are there (GDD "Pioneer work", "Dry swamp" [S1]).

## Rules

- Each of the six yishuv sites has one field site in its hex: Mefalsim, Beit Kama, Re'im, Gilat, Kiryat Shmona and Beit Shemesh fields. They are design placements, not historical fields (`Q-FIELD-1`, the owner took the plan's recommendations on #782 [S3]).
- `PioneerWork(unit, plow_fields, site)` sends an idle halutz unit to plow a field site. It follows the build rules: one project per unit, one unit per site, paid in full at once with borrowing, and no Attention (R-FIELD-001, R-SITE-001).
- A field is ready only once the yishuv in its hex is built. Before that the order is refused with the state unchanged (R-FIELD-001).
- Plowing takes 3 turns, counting the turn of the order, and costs IL 0.5m. Both are design values the owner may override (`Q-FIELD-3` [S3]).
- When the plowing finishes, the field site is plowed on the turn's end date and the unit is idle again (R-FIELD-001).
- A plowed field needs no workers (`Q-FIELD-2` [S3]). Each turn, in the turn's sixth step, after the cities' production, it adds 1 unit of Food to the national stock, one population unit's food for a month (R-FIELD-002, `Q-FIELD-3` [S3]).
- The effect is small on purpose: the nation eats 650 to 1,400 units of Food a turn over the MVP, so one field is about 0.1% of a month's need (`Q-FIELD-3` [S3]). In a turn with imports the import only tops the stock up by less; the field shows in the stock when foreign currency is exhausted and nothing is imported.
- The reference player of the goldens plows the lowest-id ready field once every yishuv site is taken, before any camp site (D-A7).
- The MVP has one swamp site, the Hula, at the swamp hex `(14, 3)`, sourced to the `hula_drainage` event's sources; its name names only the drainage (`Q-SWAMP-1`, `Q-SWAMP-3`, "Allow as planned" [S4]). The Hula field site shares its hex. It is a design placement, not a historical field.
- `PioneerWork(unit, dry_swamp, site)` sends an idle halutz unit to drain a swamp site, by the same build rules (R-SWAMP-001, R-SITE-001). Draining takes 12 turns, counting the turn of the order, and costs IL 4m, twice the yishuv build: design values the owner may override (`Q-SWAMP-1` [S3]).
- Drained, the swamp site does nothing else; the Hula field site becomes ready to plow. Before that a plow order there is refused with the state unchanged (R-SWAMP-001).
- `hula_drainage` (window 7 March to 18 May 1951) carries `site: hula`, so it fires only while the player's works at the Hula are in progress: with no works, or once the Hula is drained, it does not fire (R-SWAMP-002, `Q-SWAMP-2` [S3]). Its text, options and sources are unchanged. The reference player drains no swamp, so the card does not come in the goldens' run.

## Data touched

- `rules.yaml` `mvp.plow_fields_turns`, `mvp.plow_fields_cost` and `mvp.field_food_per_turn`: labelled design values, required in the GDD loop.
- `content/sites.yaml`: the four field rows of #785, unchanged. The loader now refuses a field site that is also a yishuv site.
- `GameState.Sites` (a plowed field is a built field site, so the hash format is unchanged), `GameState.Food`, `Treasury` and debt.
- No save or scenario format change: `pioneer UNIT plow_fields SITE` and `pioneer UNIT dry_swamp SITE` use the existing line.
- `rules.yaml` `mvp.dry_swamp_turns` and `mvp.dry_swamp_cost`: labelled design values, required in the GDD loop.
- `content/sites.yaml`: the `hula` swamp site and the `hula_field` field site (status draft). The loader refuses a field site that is also a swamp site.
- `content/events/hula_drainage.yaml`: the `site:` key, which the loader checks names a swamp site (GDD loop); `content/schema/event.schema.json` and docs/WRITING_EVENTS.md list it.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `FieldsTests`, `SwampTests`, `ProjectsTests` and `ReferencePlayerTests`.
- For the fields half, `hash1950`, `hash1952` and `mvp-end` move and are re-pinned with history rows in `docs/determinism.md`; `smoke5` and the three scenarios do not move.
- For the swamp half, `hash1952` and `mvp-end` move (the Hula card no longer fires) and are re-pinned with history rows; `hash1950`, `smoke5` and the three scenarios do not move.

## Out of scope

- A plow or drain button, and plowed fields or a drained swamp on the map. Unity's order check (`SimBridge`) still accepts only `build_yishuv`, so the game's UI cannot send `plow_fields` or `dry_swamp` until that PR adds it.
- A map change on draining (the water research's option D) and any card text about the Hula's people or land: such text goes through D-F1 review.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD Phase 4 plan, PR 4 and the owner questions table, docs/superpowers/plans/2026-10-15-gdd-phase-4.md
- [S3] GDD Phase 4 owner answers, https://github.com/eyalgolan/statecraft-israel/issues/782, checked 2026-10-17
- [S4] Q-SWAMP-3, the owner's decision on the Hula, https://github.com/eyalgolan/statecraft-israel/issues/789, checked 2026-10-17

## Open questions

- The design values (1 Food a turn, 3 turns, IL 0.5m) stand until the owner overrides them.
- The swamp design values (12 turns, IL 4m) stand until the owner overrides them.
