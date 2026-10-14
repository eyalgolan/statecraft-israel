# Statecraft: Israel: design spec

- **Date:** 2026-10-09
- **Status:** awaiting owner review
- **Working title:** Statecraft: Israel (placeholder)

## 1. Intent

**Requested by the owner:**
- A top-notch government simulation of running the Yishuv and the State of Israel from 1882 to today.
- It covers every domain: education, health, infrastructure, legal, politics, security and defence, tourism, foreign relations, economy, and aliyah.
- Its look is "Civilization VII-like".
- It includes the wars, the Holocaust, terror attacks, the British Mandate, disasters, the Cold War, relations with the Arab world, the Palestinians and world powers, and the aliyah waves.

**Purpose:**
- Build it with AI (Claude agents) toward a commercial English-language Steam release.
- Produce something early that convinces people to join the project.

**Success, for this spec:**
- A playable **1948–1956 "Founding" vertical slice**.
- It is reachable by a **browser link** for recruiting, if milestone M0 confirms that is possible.
- It is built on an architecture that can grow to the full 1882–2026 span.

**Owner decisions (2026-10-09):**

| Topic | Decision |
|---|---|
| Purpose | Recruit collaborators, then build an indie Steam release with AI |
| Language | English only for the first builds |
| Perspective | The player leads the Yishuv / Israel. **The narrative is Israeli-perspective only.** Historians are mainstream pro-Israeli. Each fact gets one sourced value; contested ranges and opposing-narrative panels are not shown. Arab and Palestinian actors remain AI actors with their own goals, but all narration and terminology use Israeli framing |
| Real people | **Real names and likenesses** of historical figures |
| Engine | Unity 6.3 LTS (URP), with the simulation as a pure C# library (approach A) |
| First slice | 1948–1956 Founding |
| Security | Israel is never at peace. Security is a permanent escalation ladder, not a war/peace switch |

**Kept regardless of perspective,** because these protect the game rather than express a viewpoint:
- every fact is sourced;
- no quotes are invented and attributed to real people;
- choices never reward atrocities;
- the Holocaust is handled respectfully, as an event the player reacts to and never one the player plays.

## 2. Research basis

The full report is `reports/Israel nation sim game build.md`, with notes in `research_notes/Israel nation sim game build/` (seven threads). The conclusions this spec depends on:
- **Eras as chapters, not resets.** Civ VII's Ages reset was poorly received; its Steam rating was about 48% positive before the May 2026 "Test of Time" update.
- **A Democracy 4-style effects web works for the ministries layer.** It runs as CSV-defined policies, situations and voter groups, with inertia and hysteresis.
- **Engine ranking:** Unity 6.3 LTS ranks first, ahead of Godot 4.6, a web stack, and Unreal.
  - Unity 6.3 LTS was released 4 Dec 2025 and is supported to December 2027.
  - Unity's official MCP server has been in open beta since May 2026. It needs Unity Cloud and a subscription or trial.
- **Civ VII's look is "readable realism"** (diorama / model railroad). Its interface is HTML/CSS on Coherent Gameface.
- **Licences:**
  - The CBS Open License allows commercial use with attribution.
  - Copernicus GLO-30 elevation data is free and requires attribution.
  - Eleven Music: the owner confirmed on 2026-10-13 that the ElevenLabs Music the project uses is licensed for commercial use (decision D-M in `docs/oss/DECISIONS.md`). This replaces the earlier note that it was not licensed for commercial games on self-serve plans.
- **Steam rules:**
  - Steam requires disclosure of AI-generated content, but coding assistants are exempt.
  - Takedowns followed glorified violence or government requests, not a game's point of view.
  - Expect reception risk such as review-bombing.

## 3. Architecture

```
israel-game/
  sim/        Pure C# library, no Unity references. All rules. xUnit tests via `dotnet test`.
  content/    YAML data: people, parties, eras, actors, policies, voter groups, events.
  tools/      Python: DEM -> heightmap, CBS/election import -> content, portrait fetch.
  unity/      Unity 6.3 URP project. Renders snapshots, sends commands. No game rules.
  promo/      Concept art pipeline (exists).
```

**Data flow:**
1. The UI sends commands (`SetPolicy`, `AllocateBudget`, `Appoint`, `ChooseEventOption`, `SecurityOrder`, `MapAction`, and so on) to `Sim.Step(state, commands, rng)`.
2. The sim returns a new state, which Unity renders as a snapshot.
3. All randomness comes from a seeded RNG.
4. **A save is:** seed + command log + content hash + schema version.

