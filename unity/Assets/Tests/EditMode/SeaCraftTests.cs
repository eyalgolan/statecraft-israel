#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The boats (SeaCraft, refs #388): promo 03's white-sailed craft off the coast and moored in and off the harbours
/// of Jaffa, Tel Aviv and Haifa, craft of the period only, every hull in open sea and off the harbour works, seeded, and
/// drawn under the kits' root.</summary>
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

    static System.Collections.Generic.List<SettlementKits.Placed> Census(MapScene map, KitGround g)
    {
        SettlementKits.Build(map.Settlements, new System.Collections.Generic.List<Settlement>(), new System.Collections.Generic.List<Localities.Area>(), g, out var placed);
        return placed;
    }

    static System.Collections.Generic.List<SeaCraft.Craft> Place(MapScene map, KitGround g) => SeaCraft.Place(g, Census(map, g));

    /// <summary>About ten boats under way (feluccas and cutters, never a steamer) off the coast, two or more moored in or off
    /// each of the three harbours with Haifa's coastal steamer at anchor among them, every hull and its margin in open sea (not a rift lake),
    /// no two boats overlapping, and promo 03's size: a sailing boat about a quarter of a hex across (0.15-0.5 allowed).</summary>
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
        Assert.AreEqual(1, crafts.Count(c => c.Kind == SeaCraft.Kind.Steamer && c.Place == "haifa"), "one coastal steamer, at anchor off Haifa");
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

    /// <summary>Review of #536: no hull ever meets a harbour work. At five landmark scales (30-120, the default 60 among
    /// them) and 16 draws each (SeaCraft.Place's salt), no boat's rectangle (its length by 30% of it) overlaps a
    /// breakwater or mole piece the kits recorded at Jaffa, Tel Aviv or Haifa; the kits record some at each.</summary>
    [Test]
    public void NoHullEverMeetsABreakwaterOrMole()
    {
        var map = OpenMap();
        int checkedBoats = 0;
        foreach (var scale in new[] { 30f, 45f, 60f, 90f, 120f })
        {
            KitScale.CityBlocks = scale;
            var g = Ground(map);
            var placed = Census(map, g);
            var works = placed.Where(p => p.Kind == "breakwater").ToList();
            foreach (var port in new[] { "jaffa", "tel_aviv", "haifa" })
                Assert.Greater(works.Count(w => w.Id == port), 0, $"{port}: the kits record its harbour works at scale {scale}");
            for (ulong salt = 0; salt < 16; salt++)
                foreach (var c in SeaCraft.Place(g, placed, salt))
                {
                    checkedBoats++;
                    foreach (var w in works)
                        Assert.IsFalse(SeaCraft.Overlap(c.At, c.Yaw, 0.5f * c.Length, 0.15f * c.Length, w.C, w.Yaw, 0.5f * w.W, 0.5f * w.D),
                            $"scale {scale}, draw {salt}: a {c.Kind} at {c.Place} ({c.At}) meets a {w.Id} breakwater piece at {w.C}");
                }
        }
        TestContext.WriteLine($"{checkedBoats} boats checked against the harbour works");
        Assert.Greater(checkedBoats, 500);
    }

    /// <summary>The rectangle test itself: crossing, touching and parted rectangles, turned and not.</summary>
    [Test]
    public void TheRectangleOverlapTestIsRight()
    {
        Assert.IsTrue(SeaCraft.Overlap(Vector2.zero, 0f, 10f, 2f, new Vector2(0f, 0f), 90f, 10f, 2f), "a cross");
        Assert.IsFalse(SeaCraft.Overlap(Vector2.zero, 0f, 10f, 2f, new Vector2(0f, 5f), 0f, 10f, 2f), "parallel, apart");
        Assert.IsFalse(SeaCraft.Overlap(Vector2.zero, 45f, 10f, 1f, new Vector2(9f, 0f), 45f, 10f, 1f), "turned, side by side");
        Assert.IsTrue(SeaCraft.Overlap(Vector2.zero, 45f, 10f, 1f, new Vector2(7f, 7f), 45f, 10f, 1f), "turned, end to end");
        Assert.IsFalse(SeaCraft.Overlap(Vector2.zero, 0f, 10f, 2f, new Vector2(13f, 13f), 45f, 4f, 1f), "a corner apart");
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
