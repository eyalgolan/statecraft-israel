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

/// <summary>The owner on v0.1.1 (18 October 2026): "the sizes in 0.1.1 dont make any sense - [Tal Heuberger's] buildings are way
/// bigger then the train. this also makes jaffa's buildings be over tel avivs buildings". Tal Heuberger's building models (#700)
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

    /// <summary>The service train's length on the map (m): Tal Heuberger's locomotive and tender at RouteTrain.Scale times
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

    static List<SettlementKits.ModelInstance> Models(out MapScene map) => Built(out map, out _).Models;

    /// <summary>The map's settlements built; their meshes destroyed, except the close meshes when <paramref name="keepClose"/>
    /// (the caller destroys those).</summary>
    static SettlementKits.Result Built(out MapScene map, out List<SettlementKits.Placed> placed, bool keepClose = false)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
        var g = new KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        foreach (var c in map.KitRouteCorridors) g.AddCorridor(c.line, c.halfWidth);   // as the map builds its kits (#786)
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        var built = SettlementKits.Build(map.Settlements, map.Localities.Select(p => p.Settlement).ToList(), new List<Localities.Area>(), g, out placed);
        foreach (var k in built.Towns) UnityEngine.Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) UnityEngine.Object.DestroyImmediate(t.mesh);
        if (!keepClose) foreach (var c in built.Close.Values) UnityEngine.Object.DestroyImmediate(c);
        return built;
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

    /// <summary>Issue #1032: each kit landmark's real height (m), the tallest part the kit draws for it, read off the kit's
    /// own parts (SettlementKits.Landmarks.cs, .Outpost.cs). The harbour works (low, in the sea), the Haram's
    /// platform (ground, sized in metres to hold the Old City's lots) and the palms (trees) are not buildings and keep the
    /// kit's size.</summary>
    static readonly Dictionary<string, float> LandmarkRealM = new Dictionary<string, float>
    {
        ["mosque"] = 30.5f,            // the minaret's 26 m shaft and its 4.5 m cap
        ["village_mosque"] = 14f,      // the hall's 5.5 m, the minaret's 7.9 m and its parapet
        ["church"] = 32f,              // the nave's 12 m, the belfry's 14 m and its 6 m roof
        ["clock_tower"] = 31.5f,
        ["lighthouse"] = 16.7f,
        ["white_tower"] = 31.2f,       // five storeys of 27 m, the lantern and its dome
        ["citadel"] = 33.5f,           // the minaret and its cap
        ["dome_of_the_rock"] = 25.6f,  // its walls and drum (19 m at 30x) and its dome (11 m at 18x): 768 m at 30x
        ["al_aqsa"] = 37.2f,           // its hall and its dome, 595 m over the platform at 16x
        ["walls"] = 9.9f,              // Suleiman's walls and their parapet at 100x
        ["wall_tower"] = 13f,          // a tower by a gate (the others 11.6 m)
        ["gate"] = 13f,
        ["outpost_yard"] = 10.5f,      // the lookout tower and its cap
    };

    /// <summary>Issue #1032 (follow-up to #1030): the kit landmarks (mosques, domes, church towers, walls, lighthouses, the
    /// Jaffa clock tower) drew up close at the kits' 60x and 100x, so they towered over the 24x house models. Up close every
    /// kit landmark stands at the models' rule, real height x <see cref="SettlementKits.ModelScale"/> (the locomotive's car
    /// over the reference house): between 0.45 and 1.15 of it. A landmark is never drawn bigger up close than far off, so
    /// the floor lets the ones fitted smaller keep their size: the Haram's Al-Aqsa and the citadel (16x and 20x, fitted to
    /// the symbolic Old City) and the village mosques at Tel Aviv's street crossings (#318, about 12x); a scale that
    /// collapsed by half fails. Above 1.15 is the kit's own scale coming back (a mosque's minaret stands on its own ground,
    /// a little above or below the hall's). Every landmark the census records has its close copy.</summary>
    [Test]
    public void EveryKitLandmarkStandsAtTheModelsScaleUpClose()
    {
        var built = Built(out _, out var placed);
        var close = built.CloseLandmarks;
        float scale = SettlementKits.ModelScale;
        Assert.AreEqual(LocoM / SettlementKits.ReferenceHouseM, scale, 0.01f, "the locomotive rule");
        var bad = new List<string>();
        foreach (var l in close)
        {
            if (!LandmarkRealM.TryGetValue(l.Kind, out var real)) { bad.Add($"{l.Id} {l.Kind}: no real height listed"); continue; }
            float k = l.H / (real * scale);
            if (k < 0.45f || k > 1.15f) bad.Add($"{l.Id} {l.Kind}: {l.H:0} m tall up close, {k:0.00} of {real} m x {scale:0}");
        }
        foreach (var kind in LandmarkRealM.Keys)
        {
            if (kind == "walls" || kind == "wall_tower") continue;   // the census records the circuit once, the kit draws it in pieces
            int want = placed.Count(p => p.Kind == kind), got = close.Count(l => l.Kind == kind);
            if (want != got) bad.Add($"{kind}: {want} in the census, {got} up close");
        }
        TestContext.WriteLine(string.Join("; ", close.GroupBy(l => l.Kind).OrderBy(g => g.Key)
            .Select(g => $"{g.Key} x{g.Count()} {g.Max(l => l.H):0} m ({g.Max(l => l.H) / (LandmarkRealM.TryGetValue(g.Key, out var r) ? r : 1f):0.0}x)")));
        CollectionAssert.IsSubsetOf(LandmarkRealM.Keys, close.Select(l => l.Kind).Distinct().ToList(), "every kind of kit landmark is on the map");
        CollectionAssert.IsEmpty(bad.Take(15), $"{bad.Count} landmarks off the models' scale");
    }

    /// <summary>Issue #1032, the owner's review of #1048: the close mesh itself draws each kit landmark at the models' scale,
    /// not only its record. KitMesh.CloseCopy cuts the kit-scale landmark (CutEnd) and draws the shrunk copy instead; without
    /// the cut the 60x or 100x original stands in the close mesh beside its copy and towers over the 24x houses again. So
    /// round each close landmark's anchor no close-mesh vertex stands higher than its copy's top (its ground level Y plus
    /// its height H, 1 m for float rounding), or a neighbouring copy's whose circle meets it. The circle is the copy's
    /// smaller half side, so a neighbour's palm or plinth stays out; a wall piece's circle (its thickness, mid-length) holds
    /// no vertex and is skipped, its towers and gates are checked.</summary>
    [Test]
    public void TheCloseMeshDrawsEveryKitLandmarkAtItsCopysHeight()
    {
        var built = Built(out _, out _, keepClose: true);
        try
        {
            // Every close mesh's vertices, bucketed on a 200 m grid. The close meshes keep no CPU copy (UploadMeshData), so
            // their positions are read back from the vertex buffer.
            const float Cell = 200f;
            var grid = new Dictionary<(int, int), List<Vector3>>();
            foreach (var m in built.Close.Values)
                foreach (var v in Positions(m))
                {
                    var key = (Mathf.FloorToInt(v.x / Cell), Mathf.FloorToInt(v.z / Cell));
                    if (!grid.TryGetValue(key, out var l)) grid[key] = l = new List<Vector3>();
                    l.Add(v);
                }
            var bad = new List<string>();
            int checkedSites = 0;
            foreach (var l in built.CloseLandmarks)
            {
                float r = 0.5f * Mathf.Min(l.W, l.D), top = float.MinValue;
                for (int x = Mathf.FloorToInt((l.C.x - r) / Cell); x <= Mathf.FloorToInt((l.C.x + r) / Cell); x++)
                    for (int z = Mathf.FloorToInt((l.C.y - r) / Cell); z <= Mathf.FloorToInt((l.C.y + r) / Cell); z++)
                        if (grid.TryGetValue((x, z), out var cell))
                            foreach (var v in cell)
                                if ((new Vector2(v.x, v.z) - l.C).sqrMagnitude <= r * r) top = Mathf.Max(top, v.y);
                if (top == float.MinValue) continue;
                checkedSites++;
                // A neighbouring copy whose circle meets this one (a gate beside its tower, on higher ground) may stand higher.
                float limit = built.CloseLandmarks.Where(o => (o.C - l.C).magnitude < r + 0.5f * Mathf.Min(o.W, o.D)).Max(o => o.Y + o.H);
                if (top > limit + 1f) bad.Add($"{l.Id} {l.Kind}: the close mesh stands {top - l.Y:0} m over its ground, its copy {l.H:0} m");
            }
            TestContext.WriteLine($"{checkedSites} of {built.CloseLandmarks.Count} landmark sites checked");
            Assert.Greater(checkedSites, built.CloseLandmarks.Count * 3 / 4, "most landmark sites have close-mesh vertices");
            CollectionAssert.IsEmpty(bad.Take(15), $"{bad.Count} landmarks drawn above their copy in the close mesh");
        }
        finally { foreach (var c in built.Close.Values) UnityEngine.Object.DestroyImmediate(c); }
    }

    /// <summary>A mesh's vertex positions, read back from its vertex buffer when it keeps no CPU copy.</summary>
    static Vector3[] Positions(Mesh m)
    {
        if (m.isReadable) return m.vertices;
        // The readback needs the editor's graphics device (not -nographics) and float32 x 3 positions (SetVertices).
        Assert.AreEqual(UnityEngine.Rendering.VertexAttributeFormat.Float32, m.GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute.Position), m.name);
        Assert.AreEqual(3, m.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.Position), m.name);
        int s = m.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.Position);
        int off = m.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.Position) / 4, stride = m.GetVertexBufferStride(s) / 4;
        using var buf = m.GetVertexBuffer(s);
        var raw = new float[m.vertexCount * stride];
        buf.GetData(raw);
        var p = new Vector3[m.vertexCount];
        for (int i = 0; i < p.Length; i++) p[i] = new Vector3(raw[i * stride + off], raw[i * stride + off + 1], raw[i * stride + off + 2]);
        return p;
    }

    /// <summary>The products of two plot sides of 1 to <see cref="SettlementKits.MaxPerSide"/>: the counts a full grid holds.</summary>
    static readonly HashSet<int> GridCounts = new HashSet<int>(
        Enumerable.Range(1, SettlementKits.MaxPerSide).SelectMany(a => Enumerable.Range(1, SettlementKits.MaxPerSide).Select(b => a * b)));

    /// <summary>Issue #1032: a kit plot's models no longer sit in a regular grid (they read as regular clusters): each is
    /// turned a little off the plot (up to <see cref="SettlementKits.MaxTurnDeg"/>) and moved within its cell, the group is
    /// off the plot's centre, and some cells are left empty (a varied count, a street through the plot). Seeded: two builds
    /// place every model at the same matrix; and no two models of one plot overlap.</summary>
    [Test]
    public void APlotsModelsAreSeededAndNotARegularGrid()
    {
        var a = Models(out _); var b = Models(out _);
        Assert.AreEqual(a.Count, b.Count, "two builds place as many models");
        for (int i = 0; i < a.Count; i++)
            if (a[i].Model != b[i].Model || a[i].M != b[i].M) Assert.Fail($"model {i} ({a[i].Id} {a[i].Model}) differs between two builds");
        var plots = a.GroupBy(m => (m.Id, m.Plot)).Where(g => g.Count() > 1).Select(g => g.ToList()).ToList();
        Assert.Greater(plots.Count, 500);
        int turned = 0, offCentre = 0, uneven = 0;
        var bad = new List<string>();
        foreach (var g in plots)
        {
            var plot = g[0].Plot;
            if (g.Select(m => Mathf.Round(m.Yaw * 10f)).Distinct().Count() > 1) turned++;
            if ((g.Aggregate(Vector2.zero, (s, m) => s + m.C) / g.Count - plot).magnitude > 1f) offCentre++;
            if (!GridCounts.Contains(g.Count)) uneven++;
            for (int i = 0; i < g.Count; i++)
                for (int j = i + 1; j < g.Count; j++)
                    if (Overlap(Box(g[i]).quad, Box(g[j]).quad, 1f)) bad.Add($"{g[i].Id} at {plot}: {g[i].Model} and {g[j].Model} overlap");
        }
        TestContext.WriteLine($"{plots.Count} plots with several models: {turned} turned, {offCentre} off centre, {uneven} with a count no grid holds");
        Assert.Greater(turned, plots.Count / 2, "the models are turned off their plot");
        Assert.Greater(offCentre, plots.Count / 2, "the groups are off their plot's centre");
        Assert.Greater(uneven, plots.Count / 10, "some cells are left empty");
        CollectionAssert.IsEmpty(bad.Take(15), $"{bad.Count} overlapping models within a plot");
    }
}
