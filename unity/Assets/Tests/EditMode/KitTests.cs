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
    /// <see cref="HillCrownsPer100Km2"/> olive crowns per 100 km², about <see cref="Vegetation.ScrubShare"/> of the scatter
    /// low scrub (a squat olive); no scrub crown reaches a town's or village's footprint or a grove's tree; and the scrub is
    /// spread loosely, neither clumped nor in rows: the Clark-Evans ratio of its nearest-neighbour distances (1 for random
    /// points, under 1 clumped, about 2 on a grid) stays within <see cref="ScrubSpread"/>.</summary>
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
                              $"scrub spread (Clark-Evans) {spread:0.00}; trees {veg.Count}; scrub reaching a footprint {inTown}, a grove tree {inGrove}");
        Assert.GreaterOrEqual(per100, HillCrownsPer100Km2, "olive crowns over the hill country");
        Assert.Greater(scrub.Count, 0.25f * olives.Count * Vegetation.ScrubShare, "low scrub among them");
        Assert.That(spread, Is.InRange(ScrubSpread.x, ScrubSpread.y), "the scrub is spread loosely, not clumped or in rows");
        Assert.AreEqual(0, inTown, "scrub reaching a town's or village's footprint");
        Assert.AreEqual(0, inGrove, "scrub reaching a grove's tree");
    }

    /// <remarks>Measured 2026-10-14 over 11,285 km² of hill country: main 3,480 olives (30.8 per 100 km²), no scrub; with the
    /// scatter 15,165 (134.4), 4,234 of them scrub, Clark-Evans 1.08.</remarks>
    public const float HillCrownsPer100Km2 = 120f;
    public static readonly Vector2 ScrubSpread = new Vector2(0.8f, 1.3f);

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
}
