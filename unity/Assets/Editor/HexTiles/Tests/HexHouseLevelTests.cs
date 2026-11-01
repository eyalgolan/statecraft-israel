#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEngine;

/// <summary>#1901: the lighter level of Tal Heuberger's generated town houses (HexHouseLevels), which the Tiberias basalt
/// houses draw. Three promises: the boards keep their triangle budgets with it (counted as HexPrototypeTests counts them),
/// Tal's delivered files stay byte for byte as delivered, and the lighter mesh keeps their seams and texture (every vertex
/// one of their own, exact; no seam or edge opened; the corners where three UV charts meet all kept; no new see-through).</summary>
public class HexHouseLevelTests
{
    const string Folder = "Assets/Art/Settlements/Prototype/Models/", BasaltHouse = "tiles:tiberias_town/basalt_house";
    static readonly string[] Houses =
    {
        "generated-1948-01-netanya-tiled-house-original", "generated-1948-02-netanya-two-storey-balcony-house",
        "generated-1948-03-netanya-flat-roof-porch-house", "generated-1948-04-dark-basalt-house",
        "generated-1948-05-plastered-courtyard-house-original",
    };
    static string Stem(string id) => Folder + id + "/" + id;
    static string Light(string id) => Stem(id) + "-light";

    [Serializable] sealed class Provenance { public string fbx_sha256 = "", embedded_image_sha256 = ""; }
    [Serializable] sealed class Record { public string source = "", source_sha256 = ""; public int source_triangles, triangles; }

