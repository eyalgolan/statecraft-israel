# Units in play: design (2026-10-11)

Status: decided and implemented on `units-in-play` (2026-10-11). The rulings are below; the built behaviour, its
sources and what is left open are in `docs/units.md`, "Units in play".

## Rulings (2026-10-11)

The owner ruled on questions 1–4 and the controller adopted the defaults for 5–7:
1. One token per brigade.
2. The figure count follows the call-up (1, 2 or 3).
3. An Avia S-199 until the Spitfire's sourced date (21 October 1948), then the Spitfire.
4. The four brigades with no sourced position are hidden; the rest ship.
5. Dated, sourced positions only; no movement in operation events.
6. After the armistices, only what a source says continued is shown.
7. No orders; the unit's card links to the Defence screen.

After the staff review, the controller ruled (2026-10-11):
- **C1:** no invented end dates. Each window ends at its sourced end, the unit's sourced disbandment or its front's
  armistice, marked `end_basis: source` or `front_armistice`; the blanket 20 July 1949 is gone.
- **I1:** "since" only from a sourced start, never the game's first day.
- **I2:** at most one hex from the sourced anchor, compact beside a town. Never in ground the other side held; until
  front data exists, the one-hex limit is the guard.
- **I3:** 101 Squadron's bases where two sources agree (Hatzor from 21 November 1948).
- **I4:** the Spitfire from its earliest sourced service. That is the first sourced sortie on 15 October 1948; the
  aircraft arrived on 27 September, unarmed.
- **I5:** units appear in screenshots and editor renders only when asked for.
- **I6:** the promo's badge, a wider trio, headgear only where sourced, and the ring for selection only.

Corrections found while implementing:
- Wedgwood's pennant number: the earlier draft said K-18 without a source. Its two articles conflict (the Beauharnois
  article gives Hashomer, K 20), so the map shows "INS Wedgwood" and no number.
- Positions now come with dates from the sources, including Hebrew Wikipedia: Golani at Yavne'el, then Julis from
  December 1948; the Negev Brigade at the Mekorot camp near Nir Am; the 7th Brigade at Latrun, then Ein Shemer.

Goal: the PR #139 unit art (`docs/units.md`, today behind `?units`) in normal play, looking like
`promo/final/03-gameplay-1936.png`, driven only by sim state plus sourced map-only data. The sim and goldens do not move.

## 1. What the sim knows

The military in the sim is one national ladder and one number. It has no formations and no positions.

| State | Where |
|---|---|
| Security level (rung 1–5) | `GameState.Rung`, sim/Statecraft.Sim/GameState.cs:30; hashed at :88 |
| UN ceasefire clock (weeks) | `GameState.CeasefireWeeksLeft`, GameState.cs:31; run by SecuritySystem.cs:64-73 |
| Reserve call-up (0–1) | `GameState.Mobilization`, GameState.cs:32; set by SecuritySystem.cs:44-51 |
| Minimum call-up per rung | content/rules.yaml:21 `[0.02, 0.05, 0.15, 0.4, 0.8]`; floor applied at SecuritySystem.cs:53-62 |
| Starting rung | content/rules.yaml:3 (`start_rung: 5`, the war is under way on 1948-05-15) |
| Orders | `SetMobilization` and `SecurityOrder`, sim/Statecraft.Sim/Commands.cs:31, :38; executed at Simulation.cs:108-117 |
| Events that set the rung | `EventOption.SetRung`, Content.cs:233; applied at EventSystem.cs:76 (for example operation_horev.yaml:18, second_truce.yaml:18) |
| Which operations happened | `FiredEvents` and `ChosenOptions`, GameState.cs:35, :38 |
| Security indicator | content/model.yaml:7 (`security`, national 0–1) |

Brigades exist only as prose in event text ("with five brigades", operation_horev.yaml:3).

**Location data: none.** The sim has no brigades, no air force and no navy. It has no unit identifiers and no
coordinates. The only things it keeps on hexes are map actions (`Placement`, MapActions.cs:8-15), which are transit
camps and housing.

## 2. How units appear in normal play

All of this is presentation. A pure function reads `(Content, GameState, UnitData)` and returns a roster of
`(unit id, art type, hex, figure count, pose)`. The map rebuilds the roster only when its inputs change, in the same way
that `GameRoot.RenderPlacementsIfChanged` works (unity/Assets/Scripts/UI/GameRoot.cs:226-235).

