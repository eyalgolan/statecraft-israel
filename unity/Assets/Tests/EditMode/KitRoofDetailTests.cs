#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The kit buildings' roof detail (owner, 3 November 2026: the kit buildings get real geometry, nearer promo 03's
/// model quality; SettlementKits.RoofDetail): roof parapets, Tel Aviv's set-back top storeys and the stair heads' water
/// tanks. The detail moves no building, stays inside each building's close-mesh cut, and keeps the far map's vertices
/// under <see cref="MaxGrowth"/> times the map without it.</summary>
public class KitRoofDetailTests
{
    [TearDown]
    public void Restore()
    {
        SettlementKits.RoofDetail = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        KitScale.Reset();
    }

    /// <summary>The most the roof detail may multiply the far kit's vertices by (towns and locality tiles together).</summary>
    const float MaxGrowth = 2f;

    /// <summary>The towns the PR reports (the White City twice, the old towns twice, and Lod's village-style lattice), each of
    /// which must gain detail.</summary>
    static readonly string[] Reported = { "tel_aviv", "haifa", "jaffa", "jerusalem", "lod" };

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    /// <summary>The kits as TownCensusTests.Census builds them (the routes' and units' corridors, the 1948 localities).</summary>
    static SettlementKits.Result Build(MapScene map, out List<SettlementKits.Placed> placed)
    {
        var g = new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);
        foreach (var c in map.KitRouteCorridors) g.AddCorridor(c.line, c.halfWidth);
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        return SettlementKits.Build(map.Settlements, map.Localities.Select(p => p.Settlement).ToList(), new List<Localities.Area>(), g, out placed);
    }

    static void Destroy(SettlementKits.Result r)
    {
        foreach (var t in r.Towns) Object.DestroyImmediate(t.mesh);
        foreach (var t in r.Tiles) Object.DestroyImmediate(t.mesh);
        foreach (var m in r.Close.Values) Object.DestroyImmediate(m);
    }

    [Test]
    public void RoofDetailMovesNothingStaysInsideItsBuildingsAndKeepsTheBudget()
    {
        var map = OpenMap();
        SettlementKits.RoofDetail = false;
        SettlementKits.Result off; List<SettlementKits.Placed> offPlaced;
        try { off = Build(map, out offPlaced); }
        finally { SettlementKits.RoofDetail = true; }
        var on = Build(map, out var onPlaced);
        try
        {
            // It draws from each building's own stream: every building, landmark and palm is where, as large and as coloured
            // as without it.
            Assert.AreEqual(offPlaced.Count, onPlaced.Count, "census entries");
            for (int i = 0; i < offPlaced.Count; i++)
            {
                SettlementKits.Placed a = offPlaced[i], b = onPlaced[i];
                Assert.IsTrue(a.Id == b.Id && a.Kind == b.Kind && a.C == b.C && a.Yaw == b.Yaw && a.W == b.W && a.D == b.D && a.H == b.H &&
                    a.Wall.Equals(b.Wall), $"census entry {i} ({a.Id} {a.Kind}) moved");
            }
            // It lies inside each building's CutStart/CutEnd span: the close meshes (the kit without its buildings) are unchanged.
            CollectionAssert.AreEquivalent(off.Close.Keys, on.Close.Keys, "close meshes");
            foreach (var kv in off.Close) Assert.AreEqual(kv.Value.vertexCount, on.Close[kv.Key].vertexCount, kv.Key + "'s close mesh");

            var offTowns = off.Towns.ToDictionary(t => t.s.Id, t => t.mesh.vertexCount);
            var onTowns = on.Towns.ToDictionary(t => t.s.Id, t => t.mesh.vertexCount);
            foreach (var kv in offTowns) Assert.GreaterOrEqual(onTowns[kv.Key], kv.Value, kv.Key);
            foreach (var id in Reported)
            {
                TestContext.WriteLine($"ROOF_DETAIL {id} vertices {offTowns[id]} -> {onTowns[id]} (x{onTowns[id] / (float)offTowns[id]:0.00})");
                Assert.Greater(onTowns[id], offTowns[id], id + " gains roof detail");
            }
            int offTiles = off.Tiles.Sum(t => t.mesh.vertexCount), onTiles = on.Tiles.Sum(t => t.mesh.vertexCount);
            int offAll = offTowns.Values.Sum() + offTiles, onAll = onTowns.Values.Sum() + onTiles;
            TestContext.WriteLine($"ROOF_DETAIL localities (tiles) vertices {offTiles} -> {onTiles} (x{onTiles / (float)offTiles:0.00})");
            TestContext.WriteLine($"ROOF_DETAIL far map vertices {offAll} -> {onAll} (x{onAll / (float)offAll:0.00})");
            // What the towns' streams drew, for comparing two versions of the roof code: every census entry's place, size and
            // colour (a later building's numbers move if an earlier one draws more or fewer), and the close meshes' vertices.
            ulong census = 14695981039346656037UL;
            void Mix(int x) => census = (census ^ (uint)x) * 1099511628211UL;
            foreach (var e in onPlaced)
            {
                Mix((int)KitRandom.Fnv1a(e.Id)); Mix((int)KitRandom.Fnv1a(e.Kind));
                foreach (float f in new[] { e.C.x, e.C.y, e.Yaw, e.W, e.D, e.H }) Mix(System.BitConverter.SingleToInt32Bits(f));
                Mix(e.Wall.r | e.Wall.g << 8 | e.Wall.b << 16 | e.Wall.a << 24);
            }
            TestContext.WriteLine($"ROOF_DETAIL census {onPlaced.Count} entries, digest {census:X16}; close meshes {on.Close.Count}, " +
                $"{on.Close.Values.Sum(c => c.vertexCount)} vertices");
            Assert.Greater(onTiles, offTiles, "the villages gain roof detail");
            Assert.LessOrEqual(onAll, MaxGrowth * offAll, "the far map's vertices");
        }
        finally { Destroy(off); Destroy(on); }
    }

    /// <summary>On every set-back Tel Aviv block the stair head stands on the top storey's roof: a sweep of the real specs
    /// (SettlementKits.TelAviv.cs: block 21-24 x 13-15 m, a stepped block's main part 0.6-0.72 of its width; Fits' sizes 1,
    /// 0.72 and 0.5) and of every draw's range ends (set-back 2.5-3.5 m, width share 0.6-0.85, shift +-0.5; stair head
    /// 0.2-0.3 w x 0.3-0.42 d, set off +-0.2 pw and +-0.15 pd). Each draw enters the stair head's reach linearly, so its range
    /// ends bound it; the sizes and set-backs take five steps, as the set-back's cap bends there.</summary>
    [Test]
    public void SetBackStairHeadStandsOnTheTopStorey()
    {
        float m = KitScale.DefaultCityBlocks, eps = 1e-3f * m;
        float[] Steps(float a, float b) => new[] { a, a + 0.25f * (b - a), a + 0.5f * (b - a), a + 0.75f * (b - a), b };
        float[] ends = { 0f, 1f };
        int n = 0;
        foreach (float k in new[] { 1f, 0.72f, 0.5f })
        foreach (float bw in new[] { 21f, 24f })
        foreach (float part in new[] { 1f, 0.6f, 0.72f })
        foreach (float bd in Steps(13f, 15f))
        foreach (float backM in Steps(2.5f, 3.5f))
        foreach (float s in ends) foreach (float sh in ends) foreach (float u in ends) foreach (float v in ends)
        foreach (float rwS in ends) foreach (float rdS in ends)
        {
            float w = bw * part * k * m, d = bd * k * m;
            float share = Mathf.Lerp(0.6f, 0.85f, s), shift = Mathf.Lerp(-0.5f, 0.5f, sh), fu = Mathf.Lerp(-0.2f, 0.2f, u), fv = Mathf.Lerp(-0.15f, 0.15f, v);
            float rw = w * Mathf.Lerp(0.2f, 0.3f, rwS), rd = d * Mathf.Lerp(0.3f, 0.42f, rdS);
            var (pw, pd, pu, pv, su, sv) = SettlementKits.TelAvivRoofLayout(w, d, m, true, backM, share, shift, fu, fv);
            string spec = $"block {bw * part * k:0.##} x {bd * k:0.##} m, set-back {backM:0.##} m: top storey {pw / m:0.##} x {pd / m:0.##} m";
            Assert.LessOrEqual(Mathf.Abs(su - pu) + 0.5f * rw, 0.5f * pw + eps, spec + ", stair head past its side");
            Assert.LessOrEqual(Mathf.Abs(sv - pv) + 0.5f * rd, 0.5f * pd + eps, spec + ", stair head past its front or back");
            // And the top storey stands on the main block's roof.
            Assert.LessOrEqual(Mathf.Abs(pu) + 0.5f * pw, 0.5f * w + eps, spec + ", top storey past the block's side");
            Assert.LessOrEqual(Mathf.Abs(pv) + 0.5f * pd, 0.5f * d + eps, spec + ", top storey past the block's front or back");
            n++;
        }
        Assert.Greater(n, 0, "specs swept");
    }
}
