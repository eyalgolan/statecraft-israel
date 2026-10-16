using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Routes;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The 1948 railways and roads (issue #35): the traced data, the ribbons, the train, the toggles and the look.</summary>
public class RouteTests
{
    static readonly (double, double, double, double) Bounds = (34.2, 35.9, 29.45, 33.35);
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", RoutePaths.DefaultFileName));

    [Test]
    public void TheCommittedRoutesParse()
    {
        var routes = RoutePaths.Parse(CommittedYaml, Bounds);
        Assert.GreaterOrEqual(routes.Count, 19);
        var ids = routes.Select(r => r.Id).ToList();
        CollectionAssert.IsSubsetOf(new[] { "jaffa_jerusalem_railway", "kantara_main_line_south", "eastern_railway",
            "hadera_haifa_railway", "jaffa_jerusalem_road", "burma_road", "tel_aviv_haifa_road", "coastal_road_south" }, ids);
        Assert.AreEqual(RoutePaths.Rail, routes.First(r => r.Id == RouteLayer.TrainRoute).Kind);
        Assert.IsTrue(routes.All(r => r.Status == "draft" || r.Status == "fact-checked"));
    }

    [TestCase("routes: []", "no routes")]
    [TestCase("routes:\n- {id: a, kind: canal, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "unknown kind 'canal'")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5]]}", "'a' needs at least two points")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [40.0, 31.6]]}", "'a' point 1 (40, 31.6) is outside the map")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [34.6]]}", "'a' point 1 is not a [lon, lat] pair")]
    [TestCase("routes:\n- {id: Bad, kind: road, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "'Bad' id must be lower snake_case")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}\n- {id: a, kind: rail, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "'a' duplicate id")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "'a' from/to")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, from: '1948-6-1', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "'a' from/to")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, from: '1948', to: '1947', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "'a' to must be after from")]
    [TestCase("routes:\n- {id: a, kind: road, colour: red, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "colour")]
    public void BadRouteFilesNameTheFileAndTheProblem(string yaml, string expected)
    {
        var e = Assert.Throws<InvalidDataException>(() => RoutePaths.Parse(yaml, Bounds, "r.yaml"));
        StringAssert.StartsWith("r.yaml:", e.Message);
        StringAssert.Contains(expected, e.Message);
    }

    /// <summary>Map-only: the sim's content hash is the same with and without the route files (so neither moves the
    /// goldens), and the WebGL manifest ships the traced file the map reads.</summary>
    [Test]
    public void TheRoutesDoNotMoveTheContentHash()
    {
        var copy = Path.Combine(Path.GetTempPath(), "statecraft-noroutes-" + System.Guid.NewGuid().ToString("N"));
        try
        {
            Statecraft.Game.Boot.ContentManifest.Copy(Paths.Content, copy);
            foreach (var f in Directory.GetFiles(Path.Combine(copy, "map"), "route*_1948.yaml")) File.Delete(f);
            Assert.AreEqual(ContentLoader.Load(Paths.Content).Hash, ContentLoader.Load(copy).Hash);
            var manifest = Statecraft.Game.Boot.ContentManifest.Collect(Paths.Content);
            CollectionAssert.Contains(manifest, "map/" + RoutePaths.DefaultFileName);
            CollectionAssert.DoesNotContain(manifest, "map/routes_1948.yaml", "the tracer's hand-written input is never downloaded (review M10)");
        }
        finally { if (Directory.Exists(copy)) Directory.Delete(copy, true); }
    }

    [Test]
    public void ResampleKeepsTheEndsAndTheSpacing()
    {
        var (pts, along) = RouteLayer.Resample(new List<Vector2> { new Vector2(0, 0), new Vector2(250, 0), new Vector2(250, 100) }, 120f);
        Assert.AreEqual(new Vector2(0, 0), pts[0]);
        Assert.AreEqual(new Vector2(250, 100), pts[pts.Count - 1]);
        Assert.AreEqual(350f, along[along.Count - 1], 1e-3f);
        for (int i = 1; i + 1 < along.Count; i++) Assert.AreEqual(120f * i, along[i], 1e-3f);
    }

    internal static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        map.Build();
        return map;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    internal static Transform RoutesRoot(MapScene map) => map.Terrain.transform.Find(RouteLayer.RootName);

    static IEnumerable<MeshFilter> Ribbons(Transform root, string name) =>
        root.Find(RouteLayer.RibbonsName).GetComponentsInChildren<MeshFilter>(true).Where(f => f.name == name);

    /// <summary>The rail ribbon's vertices, hashed (SHA-256 over their float bits). Pinned below, so a fresh editor process
    /// building the committed data must give the same geometry bit for bit (review M11), and an unintended geometry
    /// change fails here. A deliberate change (new data, new widths) updates the constant.</summary>
    static string Hash(Mesh m)
    {
        var bytes = new List<byte>();
        foreach (var v in m.vertices) { bytes.AddRange(System.BitConverter.GetBytes(v.x)); bytes.AddRange(System.BitConverter.GetBytes(v.y)); bytes.AddRange(System.BitConverter.GetBytes(v.z)); }
        foreach (var uv in m.uv) { bytes.AddRange(System.BitConverter.GetBytes(uv.x)); bytes.AddRange(System.BitConverter.GetBytes(uv.y)); }
        using var sha = System.Security.Cryptography.SHA256.Create();
        return string.Concat(sha.ComputeHash(bytes.ToArray()).Select(b => b.ToString("x2")));
    }
    const string RailMeshHash = "e6b1710041c05f9ce18d1591e5173c003b933308e9cb84fa807e23b015f16fbd";   // #252: full width where only one side leaves the tolerance; #319: cut at the Old City's walls

    [Test]
    public void TheMapBuildsRibbonsOnTheGroundAndTheTogglesHideThem()
    {
        var map = BuildMap();
        List<Object> owned;
        try
        {
            var root = RoutesRoot(map);
            Assert.IsNotNull(root, "MapScene.Build adds the routes under the terrain (a rebuild destroys them)");
            var rail = Ribbons(root, "Rail").ToList(); var roads = Ribbons(root, "Roads").ToList();
            Assert.IsTrue(rail.Count > 0 && roads.Count > 0);
            Assert.AreSame(Resources.Load<Material>(RouteLayer.RailMaterialResource), rail[0].GetComponent<MeshRenderer>().sharedMaterial);
            // Budget: well under 100k triangles in all (WebGL's integrated GPUs draw millions), one draw call per window
            // group and look (about a dozen).
            int tris = (int)(rail.Concat(roads).Sum(f => (long)f.sharedMesh.GetIndexCount(0)) / 3);
            int railVerts = rail.Sum(f => f.sharedMesh.vertexCount), roadVerts = roads.Sum(f => f.sharedMesh.vertexCount);
            TestContext.WriteLine($"rail {railVerts} vertices, roads {roadVerts}, {tris} triangles, {rail.Count + roads.Count} draw calls");
            Assert.Less(tris, 100000);
            Assert.LessOrEqual(rail.Count + roads.Count, 20);
            // The meshes are freed after upload (no CPU copy), so check a fresh build of the same data: every vertex
            // clears the ground under it by the clearance, and lies on the terrain.
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            var check = RouteLayer.Ribbon(routes.Where(r => r.Kind == RoutePaths.Rail).Select(r => (RouteLayer.Drawn(map.Frame, r), RouteLayer.RailWidthM)), data, origin, "check", railGrade: true);
            try
            {
                Assert.AreEqual(railVerts, check.vertexCount, "the same data gives the same vertices");
                foreach (var v in check.vertices)
                {
                    Assert.That(v.x, Is.InRange(origin.x, origin.x + data.size.x)); Assert.That(v.z, Is.InRange(origin.z, origin.z + data.size.z));
                }
                // Roads keep main's drape: every vertex clears the ground under it by the clearance (the railways' clearance
                // under their rails is TheRailClearsTheGroundUnderItsRailsAndRampsItsWidth).
                var roadCheck = RouteLayer.Ribbon(routes.Where(r => r.Kind != RoutePaths.Rail).Select(r => (RouteLayer.Drawn(map.Frame, r), RouteLayer.WidthOf(r.Kind))), data, origin, "roads");
                Assert.AreEqual(roadVerts, roadCheck.vertexCount);
                foreach (var v in roadCheck.vertices)
                    Assert.GreaterOrEqual(v.y - RouteLayer.Ground(data, origin, new Vector2(v.x, v.z)), RouteLayer.ClearanceM - 0.01f);
                Object.DestroyImmediate(roadCheck);
                var hash = Hash(check);
                TestContext.WriteLine($"rail mesh hash {hash}");
                Assert.AreEqual(RailMeshHash, hash, "the committed data builds the pinned rail geometry in a fresh process");
            }
            finally { Object.DestroyImmediate(check); }

            CollectionAssert.Contains(VisualToggles.Names, RouteLayer.RoutesToggle);
            CollectionAssert.Contains(VisualToggles.Names, RouteLayer.TrainToggle);
            VisualToggles.Set(RouteLayer.RoutesToggle, false);
            Assert.IsFalse(root.Find(RouteLayer.RibbonsName).gameObject.activeSelf);
            Assert.IsTrue(root.Find("Train").gameObject.activeSelf, "each toggle prices its own part");
            VisualToggles.Set(RouteLayer.TrainToggle, false);
            Assert.IsFalse(root.Find("Train").gameObject.activeSelf);
            VisualToggles.SetAll(true);
            Assert.IsTrue(root.Find(RouteLayer.RibbonsName).gameObject.activeSelf && root.Find("Train").gameObject.activeSelf);
            owned = root.GetComponentsInChildren<MeshFilter>(true).Select(f => (Object)f.sharedMesh).ToList();
            var paint = root.Find("Train/Locomotive").GetComponent<MeshRenderer>().sharedMaterial;
            owned.Add(paint); owned.Add(paint.GetTexture("_BaseMap"));
            Assert.IsTrue(owned.All(o => o != null));
        }
        finally { Object.DestroyImmediate(map.gameObject); }
        Assert.IsTrue(owned.All(o => o == null), "destroying the map frees the route meshes, the train's material and its palette texture");
        CollectionAssert.DoesNotContain(VisualToggles.Names, RouteLayer.RoutesToggle, "the map's teardown removes the switches");
        CollectionAssert.DoesNotContain(VisualToggles.Names, RouteLayer.TrainToggle);
    }

    /// <summary>Review B15: the railway follows its own grade instead of riding up the gorge walls. In the Sorek gorge
    /// (Artuf to Battir) every cross-section is flat across (main's rule, each vertex at the highest ground within half a
    /// step and half a vertex spacing, 85 m, of itself, warped the band by hundreds of metres across: the bug this guards);
    /// the band's lift over the ground at the line stays bounded, and the band narrows where the walls close in.</summary>
    [Test]
    public void TheRailwayKeepsItsGradeInTheSorekGorge()
    {
        var map = BuildMap();
        try
        {
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            var jjr = RoutePaths.Parse(CommittedYaml, Bounds).First(r => r.Id == "jaffa_jerusalem_railway");
            var line = RouteLayer.World(map.Frame, jjr);
            var mesh = RouteLayer.Ribbon(new[] { (line, RouteLayer.RailWidthM) }, data, origin, "sorek", railGrade: true);
            // The old rule, for comparison: a vertex at q took the highest ground at q and half a step along / half a
            // vertex spacing across either way, plus the clearance.
            float spacing = RouteLayer.RailWidthM / (RouteLayer.Across - 1);
            float Old(Vector2 q, Vector2 t)
            {
                var side = new Vector2(t.y, -t.x); float y = RouteLayer.Ground(data, origin, q);
                foreach (var d in new[] { t * (RouteLayer.StepM / 2), -t * (RouteLayer.StepM / 2), side * (spacing / 2), -side * (spacing / 2) })
                    y = Mathf.Max(y, RouteLayer.Ground(data, origin, q + d));
                return y + RouteLayer.ClearanceM;
            }
            try
            {
                var v = mesh.vertices; int n = RouteLayer.Across, mid = n / 2;
                var (west, east) = (map.Frame.ToWorld(35.00, 31.75, 0).x, map.Frame.ToWorld(35.13, 31.75, 0).x);   // Artuf to Battir
                var lifts = new List<float>(); var oldWarps = new List<float>(); int narrowed = 0;
                for (int i = 0; i + n <= v.Length; i += n)
                {
                    for (int j = 0; j < n; j++) Assert.AreEqual(v[i + mid].y, v[i + j].y, 0.01f, "a cross-section is flat across");
                    var c = v[i + mid];
                    if (c.x < west || c.x > east) continue;
                    var cq = new Vector2(c.x, c.z);
                    var across = (new Vector2(v[i + n - 1].x, v[i + n - 1].z) - new Vector2(v[i].x, v[i].z));
                    var t = new Vector2(-across.y, across.x).normalized;            // along the line (square to the section)
                    // What the old rule did across the full 680 m band: its five vertices' spread (the warp).
                    var side = new Vector2(t.y, -t.x);
                    var olds = Enumerable.Range(0, n).Select(j => Old(cq + side * ((j / (float)(n - 1) - 0.5f) * RouteLayer.RailWidthM), t)).ToList();
                    oldWarps.Add(olds.Max() - olds.Min());
                    lifts.Add(c.y - RouteLayer.Ground(data, origin, cq) - RouteLayer.ClearanceM);
                    if (across.magnitude < RouteLayer.RailWidthM - 1f) narrowed++;
                }
                lifts.Sort(); oldWarps.Sort();
                float P(List<float> l, float q) => l[Mathf.Min(l.Count - 1, (int)(q * l.Count))];
                TestContext.WriteLine($"Sorek: {lifts.Count} sections; new warp 0 (flat); old warp median {P(oldWarps, 0.5f):0} m, p90 {P(oldWarps, 0.9f):0} m; " +
                    $"new lift over the ground at the line median {P(lifts, 0.5f):0} m, p90 {P(lifts, 0.9f):0} m, max {lifts[lifts.Count - 1]:0} m; {narrowed} narrowed");
                Assert.Greater(lifts.Count, 50);
                Assert.Greater(P(oldWarps, 0.5f), 50f, "the old rule warped the band across the gorge (the bug this guards)");
                Assert.Less(P(lifts, 0.5f), 12f, "the band sits at the line's grade");
                Assert.Less(P(lifts, 0.9f), LiftP90BoundM, "and stays near it where the trace runs on the canyon side");
                Assert.Greater(narrowed, 0, "the band narrows where the gorge walls close in");
            }
            finally { Object.DestroyImmediate(mesh); }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
    // Measured 2026-10-12 (3288a93, unchanged at 6c1f964): old warp median 315 m (p90 454 m); new lift median 10 m, p90 25 m,
    // max 40 m. 005e60e: median 11 m, p90 38 m, so the p90 bound fails there.
    const float LiftP90BoundM = 35f;

    /// <summary>Review A-I1 and A-I2, on every railway: the ground never rises above the band under its rails
    /// (main had none; 005e60e buried the rails by up to 32 m from Artuf to Battir and in 15 sections of the Jezreel line),
    /// the band's width never jumps by more than a tenth of the full width from one 120 m section to the next (005e60e
    /// beaded), and its edges never float far over the ground.</summary>
    [Test]
    public void TheRailClearsTheGroundUnderItsRailsAndRampsItsWidth()
    {
        var map = BuildMap();
        try
        {
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            int buried = 0, sections = 0, jumps = 0; var floats = new List<float>(); var worst = "";
            foreach (var r in RoutePaths.Parse(CommittedYaml, Bounds).Where(r => r.Kind == RoutePaths.Rail))
            {
                var mesh = RouteLayer.Ribbon(new[] { (RouteLayer.World(map.Frame, r), RouteLayer.RailWidthM) }, data, origin, r.Id, railGrade: true);
                try
                {
                    var v = mesh.vertices; int n = RouteLayer.Across, mid = n / 2; float lastWidth = float.NaN;
                    for (int i = 0; i + n <= v.Length; i += n)
                    {
                        sections++;
                        var c = v[i + mid]; var left = new Vector2(v[i].x, v[i].z); var right = new Vector2(v[i + n - 1].x, v[i + n - 1].z);
                        var cq = new Vector2(c.x, c.z); float width = (right - left).magnitude;
                        var side = (right - left) / Mathf.Max(width, 1e-3f);
                        // The rails: the shader's rails sit 0.12 of the band's width either side of the line, 0.018 wide.
                        float span = 0.14f * width;
                        float highest = float.MinValue;
                        for (int k = -6; k <= 6; k++) highest = Mathf.Max(highest, RouteLayer.Ground(data, origin, cq + side * (span * k / 6f)));
                        if (highest > c.y + 0.5f) { buried++; worst = $"{r.Id} at {map.Frame.ToLonLat(c)} by {highest - c.y:0} m"; }
                        if (!float.IsNaN(lastWidth) && Mathf.Abs(width - lastWidth) > 0.1f * RouteLayer.RailWidthM + 0.5f) jumps++;
                        lastWidth = width;
                        floats.Add(c.y - RouteLayer.Ground(data, origin, left)); floats.Add(c.y - RouteLayer.Ground(data, origin, right));
                    }
                }
                finally { Object.DestroyImmediate(mesh); }
            }
            floats.Sort();
            float p99 = floats[(int)(0.99f * (floats.Count - 1))];
            TestContext.WriteLine($"rail: {sections} sections, {buried} with ground over the rails (worst {worst}), {jumps} width jumps over 10%, edge float p99 {p99:0} m, max {floats[floats.Count - 1]:0} m");
            Assert.AreEqual(0, buried, $"the ground rises above the rails: {worst}");
            Assert.AreEqual(0, jumps, "the band's width ramps, never jumps");
            Assert.Less(p99, EdgeFloatP99BoundM, "the band's edges stay near the ground");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
    // Measured 2026-10-12 (3288a93, unchanged at 6c1f964): edge float p99 85 m, max 223 m (flat sections on hillsides; the floor
    // is 25% width). 005e60e: 67 sections with ground over the rails, 819 width jumps, edge float p99 107 m.
    const float EdgeFloatP99BoundM = 100f;

    /// <summary>#252: a railway keeps its width outside the gorges. A section is in a gorge where, within the band's
    /// half-width, the ground leaves the tolerance on both sides (a wall or a drop each side: a gorge floor, a gorge side,
    /// a ridge); one hill or one drop beside the line is not a gorge. Every line keeps at least 90% of its width, on
    /// average, outside the gorges, and its mean width over all its sections stays at or above the pinned floor.</summary>
    [Test]
    public void EveryRailwayKeepsItsWidthOutsideTheGorges()
    {
        var map = BuildMap();
        try
        {
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            var failures = new List<string>();
            foreach (var r in RoutePaths.Parse(CommittedYaml, Bounds).Where(r => r.Kind == RoutePaths.Rail))
            {
                var mesh = RouteLayer.Ribbon(new[] { (RouteLayer.World(map.Frame, r), RouteLayer.RailWidthM) }, data, origin, r.Id, railGrade: true);
                try
                {
                    var v = mesh.vertices; int n = RouteLayer.Across; float half = RouteLayer.RailWidthM / 2f;
                    var all = new List<float>(); var open = new List<float>(); int gorge = 0;
                    for (int i = 0; i + n <= v.Length; i += n)
                    {
                        var left = new Vector2(v[i].x, v[i].z); var right = new Vector2(v[i + n - 1].x, v[i + n - 1].z);
                        var cq = (left + right) / 2f; float share = (right - left).magnitude / RouteLayer.RailWidthM;
                        var side = (right - left).normalized; var t = new Vector2(-side.y, side.x);
                        float y = RouteLayer.GradeHeight(data, origin, cq, t);
                        bool Leaves(float s)
                        {
                            for (int k = 1; k <= 8; k++)
                            {
                                float g = RouteLayer.Ground(data, origin, cq + side * (s * half * k / 8f));
                                if (g > y + RouteLayer.RiseToleranceM || g < y - RouteLayer.FallToleranceM) return true;
                            }
                            return false;
                        }
                        all.Add(share);
                        if (Leaves(-1f) && Leaves(1f)) gorge++; else open.Add(share);
                    }
                    float mean = all.Average(), outside = open.Count > 0 ? open.Average() : 1f;
                    float floor = MeanWidthFloor.TryGetValue(r.Id, out var f) ? f : 0.9f;
                    TestContext.WriteLine($"{r.Id}: {all.Count} sections, {gorge} in gorges; mean width {mean:P1} (floor {floor:P0}), outside the gorges {outside:P1}");
                    if (outside < 0.9f) failures.Add($"{r.Id} keeps {outside:P1} of its width outside the gorges");
                    if (mean < floor) failures.Add($"{r.Id} keeps {mean:P1} of its width, under its floor {floor:P0}");
                }
                finally { Object.DestroyImmediate(mesh); }
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
    /// <summary>Each line's pinned mean width (share of the full width, all sections, gorges included). Lines without an
    /// entry need 90%. Measured 2026-10-12 (#252), main (f974c9f) in brackets: Jaffa–Jerusalem 76.2% (70.7%; 178 of 665
    /// sections in gorges: the Sorek), main line south 100.0% (98.9%), Eastern Railway 100.0% (99.3%), Hadera–Haifa 97.4%
    /// (92.0%), Haifa–Beirut 95.5% (95.1%), Jezreel Valley 82.5% (74.5%; 113 of 775 in gorges: chiefly the Beisan–Jordan
    /// descent and the Yarmuk, and about 20 under the Carmel by Haifa). Outside the gorges main kept 87.4% on Jaffa–Jerusalem and 82.8% on the Jezreel line, so this fails there.</summary>
    static readonly Dictionary<string, float> MeanWidthFloor = new Dictionary<string, float>
    {
        ["jaffa_jerusalem_railway"] = 0.76f, ["kantara_main_line_south"] = 0.99f, ["eastern_railway"] = 0.99f,
        ["hadera_haifa_railway"] = 0.97f, ["haifa_beirut_railway"] = 0.95f, ["jezreel_valley_railway"] = 0.82f,
    };

    /// <summary>C1: the Burma Road shows only from its completion on 14 June 1948; the start (15 May 1948) does not have
    /// it, every other route is there from the start.</summary>
    [Test]
    public void TheBurmaRoadAppearsOn14June1948()
    {
        var map = BuildMap();
        try
        {
            var groups = RoutesRoot(map).Find(RouteLayer.RibbonsName).GetComponentsInChildren<RouteGroup>(true).ToList();
            var burma = groups.Single(g => g.Window.From == new System.DateTime(1948, 6, 14));
            Assert.IsFalse(burma.gameObject.activeSelf, "built as at the game's start, 15 May 1948");
            Assert.IsTrue(groups.Where(g => g != burma).All(g => g.gameObject.activeSelf), "the rest are on the 1946 map");
            RouteLayer.ShowAt(map, new System.DateTime(1948, 6, 13));
            Assert.IsFalse(burma.gameObject.activeSelf);
            RouteLayer.ShowAt(map, new System.DateTime(1948, 6, 14));
            Assert.IsTrue(burma.gameObject.activeSelf);
            RouteLayer.ShowAt(map, new System.DateTime(1948, 5, 15));
            Assert.IsFalse(burma.gameObject.activeSelf, "a reloaded earlier save hides it again");
            RouteLayer.ShowAt(map, new System.DateTime(1910, 1, 1));
            Assert.IsTrue(groups.Where(g => g.Window.From.Year > 1910).All(g => !g.gameObject.activeSelf), "nothing before its window");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void WindowsAndBounds()
    {
        Assert.AreEqual(new System.DateTime(1946, 1, 1), RoutePaths.Bound("1946"));
        Assert.AreEqual(new System.DateTime(1948, 6, 14), RoutePaths.Bound("1948-06-14"));
        Assert.Throws<System.FormatException>(() => RoutePaths.Bound("48"));
        var w = new RouteWindow(new System.DateTime(1948, 6, 14), new System.DateTime(1949, 1, 1));
        Assert.IsFalse(w.Covers(new System.DateTime(1948, 6, 13))); Assert.IsTrue(w.Covers(new System.DateTime(1948, 6, 14)));
        Assert.IsFalse(w.Covers(new System.DateTime(1949, 1, 1)), "to is exclusive");
    }

    /// <summary>I3: a missing or malformed route file means no routes and a warning, never a broken map.</summary>
    [Test]
    public void AMissingOrBadRouteFileLeavesTheMapWithoutRoutes()
    {
        var warnings = new List<string>();
        Assert.IsEmpty(RoutePaths.ReadOrNone("", Bounds, warnings.Add));
        Assert.IsEmpty(RoutePaths.ReadOrNone("routes: [", Bounds, warnings.Add));
        Assert.AreEqual(2, warnings.Count);
        StringAssert.Contains("missing", warnings[0]);
        Assert.Greater(RoutePaths.ReadOrNone(CommittedYaml, Bounds, warnings.Add).Count, 0);
        Assert.AreEqual(2, warnings.Count, "the committed file reads without a warning");

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
        inputs.RoutesYaml = "";
        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("route_paths_1948.yaml: missing"));
        try
        {
            map.Build(inputs);
            Assert.IsNotNull(map.Terrain, "the map still builds");
            Assert.IsEmpty(RoutesRoot(map).GetComponentsInChildren<MeshFilter>(true), "no ribbons and no train");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Re-review nit 1: a route with an impossible date (year 0000, a 13th month) is skipped with a warning by the
    /// tolerant read, and the map still builds with every other route; the strict parse still rejects it.</summary>
    [Test]
    public void ABadDateSkipsOnlyThatRoute()
    {
        const string good = "- {id: a, kind: road, status: draft, from: '1946', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}\n";
        foreach (var bad in new[] { "0000", "1948-13-40" })
        {
            var yaml = "routes:\n" + good + $"- {{id: b, kind: road, status: draft, from: '{bad}', length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}}\n";
            var warnings = new List<string>();
            var routes = RoutePaths.ReadOrNone(yaml, Bounds, warnings.Add);
            CollectionAssert.AreEqual(new[] { "a" }, routes.Select(r => r.Id).ToList(), bad);
            Assert.AreEqual(1, warnings.Count, bad);
            StringAssert.Contains("'b' from/to", warnings[0]);
            var e = Assert.Throws<InvalidDataException>(() => RoutePaths.Parse(yaml, Bounds, "r.yaml"));
            StringAssert.Contains("'b' from/to", e.Message);
        }

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
        Assert.IsTrue(inputs.RoutesYaml.Contains("from: '1948-06-14'"));
        inputs.RoutesYaml = inputs.RoutesYaml.Replace("from: '1948-06-14'", "from: '0000'");
        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("'burma_road' from/to"));
        try
        {
            map.Build(inputs);
            var groups = RoutesRoot(map).Find(RouteLayer.RibbonsName).GetComponentsInChildren<RouteGroup>(true);
            Assert.Greater(groups.Length, 0, "the other routes are drawn");
            Assert.IsFalse(groups.Any(g => g.Window.From.Year == 1948), "only the bad route is left out");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Re-review nit 2: a map rebuilt mid-game draws its routes as at the game's date at once, not as at 15 May
    /// until the next state change.</summary>
    [Test]
    public void ARebuiltMapKeepsTheGameDate()
    {
        var map = BuildMap();
        try
        {
            Assert.IsNull(map.RouteDate);
            RouteLayer.ShowAt(map, new System.DateTime(1948, 7, 1));
            Assert.AreEqual(new System.DateTime(1948, 7, 1), map.RouteDate);
            map.Build();
            var burma = RoutesRoot(map).Find(RouteLayer.RibbonsName).GetComponentsInChildren<RouteGroup>(true)
                .Single(g => g.Window.From == new System.DateTime(1948, 6, 14));
            Assert.IsTrue(burma.gameObject.activeSelf, "shown at once after the rebuild");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>RouteLayer.TrimEndsInside, on hand-made lines (no map): an end inside the square is cut exactly on the wall
    /// line, nothing that is left lies inside, and only the ends are cut (#319 review). The railway case uses its real
    /// numbers, in metres from the square's centre: its first point inside is only 92 m deep, well under the 340 m the
    /// ribbon test allows, so this test is what pins the crossing.</summary>
    [Test]
    public void TrimEndsInsideCutsEachEndExactlyOnTheWall()
    {
        var c = new Vector2(98264f, 257260f); const float half = 2000f, eps = 0.05f;   // float32 at these world coordinates has a 0.03 m step
        List<Vector2> Line(params float[] xy) { var l = new List<Vector2>(); for (int i = 0; i < xy.Length; i += 2) l.Add(c + new Vector2(xy[i], xy[i + 1])); return l; }
        float Cheb(Vector2 p) => Mathf.Max(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y));
        void NothingInside(List<Vector2> l, string what) { foreach (var p in l) Assert.GreaterOrEqual(Cheb(p), half - eps, $"{what}: {p - c} is inside"); }

        // The railway: in through the south wall, its last two points inside (92 m and 1094 m deep).
        var rail = RouteLayer.TrimEndsInside(Line(-2227f, -2570f, -1743f, -1908f, -904f, -1094f), c, half);
        Assert.AreEqual(2, rail.Count, "the railway keeps its outside point and gains the crossing");
        var end = rail[rail.Count - 1] - c;
        Assert.AreEqual(-2000f, end.y, eps, "the railway ends on the south wall");
        Assert.AreEqual(-1743f + (-484f) * (92f / 662f), end.x, 0.1f, "where its last segment crosses it");
        NothingInside(rail, "railway");

        // A line that starts inside and leaves by the east wall: its first point becomes the crossing (2000, 150).
        var east = RouteLayer.TrimEndsInside(Line(0f, 0f, 1500f, 100f, 2500f, 200f), c, half);
        Assert.AreEqual(2, east.Count);
        Assert.AreEqual(2000f, (east[0] - c).x, eps); Assert.AreEqual(150f, (east[0] - c).y, eps);
        Assert.AreEqual(half, Cheb(east[0]), eps, "the cut end lies on the wall line");
        NothingInside(east, "east");

        // Wholly outside: unchanged. Wholly inside: empty. Through the square: only the ends are cut, so it keeps its middle.
        var outside = Line(-3000f, -3000f, 3000f, -3000f);
        CollectionAssert.AreEqual(outside, RouteLayer.TrimEndsInside(outside, c, half));
        CollectionAssert.IsEmpty(RouteLayer.TrimEndsInside(Line(-100f, 0f, 100f, 0f), c, half));
        var through = Line(-3000f, 0f, 0f, 0f, 3000f, 0f);
        CollectionAssert.AreEqual(through, RouteLayer.TrimEndsInside(through, c, half));
    }

    /// <summary>Jerusalem's symbolic Old City is a walled square 4 km across (SettlementKits.OldCityWalls), so the real 1948
    /// roads that meet by New Gate, and the Jaffa-Jerusalem railway's station, all outside the real walls, fall inside it.
    /// The map cuts each road's and the railway's ribbon at the walls (#319; the railway on the owner's ruling, "Cut it at
    /// the walls"): no ribbon vertex lies inside the square by more than half its ribbon's width (an end section is square
    /// to the line, so a line meeting the wall at a slant reaches that far in, under the wall), and the four roads and the
    /// railway that meet there each reach to within 300 m outside it.</summary>
    [Test]
    public void TheRoadsAndTheRailwayStopAtTheOldCityWalls()
    {
        var map = BuildMap();
        try
        {
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            var (c, half) = Statecraft.Game.Map.Kits.SettlementKits.OldCityWalls(map.Frame);
            Assert.Greater(half, 1000f, "the square is not empty");
            var inside = new SortedSet<string>(); var reach = new SortedSet<string>();
            foreach (var plan in RouteLayer.Plan(RoutePaths.Parse(CommittedYaml, Bounds)))
            {
                float allowed = (plan.RailGrade ? RouteLayer.RailWidthM : RouteLayer.RoadWidthM) / 2f;
                var g = RouteLayer.Derive(plan, map.Frame, data, origin);
                foreach (var span in g.Routes)
                    for (int v = span.FirstVertex; v < span.FirstVertex + span.VertexCount; v++)
                    {
                        var p = g.Vertices[v];
                        float d = Mathf.Max(Mathf.Abs(p.x - c.x), Mathf.Abs(p.z - c.y));   // under half: inside the walls
                        if (d < half - allowed) inside.Add(span.Id);
                        else if (d < half + 300f) reach.Add(span.Id);
                    }
            }
            CollectionAssert.IsEmpty(inside, "ribbons inside the Old City's walls");
            CollectionAssert.IsSubsetOf(new[] { "jaffa_jerusalem_road", "jerusalem_hebron_beersheba_road", "jerusalem_nablus_nazareth_road",
                "jerusalem_jericho_road", "jaffa_jerusalem_railway" }, reach, "the roads and the railway that reach the walls");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Kits and routes together (#179 on main): no kit building, landmark or tree reaches into any route's
    /// corridor, half the drawn width plus RouteLayer.CorridorMarginM, the Burma Road included (the kits are built once,
    /// not per date). Checked by brute force against every segment, independent of KitGround's grid.</summary>
    [Test]
    public void NoKitBuildingOrTreeStandsOnARoute()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        try
        {
            map.Build();
            Assert.IsNotNull(map.KitStats, "Map.unity builds the kits");
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            var corridors = RouteLayer.Corridors(map.Frame, routes);
            // Plus one segment per unit clearing (a unit token in a town: docs/units.md, "Placement").
            Assert.AreEqual(corridors.Sum(c => c.line.Count - 1) + map.UnitClearings.Count, map.KitStats.CorridorSegments, "the map hands every route to the kits");
            Assert.IsTrue(routes.Any(r => r.Id == "burma_road"), "the dated Burma Road keeps its corridor from the start");
            // Every corridor clears its drawn ribbon by at least the roads' margin, and the roads' stays main's 250 m (so the
            // wider road moves no town kit).
            for (int i = 0; i < routes.Count; i++)
                Assert.GreaterOrEqual(corridors[i].halfWidth, RouteLayer.WidthOf(routes[i].Kind) / 2f + RouteLayer.RoadCorridorMarginM, routes[i].Id);
            Assert.AreEqual(250f, RouteLayer.RoadWidthM / 2f + RouteLayer.RoadCorridorMarginM, "the roads' kit corridor");

            // The same placement the map runs, with the same corridors.
            var g = new Statecraft.Game.Map.Kits.KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Paths.StreamingMap).meta.elev_min_m);
            foreach (var (line, half) in corridors) g.AddCorridor(line, half);
            var built = Statecraft.Game.Map.Kits.SettlementKits.Build(map.Settlements, g, out var lots);
            foreach (var k in built) Object.DestroyImmediate(k.mesh);
            foreach (var l in lots) g.Lots.Add((l.centre, l.radius));
            var trees = Statecraft.Game.Map.Kits.Vegetation.Plant(map.Settlements, g);
            Assert.Greater(lots.Count, 200); Assert.Greater(trees.Count, 1000);   // 264 lots and about 7,000 trees (306 lots without the corridors)

            float Clearance(Vector2 p) => corridors.Min(c =>
            {
                float best = float.MaxValue;
                for (int i = 0; i + 1 < c.line.Count; i++)
                    best = Mathf.Min(best, Statecraft.Game.Map.Kits.KitGround.SegmentDistance(p, c.line[i], c.line[i + 1]) - c.halfWidth);
                return best;
            });
            int badLots = lots.Count(l => Clearance(l.centre) < l.radius - 0.01f);
            int badTrees = trees.Count(t =>
            {
                Vector3 p = t.Item2.GetColumn(3);
                float crown = Statecraft.Game.Map.Kits.Vegetation.CrownRadiusM(t.Item1) * ((Vector3)t.Item2.GetColumn(0)).magnitude;
                return Clearance(new Vector2(p.x, p.z)) < crown - 0.01f;
            });
            TestContext.WriteLine($"{lots.Count} lots, {trees.Count} trees, {map.KitStats.CorridorSegments} route segments: {badLots} lots and {badTrees} trees on a route");
            Assert.AreEqual(0, badLots, "no building or landmark lot reaches a route corridor");
            Assert.AreEqual(0, badTrees, "no tree crown reaches a route corridor");
            Assert.AreEqual(map.Settlements.Count, lots.Select(l => l.id).Distinct().Count(), "every town still has buildings");
        }
        finally
        {
            Object.DestroyImmediate(map.gameObject);
            Statecraft.Game.Map.Kits.KitScale.Reset();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    /// <summary>I1 and M11: the promo's train is a showcase. Editor renders and `?shot` hold it still (the promo frames);
    /// `?train` or `?units` runs it; normal play runs the service train instead, from its sourced date (issue #668,
    /// ServiceTrainTests).</summary>
    [Test]
    public void ThePromosTrainIsAShowcase()
    {
        Assert.AreEqual(RouteLayer.TrainMode.Still, RouteLayer.ModeFor(playing: false, shot: false, requested: false), "editor renders");
        Assert.AreEqual(RouteLayer.TrainMode.Still, RouteLayer.ModeFor(playing: true, shot: true, requested: false), "?shot freezes it");
        Assert.AreEqual(RouteLayer.TrainMode.Still, RouteLayer.ModeFor(playing: true, shot: true, requested: true));
        Assert.AreEqual(RouteLayer.TrainMode.Moving, RouteLayer.ModeFor(playing: true, shot: false, requested: true));
        Assert.AreEqual(RouteLayer.TrainMode.Service, RouteLayer.ModeFor(playing: true, shot: false, requested: false), "normal play: the service train (#668)");
        Assert.IsTrue(RouteLayer.TrainRequested("https://x/index.html?train", new string[0], true));
        Assert.IsTrue(RouteLayer.TrainRequested("https://x/index.html?golden&units", new string[0], true));
        Assert.IsFalse(RouteLayer.TrainRequested("https://x/index.html?golden&smoke=5", new string[0], true));
        Assert.IsFalse(RouteLayer.TrainRequested("https://x/index.html?trainx", new string[0], true));
        Assert.IsTrue(RouteLayer.TrainRequested(null, new[] { "player", "-train" }, false));
        Assert.IsFalse(RouteLayer.TrainRequested(null, new[] { "player", "-shot" }, false));
        Assert.IsTrue(VisualShots.Requested("https://x/?golden&shot", new string[0], true), "the flag ModeForThisRun reads for ?shot");

        var map = BuildMap();
        try
        {
            var data = map.Terrain.terrainData;
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            var none = RouteLayer.Build(map.Terrain, map.Frame, routes, RouteLayer.TrainMode.None, RouteLayer.DefaultDate);
            Assert.IsNull(none.GetComponentInChildren<RouteTrain>(true), "no train");
            var moving = RouteLayer.Build(map.Terrain, map.Frame, routes, RouteLayer.TrainMode.Moving, RouteLayer.DefaultDate);
            Assert.IsNull(moving.GetComponentInChildren<RouteTrain>().StillHead, "the showcase runs");
            Object.DestroyImmediate(none); Object.DestroyImmediate(moving);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheStillTrainStandsOnTheLineInTheGameplayView()
    {
        var map = BuildMap();
        try
        {
            var train = RoutesRoot(map).GetComponentInChildren<RouteTrain>();
            Assert.IsNotNull(train.StillHead, "an editor build holds the train still (screenshots repeat)");
            Assert.AreEqual(2 + TrainModels.Coaches, train.Cars.Count);
            // Its middle by Mikve Israel, east of Jaffa, as the promo's train.
            var still = map.Frame.ToWorld(RouteTrain.StillAt.lon, RouteTrain.StillAt.lat, 0);
            var mid = Vector3.Lerp(train.Cars[0].position, train.Cars[train.Cars.Count - 1].position, 0.5f);
            Assert.Less(new Vector2(mid.x - still.x, mid.z - still.z).magnitude, 1500f);
            // About the promo's 8.8 km, and each car on the ribbon (ground + clearance + ride), heading inland (east) first.
            Assert.That(train.Length, Is.InRange(6000f, 10000f));
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            // On the ribbon's own centre height (the same max-of-samples rule, review M4) plus the ride height: never below
            // the ribbon's bed at the car's middle.
            foreach (var c in train.Cars)
            {
                var p = new Vector2(c.position.x, c.position.z);
                float bed = RouteLayer.GradeHeight(data, origin, p, new Vector2(c.forward.x, c.forward.z).normalized);
                Assert.AreEqual(bed + RouteTrain.RideM, c.position.y, 40f, c.name);
                Assert.GreaterOrEqual(c.position.y, RouteLayer.Ground(data, origin, p) + RouteLayer.ClearanceM, c.name);
            }
            Assert.Greater(train.Cars[0].position.x, train.Cars[train.Cars.Count - 1].position.x, "the locomotive leads towards Lydda");
            var cam = Camera.main; PromoPose.Gameplay03.Apply(cam.GetComponent<CameraRig>(), map.Frame);
            foreach (var c in train.Cars)
            {
                var vp = cam.WorldToViewportPoint(c.position);
                Assert.That(vp.x, Is.InRange(0.05f, 0.95f), c.name); Assert.That(vp.y, Is.InRange(0.05f, 0.95f), c.name);
            }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheTrainShuttlesWithTheLocomotiveLeading()
    {
        var map = BuildMap();
        try
        {
            var train = RoutesRoot(map).GetComponentInChildren<RouteTrain>();
            var (start, end) = train.Stretch;
            Assert.Greater(end - start, 2f * train.Length, "Jaffa to Lydda is longer than the train");
            var (h0, d0) = train.HeadAt(0f);
            Assert.AreEqual(start + train.Length, h0, 1e-2f); Assert.AreEqual(1, d0);
            float leg = (end - start - train.Length) / RouteTrain.SpeedMps + RouteTrain.DwellSeconds;
            var (h1, d1) = train.HeadAt(leg - 0.01f);
            Assert.AreEqual(end, h1, 1f, "at Lydda before turning"); Assert.AreEqual(1, d1);
            var (h2, d2) = train.HeadAt(leg + 0.01f);
            Assert.AreEqual(-1, d2, "back towards Jaffa");
            Assert.AreEqual(end - train.Length, h2, 5f, "the consist stays where it stopped; the locomotive is now at the other end");
            // Smooth: no jump between consecutive frames while running.
            float prev = train.HeadAt(1f).head;
            for (float t = 1f + 1f / 60f; t < leg - RouteTrain.DwellSeconds; t += 1f / 60f)
            {
                float h = train.HeadAt(t).head;
                Assert.Less(Mathf.Abs(h - prev), RouteTrain.SpeedMps / 60f + 0.01f);
                prev = h;
            }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TrainModelsAreRepeatable()
    {
        var a = TrainModels.Consist(); var b = TrainModels.Consist();
        int tris = 0;
        for (int i = 0; i < a.Count; i++)
        {
            CollectionAssert.AreEqual(a[i].mesh.vertices, b[i].mesh.vertices, a[i].name);
            CollectionAssert.AreEqual(a[i].mesh.triangles, b[i].mesh.triangles, a[i].name);
            tris += a[i].mesh.triangles.Length / 3;
        }
        Assert.Less(tris, 5000, $"train triangles {tris}");
        foreach (var c in a.Concat(b)) Object.DestroyImmediate(c.mesh);
    }

    [Test]
    public void TheRouteShaderRunsOnWebGL2()
    {
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/Route")));
        var rail = Resources.Load<Material>(RouteLayer.RailMaterialResource); var road = Resources.Load<Material>(RouteLayer.RoadMaterialResource);
        Assert.AreEqual(1f, rail.GetFloat("_Kind")); Assert.AreEqual(0f, road.GetFloat("_Kind"));
        Assert.Less(rail.renderQueue, road.renderQueue, "the railway draws first, so it stays on top where it crosses a road");
    }

    /// <summary>The look at gameplay03 (promo 03): the railway reads darker than the ground beside it (ballast and
    /// sleepers) and a road lighter (pale dirt), and switching the routes off restores the ground.</summary>
    [Test]
    public void AtTheGameplayPoseTheRailwayIsDarkAndTheRoadPale()
    {
        var map = BuildMap();
        try
        {
            RenderSample.TiltShift(false);
            VisualToggles.Set(RouteLayer.TrainToggle, false);
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            PromoPose.Gameplay03.Apply(rig, map.Frame);
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            // A railway point and a road point in open country near the pose's centre (east of Tel Aviv).
            Vector3 OnRoute(string id, double lon, double lat)
            {
                var line = RouteLayer.World(map.Frame, routes.First(r => r.Id == id));
                var want = map.Frame.ToWorld(lon, lat, 0);
                var best = line.OrderBy(p => (p - new Vector2(want.x, want.z)).sqrMagnitude).First();
                return new Vector3(best.x, RouteLayer.Ground(map.Terrain.terrainData, map.Terrain.transform.position, best), best.y);
            }
            var railAt = OnRoute("eastern_railway", 34.93, 32.06);
            var roadAt = OnRoute("tel_aviv_tulkarm_nablus_road", 34.92, 32.09);
            var px = new Vector2[2];
            var on = RenderSample.Render(cam, 1920, 1080, new[] { railAt, roadAt }, px);
            VisualToggles.Set(RouteLayer.RoutesToggle, false);
            var off = RenderSample.Render(cam, 1920, 1080);
            Color railOn = on.Mean((int)px[0].x, (int)px[0].y, 1), railOff = off.Mean((int)px[0].x, (int)px[0].y, 1);
            Color roadOn = on.Mean((int)px[1].x, (int)px[1].y, 1), roadOff = off.Mean((int)px[1].x, (int)px[1].y, 1);
            TestContext.WriteLine($"rail {railOn} over ground {railOff}; road {roadOn} over ground {roadOff}");
            Assert.Less(RenderSample.Luma(railOn), RenderSample.Luma(railOff) - 0.05f, "the railway is darker than the ground");
            Assert.Greater(RenderSample.Luma(roadOn), RenderSample.Luma(roadOff) + 0.03f, "the road is paler than the ground");
        }
        finally { VisualToggles.SetAll(true); Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A walk across a route at gameplay03's pose: the pixels from -<paramref name="halfM"/> to +halfM across the
    /// line at the line point nearest (lon, lat), with the routes on and off, and each pixel's offset (m) from the line.</summary>
    sealed class Across { public readonly List<Color> On = new List<Color>(), Off = new List<Color>(); public readonly List<float> M = new List<float>(); public readonly List<Vector2> P = new List<Vector2>(); }

    static List<Across> WalkAcross(MapScene map, Camera cam, List<Vector2> line, IEnumerable<int> at, float halfM, float stepM)
    {
        var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
        var world = new List<Vector3>(); var offs = new List<(int walk, float m)>();
        int walk = 0;
        foreach (int i in at)
        {
            var dir = (line[Mathf.Min(line.Count - 1, i + 1)] - line[Mathf.Max(0, i - 1)]).normalized; var side = new Vector2(dir.y, -dir.x);
            for (float m = -halfM; m <= halfM + 1e-3f; m += stepM)
            {
                var q = line[i] + side * m;
                world.Add(new Vector3(q.x, RouteLayer.Ground(data, origin, q), q.y)); offs.Add((walk, m));
            }
            walk++;
        }
        var px = new Vector2[world.Count];
        var on = RenderSample.Render(cam, 1920, 1080, world.ToArray(), px);
        VisualToggles.Set(RouteLayer.RoutesToggle, false);
        var off = RenderSample.Render(cam, 1920, 1080);
        VisualToggles.Set(RouteLayer.RoutesToggle, true);
        var walks = Enumerable.Range(0, walk).Select(_ => new Across()).ToList();
        var seen = new HashSet<(int, int, int)>();
        for (int k = 0; k < world.Count; k++)
        {
            int x = Mathf.FloorToInt(px[k].x), y = Mathf.FloorToInt(px[k].y);
            if (!seen.Add((offs[k].walk, x, y))) continue;
            var w = walks[offs[k].walk]; w.On.Add(on.At(x, y)); w.Off.Add(off.At(x, y)); w.M.Add(offs[k].m); w.P.Add(px[k]);
        }
        return walks;
    }

    /// <summary><paramref name="count"/> points of <paramref name="line"/> in the frame's middle, spread along the line; null
    /// when fewer lie there.</summary>
    static List<int> InFrame(MapScene map, Camera cam, List<Vector2> line, int count)
    {
        var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
        var inside = new List<int>();
        for (int i = 2; i + 2 < line.Count; i++)
        {
            var v = cam.WorldToViewportPoint(new Vector3(line[i].x, RouteLayer.Ground(data, origin, line[i]), line[i].y));
            if (v.z > 0 && v.x > 0.2f && v.x < 0.8f && v.y > 0.3f && v.y < 0.7f) inside.Add(i);
        }
        if (inside.Count < count) return null;
        return Enumerable.Range(0, count).Select(k => inside[(int)((k + 0.5f) * inside.Count / count)]).ToList();
    }

    /// <summary>Promo 03 and the video's 0:21 close-up draw two bright steel rails on the grey ballast. A rail is 0.036 of
    /// the 680 m bed, under a pixel at gameplay03, so it is drawn at least RailMinPx wide. A line detector at gameplay03
    /// (tilt-shift off, the train hidden): at 30 points along each of two railways in the frame's middle, the luma at each
    /// offset across the line, averaged along the line (so the sleepers, which cross it, average out and a line along it
    /// stays); on each side, the rail's offset (0.12 of the width, about 82 m) stands above the bed either side of it by
    /// RailContrast.</summary>
    [Test]
    public void TheRailsReadAsTwoBrightLinesAtGameplay03()
    {
        var map = BuildMap();
        try
        {
            RenderSample.TiltShift(false);
            VisualToggles.Set(RouteLayer.TrainToggle, false);
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            const float Step = 5f, Half = 200f;
            int n = Mathf.RoundToInt(2 * Half / Step) + 1;
            foreach (var id in new[] { "eastern_railway", "jaffa_jerusalem_railway" })
            {
                var line = RouteLayer.Resample(RouteLayer.World(map.Frame, routes.First(r => r.Id == id)), 60f).points;
                var at = InFrame(map, cam, line, 30);
                Assert.IsNotNull(at, $"{id} crosses the middle of the gameplay frame");
                var world = new List<Vector3>();
                foreach (int i in at)
                {
                    var dir = (line[Mathf.Min(line.Count - 1, i + 1)] - line[Mathf.Max(0, i - 1)]).normalized; var side = new Vector2(dir.y, -dir.x);
                    for (int k = 0; k < n; k++)
                    {
                        var q = line[i] + side * (-Half + k * Step);
                        world.Add(new Vector3(q.x, RouteLayer.GradeHeight(data, origin, q, dir), q.y));
                    }
                }
                var px = new Vector2[world.Count];
                var on = RenderSample.Render(cam, 1920, 1080, world.ToArray(), px);
                var profile = new float[n];
                for (int j = 0; j < world.Count; j++) profile[j % n] += RenderSample.Luma(on.At(Mathf.FloorToInt(px[j].x), Mathf.FloorToInt(px[j].y))) / at.Count;
                float Window(float lo, float hi, int sign, bool max)
                {
                    var v = Enumerable.Range(0, n).Select(k => (m: -Half + k * Step, l: profile[k]))
                        .Where(x => Mathf.Sign(x.m) == sign && Mathf.Abs(x.m) >= lo && Mathf.Abs(x.m) <= hi).Select(x => x.l).ToList();
                    return max ? v.Max() : v.Average();
                }
                TestContext.WriteLine($"{id}: profile along the line {string.Join(" ", profile.Select(l => l.ToString("0.000")))}");
                foreach (int sign in new[] { -1, +1 })
                {
                    float rail = Window(65f, 100f, sign, true), bed = 0.5f * (Window(20f, 50f, sign, false) + Window(115f, 145f, sign, false));
                    TestContext.WriteLine($"{id} side {sign}: rail {rail:0.000} bed {bed:0.000} contrast {rail - bed:0.000}");
                    Assert.GreaterOrEqual(rail - bed, RailContrast, $"{id}: the rail on side {sign} reads as a bright line along the bed");
                }
            }
        }
        finally { VisualToggles.SetAll(true); Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Bound for the rail test, set from its log.</summary>
    const float RailContrast = 0.15f;   // measured 0.18-0.21 (main's shader: 0.10-0.11)

    /// <summary>Promo 03's dirt tracks read on any ground. On the palest ground a road crosses in the gameplay03 frame (the
    /// sand and dry grass, where a pale road alone could melt in), across the road the verges are darker than its core by
    /// VergeStep, and either darker than the ground beside them with the routes off, or the core is paler than the ground
    /// under it by CoreAboveGround (the wide pale tracks, where the verges are soft and no longer a dark line).</summary>
    [Test]
    public void RoadsReadOnPaleGroundByTheirVerges()
    {
        var map = BuildMap();
        try
        {
            RenderSample.TiltShift(false);
            VisualToggles.Set(RouteLayer.TrainToggle, false);
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            // Every road point in the frame's middle, walked across; the palest ground (the routes off) first.
            var walks = new List<(string id, Across w, float ground)>();
            foreach (var r in routes.Where(r => r.Kind == RoutePaths.Road))
            {
                var line = RouteLayer.World(map.Frame, r);
                var at = InFrame(map, cam, line, 6);
                if (at == null) continue;
                foreach (var w in WalkAcross(map, cam, line, at, Road, 5f))
                {
                    var g = w.M.Select((m, k) => (m, k)).Where(x => Mathf.Abs(x.m) > 0.67f * Road).Select(x => RenderSample.Luma(w.Off[x.k])).ToList();
                    if (g.Count > 0) walks.Add((r.Id, w, g.Average()));
                }
            }
            var pale = walks.OrderByDescending(x => x.ground).Take(8).ToList();
            Assert.GreaterOrEqual(pale.Count, 6, "roads cross the gameplay frame");
            int read = 0;
            foreach (var (id, w, ground) in pale)
            {
                float Mean(float lo, float hi, List<Color> c) { var v = w.M.Select((m, k) => (m, k)).Where(x => Mathf.Abs(x.m) >= lo && Mathf.Abs(x.m) <= hi).Select(x => RenderSample.Luma(c[x.k])).ToList(); return v.Count == 0 ? float.NaN : v.Average(); }
                float core = Mean(0f, 0.1f * Road, w.On);
                // The verge's darkest pixel on each side (the verges lie about a third of the width out from the line).
                float VergeSide(int sign) { var v = w.M.Select((m, k) => (m, k)).Where(x => Mathf.Sign(x.m) == sign && Mathf.Abs(x.m) >= VergeFrom * Road && Mathf.Abs(x.m) <= VergeTo * Road).Select(x => RenderSample.Luma(w.On[x.k])).ToList(); return v.Count == 0 ? float.NaN : v.Min(); }
                float verge = Mathf.Max(VergeSide(-1), VergeSide(+1));   // the weaker side
                float off = Mean(VergeFrom * Road, VergeTo * Road, w.Off), under = Mean(0f, 0.1f * Road, w.Off);
                // It reads when the verges outline the core (darker than the core) and either the verges are darker than
                // the ground beside them (main's dark-verge look, #404) or the core is clearly paler than the ground under it
                // (the wide pale track's look).
                bool outlined = core - verge >= VergeStep;
                bool ok = outlined && (verge < off - VergeBelowGround || core - under >= CoreAboveGround);
                if (ok) read++;
                TestContext.WriteLine($"{id}: ground {ground:0.000} core {core:0.000} (under {under:0.000}) verge {verge:0.000} (off {off:0.000}) -> {(ok ? "reads" : "melts")}");
            }
            TestContext.WriteLine($"{read} of {pale.Count} pale-ground walks read");
            Assert.GreaterOrEqual(read / (float)pale.Count, PaleReadShare, "roads read on pale ground");
        }
        finally { VisualToggles.SetAll(true); Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Bounds for the road test, set from its log.</summary>
    const float VergeStep = 0.04f, VergeBelowGround = 0.01f, CoreAboveGround = 0.08f, PaleReadShare = 0.6f;
    /// <summary>The road's width, and where its verges lie across it (shares of the width from the line; 60-130 m when the
    /// road was 300 m wide).</summary>
    const float Road = RouteLayer.RoadWidthM, VergeFrom = 0.2f, VergeTo = 0.43f;

    /// <summary>Promo 03's roads are wide, pale dirt tracks (about 8 px at 1920 px); main's 300 m road drew a pale core of
    /// about 4 px between dark verges that almost vanished at gameplay03. Across every road in the frame's middle (6 points
    /// each, tilt-shift off, the train hidden), the pale band is the run of pixels the routes make paler than the ground
    /// (luma up by PaleStep), from the line out, measured on screen; its median width in pixels must reach RoadPalePx at gameplay03 (60 km) and more at close30
    /// (30 km). Logged per pose, with the median contrast of the band's core over the ground.</summary>
    [TestCase("gameplay03", 34.86, 32.00, 60000f, RoadPalePx)]
    [TestCase("close30", 34.83, 32.00, 30000f, 2 * RoadPalePx)]
    public void RoadsReadAsWidePaleTracks(string pose, double lon, double lat, float distance, float minPx)
    {
        var map = BuildMap();
        try
        {
            RenderSample.TiltShift(false);
            VisualToggles.Set(RouteLayer.TrainToggle, false);
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            new PromoPose(pose, lon, lat, distance, 10f).Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            var widths = new List<float>(); var lifts = new List<float>();
            foreach (var r in routes.Where(r => r.Kind == RoutePaths.Road))
            {
                var line = RouteLayer.World(map.Frame, r);
                var at = InFrame(map, cam, line, 6);
                if (at == null) continue;
                foreach (var w in WalkAcross(map, cam, line, at, Road, 2f))
                {
                    // The run of pale samples around the line (the walk is in order across it), measured on screen.
                    bool Pale(int k) => RenderSample.Luma(w.On[k]) - RenderSample.Luma(w.Off[k]) >= PaleStep;
                    int mid = Enumerable.Range(0, w.M.Count).OrderBy(k => Mathf.Abs(w.M[k])).First(), lo = mid, hi = mid;
                    if (!Pale(mid)) { widths.Add(0f); continue; }
                    while (lo > 0 && Pale(lo - 1)) lo--;
                    while (hi + 1 < w.M.Count && Pale(hi + 1)) hi++;
                    widths.Add((w.P[hi] - w.P[lo]).magnitude + 1f);
                    var coreLift = Enumerable.Range(0, w.M.Count).Where(k => Mathf.Abs(w.M[k]) <= 0.1f * Road).Select(k => RenderSample.Luma(w.On[k]) - RenderSample.Luma(w.Off[k])).ToList();
                    if (coreLift.Count > 0) lifts.Add(coreLift.Average());
                }
            }
            Assert.GreaterOrEqual(widths.Count, 12, "roads cross the frame");
            float Median(List<float> v) { var o = v.OrderBy(x => x).ToList(); return o[o.Count / 2]; }
            float px = Median(widths), lift = Median(lifts);
            TestContext.WriteLine($"{pose}: {widths.Count} walks, pale band median {px:0} px (p25 {widths.OrderBy(x => x).ElementAt(widths.Count / 4):0}), core over ground median {lift:+0.000}");
            Assert.GreaterOrEqual(px, minPx, $"{pose}: the roads read as wide pale tracks");
        }
        finally { VisualToggles.SetAll(true); Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Bounds for the road width test, set from its log.</summary>
    const float PaleStep = 0.03f, RoadPalePx = 6f;

    /// <summary>Issue #321: the baked ribbons (RouteRibbonFile.AssetPath, Editor RouteRibbonBake.Bake) are what the shipped
    /// heightmap and route file give now, byte for byte; the map scene places them without deriving; and what it places
    /// equals what the map's own terrain derives, vertex for vertex, with each route's range in the index.</summary>
    [Test]
    public void TheBakedRibbonsMatchTheRoutes()
    {
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
        var baked = AssetDatabase.LoadAssetAtPath<TextAsset>(RouteRibbonFile.AssetPath);
        Assert.IsNotNull(baked, $"{RouteRibbonFile.AssetPath} is missing: run Statecraft.Game.EditorTools.RouteRibbonBake.Bake");
        CollectionAssert.AreEqual(RouteRibbonFile.Bake(inputs), baked.bytes, "stale: re-run Statecraft.Game.EditorTools.RouteRibbonBake.Bake");

        var derivedAtLoad = new List<string>();
        void Watch(string message, string stack, LogType type) { if (message.Contains("deriving the route ribbons")) derivedAtLoad.Add(message); }
        Application.logMessageReceived += Watch;
        MapScene map;
        try { map = BuildMap(); }
        finally { Application.logMessageReceived -= Watch; }
        try
        {
            CollectionAssert.IsEmpty(derivedAtLoad, "the map places the bake");
            var (_, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16z);
            var source = RouteRibbonFile.Source.Of(heights, inputs.HeightmapJson, inputs.RoutesYaml);
            var plans = RouteLayer.Plan(RoutePaths.Parse(inputs.RoutesYaml, Bounds));
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            Assert.IsTrue(RouteRibbonFile.TryRead(baked.bytes, source, data.size, origin, plans, out var read, out var why), why);
            var placed = Ribbons(RoutesRoot(map), "Rail").Concat(Ribbons(RoutesRoot(map), "Roads"))
                .Select(f => f.sharedMesh).ToDictionary(m => m.name);
            Assert.AreEqual(plans.Count, placed.Count);
            for (int i = 0; i < plans.Count; i++)
            {
                var derived = RouteLayer.Derive(plans[i], map.Frame, data, origin);
                var name = plans[i].MeshName;
                CollectionAssert.AreEqual(derived.Vertices, read[i].Vertices, name);
                CollectionAssert.AreEqual(derived.Normals, read[i].Normals, name);
                CollectionAssert.AreEqual(derived.Uvs, read[i].Uvs, name);
                CollectionAssert.AreEqual(derived.Indices, read[i].Indices, name);
                CollectionAssert.AreEqual(derived.Routes, read[i].Routes, name);
                CollectionAssert.AreEqual(plans[i].Routes.Select(r => r.Id), read[i].Routes.Select(s => s.Id), name);
                Assert.AreEqual(derived.Vertices.Length, placed[name].vertexCount, name);
                Assert.AreEqual((uint)derived.Indices.Length, placed[name].GetIndexCount(0), name);
            }
            // Every route is in the index once, and its ranges tile its mesh.
            CollectionAssert.AreEquivalent(plans.SelectMany(p => p.Routes).Select(r => r.Id), read.SelectMany(g => g.Routes).Select(s => s.Id));
            foreach (var g in read)
                for (int k = 0; k < g.Routes.Length; k++)
                {
                    var s = g.Routes[k];
                    Assert.AreEqual(k == 0 ? 0 : g.Routes[k - 1].FirstVertex + g.Routes[k - 1].VertexCount, s.FirstVertex, s.Id);
                    Assert.AreEqual(k == 0 ? 0 : g.Routes[k - 1].FirstIndex + g.Routes[k - 1].IndexCount, s.FirstIndex, s.Id);
                    for (int i = s.FirstIndex; i < s.FirstIndex + s.IndexCount; i++)
                        Assert.That(g.Indices[i], Is.InRange(s.FirstVertex, s.FirstVertex + s.VertexCount - 1), s.Id);
                }

            // A bake made from other inputs is refused.
            var other = new[]
            {
                new RouteRibbonFile.Source(source.Resolution, source.HeightsCheck + 1, source.MapCheck, source.RoutesCheck),
                new RouteRibbonFile.Source(source.Resolution, source.HeightsCheck, source.MapCheck + 1, source.RoutesCheck),
                new RouteRibbonFile.Source(source.Resolution, source.HeightsCheck, source.MapCheck, source.RoutesCheck + 1),
            };
            foreach (var s in other) Assert.IsFalse(RouteRibbonFile.TryRead(baked.bytes, s, data.size, origin, plans, out _, out _));
            Assert.IsFalse(RouteRibbonFile.TryRead(baked.bytes, source, data.size + Vector3.up, origin, plans, out _, out _), "another terrain size");
            Assert.IsFalse(RouteRibbonFile.TryRead(baked.bytes.Take(baked.bytes.Length - 1).ToArray(), source, data.size, origin, plans, out _, out why));
            Assert.AreEqual("truncated", why);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Issue #321: a stale bake is refused with a warning and the ribbons are derived at load, the same meshes as
    /// a build without the bake.</summary>
    [Test]
    public void AStaleRibbonBakeIsDerivedAtLoad()
    {
        var map = BuildMap();
        try
        {
            var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
            var (_, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16z);
            var routes = RoutePaths.Parse(inputs.RoutesYaml, Bounds);
            var stale = RouteRibbonFile.Source.Of(heights, inputs.HeightmapJson, inputs.RoutesYaml + "\n# edited");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("deriving the route ribbons at load"));
            var root = RouteLayer.Build(map.Terrain, map.Frame, routes, RouteLayer.TrainMode.None, RouteLayer.DefaultDate, stale);
            try
            {
                var plans = RouteLayer.Plan(routes);
                var meshes = root.transform.Find(RouteLayer.RibbonsName).GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToList();
                Assert.AreEqual(plans.Count, meshes.Count);
                var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
                for (int i = 0; i < plans.Count; i++)
                {
                    var derived = RouteLayer.Derive(plans[i], map.Frame, data, origin);
                    Assert.AreEqual(plans[i].MeshName, meshes[i].name);
                    Assert.AreEqual(derived.Vertices.Length, meshes[i].vertexCount, plans[i].MeshName);
                    Assert.AreEqual((uint)derived.Indices.Length, meshes[i].GetIndexCount(0), plans[i].MeshName);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
}
