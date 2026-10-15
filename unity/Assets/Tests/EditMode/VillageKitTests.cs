#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Visual gap S1 (docs/visual-v1.md, "Arab villages: dense, terraced cores"): the 1948 localities' Arab villages and
/// towns are dense packs of small stone houses on a plot lattice, at the house scale (100x), and a village's mosque is a
/// village mosque (a one-storey hall with a short square minaret), not the domed town mosque.</summary>
public class VillageKitTests
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

    /// <summary>The Arab localities (villages and Arab towns, Nesher's mixed kit aside).</summary>
    static List<Settlement> ArabLocalities(MapScene map) => map.Localities.Select(p => p.Settlement)
        .Where(s => SettlementKits.ArchetypeOf(s) == Archetype.ArabTown && !Localities.IsMixed(s.Id)).ToList();

    static bool Inside(Vector2 p, SettlementKits.Placed b)
    {
        float a = b.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        var d = p - b.C;
        float u = d.x * ca + d.y * sa, v = -d.x * sa + d.y * ca;
        return Mathf.Abs(u) <= b.W * 0.5f && Mathf.Abs(v) <= b.D * 0.5f;
    }

    /// <summary>Every Arab locality's houses: the median village (and hill village) at least the village floor, nearly every
    /// village outside the game towns at least six, each house at most the village house's largest real size (5.4 x 5.0 m)
    /// at the house scale, and a hill village's core (inner half radius) built up. Logs one VILLAGES line (the census the PR
    /// quotes) and the villages under six houses.</summary>
    [Test]
    public void ArabVillagesAreDensePacksOfSmallHouses()
    {
        var map = OpenMap();
        TownCensusTests.Census(map, out var placed, out var g);
        var byId = placed.GroupBy(p => p.Id).ToDictionary(x => x.Key, x => x.ToList());
        var counts = new List<int>(); var few = new List<string>(); int openFew = 0, open = 0; var hillCounts = new List<int>(); var hillCore = new List<float>();
        var bad = new List<string>();
        float maxSide = VillageHouseMaxM * KitScale.Houses + 1f;
        foreach (var s in ArabLocalities(map))
        {
            var mine = byId.TryGetValue(s.Id, out var l) ? l : new List<SettlementKits.Placed>();
            var houses = mine.Where(p => p.Kind == "building").ToList();
            counts.Add(houses.Count);
            // A village whose heart lies inside a game settlement's footprint (Silwan in Jerusalem's, Sheikh Muwannis in Tel
            // Aviv's) builds only in its own cell, round the city's houses, which are placed first.
            var wc = map.Frame.ToWorld(s.Lon, s.Lat, 0); var cc = new Vector2(wc.x, wc.z);
            bool inCity = g.Footprints.Any(f => f.id != s.Id && map.Settlements.Any(t => t.Id == f.id) && (cc - f.centre).magnitude < f.radius);
            if (!inCity) open++;
            if (houses.Count < 6) { few.Add($"{s.Id}={houses.Count}{(inCity ? " (in a town)" : "")}"); if (!inCity) openFew++; }
            foreach (var h in houses)
                if (Mathf.Max(h.W, h.D) > maxSide) { bad.Add($"{s.Id}: a house {h.W:0} x {h.D:0} m, over the village house's size"); break; }
            var c = cc;
            if (g.ElevM(c.x, c.y) < 120f) continue;
            // A hill village: the share of its inner half radius (on land) under its houses, sampled every 100 m. Houses only:
            // the mosque alone covers about a third of the core (review of #647), so counting it would pass with no house.
            float R = SettlementKits.FootprintRadiusM(s, g.HexSizeM);
            int n = Mathf.CeilToInt(R / 100f), cells = 0, hit = 0;
            for (int j = -n; j <= n; j++)
                for (int i = -n; i <= n; i++)
                {
                    var p = c + new Vector2(i, j) * 100f;
                    if ((p - c).magnitude > 0.5f * R || g.Water(p.x, p.y)) continue;
                    cells++; if (houses.Any(b => Inside(p, b))) hit++;
                }
            hillCounts.Add(houses.Count); hillCore.Add(hit / (float)Mathf.Max(1, cells));
        }
        counts.Sort(); hillCounts.Sort(); hillCore.Sort();
        int median = counts[counts.Count / 2], hillMedian = hillCounts[hillCounts.Count / 2];
        float coreMedian = hillCore[hillCore.Count / 2];
        string line = $"VILLAGES arab={counts.Count} houses={counts.Sum()} median={median} min={counts[0]} max={counts[counts.Count - 1]} " +
            $"hill={hillCounts.Count} hill_median={hillMedian} hill_core_median={coreMedian:P1} under6={counts.Count(x => x < 6)}";
        TestContext.WriteLine(line); Debug.Log(line);
        TestContext.WriteLine("under six houses: " + string.Join(" ", few));
        Assert.GreaterOrEqual(median, VillageFloor, "the median Arab village draws a dense pack of houses");
        Assert.GreaterOrEqual(hillMedian, VillageFloor, "the median hill village draws a dense pack of houses");
        // Measured 17.5% (houses only, 2026-10-15); the floor sits about 15% under it, as the towns' cover floors do.
        Assert.GreaterOrEqual(coreMedian, 0.15f, "a hill village's core is built up with houses");
        Assert.LessOrEqual(openFew, open / 20, "nearly every village outside the game towns keeps at least six houses");
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} villages with an oversized house");
    }

    /// <summary>The village house's largest real side (m), as SettlementKits' Style.Village draws it.</summary>
    const float VillageHouseMaxM = 5.4f;
    /// <summary>SettlementKits.VillageFloor: the median village keeps at least its floor.</summary>
    const int VillageFloor = 10;

    /// <summary>The mosque rule (docs/visual-v1.md, "Arab villages: dense, terraced cores"): every Muslim locality keeps a
    /// mosque (all of them are places of 1,000 or more people), but a village's is a village mosque, a one-storey hall with a
    /// short square minaret and no dome, smaller than the domed town mosque that only the Arab towns keep.</summary>
    [Test]
    public void OnlyTheArabTownsKeepTheDomedMosque()
    {
        var map = OpenMap();
        TownCensusTests.Census(map, out var placed, out _);
        var kinds = placed.GroupBy(p => p.Id).ToDictionary(x => x.Key, x => x.ToList());
        var bad = new List<string>(); int villages = 0, towns = 0, villageMosques = 0;
        foreach (var s in ArabLocalities(map))
        {
            if (Localities.LandmarkOf(s.Id) != Landmark.Mosque) continue;
            var mine = kinds.TryGetValue(s.Id, out var l) ? l : new List<SettlementKits.Placed>();
            if (s.Type == "village")
            {
                villages++;
                if (mine.Any(p => p.Kind == "mosque")) bad.Add($"{s.Id}: a domed town mosque in a village");
                var vm = mine.FirstOrDefault(p => p.Kind == "village_mosque");
                if (vm.Kind == null) { bad.Add($"{s.Id}: no village mosque"); continue; }
                villageMosques++;
                if (vm.W > 12f * KitScale.Landmarks) bad.Add($"{s.Id}: its village mosque is {vm.W:0} m wide");
            }
            else
            {
                towns++;
                if (!mine.Any(p => p.Kind == "mosque")) bad.Add($"{s.Id}: an Arab town without its mosque");
            }
        }
        TestContext.WriteLine($"{villages} Muslim villages, {villageMosques} village mosques; {towns} Arab towns");
        Assert.AreEqual(villages, villageMosques, "every Muslim village keeps its (village) mosque (owner: keep, every village)");
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} mosques against the rule");
    }
}
