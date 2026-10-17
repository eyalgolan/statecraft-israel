# Statecraft Israel Game Design

Core systems and player choices
Version 0.2 | 17 October 2026 (version 0.1: 12 October 2026)
Map and location specifications updated 17 October 2026.
The owner's game design of record, based on the original document ([GDD-v0.1.pdf](GDD-v0.1.pdf)) with the additions dated above. The owner's rulings of 12 and 17 October 2026 are at the end. Where an older spec or ruling disagrees, this document wins (`AGENTS.md`).

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
2. Move units, issue orders, start city projects and choose ministers.
3. Select event outcomes.
4. Resolve production, consumption, training, construction and combat.

The order of resolution and the number of actions per turn are open.

### Minister selection TBD

The player can choose ministers. TBD: minister roles, available candidates, appointment rules, costs and effects on gameplay.

### Resources and national parameters

| Resource or parameter | Role | Still to define |
|---|---|---|
| Money | Funds development and other paid actions. Craftsmen earn more money. | Income per job, construction costs and upkeep. |
| Arms | Available military equipment. | Decided, D-G6: "From events (sourced arms deals) and Money. Training and attacks consume it" ([impact plan §5](superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md)). Open, asked of Amir ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231351946), #781): the stock on 15 May 1948 and what one unit is, the price in IL millions, Arms per recruit, and the Attention cost of the buy order. Whether replacements consume it is open. |
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

### Movement and civilian actions

The player can move units. Movement allowance, terrain costs, road and rail effects, stacking, transport and access to enemy territory are open.

A civilian can be employed in an available city employment slot. A civilian can train in a military camp and become a recruit, or train in a Hakhshara (הכשרה) and become a Halutz. The source of a Hakhshara and whether the player builds one are open.

### Progression

Two transitions are defined:
- civilian to recruit, through basic training;
- civilian to Halutz, through pioneer training.

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

### Map editor tool

Provide an internal authoring tool that lets designers inspect and change tiles/hexes and move named locations by dragging or entering hex coordinates. It must support undo/redo, show unsaved changes, and save and reload version-controlled map data with consistent logical and rendered placement.

The full requirements and acceptance workflow are in [Map editor](gdd/map-editor.md). Location identities, sourced reference coordinates, historical names, dates and review metadata remain attached to each location. Moving a location must identify affected designated sites and corridor endpoints without silently changing sourced route geometry. The saved authoring changes must survive map-data regeneration.

The editor's host environment, authoring-data format and exact editable hex-property list are **TBD**. New terrain types remain TBD as above. Campaign movement and construction continue to follow the simulation rules.

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

Open: when is a settlement captured, what constitutes enemy control, and can an occupying force withdraw before the timer ends? Capturing enemy soldiers earns the bonus specified in the MVP objectives. Settlement-control rewards and political penalties remain open.

## Events and consequences

The player responds to events by selecting an outcome that produces a bonus, a penalty, or both. Effects can target money, arms, food, Social cohesion and International Support. Effects on units, cities or project availability are a proposal. Completed MVP objectives also trigger the historical event cards described at the beginning of this document.

Each event needs a trigger, readable context, available options and their consequences. Open: event frequency, mandatory versus optional events, delayed effects, and how much the player can see before choosing.

Historical dates, unit identities, named places and claims in event text require sources. Game balance values and hypothetical alternatives must be identified as design assumptions.

## Production decisions to resolve

Designated building sites and transport corridors bound the locations and routes we must support. Population scale, project timing and the effect of terrain on movement remain major decisions for map production.

