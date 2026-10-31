# C06 point-fire guide and provisional fallback candidate

Refs [#1866](https://github.com/eyalgolan/statecraft-israel/issues/1866), [#1867](https://github.com/eyalgolan/statecraft-israel/issues/1867), [#2007](https://github.com/eyalgolan/statecraft-israel/issues/2007).

Tal Heuberger’s latest direction supports the local C06 + K98k experiment as a guide and proposed provisional fallback where no better existing clip exists. Provisional in-game use still requires the project owner’s decision; Tal’s art preference is not project-owner acceptance. This document does not install or enable it. Compare the existing C06 fire/shoot clips before choosing a fallback; do not blanket-replace other characters' clips.

The saved review uses the original 67-bone binding, rest mesh and weights. It has 97 frames at 24 fps, with carrying endpoints, supported point-fire and two recoil beats. Local sampled static checks passed 54/54; full-cycle relative checks passed 872/873. These relative thresholds are not anatomy, nonpenetration, art or runtime acceptance.

Known limits:

- A cheek-stock weld and strict sighted aim were not achieved; this is approximate point-fire.
- Frame 17 has 280 right-hand/rifle triangle-intersection pairs against 279 in the baseline. Without pair identity, location and depth, the severity of that difference is inconclusive. Baseline intersections persist; contact is not certified clean.
- Elbow steps around 18–20 and recovery 73–74 are larger than the baseline. Consecutive rendered views show no clear bend flip or teleport, but independent normal/slow playback review remains required. No smoothing was applied without demonstrated visual need.
- Unity import, avatar/clip binding, runtime dispatch, transitions, fixed weapon pairing, supported player builds and matched in-game captures remain untested. Do not describe this as production-quality or a validated replacement.

The original source V2 was preserved. Earlier exact-sight experiments that extended the neck were rejected; do not use them as a starting point. No re-rigging, skin repair, runtime weapon-switching system or simulation change is requested.

Binary contribution is separately gated by the source/derivative rights and the repository's contribution terms. Local art-use approval does not fill that record. Coordinate asset paths with the #1900 publisher before an import or code change. A future draft asset PR should include the native source, compatible export, exact source/output hashes, transformation settings, per-file rights evidence, known limits and genuine before/after runtime captures when available. This text-only guide neither closes the issues nor supplies an accepted asset.
