#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Plan Task 3: the two-band tilt-shift.</summary>
public class TiltShiftTests
{
    static readonly Vector4 P = new Vector4(0.5f, 0.13f, 0.17f, 14f / 1080f);

    [TearDown]
    public void Reset()
    {
        TiltShift.Enabled = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
    }

    [Test]
    public void TheMiddleBandIsSharpAndBothEdgesBlur()
    {
        Assert.AreEqual(0f, TiltShift.Weight(0.5f, P));
        Assert.AreEqual(0f, TiltShift.Weight(0.38f, P), 1e-6f, "inside the sharp half-width");
        Assert.AreEqual(1f, TiltShift.Weight(0.0f, P)); Assert.AreEqual(1f, TiltShift.Weight(1.0f, P));
        Assert.AreEqual(TiltShift.Weight(0.2f, P), TiltShift.Weight(0.8f, P), 1e-6f, "symmetric about the focus");
        Assert.Greater(TiltShift.Weight(0.25f, P), TiltShift.Weight(0.30f, P), "stronger towards the edge");
        Assert.AreEqual(0f, TiltShift.Weight(0.5f, new Vector4(0.5f, 0.1f, 0f, 0.01f)), "a zero feather is a step: sharp inside");
        Assert.AreEqual(1f, TiltShift.Weight(0.65f, new Vector4(0.5f, 0.1f, 0f, 0.01f)), "a zero feather is a step: blurred outside");
    }

    /// <summary>Review I2: the promo blurs about the bottom 15% and the top 20% fully. With no bar, full blur reaches 20% in
    /// from each edge; under the web tier's top bar (about 9% of the screen), the map area keeps about 15% of the screen fully
    /// blurred at the bottom and as much again between the band and the bar.</summary>
    [Test]
    public void TheFullBlurBandsAreAsTallAsThePromos()
    {
        Assert.AreEqual(0.13f, TiltShift.HalfWidth); Assert.AreEqual(0.17f, TiltShift.Feather);
        var none = TiltShift.Params(0f, 30000f);
        Assert.AreEqual(1f, TiltShift.Weight(0.20f, none), 1e-4f, "full blur in the bottom 20% with no bar");
        Assert.AreEqual(1f, TiltShift.Weight(0.80f, none), 1e-4f, "full blur in the top 20% with no bar");
        Assert.Less(TiltShift.Weight(0.25f, none), 1f, "but not further in");
        var web = TiltShift.Params(0.09f, 30000f);
        Assert.AreEqual(1f, TiltShift.Weight(0.155f, web), 1e-4f, "bottom 15.5% under a 9% bar");
        Assert.AreEqual(1f, TiltShift.Weight(0.755f, web), 1e-4f, "0.755 to the bar at 0.91: 15.5% at the top");
    }

