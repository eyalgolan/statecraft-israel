# Accessibility audit, 14 October 2026

Issue #41. An audit only: nothing here is fixed yet. Each finding says what fails, where, how it was found, the WCAG 2.1
criterion it is measured against, and a proposed fix. The fixes go in later, small PRs.

## How it was tested

- **Build:** a WebGL build of the PR #461 branch made on 14 October before its final rebase, served locally and driven
  in a headed Chrome for Testing over the DevTools protocol, the way `tools/headed.py` does. Its UI is that of `main` at
  04bf7cb: the only later changes under `unity/` are one line of `WhyPresenter` (a group heading) and the tilt-shift
  shader. A build of 04bf7cb itself was queued behind the machine lock for the whole audit. Keys were sent as real key events (`Input.dispatchKeyEvent`); no
  mouse was used after the page loaded. `?navlog` logged which element received each Enter.
- **Window:** a 1568x752 CSS-pixel viewport (the playtest window) at device pixel ratio 1, unless a finding says
  otherwise. At that size the UI is drawn at 0.754 of its stylesheet size (PanelSettings: 1920x1080 reference, match 0.5),
  so a `font-size: 20px` label is about 15 CSS px on screen. WCAG's "large text" sizes are CSS px, so a stylesheet size
  has to be scaled before it is compared with them.
- **Play-through:** start screen, New Game, the top bar, Cabinet (select a seat, appoint a minister), Budget (move a
  line, submit), Defence (call-up and level), Why, two decision cards (choose an option; Hide, "Decision waiting",
  Escape), and six End Turns, all by keyboard.
- **Contrast:** computed from the colours in `unity/Assets/UI/Statecraft.uss`, with translucent colours composited in
  linear light as Unity does, and measured from screenshots where a photograph, the painting or the map sits behind the
  text. The measurement takes the most common colour in a box as the background and the median of the 3% of pixels
  furthest from it as the text, so anti-aliasing pulls measured ratios slightly below computed ones.
- **Colour vision:** colour pairs run through the Machado, Oliveira and Fernandes (2009) deuteranopia and protanopia
  matrices that `tools/art_compare.py` uses, then compared by contrast ratio and CIELAB distance (ΔE).
- **Evidence:** the screenshots are in `.superpowers/a11y-2026-10-14/` (local, not committed), named as below, with the
  driver and the scripts beside them.

Not covered: screen readers (see H5), touch, gamepad, and Hebrew UI strings (only the start screen's Hebrew is on
screen today).

## Summary

A full turn can be played without a mouse: answer an event, change a budget line, appoint a minister, end the turn.
It is slow and in places blind, and the map cannot be used at all.

| Rank | Finding | WCAG |
|---|---|---|
| Blocker | B1. The map's hexes, the map actions (camp, housing, defence) and the minimap need a mouse; zoom needs a wheel | 2.1.1 |
| High | H1. Tab moves focus to cards and candidates scrolled out of view; the list does not follow | 2.4.7, 2.4.11 |
| High | H2. Each Budget + or - press destroys the focused button; the next Tab starts again at Cabinet | 2.4.3, 2.1.1 |
| High | H3. Why, the event text and the Defence body cannot be scrolled from the keyboard | 2.1.1 |
| High | H4. Text cannot be enlarged: browser zoom has no effect and there is no text-size setting | 1.4.4 |
| High | H5. Nothing is exposed to assistive technology; refusals and results are visual only | 4.1.2, 4.1.3 |
| Medium | M1. Focus is dropped after the start screen, after choosing an option, and after Escape from a screen | 2.4.3 |
| Medium | M2. In the Cabinet the candidate list comes before the cards in Tab order, 50 names long | 2.4.3 |
| Medium | M3. Gold text on the focus fill is 3.6 to 3.7:1 (End Turn, Decision waiting, Illustration); ink on it is 4.2 to 4.6:1 | 1.4.3 |
| Medium | M4. The current escalation rung's turn text is 2.7:1 | 1.4.3 |
| Medium | M5. Coalition or opposition is shown by swatch colour only in the party chips | 1.4.1 |
| Medium | M6. A selected Cabinet seat differs from the others by 1.2:1 fill and 2.4:1 rim | 1.4.11 |
| Medium | M7. The Cabinet's "Ordered:" line and its candidate list are cut off at 1568x752 | 1.4.10 |
| Low | L1. Up and down changes are green and red that deuteranopia cannot tell apart (the sign carries it) | 1.4.1 (passes) |
| Low | L2. Escape withdraws New Game on the start screen but not in the top bar | consistency |
| Low | L3. The event card's own focus ring is a 1 px rim at 3.0:1 against its resting rim | 2.4.11 |
| Low | L4. Arrow keys never move focus, so every list is Tab-only | 2.1.1 (passes) |
| Low | L5. Start screen secondary lines fall to about 4.0:1 where the painting is brightest | 1.4.3 |

