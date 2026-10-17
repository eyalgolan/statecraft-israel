#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
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
}
