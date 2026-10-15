#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Towns round 2 (docs/visual-v1.md, "Towns round 2"): a census of what each town's kit places (buildings,
/// landmarks, the share of its land the buildings cover, heights and wall colours), logged as one CENSUS line per town.</summary>
public class TownCensusTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        KitScale.Reset();
    }

    internal sealed class Row
    {
        public string Id = ""; public int Buildings, Wings, Palms; public List<string> Landmarks = new List<string>();
        public float Radius, Coverage, CoreCoverage; public int LandCells, CoreCells;
        public float HMin, HMedian, HMax, HSpread; public int Colours; public Color MeanWall;
        public override string ToString() =>
            $"CENSUS {Id} R={Radius:0} buildings={Buildings} wings={Wings} palms={Palms} landmarks={Landmarks.Count}[{string.Join(",", Landmarks)}] " +
            $"coverage={Coverage:P1} core={CoreCoverage:P1} land_cells={LandCells} h_min={HMin:0} h_med={HMedian:0} h_max={HMax:0} " +
            $"h_cv={HSpread:0.00} colours={Colours} wall=({MeanWall.r:0.00},{MeanWall.g:0.00},{MeanWall.b:0.00})";
    }

    /// <summary>The census of one build: per town, from SettlementKits.Build's list of what it placed. Coverage is the
    /// share of the town's own land (its footprint disc, on land, in its cell: the cells the kit may build in) under a
    /// building's or landmark's footprint, sampled every <paramref name="cellM"/> m; "core" is the inner half radius.</summary>
    internal static List<Row> Census(MapScene map, out List<SettlementKits.Placed> placed, float cellM = 100f) => Census(map, out placed, out _, cellM);

    internal static List<Row> Census(MapScene map, out List<SettlementKits.Placed> placed, out KitGround g, float cellM = 100f)
    {
        g = new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);
        // The corridors MapScene.Build gives the kits: the routes and the units' clearings.
        foreach (var c in map.RouteCorridors) g.AddCorridor(c.line, c.halfWidth);
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        // With the 1948 localities, as the map builds them (their landmarks claim their hearts before any town's houses).
        var localities = map.Localities.Select(p => p.Settlement).ToList();
        var built = SettlementKits.Build(map.Settlements, localities, new List<Localities.Area>(), g, out placed);
        foreach (var k in built.Towns) Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) Object.DestroyImmediate(t.mesh);
        var rows = new List<Row>();
        var towns = new HashSet<string>(map.Settlements.Select(t => t.Id));
        foreach (var s in map.Settlements)
        {
            var w = map.Frame.ToWorld(s.Lon, s.Lat, 0); var centre = new Vector2(w.x, w.z);
            float R = SettlementKits.FootprintRadiusM(s, g.HexSizeM);
            // Street palms are counted, but are neither built cover nor landmarks.
            var mine = placed.Where(p => p.Id == s.Id && !Aggregate(p.Kind) && !p.InSea && p.Kind != "palm").ToList();
            var row = new Row { Id = s.Id, Radius = R, Buildings = mine.Count(p => p.Kind == "building"), Wings = mine.Count(p => p.Kind == "wing"),
                Palms = placed.Count(p => p.Id == s.Id && p.Kind == "palm") };
            row.Landmarks = placed.Where(p => p.Id == s.Id && p.Kind != "building" && p.Kind != "wing" && p.Kind != "palm").Select(p => p.Kind).ToList();
            int n = Mathf.CeilToInt(R / cellM);
            int covered = 0, coreCovered = 0;
            for (int j = -n; j <= n; j++)
                for (int i = -n; i <= n; i++)
                {
                    var p = centre + new Vector2(i, j) * cellM;
                    float d = (p - centre).magnitude / R;
                    if (d > 1f || g.Water(p.x, p.y)) continue;
                    bool own = true;
                    // Its cell among the game settlements (a town's houses ignore the 1948 localities' cells).
                    foreach (var f in g.Footprints) if (f.id != s.Id && towns.Contains(f.id) && (p - f.centre).magnitude / f.radius < d) { own = false; break; }
                    if (!own) continue;
                    bool hit = mine.Any(b => Inside(p, b));
                    row.LandCells++; if (hit) covered++;
                    if (d <= 0.5f) { row.CoreCells++; if (hit) coreCovered++; }
                }
            row.Coverage = covered / (float)Mathf.Max(1, row.LandCells);
            row.CoreCoverage = coreCovered / (float)Mathf.Max(1, row.CoreCells);
            var hs = mine.Where(p => p.Kind == "building").Select(p => p.H).OrderBy(h => h).ToList();
            if (hs.Count > 0)
            {
                row.HMin = hs[0]; row.HMax = hs[hs.Count - 1]; row.HMedian = hs[hs.Count / 2];
                float mean = hs.Average(); row.HSpread = Mathf.Sqrt(hs.Average(h => (h - mean) * (h - mean))) / mean;
                var walls = mine.Where(p => p.Kind == "building").Select(p => p.Wall).ToList();
                row.Colours = walls.Select(c => (c.r >> 4, c.g >> 4, c.b >> 4)).Distinct().Count();
                row.MeanWall = new Color((float)walls.Average(c => c.r) / 255f, (float)walls.Average(c => c.g) / 255f, (float)walls.Average(c => c.b) / 255f);
            }
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>A census entry that names a group (the Old City's walls, the German Colony) rather than one footprint.</summary>
    static bool Aggregate(string kind) => kind == "walls" || kind == "german_colony";

    static bool Inside(Vector2 p, SettlementKits.Placed b)
    {
        float a = b.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        var d = p - b.C;
        float u = d.x * ca + d.y * sa, v = -d.x * sa + d.y * ca;
        return Mathf.Abs(u) <= b.W * 0.5f && Mathf.Abs(v) <= b.D * 0.5f;
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    [Test]
    public void TheCensusIsLogged()
    {
        var map = OpenMap();
        var rows = Census(map, out var placed);
        foreach (var r in rows) { TestContext.WriteLine(r); Debug.Log(r); }
        Debug.Log($"CENSUS total buildings={rows.Sum(r => r.Buildings)} landmarks={rows.Sum(r => r.Landmarks.Count)} placed={placed.Count} {map.KitStats}");
        Assert.AreEqual(map.Settlements.Count, rows.Count);
    }

    /// <summary>The same map draws the same towns: every placement, in order, with its size.</summary>
    [Test]
    public void EveryBuildPlacesTheSameTowns()
    {
        var map = OpenMap();
        Census(map, out var a); Census(map, out var b);
        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.AreEqual(a[i].Id, b[i].Id, $"#{i}"); Assert.AreEqual(a[i].Kind, b[i].Kind, $"#{i}");
            Assert.AreEqual(a[i].C, b[i].C, $"#{i} {a[i].Id}"); Assert.AreEqual(a[i].W, b[i].W); Assert.AreEqual(a[i].D, b[i].D); Assert.AreEqual(a[i].H, b[i].H);
        }
    }

    /// <summary>Each city's landmarks are built (docs/visual-v1.md, "Towns round 2"); every other Arab or old town has its
    /// mosque, and every moshava, kibbutz and moshav its water tower.</summary>
    [Test]
    public void EveryTownHasItsLandmarks()
    {
        var map = OpenMap();
        Census(map, out var placed);
        var want = new Dictionary<string, string[]>
        {
            ["tel_aviv"] = new[] { "port", "warehouse" },
            ["jaffa"] = new[] { "mosque", "clock_tower", "church", "lighthouse", "port" },
            ["haifa"] = new[] { "port", "warehouse", "german_colony" },
            ["jerusalem"] = new[] { "walls", "gate", "citadel", "haram", "dome_of_the_rock", "al_aqsa" },
            ["ramla"] = new[] { "mosque", "white_tower" },
        };
        var missing = new List<string>();
        foreach (var s in map.Settlements)
        {
            var kinds = new HashSet<string>(placed.Where(p => p.Id == s.Id).Select(p => p.Kind));
            var expect = want.TryGetValue(s.Id, out var w) ? w
                : SettlementKits.ArchetypeOf(s) == Archetype.Moshava ? new[] { "water_tower" }
                : SettlementKits.ArchetypeOf(s) == Archetype.WhiteCity ? new string[0] : new[] { "mosque" };
            foreach (var k in expect) if (!kinds.Contains(k)) missing.Add($"{s.Id}: {k}");
        }
        CollectionAssert.IsEmpty(missing, "landmarks left out");
        Assert.GreaterOrEqual(placed.Count(p => p.Id == "jerusalem" && p.Kind == "gate"), 3, "most of the Old City's gates stand");
    }

    /// <summary>No building or landmark stands on a railway or a road (their corridors, RouteLayer.Corridors, and the units'
    /// clearings), and none stands in the sea: a building's centre and four corners are dry. Only harbour works (the
    /// breakwaters) stand in the water.</summary>
    [Test]
    public void NoBuildingStandsOnARouteOrInTheSea()
    {
        var map = OpenMap();
        Census(map, out var placed, out var g);
        var bad = new List<string>();
        foreach (var p in placed)
        {
            if (p.InSea || Aggregate(p.Kind)) continue;
            bool building = p.Kind == "building" || p.Kind == "wing";
            float reach = building ? 0.5f * Mathf.Sqrt(p.W * p.W + p.D * p.D) : 0.5f * Mathf.Min(p.W, p.D);
            if (p.Kind == "wing") reach = 0.5f * Mathf.Min(p.W, p.D);   // inside its block's plot, which was checked whole
            if (g.OnRoute(p.C, reach)) bad.Add($"{p.Id} {p.Kind} on a route at {p.C}");
            if (g.Water(p.C.x, p.C.y)) bad.Add($"{p.Id} {p.Kind} in the sea at {p.C}");
            if (!building) continue;
            float a = p.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            for (int i = 0; i < 4; i++)
            {
                float u = ((i & 1) == 0 ? -0.5f : 0.5f) * p.W, v = ((i & 2) == 0 ? -0.5f : 0.5f) * p.D;
                if (g.Water(p.C.x + u * ca - v * sa, p.C.y + u * sa + v * ca)) { bad.Add($"{p.Id} {p.Kind} corner in the sea at {p.C}"); break; }
            }
        }
        TestContext.WriteLine($"{placed.Count} placements checked");
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} placements on a route or in the sea");
    }

    /// <summary>Every town's houses on main before towns round 2 (main a3202ad, with the 1948 localities: this census), the
    /// floor no town may fall below (#317). Wings are not counted.</summary>
    internal static readonly Dictionary<string, int> MainBuildings = new Dictionary<string, int>
    {
        ["acre"] = 2, ["beersheba"] = 5, ["degania_alef"] = 1, ["ein_harod"] = 1, ["gaza"] = 15, ["hadera"] = 1, ["haifa"] = 30,
        ["hanita"] = 2, ["jaffa"] = 15, ["jerusalem"] = 70, ["lod"] = 5, ["nahalal"] = 1, ["nazareth"] = 5, ["netanya"] = 2,
        ["petah_tikva"] = 2, ["ramla"] = 5, ["rehovot"] = 3, ["revivim"] = 3, ["rishon_lezion"] = 2, ["rosh_pinna"] = 0,
        ["safed"] = 5, ["tel_aviv"] = TelAvivFloor, ["tiberias"] = 3, ["yad_mordechai"] = 0, ["zikhron_yaakov"] = 2,
    };

    /// <summary>Floors measured on today's map for the towns whose main count is 0, which floors nothing (review of #570: a
    /// unit's clearing that thinned Rosh Pinna would pass). Main a3202ad drew no house in Rosh Pinna or Yad Mordechai. Since
    /// round 2's small-town fallback they draw 2 and 3 (main 46ab572, 2026-10-15). The rule is MainBuildings': the count,
    /// with no margin. The cities' floors sit 10-15% under their measure, but on 2 or 3 houses that leaves a floor of 1 or
    /// 2, which would still let a clearing take a house. Yad Mordechai's row is the owner's decision on #621 (2026-10-15).</summary>
    internal static readonly Dictionary<string, int> MeasuredBuildings = new Dictionary<string, int> { ["rosh_pinna"] = 2, ["yad_mordechai"] = 3 };

    /// <summary>Review of towns round 2. No town draws fewer houses than main did (#317: on the 100x lattice a small town
    /// has some 7 plots; Tiberias drew 0 against main's 3, Netanya 1 against 2, Beersheba 3 against 5, until the small-town
    /// floor). The beach setback is the Mediterranean's (Degania on the Sea of Galilee), and Nahalal's ring reaches its
    /// lattice's first ring of plots. No block stands taller than promo 03's tallest, eight storeys (60x: 1,584 m).</summary>
    [Test]
    public void SmallTownsKeepTheirHousesAndNoBlockOutgrowsThePromo()
    {
        var map = OpenMap();
        var rows = Census(map, out var placed).ToDictionary(r => r.Id);
        var bad = new List<string>();
        CollectionAssert.AreEquivalent(MainBuildings.Keys, rows.Keys, "the floor lists every game town");
        foreach (var kv in MainBuildings)
        {
            TestContext.WriteLine(rows[kv.Key]);
            if (rows[kv.Key].Buildings < kv.Value) bad.Add($"{kv.Key}: {rows[kv.Key].Buildings} houses, main {kv.Value}");
        }
        foreach (var id in new[] { "tiberias", "rosh_pinna" })
            if (rows[id].Buildings < 1) bad.Add(id + " builds no house");
        foreach (var kv in MeasuredBuildings)
            if (rows[kv.Key].Buildings < kv.Value) bad.Add($"{kv.Key}: {rows[kv.Key].Buildings} houses, measured {kv.Value}");
        float tallest = placed.Where(p => p.Kind == "building").Max(p => p.H);
        if (tallest > 8f * 3.3f * KitScale.CityBlocks + 1f) bad.Add($"a block {tallest:0} m tall, over eight storeys");
        CollectionAssert.IsEmpty(bad);
    }

    /// <summary>No two buildings of one town overlap (review of #339, F1: the small-town fallback tests every lot, #317).
    /// Separating axes on the footprints, 1 m tolerance. Wings are left out: a wing shares its block's plot.</summary>
    [Test]
    public void NoTwoHousesOfATownOverlap()
    {
        var map = OpenMap();
        Census(map, out var placed);
        var b = placed.Where(p => p.Kind == "building").ToList();
        var bad = new List<string>();
        for (int i = 0; i < b.Count; i++)
            for (int j = i + 1; j < b.Count; j++)
                if (b[i].Id == b[j].Id && Overlap(b[i], b[j])) bad.Add($"{b[i].Id} at {b[i].C} and {b[j].C}");
        TestContext.WriteLine($"{b.Count} buildings checked");
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} overlapping pairs");
    }

    static bool Overlap(SettlementKits.Placed a, SettlementKits.Placed b)
    {
        foreach (var yaw in new[] { a.Yaw, b.Yaw })
            foreach (var ax in new[] { 0f, 90f })
            {
                float t = (yaw + ax) * Mathf.Deg2Rad; var n = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                float Half(SettlementKits.Placed p)
                {
                    float r = (p.Yaw - yaw - ax) * Mathf.Deg2Rad;
                    return 0.5f * (p.W * Mathf.Abs(Mathf.Cos(r)) + p.D * Mathf.Abs(Mathf.Sin(r)));
                }
                if (Mathf.Abs(Vector2.Dot(a.C - b.C, n)) >= Half(a) + Half(b) - 1f) return false;
            }
        return true;
    }

    /// <summary>The floors of the four cities' buildings and built cover (share of the town's own land under a building or
    /// landmark, whole footprint and inner half radius). Towns round 2 measured them with the 1948 localities (docs/visual-v1.md,
    /// "Towns round 2"): Tel Aviv 43 / 17.4% / 18.5%, Jaffa 33 / 32.6% / 31.9%, Haifa 45 / 21.0% / 20.6%, Jerusalem 110 /
    /// 22.7% / 24.9%; main before it had 36, 15, 30 and 70 buildings. The building floors sit above main's counts, so a change
    /// that thins a city back to round 1's sparse handful fails here; the cover floors are about 10% under the measure.
    /// Tel Aviv's row was #318's (owner ruling 2026-10-12: 55 / 25.2% / 25.6% drawn, core floor 25%). Since the owner's ruling on
    /// #652 (2026-10-15, "Lower floor to ~30, wider blocks") it is <see cref="TelAvivFloor"/>'s: fewer, larger blocks with
    /// open plots between them; its localities' landmarks still stand at its street crossings.</summary>
    internal static readonly Dictionary<string, (int buildings, float all, float core)> Floors = new Dictionary<string, (int, float, float)>
    {
        ["tel_aviv"] = (TelAvivFloor, 0.245f, 0.21f), ["jaffa"] = (28, 0.29f, 0.285f), ["haifa"] = (40, 0.19f, 0.185f), ["jerusalem"] = (95, 0.20f, 0.22f),
    };

    /// <summary>Tel Aviv's building floor here and in MainBuildings, and its cover floors in Floors (was 50 / 22.5% / 25%
    /// and MainBuildings 36): owner ruling on #652, 2026-10-15, "Lower floor to ~30, wider blocks". Its blocks are about
    /// 1.6-2 times main's width, with open plots for palms (visual gap S2). It draws 27 / 27.2% / 23.7%; the floors sit about
    /// 10% under. The owner then chose 27 wide blocks over narrower ones to reach 30 (ruling on #652, 2026-10-15: "27 wide
    /// blocks (Recommended)"). Every other town's floor is unchanged.</summary>
    internal const int TelAvivFloor = 24;

    /// <summary>#318 (owner ruling 2026-10-12): Tel Aviv's 1948 localities keep their hearts (#291, review M8). Every locality
    /// whose centre lies inside Tel Aviv's footprint and whose kind has a landmark builds it, before any of the city's blocks.</summary>
    [Test]
    public void TelAvivsLocalitiesKeepTheirLandmarks()
    {
        var map = OpenMap();
        Census(map, out var placed, out var g);
        var ta = map.Settlements.First(s => s.Id == "tel_aviv");
        var w = map.Frame.ToWorld(ta.Lon, ta.Lat, 0); var c = new Vector2(w.x, w.z);
        float R = SettlementKits.FootprintRadiusM(ta, g.HexSizeM);
        var bad = new List<string>(); int checkedN = 0;
        foreach (var loc in map.Localities.Select(p => p.Settlement))
        {
            var lw = map.Frame.ToWorld(loc.Lon, loc.Lat, 0);
            if ((new Vector2(lw.x, lw.z) - c).magnitude > R) continue;
            if (Localities.LandmarkOf(loc.Id) == Landmark.None) continue;
            checkedN++;
            int firstBlock = placed.FindIndex(p => p.Id == "tel_aviv" && p.Kind == "building");
            int at = placed.FindIndex(p => p.Id == loc.Id && p.Kind != "building" && p.Kind != "wing");
            if (at < 0) bad.Add(loc.Id + " lost its landmark");
            else if (firstBlock >= 0 && at > firstBlock) bad.Add(loc.Id + "'s landmark came after Tel Aviv's blocks");
        }
        TestContext.WriteLine($"{checkedN} localities inside Tel Aviv's footprint checked");
        Assert.GreaterOrEqual(checkedN, 8, "Sheikh Muwannis, Jammasin, Salama, Ramat Gan, Givatayim, Bnei Brak... lie inside it");
        CollectionAssert.IsEmpty(bad);
    }

    /// <summary>Visual gap S2 (docs/visual-v1.md, "Tel Aviv's White City"; owner ruling on #652): Tel Aviv's blocks stand
    /// four to eight storeys, with the median in promo 03's five to eight and a height spread (cv) of at least
    /// <see cref="TelAvivHeightSpread"/> (main 0.30); its median block is at least <see cref="TelAvivMedianWidthM"/> wide (main
    /// 13.1 m real, about half the promo's on-screen size); it draws at least <see cref="TelAvivPalms"/> street palms, and no
    /// palm's crown reaches one of its blocks or wings.</summary>
    [Test]
    public void TelAvivIsThePromosWhiteCity()
    {
        var map = OpenMap();
        var rows = Census(map, out var placed).ToDictionary(r => r.Id);
        var ta = rows["tel_aviv"];
        TestContext.WriteLine(ta);
        float storey = 3.3f * KitScale.CityBlocks;
        var blocks = placed.Where(p => p.Id == "tel_aviv" && p.Kind == "building").ToList();
        var widths = blocks.Select(b => Mathf.Max(b.W, b.D)).OrderBy(w => w).ToList();
        TestContext.WriteLine($"storeys min {blocks.Min(b => b.H) / storey:0.0} median {ta.HMedian / storey:0.0} max {ta.HMax / storey:0.0}; " +
            $"median width {widths[widths.Count / 2] / KitScale.CityBlocks:0.0} real m; palms {ta.Palms}");
        Assert.GreaterOrEqual(blocks.Min(b => b.H), SettlementKits.TelAvivMinStoreys * storey - 1f, "a block under four storeys");
        Assert.That(ta.HMedian, Is.InRange(5f * storey - 1f, 8f * storey + 1f), "the median block is not five to eight storeys");
        Assert.GreaterOrEqual(widths[widths.Count / 2], TelAvivMedianWidthM * KitScale.CityBlocks, "the median block's width");
        Assert.GreaterOrEqual(ta.Palms, TelAvivPalms, "street palms");
        Assert.GreaterOrEqual(ta.HSpread, TelAvivHeightSpread, "the blocks' height spread (cv)");
        var bad = new List<string>();
        var walls = placed.Where(p => p.Id == "tel_aviv" && (p.Kind == "building" || p.Kind == "wing")).ToList();
        foreach (var palm in placed.Where(p => p.Id == "tel_aviv" && p.Kind == "palm"))
            foreach (var b in walls)
            {
                float a = b.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                var d = palm.C - b.C;
                float u = Mathf.Max(0f, Mathf.Abs(d.x * ca + d.y * sa) - 0.5f * b.W), v = Mathf.Max(0f, Mathf.Abs(-d.x * sa + d.y * ca) - 0.5f * b.D);
                if (u * u + v * v < 0.25f * palm.W * palm.W - 1f) { bad.Add($"a palm at {palm.C} reaches the {b.Kind} at {b.C}"); break; }
            }
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} palms reach a block");
    }

    /// <summary>Floors for TelAvivIsThePromosWhiteCity, about 10% under the measure (docs/visual-v1.md, "Tel Aviv's White
    /// City"): the median block's longer side in real metres (21.9: 1.67 times main's), the street palms (26) and the
    /// height spread (0.31).</summary>
    const float TelAvivMedianWidthM = 20.5f, TelAvivHeightSpread = 0.28f; const int TelAvivPalms = 23;

    [Test]
    public void TheCitiesCoverTheirGround()
    {
        var map = OpenMap();
        var rows = Census(map, out _).ToDictionary(r => r.Id);
        foreach (var kv in Floors)
        {
            var r = rows[kv.Key];
            TestContext.WriteLine(r);
            Assert.GreaterOrEqual(r.Buildings, kv.Value.buildings, kv.Key + ": buildings");
            Assert.GreaterOrEqual(r.Coverage, kv.Value.all, kv.Key + ": built cover of the footprint");
            Assert.GreaterOrEqual(r.CoreCoverage, kv.Value.core, kv.Key + ": built cover of the inner half");
        }
    }
}