### Data addition: `content/map/units_1948.yaml` (map-only)

`ContentLoader` hashes only the files it reads (ContentLoader.cs:156-191). It will never read this file, so the content
hash and the goldens stay fixed. This is the pattern `routes_1948.yaml` already uses (docs/routes.md:17). Each entry has:

- `id`, `name`, `number`, `service` (army, air or navy) and `art_type` (a `UnitShowcase.*Type` constant);
- `from` and `to` dates: formation and disbandment;
- `anchors`: a dated list of named places, each resolved to a hex the way `settlement_hexes.yaml` is generated;
- `sources` (`url` and `checked`) for every date and every anchor.

An entry whose anchor has no source keeps `anchor: null` and draws no token. Positions are never interpolated or guessed.

### Army: one token per brigade, 12 in 1948

Sources checked 2026-10-11, all on en.wikipedia.org:

| Brigade | Sourced sector or base | Source |
|---|---|---|
| Golani (1st) | HQ Yavne'el; Lower Galilee, Jezreel, Jordan and Hula valleys | /wiki/Golani_Brigade |
| Carmeli (2nd) | "the northwest" (split from Levanoni, Feb 1948); no HQ named | /wiki/Carmeli_Brigade, /wiki/Golani_Brigade |
| Alexandroni (3rd) | "the Sharon region"; no HQ named | /wiki/Golani_Brigade (Plan Dalet section) |
| Kiryati (4th) | Tel Aviv | /wiki/4th_Armored_Brigade_"Kiryati" |
| Givati (5th) | from the war's second stage: Gedera, Gan Yavne, Be'er Tuvia | /wiki/Givati_Brigade |
| Etzioni (6th) | Jerusalem | /wiki/Etzioni_Brigade |
| 7th | Latrun (May–July 1948) | /wiki/7th_Armored_Brigade_(Israel) |
| 8th | formed 24 May 1948; no sector named | /wiki/8th_Armored_Brigade_(Israel) |
| Oded (9th) | conflicting: "headquartered in Jerusalem", yet it fought at al-Malkiyya | /wiki/Oded_Brigade |
| Harel (10th) | the Jerusalem corridor; formed 16 April 1948 | /wiki/Harel_Brigade |
| Yiftach (11th) | formed late April 1948 from Jezreel and eastern-valleys battalions; disbanded May 1949 | /wiki/Yiftach_Brigade |
| Negev (12th) | the Negev, split along the Beersheba–Gaza road | /wiki/Negev_Brigade |

