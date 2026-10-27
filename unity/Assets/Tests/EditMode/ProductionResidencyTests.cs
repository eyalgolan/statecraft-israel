using System;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using UnityEngine;

public class ProductionResidencyTests
{
    static Type PolicyType => typeof(MapScene).Assembly.GetType("Statecraft.Game.Map.Collection.ProductionResidency")!;
    // Production startup must not depend on a private diagnostic bootstrap.
    [Test]
    public void NormalMapOwnsAnExplicitProductionPolicyBeforeAnyCategoryRequests()
    {
        var go = new GameObject("production-policy");
        try
        {
            var map = go.AddComponent<MapScene>();
            var property = typeof(MapScene).GetProperty("Residency", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(property, "MapScene must pass an explicit production policy to its consumers");
            Assert.NotNull(property!.GetValue(map));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [Test]
    public void ApprovedDefaultsHaveOwnerEvidenceAndDiagnosticsAreOff()
    {
        Assert.NotNull(PolicyType, "Shipping residency must have validated owner-approved configuration");
        var create = PolicyType.GetMethod("Approved", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(create);
        var config = create!.Invoke(null, null)!;
        Assert.AreEqual(false, PolicyType.GetProperty("Diagnostics", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(config));
        StringAssert.Contains("owner ruling, 26 Oct 2026, #1273: Yes, build it", PolicyType.GetField("Evidence", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as string);
    }
    [Test]
    public void EveryApprovedCategoryAndTownUsesTheExactOwnerValues()
    {
        var p = ProductionResidency.Approved();
        foreach (var c in new[] { p.People, p.Rural, p.Special, p.Units, p.SharedRoad })
        {
            Assert.AreEqual(6000, c.EnterM); Assert.AreEqual(8000, c.ExitM);
            Assert.AreEqual(30000, c.ViewEnterM); Assert.AreEqual(40000, c.ViewExitM);
            Assert.AreEqual(1, c.CameraGraceFrames); Assert.AreEqual(ProductionResidency.Evidence, c.Evidence);
        }
        Assert.IsTrue(p.Towns.Enabled); Assert.AreEqual(0, p.Towns.Enter); Assert.AreEqual(0, p.Towns.Exit);
        Assert.AreEqual(0, p.Towns.Edge); Assert.AreEqual(0, p.Towns.MaxEdges); Assert.AreEqual(1, p.Towns.Grace);
        Assert.AreEqual(120000, p.Towns.ViewEnter); Assert.AreEqual(160000, p.Towns.ViewExit);
        Assert.Throws<ArgumentNullException>(() => new ProductionResidency(null!, p.Rural, p.Special, p.Units, p.SharedRoad, p.Towns));
        Assert.Throws<ArgumentException>(() => new ProductionResidency.TownPolicy(true, 0, 0, 0, 0, 1, float.NaN, 1, "bad"));
    }
    /// <summary>Owner ruling, 26 Oct 2026, #1273: "Craft visible at 60 km (Recommended)". The sea/air craft are not under
    /// residency: the approved policy has no craft category, and the map's craft layer installs none, so it asks for its
    /// models once in view (as before residency) and they show at the 60 km gameplay view.</summary>
    [Test]
    public void TheSeaAirCraftAreNotUnderResidencyByOwnerRuling()
    {
        Assert.IsNull(typeof(ProductionResidency).GetField("Craft", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
            "no craft residency policy (owner ruling, 26 Oct 2026, #1273: Craft visible at 60 km)");
        var saved = SeaAirCollection.Request;
        SeaAirCollection.Request = (_, __, done) => done(null);
        try
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Map.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            var map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
            var layer = SeaAirCollection.ShowAt(map, new DateTime(1948, 6, 15), Array.Empty<Statecraft.Sim.Placement>(),
                Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content))!;
            Assert.IsNull(typeof(SeaAirCollection).GetField("residencyView", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(layer),
                "the map's craft layer installs no residency (owner ruling, 26 Oct 2026, #1273)");
        }
        finally
        {
            SeaAirCollection.Request = saved;
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        }
    }
    [Test]
    public void NewSessionHasAnIndependentPolicyAndDiagnosticsResetCannotUninstallIt()
    {
        var a = new GameObject("session one"); var b = new GameObject("session two");
        try
        {
            var first = a.AddComponent<MapScene>().Residency; var second = b.AddComponent<MapScene>().Residency;
            Assert.AreNotSame(first, second);
            CollectionResidencyMetrics.Enable(); CollectionResidencyMetrics.ResetDiagnostics();
            Assert.IsFalse(CollectionResidencyMetrics.Enabled);
            Assert.AreSame(first, a.GetComponent<MapScene>().Residency);
            Assert.AreSame(second, b.GetComponent<MapScene>().Residency);
        }
        finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
    }
    [Test]
    public void UnitSessionResetDropsOldCacheAndTestSourceWithoutRemovingMapPolicy()
    {
        Statecraft.Game.Map.Units.UnitFigures.Source = (_, __, ___) => { };
        Statecraft.Game.Map.Units.UnitFigures.ResetSession();
        Assert.IsNull(Statecraft.Game.Map.Units.UnitFigures.Source);
        Assert.IsFalse(Statecraft.Game.Map.Units.UnitFigures.Arrived(Statecraft.Game.Map.Units.UnitFigures.YoungRecruit));
        Assert.AreEqual(6000, ProductionResidency.Approved().Units.EnterM);
    }
    [Test]
    public void TownViewExitUsesApproved160KmBandRatherThanTheOldStrategicCap()
    {
        var root = new GameObject("town hysteresis"); var layer = root.AddComponent<TownModelLayer>();
        var camera = new GameObject("town camera").AddComponent<Camera>();
        try
        {
            layer.ConfigureProduction(ProductionResidency.Approved().Towns);
            layer.ConfigureWholeTowns(new[] { ("town", new Bounds(Vector3.zero, new Vector3(1000, 0, 1000))) });
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.transform.position = new Vector3(0, 100000, 0); layer.PrepareCamera(camera);
            Assert.IsTrue(layer.WantsWholeTown("town", camera));
            camera.transform.position = new Vector3(0, 150000, 0); layer.PrepareCamera(camera); layer.PrepareCamera(camera);
            Assert.IsTrue(layer.WantsWholeTown("town", camera), "retained focus must last through the approved exit band");
            camera.transform.position = new Vector3(0, 170000, 0); layer.PrepareCamera(camera);
            Assert.IsFalse(layer.WantsWholeTown("town", camera));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(camera.gameObject); }
    }
    [Test]
    public void InFootprintSelectionIsPreciseAtTheActualLargeWorldCoordinates()
    {
        var root = new GameObject("precision town"); var layer = root.AddComponent<TownModelLayer>();
        var camera = new GameObject("precision camera").AddComponent<Camera>();
        var at = new Vector3(83776.73f, 0, 384540.30f);
        try
        {
            layer.ConfigureProduction(ProductionResidency.Approved().Towns);
            layer.ConfigureWholeTowns(new[] { ("town", new Bounds(at, new Vector3(1000, 0, 1000))) });
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.farClipPlane = 1000000;
            camera.transform.position = at + Vector3.up * 100000;
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f));
            new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float along);
            layer.PrepareCamera(camera);
            Assert.IsTrue(layer.WantsWholeTown("town", camera), "precision ray=" + ray + " ground=" + ray.GetPoint(along));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(camera.gameObject); }
    }
}
