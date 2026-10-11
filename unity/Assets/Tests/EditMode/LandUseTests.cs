#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Issue #33 (promo-land): the land-use texture (tools/land_bake.py) and the fields, terraces and farm tracks the terrain
/// shader paints from it (StatecraftLand.hlsl).</summary>
public class LandUseTests
{
    const string TerrainMatPath = "Assets/Materials/Terrain.mat";

    [TearDown]
    public void Reset()
    {
        RenderSample.TiltShift(true);
        VisualToggles.SetAll(true);   // review M10: Clear alone left the global _ScLandOn at 0 for the next test
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    /// <summary>The map built for measuring the ground itself (integration bug B4): without the settlement kits, whose
    /// blocks, trees and paved town ground stand on the sampled plain once promo-art is merged. By name and by reflection,
    /// as SeaTests does (promo-art 689880d), so this compiles and runs where the kits do not exist yet.</summary>
    static MapScene OpenMapWithoutKits()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);   // the scene is not saved
        var map = Object.FindFirstObjectByType<MapScene>();
        typeof(MapScene).GetMethod("ConfigureKits")?.Invoke(map, new object?[] { null, null });
        map.Build();
        foreach (var n in new[] { "kits", "trees" }) if (System.Linq.Enumerable.Contains(VisualToggles.Names, n)) VisualToggles.Set(n, false);
        return map;
    }

    static Texture2D Read(string path)
    {
        var png = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        Assert.IsTrue(png.LoadImage(System.IO.File.ReadAllBytes(path)));
        return png;
    }


    static HeightmapMeta Meta() =>
        JsonUtility.FromJson<HeightmapMeta>(System.IO.File.ReadAllText(System.IO.Path.Combine(Paths.StreamingMap, "heightmap.json")));

    /// <summary>The texel at (lon, lat), from the source PNG (Texture2D row 0 is south).</summary>
    static Color MaskAt(Texture2D png, HeightmapMeta m, double lon, double lat) =>
        png.GetPixel((int)((lon - m.lon0) / (m.lon1 - m.lon0) * png.width), (int)((lat - m.lat0) / (m.lat1 - m.lat0) * png.height));

    /// <summary>The land weight (alpha) averaged over the 13 x 13 texels (about 1 x 1.4 km) round (lon, lat): parcels left
    /// as grass and hedges vary it texel by texel.</summary>
    static float WeightAround(Texture2D png, HeightmapMeta m, double lon, double lat)
    {
        int x = (int)((lon - m.lon0) / (m.lon1 - m.lon0) * png.width), y = (int)((lat - m.lat0) / (m.lat1 - m.lat0) * png.height);
        float sum = 0; for (int dy = -6; dy <= 6; dy++) for (int dx = -6; dx <= 6; dx++) sum += png.GetPixel(x + dx, y + dy).a;
        return sum / 169f;
    }

    [Test]
    public void TheTexturesImportForTheShaderAndEveryWebBuild()
    {
        var land = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Land);
        Assert.AreEqual((2048, 4096), (land.width, land.height), "about 79 x 105 m per texel");
        Assert.AreEqual(TextureWrapMode.Clamp, land.wrapMode);
        Assert.IsTrue(land.isDataSRGB, "sRGB colour: 8 bits and BC3 endpoints where the eye needs them (review M3)");
        Assert.AreEqual(FilterMode.Trilinear, land.filterMode); Assert.Greater(land.mipmapCount, 1); Assert.AreEqual(2, land.anisoLevel);
        Assert.AreEqual(TextureFormat.DXT5, land.format, "colour and weight block-compressed (BC3) on the desktop platforms");
        // Review I4: the WebGL override keeps the 4096 rows and lets the build's texture subtarget choose the format (DXT5 in
        // the desktop build, ASTC 6x6 / ETC2 in a mobile one), instead of pinning DXT5, which phones decompress on the CPU.
        var web = ((TextureImporter)AssetImporter.GetAtPath(MapTexturePaths.Land)).GetPlatformTextureSettings("WebGL");
        Assert.IsTrue(web.overridden); Assert.AreEqual(4096, web.maxTextureSize);
        Assert.Less((int)web.format, 0, $"WebGL format {web.format}: an automatic format (the negative enum values), not a pinned one");
        Assert.AreEqual(TextureImporterCompression.Compressed, web.textureCompression);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        Assert.AreEqual(land, mat.GetTexture("_ScLandTex"));
    }

    /// <summary>Build.Web -textureSubtarget names the WebGL texture subtarget (desktop DXT, mobile ASTC or ETC2), and
    /// refuses anything else. Build lives in the predefined editor assembly: reached by reflection.</summary>
    [Test]
    public void TheWebBuildTakesATextureSubtarget()
    {
        var build = System.Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor");
        Assert.IsNotNull(build);
        var parse = build!.GetMethod("TextureSubtarget"); Assert.IsNotNull(parse);
        Assert.AreEqual(WebGLTextureSubtarget.ASTC, parse!.Invoke(null, new object?[] { "astc" }));
        Assert.AreEqual(WebGLTextureSubtarget.DXT, parse.Invoke(null, new object?[] { "dxt" }));
        Assert.AreEqual(WebGLTextureSubtarget.ETC2, parse.Invoke(null, new object?[] { "etc2" }));
        Assert.IsNull(parse.Invoke(null, new object?[] { null }));
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => parse.Invoke(null, new object?[] { "pvrtc" }));
        Assert.IsInstanceOf<System.ArgumentException>(ex!.InnerException);
    }

    /// <summary>Review M9: MapCapture -extraPose checks its value: five fields, a safe name that is not a promo pose's,
    /// numbers, a point on the map and a positive distance.</summary>
    [Test]
    public void AnExtraCapturePoseIsValidated()
    {
        var capture = System.Type.GetType("MapCapture, Assembly-CSharp-Editor");
        Assert.IsNotNull(capture);
        var parse = capture!.GetMethod("ParseExtraPose"); Assert.IsNotNull(parse);
        var pose = (PromoPose)parse!.Invoke(null, new object[] { "judea20:35.15:31.80:20000:0" })!;
        Assert.AreEqual(("judea20", 35.15, 31.80, 20000f, 0f), (pose.Name, pose.Lon, pose.Lat, pose.Distance, pose.Yaw));
        foreach (var bad in new[] { "judea20:35.15:31.80:20000", "../x:35.15:31.80:20000:0", "a/b:35.15:31.80:20000:0",
                                    "gameplay03:35.15:31.80:20000:0", "x:east:31.80:20000:0", "x:30.0:31.80:20000:0", "x:35.15:31.80:0:0" })
        {
            var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => parse.Invoke(null, new object[] { bad }), bad);
            Assert.IsInstanceOf<System.ArgumentException>(ex!.InnerException, bad);
        }
    }

    /// <summary>The land follows the real ground: it covers the coastal plain by Rehovot, the Jezreel valley and (as
    /// garrigue and terraces) the Judean hills, and leaves the central Negev and the Dead Sea to the biome and the water.</summary>
    [Test]
    public void TheLandFollowsSlopeRainAndTowns()
    {
        var png = Read(MapTexturePaths.Land); var m = Meta();
        try
        {
            float plain = WeightAround(png, m, 34.84, 31.92), jezreel = WeightAround(png, m, 35.30, 32.62);
            float negev = WeightAround(png, m, 34.90, 30.60), deadSea = WeightAround(png, m, 35.50, 31.50), judea = WeightAround(png, m, 35.15, 31.78);
            TestContext.WriteLine($"land weight: plain {plain:0.000}, Jezreel {jezreel:0.000}, Negev {negev:0.000}, Dead Sea {deadSea:0.000}, Judea {judea:0.000}");
            Assert.Greater(plain, 0.8f, "fields on the coastal plain"); Assert.Greater(jezreel, 0.8f, "fields in the Jezreel valley");
            Assert.Less(negev, 0.02f, "the central Negev stays desert"); Assert.Less(deadSea, 0.02f, "water");
            Assert.Greater(judea, 0.4f, "garrigue and terraces on the Judean hills");
        }
        finally { Object.DestroyImmediate(png); }
    }

    /// <summary>Rendered at the gameplay pose: the fields, hedges and farm tracks add local detail to the plain (the flat
    /// mustard ground had little): the gradient energy of the frame's lower half rises by at least 15% against the bare
    /// biome ground (the "land" toggle off), and the mean colour stays the same ground (saturation within 0.06).</summary>
    [UnityTest]
    public IEnumerator TheFieldsAddDetailToThePlainWithoutChangingItsColour()
    {
        var map = OpenMapWithoutKits();
        RenderSample.TiltShift(false);
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var farmed = RenderSample.Render(cam, 960, 540);
        VisualToggles.Set("land", false);
        var bare = RenderSample.Render(cam, 960, 540);
        TestContext.WriteLine($"gradient {bare.GradientEnergy(0f, 0.5f):0.0000} -> {farmed.GradientEnergy(0f, 0.5f):0.0000}; " +
                              $"saturation {bare.MeanSaturation():0.000} -> {farmed.MeanSaturation():0.000}");
        Assert.Greater(farmed.GradientEnergy(0f, 0.5f), bare.GradientEnergy(0f, 0.5f) * 1.15f);
        Assert.AreEqual(bare.MeanSaturation(), farmed.MeanSaturation(), 0.06f);
    }
}