    static string Disk(string assetPath) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    static string Hash(string assetPath)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Disk(assetPath)))).Replace("-", "").ToLowerInvariant();
    }

    static Mesh SourceMesh(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(Stem(id) + ".fbx").GetComponentInChildren<MeshFilter>().sharedMesh;

    [TestCase("israel", "tiberias_town")]
    [TestCase("israel", "tel_aviv_city")]
    [TestCase("tel_aviv", "tel_aviv_city")]
    public void TheBoardTileKeepsItsBudgetWithTheLighterHouses(string boardId, string prototype)
    {
        var board = HexBoard.Load(boardId);
        var surface = new HexBoardSurface(board);
        var originals = HexPrototypeTests.OriginalMeshPaths();
        int hexes = 0, basalt = 0;
        foreach (var h in board.Hexes.Values.Where(h => h.prototype == prototype))
        {
            var proto = HexPrototypes.Create(prototype);
            var ctx = new HexTileContext(h.q, h.r, surface, 1948);
            proto.Build(ctx);
            int tris = ctx.Placements.Sum(p => p.StandIn == StandIn.Flat ? 12 : HexPrototypeTests.CountTowardsCap(
                HexPrototypeTests.Meshes(HexModels.Prefab(HexModels.Resolve(p.Model, p.Position))),
                HexModels.Triangles(p.Model, p.StandIn, p.Position), originals));
            foreach (var p in ctx.Placements.Where(p => p.Model == BasaltHouse))
            {
                Assert.AreEqual(Light("generated-1948-04-dark-basalt-house") + ".prefab", AssetDatabase.GetAssetPath(HexModels.Prefab(p.Model)), "the basalt houses draw house 04's lighter level");
                basalt++;
            }
            TestContext.WriteLine($"{boardId} {prototype} ({h.q},{h.r}): {tris} of {proto.TriangleBudget} triangles, {basalt} basalt houses");
            Assert.That(tris, Is.LessThanOrEqualTo(proto.TriangleBudget), $"{boardId} {prototype} ({h.q},{h.r})");
            hexes++;
        }
        Assert.That(hexes, Is.GreaterThan(0), $"board {boardId} has no {prototype}");
        if (prototype == "tiberias_town") Assert.That(basalt, Is.GreaterThan(0), "Tiberias placed no basalt house");
    }

    [TestCaseSource(nameof(Houses))]
    public void TalsDeliveredFilesAreUntouchedAndTheRecordNamesThem(string id)
    {
        var provenance = JsonUtility.FromJson<Provenance>(File.ReadAllText(Disk(Folder + id + "/provenance.json")));
        string fbx = Stem(id) + ".fbx";
        Assert.AreEqual(provenance.fbx_sha256, Hash(fbx), fbx);
        Assert.AreEqual(provenance.embedded_image_sha256, Hash(Stem(id) + "_basecolor.jpg"), id + " colour map");
        var record = JsonUtility.FromJson<Record>(File.ReadAllText(Disk(Light(id) + ".json")));
        Assert.AreEqual(fbx, record.source, id);
        Assert.AreEqual(provenance.fbx_sha256, record.source_sha256, id + ": the lighter level was made from the delivered FBX");
        Assert.AreEqual(SourceMesh(id).GetIndexCount(0) / 3, record.source_triangles, id);
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Mesh>(Light(id) + ".asset").triangles.Length / 3, record.triangles, id);
    }

    [TestCaseSource(nameof(Houses))]
    public void TheLighterLevelKeepsTheSeamsAndTheTexture(string id)
    {
        var light = AssetDatabase.LoadAssetAtPath<Mesh>(Light(id) + ".asset");
        Assert.IsNotNull(light, id + ": no lighter level");
        KeepsTheSeamsAndTheTexture(id, SourceMesh(id), light);

        // No new see-through (the hex tiles' wholeness check, HexModelMeshTests).
        var full = HexMeshCheck.Of(AssetDatabase.LoadAssetAtPath<GameObject>(Stem(id) + "-stream.prefab"));
        var lighter = HexMeshCheck.Of(AssetDatabase.LoadAssetAtPath<GameObject>(Light(id) + ".prefab"));
        TestContext.WriteLine($"{id}: full {full}; light {lighter}");
        Assert.That(lighter.SeeThroughShare, Is.LessThanOrEqualTo(full.SeeThroughShare + 0.0005f), id);
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Material>(Stem(id) + ".mat"),
            AssetDatabase.LoadAssetAtPath<GameObject>(Light(id) + ".prefab").GetComponentInChildren<MeshRenderer>().sharedMaterial, id + ": Tal's material");
    }

    /// <summary>The checks above test the committed -light files, so a broken HexHouseLevels.Simplify would only show at the
    /// next rebuild (owner, PR #1994, 30 October 2026). This runs it on house 04, the one the Tiberias boards draw, with
    /// Build's settings, and holds the fresh mesh to the same seam and texture promises.</summary>
    [Test]
    public void TheSimplifierKeepsTheSeamsAndTheTextureOfHouse04()
    {
        const string id = "generated-1948-04-dark-basalt-house";
        var source = SourceMesh(id);
        int texels = AssetDatabase.LoadAssetAtPath<Material>(Stem(id) + ".mat").mainTexture.width;
        var light = HexHouseLevels.Simplify(source, HexHouseLevels.UvToleranceTexels / texels, HexHouseLevels.EdgeTolerance, HexHouseLevels.MaxError);
        try { KeepsTheSeamsAndTheTexture(id, source, light); }
        finally { UnityEngine.Object.DestroyImmediate(light); }
    }

    /// <summary>The lighter mesh's promises that need no prefab: fewer triangles, every vertex one of the source's, no seam
    /// or edge opened, the chart corners kept, the texture border within the tolerances.</summary>
    static void KeepsTheSeamsAndTheTexture(string id, Mesh source, Mesh light)
    {
        int sourceTris = source.triangles.Length / 3, lightTris = light.triangles.Length / 3;
        Assert.That(lightTris, Is.LessThan(sourceTris), id);

        // Every vertex is one of Tal's, with its exact position, UV and normal.
        var corners = new HashSet<(Vector3, Vector2, Vector3)>();
        var sv = source.vertices; var suv = source.uv; var sn = source.normals;
        for (int i = 0; i < sv.Length; i++) corners.Add((sv[i], suv[i], sn[i]));
        var lv = light.vertices; var luv = light.uv; var ln = light.normals;
        for (int i = 0; i < lv.Length; i++) Assert.IsTrue(corners.Contains((lv[i], luv[i], ln[i])), $"{id}: vertex {i} is not one of the source's");

        // No new degenerate triangle (Tripo's own zero-area faces stay as delivered).
        var lt = light.triangles;
        var sourceFlat = Flat(sv, source.triangles);
        Assert.That(Flat(lv, lt).Where(f => !sourceFlat.Contains(f)), Is.Empty, id + ": new degenerate triangles");

        // The open (and non-manifold) edges, welded by position, are exactly the source's: no seam or crack opened, the
        // open base and the source's own cracks kept as they are.
        var (sourceOpen, sourceHard) = Edges(sv, source.triangles);
        var (lightOpen, _) = Edges(lv, lt);
        CollectionAssert.AreEquivalent(sourceOpen, lightOpen, id + ": open edges");
        var kept = new HashSet<Vector3>(lv);
        Assert.That(sourceHard.Where(p => !kept.Contains(p)), Is.Empty, id + ": vertices on open or non-manifold edges");

        // The texture's chart corners: every position where three or more UV charts meet is kept.
        var charts = Enumerable.Range(0, sv.Length).GroupBy(i => sv[i]).Where(g => g.Select(i => suv[i]).Distinct().Count() >= 3).Select(g => g.Key);
        Assert.That(charts.Where(p => !kept.Contains(p)), Is.Empty, id + ": positions where three or more charts meet");

        // Every seam position removed (each of its chart copies) lies within the tolerances of a seam segment left in the lighter mesh, in UV (one texel
        // of Tal's colour map) and in 3D at once: the texture border moved by at most that, summed over all the steps.
        var sourcePositions = Enumerable.Range(0, sv.Length).GroupBy(i => sv[i]).ToDictionary(g => g.Key, g => g.Count());
        var seams = SeamSegments(lv, luv, lt);
        int texels = AssetDatabase.LoadAssetAtPath<Material>(Stem(id) + ".mat").mainTexture.width;
        float uvTol = HexHouseLevels.UvToleranceTexels / texels * 1.001f;
        float size = source.bounds.size.magnitude, tol3 = HexHouseLevels.EdgeTolerance * size * 1.001f;
        int removed = 0;
        for (int i = 0; i < sv.Length; i++)
        {
            if (sourcePositions[sv[i]] < 2 || kept.Contains(sv[i])) continue;
            removed++;
            Assert.IsTrue(seams.Any(s => Off(suv[i], s.ua, s.ub) <= uvTol && Off(sv[i], s.pa, s.pb) <= tol3),
                $"{id}: removed seam vertex {i} lies off the border left behind (nearest in UV {seams.Min(s => Off(suv[i], s.ua, s.ub)) * texels:F2} texels, in 3D {seams.Min(s => Off(sv[i], s.pa, s.pb)) / size:P3})");
        }
        TestContext.WriteLine($"{id}: {removed} seam corners removed, each within the tolerances of a kept seam segment");
    }

    /// <summary>Open (one face) and non-manifold (three or more) edges after welding by position, and the positions on them.</summary>
    static (List<(Vector3, Vector3)> open, HashSet<Vector3> hard) Edges(Vector3[] v, int[] t)
    {
        var count = new Dictionary<(Vector3, Vector3), int>();
        for (int i = 0; i < t.Length; i += 3)
            foreach (var (a, b) in new[] { (v[t[i]], v[t[i + 1]]), (v[t[i + 1]], v[t[i + 2]]), (v[t[i + 2]], v[t[i]]) })
            {
                var e = Less(a, b) ? (a, b) : (b, a);
                count[e] = count.TryGetValue(e, out var n) ? n + 1 : 1;
            }
        var hard = new HashSet<Vector3>();
        foreach (var (e, n) in count) if (n != 2) { hard.Add(e.Item1); hard.Add(e.Item2); }
        return (count.Where(kv => kv.Value == 1).Select(kv => kv.Key).ToList(), hard);
    }

    /// <summary>The chart borders that are seams: index edges with one face whose positions have two faces.</summary>
    static List<(Vector2 ua, Vector2 ub, Vector3 pa, Vector3 pb)> SeamSegments(Vector3[] v, Vector2[] uv, int[] t)
    {
        var index = new Dictionary<(int, int), int>();
        for (int i = 0; i < t.Length; i += 3)
            for (int c = 0; c < 3; c++)
            {
                int a = t[i + c], b = t[i + (c + 1) % 3];
                var e = a < b ? (a, b) : (b, a);
                index[e] = index.TryGetValue(e, out var n) ? n + 1 : 1;
            }
        var (open, _) = Edges(v, t);
        var openPositions = new HashSet<(Vector3, Vector3)>(open);
        return index.Where(kv => kv.Value == 1)
            .Where(kv => !openPositions.Contains(Less(v[kv.Key.Item1], v[kv.Key.Item2]) ? (v[kv.Key.Item1], v[kv.Key.Item2]) : (v[kv.Key.Item2], v[kv.Key.Item1])))
            .Select(kv => (uv[kv.Key.Item1], uv[kv.Key.Item2], v[kv.Key.Item1], v[kv.Key.Item2])).ToList();
    }

    static float Off(Vector3 x, Vector3 a, Vector3 b)
    {
        var d = b - a; var w = x - a; float l2 = d.sqrMagnitude;
        return l2 < 1e-20f ? w.magnitude : (w - d * Mathf.Clamp01(Vector3.Dot(w, d) / l2)).magnitude;
    }

    /// <summary>The zero-area triangles, each as its three corner positions in order.</summary>
    static HashSet<(Vector3, Vector3, Vector3)> Flat(Vector3[] v, int[] t)
    {
        var flat = new HashSet<(Vector3, Vector3, Vector3)>();
        for (int i = 0; i < t.Length; i += 3)
            if (Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).sqrMagnitude == 0f) flat.Add((v[t[i]], v[t[i + 1]], v[t[i + 2]]));
        return flat;
    }

    static bool Less(Vector3 a, Vector3 b) => a.x != b.x ? a.x < b.x : a.y != b.y ? a.y < b.y : a.z < b.z;
}