## Blocker

### B1. The map is mouse-only

- **What:** a hex can only be chosen by clicking it (`MapPointer` binds `<Mouse>/leftButton`,
  `unity/Assets/Scripts/Map/MapPointer.cs:25`; `GameRoot.cs:213-214` wires `HexClicked` to the info panel). Until a hex is
  chosen the info panel shows only the hint "Click the map to inspect a hex" (`strings.en.yaml:98`), so its Build camp,
  Build housing and Defence buttons (`Game.uxml:104-106`) are never in the Tab order. The minimap jumps the camera only on
  a pointer press (`MinimapPresenter.cs:34`). Zoom is bound to the mouse wheel only
  (`InputSystem_Actions.inputactions:1083`). Pan (WASD, arrows) and orbit (Q, E) do work from the keyboard.
- **How found:** Tab through the HUD at every stage of the play-through never reaches the info panel
  (`short-window/03-hud-tab-order-strip.png`); pan and orbit checked with D and E (`30a-before-keys.png`,
  `30b-after-d-and-e.png`).
- **Proposed fix:** a keyboard cursor on the map: a key (for example Enter on the map, or a "Select hex" control) that
  picks the hex at the screen centre, so pan plus that key reaches any hex; the info panel's buttons then take Tab like
  the rest. Bind zoom to keys too (`+`/`-` or PageUp/PageDown), and give the minimap a focusable control or drop it from
  the keyboard path, since pan covers it.

## High

### H1. Focus goes out of sight inside scroll views

- **What:** the Cabinet's person cards and its candidate list are `ScrollView`s (`Game.uxml:160,183`). Tab moves the
  focus onto cards and candidates below the visible area and the view does not scroll, so the focused element cannot be
  seen. At 1568x752 the first row holds five cards; the next Tab goes to the first card of the second row, out of view,
  and so on through the rest. After a seat is selected, Shift+Tab lands on the last of 50 candidates, also out of view,
  and Tab from the first candidate reaches the third one out of view.
- **How found:** `05a-cabinet-last-visible-card-focus.png` (the fifth card lit), `05-cabinet-focus-offscreen.png` (one
  Tab later: nothing lit), `07-cabinet-candidate-offscreen.png`, `11a-candidate-third-offscreen.png`. In a
  609 px tall window the candidate list had no visible height at all, so every candidate took focus unseen
  (`short-window/s05.png`).
- **Proposed fix:** one `FocusInEvent` callback on each `ScrollView` (or one at the root that finds the nearest
  `ScrollView` ancestor) calling `ScrollTo(target)`. An EditMode test: focus the last card, assert it lies inside the
  viewport.

### H2. The Budget screen drops focus on every step

- **What:** `BudgetPresenter.Change` calls `Render` (`BudgetPresenter.cs:87`), which clears and rebuilds every row
  (`BudgetPresenter.cs:107`). The + or - button that had focus is destroyed, nothing has focus, and the next Tab starts
  again at the Cabinet button. Moving one line by 15 points takes three rounds of about nine Tabs each.
- **How found:** `13-budget-plus-focus.png` (Immigration + lit), `14-budget-after-plus.png` (the share moved 15% to
  20%, nothing lit), `14a-budget-second-enter-nothing.png` (a second Enter did nothing; `?navlog` shows no submit),
  `15-budget-tab-restarts.png` (Cabinet lit).
- **Proposed fix:** build the rows once and update their labels in place, as `CabinetPresenter` already does for its cards
  ("updates its cards in place, so keyboard focus survives", `CabinetPresenter.cs:20`). A test as
  `AHiddenScreenDoesNoWorkAndAnOpenOneKeepsFocus` (`PromoScreensTests.cs:521`): focus a + button, submit it, assert it
  still has focus.

### H3. Text-only panels cannot be scrolled from the keyboard

- **What:** the Why screen (`why-rows`), the event text (`event-scroll`, which scrolls on short windows) and the Defence
  body (`defence-body`) hold labels only, so nothing in them takes focus and Tab cannot scroll them. The arrow keys pan
  the camera and `NavigationGuard` (`NavigationGuard.cs:15-20`) drops directional navigation by design; PageDown does
  nothing. After a turn with several causes the Why list runs past the bottom of the screen.
