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
    ///   arab_village hex tile draws it at the village's edge.
    /// Each stands on the nearest clear ground to its place (<see cref="NearestSpot"/>: a station within one hex of its
    /// point, a tent or a hedge just outside its camp's or village's kit footprint), clear of every kit footprint, the
    /// routes, the unit tokens, water, the other Collection layers and each other, at the buildings' one world scale, and
    /// draws only up close (#700's switch). The props stream
    /// from their own bundle (<see cref="HexProps"/>, 5.8 MB for WebGL), which is asked for only when a camera first comes in
    /// to the close zoom, so the opening view fetches and draws nothing new. A prop that does not stream stays off the map.</summary>
    [ExecuteAlways]
    public sealed class HexPropModels : MonoBehaviour
    {
        public const string Name = "HexPropModels";
        public const string Station = "tiles:railway_halt/station", Tent = "tiles:desert_edge/tent", PricklyPear = "tiles:arab_village/prickly_pear";
        public static readonly string[] Models = { Station, Tent, PricklyPear };

        /// <summary>The one world scale of Tal Heuberger's models (<see cref="Kits.SettlementKits.ModelScale"/>, issue #1032).</summary>
        public const float WorldScale = Kits.SettlementKits.ModelScale;

        /// <summary>Design values, not sourced measurements: the real long side (m) each prop stands for, seen from above. A
        /// small stone station building; the camp kit's goat-hair tents are 7 to 12 m (SettlementKits.Encampments, as
        /// RuralLife's tent); a prickly-pear clump a few metres across.</summary>
        public static float RealLengthM(string model) => model switch
        {
            Station => 12f, Tent => 10f, PricklyPear => 4f,
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
            public readonly string Name; public readonly double Lon, Lat; public readonly DateTime From;
            public RailStation(string name, double lon, double lat, DateTime from) { Name = name; Lon = lon; Lat = lat; From = from; }
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
                if (date < s.From) continue;
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
            return result;
        }

        /// <summary>Test seam: how a prop is asked for (the hex props' own stream in a player, the AssetDatabase in the editor).</summary>
        internal static Action<string, Action<GameObject?>> Request = (id, done) =>
            CollectionStream.Request(HexProps.Catalog, id, CollectionDetail.Near, done);

        MapScene? map; DateTime date; bool shown, requested;
        List<Want> wants = new List<Want>();
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<Renderer> renderers = new List<Renderer>();
        public readonly List<(string Key, string Model, Vector3 At)> Placed = new List<(string, string, Vector3)>();
        public IReadOnlyList<Want> Wanted => wants;
        /// <summary>Whether the props were asked for: only once a camera came in to the close zoom.</summary>
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
            requested = true;
            foreach (var id in Models)
            {
                string model = id;
                Request(model, p =>
                {
                    if (this == null) return;
                    prefabs[model] = p;
                    if (p == null) Debug.LogWarning($"HEXPROPS: {model} did not stream; it stays off the map.");
                    Rebuild();
                });
            }
        }

        /// <summary>The ground the other Collection layers hold, so no prop stands on them: the civilian figures and the
        /// wildlife (as RuralLife.OtherProps), the rural scenes and the special buildings (GameRoot shows them all first).</summary>
        static List<(Vector2 centre, float radius)> Others(MapScene map)
        {
            var discs = RuralLife.OtherProps(map);
            var rural = map.Terrain.transform.Find(RuralLife.Name)?.GetComponent<RuralLife>();
            if (rural != null) foreach (var p in rural.Placed) discs.Add((new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model)));
            var special = map.Terrain.transform.Find(SpecialBuildingModels.Name)?.GetComponent<SpecialBuildingModels>();
            if (special != null) foreach (var p in special.Placed) discs.Add((new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model)));
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
            if (prefabs.Count == 0) return;
            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var taken = Others(map);
            bool Free(Vector2 p, float r) => TownLandmarks.Clear(p, r, taken) && TownLandmarks.Clear(p, r, footprints)
                && ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)
                && !ground.Water(p.x, p.y) && !ground.OnRoute(p, r) && !ground.OnToken(p, r, date);
            var holes = new List<(Vector2 centre, float radius)>();
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab) || prefab == null) continue;
                float clearance = ClearanceOf(w.Model);
                var at = NearestSpot(w.Key, w.Anchor, clearance, Free, w.Start, ReachOf(w.Model, ground.HexSizeM));
                if (at == null) { Debug.LogWarning($"HEXPROPS: no clear ground near {w.Key}; its {w.Model} is not drawn."); continue; }
                taken.Add((at.Value, clearance)); holes.Add((at.Value, clearance));
                float yaw = w.Model == Station ? AlongNearestRoute(w.Anchor, map.KitRouteCorridors) ?? 0f : YawOf(w.Key);
                var go = Instantiate(prefab, transform, false); go.name = w.Key;
                go.transform.localRotation = prefab.transform.localRotation;
                var b = Bounds(go); float edge = Mathf.Max(b.size.x, b.size.z);
                go.transform.localScale = prefab.transform.localScale * (edge > 1e-4f ? SizeOf(w.Model) / edge : 1f);
                go.transform.localRotation = SpecialBuildingModels.Rotation(prefab.transform.localRotation, yaw);
                var pos = new Vector3(at.Value.x, ground.Y(at.Value.x, at.Value.y), at.Value.y);
                go.transform.position = pos; b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.forceRenderingOff = !LastClose; renderers.Add(r); }
                Placed.Add((w.Key, w.Model, pos));
            }
            // No kit tree stands on a prop (Kits.VegetationLayer.Hide, as under the special buildings).
            Units.UnitShowcase.HideTrees(map, Name, holes);
            Debug.Log($"HEXPROPS: {Placed.Count} of {wants.Count} hex props placed ({string.Join(", ", Models.Select(m => $"{m.Substring(m.LastIndexOf('/') + 1)} {Placed.Count(p => p.Model == m)}"))})");
        }

        void ClearVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
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
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        /// <summary>#700's close-zoom switch with its hysteresis (<see cref="SpecialBuildingModels.CloseAt"/>); the first close
        /// camera asks for the props.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool close = SpecialBuildingModels.CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close; LastClose = close;
            if (close && map != null) Ask();
            foreach (var r in renderers) if (r != null) r.forceRenderingOff = !close;
        }
    }
}
