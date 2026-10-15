# Objectives: the completion card and the score

## Player story

As a player of the MVP, I want completing an objective to show me the real event it stands for and score me for finishing it, with more points the earlier I finish than history did, so that racing the historical record is worth it (GDD "Historical events on objective completion" [S1]; decisions D-B2, D-B6 [S2]).

## Rules

- The first objective, the first new yishuv, completes when Mefalsim is built (Build yishuv, R-SITE-001 to R-SITE-003). Its reference date is Mefalsim's founding, 12 June 1949 [S4][S5], and the loader refuses any other (R-OBJ-001).
- An objective is recorded once, in the last step of the GDD turn, with the turn's end date, and only in the GDD loop (R-OBJ-001).
- Completing an objective scores 100 points (unitless) once (D-B6 [S2], R-OBJ-002).
- Each calendar month (unitless) between the completion's month and the reference date's month adds 5 points (unitless), up to 60 points (unitless) (D-B2 [S2], R-OBJ-002). The same month is on time and earns no bonus; finishing late costs nothing. Calendar months match the owner's card wording [S6], and the owner confirmed them on 15 October 2026 [S7].
- The card's line, in a later Unity PR, reads "n months early", "Completed on time, as in history." in the same month, or "Completed n months later than in history." when late, in English and Hebrew (owner, 14 October 2026 [S6]). `Objectives.MonthsFromHistory` gives n in months: positive early, zero on time, negative late.
- The objective's card fires once, at the end of the turn the objective is completed in, so the player sees it at the start of the next turn. It never fires in the legacy loop (R-OBJ-003).
- A card has exactly one option. If the player leaves it unanswered, it resolves to that option without drawing from the RNG (R-OBJ-003).
- Objectives and score are the last step of the GDD turn, in the order the owner approved (D-A6 [S2][S3]).

## Data touched

- `content/objectives.yaml` (`id`, `name`, `completes_when.site_built`, `reference_date`, `card`), read in both loops when present: `first_new_yishuv` on Mefalsim.
- `content/events/first_new_yishuv.yaml`, the card: two sentences from the site's sources [S4][S5], `status: fact-checked`.
- An event's `objective` key (`content/schema/event.schema.json`, `docs/WRITING_EVENTS.md`).
- `rules.yaml` `mvp.objective_points`, `objective_bonus_per_month`, `objective_bonus_cap`, required in the GDD loop, as design values (R-LOOP-005). The score reads the rules of the date the turn starts from.
- `GameState`: `Objectives` (hashed as `obj{...}`) and `Score` (hashed as `score=`), both only when set (R-LOOP-002).

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `ObjectivesTests`.
- `hash1950` moves to `ad806776`, `hash1952` and `mvp-end` to `0f54dc51`: the reference player builds Mefalsim in the month of its founding, so the objective completes on time for 100 points and its card fires and is answered (docs/determinism.md). `smoke5`, `scenario-cautious`, `scenario-reckless` and `scenario-mvp-absorb-first` do not move.

## Out of scope

- The war, absorption and railway objectives (Phase 2 and later); the card's photograph and its "completed on, historically" line in English and Hebrew (PR 7); the score on the HUD is already in (#598).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 6, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] Yad Ben-Zvi, "Israel Revealed to the Eye", Kibbutz Mefalsim, <https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%9E%D7%A4%D7%9C%D7%A1%D7%99%D7%9D/>, checked 2026-10-15
- [S5] Haim Gvati, 100 Years of Settlement, vol. 2, Ben-Yehuda Project edition, <https://benyehuda.org/read/14684>, checked 2026-10-15
- [S6] Owner decision on the card wording, <https://github.com/eyalgolan/statecraft-israel/issues/379>, checked 2026-10-15
- [S7] Owner decision on calendar months, <https://github.com/eyalgolan/statecraft-israel/pull/653#issuecomment-6197448493>, checked 2026-10-15

## Open questions

- At most 3 soft events fire in a turn. In a busy month a soft card could wait a turn. Should a card be exempt from that cap? (The card's `from: 1948-05-15` sorts it ahead of every later-dated soft event, and in the golden run it fires on its own turn; the reviewer suggests leaving the cap alone for now.)
- Whether the card should show at the start of the turn after completion (built here) or one turn later.
- Whether the bonus is the same for every objective (built here, as the decisions read) or set per objective.
