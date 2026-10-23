#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>GDD Phase 4 PR 8, item 3 (#792): a built site draws the kit of what was built there. A built yishuv is a rural kit
/// (a kibbutz's houses and its dining hall), a built camp is a camp kit (a fenced yard of barracks), never the other's.</summary>
public class SettlementPlacerTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);

    /// <summary>The game's map scene (Map.unity, with its kit materials and town models), built: SiteLayer draws kits only
    /// when the map does.</summary>
    static MapScene Map()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        return map;
    }

    /// <summary>Renders <paramref name="id"/> built, checks a kit (not the marker box) stands on the site's hex, and returns the
    /// kinds of what SiteLayer itself placed for that site (SiteLayer.Placed).</summary>
    static List<string> BuiltKitKinds(string id)
    {
        var map = Map();
        var layer = new GameObject("Sites").AddComponent<SiteLayer>();
        layer.Render(new[] { SiteWork.Built(id, new DateTime(1949, 6, 30)) }, C, map);
        var site = C.Sites.Single(x => x.Id == id);
        var place = layer.Places.Single();
        Assert.AreEqual(SiteLayer.Prefix + id, place.Id, "the site's place");
        Assert.IsTrue(map.KitsShown, "the map draws kits, so the hex check below runs");
        var drawn = layer.transform.GetChild(0);
        Assert.AreNotEqual("Cube", drawn.GetComponent<MeshFilter>().sharedMesh.name, "a kit, not the marker box");
        var b = drawn.GetComponent<MeshRenderer>().bounds;
        Assert.AreEqual((site.Q, site.R), MapPointer.Pick(map, b.center), "the kit stands on the site's hex");
        return layer.Placed.Where(p => p.Id == place.Id).Select(p => p.Kind).ToList();
    }

    [Test]
    public void ABuiltYishuvDrawsAKit()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var kinds = BuiltKitKinds("mefalsim");
            Assert.Contains("dining_hall", kinds, "a kibbutz's dining hall: the rural kit");
            Assert.IsFalse(kinds.Contains("camp_yard") || kinds.Contains("barracks"), "no camp parts: " + string.Join(", ", kinds));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void ABuiltCampDrawsACampKit()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var camps = C.Sites.Where(s => s.Kinds.Contains("military_camp")).Select(s => s.Id).ToList();
            CollectionAssert.IsSubsetOf(new[] { "camp_80", "tzrifin" }, camps, "sites.yaml's camp sites");
            foreach (var id in camps)
            {
                var kinds = BuiltKitKinds(id);
                Assert.AreEqual(1, kinds.Count(k => k == "camp_yard"), id + ": one fenced yard");
                Assert.GreaterOrEqual(kinds.Count(k => k == "barracks"), 4, id + ": rows of barracks");
                Assert.IsFalse(kinds.Contains("dining_hall") || kinds.Contains("building"), id + ": no kibbutz parts: " + string.Join(", ", kinds));
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
