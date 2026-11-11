#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.HexTiles;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tal Heuberger's hex-tile props (#1155) on the main map (owner, 27 October 2026, "2+3": put them on the hex boards AND
    /// bring them to the main diorama map; owner, 2 November 2026: "make sure all of tal's new models are used"), only where the
    /// map's content gives the place:
    /// - the railway halt's station building at each station the 1948 railways name (content/map/routes_1948.yaml, a rail
    ///   route's `via` waypoint whose name says "station"), from its route's first day;
    /// - the desert-edge goat-hair tent beside each Bedouin camp (localities_1948.yaml `encampments`: the kits scatter one
    ///   symbolic camp per 1,000 people as `camp:` footprints, which RuralLife's camel and tent use too); the camps' own
    ///   tents stay as they are;
    /// - the Arab village's prickly-pear hedge beside each Arab village (localities_1948.yaml, type village), as the
    ///   arab_village hex tile draws it at the village's edge: a short row of 3 to 5 clumps along the edge (owner, 2 November
    ///   2026: "Hedge row of several clumps (Recommended)");
    /// - a light scatter of garrigue and scrub on open hill ground (the land hexes of biome hills or mountains in
    ///   content/map/hexes.json), up close only (owner, 2 November 2026: "Sparse, close zoom only (Recommended)");
    /// - and in place of the kit's own minarets, belfries and Jaffa's clock tower, Tal Heuberger's at every spot the kit builds
    ///   one, far off and up close (owner, 2 November 2026: "tal's high quality models replace the built ones everytwhere"):
    ///   the kit draws those parts (their meshes' submesh 1, KitLayer.LandmarkParts) until the models stand there, and keeps
    ///   them if the bundle fails.
    /// Each stands on the nearest clear ground to its place (<see cref="NearestSpot"/>: a station within one hex of its
    /// point, a tent or a hedge just outside its camp's or village's kit footprint), clear of every kit footprint, the
    /// routes, the unit tokens, water, the other Collection layers and each other, at the buildings' one world scale, and
    /// draws only up close (#700's switch). The landmark models draw where the kit's part would, far off or up close, each
    /// following its kit renderer's swap (TownModelLayer.Follow). The props stream from their own bundle
    /// (<see cref="HexProps"/>, 5.8 MB for WebGL), asked for by the first camera that renders (after the first frame) when the
    /// map has landmark parts to replace, else by the first close camera. A prop that does not stream stays off the map.</summary>
    [ExecuteAlways]
    public sealed class HexPropModels : MonoBehaviour
    {
        public const string Name = "HexPropModels";
        public const string Station = "tiles:railway_halt/station", Tent = "tiles:desert_edge/tent", PricklyPear = "tiles:arab_village/prickly_pear",
            Garrigue = "tiles:judean_highlands/garrigue", Scrub = "tiles:coastal_dunes/scrub",
            Minaret = "tiles:arab_village/minaret", Belfry = "tiles:jaffa_old_city/belfry", ClockTower = "tiles:jaffa_old_city/clock_tower";
        /// <summary>The props this layer places on clear ground (close up only).</summary>
        public static readonly string[] Standalone = { Station, Tent, PricklyPear, Garrigue, Scrub };
        /// <summary>The props that replace the kit's landmark parts, by the part's kind (KitLayer.LandmarkPart.Kind).</summary>
        public static readonly IReadOnlyDictionary<string, string> ByPart = new Dictionary<string, string>(StringComparer.Ordinal)
        { ["minaret"] = Minaret, ["belfry"] = Belfry, ["clock_tower"] = ClockTower };
        public static readonly string[] Models = { Station, Tent, PricklyPear, Garrigue, Scrub, Minaret, Belfry, ClockTower };
        /// <summary>The child that holds the landmark models (a rebuild of the standalone props leaves it alone).</summary>
        public const string LandmarksName = "Landmarks";

        /// <summary>The one world scale of Tal Heuberger's models (<see cref="Kits.SettlementKits.ModelScale"/>, issue #1032).</summary>
        public const float WorldScale = Kits.SettlementKits.ModelScale;

        /// <summary>Design values, not sourced measurements: the real long side (m) each prop stands for, seen from above. A
        /// small stone station building; the camp kit's goat-hair tents are 7 to 12 m (SettlementKits.Encampments, as
        /// RuralLife's tent); a prickly-pear clump a few metres across.</summary>
        public static float RealLengthM(string model) => model switch
        {
            Station => 12f, Tent => 10f, PricklyPear => 4f,
            // Design values: a low shrub a few metres across.
            Garrigue => 3f, Scrub => 3f,
            _ => throw new ArgumentException($"'{model}' is not one of the main map's hex props", nameof(model)),
        };

        public static float SizeOf(string model) => RealLengthM(model) * WorldScale;
        public static float ClearanceOf(string model) => 0.71f * SizeOf(model);

        /// <summary>How far beyond <see cref="Want.Start"/> the search for clear ground goes (m). Design values: a station may
        /// stand up to one map hex (<paramref name="hexSizeM"/>, the 4.4 km grid's circumradius) from its sourced point, since
        /// a station inside a town's kit footprint has its nearest clear ground beyond the town's edge; a tent or a hedge
        /// stays within four of its clearances of its place's footprint.</summary>
        public static float ReachOf(string model, float hexSizeM) => model == Station ? hexSizeM : 4f * ClearanceOf(model);

        // Design values: 24 bearings, the ring radius growing by half a clearance from one and a half clearances out.
        public const int SpotBearings = 24;
        public const float FirstRingClearances = 1.5f, RingStepClearances = 0.5f;

        /// <summary>Design values (owner, 2 November 2026: "A short row of 3-5 clumps along each village's edge"): a village's
        /// hedge is 3 to 5 clumps (FNV-1a of its key), one and a quarter clumps apart, along the tangent to its footprint.</summary>
        public static int HedgeClumps(string key) => 3 + (int)(RuralLife.StableHash(key + ":hedge") % 3u);
        public const float HedgeSpacingSizes = 1.25f;
        public static Vector2[] HedgeMembers(Want w, Vector2 spot)
        {
            var members = new Vector2[HedgeClumps(w.Key)];
            HedgeMembers(w, spot, members);
            return members;
        }

        /// <summary>As HedgeMembers, into <paramref name="into"/> (at least five long); returns how many.</summary>
        public static int HedgeMembers(Want w, Vector2 spot, Vector2[] into)
        {
            var radial = spot - w.Anchor; var along = radial.sqrMagnitude > 1e-6f ? new Vector2(-radial.y, radial.x).normalized : Vector2.right;
            int n = HedgeClumps(w.Key);
            for (int i = 0; i < n; i++) into[i] = spot + along * ((i - 0.5f * (n - 1)) * HedgeSpacingSizes * SizeOf(PricklyPear));
            return n;
        }

        /// <summary>Design values (owner, 2 November 2026: "A light scatter on open hill ground under 40 km"): one garrigue and
        /// one scrub per land hex of biome hills or mountains, each at a point FNV-1a picks within ScatterReachHex of the hex's
        /// size from its centre, moved to the nearest clear ground.</summary>
        public static readonly string[] HillBiomes = { "hills", "mountains" };
        public const float ScatterReachHex = 0.6f;

        /// <summary>The nearest clear point to <paramref name="anchor"/> on rings from <paramref name="start"/> plus
        /// FirstRingClearances clearances out, growing by RingStepClearances clearances up to <paramref name="start"/> plus
        /// <paramref name="reach"/>; on each ring the first bearing is chosen by FNV-1a of <paramref name="key"/> (no Unity
        /// random stream or simulation state moves). Null when none is clear.</summary>
        public static Vector2? NearestSpot(string key, Vector2 anchor, float clearance, Func<Vector2, float, bool> free, float start, float reach)
        {
            int first = (int)(RuralLife.StableHash(key) % SpotBearings);
            for (int ring = 0; ; ring++)
            {
                float radius = start + clearance * (FirstRingClearances + ring * RingStepClearances);
                if (radius > start + reach) return null;
                for (int k = 0; k < SpotBearings; k++)
                {
                    float angle = 2f * Mathf.PI * ((first + k) % SpotBearings) / SpotBearings;
                    var p = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (free(p, clearance)) return p;
                }
            }
        }

        /// <summary>A named station of the 1948 railways: its waypoint in content/map/routes_1948.yaml (name, point) and the first
        /// day the map draws a route through it (the earliest `from` of the routes whose `via` lists it). HexPropModelsTests
        /// checks this list against the file.</summary>
        public readonly struct RailStation
        {
            public readonly string Name; public readonly double Lon, Lat; public readonly DateTime From; public readonly DateTime? To;
            public RailStation(string name, double lon, double lat, DateTime from, DateTime? to = null) { Name = name; Lon = lon; Lat = lat; From = from; To = to; }
            /// <summary>Whether a route through it is drawn on <paramref name="date"/> (RouteWindow: from inclusive, to exclusive).</summary>
            public bool DrawnOn(DateTime date) => date >= From && (To == null || date < To.Value);
        }

        public static readonly RailStation[] Stations =
        {
            new RailStation("Jaffa railway station", 34.7614, 32.059, new DateTime(1920, 10, 5)),
            new RailStation("Lod railway station", 34.87556, 31.94583, new DateTime(1915, 1, 1)),
            new RailStation("Ramla railway station", 34.87684, 31.92936, new DateTime(1920, 10, 5)),
            new RailStation("Beit Shemesh railway station (Artuf)", 34.99088, 31.75744, new DateTime(1920, 10, 5)),
            new RailStation("Jerusalem (Khan) railway station", 35.22471, 31.76685, new DateTime(1920, 10, 5)),
            new RailStation("Rehovot railway station", 34.80639, 31.90889, new DateTime(1946, 1, 1)),
            new RailStation("Dir Snid station", 34.55776, 31.58195, new DateTime(1946, 1, 1)),
            new RailStation("Rosh HaAyin South railway station", 34.93472, 32.10417, new DateTime(1915, 1, 1)),
            new RailStation("Qaqun (Ahituv station)", 35.00194, 32.38356, new DateTime(1915, 1, 1)),
            new RailStation("Hadera East railway station", 34.95, 32.44611, new DateTime(1915, 1, 1)),
            new RailStation("Binyamina railway station", 34.94972, 32.51389, new DateTime(1918, 1, 1)),
            new RailStation("Atlit railway station", 34.9402, 32.69275, new DateTime(1918, 1, 1)),
            new RailStation("Haifa East railway station", 35.00472, 32.81194, new DateTime(1905, 10, 15)),
            new RailStation("Acre railway station", 35.08282, 32.92815, new DateTime(1942, 1, 1)),
            new RailStation("Nahariya railway station", 35.09889, 33.00528, new DateTime(1942, 1, 1)),
            new RailStation("Beit She'an former railway station", 35.4936, 32.5128, new DateTime(1905, 10, 15)),
            new RailStation("Samakh railway station", 35.587, 32.7032, new DateTime(1905, 10, 15)),
            new RailStation("Al-Hamma station", 35.66648, 32.68675, new DateTime(1905, 10, 15)),
            new RailStation("Petah Tikva railway station (1921)", 34.8813, 32.0968, new DateTime(1949, 9, 20)),
            new RailStation("Tel Aviv North railway station", 34.831, 32.1029, new DateTime(1949, 9, 20)),
        };

        public readonly struct Want : IEquatable<Want>
        {
            /// <summary>Key: "station:&lt;name&gt;", or the village's or camp's footprint id. Anchor: the station's sourced point, or
            /// the place's footprint centre. Start: where the search for clear ground starts (0 at a station's point; the
            /// place's footprint radius).</summary>
            public readonly string Key, Model; public readonly Vector2 Anchor; public readonly float Start;
            public Want(string key, string model, Vector2 anchor, float start) { Key = key; Model = model; Anchor = anchor; Start = start; }
            public bool Equals(Want o) => Key == o.Key && Model == o.Model && Anchor == o.Anchor && Start == o.Start;
            public override bool Equals(object? o) => o is Want w && Equals(w);
            public override int GetHashCode() => (Key, Model, Anchor, Start).GetHashCode();
        }

        /// <summary>Every prop the map wants on <paramref name="date"/>, in a fixed order: the stations as listed, then the
        /// camps and the villages by key.</summary>
        public static List<Want> Wants(MapScene map, DateTime date)
        {
            var result = StationWants(map, date);
            result.AddRange(FixedWants(map));
            return result;
        }

        /// <summary>The stations drawn on <paramref name="date"/>, in the listed order.</summary>
        static List<Want> StationWants(MapScene map, DateTime date)
        {
            var result = new List<Want>();
            foreach (var s in Stations)
            {
                if (!s.DrawnOn(date)) continue;
                var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
                result.Add(new Want("station:" + s.Name, Station, new Vector2(w.x, w.z), 0f));
            }
            return result;
        }

        /// <summary>The wants that do not change with the date: the camps, the villages by key and the hill scatter.</summary>
        static List<Want> FixedWants(MapScene map)
        {
            var result = new List<Want>();
            // Every camp: a sub-district's camps share their area's footprint id, so each is keyed by its id and its place
            // among them, in claim order (the kits' seeded scatter).
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            var camps = new List<Want>();
            foreach (var f in map.KitFootprints.Where(f => f.id.StartsWith("camp:", StringComparison.Ordinal)))
            {
                seen.TryGetValue(f.id, out int n); seen[f.id] = n + 1;
                camps.Add(new Want(f.id + "#" + n, Tent, f.centre, f.radius));
            }
            result.AddRange(camps.OrderBy(w => w.Key, StringComparer.Ordinal));
            var byId = new Dictionary<string, (Vector2 centre, float radius)>(StringComparer.Ordinal);
            foreach (var f in map.KitFootprints) if (!byId.ContainsKey(f.id)) byId[f.id] = (f.centre, f.radius);
            var villages = map.Localities.Select(p => p.Settlement).Where(s => s.Type == "village" && byId.ContainsKey(s.Id))
                .Select(s => s.Id).Distinct().OrderBy(k => k, StringComparer.Ordinal);
            foreach (var id in villages) result.Add(new Want(id, PricklyPear, byId[id].centre, byId[id].radius));
            float hexM = map.Hexes.hex_size_km * 1000f;
            foreach (var h in map.Hexes.hexes.Where(h => h.land && HillBiomes.Contains(h.biome)).OrderBy(h => h.r).ThenBy(h => h.q))
                foreach (var model in new[] { Garrigue, Scrub })
                {
                    string key = $"scatter:{h.q},{h.r}:{model.Substring(model.LastIndexOf('/') + 1)}";
                    uint hash = RuralLife.StableHash(key);
                    float a = (hash % 3600u) * Mathf.PI / 1800f, d = ScatterReachHex * hexM * Mathf.Sqrt((hash / 3600u % 1000u) / 1000f);
                    var c = map.Frame.ToWorld(h.lon, h.lat, 0);
                    result.Add(new Want(key, model, new Vector2(c.x, c.z) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d, 0f));
                }
            return result;
        }

        /// <summary>Test seam: how a prop is asked for (the hex props' own stream in a player, the AssetDatabase in the editor).</summary>
        internal static Action<string, Action<GameObject?>> Request = (id, done) =>
            CollectionStream.Request(HexProps.Catalog, id, CollectionDetail.Near, done);

        MapScene? map; DateTime date; bool shown, requested;
        /// <summary>The b21 of each built arms workshop (#1701 slice F), kept clear of with the sourced buildings.</summary>
        IReadOnlyList<SpecialBuildingModels.Want> workshops = Array.Empty<SpecialBuildingModels.Want>();
        int ruralVersion;
        // ShowAt's key (#2905): what the build read when it last re-planned.
        List<Want> stations = new List<Want>();
        IReadOnlyList<SpecialBuildingModels.Want> buildings = Array.Empty<SpecialBuildingModels.Want>();
        int tokenEpoch, peopleVersion; object? townsWanted;
        float maxPropOffsetShown; bool townEdgeFallbackShown;
        /// <summary>Whether a camera has come in to the close zoom: the standalone props are built only from then on (none of
        /// them draws further out, #2266 review).</summary>
        bool everClose;
        /// <summary>The standalone props need a build (an arrival or a date change) once a camera is close; in a render, it
        /// waits for LateUpdate (nothing is instantiated while a camera renders).</summary>
        bool standaloneDirty, standaloneQueued;
        /// <summary>A camera asked for the props while rendering: the request waits for LateUpdate, so an answer that comes at
        /// once never instantiates inside a render (#2266 review).</summary>
        bool askQueued;
        /// <summary>The close state last written to the standalone renderers (null: none yet): ChooseCore writes only a change.</summary>
        bool? appliedClose;
        List<Want> wants = new List<Want>();
        List<Want>? fixedWants;
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<Renderer> renderers = new List<Renderer>();
        /// <summary>The standalone props standing, by object name: a rebuild keeps one whose model and spot are unchanged.</summary>
        Dictionary<string, (GameObject go, string model, Vector2 spot)> live = new Dictionary<string, (GameObject, string, Vector2)>(StringComparer.Ordinal);
        List<(Vector2 centre, float radius)>? lastHoles;
        /// <summary>Every standalone prop placed: its want's key, model, ground point and object name (a hedge's clumps share
        /// their village's key).</summary>
        public readonly List<(string Key, string Model, Vector3 At, string Name)> Placed = new List<(string, string, Vector3, string)>();
        /// <summary>Every landmark model standing in for a kit part: owner, kind, far or close, object name.</summary>
        public readonly List<(string Owner, string Kind, bool Close, string Name)> Replaced = new List<(string, string, bool, string)>();
        /// <summary>The kit renderers (by name) whose own parts are hidden because Tal Heuberger's models stand in for them.</summary>
        public readonly List<string> Hidden = new List<string>();
        public IReadOnlyList<Want> Wanted => wants;
        /// <summary>Whether the props were asked for: by the first camera to render when there are landmark parts, else by the
        /// first close camera.</summary>
        public bool Requested => requested;
        public bool LastClose { get; private set; }
        /// <summary>The last standalone build's phases (ms): the keep-clear ground, the search, the instances and the total (the
        /// rest is the trees' pass; a test aid).</summary>
        public (long others, long search, long instances, long total) LastBuildMs { get; private set; }

        public static HexPropModels? ShowAt(MapScene map, DateTime date) => ShowAt(map, date, Array.Empty<SpecialBuildingModels.Want>());

        /// <summary>As <see cref="ShowAt(MapScene, DateTime)"/>, keeping clear of the b21 planned at each built arms workshop of the
        /// state too (SpecialBuildingModels.ArmsWorkshops; gdd-0.3 only, #1701 slice F).</summary>
        public static HexPropModels? ShowAt(MapScene map, DateTime date, Statecraft.Sim.Content content, IEnumerable<Statecraft.Sim.CityProject> projects) =>
            ShowAt(map, date, SpecialBuildingModels.ArmsWorkshops(content, projects));

        /// <summary>As <see cref="ShowAt(MapScene, DateTime)"/>, with <paramref name="workshops"/> (SpecialBuildingModels.ArmsWorkshops,
        /// computed once by the caller for both layers).</summary>
        internal static HexPropModels? ShowAt(MapScene map, DateTime date, IReadOnlyList<SpecialBuildingModels.Want> workshops)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<HexPropModels>();
            if (layer == null)
            {
                var go = new GameObject(Name); go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<HexPropModels>(); layer.map = map;
            }
            // GameRoot calls this on every refresh. The props re-plan on events only, keyed on what the build (Rebuild and Others)
            // reads, not on the raw date (#2905, as SpecialBuildingModels.ShowAt since #3056): the dated stations shown (their
            // RouteWindow), the special buildings' wanted list (the dated rows and the built arms workshops), the dated tokens
            // still present (the build's and the special buildings' plan's only other read of the date), the civilians' list
            // (CivilianPeople.WantedVersion: RuralLife.OtherProps, the special buildings' plan, and the rural plan while no rural
            // layer is shown), the rural plan (RuralLife.VersionOf; the workshops' b21 rebuild on it too, so both stand on one
            // plan), the whole-town models' wanted list (TownLandmarks.Wanted, swapped only on a change; Others' seat plan is
            // cached on the same object) and the special buildings' two test seams.
            var stations = StationWants(map, date);
            var buildings = workshops.Count == 0 ? SpecialBuildingModels.Placements(date)
                : SpecialBuildingModels.Placements(date).Concat(workshops).ToList();
            int tokens = SpecialBuildingModels.TokenEpoch(map, date);
            var shownPeople = map.Terrain.transform.Find(CivilianPeople.Name)?.GetComponent<CivilianPeople>();
            int people = shownPeople != null ? shownPeople.WantedVersion : 0;
            int rural = RuralLife.VersionOf(map);
            var shownTowns = map.Terrain.transform.Find(TownLandmarks.Name)?.GetComponent<TownLandmarks>();
            object? towns = shownTowns != null ? shownTowns.Wanted : null;
            bool same = layer.shown && layer.stations.SequenceEqual(stations) && SpecialBuildingModels.Same(layer.buildings, buildings)
                && layer.tokenEpoch == tokens && layer.peopleVersion == people && layer.ruralVersion == rural
                && ReferenceEquals(layer.townsWanted, towns) && layer.maxPropOffsetShown == SpecialBuildingModels.MaxPropOffset
                && layer.townEdgeFallbackShown == SpecialBuildingModels.TownEdgeFallback;
            // The build is the same on either date; the field still follows the game, so a later Rebuild (a stream answer, the
            // first close camera, a queued render build) builds on today's date.
            layer.date = date;
            if (same) return layer;
#if UNITY_EDITOR
            layer.Replans++;
#endif
            layer.fixedWants ??= FixedWants(map);
            layer.shown = true; layer.workshops = workshops; layer.buildings = buildings; layer.stations = stations;
            layer.tokenEpoch = tokens; layer.peopleVersion = people; layer.ruralVersion = rural; layer.townsWanted = towns;
            layer.maxPropOffsetShown = SpecialBuildingModels.MaxPropOffset; layer.townEdgeFallbackShown = SpecialBuildingModels.TownEdgeFallback;
            layer.wants = new List<Want>(stations); layer.wants.AddRange(layer.fixedWants);
            layer.standaloneDirty = true; layer.BuildStandaloneIfDue(inRender: false);
            return layer;
        }

