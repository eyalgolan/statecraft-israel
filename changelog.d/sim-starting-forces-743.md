- GDD Phase 5 PR 4: the sourced starting forces, both sides (refs #743, #385). A new Historical game starts with one sim
  unit per row of `content/map/units_1948.yaml` that is not hidden, is an army row, has a 15 May strength and an anchor
  window covering 15 May 1948, on that anchor's hex (R-UNIT-006; precomputed by the new `tools/unit_anchor_hexes.py`
  into `content/map/unit_anchor_hexes.yaml`, so the sim does no projection maths): Golani, Carmeli (at Haifa), Alexandroni, Kiryati,
  Givati, Harel and Negev as `veteran_soldier`, and the Egyptian force, the Arab Legion, the Syrian and Iraqi forces and
  the ALA, each under its side's new infantry type in `content/unit_types.yaml` (labelled design values, 3 movement
  points). Strength is the row's people in 1,000-person units, halves up, never below 1 (owner, 20 October 2026, #743:
  "all five defaults"). The sim now reads the file, so it is in the content hash and every golden moves (re-pinned,
  docs/determinism.md): the twelve units take ids 0 to 11, and nothing else in the run changes. `--release-gate`,
  `Build -shareable` and `tools/package_release.py` now refuse until every row a unit starts from is `fact-checked`
  (Q-START-3); today none is. Open mode still cannot start (R-UNIT-007).
