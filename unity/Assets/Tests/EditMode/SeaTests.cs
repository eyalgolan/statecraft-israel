#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 7: the water inside the map (since issue #36 a frame, not a skirt of sea, lies past its edge).</summary>
public class SeaTests
{
    const string SeaMatPath = "Assets/Materials/Sea.mat";

    [TearDown]
    public void Reset() { MapShaderGlobals.PinWaterTime(null); RenderSample.TiltShift(true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static MapScene OpenMap(bool reopen = true)
    {
        if (reopen) EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        // Every render here measures colour or local detail: Track A's tilt-shift blurs the top and bottom bands (task-3
        // carry m6, applied at the merge point), so it is off; the TearDown turns it back on.
        RenderSample.TiltShift(false);
        // The water and shore themselves: the settlement kits' shore palms, harbour and town blocks stand on and in front of
        // the coast samples, so they are off for every sea render (the map's teardown removes the switches).
        foreach (var n in new[] { "kits", "trees" }) if (VisualToggles.Names.Contains(n)) VisualToggles.Set(n, false);
        return map;
    }

    [Test]
    public void TheSkirtSeaUsesTheWaterShaderWithTheCoastTexture()
    {
        var sea = AssetDatabase.LoadAssetAtPath<Material>(SeaMatPath);
        Assert.AreEqual("Statecraft/Sea", sea.shader.name);
        Assert.IsFalse(ShaderUtil.ShaderHasError(sea.shader));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast), sea.GetTexture("_ScCoastTex"));
        Assert.GreaterOrEqual(sea.FindPass("DepthOnly"), 0, "in the depth prepass, like the selection band (m3-fix N6)");
        Assert.GreaterOrEqual(sea.FindPass("DepthNormals"), 0);
    }

    [Test]
    public void TheWaterShadersCompileForWebGL2()
    {
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/Sea")));
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat").shader));
    }

