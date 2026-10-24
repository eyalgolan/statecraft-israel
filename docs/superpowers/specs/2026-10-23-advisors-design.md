# Civ-style advisors: design spec

**Status:** the design was APPROVED by the owner on 23 October 2026: "the advisors design looks good. approved" (in the
coordinator session). This revision adds the owner's later rulings of the same day (the side popup, the small video,
the voices, the portraits, private people by role) and is for the owner's review of the text. It changes no code.
Nothing is built until this PR is merged, every slice below is its own PR, and nothing is built from an open question
(section 18).
**Date:** 23 October 2026. **Base:** origin/main `0058f67c` (#1488 merged). Every file:line below was read on that commit.
**Plan:** none yet. The plan is written after this spec merges, slice by slice (section 17).

## What changed in this revision

- **24 October 2026, the media route** (sections 11 and 12, for the owner's approval): real archival photographs,
  faithfully colorized, animated with ElevenLabs Kling 3.0 Pro and lip-synced with Sync 3 to voices made as close to the
  real people as possible (Fish Audio), English first, with a live disclosure label, a credit per clip and a review of
  every clip. Where the older text below (section 2) still describes aide narration or estate consent first, sections 11
  and 12 govern.
- **24 October 2026, the owner's answers to OQ-13 to OQ-17** (section 11.3): the estate-consent rule and the rest of the
  advisor exception are amended to this route (`docs/CONTENT_POLICY.md` and D-ADV, in this PR); the Golda photograph
  stays, with a tighter crop; the label is "AI animation and voice. Fictional dialogue.", shown small; the fictional
  aides are retired.
- The advisor is a popup on the side, as in Civ (section 7.1): the upper part of the right column, above the minimap,
  400 px wide; a sheet under the top bar on an upright phone; the same place in Hebrew with its inside mirrored. It
  hides under the modal layers (section 4.2) and steals no focus (section 16).
- "Watch" plays a small video inside the popup on desktop and in a centred overlay where the popup has no room
  (section 12). The picture is a silent clip on the existing player; captions are live text; the narration is a
  separate audio file. The first three sample clips and their settings are recorded in section 12.1.
- The video sources are the three photographs the owner chose (section 11.1).
- The fictional aides who were to narrate are retired (24 October 2026, section 11.3); their saved voices and the 43
  Hebrew lines rendered with them are not used (section 13.1).
- Private people are named by role (section 2). The game never charges money and never has ads (section 2).
- The opening chain's event step is restated: the advisor speaks before the card opens and steps aside when "Show me"
  opens it (section 9.3).

## 1. The request and the goal

The owner (23 October 2026, verbatim): "we need to add advisors like in civ ... ben gurion will be a general assistanat
about game mechanisems, golda meir will explain about stuff that are more specific but not military and moshe dayan will
explain military ... best that they completly replace [the existing tutorials]. Use our elevenlabs subscription to make
the videos for these."

**Goal.** Three advisors, as in Civilization, teach each mechanic the first time the player meets it and then give
short warnings and suggestions as the game goes on. They replace the first-turn guide and the How to play screen. A
setting controls how much they speak. They show in a small popup on the side of the screen, so the player stays in the
game. They never change the simulation, the save or a golden hash: like the guide today, they are presentation only
(`FirstTurnGuidePresenter.cs:19-20`).

## 2. The rulings

The owner's answers (23 October 2026, coordinator session, verbatim). An answer chosen from a list of options is quoted
as the option's label; "(Recommended)" marks the option we had recommended.

| Question | Ruling |
|---|---|
| The advisors' role | "Teach + advise (Civ VI style) (Recommended)": teach each mechanic when first met, and give ongoing warnings and suggestions; a setting controls how chatty they are |
| Synthetic voices | "you can sythesize their voices". This lifts the project's earlier rule that a synthetic voice never speaks as a real person (section 13.1). ElevenLabs' own terms still apply (section 13.2) |
| Their words | "Labelled game dialogue (Recommended)": advisor lines are original game dialogue in the persona, shown in the advisor popup and never styled as quotes. Real quotes come only from a verified list, styled as quotes with their source. Nothing puts an advisor on a contested issue beyond neutral game facts |
| Media | "2+videos of them explaining", then, asked what that means, "The real figures talking". Later, on how video shows in the popup: "Small video in popup (Recommended)" (Watch plays in the popup on desktop and in a centred overlay on phones; live-text captions; the narration as separate audio) |
| Golda's name | "Golda Meyerson (later Meir) (Recommended)" |
| The whole design | "the advisors design looks good. approved" |
| Placement | "the advistors need to be a popup window on the side like in civ" |
| Voices | "Yes, both": save the three chosen aide voices, then render the Hebrew lines with ElevenLabs `eleven_v3` |
| Private people | "By role (Recommended)": private people are named by their role in research notes; public figures keep their names. This spec follows the same rule |
| Video sources | Ben-Gurion: "1951 official PM portrait"; Golda: "1964 desk photo (Recommended)"; Dayan: "1956 map briefing (Recommended)". Choosing them approved downloading those Commons originals |
| Which lines talk (24 October 2026) | "select some dayan, some ben gurion and some golda to match the budget. the rest of the advisor screen would use their photos for now until we have more tolens": only some lines of each advisor get a clip; every other line shows the advisor's portrait, with the narration where one exists (sections 7.1, 12) |
| Price and ads | "the game is never going to charge any money. ever", and then "the game is never going to have ads" (`docs/oss/DECISIONS.md` D1, `:15`) |

How "The real figures talking" is carried out (part of the approved design): the real figures' own voices or likeness
talk only after written consent from their estates (the Ben-Gurion Heritage Institute; Golda Meir's family; Moshe
Dayan's heirs). Until then each video is a **photo video**: the archival photograph with one slow move, narrated by a
**fictional aide** for each advisor, in a voice made with ElevenLabs Voice Design (a designed voice, not a clone; no real
name in the prompt), in English and Hebrew (section 13). The aides are fictional and never imitate anyone.
(Superseded on 24 October 2026 by the owner's media rulings, section 11.2, and the amended estate-consent rule,
section 11.3.)

Already decided, and kept as they are:

- **The perspective and sourcing rules** of `docs/CONTENT_POLICY.md` sections 1 and 2. This PR amends principle 2 and the
  AI-likeness lines (`CONTENT_POLICY.md:28-34`, `:104-118`) to match the rulings above (section 19).
- **Portraits** are public domain, CC0, CC BY 4.0 or CC BY-SA 4.0 photographs (`CONTENT_POLICY.md:39-44`, `:85-88`).
  Government Press Office photographs tagged only `PD-Israel` are accepted for the free release by D-P
  (`docs/oss/DECISIONS.md:32`), and there is no paid release (`:50`).
- **In-game AI narration** (15 October 2026, #639, recorded in
  `.github/skills/statecraft-video-script/references/06-ai-voice-music-and-disclosure.md:28`): allowed in its own voice,
  never as a real person, disclosed, with an `ASSETS.yaml` row. The real-figure clips (S6b) need that file's rules 1
  and 9 (`:16`, `:24`) amended for the three advisors; the first clip PR does it (the plan's Task 26).
- **ElevenLabs output in the game** is recorded in `ASSETS.yaml` as AI-generated (the 18 October ruling on #39,
  `docs/oss/DECISIONS.md:83`).

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
holds no office for Dayan before 1953, and no row holds the Moscow post, so these facts are sourced before any text names
them). Calling all three "Advisor" is a game role, and nothing in the UI claims they held it.

**The name.** The popup shows "Golda Meyerson (later Meir)" on the advisor's first appearance and "Golda Meyerson" after
it, in Hebrew "גולדה מאירסון (לימים מאיר)". `content/people.yaml:585` keeps "Golda Meir" for the cabinet; the advisor's
display name lives in the advisor data file, not in `people.yaml`.

**The voice.** In a clip the advisor speaks, in a synthetic voice made as close to theirs as possible (section 11.2).
The fictional aides of the first version, who were to narrate, are retired (owner, 24 October 2026, section 11.3).

### 3.2 What they may say

- **Game dialogue, labelled.** Every line is original game text in the advisor's persona. It is shown in the advisor
  popup under the label "Advisor", never in quotation marks, never in italics with an attribution, never in the style the
  game uses for a real quote. It describes the game's own mechanics and state: "Our debt is close to the ceiling; one
  more loan and the economy breaks."
- **Neutral game facts only.** No advisor gives a view on a contested issue: the refugees' flight, Deport, the borders,
  the Arab states. On a capture, Dayan explains the three answers and their costs as the game computes them
  (`SettlementControl.CaptureOptions`, R-CTRL-008, `docs/rules/mvp.md:47`) and recommends none; Deport is never
  suggested, never praised, and never the first option (R-CTRL-008).
- **Real quotes only from the verified list** (section 3.3), each shown as a quote with its source line under it, and only
  where the list says it may be used.
- **No made-up history.** A line may name a date, place or number only when the game's content already sources it.
- **A clip carries its disclosure**, "AI animation and voice. Fictional dialogue." (section 12).
- **Plain English** by `docs/WRITING_UI.md`, and Hebrew by `docs/WRITING_UI_HE.md` (section 14). A popup line is one or
  two sentences, about 35 words at most; the encyclopedia page may be longer.
- **The owner reviews every advisor line** before it ships (section 17).

### 3.3 The verified quotes list

From a research note checked on 23 October 2026 (Codex cx285); Q3's punctuation and Q4's date were corrected against their sources on 24 October 2026 (Codex cx439, re-checked). Only these may appear, and only as labelled quotes:

| # | Speaker | Text | Source | Use |
|---|---|---|---|---|
| Q1 | The Declaration (a document, not a personal line) | "THE STATE OF ISRAEL will be open for Jewish immigration and for the Ingathering of the Exiles;" Hebrew: «מדינת ישראל תהא פתוחה לעליה יהודית ולקיבוץ גלויות;» | Declaration of the Establishment of the State of Israel, *Iton Rishmi* no. 1, 14 May 1948, p. 1; Knesset official Hebrew and English texts | Usable, attributed to the Declaration, never to Ben-Gurion |
| Q2 | The Declaration | "WE EXTEND our hand to all neighboring states and their peoples in an offer of peace and good neighborliness," Hebrew: «אנו מושיטים יד שלום ושכנות טובה לכל המדינות השכנות ועמיהן,» | As Q1; the Knesset's current English page (American spelling). One official edition is used throughout, never mixed with the British-spelling CBS edition | Usable, as Q1 |
| Q3 | Ben-Gurion | «קמה מדינת־ישראל! תמה הישיבה הזאת.» English (a translation, labelled so): "The State of Israel has arisen! This session is closed." | People's Council, fourth sitting, 14 May 1948, official protocol, printed p. 26 (Knesset PDF `0_ptm_258582.pdf`, its page 3; checked 24 October 2026); the ceremony recording | Usable. The Hebrew is the quote; the punctuation is the protocol's: an exclamation mark after «ישראל», a full stop at the end, and a maqaf in «מדינת־ישראל» |
| Q4 | Golda Meyerson | "A dank eich vos ihr seit geblieben Yidden" ("Thank you for having remained Jews") | Golda Meir, *My Life* (Putnam, 1975), ch. 9, p. 183; the memoir's later account of the first day of Rosh Hashanah 5709, 4 October 1948 (the festival began at sunset on 3 October; the crowd scene was on 4 October: Gennady Estraikh, "The Moscow Choral Synagogue and Its Rabbinate, 1940s-1980s", *Iudaica Russica* 14 (2025), p. 10, checked 24 October 2026) | Usable, labelled as a recollection in *My Life* (1975), spoken in Yiddish. The Hebrew «תודה שנשארתם יהודים» is labelled a translation |
| - | Golda Meyerson, Chicago | "You cannot decide whether we should fight or not. We will." | *My Life*, which dates it 21 January 1948; the event was 25 January 1948 | **Not usable as stated**: the memoir's date is wrong and no contemporaneous transcript was opened |
| - | Ben-Gurion, the Negev | "In the Negev the people and State of Israel will be tested" | «בנגב ייבחן העם בישראל ומדינתו», *Davar*, 17 January 1955 | **Not usable as stated**: the English "will/shall" is a translation, and 1955 is outside the game's period |
| - | Dayan | none | No short, non-political 1948-52 line was found in a primary source | **No Dayan quote** |

## 4. The system model (as in Civ)

Civ's advisors are driven by a data table of messages, each with a trigger, conditions, a help level and a priority,
shown one at a time. We copy that shape.

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
| `actions` | Up to two buttons: `show_me` (highlight a UI element by name, as the guide's ring does, `FirstTurnGuidePresenter.cs:278-303`), `open_screen` (a side screen by name, `ScreenNav.cs:13`), `encyclopedia` (a page id). The dismiss controls (section 7.1) are the popup's own, not data |
| `media` | Optional: a silent clip or still (itself optional), the narration file per language, and the caption cues per language (section 12). With narration but no picture, the advisor's popup portrait is the picture |
| `quote` | Optional id from the verified list (section 3.3); only these lines show a quote |

A load-time test checks that every message names a known advisor, signal, condition, screen and string key in both
languages, that every `media` row's supplied files and cues exist in both languages, that a row without a picture has
narration, that `requires` has no cycle, and that every
`quote` is on the list.

### 4.2 The runtime

`AdvisorPresenter` (new, S1) subscribes to the bridge's `Changed`, `TurnEnded` and `Refused` events
(`unity/Assets/Scripts/UI/SimBridge.cs:31`, `:33`, `:35`), evaluates signals on the state the screen shows, and queues the
messages that fire.

- **One popup at a time.** A popup waits while any modal layer is open: the start screen (`StartScreenPresenter.cs:26`),
  the decision card (`EventCardPresenter.cs:24`, while it is not collapsed), the capture dialog
  (`CaptureDialogPresenter.cs:28`), the turn report (`TurnReportPresenter.cs:24`), the outcome clip
  (`OutcomePresenter.cs:26`) and the end screen (`EndScreenPresenter.cs:22`). These are the root's open classes the code
  already sets, so the advisor reads them and adds no new coupling. On the two-row HUD it also waits while any side
  screen is open, because the screen then takes the full width and hides the right column (`Statecraft.uss:767-770`).
- **A layer opening over it.** If one of those layers opens while a popup shows (the player presses "Decision
  waiting"), the popup hides at once, and its sound and video stop. When the layer closes it comes back if its signal
  still holds; otherwise it goes to the Counsel history (section 7.4) without showing again.
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

"Off" keeps the critical warnings by the owner's design: the advisors must never stay silent on a real threat. There is
no per-message "don't show again" box: the help level is that control, as in Civ VI and VII. The encyclopedia
(section 10) and the Counsel screen are open at every level.

## 7. The UI

All new strings follow `docs/WRITING_UI.md`, `docs/WRITING_UI_HE.md` and `docs/UX_GLOSSARY.yaml`.

### 7.1 The popup

**Where it goes.** The owner asked for "a popup window on the side like in civ". The side popup is Civ VII's (middle
right) and Civ V's (top right); Civ VI's advisor sits at the top centre. Our layout already leaves the upper part of the
right column free at every tier: the column (`Statecraft.uss:214`, `right: 16px; top: 112px; width: 480px`) holds a
flexible gap (`.right-gap`, `:266`) above the 222x380 minimap (`.minimap`, `:433`), and the side screens stop short of it
(`.screen`, `right: 512px`, `:457`). The panel scales from 1920x1080 with match 0.5 (`HudPresenter.cs:19-20`), so the
sizes below are logical (panel) px.

| HUD tier (example windows) | Dock and size |
|---|---|
| One row (1920x1080, 1280x720, 1366x768, 1024x768, a phone held sideways at 844x390) | The right column: `right: 16px`, 400 px wide. Its top is the column's top plus the "Decision waiting" row (about 46 px) and 8 px, so it never jumps when that button appears; its bottom stays 12 px above the minimap. Height follows the content up to that space (about 506 px at 1920x1080, 400 at 844x390), then the text scrolls |
| Two rows (960x1080, square windows) | The same right-column slot, 400 px wide, up to about 746 px tall. Waits while a side screen is open (section 4.2) |
| Three rows (390x844 and other upright phones) | A sheet under the top bar, from the left column's edge to the right edge (`left: 88px; right: 16px`), its top under "Decision waiting" and the toast when they show (placed in code); its bottom 12 px above the higher of the info panel and the minimap's label; the text scrolls |
| Hebrew, every tier | The same place: the HUD does not mirror its panels (`Statecraft.uss:943-946`). Only the inside mirrors (below) |

At 1920x1080 the free slot is about x 1424 to 1904, y 166 to 672; the 400 px popup takes x 1504 to 1904. 400 and not
480 keeps a 24 px gap to the decision card's right edge (x 1480), but the popup never shows with an open card anyway
(section 4.2). The left side holds the side menu, the info panel, the toast and the screens, and the top centre holds the
bar's date and the toast, so neither is used.

**What is in it.**

```text
+--[portrait 96x120]--+ Golda Meyerson               [X]
| archival photograph | ADVISOR
+---------------------+
 One or two short sentences of live text.
 [Show me] [Tell me more]                     [Got it]
                                               1 of 3
```

- **Portrait**: a still archival photograph cropped round the face, about 96x120, in the info panel's gold frame. Never
  flipped, never animated. When the message has a clip or narration in the current language, a small play badge on it is
  the "Watch" button (section 12). If that line has narration but no clip, the portrait stays visible while the voice and
  live captions play. If it has neither, the normal portrait and text remain and there is no Watch button.
- **Name and label**: the advisor's name (section 3.1) and "Advisor" in small capitals; never quotation marks or italics.
  The name wraps and never shrinks (Hebrew names are longer).
- **Text**: one or two sentences, 22 px panel size like `.guide-text`, raised to the 12 screen px text floor
  (`HudPresenter.FlooredTextSize`, `HudPresenter.cs:89-92`) as the guide does (`FirstTurnGuidePresenter.cs:345-359`).
- **Buttons**, at most three, the main action first: teaching `Show me`, `Tell me more`, `Got it`; advice and warnings
  `Show me`, `Not now`, `Got it`. `Not now` puts the message off to the next turn and into Counsel; `Got it` marks it
  seen. An X in the corner does the same as `Got it`; a phone has no Escape, as the screens' X shows
  (`ScreenNav.cs:198-204`). A small "Advisor settings" link in the footer may open Settings.
- **Counter**: "1 of 3" in the footer when more messages wait.
- **Critical warnings** have a red-gold edge and no `Not now`.
- **Hebrew**: the header and the button row run `row-reverse` (as `.lang-he .row`, `Statecraft.uss:957-960`), so the
  portrait sits at the right where a Hebrew reader starts; each label's paragraph direction comes from its own words
  (`UiDirection`), since names, dates and numbers can be English. Photographs and video never flip.
- It takes no pointer outside its panel, so the HUD stays usable under it, as the guide's layer does
  (`Game.uxml:444`, `FirstTurnGuidePresenter.cs:26-27`). It never blocks End turn.

### 7.2 Tell me more

Opens the encyclopedia page for the message's mechanic (section 10).

### 7.3 Show me

Draws the gold ring round the named UI element on the real HUD, never a picture inside the popup, reusing the guide's
targets, ring and its 100 ms follow-the-layout timer (`FirstTurnGuidePresenter.cs:110-113`, `:278-343`), and, for a unit
or a city, selects it and centres the camera as the guide's move step does (`GameRoot.cs:505-511`). If the target's
rectangle meets the popup's, the popup moves within its slot (desktop) or the camera centres the target below the sheet
(upright phone). It never carries out the advised action.

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
  "move", "event", "objectives" }` (`:37`), each with a gold ring round its target (`Targets`, `:278-303`), shown once on
  the first new game (`NewGame`, `:128`; seen key `first_turn_guide`, `:35`), reopened from How to play
  (`how_to_play.guide`, `strings.en.yaml:73`; button `how-to-play-guide`, `Game.uxml:376`). 17 EditMode tests in
  `FirstTurnGuideTests.cs`. On its event step the card is shown and the guide's panel sits over it (`:23-25`).
- **How to play** (`HowToPlayPresenter.cs:15`): eight lines, `how_to_play.turn` to `how_to_play.why`
  (`strings.en.yaml:57-65`; `Game.uxml:363-373`). 2 EditMode tests in `HowToPlayScreenTests.cs`; the menu button is
  pinned by `HebrewUiTests.cs:283`, `:289`, `KeyboardTurnTests.cs:109`, `LeftColumnTests.cs:37`, `:259`,
  `PromoHudTests.cs:43` and `TopBarTests.cs:628`.

### 9.2 The opening chain (slice S2)

On the first new game, Ben-Gurion greets the player, then each advisor teaches the turn-one essentials, pointing at the
same UI elements the guide does: Attention (Ben-Gurion), a city's housing and jobs and training (Meyerson), moving a unit
(Dayan), the event card (Ben-Gurion), objectives (Ben-Gurion). The rules of the guide carry over: shown once, never on a
Continue, never without a settings file (automated runs), skippable at any step, the selection put back after.

The event step changes, because the popup never sits over an open card (section 4.2): the advisor speaks **before** the
card opens ("A decision is waiting..."), with the card collapsed and the ring on "Decision waiting". Its `Show me` opens
the card, the popup steps aside at once, and the card's own text explains the options. When the card closes, the chain
goes on.

### 9.3 Parity first, tests migrate first

S2 starts by porting `FirstTurnGuideTests.cs`' 17 tests to the opening chain (the targets, the once-only rule, Continue,
skip, putting the selection back, the End turn close), and they must pass on the chain before the guide's code is
removed. The event step's test is restated, not ported as it is: it proves that (1) the message shows while the card is
collapsed, (2) `Show me` opens the card, (3) the popup is hidden while the card is open, and (4) closing or answering the
card moves the chain on or puts it back correctly. Only then is `FirstTurnGuidePresenter` deleted, in the same PR. The
seen key `first_turn_guide=seen` is honoured: a player who saw the guide does not get the chain by itself. S3 does the
same for How to play: its tests and the seven menu pins above move to the encyclopedia before the old screen goes.

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

**Amended 24 October 2026.** The owner chose how the clips are made: real archival photographs, faithfully colorized,
made to talk with voices as close to the real people as possible (section 11.2). This replaces the photo videos narrated
by fictional aides (this spec's first version) and the stylized local routes tried on 23 October. On 24 October the
owner amended the estate-consent rule and the rest of the advisor exception to this route, and this PR amends
`docs/CONTENT_POLICY.md:104-118` and D-ADV (`docs/oss/DECISIONS.md:70`) to match (section 11.3). Each clip still ships
only after the owner's review of that clip (section 11.5).

### 11.1 Photographs

**The popup's still portraits** are as approved in the first version of this spec, all public domain or CC
(`CONTENT_POLICY.md:39-44`):

- Ben-Gurion: `unity/Assets/UI/Portraits/ben_gurion.jpg` (Public domain, Abbie Rowe, NARA; `ASSETS.portraits.yaml:11-16`).
- Golda Meyerson: `unity/Assets/UI/Portraits/golda_meir.jpg` (CC0, Willem van de Poll; `ASSETS.portraits.yaml:207-212`).
- Dayan (new): the National Library of Israel photograph on Commons, 7 December 1948, Benno Rothenberg / Meitar
  Collection / National Library of Israel / The Pritzker Family National Photography Collection, CC BY 4.0,
  1,403 x 908 px: https://commons.wikimedia.org/wiki/File:Moshe_and_Ruth_Dayan_(997008137270105171).jpg. Cropped round
  Dayan and downscaled, never enlarged; credited in `NOTICE`; a row in `ASSETS.portraits.yaml`.

Whether the stills should switch to the video photographs below is open (section 18).

**The video photographs** are real archival photographs only. The owner rejected an AI-repainted uniform on Dayan: "dayan
must have real uniform not what you put" (owner, 24 Oct 2026, in chat). On 24 October the owner picked the Golda and
Dayan photographs below from contact sheets, replacing the 1964 desk photo and the 1956 map briefing chosen on 23
October. Checked on Commons on 24 October 2026:

- **David Ben-Gurion**, official prime-minister portrait, 16 April 1951, 2052 x 3066 px:
  https://commons.wikimedia.org/wiki/File:Prime_minister_David_Ben-Gurion,_1951.jpg. National Photo Collection
  `D683-095`, Israeli Government Press Office photographer, `PD-Israel`. Commons gives no United States public-domain
  rationale, so a URAA-restored US copyright is possible; it is used under D-P's acceptance of GPO photographs for the
  free release (OQ-5).
- **Golda Meyerson (later Meir)** with the children of Kibbutz Shefayim, 24 July 1950, 2508 x 2508 px:
  https://commons.wikimedia.org/wiki/File:Golda_Meir_with_children_of_Kibbutz_Shfayim.jpg. National Photo Collection
  `D703-086`, Théodore Brauner, Government Press Office, `PD-Israel` (the same URAA note). The archive's caption: "After
  the opening of the Tel Aviv Nathania highway, Mrs. Golda Meir poses for a picture with the children of Kibbutz
  Shefaim". It shows identifiable private people, children in 1950, and is an event photograph as much as a portrait.
  The owner kept it under the advisors' narrow exception (section 11.3): "you can ship as is but it doesnt look good at
  the moment - i think the fact that there are too many people is making elevenlabs do a bad job", then "Tighter crop,
  same photo" and "B: medium" (24 Oct 2026). So the Golda clips start from the box (850,160,2150,891) of the colorized
  master (1300 x 731 px, 16:9, no upscaling), and the bystanders left in it are never animated or altered.
- **Moshe Dayan** in uniform, 9 December 1953, 1297 x 1314 px:
  https://commons.wikimedia.org/wiki/File:Moshe_Dayan_(997008136641805171).jpg. NLI `997008136641805171`, Benno
  Rothenberg, CC BY 4.0. Credit, in full: "Benno Rothenberg / Meitar Collection / National Library of Israel / The
  Pritzker Family National Photography Collection / CC BY 4.0". The lane that found it reported the Commons scan as
  mirrored; its orientation is checked against Dayan's left-eye patch before use, and a flip back is recorded as a
  change. The owner approved its colorized version: "I agree" (owner, 24 Oct 2026, in chat).

The Ben-Gurion and Golda photographs fall in the game's 1948-52 period; the Dayan photograph is later, its credit gives
its date, and nothing implies it was taken in the period. Every photograph used gets its `ASSETS.yaml` or
`ASSETS.portraits.yaml` row, and its `NOTICE` credit where its licence asks.

**Faithful colorization.** Each video photograph is colorized before it is animated. Colorization may add plausible
colour and set the tone, but it changes nothing the photograph shows: the face, hair, body, clothing, uniform, insignia,
Dayan's eye patch, the pose, the people around and the objects. A colorization that restyles any of them is rejected,
as the first Golda colorization was (it changed her hair). Ribbon and insignia colours that the photograph cannot show are
a guess and are recorded as one. Faithful colorization is allowed for these three photographs and is recorded in the
credit ("Amend to my route", section 11.3).

**Amendment for the first English clips (owner, 24 October 2026, "Record amendment").** Two changes beyond colorization
are approved, and each clip's credit line and `ASSETS.yaml` row say exactly what was changed:

- Ben-Gurion (`opening.greeting`, `opening.attention`, `opening.event`): the 1951 portrait is tinted and painted
  locally, his face still the photograph, and set on an AI-generated office background (an empty room drawn from two
  1949 photographs of his office, with the flag the owner asked for).
- Golda (`opening.city`, `opening.training`, `meyerson.exhausted`, `meyerson.debt_ceiling`): the clip is the raw Kling
  3.0 Pro and Sync 3 output of the colorized crop, with no composite and nothing painted out ("A, raw", about 14:10
  IDT). The whole frame is animated: the camera pushes in and pans, the flag waves and the people around her change
  expression.

### 11.2 The route (slice S6b)

The owner's rulings, verbatim, from the coordinator session ("in chat"):

| Ruling | Owner's words |
|---|---|
| Voices as close to the real people as possible | "you can sythesize their voices" (23 Oct 2026); "we need to make the sound sound like ben gurion" and "we must make the voices of all 3 characters as close to reality as possible" (23 Oct 2026) |
| The video recipe | On the Ben-Gurion English test (ElevenLabs Kling 3.0 Pro image-to-video, silent, then Sync 3 lip-sync to an English voice cloned in ElevenLabs from an Israel State Archives 1948 recording): "looks good" (24 Oct 2026) |
| Tools | "fish audio is good. local video is shit." and "we have dreamina for video" (24 Oct 2026); then "use elevenlabs, fish, dreamina as best as possible" (24 Oct 2026) |
| English first | "lets focus on the english voices and lipsync for now" (23 Oct 2026) |
| ElevenLabs' likeness terms | Told that ElevenLabs' Image and Video Terms, section 2, make the uploader warrant every consent a person's likeness needs (section 13.2): "do it" (23 Oct 2026). On ElevenLabs' first-use Prohibited Use Policy dialog: "Yes, accept it for me" (24 Oct 2026) |
| The Israel State Archives' derivative-work clause, for the voice clone | "Proceed, I accept it (Recommended)" (24 Oct 2026) |
| Dreamina's "AI" label on a take | "Crop it out by framing (Recommended)" (24 Oct 2026). G5 (`docs/oss/DECISIONS.md:69`, "Keep the badge") still covers the four decision-card clips it names |

So:

- **Voices**: Fish Audio, from lawfully usable archival recordings of each person. The approved Ben-Gurion test used an
  ElevenLabs voice clone; ElevenLabs stays available where Fish falls short. Each person has their own voice, and the
  source, licence and permitted use of every reference recording are recorded (section 11.4).
- **Video**: ElevenLabs Kling 3.0 Pro image-to-video for the motion and Sync 3 for the lip-sync, the approved recipe.
  Dreamina may make a colorization or a take under the same rules; a Dreamina take's "AI" label is cropped out by
  framing, never erased or inpainted. No local video model is used.
- **Their words** stay labelled game dialogue (section 3.2): the voice speaks the owner-approved advisor line word for
  word, never a quote and never a line the person said.
- **English first.** Hebrew clips come later, each after a native reviewer approves its Hebrew (OQ-4).
- **The fictional aides** (section 3.1) are retired (section 11.3).
- **Terms.** Before the first clip ships, the Fish Audio and Dreamina terms that apply to a real person's voice and
  likeness are read and recorded, as section 13.2 does for ElevenLabs. D-D (`docs/oss/DECISIONS.md:25`) covers only the
  start-screen painting.

The approved Ben-Gurion test approves the recipe, not any later file (section 11.5).

**How one English clip is made:**

1. Start from the photograph in section 11.1 and its approved faithful colorization; crop to 16:9 so no label or
   watermark is in frame.
2. Make the voice line with Fish Audio from the final, owner-approved English advisor line. The words heard match the
   line exactly: nothing added, dropped or paraphrased.
3. Make the silent motion with Kling 3.0 Pro (or a Dreamina take), with a calm prompt. Then lip-sync it with Sync 3 to
   that voice file.
4. Reject a take with identity drift, a changed face, hair, clothing or uniform, invented insignia, a changed eye patch,
   added objects or people, animated or changed bystanders, deformed hands or face, or implausible mouth motion. (The
   first Golda take animated the crowd and added a child; a second take was ordered.)
5. Export the picture as a silent clip (section 12.1) and the voice as the PCM WAV the lip-sync used.

### 11.3 Estate consent and the content policy (decided 24 October 2026)

The policy and D-ADV, as merged on 23 October, allowed only a stylized, non-photoreal portrayal, made locally, with a
fictional aide's voice, labelled "Stylized portrayal. Fictional dialogue and voice.", and a photoreal likeness or a
voice clone only after written consent from the estates. The chosen route is a photoreal likeness with cloned voices
made with third-party services, so the owner was asked OQ-13 to OQ-17. The owner's answers, 24 October 2026, in the
coordinator session, verbatim:

| # | Question | Owner's answer | What it means |
|---|---|---|---|
| OQ-13 | Estate consent: do the estates' letters go out first, or is the rule amended? | "Amend the rule" | Real public figures from archival photographs may be animated and voiced without written estate letters, labelled as AI. No letters are sent |
| OQ-14 | Amend the rest of the advisor exception to the chosen route? | "Amend to my route" | A narrow exception for the three advisors only: real archival photographs (faithful colorization recorded in the credit), AI animation, close-to-real cloned voices, labelled, each clip reviewed |
| OQ-15 | The Golda photograph of 24 July 1950 (GPO, `PD-Israel`, children of Kibbutz Shefayim) | "you can ship as is but it doesnt look good at the moment - i think the fact that there are too many people is making elevenlabs do a bad job", then "Tighter crop, same photo" and "B: medium" | The photograph stays under the narrow exception, cropped to box (850,160,2150,891) of the colorized master; no alteration or animation of bystanders is intended (section 11.1) |
| OQ-16 | The disclosure's wording | Option 2, "AI animation and voice. Fictional dialogue.", and "make sure tha label is small" | That exact text, shown small: caption size, not a banner, at the text floor (section 12) |
| OQ-17 | Retire the fictional aides? | Answered in substance by D-ADV's "The real figures talking" | The aides are retired, and OQ-1 and OQ-2 (their names) are closed; their lines are removed from this spec |

The same day the owner also ruled that lines without a clip show the advisor's photograph: "the rest of the advisor
screen would use their photos for now until we have more tolens" (#1547; sections 2, 12).

`docs/CONTENT_POLICY.md:104-118` and D-ADV (`docs/oss/DECISIONS.md:70`) are amended in this PR, quoting the answers.
This reading is factual, not legal advice.

### 11.4 Credits, licences and records, per clip

- **On screen**: each clip has its own credit, reachable from the clip (beside Watch, and in the overlay): the source
  photograph, its date, photographer or agency, collection and id, licence, and what was changed ("colorized, animated,
  lip-synced, cropped", as applies). Dayan's credit is the full CC BY 4.0 line of section 11.1 with the licence linked.
  `NOTICE` repeats every credit a licence asks for.
- **`ASSETS.yaml`**: one row per shipped file, marked AI-generated: the source photograph and its licence, every service
  and model used (with versions where shown), the voice and its reference recordings with their sources and licences,
  the dates, the exact script, the settings where the service shows them, the owner's accepted terms that apply, and the
  file's sha256 (section 13.3).
- **Disclosure surfaces**: `docs/ai-disclosure.md`, the README's AI section and the Steam draft change in the same PR as
  the first shipped clip (`06-ai-voice-music-and-disclosure.md:23`).

### 11.5 Per-clip review

No clip ships on the recipe's approval alone. The owner reviews each final file, identified by its sha256, for: the line
(approved game dialogue, not styled as a quote); the voice's likeness; the face, clothing, uniform, insignia, eye patch,
pose and bystanders against the photograph; lip-sync, timing and pronunciation, with no word added or missing; the
disclosure label for the whole clip; and a complete credit, licence and `ASSETS.yaml` row. A changed file needs a new
review. Approving one language approves no other.

## 12. Video and sound

- **Never autoplays.** The popup's play badge ("Watch") plays the available media, as do the encyclopedia and Counsel.
  Per line: play the clip (or a supplied still) when present; otherwise keep the advisor's portrait visible and play narration when present;
  otherwise show the portrait and text with no Watch control. "Present" means available in the current UI language.
- **Small, inside the popup** where the popup's slot has room for the picture, two caption lines and the buttons (about
  460 logical px tall): 1920x1080, 1280x720, 1366x768, 1024x768 and 960x1080. The picture, 364x205 at 400 px wide,
  takes the portrait row's place when a clip or explicit media still exists. With narration only, the portrait row stays;
  the live caption uses the text's place. The buttons stay.
- **In a centred overlay** where it has no room: a phone held sideways (400 px free) and an upright phone (the sheet).
  The overlay follows `.outcome-layer` (`Statecraft.uss:322-332`), keeps the captions under the picture, and closes with
  its own X and Escape. The choice is made from the measured slot height, not from a guess at the device.
- **Captions are live text**, never burned into the clip: timed cue strings per language (`advisor.<id>.cue.<n>` with a
  start time), the same words as the voice, under the text floor and direction rules, and in Counsel's history. A
  version with burned captions may be made for sharing outside the game; it is not the game's file.
- **The disclosure is live text** too, for the whole clip, separate from the caption and the credit: "AI animation and
  voice. Fictional dialogue." (owner, 24 Oct 2026, section 11.3). It is small: one muted line at caption size, not a
  banner, at the bar's small text size (`HudPresenter.SmallTextPx`, 15 panel px, `HudPresenter.cs:83`) raised to the 12
  screen px floor by `HudPresenter.SmallTextSize` (`:87`). It sits under the live caption: under the picture in the
  popup, and under the picture's caption in the overlay; never over the picture. Its key is the media row's `label`.
- **The picture is a silent clip** on the existing player. `CardVideo` is silent by design: "A silent clip player ... No
  audio is output" (`CardVideo.cs:9-15`), `audioOutputMode = VideoAudioOutputMode.None` (`:102`), into one 960x540
  RenderTexture (`:18`, `:90-109`); the encoder passes `-an` (`tools/encode_event_videos.py:42-45`) and its check rejects
  any audio stream (`:111-126`). Neither changes: the sound Sync 3 puts in its output is dropped from the picture. A line
  without a clip does not borrow the video player; it uses the advisor's existing portrait.
- **The voice is a separate audio file**, the same WAV the lip-sync used, fetched and played the way the start screen's
  recording is (`DeclarationRecording.cs:41-56`: a fetched `AudioClip`, an output with a play position; `SourceOutput`,
  an `AudioSource`, `:240-261`). The captions follow the audio's play position, and so does the picture: lip-sync makes
  drift visible, so the player seeks the picture back to the voice when they drift apart (the plan's Task 24, `DriftS`).
  Any button, the X, a layer opening over the popup, or the message ending stops both the sound and the picture.
- **UNVERIFIED: sound from Unity's WebGL video player.** The design does not need it. Muxing sound into the clip would
  first need that verified, the browser's gesture rules checked, and the encoder's no-audio check changed; none of that
  is in S6.
- **One player, borrowed.** The popup borrows the decision card's `CardVideo` (`GameRoot.cs:254`): it plays only while no
  card is open, and the card's illustration plays only while the card is open. On hand-over it calls `Stop()` (`:74-80`)
  and the card puts its clip back with `ShowIn` (`:66-72`). The outcome clips keep their own player (`GameRoot.cs:276`).
- **Lazy**: the `CardVideo` pattern: one player made the first time a clip is asked for, never at boot, playing from
  `StreamingAssets/video` by URL with the build id (`CardVideo.Url`, `:40`), and off in every automated run
  (`CardVideo.ForcedOff`, `:29-37`); the voice files likewise. The load-time gate holds: the default first download
  does not grow beyond code-size noise, and boot time is measured on base and PR.
- **In the browser**: each clip and its voice are checked together in Chrome and Safari WebGL: first play, stop, replay,
  closing the overlay, a layer opening during playback, and the picture staying in step with the voice.

### 12.1 The format and the first English clips (24 October 2026)

The 23 October Hebrew photo-video samples (popup-v1) are superseded. Made outside the repository on 24 October, nothing
shipped: Ben-Gurion's `opening.greeting` (approved, "looks good"), Dayan's `opening.move` and Golda's `opening.city`
(waiting for the owner's review), each at 1080p with the voice muxed, for review only. What ships follows:

- **Format**, matching `FFMPEG_VIDEO` (`tools/encode_event_videos.py:43-45`) and the 960x540 RenderTexture: 960x540, SAR
  1:1, 24 fps, H.264 High (level 3.1), yuv420p, `-preset slow -crf 28`, `-g 48 -keyint_min 48 -sc_threshold 0`, `-an`,
  `-movflags +faststart`, `-map_metadata -1`. Each file passes the checks of the encoder's `problems()`. The player draws
  into one 960x540 texture, so a 960x540 clip fills it 1:1.
- **Quality**: CRF 28 unless the owner picks CRF 20 (OQ-3); sizes are measured per clip, since talking footage differs
  from the old photo moves.
- **Voice**: 16-bit PCM WAV, the file the lip-sync used, so its timing matches the picture.
- **Caption cues**: one sentence at a time, switching at a pause found in the voice file; at most two lines; Hebrew
  right to left, right-aligned, when Hebrew clips come.

## 13. Voice and ElevenLabs

### 13.1 What the rulings change, and what exists

The owner's "you can sythesize their voices" lifts the rule that a synthetic voice never speaks as a real person, for
these three advisors. On 23 and 24 October the owner asked for voices as close to the real people as possible and
accepted ElevenLabs' and the Israel State Archives' terms (section 11.2), and on 24 October amended the estate-consent
rule: "Amend the rule" (section 11.3).

Done on 23 October 2026 (outside the repository, nothing shipped): the aide voices were designed with ElevenLabs Voice
Design (`eleven_ttv_v3`) from prompts that name no real person and ask to resemble no one; after the owner's "Yes,
both", five were saved as private voices (the Prime Minister's office aide for both languages, and an English and a
Hebrew voice for each of the other two), and 43 Hebrew advisor lines were rendered with `eleven_v3`. Those Hebrew lines
are not used: the aides are retired (section 11.3).

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
can give. The owner read this and amended the estate-consent rule on 24 October (section 11.3). This reading is
factual, not legal advice. The game being free forever and
without ads does not remove any consent, licence or credit duty.

### 13.3 The account and the records

- The owner's paid ElevenLabs plan, no Beta features. ElevenLabs' documentation calls Voice Design experimental; if the
  product marks the workflow Beta, production stops for the owner's decision.
- The voices stay private and are never shared to the Voice Library.
- Every output gets an `ASSETS.yaml` row: AI-generated, the tool, the voice id, the model, the date, the exact script, and
  the file's sha256, as the video-script rules ask (`06-ai-voice-music-and-disclosure.md:19`).
- `docs/ai-disclosure.md`, the README's AI section and the Steam draft change in the same PR as the first voiced video
  (`06-ai-voice-music-and-disclosure.md:23`).

## 14. Localisation, English and Hebrew

- Every advisor string and caption cue has an English and a Hebrew key; the existing parity tests (`HebrewUiTests.cs`)
  cover them.
- **The player is addressed gender-neutrally** in Hebrew, by HE-03 (`docs/WRITING_UI_HE.md:73-76`): no masculine-only
  singular, no slash forms; plural address or "יש ל..." forms. The advisors speak in their own grammatical gender
  (Meyerson in the feminine), which is the speaker's, not the player's.
- Hebrew lines are agent-drafted and go to the native reviewer before they ship, as the rest of the UI.
- Names: "דוד בן-גוריון", "גולדה מאירסון (לימים מאיר)", "משה דיין"; the people rows (`content/people.yaml:30`, `:586`,
  `:1120`) hold the first and third.
- The popup stays on the right in Hebrew; its header, buttons and text direction mirror inside it (section 7.1).

## 15. Persistence

- The help level, each message's "seen" mark and the opening chain's mark are lines in the settings file
  (`SettingsStore`, `unity/Assets/Scripts/SettingsStore.cs:8-13`: "presentation only, never the sim or the save"), one
  `key=value` line each, as the guide's `first_turn_guide=seen` (`FirstTurnGuidePresenter.cs:35`).
- Seen marks are kept compact: one line, `advisor_seen=<id>,<id>,...`.
- Nothing goes in the save. A loaded game keeps the player's seen marks (they are the player's, not the game's).
- With no settings file (automated runs, EditMode tests) the advisors never show by themselves, as the guide today
  (`FirstTurnGuidePresenter.cs:16-17`). So no golden, smoke run or browser check changes.

## 16. Accessibility, motion, performance and testing

**Motion.** The popup slides 16 px in from the right edge while it fades in, over 0.25 s ease-out, and fades out over
0.15 s, with no delay before it shows. It reuses `ScreenNav.Show`'s opening and closing classes (`ScreenNav.cs:107-136`;
`Statecraft.uss:458-466`) with its own USS: a slide instead of the screens' scale. The game has no reduce-motion setting
today; if one is added, the popup only fades.

**Keyboard and focus.** The popup is not modal and does not take the focus when it appears; the map, the bar and the
info panel stay usable. It takes the focus only when the player opens it (the opening chain's first step, or Counsel),
and gives it back with `FocusReturn`. Its buttons are in the Tab order. Escape closes it only while the focus is inside
it, so Escape keeps closing screens as now (`ScreenNav.cs:182-186`). A key to jump into the popup is open (section 18).

**Screen readers.** The game has no screen-reader support today, and this design claims none. The floor is: every
meaning is live text (nothing only in sound or baked into video), the captions are live labels, contrast uses the
existing tokens, text keeps the 12 screen px floor (`HudPresenter.TextFloorPx`, `HudPresenter.cs:81`), focus is
visible, and every message can be read again in Counsel's history.

**Performance.** Signals are read on `Changed` and `TurnEnded` only, never each frame. Portraits are small prepared
files. Video and narration are lazy (section 12), and the one 960x540 texture is borrowed. Nothing loads at boot.

**Testing.** Each slice adds EditMode tests first and runs the full Unity EditMode suite on the PR merged with main, and
shows matched English and Hebrew before and after screenshots (`CONTRIBUTING.md`, "Pull requests"; the PR template's
"Visual evidence"). The message file has a load test (section 4.1). Signals are unit-tested on built states. Layout tests
cover 1920x1080, 1280x720, 1366x768, 1024x768, 960x1080, 390x844 and 844x390: the slot, the phone sheet, the text
floor, the Hebrew inside mirror, a `Show me` target under the popup, every modal layer, focus return, and which windows
play video inline. Media tests prove the live cues, the separate audio, that one press stops both, and that the clips
stay silent. The opening chain inherits the guide's 17 tests, with the event step restated (section 9.3). No golden
moves: the advisors never touch the sim.

## 17. Slices and build order

Each slice is its own PR, tests first, a full Unity EditMode run, English and Hebrew screenshots. **Every advisor line
goes to the owner for content review before it ships.**

| Slice | What | Depends on |
|---|---|---|
| S1 | The engine and the data format: message file, loader and its test, `AdvisorPresenter`, the queue behind the modal layers, the settings lines, and the side popup's shell (placement per tier, motion, focus, hiding under layers). No messages shown yet | - |
| S2 | The opening chain, replacing the first-turn guide (section 9) | S1 |
| S3 | The encyclopedia, replacing How to play (section 10) | S1 |
| S4 | Ongoing warnings and advice (section 5), the help-level setting (section 6) | S1; the budget signal waits for #1352 |
| S5 | The Counsel screen and history, disagreement (sections 7.4, 8) | S4 |
| S6 | The media player (the plan's S6a), then the English real-figure clips (S6b; sections 11, 12): the silent clips, the separate voice, the live captions and disclosure, the inline player and the overlay, the per-clip credits, the Dayan portrait (section 11.1) | S3; for S6b, the policy as amended on 24 October (section 11.3), the owner's review of each clip, the CRF, and the Hebrew review before any Hebrew clip (section 18) |
| S7 | No separate slice: the real-figure clips are S6b, under the policy as amended on 24 October (section 11.3) | - |

## 18. Open questions for the owner

| # | Question | Default until answered |
|---|---|---|
| OQ-3 | Which quality for the clips: CRF 28 (the existing pinned profile; 367 to 849 KB per clip) or CRF 20 (keeps the film grain; 1.08 to 2.83 MB)? Both play the same way (section 12.1) | CRF 28 |
| OQ-4 | Who reviews the Hebrew (the narration, the captions and the popup text) as a native speaker, and is that review done before S2 or only before S6? | No Hebrew advisor line or narration ships before a native reviewer approves it |
| OQ-5 | D-P accepted six named Government Press Office portraits for the free release. Does it also cover the new 1951 Ben-Gurion photograph you chose (possible URAA-restored US copyright)? | Used under D-P, with the risk note in its asset row |
| OQ-6 | The popup's still portraits (section 11.1) are the first version's. Should they switch to crops of the three video photographs, so the still matches the clip? | Keep the first version's stills |
| OQ-7 | Dayan's 1948-49 posts (battalion commander, commander in Jerusalem, Aluf in October 1949) are not yet in `content/people.yaml`. They matter only if a line names them | No line names them |
| OQ-8 | Does "Off" also keep the capture prompt (a capture awaiting an answer, R-CTRL-007)? It is a decision, not a threat | No: the capture dialog already asks |
| OQ-9 | The civ-movement spec changes `guide.move.text` (its section 8.2); if that lands first, S2 ports the new text | S2 ports whatever is on main |
| OQ-10 | The end screen has no player text for `defeat_war` (`strings.en.yaml:279-280` has `mvp_ended` and `defeat_economy`; the sim's English is "The enemy holds a city", `PlayerText.cs:27`). Ben-Gurion's defeat message needs it | S4 adds `end.outcome.defeat_war` in both languages |
| OQ-11 | Add Civ VII's folded tab: a 56 px portrait at the right edge under the bar, with a count, for messages waiting or put off with `Not now`? | No tab; the queue and Counsel keep the messages. If approved, it comes in S5 |
| OQ-12 | Which key, free of the existing bindings, moves the focus into an open popup? | No new key; Tab and the popup's openers reach it |

OQ-1, OQ-2 and OQ-13 to OQ-17 were answered on 24 October 2026 (section 11.3).

## 19. Policy edits in this PR

- `docs/CONTENT_POLICY.md` principle 2 (`:28-29`): advisor lines are labelled game dialogue, not quotes, under the owner's
  "Labelled game dialogue" ruling; real quotes only from a verified list.
- `docs/CONTENT_POLICY.md` the AI-likeness lines (`:99-100`): the advisors' synthetic voices ("you can sythesize their
  voices") and the real-figure videos only after the estates' written consent; the aide voices are not likenesses.
- `docs/oss/DECISIONS.md`: a D-ADV line recording the rulings.
- 24 October 2026 (#1530): the advisor exception in `docs/CONTENT_POLICY.md` (`:104-118`) and D-ADV amended to the
  owner's answers to OQ-13 to OQ-17 (section 11.3).

## 20. Risks

| Risk | Handling |
|---|---|
| An advisor line reads as a real quote | Labelled popup, never quotation marks; quotes only from section 3.3; the owner reviews every line |
| An advisor appears to take a side on a contested issue | Neutral game facts only (3.2); Deport never suggested; the owner's review |
| The popup covers something the player needs | The free right-column slot; the phone sheet; it moves off a `Show me` target; it hides under every modal layer and, on two rows, under a screen |
| Nagging | One teaching and one warning a turn, cooldowns, help levels, `Not now` |
| Silence on a real threat | Critical warnings at every level, "Off" included |
| A regression in onboarding when the guide goes | Parity first: the guide's tests pass on the chain before its code is removed; the event step's test is restated on purpose (9.3) |
| Captions unreadable or out of reach | Live, localised cue text at the text floor; never burned into the game's clip |
| Narration and picture drift, or keep playing under a layer | The audio's position drives the cues; any press or covering layer stops both |
| WebGL video sound does not work | Not needed: a silent clip and a separate audio file on the path the start screen already uses |
| A photograph's licence or date is misstated | The exact source, date, credit and risk note per photograph; later photographs dated; no upscaling |
| A synthetic voice of a real figure without the right consent | The owner amended the consent rule (11.3); every clip is labelled and the owner reviews each one before it ships (11.5) |
| A clip that misrepresents the photograph or the person | Real photographs only, faithful colorization, and the owner reviews every final file (11.1, 11.5) |
| Video growing the download or boot time | Never autoplays, lazy, StreamingAssets, never at boot; the load-time gate |
| Hebrew addressing the player in one gender, or mirrored photographs | HE-03 and the native reviewer; only the popup's inside mirrors |
| Advisors changing a golden | They never touch the sim; no settings file in automated runs |
