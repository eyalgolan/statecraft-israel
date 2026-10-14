# Keyboard map, scroll views and text size: plan

Date: 2026-10-14. Issue #41, task T39. Branch `docs/a11y-keyboard-plan-41` from `origin/main` `76331cc`.

A plan only. Nothing here changes code. It covers four findings of
[the accessibility audit](../../accessibility-audit-2026-10-14.md): B1 (the map is mouse-only), H1 (focus goes out of
sight in scroll views), H3 (text panels can't be scrolled from the keyboard) and H4 (text can't be made larger).
H2, M1, M3, M4 and M7 are being fixed in another session (T38) and are not planned here. H5, M5 and M6 are out of scope
and come up only where a plan below touches them.

Each item has its options, a recommendation, the code it changes, how it meets the existing keys, and how to test it.
Anything that changes how the game looks or plays is marked **Owner question**, with the choices. Per the owner's
ruling of 2026-10-12 (plan approval before code), no PR below starts until the owner has answered the questions it
depends on.

## The keys today

Checked against the code at `76331cc`:

| Key | Does | Where |
|---|---|---|
| W A S D, arrows | Pan the camera, whatever has focus | `InputSystem_Actions.inputactions` Camera map, `Pan`; `CameraController.cs:72-79` |
| Q, E | Orbit | Camera map, `Orbit` |
| Mouse wheel | Zoom; ignored while the pointer is over the UI | Camera map, `Zoom` (`inputactions:1083`); gated at `CameraController.cs:110` |
| Right drag | Pan | Camera map, `DragPan` |
| Tab, Shift+Tab | Move focus | UI Toolkit; `NavigationGuard.cs:15-20` drops every other navigation move, so the arrows never move focus (B6) |
| Enter, Space | Press the focused button | UI map `Submit` |
| Escape | Close the open screen; Hide on the open card; withdraw New Game on the start screen | `ScreenNav.cs:73`, `EventCardPresenter.cs:78,134`, `StartScreenPresenter.cs:73`, and the poll at `GameRoot.cs:366` for when nothing has focus |
| Left click on the map | Pick a hex | `MapPointer.cs:25`, `HandleClick` `:54-68`, wired at `GameRoot.cs:211-214` to `OnHexClicked` `:289-302` |

