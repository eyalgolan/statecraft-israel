#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The boats (SeaCraft, refs #388): promo 03's white-sailed craft off the coast and moored in the harbours of Jaffa,
/// Tel Aviv and Haifa, 1948's own kinds, every hull in open sea, seeded, and drawn under the kits' root.</summary>
public class SeaCraftTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        KitScale.Reset();
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    static KitGround Ground(MapScene map) =>
        new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);

    static System.Collections.Generic.List<SeaCraft.Craft> Place(MapScene map, KitGround g)
    {
        SettlementKits.Build(map.Settlements, new System.Collections.Generic.List<Settlement>(), new System.Collections.Generic.List<Localities.Area>(), g, out var placed);
        return SeaCraft.Place(g, placed);
    }

    /// <summary>About ten boats under way (feluccas and cutters, never a steamer) off the coast, two or more moored in each of
    /// the three harbours with Haifa's coastal steamer among them, every hull and its margin in open sea (not a rift lake),
    /// no two boats overlapping, and promo 03's size: a sailing boat a third to a half of a hex across.</summary>
    [Test]
    public void TheBoatsSailOffTheCoastAndMoorInTheHarbours()
    {
        var map = OpenMap();
        var g = Ground(map);
        var crafts = Place(map, g);
        var under = crafts.Where(c => c.Kind == SeaCraft.Kind.Felucca || c.Kind == SeaCraft.Kind.Cutter).ToList();
        TestContext.WriteLine(string.Join("\n", crafts.Select(c => $"{c.Kind} {c.Place} at {c.At} yaw {c.Yaw:0} length {c.Length:0} m")));
        Assert.That(under.Count, Is.InRange(9, 12), "about ten boats under way");
        foreach (var port in new[] { "jaffa", "tel_aviv", "haifa" })
            Assert.GreaterOrEqual(crafts.Count(c => c.Place == port && (c.Kind == SeaCraft.Kind.Moored || c.Kind == SeaCraft.Kind.Steamer)), 2, port + ": moored craft");
        Assert.AreEqual(1, crafts.Count(c => c.Kind == SeaCraft.Kind.Steamer && c.Place == "haifa"), "one coastal steamer, at Haifa's quays");
        float hexAcross = Mathf.Sqrt(3f) * g.HexSizeM;
        foreach (var c in under) Assert.That(c.Length / hexAcross, Is.InRange(0.15f, 0.5f), "a sailing boat at promo 03's size");
        foreach (var c in crafts)
        {
            var f = c.Forward; var side = new Vector2(-f.y, f.x);
            foreach (var p in new[] { c.At, c.At + f * 0.5f * c.Length, c.At - f * 0.5f * c.Length, c.At + side * 0.15f * c.Length, c.At - side * 0.15f * c.Length })
            {
                Assert.IsTrue(g.Water(p.x, p.y), $"{c.Kind} at {c.Place}: in the water");
                Assert.Greater(g.ElevM(p.x, p.y), -50f, $"{c.Kind} at {c.Place}: on the sea, not a rift lake");
            }
            foreach (var o in crafts)
                if (!o.Equals(c)) Assert.Greater((o.At - c.At).magnitude, 0.5f * (o.Length + c.Length), $"{c.Kind} at {c.Place} overlaps a {o.Kind}");
        }
    }

    /// <summary>Seeded: the same map places the same boats. They are drawn as one mesh under the kits' root, the "kits"
    /// toggle hides them with the towns, and the build logs their count.</summary>
    [Test]
    public void TheBoatsAreSeededAndDrawnWithTheKits()
    {
        var map = OpenMap();
        var a = Place(map, Ground(map)); var b = Place(map, Ground(map));
        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++) { Assert.AreEqual(a[i].At, b[i].At); Assert.AreEqual(a[i].Yaw, b[i].Yaw); Assert.AreEqual(a[i].Kind, b[i].Kind); }
        var boats = map.transform.Find(KitLayer.RootName).Find(KitLayer.SeaCraftName).GetComponent<MeshRenderer>();
        Assert.AreEqual(a.Count, map.KitStats!.Boats);
        Assert.IsTrue(boats.enabled);
        VisualToggles.Set("kits", false);
        Assert.IsFalse(boats.enabled, "the kits toggle hides the boats");
    }
}
