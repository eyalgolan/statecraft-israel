#nullable enable

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A/B variant of tel_aviv_city for review: the same layout, but every other outer-ring building is a block
    /// from the first (10 October, faceted low-poly) Tripo batch, drawn as a cluster of houses at a building's footprint, so detail
    /// stays realistic at the square and the edge reads denser. Render it on the city's hex with
    /// -prototype tel_aviv_city_mixed -on tel_aviv_city.</summary>
    [HexPrototype("tel_aviv_city_mixed")]
    public sealed class TelAvivCityMixed : TelAvivCity
    {
        public override string Title => "Tel Aviv, White City (1948), mixed with the first batch";
        static readonly string[] FirstBatch = { "proto:tel-aviv-white-city-v1", "proto:moshava-town-v1" };

        protected override (string model, float width) OuterBuilding(int k, HexTileContext ctx) =>
            k % 2 == 1 ? (FirstBatch[(k / 2) % FirstBatch.Length], ctx.Range(0.165f, 0.18f) * ctx.S) : base.OuterBuilding(k, ctx);
    }
}
