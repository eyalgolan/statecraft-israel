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

/// <summary>Plan Task 7: the water, inside the map and past its edge, and the plateau skirt.</summary>
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

    /// <summary>The controller's known gap: at the Jordanian plateau the land skirt holds the edge's height for 20 km (at
    /// least 80% of its height above the sea) and reaches sea level only at the last ring.</summary>
    [Test]
    public void TheLandSkirtHoldsThePlateauThenEasesToSeaLevel()
    {
        var map = OpenMap();
        var size = map.Terrain.terrainData.size;
        var verts = map.Terrain.transform.Find(MapScene.BorderSkirtName).GetComponent<MeshFilter>().sharedMesh.vertices;
        var meta = HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta;
        float seaY = (0f - meta.elev_min_m) * MapBuilder.VerticalExaggeration;
        float zAmman = map.Frame.ToWorld(35.9, 31.95, 0).z;
        int best = -1;
        for (int i = 0; i < verts.Length; i++)
            if (Mathf.Abs(verts[i].x - size.x) < 0.5f && (best < 0 || Mathf.Abs(verts[i].z - zAmman) < Mathf.Abs(verts[best].z - zAmman))) best = i;
        Assert.GreaterOrEqual(best, 0, "an east-edge spoke");
        float edge = verts[best].y - seaY;
        Assert.Greater(edge, 1500f, "the plateau stands well above the sea (about 900 m, exaggerated 3x)");
        Assert.AreEqual(size.x + MapBuilder.SkirtRings[2], verts[best + 2].x, 1f, "ring 2 of the same spoke");
        Assert.GreaterOrEqual(verts[best + 2].y - seaY, 0.8f * edge, "held 20 km out");
        Assert.AreEqual(seaY, verts[best + MapBuilder.SkirtRings.Length - 1].y, 1f, "at sea level at the last ring");
    }

    /// <summary>Task 7 checkpoint finding: holding each edge vertex's own height for 20 km drew the edge's ridges out as long
    /// stripes across the Jordan skirt. Along the east edge, the held ring 2 is far smoother from spoke to spoke than the
    /// edge (ring 0, which meets the terrain exactly). Fix round 1 (review M4): the hold is a running mean over the edge's
    /// own samples, not 17 taps 1 km apart that undersampled ridges shorter than about 2 km: under 18% of the edge's step.
    /// What a +-8 km mean keeps is the plateau's own trend along the edge (about 400 m of exaggerated height over 16 km, over
    /// a 77-sample window: 5.2 m per spoke); the 17 taps added their aliasing on top (8.5 m).</summary>
    [Test]
    public void TheHeldSkirtCarriesThePlateauNotTheEdgesRidges()
    {
        var map = OpenMap();
        var size = map.Terrain.terrainData.size;
        var verts = map.Terrain.transform.Find(MapScene.BorderSkirtName).GetComponent<MeshFilter>().sharedMesh.vertices;
        int rings = MapBuilder.SkirtRings.Length;
        float edge = 0f, held = 0f; int pairs = 0, prev = -1;
        for (int i = 0; i + rings <= verts.Length; i += rings)
        {
            bool east = Mathf.Abs(verts[i].x - size.x) < 0.5f && verts[i].z > 0.3f * size.z && verts[i].z < 0.9f * size.z
                && Mathf.Abs(verts[i + 2].x - (size.x + MapBuilder.SkirtRings[2])) < 1f;
            if (!east) { prev = -1; continue; }
            if (prev >= 0) { edge += Mathf.Abs(verts[i].y - verts[prev].y); held += Mathf.Abs(verts[i + 2].y - verts[prev + 2].y); pairs++; }
            prev = i;
        }
        TestContext.WriteLine($"east edge: {pairs} spoke pairs, mean step at the edge {edge / pairs:0.0} m, held 20 km out {held / pairs:0.0} m");
        Assert.Greater(pairs, 500);
        Assert.Less(held, 0.18f * edge, "the held rings carry the plateau's broad height, not the edge's ridges (no aliasing)");
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
