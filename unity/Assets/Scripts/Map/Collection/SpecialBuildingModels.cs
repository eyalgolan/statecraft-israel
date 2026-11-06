#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Issue #707: Tal Heuberger's single-building models at sourced places, and the tank stopped at Degania Alef. They use the collection stream after the
    /// first frame, draw only close up (#700's switch, TownModelLayer.CloseEnterM / CloseExitM), follow the game date and
    /// fail open. #1701 slice F (owner, 27 October 2026: "Arms workshops (Recommended)"): under gdd-0.3, b21 stands on the hex of
    /// each built arms workshop (<see cref="ArmsWorkshops"/>). Tal Heuberger's four
    /// props stand at their sourced places and dates too (owner, 2 November 2026: "Place all four now (Recommended)"; the
    /// option: "Wire each at its sourced place and dates, with the sources in the data rows marked agent-read"): the well at
    /// Abraham's Well, Beersheba; the sandbags on the Tel Aviv shore on the Altalena's turn only; the crates on the Haifa port
    /// quay and the low wall by the Jaffa-Jerusalem road near Abu Ghosh, on the 1948 turns.</summary>
    [ExecuteAlways]
    public sealed class SpecialBuildingModels : MonoBehaviour
    {
        public const string Name = "SpecialBuildingModels";
        public const string Shikun = "b11-shikun-housing-v2", Tegart = "b12-tegart-fort-v2",
            Barracks = "b18-military-barracks-v2", Workshop = "b21-artisan-workshop-v2",
            Factory = "b22-industrial-factory-v2", SyrianR35 = CollectionCatalog.SyrianR35,
            Well = CollectionCatalog.StoneWell, Sandbags = CollectionCatalog.Sandbags, Crates = CollectionCatalog.CrateStack,
            Wall = CollectionCatalog.StoneWall;

        /// <summary>The one world scale of Tal Heuberger's building models: <see cref="Kits.SettlementKits.ModelScale"/> (issue
        /// #1032).</summary>
        public const float WorldScale = Kits.SettlementKits.ModelScale;   // #1032: one shared constant

        /// <summary>Design values, not sourced measurements: the real long side (m) each model stands for, so that at
        /// <see cref="WorldScale"/> the fort, the barracks, the factory and the shikun block are each a few locomotive cars
        /// long and shorter than the whole train, and the workshop is a little over one.</summary>
        public static float RealLengthM(string model) => model switch
        {
            Tegart => 30f, Barracks => 25f, Factory => 30f, Shikun => 30f, Workshop => 12f,
            // Sourced, unlike the buildings' design values: the Renault R35's length, 4.02 m (en.wiki Renault R35, infobox,
            // checked 2026-10-28; excerpt in research_notes/Map/degania-r35.md).
            SyrianR35 => 4.02f,
            // Sourced: Abraham's Well's diameter, 3.75 m (he.wiki "באר אברהם", revision 44049275, agent-read 2026-11-02, a
            // person's check pending). The other three props are DESIGN VALUES, typical of the kind: a 4 m sandbag post, a 3 m
            // crate stack, an 8 m run of low wall.
            Well => 3.75f, Sandbags => 4f, Crates => 3f, Wall => 8f,
            _ => throw new ArgumentException($"'{model}' is not one of #707's models", nameof(model)),
        };

        /// <summary>The model's long side on the map (m), seen from above: its real size at the world scale, times its row's own
        /// scale (<see cref="Want.Scale"/>).</summary>
        public static float SizeOf(string model) => RealLengthM(model) * WorldScale * ScaleOf(model);

        /// <summary>The scale over <see cref="WorldScale"/> of <paramref name="model"/>'s rows (1 for a model with none).</summary>
        public static float ScaleOf(string model)
        {
            foreach (var w in All) if (w.Model == model) return w.Scale;
            return 1f;
        }

        /// <summary>The owner, 2 November 2026, on #2274: "Bigger, closer (Recommended)" ("Draw these four at about 3x so they read
        /// at close zoom, and let them stand at the town's edge right by the sourced spot"): the scale of Tal Heuberger's four
        /// props over <see cref="WorldScale"/>. At the world scale alone they were about 10 px at the closest zoom.</summary>
        public const float PropScale = 3f;

        /// <summary>The farthest (m) a town-edge prop may stand from its sourced point (owner on #2274: "right by the sourced
        /// spot"): a spot farther out is not drawn, with a warning.</summary>
        public const float MaxPropOffsetM = 1500f;

        /// <summary>The cap in force: <see cref="MaxPropOffsetM"/>, which a test may lower (a test seam).</summary>
        internal static float MaxPropOffset = MaxPropOffsetM;

        /// <summary>DESIGN VALUE read off the wall model (generated-1948-12-low-stone-wall-module.fbx): its depth over its length seen
        /// from above, 0.1597 (vertex extents 0.9995 by 0.1597 FBX units, 2 November 2026), so its roadside spot is planned from
        /// data, not from the streamed prefab.</summary>
        public const float WallDepthShare = 0.1597f;

        /// <summary>A row's long side on the map (m): its model's real size at the world scale times the row's own scale.</summary>
        public static float SizeOf(Want w) => RealLengthM(w.Model) * WorldScale * w.Scale;

        /// <summary>The disc a row's model keeps clear (m): its half diagonal, as if square.</summary>
        public static float ClearanceOf(Want w) => 0.71f * SizeOf(w);

        /// <summary>The disc a placed model keeps clear (m): its half diagonal, as if square.</summary>
        public static float ClearanceOf(string model) => 0.71f * SizeOf(model);

        // Design value: the bounded deterministic search around each sourced point.
        public const int SpotAttempts = 24;

        /// <summary>DESIGN VALUES: when no spot of <see cref="SeededSpot"/>'s ring is clear, the ring moves out by
        /// <see cref="WidenShare"/> clearances (the width one ring covers, 1.5 to 3.5 clearances out) up to
        /// <see cref="WidenRings"/> times, so a model whose sourced point is ringed by routes and kits (Abraham's Well, in
        /// Beersheba) still stands at the nearest clear ground. A model whose first ring has a clear spot is unchanged.</summary>
        public const float WidenShare = 2f;
        public const int WidenRings = 20;

        /// <summary>How a model's spot is found from its sourced point: the seeded ring search round it (<see cref="SeededSpot"/>),
        /// on the beach of a SeaCraft coast (<see cref="ShoreSpot"/>), or beside a route, along it (<see cref="RoadSpot"/>).</summary>
        public enum Siting { Seeded, Shore, Road }

        public readonly struct Want
        {
            public readonly string Key, Model, Source; public readonly DateTime From; public readonly double Lon, Lat;
            /// <summary>The first day it is no longer drawn, or null: drawn from <see cref="From"/> on.</summary>
            public readonly DateTime? To;
            public readonly Siting Site;
            /// <summary>The row's own scale over <see cref="WorldScale"/> (1, or <see cref="PropScale"/> for the four props), and
            /// whether it may stand at a town's edge, inside its kit footprint, clear of the town's building models, the tokens in
            /// the towns and the name points instead (the four props; owner on #2274: "Bigger, closer").</summary>
            public readonly float Scale; public readonly bool TownEdge;
            /// <summary>Whether its bundle streams only while a camera is close to it (the residency policy's enter and exit
            /// distances round its sourced point), instead of map-wide on its dates: the four props (owner, 2 November 2026, on
            /// #2274: "Near-only for these props (Recommended)"). Every other special keeps its map-wide request (owner ruling 29,
            /// 30 October 2026: "Far models map-wide (Recommended)").</summary>
            public readonly bool NearOnly;
            /// <summary>The SeaCraft coast's place (<see cref="Siting.Shore"/>) or the route's id (<see cref="Siting.Road"/>).</summary>
            public readonly string Along;
            /// <summary>A workshop's city (CityProject.City; <see cref="ArmsWorkshops"/>), "" for every other row: in a major city
            /// it stands at the city's edge (<see cref="EdgeSpot"/>).</summary>
            public readonly string City;
            public Want(string key, string model, DateTime from, double lon, double lat, string source)
                : this(key, model, from, null, lon, lat, source, Siting.Seeded, "") { }
            public Want(string key, string model, DateTime from, DateTime? to, double lon, double lat, string source, Siting site, string along,
                float scale = 1f, bool townEdge = false, bool nearOnly = false, string city = "")
            { Key = key; Model = model; From = from; To = to; Lon = lon; Lat = lat; Source = source; Site = site; Along = along; Scale = scale; TownEdge = townEdge; NearOnly = nearOnly; City = city; }
            public bool Covers(DateTime date) => date >= From && (To == null || date < To.Value);
        }

        /// <summary>The Altalena card's window (content/events/altalena.yaml, from 15 June to 23 June 1948, owner Q-CUT-7), the turn
        /// that holds the night of 21-22 June when the sandbag post on the Tel Aviv shore fired on the ship's boat; the same window
        /// SeaAirCollection draws the Altalena off Tel Aviv in (a test holds the two equal).</summary>
        public static readonly DateTime SandbagsFrom = new DateTime(1948, 6, 15), SandbagsTo = new DateTime(1948, 6, 23);

        /// <summary>The 1948 turns, for the two props whose sources give the year only (the crates, the wall).</summary>
        public static readonly DateTime Year1948From = new DateTime(1948, 5, 15), Year1948To = new DateTime(1949, 1, 1);

        /// <summary>The route the low wall stands beside (content/map/routes_1948.yaml).</summary>
        public const string WallRoute = "jaffa_jerusalem_road";

        /// <summary>Abu Ghosh, 31.80472 N, 35.11083 E (en.wiki "Abu Ghosh", revision 1381085214, agent-read 2026-11-02): not a
        /// content place, so the wall's point. Its spot is on <see cref="WallRoute"/> nearest this point.</summary>
        public const double AbuGhoshLon = 35.11083, AbuGhoshLat = 31.80472;

        /// <summary>Facts already sourced in the repository. Latrun's police fort (the Arab Legion's gun on the roof of "the
        /// Latrun Tegart fort, 1948", unit-art sourcing note; the Legion held Latrun from 18 May 1948, routes_1948.yaml) stands
        /// at the Latrun point of routes_1948.yaml (en.wiki Latrun), from the game's first day: it was built in the
        /// Mandate's 1940-41 police-station programme (en.wiki Tegart fort, checked 18 October 2026). Tzrifin and Camp 80
        /// use sites.yaml's sourced dates and coordinates. Netanya's industry: the Jerusalem Post report of 28 July 1950 in
        /// the Phase 4 sourcing note (waypoint N5) puts "the area set aside for the new industrial zone" at the town's
        /// entrance, and Netanya's Kiryat Eliezer Kaplan industrial zone was inaugurated in 1949 (en.wiki Netanya, checked
        /// 18 October 2026). Ramat HaHayal's shikun: most of its families moved in during 1950 (Balslev, Iyunim Bitkumat
        /// Israel 23, p. 247, in the shikun research note), and the owner ruled on 18 October 2026 ("Ramat HaHayal, 1950",
        /// #1068) that the year-level date starts on 1 January 1951; its point is Wikidata Q2776362's (checked 2026-10-18).
        /// Degania's tank (owner, 27 October 2026: "Degania landmark (Recommended)"): Tal Heuberger's Syrian Renault R35 stands at
        /// Degania Alef from the day of the attack, 20 May 1948: the Syrian attack of that dawn, "an R35 that broke through the
        /// fence and was stopped (it stands there today)" (JVL, credited to the IDF, in content/events/degania_defence.yaml),
        /// and en.wiki Renault R35 ("five R35s took part in an unsuccessful Syrian Army attack on the Jewish kibbutz Degania
        /// Alef" on 20 May 1948; one disabled R35 still stands near the kibbutz as a memorial; checked 2026-10-28). Its point
        /// is Degania Alef's row in content/map/settlements_1948.yaml (en.wiki Degania Alef). The excerpts are in
        /// research_notes/Map/degania-r35.md.</summary>
        static readonly Want[] All =
        {
            new Want("latrun_tegart", Tegart, new DateTime(1948, 5, 15), 34.98139, 31.83194,
                "content/map/routes_1948.yaml; research_notes/GDD phase 5/unit-art-sourcing-1948.md"),
            new Want("tzrifin_barracks", Barracks, new DateTime(1948, 5, 19), 34.8395, 31.9587, "content/sites.yaml: tzrifin"),
            new Want("camp_80_barracks", Barracks, new DateTime(1948, 7, 13), 34.9866, 32.4628, "content/sites.yaml: camp_80"),
            new Want("netanya_factory", Factory, new DateTime(1950, 7, 28), 34.8567, 32.3286,
                "research_notes/gdd-phase-4-sourcing.md: N5; https://en.wikipedia.org/wiki/Netanya (checked 2026-10-18)"),
            new Want("ramat_hahayal_shikun", Shikun, new DateTime(1951, 1, 1), 34.8365083, 32.1152583,
                "research_notes/Map/shikun-before-1952.md: 1 (Balslev, Iyunim Bitkumat Israel 23, p. 247, " +
                "https://in.bgu.ac.il/bgi/iyunim/23/yaron-balslev.pdf); https://www.wikidata.org/wiki/Q2776362 (checked 2026-10-18)"),
            new Want("degania_r35", SyrianR35, new DateTime(1948, 5, 20), 35.5747, 32.7081,
                "content/events/degania_defence.yaml (https://www.jewishvirtuallibrary.org/the-battle-for-degania-may-1948); " +
                "https://en.wikipedia.org/wiki/Renault_R35 (checked 2026-10-28); content/map/settlements_1948.yaml: degania_alef"),
            // Tal Heuberger's four props (owner, 2 November 2026: "Place all four now (Recommended)"). Every source below was read by
            // an AI agent on 2 November 2026 ("agent-read"); a person's check against it is pending (CONTENT_POLICY section 2,
            // rule 6). The excerpts and what each person-check needs are in research_notes/Map/tal-unused-models-placed.md.
            // The well: Abraham's Well at Beersheba, at its he.wiki and en.wiki point, at every date: "בשנת 1948, עם כיבוש באר
            // שבע במסגרת מבצע יואב, הושארה באר אברהם כשטח ציבורי ארכאולוגי" (he.wiki, uncited sentence).
            new Want("beersheba_abrahams_well", Well, new DateTime(1948, 5, 15), null, 34.79307778, 31.237225,
                "he.wiki באר אברהם, https://he.wikipedia.org/w/index.php?oldid=44049275 (agent-read 2026-11-02); " +
                "en.wiki Abraham's Well, https://en.wikipedia.org/w/index.php?oldid=1374743581 (agent-read 2026-11-02); person check pending",
                Siting.Seeded, "", PropScale, townEdge: true, nearOnly: true),
            // The sandbags: "אש שנורתה מתוך עמדת שקי החול שלמרגלות מטה הפלמ"ח אילצה את הסירה לחזור" (he.wiki אלטלנה, footnote
            // Nakdimon 2016), the night of 21-22 June 1948, on the Tel Aviv shore; only on the Altalena's turn.
            new Want("tel_aviv_altalena_sandbags", Sandbags, SandbagsFrom, SandbagsTo, 34.7732528, 32.0964833,
                "he.wiki אלטלנה, https://he.wikipedia.org/w/index.php?oldid=44137735 (footnote nakdimon2016; agent-read 2026-11-02; " +
                "Nakdimon not opened); content/events/altalena.yaml; person check pending", Siting.Shore, "tel_aviv", PropScale, townEdge: true, nearOnly: true),
            // The crates: "Het transportschip Bulgaria met emigranten in de haven van Haifa ... met op de kade kisten en kratten",
            // "Datum : 1948" (Nationaal Archief 255-1124, its Commons copy); year only, so the 1948 turns.
            new Want("haifa_quay_crates", Crates, Year1948From, Year1948To, 35.00448, 32.820118,
                "Commons, https://commons.wikimedia.org/w/index.php?oldid=970193372 (Nationaal Archief 255-1124, " +
                "https://www.nationaalarchief.nl/onderzoeken/fotocollectie/aed57008-d0b4-102d-bcf8-003048976d84; agent-read 2026-11-02); " +
                "person check pending", Siting.Shore, "haifa", PropScale, townEdge: true, nearOnly: true),
            // The wall: "met op de voorgrond een stenen muurtje langs een weg; het dorp op de top is vermoedelijk Abu Ghosh", series
            // "Israël 1948-1949: onderweg naar Jeruzalem" (Nationaal Archief 255-0876, its Commons copy); year only, the 1948 turns.
            new Want("abu_ghosh_road_wall", Wall, Year1948From, Year1948To, AbuGhoshLon, AbuGhoshLat,
                "Commons, https://commons.wikimedia.org/w/index.php?oldid=1225901219 (Nationaal Archief 255-0876, " +
                "https://www.nationaalarchief.nl/onderzoeken/fotocollectie/aed4b5dc-d0b4-102d-bcf8-003048976d84; agent-read 2026-11-02); " +
                "point: en.wiki Abu Ghosh, https://en.wikipedia.org/w/index.php?oldid=1381085214 (agent-read 2026-11-02); person check pending",
                Siting.Road, WallRoute, PropScale, townEdge: true, nearOnly: true),
        };

        public static IReadOnlyList<Want> Placements(DateTime date) => All.Where(x => x.Covers(date)).ToList();

        /// <summary>Every placement, whatever its dates.</summary>
        public static IReadOnlyList<Want> EveryPlacement => All;

        /// <summary>DESIGN VALUES: the beach search: candidate points every <see cref="ShoreStepM"/> along the coast from the coast's
        /// point, nearest first, each on the land <see cref="ShoreInsetM"/> plus the model's clearance in from the water's edge.</summary>
        public const float ShoreStepM = 250f, ShoreInsetM = 60f;
        const int ShoreSteps = 80;

        /// <summary>The first clear spot on the beach of <paramref name="coast"/> (SeaCraft.Coasts): walking along the shore both
        /// ways from the coast's point, the water's edge found seaward of each step and the spot set just inland of it. Null when
        /// the coast is unknown or none is clear.</summary>
        internal static Vector2? ShoreSpot(string coast, Kits.KitGround ground, float clearance, Func<Vector2, float, bool> free)
        {
            var row = Kits.SeaCraft.Coasts.FirstOrDefault(c => c.place == coast);
            if (row.place == null) return null;
            var w = ground.Frame.ToWorld(row.lon, row.lat, 0); var origin = new Vector2(w.x, w.z);
            var along = new Vector2(-row.seaward.y, row.seaward.x);
            for (int i = 0; i <= 2 * ShoreSteps; i++)
            {
                int step = (i + 1) / 2 * (i % 2 == 1 ? 1 : -1);   // 0, +1, -1, +2, -2 ...
                var p = origin + along * (step * ShoreStepM);
                // Inland until dry, then seaward to the water's edge.
                for (int k = 0; k < 200 && ground.Water(p.x, p.y); k++) p -= row.seaward * 50f;
                if (ground.Water(p.x, p.y)) continue;
                var edge = Kits.SeaCraft.FirstWater(p, row.seaward, ground, 20000f);
                if (!(edge is Vector2 e)) continue;
                var spot = e - row.seaward * (clearance + ShoreInsetM);
                if (free(spot, clearance)) return spot;
            }
            return null;
        }

        /// <summary>The first clear spot beside route <paramref name="route"/>'s line (RouteLayer.Corridors) nearest
        /// <paramref name="anchor"/>, just off its corridor on either side, stepping along the line both ways; and the line's
        /// direction there (world x, z, unit length), for the model to lie along it. The model is checked along its whole
        /// length, every half depth. Null when the route is unknown or none is clear.</summary>
        internal static (Vector2 at, Vector2 along)? RoadSpot(MapScene map, string route, Vector2 anchor, float lengthHalf, float depthHalf, Func<Vector2, float, bool> free)
        {
            var corridors = Routes.RouteLayer.Corridors(map.Frame, map.RouteList.Where(r => r.Id == route));
            if (corridors.Count == 0) return null;
            var (line, halfWidth) = corridors[0];
            int seg = -1; float best = float.MaxValue, bestT = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                var a = line[i]; var ab = line[i + 1] - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(anchor - a, ab) / ab.sqrMagnitude) : 0f;
                float d = (a + ab * t - anchor).sqrMagnitude;
                if (d < best) { best = d; seg = i; bestT = t; }
            }
            if (seg < 0) return null;
            // Walk along the line from the nearest point, both ways, by the model's depth.
            float stepM = Mathf.Max(2f * depthHalf, 50f), reach = Mathf.Max(lengthHalf - depthHalf, 0f);
            for (int i = 0; i <= 2 * ShoreSteps; i++)
            {
                int step = (i + 1) / 2 * (i % 2 == 1 ? 1 : -1);
                if (!(PointAlong(line, seg, bestT, step * stepM) is (Vector2 q, Vector2 dir))) continue;
                var normal = new Vector2(-dir.y, dir.x);
                // The side facing the sourced point first.
                float side = Vector2.Dot(anchor - q, normal) >= 0f ? 1f : -1f;
                foreach (var s in new[] { side, -side })
                {
                    // Just off the corridor, its long side along the road: its middle and both ends, each a disc of its half depth.
                    var p = q + normal * s * (halfWidth + depthHalf + 1f);
                    if (AlongIsFree(p, dir, reach, depthHalf, free)) return (p, dir);
                }
            }
            return null;
        }

        /// <summary>Whether discs of radius <paramref name="half"/> every <paramref name="half"/> m along <paramref name="dir"/>,
        /// from <paramref name="reach"/> m back to <paramref name="reach"/> m on through <paramref name="p"/>, are all free.</summary>
        static bool AlongIsFree(Vector2 p, Vector2 dir, float reach, float half, Func<Vector2, float, bool> free)
        {
            float step = Mathf.Max(half, 1f); reach = Mathf.Max(reach, 0f);
            for (float t = -reach; t < reach; t += step) if (!free(p + dir * t, half)) return false;
            return free(p + dir * reach, half);
        }

        /// <summary>The point <paramref name="metres"/> along <paramref name="line"/> from segment <paramref name="seg"/>'s point at
        /// <paramref name="t"/> (negative: back along it), and the line's direction there; null past either end.</summary>
        static (Vector2 at, Vector2 dir)? PointAlong(List<Vector2> line, int seg, float t, float metres)
        {
            var at = line[seg] + (line[seg + 1] - line[seg]) * t;
            int i = seg; float left = Mathf.Abs(metres); bool forward = metres >= 0f;
            while (true)
            {
                var to = forward ? line[i + 1] : line[i];
                var d = to - at; float len = d.magnitude;
                var dir = (line[i + 1] - line[i]).normalized;
                if (len >= left) return (at + (len > 0f ? d / len : Vector2.zero) * left, dir);
                left -= len; at = to;
                i += forward ? 1 : -1;
                if (i < 0 || i + 1 >= line.Count) return null;
            }
        }

        /// <summary>#1701 slice F: one b21 per built arms workshop (R-ARMS-004, Workshops.ArmsWorkshop), on its city project's
        /// hex from the day the build completed (CityProject.BuiltOn), in ascending project id. Under gdd-0.3 only; nothing for a
        /// project still being built, nor for one no longer in the state.</summary>
        public static IReadOnlyList<Want> ArmsWorkshops(Statecraft.Sim.Content content, IEnumerable<Statecraft.Sim.CityProject> projects)
        {
            var list = new List<Want>();
            if (content.Ruleset != Statecraft.Sim.SaveGame.Gdd03Ruleset) return list;
            foreach (var p in projects.OrderBy(x => x.Id))
            {
                if (p.Kind != Statecraft.Sim.Workshops.ArmsWorkshop || !(p.BuiltOn is DateTime on)) continue;
                var hex = content.HexAt(p.Q, p.R);
                if (hex == null) continue;
                list.Add(new Want(WorkshopKey(p.Id), Workshop, on, null, hex.Lon, hex.Lat,
                    FormattableString.Invariant($"city project {p.Id}: {p.City}'s arms workshop on hex {p.Q},{p.R}"), Siting.Seeded, "", city: p.City));
            }
            return list;
        }

        /// <summary>The layer key (and instance name) of the b21 standing for city project <paramref name="project"/>.</summary>
        public static string WorkshopKey(int project) => FormattableString.Invariant($"arms_workshop_{project}");

        /// <summary>How far from its hex's centre a workshop's model may reach (m): the hex's inner radius (HexScale.HexM is the
        /// circumradius), so the whole model stands on the project's own hex.</summary>
        public const float HexInnerRadiusM = 0.8660254f * Statecraft.HexTiles.HexScale.HexM;

        /// <summary>How far inside a town's footprint <paramref name="anchor"/> lies (m, 0 outside every footprint): a sourced
        /// point at a town's edge (Netanya's entrance) is inside the kit's symbolic footprint, so the search starts that far
        /// out.</summary>
        public static float Inside(Vector2 anchor, IEnumerable<(Vector2 centre, float radius)> footprints)
        {
            float most = 0f;
            foreach (var (c, r) in footprints) most = Mathf.Max(most, r - (anchor - c).magnitude);
            return most;
        }

        /// <summary>A stable ring search, from <paramref name="from"/> m out. FNV-1a chooses the first bearing; no Unity
        /// random stream or simulation state moves.</summary>
        public static Vector2? SeededSpot(string key, Vector2 anchor, float clearance, Func<Vector2, float, bool> free, float from = 0f)
        {
            uint h = 2166136261;
            foreach (char c in key) { h ^= c; h *= 16777619; }
            int start = (int)(h % SpotAttempts);
            for (int i = 0; i < SpotAttempts; i++)
            {
                int k = (start + i) % SpotAttempts;
                float angle = 2f * Mathf.PI * k / SpotAttempts;
                float radius = from + clearance * (1.5f + k / 8);
                var p = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (free(p, clearance)) return p;
            }
            return null;
        }

        /// <summary>The first clear point just outside <paramref name="villageBounds"/>, on the side facing the nearest
        /// mapped transport segment. Bearings beside that segment are tried symmetrically when the exact bearing is occupied.</summary>
        internal static Vector2? RouteFacingEdgeSpot(Vector2 centre, Bounds villageBounds, float clearance,
            IEnumerable<(List<Vector2> line, float halfWidth)> routes, Func<Vector2, float, bool> free)
        {
            Vector2 toward = Vector2.zero; float nearest = float.MaxValue;
            foreach (var (line, _) in routes)
                for (int i = 0; i + 1 < line.Count; i++)
                {
                    var a = line[i]; var ab = line[i + 1] - a;
                    float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(centre - a, ab) / ab.sqrMagnitude) : 0f;
                    var q = a + ab * t; float d = (q - centre).sqrMagnitude;
                    if (d < nearest && d > 1e-4f) { nearest = d; toward = (q - centre).normalized; }
                }
            if (toward == Vector2.zero) return null;
            int halfPlaneCandidateCount = SpotAttempts / 2 + 1; // Facing half-plane: the route bearing, then both sides through +/-90 degrees.
            var exitBounds = villageBounds;
            float boundsSizeExpansion = 2f * clearance; // Bounds.Expand takes total size growth, so 2x adds one clearance on each side.
            exitBounds.Expand(new Vector3(boundsSizeExpansion, 0f, boundsSizeExpansion));
            for (int i = 0; i < halfPlaneCandidateCount; i++)
            {
                int step = i == 0 ? 0 : (i + 1) / 2 * (i % 2 == 1 ? 1 : -1);
                float a = 2f * Mathf.PI * step / SpotAttempts, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                var dir = new Vector2(toward.x * ca - toward.y * sa, toward.x * sa + toward.y * ca);
                float tx = Mathf.Abs(dir.x) > 1e-5f ? (dir.x > 0f ? exitBounds.max.x - centre.x : centre.x - exitBounds.min.x) / Mathf.Abs(dir.x) : float.MaxValue;
                float tz = Mathf.Abs(dir.y) > 1e-5f ? (dir.y > 0f ? exitBounds.max.z - centre.y : centre.y - exitBounds.min.z) / Mathf.Abs(dir.y) : float.MaxValue;
                var p = centre + dir * Mathf.Min(tx, tz);
                if (free(p, clearance)) return p;
            }
            return null;
        }

        /// <summary>The world box of the kit meshes drawn for <paramref name="id"/> (its kit and its close kit, KitLayer), or null
        /// when it has none. It is taken from the meshes, not from their renderers: the "kits" toggle disables the renderers, and
        /// a disabled or inactive renderer can report an empty box at the origin.</summary>
        internal static Bounds? KitBounds(Transform kits, string id)
        {
            Bounds? box = null;
            foreach (Transform t in kits)
            {
                if (t.name != id && t.name != id + Kits.KitLayer.CloseSuffix) continue;
                if (!t.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                var local = filter.sharedMesh.bounds; var m = t.localToWorldMatrix;
                var b = new Bounds(m.MultiplyPoint3x4(local.center), Vector3.zero);
                for (int corner = 0; corner < 8; corner++)
                    b.Encapsulate(m.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f))));
                if (box is Bounds sofar) { sofar.Encapsulate(b); box = sofar; } else box = b;
            }
            return box;
        }

        public static Quaternion Rotation(Quaternion prefabRotation, float yaw) =>
            Quaternion.AngleAxis(yaw, Vector3.up) * prefabRotation;

        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = CollectionStream.Request;
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> asked = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<Renderer> renderers = new List<Renderer>();
        Action<List<(string key, Vector3 at, float radius)>>? fillResidencyAnchors;
        IReadOnlyList<Want> wants = Array.Empty<Want>();

        CollectionResidency? residency;
        CollectionResidencyView? residencyView;
        Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? ownedAcquire;
        readonly List<UnityEngine.Object> retired = new List<UnityEngine.Object>();
        internal CollectionResidencyMetrics.Category ResidencySnapshot() => CollectionResidencyMetrics.Row("special",
            isActiveAndEnabled ? Admitted().Count() : 0, transform.childCount,
            prefabs.Keys.Concat((isActiveAndEnabled ? Admitted().Select(w => w.Model) : Enumerable.Empty<string>())), residency?.OwnerCount ?? 0, residency?.PendingCount ?? 0,
            prefabs.Values.Count(p => p != null), GetComponentsInChildren<Renderer>(true).Length, 0, prefabs.Values.Count(p => p == null), prefabs.Keys.Select(k => k + "|Near"));

        internal void ConfigureResidency(CollectionResidencyPolicy policy,
            Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? acquire)
        {
            if (residencyView != null) throw new InvalidOperationException("Residency already configured.");
            if (asked.Count != 0 && acquire == null) throw new InvalidOperationException("Configure residency before requests; pinned originals remain blockers.");
            ownedAcquire = acquire; residencyView = new CollectionResidencyView(policy);
            // Construction configures before first Show; injected acquisition is a test seam.
            ClearForRetirement();
        }
        IDisposable AcquireOriginal(string id, CollectionDetail detail, Action<GameObject?> answer) =>
            ownedAcquire != null ? ownedAcquire(id, detail, answer) : CollectionStream.Acquire(id, detail, answer);
        IEnumerable<Want> Admitted()
        {
            var keys = residencyView?.Keys;
            return keys == null ? wants : wants.Where(w => keys.Contains(w.Key));
        }
        void FillResidencyAnchors(List<(string key, Vector3 at, float radius)> anchors)
        {
            if (map == null) return;
            for (int i = 0; i < wants.Count; i++)
            {
                var w = wants[i];
                anchors.Add((w.Key, map.Frame.ToWorld(w.Lon, w.Lat, 0), ClearanceOf(w)));
            }
        }
        static bool SameKeys(HashSet<string> before, HashSet<string> after)
        {
            if (before.Count != after.Count) return false;
            foreach (string key in before) if (!after.Contains(key)) return false;
            return true;
        }
        void RefreshResidency() => RefreshResidencyCore(true);
        void RefreshResidencyCore(bool redraw)
        {
            if (residencyView == null || !isActiveAndEnabled) return;
            residency ??= new CollectionResidency(AcquireOriginal, ClearForRetirement, (id, detail, prefab) =>
                {
                    prefabs[id] = prefab;
                    if (prefab == null) { FailedOpen = true; Debug.LogWarning($"BUILDINGS: {id} did not stream; it stays off the map."); }
                    Rebuild();
                },
                (instances, owners, keys, complete) => CollectionResidencyMetrics.AfterDestroyed("special", instances, owners, complete,
                    Application.isPlaying && ownedAcquire == null ? () => CollectionStream.Settled(keys) : (Func<bool>?)null, keys), true);
            // v0.3.1 Show requested every wanted special as Near immediately; only its renderers were close-camera gated (owner
            // ruling 29, 30 October 2026: "Far models map-wide"). The four props are the exception (owner on #2274: "Near-only
            // for these props"): each is requested only while a camera is close to it (the view's keys), and released on leave.
            var near = residencyView.Keys;
            // A near-only prop with no planned spot (no clear ground, or past the cap) is not asked for: it would never be drawn.
            residency.Replace(wants.Where(w => !w.NearOnly || near.Contains(w.Key) && planned.ContainsKey(w.Key)).Select(w => (w.Model, CollectionDetail.Near)));
            if (redraw) Rebuild();
        }
        IReadOnlyList<UnityEngine.Object> ClearForRetirement(IReadOnlyCollection<(string id, CollectionDetail detail)> keys)
        {
            var removed = new List<UnityEngine.Object>();
            foreach (var key in keys)
            {
                prefabs.Remove(key.id); asked.Remove(key.id);
                if (!originals.TryGetValue(key.id, out var list)) continue;
                foreach (var go in list) if (go != null)
                {
                    var ownedRenderers = new HashSet<Renderer>(go.GetComponentsInChildren<Renderer>(true));
                    renderers.RemoveAll(ownedRenderers.Contains);
                    removed.Add(go);
                    if (Application.isPlaying) { if (gameObject.activeInHierarchy) go.transform.SetParent(null); Destroy(go); } else DestroyImmediate(go);
                }
                originals.Remove(key.id);
                Placed.RemoveAll(p => p.Model == key.id);
                if (key.id == Workshop) workshopBoxes = new List<Bounds[]>();   // a new list: the retired b21 leave the labels too
            }
            return removed;
        }
        IReadOnlyList<UnityEngine.Object> ClearForRetirement()
        {
            ClearVisuals(); prefabs.Clear(); asked.Clear();
            closeFor.Clear();
            var pending = retired.ToArray(); retired.Clear(); return pending;
        }
        void ClearVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject; if (residencyView != null) retired.Add(old);
                if (Application.isPlaying) { if (gameObject.activeInHierarchy) old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            renderers.Clear(); Placed.Clear(); workshopBoxes = new List<Bounds[]>();
            originals.Clear();
            if (map != null) Statecraft.Game.Map.Units.UnitShowcase.HideTrees(map, Name, Array.Empty<(Vector2, float)>());
        }
        bool residencyDirty;
        void ObserveResidency(Camera camera, bool inRender)
        {
            var before = residencyView!.Keys;
            residencyView.Observe(camera, fillResidencyAnchors ??= FillResidencyAnchors, Time.frameCount);
            if (!SameKeys(before, residencyView.Keys))
            {
                if (Application.isPlaying || inRender)
                {
                    residencyDirty = true;
#if UNITY_EDITOR
                    // Edit mode runs LateUpdate only when something asks for a player-loop update.
                    if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
                }
                else RefreshResidencyCore(false);
            }
        }
        void LateUpdate()
        {
            if (residencyView != null && residencyView.Prune(Time.frameCount)) residencyDirty = true;
            if (residencyDirty) { residencyDirty = false; RefreshResidencyCore(false); }
        }
        void OnDestroy() { residency?.Dispose(); residency = null; }

        MapScene? map; DateTime date; bool shown;
        IReadOnlyList<Want> workshops = Array.Empty<Want>();
        int ruralVersion;

        internal static bool Same(IReadOnlyList<Want> a, IReadOnlyList<Want> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i].Key != b[i].Key || a[i].From != b[i].From || a[i].Lon != b[i].Lon || a[i].Lat != b[i].Lat) return false;
            return true;
        }
        public readonly List<(string Key, string Model, Vector3 At)> Placed = new List<(string, string, Vector3)>();
        List<Bounds[]> workshopBoxes = new List<Bounds[]>();

        /// <summary>Review of #2281 (option 3, coordinator 3 November 2026): the drawn b21s' world boxes while they are drawn (the
        /// close zoom), for the name pills to keep clear of. GameRoot passes them to MapLabelsPresenter's keepClear with the
        /// units' and the craft's; a new list each rebuild, so the labels refresh when it changes.</summary>
        public IReadOnlyList<Bounds[]> KeepClear => isActiveAndEnabled && LastClose ? workshopBoxes : (IReadOnlyList<Bounds[]>)Array.Empty<Bounds[]>();

        /// <summary>As <see cref="KeepClear"/> for <paramref name="camera"/>: whether that camera last drew the b21 (its own
        /// close-zoom switch), not whichever camera rendered last (review of #2281, S4).</summary>
        public IReadOnlyList<Bounds[]> KeepClearFor(Camera camera) =>
            isActiveAndEnabled && closeFor.TryGetValue(camera.GetInstanceID(), out var close) && close ? workshopBoxes : (IReadOnlyList<Bounds[]>)Array.Empty<Bounds[]>();
        public bool FailedOpen { get; private set; }
        public bool LastClose { get; private set; }
        readonly Dictionary<string, List<GameObject>> originals = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);

        public static SpecialBuildingModels? ShowAt(MapScene map, DateTime date) => ShowAt(map, date, Array.Empty<Want>());

        /// <summary>As <see cref="ShowAt(MapScene, DateTime)"/>, with a b21 at each built arms workshop of the state
        /// (<see cref="ArmsWorkshops"/>; gdd-0.3 only).</summary>
        public static SpecialBuildingModels? ShowAt(MapScene map, DateTime date, Statecraft.Sim.Content content,
            IEnumerable<Statecraft.Sim.CityProject> projects) => ShowAt(map, date, ArmsWorkshops(content, projects));

        /// <summary>As <see cref="ShowAt(MapScene, DateTime)"/>, with <paramref name="workshops"/> (<see cref="ArmsWorkshops"/>,
        /// computed once by the caller for this layer and HexPropModels).</summary>
        internal static SpecialBuildingModels? ShowAt(MapScene map, DateTime date, IReadOnlyList<Want> workshops)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<SpecialBuildingModels>();
            if (layer == null)
            {
                var go = new GameObject(Name); go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<SpecialBuildingModels>(); layer.map = map;
            }
            if (layer.residencyView == null) layer.ConfigureResidency(map.Residency.Special, null);
            // GameRoot calls this on every refresh. The layer rebuilds on events only, never per frame: the game date changes
            // (the dated list and the dated tokens' clearance both follow it), the built arms workshops change (one completed, or
            // one left the state), or, while a wanted row keeps off the farm scenes (KeepsOffFarms: the workshops and Tal's four
            // props, so every date the well stands), the farm scenes' plan does. Its version (RuralLife.VersionOf) moves only
            // when RuralLife.Show is given other wants or other ground to keep clear of (the civilians follow the date, the
            // placements, the settlements and the city projects); HexPropModels rebuilds on it too, so both stand on one plan.
            var next = workshops.Count == 0 ? Placements(date) : Placements(date).Concat(workshops).ToList();
            int rural = next.Any(KeepsOffFarms) ? RuralLife.VersionOf(map) : 0;
            if (layer.shown && layer.date == date && Same(layer.workshops, workshops) && layer.ruralVersion == rural) return layer;
            layer.shown = true; layer.date = date; layer.workshops = workshops; layer.ruralVersion = rural;
            layer.Show(next); return layer;
        }

        internal void ShowForTest(IReadOnlyList<Want> wants) => Show(wants);

        void Show(IReadOnlyList<Want> wants)
        {
            this.wants = wants;
            Replan(log: true);
            if (residencyView != null) { RefreshResidency(); return; }
            foreach (var w in wants)
                if (asked.Add(w.Model)) { string model = w.Model; Request(model, CollectionDetail.Near, p => { if (this == null) return; prefabs[model] = p; if (p == null) { FailedOpen = true; Debug.LogWarning($"BUILDINGS: {model} did not stream; it stays off the map."); } Rebuild(); }); }
            Rebuild();
        }

        /// <summary>Each wanted row's planned spot and, beside a road, the road's direction: <see cref="PlanWithRoutes"/> over the
        /// whole wanted list with every model available (#2266's contract), whatever has streamed. The residency reads it (a
        /// near-only prop with no spot is not asked for) and Rebuild draws from it. <paramref name="log"/> writes the warnings,
        /// once per Show (ShowAt), not on every redraw.</summary>
        void Replan(bool log)
        {
            planned.Clear();
            if (map == null || map.Terrain == null) return;
            var plan = PlanWithRoutes(map, date, wants, _ => true, log, null, out var along);
            foreach (var (w, at) in plan) planned[w.Key] = (at, along.TryGetValue(w.Key, out var road) ? road : (Vector2?)null);
        }

        readonly Dictionary<string, (Vector2 at, Vector2? along)> planned = new Dictionary<string, (Vector2, Vector2?)>(StringComparer.Ordinal);

        /// <summary>Where the buildings stand: each want in order at its route-facing (Degania), beach (<see cref="Siting.Shore"/>),
        /// roadside (<see cref="Siting.Road"/>) or seeded spot, clear of the towns, the routes, the tokens of <paramref name="date"/>,
        /// the civilians, the wildlife and each other (a workshop also of its hex's edge and the farm scenes' plan, and on a hex
        /// a major city's kit blocks reach, in the camera's sight past them, <see cref="InSight"/>; a major city's workshop at
        /// the edge of the city's kit blocks instead, <see cref="EdgeSpot"/>; one of Tal's
        /// four props, <see cref="Want.TownEdge"/>, at a town's edge, within <see cref="MaxPropOffset"/> of its point); the list
        /// keeps those whose model <paramref name="available"/> holds. A row with no spot (no clear ground, or past the cap) is left
        /// out whatever <paramref name="available"/> says, so no layer keeps clear of a prop that is not drawn. It reads only the
        /// map and the date, never what has streamed, so with every model available it is the plan the layer draws once everything
        /// has arrived (HexPropModels keeps clear of it, #2266).</summary>
        internal static List<(Want want, Vector2 at)> Plan(MapScene map, DateTime date, IReadOnlyList<Want> wants, Func<string, bool> available, bool log,
            IReadOnlyList<(RuralLife.Want want, Vector2 spot)>? ruralPlan = null)
            => PlanWithRoutes(map, date, wants, available, log, ruralPlan, out _);

        /// <summary>As <see cref="Plan"/>, also giving each roadside row's road direction (world x, z, unit length) by key, for the
        /// model to lie along the road; only rows that got a spot have one.</summary>
        internal static List<(Want want, Vector2 at)> PlanWithRoutes(MapScene map, DateTime date, IReadOnlyList<Want> wants, Func<string, bool> available, bool log,
            IReadOnlyList<(RuralLife.Want want, Vector2 spot)>? ruralPlan, out IReadOnlyDictionary<string, Vector2> along)
        {
            var roads = new Dictionary<string, Vector2>(StringComparer.Ordinal); along = roads;
            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var taken = new List<(Vector2 centre, float radius)>();
            // The civilian figures (#666, shown first by GameRoot) and the wildlife (#670) keep their ground too.
            var people = map.Terrain.transform.Find(CivilianPeople.Name)?.GetComponent<CivilianPeople>();
            if (people != null) foreach (var p in people.Wanted) taken.Add((new Vector2(p.At.x, p.At.z), CivilianPeople.FigureClearanceM));
            foreach (var a in WildlifeLayer.Wants(map)) taken.Add((a.At, 0.5f * WildlifeLayer.SizeM));
            // A workshop (#1701 slice F) and Tal's four props (KeepsOffFarms) also keep off the farm scenes (#667): where they
            // WILL stand (RuralLife's plan with every model available, RuralLife.PlanOf, the one HexPropModels.Others reads too),
            // never what has streamed so far, so a spot is the same whichever layer streams first and the hex props keep clear
            // of the models drawn.
            var farms = new List<(Vector2 centre, float radius)>();
            if (wants.Any(KeepsOffFarms))
                foreach (var (want, spot) in ruralPlan ?? RuralLife.PlanOf(map))
                    farms.Add((spot, RuralLife.SceneRadiusM(want.Model)));
            bool FreeOn(Vector2 p, float r, DateTime on) => TownLandmarks.Clear(p, r, taken) && TownLandmarks.Clear(p, r, footprints)
                && ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)
                && !ground.Water(p.x, p.y) && !ground.OnRoute(p, r) && !ground.OnToken(p, r, on);
            bool Free(Vector2 p, float r) => FreeOn(p, r, date);
            // The four props stand at a town's edge (owner on #2274: "Bigger, closer"): inside a footprint if need be, clear of the
            // towns' drawn buildings (a major city's kit blocks, every other town's building models: #2876), of the tokens
            // standing in towns and of every settlement's and locality's name point instead, and of the farm scenes' plan, as the
            // workshops (coordinator, 2 November 2026, on #2274: Q3).
            List<(Vector2, float)>? buildings = null, clearings = null; List<Vector2>? names = null;
            List<(Vector2 c, float yaw, float w, float d)>? blocks = null;
            bool FreeAtEdge(Vector2 p, float r)
            {
                if (buildings == null)
                {
                    var towns = map.GetComponentInChildren<Kits.TownModelLayer>(true);
                    buildings = towns == null ? new List<(Vector2, float)>()
                        : map.KitFootprints.Where(f => !Kits.TownModels.IsMajorCity(f.id)).SelectMany(f => towns.InstancesOf(f.id))
                            .Select(i => (i.C, 0.5f * new Vector2(i.W, i.D).magnitude)).ToList();
                    blocks = map.MajorCityBlocks.Select(b => (b.C, b.Yaw, b.W, b.D)).ToList();
                    clearings = map.UnitClearings.ToList();
                    names = map.Settlements.Concat(map.Localities.Select(l => l.Settlement))
                        .Select(s => { var n = map.Frame.ToWorld(s.Lon, s.Lat, 0); return new Vector2(n.x, n.z); }).ToList();
                }
                foreach (var n in names!) if ((n - p).sqrMagnitude <= r * r) return false;
                return TownLandmarks.Clear(p, r, taken) && TownLandmarks.Clear(p, r, buildings) && ClearOfBoxes(p, r, blocks!)
                    && TownLandmarks.Clear(p, r, clearings!) && TownLandmarks.Clear(p, r, farms)
                    && ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)
                    && !ground.Water(p.x, p.y) && !ground.OnRoute(p, r) && !ground.OnToken(p, r, date);
            }
            var plan = new List<(Want, Vector2)>();
            var kits = map.transform.Find(Kits.KitLayer.RootName);
            foreach (var w in wants)
            {
                // Every want takes its ground whether or not its model has streamed (only the plan's output is filtered), so a
                // later want's spot, a workshop's included, never depends on what streamed first.
                var anchor3 = map.Frame.ToWorld(w.Lon, w.Lat, 0); var anchor = new Vector2(anchor3.x, anchor3.z);
                float clearance = ClearanceOf(w);
                Vector2? at = null;
                if (w.Key == "degania_r35" && kits != null)
                {
                    var village = KitBounds(kits, "degania_alef");
                    var footprint = map.KitFootprints.FirstOrDefault(f => f.id == "degania_alef");
                    if (village is Bounds drawn && footprint.id != null)   // no kit or no footprint: the seeded spot below
                        at = RouteFacingEdgeSpot(footprint.centre, drawn, clearance, map.KitRouteCorridors, Free);
                }
                Vector2? dir = null;
                Func<Vector2, float, bool> free = w.TownEdge ? FreeAtEdge : Free;
                if (w.Site == Siting.Shore) at = ShoreSpot(w.Along, ground, clearance, free);
                else if (w.Site == Siting.Road)
                {
                    float lengthHalf = 0.5f * SizeOf(w), depthHalf = WallDepthShare * lengthHalf;
                    if (RoadSpot(map, w.Along, anchor, lengthHalf, depthHalf, free) is (Vector2 roadAt, Vector2 roadDir)) { at = roadAt; dir = roadDir; }
                }
                if (at == null && w.Site != Siting.Seeded)
                {
                    if (log && available(w.Model)) Debug.LogWarning($"BUILDINGS: no clear {(w.Site == Siting.Shore ? "beach" : "roadside")} spot for {w.Key}; its {w.Model} is not drawn.");
                    continue;
                }
                if (at == null && w.Model == Workshop && Kits.TownModels.IsMajorCity(w.City)
                    && map.KitFootprints.Any(f => f.id == w.City) && map.MajorCityBlocks.Any(b => b.Id == w.City))
                {
                    // #2876, the owner, 6 November 2026 (verbatim): "make the workshop at the edge of the city". In a major city
                    // (its kit blocks drawn at every zoom) the b21 stands at the edge of the city's blocks, on the project's hex
                    // where it can, in the camera's sight (EdgeSpot), on ground the player's side holds (OnHeldGround), clear of
                    // the farm scenes, the other models, the routes and the tokens and figures, each of the day it was built (as
                    // below: the spot is fixed at the build and kept as the front moves). No spot: it is not drawn, and the
                    // warning names the city.
                    var built = w.From; var home = ground.Hex(anchor.x, anchor.y);
                    at = EdgeSpot(map, ground, w.City, w.Key, anchor, clearance, SizeOf(w), new[]
                    {
                        new SpotCheck("held", (p, r) => OnHeldGround(map, ground, p, r, built, home)),
                        new SpotCheck("farms", (p, r) => TownLandmarks.Clear(p, r, farms)),
                        new SpotCheck("taken", (p, r) => TownLandmarks.Clear(p, r, taken)),
                        new SpotCheck("routes", (p, r) => !ground.OnRoute(p, r)),
                        new SpotCheck("tokens", (p, r) => !ground.OnToken(p, r, built)),
                        new SpotCheck("figures", (p, r) => ClearOfFigures(map, p, r, built)),
                    }, out var tally);
                    if (at == null)
                    {
                        if (log && available(w.Model))
                            Debug.LogWarning($"BUILDINGS: no spot at the edge of {w.City} passes every test for {w.Key} ({tally}); its {w.Model} is not drawn.");
                        continue;
                    }
                }
                else if (at == null && w.Model == Workshop)
                {
                    // On the project's own hex (its centre is the anchor), clear of the farm scenes, and as far out of a town's
                    // footprint as the civilian figures keep from its label (CivilianPeople.LabelClearanceM). The units' tokens and
                    // figures are those of the map's planned unit spots (MapScene.UnitClearings and UnitTokens, built with the map
                    // from units_1948, not the units' live places) on the day the workshop was built (w.From), so its spot does not
                    // move as the game's date passes a token's last day (review of #2281, S2).
                    var built = w.From; var home = ground.Hex(anchor.x, anchor.y);
                    // #2876: clear of the major cities' kit blocks too, which reach past their footprints (out to
                    // SettlementKits.PlotReach of the radius), so a spot just outside one keeps off its outer blocks.
                    var hexBlocks = MajorCityBlocksNear(map, anchor);
                    // #2876: on a hex those blocks reach, also where the camera sees the b21's top past them (InSight: the blocks
                    // stand up to about 1.6 km at 60x, the b21 about 160 m). Every other hex: no test (sight stays null).
                    float side = SizeOf(w), tan = SightTan;
                    var sight = hexBlocks.Count == 0 ? null : MajorCitySightBlocks(map, ground, anchor, tan);
                    bool Seen(Vector2 p, float yawDeg) => sight == null || InSight(p, yawDeg, side, ground.Y(p.x, p.y) + WorkshopTopShare * side, sight, tan);
                    bool OnHex(Vector2 p, float r) => (p - anchor).magnitude + r <= HexInnerRadiusM && TownLandmarks.Clear(p, r, farms)
                        && ClearOfBoxes(p, r, hexBlocks) && ClearOfFigures(map, p, r, built) && FreeOn(p, r, built) && Seen(p, 0f)
                        && OnHeldGround(map, ground, p, r, built, home);
                    float inside = Inside(anchor, footprints);
                    at = SeededSpot(w.Key, anchor, clearance, OnHex, inside > 0f ? inside + CivilianPeople.LabelClearanceM : 0f);
                    // A town whose footprint covers the whole hex (Jerusalem, Tel Aviv): a gap between its drawn buildings (owner,
                    // 2 November 2026: "Among the houses"), or, between a major city's kit blocks, a street (#2876): the model then
                    // lies along it (dir), as a roadside model along its road.
                    if (at == null && inside > 0f) at = TownGap(map, w.Key, anchor, clearance, side, (p, r) =>
                        (p - anchor).magnitude + r <= HexInnerRadiusM && TownLandmarks.Clear(p, r, farms) && TownLandmarks.Clear(p, r, taken)
                        && ClearOfFigures(map, p, r, built)
                        && ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)
                        && !ground.Water(p.x, p.y) && !ground.OnRoute(p, r) && !ground.OnToken(p, r, built)
                        && OnHeldGround(map, ground, p, r, built, home), Seen, out dir);
                    // #2876 (coordinator, 6 November 2026: no quiet fallback to a hidden spot): where no spot on the hex passes
                    // every test, the camera's sight included, the b21 is not drawn, and the warning names the cities.
                    if (at == null && sight != null)
                    {
                        if (log && available(w.Model))
                            Debug.LogWarning($"BUILDINGS: no spot on the hex of {w.Key} passes every test, the camera's sight past the kit blocks of {string.Join(", ", sight.Select(b => b.Id).Distinct())} included; its {w.Model} is not drawn.");
                        continue;
                    }
                }
                else if (at == null)
                {
                    if (w.Key == "degania_r35" && log && available(w.Model))
                        Debug.LogWarning("BUILDINGS: route-facing spot unavailable for degania_r35; falling back to the seeded spot.");
                    float from = w.TownEdge ? 0f : Inside(anchor, footprints);
                    at = SeededSpot(w.Key, anchor, clearance, free, from);
                    for (int ring = 1; at == null && ring <= WidenRings; ring++)
                        at = SeededSpot(w.Key, anchor, clearance, free, from + ring * WidenShare * clearance);
                }
                if (at == null) { if (log && available(w.Model)) Debug.LogWarning($"BUILDINGS: no clear ground near {w.Key}; its {w.Model} is not drawn."); continue; }
                if (w.TownEdge && (at.Value - anchor).magnitude > MaxPropOffset)
                {
                    if (log && available(w.Model)) Debug.LogWarning(FormattableString.Invariant($"BUILDINGS: the nearest clear spot for {w.Key} is {(at.Value - anchor).magnitude:0} m from its sourced point, past {MaxPropOffset:0} m; its {w.Model} is not drawn."));
                    continue;
                }
                taken.Add((at.Value, clearance));
                if (dir is Vector2 road) roads[w.Key] = road;
                if (available(w.Model)) plan.Add((w, at.Value));
            }
            return plan;
        }

        /// <summary>Whether a row keeps off the farm scenes' plan (RuralLife.PlanOf): a workshop (#1701 slice F) and Tal's four
        /// props (<see cref="Want.TownEdge"/>; coordinator, 2 November 2026, on #2274: Q3).</summary>
        internal static bool KeepsOffFarms(Want w) => w.Model == Workshop || w.TownEdge;

        /// <summary>The ground (m) a place's name pill can take, a workshop among a town's houses keeping off it: the band from the
        /// footprint's south edge to <see cref="TownLabelHeightM"/> past its north edge, <see cref="TownLabelHalfWidthM"/> either
        /// side of its centre. It models MapLabelsPresenter's Above spots (centred, or starting at the kit's centre to either
        /// side, as far as the half width reaches); a pill pushed Beside or Below its kit is not modelled, and the on-screen
        /// checks (MapLabelTests.NoNamePillCoversAWorkshopModel*) take the zooms and framings a player sees.
        /// Design values: a long locality pill is about 141 by 23 px at a 1280 px frame, some 18 m a pixel at the 30 km zoom
        /// and 27 m at the close zoom's 45 km exit, with the lift above the kit and a margin.</summary>
        public const float TownLabelHalfWidthM = 2000f, TownLabelHeightM = 1000f;

        /// <summary>Whether a disc of <paramref name="r"/> at <paramref name="p"/> keeps clear of the name pill of the place whose
        /// footprint is centred at <paramref name="centre"/> with <paramref name="radius"/>.</summary>
        public static bool ClearOfName(Vector2 p, float r, Vector2 centre, float radius) =>
            Mathf.Abs(p.x - centre.x) >= TownLabelHalfWidthM + r
            || p.y - centre.y <= -radius - r || p.y - centre.y >= radius + TownLabelHeightM + r;

        /// <summary>How far north on the ground a unit's figures hide what stands behind them (m): their height,
        /// UnitBadge.FigureHeightM times UnitShowcase.FigureScale (as UnitLayer sizes them, UnitLayer.cs:764), seen at the view's
        /// shallowest ray, CameraRig.Pitch - Fov/2, so height times cot(Pitch - Fov/2). (The flag's thin pole stands higher; keeping clear of its full height and
        /// its cloth's reach leaves Jerusalem's hex no ground, and ArmsWorkshopVisibilityTests checks the drawn tokens, flags
        /// included.)</summary>
        public static float FigureShadowM => Units.UnitBadge.FigureHeightM * Units.UnitShowcase.FigureScale
            / Mathf.Tan((CameraRig.Pitch - 0.5f * CameraRig.Fov) * Mathf.Deg2Rad);
        // The framing (review of #2281, S1): CameraRig turns the camera Euler(Pitch, Yaw, 0) (CameraRig.cs:72), so Pitch is the
        // view's depression below the horizon, exact on the screen's centre row; rows above it look down at less, to
        // Pitch - Fov/2 at the top edge, where a figure hides the most ground. The bound takes that shallowest ray: 2.49 km x
        // cot(32.5 deg) = 3.91 km (2.09 km at the centre row's 50 deg). The yaw is 0 under gdd-0.3, the only ruleset with
        // workshops (CameraHeadingPolicy.cs:6, fixed north), and FigureScale does not change with the zoom (UnitLayer.cs:738-768).

        /// <summary>Review of #2281 (the Jerusalem b21 hidden behind a garrison's figures): whether a model of clearance
        /// <paramref name="r"/> at <paramref name="p"/> stands clear of the ground a unit spot's figures hide in the map's oblique
        /// view: north of the spot (the camera looks north, CameraRig yaw 0), within the spot's disc either side, as far as
        /// <see cref="FigureShadowM"/> past it. The spots: the units' town clearings (MapScene.UnitClearings) and their rings
        /// out of town (MapScene.UnitTokens, while their dates last), the planned ground the figures stand on.</summary>
        public static bool ClearOfFigures(MapScene map, Vector2 p, float r, DateTime on)
        {
            foreach (var (c, radius) in map.UnitClearings) if (Hidden(p, r, c, radius)) return false;
            foreach (var (c, radius, until) in map.UnitTokens) if (until > on && Hidden(p, r, c, radius)) return false;   // as KitGround.OnToken
            return true;
        }

        /// <summary>#2876 (coordinator, 6 November 2026: the Jerusalem b21 stood on the other side's ground): whether a disc of
        /// <paramref name="r"/> at <paramref name="p"/> stands on ground the player's side holds on <paramref name="on"/>, as the
        /// map draws it: its centre and 8 points round it each on <paramref name="home"/>, the project's own hex (a player city:
        /// the sim builds there only for the player, and the wash's live overlay, MapRegions.LayerOn, paints a settlement the
        /// player holds as the player's), or on a hex the control wash (MapScene.Control, control_hexes_1948_49.yaml, the
        /// layer GameRoot draws through MapRegions) gives the Israeli side that day. The other side, contested ground, no-man's
        /// land, a demilitarized zone, no colour and off the data are not held. A workshop reads it on the day it was built
        /// (Want.From), so its spot stays as the front moves. A map without control data draws no wash: every disc passes.</summary>
        internal static bool OnHeldGround(MapScene map, Kits.KitGround ground, Vector2 p, float r, DateTime on, (int, int) home)
        {
            var control = map.Control;
            if (control == null) return true;
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI / 4f;
                var s = i == 8 ? p : p + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var hex = ground.Hex(s.x, s.y);
                if (hex == home) continue;
                if (control.At(hex.Item1, hex.Item2, on).side != Control1948.Side.Israel) return false;
            }
            return true;
        }

        /// <summary>Whether a disc of <paramref name="r"/> at <paramref name="p"/> is on the ground the figures of a unit spot at
        /// <paramref name="c"/> (its disc <paramref name="radius"/>) hide: within the disc either side, from its south edge to
        /// <see cref="FigureShadowM"/> past its north edge.</summary>
        public static bool Hidden(Vector2 p, float r, Vector2 c, float radius) =>
            Mathf.Abs(p.x - c.x) < radius + r && p.y + r > c.y - radius && p.y - r < c.y + radius + FigureShadowM;

        /// <summary>Whether a disc of <paramref name="r"/> at <paramref name="p"/> keeps clear of the box centred at <paramref name="c"/>,
        /// turned <paramref name="yawDeg"/> anticlockwise in x-z (KitMesh.Corners), <paramref name="w"/> along its turn and
        /// <paramref name="d"/> across.</summary>
        public static bool ClearOfBox(Vector2 p, float r, Vector2 c, float yawDeg, float w, float d)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            var q = p - c; float u = q.x * ca + q.y * sa, v = -q.x * sa + q.y * ca;
            float du = Mathf.Max(0f, Mathf.Abs(u) - 0.5f * w), dv = Mathf.Max(0f, Mathf.Abs(v) - 0.5f * d);
            return du * du + dv * dv >= r * r;
        }

        /// <summary>Review of #2281: what else the kits drew on the hex about <paramref name="anchor"/>, which no house stands on by
        /// design: the walls, platforms, landmarks, harbour works and street palms the kits placed and each landmark's close copy
        /// (KitLayer.Stats.Landmarks), and the replaceable landmark parts' close boxes (KitLayer.Stats.LandmarkParts, #2266). All
        /// planned at map build. Those within <paramref name="extent"/> of <paramref name="anchor"/> (the hex's inner radius, or
        /// the ground a search covers, #2876) plus half their width and depth.</summary>
        internal static List<(Vector2 c, float yaw, float w, float d)> TownLandmarkBoxes(MapScene map, Vector2 anchor, float extent = HexInnerRadiusM)
        {
            var boxes = new List<(Vector2 c, float yaw, float w, float d)>();
            var stats = map.KitStats;
            if (stats == null) return boxes;
            foreach (var p in stats.Landmarks)
                if ((p.C - anchor).magnitude <= extent + 0.5f * (p.W + p.D)) boxes.Add((p.C, p.Yaw, p.W, p.D));
            foreach (var part in stats.LandmarkParts)
            {
                var c = new Vector2(part.Near.center.x, part.Near.center.z);
                if ((c - anchor).magnitude <= extent + 0.5f * (part.Near.size.x + part.Near.size.z)) boxes.Add((c, 0f, part.Near.size.x, part.Near.size.z));
            }
            return boxes;
        }

        /// <summary>The drawn buildings on the hex about <paramref name="anchor"/>: each town model instance the kit planned at
        /// map build (TownModelLayer.InstancesOf; never what has streamed) for every footprint that reaches the hex, as a disc of
        /// its half diagonal, but a major city's (TownModels.IsMajorCity), which draws its kit blocks at every zoom and never its
        /// models (#2876: <see cref="MajorCityBlocksNear"/>); <paramref name="names"/>: those footprints, a major city's
        /// included (their names stand above them).</summary>
        internal static List<(Vector2 centre, float radius)> TownBuildings(MapScene map, Vector2 anchor, out List<(Vector2 centre, float radius)> names)
        {
            var discs = new List<(Vector2 centre, float radius)>(); names = new List<(Vector2 centre, float radius)>();
            var layer = map.GetComponentInChildren<Kits.TownModelLayer>(true);
            foreach (var f in map.KitFootprints)
            {
                if ((anchor - f.centre).magnitude >= f.radius + HexInnerRadiusM) continue;
                names.Add((f.centre, f.radius));
                if (layer == null || Kits.TownModels.IsMajorCity(f.id)) continue;
                foreach (var b in layer.InstancesOf(f.id)) discs.Add((b.C, 0.5f * Mathf.Sqrt(b.W * b.W + b.D * b.D)));
            }
            return discs;
        }

        /// <summary>#2876: the major cities' kit blocks (MapScene.MajorCityBlocks: every "building" and "wing" their kits placed,
        /// drawn at every zoom) that may reach the hex about <paramref name="anchor"/>, as boxes (their yaw as KitMesh.Corners),
        /// in placement order. A block's reach is taken as half its width plus half its depth (no less than its half diagonal).
        /// <paramref name="extent"/>: the hex's inner radius, or the ground a search covers about <paramref name="anchor"/> (#2876).</summary>
        internal static List<(Vector2 c, float yaw, float w, float d)> MajorCityBlocksNear(MapScene map, Vector2 anchor, float extent = HexInnerRadiusM)
        {
            var boxes = new List<(Vector2 c, float yaw, float w, float d)>();
            foreach (var b in map.MajorCityBlocks)
                if ((b.C - anchor).magnitude <= extent + 0.5f * (b.W + b.D)) boxes.Add((b.C, b.Yaw, b.W, b.D));
            return boxes;
        }

        /// <summary>Whether a disc of <paramref name="r"/> at <paramref name="p"/> keeps clear of every box of
        /// <paramref name="boxes"/> (<see cref="ClearOfBox"/>; a box whose centre is past r plus half its width and half its depth
        /// cannot reach it and is skipped).</summary>
        internal static bool ClearOfBoxes(Vector2 p, float r, List<(Vector2 c, float yaw, float w, float d)> boxes)
        {
            foreach (var (c, yaw, w, d) in boxes)
            {
                float reach = r + 0.5f * (w + d);
                if ((p - c).sqrMagnitude > reach * reach) continue;
                if (!ClearOfBox(p, r, c, yaw, w, d)) return false;
            }
            return true;
        }

        /// <summary>#2876: the tangent of the view's shallowest ray. CameraRig turns the camera Euler(Pitch, Yaw, 0) at every zoom
        /// (CameraRig.Apply; no pitch curve), so on the screen's centre column a point on its centre row is seen at Pitch below
        /// the horizon and one at its top edge at Pitch - Fov / 2, 32.5 degrees, the lowest (as <see cref="FigureShadowM"/>).
        /// The top bar's inset (CameraRig.TopInset) only lowers the map area's top edge, so it never looks lower. Toward the
        /// screen's top corners a ray looks lower still and turns off north; that is not modelled.</summary>
        public static float SightTan => Mathf.Tan((CameraRig.Pitch - 0.5f * CameraRig.Fov) * Mathf.Deg2Rad);

        /// <summary>#2876: the b21's top over its ground, as a share of its <see cref="SizeOf(Want)"/>. Its FBX's vertex extents are
        /// 0.959 by 0.999 across and 0.549 high (b21-artisan-workshop-v2.fbx, read 6 November 2026) and Rebuild scales its wider
        /// side to SizeOf, so its top is 0.55 of it; taken a little low, as a lower top hides behind more.</summary>
        public const float WorkshopTopShare = 0.5f;

        /// <summary>#2876: how far above its census wall height (Placed.H) a major city's block reaches (m). Tel Aviv's blocks
        /// (SettlementKits.TelAvivBlock, its only lattice) at most a stair head, 2.8 real m over the wall top, and its water
        /// tank, 1.6 real m on that, at KitScale.CityBlocks. Another major city's buildings are white, stone and old blocks and
        /// houses at either scale (the most over a flat roof the same 4.4 real m) and Haifa's Templer houses, whose gable rises
        /// 0.55 of the house's narrower side (SettlementKits.ModelHeight's Templer).</summary>
        internal static float RoofRiseOf(string id, float w, float d) => id == "tel_aviv" ? 4.4f * Kits.KitScale.CityBlocks
            : Mathf.Max(4.4f * Mathf.Max(Kits.KitScale.CityBlocks, Kits.KitScale.Houses), 0.55f * Mathf.Min(w, d));

        /// <summary>#2876: a major city's kit block as <see cref="InSight"/> reads it: its box (as KitMesh.Corners) and its top,
        /// world y.</summary>
        internal readonly struct SightBlock
        {
            public readonly string Id; public readonly Vector2 C; public readonly float Yaw, W, D, Top;
            public SightBlock(string id, Vector2 c, float yaw, float w, float d, float top) { Id = id; C = c; Yaw = yaw; W = w; D = d; Top = top; }
        }

        /// <summary>#2876: the major cities' kit blocks (MapScene.MajorCityBlocks) that may hide a model on the hex about
        /// <paramref name="anchor"/> from the camera, in placement order. A block's top is the ground it stands on (the highest
        /// of its corners and centre, as SettlementKits.GroundSpan stands it) plus its wall height and <see cref="RoofRiseOf"/>.
        /// It can hide ground as far as its top over y = 0 (the terrain's lowest point: CameraController.MinClearanceShare)
        /// over <paramref name="tanPitch"/> north of it. <paramref name="extent"/>: the hex's inner radius, or the ground a search
        /// covers about <paramref name="anchor"/> (#2876).</summary>
        internal static List<SightBlock> MajorCitySightBlocks(MapScene map, Kits.KitGround ground, Vector2 anchor, float tanPitch, float extent = HexInnerRadiusM)
        {
            var list = new List<SightBlock>();
            foreach (var b in map.MajorCityBlocks)
            {
                float a = b.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a), hi = float.MinValue;
                for (int i = 0; i < 5; i++)
                {
                    float u = i == 4 ? 0f : ((i & 1) == 0 ? -0.5f : 0.5f) * b.W, v = i == 4 ? 0f : ((i & 2) == 0 ? -0.5f : 0.5f) * b.D;
                    hi = Mathf.Max(hi, ground.Y(b.C.x + u * ca - v * sa, b.C.y + u * sa + v * ca));
                }
                float top = hi + b.H + RoofRiseOf(b.Id, b.W, b.D);
                float reach = extent + 0.5f * (b.W + b.D) + Mathf.Max(0f, top) / tanPitch;
                if ((b.C - anchor).sqrMagnitude <= reach * reach) list.Add(new SightBlock(b.Id, b.C, b.Yaw, b.W, b.D, top));
            }
            return list;
        }

        /// <summary>#2876 (coordinator, 6 November 2026: the b21 must be visible among the major cities' tall blocks): whether
        /// the camera sees the whole top of a model at <paramref name="p"/>, its ground a square <paramref name="size"/> a side
        /// turned <paramref name="yawDeg"/> (as KitMesh.Corners), its top at world y <paramref name="topY"/>, past every block of
        /// <paramref name="blocks"/>. The camera looks north (CameraRig yaw 0: fixed north under gdd-0.3, the only ruleset with
        /// workshops, CameraHeadingPolicy.IsFixedNorth), so it stands south of the model (x east, z north) and a ray from the
        /// top toward it runs south (-z), rising <paramref name="tanPitch"/> a metre. A block whose top is
        /// <c>rise</c> above the model's hides it when its box meets the square swept <c>rise / tanPitch</c> south: the hull of
        /// the square and its copy that far south, tested against the box on their side axes and the sweep's own (x), with
        /// touching as apart (as <see cref="BoxesClear"/>). A block no higher than the top hides nothing.</summary>
        internal static bool InSight(Vector2 p, float yawDeg, float size, float topY, List<SightBlock> blocks, float tanPitch)
        {
            float a = yawDeg * Mathf.Deg2Rad, half = 0.5f * size;
            var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var v = new Vector2(-u.y, u.x);
            float ex = half * (Mathf.Abs(u.x) + Mathf.Abs(v.x)), ez = half * (Mathf.Abs(u.y) + Mathf.Abs(v.y));
            foreach (var b in blocks)
            {
                float rise = b.Top - topY;
                if (rise <= 0f) continue;
                float run = rise / tanPitch, reach = 0.5f * (b.W + b.D);
                // Its box's bounds (within reach of its centre) against the swept square's: apart, it cannot hide the model.
                if (b.C.x - reach >= p.x + ex || b.C.x + reach <= p.x - ex || b.C.y - reach >= p.y + ez || b.C.y + reach <= p.y - ez - run) continue;
                float ba = b.Yaw * Mathf.Deg2Rad;
                var bu = new Vector2(Mathf.Cos(ba), Mathf.Sin(ba)); var bv = new Vector2(-bu.y, bu.x);
                var t = b.C - p;
                bool Apart(Vector2 n)
                {
                    float r1 = half * (Mathf.Abs(Vector2.Dot(u, n)) + Mathf.Abs(Vector2.Dot(v, n)));
                    float back = -run * n.y;   // the swept copy's centre, projected (the model's centre projects to 0)
                    float lo = Mathf.Min(0f, back) - r1, hi = Mathf.Max(0f, back) + r1;
                    float cb = Vector2.Dot(t, n), rb = 0.5f * b.W * Mathf.Abs(Vector2.Dot(bu, n)) + 0.5f * b.D * Mathf.Abs(Vector2.Dot(bv, n));
                    return hi <= cb - rb || lo >= cb + rb;
                }
                if (!(Apart(u) || Apart(v) || Apart(Vector2.right) || Apart(bu) || Apart(bv))) return false;
            }
            return true;
        }

        /// <summary>Whether two boxes, each centred at c, turned yaw degrees anticlockwise in x-z (KitMesh.Corners), w along its
        /// turn and d across, are apart (touching counts as apart): one of their four side axes separates them.</summary>
        public static bool BoxesClear(Vector2 c1, float yaw1, float w1, float d1, Vector2 c2, float yaw2, float w2, float d2)
        {
            float a1 = yaw1 * Mathf.Deg2Rad, a2 = yaw2 * Mathf.Deg2Rad;
            var u1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)); var v1 = new Vector2(-u1.y, u1.x);
            var u2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)); var v2 = new Vector2(-u2.y, u2.x);
            var t = c2 - c1;
            bool Apart(Vector2 n) => Mathf.Abs(Vector2.Dot(t, n)) >= 0.5f * w1 * Mathf.Abs(Vector2.Dot(u1, n)) + 0.5f * d1 * Mathf.Abs(Vector2.Dot(v1, n))
                + 0.5f * w2 * Mathf.Abs(Vector2.Dot(u2, n)) + 0.5f * d2 * Mathf.Abs(Vector2.Dot(v2, n));
            return Apart(u1) || Apart(v1) || Apart(u2) || Apart(v2);
        }

        /// <summary>DESIGN VALUES (#2876; the owner, 6 November 2026, verbatim: "make the workshop at the edge of the city"): how far
        /// past the outermost kit block on its bearing a major city's b21 may stand (m, from the block's face to the b21's
        /// clearance disc; about one plot of the major cities' kit lattices, which are 0.8 to 1.6 km a side at 60x), and how many
        /// compass bearings round the city's centre its edge is walked on (3.75 degrees apart: about 460 m at Tel Aviv's 7 km
        /// edge, a little more than the disc's 410 m width).</summary>
        public const float EdgeGapM = 1000f;
        public const int EdgeBearings = 96;

        /// <summary>One named test of a candidate spot, given the spot and the clearance (<see cref="EdgeSpot"/> counts each).</summary>
        internal readonly struct SpotCheck
        {
            public readonly string Name; public readonly Func<Vector2, float, bool> Test;
            public SpotCheck(string name, Func<Vector2, float, bool> test) { Name = name; Test = test; }
        }

        /// <summary>#2876 (the owner, 6 November 2026, verbatim: "make the workshop at the edge of the city"): a spot for a b21 of
        /// clearance <paramref name="clearance"/> (its square <paramref name="size"/> a side, drawn at yaw 0) at the edge of the
        /// major city <paramref name="city"/>'s kit blocks (MapScene.MajorCityBlocks), or null, with <paramref name="tally"/>
        /// saying how many points each test turned away first.
        /// The points, a fixed walk: <see cref="EdgeBearings"/> compass bearings from the city's footprint centre, due south first
        /// (the camera's side: it looks north under gdd-0.3, CameraHeadingPolicy, so the city's blocks stand behind a b21 south of
        /// them, not between it and the camera), then 3.75 degrees either side of south in turn (east or west first by the parity
        /// of the key's FNV-1a hash), out to due north last. On each bearing, the edge is the farthest reach along it of the
        /// city's blocks in the band the disc sweeps (its clearance either side of the ray); the points stand from just past it
        /// (the disc touching that face) out to <see cref="EdgeGapM"/> past it, every half clearance, nearest first. A bearing
        /// with no block of the city in its band has no points.
        /// Two passes over the points: first only those whose disc is wholly on the project's hex (about
        /// <paramref name="anchor"/>, as every other workshop stands), then any (the edge of Tel Aviv and Jerusalem lies off
        /// their hexes: their footprints cover them). A point passes, in this order: on the map and dry; within EdgeGapM plus
        /// its clearance of a block of the city, and nearer the city's blocks than any other major city's (box distance); on the
        /// city's ground (deeper in its footprint, distance over radius, than in any
        /// other place's, the kits' partition, SettlementKits.OwnsFootprint; and clear of every other place's plot reach,
        /// SettlementKits.PlotReach of its radius, where that place's buildings stand), so it reads as that city's; clear of
        /// every place's name pill (<see cref="ClearOfName"/>), of every major city's kit block and of the kits' landmarks
        /// (<see cref="TownLandmarkBoxes"/>); off the beach (no open sea, KitGround.SeaDistance, within the drawn full sand,
        /// BeachBands.FullSandAt, plus its clearance); <paramref name="free"/> (the plan's held ground, farms, models, routes,
        /// tokens and figures); and in the camera's sight past every major city's blocks (<see cref="InSight"/>). The points and
        /// the tests read only the map, the key and the caller's lists, so the spot is the same whenever the plan is made.</summary>
        internal static Vector2? EdgeSpot(MapScene map, Kits.KitGround ground, string city, string key, Vector2 anchor, float clearance, float size,
            IReadOnlyList<SpotCheck> free, out string tally)
        {
            var home = map.KitFootprints.First(f => f.id == city);
            Vector2 c = home.centre;
            // The city's blocks: each one's centre from the city's centre, its sides' directions (as KitMesh.Corners) and half sides.
            var own = new List<(Vector2 q, Vector2 u, Vector2 v, float hw, float hd)>();
            foreach (var b in map.MajorCityBlocks)
            {
                if (b.Id != city) continue;
                float a = b.Yaw * Mathf.Deg2Rad; var bu = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                own.Add((b.C - c, bu, new Vector2(-bu.y, bu.x), 0.5f * b.W, 0.5f * b.D));
            }
            // Every other major city's blocks, the same way (from the city's centre too).
            var others = new List<(Vector2 q, Vector2 u, Vector2 v, float hw, float hd)>();
            foreach (var b in map.MajorCityBlocks)
            {
                if (b.Id == city) continue;
                float a = b.Yaw * Mathf.Deg2Rad; var bu = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                others.Add((b.C - c, bu, new Vector2(-bu.y, bu.x), 0.5f * b.W, 0.5f * b.D));
            }
            uint h = 2166136261;
            foreach (char ch in key) { h ^= ch; h *= 16777619; }
            int first = (h & 1u) == 0u ? 1 : -1;
            float step = 0.5f * clearance; int steps = Mathf.FloorToInt(EdgeGapM / step);
            var points = new List<Vector2>();
            for (int i = 0; i < EdgeBearings; i++)
            {
                int k = (i + 1) / 2 * (i % 2 == 1 ? first : -first);   // 0, +1, -1, +2, -2 ... (in degrees, k x 3.75), 48 last
                float bearing = (180f + k * (360f / EdgeBearings)) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing)); var across = new Vector2(-dir.y, dir.x);
                float edge = float.NegativeInfinity;
                foreach (var (q, u, v, hw, hd) in own)
                {
                    float along = hw * Mathf.Abs(Vector2.Dot(u, dir)) + hd * Mathf.Abs(Vector2.Dot(v, dir));
                    float wide = hw * Mathf.Abs(Vector2.Dot(u, across)) + hd * Mathf.Abs(Vector2.Dot(v, across));
                    float reach = Vector2.Dot(q, dir) + along;
                    if (reach > 0f && Mathf.Abs(Vector2.Dot(q, across)) - wide < clearance) edge = Mathf.Max(edge, reach);
                }
                if (float.IsNegativeInfinity(edge)) continue;
                for (int j = 0; j <= steps; j++) points.Add(c + dir * (edge + clearance + j * step));
            }
            // The ground the points' discs cover about the city's centre, and what stands there.
            float extent = 0f;
            foreach (var p in points) extent = Mathf.Max(extent, (p - c).magnitude);
            extent += clearance;
            var blocks = MajorCityBlocksNear(map, c, extent);
            var landmarks = TownLandmarkBoxes(map, c, extent);
            float tan = SightTan;
            var sight = MajorCitySightBlocks(map, ground, c, tan, extent);
            // The places that can matter to a point: a footprint deeper in at one (it is then within extent times its radius
            // over the city's), a name pill over one, or a plot reach under one.
            var places = new List<(Vector2 centre, float radius, string id)>();
            foreach (var f in map.KitFootprints)
            {
                if (f.id == city) continue;
                float matters = Mathf.Max(extent * f.radius / home.radius, TownLabelHalfWidthM + TownLabelHeightM + f.radius + 2f * clearance,
                    f.radius * Kits.SettlementKits.PlotReach + clearance);
                if ((f.centre - c).magnitude < extent + matters) places.Add(f);
            }
            // The box distance (m) from a point to the nearest of <boxes>.
            float Nearest(Vector2 p, List<(Vector2 q, Vector2 u, Vector2 v, float hw, float hd)> boxes)
            {
                float best = float.MaxValue;
                foreach (var (q, u, v, hw, hd) in boxes)
                {
                    var d = c + q - p;
                    float du = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, u)) - hw), dv = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, v)) - hd);
                    best = Mathf.Min(best, du * du + dv * dv);
                }
                return Mathf.Sqrt(best);
            }
            bool CitysGround(Vector2 p, float r)
            {
                float mine = (p - c).magnitude / home.radius;
                foreach (var (fc, fr, _) in places)
                {
                    float d = (p - fc).magnitude;
                    if (d / fr <= mine || d < fr * Kits.SettlementKits.PlotReach + r) return false;
                }
                return true;
            }
            bool Unnamed(Vector2 p, float r)
            {
                if (!ClearOfName(p, r, c, home.radius)) return false;
                foreach (var (fc, fr, _) in places) if (!ClearOfName(p, r, fc, fr)) return false;
                return true;
            }
            List<(Vector2 centre, float reach)>? sand = null;
            bool OffBeach(Vector2 p, float r)
            {
                sand ??= Kits.BeachBands.Reaches(map.Settlements.Concat(map.Localities.Select(l => l.Settlement)).ToList(), ground);
                return ground.SeaDistance(p.x, p.y, Kits.BeachBands.FullSandAt(map.Frame, p.x, p.y, sand) + r) == float.MaxValue;
            }
            var checks = new List<SpotCheck>
            {
                new SpotCheck("hex", (p, r) => (p - anchor).magnitude + r <= HexInnerRadiusM),
                new SpotCheck("map", (p, r) => ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)),
                new SpotCheck("water", (p, r) => !ground.Water(p.x, p.y)),
                new SpotCheck("near", (p, r) => Nearest(p, own) <= EdgeGapM + clearance),
                new SpotCheck("nearer", (p, r) => Nearest(p, own) < Nearest(p, others)),
                new SpotCheck("ground", CitysGround),
                new SpotCheck("names", Unnamed),
                new SpotCheck("blocks", (p, r) => ClearOfBoxes(p, r, blocks)),
                new SpotCheck("landmarks", (p, r) =>
                {
                    foreach (var (bc, yaw, bw, bd) in landmarks) if (!ClearOfBox(p, r, bc, yaw, bw, bd)) return false;
                    return true;
                }),
                new SpotCheck("beach", OffBeach),
            };
            checks.AddRange(free);
            checks.Add(new SpotCheck("sight", (p, r) => InSight(p, 0f, size, ground.Y(p.x, p.y) + WorkshopTopShare * size, sight, tan)));
            var turned = new int[checks.Count];
            // First on the project's hex (every test), then anywhere (every test but "hex", the first).
            for (int pass = 0; pass < 2; pass++)
            {
                Array.Clear(turned, 0, turned.Length);
                foreach (var p in points)
                {
                    int failed = -1;
                    for (int i = pass; i < checks.Count && failed < 0; i++) if (!checks[i].Test(p, clearance)) failed = i;
                    if (failed < 0) { tally = ""; return p; }
                    turned[failed]++;
                }
            }
            tally = FormattableString.Invariant($"{points.Count} points past its kit blocks, none on or off its hex passing; turned away first by ")
                + string.Join(", ", checks.Select((x, i) => FormattableString.Invariant($"{x.Name} {turned[i]}")));
            return null;
        }

        /// <summary>#1701 slice F, owner 2 November 2026 ("Among the houses"): a workshop on a hex a town's footprint covers stands
        /// in a gap between the drawn buildings on the hex (<see cref="TownBuildings"/>, and a major city's kit blocks,
        /// <see cref="MajorCityBlocksNear"/>, #2876), off the kits' walls, platforms and landmarks (<see cref="TownLandmarkBoxes"/>),
        /// clear of every name there (<see cref="ClearOfName"/>) and <paramref name="free"/> (the hex, the routes, the tokens, the
        /// other models). A fixed search: rings half a clearance apart outward from the hex centre, each from an FNV-1a bearing of
        /// the key. Where no gap is clear and major-city blocks reach the hex (their streets are narrower than the clearance disc:
        /// Tel Aviv's 60x blocks stand 180 to 360 m apart, the disc is about 409 m across), the same search for a street: the
        /// model's ground taken as its square, <paramref name="size"/> a side (b21 is nearly square from above: vertex extents
        /// 0.959 by 0.999 FBX units, read from b21-artisan-workshop-v2.fbx on 6 November 2026, centred, so its square holds it),
        /// turned with the nearest block within reach, clear of every block and kit landmark as boxes, every other test
        /// unchanged (the disc); <paramref name="along"/> is then that street's direction (world x, z, unit length), for the model
        /// to lie along it. Both searches also take only a spot <paramref name="seen"/> passes, given the spot and the turn (degrees,
        /// as KitMesh.Corners) the model is drawn at: 0 in a gap, the street's on a street (#2876: <see cref="InSight"/> on a hex
        /// a major city's blocks reach, true elsewhere). Null when neither is clear.</summary>
        internal static Vector2? TownGap(MapScene map, string key, Vector2 anchor, float clearance, float size, Func<Vector2, float, bool> free,
            Func<Vector2, float, bool> seen, out Vector2? along)
        {
            along = null;
            var houses = TownBuildings(map, anchor, out var names);
            var blocks = MajorCityBlocksNear(map, anchor);
            var landmarks = TownLandmarkBoxes(map, anchor);
            uint h = 2166136261;
            foreach (char c in key) { h ^= c; h *= 16777619; }
            Vector2? Search(Func<Vector2, bool> clear)
            {
                float step = 0.5f * clearance;
                for (float radius = 0f; radius + clearance <= HexInnerRadiusM; radius += step)
                {
                    int n = radius == 0f ? 1 : Mathf.Max(6, Mathf.CeilToInt(2f * Mathf.PI * radius / step));
                    int start = (int)(h % (uint)n);
                    for (int i = 0; i < n; i++)
                    {
                        float angle = 2f * Mathf.PI * ((start + i) % n) / n;
                        var p = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                        if (clear(p)) return p;
                    }
                }
                return null;
            }
            bool Named(Vector2 p)
            {
                foreach (var (c, fr) in names) if (!ClearOfName(p, clearance, c, fr)) return true;
                return false;
            }
            var gap = Search(p =>
            {
                if (Named(p) || !TownLandmarks.Clear(p, clearance, houses) || !ClearOfBoxes(p, clearance, blocks)) return false;
                foreach (var (bc, yaw, bw, bd) in landmarks) if (!ClearOfBox(p, clearance, bc, yaw, bw, bd)) return false;
                return free(p, clearance) && seen(p, 0f);
            });
            if (gap != null || blocks.Count == 0) return gap;
            float streetYaw = 0f;
            var street = Search(p =>
            {
                if (Named(p) || !TownLandmarks.Clear(p, clearance, houses)) return false;
                // The street's turn: the nearest block's (a street runs square to its blocks); with no block within reach of
                // the square, the disc search above has already judged the spot.
                float best = float.MaxValue; bool any = false;
                foreach (var (bc, yaw, bw, bd) in blocks)
                {
                    float reach = size + 0.5f * (bw + bd);
                    if ((p - bc).sqrMagnitude > reach * reach) continue;
                    float a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    var q = p - bc; float u = q.x * ca + q.y * sa, v = -q.x * sa + q.y * ca;
                    float du = Mathf.Max(0f, Mathf.Abs(u) - 0.5f * bw), dv = Mathf.Max(0f, Mathf.Abs(v) - 0.5f * bd);
                    float dist = du * du + dv * dv;
                    if (dist < best) { best = dist; streetYaw = yaw; any = true; }
                }
                if (!any) return false;
                foreach (var (bc, yaw, bw, bd) in blocks)
                {
                    float reach = size + 0.5f * (bw + bd);
                    if ((p - bc).sqrMagnitude > reach * reach) continue;
                    if (!BoxesClear(p, streetYaw, size, size, bc, yaw, bw, bd)) return false;
                }
                foreach (var (bc, yaw, bw, bd) in landmarks) if (!BoxesClear(p, streetYaw, size, size, bc, yaw, bw, bd)) return false;
                return free(p, clearance) && seen(p, streetYaw);
            });
            if (street != null) { float a = streetYaw * Mathf.Deg2Rad; along = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }
            return street;
        }

        void Rebuild()
        {
            CollectionResidencyMetrics.MarkDirty();
            if (map == null || map.Terrain == null) return;
            ClearVisuals();
            Replan(log: false);
            var ground = PlacementLayer.CampGround(map);
            var holes = new List<(Vector2 centre, float radius)>();
            var lanes = new List<(Vector2 at, float half, float baseY, float tan)>();
            var boxes = new List<Bounds[]>();
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab) || prefab == null) continue;
                if (!planned.TryGetValue(w.Key, out var spot)) continue;
                Vector2? at = spot.at, along = spot.along;
                holes.Add((spot.at, ClearanceOf(w)));
                var go = Instantiate(prefab, transform, false); go.name = w.Key;
                if (!originals.TryGetValue(w.Model, out var owned)) originals[w.Model] = owned = new List<GameObject>();
                owned.Add(go);
                go.transform.localRotation = Rotation(prefab.transform.localRotation, 0f);
                var b = Bounds(go); float edge = Mathf.Max(b.size.x, b.size.z);
                if (along is Vector2 road)
                {
                    // Its long side along the road: Unity's yaw turns +x toward -z.
                    float yaw = -Mathf.Atan2(road.y, road.x) * Mathf.Rad2Deg + (b.size.z > b.size.x ? 90f : 0f);
                    go.transform.localRotation = Rotation(prefab.transform.localRotation, yaw);
                    b = Bounds(go);
                }
                go.transform.localScale *= edge > 1e-3f ? SizeOf(w) / edge : 1f;
                var pos = new Vector3(at.Value.x, ground.Y(at.Value.x, at.Value.y), at.Value.y);
                go.transform.position = pos; b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.forceRenderingOff = !LastClose; renderers.Add(r); }
                Placed.Add((w.Key, w.Model, pos));
                if (w.Model == Workshop) boxes.Add(new[] { Bounds(go) });
                // #2876 (coordinator, 6 November 2026: a palm in front of the Jaffa b21 covered it): a major city's b21 also
                // keeps the kit trees out of its sight lane toward the camera (south), across its clearance (it holds the
                // model at any turn) and as far south as a tree's top would rise into the ray from its foot at the view's
                // shallowest depression (SightTan): the trees are 100 times life size (KitScale.Trees), the b21 24 times.
                if (w.Model == Workshop && Kits.TownModels.IsMajorCity(w.City)) lanes.Add((spot.at, ClearanceOf(w), pos.y, SightTan));
            }
            // Published once, whole (review of #2281, M1): a list handed to the labels is never changed after.
            workshopBoxes = boxes;
            // No kit tree stands on a building: the trees whose crowns reach its ground are left out at draw time, as under
            // the unit figures (Kits.VegetationLayer.Hide), and none in a major city's b21's sight lane.
            var veg = map.GetComponentInChildren<Kits.VegetationLayer>(true);
            if (veg != null) veg.Hide(Name, holes, lanes);
            if (prefabs.Count > 0) Debug.Log($"BUILDINGS: {Placed.Count} of {wants.Count} special buildings placed ({string.Join(", ", Placed.Select(p => p.Key))})"
                + (lanes.Count > 0 && veg != null ? FormattableString.Invariant($"; {veg.SightHidden} kit trees left out of {lanes.Count} b21 sight lanes") : ""));
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        /// <summary>#700's close-zoom switch with its hysteresis: on at TownModelLayer.CloseEnterM and nearer, off beyond
        /// TownModelLayer.CloseExitM.</summary>
        public static bool CloseAt(float distance, bool? was) =>
            was == true ? distance <= Statecraft.Game.Map.Kits.TownModelLayer.CloseExitM : distance <= Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM;
        void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (residencyView != null) { residencyDirty = false; residencyView.Clear(); residency?.Replace(Array.Empty<(string, CollectionDetail)>()); }
        }
        /// <summary>The render callback. A change of residency never instantiates or destroys models while a camera renders, in
        /// edit mode either (a Scene or Game view camera): it waits for LateUpdate, as in play mode.</summary>
        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam) => ChooseCore(cam, inRender: true);
        /// <summary>A direct call (tests and editor tools, outside any render) applies a change of residency at once in edit mode.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam) => ChooseCore(cam, inRender: false);
        internal void ChooseCore(Camera cam, bool inRender)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            if (residencyView != null) ObserveResidency(cam, inRender);
            int id = cam.GetInstanceID(); bool close = CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close; LastClose = close; foreach (var r in renderers) if (r != null) r.forceRenderingOff = !close;
        }
    }
}
