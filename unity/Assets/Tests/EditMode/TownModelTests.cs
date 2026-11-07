#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using M = Statecraft.Game.Map.Kits.TownModel;

/// <summary>Issue #659 step 2: the towns are built from Tal Heuberger's building models, by town type (the
/// issue's table), one model per placed building, fitted to the kit's footprints (the layout and census unchanged), loaded
/// with the map through the scene (never through a Resources folder), drawn instanced: the originals below the close switch
/// (#1273), their far-off copies beyond it (#2378).</summary>
public class TownModelTests
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
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
        var layer = map.GetComponentInChildren<TownModelLayer>();
        layer.ConfigureResidency(1000000, 1000001, 1000000, 1000, "GEOMETRY TEST ONLY; not production defaults");
        return map;
    }

    static SettlementKits.Result Build(MapScene map, out List<SettlementKits.Placed> placed)
    {
        var g = new KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        foreach (var c in map.KitRouteCorridors) g.AddCorridor(c.line, c.halfWidth);   // as the map builds its kits (#786)
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        var built = SettlementKits.Build(map.Settlements, map.Localities.Select(p => p.Settlement).ToList(), new List<Localities.Area>(), g, out placed);
        foreach (var k in built.Towns) UnityEngine.Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) UnityEngine.Object.DestroyImmediate(t.mesh);
        return built;
    }

    /// <summary>The issue's table, per game settlement: the models its buildings may be (its water tower's and its port's
    /// included).</summary>
    static HashSet<M> Allowed(Settlement s)
    {
        var moshava = new[] { M.B05aMoshavaHouse, M.B16Farmhouse, M.B24Barn, M.B28WaterTower };
        switch (s.Id)
        {
            case "tel_aviv": return new HashSet<M> { M.B01WhiteCity, M.B02aCornerBlock, M.B02bNarrowBlock, M.B32CargoShed };
            case "haifa": return new HashSet<M> { M.B04aStoneBalconies, M.B04bLoadingDoors, M.B32CargoShed, M.B05aMoshavaHouse };
            // Jerusalem's village plots are stone-village plots: house 05 only (owner, 2 November 2026: "we need everything to be
            // in the new generation"); its city blocks keep #507's b03 models, which no newer model replaces yet.
            case "jerusalem": return new HashSet<M> { M.B03aArches, M.B03bExternalStair, M.B03cPlain, M.G05CourtyardHouse };
            // Tal Heuberger's Netanya houses (01 to 03) in Netanya only, without #507's b13a (owner, 2 November 2026), and the
            // basalt house (04) in Tiberias only (#756).
            case "netanya": return new HashSet<M> { M.G01NetanyaTiledHouse, M.G02NetanyaBalconyHouse, M.G03NetanyaPorchHouse, M.B28WaterTower };
            case "tiberias": return new HashSet<M> { M.G04BasaltHouse };
        }
        switch (s.Type)
        {
            case "kibbutz": return new HashSet<M> { M.B07DiningHall, M.B09aCommunal, M.B24Barn, M.B28WaterTower };   // #820: sheds are barns
            case "moshav": return new HashSet<M>(moshava) { M.B09aCommunal };   // #659: Nahalal's public building
            case "town" when s.Id != "tiberias" && s.Id != "safed":
                var h = new HashSet<M>(moshava) { M.B13aTwoStorey };
                if (TownModels.WineMoshava(s.Id)) h.Add(M.B23Winery);
                return h;
            default: return new HashSet<M> { M.G05CourtyardHouse };   // the old towns and the Arab towns (Safed too): house 05 only, no b17
        }
    }

    [Test]
    public void EveryTownDrawsItsBuildingsFromItsTypesModels()
    {
        var map = OpenMap();
        var built = Build(map, out var placed);
        var outposts = Outposts(placed);
        Assert.IsNotEmpty(outposts, "Revivim is drawn as its walled lookout (#825)");
        var bad = new List<string>();
        foreach (var s in map.Settlements)
        {
            var used = built.Models.Where(m => m.Id == s.Id).Select(m => m.Model).ToList();
            TestContext.WriteLine($"{s.Id} ({s.Type}): {used.Count} models: " +
                string.Join(", ", used.GroupBy(x => x).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")));
            if (used.Count == 0) bad.Add(s.Id + " has no models");
            var wrong = used.Where(u => !Allowed(s).Contains(u)).Distinct().ToList();
            if (wrong.Count > 0) bad.Add($"{s.Id} ({s.Type}) uses {string.Join(", ", wrong)}");
            // The b07 dining hall stands on the kibbutz's dining hall (#820's landmark), one each, and nowhere else.
            var halls = placed.Where(p => p.Id == s.Id && p.Kind == "dining_hall").ToList();
            var b07 = built.Models.Where(m => m.Id == s.Id && m.Model == M.B07DiningHall).ToList();
            if (b07.Count != halls.Count) bad.Add($"{s.Id}: {b07.Count} b07 models for {halls.Count} dining halls");
            // A lookout's hall stands in its yard's shrunk close copy, not on the kit's plot (RevivimAndNahalalDrawB09aAndB07UpClose).
            else if (halls.Count == 1 && !outposts.Contains(s.Id) && (b07[0].C - halls[0].C).magnitude > 1f) bad.Add($"{s.Id}: the b07 model is not on the dining hall");
            if (TownModels.WineMoshava(s.Id) && used.Count(u => u == M.B23Winery) != 1) bad.Add(s.Id + ": not one winery");
        }
        // The cities' own models are there (not only a fallback).
        foreach (var (id, model) in new[] { ("tel_aviv", M.B01WhiteCity), ("tel_aviv", M.B02bNarrowBlock), ("haifa", M.B04aStoneBalconies),
                     ("haifa", M.B32CargoShed), ("jerusalem", M.B03cPlain), ("jaffa", M.G05CourtyardHouse), ("safed", M.G05CourtyardHouse),
                     ("acre", M.G05CourtyardHouse), ("netanya", M.G01NetanyaTiledHouse), ("netanya", M.G02NetanyaBalconyHouse), ("netanya", M.G03NetanyaPorchHouse),
                     ("tiberias", M.G04BasaltHouse) })
            if (!built.Models.Any(m => m.Id == id && m.Model == model)) bad.Add($"{id} has no {model}");
        // The generated houses stand only where they were placed: 01 to 03 in Netanya, 04 in Tiberias (game settlements and
        // 1948 localities alike). And the owner, 2 November 2026: "we need everything to be in the new generation": no game
        // settlement or locality (the mixed ones included) draws #507's b17 courtyard house, and #507's b13a two-storey house
        // stands only on the game towns' TownBlock plots that no newer model replaces (never in Netanya).
        var townCentres = map.Settlements.Where(s => s.Type == "town" && s.Id != "netanya").Select(s => s.Id).ToHashSet();
        foreach (var m in built.Models)
        {
            if ((m.Model == M.G01NetanyaTiledHouse || m.Model == M.G02NetanyaBalconyHouse || m.Model == M.G03NetanyaPorchHouse) && m.Id != "netanya") bad.Add($"{m.Id} has {m.Model}");
            if (m.Model == M.G04BasaltHouse && m.Id != "tiberias") bad.Add($"{m.Id} has {m.Model}");
            if (m.Model == M.B17CourtyardHouse || (m.Model == M.B13aTwoStorey && !townCentres.Contains(m.Id))) bad.Add($"{m.Id} has {m.Model}");
        }
        // A 1948 Arab village is built of the plastered courtyard house (05) alone.
        var villages = map.Localities.Select(p => p.Settlement).Where(s => s.Type == "village" && !Localities.IsMixed(s.Id)).Select(s => s.Id).ToHashSet();
        var villageModels = built.Models.Where(m => villages.Contains(m.Id) && m.Model != M.B28WaterTower).Select(m => m.Model).Distinct().ToList();
        CollectionAssert.AreEquivalent(new[] { M.G05CourtyardHouse }, villageModels, "the villages' houses: house 05 only");
        // Every farm shed of every kibbutz (#820: the game settlements' and the localities') is a b24 barn, and no kibbutz
        // house is.
        var kibbutzim = map.Settlements.Concat(map.Localities.Select(p => p.Settlement)).Where(s => s.Type == "kibbutz").Select(s => s.Id).ToHashSet();
        // At the world scale a shed's plot holds a group of barns (SettlementKits.PlaceModel): one plot of barns per shed.
        int barns = built.Models.Count(m => kibbutzim.Contains(m.Id) && m.Model == M.B24Barn);
        int barnPlots = built.Models.Where(m => kibbutzim.Contains(m.Id) && m.Model == M.B24Barn).Select(m => (m.Id, m.Plot)).Distinct().Count();
        TestContext.WriteLine($"kibbutz sheds {built.FarmSheds}, b24 barns in kibbutzim {barns} on {barnPlots} plots, dining halls {built.KibbutzHalls}");
        Assert.Greater(built.FarmSheds, 0, "the kibbutzim have farm sheds");
        Assert.AreEqual(built.FarmSheds, barnPlots, "one plot of b24 barns per kibbutz farm shed");
        CollectionAssert.IsEmpty(bad);
    }

    /// <summary>The places drawn as a Negev lookout's walled yard (#825), by their census record.</summary>
    static HashSet<string> Outposts(List<SettlementKits.Placed> placed) =>
        placed.Where(p => p.Kind == "outpost_yard").Select(p => p.Id).ToHashSet();

    /// <summary>Every building the census records (and every water tower and port warehouse) is built of models on its plot:
    /// at the world scale (SettlementKits.ModelScale) a kit plot holds a grid of them (a water tower, a warehouse, a dining
    /// hall and a place's one-off building one), each turned with the plot (up to SettlementKits.MaxTurnDeg off it, #1032) and inside its footprint; no other model is
    /// placed.</summary>
    [Test]
    public void EveryPlacedBuildingIsOneModelOnItsPlot()
    {
        var map = OpenMap();
        var built = Build(map, out var placed);
        var outposts = Outposts(placed);   // a lookout's models stand in its yard copy, not on kit plots (RevivimAndNahalalDrawB09aAndB07UpClose)
        var towns = map.Settlements.Select(s => s.Id).Where(id => !outposts.Contains(id)).ToHashSet();
        var want = placed.Where(p => towns.Contains(p.Id)
            && (p.Kind == "building" || p.Kind == "water_tower" || p.Kind == "warehouse" || p.Kind == "dining_hall" || p.Kind == "public_building")).ToList();
        Assert.IsTrue(want.Any(p => p.Kind == "dining_hall"), "the kibbutzim's dining halls are in the census (#820)");
        var got = built.Models.Where(m => towns.Contains(m.Id)).ToList();
        TestContext.WriteLine($"{want.Count} town buildings in the census, {got.Count} town models, {built.Models.Count} models with the localities");
        var byKey = got.ToLookup(m => (m.Id, Mathf.RoundToInt(m.Plot.x), Mathf.RoundToInt(m.Plot.y)));
        Assert.AreEqual(want.Count, byKey.Count, "every placed building's plot has its models, and no model stands elsewhere");
        var bad = new List<string>();
        var corners = new List<Vector3>();
        foreach (var p in want)
        {
            var at = byKey[(p.Id, Mathf.RoundToInt(p.C.x), Mathf.RoundToInt(p.C.y))].ToList();
            if (at.Count == 0) { bad.Add($"{p.Id} {p.Kind} at {p.C}: no model"); continue; }
            if (p.Kind != "building" && at.Count != 1) bad.Add($"{p.Id} {p.Kind} at {p.C}: {at.Count} models");
            float a = p.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            foreach (var mi in at)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(mi.Yaw, p.Yaw)) > SettlementKits.MaxTurnDeg + 0.01f) bad.Add($"{p.Id} {p.Kind} at {p.C}: turned off its plot");   // #1032: up to MaxTurnDeg
                corners.Clear(); mi.Corners(corners);
                foreach (var c in corners)
                {
                    float dx = c.x - p.C.x, dz = c.z - p.C.y;
                    if (Mathf.Abs(dx * ca + dz * sa) > 0.5f * p.W + 0.5f || Mathf.Abs(-dx * sa + dz * ca) > 0.5f * p.D + 0.5f)
                    { bad.Add($"{p.Id} {p.Kind} at {p.C}: a model outside its plot"); break; }
                }
            }
        }
        TestContext.WriteLine($"{got.Count} town models on {byKey.Count} plots");
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} mismatches");
        Assert.Greater(built.Models.Count, got.Count, "the 1948 localities' houses are models too");
    }

    /// <summary>Owner ruling, 18 October 2026: "Whole model far, buildings near". Every town
    /// with a whole-town model (TownLandmarks.ModelFor: the cities, the six moshavot, every kibbutz, the moshav and every
    /// Arab town) is built of the single building models up close: every house, hall, water tower and warehouse its kit
    /// places is a model on its plot. What keeps the kit up close is a landmark no model fits (the list below). Nahalal's
    /// public building is a b09a and Revivim's walled lookout, whose buildings stand in its yard's close copy rather than on
    /// their kit plots, has its own test (the owner, 18 October 2026: "Use b09a and b07"). A new kind of building in these
    /// towns fails here until it is given a model or named as a landmark.</summary>
    [Test]
    public void EveryTownWithAWholeTownModelIsTheBuildingModelsUpClose()
    {
        var map = OpenMap();
        var built = Build(map, out var placed);
        // With the four major cities, which have no whole-town model: their building models draw below the close switch
        // (owner, 7 November 2026, "Tal's models close (Recommended)"), their kits from 40 km out (TownModels.IsMajorCity).
        var mapped = map.Settlements.Where(s => Statecraft.Game.Map.Collection.TownLandmarks.ModelFor(s) != null
            || TownLandmarksTests.BigCities.Contains(s.Id)).ToList();
        TestContext.WriteLine($"{mapped.Count} towns with a whole-town model or major cities: {string.Join(", ", mapped.Select(s => s.Id))}");
        var ids = mapped.Select(s => s.Id).ToHashSet();
        CollectionAssert.IsSubsetOf(new[] { "tel_aviv", "jerusalem", "haifa", "rishon_lezion", "degania_alef", "nahalal", "nazareth" }, ids);
        Assert.AreEqual(new[] { "arab_town", "city", "kibbutz", "moshav", "town" }, mapped.Select(s => s.Type).Distinct().OrderBy(t => t).ToArray());
        var modelled = new HashSet<string> { "building", "dining_hall", "water_tower", "warehouse", "public_building" };
        var landmarks = new HashSet<string>
        {
            "al_aqsa", "breakwater", "church", "citadel", "clock_tower", "dome_of_the_rock", "gate", "german_colony", "haram",
            "lighthouse", "mosque", "palm", "port", "village_mosque", "walls", "white_tower",
        };
        var outposts = Outposts(placed);
        CollectionAssert.AreEqual(new[] { "revivim" }, outposts.Where(ids.Contains).ToArray(), "the one walled lookout (RevivimAndNahalalDrawB09aAndB07UpClose)");
        var at = built.Models.ToLookup(m => (m.Id, Mathf.RoundToInt(m.Plot.x), Mathf.RoundToInt(m.Plot.y)));
        var bad = new List<string>();
        int houses = 0;
        foreach (var p in placed.Where(p => ids.Contains(p.Id) && !outposts.Contains(p.Id)))
        {
            if (p.Kind == "wing") continue;   // a stepped block's lower wing: inside its block's plot, which its model fills
            if (modelled.Contains(p.Kind))
            {
                houses++;
                if (!at[(p.Id, Mathf.RoundToInt(p.C.x), Mathf.RoundToInt(p.C.y))].Any()) bad.Add($"{p.Id}: a {p.Kind} at {p.C} with no model");
            }
            else if (!landmarks.Contains(p.Kind)) bad.Add($"{p.Id}: {p.Kind} keeps the kit up close");
        }
        TestContext.WriteLine($"{houses} buildings in those towns, all models: {bad.Count == 0}");
        Assert.Greater(houses, 100);
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} kit buildings left up close");
    }

    /// <summary>Issue #659, the owner on 18 October 2026: "Use b09a and b07" at Revivim and Nahalal, the two places whose
    /// kit buildings had no fitting model. Up close Nahalal's public building is one b09a communal building on its plot, and
    /// the kit's hall no longer draws there. Revivim's walled lookout keeps its wall and tower (no model of either), and its
    /// four buildings are models: the dining hall a b07 and the other three b09a, each inside the yard's close copy, so they
    /// stand within its wall at the models' scale.</summary>
    [Test]
    public void RevivimAndNahalalDrawB09aAndB07UpClose()
    {
        var map = OpenMap();
        var built = Build(map, out var placed);
        var hall = placed.Single(p => p.Id == "nahalal" && p.Kind == "public_building");
        var onHall = built.Models.Where(m => m.Id == "nahalal" && (m.Plot - hall.C).magnitude < 1f).Select(m => m.Model).ToArray();
        CollectionAssert.AreEqual(new[] { M.B09aCommunal }, onHall, "Nahalal's public building up close");
        Assert.IsFalse(built.CloseLandmarks.Any(l => l.Id == "nahalal" && l.Kind == "public_building"), "Nahalal's kit hall still draws up close");
        var revivim = built.Models.Where(m => m.Id == "revivim").ToList();
        TestContext.WriteLine($"revivim: {string.Join(", ", revivim.Select(m => $"{m.Model} at {m.C}"))}");
        Assert.AreEqual(1, revivim.Count(m => m.Model == M.B07DiningHall), "Revivim's dining hall is a b07");
        Assert.AreEqual(3, revivim.Count(m => m.Model == M.B09aCommunal), "Revivim's other three buildings are b09a");
        Assert.AreEqual(4, revivim.Count, "Revivim's four buildings, no other model");
        var yard = built.CloseLandmarks.Single(l => l.Id == "revivim" && l.Kind == "outpost_yard");
        var corners = new List<Vector3>();
        foreach (var mi in revivim)
        {
            corners.Clear(); mi.Corners(corners);
            foreach (var c in corners)
                Assert.IsTrue(Mathf.Abs(c.x - yard.C.x) <= 0.5f * yard.W + 1f && Mathf.Abs(c.z - yard.C.y) <= 0.5f * yard.D + 1f,
                    $"Revivim's {mi.Model} at {mi.C} stands outside the yard's close copy ({yard.C}, {yard.W:0} x {yard.D:0} m)");
        }
    }

    /// <summary>A model stands on its plot at the world scale (SettlementKits.ModelScale, the owner on v0.1.1: the buildings
    /// were "way bigger then the train"): a plot w x d x h at the kit's scale k stands for a building w/k x d/k x h/k real,
    /// so the model's longer side is max(w, d) / k x ModelScale along the plot's longer side, one scale on both ground axes
    /// (its own proportions, not stretched to the plot), centred on the plot, on the ground, as tall as h / k x ModelScale
    /// (city blocks exactly; a house within its stretch limits), at any yaw, never mirrored.</summary>
    [Test]
    public void AModelStandsOnItsPlotAtTheWorldScale([Values(0f, 20f, 90f, 137f)] float yaw, [Values(false, true)] bool flip)
    {
        const float kit = 60f;
        float toWorld = SettlementKits.ModelScale / kit;
        foreach (M model in Enum.GetValues(typeof(M)))
            foreach (var (w, d) in new[] { (840f, 600f), (500f, 900f) })
            {
                var mi = SettlementKits.Fit("t", model, new Vector2(1000f, 2000f), yaw, w, d, 1200f, 50f, exactHeight: true, flip, kit);
                var s = TownModels.Size[(int)model];
                var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var hi = -lo;
                float a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                float maxU = 0f, maxV = 0f;
                for (int i = 0; i < 8; i++)
                {
                    var c = mi.M.MultiplyPoint3x4(new Vector3(((i & 1) - 0.5f) * s.x, (((i >> 1) & 1) - 0.5f) * s.y, ((i >> 2) & 1) * s.z));
                    lo = Vector3.Min(lo, c); hi = Vector3.Max(hi, c);
                    float dx = c.x - 1000f, dz = c.z - 2000f;
                    maxU = Mathf.Max(maxU, Mathf.Abs(dx * ca + dz * sa)); maxV = Mathf.Max(maxV, Mathf.Abs(-dx * sa + dz * ca));
                }
                Assert.AreEqual(50f, lo.y, 0.01f, $"{model} stands on the ground"); Assert.AreEqual(50f + 1200f * toWorld, hi.y, 0.05f, $"{model} height");
                float along = 2f * (w >= d ? maxU : maxV), across = 2f * (w >= d ? maxV : maxU);
                Assert.AreEqual(Mathf.Max(w, d) * toWorld, along, 0.05f, $"{model}: its longer side at the world scale, yaw {yaw}");
                Assert.AreEqual(Mathf.Min(s.x, s.y) / Mathf.Max(s.x, s.y), across / along, 1e-3f, $"{model} keeps its own proportions");
                Assert.AreEqual(2f * maxU, mi.W, 0.05f, $"{model}: W along the yaw"); Assert.AreEqual(2f * maxV, mi.D, 0.05f, $"{model}: D across");
                Assert.Greater(mi.M.determinant, 0f, $"{model}: not mirrored (the faces keep their winding)");
                var house = SettlementKits.Fit("t", model, Vector2.zero, yaw, w, d, 99999f, 0f, exactHeight: false, flip, kit);
                float natural = house.H / SettlementKits.MaxStretch;
                Assert.AreEqual(s.z * Mathf.Max(w, d) * toWorld / Mathf.Max(s.x, s.y), natural, natural * 1e-3f, $"{model}: a house's height stops at its stretch limit");
            }
    }

    /// <summary>The models load with the map (owner, 16 October 2026): the map scene's MapScene references the town model
    /// list, which lists exactly the issue's models with their meshes and materials, in TownModel order; neither the list
    /// nor any model is reachable from a Resources folder.</summary>
    [Test]
    public void TheMapSceneLeavesOriginalModelsToStreamingAndNoResourcesFolderLoadsThem()
    {
        var set = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
        Assert.IsNotNull(set, TownModels.SetPath);
        set.Validate();
        Assert.AreEqual(Enum.GetValues(typeof(M)).Length, TownModels.Ids.Length);
        for (int i = 0; i < TownModels.Ids.Length; i++)
        {
            string id = TownModels.Ids[i];
            Assert.AreEqual($"{TownModels.Folder}/{id}/{id}.fbx", AssetDatabase.GetAssetPath(set.meshes[i]), ((M)i).ToString());
            Assert.AreEqual($"{TownModels.Folder}/{id}/{id}.mat", AssetDatabase.GetAssetPath(set.materials[i]), ((M)i).ToString());
            // The fitting table matches the imported mesh: Z-up, centred on x and y, standing on z = 0.
            var b = set.meshes[i].bounds; var s = TownModels.Size[i];
            Assert.AreEqual(s.x, b.size.x, 0.006f, id + " width"); Assert.AreEqual(s.y, b.size.y, 0.006f, id + " depth");
            Assert.AreEqual(s.z, b.size.z, 0.006f, id + " height");
            Assert.AreEqual(0f, b.center.x, 0.006f, id); Assert.AreEqual(0f, b.center.y, 0.006f, id); Assert.AreEqual(0f, b.min.z, 0.006f, id + " base");
        }
        var deps = AssetDatabase.GetDependencies("Assets/Map.unity", true);
        CollectionAssert.DoesNotContain(deps, TownModels.SetPath, "#1273: the map scene must not eagerly load the original list");
        foreach (var id in TownModels.Ids)
        {
            CollectionAssert.DoesNotContain(deps, $"{TownModels.Folder}/{id}/{id}.fbx", id + " streams after the first frame");
            CollectionAssert.DoesNotContain(deps, TownModels.FarStem(id) + ".asset", id + ": its far-off copy streams too");
        }
        Assert.IsFalse(TownModels.SetPath.Split('/').Any(x => x.Equals("Resources", StringComparison.OrdinalIgnoreCase)));
        var inResources = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)
                && p.Split('/').Any(x => x.Equals("Resources", StringComparison.OrdinalIgnoreCase))).ToArray();
        Assert.IsNotEmpty(inResources);
        CollectionAssert.DoesNotContain(AssetDatabase.GetDependencies(inResources, true), TownModels.SetPath, "the list is reached from a Resources folder");
    }

    /// <summary>The fail-open check (#700): a model list that is missing, or lacks a model or a far copy, never leaves a town's
    /// plots empty. The map still builds, says so in an error, and every town keeps its kit buildings at every zoom, the
    /// close views included (no model is drawn and no kit mesh gives way to its close mesh). The editor's bake still
    /// refuses an incomplete list (Validate).</summary>
    [UnityTest]
    public IEnumerator AMissingModelLeavesTheTownsTheirKits()
    {
        TownModelSet Clone()
        {
            var full = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
            // Kept through OpenScene, which unloads unreferenced objects: an unloaded list would reach MapScene as Unity's
            // fake null and test the "no list" case twice.
            var c = ScriptableObject.CreateInstance<TownModelSet>(); c.hideFlags = HideFlags.DontUnloadUnusedAsset;
            c.meshes = (Mesh[])full.meshes.Clone(); c.materials = (Material[])full.materials.Clone();
            c.mid = (Mesh[])full.mid.Clone(); c.far = (Mesh[])full.far.Clone(); c.farMaterials = (Material[])full.farMaterials.Clone();
            return c;
        }
        var probe = Clone();
        try
        {
            Assert.IsNull(probe.Missing(), "the full list");
            probe.far[(int)M.B17CourtyardHouse] = null!;
            StringAssert.Contains("B17CourtyardHouse has no far mesh", probe.Missing());
            Assert.Throws<System.IO.InvalidDataException>(() => probe.Validate(), "the bake refuses a missing far copy");
            probe.far[(int)M.B17CourtyardHouse] = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath).far[(int)M.B17CourtyardHouse];
            probe.farMaterials[(int)M.B17CourtyardHouse] = null!;
            StringAssert.Contains("B17CourtyardHouse has no far material", probe.Missing());
            probe.farMaterials[(int)M.B17CourtyardHouse] = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath).farMaterials[(int)M.B17CourtyardHouse];
            probe.meshes[(int)M.B17CourtyardHouse] = null!;
            StringAssert.Contains("B17CourtyardHouse has no mesh", probe.Missing());
            var empty = ScriptableObject.CreateInstance<TownModelSet>();
            StringAssert.Contains("want 23", empty.Missing());
            UnityEngine.Object.DestroyImmediate(empty);
        }
        finally { UnityEngine.Object.DestroyImmediate(probe); }

        foreach (bool none in new[] { true, false })
        {
            string what = none ? "no model list" : "a list without b01";
            TownModelSet? broken = null;
            try
            {
                EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
                if (!none) { broken = Clone(); broken.meshes[(int)M.B01WhiteCity] = null!; }   // after OpenScene (see Clone)
                var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
                // Set directly: an unsaved list assigned through SerializedObject reached MapScene as null, which would test
                // the "no list" case twice. The scene is not saved.
                map.ConfigureTownModels(broken);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "town models: " + (none ? "the map scene has no model list" : "B01WhiteCity has no mesh") + "; the towns keep their kits at every zoom"));
                map.Build();
                yield return null;
                var root = map.transform.Find(KitLayer.RootName);
                Assert.IsNotNull(root, what + ": the kits are built");
                var layer = root.GetComponentInChildren<TownModelLayer>(); Assert.IsNotNull(layer, what);
                Assert.IsFalse(layer.HasModels, what);
                var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                var closeRs = renderers.Where(r => r.name.EndsWith(KitLayer.CloseSuffix)).ToList();
                var kitRs = renderers.Where(r => !r.name.EndsWith(KitLayer.CloseSuffix)).ToList();
                Assert.Greater(kitRs.Count, 40, what + ": kit meshes");
                var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
                foreach (var pose in PromoPose.CloseUps)
                {
                    pose.Apply(rig, map.Frame); RenderSample.Render(cam, 320, 180);
                    Assert.IsFalse(layer.LastClose, what + ", " + pose.Name);
                    Assert.AreEqual(0, layer.LastDrawn, what + ", " + pose.Name + ": no model");
                    Assert.IsTrue(kitRs.All(r => !r.forceRenderingOff), what + ", " + pose.Name + ": every kit mesh draws");
                    Assert.IsTrue(closeRs.All(r => r.forceRenderingOff), what + ", " + pose.Name + ": no close mesh draws");
                }
            }
            finally { if (broken != null) UnityEngine.Object.DestroyImmediate(broken); }
        }
    }

    /// <summary>The record FarModelLevels writes beside each far-off copy (the fields this test reads).</summary>
    [Serializable]
    sealed class FarRecord
    {
        public string source = "", source_sha256 = "", colour_map = "", colour_map_sha256 = "", provenance = "";
        public int triangles = -1, target_triangles = -1;
    }

    /// <summary>#1273 near, #2378 far. Below the close switch every town draws Tal Heuberger's original FBX mesh, GPU-only,
    /// with no baked lighter substitute: the near tier and its mid alias (the hex-tile sandbox's "@mid" ids) are that mesh.
    /// Beyond the close switch the towns draw the far-off copies (owner, 3 November 2026: "Ask Tal; allow them far off only",
    /// then "I have Tal's OK; go ahead", "it must be high quality"): a derived mesh of at most TownModels.FarTriangles
    /// triangles, under a fifth of the original, with Tal's shader and a colour map baked from Tal's (never Tal's own JPG),
    /// made from the sources whose hashes its record holds, so a far copy older than its source fails here. The far prefab
    /// (what its bundle streams) depends on none of the original's files.</summary>
    [Test]
    public void TheNearTierIsTheOriginalAndTheFarTierIsItsDerivedCopy()
    {
        var set = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
        set.Validate();
        foreach (M model in Enum.GetValues(typeof(M)))
        {
            var full = set.Mesh(model); string id = TownModels.Ids[(int)model];
            string fbx = $"{TownModels.Folder}/{id}/{id}.fbx", jpg = $"{TownModels.Folder}/{id}/{id}_basecolor.jpg";
            Assert.AreEqual(fbx, AssetDatabase.GetAssetPath(full), model + " full original");
            Assert.Greater(full.GetIndexCount(0), 0, model + " geometry present");
            Assert.IsFalse(full.isReadable, model + " GPU-only");
            Assert.AreSame(full, set.mid[(int)model], model + ": no generated LOD or substitute near");
            var far = set.FarMesh(model);
            Assert.AreEqual(TownModels.FarStem(id) + ".asset", AssetDatabase.GetAssetPath(far), model + " far-off copy");
            int triangles = (int)(far.GetIndexCount(0) / 3), source = (int)(full.GetIndexCount(0) / 3);
            Assert.That(triangles, Is.InRange(1, TownModels.FarTriangles), model + " far triangles");
            Assert.Less(triangles * 5, source, model + ": the far copy is under a fifth of the original");
            Assert.AreEqual(1, far.subMeshCount, model + " far copy: one material");
            var farMaterial = set.FarMaterial(model);
            Assert.AreEqual(TownModels.FarStem(id) + ".mat", AssetDatabase.GetAssetPath(farMaterial), model + " far material");
            Assert.AreEqual(set.Material(model).shader, farMaterial.shader, model + ": the far material keeps Tal's shader");
            Assert.IsTrue(farMaterial.enableInstancing, model + " far material instanced");
            Assert.AreEqual(TownModels.FarStem(id) + ".png", AssetDatabase.GetAssetPath(farMaterial.mainTexture), model + " far colour map");
            var record = JsonUtility.FromJson<FarRecord>(File.ReadAllText(TownModels.FarStem(id) + ".json"));
            Assert.AreEqual(fbx, record.source, model + " record source");
            Assert.AreEqual(CollectionCatalog.Sha256(fbx), record.source_sha256, model + ": the far copy was made from this FBX");
            Assert.AreEqual(jpg, record.colour_map, model + " record colour map");
            Assert.AreEqual(CollectionCatalog.Sha256(jpg), record.colour_map_sha256, model + ": and from this colour map");
            Assert.AreEqual(triangles, record.triangles, model + " record triangles");
            Assert.AreEqual(TownModels.FarTriangles, record.target_triangles, model + " record target");
            StringAssert.Contains("owner confirms Tal's permission, 3 Nov 2026 (#2378)", record.provenance, model.ToString());
            // The far bundle carries nothing of the original: its prefab reaches the far mesh, material and map only.
            string prefab = CollectionCatalog.PrefabPath(CollectionCatalog.FarCopy(id), CollectionDetail.Far);
            var deps = AssetDatabase.GetDependencies(prefab, true);
            CollectionAssert.Contains(deps, TownModels.FarStem(id) + ".asset", model + " far prefab mesh");
            CollectionAssert.Contains(deps, TownModels.FarStem(id) + ".png", model + " far prefab map");
            CollectionAssert.DoesNotContain(deps, fbx, model + ": the far prefab must not pull the original mesh");
            CollectionAssert.DoesNotContain(deps, jpg, model + ": the far prefab must not pull the original colour map");
            CollectionAssert.DoesNotContain(deps, $"{TownModels.Folder}/{id}/{id}.mat", model + ": nor the original material");
        }
    }

    /// <summary>Owner ruling on #700, "Models up close only": the switch picks by the camera's view distance with
    /// hysteresis (the originals from below 40 km, never above 45 km), on either side of the geometric middle of
    /// tel_aviv30 and gameplay03, the band wider than one wheel notch; the view distance is the camera rig's zoom. Beyond
    /// it the far band (#2378) admits the far-off copies out to 100 km.</summary>
    [Test]
    public void TheSwitchHasHysteresisBetweenTheCloseViewsAndGameplay()
    {
        Assert.IsTrue(TownModelLayer.CloseAt(39000f, null)); Assert.IsFalse(TownModelLayer.CloseAt(41000f, null));
        Assert.IsTrue(TownModelLayer.CloseAt(43000f, true), "zooming out from close: still models inside the band");
        Assert.IsFalse(TownModelLayer.CloseAt(43000f, false), "zooming in from far: still kits inside the band");
        Assert.IsFalse(TownModelLayer.CloseAt(46000f, true)); Assert.IsTrue(TownModelLayer.CloseAt(39000f, false));
        Assert.That(Mathf.Sqrt(30000f * 60000f), Is.InRange(TownModelLayer.CloseEnterM, TownModelLayer.CloseExitM), "the band holds the middle");
        Assert.Greater(TownModelLayer.CloseExitM / TownModelLayer.CloseEnterM - 1f, CameraController.ZoomPerStep, "wider than one notch");
        foreach (var pose in PromoPose.All) Assert.Greater(pose.Distance, TownModelLayer.CloseExitM, pose.Name + " draws no original");
        foreach (var pose in PromoPose.CloseUps) Assert.Less(pose.Distance, TownModelLayer.CloseEnterM, pose.Name + " draws the originals");
        // The far band (#2378): the far-off copies from the close switch out to 100 km, the same hysteresis shape, this
        // layer's alone (the seven other layers keep the 40/45 km switch untouched); it holds the 60 km gameplay view and
        // the 90 km opening, not diorama02, and ends under the strategic cap.
        Assert.IsTrue(TownModelLayer.FarAt(99_000f, null)); Assert.IsFalse(TownModelLayer.FarAt(101_000f, null));
        Assert.IsTrue(TownModelLayer.FarAt(105_000f, true), "zooming out: still the far copies inside the band");
        Assert.IsFalse(TownModelLayer.FarAt(105_000f, false), "zooming in: still the kits inside the band");
        Assert.IsFalse(TownModelLayer.FarAt(113_000f, true)); Assert.IsTrue(TownModelLayer.FarAt(99_000f, false));
        Assert.Greater(TownModelLayer.FarExitM / TownModelLayer.FarEnterM - 1f, CameraController.ZoomPerStep, "the far band is wider than one notch");
        Assert.Greater(TownModelLayer.FarEnterM, TownModelLayer.CloseExitM, "the far band begins where the close switch ends");
        Assert.LessOrEqual(TownModelLayer.FarExitM, OriginalModelDistance.EnterM, "under the strategic cap");
        Assert.Less(PromoPose.Gameplay03.Distance, TownModelLayer.FarEnterM, "gameplay03 draws the far copies");
        Assert.Less(90_000f, TownModelLayer.FarEnterM, "the 90 km opening draws the far copies");
        Assert.Greater(PromoPose.Diorama02.Distance, TownModelLayer.FarExitM, "diorama02 draws the kits");
        var go = new GameObject("cam"); var cam = go.AddComponent<Camera>(); var rig = go.AddComponent<CameraRig>();
        try
        {
            foreach (var d in new[] { 6000f, 30000f, 60000f, 250000f })
            {
                rig.LookAt = new Vector3(100000f, 0f, 200000f); rig.Distance = d; rig.Yaw = 10f; rig.Apply();
                Assert.AreEqual(d, TownModelLayer.ViewDistance(cam), d * 1e-4f, "the view distance is the rig's zoom");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    /// <summary>A kit mesh's close mesh is the mesh without its marked buildings, plus the plinths; the kit mesh itself keeps
    /// everything (it is main's, drawn far off).</summary>
    [Test]
    public void TheCloseMeshDropsTheBuildingsAndAddsThePlinths()
    {
        var km = new KitMesh();
        var grey = new Color32(200, 200, 200, 255);
        km.Box(0f, 0f, 0f, 10f, 10f, 0f, 0f, 30f, grey, grey, Facade.Plain, 1f);   // a landmark: kept
        var cut = km.CutStart();
        km.Box(50f, 0f, 0f, 10f, 10f, 0f, 0f, 10f, grey, grey, Facade.Plain, 1f);   // a building: left out
        km.Gable(50f, 0f, 0f, 10f, 10f, 10f, 3f, grey, grey);
        km.CutEnd(cut);
        km.Box(90f, 0f, 0f, 4f, 4f, 0f, 0f, 2f, grey, grey, Facade.Plain, 1f);   // a palm or wall after it: kept
        int keep = 0;
        { var only = new KitMesh(); only.Box(0f, 0f, 0f, 10f, 10f, 0f, 0f, 30f, grey, grey, Facade.Plain, 1f); only.Box(90f, 0f, 0f, 4f, 4f, 0f, 0f, 2f, grey, grey, Facade.Plain, 1f); keep = only.VertexCount; }
        km.Plinths.Box(50f, 0f, 0f, 10f, 10f, -2f, 0f, 0f, grey, grey, Facade.Plain, 1f, roofed: true);
        int plinth = km.Plinths.VertexCount;
        int all = km.VertexCount;
        var close = km.ToCloseMesh("close");
        Assert.IsNotNull(close);
        Assert.AreEqual(1, km.Cuts);
        Assert.Greater(plinth, 0); Assert.Greater(all, keep);
        Assert.AreEqual(keep + plinth, close!.vertexCount, "the landmark, the wall and the plinth; not the building");
        Assert.AreEqual(all, km.ToMesh("kit").vertexCount, "the kit mesh keeps everything");
        Assert.Less(close.bounds.max.y, 31f); Assert.Greater(close.bounds.max.x, 91f);
        UnityEngine.Object.DestroyImmediate(close);
    }

    /// <summary>Owner ruling on #700, "Models up close only", and #2378 (owner, 3 November 2026, "allow them far off only"):
    /// at the close views (tel_aviv30, jaffa12, petah_tikva30) the originals draw (well over a hundred), every kit
    /// individual-town mesh gives way to its close mesh, the major cities' included (mixed locality tiles keep fallback), and
    /// the models stay within the fixture's complete original-geometry work. The major cities (TownModels.IsMajorCity) draw
    /// Tal Heuberger's models below the close switch (owner, 7 November 2026, "Tal's models close (Recommended)") and keep
    /// their kits from 40 km out (owner, 6 November 2026, "Big blocks, every zoom", which "Tal's models close
    /// (Recommended)" supersedes below the switch). Within the far band (gameplay03 at 60 km, the 90 km opening) the
    /// towns with a whole-town model draw their far-off copies instead (owner, 3 November 2026, "Cities/towns only
    /// (Recommended)"), at most TownModels.FarTriangles each, so the 90 km opening stays under four million triangles; the
    /// 1948 localities keep their kits. At diorama02 (250 km) no model draws. Zooming back out restores the kits. The "kits"
    /// toggle stops the models too. The log line is the draw cost at each pose.</summary>
    [UnityTest]
    public IEnumerator TheKitsDrawFarAndTheModelsUpClose()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var root = map.transform.Find(KitLayer.RootName);
        var layer = root.GetComponentInChildren<TownModelLayer>(); Assert.IsNotNull(layer);
        // TEST ONLY: this complete-geometry fixture prices/draws all original towns; it is not a shipping policy.
        const System.Reflection.BindingFlags fixtureFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var places = (IReadOnlyList<(string id, Bounds bounds)>)typeof(TownModelLayer).GetField("places", fixtureFlags)!.GetValue(layer)!;
        Assert.IsNotEmpty(places);
        float coverage = Mathf.Max(1f, places.Max(p => p.bounds.max.x) - places.Min(p => p.bounds.min.x)
            + places.Max(p => p.bounds.max.z) - places.Min(p => p.bounds.min.z));
        typeof(TownModelLayer).GetField("EdgeAllowance", fixtureFlags)!.SetValue(layer,
            new Func<IReadOnlyList<string>, string, bool>((_, __) => true));
        typeof(TownModelLayer).GetMethod("ConfigureResidency", fixtureFlags)!.Invoke(layer,
            new object[] { coverage, coverage, coverage, places.Count, "TEST ONLY full geometry; NOT accepted residency limits" });
        var identity = (IDictionary<Renderer, string>)typeof(TownModelLayer).GetField("swapIds", fixtureFlags)!.GetValue(layer)!;
        Assert.IsTrue(layer.HasModels, "this editor geometry fixture requires the complete original model set");
        Assert.Greater(layer.Count, 1000);
        int Plots(string id) => layer.InstancesOf(id).Select(m => m.Plot).Distinct().Count();
        Assert.Greater(Plots("tel_aviv"), Plots("petah_tikva"), "the big city has more building plots");
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        var closeRs = renderers.Where(r => r.name.EndsWith(KitLayer.CloseSuffix)).ToList();
        var kitRs = map.Settlements.Select(s => root.Find(s.Id).GetComponent<MeshRenderer>())
            .Concat(renderers.Where(r => r.name.StartsWith("Localities ") && !r.name.EndsWith(KitLayer.CloseSuffix))).ToList();
        Assert.Greater(closeRs.Count, 20, "close meshes"); Assert.Greater(kitRs.Count, 40, "kit meshes");
        Assert.IsNotNull(root.Find("tel_aviv" + KitLayer.CloseSuffix), "Tel Aviv's close mesh is still built (a major city: it never swaps in)");
        var originals = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
        // Full-original geometry has no decimated-mesh triangle cap. Bound submitted WORK instead:
        // rendering more than every fixture instance once is a duplicate-submission regression.
        long maxGeometryWork = layer.Instances().Sum(i => (long)originals.meshes[(int)i.model].triangles.Length / 3);
        var results = new List<string>();
        var big = map.Settlements.Where(s => TownModels.IsMajorCity(s.Id)).Select(s => s.Id).ToList();
        CollectionAssert.AreEquivalent(TownLandmarksTests.BigCities, big, "the four major cities");
        int bigInstances = big.Sum(id => layer.CountFor(id));
        // The places that draw the far-off copies beyond the close switch (KitLayer): every settlement with a whole-town
        // model, i.e. the moshavot and towns, the kibbutzim, Nahalal and the Arab towns (Q15 option a). Not the four major
        // cities, which keep their kits from 40 km out (owner, 6 November 2026, "Big blocks, every zoom").
        var farTier = map.Settlements.Where(s => Statecraft.Game.Map.Collection.TownLandmarks.ModelFor(s) != null)
            .Select(s => s.Id).ToHashSet();
        CollectionAssert.IsEmpty(farTier.Intersect(big).ToList(), "the four major cities take no far-off copies");
        CollectionAssert.Contains(farTier, "petah_tikva", "a moshava draws its far-off copies");
        // The far tier pinned to the content, not to the condition above: the 21 game settlements of
        // content/map/settlements_1948.yaml that are not major cities, written out by hand (the six moshavot, Tiberias, Safed,
        // Netanya and Ramla; the five kibbutzim and Nahalal; Acre, Nazareth, Lod, Gaza and Beersheba). KitLayer's own list is
        // checked, so a broken condition there cannot pass by agreeing with this test's copy of it.
        string[] expectedFarTier =
        {
            "acre", "beersheba", "degania_alef", "ein_harod", "gaza", "hadera", "hanita", "lod",
            "nahalal", "nazareth", "netanya", "petah_tikva", "ramla", "rehovot", "revivim", "rishon_lezion", "rosh_pinna",
            "safed", "tiberias", "yad_mordechai", "zikhron_yaakov",
        };
        Assert.AreEqual(21, expectedFarTier.Distinct().Count(), "the expected far tier names 21 settlements");
        var productionFarTier = (HashSet<string>?)typeof(TownModelLayer).GetField("farTowns", fixtureFlags)!.GetValue(layer);
        Assert.IsNotNull(productionFarTier, "KitLayer passes the far tier to the layer");
        CollectionAssert.AreEquivalent(expectedFarTier, productionFarTier!, "KitLayer's far tier: exactly the 21 game settlements, no major city, no locality");
        CollectionAssert.Contains(productionFarTier!, "degania_alef", "a kibbutz (a whole-town row by type) draws its far-off copies");
        CollectionAssert.AreEquivalent(expectedFarTier, farTier, "this test's far tier matches the content");
        int farInstances = farTier.Sum(id => layer.CountFor(id));
        void Check(PromoPose pose, bool close, string? floorTown = null)
        {
            // Up close every detailed town, the major cities included; within the far band the far-tier places (never a major
            // city), beyond it none, and in this fixture only within its focus admission (OriginalModelDistance.EnterM, 120 km:
            // gameplay03 at 60 km and the 90 km opening, not diorama02 at 250 km). This fixture has no view admission;
            // TheFourCitiesKeepTheirKitsFarAndDrawTalsModelsClose checks production's at 250 km.
            bool farFocus = pose.Distance < Statecraft.Game.Map.Collection.OriginalModelDistance.EnterM;
            bool farBand = TownModelLayer.FarAt(pose.Distance, null);
            bool Ready(string town) => layer.CountFor(town) > 0 && (close || (farFocus && farBand && !big.Contains(town) && farTier.Contains(town)));
            foreach (var s in map.Settlements)
                Assert.AreEqual(Ready(s.Id), layer.ReadyForTown(s.Id, cam), pose.Name + ": " + s.Id + (Ready(s.Id) ? " draws its building models" : " has no building model"));
            Assert.AreEqual(close, layer.LastClose, pose.Name);
            foreach (var renderer in kitRs)
            {
                bool ready = identity.TryGetValue(renderer, out var town) && Ready(town);
                Assert.AreEqual(ready, renderer.forceRenderingOff, pose.Name + ": " + renderer.name + " fallback");
            }
            foreach (var renderer in closeRs)
            {
                var kitName = renderer.name.Substring(0, renderer.name.Length - KitLayer.CloseSuffix.Length);
                var kit = root.Find(kitName).GetComponent<MeshRenderer>();
                bool ready = identity.TryGetValue(kit, out var town) && Ready(town);
                Assert.AreEqual(!ready, renderer.forceRenderingOff, pose.Name + ": " + renderer.name + " close mesh");
            }
            Assert.IsTrue(kitRs.Any(r => !identity.ContainsKey(r)), "mixed locality tiles stay fallback at every zoom");
            results.Add($"{pose.Name}: close={close} {layer.LastDrawn} models, {layer.LastTriangles} triangles, {layer.LastDraws} draws");
            if (!close)
            {
                // Beyond the close switch only the far-off copies: never more instances than the far-tier places hold, and
                // never more than TownModels.FarTriangles each; some at gameplay03 and at the 90 km opening, none at
                // diorama02 (outside this fixture's focus admission).
                Assert.LessOrEqual(layer.LastDrawn, farBand ? farInstances : 0, pose.Name + ": no other town's building model");
                Assert.LessOrEqual(layer.LastTriangles, (long)layer.LastDrawn * TownModels.FarTriangles, pose.Name + ": the far-off copies, not the originals");
                if (farFocus) Assert.Greater(layer.LastDrawn, 0, pose.Name + ": the buildings in view");
                else Assert.AreEqual(0, layer.LastDrawn, pose.Name + ": nothing outside the fixture's focus admission");
                return;
            }
            // Up close: well over a hundred models, and over floorTown at least all of its instances.
            Assert.Greater(layer.LastDrawn, 100, pose.Name + ": models drawn");
            if (floorTown != null)
            {
                Assert.Greater(layer.CountFor(floorTown), 0, floorTown + " has building models");
                Assert.GreaterOrEqual(layer.LastDrawn, layer.CountFor(floorTown), pose.Name + ": " + floorTown + "'s models drawn");
            }
            Assert.LessOrEqual(layer.LastTriangles, maxGeometryWork, pose.Name + ": no more than all original instances once");
        }
        foreach (var pose in PromoPose.All) { pose.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); layer.PrepareCamera(cam); Check(pose, false); }
        foreach (var pose in PromoPose.CloseUps) { pose.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); layer.PrepareCamera(cam); Check(pose, true); }
        // Petah Tikva at 30 km (its settlements_1948.yaml point), framed as tel_aviv30: a close view over a town that draws
        // its building models.
        var petah30 = new PromoPose("petah_tikva30", 34.8864, 32.0889, 30000f, 10f);
        petah30.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); layer.PrepareCamera(cam); Check(petah30, true, "petah_tikva");
        PromoPose.Gameplay03.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); layer.PrepareCamera(cam); Check(PromoPose.Gameplay03, false);
        // The opening camera of today (CameraRig.Distance, 90 km, over 34.95E 31.95N): the far-off copies' budget there.
        var start90 = new PromoPose("start90", 34.95, 31.95, 90_000f, 0f);
        start90.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); layer.PrepareCamera(cam); Check(start90, false);
        Assert.GreaterOrEqual(layer.LastDrawn, layer.CountFor("lod"), "start90: the far copies of Lod, under the opening's centre, and more");
        Assert.LessOrEqual(layer.LastTriangles, 4_000_000L, "start90: the far tier's budget at the 90 km opening");
        TestContext.WriteLine($"{layer.Count} models ({bigInstances} in the four major cities, drawn below the close switch: " +
            string.Join(", ", big.Select(id => $"{id} {layer.CountFor(id)}")) + $"; {farInstances} in the {farTier.Count} far-tier game settlements); " + string.Join("; ", results));
        PromoPose.CloseUps[0].Apply(rig, map.Frame);
        VisualToggles.Set("kits", false); RenderSample.Render(cam, 320, 180);
        Assert.IsFalse(layer.Draw, "the kits toggle hides the models");
        Assert.AreEqual(0, layer.LastDrawn, "no model drawn with the kits off");
    }

}
