# Chapters architecture: how eras chain without resets

- **Date:** 2026-10-10 (revised after staff review the same day)
- **Status: PROPOSED — owner review needed for the decisions listed in §Decisions**
- **Issue:** #53 (roadmap tracker). Era epics that depend on it: #49 (Yishuv), #50 (1957–1977), #51 (1977–1993), #52 (1993–2026).
- **Parent spec:** [2026-10-09-statecraft-israel-design.md](2026-10-09-statecraft-israel-design.md) (§2 "Eras as chapters, not resets"; §3 saves; §5 content; §7 saves; §8 testing; §9–§11)

This document says how one game runs from one era into the next without a reset, in this codebase. It names the files
and types it changes. It writes no code; the work items are listed in §15 and drafted as separate issues.

**Words used here.** In data, an **era** is one record in `content/eras.yaml`. On screen, the player sees it as a
**chapter**. One era is one chapter. A **boundary** is the date where one era ends and the next begins.

## Decisions

Each needs the owner's yes or no. Everything else in this document follows from them or is a technical choice with a
stated default.

| # | Decision | Recommended default |
|---|---|---|
| D1 | How the game carries across a boundary (§4.3) | **Deviation carry:** the next chapter starts from its sourced start, moved by how far the player's game ended from the previous chapter's historical reference run. This applies to money, population, and the policy levels and indicator shifts the next chapter's calibration reads. Everything else persists as it is. Not absolute carry, not a reset |
| D2 | Can a player start at a later chapter without playing the earlier ones? (§6) | **Yes.** Every chapter has a sourced start and can be started fresh. Playing straight through is the main mode |
| D3 | Chapter dates and the end of the game (§3.1, §4.5) | **Founding = 15 May 1948 to 1 January 1957.** Later boundaries are set in #49–#52. A boundary is a date, and the turn grid is adjusted to land on it. Reaching the last era's end ends the game after the legacy report |
| D4 | Saves across chapters (§7) | **A checkpoint at each boundary.** A save keeps the state at the start of the current chapter, so a content update to an earlier chapter does not change a game that is already past it |
| D5 | Money on screen (§11) | **Display only:** the sim keeps one internal unit for each money account in every era. The UI shows each period's currency, converted by sourced redenomination dates and factors |
| D6 | Browser build: how content is shipped (§9) | **One content pack for all eras, loaded at boot.** Loading one era at a time is built only if a measured IL2CPP WebGL boot time goes over budget |

## 1. The rules this design keeps

1. **No reset.** Choices made in one chapter still matter in the next: money, population, policies, the map,
   standing, and which options were chosen in past events.
