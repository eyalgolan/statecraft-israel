#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: the Jezreel Valley's broad cooperative fields, 1948. Big rectangular field blocks of wheat,
    /// green fodder and fresh ploughing in a planned rotation pattern, straight farm roads and irrigation channels between
    /// them, windbreak lines of cypress and eucalyptus along the channels, and one distant farmstead (a barn and a water
    /// tower) at the hex edge only. Larger blocks and stronger geometry than wheat_fields: this is the valley's signature.
    /// Mount Tabor is not on this tile. No settlement centre; the farmstead is generic, not a named kibbutz or moshav.
    ///
    /// Sources (checked 2026-10-19):
    /// - The valley's swamps were drained for farming after the 1920s purchases; Nahalal (11 September 1921) and Ein Harod
    ///   (1921) were followed by dozens more settlements; the fertile plain grows wheat and other field crops:
    ///   https://en.wikipedia.org/wiki/Jezreel_Valley and https://he.wikipedia.org/wiki/עמק_יזרעאל
    /// - Eucalyptus was planted in the new Jewish farming settlements to drain swamps, give shade, block wind and mark
    ///   fields (a general source for the settlements, not for the Jezreel alone):
    ///   https://www.kkl-jnf.org/about-kkl-jnf/israel-at-70/israel-grows-from-sand-and-rocks/
    /// - Cypress windbreaks and irrigation channels are a generic look; no source ties them to this valley in 1948.
    ///
    /// Layout: everything is anchored to one world lattice with a fixed orientation (blocks 0.62 x 0.34 S, pitch 0.66 x
    /// 0.38 S), not to the hex centre, so two like hexes continue one field grid across their shared edge. The crop of a
    /// block comes from its lattice indices (a planned rotation, not random), so a block cut by an edge gets the same crop
    /// on both sides. North-south lines alternate between farm roads and irrigation channels; the channel lines carry
    /// windbreaks (cypress and eucalyptus alternating), and every third east-west line carries a eucalyptus windbreak.
    /// Tall pieces keep out of ctx.InUnitSpot and off the farmstead yard.
    ///
    /// Triangle budget by arithmetic (hard caps below; FlatSurface refuses nothing): 14 cypress x 1,600 = 22,400;
    /// 50 eucalyptus stand-ins x under 100 = under 5,000; barn and water tower, 2 x 1,400 = 2,800. Worst case under 30,200
    /// of 34,000.</summary>
    [HexPrototype("jezreel_valley", Kind = "nature", Region = "north")]
    public sealed class JezreelValley : IHexPrototype
    {
        public string Title => "Jezreel Valley cooperative fields (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 34000;

        const string Cypress = "sandbox:cypress_lite", Eucalyptus = "tiles:jezreel_valley/eucalyptus",
            Barn = "sandbox:kit_moshava_3", Tower = "sandbox:kit_kibbutz_4";
        const int MaxCypress = 14, MaxEucalyptus = 50;
        const float BlockW = 0.62f, BlockD = 0.34f, PitchX = 0.66f, PitchZ = 0.38f, TreeStep = 0.11f;

        // A planned rotation: the crop of block (i, j) is Rotation[(2i + j) mod 5].
        static readonly string[] Rotation = { "wheat", "green_crop", "wheat", "ploughed", "green_crop" };

        static readonly Color CypressGreen = new Color(0.16f, 0.28f, 0.15f), EucalyptusGrey = new Color(0.45f, 0.53f, 0.42f),
            BarnRed = new Color(0.62f, 0.42f, 0.32f), TowerConcrete = new Color(0.78f, 0.76f, 0.70f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, px = PitchX * s, pz = PitchZ * s, reach = 1.05f * s;
            Vector3 c = ctx.Centre;
            int i0 = Mathf.FloorToInt((c.x - reach) / px), i1 = Mathf.CeilToInt((c.x + reach) / px);
            int j0 = Mathf.FloorToInt((c.z - reach) / pz), j1 = Mathf.CeilToInt((c.z + reach) / pz);

            // 1. The farmstead first, at a seeded hex corner, out at the edge, so the windbreaks keep off its yard (the
            //    yard is painted last, step 5).
            float corner = 60f * ctx.Rng.Next(6) + ctx.Range(-8f, 8f);
            var yard = ctx.At(corner, 0.74f);
            var side = Quaternion.Euler(0f, corner + 90f, 0f) * Vector3.forward;
            ctx.PlaceByWidth(Barn, yard - side * (0.06f * s), corner + 180f, 0.12f * s, StandIn.Box, BarnRed);
            ctx.PlaceByWidth(Tower, yard + side * (0.09f * s), corner + 180f, 0.07f * s, StandIn.Box, TowerConcrete);
            bool NearYard(Vector3 p) => Flat(p - yard).magnitude < 0.26f * s;

            // 2. The field blocks, painted on the world lattice; crops from the lattice indices.
            for (int i = i0; i <= i1; i++)
                for (int j = j0; j <= j1; j++)
                {
                    var at = new Vector3((i + 0.5f) * px, c.y, (j + 0.5f) * pz);
                    if (Flat(at - c).magnitude > 1.3f * s) continue;
                    string crop = Rotation[Mod(2 * i + j, Rotation.Length)];
                    ctx.PaintGround(crop, at, 0f, BlockW * s, BlockD * s, 0.05f, 0.95f);
                }

            // 3. North-south lines: even ones are farm roads, odd ones irrigation channels. East-west: every third line a
            //    channel. All run past the hex edge (the paint is clipped to the hex).
            for (int i = i0; i <= i1; i++)
            {
                float x = i * px;
                var a = new Vector3(x, c.y, c.z - reach); var b = new Vector3(x, c.y, c.z + reach);
                if (Mod(i, 2) == 0) ctx.PaintStroke("dirt", a, b, 0.028f * s, 0.9f);
                else ctx.PaintStroke("dirt", a, b, 0.012f * s, 0.7f);
            }
            for (int j = j0; j <= j1; j++)
            {
                if (Mod(j, 3) != 0) continue;
                float z = j * pz;
                ctx.PaintStroke("dirt", new Vector3(c.x - reach, c.y, z), new Vector3(c.x + reach, c.y, z), 0.012f * s, 0.7f);
            }

            // 4. Windbreaks on world-anchored spots: along each north-south channel line (cypress and eucalyptus in turn),
            //    and along every third east-west line (eucalyptus). Out of the unit spot, the yard and the crossings.
            int cypress = 0, eucalyptus = 0;
            for (int i = i0; i <= i1; i++)
            {
                if (Mod(i, 2) != 1) continue;
                float x = i * px + 0.02f * s;   // beside the channel, not on it
                int k0 = Mathf.FloorToInt((c.z - reach) / (TreeStep * s)), k1 = Mathf.CeilToInt((c.z + reach) / (TreeStep * s));
                for (int k = k0; k <= k1; k++)
                {
                    var at = new Vector3(x, c.y, k * TreeStep * s);
                    if (!ctx.Inside(at, 0.03f * s) || ctx.InUnitSpot(at, 0.05f * s) || NearYard(at) || NearEastWestBreak(at.z)) continue;
                    if (Mod(k, 2) == 0 && cypress < MaxCypress)
                    {
                        if (ctx.PlaceByHeight(Cypress, at, 0f, ctx.Range(0.16f, 0.18f) * s, StandIn.Pine, CypressGreen, 0.25f)) cypress++;
                    }
                    else if (eucalyptus < MaxEucalyptus)
                    {
                        if (ctx.PlaceByHeight(Eucalyptus, at, ctx.Range(0f, 360f), ctx.Range(0.17f, 0.20f) * s, StandIn.Tree, EucalyptusGrey, 0.35f)) eucalyptus++;
                    }
                }
            }
            for (int j = j0; j <= j1; j++)
            {
                if (Mod(j, 3) != 0) continue;
                float z = j * pz + 0.02f * s;
                int k0 = Mathf.FloorToInt((c.x - reach) / (TreeStep * s)), k1 = Mathf.CeilToInt((c.x + reach) / (TreeStep * s));
                for (int k = k0; k <= k1 && eucalyptus < MaxEucalyptus; k++)
                {
                    var at = new Vector3(k * TreeStep * s, c.y, z);
                    if (!ctx.Inside(at, 0.03f * s) || ctx.InUnitSpot(at, 0.05f * s) || NearYard(at) || NearNorthSouthLine(at.x)) continue;
                    if (ctx.PlaceByHeight(Eucalyptus, at, ctx.Range(0f, 360f), ctx.Range(0.17f, 0.20f) * s, StandIn.Tree, EucalyptusGrey, 0.35f)) eucalyptus++;
                }
            }

            // A crossing stays open: no tree within 0.06 S of a line of the other direction.
            bool NearEastWestBreak(float z)
            {
                float r = Mod(Mathf.RoundToInt(z / pz), 3) == 0 ? Mathf.Abs(z - Mathf.Round(z / pz) * pz) : float.MaxValue;
                return r < 0.06f * s;
            }
            bool NearNorthSouthLine(float x) => Mathf.Abs(x - Mathf.Round(x / px) * px) < 0.06f * s;

            // 5. The farmstead yard's paint, last (paints apply in order), so the barn and tower stand on it, not in wheat.
            ctx.PaintGround("dirt", yard, corner, 0.30f * s, 0.22f * s, 0.3f, 0.9f);
        }

        static int Mod(int a, int n) => ((a % n) + n) % n;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
