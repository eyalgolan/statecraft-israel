#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A Jerusalem garden suburb of the 1920s to 1940s on a hill, the edge variant to the city hexes. Its models
    /// are Rehavia (land bought 1921, founded 1922, first house 1924, laid out by Richard Kauffmann with curving streets and
    /// a tree-lined pedestrian boulevard; https://en.wikipedia.org/wiki/Rehavia), Talbiya (built up from the early 1920s;
    /// https://en.wikipedia.org/wiki/Talbiya) and Beit HaKerem (founded 1922;
    /// https://en.wikipedia.org/wiki/Beit_HaKerem,_Jerusalem), all checked 2026-10-19. No quarter or landmark is named
    /// in the tile itself.
    /// - A round green at the centre, kept clear for a unit.
    /// - Two curving ring streets (an inner one at about 0.25 S, an outer one at about 0.55 S), gently waved so they
    ///   read as garden-suburb streets rather than circles.
    /// - Detached stone villas (kit_jerusalem, _2, _3) between and beyond the rings: 8 on the inner ring, 12 on the
    ///   outer, each turned to its street, each with a garden tree beside it (a carob, or a cypress every other villa).
    /// - A short boulevard from the green to the south-west edge: a painted walk with an aleppo pine row on each side.
    ///   Villa slots on its line are left out.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 20 villas (kit_jerusalem*, up to 1,400 each) = 28,000.
    /// - Garden trees: at most 10 carobs x 650 = 6,500 and 10 cypresses x 1,602 = 16,020.
    /// - Boulevard: at most 8 aleppo pines x 1,602 = 12,816.
    /// - 2 townspeople (v2 figures, up to 4,000 each) on the green = 8,000.
    /// - Total about 71,300 of 80,000.</summary>
    [HexPrototype("jerusalem_garden_quarter", Kind = "town", Region = "centre")]
    public sealed class JerusalemGardenQuarter : IHexPrototype
    {
        public string Title => "Jerusalem garden suburb (1920s to 1940s)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 80000;

        // Explicit ids: kit_jerusalem_4 is the mosque-like b17 mesh.
        static readonly string[] Villas = { "sandbox:kit_jerusalem", "sandbox:kit_jerusalem_2", "sandbox:kit_jerusalem_3" };
        static readonly string[] People = { "proto:figure-townsman-c02-1948-v2", "proto:figure-woman-blue-dress-c10-1948-v2" };
        const string Pine = "sandbox:aleppo_pine_lite", Cypress = "sandbox:cypress_lite", Carob = "sandbox:carob_lite";

        static readonly Color Limestone = new Color(0.87f, 0.81f, 0.69f), PineLeaf = new Color(0.20f, 0.36f, 0.18f),
            CypressLeaf = new Color(0.14f, 0.28f, 0.15f), CarobLeaf = new Color(0.22f, 0.36f, 0.17f);

        const float Slope = 40f;
        const int MaxVillas = 20, MaxCarobs = 10, MaxCypresses = 10, MaxPines = 8;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, turn = ctx.Range(0f, 360f), phase = ctx.Range(0f, 360f);
            float boulevard = turn + 225f;
            // A ring radius that waves gently with the bearing, so the streets curve.
            float R(float baseShare, float bearing) => baseShare + 0.02f * Mathf.Sin(Mathf.Deg2Rad * (3f * bearing + phase));
            bool OnBoulevard(float bearing, float clearDeg) => Mathf.Abs(Mathf.DeltaAngle(bearing, boulevard)) < clearDeg;

            // 1. Ground: the green at the centre, the two ring streets as short painted segments, the boulevard walk.
            ctx.PaintGround("grass", ctx.Centre, turn, 0.34f * s, 0.34f * s, 0.45f, 0.8f);
            ctx.PaintGround("grass", ctx.Centre, turn + 45f, 0.34f * s, 0.34f * s, 0.45f, 0.8f);
            foreach (float ring in new[] { 0.25f, 0.55f })
                for (int k = 0; k < 24; k++)
                {
                    float b0 = turn + 15f * k, b1 = b0 + 15f;
                    ctx.PaintStroke("dirt", ctx.At(b0, R(ring, b0)), ctx.At(b1, R(ring, b1)), 0.04f * s, 0.8f);
                }
            ctx.PaintStroke("dirt", ctx.At(boulevard, 0.17f), ctx.At(boulevard, 0.9f), 0.05f * s, 0.85f);
            ctx.PaintStroke("grass", ctx.At(boulevard, 0.3f), ctx.At(boulevard, 0.9f), 0.13f * s, 0.5f);

            // 2. Villas: 8 on the inner ring (0.40 S) and 12 on the outer (0.665 S), each turned to face the street inside
            //    it, with a garden tree half a slot along. Slots on the boulevard are skipped (their draws are still made).
            int villas = 0, carobs = 0, cypresses = 0, slot = 0;
            foreach (var (ring, count, offset) in new[] { (0.40f, 8, 22.5f), (0.665f, 12, 15f) })
                for (int k = 0; k < count; k++)
                {
                    float step = 360f / count, b = turn + offset + step * k + ctx.Range(-4f, 4f);
                    float r = R(ring, b), w = ctx.Range(0.115f, 0.125f), yaw = b + 180f + ctx.Range(-8f, 8f);
                    float tb = b + 0.5f * step, tr = R(ring, tb) + ctx.Range(-0.02f, 0.02f), th = ctx.Range(0.13f, 0.15f);
                    if (OnBoulevard(b, 0.5f * step + 6f)) continue;
                    var at = ctx.At(b, r);
                    if (villas >= MaxVillas || !ctx.PlaceByWidth(Villas[villas % Villas.Length], at, yaw, w * s, StandIn.Box, Limestone, Slope)) continue;
                    villas++;
                    ctx.PaintGround("grass", at, yaw, 0.21f * s, 0.21f * s, 0.35f, 0.6f);
                    if (OnBoulevard(tb, 8f)) continue;
                    if (slot++ % 2 == 0)
                    {
                        if (carobs < MaxCarobs && ctx.PlaceByHeight(Carob, ctx.At(tb, tr), ctx.Range(0f, 360f), th * s, StandIn.Tree, CarobLeaf, 0.5f, Slope)) carobs++;
                    }
                    else if (cypresses < MaxCypresses && ctx.PlaceByHeight(Cypress, ctx.At(tb, tr), ctx.Range(0f, 360f), (th + 0.02f) * s, StandIn.Pine, CypressLeaf, 0.3f, Slope))
                        cypresses++;
                }

            // 3. The boulevard's two pine rows, from just outside the unit spot to the edge.
            int pines = 0;
            var across = Quaternion.Euler(0f, boulevard + 90f, 0f) * Vector3.forward * (0.065f * s);
            for (float r = 0.34f; r <= 0.86f && pines < MaxPines; r += 0.13f)
                foreach (float side in new[] { -1f, 1f })
                    if (pines < MaxPines && ctx.PlaceByHeight(Pine, ctx.At(boulevard, r) + across * side, ctx.Range(0f, 360f), ctx.Range(0.13f, 0.15f) * s, StandIn.Pine, PineLeaf, 0.4f, Slope))
                        pines++;

            // 4. Life: two townspeople walking on the green.
            for (int k = 0; k < People.Length; k++)
            {
                float b = turn + 40f + 150f * k;
                ctx.PlaceByHeight(People[k], ctx.At(b, 0.1f), b + 90f, 0.075f * s, StandIn.Figure, null, 0.4f, Slope);
            }
        }
    }
}
