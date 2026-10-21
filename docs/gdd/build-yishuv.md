# Build yishuv: one designated site

## Player story

As a player of the MVP, I want to send a group of halutzim to build a new yishuv at a designated site, so that the place is founded under its historical name (GDD "Pioneer work" [S1]; decisions D-A3, D-A4, D-D6 [S2]).

## Rules

- Building is allowed only at designated sites, listed with their sources in `content/sites.yaml`. The first slice had one: Mefalsim, founded on 12 June 1949, the day of the ceremony of going onto the land [S4][S5]. The owner chose it on #378 [S3].
- Since the GDD phase of pioneer sites (#785) the MVP has six yishuv sites: Mefalsim, Beit Kama (18 April 1949), Re'im (20 October 1949), Gilat (4 December 1949), Kiryat Shmona (18 July 1949) and Beit Shemesh (15 December 1950) [S4][S6]. The owner took the plan's recommendation on #782 [S7], and set the last two days on #785 [S10]: Kiryat Shmona's is the only day two independent sources give, and Beit Shemesh's is the National Library's December 1950 under the monthly turn. The Re'im and Mefalsim records cover the founding only.
- Each site has one or more kinds: `yishuv`, `field`, `swamp`, `military_camp` or `hakhshara`. A pioneer action is accepted only at a site of its kind, so `build_yishuv` is refused at a field site (R-SITE-004). A camp site names the city it trains for. At most one `yishuv` site sits in a hex; a site of another kind may share it, as each yishuv's field site does (R-SITE-005). Plowing is in [fields-and-swamps.md](fields-and-swamps.md); draining and building camps come with later PRs of that phase.
- A site is available from the game's start. It may sit on an armistice line hex, as Mefalsim's hex does [S3].
- The map roster supplements this slice: the owner approved Kiryat Shmona and Beit Shemesh as designated founding sites on 17 October 2026 ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233524779)). The owner gave the sensitivity sign-off for them on #782 [S7], so they are among the six above. They do not replace Mefalsim or its objective. Historical founding and opening dates in the roster are reference dates, not new automatic availability gates [S8].
- Moving a location in the authoring tool must preserve its identity and reference evidence, update the authored site binding consistently, and surface dependent references before saving [S8][S9].
- `PioneerWork(unit, build_yishuv, site)` sends an idle halutz unit to build. It costs IL 2m, a placeholder paid in full at once, borrowing what the Treasury lacks (owner decision on #378 [S3]), and no Attention, because unit orders are free (D-A6). The unit works from where it stands (R-SITE-001).
- It is refused, with the state unchanged, for an unknown unit, a unit that is not a halutz or already has a project (one project per unit, D-D6), an unknown action or site, a site being built or built (one unit per site), or the legacy loop (R-SITE-001).
- `StopWork(unit)` ends the build. The site is available again; the progress and the Money paid are lost (D-A3) (R-SITE-002).
- Construction resolves after training and before production in each turn (D-A6). A build ordered this turn counts this turn: it takes 6 turns, a placeholder (R-SITE-003).
- When the build finishes, the site is marked built on the turn's end date and the unit is idle again. It stays a halutz unit until it has worked `veteran_halutz_service` turns (docs/gdd/veteran-pioneer.md). Nothing else changes yet: no city, housing, jobs, population or Social cohesion. The objective and its card come with the objective PR (R-SITE-003).

## Data touched

- `content/sites.yaml` (new; sourced facts: id, names, hex, kinds, founding date, sources, status). The loader reads it only in the GDD loop, so the legacy content hash does not move.
- `rules.yaml` `mvp.build_yishuv_turns` and `mvp.build_yishuv_cost`, and `attention_cost.pioneer_work` and `stop_work`: supplied by `MvpSlice.Overlay`, never under `content/` (ruling 2 [S3]).
- `GameState.Sites`, hashed as `site{...}` when there are some; `Treasury`.
- Save files: the `pioneer UNIT ACTION SITE` and `stop UNIT` lines. Scenarios: `pioneer_work` and `stop_work`.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `ProjectsTests`, `SitesContentTests`, `GddStateTests` and `ReferencePlayerTests`.
- `hash1950`, `hash1952`, `smoke5`, `scenario-cautious`, `scenario-reckless` and `scenario-mvp-absorb-first` do not move.

## Out of scope

- The objective, its card and the score (PR 6), the build button and the built yishuv on the map (PRs 7 and 8), movement (Phase 5), other pioneer work and other kinds of site, and more sites.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 plan for PR 5 and the owner's decisions, https://github.com/eyalgolan/statecraft-israel/issues/378, checked 2026-10-14
- [S4] Haim Gvati, 100 Years of Settlement, vol. 2, list of all Jewish rural settlements, https://benyehuda.org/read/14684, checked 2026-10-14
- [S5] Yad Ben-Zvi, "Israel Revealed to the Eye", Kibbutz Mefalsim, https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%9E%D7%A4%D7%9C%D7%A1%D7%99%D7%9D/, checked 2026-10-14
- [S6] Settlements founded in 1949-1951, rows 7, 16, 22, 23 and 25, research_notes/GDD objectives/settlements-1949-51.md
- [S7] GDD Phase 4 owner answers, https://github.com/eyalgolan/statecraft-israel/issues/782, checked 2026-10-17
- [S8] MVP location roster and historical-reference versus site-availability rules, docs/locations.md
- [S9] Internal map-authoring workflow, docs/gdd/map-editor.md
- [S10] Owner decision on the founding days of Kiryat Shmona and Beit Shemesh, with the research note, https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6237624501, checked 2026-10-17

## Open questions

- The placeholder values (6 turns, IL 2m) wait for the owner.
