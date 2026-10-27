#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEngine;

/// <summary>The contract every hex prototype keeps, checked for each one registered (so a new file is tested with no
/// edit here): a lower_snake_case id; the same placements for the same seed; every piece inside its inset hexagon; at
/// most its triangle budget for non-original placements (Tal originals use the #1273 memory gates); and every board names known prototypes.</summary>
public class HexPrototypeTests
{
    static IEnumerable<string> Ids() => HexPrototypes.All.Keys.OrderBy(k => k);

    static HexTileContext Built(string id, int seed = 1948)
    {
        var ctx = new HexTileContext(2, -1, new FlatSurface(), seed);
        HexPrototypes.Create(id).Build(ctx);
        return ctx;
    }

    [Test] public void SomePrototypesAreRegistered() => Assert.That(HexPrototypes.All.Count, Is.GreaterThan(0));

    [TestCaseSource(nameof(Ids))]
    public void IdIsLowerSnakeCase(string id) => Assert.That(Regex.IsMatch(id, "^[a-z][a-z0-9_]*$"), id);

    [TestCaseSource(nameof(Ids))]
    public void NamesAPaletteKindAndRegion(string id)
    {
        var info = HexPrototypes.Info(id);
        Assert.That(HexPrototypes.Kinds, Does.Contain(info.Kind), $"{id}: kind '{info.Kind}'");
        Assert.That(info.Region == "" || HexPrototypes.Regions.Contains(info.Region), $"{id}: region '{info.Region}'");
    }

    [TestCaseSource(nameof(Ids))]
    public void BuildsSomething(string id) => Assert.That(Built(id).Placements.Count, Is.GreaterThan(0), id);

    [TestCaseSource(nameof(Ids))]
    public void IsDeterministic(string id)
    {
        var a = Built(id).Placements.Select(p => (p.Model, p.Matrix)).ToList();
        var b = Built(id).Placements.Select(p => (p.Model, p.Matrix)).ToList();
        CollectionAssert.AreEqual(a, b, id);
    }

    [TestCaseSource(nameof(Ids))]
    public void StaysInsideItsHex(string id)
    {
        var ctx = Built(id);
        foreach (var p in ctx.Placements)
            Assert.That(HexGeometry.InHex(p.Position, ctx.Centre, ctx.SizeM, ctx.InsetM - 1f), $"{id}: {p.Model} at {p.Position} is outside its inset hexagon");
    }

    [TestCaseSource(nameof(Ids))]
    public void KeepsToItsTriangleBudget(string id)
    {
        var proto = HexPrototypes.Create(id);
        var ctx = Built(id);
        var originals = OriginalMeshPaths();
        int tris = ctx.Placements.Sum(p => p.StandIn == StandIn.Flat ? 12 : CountTowardsCap(
            Meshes(HexModels.Prefab(HexModels.Resolve(p.Model, p.Position))),
            HexModels.Triangles(p.Model, p.StandIn, p.Position), originals));
        AssertBudget(id, tris, proto.TriangleBudget);
    }

    // Tal Heuberger chose option 1 (exempt originals; memory gates instead), relayed by the owner, 26 Oct 2026, #1273.
    // A piece is one placement, not a whole prototype: mixed/generated pieces and stand-ins retain their cap.
    [Serializable] sealed class OriginalManifest { public OriginalFile[] files = Array.Empty<OriginalFile>(); }
    [Serializable] sealed class OriginalFile { public string path = ""; public string source = ""; }

    static HashSet<string> OriginalMeshPaths()
    {
        // Override is local test instrumentation for the required empty-manifest RED, never a game/build setting.
        string file = Environment.GetEnvironmentVariable("STATECRAFT_CAPS_MANIFEST")
            ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/original-models.json"));
        var manifest = JsonUtility.FromJson<OriginalManifest>(File.ReadAllText(file));
        return manifest.files.Where(f => (f.source == "507" || f.source == "336")
                && f.path.StartsWith("unity/Assets/", StringComparison.Ordinal)
                && f.path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.path.Substring("unity/".Length)).ToHashSet(StringComparer.Ordinal);
    }

