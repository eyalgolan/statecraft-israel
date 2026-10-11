# Changelog

All notable changes to Statecraft: Israel. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Every pull request that changes the game (`sim/`, `content/`, `unity/`)
adds a line under **Unreleased**; CI's `Changelog` job checks this (see `CONTRIBUTING.md`).

## [Unreleased]

### Added
- A portrait of Chaim Weizmann: a crop of a Bain News Service photo, ca. 1915-1920, from the Library of Congress
  (US public domain, PD-Bain; an earlier likeness than his presidency; #6).
- A portrait of Yaakov Dori: a crop of Willem van de Poll's 1949 Nationaal Archief photo (CC0; #6).
- The Third Knesset and the 26 July 1955 election results (seats by list), with the National Religious Front and the
  Religious Torah Front as parties; the Second Knesset now ends on 15 August 1955 (refs #26).
- Hebrew UI translation data with key and format-token checks; not yet enabled in the game (#12).
- A local multi-size favicon for the WebGL page.
- A portrait of Zalman Shazar: a crop of Yoichi Okamoto's 1966 White House photo (US public domain, PD-USGov; #6).
- Region borders in the promo look (promo-borders): two-tone outlines along hex edges (a blue band with a white inner
  stripe, a red-orange band with a cream one), drawn inside each region by the terrain shader from one texel per hex, with
  a "borders" visual toggle. Visual only. The game ships no region data yet (no invented boundaries: the sourced 1945-48
  sub-districts, 1949 armistice lines and fronts are a research item), so nothing is drawn in play.
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
- The historical AI no longer sends an order the turn's attention cannot cover (the rule the UI's order queue applies),
  so the sim never has to refuse one; the golden playthroughs are unchanged.
- The browser build downloads a smaller heightmap: `heightmap.r16z` (4.1 MB, packed losslessly: row deltas in byte
  planes, one zlib stream) replaces the raw 8.4 MB `heightmap.r16`, which was served outside Unity's build compression.
  It decodes to the same bytes, so the terrain is unchanged (#44).

### Fixed
- The camera keys (WASD and the arrows) no longer move keyboard focus through the HUD. Before, panning could walk focus
  onto New Game or End Turn, so a stray Enter or Space could press it. Tab still moves focus.
- The Cabinet's "Appoint to …" heading has its own column again and stays clear of the portrait grid's scroll bar.
- The "Not done" toast is opaque and sits clear of the open screen.
- "International Standing" keeps to one line on a full-width bar.
- The open sea no longer shows a blocky checker up close (about 6-20 km): its waves now come from smooth gradient noise
  on rotated lattices instead of value noise differenced along the map's axes. Presentation only.
