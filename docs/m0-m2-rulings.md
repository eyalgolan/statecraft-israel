# M0–M2 decisions log (rulings)

These are the decisions made while building M0–M2 where the spec or plan was silent, wrong, or in conflict with itself. Each entry gives what was decided, why, and what it costs if the decision is wrong. The full ledger, with 83 ruling lines plus task history, was kept during the build. This digest keeps the ones that shape the game or the codebase.

## Simulation rules (M1)

- **Event effects persist.** The plan as written had a defect: an event's effect on an indicator was wiped the next turn. The fix is a two-layer model:
  - inner = clamp(base + Σ policy effects)
  - level = clamp(inner + event shift)
  - An event sets its shift to exactly the change the player sees.
  - Cost if wrong: M4 balance tuning only.
- **Hard historical anchors always happen in Historical mode.**
  - If an anchor is delegated, it resolves to its historical option.
  - Hard anchors are never dropped by the per-turn cap; only soft events are capped at 3 per turn.
  - Each hard anchor must have exactly one historical option, or the content fails to load.
- **Attention is priced at the security rung the turn started at,** which is the price the player saw.
- **A defeated Knesset vote is an outcome, not an illegal command.** It costs Attention and is reported as a cause. Before this change, a player could retry a failed vote for free until it passed. Validation failures are still refunded.
- **Budget allocations must cover exactly the budgeted ministries.** Offices without a budget line count as neutral.
- **Illegal input is rejected with a reason and never corrupts state.** This covers NaN or infinite numbers, unknown ids and malformed CLI numbers. A null command from code still throws, because it is a programmer error, not player input.
- **"Why did this happen?" is truthful.**
  - A clamped indicator reports nothing.
  - Each cause carries the visible change.
  - All text is culture-invariant, with no "-+0.00".
- **Determinism.**
  - Doubles are hashed as IEEE bits, with -0.0 normalised.
  - The golden hash eb975652… is identical on .NET and in Unity editor Mono.
  - IL2CPP and WebGL parity is checked in M3.
- **Content errors fail at load and name the file and field.** This covers:
  - null lists;
  - enum values;
  - numeric ranges;
  - event condition ranges;
  - id shapes;
  - placeholder coverage over each event's window;
  - overlapping terms of one person whose parties disagree.
- **Sensitive events** (case-insensitive tags) can never pay money, including foreign-currency reserves.
- **`living` is required for every person.** `living: false` was set only after each death date was confirmed from the cited source.
- **Deferred:**
  - delegation using loyalty: M3/M4;
  - hard/soft/emergent anchors in Open mode: M4;
  - economy calibration: M4 (the historical autoplay currently runs up debt).

## History and content

- **Real names, 1948 cabinet, pro-Israeli historians, Israeli framing; no contested narratives shown.** These are owner decisions, recorded in the spec.
- **Party is dated per term** (Offices.PartyOf). For example, Rosen is Progressive, and Shapira, Maimon and Levin are United Religious Front in 1949.
- **14 Feb – 10 Mar 1949:** the caretaker Provisional Government is measured against the First Knesset's seats.
- **Egypt armistice:** refusing it keeps the war at rung 5, while refusing the other armistices drops to rung 4.
- **Population calibration:** the 1948-11-08 registration (716,700) is an independent check. The end-1948 and end-1949 figures are consistency checks.

## Map and geography (M2)

- **Desert comes from real climatology, not a drawn line.**
  - It uses CHELSA v2.1 annual rainfall (CC0), bias-corrected with 263 Israel Meteorological Service gauges (data.gov.il), with desert below 200 mm.
  - The Köppen-Geiger map and the raw gridded data were tried first and failed the real-place checks.
  - Mar Saba is left unconstrained because its IMS gauge reads 216 mm.
  - The Judean desert band is narrower than the named region, consistent with the 200 mm definition used for the Negev.
- **The Dead Sea basin and northern Arava are desert,** not green valley.
- **Settlement markers sit at their sourced coordinates** (real geography). The sim's hex ownership of a settlement (Tiberias, for example) is a separate rule.
- **Look.**
  - Biome borders blend over about 0.6 of a hex.
  - The terrain is matte; the glossy streaks came from texture alpha.
  - Hex outlines are draped above the terrain, with terrain pixel error 1. The cost of that setting is to be profiled on WebGL in M3.
  - Labels are near-white with a shadow and readable in the 1080p flythrough. Top-down labels are small; that image is a check, not a deliverable.
- **Unity rules** (M0 spike findings):
  - no runtime Shader.Find (materials are editor-made assets);
  - post-processing is on;
  - IL2CPP;
  - new Input System only;
  - capture tools never rewrite Map.unity.
- **Licences:**
  - the data.gov.il licence is "Other (Open)" with no URL, so it gets a legal review before release (M5);
  - the Copernicus, CHELSA and IMS attribution travels with the map data.

## Process

- **M1 sim and M2a tools ran as parallel tracks** in separate worktrees. Task 25 also ran in parallel with Task 24's data fixes. Every task still got its own implementer and reviewer.
- **The owner's "no known gap carried forward" rule overrode the default of a single fix wave.** The branch went through three whole-branch fix waves and up to five fix rounds per task.
- **Commit trailers name the model that actually wrote the code.**
- **Task 3 (Unity MCP via Unity Cloud) is NOT verified.**
  - The session's permission policy blocked the Cloud steps, and the controller did not work around it.
  - The owner steps are documented in docs/spikes/m0-results.md.
  - Batchmode plus scripts is the proven fallback.
