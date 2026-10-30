# Statecraft Israel Game Design

Core systems and player choices
Current version 0.3 | 30 October 2026 (consolidated design proposal)
Previous published baseline metadata:
Version 0.2 | 17 October 2026 (version 0.1: 12 October 2026)
Map and location specifications updated 17 October 2026.
The owner's game design of record, based on the original document ([GDD-v0.1.pdf](GDD-v0.1.pdf)) with the additions dated above. The owner's rulings of 12 and 17 October 2026 are at the end. Where an older spec or ruling disagrees, this document wins (`AGENTS.md`).

This 0.3 revision adds Amir's subsequent design decisions to the current design of record and records the owner's 30 October 2026 conflict decisions. It describes intended changes; it does not claim the existing simulation implements them or that they supersede recorded owner rulings.

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
*Amir's #1668 proposed removing automatic borrowing and automatic Food refill while making Money and workers the construction constraints. The owner's 30 October 2026 conflict review ([conflict decision](#30-october-2026-conflict-decisions)) did not rule on this; the operative main rules are `R-MONEY-003` and `R-FOOD-002` (`docs/rules/mvp.md:399` and `:409`).*

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
* *Considered and declined (owner, 30 Oct 2026; Upkeep conflict, option A; [conflict decision](#30-october-2026-conflict-decisions)): Amir's #1668 proposed housing-only upkeep deterioration. For his reception before civilian work, the review did not rule; the operative main rule is `R-CITY-004` (`docs/rules/mvp.md:105`), with an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`).*
* **Agreed** in the balance catalogue means an explicit interview decision or a retained approved baseline. Unresolved numbers remain TBD rather than receiving assistant-supplied defaults.
* Existing `content/` values, tests, saves and code still describe the earlier implementation where they conflict. Implementation work must migrate them together, update rule-ID coverage and deliberately review hash changes. This documentation revision does not silently change that code or its data.

### 30 October 2026 conflict decisions

On 30 October 2026 the owner reviewed all 13 conflicts between the recorded design and Amir's #1668 proposal and chose to keep every recorded ruling. Each declined note in this additive design links here; it preserves the adjacent ruling rather than reopening or replacing it.

Conflicting #1668 mechanics and catalogue entries retained for traceability are non-operative. The recorded ruling beside each declined note remains authoritative; an unresolved value is `open (owner)` and must not be inferred from the declined proposal.

## Deferred decisions

These are recorded once here. Feature files link here instead of maintaining competing open-question lists.

|Decision|Status / boundary|
|-|-|
|Starting army size outside Historical mode|Historical mode keeps the sourced seed from `content/map/units_1948.yaml` (`Q-START-1`, decided 15 October 2026). Parameter `A` applies only to non-Historical modes and remains deferred; no previously proposed count is accepted (owner, 30 Oct 2026).|
|Regional-capital model|**Amir's proposal, declined by the owner on 30 October 2026:** create a regional-capital roster and shared regional administration. The retained per-city rules remain authoritative.|
|Founding dates|The owner set Kiryat Shmona to 18 July 1949 and Beit Shemesh to 15 December 1950 under the monthly turn ([#785](https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6237624501)).|
|Facility availability and starting training-centre host|The existing owner ruling already starts Schneller and Tel HaShomer as military facilities; the other military camps are approved build sites. Verify reception-camp availability and choose the starting reception/centre host without assuming every roster entry operates on the opening turn. Centre and teacher counts are decided.|
|Final defeat conditions|Under `gdd-0.3` the retained defeat is the economic one (Money and Food exhausted); losing a city or region alone does not end the game ([rules](rules/mvp.md#defeat)), and [the retained end rules](gdd/ministers-events-end.md#rules) add no coalition collapse. *Considered and declined (owner, 30 Oct 2026; [conflict decision](#30-october-2026-conflict-decisions)): Amir's #1668 proposed making every terminal defeat TBD.*|
|Minister-specific gameplay|The existing office-competence slice (`R-MIN-001` to `R-MIN-003` and `Cabinet.Appoint`) is the complete MVP minister mechanic ([owner ruling #1513](https://github.com/eyalgolan/statecraft-israel/issues/1513#issuecomment-6372357178)). Historical offices and event references remain, and the existing cabinet stays; under `gdd-0.3` there is no Attention resource ([rules](rules/mvp.md#government)).|
|Terrain presentation|**TBD**. The MVP catalogue itself remains frozen at the existing eight biomes.|
|Novice profession lock-in|Trained civilian roles can return and be reassigned while keeping their qualification ([owner ruling #1511](https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6372356482)). Amir's conflicting permanent, mutually exclusive profession proposal was declined on 30 October 2026.|
|Sensitive capture outcomes|Raid and Deport remain subject to the existing [content policy](CONTENT_POLICY.md), owner rulings and review gates; do not infer displacement, rewards or civilian labour.|
|Specialised military equipment and operation-card orders|Specific gun/vehicle models and unsourced operation orders remain evidence/review work; ordinary recruitment and the retained Arms-expenditure rules are settled.|

Reception-camp growth into permanent towns, an individually managed economy for every rural settlement, and a detailed enemy supply/reinforcement economy are outside the MVP. They are not unresolved numerical values.
