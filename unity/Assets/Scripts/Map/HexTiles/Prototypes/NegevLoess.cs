#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The northern Negev's loess plain, 1948: wide, flat, pale-yellow ground (the desert layer, with ploughed at
    /// low strength over it and a faint green tinge of plains in places), softer and greener than desert_edge. A shallow
    /// braided wadi crosses one side of the hex: thin dirt channels that part and rejoin, with a pale bed. Bedouin barley
    /// patches lie about the plain, irregular and partly grown (wheat at partial strength, each patch two offset lobes). A
    /// few acacias and tamarisks stand along the wadi. A small Bedouin tent camp sits at the far edge, with up to two camels.
    /// No town is drawn and no place is named; the unit spot stays open.
    ///
    /// Camels: the local model proto:figure-dromedary-camel-1948-v1 measures 29,451 triangles (counted from its FBX), so two
    /// would be double the 30,000 cap and one leaves no headroom. The camels are therefore stand-ins,
    /// tiles:negev_loess/camel (Figure, camel tan), ready for a light model (the sheep-lite figure is 2,500).
    ///
    /// Stand-ins: tiles:negev_loess/tent (two or three dark Box stand-ins in a row per tent), tiles:negev_loess/camel and
    /// tiles:negev_loess/tamarisk (Tree, grey-green). Every piece keeps a clearance of about 0.95 x its asked width (PieceScale
    /// 1.35) from earlier pieces and from the unit spot.
    ///
    /// Worst-case triangles on a flat surface (caps below; the acacia counted from its OBJ file on 19 October 2026):
    /// 4 acacias x 1,164 = 4,656. 3 tamarisk Tree stand-ins x 56 = 168. 9 tent boxes x 16 = 144. 2 camel Figure stand-ins
    /// (a capsule) x ~830 = 1,660. Total about 6,630; budget 8,000.</summary>
    [HexPrototype("negev_loess", Kind = "nature", Region = "south")]
    public sealed class NegevLoess : IHexPrototype
    {
        public string Title => "Negev loess plain: barley patches, braided wadi, Bedouin camp (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "desert";
        public int TriangleBudget => 8000;

        const string Acacia = "sandbox:acacia_lite", Tamarisk = "tiles:negev_loess/tamarisk", Tent = "tiles:negev_loess/tent",
            Camel = "tiles:negev_loess/camel";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxAcacias = 4, MaxTamarisks = 3, MaxTents = 3, MaxCamels = 2, MaxPatches = 6;

        static readonly Color AcaciaGreen = new Color(0.48f, 0.50f, 0.30f), TamariskGrey = new Color(0.55f, 0.60f, 0.50f),
            TentBlack = new Color(0.18f, 0.15f, 0.13f), CamelTan = new Color(0.74f, 0.60f, 0.42f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. Loess: the desert layer to the rim, ploughed over it at low strength, and three faint plains patches.
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.8f * s, 2.0f * s, 0.2f, 0.8f);
            ctx.PaintGround("ploughed", ctx.Centre, ctx.Range(0f, 180f), 1.5f * s, 1.6f * s, 0.4f, 0.3f);
            for (int k = 0; k < 3; k++)
                ctx.PaintGround("plains", ctx.RandomPoint(0.2f * s), ctx.Range(0f, 180f), ctx.Range(0.4f, 0.6f) * s, ctx.Range(0.3f, 0.45f) * s, 0.5f, 0.3f);

            // 2. The braided wadi: a main channel across one side (0.40 S off the centre), with two or three braids that part
            //    from it and rejoin (a sine lens between two points), all thin and shallow.
            float wadiB = ctx.Range(0f, 360f);
            var normal = Flat(ctx.At(wadiB, 1f) - ctx.Centre).normalized;
            var along = new Vector3(normal.z, 0f, -normal.x);
            float offset = ctx.Range(0.38f, 0.44f) * s, amp = ctx.Range(0.03f, 0.05f) * s, phase = ctx.Range(0f, 6.283f);
            Vector3 Main(float t) => ctx.Centre + normal * (offset + amp * Mathf.Sin(3f * t + phase)) + along * (t * s);
            var channels = new List<List<Vector3>>();
            var main = new List<Vector3>();
            for (int k = 0; k <= 20; k++) main.Add(Main(-1f + 0.1f * k));
            channels.Add(main);
            int braids = 2 + ctx.Rng.Next(2);
            for (int j = 0; j < braids; j++)
            {
                float t0 = ctx.Range(-0.9f, 0.3f), t1 = t0 + ctx.Range(0.3f, 0.6f), lens = (j % 2 == 0 ? 1f : -1f) * ctx.Range(0.04f, 0.07f) * s;
                var braid = new List<Vector3>();
                for (int k = 0; k <= 8; k++)
                {
                    float u = k / 8f;
                    braid.Add(Main(Mathf.Lerp(t0, t1, u)) + normal * (lens * Mathf.Sin(Mathf.PI * u)));
                }
                channels.Add(braid);
            }
            foreach (var ch in channels)
                for (int k = 0; k < ch.Count - 1; k++) ctx.PaintStroke("dirt", ch[k], ch[k + 1], 0.024f * s, 0.6f);
            for (int k = 0; k < main.Count - 1; k++) ctx.PaintStroke("beach", main[k], main[k + 1], 0.012f * s, 0.5f);
            bool NearWadi(Vector3 p, float within)
            {
                foreach (var ch in channels)
                    for (int k = 0; k < ch.Count - 1; k++) if (DistToSegment(p, ch[k], ch[k + 1]) < within) return true;
                return false;
            }

            // Pieces already down, as circles (centre, radius in metres), so later pieces keep off them.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }

            // 3. The tent camp on the far side from the wadi, at 0.62 S: trampled ground, then up to three tents in a loose
            //    row along the edge, each two or three dark boxes along its heading, and up to two camels just inside.
            float campB = wadiB + 180f + ctx.Range(-30f, 30f);
            var camp = ctx.At(campB, 0.62f);
            var radial = Flat(camp - ctx.Centre).normalized;
            var contour = new Vector3(radial.z, 0f, -radial.x);
            ctx.PaintGround("dirt", camp, campB, 0.55f * s, 0.20f * s, 0.5f, 0.45f);
            float[] stagger = { 0.02f, -0.03f, 0.03f };
            for (int j = 0; j < MaxTents; j++)
            {
                int boxes = 2 + ctx.Rng.Next(2);
                float w = ctx.Range(0.042f, 0.05f) * s, turn = ctx.Range(-10f, 10f);
                var centre = camp + contour * ((j - 1) * 0.26f * s) + radial * (stagger[j] * s);
                var dir = Quaternion.Euler(0f, turn, 0f) * contour;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                for (int b = 0; b < boxes; b++)
                {
                    var at = centre + dir * ((b - 0.5f * (boxes - 1)) * 1.04f * 1.35f * w);
                    if (ctx.InUnitSpot(at, 0.95f * w) || NearWadi(at, w)) continue;
                    if (ctx.PlaceByWidth(Tent, at, yaw, w, StandIn.Box, TentBlack)) taken.Add((at, 0.95f * w));
                }
            }
            int camels = 0;
            for (int i = 0; i < 10 && camels < MaxCamels; i++)
            {
                var at = camp - radial * (ctx.Range(0.12f, 0.17f) * s) + contour * (ctx.Range(-0.25f, 0.25f) * s);
                float h = ctx.Range(0.07f, 0.08f) * s, r = 0.075f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Camel, at, ctx.Range(0f, 360f), h, StandIn.Figure, CamelTan, footprintShare: 1.0f)) { taken.Add((at, r)); camels++; }
            }

            // 4. Barley patches: irregular, partly grown, each two offset lobes of wheat at partial strength, off the wadi
            //    and the camp. Paint only, so they may run under the unit spot.
            int patches = 0;
            for (int i = 0; i < 30 && patches < MaxPatches; i++)
            {
                var at = ctx.RandomPoint(0.12f * s);
                if (NearWadi(at, 0.12f * s) || Flat(at - camp).magnitude < 0.35f * s) continue;
                float yaw = ctx.Range(0f, 180f), w = ctx.Range(0.18f, 0.28f) * s, d = ctx.Range(0.10f, 0.17f) * s;
                ctx.PaintGround("wheat", at, yaw, w, d, 0.35f, ctx.Range(0.45f, 0.6f));
                var lobe = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.45f * w, 0f, ctx.Range(-0.3f, 0.3f) * d);
                ctx.PaintGround("wheat", lobe, yaw + ctx.Range(-25f, 25f), 0.6f * w, 0.8f * d, 0.4f, ctx.Range(0.4f, 0.55f));
                patches++;
            }

            // 5. Trees along the wadi banks, alternating sides: acacias and tamarisks, a few only.
            int acacias = 0, tamarisks = 0;
            for (int k = 1; k < main.Count - 1; k += 2)
            {
                if (acacias >= MaxAcacias && tamarisks >= MaxTamarisks) break;
                var dirK = Flat(main[k + 1] - main[k]).normalized;
                var bank = new Vector3(dirK.z, 0f, -dirK.x) * ((k / 2) % 2 == 0 ? 1f : -1f);
                var at = main[k] + bank * (ctx.Range(0.08f, 0.10f) * s);
                // Two acacias to one tamarisk along the bank, switching kind when one has reached its cap.
                bool acacia = (k / 2) % 3 != 1;
                if (acacia && acacias >= MaxAcacias) acacia = false;
                else if (!acacia && tamarisks >= MaxTamarisks) acacia = true;
                float h = acacia ? ctx.Range(0.12f, 0.14f) * s : ctx.Range(0.10f, 0.12f) * s, r = acacia ? 0.08f * s : 0.065f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                bool ok = acacia
                    ? ctx.PlaceByHeight(Acacia, at, ctx.Range(0f, 360f), h, StandIn.Tree, AcaciaGreen, footprintShare: 0.6f)
                    : ctx.PlaceByHeight(Tamarisk, at, ctx.Range(0f, 360f), h, StandIn.Tree, TamariskGrey, footprintShare: 0.6f);
                if (!ok) continue;
                taken.Add((at, r));
                if (acacia) acacias++; else tamarisks++;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = Flat(b - a), ap = Flat(p - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (ap - ab * t).magnitude;
        }
    }
}
