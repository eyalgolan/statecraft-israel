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
    /// the hut kit_stone_village_2 x 700. Total at most 46,936 of 50,000.
    ///
    /// The regional variants (OliveTerraces&lt;Region&gt;&lt;A|B&gt;) subclass it through the protected virtual members: the
    /// bands and risers, their layers, the trees per ring, the species and the caps. Their defaults are this tile's own,
    /// so olive_terraces itself builds exactly as before (HexVariantTests).</summary>
    [HexPrototype("olive_terraces", Kind = "farm")]
    public class OliveTerraces : IHexPrototype
    {
        public virtual string Title => "Olive terraces on a hillside (1948)";
        public virtual HexTerrain Terrain => HexTerrain.Hills;
        public virtual string Ground => "hills";
        public virtual int TriangleBudget => 60000;   // its farmhouse at full detail: 51k measured

        // The hut is explicit, not "kit_stone_village*": the wildcard would also pick b17, the village mosque.
        protected const string Olive = "sandbox:olive_lite", Carob = "sandbox:carob_lite", Stone = "sandbox:limestone*",
            Hut = "sandbox:kit_stone_village_2";
        const int Segments = 24;

        protected static readonly Color OliveLeaf = new Color(0.38f, 0.44f, 0.28f), CarobLeaf = new Color(0.18f, 0.32f, 0.15f),
            Limestone = new Color(0.80f, 0.76f, 0.66f), HutStone = new Color(0.76f, 0.70f, 0.58f);

        static readonly float[] BaseBands = { 0.42f, 0.60f, 0.78f };          // terrace soil (tree rows on the inner two, corners on the outer)
        static readonly float[] BaseRisers = { 0.33f, 0.51f, 0.69f, 0.87f };  // dry-stone walls, downhill of each band

        /// <summary>Terrace bands and the dry-stone risers downhill of them, as shares of S from the centre. The tree rows
        /// stand on Bands[0], Bands[1] and Bands[2]; the stone runs on Risers[1] and Risers[2].</summary>
        protected virtual float[] Bands => BaseBands;
        protected virtual float[] Risers => BaseRisers;
        protected virtual string BandLayer => "ploughed";
        protected virtual float BandWidth => 0.12f;
        protected virtual string RiserLayer => "mountain";
        /// <summary>Trees on the middle band, the upper band and the outer band's corners.</summary>
        protected virtual int MiddleRow => 13;
        protected virtual int UpperRow => 9;
        protected virtual int OuterCorners => 6;
        protected virtual int MaxOlives => 22;
        protected virtual int MaxCarobs => 4;
        protected virtual int StoneRuns => 4;
        /// <summary>A tree slot rolls 0 to 9: below GapRolls it stays empty, the next CarobRolls take the second tree.</summary>
        protected virtual int GapRolls => 1;
        protected virtual int CarobRolls => 1;
        /// <summary>The main row tree (an olive) and the second tree (a carob), with their height ranges in shares of S.</summary>
        protected virtual (string model, StandIn standIn, Color tint, float min, float max) RowTree => (Olive, StandIn.Tree, OliveLeaf, 0.12f, 0.13f);
        protected virtual (string model, StandIn standIn, Color tint, float min, float max) SecondTree => (Carob, StandIn.Tree, CarobLeaf, 0.14f, 0.16f);
        /// <summary>Whether the field hut stands on the middle terrace.</summary>
        protected virtual bool HasHut => true;

        const int StonesPerRun = 3;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float[] bands = Bands, risers = Risers;
            int maxOlives = MaxOlives, maxCarobs = MaxCarobs, gapRolls = GapRolls, carobRolls = CarobRolls;
            var rowTree = RowTree; var second = SecondTree;
            // One contour shape for every ring: two low harmonics, so the terraces swell and pinch together.
            float phase1 = ctx.Range(0f, 360f), phase2 = ctx.Range(0f, 360f);
            float Wobble(float b) => 1f + 0.05f * Mathf.Sin((b + phase1) * Mathf.Deg2Rad) + 0.03f * Mathf.Sin((2f * b + phase2) * Mathf.Deg2Rad);
            Vector3 On(float r, float b) => ctx.At(b, r * Wobble(b));

            float trackB = ctx.Range(160f, 200f);                                          // the track up from the south
            float hutB = trackB + (ctx.Rng.Next(2) == 0 ? -1f : 1f) * ctx.Range(70f, 110f);  // the hut, off the track
            var hut = On(bands[1], hutB);

            // 1. Ground: the terrace bands, then the risers, then the track, the hut's path and its yard.
            foreach (float r in bands) Ring(BandLayer, r, BandWidth, 0.85f);
            foreach (float r in risers) Ring(RiserLayer, r, 0.022f, 0.8f);
            ctx.PaintStroke("dirt", ctx.At(trackB, 0.97f), ctx.Centre, 0.03f * s, 0.9f);
            if (HasHut)
            {
                ctx.PaintStroke("dirt", hut, ctx.At(hutB, 0.97f), 0.02f * s, 0.85f);
                ctx.PaintGround("dirt", hut, hutB, 0.15f * s, 0.15f * s, 0.3f, 0.8f);

                // 2. The field hut, its front down the slope.
                if (!ctx.InUnitSpot(hut, 0.07f * s)) ctx.PlaceByWidth(Hut, hut, hutB, 0.10f * s, StandIn.Box, HutStone);
            }

            // 3. Trees in rows along the bands: the middle terrace first (the most seen), then the upper, then one at each
            //    corner of the outer terrace, where the hexagon has room. Gaps where the track and the hut's path cross.
            int olives = 0, carobs = 0, middle = MiddleRow, upper = UpperRow, corners = OuterCorners;
            float o1 = ctx.Range(0f, 360f / middle), o0 = ctx.Range(0f, 360f / upper);
            for (int k = 0; k < middle; k++)
            {
                float b = o1 + k * 360f / middle;
                if (HasHut && Mathf.Abs(Mathf.DeltaAngle(b, hutB)) < 16f) continue;
                Tree(bands[1], b);
            }
            for (int k = 0; k < upper; k++) Tree(bands[0], o0 + k * (360f / upper));
            for (int k = 0; k < corners; k++)
            {
                float b = k * (360f / corners) + ctx.Range(-5f, 5f);
                if (HasHut && Mathf.Abs(Mathf.DeltaAngle(b, hutB)) < 10f) continue;
                Tree(bands[2], b);
            }

            // 4. Short runs of limestone where the risers show between the rows.
            for (int run = 0; run < StoneRuns; run++)
            {
                float r = risers[1 + ctx.Rng.Next(2)], b0 = ctx.Range(0f, 360f);
                if (Mathf.Abs(Mathf.DeltaAngle(b0, trackB)) < 12f) continue;
                for (int i = 0; i < StonesPerRun; i++)
                {
                    var p = On(r, b0 + (i - 1) * 5f);
                    if (ctx.InUnitSpot(p, 0.03f * s)) continue;
                    ctx.PlaceByWidth(Stone, p, ctx.Range(0f, 360f), ctx.Range(0.035f, 0.05f) * s, StandIn.Rock, Limestone);
                }
            }
            Dress(ctx);

            // A closed contour ring of one ground layer, as a polyline with a little irregularity at each vertex.
            void Ring(string layer, float r, float widthShare, float strength)
            {
                var pts = new Vector3[Segments];
                for (int k = 0; k < Segments; k++) pts[k] = On(r + ctx.Range(-0.008f, 0.008f), k * 360f / Segments);
                for (int k = 0; k < Segments; k++) ctx.PaintStroke(layer, pts[k], pts[(k + 1) % Segments], widthShare * s, strength);
            }

            // One tree slot on a band: now and then a gap or the second tree, otherwise the row tree, up to the caps.
            void Tree(float r, float b)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(b, trackB)) < 10f) return;
                var p = On(r, b);
                if (ctx.InUnitSpot(p, 0.07f * s)) return;
                int roll = ctx.Rng.Next(10);
                if (roll < gapRolls) return;
                if (roll < gapRolls + carobRolls && carobs < maxCarobs)
                {
                    if (ctx.PlaceByHeight(second.model, p, ctx.Range(0f, 360f), ctx.Range(second.min, second.max) * s, second.standIn, second.tint, 0.5f)) carobs++;
                    return;
                }
                if (olives >= maxOlives) return;
                if (ctx.PlaceByHeight(rowTree.model, p, ctx.Range(0f, 360f), ctx.Range(rowTree.min, rowTree.max) * s, rowTree.standIn, rowTree.tint, 0.5f)) olives++;
            }
        }

        /// <summary>Anything a variant adds last: nothing here.</summary>
        protected virtual void Dress(HexTileContext ctx) { }
    }
}
