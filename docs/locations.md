# Statecraft: Israel — Locations specification

**Location roster for GDD v0.3.** This file owns inclusion, priority, aliases and facility identity. [The GDD](GDD.md#document-authority) points to the owners of gameplay, costs, production, capacity and objective membership. Priorities are design choices, not historical classifications.

## Inclusion and management

Must-have places comprise the core roster, retained places, required founding markers and facility roster below. Optional markers may remain while bespoke authoring waits. Existing deployment anchors remain context, without automatically creating managed locations.

A marker does not automatically create a managed city, target, objective, population unit or operating facility. Friendly rural work uses [regional capitals](gdd/cities.md); enemy towns use [individual control/taxes](gdd/settlement-control.md) with no player workforce. Proposed partition, wartime reference control, campaign control and armistice lines remain separate layers.

Aliases such as Tsfat/Safed, Zichron/Zikhron Ya'akov, Lod/Lydda and Nablus/Shechem share stable ids. Tel Aviv/Jaffa keep distinct map identities with shared regional administration, not duplicate population. West Jerusalem and East Jerusalem/Old City require distinct control/label bindings; do not duplicate the existing `jerusalem` western record.

## Must-have places

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
| Zichron Ya'akov | Moshava / town | Must-have vineyards/winery theme under shared Industry rules, without extra Food or tax bonuses. |
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
| Sha'ar HaAliyah | Historical reception-camp location | Retain the approved marker/reference [L3]; not another assumed opening-turn camp beyond the operating roster. |
| Kiryat Shmona | Designated founding site | Required northern founding marker; inclusion/sensitivity scope approved, with exact founding-day evidence still to resolve [L4]. |
| Beit Shemesh | Designated founding site | Required founding-site marker; housing context is historical, not automatic reception-camp-to-town progression. Reference verification remains gated [L5]. |
| Ramallah | Town / regional hub | A central-hills route/control anchor north of Jerusalem; no automatic reinforcement rule. |
| East Jerusalem / Old City | City / contested urban area | Represent the other side of divided Jerusalem, civilian population and the Old City's military and political importance; source starting and reference control, then follow campaign ownership. |
| Nablus / Shechem | City / regional hub | Anchor the northern central-hills front and the connections toward Jenin and Tulkarm; a rear-area map node, without automatic reinforcements. |
| Jenin | Town / northern front | Provide a northern front facing the Jezreel Valley and a location for the historical Jenin fighting. |
| Tulkarm | Town / western front | Anchor the central-hills front facing the Sharon coastal plain and connect it with Nablus and Jenin. |
| Hebron | Town / southern-hills hub | Represent population and movement in the southern hills and the route toward Bethlehem and Jerusalem. |
| Gaza City | City / southern coastal hub | Represent a major civilian centre and the Egyptian-held coastal front, with connections north and south. |
| Isdud | Town / coastal front | Add the wartime town near modern Ashdod to the Egyptian coastal route; keep the settlement distinct from the nearby Ad Halom bridge. |
| Al-Majdal | Town / coastal front | Add the wartime town in the area of modern Ashkelon to the coastal route linking Gaza and Isdud. |
| Hadera | Town | Retained must-have population/transport location; economy binds through authored regions. |
| Tiberias | Town | Retained must-have northern development location, not automatically another population pool. |

## Retained existing must-have places

| Location | Required treatment |
| --- | --- |
| Petah Tikva | Preserve existing identity; civilian management binds regionally. |
| Rishon LeZion | Preserve approved winery Industry theme; generic/winery labels cannot double-count a job. |
| Rosh Pinna | Preserve existing northern town identity. |
| Rehovot | Preserve existing town identity. |
| Netanya | Preserve marker and regional farming example; exact boundaries remain authoring work. |
| Acre | Preserve city/control identity. |
| Hanita | Preserve rural marker without a separate workforce screen. |
| Mefalsim | Preserve required designated founding marker and sourced identity. |
| Beit Kama | Preserve required founding marker. |
| Re'im | Preserve required founding marker and founding-only sensitivity scope. |
| Gilat | Preserve required founding marker. |
| Hula drainage site | Preserve approved land-preparation site/event, without unstaffed Food production. |

[Objectives](gdd/objective-card.md) alone owns required membership, references and points. Reuse existing ids when site/place refer to the same identity.

## Facility roster

All are must-have identities. Keys below are proposed bindings/aliases, not assertions that records already exist or every camp operates on the first day. Reception availability and the starting centre's host remain in the [GDD register](GDD.md#deferred-decisions).

| Facility | Proposed stable key | Role and identity boundary |
| --- | --- | --- |
| Tel HaShomer / Tel Litvinsky | `military_tel_litvinsky` | One military camp with aliases; existing formation evidence does not spawn another army unit. |
| Schneller | `military_schneller` | Separately selectable military camp within Jerusalem. |
| Tzrifin / Sarafand | `military_tzrifin` | Military camp, distinct from Sarafand village records. |
| Camp 80 | `military_camp_80` | Military camp, distinct from Pardes Hanna reception. |
| Atlit reception camp | `reception_atlit` | Dedicated reception identity, distinct from nearby settlement/anchors. |
| Rosh HaAyin reception camp | `reception_rosh_haayin` | Required rather than optional; distinct from railway waypoint. |
| Pardes Hanna reception camp | `reception_pardes_hanna` | Share appropriate regional place binding, not Camp 80 capacity. |
| Beit Lid reception camp | `reception_beit_lid` | Do not equate it automatically with another military/Gadna camp. |

The retained owner ruling starts Schneller and Tel HaShomer as military facilities and treats Camp 80/Tzrifin as build sites: [Q-CAMP-2](https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233777137), checked 2026-10-17, with [recorded research](../research_notes/gdd-phase-4-sourcing.md). This does not decide starting army population.

Military camps are separately selectable, highlighted and enlarged; visual offsets never change logical hex or evidence coordinates. Reception facilities are designated tiles or city upgrades. [Training](gdd/training.md) alone owns operation and [balance](gdd/balance.md) amounts. Training centres upgrade reception facilities rather than adding another place roster. Sha'ar HaAliyah retains its approved historical marker without implying another starting facility.

## Optional places

| Location | Kind | Gameplay purpose |
|---|---|---|
| Nazareth | Town | Adds northern settlement administration and control decisions. |
| Gesher | Kibbutz / river crossing | Adds a second eastern defensive area, crossings and reconstruction. |
| Revivim | Kibbutz / remote settlement | Adds isolated Negev settlement development and protection. |
| Ein Harod | Kibbutz | Adds another agricultural and manpower hub alongside the core rural settlements. |
| Nitzanim | Kibbutz | Adds another exposed southern settlement and historically grounded loss-and-recovery content. |
| Mishmar HaEmek | Kibbutz | Agriculture and historical training heritage, not a new operating camp [L6]. |
| Faluja | Town / military area | Adds detail to the southern campaign and pocket-related events. |
| Umm Rashrash / future Eilat | Outpost / expedition objective | Adds the southern expedition and an additional end-of-war milestone (approved by the owner on 17 October 2026). |
| Bethlehem | Town / southern Jerusalem approach | Add another civilian town and road node between Hebron and Jerusalem. |
| Jericho | Town / Jordan Valley approach | Add population and the eastern approaches through the Jordan Valley; an eastern route waypoint. |
| Qalqilya | Town / western central hills | Add another border-area town facing the coastal plain; its dedicated gameplay can follow the Tulkarm front. |
| Khan Yunis | Town / southern coastal route | Add population and another road node between Gaza and Rafah. |
| Rafah | Town / Egypt–Palestine frontier | Add the southern coastal connection to Egypt and route context without automatic reinforcements. |

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
- Both winery themes use shared regional Industry jobs; no duplicate passive Money or Food.
- Reference dates are not automatic unlocks. Site and facility availability have separate owners.
- [Map editor](gdd/map-editor.md) preserves ids/evidence/regional slot dependencies and generation-safe placement overrides. The pending terrain catalogue stays only in [the GDD](GDD.md#terrain--tile--hex-types--tbd).

## Historical sources

Historical identities and positions use the source entries of the [settlement roster](../content/map/settlements_1948.yaml), [localities](../content/map/localities_1948.yaml), [route definitions](../content/map/routes_1948.yaml) and approved feature specifications. These references support historical context; the priorities and gameplay-purpose labels above are design decisions.

- [L1] US Office of the Historian, diplomatic report of 14 May 1948 recording planned Arab Legion deployment to Hebron, Ramallah and Nablus: https://history.state.gov/historicaldocuments/frus1948v05p2/d268, checked 2026-10-17. This records a plan, not proof of an exact garrison or headquarters.
- [L2] Ben-Gurion Archive, diary of 3 June 1948, recording the Jenin and Tulkarm fronts and an Iraqi force arriving from Nablus: https://bengurionarchive.bgu.ac.il/en/node/15260, access remains unverified following the PR review; do not rely on this entry alone for released factual text.
- [L3] Rhona D. Seidelman, Sha'ar HaAliyah research abstract, printed page 930, opening in March 1949: https://www.bgu.ac.il/media/gedf3fo2/bguphd2009.pdf, checked 2026-10-16.
- [L4] Kiryat Shmona municipality, town history and the 1949 founding: https://www.k-8.co.il/היסטוריה/, checked 2026-10-16.
- [L5] Beit Shemesh, early development history and the 1950 camp foundation: https://en.wikipedia.org/wiki/Beit_Shemesh, checked 2026-10-16.
- [L6] Palmach Heritage, Mishmar HaEmek training heritage in 1941–1942; not evidence of an MVP training camp: https://palmach.org.il/history/database/?itemId=5647, checked 2026-10-16.
- [L7] Palmach Heritage, Operation Yoav and the Rafah–Al-Majdal–Isdud coastal front: https://www.palmach.org.il/history/database/?itemId=8493, checked 2026-10-17.
- [L8] Palmach Heritage, Burma Road and the Hulda–Latrun approach: https://www.palmach.org.il/history/database/?itemId=5629, checked 2026-10-16.
- [L9] Palmach Heritage, the Negev water line from Nir Am: https://www.palmach.org.il/history/database/?itemId=8471, checked 2026-10-16.
- [L10] Naharayim at Gesher, visitor-site context for settlement and crossings; corroborating historical evidence remains required: https://naharayim.co.il/הסיור-שלנו/, checked 2026-10-16.
- [L11] War of Independence sites, Nitzanim and its wartime site: https://moreshet-map.org.il/step-3/בלימת-הצבא-המצרי-בגזרת-ציר-החוף/ניצנים-3/, checked 2026-10-16.


Source-check dates are inherited from the branch register, not new human sign-offs. Archive access, page-specific evidence, out-of-period training and visitor-site limitations remain visible. Amir's named-camp inclusion is a design decision; historical operation dates require their own records before scenario release.
