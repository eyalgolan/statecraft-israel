#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Kits;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEngine;

/// <summary>Buildings must be whole (Tal, 19 October 2026: a Tel Aviv block with its walls missing, grass through its
/// windows). The buildings' materials cull back faces and Tripo's buildings are open underneath, so a view ray that meets
/// a building but none of its front faces shows the ground through it: every building mesh the hex tiles resolve to (ids
/// "sandbox:hq_*", "sandbox:kit_*" and "town:*") keeps that share of its view rays within <see cref="Tolerance"/>
/// (HexMeshCheck.SeeThrough). Open edges are not the test: the shipped b*-v2
/// meshes have thousands (T-junctions, hairline cracks and the open base), so they are reported only. The decimated
/// sandbox kit (planar dissolve) dropped faces; the shipped b*-v2 town models (TownModelSet) are whole. It writes the
/// measurements of every candidate mesh (the local sandbox prefabs and the set's full and mid meshes) to
/// asset-staging/ui-local/hex-tiles/round2/buildings/mesh-report.txt, and checks that each sandbox id drawn as a town
/// model faces the way its sandbox prefab faced (the prototypes turn doors to the street by the sandbox frame).
/// Not caught: a hole whose rays meet nothing behind it (a roof hole over the open base); the close shots
/// (HexModelCapture) are the check for that.</summary>
public class HexModelMeshTests
{
    /// <summary>The share of view rays a resolved building may see through (a few stray inverted faces in a Tripo mesh).</summary>
    const float Tolerance = 0.005f;
    /// <summary>Shipped models allowed a little more, each for a reason seen in its HexModelCapture sheet: thin slivers
    /// between balcony slabs, not a missing wall (Tal chose the full meshes on 19 October 2026 knowing of them).</summary>
    static readonly Dictionary<string, float> Slivers = new Dictionary<string, float>(System.StringComparer.Ordinal)
    {
        ["b02a-tel-aviv-corner-block-v2"] = 0.0065f,   // 0.55%: between the balcony slabs
        ["b13a-netanya-two-storey-v2"] = 0.008f,       // 0.69%: under the balcony slabs
    };

    static float ToleranceFor(GameObject drawn) => Slivers.TryGetValue(drawn.name, out var t) ? t : Tolerance;
    /// <summary>Another turn must lie this much nearer than the drawn one to count as a wrong facing.</summary>
    const float FacingMargin = 0.8f;

    static bool Building(string id) => id.StartsWith("sandbox:hq_") || id.StartsWith("sandbox:kit_") || id.StartsWith(HexModels.TownPrefix);

    /// <summary>Every building id a registered prototype places, with the concrete ids it resolves to there.</summary>
    static IEnumerable<string> ResolvedBuildings()
    {
        var ids = new SortedSet<string>(System.StringComparer.Ordinal);
        foreach (var proto in HexPrototypes.All.Keys)
            foreach (var seed in new[] { 1948, 7 })
                foreach (var (q, r) in new[] { (2, -1), (-3, 19), (0, 0) })
                {
                    var ctx = new HexTileContext(q, r, new FlatSurface(), seed);
                    HexPrototypes.Create(proto).Build(ctx);
                    foreach (var p in ctx.Placements) if (Building(p.Model)) ids.Add(HexModels.Resolve(p.Model, p.Position));
                }
        return ids;
    }

    static Mesh Cube() => Resources.GetBuiltinResource<Mesh>("Cube.fbx");

    /// <summary>The check itself: Unity's cube (split vertices, welded here) is whole; without its top face, rays from
    /// above meet the inside of its bottom face (a back face) and see through.</summary>
    [Test]
    public void CheckSeesAMissingFace()
    {
        var whole = HexMeshCheck.Of(new[] { (Cube(), Matrix4x4.identity) });
        Assert.That(whole.SeeThrough, Is.Zero, whole.ToString());
        Assert.That(whole.Open, Is.Zero, whole.ToString());
        var open = Object.Instantiate(Cube());
        try
        {
            var v = open.vertices; var t = open.triangles;
            bool Top(int i) => v[t[3 * i]].y > 0.4f && v[t[3 * i + 1]].y > 0.4f && v[t[3 * i + 2]].y > 0.4f;
            open.triangles = Enumerable.Range(0, t.Length / 3).Where(i => !Top(i)).SelectMany(i => new[] { t[3 * i], t[3 * i + 1], t[3 * i + 2] }).ToArray();
            var m = HexMeshCheck.Of(new[] { (open, Matrix4x4.identity) });
            Assert.That(m.SeeThroughShare, Is.GreaterThan(Tolerance), m.ToString());
            Assert.That(m.OpenOffBase, Is.EqualTo(4), m.ToString());
        }
        finally { Object.DestroyImmediate(open); }
    }

