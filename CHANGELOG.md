# Changelog

All notable changes to Statecraft: Israel. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Every pull request that changes the game (`sim/`, `content/`, `unity/`)
adds a line under **Unreleased**; CI's `Changelog` job checks this (see `CONTRIBUTING.md`).

## [Unreleased]

### Added
- Writer preview: `dotnet run --project sim/Statecraft.Cli -- --preview --set key=value` plays the historical run with a
  rules.yaml change applied in memory and prints the changed state lines and a table of chosen values, without Unity and
  without touching `content/` (fixes #295).
- One golden manifest (`sim/Statecraft.Sim.Tests/golden/manifest.txt`) pins the golden hashes; `STATECRAFT_UPDATE_GOLDEN=1`
  now rewrites it and the `GoldenRun.cs` constants, and a test names any doc that shows an old hash (fixes #293).
- Era system switches in eras.yaml (`systems`): `legislature: state | assembly` (an assembly's vote against a policy or
  the budget is held and reported, but does not bind) and `map_actions` (the actions the era allows). The Cabinet and
  Budget screens say when the body's vote does not bind, and the hex panel shows only the era's map actions. With no
  `systems` key an era behaves as before; goldens unchanged (fixes #58).
- An office can now have several holders at once (`several_holders: true` in offices.yaml): Minister without Portfolio
  and the deputy ministers of Education. Adds Dov Yosef's term without portfolio (24 December 1952 to 15 June 1953)
  and Ami Assaf as a second Deputy Minister of Education (from 11 June 1956). Event text, policies and effects cannot
  name such an office, and the Cabinet refuses to appoint to it. The Cabinet screen shows a card for each holder. No
  golden moves (#277).
  now rewrites it and the `GoldenRun.cs` constants, `tools/art_checkpoint.sh` reads it, and a test names any doc that shows an
  old hash (fixes #293).
- Rules per era (#58, second piece): the attention, economy, population, security, map-action and cabinet systems read
  `RulesAt(date)` of the turn's starting date instead of `rules.yaml` directly, and the Defence and Info screens and the
  HUD follow. With no `content/rules/<era>.yaml` files yet, nothing changes: the goldens do not move. A test fails if a
  system reads the base rules directly for anything but a start value.
- JSON Schemas for `people.yaml`, `offices.yaml`, `parties.yaml` and the event files (#265), in `content/schema/`. Each file
  names its schema on the first line, so an editor with a YAML language server flags a wrong field name or a missing
  `sources:` as you type. `SchemaTests` checks all real content against them, keeps their field names equal to the loader
  types and checks that they reject the usual mistakes. The comment changes the content hash, so an old save shows the
  content-changed warning once. The golden hashes do not move.
- The Fourth to Seventh Governments (24 December 1952 to 1956) with their coalitions and the ministers' dated terms,
  the 1953-1956 deputy ministers, the Ministries of Postal Services (from 3 November 1952) and Development (from
  15 June 1953), and the Chiefs of Staff Maklef and Dayan. Dates follow the Knesset's own position records and plenum
  minutes, checked against both Wikipedias. The Third Government now ends on 24 December 1952 and gains its 1952 changes:
  Haim Cohn at Justice, Lavon without portfolio, Nurock at Postal Services, and Welfare vacant after Levin left on
  18 September (refs #26).
- A golden-hash failure now shows what changed (#256): `GameState.Describe()` writes the state `Hash()` hashes as readable
  lines, each golden run's final state is committed under `sim/Statecraft.Sim.Tests/golden/`, and a failing golden test
  prints the changed lines by section. `STATECRAFT_UPDATE_GOLDEN=1 dotnet test` rewrites the snapshots. `Hash()` is
  unchanged, so the goldens do not move.
- A start screen (#247): a painting of the 14 May 1948 ceremony in the Tel Aviv Museum hall (AI-generated concept art
  with no identifiable person), two lines of the Declaration of Independence in the official Hebrew with the official
  English under each, and Continue (only with a save) and New Game (with the top bar's two-press confirmation). Automated
  runs skip it (golden, smoke, shot, the profiles, units, borderproof), as does `?nostart`. The Hebrew is set in Frank Ruhl
  Libre (OFL) and laid out right to left by UI Toolkit's advanced text generator (#193).
- Rules per era (#58, first piece): an era can override `rules.yaml` keys in `content/rules/<era>.yaml` (only the keys it
  changes; maps merge key by key, a list or a value replaces). The merged rules are read with the strict `Rules` type, so a
  misspelt key or wrong type fails naming the era file and line, start values (`treasury_start`, `start_rung` and so on) are
  refused, a file that names no era is refused, and `rules/` is in the player manifest. They are checked like `rules.yaml`. `Content.RulesAt(date)` returns the rules of
  the era a date is in (built once at load); the founding era has no file, so its rules are `rules.yaml`. Nothing reads
  `RulesAt` yet, so the goldens do not move.
- Units in play: the map draws the 1948 brigades, 101 Squadron and the first navy ships as Civ-style tokens at their
  dated, sourced positions (`content/map/units_1948.yaml`, map-only, outside the content hash). The figures follow the
  reserve call-up, the squadron flies at security level 4 or 5 (an Avia S-199, then the Spitfire from its first sourced
  sortie, 15 October 1948), a badge marks each token, and selecting one shows the promo's ring and a read-only card with
  a link to the Defence screen. Every position ends on a sourced date or on its front's armistice. Screenshot runs show
  units only with `&units`. The goldens do not move.
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
- The Third Knesset and the 26 July 1955 election results (seats by list), with the National Religious Front and the
  Religious Torah Front as parties; the Second Knesset now ends on 15 August 1955 (refs #26).
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
- The 1949 armistice lines on the map (armistice-lines, #196): from each agreement's date (the Mandate sub-district
  layer's per-front switch) that front's armistice zones are drawn over the sub-districts, and the label and info panel
  name its lines; the sub-district bands stay as the base. Egypt (24 Feb 1949): the El Auja Demilitarized Zone and the Gaza-Rafah line from the
  Wadi Hasi to Gaza, and from 22 Feb 1950 the line on the signed modus vivendi map; Lebanon (23 Mar 1949): the
  international boundary; Jordan (3 Apr 1949, line A in the Iraqi sector by its May-July 1949 stages): low accuracy,
  traced from a low-resolution copy of the signed map and labelled so, Jerusalem left out; Syria (20 Jul 1949): the line
  through Annex I's points and the three-part Demilitarized Zone, from the annexed 1:50,000 map. Stored as true lines
  (`content/map/armistice_lines_1949.geojson`, status draft), drawn snapped to hex edges in the red-orange style; the
  info panel names the line or zone and its agreement. Display only: the sim does not read them.
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
- Map: the 271 other places of 1,000 or more people on 15 May 1948 (Village Statistics, 1945) as Arab village and Jewish
  settlement kits beside the 25 game settlements (Nesher as a mixed locality), named by their 1948 names at close zoom,
  with today's name in a tooltip only where a living place carries on, and a light scatter of Bedouin camps sized by
  each sub-district's listed tribal total (`content/map/localities_1948.yaml`, draft; the player reads its baked
  compact form, `localities_1948.tsv`, from `tools/localities_bake.py`). The unit tokens keep off the
  villages and camps as they do off the towns. Presentation only: the sim never reads the file.
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
- The game settlements Lod and Ramla show their 1948 names, Lydda and Ramle, as Village Statistics 1945 lists them
  (owner ruling). Content only: the goldens and the smoke run are unchanged; saves made before load with the
  "different game content" warning.
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
- The Unity idle test `PromoScreensTests.AnOpenScreenIsStillWhenIdle` no longer fails now and then (#242). It was not a
  font atlas and not a recurring layout. A batch-mode editor runs few frames, and now and then it tells
  `UIDocument` late that the test's GameObject exists. `UIDocument` then re-adds its root and the whole screen lays out
  once more. The test now queues a player-loop update on every step, waits for that editor notification plus 5 frames,
  and then allows no relayout or repaint in the next 60 frames (before: at most one, in the first 60 steps).
- The Ministry of Rationing and Supply, and Dov Yosef as its minister, now start on 10 March 1949, when the First
  Government took office, not on 26 April 1949 (the day rationing was decided, which the austerity event keeps). The
  Knesset's 8 March 1949 First Government document names him. Agriculture is unchanged. The goldens do not move; the
  content hash does, so an older save loads with the "different game content" warning (#225).
- Railways keep their width outside the gorges: a rail band narrows only where the ground leaves its tolerance on both
  sides (a gorge floor, a gorge side, a ridge), not for one hill or one drop beside the line. The Jaffa–Jerusalem line
  keeps 94.9% of its width outside the Sorek (87.4% before) and the Jezreel Valley line 92.1% outside its gorges (82.8%).
  Map only; the goldens do not move (#252).
- Railways and roads on the map no longer zigzag on straight stretches (the traced lines are smoothed, keeping real
  bends), and the Jaffa–Jerusalem railway no longer climbs the Sorek gorge's walls: the track keeps its own grade,
  flat across, narrows smoothly to the gorge floor and stays clear of the ground under its rails (#212, #213).
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
- Panning past the map's edge no longer fills the view with flat land and a hard seam: the camera stops where the view
  meets the edge of the map, and at close zoom over high ground (the Golan, the Galilee hills) it no longer sinks into
  the hills. At the overview, where the view is wider than the map, it is centred east to west (#174).
