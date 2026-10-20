#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Terraced hill farming in the Samarian and Judean foothills (1948). Contour terraces ring the hill dome
    /// about the hex centre. Each terrace is a band of ploughed soil (at 0.42, 0.60 and 0.78 S) with a dry-stone riser on
    /// its downhill side. The risers are painted as thin rocky lines, with short runs of limestone boulders where they show.
    /// All the rings share one slightly irregular contour shape, so they read as contours, not circles. Olive trees stand
    /// in rows along the bands, with a few carobs among them. A stone field hut stands on the middle terrace, with its path
    /// down to the hex edge. A track climbs from the south edge to the bare summit, where a unit stands.
    ///
    /// The bands are polylines of PaintStroke at strength 0.85. Paints lerp in turn, so at a lower strength the joints
    /// between strokes would bead visibly.
    ///
    /// Triangle estimate at the maximum counts (capped by counters; FlatSurface refuses nothing), with measured per-model
    /// counts: at most 22 olives x 1,602 = 35,244, at most 4 carobs x 648 = 2,592, at most 12 limestone x 700 = 8,400, and
    /// the hut kit_stone_village_2 x 700. Total at most 46,936 of 50,000.</summary>
    [HexPrototype("olive_terraces")]
    public sealed class OliveTerraces : IHexPrototype
    {
        public string Title => "Olive terraces on a hillside (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 50000;

        // The hut is explicit, not "kit_stone_village*": the wildcard would also pick b17, the village mosque.
        const string Olive = "sandbox:olive_lite", Carob = "sandbox:carob_lite", Stone = "sandbox:limestone*",
            Hut = "sandbox:kit_stone_village_2";
        const int MaxOlives = 22, MaxCarobs = 4, StoneRuns = 4, StonesPerRun = 3, Segments = 24;

        static readonly Color OliveLeaf = new Color(0.38f, 0.44f, 0.28f), CarobLeaf = new Color(0.18f, 0.32f, 0.15f),
            Limestone = new Color(0.80f, 0.76f, 0.66f), HutStone = new Color(0.76f, 0.70f, 0.58f);

        static readonly float[] Bands = { 0.42f, 0.60f, 0.78f };          // terrace soil (tree rows on the inner two, corners on the outer)
        static readonly float[] Risers = { 0.33f, 0.51f, 0.69f, 0.87f };  // dry-stone walls, downhill of each band

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            // One contour shape for every ring: two low harmonics, so the terraces swell and pinch together.
            float phase1 = ctx.Range(0f, 360f), phase2 = ctx.Range(0f, 360f);
            float Wobble(float b) => 1f + 0.05f * Mathf.Sin((b + phase1) * Mathf.Deg2Rad) + 0.03f * Mathf.Sin((2f * b + phase2) * Mathf.Deg2Rad);
            Vector3 On(float r, float b) => ctx.At(b, r * Wobble(b));

            float trackB = ctx.Range(160f, 200f);                                          // the track up from the south
            float hutB = trackB + (ctx.Rng.Next(2) == 0 ? -1f : 1f) * ctx.Range(70f, 110f);  // the hut, off the track
            var hut = On(Bands[1], hutB);

            // 1. Ground: the terrace bands, then the risers, then the track, the hut's path and its yard.
            foreach (float r in Bands) Ring("ploughed", r, 0.12f, 0.85f);
            foreach (float r in Risers) Ring("mountain", r, 0.022f, 0.8f);
            ctx.PaintStroke("dirt", ctx.At(trackB, 0.97f), ctx.Centre, 0.03f * s, 0.9f);
            ctx.PaintStroke("dirt", hut, ctx.At(hutB, 0.97f), 0.02f * s, 0.85f);
            ctx.PaintGround("dirt", hut, hutB, 0.15f * s, 0.15f * s, 0.3f, 0.8f);

            // 2. The field hut, its front down the slope.
            if (!ctx.InUnitSpot(hut, 0.07f * s)) ctx.PlaceByWidth(Hut, hut, hutB, 0.10f * s, StandIn.Box, HutStone);

            // 3. Trees in rows along the bands: the middle terrace first (the most seen), then the upper, then one at each
            //    corner of the outer terrace, where the hexagon has room. Gaps where the track and the hut's path cross.
            int olives = 0, carobs = 0;
            float o1 = ctx.Range(0f, 360f / 13f), o0 = ctx.Range(0f, 40f);
            for (int k = 0; k < 13; k++)
            {
                float b = o1 + k * 360f / 13f;
                if (Mathf.Abs(Mathf.DeltaAngle(b, hutB)) < 16f) continue;
                Tree(Bands[1], b);
            }
            for (int k = 0; k < 9; k++) Tree(Bands[0], o0 + k * 40f);
            for (int k = 0; k < 6; k++)
            {
                float b = k * 60f + ctx.Range(-5f, 5f);
                if (Mathf.Abs(Mathf.DeltaAngle(b, hutB)) < 10f) continue;
                Tree(Bands[2], b);
            }

            // 4. Short runs of limestone where the risers show between the rows.
            for (int run = 0; run < StoneRuns; run++)
            {
                float r = Risers[1 + ctx.Rng.Next(2)], b0 = ctx.Range(0f, 360f);
                if (Mathf.Abs(Mathf.DeltaAngle(b0, trackB)) < 12f) continue;
                for (int i = 0; i < StonesPerRun; i++)
                {
                    var p = On(r, b0 + (i - 1) * 5f);
                    if (ctx.InUnitSpot(p, 0.03f * s)) continue;
                    ctx.PlaceByWidth(Stone, p, ctx.Range(0f, 360f), ctx.Range(0.035f, 0.05f) * s, StandIn.Rock, Limestone);
                }
            }

            // A closed contour ring of one ground layer, as a polyline with a little irregularity at each vertex.
            void Ring(string layer, float r, float widthShare, float strength)
            {
                var pts = new Vector3[Segments];
                for (int k = 0; k < Segments; k++) pts[k] = On(r + ctx.Range(-0.008f, 0.008f), k * 360f / Segments);
                for (int k = 0; k < Segments; k++) ctx.PaintStroke(layer, pts[k], pts[(k + 1) % Segments], widthShare * s, strength);
            }

            // One tree slot on a band: now and then a gap or a carob, otherwise an olive, up to the caps.
            void Tree(float r, float b)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(b, trackB)) < 10f) return;
                var p = On(r, b);
                if (ctx.InUnitSpot(p, 0.07f * s)) return;
                int roll = ctx.Rng.Next(10);
                if (roll == 0) return;
                if (roll == 1 && carobs < MaxCarobs)
                {
                    if (ctx.PlaceByHeight(Carob, p, ctx.Range(0f, 360f), ctx.Range(0.14f, 0.16f) * s, StandIn.Tree, CarobLeaf, 0.5f)) carobs++;
                    return;
                }
                if (olives >= MaxOlives) return;
                if (ctx.PlaceByHeight(Olive, p, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.13f) * s, StandIn.Tree, OliveLeaf, 0.5f)) olives++;
            }
        }
    }
}
