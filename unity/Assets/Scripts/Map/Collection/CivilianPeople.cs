#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Small groups of MATKOT's civilian figures at the Israeli towns, kibbutzim, moshavim and the player's transit
    /// camps (issue #666 and #663's owner ruling that every delivered model is used). They are period-generic ambience,
    /// never units and never simulation state. C02, C03 and C13 stand at the settled places; C01 and C10 stand at a transit
    /// camp from the day it is placed. The Arab towns get none of this set: #666 maps it to the towns, kibbutzim, moshavim and
    /// camps, and the Collection's Arab villager figure (#708, waiting on its licence line) is the one made for them. Which
    /// figures stand at the Arab towns until then is an owner question on the pull request.
    ///
    /// Far static meshes stream after the first frame through <see cref="CollectionStream"/>. Near skinned meshes and their
    /// idle clips are not even requested until a camera enters close zoom. A missing bundle or prefab fails open: the affected
    /// figures stay off the map, the far mesh remains when a near mesh fails, and play continues. Repeated figures share the
    /// collection prefabs' GPU-instanced materials.</summary>
    public sealed class CivilianPeople : MonoBehaviour
    {
        public const string Name = "CivilianPeople";

        public const string ArrivingMan = "figure-c01-arriving-man-v2", Townsman = "figure-c02-townsman-v2",
            Craftsman = "figure-c03-smith-carpenter-v2", Woman = "figure-c10-woman-blue-dress-v2", Nurse = "figure-c13-nurse-v2";

        public static readonly string[] SettledModels = { Townsman, Craftsman, Nurse };
        public static readonly string[] CampModels = { ArrivingMan, Woman };
        public static readonly string[] SettledTypes = { "city", "town", "kibbutz", "moshav" };

        public const int SettledGroupSize = 3;   // design value: one small, legible group at a permanent settlement
        public const int CampGroupSize = 2;      // design value: one arriving pair at a transit camp
        public const float FigureScale = UnitShowcase.FigureScale;   // design value: reuse the established symbolic figure scale
        public const float FigureHeightM = UnitBadge.FigureHeightM * FigureScale;
        public const float FigureSpacingM = 1200f;   // design value: separates silhouettes without reading as a crowd
        public const float FigureClearanceM = 500f;  // design value: conservative half-width/clearance for one symbolic figure
        public const float LabelClearanceM = 400f;   // design value: extra ground outside the labelled settlement footprint
        public const float SearchStepM = 1400f;      // design value: outward step when the nearest ring is occupied
        public const int SearchRings = 8, SearchBearings = 16;   // design values: bounded cosmetic placement search
        // The near (animated) figures from 40 km in, the far ones again beyond 45 km: the close-zoom band of the town models
        // (TownModelLayer), so the people change detail with the buildings they stand by.
        public const float CloseEnterM = TownModelLayer.CloseEnterM, CloseExitM = TownModelLayer.CloseExitM;

        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = CollectionStream.Request;

        public readonly struct Want : IEquatable<Want>
        {
            public readonly string Key, Model; public readonly Vector3 At; public readonly float Yaw;
            public Want(string key, string model, Vector3 at, float yaw) { Key = key; Model = model; At = at; Yaw = yaw; }
            public bool Equals(Want other) => Key == other.Key && Model == other.Model && At == other.At && Yaw == other.Yaw;
            public override bool Equals(object? obj) => obj is Want other && Equals(other);
            public override int GetHashCode() => (Key, Model, At, Yaw).GetHashCode();
        }

        /// <summary>One successfully placed figure (test and diagnostics aid).</summary>
        public readonly struct PlacedFigure
        {
            public readonly string Key, Model; public readonly Vector3 At; public readonly float Yaw;
            public PlacedFigure(Want want) { Key = want.Key; Model = want.Model; At = want.At; Yaw = want.Yaw; }
        }

        /// <summary>Builds the deterministic presentation plan. Every group is outside its settlement or camp footprint,
        /// which also keeps it clear of the name pill anchored there, and clear of route corridors, water, unit tokens and
        /// all earlier groups. Settlement and placement order are their stable source-file/state order.</summary>
        public static List<Want> Wants(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content)
        {
            var wants = new List<Want>();
            if (map == null || map.Terrain == null || map.Hexes == null) return wants;
            var ground = PlacementLayer.CampGround(map);
            var radii = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var (_, radius, id) in map.KitFootprints) if (!radii.ContainsKey(id)) radii[id] = radius;
            var reserved = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var taken = new List<(Vector2 centre, float radius)>();

            foreach (var settlement in map.Settlements)
            {
                if (!SettledTypes.Contains(settlement.Type)) continue;
                var world = map.Frame.ToWorld(settlement.Lon, settlement.Lat, 0f);
                float radius = radii.TryGetValue(settlement.Id, out var kitRadius)
                    ? kitRadius : 0.5f * SettlementPlacer.MarkerHeightM(settlement.Type);
                AddGroup(settlement.Id, new Vector2(world.x, world.z), radius, SettledModels, date, ground, reserved, taken, wants);
            }

            foreach (var placement in placements)
            {
                if (placement.Action != MapActions.TransitCamp || date < placement.Date) continue;
                var hex = content.HexAt(placement.Q, placement.R);
                if (hex == null) continue;
                var world = map.Frame.ToWorld(hex.Lon, hex.Lat, 0f);
                string key = $"{placement.Action}_{placement.Q}_{placement.R}";
                // Reserve the whole hex around the later-built ma'abara. Its seeded kit may move inside the hex while it
                // searches for room; the people therefore stand just outside it rather than assuming an unsourced camp plan.
                AddGroup(key, new Vector2(world.x, world.z), map.Hexes.hex_size_km * 1000f, CampModels, placement.Date,
                    ground, reserved, taken, wants);
            }
            return wants;
        }

        static void AddGroup(string key, Vector2 anchor, float anchorRadius, IReadOnlyList<string> models, DateTime from,
            KitGround ground, List<(Vector2 centre, float radius)> reserved, List<(Vector2 centre, float radius)> taken, List<Want> wants)
        {
            var random = new KitRandom(key, 666UL);
            float turn = random.Range(0f, 360f);
            var offsets = Offsets(models.Count, turn);
            float groupRadius = offsets.Max(o => o.magnitude) + FigureClearanceM;

            bool Free(Vector2 centre, float radius)
            {
                if (!TownLandmarks.Clear(centre, radius, reserved) || !TownLandmarks.Clear(centre, radius, taken)) return false;
                if (!ground.Inside(centre.x - radius, centre.y - radius) || !ground.Inside(centre.x + radius, centre.y + radius)) return false;
                if (ground.OnRoute(centre, radius) || ground.OnToken(centre, radius) || ground.OnToken(centre, radius, from)) return false;
                foreach (var offset in offsets)
                {
                    var p = centre + offset;
                    if (ground.Water(p.x, p.y)) return false;
                }
                return true;
            }

            Vector2? spot = null;
            int firstBearing = random.Range(0, SearchBearings);
            for (int ring = 0; ring < SearchRings && spot == null; ring++)
            {
                float distance = anchorRadius + LabelClearanceM + groupRadius + ring * SearchStepM;
                for (int step = 0; step < SearchBearings; step++)
                {
                    float angle = (firstBearing + step) * (360f / SearchBearings) * Mathf.Deg2Rad;
                    var candidate = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (Free(candidate, groupRadius)) { spot = candidate; break; }
                }
            }
            if (spot == null) return;
            taken.Add((spot.Value, groupRadius));
            for (int i = 0; i < models.Count; i++)
            {
                var p = spot.Value + offsets[i];
                wants.Add(new Want($"{key}/{i}", models[i], new Vector3(p.x, ground.Y(p.x, p.y), p.y), random.Range(0f, 360f)));
            }
        }

        static Vector2[] Offsets(int count, float turn)
        {
            var offsets = new Vector2[count];
            float radius = count == 2 ? FigureSpacingM * 0.5f : FigureSpacingM / Mathf.Sqrt(3f);
            float phase = turn * Mathf.Deg2Rad;
            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * 2f * Mathf.PI / count;
                offsets[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return offsets;
        }

        MapScene? map;
        List<Want> wanted = new List<Want>();
        readonly Dictionary<(string Model, CollectionDetail Detail), GameObject?> prefabs =
            new Dictionary<(string, CollectionDetail), GameObject?>();
        readonly HashSet<(string Model, CollectionDetail Detail)> asked = new HashSet<(string, CollectionDetail)>();
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<(Renderer[] Far, Renderer[] Near)> pairs = new List<(Renderer[], Renderer[])>();
        bool lastClose;

        public readonly List<PlacedFigure> Placed = new List<PlacedFigure>();
        public readonly List<string> Missing = new List<string>();
        public IReadOnlyList<Want> Wanted => wanted;

        public static CivilianPeople? ShowAt(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<CivilianPeople>();
            if (layer == null)
            {
                var go = new GameObject(Name);
                go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<CivilianPeople>();
                layer.map = map;
            }
            layer.Show(date, placements, content);
            return layer;
        }

        void Show(DateTime at, IEnumerable<Placement> nextPlacements, Content nextContent)
        {
            var next = Wants(map!, at, nextPlacements, nextContent);
            if (next.SequenceEqual(wanted)) return;
            wanted = next;
            Ask(CollectionDetail.Far);
            Rebuild();
        }

        void Ask(CollectionDetail detail)
        {
            foreach (var model in wanted.Select(w => w.Model).Distinct())
            {
                var key = (model, detail);
                if (!asked.Add(key)) continue;
                Request(model, detail, prefab => OnModel(model, detail, prefab));
            }
        }

        void OnModel(string model, CollectionDetail detail, GameObject? prefab)
        {
            if (this == null) return;
            prefabs[(model, detail)] = prefab;
            if (prefab == null)
                Debug.LogWarning($"PEOPLE: {model} {detail.ToString().ToLowerInvariant()} did not stream; "
                    + (detail == CollectionDetail.Far ? "its civilians are not drawn" : "its far civilians remain") + ", and the map plays on.");
            Rebuild();
        }

        void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            pairs.Clear(); Placed.Clear(); Missing.Clear();
            var parents = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var want in wanted)
            {
                bool farKnown = prefabs.TryGetValue((want.Model, CollectionDetail.Far), out var farPrefab);
                if (!farKnown) continue;
                if (farPrefab == null) { Missing.Add(want.Key); continue; }
                int slash = want.Key.LastIndexOf('/');
                string groupKey = slash < 0 ? want.Key : want.Key.Substring(0, slash);
                string personKey = slash < 0 ? "0" : want.Key.Substring(slash + 1);
                if (!parents.TryGetValue(groupKey, out var parent))
                {
                    parent = new GameObject(groupKey).transform;
                    parent.SetParent(transform, false);
                    parents[groupKey] = parent;
                }
                var person = new GameObject(personKey).transform;
                person.SetParent(parent, false);
                var far = Make(person, "Far", farPrefab, want);
                Renderer[] near = Array.Empty<Renderer>();
                if (prefabs.TryGetValue((want.Model, CollectionDetail.Near), out var nearPrefab) && nearPrefab != null)
                    near = Make(person, "Near", nearPrefab, want);
                pairs.Add((far, near));
                Placed.Add(new PlacedFigure(want));
            }
            Apply(lastClose);
            Debug.Log($"PEOPLE: {Placed.Count} civilian figures placed, {Missing.Count} not drawn");
        }

        static Renderer[] Make(Transform parent, string name, GameObject prefab, Want want)
        {
            var go = Instantiate(prefab, parent, false);
            go.name = name;
            // Compose the seeded yaw on top of the prefab root's FBX -90-degree X conversion (#979).
            go.transform.localRotation = Quaternion.Euler(0f, want.Yaw, 0f) * prefab.transform.localRotation;
            var before = Bounds(go);
            go.transform.localScale *= before.size.y > 1e-3f ? FigureHeightM / before.size.y : FigureScale;
            go.transform.position = want.At;
            var after = Bounds(go);
            go.transform.position += new Vector3(want.At.x - after.center.x, want.At.y - after.min.y, want.At.z - after.center.z);
            return go.GetComponentsInChildren<Renderer>(true);
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        public static bool CloseAt(float distanceM, bool? was) =>
            was == true ? distanceM <= CloseExitM : distanceM <= CloseEnterM;

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        internal void Choose(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
            int id = camera.GetInstanceID();
            bool close = CloseAt(TownLandmarks.ViewDistance(camera), closeFor.TryGetValue(id, out var previous) ? previous : (bool?)null);
            closeFor[id] = close; lastClose = close;
            if (close) Ask(CollectionDetail.Near);
            Apply(close);
        }

        void Apply(bool close)
        {
            foreach (var pair in pairs)
            {
                bool near = close && pair.Near.Length > 0;
                foreach (var renderer in pair.Far) renderer.forceRenderingOff = near;
                foreach (var renderer in pair.Near) renderer.forceRenderingOff = !near;
            }
        }
    }
}
