### Changed

- One spelling rule for Arabic place names is written down (owner, 25 October 2026, #1586): the 1948 names follow
  *Village Statistics, 1945* (Beit and Deir), and a quoted source form keeps the spelling its source prints.
  `docs/WRITING_GUIDE.md` gains a "Place names" section, and the glossary gains a `place-names` convention
  (`docs/UX_GLOSSARY.yaml` and its Markdown view), with the Hebrew-term transliteration left open as it was.
- The content that broke the rule now follows it: the four `burma_road` waypoints of `content/map/routes_1948.yaml`, the
  three `burma_road` segment ends of `content/corridors.yaml`, the prose that names them there and in
  `content/map/units_1948.yaml`, and the same names in `docs/units.md` and `docs/visual-v1.md`.
  Quoted forms, Wikipedia URLs, record ids and the present-day `today.name` of Beit Jann are unchanged, as is the dated
  decision record in `docs/superpowers/plans/2026-10-15-gdd-phase-4.md` and the English Wikipedia phrase quoted in
  `docs/routes.md`. The baked
  `content/map/route_paths_1948.tsv` is byte-identical: it carries coordinates, not names.
