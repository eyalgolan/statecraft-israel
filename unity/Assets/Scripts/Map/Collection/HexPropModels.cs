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
            var radial = spot - w.Anchor; var along = radial.sqrMagnitude > 1e-6f ? new Vector2(-radial.y, radial.x).normalized : Vector2.right;
            int n = HedgeClumps(w.Key); var members = new Vector2[n];
            for (int i = 0; i < n; i++) members[i] = spot + along * ((i - 0.5f * (n - 1)) * HedgeSpacingSizes * SizeOf(PricklyPear));
            return members;
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
            var result = new List<Want>();
            foreach (var s in Stations)
            {
                if (!s.DrawnOn(date)) continue;
                var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
                result.Add(new Want("station:" + s.Name, Station, new Vector2(w.x, w.z), 0f));
            }
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

        MapScene? map; DateTime date; bool shown, requested; int pending;
        List<Want> wants = new List<Want>();
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<Renderer> renderers = new List<Renderer>();
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

        public static HexPropModels? ShowAt(MapScene map, DateTime date)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<HexPropModels>();
            if (layer == null)
            {
                var go = new GameObject(Name); go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<HexPropModels>(); layer.map = map;
            }
            // GameRoot calls this on every refresh: rebuild only when the date changes (the dated stations and tokens follow it).
            if (layer.shown && layer.date == date) return layer;
            layer.shown = true; layer.date = date; layer.wants = Wants(map, date); layer.Rebuild();
            return layer;
        }

        void Ask()
        {
            if (requested) return;
            requested = true; pending = Models.Length;
            foreach (var id in Models)
            {
                string model = id;
                Request(model, p =>
                {
                    if (this == null) return;
                    prefabs[model] = p;
                    if (p == null) Debug.LogWarning($"HEXPROPS: {model} did not stream; it stays off the map.");
                    // One build once every prop has answered (the bundle carries them all), not one per answer.
                    if (--pending == 0) { RebuildLandmarks(); Rebuild(); }
                });
            }
        }

        /// <summary>The ground the other Collection layers hold or will hold, so no prop stands on them: the civilian figures
        /// and the wildlife (as RuralLife.OtherProps), and the rural scenes' and special buildings' PLANNED spots (their Plan with
        /// every model available), never what has streamed so far: the props stand in the same places whatever the camera
        /// has visited, and a scene or building that streams in later never lands on one (#2266 review).</summary>
        internal static List<(Vector2 centre, float radius)> Others(MapScene map, DateTime date)
        {
            var discs = RuralLife.OtherProps(map);
            foreach (var (want, spot) in RuralLife.Plan(map, RuralLife.Wants(map), RuralLife.OtherProps(map), _ => true))
                discs.Add((spot, RuralLife.SceneRadiusM(want.Model)));
            foreach (var (w, at) in SpecialBuildingModels.Plan(map, date, SpecialBuildingModels.Placements(date), _ => true, log: false))
                discs.Add((at, SpecialBuildingModels.ClearanceOf(w.Model)));
            return discs;
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
            ClearVisuals();
            if (prefabs.Count == 0 || pending > 0) return;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var taken = Others(map, date);
            bool Free(Vector2 p, float r) => TownLandmarks.Clear(p, r, taken) && TownLandmarks.Clear(p, r, footprints)
                && ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)
                && !ground.Water(p.x, p.y) && !ground.OnRoute(p, r) && !ground.OnToken(p, r, date);
            var holes = new List<(Vector2 centre, float radius)>();
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab) || prefab == null) continue;
                float clearance = ClearanceOf(w.Model);
                bool hedge = w.Model == PricklyPear;
                Func<Vector2, float, bool> free = hedge ? (p, r) => HedgeMembers(w, p).All(m => Free(m, r)) : (Func<Vector2, float, bool>)Free;
                var at = NearestSpot(w.Key, w.Anchor, clearance, free, w.Start, ReachOf(w.Model, ground.HexSizeM));
                if (at == null)
                {
                    if (w.Model != Garrigue && w.Model != Scrub) Debug.LogWarning($"HEXPROPS: no clear ground near {w.Key}; its {w.Model} is not drawn.");
                    continue;
                }
                var spots = hedge ? HedgeMembers(w, at.Value) : new[] { at.Value };
                for (int i = 0; i < spots.Length; i++) Place(w, spots[i], hedge ? w.Key + "#" + i : w.Key, prefab);
            }
            void Place(Want w, Vector2 spot, string name, GameObject prefab)
            {
                float clearance = ClearanceOf(w.Model);
                taken.Add((spot, clearance)); holes.Add((spot, clearance));
                var at = (Vector2?)spot;
                float yaw = w.Model == Station ? AlongNearestRoute(w.Anchor, map.KitRouteCorridors) ?? 0f : YawOf(name);
                var go = Instantiate(prefab, transform, false); go.name = name;
                go.transform.localRotation = prefab.transform.localRotation;
                var b = Bounds(go); float edge = Mathf.Max(b.size.x, b.size.z);
                go.transform.localScale = prefab.transform.localScale * (edge > 1e-4f ? SizeOf(w.Model) / edge : 1f);
                go.transform.localRotation = SpecialBuildingModels.Rotation(prefab.transform.localRotation, yaw);
                var pos = new Vector3(at.Value.x, ground.Y(at.Value.x, at.Value.y), at.Value.y);
                go.transform.position = pos; b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.forceRenderingOff = !LastClose; renderers.Add(r); }
                Placed.Add((w.Key, w.Model, pos, name));
            }
            // No kit tree stands on a prop (Kits.VegetationLayer.Hide, as under the special buildings).
            Units.UnitShowcase.HideTrees(map, Name, holes);
            Debug.Log($"HEXPROPS: {Placed.Count} hex props placed for {wants.Count} wants in {clock.ElapsedMilliseconds} ms " +
                $"({string.Join(", ", Standalone.Select(m => $"{m.Substring(m.LastIndexOf('/') + 1)} {Placed.Count(p => p.Model == m)}"))})");
        }

        /// <summary>Tal Heuberger's minarets, belfries and clock tower in place of the kit's parts (#2266). A kit renderer's parts
        /// are replaced together, and only when every kind among them streamed; then its fallback parts (far and close) are
        /// hidden. Each model stands where its part stands, as tall as the part, turned as the kit turns it, and draws exactly
        /// when the part's kit or close renderer does (TownModelLayer.Follow). Built once: the kit does not change with the date.
        /// The kit renderers then stop drawing their own parts (KitLayer.ShowParts).</summary>
        void RebuildLandmarks()
        {
            if (map == null || map.Terrain == null || map.KitStats == null) return;
            var kits = map.transform.Find(Kits.KitLayer.RootName);
            var towns = kits != null ? kits.GetComponentInChildren<Kits.TownModelLayer>(true) : null;
            ClearLandmarks();
            if (kits == null || towns == null || map.KitStats.LandmarkParts.Count == 0) return;
            var holder = new GameObject(LandmarksName).transform; holder.SetParent(transform, false);
            var ground = PlacementLayer.CampGround(map);
            foreach (var group in map.KitStats.LandmarkParts.GroupBy(p => p.Kit))
            {
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
                ShowKitParts(kits, group.Key, false);
                Hidden.Add(group.Key);
            }
            Debug.Log($"HEXPROPS: {Replaced.Count} landmark models stand in for the kit's parts of {Hidden.Count} kit meshes " +
                $"({string.Join(", ", ByPart.Keys.Select(k => $"{k} {Replaced.Count(r => r.Kind == k && !r.Close)}"))})");

            void Stand(Kits.KitLayer.LandmarkPart part, GameObject prefab, Bounds box, Renderer leader, bool close, int n)
            {
                var go = Instantiate(prefab, holder, false); go.name = $"{part.Owner}:{part.Kind}:{n}:{(close ? "close" : "far")}";
                go.transform.localRotation = SpecialBuildingModels.Rotation(prefab.transform.localRotation, part.Yaw);
                float baseY = ground.Y(box.center.x, box.center.z), height = box.max.y - baseY;
                var b = Bounds(go);
                go.transform.localScale = prefab.transform.localScale * (b.size.y > 1e-4f && height > 0f ? height / b.size.y : 1f);
                var pos = new Vector3(box.center.x, baseY, box.center.z);
                go.transform.position = pos; b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; towns!.Follow(leader, r); }
                Replaced.Add((part.Owner, part.Kind, close, go.name));
            }
        }

        void ClearLandmarks()
        {
            var old = transform.Find(LandmarksName);
            if (old != null) { if (Application.isPlaying) { old.SetParent(null); Destroy(old.gameObject); } else DestroyImmediate(old.gameObject); }
            var kits = map != null ? map.transform.Find(Kits.KitLayer.RootName) : null;
            if (kits != null) foreach (var name in Hidden) ShowKitParts(kits, name, true);
            Hidden.Clear(); Replaced.Clear();
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
                if (old.name == LandmarksName) continue;
                if (Application.isPlaying) { if (gameObject.activeInHierarchy) old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            renderers.Clear(); Placed.Clear();
            if (map != null) Units.UnitShowcase.HideTrees(map, Name, Array.Empty<(Vector2, float)>());
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        /// <summary>A removed layer gives the kit back its own parts, so no spot is ever left empty.</summary>
        void OnDestroy()
        {
            var kits = map != null ? map.transform.Find(Kits.KitLayer.RootName) : null;
            if (kits == null) return;
            foreach (var name in Hidden) ShowKitParts(kits, name, true);
        }
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        /// <summary>#700's close-zoom switch with its hysteresis (<see cref="SpecialBuildingModels.CloseAt"/>) for the standalone
        /// props, and the request (Ask). The landmark models follow their kit renderers instead.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool close = SpecialBuildingModels.CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close; LastClose = close;
            // The kit's minarets, belfries and clock tower draw at every zoom, so with any to replace the first camera to render
            // (after the first frame) asks; otherwise only the close zoom shows a prop.
            if (map != null && (close || map.KitStats?.LandmarkParts.Count > 0)) Ask();
            foreach (var r in renderers) if (r != null) r.forceRenderingOff = !close;
        }
    }
}
