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
    /// steam train on the Jaffa–Jerusalem line is a showcase, never normal play (review I1, <see cref="ModeFor"/>). Widths are symbolic, measured on the
    /// promo's gameplay frame like the settlements and units (docs/routes.md): a road about 8 px and the railway about
    /// 16 px wide at 1920 px (the soft edges take about a sixth of the railway's width, so its bed is drawn 680 m), so they read at the gameplay zoom as the promo draws them. Registers the "routes" (ribbons)
    /// and "train" visual toggles, so the frame profile prices each. Pure geometry from the data: the same file gives the
    /// same meshes, vertex for vertex.</summary>
    public static class RouteLayer
    {
        public const string RootName = "Routes", RibbonsName = "RouteRibbons", RoutesToggle = "routes", TrainToggle = "train";
        public const string RailMaterialResource = "Routes/Rail", RoadMaterialResource = "Routes/Road";
        public const string Folder = "Assets/Resources/Routes";

        /// <summary>Full ribbon widths (m), soft edges included (the shader fades the outer 16-30%).</summary>
        public const float RailWidthM = 680f, RoadWidthM = 300f, TrackWidthM = 200f;
        /// <summary>Spacing (m) of the ribbon's cross-sections along its line: under the heightmap's 211 m N-S spacing, so
        /// every terrain row gets a section; about 3 px at the gameplay zoom.</summary>
        public const float StepM = 120f;
        /// <summary>Vertices across a ribbon: its edges, its centre and the quarter lines, each on its own ground, so a
        /// ribbon on a slope follows the slope instead of standing out flat.</summary>
        public const int Across = 5;
        /// <summary>Height above the highest ground sampled around each vertex; the shader's depth offset does the rest.</summary>
        public const float ClearanceM = 20f;

        /// <summary>The showcase train's route and stretch: the Jaffa–Jerusalem railway from its Jaffa end to Lydda junction,
        /// the stretch the promo's train runs (promo/final/03). It is ambience for the promo frames, not a timetable: no
        /// regular trains ran here from the British withdrawal in May 1948 until Lydda was taken in July and service resumed
        /// later (Palestine Railways), which is why normal play shows no train (<see cref="ModeFor"/>).</summary>
        public const string TrainRoute = "jaffa_jerusalem_railway";
        public static readonly (double lon, double lat) TrainTurnAt = (34.87556, 31.94583);   // Lod railway station

        /// <summary>The date the map shows before the game tells it one (editor renders, screenshots before GAME OK): the
        /// game's start, 15 May 1948.</summary>
        public static readonly DateTime DefaultDate = new DateTime(1948, 5, 15);

        public enum TrainMode { None, Still, Moving }

        /// <summary>Whether there is a train and whether it moves (review I1): editor renders and screenshots (`?shot` /
        /// `-shot`) show the promo's still train; `?train` or `?units` (desktop `-train` / `-units`) asks for the moving
        /// showcase; normal play has none, as no service is sourced for the line at the game's dates.</summary>
        public static TrainMode ModeFor(bool playing, bool shot, bool requested) =>
            !playing || shot ? TrainMode.Still : requested ? TrainMode.Moving : TrainMode.None;

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
            var ribbons = map.Terrain.transform.Find($"{RootName}/{RibbonsName}");
            if (ribbons == null) return;
            foreach (var g in ribbons.GetComponentsInChildren<RouteGroup>(true)) g.gameObject.SetActive(g.Window.Covers(date));
        }

        /// <summary>Clearance (m) beyond a ribbon's drawn half-width that kit buildings and trees keep from it, so a lot's or
        /// crown's edge never touches the soft edge of the road or the ballast.</summary>
        public const float CorridorMarginM = 100f;

        /// <summary>Every route's line (world x, z) with the half-width kits must keep clear of: half the drawn width plus
        /// <see cref="CorridorMarginM"/>. All routes, whatever their window: the kits are built once, not per date, so a route
        /// that appears later (the Burma Road) has its corridor from the start.</summary>
        public static List<(List<Vector2> line, float halfWidth)> Corridors(GeoFrame frame, IEnumerable<RoutePath> routes) =>
            routes.Select(r => (World(frame, r), WidthOf(r.Kind) / 2f + CorridorMarginM)).ToList();

        public static float WidthOf(string kind) => kind switch
        {
            RoutePaths.Rail => RailWidthM,
            RoutePaths.Road => RoadWidthM,
            RoutePaths.Track => TrackWidthM,
            _ => throw new ArgumentException($"unknown route kind '{kind}'", nameof(kind)),
        };

        /// <summary>Builds the routes under <paramref name="terrain"/> (so a map rebuild destroys them): one group per display
        /// window (its rail and road ribbons), shown as at <paramref name="date"/>, and, when <paramref name="train"/> is not
        /// None and the train's route is in <paramref name="routes"/>, the train (still: it stands at its still position, so
        /// screenshots and editor renders repeat exactly). Meshes, the train's palette and its material copy are owned by
        /// the root's own MapRuntimeAssets, which also unregisters the toggles. Returns the root.</summary>
        public static GameObject Build(Terrain terrain, GeoFrame frame, IReadOnlyList<RoutePath> routes, TrainMode train, DateTime date)
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
            foreach (var group in routes.GroupBy(RoutePaths.WindowOf).OrderBy(g => g.Key.From).ThenBy(g => g.Key.To ?? DateTime.MaxValue))
            {
                var holder = new GameObject($"Window {group.Key}");
                holder.transform.SetParent(ribbons.transform, false);
                holder.AddComponent<RouteGroup>().Window = group.Key;
                foreach (var (kinds, material, name) in new[]
                         {
                             (new[] { RoutePaths.Rail }, rail, "Rail"),
                             (new[] { RoutePaths.Road, RoutePaths.Track }, road, "Roads"),
                         })
                {
                    var lines = group.Where(r => kinds.Contains(r.Kind)).Select(r => (World(frame, r), WidthOf(r.Kind))).ToList();
                    if (lines.Count == 0) continue;
                    var mesh = assets.Own(Ribbon(lines, data, origin, $"Route{name} {group.Key}", railGrade: name == "Rail"));
                    mesh.UploadMeshData(true);
                    var go = new GameObject(name);
                    go.transform.SetParent(holder.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = material;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = true;
                }
                holder.SetActive(group.Key.Covers(date));
            }
            assets.OnTeardown(VisualToggles.Register(RoutesToggle, on => { if (ribbons != null) ribbons.SetActive(on); }));

            var trainRoute = routes.FirstOrDefault(r => r.Id == TrainRoute);
            if (trainRoute != null && train != TrainMode.None)
            {
                var go = RouteTrain.Spawn(root.transform, assets, World(frame, trainRoute), data, origin,
                    frame.ToWorld(TrainTurnAt.lon, TrainTurnAt.lat, 0), frame.ToWorld(RouteTrain.StillAt.lon, RouteTrain.StillAt.lat, 0),
                    train == TrainMode.Still);
                assets.OnTeardown(VisualToggles.Register(TrainToggle, on => { if (go != null) go.SetActive(on); }));
            }
            return root;
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
        const int NarrowSamples = 8;

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
        const int LiftSamples = 4;

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
            bool railGrade = false)
        {
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            foreach (var (line, width) in lines)
            {
                if (line.Count < 2) continue;
                var (pts, along) = Resample(line, StepM);
                int n = pts.Count, first = verts.Count;
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
            }
            var mesh = new Mesh { name = name };
            if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
