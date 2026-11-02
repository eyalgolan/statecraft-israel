# Statecraft Israel Game Design

Core systems and player choices
Current version 0.3 | 30 October 2026 (consolidated design proposal); open questions answered by the owner on 1 November 2026
Previous published baseline metadata:
Version 0.2 | 17 October 2026 (version 0.1: 12 October 2026)
Map and location specifications updated 17 October 2026.
The owner's game design of record, based on the original document ([GDD-v0.1.pdf](GDD-v0.1.pdf)) with the additions dated above. The owner's rulings of 12 and 17 October 2026 are at the end. Where an older spec or ruling disagrees, this document wins (`AGENTS.md`).

This 0.3 revision adds Amir's subsequent design decisions to the current design of record and records the owner's 30 October 2026 conflict decisions and the owner's [1 November 2026 answers](#1-november-2026-answers) to its open questions. It describes intended changes; it does not claim the existing simulation implements them or that they supersede recorded owner rulings.

## MVP scope 1948 to 1951

The MVP tells the story of the Liberation War, the austerity period (תקופת הצנע), and immigration during 1948-1951.

Each turn advances history by one month. The MVP runs from 15 May 1948 to 15 January 1952, 44 monthly turns on the 15th (owner rulings D-B1 and D-E5, "End on 15 Jan 1952", [impact plan §5](superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md); `content/rules.yaml` `mvp.end`), giving the player time to finish the war, house and employ immigrants, construct the Tel-Aviv to Haifa railway and the Burma Road, and establish new yishuvim.

### Objectives

1. Survive the Liberation War. Earn bonus points for finishing the war earlier than its historical end and for capturing enemy soldiers.
2. Absorb immigrants. Provide immigrants with housing and employment.
3. Establish key infrastructure. Complete the Tel-Aviv to Haifa railway and the Burma Road corridor to Jerusalem, and establish new yishuvim at designated sites.

### Historical events on objective completion

Each time an objective is completed, it triggers a historical event card connected to the corresponding real event. The card contains an authentic image of that event, a short historical account, and the actual date when it happened.

The card also shows when the player completed the objective. Compare the player's completion date with the actual historical date: completing it earlier awards bonus points. Exact point values remain open.

## Core gameplay

A turn-based strategy game about building and defending Israel. Players allocate people and limited resources to employment, settlement, infrastructure and military activity, then respond to events that change their situation.

The rules below define the current design direction. Items marked Open or Proposal need a decision before implementation. Resource amounts, action costs and balance values remain unset.

### Player decisions and turn loop

The central decision is where people should contribute next. A civilian can take a job, enter military training or train as a pioneer. These choices compete for the same available population.