**Turn length:**
- One quarter by default.
- One month on security rung 4.
- One week on rung 5.
- The slice runs Q2 1948 to Q4 1956, which is **35 quarters** plus any war-time turns.

### Simulation systems

Each system is a separate unit with one job.

1. **Effects web.** Policies, indicators, situations and voter groups. Each effect has a formula, operator and inertia. Situations switch on and off at different thresholds so they don't flicker. Ministers' competence scales how well policies work.
2. **Legislature and coalition.** Which body legislates is set per era: the Provisional State Council (37 members) in 1948–49, then the Knesset (120). Parties have real seat results. Coalitions have demands, crises and early elections.
3. **Ministers and appointments.** Real people from `people.yaml`, each with competence, loyalty and traits.
4. **Budget and economy.**
   - Treasury, debt and **foreign currency reserves**. A real early-state crisis: exports covered under a third of imports, and creditors refused credit in late 1951.
   - Aid and reparations.
   - Budget approval is a per-era data field.
5. **Population and aliyah.**
   - Groups by country of origin, transit camps and housing.
   - Calibrated to the CBS annual series. About 690k people arrived in 1948–51, including about 120k from Iraq.
6. **Events.**
   - **Hard anchors:** historical events that happen in the "Historical" mode.
   - **Soft goals.**
   - **Emergent crises:** fired by thresholds in the simulation.
   - Some events open full decision scenes.
7. **Foreign and other actors.** These are AI actors with goals:
   - the US, USSR, France, UK and West Germany;
   - the UN (armistice commissions and ceasefire clocks);
   - Egypt, Jordan, Syria, Lebanon and Iraq;
   - the Arab refugees, and Arab citizens under military rule.

   That makes **13 actors**.

   Each has a standing meter with Israel.
8. **Map state.** Each hex tile holds settlements, population, land use and infrastructure. The map's hex boundaries carry no political meaning.
9. **Defence and the IDF.**
   - **Command:** the Defence Minister, Chief of Staff, intelligence chief and branch commanders are real people with a doctrine stance (restraint or activist), competence and ambition. Friction between the government and the army is possible.
   - **Forces:** regular army, reserves, air force and navy, each with readiness, equipment, training and morale. **Keeping reserves mobilised drains the economy and the labour force every turn.**
   - **Manpower policy:** who serves, for how long, and who is exempt. This feeds coalition politics and society.
   - **Procurement:** supplier relationships and embargoes, the domestic defence industry, and long-running secret projects.
   - **Intelligence:** the player sees an *estimate* that carries uncertainty and can be wrong.
   - **Border security:** policy on infiltration, retaliation and border settlements, with diplomatic, civilian and deterrence consequences.
   - **War mode** (rung 5):
     - Operational-level campaigns are fought on the hex map; there is no tactical combat.
     - Outcomes depend on force balance, intelligence, doctrine and morale.
     - Superpower pressure and a **UN ceasefire clock** set limits.
   - **Delegation:** the player can set doctrine and hand day-to-day security to the Chief of Staff, or take decisions personally.

### Security escalation ladder

This ladder is always active.

| Rung | Meaning | Turn length |
|---|---|---|
| 1 | Routine threat (border incidents, deterrence upkeep) | quarter |
| 2 | Wave (terror or infiltration, hostile buildup) | quarter |
| 3 | Limited operation (raids, reprisals) | quarter, plus a decision scene |
| 4 | Prolonged conflict (attrition, long call-ups) | month |
| 5 | Full war | week |

**Design rule:** every rung costs something civilian (economy, budget, morale, emigration, international standing, coalition). All ministries keep running during war. For example, in 1948–49 the player fights the war *and* absorbs mass immigration in the same turns.

## 4. Gameplay loop

Each turn runs in six steps:
1. **Briefing.** Reports arrive from ministries, intelligence and the coalition whip. Only the 3–5 most important items reach the PM's desk; the rest go to an inbox. Reviewers criticised Victoria 3's notification pile-up.
2. **Cabinet agenda.** The PM has a limited **Attention** budget per turn. This is our own design, not borrowed from another game.
   - Issues the PM doesn't handle personally are **delegated**, and the outcome depends on the minister's competence and loyalty.
   - Attention costs rise with the security rung.
3. **Actions:**
   - policies;
   - budget shifts, with the annual budget needing the legislature's approval;
   - appointments;
   - legislation, voted by real parties under coalition discipline;
   - diplomacy;
   - security orders;
   - map actions: where transit camps, new towns, kibbutzim, housing, water and roads go.
