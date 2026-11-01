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
    /// fail open. b21 remains catalogued but unplaced until an artisan-workshop location is sourced.</summary>
    [ExecuteAlways]
    public sealed class SpecialBuildingModels : MonoBehaviour
    {
        public const string Name = "SpecialBuildingModels";
        public const string Shikun = "b11-shikun-housing-v2", Tegart = "b12-tegart-fort-v2",
            Barracks = "b18-military-barracks-v2", Workshop = "b21-artisan-workshop-v2",
            Factory = "b22-industrial-factory-v2", SyrianR35 = CollectionCatalog.SyrianR35;

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
            _ => throw new ArgumentException($"'{model}' is not one of #707's models", nameof(model)),
        };

        /// <summary>The model's long side on the map (m), seen from above.</summary>
        public static float SizeOf(string model) => RealLengthM(model) * WorldScale;

        /// <summary>The disc a placed model keeps clear (m): its half diagonal, as if square.</summary>
        public static float ClearanceOf(string model) => 0.71f * SizeOf(model);

        // Design value: the bounded deterministic search around each sourced point.
        public const int SpotAttempts = 24;

        public readonly struct Want
        {
            public readonly string Key, Model, Source; public readonly DateTime From; public readonly double Lon, Lat;
            public Want(string key, string model, DateTime from, double lon, double lat, string source)
            { Key = key; Model = model; From = from; Lon = lon; Lat = lat; Source = source; }
        }

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
        };

        public static IReadOnlyList<Want> Placements(DateTime date) => All.Where(x => date >= x.From).ToList();

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
        IEnumerable<(string key, Vector3 at, float radius)> Anchors()
        {
            if (map == null) yield break;
            foreach (var w in wants) yield return (w.Key, map.Frame.ToWorld(w.Lon, w.Lat, 0), ClearanceOf(w.Model));
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
            // v0.3.1 Show requested every wanted special as Near immediately; only its renderers were close-camera gated.
            residency.Replace(wants.Select(w => (w.Model, CollectionDetail.Near)));
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
            renderers.Clear(); Placed.Clear();
            originals.Clear();
            if (map != null) Statecraft.Game.Map.Units.UnitShowcase.HideTrees(map, Name, Array.Empty<(Vector2, float)>());
        }
        bool residencyDirty;
        void ObserveResidency(Camera camera, bool inRender)
        {
            var before = residencyView!.Keys;
            residencyView.Observe(camera, Anchors(), Time.frameCount);
            if (!before.SetEquals(residencyView.Keys))
            { if (Application.isPlaying) residencyDirty = true; else RefreshResidencyCore(false); }
        }
        void LateUpdate()
        {
            if (residencyView != null && residencyView.Prune(Time.frameCount)) residencyDirty = true;
            if (residencyDirty) { residencyDirty = false; RefreshResidencyCore(false); }
        }
        void OnDestroy() { residency?.Dispose(); residency = null; }

        MapScene? map; DateTime date; bool shown;
        public readonly List<(string Key, string Model, Vector3 At)> Placed = new List<(string, string, Vector3)>();
        public bool FailedOpen { get; private set; }
        public bool LastClose { get; private set; }
        readonly Dictionary<string, List<GameObject>> originals = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);

        public static SpecialBuildingModels? ShowAt(MapScene map, DateTime date)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<SpecialBuildingModels>();
            if (layer == null)
            {
                var go = new GameObject(Name); go.transform.SetParent(map.Terrain.transform, false);
                layer = go.AddComponent<SpecialBuildingModels>(); layer.map = map;
            }
            if (layer.residencyView == null) layer.ConfigureResidency(map.Residency.Special, null);
            // GameRoot calls this on every refresh: rebuild only when the game date changes (the dated list and the dated
            // tokens' clearance both follow it).
            if (layer.shown && layer.date == date) return layer;
            layer.shown = true; layer.date = date; layer.Show(Placements(date)); return layer;
        }

        internal void ShowForTest(IReadOnlyList<Want> wants) => Show(wants);

        void Show(IReadOnlyList<Want> wants)
        {
            this.wants = wants;
            if (residencyView != null) { RefreshResidency(); return; }
            foreach (var w in wants)
                if (asked.Add(w.Model)) { string model = w.Model; Request(model, CollectionDetail.Near, p => { if (this == null) return; prefabs[model] = p; if (p == null) { FailedOpen = true; Debug.LogWarning($"BUILDINGS: {model} did not stream; it stays off the map."); } Rebuild(); }); }
            Rebuild();
        }

        void Rebuild()
        {
            CollectionResidencyMetrics.MarkDirty();
            if (map == null || map.Terrain == null) return;
            ClearVisuals();
            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var taken = new List<(Vector2 centre, float radius)>();
            // The civilian figures (#666, shown first by GameRoot) and the wildlife (#670) keep their ground too.
            var people = map.Terrain.transform.Find(CivilianPeople.Name)?.GetComponent<CivilianPeople>();
            if (people != null) foreach (var p in people.Wanted) taken.Add((new Vector2(p.At.x, p.At.z), CivilianPeople.FigureClearanceM));
            foreach (var a in WildlifeLayer.Wants(map)) taken.Add((a.At, 0.5f * WildlifeLayer.SizeM));
            bool Free(Vector2 p, float r) => TownLandmarks.Clear(p, r, taken) && TownLandmarks.Clear(p, r, footprints)
                && ground.Inside(p.x - r, p.y - r) && ground.Inside(p.x + r, p.y + r)
                && !ground.Water(p.x, p.y) && !ground.OnRoute(p, r) && !ground.OnToken(p, r, date);
            var holes = new List<(Vector2 centre, float radius)>();
            var kits = map.transform.Find(Kits.KitLayer.RootName);
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab) || prefab == null) continue;
                var anchor3 = map.Frame.ToWorld(w.Lon, w.Lat, 0); var anchor = new Vector2(anchor3.x, anchor3.z);
                float clearance = ClearanceOf(w.Model);
                Vector2? at = null;
                if (w.Key == "degania_r35" && kits != null)
                {
                    var village = KitBounds(kits, "degania_alef");
                    var footprint = map.KitFootprints.FirstOrDefault(f => f.id == "degania_alef");
                    if (village is Bounds drawn && footprint.id != null)   // no kit or no footprint: the seeded spot below
                        at = RouteFacingEdgeSpot(footprint.centre, drawn, clearance, map.KitRouteCorridors, Free);
                }
                if (at == null)
                {
                    if (w.Key == "degania_r35")
                        Debug.LogWarning("BUILDINGS: route-facing spot unavailable for degania_r35; falling back to the seeded spot.");
                    at = SeededSpot(w.Key, anchor, clearance, Free, Inside(anchor, footprints));
                }
                if (at == null) { Debug.LogWarning($"BUILDINGS: no clear ground near {w.Key}; its {w.Model} is not drawn."); continue; }
                taken.Add((at.Value, clearance));
                holes.Add((at.Value, clearance));
                var go = Instantiate(prefab, transform, false); go.name = w.Key;
                if (!originals.TryGetValue(w.Model, out var owned)) originals[w.Model] = owned = new List<GameObject>();
                owned.Add(go);
                go.transform.localRotation = Rotation(prefab.transform.localRotation, 0f);
                var b = Bounds(go); float edge = Mathf.Max(b.size.x, b.size.z);
                go.transform.localScale *= edge > 1e-3f ? SizeOf(w.Model) / edge : 1f;
                var pos = new Vector3(at.Value.x, ground.Y(at.Value.x, at.Value.y), at.Value.y);
                go.transform.position = pos; b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.forceRenderingOff = !LastClose; renderers.Add(r); }
                Placed.Add((w.Key, w.Model, pos));
            }
            // No kit tree stands on a building: the trees whose crowns reach its ground are left out at draw time, as under
            // the unit figures (Kits.VegetationLayer.Hide).
            Units.UnitShowcase.HideTrees(map, Name, holes);
            if (prefabs.Count > 0) Debug.Log($"BUILDINGS: {Placed.Count} of {wants.Count} special buildings placed ({string.Join(", ", Placed.Select(p => p.Key))})");
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
