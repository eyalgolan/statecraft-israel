#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A small railway halt with a packing house beside the line: a generic coastal-plain scene, not a specific
    /// place. Few, large pieces, as Civ VI draws a tile. A single track crosses the hex as a chord 0.31 S out from the
    /// centre: a painted ballast strip with a dashed dark rail bed of short flats along it. The stone station building
    /// stands on the far side of the track, parallel to it and facing it, with a painted yard in front; it slides along
    /// the track if the ground refuses the first spot. On the near side, a packing house faces the centre with two crate
    /// stacks at each front corner and a few citrus trees to either side behind it. A stone well with a trough and a
    /// short windbreak row of eucalyptus, kept off the track, finish the hex. The centre stays clear for a unit.
    ///
    /// Every placed footprint (HexScale.PieceScale applied, as Place does) is kept as a turned rectangle, and a piece
    /// goes down only where its rectangle overlaps none of them, stays out of the unit spot and, for trees, off the track,
    /// so the tile reads cleanly with models or with stand-ins.
    ///
    /// Sizes (shares of S, as asked of Place): station 0.30 wide, packing house 0.30, crates 0.07, well 0.12, eucalyptus
    /// 0.26 to 0.30 tall, citrus 0.11 to 0.13.
    ///
    /// Triangle estimate, at the maximum counts (Higgsfield props, local only, measured): station 6,618 + packing house
    /// 3,653 + 4 crate stacks x 1,289 = 5,156 + well 4,768 + 3 eucalyptus x 6,423 = 19,269 + 3 citrus x 1,600 = 4,800
    /// + 10 flats x 12 = 120. Total about 44,400 of 50,000.
    ///
    /// Models are tiles:railway_halt/... (local, pending rights); without them the tile draws Box and Tree stand-ins.</summary>
    [HexPrototype("railway_halt", Kind = "farm", Region = "centre")]
    public sealed class RailwayHalt : IHexPrototype
    {
        public string Title => "Railway halt and packing house";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 50000;

        const string Station = "tiles:railway_halt/station", PackingHouse = "tiles:railway_halt/packing_house", Crates = "tiles:railway_halt/crates",
            Well = "tiles:railway_halt/well", Eucalyptus = "tiles:railway_halt/eucalyptus", Citrus = "sandbox:citrus_lite";

        static readonly Color Stone = new Color(0.84f, 0.79f, 0.68f), Whitewash = new Color(0.92f, 0.91f, 0.86f),
            Timber = new Color(0.55f, 0.42f, 0.28f), RailBed = new Color(0.30f, 0.27f, 0.24f),
            GumLeaf = new Color(0.48f, 0.56f, 0.46f), CitrusLeaf = new Color(0.26f, 0.42f, 0.22f);

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

            // 1. The track: a chord between bearings vy - 69 and vy + 69 at 0.86 S, so it passes 0.31 S from the centre and
            //    leaves the far side room for the station. The rail bed is short flats along it (yaw vy runs their width
            //    along the chord); flats may run beside the unit spot.
            var a = ctx.At(vy - 69f, 0.86f); var b = ctx.At(vy + 69f, 0.86f);
            var n = (ctx.At(vy, 1f) - ctx.Centre) / s;            // across the track, towards the far side
            var along = (ctx.At(vy + 90f, 1f) - ctx.Centre) / s;  // along the track
            float trackD = Vector3.Dot(a - ctx.Centre, n), ballast = 0.035f * s;
            bool OffTrack((Vector3 at, Vector3 x, Vector3 z, float hx, float hz) p) => Mathf.Abs(Vector3.Dot(p.at - ctx.Centre, n) - trackD) >= ballast + Reach(p, n);
            ctx.PaintStroke("dirt", a, b, 2f * ballast, 0.9f);
            for (int k = 0; k < 10; k++)
                ctx.PlaceFlat(Vector3.Lerp(a, b, 0.12f + 0.76f * k / 9f), vy, 0.10f * s, 0.022f * s, RailBed);

            // 2. The station on the far side, parallel to the track and facing it, its front just clear of the ballast;
            //    its far corners stay inside the inset hexagon at any heading. It slides along the track only when the
            //    ground refuses the first spot. Its yard is painted along its front.
            float stationW = 0.30f * s, stationAcross = trackD + ballast + gap + 0.5f * HexScale.PieceScale * stationW;
            foreach (float off in new[] { 0f, -0.06f, 0.06f, -0.12f, 0.12f })
            {
                var st = ctx.Centre + n * stationAcross + along * (off * s);
                if (!Put(() => ctx.PlaceByWidth(Station, st, vy + 180f, stationW, StandIn.Box, Stone), Box(st, vy + 180f, stationW))) continue;
                ctx.PaintGround("dirt", ctx.Centre + n * (trackD + ballast + 0.01f * s) + along * (off * s), vy, 0.44f * s, 0.05f * s, 0.4f, 0.7f);
                break;
            }

            // 3. The packing house on the near side, facing the centre with its front at the edge of the unit spot, and two
            //    crate stacks beside each front corner (turned up to 20 degrees, so set out by their turned reach).
            float pb = vy + 125f, houseW = 0.30f * s, houseHalf = 0.5f * HexScale.PieceScale * houseW, crateW = 0.07f * s;
            var u = (ctx.At(pb, 1f) - ctx.Centre) / s; var v = (ctx.At(pb + 90f, 1f) - ctx.Centre) / s;
            var ph = ctx.Centre + u * (spot + gap + houseHalf);
            if (Put(() => ctx.PlaceByWidth(PackingHouse, ph, pb + 180f, houseW, StandIn.Box, Whitewash), Box(ph, pb + 180f, houseW)))
            {
                ctx.PaintGround("dirt", ctx.Centre + u * (spot + 0.03f * s), pb, 0.62f * s, 0.08f * s, 0.4f, 0.7f);
                float side = houseHalf + 0.5f * HexScale.PieceScale * crateW * 1.29f + gap + 0.005f * s;
                foreach (var (row, sign) in new[] { (0f, 1f), (0f, -1f), (1f, 1f), (1f, -1f) })
                {
                    var c = ctx.Centre + u * (spot + gap + (0.07f + 0.14f * row) * s) + v * (sign * side);
                    float yaw = pb + ctx.Range(-20f, 20f);
                    Put(() => ctx.PlaceByWidth(Crates, c, yaw, crateW, StandIn.Box, Timber), Box(c, yaw, crateW));
                }
            }

            // 4. A few citrus trees to either side behind the packing house.
            int citrus = 0;
            for (int k = 0; k < 40 && citrus < 3; k++)
            {
                float sign = k % 2 == 0 ? 1f : -1f;
                var c = ctx.At(pb + sign * ctx.Range(30f, 62f), ctx.Range(0.52f, 0.68f));
                float h = ctx.Range(0.11f, 0.13f) * s, yaw = ctx.Range(0f, 360f);
                var foot = Box(c, yaw, h * 0.6f);
                if (OffTrack(foot) && Put(() => ctx.PlaceByHeight(Citrus, c, yaw, h, StandIn.Tree, CitrusLeaf, 0.6f), foot)) citrus++;
            }

            // 5. The well and trough, and a short windbreak row of eucalyptus, kept off the track.
            var w = ctx.At(vy + 235f, 0.48f);
            float wellW = 0.12f * s;
            Put(() => ctx.PlaceByWidth(Well, w, vy + 235f, wellW, StandIn.Box, Stone), Box(w, vy + 235f, wellW));
            int gums = 0;
            for (int k = 0; k < 10 && gums < 3; k++)
            {
                var e = ctx.At(vy + 250f + 17f * k, 0.66f);
                float h = ctx.Range(0.26f, 0.30f) * s, yaw = ctx.Range(0f, 360f);
                var foot = Box(e, yaw, h * 0.4f);
                if (OffTrack(foot) && Put(() => ctx.PlaceByHeight(Eucalyptus, e, yaw, h, StandIn.Tree, GumLeaf, 0.4f), foot)) gums++;
            }
        }
    }
}
