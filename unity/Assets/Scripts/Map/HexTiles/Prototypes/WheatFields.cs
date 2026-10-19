#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: a farmed plain, Civ's farm improvement. A patchwork of large fields on one grid (wheat,
    /// green fodder, fresh ploughing) with tracks between, olive hedgerows along a few edges, and one farmhouse with its
    /// barn (13 October Tripo modules) by the main track. Light, so many hexes can wear it.</summary>
    [HexPrototype("wheat_fields")]
    public sealed class WheatFields : IHexPrototype
    {
        public string Title => "Wheat fields and a farmstead";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => HexScale.BudgetFields + 10000;


        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(-25f, 25f), cell = 0.36f * s, gap = 0.035f * s;
            var turn = Quaternion.Euler(0f, yaw, 0f);
            // Fields: a 5 x 5 grid of parcels painted into the ground as crop rows (wheat, green crops, fresh ploughing),
            // a few left as pasture, with soft edges so they read as fields, not boards, from far away.
            string[] crops = { "wheat", "wheat", "green_crop", "ploughed", "wheat", "green_crop" };
            for (int i = -2; i <= 2; i++)
                for (int j = -2; j <= 2; j++)
                {
                    var at = ctx.Centre + turn * new Vector3(i * cell, 0f, j * cell);
                    string crop = crops[ctx.Rng.Next(crops.Length)];
                    bool across = ctx.Rng.Next(2) == 0;
                    if (ctx.Rng.Next(7) == 0) continue;   // pasture
                    ctx.PaintGround(crop, at, yaw + (across ? 90f : 0f), cell - gap, cell - gap, 0.08f, 0.95f);
                }
            // The farm track: one straight dirt track along the grid through the hex, the farmstead on it.
            ctx.PaintStroke("dirt", ctx.Centre + turn * new Vector3(0.5f * cell, 0f, -2.6f * cell), ctx.Centre + turn * new Vector3(0.5f * cell, 0f, 2.6f * cell), 0.035f * s);
            // Hedgerows: olives down two parcel edges.
            for (int k = -4; k <= 4; k++)
                foreach (float x in new[] { -0.5f, 0.5f })
                    if (ctx.Rng.Next(3) != 0 && !ctx.InUnitSpot(ctx.Centre + turn * new Vector3(x * cell, 0f, k * cell * 0.5f), 0.05f * s))
                        ctx.PlaceByHeight("sandbox:olive_lite", ctx.Centre + turn * new Vector3(x * cell, 0f, k * cell * 0.5f), ctx.Range(0f, 360f), 0.12f * s, StandIn.Tree, null, 0.5f);
            // The farmstead on a yard by the centre track: a two-storey farmhouse and a barn.
            var yard = ctx.Centre + turn * new Vector3(0.48f * s, 0f, -0.18f * s);   // out of the unit spot
            ctx.PaintGround("dirt", yard, yaw, 0.3f * s, 0.24f * s, 0.3f, 0.9f);
            ctx.PlaceByWidth("sandbox:kit_moshava", yard + turn * new Vector3(-0.06f * s, 0f, 0f), yaw + 180f, 0.13f * s, StandIn.Box);
            ctx.PlaceByWidth("sandbox:kit_moshava_3", yard + turn * new Vector3(0.08f * s, 0f, 0.02f * s), yaw + 90f, 0.12f * s, StandIn.Box);
        }
    }

    /// <summary>Context tile: open grazing land in the coastal plain and foothills. Scattered olive and carob trees, a few
    /// rock outcrops, and a flock of sheep with a donkey and their shepherd's track (13 October Tripo animals).</summary>
    [HexPrototype("grazing_land")]
    public sealed class GrazingLand : IHexPrototype
    {
        public string Title => "Grazing land with a flock";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "grass";
        public int TriangleBudget => 60000;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            int trees = 0;
            for (int k = 0; k < 60 && trees < 12; k++)
            {
                bool carob = ctx.Rng.Next(3) == 0;
                var spot = ctx.RandomPoint(0.04f * s);
                if (ctx.InUnitSpot(spot, 0.05f * s)) continue;
                if (ctx.PlaceByHeight(carob ? "sandbox:carob_lite" : "sandbox:olive_lite", spot, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.15f) * s, StandIn.Tree, null, 0.5f)) trees++;
            }
            for (int k = 0; k < 3; k++)
                ctx.PlaceByWidth("sandbox:limestone*", ctx.At(ctx.Range(0f, 360f), ctx.Range(0.4f, 0.6f)), ctx.Range(0f, 360f), ctx.Range(0.07f, 0.1f) * s, StandIn.Rock);
            // The flock: one compact group, all grazing the same way, the donkey at its edge.
            var flock = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.45f, 0.55f));   // grazing beside the unit spot, not in it
            float heading = ctx.Range(0f, 360f);
            for (int k = 0; k < 6; k++)
            {
                var p = flock + new Vector3(ctx.Range(-0.1f, 0.1f) * s, 0f, ctx.Range(-0.1f, 0.1f) * s);
                ctx.PlaceByHeight("proto:figure-sheep-lite-1948", p, heading + ctx.Range(-30f, 30f), 0.06f * s, StandIn.Figure, null, 1.2f);
            }
            ctx.PlaceByHeight("proto:figure-donkey-lite-1948", flock + new Vector3(0.13f * s, 0f, 0f), heading, 0.08f * s, StandIn.Figure, null, 1.0f);
        }
    }
}
