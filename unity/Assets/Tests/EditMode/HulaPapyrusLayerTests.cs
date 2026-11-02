#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.HexTiles;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>#2082 on the main map (owner, 2 November 2026: "Hula + Arava, map and boards (Recommended)"): Tal Heuberger's
/// papyrus stands at the Hula swamp's sourced site (content/sites.yaml, site hula, hex (16, 3)), deterministic, at close
/// zoom only, clear of kit footprints, routes and unit tokens, at 3x Tal's scale (owner, 2 November 2026: "3x bigger, like
/// the props (Recommended)"), streamed as its own bundle on the first close
/// view, failing open, and gone once the swamp is drained. The tamarisk has no sourced main-map place ([GAP]).</summary>
public class HulaPapyrusLayerTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();
    static readonly Content content = ContentLoader.Load(Statecraft.Game.Paths.Content);

    [SetUp]
    public void Seam()
    {
        saved = HulaPapyrusLayer.Request;
        asked.Clear();
        HulaPapyrusLayer.Request = (id, detail, done) => asked.Add((id, detail, done));
    }

    [TearDown]
    public void Reset()
    {
        HulaPapyrusLayer.Request = saved;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        return map;
    }

    static GameObject Papyrus() => AssetDatabase.LoadAssetAtPath<GameObject>(HexProps.PrefabPath(HexProps.Papyrus));

    [Test]
    public void TheSiteIsTheSourcedHulaSwampHex()
    {
        // content/sites.yaml: site hula, "Hula swamp", kinds [swamp], at (16, 3), sourced to the 1946 Survey of Palestine map,
        // FRUS 1951 v.V doc 288 and the JTA.
        var site = content.Sites.Single(s => s.Id == HulaPapyrusLayer.SiteId);
        Assert.AreEqual((16, 3), (site.Q, site.R));
        CollectionAssert.Contains(site.Kinds, "swamp");
        Assert.AreEqual((16, 3), HulaPapyrusLayer.SiteHex(content));
        Assert.AreEqual(HexProps.Papyrus, HulaPapyrusLayer.Model);
    }

    [Test]
    public void ClumpsStandInsideTheSitesHexDeterministicallyAndClear()
    {
        var map = OpenMap();
        var a = HulaPapyrusLayer.Wants(map, content);
        var b = HulaPapyrusLayer.Wants(map, content);
        CollectionAssert.AreEqual(a, b, "the same map and fixed seed choose the same clumps");
        Assert.That(a.Count, Is.InRange(HulaPapyrusLayer.ClumpsPerStand, HulaPapyrusLayer.Stands * HulaPapyrusLayer.ClumpsPerStand));
        var ground = new Statecraft.Game.Map.Kits.KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        var token = Statecraft.Game.Map.PlacementLayer.CampGround(map);
        float r = 0.5f * HulaPapyrusLayer.ClumpM;
        foreach (var w in a)
        {
            Assert.AreEqual((16, 3), ground.Hex(w.At.x, w.At.y), w.Key + " stands in site hula's hex");
            Assert.IsFalse(ground.Water(w.At.x, w.At.y), w.Key + " is not in the lake");
            Assert.IsFalse(token.OnRoute(w.At, r), w.Key + " keeps off the routes");
            Assert.IsFalse(token.OnToken(w.At, r), w.Key + " keeps off the unit tokens");
            Assert.IsFalse(map.KitFootprints.Any(f => (f.centre - w.At).magnitude < f.radius + r), w.Key + " keeps off every kit footprint");
            Assert.LessOrEqual(HulaPapyrusLayer.Relief(ground, w.At), HulaPapyrusLayer.MaxReliefM, w.Key);
            Assert.GreaterOrEqual(a.Where(o => o.Key != w.Key).Select(o => Vector2.Distance(o.At, w.At)).DefaultIfEmpty(float.MaxValue).Min(),
                HulaPapyrusLayer.MinClumpSpacingM, w.Key);
        }
        TestContext.WriteLine($"HULA PAPYRUS clumps={a.Count} clumpM={HulaPapyrusLayer.ClumpM:0.0} kit footprints={map.KitFootprints.Count}");
    }

    [Test]
    public void StreamsOnTheFirstCloseViewAndDrawsAtCloseZoomOnlyAtThreeTimesTalsScale()
    {
        var map = OpenMap();
        var layer = HulaPapyrusLayer.ShowAt(map, content, drained: false)!;
        Assert.That(layer.Wanted, Is.GreaterThan(0));
        layer.SetClose(false);
        CollectionAssert.IsEmpty(asked, "nothing is fetched before a close view");
        layer.SetClose(true);
        layer.SetClose(true);
        Assert.AreEqual(1, asked.Count, "asked for once");
        Assert.AreEqual(HexProps.Papyrus, asked[0].id);
        Assert.AreEqual("hex-props-papyrus", HexProps.Catalog.BundleOf(asked[0].id));
        asked[0].done(Papyrus());
        Assert.AreEqual(layer.Wanted, layer.Placed.Count);
        Assert.AreEqual(3f * Statecraft.Game.Map.Kits.SettlementKits.ModelScale, HulaPapyrusLayer.ClumpM, 1e-3f,
            "spacing and clearances are measured on the drawn (3x) clump");
        // The drawn height, from figures outside the layer: Tal's OBJ spans y -0.5 to 0.5 (1 m at file scale 1, as
        // delivered), times the world scale of every model, times the owner's 3 (#2278, 2 November 2026: "3x bigger, like
        // the props (Recommended)"). The boards keep Tal's own scale (HexPropsTests.TalsPapyrusAndTamariskAreTalsMeshesAtTalsScale).
        const float objHeightM = 1f;
        float expectedM = 3f * objHeightM * Statecraft.Game.Map.Kits.SettlementKits.ModelScale;
        var ground = new Statecraft.Game.Map.Kits.KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        foreach (var p in layer.Placed)
        {
            var child = layer.transform.Find(p.Key);
            Assert.IsNotNull(child, p.Key);
            var rs = child.GetComponentsInChildren<Renderer>();
            Assert.IsTrue(rs.All(x => !x.forceRenderingOff), p.Key + " draws at close zoom");
            var b = Bounds(child.gameObject);
            Assert.AreEqual(expectedM, b.size.y, 0.01f * expectedM, p.Key + ": 3x Tal's model at the world scale of every model");
            // On the terrain itself (sampled here, not the layer's own base height): the clump's base lies within the
            // ground's span under its centre and eight points on a circle of half the clump's drawn height.
            float lo = ground.Y(b.center.x, b.center.z), hi = lo;
            for (int k = 0; k < 8; k++)
            {
                float y = ground.Y(b.center.x + Mathf.Cos(k * Mathf.PI / 4f) * 0.5f * expectedM, b.center.z + Mathf.Sin(k * Mathf.PI / 4f) * 0.5f * expectedM);
                lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            Assert.That(b.min.y, Is.InRange(lo - 0.5f, hi + 0.5f), p.Key + " stands on the terrain, neither floating nor buried");
        }
        layer.SetClose(false);
        Assert.IsTrue(layer.GetComponentsInChildren<Renderer>(true).All(x => x.forceRenderingOff), "nothing draws beyond close zoom");
        Assert.AreEqual(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM, HulaPapyrusLayer.CloseEnterM);
        Assert.AreEqual(Statecraft.Game.Map.Kits.TownModelLayer.CloseExitM, HulaPapyrusLayer.CloseExitM);
        Assert.IsTrue(HulaPapyrusLayer.CloseAt(HulaPapyrusLayer.CloseEnterM - 1f, null));
        Assert.AreEqual(Statecraft.Game.Map.Kits.TownModelLayer.CloseAt(HulaPapyrusLayer.CloseEnterM, null),
            HulaPapyrusLayer.CloseAt(HulaPapyrusLayer.CloseEnterM, null), "the band's edges switch as the building models' do");
        Assert.AreEqual(Statecraft.Game.Map.Kits.TownModelLayer.CloseAt(HulaPapyrusLayer.CloseExitM, true),
            HulaPapyrusLayer.CloseAt(HulaPapyrusLayer.CloseExitM, true));
        Assert.IsFalse(HulaPapyrusLayer.CloseAt(HulaPapyrusLayer.CloseExitM + 1f, null));
        float band = 0.5f * (HulaPapyrusLayer.CloseEnterM + HulaPapyrusLayer.CloseExitM);
        Assert.IsTrue(HulaPapyrusLayer.CloseAt(band, true));
        Assert.IsFalse(HulaPapyrusLayer.CloseAt(band, false));
    }

    /// <summary>R-SWAMP-001: a built (drained) Hula site has no swamp, so no papyrus: none is fetched, and a drawn stand goes.</summary>
    [Test]
    public void TheDrainedSwampHasNoPapyrus()
    {
        var state = new GameState();
        Assert.IsFalse(HulaPapyrusLayer.Drained(state));
        state.Sites["hula"] = SiteWork.Built("hula", new DateTime(1951, 6, 1));
        Assert.IsTrue(HulaPapyrusLayer.Drained(state));

        var map = OpenMap();
        var layer = HulaPapyrusLayer.ShowAt(map, content, drained: true)!;
        layer.SetClose(true);
        CollectionAssert.IsEmpty(asked, "a drained swamp fetches nothing");
        Assert.AreEqual(0, layer.Placed.Count);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        map = OpenMap();
        layer = HulaPapyrusLayer.ShowAt(map, content, drained: false)!;
        layer.SetClose(true);
        asked.Single().done(Papyrus());
        Assert.That(layer.Placed.Count, Is.GreaterThan(0));
        Assert.AreSame(layer, HulaPapyrusLayer.ShowAt(map, content, drained: true));
        Assert.AreEqual(0, layer.Placed.Count, "draining removes the stand");
        Assert.AreEqual(0, layer.transform.childCount);
    }

    [Test]
    public void AMissingModelFailsOpen()
    {
        var map = OpenMap();
        var layer = HulaPapyrusLayer.ShowAt(map, content, drained: false)!;
        layer.SetClose(true);
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("HULA PAPYRUS: the model did not stream"));
        asked.Single().done(null);
        Assert.AreEqual(0, layer.Placed.Count);
        Assert.DoesNotThrow(() => HulaPapyrusLayer.ShowAt(map, content, drained: false));
    }

    [Test]
    public void ProductionSeamIsTheHexPropsStreamAndTheModelIsNotInResources()
    {
        Assert.AreEqual(typeof(HulaPapyrusLayer), saved.Method.DeclaringType);
        Assert.AreEqual("RequestProp", saved.Method.Name, "the default seam is the layer's hex-props request");
        var got = new List<GameObject?>();
        var stream = new CollectionStream(new CollectionStream.EditorSource(), HexProps.Catalog);
        Assert.IsTrue(stream.Enqueue(HexProps.Papyrus, CollectionDetail.Near, got.Add));
        Assert.IsNull(Resources.Load<GameObject>(HexModels.PathOf(HexProps.Papyrus)));
        Assert.IsNull(CollectionCatalog.BundleOf(HexProps.Papyrus), "not in The 1948 Collection");
        // The game draws the layer from GameRoot's placement pass, with the drained state.
        var root = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameRoot.cs"));
        StringAssert.Contains("HulaPapyrusLayer.ShowAt(map, Bridge.Content,", root);
        StringAssert.Contains("HulaPapyrusLayer.Drained(Bridge.State)", root);
    }

    /// <summary>[GAP] (#1951): the owner's ruling puts the tamarisk at Arava oases, but the repo has no sourced Arava spring
    /// or oasis place: AravaOasis.cs cites KKL-JNF for "palm oases grew round springs in the central Arava" and names no
    /// spring; tools/hex_tiles/regions.yaml's arava region picks arava_oasis by random weight; content/sites.yaml has no
    /// spring or oasis site. So no place is invented: the tamarisk stands on the hex boards only, and nothing on the main map
    /// asks for it. When a sourced place lands, this test is the one to change.</summary>
    [Test]
    public void TheMainMapHasNoTamarisk_Gap()
    {
        Assert.IsFalse(content.Sites.Any(s => s.Kinds.Any(k => k.Contains("oasis") || k.Contains("spring"))),
            "a sourced oasis or spring site now exists: place the tamarisk there");
        var scripts = Path.Combine(Application.dataPath, "Scripts");
        var hexTiles = Path.Combine(scripts, "Map", "HexTiles") + Path.DirectorySeparatorChar;
        var offBoard = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.StartsWith(hexTiles, StringComparison.Ordinal))
            .Where(f => File.ReadAllText(f).Contains("arava_oasis/tamarisk") || File.ReadAllText(f).Contains("HexProps.Tamarisk"))
            .Select(f => f.Substring(scripts.Length + 1)).ToArray();
        CollectionAssert.IsEmpty(offBoard, "main-map code that asks for the tamarisk");
    }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
