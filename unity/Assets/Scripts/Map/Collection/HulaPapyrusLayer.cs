#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using Statecraft.HexTiles;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tal Heuberger's papyrus (#2082) at the Hula swamp on the main map, at close zoom only. Owner, 2 November 2026:
    /// "Hula + Arava, map and boards (Recommended)"; the option read: "Papyrus at the Hula swamp, tamarisk at Arava oases, on
    /// the main map at close zoom and on the hex boards; Tal's scale, sourced places, load measured." The owner then ruled on
    /// #2278, 2 November 2026: "3x bigger, like the props (Recommended)"; the option read: "Same treatment you chose for the
    /// sandbags and well: about 3x so it reads at close zoom." So this layer draws 3x Tal's scale; the boards keep it.
    /// The place is the hex of content/sites.yaml's site <see cref="SiteId"/> ((16, 3)), sourced there to the 1946 Survey of
    /// Palestine 1:250,000 map (the marsh north of Lake Hula), FRUS 1951 v.V doc 288 and the JTA, with the 4.4 km re-grid note
    /// (#1183). The layer reads the hex from the content, so it moves with the site. Papyrus grew thickly in that marsh
    /// (HulaSwamp.cs's sources).
    /// Scale: Tal's model as delivered (1 unit tall in its OBJ, imported at file scale 1, so 1 m), times the one world scale
    /// of every model, <see cref="SettlementKits.ModelScale"/> (the owner on #1033: "Train scale for all ambient life", as
    /// <see cref="WildlifeLayer"/>), times <see cref="DrawScale"/> so it reads at close zoom (the owner on #2278, 2 November
    /// 2026: "3x bigger, like the props (Recommended)"). This layer only: the hex boards keep Tal's scale. No sourced papyrus
    /// height is used.
    /// The drainage stands as the game has it (R-SWAMP-001): once the site is drained (built), no papyrus is drawn.
    /// The clumps keep off water, routes, unit tokens and every kit footprint, in <see cref="Stands"/> stands of
    /// <see cref="ClumpsPerStand"/> clumps at most, at seeded spots (a stable integer hash, never runtime RNG). The model
    /// streams as its own bundle in the hex props' stream (<see cref="HexProps.PapyrusBundle"/>), asked for only on the first
    /// close view, so a player who never zooms in fetches nothing. A missing model fails open: nothing is drawn.
    /// There is no tamarisk on the main map: the repo has no sourced Arava spring or oasis place ([GAP], #1951).</summary>
    public sealed class HulaPapyrusLayer : MonoBehaviour
    {
        public const string Name = "HulaPapyrus";
        public const string SiteId = "hula";
        public const string Model = "tiles:hula_swamp/papyrus";

        // Design values: a few stands of a handful of clumps, enough to read as a reed swamp at close zoom.
        public const int Stands = 4, ClumpsPerStand = 10;
        /// <summary>The model's own height (m) as delivered and imported (its OBJ spans y -0.5 to 0.5).</summary>
        public const float ModelHeightM = 1f;
        /// <summary>The main map's enlargement of Tal's model. Owner, 2 November 2026, on #2278: "3x bigger, like the props
        /// (Recommended)" ("Same treatment you chose for the sandbags and well: about 3x so it reads at close zoom").</summary>
        public const float DrawScale = 3f;
        /// <summary>A clump's ground edge (m) for spacing and clearance: its drawn height (the model is about 0.87 as wide as
        /// it is tall, so this holds the whole clump).</summary>
        public const float ClumpM = ModelHeightM * SettlementKits.ModelScale * DrawScale;
        // Design values: clumps of a stand sit within StandRadius clump edges of its centre, at least MinClumpSpacing apart;
        // stands at least MinStandSpacing apart.
        public const float StandRadiusM = 3f * ClumpM, MinClumpSpacingM = 0.75f * ClumpM, MinStandSpacingM = 4f * StandRadiusM;
        /// <summary>The close zoom (view distance, m), the band of the close-zoom building models, with its switch
        /// (<see cref="CloseAt"/>) as <see cref="TownModelLayer.CloseAt"/>'s: in below CloseEnterM, out above CloseExitM.</summary>
        public const float CloseEnterM = TownModelLayer.CloseEnterM, CloseExitM = TownModelLayer.CloseExitM;
        public const float MaxReliefM = 0.1f * ClumpM;   // design value, as WildlifeLayer's: no clump floats or sinks
        const uint Seed = 0x20821948u;   // design value: fixed placement seed
        const uint StandTries = 64, ClumpTries = 40;   // design values: seeded spots tried per stand and per clump

        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = RequestProp;

        /// <summary>The production source: the hex props' stream (<see cref="HexProps.Catalog"/>).</summary>
        static void RequestProp(string id, CollectionDetail detail, Action<GameObject?> done) =>
            CollectionStream.Request(HexProps.Catalog, id, detail, done);

        static uint Mix(uint x)
        {
            x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; return x ^ (x >> 16);
        }

        /// <summary>The site's hex (q, r) in <paramref name="content"/>, or null when there is no such site.</summary>
        public static (int q, int r)? SiteHex(Statecraft.Sim.Content content)
        {
            var site = content.Sites.FirstOrDefault(s => s.Id == SiteId);
            return site == null ? ((int, int)?)null : (site.Q, site.R);
        }

        /// <summary>Whether a clump at <paramref name="at"/> has its ground: inside the site's hex, clear of water, routes,
        /// unit tokens and every kit footprint, and level within <see cref="MaxReliefM"/>.</summary>
        internal static bool Clear(KitGround ground, MapScene map, (int q, int r) hex, Vector2 at, float radius)
        {
            if (!ground.Inside(at.x, at.y) || ground.Hex(at.x, at.y) != hex) return false;
            if (ground.Water(at.x, at.y) || ground.OnRoute(at, radius) || ground.OnToken(at, radius) ||
                map.KitFootprints.Any(f => (f.centre - at).sqrMagnitude < (f.radius + radius) * (f.radius + radius))) return false;
            return Relief(ground, at) <= MaxReliefM;
        }

        /// <summary>The lowest and highest ground (world y) under a clump: its centre and eight points on its half edge.</summary>
        public static (float Lo, float Hi) GroundSpan(KitGround ground, Vector2 at)
        {
            float lo = ground.Y(at.x, at.y), hi = lo;
            for (int k = 0; k < 8; k++)
            {
                var p = at + new Vector2(Mathf.Cos(k * Mathf.PI / 4f), Mathf.Sin(k * Mathf.PI / 4f)) * (0.5f * ClumpM);
                float y = ground.Y(p.x, p.y); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            return (lo, hi);
        }

        public static float Relief(KitGround ground, Vector2 at) { var (lo, hi) = GroundSpan(ground, at); return hi - lo; }

        /// <summary>The fixed-seed clumps wanted on this map, in the site's hex: (key, ground spot, yaw).</summary>
        public static List<(string Key, Vector2 At, float Yaw)> Wants(MapScene map, Statecraft.Sim.Content content)
        {
            var result = new List<(string Key, Vector2 At, float Yaw)>();
            if (map == null || map.Hexes == null || content == null) return result;
            var site = SiteHex(content);
            if (site == null) return result;
            var hex = site.Value;
            var h = map.Hexes.hexes.FirstOrDefault(x => x.q == hex.q && x.r == hex.r);
            if (h == null) return result;
            var ground = PlacementLayer.CampGround(map);
            var centre3 = map.Frame.ToWorld(h.lon, h.lat, 0f);
            var centre = new Vector2(centre3.x, centre3.z);
            float hexM = map.Hexes.hex_size_km * 1000f;
            var stands = new List<Vector2>();
            for (uint t = 0; t < StandTries && stands.Count < Stands; t++)
            {
                uint hash = Mix(Seed ^ (t * 0x9e3779b9u));
                float angle = (hash & 0xffffu) * (2f * Mathf.PI / 65536f);
                float radius = hexM * (0.05f + 0.40f * ((hash >> 16) / 65535f));   // design values: inside the hex
                var at = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (!Clear(ground, map, hex, at, StandRadiusM) || stands.Any(s => (s - at).sqrMagnitude < MinStandSpacingM * MinStandSpacingM)) continue;
                stands.Add(at);
            }
            for (int s = 0; s < stands.Count; s++)
            {
                int n = 0;
                for (uint t = 0; t < ClumpTries && n < ClumpsPerStand; t++)
                {
                    uint hash = Mix(Seed ^ 0x5a17u ^ ((uint)s * 0x85ebca6bu) ^ (t * 0x632be5abu));
                    float angle = (hash & 0xffffu) * (2f * Mathf.PI / 65536f);
                    float radius = StandRadiusM * Mathf.Sqrt((hash >> 16) / 65535f);
                    var at = stands[s] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (!Clear(ground, map, hex, at, 0.5f * ClumpM) ||
                        result.Any(w => (w.At - at).sqrMagnitude < MinClumpSpacingM * MinClumpSpacingM)) continue;
                    result.Add(($"papyrus-{s}-{n}", at, Mix(hash ^ 0x51ed270bu) % 360u));
                    n++;
                }
            }
            return result;
        }

        MapScene? map;
        Statecraft.Sim.Content? content;
        GameObject? prefab;
        bool requested, answered, drained;
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        List<(string Key, Vector2 At, float Yaw)> wants = new List<(string, Vector2, float)>();
        public readonly List<(string Key, Vector3 At, float Yaw)> Placed = new List<(string, Vector3, float)>();
        public bool LastClose { get; private set; }
        public bool Requested => requested;
        public int Wanted => wants.Count;

        /// <summary>The layer on <paramref name="map"/>, made on the first call. <paramref name="drained"/>: the Hula site is
        /// built (drained, R-SWAMP-001), so no papyrus is drawn.</summary>
        public static HulaPapyrusLayer? ShowAt(MapScene map, Statecraft.Sim.Content content, bool drained)
        {
            if (map == null || map.Terrain == null || map.Hexes == null || content == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<HulaPapyrusLayer>();
            if (layer == null)
            {
                var go = new GameObject(Name);
                go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<HulaPapyrusLayer>();
                layer.map = map; layer.content = content;
                layer.wants = Wants(map, content);
                layer.drained = drained;
                return layer;
            }
            if (layer.drained != drained) { layer.drained = drained; layer.Rebuild(); }
            return layer;
        }

        /// <summary>Whether the layer's papyrus is drained away (the site built).</summary>
        public static bool Drained(Statecraft.Sim.GameState state) =>
            state.Sites.TryGetValue(SiteId, out var w) && w.BuiltOn != null;

        /// <summary>The close-zoom switch for one camera: the first close view asks for the model.</summary>
        internal void SetClose(bool on)
        {
            LastClose = on;
            if (on && !requested && !drained && wants.Count > 0)
            {
                requested = true;
                Request(Model, CollectionDetail.Near, OnModel);
            }
            foreach (var r in renderers) if (r != null) r.forceRenderingOff = !on;
        }

        void OnModel(GameObject? p)
        {
            if (this == null) return;
            answered = true;
            prefab = p;
            if (p == null) Debug.LogWarning("HULA PAPYRUS: the model did not stream; no papyrus is drawn, and the map plays on without it.");
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
            renderers.Clear(); Placed.Clear();
            if (drained || !answered || prefab == null) return;
            var ground = PlacementLayer.CampGround(map);
            var hex = SiteHex(content!);
            if (hex == null) return;
            foreach (var w in wants)
            {
                if (!Clear(ground, map, hex.Value, w.At, 0.5f * ClumpM)) continue;
                var go = Instantiate(prefab, transform, false);
                go.name = w.Key;
                go.transform.localRotation = Quaternion.Euler(0f, w.Yaw, 0f) * prefab.transform.localRotation;
                go.transform.localScale = prefab.transform.localScale * (SettlementKits.ModelScale * DrawScale);
                var (lo, hi) = GroundSpan(ground, w.At);
                var pos = new Vector3(w.At.x, 0.5f * (lo + hi), w.At.y);
                go.transform.position = pos;
                var placed = Bounds(go);
                go.transform.position += new Vector3(pos.x - placed.center.x, pos.y - placed.min.y, pos.z - placed.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.forceRenderingOff = !LastClose;
                    renderers.Add(r);
                }
                Placed.Add((w.Key, pos, w.Yaw));
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

        public static bool CloseAt(float distanceM, bool? was) => was == true ? distanceM <= CloseExitM : distanceM < CloseEnterM;

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool on = CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = on;
            SetClose(on);
        }
    }
}
