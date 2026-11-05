# PR #2621 captures: operation card requirement and refusal lines (EN + HE)

In-play Unity captures (Map.unity in Play mode, `GameRoot.Begin`, `-nostart`, 1920x1080) for
[#2621](https://github.com/eyalgolan/statecraft-israel/pull/2621) (Fixes #2543). Made by an agent with a throwaway EditMode rig; nothing here is game code.

| | SHA |
|---|---|
| main (before) | `06f4ae6b82ec517c4858bbf6fb551db25c19528e` (origin/main) |
| PR head | `072487aec55ee06f4ab535e8f34cd3f7d1534e59` **plus a local compile fix** (below) |

## The PR head does not compile in Unity

At `072487aec`, `unity/Assets/Scripts/UI/EventCardPresenter.cs` fails with CS0136 at lines 304 and 310:
`foreach (var row in requirement.Required)` / `foreach (var row in requirement.Missing)` reuse the name `row`, which
`RenderCard` already declares at line 262 (`var row = video != null && ...`, present on the PR's base `7b6acd9e4`).
The head captures were therefore taken on the head with only those loop variables renamed `row` -> `req`
(`compile-fix-used-locally.diff`; not pushed anywhere). With that rename, Statecraft.Game and the EditMode tests compile with 0 errors.

## Setup per shot (all at 1920x1080, ruleset gdd-0.1)

The fixture of `EventCardTests.AnOperationCardExplainsAndEnforcesItsUnitPlacementInBothLanguages`, applied to the live session:
`State.Date = operation_horev.From` (15 Dec 1948), `PendingEvents = [operation_horev]`, then SimBridge's `Changed` is raised so
the HUD re-reads the state, and the card is reopened.

- **unmet**: Golani Brigade left at its start hex (11,11), outside Horev's regions (Negev / Southern Coast).
- **met**: Golani moved to (-17,35), the first hex of Southern Coast (`RequiresUnitRegions[0]`, as the test does).
- **refused**: unmet setup; `launch` queued through `SimBridge.Queue` (the button itself is disabled), every unit waiting for orders skipped, End turn pressed. The toast shows the sim's refusal; the turn report opens over the card.
- **main (before)**: the unmet setup on origin/main (main has no requirement lines; Launch is enabled).

## Files

- `main-unmet-{en,he}.png` (+ `-zoom`): main (before).
- `head-unmet-{en,he}.png` (+ `-zoom`): requirement line (muted), red missing line, Launch disabled.
- `head-met-{en,he}.png` (+ `-zoom`): requirement line only, Launch enabled.
- `head-refused-{en,he}.png` (+ `-zoom` of the toast): "Not done: The operation's required units are not in place" / "לא בוצע: היחידות הנדרשות למבצע אינן במקומן".
- `sbs-main-vs-head-unmet-{en,he}.png`: card zooms, main above, head below.
- `sbs-head-unmet-vs-met-{en,he}.png`: card zooms on the head, unmet above, met below.
- `compile-fix-used-locally.diff`: the rename used for the head captures.

## Seen in the captures

- EN unmet: "Golani Brigade must be in Negev / Southern Coast." then, in red, "Move Golani Brigade to Negev / Southern Coast before this operation can begin." Launch greyed out; Hold enabled.
- HE unmet: "נדרשת הצבת חטיבת גולני בהנגב / מישור החוף הדרומי." and in red "יש להעביר את חטיבת גולני אל הנגב / מישור החוף הדרומי לפני שאפשר לפתוח במבצע." Right-aligned, RTL order, legible.
- Hebrew wording: `ב{1}` plus a region name that starts with ה gives "בהנגב" (should read "בנגב"), and the second region after the slash gets no ב. The strings are agent drafts awaiting the native read (#564).
- After the refused turn, Horev is no longer pending and was not fired (postponed), and the date moves to 15 Jan 1949.