4. **Events.** 0–3 per turn.
5. **Resolution.** Effects are applied with their delays and immigration arrives. The economy and foreign currency reserves update. Actors act, the security ladder updates, coalition stability is checked, and elections are held when due.
6. **"Why did this happen?" report.** It shows the causal chain behind each change.

**Meters:**
- treasury, debt and foreign currency reserves;
- support from each voter group;
- coalition stability;
- standing with each actor;
- security rung;
- Attention;
- Historical Track (how far the game has drifted from real history).

**End of a chapter:**
- There is no "win" screen. Instead there is a **legacy report** that scores security, prosperity, democracy and rule of law, absorption, international standing, and social cohesion, each compared with what really happened.
- **Failure:**
  - Losing the premiership through an election or coalition collapse ends the playthrough.
  - Military or economic collapse ends the state.

**Modes:**
- **Historical:** major anchors happen, and the player shapes how they unfold.
- **Open:** anchors depend on conditions. For example, Suez only happens if the French alliance and Egypt's buildup lead there.

**Slice content budget:**

| Item | Amount |
|---|---|
| Turns | 35 quarters, plus war turns |
| Events | about 40 |
| Full decision scenes | about 6 |
| Policies | about 60 |
| Voter groups | about 15 |
| Actors | 13 |

**Real anchors in the slice:**
- the 1948 war (the slice opens inside it);
- the elections of **25 Jan 1949**, **30 Jul 1951** and **26 Jul 1955**;
- mass immigration and transit camps;
- austerity, with rationing until 1959;
- the reparations question: the Knesset debate of January 1952, whose decision passed 61–50; the agreement was signed 10 Sep 1952 and took effect 27 Mar 1953;
- the Czech–Egyptian arms deal, announced 27 Sep 1955;
- Suez, 1956.

## 5. Content and historical-accuracy pipeline

1. **Facts are structured data.**
   - Dates, numbers and names live in typed fields in `content/*.yaml`.
   - Event text uses placeholders (`{defense_minister}`, `{arrivals_1950}`), so prose cannot drift from its sources.
2. **Every fact field carries `sources:`** (citation or URL plus the date it was checked) and a review `status`:
   - `draft` → `fact-checked` → `historian-reviewed`, then `sensitivity-reviewed` for sensitive tags.
   - **Release builds include only fully reviewed content.**
   - Dev builds show drafts with a DRAFT stamp.
   - Each fact has **one value**, taken from Israeli official or mainstream sources (CBS, IDF, Ministry of Foreign Affairs, State Archives, mainstream Israeli historiography).
3. **People are data.** `people.yaml` holds:
   - names in English and Hebrew;
   - birth and death dates, and a `living` flag;
   - **offices with sourced start and end dates**;
   - a portrait record (file, licence, source URL);
   - traits.

   Events ask the simulation "who held this office on this date" and never hard-code a name. People with `living: true` are excluded from release builds until legal review is recorded. Grounds:
   - Israel's right of publicity, under Privacy Protection Law §2(6);
   - a ruling that the right survives death for at least 25 years;
   - the 1949 Flag and Emblem Law, which requires a permit for commercial use of the state emblem.
4. **AI-assisted writing:**
   - A drafting agent writes **only from a provided source packet**, and outputs the text plus a list of the factual claims it makes.
   - A separate verifier agent checks each claim against the packet. It sees no drafter context, and any mismatch blocks the event.
   - A pro-Israeli historian advisory board does the final review.
   - **No quotes are attributed to real people** unless they are sourced verbatim.
5. **Sensitivity rules** apply to events tagged `holocaust`, `terror`, `massacre` or `war-casualties`:
   - They cannot grant treasury or score rewards for the atrocity side of a choice.
   - They require a recorded sensitivity review.
   - Holocaust content contains no swastikas. This matters for Germany's USK rating, which judges such symbols case by case.