    [Test]
    public void ResolvedBuildingsAreWhole()
    {
        WriteReport();
        var holes = new List<string>(); int checkedCount = 0;
        foreach (var id in ResolvedBuildings())
        {
            var prefab = HexModels.Prefab(id);
            if (prefab == null) continue;   // drawn as its stand-in on this machine
            var m = HexMeshCheck.Of(prefab); checkedCount++;
            if (m.SeeThroughShare > ToleranceFor(prefab)) holes.Add($"{id} ({prefab.name}): {m}");
        }
        Assert.That(checkedCount, Is.GreaterThan(0), "no building model resolved (no town set and no local sandbox kit)");
        Assert.That(holes, Is.Empty, "buildings that show the ground through their walls:\n" + string.Join("\n", holes));
    }

    // The set's baked mid levels are not tested here: the hex tiles draw only full meshes (Tal, 19 October 2026), and
    // most mid levels see through (the report lists them); they are the main map's, a follow-up of their own.

    /// <summary>With the town models off (or their set missing) a sandbox id draws its sandbox prefab again, or nothing
    /// (its stand-in) on a machine without it; a "town:" id draws nothing. With them on, it draws its town model.</summary>
    [Test]
    public void WithoutTownModelsTheSandboxPrefabDraws()
    {
        var (first, town, _) = HexModels.TownAliases[0];
        try
        {
            HexModels.DrawTownModels = false; HexModels.Clear();
            foreach (var (id, _, _) in HexModels.TownAliases)
                Assert.That(HexModels.Prefab(id), Is.EqualTo(Resources.Load<GameObject>(HexModels.PathOf(id))), id);
            Assert.That(HexModels.Prefab(town), Is.Null, town);
        }
        finally { HexModels.DrawTownModels = true; HexModels.Clear(); }
        var drawn = HexModels.Prefab(first);
        Assert.That(drawn, Is.Not.Null, first);
        Assert.That(drawn!.name, Is.EqualTo(town.Substring(HexModels.TownPrefix.Length)), first);
        Assert.That(drawn.activeInHierarchy, Is.False, "a template must not draw itself");
    }

    /// <summary>Every sandbox id drawn as the same Tripo model keeps its facing: of the four quarter turns, the sandbox
    /// prefab's vertices lie nearest the town model's as drawn (both in the unit frame), unless another turn is within
    /// <see cref="FacingMargin"/> of it (a near-symmetric block, where the close shots decide). Mirrors are reported, not
    /// tested: the renderer cannot draw a negative scale.</summary>
    [Test]
    public void TownAliasesFaceAsTheirSandboxPrefabs()
    {
        var wrong = new List<string>(); int compared = 0;
        foreach (var (id, town, same) in HexModels.TownAliases)
        {
            if (!same) continue;   // a remap to another model: nothing to match
            var sandbox = Resources.Load<GameObject>(HexModels.PathOf(id));
            var drawn = HexModels.Prefab(id);
            if (sandbox == null || drawn == null || drawn == sandbox) continue;
            var scores = HexMeshCheck.Facing(sandbox, drawn); compared++;
            var turns = scores.Take(4).ToArray();
            int best = System.Array.IndexOf(turns, turns.Min());
            if (best != 0 && turns[best] < FacingMargin * turns[0]) wrong.Add($"{id} -> {town}: best {HexMeshCheck.FacingNames[best]} ({scores[best]:F4}) vs as drawn {scores[0]:F4}");
        }
        if (compared == 0) Assert.Ignore("no local sandbox prefabs to compare");
        Assert.That(wrong, Is.Empty, string.Join("\n", wrong));
    }