- **How found:** `25-why-overflow.png` (scroll bar shown, list cut off), `26-why-keys-no-scroll.png` (after PageDown and
  ArrowDown: unchanged, the map panned instead).
- **Proposed fix:** while a screen is open, PageUp, PageDown, Home and End scroll its `ScrollView` (handled in
  `ScreenNav`, which knows which screen is open), and the same keys scroll the open card's text. Leave the arrows to the
  camera, as ruled in B6.

### H4. Text cannot be made larger

- **What:** the UI scales with the canvas's pixel size (`StatecraftPanel.asset`: scale mode 2, reference 1920x1080,
  match 0.5). Browser zoom changes the CSS size and the device pixel ratio together, so the canvas keeps the same pixel
  size and the UI does not grow. There is no in-game text or UI size setting. The smallest text is 13 px in the
  stylesheet (photo credit, `Statecraft.uss:224`; "members" under the house, `:380`), 14 px (screen button captions
  `:114`, security level `:105`, card stat names `:348`, party chips `:387`) and 15 px; at 1568x752 those are 9.8, 10.6
  and 11.3 CSS px.
- **How found:** the HUD at a 784x376 viewport with device pixel ratio 2 (what 200% browser zoom gives) is pixel for
  pixel the HUD at 1568x752 and ratio 1: the two top bars differ by 0.0 in mean pixel value (`31b-zoom-200.png`,
  `31a-zoom-100.png`).
- **Proposed fix:** a UI size setting (100%, 125%, 150%) that multiplies `PanelSettings.scale` and is saved with the
  other preferences; the layout tiers in `HudPresenter` already react to the bar's width. Separately, raise the floor to
  15 px in the stylesheet so the smallest text is at least 11 CSS px at the playtest size.

### H5. No assistive-technology output