2. **Each chapter is calibrated on its own.** A chapter's calibration test does not depend on how well an earlier
   chapter was calibrated (issue #53: "one tolerance set per era"). A looser check covers games that arrive by
   playing through (§12).
3. **Existing goldens do not move.** `GoldenRun.Hash1950`, `GoldenRun.Hash1952` (`sim/Statecraft.Sim/GoldenRun.cs`,
   `docs/determinism.md`) and the browser `SMOKE OK 5 turns` value stay the same through every item in §15. Each item
   says how.
4. **Data decides, code implements.** Eras, dates and which systems are on are content. Code holds one implementation
   of each system and reads the switches.
5. **The content rules apply in every era** (spec §1, §5): Israeli-perspective narration, one sourced value per fact,
   real names, no invented quotes, nothing that rewards an atrocity, the Holocaust reacted to and never played, and
   living people out of release builds until legal review. §13 turns the last two into checks.
6. **Build machinery when content needs it.** Items whose only user would be a second era are written down here and
   deferred until #49 or #50 content exists (§15).

## 2. What exists today (checked against the code)

| Area | Today | Gap for chapters |
|---|---|---|
| Game start | `Simulation.NewGame` starts at `Rules.Start` (`content/rules.yaml`: 1948-05-15) | One start only |
| Game end | No end date: a game steps on until it ends by collapse; `GoldenRun` stops at 1000 turns and saves at 2000 | No end of content |
| Dated content | Events have `from`/`to`; people have dated office terms; legislatures and governments have `from`/`to`; offices have `until` only (`Content.cs`) | Indicators, policies, effects, situations, voter groups and actors have no dates; offices have no `from` |
| Model in state | `EffectsSystem.Init` puts **every** indicator, policy and effect in the state, and `Apply` loops over all of them | Adding any later-era policy or effect to `model.yaml` today **moves both goldens** (new keys in the hashed `pol{}` and `eff{}` maps) |
| Lookups by id | `SetPolicy` checks the id against all of `c.Policies` (`Simulation.cs`); event conditions, effects, `EffectsSystem.Shift` and voter-group weights read `s.Indicators[id]` directly | Once records have windows, an inactive id must be refused or never referenced |
| Rules | One `Rules` object for the whole game; economy values are calibrated to 1950–51 | No per-era values |
| Switches by date | `Offices.Legislature(date)` picks the sitting body; `Legislature.BudgetApproval` sets whether the budget is voted; `Rules.MapActionsFrom` | These already are "systems switched by data"; nothing else is |
| Time counting | `SecuritySystem.Apply` takes whole weeks per turn length (13, 4 or 1) off the ceasefire clock | A shortened turn needs real days |
| State | `GameState` (`sim/Statecraft.Sim/GameState.cs`) has no era field; `Hash()` covers every field | A new field changes the hash text, so it moves the goldens |
| Saves | `SaveGame` schema 1: content hash, seed, mode, command log, end hash; replay from the seed; `MaxTurns = 2000` | No chapter, no checkpoint; one content hash for everything |
| Content hash | `ContentLoader.HashOf` hashes each file's relative path, length and text | Moving a file changes `Content.Hash` (saves then warn once) |
| Content loading | `ContentLoader.Load` reads every file; WebGL fetches every file in `manifest.json` in parallel (`GameInputs.FetchAsync`, `ContentManifest`); the manifest has paths only | One HTTP request per file; itch.io allows 1,000 files per upload and the build has 51 (`docs/m3-release.md`) |
| Validation cost | The office-overlap check compares every pair of terms (`ContentLoader.Validate`) | Quadratic in the number of terms |
| Release gate | `ReleaseGate` checks only that sensitive-tagged events are `sensitivity-reviewed` | No check for living people; no gate on sensitive map data |
| Settlements | `content/map/settlements_1948.yaml` (25 places): `founded` is a year, or `null` with `pre_modern: true`; `settlement_hexes.yaml` is built from it; the Unity map reads the 1948 file by name (`SettlementPlacer.DefaultFileName`) | The dates are not used; no end dates or renames |
| Water | `tools/terrain_bake.py` derives sea and lakes from the modern DEM into one `coast.png` (86 KB) | Modern water only (#16) |
| Money display | `model.yaml money` gives one prefix per account (`IL `, `$`) | One currency for all time |
| Office titles | One `title` per office | Titles that change with time |
| UI text | `unity/Assets/UI/strings.en.yaml`; but `Game.uxml` has "Loading the map of 1948…" | Era words in the UI |

**Measured (2026-10-10, .NET 10, Release, this worktree):** today's 31 event files hold 44,357 bytes (about 1.4 KB per
event); gzip -9 of them together is 12,020 bytes. Loading today's content takes 142 ms including JIT start-up; loading
it plus 3,000 copies of `hula_drainage.yaml` takes 711 ms. The golden run to 1952 (43 turns) plays in 5–10 ms with
either. At 3,000 events the event text would be about 4.3 MB raw and about 1.2 MB gzipped, about half the 8 MB
heightmap. The binding limit is the number of files, not their size, and the sim's per-turn cost is not a problem.
IL2CPP WebGL load time is not measured yet.

## 3. What an era is in data

### 3.1 `content/eras.yaml`

One file lists the eras in order. Eras are contiguous: each era's `from` is the previous era's `to`.

```yaml
eras:
  - id: founding
    name: The Founding           # the chapter name the player sees
    from: 1948-05-15             # = rules.yaml start today
    to: 1957-01-01               # exclusive, like every other window; the last era's `to` ends the game (§4.5)
    systems:                     # switches for code that exists (§5); strict keys
      legislature: state         # state | assembly (a consent body without force of law)
      map_actions: [transit_camp, housing]
    start: start/founding.yaml   # sourced start values for a fresh start (§6); absent for founding (rules.yaml holds them)
    carry: carry/founding.yaml   # how the previous era's state maps into this one (§4); absent for the first era
```

- `id` is the key used by saves, goldens and file folders.
- The loader checks that eras are contiguous and non-overlapping, that every dated content record falls inside the
  eras' range, and that `systems` uses only known keys.
- Today the file has one era, `founding`.

**D3:** the founding era runs from 15 May 1948 (the current start) to 1 January 1957, the spec's "Q2 1948 to
Q4 1956". Suez (October–November 1956) and the withdrawal that followed straddle that date. An event may not span a
boundary (§9), so events after the war fall in the next era. If the owner prefers, the founding era can end after the
withdrawal instead. That date would need a source. The Yishuv era (#49) would end on 15 May 1948. Each later epic sets
its own `to` by the same rule.

### 3.2 Dated content (content windows)

Every content record that can begin or end gets an optional `from`/`to` window, half-open like events. A record
without a window is active in every era. Events, terms and legislatures already follow this rule.

| File | Records that gain `from`/`to` |
|---|---|
| `model.yaml` | indicators, policies, effects, situations, voter groups, actors (money display is dated in `currency.yaml`, §11) |
| `offices.yaml` | `from` (created), next to the existing `until`; plus `successor` (§4.2) and dated `titles` (§11) |
| `aliyah.yaml` | regions (the series is already dated) |
| `rules.yaml` map actions | each action |

Records that are already dated keep their fields. People span eras (one person's office terms can fall in two or
more chapters), so `people.yaml` stays one file.

**The active view.** The loader computes the window segments once: a sorted, immutable array of the dates on which
any record begins or ends, and the active lists for each segment. `Content.At(date)` finds the segment by binary
search. It is not a lazy cache, so tests that run in parallel share nothing mutable. `EffectsSystem`,
`PoliticsSystem`, `HistoricalAI` and the UI presenters read the view instead of the raw lists.

**State follows the windows.** When a turn ends past a record's `from`, the record is added to the state as `Init`
would add it: an indicator at `base` with shift 0, a policy at `default`, an effect's contribution at 0. When the turn
ends past a record's `to`, the record is removed. Records are added and removed in ordinal id order. This is done in
one place, `ContentWindows.Update(c, s, turnStart)`, called in `Simulation.Step` after the date moves.

**Nothing may reach an inactive record:**
- Commands are checked against `Content.At(s.Date)`. `SetPolicy` for a policy outside its window is refused with a
  reason, like an unknown policy today.
- The loader refuses content that could reference an inactive record. Every reference must be to a record that is
  active for the whole window of the record that uses it. That covers event conditions, option effect targets,
  `set_policy`, effect sources and targets, situations' indicators, voter-group weights and policy offices.
- A test for each reference kind, on a fixture.

**Golden safety:** every existing record has no window, so the state of the 1948–52 runs holds the same keys as
today.

### 3.3 Rules per era

`rules.yaml` stays the base. An era may add `rules/<era>.yaml` with only the keys it changes. The loader merges at
the YAML-node level (base mapping, then the era's keys), then reads the result with the same strict `Rules` type, so a
typo in an era file still fails. Systems ask `Content.RulesAt(date)` instead of `Content.Rules`. Rules change only at
boundaries. Nothing inside an era changes the unit of money (§11).

The founding era has no override file, so its rules are exactly today's. The economy values in `rules.yaml` are
calibrated to 1950–51 (`EconomyCalibrationTests`). A later era sets its own in its own file, with its own calibration
test.

**Alternatives considered for era-specific content:**

| Option | How | For | Against |
|---|---|---|---|
| A. Dated records in the existing files (recommended) | `from`/`to` on records, `eras.yaml`, partial `rules/<era>.yaml` | One rule everywhere (windows); people and parties that span eras stay single | `model.yaml` grows long; needs the active view |
| B. A folder per era with full copies | `content/eras/<id>/model.yaml`, `rules.yaml`, ... | Each era is self-contained | Records that span eras are duplicated and drift apart; a policy that lives 1948–1990 is written three times |
| C. Era code modules | A C# class per era registers its systems | Full freedom | Rules move out of data; contributors must write code to add an era |

## 4. Crossing a boundary

### 4.1 The boundary turn

The turn grid does not land on boundary dates by itself. Quarters from 15 May fall on the 15th, and war months and
weeks shift the grid. So the turn that would pass the next era's `from` is adjusted to end exactly on it:

- If the turn would cross the boundary, it ends on the boundary (`next = nextEra.From`).
- If the turn after that would be shorter than 14 days, the current turn is lengthened to the boundary instead. The
  player never gets a turn of a few days with a full turn of attention, events, votes and map actions.
- Systems that count time use the turn's real days for an adjusted turn. The year fraction is days / 365. The
  ceasefire clock takes `days / 7` whole weeks (integer division). Whole turns keep today's fixed values (13, 4 or 1
  weeks; 0.25, 1/12 or 7/365 of a year).
- Attention, the event cap per turn and the map-action cap are per turn as today. The 14-day rule keeps an adjusted
  turn no shorter than the smaller of its normal length and 14 days, and no longer than its normal length plus
  14 days (so a weekly turn stays between 7 and 21 days, a quarterly one at most a quarter plus 14 days).

With these rules, a game that played into a chapter and a game that started fresh at it share the same calendar from
the boundary on, so per-era goldens and event windows line up. **Golden safety:** there is no boundary inside
1948–1957, so no turn of the existing runs changes.

Order inside `Simulation.Step` when a turn ends on a boundary:

1. the normal turn, as today, ending on the boundary date;
2. `ContentWindows.Update` (records that ended are removed, records that begin are added);
3. `EraSystem.Cross` applies the next era's carry rules (§4.2, §4.3) and adds Why lines ("A new chapter begins:
   ...");
4. `Cabinet.ExpireOnGovernmentChange`, `AttentionSystem.Refill` and `EventSystem.Trigger`, as today.

A chapter's end also produces the legacy report (#19). The game then goes on in the same session; there is no new
game.

### 4.2 What happens to each part of `GameState`

| Field | At a boundary | Notes |
|---|---|---|
| `Date`, `Turn`, `RngState`, `Mode` | persist | one RNG stream for the whole game; `EraSystem.Cross` draws nothing from it |
| `Indicators` | recomputed | level = base + effects + shift, as today |
| `IndicatorShifts` | deviation carry for indicators the next era's start lists (additive, clamped so the level stays 0–1); persist for the others | the player's lasting marks on morale, housing and so on |
| `EffectContributions` | persist where the effect continues | dropped for effects whose window ended |
| `Policies` | deviation carry for policies the next era's start lists (additive, clamped 0–1); persist for the others; mapped where the carry file names a successor | a new policy starts at its default |
| `ActiveSituations` | persist where the situation continues | the hysteresis state is kept |
| `Budget` | mapped through office successors, then rescaled to sum to 1 | e.g. a Yishuv institution's line passes to its state ministry |
| `Treasury`, `Debt`, `FxReserves` | deviation carry (additive) | in the sim's internal unit; no currency conversion (D5) |
| `Population` | groups mapped by the carry file, then deviation carry (ratio) | §4.3 |
| `Rung`, `CeasefireWeeksLeft`, `Mobilization` | persist | the ladder is always on; 1948 opens at war |
| `CoalitionStability` | persist | its meaning follows the sitting body (assembly or Knesset) |
| `Attention` | refilled | as every turn |
| `FiredEvents`, `ChosenOptions` | persist | later events may `require` earlier choices |
| `PendingEvents` | persist | events raised on the boundary turn are answered in the new chapter's first turn |
| `Appointments`, `AppointedUnder` | persist | `Cabinet.ExpireOnGovernmentChange` ends them when the government changes, at a boundary or not |
| `Placements`, `MapActionsTurnDate`, `MapActionsThisTurn` | persist | what the player built stays on the map |
| `GameOver` | — | a game that ended does not cross |

**Golden safety:** this needs **no new `GameState` field.** The current era is derived from `Date` and
`eras.yaml`. If a later item does need a field, it is written into `Hash()` only when it differs from its default, so
the existing hash text does not change. That convention is documented in `docs/determinism.md` with the field.

### 4.3 How the game carries (D1)

Each era has a sourced start (§6). It holds treasury, debt, reserves, population by group, budget shares and start
rung on its `from` date, plus the policy levels and indicator shifts its calibration test reads. The question is how
a game that played the previous chapter enters it.

| Option | Rule at the boundary | For | Against |
|---|---|---|---|
| A. Absolute carry | new value = the player's value, mapped | Simplest; no reference data | Every chapter's calibration error adds up: a small 1948 economy error is still there in 2026. A later chapter cannot be calibrated alone |
| **B. Deviation carry (recommended)** | additive: `start + (player − reference)`; ratio (population): `start * (player / reference)`. *Reference* is the previous chapter's historical run at the boundary | A historical playthrough enters the next chapter at its sourced start; a player who did better or worse carries the difference. Each chapter is calibrated alone | Needs a pinned reference file per boundary, regenerated when the earlier chapter changes |
| C. Reset to sourced values | new value = start | Simplest for balance | It is the Civ VII reset the spec rejects (§2) |

**Exact arithmetic.** The carry must be bit-for-bit deterministic and must return the sourced start exactly for the
historical run:
- Ratio carry is computed as `start * (player / reference)`. When `player == reference` the quotient is exactly 1.0,
  so the result is exactly `start`. Additive carry is `start + (player - reference)`, which is exactly `start` when
  the difference is 0.0.
- If a mapped group's reference is zero or absent, that group falls back to additive carry.
- Quantities are processed in ordinal key order. The rebuilt `Population` dictionary is filled in ordinal key order.
  So is every other dictionary the carry rebuilds, because sums over `Population.Values` follow insertion order
  (`GameState.TotalPopulation`).
- `EraSystem.Cross` uses no RNG and no trigonometry; only `+ - * /`, `Math.Min` and `Math.Max`.
- A test at the golden seed asserts bit equality between a historical chain's carried values and the sourced start.

**Reference file.** `content/carry/<era>.reference.yaml` holds the carried quantities of the previous chapter's
golden run (golden seed, Historical mode) on the boundary date. Doubles are written as IEEE bit patterns in hex, as
saves write them. A tool writes the file, and a test regenerates it and compares, as the goldens are checked today. A
change to the earlier chapter that moves its golden also changes this file, in the same commit. The reference is
"the historical path in this game": the sim's own run, not the historical record.

**Rules that keep it sane:**
- The carry file lists only quantities that have a sourced start in the next era. Everything else in §4.2 persists.
- Each carried quantity has a floor and a ceiling in the carry file, so an extreme game cannot enter the next chapter
  with an impossible value (for example negative population). A clamp is reported in the Why panel.
- With another seed, a historical run ends near the reference, not exactly at it. That small difference carries,
  which is wanted: it is the effect of that game's random draws.
- **Explained to the player:** the chapter card lists each carried quantity as "your game: X; the historical path in
  this game: Y; the new chapter starts at Z + (X − Y)". The legacy report (#19) uses the same reference run.
- Later, if additive money carry proves wrong across a large change in the economy's scale (for example into the
  1980s), the carry file can name a scale quantity and carry money as a share of it. This is not needed now.

### 4.4 Worked example: Yishuv institutions become state institutions (1948)

This is how #49 would hand over to the founding chapter without a reset. Office ids and dates here are examples; the
real mapping is #49's research and needs sources.

| Yishuv-era thing | At the 15 May 1948 boundary | Mechanism |
|---|---|---|
| Jewish Agency Executive and Vaad Leumi offices (e.g. the chair of the Jewish Agency Executive) | end; their budget lines and policies pass to state offices (e.g. Prime Minister) | `offices.yaml`: `until` plus `successor: prime_minister`; `carry/founding.yaml` maps budget lines |
| The people who held them | continue in their new offices on the sourced term dates | `people.yaml` terms; nothing moves |
| The Assembly of Representatives / People's Council | gives way to the Provisional State Council | `legislatures.yaml` periods; the era switch `legislature: assembly` or `state` |
| National funds (treasury in the Yishuv) | become the state treasury by deviation carry | `carry/founding.yaml`; the display currency is §11's business |
| Population groups (e.g. by aliyah wave) | map to the founding chapter's groups (all pre-state residents become `veterans`), then ratio carry | `carry/founding.yaml` group map |
| The Haganah as a policy-led force | the IDF is founded inside the chapter (the existing `idf_founding` event, window 22 May to 1 June 1948); mobilisation and rung persist | Defence state persists |
| Settlements the player founded (e.g. tower-and-stockade) | stay on the map and count as settlements for later map actions | `Placements` persist |
| The British Mandate as an AI actor | its window ends; the United Kingdom actor continues | actor `to` date |
| Choices in Yishuv events | stay in `ChosenOptions`; founding events may `require` them | event `requires` across eras |

### 4.5 The end of the last era

Reaching the last era's `to` ends the game. The turn that reaches it is adjusted as in §4.1. The legacy report (#19)
is shown, then `GameOver` is set to the stable code `era_ended`, which the UI shows as "The chapter has ended" (as #53
ruled: codes in state, text in the strings files). Commands after that are
refused as for any ended game. Today the last era is `founding`, so a game ends on 1 January 1957. When the next era
is added, the same date becomes a boundary instead. **Golden safety:** the golden runs end in 1950 and 1952.

## 5. Era-specific systems

Systems are switched on and off by data, in `eras.yaml systems`, and by dated records. Code implements each system
once.

| Option | For | Against |
|---|---|---|
| **A. Switches in `eras.yaml` for code that exists (recommended)** | Readable; tested once per switch; contributors can see what an era uses | A new kind of switch still needs code |
| B. Date checks in code (`if date < 1948-05-15`) | Fast to write | Dates hide in code; already avoided today (`Rules.MapActionsFrom` is data) |
| C. Era plug-in classes | Any rule possible | Moves rules out of data; harder to test and review |

Rules for switches:
- A key is added to `systems` only in the same change that adds the code behind it, with a test for both values.
  The loader refuses unknown keys, as it does everywhere.
- What is already dated stays dated: the sitting legislature, `budget_approval`, office existence, map-action dates.
  A switch is for a whole-era difference. Examples: no state (no tax law, no army); a consent assembly instead of a
  parliament; an outside ruler that sets immigration quotas (the Mandate's certificates); whether war mode is
  available.
- The budget-approval question (#3) stays a field on each legislature record, which is already era-keyed.

## 6. Starting at a chapter (D2)

`Simulation.NewGame(content, seed, mode, era)`. The existing three-argument form means `founding`, so the goldens and
all existing callers keep their behaviour.

A fresh start:
- sets `Date` to the era's `from`, `Turn` to 0 and `RngState` to the seed;
- reads the sourced start values from `start/<era>.yaml`. For the founding era these are today's `rules.yaml` keys
  (`start`, `start_rung`, `treasury_start`, `fx_start`, `initial_population`, `budget_start`), which stay where they
  are, so nothing moves;
- marks as resolved, with their historical option, exactly the earlier-era events that this era's content `requires`.
  A later chapter's opening therefore does not depend on unrelated edits to earlier chapters. With one pack for all
  eras (D6) the earlier events are loaded, so their historical options can be read directly. If per-era loading is
  ever built, a tool writes the `(event, historical option)` pairs into `start/<era>.yaml` instead, checked by a test.

**Invariant, tested:** a historical playthrough of chapter N, carried into N+1, equals a fresh start of N+1 bit for
bit on every carried quantity (§4.3). The turn number, RNG state and event history differ, which is expected.

## 7. Saves across chapters (D4)

Today a save is the seed and every command from the start. Loading replays all of it; a content change warns and may
make the game diverge (`GameSession.Load`). Replay time is not the issue (43 turns play in 5–10 ms). The issue is
content updates. Over a long release history, earlier chapters will be patched, and every patch would change the
replay of every game already past them.

| Option | For | Against |
|---|---|---|
| A. Replay from the start, as today | One source of truth; no new format | Any patch to 1948 content changes every later game on load (the warning fires and the game drifts) |
| **B. A checkpoint at each boundary (recommended)** | Earlier chapters are frozen; load replays only the current chapter | Needs a lossless state text, and the checkpoint is hostile input that must be validated |

**Schema 2** (`SaveGame.SchemaVersion = 2`):

```
statecraft-save 2
chapter founding                 era the game started in
seed 1948
mode Historical
content <hash>                   Content.Hash of the content it was played with, as today
checkpoint <era-id> <hash>       optional: the state on the boundary into that era; only the latest is kept
state <key> <value>              ... lossless state lines (doubles as IEEE bit patterns, as today)
turn                             the current chapter's turns, as today
<command> ...
end <hash>
```

- **Lossless state text.** `GameState` gains `Write`/`Read`. Dictionaries are written **in insertion order**, not in
  the hash's sorted order. `TotalPopulation` sums `Population.Values` in insertion order, so a restored state with
  another order can differ in the last bit and diverge later. (Making every dictionary reduction iterate in ordinal
  key order would remove the risk. It is a deliberate rebaseline, done separately if ever.)
- **Test:** for every turn of both golden runs, restore from `Write`, then play **to the end** and compare the final
  hash with the uninterrupted run.
- **A checkpoint is hostile input.** Today a save cannot contain an impossible state, because it is replayed. A
  checkpoint's lines are parsed directly, and its hash is the save's own, so after `Read` the loader validates:
  - every id exists and is active on the checkpoint date;
  - every number is finite and inside the rules' ranges;
  - placements are on land hexes;
  - pending events exist in the content.

  It then reconciles with `ContentWindows` (adds records the current content has, drops ones it does not) and warns if
  that changed the hash. Each rejection has a test, as in `SaveGameTests`.
- **Loading.** With a checkpoint: read, validate and reconcile it, then replay the turns after it. Without one: replay
  from the seed, as today.
- **History.** Checkpoints drop earlier chapters' command logs. Anything that needs whole-game history (a legacy
  report across chapters, a timeline) must be summarised into the checkpoint.
- **Migration.** Schema 1 saves load as `chapter founding` with no checkpoint. A test keeps a schema 1 save fixture and
  checks it loads to the same hash. Older builds refuse a schema 2 save with the existing "newer than this game"
  message. Re-run the save check across two itch.io uploads (#43) after this lands.
- **Length.** `MaxTurns` applies per chapter, not per game.
- **Golden safety:** the save format is not hashed; state hashes do not change.

## 8. Determinism and goldens

The existing method stays (`docs/determinism.md`): SplitMix64, no trigonometry, a canonical hash, goldens pinned in
three places.

**Golden-safe checklist** (each item in §15 shows it holds):
1. Records without windows behave as today; new records have windows outside 1948–1952 or no state effect there.
2. No era override file for `founding`.
3. No boundary and no era end inside the existing runs.
4. No new hashed state text unless it is omitted at its default.
5. Nothing in the sim depends on the order of `Content.Events` (`EventSystem.Trigger` sorts by `from`, then id). A
   test checks that results do not depend on how content is split into files or packs (§9).

**New goldens per era.** `GoldenRun.PlayChapter(content, era, until)` plays a fresh start of an era with the golden
seed and `HistoricalAI`. Each era pins at least one constant, named by era and end year (for example
`Hash_<era>_1960`), in the same three places. The Unity golden check prints one `GOLDEN OK` line per pinned run. A
**chain golden** pins a run that crosses each boundary. The founding era's goldens are today's `Hash1950` and
`Hash1952`, unchanged.

**Rebaselines stay local.** A fresh start reads sourced values. So a change to the 1950 events moves only the founding
goldens, the chain goldens and the carry reference file for the next era. The next era's fresh-start golden stays.

## 9. Content at scale (D6)

The spec's full span could hold thousands of events and hundreds of people. The limits are in the browser:

| Limit | Today | At 3,000 events | Design answer |
|---|---|---|---|
| Files per upload (itch.io) | 51 files | over 3,000 | one content pack file |
| HTTP requests at boot | one per content file | over 3,000 | one request for the pack |
| Event text | 44 KB raw, 12 KB gzip (31 events) | about 4.3 MB raw, about 1.2 MB gzip | one gzipped pack |
| Load time (.NET) | 142 ms | 711 ms | measure IL2CPP WebGL before building anything more |
| Office-overlap check | pairwise over all terms | quadratic | sort terms by office and compare neighbours |
| Portraits | 30 in the build, 628 KB | hundreds | a follow-up with #30 and #44 |

**Layout.** Events move to `content/events/<era>/`; today's 31 files would go to `content/events/founding/`. The
loader checks that an event's window lies inside its folder's era. An event may not span a boundary; it is split.
`ContentLoader.Load(dir)` and `ContentManifest.Collect` become recursive under `events/`. **The move changes
`Content.Hash`**, because `HashOf` hashes each file's path. Every existing save then shows the "different game
content" warning once and replays the same game, since the move changes no state. Do the move in one quiet commit
(no event PRs open, as #5 and #28 are writing event files), with a `CHANGELOG.md` line that explains the one-time
warning. Keeping the old paths in the hash would hide a real difference, so the hash is not patched around.

**The pack.** The Unity build step (`ContentSync`) writes one `content.pack.gz`. It holds every content file, each
with its relative path and length, in the framing `ContentLoader.HashOf` already uses, and is gzip-compressed at build
time. StreamingAssets are served as is, outside Unity's Brotli (`docs/m3-release.md`), so the build must compress the
pack itself. The client decompresses with `GZipStream`, which must be verified in an IL2CPP WebGL build. The manifest
gives the pack's file name, size and SHA-256. The client checks both after the fetch, so a cached or truncated pack
fails with a clear message instead of a parse error mid-game. `Content.Hash` stays defined over logical files, so
packing does not change it.

**Loading.**

| Option | For | Against |
|---|---|---|
| **A. One pack for all eras, loaded at boot (recommended)** | One request; one code path; a fresh start or a replay can see any era; `Content` never changes during a session | Boot parse time grows with every chapter |
| B. Core + current era, next era prefetched | Boot cost stays about one chapter's worth | `Content` grows during a session; fresh starts and replays need data from packs that are not loaded; more to test |
| C. Precompiled binary content | Fastest parse | A second format to keep in step with YAML |

Option B is built only if a measured IL2CPP WebGL boot time with a 3,000-event synthetic pack goes over the boot
budget in `docs/visual-v1.md`.

**Test, for any packing:** the golden runs and a two-era fixture chain give the same hash on every turn whether
content is read from files, from one pack, or split into several packs.

## 10. Map eras

One heightmap (the modern Copernicus DEM, 8 MB) serves every era. What changes by date is drawn on top of it.

| Layer | Data | Change |
|---|---|---|
| Settlements | One `content/map/settlements.yaml` replacing `settlements_1948.yaml`: `founded` as a date or a year (a year alone means 1 January), or `null` with `pre_modern: true` (exists on every date); optional `until` (abandoned, depopulated or merged) and dated `names` for renames | Unity places what exists on the current date; `MapActions.Check` counts only settlements that exist on the turn's date; `tools/settlement_hexes.py` hexes every row. This answers #27's "agree the file shape first". `until` and renames are sensitive (§13) |
| Water | dated water bodies, levels and sources; one `coast` mask per water epoch (86 KB each) | Built under #16, which already proposes the data file and the bake. Water is **visual only**: `MapActions.Check` keeps using the static `Land` flag in `hexes.json`, so the sim does not change. Dated land flags would be hashed and are left out until a rule needs them |
| Lines | `content/map/lines.yaml`: dated polylines (the 1949 armistice lines, later lines), with Israeli-perspective names and sources | Drawn as overlays; hex cells keep no political meaning (spec §3). Sensitive (§13) |

| Option for water | For | Against |
|---|---|---|
| **A. One heightmap + dated masks (recommended)** | Small (a mask per epoch); terrain identical in every era | Water under a raised level is drawn flat over today's relief |
| B. A heightmap per era | Exact relief | 8 MB each; the download budget (#44) cannot carry it |
| C. Water polygons drawn at runtime | No bake step | A second water renderer next to the shader's coast mask |

The map changes on its own dates, not only at boundaries. #16 dates the Hula drainage 1951–1958, inside the founding
chapter.

## 11. UI per era (D5)

| Item | Change |
|---|---|
| Office titles | `offices.yaml titles: [{ from, title }]` next to `title`; `PlayerText.OfficeTitle(c, id, date)` |
| Money | `content/currency.yaml`: dated currencies, each with a display prefix, a cumulative factor from the sim's internal unit, and sources. `PlayerText.Money(c, id, amount, date)` divides by the factor and adds the prefix. **The sim never converts**: its stored amounts, rules and hashes are in one unit forever. Which currencies and which dates (the pound/lira, the shekel, the new shekel) is content to source. Inflation is a calibration question for #51, not a currency mechanism |
| Units | Land in dunams and population in people stay; amounts stay in millions; the reserves stay in US dollars |
| UI strings | `strings.en.yaml` keys may have era variants, `key@<era>`, used when present; the loading line in `Game.uxml` becomes a string with the chapter's name |
| Chapter name | shown in the top bar, and on a chapter card at each boundary next to the legacy report (#19); the card explains the carry (§4.3) |

| Option for money (D5) | For | Against |
|---|---|---|
| **A. Display-only conversion (recommended)** | Numbers read like the period's currency; no jump in hashed state; no per-date rules | The internal unit is an abstraction the player never sees |
| B. Convert stored amounts on redenomination dates | The internal numbers match the sources | Redenominations in 1980 and 1985 fall inside #51's era. Every money-valued rule would be off by 10 and then 10,000 times, unless rules changed by date inside an era |
| C. One currency shown throughout (e.g. 1948 pounds) | No conversion at all | Numbers stop matching any source after 1980 |

## 12. Testing

| Test | What it checks |
|---|---|
| `DeterminismTests` | today's `Hash1950` and `Hash1952`, unchanged through every item in §15; plus each era's fresh-start golden and the chain goldens |
| Carry reference test | regenerates `carry/<era>.reference.yaml` from the previous era's golden run and compares |
| Carry invariant | a historical chain into era N+1 equals a fresh start of N+1, bit for bit, on the carried quantities |
| Era content tests | eras contiguous; every dated record inside the eras' range; event windows inside their folder's era; references only to active records (§3.2); `requires` only backwards in time; strict `systems` keys |
| Window tests | a record that begins or ends mid-game enters and leaves the state as `Init` would add it; `Content.At` segments are computed once at load |
| Boundary tests | on a two-era fixture built with the existing `Fixture.cs` pattern: the adjusted turn and the 14-day rule, the ceasefire clock by days, successor mapping, group mapping, clamps, the zero-reference fallback, Why lines |
| End-of-game test | reaching the last era's `to` ends the game with the player-facing reason |
| Packing test | the same hashes whether content comes from files, one pack or several (§9) |
| Save tests | schema 1 fixture still loads; schema 2 round-trips from every golden turn to the end; hostile checkpoints are refused |
| `SliceSmokeTests` per era | the existing smoke, parameterised by era: a fresh start through the era, and a chain across each boundary, each under a minute |
| Calibration per era | one tolerance set per era (`<Era>CalibrationTests`), as `EconomyCalibrationTests` is for 1950–51, run on a fresh start; plus a looser check on a historical chain into the era, so players who play through also see a calibrated chapter |
| Release gates | §13 |
| Unity | `SimSmokeTests` and `web_check.py` print one `GOLDEN OK` line per pinned run |

## 13. Perspective and review across eras

The rules do not change by era, but more content is sensitive. Today's `ReleaseGate` checks only sensitive-tagged
events. Chapters add these gates, enforced by `ReleaseGate`, `Statecraft.Cli --release-gate`, the `-shareable` build
and `tools/package_release.py`:

- **Living people.** A person with `living: true` blocks a shareable build until a legal review is recorded on the
  record (spec §5). This is new code: no such check exists today. It matters most for #52.
- **Sensitive map data.** A settlement row with `until` (for example Arab towns and villages depopulated in 1948;
  `type: arab_town` rows already exist) or a rename, and every `lines.yaml` entry, needs `status: sensitivity-reviewed`
  before a shareable build. The rows are submitted as `fact-checked`; the owner sets the reviewed status, as for
  events.
- **Holocaust content** in #49 is an event the player reacts to, never a playable scenario. The existing tag rules
  cover it.

## 14. Out of scope

- The content of any era (#49–#52) and their dates beyond the founding era.
- Diplomacy state and actor standing in `GameState` (#11). When it lands, it follows §4.2 (persist; actors by window).
- Open mode (#10); nothing here depends on the mode.
- Hebrew content (#55); era variants of strings would follow #12's keys.
  **Then decided (owner, 18 Oct 2026, "all defaults"; [#55](https://github.com/eyalgolan/statecraft-israel/issues/55#issuecomment-6249502081)):** yes, after the MVP: Hebrew versions of the English story texts, event titles and party names first.
- Coastline changes from ports and land reclamation.
- Water that changes what the sim allows (§10).

## 15. Work items

The first group has users now (the founding era, #27, release builds) and is built in order. The second group is
built when the first second-era content lands (#49 or #50); until then its issues are marked deferred.

**Build now**

| # | Item | Needs |
|---|---|---|
| 1 | `eras.yaml`, the era lookup and the end of the last era | — |
| 2 | Content windows for the model and offices, with reference checks | 1 |
| 3 | Rules and system switches per era | 1 |
| 4 | Boundary crossing: the adjusted turn and carry rules | 2, 3, D1 |
| 5 | Fresh chapter starts and per-era goldens | 4, D2 |
| 6 | Dated settlements on the map | 1 |
| 7 | Release gates: living people and sensitive map data | — (map rows from 6, 8) |
| 8 | Dated lines on the map (`lines.yaml`) | 7 |

Water epochs are not a separate item: they are folded into #16 (dated water bodies, one mask per epoch, visual only).

**Deferred: build when #49 or #50 content exists**

| # | Item | Needs |
|---|---|---|
| 9 | Save schema 2 with chapter checkpoints | 5, D4 |
| 10 | Event folders per era and one content pack; measure IL2CPP boot time | 1, D6 |
| 11 | Era-aware UI text, office titles and display currency | 1, 3, D5 |
| 12 | Per-era smoke and chain calibration | 5 |

Items 1–5 unblock #49: with them, a Yishuv era can be added as data and chained into the founding era.