    [Test]
    public void TearingDownTheMapClearsTheBandAndTurnsTheToggleBackOn()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        Assert.IsTrue(TiltShift.HasBand, "Init applies the band");
        VisualToggles.Set("tiltshift", false); Assert.IsFalse(TiltShift.Enabled);
        Object.DestroyImmediate(map.gameObject);
        Assert.IsFalse(TiltShift.HasBand, "no band after teardown: the feature draws nothing");
        Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(TiltShift.ParamsId));
        Assert.IsTrue(TiltShift.Enabled, "the toggle is back on");
        CollectionAssert.DoesNotContain(VisualToggles.Names, "tiltshift");
    }

    /// <summary>Review m3: "notiltshift" in the page URL (-notiltshift on desktop) boots with the tilt-shift off, so a WebGL
    /// shot with the real top bar can be compared with one that has it.</summary>
    [Test]
    public void NoTiltShiftInTheUrlTurnsItOff()
    {
        Assert.IsTrue(TiltShift.OffRequested("http://localhost/?shot&notiltshift", new string[0], true));
        Assert.IsFalse(TiltShift.OffRequested("http://localhost/?shot", new string[0], true));
        Assert.IsFalse(TiltShift.OffRequested("http://localhost/?notiltshifts", new string[0], true));
        Assert.IsTrue(TiltShift.OffRequested(null, new[] { "player", "-notiltshift" }, false));
        Assert.IsFalse(TiltShift.OffRequested("http://localhost/?notiltshift", new string[0], false), "desktop reads args only");
    }

    [Test]
    public void TheBlurIsResolutionIndependentAndFollowsTheTopBar()
    {
        Assert.AreEqual(0.5f, TiltShift.FocusCentre(0f));
        Assert.AreEqual(0.45f, TiltShift.FocusCentre(0.1f), 1e-6f, "the middle of the map area below a 10% bar");
        Assert.AreEqual(0.25f, TiltShift.FocusCentre(5f), 1e-6f, "clamped to CameraRig.MaxTopInset");
        Assert.AreEqual(0.45f, TiltShift.Params(0.1f, 30000f).x, 1e-6f);
        Assert.AreEqual(14f / 1080f, TiltShift.MaxRadiusUv, 1e-7f, "14 px of a 1080-line picture, whatever the window or render scale");
        Assert.AreEqual(TiltShift.MaxRadiusUv, TiltShift.Params(0f, 30000f).w, 1e-7f, "full strength when close");
        Assert.AreEqual(TiltShift.MaxRadiusUv * TiltShift.MinStrength, TiltShift.Params(0f, CameraController.MaxDistance).w, 1e-7f, "weakest at the overview");
    }

    /// <summary>Issue #390: the far band's strength by zoom, and the near band never takes it.</summary>
    [Test]
    public void TheFarBandIsLighterAtTheGameplayZoom()
    {
        Assert.AreEqual(TiltShift.FarGameplayStrength, TiltShift.FarStrength(PromoPose.Gameplay03.Distance), 1e-6f);
        Assert.AreEqual(TiltShift.FarGameplayStrength, TiltShift.FarStrength(120000f), 1e-6f, "CameraRig's starting zoom");
        Assert.AreEqual(1f, TiltShift.FarStrength(CameraController.MaxDistance), 1e-6f, "the overview keeps it in full");
        Assert.Less(TiltShift.FarStrength(200000f), 1f); Assert.Greater(TiltShift.FarStrength(200000f), TiltShift.FarGameplayStrength);
        var p = TiltShift.Params(0.09f, 60000f);
        Assert.AreEqual(0.35f, TiltShift.BandStrength(0.9f, p, 0.35f, 0.4f), "the far band above the focus");
        Assert.AreEqual(0.4f, TiltShift.BandStrength(0.1f, p, 0.35f, 0.4f), "the near band below it");
    }

    /// <summary>The owner's ruling (2026-10-14): the near (bottom) band as on promo 03, gentle at the gameplay zoom and
    /// about none at the overview, where promo 02's foreground is sharp. The far band keeps its own curve (issue #390).</summary>
    [Test]
    public void TheNearBandIsGentleAtTheGameplayZoomAndGoneAtTheOverview()
    {
        Assert.That(TiltShift.NearGameplayStrength, Is.InRange(0.5f, 0.9f), "lighter than before, but a tilt-shift");
        Assert.AreEqual(TiltShift.NearGameplayStrength, TiltShift.NearStrength(PromoPose.Gameplay03.Distance), 1e-6f);
        Assert.AreEqual(TiltShift.NearGameplayStrength, TiltShift.NearStrength(120000f), 1e-6f, "CameraRig's starting zoom");
        Assert.AreEqual(0f, TiltShift.NearStrength(CameraController.MaxDistance), 1e-6f, "none at the overview");
        Assert.Less(TiltShift.NearStrength(200000f), TiltShift.NearGameplayStrength); Assert.Greater(TiltShift.NearStrength(200000f), 0f);
        Assert.AreEqual(0.35f, TiltShift.FarGameplayStrength, "the far band is the #390 ruling's");
    }

    [Test]
    public void BothTiersRunTheTiltShiftAndTheFarBlurIsGone()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(PostFxPaths.TiltShiftMaterial);
        Assert.IsNotNull(mat); Assert.AreEqual("Statecraft/TiltShift", mat.shader.name);
        foreach (var path in new[] { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" })
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var f = data.rendererFeatures.OfType<TiltShiftFeature>().SingleOrDefault();
            Assert.IsNotNull(f, path); Assert.IsTrue(f!.isActive, path);
            // The same asset, compared by instance id: Unity can hand out two managed wrappers for one native object
            // (seen for the PC renderer, the active one), so AreSame on the wrappers fails for the right asset.
            Assert.IsNotNull(f.Material, path); Assert.AreEqual(mat.GetInstanceID(), f.Material!.GetInstanceID(), path);
        }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build();
        Assert.IsFalse(Camera.main.GetComponent<CameraRig>().Volume.profile.Has<DepthOfField>(), "the far-only Gaussian DoF is retired");
        CollectionAssert.Contains(VisualToggles.Names, "tiltshift");
    }

    [Test]
    public void TheTiltShiftShaderCompilesForWebGL2() =>
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/TiltShift")));

    /// <summary>Rendered on both tiers at the gameplay pose: the bottom (near) band keeps 28-60% of its local contrast
    /// (gently soft since the owner's ruling of 2026-10-14, as on promo 03: main's full band kept about 20%), the middle
    /// band at least 95%. (Thresholds leave room for the terrain and grade tracks: a blur lowers gradient energy whatever
    /// the texture.) The top (far) band is lighter at this zoom since issue #390:
    /// TheFarBandStaysReadableAtTheGameplayZoom.</summary>
    [UnityTest]
    public IEnumerator TheBottomBlursAndTheMiddleStaysSharpOnBothTiers()
    {
        int saved = QualitySettings.GetQualityLevel();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        try
        {
            foreach (var level in new[] { 0, 1 })
            {
                QualitySettings.SetQualityLevel(level, true);
                TiltShift.Apply(0f, rig.Distance);
                TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
                TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
                float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
                float bottom = Ratio(0.02f, 0.10f), middle = Ratio(0.46f, 0.54f);
                float bottomInner = Ratio(0.13f, 0.19f);   // review I2: the bands are tall
                TestContext.WriteLine($"{QualitySettings.names[level]}: bottom {bottom:0.00}, middle {middle:0.00}, bottom 0.13-0.19 {bottomInner:0.00}");
                Assert.That(bottom, Is.InRange(0.28f, 0.6f), "bottom band gently soft");
                Assert.That(bottomInner, Is.InRange(0.28f, 0.6f), "13-19% above the bottom too (review I2: the bands are tall)");
                Assert.Greater(middle, 0.95f, "middle band sharp");
                // Review N6: the same blur at twice the resolution (the radius is a share of the picture's height).
                TiltShift.Enabled = false; var sharpBig = RenderSample.Render(cam, 1920, 1080);
                TiltShift.Enabled = true; var shiftedBig = RenderSample.Render(cam, 1920, 1080);
                float bottomBig = shiftedBig.GradientEnergy(0.02f, 0.10f) / sharpBig.GradientEnergy(0.02f, 0.10f);
                TestContext.WriteLine($"{QualitySettings.names[level]}: bottom at 1920x1080 {bottomBig:0.00}");
                Assert.AreEqual(bottom, bottomBig, 0.1f, "the bottom band loses the same share of its detail at 1080 lines as at 540");
            }
        }
        finally { QualitySettings.SetQualityLevel(saved, true); }
    }

    /// <summary>Issue #390, the owner's ruling (2026-10-13): a lighter tilt-shift at the gameplay zoom, so the far towns and
    /// tokens stay readable, as on promo 03, whose far edge is only gently soft. At gameplay03 (60 km) and at the
    /// starting zoom (CameraRig's 120 km, the README map), under the web tier's top bar, the far band keeps at least 60% of
    /// its local contrast (4abb64f kept 14-25%: a smear). Promo 03's far band, measured against its own focus band and
    /// compared with the same ratio on a sharp frame of ours, keeps about 37%, so 60% is at least as readable.</summary>
    [UnityTest]
    public IEnumerator TheFarBandStaysReadableAtTheGameplayZoom()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        try
        {
            foreach (var distance in new[] { PromoPose.Gameplay03.Distance, 120000f })   // 120 km: CameraRig's starting zoom
            {
                PromoPose.Gameplay03.Apply(rig, map.Frame); rig.Distance = distance; rig.TopInset = 0.09f; rig.Apply();
                TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
                TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
                float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
                float far = Ratio(0.78f, 0.90f), near = Ratio(0.02f, 0.10f);
                TestContext.WriteLine($"{distance / 1000f:0} km: far band 0.78-0.90 {far:0.00}, near band {near:0.00}");
                Assert.GreaterOrEqual(far, 0.6f, $"far band readable at {distance / 1000f:0} km");
                Assert.Less(far, 0.95f, "but still gently soft (a tilt-shift, not none)");
                Assert.That(near, Is.InRange(0.28f, 0.6f), "the near band gently soft (the 2026-10-14 ruling)");
            }
        }
        finally { rig.TopInset = 0f; }
    }

    /// <summary>The overview (diorama02, CameraController.MaxDistance) keeps the far band in full (issue #390): at most 30%
    /// of its local contrast (21% measured). Its near band is about gone since the owner's ruling of 2026-10-14, so the
    /// foreground is sharp as on promo 02: at least 95% (main kept 33%).</summary>
    [UnityTest]
    public IEnumerator TheDioramaKeepsTheFarBandAndASharpForeground()
    {
        Assert.AreEqual(1f, TiltShift.FarStrength(CameraController.MaxDistance), "full far band at the overview");
        Assert.AreEqual(0f, TiltShift.NearStrength(CameraController.MaxDistance), "no near band at the overview");
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        try
        {
            PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0.09f; rig.Apply();
            TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
            TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
            float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
            float far = Ratio(0.78f, 0.90f), near = Ratio(0.02f, 0.10f), middle = Ratio(0.43f, 0.48f);
            TestContext.WriteLine($"diorama02: far {far:0.00}, near {near:0.00}, middle {middle:0.00}");
            Assert.Less(far, 0.3f, "far band blurred in full");
            Assert.Greater(near, 0.95f, "near band about gone: a sharp foreground");
            Assert.Greater(middle, 0.95f, "middle band sharp");
        }
        finally { rig.TopInset = 0f; }
    }

    /// <summary>Review I4: an asymmetric case, so a vertically flipped composite or a focus that ignores the top bar fails.
    /// With TopInset 0.3 the focus is at 0.35: the band around 0.35 stays sharp, and 0.70-0.78 (sharp when the focus is at
    /// 0.5) is blurred. At the overview, where the far band is in full (issue #390).</summary>
    [UnityTest]
    public IEnumerator AnOffCentreFocusFollowsTheTopBar()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0.3f; rig.Apply();   // the far band in full (issue #390)
        Assert.AreEqual(0.35f, Shader.GetGlobalVector(TiltShift.ParamsId).x, 1e-5f, "CameraRig.Apply passes the inset");
        TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
        TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
        float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
        float focus = Ratio(0.33f, 0.37f), upper = Ratio(0.70f, 0.78f);
        TestContext.WriteLine($"focus band {focus:0.00}, upper band {upper:0.00}");
        Assert.Greater(focus, 0.95f, "sharp at the inset's focus (0.35)");
        Assert.Less(upper, 0.8f, "blurred above it");
        rig.TopInset = 0f; rig.Apply();
    }
}
