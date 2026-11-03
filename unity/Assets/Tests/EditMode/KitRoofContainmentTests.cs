#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>PR #2371's review (3 November 2026): what stands on a kit roof stays inside that roof's parapet. Each test draws
/// one building at a time (SettlementKits.EmitParts) over its Spec's real width and depth range (Pick's: both ends of each and
/// the middle), Fits' sizes 1, 0.72 and 0.5, the yaws 0, 90 and 45 degrees and 40 seeds, and reads the parapet's inner edge
/// off the mesh itself: the rim's top is a level of vertices just above the roof whose positions, in the building's own frame,
/// take exactly four values along it and four across (outer and inner edge each side). Each test also logs the town stream's
/// draws per building (ROOF_RNG), so a change to the roof code can show it draws what it drew before.</summary>
public class KitRoofContainmentTests
{
    [TearDown]
    public void Restore()
    {
        SettlementKits.RoofDetail = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        KitScale.Reset();
    }

    const float Storey = 3.3f;
    /// <summary>The highest parapet any building draws (real m: the White City's and a town house's top, 1.0).</summary>
    const float MaxParapet = 1.0f;
    /// <summary>Anything higher than this above its roof (real m) stands on the roof: a stair head (2.8), its tank or a roof
    /// room (0.85 storey); a White City laundry room (2.4), a dome (at most about 2.2) and the parapets stay below it.</summary>
    const float StandsOn = 2.5f;
    static readonly float[] Shrinks = { 1f, 0.72f, 0.5f };
    static readonly float[] Yaws = { 0f, 90f, 45f };
    const int Seeds = 40;

    delegate void Emit(KitMesh km, Vector2 p, float yaw, SettlementKits.Spec b, ref KitRandom rnd);

    /// <summary>A kind of building: its Spec ranges (real m, Pick's), storeys, scale and how it is drawn.</summary>
    sealed class Kind
    {
        public string Name = ""; public float W0, W1, D0, D1, M; public int[] Storeys = { 1 }; public Emit Draw = null!;
    }

