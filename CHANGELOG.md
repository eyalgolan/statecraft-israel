# Changelog

All notable changes to Statecraft: Israel. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Every pull request that changes the game (`sim/`, `content/`, `unity/`)
adds a line under **Unreleased**; CI's `Changelog` job checks this (see `CONTRIBUTING.md`).

## [Unreleased]

### Added
- `content/eras.yaml` with the `founding` era (15 May 1948 to 1 January 1957), a `Content.EraAt(date)` lookup, and the end
  of the last era: the turn that reaches it ends on it (a turn that would leave under 14 days is lengthened to it), the
  game ends with the stable code `era_ended` and the text "The chapter has ended", and later orders are refused. The
  rejection text for a finished game now shows the reason, not its code. The Why panel shows the end under a new
  "Chapter" group. The golden hashes do not change (#56).
- Region borders in the promo look (promo-borders): two-tone outlines along hex edges (a blue band with a white inner
  stripe, a red-orange band with a cream one), drawn inside each region by the terrain shader from one texel per hex, with
  a "borders" visual toggle. Visual only. No invented boundaries: the first real data is the 1948 sub-districts (below);
  the 1949 armistice lines and fronts are research items.
- A portrait of Yitzhak Ben-Zvi: a crop of Harry Pot's 1958 Anefo photo (CC0; later than the Founding slice; #6).
- A portrait of Chaim Weizmann: a crop of a Bain News Service photo, ca. 1915-1920, from the Library of Congress
  (US public domain, PD-Bain; an earlier likeness than his presidency; #6).
- A portrait of Yaakov Dori: a crop of Willem van de Poll's 1949 Nationaal Archief photo (CC0; #6).
- Hebrew UI translation data with key and format-token checks; not yet enabled in the game (#12).
- A local multi-size favicon for the WebGL page.
- A portrait of Zalman Shazar: a crop of Yoichi Okamoto's 1966 White House photo (US public domain, PD-USGov; #6).
- Hebrew names and sourced birth and death dates for all 29 people (issue #1).
- Successors for offices left by deaths in 1952: Ben-Gurion takes Transportation after Pinkas (14 August 1952) and
  Yitzhak Ben-Zvi becomes President on 16 December 1952 after Weizmann; the presidency is vacant in between, while
  the Speaker was acting President (issue #1).
- A portrait of Golda Meir: a 1964 Nationaal Archief photo by Willem van de Poll (CC0; #6).
- Historical-only deputy titles and sourced 1951 terms; Kaplan's June 1952 Deputy Prime Minister title and the linked Finance/Agriculture succession (issue #4).
- People can carry a birth date and a source for each biographical value (Hebrew name, birth and death dates) in
  `people.yaml`; the loader rejects unknown source keys, sources without a `url` or `checked` date, and impossible birth
  dates.
- Browser-playable 1948–1951 (milestone M3): HUD, Cabinet with appointments, Budget, Events, Defence and "Why did this
  happen?" screens over the 3D hex map; sourced content to 31 December 1951; saves; a calibrated economy; the golden
  hash checked in WebGL and macOS players.
- The map (milestone M2): terrain from Copernicus elevation data, biomes from rainfall, 1948 settlements on hexes.
- The 1948 sub-districts on the map (promo-borderdata): the sixteen Mandate sub-districts in force on 15 May 1948
  (Palestine Gazette No. 1552, 1947), digitised from the Survey of Palestine's "Administrative Divisions" map and stored
  as true lines (`content/map/subdistricts_1948.geojson`, sensitivity-reviewed); the map shows them snapped to hex edges in the
  blue border style, labelled "Mandate sub-districts, 1947" from the 1948 start until the 1949 armistice lines replace
  them; the info panel names a hex's sub-district and district. Display only: the sim does not read them.
- Open-source release (public since 2026-10-10): licences (MIT code, CC BY-SA 4.0 content, CC BY 4.0 original docs
  and art, CC0 for AI-only images), asset provenance (`ASSETS.yaml`), contributor
  and content policies, CI for the sim, tools and repository policy.
- Unity-MCP 0.93.3 (`com.ivanmurzak.unity.mcp`, from OpenUPM) as the standard AI-agent bridge in the editor, local
  Custom mode with token auth only (held by `UnityMcpGuard`). It stays offline in batch mode, opens no window there, and is
  left out of player builds.
- A runtime quality fallback for slow machines in the browser: when frames stay slower than 30 fps, the game lowers its
  render scale (to 0.9, then 0.8) and then turns the tilt-shift off, and steps back up when frames are comfortably fast
  again. Presentation only; `?nofallback` turns it off (#44).
- Map: procedural settlement kits and instanced trees (placeholder art) replace the marker boxes: a white Tel Aviv,
  stone Jaffa with its harbour, Arab towns, moshavot and kibbutzim sized by their 1945 population
  (`content/map/settlement_population_1945.yaml`), with citrus groves, olives, palms and cypresses, at the owner's
  symbolic scale (about 60x real size for city blocks, 100x for houses and trees). Presentation only (#31, #32, #34).
- The sea and coast in the promo look: turquoise shallows that deepen to a muted blue offshore, rolling surf filtered
  so it does not shimmer, a fine chop, and a sand strip with wet sand along every sea-level coast (salt flats at the
  Dead Sea). Presentation only.
- Event cards have a promo-style header photograph: real archival photographs for 24 of the 31 events (Meitar
  Collection / National Library of Israel under CC BY 4.0, Nationaal Archief under CC0, and the US National Archives),
  shown in black and white with a subtle sepia/navy grade and a gold frame, with a credit line and alt text. The seven
  events without a free photograph keep the minister's portrait. No card shows the outcome of the choice it asks
  for. On a short window the body scrolls to keep the photograph whole, which also shows photographs at 21:9; if the
  options would be cut, the photograph is dropped whole rather than cut through its credit, and it returns when the
  window grows. Sensitive pictures need the owner's review
  before a `-shareable` build, and `tools/package_release.py` now refuses a build folder that was not built
  `-shareable` or carries an unreviewed sensitive picture (docs/event-images.md). Presentation only: the content hash
  and saves are unchanged.
- The unit art slice (a showcase for the art track, not gameplay): a 1948 IDF rifle trio with a selection ring, an IAF
  Spitfire flying a loop with its shadow, and the corvette INS Haganah, built from code as painted miniatures at the
  promo's symbolic scale. Shown only with `?units` (desktop `-units`) and in `tools/art_checkpoint.sh --units`; an
  authored model can replace a code-built one per unit type (docs/units.md).
- The railways and main roads of 1948 on the map: the Jaffa–Jerusalem line, the main line from Lydda to Rafah, the
  Eastern Railway, Hadera–Haifa, Haifa–Acre–Ras an-Naqura and the Jezreel Valley line, with 13 main roads, traced on
  the public-domain 1946 Survey of Palestine map (`content/map/routes_1948.yaml`, sources per route), drawn as pale dirt
  roads and ballasted track with sleepers at the promo's scale. Each route shows only from its sourced date: the Burma
  Road appears on 14 June 1948, when it was completed. Settlement buildings and trees keep clear of every route. A steam train is a showcase only (`?train`, `?units` and the
  promo screenshots), not normal play. Map-only: the sim does not read them (#35).
- Land use on the map (#33): a patchwork of strip fields (green, ochre stubble, ploughed, fallow, groves in the citrus
  belt) with hedges and farm tracks, and garrigue, terraces and scrub on the wet hills, placed by real slope, rainfall and
  towns and baked by `tools/land_bake.py`. Presentation only. Frame time: measured only on a busy machine so far (about
  +0.9 ms at the gameplay zoom), not yet on an idle one.

### Changed
- Sharett's in-game portrait uses a CC0 Van de Poll photograph in place of the US-unverified GPO source (refs #30).
- Hebrew UI wording, still not enabled: a count-neutral pending-decisions line, "מנדטים" for Knesset seats, and
  reworded attention and full-war labels (#193).
- Ben-Gurion's Transportation term ends on 24 December 1952, when the Fourth Government took office; the seat
  shows as vacant from then until Serlin and Sapir are authored (#26).
- The four armistice events end with 1949 (`to: 1950-01-01`). They still fire on their 1949 dates; a finite window
  means their Prime Minister placeholder doesn't need a holder after his death in 1973.
- Ben-Gurion's in-game portrait is now a crop of Abbie Rowe's 8 May 1951 White House photo (NARA 200318,
  PD-USGov) instead of a Government Press Office photo with no clear US status (#30, decision D-P).
- Later death dates can be recorded for people with open-ended terms in the latest authored government; explicit term ends still cannot pass the holder's death.
- Source Dov Yosef's food-supply and rationing responsibility in the Third Government from 8 October 1951,
  replacing the unsupported vacancy (#2).
- The interface is restyled to the promo art (`promo/final/03`, `04`, `05`, `08`):
  - navy panels with gold trim, corner brackets and a soft shadow;
  - an icon for each indicator in the top bar, with its value and its change since the last turn;
  - a framed date badge;
  - round icon buttons for the screens, with their names under them;
  - signs on the event options;
  - a terrain sign in the hex panel (still no tile yields);
  - the Cabinet as a grid of portrait cards.

  Narrower windows get a compact top bar, and windows narrower than about square get a taller one that puts each change
  under its value. Keyboard focus has a fill at least 3:1 against the unfocused button, and a mouse click leaves no
  focus behind. Presentation only.
- The Cabinet and Defence screens are laid out as the promo (`promo/final/04`, `08`), wider, over the hex panel (which
  is hidden while they are open, so its buttons take no focus):
  - Cabinet: the Prime Minister's card at the side (with every office the PM holds), the legislature as a half circle of
    seats, coalition and opposition, with the majority line and each party's seats, and a card per office with portrait
    (or initials), party, and competence and loyalty as bars;
  - Defence: the Prime Minister, Minister of Defense and Chief of Staff as cards (one per person), the escalation
    ladder with the ordered level marked, the reserve call-up as a bar with its minimum, and the defence share of the
    budget.

  Every figure is the sim's; the promo's forces, intelligence and procurement panels are left out because the sim does
  not model them yet. A hidden screen does no work when the game changes, and an open one updates in place, keeping
  keyboard focus. Presentation only.
- The screens fit narrow windows (#210, #211): in the two-row HUD (narrower than about square, such as a phone's 390 px)
  every screen takes the full width, with the event column and the hex panel hidden under it, and a screen's header
  wraps. Escape closes the open screen. The Cabinet says a refusal that applies to every candidate (the Prime Minister's
  or the President's office) once, not under each name (#215).
- The historical AI no longer sends an order the turn's attention cannot cover (the rule the UI's order queue applies),
  so the sim never has to refuse one; the golden playthroughs are unchanged.
- The browser build downloads a smaller heightmap: `heightmap.r16z` (4.1 MB, packed losslessly: row deltas in byte
  planes, one zlib stream) replaces the raw 8.4 MB `heightmap.r16`, which was served outside Unity's build compression.
  It decodes to the same bytes, so the terrain is unchanged (#44).

### Fixed
- Place names no longer cover the buildings of small settlements such as Degania Alef and Zikhron Ya'akov (#214). A name
  now sits above its town's buildings, or beside or below them when that space is taken, and covers at most a tenth of
  any town. With the "kits" visual toggle off, names keep clear of the marker boxes instead. Presentation only.
- The web build's quality fallback no longer lowers the quality while the page is hidden (#216). A background tab or a
  minimised window (and in Chromium a window covered by others) runs at a few frames a second, which had counted as
  load. Presentation only.
- The border determinism test no longer fails now and then on a pixel or two: it shows the proof pattern once, renders a
  warm-up frame, and compiles shaders synchronously, so both compared frames draw the same font atlas and shaders.
- The 1948 Cabinet no longer shows four vacant seats for offices that did not exist yet. Offices can now carry a `from`
  date, and before it the office is not a Cabinet seat and cannot be filled. The dates are Education and Culture 10 March
  1949, rationing 26 April 1949, President of Israel 16 February 1949 and Minister without Portfolio 8 October 1951. The
  Budget row for Education reads "no ministry until 10 March 1949" (#217).
- The Defence screen's UN ceasefire clock now reads "If full war goes on, the UN imposes a ceasefire in N weeks". The
  old "A UN ceasefire will be imposed in N weeks" contradicted the First Truce, which keeps the game at full war (#218).
- The camera keys (WASD and the arrows) no longer move keyboard focus through the HUD. Before, panning could walk focus
  onto New Game or End Turn, so a stray Enter or Space could press it. Tab still moves focus.
- The Cabinet's "Appoint to …" heading has its own column again and stays clear of the portrait grid's scroll bar.
- The "Not done" toast is opaque and sits clear of the open screen.
- "International Standing" keeps to one line on a full-width bar.
- The open sea no longer shows a blocky checker up close (about 6-20 km): its waves now come from smooth gradient noise
  on rotated lattices instead of value noise differenced along the map's axes. Presentation only.
