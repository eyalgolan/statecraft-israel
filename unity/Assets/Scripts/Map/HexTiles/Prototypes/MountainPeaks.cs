#nullable enable

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Built-in context tile for a mountain hex: two to four great rock masses (Poly Haven scans, stand-in rocks).</summary>
    [HexPrototype("mountain_peaks")]
    public sealed class MountainPeaks : IHexPrototype
    {
        public string Title => "Mountain peaks";
        public HexTerrain Terrain => HexTerrain.Mountain;
        public string Ground => "mountain";
        public int TriangleBudget => 6000;

        public void Build(HexTileContext ctx)
        {
            int n = 2 + ctx.Rng.Next(3);
            for (int k = 0; k < n; k++)
            {
                float width = ctx.Range(0.45f, 0.75f) * ctx.S;
                var at = ctx.At(ctx.Range(0f, 360f), ctx.Range(0f, 0.25f));
                ctx.PlaceByWidth("sandbox:limestone*", at, ctx.Range(0f, 360f), width, StandIn.Rock, null, 90f);
            }
        }
    }
}
