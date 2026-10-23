# Civ-style advisors: design spec

**Status:** APPROVED by the owner on 23 October 2026: "the advisors design looks good. approved" (in the coordinator
session). This file writes the approved design out for the owner's review of the text; it changes no code. Nothing is
built until this PR is merged, and every slice below is its own PR.
**Date:** 23 October 2026. **Base:** origin/main `2a4c95a3` (#1477 merged). Every file:line below was read on that commit.
**Plan:** none yet. The plan is written after this spec merges, slice by slice (section 17).

## 1. The request and the goal

The owner (23 October 2026, verbatim): "we need to add advisors like in civ ... ben gurion will be a general assistanat
about game mechanisems, golda meir will explain about stuff that are more specific but not military and moshe dayan will
explain military ... best that they completly replace [the existing tutorials]. Use our elevenlabs subscription to make
the videos for these."

**Goal.** Three advisors, as in Civilization VI, teach each mechanic the first time the player meets it and then give
short warnings and suggestions as the game goes on. They replace the first-turn guide and the How to play screen. A
setting controls how much they speak. They never change the simulation, the save or a golden hash: like the guide today,
they are presentation only (`FirstTurnGuidePresenter.cs:19-20`).

## 2. The rulings

The owner's answers to our questions (23 October 2026, coordinator session, verbatim):

| Question | Ruling |
|---|---|
| The advisors' role | "Teach + advise (Civ VI style)": teach each mechanic when first met, and give ongoing warnings and suggestions; a setting controls how chatty they are |
| Synthetic voices | "you can sythesize their voices". This lifts the project's earlier rule that a synthetic voice never speaks as a real person (section 13.1). ElevenLabs' own terms still apply (section 13.2) |
| Their words | "Labelled game dialogue": advisor lines are original game dialogue in the persona, shown in the advisor panel and never styled as quotes. Real quotes come only from a verified list, styled as quotes with their source. Nothing puts an advisor on a contested issue beyond neutral game facts |
| Media | "2+videos of them explaining", then, asked what that means, "The real figures talking" |
| Golda's name | "Golda Meyerson (later Meir) (Recommended)" |
| The whole design | "the advisors design looks good. approved" |

How "The real figures talking" is carried out (part of the approved design): the real figures appear to speak only after
written consent from their estates (the Ben-Gurion Heritage Institute; Golda Meir's family; Moshe Dayan's heirs). Until
then each video is a **photo video**: the archival portrait, a slow pan, the game's UI highlights and captions, narrated by
a **fictional, named aide** for each advisor, in a voice made with ElevenLabs Voice Design (a designed voice, not a clone;
no real name in the prompt), in English and Hebrew (section 13).

Already decided, and kept as they are:

- **The perspective and sourcing rules** of `docs/CONTENT_POLICY.md` sections 1 and 2. This PR amends principle 2 and the
  AI-likeness lines (`CONTENT_POLICY.md:28-29`, `:99-100`) to match the rulings above (section 19).
- **Portraits** are public domain, CC0, CC BY 4.0 or CC BY-SA 4.0 photographs (`CONTENT_POLICY.md:35-39`, `:80-83`).
- **In-game AI narration** (15 October 2026, #639, recorded in
  `.github/skills/statecraft-video-script/references/06-ai-voice-music-and-disclosure.md:28`): allowed in its own voice,
  never as a real person, disclosed, with an `ASSETS.yaml` row. The aide narration follows it as it stands. Only slice S7
  (real-figure video, after consent) needs that file's rules 1 and 9 (`:16`, `:24`) amended; S7 does it.
- **ElevenLabs output in the game** is recorded in `ASSETS.yaml` as AI-generated (the 18 October ruling on #39,
  `docs/oss/DECISIONS.md:82`).

## 3. Personas and voice rules

### 3.1 Who covers what

| Advisor | UI label | Covers |
|---|---|---|
| David Ben-Gurion | Advisor | The whole game: turns and End turn, Attention, objectives and score, events and delegation, defeat, the big picture |
| Golda Meyerson (later Meir) | Advisor | Cities, housing and jobs, immigration and refugees, training civilians, city projects, pioneer work, corridors, Money and debt, the budget, the cabinet, Food, the national meters (Social cohesion, International Support) |
| Moshe Dayan | Advisor | Units, movement and go-to, standing orders, combat, Arms, truces, capture, Send home, the enemy's phase |

**Why "Advisor" and not their office.** Their 1948 offices do not fit the role. Golda Meyerson was Israel's envoy in
Moscow until March 1949 and Minister of Labour from 10 March 1949 (`content/people.yaml:603`); Dayan was a battalion
commander and then commander in Jerusalem in 1948, and became an Aluf only in October 1949 (`content/people.yaml:1118-1134`
holds no office for Dayan before 1953, and no row holds the Moscow post, so these facts are sourced before any text names them). Calling all three
"Advisor" is a game role, and nothing in the UI claims they held it.

**The name.** The panel shows "Golda Meyerson (later Meir)" on the advisor's first appearance and "Golda Meyerson" after it, in
Hebrew "גולדה מאירסון (לימים מאיר)". `content/people.yaml:585` keeps "Golda Meir" for the cabinet; the advisor's display
name lives in the advisor data file, not in `people.yaml`.

### 3.2 What they may say

- **Game dialogue, labelled.** Every line is original game text in the advisor's persona. It is shown in the advisor panel
  under the label "Advisor", never in quotation marks, never in italics with an attribution, never in the style the game
  uses for a real quote. It describes the game's own mechanics and state: "Our debt is close to the ceiling; one more
  loan and the economy breaks."
- **Neutral game facts only.** No advisor gives a view on a contested issue: the refugees' flight, Deport, the borders,
  the Arab states. On a capture, Dayan explains the three answers and their costs as the game computes them
  (`SettlementControl.CaptureOptions`, R-CTRL-008, `docs/rules/mvp.md:47`) and recommends none; Deport is never
  suggested, never praised, and never the first option (R-CTRL-008).
- **Real quotes only from the verified list** (section 3.3), each shown as a quote with its source line under it, and only
  where the list says it may be used.
- **No made-up history.** A line may name a date, place or number only when the game's content already sources it.
- **Plain English** by `docs/WRITING_UI.md`, and Hebrew by `docs/WRITING_UI_HE.md` (section 14). Lines are at most two
  sentences in the popup; the encyclopedia page may be longer.
- **The owner reviews every advisor line** before it ships (section 17).

### 3.3 The verified quotes list

From a research note checked on 23 October 2026 (Codex cx285). Only these may appear, and only as labelled quotes:

| # | Speaker | Text | Source | Use |
|---|---|---|---|---|
| Q1 | The Declaration (a document, not a personal line) | "THE STATE OF ISRAEL will be open for Jewish immigration and for the Ingathering of the Exiles;" Hebrew: «מדינת ישראל תהא פתוחה לעליה יהודית ולקיבוץ גלויות;» | Declaration of the Establishment of the State of Israel, *Iton Rishmi* no. 1, 14 May 1948, p. 1; Knesset official Hebrew and English texts | Usable, attributed to the Declaration, never to Ben-Gurion |
| Q2 | The Declaration | "WE EXTEND our hand to all neighboring states and their peoples in an offer of peace and good neighborliness," Hebrew: «אנו מושיטים יד שלום ושכנות טובה לכל המדינות השכנות ועמיהן,» | As Q1; the Knesset's current English page (American spelling). One official edition is used throughout, never mixed with the British-spelling CBS edition | Usable, as Q1 |
| Q3 | Ben-Gurion | «קמה מדינת ישראל. תמה הישיבה הזאת.» English (a translation, labelled so): "The State of Israel has arisen. This session is closed." | People's Council, fourth sitting, 14 May 1948, official protocol, p. 26 (Knesset PDF); the ceremony recording | Usable. The Hebrew is the quote; the punctuation is the protocol's (two full stops, no exclamation mark) |
| Q4 | Golda Meyerson | "A dank eich vos ihr seit geblieben Yidden" ("Thank you for having remained Jews") | Golda Meir, *My Life* (Putnam, 1975), ch. 9, p. 183; the memoir's later account of Rosh Hashanah, 3 October 1948 | Usable, labelled as a recollection in *My Life* (1975), spoken in Yiddish. The Hebrew «תודה שנשארתם יהודים» is labelled a translation |
| - | Golda Meyerson, Chicago | "You cannot decide whether we should fight or not. We will." | *My Life*, which dates it 21 January 1948; the event was 25 January 1948 | **Not usable as stated**: the memoir's date is wrong and no contemporaneous transcript was opened |
| - | Ben-Gurion, the Negev | "In the Negev the people and State of Israel will be tested" | «בנגב ייבחן העם בישראל ומדינתו», *Davar*, 17 January 1955 | **Not usable as stated**: the English "will/shall" is a translation, and 1955 is outside the game's period |
| - | Dayan | none | No short, non-political 1948-52 line was found in a primary source | **No Dayan quote** |

## 4. The system model (as in Civ VI)

Civ VI's advisors are driven by a data table of messages, each with a trigger, conditions, a help level and a
priority, shown one at a time. We copy that shape.

### 4.1 The message file

`unity/Assets/Resources/Advisors/advisor-messages.yaml` (new, slice S1). Each message:

| Field | Meaning |
|---|---|
| `id` | Stable id, also the string key stem (`advisor.<id>.text`, `advisor.<id>.more`) |
| `advisor` | `ben_gurion`, `meyerson` or `dayan` |
| `signal` | The game signal it waits for (section 5), from a fixed list |
| `when` | An optional condition on that signal, from a fixed list of named conditions with one number (for example `debt_ratio_at_least: 0.8`); no expression language |
| `requires` | Message ids that must have been shown first (a teaching chain) |
| `level` | The lowest help level that shows it: `new`, `experienced`, `warnings`, or `critical` (section 6) |
| `priority` | 1 to 3; a higher one jumps the queue |
| `repeat` | `once`, or `every` with a `cooldown_turns` |
| `actions` | Up to two buttons: `show_me` (highlight a UI element by name, as the guide's ring does, `FirstTurnGuidePresenter.cs:282-300`), `open_screen` (a side screen by name, `ScreenNav.cs:13`), `encyclopedia` (a page id) |
| `quote` | Optional id from the verified list (section 3.3); only these lines show a quote |

A load-time test checks that every message names a known advisor, signal, condition, screen and string key in both
languages, that `requires` has no cycle, and that every `quote` is on the list.

### 4.2 The runtime

`AdvisorPresenter` (new, S1) subscribes to the bridge's `Changed`, `TurnEnded` and `Refused` events
(`unity/Assets/Scripts/UI/SimBridge.cs:31`, `:33`, `:35`), evaluates signals on the state the screen shows, and queues the
messages that fire.

- **One popup at a time.** A popup waits while any modal layer is open: the start screen (`StartScreenPresenter.cs:26`),
  the decision card (`EventCardPresenter.cs:24`, while it is not collapsed), the capture dialog
  (`CaptureDialogPresenter.cs:28`), the turn report (`TurnReportPresenter.cs:24`), the outcome clip
  (`OutcomePresenter.cs:26`) and the end screen (`EndScreenPresenter.cs:22`). These are the root's open classes the code
  already sets, so the advisor reads them and adds no new coupling.
- **Queue order:** critical first, then priority, then the order the signals fired. At most one teaching message and one
  warning a turn are shown by themselves; the rest wait for the next turn or the Counsel screen (section 7.4). This is
  the rule against nagging.
- **Seen state** lives in the settings file (section 15), never in the save.
- The advisor reads the state and the bridge only. It issues no command, queues no order and never presses End turn.

## 5. Triggers and signals

What each mechanic's first message waits for. "Exists" means a signal on main the advisor can read today; "Needs" means
the slice adds a read-only helper (in Unity, never in the sim's hash).

| Mechanic | Advisor | Rules | Signal on main |
|---|---|---|---|
| Turns, End turn | Ben-Gurion | R-TURN (`mvp.md:13-19`) | Exists: a new game (`FirstTurnGuidePresenter.NewGame`, `:128`, called from `GameRoot.cs:467`, `:514`); `TurnEnded` (`SimBridge.cs:33`) |
| Attention | Ben-Gurion | R-GOV-002 (`mvp.md:361`) | Exists: `SimBridge.AttentionLeft` (`:88`); `Refused` for "not enough Attention" (`:35`) |
| Events, delegation | Ben-Gurion | R-EVT (`mvp.md:407-415`) | Exists: `EventCardPresenter.Shown` (`:200`) |
| Objectives, score | Ben-Gurion | R-OBJ, R-SCORE (`mvp.md:291-305`) | Exists: `GameState.Objectives` (`GameState.cs:83`), `Score` (`:88`) |
| Defeat | Ben-Gurion | R-END-001 to R-END-003 (`mvp.md:341-345`) | Exists: `GameState.ExhaustedTurns` (`:100`), `Debt` (`:41`) against `debt_ceiling` (R-END-001), `Food` (`:94`). Needs: a warning before an enemy attack on one of the player's cities (R-END-003); see below the table |
| Cities, housing, jobs | Meyerson | R-CITY, R-HOUSE (`mvp.md:67-91`) | Needs: a "city shown" event. A click opens the info panel through `GameRoot.OnHexClicked` (`GameRoot.cs:572`), which raises nothing, and `Changed` does not fire on a look. S2 adds the event in Unity |
| Immigration, refugees | Meyerson | R-CITY-004 (`mvp.md:75`) | Exists: `GameState.Cities` (`:66`) refugees, after `TurnEnded` |
| Training | Meyerson | R-TRAIN (`mvp.md:111-115`) | Exists: `GameState.Cohorts` (`:70`) |
| City projects | Meyerson | R-PROJ (`mvp.md:97-105`) | Exists: `GameState.CityProjects` (`:74`), `UnlockedProjects` (`:79`) |
| Pioneer work | Meyerson | R-SITE, R-FIELD, R-SWAMP, R-CAMP (`mvp.md:127-155`) | Exists: `GameState.Sites` (`:72`); a halutz unit with no project |
| Corridors | Meyerson | R-COR (`mvp.md:185-189`) | Exists: `GameState.Corridors` (`:77`) |
| Money, debt | Meyerson | R-MONEY (`mvp.md:309-313`), R-END-001 | Exists: `Treasury`, `Debt` (`:40-41`) |
| Budget | Meyerson | no R-ID yet: the rules live in `EconomySystem.Allocate` and `Simulation.Execute` | Exists: a queued `AllocateBudget` (`BudgetPresenter.cs:54`, through `SimBridge.QueuedLike`). Needs, for the warning: "the budget is edited but not submitted"; #1352 (Beri) adds `BudgetUnsubmitted` to `SimBridge`, and S4 reads it once merged |
| Cabinet | Meyerson | R-MIN (`mvp.md:365-369`) | Exists: `SimBridge.Vacancies` (`:85`), already the Cabinet badge (`HudPresenter.cs:484`) |
| Food | Meyerson | R-FOOD (`mvp.md:321-325`) | Exists: `Food`, `FoodNeed`, `FoodUnmet` (`GameState.cs:94-98`) |
| National meters | Meyerson | R-NAT (`mvp.md:375-381`) | Exists: `Indicators` (`:33`), `ActiveSituations` (`:38`, set by `EffectsSystem.UpdateSituations`, `EffectsSystem.cs:139`) |
| Units, movement, go-to | Dayan | R-MOVE (`mvp.md:195-213`) | The sim side exists: `Movement.NeedsOrders` (`sim/Statecraft.Sim/Movement.cs:182`), `GameSession.Committed` and `Now` (`GameSession.cs:22-26`). Needs: the Unity side, which still queues one Move per unit; the civ-movement slices (U1 to U4) add it, and the advisor reads their "Move picked" and "order committed" points |
| Standing orders, Send home | Dayan | R-ORDER (`mvp.md:219-223`), R-UNIT-005 (`:169`) | Exists: a unit's `Order` |
| Combat | Dayan | R-COMBAT (`mvp.md:229-249`) | Exists: the turn's Why lines (`StepResult.Why`, `Commands.cs:172`; `PreviewResult.Why`, `:184`) |
| Arms | Dayan | R-ARMS (`mvp.md:331-335`) | Exists: `GameState.Arms` (`:102`) |
| Truces | Dayan | R-COMBAT-009 (`mvp.md:249`) | Exists: the truce windows (`Combat.PlayerTruces`, `Combat.cs:60`) and the chosen option (`ChosenOptions`, `GameState.cs:52`) |
| Capture | Dayan | R-CTRL (`mvp.md:35-51`) | Exists: a settlement `AwaitingChoice` (`SettlementControl.cs:14`) |
| The enemy's phase | Dayan | R-ENEMY (`mvp.md:255-263`) | Exists: the enemy lines in the turn report (`TurnReportPresenter.cs:112-128`) |

**The one new sim signal: a city under threat.** The critical warning "one of our cities is under attack" must come
before End turn, but the enemy chooses its orders only inside End turn (resolver step 1, `TurnResolver.cs:18`). Those
orders are a function of the state alone (R-ENEMY-001, `mvp.md:255`), so S4 adds a read-only sim helper that runs
`EnemyAI.Order` (`EnemyAI.cs:46`) on a clone of the shown state and returns the attacks aimed at a player city's hex. It
changes no state and draws no random number, so no golden moves; the warning is never a Unity copy of the enemy's
logic. The advisor shows it as "likely", since the player's own orders this turn can still change the board.

A full mechanic-by-mechanic map with rule IDs and draft first-time lines was made separately (Codex cx296, 23 October
2026, a research note outside the repo). It agrees with this table: of 24 mechanic groups, 22 have a usable signal on
main, and two (a city shown, movement) need one; the city-threat warning needs the helper above. It also found that
`mvp.md` has no rule ID for Attention's refill or for the budget's shares and vote; S3's encyclopedia pages describe them
from the code (`EconomySystem.Allocate`, `Simulation.Execute`) and name no rule ID. S1 checks every row against the
code of its day.

## 6. Help levels

A setting in Settings (`SettingsPresenter.cs`), saved in the settings file as `advisor_level`:

| Level | Shows |
|---|---|
| New to strategy games (default on a first game) | Every teaching message, warnings and suggestions |
| Experienced | Teaching only for this game's own mechanics (Attention, delegation, capture, pioneer work, corridors); warnings and suggestions |
| Warnings only | Warnings, no teaching |
| Off | Only the **critical** warnings: one of the player's cities about to fall (R-END-003), approaching defeat (R-END-001 and R-END-002: Money and Food exhausted, `ExhaustedTurns` above 0), and the debt near the ceiling |

"Off" keeps the critical warnings by the owner's design: the advisors must never stay silent on a real threat. The
encyclopedia (section 9) is open at every level.

## 7. The UI

All new strings follow `docs/WRITING_UI.md`, `docs/WRITING_UI_HE.md` and `docs/UX_GLOSSARY.yaml`.

### 7.1 The popup

A panel docked at the screen's edge, as the guide's dock is today (`Game.uxml:444`, `guide-layer`), with the advisor's
portrait, their name, the label "Advisor", the line, and up to two buttons. It takes no pointer outside its panel, so the
HUD stays usable under it, as the guide does (`FirstTurnGuidePresenter.cs:26-27`). Escape or "Close" dismisses it.
It never blocks End turn.

### 7.2 Tell me more

Opens the encyclopedia page for the message's mechanic (section 9).

### 7.3 Show me

Draws the gold ring round the named UI element, reusing the guide's ring and its follow-the-layout timer
(`FirstTurnGuidePresenter.cs:110-113`, `:282-300`), and, for a unit or a city, selects it and centres the camera as the
guide's move step does (`GameRoot.cs:505-511`).

### 7.4 Counsel screen and history

A new side screen, "Counsel", in the left menu (`ScreenNav.Screens`, `ScreenNav.cs:13`): one card per advisor with
their top concern now (the highest-priority message whose signal holds), and below it the full history of every message
shown this game, newest first, each with its turn date. The history is kept for the session only; on load it starts
empty (it is not in the save, section 15).

## 8. Disagreement

Sometimes two advisors see one choice differently: Meyerson wants Money kept for housing, Dayan wants it for Arms. The
Counsel screen then shows both views side by side as options, each with what the game says it costs. The game never
picks for the player, and no advisor's line is phrased as an order. Disagreements are rare by design (a message flag,
`disagrees_with`, at most one shown a turn) and only on game trade-offs, never on a contested historical issue.

## 9. Replacing the first-turn guide and How to play

The owner: "best that they completly replace [the existing tutorials]".

### 9.1 What exists

- **The first-turn guide** (`FirstTurnGuidePresenter.cs`, 404 lines): six steps, `{ "attention", "city", "training",
  "move", "event", "objectives" }` (`:37`), each with a gold ring round its target (`Targets`, `:282-300`), shown once on
  the first new game (`NewGame`, `:128`; seen key `first_turn_guide`, `:35`), reopened from How to play
  (`how_to_play.guide`, `strings.en.yaml:73`; button `how-to-play-guide`, `Game.uxml:376`). 17 EditMode tests in
  `FirstTurnGuideTests.cs`.
- **How to play** (`HowToPlayPresenter.cs:15`): eight lines, `how_to_play.turn` to `how_to_play.why`
  (`strings.en.yaml:57-65`; `Game.uxml:363-373`). 2 EditMode tests in `HowToPlayScreenTests.cs`; the menu button is
  pinned by `HebrewUiTests.cs:283`, `:289`, `KeyboardTurnTests.cs:109`, `LeftColumnTests.cs:37`, `:259`,
  `PromoHudTests.cs:43` and `TopBarTests.cs:628`.

### 9.2 The opening chain (slice S2)

On the first new game, Ben-Gurion greets the player, then each advisor teaches the turn-one essentials, pointing at the
same UI elements the guide does: Attention (Ben-Gurion), a city's housing and jobs and training (Meyerson), moving a unit
(Dayan), the event card (Ben-Gurion), objectives (Ben-Gurion). The rules of the guide carry over: shown once, never on a
Continue, never without a settings file (automated runs), skippable at any step, the decision card hidden except on its
step, the selection put back after.

### 9.3 Parity first, tests migrate first

S2 starts by porting `FirstTurnGuideTests.cs`' 17 tests to the opening chain, unchanged in what they assert (the targets,
the once-only rule, Continue, skip, the card's hide and restore, the End turn close), and they must pass on the chain
before the guide's code is removed. Only then is `FirstTurnGuidePresenter` deleted, in the same PR. The seen key
`first_turn_guide=seen` is honoured: a player who saw the guide does not get the chain by itself. S3 does the same for
How to play: its tests and the seven menu pins above move to the encyclopedia before the old screen goes.

## 10. Encyclopedia coverage

The How to play screen becomes the advisors' encyclopedia: one page per mechanic, grouped by advisor, each a short
explanation, the rule it follows in plain words, and a "Show me". It covers the mechanics the guide and How to play
already teach (turns, Attention, cities, training, moving a unit, events, objectives, the top bar, Why) and the 14 that
nothing teaches today (no `how_to_play.*` or `guide.*` line names them, `strings.en.yaml:57-93`):

1. the budget; 2. the cabinet and vacancies; 3. combat; 4. capture and its three answers; 5. corridors; 6. city
projects; 7. pioneer work (yishuv, fields, swamps, camps); 8. Food; 9. Arms; 10. the end conditions and defeat;
11. Money and debt; 12. standing orders and Send home; 13. truces; 14. the enemy's phase.

Immigration and the national meters get pages too.

## 11. Media pipeline

### 11.1 Portraits

Public-domain or CC archival photographs, as every portrait (`CONTENT_POLICY.md:35-39`):

- Ben-Gurion: `unity/Assets/UI/Portraits/ben_gurion.jpg` (Public domain, Abbie Rowe, NARA; `ASSETS.portraits.yaml:11-16`).
- Golda Meyerson: `unity/Assets/UI/Portraits/golda_meir.jpg` (CC0, Willem van de Poll; `ASSETS.portraits.yaml:207-212`).
- Dayan (new): the National Library of Israel photograph on Commons, 7 December 1948 (the memorial and the opening of the
  Highway of Valor), Benno Rothenberg / Meitar Collection / National Library of Israel / The Pritzker Family National
  Photography Collection, CC BY 4.0, 1,403 x 908 px:
  https://commons.wikimedia.org/wiki/File:Moshe_and_Ruth_Dayan_(997008137270105171).jpg. Cropped round Dayan and
  downscaled to the existing portraits' 512 px height, never enlarged; credited in `NOTICE`; a row in
  `ASSETS.portraits.yaml`. Two fallbacks from the same collection, also CC BY 4.0, are recorded in the research note
  (Beersheba, September 1950; Tel Aviv-Yafo, 13 October 1949).

### 11.2 Aide videos (slice S6)

Two or more short videos per advisor ("2+videos of them explaining"), each a photo video:

- the advisor's archival portrait with a slow pan, the game's UI highlights, and captions;
- narrated by a **fictional, named aide** for that advisor (one per office), introduced on screen as the aide, never as
  the advisor;
- the aide's voice made with **ElevenLabs Voice Design** (a voice designed from a text description), not a clone, with no
  real person's name in the prompt;
- in English and Hebrew; the Hebrew narration waits for a native reviewer's approval of a sample (the video-script
  rules, `06-ai-voice-music-and-disclosure.md:20`).

The picture is composed locally (ffmpeg) from the archival photograph and game captures. No photograph of a real person
is sent to ElevenLabs as input; only the aide's script goes to Voice Design and text to speech.

### 11.3 Real-figure videos (slice S7, only after consent)

The real figures appear to speak only after written consent from the Ben-Gurion Heritage Institute, Golda Meir's family
and Moshe Dayan's heirs, each for their own figure. **The coordinator drafts the letters and the owner sends them; no
outreach happens without the owner.** The consent letters are kept as ElevenLabs' terms require (section 13.2).

## 12. Video constraints

- **Off by default**, behind a "Watch" button in the encyclopedia and the Counsel screen.
- **Lazy**: the `CardVideo` pattern (`unity/Assets/Scripts/UI/CardVideo.cs:9-16`): one player made the first time a clip
  is asked for, never at boot, playing from `StreamingAssets/video` by URL with the build id
  (`CardVideo.Url`, `:40`), and off in every automated run (`CardVideo.ForcedOff`, `:29-37`).
- The load-time gate holds: the default first download does not grow beyond code-size noise, and boot time is measured
  on base and PR.
- Captions are always on and are the same text as the narration.

## 13. Voice and ElevenLabs

### 13.1 What the ruling changes

The owner's "you can sythesize their voices" lifts the rule that a synthetic voice never speaks as a real person, for
these three advisors. The design still waits for estate consent before any synthetic voice speaks as them (section
11.3), for the reasons in 13.2. Until then the aides' designed voices are used, which the 15 October rule already allows.

### 13.2 ElevenLabs' terms (read 23 October 2026, both pages "Last updated: 09 October 2026")

- **Prohibited Use Policy, section 3** (Deception, Manipulation and Impersonation): it prohibits "Cloning, synthesizing,
  or impersonating the voice, identity, or likeness of any living individual ... without their explicit and informed
  consent or other valid authorization, or of any deceased individual in a manner that is reasonably likely to cause
  harm or infringes applicable post-mortem rights."
- **Prohibited Use Policy, section 4** (Elections and Civic Integrity): "Impersonating political candidates or elected
  officials without authorization". All three were elected Knesset members.
- **Image and Video Terms, section 2** (Consents and Records): input with "the voice, image, or other visual likeness of
  an identifiable natural person" needs "all notices ... consents, releases, and authorizations required under
  applicable law ... from that individual or any other person legally authorized to give them", and the records are kept
  for at least five years.

So a voice or image of Ben-Gurion, Meyerson or Dayan made with ElevenLabs needs the authorisation that only their estates
can give. This is why S7 waits for consent. This reading is factual, not legal advice.

### 13.3 The account and the records

- The owner's paid ElevenLabs plan, no Beta features.
- Every output gets an `ASSETS.yaml` row: AI-generated, the tool, the voice id, the model, the date, the exact script, and
  the file's sha256, as the video-script rules ask (`06-ai-voice-music-and-disclosure.md:19`).
- `docs/ai-disclosure.md`, the README's AI section and the Steam draft change in the same PR as the first voiced video
  (`06-ai-voice-music-and-disclosure.md:23`).

## 14. Localisation, English and Hebrew

- Every advisor string has an English and a Hebrew key; the existing parity tests (`HebrewUiTests.cs`) cover them.
- **The player is addressed gender-neutrally** in Hebrew, by HE-03 (`docs/WRITING_UI_HE.md:73-76`): no masculine-only
  singular, no slash forms; plural address or "יש ל..." forms. The advisors speak in their own grammatical gender
  (Meyerson in the feminine), which is the persona's, not the player's.
- Hebrew lines are agent-drafted and go to the native reviewer before they ship, as the rest of the UI.
- Names: "דוד בן-גוריון", "גולדה מאירסון (לימים מאיר)", "משה דיין"; the people rows (`content/people.yaml:30`, `:586`,
  `:1120`) hold the first and third.

## 15. Persistence

- The help level, each message's "seen" mark and the opening chain's mark are lines in the settings file
  (`SettingsStore`, `unity/Assets/Scripts/SettingsStore.cs:8-13`: "presentation only, never the sim or the save"), one
  `key=value` line each, as the guide's `first_turn_guide=seen` (`FirstTurnGuidePresenter.cs:35`).
- Seen marks are kept compact: one line, `advisor_seen=<id>,<id>,...`.
- Nothing goes in the save. A loaded game keeps the player's seen marks (they are the player's, not the game's).
- With no settings file (automated runs, EditMode tests) the advisors never show by themselves, as the guide today
  (`FirstTurnGuidePresenter.cs:16-17`). So no golden, smoke run or browser check changes.

## 16. Accessibility, performance and testing

**Accessibility.** The popup is reachable by keyboard and returns focus where it was (`FocusReturn`), meets the text
floor (`HudPresenter.TextFloorPx`, as the guide does, `FirstTurnGuidePresenter.cs:47-50`), and never relies on audio:
every video has captions, and every popup is text.

**Performance.** Signals are read on `Changed` and `TurnEnded` only, never each frame. Portraits are the existing
512 px files. Videos are lazy (section 12). Nothing loads at boot.

**Testing.** Each slice adds EditMode tests first and runs the full Unity EditMode suite on the PR merged with main, and
shows matched English and Hebrew before and after screenshots (`CONTRIBUTING.md`, "Pull requests"; the PR template's "Visual evidence"). The message file has a load test (section 4.1).
Signals are unit-tested on built states. The opening chain inherits the guide's 17 tests (section 9.3). No golden moves:
the advisors never touch the sim.

## 17. Slices and build order

Each slice is its own PR, tests first, a full Unity EditMode run, English and Hebrew screenshots. **Every advisor line
goes to the owner for content review before it ships.**

| Slice | What | Depends on |
|---|---|---|
| S1 | The engine and the data format: message file, loader and its test, `AdvisorPresenter`, the queue behind the modal layers, the settings lines. No messages shown yet | - |
| S2 | The opening chain, replacing the first-turn guide (section 9) | S1 |
| S3 | The encyclopedia, replacing How to play (section 10) | S1 |
| S4 | Ongoing warnings and advice (section 5), the help-level setting (section 6) | S1; the budget signal waits for #1352 |
| S5 | The Counsel screen and history, disagreement (sections 7.4, 8) | S4 |
| S6 | The aide videos (section 11.2), the Dayan portrait (section 11.1) | S3 |
| S7 | Real-figure videos, only after written estate consent (section 11.3) | consent |

## 18. Open questions

| # | Question | Default until answered |
|---|---|---|
| OQ-1 | The aides' names: invented, plausible period names in English and Hebrew, one per advisor. Does the owner want to choose them? | The coordinator proposes three in S6's PR for the owner's review |
| OQ-2 | Dayan's 1948-49 posts (battalion commander, commander in Jerusalem, Aluf in October 1949) are not yet in `content/people.yaml`. They matter only if a line names them | No line names them |
| OQ-3 | Does "Off" also keep the capture prompt (a capture awaiting an answer, R-CTRL-007)? It is a decision, not a threat | No: the capture dialog already asks |
| OQ-4 | The civ-movement spec changes `guide.move.text` (its section 8.2); if that lands first, S2 ports the new text | S2 ports whatever is on main |
| OQ-5 | The end screen has no player text for `defeat_war` (`strings.en.yaml:279-280` has `mvp_ended` and `defeat_economy`; the sim's English is "The enemy holds a city", `PlayerText.cs:27`). Ben-Gurion's defeat message needs it | S4 adds `end.outcome.defeat_war` in both languages |

## 19. Policy edits in this PR

- `docs/CONTENT_POLICY.md` principle 2 (`:28-29`): advisor lines are labelled game dialogue, not quotes, under the owner's
  "Labelled game dialogue" ruling; real quotes only from a verified list.
- `docs/CONTENT_POLICY.md` the AI-likeness lines (`:99-100`): the advisors' synthetic voices ("you can sythesize their
  voices") and the real-figure videos only after the estates' written consent; the aide voices are not likenesses.
- `docs/oss/DECISIONS.md`: a D-ADV line recording the rulings.

## 20. Risks

| Risk | Handling |
|---|---|
| An advisor line reads as a real quote | Labelled panel, never quotation marks; quotes only from section 3.3; the owner reviews every line |
| An advisor appears to take a side on a contested issue | Neutral game facts only (3.2); Deport never suggested; the owner's review |
| Nagging | One teaching and one warning a turn, cooldowns, help levels |
| Silence on a real threat | Critical warnings at every level, "Off" included |
| A regression in onboarding when the guide goes | Parity first: the guide's tests pass on the chain before its code is removed (9.3) |
| A synthetic voice of a real figure without the right consent | No real-figure voice or likeness before written estate consent (11.3, 13.2) |
| Video growing the download or boot time | Off by default, lazy, StreamingAssets, never at boot; the load-time gate |
| Hebrew addressing the player in one gender | HE-03 and the native reviewer |
| Advisors changing a golden | They never touch the sim; no settings file in automated runs |
