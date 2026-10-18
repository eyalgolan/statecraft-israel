# Alternate timelines: follow-up cards for the roads not taken. Design spec and implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **The owner reviewed this plan and accepted every recommendation (17 October 2026, #945).** Every task names the questions it rests on; Task 0 recorded the answers. Agents draft and the owner edits: nothing is built until it is signed off (the owner's process for #783, 17 October 2026, which #939 adopts).

**Goal:** When the player picks an option that did not happen, the game brings the sourced follow-up dilemmas that contemporaries expected from that road, bends the later historical cards to fit, and folds the story back into the record at the real outside events.

**Architecture:** No new game state. The divergence is derived from `GameState.ChosenOptions` (already hashed) and each event's `historical` option. Content gains four small keys: `requires` becomes a list (AND) whose entries can say `diverged: true`; options carry an `alternative` record (who proposed it, on what source); a `consequence` event kind for cards that exist only on an alternative road, with a Ferguson gate in the loader; `rejoin` on a consequence option for foldback; and `variants` (alternative text) on `reconvergence` events. A pure `Timeline` helper feeds the card banner and a two-column timeline screen. The historical run never meets any of it, so the goldens do not move until a content slice adds a card a scripted run reaches.

**Tech stack:** as Phases 1 to 7: C# netstandard2.1 sim (`sim/Statecraft.Sim`), xUnit on net10.0 (`sim/Statecraft.Sim.Tests`), YamlDotNet 18.1.0 content, Unity 6000.3.25f1 UI Toolkit (`unity/`), Python tools and policy checks under `uv`.

**Spec:** Part A of this file. Design of record: [docs/GDD.md](../../GDD.md) ("Events and consequences": "Game balance values and hypothetical alternatives must be identified as design assumptions", `GDD.md:195`; owner ruling 1, "Deport outcome: honest cost, no reward", `GDD.md:237`). Policy: [CONTENT_POLICY.md](../../CONTENT_POLICY.md) §1, §2 and §4. Event contract: [WRITING_EVENTS.md](../../WRITING_EVENTS.md). Rules: [docs/rules/mvp.md](../../rules/mvp.md). Determinism: [docs/determinism.md](../../determinism.md). The format follows [the Phase 5 plan](2026-10-15-gdd-phase-5.md) and [the Phase 7 plan](2026-10-15-gdd-phase-7.md). Issues: #939 (scope widened to this mechanism by the owner, 17 October 2026), #937 and #938 (related fixes, already claimed).

**The owner's request, verbatim (17 October 2026):** "we also need followup cards - if the player chose an option that didnt happen in real life - we need followup cards for dilemas and events that didnt happen. this is very delecate as we need a mechanisem that would take the historical reality that was at the original timeline, and bend it per the user's choises - to create the events and dilemas they would face in the alternative timeline they got into. create an in depth plan using the superpowers and do a deep research online as to how games handle this types of challenges. then create issues to fix this gap in the game"

