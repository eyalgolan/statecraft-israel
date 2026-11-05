# #2580 hex panel refusals: before / after

These are captures for PR #2580 (Train and corridor Build refusals, and the localized "depopulated" reason).

- BEFORE: origin/main `ae4bfdaab5e92815faed8515ecc2604aed52c8ea` (folder `main/`).
- AFTER: `244e9526f15618d3b086a7cd86e2ed369a12eabe` (folder `head/`). This is a local merge commit of main `ae4bfdaab` and the #2580 head `d3b1fb6e06d3e99810edd3f08d53d252ada1c08a`. It was made with `git merge-tree --write-tree`, which merged cleanly to tree `55e92bb43`, then `git commit-tree`. It was never pushed.
- Captured on 5 Nov 2026, 13:59-14:01 IST (before) and 13:57-13:59 IST (after), with Unity 6000.3.25f1. There was one run per SHA, in the same worktree, with the same rig. The Hebrew test ran under `he_IL`, and the English test under `en_US`.

## Rig

The rig is a throwaway EditMode test (never committed), copied from the #2687 capture rig. It opens Map.unity, enters Play mode, and calls `GameRoot.Begin` with `UiDirection` on, at 1920x1080 and with the same camera on both SHAs. Before any order is committed, it edits the live session's turn-start state the way the PR's tests edit a new game. The game's own `InfoPanelPresenter.Show` then shows the hex. The orders go through `SimBridge.Queue`.

## Strips (`sbs/`, each in `-en` and `-he`)

BEFORE is on the left and AFTER on the right. The info panel is cropped to the same box on both sides. The changed element is outlined in red, and its tag gives the state read from the element itself (`enabledSelf`, or its text).

| strip | state | before | after |
|---|---|---|---|
| `sbs-a-train-*` | (a) Tel Aviv with 2 idle civilians (`ATrainPressCannotTakeTheCivilianALaterReassignmentNeeds` setup); Train halutz (1), then ReassignCraftsmen (1), queued | Train halutz **OFF** | Train halutz **OFF** (no visible change: see below) |
| `sbs-a2-recruit-no-arms-*` | (a2) Tel Aviv at the start with Arms = 0 (`RecruitTrainingIsDisabledWithNoArms`) | Train recruits **ON** | Train recruits **OFF** (artillery and armoured cars also OFF) |
| `sbs-b-corridor-build-*` | (b) Jaffa depopulated and moved onto Burma Road segment 1's first hex, plus an idle halutz group (`ADepopulatedPlaceCrossingDisablesCorridorBuild`) | Build road **ON** | Build road **OFF** (segment 2's Build is OFF too) |
| `sbs-c-depopulated-reason-*` | (c) Jaffa depopulated, with the ma'abarot policy on (`TheDepopulatedReasonShowsInThePanelsLanguage`) | EN: "Nothing can be built on a depopulated place"; HE: the same English text (the sim's fallback) | EN: unchanged; HE: "אי אפשר לבנות במקום מרוקן מתושביו" |

Notes:

- **(a) shows no difference against main.** On main the Train halutz button is already OFF in this state, because `Training.Available` counts both queued orders, so 2 - 2 = 0 free. The test's red-first prediction is against an earlier PR head (`f28230b07`), not against main. The strip is included as evidence, and (a2) shows the Train change that is visible against main.
- **(b):** the corridor box shows no refusal text on either SHA. The PR only disables the button.
- The `main/` and `head/` folders hold the full 1920x1080 frames, with the same red outline.
