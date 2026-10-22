#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Capture;
using UnityEditor;

/// <summary>The contract every hex prototype keeps, checked for each one registered (so a new file is tested with no
/// edit here): a lower_snake_case id; the same placements for the same seed; every piece inside its inset hexagon; at
/// most its triangle budget (with the models on this machine, else stand-ins); and every board names known prototypes.</summary>
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
        int tris = ctx.Placements.Sum(p => p.StandIn == StandIn.Flat ? 12 : HexModels.Triangles(p.Model, p.StandIn, p.Position));
        Assert.That(tris, Is.LessThanOrEqualTo(proto.TriangleBudget), $"{id}: {tris} triangles");
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