| Decision | Why it changes production |
|---|---|
| MVP locations and map scale | The must-have and optional location roster is specified in [locations.md](locations.md). Use the existing grid in [Map units](units.md); any required extent or resolution change and the handling of closely spaced locations remain open. |
| Terrain type (tile/hex type) | MVP: the eight `hexes.json` biomes with a movement cost per biome (Q-MOVE-1, decided). TBD: any new types, their catalogue, classification, visuals, extra properties, construction rules and terrain conversions. |
| Map editor tool | See [Map editor](gdd/map-editor.md) for hex editing, location moves, undo/redo, save/reload, regeneration and reference validation. Host environment, authoring format and editable-property list remain TBD. |
| Road and rail corridors | Routes follow designated corridors. Each corridor needs predefined route geometry and construction states. The corridor list, costs and duration remain open. |
| Unit population scale | A person or a group changes unit count, housing and job assignment, selection, movement and casualty representation. |
| Population role model | Changing one unit between jobs and training differs from separate worker pools. Decide reassignment and return to civilian life. |
| Work and training time | Instant actions and multi-turn projects require different feedback, commitment rules, cancellation and progress displays. |
| Food and arms supply | National pools need fewer logistics rules. Local storage and supply routes add transport, shortages and vulnerability to disruption. |
| Settlement control | Occupation, indirect rule, annexation, displacement and destruction require explicit population, ownership and building states. |

## MVP completion rules

The MVP years, monthly turns and three objectives are fixed above. These remain to decide:
- the map extent, starting unit counts, event count and the session's target play time (asked of Amir, [#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231565826)); the span is 15 May 1948 to 15 January 1952, as above;
- when the war-survival objective is complete, which immigrants must have housing and employment, and which new yishuvim are required.

Each objective needs a specific historical event and reference date for the completion card. For ongoing processes such as immigrant absorption, choose a defined milestone with a sourced date. Bonus amounts, the timing formula, defeat conditions and whether all objectives are required to finish the MVP remain open.

Minister selection is a planned TBD mechanic with player-chosen ministers. The role of coalition, Attention, diplomacy and national policy systems in this core loop remains open.

## Relationship to the existing design

The existing units-in-play design describes units as a presentation layer with no individual orders. This GDD calls for units, jobs, training, construction and settlement control to affect gameplay. Those rules must become simulation state and actions under the existing deterministic simulation architecture.

Reference files from the repository snapshot reviewed on 12 October 2026:
- Original Statecraft Israel design specification
- Units in play design and owner rulings
- Roads and railways implementation notes
- M3 scope rulings

---

## Owner rulings, 2026-10-12

1. **Deport outcome: honest cost, no reward.** It stays as a historical possibility but never pays. It carries large Social cohesion and International Support penalties, creates refugees, has permanent consequences, and is shown soberly through sourced event cards. CONTENT_POLICY §4 is updated to say how this is handled ("no mechanic may reward violence against civilians, from any side").
2. **Adoption: spec and impact plan first.** The GDD is committed as the design of record and analysed against the current code. The phased build plan and open decisions are proposed, and the owner approves before any code changes. Visual and promo work continues meanwhile.

## Owner rulings, 2026-10-17

1. **Map and location additions (B1-B7): approved as written** ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6230935027)): the location roster (approved as 31 must-have and 16 optional; review edit A2 then moved Hadera and Tiberias, already managed cities, to must-have, giving 33 and 14), the West/East Jerusalem split, the new location kinds (reception camp, forts, passes, bridges), the Arab-held towns on the map but not as targets, the Zichron Ya'akov industry, the map editor as a new tool, and the existing grid.
2. **Four further additions: approved** ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233524779)): Kiryat Shmona and Beit Shemesh as designated founding sites, Nir Am's infrastructure-protection objective, Umm Rashrash (future Eilat) as an end-of-war milestone, and Zichron Ya'akov as a must-have location for its vineyards and winery. The sensitivity sign-off for Kiryat Shmona and Beit Shemesh is open (#782); Phase 4's site list follows once it is given.
3. **Session span** (Q-DOC-1, [#784](https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6232838937)): the MVP is 44 monthly turns, 15 May 1948 to 15 January 1952. The session's target play time is open, asked of Amir ([#856](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6231565826)).
