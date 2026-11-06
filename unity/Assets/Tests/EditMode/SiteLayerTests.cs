#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>GDD Phase 1 PR 8 (#381): SiteLayer draws a site being built as a scaffold on its hex, a built site as a rural
/// kit (or the kibbutz marker box with the kits off) on its hex, an available site as nothing, and replaces what it drew.</summary>
public class SiteLayerTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    const string Id = "mefalsim";

    static MapScene Map()
    {
        new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSelection.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sea.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MapFrame.mat"));
        map.Build();
        return map;
    }

    static (int q, int r) SiteHex => C.Sites.Where(x => x.Id == Id).Select(x => (x.Q, x.R)).Single();

    [Test]
    public void EachSiteStateIsDrawnOnItsHexAndRenderReplacesIt()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var map = Map();
            var layer = new GameObject("Sites").AddComponent<SiteLayer>();
            var hex = C.HexAt(SiteHex.q, SiteHex.r)!;
            var centre = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);

            layer.Render(Array.Empty<SiteWork>(), C, map);
            Assert.AreEqual(0, layer.transform.childCount, "an available site draws nothing");

            layer.Render(new[] { new SiteWork(Id, 1, 4, null) }, C, map);
            Assert.AreEqual(1, layer.transform.childCount, "one scaffold");
            var scaffold = layer.transform.GetChild(0);
            Assert.AreEqual(SiteLayer.Prefix + Id + SiteLayer.ScaffoldSuffix, scaffold.name);
            Assert.AreEqual(centre.x, scaffold.position.x, 1f); Assert.AreEqual(centre.z, scaffold.position.z, 1f);
            Assert.AreEqual(SiteHex, MapPointer.Pick(map, scaffold.position), "the scaffold stands on the site's hex");
            Assert.IsEmpty(layer.Columns, "no kit while it is built");

            layer.Render(new[] { SiteWork.Built(Id, new DateTime(1949, 6, 30)) }, C, map);
            Assert.AreEqual(1, layer.transform.childCount, "Render replaced the scaffold");
            var place = layer.transform.GetChild(0);
            Assert.AreEqual(SiteLayer.Prefix + Id, place.name);
            var bounds = place.GetComponent<MeshRenderer>().bounds;
            Assert.AreEqual(SiteHex, MapPointer.Pick(map, new Vector3(bounds.center.x, centre.y, bounds.center.z)), "the place stands on the site's hex");
            if (map.KitsShown)
            {
                Assert.AreNotEqual("Cube", place.GetComponent<MeshFilter>().sharedMesh.name, "a rural kit, not the marker box");
                Assert.IsTrue(layer.Columns.ContainsKey(SiteLayer.Prefix + Id), "its columns are kept for the name pill");
            }
            else Assert.AreEqual("Cube", place.GetComponent<MeshFilter>().sharedMesh.name, "with the kits off, the kibbutz marker box");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    /// <summary>Issue #2812 ("Ground change only"): a built field site in a yishuv's hex is no place of its own. Built Beit
    /// Kama and built Beit Kama fields, both at (-9, 32) in sites.yaml, give one place (one pill) and one kit or box there.</summary>
    [Test]
    public void ABuiltYishuvAndItsBuiltFieldGiveOnePlaceAndOneKitOnTheirHex()
    {
        var yishuv = C.Sites.Single(x => x.Id == "beit_kama"); var field = C.Sites.Single(x => x.Id == "beit_kama_field");
        Assert.AreEqual((-9, 32), (yishuv.Q, yishuv.R)); Assert.AreEqual((yishuv.Q, yishuv.R), (field.Q, field.R), "one hex");
        Assert.Contains(Projects.YishuvKind, yishuv.Kinds); Assert.Contains(Projects.FieldKind, field.Kinds);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var map = Map();
            var layer = new GameObject("Sites").AddComponent<SiteLayer>();
            layer.Render(new[] { SiteWork.Built(yishuv.Id, new DateTime(1949, 4, 18)), SiteWork.Built(field.Id, new DateTime(1949, 6, 30)) }, C, map);
            Assert.AreEqual(new[] { SiteLayer.Prefix + yishuv.Id }, layer.Places.Select(p => p.Id).ToArray(), "the yishuv is the hex's one place");
            Assert.AreEqual(1, layer.transform.childCount, "one kit or box on the hex");
            Assert.AreEqual(SiteLayer.Prefix + yishuv.Id, layer.transform.GetChild(0).name);
            Assert.IsFalse(layer.Columns.ContainsKey(SiteLayer.Prefix + field.Id), "no kit for the field");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    /// <summary>Issue #2812 ("Ground change only"): a drained swamp (the built hula site, kinds [swamp]) is no place: no
    /// pill, no kit and no box.</summary>
    [Test]
    public void ADrainedSwampGivesNoPlaceAndNoKit()
    {
        var swamp = C.Sites.Single(x => x.Id == "hula");
        Assert.Contains(Projects.SwampKind, swamp.Kinds);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var map = Map();
            var layer = new GameObject("Sites").AddComponent<SiteLayer>();
            layer.Render(new[] { SiteWork.Built(swamp.Id, new DateTime(1951, 6, 30)) }, C, map);
            Assert.IsEmpty(layer.Places, "no place, so no pill");
            Assert.AreEqual(0, layer.transform.childCount, "no kit and no box");
            Assert.IsEmpty(layer.Columns);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    /// <summary>Issue #2812: a built camp site (tzrifin, kinds [military_camp]) is still a place with its own camp kit.</summary>
    [Test]
    public void ABuiltCampStillGetsItsOwnKit()
    {
        var camp = C.Sites.Single(x => x.Id == "tzrifin");
        Assert.IsTrue(Projects.IsCamp(camp));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var map = Map();
            var layer = new GameObject("Sites").AddComponent<SiteLayer>();
            layer.Render(new[] { SiteWork.Built(camp.Id, new DateTime(1949, 6, 30)) }, C, map);
            Assert.AreEqual(new[] { SiteLayer.Prefix + camp.Id }, layer.Places.Select(p => p.Id).ToArray(), "the camp is a place");
            Assert.AreEqual(1, layer.transform.childCount);
            Assert.AreEqual(SettlementKits.CampType, SiteLayer.KitOf(camp, layer.Places[0]).Type, "its kit is the camp's");
            var mesh = layer.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
            if (map.KitsShown)
            {
                Assert.AreNotEqual("Cube", mesh.name, "a camp kit, not the marker box");
                Assert.IsTrue(layer.Columns.ContainsKey(SiteLayer.Prefix + camp.Id));
            }
            else Assert.AreEqual("Cube", mesh.name, "with the kits off, the marker box");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