Free today: PageUp, PageDown, Home, End, `+` / `=`, `-`, the numpad, F, R, Z, X. Enter has no global meaning: it
only presses what has focus. End Turn is reached by Tab (and, after T38's M1 fix, by focus returning to it).

The Camera map is switched off while the start screen shows (`GameRoot.cs:249`, `cam.enabled = !shown`), so nothing
below fires under the start screen unless it reads keys outside that map.

## B1. The map from the keyboard

Four parts: zoom, choosing a hex, the info panel's buttons, and the minimap.

### B1a. Zoom keys

**Now.** `Zoom` has one binding, `<Mouse>/scroll/y` (`inputactions:1083`), read as wheel notches per frame
(`CameraController.cs:89-103`), and `Step` drops it while the pointer is over the UI (`:110`). A key bound to the same
action would read 1 every frame it is held: ten percent per frame, the whole range in under a second. It would also be
dropped whenever the mouse pointer happens to rest over a panel.

**Options.**

1. A second action, `ZoomKeys` (1D axis), in the Camera map. `Gather` turns it into notches per second
   (`ZoomKeysPerSecond`, about 6, so 6 km to 250 km takes about six seconds held) and passes it in a new
   `CameraInput.KeyZoom` field, which `Step` adds after the over-UI gate, as keyboard pan already is (plan amendment
   A4).
2. Discrete steps: a Button action whose each press is one wheel notch. Simpler, but slow at the far ends (35 presses
   from closest to farthest) and unlike pan and orbit, which move while held.
3. Bind keys to the existing `Zoom` action and scale in `Gather`. Mixes two units in one action and breaks the over-UI
   gate. Not recommended.

**Recommendation.** Option 1.

**Owner question (new keys).** Which keys zoom?
- (a) `+` / `=` and `-`, with the numpad's `+` and `-`. Recommended: the usual zoom keys, free today. Only unmodified
  presses count, so Ctrl/Cmd `+` stays the browser's own zoom.
- (b) PageUp / PageDown. Clashes with H3, which wants those keys to scroll an open screen; it would need a rule like
  "scroll when a screen is open, zoom when not".
- (c) R / F or Z / X, next to WASD and Q/E.

**Changes.**
- `unity/Assets/InputSystem_Actions.inputactions`: new `ZoomKeys` action and its composite in the Camera map (after
  `Zoom`, around `:1083`).
- `unity/Assets/Scripts/Map/CameraController.cs`: `CameraInput` (`:10-17`) gains `KeyZoom`; `:55` and `OnEnable`
  (`:59-66`) find the action; `Update` (`:77-78`) and `Gather` (`:89-103`) take it, scaled by `dt`; `Step` (`:110-114`)
  applies `input.Zoom` (gated) plus `input.KeyZoom` (not gated). The clamp to `[MinDistance, MaxDistance]` stays.

**Tests.**
- `CameraInputTests.TheCameraActionMapIsInTheProjectWideAsset` (`CameraInputTests.cs:25-33`): add `ZoomKeys` and its
  binding count; `Zoom` keeps its one binding (`:32`).
- New: key zoom still works with `OverUi = true` (beside `KeysStillPanAndOrbitWhileThePointerIsOverTheUi`, `:98`);
  holding it for one second moves the distance by `(1 - ZoomPerStep)^ZoomKeysPerSecond`; two half-second steps equal
  one full step (as `TwoHalfStepsEqualOneFullStep`, `:141`).
- `PromoHudTests.CameraKeysNeverMoveFocusOrPressAButton` (`:414`): the zoom keys move no focus and press nothing.
- Driver: `key plus` held, with `?camlog` to read the distance line (`GameRoot.cs:339-345`).

### B1b. Choosing a hex

**Now.** Only `MapPointer.HandleClick` raises `HexClicked` (`MapPointer.cs:54-68`). Until a hex is chosen the info
panel shows the hint (`InfoPanelPresenter.cs:175-183`, `strings.en.yaml:98`, `strings.he.yaml:93`) and its buttons are
hidden, so Tab never reaches them.

**Options.**

1. **A map stop with a centre mark.** A focusable, transparent element, `map-focus`, takes a place in the Tab order.
   While it has focus a mark shows at the centre of the view (the camera's look-at point, which `CameraRig` keeps at
   the centre of the area under the top bar). The arrows and WASD keep panning, as they do with any focus (B6), so the
   player pans the map under the mark. Enter or Space on the stop picks the hex under the mark, through the same
   `OnHexClicked` as a click (`GameRoot.cs:289-302`), so units, cover by a screen (#210) and the outline all behave as
   for a click. The mark is the stop's focus indicator.
2. **A hex cursor.** A cursor on a hex, moved one hex per press, with the camera following. The arrows would have to
   stop panning while the cursor is active, which reopens the B6 ruling. Four arrow keys map badly onto six hex
   neighbours, and after an orbit (Q/E) "up" no longer means north on screen. More code: cursor state, a drawn
   cursor, camera follow, neighbour stepping, a mode switch.
3. **A key that picks the centre hex with no Tab stop**, as the audit suggested (for example Enter when nothing has
   focus). Least code, but Enter is meant for buttons, and after T38's M1 fix something nearly always has focus. With
   no mark, the player can't see which hex Enter will pick.

**Recommendation.** Option 1. It reuses the camera keys players already have, leaves the arrows to the camera, and
needs no mode. It also fits what the GDD plans next: "Move units, issue orders" (turn structure, step 2) and "The
player can move units" (Movement and civilian actions). A move order can use the same stop: pick the unit with Enter,
pan, then press Enter on the destination. The mark shows only while the stop has keyboard focus, so a mouse player,
the promos and the `?shot` captures never see it.

Precision: hexes are 5 km (`hex_size_km`). At the 6 km hero zoom, keyboard pan moves 4.8 km a second
(`PanPerSecond` 0.8 × distance), so a short tap moves well under a hex. Farther out, the player zooms in first (B1a).

**Owner question (new visible mark).** What does the centre mark look like?
- (a) A small crosshair or ring at the centre, in the HUD's gold-light with a dark outline so it keeps 3:1 against
  sea, sand and forest (1.4.11). Recommended: drawn in the UI, no map change.
- (b) The hex under the centre outlined live as the player pans, with the same outline a click gives
  (`MapScene.Select`). Clearer about which hex Enter will pick, but it changes the map's look and needs a second
  outline style, since the picked hex already uses it.
- (c) Both: the mark, plus the live outline.

**Owner question (Tab order).** Where does the map stop sit?
- (a) After End Turn, before the info panel's buttons. Recommended: the HUD stays first, and one Tab after the stop
  reaches Build camp.
- (b) First in the order, before Cabinet.

**Owner question (hint text).** The hint "Click the map to inspect a hex" (`strings.en.yaml:98`, Hebrew `:93`) doesn't
mention the keyboard. Change it to something like "Click the map, or Tab to the map and press Enter, to inspect a
hex", or leave it as it is?

**Changes.**
- `unity/Assets/UI/Game.uxml`: a `map-focus` element after `top-bar` (`:8-77`), before `right-column` (`:79`), with
  `picking-mode="Ignore"` so clicks still fall through to the map (`UiPointer.IsOverUi`), `focusable` and `tabindex="0"`,
  and the mark inside it.
- `unity/Assets/UI/Statecraft.uss`: the mark, hidden unless the stop has focus.
- New `unity/Assets/Scripts/UI/MapFocusPresenter.cs`: places the mark at the look-at point, as `MapLabelsPresenter`
  places its pills, and on `NavigationSubmitEvent` calls a pick at the centre. One class, like `PointerBlur` and
  `NavigationGuard`.
- `unity/Assets/Scripts/Map/MapPointer.cs`: split `HandleClick` (`:54-68`) into the UI check (`:56-58`) and a
  `PickAt(Vector2 screen)` that does the raycast and raises `HexClicked`. The keyboard pick projects `rig.LookAt` to
  the screen and calls `PickAt`, which skips the UI check: the mark is not a pointer, and the info panel may sit over
  the centre on a narrow window. A raycast (rather than `rig.LookAt`'s own lon/lat) picks the hex the player sees
  under the mark on high ground.
- `unity/Assets/Scripts/UI/ScreenNav.cs`: `DisableCovered` (`:32-50`) disables `map-focus` exactly when it disables
  `info-panel` (`:42`): under the start screen, the open card, a wide screen, or any screen in the two-row HUD. The
  stop is then never in reach when a pick would be refused.
- `unity/Assets/Scripts/UI/GameRoot.cs`: create the presenter beside the pointer wiring (`:211-221`).
- Strings, if the owner changes the hint.

**Keys.** Arrows and WASD still pan, with the stop focused or not (`NavigationGuard` unchanged). Enter and Space on the
stop pick; elsewhere they press buttons as now. Escape on the stop does what it does with any focus: closes the open
screen, or nothing. Q/E orbit; the mark stays at the centre because orbit turns about the look-at point.

**Tests.**
- EditMode (new, beside `MapPressTests` / `MapActionUiTests`): with a built map and rig, focusing `map-focus` and
  sending `NavigationSubmitEvent` raises `HexClicked` for the hex under the look-at point; on a unit's hex the first
  submit selects the unit and the second shows the hex (as two clicks do, `GameRoot.cs:286-302`); with a wide screen
  open the stop is disabled and Tab skips it.
- `UiLayoutTests.TheTwoRowHudKeepsWhatAScreenCoversOutOfReach` and
  `StartScreenTests.WhileItShowsTheHudIsHiddenAndOnlyItsButtonsTakeFocus`: add `map-focus` to what must be out of
  reach.
- Tab order: from End Turn, Tab reaches `map-focus`; after a pick, the next Tab reaches `build-camp` (or
  `build-housing` or `info-defence`, whichever shows and is enabled).
- `PromoHudTests.CameraKeysNeverMoveFocusOrPressAButton`: add `map-focus` to the focused starting points, so a pan
  with the stop focused neither moves focus nor picks.
- The mark's contrast: a test in the style of `FocusChangesEachControlByAtLeastThreeToOne`, against the brightest and
  darkest biome colours (`MapBuilder.BiomeColours`).
- Driver: Tab to the stop, shot (mark visible); `key d` a few times, Enter, shot (info panel filled); Tab, shot
  (Build camp focused).

### B1c. The info panel's buttons

Nothing new is needed once B1b lands: `build-camp`, `build-housing` and `info-defence` (`Game.uxml:104-106`) are
ordinary Buttons, wired at `InfoPanelPresenter.cs:53-60`, and take Tab as soon as they show. Disabled ones (an action
the sim refuses, `:210`) are skipped by Tab, and the reason stays on screen under them (`:216`).

Two details for the B1b PR:
- After a pick, focus stays on the map stop, so the player can pan and pick again. One Tab reaches the buttons.
  Moving focus to the first button instead would cost a Shift+Tab to get back to the map.
- A keyboard player can't hover a 1948 locality's pill to see today's name (`GameRoot.cs:352-356`). The info panel
  already lists the hex's localities with today's name (`InfoPanelPresenter.LocalitiesText`, `:133-140`), so a pick
  covers it.

### B1d. The minimap

**Now.** A pointer press on the minimap moves the camera (`MinimapPresenter.cs:34,57-63`; `GameRoot.cs:221`). It
takes no focus.

**Options.**
1. Leave it out of the keyboard path. Everything it does (move the view to a place) can be done with zoom out, pan,
   zoom in, which B1a makes possible. 2.1.1 asks that the function be operable from the keyboard, not every control.
2. A focusable minimap: Enter jumps to the overview (the whole country); or the arrows move its view frame. The
   arrows are the camera's (B6), so only the first is cheap.
3. A key that jumps to the overview, with no minimap focus.

**Recommendation.** Option 1, with no code. Revisit if playtests show keyboard players getting lost.

**Owner question (only if wanted).** Should there also be a one-key "whole country" view (option 2 or 3)? If yes,
which key: Home clashes with H3 while a screen is open; `0` or `O` are free.

## H1. Focus follows into scroll views

**Now.** The Cabinet's cards (`cabinet-seats`, `Game.uxml:183`) and candidates (`cabinet-candidates`, `:160`) are
`ScrollView`s. Tab moves focus onto children below the viewport and the view doesn't move. The Budget's rows
(`budget-rows`, `:192`) have the same shape and will show it on short windows once T38's H2 fix keeps focus on a row.

**Options.**
1. One `FocusInEvent` callback at the UI root. It finds the target's nearest `ScrollView` ancestor and calls
   `ScrollTo(target)` when the target isn't fully inside the viewport. A new class like `PointerBlur`, created in
   `GameRoot` beside it (`GameRoot.cs:145`). It covers every scroll view, including ones added later.
2. One callback per `ScrollView`, in each presenter (`CabinetPresenter.cs:42`, `BudgetPresenter`). Same effect, more
   places to forget.

**Recommendation.** Option 1, keyboard only: a pointer press records itself (root `PointerDownEvent`, trickle-down)
and the focus that follows it is not scrolled. A click on a half-visible card then doesn't jolt the list, so mouse
play is unchanged. Programmatic focus (the card taking focus, T38's M1 returns) is scrolled, which is what's wanted.
If the target has no layout yet (a candidate list just rebuilt by `Select`), retry once on the next frame with
`schedule.Execute`.

No owner question: nothing changes for a mouse player, and a keyboard player sees the focused element where before
they saw nothing.

**Changes.**
- New `unity/Assets/Scripts/UI/FocusScroll.cs` (about 40 lines).
- `unity/Assets/Scripts/UI/GameRoot.cs:145-146`: `Own(new FocusScroll(root));`.

**Interactions.**
- T38's M7 gives the candidate list a minimum height. In the 609 px window the list had no height at all
  (`short-window/s05.png`), and no `ScrollTo` can show an element in a zero-height viewport. H1 for candidates is only
  complete with M7.
- M2 (candidates before cards in Tab order) isn't in T38's set or this plan. H1 makes the 50-name list visible as
  Shift+Tab walks it, but doesn't shorten the walk.

**Tests.**
- `CabinetScreenTests` (new case): open the Cabinet in a 1568x752 panel, focus the sixth card, wait two frames,
  assert its `worldBound` lies inside `cabinet-seats`' `contentViewport.worldBound`. The same for the last candidate
  after `Select`. The same for the last Budget row.
- A pointer-down followed by focus on a half-visible card leaves `scrollOffset` unchanged.
- Driver: repeat the audit's `05-cabinet-focus-offscreen`, `07-cabinet-candidate-offscreen` and
  `11a-candidate-third-offscreen` steps; each shot should now show the focus.

## H3. Scrolling text panels from the keyboard

**Now.** `why-rows` (`Game.uxml:256`, filled by `WhyPresenter.cs:36-57`), `event-scroll` (`:133`) and `defence-body`
(`:204`) hold labels. Nothing in them takes focus. PageDown does nothing; the arrows pan the camera.

**Options.**
1. **Page keys.** PageUp and PageDown scroll by one viewport less a line; Home and End go to the top and the bottom.
   The target: the `ScrollView` around the focused element if it is inside the open screen, else the open screen's
   main view (Cabinet `cabinet-seats`, Budget `budget-rows`, Defence `defence-body`, Why `why-rows`), else, with no
   screen open and the card open and not collapsed, `event-scroll`. Read by a poll in `GameRoot.Update` beside the
   Escape poll (`GameRoot.cs:366`), so it works with or without focus, and skipped while the start screen shows.
2. **Focusable text panels.** Give the three views `tabindex` so Tab stops on them, and scroll them with the arrows
   while they have focus. The arrows also pan the camera (the Camera map reads the keyboard directly), so the camera
   would have to stop panning while a text panel has focus. That reopens the B6 ruling, and L4 with it.
3. Both: option 1, plus Tab stops on the text panels with no arrow scrolling. A Tab stop would give H5 (a hidden DOM
   region) something to announce later, but on its own it adds a Tab press with nothing to do.

**Recommendation.** Option 1. The page keys are free, need no change to B6, and work the same on every screen and the
card. Option 3's Tab stops can come with H5 if the owner takes that up.

**Owner question (new keys).** PageUp / PageDown / Home / End scroll the open screen or the open card. Yes, or other
keys? If the owner picks PageUp / PageDown for zoom in B1a, they can't also do this.

**Changes.**
- New `unity/Assets/Scripts/UI/KeyScroll.cs`: `static bool Page(ScrollView view, int pages)` and `ToEnd(view, bool
  bottom)`, pure apart from the view, plus the target lookup. The table of main views lives here, keyed by
  `ScreenNav.Screens` (`ScreenNav.cs:13`).
- `unity/Assets/Scripts/UI/GameRoot.cs:359-371`: poll `pageUpKey`, `pageDownKey`, `homeKey`, `endKey` with no
  modifier, after the Escape line; the card comes from `card.Shown` and `card.Collapsed`
  (`EventCardPresenter.cs:119,150`), the open screen from `nav.Current` (`ScreenNav.cs:105`).
- No change to `NavigationGuard`.

**Tests.**
- `WhyScreenTests` (new case): a result with enough causes to overflow; `KeyScroll.Page(rows, 1)` moves
  `scrollOffset.y` by the viewport height less one line, End reaches `verticalScroller.highValue`, Home returns to 0.
- `DefenceScreenTests` and `DecisionCardTests` (a short panel, so `event-scroll` overflows): the same.
- The target lookup: with focus on a candidate, PageDown scrolls `cabinet-candidates`, not `cabinet-seats`; with the
  card collapsed and no screen, PageDown does nothing.
- `PromoHudTests.CameraKeysNeverMoveFocusOrPressAButton`: the page keys move no focus and press nothing.
- Driver: the audit's `25-why-overflow` / `26-why-keys-no-scroll` steps; after PageDown the list has moved and the map
  hasn't. Add `PageUp` and `End` to the driver's `KEYS`.

## H4. Larger text

**Now.** `StatecraftPanel.asset` (`:21-29`): scale mode 2 (scale with screen size), reference 1920x1080, match 0.5.
The UI is fitted to the canvas's device pixels, which browser zoom doesn't change. The smallest sizes in
`Statecraft.uss` are 13 px (photo credit `:224`, "members" `:380`), 14 px (security level `:105`, screen captions
`:114`, card stat names `:348`, party chips `:387`, the locality tooltip `:471`) and 15 px (many, including locality
pills `:466`). At 1568x752 the UI draws at 0.754, so 13 px is 9.8 CSS px.

**Why browser zoom can't simply be followed.** At 200% zoom the CSS viewport halves and the device pixel ratio
doubles. A UI fitted to the window (which this one must be, for the HUD tiers) cancels any zoom it reads: fitting to
the CSS size gives half the scale, and multiplying by the ratio gives it back. To honour zoom, the game would have to
compare the ratio with the one at load (`window.devicePixelRatio` through a `.jslib`). That can't tell a zoom set
before the page loaded (browsers remember zoom per site) from a high-density screen, and it jumps when the window
moves between monitors.

**Options.**
1. **A UI size setting** in the game: 100, 125, 150, 175 and 200%. At runtime `GameRoot` clones the panel settings and
   divides the reference resolution by the factor; the asset itself stays as it is (`UiFoundationTests.cs:15-16` pins
   it, and `UiTestKit` loads it directly). `PanelSettings.scale` might do the same more simply, but it is not certain
   that it applies in scale-with-screen-size mode on 6000.3; the first test settles which one to use. The HUD tiers
   follow on their own, since they key off the bar's logical width (`HudPresenter.cs:15-36,115-121`): at 150% in the
   1568x752 window the logical width is about 790, which is the three-row tier. The top bar's height feeds the
   camera's top inset (`TopBarInset.cs:30-32`, capped at half the screen by `CameraRig.MaxTopInset`).
2. **Follow browser zoom** as above. Not recommended alone.
3. **Raise the smallest sizes** in the stylesheet to 15 px or more (the audit's floor: 11 CSS px at the playtest
   size). Doesn't meet 1.4.4 by itself (that asks for 200%), but helps everyone at no runtime cost.

**Recommendation.** Option 3 first as a small PR, then option 1. 200% is what 1.4.4 asks for; whether every screen
still fits there is the main risk, and the PR must show each step on screenshots.

**Owner question (layout change).** Raise the 13 and 14 px text (credit, "members", security level, screen captions,
card stat names, party chips, locality tooltip) to 15 px? It changes the look of the top bar captions and the
Cabinet cards, and may push the top bar into the compact tier sooner.

**Owner question (new setting).** A UI size setting:
- Steps: 100 / 125 / 150 / 175 / 200% (recommended, reaches 1.4.4's 200%), or 100 / 125 / 150 (fewer layouts to
  check, doesn't reach 200%).
- Where the control lives. (a) A "Text size" button on the start screen next to Continue and New Game
  (`Game.uxml:276-279`), cycling the steps; the start screen opens every session, so the setting is always one step
  away. Recommended. (b) A sixth button in the top bar's nav (`Game.uxml:66-72`); the bar is already the tightest
  layout. (c) A small Settings screen in `ScreenNav`, which could later hold other options; the most work.
- Whether the map's settlement pills grow with it. They're in the same panel (`map-labels`, `Game.uxml:7`), so they
  will unless they're given their own counter-scale. Growing pills cover more map at 200%.

**Changes (option 3).** `unity/Assets/UI/Statecraft.uss` `:105,114,224,348,380,387,471`. Check the top bar tiers at
1568x752 and 1920x1080.

**Changes (option 1).**
- New `unity/Assets/Scripts/UI/UiScale.cs`: the steps, the factor to reference-resolution maths, and reading and
  writing the choice. There are no stored preferences today (no `PlayerPrefs` anywhere under
  `unity/Assets/Scripts`, and the audit's "the other preferences" doesn't exist). Use `PlayerPrefs`, which WebGL keeps
  in IndexedDB. It must survive New Game, which deletes the autosave (`SaveStore.DeleteAutosave`).
- `unity/Assets/Scripts/UI/GameRoot.cs`: apply the factor to a runtime clone of `document.panelSettings` (`:33`) at
  `Begin`, destroy the clone in `Shutdown`.
- `unity/Assets/UI/Game.uxml` and `StartScreenPresenter.cs` (or `HudPresenter`, per the owner's answer): the control.
- `strings.en.yaml` / `strings.he.yaml`: its label.
- A `?uiscale=150` flag (`UrlFlags`) so the driver and `tools/headed.py` can open a build at each step without
  clicking.

**Tests (option 1).**
- `UiScale` maths: each step gives the expected reference resolution; an unknown stored value falls back to 100%.
- With the clone at 200%, a 13 px label's `worldBound` height is twice what it is at 100% in the same panel size.
- `UiFoundationTests` still passes: the asset is untouched after a run.
- `UiLayoutTests` at each step in 1568x752 and 1920x1080: the top bar's tier, the event card's options all inside the
  card, the Cabinet's first card row inside the screen.
- Driver: `?uiscale=` 100, 150 and 200 at 1568x752, a shot of the HUD, the open card, the Cabinet and Defence at each.

## What this does not change

- No arrow key changes meaning. The B6 ruling stands: the arrows pan and never move focus.
- Enter keeps one meaning: press what has focus. The map stop is one more thing that can have focus.
- No change to the sim, `content/` or the goldens. B1b's pick goes through the same `MapAction` orders as a click.
- H5: the map stop and any Tab stops on text panels are the elements an H5 hidden DOM region would announce. If H5 goes
  ahead, the stop should have an accessible name (for example "Map. Enter picks the hex at the centre").
- M5, M6: no interaction.
- T38's files overlap with these PRs in `ScreenNav.cs` (M1's focus return on close; B1b adds one line to
  `DisableCovered`), `GameRoot.cs`, `Statecraft.uss` and `Game.uxml`. Whichever lands second rebases; none of the
  overlaps is in the same lines.

## The keyboard driver

`.superpowers/a11y-2026-10-14/tools/driver.py` is local and not committed. It imports `headed` from
`.worktrees/a11y-41/tools` and `run.sh` points at one session's scratchpad, so each PR's evidence run needs those two
paths changed first. Its `KEYS` table needs `PageUp`, `End`, `Equal` and the numpad keys, and a longer hold for the
zoom keys (`hold=` argument). Whether to commit a cleaned copy to `tools/` is a separate question; nothing in this
plan depends on it.

Unity EditMode tests and WebGL builds aren't in CI (`AGENTS.md`). Each PR runs them locally and pastes the result, and
takes the machine lock for its WebGL build (`tools/machine_lock.py`).

## PRs, in order

| # | PR | Finding | Size | Needs the owner's answer on |
|---|---|---|---|---|
| 1 | Focus follows into scroll views | H1 | Small: one new class (about 40 lines), one line in `GameRoot`, three tests | Nothing |
| 2 | Page keys scroll screens and the card | H3 | Small: one new class, a few lines in `GameRoot.Update`, four tests | Keys (H3) |
| 3 | Zoom keys | B1a | Small: one action, `CameraController` changes, three tests | Keys (B1a) |
| 4 | Map stop, centre mark, keyboard pick | B1b, B1c | Medium: UXML, USS, a new presenter, `MapPointer` split, `DisableCovered`, about six tests | Mark, Tab order, hint text |
| 5 | Smallest text to 15 px | H4 (option 3) | Small: stylesheet only, layout tests, screenshots | Font floor |
| 6 | UI size setting | H4 (option 1) | Medium to large: preferences, panel clone, the control, a URL flag, layout tests at each step | Steps, control, pills |

PR 1 can start at once. PRs 2 and 3 need only the key answers and are independent of each other. PR 4 should follow
PR 3, so that zoom is there when the pick ships and the player can get close enough to pick with care. PRs 5 and 6 are
independent of the map work. PR 1 should land after, or be rebased onto, T38's H2 and M7, since it is checked against
the Budget rows and the candidate list those fixes change.

B1 stays open until PR 4 lands. The minimap needs no PR unless the owner wants the overview key.
