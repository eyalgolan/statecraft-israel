#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>MATKOT's four civilian road vehicles of The 1948 Collection (H01, H04, H06, H10) on their 1948 roads (issue
    /// #725). Presentation only: a few parked vehicles, never units, never in the sim. The near prefabs stream through
    /// <see cref="CollectionStream"/> (nothing is fetched before the frame after the request, so never for the first frame)
    /// and draw only at the close zoom. Each vehicle stands on its road's drawn line, faces along it, keeps clear of the
    /// towns (their labels), the unit tokens and the other vehicles, and stands only on ground Israel held on the game date
    /// (<see cref="Control1948"/>, the map's own sourced control layer). It is sized against the steam locomotive the map
    /// runs (<see cref="ServiceTrain"/>): <see cref="LengthShare"/> of the locomotive's car. A model that does not stream
    /// removes only its own vehicles.</summary>
    public sealed class RoadVehicles : MonoBehaviour
    {
        public const string Name = "RoadVehicles";
        public const string IntercityBus = "figure-vehicle-intercity-bus-1948-v2";
        public const string CitrusTruck = "figure-vehicle-citrus-truck-1948-v2";
        public const string CanvasLorry = "figure-vehicle-canvas-lorry-1948-v2";
        public const string SandwichConvoy = "figure-vehicle-sandwich-convoy-1948-v2";

        /// <summary>The placement seed (design value).</summary>
        public const int Seed = 19480725;
        /// <summary>The close zoom (design value): the vehicles draw from 30 km of camera distance in, and off again beyond 36 km.</summary>
        public const float CloseEnterM = 30000f, CloseExitM = 36000f;
        /// <summary>The locomotive's car of the service train on the map (m): MATKOT's locomotive and tender at
        /// RouteTrain.Scale times ServiceTrain.ModelSize, in ServiceTrain.Cars cars (about 204 m).</summary>
        public const float LocomotiveCarM = RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize / ServiceTrain.Cars;
        /// <summary>A vehicle's length as a share of the locomotive's car (design value; the owner on v0.1.1, 18 October
        /// 2026: MATKOT's models were "way bigger then the train"): every road vehicle reads smaller than the locomotive,
        /// about 102 m.</summary>
        public const float LengthShare = 0.5f;
        public const float VehicleLengthM = LengthShare * LocomotiveCarM;
        /// <summary>How far a vehicle keeps from a town's ground, a unit's clearing or a token's ring (m, design value).</summary>
        public const float AnchorClearanceM = 450f;
        /// <summary>How far apart two vehicles stand (m, design value).</summary>
        public const float PairSpacingM = 1800f;
        /// <summary>Seeded spots tried per vehicle before it is left out (design value).</summary>
        public const int CandidateCount = 96;
        /// <summary>The vehicle's base above the ground under its centre (m): the route ribbons' own lift
        /// (RouteLayer.ClearanceM), plus 2 m, so it stands on the ribbon, not in it.</summary>
        public const float RideM = RouteLayer.ClearanceM + 2f;

        /// <summary>One model on one route from a date (and to one, exclusive). Counts are design values.</summary>
        public readonly struct Plan
        {
            public readonly string Model, Route; public readonly DateTime From; public readonly DateTime? To; public readonly int Count;
            public Plan(string model, string route, DateTime from, DateTime? to, int count)
            { Model = model; Route = route; From = from; To = to; Count = count; }
            public bool Covers(DateTime date) => date >= From && (To == null || date < To.Value);
        }

        static readonly DateTime Start = new DateTime(1948, 5, 15), BurmaOpen = new DateTime(1948, 6, 14);
        // Approximate: Road of Valour inaugurated 7 December 1948, a proxy for the last convoy under the owner's #949 rule.
        // Source: research_notes/Map/burma-road-convoy-end.md (#1051).
        static readonly DateTime BurmaConvoysEnd = new DateTime(1948, 12, 7);

        /// <summary>Where each model runs (content/map/routes_1948.yaml): the convoy truck and the canvas-tilt lorry on the
        /// Burma Road, Jerusalem's supply road from its completion on 14 June 1948 (the route's `from`, Burma Road (Israel));
        /// the Jaffa–Jerusalem road gets no convoy, as no source in content dates one on it inside the game window (from 15
        /// May 1948; the Arab Legion held Latrun from 18 May). The bus and the citrus truck on the Tel Aviv–Haifa road (1946
        /// map, from 1937) from the game's start. On every date a vehicle stands only where the control layer has Israel.</summary>
        public static readonly IReadOnlyList<Plan> Plans = new[]
        {
            new Plan(SandwichConvoy, "burma_road", BurmaOpen, BurmaConvoysEnd, 1),
            new Plan(CanvasLorry, "burma_road", BurmaOpen, BurmaConvoysEnd, 1),
            new Plan(IntercityBus, "tel_aviv_haifa_road", Start, null, 2),
            new Plan(CitrusTruck, "tel_aviv_haifa_road", Start, null, 2),
        };

        /// <summary>A test seam: how the layer asks for a model. Null (the default): the game's stream in Play mode, and none
        /// in the editor's edit mode (as UnitFigures), so a map built by an editor tool or another test fetches nothing.</summary>
        internal static Action<string, CollectionDetail, Action<GameObject?>>? Request;
        static Action<string, CollectionDetail, Action<GameObject?>>? Stream =>
            Request ?? (Application.isPlaying ? CollectionStream.Request : (Action<string, CollectionDetail, Action<GameObject?>>?)null);

        public readonly struct PlacedVehicle
        {
            public readonly string Model, Route; public readonly Vector2 At, Along; public readonly Transform Transform;
            public PlacedVehicle(string model, string route, Vector2 at, Vector2 along, Transform transform)
            { Model = model; Route = route; At = at; Along = along; Transform = transform; }
        }

        MapScene? map;
        TerrainData? terrain;
        Vector3 terrainOrigin;
        readonly Dictionary<string, List<Vector2>> lines = new Dictionary<string, List<Vector2>>(StringComparer.Ordinal);
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> asked = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<Renderer> renderers = new List<Renderer>();
        DateTime date;
        string? drawnKey;
        bool lastClose;

        public readonly List<PlacedVehicle> Placed = new List<PlacedVehicle>();
        /// <summary>How many times the vehicles were laid out (a test reads it: a date change with nothing new costs nothing).</summary>
        public int Builds { get; private set; }

        public static RoadVehicles Create(Transform parent, MapScene map, IReadOnlyList<RoutePath> routes, DateTime date)
        {
            var go = new GameObject(Name); go.transform.SetParent(parent, false);
            var layer = go.AddComponent<RoadVehicles>();
            layer.map = map; layer.terrain = map.Terrain.terrainData; layer.terrainOrigin = map.Terrain.transform.position;
            foreach (var route in routes)
                if (Plans.Any(p => p.Route == route.Id)) layer.lines[route.Id] = RouteLayer.Drawn(map.Frame, route);
            layer.ShowAt(date);
            return layer;
        }

        /// <summary>Shows the vehicles as at <paramref name="at"/>. The game calls it on every state change, so it lays them
        /// out again only when what it would draw changes: the plans in force, the control period, or a model arriving.</summary>
        public void ShowAt(DateTime at)
        {
            date = at;
            var stream = Stream;
            if (stream != null)
                foreach (var plan in Plans)
                    if (plan.Covers(at) && asked.Add(plan.Model)) { var id = plan.Model; stream(id, CollectionDetail.Near, p => OnModel(id, p)); }
            Rebuild();
        }

        void OnModel(string model, GameObject? prefab)
        {
            if (this == null) return;   // the map was rebuilt before the model came; the new layer asks again
            prefabs[model] = prefab;
            if (prefab == null)
                Debug.LogWarning($"ROAD VEHICLES: {model} did not stream; its vehicles are not drawn, and the map plays on without them.");
            Rebuild();
        }

        /// <summary>What the layer would draw on the current date, as a key: the control period, the plans in force and the
        /// models that have arrived.</summary>
        string Key()
        {
            var control = map != null ? map.Control : null;
            var period = control?.PeriodOn(date)?.Id ?? "none";
            return period + "|" + string.Concat(Plans.Select(p => p.Covers(date) ? "1" : "0")) + "|"
                + string.Concat(Plans.Select(p => prefabs.TryGetValue(p.Model, out var g) && g != null ? "1" : "0"));
        }

        void Rebuild()
        {
            if (map == null || terrain == null) return;
            var key = Key();
            if (key == drawnKey) return;
            drawnKey = key; Builds++;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            renderers.Clear(); Placed.Clear();
            var control = map.Control;
            if (control == null) return;   // no control layer: no claim about who held a road, so no vehicle
            // Every plan in force reserves its spots, drawn or not, so a model streaming in late never moves another's.
            var taken = new List<Vector2>();
            for (int planIndex = 0; planIndex < Plans.Count; planIndex++)
            {
                var plan = Plans[planIndex];
                if (!plan.Covers(date) || !lines.TryGetValue(plan.Route, out var line)) continue;
                prefabs.TryGetValue(plan.Model, out var prefab);
                for (int n = 0; n < plan.Count; n++)
                {
                    var spot = Spot(line, planIndex, n, taken, control);
                    if (spot == null) continue;
                    var (at, along) = spot.Value;
                    taken.Add(at);
                    if (prefab != null) Place(plan, n, prefab, at, along);
                }
            }
        }

        /// <summary>One vehicle: the prefab (its root keeps the FBX's axis turn), its long side turned to +Z, sized to
        /// <see cref="VehicleLengthM"/> while still unturned (a box measured after the yaw would shrink it on a diagonal),
        /// then the road's yaw on top, centred on the spot with its base <see cref="RideM"/> over the ground.</summary>
        void Place(Plan plan, int n, GameObject prefab, Vector2 at, Vector2 along)
        {
            var go = Instantiate(prefab, transform, false); go.name = $"{plan.Model}_{n}";
            var own = prefab.transform.localRotation;
            go.transform.localRotation = own;
            var b = Bounds(go);
            if (b.size.x > b.size.z) { own = Quaternion.Euler(0f, 90f, 0f) * own; go.transform.localRotation = own; b = Bounds(go); }
            if (b.size.z > 1e-3f) go.transform.localScale *= VehicleLengthM / b.size.z;
            float yaw = Mathf.Atan2(along.x, along.y) * Mathf.Rad2Deg;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * own;
            float y = RouteLayer.Ground(terrain!, terrainOrigin, at) + RideM;
            go.transform.position = new Vector3(at.x, y, at.y);
            b = Bounds(go);
            go.transform.position += new Vector3(at.x - b.center.x, y - b.min.y, at.y - b.center.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On; r.forceRenderingOff = !lastClose; renderers.Add(r);
            }
            Placed.Add(new PlacedVehicle(plan.Model, plan.Route, at, along, go.transform));
        }

        (Vector2 at, Vector2 along)? Spot(IReadOnlyList<Vector2> line, int planIndex, int ordinal, List<Vector2> taken, Control1948 control)
        {
            if (map == null || line.Count < 2) return null;
            var lengths = new float[line.Count];
            for (int i = 1; i < line.Count; i++) lengths[i] = lengths[i - 1] + (line[i] - line[i - 1]).magnitude;
            float total = lengths[lengths.Length - 1]; if (total < 1f) return null;
            uint state = (uint)(Seed ^ (planIndex * 73856093) ^ (ordinal * 19349663));
            for (int k = 0; k < CandidateCount; k++)
            {
                state = state * 1664525u + 1013904223u;
                float s = total * ((state & 0x00ffffffu) / 16777216f);
                int i = 1; while (i < lengths.Length - 1 && lengths[i] < s) i++;
                var delta = line[i] - line[i - 1]; float len = delta.magnitude;
                if (len < 1e-3f) continue;
                var at = Vector2.Lerp(line[i - 1], line[i], (s - lengths[i - 1]) / len);
                if (Free(at, taken) && Held(at, control)) return (at, delta / len);
            }
            return null;
        }

        /// <summary>Israel held the hex under <paramref name="at"/> on the date (the control layer's code; contested,
        /// no-man's land and no data count as not held).</summary>
        bool Held(Vector2 at, Control1948 control)
        {
            var (q, r) = MapPointer.Pick(map!, new Vector3(at.x, 0f, at.y));
            return control.At(q, r, date).side == Control1948.Side.Israel;
        }

        bool Free(Vector2 at, IEnumerable<Vector2> taken)
        {
            if (map == null) return false;
            foreach (var p in taken) if ((at - p).sqrMagnitude < PairSpacingM * PairSpacingM) return false;
            foreach (var f in map.KitFootprints)
            { float d = f.radius + AnchorClearanceM; if ((at - f.centre).sqrMagnitude < d * d) return false; }
            foreach (var c in map.UnitClearings)
            { float d = c.radius + AnchorClearanceM; if ((at - c.centre).sqrMagnitude < d * d) return false; }
            foreach (var t in map.UnitTokens)
            { float d = t.radius + AnchorClearanceM; if ((at - t.centre).sqrMagnitude < d * d) return false; }
            return true;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        public static bool CloseAt(float distanceM, bool was) => was ? distanceM <= CloseExitM : distanceM <= CloseEnterM;
        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;
        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID(); bool on = CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) && was);
            closeFor[id] = on; lastClose = on;
            foreach (var r in renderers) if (r != null) r.forceRenderingOff = !on;
        }
    }
}
