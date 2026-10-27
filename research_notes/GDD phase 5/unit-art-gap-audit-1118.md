# Unit art gap audit: what Phase 5 PR 17 still lacks against the 1948 Collection

**AI-prepared draft. A person has not checked it.** Written 2026-10-19 by Claude (an AI agent working for the
contributor beriki770-ship-it), against `main` as of 2026-10-19 (first read at `9a69d5b4`; the cited unity, content, docs and tools files are unchanged at the branch base). Claude read the repository files and issue comments named below; pages marked [carried] were not opened again. No person has
compared any of it with the models, the code or a source, so this note stamps no `checked:` date on any source
(`docs/CONTENT_POLICY.md` section 2, rules 6 and 7). The "What a person must check" list at the end says what is left.

**H01 reply update, 20 October 2026 (Codex):** the [owner's conditional decision of 17 October](https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6233506419) says that no new model is needed if H01 is the armoured sandwich truck. The tagged contributor [`pixelrenderai-hash` confirmed on 19 October](https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6267073867) that it is that truck. Codex read both comments for this update; no further identity of the respondent is inferred. This is separate from Claude's original research above and supplies no independent mesh comparison, historical source check or human sign-off.

Slice #1118 (refs #756, GDD Phase 5 PR 17, plan `docs/superpowers/plans/2026-10-15-gdd-phase-5.md`, "PR 17"). It is an
inventory: no model, no art, no code and no content row changes, and no unit art is approved here. Claim:
https://github.com/eyalgolan/statecraft-israel/issues/1118#issuecomment-6266104162.

## Conventions

- **[R]** Claude read it in the repository at `9a69d5b4` (path and line).
- **[I]** Claude read it in a GitHub issue comment on 2026-10-19 (a link).
- **[W]** Claude opened the web page on 2026-10-19 and read the words quoted.
- **[carried]** taken from an earlier note in this repository, not opened again here.
- **[S]** and **[C]**: single or secondary source, and sources conflict, as in `unit-art-sourcing-1948.md` "Conventions".
- **[G]** not found, or not reachable from the machine this was written on.
- "MATKOT" is the name the owner's comments use for the 1948 Collection's author (#756, 17 and 18 October). The
  repository's docs and `ASSETS.yaml` say Tal Heuberger. This note assumes the owner means the same author, as his
  comments imply; the repository does not say so in a file.

## 1. The requirements, as the owner and the plan state them

| Requirement | Words and where | Source |
|---|---|---|
| Which models | "yes, model both the 65 mm "Napoleonchik" gun and the "sandwich" armoured truck (sourced in #724), in Tal's style. They go into the model queue and must pass the asset load gate (stream after the first frame, slimmed, no LFS, credited)." Owner, 17 Oct 2026 | [I] https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6231517265; plan line 778 and 963 [R] |
| The gun is H02 | "Tal's existing H02 "Napoleonchik" model (on main, #680) is the gun. Only the armoured car or truck needs a new model from Tal." Owner, 17 Oct | [I] https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6232838755 |
| The truck is H01 | The owner said on 17 Oct that no new model is needed if the existing model is the armoured "sandwich" truck. The tagged contributor `pixelrenderai-hash` confirmed H01's identity on 19 Oct. This records that condition and reply, not a source or mesh comparison | [Owner condition](https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6233506419) and [tagged contributor reply](https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6267073867), read by Codex for this update |
| Who reviews | Q-ART-3: "Me (the owner)"; then "unit models by MATKOT count as reviewed and approved. They need no separate owner sign-off, only the asset load gate, the licence and the provenance rows. Unit art from anyone else goes to the owner for sign-off." | [I] https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6240322942 and https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6240458367 |
| The `-shareable` gate | "the `-shareable` gate does not need to block MATKOT's unit art for an owner review; any concern found in code review goes to the owner as usual. Other unit art keeps the named review." Owner, 18 Oct | [I] https://github.com/eyalgolan/statecraft-israel/issues/756#issuecomment-6254447209. `Build.ShareableRefusal` checks events, photographs and clips only [R] `unity/Assets/Editor/Build.cs:57-119` |
| Neutral pennant | Q-ART-1: the owner answered "2+3", which the comment spells out as a neutral pennant now, replaced by a sourced emblem when one is found. Owner, 15 Oct | [I] https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183586870; plan line 962 [R] |
| No placeholder art | "Nothing is drawn with IDF figures, and there is no placeholder art." Owner, 17 Oct, on the Arab rows | [I] https://github.com/eyalgolan/statecraft-israel/issues/740#issuecomment-6230439760 |
| Scope of PR 17 | Figures or models for artillery, the Israeli armoured car and each Arab force D-G1 names; "sourced to the same standard as the IDF units"; the art hook is `unity/Assets/Resources/Units/Authored/<art type>`; test: every unit type in `unit_types.yaml` has an art type with a model | [R] plan lines 776-791 |

Plan drift, reported and not edited: `unity/Assets/Resources/Units/Authored/` does not exist. The Collection models
stream from `CollectionStream` and are never in `Resources/` ([R] `docs/units.md:164-170`). And `content/unit_types.yaml`
has no `art_type` field ([R] lines 28-29), so the plan's test cannot be written yet.

## 2. What exists

**Models on main** [R] `unity/Assets/Art/Collection/`: `figure-vehicle-napoleonchik-65mm-1948-v2` (H02),
`figure-c08-arab-legion-v2`, `figure-c09-irregular-v2` and `figure-vehicle-sandwich-convoy-1948-v2`, each with a far
mesh, a near mesh, a material and a 512 px colour map. One `ASSETS.yaml` row covers all of them (CC0, AI-generated with
Tripo, artist Tal Heuberger, status `verified`; `ASSETS.yaml:585-591`). The per-model prompts, concept images and task
ids are "kept outside the repository" (same row), so no file here records what any one model is meant to show.

**Where each model is used** [R]:
- `UnitFigures.cs:31-32` names C08, C09 and H02. `BySide` maps `jordan` to C08 and `ala` to C09 (lines 58-61);
  `ByVehicle` maps `gun_65mm` to H02 and `jeep` to H03 (lines 65-68).
- `UnitData.cs:191-192`: the only vehicle types a row may carry are `gun_65mm` and `jeep`.
- `content/map/units_1948.yaml`: `gun_65mm` from 11 July 1948 on five Israeli rows (Carmeli, Givati, Harel, Yiftach,
  Negev; Ben-Gurion's diary, 11 July 1948, https://bengurionarchive.bgu.ac.il/en/node/14176 [carried]). The Arab Legion
  row (`arab_legion`, side `jordan`) and the Arab Liberation Army row (`arab_liberation_army`, side `ala`) carry no
  vehicles.
- `RoadVehicles.cs:25,70`: the sandwich model is drawn as one convoy truck on the Burma Road, 14 June to 7 December 1948,
  at the close zoom only, "never units, never in the sim" (same file, class summary). No unit token uses it.
- The sim has `artillery` and `armoured_car` unit types ([R] `content/unit_types.yaml:28-29`): a side and a class, no art
  type. The roster still reads `units_1948.yaml`, not `State.Units`; that switch is #753, open, unassigned, no PR found as of 2026-10-19 [I]
  https://github.com/eyalgolan/statecraft-israel/issues/753.
- The neutral pennant is built: a plain grey triangle in no side's colours, flown by the two Arab forces the map draws
  ([R] `UnitFlags.cs:26-30,48-49`; `docs/units.md:429-431`). Israel's flag is the only sourced flag the map draws.
- Arab rows with no figure (Egypt, Syria, Iraq, Lebanon) are data only and off the map (`UnitRoster.OnMap`,
  `UnitRoster.cs:61`). There is no row for the local Arab forces.

## 3. Per-type gaps

"Checked" is deliberately not a person's check. Every row is **Claude read 2026-10-19, person to verify**.

| Unit type | Model used now | Current use | Gap | Source | Existing model fits, or a distinct military truck? |
|---|---|---|---|---|---|
| Artillery, the 65 mm gun (`gun_65mm`; sim class `artillery`) | H02 `figure-vehicle-napoleonchik-65mm-1948-v2` | On the base behind the trio of five Israeli brigades from 11 July 1948 ([R] `UnitFigures.cs:63-68`, `docs/units.md:180`) | (1) No file records that H02's shape was compared with the gun. The owner chose it by decision, not by comparison. (2) The sim `artillery` type has no art type. (3) Wheel size, track and length of the real gun are [G] in the sourcing note, so any model scales from photographs. (4) No Arab gun is modelled; one Commons photo shows an Arab Legion gun described as a 25-pounder, while its category calls it a 3.7-inch howitzer [C, carried]. Not requested by the owner | Owner's decision, link in section 1. Type: https://en.wikipedia.org/wiki/Canon_de_65_M_(montagne)_mod%C3%A8le_1906 [carried]; shape photographs in `unit-art-sourcing-1948.md` section 3 [R]. Claude read, person to verify | **Fits by the owner's decision.** A person should still set it beside the Commons photographs of the surviving guns (Yad Mordechai, Batey HaOsef) |
| Israeli armoured car (sim `armoured_car`; "sandwich" truck) | No unit token uses it yet. The model identified in the tagged contributor's reply is H01 `figure-vehicle-sandwich-convoy-1948-v2`, used only as a road vehicle | No token draws any armoured vehicle. The model is a lone Burma Road convoy truck | (1) **H01 is identified as the armoured sandwich truck in the tagged contributor's reply** (the condition and reply in section 1); an independent comparison of its meshes with the sources remains unrecorded. The repository holds no prompt and no description. The model's name says "convoy"; issue #725's body calls it a "sandwich-armoured convoy truck" [I] https://github.com/eyalgolan/statecraft-israel/issues/725, which is the issue's wording, not an inspection. (2) No `armoured_car` vehicle type in `UnitData.VehicleTypes`. (3) No row in `units_1948.yaml` puts an armoured car with a unit from a day; the type-level in-window source is [G]. (4) The sim type has no art type. (5) No source in the files read says which vehicles ran the Burma Road convoys (`burma-road-convoy-end.md` and `RoadVehicles.cs` place the model there with no vehicle-type source) | Build: Tank Archives, "Israeli Sandwiches", https://www.tankarchives.com/2017/08/israeli-sandwiches.html [carried; not opened again, the site redirected to a Google captcha from this machine]: steel-wood-steel sides and rear, a solid 14 mm front plate, most often on the Ford F15 15 CWT chassis, 127 built 28 December 1947 to 15 April 1948. Steel-wood-steel: https://en.wikipedia.org/wiki/Bab_al-Wad [W]: "regular trucks with improvised armour made of two sheets of steel and a layer of wood in between". Class-level diary mentions in `war-1948.md` section 8 [carried]. Claude read, person to verify | **Identified by the tagged contributor; no new truck model is needed under the owner's condition.** A person must still compare the near and far meshes with the sourced features in section 4: slab armour on the sides and rear and a solid front plate on a light truck chassis. The contributor's identification does not record that comparison |
| Arab Legion (side `jordan`) | C08 `figure-c08-arab-legion-v2` | Three figures on the `arab_legion` row, Ramallah to 11 June 1948, then Latrun; drawn only after C08 streams in ([R] `docs/units.md:119-162`) | (1) No file records C08 against any photograph of the Legion. (2) The sourcing note finds no dated primary text for any force's dress; the Legion's red-and-white shemagh is a magazine essay about the 1950s [carried]. So C08's fit is unverifiable from sources, and rests on the owner's "MATKOT counts as reviewed". (3) A Transjordan flag text exists (Organic Law of 1928, Art. 3, https://mjp.univ-perp.fr/constit/jo1928.htm [carried]) but the token flies the neutral pennant, by Q-ART-1 | `unit-art-sourcing-1948.md` sections 1, 2 and 5 [R]. Claude read, person to verify | **Fits by the owner's ruling, not by a source.** No distinct model is asked for |
| Arab Liberation Army (side `ala`) | C09 `figure-c09-irregular-v2` | Three figures on the `arab_liberation_army` row, al-Malikiyya to 29 May | (1) The mapping rests on one line, "an irregular army" of volunteers (https://en.wikipedia.org/wiki/Arab_Liberation_Army, cited in `UnitFigures.cs:54-57`). (2) The same C09 would be the natural figure for the local Palestinian irregulars, who have no row; `arab-forces-1948.md` section 7 describes them, and its "Conventions" send the sensitive events near these positions to a separate sensitivity review. (3) No ALA flag or emblem is sourced; the dagger-and-star drawing is flagged dubious and must not be drawn ([R] `UnitFlags.cs:28`, sourcing note section 1) | `unit-art-sourcing-1948.md` sections 1, 2 and 5 [R]. Claude read, person to verify | **Fits by decision.** A person should confirm C09 reads as volunteers in mixed surplus dress, not as an invented uniform |
| Egyptian, Syrian, Iraqi and Lebanese forces | None | Data rows only; no token | No figure, and the "no placeholder art" ruling bars borrowing C08, C09 or an IDF figure. Dress for these forces is [S] or [G] in the sourcing note (a wargaming-site comment: "British kit", "French kit") | `unit-art-sourcing-1948.md` section 2 [R]. Claude read, person to verify | **Distinct figures needed**, not asked for yet. They would be new art: MATKOT's would need only the load gate, the licence and provenance rows; anyone else's goes to the owner |
| Local Arab forces (side `local_arab_forces`) | None | No row | No row, no figure, no flag; the sourcing note found none for the force on 15 May 1948 | `unit-art-sourcing-1948.md` section 1 [R] | Out of scope until a row exists |

## 4. The armoured truck: what a person looks for

The sourced features, all [carried] from `unit-art-sourcing-1948.md` section 4 unless marked:
- sides and rear a 38 mm layer of wood between 5 mm and 3 mm steel plates; front a solid 14 mm plate;
- a commercial chassis, most often the Ford F15 15 CWT; others were 1.5 and 3 ton trucks and the Chevrolet G-7117;
- a supply truck that also served as an armoured personnel carrier; the steel-wood-steel build is [W] on Bab al-Wad;
- length, width and height are [G]: a model takes them from the donor chassis.

Two earlier notes in this repository describe the type differently, and neither was reconciled: `war-1948.md` section 8
says "armoured cabin and engine compartment on a commercial truck chassis" and "a carrier and a turreted combat variant"
(`war-1948.md:268` calls the type "our reading" of the diary plus Tank Archives, and its type-level identification, which includes the two variants, is [S]); the sourcing note says sides and rear in sandwich armour and a solid front. A person should read
the Tank Archives page itself.

Photographs the sourcing note lists as references only (no image is in the repository): Latrun museum (PikiWiki 19387,
CC BY 2.5), Batey HaOsef (`Armored-car-batey-haosef-1.jpg`), Sha'ar HaGai remains, and Commons' category "Improvised
armored vehicles of Israel" (about 140 files, none opened one by one [carried]).

## 5. Constraints for any next piece

- **No placeholder art, no IDF figure on an Arab token** (owner, #740). A force with no figure stays off the map.
- **Neutral pennant** (Q-ART-1): a plain grey triangle, in no side's colours, until a sourced emblem is found. The
  sourcing note found flag texts for Transjordan, Iraq, Lebanon (cedar colour [C]) and Syria (a transcription) and
  lists three questions for the owner about them. No answer to those is recorded in the files read. Whether a sourced
  flag should replace the pennant on the Arab Legion token is therefore the owner's call, not this slice's.
- **Asset load gate** (owner, 17 Oct): stream after the first frame, slimmed, no LFS, credited. The slimming limits are
  in `tools/slim_collection.py:46-51` (far mesh at most 4,500 triangles for vehicles, near at most 8,000, 512 px colour
  map) [R].
- **Non-MATKOT unit art goes to the owner for sign-off** before it ships (owner, 17 Oct; #1118's scope limits).
- **Provenance** for any new model: add or update an `ASSETS.yaml` row with its actual source, author and licence.
  The existing `unity/Assets/Art/Collection/*` row describes the named Tal Heuberger Collection assets and their
  provenance; placing a different or newly sourced model in that folder does not establish the same provenance.
- **Never invent** a dress detail, marking or emblem the sources lack.

## 6. Proposed next scope

1. **Record the existing condition and reply (no new art).** The owner's 17 Oct condition and `pixelrenderai-hash`'s
   19 Oct confirmation identify the existing H01 as the armoured sandwich truck; no new model is needed under that condition. Record that existing answer when `docs/units.md` ("The 1948 Collection on the tokens")
   is next updated. Source and mesh comparisons remain separate, as listed below.
2. **Use H01 for the proposed code hook:** no new model. A code PR adds an `armoured_car` vehicle type (`UnitData.VehicleTypes`,
   `UnitFigures.ByVehicle`) and an `art_type` for the sim's `armoured_car`. It adds **no** `units_1948.yaml` row until a
   source puts armoured cars with a named unit from a day.
3. **Keep the remaining comparisons separate.** A person still compares H01 with section 4's sourced features; that
   pending comparison is not an unanswered identity question or a request for a new truck. Any separate new model
   retains the load gate. MATKOT's work needs no owner sign-off; anyone else's does.
4. **A source slice for the armoured car** (research only): go through Commons' "Improvised armored vehicles of Israel"
   category for a dated 1947-49 file whose own description names a sandwich, and find a dated type-level text for a unit.
5. **A person compares C08, C09 and H02 with the photographs** listed in the sourcing note, and records what each model
   shows. Until then their fit rests on the owner's ruling alone.
6. **Leave the other Arab forces off the map** until a dated dress source exists (the note names Osprey Men-at-Arms 128
   as the printed reference, which was not reachable).

## 7. Gates still open

| Gate | Owner of the gate | Status in the files read |
|---|---|---|
| H01 identification | owner condition and tagged contributor confirmation | Owner condition, 17 Oct; `pixelrenderai-hash` confirmation, 19 Oct: H01 is the armoured sandwich truck, so no new truck model is needed under that condition (both links in section 1). Source and mesh comparisons below remain pending |
| Per-model provenance for H01, H02, C08, C09 (prompt, concept image) | Tal / the owner | Kept outside the repository |
| A person verifies every source in this note | a contributor with a browser | Not done; this is an AI draft |
| A person confirms H02, C08, C09 against photographs | a contributor or the owner | Not done; no file records it |
| Sourced flag in place of the pennant (Transjordan, Iraq, Lebanon, Syria) | owner | Three questions in the sourcing note; no answer found |
| Dress sources for the Arab forces | a person with the Osprey plates or a photograph pass | [G] |
| Non-MATKOT unit art | owner sign-off | Required; none offered |
| `-shareable` gate for unit art | owner | Decided 18 Oct: not needed for MATKOT's art; nothing in `Build.cs` checks it |
| Names on screen ("Artillery", "Armoured car") | owner | Decided (Q-UI-2, Q-ARTY-4), plan lines 939 and 960 [R] |

## What a person must check

1. Every URL in this note, and the quotes taken from it. Tank Archives could not be opened from the writing machine; the
   build details are carried from the 2026-10-16 note, which itself marks them single-source.
2. Compare `figure-vehicle-sandwich-convoy-1948-v2-far.fbx` and `-near.fbx` with the sourced features in section 4,
   separately from the tagged contributor's H01 identification under the owner's condition. Claude did not render or inspect any mesh, and cannot judge
   a shape from a texture atlas; this update supplies no such comparison either.
3. Whether the line numbers still hold on `main` when you read this.
4. That "MATKOT" and "Tal Heuberger" are the same author for the owner's ruling (the issues and the docs use different names).
5. The two descriptions of the sandwich type in section 4: which one the Tank Archives page supports.
6. A lead not followed in this note: `research_notes/GDD phase 4/burma-road-photographs.md` (lines 36, 90, 93) describes
   NLI/Rothenberg photographs of June 1948 showing improvised armoured trucks on the Burma Road, one with firing slits.
   It bears on whether the sandwich truck fits a Burma Road convoy; open those photographs before deciding.
