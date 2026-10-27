#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A village flour mill with threshing floors and terraced fields: a generic village scene, not a specific
    /// place. Few, large pieces, as Civ VI draws a tile. A seasonal stream bed (painted) crosses the hex 0.35 S from the
    /// centre, and a squat stone mill stands on its far bank, parallel to it. Two round threshing floors sit on open
    /// ground beside wheat stubble, a stone well with a trough stands near them, and two sycamore figs give shade. On the
    /// far side, short dry-stone terrace walls, each on a painted wall line, run along the contour with strips of tilled
    /// soil above them and a few olives behind. The centre stays clear for a unit.
    ///
    /// Every placed footprint (HexScale.PieceScale applied, as Place does) is kept as a turned rectangle, and a piece
    /// goes down only where its rectangle overlaps none of them, stays out of the unit spot and keeps off the stream bed,
    /// so the tile reads cleanly with models or with stand-ins.
    ///
    /// Sizes (shares of S, as asked of Place): mill 0.27 wide, threshing floors 0.18 and 0.16, terrace walls 0.10, well
    /// 0.12, sycamore figs 0.22 to 0.24 tall, olives 0.15 to 0.17.
    ///
    /// Triangle estimate, at the maximum counts (Higgsfield props, local only, measured): mill 5,853 + 2 threshing floors
    /// x 3,883 = 7,766 + 5 terrace walls x 3,816 = 19,080 + well 4,768 + 2 sycamores x 3,218 = 6,436 + 4 olives x 1,600 =
    /// 6,400. Total about 50,300 of 60,000.
    ///
    /// Models are tiles:village_mill/... and the railway_halt well (local, pending rights); without them the tile draws Box and Tree stand-ins.</summary>
    [HexPrototype("village_mill", Kind = "village")]
    public sealed class VillageMill : IHexPrototype
    {
        public string Title => "Village mill, threshing floors and terraces";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "hills";
        public int TriangleBudget => 60000;

        const string Mill = "tiles:village_mill/mill", Floor = "tiles:village_mill/threshing_floor", Wall = "tiles:village_mill/terrace_wall",
            Well = "tiles:railway_halt/well", Sycamore = "tiles:village_mill/sycamore_fig", Olive = "sandbox:olive_lite";

        static readonly Color Limestone = new Color(0.82f, 0.76f, 0.64f), Earth = new Color(0.72f, 0.60f, 0.42f),
            FigLeaf = new Color(0.22f, 0.36f, 0.18f), OliveLeaf = new Color(0.42f, 0.50f, 0.34f);

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

            // 1. The stream bed: a painted chord between bearings vy - 66 and vy + 66 at 0.86 S, 0.35 S from the centre, so
            //    the far bank has room for the mill. The mill stands there, parallel to it, its front just clear of the bed;
            //    its far corners stay inside the inset hexagon at any heading.
            var a = ctx.At(vy - 66f, 0.86f); var b = ctx.At(vy + 66f, 0.86f);
            var n = (ctx.At(vy, 1f) - ctx.Centre) / s;   // across the stream bed, towards the far bank
            float wadiD = Vector3.Dot(a - ctx.Centre, n), bed = 0.035f * s;
            bool OffWadi((Vector3 at, Vector3 x, Vector3 z, float hx, float hz) p) => Mathf.Abs(Vector3.Dot(p.at - ctx.Centre, n) - wadiD) >= bed + Reach(p, n);
            ctx.PaintStroke("dirt", a, b, 2f * bed, 0.9f);
            float millW = 0.27f * s;
            var mill = ctx.Centre + n * (wadiD + bed + gap + 0.5f * HexScale.PieceScale * millW);
            Put(() => ctx.PlaceByWidth(Mill, mill, vy + 180f, millW, StandIn.Box, Limestone), Box(mill, vy + 180f, millW));

            // 2. Threshing floors on open ground, wheat stubble painted beside them, the well nearby.
            ctx.PaintGround("wheat", ctx.At(vy + 165f, 0.58f), vy + 165f, 0.40f * s, 0.22f * s, 0.5f, 0.7f);
            foreach (var (fb, fr, fw) in new[] { (150f, 0.50f, 0.18f), (185f, 0.62f, 0.16f) })
            {
                var f = ctx.At(vy + fb, fr);
                float yaw = ctx.Range(0f, 360f);
                var foot = Box(f, yaw, fw * s);
                if (OffWadi(foot)) Put(() => ctx.PlaceByWidth(Floor, f, yaw, fw * s, StandIn.Box, Earth), foot);
            }
            var w = ctx.At(vy + 100f, 0.48f);
            float wellW = 0.12f * s;
            var wellFoot = Box(w, vy + 100f, wellW);
            if (OffWadi(wellFoot)) Put(() => ctx.PlaceByWidth(Well, w, vy + 100f, wellW, StandIn.Box, Limestone), wellFoot);

            // 3. Sycamore figs for shade, one on the near bank and one by the well, off the stream bed.
            int figs = 0;
            foreach (var at in new[] { ctx.At(vy + 75f, 0.60f), ctx.At(vy + 120f, 0.65f) })
            {
                float h = ctx.Range(0.22f, 0.24f) * s, yaw = ctx.Range(0f, 360f);
                var foot = Box(at, yaw, h * 0.6f);
                if (figs < 2 && OffWadi(foot) && Put(() => ctx.PlaceByHeight(Sycamore, at, yaw, h, StandIn.Tree, FigLeaf, 0.6f), foot)) figs++;
            }

            // 4. Terraces on the far side: short dry-stone walls along the contour (yaw = bearing), each on a painted wall
            //    line so the terrace reads with stand-ins too, a tilled strip painted above each, and olives behind.
            float wallW = 0.10f * s;
            for (int k = 0; k < 5; k++)
            {
                float tb = vy + 215f + 20f * k, r = k % 2 == 0 ? 0.42f : 0.54f;
                var at = ctx.At(tb, r);
                ctx.PaintGround("dirt", at, tb, 0.24f * s, 0.03f * s, 0.4f, 0.9f);
                ctx.PaintGround("ploughed", ctx.At(tb, r + 0.06f), tb, 0.26f * s, 0.07f * s, 0.4f, 0.6f);
                var foot = Box(at, tb, wallW);
                // The wall model is long along its local z, so it turns a further 90 to lie along the contour (the
                //    footprint is square, so the spacing test is unchanged).
                if (OffWadi(foot)) Put(() => ctx.PlaceByWidth(Wall, at, tb + 90f, wallW, StandIn.Box, Limestone), foot);
            }
            int olives = 0;
            for (int k = 0; k < 60 && olives < 4; k++)
            {
                var at = ctx.At(vy + ctx.Range(205f, 290f), ctx.Range(0.62f, 0.70f));
                float h = ctx.Range(0.15f, 0.17f) * s, yaw = ctx.Range(0f, 360f);
                var foot = Box(at, yaw, h * 0.5f);
                if (OffWadi(foot) && Put(() => ctx.PlaceByHeight(Olive, at, yaw, h, StandIn.Tree, OliveLeaf, 0.5f, 40f), foot)) olives++;
            }
        }
    }
}
