#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The coastal sand dunes north of Tel Aviv, 1948. Large drifting dunes are drawn as pale and warm sand flats
    /// of varied size and heading that never overlap. Low retama-like white-broom scrub is scattered over them, with one
    /// small clump of date palms. If the hex touches the sea, a fishing boat is pulled up on the dry sand just short of the
    /// water. The tile is light, so a whole coast can wear it.
    ///
    /// Triangle estimate, at the maximum counts, with measured per-model counts: fishing boat x 13,437 (only on a hex that
    /// touches water, so the FlatSurface test never places it). 4 date palms x 284 = 1,136. 16 scrub stand-ins x about 46 =
    /// 740. 12 flats x 12 = 144. Total about 15,500 of 20,000 with the boat.
    ///
    /// The scrub has no model (tiles:coastal_dunes/scrub), so it draws the Tree stand-in tinted pale sage.</summary>
    [HexPrototype("coastal_dunes")]
    public sealed class CoastalDunes : IHexPrototype
    {
        public string Title => "Coastal dunes (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "beach";
        public int TriangleBudget => HexScale.BudgetFields;

        const string Boat = "proto:vehicle-fishing-boat-1948-v2", Scrub = "tiles:coastal_dunes/scrub", Palm = "sandbox:date_palm";
        // One clearly lighter and one warmer, darker sand, so the dunes read against the beach ground from far away.
        static readonly Color[] Sand = { new Color(0.98f, 0.95f, 0.86f), new Color(0.82f, 0.70f, 0.50f), new Color(0.95f, 0.90f, 0.78f) };
        static readonly Color Sage = new Color(0.66f, 0.70f, 0.56f), Hull = new Color(0.45f, 0.33f, 0.22f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            // Pieces already down, as circles (centre, radius in metres), so the dunes never overlap each other or the boat.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r) { foreach (var t in taken) if (Vector2.Distance(new Vector2(at.x, at.z), new Vector2(t.at.x, t.at.z)) < r + t.r) return false; return true; }

            // 1. The boat, first: step out along each bearing until the sea shows about 0.1 S ahead, then try that spot and
            //    up to two steps back. The first success ends the search; no water near, no boat.
            float start = ctx.Range(0f, 360f);
            bool boat = false;
            var boatAt = Vector3.zero;
            bool OffBoat(Vector3 at, float r) => !boat || Vector2.Distance(new Vector2(at.x, at.z), new Vector2(boatAt.x, boatAt.z)) >= r + 0.11f * s;
            for (int i = 0; i < 24 && !boat; i++)
            {
                float b = start + 15f * i;
                for (float r = 0.30f; r <= 0.80f; r += 0.03f)
                {
                    if (!ctx.IsWater(ctx.At(b, r + 0.10f))) continue;
                    for (int back = 0; back < 3 && !boat; back++)
                    {
                        var at = ctx.At(b, r - 0.03f * back);
                        if (ctx.PlaceByWidth(Boat, at, b + ctx.Range(-15f, 15f), 0.14f * s, StandIn.Box, Hull))
                        {
                            taken.Add((at, 0.11f * s));
                            boatAt = at;
                            boat = true;
                        }
                    }
                    break;
                }
            }

            // 2. Dunes: large sand flats, longer than wide, at random headings, placed only where they overlap nothing.
            int dunes = 0;
            for (int k = 0; k < 70 && dunes < 12; k++)
            {
                float w = ctx.Range(0.16f, 0.30f) * s, d = w * ctx.Range(0.45f, 0.7f), yaw = ctx.Range(0f, 180f);
                var at = ctx.RandomPoint(0.04f * s);
                // Dunes are ground, not objects: soft blobs of pale sand painted into the beach, a few greener hollows.
                string layer = ctx.Rng.Next(Sand.Length) == 1 ? "plains" : "desert";
                float r = 0.5f * Mathf.Sqrt(w * w + d * d);
                if (!Free(at, r)) continue;
                ctx.PaintGround(layer, at, yaw, w, d, 0.45f, layer == "plains" ? 0.55f : 0.85f);
                taken.Add((at, r)); dunes++;
            }

            // 3. A small clump of date palms, kept off the boat.
            var grove = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.2f, 0.45f));
            int palms = 0;
            for (int k = 0; k < 12 && palms < 4; k++)
            {
                var at = grove + new Vector3(ctx.Range(-0.08f, 0.08f) * s, 0f, ctx.Range(-0.08f, 0.08f) * s);
                if (!OffBoat(at, 0.03f * s)) continue;
                if (ctx.PlaceByHeight(Palm, at, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.15f) * s, StandIn.Palm, null, 0.3f)) palms++;
            }

            // 4. Scrub: low, wide white-broom bushes scattered over the sand and the dunes, kept off the boat.
            int scrub = 0;
            for (int k = 0; k < 60 && scrub < 16; k++)
            {
                var at = ctx.RandomPoint(0.03f * s);
                if (!OffBoat(at, 0.04f * s)) continue;
                if (ctx.PlaceByHeight(Scrub, at, ctx.Range(0f, 360f), ctx.Range(0.06f, 0.09f) * s, StandIn.Tree, Sage, 1.0f)) scrub++;
            }
        }
    }
}
