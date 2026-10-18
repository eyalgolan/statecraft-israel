#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>The nine whole-town models of The 1948 Collection (issue #663; owner ruling of 17 October 2026: "The nine
    /// whole-town models ship (this reverses the 15 Oct 'layout references only' ruling). They are slimmed, streamed after
    /// the first frame, and pass the load gate. The ma'abara and the shikun appear only from their sourced 1950s dates.").
    /// The models are MATKOT's, made with Tripo Studio and offered in #507; which town each stands at is #507's own mapping
    /// (BuildingPreview at 3ef0a7a5): White City at Tel Aviv, the stone city at Jerusalem, the port city at Haifa, the
    /// moshava town at the six moshavot, Nahalal at the moshav, the compound at every kibbutz, the stone village at every
    /// Arab town, the ma'abara at a transit camp and the shikun at a housing project.
    /// Owner ruling of 18 October 2026 (recorded on #917, #663, #659 and #700): the towns are MATKOT's models, "Whole model
    /// far, buildings near". Far off, a mapped town's whole-town model stands ON its town, on its core (<see cref="CoreOf"/>:
    /// where most of its building models stand, inside its kit footprint, clear of the next town and of the sea), as wide as
    /// that core, tilted to its ground and seated so it never floats (<see cref="SeatOn"/>).
    /// Up close, where #700's building models draw the same town (TownModelLayer.CloseAt, with its hysteresis), the
    /// whole-town model hides (<see cref="Choose"/>). The town's procedural houses never draw, at any zoom: its kit mesh gives
    /// way to its close mesh (the kit without its buildings; its mosques, churches, walls and port stay, as #700 keeps them)
    /// for good (TownModelLayer.Suppress). A placement's landmark (the ma'abara, the shikun) still stands beside its camp or
    /// housing project, on free ground (<see cref="Spot"/>), <see cref="SizeM"/> across, and hides up close too.
    /// Each model is the strategic zoom's mesh (CollectionDetail.Far, at most 4,500 triangles), asked of
    /// <see cref="CollectionStream"/> when the first landmark needs it, so it is never fetched before the first frame. It fails
    /// open: a model that does not stream logs one warning, its landmarks are not drawn and its towns keep their kit houses
    /// far off (they are not suppressed); without #700's building models (TownModelLayer.HasModels false) the whole-town
    /// models draw at every zoom, so no mapped town is ever left without buildings.</summary>
    [ExecuteAlways]
    public sealed class TownLandmarks : MonoBehaviour
    {
        public const string Name = "TownLandmarks";

        /// <summary>A placement's landmark's largest edge on the ground (m), beside its camp or housing project: about 41 px
        /// across at 250 km and 86 px at 120 km on a 1080 px screen (CameraRig.Fov 35 degrees; owner on #917: "Larger", about
        /// 6 km). A town's whole-town model is as wide as the town's kit footprint instead.</summary>
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

        /// <summary>One landmark to draw: its key (the settlement id, or the placement's box name), its model, the centre
        /// and radius (m, world x/z) of the ground its town or placement holds, and whether it stands on that ground (a town)
        /// or beside it (a placement).</summary>
        public readonly struct Want : IEquatable<Want>
        {
            public readonly string Key, Model; public readonly Vector2 At; public readonly float Radius; public readonly bool OnTown;
            public Want(string key, string model, Vector2 at, float radius, bool onTown)
            { Key = key; Model = model; At = at; Radius = radius; OnTown = onTown; }
            public bool Equals(Want o) => Key == o.Key && Model == o.Model && At == o.At && Radius == o.Radius && OnTown == o.OnTown;
            public override bool Equals(object? o) => o is Want w && Equals(w);
            public override int GetHashCode() => (Key, Model, At, Radius, OnTown).GetHashCode();
        }

        /// <summary>The landmarks the map wants as at <paramref name="date"/>: one per mapped settlement, on its kit footprint
        /// (its centre and radius; without kits its marker box's half edge), and one per mapped placement from its date (its
        /// hex's half width), in a fixed order: settlements as the file lists them, then placements as placed.</summary>
        public static List<Want> Wants(MapScene map, DateTime date, IEnumerable<Placement> placements, Content content)
        {
            var footprint = new Dictionary<string, (Vector2 centre, float radius)>(StringComparer.Ordinal);
            foreach (var (c, r, id) in map.KitFootprints) if (!footprint.ContainsKey(id)) footprint[id] = (c, r);
            var wants = new List<Want>();
            foreach (var s in map.Settlements)
            {
                var model = ModelFor(s);
                if (model == null) continue;
                var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
                var (at, r) = footprint.TryGetValue(s.Id, out var f) ? f : (new Vector2(w.x, w.z), 0.5f * SettlementPlacer.MarkerHeightM(s.Type));
                wants.Add(new Want(s.Id, model, at, r, onTown: true));
            }
            float hexHalf = 500f * map.Hexes.hex_size_km;
            foreach (var p in placements)
            {
                var model = ModelFor(p.Action, date);
                var hex = model == null ? null : content.HexAt(p.Q, p.R);
                if (hex == null) continue;
                var w = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                wants.Add(new Want($"{p.Action}_{p.Q}_{p.R}", model!, new Vector2(w.x, w.z), hexHalf, onTown: false));
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

        /// <summary>The share of a town's building plots (#700's models at the world scale, SettlementKits.ModelScale) its
        /// core holds: the whole-town model is as wide as the disc round them (<see cref="CoreOf"/>).</summary>
        public const float CoreShare = 0.7f;
        /// <summary>The most of the ground under a whole-town model that may be water (a coastal town keeps its shore).</summary>
        public const float MaxWaterShare = 0.2f;
        /// <summary>A whole-town model's half edge stays under this share of the distance to the nearest other game settlement,
        /// so its ground rectangle (its corners reach about 1.41 half edges out) never covers that town.</summary>
        public const float NeighbourShare = 0.65f;

        /// <summary>A town's core (owner, 18 October 2026, through the coordinator: sized "so Tel Aviv doesn't cover Jaffa and
        /// Haifa doesn't reach into the bay"): the centroid of the <see cref="CoreShare"/> of its building plots
        /// (<paramref name="plots"/>) nearest its centre (<paramref name="centre"/>), and the radius that holds that share round
        /// it, no more than its kit footprint (<paramref name="footprint"/>), under <see cref="NeighbourShare"/> of the distance
        /// to the nearest of <paramref name="others"/>, then pulled in (by tenths, to a third of where it started) while more
        /// than <see cref="MaxWaterShare"/> of the disc is water. With fewer than five plots: the centre and the footprint,
        /// under the same two limits.</summary>
        public static (Vector2 centre, float radius) CoreOf(Vector2 centre, float footprint, IReadOnlyList<Vector2> plots, IEnumerable<Vector2> others,
            Func<float, float, bool> water)
        {
            Vector2 c = centre; float r = footprint;
            if (plots.Count >= 5)
            {
                var near = plots.OrderBy(p => (p - centre).sqrMagnitude).Take(Mathf.Max(5, Mathf.CeilToInt(CoreShare * plots.Count))).ToList();
                c = near.Aggregate(Vector2.zero, (a, p) => a + p) / near.Count;
                var d = near.Select(p => (p - c).magnitude).OrderBy(x => x).ToList();
                r = Mathf.Min(footprint, d[d.Count - 1]);
            }
            foreach (var o in others) r = Mathf.Min(r, NeighbourShare * (o - c).magnitude);
            float floor = r / 3f;
            for (int i = 0; i < 12 && r > floor && WaterShare(c, r, water) > MaxWaterShare; i++) r = Mathf.Max(floor, 0.9f * r);
            return (c, r);
        }

        /// <summary>The share of 11 x 11 points in the disc at <paramref name="c"/> of <paramref name="r"/> that are water.</summary>
        public static float WaterShare(Vector2 c, float r, Func<float, float, bool> water)
        {
            int n = 0, wet = 0;
            for (int i = 0; i <= 10; i++)
                for (int k = 0; k <= 10; k++)
                {
                    var q = c + new Vector2(-r + 2f * r * i / 10f, -r + 2f * r * k / 10f);
                    if ((q - c).sqrMagnitude > r * r * 1.0001f) continue;
                    n++; if (water(q.x, q.y)) wet++;
                }
            return n == 0 ? 0f : (float)wet / n;
        }

        /// <summary>The steepest a whole-town model leans with its ground (degrees).</summary>
        public const float MaxTiltDeg = 10f;
        /// <summary>Samples across the footprint's square (per side) for <see cref="SeatOn"/>.</summary>
        public const int SeatSamples = 11;

        /// <summary>Where a whole-town model sits: the centre of its town (world x/z), its largest horizontal edge (m), the
        /// normal of its base plane and that plane's height (m) at the centre.</summary>
        public readonly struct Seat
        {
            public readonly Vector2 Centre; public readonly float Edge; public readonly Vector3 Normal; public readonly float BaseY;
            public Seat(Vector2 centre, float edge, Vector3 normal, float baseY) { Centre = centre; Edge = edge; Normal = normal; BaseY = baseY; }
        }

        /// <summary>The base plane of a model over the disc at <paramref name="centre"/> of <paramref name="radius"/>: the
        /// least-squares plane of the ground heights <paramref name="y"/> at <see cref="SeatSamples"/> squared points in the disc
        /// (water points left out, <paramref name="water"/>), its slope clamped to <see cref="MaxTiltDeg"/>, then lowered to
        /// the lowest of them, so no land point lies below it: the model follows its slope and never floats (the kibbutz cut
        /// into its hill on #917 when it stood level at its centre's height). Returns the plane's normal and its height at the
        /// centre; on flat or all-water ground, level at the centre's height.</summary>
        public static (Vector3 normal, float baseY) SeatOn(Vector2 centre, float radius, Func<float, float, float> y, Func<float, float, bool> water)
        {
            var pts = new List<(float dx, float dz, float h)>();
            for (int i = 0; i < SeatSamples; i++)
                for (int k = 0; k < SeatSamples; k++)
                {
                    float dx = radius * (2f * i / (SeatSamples - 1) - 1f), dz = radius * (2f * k / (SeatSamples - 1) - 1f);
                    if (dx * dx + dz * dz > radius * radius * 1.0001f || water(centre.x + dx, centre.y + dz)) continue;   // the rim kept despite rounding
                    pts.Add((dx, dz, y(centre.x + dx, centre.y + dz)));
                }
            if (pts.Count < 3) return (Vector3.up, y(centre.x, centre.y));
            // Least squares h = a + b dx + c dz (in doubles: the sums of squares of 10 km offsets).
            double n = pts.Count, sx = 0, sz = 0, sh = 0, sxx = 0, szz = 0, sxz = 0, sxh = 0, szh = 0;
            foreach (var (dx, dz, h) in pts) { sx += dx; sz += dz; sh += h; sxx += dx * dx; szz += dz * dz; sxz += dx * dz; sxh += dx * h; szh += dz * h; }
            double mx = sx / n, mz = sz / n, mh = sh / n;
            double cxx = sxx / n - mx * mx, czz = szz / n - mz * mz, cxz = sxz / n - mx * mz, cxh = sxh / n - mx * mh, czh = szh / n - mz * mh;
            double det = cxx * czz - cxz * cxz;
            double b = det > 1e-6 ? (cxh * czz - czh * cxz) / det : 0, c = det > 1e-6 ? (czh * cxx - cxh * cxz) / det : 0;
            double slope = Math.Sqrt(b * b + c * c), max = Math.Tan(MaxTiltDeg * Mathf.Deg2Rad);
            if (slope > max) { b *= max / slope; c *= max / slope; }
            double low = double.MaxValue;
            foreach (var (dx, dz, h) in pts) low = Math.Min(low, h - (b * dx + c * dz));
            return (new Vector3((float)-b, 1f, (float)-c).normalized, (float)low);
        }

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
        /// <summary>Each camera's last choice (by instance id): TownModelLayer.CloseAt's hysteresis.</summary>
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        readonly List<Renderer> renderers = new List<Renderer>();
        List<Want> wants = new List<Want>();
        DateTime date;
        bool dirty;

        /// <summary>The landmarks drawn (key, model, the spot its footprint is centred on, at ground height): a test aid.</summary>
        public readonly List<(string Key, string Model, Vector3 At)> Placed = new List<(string, string, Vector3)>();
        /// <summary>The landmarks wanted but not drawn: their model did not stream, or no spot was free (a test aid).</summary>
        public readonly List<string> Missing = new List<string>();
        /// <summary>Where each town's whole-town model sits, by settlement id (a test aid).</summary>
        public readonly Dictionary<string, Seat> Seats = new Dictionary<string, Seat>(StringComparer.Ordinal);
        /// <summary>For the last camera: whether it hid the landmarks (up close, where #700's building models draw).</summary>
        public bool LastHidden { get; private set; }
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
            renderers.Clear(); Placed.Clear(); Missing.Clear(); Seats.Clear();
            if (!wants.Any(w => prefabs.TryGetValue(w.Model, out var p) && p != null))
            {
                Missing.AddRange(wants.Where(w => prefabs.ContainsKey(w.Model)).Select(w => w.Key));
                Layer()?.Suppress(Array.Empty<string>());
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
            var layer = Layer();
            // Every game settlement's centre (its kit footprint's), for the cores' neighbour limit.
            var gameIds = new HashSet<string>(map.Settlements.Select(x => x.Id), StringComparer.Ordinal);
            var townCentres = map.KitFootprints.Where(f => gameIds.Contains(f.id)).Select(f => (c: f.centre, id: f.id)).ToList();
            foreach (var w in wants)
            {
                if (!prefabs.TryGetValue(w.Model, out var prefab)) continue;   // still streaming
                if (prefab == null) { Missing.Add(w.Key); continue; }
                Vector2 at; float edge, baseY; Vector3 normal = Vector3.up;
                if (w.OnTown)
                {
                    // On the town's core, not its whole kit footprint (Tel Aviv's held Jaffa, Haifa's the bay).
                    var plots = layer == null ? (IReadOnlyList<Vector2>)Array.Empty<Vector2>()
                        : layer.InstancesOf(w.Key).Select(m => m.Plot).Distinct().ToList();
                    var others = wants.Where(o => o.OnTown && o.Key != w.Key).Select(o => o.At).Concat(townCentres.Where(o => o.id != w.Key).Select(o => o.c));
                    var (core, rad) = CoreOf(w.At, w.Radius, plots, others, ground.Water);
                    at = core; edge = 2f * rad;
                    (normal, baseY) = SeatOn(at, rad, ground.Y, ground.Water);
                }
                else
                {
                    var spot = Spot(w.At, w.Radius, r, Free);
                    if (spot == null) { Missing.Add(w.Key); continue; }
                    at = spot.Value; edge = SizeM; baseY = ground.Y(at.x, at.y);
                    taken.Add((at, r));
                }
                var go = Instantiate(prefab, transform, false);
                go.name = w.Key;
                // The yaw goes on top of the prefab's own rotation: every Collection Far prefab's root carries the FBX axis
                // conversion (-90 degrees about X; bakeAxisConversion is off), and setting the yaw alone laid each model on its
                // side (owner, 18 October 2026: the models "seem to be on their side on the map"; #979).
                go.transform.localRotation = Quaternion.Euler(0f, YawOf(w.Model), 0f) * prefab.transform.localRotation;
                var bounds = Bounds(go);
                float size = Mathf.Max(bounds.size.x, bounds.size.z);
                go.transform.localScale *= size > 1e-3f ? edge / size : 1f;
                // The model's base, after the scale, stands on its seat, centred on its town or spot; then it leans with the
                // ground about that point (level for a placement).
                var pos = new Vector3(at.x, baseY, at.y);
                go.transform.position = pos;
                var b = Bounds(go);
                go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
                var tilt = Quaternion.FromToRotation(Vector3.up, normal);
                go.transform.SetPositionAndRotation(pos + tilt * (go.transform.position - pos), tilt * go.transform.rotation);
                foreach (var rr in go.GetComponentsInChildren<Renderer>(true))
                {
                    rr.shadowCastingMode = ShadowCastingMode.On;
                    rr.forceRenderingOff = LastHidden;
                    renderers.Add(rr);
                }
                if (w.OnTown) Seats[w.Key] = new Seat(at, edge, normal, baseY);
                Placed.Add((w.Key, w.Model, pos));
            }
            // The towns that have their whole-town model never draw their kit houses (owner, 18 October 2026: no procedural
            // houses in those towns at any zoom); a town whose model did not stream keeps them.
            if (layer != null) layer.Suppress(Seats.Keys);
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

        /// <summary>The camera's view distance (m), as TownModelLayer measures it (the rig's zoom); the people, wildlife and
        /// road vehicle layers read it from here.</summary>
        public static float ViewDistance(Camera cam) => Kits.TownModelLayer.ViewDistance(cam);

        /// <summary>#700's building layer of this map (null without kits).</summary>
        Kits.TownModelLayer? Layer() => map == null ? null : map.GetComponentInChildren<Kits.TownModelLayer>(true);

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        /// <summary>Before each camera culls: the landmarks hide where #700's building models draw the towns
        /// (TownModelLayer.CloseAt at the camera's view distance, with its hysteresis), and draw everywhere else. Without the
        /// building models (no kits, or TownModelLayer.HasModels false) they draw at every zoom.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            var layer = Layer();
            int id = cam.GetInstanceID();
            bool close = Kits.TownModelLayer.CloseAt(Kits.TownModelLayer.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close;
            bool hide = close && layer != null && layer.HasModels;
            LastHidden = hide;
            foreach (var r in renderers) if (r != null) r.forceRenderingOff = hide;
        }
    }
}
