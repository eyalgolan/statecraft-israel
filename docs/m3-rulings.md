# M3 decisions log (rulings)

These are the decisions made while building M3 where the spec or plan was silent, wrong, or in conflict with itself. Each entry gives what was decided, why, and what it costs if the decision is wrong. The full ledger (`.superpowers/sdd/2026-10-10-statecraft-m3/progress.md`) was kept during the build. This digest keeps the ones that shape the game or the codebase and drops task bookkeeping. Hash prefixes quoted here are the values recorded in the ledger at the time. The code and the golden tests are authoritative.

## Simulation and determinism

- **Golden hashes are re-recorded at integration, not per task.**
  - Event content, Cause text fixes and sim features each moved the hashes. Implementers recorded the observed value with the reason.
  - The plan's hash literals went stale after the Task 4 and Task 5 fix rounds and were superseded.
  - Hashes were re-recorded once after the final merge. The golden tests pass on the merged tree, with 1950 = 71e9904d… and 1952 = 73590e32… in code.
  - Cost if wrong: one extra hash-recording commit.
- **Appointment and map-action state is part of the hash.** Appointment fields moved the hash in Task 6. A map-action counter moved it again in Task 8. A stale counter in older saves is accepted, because replay reproduces the identical sequence and determinism holds.
- **Map actions are capped at 2 per turn, counted explicitly.**
  - The first version derived the count from surviving placements. A camp-to-housing replacement on the same hex bypassed it.
  - The cap is now an accepted-actions counter in state, Clone, Hash and the turn reset.
  - Specific refusal reasons are checked before the cap.
  - Cost if wrong: an M4 balance change. Upkeep and capacity balance is a Deferred M4 row.
- **Appointments are legal-only.**
  - The President and the serving Chief of Staff cannot be appointed to other offices.
  - An appointee who dies leaves office on their `died` date.
  - Holding two offices is allowed by design, because it is historical (Ben-Gurion as PM and Defence, Shapira with three portfolios).
  - Cost if wrong: a rule tweak.
- **Economy calibration moved from M4 into M3.** The model has no fully independent economy anchor, so every anchor is labelled consistency or independent.
  - One loose independent check was added. The 1950–51 external debt rise of 65.7 (Michaely) is compared against donations and loans net of the fx change. It passed at 2.19x inside a 1–3x band.
  - The first fx crisis was knife-edge. The valid range of `capital_inflow_per_year` was about 0.12 wide, because the model lacked the roughly $45m short-term credit maturing Oct–Dec 1951 (FRUS d513).
  - The fix adds -45 fx on both forex options, labelled a game assumption. The anchor is rewritten to FRUS facts, and the cap is re-fit to 200 (valid window 193.776–206.620, about 100x wider).
  - Tolerances were never loosened.
  - Cost if wrong: one more parameter re-fit. The 1952 recovery (NEP) is a Deferred M4 row.
- **Event preconditions.** A minimal `requires {event, option}` precondition was added to the sim so event chains cannot contradict each other. Cost if wrong: a small sim feature to revise.
- **Event cap and gaps.** The historical run may have at most a 4-turn gap with no events. A non-historical 7-turn gap is accepted, because the guard covers the historical run only. Cost: one quiet stretch off the historical path.
- **Dated office terms replace "vacant" when a source says an office ended.**
  - Immigration and War Victims end on 1951-10-08, because the Third Government has no such portfolio. No event text after that date may reference them.
  - The rationing minister is the minister responsible for rationing: Yosef from 1949-04-26, Lavon from 1950-11-01 (cited), then vacant from 1951-10-08.
  - The vacant stretch is a Deferred research row. Q4 1951 rationing therefore runs at vacant competence.
  - Cost if wrong: a content tweak or an office re-added.
- **Shared rules are public sim helpers.** `ClampRung`, `MinMobilization`, `EffectiveMobilization` and `CeasefireWeeksAfter` are used by `RaiseToFloor`, `HistoricalAI` and the Defence presenter. `EffectsSystem.BudgetFactor` has an overload for the Budget screen. UI code does not re-implement a sim rule. Cost if wrong: duplicated rules drift apart.

## Content and history

