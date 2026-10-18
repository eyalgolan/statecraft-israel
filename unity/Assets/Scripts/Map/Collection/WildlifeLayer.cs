#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>MATKOT's two wild-animal models from The 1948 Collection (issue #670), placed sparsely and deterministically
    /// by terrain zone, each zone from its source:
    /// - Nubian ibex on steep desert or mountain hexes of the Judean Desert, west of the Dead Sea: the Israel Nature and Parks
    ///   Authority's En Avdat brochure, "Ibexes are common in the Negev, the Eilat Mountains and the Judean Desert near the
    ///   Dead Sea" (https://static-en.parks.org.il/wp-content/uploads/2018/06/en-avdat-eng.pdf, checked 18 October 2026).
    /// - Striped hyena on hill hexes of Mount Carmel and of the central hill country (the Shephelah, Judea and Samaria), west
    ///   of the Jordan. Hadad, Kosicki and Yosef, "Population trends of striped hyena (Hyaena hyaena) in Israel for the past
    ///   five decades", Scientific Reports 13 (2023), https://doi.org/10.1038/s41598-023-31137-2 (checked 18 October 2026):
    ///   "we also found the highest density of hyenas in the Carmel Mountains", fieldwork that "supports this conclusion in
    ///   the Judea region in central Israel", and (after Ilani) hyenas "non-existent or extremely rare" along the coast, in
    ///   the Jezreel Valley and the lower Galilee, which are left out. Bar-Ziv et al., Frontiers in Ecology and Evolution 10
    ///   (2022) 897132, https://doi.org/10.3389/fevo.2022.897132 (checked 18 October 2026), tracked hyenas in central Israel
    ///   "between the Shfela lowlands", "the Judaean mountains" and "Judea and Samaria to the east".
    /// These are modern surveys of where the species lives; the game uses them for the range only, not for numbers in 1948
    /// (hyenas were then fewer: the same Scientific Reports paper reports "extreme population fluctuations owing to poisoning
    /// campaigns during the British Mandate (1918-1948)"), and draws only a handful of each.
    /// Each model stands on fairly level ground (<see cref="MaxReliefM"/>), so no leg floats or sinks.
    /// Both near meshes stream through <see cref="CollectionStream"/> (nothing is fetched before the frame after the
    /// request) and draw only at close zoom. Their shared materials have GPU instancing enabled. A missing model fails open:
    /// that species is omitted while the map and game continue.</summary>
    public sealed class WildlifeLayer : MonoBehaviour
    {
        public const string Name = "Wildlife";
        public const string Ibex = "figure-nubian-ibex-1948-v1", Hyena = "figure-striped-hyena-1948-v1";

        // Design values (#670): sparse enough that the whole map contains only a handful.
        public const int IbexCount = 3, HyenaCount = 3;
        /// <summary>An animal's largest ground edge (m): sized against the man, as the units' symbolic scale sizes everything
        /// (UnitShowcase.FigureScale: a man about 2.5 km tall), at <see cref="LengthOverMan"/> of his height, about 1.9 km.
        /// A real-scale animal would be a speck beside the kits' houses and groves.</summary>
        public const float LengthOverMan = 0.75f;   // design value: a quadruped's length against a standing man
        public const float SizeM = LengthOverMan * UnitBadge.FigureHeightM * UnitShowcase.FigureScale;
        // Design value: no two animals closer than two lengths, so they never overlap or read as a herd.
        public const float MinSpacingM = 2f * SizeM;
        /// <summary>The close zoom (view distance, m) they draw at, with hysteresis: in from 40 km, out beyond 45 km, the band
        /// of the close-zoom building models (<see cref="TownModelLayer"/>), so the animals come in with the rest of the close
        /// detail; the gameplay view (60 km, gameplay03) and the overview never draw them.</summary>
        public const float CloseEnterM = TownModelLayer.CloseEnterM, CloseExitM = TownModelLayer.CloseExitM;
        // Design value: a hex has cliff-like relief when an axial neighbour's centre differs by at least this much.
        public const float CliffReliefM = 150f, MinimumCliffElevationM = 100f;
        // Design-value boxes (lon, lat) around the sourced ranges above: the Judean Desert west of the Dead Sea, for ibex;
        // Mount Carmel and the central hill country west of the Jordan, for hyenas.
        public const double JudeanWest = 35.20, JudeanEast = 35.45, JudeanSouth = 31.10, JudeanNorth = 31.80;
        public const double CarmelWest = 34.95, CarmelEast = 35.15, CarmelSouth = 32.55, CarmelNorth = 32.80;
        public const double CentralWest = 34.85, CentralEast = 35.45, CentralSouth = 31.35, CentralNorth = 32.45;
        // Design value: the ground under a model (its centre and eight points on its half edge) varies by at most a tenth
        // of its length (world m, after MapBuilder.VerticalExaggeration); standing at the middle of that span (BaseY), no
        // leg floats or sinks by more than half of it.
        public const float MaxReliefM = 0.1f * SizeM;
        const uint Seed = 0x6701948u; // design value: fixed placement seed
        const uint SpotsPerHex = 16;  // design value: seeded spots tried in a candidate hex before the next hex

        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = CollectionStream.Request;

        public readonly struct Want : IEquatable<Want>
        {
            public readonly string Key, Model; public readonly Vector2 At; public readonly float Yaw;
            public Want(string key, string model, Vector2 at, float yaw) { Key = key; Model = model; At = at; Yaw = yaw; }
            public bool Equals(Want o) => Key == o.Key && Model == o.Model && At == o.At && Yaw == o.Yaw;
            public override bool Equals(object? o) => o is Want w && Equals(w);
            public override int GetHashCode() => (Key, Model, At, Yaw).GetHashCode();
        }

        static uint Mix(uint x)
        {
            x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; return x ^ (x >> 16);
        }

        static uint Hash(HexJson h, uint salt) => Mix(Seed ^ salt ^ (uint)h.q * 0x9e3779b9u ^ (uint)h.r * 0x85ebca6bu);

        internal static bool IbexHex(HexJson h, IReadOnlyDictionary<(int, int), HexJson> byHex)
        {
            if (!h.land || h.elev_m < MinimumCliffElevationM || (h.biome != "desert" && h.biome != "mountains") ||
                !In(h, JudeanWest, JudeanEast, JudeanSouth, JudeanNorth))
                return false;
            foreach (var (dq, dr) in Neighbours)
                if (byHex.TryGetValue((h.q + dq, h.r + dr), out var n) && Mathf.Abs(h.elev_m - n.elev_m) >= CliffReliefM) return true;
            return false;
        }

        static bool In(HexJson h, double west, double east, double south, double north) =>
            h.lon >= west && h.lon <= east && h.lat >= south && h.lat <= north;

        internal static bool HyenaHex(HexJson h) => h.land && h.biome == "hills" &&
            (In(h, CarmelWest, CarmelEast, CarmelSouth, CarmelNorth) || In(h, CentralWest, CentralEast, CentralSouth, CentralNorth));

        /// <summary>Whether a model at <paramref name="at"/> has its ground: clear of water, routes, unit tokens and every kit
        /// footprint, and level within <see cref="MaxReliefM"/> under its footprint.</summary>
        static bool Clear(KitGround ground, MapScene map, Vector2 at)
        {
            float r = 0.5f * SizeM;
            if (ground.Water(at.x, at.y) || ground.OnRoute(at, r) || ground.OnToken(at, r) ||
                map.KitFootprints.Any(f => (f.centre - at).sqrMagnitude < (f.radius + r) * (f.radius + r))) return false;
            return Relief(ground, at) <= MaxReliefM;
        }

        /// <summary>The lowest and highest ground (world y) under a model at <paramref name="at"/>: its centre and eight points
        /// on its half edge.</summary>
        public static (float Lo, float Hi) GroundSpan(KitGround ground, Vector2 at)
        {
            float lo = ground.Y(at.x, at.y), hi = lo;
            for (int k = 0; k < 8; k++)
            {
                var p = at + new Vector2(Mathf.Cos(k * Mathf.PI / 4f), Mathf.Sin(k * Mathf.PI / 4f)) * (0.5f * SizeM);
                float y = ground.Y(p.x, p.y); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            return (lo, hi);
        }

        /// <summary>The spread of ground height (world m) under a model at <paramref name="at"/>.</summary>
        public static float Relief(KitGround ground, Vector2 at) { var (lo, hi) = GroundSpan(ground, at); return hi - lo; }

        /// <summary>The height (world y) a model's base stands at: midway between the lowest and highest ground under it, so
        /// on a slope no leg floats or sinks by more than half of <see cref="MaxReliefM"/>.</summary>
        public static float BaseY(KitGround ground, Vector2 at) { var (lo, hi) = GroundSpan(ground, at); return 0.5f * (lo + hi); }

        static readonly (int q, int r)[] Neighbours = { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };

        /// <summary>The fixed-seed spots wanted on this map. Candidate order is a stable integer hash, never runtime RNG.</summary>
        public static List<Want> Wants(MapScene map)
        {
            if (map == null || map.Hexes == null) return new List<Want>();
            var byHex = map.Hexes.hexes.ToDictionary(h => (h.q, h.r));
            var ground = PlacementLayer.CampGround(map);
            var result = new List<Want>();
            Add(Ibex, IbexCount, 0x1be7u, map.Hexes.hexes.Where(h => IbexHex(h, byHex)));
            Add(Hyena, HyenaCount, 0x2ae9u, map.Hexes.hexes.Where(HyenaHex));
            return result;

            void Add(string model, int count, uint salt, IEnumerable<HexJson> candidates)
            {
                int index = 0;
                foreach (var h in candidates.OrderBy(h => Hash(h, salt)).ThenBy(h => h.q).ThenBy(h => h.r))
                {
                    // Up to SpotsPerHex seeded spots in the hex, the first with level, clear ground.
                    for (uint t = 0; t < SpotsPerHex; t++)
                    {
                        uint hash = Hash(h, salt ^ 0xa511e9b3u ^ (t * 0x632be5abu));
                        float angle = (hash & 0xffffu) * (2f * Mathf.PI / 65536f);
                        float radius = map.Hexes.hex_size_km * 1000f * (0.05f + 0.35f * ((hash >> 16) / 65535f)); // design values: inside the chosen hex
                        var centre = map.Frame.ToWorld(h.lon, h.lat, 0f);
                        var at = new Vector2(centre.x, centre.z) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                        if (!Clear(ground, map, at) || result.Any(w => (w.At - at).sqrMagnitude < MinSpacingM * MinSpacingM)) continue;
                        float yaw = Mix(hash ^ 0x51ed270bu) % 360u; // design value: seeded facing
                        result.Add(new Want(model + "-" + index, model, at, yaw));
                        index++;
                        break;
                    }
                    if (index == count) break;
                }
            }
        }

        MapScene? map;
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        List<Want> wants = new List<Want>();
        public readonly List<(string Key, string Model, Vector3 At, float Yaw)> Placed = new List<(string, string, Vector3, float)>();
        public readonly List<string> Missing = new List<string>();
        public bool LastClose { get; private set; }

        public static WildlifeLayer? Show(MapScene map)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<WildlifeLayer>();
            if (layer != null) return layer;
            var go = new GameObject(Name);
            go.transform.SetParent(map.Terrain.transform, false);
            layer = go.AddComponent<WildlifeLayer>();
            layer.map = map;
            layer.wants = Wants(map);
            foreach (var model in layer.wants.Select(w => w.Model).Distinct())
            {
                var requested = model;
                Request(requested, CollectionDetail.Near, p => layer.OnModel(requested, p));
            }
            return layer;
        }

        void OnModel(string model, GameObject? prefab)
        {
            if (this == null) return;
            prefabs[model] = prefab;
            if (prefab == null) Debug.LogWarning($"WILDLIFE: {model} did not stream; that species is not drawn, and the map plays on without it.");
            Rebuild();
        }

        void Rebuild()
        {
            if (map == null || map.Terrain == null) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            renderers.Clear(); Placed.Clear(); Missing.Clear();
            var ground = PlacementLayer.CampGround(map);
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab)) continue;
                if (prefab == null) { Missing.Add(w.Key); continue; }
                if (!Clear(ground, map, w.At)) { Missing.Add(w.Key); continue; }
                var go = Instantiate(prefab, transform, false);
                go.name = w.Key;
                go.transform.localRotation = Quaternion.Euler(0f, w.Yaw, 0f) * prefab.transform.localRotation;
                var bounds = Bounds(go);
                float edge = Mathf.Max(bounds.size.x, bounds.size.z);
                go.transform.localScale *= edge > 1e-3f ? SizeM / edge : 1f;
                var pos = new Vector3(w.At.x, BaseY(ground, w.At), w.At.y);
                go.transform.position = pos;
                var placed = Bounds(go);
                go.transform.position += new Vector3(pos.x - placed.center.x, pos.y - placed.min.y, pos.z - placed.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.forceRenderingOff = !LastClose;
                    renderers.Add(r);
                }
                Placed.Add((w.Key, w.Model, pos, w.Yaw));
            }
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        public static bool CloseAt(float distanceM, bool? was) => was == true ? distanceM <= CloseExitM : distanceM <= CloseEnterM;

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool on = CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = on; LastClose = on;
            foreach (var r in renderers) if (r != null) r.forceRenderingOff = !on;
        }
    }
}
