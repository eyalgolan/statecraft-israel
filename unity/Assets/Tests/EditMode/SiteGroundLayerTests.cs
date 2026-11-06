#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Routes;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#2877: a plowed field or a drained swamp shows on its hex's ground (the owner on #2812, "Ground change only"), from
/// State.Sites, as HulaPapyrusLayer redraws from it: nothing to bake, built only for a site that just changed (so no hitch at
/// End turn), and right after a save is loaded.</summary>
public class SiteGroundLayerTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly DateTime Date = new DateTime(1949, 6, 30);
    static SiteWork Built(string id) => SiteWork.Built(id, Date);

    [TearDown]
    public void Empty() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        return map;
    }

    static (int q, int r) HexOf(string id) { var s = C.Sites.Single(x => x.Id == id); return (s.Q, s.R); }

    [Test]
    public void OnlyABuiltFieldOrDrainedSwampWantsGround()
    {
        var wanted = SiteGroundLayer.Wanted(C, new[]
        {
            Built("beit_kama"), Built("tzrifin"), new SiteWork("reim_field", 1, 4, null), Built("gilat_field"), Built("hula"), Built("no_such_site"),
        });
        CollectionAssert.AreEqual(new[] { ("gilat_field", SiteGroundLayer.Field), ("hula", SiteGroundLayer.Drained) }, wanted.Select(w => (w.Site.Id, w.Look)).ToArray());
    }

    [Test]
    public void AHexShowsOneGroundAndTheFieldBeatsTheDrainedSwampItWasBuiltOn()
    {
        // content/sites.yaml: hula_field stands in the swamp's hex and is plowed once the swamp is drained (R-SWAMP-001).
        Assert.AreEqual(HexOf("hula"), HexOf("hula_field"));
        var wanted = SiteGroundLayer.Wanted(C, new[] { Built("hula"), Built("hula_field") });
        CollectionAssert.AreEqual(new[] { ("hula_field", SiteGroundLayer.Field) }, wanted.Select(w => (w.Site.Id, w.Look)).ToArray());
    }

    [Test]
    public void TheGroundStaysOnItsHexAndKeepsOffWaterAndTowns()
    {
        var map = OpenMap();
        var layer = SiteGroundLayer.ShowAt(map, C, new[] { Built("beit_kama_field"), Built("hula"), Built("beit_shemesh_field") })!;
        var ground = PlacementLayer.CampGround(map);
        foreach (var (id, hex) in new[] { "beit_kama_field", "hula", "beit_shemesh_field" }.Select(i => (i, HexOf(i))))
        {
            var mesh = layer.MeshOf(id)!;
            Assert.Greater(mesh.vertexCount, 1000, id + ": the mesh covers the hex");
            Assert.LessOrEqual(mesh.vertexCount, SiteGroundLayer.MaxVertices, id);
            var v = mesh.vertices; var c = mesh.colors32; var n = mesh.normals;
            int shown = 0;
            for (int i = 0; i < v.Length; i++)
            {
                Assert.Greater(n[i].y, 0f, id + ": normals up, as the ground's");
                if (c[i].a == 0) continue;
                shown++;
                Assert.AreEqual(hex, ground.Hex(v[i].x, v[i].z), $"{id} vertex {i}");
                Assert.IsFalse(ground.Water(v[i].x, v[i].z), $"{id} vertex {i} on water");
                foreach (var f in map.KitFootprints)
                    Assert.Greater((f.centre - new Vector2(v[i].x, v[i].z)).magnitude, SiteGroundLayer.ClearShare * f.radius, $"{id} vertex {i} inside {f.id}");
                float y = ground.Y(v[i].x, v[i].z);
                Assert.That(v[i].y - y, Is.InRange(0f, 60f), $"{id} vertex {i} lies on the ground");
            }
            Assert.Greater(shown, 300, id + ": most of the hex shows the change");
        }
    }

    [Test]
    public void TheGroundIsTheSameEveryTime()
    {
        var map = OpenMap();
        var a = SiteGroundLayer.ShowAt(map, C, new[] { Built("gilat_field") })!.MeshOf("gilat_field")!;
        var (v, uv, c) = (a.vertices, a.uv, a.colors32);
        SiteGroundLayer.ShowAt(map, C, Array.Empty<SiteWork>());
        var b = SiteGroundLayer.ShowAt(map, C, new[] { Built("gilat_field") })!.MeshOf("gilat_field")!;
        Assert.AreNotSame(a, b, "dropped and made again");
        CollectionAssert.AreEqual(v, b.vertices); CollectionAssert.AreEqual(uv, b.uv); CollectionAssert.AreEqual(c, b.colors32);
    }

    [Test]
    public void TheSeedIsFixedAcrossRuns()
    {
        // FNV-1a by hand: string.GetHashCode would give another field layout in each run.
        Assert.AreEqual(3907230421u, SiteGroundLayer.Hash("gilat_field"));
    }

    [Test]
    public void OnlyASiteThatChangedIsBuilt()
    {
        var map = OpenMap();
        var layer = SiteGroundLayer.ShowAt(map, C, new[] { Built("gilat_field") })!;
        var first = layer.MeshOf("gilat_field");
        Assert.NotNull(first);
        int built = layer.MeshesBuilt;
        Assert.AreSame(layer, SiteGroundLayer.ShowAt(map, C, new[] { Built("gilat_field") }), "one layer on the map");
        Assert.AreEqual(built, layer.MeshesBuilt, "nothing changed: the next turn's redraw builds nothing");
        SiteGroundLayer.ShowAt(map, C, new[] { Built("gilat_field"), Built("reim_field") });
        Assert.AreEqual(built + 1, layer.MeshesBuilt, "one more site: one more mesh");
        Assert.AreSame(first, layer.MeshOf("gilat_field"));
        CollectionAssert.AreEquivalent(new[] { "gilat_field", "reim_field" }, layer.Shown);
    }

    [Test]
    public void TheShaderIsWarmBeforeAnySiteIsBuilt()
    {
        // The first draw of a shader compiles it, which must not fall on the End turn that finishes the first site.
        var map = OpenMap();
        var layer = SiteGroundLayer.ShowAt(map, C, Array.Empty<SiteWork>())!;
        CollectionAssert.IsEmpty(layer.Shown);
        var warm = layer.transform.Find(SiteGroundLayer.WarmName).GetComponent<MeshRenderer>();
        Assert.AreEqual(SiteGroundLayer.MaterialFor(), warm.sharedMaterial);
        var mesh = warm.GetComponent<MeshFilter>().sharedMesh;
        Assert.AreEqual(1, mesh.vertices.Distinct().Count(), "no area: nothing shows");
        Assert.AreEqual(ShadowCastingMode.Off, warm.shadowCastingMode);
        Assert.Greater(warm.bounds.size.x, map.Frame.WidthM * 0.9f, "never culled, so the draw happens at any view");
        Assert.Greater(warm.bounds.size.z, map.Frame.LengthM * 0.9f);
    }

    [Test]
    public void TheMaterialDrawsUnderTheRoadsAndCompilesForWebGL2()
    {
        var material = SiteGroundLayer.MaterialFor();
        Assert.NotNull(material, "Resources/" + SiteGroundLayer.MaterialResource);
        Assert.Less(material.renderQueue, Resources.Load<Material>(RouteLayer.RailMaterialResource).renderQueue, "a railway crosses a field on top");
        Assert.Less(material.renderQueue, Resources.Load<Material>(RouteLayer.RoadMaterialResource).renderQueue, "a road crosses a field on top");
        Assert.GreaterOrEqual(material.renderQueue, (int)RenderQueue.GeometryLast + 1, "after the ground and the kits");
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(material.shader));
    }

    [Test]
    public void TheToggleSwitchesTheGroundAndGoesWithTheMap()
    {
        var map = OpenMap();
        var layer = SiteGroundLayer.ShowAt(map, C, new[] { Built("gilat_field") })!;
        CollectionAssert.Contains(VisualToggles.Names, SiteGroundLayer.ToggleName);
        VisualToggles.Set(SiteGroundLayer.ToggleName, false);
        Assert.IsFalse(layer.gameObject.activeSelf);
        Assert.AreSame(layer, SiteGroundLayer.ShowAt(map, C, new[] { Built("gilat_field") }), "still found while switched off");
        VisualToggles.Set(SiteGroundLayer.ToggleName, true);
        Assert.IsTrue(layer.gameObject.activeSelf);
        map.Terrain.GetComponent<MapRuntimeAssets>().RunTeardown();
        CollectionAssert.DoesNotContain(VisualToggles.Names, SiteGroundLayer.ToggleName);
    }

    /// <summary>The camera's frame over the map, as 8-bit pixels (batch runs have a GPU: no -nographics).</summary>
    static Color32[] Frame(Camera cam, int size)
    {
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture;
        cam.targetTexture = rt; cam.Render(); cam.targetTexture = before;
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0); tex.Apply();
        RenderTexture.active = prev;
        var px = tex.GetPixels32();
        Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
        return px;
    }

    [Test]
    public void TheHexsGroundVisiblyChangesInARealRenderAndTheWarmMeshDrawsNothing()
    {
        var map = OpenMap();
        const int Size = 512;
        var hex = C.HexAt(-9, 32)!;
        var at = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        var cam = Camera.main;
        cam.aspect = 1f;
        var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Distance = 31000f; rig.Yaw = 0f; rig.Apply();
        // Without a layer, then with the layer and no sites (only the warm mesh), then with the site built.
        var without = Frame(cam, Size);
        SiteGroundLayer.ShowAt(map, C, Array.Empty<SiteWork>());
        var warm = Frame(cam, Size);
        SiteGroundLayer.ShowAt(map, C, new[] { Built("beit_kama_field") });
        var with = Frame(cam, Size);
        int changed = 0, warmChanged = 0, pink = 0;
        for (int i = 0; i < with.Length; i++)
        {
            if (Mathf.Abs(warm[i].r - without[i].r) + Mathf.Abs(warm[i].g - without[i].g) + Mathf.Abs(warm[i].b - without[i].b) > 6) warmChanged++;
            if (Mathf.Abs(with[i].r - without[i].r) + Mathf.Abs(with[i].g - without[i].g) + Mathf.Abs(with[i].b - without[i].b) <= 30) continue;
            changed++;
            if (with[i].r > 200 && with[i].b > 200 && with[i].g < 80) pink++;
        }
        Assert.AreEqual(0, warmChanged, "the warm mesh leaves the frame as it was");
        Assert.Greater(changed, with.Length / 200, "the hex's ground reads as changed at 31 km");
        Assert.Less(changed, with.Length / 4, "only the hex changes");
        Assert.AreEqual(0, pink, "the shader's error colour");
    }

    /// <summary>Through the game itself: GameRoot draws the ground on the bridge's Changed, and a loaded save (SimBridge.Replace,
    /// the load path) draws its own sites and drops what the old game had.</summary>
    [UnityTest]
    public IEnumerator TheGameDrawsTheGroundAndALoadedSaveShowsItsOwn()
    {
        var saves = Path.Combine(Path.GetTempPath(), "statecraft-site-ground-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(saves);
        RenderTexture? rt = null; PanelSettings? ps = null; GameRoot root = null!;
        yield return CivMovementShots.Boot(false, saves, (t, p, u, g) => { rt = t; ps = p; root = g; });
        try
        {
            var map = Object.FindFirstObjectByType<MapScene>();
            var bridge = root.Bridge!;
            var layer = map.Terrain.transform.Find(SiteGroundLayer.Name).GetComponent<SiteGroundLayer>();
            CollectionAssert.IsEmpty(layer.Shown, "a new game has no built field");

            bridge.State.Sites["gilat_field"] = SiteWork.Built("gilat_field", bridge.State.Date);
            bridge.Skip(-2877001);
            CollectionAssert.AreEquivalent(new[] { "gilat_field" }, layer.Shown, "drawn on Changed, as End turn raises it");

            var loaded = GameSession.New(bridge.Session.Content, GoldenRun.Seed, Mode.Historical);
            loaded.State.Sites["reim_field"] = SiteWork.Built("reim_field", loaded.State.Date);
            bridge.Replace(loaded);
            CollectionAssert.AreEquivalent(new[] { "reim_field" }, layer.Shown, "the save's fields, not the old game's");
        }
        finally
        {
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            Directory.Delete(saves, true);
        }
    }
}