#if UNITY_EDITOR
        /// <summary>How many times ShowAt has passed its early-out and re-planned the standalone props: a test aid (#2905), not
        /// in a player.</summary>
        internal int Replans { get; private set; }
#endif

        void Ask()
        {
            if (requested) return;
            requested = true;
            Boot.DiagLog.BootPhase("hex props asked");
            foreach (var id in Models)
            {
                string model = id;
                // Each answer builds what it can at once: one answer that never comes holds back nothing else (#2266 review).
                Request(model, p =>
                {
                    if (this == null || prefabs.ContainsKey(model)) return;   // a duplicate answer changes nothing
                    prefabs[model] = p;
                    if (p == null) Debug.LogWarning($"HEXPROPS: {model} did not stream; it stays off the map.");
                    if (ByPart.Values.Contains(model)) RebuildLandmarks();
                    else { standaloneDirty = true; BuildStandaloneIfDue(inRender: false); }
                });
            }
        }

        /// <summary>Builds the standalone props when they are due: something changed and a camera has come in close.</summary>
        void BuildStandaloneIfDue(bool inRender)
        {
            if (!standaloneDirty || !everClose) return;
            if (inRender)
            {
                standaloneQueued = true;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
                return;
            }
            standaloneDirty = standaloneQueued = false;
            Rebuild();
        }

        void LateUpdate()
        {
            if (askQueued) { askQueued = false; Ask(); }
            if (standaloneQueued) BuildStandaloneIfDue(inRender: false);
        }

        // ---- The keep-clear ground (#2266 review: planned, cached, indexed) ----

        IReadOnlyList<(string Key, Vector2 Core, float Reach)>? seatsPlan; object? seatsPlanWants;

        /// <summary>The ground the other Collection layers hold or will hold, so no prop stands on them: the civilian figures
        /// and the wildlife (as RuralLife.OtherProps), and the rural scenes', special buildings' and whole-town models' PLANNED
        /// spots (their plans with every model available, which is also where each of them draws its streamed models), never
        /// what has streamed so far: the props stand in the same places whatever the camera has visited, and a scene or
        /// building that streams in later never lands on one (#2266 review). The rural plan is the one RuralLife keeps for
        /// what it was last shown (RuralLife.PlanOf), the same one SpecialBuildingModels places the workshops' b21 against, so
        /// the props keep clear of the b21 that is drawn (#1701 slice F); the special buildings include the state's workshops.</summary>
        internal List<(Vector2 centre, float radius)> Others(MapScene map, DateTime date)
        {
            var discs = new List<(Vector2 centre, float radius)>(RuralLife.OtherProps(map));
            var rural = RuralLife.PlanOf(map);
            foreach (var (want, spot) in rural) discs.Add((spot, RuralLife.SceneRadiusM(want.Model)));
            // With the b21 of each built arms workshop (#1701 slice F), after the sourced buildings as their layer orders them.
            var buildings = workshops.Count == 0 ? SpecialBuildingModels.Placements(date)
                : SpecialBuildingModels.Placements(date).Concat(workshops).ToList();
            foreach (var (w, at) in SpecialBuildingModels.Plan(map, date, buildings, _ => true, log: false, rural))
                discs.Add((at, SpecialBuildingModels.ClearanceOf(w.Model)));
            // The whole-town models (TownLandmarks, shown first by GameRoot) where they will stand, by the same plan.
            var towns = map.Terrain.transform.Find(TownLandmarks.Name)?.GetComponent<TownLandmarks>();
            if (towns != null)
            {
                if (seatsPlan == null || !ReferenceEquals(seatsPlanWants, towns.Wanted)) { seatsPlan = towns.PlannedSeats(); seatsPlanWants = towns.Wanted; }
                foreach (var seat in seatsPlan) discs.Add((seat.Core, seat.Reach));
            }
            return discs;
        }

        /// <summary>Discs in a uniform grid: the same answer as TownLandmarks.Clear over all of them, checking only the cells a
        /// query's disc reaches (#2266 review: the linear scans made a build O(n²)).</summary>
        internal sealed class DiscGrid
        {
            const float Cell = 2000f;
            readonly Dictionary<(int, int), List<(Vector2 c, float r)>> cells = new Dictionary<(int, int), List<(Vector2, float)>>();
            public DiscGrid(IEnumerable<(Vector2 centre, float radius)> discs) { foreach (var (c, r) in discs) Add(c, r); }
            public void Add(Vector2 c, float r)
            {
                for (int x = Mathf.FloorToInt((c.x - r) / Cell); x <= Mathf.FloorToInt((c.x + r) / Cell); x++)
                    for (int z = Mathf.FloorToInt((c.y - r) / Cell); z <= Mathf.FloorToInt((c.y + r) / Cell); z++)
                    {
                        if (!cells.TryGetValue((x, z), out var list)) cells[(x, z)] = list = new List<(Vector2, float)>();
                        list.Add((c, r));
                    }
            }
            /// <summary>Whether the disc at <paramref name="p"/> of <paramref name="r"/> overlaps none of the discs.</summary>
            public bool Clear(Vector2 p, float r)
            {
                // A stored disc that overlaps the query is in every cell it covers, including the cell of their nearest
                // point, which is within the query's own box: so the query's box of cells finds it.
                for (int x = Mathf.FloorToInt((p.x - r) / Cell); x <= Mathf.FloorToInt((p.x + r) / Cell); x++)
                    for (int z = Mathf.FloorToInt((p.y - r) / Cell); z <= Mathf.FloorToInt((p.y + r) / Cell); z++)
                        if (cells.TryGetValue((x, z), out var list))
                            foreach (var (c, cr) in list) { float d = cr + r; if ((p - c).sqrMagnitude < d * d) return false; }
                return true;
            }
        }

        /// <summary>The heading (degrees about up) of the mapped route segment nearest <paramref name="p"/> (for a station, its
        /// sourced point, which lies on its railway's traced line), or null with no routes.</summary>
        internal static float? AlongNearestRoute(Vector2 p, IEnumerable<(List<Vector2> line, float halfWidth)> routes)
        {
            float best = float.MaxValue; float? yaw = null;
            foreach (var (line, _) in routes)
                for (int i = 0; i + 1 < line.Count; i++)
                {
                    var a = line[i]; var ab = line[i + 1] - a; if (ab.sqrMagnitude < 1e-6f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    float d = (a + ab * t - p).sqrMagnitude;
                    if (d < best) { best = d; yaw = -Mathf.Atan2(ab.y, ab.x) * Mathf.Rad2Deg; }
                }
            return yaw;
        }

        /// <summary>A fixed heading per place (FNV-1a, as RuralLife.YawOf): no Unity random stream or simulation state moves.</summary>
        public static float YawOf(string key) => RuralLife.StableHash(key) % 360u;

        void Rebuild()
        {
            if (map == null || map.Terrain == null) return;
            if (!Standalone.Any(m => prefabs.TryGetValue(m, out var p) && p != null)) { ClearVisuals(); return; }
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var ground = PlacementLayer.CampGround(map);
            var footprints = new DiscGrid(map.KitFootprints.Select(f => (f.centre, f.radius)));
            var taken = new DiscGrid(Others(map, date));
            long othersMs = clock.ElapsedMilliseconds;
            bool Free(Vector2 p, float r) => taken.Clear(p, r) && footprints.Clear(p, r)
                && ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)
                && !ground.Water(p.x, p.y) && !ground.OnRoute(p, r) && !ground.OnToken(p, r, date);
            var holes = new List<(Vector2 centre, float radius)>();
            var row = new Vector2[5];
            var spots = new List<(Want w, Vector2 spot, string name)>();
            // Every want takes its ground whether or not its model has streamed (or failed): a prop's spot never depends on
            // which others have arrived (#2266 review, as RuralLife.Plan). Only the streamed ones are drawn.
            foreach (var w in wants)
            {
                bool drawn = prefabs.TryGetValue(w.Model, out var prefab) && prefab != null;
                float clearance = ClearanceOf(w.Model);
                bool hedge = w.Model == PricklyPear;
                bool RowFree(Vector2 p, float r)
                {
                    int n = HedgeMembers(w, p, row);
                    for (int i = 0; i < n; i++) if (!Free(row[i], r)) return false;
                    return true;
                }
                var at = NearestSpot(w.Key, w.Anchor, clearance, hedge ? RowFree : (Func<Vector2, float, bool>)Free, w.Start, ReachOf(w.Model, ground.HexSizeM));
                if (at == null)
                {
                    if (drawn && w.Model != Garrigue && w.Model != Scrub) Debug.LogWarning($"HEXPROPS: no clear ground near {w.Key}; its {w.Model} is not drawn.");
                    continue;
                }
                int count = hedge ? HedgeMembers(w, at.Value, row) : 1;
                for (int i = 0; i < count; i++)
                {
                    var spot = hedge ? row[i] : at.Value;
                    taken.Add(spot, clearance);
                    if (!drawn) continue;
                    holes.Add((spot, clearance));
                    spots.Add((w, spot, hedge ? w.Key + "#" + i : w.Key));
                }
            }
            long searchMs = clock.ElapsedMilliseconds - othersMs;
            // A prop whose model and spot did not change keeps its object: a date change usually moves few of them.
            var next = new Dictionary<string, (GameObject go, string model, Vector2 spot)>(StringComparer.Ordinal);
            renderers.Clear(); Placed.Clear();
            int kept = 0;
            foreach (var (w, spot, name) in spots)
            {
                var pos = new Vector3(spot.x, ground.Y(spot.x, spot.y), spot.y);
                GameObject go;
                if (live.TryGetValue(name, out var old) && old.go != null && old.model == w.Model && (old.spot - spot).sqrMagnitude < 1e-6f)
                { go = old.go; live.Remove(name); kept++; }
                else
                {
                    if (live.TryGetValue(name, out var stale)) { DestroyNow(stale.go); live.Remove(name); }
                    var prefab = prefabs[w.Model]!;
                    float yaw = w.Model == Station ? AlongNearestRoute(w.Anchor, map.KitRouteCorridors) ?? 0f : YawOf(name);
                    go = Instantiate(prefab, transform, false); go.name = name;
                    go.transform.localRotation = prefab.transform.localRotation;
                    var b = Bounds(go); float edge = Mathf.Max(b.size.x, b.size.z);
                    go.transform.localScale = prefab.transform.localScale * (edge > 1e-4f ? SizeOf(w.Model) / edge : 1f);
                    go.transform.localRotation = SpecialBuildingModels.Rotation(prefab.transform.localRotation, yaw);
                    go.transform.position = pos; b = Bounds(go);
                    go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
                }
                next[name] = (go, w.Model, spot);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.forceRenderingOff = !(appliedClose ?? LastClose); renderers.Add(r); }
                Placed.Add((w.Key, w.Model, pos, name));
            }
            foreach (var gone in live.Values) DestroyNow(gone.go);
            live = next;
            long instancesMs = clock.ElapsedMilliseconds - othersMs - searchMs;
            // No kit tree stands on a prop (Kits.VegetationLayer.Hide, as under the special buildings); unchanged holes need no pass.
            if (lastHoles == null || !holes.SequenceEqual(lastHoles)) { Units.UnitShowcase.HideTrees(map, Name, holes); lastHoles = holes; }
            long total = clock.ElapsedMilliseconds;
            LastBuildMs = (othersMs, searchMs, instancesMs, total);
            Debug.Log($"HEXPROPS: {Placed.Count} hex props placed for {wants.Count} wants in {total} ms (keep-clear ground {othersMs} ms, " +
                $"search {searchMs} ms, instances {instancesMs} ms with {kept} kept, trees {total - othersMs - searchMs - instancesMs} ms) " +
                $"({string.Join(", ", Standalone.Select(m => $"{m.Substring(m.LastIndexOf('/') + 1)} {Placed.Count(p => p.Model == m)}"))})");
        }

        void DestroyNow(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) { if (gameObject.activeInHierarchy) go.transform.SetParent(null); Destroy(go); } else DestroyImmediate(go);
        }

        /// <summary>Tal Heuberger's minarets, belfries and clock tower in place of the kit's parts (#2266). A kit renderer's parts
        /// are replaced together, and only when every kind among them streamed; then its fallback parts (far and close) are
        /// hidden. It runs on each landmark answer and builds the kit meshes that answer completes, so an answer that never
        /// comes holds back only the kit meshes that need it.
        /// Each model stands on its part's own base (its box's bottom, or the ground where the part reaches below it), as tall
        /// as the part, turned as the kit turns it, and draws exactly when the part's kit or close renderer does
        /// (TownModelLayer.Follow). The kit renderers then stop drawing their own parts (KitLayer.ShowParts).</summary>
        void RebuildLandmarks()
        {
            if (map == null || map.Terrain == null || map.KitStats == null) return;
            var kits = map.transform.Find(Kits.KitLayer.RootName);
            var towns = kits != null ? kits.GetComponentInChildren<Kits.TownModelLayer>(true) : null;
            if (kits == null || towns == null || map.KitStats.LandmarkParts.Count == 0) return;
            var holder = transform.Find(LandmarksName);
            if (holder == null) { holder = new GameObject(LandmarksName).transform; holder.SetParent(transform, false); }
            var ground = PlacementLayer.CampGround(map);
            // Only the kit meshes this answer completes: those already replaced stay as they are.
            foreach (var group in map.KitStats.LandmarkParts.GroupBy(p => p.Kit))
            {
                if (Hidden.Contains(group.Key)) continue;
                if (!group.All(p => ByPart.TryGetValue(p.Kind, out var id) && prefabs.TryGetValue(id, out var pf) && pf != null)) continue;
                var kit = kits.Find(group.Key)?.GetComponent<Renderer>();
                if (kit == null) continue;
                int n = 0;
                foreach (var part in group)
                {
                    var prefab = prefabs[ByPart[part.Kind]]!;
                    Stand(part, prefab, part.Far, kit, false, n);
                    var close = part.Close != null ? kits.Find(part.Close)?.GetComponent<Renderer>() : null;
                    if (close != null) Stand(part, prefab, part.Near, close, true, n);
                    n++;
                }
                Hidden.Add(group.Key);
            }
            if (isActiveAndEnabled) foreach (var name in Hidden) ShowKitParts(kits, name, false);
            Boot.DiagLog.BootPhase("hex landmarks built");
            Debug.Log($"HEXPROPS: {Replaced.Count} landmark models stand in for the kit's parts of {Hidden.Count} kit meshes " +
                $"({string.Join(", ", ByPart.Keys.Select(k => $"{k} {Replaced.Count(r => r.Kind == k && !r.Close)}"))})");

            void Stand(Kits.KitLayer.LandmarkPart part, GameObject prefab, Bounds box, Renderer leader, bool close, int n)
            {
                var go = Instantiate(prefab, holder, false); go.name = $"{part.Owner}:{part.Kind}:{n}:{(close ? "close" : "far")}";
                go.transform.localRotation = SpecialBuildingModels.Rotation(prefab.transform.localRotation, part.Yaw);
                // A roof-mounted part (a village minaret, a belfry) starts on the roof; a part with foundations starts on the ground.
                float baseY = Mathf.Max(box.min.y, ground.Y(box.center.x, box.center.z)), height = box.max.y - baseY;
                var b = Bounds(go);
                go.transform.localScale = prefab.transform.localScale * (b.size.y > 1e-4f && height > 0f ? height / b.size.y : 1f);
                var pos = new Vector3(box.center.x, baseY, box.center.z);
                go.transform.position = pos; b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; towns!.Follow(leader, r); }
                Replaced.Add((part.Owner, part.Kind, close, go.name));
            }
        }

        /// <summary>Whether the kit draws its own parts where Tal Heuberger's models stand (<see cref="Hidden"/>).</summary>
        void ShowAllKitParts(bool on)
        {
            var kits = map != null ? map.transform.Find(Kits.KitLayer.RootName) : null;
            if (kits != null) foreach (var name in Hidden) ShowKitParts(kits, name, on);
        }

        static void ShowKitParts(Transform kits, string kit, bool on)
        {
            foreach (var name in new[] { kit, kit + Kits.KitLayer.CloseSuffix })
                if (kits.Find(name)?.GetComponent<Renderer>() is Renderer r) Kits.KitLayer.ShowParts(r, on);
        }

        void ClearVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (old.name != LandmarksName) DestroyNow(old);
            }
            live.Clear(); renderers.Clear(); Placed.Clear();
            if (map != null && lastHoles != null) { Units.UnitShowcase.HideTrees(map, Name, Array.Empty<(Vector2, float)>()); lastHoles = null; }
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        /// <summary>Never an empty spot: while the layer (and so Tal Heuberger's models under it) is disabled or gone, the kit
        /// draws its own parts again; enabled, the models stand in for them (#2266 review).</summary>
        void OnEnable() { RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering; ShowAllKitParts(false); }
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering; ShowAllKitParts(true); }
        void OnDestroy() => ShowAllKitParts(true);

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam) => ChooseCore(cam, inRender: true);
        /// <summary>A direct call (tests and editor tools, outside any render) builds what is due at once.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam) => ChooseCore(cam, inRender: false);

        /// <summary>#700's close-zoom switch with its hysteresis (<see cref="SpecialBuildingModels.CloseAt"/>) for the standalone
        /// props, and the request (Ask). The landmark models follow their kit renderers instead.</summary>
        void ChooseCore(Camera cam, bool inRender)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool close = SpecialBuildingModels.CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close; LastClose = close;
            // The kit's minarets, belfries and clock tower draw at every zoom (owner, 2 November 2026: "tal's high quality models
            // replace the built ones everytwhere"), so with any to replace the first camera to render (after the first frame)
            // asks; the standalone props are built only once a camera comes in close, where they draw.
            if (map != null && !requested && (close || map.KitStats?.LandmarkParts.Count > 0))
            {
                if (!inRender) Ask();
                else
                {
                    askQueued = true;
#if UNITY_EDITOR
                    if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
                }
            }
            if (close && !everClose) { everClose = true; standaloneDirty = true; }
            if (close) BuildStandaloneIfDue(inRender);
            // Only a change is written: thousands of props, every camera, every frame (#2266 review).
            if (appliedClose != close)
            {
                appliedClose = close;
                foreach (var r in renderers) if (r != null) r.forceRenderingOff = !close;
            }
        }
    }
}
