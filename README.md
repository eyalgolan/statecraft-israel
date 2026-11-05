# PR #2621 operation requirements: in-play captures v2 (no local fix)

Recapture of the Operation Horev card for PR #2621 after the author's fixes. Supersedes `captures/2621-op-requirements` (v1, left untouched).

- PR head: `ed5ec07e68ad1e948efda5ac46c48a42129ac188` (checked with `gh pr view 2621 --json headRefOid` before the run)
- main (before): `b5b34e2140ba6bdfa3ba15090ae7b8d2393b1e07` (origin/main at shoot time; main had moved from v1's 06f4ae6b8, so main was re-shot)
- **No local fix.** The head compiled as-is (csc check: Statecraft.Game 0 errors, Statecraft.Tests.EditMode 0 errors), and the EditMode run passed 6/6 (main: 2/2).
- Unity 6000.3.25f1, EditMode batch run of a throwaway in-play test (`GameRoot.Begin`, 1920x1080), through the gate slots and `tools/machine_lock.py --shared`. Nothing was committed to the PR.

## Setup
- GDD ruleset; date set to 15 Dec 1948 with only `operation_horev` pending.
- **unmet**: Golani Brigade at its start hex (11,11), region valleys (outside Horev's regions).
- **met**: Golani Brigade moved to (-17,35), southern_coast.
- **refused**: 'Launch' queued with Golani out of place, units skipped, End turn pressed. The refusal toast is at the top left, with the turn report open.
- EN and HE UI for each.

## What changed vs v1
- v1 (head 072487aec) needed a local CS0136 compile fix (a `row` loop variable). v2 uses none: the loops are now `need` (EventCardPresenter.cs:304/:310).
- HE requirement line: v1 showed "בהנגב". v2 shows **"נדרשת הצבת חטיבת גולני באזור הנגב / מישור החוף הדרומי."**
- HE missing line (red): "יש להעביר את חטיבת גולני אל הנגב / מישור החוף הדרומי לפני שאפשר לפתוח במבצע."
- HE refusal toast: "לא בוצע: היחידות הנדרשות למבצע אינן במקומן"
- EN (unchanged): "Golani Brigade must be in Negev / Southern Coast." / "Move Golani Brigade to Negev / Southern Coast before this operation can begin." / toast "Not done: The operation's required units are not in place"
- The HE Hide button reads "הסתרה" on main and "הסתר" on the head. This comes from main changing `event.hide` after the PR's base, not from the PR.

## Files
- `sbs-main-vs-head-unmet-{en,he}.png`: main (top) vs head (bottom), Golani out of place (card zoom x2).
- `sbs-head-unmet-vs-met-{en,he}.png`: head, Golani out of place (top) vs in place (bottom).
- `head-{unmet,met,refused}-{en,he}.png`: full 1920x1080 frames for the head; `main-unmet-{en,he}.png`: full frames for main.
- `*-zoom.png`: x2 crops of the card text/options (for refused: the toast).