    static KitGround Ground()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);
    }

    static Emit Lattice(SettlementKits.Style st, KitGround g, float m) =>
        (KitMesh km, Vector2 p, float yaw, SettlementKits.Spec b, ref KitRandom rnd) =>
            SettlementKits.EmitParts("test", st, p, yaw, b, 0.3f, g, km, -2f, 0f, m, ref rnd);

    static Emit Locality(Archetype a, KitGround g, float m) =>
        (KitMesh km, Vector2 p, float yaw, SettlementKits.Spec b, ref KitRandom rnd) =>
            SettlementKits.EmitParts(a, p, yaw, b, g, km, -2f, 0f, m, ref rnd);

    /// <summary>One building drawn for every size, yaw and seed, with its roof at y = b.h (the ground at 0).</summary>
    static IEnumerable<(string spec, float m, float roofY, List<Vector3> v, Vector2 p, float yaw, int draws)> Sweep(Kind k)
    {
        foreach (var (wr, dr) in new[] { (0f, 0f), (0f, 1f), (1f, 0f), (1f, 1f), (0.5f, 0.5f) })
        foreach (float shrink in Shrinks)
        foreach (int storeys in k.Storeys)
        foreach (float yaw in Yaws)
        for (int seed = 0; seed < Seeds; seed++)
        {
            float w = Mathf.Lerp(k.W0, k.W1, wr) * shrink, d = Mathf.Lerp(k.D0, k.D1, dr) * shrink, m = k.M;
            var b = new SettlementKits.Spec
            {
                w = w * m, d = d * m, h = storeys * Storey * m, storeys = storeys, lotR = 0.75f * Mathf.Sqrt(w * w + d * d) * m,
            };
            // Each seed its own plot (the detail streams are seeded by the plot's position), a few hundred metres apart.
            var p = new Vector2(1000f + 311f * seed, 1000f + 173f * seed);
            var rnd = new KitRandom($"{k.Name}/{seed}"); var before = rnd;
            var km = new KitMesh();
            k.Draw(km, p, yaw, b, ref rnd);
            var v = new List<Vector3>(); km.Positions(0, km.VertexCount, v);
            yield return ($"{k.Name} {w:0.##} x {d:0.##} m, {storeys} storeys, yaw {yaw}, seed {seed}", m, b.h, v, p, yaw, Draws(before, rnd));
        }
    }

    /// <summary>How many numbers the stream drew between <paramref name="before"/> and <paramref name="after"/>: every draw
    /// (Value, Range, Chance, Bell's two) is one Next, so the count is exact.</summary>
    static int Draws(KitRandom before, KitRandom after)
    {
        ulong target = after.Next();
        for (int k = 0; k < 4096; k++) { var probe = before; if (probe.Next() == target) return k; before.Next(); }
        return -1;
    }

    /// <summary>(u, v): <paramref name="q"/> in the frame of a building at <paramref name="p"/> turned <paramref name="yaw"/>
    /// degrees (as KitMesh.Corners lays a footprint out).</summary>
    static Vector2 Local(Vector3 q, Vector2 p, float yaw)
    {
        float a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a), dx = q.x - p.x, dz = q.z - p.y;
        return new Vector2(dx * ca + dz * sa, -dx * sa + dz * ca);
    }

    static List<float> Distinct(IEnumerable<float> xs, float eps)
    {
        var o = new List<float>();
        foreach (var x in xs.OrderBy(x => x)) if (o.Count == 0 || x - o[o.Count - 1] > eps) o.Add(x);
        return o;
    }

    /// <summary>The inner edge (u0, u1, v0, v1, in the building's frame) of the parapet whose top lies above
    /// <paramref name="y0"/> and no higher than <paramref name="y1"/>, or null without one.</summary>
    static (float u0, float u1, float v0, float v1)? Rim(List<Vector3> v, Vector2 p, float yaw, float y0, float y1, float eps)
    {
        var levels = Distinct(v.Where(q => q.y > y0 + eps && q.y <= y1 + eps).Select(q => q.y), eps);
        foreach (float y in levels)
        {
            var at = v.Where(q => Mathf.Abs(q.y - y) <= eps).Select(q => Local(q, p, yaw)).ToList();
            var us = Distinct(at.Select(l => l.x), eps); var vs = Distinct(at.Select(l => l.y), eps);
            if (us.Count == 4 && vs.Count == 4) return (us[1], us[2], vs[1], vs[2]);
        }
        return null;
    }

    static bool Inside(Vector2 l, (float u0, float u1, float v0, float v1) r, float eps) =>
        l.x >= r.u0 - eps && l.x <= r.u1 + eps && l.y >= r.v0 - eps && l.y <= r.v1 + eps;

    static void Log(string what, List<int> draws) =>
        TestContext.WriteLine($"ROOF_RNG {what}: {draws.Count} buildings, {draws.Sum()} draws, per-building digest " +
            $"{draws.Aggregate(14695981039346656037UL, (h, n) => (h ^ (ulong)(uint)n) * 1099511628211UL):X16}");

    /// <summary>Every stair head (and the water tank on it) of a White City block, Haifa's Hadar's (WhiteBlock, stepped or
    /// not) and a city's fallback block's, stands inside the parapet's inner edge of the roof it stands on.</summary>
    [Test]
    public void EveryStairHeadAndTankStandsInsideItsRoofsParapet()
    {
        var g = Ground();
        float cm = KitScale.CityBlocks;
        var kinds = new[]
        {
            new Kind { Name = "WhiteBlock", W0 = 12f, W1 = 16f, D0 = 9f, D1 = 11.5f, M = cm, Storeys = new[] { 3, 6 },
                Draw = Lattice(SettlementKits.Style.WhiteBlock, g, cm) },
            new Kind { Name = "WhiteCity fallback", W0 = 15f, W1 = 26f, D0 = 11f, D1 = 18f, M = cm, Storeys = new[] { 2, 4 },
                Draw = Locality(Archetype.WhiteCity, g, cm) },
        };
        var bad = new List<string>();
        foreach (var k in kinds)
        {
            int heads = 0; var draws = new List<int>();
            foreach (var (spec, m, roofY, v, p, yaw, n) in Sweep(k))
            {
                draws.Add(n);
                float eps = 0.01f * m;
                var rim = Rim(v, p, yaw, roofY, roofY + MaxParapet * m, eps);
                if (rim == null) { bad.Add(spec + ": no parapet on the roof"); continue; }
                var on = v.Where(q => q.y > roofY + StandsOn * m).ToList();
                if (on.Count == 0) continue;
                heads++;
                var outside = on.Select(q => Local(q, p, yaw)).Where(l => !Inside(l, rim.Value, eps)).ToList();
                if (outside.Count > 0)
                {
                    var worst = outside[0];
                    bad.Add($"{spec}: stair head at ({worst.x / m:0.00}, {worst.y / m:0.00}) m, parapet inside u {rim.Value.u0 / m:0.00}..{rim.Value.u1 / m:0.00}, v {rim.Value.v0 / m:0.00}..{rim.Value.v1 / m:0.00}");
                }
            }
            TestContext.WriteLine($"ROOF_CONTAIN {k.Name}: {draws.Count} blocks, {heads} stair heads checked");
            Log(k.Name, draws);
            Assert.Greater(heads, 0, k.Name + ": stair heads swept");
        }
        TestContext.WriteLine($"ROOF_CONTAIN stair heads or tanks outside the parapet's inner edge: {bad.Count}");
        foreach (var s in bad.Take(12)) TestContext.WriteLine("  " + s);
        Assert.IsEmpty(bad, "stair heads or tanks outside their roof's parapet");
    }

    /// <summary>A stone or mud-brick house's roof room (StoneHouse: the old towns', the Arab towns' and villages') never cuts
    /// a parapet: a roof with a room has no rim the room's walls stand through, and the rim goes on the room, the topmost
    /// roof (a town house always has one there unless it is domed).</summary>
    [Test]
    public void NoRoofRoomCutsTheRim()
    {
        var g = Ground();
        float hm = KitScale.Houses;
        var kinds = new[]
        {
            new Kind { Name = "OldHouse", W0 = 4.6f, W1 = 6.6f, D0 = 4.4f, D1 = 6.2f, M = hm, Storeys = new[] { 1, 2 },
                Draw = Lattice(SettlementKits.Style.OldHouse, g, hm) },
            new Kind { Name = "Village", W0 = 3.8f, W1 = 5.4f, D0 = 3.6f, D1 = 5.0f, M = hm, Storeys = new[] { 1, 2 },
                Draw = Lattice(SettlementKits.Style.Village, g, hm) },
            new Kind { Name = "OldTown fallback", W0 = 7f, W1 = 14f, D0 = 7f, D1 = 12f, M = hm, Storeys = new[] { 1, 2 },
                Draw = Locality(Archetype.OldTown, g, hm) },
            new Kind { Name = "ArabTown fallback", W0 = 6.5f, W1 = 12f, D0 = 6f, D1 = 10f, M = hm, Storeys = new[] { 1, 2 },
                Draw = Locality(Archetype.ArabTown, g, hm) },
        };
        var bad = new List<string>();
        int townRoomsWithRim = 0;
        foreach (var k in kinds)
        {
            int rooms = 0, underRim = 0, ownRim = 0; var draws = new List<int>();
            foreach (var (spec, m, roofY, v, p, yaw, n) in Sweep(k))
            {
                draws.Add(n);
                float eps = 0.01f * m, roomTop = roofY + 0.85f * Storey * m;
                var on = v.Where(q => q.y > roofY + StandsOn * m).ToList();
                if (on.Count == 0) continue;
                rooms++;
                var rim = Rim(v, p, yaw, roofY, roofY + MaxParapet * m, eps);
                if (rim != null)
                {
                    underRim++;
                    if (on.Any(q => !Inside(Local(q, p, yaw), rim.Value, eps)))
                        bad.Add($"{spec}: the roof room stands through the parapet (inner edge u {rim.Value.u0 / m:0.00}..{rim.Value.u1 / m:0.00}, v {rim.Value.v0 / m:0.00}..{rim.Value.v1 / m:0.00} m)");
                }
                if (Rim(v, p, yaw, roomTop, roomTop + MaxParapet * m, eps) != null) ownRim++;
            }
            if (k.Name == "OldHouse") townRoomsWithRim = ownRim;
            TestContext.WriteLine($"ROOF_CONTAIN {k.Name}: {draws.Count} houses, {rooms} roof rooms, {underRim} on a roof with a parapet, {ownRim} with their own parapet");
            Log(k.Name, draws);
            Assert.Greater(rooms, 0, k.Name + ": roof rooms swept");
        }
        TestContext.WriteLine($"ROOF_CONTAIN roof rooms through a parapet: {bad.Count}");
        foreach (var s in bad.Take(12)) TestContext.WriteLine("  " + s);
        Assert.IsEmpty(bad, "roof rooms through a parapet");
        Assert.Greater(townRoomsWithRim, 0, "an old town house's roof room carries the parapet");
    }
}