**Status (17 October 2026):** the owner accepted every recommendation for Q-ALT-1 to Q-ALT-15 ("Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)). The answers are recorded in Part A §A.5. Q-ALT-4 is (a), so the optional relative-windows task is dropped. Tasks map to issues #945 to #958 and #939 (the task list).

## Global Constraints

- **Span and turns:** monthly turns on the 15th, 15 May 1948 to 15 January 1952, 44 turns (GDD "MVP scope"; R-LOOP-004). A consequence card's window lies inside that span.
- **The GDD wins** over every older spec (`AGENTS.md`). Anything it marks Open, Proposal or TBD needs an owner decision before it is built. No task decides an owner question; Task 0 records the answers.
- **Determinism** (`docs/determinism.md`): ordinal ordering for every id comparison and sort (`StringComparer.Ordinal`); no clock; no new RNG and no new draws; no floats in any new rule decision (depth, ordering and matching are integers, booleans and ordinal strings); no unordered iteration into state.
- **State and saves:** this plan adds no `GameState` field and no command in Tasks 1 to 14, so the save format and `SaveGame.SchemaVersion` (2, `SaveGame.cs:47`) stay. Any `GameState` or save change bumps the save version and re-pins the goldens; none is planned (Q-ALT-4 is (a)).
- **Goldens:** re-pinned only from a real `STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln` run, followed by a plain run, with a history row in `docs/determinism.md` and the reason in the commit and the PR. Never typed by hand. Golden-moving PRs merge one at a time, each rebased on the last.
- **Sources (CONTENT_POLICY §2.1):** every fact in `content/` carries `sources:` with an `https://` URL and a `checked:` date on or after 2026-10-09. A consequence card's text states only: what a party publicly threatened or promised, what officials recorded that they feared or expected (UN records, FRUS, cabinet and Knesset minutes, the contemporary press), and indicator changes as labelled design values. Never what "would have happened".
- **Framing (CONTENT_POLICY §1):** Israeli framing; one value per fact; no opposing-narrative panels. Not reopened.
- **Sensitive content:** the tags `holocaust`, `terror`, `massacre` and `war-casualties` (`EventRules.cs:11`) carry no positive delta to `treasury`, `fx_reserves`, `food`, `food_supply`, `morale` or `intl_standing` (`EventRules.cs:18`, `:92-93`). A consequence card inherits its parent's sensitive tags (R-ALT-007). A sensitive card is `sensitivity-reviewed` by the owner before any build is shared (`ReleaseGate.cs:20-22`). "Deport never pays": an honest cost, no reward (GDD owner ruling 1; CONTENT_POLICY §4).
- **Real people:** no invented quotes and no invented deeds for a named real person (CONTENT_POLICY §2.2, §2.5). Consequence and variant text uses office placeholders (`{prime_minister}`), the stated threats and promises of bodies and offices, and documented fears. An `alternative.by` names a body or an office in words, never a person.
- **Process (#783, adopted for #939):** "agents draft, the owner edits … nothing is built until it's signed off". An agent posts each card as a sourced draft on its issue; the owner accepts, changes or rejects it in words; only then is it written to `content/`.
- **Design values:** every number an option carries on a new card is a labelled design value with a comment ("not a historical figure and not a moral judgement", the wording of `reparations_claim.yaml:23-27`), owner-approved on the PR. The depth cap is a labelled design value (Q-ALT-5).
- **Review status:** a person checks every factual claim before a row is `fact-checked` (CONTENT_POLICY §2.6). `reviewed_by` and `reviewed_on` are written only by that person; an agent never adds, changes or removes them, and never edits a signed row (`scripts/oss/reviewed_by_guard.py`).
- **Mode:** Historical mode is the only startable mode (`Simulation.cs:13`; `SaveGame.cs:155`). The mechanism works in Historical mode, because the player's explicit answer is never overridden there (`EventSystem.cs:134` forces only an *unanswered* hard anchor). Whether to keep it there or move it to Open mode is Q-ALT-1.
- **Rule IDs:** new rules use the proposed area `R-ALT` (maintainer sign-off on the area name, as #532 asked for its areas). Each ID is defined in `docs/rules/mvp.md` in the PR that adds its citing test (`scripts/oss/rule_ids.py`).
- **Each PR:** a branch from current `origin/main` in its own worktree; a claimed Task issue; a `CHANGELOG.md` line under `## [Unreleased]` for `sim/`, `content/` or `unity/`; the session's `Co-Authored-By` line on commits and its attribution line at the end of the PR body; "Refs #939" (only the last content slice may say "Fixes #939"); the PR template filled in, including "AI assistance"; a red-first test and a fail-open mutation check; the licence attestation box left for the person contributing.
- **Checks before every PR:** `scripts/check.sh` (the changelog rule, the reviewed_by guard, `gdd_lint.py`, the policy checker tests, `check_repo.py`, `rule_ids.py`, the sim build, the sim tests with 0 warnings, the tools tests). Echo each command first; a run that collects 0 tests is a broken command.
- **Unity:** a PR that changes `unity/` or the DLL's behaviour runs `tools/build_sim_for_unity.sh`, then EditMode under `uv run tools/machine_lock.py --shared --wait-unity -- ...` (never `-nographics` on the editor, never `-quit` with `-runTests`; results XML total above 0; revert `unity/ProjectSettings` and `unity/Assets/Settings` afterwards). A PR that cannot run Unity says so and does not merge until someone has.

## Review Focus

1. **The historical run meets none of this.** Playing `GoldenRun.Orders` from 15 May 1948 to 15 January 1952 raises no `consequence` card, shows no variant and records no rejoin, so `hash1950`, `hash1952` and `mvp-end` are unchanged by every mechanism task. Test: Task 5, `AlternateTimelineTests.TheHistoricalRunRaisesNoConsequenceCard`; Task 7, `TheHistoricalRunShowsNoVariant`.
2. **A consequence card that can never fire.** The parent fires late, or three other soft cards fill the turn, and the card's window closes before it is raised. The player who took the road expects its card. Test: Task 5, `ConsequenceCardsTakeTheFirstSoftSlots` (fixture with three ordinary soft cards), and `EveryConsequenceCardFiresWhenItsRoadIsTaken` (real content, through `AltRun`).
3. **Foldback that resurrects a closed card or loses the record.** After a `rejoin`, a historical follow-up whose window has closed must not fire, one whose window is still open must fire, and the timeline must still show that the player once took the other road. Test: Task 6, `ARejoinReopensOnlyCardsStillInsideTheirWindow` and `TheTimelineKeepsTheRoadAfterARejoin`.
4. **A reward through the back door.** A consequence card that follows a sensitive event (or a Phase 6 card) gives a positive delta to a no-gain resource. Test: Task 5, `AConsequenceOfASensitiveEventCarriesItsTags` (the inherited tag makes the existing no-gain rule refuse the gain at load).
5. **A variant that flips while the card is open.** A variant keyed on an event answered in the same turn shows one text when the card appears and another after the player answers the other card. Test: Task 7, `AVariantNeedsItsRoadClosedBeforeTheCardOpens` (loader) and `EveryVariantShowsWhenItsRoadIsTaken` (real content, through `AltRun`).

---

# Part A: the design

## A.1 Where things stand (checked on `origin/main` at `1a67800e`, 17 October 2026, after GDD Phase 7 PR 1, #898)

**The engine.**
- An event is one YAML file. `GameEvent` (`Content.cs:692-726`) has a `[from, to)` window, `conditions` on indicators, one optional `Requires` (`Content.cs:704`), options and sources. `EventRequirement` is one `{Event, Option}` pair (`Content.cs:676`). `EventOption` has `Historical`, `Recommended`, `Effects`, `SetRung` and `SetPolicy` (`Content.cs:680-689`).
- `EventSystem.Trigger` (`EventSystem.cs:21-43`) fires every unfired event whose window overlaps the turn, whose requirement matches exactly (`:26`), whose objective is done (`:28`) and whose conditions hold (`:30-31`), ordered by `From` then ordinal id (`:32`). Every hard anchor fires; soft events take `max(0, 3 - hard)` slots (`:9`, `:34-36`). A fired id goes into `FiredEvents` and `PendingEvents` (`:39-40`), so no event fires twice.
- `Resolve` records `ChosenOptions[event] = option` (`EventSystem.cs:62`), for a chosen and a delegated answer alike. `Delegate` (`:121-141`) gives an unanswered card to its minister; in Historical mode an unanswered hard anchor takes its historical option with no draw (`:134`). An explicit answer is never overridden. `EventSystem.OptionEffects` (`:103-115`, R-EVT-005, #898) lists every effect of an option for the card.
- `HistoricalAI` answers every pending card with its historical option, else its first (`HistoricalAI.cs:15-19`). The golden runs take their orders from it through the reference player (`docs/determinism.md:19-25`).
- `GameState` keeps `FiredEvents`, `PendingEvents` and `ChosenOptions` (`GameState.cs:45-48`). `ChosenOptions` is hashed (`GameState.cs:160-162`). There is no flag store, no divergence measure and no resolution date.
- Load checks: requirement references (`EventRules.cs:59-66`), at most one historical option and one for every hard anchor (`:67-69`), the no-gain rule for sensitive tags (`:70`, `:92-93`), requirement cycles (`:136-153`).
- Saves are command logs replayed under a content hash, schema version 2 (`SaveGame.cs:47`); Open mode cannot start (`Simulation.cs:13`) and its saves are refused (`SaveGame.cs:155`).

**The content.**
- 32 event files. 30 non-historical options (alternatives) across them. Five events have `requires`, and all five name the *historical* option of their parent: `ten_days` (first_truce/accept), `latrun_proposal` (second_truce/comply), `jerusalem_ceasefire` (jerusalem_military_government/military_government), `sinai_withdrawal` and `rhodes_talks` (operation_horev/launch). Choosing an alternative can only remove those cards. Of the 30 alternatives, 23 change nothing later except their immediate effects, and 7 change only the security rung (the audit of 17 October 2026, summarised in #939).
- Many card bodies assert an earlier historical act as done (for example `ten_days.yaml:4`, "fighting has resumed"; each armistice card, "negotiators have reached an agreement"). Only the five cards above are guarded by a requirement.
- `EventRetimingTests.EveryEventFiresInTheHistoricalMonthlyRun` requires every event to fire in the historical run and pins the count at 32 (`EventRetimingTests.cs:31-37`). A consequence card cannot pass that test as it stands: Task 5 narrows it to historical-kind events.
- `scenario-reckless` refuses the first truce on 1 June 1948 (`sim/Statecraft.Sim.Tests/scenarios/reckless.yaml:1-11`). Any card that follows `first_truce/refuse` moves that scenario golden.
- The historical run resolves all 32 events on their historical options: a probe of `GoldenRun.PlayToEnd` on this commit found 32 chosen options and none off the record (so R-ALT-012 can assert it).
- `smoke5` presses each card's first option, and every event's first option is its historical one (checked for all 32 files; `WarEvents1948Tests.cs:87` pins it for the war cards). So the browser smoke never takes an alternative road.

**Related decisions and work.**
- **Q-OBJ-2 (Phase 5 PR 13, decided 15 October 2026):** "A refused card returns when the front is quiet again" (`docs/gdd/military-combat.md:21`); "The card comes back on the next turn its front has again been quiet for N turns, and before its sourced date. The objective stays open until it is signed or the MVP ends." (Phase 5 plan, Q-OBJ-2 row). This is R-WAR-006, built in Phase 5 PR 13. It is the follow-up for the four armistice `hold_out` options, so this plan writes no consequence card for them (§A.4.9).
- **Q-EVT-3 (Phase 7 PR 1, decided 17 October 2026, built as R-EVT-004 in #898):** "None in the MVP. Chains stay as they are, through `requires`." Its reason: "a delay would add pending state to the hash for no GDD rule". R-EVT-004: "an option has no delayed form". This plan keeps that: windows stay absolute (§A.4.3; Q-ALT-4).
- **Q-EVT-1 (decided):** "A `front_quiet` armistice takes the first soft slot". R-EVT-002 (#898) says the soft events take the free slots by `from`, then id, and is reworded when Phase 5 PR 13 adds `front_quiet`. The consequence-card priority (R-ALT-008) sits after that armistice, and Task 5 rewords R-EVT-002 to name it.
- **R-EVT-005 (#898):** `EventSystem.OptionEffects` lists every effect of an option; `EventRulesGddTests.ThePreviewListsEveryEffectOnTheRealContent` takes each event's state from the historical run (`EventRulesGddTests.cs:201`, "every event fires in the run"). A consequence card never fires there, so Task 5 narrows that test, and Task 6 adds the `rejoin` line to `OptionEffects`.
- **Q-CUT-4 (Phase 2, decided):** "A window-only change keeps `sensitivity-reviewed`. A card whose text or options change goes back to `fact-checked` until the owner re-reviews it." Q-ALT-10 asks how this applies to metadata-only edits.
- **#937** (Egypt `hold_out` gets `set_rung: 4`, owner: "Yes, match the others") and **#938** (a real cost on `reparations_claim/refuse`, owner: "A cost for refusing too") are claimed by a contributor and land on their own. Neither waits for this plan, and this plan does not touch either option's effects.
- **#939** ("A few in the MVP"; claimed by a contributor) becomes the first content slice (Task 11 and Task 12).

## A.2 How games and historians handle the road not taken

The research for this plan (17 October 2026) read shipped games, narrative-design writing and counterfactual method. Forum and fan-wiki sources are marked as such.

| Pattern | Who does it | What we take | Source |
|---|---|---|---|
| History is the default track; divergence is an explicit act, and alt content is gated on the prior choice *and* the current world state | Hearts of Iron IV (historical focus switch, `has_game_rule`, flags in AI strategy plans) | The gate on a prior choice (`requires`), not the fantasy branches | https://hoi4.paradoxwikis.com/AI_modding ; https://steamcommunity.com/app/394360/discussions/0/1843566500523128395 (forum) |
| A follow-up re-checks its trigger on the day it fires | Europa Universalis IV (`trigger`, flags, delayed chains); Crusader Kings III (valid when queued and when the delay ends) | Requirements are evaluated at fire time; no MTTH (not deterministic, hard to test) | https://eu4.paradoxwikis.com/Event_modding ; https://ck3.paradoxwikis.com/Event_modding |
| `first_valid` plus a `fallback`: the most specific card that passes, else a generic one | Crusader Kings III on_actions | Authors may write a specific and a fallback card for one slot; the soft-cap priority keeps them from being crowded out | https://ck3.paradoxwikis.com/Event_modding |
| Storylets: content plus prerequisites plus effects, small branching pockets inside a larger structure | Failbetter; Emily Short | Our events already are storylets; we add AND requirements, not per-path flags | https://emshort.blog/2019/11/29/storylets-you-want-them/ ; https://emshort.blog/2016/04/12/beyond-branching-quality-based-and-salience-based-narrative-structures/ |
| Branch and bottleneck: branches rejoin at events every version shares | Sam Kabo Ashwell's pattern survey; Choice of Games | The real outside events of 1948-52 (truces, UN votes, armistices, the elections, mass immigration) are honest bottlenecks: `rejoin` and `reconvergence` | https://gwern.net/doc/www/heterogenoustasks.wordpress.com/927e47e1d10dd1b25586b7dc48eaa3a5a4ab5cdd.html ; https://www.choiceofgames.com/2011/07/by-the-numbers-how-to-write-a-long-interactive-novel-that-doesnt-suck/ |
| The weave: a short local branch that rejoins fast | inkle (80 Days) | A short horizon: 1 or 2 cards, then foldback (Q-ALT-5) | https://medium.com/game-writing-guide/introduction-to-ink-3e6c224865f8 |
| Uncertainty is labelled, theories never confirmed | inkle (Heaven's Vault) | Text says what was feared or proposed, not what would have happened | https://mcvuk.com/development-news/heavens-vault-how-inkle-wrote-its-own-herstory/ |
| Procedural prose | Voyageur (Bruno Dias) | **Rejected** for real people and a war: we assemble sourced cards, never generate claims | https://emshort.blog/2017/02/07/voyageur-launched/ |
| The event is real; the direction it cut is the counterfactual (two printed outcomes per card) | GMT's COIN series | Most of our alternatives already are "the other side of the same real decision" | https://en.wikipedia.org/wiki/COIN_(board_game) |
| Prerequisite cards; history as contemporaries perceived it | Twilight Struggle | Consequence text models what decision-makers believed and feared, including fears later shown wrong | https://theforge.defence.gov.au/wargaming/wargame-design-decisions-twilight-struggle-and-elsewhere ; https://rulespal.com/twilight-struggle/rulebook |
| History immovable, the player acts in the margins | Through the Darkest of Times | The fallback when a road has no sourced consequence: indicators change, the narrative stays on the record | https://www.pcgamer.com/through-the-darkest-of-times-review/ |
| A timeline screen of the player's moments | Civilization VI (Historic Moments) | The two-column timeline: "Your course" beside "The record" | https://pcgamesn.com/civilization-vi/civ-6-rise-and-fall-anton-strenger-interview |
| Ministers argue different views, no verdict | Suzerain | Later, if wanted: a minister's recommendation quotes a sourced stated position (out of scope here) | https://www.avclub.com/suzerain-interview |
| Only alternatives contemporaries can be shown to have considered | Niall Ferguson, *Virtual History* (1997) | **The Ferguson gate**: a consequence card may follow only an alternative with a sourced proposal or debate (Q-ALT-3) | https://whitneyannetrettien.com/whiki/index.php/Ferguson_1999 ; https://www.h-net.org/reviews/showpdf.php?id=3721 |
| The minimal-rewrite rule: change as few well-established facts as possible | Tetlock and Belkin (1996), via secondary summaries | A consequence card moves the world only as far as its sources say; then it folds back | https://www.extinctblog.org/extinct/2017/7/31/false-history-the-minimal-rewrite-rule |

**What the research ranks highest for us** (fit 10 down to 7): the Ferguson gate with per-option sources; consequence cards on `requires` with a short horizon and a re-check at fire time; reconvergence at real outside events; per-front drift with premise-blocked hard anchors; the two-column timeline and "not adopted" banners; most-specific-first selection. **Avoid:** fantasy branches, invented characters, procedural prose about real events, MTTH timing, long sorting-hat branches.

**Where this plan departs from the research's ranking, and why.** The research suggests a numeric drift (global or per front). This plan recommends no stored drift (Q-ALT-2): every rule that needs "has the player left the record here?" can be answered from `ChosenOptions`, so drift would add state, a hash section and a design value without enabling a card that `requires` cannot already gate. Process objects whose pace bends (Victoria 3's journal entries) are also left out: they are a new sim concept, and the GDD's objectives already carry the "earlier or later than history" comparison.

## A.3 The smallest mechanism that delivers the goal

The owner's goal has three parts: (1) when the player leaves the record, they meet the dilemmas that road brings; (2) the later historical cards bend to fit; (3) the whole stays sourced and delicate. The mechanism below is the least that does all three.

1. **Know when the player left the record** (derived, no state): an event has diverged when its chosen option is not its historical option.
2. **Gate cards on that**: a requirement list (AND), with `diverged: true` for "any alternative of event X".
3. **Say who proposed the road** (`alternative` on each non-historical option), and allow follow-up cards only after an alternative someone is recorded proposing or debating (the Ferguson gate).
4. **Mark the follow-up cards** (`kind: consequence`), soft, with no historical option, at most 2 deep, inheriting sensitive tags, taking soft slots first.
5. **Fold back** (`rejoin` on a consequence option): the later historical cards that need the parent's historical option can fire again, inside their own windows.
6. **Bend the later historical text** (`variants` on a `kind: reconvergence` event): the first matching variant replaces the title and text; options and effects stay.
7. **Tell the player** (sim `Timeline` helper; the card banner; a two-column timeline screen).
8. **Gate the release**: consequence cards and variants need a person's sign-off before a shareable build (Q-ALT-9).

Everything else is content, in slices, through the review gates of §A.4.8.

## A.4 Design decisions and the questions they raise

### A.4.1 The divergence model

**Proposed (Q-ALT-2, recommended (a)):** no stored divergence. `Timeline.Diverged(c, s, eventId)` is true when `s.ChosenOptions[eventId]` exists, the event has a historical option, and the chosen option is not it. An event with no historical option never diverges. A delegated answer counts the same as a chosen one, as `requires` does today (`EventRequiresTests.ADelegatedChoiceCountsToo`; Q-ALT-11).

Per-front drift (the research's pattern 5) stays available as a later step if the owner wants it: it is a pure function of `ChosenOptions` and a front tag, and adding it later moves nothing.

### A.4.2 Content kinds and the Ferguson gate

| Kind | Where | Meaning | Loader rules |
|---|---|---|---|
| historical | option (`historical: true`, existing) | What really happened | Unchanged: at most one per event; every hard anchor has one |
| considered | option (`alternative.basis: proposed` or `debated`) | An alternative a named body or office is recorded proposing or debating at the time | `alternative.by` (words, no braces, no person's name by review rule), `alternative.date`, `alternative.sources` (at least one, `https://`, checked) (R-ALT-003) |
| design | option (`alternative.basis: design`) | An alternative nobody is recorded proposing: the GDD's "design assumption" (`GDD.md:195`) | `by` empty, no `date`; sources optional (R-ALT-003). No consequence card may follow it (Q-ALT-3 (a), decided) |
| historical (event) | event (`kind` omitted or `historical`) | Every event on `main` today | Unchanged |
| consequence | event (`kind: consequence`) | A card that exists only because the player left the record | Soft; no historical option; at least one requirement is an alternative road; every option has an `alternative`; depth at most 2; inherits the parent's sensitive tags (R-ALT-005 to R-ALT-007) |
| reconvergence | event (`kind: reconvergence`) | A real outside event that happened regardless, which may carry `variants` | Has a historical option; only this kind may carry `variants` (R-ALT-010) |

**The Ferguson gate in the loader (R-ALT-006):** a consequence card's alternative-road requirement must name an option whose `alternative.basis` is `proposed` or `debated`. For `diverged: true`, every non-historical option of that event must be. A chain's second card follows an option of the first card, which must also be `proposed` or `debated`. Q-ALT-3 is decided (a), so a `design` alternative gets no follow-up card.

**Why the gate matters here:** the leads inventory (§A.6) shows that some alternatives have sourced *consequences* but no recorded Israeli *advocate*. `first_truce.yaml` itself says: "the notes found no Israeli voice for refusing". Under (a), refusing the first truce gets no follow-up card even though the Security Council's warning is on record. That is the delicate trade-off, and it is the owner's call.

**The release gate (R-ALT-011):** `ReleaseGate.AlternateUnsigned(c)` lists every `consequence` event and every event with `variants` that has no `reviewed_by`. `Statecraft.Cli --release-gate` refuses a shareable build while the list is not empty (Q-ALT-9). The existing sensitive-status gate (`ReleaseGate.cs:20-22`) applies on top.

### A.4.3 Requirements and timing

- **A list, all of which must hold (R-ALT-001).** `requires: [{ event: a, option: x }, { event: b, option: y }]`. The five existing files change from `requires: { ... }` to `requires: [{ ... }]` with the same meaning (Task 1). The schema validator in `SchemaTests` supports no `oneOf`, so the list is the only form.
- **Any alternative of an event (R-ALT-002).** `{ event: a, diverged: true }`. The loader refuses `diverged` on an event with no historical option, and an entry with both `option` and `diverged`, or neither.
- **No OR and no negation.** Two roads to one card are two cards (the fallback pattern). YAGNI.
- **Relative timing ("N turns after").** **Decided (Q-ALT-4 (a), owner, 17 October 2026, #945):** absolute windows only. Every parent fires on a fixed turn in a run that reaches it (hard anchors at their window; soft ones at most a cap-delay later), so an absolute window after the parent's window does the job. It needs no resolution date in state, which Q-EVT-3 decided against ("a delay would add pending state to the hash"). A late parent is caught by the reachability test (Review Focus 2).

### A.4.4 Reconvergence, foldback and broken premises

- **Foldback at outside events (`rejoin`, R-ALT-009).** An option of a consequence card may say `rejoin: <parent event>`. Once that option is chosen, a requirement on the parent's *historical* option is met, so the record's later cards come back if their windows are still open. `ChosenOptions[parent]` is not rewritten: the record of the road stays, and `diverged: true` on the parent stays true. Example shape: a card after refusing a truce offers "accept the truce now", which rejoins the truce event.
- **Reconvergence events.** The outside events that happened regardless (the truces' UN orders, the armistice talks, the January 1949 elections, mass immigration) are marked `kind: reconvergence` when a variant is written for them. They keep firing for every player. Their options and effects stay; their text bends (§A.4.5).
- **A hard anchor whose premise no longer holds.** Two tools, both existing or small:
  1. *Suppress*: give the hard anchor a requirement on the premise's historical option (as the five existing cards already do). For a divergent player it never fires.
  2. *Replace*: write a consequence card for the same slot, `requires: [{ event: premise, diverged: true }]`, which takes the first soft slot (R-ALT-008).
  Which cards to suppress, replace or bend is a per-card editorial call made in each content slice from the premise table (Task 3). Adding a requirement to a card on `main` changes that card, so it follows Q-CUT-4 (Q-ALT-10).
- **The armistices.** A refused armistice comes back under R-WAR-006 (Phase 5 PR 13). This plan adds nothing for the four `hold_out` options (§A.4.9).

### A.4.5 Variant text for later cards

`variants: [{ when: [requirements], title, text, sources }]` on a `reconvergence` event (R-ALT-010). The first variant, in file order, whose `when` all hold supplies the title (if given) and text; otherwise the event's own. Options and effects never vary: a different choice set is a consequence card. Each variant carries its own sources, its placeholders are checked over the event's window, and at least one `when` entry must be an alternative road. Every `when` event's window must close on or before the variant event's `from`, so the road is settled before the card opens (Review Focus 5). `EventSystem.Render(c, s, e)` and a new `EventSystem.Title(c, s, e)` apply it. Cause text is derived and not hashed (`docs/determinism.md`, "Cause text is derived, not hashed"), so a variant moves no golden.

### A.4.6 Ordering under the soft cap

Consequence cards take the first soft slots of a turn (R-ALT-008), after any `front_quiet` armistice (R-EVT-002, decided), then the rest by `From` and ordinal id. With no consequence card eligible, the order is the same as today (a stable sort with one key value), so the historical runs do not move (Q-ALT-14).

### A.4.7 What the player sees

- **On a consequence card (Task 9):** a sober banner above the text, from `Timeline.Origin`: "Alternative course. It follows your decision: *[the option you took]* (*[the parent card's title]*). In the record: *[the historical option]*."
- **On a considered option (Q-ALT-7, recommended (a)):** "Not adopted. Proposed by *[by]*, *[date]*." is shown after the choice, on the follow-up card and in the timeline, not on the button before choosing, so a choice is not reduced to "pick history".
- **On a rejoin option:** "Returns to the recorded course."
- **The timeline screen (Task 10; Q-ALT-8, recommended (a)):** two columns, "Your course" and "The record", one row per resolved event in window order, a marker where they differ, and "rejoined" where a rejoin closed the gap. Fed by `Timeline.Entries`, a pure sim helper.
- Hebrew strings land with English (`strings.he.yaml`), flagged for native review (#564).

### A.4.8 The content pipeline and its review gates

| Gate | Who | Output | Blocks |
|---|---|---|---|
| G1. Leads | Research agents | The unverified leads (§A.6) | Nothing: leads only |
| G2. Independent fact-check | An agent that did not write the leads, then a person | `research_notes/Alternate timelines/roads-checked.md`: for each alternative, its basis (proposed / debated / design) with a verbatim excerpt under 15 words and the source, and each expected consequence with view-holder, date, excerpt and source; every lead that fails is marked dropped, with why; plus the premise table (which later cards' text the alternative contradicts) | Every `alternative` record and every card |
| G3. Draft on the issue | An agent | A comment per card: title, text, options with `alternative` records, effects as labelled design values, window, sources, sensitive tags, the premise it bends | The owner's ruling |
| G4. Owner ruling | The owner, in words | Accept, change or reject, per card | Any YAML |
| G5. PR | An agent or contributor | YAML exactly as ruled; tests; `status: fact-checked` only after a person checked each claim (CONTENT_POLICY §2.6) | Merge |
| G6. Sensitivity review | The owner | `status: sensitivity-reviewed` on a sensitive card | A shareable build |
| G7. Sign-off | The reviewer, in their own commit | `reviewed_by`, `reviewed_on` | A shareable build (R-ALT-011; Q-ALT-9 (a), decided) |

### A.4.9 How this fits Phases 5 to 7

- **Phase 5 PR 13 (R-WAR-005, R-WAR-006, Q-OBJ-1, Q-OBJ-2).** The re-offered armistice is Phase 5's rule. When it is signed, `ChosenOptions[armistice_x]` becomes `sign` (or the re-offer card's own option, if Phase 5 builds the re-offer as a separate card, which can then carry `rejoin`). Either way `diverged` clears or a rejoin closes it, and the timeline shows it. This plan writes no consequence card on an armistice `hold_out` and does not touch R-WAR-006. If Phase 5 builds the re-offer as a separate event, it should use `kind: consequence` and `rejoin` from this plan; Task 0 records that note on the Phase 5 plan.
- **Phase 5 Q-ENEMY-5** ("Cards give orders to units"). A consequence card on a war front gives no unit orders until Phase 7 PR 2's structured effects exist. Until then its options use numeric effects and `set_rung` only.
- **Phase 6 (settlement control).** The capture outcomes (Raid, Occupy, Deport) are commands, not events; this plan writes no card after them. If Phase 6 adds follow-up cards (its Q-CARD-1), a consequence card after them inherits their sensitive tags (R-ALT-007), and a tag Phase 6 adds to `EventRules.SensitiveTags` (for example `displacement`, its Q-DEP-6) is inherited the same way.
- **Phase 7 PR 1 (R-EVT-001 to R-EVT-005, merged as #898).** R-EVT-004 "never later": unchanged, because no effect is delayed. R-EVT-002: the consequence priority sits after the `front_quiet` armistice (Task 5 rewords it). R-EVT-005: `OptionEffects` lists `rejoin` as a line (Task 6).
- **Phase 7 PR 3 (the MVP's event set) and R-TURN-003 (the gap rule).** The gap rule is measured on the historical run, where no consequence card fires, so it is unaffected. Q-EVT-5's "the size of the set" counts historical cards; consequence cards are counted separately in the PR that adds them.
- **Phase 7 PR 8 (the event card) and PR 9 (the end screen).** Task 9's banner is a change to the same card; whichever lands second rebases. The end screen may show the timeline (Q-ALT-8 (c)).

### A.4.10 Out of scope (YAGNI)

A stored drift number; per-front drift; OR or NOT requirements; relative windows (Q-ALT-4 is (a)); delayed effects; probabilistic timing; procedural text; invented characters; unit-order effects on consequence cards (Phase 7 PR 2); alternative effects in variants; a new game mode; any change to the effects of `armistice_egypt/hold_out` (#937) or `reparations_claim/refuse` (#938).

### A.4.11 Determinism, saves and goldens

| Task | GameState / save | Goldens |
|---|---|---|
| 1 requirement list | none | none (same meaning; checked by a real update run that writes no diff) |
| 2 `alternative` shape | none | none |
| 3 fact-check | none (research note) | none |
| 4 backfill | none; content hash changes | none: content-hash changes do not move goldens (`AGENTS.md`, Determinism); checked by a real run |
| 5 consequence kind | none | none (no card in content yet) |
| 6 rejoin | none | none |
| 7 variants | none | none (text is not hashed) |
| 8 timeline helper, release gate, report | none | none |
| 9, 10 Unity | none | `smoke5` unchanged (first options are historical) |
| 11-14 content slices | none | `scenario-reckless` moves if a card follows `first_truce/refuse`; other goldens move only if a scripted run takes the road; re-pinned from a real run |

## A.5 Owner questions

The owner answered every row on 17 October 2026: "Accept all recommendations" ([#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172); recorded on #943). Each row quotes the recommendation that is now the answer.

| Id | Question | Options | Answer | Waits |
|---|---|---|---|---|
| Q-ALT-1 | **Which mode carries the alternate timeline?** Historical mode is the only startable mode; a player can already take an alternative there, and an explicit answer is never overridden. | (a) Historical mode, as today; (b) only Open mode, so Historical mode keeps the player on the record (needs Open mode, M4, which cannot start today); (c) Historical mode now and Open mode later, with a start-screen note that choices can leave the record | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): it needs nothing new, and the Historical-mode rule ("an unanswered hard anchor takes its historical option") already means "history unless you choose otherwise" | Tasks 5 to 14 |
| Q-ALT-2 | **How is divergence measured?** | (a) derived from the choices, no stored number; (b) derived per-front counts (Jerusalem, the south, the north, the home front) from a front tag on each event, for gating and display; (c) a stored drift value shown to the player | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): `requires` can gate every card the leads support; (b) can be added later with no golden move | Task 5 |
| Q-ALT-3 | **The Ferguson gate: which alternatives may get follow-up cards?** | (a) only alternatives with a sourced contemporary proposal or debate (`proposed` or `debated`); (b) also `design` alternatives, with the card's text limited to what outside parties had publicly threatened or promised; (c) no gate | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): it is the editorial rule a historian can apply, and it keeps invented history out. It excludes `first_truce/refuse` unless an Israeli advocate is found | Task 5, Tasks 11 to 14 |
| Q-ALT-4 | **Relative timing ("N turns after").** | (a) absolute windows only; (b) an `after: { event, turns }` key, which stores each divergent answer's date in state (save version 3, re-pinned goldens) | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): consistent with Q-EVT-3 ("None in the MVP … a delay would add pending state to the hash") | none (the relative-windows task is dropped) |
| Q-ALT-5 | **How far does a road run before it folds back?** (a labelled design value) | (a) 1 card; (b) 2 cards; (c) 3 cards | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (b): the research's 1-3 range; plausibility drops with each unsourced step, and the leads rarely support a third | Task 5 |
| Q-ALT-6 | **Foldback.** | (a) `rejoin` on a consequence option makes the parent count as historical for later requirements, keeping the record of the road; (b) no rejoin: once off the record, its later historical cards stay suppressed; (c) OR requirements instead of rejoin | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): honest bottlenecks, smallest change, no lost history | Task 6 |
| Q-ALT-7 | **When is "Not adopted, proposed by …" shown?** | (a) after the choice: on the follow-up card and in the timeline; (b) on the option before choosing; (c) both | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): the choice stays a dilemma, and the label still teaches the record | Task 9 |
| Q-ALT-8 | **The two-column timeline screen.** | (a) a screen in the MVP, reached from the HUD; (b) later; (c) only on the end screen (Phase 7 PR 9) | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): the research's strongest respect signal, and the data is a pure helper | Task 10 |
| Q-ALT-9 | **The release gate for alternative content.** | (a) a shareable build needs a person's sign-off (`reviewed_by`) on every consequence card and every variant; (b) the status rules only, as for any card; (c) (a) plus a recorded historian review | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): it is the #783 rule ("nothing is built until it's signed off") enforced in code | Task 8 |
| Q-ALT-10 | **Do metadata-only edits reset a reviewed card?** Adding an `alternative` record to an option, or changing `requires: {…}` to `requires: [{…}]` with the same meaning, touches six `sensitivity-reviewed` cards (`altalena`, `bernadotte_assassination`, `ten_days`, `operation_yoav`, `operation_hiram`, `operation_horev`). Q-CUT-4: "A card whose text or options change goes back to `fact-checked`." | (a) no: neither changes text, effects or what the player sees on the card, so the review stays; (b) yes: each goes back to `fact-checked` for the owner to re-review; (c) the requirement form keeps the review, the `alternative` record resets it | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a), with the diff listed in the PR for the owner to see | Tasks 1, 4 |
| Q-ALT-11 | **Does a minister's delegated alternative count?** A soft card left unanswered can be delegated to an alternative (random pick with probability 1 - competence/10). | (a) yes, the government chose, and the banner says the minister decided; (b) no, only the player's own answer diverges (needs a stored "how", a state change); (c) in Historical mode delegation always takes the historical option of any event that has one (moves `smoke5` and every run that leaves cards to ministers) | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): it is how `requires` already works, and it needs no state | Task 5 |
| Q-ALT-12 | **The alternatives with no sourced consequence.** About ten alternatives have no qualifying expected consequence in the leads (§A.6). | (a) keep them, with `basis: design` where nobody proposed them, and no follow-up; (b) rewrite each toward an alternative that was proposed; (c) remove the option (changes option counts that tests pin) | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): honest labelling, no content churn | Task 4 |
| Q-ALT-13 | **The first slice (#939: "A few in the MVP").** Which 3 to 5 roads first? | (a) the leads with the strongest records: `second_truce/defy`, `operation_yoav/push_to_hebron`, `sinai_withdrawal/hold_el_arish`, `jerusalem_military_government/accept_demilitarisation`, `operation_hiram/advance_into_lebanon`; (b) #939's original list (adds `first_truce/refuse` and `operation_horev/hold`, which have no recorded advocate or no lead); (c) the owner's own list | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a), subject to the fact-check (G2) and Q-ALT-3 | Tasks 11, 12 |
| Q-ALT-14 | **Consequence cards and the soft cap.** | (a) they take the first soft slots, after a `front_quiet` armistice; (b) the usual order by `From`, then id | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): a road the player took should not be crowded out of its window | Task 5 |
| Q-ALT-15 | **The rule-ID area.** | (a) `R-ALT`; (b) under `R-EVT` (Phase 7's area) | **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** (a): one area keeps the mechanism's rules together | Task 1 |

## A.6 Leads for the content slices (UNVERIFIED)

These are leads from a research pass of 17 October 2026 (OpenAI Codex). They are **not verified**: 26 points are marked as gaps, and no excerpt has been checked by a person. Nothing below may enter `content/` before gate G2 (Task 3) has checked it. The URLs are where to start.

| Road (event / option) | Lead on what contemporaries expected | Where to check |
|---|---|---|
| `first_truce/refuse` | Security Council consideration of Chapter VII action; US officials' study of sanctions. No Israeli advocate recorded (`first_truce.yaml` comment) | https://avalon.law.yale.edu/20th_century/scres050.asp ; https://history.state.gov/historicaldocuments/frus1948v05p2/d428 |
| `second_truce/defy` | Chapter VII enforcement ordered in Resolution 54; a US expectation that sanctions would run against Israel | https://avalon.law.yale.edu/20th_century/scres054.asp ; https://history.state.gov/historicaldocuments/frus1948v05p2/d735 |
| `operation_yoav/push_to_hebron` | A war with the Arab Legion and a wider war (an IDF History Department study citing the war diary); fear of British intervention | https://www.idf.il/media/rccdyzpl/%D7%94%D7%A8-%D7%97%D7%91%D7%A8%D7%95%D7%9F-%D7%91%D7%9E%D7%9C%D7%97%D7%9E%D7%AA-%D7%94%D7%A2%D7%A6%D7%9E%D7%90%D7%95%D7%AA-%D7%90%D7%97%D7%A8%D7%99-%D7%A2%D7%99%D7%9E%D7%95%D7%93-1.pdf ; https://en.wikipedia.org/wiki/Operation_Yoav |
| `sinai_withdrawal/hold_el_arish` | British action under the 1936 Anglo-Egyptian treaty; US pressure; a wider conflict outside Palestine | https://history.state.gov/historicaldocuments/frus1949v06/d341 ; https://history.state.gov/historicaldocuments/frus1949v06/d354 |
| `jerusalem_military_government/accept_demilitarisation` | Withdrawal of both principal forces from Jerusalem; a proposed UN guard; a UN regime for the city | https://history.state.gov/historicaldocuments/frus1948v05p2/d428 |
| `operation_hiram/advance_into_lebanon` | International condemnation; UN pressure to withdraw behind the frontier | https://en.wikipedia.org/wiki/Operation_Hiram ; https://documents.un.org/api/symbol/access?s=S/1071&l=en&t=pdf |
| `austerity_1949/decline` | Forgoing the foreign-currency saving the plan's presenters called necessary | https://fs.knesset.gov.il/1/Plenum/1_ptm_250102.pdf |
| `clothing_rationing_1950/relax` | More foreign-currency spending; an end to the retailers' strike under a modified programme | https://fs.knesset.gov.il/1/Plenum/1_ptm_250241.pdf ; https://www.jta.org/archive/strike-of-clothing-and-shoe-storekeepers-in-israel-ends-modified-rationing-seen |
| `reparations_claim/refuse` (sensitive: `holocaust`) | Forgoing the four-power claim of $1.5 billion | https://history.state.gov/historicaldocuments/frus1951v05/d296 |
| `altalena/compromise` (sensitive) | Competing military authority inside the IDF; a breach of the truce's arms terms | https://benyehuda.org/read/56639 ; https://fs.knesset.gov.il/0/Plenum/0_ptm_249974.pdf |
| The four armistice `hold_out` options | Weak: the agreements say what signing secured, not what refusal would bring. Covered by R-WAR-006 instead | (no card in this plan) |
| No qualifying lead | `bernadotte_assassination/investigate_only`, `jerusalem_ceasefire/hold_out`, `latrun_proposal/press_offensive`, `operation_horev/hold`, `ten_days/southern_front`, `rhodes_talks/decline`, `education_registration_1951/concede`, `iraqi_exodus/pace`, `israel_bonds/donations_only`, `ministry_supply_crisis/keep_ministry` | Q-ALT-12 |

---

# Part B: the implementation plan

## The task list

| Task | Branch | Issue | What | Rests on (decided 17 Oct 2026) |
|---|---|---|---|---|
| 0 | `docs/alternate-timelines-answers` | #945 | Record the answers in this plan, the Phase 5 plan note, `docs/gdd/alternate-timelines.md` | the answers |
| 1 | `sim/alt-requires-list` | #946 | `requires` as a list (AND) and `diverged: true`; the five files migrated | Q-ALT-10, Q-ALT-15 |
| 2 | `sim/alt-option-alternative` | #947 | The `alternative` record on options (shape only, optional) | none beyond Task 0 |
| 3 | `research/alt-roads-checked` | #948 | Gate G2: the independent fact-check and the premise table | none |
| 4 | `content/alt-alternatives-backfill` | #949 | `alternative` on all 30 alternatives; the loader then requires it | Q-ALT-10, Q-ALT-12 |
| 5 | `sim/alt-consequence-cards` | #950 | `kind: consequence`, the Ferguson gate, the depth cap, tag inheritance, the soft-slot priority, `AltRun` | Q-ALT-1, Q-ALT-2, Q-ALT-3, Q-ALT-5, Q-ALT-11, Q-ALT-14 |
| 6 | `sim/alt-rejoin` | #951 | `rejoin` foldback | Q-ALT-6 |
| 7 | `sim/alt-variants` | #952 | `kind: reconvergence` and `variants` | none beyond Task 0 |
| 8 | `sim/alt-timeline-gate` | #953 | `Timeline.Entries` and `Origin`, the release gate, the CLI coverage report | Q-ALT-9 |
| 9 | `unity/alt-card-banner` | #954 | The banner on consequence cards and the rejoin line | Q-ALT-7 |
| 10 | `unity/alt-timeline-screen` | #955 | The two-column timeline screen | Q-ALT-8 |
| 11 | `docs/alt-slice-1-drafts` | #939 | Gate G3 for slice 1: sourced card drafts posted on #939 | Q-ALT-13 |
| 12 | `content/alt-slice-1` | #956 | The cards the owner accepted from Task 11 | the per-card rulings |
| 13 | `content/alt-slice-2` | #957 | The home-front and Jerusalem roads: drafts, ruling, cards | the per-card rulings |
| 14 | `content/alt-reconvergence-variants` | #958 | Variants on the outside events the slices bend; suppressions and replacements from the premise table | the per-card rulings |

**Sequencing.** Task 0 first. Tasks 1, 2 and 3 can run in parallel (different files). Task 4 needs 2 and 3. Task 5 needs 1 and 2. Tasks 6 and 7 need 5 (7 needs only 1, but shares `EventRules.cs` with 5, so it follows to avoid conflicts). Task 8 needs 5, 6 and 7. Task 11 needs 3. Task 12 needs 4, 5, 6, 8 and the rulings on Task 11. Tasks 9 and 10 need 8 and at least one consequence card on `main` (Task 12). Tasks 13 and 14 follow 12. Golden-moving PRs (12, 13, 14 when they move one) merge one at a time.

## File map

| File | Responsibility | Tasks |
|---|---|---|
| `sim/Statecraft.Sim/Content.cs` | `EventRequirement.Diverged`; `GameEvent.Requires` as a list, `Kind`, `Variants`; `EventOption.Alternative`, `Rejoin`; new `OptionAlternative`, `EventVariant` | 1, 2, 5, 6, 7 |
| `sim/Statecraft.Sim/Timeline.cs` (new) | Pure helpers: `Diverged`, `Met`, `AllMet`, `Rejoined`, `Variant`, `Origin`, `Entries` | 1, 6, 7, 8 |
| `sim/Statecraft.Sim/EventRules.cs` | Load checks R-ALT-001 to R-ALT-007, R-ALT-009, R-ALT-010; the cycle check over lists | 1, 2, 4, 5, 6, 7 |
| `sim/Statecraft.Sim/EventSystem.cs` | Eligibility through `Timeline.AllMet`; the soft-slot priority; the rejoin line in `OptionEffects`; `Title`; variant text in `Render` | 1, 5, 6, 7 |
| `sim/Statecraft.Sim/ReleaseGate.cs`, `sim/Statecraft.Cli/Program.cs` | `AlternateUnsigned`; `--release-gate`; `--timeline-report` | 8 |
| `content/schema/event.schema.json` | `requirement`, `alternative`, `variant` definitions; `kind`, `variants`, `rejoin` | 1, 2, 5, 6, 7 |
| `content/events/*.yaml` | The migration, the backfill, the slices | 1, 4, 12, 13, 14 |
| `sim/Statecraft.Sim.Tests/EventRequiresTests.cs`, new `AlternateTimelineTests.cs`, new `AltRun.cs`, `SchemaTests.cs`, `WarEvents1948Tests.cs`, `EventRetimingTests.cs`, `EventRulesGddTests.cs`, `FinalWaveTests.cs` | Tests | 1 to 8, 12 to 14 |
| `docs/WRITING_EVENTS.md`, `docs/rules/mvp.md`, `docs/gdd/alternate-timelines.md` (new) | The authoring contract, the rules, the feature section | 0, 1, 2, 5, 6, 7, 8 |
| `research_notes/Alternate timelines/roads-checked.md` (new) | Gate G2's record | 3 |
| `unity/Assets/Scripts/UI/EventCardPresenter.cs`, `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`, `strings.he.yaml`, new `TimelinePresenter.cs`, EditMode tests | The banner and the timeline screen | 9, 10 |

---

### Task 0 (#945): record the owner's answers

**Files:**
- Modify: `docs/superpowers/plans/2026-10-17-alternate-timelines.md` (§A.5: each row's answer, quoted, with the comment URL; each task's "Waits on")
- Modify: `docs/superpowers/plans/2026-10-15-gdd-phase-5.md` (PR 13: one note that a separate re-offer card uses `kind: consequence` and `rejoin`)
- Create: `docs/gdd/alternate-timelines.md` (from `docs/gdd/TEMPLATE.md`)

**Interfaces:**
- Consumes: the owner's answers to Q-ALT-1 to Q-ALT-15, in words.
- Produces: decided rows every later task cites by id.

- [x] **Step 1: Copy each answer verbatim** into §A.5's Answer column as `**Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](...)):**` followed by the recommended option. Done; Q-ALT-4 (a) dropped the relative-windows task.
- [x] **Step 2: Write `docs/gdd/alternate-timelines.md`** with the template's sections (Player story, Rules, Data touched, Done when, Out of scope, Sources, Open questions). Rules lists only decided rows, each citing `[S<n>]`; Sources lists the owner's comment URLs and this plan, each with a checked date. Every number carries a unit or `(unitless)`.
- [x] **Step 3: Run the doc checks.**

```bash
echo "uv run scripts/oss/gdd_lint.py && uv run scripts/oss/md_links.py && uv run scripts/oss/check_repo.py"
uv run scripts/oss/gdd_lint.py && uv run scripts/oss/md_links.py && uv run scripts/oss/check_repo.py
```
Expected: no findings, exit 0.

- [x] **Step 4: Commit.**

```bash
git add docs/superpowers/plans/2026-10-17-alternate-timelines.md docs/superpowers/plans/2026-10-15-gdd-phase-5.md docs/gdd/alternate-timelines.md
git commit -m "docs(plan): record the owner's alternate-timeline answers (refs #945, #939)"
```

---

### Task 1 (#946): `requires` as a list, and `diverged: true`

**Files:**
- Modify: `sim/Statecraft.Sim/Content.cs:675-676` (`EventRequirement`), `:703-704` (`Requires`)
- Create: `sim/Statecraft.Sim/Timeline.cs`
- Modify: `sim/Statecraft.Sim/EventSystem.cs:26`
- Modify: `sim/Statecraft.Sim/EventRules.cs:59-66`, `:136-153`
- Modify: `sim/Statecraft.Sim/ContentLoader.cs:332` (the comment)
- Modify: `content/schema/event.schema.json:35-43`
- Modify: `content/events/{ten_days,latrun_proposal,jerusalem_ceasefire,sinai_withdrawal,rhodes_talks}.yaml` (the `requires:` line)
- Modify: `sim/Statecraft.Sim.Tests/EventRequiresTests.cs`, `SchemaTests.cs:194`, `WarEvents1948Tests.cs:89-90`
- Modify: `docs/WRITING_EVENTS.md:40`, `:115-122`; `docs/rules/mvp.md` (new `## Alternate timelines` section); `CHANGELOG.md`

**Interfaces:**
- Produces: `List<EventRequirement> GameEvent.Requires` (empty when none); `bool EventRequirement.Diverged`; `static class Timeline` with `EventOption? HistoricalOption(GameEvent e)`, `bool Diverged(Content c, GameState s, string eventId)`, `bool Met(Content c, GameState s, EventRequirement r)`, `bool AllMet(Content c, GameState s, GameEvent e)`.

- [ ] **Step 1: Write the failing tests.** In `EventRequiresTests.cs`, change `Follow`'s line to `requires: [{ event: first, option: go }]`, change `Requiring` to `requires: [{{ event: {other}, option: ok }}]`, and change the theory rows' fields from `events[follow].requires.event` / `.option` to `events[follow].requires[0].event` / `.option` (and their `Replace` source string to the list form). Change `TheRequirementIsReadFromYaml` to:

```csharp
    [Fact]
    public void TheRequirementIsReadFromYaml()
    {
        var r = Assert.Single(Load(Follow).Events.Find(x => x.Id == "follow")!.Requires);
        Assert.Equal("first", r.Event);
        Assert.Equal("go", r.Option);
        Assert.False(r.Diverged);
        Assert.Empty(Load(Follow).Events.Find(x => x.Id == "first")!.Requires);
    }
```

Add:

```csharp
    const string Second = @"
id: second
title: Second
text: 'Another choice.'
from: 1948-06-01
options:
  - id: agree
    text: Agree
    historical: true
  - id: decline
    text: Decline
";

    const string Plain = @"
id: plain
title: Plain
text: 'No record of what happened.'
from: 1948-06-01
options:
  - id: one
    text: One
  - id: two
    text: Two
";

    static Content LoadAll(string follow) => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
    {
        ["first.yaml"] = First, ["second.yaml"] = Second, ["plain.yaml"] = Plain, ["follow.yaml"] = follow,
    }));

    /// <summary>Plays the two choices, then triggers once more and says whether the follow-up fired.</summary>
    static bool FiresAfter(Content c, string first, string second)
    {
        var s = State(c);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "first", first, why));
        Assert.Null(EventSystem.Choose(c, s, "second", second, why));
        EventSystem.Trigger(c, s, why);
        return s.FiredEvents.Contains("follow");
    }

    // R-ALT-001
    [Theory]
    [InlineData("go", "decline", true)]
    [InlineData("go", "agree", false)]
    [InlineData("stay", "decline", false)]
    public void EveryRequirementInTheListMustHold(string first, string second, bool fires)
    {
        var c = LoadAll(Follow.Replace("requires: [{ event: first, option: go }]",
            "requires: [{ event: first, option: go }, { event: second, option: decline }]"));
        Assert.Equal(fires, FiresAfter(c, first, second));
    }

    // R-ALT-002
    [Theory]
    [InlineData("go", false)]
    [InlineData("stay", true)]
    public void DivergedMeansAnyOptionButTheHistoricalOne(string first, bool fires)
    {
        var c = LoadAll(Follow.Replace("requires: [{ event: first, option: go }]", "requires: [{ event: first, diverged: true }]"));
        Assert.Equal(fires, FiresAfter(c, first, "agree"));
    }

    [Fact]
    public void AnUnansweredEventHasNotDiverged()
    {
        var c = LoadAll(Follow);
        var s = State(c);
        Assert.False(Timeline.Diverged(c, s, "first"));
        Assert.False(Timeline.Diverged(c, s, "plain"));
        s.ChosenOptions["plain"] = "two";
        Assert.False(Timeline.Diverged(c, s, "plain"));    // no historical option: nothing to diverge from
    }

    // R-ALT-002
    [Theory]
    [InlineData("requires: [{ event: first }]", "events[follow].requires[0]", "needs an option or diverged: true")]
    [InlineData("requires: [{ event: first, option: go, diverged: true }]", "events[follow].requires[0]", "an option or diverged: true, not both")]
    [InlineData("requires: [{ event: plain, diverged: true }]", "events[follow].requires[0].diverged", "event 'plain' has no historical option")]
    public void ABadDivergedRequirementIsALoadError(string line, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => LoadAll(Follow.Replace("requires: [{ event: first, option: go }]", line)));
        Assert.Equal("events/follow.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    /// <summary>The cycle check walks every entry of the list, not only the first.</summary>
    [Fact]
    public void ACycleThroughASecondRequirementIsFound()
    {
        var a = "id: a\ntitle: a\ntext: x\nfrom: 1948-06-01\nrequires: [{ event: first, option: go }, { event: b, option: ok }]\noptions:\n  - id: ok\n    text: Ok\n";
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["first.yaml"] = First, ["a.yaml"] = a, ["b.yaml"] = Requiring("b", "a"),
        })));
        Assert.Equal("events[a].requires", ex.Field);
        Assert.Contains("requirements form a cycle: events/a.yaml -> events/b.yaml -> events/a.yaml", ex.Message);
    }
```

In `WarEvents1948Tests.cs:89-90`:

```csharp
        if (chain.Count == 0) Assert.Empty(e.Requires);
        else { var r = Assert.Single(e.Requires); Assert.Equal(chain[0].Event, r.Event); Assert.Equal(chain[0].Option, r.Option); }
```

In `SchemaTests.cs:194`, change the pointer `"/properties/requires"` to `"/definitions/requirement"`.

- [ ] **Step 2: Run the tests to see them fail.**

```bash
echo "dotnet test sim/Statecraft.sln --filter 'FullyQualifiedName~EventRequiresTests|FullyQualifiedName~SchemaTests|FullyQualifiedName~WarEvents1948Tests'"
dotnet test sim/Statecraft.sln --filter 'FullyQualifiedName~EventRequiresTests|FullyQualifiedName~SchemaTests|FullyQualifiedName~WarEvents1948Tests'
```
Expected: build errors (`Timeline` does not exist; `Requires` is not a list).

- [ ] **Step 3: Implement.** In `Content.cs`:

```csharp
    /// <summary>One precondition of an event: <see cref="Event"/> was resolved with <see cref="Option"/>, or, with
    /// <see cref="Diverged"/>, with any option but its historical one (R-ALT-001, R-ALT-002).</summary>
    public sealed class EventRequirement
    {
        public string Event { get; set; } = "";
        public string Option { get; set; } = "";
        public bool Diverged { get; set; }
    }
```

and in `GameEvent`:

```csharp
        /// <summary>Eligible only once every one of these holds (R-ALT-001). Empty: no requirement.</summary>
        public List<EventRequirement> Requires { get; set; } = new List<EventRequirement>();
```

Create `sim/Statecraft.Sim/Timeline.cs`:

```csharp
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Where the player's course stands against the record. Every answer is derived from
    /// <see cref="GameState.ChosenOptions"/> and the content, so nothing here is state (docs/superpowers/plans/2026-10-17-alternate-timelines.md, A.4.1).</summary>
    public static class Timeline
    {
        public static EventOption? HistoricalOption(GameEvent e) => e.Options.FirstOrDefault(o => o.Historical);

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        /// <summary>R-ALT-002: the event was resolved, it has a historical option, and the chosen option is another.</summary>
        public static bool Diverged(Content c, GameState s, string eventId)
        {
            if (!s.ChosenOptions.TryGetValue(eventId, out var picked)) return false;
            var historical = HistoricalOption(Get(c, eventId));
            return historical != null && picked != historical.Id;
        }

        /// <summary>R-ALT-001 and R-ALT-002: one requirement holds.</summary>
        public static bool Met(Content c, GameState s, EventRequirement r)
        {
            if (r.Diverged) return Diverged(c, s, r.Event);
            return s.ChosenOptions.TryGetValue(r.Event, out var picked) && picked == r.Option;
        }

        /// <summary>R-ALT-001: every requirement of the event holds (true when it has none).</summary>
        public static bool AllMet(Content c, GameState s, GameEvent e) => e.Requires.All(r => Met(c, s, r));
    }
}
```

In `EventSystem.cs:26`, replace the line with:

```csharp
                .Where(e => Timeline.AllMet(c, s, e))
```

In `EventRules.cs`, replace `:59-66` with:

```csharp
                for (int i = 0; i < e.Requires.Count; i++) CheckRequirement(c, e, $"requires[{i}]", e.Requires[i]);
```

and add, next to `CheckRequirementCycles`:

```csharp
        /// <summary>R-ALT-001 and R-ALT-002: a requirement names an existing other event and either one of its options or
        /// `diverged: true`, which needs that event to have a historical option.</summary>
        internal static void CheckRequirement(Content c, GameEvent e, string at, EventRequirement r)
        {
            string F(string field) => $"events[{e.Id}].{at}{field}";
            if (r.Event == e.Id) throw new ContentException(e.File, F(".event"), "an event cannot require itself");
            var required = c.Events.FirstOrDefault(x => x.Id == r.Event)
                ?? throw new ContentException(e.File, F(".event"), $"unknown event '{r.Event}'");
            if (r.Diverged && r.Option != "") throw new ContentException(e.File, F(""), "give an option or diverged: true, not both");
            if (!r.Diverged && r.Option == "") throw new ContentException(e.File, F(""), "needs an option or diverged: true");
            if (r.Diverged && !required.Options.Any(o => o.Historical))
                throw new ContentException(e.File, F(".diverged"), $"event '{required.Id}' has no historical option, so it cannot diverge");
            if (!r.Diverged && required.Options.All(o => o.Id != r.Option))
                throw new ContentException(e.File, F(".option"), $"event '{required.Id}' has no option '{r.Option}'");
        }
```

Replace `CheckRequirementCycles` (`:136-153`) with a walk over every entry:

```csharp
        /// <summary>Re-review N4: a cycle of requirements (A needs B, B needs A, ...) could never fire. Called after every
        /// requirement's ids are known to exist. It walks each list in order; the error names the files around the first
        /// cycle found, starting from the first event (in load order) that lies on one.</summary>
        static void CheckRequirementCycles(Content c)
        {
            var byId = c.Events.ToDictionary(e => e.Id);
            foreach (var start in c.Events)
            {
                var cycle = CycleFrom(byId, start, start, new List<GameEvent> { start }, new HashSet<string> { start.Id });
                if (cycle != null)
                    throw new ContentException(start.File, $"events[{start.Id}].requires",
                        "requirements form a cycle: " + string.Join(" -> ", cycle.Select(x => x.File)));
            }
        }

        static List<GameEvent>? CycleFrom(Dictionary<string, GameEvent> byId, GameEvent start, GameEvent at, List<GameEvent> path, HashSet<string> seen)
        {
            foreach (var r in at.Requires)
            {
                var next = byId[r.Event];
                if (next.Id == start.Id) return new List<GameEvent>(path) { next };
                if (!seen.Add(next.Id)) continue;   // already walked from start; a cycle not through start is reported from its own member
                path.Add(next);
                var found = CycleFrom(byId, start, next, path, seen);
                if (found != null) return found;
                path.RemoveAt(path.Count - 1);
            }
            return null;
        }
```

In `event.schema.json`, add to `definitions`:

```json
    "requirement": {
      "type": "object",
      "additionalProperties": false,
      "required": ["event"],
      "description": "One precondition (R-ALT-001): the event was resolved with this option, or, with diverged: true, with any option but its historical one (R-ALT-002). Give option or diverged, not both.",
      "properties": {
        "event": { "$ref": "#/definitions/id" },
        "option": { "$ref": "#/definitions/id" },
        "diverged": { "type": "boolean" }
      }
    }
```

and replace `properties.requires` with:

```json
    "requires": { "type": "array", "items": { "$ref": "#/definitions/requirement" }, "description": "Every entry must hold (R-ALT-001)." },
```

Migrate the five files:

```bash
for f in ten_days latrun_proposal jerusalem_ceasefire sinai_withdrawal rhodes_talks; do
  sed -i '' -E 's/^requires: \{ (.*) \}$/requires: [{ \1 }]/' "content/events/$f.yaml"
done
grep -n '^requires:' content/events/*.yaml
```
Expected: five lines, each `requires: [{ event: ..., option: ... }]`.

Update the comment at `ContentLoader.cs:332` to `//   requires: [{ event, option } | { event, diverged: true }, ...]   fires only once every entry holds`. In `docs/WRITING_EVENTS.md`, change the table row (`:40`) to "Fires only after every listed requirement holds. See below." and rewrite the `requires` section (`:115-122`) to show the list form and `diverged: true`, with the loader rules from `CheckRequirement`. Add to `docs/rules/mvp.md`:

```markdown
## Alternate timelines

R-ALT-001: An event's `requires` is a list, and the event is eligible only when every entry holds. An entry `{ event, option }` holds when that event was resolved (chosen or delegated) with that option.

R-ALT-002: An entry `{ event, diverged: true }` holds when that event was resolved with any option except its historical one. An event with no historical option cannot diverge, and the loader refuses `diverged` on it.
```

CHANGELOG under `## [Unreleased]`: "Events: `requires` is now a list whose entries must all hold, and an entry can say `diverged: true` (any option but the historical one) (R-ALT-001, R-ALT-002; refs #939)."

- [ ] **Step 4: Run the tests to see them pass, then the whole suite.**

```bash
echo "dotnet test sim/Statecraft.sln"
dotnet test sim/Statecraft.sln
```
Expected: all pass, 0 warnings, the test count up by the new cases.

- [ ] **Step 5: Confirm no golden moved, from a real run.**

```bash
echo "STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln && git status --short sim/Statecraft.Sim.Tests/golden sim/Statecraft.Sim/GoldenRun.cs"
STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln
git status --short sim/Statecraft.Sim.Tests/golden sim/Statecraft.Sim/GoldenRun.cs
```
Expected: no file listed (the requirements mean the same, so no run changes).

- [ ] **Step 6: Fail-open check.** Change `AllMet` to `e.Requires.Take(1).All(r => Met(c, s, r))`; run `EveryRequirementInTheListMustHold`; expected: the `("go", "agree", false)` row fails. Restore.
- [ ] **Step 7: Rule IDs and repo checks.** Run `uv run scripts/oss/rule_ids.py` and `scripts/check.sh`. Expected: clean.
- [ ] **Step 8: Commit.**

```bash
git add sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/Timeline.cs sim/Statecraft.Sim/EventSystem.cs sim/Statecraft.Sim/EventRules.cs sim/Statecraft.Sim/ContentLoader.cs content/schema/event.schema.json content/events/ten_days.yaml content/events/latrun_proposal.yaml content/events/jerusalem_ceasefire.yaml content/events/sinai_withdrawal.yaml content/events/rhodes_talks.yaml sim/Statecraft.Sim.Tests/EventRequiresTests.cs sim/Statecraft.Sim.Tests/SchemaTests.cs sim/Statecraft.Sim.Tests/WarEvents1948Tests.cs docs/WRITING_EVENTS.md docs/rules/mvp.md CHANGELOG.md
git commit -m "sim: requires as a list, and diverged: true (R-ALT-001, R-ALT-002; refs #939)"
```

**Golden impact:** none (Step 5). **Unity:** rebuild the DLL and run EditMode; no `unity/` change. Q-ALT-10 is decided (a): `ten_days` (sensitivity-reviewed) keeps its status through the one-line form change; the PR lists the diff for the owner to see.

---

### Task 2 (#947): the `alternative` record on options

**Files:**
- Modify: `sim/Statecraft.Sim/Content.cs` (new `OptionAlternative`; `EventOption.Alternative`)
- Modify: `sim/Statecraft.Sim/EventRules.cs` (in the option loop, `:72-95`)
- Modify: `content/schema/event.schema.json` (`definitions.alternative`; `options.items.properties.alternative`)
- Modify: `sim/Statecraft.Sim.Tests/SchemaTests.cs` (`TypeMap`: `/definitions/alternative` to `OptionAlternative`)
- Create: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs`
- Modify: `docs/WRITING_EVENTS.md` (options table and a new `### alternative` section), `docs/rules/mvp.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: Task 1's `Timeline`.
- Produces: `OptionAlternative { string Basis; string By; DateTime? Date; List<Source> Sources }`; `EventOption.Alternative` (null when absent); constants `OptionAlternative.Proposed = "proposed"`, `Debated = "debated"`, `Design = "design"`; `bool OptionAlternative.IsConsidered` (proposed or debated).

- [ ] **Step 1: Write the failing tests** in a new `AlternateTimelineTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The alternate-timeline mechanism (docs/superpowers/plans/2026-10-17-alternate-timelines.md).</summary>
public class AlternateTimelineTests
{
    internal const string Parent = @"
id: parent
title: Parent
text: 'A real decision.'
from: 1948-06-01
to: 1948-06-11
options:
  - id: record
    text: What happened
    historical: true
  - id: road
    text: The road not taken
    alternative:
      basis: proposed
      by: the opposition benches
      date: 1948-06-05
      sources: [{ url: ""https://example.org/minutes"", checked: 2026-10-17 }]
";

    internal static Content Load(params (string file, string yaml)[] events) =>
        ContentLoader.Load(Fixture.Write(events: events.ToDictionary(x => x.file, x => x.yaml)));

    [Fact]
    // R-ALT-003
    public void TheAlternativeRecordIsReadFromYaml()
    {
        var a = Load(("parent.yaml", Parent)).Events.Single().Options.Single(o => o.Id == "road").Alternative!;
        Assert.Equal("proposed", a.Basis);
        Assert.Equal("the opposition benches", a.By);
        Assert.Equal(new DateTime(1948, 6, 5), a.Date);
        Assert.Equal("https://example.org/minutes", Assert.Single(a.Sources).Url);
        Assert.True(a.IsConsidered);
    }

    // R-ALT-003
    [Theory]
    [InlineData("basis: proposed", "basis: maybe", "alternative.basis", "must be proposed, debated or design")]
    [InlineData("by: the opposition benches", "by: ''", "alternative.by", "a proposed or debated alternative names who put it forward")]
    [InlineData("by: the opposition benches", "by: '{prime_minister}'", "alternative.by", "words only, no placeholder")]
    [InlineData("      date: 1948-06-05\n", "", "alternative.date", "needs the date it was put forward")]
    [InlineData("sources: [{ url: \"https://example.org/minutes\", checked: 2026-10-17 }]", "sources: []", "alternative.sources", "needs at least one source")]
    [InlineData("https://example.org/minutes", "http://example.org/minutes", "alternative.sources", "needs an https url and a checked date")]
    public void ABadAlternativeIsALoadError(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent.Replace(from, to))));
        Assert.Equal("events[parent].options[road]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    // R-ALT-003
    public void ADesignAlternativeNamesNobody()
    {
        var design = Parent.Replace("basis: proposed", "basis: design");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", design)));
        Assert.Equal("events[parent].options[road].alternative.by", ex.Field);
        Assert.Contains("a design alternative names nobody and no date", ex.Message);
        var bare = design.Replace("      by: the opposition benches\n      date: 1948-06-05\n", "");
        Assert.False(Load(("parent.yaml", bare)).Events.Single().Options.Single(o => o.Id == "road").Alternative!.IsConsidered);
    }

    [Fact]
    // R-ALT-003
    public void TheHistoricalOptionHasNoAlternativeRecord()
    {
        var bad = Parent.Replace("    historical: true\n", "    historical: true\n    alternative: { basis: design }\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", bad)));
        Assert.Equal("events[parent].options[record].alternative", ex.Field);
        Assert.Contains("the historical option is the record, not an alternative", ex.Message);
    }
}
```

Add to `SchemaTests.TypeMap`: `yield return new object[] { "event.schema.json", "/definitions/alternative", typeof(OptionAlternative) };`

- [ ] **Step 2: Run them to see them fail.**

```bash
echo "dotnet test sim/Statecraft.sln --filter 'FullyQualifiedName~AlternateTimelineTests|FullyQualifiedName~SchemaTests'"
dotnet test sim/Statecraft.sln --filter 'FullyQualifiedName~AlternateTimelineTests|FullyQualifiedName~SchemaTests'
```
Expected: build error, `OptionAlternative` does not exist.

- [ ] **Step 3: Implement.** In `Content.cs`:

```csharp
    /// <summary>Who put forward a non-historical option, and on what record (R-ALT-003). `proposed` and `debated` name a
    /// body or office in words and the date, with sources; `design` is the GDD's "design assumption": nobody is recorded
    /// proposing it, so it names nobody.</summary>
    public sealed class OptionAlternative
    {
        public const string Proposed = "proposed", Debated = "debated", Design = "design";
        public string Basis { get; set; } = "";
        public string By { get; set; } = "";
        public DateTime? Date { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
        /// <summary>True for an alternative contemporaries are recorded proposing or debating (the Ferguson test).</summary>
        [YamlIgnore] public bool IsConsidered => Basis == Proposed || Basis == Debated;
    }
```

and in `EventOption`: `public OptionAlternative? Alternative { get; set; }`.

In `EventRules.Validate`, inside the option loop after `CheckPlaceholders(... o.Text ...)`:

```csharp
                    if (o.Alternative != null) CheckAlternative(e, o, F($"options[{o.Id}].alternative"));
```

and add:

```csharp
        /// <summary>R-ALT-003: the shape of an option's `alternative` record.</summary>
        static void CheckAlternative(GameEvent e, EventOption o, string at)
        {
            var a = o.Alternative!;
            if (o.Historical) throw new ContentException(e.File, at, "the historical option is the record, not an alternative");
            if (a.Basis != OptionAlternative.Proposed && a.Basis != OptionAlternative.Debated && a.Basis != OptionAlternative.Design)
                throw new ContentException(e.File, at + ".basis", $"'{a.Basis}' must be proposed, debated or design");
            if (a.Basis == OptionAlternative.Design)
            {
                if (a.By != "" || a.Date.HasValue) throw new ContentException(e.File, at + ".by", "a design alternative names nobody and no date");
                return;
            }
            if (a.By.Trim() == "") throw new ContentException(e.File, at + ".by", "a proposed or debated alternative names who put it forward");
            if (a.By.IndexOfAny(new[] { '{', '}' }) >= 0) throw new ContentException(e.File, at + ".by", "words only, no placeholder: name the body or office");
            if (!a.Date.HasValue) throw new ContentException(e.File, at + ".date", "needs the date it was put forward");
            if (a.Sources.Count == 0) throw new ContentException(e.File, at + ".sources", "needs at least one source");
            foreach (var src in a.Sources)
                if (!src.Url.StartsWith("https://", StringComparison.Ordinal) || src.Checked == default)
                    throw new ContentException(e.File, at + ".sources", "needs an https url and a checked date");
        }
```

In `event.schema.json`, add `definitions.alternative` (object, `additionalProperties: false`, `required: ["basis"]`, `basis` enum `proposed|debated|design`, `by` string, `date` `#/definitions/date`, `sources` array of the same `{url, checked}` object as the top-level `sources` items) and `"alternative": { "$ref": "#/definitions/alternative" }` in the option properties. In `docs/WRITING_EVENTS.md`, add the `alternative` row to the options table and a section with the example from the test and the rule that `by` names a body or office, never a person. `docs/rules/mvp.md`:

```markdown
R-ALT-003: A non-historical option may carry `alternative: { basis, by, date, sources }`. `basis` is `proposed` or `debated` (a body or office, in words, is recorded putting it forward on that date; at least one https source with a checked date) or `design` (nobody is recorded proposing it: a design assumption, which names nobody and no date). The historical option carries none.
```

CHANGELOG: "Events: options can record who proposed an alternative and on what source (`alternative`, R-ALT-003; refs #939)."

- [ ] **Step 4: Run the tests to see them pass, then the whole suite.** `dotnet test sim/Statecraft.sln`. Expected: all pass, 0 warnings.
- [ ] **Step 5: Fail-open check.** Delete the `if (a.Sources.Count == 0)` line; run `ABadAlternativeIsALoadError`; expected: the `sources: []` row fails. Restore.
- [ ] **Step 6: `uv run scripts/oss/rule_ids.py` and `scripts/check.sh`.** Expected: clean.
- [ ] **Step 7: Commit.**

```bash
git add sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/EventRules.cs content/schema/event.schema.json sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs sim/Statecraft.Sim.Tests/SchemaTests.cs docs/WRITING_EVENTS.md docs/rules/mvp.md CHANGELOG.md
git commit -m "sim: the alternative record on options (R-ALT-003; refs #939)"
```

**Golden impact:** none (no content uses it yet). **Unity:** DLL rebuild and EditMode.

---

### Task 3 (#948): gate G2, the independent fact-check and the premise table

**Files:**
- Create: `research_notes/Alternate timelines/roads-checked.md`

**Interfaces:**
- Consumes: the leads of §A.6 (unverified).
- Produces: for each of the 30 alternatives, a row Task 4 copies into `alternative` and Tasks 11 to 14 draft from.

- [ ] **Step 1: Assign the check to an agent that did not write the leads**, with this brief: open every URL in §A.6 and in each event file's `sources`; for each alternative record (a) the basis (proposed / debated / design) with who, when, a verbatim excerpt under 15 words, the URL and the day opened; (b) each expected consequence with view-holder, date, verbatim excerpt under 15 words, URL and day opened, and "one source" or "several sources"; (c) each lead that fails, marked **dropped** with the reason. Put no owner email or identifier in any request.
- [ ] **Step 2: Write the premise table** in the same note: for each alternative, every later event whose `text` asserts something that alternative contradicts, quoting the file and line (for example `ten_days.yaml:4`, "fighting has resumed"), and one of: no conflict / suppress / replace / variant, as a proposal for the owner.
- [ ] **Step 3: A person checks the note** against its sources and writes "checked by <handle>, <date>" at the top in their own commit. Until then the note is a draft and no later task uses it.
- [ ] **Step 4: Run the checks.** `uv run scripts/oss/md_links.py && uv run scripts/oss/check_repo.py`. Expected: clean.
- [ ] **Step 5: Commit.**

```bash
git add "research_notes/Alternate timelines/roads-checked.md"
git commit -m "research: the roads not taken, fact-checked, with the premise table (refs #939)"
```

**Golden impact:** none. **Unity:** none.

---

### Task 4 (#949): `alternative` on all 30 alternatives; the loader then requires it

> **Decided (owner, 17 Oct 2026, "Accept all recommendations", [#945](https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172)):** Q-ALT-10 and Q-ALT-12 as recorded above. **Then decided (owner, 18 Oct 2026, "all defaults"; [#949](https://github.com/eyalgolan/statecraft-israel/issues/949#issuecomment-6249500866)):** roads with only a date range use the first day of the range in the `YYYY-MM-DD` field, flagged approximate.

**Files:**
- Modify: the 25 `content/events/*.yaml` files that have a non-historical option (each such option)
- Modify: `sim/Statecraft.Sim/EventRules.cs` (the option loop)
- Modify: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs`, `RealContentTests.cs`
- Modify: `docs/rules/mvp.md`, `docs/WRITING_EVENTS.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: Task 2's `OptionAlternative`; Task 3's checked rows; Q-ALT-10 and Q-ALT-12.
- Produces: every non-historical option of an event with a historical option has an `alternative` (R-ALT-004).

- [ ] **Step 1: Write the failing tests.** In `AlternateTimelineTests.cs`:

```csharp
    [Fact]
    // R-ALT-004
    public void AnAlternativeWithoutARecordIsALoadError()
    {
        var bare = Parent.Substring(0, Parent.IndexOf("    alternative:", StringComparison.Ordinal));
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", bare)));
        Assert.Equal("events[parent].options[road].alternative", ex.Field);
        Assert.Contains("an option that did not happen says who proposed it, or basis: design", ex.Message);
    }
```

In `RealContentTests.cs`:

```csharp
    /// <summary>R-ALT-004 on the real content: every one of the 30 alternatives carries its record.</summary>
    [Fact]
    public void EveryAlternativeInContentSaysWhoProposedIt()
    {
        var c = ContentLoader.Load(ContentRoot());
        var alternatives = c.Events.Where(e => e.Options.Any(o => o.Historical)).SelectMany(e => e.Options.Where(o => !o.Historical)).ToList();
        Assert.Equal(30, alternatives.Count);
        Assert.All(alternatives, o => Assert.NotNull(o.Alternative));
    }
```

- [ ] **Step 2: Run them to see them fail.** `dotnet test sim/Statecraft.sln --filter 'FullyQualifiedName~AlternateTimelineTests|FullyQualifiedName~RealContentTests'`. Expected: both fail (no rule; no records).
- [ ] **Step 3: Add the records.** For each alternative, copy Task 3's checked row exactly. A `proposed` or `debated` row:

```yaml
    alternative:
      basis: debated
      by: <the body or office, in words, from the checked row>
      date: <the date from the checked row>
      sources: [{ url: "<the checked URL>", checked: <the day it was opened> }]
```

A row with no recorded advocate (Q-ALT-12 (a), decided):

```yaml
    alternative: { basis: design }
```

Then add the rule in `EventRules.Validate`'s option loop:

```csharp
                    // R-ALT-004: an option that did not happen says who proposed it, or that it is a design assumption.
                    if (!o.Historical && e.Options.Any(x => x.Historical) && o.Alternative == null)
                        throw new ContentException(e.File, F($"options[{o.Id}].alternative"), "an option that did not happen says who proposed it, or basis: design");
```

`docs/rules/mvp.md`:

```markdown
R-ALT-004: Every non-historical option of an event that has a historical option carries an `alternative` record (R-ALT-003).
```

`docs/WRITING_EVENTS.md`: state that the record is required. CHANGELOG: "Content: every option that did not happen now says who proposed it, with sources, or that it is a design assumption (R-ALT-004; refs #939)."

- [ ] **Step 4: Run the whole suite.** `dotnet test sim/Statecraft.sln`. Expected: pass, 0 warnings.
- [ ] **Step 5: Confirm no golden moved** (the Task 1 Step 5 command). Expected: no file listed.
- [ ] **Step 6: Fail-open check.** Remove the `alternative` block from `first_truce.yaml`'s `refuse`; run `dotnet test sim/Statecraft.sln --filter FullyQualifiedName~RealContentTests`; expected: a load error naming `content/events/first_truce.yaml`. Restore.
- [ ] **Step 7: Paste `uv run scripts/pr_content_summary.py origin/main` into the PR.** List the six sensitivity-reviewed files touched, for Q-ALT-10. Q-ALT-10 is decided (a), so their status stays.
- [ ] **Step 8: `scripts/check.sh`, then commit.**

```bash
git add content/events sim/Statecraft.Sim/EventRules.cs sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs sim/Statecraft.Sim.Tests/RealContentTests.cs docs/rules/mvp.md docs/WRITING_EVENTS.md CHANGELOG.md
git commit -m "content: who proposed each road not taken (R-ALT-004; refs #939)"
```

**Golden impact:** none (Step 5). **Unity:** DLL rebuild and EditMode.

---

### Task 5 (#950): consequence cards, the Ferguson gate, the depth cap, tag inheritance, the soft-slot priority

**Files:**
- Modify: `sim/Statecraft.Sim/Content.cs` (`GameEvent.Kind`)
- Modify: `sim/Statecraft.Sim/EventRules.cs` (new `CheckKinds`, called after `CheckRequirementCycles`)
- Modify: `sim/Statecraft.Sim/EventSystem.cs:32`
- Modify: `content/schema/event.schema.json` (`kind`)
- Create: `sim/Statecraft.Sim.Tests/AltRun.cs`
- Modify: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs`, `EventRetimingTests.cs:31-37`, `EventRulesGddTests.cs:199-201`
- Modify: `docs/rules/mvp.md`, `docs/WRITING_EVENTS.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: Tasks 1, 2 and 4.
- Produces: `string GameEvent.Kind` (`"historical"` default, `"consequence"`, `"reconvergence"`); constants `EventKinds.Historical`, `EventKinds.Consequence`, `EventKinds.Reconvergence`; `const int EventRules.MaxConsequenceDepth = 2`; `static bool EventRules.IsAlternativeRoad(Content c, EventRequirement r)`; test helper `AltRun.Play(Content c, IReadOnlyDictionary<string, string> forced)` returning `List<GameState>`.

- [ ] **Step 1: Write the failing tests** in `AlternateTimelineTests.cs`:

```csharp
    internal const string Consequence = @"
id: after_road
kind: consequence
title: After the road
text: 'What the record says was feared.'
from: 1948-06-15
to: 1948-08-15
requires: [{ event: parent, option: road }]
options:
  - id: hold
    text: Hold the course
    alternative:
      basis: debated
      by: the cabinet
      date: 1948-06-20
      sources: [{ url: ""https://example.org/cabinet"", checked: 2026-10-17 }]
";

    static ContentException LoadError(string consequence) =>
        Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", consequence)));

    const string RecordToo = "options:\n  - id: record_too\n    text: As recorded\n    historical: true\n";

    // R-ALT-005
    [Theory]
    [InlineData("requires: [{ event: parent, option: road }]\n", "", "requires", "a consequence card follows a road not taken")]
    [InlineData("option: road", "option: record", "requires", "a consequence card follows a road not taken")]
    [InlineData("options:\n", RecordToo, "options", "a consequence card has no historical option")]
    [InlineData("kind: consequence", "kind: sideways", "kind", "must be historical, consequence or reconvergence")]
    public void AConsequenceCardHasItsShape(string from, string to, string field, string message)
    {
        var ex = LoadError(Consequence.Replace(from, to));
        Assert.Equal("events[after_road]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    // R-ALT-005
    public void AConsequenceCardIsSoft()
    {
        // A hard anchor needs a historical option (EventRules.cs:69), so the fixture gives it one; the soft rule is checked first.
        var ex = LoadError(Consequence.Replace("kind: consequence\n", "kind: consequence\nanchor: hard\n").Replace("options:\n", RecordToo));
        Assert.Equal("events[after_road].anchor", ex.Field);
        Assert.Contains("a consequence card is soft", ex.Message);
    }

    [Fact]
    // R-ALT-005
    public void EveryOptionOfAConsequenceCardHasARecord()
    {
        var ex = LoadError(Consequence.Substring(0, Consequence.IndexOf("    alternative:", StringComparison.Ordinal)));
        Assert.Equal("events[after_road].options[hold].alternative", ex.Field);
    }

    [Fact]
    // R-ALT-006
    public void NoConsequenceFollowsADesignAlternative()
    {
        var design = Parent.Replace("      basis: proposed\n      by: the opposition benches\n      date: 1948-06-05\n      sources: [{ url: \"https://example.org/minutes\", checked: 2026-10-17 }]\n", "      basis: design\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", design), ("after_road.yaml", Consequence)));
        Assert.Equal("events[after_road].requires[0]", ex.Field);
        Assert.Contains("follows 'parent/road', which nobody is recorded proposing (basis: design)", ex.Message);
    }

    static string Chained(string id, string parent, string option) => Consequence
        .Replace("id: after_road", "id: " + id)
        .Replace("requires: [{ event: parent, option: road }]", $"requires: [{{ event: {parent}, option: {option} }}]");

    [Fact]
    // R-ALT-005
    public void ARoadRunsAtMostTwoCardsDeep()
    {
        Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("second.yaml", Chained("second", "after_road", "hold")));
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", Consequence),
            ("second.yaml", Chained("second", "after_road", "hold")), ("third.yaml", Chained("third", "second", "hold"))));
        Assert.Equal("events[third].requires", ex.Field);
        Assert.Contains("3 cards deep; the most is 2", ex.Message);
    }

    [Fact]
    // R-ALT-007
    public void AConsequenceOfASensitiveEventCarriesItsTags()
    {
        var sensitive = Parent.Replace("from: 1948-06-01\n", "from: 1948-06-01\ntags: [war-casualties]\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", sensitive), ("after_road.yaml", Consequence)));
        Assert.Equal("events[after_road].tags", ex.Field);
        Assert.Contains("must carry 'war-casualties', as 'parent' does", ex.Message);
        // With the tag inherited, the existing no-gain rule (EventRules.cs:92-93) refuses a reward.
        var tagged = Consequence.Replace("kind: consequence\n", "kind: consequence\ntags: [war-casualties]\n")
            .Replace("    text: Hold the course\n", "    text: Hold the course\n    effects: [{ target: morale, delta: 0.01 }]\n");
        var gain = Assert.Throws<ContentException>(() => Load(("parent.yaml", sensitive), ("after_road.yaml", tagged)));
        Assert.Contains("sensitive events cannot reward morale", gain.Message);
    }

    static GameState NewState(Content c)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 15), RngState = 3, Mode = Mode.Historical };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        return s;
    }

    static string Soft(string id) =>
        $"id: {id}\ntitle: {id}\ntext: 'Ordinary.'\nfrom: 1948-06-01\nto: 1948-08-15\noptions:\n  - id: ok\n    text: Ok\n";

    [Fact]
    // R-ALT-008
    public void ConsequenceCardsTakeTheFirstSoftSlots()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("a1.yaml", Soft("a1")), ("a2.yaml", Soft("a2")), ("a3.yaml", Soft("a3")));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road";
        s.FiredEvents.Add("parent");
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Equal(new[] { "after_road", "a1", "a2" }, s.PendingEvents);   // a3 waits; the road's card is not crowded out
    }

    [Fact]
    // R-ALT-008
    public void WithNoConsequenceCardTheOrderIsUnchanged()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("a1.yaml", Soft("a1")), ("a2.yaml", Soft("a2")), ("a3.yaml", Soft("a3")));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "record";
        s.FiredEvents.Add("parent");
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Equal(new[] { "a1", "a2", "a3" }, s.PendingEvents);
    }

    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    [Fact]
    // R-ALT-012
    public void TheHistoricalRunRaisesNoConsequenceCard()
    {
        var end = GoldenRun.PlayToEnd(Real);
        var raised = Real.Events.Where(e => e.Kind == EventKinds.Consequence && end.FiredEvents.Contains(e.Id)).Select(e => e.Id).ToList();
        Assert.True(raised.Count == 0, "the historical run raised: " + string.Join(", ", raised));
        Assert.All(end.ChosenOptions, kv => Assert.False(Timeline.Diverged(Real, end, kv.Key), $"the historical run diverged at {kv.Key}"));
    }

    /// <summary>Review focus 2: every consequence card in content/ fires in a run that takes its road (and, for a second
    /// card, the first card's option too), with the reference player's orders everywhere else.</summary>
    [Fact]
    public void EveryConsequenceCardFiresWhenItsRoadIsTaken()
    {
        foreach (var e in Real.Events.Where(x => x.Kind == EventKinds.Consequence))
        {
            var forced = AltRun.RoadTo(Real, e);
            var run = AltRun.Play(Real, forced);
            Assert.True(run.Any(s => s.FiredEvents.Contains(e.Id)), $"{e.Id} never fired on the road {string.Join(", ", forced.Select(kv => kv.Key + "/" + kv.Value))}");
        }
    }

    [Fact]
    public void AltRunFindsAFixtureCard()
    {
        // The real-content test above has nothing to check until a slice lands; this pins that AltRun works.
        var forced = AltRun.RoadTo(Load(("parent.yaml", Parent), ("after_road.yaml", Consequence)), Load(("parent.yaml", Parent), ("after_road.yaml", Consequence)).Events.Single(x => x.Id == "after_road"));
        Assert.Equal(new Dictionary<string, string> { ["parent"] = "road" }, forced);
    }
```

In `EventRetimingTests.cs:31-37`, change `EveryEventFiresInTheHistoricalMonthlyRun` to count historical-kind events only:

```csharp
        var record = C.Events.Where(e => e.Kind != EventKinds.Consequence).ToList();
        var missing = record.Select(e => e.Id).Where(id => CardDate(id) == null).ToList();
        Assert.True(missing.Count == 0, "never fired: " + string.Join(", ", missing));
        Assert.Equal(32, record.Count);
```

In `EventRulesGddTests.cs:199-201`, `ThePreviewListsEveryEffectOnTheRealContent` takes each event's state from the historical run, where a consequence card never fires; change its loop to `foreach (var e in C.Events.Where(x => x.Kind != EventKinds.Consequence))` and add, after the loop, the same check for each consequence card on a state from `AltRun.Play(C, AltRun.RoadTo(C, e))` where it has fired.

Create `AltRun.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>A run that leaves the record: the reference player's orders (GoldenRun.Orders) on the golden seed, except that
/// each event in <c>forced</c> is answered with the given option. Test code, a stated policy, like the scenarios.</summary>
public static class AltRun
{
    public static List<GameState> Play(Content c, IReadOnlyDictionary<string, string> forced)
    {
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var run = new List<GameState> { s };
        while (s.GameOver == null && run.Count < 1000)
        {
            var orders = GoldenRun.Orders(c, s)
                .Select(o => o is ChooseEventOption ch && forced.TryGetValue(ch.EventId, out var opt) ? new ChooseEventOption(ch.EventId, opt) : o)
                .ToList();
            s = Simulation.Step(c, s, orders).State;
            run.Add(s);
        }
        return run;
    }

    /// <summary>The choices that lead to <paramref name="card"/>: for each requirement, its option, or for `diverged: true`
    /// the event's first non-historical option in file order; walked back through a parent consequence card.</summary>
    public static Dictionary<string, string> RoadTo(Content c, GameEvent card)
    {
        var road = new Dictionary<string, string>();
        foreach (var r in card.Requires)
        {
            var parent = c.Events.First(x => x.Id == r.Event);
            road[r.Event] = r.Diverged ? parent.Options.First(o => !o.Historical).Id : r.Option;
            if (parent.Kind == EventKinds.Consequence)
                foreach (var kv in RoadTo(c, parent)) road[kv.Key] = kv.Value;
        }
        return road;
    }
}
```

- [ ] **Step 2: Run them to see them fail.** `dotnet test sim/Statecraft.sln --filter 'FullyQualifiedName~AlternateTimelineTests|FullyQualifiedName~EventRetimingTests'`. Expected: build error (`EventKinds`, `Kind` do not exist).
- [ ] **Step 3: Implement.** In `Content.cs`:

```csharp
    /// <summary>The kinds of event (R-ALT-005, R-ALT-010).</summary>
    public static class EventKinds
    {
        public const string Historical = "historical", Consequence = "consequence", Reconvergence = "reconvergence";
    }
```

and in `GameEvent`: `public string Kind { get; set; } = EventKinds.Historical;`.

In `EventRules.cs`:

```csharp
        /// <summary>R-ALT-005: how far a road runs before it folds back. DESIGN VALUE: owner-set (Q-ALT-5), not a historical figure.</summary>
        public const int MaxConsequenceDepth = 2;

        /// <summary>A requirement that names a road not taken: `diverged: true`, a non-historical option of an event that has
        /// a historical option, or any option of a consequence card.</summary>
        public static bool IsAlternativeRoad(Content c, EventRequirement r)
        {
            var parent = c.Events.First(x => x.Id == r.Event);
            if (r.Diverged || parent.Kind == EventKinds.Consequence) return true;
            return parent.Options.Any(o => o.Historical) && parent.Options.Any(o => o.Id == r.Option && !o.Historical);
        }

        /// <summary>R-ALT-005 to R-ALT-007. Called after every requirement is known to exist and to form no cycle.</summary>
        static void CheckKinds(Content c)
        {
            var depth = new Dictionary<string, int>();
            int Depth(GameEvent e) => depth.TryGetValue(e.Id, out var d) ? d : depth[e.Id] =
                e.Kind != EventKinds.Consequence ? 0 : 1 + e.Requires.Select(r => Depth(c.Events.First(x => x.Id == r.Event))).DefaultIfEmpty(0).Max();
            foreach (var e in c.Events)
            {
                string F(string field) => $"events[{e.Id}].{field}";
                if (e.Kind != EventKinds.Historical && e.Kind != EventKinds.Consequence && e.Kind != EventKinds.Reconvergence)
                    throw new ContentException(e.File, F("kind"), $"'{e.Kind}' must be historical, consequence or reconvergence");
                if (e.Kind != EventKinds.Consequence) continue;
                if (e.Anchor == "hard") throw new ContentException(e.File, F("anchor"), "a consequence card is soft: it exists only on a road not taken");
                if (e.Options.Any(o => o.Historical)) throw new ContentException(e.File, F("options"), "a consequence card has no historical option");
                if (!e.Requires.Any(r => IsAlternativeRoad(c, r)))
                    throw new ContentException(e.File, F("requires"), "a consequence card follows a road not taken: at least one requirement names an option that did not happen, or diverged: true");
                foreach (var o in e.Options.Where(o => o.Alternative == null))
                    throw new ContentException(e.File, F($"options[{o.Id}].alternative"), "every option of a consequence card says who proposed it, or basis: design");
                // R-ALT-006, the Ferguson gate (Q-ALT-3): the road must be one contemporaries are recorded proposing or debating.
                for (int i = 0; i < e.Requires.Count; i++)
                {
                    var r = e.Requires[i];
                    if (!IsAlternativeRoad(c, r)) continue;
                    var parent = c.Events.First(x => x.Id == r.Event);
                    var roads = r.Diverged ? parent.Options.Where(o => !o.Historical) : parent.Options.Where(o => o.Id == r.Option);
                    foreach (var o in roads.Where(o => o.Alternative == null || !o.Alternative.IsConsidered))
                        throw new ContentException(e.File, F($"requires[{i}]"), $"follows '{parent.Id}/{o.Id}', which nobody is recorded proposing (basis: design)");
                    // R-ALT-007: a card on a sensitive road carries the road's sensitive tags, so the no-gain rule covers it.
                    foreach (var tag in parent.Tags.Where(SensitiveTags.Contains).OrderBy(t => t, StringComparer.Ordinal))
                        if (!e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                            throw new ContentException(e.File, F("tags"), $"must carry '{tag}', as '{parent.Id}' does");
                }
                if (Depth(e) > MaxConsequenceDepth)
                    throw new ContentException(e.File, F("requires"), FormattableString.Invariant($"this road runs {Depth(e)} cards deep; the most is {MaxConsequenceDepth} (Q-ALT-5)"));
            }
        }
```

Call `CheckKinds(c);` after `CheckRequirementCycles(c);` in `Validate`. `Validate`'s per-event loop runs first, so an untagged card reaches `CheckKinds` and gets the missing-tag error, and a tagged card with a gain is refused by the existing no-gain check; `AConsequenceOfASensitiveEventCarriesItsTags` pins both.

In `EventSystem.cs:32`, replace the ordering with:

```csharp
                // R-ALT-008: a road the player took is not crowded out of its window. With no consequence card eligible every key
                // is equal and OrderBy is stable, so the order is the From-then-id order of before.
                .OrderBy(e => e.Kind == EventKinds.Consequence ? 0 : 1).ThenBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal)
```

Schema: `"kind": { "enum": ["historical", "consequence", "reconvergence"], "description": "Default historical. consequence: a card only on a road not taken (R-ALT-005). reconvergence: a real outside event that may carry variants (R-ALT-010)." }`. `docs/rules/mvp.md`:

```markdown
R-ALT-005: An event of `kind: consequence` is soft, has no historical option, has at least one requirement naming a road not taken (`diverged: true`, a non-historical option, or an option of another consequence card), carries an `alternative` record on every option, and lies at most 2 cards deep from the record (a labelled design value, Q-ALT-5).

R-ALT-006: A consequence card may follow only an alternative whose `alternative.basis` is `proposed` or `debated` (the Ferguson test, Q-ALT-3).

R-ALT-007: A consequence card carries every sensitive tag of each event whose road it follows, so the no-gain rule covers it.

R-ALT-008: Consequence cards take a turn's soft slots before the other soft events, then by `From` and id. A `front_quiet` armistice (Q-EVT-1, Phase 5 PR 13) will come before them.
```

and reword R-EVT-002 to: "The soft events take the free slots in order: consequence cards first (R-ALT-008), then the rest by `from`, then id (ordinal). The decided place of an early armistice ahead of the other soft events (Q-EVT-1, through `front_quiet`, Q-OBJ-1) lands with that key in Phase 5 PR 13 (#752), and this rule is reworded then." `EventRulesGddTests.SoftEventsTakeTheSlotsInOrderOfFromThenId` must still pass unchanged.

```markdown

R-ALT-012: The historical run (the reference player's orders from 15 May 1948 to 15 January 1952) raises no consequence card and diverges nowhere.
```

`docs/WRITING_EVENTS.md`: a `### kind` section with the consequence example from the test and the authoring rules (absolute windows that open after the parent's window; text limited to stated threats, promises and recorded fears; office placeholders). CHANGELOG: "Events: consequence cards, which follow only a road contemporaries proposed, at most 2 deep, inherit sensitive tags and take the first soft slots (R-ALT-005 to R-ALT-008, R-ALT-012; refs #939)."

- [ ] **Step 4: Run the whole suite.** `dotnet test sim/Statecraft.sln`. Expected: pass, 0 warnings.
- [ ] **Step 5: Confirm no golden moved** (Task 1 Step 5). Expected: no file listed.
- [ ] **Step 6: Fail-open checks.** (a) Delete the `foreach (var o in roads.Where(...))` throw: `NoConsequenceFollowsADesignAlternative` fails. (b) Change the ordering key to `1` for every kind: `ConsequenceCardsTakeTheFirstSoftSlots` fails. (c) Delete the tag check: `AConsequenceOfASensitiveEventCarriesItsTags` fails. Restore each.
- [ ] **Step 7: `uv run scripts/oss/rule_ids.py` and `scripts/check.sh`.**
- [ ] **Step 8: Commit.**

```bash
git add sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/EventRules.cs sim/Statecraft.Sim/EventSystem.cs content/schema/event.schema.json sim/Statecraft.Sim.Tests/AltRun.cs sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs sim/Statecraft.Sim.Tests/EventRetimingTests.cs sim/Statecraft.Sim.Tests/EventRulesGddTests.cs docs/rules/mvp.md docs/WRITING_EVENTS.md CHANGELOG.md
git commit -m "sim: consequence cards with the Ferguson gate (R-ALT-005 to R-ALT-008, R-ALT-012; refs #939)"
```

**Golden impact:** none (no consequence card in content). **Unity:** DLL rebuild and EditMode.

---

### Task 6 (#951): `rejoin`, the foldback

**Files:**
- Modify: `sim/Statecraft.Sim/Content.cs` (`EventOption.Rejoin`)
- Modify: `sim/Statecraft.Sim/Timeline.cs` (`Rejoined`; `Met`)
- Modify: `sim/Statecraft.Sim/EventRules.cs` (`CheckKinds`)
- Modify: `content/schema/event.schema.json` (`rejoin` on options)
- Modify: `sim/Statecraft.Sim/EventSystem.cs:103-115` (`OptionEffects`: the rejoin line, R-EVT-005)
- Modify: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs`, `SaveGameTests.cs`, `EventRulesGddTests.cs:204-207`
- Modify: `docs/rules/mvp.md`, `docs/WRITING_EVENTS.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: Task 5.
- Produces: `string EventOption.Rejoin` (empty when none); `bool Timeline.Rejoined(Content c, GameState s, string eventId)`; `Timeline.Met` now also holds for `{event: E, option: <E's historical option>}` when `Rejoined(c, s, E)`.

- [ ] **Step 1: Write the failing tests** in `AlternateTimelineTests.cs`:

```csharp
    static string WithRejoin => Consequence.Replace("    text: Hold the course\n", "    text: Hold the course\n    rejoin: parent\n");

    internal const string RecordFollowUp = @"
id: on_record
title: On the record
text: 'The next real decision.'
from: 1948-07-01
to: 1948-09-15
requires: [{ event: parent, option: record }]
options:
  - id: ok
    text: Ok
";

    [Fact]
    // R-ALT-009
    public void ARejoinReopensOnlyCardsStillInsideTheirWindow()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin), ("on_record.yaml", RecordFollowUp));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road"; s.FiredEvents.Add("parent");
        s.Date = new DateTime(1948, 7, 15);
        EventSystem.Trigger(c, s, new List<Cause>(), new DateTime(1948, 6, 15));
        Assert.DoesNotContain("on_record", s.FiredEvents);                 // off the record: the follow-up waits
        Assert.Null(EventSystem.Choose(c, s, "after_road", "hold", new List<Cause>()));
        Assert.True(Timeline.Rejoined(c, s, "parent"));
        EventSystem.Trigger(c, s, new List<Cause>(), new DateTime(1948, 6, 15));
        Assert.Contains("on_record", s.FiredEvents);                       // its window is still open

        var late = NewState(c);
        late.ChosenOptions["parent"] = "road"; late.ChosenOptions["after_road"] = "hold";
        late.FiredEvents.Add("parent"); late.FiredEvents.Add("after_road");
        late.Date = new DateTime(1948, 10, 15);
        EventSystem.Trigger(c, late, new List<Cause>(), new DateTime(1948, 9, 15));
        Assert.DoesNotContain("on_record", late.FiredEvents);             // its window closed on 15 September: no resurrection
    }

    [Fact]
    // R-ALT-009
    public void TheTimelineKeepsTheRoadAfterARejoin()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road"; s.ChosenOptions["after_road"] = "hold";
        Assert.Equal("road", s.ChosenOptions["parent"]);                   // the record of the road is not rewritten
        Assert.True(Timeline.Diverged(c, s, "parent"));
        Assert.True(Timeline.Met(c, s, new EventRequirement { Event = "parent", Option = "record" }));
        Assert.True(Timeline.Met(c, s, new EventRequirement { Event = "parent", Option = "road" }));   // and the road itself still holds
    }

    // R-ALT-009
    [Theory]
    [InlineData("rejoin: parent", "rejoin: nowhere", "rejoin", "unknown event 'nowhere'")]
    [InlineData("rejoin: parent", "rejoin: after_road", "rejoin", "a card cannot rejoin itself")]
    public void ABadRejoinIsALoadError(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin.Replace(from, to))));
        Assert.Equal("events[after_road].options[hold]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    // R-ALT-009
    public void OnlyAConsequenceCardRejoinsAndOnlyARoadItFollows()
    {
        var other = "id: other\ntitle: Other\ntext: x\nfrom: 1948-06-01\noptions:\n  - id: a\n    text: A\n    historical: true\n  - id: b\n    text: B\n    alternative: { basis: design }\n";
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("other.yaml", other), ("after_road.yaml", WithRejoin.Replace("rejoin: parent", "rejoin: other"))));
        Assert.Contains("rejoins only an event whose road this card follows", ex.Message);
        var onRecord = RecordFollowUp.Replace("    text: Ok\n", "    text: Ok\n    rejoin: parent\n");
        var ex2 = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("on_record.yaml", onRecord)));
        Assert.Contains("only a consequence card rejoins", ex2.Message);
    }
```

Also, for R-EVT-005:

```csharp
    [Fact]
    // R-ALT-009, R-EVT-005: the card's effect list names the way back.
    public void TheEffectListNamesTheRejoin()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin));
        var o = c.Events.Single(x => x.Id == "after_road").Options.Single();
        var line = Assert.Single(EventSystem.OptionEffects(c, NewState(c), o));
        Assert.Equal(("rejoin", 0.0), (line.Target, line.Delta));
        Assert.Equal("Returns to the recorded course: Parent", line.Message);
    }
```

In `EventRulesGddTests.cs:204-206`, append `.Concat(o.Rejoin != "" ? new[] { ("rejoin", 0.0) } : new (string, double)[0])` to `expected`.

In `SaveGameTests.cs`, add a test that a fixture game that takes `road`, answers `after_road` with `hold`, then saves and loads, replays to the same `Hash()` and `ChosenOptions` (use the file's existing save-then-load helper and the fixture above).

- [ ] **Step 2: Run them to see them fail.** `dotnet test sim/Statecraft.sln --filter 'FullyQualifiedName~AlternateTimelineTests|FullyQualifiedName~SaveGameTests'`. Expected: build error (`Rejoin`, `Rejoined`).
- [ ] **Step 3: Implement.** `EventOption`: `public string Rejoin { get; set; } = "";`. In `Timeline.cs`:

```csharp
        /// <summary>R-ALT-009: a consequence card's chosen option rejoined this event to the record.</summary>
        public static bool Rejoined(Content c, GameState s, string eventId) =>
            c.Events.Any(e => e.Kind == EventKinds.Consequence && s.ChosenOptions.TryGetValue(e.Id, out var o)
                              && e.Options.Any(x => x.Id == o && x.Rejoin == eventId));
```

and change `Met`'s last line to:

```csharp
            if (!s.ChosenOptions.TryGetValue(r.Event, out var picked)) return false;
            if (picked == r.Option) return true;
            // R-ALT-009: after a rejoin, the record's own option counts as taken; `diverged` stays true.
            return Rejoined(c, s, r.Event) && HistoricalOption(Get(c, r.Event))?.Id == r.Option;
```

In `EventRules.CheckKinds`, before `if (e.Kind != EventKinds.Consequence) continue;`:

```csharp
                foreach (var o in e.Options.Where(o => o.Rejoin != ""))
                {
                    var at = F($"options[{o.Id}].rejoin");
                    if (e.Kind != EventKinds.Consequence) throw new ContentException(e.File, at, "only a consequence card rejoins the record");
                    if (o.Rejoin == e.Id) throw new ContentException(e.File, at, "a card cannot rejoin itself");
                    var target = c.Events.FirstOrDefault(x => x.Id == o.Rejoin) ?? throw new ContentException(e.File, at, $"unknown event '{o.Rejoin}'");
                    if (!target.Options.Any(x => x.Historical) || !e.Requires.Any(r => r.Event == target.Id && IsAlternativeRoad(c, r)))
                        throw new ContentException(e.File, at, "rejoins only an event whose road this card follows, and which has a historical option");
                }
```

In `EventSystem.OptionEffects`, after the security-level line:

```csharp
            // R-ALT-009: the way back to the record is shown with the other effects (R-EVT-005).
            if (o.Rejoin != "")
                lines.Add(new Cause("events", "rejoin", 0, $"Returns to the recorded course: {Cabinet.Fill(c, s, c.Events.First(x => x.Id == o.Rejoin).Title, s.Date)}"));
```

and add "then the way back to the record (`rejoin`, R-ALT-009)" to R-EVT-005's list in `docs/rules/mvp.md`. Schema: `"rejoin": { "$ref": "#/definitions/id", "description": "A consequence card's option that returns the course to the record of this event (R-ALT-009)." }` in option properties. `docs/rules/mvp.md`:

```markdown
R-ALT-009: An option of a consequence card may `rejoin` an event whose road the card follows (and which has a historical option). Once it is chosen, a requirement on that event's historical option holds, so the record's later cards fire again if their windows are still open. The event's own chosen option is not rewritten, and `diverged: true` on it still holds.
```

CHANGELOG: "Events: a consequence card's option can rejoin the record (`rejoin`, R-ALT-009; refs #939)."

- [ ] **Step 4: Run the whole suite.** Expected: pass, 0 warnings.
- [ ] **Step 5: Confirm no golden moved** (Task 1 Step 5).
- [ ] **Step 6: Fail-open check.** Make `Rejoined` return `false`; `ARejoinReopensOnlyCardsStillInsideTheirWindow` fails. Restore.
- [ ] **Step 7: `uv run scripts/oss/rule_ids.py`, `scripts/check.sh`, commit.**

```bash
git add sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/Timeline.cs sim/Statecraft.Sim/EventRules.cs sim/Statecraft.Sim/EventSystem.cs content/schema/event.schema.json sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs sim/Statecraft.Sim.Tests/SaveGameTests.cs sim/Statecraft.Sim.Tests/EventRulesGddTests.cs docs/rules/mvp.md docs/WRITING_EVENTS.md CHANGELOG.md
git commit -m "sim: rejoin, the foldback to the record (R-ALT-009; refs #939)"
```

**Golden impact:** none. **Unity:** DLL rebuild and EditMode.

---

### Task 7 (#952): reconvergence events and variant text

**Files:**
- Modify: `sim/Statecraft.Sim/Content.cs` (new `EventVariant`; `GameEvent.Variants`)
- Modify: `sim/Statecraft.Sim/Timeline.cs` (`Variant`)
- Modify: `sim/Statecraft.Sim/EventSystem.cs:41`, `:64-65`, `:146` (`Title`, `Render`)
- Modify: `sim/Statecraft.Sim/EventRules.cs` (`CheckKinds`; placeholders through `CheckPlaceholders`)
- Modify: `content/schema/event.schema.json` (`definitions.variant`; `variants`)
- Modify: `unity/Assets/Scripts/UI/EventCardPresenter.cs:219` (title through `EventSystem.Title`)
- Modify: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs`, `SchemaTests.cs` (`TypeMap`: `/definitions/variant` to `EventVariant`)
- Modify: `docs/rules/mvp.md`, `docs/WRITING_EVENTS.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: Tasks 1 and 5.
- Produces: `EventVariant { List<EventRequirement> When; string Title; string Text; List<Source> Sources }`; `List<EventVariant> GameEvent.Variants`; `EventVariant? Timeline.Variant(Content c, GameState s, GameEvent e)`; `string EventSystem.Title(Content c, GameState s, GameEvent e)`.

- [ ] **Step 1: Write the failing tests** in `AlternateTimelineTests.cs`:

```csharp
    internal const string Outside = @"
id: outside
kind: reconvergence
title: The outside event
text: 'It happened regardless.'
from: 1948-07-01
to: 1948-08-01
options:
  - id: record
    text: As it happened
    historical: true
variants:
  - when: [{ event: parent, diverged: true }]
    title: The outside event, on your course
    text: 'It happened regardless; on your course, the earlier decision went the other way.'
    sources: [{ url: ""https://example.org/outside"", checked: 2026-10-17 }]
";

    [Theory]
    // R-ALT-010
    [InlineData("road", "The outside event, on your course", "It happened regardless; on your course")]
    [InlineData("record", "The outside event", "It happened regardless.")]
    public void TheFirstMatchingVariantSuppliesTheText(string picked, string title, string text)
    {
        var c = Load(("parent.yaml", Parent), ("outside.yaml", Outside));
        var s = NewState(c);
        s.ChosenOptions["parent"] = picked;
        var e = c.Events.Single(x => x.Id == "outside");
        Assert.Equal(title, EventSystem.Title(c, s, e));
        Assert.StartsWith(text, EventSystem.Render(c, s, e));
        Assert.Equal("As it happened", e.Options.Single().Text);             // options never vary
    }

    // R-ALT-010
    [Theory]
    [InlineData("kind: reconvergence\n", "", "variants", "only a reconvergence event carries variants")]
    [InlineData("  - when: [{ event: parent, diverged: true }]", "  - when: [{ event: parent, option: record }]", "variants[0].when", "a variant follows a road not taken")]
    [InlineData("  - when: [{ event: parent, diverged: true }]", "  - when: []", "variants[0].when", "a variant follows a road not taken")]
    [InlineData("    sources: [{ url: \"https://example.org/outside\", checked: 2026-10-17 }]\n", "", "variants[0].sources", "needs at least one source")]
    [InlineData("on your course, the earlier", "on your course, {nobody} and the earlier", "variants[0].text", "unknown office placeholder '{nobody}'")]
    public void ABadVariantIsALoadError(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("outside.yaml", Outside.Replace(from, to))));
        Assert.Equal("events[outside]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    // R-ALT-010, review focus 5
    public void AVariantNeedsItsRoadClosedBeforeTheCardOpens()
    {
        var early = Outside.Replace("from: 1948-07-01", "from: 1948-06-05");   // parent's window runs to 11 June
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("outside.yaml", early)));
        Assert.Equal("events[outside].variants[0].when[0]", ex.Field);
        Assert.Contains("'parent' can still be open when this card opens", ex.Message);
    }

    [Fact]
    // R-ALT-012
    public void TheHistoricalRunShowsNoVariant()
    {
        var end = GoldenRun.PlayToEnd(Real);
        foreach (var e in Real.Events.Where(x => x.Variants.Count > 0))
            Assert.Null(Timeline.Variant(Real, end, e));
    }

    /// <summary>Review focus 5 on real content: at the state where the card first appears on its road, the variant applies.</summary>
    [Fact]
    public void EveryVariantShowsWhenItsRoadIsTaken()
    {
        foreach (var e in Real.Events.Where(x => x.Variants.Count > 0))
            for (int i = 0; i < e.Variants.Count; i++)
            {
                var road = AltRun.RoadTo(Real, new GameEvent { Id = e.Id, Requires = e.Variants[i].When });
                var fired = AltRun.Play(Real, road).FirstOrDefault(s => s.FiredEvents.Contains(e.Id));
                Assert.NotNull(fired);
                Assert.Same(e.Variants[i], Timeline.Variant(Real, fired!, e));
            }
    }
```

- [ ] **Step 2: Run them to see them fail.** Expected: build error.
- [ ] **Step 3: Implement.** `Content.cs`:

```csharp
    /// <summary>R-ALT-010: alternative wording of a reconvergence event for a course that left the record. Options and
    /// effects never vary.</summary>
    public sealed class EventVariant
    {
        public List<EventRequirement> When { get; set; } = new List<EventRequirement>();
        /// <summary>Empty: the event's own title.</summary>
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public List<Source> Sources { get; set; } = new List<Source>();
    }
```

and `GameEvent`: `public List<EventVariant> Variants { get; set; } = new List<EventVariant>();`.

`Timeline.cs`:

```csharp
        /// <summary>R-ALT-010: the first variant, in file order, whose every `when` entry holds; null for the event's own text.</summary>
        public static EventVariant? Variant(Content c, GameState s, GameEvent e) =>
            e.Variants.FirstOrDefault(v => v.When.All(r => Met(c, s, r)));
```

`EventSystem.cs`: add

```csharp
        /// <summary>The card's title as the player sees it now: a matching variant's, else the event's (R-ALT-010).</summary>
        public static string Title(Content c, GameState s, GameEvent e)
        {
            var v = Timeline.Variant(c, s, e);
            return Cabinet.Fill(c, s, v != null && v.Title != "" ? v.Title : e.Title, FillDate(e, s.Date));
        }
```

change `Render(Content c, GameState s, GameEvent e)` (`:146`) to `Cabinet.Fill(c, s, Timeline.Variant(c, s, e)?.Text ?? e.Text, FillDate(e, s.Date))`, and use `Title(c, s, e)` in the `Trigger` cause (`:41`) and for `title` in `Resolve` (`:64`). In `EventCardPresenter.cs:219`: `card.Q<Label>("event-title").text = EventSystem.Title(c, s, e);`.

`EventRules.CheckKinds`, for every event:

```csharp
                if (e.Variants.Count > 0 && e.Kind != EventKinds.Reconvergence)
                    throw new ContentException(e.File, F("variants"), "only a reconvergence event carries variants");
                if (e.Kind == EventKinds.Reconvergence && !e.Options.Any(o => o.Historical))
                    throw new ContentException(e.File, F("options"), "a reconvergence event happened: it has a historical option");
                for (int i = 0; i < e.Variants.Count; i++)
                {
                    var v = e.Variants[i];
                    var at = $"variants[{i}]";
                    for (int j = 0; j < v.When.Count; j++)
                    {
                        CheckRequirement(c, e, $"{at}.when[{j}]", v.When[j]);
                        var road = c.Events.First(x => x.Id == v.When[j].Event);
                        // Review focus 5: the road is settled before the card opens, so the text never flips while it is shown.
                        if (!road.To.HasValue || road.To.Value > e.From)
                            throw new ContentException(e.File, F($"{at}.when[{j}]"), $"'{road.Id}' can still be open when this card opens: its window must close on or before {e.From:yyyy-MM-dd}");
                    }
                    if (!v.When.Any(r => IsAlternativeRoad(c, r)))
                        throw new ContentException(e.File, F($"{at}.when"), "a variant follows a road not taken: at least one entry names an option that did not happen, or diverged: true");
                    if (v.Text.Trim() == "") throw new ContentException(e.File, F($"{at}.text"), "a variant has text");
                    if (v.Sources.Count == 0) throw new ContentException(e.File, F($"{at}.sources"), "needs at least one source");
                    if (v.Title != "") CheckPlaceholders(c, e, F($"{at}.title"), v.Title, offices);
                    CheckPlaceholders(c, e, F($"{at}.text"), v.Text, offices);
                }
```

`CheckKinds` needs `offices`: change its signature to `CheckKinds(Content c, HashSet<string> offices)` and pass it from `Validate`. Format the date with `FormattableString.Invariant`. Schema: `definitions.variant` and `"variants": { "type": "array", "items": { "$ref": "#/definitions/variant" } }`. `docs/rules/mvp.md`:

```markdown
R-ALT-010: An event of `kind: reconvergence` has a historical option and may carry `variants`. The first variant whose every `when` entry holds supplies the card's title (if it gives one) and text; options and effects never vary. Each variant follows a road not taken, carries sources, has its placeholders checked over the event's window, and names only roads whose windows close on or before the event's `from`. Only a reconvergence event carries variants.
```

CHANGELOG: "Events: reconvergence events can bend their text to the player's course (`variants`, R-ALT-010; refs #939)."

- [ ] **Step 4: Run the whole suite.** Expected: pass, 0 warnings.
- [ ] **Step 5: Confirm no golden moved** (Task 1 Step 5). Cause text is not hashed.
- [ ] **Step 6: Fail-open check.** Delete the window-close check; `AVariantNeedsItsRoadClosedBeforeTheCardOpens` fails. Restore.
- [ ] **Step 7: Unity.** `tools/build_sim_for_unity.sh`, then EditMode under the machine lock. `EventCardTests.TheCardShowsThePendingEventWithTheMinisterAndOptions` must still pass ("Armistice with Egypt"). Paste the results XML total.
- [ ] **Step 8: `uv run scripts/oss/rule_ids.py`, `scripts/check.sh`, commit.**

```bash
git add sim/Statecraft.Sim/Content.cs sim/Statecraft.Sim/Timeline.cs sim/Statecraft.Sim/EventSystem.cs sim/Statecraft.Sim/EventRules.cs content/schema/event.schema.json unity/Assets/Scripts/UI/EventCardPresenter.cs sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs sim/Statecraft.Sim.Tests/SchemaTests.cs docs/rules/mvp.md docs/WRITING_EVENTS.md CHANGELOG.md
git commit -m "sim: reconvergence events and variant text (R-ALT-010; refs #939)"
```

**Golden impact:** none. **Unity:** yes (one line; EditMode run required).

---

### Task 8 (#953): the timeline helper, the release gate and the coverage report

**Files:**
- Modify: `sim/Statecraft.Sim/Timeline.cs` (`TimelineEntry`, `Entries`, `Origin`)
- Modify: `sim/Statecraft.Sim/ReleaseGate.cs` (`AlternateUnsigned`)
- Modify: `sim/Statecraft.Cli/Program.cs:62-70` (`--release-gate`), new `--timeline-report`
- Modify: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs`, `FinalWaveTests.cs`
- Modify: `docs/rules/mvp.md`, `docs/WRITING_EVENTS.md`, `docs/m3-release.md` (the gate's description), `CHANGELOG.md`

**Interfaces:**
- Consumes: Tasks 5, 6, 7.
- Produces: `sealed class TimelineEntry { string Event; string Chosen; string Historical; bool Diverged; string RejoinedBy; }`; `IReadOnlyList<TimelineEntry> Timeline.Entries(Content c, GameState s)`; `(string Event, string Option)? Timeline.Origin(Content c, GameState s, GameEvent card)`; `IReadOnlyList<string> ReleaseGate.AlternateUnsigned(Content c)`.

- [ ] **Step 1: Write the failing tests.** In `AlternateTimelineTests.cs`:

```csharp
    [Fact]
    public void TheTimelineListsEachResolvedEventInWindowOrder()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin), ("outside.yaml", Outside));
        var s = NewState(c);
        s.ChosenOptions["outside"] = "record"; s.ChosenOptions["parent"] = "road"; s.ChosenOptions["after_road"] = "hold";
        var rows = Timeline.Entries(c, s);
        Assert.Equal(new[] { "parent", "after_road", "outside" }, rows.Select(r => r.Event));
        var p = rows[0];
        Assert.Equal(("road", "record", true, "after_road"), (p.Chosen, p.Historical, p.Diverged, p.RejoinedBy));
        Assert.Equal(("hold", "", false), (rows[1].Chosen, rows[1].Historical, rows[1].Diverged));
    }

    [Fact]
    public void TheOriginNamesTheRoadTheCardFollows()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road";
        Assert.Equal(("parent", "road"), Timeline.Origin(c, s, c.Events.Single(x => x.Id == "after_road"))!.Value);
        Assert.Null(Timeline.Origin(c, s, c.Events.Single(x => x.Id == "parent")));
    }
```

In `FinalWaveTests.cs`:

```csharp
    /// <summary>R-ALT-011: a consequence card or a variant needs a person's sign-off before a shareable build.</summary>
    [Fact]
    public void AShareableBuildNeedsEveryAlternateCardSignedOff()
    {
        Assert.Empty(ReleaseGate.AlternateUnsigned(Real));
        var c = AlternateTimelineTests.Load(("parent.yaml", AlternateTimelineTests.Parent), ("after_road.yaml", AlternateTimelineTests.Consequence), ("outside.yaml", AlternateTimelineTests.Outside));
        Assert.Equal(new[] { "after_road", "outside" }, ReleaseGate.AlternateUnsigned(c));
        var signed = AlternateTimelineTests.Consequence + "status: fact-checked\nreviewed_by: a-reviewer\nreviewed_on: 2026-10-20\n";
        var c2 = AlternateTimelineTests.Load(("parent.yaml", AlternateTimelineTests.Parent), ("after_road.yaml", signed));
        Assert.Empty(ReleaseGate.AlternateUnsigned(c2));
    }
```

Make `Load`, `Parent`, `Consequence` and `Outside` `internal` (they are, above). The real-content assertion `Assert.Empty(ReleaseGate.AlternateUnsigned(Real))` holds until the first slice; Task 12 changes it to list the slice's cards until the owner signs them, exactly as `SensitiveUnreviewed` is handled.

- [ ] **Step 2: Run them to see them fail.** Expected: build error.
- [ ] **Step 3: Implement.** `Timeline.cs`:

```csharp
        /// <summary>One row of the timeline screen: the player's answer beside the record's.</summary>
        public sealed class TimelineEntry
        {
            public TimelineEntry(string ev, string chosen, string historical, bool diverged, string rejoinedBy)
            { Event = ev; Chosen = chosen; Historical = historical; Diverged = diverged; RejoinedBy = rejoinedBy; }
            public string Event { get; } public string Chosen { get; } public string Historical { get; }
            public bool Diverged { get; } public string RejoinedBy { get; }
        }

        /// <summary>Every resolved event, by window start then ordinal id. <c>Historical</c> is empty for an event with no
        /// historical option; <c>RejoinedBy</c> is the consequence card whose option rejoined it, or empty.</summary>
        public static IReadOnlyList<TimelineEntry> Entries(Content c, GameState s) =>
            c.Events.Where(e => s.ChosenOptions.ContainsKey(e.Id))
                .OrderBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal)
                .Select(e => new TimelineEntry(e.Id, s.ChosenOptions[e.Id], HistoricalOption(e)?.Id ?? "", Diverged(c, s, e.Id),
                    c.Events.Where(k => k.Kind == EventKinds.Consequence && s.ChosenOptions.TryGetValue(k.Id, out var o)
                                        && k.Options.Any(x => x.Id == o && x.Rejoin == e.Id))
                            .Select(k => k.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault() ?? ""))
                .ToList();

        /// <summary>The road a consequence card follows, as the player took it: the first requirement that names a road
        /// not taken, with the option chosen there. Null for any other event.</summary>
        public static (string Event, string Option)? Origin(Content c, GameState s, GameEvent card)
        {
            if (card.Kind != EventKinds.Consequence) return null;
            var r = card.Requires.First(x => EventRules.IsAlternativeRoad(c, x));
            return (r.Event, s.ChosenOptions.TryGetValue(r.Event, out var o) ? o : r.Option);
        }
```

Add `using System; using System.Collections.Generic;` to `Timeline.cs`. `EventRules` is `static class` (internal); make `IsAlternativeRoad` `internal` and keep `Timeline` in the same assembly. `ReleaseGate.cs`:

```csharp
        /// <summary>R-ALT-011 (Q-ALT-9): every consequence card and every event with variants that no person has signed off
        /// (no reviewed_by), in id order. A shareable build refuses while it is not empty.</summary>
        public static IReadOnlyList<string> AlternateUnsigned(Content c) =>
            c.Events.Where(e => (e.Kind == EventKinds.Consequence || e.Variants.Count > 0) && e.ReviewedBy == "")
                .Select(e => e.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
```

In `Program.cs`'s `--release-gate` block, also print each id from `ReleaseGate.AlternateUnsigned(content)` as `  unsigned alternate card: <id>` and exit 3 if either list is non-empty. Add `--timeline-report [--content DIR]`: for each event with a historical option, in window order, for each non-historical option print `<event>/<option>  basis=<basis>  cards=<ids of consequence cards whose requirement names it, or diverged on its event, comma-separated, or ->  variants=<ids of events with a variant naming it, or ->  breaks=<ids of events requiring the event's historical option, or ->`, then exit 0. Add to `docs/rules/mvp.md`:

```markdown
R-ALT-011: A shareable build refuses while any consequence card, or any event with variants, has no `reviewed_by` (a person's sign-off), alongside the sensitivity gate (Q-ALT-9).
```

`docs/WRITING_EVENTS.md`: the `--timeline-report` command in the helpers list. CHANGELOG: "Release gate: consequence cards and variants need a person's sign-off; `Statecraft.Cli --timeline-report` lists each road not taken and what follows it (R-ALT-011; refs #939)."

- [ ] **Step 4: Run the whole suite**, and the CLI:

```bash
echo "dotnet run --project sim/Statecraft.Cli -v q -- --timeline-report --content \"$PWD/content\""
dotnet run --project sim/Statecraft.Cli -v q -- --timeline-report --content "$PWD/content"
```
Expected: 30 lines, one per alternative, each with `cards=-` until the first slice.

- [ ] **Step 5: Fail-open check.** Drop `|| e.Variants.Count > 0` from `AlternateUnsigned`; `AShareableBuildNeedsEveryAlternateCardSignedOff` fails. Restore.
- [ ] **Step 6: `uv run scripts/oss/rule_ids.py`, `scripts/check.sh`, `scripts/oss/release_gate.sh` (it runs `--release-gate`; it must still pass on `main`'s content). Commit.**

```bash
git add sim/Statecraft.Sim/Timeline.cs sim/Statecraft.Sim/ReleaseGate.cs sim/Statecraft.Sim/EventRules.cs sim/Statecraft.Cli/Program.cs sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs sim/Statecraft.Sim.Tests/FinalWaveTests.cs docs/rules/mvp.md docs/WRITING_EVENTS.md docs/m3-release.md CHANGELOG.md
git commit -m "sim: the timeline helper, the alternate-card release gate and the coverage report (R-ALT-011; refs #939)"
```

**Golden impact:** none. **Unity:** DLL rebuild and EditMode.

---

### Task 9 (#954): the banner on a consequence card (Unity)

**Files:**
- Modify: `unity/Assets/UI/Game.uxml:162-163` (a label `event-alt-banner` above `event-title`)
- Modify: `unity/Assets/Scripts/UI/EventCardPresenter.cs:185-232` (`Render`: the banner; the rejoin line under an option)
- Modify: `unity/Assets/UI/strings.en.yaml`, `strings.he.yaml`
- Modify: `unity/Assets/Tests/EditMode/EventCardTests.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: `Timeline.Origin`, `Timeline.HistoricalOption`, `EventKinds`, `EventOption.Rejoin`, `EventOption.Alternative` (Tasks 2, 6, 8); at least one consequence card on `main` (Task 12).

- [ ] **Step 1: Write the failing EditMode tests** in `EventCardTests.cs`:

```csharp
    /// <summary>A game that takes the road of the first consequence card in content/ and plays until that card waits.</summary>
    static (SimBridge bridge, GameEvent card) AtFirstConsequence()
    {
        var card = C.Events.First(e => e.Kind == EventKinds.Consequence);
        var road = card.Requires.Where(r => C.Events.First(x => x.Id == r.Event).Kind != EventKinds.Consequence)
            .ToDictionary(r => r.Event, r => r.Diverged ? C.Events.First(x => x.Id == r.Event).Options.First(o => !o.Historical).Id : r.Option);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (int turn = 0; turn < 60 && !g.State.PendingEvents.Contains(card.Id); turn++)
            g.Step(GoldenRun.Orders(C, g.State).Select(o => o is ChooseEventOption ch && road.TryGetValue(ch.EventId, out var opt) ? new ChooseEventOption(ch.EventId, opt) : o).ToArray());
        Assert.Contains(card.Id, g.State.PendingEvents.ToList());
        return (new SimBridge(g, Text), card);
    }

    [Test]
    public void AConsequenceCardSaysWhichRoadItFollows()
    {
        using var ui = new UiTestKit();
        var (bridge, card) = AtFirstConsequence();
        var presenter = new EventCardPresenter(ui.Root, bridge, Text, AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath));
        while (presenter.Shown!.Id != card.Id) presenter.Next();
        var banner = ui.Q<Label>("event-alt-banner");
        Assert.AreEqual(DisplayStyle.Flex, banner.style.display.value);
        var (parentId, chosen) = Timeline.Origin(C, bridge.State, card)!.Value;
        var parent = C.Events.First(e => e.Id == parentId);
        StringAssert.Contains(parent.Options.First(o => o.Id == chosen).Text, banner.text);
        StringAssert.Contains(Timeline.HistoricalOption(parent)!.Text, banner.text);
    }

    [Test]
    public void AnOrdinaryCardHasNoBanner()
    {
        using var ui = new UiTestKit();
        new EventCardPresenter(ui.Root, AtArmistice(), Text, AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath));
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-alt-banner").style.display.value);
    }
```

If `EventCardPresenter` has no `Next()` and `SimBridge` no `State` under those names, use the presenter's existing way to page between pending cards and the bridge's existing state accessor (read them in `EventCardPresenter.cs:185-232` and `SimBridge.cs` before writing the test); do not add new public API for the test.

- [ ] **Step 2: Run EditMode to see them fail** (under the machine lock, the Global Constraints' command). Expected: `event-alt-banner` not found.
- [ ] **Step 3: Implement.** `Game.uxml`: `<ui:Label name="event-alt-banner" class="alt-banner" />` before `event-title`. In `Render`, after the title:

```csharp
            var banner = card.Q<Label>("event-alt-banner");
            var origin = Timeline.Origin(c, s, e);
            if (origin.HasValue)
            {
                var parent = c.Events.First(x => x.Id == origin.Value.Event);
                var took = parent.Options.First(o => o.Id == origin.Value.Option).Text;
                var record = Timeline.HistoricalOption(parent)?.Text ?? "";
                banner.text = text.Format("event.alt_origin", Cabinet.Fill(c, s, took, s.Date), Cabinet.Fill(c, s, parent.Title, s.Date), Cabinet.Fill(c, s, record, s.Date));
                banner.style.display = DisplayStyle.Flex;
            }
            else banner.style.display = DisplayStyle.None;
```

and for each option with `o.Rejoin != ""`, a small label under its button with `text["event.rejoin"]`. Strings (`strings.en.yaml`):

```yaml
event.alt_origin: "Alternative course. It follows your decision: {0} ({1}). In the record: {2}."
event.rejoin: "Returns to the recorded course."
event.alt_proposed: "Not adopted. Proposed by {0}, {1}."
```

`strings.he.yaml`: the same three keys in Hebrew with the same `{n}` tokens, each preceded by `# agent draft, needs a native speaker's review (#564)`. `Statecraft.uss`: `.alt-banner` in the card's muted text colour (no new colour). CHANGELOG: "Event card: a consequence card names the road it follows and the record beside it (refs #939)."

- [ ] **Step 4: Run EditMode to see them pass;** paste the XML total. Then the WebGL `web_check` with the current golden and smoke values (both unchanged).
- [ ] **Step 5: Screenshots** of the card in English and Hebrew in the PR.
- [ ] **Step 6: Revert the settings churn, `scripts/check.sh`, commit.**

```bash
git checkout -- unity/ProjectSettings unity/Assets/Settings
git add unity/Assets/UI/Game.uxml unity/Assets/Scripts/UI/EventCardPresenter.cs unity/Assets/UI/strings.en.yaml unity/Assets/UI/strings.he.yaml unity/Assets/UI/Statecraft.uss unity/Assets/Tests/EditMode/EventCardTests.cs CHANGELOG.md
git commit -m "unity: the consequence card's banner and the rejoin line (refs #939)"
```

**Golden impact:** none (`smoke5` presses first options, which are historical). **Unity:** yes. Q-ALT-7 is decided (a): `event.alt_proposed` is shown after the choice only (on the follow-up card and in the timeline), never on the button before choosing.

---

### Task 10 (#955): the two-column timeline screen (Unity)

**Files:**
- Create: `unity/Assets/Scripts/UI/TimelinePresenter.cs`
- Modify: `unity/Assets/UI/Game.uxml` (a `timeline-screen` panel and a HUD button `nav-timeline`, beside the existing screen buttons)
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs` (construct the presenter as the other screens are)
- Modify: `unity/Assets/UI/strings.en.yaml`, `strings.he.yaml`, `unity/Assets/UI/Statecraft.uss`
- Create: `unity/Assets/Tests/EditMode/TimelineScreenTests.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: `Timeline.Entries` (Task 8).

- [ ] **Step 1: Write the failing EditMode tests:**

```csharp
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void OneRowPerResolvedEventAndNoMarkOnTheRecord()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < 6; i++) g.Step(GoldenRun.Orders(C, g.State).ToArray());
        var screen = new TimelinePresenter(ui.Root, new SimBridge(g, Text), Text);
        screen.Show();
        var rows = ui.Root.Query<VisualElement>(className: "timeline-row").ToList();
        Assert.AreEqual(Timeline.Entries(C, g.State).Count, rows.Count);
        Assert.AreEqual(0, ui.Root.Query<VisualElement>(className: "timeline-diverged").ToList().Count);
    }

    [Test]
    public void ARoadNotTakenIsMarkedBesideTheRecord()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.Step(new Command[] { new ChooseEventOption("idf_founding", C.Events.First(e => e.Id == "idf_founding").Options.First(o => !o.Historical).Id) });
        var screen = new TimelinePresenter(ui.Root, new SimBridge(g, Text), Text);
        screen.Show();
        var marked = ui.Root.Query<VisualElement>(className: "timeline-diverged").ToList();
        Assert.AreEqual(1, marked.Count);
        StringAssert.Contains(Timeline.HistoricalOption(C.Events.First(e => e.Id == "idf_founding"))!.Text, marked[0].Q<Label>("timeline-record").text);
    }
}
```

`idf_founding` is pending on the first turn (its card comes on 15 May 1948, `EventRetimingTests`), so the first step's command answers it.

- [ ] **Step 2: Run EditMode to see them fail.** Expected: `TimelinePresenter` does not exist.
- [ ] **Step 3: Implement** `TimelinePresenter` after the pattern of the existing screen presenters (`CabinetPresenter.cs`): a constructor `(VisualElement root, SimBridge bridge, UiText text)`, `Show()`, `Hide()`, and a `Render()` that clears `timeline-list` and adds, for each `Timeline.Entries(c, s)` row, a `VisualElement` with class `timeline-row` (plus `timeline-diverged` when `Diverged`) holding `Label`s `timeline-date` (the event's `From` through the existing `date.*` strings), `timeline-title` (`EventSystem.Title`), `timeline-yours` (the chosen option's text), `timeline-record` (the historical option's text, or `text["timeline.no_record"]`) and, when `RejoinedBy != ""`, `text["timeline.rejoined"]`. Re-render on `bridge.Changed`. Strings:

```yaml
timeline.title: "Your course and the record"
timeline.yours: "Your course"
timeline.record: "The record"
timeline.no_record: "No single recorded answer"
timeline.rejoined: "Rejoined the record"
nav.timeline: "Timeline"
```

Hebrew keys with the `#564` review comment. CHANGELOG: "A timeline screen shows the player's course beside the record (refs #939)."

- [ ] **Step 4: EditMode passes** (paste the XML total); `web_check` with the unchanged values; screenshots in English and Hebrew; keyboard focus order checked against `docs/accessibility-audit-2026-10-14.md`.
- [ ] **Step 5: Revert the settings churn, `scripts/check.sh`, commit.**

```bash
git checkout -- unity/ProjectSettings unity/Assets/Settings
git add unity/Assets/Scripts/UI/TimelinePresenter.cs unity/Assets/Scripts/UI/GameRoot.cs unity/Assets/UI/Game.uxml unity/Assets/UI/strings.en.yaml unity/Assets/UI/strings.he.yaml unity/Assets/UI/Statecraft.uss unity/Assets/Tests/EditMode/TimelineScreenTests.cs CHANGELOG.md
git commit -m "unity: the timeline screen, your course beside the record (refs #939)"
```

**Golden impact:** none. **Unity:** yes.

---

### Task 11 (#939): gate G3 for slice 1, the drafts on #939

**Files:** none in the repo (an issue comment).

**Interfaces:**
- Consumes: Task 3's checked note; Q-ALT-3 and Q-ALT-13.
- Produces: one draft per road on #939, which the owner rules on.

- [ ] **Step 1: For each road in Q-ALT-13's answer**, check in the checked note that it passes the Ferguson gate (Q-ALT-3 (a): `proposed` or `debated` only). A road that fails is listed in the comment as "no card: <why>", not drafted.
- [ ] **Step 2: Draft each card** in the comment, in this order: the road (event/option, its `alternative` record); the card's id, `kind: consequence`, window (absolute, opening after the parent's window closes, at least two turns long), office; title and text using office placeholders and only the checked consequences, each sentence followed by its source number; two or three options, each with its `alternative` record from the note, one of them a `rejoin` if the record offers a way back; each option's effects as labelled design values with the reparations wording; sensitive tags inherited from the parent; and the premise table rows this card bends (suppress / replace / variant), as proposals. No quotes beyond the note's verbatim excerpts under 15 words; no person named in any text.
- [ ] **Step 3: Post one comment on #939** headed "Drafts for the owner to edit (not decided). Nothing is built until the owner signs off each card." Put no owner email or identifier in it. Link the checked note on `main`.
- [ ] **Step 4: Record the owner's ruling** per card (accept / change / reject, in their words) in a follow-up comment and in Task 12's PR body.

**Golden impact:** none. **Unity:** none.

---

### Task 12 (#956): slice 1, the accepted cards

**Files:**
- Create: `content/events/<id>.yaml` for each card the owner accepted in Task 11
- Modify: any card on `main` whose premise row the owner accepted as "suppress" (a `requires` entry) or "variant" (in Task 14, not here)
- Modify: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs` (per-card tests), `FinalWaveTests.cs` (the sensitive list and `AlternateUnsigned` expectation), `docs/m3-release.md` (the sensitivity table, for a sensitive card)
- Modify: golden snapshots and manifest only from a real update run, `docs/determinism.md` (history row), `CHANGELOG.md`

**Interfaces:**
- Consumes: Tasks 4, 5, 6, 8; the rulings recorded in Task 11 Step 4.

- [ ] **Step 1: Write the failing per-card tests** in `AlternateTimelineTests.cs`, one `[Theory]` row per accepted card:

```csharp
    // One row per card the owner accepted on #939: the card, the road that brings it, and the turn it reaches the player.
    [Theory]
    [MemberData(nameof(SliceOneCards))]
    public void ASliceOneCardComesOnItsRoad(string card, string ev, string option, string turn)
    {
        var run = AltRun.Play(Real, new Dictionary<string, string> { [ev] = option });
        var first = run.FirstOrDefault(s => s.FiredEvents.Contains(card));
        Assert.NotNull(first);
        Assert.Equal(DateTime.Parse(turn, System.Globalization.CultureInfo.InvariantCulture), first!.Date);
        Assert.DoesNotContain(card, GoldenRun.PlayToEnd(Real).FiredEvents);
    }
```

`SliceOneCards` yields one `object[] { card id, parent event, parent option, "yyyy-MM-dd" }` per accepted card, with the ids and turns from the ruled drafts. Each card's effects are pinned by a row in the same file that reads the card and compares its options' `(target, delta)` lists with the ruled values.

- [ ] **Step 2: Run them to see them fail** (the files do not exist).
- [ ] **Step 3: Write each YAML file** exactly as ruled: `kind: consequence`, `anchor: soft`, the window, `office`, `requires`, `tags`, `title`, `text`, each option with `alternative`, `effects` (each with the comment `# DESIGN VALUE: owner-set, not a historical figure and not a moral judgement (#939, <ruling URL>)`) and `rejoin` where ruled, `sources` with today's `checked:` date, and `status: fact-checked` only after a person has checked each claim against its source. Never `reviewed_by`.
- [ ] **Step 4: Run the whole suite.** `EveryConsequenceCardFiresWhenItsRoadIsTaken` now checks each card. `AShareableBuildNeedsEveryAlternateCardSignedOff`'s real-content line changes to `Assert.Equal(new[] { <the slice's ids, ordinal> }, ReleaseGate.AlternateUnsigned(Real))` until the owner signs them.
- [ ] **Step 5: Re-pin the goldens that moved, from a real run.**

```bash
echo "STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln; dotnet test sim/Statecraft.sln"
STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln
dotnet test sim/Statecraft.sln
git diff --stat sim/Statecraft.Sim.Tests/golden sim/Statecraft.Sim/GoldenRun.cs
```
Expected: only scenario snapshots whose script takes a slice road move (`scenario-reckless` if a card follows `first_truce/refuse`); `hash1950`, `hash1952`, `mvp-end` and `smoke5` unchanged. Add a history row to `docs/determinism.md` naming the cards, and update the places `GoldenManifestTests` names.
- [ ] **Step 6: Coverage.** Paste `dotnet run --project sim/Statecraft.Cli -v q -- --timeline-report --content "$PWD/content"` and `uv run scripts/pr_content_summary.py origin/main` into the PR.
- [ ] **Step 7: Fail-open check.** Change one card's `requires` option to the parent's historical option: the loader refuses it (R-ALT-005). Restore.
- [ ] **Step 8: `scripts/check.sh`, commit.**

```bash
git add content/events sim/Statecraft.Sim.Tests docs/determinism.md docs/m3-release.md CHANGELOG.md
git commit -m "content: the first roads not taken get their sourced follow-up cards (refs #939)"
```

**Golden impact:** as Step 5. **Unity:** a photograph per card only where one meets the event-image ruling (`docs/event-images.md`); otherwise none. **Sign-off:** the owner's sensitivity review for any sensitive card; the owner's `reviewed_by` on each card before a shareable build (R-ALT-011).

---

### Task 13 (#957): slice 2, the home-front and Jerusalem roads

**Files:** as Task 12, for the roads in §A.6 not in slice 1 that pass the gate (the leads name `austerity_1949/decline`, `clothing_rationing_1950/relax`, and, if not in slice 1, `jerusalem_military_government/accept_demilitarisation`; `reparations_claim/refuse` only after #938 lands, and as a sensitive card).

**Interfaces:** as Task 12.

- [ ] **Step 1: Drafts on a new issue** (Task 11's steps 1 to 4, for these roads).
- [ ] **Step 2: Per-card tests** (Task 12 Step 1, with `SliceTwoCards`).
- [ ] **Step 3: YAML exactly as ruled** (Task 12 Step 3).
- [ ] **Step 4: Whole suite; goldens from a real run** (Task 12 Steps 4 and 5).
- [ ] **Step 5: Coverage report and content summary in the PR; fail-open check; `scripts/check.sh`; commit** with `git commit -m "content: follow-up cards for the home-front roads not taken (refs #939)"`.

**Golden impact:** as Task 12. **Unity:** none beyond optional photographs.

---

### Task 14 (#958): variants, suppressions and replacements at the outside events

**Files:**
- Modify: the reconvergence events the premise table names for the roads that now have cards (likely candidates from the inventory: the four armistices, `elections_1949`, `rhodes_talks`), adding `kind: reconvergence` and `variants`
- Modify: any card the owner ruled "suppress" (a `requires` entry on the premise's historical option) and add its ruled replacement card (`kind: consequence`, `requires: [{ event: <premise>, diverged: true }]`)
- Modify: `sim/Statecraft.Sim.Tests/AlternateTimelineTests.cs`, `FinalWaveTests.cs`, `CHANGELOG.md`

**Interfaces:** consumes Tasks 7 and 12; the rulings on the drafts.

- [ ] **Step 1: Drafts on a new issue:** for each row, the card on `main`, the road, the proposed variant text (sourced; office placeholders; it names the difference without saying what "would have happened") or the suppression and its replacement. Owner rules per row.
- [ ] **Step 2: Failing tests:** `EveryVariantShowsWhenItsRoadIsTaken` covers each variant; add one `[InlineData]` per suppressed card to a theory asserting that on its road the card does not fire and its replacement does.
- [ ] **Step 3: Edit the files exactly as ruled.** A card whose text changes follows Q-CUT-4 (its review resets); a variant leaves the card's own text, options and effects unchanged, and under Q-ALT-10 (a) adding `kind` and `variants` keeps its review.
- [ ] **Step 4: Whole suite; goldens from a real run** (only scripted runs that take a road can move; cause text is not hashed).
- [ ] **Step 5: Coverage report in the PR, fail-open check (delete one variant's `when`: the loader refuses it), `scripts/check.sh`, commit** with `git commit -m "content: the outside events bend to the player's course (refs #939)"`. This is the last slice: "Fixes #939" if the owner agrees the gap is closed.

**Golden impact:** as above. **Unity:** none (Task 7 already routes the title).

---

## Self-review

**1. Spec coverage.**
- The divergence model: §A.4.1, Task 1 (`Timeline.Diverged`), Q-ALT-2.
- The option kinds and the Ferguson gate in the loader: §A.4.2, Tasks 2, 4 and 5 (R-ALT-003 to R-ALT-006), Q-ALT-3; the release gate: Task 8 (R-ALT-011), Q-ALT-9.
- The `requires` extensions: the list (AND) and `diverged` in Task 1; relative timing decided out (Q-ALT-4 (a)).
- Reconvergence, foldback and broken premises: §A.4.4, Task 6 (`rejoin`), Task 14 (suppress / replace / variant), Q-ALT-6.
- Variant text: §A.4.5, Task 7.
- The UI: the banner in Task 9, the timeline in Task 10, Q-ALT-7 and Q-ALT-8.
- The content pipeline and its gates: §A.4.8; Tasks 3 (G2), 11 (G3, G4), 12 to 14 (G5 to G7).
- Phases 5 to 7: §A.4.9; R-ALT-008 sits after R-EVT-002; R-WAR-006 untouched; Task 0 records the Phase 5 note.
- Open mode: Q-ALT-1. YAGNI: §A.4.10.
- The owner's goal, part by part (§A.3): the dilemmas (Tasks 5, 12, 13), the bending (Tasks 6, 7, 14), the delicacy (Tasks 3, 5, 8, 11 and the gates).

**2. Placeholder scan.** The only angle-bracketed values left are in Task 4's YAML template, Task 11's draft order and Task 12's design-value comment, and each says where the value comes from (the checked note or the owner's ruling). They are inputs from a gated step, not unfinished design. No "TBD" or "similar to Task N" remains.

**3. Type consistency.** `EventRequirement.Diverged`, `GameEvent.Requires : List<EventRequirement>`, `GameEvent.Kind`, `EventKinds.*`, `OptionAlternative` (`Basis`, `By`, `Date`, `Sources`, `IsConsidered`), `EventOption.Alternative`, `EventOption.Rejoin`, `EventVariant` (`When`, `Title`, `Text`, `Sources`), `GameEvent.Variants`, `Timeline.{HistoricalOption, Diverged, Met, AllMet, Rejoined, Variant, Origin, Entries}`, `TimelineEntry`, `EventRules.{CheckRequirement, IsAlternativeRoad, CheckKinds, MaxConsequenceDepth}`, `EventSystem.Title`, `ReleaseGate.AlternateUnsigned`, `AltRun.{Play, RoadTo}`: each is defined in the task named in its Interfaces block and used with the same name and signature later. `CheckKinds` gains its `offices` parameter in Task 7, which says so.

**4. Review Focus.** Each of the five has its test in the owning task: 1 in Tasks 5 and 7; 2 in Task 5; 3 in Task 6; 4 in Task 5; 5 in Task 7. A further risk with no test: a delegated alternative on a soft card brings a consequence card the player did not pick (Q-ALT-11); the banner text in Task 9 can say the minister decided once the answer is known.
