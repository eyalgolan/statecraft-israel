#nullable enable
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

/// <summary>The art spike's procedural settlement kits and trees (issues #31 #32 #34 #37): seeded, sized by population, on
/// land, drawn for WebGL 2, and with shadows that start at the walls.</summary>
public class KitTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShadowFit.Restore();
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

    [Test]
    public void TheSeedIsTheKeyNotTheClock()
    {
        var a = new KitRandom("tel_aviv", 1); var b = new KitRandom("tel_aviv", 1); var c = new KitRandom("jaffa", 1);
        var sa = Enumerable.Range(0, 64).Select(_ => a.Next()).ToArray();
        CollectionAssert.AreEqual(sa, Enumerable.Range(0, 64).Select(_ => b.Next()).ToArray());
        CollectionAssert.AreNotEqual(sa, Enumerable.Range(0, 64).Select(_ => c.Next()).ToArray());
        var r = new KitRandom("x");
        for (int i = 0; i < 1000; i++) { var v = r.Value(); Assert.That(v, Is.InRange(0f, 0.99999994f)); }
    }

    [Test]
    public void EveryBuildDrawsTheSameTownsAndTrees()
    {
        var map = OpenMap();
        var list = map.Settlements;
        var first = SettlementKits.Build(list, Ground(map)); var second = SettlementKits.Build(list, Ground(map));
        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].s.Id, second[i].s.Id);
            Assert.AreEqual(first[i].buildings, second[i].buildings, first[i].s.Id);
            Assert.AreEqual(first[i].mesh.vertexCount, second[i].mesh.vertexCount, first[i].s.Id);
            Assert.AreEqual(first[i].mesh.bounds, second[i].mesh.bounds, first[i].s.Id);
        }
        foreach (var k in first.Concat(second)) Object.DestroyImmediate(k.mesh);
        var t1 = Vegetation.Plant(list, Ground(map)); var t2 = Vegetation.Plant(list, Ground(map));
        Assert.AreEqual(t1.Count, t2.Count);
        for (int i = 0; i < t1.Count; i++) { Assert.AreEqual(t1[i].kind, t2[i].kind); Assert.AreEqual(t1[i].m, t2[i].m); }
        TestContext.WriteLine(map.KitStats);
    }

    [Test]
    public void TownsSpreadInProportionToTheirPeople()
    {
        var map = OpenMap();
        float hex = map.Hexes.hex_size_km * 1000f;
        var byId = map.Settlements.ToDictionary(s => s.Id);
        float R(string id) => SettlementKits.FootprintRadiusM(byId[id], hex);
        float hexArea = 1.5f * Mathf.Sqrt(3f) * hex * hex;
        // Area in hexes = people / PeoplePerHex (Tel Aviv 166,660 in 1945: about 2.8 hexes), floored for the smallest places.
        Assert.AreEqual(166660f / SettlementKits.PeoplePerHex, Mathf.PI * R("tel_aviv") * R("tel_aviv") / hexArea, 0.01f);
        Assert.AreEqual(94310f / 166660f, (R("jaffa") * R("jaffa")) / (R("tel_aviv") * R("tel_aviv")), 0.001f, "area ratio = population ratio");
        Assert.Greater(R("jaffa"), R("lod")); Assert.Greater(R("lod"), R("rishon_lezion"));
        Assert.AreEqual(R("hanita"), R("revivim"), 1e-3f, "the floor");
        // The kits root: one merged mesh per settlement with Kit.mat, and more buildings in bigger towns.
        var root = map.transform.Find(KitLayer.RootName); Assert.IsNotNull(root);
        var kit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        foreach (var s in map.Settlements)
        {
            var t = root.Find(s.Id); Assert.IsNotNull(t, s.Id);
            var r = t.GetComponent<MeshRenderer>(); Assert.AreSame(kit, r.sharedMaterial, s.Id);
            Assert.Greater(t.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0, s.Id);
            Assert.IsFalse(map.transform.Find("Settlements").Find(s.Id).GetComponent<MeshRenderer>().enabled, s.Id + ": the marker box is hidden");
        }
        int V(string id) => root.Find(id).GetComponent<MeshFilter>().sharedMesh.vertexCount;
        TestContext.WriteLine($"vertices tel_aviv {V("tel_aviv")} petah_tikva {V("petah_tikva")} jaffa {V("jaffa")} lod {V("lod")} hanita {V("hanita")}");
        Assert.Greater(V("tel_aviv"), V("petah_tikva")); Assert.Greater(V("jaffa"), V("lod")); 
        // Petah Tikva (17,250) and Hanita (240) both draw TargetCount's floor of six houses at the 100x house scale plus their
        // water towers (both kept: TheVillagesAreSeededAndTheGameTownsKeepTheirLandmarks), so their vertex counts can be equal;
        // a moshava's size shows in its footprint (R above).
        Assert.GreaterOrEqual(V("petah_tikva"), V("hanita"));
        TestContext.WriteLine(map.KitStats);
    }

    /// <summary>Review N1: with the kits on, a map without its population file refuses to build in the editor (a player
    /// would warn and give every town the smallest footprint).</summary>
    [Test]
    public void AMissingPopulationFileIsAnErrorInTheEditor()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        var localities = inputs.LocalitiesTsv;
        inputs.LocalitiesTsv = "";
        StringAssert.Contains(Localities.RuntimeFileName, Assert.Throws<System.IO.InvalidDataException>(() => map.Build(inputs)).Message);
        inputs.LocalitiesTsv = localities;
        inputs.PopulationYaml = "";
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => map.Build(inputs));
        StringAssert.Contains(KitPopulation.FileName, ex.Message);
        map.ConfigureKits(null, null);   // without the kits the file is not needed (the scene is not saved)
        Assert.DoesNotThrow(() => map.Build(inputs));
    }

    /// <summary>Review N2: no two towns' lots overlap, landmarks (mosques, water towers, Jaffa's clock tower and church)
    /// included: a landmark looks for a free spot near its town's heart and is left out when there is none.</summary>
    [Test]
    public void NoTownsBuildingsOrLandmarksOverlapAnotherTowns()
    {
        var map = OpenMap();
        var built = SettlementKits.Build(map.Settlements, Ground(map), out var lots);
        int overlaps = 0;
        for (int i = 0; i < lots.Count; i++)
            for (int j = i + 1; j < lots.Count; j++)
                if (lots[i].id != lots[j].id && (lots[i].centre - lots[j].centre).magnitude < lots[i].radius + lots[j].radius - 1f) overlaps++;
        foreach (var k in built) Object.DestroyImmediate(k.mesh);
        TestContext.WriteLine($"{lots.Count} lots over {lots.Select(l => l.id).Distinct().Count()} towns");
        Assert.AreEqual(0, overlaps, "lots of different towns overlap");
        Assert.AreEqual(map.Settlements.Count, lots.Select(l => l.id).Distinct().Count(), "every town claimed ground");
    }

    /// <summary>Integration bug B9: palms grew out of Tel Aviv's and Jaffa's roofs and grove rows ran through blocks. No tree
    /// instance stands inside, or reaches with its crown into, any building's or landmark's lot.</summary>
    [Test]
    public void NoTreeGrowsThroughABuildingOrLandmark()
    {
        var map = OpenMap();
        var g = Ground(map);
        var built = SettlementKits.Build(map.Settlements, g, out var lots);
        foreach (var k in built) Object.DestroyImmediate(k.mesh);
        foreach (var l in lots) g.Lots.Add((l.centre, l.radius));
        var trees = Vegetation.Plant(map.Settlements, g);
        int inside = 0, touching = 0;
        foreach (var (kind, m) in trees)
        {
            Vector3 p = m.GetColumn(3); float scale = ((Vector3)m.GetColumn(0)).magnitude;
            foreach (var l in lots)
            {
                float d = (new Vector2(p.x, p.z) - l.centre).magnitude;
                if (d < l.radius) inside++;
                else if (d < l.radius + Vegetation.CrownRadiusM(kind) * scale) touching++;
            }
        }
        TestContext.WriteLine($"{trees.Count} trees against {lots.Count} lots: {inside} inside a lot, {touching} crowns reaching one");
        Assert.AreEqual(0, inside, "trees standing inside a building's or landmark's lot");
        Assert.AreEqual(0, touching, "crowns reaching into a lot");
        Assert.Greater(trees.Count, 3000, "the groves and hills still have their trees");
    }

    static string LocalitiesYaml =>
        System.IO.File.ReadAllText(System.IO.Path.Combine(Statecraft.Game.Paths.Content, "map", Localities.FileName));

    static Localities.Doc LoadLocalities(MapScene map)
    {
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        var b = HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta;
        var doc = Localities.Parse(LocalitiesYaml, (b.lon0, b.lon1, b.lat0, b.lat1), map.Settlements.Select(s => s.Id).ToList());
        Localities.FillHexes(doc, Subdistricts1948.Parse(inputs.RegionsYaml), map.Hexes);
        // The map build's light reader of the sub-district file (boot budget) gives the same areas as the full parse.
        var light = Localities.Parse(LocalitiesYaml, (b.lon0, b.lon1, b.lat0, b.lat1), map.Settlements.Select(s => s.Id).ToList());
        Localities.FillHexes(light, inputs.RegionsYaml, map.Hexes);
        for (int i = 0; i < doc.Encampments.Count; i++)
            CollectionAssert.AreEqual(doc.Encampments[i].Hexes, light.Encampments[i].Hexes, doc.Encampments[i].Subdistrict);
        Assert.IsTrue(doc.Encampments.All(a => a.Hexes.Count > 0));
        return doc;
    }

    /// <summary>The railways' and roads' corridors as MapScene hands them to the kits.</summary>
    static IReadOnlyList<(List<Vector2> line, float halfWidth)> Corridors(MapScene map)
    {
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        var b = HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta;
        var routes = Statecraft.Game.Map.Routes.RoutePaths.ReadOrNone(inputs.RoutesYaml, (b.lon0, b.lon1, b.lat0, b.lat1), _ => { });
        return Statecraft.Game.Map.Routes.RouteLayer.Corridors(map.Frame, routes);
    }

    /// <summary>Owner rulings 2026-10-11 and 2026-10-12: the 271 places of 1,000+ people beside the 25 game settlements, as Arab village and
    /// Jewish settlement kits from content/map/localities_1948.yaml, merged into a few tile meshes; landmarks from the
    /// file (a church in Bethlehem, none in Druze Daliyat al-Karmel); Bedouin camps sized by the sourced totals.</summary>
    [Test]
    public void TheLocalitiesAndCampsAreDrawnFromTheirFile()
    {
        var map = OpenMap();
        var doc = LoadLocalities(map);
        Assert.AreEqual(271, doc.Places.Count); Assert.AreEqual(271, map.Localities.Count);
        Assert.AreEqual(8, doc.Encampments.Count);
        Assert.IsTrue(doc.Encampments.All(a => a.Hexes.Count > 0), "every camp area has its sub-district's hexes (read at load)");
        var by = doc.Places.ToDictionary(p => p.Settlement.Id);
        Assert.AreEqual("Beisan", by["beit_shean"].Settlement.Name); Assert.AreEqual("Beit She'an", by["beit_shean"].Today);
        Assert.IsNull(by["isdud"].Today, "no today name for a place that did not carry on (owner ruling 2026-10-12)");
        Assert.IsTrue(by["nesher"].Mixed); Assert.IsTrue(Localities.IsMixed("nesher"));
        Assert.AreEqual(Landmark.Church, Localities.LandmarkOf("shefa_amr"), "Shafa 'Amr: the Urban row (review I1)");
        Assert.AreEqual(Landmark.Mosque, Localities.LandmarkOf("dayr_al_qassi"), "review I2");
        Assert.AreEqual(Landmark.Church, Localities.LandmarkOf("bethlehem")); Assert.AreEqual(Landmark.None, Localities.LandmarkOf("daliyat_al_karmel"));
        Assert.AreEqual(Landmark.Mosque, Localities.LandmarkOf("nablus")); Assert.AreEqual(Landmark.WaterTower, Localities.LandmarkOf("kfar_saba"));
        Assert.IsNull(Localities.LandmarkOf("tel_aviv"), "a game settlement keeps its archetype's landmark");
        Assert.AreEqual(Archetype.ArabTown, SettlementKits.ArchetypeOf(by["isdud"].Settlement));
        Assert.AreEqual(Archetype.Moshava, SettlementKits.ArchetypeOf(by["yagur"].Settlement));
        Assert.AreEqual(Archetype.Moshava, SettlementKits.ArchetypeOf(by["ramat_gan"].Settlement));
        Assert.AreEqual(5180, KitPopulation.Of("beit_shean"), "the localities' populations size their kits");
        var st = map.KitStats; TestContext.WriteLine(st);
        Assert.AreEqual(271, st.Localities);
        Assert.Greater(st.LocalityBuildings, 271 * 4, "the localities have their houses (six wanted each; some lose room to a neighbour)");
        int expected = doc.Encampments.Sum(Localities.CampsOf);
        Assert.AreEqual(65, expected, "one per 1,000 rounded half up, no floor: Nablus's 220 draw none (review M4)");
        Assert.AreEqual(0, Localities.CampsOf(doc.Encampments.Single(a => a.Subdistrict == "nablus")));
        Assert.GreaterOrEqual(st.Camps, (int)(0.9f * expected), "nearly every camp found dry, free ground"); Assert.LessOrEqual(st.Camps, expected);
        Assert.Less(st.Draws, map.Settlements.Count + 50, "localities and camps are merged per tile, not one draw per place");
        var root = map.transform.Find(KitLayer.RootName);
        Assert.Greater(root.Cast<Transform>().Count(t => t.name.StartsWith("Localities ")), 5);
    }

    /// <summary>The shared lot list over all 296 places and the camps, with the railways' and roads' corridors as the map
    /// has them: no two places' (or camps') lots overlap, every place claimed ground, every lot stands on dry land, no tent
    /// or house reaches a route (review M8), and no tree grows through any of them.</summary>
    [Test]
    public void NoLotsOverlapAndNoTreeGrowsThroughAVillageOrTent()
    {
        var map = OpenMap();
        var g = Ground(map);
        foreach (var (line, half) in Corridors(map)) g.AddCorridor(line, half);
        Assert.Greater(g.CorridorSegments, 100, "the routes are in");
        var doc = LoadLocalities(map);
        var locs = doc.Places.Select(p => p.Settlement).ToList();
        var built = SettlementKits.Build(map.Settlements, locs, doc.Encampments, g);
        foreach (var k in built.Towns) Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) Object.DestroyImmediate(t.mesh);
        var lots = built.Claimed;
        int overlaps = 0, wet = 0;
        var grid = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < lots.Count; i++)
        {
            var key = (Mathf.FloorToInt(lots[i].centre.x / 5000f), Mathf.FloorToInt(lots[i].centre.y / 5000f));
            if (!grid.TryGetValue(key, out var cell)) grid[key] = cell = new List<int>();
            cell.Add(i);
            if (g.Water(lots[i].centre.x, lots[i].centre.y)) wet++;
        }
        for (int i = 0; i < lots.Count; i++)
        {
            int cx = Mathf.FloorToInt(lots[i].centre.x / 5000f), cz = Mathf.FloorToInt(lots[i].centre.y / 5000f);
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                if (grid.TryGetValue((cx + dx, cz + dz), out var cell))
                    foreach (var j in cell)
                        if (j > i && lots[i].id != lots[j].id && (lots[i].centre - lots[j].centre).magnitude < lots[i].radius + lots[j].radius - 1f) overlaps++;
        }
        var owners = new HashSet<string>(lots.Select(l => l.id));
        var missing = map.Settlements.Concat(locs).Where(s => !owners.Contains(s.Id)).Select(s => s.Id).ToList();
        TestContext.WriteLine($"{lots.Count} lots over {owners.Count} owners; camps {built.Camps}, tents {built.Tents}; places with no lot: {string.Join(" ", missing)}");
        int onRoute = lots.Count(l => (l.id.StartsWith("camp:") || locs.Any(x => x.Id == l.id)) && g.OnRoute(l.centre, l.radius));
        TestContext.WriteLine($"{onRoute} locality or tent lots reaching a route");
        Assert.AreEqual(0, onRoute, "a tent or village house on a railway or road");
        Assert.AreEqual(0, overlaps, "lots of different places overlap");
        Assert.AreEqual(0, wet, "a lot in the water");
        CollectionAssert.IsEmpty(missing, "every place claimed ground");
        foreach (var l in lots) g.Lots.Add((l.centre, l.radius));
        var trees = Vegetation.Plant(map.Settlements.Concat(locs).ToList(), g);
        int inside = 0;
        foreach (var (kind, m) in trees)
        {
            Vector3 p = m.GetColumn(3); float scale = ((Vector3)m.GetColumn(0)).magnitude;
            foreach (var l in lots) if ((new Vector2(p.x, p.z) - l.centre).magnitude < l.radius + Vegetation.CrownRadiusM(kind) * scale) { inside++; break; }
        }
        TestContext.WriteLine($"{trees.Count} trees, {inside} reaching a lot");
        Assert.AreEqual(0, inside, "a tree grows through a building, landmark or tent");
    }

    /// <summary>Review M8: the localities, camps and tile meshes are as deterministic as the towns (two builds, same tiles'
    /// vertex counts and bounds, same camps and tents), and every game settlement whose archetype has a landmark keeps it
    /// (the game towns' landmarks claim their spots before any locality's).</summary>
    [Test]
    public void TheVillagesAreSeededAndTheGameTownsKeepTheirLandmarks()
    {
        var map = OpenMap();
        var doc = LoadLocalities(map);
        var locs = doc.Places.Select(p => p.Settlement).ToList();
        var corridors = Corridors(map);
        SettlementKits.Result Once()
        {
            var g = Ground(map);
            foreach (var (line, half) in corridors) g.AddCorridor(line, half);
            return SettlementKits.Build(map.Settlements, locs, doc.Encampments, g);
        }
        var a = Once(); var b = Once();
        try
        {
            Assert.AreEqual(a.Tiles.Count, b.Tiles.Count);
            for (int i = 0; i < a.Tiles.Count; i++)
            {
                Assert.AreEqual(a.Tiles[i].name, b.Tiles[i].name);
                Assert.AreEqual(a.Tiles[i].mesh.vertexCount, b.Tiles[i].mesh.vertexCount, a.Tiles[i].name);
                Assert.AreEqual(a.Tiles[i].mesh.bounds, b.Tiles[i].mesh.bounds, a.Tiles[i].name);
            }
            Assert.AreEqual((a.Camps, a.Tents, a.LocalityBuildings), (b.Camps, b.Tents, b.LocalityBuildings));
            var lost = map.Settlements.Where(s => SettlementKits.ArchetypeOf(s) != Archetype.WhiteCity && a.TownLandmarks[s.Id] == 0).Select(s => s.Id).ToList();
            TestContext.WriteLine($"tiles {a.Tiles.Count}, camps {a.Camps}, tents {a.Tents}, locality buildings {a.LocalityBuildings}; town landmarks: "
                + string.Join(" ", a.TownLandmarks.Select(kv => $"{kv.Key}={kv.Value}")));
            CollectionAssert.IsEmpty(lost, "a game settlement lost its landmark");
        }
        finally
        {
            foreach (var r in new[] { a, b })
            {
                foreach (var k in r.Towns) Object.DestroyImmediate(k.mesh);
                foreach (var t in r.Tiles) Object.DestroyImmediate(t.mesh);
            }
        }
    }

    /// <summary>#317: a lake hex is not all water. Tiberias's shore (35.530 E, 32.7944 N, about 114 m below sea level) lies in
    /// a lake hex but 100 m above the Sea of Galilee's surface (about -214 m), and it is land; the open lake (35.58 E, 32.80
    /// N) is water. The lake's water is told as tools/terrain_bake.py tells it for the drawn water.</summary>
    [Test]
    public void TheLakeIsItsWaterNotItsWholeHex()
    {
        var map = OpenMap();
        var g = Ground(map);
        var shore = map.Frame.ToWorld(35.530, 32.7944, 0); var lake = map.Frame.ToWorld(35.58, 32.80, 0);
        TestContext.WriteLine($"shore: {g.Biome(shore.x, shore.z)} {g.ElevM(shore.x, shore.z):0.0} m; lake: {g.Biome(lake.x, lake.z)} {g.ElevM(lake.x, lake.z):0.0} m");
        Assert.AreEqual("lake", g.Biome(shore.x, shore.z), "the shore point lies in a lake hex");
        Assert.Greater(g.ElevM(shore.x, shore.z), g.ElevM(lake.x, lake.z) + 50f, "the shore point stands well above the lake");
        Assert.IsFalse(g.Water(shore.x, shore.z), "Tiberias's shore in the lake hex is land");
        Assert.AreEqual("lake", g.Biome(lake.x, lake.z));
        Assert.IsTrue(g.Water(lake.x, lake.z), "the Sea of Galilee is water");
    }

    /// <summary>Sea round 3 (#388, owner 15 October 2026: "keep them off the sand"): no citrus or olive crown reaches the
    /// beach's full sand. Read from coast.png itself (the file the terrain's ScShore draws the beach from, beach-scaled on the
    /// dune coasts): ScShore paints full sand where the land-side distance is under 0.55 of a width that reaches 1.4 x
    /// ScBeachM (693 m), and the distance field is 1-Lipschitz there, so a trunk at that distance plus its crown's radius
    /// keeps the whole crown off it. One 8-bit level of coast.png (94 m) is the tolerance. ScShore draws sand only by
    /// sea-level water (above -100 m), and coast.png also measures the distance to the rift lakes (the Sea of Galilee at
    /// -210 m, the Dead Sea): a crown that reads near water is counted only when KitGround.SeaDistance, the sea-level test
    /// the keep-out itself uses, finds the sea within that reach (plus the dune coasts' extra width, which coast.png takes
    /// out of its land side), so a tree by a lake, where no sand is drawn, is not counted.</summary>
    [Test]
    public void NoGroveOrOliveCrownStandsOnTheBeach()
    {
        var map = OpenMap();
        var coast = new Texture2D(2, 2);
        try
        {
            Assert.IsTrue(coast.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath, "Textures/Map/coast.png"))));
            var g = Ground(map);
            // The game's list (towns and 1948 localities), so the keep-out's sand yields to every place's own ground as the
            // bake's does (option (c)).
            var trees = Vegetation.Plant(map.Settlements.Concat(LoadLocalities(map).Places.Select(p => p.Settlement)).ToList(), g);
            float LandM(float x, float z) => (coast.GetPixelBilinear(x / g.Width, z / g.Length).r - 0.5f) * 2f * 12000f;   // ScCoastRangeM
            const float FullSandM = 0.55f * 1.4f * 900f, LevelM = 2f * 12000f / 255f;
            var bad = new List<string>();
            int citrus = 0, olives = 0, byLake = 0;
            foreach (var (kind, m) in trees)
            {
                if (kind != TreeKind.Citrus && kind != TreeKind.CitrusPatch && kind != TreeKind.Olive) continue;
                if (kind == TreeKind.Olive) olives++; else citrus++;
                Vector3 p = m.GetColumn(3); float crown = Vegetation.CrownRadiusM(kind) * ((Vector3)m.GetColumn(0)).magnitude;
                float d = LandM(p.x, p.z) - crown;
                if (d >= FullSandM - LevelM) continue;
                // The water coast.png sees, in metres: on the dune coasts its land side reads up to BeachBands.ExtraM short.
                float reach = LandM(p.x, p.z) + BeachBands.ExtraM + 2f * LevelM;
                if (g.SeaDistance(p.x, p.z, reach) == float.MaxValue)   // that water is a lake
                {
                    // A crown SeaDistance misses would land here and pass silently, so it must stand in the rift: the
                    // Mediterranean's shore is at sea level, the Sea of Galilee's at -210 m (today 6 crowns, -70 to -214 m,
                    // on its western shore by Tiberias).
                    byLake++;
                    if (g.ElevM(p.x, p.z) >= -50f) bad.Add($"{kind} at ({p.x:0}, {p.z:0}): near water SeaDistance misses, but not in the rift ({g.ElevM(p.x, p.z):0} m)");
                    continue;
                }
                bad.Add($"{kind} at ({p.x:0}, {p.z:0}): crown {d:0} m from the sea's edge in coast.png");
            }
            TestContext.WriteLine($"{citrus} citrus and {olives} olive instances; {bad.Count} crowns on the full sand; {byLake} near a rift lake only");
            CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} crowns on the beach's full sand");
            Assert.Greater(citrus, 500, "the groves keep their trees");
        }
        finally { Object.DestroyImmediate(coast); }
    }

    /// <summary>Option (c) (#685; owner, 16 October 2026: "Keep each settlement's own lots and fields off the sand"): the
    /// lots the map builds (every building's, landmark's and tent's, over the 296 places, the camps and the routes), each
    /// centre read against coast.png's full sand: ScShore paints full sand under 0.55 of a width of 0.6-1.4 x ScBeachM
    /// (900 m), so under 297 m at any noise width and under 693 m at the widest. A lot by a lake only is not counted (no sand
    /// is drawn there; KitGround.SeaDistance, as NoGroveOrOliveCrownStandsOnTheBeach). Main before the wide beach (adf102c9)
    /// has 9 and 30 such lots (the old towns' waterfronts, al-Jura, Deir al-Balah and the like, which met the shore); the
    /// wide beach before option (c) had 83 and 119, over 17 places.</summary>
    [Test]
    public void TheSettlementsOwnLotsStayOffTheDuneSand()
    {
        var (any, widest, places) = LotsOnTheSand(System.IO.Path.Combine(Application.dataPath, "Textures/Map/coast.png"));
        TestContext.WriteLine($"lot centres on the full sand: {any} at any noise, {widest} at the widest; places: {string.Join(", ", places)}");
        Assert.LessOrEqual(any, 9, "lots on the full sand at any noise width: main's 9");
        Assert.LessOrEqual(widest, 30, "lots on the full sand at the widest noise: main's 30");
    }

    /// <summary>The map's lots (TheSettlementsOwnLotsStayOffTheDuneSand) whose centre is on the full sand of the coast.png at
    /// <paramref name="coastPath"/>: at any noise width, at the widest, and the places they belong to (sorted).</summary>
    internal static (int any, int widest, List<string> places) LotsOnTheSand(string coastPath)
    {
        var map = OpenMap();
        var g = Ground(map);
        foreach (var (line, half) in Corridors(map)) g.AddCorridor(line, half);
        var doc = LoadLocalities(map);
        var built = SettlementKits.Build(map.Settlements, doc.Places.Select(p => p.Settlement).ToList(), doc.Encampments, g);
        foreach (var k in built.Towns) Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) Object.DestroyImmediate(t.mesh);
        Assert.Greater(built.Claimed.Count, 3000, "the whole map's lots");
        var coast = new Texture2D(2, 2);
        try
        {
            Assert.IsTrue(coast.LoadImage(System.IO.File.ReadAllBytes(coastPath)));
            int any = 0, widest = 0; var places = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (var (c, _, id) in built.Claimed)
            {
                float d = (coast.GetPixelBilinear(c.x / g.Width, c.y / g.Length).r - 0.5f) * 2f * 12000f;   // ScCoastRangeM
                if (d <= 0f || d >= 0.55f * 1.4f * 900f) continue;
                if (g.SeaDistance(c.x, c.y, 3000f) == float.MaxValue) continue;   // a lake: no sand drawn
                widest++; if (d < 0.55f * 0.6f * 900f) any++;
                places.Add(id);
            }
            return (any, widest, places.ToList());
        }
        finally { Object.DestroyImmediate(coast); }
    }

    /// <summary>#317: nothing stands in a lake's drawn water. Read from coast.png itself (the file the water shader samples,
    /// decoded here because the import is not CPU-readable): no tree trunk and no building's or landmark's lot centre in the
    /// rift (a lake hex, or ground below MapBuilder.LakeBelowM) lies where the drawn water covers the ground (coast below
    /// 0.5). A first version of the lake test put a palm in the Kinneret by Ghuweir Abu Shusha, in a shore hex of another
    /// biome.</summary>
    [Test]
    public void NothingStandsInALakesDrawnWater()
    {
        var map = OpenMap();
        var coast = new Texture2D(2, 2);
        try
        {
            Assert.IsTrue(coast.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath, "Textures/Map/coast.png"))));
            var g = Ground(map);
            foreach (var (line, half) in Corridors(map)) g.AddCorridor(line, half);
            var doc = LoadLocalities(map);
            var locs = doc.Places.Select(p => p.Settlement).ToList();
            var built = SettlementKits.Build(map.Settlements, locs, doc.Encampments, g);
            foreach (var k in built.Towns) Object.DestroyImmediate(k.mesh);
            foreach (var t in built.Tiles) Object.DestroyImmediate(t.mesh);
            foreach (var l in built.Claimed) g.Lots.Add((l.centre, l.radius));
            var trees = Vegetation.Plant(map.Settlements.Concat(locs).ToList(), g);
            bool Wet(float x, float z) => coast.GetPixelBilinear(x / g.Width, z / g.Length).r < 0.5f;
            var bad = new List<string>();
            int checkedN = 0;
            bool Rift(float x, float z) => g.Biome(x, z) == "lake" || g.ElevM(x, z) < MapBuilder.LakeBelowM;
            foreach (var l in built.Claimed)
                if (Rift(l.centre.x, l.centre.y)) { checkedN++; if (Wet(l.centre.x, l.centre.y)) bad.Add($"{l.id} lot at {l.centre}"); }
            foreach (var (kind, m) in trees)
            {
                Vector3 p = m.GetColumn(3);
                if (!Rift(p.x, p.z)) continue;
                checkedN++;
                if (Wet(p.x, p.z)) bad.Add($"{kind} tree at ({p.x:0}, {p.z:0})");
            }
            TestContext.WriteLine($"{checkedN} lots and trees in the rift checked against the drawn water");
            CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} in the drawn water");
            Assert.Greater(checkedN, 20, "the rift's shores hold lots and trees");
        }
        finally { Object.DestroyImmediate(coast); }
    }

    /// <summary>#317, review of #339 (F2): the small towns' fallback houses take only ground no other place builds on. With
    /// the fallback on, every lot every place claims (the towns', the 1948 localities' and the camps') is the same, in the
    /// same order, as with it off; the fallback's lots only follow them, and each belongs to a game town.</summary>
    [Test]
    public void TheSmallTownFloorMovesNoOtherPlace()
    {
        var map = OpenMap();
        var doc = LoadLocalities(map);
        var locs = doc.Places.Select(p => p.Settlement).ToList();
        var corridors = Corridors(map);
        SettlementKits.Result Once(bool scatter)
        {
            SettlementKits.SmallTownScatter = scatter;
            try
            {
                var g = Ground(map);
                foreach (var (line, half) in corridors) g.AddCorridor(line, half);
                var r = SettlementKits.Build(map.Settlements, locs, doc.Encampments, g);
                foreach (var k in r.Towns) Object.DestroyImmediate(k.mesh);
                foreach (var t in r.Tiles) Object.DestroyImmediate(t.mesh);
                return r;
            }
            finally { SettlementKits.SmallTownScatter = true; }
        }
        var off = Once(false); var on = Once(true);
        Assert.Greater(on.Claimed.Count, off.Claimed.Count, "the fallback placed houses");
        for (int i = 0; i < off.Claimed.Count; i++)
            Assert.AreEqual(off.Claimed[i], on.Claimed[i], $"lot #{i} ({off.Claimed[i].id}) moved with the fallback on");
        var towns = new HashSet<string>(map.Settlements.Select(s => s.Id));
        var extra = on.Claimed.Skip(off.Claimed.Count).Select(l => l.id).ToList();
        CollectionAssert.IsEmpty(extra.Where(id => !towns.Contains(id)).ToList(), "a fallback lot owned by a place that is not a game town");
        Assert.AreEqual((off.Camps, off.Tents, off.LocalityBuildings), (on.Camps, on.Tents, on.LocalityBuildings));
        TestContext.WriteLine($"{extra.Count} fallback houses in {extra.Distinct().Count()} towns: {string.Join(" ", extra.Distinct())}");
    }

    /// <summary>The boot reads the baked runtime form (tools/localities_bake.py; pytest fails a stale bake). It yields the same
    /// rows as the sourced YAML: ids, 1948 and today's names (English and Hebrew), coordinates, type, landmark, the mixed
    /// flag, population and how many sources each row cites; and the same camp areas.</summary>
    [Test]
    public void TheRuntimeFileYieldsTheSameRowsAsTheYaml()
    {
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        var b = HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta;
        var bounds = (b.lon0, b.lon1, b.lat0, b.lat1);
        var reserved = new[] { "tel_aviv" };
        var yaml = Localities.Parse(LocalitiesYaml, bounds, reserved);
        var tsv = Localities.ParseRuntime(inputs.LocalitiesTsv, bounds, reserved);
        Assert.AreEqual(yaml.Places.Count, tsv.Places.Count);
        for (int i = 0; i < yaml.Places.Count; i++)
        {
            var (y, t) = (yaml.Places[i], tsv.Places[i]);
            string id = y.Settlement.Id;
            Assert.AreEqual((id, y.Settlement.Name, y.Settlement.Type, y.Settlement.Lon, y.Settlement.Lat),
                (t.Settlement.Id, t.Settlement.Name, t.Settlement.Type, t.Settlement.Lon, t.Settlement.Lat), id);
            Assert.AreEqual((y.NameHe, y.Today, y.TodayHe, y.Population, y.Landmark, y.Mixed, y.Sources), (t.NameHe, t.Today, t.TodayHe, t.Population, t.Landmark, t.Mixed, t.Sources), id);
            Assert.Greater(t.Sources, 0, id);
        }
        Assert.AreEqual(yaml.Encampments.Select(a => (a.Subdistrict, a.Population, a.Sources)).ToList(),
            tsv.Encampments.Select(a => (a.Subdistrict, a.Population, a.Sources)).ToList());
        Assert.AreEqual("Sha'ab", tsv.Places.Single(p => p.Settlement.Id == "shaab").Today, "owner rule: Sha'ab is a living local council");
        Assert.IsTrue(tsv.Places.Single(p => p.Settlement.Id == "nesher").Mixed);
        Assert.AreEqual("ביסאן", tsv.Places.Single(p => p.Settlement.Id == "beit_shean").NameHe, "#558: the 1948 form, as the English shows it");
        Assert.IsNull(tsv.Places.Single(p => p.Settlement.Id == "naana").NameHe, "#558: no sourced Hebrew name, so none");
    }

    /// <summary>A malformed runtime row is an error naming the file and the line.</summary>
    [Test]
    public void TheRuntimeFileRejectsBadRowsByLine()
    {
        var b = (34.2, 35.9, 29.45, 33.35);
        var reserved = new[] { "tel_aviv" };
        const string ok = "place\ta\tA\t\t\t\t35.0\t32.0\tvillage\tmosque\t0\t1200\tdraft\t2026-10-11\t3";
        Assert.AreEqual(1, Localities.ParseRuntime("# header\n" + ok + "\n", b, reserved, "loc.tsv").Places.Count);
        void Bad(string tsv, string expect)
        {
            var ex = Assert.Throws<System.IO.InvalidDataException>(() => Localities.ParseRuntime(tsv, b, reserved, "loc.tsv"));
            StringAssert.Contains(expect, ex.Message);
        }
        Bad("# header\n" + ok.Replace("\t35.0\t", "\t35,0\t") + "\n", "loc.tsv:2: lon '35,0' is not a number");
        Bad("# header\n" + ok + "\n" + ok.Substring(0, ok.LastIndexOf('\t')) + "\n", "loc.tsv:3: a place row has 14 fields");
        Bad("# header\n" + ok.Replace("\t1200\t", "\t0\t") + "\n", "loc.tsv:2: 'a' population 0");
        Bad("# header\n" + ok.Replace("\t2026-10-11\t3", "\t2026-10-11\t0") + "\n", "loc.tsv:2: 'a' sources is empty");
        Bad("# header\n" + ok.Replace("\tmosque\t0\t", "\tmosque\t2\t") + "\n", "loc.tsv:2: mixed '2'");
        Bad("# header\n" + ok.Replace("place\t", "town\t") + "\n", "loc.tsv:2: a row starts 'town'");
        Bad("# header\n" + ok.Replace("\tvillage\t", "\thamlet\t") + "\n", "loc.tsv:2: 'a' has unknown type");
    }

    [Test]
    public void TheLocalitiesFileRejectsBadRows()
    {
        var b = (34.2, 35.9, 29.45, 33.35);
        var reserved = new[] { "tel_aviv" };
        const string src = "  checked: '2026-10-11'\n  status: draft\n  sources:\n  - https://example.org/x\n";
        string Place(string id, string extra = "") =>
            $"- id: {id}\n  name: X\n  lon: 35.0\n  lat: 32.0\n  type: village\n  landmark: mosque\n  population: 1200\n{extra}" + src;
        Assert.AreEqual(1, Localities.Parse("places:\n" + Place("a"), b, reserved, "loc.yaml").Places.Count);
        void Bad(string yaml, string expect)
        {
            var ex = Assert.Throws<System.IO.InvalidDataException>(() => Localities.Parse(yaml, b, reserved, "loc.yaml"));
            StringAssert.Contains("loc.yaml", ex.Message); StringAssert.Contains(expect, ex.Message);
        }
        Bad("places:\n" + Place("tel_aviv"), "game settlement");
        Bad("places:\n" + Place("a") + Place("a"), "duplicate");
        Bad("places:\n" + Place("a").Replace("type: village", "type: hamlet"), "type");
        Bad("places:\n" + Place("a").Replace("landmark: mosque", "landmark: tower"), "landmark");
        Bad("places:\n" + Place("a").Replace("lon: 35.0", "lon: 36.5"), "outside");
        Bad("places:\n" + Place("a").Replace("population: 1200", "population: 0"), "population");
        Bad("places:\n" + Place("a").Replace("status: draft", "status: guess"), "status");
        Bad("places:\n" + Place("a").Replace("https://example.org/x", "example.org/x"), "URL");
        Bad("places:\n" + Place("a", "  fate: x\n"), "fate");
        Bad("places:\n" + Place("a", "  today:\n    name: Y\n") , "today");
        // A person's sign-off (#375): both or neither, a GitHub handle (never an email), a day; on a place or a camp row.
        Assert.AreEqual(1, Localities.Parse("places:\n" + Place("a", "  reviewed_by: eyalgolan\n  reviewed_on: '2026-10-15'\n"), b, reserved, "loc.yaml").Places.Count);
        Bad("places:\n" + Place("a", "  reviewed_by: eyalgolan\n"), "reviewed_on");
        Bad("places:\n" + Place("a", "  reviewed_on: '2026-10-15'\n"), "reviewed_by");
        Bad("places:\n" + Place("a", "  reviewed_by: someone@example.org\n  reviewed_on: '2026-10-15'\n"), "reviewed_by");
        Bad("places:\n" + Place("a", "  reviewed_by: ''\n  reviewed_on: '2026-10-15'\n"), "reviewed_by");
        Bad("places:\n" + Place("a", "  reviewed_by: eyalgolan\n  reviewed_on: 15 October\n"), "reviewed_on");
        Bad("places:\n" + Place("a") + "encampments:\n- subdistrict: gaza\n  population: 500\n  reviewed_by: eyalgolan\n" + src, "reviewed_on");
        // #558: name_he_sources need the name, and each source an https url and a checked date.
        Bad("places:\n" + Place("a", "  name_he_sources:\n  - {url: 'https://he.wikipedia.org/x', checked: '2026-10-14'}\n"), "name_he_sources without a name_he");
        Bad("places:\n" + Place("a", "  name_he: \"\\u05D0\"\n  name_he_sources:\n  - {url: 'https://he.wikipedia.org/x'}\n"), "name_he_sources[0]");
        Bad("places:\n" + Place("a", "  name_he: \"\\u05D0\"\n  name_he_sources:\n  - {url: 'https://x', when: y}\n"), "unknown key 'when'");
        // The review flag (#564): agent-checked on a named row only, any other value refused.
        Bad("places:\n" + Place("a", "  name_he_review: agent-checked\n"), "name_he_review without a name_he");
        Bad("places:\n" + Place("a", "  name_he: \"\\u05D0\"\n  name_he_review: checked\n"), "name_he_review 'checked'");
        // A blank, empty or non-scalar flag is not a review (second review of #610).
        foreach (var blank in new[] { "''", "~", "", "[agent-checked]" })
            Bad("places:\n" + Place("a", $"  name_he: \"\\u05D0\"\n  name_he_review: {blank}\n"), "name_he_review");
        Assert.AreEqual(1, Localities.Parse("places:\n" + Place("a", "  name_he: \"\\u05D0\"\n  name_he_review: agent-checked\n"), b, reserved, "loc.yaml").Places.Count);
        Assert.AreEqual("\u05D0", Localities.Parse("places:\n" + Place("a", "  name_he: \"\\u05D0\"\n  name_he_sources:\n  - {url: 'https://he.wikipedia.org/x', checked: '2026-10-14'}\n"),
            b, reserved, "loc.yaml").Places[0].NameHe);
        Bad("places:\n" + Place("a") + "encampments:\n- subdistrict: gaza\n  population: 0\n" + src, "population");
        Bad("places:\n" + Place("a") + "encampments:\n- subdistrict: gaza\n  population: 500\n  name: Camp\n" + src, "name");
        Bad("places:\n" + Place("a") + "encampments:\n- subdistrict: gaza\n  population: 500\n  hexes: [[1, 2]]\n" + src, "hexes");
        // Malformed YAML (the event reader, review I6): each error names the file and the line.
        Bad("places:\n" + Place("a").Replace("name: X", "name: \"X"), "loc.yaml:3: ");                 // an unclosed quote (from its line)
        Bad("places:\n" + Place("a").Replace("name: X", "name: X\n  name: Y"), "duplicate key 'name'");
        Bad("places:\n" + Place("a").Replace("name: X", "name: X\n  name: Y"), "loc.yaml:4: ");
        Bad("places:\n" + Place("a").Replace("lon: 35.0", "lon: 35,0"), "lon is not a number (line 4)");
        Bad("places:\n" + Place("a", "  fate: x\n"), "line 9");
        Bad("places:\n" + Place("a").Replace("population: 1200", "population: 0"), "loc.yaml:2: 'a'");
        Bad("places:\n" + Place("a").Replace("name: X", "name: &n X\n  name_he: *n"), "loc.yaml:4: anchors and aliases");
        Bad("places: x\n", "'places' is not a list (line 1)");
        Bad("places:\n" + Place("a").Replace("  sources:\n  - https://example.org/x", "  sources: https://example.org/x"), "'sources' is not a list");
        // Quoting, escapes and Hebrew read as YAML says: a quoted "null" is a name, "\u05D0" is aleph.
        var ok = Localities.Parse("places:\n" + Place("a").Replace("name: X", "name: \"null\"")
            .Replace("type: village", "type: village\n  today: {name: 'Bet \\u05D0', name_he: \"\\u05D0'\", source: 'https://x.org/it''s'}")
            .Replace("lon: 35.0", "lon: '35.0'"), b, reserved, "loc.yaml").Places[0];
        Assert.AreEqual("null", ok.Settlement.Name);
        Assert.AreEqual("Bet \\u05D0", ok.Today, "single quotes keep a backslash");
        Assert.AreEqual("\u05D0'", ok.TodayHe, "double quotes read the escape");
        Assert.AreEqual(35.0, ok.Settlement.Lon);
    }

    /// <summary>Review M6: switching the kits off shows the marker boxes again, so a settlement is never left undrawn.</summary>
    [Test]
    public void TheKitsToggleSwapsKitsAndMarkers()
    {
        var map = OpenMap();
        var kits = map.transform.Find(KitLayer.RootName).GetComponentsInChildren<MeshRenderer>();
        var markers = map.transform.Find("Settlements").GetComponentsInChildren<MeshRenderer>();
        Assert.IsTrue(kits.All(r => r.enabled)); Assert.IsTrue(markers.All(r => !r.enabled));
        VisualToggles.Set("kits", false);
        Assert.IsTrue(kits.All(r => !r.enabled)); Assert.IsTrue(markers.All(r => r.enabled), "the markers come back");
        VisualToggles.Set("kits", true);
        Assert.IsTrue(kits.All(r => r.enabled)); Assert.IsTrue(markers.All(r => !r.enabled));
    }

    /// <summary>A settlement's point on the ground plane (world x, z).</summary>
    static Vector2 Spot(MapScene map, string id)
    {
        var s = map.Settlements.Single(x => x.Id == id); var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
        return new Vector2(w.x, w.z);
    }

    /// <summary>Issue #32: a ma'abara of 1950-51 is mostly canvas tents with a few tin or canvas huts. The hut share is a
    /// parameter (a staged look later), and it changes what stands on a plot, never which plots are used.</summary>
    [Test]
    public void AMaabaraIsMostlyTentsWithAFewHuts()
    {
        var map = OpenMap();
        var g = PlacementLayer.CampGround(map);
        var camp = new List<(string id, Vector2 at)> { ("transit_camp_revivim", Spot(map, "revivim")) };
        var mixed = SettlementKits.Maabarot(camp, g)[0];
        var tents = SettlementKits.Maabarot(camp, g, 0f)[0];
        var huts = SettlementKits.Maabarot(camp, g, 1f)[0];
        TestContext.WriteLine($"share {SettlementKits.MaabaraHutShare}: {mixed.tents} tents, {mixed.huts} huts, {mixed.mesh?.vertexCount} vertices");
        Assert.IsNotNull(mixed.mesh, "the camp found free ground");
        Assert.Greater(mixed.huts, 0, "a few huts"); Assert.Greater(mixed.tents, 2 * mixed.huts, "mostly tents");
        Assert.AreEqual((mixed.tents + mixed.huts, 0), (tents.tents, tents.huts), "share 0: tents only, on the same plots");
        Assert.AreEqual((0, mixed.tents + mixed.huts), (huts.tents, huts.huts), "share 1: huts only, on the same plots");
        foreach (var k in new[] { mixed, tents, huts }) if (k.mesh != null) Object.DestroyImmediate(k.mesh);
    }

    /// <summary>Issue #32: ma'abarot are seeded (two builds, the same camps), stand on dry land clear of every town's plots,
    /// the 1948 localities and the Bedouin camps, keep off the routes, and two camps asked for on the same ground keep
    /// apart: a camp stands in its own hex (review #557), so the second finds none left there (the first camp's lots) and
    /// is drawn as its box, as a camp on Tel Aviv's own point is, the city's plots covering its hex. No pairwise overlap
    /// check between camps: a hex holds one placement (MapActions.Check), every structure stands in its camp's hex, and
    /// on all 34 pairs of neighbouring settlement hexes the two camps never touched, built together (1 km apart at the
    /// closest) or separately on their own lots (review #557), so such a check would compare nothing that could fail.</summary>
    [Test]
    public void MaabarotAreSeededAndStandOnFreeDryGround()
    {
        var map = OpenMap();
        var camps = new List<(string id, Vector2 at)> { ("a", Spot(map, "revivim")), ("b", Spot(map, "revivim")), ("c", Spot(map, "tel_aviv")) };
        var g = PlacementLayer.CampGround(map);
        var first = SettlementKits.Maabarot(camps, g, out var placed);
        var second = SettlementKits.Maabarot(camps, PlacementLayer.CampGround(map));
        Assert.IsNull(first[1].mesh, "b: no ground left in Revivim's hex"); Assert.IsNull(second[1].mesh);
        Assert.IsNull(first[2].mesh, "c: no ground in Tel Aviv's hex"); Assert.IsNull(second[2].mesh);
        for (int i = 0; i < 1; i++)
        {
            Assert.IsNotNull(first[i].mesh, camps[i].id + ": found free ground");
            Assert.AreEqual((first[i].tents, first[i].huts), (second[i].tents, second[i].huts), camps[i].id);
            Assert.AreEqual(first[i].mesh!.vertexCount, second[i].mesh!.vertexCount, camps[i].id);
            Assert.AreEqual(first[i].mesh!.bounds, second[i].mesh!.bounds, camps[i].id);
            Assert.Greater(placed.Count(p => p.Id == camps[i].id && p.Kind == "tent"), 0, camps[i].id);
            Assert.LessOrEqual(placed.Count(p => p.Id == camps[i].id && p.Kind == "communal_hut"), 1, camps[i].id);
        }
        foreach (var k in first.Concat(second)) if (k.mesh != null) Object.DestroyImmediate(k.mesh);
        TestContext.WriteLine(string.Join(", ", camps.Select((c, i) => $"{c.id}: {first[i].tents} tents, {first[i].huts} huts")));
        int wet = 0, inTown = 0, onRoute = 0;
        foreach (var p in placed)
        {
            if (g.Water(p.C.x, p.C.y)) wet++;
            if (g.InTown(p.C.x, p.C.y, SettlementKits.PlotReach)) inTown++;
            if (g.OnRoute(p.C, 0f)) onRoute++;
        }
        Assert.AreEqual(0, wet, "a tent or hut in the water");
        Assert.AreEqual(0, inTown, "a tent or hut on a town's, locality's or Bedouin camp's ground");
        Assert.AreEqual(0, onRoute, "a tent or hut on a railway or road");
    }

    /// <summary>Issue #32: with the kits on, a transit camp is drawn as a ma'abara kit (Kit.mat, its own mesh) in place of its
    /// marker box, built when the camp is placed; housing keeps its box, and the redraw frees the camp's mesh.</summary>
    [Test]
    public void ATransitCampIsDrawnAsAMaabaraWhenItIsPlaced()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var hex = c.SettlementHexes.Single(s => s.Id == "revivim");
        var date = new System.DateTime(1950, 8, 26);
        var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
        layer.Render(new[] { new Statecraft.Sim.Placement(Statecraft.Sim.MapActions.TransitCamp, hex.Q, hex.R, date) }, c, map);
        Assert.AreEqual(1, layer.transform.childCount);
        var camp = layer.transform.GetChild(0);
        Assert.AreEqual($"{Statecraft.Sim.MapActions.TransitCamp}_{hex.Q}_{hex.R}", camp.name, "named as its box was");
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat"), camp.GetComponent<MeshRenderer>().sharedMaterial);
        var mesh = camp.GetComponent<MeshFilter>().sharedMesh;
        Assert.AreNotSame(Resources.GetBuiltinResource<Mesh>("Cube.fbx"), mesh, "a kit, not the marker box");
        Assert.Greater(mesh.vertexCount, 300, "rows of tents");
        layer.Render(new[] { new Statecraft.Sim.Placement(Statecraft.Sim.MapActions.Housing, hex.Q, hex.R, date) }, c, map);
        Assert.IsTrue(mesh == null, "the camp's mesh goes with it");
        Assert.AreEqual(1, layer.transform.childCount);
        Assert.AreSame(map.MarkerMaterial, layer.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial, "housing keeps its box");
    }

    /// <summary>Review (issue #32): with the "kits" toggle off a transit camp is drawn as its marker box, as the settlements
    /// are, and no camp kit is visible; switching the kits back on draws the ma'abara again.</summary>
    [Test]
    public void WithTheKitsOffATransitCampIsItsBox()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var hex = c.SettlementHexes.Single(s => s.Id == "revivim");
        var camp = new[] { new Statecraft.Sim.Placement(Statecraft.Sim.MapActions.TransitCamp, hex.Q, hex.R, new System.DateTime(1950, 8, 26)) };
        var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
        var kitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        VisualToggles.Set("kits", false);
        layer.Render(camp, c, map);
        Assert.AreEqual(1, layer.transform.childCount);
        var box = layer.transform.GetChild(0);
        Assert.AreSame(Resources.GetBuiltinResource<Mesh>("Cube.fbx"), box.GetComponent<MeshFilter>().sharedMesh, "the marker box");
        Assert.AreSame(map.MarkerMaterial, box.GetComponent<MeshRenderer>().sharedMaterial);
        Assert.IsFalse(layer.GetComponentsInChildren<MeshRenderer>().Any(r => r.enabled && r.sharedMaterial == kitMat), "no camp kit drawn");
        VisualToggles.Set("kits", true);
        layer.Render(camp, c, map);
        Assert.AreSame(kitMat, layer.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial, "the kit again");
    }

    /// <summary>Review (issue #32): the trees planted at map build do not stand in a ma'abara built later. While the camp's kit
    /// is drawn no tree drawn has its crown on the camp's ground (the disc round each tent, hut, communal hut and water
    /// tower); housing in its place, or the kits off (the camp is its box), gives them back. Safed's camp lands among the
    /// hill olives (it was Rosh Pinna's until main's units file left that hex no ground for a camp with no date: see
    /// AMaabaraKeepsOffTokensGridMarksAndSteepGround).</summary>
    [Test]
    public void NoTreeStandsInAMaabara()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var hex = c.SettlementHexes.Single(s => s.Id == "safed");
        var date = new System.DateTime(1950, 8, 26);
        var veg = map.GetComponentInChildren<VegetationLayer>(); Assert.IsNotNull(veg);
        int before = veg.Drawable().Count();
        var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
        var camp = new[] { new Statecraft.Sim.Placement(Statecraft.Sim.MapActions.TransitCamp, hex.Q, hex.R, date) };
        layer.Render(camp, c, map);
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat"), layer.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial, "drawn as its kit");
        var at = c.HexAt(hex.Q, hex.R)!; var w = map.Frame.ToWorld(at.Lon, at.Lat, 0);
        var built = SettlementKits.Maabarot(new List<(string id, Vector2 at)> { (layer.transform.GetChild(0).name, new Vector2(w.x, w.z)) },
            PlacementLayer.CampGround(map), out var placed)[0];
        if (built.mesh != null) Object.DestroyImmediate(built.mesh);
        int In(IEnumerable<(TreeKind kind, Matrix4x4 m)> trees) => trees.Count(t => placed.Any(p =>
            (new Vector2(t.m.m03, t.m.m23) - p.C).magnitude < 0.5f * new Vector2(p.W, p.D).magnitude + VegetationLayer.CrownRadius(t.kind, t.m)));
        int planted = In(veg.Planted());
        TestContext.WriteLine($"{placed.Count} structures: {planted} trees planted on them, {veg.Hidden} hidden");
        Assert.Greater(planted, 0, "the camp stands where trees were planted");
        Assert.AreEqual(0, In(veg.Drawable()), "a tree drawn in the camp");
        VisualToggles.Set("kits", false);
        layer.Render(camp, c, map);
        Assert.AreEqual(before, veg.Drawable().Count(), "the camp as its box gives the trees back");
        VisualToggles.Set("kits", true);
        layer.Render(camp, c, map);
        Assert.AreEqual(0, In(veg.Drawable()));
        layer.Render(new[] { new Statecraft.Sim.Placement(Statecraft.Sim.MapActions.Housing, hex.Q, hex.R, date) }, c, map);
        Assert.AreEqual(before, veg.Drawable().Count(), "housing in the camp's place gives the trees back");
        Assert.AreEqual(planted, In(veg.Drawable()));
    }

    /// <summary>Review (issue #32): a ma'abara keeps to the Bedouin camps' rules (Encampments): no tent, hut or water tower
    /// on a unit token, on a hex edge's grid mark or on steep ground, so a token on the camp's own hex moves it off. Safed's
    /// camp stands on hilly ground by a hex edge, where those rules refuse plots a build without them fills (review #557: on
    /// flat Revivim the test passed with the rules removed). It was Rosh Pinna until main's units file placed Carmeli's
    /// Operation Brosh token beside it (July 1948), whose ring leaves a camp with no date no ground in that hex, so it is
    /// a box there. A camp of 1950 keeps off only the tokens present from its date
    /// (RoshPinnasCampOf1950IsAKitOfTentsAndHutsAgain).</summary>
    [Test]
    public void AMaabaraKeepsOffTokensGridMarksAndSteepGround()
    {
        var map = OpenMap();
        var g = PlacementLayer.CampGround(map);
        var at = Spot(map, "safed");
        g.Tokens.Add((at, 300f));
        var camp = new List<(string id, Vector2 at)> { ("transit_camp_safed", at) };
        var built = SettlementKits.Maabarot(camp, g, out var placed)[0];
        Assert.IsNotNull(built.mesh, "the camp found free ground");
        Object.DestroyImmediate(built.mesh);
        Assert.Greater(placed.Count, 0);
        float lot = 4f * KitScale.Houses;   // a plot's lot and the water tower's (SettlementKits.MaabaraPlotR, WaterTower)
        foreach (var p in placed)
        {
            Assert.IsFalse(g.OnToken(p.C, 0f), p.Kind + " on a unit token");
            if (p.Kind != "tent" && p.Kind != "hut" && p.Kind != "water_tower") continue;
            Assert.IsFalse(g.OnGridMark(p.C, lot), p.Kind + " on a grid mark");
            Assert.LessOrEqual(g.Slope(p.C.x, p.C.y, 600f), 0.12f, p.Kind + " on steep ground");
        }
        SettlementKits.MaabaraGroundRules = false;
        try
        {
            var loose = SettlementKits.Maabarot(camp, g)[0];
            if (loose.mesh != null) Object.DestroyImmediate(loose.mesh);
            TestContext.WriteLine($"{built.tents + built.huts} plots with the rules, {loose.tents + loose.huts} without; "
                + $"{placed.Count(p => p.Kind == "water_tower")} water tower");
            Assert.Greater(loose.tents + loose.huts, built.tents + built.huts, "the grid-mark and slope rules refused a plot");
        }
        finally { SettlementKits.MaabaraGroundRules = true; }
    }

    /// <summary>Review #557: a camp stands in the hex the player built it on, where selection and the info panel point. Over
    /// the settlement hexes each camp's centre and the whole footprint of every structure it places (tents, huts, the
    /// communal hut, the water tower) map back to its own hex, or the camp has no kit and is drawn as its box. Its
    /// tents, huts and water tower keep off the grid marks and steep ground (the water tower too: review #557, should-fix 3).
    /// The camps are built on 26 August 1950, as the game builds them (date-aware tokens, T67), and each kit has the owner's
    /// minimum: SettlementKits.MaabaraMinPlots plots, one of them a hut.</summary>
    [Test]
    public void AMaabaraStandsInItsOwnHex()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var g = PlacementLayer.CampGround(map);
        float lot = 4f * KitScale.Houses;   // a plot's lot and the water tower's (SettlementKits.MaabaraPlotR, WaterTower)
        int kits = 0, towers = 0, structures = 0;
        var boxes = new List<string>();
        foreach (var hex in c.SettlementHexes)
        {
            var at = c.HexAt(hex.Q, hex.R)!; var w = map.Frame.ToWorld(at.Lon, at.Lat, 0);
            var camp = new List<(string id, Vector2 at, System.DateTime from)>
                { ($"{Statecraft.Sim.MapActions.TransitCamp}_{hex.Q}_{hex.R}", new Vector2(w.x, w.z), new System.DateTime(1950, 8, 26)) };
            var built = SettlementKits.Maabarot(camp, g, out var placed)[0];
            if (built.mesh == null) { boxes.Add(hex.Id); continue; }
            kits++;
            Object.DestroyImmediate(built.mesh);
            TestContext.WriteLine($"{hex.Id}: {built.tents} tents, {built.huts} huts, {placed.Count} structures, {(built.at - new Vector2(w.x, w.z)).magnitude:F0} m from the hex centre");
            Assert.GreaterOrEqual(built.tents + built.huts, SettlementKits.MaabaraMinPlots, hex.Id + "'s camp has the owner's minimum of plots");
            Assert.Greater(built.huts, 0, hex.Id + "'s camp has a hut");
            (int, int) HexOf(Vector2 p)
            {
                var (lon, lat) = map.Frame.ToLonLat(new Vector3(p.x, 0f, p.y));
                return HexPicker.FromLonLat(lon, lat, map.Hexes.bounds, map.Hexes.hex_size_km);
            }
            Assert.AreEqual((hex.Q, hex.R), HexOf(built.at), hex.Id + "'s camp");
            // Every structure, not only the centre: at the house scale the rows reach about 1.6 km from it, the communal
            // hut about 3 km and the water tower about 2.6 km, against the hex's 4.33 km inradius (review #557). And the
            // whole of each, not only its centre (review #557, c9): the disc it claims (a plot's lot, which holds a tent or
            // hut with its washing and belongings, and the water tower; the communal hut's half-diagonal), sampled every 30°.
            foreach (var p in placed)
            {
                structures++;
                Assert.AreEqual((hex.Q, hex.R), HexOf(p.C), $"{hex.Id}'s {p.Kind} at ({p.C.x:F0}, {p.C.y:F0}) stands in another hex");
                float r = p.Kind == "communal_hut" ? 0.5f * new Vector2(p.W, p.D).magnitude : lot;
                for (int k = 0; k < 12; k++)
                {
                    var q = p.C + new Vector2(Mathf.Cos(k * Mathf.PI / 6f), Mathf.Sin(k * Mathf.PI / 6f)) * r;
                    Assert.AreEqual((hex.Q, hex.R), HexOf(q), $"{hex.Id}'s {p.Kind} at ({p.C.x:F0}, {p.C.y:F0}) reaches into another hex at {k * 30}°");
                }
            }
            foreach (var p in placed.Where(p => p.Kind == "tent" || p.Kind == "hut" || p.Kind == "water_tower"))
            {
                if (p.Kind == "water_tower") towers++;
                Assert.IsFalse(g.OnGridMark(p.C, lot), $"{hex.Id}'s {p.Kind} on a grid mark");
                Assert.LessOrEqual(g.Slope(p.C.x, p.C.y, 600f), 0.12f, $"{hex.Id}'s {p.Kind} on steep ground");
            }
        }
        TestContext.WriteLine($"{c.SettlementHexes.Count} settlement hexes: {kits} camps drawn as kits ({structures} structures, {towers} water towers), "
            + $"{boxes.Count} as boxes ({string.Join(", ", boxes)})");
        Assert.Greater(kits, 0, "some camp finds ground in its own hex");
    }

    /// <summary>A transit camp the player builds on <paramref name="id"/>'s hex on 26 August 1950, rendered by a placement
    /// layer: whether it is drawn as its kit (Kit.mat) rather than its marker box.</summary>
    static bool DrawnAsKit(MapScene map, Statecraft.Sim.Content c, string id)
    {
        var hex = c.SettlementHexes.Single(s => s.Id == id);
        var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
        layer.Render(new[] { new Statecraft.Sim.Placement(Statecraft.Sim.MapActions.TransitCamp, hex.Q, hex.R, new System.DateTime(1950, 8, 26)) }, c, map);
        bool kit = layer.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        layer.Render(new Statecraft.Sim.Placement[0], c, map);   // frees the camp's mesh
        Object.DestroyImmediate(layer.gameObject);
        return kit;
    }

    /// <summary>The centre of <paramref name="id"/>'s hex on the ground plane (world x, z), where PlacementLayer asks for the
    /// camp the player builds on it.</summary>
    static Vector2 HexCentre(MapScene map, Statecraft.Sim.Content c, string id)
    {
        var hex = c.SettlementHexes.Single(s => s.Id == id);
        var at = c.HexAt(hex.Q, hex.R)!; var w = map.Frame.ToWorld(at.Lon, at.Lat, 0);
        return new Vector2(w.x, w.z);
    }

    /// <summary>T67 (owner, 15 October 2026: "Yes, date-aware"): a camp keeps clear only of the unit tokens present while it
    /// stands, from its own date on. Camps are built from 1950, and Carmeli's Operation Brosh token by Rosh Pinna stands
    /// 9-18 July 1948 only (content/map/units_1948.yaml), so Rosh Pinna's camp of 1950 is a kit of tents and huts again, on
    /// its hex's centre. With no date it keeps off that 1948 token too: then it is a box with the coarse rings, or a few
    /// tents at the hex's far side with the fine ones.</summary>
    [Test]
    public void RoshPinnasCampOf1950IsAKitOfTentsAndHutsAgain()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        Assert.IsTrue(DrawnAsKit(map, c, "rosh_pinna"), "Rosh Pinna's camp of 1950 is drawn as its kit");
        var at = HexCentre(map, c, "rosh_pinna");
        var g = PlacementLayer.CampGround(map);
        var built = SettlementKits.Maabarot(new List<(string id, Vector2 at, System.DateTime from)> { ("transit_camp_rosh_pinna", at, new System.DateTime(1950, 8, 26)) }, g)[0];
        var undated = SettlementKits.Maabarot(new List<(string id, Vector2 at)> { ("transit_camp_rosh_pinna", at) }, g)[0];
        TestContext.WriteLine($"1950: {built.tents} tents, {built.huts} huts, {(built.at - at).magnitude:F0} m from the hex centre; "
            + $"no date: {undated.tents} tents, {undated.huts} huts, {(undated.at - at).magnitude:F0} m");
        foreach (var k in new[] { built, undated }) if (k.mesh != null) Object.DestroyImmediate(k.mesh);
        Assert.Greater(built.tents, 0, "tents"); Assert.Greater(built.huts, 0, "and huts");
        Assert.Less(undated.tents + undated.huts, built.tents + built.huts, "with no date the 1948 token still takes the camp's ground");
    }

    /// <summary>T67: a token present on the camp's date still moves every tent, hut, communal hut and water tower off its ring;
    /// one whose window ended before that date, on the same spot, changes nothing.</summary>
    [Test]
    public void ACampKeepsClearOfTheTokensPresentAtItsDate()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var g = PlacementLayer.CampGround(map);
        var camp = new List<(string id, Vector2 at, System.DateTime from)> { ("transit_camp_rosh_pinna", HexCentre(map, c, "rosh_pinna"), new System.DateTime(1950, 8, 26)) };
        var built = SettlementKits.Maabarot(camp, g)[0];
        Assert.IsNotNull(built.mesh, "Rosh Pinna's camp of 1950 is a kit"); Object.DestroyImmediate(built.mesh);
        float lot = 4f * KitScale.Houses;   // a plot's lot (SettlementKits.MaabaraPlotR)
        var token = built.at + new Vector2(lot, 0f);   // on the camp's own ground
        g.DatedTokens.Add((token, 300f, new System.DateTime(1949, 1, 1)));
        var past = SettlementKits.Maabarot(camp, g)[0];
        Assert.AreEqual((built.tents, built.huts, built.at), (past.tents, past.huts, past.at), "a token gone before the camp's date changes nothing");
        if (past.mesh != null) Object.DestroyImmediate(past.mesh);

        g.DatedTokens.Add((token, 300f, new System.DateTime(1951, 1, 1)));
        var moved = SettlementKits.Maabarot(camp, g, out var clear)[0];
        if (moved.mesh != null) Object.DestroyImmediate(moved.mesh);
        TestContext.WriteLine($"with a token present in 1950: {moved.tents} tents, {moved.huts} huts, {clear.Count} structures");
        Assert.AreNotEqual(built.at, moved.at, "the token present in 1950 moved the camp");
        Assert.Greater(clear.Count, 0, "the camp found other ground");
        foreach (var p in clear)
            Assert.Greater((p.C - token).magnitude, 300f + 0.5f * Mathf.Min(p.W, p.D), $"{p.Kind} at ({p.C.x:F0}, {p.C.y:F0}) on the token present in 1950");
    }

    /// <summary>T67 (issue #32; owner: "Yes, a follow-up"): the ma'abarot stood at the edges of the towns. A camp's centre is
    /// looked for on rings an eighth of its reach apart (450 m at the house scale), not half of it (1.8 km), so a camp finds
    /// the free ground at a town's edge in its hex: Degania Alef, a box with the coarse rings, gets its kit. Every structure
    /// still stands wholly in the hex and off the towns, routes and steep ground (AMaabaraStandsInItsOwnHex,
    /// MaabarotAreSeededAndStandOnFreeDryGround). Gaza's edge has room for 4 tents only, under the owner's minimum, so it
    /// stays its box (ACampIsAtLeastSixPlotsAndAHutOrItsBox).</summary>
    [Test]
    public void ACampFindsTheGroundAtATownsEdge()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        Assert.IsTrue(DrawnAsKit(map, c, "degania_alef"), "Degania Alef's camp is drawn as its kit");
    }

    /// <summary>Owner, 15 October 2026 ("Yes, min 6 plots + a hut"): a ma'abara is at least SettlementKits.MaabaraMinPlots
    /// plots, one of them a hut, or its search goes on further out in its hex, and it is its box only when nowhere in the
    /// hex has room. With the nearest free centre alone, Lod's and Ramla's camps of 1950 were 3 tents and no hut, Gaza's 4
    /// (second review of #646).</summary>
    [Test]
    public void ACampIsAtLeastSixPlotsAndAHutOrItsBox()
    {
        var map = OpenMap();
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var g = PlacementLayer.CampGround(map);
        foreach (var id in new[] { "lod", "ramla", "gaza" })
        {
            var at = HexCentre(map, c, id);
            var hex = c.SettlementHexes.Single(s => s.Id == id);   // the game's seed id (PlacementLayer), so the game's layout
            var built = SettlementKits.Maabarot(new List<(string id, Vector2 at, System.DateTime from)>
                { ($"{Statecraft.Sim.MapActions.TransitCamp}_{hex.Q}_{hex.R}", at, new System.DateTime(1950, 8, 26)) }, g)[0];
            TestContext.WriteLine($"{id}: {(built.mesh == null ? "box" : $"{built.tents} tents, {built.huts} huts, {(built.at - at).magnitude:F0} m from the hex centre")}");
            if (id == "lod") Assert.IsNotNull(built.mesh, "Lod's camp of 1950 is a kit, not its box (second review of #646)");
            if (built.mesh == null) continue;
            Object.DestroyImmediate(built.mesh);
            Assert.GreaterOrEqual(built.tents + built.huts, SettlementKits.MaabaraMinPlots, id + ": at least the minimum of plots");
            Assert.Greater(built.huts, 0, id + ": a hut");
        }
    }

    /// <summary>Review I2: the populations come from content/map/settlement_population_1945.yaml through the map inputs.</summary>
    [Test]
    public void ThePopulationFileLoadsAndRejectsBadRows()
    {
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        var table = KitPopulation.Parse(inputs.PopulationYaml);
        Assert.AreEqual(166660, table["tel_aviv"]); Assert.AreEqual(94310, table["jaffa"]); Assert.AreEqual(5570, table["beersheba"]);
        Assert.IsFalse(table.ContainsKey("revivim"), "a place the source does not list has no figure");
        Assert.AreEqual(23, table.Count);
        OpenMap();
        Assert.AreEqual(166660, KitPopulation.Of("tel_aviv"), "MapScene.Build hands the table to the kits");
        Assert.AreEqual(0, KitPopulation.Of("revivim"));
        const string src = "  sources:\n  - url: https://example.org/x\n    checked: '2026-10-11'\n";
        void Bad(string yaml, string expect)
        {
            var ex = Assert.Throws<System.IO.InvalidDataException>(() => KitPopulation.Parse(yaml, "pop.yaml"));
            StringAssert.Contains("pop.yaml", ex.Message); StringAssert.Contains(expect, ex.Message);
        }
        Bad("settlements:\n- id: a\n  population: -1\n  status: owner-verify\n" + src, "negative");
        Bad("settlements:\n- id: a\n  population: 1\n  status: maybe\n" + src, "status");
        Bad("settlements:\n- id: a\n  population: 1\n  status: owner-verify\n  sources: []\n", "sources");
        Bad("settlements:\n- id: a\n  population: 1\n  status: owner-verify\n" + src + "- id: a\n  population: 2\n  status: owner-verify\n" + src, "duplicate");
        Bad("settlements:\n- id: A\n  population: 1\n  status: owner-verify\n" + src, "snake_case");
        Bad("settlements:\n- id: a\n  people: 1\n", "people");
        // A person's sign-off (#375): both or neither, a GitHub handle, a day.
        Bad("settlements:\n- id: a\n  population: 1\n  status: verified\n  reviewed_by: eyalgolan\n" + src, "reviewed_on");
        Bad("settlements:\n- id: a\n  population: 1\n  status: verified\n  reviewed_by: a@example.org\n  reviewed_on: '2026-10-15'\n" + src, "reviewed_by");
        Assert.AreEqual(1, KitPopulation.Parse("settlements:\n- id: a\n  population: 1\n  status: verified\n  reviewed_by: eyalgolan\n  reviewed_on: '2026-10-15'\n" + src, "pop.yaml").Count);
    }

    [Test]
    public void KitsAndTreesStandOnLand()
    {
        var map = OpenMap();
        var g = Ground(map);
        var root = map.transform.Find(KitLayer.RootName);
        foreach (var s in map.Settlements)
        {
            var b = root.Find(s.Id).GetComponent<MeshRenderer>().bounds;
            Assert.Less(b.size.x, 2.6f * SettlementKits.FootprintRadiusM(s, g.HexSizeM) + 2000f, s.Id + ": the town stays within its footprint");
        }
        var trees = Vegetation.Plant(map.Settlements, g);
        Assert.Greater(trees.Count(t => t.kind == TreeKind.Citrus) + 4 * trees.Count(t => t.kind == TreeKind.CitrusPatch), 1000); Assert.Greater(trees.Count(t => t.kind == TreeKind.Olive), 1000);
        Assert.Greater(trees.Count(t => t.kind == TreeKind.Palm), 50); Assert.Greater(trees.Count(t => t.kind == TreeKind.Cypress), 50);
        int wet = 0, inTown = 0;
        foreach (var (kind, m) in trees)
        {
            Vector3 p = m.GetColumn(3);
            if (g.Water(p.x, p.z)) wet++;
            Assert.AreEqual(g.Y(p.x, p.z), p.y, 0.5f, "rooted on the ground");
            if (kind != TreeKind.CitrusPatch) continue;
            // Every tree of a 2x2 patch, not only its centre, stands on land and outside the towns (review M2).
            float d = Vegetation.GroveStepM * 0.5f;
            foreach (var local in new[] { new Vector3(-d, 0, -d), new Vector3(d, 0, -d), new Vector3(-d, 0, d), new Vector3(d, 0, d) })
            {
                var t = m.MultiplyPoint3x4(local);
                if (g.Water(t.x, t.z)) wet++;
                if (g.InTown(t.x, t.z, 0.85f)) inTown++;
            }
        }
        Assert.AreEqual(0, wet, "trees in the water");
        Assert.AreEqual(0, inTown, "patch trees inside a town");
    }

    /// <summary>Issue #34, vegetation round 2: promo 03's hills carry a loose scatter of single olives and low scrub between
    /// the olive clumps. Over the wet hill country (KitGround.HillCountry, 120-950 m) the map as built has at least
    /// <see cref="HillCrownShare"/> of the ground under an olive crown (planar, before overlaps), about
    /// <see cref="Vegetation.ScrubShare"/> of the scatter low scrub (a squat olive); no scrub crown reaches a town's or
    /// village's footprint or a grove's tree; and the scrub is spread loosely, neither clumped nor in rows: the Clark-Evans
    /// ratio of its nearest-neighbour distances (1 for random points, under 1 clumped, about 2 on a grid) stays within
    /// <see cref="ScrubSpread"/>.</summary>
    [Test]
    public void TheHillsCarryALooseScatterOfOlivesAndScrub()
    {
        var map = OpenMap();
        var g = Ground(map);
        var veg = map.GetComponentInChildren<VegetationLayer>(); Assert.IsNotNull(veg);
        bool Hill(float x, float z) { float e = g.ElevM(x, z); return e >= 120f && e <= 950f && g.HillCountry(x, z); }
        int hillKm2 = 0;
        for (float z = 500f; z < g.Length; z += 1000f) for (float x = 500f; x < g.Width; x += 1000f) if (Hill(x, z)) hillKm2++;
        var olives = veg.Planted().Where(t => t.kind == TreeKind.Olive).Select(t => t.m).Where(m => Hill(m.m03, m.m23)).ToList();
        static bool Squat(Matrix4x4 m) => ((Vector3)m.GetColumn(1)).magnitude < 0.8f * ((Vector3)m.GetColumn(0)).magnitude;
        var scrubM = olives.Where(Squat).ToList();
        var scrub = scrubM.Select(m => new Vector2(m.m03, m.m23)).ToList();
        float per100 = olives.Count * 100f / hillKm2;
        // The share of the hill country under an olive crown, planar and before overlaps (Σ π r² over the area).
        float crownShare = olives.Sum(m => { float r = Vegetation.CrownRadiusM(TreeKind.Olive) * ((Vector3)m.GetColumn(0)).magnitude; return Mathf.PI * r * r; }) / (hillKm2 * 1e6f);
        // Nearest neighbours, footprints and grove trees through a 5 km grid.
        static Dictionary<(int, int), List<Vector2>> Grid(IEnumerable<Vector2> ps)
        {
            var d = new Dictionary<(int, int), List<Vector2>>();
            foreach (var p in ps) { var k = (Mathf.FloorToInt(p.x / 5000f), Mathf.FloorToInt(p.y / 5000f)); if (!d.TryGetValue(k, out var l)) d[k] = l = new List<Vector2>(); l.Add(p); }
            return d;
        }
        static IEnumerable<Vector2> Near(Dictionary<(int, int), List<Vector2>> d, Vector2 p)
        {
            int cx = Mathf.FloorToInt(p.x / 5000f), cz = Mathf.FloorToInt(p.y / 5000f);
            for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) if (d.TryGetValue((cx + dx, cz + dz), out var l)) foreach (var q in l) yield return q;
        }
        var scrubGrid = Grid(scrub);
        double nn = 0; int nnCount = 0;
        foreach (var p in scrub)
        {
            float best = float.MaxValue;
            foreach (var q in Near(scrubGrid, p)) if (q != p) best = Mathf.Min(best, (q - p).magnitude);
            if (best < 5000f) { nn += best; nnCount++; }
        }
        float spread = nnCount == 0 ? 0f : (float)(nn / nnCount) / (0.5f * Mathf.Sqrt(hillKm2 * 1e6f / Mathf.Max(1, scrub.Count)));
        // A crown is its kind's real crown times the instance's scale (NoTreeGrowsThroughABuildingOrLandmark).
        static float Crown(TreeKind k, Matrix4x4 m) => Vegetation.CrownRadiusM(k) * ((Vector3)m.GetColumn(0)).magnitude;
        int inTown = scrubM.Count(m => map.KitFootprints.Any(f => (new Vector2(m.m03, m.m23) - f.centre).magnitude < f.radius + Crown(TreeKind.Olive, m)));
        var groveTrees = veg.Planted().Where(t => t.kind == TreeKind.Citrus || t.kind == TreeKind.CitrusPatch).ToList();
        var groveGrid = Grid(groveTrees.Select(t => new Vector2(t.m.m03, t.m.m23)));
        float groveCrownMax = groveTrees.Count == 0 ? 0f : groveTrees.Max(t => Crown(t.kind, t.m));
        int inGrove = scrubM.Count(m => { var p = new Vector2(m.m03, m.m23); return Near(groveGrid, p).Any(q => (q - p).magnitude < Crown(TreeKind.Olive, m) + groveCrownMax); });
        TestContext.WriteLine($"hill country {hillKm2} km²: {olives.Count} olives ({per100:0.0} per 100 km²), {scrub.Count} of them scrub; " +
                              $"crown area {crownShare:0.000} of the hill country; scrub spread (Clark-Evans) {spread:0.00}; trees {veg.Count}; scrub reaching a footprint {inTown}, a grove tree {inGrove}");
        Assert.GreaterOrEqual(crownShare, HillCrownShare, "olive crown area over the hill country");
        Assert.Greater(scrub.Count, 0.25f * olives.Count * Vegetation.ScrubShare, "low scrub among them");
        Assert.That(spread, Is.InRange(ScrubSpread.x, ScrubSpread.y), "the scrub is spread loosely, not clumped or in rows");
        Assert.AreEqual(0, inTown, "scrub reaching a town's or village's footprint");
        Assert.AreEqual(0, inGrove, "scrub reaching a grove's tree");
    }

    /// <remarks>Measured 2026-10-14 over 11,285 km² of hill country: main 3,480 olives (30.8 per 100 km²), no scrub; with the
    /// scatter 15,165 (134.4), 4,234 of them scrub, Clark-Evans 1.08. Visual gap L1 (15 Oct 2026) makes the hill olives
    /// fewer and larger (Vegetation.HillOliveSize, HillOliveShare), so the floor is now on the crowns' area, not their count:
    /// main 8e45d8dc measured 15,167 olives (134.4 per 100 km²) under 0.292 of the hill country, and L1 10,405 (92.2) under
    /// 0.454 (16 Oct 2026). The floor is about 90% of L1's share, the headroom the old count floor had (120 against 134),
    /// so it fails on main's smaller crowns and on a loss of a tenth of L1's crown area.</remarks>
    public const float HillCrownShare = 0.41f;
    public static readonly Vector2 ScrubSpread = new Vector2(0.8f, 1.3f);

    /// <summary>Visual gap L1 (issue #34): promo 03's hill olives are large rounded crowns. At gameplay03 (top inset off; the
    /// crowns are projected geometry, not a render), every full-size hill olive (an olive on the hills or mountains that is
    /// not squat scrub) whose trunk falls in a window of the east hills has its crown's vertices projected; its on-screen
    /// diameter is their horizontal spread, as a share of the frame's width. The median in each window must fall in promo
    /// 03's band for that frame height.</summary>
    /// <remarks>The windows are the plan's OW1 and OW2 frame heights (top-down y 0.269-0.361 and 0.481-0.620) across the east
    /// hills (x 0.66-0.96). The bands come from 53 crowns hand-marked as circles on promo 03 (2752 px wide), 15 Oct 2026:
    /// PW2 (x 1880-2280, y 420-620, top-down frame y 0.27-0.40) 33 crowns, median 0.97% of the width (IQR 0.92-1.02); PW3
    /// (x 2110-2310, y 740-940, y 0.48-0.61) 20 crowns, median 1.21% (IQR 0.97-1.33). Each band is the median ±15%, cut to
    /// the plan's provisional band (1.0-1.2% and 1.2-1.45%), which the marks may narrow but not move. So the upper band
    /// (1.00-1.12%) sits above PW2's own median (0.97%) by design: the ±15% band (0.82-1.12%) is cut at the provisional
    /// 1.0% floor, and a crown at the promo's exact median would fail it.</remarks>
    public static readonly Vector2 PromoCrownUpper = new Vector2(0.0100f, 0.0112f), PromoCrownMiddle = new Vector2(0.0120f, 0.0139f);

    static readonly Rect EastHillsUpper = Rect.MinMaxRect(0.66f, 1f - 0.361f, 0.96f, 1f - 0.269f);
    static readonly Rect EastHillsMiddle = Rect.MinMaxRect(0.66f, 1f - 0.620f, 0.96f, 1f - 0.481f);

    static bool FullHillOlive(KitGround g, TreeKind k, Matrix4x4 m)
    {
        if (k != TreeKind.Olive || ((Vector3)m.GetColumn(1)).magnitude < 0.8f * ((Vector3)m.GetColumn(0)).magnitude) return false;
        var b = g.Biome(m.m03, m.m23);
        return b == "hills" || b == "mountains";
    }

    [UnityTest]
    public IEnumerator TheHillOlivesMatchPromo03sCrownSize()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var g = Ground(map);
        var veg = map.GetComponentInChildren<VegetationLayer>(); Assert.IsNotNull(veg);
        var olive = Vegetation.Build(TreeKind.Olive); var all = new List<Vector3>();
        olive.Positions(0, olive.VertexCount, all);
        var crown = all.Where(v => v.y > 1.75f).ToList();   // above the trunk's top (Vegetation.Build)
        var rt = new RenderTexture(1920, 1080, 24); var old = cam.targetTexture;
        var upper = new List<float>(); var middle = new List<float>();
        try
        {
            cam.targetTexture = rt;
            Assert.AreEqual(16f / 9f, cam.aspect, 1e-3f, "the frame is 16:9");
            foreach (var (k, m) in veg.Planted())
            {
                if (!FullHillOlive(g, k, m)) continue;
                var at = cam.WorldToViewportPoint(m.GetColumn(3));
                if (at.z <= 0f) continue;
                var w = EastHillsUpper.Contains(at) ? upper : EastHillsMiddle.Contains(at) ? middle : null;
                if (w == null) continue;
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var v in crown) { float x = cam.WorldToViewportPoint(m.MultiplyPoint3x4(v)).x; lo = Mathf.Min(lo, x); hi = Mathf.Max(hi, x); }
                w.Add(hi - lo);
            }
        }
        finally { cam.targetTexture = old; rt.Release(); Object.DestroyImmediate(rt); }
        static float Median(List<float> l) { l.Sort(); return l.Count == 0 ? 0f : l.Count % 2 == 1 ? l[l.Count / 2] : 0.5f * (l[l.Count / 2 - 1] + l[l.Count / 2]); }
        float mu = Median(upper), mm = Median(middle);
        TestContext.WriteLine($"gameplay03 hill olive crowns: upper window {upper.Count} trees, median {mu:P2} of the width (promo {PromoCrownUpper.x:P2}-{PromoCrownUpper.y:P2}); " +
                              $"middle window {middle.Count} trees, median {mm:P2} (promo {PromoCrownMiddle.x:P2}-{PromoCrownMiddle.y:P2})");
        Assert.GreaterOrEqual(upper.Count, 15, "enough hill olives in the upper window"); Assert.GreaterOrEqual(middle.Count, 15, "enough in the middle window");
        Assert.That(mu, Is.InRange(PromoCrownUpper.x, PromoCrownUpper.y), "upper window: hill olive crown size against promo 03");
        Assert.That(mm, Is.InRange(PromoCrownMiddle.x, PromoCrownMiddle.y), "middle window: hill olive crown size against promo 03");
    }

    /// <summary>Visual gap L1 (issue #34): the hill olives cover about as much of the hills as promo 03's do. At gameplay03
    /// (tilt-shift and top inset off), the share of pixels in the plan's windows OW1 and OW2 whose luma changes by more
    /// than 3/255 when the trees are switched off. With shadows off this is the crowns alone, gated against promo 03's
    /// hand-marked crown cover; with shadows on (crowns and their shadows) it is reported.</summary>
    /// <remarks>Promo 03's crown cover, the union of the hand-marked circles (TheHillOlivesMatchPromo03sCrownSize): PW2
    /// 0.234, PW3 0.412, mean 0.323; the cover here is likewise the mean of OW1's and OW2's shares. The band is 0.323 ±15%.
    /// OW1 is x 1530-1730, y 290-390 and OW2 x 1540-1740, y 520-670 (top-down) of 1920 x 1080, fixed by the plan.</remarks>
    public static readonly Vector2 PromoCrownCover = new Vector2(0.275f, 0.371f);

    [UnityTest]
    public IEnumerator TheHillOlivesCoverPromo03sShareOfTheHills()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        RenderSample.TiltShift(false);
        var windows = new[] { new RectInt(1530, 290, 200, 100), new RectInt(1540, 520, 200, 150) };
        (float cover, int[] each) Cover(bool shadows)
        {
            VisualToggles.Set("shadows", shadows);
            VisualToggles.Set("trees", true); var on = RenderSample.Render(cam, 1920, 1080);
            VisualToggles.Set("trees", false); var off = RenderSample.Render(cam, 1920, 1080);
            VisualToggles.Set("trees", true); VisualToggles.Set("shadows", true);
            var each = new int[windows.Length];
            for (int i = 0; i < windows.Length; i++)
            {
                var w = windows[i];
                for (int y = w.yMin; y < w.yMax; y++)
                    for (int x = w.xMin; x < w.xMax; x++)
                    {
                        int row = 1079 - y;   // RenderSample's row 0 is the bottom
                        if (Mathf.Abs(RenderSample.Luma(on.At(x, row)) - RenderSample.Luma(off.At(x, row))) > 3f / 255f) each[i]++;
                    }
            }
            return (0.5f * (each[0] / 20000f + each[1] / 30000f), each);
        }
        var crowns = Cover(false); var withShadows = Cover(true);
        TestContext.WriteLine($"gameplay03 OW1+OW2 cover: crowns {crowns.cover:0.000} (OW1 {crowns.each[0] / 20000f:0.000}, OW2 {crowns.each[1] / 30000f:0.000}; promo {PromoCrownCover.x:0.00}-{PromoCrownCover.y:0.00}), " +
                              $"crowns and shadows {withShadows.cover:0.000} (OW1 {withShadows.each[0] / 20000f:0.000}, OW2 {withShadows.each[1] / 30000f:0.000})");
        Assert.That(crowns.cover, Is.InRange(PromoCrownCover.x, PromoCrownCover.y), "hill olive crown cover against promo 03");
    }

    /// <summary>Visual gap L2 (issue #34; owner, 15 Oct 2026: crown tone "Darker, with pale sunlit tops"): promo 03's hill
    /// olives are dark crowns with pale, sunlit tops. At gameplay03 (tilt-shift and top inset off, shadows on), the crown
    /// pixels are those of the plan's windows OW1 and OW2 that the trees change by more than 3/255 in luma with shadows off
    /// (TheHillOlivesCoverPromo03sShareOfTheHills: the crowns, not their cast shadows), less a pixel at every edge (a pixel
    /// counts only if its four neighbours do), so the ground blended into a crown's rim is left out. Their median luma (the
    /// body), their 90th percentile less the median (the sunlit top against the body) and their median saturation (#772) must
    /// fall in promo 03's bands.</summary>
    /// <remarks>Promo 03's numbers are from the 53 hand-marked crowns of TheHillOlivesMatchPromo03sCrownSize, each circle's
    /// inner 0.8 of the radius (again leaving out the rim), 22,849 pixels, measured 16 Oct 2026: body (median luma) 0.302,
    /// 90th percentile 0.484, top against body 0.181; hue median 28 degrees, saturation 0.32; lit from the upper left (the
    /// crowns' upper halves 0.351 against lower 0.279, left 0.354 against right 0.277). Each band is the promo's value ±15%.
    /// </remarks>
    public static readonly Vector2 PromoCrownBodyLuma = new Vector2(0.257f, 0.347f), PromoCrownTopContrast = new Vector2(0.154f, 0.208f);
    /// <summary>Issue #772: promo 03's crowns read grey olive, saturation 0.322; ±15% as the bands above. #697's crowns
    /// measured 0.41 (chocolate brown) in the WebGL shot.</summary>
    public static readonly Vector2 PromoCrownSaturation = new Vector2(0.274f, 0.370f);

    [UnityTest]
    public IEnumerator TheHillOliveCrownsAreDarkWithPaleSunlitTops()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        RenderSample.TiltShift(false);
        var windows = new[] { new RectInt(1530, 290, 200, 100), new RectInt(1540, 520, 200, 150) };
        VisualToggles.Set("shadows", false);
        VisualToggles.Set("trees", true); var on = RenderSample.Render(cam, 1920, 1080);
        VisualToggles.Set("trees", false); var off = RenderSample.Render(cam, 1920, 1080);
        VisualToggles.Set("trees", true); VisualToggles.Set("shadows", true);
        var look = RenderSample.Render(cam, 1920, 1080);
        bool Crown(int x, int row) => Mathf.Abs(RenderSample.Luma(on.At(x, row)) - RenderSample.Luma(off.At(x, row))) > 3f / 255f;
        var luma = new List<float>(); var hue = new List<float>(); var sat = new List<float>();
        foreach (var w in windows)
            for (int y = w.yMin; y < w.yMax; y++)
                for (int x = w.xMin; x < w.xMax; x++)
                {
                    int row = 1079 - y;   // RenderSample's row 0 is the bottom
                    if (!(Crown(x, row) && Crown(x - 1, row) && Crown(x + 1, row) && Crown(x, row - 1) && Crown(x, row + 1))) continue;
                    var c = look.At(x, row);
                    luma.Add(RenderSample.Luma(c));
                    Color.RGBToHSV(c, out float h, out float s, out _); hue.Add(h * 360f); sat.Add(s);
                }
        static float Pct(List<float> l, float p) { l.Sort(); return l.Count == 0 ? float.NaN : l[Mathf.Clamp(Mathf.RoundToInt(p * (l.Count - 1)), 0, l.Count - 1)]; }
        float body = Pct(luma, 0.5f), top = Pct(luma, 0.9f), contrast = top - body;
        TestContext.WriteLine($"gameplay03 OW1+OW2 olive crowns: {luma.Count} pixels; body (median luma) {body:0.000} (promo {PromoCrownBodyLuma.x:0.000}-{PromoCrownBodyLuma.y:0.000}), " +
                              $"90th percentile {top:0.000}, top against body {contrast:0.000} (promo {PromoCrownTopContrast.x:0.000}-{PromoCrownTopContrast.y:0.000}); " +
                              $"hue median {Pct(hue, 0.5f):0.0} (IQR {Pct(hue, 0.25f):0.0}-{Pct(hue, 0.75f):0.0}; promo 28.2), saturation median {Pct(sat, 0.5f):0.000} (promo {PromoCrownSaturation.x:0.000}-{PromoCrownSaturation.y:0.000})");
        Assert.Greater(luma.Count, 2000, "enough crown pixels in the windows");
        Assert.That(body, Is.InRange(PromoCrownBodyLuma.x, PromoCrownBodyLuma.y), "olive crown body luma against promo 03");
        Assert.That(contrast, Is.InRange(PromoCrownTopContrast.x, PromoCrownTopContrast.y), "olive crown sunlit top against its body, against promo 03");
        Assert.That(Pct(sat, 0.5f), Is.InRange(PromoCrownSaturation.x, PromoCrownSaturation.y), "olive crown saturation against promo 03 (grey olive, not brown)");
    }

    /// <summary>The WebGL checkpoint drew every palm black: a frond's tip quad collapsed to a point, its edge-based normal was
    /// zero and normalize(0) is NaN on WebGL 2 (the tilt-shift then smeared it into black blocks). No part may have one.</summary>
    [Test]
    public void EveryPartHasAUnitNormal()
    {
        foreach (TreeKind k in System.Enum.GetValues(typeof(TreeKind))) Assert.AreEqual(0, Vegetation.Build(k).BadNormals, k.ToString());
        var km = new KitMesh(); var rnd = new KitRandom("parts");
        km.Box(0, 0, 17f, 500f, 300f, -50f, 0f, 400f, new Color32(255, 255, 255, 255), new Color32(200, 200, 200, 255), Facade.WhiteCity, 25f);
        km.Gable(0, 0, 17f, 500f, 300f, 400f, 120f, new Color32(200, 80, 50, 255), new Color32(255, 255, 255, 255));
        km.Pyramid(0, 0, 0f, 100f, 100f, 0f, 80f, new Color32(255, 255, 255, 255));
        km.Dome(0, 0, 0f, 100f, new Color32(255, 255, 255, 255));
        km.Shaft(Vector3.zero, Vector3.up * 300f, 30f, 20f, new Color32(255, 255, 255, 255), 8);
        km.Cone(Vector3.zero, 30f, 40f, new Color32(255, 255, 255, 255), 8);
        km.Crown(Vector3.zero, Vector3.one * 50f, new Color32(40, 80, 30, 255), ref rnd);
        km.Frond(Vector3.zero, Vector3.right, 100f, 25f, 0.7f, new Color32(40, 80, 30, 255));
        Assert.AreEqual(0, km.BadNormals);
    }

    [Test]
    public void TheKitShaderRunsOnWebGL2()
    {
        var shader = Shader.Find("Statecraft/Kit"); Assert.IsNotNull(shader);
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(shader));
        var tree = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tree.mat");
        Assert.AreSame(shader, tree.shader); Assert.IsTrue(tree.enableInstancing, "the trees are GPU-instanced");
    }

    [UnityTest]
    public IEnumerator TheTreesAreDrawnAtTheGameplayPoseAndThinnedAtTheOverview()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var veg = map.GetComponentInChildren<VegetationLayer>(); Assert.IsNotNull(veg);
        PromoPose.Gameplay03.Apply(rig, map.Frame); RenderSample.Render(cam, 320, 180);
        int near = veg.LastDrawn;
        PromoPose.Diorama02.Apply(rig, map.Frame); RenderSample.Render(cam, 320, 180);
        int far = veg.LastDrawn;
        TestContext.WriteLine($"trees {veg.Count}: drawn at gameplay03 {near}, at diorama02 {far}");
        Assert.Greater(near, 1000); Assert.Less(far, veg.Count * 0.6f, "the overview thins the far tiles");
        VisualToggles.Set("trees", false); RenderSample.Render(cam, 320, 180);
        Assert.AreEqual(far, veg.LastDrawn, "the trees toggle stops the draws (no new submission)");
    }

    /// <summary>Review M5: the hex grid stays readable through the towns and groves at gameplay03. Midpoints of the land
    /// hex edges in view; at each, the line shows when switching the grid off changes the 3x3 neighbourhood's luma by more
    /// than 3/255 (HexGridTests' threshold). With kits and trees on, at least <see cref="GridReadableShare"/> of the
    /// midpoints whose line shows on bare ground must still show it (the rest are covered by a building or a crown).</summary>
    /// <remarks>Measured 84% (76 of 90) with the 25 game settlements alone; 78% (69 of 89) with the 272 localities of 1948
    /// and main's routes (2026-10-11); 79% (70 of 89) after #250's units, with the localities' houses and tents off the edge
    /// midpoints (KitGround.OnGridMark, 2026-10-12).</remarks>
    public const float GridReadableShare = 0.75f;

    /// <summary>Visual gap L1's grid guard (G4; owner, 15 Oct 2026: "Yes, add the guard now"): no olive's crown, clump or
    /// scatter, reaches the midpoint of an edge of its hex (KitGround.OnGridMark), as the 1948 villages' houses and the
    /// camps' tents already keep off them, so the larger hill crowns never bury the grid where it is easiest to see.</summary>
    [Test]
    public void NoOliveCrownCoversAHexEdgeMidpoint()
    {
        var map = OpenMap();
        var g = Ground(map);
        var olives = Vegetation.Plant(map.Settlements, g).Where(t => t.kind == TreeKind.Olive).ToList();
        int onMark = olives.Count(t => g.OnGridMark(new Vector2(t.m.m03, t.m.m23), Vegetation.CrownRadiusM(TreeKind.Olive) * ((Vector3)t.m.GetColumn(0)).magnitude));
        TestContext.WriteLine($"{olives.Count} olives, {onMark} with a crown over a hex-edge midpoint");
        Assert.Greater(olives.Count, 1000);
        Assert.AreEqual(0, onMark, "olive crowns over a hex-edge midpoint");
    }

    [UnityTest]
    public IEnumerator TheGridStaysReadableThroughTownsAndGroves()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        RenderSample.TiltShift(false);
        float size = map.Hexes.hex_size_km * 1000f;
        var origin = map.Terrain.transform.position; var g = Ground(map);
        var look = PromoPose.Gameplay03; var c = map.Frame.ToWorld(look.Lon, look.Lat, 0);
        var mids = new List<Vector3>(); var seen = new HashSet<(int, int)>();
        foreach (var h in map.Hexes.hexes)
        {
            if (!h.land) continue;
            var hc = HexGrid.Centre(h.q, h.r, size);
            for (int i = 0; i < 6; i++)
            {
                var m2 = (HexMath.Corner(hc, size, i) + HexMath.Corner(hc, size, (i + 1) % 6)) * 0.5f;
                var w = HexGrid.ToWorld(m2, origin, map.Frame.LengthM);
                if (Mathf.Abs(w.x - c.x) > 30000f || Mathf.Abs(w.z - c.z) > 30000f || g.Water(w.x, w.z)) continue;
                if (!seen.Add((Mathf.RoundToInt(w.x / 10f), Mathf.RoundToInt(w.z / 10f)))) continue;
                w.y = g.Y(w.x, w.z); mids.Add(w);
            }
        }
        var px = new Vector2[mids.Count];
        bool[] Shows(bool kits)
        {
            VisualToggles.Set("kits", kits); VisualToggles.Set("trees", kits);
            var on = RenderSample.Render(cam, 1920, 1080, mids.ToArray(), px);
            VisualToggles.Set("grid", false);
            var off = RenderSample.Render(cam, 1920, 1080, mids.ToArray(), px);
            VisualToggles.Set("grid", true);
            var shows = new bool[mids.Count];
            for (int i = 0; i < mids.Count; i++)
            {
                int x = (int)px[i].x, y = (int)px[i].y;
                if (x < 2 || y < 2 || x > 1917 || y > 1077) continue;
                float change = 0f;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        change = Mathf.Max(change, Mathf.Abs(RenderSample.Luma(on.At(x + dx, y + dy)) - RenderSample.Luma(off.At(x + dx, y + dy))));
                shows[i] = change > 0.012f;
            }
            return shows;
        }
        var bare = Shows(false); var built = Shows(true);
        int baseCount = bare.Count(b => b), kept = Enumerable.Range(0, mids.Count).Count(i => bare[i] && built[i]);
        float share = kept / (float)Mathf.Max(1, baseCount);
        TestContext.WriteLine($"gameplay03: {mids.Count} land edge midpoints in range, line shows at {baseCount} on bare ground, {kept} of them with kits and trees on ({share:P0})");
        Assert.Greater(baseCount, 50, "enough edges in view");
        Assert.GreaterOrEqual(share, GridReadableShare, "the grid is buried under the kits");
    }

    /// <summary>Issue #37 (review I3): with the sun's own URP bias, unchanged, a kit's shadow starts at its walls and its lit
    /// faces show no acne. A lone kit box on the open coastal plain at the hero (12 km), gameplay (60 km) and overview
    /// (250 km) zooms, on both tiers. Walking out from the shadow-side wall along the sun's travel, from 1 m in 1 m steps
    /// to 20 m and then in steps of a twentieth of a shadow texel, the gap is the distance to the first ground sample that
    /// is clearly in shadow (darker, shadows on against off, by 60% of the deep shadow's darkening); it must stay within one
    /// screen pixel, so no lit strip can show. The sunlit roof must not darken with shadows on (acne). The old
    /// marker material (URP Lit) is measured alongside for the record.</summary>
    [UnityTest]
    public IEnumerator AKitShadowStartsAtItsWalls([Values(0, 1)] int level, [Values(12000f, 60000f, 250000f)] float distance)
    {
        int previous = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(level, false);   // 0 = the web tier (1 cascade, bias 1 / 1), 1 = desktop
        try { yield return Measure(level, distance); }
        finally { QualitySettings.SetQualityLevel(previous, false); }
    }

    IEnumerator Measure(int level, float distance)
    {
        var map = OpenMap();
        yield return null;
        foreach (var n in VisualToggles.Names.Where(n => n != "shadows")) VisualToggles.Set(n, false);
        var g = Ground(map);
        var at = map.Frame.ToWorld(34.70, 31.70, 0);
        float w = distance > 100000f ? 1800f : distance > 30000f ? 600f : 300f, h = w;
        var km = new KitMesh();
        float y = g.Y(at.x, at.z);
        km.Box(at.x, at.z, 0f, w, w, y - 30f, y, h, new Color32(235, 230, 220, 255), new Color32(200, 200, 190, 255), Facade.Plain, 25f);
        var mesh = km.ToMesh("ShadowProbe");
        var kit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        var marker = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat");
        var go = new GameObject("ShadowProbe"); go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = kit;
        try
        {
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            rig.LookAt = at; rig.Distance = distance; rig.Yaw = 0f; rig.TopInset = 0f; rig.Apply();
            var sun = MapLighting.FindSun()!; var fall = sun.transform.forward; fall.y = 0f; fall.Normalize();
            // Distance from the centre to the wall along the fall (the fall crosses the wall facing it).
            float toWall = 0.5f * w / Mathf.Max(Mathf.Abs(fall.x), Mathf.Abs(fall.z));
            float texel = 2f * ShadowFit.Distance(cam, UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset!.cascadeBorder) / 2048f;
            var offsets = new List<float>();
            for (int i = 1; i <= 20; i++) offsets.Add(i);
            for (int i = 1; i <= 40; i++) offsets.Add(20f + texel * i / 20f);
            var pts = new List<Vector3>();
            foreach (var o in offsets) { var p = at + fall * (toWall + o); p.y = g.Y(p.x, p.z); pts.Add(p); }
            var deep = at + fall * (toWall + h * 1.2f); deep.y = g.Y(deep.x, deep.z); pts.Add(deep);
            // Two roof samples (the sunlit face the camera sees from this pose; the west wall is edge-on to it): centre and
            // near the sunward edge, where self-shadow acne starts first.
            var roof = new Vector3(at.x, y + h, at.z); pts.Add(roof);
            var roofEdge = new Vector3(at.x, y + h, at.z) - fall * (toWall * 0.85f); pts.Add(roofEdge);
            int deepI = offsets.Count, roofI = deepI + 1, wallI = deepI + 2;   // wallI: the roof's sunward edge
            var px = new Vector2[pts.Count];
            float Gap(Material m, out float deepDark, out float wallDark, out float roofDark)
            {
                mr.sharedMaterial = m;
                VisualToggles.Set("shadows", true); var on = RenderSample.Render(cam, 1920, 1080, pts.ToArray(), px);
                VisualToggles.Set("shadows", false); var off = RenderSample.Render(cam, 1920, 1080, pts.ToArray(), px);
                VisualToggles.Set("shadows", true);
                float Dark(int i) => 1f - RenderSample.Luma(on.At((int)px[i].x, (int)px[i].y)) / Mathf.Max(1e-4f, RenderSample.Luma(off.At((int)px[i].x, (int)px[i].y)));
                deepDark = Dark(deepI); wallDark = Dark(wallI); roofDark = Dark(roofI);
                for (int i = 0; i < offsets.Count; i++) if (Dark(i) > 0.6f * deepDark) return offsets[i];
                return float.PositiveInfinity;
            }
            float kitGap = Gap(kit, out var kitDeep, out var kitWall, out var kitRoof);
            float markerGap = Gap(marker, out var markerDeep, out _, out _);
            float metresPerPixel = 2f * distance * Mathf.Tan(CameraRig.Fov * 0.5f * Mathf.Deg2Rad) / 1080f;
            TestContext.WriteLine($"tier {QualitySettings.names[level]}, {distance / 1000f:0} km, {w:0} m box, shadow texel {texel:0} m, pixel {metresPerPixel:0.0} m: " +
                $"first shadowed sample {kitGap:0} m from the wall with Kit.mat (sun's bias), {markerGap:0} m with the marker material; deep shadow darker by " +
                $"{kitDeep:P0} / {markerDeep:P0}; with shadows on the roof darkens {kitRoof:P1} at its centre and {kitWall:P1} by its sunward edge");
            Assert.Greater(kitDeep, 0.15f, "the box casts a shadow at all");
            Assert.LessOrEqual(kitGap, metresPerPixel + 1f, "no lit strip between the wall and its shadow");
            Assert.Less(kitRoof, 0.05f, "acne on the roof"); Assert.Less(kitWall, 0.05f, "acne by the roof's sunward edge");
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mesh); }
    }

    /// <summary>A kit face in shadow is a darker version of its own colour, not blue-grey. A pale canvas kit box on the open
    /// coastal plain at 12 km, web tier, seen from its shaded side: the wall facing away from the sun is lit by the ambient
    /// alone, the roof by the sun. With the trilight's own colour (near grey at a wall) a pale face in shade kept almost no
    /// colour, and the grade's teal shadows turned it green-grey: measured before the fix, hue 69 deg against the roof's
    /// 38, green over red. Graded, as the player sees it (the other switches off): the shaded wall must keep its hue within
    /// <see cref="ShadeHueDegrees"/> of the sunlit roof's, red its strongest channel, and at least 0.8 of its saturation.</summary>
    public const float ShadeHueDegrees = 15f;

    [UnityTest]
    public IEnumerator AKitFaceInShadowKeepsItsOwnHue()
    {
        int previous = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(0, false);
        var map = OpenMap();
        yield return null;
        foreach (var n in VisualToggles.Names.Where(n => n != "shadows" && n != "grade")) VisualToggles.Set(n, false);
        var g = Ground(map);
        var at = map.Frame.ToWorld(34.70, 31.70, 0);
        var sun = MapLighting.FindSun()!; var fall = sun.transform.forward; fall.y = 0f; fall.Normalize();
        const float w = 300f;
        float toWall = 0.5f * w / Mathf.Max(Mathf.Abs(fall.x), Mathf.Abs(fall.z));
        var canvas = new Color32(229, 221, 198, 255);   // a pale warm albedo: canvas, lime wash, the camps' tents
        var km = new KitMesh();
        float y = g.Y(at.x, at.z);
        km.Box(at.x, at.z, 0f, w, w, y - 30f, y, w, canvas, canvas, Facade.Plain, 25f);
        var mesh = km.ToMesh("ShadeProbe");
        var go = new GameObject("ShadeProbe"); go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        try
        {
            // Looking towards the sun, so the wall facing away from it (the fall crosses it) faces the camera.
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            rig.LookAt = at; rig.Distance = 12000f; rig.Yaw = Mathf.Atan2(-fall.x, -fall.z) * Mathf.Rad2Deg; rig.TopInset = 0f; rig.Apply();
            var pts = new[] { at + fall * (toWall * 0.98f) + Vector3.up * (y + 0.5f * w - at.y), new Vector3(at.x, y + w, at.z) };
            var px = new Vector2[2];
            var s = RenderSample.Render(cam, 1920, 1080, pts, px);
            Color shade = s.Mean((int)px[0].x, (int)px[0].y, 2), lit = s.Mean((int)px[1].x, (int)px[1].y, 2);
            Color.RGBToHSV(shade, out var hs, out var ss, out var vs); Color.RGBToHSV(lit, out var hl, out var sl, out var vl);
            TestContext.WriteLine($"sunlit roof rgb ({lit.r:0.000}, {lit.g:0.000}, {lit.b:0.000}) hue {hl * 360f:0} deg sat {sl:0.00} val {vl:0.00}; " +
                $"shaded wall rgb ({shade.r:0.000}, {shade.g:0.000}, {shade.b:0.000}) hue {hs * 360f:0} deg sat {ss:0.00} val {vs:0.00}");
            Assert.Less(vs, 0.8f * vl, "the wall is in shade");
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(hs * 360f, hl * 360f)), ShadeHueDegrees, "the shaded face turns another hue");
            Assert.Greater(shade.r, Mathf.Max(shade.g, shade.b), "the shaded face of a warm albedo goes green- or blue-grey");
            // The hue alone stops failing once the grade's shadows turn slate (#519); the washed-out colour still shows here.
            Assert.GreaterOrEqual(ss, 0.8f * sl, "the shaded face keeps its colour");
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mesh); QualitySettings.SetQualityLevel(previous, false); }
    }
}