    /// <summary>Issue #195: the water inside the map is drawn by MapScene's TerrainWater, the terrain's surface over the
    /// water and its shore (MapBuilder.BuildWaterMesh) with a copy of Sea.mat in its terrain-water mode: added on top right
    /// after the terrain and before the other opaques, no depth write, no shadow, no collider. The mesh lies on the
    /// heightmap (its vertices are heightmap samples or flat fans), reaches the open sea and both rift lakes, and stays off
    /// inland hills.</summary>
    [Test]
    public void TheTerrainWaterLiesOnTheWaterWithTheSeaShader()
    {
        var map = OpenMap();
        var t = map.Terrain.transform.Find(MapScene.TerrainWaterName);
        Assert.IsNotNull(t, "TerrainWater under the terrain");
        Assert.AreEqual(map.Terrain.transform.position, t.position);
        Assert.IsNull(t.GetComponent<Collider>(), "clicks pick the terrain only");
        var r = t.GetComponent<MeshRenderer>();
        Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode);
        var m = r.sharedMaterial;
        Assert.AreEqual("Statecraft/Sea", m.shader.name);
        Assert.AreEqual(1f, m.GetFloat("_ScTerrainWater"));
        Assert.Greater(m.renderQueue, map.Terrain.materialTemplate.renderQueue, "after the terrain");
        Assert.Less(m.renderQueue, (int)UnityEngine.Rendering.RenderQueue.Geometry, "before boats, harbours and units");
        Assert.AreEqual(0f, m.GetFloat("_ScZWrite"));
        Assert.IsFalse(m.GetShaderPassEnabled("DepthOnly")); Assert.IsFalse(m.GetShaderPassEnabled("DepthNormals"));
        var mesh = t.GetComponent<MeshFilter>().sharedMesh;
        var verts = mesh.vertices;
        TestContext.WriteLine($"water mesh: {verts.Length} vertices, {mesh.triangles.Length / 3} triangles");
        Assert.Less(mesh.triangles.Length / 3, 1_000_000, "a light mesh, not a second terrain");
        // Every vertex lies on the terrain (a fan's centre on its flat block).
        var origin = map.Terrain.transform.position;
        for (int i = 0; i < verts.Length; i += 97)
            Assert.AreEqual(map.Terrain.SampleHeight(origin + verts[i]), verts[i].y, 0.25f, $"vertex {i} {verts[i]}");
        // Wet places are covered, an inland hill is not: horizontal hits on the mesh's triangles.
        bool Covers(double lon, double lat)
        {
            var p = map.Frame.ToWorld(lon, lat, 0) - origin;
            var tri = mesh.triangles;
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 a = verts[tri[i]], b = verts[tri[i + 1]], c = verts[tri[i + 2]];
                float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                float u = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.z - c.z)) / d;
                float v = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.z - c.z)) / d;
                if (u >= 0 && v >= 0 && u + v <= 1) return true;
            }
            return false;
        }
        Assert.IsTrue(Covers(34.60, 32.08), "the open sea west of Tel Aviv");
        Assert.IsTrue(Covers(35.50, 31.50), "the Dead Sea");
        Assert.IsTrue(Covers(35.59, 32.82), "the Sea of Galilee");
        Assert.IsFalse(Covers(35.21, 31.77), "Jerusalem's hills");
    }

    /// <summary>Issue #195: the baked water mesh (terrain_water.bytes, Editor TerrainWaterBake.Bake) is what the shipped
    /// heightmap gives now, byte for byte, and the map scene uses it; a stale bake is refused and the map derives the mesh
    /// instead, so it is never wrong, only slower to boot.</summary>
    [Test]
    public void TheBakedWaterMeshMatchesTheHeightmap()
    {
        var (meta, heights) = HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap);
        var (grid, indices) = MapBuilder.WaterMeshGrid(meta.width, MapBuilder.TerrainSize(meta), heights, meta.elev_min_m);
        var baked = AssetDatabase.LoadAssetAtPath<TextAsset>(MapTexturePaths.WaterMesh);
        Assert.IsNotNull(baked, MapTexturePaths.WaterMesh);
        CollectionAssert.AreEqual(MapBuilder.WaterMeshBytes(meta.width, heights, grid, indices), baked.bytes,
            "stale: re-run Statecraft.Game.EditorTools.TerrainWaterBake.Bake");
        Assert.IsTrue(MapBuilder.TryReadWaterMesh(baked.bytes, meta.width, heights, out _, out _, out var why), why);
        heights[145, 1995] += 0.001f;   // sample 997 x 300, one HeightsCheck reads
        Assert.IsFalse(MapBuilder.TryReadWaterMesh(baked.bytes, meta.width, heights, out _, out _, out why), "other heights");
        var derived = new System.Collections.Generic.List<string>();
        void Watch(string message, string stack, LogType type) { if (message.Contains("deriving the water mesh")) derived.Add(message); }
        Application.logMessageReceived += Watch;
        try
        {
            var map = OpenMap();
            var mesh = map.Terrain.transform.Find(MapScene.TerrainWaterName).GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(grid.Length, mesh.vertexCount);
        }
        finally { Application.logMessageReceived -= Watch; }
        CollectionAssert.IsEmpty(derived, "the scene's map uses the bake");
    }

    /// <summary>Issue #195: the terrain graph's function carries no water shading. Its size, not its arithmetic, cost about
    /// 3 ms a frame at gameplay03 in WebGL (even with the water toggled off, so no toggle measures it); the water belongs in
    /// Sea.shader's terrain-water mode.</summary>
    [Test]
    public void TheTerrainFunctionCarriesNoWaterShading()
    {
        var src = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText("Assets/Shaders/StatecraftTerrain.hlsl"), "//[^\n]*", "");   // comments may name them
        foreach (var call in new[] { "ScWaterShade", "ScBreakers", "ScHazeWater", "ddx(", "ddy(" })
            StringAssert.DoesNotContain(call, src);
    }

    static float Luma(Color c) => RenderSample.Luma(c);

    /// <summary>Rendered west of Tel Aviv: just offshore (about 120-200 m out, on the swash line: full to 180 m, gone by
    /// 324 m) is brighter than 1.2 km out (inside the turquoise shallows), which is lighter and greener than the open sea
    /// 24 km out. The shore is found in coast.png itself.</summary>
    [UnityTest]
    public IEnumerator ShallowsAreLighterThanTheOpenSeaAndFoamLinesTheShore()
    {
        var map = OpenMap();
        yield return null;
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));   // readable copy; row 0 = south
        int row = Mathf.FloorToInt((32.08f - 29.45f) / (33.35f - 29.45f) * coast.height);
        int col = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
        while (col > 0 && coast.GetPixel(col, row).r >= 0.5f) col--;          // walk west to the first water texel
        Object.DestroyImmediate(coast);
        Vector3 At(int c)
        {
            double lon = 34.2 + (c + 0.5) / 2048.0 * (35.9 - 34.2), lat = 29.45 + (row + 0.5) / 2048.0 * (33.35 - 29.45);
            var w = map.Frame.ToWorld(lon, lat, 0); w.y = map.Terrain.SampleHeight(w); return w;
        }
        Vector3 foam = At(col - 2), shallow = At(col - 15), deep = At(col - 300);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = shallow; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[2];
        var near = RenderSample.Render(cam, 960, 540, new[] { foam, shallow }, px);
        Color f = near.Mean((int)px[0].x, (int)px[0].y, 1), s = near.Mean((int)px[1].x, (int)px[1].y, 1);
        rig.LookAt = deep; rig.Apply();
        var far = RenderSample.Render(cam, 960, 540, new[] { deep }, px);
        Color d = far.Mean((int)px[0].x, (int)px[0].y, 2);
        TestContext.WriteLine($"foam {f}, shallow {s}, deep {d}");
        Assert.Greater(Luma(f), Luma(s) + 0.02f, "foam at the shore");
        Assert.Greater(Luma(s), Luma(d) + 0.05f, "shallows lighter");
        Assert.Greater(s.g / s.b, d.g / d.b, "shallows greener");
        // promo-sea (open item 3, issue #38): the open sea is the promo's muted steel blue, not a saturated navy. HSV
        // saturation before the grade: 0.91 with Task 7's ramp, 0.58 now (the grade lifts it: promo 03's sea measures 0.49).
        Color.RGBToHSV(d, out _, out float deepSat, out _);
        Assert.Less(deepSat, 0.7f, "the open sea is muted");
    }

    /// <summary>Review N3: with the settlement kits on, the beach along Tel Aviv's shore (inside its paved footprint, with its
    /// shore palms) still reads as pale sand. One beach sample per coast-texture row from 32.06 to 32.12 N (Tel Aviv's own
    /// beach: Jaffa's old town and harbour meet the sea south of it, and the Yarkon's mouth lies north of it), 200 m inland of
    /// the shore; a palm crown or a block stands in front of some of them from this pose, so at least
    /// <see cref="SandShareBesideTelAviv"/> of them must be sand, and those must be lighter than open coastal plain outside
    /// every town's footprint (5 km inland at 32.22 N, north of Herzliya) by <see cref="BeachOverPlain"/>.</summary>
    [UnityTest]
    public IEnumerator TheBeachBesideTelAvivStaysSandWithTheKitsOn()
    {
        var map = OpenMap();
        yield return null;
        foreach (var n in new[] { "kits", "trees" }) VisualToggles.Set(n, true);
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));
        int RowOf(float lat) => Mathf.FloorToInt((lat - 29.45f) / (33.35f - 29.45f) * coast.height);
        int Shore(int row)
        {
            int c = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
            while (c > 0 && coast.GetPixel(c, row).r >= 0.5f) c--;
            return c;
        }
        Vector3 At(int c, int r)
        {
            double lon = 34.2 + (c + 0.5) / 2048.0 * (35.9 - 34.2), lat = 29.45 + (r + 0.5) / 2048.0 * (33.35 - 29.45);
            var w = map.Frame.ToWorld(lon, lat, 0); w.y = map.Terrain.SampleHeight(w); return w;
        }
        var beaches = new System.Collections.Generic.List<Vector3>();
        for (int r = RowOf(32.06f); r <= RowOf(32.12f); r++) beaches.Add(At(Shore(r) + 3, r));
        int rowN = RowOf(32.22f); var plain = At(Shore(rowN) + 60, rowN);
        Object.DestroyImmediate(coast);
        bool Paved(Vector3 p) => map.Settlements.Any(t =>
        {
            var c = map.Frame.ToWorld(t.Lon, t.Lat, 0);
            return new Vector2(p.x - c.x, p.z - c.z).magnitude < 1.1f * Statecraft.Game.Map.Kits.SettlementKits.FootprintRadiusM(t, map.Hexes.hex_size_km * 1000f);
        });
        Assert.IsTrue(beaches.All(Paved), "the beach samples lie inside a town's footprint");
        Assert.IsFalse(Paved(plain), "the plain reference lies outside every footprint");
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = beaches[beaches.Count / 2]; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[beaches.Count];
        var shot = RenderSample.Render(cam, 1920, 1080, beaches.ToArray(), px);
        var sand = new System.Collections.Generic.List<Color>();
        for (int i = 0; i < beaches.Count; i++)
        {
            var c = shot.Mean((int)px[i].x, (int)px[i].y, 1);
            if (Luma(c) > 0.72f && c.b / c.g > 0.62f) sand.Add(c);
        }
        var one = new Vector2[1];
        rig.LookAt = plain; rig.Apply();
        Color l = RenderSample.Render(cam, 960, 540, new[] { plain }, one).Mean((int)one[0].x, (int)one[0].y, 2);
        float share = sand.Count / (float)beaches.Count, sandLuma = sand.Count > 0 ? sand.Average(c => Luma(c)) : 0f;
        TestContext.WriteLine($"{sand.Count} of {beaches.Count} beach samples along Tel Aviv read as sand ({share:P0}), mean luma {sandLuma:0.000}; " +
            $"plain north of Herzliya {l} (luma {Luma(l):0.000})");
        Assert.GreaterOrEqual(share, SandShareBesideTelAviv, "the kits bury Tel Aviv's beach");
        Assert.Greater(sandLuma, Luma(l) + BeachOverPlain, "the beach is lighter than the open plain");
    }

    /// <summary>Floors for TheBeachBesideTelAvivStaysSandWithTheKitsOn.</summary>
    const float SandShareBesideTelAviv = 0.75f, BeachOverPlain = 0.10f;

    /// <summary>promo-sea: a sand strip lines the Mediterranean shore (promo 03's beach). Rendered on the same row west of
    /// Tel Aviv: about 200 m inland of the shore the ground is pale sand, lighter than the plain 5 km inland and less
    /// yellow-green than the bare coastal edge it covers. The map is built without the settlement kits, which pave the ground
    /// of a town's footprint (Tel Aviv's covers that plain sample).</summary>
    [UnityTest]
    public IEnumerator ASandStripLinesTheMediterraneanShore()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().ConfigureKits(null, null);   // the scene is not saved
        var map = OpenMap(reopen: false);
        yield return null;
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));   // readable copy; row 0 = south
        int row = Mathf.FloorToInt((32.08f - 29.45f) / (33.35f - 29.45f) * coast.height);
        int col = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
        while (col > 0 && coast.GetPixel(col, row).r >= 0.5f) col--;          // walk west to the first water texel
        Object.DestroyImmediate(coast);
        Vector3 At(int c)
        {
            double lon = 34.2 + (c + 0.5) / 2048.0 * (35.9 - 34.2), lat = 29.45 + (row + 0.5) / 2048.0 * (33.35 - 29.45);
            var w = map.Frame.ToWorld(lon, lat, 0); w.y = map.Terrain.SampleHeight(w); return w;
        }
        Vector3 beach = At(col + 3), inland = At(col + 60);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = Vector3.Lerp(beach, inland, 0.5f); rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[2];
        var r = RenderSample.Render(cam, 960, 540, new[] { beach, inland }, px);
        Color b = r.Mean((int)px[0].x, (int)px[0].y, 1), l = r.Mean((int)px[1].x, (int)px[1].y, 2);
        TestContext.WriteLine($"beach {b}, plain {l}");
        // Without the strip the same pixel is the plain's pale coastal edge, RGBA(0.716, 0.656, 0.378): luma 0.65, b/g 0.58.
        // With it RGBA(0.906, 0.771, 0.516): luma 0.78, b/g 0.67.
        Assert.Greater(Luma(b), Luma(l) + 0.32f, "the sand is lighter than the plain (0.25 without the strip)");
        Assert.Greater(Luma(b), 0.72f, "pale sand");
        Assert.Greater(b.b / b.g, 0.62f, "sand, not the plain's yellow-green");
    }

    /// <summary>The coast.png column of the first water texel west of (34.95 E, lat) and its row; row 0 = south.</summary>
    static (int col, int row) ShoreTexel(float lat)
    {
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));   // readable copy
        int row = Mathf.FloorToInt((lat - 29.45f) / (33.35f - 29.45f) * coast.height);
        int col = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
        while (col > 0 && coast.GetPixel(col, row).r >= 0.5f) col--;
        Object.DestroyImmediate(coast);
        return (col, row);
    }

    static Vector3 TexelWorld(MapScene map, int col, int row)
    {
        double lon = 34.2 + (col + 0.5) / 2048.0 * (35.9 - 34.2), lat = 29.45 + (row + 0.5) / 2048.0 * (33.35 - 29.45);
        var w = map.Frame.ToWorld(lon, lat, 0); w.y = map.Terrain.SampleHeight(w); return w;
    }

    /// <summary>Sea round 3 (#388): beyond the swash line, a broken white breaker stands clear in the surf at the gameplay
    /// zoom, and the surf is lace, not a solid band. What this guarantees is the swash plus at least one breaker; promo 03
    /// shows 2-4 broken lines, and that gap stays open until #519's grade is in. At TheBreakersAreFilteredToThePixel's pose
    /// (30 km at 960x540: about 45 m a pixel, as gameplay03), the water's clock pinned at four moments of a breaker cycle, a
    /// one-pixel line is walked across the coast from the shore out to 1.8 km, west of Tel Aviv (32.08 N) and south of Jaffa
    /// (31.95 N). A pixel is foam when its luma is at least 0.72 and its channels lie within 30/255 (white, not turquoise),
    /// and a line is a run of at least 2 foam pixels; the run that starts at the shore is the swash and is not counted as a
    /// breaker. On each line at 3 or more of the 4 moments: at least one breaker past the swash, and the median foam share
    /// of the stretch from the farthest foam pixel to the shore is within <see cref="LaceShare"/>. 3 of 4, not 4 of 4: the
    /// branch shows a breaker at all 4 moments, but at 32.08 N, t=110 it is 2 px wide, exactly the run minimum (the others
    /// are 4-9 px), so one moment may read as none on another GPU; main shows one at only 2 of the 4.</summary>
    [UnityTest]
    public IEnumerator TheSurfShowsABrokenBreakerBeyondTheSwashAtTheGameplayZoom()
    {
        var map = OpenMap();
        yield return null;
        VisualToggles.Set("grid", false);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var log = new System.Text.StringBuilder();
        foreach (var lat in new[] { 32.08f, 31.95f })
        {
            var (col, row) = ShoreTexel(lat);
            const float texelM = 161529f / 2048f;   // a coast.png texel east-west
            Vector3 shore = TexelWorld(map, col, row), reach = TexelWorld(map, col - Mathf.RoundToInt(1800f / texelM), row);
            rig.LookAt = reach; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            int laced = 0; var shares = new System.Collections.Generic.List<float>();
            foreach (var t in new[] { 100f, 105f, 110f, 115f })
            {
                MapShaderGlobals.PinWaterTime(t);
                var px = new Vector2[2];
                var r = RenderSample.Render(cam, 960, 540, new[] { shore, reach }, px);
                int n = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(px[1].x - px[0].x), Mathf.Abs(px[1].y - px[0].y)));
                var foam = new bool[n + 1];   // index 0 at the shore
                var trace = new System.Text.StringBuilder();
                for (int k = 0; k <= n; k++)
                {
                    var p = Vector2.Lerp(px[0], px[1], k / (float)n);
                    var c = r.At(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
                    foam[k] = Luma(c) >= 0.72f && Mathf.Max(c.r, c.g, c.b) - Mathf.Min(c.r, c.g, c.b) <= 30f / 255f;
                    trace.Append(foam[k] ? '#' : '.');
                }
                int far = System.Array.LastIndexOf(foam, true), breakers = 0, run = 0, on = 0;
                for (int k = 0; k <= far + 1; k++)
                {
                    if (k <= far && foam[k]) { run++; on++; }
                    else { if (run >= 2 && k - run > 0) breakers++; run = 0; }   // a run from index 0 is the swash
                }
                float share = far >= 0 ? on / (float)(far + 1) : 0f;
                if (breakers >= 1) laced++;
                shares.Add(share);
                log.AppendLine($"{lat:0.00} N t={t}: swash {(foam[0] ? "yes" : "no")}, {breakers} breakers past it, share {share:0.00}  {trace}");
            }
            shares.Sort();
            float median = 0.5f * (shares[1] + shares[2]);
            TestContext.WriteLine(log.ToString());
            Assert.GreaterOrEqual(laced, 3, $"{lat} N: a breaker past the swash at 3 of the 4 moments");
            Assert.That(median, Is.InRange(LaceShare.x, LaceShare.y), $"{lat} N: the surf is lace, not a solid band");
        }
    }

    /// <summary>TheSurfShowsABrokenBreakerBeyondTheSwashAtTheGameplayZoom's foam share band (promo 03 south of Jaffa: a median
    /// share of 0.56, plan C1). The branch's median at 32.08 N is 0.67, close to the upper bound: another GPU or driver may
    /// read it a little differently.</summary>
    static readonly Vector2 LaceShare = new Vector2(0.3f, 0.7f);

    /// <summary>promo-sea review I2 / M5 / N1: the breakers are box-filtered over the pixel, so they never alias. Around
    /// the surf zone west of Tel Aviv, at the gameplay footprint (30 km at 960x540: about 45 m a pixel, the same as
    /// gameplay03's 60 km at 1920x1080), where an unfiltered crest is sub-pixel but not yet faded, every water pixel of a
    /// 49x49 patch matches the mean of its 4x4 block in the same view rendered at 4x, at four moments of a breaker cycle.
    /// The breakers still show (the surf pixel's luma spans a range over one cycle), and at the overview (250 km) they are
    /// their mean: two moments 0.5 s apart render the same. Grid off: its pixel-width lines differ between resolutions by
    /// design.</summary>
    [UnityTest]
    public IEnumerator TheBreakersAreFilteredToThePixel()
    {
        var map = OpenMap();
        yield return null;
        VisualToggles.Set("grid", false);
        var (col, row) = ShoreTexel(32.08f);
        var surf = TexelWorld(map, col - 5, row);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = surf; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[1];
        var errs = new System.Collections.Generic.List<double>();
        int cx = 0, cy = 0;
        foreach (var t in new[] { 100f, 105f, 110f, 115f })   // four moments over one breaker cycle (20 s)
        {
            MapShaderGlobals.PinWaterTime(t);
            var lo = RenderSample.Render(cam, 960, 540, new[] { surf }, px);
            cx = (int)px[0].x; cy = (int)px[0].y;
            var hi = RenderSample.Render(cam, 3840, 2160);
            for (int y = cy - 24; y <= cy + 24; y++)
                for (int x = cx - 24; x <= cx + 24; x++)
                {
                    // Water pixels only: the shoreline's own step and the settlement markers' edges are not
                    // filtered by design, so a pixel counts only when it and all 16 of its 4x samples are water or
                    // foam (blue at least red - 0.03; sand, plain and markers are clearly red over blue).
                    Color sum = Color.clear; bool water = lo.At(x, y).b > lo.At(x, y).r - 0.03f;
                    for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++)
                    {
                        var c = hi.At(4 * x + i, 4 * y + j); sum += c; water &= c.b > c.r - 0.03f;
                    }
                    if (water) errs.Add(Mathf.Abs(Luma(lo.At(x, y)) - Luma(sum / 16f)));
                }
        }
        errs.Sort();
        double worst = errs[errs.Count - 1], p99 = errs[(int)(0.99 * (errs.Count - 1))], err = 0;
        foreach (var e in errs) err += e;
        err /= errs.Count;
        float lMin = 1f, lMax = 0f;
        for (int k = 0; k < 8; k++)
        {
            MapShaderGlobals.PinWaterTime(100f + k * 2.5f);   // one breaker cycle is 1 / ScBreakerRate = 20 s
            var r = RenderSample.Render(cam, 960, 540);
            float l = Luma(r.Mean(cx, cy, 0)); lMin = Mathf.Min(lMin, l); lMax = Mathf.Max(lMax, l);
        }
        rig.Distance = 250000f; rig.Apply();
        MapShaderGlobals.PinWaterTime(100f);
        var a = RenderSample.Render(cam, 960, 540, new[] { surf }, px);
        MapShaderGlobals.PinWaterTime(100.5f);
        var b = RenderSample.Render(cam, 960, 540);
        float far = Mathf.Abs(Luma(a.Mean((int)px[0].x, (int)px[0].y, 0)) - Luma(b.Mean((int)px[0].x, (int)px[0].y, 0)));
        TestContext.WriteLine($"1x against 4x, {errs.Count} water pixels of 49x49 at 4 moments: mean {err:0.0000}, p99 {p99:0.0000}, worst {worst:0.0000}; breaker cycle luma {lMin:0.000}..{lMax:0.000}; overview 0.5 s apart {far:0.0000}");
        // Red with Round 0's unfiltered crest (pow(tri, 5), 160 m, faded only from 40 m pixels): p99 0.020, worst 0.062.
        // Green with the box filter: p99 0.008, worst 0.018.
        Assert.Less(p99, 0.014, "each water pixel is its footprint's mean (no aliasing)");
        Assert.Less(worst, 0.035, "no breaker crest aliases on any water pixel");
        Assert.Greater(lMax - lMin, 0.03f, "the breakers roll through at the gameplay zoom");
        Assert.Less(far, 0.01f, "the overview sees the breakers' mean, not a crawl");
    }

    /// <summary>Bug B10: the open sea showed a blocky checker up close. The waves came from value noise on map-aligned
    /// lattices (700 m swell, 170 m chop), differenced along the axes, so the shading jumped at the lattice lines. Rendered
    /// straight down (orthographic, 4 m a pixel, 25 km offshore, the chop and tilt at full strength), the luma gradient
    /// across each axis is folded by its position within each lattice's cell (16 bins): a block grid piles its edges into a
    /// few bins, isotropic waves spread them evenly. The block-edge energy is (largest bin - smallest) / mean.</summary>
    [UnityTest]
    public IEnumerator TheOpenSeaHasNoBlockGrid()
    {
        var map = OpenMap();
        yield return null;
        VisualToggles.Set("grid", false);
        MapShaderGlobals.PinWaterTime(100f);
        var centre = map.Frame.ToWorld(34.45, 32.0, 0); centre.y = map.Terrain.SampleHeight(centre);
        var origin = map.Terrain.transform.position;
        const int n = 1024; const float metresPerPixel = 4f;
        var go = new GameObject("TopSea"); var cam = go.AddComponent<Camera>();
        try
        {
            cam.orthographic = true; cam.orthographicSize = n * metresPerPixel / 2f; cam.aspect = 1f;
            cam.transform.position = centre + Vector3.up * 20000f; cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.nearClipPlane = 100f; cam.farClipPlane = 60000f;
            var r = RenderSample.Render(cam, n, n);
            float x0 = centre.x - origin.x - n * metresPerPixel / 2f, z0 = centre.z - origin.z - n * metresPerPixel / 2f;
            float worst = 0f; string log = "";
            foreach (var cell in new[] { 700f, 170f })
                foreach (var alongX in new[] { true, false })
                {
                    var sum = new double[16]; var count = new int[16];
                    for (int j = 1; j < n - 1; j++)
                        for (int i = 1; i < n - 1; i++)
                        {
                            // The edge between pixel (i, j) and its next neighbour lies at map metre x0 + (i + 1) * 4 (x
                            // east; image row 0 is the south edge, so rows run north like map z).
                            float edgeM = (alongX ? x0 + (i + 1) * metresPerPixel : z0 + (j + 1) * metresPerPixel) / cell;
                            int bin = Mathf.Clamp((int)((edgeM - Mathf.Floor(edgeM)) * 16f), 0, 15);
                            float d = alongX ? Luma(r.At(i + 1, j)) - Luma(r.At(i, j)) : Luma(r.At(i, j + 1)) - Luma(r.At(i, j));
                            sum[bin] += Mathf.Abs(d); count[bin]++;
                        }
                    double lo = double.MaxValue, hi = 0, all = 0;
                    for (int b = 0; b < 16; b++) { double m = sum[b] / count[b]; lo = System.Math.Min(lo, m); hi = System.Math.Max(hi, m); all += m / 16; }
                    float energy = (float)((hi - lo) / all);
                    worst = Mathf.Max(worst, energy);
                    log += $"{cell:0} m {(alongX ? "x" : "z")} {energy:0.000}; ";
                }
            TestContext.WriteLine($"block-edge energy: {log}worst {worst:0.000}");
            Assert.Less(worst, 0.25f, "no lattice of block edges in the sea's shading");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Issue #195 review: the terrain water's depth pull follows an orthographic camera's parallel rays. Pulled
    /// towards the camera's position instead, the water slid towards the view's axis, and the terrain's black water showed
    /// between it and the shore. Straight down over the coast south of Tel Aviv, 30 km across with the axis 12 km offshore,
    /// no pixel is black.</summary>
    [UnityTest]
    public IEnumerator TheTerrainWaterStaysPutUnderAnOrthographicCamera()
    {
        var map = OpenMap();
        yield return null;
        var centre = map.Frame.ToWorld(34.62, 32.0, 0); centre.y = 0f;
        var go = new GameObject("TopCoast"); var cam = go.AddComponent<Camera>();
        try
        {
            const int n = 512;
            cam.orthographic = true; cam.orthographicSize = 15000f; cam.aspect = 1f;
            cam.transform.position = centre + Vector3.up * 20000f; cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.nearClipPlane = 100f; cam.farClipPlane = 60000f;
            var r = RenderSample.Render(cam, n, n);
            int black = r.Pixels.Count(c => Mathf.Max(c.r, Mathf.Max(c.g, c.b)) < 0.02f);
            TestContext.WriteLine($"black pixels: {black} of {n * n}");
            Assert.AreEqual(0, black, "the water covers the terrain's black water up to the shore");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Sea boats (#388, the 2026-10-13 gap audit): at the gameplay zoom the open sea calms away from the shore,
    /// while the water near it keeps #402's texture. Rendered at gameplay03's distance (60 km, about 46 m a pixel) with the
    /// water's clock pinned and the grid off, on one coast-texture row south of Jaffa (31.95 N): the fine texture (the mean
    /// change between neighbouring pixels' luma, over a 25 x 25 window) 20 km out must be under
    /// <see cref="FarOverNearTexture"/> of that 2.2 km out (past the surf, in the shallows' outer edge). On main the far
    /// sea was the busier (far / near 0.89: its ripple ran at full strength, against the shallows' 40-100%); calmed, 0.23.</summary>
    [UnityTest]
    public IEnumerator TheOpenSeaCalmsAwayFromTheShore()
    {
        var map = OpenMap();
        yield return null;
        VisualToggles.Set("grid", false);
        MapShaderGlobals.PinWaterTime(100f);
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));
        int row = Mathf.FloorToInt((31.95f - 29.45f) / (33.35f - 29.45f) * coast.height);
        int col = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
        while (col > 0 && coast.GetPixel(col, row).r >= 0.5f) col--;
        Object.DestroyImmediate(coast);
        Vector3 At(float offshoreM)
        {
            // The shore texel's centre, then due west (world -x) out to sea.
            var shore = map.Frame.ToWorld(34.2 + (col + 0.5) / 2048.0 * (35.9 - 34.2), 29.45 + (row + 0.5) / 2048.0 * (33.35 - 29.45), 0);
            var w = shore + Vector3.left * offshoreM; w.y = map.Terrain.SampleHeight(w); return w;
        }
        Vector3 near = At(2200f), far = At(20000f);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = (near + far) * 0.5f; rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[2];
        var r = RenderSample.Render(cam, 1920, 1080, new[] { near, far }, px);
        float Texture(Vector2 p)
        {
            double sum = 0; int n = 0;
            for (int y = (int)p.y - 12; y <= (int)p.y + 12; y++)
                for (int x = (int)p.x - 12; x <= (int)p.x + 12; x++)
                {
                    float l = Luma(r.At(x, y));
                    sum += Mathf.Abs(Luma(r.At(x + 1, y)) - l) + Mathf.Abs(Luma(r.At(x, y + 1)) - l); n++;
                }
            return (float)(sum / n);
        }
        float tn = Texture(px[0]), tf = Texture(px[1]);
        TestContext.WriteLine($"fine texture 2.2 km out {tn:0.0000} (at {px[0]}), 20 km out {tf:0.0000} (at {px[1]}): far / near {tf / tn:0.00}");
        Assert.Greater(tn, 0.005f, "the sea near the shore keeps its texture");
        Assert.Less(tf / tn, FarOverNearTexture, "the open sea far from the shore is calmer than near it");
    }

    /// <summary>Bound for TheOpenSeaCalmsAwayFromTheShore.</summary>
    const float FarOverNearTexture = 0.6f;

    /// <summary>Sea boats (#388): at the overview (diorama02's pose, 190-300 m pixels) the sea along the coast reads as a
    /// wide, lighter teal shelf over the deep blue, as promo 02's does: on the row of TheOpenSeaCalmsAwayFromTheShore, the
    /// water 5 km out is lighter than 25 km out by at least <see cref="OverviewShelfOverDeep"/> in luma (a mean over 5 x 5
    /// pixels, the water's clock pinned, the grid off). Main measured 0.048 (the depth ramp alone), the band 0.086.</summary>
    [UnityTest]
    public IEnumerator TheOverviewSeaIsLighterAlongTheCoast()
    {
        var map = OpenMap();
        yield return null;
        VisualToggles.Set("grid", false);
        MapShaderGlobals.PinWaterTime(100f);
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));
        int row = Mathf.FloorToInt((31.95f - 29.45f) / (33.35f - 29.45f) * coast.height);
        int col = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
        while (col > 0 && coast.GetPixel(col, row).r >= 0.5f) col--;
        Object.DestroyImmediate(coast);
        var shore = map.Frame.ToWorld(34.2 + (col + 0.5) / 2048.0 * (35.9 - 34.2), 29.45 + (row + 0.5) / 2048.0 * (33.35 - 29.45), 0);
        Vector3 At(float offshoreM) { var w = shore + Vector3.left * offshoreM; w.y = map.Terrain.SampleHeight(w); return w; }
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var px = new Vector2[2];
        var r = RenderSample.Render(cam, 1920, 1080, new[] { At(5000f), At(25000f) }, px);
        Color shelf = r.Mean((int)px[0].x, (int)px[0].y, 2), deep = r.Mean((int)px[1].x, (int)px[1].y, 2);
        TestContext.WriteLine($"overview: 5 km out {shelf} (luma {Luma(shelf):0.000}, at {px[0]}), 25 km out {deep} (luma {Luma(deep):0.000}, at {px[1]})");
        Assert.Greater(Luma(shelf), Luma(deep) + OverviewShelfOverDeep, "the coastal shelf is lighter than the deep sea at the overview");
    }

    /// <summary>Bound for TheOverviewSeaIsLighterAlongTheCoast.</summary>
    const float OverviewShelfOverDeep = 0.07f;

    /// <summary>World points on the water <paramref name="offshoreM"/> due west of the shore, on coast-texture rows from 31.6 to
    /// 32.4 N every 0.1 degree (south of Gaza's latitude to north of Netanya's): the shore is each row's first water texel
    /// walking west from 34.95 E.</summary>
    static Vector3[] OffshorePoints(MapScene map, float[] offshoreM)
    {
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));
        var points = new System.Collections.Generic.List<Vector3>();
        for (float lat = 31.6f; lat <= 32.41f; lat += 0.1f)
        {
            int row = Mathf.FloorToInt((lat - 29.45f) / (33.35f - 29.45f) * coast.height);
            int col = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
            while (col > 0 && coast.GetPixel(col, row).r >= 0.5f) col--;
            var shore = map.Frame.ToWorld(34.2 + (col + 0.5) / 2048.0 * (35.9 - 34.2), 29.45 + (row + 0.5) / 2048.0 * (33.35 - 29.45), 0);
            foreach (var d in offshoreM) { var w = shore + Vector3.left * d; w.y = map.Terrain.SampleHeight(w); points.Add(w); }
        }
        Object.DestroyImmediate(coast);
        return points.ToArray();
    }

    /// <summary>The largest channel change (0-255) over 3 x 3 pixels round each projected point between a render with the
    /// "seacalm" toggle on and one with it off (the water's clock pinned, the grid and the tilt-shift off), at a pose and
    /// a resolution.</summary>
    static float SeaCalmChange(MapScene map, PromoPose pose, Vector3[] points, out int onScreen, int width = 1920, int height = 1080)
    {
        VisualToggles.Set("grid", false);
        MapShaderGlobals.PinWaterTime(100f);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        pose.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var px = new Vector2[points.Length];
        var on = RenderSample.Render(cam, width, height, points, px);
        VisualToggles.Set("seacalm", false);
        var off = RenderSample.Render(cam, width, height, points, px);
        VisualToggles.Set("seacalm", true);
        float worst = 0f; onScreen = 0;
        foreach (var p in px)
        {
            if (p.x < 2 || p.y < 2 || p.x > width - 3 || p.y > height - 3) continue;
            onScreen++;
            for (int y = (int)p.y - 1; y <= (int)p.y + 1; y++)
                for (int x = (int)p.x - 1; x <= (int)p.x + 1; x++)
                {
                    Color a = on.At(x, y), b = off.At(x, y);
                    worst = Mathf.Max(worst, 255f * Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))));
                }
        }
        return worst;
    }

    /// <summary>Review of #536: at the gameplay zoom nothing of the sea boats' sea changes the water within 3.4 km of the
    /// shore, the surf, the shallows (whose edge reaches at most 3.5 km) and the near-coast chop: with "seacalm" on and off,
    /// gameplay03 renders those points the same. Fails if the calm starts at the shore (ScCalmNearM 0) or if the overview's
    /// shelf reaches the gameplay zoom.</summary>
    [UnityTest]
    public IEnumerator TheGameplayZoomKeepsTheNearShoreSea()
    {
        var map = OpenMap();
        yield return null;
        var points = OffshorePoints(map, new[] { 300f, 900f, 1600f, 2400f, 3000f, 3400f });
        float worst = SeaCalmChange(map, PromoPose.Gameplay03, points, out int seen);
        TestContext.WriteLine($"gameplay03: {seen} near-shore points on screen, largest change with seacalm off {worst:0.0}/255");
        Assert.Greater(seen, 20, "enough of the near-shore points are on screen");
        Assert.AreEqual(0f, worst, "the near-shore sea is unchanged at the gameplay zoom");
    }

    /// <summary>Review of #536: the overview gets no calm and no broad swells, at any resolution. Past the shelf's reach
    /// (ScCoastRangeM, 12 km), 15-40 km out, diorama02 renders the open sea the same with "seacalm" on and off, at 1920 x 1080
    /// and at 3840 x 2160 (a device pixel ratio of 2, where a pixel covers half the metres). Fails if the calm or the swells
    /// lose their fade with the zoom, or fade with the pixel size instead (then the 2160p render calms).</summary>
    [UnityTest]
    public IEnumerator TheOverviewGetsNoCalmOrSwells()
    {
        var map = OpenMap();
        yield return null;
        var points = OffshorePoints(map, new[] { 15000f, 20000f, 25000f, 30000f, 40000f });
        foreach (var (w, h) in new[] { (1920, 1080), (3840, 2160) })
        {
            float worst = SeaCalmChange(map, PromoPose.Diorama02, points, out int seen, w, h);
            TestContext.WriteLine($"diorama02 at {w}x{h}: {seen} open-sea points on screen, largest change with seacalm off {worst:0.0}/255");
            Assert.Greater(seen, 20, "enough of the open-sea points are on screen");
            Assert.AreEqual(0f, worst, $"the overview's open sea is unchanged at {w}x{h}");
        }
    }

    /// <summary>Review of #536: the coastal shelf is the overview's and only the overview's. At diorama02 the water 4-8 km out
    /// changes with "seacalm" (the shelf is on there); TheGameplayZoomKeepsTheNearShoreSea checks the gameplay zoom has none
    /// of it.</summary>
    [UnityTest]
    public IEnumerator TheShelfShowsAtTheOverview()
    {
        var map = OpenMap();
        yield return null;
        var points = OffshorePoints(map, new[] { 4000f, 6000f, 8000f });
        float worst = SeaCalmChange(map, PromoPose.Diorama02, points, out int seen);
        TestContext.WriteLine($"diorama02: {seen} shelf points on screen, largest change with seacalm off {worst:0.0}/255");
        Assert.Greater(worst, 3f, "the shelf lightens the coastal water at the overview");
    }

    /// <summary>Rendered: the Dead Sea is pale turquoise, much lighter than the open Mediterranean.</summary>
    [UnityTest]
    public IEnumerator TheDeadSeaIsPalerThanTheOpenSea()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        Color Sample(double lon, double lat)
        {
            var w = map.Frame.ToWorld(lon, lat, 0); w.y = map.Terrain.SampleHeight(w);
            rig.LookAt = w; rig.Distance = 40000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var px = new Vector2[1];
            var r = RenderSample.Render(cam, 480, 270, new[] { w }, px);
            return r.Mean((int)px[0].x, (int)px[0].y, 2);
        }
        Color dead = Sample(35.50, 31.50), med = Sample(34.40, 32.00);
        TestContext.WriteLine($"Dead Sea {dead}, Mediterranean {med}");
        Assert.Greater(Luma(dead), Luma(med) + 0.1f);
    }

    /// <summary>Review I2: at the country-wide pose (about 300 km from the camera) the open sea stays saturated blue under the
    /// water haze: b > r + 0.15 (worked out in Step 4: about 0.21 before the grade). And the water toggle exists. Fix round
    /// 1 (review I1): the water haze applies at all: with the haze off the same sample moves by more than 0.01 in luma
    /// (about 0.18 x 0.33 of the way to the cool water haze at about 300 km).</summary>
    [UnityTest]
    public IEnumerator TheOpenSeaStaysBlueUnderTheHaze()
    {
        var map = OpenMap();
        yield return null;
        CollectionAssert.Contains(VisualToggles.Names, "water");
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var sea = map.Frame.ToWorld(34.40, 31.80, 0); sea.y = map.Terrain.SampleHeight(sea);
        var px = new Vector2[1];
        var s = RenderSample.Render(cam, 960, 540, new[] { sea }, px);
        var c = s.Mean((int)px[0].x, (int)px[0].y, 2);
        VisualToggles.Set("haze", false);
        var clear = RenderSample.Render(cam, 960, 540, new[] { sea }, px);
        VisualToggles.Set("haze", true);
        var c0 = clear.Mean((int)px[0].x, (int)px[0].y, 2);
        TestContext.WriteLine($"open sea at the diorama pose {c}, b - r = {c.b - c.r:0.000}; haze off {c0}, luma shift {Luma(c) - Luma(c0):0.000}");
        Assert.Greater(c.b, c.r + 0.15f);
        Assert.Greater(Mathf.Abs(Luma(c) - Luma(c0)), 0.01f, "the water haze moves the far open sea");
    }
}
