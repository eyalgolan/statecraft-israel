#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>The nine whole-town models of The 1948 Collection as overview landmarks (issue #663; owner ruling of 17 October
    /// 2026: "The nine whole-town models ship (this reverses the 15 Oct 'layout references only' ruling). They are slimmed,
    /// streamed after the first frame, and pass the load gate. The ma'abara and the shikun appear only from their sourced
    /// 1950s dates."). The models are MATKOT's, made with Tripo Studio and offered in #507; which town each
    /// stands at is #507's own mapping (BuildingPreview at 3ef0a7a5): White City at Tel Aviv, the stone city at Jerusalem, the
    /// port city at Haifa, the moshava town at the six moshavot, Nahalal at the moshav, the compound at every kibbutz, the
    /// stone village at every Arab town, the ma'abara at a transit camp and the shikun at a housing project.
    /// Each stands beside its town, never on it: on the nearest free ground clear of every kit footprint (towns, 1948
    /// localities, Bedouin camps: MapScene.KitFootprints), every route corridor, the water, the units and every other
    /// landmark (<see cref="Spot"/>), so the settlement kits (#820) and the close-zoom building models (#700) are untouched.
    /// They draw only at the overview zoom (a camera's view distance from <see cref="OverviewEnterM"/> on, back off below
    /// <see cref="OverviewExitM"/>), where a kit town is a few pixels across; at the gameplay and close zooms nothing changes.
    /// Each model is the strategic zoom's mesh (CollectionDetail.Far, at most 4,500 triangles), asked of
    /// <see cref="CollectionStream"/> when the first landmark needs it, so it is never fetched before the first frame, and
    /// the spots are worked out only once a model has come. It fails open: a model that does not stream logs one warning
    /// and its landmarks are not drawn; the map and the game run on.</summary>
    public sealed class TownLandmarks : MonoBehaviour
    {
        public const string Name = "TownLandmarks";

        /// <summary>The view distances (m) of the switch, with hysteresis: from 110 km on the landmarks draw, below 98 km they
        /// go (owner on #917, 17 October 2026: "Larger, from 120 km"). The game starts at 120 km (CameraRig.Distance), so the
        /// player sees them at once, as at the overview (diorama02 and the minimap's whole-map button:
        /// CameraController.MaxDistance, 250 km); the gameplay view (60 km, gameplay03) and every closer zoom, where the
        /// per-building kits draw, do not. The band is wider than one wheel notch (CameraController.ZoomPerStep, 10% of the
        /// distance).</summary>
        public const float OverviewEnterM = 110000f, OverviewExitM = 98000f;

        /// <summary>A landmark's largest edge on the ground (m): about 41 px across at 250 km and 86 px at 120 km on a 1080 px
        /// screen (CameraRig.Fov 35 degrees). Symbolic, like the kits' scale (owner on #917: "Larger", about 6 km).</summary>
        public const float SizeM = 6000f;

        /// <summary>The ma'abara from the date the first one is reported: Kachansky, "The Ma'abarot" (Yad Ben-Zvi, 1987),
        /// quoting Davar of 23 May 1950 on the first ma'abara, at Kesalon (content/events/maabarot_policy.yaml, with its
        /// sources).</summary>
        public static readonly DateTime MaabaraFrom = new DateTime(1950, 5, 23);

        /// <summary>The shikun from 11 December 1949: the owner's ruling on #917 (17 October 2026, "Use 11 Dec 1949",
        /// https://github.com/eyalgolan/statecraft-israel/pull/917#issuecomment-6240322628), a conservative "completed
        /// immigrant housing existed by this date" landmark resting on one source, the government's review as published by
        /// Project Ben-Yehuda (single-source). [GAP] The exact first shikun project and its day are not known. On the game's
        /// monthly turns (the 15th) it first shows on the 15 December 1949 turn; in play it stands only by a housing project
        /// the player has built, and those can be placed from rules.yaml's map_actions_from (1 January 1950).</summary>
        public static readonly DateTime ShikunFrom = new DateTime(1949, 12, 11);

        public const string TelAviv = "tel-aviv-white-city-v1", Jerusalem = "jerusalem-stone-city-v1", Haifa = "haifa-port-city-v1",
            Moshava = "moshava-town-v1", Nahalal = "nahalal-moshav-v1", Kibbutz = "kibbutz-compound-v2", StoneVillage = "stone-village-v1",
            Maabara = "maabara-camp-v1", Shikun = "shikun-housing-v1";

        /// <summary>By settlement id first (#507's mapping).</summary>
        public static readonly IReadOnlyDictionary<string, string> ById = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tel_aviv"] = TelAviv, ["jerusalem"] = Jerusalem, ["haifa"] = Haifa,
            ["petah_tikva"] = Moshava, ["rishon_lezion"] = Moshava, ["zikhron_yaakov"] = Moshava,
            ["rosh_pinna"] = Moshava, ["rehovot"] = Moshava, ["hadera"] = Moshava,
        };

        /// <summary>Then by settlement type (#507's mapping).</summary>
        public static readonly IReadOnlyDictionary<string, string> ByType = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["kibbutz"] = Kibbutz, ["moshav"] = Nahalal, ["arab_town"] = StoneVillage,
        };

        /// <summary>The player's placements (#507's mapping), each with the first day it may show (the owner's ruling: "only
        /// from their sourced 1950s dates"): the ma'abara from <see cref="MaabaraFrom"/>, the shikun from
        /// <see cref="ShikunFrom"/>. A row with no date (null) is never drawn.</summary>
        public static readonly IReadOnlyDictionary<string, (string Model, DateTime? From)> ByAction =
            new Dictionary<string, (string, DateTime?)>(StringComparer.Ordinal)
            {
                [MapActions.TransitCamp] = (Maabara, MaabaraFrom), [MapActions.Housing] = (Shikun, ShikunFrom),
            };

        /// <summary>The turn about the vertical (degrees) that shows each model's front to a camera looking north: #507's
        /// table (BuildingPreview.Yaw at 3ef0a7a5, tuned there from renders), 200 for a model without a row of its own.</summary>
        public static float YawOf(string model) => model == TelAviv ? 200f : model == StoneVillage ? 40f : 200f;

        /// <summary>How the landmarks ask for a model: the game's stream. A test seam (internal; the tests see it through
        /// InternalsVisibleTo).</summary>
        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = CollectionStream.Request;

        /// <summary>The model for settlement <paramref name="s"/>, or null for none.</summary>
        public static string? ModelFor(Settlement s) =>
            ById.TryGetValue(s.Id, out var m) ? m : ByType.TryGetValue(s.Type, out var t) ? t : null;

        /// <summary>The model for a placement of <paramref name="action"/> as at <paramref name="date"/>, or null: none for that
        /// action, before its sourced date, or no sourced date at all.</summary>
        public static string? ModelFor(string action, DateTime date) =>
            ByAction.TryGetValue(action, out var row) && row.From is { } from && date >= from ? row.Model : null;

        /// <summary>One landmark to draw: its key (the settlement id, or the placement's box name), its model, and the centre
        /// and radius (m, world x/z) of the ground its town or placement holds.</summary>
        public readonly struct Want : IEquatable<Want>
        {
            public readonly string Key, Model; public readonly Vector2 At; public readonly float Radius;
            public Want(string key, string model, Vector2 at, float radius) { Key = key; Model = model; At = at; Radius = radius; }
            public bool Equals(Want o) => Key == o.Key && Model == o.Model && At == o.At && Radius == o.Radius;
            public override bool Equals(object? o) => o is Want w && Equals(w);
            public override int GetHashCode() => (Key, Model, At, Radius).GetHashCode();
        }

        /// <summary>The landmarks the map wants as at <paramref name="date"/>: one per mapped settlement (its kit footprint's
        /// radius, or its marker box's half edge without kits) and one per mapped placement from its date (its hex's
        /// half width), in a fixed order: settlements as the file lists them, then placements as placed.</summary>
        public static List<Want> Wants(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content)
        {
            var radius = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var (_, r, id) in map.KitFootprints) if (!radius.ContainsKey(id)) radius[id] = r;
            var wants = new List<Want>();
            foreach (var s in map.Settlements)
            {
                var model = ModelFor(s);
                if (model == null) continue;
                var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
                float r = radius.TryGetValue(s.Id, out var kr) ? kr : 0.5f * SettlementPlacer.MarkerHeightM(s.Type);
                wants.Add(new Want(s.Id, model, new Vector2(w.x, w.z), r));
            }
            float hexHalf = 500f * map.Hexes.hex_size_km;
            foreach (var p in placements)
            {
                var model = ModelFor(p.Action, date);
                var hex = model == null ? null : content.HexAt(p.Q, p.R);
                if (hex == null) continue;
                var w = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                wants.Add(new Want($"{p.Action}_{p.Q}_{p.R}", model!, new Vector2(w.x, w.z), hexHalf));
            }
            return wants;
        }

        /// <summary>The first spot (world x/z) for a landmark of radius <paramref name="radius"/> beside a town at
        /// <paramref name="town"/> holding <paramref name="townRadius"/>, for which <paramref name="free"/> holds: rings from
        /// the town's edge outwards (one landmark radius apart, <see cref="Rings"/> of them), <see cref="Bearings"/> bearings
        /// each, starting north-east and turning clockwise. Null when none is free.</summary>
        public static Vector2? Spot(Vector2 town, float townRadius, float radius, Func<Vector2, float, bool> free)
        {
            for (int ring = 0; ring < Rings; ring++)
            {
                float d = townRadius + radius * (1.1f + ring);
                for (int k = 0; k < Bearings; k++)
                {
                    float a = (45f - 360f * k / Bearings) * Mathf.Deg2Rad;
                    var p = town + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                    if (free(p, radius)) return p;
                }
            }
            return null;
        }

        public const int Rings = 8, Bearings = 16;

        /// <summary>Whether a disc at <paramref name="p"/> of radius <paramref name="r"/> stays off every disc of
        /// <paramref name="discs"/> (edge to edge).</summary>
        public static bool Clear(Vector2 p, float r, IEnumerable<(Vector2 centre, float radius)> discs)
        {
            foreach (var (c, cr) in discs) { float d = cr + r; if ((p - c).sqrMagnitude < d * d) return false; }
            return true;
        }

        // ---- the layer ----

        MapScene? map;
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> asked = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<int, bool> overviewFor = new Dictionary<int, bool>();
        readonly List<Renderer> renderers = new List<Renderer>();
        List<Want> wants = new List<Want>();
        DateTime date;
        bool dirty;

        /// <summary>The landmarks drawn (key, model, the spot its footprint is centred on, at ground height): a test aid.</summary>
        public readonly List<(string Key, string Model, Vector3 At)> Placed = new List<(string, string, Vector3)>();
        /// <summary>The landmarks wanted but not drawn: their model did not stream, or no spot was free (a test aid).</summary>
        public readonly List<string> Missing = new List<string>();
        /// <summary>For the last camera: whether it drew the landmarks.</summary>
        public bool LastOverview { get; private set; }
        public IReadOnlyList<Want> Wanted => wants;

        /// <summary>Shows the landmarks the map wants as at <paramref name="date"/> (GameRoot calls it on every change, beside
        /// RouteLayer.ShowAt): makes the layer under the terrain the first time, asks the stream for each model the first time
        /// one is wanted, and redraws only when the wanted set changes.</summary>
        public static TownLandmarks? ShowAt(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) return null;
            var t = map.Terrain.transform.Find(Name)?.GetComponent<TownLandmarks>();
            if (t == null)
            {
                var go = new GameObject(Name);
                go.transform.SetParent(map.Terrain.transform, false);
                t = go.AddComponent<TownLandmarks>();
                t.map = map;
            }
            t.Show(Wants(map, date, placements, content), date);
            return t;
        }

        void Show(List<Want> next, DateTime at)
        {
            date = at;
            if (next.SequenceEqual(wants)) return;
            wants = next;
            foreach (var w in wants)
                if (asked.Add(w.Model)) { var model = w.Model; Request(model, CollectionDetail.Far, p => OnModel(model, p)); }
            dirty = true;
            Rebuild();
        }

        void OnModel(string model, GameObject? prefab)
        {
            if (this == null) return;   // the map was rebuilt before the model came; the new map asks again
            prefabs[model] = prefab;
            if (prefab == null)
                Debug.LogWarning($"LANDMARKS: the town model {model} did not stream; its landmarks are not drawn, and the map plays on without them.");
            dirty = true;
            Rebuild();
        }

        /// <summary>Redraws every landmark whose model has come, in the wanted order, each on the first free spot by its town.</summary>
        void Rebuild()
        {
            if (!dirty || map == null || map.Terrain == null) return;
            dirty = false;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            renderers.Clear(); Placed.Clear(); Missing.Clear();
            if (!wants.Any(w => prefabs.TryGetValue(w.Model, out var p) && p != null))
            {
                Missing.AddRange(wants.Where(w => prefabs.ContainsKey(w.Model)).Select(w => w.Key));
                return;
            }
            var ground = PlacementLayer.CampGround(map);
            float r = 0.5f * SizeM;
            var taken = new List<(Vector2 centre, float radius)>();
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            // The other towns' and placements' own ground, for those the kits do not draw (the placements, or no kits at all).
            footprints.AddRange(wants.Select(w => (w.At, w.Radius)));
            bool Free(Vector2 p, float rad)
            {
                if (!Clear(p, rad, footprints) || !Clear(p, rad, taken)) return false;
                if (!ground.Inside(p.x - rad, p.y - rad) || !ground.Inside(p.x + rad, p.y + rad)) return false;
                for (int k = 0; k < 9; k++)
                {
                    var q = k == 0 ? p : p + new Vector2(Mathf.Cos(k * Mathf.PI / 4f), Mathf.Sin(k * Mathf.PI / 4f)) * rad * 0.8f;
                    if (ground.Water(q.x, q.y)) return false;
                }
                return !ground.OnRoute(p, rad) && !ground.OnToken(p, rad) && !ground.OnToken(p, rad, date);
            }
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab)) continue;   // still streaming
                var at = prefab == null ? null : Spot(w.At, w.Radius, r, Free);
                if (at == null) { Missing.Add(w.Key); continue; }
                taken.Add((at.Value, r));
                var go = Instantiate(prefab!, transform, false);
                go.name = w.Key;
                // The yaw goes on top of the prefab's own rotation: every Collection Far prefab's root carries the FBX axis
                // conversion (-90 degrees about X; bakeAxisConversion is off), and setting the yaw alone laid each model on its
                // side (owner, 18 October 2026: the models "seem to be on their side on the map").
                go.transform.localRotation = Quaternion.Euler(0f, YawOf(w.Model), 0f) * prefab!.transform.localRotation;
                var bounds = Bounds(go);
                float edge = Mathf.Max(bounds.size.x, bounds.size.z);
                go.transform.localScale *= edge > 1e-3f ? SizeM / edge : 1f;
                var pos = new Vector3(at.Value.x, ground.Y(at.Value.x, at.Value.y), at.Value.y);
                // The model's base, after the scale, stands on the ground at its spot.
                go.transform.position = pos;
                var b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                foreach (var rr in go.GetComponentsInChildren<Renderer>(true))
                {
                    rr.shadowCastingMode = ShadowCastingMode.On;
                    rr.forceRenderingOff = !LastOverview;
                    renderers.Add(rr);
                }
                Placed.Add((w.Key, w.Model, pos));
            }
            Debug.Log($"LANDMARKS: {Placed.Count} town landmarks placed, {Missing.Count} not drawn");
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>The camera's view distance (m): along its view ray to the map's ground plane (y = 0, where the camera rig's
        /// look-at stands), so it is the rig's zoom (CameraRig.Distance). A camera looking up or level sees no ground: far.</summary>
        public static float ViewDistance(Camera cam)
        {
            var t = cam.transform;
            return t.forward.y < -1e-4f ? t.position.y / -t.forward.y : float.PositiveInfinity;
        }

        /// <summary>Whether a camera at <paramref name="distanceM"/> draws the landmarks, given its last choice
        /// (<paramref name="was"/>, null for none).</summary>
        public static bool OverviewAt(float distanceM, bool? was) =>
            was == true ? distanceM >= OverviewExitM : distanceM >= OverviewEnterM;

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        /// <summary>Before each camera culls: the landmarks on at its overview zoom, off otherwise.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool on = OverviewAt(ViewDistance(cam), overviewFor.TryGetValue(id, out var was) ? was : (bool?)null);
            overviewFor[id] = on;
            LastOverview = on;
            foreach (var r in renderers) if (r != null) r.forceRenderingOff = !on;
        }
    }
}
