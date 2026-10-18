#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using M = Statecraft.Game.Map.Kits.TownModel;

/// <summary>Issue #659 step 2: the towns are built from MATKOT's building models, by town type (the
/// issue's table), one model per placed building, fitted to the kit's footprints (the layout and census unchanged), loaded
/// with the map through the scene (never through a Resources folder), drawn instanced with baked levels of detail.</summary>
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
        return map;
    }

    static SettlementKits.Result Build(MapScene map, out List<SettlementKits.Placed> placed)
    {
        var g = new KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        foreach (var c in map.RouteCorridors) g.AddCorridor(c.line, c.halfWidth);
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
            case "jerusalem": return new HashSet<M> { M.B03aArches, M.B03bExternalStair, M.B03cPlain, M.B17CourtyardHouse };
            case "netanya": return new HashSet<M> { M.B13aTwoStorey, M.B28WaterTower };
        }
        switch (s.Type)
        {
            case "kibbutz": return new HashSet<M> { M.B07DiningHall, M.B09aCommunal, M.B24Barn, M.B28WaterTower };   // #820: sheds are barns
            case "moshav": return new HashSet<M>(moshava);
            case "town" when s.Id != "tiberias" && s.Id != "safed":
                var h = new HashSet<M>(moshava) { M.B13aTwoStorey };
                if (TownModels.WineMoshava(s.Id)) h.Add(M.B23Winery);
                return h;
            default: return new HashSet<M> { M.B17CourtyardHouse };   // the old towns and the Arab towns
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
            // A Negev lookout's yard (#825) has no model that fits a 1943 walled outpost: it keeps its kit up close.
            if (outposts.Contains(s.Id)) { if (used.Count > 0) bad.Add(s.Id + ": a lookout with models"); continue; }
            if (used.Count == 0) bad.Add(s.Id + " has no models");
            var wrong = used.Where(u => !Allowed(s).Contains(u)).Distinct().ToList();
            if (wrong.Count > 0) bad.Add($"{s.Id} ({s.Type}) uses {string.Join(", ", wrong)}");
            // The b07 dining hall stands on the kibbutz's dining hall (#820's landmark), one each, and nowhere else.
            var halls = placed.Where(p => p.Id == s.Id && p.Kind == "dining_hall").ToList();
            var b07 = built.Models.Where(m => m.Id == s.Id && m.Model == M.B07DiningHall).ToList();
            if (b07.Count != halls.Count) bad.Add($"{s.Id}: {b07.Count} b07 models for {halls.Count} dining halls");
            else if (halls.Count == 1 && (b07[0].C - halls[0].C).magnitude > 1f) bad.Add($"{s.Id}: the b07 model is not on the dining hall");
            if (TownModels.WineMoshava(s.Id) && used.Count(u => u == M.B23Winery) != 1) bad.Add(s.Id + ": not one winery");
        }
        // The cities' own models are there (not only a fallback).
        foreach (var (id, model) in new[] { ("tel_aviv", M.B01WhiteCity), ("tel_aviv", M.B02bNarrowBlock), ("haifa", M.B04aStoneBalconies),
                     ("haifa", M.B32CargoShed), ("jerusalem", M.B03cPlain), ("jaffa", M.B17CourtyardHouse), ("netanya", M.B13aTwoStorey) })
            if (!built.Models.Any(m => m.Id == id && m.Model == model)) bad.Add($"{id} has no {model}");
        // A 1948 Arab village is built of courtyard houses.
        var villages = map.Localities.Select(p => p.Settlement).Where(s => s.Type == "village" && !Localities.IsMixed(s.Id)).Select(s => s.Id).ToHashSet();
        var villageModels = built.Models.Where(m => villages.Contains(m.Id) && m.Model != M.B28WaterTower).Select(m => m.Model).Distinct().ToList();
        CollectionAssert.AreEqual(new[] { M.B17CourtyardHouse }, villageModels, "the villages' houses");
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
    /// hall and a place's one-off building one), each turned with the plot and inside its footprint; no other model is
    /// placed.</summary>
    [Test]
    public void EveryPlacedBuildingIsOneModelOnItsPlot()
    {
        var map = OpenMap();
        var built = Build(map, out var placed);
        var outposts = Outposts(placed);   // a Negev lookout keeps its kit up close (#825)
        var towns = map.Settlements.Select(s => s.Id).Where(id => !outposts.Contains(id)).ToHashSet();
        var want = placed.Where(p => towns.Contains(p.Id)
            && (p.Kind == "building" || p.Kind == "water_tower" || p.Kind == "warehouse" || p.Kind == "dining_hall")).ToList();
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
                if (Mathf.Abs(mi.Yaw - p.Yaw) > 0.01f) bad.Add($"{p.Id} {p.Kind} at {p.C}: turned off its plot");
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
    /// places is a model on its plot. What keeps the kit up close is a landmark no model fits (the list below), Nahalal's
    /// public building (#851: no model of a moshav's school or offices) and Revivim's walled lookout (#825: no model of a
    /// 1943 walled outpost); those two are the gaps the PR lists. A new kind of building in these towns fails here until it
    /// is given a model or named as a gap.</summary>
    [Test]
    public void EveryTownWithAWholeTownModelIsTheBuildingModelsUpClose()
    {
        var map = OpenMap();
        var built = Build(map, out var placed);
        var mapped = map.Settlements.Where(s => Statecraft.Game.Map.Collection.TownLandmarks.ModelFor(s) != null).ToList();
        TestContext.WriteLine($"{mapped.Count} towns with a whole-town model: {string.Join(", ", mapped.Select(s => s.Id))}");
        var ids = mapped.Select(s => s.Id).ToHashSet();
        CollectionAssert.IsSubsetOf(new[] { "tel_aviv", "jerusalem", "haifa", "rishon_lezion", "degania_alef", "nahalal", "nazareth" }, ids);
        Assert.AreEqual(new[] { "arab_town", "city", "kibbutz", "moshav", "town" }, mapped.Select(s => s.Type).Distinct().OrderBy(t => t).ToArray());
        var modelled = new HashSet<string> { "building", "dining_hall", "water_tower", "warehouse" };
        var landmarks = new HashSet<string>
        {
            "al_aqsa", "breakwater", "church", "citadel", "clock_tower", "dome_of_the_rock", "gate", "german_colony", "haram",
            "lighthouse", "mosque", "palm", "port", "village_mosque", "walls", "white_tower",
        };
        var outposts = Outposts(placed);
        CollectionAssert.AreEqual(new[] { "revivim" }, outposts.Where(ids.Contains).ToArray(), "the one walled lookout (a listed gap)");
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
            else if (!landmarks.Contains(p.Kind) && !(p.Id == "nahalal" && p.Kind == "public_building")) bad.Add($"{p.Id}: {p.Kind} keeps the kit up close");
        }
        TestContext.WriteLine($"{houses} buildings in those towns, all models: {bad.Count == 0}");
        Assert.Greater(houses, 100);
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} kit buildings left up close");
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
    public void TheMapSceneLoadsTheModelsAndNoResourcesFolderDoes()
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
        Assert.Contains(TownModels.SetPath, deps, "the map scene loads the town models");
        foreach (var id in TownModels.Ids) Assert.Contains($"{TownModels.Folder}/{id}/{id}.fbx", deps, id);
        Assert.IsFalse(TownModels.SetPath.Split('/').Any(x => x.Equals("Resources", StringComparison.OrdinalIgnoreCase)));
        var inResources = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)
                && p.Split('/').Any(x => x.Equals("Resources", StringComparison.OrdinalIgnoreCase))).ToArray();
        Assert.IsNotEmpty(inResources);
        CollectionAssert.DoesNotContain(AssetDatabase.GetDependencies(inResources, true), TownModels.SetPath, "the list is reached from a Resources folder");
    }

    /// <summary>The fail-open check (#700): a model list that is missing, or lacks a model or a level, never leaves a town's
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
            c.mid = (Mesh[])full.mid.Clone(); c.far = (Mesh[])full.far.Clone(); c.farthest = (Mesh[])full.farthest.Clone();
            return c;
        }
        var probe = Clone();
        try
        {
            Assert.IsNull(probe.Missing(), "the full list");
            probe.far[(int)M.B17CourtyardHouse] = null!;
            StringAssert.Contains("B17CourtyardHouse has no far level", probe.Missing());
            Assert.Throws<System.IO.InvalidDataException>(() => probe.Validate(), "the bake refuses a missing level");
            probe.far[(int)M.B17CourtyardHouse] = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath).far[(int)M.B17CourtyardHouse];
            probe.meshes[(int)M.B17CourtyardHouse] = null!;
            StringAssert.Contains("B17CourtyardHouse has no mesh", probe.Missing());
            var empty = ScriptableObject.CreateInstance<TownModelSet>();
            StringAssert.Contains("want 18", empty.Missing());
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

    /// <summary>Each model has three lighter levels of detail baked into the model list (separate meshes: a WebGL build
    /// ignored RenderParams.forceMeshLod and drew every building's full mesh): about 800, about 200 and the lightest that
    /// keeps the model's size, standing on the model's base, none heavier than the one before,
    /// in the model's own mesh space and within a fifth of its size (so one fitted matrix places every level), without a
    /// CPU copy; the far views draw the light ones (LodFor, TownModelSet.Mesh).</summary>
    [Test]
    public void EveryModelHasBakedLevelsAndTheFarViewDrawsALightOne()
    {
        var set = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
        foreach (M model in Enum.GetValues(typeof(M)))
        {
            var full = set.Mesh(model); var levels = new[] { set.mid[(int)model], set.far[(int)model], set.farthest[(int)model] };
            long T(Mesh m) => m.GetIndexCount(0) / 3;
            TestContext.WriteLine($"{model}: {T(full)} / {T(levels[0])} / {T(levels[1])} / {T(levels[2])} triangles");
            Assert.That(T(levels[0]), Is.InRange(400, 1600), model + " mid");
            // The far levels are the lightest generated levels that keep the model's size (TownModelAssets.KeepsShape; the
            // base is put back on the model's by the bake): about 200 triangles or fewer for most, never more than the mid
            // target (b03a's lightest is 476, b03c's 768).
            Assert.LessOrEqual(T(levels[1]), TownModelSet.MidTriangles, model + " far");
            Assert.LessOrEqual(T(levels[2]), TownModelSet.MidTriangles, model + " farthest");
            Assert.Greater(T(full), T(levels[0])); Assert.GreaterOrEqual(T(levels[0]), T(levels[1])); Assert.GreaterOrEqual(T(levels[1]), T(levels[2]));
            foreach (var m in levels)
            {
                Assert.AreEqual(TownModels.SetPath, AssetDatabase.GetAssetPath(m), m.name + " lives in the model list");
                Assert.IsFalse(m.isReadable, m.name + " keeps no CPU copy");
                Assert.AreEqual(full.bounds.min.z, m.bounds.min.z, 0.02f, m.name + " stands on the model's base");
                Assert.AreEqual(full.bounds.size.y, m.bounds.size.y, 0.2f, m.name + " depth");
                // A light level may lose a roof's small top (b01's far mesh is 15% lower): within a fifth of the model.
                Assert.AreEqual(full.bounds.size.x, m.bounds.size.x, 0.2f, m.name + " width"); Assert.AreEqual(full.bounds.size.z, m.bounds.size.z, 0.2f, m.name + " height");
                Assert.AreEqual(full.bounds.center.x, m.bounds.center.x, 0.05f, m.name + " centre");
            }
            Assert.AreSame(full, set.Mesh(model, 0)); Assert.AreSame(levels[0], set.Mesh(model, 3)); Assert.AreSame(levels[1], set.Mesh(model, 5));
            Assert.AreSame(levels[2], set.Mesh(model, 6));
        }
        // A 840 m block (a Tel Aviv block at 60x), 1080 px tall, the camera's 30 degrees: at tel_aviv30 (30 km) about 56 px,
        // the mid mesh; at the 6 km closest zoom about 280 px, the full mesh.
        int L = TownModelSet.Levels + 1;
        Assert.AreEqual(3, TownModelLayer.LodFor(840f, 30000f, 30f, 1080f, L), "tel_aviv30: the mid mesh");
        Assert.AreEqual(2, TownModelLayer.LodFor(460f, 12000f, 30f, 1080f, L), "jaffa12, a village house: the mid mesh");
        Assert.AreEqual(0, TownModelLayer.LodFor(840f, CameraController.MinDistance, 30f, 1080f, L), "the closest zoom: the full mesh");
        Assert.AreSame(set.mid[0], set.Mesh((M)0, 2)); Assert.AreSame(set.mid[0], set.Mesh((M)0, 3));
    }

    /// <summary>Owner ruling on #700, "Models up close only": the switch picks by the camera's view distance with
    /// hysteresis (models from below 40 km, kits again from above 45 km), on either side of the geometric middle of
    /// tel_aviv30 and gameplay03, the band wider than one wheel notch; the view distance is the camera rig's zoom.</summary>
    [Test]
    public void TheSwitchHasHysteresisBetweenTheCloseViewsAndGameplay()
    {
        Assert.IsTrue(TownModelLayer.CloseAt(39000f, null)); Assert.IsFalse(TownModelLayer.CloseAt(41000f, null));
        Assert.IsTrue(TownModelLayer.CloseAt(43000f, true), "zooming out from close: still models inside the band");
        Assert.IsFalse(TownModelLayer.CloseAt(43000f, false), "zooming in from far: still kits inside the band");
        Assert.IsFalse(TownModelLayer.CloseAt(46000f, true)); Assert.IsTrue(TownModelLayer.CloseAt(39000f, false));
        Assert.That(Mathf.Sqrt(30000f * 60000f), Is.InRange(TownModelLayer.CloseEnterM, TownModelLayer.CloseExitM), "the band holds the middle");
        Assert.Greater(TownModelLayer.CloseExitM / TownModelLayer.CloseEnterM - 1f, CameraController.ZoomPerStep, "wider than one notch");
        foreach (var pose in PromoPose.All) Assert.Greater(pose.Distance, TownModelLayer.CloseExitM, pose.Name + " draws the kits");
        foreach (var pose in PromoPose.CloseUps) Assert.Less(pose.Distance, TownModelLayer.CloseEnterM, pose.Name + " draws the models");
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

    /// <summary>Owner ruling on #700, "Models up close only": at gameplay03 and diorama02 every kit mesh draws (main's
    /// towns) and no model does; at the close views (tel_aviv30, jaffa12) the models draw (well over a hundred), every kit
    /// mesh gives way to its close mesh, and the models stay within <see cref="MaxTriangles"/>; zooming back out restores
    /// the kits. The "kits" toggle stops the models too.</summary>
    [UnityTest]
    public IEnumerator TheKitsDrawFarAndTheModelsUpClose()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var root = map.transform.Find(KitLayer.RootName);
        var layer = root.GetComponentInChildren<TownModelLayer>(); Assert.IsNotNull(layer);
        Assert.Greater(layer.Count, 1000);
        int Plots(string id) => layer.InstancesOf(id).Select(m => m.Plot).Distinct().Count();
        Assert.Greater(Plots("tel_aviv"), Plots("petah_tikva"), "the big city has more building plots");
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        var closeRs = renderers.Where(r => r.name.EndsWith(KitLayer.CloseSuffix)).ToList();
        var kitRs = map.Settlements.Select(s => root.Find(s.Id).GetComponent<MeshRenderer>())
            .Concat(renderers.Where(r => r.name.StartsWith("Localities ") && !r.name.EndsWith(KitLayer.CloseSuffix))).ToList();
        Assert.Greater(closeRs.Count, 20, "close meshes"); Assert.Greater(kitRs.Count, 40, "kit meshes");
        Assert.IsNotNull(root.Find("tel_aviv" + KitLayer.CloseSuffix), "Tel Aviv keeps its landmarks and palms up close");
        var results = new List<string>();
        void Check(PromoPose pose, bool close)
        {
            Assert.AreEqual(close, layer.LastClose, pose.Name);
            Assert.IsTrue(kitRs.All(r => r.forceRenderingOff == close), pose.Name + ": the kit meshes");
            Assert.IsTrue(closeRs.All(r => r.forceRenderingOff == !close), pose.Name + ": the close meshes");
            results.Add($"{pose.Name}: close={close} {layer.LastDrawn} models, {layer.LastTriangles} triangles, {layer.LastDraws} draws");
            if (!close) { Assert.AreEqual(0, layer.LastDrawn, pose.Name + ": no model"); return; }
            Assert.Greater(layer.LastDrawn, 100, pose.Name + ": models drawn");
            Assert.LessOrEqual(layer.LastTriangles, MaxTriangles, pose.Name);
        }
        foreach (var pose in PromoPose.All) { pose.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); Check(pose, false); }
        foreach (var pose in PromoPose.CloseUps) { pose.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); Check(pose, true); }
        PromoPose.Gameplay03.Apply(rig, map.Frame); RenderSample.Render(cam, 1920, 1080); Check(PromoPose.Gameplay03, false);
        TestContext.WriteLine($"{layer.Count} models; " + string.Join("; ", results));
        PromoPose.CloseUps[0].Apply(rig, map.Frame);
        VisualToggles.Set("kits", false); RenderSample.Render(cam, 320, 180);
        Assert.IsFalse(layer.Draw, "the kits toggle hides the models");
        Assert.AreEqual(0, layer.LastDrawn, "no model drawn with the kits off");
    }

    /// <summary>The town models' triangle budget per frame at the close views (the shadow pass draws them again).</summary>
    public const long MaxTriangles = 1_500_000;
}