- **Pro-Israeli framing, real names, nothing contested shown.** Lydda and Ramle appear as military events only. This follows the owner's earlier "no contested narratives shown" decision. Cost if wrong: one text edit.
- **Conflicting dates follow the majority or primary source.** The other date goes in a comment. Examples: the first truce ends 9 July (UN), Operation Horev is 22 December, and the RAF clash is 7 January. Weizmann's term starts 17 February 1949, although the MFA says 16. His second term starts 19 November 1951 (the election date), with a comment on the 25 November swearing-in. Maimon runs to 1951-02-14, the latest possible date, with an honest comment.
- **No invented alternatives.** Where no advocate is sourced, the alternative option is labelled a game assumption in a comment.
- **The 1948 war period has sourced events (15 events after review).**
  - The playtest found no events between 15 May 1948 and 24 February 1949, about 40 turns.
  - The census is folded into the elections event.
  - Magic Carpet and the Etzion POWs are outside the window.
  - `jerusalem_military_government` was added to close a 64-day gap.
  - Hebron, Dayan-el-Tell and Rhodes are included per review.
- **Historical choices have halved security and stability deltas.** Alternatives are halved too, which keeps the shape of each trade-off. The elections event has a single option.
- **Sensitive tags stay honest.** The war-casualties and terror tags were kept, because they already sit in the loader's sensitive list. The sensitivity review of 7 events is an owner step, not a self-approval.
- **Event timing is fitted to the facts, not to the plan.** For example, `hula_drainage` waits for 7 March 1951, and the forex event waits for 1951-10-19. The Bonds window starts at the 10 May launch. Clothing rationing has no food effects. Education concede costs morale 0.03, because the model has no coalition target. Reparations refuse has no penalty (no moral judgement). Maabarot text says "transit camps" before 21 April 1950. Event effects on coalition or support are a Deferred M4 row.
- **Dated budget ownership is deferred.** The Immigration budget stays on the PM from game start, not Shapira. Cost if wrong: absorption spending is scaled by the PM, not Shapira, for 1948–51. Update (#108): dated budget ownership landed; see `docs/determinism.md`.

## UI and input

- **Look follows the 1936 concept.** Screens: HUD, Cabinet, Budget, Event, Defence, Why. Diplomacy and legacy are M4. There are no tile yields, because the concept image shows them but they are out.
- **Every player-facing string uses content display names, never ids.**
  - Cause text uses region and effect display names. The Why panel heading is "Ongoing effects".
  - A guard test scans all Cause messages from the 1948→1952 historical run for whole-word ids, including single-word ids, player commands and refusals.
  - A second guard counts raw `new Cause(` against the literals.
  - There is no silent id fallback.
- **One formatting rule each for dates, money and scale.**
  - Dates read "15 May 1948" through one formatter.
  - Money carries a unit (IL or $) from the content money list, with one sign style ("-IL 3.0m") for values and changes.
  - Indicators use one 0–100 display scale through one helper.
  - Vacant offices read "(vacant)".
  - Cause lines are sentence-case.
  - Debug coordinates are removed from player text.
- **Defense/Defence spelling is kept as is.** The office title is "Minister of Defense", an official proper noun. The UI uses British "Defence". A content comment explains this so it is not "fixed" later.
- **Pointer input is gated over UI; keyboard is not.** `CameraController` gates only the wheel and drag when over UI. Keyboard pan and orbit always work.
- **Click-through root cause.**
  - The Input System merges mouse events within a frame, so two quick clicks were lost or mis-routed.
  - The fix turns merging off and queues map presses per press, so each physical click toggles exactly once.
  - Cost if wrong: the toggle bug returns at low frame rates.
- **Screens are hidden until the game is ready.**
  - Screens, the event card and the top-bar values stay hidden until `GameRoot` is ready, via a USS default and a loading state. The placeholder "DRAFT" can never show.
  - A boot failure surfaces as a failure, not a hung loading line.
  - Boot phases are logged under `?bootlog`.
- **Layout is tested at real aspect ratios.** Tests cover 1531×784, 1366×657, 1568×752, 1920×1080 and 2560×1080 (plus 4:3). Event option text wraps inside its button, and the card sizes to fit its longest real event without scrolling. The Cabinet list scrolls inside its panel. Cost if wrong: clipped UI on unusual windows.
- **Map presentation.**
  - The camera background and a sea plane match the sea colour, with no grey void.
  - An edge skirt keeps neighbouring land as land and sea as sea, and shows below-sea-level land (Dead Sea, Jordan valley, Arava).
  - The skirt falls off softly at the Jordanian plateau edge. That is cosmetic and owned by visual-v1.
  - The selected hex has a gold draped outline.
  - Portraits have a per-image focus point.
  - Draft portraits are downsized to 512 px for repo size.

## Saves

- **A save is a replay of recorded commands, validated hard.**
  - Recorded turns are copied, so a caller mutating a budget dictionary cannot corrupt a save.
  - Null ids, lone surrogates and bad numbers are refused.
  - Hostile input (open mode, 400k turns) raises `SaveException`.
  - A foreign `Command` subclass throws instead of being silently dropped, and a reflection test guards new command types.
  - The content hash and save text are strict UTF-8 with no BOM.
  - Cost if wrong: a damaged save is reported as a determinism bug.
- **An unreadable autosave never stops the game starting.** `GameStart.Begin` catches all exceptions, sets the file aside and shows a privacy-safe note. A save made with old content continues, with a content-change warning.
- **Autosave writes are crash-safe.** A leftover `.tmp` is recovered. New Game confirmation is disarmed when its prompt changes. Save failure toasts do not replace refusal messages. The smoke session never touches the player's real autosave.
- **WebGL saves live in `/idbfs/statecraft-saves`.** Real browser persistence was verified by simulation only (case c simulated). The cross-upload test on a hosted build is deferred to M4.

## WebGL and release

- **Golden check in the browser.** The WebGL player runs the golden hashes via a URL flag, driven by `tools/web_check.py` with a cached Chrome for Testing, because the Playwright npm package was absent. URL flags match `[?&]flag\b`, so a path like "/golden-project/" does not trigger them. The desktop equivalent is `-golden`, with `-smoke`, and `-shareable` is a gate.
  - No `-ffp-contract=off` was needed.
  - The Mac IL2CPP player prints the same golden lines.
  - The brotli build is checked with web_check at release.
  - Cost if wrong: WebGL parity regressions go unnoticed.
- **Content ships through ContentSync.** Content goes to git-ignored StreamingAssets, verified against the manifest. WebGL fetch uses a 60 s wall-clock wait in tests, never a frame count. The original 600-frame wait was flaky.
- **Build safety.**
  - Settings changes are staged before any build, because `git checkout --` restores from the index and would discard unstaged settings.
  - Build logs use the `unity-*.log` names that are already git-ignored.
  - Tasks stage explicit paths, never `git add -A`, and Unity `.meta` files are staged.
  - A failing build always restores settings in a `finally` and exits non-zero.
  - Cost if wrong: lost settings or committed junk.
- **Shaders.** GLES3-only stripping is used for WebGL. Real-GPU Chrome logs the URP utility shaders as unsupported, but rendering is correct. The fix strips them for WebGL only, and the web console is clean.
- **Release is a local browser build in M3.** itch.io hosting is deferred to the start of M4 as a private Draft. The 20 MB zip also exceeds the browser tool's upload limit. See the owner rulings.
- **Warnings are errors in practice.** The tree is held at 0 warnings. Test files use `#nullable enable`, the repo convention. Controller re-checks found warnings that implementers reported as 0.

## Process and review

- **An independent opus review replaced owner review of the plan,** because the owner was asleep and pre-authorised continuing. All required and recommended changes were adopted as `plan-amendments-1`. Cost if wrong: the owner may want plan changes. The branch stayed unmerged until the final review.
- **Parallel tracks.** Track A (sim/content), Track B (UI), Track C (platform) and Track D (integration) ran in separate worktrees. A file-ownership table was verified by the plan review. After merge points the controller re-ran every suite. Task 23 ran as a fresh implementer with a binding carry list.
- **Fix rounds close everything, with no carried gaps.** This continues the M0–M2 rule. Cap: 5 rounds per task. Optional follow-ups still get a round. Items that belong elsewhere go into the carry lists or a Deferred row. Residual minors were batched into a final round.
- **Standing delta rule.** "Replace the whole of X" blocks in the plan predate fix rounds. Implementers apply only the intended delta onto the current file and diff to confirm no fix-round change is reverted.
- **Reviewers perform live verification.** Rules 1–2 of the standing rules are enforced at review. The Task 2 review verified every death date live because the implementer skipped it. The Task 3 review verified independence of its anchors. Mutation checks confirm that tests fail when the guarded code is broken. Reviewers are read-only. One reviewer checked out a commit in a worktree, and the controller verified the HEAD was restored.
- **Playtests are first-class.** The controller played real-GPU Chrome builds twice. Findings are binding, are fixed on m3 and are re-verified. Playtest 1 used a separate m3-fix worktree. Playtest 2 fixes went straight onto m3, because integration had no parallel writers.
- **Commit trailers name the model that actually wrote the code.** The M0–M2 ruling stands, and plan text saying "Opus" is overridden.
- **Visual-v1 plan review (opus) said "needs revision."** C1/C2 and I1–I10 were all adopted. Its rulings: a script-generated terrain shader with a hard import gate, labels in UI Toolkit, haze in the shaders rather than URP fog, and checkpoint PNGs taken from the WebGL player.
- **A final whole-branch review (opus) found it ready to merge once conditions were met.** One final fix wave closed I1–I5 and M1–M10: stale brotli and Mac golden evidence, the itch deferral in the release doc, the sensitivity table and mechanism, boot failure handling, the loading background, and Cause-text docs.

## Owner rulings (binding)

- **No upload of anything below promo quality.** itch.io hosting and the cross-upload save test are deferred to the start of M4 as a private Draft. M3 ships as the local browser build. Cost if wrong: an itch save-path problem is found in M4 instead of M3 (the fallback is documented).
- **itch.io upload stays an owner step.** The auto-mode classifier denied further itch.io actions ("Create Public Surface") and the controller did not work around it.
- **Promo-quality visuals are the priority after the M3 merge.**
  - Map scale is Civ-style symbolic: settlements spread over neighbouring hexes by population, buildings and trees about 10–25x real size, and a closer hero zoom. Geography stays real.
    - **Superseded on 2026-10-11** (owner ruling during the art spike): buildings and trees match promo 03's measured scale per class, about 60x real size for city blocks and about 100x for houses and trees (`KitScale` in `unity/Assets/Scripts/Map/Kits/SettlementKits.cs`; measurements in `docs/visual-v1.md`, "Art spike"). Settlements still spread over hexes by population, and geography stays real.
  - Match the promo skyline where the promo shows it (for example, 1936 Tel Aviv towers).
  - Order: visual quick wins first (lighting, shadows, tilt-shift, terrain shader with baked AO, sea, colour grade; about 1–2 weeks), then the settlements and vegetation art track (research estimate 17–23 weeks).
  - Owner art checkpoints are screenshot comparisons against `promo/final/03-gameplay-1936.png`. Work continues past a checkpoint and owner feedback arrives as fix rounds. Cost if wrong: art rework.
- **Open source.** Statecraft becomes an open-source project, built with the superpowers workflow and the no_human lessons (README, merge policies).
  - The README hero image is `promo/final/03-gameplay-1936.png`, captioned as AI-generated concept art and target look, with real build screenshots below. It shows tile yields, which are out. Provenance and licence go in the notices.
  - No history scrubbing, rewriting or history enforcement. A read-only secret audit is kept for information, and any real secret is handled by key rotation (an owner step) before publishing.
  - The repo should be public, published after the full-history secret audit is clean and LICENSE and README land, following the M3 merge. No git remote exists yet. `gh repo create` is attempted first, with an owner command as the fallback. Repo creation, going public and branch protection stay owner steps.
- **Sensitivity sign-off is the owner's.** The `-shareable` gate refuses until the owner signs off the 7 sensitive events. Nothing self-approves.
- **Earlier owner decisions carried into M3:** real names, 1948 cabinet, pro-Israeli historians and framing, no contested narratives, and the "no known gap carried forward" rule.
