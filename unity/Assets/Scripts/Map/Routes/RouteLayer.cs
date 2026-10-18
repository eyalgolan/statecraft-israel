#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>The 1948 railways and roads on the map (issue #35): one ribbon mesh per look (rail; roads and the Burma
    /// Road's track), draped on the terrain along the traced lines of route_paths_1948.yaml, drawn with the editor-made
    /// Statecraft/Route materials in Resources (no runtime Shader.Find, no new serialized field on MapScene). Each route
    /// shows only while the game date is inside its window (RoutePath.From/To; review C1: the Burma Road from 14 June
    /// 1948): routes are grouped by window, one ribbon pair per group, and <see cref="ShowAt"/> switches the groups. A
    /// steam train runs on the Jaffa–Jerusalem line: in normal play only from the sourced date regular service resumed
    /// (<see cref="ServiceTrain"/>, issue #668), and as the promo's showcase in screenshots and `?train` (<see cref="ModeFor"/>). Widths are symbolic, measured on the
    /// promo's gameplay frame like the settlements and units (docs/routes.md): a road's pale core about 8 px and the railway
    /// about 16 px wide at 1920 px (the soft edges take about a sixth of the railway's width, so its bed is drawn 680 m), so they read at the gameplay zoom as the promo draws them. Registers the "routes" (ribbons)
    /// and "train" visual toggles, so the frame profile prices each. Pure geometry from the data: the same file gives the
    /// same meshes, vertex for vertex.</summary>
    public static class RouteLayer
    {
        public const string RootName = "Routes", RibbonsName = "RouteRibbons", RoutesToggle = "routes", TrainToggle = "train";
        public const string RailMaterialResource = "Routes/Rail", RoadMaterialResource = "Routes/Road";
        public const string Folder = "Assets/Resources/Routes";

        /// <summary>Full ribbon widths (m), soft edges included (the shader fades the outer 16-30%). A road is 420 m: its pale
        /// core, inside the verges, is about 280 m, about 8 px at the gameplay zoom, as promo 03's dirt tracks (at 300 m the
        /// core was about 4 px, a thin line between dark verges); still well under the railway's bed.</summary>
        public const float RailWidthM = 680f, RoadWidthM = 420f, TrackWidthM = 200f;
        /// <summary>Spacing (m) of the ribbon's cross-sections along its line: under the heightmap's 211 m N-S spacing, so
        /// every terrain row gets a section; about 3 px at the gameplay zoom.</summary>
        public const float StepM = 120f;
        /// <summary>Vertices across a ribbon: its edges, its centre and the quarter lines, each on its own ground, so a
        /// ribbon on a slope follows the slope instead of standing out flat.</summary>
        public const int Across = 5;
        /// <summary>Height above the highest ground sampled around each vertex; the shader's depth offset does the rest.</summary>
        public const float ClearanceM = 20f;

        /// <summary>The train's route: the Jaffa–Jerusalem railway. The showcase runs from its Jaffa end to Lydda junction
        /// (<see cref="TrainTurnAt"/>), the stretch the promo's train runs (promo/final/03): ambience for the promo frames, not
        /// a timetable. No regular trains ran on the line from the British withdrawal in May 1948 until 2 March 1950, after
        /// Lydda was taken in July 1948 and the 1949 armistice (Israel Railways; routes_1948.yaml), so normal play runs its
        /// train only from then, on the stretches of the route's service that run (Lod to Jerusalem, the 1950 service's
        /// stretch of this line; Tel Aviv South to Lod once a person signs it off): <see cref="ServiceTrain"/>, maintainer
        /// decision on #35, 16 October 2026.</summary>
        public const string TrainRoute = "jaffa_jerusalem_railway";
        public static readonly (double lon, double lat) TrainTurnAt = (34.87556, 31.94583);   // Lod railway station

        /// <summary>The date the map shows before the game tells it one (editor renders, screenshots before GAME OK): the
        /// game's start, 15 May 1948.</summary>
        public static readonly DateTime DefaultDate = new DateTime(1948, 5, 15);

        /// <summary>None: no train. Still and Moving: the promo's showcase train. Service: normal play's (issue #668), from
        /// the route's sourced service date only.</summary>
        public enum TrainMode { None, Still, Moving, Service }

        /// <summary>Which train (review I1; issue #668): editor renders and screenshots (`?shot` / `-shot`) show the promo's
        /// still train; `?train` or `?units` (desktop `-train` / `-units`) asks for the moving showcase; normal play runs the
        /// service train, from the date regular service resumed (<see cref="ServiceTrain"/>).</summary>
        public static TrainMode ModeFor(bool playing, bool shot, bool requested) =>
            !playing || shot ? TrainMode.Still : requested ? TrainMode.Moving : TrainMode.Service;

        public static bool TrainRequested(string? url, string[] args, bool webgl) =>
            webgl ? Boot.UrlFlags.Has(url, "train") || Boot.UrlFlags.Has(url, "units") : Array.IndexOf(args, "-train") >= 0 || Array.IndexOf(args, "-units") >= 0;

        public static TrainMode ModeForThisRun() => ModeFor(Application.isPlaying, VisualShots.Requested(),
            TrainRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer));

        /// <summary>Shows the route groups whose window covers <paramref name="date"/> and hides the rest (the game calls it
        /// on every state change; cheap: a few SetActive calls), and remembers the date on the map, so a rebuild of the map
        /// (MapScene.Build) draws the routes as at the game's date at once. A map without routes is left alone.</summary>
        public static void ShowAt(MapScene map, DateTime date)
        {
            if (map == null) return;
            map.RouteDate = date;                   // a later rebuild of this map starts at the game's date (re-review nit 2)
            if (map.Terrain == null) return;
            var service = map.Terrain.transform.Find($"{RootName}/{ServiceTrain.Name}");
            if (service != null) service.GetComponent<ServiceTrain>().ShowAt(date);   // from the service date on (issue #668)
            var vehicles = map.Terrain.transform.Find($"{RootName}/{RoadVehicles.Name}");
            if (vehicles != null) vehicles.GetComponent<RoadVehicles>().ShowAt(date);
            var ribbons = map.Terrain.transform.Find($"{RootName}/{RibbonsName}");
            if (ribbons == null) return;
            foreach (var g in ribbons.GetComponentsInChildren<RouteGroup>(true)) g.gameObject.SetActive(g.Window.Covers(date));
        }

        /// <summary>Clearance (m) beyond a ribbon's drawn half-width that kit buildings and trees keep from it, so a lot's or
        /// crown's edge never touches the soft edge of the road or the ballast.</summary>
        public const float CorridorMarginM = 100f;
        /// <summary>The roads' clearance (m) beyond their drawn half-width: 40 m, so their corridor stays the 250 m half-width
        /// it had when roads were 300 m wide. A wider corridor pushed town kits off the roads that run through the towns
        /// (Jerusalem's Haram al-Sharif, Beersheba's houses: TownCensusTests). The road's outer 22% is its soft edge, so a
        /// lot or crown still keeps about 130 m from the visible dirt.</summary>
        public const float RoadCorridorMarginM = 40f;

        /// <summary>Every route's traced line (world x, z; <see cref="World"/>, not cut as <see cref="Drawn"/> is) with the half-width kits must keep clear of: half the drawn width plus
        /// <see cref="CorridorMarginM"/> (<see cref="RoadCorridorMarginM"/> for roads). All routes, whatever their window: the
        /// kits are built once, not per date, so a route that appears later (the Burma Road) has its corridor from the start.</summary>
        public static List<(List<Vector2> line, float halfWidth)> Corridors(GeoFrame frame, IEnumerable<RoutePath> routes) =>
            routes.Select(r => (World(frame, r), WidthOf(r.Kind) / 2f + (r.Kind == RoutePaths.Road ? RoadCorridorMarginM : CorridorMarginM))).ToList();

        public static float WidthOf(string kind) => kind switch
        {
            RoutePaths.Rail => RailWidthM,
            RoutePaths.Road => RoadWidthM,
            RoutePaths.Track => TrackWidthM,
            _ => throw new ArgumentException($"unknown route kind '{kind}'", nameof(kind)),
        };

        /// <summary>One ribbon mesh Build draws: the routes of one display window in one look ("Rail", or "Roads" for the
        /// roads and tracks), in the file's order.</summary>
        public sealed class RibbonPlan
        {
            public RouteWindow Window;
            public string Look = "";
            public List<RoutePath> Routes = new List<RoutePath>();
            public bool RailGrade => Look == "Rail";
            public string MeshName => $"Route{Look} {Window}";
        }

        /// <summary>The ribbons Build draws, in its order: the windows in the order they open, and in each the rail, then the
        /// roads, where the window has any. The bake (RouteRibbonFile) stores its meshes in this order. A change to it, to
        /// Derive or to what Derive calls needs a re-bake (RouteRibbonBake.Bake) and a bump of RouteRibbonFile.Version
        /// (tools/tests/test_route_ribbon_bake.py fails until then, issue #535).</summary>
        public static List<RibbonPlan> Plan(IReadOnlyList<RoutePath> routes)
        {
            var plans = new List<RibbonPlan>();
            foreach (var group in routes.GroupBy(RoutePaths.WindowOf).OrderBy(g => g.Key.From).ThenBy(g => g.Key.To ?? DateTime.MaxValue))
                foreach (var (kinds, look) in new[] { (new[] { RoutePaths.Rail }, "Rail"), (new[] { RoutePaths.Road, RoutePaths.Track }, "Roads") })
                {
                    var members = group.Where(r => kinds.Contains(r.Kind)).ToList();
                    if (members.Count > 0) plans.Add(new RibbonPlan { Window = group.Key, Look = look, Routes = members });
                }
            return plans;
        }

        /// <summary>A plan's ribbon, derived from the terrain (what the bake stores). A change to it or to what it calls
        /// (World, Geometry and their helpers) needs a re-bake and a bump of RouteRibbonFile.Version (see Plan).</summary>
        public static RibbonGeometry Derive(RibbonPlan plan, GeoFrame frame, TerrainData data, Vector3 origin) =>
            Geometry(plan.Routes.Select(r => (r.Id, Drawn(frame, r), WidthOf(r.Kind))), data, origin, plan.RailGrade);

        /// <summary>Builds the routes under <paramref name="terrain"/> (so a map rebuild destroys them): one group per display
        /// window (its rail and road ribbons), shown as at <paramref name="date"/>, and, when <paramref name="train"/> is not
        /// None and the train's route is in <paramref name="routes"/>, the train (still: it stands at its still position, so
        /// screenshots and editor renders repeat exactly). Meshes, the train's palette and its material copy are owned by
        /// the root's own MapRuntimeAssets, which also unregisters the toggles. Returns the root.
        /// With <paramref name="baked"/> (the inputs the map read), the ribbons are placed from the bake
        /// (RouteRibbonFile.Resource, Editor RouteRibbonBake) when it was made from the same inputs; a missing or stale bake
        /// is refused with a warning and the ribbons derived at load, the same meshes, only slower (issue #321). Without it
        /// they are derived.</summary>
        public static GameObject Build(Terrain terrain, GeoFrame frame, IReadOnlyList<RoutePath> routes, TrainMode train, DateTime date,
            RouteRibbonFile.Source? baked = null, MapScene? map = null)
        {
            if (terrain == null) throw new ArgumentNullException(nameof(terrain));
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            var rail = Resources.Load<Material>(RailMaterialResource)
                ?? throw new InvalidOperationException($"Resources/{RailMaterialResource}.mat is missing (run RouteMaterialAssets.Create)");
            var road = Resources.Load<Material>(RoadMaterialResource)
                ?? throw new InvalidOperationException($"Resources/{RoadMaterialResource}.mat is missing (run RouteMaterialAssets.Create)");

            var root = new GameObject(RootName);
            root.transform.SetParent(terrain.transform, false);
            root.transform.position = Vector3.zero;           // children are in world space
            var assets = root.AddComponent<MapRuntimeAssets>();
            var ribbons = new GameObject(RibbonsName);
            ribbons.transform.SetParent(root.transform, false);
            var data = terrain.terrainData; var origin = terrain.transform.position;

            // One group per display window, in the order the windows open (deterministic names and draw order).
            var plans = Plan(routes);
            var geometry = Geometries(plans, frame, data, origin, baked);
            GameObject? holder = null;
            for (int i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                if (holder == null || !plans[i - 1].Window.Equals(plan.Window))
                {
                    holder = new GameObject($"Window {plan.Window}");
                    holder.transform.SetParent(ribbons.transform, false);
                    holder.AddComponent<RouteGroup>().Window = plan.Window;
                    holder.SetActive(plan.Window.Covers(date));
                }
                var mesh = assets.Own(geometry[i].ToMesh(plan.MeshName));
                mesh.UploadMeshData(true);
                var go = new GameObject(plan.Look);
                go.transform.SetParent(holder.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = plan.RailGrade ? rail : road;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
            assets.OnTeardown(VisualToggles.Register(RoutesToggle, on => { if (ribbons != null) ribbons.SetActive(on); }));

            if (map != null) RoadVehicles.Create(root.transform, map, routes, date);

            var trainRoute = routes.FirstOrDefault(r => r.Id == TrainRoute);
            if (trainRoute != null && train == TrainMode.Service)
            {
                // Normal play: nothing is built or fetched before the route's service date (issue #668). No service (the YAML
                // fallback carries none, or it was refused with a warning): no train.
                if (trainRoute.Service != null)
                {
                    var service = ServiceTrain.Create(root.transform, assets, frame, trainRoute, data, origin, date);
                    assets.OnTeardown(VisualToggles.Register(TrainToggle, on => { if (service != null) service.gameObject.SetActive(on); }));
                }
            }
            else if (trainRoute != null && train != TrainMode.None)
            {
                var go = RouteTrain.Spawn(root.transform, assets, World(frame, trainRoute), data, origin,
                    frame.ToWorld(TrainTurnAt.lon, TrainTurnAt.lat, 0), frame.ToWorld(RouteTrain.StillAt.lon, RouteTrain.StillAt.lat, 0),
                    train == TrainMode.Still);
                assets.OnTeardown(VisualToggles.Register(TrainToggle, on => { if (go != null) go.SetActive(on); }));
            }
            return root;
        }

        /// <summary>The plans' ribbons: from the bake when <paramref name="baked"/> is given and the bake is for those inputs,
        /// else derived.</summary>
        static List<RibbonGeometry> Geometries(List<RibbonPlan> plans, GeoFrame frame, TerrainData data, Vector3 origin, RouteRibbonFile.Source? baked)
        {
            if (baked is { } source && plans.Count > 0)
            {
                var asset = Resources.Load<TextAsset>(RouteRibbonFile.Resource);
                string why = "missing";
                if (asset != null)
                {
                    bool ok = RouteRibbonFile.TryRead(asset.bytes, source, data.size, origin, plans, out var read, out why);
                    if (!Application.isEditor) Resources.UnloadAsset(asset);   // its bytes were copied; drop the native copy
                    if (ok) return read;
                }
                Debug.LogWarning($"ROUTES {RouteRibbonFile.AssetPath}: {why}; deriving the route ribbons at load");
            }
            return plans.ConvertAll(p => Derive(p, frame, data, origin));
        }

        /// <summary>A route's points in world metres (x east, z north).</summary>
        public static List<Vector2> World(GeoFrame frame, RoutePath route)
        {
            var w = new List<Vector2>(route.Points.Count);
            foreach (var p in route.Points)
            {
                var v = frame.ToWorld(p[0], p[1], 0);
                var q = new Vector2(v.x, v.z);
                if (w.Count == 0 || (q - w[w.Count - 1]).sqrMagnitude > 1e-4f) w.Add(q);
            }
            return w;
        }

        /// <summary>A route's line as the map draws its ribbon: <see cref="World"/>, with a road's or a railway's ends
        /// cut at the walls of Jerusalem's symbolic Old City (#319). The walled square (SettlementKits.OldCityWalls) is 4 km
        /// across, about 4 times the real Old City, so the real 1948 roads that start outside the real walls, by New Gate and
        /// Jaffa Gate, and the Jaffa-Jerusalem railway's station, also outside them, fall inside it. Cut there, each line
        /// ends on the wall line, where the walls leave it a gap (the railway on the owner's ruling, "Cut it at the walls"),
        /// and the traced data keeps the real lines. Only a line's ends are cut: one that ran through the square would keep
        /// its middle, and RouteTests.TheRoadsAndTheRailwayStopAtTheOldCityWalls fails on it. Tracks are not cut (none comes
        /// near). The corridors (<see cref="Corridors"/>) keep the traced lines, so the kits and the units stand where
        /// they did: inside the walls the roads' clear ways read as the streets from the gates.</summary>
        public static List<Vector2> Drawn(GeoFrame frame, RoutePath route)
        {
            var line = World(frame, route);
            if (route.Kind == RoutePaths.Track) return line;
            var (centre, half) = Kits.SettlementKits.OldCityWalls(frame);
            return TrimEndsInside(line, centre, half);
        }

        /// <summary><paramref name="line"/> without its leading and trailing runs inside the square (centre
        /// <paramref name="c"/>, half side <paramref name="half"/>), each cut where it crosses the square's edge. A line wholly
        /// inside comes back empty, one wholly outside unchanged.</summary>
        public static List<Vector2> TrimEndsInside(List<Vector2> line, Vector2 c, float half)
        {
            bool Inside(Vector2 p) => Mathf.Abs(p.x - c.x) < half && Mathf.Abs(p.y - c.y) < half;
            int first = line.FindIndex(p => !Inside(p)), last = line.FindLastIndex(p => !Inside(p));
            if (first < 0) return new List<Vector2>();
            if (first == 0 && last == line.Count - 1) return line;
            var cut = new List<Vector2>(last - first + 3);
            void Add(Vector2 q) { if (cut.Count == 0 || (q - cut[cut.Count - 1]).sqrMagnitude > 1e-4f) cut.Add(q); }
            if (first > 0) Add(Exit(line[first - 1], line[first], c, half));
            for (int i = first; i <= last; i++) Add(line[i]);
            if (last < line.Count - 1) Add(Exit(line[last + 1], line[last], c, half));
            return cut;
        }

        /// <summary>Where the segment from <paramref name="a"/> (inside the square) to <paramref name="b"/> (outside) crosses
        /// its edge.</summary>
        static Vector2 Exit(Vector2 a, Vector2 b, Vector2 c, float half)
        {
            var d = b - a; float t = 1f;
            if (d.x != 0f) t = Mathf.Min(t, ((d.x > 0f ? c.x + half : c.x - half) - a.x) / d.x);
            if (d.y != 0f) t = Mathf.Min(t, ((d.y > 0f ? c.y + half : c.y - half) - a.y) / d.y);
            return a + d * Mathf.Max(0f, t);
        }

        /// <summary>Points every <paramref name="step"/> metres along a polyline, its last point included, and the
        /// distance along it of each.</summary>
        public static (List<Vector2> points, List<float> along) Resample(IReadOnlyList<Vector2> line, float step)
        {
            var pts = new List<Vector2>(); var along = new List<float>();
            if (line.Count == 0) return (pts, along);
            pts.Add(line[0]); along.Add(0f);
            float s = 0f, next = step;
            for (int i = 1; i < line.Count; i++)
            {
                var a = line[i - 1]; var b = line[i]; float len = (b - a).magnitude;
                while (next <= s + len && len > 0f)
                {
                    pts.Add(Vector2.Lerp(a, b, (next - s) / len)); along.Add(next);
                    next += step;
                }
                s += len;
            }
            if (s - along[along.Count - 1] > 1e-3f) { pts.Add(line[line.Count - 1]); along.Add(s); }
            return (pts, along);
        }

        /// <summary>A ribbon's height (world y) at line point <paramref name="q"/>: <see cref="ClearanceM"/> above the highest
        /// ground at q and a quarter step (30 m) along the line either way (<paramref name="along"/>, unit). The line's own grade: no
        /// sample across it, so the gorge walls beside the line do not lift it (review B15). A railway cross-section takes
        /// this height across (raised only where the ground would rise under its rails), and the train's wheels too
        /// (review M4).</summary>
        public static float GradeHeight(TerrainData data, Vector3 origin, Vector2 q, Vector2 along) =>
            Mathf.Max(Ground(data, origin, q), Mathf.Max(Ground(data, origin, q + along * (StepM / 4)), Ground(data, origin, q - along * (StepM / 4))))
            + ClearanceM;

        /// <summary>How far ground may lie below a cross-section before the section narrows (m, world): a ribbon edge floating
        /// higher than this over a falling slope would read as a shelf.</summary>
        public const float FallToleranceM = 60f;
        /// <summary>How far ground may rise above a cross-section before it narrows (m, world): an edge running under a
        /// little higher ground reads as a cutting, and the rails are kept clear by the lift (RailSpanShare). Without it,
        /// any rise over the clearance narrowed the band on the rolling foothills too, not only in the gorges.</summary>
        public const float RiseToleranceM = 40f;
        /// <summary>The narrowest a cross-section gets, as a share of the ribbon's width (a gorge floor).</summary>
        public const float MinWidthShare = 0.25f;
        /// <summary>Samples per side when a cross-section looks for where it must stop.</summary>
        internal const int NarrowSamples = 8;

        /// <summary>The half-width (m) a cross-section at <paramref name="q"/> (flat at <paramref name="y"/>) may span. A sample
        /// leaves the tolerance where the ground is more than <see cref="RiseToleranceM"/> above the section or more than
        /// <see cref="FallToleranceM"/> below it. Where the ground leaves it on both sides within the half-width (a gorge floor, a
        /// gorge side, a ridge), the section is cut back symmetrically (so the rails stay centred) to the last sample before
        /// the first one that leaves it, on either side, but never under <see cref="MinWidthShare"/> of the width. Where it leaves
        /// it on one side only (one hill or one drop beside the line), the full <paramref name="half"/>: that edge reads as a
        /// cutting or an embankment, and cutting both sides for it narrowed the foothill lines to about 70% (#252). Railways
        /// only (RailHalfWidths smooths it along the line): in the Sorek gorge the band keeps to the floor instead of riding up
        /// the walls (review B15); on the plains nothing changes.</summary>
        public static float FlatHalfWidth(TerrainData data, Vector3 origin, Vector2 q, Vector2 side, float y, float half)
        {
            int left = 0, right = 0;                                              // the first sample out of tolerance each side
            for (int k = 1; k <= NarrowSamples && (left == 0 || right == 0); k++)
            {
                float o = half * k / NarrowSamples;
                if (left == 0 && OutOfTolerance(Ground(data, origin, q - side * o), y)) left = k;
                if (right == 0 && OutOfTolerance(Ground(data, origin, q + side * o), y)) right = k;
            }
            if (left == 0 || right == 0) return half;
            return Mathf.Max(half * (Math.Min(left, right) - 1) / NarrowSamples, half * MinWidthShare);
        }

        static bool OutOfTolerance(float ground, float y) => ground > y + RiseToleranceM || ground < y - FallToleranceM;

        /// <summary>The ground height (world y) under (x, z), clamped to the terrain.</summary>
        public static float Ground(TerrainData data, Vector3 origin, Vector2 p) =>
            origin.y + data.GetInterpolatedHeight(Mathf.Clamp01((p.x - origin.x) / data.size.x), Mathf.Clamp01((p.y - origin.z) / data.size.z));

        /// <summary>A road vertex's height (world y) at <paramref name="q"/>: <see cref="ClearanceM"/> above the highest ground at
        /// q and half a step along and half a vertex spacing across, either way, so terrain between vertices cannot rise
        /// through the ribbon. Roads drape over the ground this way (each vertex on its own ground); their pale band has
        /// no rails or sleepers to warp, so they keep main's rule (review A-I1: narrowing them thinned and beaded every
        /// hill road).</summary>
        public static float DrapeHeight(TerrainData data, Vector3 origin, Vector2 q, Vector2 along, float spacing)
        {
            var side = new Vector2(along.y, -along.x);
            float y = Ground(data, origin, q);
            foreach (var d in new[] { along * (StepM / 2), -along * (StepM / 2), side * (spacing / 2), -side * (spacing / 2) })
                y = Mathf.Max(y, Ground(data, origin, q + d));
            return y + ClearanceM;
        }

        /// <summary>The largest step (m) of a rail band's half-width from one cross-section to the next: 5% of the full width,
        /// so its width changes by at most a tenth between sections and narrows in a ramp, never in beads (review A-I1).</summary>
        public const float HalfWidthStepShare = 0.05f;
        /// <summary>The share of the half-width, either side of the line, that must stay clear of the ground: the rails
        /// (Route.shader's RailOffset 0.12 plus RailHalfWidth 0.018 of the width, so 0.276 of the half-width, rounded up;
        /// review A-I2). Clearing the whole sleeper span (0.58) lifted the narrowed gorge band a median 39 m off the floor;
        /// the sleepers' outer ends may meet a gorge wall, as a cutting's would.</summary>
        public const float RailSpanShare = 0.3f;
        /// <summary>Clearance (m, world) of the rails over the highest ground under them, where a section had to be lifted.</summary>
        public const float RailClearanceM = 3f;
        /// <summary>Ground samples each side of the line when a section checks the ground under its rails (<see cref="RailSpanShare"/>).</summary>
        internal const int LiftSamples = 4;

        /// <summary>The half-widths of a rail line's sections (centre points <paramref name="pts"/>, unit directions
        /// <paramref name="dirs"/>, heights <paramref name="ys"/>), smoothed along the line: FlatHalfWidth per section, then
        /// the minimum over two sections each way, then limited to <see cref="HalfWidthStepShare"/> of the width per step
        /// (forward and backward passes, which only narrow, so a narrow spot stays narrow and its neighbours ramp to it).</summary>
        public static float[] RailHalfWidths(TerrainData data, Vector3 origin, IReadOnlyList<Vector2> pts, IReadOnlyList<Vector2> dirs,
            IReadOnlyList<float> ys, float width)
        {
            int n = pts.Count; float half = width / 2f, step = HalfWidthStepShare * width;
            var raw = new float[n];
            for (int i = 0; i < n; i++) raw[i] = FlatHalfWidth(data, origin, pts[i], new Vector2(dirs[i].y, -dirs[i].x), ys[i], half);
            var h = new float[n];
            for (int i = 0; i < n; i++)
            {
                float m = raw[i];
                for (int k = Math.Max(0, i - 2); k <= Math.Min(n - 1, i + 2); k++) m = Mathf.Min(m, raw[k]);
                h[i] = m;
            }
            for (int i = 1; i < n; i++) h[i] = Mathf.Min(h[i], h[i - 1] + step);
            for (int i = n - 2; i >= 0; i--) h[i] = Mathf.Min(h[i], h[i + 1] + step);
            return h;
        }

        /// <summary>One mesh for several ribbons. Along each line, a cross-section every <see cref="StepM"/>, square to the
        /// line's direction (averaged over two steps each way, so a tight bend fans instead of folding); across it,
        /// <see cref="Across"/> vertices. UV: u across (0 left edge, 1 right), v along (m).
        /// <list type="bullet">
        /// <item>Roads (<paramref name="railGrade"/> false) drape: each vertex at <see cref="DrapeHeight"/>, with the terrain's
        /// normal at that vertex (main's rule).</item>
        /// <item>Railways (<paramref name="railGrade"/> true) are flat across at the line's own grade (<see cref="GradeHeight"/>,
        /// review B15), so the rails and sleepers never warp; their width follows <see cref="RailHalfWidths"/> (full on the
        /// plains, ramping down to a gorge floor); and a section is lifted, if need be, so the ground never rises under the
        /// rails (<see cref="RailSpanShare"/>; review A-I2). Normal: the terrain's at the line.</item>
        /// </list></summary>
        public static Mesh Ribbon(IEnumerable<(List<Vector2> line, float width)> lines, TerrainData data, Vector3 origin, string name,
            bool railGrade = false) =>
            Geometry(lines.Select(l => ("", l.line, l.width)), data, origin, railGrade).ToMesh(name);

        /// <summary><see cref="Ribbon"/>'s arrays before they become a mesh, with each line's vertex and index range under its
        /// route id (<see cref="RibbonGeometry.Routes"/>).</summary>
        public static RibbonGeometry Geometry(IEnumerable<(string id, List<Vector2> line, float width)> lines, TerrainData data, Vector3 origin,
            bool railGrade = false)
        {
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            var spans = new List<RouteSpan>();
            foreach (var (id, line, width) in lines)
            {
                if (line.Count < 2) { spans.Add(new RouteSpan(id, verts.Count, 0, tris.Count, 0)); continue; }
                var (pts, along) = Resample(line, StepM);
                int n = pts.Count, first = verts.Count, firstIndex = tris.Count;
                var dirs = new Vector2[n];
                for (int i = 0; i < n; i++) dirs[i] = (pts[Math.Min(n - 1, i + 2)] - pts[Math.Max(0, i - 2)]).normalized;
                if (railGrade)
                {
                    var ys = new float[n];
                    for (int i = 0; i < n; i++) ys[i] = GradeHeight(data, origin, pts[i], dirs[i]);
                    var halves = RailHalfWidths(data, origin, pts, dirs, ys, width);
                    for (int i = 0; i < n; i++)
                    {
                        var side = new Vector2(dirs[i].y, -dirs[i].x);                   // right of the direction of travel
                        float y = ys[i], span = RailSpanShare * halves[i];
                        for (int k = -LiftSamples; k <= LiftSamples; k++) y = Mathf.Max(y, Ground(data, origin, pts[i] + side * (span * k / (float)LiftSamples)) + RailClearanceM);
                        var normal = data.GetInterpolatedNormal(Mathf.Clamp01((pts[i].x - origin.x) / data.size.x), Mathf.Clamp01((pts[i].y - origin.z) / data.size.z));
                        for (int j = 0; j < Across; j++)
                        {
                            float a = (float)j / (Across - 1);
                            var q = pts[i] + side * ((a - 0.5f) * 2f * halves[i]);
                            verts.Add(new Vector3(q.x, y, q.y)); normals.Add(normal); uvs.Add(new Vector2(a, along[i]));
                        }
                    }
                }
                else
                {
                    float spacing = width / (Across - 1);
                    for (int i = 0; i < n; i++)
                    {
                        var side = new Vector2(dirs[i].y, -dirs[i].x);
                        for (int j = 0; j < Across; j++)
                        {
                            float a = (float)j / (Across - 1);
                            var q = pts[i] + side * ((a - 0.5f) * width);
                            verts.Add(new Vector3(q.x, DrapeHeight(data, origin, q, dirs[i], spacing), q.y));
                            normals.Add(data.GetInterpolatedNormal(Mathf.Clamp01((q.x - origin.x) / data.size.x), Mathf.Clamp01((q.y - origin.z) / data.size.z)));
                            uvs.Add(new Vector2(a, along[i]));
                        }
                    }
                }
                for (int i = 0; i + 1 < n; i++)
                    for (int j = 0; j + 1 < Across; j++)
                    {
                        int a = first + i * Across + j, b = a + 1, c = a + Across, d = c + 1;
                        tris.AddRange(new[] { a, c, b, b, c, d });         // the shader draws both faces
                    }
                spans.Add(new RouteSpan(id, first, verts.Count - first, firstIndex, tris.Count - firstIndex));
            }
            return new RibbonGeometry(verts.ToArray(), normals.ToArray(), uvs.ToArray(), tris.ToArray(), spans.ToArray());
        }
    }
}
