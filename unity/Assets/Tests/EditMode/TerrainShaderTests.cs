#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 6: the Statecraft terrain graph, its baked inputs and what they do on screen.</summary>
public class TerrainShaderTests
{
    const string TerrainMatPath = "Assets/Materials/Terrain.mat";

    [TearDown]
    public void Reset() { RenderSample.TiltShift(true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        // Every render here measures colour or local detail: Track A's tilt-shift blurs the top and bottom bands (task-3
        // carry m6, applied at the merge point), so it is off; the TearDown turns it back on.
        RenderSample.TiltShift(false);
        return map;
    }

    /// <summary>Review C1, the positive half of the Task 6 gate: the generated passes include StatecraftTerrain.hlsl (the Custom
    /// Function is in File mode), and the HLSL really paints the terrain: with a magenta biome texture a flat coastal-plain
    /// pixel comes out magenta, not black or the sea colour.</summary>
    [UnityTest]
    public IEnumerator TheCustomFunctionDrivesTheTerrain()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath).shader;
        var data = ShaderUtil.GetShaderData(shader);
        int including = 0;
        for (int s = 0; s < data.SubshaderCount; s++)
        {
            var sub = data.GetSerializedSubshader(s);
            for (int p = 0; p < sub.PassCount; p++) if (sub.GetPass(p).SourceCode.Contains("StatecraftTerrain.hlsl")) including++;
        }
        Assert.Greater(including, 0, "no generated pass includes StatecraftTerrain.hlsl: the Custom Function is not in File mode");
        // Review I2: no node may compute at the template's half precision (mediump on GLES3 phones: ~210 m UV steps).
        for (int s = 0; s < data.SubshaderCount; s++)
        {
            var sub = data.GetSerializedSubshader(s);
            for (int p = 0; p < sub.PassCount; p++)
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(sub.GetPass(p).SourceCode, @"\bhalf[234]? _UV_"),
                    $"pass '{sub.GetPass(p).Name}' computes the terrain UV at half precision");
        }
        var map = OpenMap();
        // The ground itself: Lod's settlement kit (promo-art) stands on this plain point; off by name, so this runs on main too.
        foreach (var n in new[] { "kits", "trees" }) if (VisualToggles.Names.Contains(n)) VisualToggles.Set(n, false);
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        // A flat coastal-plain point (review N7): no rock blend and no stone tint can stand between the texture and the screen.
        var plain = map.Frame.ToWorld(34.90, 31.95, 0); plain.y = map.Terrain.SampleHeight(plain);
        rig.LookAt = plain; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var magenta = new Texture2D(1, 1, TextureFormat.RGBA32, false); magenta.SetPixel(0, 0, Color.magenta); magenta.Apply();
        var real = Shader.GetGlobalTexture(MapShaderGlobals.Biome);
        // The land use (fields, issue #33) paints over the plain: with an empty mask the biome colour is what remains.
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath); var realLand = mat.GetTexture("_ScLandTex");
        try
        {
            mat.SetTexture("_ScLandTex", Texture2D.blackTexture);
            Shader.SetGlobalTexture(MapShaderGlobals.Biome, magenta);
            var px = new Vector2[1];
            var s = RenderSample.Render(cam, 480, 270, new[] { plain }, px);
            var c = s.Mean((int)px[0].x, (int)px[0].y, 2);
            TestContext.WriteLine($"land with a magenta biome texture: {c}");
            Assert.Greater(c.r + c.g + c.b, 0.15f, "not black");
            Assert.Greater(Mathf.Min(c.r, c.b), c.g + 0.1f, "magenta: the biome texture reaches the screen through the HLSL");
        }
        finally { Shader.SetGlobalTexture(MapShaderGlobals.Biome, real); mat.SetTexture("_ScLandTex", realLand); Object.DestroyImmediate(magenta); }
    }

    [Test]
    public void TheTerrainUsesTheStatecraftGraphWithItsBakedTextures()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        Assert.AreEqual("Statecraft/StatecraftTerrain", mat.shader.name);
        Assert.IsFalse(ShaderUtil.ShaderHasError(mat.shader), string.Join("\n", ShaderUtil.GetShaderMessages(mat.shader).Select(m => m.message)));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Bake), mat.GetTexture("_ScBakeTex"));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast), mat.GetTexture("_ScCoastTex"));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Ao), mat.GetTexture("_ScAoTex"));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Land), mat.GetTexture("_ScLandTex"));
        CollectionAssert.AreEquivalent(MapBuilder.BiomeColours.Keys, MapPalette.Ground.Keys, "a ground colour for every biome");
    }

    [Test]
    public void TheTerrainShaderCompilesForWebGL2() =>
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath).shader));

    [Test]
    public void TheBakedTexturesImportAsLinearClampedData()
    {
        var bake = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Bake);
        var coast = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast);
        var ao = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Ao);
        foreach (var t in new[] { bake, coast, ao })
        {
            Assert.AreEqual(2048, t.width, t.name); Assert.AreEqual(2048, t.height, t.name);
            Assert.IsFalse(t.isDataSRGB, t.name + " is data, not colour");
            Assert.AreEqual(TextureWrapMode.Clamp, t.wrapMode, t.name);
            Assert.Greater(t.mipmapCount, 1, t.name);
            Assert.AreEqual(FilterMode.Trilinear, t.filterMode, t.name + ": no mip seams across the pitched view (review M5)");
        }
        Assert.AreEqual(2, bake.anisoLevel, "the bake is seen at a grazing angle");
        Assert.AreEqual(TextureFormat.R8, coast.format, "one uncompressed channel: compression blocks would show in the foam band");
        Assert.AreEqual(TextureFormat.R8, ao.format, "one uncompressed channel: BC1 banded the AO on broad slopes");
    }

    [Test]
    public void TheTerrainDrawsInOnePassAndNeverFallsBackToItsBasemap()
    {
        var terrain = OpenMap().Terrain;
        Assert.AreEqual(1, terrain.terrainData.terrainLayers.Length, "one layer: one pass (7 layers drew every pixel twice)");
        Assert.GreaterOrEqual(terrain.basemapDistance, 1e6f, "the camera is always farther than the 1 km default");
    }

    [Test]
    public void TheBiomeTextureIsOwnedByTheMapAndSetForTheShaders()
    {
        var map = OpenMap();
        var biome = Shader.GetGlobalTexture(MapShaderGlobals.Biome);
        Assert.IsNotNull(biome); Assert.AreEqual(MapBuilder.BiomeTextureResolution, biome.width);
        Assert.IsFalse(biome.isReadable, "no CPU copy of the 1024² texture stays on the heap (review M4)");
        Assert.AreEqual(FilterMode.Trilinear, biome.filterMode, "no mip seams across the pitched view (review M5)");
        var m = Shader.GetGlobalVector(MapShaderGlobals.Map);
        Assert.AreEqual(map.Terrain.terrainData.size.x, m.x, 1f); Assert.AreEqual(map.Terrain.terrainData.size.z, m.y, 1f);
        Assert.AreEqual(map.Hexes.hex_size_km * 1000f, Shader.GetGlobalVector(MapShaderGlobals.MapOrigin).w, 1e-3f);
        CollectionAssert.Contains(VisualToggles.Names, "haze");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.IsTrue(biome == null, "destroyed with the map");
    }

    /// <summary>Rendered by Tel Aviv: 10 km offshore is the sea colour, 10 km inland the ground colour.</summary>
    [UnityTest]
    public IEnumerator TheCoastSeparatesLandFromSea()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = map.Frame.ToWorld(34.78, 32.08, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var sea = map.Frame.ToWorld(34.66, 32.08, 0); var land = map.Frame.ToWorld(34.89, 32.08, 0);
        sea.y = map.Terrain.SampleHeight(sea); land.y = map.Terrain.SampleHeight(land);
        var px = new Vector2[2];
        var s = RenderSample.Render(cam, 960, 540, new[] { sea, land }, px);
        Color a = s.Mean((int)px[0].x, (int)px[0].y, 3), b = s.Mean((int)px[1].x, (int)px[1].y, 3);
        TestContext.WriteLine($"sea {a}, land {b}");
        Assert.Greater(a.b, a.r + 0.15f, "sea"); Assert.Greater(b.r, b.b + 0.05f, "land");
    }

    /// <summary>Rendered over the Judean hills: the bakes (AO, curvature, sky-view) raise the relief's local contrast
    /// (RenderSample.GradientEnergy) by at least 8% against a neutral bake (AO 1, curvature 0.5, sky 1). Local, not the
    /// frame's luma spread: this view takes in the Dead Sea (about 7% of the frame), whose land-to-sea step is about a
    /// quarter to a third of the frame's variance and shrinks as the AO darkens the land, cancelling the relief's gain
    /// (LumaStd measured 0.0830 -> 0.0827 while the gradient rose 13%).
    /// Terrain.mat is changed in memory only and restored.</summary>
    [UnityTest]
    public IEnumerator TheBakesCarveTheRelief()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = map.Frame.ToWorld(35.25, 31.70, 0); rig.Distance = 45000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        var real = mat.GetTexture("_ScBakeTex"); var realAo = mat.GetTexture("_ScAoTex");
        var neutral = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        neutral.SetPixel(0, 0, new Color(1f, 0.5f, 1f, 1f)); neutral.Apply();
        try
        {
            var carved = RenderSample.Render(cam, 480, 270);
            mat.SetTexture("_ScBakeTex", neutral); mat.SetTexture("_ScAoTex", Texture2D.whiteTexture);
            var plain = RenderSample.Render(cam, 480, 270);
            TestContext.WriteLine($"relief gradient {plain.GradientEnergy(0f, 1f):0.0000} -> {carved.GradientEnergy(0f, 1f):0.0000}");
            Assert.Greater(carved.GradientEnergy(0f, 1f), plain.GradientEnergy(0f, 1f) * 1.08f);
        }
        finally { mat.SetTexture("_ScBakeTex", real); mat.SetTexture("_ScAoTex", realAo); Object.DestroyImmediate(neutral); }
    }

    /// <summary>Rendered at the country-wide pose: the haze moves the far (top) band more than the near (bottom) band, across
    /// the full width. Task 6 sampled only the land column x 0.45..0.6, because then only the land carried haze and the far
    /// band's sea and border skirt hid the effect (full width: far 0.023, near 0.026). Since Task 7 the sea on both sides
    /// takes the water haze, so the full width is measured again (Task 7 fix round 1).</summary>
    [UnityTest]
    public IEnumerator TheHazeThickensWithDistance()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set("haze", false); var clear = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("haze", true); var hazy = RenderSample.Render(cam, 480, 270);
        const float x0 = 0f, x1 = 1f;
        float far = Mathf.Abs(hazy.MeanLuma(x0, 0.8f, x1, 0.95f) - clear.MeanLuma(x0, 0.8f, x1, 0.95f));
        float near = Mathf.Abs(hazy.MeanLuma(x0, 0.05f, x1, 0.2f) - clear.MeanLuma(x0, 0.05f, x1, 0.2f));
        TestContext.WriteLine($"haze shift far {far:0.000}, near {near:0.000}");
        Assert.Greater(far, near + 0.005f);
    }

    /// <summary>Review I1: the haze is a depth cue, not a veil. At the country-wide pose the nearest land (bottom band,
    /// about 205 km away) is inside the haze's start distance and barely moves; the middle and far bands move more and
    /// more. Full width, as in TheHazeThickensWithDistance (Task 7 fix round 1: the sea is hazed too).</summary>
    [UnityTest]
    public IEnumerator TheHazeStartsBeyondTheNearestDioramaLandAndGrowsWithDistance()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set("haze", false); var clear = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("haze", true); var hazy = RenderSample.Render(cam, 480, 270);
        float Shift(float y0, float y1) => Mathf.Abs(hazy.MeanLuma(0f, y0, 1f, y1) - clear.MeanLuma(0f, y0, 1f, y1));
        float near = Shift(0.05f, 0.2f), mid = Shift(0.425f, 0.575f), far = Shift(0.8f, 0.95f);
        TestContext.WriteLine($"haze by distance near {near:0.0000}, mid {mid:0.0000}, far {far:0.0000}");
        Assert.Less(near, 0.008f, "about no haze at the nearest land");
        Assert.Greater(mid, near); Assert.Greater(far, mid + 0.005f);
    }

    /// <summary>Task 5 carry 3 / review A3: the AO the shader samples (ao.png) shows no compression banding. On broad
    /// slopes (texels whose source steps by at most one 8-bit level to the east and north neighbours) the GPU-decoded
    /// texture must not step by two levels or more between those neighbours on more than 2% of them, over the Judean
    /// slopes and the Negev plateau. Measured on the BC1 terrain_bake.png R channel this failed (Judean slopes: 404 of 681
    /// texels, 59%), so the AO is its own uncompressed R8 texture.</summary>
    [Test]
    public void TheCompressedAoShowsNoBandingOnBroadSlopes()
    {
        var ao = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Ao);
        int n = ao.width;
        var rt = RenderTexture.GetTemporary(n, n, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        var gpu = new Texture2D(n, n, TextureFormat.RGBAFloat, false, true);
        var png = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        var prev = RenderTexture.active;
        try
        {
            Graphics.Blit(ao, rt);
            RenderTexture.active = rt; gpu.ReadPixels(new Rect(0, 0, n, n), 0, 0); gpu.Apply();
            Assert.IsTrue(png.LoadImage(System.IO.File.ReadAllBytes(MapTexturePaths.Ao)));
            Assert.AreEqual(n, png.width);
            var d = gpu.GetPixels(); var s = png.GetPixels32();
            var meta = JsonUtility.FromJson<HeightmapMeta>(System.IO.File.ReadAllText(System.IO.Path.Combine(Paths.StreamingMap, "heightmap.json")));
            int X(double lon) => (int)((lon - meta.lon0) / (meta.lon1 - meta.lon0) * n);
            int Y(double lat) => (int)((lat - meta.lat0) / (meta.lat1 - meta.lat0) * n);   // texture row 0 is south
            foreach (var (name, lon0, lon1, lat0, lat1) in new[] { ("Judean slopes", 35.05, 35.30, 31.60, 31.90), ("Negev plateau", 34.70, 35.10, 30.40, 30.80) })
            {
                int broad = 0, stepped = 0;
                for (int y = Y(lat0) + 2; y < Y(lat1) - 2; y++)
                    for (int x = X(lon0) + 2; x < X(lon1) - 2; x++)
                    {
                        int S(int xx, int yy) => s[yy * n + xx].r;
                        // Broad: the source steps by at most one 8-bit level to both neighbours.
                        if (Mathf.Max(Mathf.Abs(S(x + 1, y) - S(x, y)), Mathf.Abs(S(x, y + 1) - S(x, y))) > 1) continue;
                        broad++;
                        float r = d[y * n + x].r;
                        float step = Mathf.Max(Mathf.Abs(d[y * n + x + 1].r - r), Mathf.Abs(d[(y + 1) * n + x].r - r)) * 255f;
                        if (step >= 2f - 1e-3f) stepped++;
                    }
                float share = (float)stepped / Mathf.Max(1, broad);
                TestContext.WriteLine($"AO banding {name}: {stepped} of {broad} broad-slope texels step >= 2 levels ({share:P2})");
                Assert.Greater(broad, 500, name);
                Assert.LessOrEqual(share, 0.02f, name);
            }
        }
        finally
        {
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(gpu); Object.DestroyImmediate(png);
        }
    }

    /// <summary>Review A3: the art checkpoint also shoots the 6 km hero zoom over the Judean slopes and the Negev plateau
    /// (with the haze off), where AO steps would show; they are checks, not promo matches, so PromoPose.All (the profile's
    /// and the editor capture's list) keeps its two poses.</summary>
    [Test]
    public void TheCheckpointShootsTheHeroZoomOverTheJudeanSlopesAndTheNegev()
    {
        var checks = PromoPose.CloseChecks;
        CollectionAssert.AreEqual(new[] { "hero06judea", "hero06negev" }, checks.Select(p => p.Name).ToArray());
        Assert.AreEqual((35.15, 31.78), (checks[0].Lon, checks[0].Lat));
        Assert.AreEqual((34.90, 30.60), (checks[1].Lon, checks[1].Lat));
        foreach (var p in checks) Assert.AreEqual(6000f, p.Distance, p.Name);
        Assert.IsFalse(PromoPose.All.Any(p => checks.Any(c => c.Name == p.Name)));
    }
}
