# Civ-style movement: design spec

**Status:** DRAFT for the owner's approval. No code until the owner approves this spec (tracking issue #1398).
**Date:** 23 October 2026. **Base:** origin/main `de78253e` (#1394 merged). Every file:line below was read on that commit.
**Plan:** [docs/superpowers/plans/2026-10-23-civ-movement.md](../plans/2026-10-23-civ-movement.md).

## 1. The request and the rulings

The owner (23 October 2026, verbatim): "we need the unit to be able to make a move inside the turn and not wait for the
next turn for it to happen. the unit must have a limit to how much it can move in a turn. we need the same mechanisem as
in civ. do a deep research on this"

The owner's answers to our four questions (23 October 2026, verbatim):

| Question | Ruling |
|---|---|
| Attacks | "Instant, like Civ" (an attack resolves the moment the player orders it) |
| Undo a move or attack before End turn | "No undo, like Civ" |
| Multi-turn go-to paths | "Now" (in this change) |
| End turn while units can still move | "Block like Civ" (End turn cycles to the next unit needing orders until every unit has moved, fortified or held, or been skipped) |

Already decided, and kept as they are (quoted, not reopened):

- **Q-MOVE-1 to Q-MOVE-5** (owner, 15 October 2026, "Take all recommendations", #375; `docs/superpowers/plans/2026-10-15-gdd-phase-5.md:374`, table `:921-922`): integer movement points per unit type and an integer cost per biome, lake and sea impassable (Q-MOVE-1); only the corridors Phase 4 builds lower the cost (Q-MOVE-2); a stack limit per hex (Q-MOVE-3); no zone of control (Q-MOVE-4); no transport (Q-MOVE-5). This spec changes no cost, no point value and no stacking rule.
- **D-A6** (owner, 14 October 2026; `docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md:478`): "The fixed order of §2.3. Actions are capped by Attention, with unit orders free." The resolver order (`:257-268`) is unchanged. One line of §2.3's command table, "An attack resolves in the resolution phase" (`:251`), is superseded by the owner's "Instant, like Civ"; section 9 lists the edit.
- **R-ENEMY-001**: the enemy gets its orders in resolver step 1, after the player's orders (`docs/rules/mvp.md:243`; `TurnResolver.cs:18`). Unchanged: the enemy still moves and attacks after End turn.

Two conflicts with earlier rulings come out of "Instant, like Civ". They are owner questions in section 10, with a recommendation each: Q-COMBAT-2 and Q-ARTY-1 (combined attacks and artillery support), and the timing of captures (R-CTRL-004).

## 2. What Civ does, and what we already have

### 2.1 Civ V, VI and VII

The research summary (`civ-movement-research.md`, 23 October 2026, from the civilization.fandom.com Movement, Zone of control and Combat pages for Civ V, VI and VII, and the Firaxis Civ VI UI Lua) and the checks made for this spec:

- Each unit has movement points a turn; each tile costs points to enter. A move happens the moment the player clicks, the points left shrink, and the unit can keep moving until they run out.
- Selecting a unit lights its one-turn reach; hovering shows the path, with turn numbers for a multi-turn go-to.
- Partial points: in Civ V a unit with any points left may enter a tile that costs more, which ends its move (CivFanatics, "Unit movement in Civilization V"). In Civ VI a unit that cannot pay a tile's full cost cannot enter it ("if a unit has a single movement left and the Terrain costs more than 1, the unit will be unable to move there", Civ VI wiki as quoted on CivFanatics, "Movement in terrain", thread 612700). No source we found confirms a Civ VI rule that a unit with full points may always enter one tile.
- Attacks resolve at once and normally end the unit's movement. There is no undo in V or VI.
- Units with no points left are dimmed. The game cycles to the next unit that needs orders, and End turn is blocked while one does: the button reads "A unit needs orders" and selecting it takes the player to that unit. Civ VI lets the player force the turn to end with Shift+Enter: "It is possible to force the turn to end (hold <shift> and press <enter>), but it would probably be best for you to give your units orders first" (Steam, Civilization VI discussions, thread 340412122418074543; a player's account, not Firaxis documentation). The same thread names Spacebar as the skip-a-unit key.
- The AI moves in its own phase, after the human ends the turn.

### 2.2 What main already does (verified)

- **The sim already moves at command time.** `Simulation.Step` (`sim/Statecraft.Sim/Simulation.cs:31-82`) runs the turn's orders in order in `RunOrders` (`:48`, `:100-116`), and only then `TurnResolver.Resolve` (`:60`): enemy orders (`TurnResolver.cs:18`), combat (`:19`), capture (`:20`), the order step (`:21`). `Move` is applied in `Execute` (`Simulation.cs:190-191`) through `Movement.Move` (`Movement.cs:58-84`).
- **Points per turn already exist.** `content/unit_types.yaml`: recruit 3, veteran soldier 3, artillery 2, armoured car 5, every Arab type 3; the halutz types none, so they cannot move (R-MOVE-001, `docs/rules/mvp.md:189`). `RunOrders` keeps a local `spent` dictionary (`Simulation.cs:103`) and `Movement.Move` charges each move against it (`Movement.cs:69-72`), so a unit can already split its points over several moves in one turn.
- **The preview ignores spent points.** `Movement.Reachable` (`Movement.cs:30-38`) and `Movement.Path` (`:42-51`) search with the unit's full points.
- **The wait is the Unity layer only.** `SimBridge` holds the turn's orders until End turn ("Orders take effect at End Turn", `unity/Assets/Scripts/UI/SimBridge.cs:10`), allows one Move per unit (key `move:{unit}`, `:65`), and the map draws `bridge.State`, the state at the turn's start (`GameRoot.cs:344`). The copy says so: "The unit moves when you end the turn" (`unity/Assets/UI/strings.en.yaml:89`), "Move ordered: the unit moves when you end the turn" (`:453`), "Attack ordered: the unit attacks when you end the turn" (`:467`).
- **Attacks are standing orders.** `UnitOrder(unit, attack, q, r)` sets the unit's order (`Combat.Give`, `Combat.cs:118-126`), and resolver step 2 resolves it every turn while the hex holds an enemy (`Combat.Resolve`, `:197-240`; R-COMBAT-001, `mvp.md:217`). Units of one side attacking one hex sum their strength (Q-COMBAT-2; R-COMBAT-002, `mvp.md:219`), and artillery fires only beside another attacker (R-COMBAT-007, `mvp.md:231`).
- **Saves are seed plus one command batch per turn** (`SaveGame.cs:30-46`, `Record` `:82-89`, schema 2 `:49`), replayed on load (`GameSession.Load`, `GameSession.cs:50-75`). Unity writes the autosave only after End turn (`GameStart.Autosave`, `GameStart.cs:64-90`, subscribed to `TurnEnded` at `:89`).
- **A dry run already exists.** `Simulation.DelegatedTo` runs `RunOrders` on a clone (`Simulation.cs:84-96`).
- **The goldens.** `docs/determinism.md:31-56` and `sim/Statecraft.Sim.Tests/golden/manifest.txt`: hash1950, hash1952, smoke5, smoke-orders, four scenarios, mvp-end. A probe for this spec played the golden run to the end on main (a scratch console program calling `GoldenRun.Orders` and `Simulation.Step`): the reference player orders **0** attacks and **0** moves in the whole run, and the run has **1** engagement, the Arab Legion's on 15 October 1948 (as `docs/determinism.md:374` records). None of the four scenario files names a `move` or `unit_order` (`grep` over `sim/Statecraft.Sim.Tests/scenarios/*.yaml`).

So the request is mostly a change to the Unity layer and the save, plus three real rule changes: instant attacks, go-to, and the "needs orders" rule.

## 3. Decision 1: the turn model

### 3.1 The choice

Two models were asked for:

- **(A) Live state.** Each move and attack changes `GameSession.State` at once; End turn runs only the enemy phase and the rest of the resolver.
- **(B) Committed orders, replayed.** The turn keeps an append-only list of the player's committed unit orders. What the screen shows is `Simulation.Preview(state at turn start, committed orders)`: the same `RunOrders` code `Step` uses, on a clone. End turn sends the committed list, then the other orders, to `Simulation.Step` as one batch.

**Decision: (B).** Reasons, ranked:

1. **The command log stays the single source of truth.** A save is still seed plus command batches (`SaveGame.cs:30-46`); every state, including the mid-turn one, is a pure function of the log. Model (A) would put a half-run turn into `GameSession.State`, so a mid-turn save would need either a state snapshot (the save format has none) or a split of `Simulation.Step` into two public calls that every golden, scenario and CLI path would have to call in the right order.
2. **What the player saw is what End turn computes, by construction.** `Preview` and `Step` share `RunOrders` (as `DelegatedTo` already does, `Simulation.cs:94`). The committed list runs first, in click order, so nothing the player does later can change a result already shown (section 3.3).
3. **No golden moves.** `Step(commands)` keeps its signature and meaning; the goldens and scenarios call it as today.
4. **Cost is small.** A preview re-runs the turn's committed orders on a clone: at most a few dozen searches on the 870-hex board per click.

### 3.2 Which orders are instant

An **instant** order is final the moment it is given: it changes the shown state at once, is appended to the turn's committed list, and cannot be withdrawn (the owner's "No undo, like Civ"). Instant orders:

- `Move(unit, q, r)`;
- every `UnitOrder`: attack (section 4), fortify, hold, send home, and the new go-to (section 6).

All other orders stay **deferred**, as today: queued, replaceable by key and withdrawable until End turn (`SimBridge.Key`, `SimBridge.cs:51-68`). That covers policy, budget, event answers, appointments, workers, training, city projects, pioneer work and stop work (halutz orders; a halutz cannot move, R-MOVE-001), Arms purchases, and the answer to a capture (`ResolveCapture`).

Why not more: the owner's rulings are about moving and attacking. Making Arms purchases or capture answers instant would rewrite #1143's queued Arms purchase and the capture dialog that #1324 and #1327 build on, with no ruling asking for it. Every instant order costs 0 Attention (`content/rules.yaml:20`, `:23`: `unit_order: 0`, `move: 0`), so the split does not touch the Attention budget.

`Simulation.IsInstant(Command)` is the one definition the session and the bridge both read.

### 3.3 End turn's batch and the RNG order

End turn calls `Simulation.Step(state, committed ++ deferred)`. Inside `RunOrders` the order is:

0. **Route step** (new, section 6.3): units with a go-to order continue it, in ascending id. No RNG draw.
1. The committed orders, in click order. Each player attack draws once (section 4).
2. The deferred orders, in queue order. A legislature vote draws once (`Simulation.PassVote`, `Simulation.cs:208-214`).

Then, as today, `EventSystem.Delegate`, resolver step 1 (the enemy's orders, no draw, R-ENEMY-001), step 2 (the enemy's attacks, one draw each), and the rest.

So the RNG stream is fixed by the log: replaying a save draws the same values in the same order. Because the committed orders run before every deferred one, a vote queued after an attack cannot shift the attack's draw, and withdrawing a deferred order never changes a result already on screen.

One consequence, stated so nobody is surprised: an event answer is deferred (it takes effect at End turn, as the card says, `strings.en.yaml:91`), so on the turn a truce card is answered, attacks made before End turn come before the acceptance. R-COMBAT-009 binds "from accepting the card" (`mvp.md:237`), and the acceptance happens at End turn, so this follows the ruling's own words.

### 3.4 Mid-turn save and reload

"No undo" means a move or attack that looks done must not vanish on reload. Today it would: the autosave is written only after End turn (`GameStart.cs:89`), so a reload goes back to the turn's start.

Decision:

- The save gains an **open turn**: the committed orders of the turn in progress, written after the last `turn` block as `open` followed by one command per line. Save schema 2 becomes 3 (`SaveGame.cs:49`). The `end` hash stays the hash after the last completed turn; the open orders are checked by replaying them.
- Unity writes the autosave after every committed order as well as after End turn.
- On load, the turns replay as today, then the open orders are committed again in order. A refused one throws `SaveException` when the content is the same (the save was edited or the build has a bug), or is dropped with the `replay_differs` warning when the content changed (`GameSession.cs:41-43`).
- Deferred orders are not saved, as today: after a reload the player gives them again. Skips (section 7) are not saved either; they change nothing in the game.
- Reloading cannot re-roll an attack: the open turn replays the same orders on the same state, so the same draws come out.

**Old saves.** A schema 2 GDD save replays as before, with one exception: if any turn holds a player attack (`order <unit> attack <q> <r>`), that attack was a standing order under the old rules, so the replay would differ. Such a save is refused with the existing `older_rules` code (R-SAVE-001, `mvp.md:53`), which the UI already words ("save.refused.older_rules"). A schema 3 save loaded by an older build is refused as `newer_version` (R-SAVE-002, `mvp.md:55`), as today.

## 4. Decision 2: instant combat

### 4.1 The attack

`UnitOrder(unit, attack, q, r)` resolves at once, inside `RunOrders`:

- Refused, with the state unchanged and a reason, for everything `Combat.Check` refuses today (`Combat.cs:96-113`), less "That unit already has that order" (there is no standing attack any more), and also when the unit has no movement points left this turn: "That unit has no movement points left this turn".
- The Arms check reads the stock as it is: an attack needs `mvp.arms_per_attack` (artillery: its `arms_per_fire`) and the stock must hold it. The "other attacks already ordered" part of Q-COMBAT-4's check (`Combat.cs:109-112`) goes, since no player attack waits for step 2. The Arms are paid at once.
- One engagement is fought at once (section 4.2), with one RNG draw.
- The attack ends the unit's movement for the turn (its spent points become its full points) and clears its order (a fortify or hold is lost, as giving an attack replaces the order today, R-COMBAT-001).
- Losses, retreat and surrender follow R-COMBAT-003 unchanged (`mvp.md:221`). The attackers never advance (Q-COMBAT-3).

### 4.2 Who fights in the engagement (owner question OQ-1)

Q-COMBAT-2 says "attacks from several directions add their strengths", and Q-ARTY-1 says artillery "adds its strength to an attack within its range and never attacks alone" (`docs/gdd/military-combat.md:15`, `:17`). Both assume attacks that wait for one resolution step. With instant attacks there is no moment when several of the player's attacks meet. This is a conflict with an owner ruling, so it is OQ-1 (section 10). The recommendation, which the plan builds:

**(a) One unit per attack, like Civ.** Each attack is its own engagement: the attacking unit alone against the defenders. Artillery attacks alone from range, and since R-COMBAT-007 already spares artillery from the attackers' losses (`Combat.cs:268-269`), its attack takes no losses, which is what a Civ ranged attack does. The "holds its fire" clause of R-COMBAT-007 retires.

The two alternatives, for the owner:

- **(b) Every ready unit joins.** The attack also takes every other player military unit in reach of the hex that still has movement points, each paying its Arms and losing its points; artillery joins only beside another attacker. Keeps both rulings. The player cannot keep a unit out of an attack.
- **(c) Ready, then strike.** A unit can be readied on an enemy hex (it spends its points, pays nothing yet); the next attack on that hex this turn takes every unit readied there, summed; artillery can only be readied, never strike first. Keeps both rulings and gives control, at the cost of one more order and more UI.

The enemy phase is not part of the question: resolver step 2 keeps R-COMBAT-002 for the enemy's standing attacks, summed, as today (the owner: enemy attacks stay in the enemy phase).

### 4.3 RNG and determinism

A player engagement draws `Combat.DrawsPerEngagement` (1) value at the moment its attack runs in `RunOrders`; the enemy's engagements draw in step 2 as today. `docs/determinism.md` gains the "Combat draws" section that `Combat.cs:21` and `CombatDrawCountTests.cs:4` already cite but that does not exist on main (a dangling reference this change closes), stating both orders.

### 4.4 Capture (owner question OQ-2)

Captures happen at End turn, in resolver step 3 (R-CTRL-004, `mvp.md:39`; `TurnResolver.cs:20`). **Recommendation: keep it so.** Reasons: the player's unit can only stand on an enemy-held target with no enemy military unit on it (Move refuses an enemy unit's hex, `Movement.cs:66`), so instant capture would change only the moment the capture is recorded; it would move the capture dialog into the middle of the turn and rework the flows #1324 (Occupy) and #1327 (Deport) are building on; and a unit passing through a target in one move would capture it, which today it does not. The owner may prefer Civ's instant capture; it is OQ-2.

### 4.5 Truces

R-COMBAT-009 (`mvp.md:237`) keeps its refusal ("A truce is in force"), checked at the click. Its clause "Accepting a truce ends and clears every standing attack of the player's" has nothing left to act on, since the player has no standing attacks; `Combat.EndAttacksForTruce` (`Combat.cs:75-84`) and its call are removed. The enemy's truce guard (R-ENEMY-006) is untouched.

### 4.6 Other readers of a player attack order

- `Demobilization.Front` (`Demobilization.cs:27-42`) reads a unit's attack order to find its front; a player unit never has one now, so the "attacks" half never applies and the "borders" half decides. No behaviour a player can reach changes: an attacking unit borders its target.
- `ReferencePlayer.Attack` (`Reference/ReferencePlayer.cs:44-63`) issued standing attacks. It now issues its attacks each turn it wants them, counting this turn's Arms as it goes. It orders none in any golden run today (section 2.2), so no golden moves.

## 5. Decision 3: movement points

- `Movement.Reachable(c, s, unit, spent)` and `Movement.Path(c, s, unit, q, r, spent)` search with the points left, the unit's points less `spent[unit]`. The three-argument forms stay, as "nothing spent". The Codex draft on branch `codex/civ-move-reach-spent` (cx135) has exactly these overloads and a read-only `Simulation.Preview`; the plan starts from it.
- `Simulation.Preview` returns the state, the points spent by unit, the turn's Why lines so far and the rejections.
- **The "always one hex with full points" rule (OQ-3, optional).** Civ VI, the model our strict rule already follows, makes a unit pay a tile's full cost (section 2.1). Civ V lets a unit with any points left enter one more tile. We found no confirmed Civ VI exception for a unit with full points. **Recommendation: keep the strict rule; no change.** Consequence the owner should know: artillery (2 points) can never enter mountains (cost 3), on main today too (a probe listed every type against `mvp.move_cost`: artillery against mountains is the only case). The go-to search therefore skips any hex that costs more than the unit's full points (section 6.2), so artillery routes round mountains instead of getting stuck. No terrain cost changes.

## 6. Decision 4: multi-turn go-to

### 6.1 The order

`UnitOrder(unit, go_to, q, r)`: a standing order, saved as `order <unit> go_to <q> <r>` (the save already writes a hex for a unit order, `SaveGame.cs:127`, `:226-228`; no new save syntax). It is stored in the unit's existing `Order` and `OrderTarget` fields (`GddState.cs:110-140`), so `GameState` gains no field and the hash changes only for a unit that has the order.

Given, it is refused with the state unchanged and a reason for an unknown or enemy unit, one that cannot move or is already there, water or off the board, a hex holding an enemy unit, a closed locality (R-MOVE-005), or a hex no route reaches. Otherwise it replaces the unit's order (a fortify is lost) and the unit moves at once as far as its points left reach along the route (section 6.2). If that reaches the hex, the order is done and cleared.

### 6.2 The route

`Movement.Route(c, s, unit, q, r, spent)` is the cheapest path to the hex with no points limit, over land, never through an enemy unit's hex or a closed locality, and never into a hex whose cost is more than the unit's full points. Each hex carries the turn it is reached on: turn 1 spends the points left now, every later turn the full points; a step that does not fit what is left of a turn starts the next turn. It is the same search as `Movement.Search` (`Movement.cs:149-178`), so its ties break the same way (R-MOVE-002). The path preview shows these turn numbers.

The per-turn stop is the last hex of that turn. If it is full of the unit's side (R-MOVE-003; a path may pass through a full hex), the unit stops at the last earlier hex of that turn with room; if none has room, it waits where it is and keeps the order.

### 6.3 Each turn

At the start of each player turn (step 0 of `RunOrders`, section 3.3), in ascending id, each player unit with a go-to order:

- **stops** (order cleared, a Why line, and it needs orders) when an enemy military unit is next to it (agent default: a march does not walk into contact unasked), when no route reaches the hex any more (the hex now holds an enemy unit, or the way is shut), or when the hex itself is full on the turn the unit would arrive;
- otherwise moves along a fresh route as far as its points reach (the route is recomputed each turn from where the unit is), and on arriving clears the order.

The move spends the unit's points for that turn as any move does, so the player can see and use what is left.

The preview shows the route as the board stands. Recomputing it each turn can change the arrival turn when units move in the meantime, or, rarely, when two paths of equal cost split their steps across turns differently (the rest of a cheapest path is itself a cheapest path, but the tie-break from the new hex may pick another one). A test pins that on a board with no such tie and no other change, the unit arrives on the turn the preview showed.

## 7. Decision 5: End turn waits for orders

### 7.1 Which units need orders

`Movement.NeedsOrders(c, s, spent)`, in ascending id: the player's military units (side `israel`) with movement points (the type has some) that have **not moved or attacked this turn** (`spent[unit]` is 0) and **have no order** (`Order` is empty: fortify, fortifying, hold and go-to all count as orders). The bridge then leaves out the units the player skipped this turn.

This follows the ruling's wording: End turn waits "until every unit has moved, fortified/held, or been skipped". Civ V and VI keep a unit with points left in the cycle even after it moved; players complain that this stalls the turn for units that cannot move with what they have left (the Steam thread cited in 2.1). The ruling's reading avoids that. If the owner wants Civ's stricter reading, `NeedsOrders` changes one condition.

Halutzim never need orders: they cannot move (R-MOVE-001). Enemy units never do.

### 7.2 Skip, fortify, hold, sleep

- **Skip this turn** (new, UI only): the unit is left as it is for this turn and drops out of the cycle. It is not an order and is not saved, so it changes nothing in the game or the goldens. It ends with the turn.
- **Fortify** and **Hold** are the existing standing orders (R-ORDER-001 to R-ORDER-003). They keep a unit out of the cycle on every turn until it is given another order or (fortify) moves.
- **Sleep**: not added. Civ's sleep is fortify for a unit that cannot fortify; ours can all fortify (artillery may fortify, R-COMBAT-007), so it would duplicate fortify.
- **Go-to** keeps the unit out of the cycle until it arrives or stops.

### 7.3 The cycle and the button

- While any unit needs orders, the End turn button reads "A unit needs orders" (Civ VI's wording). Pressing it selects the next such unit after the one selected (ascending id, wrapping), centres the camera on it and opens its card with Move ready, using the camera centring #1394 added for the guide (`GameRoot.cs:480-492`).
- After an order that finishes a unit for the turn (Skip, Fortify, Hold, Send home, an attack, or a move that spends all its points), the next unit that needs orders is selected the same way (Civ's automatic cycling). A move that leaves points does not move the camera.
- **Shift+Enter ends the turn anyway**, as in Civ VI (section 2.1). It does nothing while a text field has focus. The button's tooltip says so. There is no on-screen force button, as in Civ.
- Spacebar is not taken for Skip, as Civ VI does: in our keyboard map Enter and Space press the focused button (`docs/superpowers/plans/2026-10-14-a11y-keyboard-map.md:27`). Skip is a button on the unit card.

## 8. Decision 6: the UI

All new strings follow `docs/WRITING_UI.md` and `docs/WRITING_UI_HE.md` (binding for new strings, #1395) and `docs/UX_GLOSSARY.yaml`: sentence case, "Label: value", a verb first on buttons, no directional words, matching keys and placeholders in both files. The Hebrew below is agent-drafted and needs the native reviewer, like `strings.he.yaml:77`'s note.

### 8.1 What changes on screen

- **The map shows the state after the turn's committed orders.** `SimBridge.State` becomes the preview's state; `SimBridge.TurnStart` is the state the turn began with. Tokens move at once (`GameRoot.cs:344` already redraws on `Changed`).
- **The lit reach shrinks.** Move's lit hexes (`InfoPanelPresenter.MoveTargets`, `InfoPanelPresenter.cs:180-186`) come from `Reachable(..., spent)`, so after a move the next pick lights only what the points left reach. Several moves per unit per turn; the one-Move-per-unit key goes.
- **Points on the card**: "Movement points: {0} of {1}".
- **Dimmed tokens**: a player unit with no points left this turn is drawn dimmed until the turn ends.
- **Attack targets** stay lit from `Combat.Check` (`InfoPanelPresenter.cs:192-204`), now with the unit's spent points. The odds show the unit alone (OQ-1 (a)). After the click, the result shows at once in the toast: the engagement's Why lines (losses, retreat, surrender, destruction) in the player's language (8.4).
- **Go-to**: in Move's pick, the lit hexes are this turn's moves; any other land hex a route reaches shows the route with a turn number on each turn's last hex, and a click on it gives the go-to. On touch, the first tap on such a hex shows the route and a second tap on it confirms.
- **Turn start**: a unit whose go-to arrived or stopped gets a toast line: the route step's Why line in the player's language (8.4).

### 8.2 New and changed copy (EN, then the HE draft)

| Key | English | Hebrew draft |
|---|---|---|
| `guide.move.text` (changed) | Select one of your units, press Move, and choose a lit hex. The unit moves at once, and the lit area shrinks to the movement points it has left. | בחרו אחת מהיחידות שלכם, לחצו על "תנועה" ובחרו משושה מואר. היחידה זזה מיד, והאזור המואר מצטמצם לנקודות התנועה שנותרו לה. |
| `unit.move_pick` (changed) | Choose a lit hex to move to now, or another hex to send the unit there over several turns | בחרו משושה מואר כדי לנוע אליו עכשיו, או משושה אחר כדי לשלוח אליו את היחידה במשך כמה תורות |
| `unit.points` (new) | Movement points: {0} of {1} | נקודות תנועה: {0} מתוך {1} |
| `unit.route_turns` (new) | Turns to arrive: {0} | תורות עד ההגעה: {0} |
| `unit.go_to_given` (new) | On its way to the chosen hex. Turns to arrive: {0} | בדרך למשושה שנבחר. תורות עד ההגעה: {0} |
| `unit.order.skip` (new) | Skip this turn | דילוג על התור |
| `unit.hold_queued` (changed) | Holding its hex | מחזיקה במשושה שלה |
| `unit.send_home_queued` | removed: the unit leaves the map at once, so the card closes | |
| `unit.move_queued` | removed: nothing waits for End turn | |
| `unit.attack_queued` | removed: the attack's result shows at once, through its Why lines (8.4) | |
| `hud.end_turn_needs_orders` (new) | A unit needs orders | יחידה ממתינה להוראות |
| `hud.end_turn_force` (new, tooltip) | Shift+Enter ends the turn now | Shift+Enter מסיים את התור עכשיו |
| `how_to_play.end_turn` (changed) | End turn moves to the next month. While one of your units still needs orders, the button selects it instead: move it, fortify it, hold or skip it. Shift+Enter ends the turn anyway. A card you leave unanswered, or have no Attention left to answer, goes to the responsible minister, who decides it for you. | סיום התור מעביר לחודש הבא. כל עוד אחת היחידות שלכם ממתינה להוראות, הכפתור בוחר בה במקום זאת: הזיזו אותה, בצרו אותה, החזיקו בה או דלגו עליה. Shift+Enter מסיים את התור בכל זאת. כרטיס שלא עניתם עליו, או שלא נשארה לכם תשומת לב לענות עליו, עובר לשר האחראי, והוא מחליט במקומכם. |

Unchanged on purpose: `unit.fortify_queued` (fortify still sets at the end of the turn, R-ORDER-002), `guide.event.text`, `guide.city.text` and the budget lines (those orders stay deferred).

The Why lines that name units, the attack's result and the go-to lines among them, are listed with their keys in the plan (Tasks 4a and 10a); the surrender line keeps R-POW-001's tone: nothing celebrates a capture. "Movement points" is a new term: the glossary gets a `movement-points` entry (decision-needed, Hebrew "נקודות תנועה" for the native reviewer).

### 8.3 The first-turn guide and #1392

The guide's move step (`FirstTurnGuidePresenter.cs:385-386`, `UnitToShow`) keeps its unit choice. Its text changes (8.2). #1394 (the fix for #1392, merged as `de78253e`) centres the camera on that unit (`GameRoot.cs:480-492`); the cycle in 7.3 reuses the same centring: the cycle slice moves its few lines into one helper both call. The guide must not be blocked by End turn: the guide's own steps do not press End turn, and its move step's unit, once moved, is no longer in the cycle.

### 8.4 Units named in the Why lines (#1399)

**The bug.** Every Why line about a unit names it by its sim id, in English: "Unit 37 moved to hex ...", "The attack on hex ... (unit 3 against units 4, 9)". `WhyPresenter` shows `Cause.Message` as it is (`unity/Assets/Scripts/UI/WhyPresenter.cs:55`). A sweep (Codex cx137, `raised-unit-sweep.md`) found 18 message paths at 17 sites in five files: `Movement.cs:78-82` (move, fortify lost), `Orders.cs:55-57`, `:69-73` (fortifying, holds, fortify lost at settle), `Combat.cs:75-82`, `:118-124`, `:210-238`, `:271-294`, `:299-300` (truce, attack ordered, attack ended, holds its fire, called off, the engagement's participants, destroyed, retreated, surrendered), `Demobilization.cs:63-74` (sent home), `SettlementControl.cs:107-120` (withdrawal refused, withdrew). One more path names enemy units the same way: `EnemyAI.cs:59-60` (an enemy attack ended by a truce). A raised unit has no other name anywhere; removed units (destroyed, surrendered, sent home) are gone from the final state, so the UI cannot look them up afterwards. The turn report's enemy lines already name enemy units by their row (`TurnReportPresenter.cs:112-128`, #1293).

**Why here.** This change rewrites the move and combat messages (instant attacks, go-to lines). Fixing the naming in the same pass avoids writing those lines twice.

**Decision.**

- The sim records a structured line on each cause that names units: `Cause.Key` (a stable line key from a fixed list, `WhyKeys`), `Cause.Units` (immutable snapshots, `UnitRef`: id, type, side, home city, taken when the line is written, so a removed unit keeps its identity) and `Cause.Args` (the line's other values as invariant text: hexes, counts, a ratio). `Message` stays as it is, the English developer line with the id, for logs and the sim's tests; the state hash never reads causes, so no golden moves.
- Unity formats a keyed cause from `why.line.<key>` in the player's language, with each unit named by one helper: a starting force by its row's name (Hebrew `name_he` where the row has one), as the turn report does; a raised unit by its type and home settlement, the identity #1396 gives it on the map (its type label and `ContentNames.Settlement`). `WhyPresenter`, the turn report and this change's toasts all use it. A cause with no key shows `Message` as today.
- The withdrawal refusal (`SettlementControl.cs:112`) is a refusal string, not a cause: it stops naming the unit ("A unit has no free hex next to {place} to withdraw to"), which the rule's wording allows (R-CTRL-010 fixes no text).
- Every new line this change adds (the instant attack, the go-to lines) is keyed from the start.
- Scope is the unit-naming lines of #1399. Other Why lines stay English-only as today.

**Sequencing.** A sim slice (S5) right after S2 adds the mechanism and converts the paths in `Movement.cs`, `Orders.cs`, `Combat.cs`, `Demobilization.cs` and `EnemyAI.cs`, before S3 and S4 write their new lines. `SettlementControl.cs`'s two paths sit inside the region #1327 rewrites (its hunk at `SettlementControl.cs:101-129`), so they are converted in S5 if #1324 and #1327 have landed, else in a one-commit follow-up right after they do; nothing is asked of Beri. The Unity formatting (U5) ships with U1, and needs #1396's raised-unit label.

## 9. Decision 7: goldens, saves and the rule text

### 9.1 Goldens

| Golden | Expected | Why |
|---|---|---|
| hash1950, hash1952, mvp-end | unchanged | the reference player orders no move and no attack in the run (probe, 2.2); the enemy phase is unchanged, and the enemy's march (`Movement.Toward`) is not touched: only the new `Route` uses the step-cost filter |
| the four scenarios | unchanged | no `move` or `unit_order` in their files |
| smoke-orders | unchanged | `GoldenRun.PlaySmokeOrders` steps one turn with one `Move` (`GoldenRun.cs:114-120`); the Move's meaning is unchanged |
| smoke5 (browser) | unchanged hash, new path | the run must now skip every unit that needs orders before End turn; Skip changes nothing in the sim |
| smoke-orders (browser) | unchanged hash, new path | the Move is committed at the click (checked on `Session.Committed`, not `Queued`), then the run skips the other units |

Each slice proves this by running the golden tests unchanged. If one moves, it is re-recorded with `STATECRAFT_UPDATE_GOLDEN=1`, a history row in `docs/determinism.md` and the SMOKE lines in `RELEASING.md`, never by hand.

### 9.2 Rule edits (`docs/rules/mvp.md`), for the implementing PRs

- **R-TURN-004 (new)**: the player's turn. Moves and unit orders are instant and final; the other orders are deferred; End turn's batch is route step, committed orders, deferred orders (section 3).
- **R-MOVE-001**: add that the points left are the type's points less what the unit spent this turn, the route step's move included.
- **R-MOVE-002**: `Reachable` and `Path` take the points spent this turn; add `Route` and its turn numbers.
- **R-MOVE-008 (new)**: go-to (6.1, 6.2). **R-MOVE-009 (new)**: the route step (6.3). **R-MOVE-010 (new)**: which units need orders (7.1).
- **R-ORDER-001**: an order replaces a go-to too; Skip is not an order.
- **R-COMBAT-001**: rewritten for the instant attack (4.1). **R-COMBAT-002**: step 2 resolves the enemy's attacks; a player engagement is fought when its order runs. **R-COMBAT-004**: Arms paid at the order, no call-off. **R-COMBAT-007**: per OQ-1. **R-COMBAT-009**: the standing-attack clause goes (4.5).
- **R-SAVE-003 (new)**: schema 3 and the open turn (3.4). **R-SAVE-004 (new)**: a schema 2 GDD save with a player attack is refused as `older_rules` (3.4).
- Unchanged: R-MOVE-003 to R-MOVE-007, R-ORDER-002 and R-ORDER-003, R-COMBAT-003, -005, -006, -008, R-ENEMY-001 to R-ENEMY-006, R-CTRL-004 (per OQ-2's recommendation), R-UNIT-005 (send home already "acts at once", `mvp.md:163`).

### 9.3 GDD text to amend (proposed; needs the owner's explicit approval)

- `docs/GDD.md:38`: "2. Move units, issue orders, start city projects and choose ministers." Add: "Moves and attacks happen at once; the other orders take effect at End turn."
- `docs/GDD.md:42`: "The order of resolution and the number of actions per turn are open." Stale since D-A6; replace with a pointer to D-A6 and R-TURN-004.
- `docs/GDD.md:76`: "The player can move units. Movement allowance, terrain costs, road and rail effects, stacking, transport and access to enemy territory are open." Replace with the decided Q-MOVE rulings and this spec's go-to and End turn rules.
- `docs/gdd/military-combat.md:13`: add Go-to to the standing orders (Move, Fortify, Hold, Attack).
- `docs/gdd/military-combat.md:15` and `:17`: Q-COMBAT-2 and Q-ARTY-1 as OQ-1 decides.
- `docs/gdd/military-combat.md:18`: "Enemy forces act in the first resolver step, before the player's planned actions" contradicts the code (the player's orders run first, `Simulation.cs:48`, `:60`) and R-ENEMY-001; correct to "after the player's orders".
- Impact spec `docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md:251`: "An attack resolves in the resolution phase" becomes "A player's attack resolves at once; the enemy's in the resolution phase".

## 10. Owner questions

| # | Question | Options | Recommendation | Blocks |
|---|---|---|---|---|
| OQ-1 | With instant attacks, how do several units attack together, and how does artillery fire? (reopens Q-COMBAT-2 and Q-ARTY-1, section 4.2) | (a) one unit per attack, artillery fires alone from range (Civ); (b) every ready unit in reach joins; (c) ready, then strike | (a) | the combat slices (S3, U2) |
| OQ-2 | Captures: at End turn as today, or the moment a unit enters? (section 4.4) | keep at End turn; instant | keep at End turn | nothing if kept |
| OQ-3 (optional) | Should a unit be able to enter one hex that costs more than its points left (Civ V)? (section 5) | keep strict (Civ VI, today); Civ V's rule | keep strict | nothing |
| GDD | Approve the GDD edits of 9.3 | | | the docs slice |

## 11. Conflicts and sequencing

| Item | Overlap | Handling |
|---|---|---|
| #1394 (fix for #1392, guide camera) | `GameRoot.cs`, `FirstTurnGuidePresenter.cs` | merged (`de78253e`); U4 moves its centring into one helper |
| #1396 (raised units draw no token; Codex cx134) | `UnitCard.SimUnit` maps starting forces only (`UnitCard.cs:25-30`) | must land before or with U1: a trained unit must be movable and cyclable; U5 reuses its raised-unit label |
| #1352 (Beri, Budget badge) | adds `BudgetUnsubmitted` to `SimBridge.cs` reading `queue` | no ask of Beri; if it lands first, U1 passes the deferred list there; if U1 lands first, #1352's one line takes `deferred` |
| #1324, #1327 (Beri, Occupy and Deport) | `Movement.Closed`, `TurnResolver.cs` step 10, `SettlementControl.cs` | no shared hunk for S1 and S4: they touch `Reachable`, `Path`, `Search` and `RunOrders`, not `Closed`; `ResolveCapture` stays deferred and capture stays at End turn (OQ-2), so their flows are unchanged. S5's two withdrawal lines are inside #1327's `SettlementControl.cs:101-129` hunk: converted after #1324 and #1327 land (8.4) |
| #1399 (units named by sim id in Why lines) | the cause messages of five sim files and `EnemyAI.cs:59-60` | S5 and U5 of this plan (8.4) |
| #1143 (avlco, HUD Arms, queued Arms purchase) | `HudPresenter.cs`, strings | Arms purchase stays deferred; the HUD reading `bridge.State` now shows the Arms after attacks, which is wanted; text conflicts only |
| #1155, #1154 (hex-tile art) | map rendering | U3's route numbers and U4's dimming are overlays on `MapScene` and `UnitLayer`; no shared files expected |
| #749 part 2 (Beri, enemy operation cards) | enemy phase | unchanged by this spec |
| #750 (Agam, R-WAR-004 slice) | `docs/rules/mvp.md` near R-COMBAT-009 | S3 edits R-COMBAT-009's text; whichever lands second merges the paragraph |
| #752 (war objective, war goldens) | a war scenario with player attacks would be written for the old standing attack | S3 lands before #752 writes its scenario, or #752 orders its attacks each turn |
| #798 to #800, Phase 6 | `ResolveCapture` | unchanged (deferred, capture at End turn) |

Slice order (details in the plan): S1 (preview, points left, needs-orders helper) then S2 (session and save) then S5 (units named in Why lines, #1399) then U1 with U5 (instant moves on screen, the Why lines' unit names); S3 then U2 (instant attacks) once OQ-1 is answered; S4 then U3 (go-to); U4 (End turn waits) last, since it needs Skip and the smoke runs' new path. The sim slices can be drafted by Codex workers; the Unity slices are Claude lanes.

## 12. Staff review of this spec (gaps found and closed)

1. A replaced deferred order (a vote) placed before an attack would have shifted the attack's draw after the player saw it. Closed by running committed orders first (3.3).
2. Reload would have undone moves (autosave only after End turn). Closed by the open turn and a per-order autosave (3.4).
3. Old saves with standing attacks would replay differently and fail as "damaged". Closed by refusing exactly those as `older_rules` (3.4).
4. The go-to search, with no points cap, would plan artillery through mountains it can never enter. Closed by the step-cost filter (5, 6.2), used only by the new `Route`; the enemy's `Toward` is untouched, so no golden can move from it.
5. Both smoke runs press End turn and would stall on the new block. Closed by a skip loop in both runs, with Skip kept out of the sim so their hashes stay (9.1).
6. "Combat draws" is cited in code but missing from `docs/determinism.md`. The combat slice adds it (4.3).
7. `military-combat.md:18` says the enemy acts before the player; the code does the opposite. Listed for correction (9.3).
8. Q-COMBAT-2 and Q-ARTY-1 cannot hold as written under instant attacks. Raised as OQ-1 rather than decided silently (4.2).
9. Arms purchases and capture answers were candidates for "instant"; kept deferred, because no ruling asks for it and #1143, #1324 and #1327 build on the queued flow (3.2).
10. The move and combat lines this change rewrites name units by sim id (#1399). Planned in the same pass, with unit snapshots on the cause so removed units keep their names (8.4).
