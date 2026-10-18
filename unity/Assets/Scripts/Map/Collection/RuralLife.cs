#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Routes;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Period-generic farm and village life from MATKOT's 1948 Collection (issue #667, whose sourced setting is:
    /// tractors and horse-drawn work at kibbutz, moshav and moshava fields; sheep and donkeys at villages; camels at
    /// Bedouin areas). This is presentation only: the figures are scenery, never civilian or combat units.
    ///
    /// A fixed hash orders the eligible anchors, the first <see cref="MaxAnchorsPerModel"/> with clear ground are used, and
    /// another hash chooses each anchor's first bearing. Models stand outside the anchor footprint, clear of every kit,
    /// route, unit token, water and other rural model, at the buildings' one world scale (<see cref="WorldScale"/>: a
    /// tractor is about a third of the steam train's locomotive, a sheep about a seventh). Sheep stand as a small flock.
    /// Both shared prefab details stream through <see cref="CollectionStream"/> after the first frame. Far static meshes draw above 45 km; near meshes
    /// draw at 40 km and closer, with hysteresis, and only their existing idle Animators run there. A missing streamed
    /// prefab leaves that detail out and the map continues.</summary>
    public sealed class RuralLife : MonoBehaviour
    {
        public const string Name = "RuralLife";
        public const string Tractor = "figure-vehicle-farm-tractor-1948-v2";
        public const string Wagon = "figure-vehicle-horse-wagon-1948-v2";
        public const string Horse = "figure-draught-horse-idle-1948-v2";
        public const string Sheep = "figure-sheep-idle-1948-v1";
        public const string Donkey = "figure-donkey-1948-v1";
        public const string Camel = "figure-dromedary-camel-1948-v1";
        public static readonly string[] Models = { Tractor, Wagon, Horse, Sheep, Donkey, Camel };

        // Design values (#667): sparse scenery, not a population statement or simulation rule.
        public const int MaxAnchorsPerModel = 3;
        /// <summary>The house the world scale is set by (m, real): the middle of the kit's default house, 7.5 to 10 m long.</summary>
        public const float ReferenceHouseM = 8.5f;
        /// <summary>The one world scale of the map's MATKOT models up close (the owner on v0.1.1, 18 October 2026: the
        /// buildings were "way bigger then the train"): the reference house is as long as the locomotive's car of the steam
        /// train the map runs (ServiceTrain: MATKOT's locomotive and tender at RouteTrain.Scale x ServiceTrain.ModelSize,
        /// about 816 m, in ServiceTrain.Cars cars of about 204 m), so 204 / 8.5 = 24x real. The same rule as the #700
        /// buildings' scale fix (branch map/town-models-world-scale, SettlementKits.ModelScale), so a tractor stands beside
        /// a kibbutz house at their real proportion. (Tripo's meshes are all about one unit across, so one shared scale
        /// would make a sheep as long as a tractor.)</summary>
        public const float WorldScale = RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize / ServiceTrain.Cars / ReferenceHouseM;
        /// <summary>Design values (#667), typical of each kind, not sourced measurements: the real long side (m), seen from
        /// above, each model stands for.</summary>
        public static float RealLengthM(string model) => model switch
        {
            Tractor => 3f, Wagon => 5f, Horse => 2.5f, Sheep => 1.3f, Donkey => 1.8f, Camel => 3f,
            _ => throw new ArgumentException($"'{model}' is not one of #667's models", nameof(model)),
        };
        /// <summary>The model's long side on the map (m).</summary>
        public static float SizeOf(string model) => RealLengthM(model) * WorldScale;
        // Design values (#667): a flock is a few sheep, spaced a couple of sheep apart around the scene's spot.
        public const int FlockSize = 5;
        public const float FlockSpacingLengths = 2.5f;
        public static int MembersOf(string model) => model == Sheep ? FlockSize : 1;
        /// <summary>The ground a scene takes (m, radius about its spot): its model, or the flock around the spot, with one
        /// model's length to spare. The kit trees whose crowns reach into it are left out at draw time
        /// (Kits.VegetationLayer.Hide, as under the unit figures), so no orchard tree stands over a tractor.</summary>
        public static float SceneRadiusM(string model) => (MembersOf(model) > 1 ? FlockSpacingLengths + 1.5f : 1.5f) * SizeOf(model);
        // Design value (#667): clearance for the scaled model and its readable space at gameplay zoom.
        public const float ClearanceRadiusM = 700f;
        // #700's town-detail close/far band (40/45 km), reused.
        public const float CloseEnterM = Kits.TownModelLayer.CloseEnterM, CloseExitM = Kits.TownModelLayer.CloseExitM;
        // Design value (#667): eight bearings on three rings gives a bounded search without dense scattering.
        public const int Bearings = 8, Rings = 3;
        // Design values (#667): start beyond the clearance disc, then step outward by one clearance radius per ring.
        public const float FirstRingClearances = 1.25f, RingStepClearances = 1f;
        // Design value (#667): neutral headings diversified by the deterministic seed.
        public const float DefaultYaw = 0f;

        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = CollectionStream.Request;

        public readonly struct Want : IEquatable<Want>
        {
            public readonly string Key, Model; public readonly Vector2 Anchor; public readonly float AnchorRadius;
            public Want(string key, string model, Vector2 anchor, float anchorRadius)
            { Key = key; Model = model; Anchor = anchor; AnchorRadius = anchorRadius; }
            public bool Equals(Want other) => Key == other.Key && Model == other.Model && Anchor == other.Anchor && AnchorRadius == other.AnchorRadius;
            public override bool Equals(object? obj) => obj is Want other && Equals(other);
            public override int GetHashCode() => (Key, Model, Anchor, AnchorRadius).GetHashCode();
        }

        readonly struct Anchor
        {
            public readonly string Key, Type; public readonly Vector2 At; public readonly float Radius;
            public Anchor(string key, string type, Vector2 at, float radius) { Key = key; Type = type; At = at; Radius = radius; }
        }

        /// <summary>Every eligible anchor for every model, each model's in its fixed hash order; the layer uses the first
        /// <see cref="MaxAnchorsPerModel"/> of each that have clear ground.</summary>
        public static List<Want> Wants(MapScene map)
        {
            var footprints = new Dictionary<string, (Vector2 At, float Radius)>(StringComparer.Ordinal);
            foreach (var f in map.KitFootprints) if (!footprints.ContainsKey(f.id)) footprints[f.id] = (f.centre, f.radius);
            var anchors = new List<Anchor>();
            void Add(Statecraft.Game.Map.Settlement s)
            {
                if (!footprints.TryGetValue(s.Id, out var f)) return;
                if (!anchors.Any(a => a.Key == s.Id)) anchors.Add(new Anchor(s.Id, s.Type, f.At, f.Radius));
            }
            foreach (var s in map.Settlements) Add(s);
            foreach (var p in map.Localities) Add(p.Settlement);
            foreach (var f in map.KitFootprints.Where(f => f.id.StartsWith("camp:", StringComparison.Ordinal)))
                if (!anchors.Any(a => a.Key == f.id)) anchors.Add(new Anchor(f.id, "bedouin_area", f.centre, f.radius));

            var result = new List<Want>();
            AddModel(Tractor, anchors.Where(a => a.Type == "kibbutz"));
            var horseFields = anchors.Where(a => a.Type == "moshav" ||
                (TownLandmarks.ById.TryGetValue(a.Key, out var model) && model == TownLandmarks.Moshava)).ToList();
            AddModel(Wagon, horseFields); AddModel(Horse, horseFields);
            var villages = anchors.Where(a => a.Type == "village").ToList();   // #667: "at the villages", not the towns
            AddModel(Sheep, villages); AddModel(Donkey, villages);
            AddModel(Camel, anchors.Where(a => a.Type == "bedouin_area"));
            return result;

            void AddModel(string model, IEnumerable<Anchor> eligible)
            {
                foreach (var a in eligible.OrderBy(x => StableHash(model + ":" + x.Key)).ThenBy(x => x.Key, StringComparer.Ordinal))
                    result.Add(new Want(a.Key, model, a.At, a.Radius));
            }
        }

        /// <summary>FNV-1a over UTF-16 code units: stable across Unity runtimes (unlike string.GetHashCode).</summary>
        public static uint StableHash(string value)
        {
            uint h = 2166136261u;
            foreach (char c in value) { h ^= c; h = unchecked(h * 16777619u); }
            return h;
        }

        public static Vector2? Spot(Want want, Func<Vector2, float, bool> free)
        {
            uint seed = StableHash(want.Model + ":" + want.Key);
            int first = (int)(seed % Bearings);
            for (int ring = 0; ring < Rings; ring++)
            {
                float distance = want.AnchorRadius + ClearanceRadiusM * (FirstRingClearances + ring * RingStepClearances);
                for (int k = 0; k < Bearings; k++)
                {
                    float angle = (first + k) * (360f / Bearings) * Mathf.Deg2Rad;
                    var p = want.Anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (free(p, ClearanceRadiusM)) return p;
                }
            }
            return null;
        }

        public static float YawOf(Want want, int member = 0) => DefaultYaw + StableHash(want.Key + ":" + want.Model + (member == 0 ? "" : "#" + member)) % 360u;

        /// <summary>Where member <paramref name="member"/> of a scene at <paramref name="spot"/> stands: the spot itself, or
        /// around it for a flock's other sheep.</summary>
        public static Vector2 MemberAt(Want want, Vector2 spot, int member)
        {
            if (member == 0) return spot;
            float a = (StableHash(want.Key + ":" + want.Model) % 360u + member * 360f / (MembersOf(want.Model) - 1)) * Mathf.Deg2Rad;
            return spot + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * FlockSpacingLengths * SizeOf(want.Model);
        }
        public static bool CloseAt(float distanceM, bool? was) => was == true ? distanceM <= CloseExitM : distanceM <= CloseEnterM;

        MapScene? map;
        readonly Dictionary<(string Model, CollectionDetail Detail), GameObject?> prefabs = new Dictionary<(string, CollectionDetail), GameObject?>();
        readonly HashSet<(string Model, CollectionDetail Detail)> asked = new HashSet<(string, CollectionDetail)>();
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<(Renderer Renderer, CollectionDetail Detail)> renderers = new List<(Renderer, CollectionDetail)>();
        readonly List<(Animator Animator, CollectionDetail Detail)> animators = new List<(Animator, CollectionDetail)>();
        List<Want> wants = new List<Want>();

        public readonly List<(string Key, string Model, CollectionDetail Detail, Vector3 At, string Name, int Member)> Placed =
            new List<(string, string, CollectionDetail, Vector3, string, int)>();
        /// <summary>The scenes drawn (one per anchor used), and the models with fewer than MaxAnchorsPerModel of them.</summary>
        public readonly List<Want> Scenes = new List<Want>();
        public readonly List<string> Missing = new List<string>();
        public IReadOnlyList<Want> Wanted => wants;
        public bool LastClose { get; private set; }

        /// <summary>The ground the other Collection layers hold (centre, radius m): the civilian figures CivilianPeople wants
        /// now (its FigureClearanceM) and the wildlife (half WildlifeLayer.SizeM), so no rural scene stands under them (#667:
        /// kept clear of the other props). GameRoot shows those layers first.</summary>
        public static List<(Vector2 centre, float radius)> OtherProps(MapScene map)
        {
            var discs = new List<(Vector2, float)>();
            var people = map.Terrain != null ? map.Terrain.transform.Find(CivilianPeople.Name)?.GetComponent<CivilianPeople>() : null;
            if (people != null) foreach (var w in people.Wanted) discs.Add((new Vector2(w.At.x, w.At.z), CivilianPeople.FigureClearanceM));
            foreach (var w in WildlifeLayer.Wants(map)) discs.Add((w.At, 0.5f * WildlifeLayer.SizeM));
            return discs;
        }

        public static RuralLife? Show(MapScene map)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<RuralLife>();
            if (layer == null)
            {
                var go = new GameObject(Name);
                go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<RuralLife>();
                layer.map = map;
            }
            layer.Show(Wants(map), OtherProps(map));
            return layer;
        }

        List<(Vector2 centre, float radius)> others = new List<(Vector2, float)>();

        void Show(List<Want> next, List<(Vector2 centre, float radius)> nextOthers)
        {
            if (next.SequenceEqual(wants) && nextOthers.SequenceEqual(others)) return;
            wants = next; others = nextOthers;
            foreach (var model in wants.Select(w => w.Model).Distinct())
                foreach (var detail in new[] { CollectionDetail.Far, CollectionDetail.Near })
                    if (asked.Add((model, detail)))
                    {
                        var m = model; var d = detail;
                        Request(m, d, prefab => OnModel(m, d, prefab));
                    }
            Rebuild();
        }

        void OnModel(string model, CollectionDetail detail, GameObject? prefab)
        {
            if (this == null) return;
            prefabs[(model, detail)] = prefab;
            if (prefab == null) Debug.LogWarning($"RURAL: {model} {detail.ToString().ToLowerInvariant()} did not stream; that rural detail stays off the map.");
            Rebuild();
        }

        void Rebuild()
        {
            if (map == null || map.Terrain == null) return;
            for (int i = transform.childCount - 1; i >= 0; i--) DestroyNow(transform.GetChild(i).gameObject);
            renderers.Clear(); animators.Clear(); Placed.Clear(); Scenes.Clear(); Missing.Clear();
            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var taken = new List<(Vector2 centre, float radius)>();
            bool Free(Vector2 p, float radius)
            {
                if (!TownLandmarks.Clear(p, radius, footprints) || !TownLandmarks.Clear(p, radius, taken) || !TownLandmarks.Clear(p, radius, others)) return false;
                if (!ground.Inside(p.x - radius, p.y - radius) || !ground.Inside(p.x + radius, p.y + radius)) return false;
                for (int k = 0; k < 9; k++)
                {
                    var q = k == 0 ? p : p + new Vector2(Mathf.Cos(k * Mathf.PI / 4f), Mathf.Sin(k * Mathf.PI / 4f)) * radius * 0.8f;
                    if (ground.Water(q.x, q.y)) return false;
                }
                return !ground.OnRoute(p, radius) && !ground.OnToken(p, radius);
            }
            var used = new Dictionary<string, int>(StringComparer.Ordinal);
            var holes = new List<(Vector2 centre, float radius)>();
            foreach (var want in wants)
            {
                bool answered = prefabs.ContainsKey((want.Model, CollectionDetail.Far)) && prefabs.ContainsKey((want.Model, CollectionDetail.Near));
                if (!answered) continue;
                bool any = prefabs[(want.Model, CollectionDetail.Far)] != null || prefabs[(want.Model, CollectionDetail.Near)] != null;
                used.TryGetValue(want.Model, out int n);
                if (!any || n >= MaxAnchorsPerModel) continue;
                var at = Spot(want, Free);
                if (at == null) continue;
                used[want.Model] = n + 1;
                Scenes.Add(want);
                taken.Add((at.Value, ClearanceRadiusM));
                holes.Add((at.Value, SceneRadiusM(want.Model)));
                for (int member = 0; member < MembersOf(want.Model); member++)
                {
                    var flat = MemberAt(want, at.Value, member);
                    var pos = new Vector3(flat.x, ground.Y(flat.x, flat.y), flat.y);
                    foreach (var detail in new[] { CollectionDetail.Far, CollectionDetail.Near })
                    {
                        var prefab = prefabs[(want.Model, detail)];
                        if (prefab == null) continue;
                        var go = Instantiate(prefab, transform, false);
                        string instanceName = want.Key + ":" + want.Model + (member == 0 ? "" : "#" + member) + ":" + detail.ToString().ToLowerInvariant();
                        go.name = instanceName;
                        // Sized at the world scale by its long side seen from above, measured before the map's yaw.
                        go.transform.localRotation = prefab.transform.localRotation;
                        var own = Bounds(go);
                        float edge = Mathf.Max(own.size.x, own.size.z);
                        go.transform.localScale = prefab.transform.localScale * (edge > 1e-4f ? SizeOf(want.Model) / edge : 1f);
                        go.transform.localRotation = Quaternion.Euler(0f, YawOf(want, member), 0f) * prefab.transform.localRotation;
                        go.transform.position = pos;
                        var bounds = Bounds(go);
                        go.transform.position += new Vector3(pos.x - bounds.center.x, pos.y - bounds.min.y, pos.z - bounds.center.z);
                        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                        {
                            renderer.shadowCastingMode = ShadowCastingMode.On;
                            renderer.forceRenderingOff = detail == CollectionDetail.Near ? !LastClose : LastClose;
                            renderers.Add((renderer, detail));
                        }
                        foreach (var animator in go.GetComponentsInChildren<Animator>(true))
                        {
                            animator.enabled = detail == CollectionDetail.Near && LastClose;
                            animators.Add((animator, detail));
                        }
                        Placed.Add((want.Key, want.Model, detail, pos, instanceName, member));
                    }
                }
            }
            Units.UnitShowcase.HideTrees(map, Name, holes);
            foreach (var model in wants.Select(w => w.Model).Distinct())
                if (prefabs.ContainsKey((model, CollectionDetail.Far)) && prefabs.ContainsKey((model, CollectionDetail.Near))
                    && (!used.TryGetValue(model, out int got) || got < MaxAnchorsPerModel)) Missing.Add(model);
            Debug.Log($"RURAL: {Scenes.Count} rural scenes placed ({Placed.Count} models); {Missing.Count} models short of {MaxAnchorsPerModel} scenes");
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var bounds = rs[0].bounds;
            foreach (var r in rs) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        static void DestroyNow(GameObject go)
        {
            if (Application.isPlaying) { go.transform.SetParent(null); Destroy(go); }
            else DestroyImmediate(go);
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        internal void Choose(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
            int id = camera.GetInstanceID();
            bool close = CloseAt(TownLandmarks.ViewDistance(camera), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close; LastClose = close;
            foreach (var (renderer, detail) in renderers)
                if (renderer != null) renderer.forceRenderingOff = detail == CollectionDetail.Near ? !close : close;
            foreach (var (animator, detail) in animators)
                if (animator != null) animator.enabled = detail == CollectionDetail.Near && close;
        }
    }
}
