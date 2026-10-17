# Statecraft Israel — Locations specification

Updated 17 October 2026 | MVP scope: 1948–1951

This companion to the [Game Design Document](GDD.md) defines the location roster, production priorities and location data requirements. New terrain/hex types beyond the MVP's eight biomes remain TBD in the GDD; authoring requirements are in [Map editor](gdd/map-editor.md).

## MVP location scope

The MVP map must support the 33 must-have locations below. The 14 optional locations can retain existing markers or shared functionality while additional authoring waits. These are map-production priorities: a fort, pass or bridge does not require a full city-management panel, and map inclusion does not make capture or construction of every location a victory condition.

The roster supplements existing supported map records and approved city, site and objective decisions. It does not remove locations already implemented or replace the existing Mefalsim slice and its objective. In particular, the Zichron Ya'akov entry adds an industry location to the map scope; it does not move the winery already specified for Rishon LeZion in [Cities and absorption](gdd/cities.md). Tel Aviv and Jaffa remain distinct map locations while sharing the approved Tel Aviv–Yafo city economy; the roster does not create a second population or immigrant-allocation pool for Jaffa. City balance remains in that feature section, and designated-site availability and objective rules remain in [Build yishuv](gdd/build-yishuv.md), [Corridors](gdd/corridors.md) and their referenced plans.

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
