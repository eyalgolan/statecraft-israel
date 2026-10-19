#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Ruined stone arcades on the coast: a generic ruin, not a specific site. Few, large pieces, as Civ VI draws
    /// a tile. Two broken sandstone arcades stand on patches of bare ground, with fallen blocks and boulders round their
    /// feet. If the hex touches the sea, a small timber landing stage stands on the shore (Place refuses water, so it
    /// cannot reach out over the sea). Low scrub and a few date palms dot the sand. The centre stays clear for a unit.
    ///
    /// Every placed footprint (HexScale.PieceScale applied, as Place does) is kept as a turned rectangle, and a piece
    /// goes down only where its rectangle overlaps none of them and stays out of the unit spot, so the tile reads cleanly
    /// with models or with stand-ins.
    ///
    /// Sizes (shares of S, as asked of Place): arcades 0.32 and 0.28 wide, boulders 0.07 to 0.10, landing stage 0.18,
    /// date palms 0.16 to 0.19 tall, scrub 0.08 to 0.10.
    ///
    /// Triangle estimate, at the maximum counts (Higgsfield props, local only, measured): 2 arcades x 7,390 = 14,780 + 4
    /// boulder groups x 1,435 = 5,740 + landing stage 4,428 + 8 scrub x 320 = 2,560 + 3 date palms x 284 = 852. Total
    /// about 28,400 of 35,000.
    ///
    /// Models are tiles:caesarea_ruins/... and the coastal_dunes scrub (local, pending rights); without them the tile draws
    /// Box, Rock, Palm and Tree stand-ins.</summary>
    [HexPrototype("caesarea_ruins", Kind = "nature", Region = "centre")]
    public sealed class CaesareaRuins : IHexPrototype
    {
        public string Title => "Coastal ruins";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "beach";
        public int TriangleBudget => 35000;

        const string Arches = "tiles:caesarea_ruins/arches", Boulders = "tiles:caesarea_ruins/boulders", Jetty = "tiles:caesarea_ruins/jetty",
            Scrub = "tiles:coastal_dunes/scrub", Palm = "sandbox:date_palm";

        static readonly Color Sandstone = new Color(0.80f, 0.68f, 0.50f), Timber = new Color(0.45f, 0.36f, 0.27f), Sage = new Color(0.66f, 0.70f, 0.56f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, vy = ctx.Range(0f, 360f), gap = 0.01f * s, spot = HexScale.UnitSpotShare * s;

            // Placed footprints as turned rectangles in the ground plane: centre, x and z axes, half sizes in metres.
            var taken = new List<(Vector3 at, Vector3 x, Vector3 z, float hx, float hz)>();
            (Vector3 at, Vector3 x, Vector3 z, float hx, float hz) Box(Vector3 at, float yaw, float sideM)
            {
                var q = Quaternion.Euler(0f, yaw, 0f);
                return (at, q * Vector3.right, q * Vector3.forward, 0.5f * HexScale.PieceScale * sideM, 0.5f * HexScale.PieceScale * sideM);
            }
            float Reach((Vector3 at, Vector3 x, Vector3 z, float hx, float hz) p, Vector3 l) => p.hx * Mathf.Abs(Vector3.Dot(p.x, l)) + p.hz * Mathf.Abs(Vector3.Dot(p.z, l));
            bool Apart((Vector3 at, Vector3 x, Vector3 z, float hx, float hz) p, (Vector3 at, Vector3 x, Vector3 z, float hx, float hz) o)
            {
                foreach (var l in new[] { p.x, p.z, o.x, o.z })
                    if (Mathf.Abs(Vector3.Dot(o.at - p.at, l)) > Reach(p, l) + Reach(o, l) + gap) return true;
                return false;
            }
            bool Clear((Vector3 at, Vector3 x, Vector3 z, float hx, float hz) p)
            {
                var toCentre = ctx.Centre - p.at;
                float lx = Mathf.Max(Mathf.Abs(Vector3.Dot(toCentre, p.x)) - p.hx, 0f), lz = Mathf.Max(Mathf.Abs(Vector3.Dot(toCentre, p.z)) - p.hz, 0f);
                if (lx * lx + lz * lz < spot * spot) return false;
                foreach (var o in taken) if (!Apart(p, o)) return false;
                return true;
            }
            bool Put(System.Func<bool> place, (Vector3 at, Vector3 x, Vector3 z, float hx, float hz) p) { if (!Clear(p) || !place()) return false; taken.Add(p); return true; }

            // 1. The landing stage first, as the coastal dunes place their boat: step out along each bearing until the sea
            //    shows 0.12 S ahead (so it starts beyond 0.09 S ahead), then try the stage with its seaward edge 0.01 S short
            //    of that, and up to two steps back. The first success ends the search; no sea near, no stage.
            const float stageShare = 0.18f;
            float stageHalf = 0.5f * HexScale.PieceScale * stageShare;
            bool done = false;
            for (int i = 0; i < 24 && !done; i++)
            {
                float jb = vy + 15f * i;
                for (float r = 0.35f; r <= 0.74f; r += 0.03f)
                {
                    if (!ctx.IsWater(ctx.At(jb, r + 0.12f))) continue;
                    for (int back = 0; back < 3 && !done; back++)
                    {
                        var at = ctx.At(jb, r + 0.09f - stageHalf - 0.01f - 0.03f * back);
                        done = Put(() => ctx.PlaceByWidth(Jetty, at, jb + 90f, stageShare * s, StandIn.Box, Timber), Box(at, jb + 90f, stageShare * s));
                    }
                    break;
                }
            }

            // 2. Two arcades on bare ground, each across its bearing (facing the centre), their fronts at the edge of the
            //    unit spot; their far corners stay inside the inset hexagon at any heading. A coastal hex is part sea, so
            //    each arcade walks round the bearings until it finds dry ground clear of the first.
            int arcades = 0;
            for (int i = 0; i < 24 && arcades < 2; i++)
            {
                float ab = vy + 180f + 15f * i, aw = arcades == 0 ? 0.32f : 0.28f;
                var at = ctx.At(ab, HexScale.UnitSpotShare + 0.01f + 0.5f * HexScale.PieceScale * aw);
                if (!Put(() => ctx.PlaceByWidth(Arches, at, ab + 180f, aw * s, StandIn.Box, Sandstone), Box(at, ab + 180f, aw * s))) continue;
                ctx.PaintGround("dirt", at, ab, aw * 1.5f * s, 0.22f * s, 0.5f, 0.75f);
                arcades++;
            }

            // 3. Fallen blocks and boulders round the ruins.
            int rocks = 0;
            for (int k = 0; k < 60 && rocks < 4; k++)
            {
                var at = ctx.At(vy + ctx.Range(120f, 300f), ctx.Range(0.40f, 0.70f));
                float bw = ctx.Range(0.07f, 0.10f) * s, yaw = ctx.Range(0f, 360f);
                if (Put(() => ctx.PlaceByWidth(Boulders, at, yaw, bw, StandIn.Rock, Sandstone), Box(at, yaw, bw))) rocks++;
            }

            // 4. Scrub and a few date palms on the rest of the sand. Scrub is sought at least its largest turned reach
            //    inside the inset hexagon, so the hexagon never refuses it.
            int palms = 0, scrub = 0;
            for (int k = 0; k < 30 && palms < 3; k++)
            {
                var at = ctx.At(vy + ctx.Range(-60f, 60f), ctx.Range(0.40f, 0.70f));
                float h = ctx.Range(0.16f, 0.19f) * s, yaw = ctx.Range(0f, 360f);
                if (Put(() => ctx.PlaceByHeight(Palm, at, yaw, h, StandIn.Palm, null, 0.3f), Box(at, yaw, h * 0.3f))) palms++;
            }
            for (int k = 0; k < 80 && scrub < 8; k++)
            {
                var at = ctx.RandomPoint(0.71f * HexScale.PieceScale * 0.10f * s);
                float h = ctx.Range(0.08f, 0.10f) * s, yaw = ctx.Range(0f, 360f);
                if (Put(() => ctx.PlaceByHeight(Scrub, at, yaw, h, StandIn.Tree, Sage, 1.0f), Box(at, yaw, h))) scrub++;
            }
        }
    }
}
