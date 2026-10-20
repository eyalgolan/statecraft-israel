# Independent draft-prototype review

Tool: separate-context Codex / Sol 6.1 reviewer. Read-only code review; it did not run Unity.

Initial findings, corrected before captures:

- Restore the exact shared URP shadow distance after a temporary bake/capture.
- Restore scene ambient lighting and sun after deleting the preview.
- Destroy generated volume components as well as their profile.
- Refuse scene contamination; use an empty scene for comparisons.
- Constrain a perspective card's use to the captured view and a narrow distance range.

Corrective review: no remaining blocking code correctness findings for this bounded draft.
The fixture cleanup test exercises a real bake and resource teardown. Runtime code has no Editor dependency.
Before/after pairs keep the fixture/camera fixed; close view automatically restores the original.

The reviewer checked that the source FBX, material, texture, their three metadata files, `ASSETS.yaml` and
`LICENSING.md` were byte-identical to the base snapshot. The FBX hash matches the topology report.

This review establishes neither player-build validation nor visual/performance acceptance.

## Independent image review

**Rejected.** At the exact baked gameplay camera, the lower front wall and its two windows disappear behind
a sharp horizontal cut; the original shadow remains, making the house look suspended. This is consistent with
the flat card intersecting terrain. The close pair is pixel-identical, supporting fallback only.

The reviewer judged this useful as a negative-result draft with matched captures, no rollout and no quality or
frame-benefit claim. Reporting findings were corrected: remove the unsupported wall/window-preservation claim,
mark the capture complete and candidate rejected, and identify only the card when logging additional texture cost.
The reviewer performed no Unity runs or edits.
