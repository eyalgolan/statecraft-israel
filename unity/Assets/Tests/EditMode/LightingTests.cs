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

/// <summary>Plan Task 2: the sun, the ambient fill, shadows at map scale and the hero zoom.</summary>
public class LightingTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // destroys any map: its teardown restores
        ShadowFit.Restore();
        VisualToggles.Clear();
    }

    [Test]
    public void TheSunIsLowWarmAndFromTheWestNorthWest()
    {
        var go = new GameObject("Sun"); var sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
        try
        {
            MapLighting.Apply(sun);
            var travel = sun.transform.forward;
            Assert.AreEqual(-Mathf.Sin(MapLighting.SunElevation * Mathf.Deg2Rad), travel.y, 1e-4f, "elevation");
            Assert.That(MapLighting.SunElevation, Is.InRange(25f, 35f), "late afternoon: long shadows");
            Assert.Greater(travel.x, 0f, "the light travels east: the sun is in the west");
            Assert.Less(travel.z, 0f, "and a little south: the sun is north of west");
            Assert.Greater(sun.color.r, sun.color.b + 0.2f, "warm");
            Assert.AreEqual(LightShadows.Soft, sun.shadows);
            Assert.AreSame(sun, RenderSettings.sun);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void TheAmbientFillIsCoolFromTheSkyAndDarkFromTheGround()
    {
        MapLighting.Apply(null);
        var c = new Color[2];
        RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up, Vector3.down }, c);
        Assert.Greater(c[0].b, c[0].r, "sky fill is cool");
        Assert.Greater(c[0].grayscale, c[1].grayscale + 0.05f, "brighter from above");
        Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode);
        // Review M4, closed in Task 4: the shaders read Unity's own trilight -> SH conversion of the three colours (the
        // RenderSettings.ambientProbe getter returns it, in the editor and in the WebGL player alike), and it holds linear
        // values. The trilight constants are sRGB, so comparing the evaluated probe with them directly made it look like 40%
        // of nominal. In sRGB the horizon is the equator colour; up and down are blended towards it (an L2 probe cannot
        // hold a sharp trilight). The evaluated linear values stay pinned so a change to the colours is a visible decision.
        var e = new Color[3]; RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up, Vector3.right, Vector3.down }, e);
        TestContext.WriteLine($"ambient evaluated (linear) up {e[0]} horizon {e[1]} down {e[2]}; horizon sRGB {e[1].gamma}");
        var expected = new[] { new Color(0.173f, 0.203f, 0.261f), new Color(0.146f, 0.143f, 0.138f), new Color(0.095f, 0.083f, 0.065f) };   // issue #2427
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(expected[i].r, e[i].r, 0.01f, $"direction {i} r"); Assert.AreEqual(expected[i].g, e[i].g, 0.01f, $"direction {i} g");
            Assert.AreEqual(expected[i].b, e[i].b, 0.01f, $"direction {i} b");
        }
        var horizon = e[1].gamma;
        Assert.AreEqual(MapLighting.AmbientEquator.r, horizon.r, 0.05f, "horizon r (sRGB)"); Assert.AreEqual(MapLighting.AmbientEquator.g, horizon.g, 0.05f, "horizon g (sRGB)");
        Assert.AreEqual(MapLighting.AmbientEquator.b, horizon.b, 0.05f, "horizon b (sRGB)");
        // No explicit probe: one assigned in Trilight mode is replaced by the conversion at once (measured in Task 4).
        var custom = new SphericalHarmonicsL2(); custom.AddAmbientLight(Color.white);
        RenderSettings.ambientProbe = custom;
        var after = new Color[1]; RenderSettings.ambientProbe.Evaluate(new[] { Vector3.right }, after);
        Assert.AreEqual(e[1].r, after[0].r, 1e-4f, "an explicit probe does not stick in Trilight mode");
    }

    /// <summary>Review I1: URP fades shadows by straight-line distance from the camera, starting at (1 - cascadeBorder) of
    /// the shadow distance, so every corner of the view (top corners at 21:9 and the largest top inset included) must lie
    /// inside the unfaded range.</summary>
    [Test]
    public void TheShadowDistanceCoversTheWholeView([Values(6000f, 45000f, 120000f, 250000f)] float distance, [Values(0.1f, 0.2f)] float border)
    {
        var go = new GameObject("Cam"); var cam = go.AddComponent<Camera>(); var rig = go.AddComponent<CameraRig>();
        try
        {
            cam.fieldOfView = CameraRig.Fov;
            foreach (var aspect in new[] { 16f / 9f, 21f / 9f, 4f / 3f })
            foreach (var inset in new[] { 0f, 0.08f, 0.3f, CameraRig.MaxTopInset })
            {
                cam.aspect = aspect;
                rig.LookAt = Vector3.zero; rig.Distance = distance; rig.TopInset = inset; rig.Apply();
                float fitted = ShadowFit.Distance(cam, border);
                foreach (var v in new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f) })
                {
                    var ray = cam.ViewportPointToRay(new Vector3(v.x, v.y, 0));
                    Assert.Less(ray.direction.y, 0f, "the view looks down everywhere");
                    var hit = ray.origin + ray.direction * ((ShadowFit.GroundY - ray.origin.y) / ray.direction.y);
                    var range = (hit - cam.transform.position).magnitude;
                    Assert.LessOrEqual(range, fitted * (1f - border) + 1f, $"{distance} m, aspect {aspect:0.00}, inset {inset}, viewport {v}");
                }
                Assert.LessOrEqual(fitted, ShadowFit.MaxDistanceM);
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void FittingFollowsTheCameraAndRestoringPutsTheAssetBack()
    {
        var asset = UniversalRenderPipeline.asset; Assert.IsNotNull(asset);
        var before = asset!.shadowDistance;
        var go = new GameObject("Cam"); var cam = go.AddComponent<Camera>(); var rig = go.AddComponent<CameraRig>();
        try
        {
            cam.fieldOfView = CameraRig.Fov; cam.aspect = 16f / 9f; rig.LookAt = Vector3.zero;
            rig.Distance = 60000f; rig.Apply(); ShadowFit.Apply(cam);
            var far = ShadowFit.Distance(cam, asset.cascadeBorder);
            Assert.AreEqual(far, asset.shadowDistance, 1e-3f);
            rig.Distance = 10000f; rig.Apply(); ShadowFit.Apply(cam);
            Assert.AreEqual(ShadowFit.Distance(cam, asset.cascadeBorder), asset.shadowDistance, 1e-3f);
            Assert.Less(asset.shadowDistance, far, "follows the zoom");
            rig.Distance = 100f; rig.Apply();
            Assert.AreEqual(ShadowFit.MinDistanceM, ShadowFit.Distance(cam, asset.cascadeBorder));
            ShadowFit.Restore(); Assert.AreEqual(before, asset.shadowDistance);
            ShadowFit.Restore(); Assert.AreEqual(before, asset.shadowDistance, "restoring twice is harmless");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Review M2: leaving play mode (and an assembly reload, which runs the same Restore) puts the asset back even
    /// though the map's teardown list is gone.</summary>
    [Test]
    public void LeavingPlayModeRestoresTheShadowDistance()
    {
        var asset = UniversalRenderPipeline.asset!; var before = asset.shadowDistance;
        var go = new GameObject("Cam"); var cam = go.AddComponent<Camera>(); go.AddComponent<CameraRig>().Apply();
        try
        {
            ShadowFit.Apply(cam); Assert.AreNotEqual(before, asset.shadowDistance);
            ShadowFit.OnPlayModeChanged(PlayModeStateChange.EnteredPlayMode); Assert.AreNotEqual(before, asset.shadowDistance);
            ShadowFit.OnPlayModeChanged(PlayModeStateChange.ExitingPlayMode); Assert.AreEqual(before, asset.shadowDistance);
            ShadowFit.Apply(cam); ShadowFit.OnBeforeAssemblyReload(); Assert.AreEqual(before, asset.shadowDistance);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void DestroyingTheMapRestoresTheShadowDistance()
    {
        var asset = UniversalRenderPipeline.asset!; var before = asset.shadowDistance;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build();
        Assert.AreNotEqual(before, asset.shadowDistance, "fitted to the camera by Build");
        CollectionAssert.Contains(VisualToggles.Names, "shadows");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.AreEqual(before, asset.shadowDistance, "restored with the map");
    }

    /// <summary>Review M1: the sun and the ambient are put back when the map goes, so saving an open Map.unity after a Build
    /// does not bake the runtime lighting into the scene.</summary>
    [Test]
    public void DestroyingTheMapRestoresTheSunAndTheAmbient()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var sun = MapLighting.FindSun()!; Assert.IsNotNull(sun);
        var rot = sun.transform.rotation; var colour = sun.color; var intensity = sun.intensity; var shadows = sun.shadows;
        var strength = sun.shadowStrength; var mode = RenderSettings.ambientMode; var sky = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor; var ground = RenderSettings.ambientGroundColor;
        var up = new Color[1]; RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up }, up);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        Assert.AreNotEqual(rot, sun.transform.rotation, "Build points the sun");
        Object.DestroyImmediate(map.Terrain.gameObject);   // its MapRuntimeAssets runs the teardown
        Assert.AreEqual(0f, Quaternion.Angle(rot, sun.transform.rotation), 1e-3f);
        Assert.AreEqual(colour, sun.color); Assert.AreEqual(intensity, sun.intensity); Assert.AreEqual(shadows, sun.shadows);
        Assert.AreEqual(strength, sun.shadowStrength); Assert.AreEqual(mode, RenderSettings.ambientMode);
        Assert.AreEqual(sky, RenderSettings.ambientSkyColor); Assert.AreEqual(equator, RenderSettings.ambientEquatorColor);
        Assert.AreEqual(ground, RenderSettings.ambientGroundColor);
        var upAfter = new Color[1]; RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up }, upAfter);
        Assert.AreEqual(up[0], upAfter[0]);
    }

    /// <summary>Review M6 (Review Focus 1): the sun is fixed in the world; orbiting the rig moves the light across the screen.</summary>
    [Test]
    public void TheSunStaysFixedInTheWorldWhenTheRigOrbits()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build();
        var sun = MapLighting.FindSun()!; var rig = Camera.main.GetComponent<CameraRig>();
        var forward = sun.transform.forward;
        foreach (var yaw in new[] { 90f, 180f, 270f })
        {
            rig.Yaw = yaw; rig.Apply();
            Assert.AreEqual(0f, Vector3.Angle(forward, sun.transform.forward), 1e-3f, $"yaw {yaw}");
        }
    }

    [Test]
    public void TheHeroZoomComesInToSixKilometres()
    {
        Assert.AreEqual(6000f, CameraController.MinDistance);
        Assert.AreEqual(250000f, CameraController.MaxDistance);
    }

    [Test]
    public void TheWebTierShadowMapIs2048()
    {
        var web = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        Assert.AreEqual(2048, web.mainLightShadowmapResolution);
    }

    /// <summary>Review I1: the web tier (one cascade) fades its shadows over the last tenth of the fitted distance, not the
    /// last fifth, so less of the 2048 map is spent past the view.</summary>
    [Test]
    public void TheWebTierFadesShadowsOverTheLastTenth()
    {
        var web = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        Assert.AreEqual(0.1f, web.cascadeBorder, 1e-4f);
    }

    /// <summary>Rendered, at both tiers (review I2: 0 is the web tier, 1 the PC tier the edit-mode runs default to): over the
    /// Dead Sea escarpment (the steepest relief on the map) the sun casts shadows. The measurement is isolated (review I3):
    /// every other registered visual except the water is off and the settlement markers are hidden, so only relief casts;
    /// and a control at the asset's own 50 m shows that without the fit nothing would. Much of the escarpment's shadow at
    /// 30 degrees falls east onto the Dead Sea; in Track A alone that water is URP-lit terrain and darkens; after the merge
    /// the water is ScWaterShade, which samples the main light's shadow attenuation (Task 7, review I6), so the shadow still
    /// lands on it.</summary>
    [UnityTest]
    public IEnumerator ReliefCastsShadowsAtMapScale([Values(0, 1)] int level)
    {
        int previous = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(level, false);
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
            yield return null;
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>(); var sun = MapLighting.FindSun();
            Assert.IsNotNull(sun, "Map.unity has a directional light");
            foreach (var n in VisualToggles.Names.Where(n => n != "shadows" && n != "water")) VisualToggles.Set(n, false);
            map.transform.Find("Settlements").gameObject.SetActive(false);
            rig.LookAt = map.Frame.ToWorld(35.38, 31.45, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var lit = RenderSample.Render(cam, 480, 270);
            VisualToggles.Set("shadows", false);
            var flat = RenderSample.Render(cam, 480, 270);
            VisualToggles.Set("shadows", true);
            float shadowed = RenderSample.FractionDarker(lit, flat, 0.04f);

            ShadowFit.Restore();   // the control: the asset's own shadow distance (50 m)
            var litUnfitted = RenderSample.Render(cam, 480, 270);
            float control = RenderSample.FractionDarker(litUnfitted, flat, 0.04f);
            TestContext.WriteLine($"level {level}: shadowed share {shadowed:P2}, unfitted control {control:P2}");
            Assert.Less(control, 0.002f, "without the fit the map casts (almost) nothing");
            Assert.Greater(shadowed, 0.02f, "at least 2% of the view is in a cast shadow");
        }
        finally { QualitySettings.SetQualityLevel(previous, false); }
    }
}
