#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Routes;
using UnityEditor.SceneManagement;
using UnityEngine;
using M = Statecraft.Game.Map.Kits.TownModel;

/// <summary>The owner on v0.1.1 (18 October 2026): "the sizes in 0.1.1 dont make any sense - [MATKOT's] buildings are way
/// bigger then the train. this also makes jaffa's buildings be over tel avivs buildings". MATKOT's building models (#700)
/// stand at one world scale, measured against the steam train the map runs (ServiceTrain): a house about one locomotive
/// long, a city block no longer and no taller than the whole train, and no settlement's models over another's.</summary>
public class TownModelScaleTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        KitScale.Reset();
    }

    /// <summary>The service train's length on the map (m): MATKOT's locomotive and tender at RouteTrain.Scale times
    /// ServiceTrain.ModelSize, about 816 m, in ServiceTrain.Cars cars of about 204 m: the locomotive's own car.</summary>
    static float TrainM => RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize;
    static float LocoM => TrainM / ServiceTrain.Cars;

    /// <summary>The large buildings: the city blocks (Tel Aviv, Jerusalem, Haifa), the barn, the dining hall, the winery and
    /// the port's cargo shed. Every other model is a house (or the water tower).</summary>
    static readonly HashSet<M> Large = new HashSet<M>
    {
        M.B01WhiteCity, M.B02aCornerBlock, M.B02bNarrowBlock, M.B03aArches, M.B03bExternalStair, M.B03cPlain,
        M.B04aStoneBalconies, M.B04bLoadingDoors, M.B07DiningHall, M.B23Winery, M.B24Barn, M.B32CargoShed,
    };

    static List<SettlementKits.ModelInstance> Models(out MapScene map)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
        var g = new KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        foreach (var c in map.RouteCorridors) g.AddCorridor(c.line, c.halfWidth);
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        var built = SettlementKits.Build(map.Settlements, map.Localities.Select(p => p.Settlement).ToList(), new List<Localities.Area>(), g, out _);
        foreach (var k in built.Towns) UnityEngine.Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) UnityEngine.Object.DestroyImmediate(t.mesh);
        foreach (var c in built.Close.Values) UnityEngine.Object.DestroyImmediate(c);
        return built.Models;
    }

    /// <summary>A placed model's four ground corners (world x/z) and its height (m): its mesh-space box through its matrix.</summary>
    static (Vector2[] quad, float height) Box(SettlementKits.ModelInstance mi)
    {
        var s = TownModels.Size[(int)mi.Model];
        var quad = new Vector2[4];
        float lo = float.MaxValue, hi = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var c = mi.M.MultiplyPoint3x4(new Vector3(((i & 1) - 0.5f) * s.x, (((i >> 1) & 1) - 0.5f) * s.y, ((i >> 2) & 1) * s.z));
            if (i < 4) quad[i == 2 ? 3 : i == 3 ? 2 : i] = new Vector2(c.x, c.z);   // 0,1,3,2: around the base
            lo = Mathf.Min(lo, c.y); hi = Mathf.Max(hi, c.y);
        }
        return (quad, hi - lo);
    }

    static float LongSide(Vector2[] q) => Mathf.Max((q[1] - q[0]).magnitude, (q[2] - q[1]).magnitude);

    /// <summary>Two convex quads overlap (separating axis test), by more than <paramref name="slackM"/>.</summary>
    static bool Overlap(Vector2[] a, Vector2[] b, float slackM)
    {
        foreach (var poly in new[] { a, b })
            for (int i = 0; i < 4; i++)
            {
                var e = poly[(i + 1) % 4] - poly[i];
                var n = new Vector2(-e.y, e.x).normalized;
                float amin = a.Min(p => Vector2.Dot(n, p)), amax = a.Max(p => Vector2.Dot(n, p));
                float bmin = b.Min(p => Vector2.Dot(n, p)), bmax = b.Max(p => Vector2.Dot(n, p));
                if (amax - slackM <= bmin || bmax - slackM <= amin) return false;
            }
        return true;
    }

    /// <summary>One world scale, against the locomotive: every house model's long side is between 0.2 and 1.5 of the
    /// locomotive's car (about 41 to 306 m: no house longer than one and a half locomotives; the floor is half the smallest
    /// house the kits lay, Jerusalem's Old City cells of about 2.1 m real, 51 m at the world scale, so a scale that collapsed
    /// by half or more fails),
    /// and every large building's long side and every model's height are within the whole train's length (about 816 m).
    /// On v0.1.1 the models filled the kits' plots at the kits' symbolic 60x (blocks) and 100x (houses): a house of 8 m
    /// was 800 m long and a Tel Aviv block up to 1.6 km.</summary>
    [Test]
    public void EveryBuildingModelIsSizedAgainstTheLocomotive()
    {
        var models = Models(out _);
        Assert.Greater(models.Count, 1000);
        float loco = LocoM, train = TrainM;
        Assert.AreEqual(204f, loco, 1f, "the locomotive's car"); Assert.AreEqual(816f, train, 1f, "the service train");
        var bad = new List<string>();
        var houses = new List<float>(); var large = new List<float>(); var heights = new List<float>();
        foreach (var mi in models)
        {
            var (q, h) = Box(mi);
            float side = LongSide(q);
            heights.Add(h);
            if (Large.Contains(mi.Model))
            {
                large.Add(side);
                if (side > train) bad.Add($"{mi.Id} {mi.Model}: {side:0} m long, longer than the train");
            }
            else
            {
                houses.Add(side);
                if (side > 1.5f * loco || side < 0.2f * loco) bad.Add($"{mi.Id} {mi.Model}: {side:0} m long, {side / loco:0.00} locomotives");
            }
            if (h > train) bad.Add($"{mi.Id} {mi.Model}: {h:0} m tall, taller than the train is long");
        }
        float Median(List<float> l) { var s = l.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
        TestContext.WriteLine($"locomotive {loco:0} m, train {train:0} m; houses: median {Median(houses):0} m ({houses.Min():0}-{houses.Max():0}); " +
            $"large: median {Median(large):0} m ({large.Min():0}-{large.Max():0}); heights: median {Median(heights):0} m (max {heights.Max():0})");
        CollectionAssert.IsEmpty(bad.Take(15), $"{bad.Count} of {models.Count} models off the locomotive's scale");
    }

    /// <summary>"jaffa's buildings be over tel avivs buildings": Jaffa (an Arab town in May 1948) keeps its stone houses
    /// (b17) and Tel Aviv its White City blocks, at one scale: Jaffa's tallest house stands lower than the locomotive's car is
    /// long and lower than Tel Aviv's typical block. On v0.1.1 Jaffa's 100x houses rose up to about 490 m, in front of Tel
    /// Aviv's blocks from the camera's side (it looks north), so they covered them.</summary>
    [Test]
    public void JaffasHousesStandLowerThanTelAvivsBlocks()
    {
        var models = Models(out _);
        bool Port(SettlementKits.ModelInstance m) => m.Model == M.B32CargoShed || m.Model == M.B28WaterTower;   // a port's sheds, a water tower
        var jaffa = models.Where(m => m.Id == "jaffa" && !Port(m)).ToList(); var tel = models.Where(m => m.Id == "tel_aviv" && !Port(m)).ToList();
        Assert.Greater(jaffa.Count, 10); Assert.Greater(tel.Count, 20);
        Assert.IsTrue(jaffa.All(m => m.Model == M.B17CourtyardHouse), "Jaffa's stone houses");
        Assert.IsTrue(tel.All(m => m.Model == M.B01WhiteCity || m.Model == M.B02aCornerBlock || m.Model == M.B02bNarrowBlock), "the White City");
        var th = tel.Select(m => Box(m).height).OrderBy(x => x).ToList();
        float jaffaMax = jaffa.Max(m => Box(m).height), telMedian = th[th.Count / 2];
        TestContext.WriteLine($"Jaffa's tallest house {jaffaMax:0} m, Tel Aviv's median block {telMedian:0} m");
        Assert.Less(jaffaMax, LocoM, "Jaffa's tallest house is lower than the locomotive is long");
        Assert.Less(jaffaMax, telMedian, "Jaffa's tallest house is lower than Tel Aviv's median block");
    }

    /// <summary>No settlement's building models stand over another's: no two models of different towns or localities overlap
    /// on the ground (their footprints, by the separating axis test, by more than 1 m).</summary>
    [Test]
    public void NoTwoSettlementsBuildingModelsOverlap()
    {
        var models = Models(out _);
        var boxes = models.Select(m => (m.Id, m.Model, quad: Box(m).quad)).ToList();
        const float cell = 2000f;
        var grid = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < boxes.Count; i++)
        {
            var q = boxes[i].quad;
            int x0 = Mathf.FloorToInt(q.Min(p => p.x) / cell), x1 = Mathf.FloorToInt(q.Max(p => p.x) / cell);
            int z0 = Mathf.FloorToInt(q.Min(p => p.y) / cell), z1 = Mathf.FloorToInt(q.Max(p => p.y) / cell);
            for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++)
                { if (!grid.TryGetValue((x, z), out var l)) grid[(x, z)] = l = new List<int>(); l.Add(i); }
        }
        var pairs = new HashSet<(int, int)>(); var byTowns = new Dictionary<string, int>();
        foreach (var l in grid.Values)
            for (int a = 0; a < l.Count; a++)
                for (int b = a + 1; b < l.Count; b++)
                {
                    int i = Math.Min(l[a], l[b]), j = Math.Max(l[a], l[b]);
                    if (boxes[i].Id == boxes[j].Id || !pairs.Add((i, j))) continue;
                    if (!Overlap(boxes[i].quad, boxes[j].quad, 1f)) continue;
                    var key = string.CompareOrdinal(boxes[i].Id, boxes[j].Id) < 0 ? $"{boxes[i].Id}/{boxes[j].Id}" : $"{boxes[j].Id}/{boxes[i].Id}";
                    byTowns[key] = byTowns.TryGetValue(key, out var n) ? n + 1 : 1;
                }
        TestContext.WriteLine(byTowns.Count == 0 ? "no overlaps" : string.Join("; ", byTowns.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}")));
        CollectionAssert.IsEmpty(byTowns.Keys.Take(15), $"{byTowns.Values.Sum()} overlapping pairs between settlements");
    }
}
