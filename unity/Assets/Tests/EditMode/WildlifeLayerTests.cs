#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #670: Tal Heuberger's Nubian ibex and striped hyena, sparse and deterministic in their sourced ranges, streamed
/// through CollectionStream only for close zoom and failing open when the animals bundle is unavailable.</summary>
public class WildlifeLayerTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();

    [SetUp]
    public void Seam()
    {
        saved = WildlifeLayer.Request;
        asked.Clear();
        WildlifeLayer.Request = (id, detail, done) => asked.Add((id, detail, done));
    }

    [TearDown]
    public void Reset()
    {
        WildlifeLayer.Request = saved;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        return map;
    }

    static GameObject Near(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));

    [Test]
    public void SeededTerrainSpotsAreStableAndSparse()
    {
        var map = OpenMap();
        var a = WildlifeLayer.Wants(map);
        var b = WildlifeLayer.Wants(map);
        CollectionAssert.AreEqual(a, b, "the same map and fixed seed choose the same spots");
        Assert.AreEqual(WildlifeLayer.IbexCount, a.Count(w => w.Model == WildlifeLayer.Ibex));
        Assert.AreEqual(WildlifeLayer.HyenaCount, a.Count(w => w.Model == WildlifeLayer.Hyena));
        Assert.AreEqual(WildlifeLayer.IbexCount + WildlifeLayer.HyenaCount, a.Count, "never more than a handful on the map");
        var ground = new Statecraft.Game.Map.Kits.KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        var byHex = map.Hexes.hexes.ToDictionary(h => (h.q, h.r));
        foreach (var w in a)
        {
            Assert.GreaterOrEqual(w.At.x, 0f); Assert.GreaterOrEqual(w.At.y, 0f);
            var hex = byHex[ground.Hex(w.At.x, w.At.y)];
            Assert.IsTrue(w.Model == WildlifeLayer.Ibex ? WildlifeLayer.IbexHex(hex, byHex) : WildlifeLayer.HyenaHex(hex),
                $"{w.Key} stands in its sourced zone (hex {hex.q},{hex.r} at {hex.lon:F2} E {hex.lat:F2} N, {hex.biome})");
            Assert.LessOrEqual(WildlifeLayer.Relief(ground, w.At), WildlifeLayer.MaxReliefM, w.Key + " has level enough ground");
            Assert.GreaterOrEqual(a.Where(o => !o.Equals(w)).Select(o => Vector2.Distance(w.At, o.At)).DefaultIfEmpty(float.MaxValue).Min(),
                WildlifeLayer.MinSpacingM, w.Key);
        }
    }

    [Test]
    public void PlacesBothModelsUprightAtCloseZoomThroughTheAnimalsStream()
    {
        var map = OpenMap();
        var layer = WildlifeLayer.Show(map)!;
        CollectionAssert.AreEquivalent(new[] { WildlifeLayer.Ibex, WildlifeLayer.Hyena }, asked.Select(a => a.id));
        Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Near), "close zoom uses the near static animal meshes");
        Assert.AreEqual(CollectionCatalog.Animals, CollectionCatalog.BundleOf(WildlifeLayer.Ibex));
        Assert.AreEqual(CollectionCatalog.Animals, CollectionCatalog.BundleOf(WildlifeLayer.Hyena));

        foreach (var (id, _, done) in asked.ToList()) done(Near(id));
        Assert.AreEqual(WildlifeLayer.IbexCount + WildlifeLayer.HyenaCount, layer.Placed.Count);
        var ground = new Statecraft.Game.Map.Kits.KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        foreach (var p in layer.Placed)
        {
            var prefab = Near(p.Model);
            var child = layer.transform.Find(p.Key);
            Assert.IsNotNull(child, p.Key);
            var want = Quaternion.Euler(0f, p.Yaw, 0f) * prefab.transform.localRotation;
            Assert.Less(Quaternion.Angle(want, child.localRotation), 0.01f, p.Key + " keeps the prefab's -90 X conversion");
            Assert.IsTrue(child.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterial.enableInstancing), p.Key + " is GPU-instanced");
            var rs = child.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Assert.AreEqual(p.At.y, b.min.y, 0.5f, p.Key + " stands with its base at its spot's height");
            var (lo, hi) = WildlifeLayer.GroundSpan(ground, new Vector2(p.At.x, p.At.z));
            Assert.LessOrEqual(hi - b.min.y, 0.5f * WildlifeLayer.MaxReliefM + 0.5f, p.Key + " is not buried in the ground under it");
            Assert.LessOrEqual(b.min.y - lo, 0.5f * WildlifeLayer.MaxReliefM + 0.5f, p.Key + " does not float over the ground under it");
            Assert.AreEqual(WildlifeLayer.LengthM(p.Model), Mathf.Max(b.size.x, b.size.z), 0.5f, p.Key + " is its size at the world scale");
            // Standing as the model stands: its height is the prefab's own standing height times the layer's scale (a yaw
            // about the vertical never changes a height; a model laid on its side would have its width as its height).
            var rest = Object.Instantiate(prefab);
            var rr = rest.GetComponentsInChildren<Renderer>();
            var rb = rr[0].bounds; foreach (var r in rr) rb.Encapsulate(r.bounds);
            Object.DestroyImmediate(rest);
            float scale = child.localScale.x / prefab.transform.localScale.x;
            Assert.AreEqual(rb.size.y * scale, b.size.y, 0.01f * b.size.y, p.Key + " stands up as its model does");
        }
        Assert.IsTrue(WildlifeLayer.CloseAt(WildlifeLayer.CloseEnterM - 1f, null));
        Assert.IsFalse(WildlifeLayer.CloseAt(WildlifeLayer.CloseExitM + 1f, null));
        float band = 0.5f * (WildlifeLayer.CloseEnterM + WildlifeLayer.CloseExitM);
        Assert.IsTrue(WildlifeLayer.CloseAt(band, true));
        Assert.IsFalse(WildlifeLayer.CloseAt(band, false));
    }

    /// <summary>Issue #1033 (the owner, 18 October 2026: "Train scale for all ambient life"): each animal is drawn at the
    /// building models' world scale (SettlementKits.ModelScale), an ibex of about 1.3 m about 31 m long and a striped hyena
    /// of about 1.1 m about 26 m, smaller than the reference house and a sixth or so of the steam train's locomotive car
    /// (RoadVehicles.LocomotiveCarM, about 204 m), where before each was about 1.9 km.</summary>
    [Test]
    public void EveryAnimalIsSmallerThanAHouseAndMuchSmallerThanTheLocomotiveCar()
    {
        var map = OpenMap();
        var layer = WildlifeLayer.Show(map)!;
        foreach (var (id, _, done) in asked.ToList()) done(Near(id));
        Assert.AreEqual(WildlifeLayer.IbexCount + WildlifeLayer.HyenaCount, layer.Placed.Count);
        float house = Statecraft.Game.Map.Kits.SettlementKits.ReferenceHouseM * Statecraft.Game.Map.Kits.SettlementKits.ModelScale;
        float car = Statecraft.Game.Map.Routes.RoadVehicles.LocomotiveCarM;
        foreach (var p in layer.Placed)
        {
            var rs = layer.transform.Find(p.Key).GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float longest = Mathf.Max(b.size.x, b.size.y, b.size.z);
            Assert.Less(longest, house, $"{p.Key}: {longest:0} m against the {house:0} m reference house");
            Assert.That(longest / car, Is.InRange(0.1f, 0.2f), $"{p.Key}: {longest:0} m against the {car:0} m locomotive car");
        }
    }

    [Test]
    public void MissingBundleFailsOpen()
    {
        var map = OpenMap();
        var layer = WildlifeLayer.Show(map)!;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("WILDLIFE: .* did not stream"));
        asked.Single(a => a.id == WildlifeLayer.Ibex).done(null);
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("WILDLIFE: .* did not stream"));
        asked.Single(a => a.id == WildlifeLayer.Hyena).done(null);
        Assert.AreEqual(0, layer.Placed.Count);
        Assert.AreEqual(WildlifeLayer.IbexCount + WildlifeLayer.HyenaCount, layer.Missing.Count);
        Assert.DoesNotThrow(() => WildlifeLayer.Show(map));
    }

    [Test]
    public void ProductionSeamIsCollectionStreamAndModelsAreNotResources()
    {
        Assert.AreEqual(typeof(CollectionStream), saved.Method.DeclaringType);
        Assert.AreEqual(nameof(CollectionStream.Request), saved.Method.Name);
        Assert.IsNull(Resources.Load<GameObject>(WildlifeLayer.Ibex));
        Assert.IsNull(Resources.Load<GameObject>(WildlifeLayer.Hyena));
    }
}