- **What:** the game is a WebGL canvas. UI Toolkit on WebGL exposes no accessibility tree, so a screen reader hears
  nothing: not the focused button, not the event text, not the toast that says why an order was refused ("Not done: Not
  enough attention", `14-budget-refused-toast.png`), not the result of End Turn.
- **How found:** structural; no screen reader was run.
- **Proposed fix:** a first step that fits the engine: a hidden DOM region beside the canvas, filled through a `.jslib`
  with the focused element's text on every focus change and with each toast as `aria-live="polite"`. That gives a
  screen-reader user the focus name and the refusals; the event text could follow when the card opens. A design call
  for the owner, as it is a new web-only path.

## Medium

### M1. Focus is dropped in three places

- **What:**
  - After the start screen closes, `StartScreenPresenter.Close` blurs the focused button (`StartScreenPresenter.cs:104-105`)
    and, with no event open, nothing takes focus (`02-map-no-focus.png`).
  - After an event option is chosen, the card hides (`EventCardPresenter.cs:161-162`) and the option that had focus goes
    with it (`23-after-choice-no-focus.png`).
  - After Escape closes a screen whose content had focus, `ScreenNav.Open(null)` (`ScreenNav.cs:113`) hides the screen
    and the focus goes with it (`08-after-escape-no-focus.png`).
  In each case the next Tab starts again at Cabinet (`09-tab-restarts-at-cabinet.png`, `24-tab-restarts.png`), so a
  keyboard player needs six Tabs to get back to End Turn.
- **Proposed fix:** closing a screen gives focus back to its top-bar button; choosing an option gives it to the next open
  card, or to End Turn when none is left; closing the start screen gives it to End Turn (or the card, which already
  takes it). Pin each with an EditMode test; the issue also asks for one pinning End Turn's place in the order.

### M2. The candidate list is before the cards, and long

- **What:** in `Game.uxml` the side column (the Prime Minister's card, his other offices, the candidate list) comes
  before the main column (the cards) (`Game.uxml:153-164`). Selecting a seat with Enter leaves focus on the card; the
  candidates for it are behind, reached only by Shift+Tab from the far end of a list of every person in `people.yaml`
  (50), or by Tab from the start of the screen past the Prime Minister's card.
- **How found:** `06-cabinet-seat-selected.png`, `10-candidate-first.png`.
- **Proposed fix:** on selecting a seat with the keyboard, move focus to the first candidate that can be appointed
  (refused ones are disabled and skipped by Tab already); Escape in the list returns to the seat.

### M3. Text on the keyboard focus fill is below 4.5:1

- **What:** the focus fill `--focus` (`Statecraft.uss:16,63`) was chosen for 3:1 against the resting fill, and it does
  that (3.31:1). Text on it is weaker:
  - gold-light text (End Turn `:159`, Decision waiting `:178`, the Illustration switch `:220-221`): 3.73:1 computed;
    measured 3.57:1 on Decision waiting and 3.73:1 on Illustration and End Turn. End Turn is 22 px SemiBold, which is
    "large" only at 1080p and up; at 1568x752 it is 16.6 CSS px and needs 4.5:1. Decision waiting (20 px) and
    Illustration (17 px) are normal text at any size.
  - ink text (every other button, the event options): 4.60:1 computed, 4.21:1 measured on an option.
- **How found:** `16-endturn-focus.png`, `20-decision-waiting-focus.png`, `19b-illustration-focus.png`,
  `18-option-focus.png`; ratios from the contrast script.
- **Proposed fix:** keep `--focus` (it is what gives the 3:1 fill change; a darker fill such as rgb(104, 85, 50) would
  give ink 5.66:1 but drop the fill change to 2.69:1) and use a lighter label on it instead: rgb(250, 246, 236) on
  `--focus` is 5.39:1, for every focused button including the gold ones. Extend
  `PromoHudTests.FocusChangesEachControlByAtLeastThreeToOne` to assert 4.5:1 for the label too.

### M4. The current escalation rung's turn text

- **What:** on the lit rung (`.rung-current`, `Statecraft.uss:414`) the muted turn label ("turns of a week", `:413`,
  15 px) is 3.28:1 computed and 2.68:1 measured. The rung's name passes (4.73:1 measured).
- **How found:** `27-defence.png`, rung 5.
- **Proposed fix:** `.rung-current .rung-turn { color: var(--ink); }`.

### M5. Coalition membership by colour only in the party chips

- **What:** each chip under the house (`CabinetPresenter.Chip`, `CabinetPresenter.cs:241-252`) is a swatch and "Mapai
  10"; whether a party is in the coalition is only the swatch's blue or grey (1.72:1 against each other, `Statecraft.uss:388`).
  The hemicycle is backed by the "Coalition: 30 of 37 seats" and "Opposition: 7 seats" lines, but which party is which
  is not.
- **How found:** `04-cabinet-pm-focus.png`; colour pairs below.
- **Proposed fix:** two labelled groups of chips, "Coalition" and "Opposition", or a word after each name.

### M6. A selected Cabinet seat is hard to tell from the rest

- **What:** `.seat-selected` (`Statecraft.uss:350`) raises the fill from `--navy-deep` to `--navy-raised` (1.17:1) and
  the rim from 60% gold to gold-light (2.43:1). The "Appoint to Minister of Finance" heading says which seat is selected,
  but the card itself does not stand out by 3:1, and once focus moves on it is the only mark.
- **How found:** `06-cabinet-seat-selected.png`, then `10-candidate-first.png`.
- **Proposed fix:** a second, non-colour mark on the selected card: a thicker rim (2 px) or a small "Selected" tag on the
  plate.

### M7. Cabinet text cut off at the playtest size

- **What:** at 1568x752 the side column's "Ordered: Aharon Zisling to Minister of Fi…" line is clipped at the column's
  edge (`cabinet-status`, `Game.uxml:162`, no `white-space: normal`), and the candidate list shows about one and a half
  rows.
- **How found:** `12-appointed-status-clipped.png`.
- **Proposed fix:** let `#cabinet-status` wrap, and give the candidate list a minimum height (or let the side column
  scroll as a whole).

## Low

### L1. Up and down changes under colour-vision deficiency

The top bar's changes use `--up` and `--down` (`Statecraft.uss:14-15,44-45`, set in `HudPresenter.cs:140-141`). Under
deuteranopia the pair is ΔE 8 apart, which is close to indistinguishable. This is not a 1.4.1 failure, because every
change carries its sign in the text ("+IL 5.0m", "-1.7%"; `HudTests.ChangesShowSignsAndZeroDeltasAreEmpty`). Each colour
is above 4.5:1 on the bar (8.66 and 6.02). Proposed fix, if wanted: a small up or down mark next to the number.

### L2. Escape and the top bar's New Game

On the start screen Escape withdraws an armed New Game (`StartScreenPresenter.Escape`). In the top bar it does not: the
toast "Press New Game again to abandon this game and start over" stays after Escape (`28-newgame-escape.png`). Proposed
fix: `GameRoot`'s Escape poll (`GameRoot.cs:366`) also calls `NewGamePresenter.Cancel` when it is armed.

### L3. The event card's focus ring

When a new event opens, focus goes to the card itself and its 1 px rim turns gold-light (`Statecraft.uss:187`), 3.01:1
against the resting rim: at the threshold, and thin on a 1040 px card (`17-event-card-focus.png`). Proposed fix: 2 px
when focused.

### L4. Arrow keys never move focus

By design (B6, `NavigationGuard.cs`) the arrows pan the camera and only Tab moves focus. That meets 2.1.1, but a list such
as the Budget's 16 buttons or 50 candidates is Tab-only. Proposed fix: while a screen is open and the pointer is not
panning, Up and Down move between rows. A design call; it touches the B6 ruling.

### L5. Start screen secondary lines over the painting

The English lines and the source line (`--ink-dim` on the 88% panel, `Statecraft.uss:504,519-520`) measure about 7.3:1
over the dark audience and fall to about 4.0 to 4.4:1 just above the source line where the hats are lit
(`01-start-focus.png`). Proposed fix: panel alpha 0.94, or `--ink` for the source line.

## Contrast table

Computed from `Statecraft.uss`. "Needs" is 4.5:1 for normal text and 3:1 for large text at the 1920x1080 reference size;
at the playtest size only text of 32 px (24 CSS px) or 25 px bold (18.66 CSS px) counts as large.

| Pair | Ratio | Needs | Result |
|---|---|---|---|
| Body text (ink) on the top bar and toast (`--navy-solid`) | 14.29:1 | 4.5:1 | pass |
| Body text on a screen (rgb 11, 18, 32) | 14.80:1 | 4.5:1 | pass |
| Body text on a panel over the brightest map (`--navy`, 97%) | 9.52:1 | 4.5:1 | pass |
| Button text on `--navy-deep` | 15.23:1 | 4.5:1 | pass |
| Hovered button text on `--navy-raised` | 13.01:1 | 4.5:1 | pass |
| Focused button text (ink on `--focus`) | 4.60:1 | 4.5:1 | pass, thin (measured 4.21:1) |
| Focused gold text (gold-light on `--focus`) | 3.73:1 | 4.5:1 | **fail** (M3) |
| Disabled button (ink-dim on `--navy-deep`) | 7.77:1 | exempt | pass |
| Muted text on the top bar | 7.28:1 | 4.5:1 | pass |
| Turn and date on the date badge (`--navy-raised`) | 6.63:1 | 4.5:1 | pass |
| Screen button captions (14 px) | 7.28:1 | 4.5:1 | pass |
| Open screen caption (gold-light) | 11.59:1 | 4.5:1 | pass |
| End Turn, resting | 12.36:1 | 4.5:1 | pass |
| Change up / change down on the top bar | 8.66 / 6.02:1 | 4.5:1 | pass |
| DRAFT (`--down`) on the card | 6.08:1 | 4.5:1 | pass |
| Screen title (gold-light) on a screen | 12.00:1 | 3:1 | pass |
| Section titles and Why groups (gold) | 7.61:1 | 4.5:1 | pass |
| Card office, party, stat names on a card | 7.77 to 7.83:1 | 4.5:1 | pass |
| Party chip and "members" labels on the house box | 7.78:1 | 4.5:1 | pass |
| Rung name / rung turn on a plain rung | 9.85 / 5.02:1 | 4.5:1 | pass |
| Rung turn on the current rung | 3.28:1 (measured 2.68) | 4.5:1 | **fail** (M4) |
| Hebrew (ink, Frank Ruhl Libre 34 px) on the start panel | 15.35:1 | 3:1 | pass |
| Photo credit on its strip (measured) | 7.11:1 | 4.5:1 | pass |
| Map labels on their pills (measured, over sea and land) | 9.66 to 9.71:1 | 4.5:1 | pass |
| Locality labels (72% pill) over the brightest ground | 2.33:1 | 4.5:1 | computed only; not seen at the tested zoom |

Non-text contrast (1.4.11, and the focus indicator):

| Pair | Ratio | Result |
|---|---|---|
| Focus fill against the resting fill | 3.31:1 | pass |
| Focus rim (gold-light) against the resting rim | 3.02:1 | pass, at the line |
| Button rim against its fill | 4.09:1 | pass |
| Open screen button rim against a closed one | 3.02:1 | pass, at the line |
| Selected seat fill / rim against an unselected seat | 1.17 / 2.43:1 | **fail** (M6) |
| Current rung edge against a plain rung | 7.99:1 | pass |
| Ordered rung edge (ink) against a plain rung | 9.85:1 | pass |

Colour pairs under colour-vision deficiency (ratio / ΔE):

| Pair | Normal | Deuteranopia | Protanopia |
|---|---|---|---|
| Change up against change down | 1.44:1 / 76 | 1.27:1 / 8 | 1.79:1 / 25 |
| Coalition blue against opposition grey | 1.72:1 / 47 | 1.82:1 / 50 | 1.57:1 / 45 |
| Competence bar against loyalty bar | 1.18:1 / 54 | 1.20:1 / 48 | 1.14:1 / 50 |

The bars are always labelled ("Competence", "Loyalty"), so their colours carry no meaning of their own.

## Focus order per screen (as tested)

- **Start screen (no save):** New Game only, focused on open; Tab stays on it. Enter starts the game.
- **Top bar:** Cabinet, Budget, Defence, Why?, New Game, End Turn, then round again (the info panel and the minimap
  column take no focus until a hex is chosen by mouse).
  - Since HUD round 2 (#584, after this audit): End Turn is followed by the minimap's three buttons (whole map, settlement
    names, region borders), then round again; the minimap's picture still takes no focus (B1d). Under the open decision
    card the minimap column is disabled with the info panel, so its buttons leave the order then.
- **Cabinet:** the top bar first, then the Prime Minister's card, his other office ("Minister of Defense"), the candidate
  list when a seat is selected, then the cards row by row (H1, M2).
- **Budget:** the top bar, then each line's - and +, then Reset and Submit (Submit is disabled until the draft changes).
- **Defence:** the top bar, then Lower the level, Raise the level (disabled at level 5), -5%, +5%.
- **Why:** the top bar only; the list takes no focus (H3).
- **Decision card:** the card itself on open, then each option, Hide, the Illustration switch when the event has a clip,
  then the top bar. Hide puts focus on Decision waiting; Enter there reopens the card with focus on it; Escape is Hide.

Escape closes the open screen; on the open card it is Hide; on the start screen it withdraws New Game.

## What already works

- Every button shows keyboard focus with a fill and rim change of at least 3:1, and a mouse click leaves no focus fill
  behind (`PointerBlur`; `PromoHudTests.FocusAndOpenAreDistinctAndAPointerClickClearsFocus`,
  `FocusChangesEachControlByAtLeastThreeToOne`, `PromoScreensTests.ACardsFocusIsVisibleAndAClickLeavesItUnlit`).
- Hidden panels are disabled as well as hidden, so Tab never reaches a control under the start screen, a screen or the
  card (#400; `ScreenNav.DisableCovered`, `StartScreenTests.WhileItShowsTheHudIsHiddenAndOnlyItsButtonsTakeFocus`,
  `UiLayoutTests.TheTwoRowHudKeepsWhatAScreenCoversOutOfReach`).
- A new event puts focus on the card, not on an option, and takes it back when a covering screen closes (#441;
  `DecisionCardTests`). In play this also stopped a second Enter meant for End Turn from ending the turn: it reached the
  new card instead (`?navlog`: `ui submit target=event-card`).
- Hide, Decision waiting and Escape on the card all work from the keyboard, and Hide by keyboard moves focus to
  Decision waiting (`20-decision-waiting-focus.png`, `21-reopened.png`, `22-escape-hides.png`).
- End Turn keeps focus across turns with no event, so repeated Enter plays turns.
- The camera keys never move focus or press a button (`NavigationGuard`; `PromoHudTests.CameraKeysNeverMoveFocusOrPressAButton`),
  and they pan and orbit while focus is in the UI.
- New Game needs two presses, so one stray Enter does not end the game.
- Body text is 9.5:1 to 15:1 throughout; muted text is 5:1 or more on every plain background; the Hebrew on the start
  screen is set in a Hebrew face at 15:1; bars and seat colours meet 3:1 (`PromoScreensTests.BarsAndPartyColoursMeetThreeToOne`).
- Every change in the top bar carries its sign in the text, not only its colour.

## Screenshots

In `.superpowers/a11y-2026-10-14/` (1568x752 unless noted), with the driver and the contrast, colour-vision and
measurement scripts in `tools/`. `short-window/` holds the first pass in a 1568x609 viewport at device pixel ratio 2,
where the candidate list had no height.
