# Statecraft Israel — Locations specification

Updated 17 October 2026 | MVP scope: 1948–1951
**Location roster for GDD v0.3.** This file owns inclusion, priority, aliases and facility identity. [The GDD](GDD.md#document-authority) points to the owners of gameplay, costs, production, capacity and objective membership. Priorities are design choices, not historical classifications.

This companion to the [Game Design Document](GDD.md) defines the location roster, production priorities and location data requirements. New terrain/hex types beyond the MVP's eight biomes remain TBD in the GDD; authoring requirements are in [Map editor](gdd/map-editor.md).

## MVP location scope
The approved production-priority roster comprises the 33 must-have and 14 optional locations below. Existing supported places and required founding markers keep their prior identities and rules but do not join the must-have roster unless the table lists them. Existing deployment anchors remain context, without automatically creating managed locations. Amir's proposal to make every listed facility must-have was declined by the owner on 30 October 2026 ([conflict decision](GDD.md#30-october-2026-conflict-decisions)).

The MVP map must support the 33 must-have locations below. The 14 optional locations can retain existing markers or shared functionality while additional authoring waits. These are map-production priorities: a fort, pass or bridge does not require a full city-management panel, and map inclusion does not make capture or construction of every location a victory condition.

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed promoting additional existing-place and facility identities into production-priority scope beyond the approved 33 must-have / 14 optional roster.*
A marker does not automatically create a managed city, target, objective, population unit or operating facility. **Amir's proposal, declined by the owner on 30 October 2026 ([conflict decision](GDD.md#30-october-2026-conflict-decisions)):** friendly rural work would use regional capitals, and enemy towns would use individual taxes with no player workforce. Proposed partition, wartime reference control, campaign control and armistice lines remain separate layers.

The roster supplements existing supported map records and approved city, site and objective decisions. It does not remove locations already implemented or replace the existing Mefalsim slice and its objective. In particular, the Zichron Ya'akov entry adds an industry location to the map scope; it does not move the winery already specified for Rishon LeZion in [Cities and absorption](gdd/cities.md). Tel Aviv and Jaffa remain distinct map locations while sharing the approved Tel Aviv–Yafo city economy; the roster does not create a second population or immigrant-allocation pool for Jaffa. City balance remains in that feature section, and designated-site availability and objective rules remain in [Build yishuv](gdd/build-yishuv.md), [Corridors](gdd/corridors.md) and their referenced plans.
Aliases such as Tsfat/Safed, Zichron/Zikhron Ya'akov, Lod/Lydda and Nablus/Shechem share stable ids. Tel Aviv/Jaffa keep distinct map identities while sharing the approved Tel Aviv–Yafo city economy, not duplicate population. West Jerusalem and East Jerusalem/Old City require distinct control/label bindings; do not duplicate the existing `jerusalem` western record.

Also kept, as existing supported places: Petah Tikva, Rishon LeZion, Rosh Pinna, Rehovot, Netanya, Acre, Hanita.

The gameplay-purpose column states the design intent for each place. Exact starting population, jobs, garrisons, reinforcement behavior, project parameters and supply rules require their own sourced facts or labelled design values.

## Must-have locations: 33 places

| Location | Kind | Gameplay purpose |
|---|---|---|
| Tel Aviv | City | Main population, employment and economic centre. |
| Jaffa | Town / urban area | Keep a distinct map identity and historical context alongside Tel Aviv, within the existing combined Tel Aviv–Yafo city economy. |
| Haifa | City and port | Industry, overseas arrivals and immigrant absorption. |
| West Jerusalem | City | Civilian needs and the vulnerable connection to the coast; represent the city's divided control. |
| Lod | Town | Central transport connections and settlement-control decisions. |
| Ramla | Town | A separate settlement within the same central operational area as Lod. |
| Beersheba | Town | Southern military and civilian development hub. |
| Tsfat / Safed | Town | Northern population centre, employment and rebuilding projects. |
| Zichron Ya'akov | Moshava / town | Location-specific industry: vineyards and a winery (approved by the owner on 17 October 2026). Balance and integration with the existing winery feature remain open. |
| Degania | Kibbutz | Agriculture and defence on the Jordan Valley front. |
| Negba | Kibbutz | Southern defence, farming and competing manpower needs. |
| Yad Mordechai | Kibbutz | An exposed southern coastal settlement to supply and defend. |
| Hulda | Kibbutz / staging point | Settlement and staging point for the Jerusalem corridor and Burma Road project. |
| Nir Am | Kibbutz / infrastructure site | Negev water infrastructure and an infrastructure-protection objective (approved by the owner on 17 October 2026). |
| Nahalal | Moshav | An agricultural settlement where food production competes with drafting and other work. |
| Latrun | Fort / strategic position | A strongpoint affecting the Jerusalem connection and the decision to build a bypass. |
| Sha'ar HaGai | Pass / route waypoint | A required geographical anchor for the Jerusalem supply corridor. |
| Ad Halom | Bridge / strategic position | A southern approach to defend; keep it distinct from a managed town. |
| Iraq Suwaydan fort | Fort / strategic position | A southern strongpoint supporting a meaningful military objective. |
| Sha'ar HaAliyah | Immigrant reception camp | Immigrant reception and resettlement; historical opening reference: 1949 [L3]. |
| Kiryat Shmona | Designated founding site | Northern settlement and immigrant absorption; historical foundation reference: 1949 [L4]. Approved by the owner on 17 October 2026; the sensitivity sign-off is open (#782). |
| Beit Shemesh | Designated founding site | Camp-to-town development and housing; historical foundation reference: 1950 [L5]. Approved by the owner on 17 October 2026; the sensitivity sign-off is open (#782). |
| Ramallah | Town / regional hub | A central-hills movement and reinforcement node north of Jerusalem; connect it to the Jerusalem and western approaches. |
| East Jerusalem / Old City | City / contested urban area | Represent the other side of divided Jerusalem, civilian population and the Old City's military and political importance; source starting and reference control, then follow campaign ownership. |
| Nablus / Shechem | City / regional hub | Anchor the northern central-hills front and the connections toward Jenin and Tulkarm; a rear-area reinforcement node. |
| Jenin | Town / northern front | Provide a northern front facing the Jezreel Valley and a location for the historical Jenin fighting. |
| Tulkarm | Town / western front | Anchor the central-hills front facing the Sharon coastal plain and connect it with Nablus and Jenin. |
| Hebron | Town / southern-hills hub | Represent population and movement in the southern hills and the route toward Bethlehem and Jerusalem. |
| Gaza City | City / southern coastal hub | Represent a major civilian centre and the Egyptian-held coastal front, with connections north and south. |
| Isdud | Town / coastal front | Add the wartime town near modern Ashdod to the Egyptian coastal route; keep the settlement distinct from the nearby Ad Halom bridge. |
| Al-Majdal | Town / coastal front | Add the wartime town in the area of modern Ashkelon to the coastal route linking Gaza and Isdud. |
| Hadera | Town (existing managed city, Phase 3) | Managed town on the railway corridor. |
| Tiberias | Town (existing managed city, Phase 3) | Northern population and development centre. |

The Zichron Ya'akov row records the owner's 17 October 2026 approval of Zichron as a must-have map location for its vineyards and winery. It does not approve a second implemented winery: the owner ruled on 20 October 2026 that the one specialised industry is the winery in Rishon LeZion, replacing the 18 October Zikhron Ya'akov default; the [document-authority rule](GDD.md#document-authority) therefore keeps the Rishon LeZion winery without an automatic second implementation.

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed a Zichron Ya'akov vineyards/winery theme under shared Industry rules without extra Food or tax bonuses.*

## Existing supported places and approved sites

| Location | Treatment |
| --- | --- |
| Petah Tikva | Preserve existing identity and retained per-city management. |
| Rishon LeZion | Preserve approved winery Industry theme; generic/winery labels cannot double-count a job. |
| Rosh Pinna | Preserve existing northern town identity. |
| Rehovot | Preserve existing town identity. |
| Netanya | Preserve its existing supported-place identity. |
| Acre | Preserve city/control identity. |
| Hanita | Preserve rural marker without a separate workforce screen. |
| Mefalsim | Preserve required designated founding marker and sourced identity. |
| Beit Kama | Preserve required founding marker. |
| Re'im | Preserve required founding marker and founding-only sensitivity scope. |
| Gilat | Preserve required founding marker. |
| Hula drainage site | Preserve the approved drainage site/event and retained field-site rules. |

[Objectives](gdd/objective-card.md) alone owns required membership, references and points. Reuse existing ids when site/place refer to the same identity.

## Facility roster

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed making every listed facility must-have and Rosh HaAyin required rather than optional, with proposed keys and unresolved availability/host bindings.*

| Facility | Proposed stable key | Role and identity boundary |
| --- | --- | --- |
| Tel HaShomer / Tel Litvinsky | `military_tel_litvinsky` | One military camp with aliases; existing formation evidence does not spawn another army unit. |
| Schneller | `military_schneller` | Separately selectable military camp within Jerusalem. |
| Tzrifin / Sarafand | `military_tzrifin` | Military camp, distinct from Sarafand village records. |
| Camp 80 | `military_camp_80` | Military camp, distinct from Pardes Hanna reception. |
| Atlit reception camp | `reception_atlit` | Dedicated reception identity, distinct from nearby settlement/anchors. |
| Rosh HaAyin reception camp | `reception_rosh_haayin` | Amir's proposal, declined by the owner 30 Oct 2026 (Keep ruling): required rather than optional. Rosh HaAyin remains in the approved [optional-location roster](#optional-locations-14-places). |
| Pardes Hanna reception camp | `reception_pardes_hanna` | Keep its reception identity separate from Camp 80 capacity. |
| Beit Lid reception camp | `reception_beit_lid` | Do not equate it automatically with another military/Gadna camp. |

The retained owner ruling starts Schneller and Tel HaShomer as military facilities and treats Camp 80/Tzrifin as build sites: [Q-CAMP-2](https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233777137), checked 2026-10-17, with [recorded research](../research_notes/gdd-phase-4-sourcing.md). This does not decide starting army population. The starting training centre and reception site is Kiryat Shmuel, Haifa, an owner design value (owner, 26 October 2026; confirmed 1 November 2026, O8 and 27M-25, [answers](GDD.md#1-november-2026-answers)); it has no roster row yet and needs a sourced location record before it is authored. Every military camp uses the same `military.camp_capacity`, with no per-camp exceptions (27M-25).

Military camps are separately selectable, highlighted and enlarged; visual offsets never change logical hex or evidence coordinates. Reception facilities are designated tiles or city upgrades. [Training](gdd/training.md) alone owns operation and [balance](gdd/balance.md) amounts. Training centres are separate facilities; Amir's proposed dependency on an operating reception camp is non-operative. Sha'ar HaAliyah retains its approved historical marker without implying another starting facility.

## Optional locations: 14 places

| Location | Kind | Gameplay purpose |
|---|---|---|
| Nazareth | Town | Adds northern settlement administration and control decisions. |
| Gesher | Kibbutz / river crossing | Adds a second eastern defensive area, crossings and reconstruction. |
| Revivim | Kibbutz / remote settlement | Adds isolated Negev settlement development and protection. |
| Ein Harod | Kibbutz | Adds another agricultural and manpower hub alongside the core rural settlements. |
| Nitzanim | Kibbutz | Adds another exposed southern settlement and historically grounded loss-and-recovery content. |
| Mishmar HaEmek | Kibbutz | Additional agriculture and military-training heritage [L6]. |
| Rosh HaAyin immigrant camp | Immigrant reception camp | Adds another immigrant-arrival and housing location beyond the core absorption sites. |
| Faluja | Town / military area | Adds detail to the southern campaign and pocket-related events. |
| Umm Rashrash / future Eilat | Outpost / expedition objective | Adds the southern expedition and an additional end-of-war milestone (approved by the owner on 17 October 2026). |
| Bethlehem | Town / southern Jerusalem approach | Add another civilian town and road node between Hebron and Jerusalem. |
| Jericho | Town / Jordan Valley approach | Add population and the eastern approaches through the Jordan Valley; a reinforcement-route waypoint. |
| Qalqilya | Town / western central hills | Add another border-area town facing the coastal plain; its dedicated gameplay can follow the Tulkarm front. |
| Khan Yunis | Town / southern coastal route | Add population and another road node between Gaza and Rafah. |
| Rafah | Town / Egypt–Palestine frontier | Add the southern coastal connection to Egypt and more detail for Egyptian reinforcement routes. |

## Location records and historical state

- Give every location a stable identity, a kind, a period-appropriate display name, its map position and hex binding, its MVP priority, and the sources and review state for historical fields. Track whether it is a marker, a managed city, a designated site or a settlement-control target separately from its map priority. Reuse the existing [settlement roster](../content/map/settlements_1948.yaml) and [localities](../content/map/localities_1948.yaml) identities where available; aliases in the tables do not create duplicate places.
- Distinguish settlements, immigrant reception camps, designated founding sites and military or infrastructure positions. Civilian residents and military forces keep separate states. A town and an adjacent fort or bridge remain separate records even where the map scale puts them in the same area.
- Keep sourced geographical coordinates and reference evidence separate from authored gameplay placement. Both the logical hex binding and the displayed placement must refer to the same authored location.
- Model period-appropriate names and starting control. The proposed Arab-state boundaries, historical wartime control and armistice lines are separate map concepts. Sourced history defines starting and reference states; subsequent campaign ownership follows the settlement-control rules and player or computer actions. Arab-held locations keep separate civilian and military state; the roster does not imply a single army. The deployment plans in [L1] and the wartime account in [L2] provide context, not exact garrison sizes. The existing curated-target decision for settlement control remains separate from map inclusion; naming a movement or reinforcement node does not introduce automatic reinforcements or supply mechanics.
- Use the wartime positions and names of Isdud and Al-Majdal. Modern Ashdod and Ashkelon are orientation aids. Check original versus later settlement sites when authoring Gesher and Nitzanim.
- Historical opening and founding dates are references for history and completion cards. The availability of a designated site follows the applicable site rules; the player can complete a project earlier than its historical reference date. Do not turn the reference years in the roster into new automatic unlock dates.
- Use shared settlement models and management rules. Distinctive assets are reserved for useful landmarks such as a harbour, winery, fort or immigrant camp.

The Burma Road and Tel Aviv–Haifa railway remain required construction corridors under the MVP objectives. Their route geometry, segments and progress are separate from location records. The railway objective is the 1949 Haifa–Tel Aviv link via Rosh HaAyin, not the later coastal line: Q-RAIL-1, decided by the owner on 17 October 2026 ([#782](https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128)); see [Corridors](gdd/corridors.md). Hulda, Latrun, Sha'ar HaGai and Jerusalem anchor the Jerusalem connection. Military camps and Hakhsharot are facilities at designated locations under the training and pioneer rules.

## Existing deployment anchors: context, not additional training facilities

Unit-art data names places beyond the production roster. Preserve those anchors without automatically adding training capacity or management screens; this explains the apparent mismatch with [units.md](units.md).

| Existing anchor | MVP treatment |
| --- | --- |
| Yavne'el | Historical deployment context. |
| Julis camp | Deployment context, not extra recruit capacity. |
| Mishmar HaYarden | Bridgehead/position context. |
| Netanya, Camp Dora | Deployment within Netanya, not another playable training camp. |
| Gedera | Unit-placement context. |
| Ein Shemer | Deployment context without inferred reception/training. |
| Ekron / RAF Aqir | Aviation anchor, not playable air units or extra military training. |
| Herzliya strip | Aviation/route context. |
| Hatzor | Aviation anchor. |
| Samakh | Force-position context. |
| Al-Malikiyya | Force-position context retaining its sourcing/review requirements. |

These anchors/evidence come from [units_1948.yaml](../content/map/units_1948.yaml), not new verification. Reuse already-listed place identities; formation names/garrison strength remain unit evidence rather than location rules.

## Location record contract

- Use one stable id with aliases, period names, kind, priority and explicit marker/site/facility/region/target bindings. Reuse [settlement](../content/map/settlements_1948.yaml) and [locality](../content/map/localities_1948.yaml) ids for the same place.
- Keep sourced coordinates/evidence/review metadata separate from authored hex/visual placement. Towns, forts, bridges and camps remain distinct even on shared hexes.
- Shared hexes never automatically merge resident population, taxes or control. Lod/Ramla and Rishon/Rehovot may remain separately authored and selectable.
- Use wartime Isdud/Al-Majdal; modern Ashdod/Ashkelon are orientation aliases, not automatic same-site substitutions. Check original/later Gesher/Nitzanim sites and retain the Ad Halom period-name/date verification requirement.
- Strategic/reinforcement-route labels create no garrisons, supply, reinforcements or bonus output. Explicit scenario values require evidence or design-value labels.
- **Amir's proposal, declined by the owner on 30 October 2026 ([conflict decision](GDD.md#30-october-2026-conflict-decisions)):** both winery themes would use shared regional Industry jobs with no duplicate passive Money or Food. Rishon LeZion remains the approved specialised winery under the sourced [city-project ruling](gdd/cities.md#rules).
- Reference dates are not automatic unlocks. Site and facility availability have separate owners.
- [Map editor](gdd/map-editor.md) preserves ids, evidence and generation-safe placement overrides. The pending terrain catalogue stays only in [the GDD](GDD.md#terrain-type-tilehex-type-new-types-tbd).

## Historical sources

Historical identities and positions use the source entries of the [settlement roster](../content/map/settlements_1948.yaml), [localities](../content/map/localities_1948.yaml), [route definitions](../content/map/routes_1948.yaml) and approved feature specifications. These references support historical context; the priorities and gameplay-purpose labels above are design decisions.

- [L1] US Office of the Historian, diplomatic report of 14 May 1948 recording planned Arab Legion deployment to Hebron, Ramallah and Nablus: https://history.state.gov/historicaldocuments/frus1948v05p2/d268, checked 2026-10-17. This records a plan, not proof of an exact garrison or headquarters.
- [L2] Ben-Gurion Archive, diary of 3 June 1948, recording the Jenin and Tulkarm fronts and an Iraqi force arriving from Nablus: https://bengurionarchive.bgu.ac.il/en/node/15260, checked 2026-10-17.
- [L3] Rhona D. Seidelman, Sha'ar HaAliyah research abstract, opening in March 1949: https://www.bgu.ac.il/media/gedf3fo2/bguphd2009.pdf, checked 2026-10-16.
- [L4] Kiryat Shmona municipality, town history and the 1949 founding: https://www.k-8.co.il/היסטוריה/, checked 2026-10-16.
- [L5] Beit Shemesh, early development history and the 1950 camp foundation: https://en.wikipedia.org/wiki/Beit_Shemesh, checked 2026-10-16.
- [L6] Palmach Heritage, Mishmar HaEmek and its military-training background: https://palmach.org.il/history/database/?itemId=5647, checked 2026-10-16.
- [L7] Palmach Heritage, Operation Yoav and the Rafah–Al-Majdal–Isdud coastal front: https://www.palmach.org.il/history/database/?itemId=8493, checked 2026-10-17.
- [L8] Palmach Heritage, Burma Road and the Hulda–Latrun approach: https://www.palmach.org.il/history/database/?itemId=5629, checked 2026-10-16.
- [L9] Palmach Heritage, the Negev water line from Nir Am: https://www.palmach.org.il/history/database/?itemId=8471, checked 2026-10-16.
- [L10] Naharayim at Gesher, historical settlement and crossings: https://naharayim.co.il/הסיור-שלנו/, checked 2026-10-16.
- [L11] War of Independence sites, Nitzanim and its wartime site: https://moreshet-map.org.il/step-3/בלימת-הצבא-המצרי-בגזרת-ציר-החוף/ניצנים-3/, checked 2026-10-16.
