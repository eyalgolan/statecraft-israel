#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.HexTiles;
using UnityEngine;

/// <summary>Check the mesh the fallback renderer draws, not just its placement origin.</summary>
public class HexHeightFootprintTests
{
    const string MissingModel = "tiles:__hex_boundary_regression__/absent";

    [TestCase(0.5f)]
    [TestCase(0.45f)] // PineForest's explicit broadleaf estimate must not shrink the fallback canopy.
    public void BroadleafCanopyCannotCrossTheInset(float footprintShare)
    {
        Assert.That(HexModels.Prefab(MissingModel), Is.Null, "The fixture must use the stand-in renderer");
        var ctx = new HexTileContext(0, 0, new FlatSurface());
        bool placed = ctx.PlaceByHeight(MissingModel, new Vector3(3600f, 0f, 0f), 0f, 1000f,
            StandIn.Tree, footprintShare: footprintShare);
        AssertVerticesInside(ctx);
        Assert.That(placed, Is.False, "This canopy is too wide for the remaining inset margin");
        Assert.That(ctx.Placements, Is.Empty);
    }

    [TestCase(StandIn.Tree, 0.45f, 0f)]
    [TestCase(StandIn.Tree, 0.45f, 30f)]
    [TestCase(StandIn.Tree, 0.45f, 90f)]
    [TestCase(StandIn.Pine, 0.3f, 30f)]
    [TestCase(StandIn.Palm, 0.5f, 30f)]
    public void HeightSizedStandInsStillFitAwayFromTheEdge(StandIn kind, float footprintShare, float yaw)
    {
        Assert.That(HexModels.Prefab(MissingModel), Is.Null);
        var ctx = new HexTileContext(2, -1, new FlatSurface());
        Assert.That(ctx.PlaceByHeight(MissingModel, ctx.Centre + new Vector3(3100f, 0f, 0f), yaw, 1000f,
            kind, footprintShare: footprintShare), Is.True);
        Assert.That(ctx.Placements.Count, Is.EqualTo(1));
        AssertVerticesInside(ctx);
    }

    [Test]
    public void DefaultFootprintCannotAdmitAnOverhangingCanopy()
    {
        var ctx = new HexTileContext(0, 0, new FlatSurface());
        bool placed = ctx.PlaceByHeight(MissingModel, new Vector3(3600f, 0f, 0f), 0f, 1000f, StandIn.Tree);
        AssertVerticesInside(ctx);
        Assert.That(placed, Is.False);
        Assert.That(ctx.Placements, Is.Empty);
    }

    [Test]
    public void RotatedCanopyCannotCrossASlantedEdge()
    {
        var ctx = new HexTileContext(0, 0, new FlatSurface());
        // 3,600 metres along the slanted edge's outward normal; yaw aligns the checked square to that edge.
        bool placed = ctx.PlaceByHeight(MissingModel, new Vector3(1800f, 0f, 3117.691f), 30f, 1000f,
            StandIn.Tree);
        AssertVerticesInside(ctx);
        Assert.That(placed, Is.False);
        Assert.That(ctx.Placements, Is.Empty);
    }

    [TestCase(StandIn.Pine, 0.1f, 3900f)]
    [TestCase(StandIn.Palm, 0.3f, 3700f)]
    public void NarrowEstimatesCannotAdmitOtherOverhangingStandIns(StandIn kind, float footprintShare, float x)
    {
        var ctx = new HexTileContext(0, 0, new FlatSurface());
        bool placed = ctx.PlaceByHeight(MissingModel, new Vector3(x, 0f, 0f), 0f, 1000f,
            kind, footprintShare: footprintShare);
        AssertVerticesInside(ctx);
        Assert.That(placed, Is.False);
        Assert.That(ctx.Placements, Is.Empty);
    }

    [Test]
    public void BroadleafCanopyStillFitsCloseToTheInset()
    {
        var ctx = new HexTileContext(0, 0, new FlatSurface());
        // The rendered canopy reaches x = 3,968.4, just inside the inset at x = 3,980.127.
        Assert.That(ctx.PlaceByHeight(MissingModel, new Vector3(3450f, 0f, 0f), 0f, 1000f,
            StandIn.Tree, footprintShare: 0.45f), Is.True);
        Assert.That(ctx.Placements.Count, Is.EqualTo(1));
        AssertVerticesInside(ctx);
    }

    [Test]
    public void LargerCallerFootprintStillControlsPlacement()
    {
        var ctx = new HexTileContext(0, 0, new FlatSurface());
        Assert.That(ctx.PlaceByHeight(MissingModel, new Vector3(3300f, 0f, 0f), 0f, 1000f,
            StandIn.Tree, footprintShare: 1.2f), Is.False);
        Assert.That(ctx.Placements, Is.Empty);
    }

    [TestCase("flat")]
    [TestCase("hills")]
    public void EnvironmentFallbacksKeepTheirWholeFootprintWithoutAnInset(string terrain)
    {
        var hexes = (from q in Enumerable.Range(-1, 3)
                     from r in Enumerable.Range(-1, 3)
                     select new BoardHex { q = q, r = r, env = true, terrain = terrain }).ToArray();
        var board = new HexBoard(new HexBoardFile { hexes = hexes });
        var surface = new FlatSurface();
        var placements = HexEnvironment.Dress(board, surface).ToArray();
        Assert.That(placements, Is.Not.Empty);
        CollectionAssert.AreEqual(placements.Select(p => p.Matrix),
            HexEnvironment.Dress(board, surface).Select(p => p.Matrix), "environment placement stays deterministic");
        TestContext.WriteLine($"Environment {terrain}: {placements.Length} placements, " +
            $"{placements.Count(p => p.StandIn == StandIn.Tree)} trees, {placements.Count(p => p.StandIn == StandIn.Pine)} pines");
        foreach (var placement in placements)
        {
            if (HexModels.Prefab(HexModels.Resolve(placement.Model, placement.Position)) != null) continue;
            var ctx = new HexTileContext(placement.Q, placement.R, surface, insetShare: 0f);
            AssertPlacementVerticesInside(ctx, placement);
        }
    }

    static void AssertVerticesInside(HexTileContext ctx)
    {
        foreach (var placement in ctx.Placements) AssertPlacementVerticesInside(ctx, placement);
    }

    static void AssertPlacementVerticesInside(HexTileContext ctx, HexPlacement placement)
    {
        foreach (var part in HexModels.StandInParts(placement.StandIn))
            foreach (var vertex in part.mesh.vertices)
            {
                // This is the same pair of matrices used by HexBoardRenderer for a missing model.
                var world = (placement.Matrix * part.local).MultiplyPoint3x4(vertex);
                Assert.That(ctx.Inside(world), Is.True,
                    $"{placement.StandIn} vertex {world} crosses the inset (origin {placement.Position})");
            }
    }
}