    static void WriteReport()
    {
        var lines = new List<string>
        {
            "Hex-tile building meshes. seeThru: share of view rays on the model (8 sides x 20/35/50 degrees down, 96x96 each)",
            "that meet no front face of it (back faces are culled, so the ground shows through): the test. Informational:",
            "backFirst: rays whose first face is a back face (also one-sided railings and awnings seen from behind);",
            "open: boundary edges after welding by position; offBase: those above the base plane (min height + 0.5%);",
            "nonMan: edges of 3+ faces (the shipped meshes have thousands of T-junction and crack edges that never show).",
            "Written by HexModelMeshTests.",
            "",
            $"{"mesh",-58} {"tris",6} {"seeThru",8} {"backFirst",9} {"open",6} {"offBase",8} {"nonMan",7}",
        };
        void Row(string name, HexMeshCheck.Result r) => lines.Add($"{name,-58} {r.Triangles,6} {r.SeeThroughShare,8:P2} {r.BackFirstShare,9:P2} {r.Open,6} {r.OpenOffBase,8} {r.NonManifold,7}");
        lines.Add("-- local sandbox prefabs (Resources/HexSandbox, git-ignored) --");
        foreach (var go in Resources.LoadAll<GameObject>("HexSandbox").Where(g => g.name.StartsWith("hq_") || g.name.StartsWith("kit_")).OrderBy(g => g.name, System.StringComparer.Ordinal))
            Row("sandbox:" + go.name, HexMeshCheck.Of(go));
        var set = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
        lines.Add("-- TownModelSet (" + TownModels.SetPath + ") --");
        if (set != null)
            foreach (TownModel t in System.Enum.GetValues(typeof(TownModel)))
            {
                Row(TownModels.Ids[(int)t] + " full", HexMeshCheck.Of(new[] { (set.Mesh(t), HexModels.TownTurn) }));
                Row(TownModels.Ids[(int)t] + " mid", HexMeshCheck.Of(new[] { (set.mid[(int)t], HexModels.TownTurn) }));
            }
        lines.Add("-- what each building id resolves to now --");
        foreach (var id in ResolvedBuildings())
        {
            var prefab = HexModels.Prefab(id);
            if (prefab == null) { lines.Add($"{id,-58} (stand-in)"); continue; }
            Row(id + " -> " + prefab.name, HexMeshCheck.Of(prefab));
        }
        lines.Add("-- facing: sandbox prefab vs drawn town model, mean nearest distance (unit frame) per turn --");
        foreach (var (id, town, same) in HexModels.TownAliases)
        {
            var sandbox = Resources.Load<GameObject>(HexModels.PathOf(id));
            var drawn = HexModels.Prefab(id);
            if (!same || sandbox == null || drawn == null || drawn == sandbox) { lines.Add($"{id} -> {town}: {(same ? "not compared" : "remap")}"); continue; }
            var s = HexMeshCheck.Facing(sandbox, drawn);
            lines.Add($"{id} -> {town}: " + string.Join(" ", s.Select((v, i) => $"{HexMeshCheck.FacingNames[i]}={v:F4}")));
        }
        lines.AddRange(BudgetLines());
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "asset-staging/ui-local/hex-tiles/round2/buildings/mesh-report.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
        Debug.Log("HEX MESH REPORT -> " + path + "\n" + string.Join("\n", lines));
    }

    /// <summary>Each building tile's triangles on KeepsToItsTriangleBudget's hex ((2,-1), seed 1948) by which level the
    /// town aliases draw: the sandbox kit (town models off), every alias full, every alias mid, and mid only where the
    /// mid level is whole (within <see cref="Tolerance"/>), else full.</summary>
    static List<string> BudgetLines()
    {
        var lines = new List<string> { "-- triangles per building tile at hex (2,-1), seed 1948, by the level the aliases draw --",
            $"{"tile",-22} {"budget",7} {"sandbox",8} {"all full",9} {"all mid",8} {"mid where whole",16}" };
        var set = HexModels.TownSet();
        if (set == null) return lines;
        var alias = HexModels.TownAliases.ToDictionary(a => a.id, a => a.town);
        var whole = new Dictionary<int, bool>();
        int Index(string town) => System.Array.IndexOf(TownModels.Ids, town.Substring(HexModels.TownPrefix.Length).Replace(HexModels.MidSuffix, ""));
        bool MidWhole(int i) => whole.TryGetValue(i, out var w) ? w : whole[i] = HexMeshCheck.Of(new[] { (set.mid[i], HexModels.TownTurn) }).SeeThroughShare <= Tolerance;
        foreach (var id in HexPrototypes.All.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
        {
            var proto = HexPrototypes.Create(id);
            var ctx = new HexTileContext(2, -1, new FlatSurface(), 1948);
            proto.Build(ctx);
            if (!ctx.Placements.Any(p => alias.ContainsKey(HexModels.Resolve(p.Model, p.Position)))) continue;
            int Sum(System.Func<int, bool> mid) => ctx.Placements.Sum(p =>
            {
                if (p.StandIn == StandIn.Flat) return 12;
                if (!alias.TryGetValue(HexModels.Resolve(p.Model, p.Position), out var t)) return HexModels.Triangles(p.Model, p.StandIn, p.Position);
                int i = Index(t);
                return HexModels.IndexTriangles(mid(i) ? set.mid[i] : set.meshes[i]);
            });
            int sandbox;
            try
            {
                HexModels.DrawTownModels = false; HexModels.Clear();
                sandbox = ctx.Placements.Sum(p => p.StandIn == StandIn.Flat ? 12 : HexModels.Triangles(p.Model, p.StandIn, p.Position));
            }
            finally { HexModels.DrawTownModels = true; HexModels.Clear(); }
            lines.Add($"{id,-22} {proto.TriangleBudget,7} {sandbox,8} {Sum(_ => false),9} {Sum(_ => true),8} {Sum(MidWhole),16}");
        }
        return lines;
    }
}