    static Mesh[] Meshes(GameObject? prefab) => prefab == null ? Array.Empty<Mesh>()
        : prefab.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh)
            .Concat(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(f => f.sharedMesh)).ToArray();

    static bool AllOriginals(Mesh[] meshes, HashSet<string> originals) => meshes.Length > 0
        && meshes.All(m => m != null && originals.Contains(AssetDatabase.GetAssetPath(m)));

    static int CountTowardsCap(Mesh[] meshes, int triangles, HashSet<string> originals) =>
        AllOriginals(meshes, originals) ? 0 : triangles;

    static void AssertBudget(string id, int triangles, int limit) =>
        Assert.That(triangles, Is.LessThanOrEqualTo(limit), $"{id}: {triangles} triangles");

    [Test]
    public void TheExemptSetEqualsTheManifestsActualImportedMeshes()
    {
        var paths = OriginalMeshPaths();
        Assert.Greater(paths.Count, 0, "real manifest contains original FBX");
        // Independent oracle: load actual FBX mesh subassets from the manifest, rather than using the path predicate.
        var originalMeshes = paths.SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Mesh>().ToHashSet();
        Assert.GreaterOrEqual(originalMeshes.Count, paths.Count, "every original FBX has an imported mesh");
        var models = Ids().SelectMany(id => Built(id).Placements).Where(p => p.StandIn != StandIn.Flat)
            .Select(p => HexModels.Resolve(p.Model, p.Position)).Distinct().OrderBy(id => id).ToArray();
        var expected = models.Where(id => {
            var meshes = Meshes(HexModels.Prefab(id));
            return meshes.Length > 0 && meshes.All(originalMeshes.Contains);
        }).ToArray();
        var actual = models.Where(id => AllOriginals(Meshes(HexModels.Prefab(id)), paths)).ToArray();
        CollectionAssert.AreEqual(expected, actual, "no hand-written exemption ids or whole-prototype escape");
        Assert.IsNotEmpty(actual);
        TestContext.WriteLine($"CAPS exempt placement models={actual.Length}: {string.Join(",", actual)}");
    }

    [Test]
    public void GeneratedMixedAndMissingPiecesKeepTheirCapAndAnOverBudgetPieceFails()
    {
        var generated = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
        try
        {
            var paths = OriginalMeshPaths();
            Assert.IsFalse(AllOriginals(new[] { generated }, paths));
            Assert.IsFalse(AllOriginals(Array.Empty<Mesh>(), paths));
            Assert.IsFalse(AllOriginals(new Mesh[] { null! }, paths));
            var original = paths.SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Mesh>().First();
            Assert.IsTrue(AllOriginals(new[] { original }, paths));
            Assert.IsFalse(AllOriginals(new[] { original, generated }, paths), "one original must not exempt a mixed piece");
            Assert.AreEqual(0, CountTowardsCap(new[] { original }, 1, paths));
            int generatedCount = CountTowardsCap(new[] { generated }, generated.triangles.Length / 3, paths);
            Assert.AreEqual(1, generatedCount);
            Assert.Throws<AssertionException>(() => AssertBudget("synthetic generated", generatedCount, 0));
            int mixedCount = CountTowardsCap(new[] { original, generated }, 2, paths);
            Assert.AreEqual(2, mixedCount);
            Assert.Throws<AssertionException>(() => AssertBudget("synthetic mixed", mixedCount, 1));
        }
        finally { UnityEngine.Object.DestroyImmediate(generated); }
    }

    [TestCase("https://x/?hex-tiles", "central_israel")]
    [TestCase("https://x/?a=1&hex-tiles=1", "central_israel")]
    [TestCase("https://x/?hex-tiles=tel_aviv_crop&b=2", "tel_aviv_crop")]
    [TestCase("https://x/?hex-tilesx", null)]
    [TestCase("https://x/#?hex-tiles", null)]
    [TestCase("https://x/", null)]
    public void SwitchPicksTheBoardOnTheWeb(string url, string? board) =>
        Assert.That(HexTilesLook.RequestedBoard(url, new string[0], true), Is.EqualTo(board));

    [Test]
    public void SwitchPicksTheBoardOnDesktop()
    {
        Assert.That(HexTilesLook.RequestedBoard(null, new[] { "app" }, false), Is.Null);
        Assert.That(HexTilesLook.RequestedBoard(null, new[] { "app", "-hex-tiles" }, false), Is.EqualTo(HexTilesLook.Board));
        Assert.That(HexTilesLook.RequestedBoard(null, new[] { "app", "-hex-tiles", "-ui-preview" }, false), Is.EqualTo(HexTilesLook.Board));
        Assert.That(HexTilesLook.RequestedBoard(null, new[] { "app", "-hex-tiles", "tel_aviv_crop" }, false), Is.EqualTo("tel_aviv_crop"));
    }

    [Test]
    public void BoardsNameKnownPrototypesAndTerrains()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { HexBoardCapture.BoardsFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name.EndsWith(HexBoardOverrides.Suffix)) continue;   // a board's hand edits, applied (and so checked) by LoadBoard
            var board = HexBoardCapture.LoadBoard(name);
            foreach (var h in board.Hexes.Values)
            {
                Assert.That(new[] { "flat", "hills", "mountain", "water" }, Does.Contain(h.terrain), $"{name} ({h.q},{h.r})");
                Assert.That(HexGround.Ids, Does.Contain(h.ground), $"{name} ({h.q},{h.r})");
                if (h.prototype != "") Assert.That(HexPrototypes.All.ContainsKey(h.prototype), $"{name} ({h.q},{h.r}): no prototype '{h.prototype}'");
                if (h.unit != "") Assert.That(HexUnitTypes.All.ContainsKey(h.unit), $"{name} ({h.q},{h.r}): no unit '{h.unit}'");
                if (h.region != "") Assert.That(board.Regions.ContainsKey(h.region), $"{name} ({h.q},{h.r}): no region '{h.region}'");
            }
        }
    }
}
