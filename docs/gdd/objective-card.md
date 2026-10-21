# Objectives: the completion card and the score

## Player story

As a player of the MVP, I want completing an objective to show me the real event it stands for and score me for finishing it, with more points the earlier I finish than history did, so that racing the historical record is worth it (GDD "Historical events on objective completion" [S1]; decisions D-B2, D-B6 [S2]).

## Rules

- The third objective, "Establish key infrastructure", has three parts: the Haifa–Tel Aviv rail link, the Burma Road and all six designated yishuvim [S9]. A corridor part completes when every segment is built; the yishuvim part completes when every listed site is built (R-OBJ-005). Each part records separately, earns its own timing bonus and fires its own card. The objective's 100 points (unitless) of base score are paid once, with its final part (R-OBJ-006).
- The parts' reference dates are 20 September 1949 for restored Haifa–Tel Aviv passenger service, 7 December 1948 for the Road of Valour's opening and 15 December 1950 for the last required founding, Beit Shemesh [S9]. Each part carries the sources for its date. The three cards remain `draft` until a person reads the cited newspaper and site sources [S10].
- The later Nir Am infrastructure-protection ruling is also part of the third objective, but its points remain an unset balance value [S11]. It is not silently added as a fourth build part to the decided three-part score.
- The "absorb immigrants" objective (D-B4) completes once the Jewish population has reached a stated number, and then the cities' overflow is at most a stated share of the people they house and their employed at least a stated share of their civilians (owner, 18 October 2026, option (a) [S8]; R-OBJ-004). The population gate keeps the start values from completing it on the first turn; refugees waiting do not count, since each turn's arrivals come after housing. Its numbers, reference date and card are content (`objectives.yaml` and its card), added with a person's source check [S8].
- An objective is recorded once, in the last step of the GDD turn, with the turn's end date, and only in the GDD loop (R-OBJ-001).
- Completing a standalone objective scores 100 points (unitless) once. A multipart objective pays the same base once, when its last part completes (D-B6 [S2], R-OBJ-002, R-OBJ-006).
- Each calendar month (unitless) between the completion's month and the reference date's month adds 5 points (unitless), up to 60 points (unitless) (D-B2 [S2], R-OBJ-002). The same month is on time and earns no bonus; finishing late costs nothing. Calendar months match the owner's card wording [S6], and the owner confirmed them on 15 October 2026 [S7].
- The existing card's line reads "n months early", "Completed on time, as in history." in the same month, or "Completed n months later than in history." when late, in English and Hebrew (owner, 14 October 2026 [S6]). `Objectives.MonthsFromHistory` gives n in months: positive early, zero on time, negative late.
- The objective's card fires once, at the end of the turn the objective is completed in, so the player sees it at the start of the next turn. It never fires in the legacy loop (R-OBJ-003).
- A card has exactly one option. If the player leaves it unanswered, it resolves to that option without drawing from the RNG (R-OBJ-003).
- Objectives and score are the last step of the GDD turn, in the order the owner approved (D-A6 [S2][S3]).

## Data touched

- `content/objectives.yaml`: standalone conditions plus multipart `part_of`, `corridor_complete` and `sites_built` records. The infrastructure parts are `railway`, `burma_road` and `yishuvim`; `first_new_yishuv` is now the yishuvim part's card, not a separate scored objective.
- `content/events/railway_complete.yaml`, `burma_road_complete.yaml` and `first_new_yishuv.yaml`: the three sourced completion cards, kept `draft` pending the required human source check [S10].
- An event's `objective` key (`content/schema/event.schema.json`, `docs/WRITING_EVENTS.md`).
- `rules.yaml` `mvp.objective_points`, `objective_bonus_per_month`, `objective_bonus_cap`, required in the GDD loop, as design values (R-LOOP-005). The score reads the rules of the date the turn starts from.
- `GameState`: `Objectives` (hashed as `obj{...}`) and `Score` (hashed as `score=`), both only when set (R-LOOP-002).

## Done when

- `dotnet test sim/Statecraft.sln` passes, including the R-OBJ-005 and R-OBJ-006 cases in `ObjectivesTests`.
- `hash1952` and `mvp-end` are re-pinned: the reference player completes the yishuvim part when Re'im, the last required site, is built on 15 July 1951. The old standalone objective's 100 points are removed; neither corridor completes, so the shared base is not paid. `hash1950`, `smoke5` and the scenarios do not move [S9].

## Out of scope

- Nir Am's separate protection mechanic and unset points [S11]; Unity objective UI (#792); the score on the HUD is already in (#598).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 6, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] Yad Ben-Zvi, "Israel Revealed to the Eye", Kibbutz Mefalsim, <https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%9E%D7%A4%D7%9C%D7%A1%D7%99%D7%9D/>, checked 2026-10-15
- [S5] Haim Gvati, 100 Years of Settlement, vol. 2, Ben-Yehuda Project edition, <https://benyehuda.org/read/14684>, checked 2026-10-15
- [S6] Owner decision on the card wording, <https://github.com/eyalgolan/statecraft-israel/issues/379>, checked 2026-10-15
- [S7] Owner decision on calendar months, <https://github.com/eyalgolan/statecraft-israel/pull/653#issuecomment-6197448493>, checked 2026-10-15
- [S8] Owner decision on the absorb condition and card, <https://github.com/eyalgolan/statecraft-israel/issues/738>, checked 2026-10-18
- [S9] Phase 4 objective decisions and implementation plan, <https://github.com/eyalgolan/statecraft-israel/issues/782>, checked 2026-10-17; `docs/superpowers/plans/2026-10-15-gdd-phase-4.md`, PR 7
- [S10] Objective 3 implementation issue and photograph ruling, <https://github.com/eyalgolan/statecraft-israel/issues/791>, checked 2026-10-18
- [S11] Nir Am infrastructure-protection ruling, <https://github.com/eyalgolan/statecraft-israel/issues/1006>, checked 2026-10-18

## Open questions

The three cards remain drafts pending a person’s source checks. The selected Burma Road photograph and card still require the owner’s sensitivity review [S10]. No additional scoring value is selected here.