6. **Geography and portraits** reuse the pipelines proven in `promo/`:
   - The map comes from Copernicus elevation data, with attribution.
   - Settlement coordinates and founding dates are sourced for each era.
   - Portraits use **public-domain or CC0 photos only**. The licence is recorded automatically, a human checks the likeness, and share-alike sources are rejected. *Amended: CC BY 4.0 is accepted (owner ruling on #30, 13 October 2026), and so is CC BY-SA 4.0 (owner, 14 October 2026, `docs/oss/DECISIONS.md` D-P), whose crops are themselves CC BY-SA 4.0.*
7. **Automated content tests** run in `dotnet test` and check that:
   - every file matches its schema;
   - every reference resolves;
   - every person referenced held the office on the event's date;
   - sensitive events carry their required reviews;
   - there are no reward effects on atrocity-tagged options;
   - the **calibration run** (the historical AI making the real choices) stays within tolerances of the CBS series. The tolerances are set in the plan.
8. **Steam AI disclosure:** an asset manifest marks which assets are AI-generated, in the same way as `promo/CREDITS.md`.

**Open fact to verify:** how the budget was approved in 1948–56. Knesset approval is confirmed under today's Basic Law: The State Economy, and in 1962. It is unconfirmed for the 1950s. This is a required content check against the Knesset archive and the official law gazette (*Sefer HaHukim*). No design decision depends on it.

## 6. Presentation (Unity)

- **`SimBridge`** holds the sim, sends commands and publishes immutable snapshots.
- **`MapRenderer`:**
  - draws the heightmap terrain baked by `tools/dem_to_heightmap.py`;
  - overlays the hex grid and blends biomes, following Catlike Coding's Hex Map techniques;
  - places settlements as instanced prefabs chosen by tile state and era.
- **Art lane:** stylised realism, with no low-poly packs mixed in.
- **`CameraRig`:** diorama pitch, zoom levels and a tilt-shift look.
- **UI Toolkit screens:**
  - HUD, Cabinet, Budget, Defence, Diplomacy, and Event card / decision scene;
  - the "Why did this happen?" panel and the legacy report.
- **Visual targets** are the approved concept images in `promo/final/` (01–08).

## 7. Error handling

- **Illegal commands** are rejected with a stated reason, which the UI shows.
- **Content errors:**
  - In dev builds, loading fails immediately with the file and field.
  - Release builds cannot contain content errors, because the tests gate them.
- **Saves:**
  - Saves are versioned and include the schema version, with migrations between versions.
  - A content-hash mismatch on load shows a warning.
  - The game autosaves every turn.

## 8. Testing

1. **Sim:** unit tests per system, plus **determinism**: the same seed and commands must produce an identical state hash.
2. **Content:** the tests in section 5.7.
3. **Calibration:** a historical-AI playthrough checked against the CBS series.
4. **Unity:** edit-mode tests for the bridge and UI bindings, and a headless smoke test that plays the full slice.
5. **Screens:** screenshot comparison against approved images.

## 9. Milestones

Each milestone after M0 produces something to show recruits.

| # | Milestone | Done when |
|---|---|---|
| M0 | Spike (throwaway) | Each of these is answered yes or no, with evidence: (a) a URP **browser build** runs our map test; (b) **Unity MCP on the Personal plan** works with Claude Code; (c) a plain C# library is usable from Unity; (d) UI Toolkit can build a Cabinet-complexity panel with data binding; (e) URP can produce the tilt-shift look |
| M1 | Headless sim and 1948–49 content | A text playthrough of 1948–49 runs in the terminal, covering the effects web, legislature and coalition, ministers, budget, population, the Defence ladder and real people data. Determinism and content tests pass |
| M2 | The map | A real-terrain hex map of Israel with the diorama camera and 1948 settlements. A recorded flythrough for recruiting |
| M3 | Playable 1948–51 | HUD, Cabinet, Event and Defence screens, delivered as a browser link (or a download if M0(a) fails) |
| M4 | Full 1948–56 slice | All slice content, war mode (1948 and Suez), the legacy report and historian review |
| M5 | Polish and Steam page | A wishlist page with the AI disclosure generated from the manifest |

**Fallbacks decided by M0:**
- If (a) fails, M3 ships as a download.
- If (b) fails, we develop without MCP, using scripts and the CLI.

**Plan scope:** the first implementation plan covers **M0–M2 in full detail**. M3–M5 each get their own plan.

## 10. Risks

| Risk | Mitigation |
|---|---|
| Reception and review-bombing because of the one-sided narrative | Accepted by the owner. Every fact is sourced and nothing is invented, so criticism of accuracy has no ground |
| Defence overwhelms the civilian side | Every rung carries civilian costs, ministries keep running in war, and the player can delegate |
| The content review queue stalls | Only release builds are gated; drafts remain playable in dev |
| Image models invent facts (seen in promo) | Reference maps and photos, deterministic patching, and human review of every published image |
| Shared ElevenLabs credits | `promo/gen.py` keeps a 50,000-credit floor reserved for the drowning-video project |
| Living people in later chapters | Excluded from release builds until legal review |

## 11. Out of scope for this spec

- Eras other than 1948–56.
- Hebrew localisation; strings are still kept external.
- Multiplayer.
- Mod tools beyond the YAML content.
- Music in the game (#39). The ElevenLabs Music licence is no longer the blocker (D-M); bringing music into scope is a separate decision.
- Console versions.