Where the source leaves a gap (Carmeli, Alexandroni and the 8th have no named place, and Oded's sources conflict), the
anchor must come from Netanel Lorch, *The Edge of the Sword* (1961), or from the IDF and Defense Establishment Archives
before that token is drawn. The Yiftach article says that 9 of the 12 brigades were disbanded in May 1949. Each brigade's
`to` date needs its own source.

- **Where:** on the anchor's hex. If two tokens share a hex, the later one by ordinal id moves to the nearest free land
  hex.
- **How many figures:** 3 when `Mobilization >= 0.4`, 2 when it is at least 0.15, otherwise 1. The thresholds are the
  rung floors at rules.yaml:21. The art type is the brigade's (helmet, bush hat or stocking cap).
- **When they change:** a token appears at its `from` date and goes at its `to` date. It moves only at a sourced, dated
  anchor change. The figure count changes with a call-up order or a rung change, so on the next turn. Tokens do not
  follow a player's non-historical event choice; they mark sectors, not manoeuvres.

### Air

101 Squadron, formed 20 May 1948 at IAF Ekron (ex-RAF Aqir), first mission 29 May
(/wiki/101_Squadron_(Israel)); the IAF itself was founded 28 May 1948 (/wiki/Israeli_Air_Force). The squadron flew the
Avia S-199 first and the Spitfire later (docs/units.md, "Aircraft").

- One token, at Ekron.
- At rung 4 or higher it flies the showcase loop over its base. Lower than that, it stands parked.
- It shows the Spitfire model only from that type's sourced service date. Before then it needs an S-199 (question 3).

### Navy

The navy began with four ex-Aliyah Bet ships impounded in Haifa harbour (/wiki/Israeli_Navy). Three are sourced by
name: Haganah K-20 (/wiki/HMCS_Norsyd), Wedgwood, Flower class like K-20 (/wiki/HMCS_Beauharnois_(K540)), and the
flagship Eilat (/wiki/USCGC_Northland_(WPG-49)). Haganah and Wedgwood are drawn off Haifa with the existing corvette model;
Eilat waits for a model; the fourth ship is not drawn until it is named.

## 3. Interaction

- **Selection:** click a token (`HexPicker`). The existing white ring goes on its hex.
- **Panel (read-only):** name and number, service, formation date, the sector in the source's words, commanders with
  dates only where sourced (Golani: Moshe Mann, then Nahum Golan in mid-1948), the national values the token reflects
  ("Reserve call-up 80% · Security level 5"), and the source links.
- **Orders: none in this round.** A "Defence" button opens the existing Defence screen
  (unity/Assets/Scripts/UI/DefencePresenter.cs), where call-up and security orders already live.

## 4. Performance budget

The budget is the one in docs/visual-v1.md:42-44:
- p95 of 16.7 ms or less at gameplay03 and diorama02 with every feature on;
- the units feature itself costs 2.5 ms or less (`median(all) - median(-units)` in `?vprofile`).

The worst case is about 12 tokens of 3 figures at about 3,600 triangles each. That is about 130k triangles, plus one
aircraft and two ships. To stay inside the budget:

- **Shared meshes:** one mesh and one material per art type, drawn instanced (or through the SRP Batcher). That gives
  about 10 draw calls, plus the shadow pass.
- **No per-frame CPU work** except the aircraft's single transform. The roster is rebuilt per turn or per order, not per
  frame.
- **Fallbacks, in order:** no figure shadows on the low tier; one figure per token beyond the gameplay zoom.

diorama02 already reads 17 ms p95 on a loaded machine (docs/routes.md:220), so measure on an idle machine
(`machine_lock.py --exclusive`).

## 5. Tests

- **Hash unchanged:** the content hash is the same with and without `units_1948.yaml`. This is the
  `RouteTests.TheRoutesDoNotMoveTheContentHash` pattern. The goldens stay unchanged.
- **The roster is pure:** `GameState.Hash()` is equal before and after building the roster. Two builds from equal
  inputs are equal.
- **Data lint:** every date and anchor has `url` and `checked`; anchors resolve to land hexes (sea for ships);
  `art_type` exists; `from` < `to`; ids unique; a null anchor draws nothing.
- **Roster cases:**
  - 1948-05-15 at rung 5 with call-up 0.8: every sourced brigade has 3 figures, the aircraft flies, Haganah and Wedgwood are
    off Haifa;
  - call-up 0.10: 1 figure each;
  - after a brigade's `to` date: its token is gone;
  - at rung 2: the aircraft is parked;
  - a shared hex: the deterministic offset applies.
- **Presentation golden:** the roster per turn of the historical playthrough, kept in a separate file under
  `unity/Assets/Tests`. It is not a sim golden.
- **Art checkpoint:** a gameplay03 render with units on, plus `?vprofile` against the budget above.

## 6. Owner questions (decided 2026-10-11: see "Rulings" above)

1. **Token granularity:** one per brigade (12), or one per front (north, centre, Jerusalem, south)?
   *Default: per brigade.*
2. **Figure count:** driven by call-up, or always a trio as in the promo?
   *Default: by call-up (1, 2 or 3).*
3. **Aircraft before the Spitfire's sourced service date:**
   - (a) build a code-built Avia S-199;
   - (b) show no aircraft;
   - (c) show the Spitfire throughout, because the promo beats the era.

   *Default: (a).*
4. **Tokens with no sourced anchor** (Carmeli, Alexandroni, the 8th, Oded): hide them until Lorch or the archives are
   checked, or block the feature on that research?
   *Default: hide them and ship the rest.*
5. **Movement:** dated sector anchors only, or also move tokens during operation events (Yoav, Hiram, Horev) when they
   fire?
   *Default: dated anchors only this round.*
6. **After the armistices (from mid-1949):** show only the brigades that a source says continued, or hide army tokens
   at rung 2 and below?
   *Default: show what is sourced, at its anchor.*
7. **Orders:** none this round, with a link to the Defence screen. Confirm?
   *Default: yes.*