Proposed turn structure:
1. Review resources and city needs.
2. Move units, issue orders, start city projects and choose ministers. Moves and attacks happen at once; the other orders take effect at End turn (owner, 23 October 2026, #1402 and #1398: "Instant, like Civ", "No undo, like Civ" and "Approve, start building").
3. Select event outcomes.
4. Resolve production, consumption, training, construction and combat.

Unit orders are free under D-A6, and their order of resolution follows R-TURN-004 (owner, 23 October 2026, #1402 and #1398: "Approve, start building").

### Minister selection TBD

The player can choose ministers. TBD: minister roles, available candidates, appointment rules, costs and effects on gameplay.
**Then decided (owner, 23 Oct 2026, "Yes, complete (Recommended)"; [#1513](https://github.com/eyalgolan/statecraft-israel/issues/1513#issuecomment-6372357178)):** the implemented slice is the complete MVP minister mechanic: lawful appointment (`Cabinet.Appoint`) and each mapped office's competence and budget scaling the GDD systems in its remit (R-MIN-001 to R-MIN-003). The MVP adds no further offices, candidate traits or minister actions.

### Resources and national parameters

| Resource or parameter | Role | Still to define |
|---|---|---|
| Money | Funds development and other paid actions. Craftsmen earn more money. | Income per job, construction costs and upkeep. |
| Arms | Available military equipment. | Decided, D-G6: "From events (sourced arms deals) and Money. Training and attacks consume it" ([impact plan §5](superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md)). The four numbers asked on #856 and #781 are set (owner, 18 October 2026, #781: "Approve as drafted"), labelled design values in `rules.yaml`: one Arms is the equipment package for one 1,000-person recruit group, 12 Arms on 15 May 1948, IL 1m an Arms, 1 a recruit group, and 1 Attention for the buy order whatever the quantity (R-ARMS-001 to R-ARMS-003). Whether replacements consume it is open. **Then decided (owner, 23 Oct 2026, "Replacements cost Arms"; [#1510](https://github.com/eyalgolan/statecraft-israel/issues/1510#issuecomment-6372356139)):** trained recruits can refill a damaged formation, and each replacement group consumes Arms as recruit training does (R-ARMS-003); built under #1510 as R-TRAIN-004. |
| Food | Available food supply. | Production, imports, consumption and storage; stockpile or supply capacity. |
| Social cohesion (לכידות חברתית) | National parameter describing cohesion in society. Proposed influences include housing, employment and shortages. | Scale, thresholds and gameplay effects; effects of military decisions and events. |
| International Support | National parameter describing support from other countries. Proposed influences include military decisions and events. | Scale, thresholds and gameplay effects. |

### Population and units

| Unit | Defined role | Open rule |
|---|---|---|
| Refugee | Included as a population unit. | Arrival, resettlement and transition to civilian. |
| Civilian | Can take an employment slot or enter military or pioneer training. | Job reassignment, housing use and return from trained roles. |
| Craftsman | Generates more money than an ordinary civilian. | How acquired; employment requirements and extra income. |
| Halutz or pioneer | Builds map infrastructure and performs land improvement work. | Work duration, resource cost, project commitment and simultaneous project limit. |
| Veteran pioneer | Experienced pioneer unit. | Promotion requirement and benefit over a pioneer. |
| Recruit | Military unit after basic training. | Combat strength, upkeep and training duration. |
| Veteran soldier | Experienced military unit. | Promotion requirement and benefit over a recruit. |
| Artillery | Artillery unit with the working label Napoleonchik. | Acquisition, crew, range, ammunition and combat role. |
| Armored car (Israeli) | Israeli armored combat unit. | Acquisition, armor, movement and combat rules. |
| Arab Legionnaire | Unit representing an Arab Legion soldier. | Equipment, movement, strength and behavior. |
| Palestinian militia | Palestinian militia unit. | Equipment, movement, strength and behavior. |

**Then decided (owner, 23 Oct 2026, "Two-way, keep training"; [#1511](https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6372356482)):** craftsmen, halutzim and veteran pioneers can return to civilian life and keep their qualification, and a qualified civilian can be reassigned to the trained role without training again; the rule is built under #1511 (R-QUAL-001 to R-QUAL-007).

### Movement and civilian actions

The player can move a unit at once or set a go-to route whose next step runs at End turn. Units have an integer movement allowance and pay integer biome costs; built road and rail corridors may lower those costs. Lakes and the sea are impassable, stacking is limited, there is no zone of control, and there is no transport or embarkation. A unit with its full allowance may always enter one neighbouring land hex, whatever its cost; after spending points, it needs the full cost of the next hex. End turn waits until every military unit with movement points has moved, attacked, holds an order or is skipped for the turn (`Q-MOVE-1` to `Q-MOVE-5`; owner, 23 October 2026, #1402 and #1398: "Now" for go-to, "No, strict like Civ VI", "Block like Civ" and "Approve, start building").

A civilian can be employed in an available city employment slot. A civilian can train in a military camp and become a recruit, or train in a Hakhshara (הכשרה) and become a Halutz. The source of a Hakhshara and whether the player builds one are open.

### Progression

Two transitions are defined:
- civilian to recruit, through basic training;
- civilian to Halutz, through pioneer training;
- craftsman, halutz and veteran pioneer back to civilian life, and a qualified civilian back into the trained role without training again (#1511, R-QUAL-001 to R-QUAL-007).

These transitions are open:
- refugee to civilian;
- civilian to craftsman;
- Halutz to veteran pioneer;
- recruit to veteran soldier.

Veteran roles are part of the roster, but their progression is not yet specified.

Open: does a unit represent one person or a group? The answer must determine how units occupy housing and jobs, produce income, consume food, train and suffer casualties.

## Map and locations

### Locations specification

The MVP location roster, must-have and optional priorities, location-record requirements, historical placement rules and sources are defined in the [Locations specification](locations.md). Use that companion document when authoring named locations and their map data.

### Terrain type (tile/hex type): new types TBD

For the MVP the terrain types are the eight biomes in `hexes.json`, each with an integer movement cost; lakes and the sea are impassable (Q-MOVE-1, decided by the owner on 15 October 2026; [Military and combat](gdd/military-combat.md)). Only new terrain types beyond those biomes, and their extra properties, are **TBD**. They do not block the MVP or Phase 5's movement PR.

| Terrain decision | Status |
|---|---|
| New terrain/hex types beyond the eight biomes, their names and classification | TBD |
| Which properties belong to a terrain type and which are separate hex attributes or overlays | TBD |
| Movement-cost values and access rules for new types, within the approved movement framework | TBD |
| Type-specific combat and defence values, farming and construction eligibility | TBD |
| Whether projects change terrain type, and the permitted conversions | TBD |
| Visual representation and the mapping from source geography to gameplay types | TBD |

No new terrain catalogue or numerical terrain modifiers are selected in this specification. Existing sourced map data is retained while these decisions remain open.
**Then decided (owner, 23 Oct 2026, "Freeze for MVP (Recommended)"; [#1512](https://github.com/eyalgolan/statecraft-israel/issues/1512#issuecomment-6372356819)):** the eight biomes are the complete MVP terrain catalogue, with no terrain conversions; projects such as Dry swamp change project or site state, not a hex's biome. New types, overlays and conversions are deferred beyond the MVP.

The MVP terrain catalogue is therefore frozen at the existing eight biomes, with no new types or terrain conversions. Editable properties beyond the existing biome fields and final tile/hex presentation remain **TBD**. Workable land, prepared fields, roads, forts and authored location placement can be represented as explicit properties without inventing a replacement terrain taxonomy.

### Map editor tool

Provide an internal authoring tool that lets designers inspect and change tiles/hexes and move named locations by dragging or entering hex coordinates. It must support undo/redo, show unsaved changes, and save and reload version-controlled map data with consistent logical and rendered placement.

The full requirements and acceptance workflow are in [Map editor](gdd/map-editor.md). Location identities, sourced reference coordinates, historical names, dates and review metadata remain attached to each location. Moving a location must identify affected designated sites and corridor endpoints without silently changing sourced route geometry. The saved authoring changes must survive map-data regeneration.

The editor's host environment, authoring-data format and exact editable hex-property list are **TBD**. New terrain types remain TBD as above. Campaign movement and construction continue to follow the simulation rules. **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** the host is a Unity editor window; edits are saved to `content/map/overrides.yaml`, applied last by map generation; editable properties are a hex's terrain and control and a location's position, while its id and sources stay.

### Alignment with existing feature decisions

The location roster sets map-production priorities. It does not remove existing supported places, create a city economy for every marker, or make every location a required capture or founding objective. The detailed city, founding, corridor, military and settlement-control decisions remain applicable.

- Keep Tel Aviv and Jaffa as distinct map locations while retaining the approved combined Tel Aviv–Yafo city economy in [Cities and absorption](gdd/cities.md).
- Keep the Rishon LeZion winery; Zichron Ya'akov's additional industry opportunity has no new balance values or automatic winery implementation.
- Keep the Mefalsim founding slice and its objective. Historical founding and opening dates are reference dates; designated-site availability follows [Build yishuv](gdd/build-yishuv.md), allowing earlier player completion.
- Use sourced history for starting and reference control, and simulation state for ownership during a campaign. The map roster does not select the curated settlement-control target list or introduce enemy reinforcements.

## Cities and construction

| City attribute | Meaning | Open rule |
|---|---|---|
| Housing capacity | How much population the city can house. | People per capacity point; treatment of overflow. |
| Housing condition | The quality or condition of available housing. | City average or per building; effects and improvement rules. |
| Employment slots | The number of available jobs. | Worker types, income and effects of unemployment. |

### City projects

Housing and industry, including specialized industry, are city projects. The player selects and starts these projects in a city.

| City project | Defined effect | Requirement |
|---|---|---|
| Industry | Provides employment slots. | A permitted building site; costs and build time open. |
| Housing | Provides capacity, improves conditions, or both. | A permitted building site; housing types open. |
| Specialized industry | Adds a location-dependent industry. | City has the relevant local resource or area. A winery requires areas for grapes. |

A city has both capacity and conditions: adding more room and improving the quality of existing housing are separate development choices. The method for assigning residents to housing is open.

### Pioneer work

Building is allowed only at designated spots. A new yishuv uses the name associated with its designated site. The player chooses whether and when to develop an available site.

| Pioneer action | Target or result |
|---|---|
| Build road | Construct a road along a designated corridor. Costs and construction duration remain open. |
| Build railroad | Construct a railway along a designated corridor. Costs, duration and operating requirements remain open. |
| Plow fields | Prepare agricultural land. Food yield and required ongoing labor remain open. |
| Dry swamp | Improve a wetland area. Completion effects and later land uses remain open. |
| Build yishuv | Found a settlement at a designated spot, using that spot's name. |
| Build military training camp | Create a military training location at a designated site. Training capacity remains open. |

## Military actions and settlement control

| Soldier action | Defined intent | Open rule |
|---|---|---|
| Fortify position | Prepare the current position for defence. | Cost, duration, persistence and defensive benefit. |
| Hold position | Remain on sentry duty at the position. | Protection area, interception and upkeep. |
| Attack | Engage an enemy target. | Valid targets, range, combat resolution and losses. |

Recruits and veteran soldiers use the infantry action set. Artillery has a separate combat role whose orders and rules are open. Enemy combatants and settlement residents require distinct states in the settlement-control system.

### After capturing an enemy city or village

The player selects one of three outcomes:

- Raid and withdraw: clear hostile forces, then leave. Duration and effects on future hostile presence remain open.
- Occupy: retain control of the location and begin an occupation period. Required forces, costs and local effects remain open.
- Deport: remove settlement residents. Which residents are affected, their destination and the resulting settlement state remain open. Deportation and razing need separate rules: moving residents does not specify whether buildings are destroyed.

After X turns of occupation, the player chooses to install a puppet regime or annex the settlement. X is unset. A puppet regime provides indirect control; annexation incorporates the settlement into the player's territory. Population management, jobs, housing, defense responsibilities and resource access under each outcome are open.

**Then decided (owner, 18 Oct 2026, Q-PUP-1 (c), [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249846499); 22 Oct 2026, Q-OCC-3, [#1324](https://github.com/eyalgolan/statecraft-israel/pull/1324#issuecomment-6338594839); 24 Oct 2026, in the coordinator session, "1520: do this: 1. When the player annexes a town whose residents stayed (Acre, Nazareth), how many people, homes and jobs join the game? - a. The same rule as the 12 starting cities: a sourced resident count you approve. (Recommended) 2.b 3.a"; [#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433)):** no puppet regime in the MVP. X is six monthly turns. When an occupation ends the player annexes or withdraws. The residents of a fled town are recorded as fled when it is captured, and the record of the flight stays after a Raid. A town emptied by flight adds land only. A town whose residents stayed (Acre, Nazareth) adds one city by the starting-city rule, from a sourced resident count the owner approves. An annexed town stays Israeli for good, with no upkeep and no unit that has to stay. For a town whose residents already fled at capture, Deport stays listed and is refused with its reason (OWNER QUESTION 4 (a), owner, 24 Oct 2026, "Hide it, button explains (Recommended)", [#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6375591758)). Rules R-CTRL-014 to R-CTRL-017.
**Then changed (owner, 1 Nov 2026, "3 turns (27 Oct)"; [1 November 2026 answers](#1-november-2026-answers)):** X is three monthly turns, as the owner had ruled on 27 October 2026 ("3 turns, keep upkeep", [#1712](https://github.com/eyalgolan/statecraft-israel/issues/1712#issuecomment-6461246243)). `mvp.occupation_turns` reads 3 (`R-CTRL-012`) since [#2123](https://github.com/eyalgolan/statecraft-israel/pull/2123); the upkeep is unchanged.

Open: when is a settlement captured, what constitutes enemy control, and can an occupying force withdraw before the timer ends? Capturing enemy soldiers earns the bonus specified in the MVP objectives. Settlement-control rewards and political penalties remain open.

## Events and consequences

The player responds to events by selecting an outcome that produces a bonus, a penalty, or both. Effects can target money, arms, food, Social cohesion and International Support. Effects on units, cities or project availability are a proposal. Completed MVP objectives also trigger the historical event cards described at the beginning of this document.
The campaign runs from **15 May 1948 to 15 January 1952: 44 monthly turns**. The calendar boundary follows the [owner's latest PR ruling](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233776804) (checked 2026-10-17). The real-time play target is in [balance.md](gdd/balance.md#campaign-and-indicators).

Each event needs a trigger, readable context, available options and their consequences. Open: event frequency, mandatory versus optional events, delayed effects, and how much the player can see before choosing.
The player manages the establishment of the state through population, work, settlement, infrastructure, immigration and war. The map supports direct hex-to-hex orders and the retained per-city civilian management rules. Historical events explain the period and react to the player's situation; they must follow the existing [content policy](CONTENT_POLICY.md), [writing guide](WRITING_GUIDE.md) and review gates.

Historical dates, unit identities, named places and claims in event text require sources. Game balance values and hypothetical alternatives must be identified as design assumptions.
The core objectives are to establish the designated new settlements, complete the designated transport corridors, absorb immigration, and reach the armistices. Their exact completion tests, grouping and score ownership are in [objective-card.md](gdd/objective-card.md). A campaign can reach its end without completing every objective. Under `gdd-0.3` the retained defeat is the economic one (Money and Food exhausted), and losing a city or region alone does not end the game ([rules](rules/mvp.md#defeat); [ministers-events-end.md](gdd/ministers-events-end.md)). Amir's proposal to make every terminal defeat TBD was declined by the owner on 30 October 2026 ([conflict decision](#30-october-2026-conflict-decisions)).

## Production decisions to resolve

Designated building sites and transport corridors bound the locations and routes we must support. Population scale, project timing and the effect of terrain on movement remain major decisions for map production.

| Decision | Why it changes production |
|---|---|
| MVP locations and map scale | The must-have and optional location roster is specified in [locations.md](locations.md). Use the existing grid in [Map units](units.md); any required extent or resolution change and the handling of closely spaced locations remain open. |
| Terrain type (tile/hex type) | MVP: the eight `hexes.json` biomes with a movement cost per biome (Q-MOVE-1, decided). TBD: any new types, their catalogue, classification, visuals, extra properties, construction rules and terrain conversions. **Then decided (owner, 23 Oct 2026, "Freeze for MVP (Recommended)"; [#1512](https://github.com/eyalgolan/statecraft-israel/issues/1512#issuecomment-6372356819)):** no new types or conversions in the MVP. |
| Map editor tool | See [Map editor](gdd/map-editor.md) for hex editing, location moves, undo/redo, save/reload, regeneration and reference validation. Host environment, authoring format and editable-property list remain TBD. **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** a Unity editor window saves `content/map/overrides.yaml`, which map generation applies last; editable are hex terrain and control and location position, while location id and sources stay. |
| Road and rail corridors | Routes follow designated corridors. Each corridor needs predefined route geometry and construction states. The corridor list, costs and duration remain open. |
| Unit population scale | A person or a group changes unit count, housing and job assignment, selection, movement and casualty representation. |
| Population role model | Changing one unit between jobs and training differs from separate worker pools. Decide reassignment and return to civilian life. **Then decided (owner, 23 Oct 2026, "Two-way, keep training"; [#1511](https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6372356482)):** trained civilian roles can return and be reassigned, keeping their qualification. |
| Work and training time | Instant actions and multi-turn projects require different feedback, commitment rules, cancellation and progress displays. |
| Food and arms supply | National pools need fewer logistics rules. Local storage and supply routes add transport, shortages and vulnerability to disruption. |
| Settlement control | Occupation, indirect rule, annexation, displacement and destruction require explicit population, ownership and building states. |

## Document authority

This document owns scope, the authority map and deferred decisions. The linked feature file owns the corresponding rules. [balance.md](gdd/balance.md) is the only active catalogue of numerical design values; feature files reference its keys rather than maintaining their own cost tables. Historical dates and factual evidence remain with the relevant feature or content record.

[The feature index](gdd/README.md) explains the file structure. [The rule-ID index](rules/mvp.md) preserves links from existing tests and identifies implementation contracts requiring migration. Old plans are implementation history, not additional rule owners. The original [GDD v0.1 PDF](GDD-v0.1.pdf) is a historical reference; it is not a second active GDD.

|Subject|Authoritative document|
|-|-|
|Numerical values, prices, rates, capacity, starting state|[Balance](gdd/balance.md)|
|Population identity, professions, reception, courses, military conversion|[Training and population](gdd/training.md)|
|Regions, civilian assignments, housing, construction, workshops|[Cities and regions](gdd/cities.md)|
|Money, Food, Arms, imports, immigration, upkeep settlement|[Resource layer](gdd/cutover-resource-layer.md)|
|Movement, combat, military XP, forts, enemy AI, truces and prisoners|[Military and combat](gdd/military-combat.md)|
|Region loss, evacuation, occupation, annexation and withdrawal|[Settlement control](gdd/settlement-control.md)|
|Designated site projects and field preparation|[Build yishuv](gdd/build-yishuv.md)|
|Road and railway construction|[Corridors](gdd/corridors.md)|
|Objective tests, completion cards and scoring|[Objective card](gdd/objective-card.md)|
|Resolver sequence, saves and migration|[Game loop](gdd/loop-switch.md)|
|Calendar implementation and event windows|[Monthly turns](gdd/monthly-turns.md)|
|Events, government integration and campaign ending|[Ministers, events and end](gdd/ministers-events-end.md)|
|Fixed camera and internal map authoring|[Map editor](gdd/map-editor.md)|
|Named places, aliases, inclusion priority, facility roster and source notes|[Locations](locations.md)|
|Route geometry and evidence|[Routes](routes.md)|
|Unit artwork and historical placement anchors|[Units](units.md)|

## MVP completion rules

The MVP years, monthly turns and three objectives are fixed above. These remain to decide:
- the map extent, starting unit counts, event count and the session's target play time (asked of Amir, [#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231565826)); the span is 15 May 1948 to 15 January 1952, as above;
  **Then decided (owner, 18 Oct 2026, "60-90 minutes"; [#813](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6249771314)):** a full session targets 60 to 90 minutes, about 2 minutes a turn.
  **Then changed (owner, 30 Oct 2026, "120 (data wins)"; [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164)):** a full session targets 120 minutes, matching `gdd03-balance.yaml`.
  **Then decided (owner, 30 Oct 2026; [conflict decision](#30-october-2026-conflict-decisions)):** Historical mode keeps the sourced `content/map/units_1948.yaml` seed (`Q-START-1`); parameter `A` applies only to non-Historical modes.
- when the war-survival objective is complete, which immigrants must have housing and employment, and which new yishuvim are required.

Each objective needs a specific historical event and reference date for the completion card. For ongoing processes such as immigrant absorption, choose a defined milestone with a sourced date. Bonus amounts, the timing formula, defeat conditions and whether all objectives are required to finish the MVP remain open.
**Then decided (owner, 30 Oct 2026; [conflict decision](#30-october-2026-conflict-decisions)):** keep the recorded defeat rules: under `gdd-0.3` the economic defeat (Money and Food exhausted) stays, and losing a city or region alone does not end the game ([rules](rules/mvp.md#defeat); see [Final defeat conditions](#final-defeat-conditions)).

Minister selection is a planned TBD mechanic with player-chosen ministers. The role of coalition, Attention, diplomacy and national policy systems in this core loop remains open.
**Then decided (owner, 23 Oct 2026, "Yes, complete (Recommended)"; [#1513](https://github.com/eyalgolan/statecraft-israel/issues/1513#issuecomment-6372357178)):** R-MIN-001 to R-MIN-003 and `Cabinet.Appoint` are the complete MVP minister mechanic.

## Population and management direction

The same population unit retains its identity across civilian work, training, military service and demobilisation. Civilian profession and military experience are separate attributes. Map anchors for historical formations do not create additional population units. **Amir's proposal, declined by the owner on 30 October 2026 ([conflict decision](#30-october-2026-conflict-decisions)):** population units would be assigned through regional capitals.

**Amir's proposal, declined by the owner on 30 October 2026 ([conflict decision](#30-october-2026-conflict-decisions)):** player regions would pool civilian housing and employment and use prepared regional farmland. The retained per-city rules and the sourced annexation-resident rules remain authoritative.

## Relationship to the existing design
*Amir's #1668 proposed removing automatic borrowing and automatic Food refill while making Money and workers the construction constraints. The owner's 30 October 2026 conflict review ([conflict decision](#30-october-2026-conflict-decisions)) did not rule on this. On 1 November 2026 the owner kept main's economy for the MVP (D1, "Approve all six (Recommended)"; [1 November 2026 answers](#1-november-2026-answers)): the operative rules are `R-MONEY-003` and `R-FOOD-002` (`docs/rules/mvp.md:399` and `:409`), and Amir's proposal is post-MVP.*

The existing units-in-play design describes units as a presentation layer with no individual orders. This GDD calls for units, jobs, training, construction and settlement control to affect gameplay. Those rules must become simulation state and actions under the existing deterministic simulation architecture.

Reference files from the repository snapshot reviewed on 12 October 2026:
- Original Statecraft Israel design specification
- Units in play design and owner rulings
- Roads and railways implementation notes
- M3 scope rulings

## Map, locations and presentation

The complete must-have and optional roster is maintained only in [locations.md](locations.md). Inclusion does not automatically create a managed city, a capturable target, a new population unit, or an operating facility. Military camps are distinct selectable locations, highlighted and enlarged for readability; reception camps are a separate facility category.

The camera has a fixed viewing direction and pitch, supports pan and **dolly in/out**, and does not orbit. Unit movement is turn-based between hexes. The internal editor changes tiles and moves locations while preserving identifiers, source coordinates and regeneration-safe overrides; see [map-editor.md](gdd/map-editor.md).

---

## Owner rulings, 2026-10-12

1. **Deport outcome: honest cost, no reward.** It stays as a historical possibility but never pays. It carries large Social cohesion and International Support penalties, creates refugees, has permanent consequences, and is shown soberly through sourced event cards. CONTENT_POLICY §4 is updated to say how this is handled ("no mechanic may reward violence against civilians, from any side").
2. **Adoption: spec and impact plan first.** The GDD is committed as the design of record and analysed against the current code. The phased build plan and open decisions are proposed, and the owner approves before any code changes. Visual and promo work continues meanwhile.

## Owner rulings, 2026-10-17

1. **Map and location additions (B1-B7): approved as written** ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6230935027)): the location roster (approved as 31 must-have and 16 optional; review edit A2 then moved Hadera and Tiberias, already managed cities, to must-have, giving 33 and 14), the West/East Jerusalem split, the new location kinds (reception camp, forts, passes, bridges), the Arab-held towns on the map but not as targets, the Zichron Ya'akov industry, the map editor as a new tool, and the existing grid.
2. **Four further additions: approved** ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233524779)): Kiryat Shmona and Beit Shemesh as designated founding sites, Nir Am's infrastructure-protection objective, Umm Rashrash (future Eilat) as an end-of-war milestone, and Zichron Ya'akov as a must-have location for its vineyards and winery. The sensitivity sign-off for Kiryat Shmona and Beit Shemesh is open (#782); Phase 4's site list follows once it is given.
   **Then decided (owner, 18 Oct 2026, "Part of objective 3"; [#1006](https://github.com/eyalgolan/statecraft-israel/issues/1006#issuecomment-6254333583)):** the Negev water line that began at Nir Am must stay in Israeli hands until the armistice with Egypt on 24 February 1949; its card gives the sourced 1947–48 pipeline history, with `checked:` dates and a person's fact check, and its points are a labelled design value for the balance pass.
   **Then decided (owner, 18 Oct 2026, "Bonus inside the war"; [#1007](https://github.com/eyalgolan/statecraft-israel/issues/1007#issuecomment-6254333758)):** objective 1 gives a bonus when a player unit reaches the Umm Rashrash hex before the war ends; it does not change when the war objective completes. The card is the Ink Flag, 10 March 1949 (Operation Uvda), sourced with `checked:` dates and a person's fact check; points are a labelled design value for the balance pass.
3. **Session span** (Q-DOC-1, [#784](https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6232838937)): the MVP is 44 monthly turns, 15 May 1948 to 15 January 1952. The session's target play time is open, asked of Amir ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231565826)).
   **Then decided (owner, 18 Oct 2026, "60-90 minutes"; [#813](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6249771314)):** a full session targets 60 to 90 minutes, about 2 minutes a turn.
   **Then changed (owner, 30 Oct 2026, "120 (data wins)"; [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164)):** a full session targets 120 minutes, matching `gdd03-balance.yaml`.

## Decision provenance and implementation status

* The [Arms request](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231351946) and [real-time play request](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231565826), checked 2026-10-17, are addressed by the balance catalogue and population/training rules.
* *Considered and declined (owner, 30 Oct 2026; Upkeep conflict, option A; [conflict decision](#30-october-2026-conflict-decisions)): Amir's #1668 proposed housing-only upkeep deterioration. For Amir's reception before civilian work, the review did not rule; on 1 November 2026 the owner kept `R-CITY-004` (`docs/rules/mvp.md:105`) with "Keep R-CITY-004 (Recommended)", so Amir's reception turns are post-MVP; main has an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`).*
* **Agreed** in the balance catalogue means an explicit interview decision or a retained approved baseline. Unresolved numbers remain TBD rather than receiving assistant-supplied defaults; the values this revision left `open (owner)` were answered by the owner on 1 November 2026 ([1 November 2026 answers](#1-november-2026-answers)).
* Existing `content/` values, tests, saves and code still describe the earlier implementation where they conflict. Implementation work must migrate them together, update rule-ID coverage and deliberately review hash changes. This documentation revision does not silently change that code or its data.

### 30 October 2026 conflict decisions

On 30 October 2026 the owner reviewed all 13 conflicts between the recorded design and Amir's #1668 proposal and chose to keep every recorded ruling. Each declined note in this additive design links here and covers only a proposal that is option B of one of those 13 conflicts; it preserves the adjacent ruling rather than reopening or replacing it. A #1668 proposal outside the 13 conflicts is not declined: its note says the review did not rule on it and names the operative main rule or catalogue entry, or says `open (owner)` where main has none.

Conflicting #1668 mechanics and catalogue entries retained for traceability are non-operative. The recorded ruling beside each declined note remains authoritative; an unresolved value is `open (owner)` and must not be inferred from the declined proposal. On 1 November 2026 the owner ruled "30 Oct wins for the MVP (Recommended)": these declined proposals are post-MVP design, kept for Amir to develop, and the remaining `open (owner)` values were answered ([1 November 2026 answers](#1-november-2026-answers)).

### 1 November 2026 answers

On 1 November 2026 the owner approved an answer to every open GDD 0.3 question: the numbered questions on [#1668](https://github.com/eyalgolan/statecraft-israel/pull/1668), the follow-ups in the GDD 0.3 issues and the values this revision marked `open (owner)`. The six larger decisions D1-D6 were approved with "Approve all six (Recommended)", the occupation length with "3 turns (27 Oct)", the other answers with "Approve all 79 (Recommended)", and each Medium- and Low-confidence answer one by one (for example "0.35, your 15 Oct value (Recommended)", "1.5:1 in gdd-0.3 (Recommended)", "10 per unit, main (Recommended)" and "Keep the national rate (Recommended)"). Each question, quoted exactly, with its answer and the rule behind it, is in [the approved answers on #1668](https://github.com/eyalgolan/statecraft-israel/pull/1668#issuecomment-6553884547) (checked 2026-11-01). The IDs below (D1, Q23, O7, 27M-16 and so on) are that comment's.

**MVP and post-MVP.** The owner also ruled "30 Oct wins for the MVP (Recommended)": where Amir's 0.3 design conflicts with the 30 October rulings, the MVP follows those rulings, and Amir's text stays in these files as **post-MVP** design for Amir to develop later. It is not dropped. A declined note in a feature file therefore marks post-MVP design, not a rejected idea. The owner kept refugee conversion on the next housing step with "Keep R-CITY-004 (Recommended)", so Amir's six reception turns before civilian work are post-MVP too.

| ID | Decision | Approved (owner, 1 November 2026: "Approve all six (Recommended)") |
|-|-|-|
| D1 | MVP economy | Keep main's economy: per-head tax and spending (`R-MONEY-001`), every GDD cost borrows its shortfall automatically (`R-MONEY-003`, `R-TRAIN-001`, `R-SITE-001`, `R-ARMS-002`) (Arms purchases past the debt ceiling are refused instead, #2198; [later rulings](#later-rulings-2-november-2026)), the monthly Food import refills to `cover_months` (`R-FOOD-002`), and `BuyArms` delivers at once (`R-ARMS-002`). Amir's 0.3 economy (income only from jobs, credit only for Food and Arms imports, no refill, next-turn ship deliveries) is post-MVP. |
| D2 | Population scale and starting inventory | The Historical MVP keeps main's start: census cities at 1,000 people a unit (`R-CITY-007`), Money 20 (`treasury_start`), Arms 12 (`arms_start`), Food at `cover_months` (3 months of need) and the formations from `content/map/units_1948.yaml`. Amir's 65 units at 10,000 people a unit, Money 25, Food 10, Arms 3 and farmers 5 / 10 / 5 are for Open mode, when Open mode exists. |
| D3 | Professions | Two-way professions for the MVP (`R-QUAL-001` to `R-QUAL-007`, #1511): a qualified civilian can return and be reassigned, and a Train order can take a qualified civilian, who then loses the qualification (`R-QUAL-006`). Amir's lock at the first qualified level (Q9) is post-MVP. |
| D4 | Historical army | Historical keeps the `units_1948.yaml` seed: starting strengths (`R-UNIT-006`), dated arrivals (`R-UNIT-009`) and growth only by replacements (`R-TRAIN-004`). No manning modifier and no scripted draft in Historical. |
| D5 | Southern Coast and Negev farmers | One opening housing place in each region for its farmer, once a sourced research note confirms a Jewish settlement in each region on 15 May 1948. The places are Nir Am and Nirim ([later rulings](#later-rulings-1-november-2026)). |
| D6 | An Israeli city while an enemy unit holds its hex | Its jobs pay nothing and its Food production and projects pause; its people stay. On retaking, everything resumes and nothing is deleted. Amir's region capture and automatic evacuation are post-MVP. |

| ID | Value this revision had as `open (owner)` | Approved answer (owner, 1 November 2026) |
|-|-|-|
| O1 | Fortify defence bonus | +25% (main's 125%, `R-ORDER-002`); one turn to set. |
| O2 | Hold effect | No defence bonus, own hex only, no interception (`R-ORDER-003`). |
| O3 | Combat result bands and loss granularity | Main's `mvp.combat_table` (`R-COMBAT-002`, `R-COMBAT-003`); losses fall one population unit at a time on the strongest unit. |
| O4 | `combat.enemy_attack_ratio` | 1.5 under `gdd-0.3` ("1.5:1 in gdd-0.3 (Recommended)"); `gdd-0.1` keeps 2:1 (`R-ENEMY-003`). |
| O5 | `combat.artillery_range` | 2 hexes (`R-COMBAT-007`). |
| O6 | `prisoner.food` and the shortfall | 1 Food per held unit a turn; each unit of Food missing costs 0.01 Social cohesion (`R-POW-002`). |
| O7 | `prisoner.return_points` | 10 per population unit returned, main's value ("10 per unit, main (Recommended)", `R-POW-004`), not 5 a cohort. |
| O8 | Starting reception and training-centre host | Kiryat Shmuel, Haifa, hosts the one starting training centre (owner, 26 October 2026, "Kiryat Shmuel, Haifa"). |
| D2 | `start.money`, `start.food`, `start.farmers` | Historical MVP: Money 20 and Food at `cover_months`; `start.farmers` is not used (census cities). Amir's 25 / 10 and 5 / 10 / 5 stay for Open mode. |
| Occupation | `occupation.turns` | 3 turns ("3 turns (27 Oct)"). `mvp.occupation_turns` reads 3 (`R-CTRL-012`) since [#2123](https://github.com/eyalgolan/statecraft-israel/pull/2123). |
| Post-MVP | `transit.cost`, `reception.queue_cohesion_loss` and port-bed queues, automatic and route-blocked evacuation, the editor's region-membership and workable-land fields, editor facility dependencies | Not in the MVP. There is no transit and no port queue, a held city follows D6, regions come from sub-districts and fields are authored data (Q36), and facilities are per city. |

**Other answers, by area** (all approved on 1 November 2026; the comment gives each one's reason):

- **Economy and Food.** Foreign currency stays the internal ledger under Money, so the sourced 1951 crisis still cuts the Food import (Q2) (owner 2 Nov, #813: the player may convert up to $4m a month of foreign currency into Money during the war, $100m in total, via one foreign-currency readout; note that a top-bar foreign-currency meter already existed on main, see #2204; [later rulings](#later-rulings-2-november-2026)). A bond or loan card adds its Money and the same principal to Debt, with no interest or schedule, and its figures must be sourced (Q5). Every deficit is borrowed (Q21, Q24). `debt_ceiling` (IL 250m) is only the "Money exhausted" threshold of the economic defeat; no order is refused at it (Q22) (except Arms purchases: refused past the ceiling, owner 2 Nov, #2198; [later rulings](#later-rulings-2-november-2026)). Debt starts at 0 and Money is IL 1m (Q23). The economic defeat is Debt at or above the ceiling and Food at 0 for `defeat_turns` (3) turn ends in a row (Q25), read before this turn's borrowing (Q26). Purchases and unaffordable courses borrow automatically (Q56, 27M-6). `food_supply` stays the 0-1 readout (Q6). A unit with strength 1 or more never counts below 1 after a modifier (Q7). `BuyArms` is a waiting order, editable or withdrawn at no cost until End turn and final after it; there are no player Food purchases in the MVP (Q45-Q47).
- **Events and indicators.** The seven Attention, passive-budget and refill cards lose only their Attention effects under `gdd-0.3` (Q1). Rationing and ma'abarot cards stay indicator-only (Q3). Events may write Money, Arms, the Food stock, Social cohesion and International Support; housing condition and `food_supply` are readouts (Q4). `start.cohesion` and `start.support` are opening values only, and the rationing and security links stay (Q17, Q18). The #1716 snapshot lands before the indicator changes, with no regional workforce helper (Q19).
- **Population and courses.** Commands on "N units" take ascending population ids; a returned captive keeps their id and military qualification, comes back unequipped and joins the nearest friendly city as a civilian (Q10, 27M-20). Event grants and arrivals take new ids as refugees in a city, and natural increase stays the national rate, 0.0169 a year ("Keep the national rate (Recommended)", Q11). A course occupies its people from its first turn (Q43(a)); they are released in the turn its last course turn counts and can be assigned from the next planning, or at once on cancellation, and work starts the turn after (Q43(b), Q43(c)). One veteran teacher and one pupil per centre, the teacher housed in the centre's city (Q42). A cancelled or slot-losing course is cancelled like `CancelTraining`, people return as civilians, Money and tools are not refunded and Arms return to the pool (Q44, Q52, 27M-3, 27M-4). Graduates join their city's civilians and may re-enrol at any time (27M-5). A course occupies the person for any other job, course, pioneer work or military order, without changing their housing (#1697 follow-up). The novice craftsman label starts with the craftsman course and ends on cancelling (#1698 follow-up). Workshop work by a novice changes no qualification (Q55). A draft withdrawn before End turn leaves nothing to settle (Q53).
- **Work and fields.** A worker works in their own city and farmers live in the region they farm (Q27). Employed means civilians and craftsmen in job slots over civilians plus craftsmen, and `work_lifts_cohesion` stays (Q35). Starting fields are the authored list from the owner-approved per-region split of 27 October 2026, with no numeric threshold (Q36); the 20 farmer / 8 Industry split is symbolic and kept (MAN-1). Only an Israeli-held hex can be farmed (Q37). A retaken field resumes through the city's job default (27M-1). Farming counts whole months from the month's snapshot (27M-2). A workshop reserves its cycle at the monthly production step, repeats automatically each month while staffed ("Automatic monthly (Recommended)"), takes any free civilian at the worker's cost tier, and loses progress and paid Money on cancellation (27M-11 to 27M-14). New housing and repairs follow the Money-plus-work rule for city projects, and repairs wait for #1700 (#1703 follow-ups a and b).
- **Housing, absorption and camps.** Housing effects are measured after the step's arrivals become civilians (27M-15). Every city starts at the one housing condition 0.35 ("0.35, your 15 Oct value (Recommended)", 27M-16, `R-HOUSE-001`). Housed means civilians, craftsmen, recruits and groups in training (27M-18). Absorption's overflow limit is national (27M-21) and counts employment at turn end (27M-22). Refugees cannot train until they become civilians, so 27M-23, Q54 and 27M-10 do not arise in the MVP. Training is ordered in the camp's city, with no travel (27M-7). A formation pays military upkeep from the turn it graduates (27M-8). After a cancellation or camp loss, people go to their city as civilians without a job (27M-9). Schneller and Tel HaShomer operate from the start, the other military camps are build sites, every camp uses `military.camp_capacity` 5, and the reception site is Kiryat Shmuel, Haifa (27M-25).
- **Military.** Arms equip new soldiers in training and replacements; attacks and artillery spend none. A recruit course cannot start without its Arms, and all training stays in 0.3 (ARMS-1 to ARMS-3). Artillery has 2 movement points and infantry terrain costs (Q12). Every `kind: fort` record is a permanent fort (Q13). Under `gdd-0.3`, veterancy comes from combat experience: every unit, artillery firing alone included, earns `military.combat_xp_gain` (1) for each engagement it survives, experience stops at veterancy (`military.combat_xp_full`, 5), and service time is a statistic; `gdd-0.1` keeps `R-UNIT-004` (Q14 to Q16). Prisoner points are scored only when a front's exchange card returns them; prisoners taken after that card stay held, with no points (27M-19). The armistice quiet-turn count runs per front from the scenario start, no offer comes before the 15 December 1948 turn, a quiet turn has no engagement involving that front's units, and a refused order does not break it (Q48); two full quiet turns must follow a refusal (Q49). No manning in Historical (MAN-2, D4).
- **Pioneers and the editor.** A `PioneerWork` order does not move the unit, so a pioneer works and earns experience from the turn it starts (Q50). There is no pause: stopping ends the work, and progress and Money are lost (Q51). Pioneer work borrows its shortfall (Q57, D1). Broken corridor endpoints, force anchors, target bindings and objective bindings block export, and visual and label offsets only warn (Q58). Offsets use axial hex (q, r) plus metres from the hex centre and stay inside their own hex (Q59).
- **Post-MVP** ("30 Oct wins for the MVP (Recommended)"). Regime taxes and puppet regimes (Q28 to Q33): an annexed town that stayed earns ordinary city income and a fled place pays nothing (`R-CTRL-016`). Ship immigration (Q38, Q39, #1695): arrivals come from the CBS series (`R-CITY-003`). Port and reception queues and bed reservations (Q40, Q41, #1696, #1723): refugees wait in their cities (`R-CITY-004`). Amir's import-only credit (Q20, Q57), no Food refill, ship purchases and profession lock (D1, D3).

#### Later rulings, 1 November 2026

Later on 1 November 2026 the owner answered the questions the approved answers left open and ruled on the D5 places and the operation cards. Each answer is quoted exactly.

| Question | Owner's answer (1 November 2026) | Rule |
|-|-|-|
| GDD 0.3's per-unit rates at D2's 1,000 people a unit | "Main's base x 0.3's tiers (Recommended)" | Main's per-unit rate is the base and 0.3's tiers are multiples of it: Industry income 1x / 2x / 3x the `R-CITY-002` job income (as `R-CRAFT-002` does for craftsmen), farmer Food 2x / 3x / 5x `field_food_per_turn`, and counts sized in 10,000-people units 10x (`military.camp_capacity` 5 becomes 50 units). `workshop.cost` is Money per Arms and stays. All are labelled design values, and a balance playtest follows ([#2112](https://github.com/eyalgolan/statecraft-israel/issues/2112)). |
| Reception in the MVP | "No reception-bed layer (Recommended)" | The reception camps are map records, the training centre is an ordinary facility in Kiryat Shmuel, Haifa, and reception beds with their six-turn progress are post-MVP ([#1696](https://github.com/eyalgolan/statecraft-israel/issues/1696#issuecomment-6554002672)). |
| Send home under `gdd-0.3` | "Keep R-UNIT-005 (Recommended)" | Send home stays the only way home, after the front's armistice (`R-UNIT-005`); Amir's demobilisation at a camp is post-MVP ([#1699](https://github.com/eyalgolan/statecraft-israel/issues/1699#issuecomment-6554003735)). |
| Housing repair price | "Fixed price 0.5 Money + 1 work" | A repair costs 0.5 Money and 1 work and raises the city's housing condition by 10 percentage points. The price is a labelled design value ([#1700](https://github.com/eyalgolan/statecraft-israel/issues/1700#issuecomment-6554004118), [#1703](https://github.com/eyalgolan/statecraft-israel/issues/1703#issuecomment-6554005317)). |
| Historical starting farmer tiers | "Amir's 1:2:1 ratio per city (Recommended)"; the rounding rule: "all approved" | Each census city's starting farmers are a quarter novice, half trained and a quarter veteran, a labelled design value. When a city's count is not a multiple of 4, the split uses the largest remainder with ties to trained (5 farmers: 1 / 3 / 1), and every city's total stays exact ([#1717](https://github.com/eyalgolan/statecraft-israel/issues/1717#issuecomment-6554155751), [rounding](https://github.com/eyalgolan/statecraft-israel/issues/1717#issuecomment-6555212806)). Amir's 5 / 10 / 5 stays for Open mode (D2). |
| D5 places | Southern Coast: "Nir Am (Recommended)"; Negev: "Nirim, add it to the map (Recommended)"; for Nirim: "Optional (Recommended)", "Israeli at the start (Recommended)" and "Agent checks are enough" | Nir Am is the Southern Coast's opening housing place and Nirim, at its 1948 site at Dangur (today's Nir Yitzhak), is the Negev's. Nirim is an optional location, Israeli-held at the start. Agent checks of the sources replace D5's person's check for these places ([#2113](https://github.com/eyalgolan/statecraft-israel/issues/2113#issuecomment-6554685727), [#2136](https://github.com/eyalgolan/statecraft-israel/pull/2136)). |
| Operation-card orders | "military openrations must have unit orders"; Latrun: "Harel at Sha'ar HaGai (Recommended)"; Dani: "Kiryati to Ramle (Recommended)" | Every military-operation card gives unit orders, each from a sourced, dated anchor (owner, 31 October 2026, #749): `ten_days`, `operation_yoav`, `operation_hiram`, `operation_horev` and the attack option of `latrun_proposal`. Latrun orders Harel from its Sha'ar HaGai anchor to Latrun; Dani orders Kiryati to Ramle ([#2087](https://github.com/eyalgolan/statecraft-israel/pull/2087#issuecomment-6555753104), [Latrun and Dani](https://github.com/eyalgolan/statecraft-israel/pull/2087#issuecomment-6555857468)). |
| Movement preview | "I am talking about an arrow showing which tiles the unit is going to move to. this needs to be for israeli units only" | Choosing where an Israeli unit moves shows an animated arrow over the tiles it will move through. Enemy units get no arrow ([#2134](https://github.com/eyalgolan/statecraft-israel/issues/2134)). |
| The 1949 armistice-lines overlay | "On by default (Recommended)" | The 1949 armistice-lines overlay shows in every run, with no URL flag; a release build draws only the lines the owner signed off ([#2195](https://github.com/eyalgolan/statecraft-israel/pull/2195#issuecomment-6565077805)). |
| The desktop Quit button | Hebrew label: "יציאה is right (Recommended)"; Settings placement: "Own row at the bottom (Recommended)" | Desktop builds show Quit on the start screen and in its own last row in Settings, labelled "יציאה" in Hebrew; the browser build hides it ([#2188](https://github.com/eyalgolan/statecraft-israel/pull/2188#issuecomment-6564490828)). |

#### Later rulings, 2 November 2026

On 2 November 2026 the owner ruled on Arms bought on credit, a lever on the war debt and the armistice-lines overlay. Each answer is quoted exactly.

| Question | Owner's answer (2 November 2026) | Rule |
|-|-|-|
| Arms bought past the debt ceiling | "Refuse buys over ceiling (Recommended)" | An Arms purchase whose borrowing would take Debt past `debt_ceiling` (IL 250m) is refused with a clear reason. This overrides Q22's "no order is refused at it" for Arms purchases only; every other cost still borrows its shortfall (D1) ([#2198](https://github.com/eyalgolan/statecraft-israel/issues/2198#issuecomment-6564721486), [#2203](https://github.com/eyalgolan/statecraft-israel/pull/2203)). |
| Hebrew for the Arms debt-ceiling refusal | "Hebrew is right (Recommended)" | The owner's native read approves `arms.refused_debt_ceiling` as "רכישת כמות כזאת של נשק תעלה את החוב מעל התקרה" ([#2203](https://github.com/eyalgolan/statecraft-israel/pull/2203#issuecomment-6564879060)). |
| The war debt of about IL 5m a month | "Give the player a lever (Recommended)", then "Spend the dollars (Recommended)" | The player gets a lever on the war debt: during the war, up to $4m a month of foreign currency can be moved into Money, $100m in total, which brings the sourced 1951 currency crisis and its Food-import cut closer. One foreign-currency readout is shown, so foreign currency is no longer wholly internal (Q2) ([lever](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6564721721), [options](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6564855158), [dollars](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6564883926)). |
| The armistice-lines switch and legend | Switch: "Tie to Borders button (Recommended)"; legend: "Restyle Jordan lines (Recommended)" | The minimap's Borders button also hides and shows the armistice lines, with no new strings. The low-accuracy Jordan lines get a pattern apart from the legend's grey dash for no-man's land and the demilitarized zones, with no text changes ([#2195](https://github.com/eyalgolan/statecraft-israel/pull/2195#issuecomment-6565077805)). |

#### Later rulings, 2 November 2026

| Question | Owner's answer (2 November 2026) | Rule |
|-|-|-|
| A lever for the war-time debt (#813) | "Spend the dollars (Recommended)" | During the war (rungs 4 and 5) the player may convert up to $4m a month of foreign currency into Money, $100m in total; the reserves run out sooner, so the sourced 1951 currency crisis and its Food-import cut come closer (`R-MONEY-004`; [ruling](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6564883926), [options](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6564855158)). This overrides D-C3 ("Keep them internal, under Money, at least through cutover ... and hide them if they confuse") only for the one foreign-currency readout this order needs, on the Defence screen where it is made. |

## Deferred decisions

These are recorded once here. Feature files link here instead of maintaining competing open-question lists.

|Decision|Status / boundary|
|-|-|
|Starting army size outside Historical mode|Historical mode keeps the sourced seed from `content/map/units_1948.yaml` (`Q-START-1`, decided 15 October 2026). Parameter `A` applies only to non-Historical modes and remains deferred; no previously proposed count is accepted (owner, 30 Oct 2026). Historical army growth is only by dated arrivals and replacements, with no manning modifier or scripted draft (owner, 1 November 2026, D4).|
|Regional-capital model|**Amir's proposal, declined by the owner on 30 October 2026:** create a regional-capital roster and shared regional administration. The retained per-city rules remain authoritative.|
|Founding dates|The owner set Kiryat Shmona to 18 July 1949 and Beit Shemesh to 15 December 1950 under the monthly turn ([#785](https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6237624501)).|
|Facility availability and starting training-centre host|The existing owner ruling already starts Schneller and Tel HaShomer as military facilities; the other military camps are approved build sites. Centre and teacher counts are decided. **Decided (owner, 1 November 2026, O8 and 27M-25; [1 November 2026 answers](#1-november-2026-answers)):** Kiryat Shmuel, Haifa, hosts the one starting training centre and is the reception site; every camp uses the same `military.camp_capacity` of 5, with no per-camp exceptions. Reception-camp availability beyond that still needs a source before it is authored.|
|Final defeat conditions|Under `gdd-0.3` the retained defeat is the economic one (Money and Food exhausted); losing a city or region alone does not end the game ([rules](rules/mvp.md#defeat)), and [the retained end rules](gdd/ministers-events-end.md#rules) add no coalition collapse. *Considered and declined (owner, 30 Oct 2026; [conflict decision](#30-october-2026-conflict-decisions)): Amir's #1668 proposed making every terminal defeat TBD.* While an enemy unit holds an Israeli city's hex, the city's jobs pay nothing and its Food production and projects pause until it is retaken (owner, 1 November 2026, D6).|
|Minister-specific gameplay|The existing office-competence slice (`R-MIN-001` to `R-MIN-003` and `Cabinet.Appoint`) is the complete MVP minister mechanic ([owner ruling #1513](https://github.com/eyalgolan/statecraft-israel/issues/1513#issuecomment-6372357178)). Historical offices and event references remain, and the existing cabinet stays; under `gdd-0.3` there is no Attention resource ([rules](rules/mvp.md#government)).|
|Terrain presentation|**TBD**. The MVP catalogue itself remains frozen at the existing eight biomes.|
|Novice profession lock-in|Trained civilian roles can return and be reassigned while keeping their qualification ([owner ruling #1511](https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6372356482)). Amir's conflicting permanent, mutually exclusive profession proposal was declined on 30 October 2026; the owner kept two-way professions for the MVP on 1 November 2026 (D3), and Amir's lock (Q9) is post-MVP.|
|Sensitive capture outcomes|Raid and Deport remain subject to the existing [content policy](CONTENT_POLICY.md), owner rulings and review gates; do not infer displacement, rewards or civilian labour.|
|Specialised military equipment and operation-card orders|Specific gun/vehicle models and unsourced operation orders remain evidence/review work; ordinary recruitment and the retained Arms-expenditure rules are settled. Every military-operation card gives unit orders from sourced anchors (owner, 1 November 2026, "military openrations must have unit orders"; [later rulings](#later-rulings-1-november-2026)).|

Reception-camp growth into permanent towns, an individually managed economy for every rural settlement, and a detailed enemy supply/reinforcement economy are outside the MVP. They are not unresolved numerical values.
