#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public class BuildingLodProbeTests
{
    GameObject root = null!;
    MeshRenderer source = null!, card = null!;
    Camera camera = null!;
    BuildingLodProbe probe = null!;

    [SetUp] public void Setup()
    {
        root = new GameObject("LOD test");
        MeshRenderer Renderer(string name)
        {
            var go = new GameObject(name, typeof(MeshRenderer)); go.transform.SetParent(root.transform);
            return go.GetComponent<MeshRenderer>();
        }
        source = Renderer("source"); card = Renderer("card"); card.enabled = false;
        source.shadowCastingMode = ShadowCastingMode.On;
        var view = new GameObject("camera", typeof(Camera)); view.transform.SetParent(root.transform);
        camera = view.GetComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -20000);
        probe = root.AddComponent<BuildingLodProbe>(); probe.Configure(source, card, camera, 10000);
    }

    [TearDown] public void Cleanup() => Object.DestroyImmediate(root);

    [Test] public void DefaultIsOriginal()
    {
        Assert.That(probe.ExperimentEnabled, Is.False);
        AssertOriginal();
    }

    [Test] public void DistantCapturedViewUsesCardAndRetainsOriginalShadow()
    {
        probe.ExperimentEnabled = true; probe.Refresh();
        Assert.That(probe.UsingImpostor, Is.True);
        Assert.That(card.enabled, Is.True);
        Assert.That(source.enabled, Is.True);
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));
    }

    [Test] public void CloseViewAutomaticallyReturnsToOriginal()
    {
        probe.ExperimentEnabled = true; probe.Refresh();
        camera.transform.position = new Vector3(0, 0, -3000); probe.Refresh();
        AssertOriginal();
    }

    [Test] public void ChangedViewOrProjectionReturnsToOriginal()
    {
        probe.ExperimentEnabled = true;
        camera.transform.rotation = Quaternion.Euler(0, 10, 0); probe.Refresh(); AssertOriginal();
        camera.transform.rotation = Quaternion.identity;
        camera.transform.position += new Vector3(1000, 0, 0); probe.Refresh(); AssertOriginal();
        camera.transform.position = new Vector3(0, 0, -20000);
        camera.orthographic = true; probe.Refresh(); AssertOriginal();
    }

    [Test] public void ChangedBuildingTransformReturnsToOriginal()
    {
        probe.ExperimentEnabled = true;
        source.transform.position = Vector3.right; probe.Refresh(); AssertOriginal();
    }

    [Test] public void ALongerDistanceIsOutsideThisSingleViewPrototype()
    {
        probe.ExperimentEnabled = true;
        camera.transform.position = new Vector3(0, 0, -30000); probe.Refresh(); AssertOriginal();
    }

    [TestCase(ShadowCastingMode.TwoSided)] [TestCase(ShadowCastingMode.ShadowsOnly)]
    public void UnsupportedShadowModesKeepOriginal(ShadowCastingMode mode)
    {
        Object.DestroyImmediate(probe); source.shadowCastingMode = mode;
        probe = root.AddComponent<BuildingLodProbe>(); probe.Configure(source, card, camera, 10000);
        probe.ExperimentEnabled = true; probe.Refresh();
        Assert.That(probe.UsingImpostor, Is.False); Assert.That(card.enabled, Is.False);
        Assert.That(source.shadowCastingMode, Is.EqualTo(mode));
    }

    [Test] public void ChangedOrDestroyedBakeLightKeepsOriginal()
    {
        var light = new GameObject("light", typeof(Light)); light.transform.SetParent(root.transform);
        var sun = light.GetComponent<Light>();
        probe.Configure(source, card, camera, 10000, sun); probe.ExperimentEnabled = true; probe.Refresh();
        Assert.That(probe.UsingImpostor, Is.True);
        sun.intensity += 1f; probe.Refresh(); AssertOriginal();
        Object.DestroyImmediate(light); probe.Refresh(); AssertOriginal();
    }

    [Test] public void DisablingOrDestroyingProbeRestoresBorrowedRenderer()
    {
        probe.ExperimentEnabled = true; probe.Refresh(); probe.enabled = false; AssertOriginal();
        probe.enabled = true; probe.Refresh(); Object.DestroyImmediate(probe);
        Assert.That(source.enabled, Is.True);
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        Assert.That(card.enabled, Is.False);
    }

    [Test] public void ShadowOffRemainsOffAndDoesNotPayForAnInvisibleSource()
    {
        Object.DestroyImmediate(probe);
        source.shadowCastingMode = ShadowCastingMode.Off;
        probe = root.AddComponent<BuildingLodProbe>();
        probe.Configure(source, card, camera, 10000); probe.enabled = true;
        probe.ExperimentEnabled = true; probe.Refresh();
        Assert.That(source.enabled, Is.False); Assert.That(card.enabled, Is.True);
        probe.enabled = false;
        Assert.That(source.enabled, Is.True);
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
    }

    [Test] public void OriginallyDisabledSourceIsNeverEnabled()
    {
        // Reconfiguration restores the first borrowed source before snapshotting the new one.
        probe.enabled = false;
        var other = new GameObject("disabled source", typeof(MeshRenderer)); other.transform.SetParent(root.transform);
        var disabled = other.GetComponent<MeshRenderer>(); disabled.enabled = false;
        probe.Configure(disabled, card, camera, 10000); probe.enabled = true;
        probe.ExperimentEnabled = true; probe.Refresh();
        Assert.That(disabled.enabled, Is.False); Assert.That(card.enabled, Is.False);
    }

    [Test] public void MissingCameraOrCardRestoresOriginal()
    {
        probe.ExperimentEnabled = true; probe.Refresh();
        Object.DestroyImmediate(camera.gameObject); probe.Refresh(); AssertOriginal();
        Object.DestroyImmediate(card.gameObject); probe.Refresh();
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
    }

    [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
    public void InvalidDistanceIsRefused(float distance) =>
        Assert.Throws<ArgumentException>(() => probe.Configure(source, card, camera, distance));

    [Test] public void ShippedSourceImportsWithItsOriginalUvAndMaterial()
    {
        var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingLodCapture.SourceFolder + BuildingLodCapture.Asset + ".fbx");
        var mesh = sourceAsset.GetComponentInChildren<MeshFilter>().sharedMesh;
        Assert.That(HexModels.IndexTriangles(mesh), Is.EqualTo(6000));
        Assert.That(mesh.subMeshCount, Is.EqualTo(1));
        Assert.That(mesh.HasVertexAttribute(VertexAttribute.TexCoord0), Is.True);
        var material = AssetDatabase.LoadAssetAtPath<Material>(BuildingLodCapture.SourceFolder + BuildingLodCapture.Asset + ".mat");
        Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null);
        Assert.That(material.shader.isSupported, Is.True);
    }

    [Test] public void FixtureUsesAnExistingRealHexAndARealPrototypePlacement()
    {
        var board = HexBoard.Load("tel_aviv");
        var hex = board.Hexes[(-3, 21)]; Assert.That(hex.prototype, Is.EqualTo("moshava_village"));
        var ctx = new HexTileContext(hex.q, hex.r, new HexBoardSurface(board), 1948);
        HexPrototypes.Create(hex.prototype).Build(ctx);
        Assert.That(System.Linq.Enumerable.Any(ctx.Placements, p => p.Model == "sandbox:kit_moshava"), Is.True);
    }

    [Test] public void FixtureRefusesContaminationFromExistingSceneObjects() =>
        Assert.Throws<InvalidOperationException>(() => BuildingLodCapture.Create());

    [Test] public void FixtureTeardownRestoresLightingAndDestroysGeneratedVolumeComponents()
    {
        Object.DestroyImmediate(root);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var mode = RenderSettings.ambientMode; var sky = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor; var ground = RenderSettings.ambientGroundColor;
        var sun = RenderSettings.sun;
        var pipeline = UniversalRenderPipeline.asset; float shadowDistance = pipeline.shadowDistance;
        var fixture = BuildingLodCapture.Create(); root = fixture.root;
        var volume = root.GetComponentInChildren<Volume>();
        var profile = volume.profile; var components = profile.components.ToArray();
        Assert.That(pipeline.shadowDistance, Is.EqualTo(shadowDistance), "temporary bake restores shared URP state");
        Assert.That(fixture.probe.UsingImpostor, Is.True);
        var generated = root.GetComponentsInChildren<MeshRenderer>();
        Assert.That(System.Linq.Enumerable.Any(generated, r => r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount == 4), Is.True);
        Object.DestroyImmediate(root);
        Assert.That(profile == null, Is.True);
        Assert.That(System.Linq.Enumerable.All(components, c => c == null), Is.True);
        Assert.That(RenderSettings.ambientMode, Is.EqualTo(mode));
        Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(sky));
        Assert.That(RenderSettings.ambientEquatorColor, Is.EqualTo(equator));
        Assert.That(RenderSettings.ambientGroundColor, Is.EqualTo(ground));
        Assert.That(RenderSettings.sun, Is.EqualTo(sun));
    }

    void AssertOriginal()
    {
        Assert.That(probe.UsingImpostor, Is.False);
        Assert.That(source.enabled, Is.True);
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        Assert.That(card.enabled, Is.False);
    }
}
