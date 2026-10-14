using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Blended biome weights (spec §6): every hex centre within <see cref="BlendRadiusHexes"/> hex radii of a point
    /// contributes 1 - smoothstep(distance / radius); weights are normalised to sum to 1. A point no centre reaches (map
    /// edge, beyond the outermost centres) takes the nearest centre's biome. Deterministic: fixed iteration order.
    /// The biome colours are baked from it (BiomeColourFile): a change to the weights needs a re-bake and a bump of
    /// BiomeColourFile.Version (tools/tests/test_biome_colour_bake.py fails until then).</summary>
    public sealed class BiomeBlender
    {
        /// <summary>Falloff radius in hex radii. Neighbouring centres are sqrt(3) = 1.732 radii apart, so 1.72 keeps every
        /// neighbour out of reach of a hex's own centre (centre weight stays 1) while making the band where both biomes
        /// have more than 10% weight about 0.6 of a hex width (1.25 gave about 0.24).</summary>
        public const float BlendRadiusHexes = 1.72f;

        // The radius-sized grid as flat arrays (playtest 2, P2-1: the tuple-keyed dictionary made this the largest avoidable
        // boot phase). Cell (ix, iy) holds items cellStart[c] .. cellStart[c + 1] - 1, in insertion order, so the weights are
        // bit-identical to the dictionary version (BiomeBlenderSpeedTests).
        readonly int minX, minY, nx, ny;
        readonly int[] cellStart;
        readonly float[] px, py; readonly int[] pl;
        readonly List<(Vector2 p, int layer)> all = new List<(Vector2, int)>();
        readonly float radius, cell; readonly int layerCount;

        public BiomeBlender(IReadOnlyList<Vector2> centres, IReadOnlyList<int> layers, float hexSizeM, int layerCount)
        {
            if (centres.Count != layers.Count) throw new ArgumentException("centres and layers differ in length");
            if (!(hexSizeM > 0)) throw new ArgumentOutOfRangeException(nameof(hexSizeM));
            this.layerCount = layerCount;
            radius = BlendRadiusHexes * hexSizeM;
            cell = radius; // a radius-sized cell: the 3x3 neighbourhood covers every centre within the radius
            int maxX = int.MinValue, maxY = int.MinValue; minX = int.MaxValue; minY = int.MaxValue;
            for (int i = 0; i < centres.Count; i++)
            {
                if (layers[i] < 0 || layers[i] >= layerCount) throw new ArgumentOutOfRangeException(nameof(layers));
                all.Add((centres[i], layers[i]));
                var (kx, ky) = Key(centres[i]);
                minX = Math.Min(minX, kx); minY = Math.Min(minY, ky); maxX = Math.Max(maxX, kx); maxY = Math.Max(maxY, ky);
            }
            if (centres.Count == 0) { minX = minY = 0; maxX = maxY = -1; }
            nx = maxX - minX + 1; ny = maxY - minY + 1;
            cellStart = new int[nx * ny + 1];
            var counts = new int[nx * ny];
            foreach (var c in all) { var (kx, ky) = Key(c.p); counts[(kx - minX) + (ky - minY) * nx]++; }
            for (int c = 0; c < counts.Length; c++) cellStart[c + 1] = cellStart[c] + counts[c];
            px = new float[all.Count]; py = new float[all.Count]; pl = new int[all.Count];
            var fill = (int[])cellStart.Clone();
            foreach (var c in all)                                      // insertion order within each cell
            {
                var (kx, ky) = Key(c.p);
                var at = fill[(kx - minX) + (ky - minY) * nx]++;
                px[at] = c.p.x; py[at] = c.p.y; pl[at] = c.layer;
            }
        }

        (int, int) Key(Vector2 p) => (Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell));

        /// <summary>Fills <paramref name="w"/> (length = layer count) with normalised weights at <paramref name="p"/>.</summary>
        public void Weights(Vector2 p, float[] w)
        {
            if (w.Length != layerCount) throw new ArgumentException($"weights array length {w.Length} != {layerCount}");
            Array.Clear(w, 0, w.Length);
            var (cx, cy) = Key(p); float total = 0f;
            for (int dx = -1; dx <= 1; dx++)
            {
                int gx = cx + dx - minX;
                if (gx < 0 || gx >= nx) continue;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int gy = cy + dy - minY;
                    if (gy < 0 || gy >= ny) continue;
                    int c = gx + gy * nx;
                    for (int k = cellStart[c]; k < cellStart[c + 1]; k++)
                    {
                        float t = new Vector2(px[k] - p.x, py[k] - p.y).magnitude / radius;
                        if (t >= 1f) continue;
                        float f = 1f - t * t * (3f - 2f * t); // 1 - smoothstep
                        w[pl[k]] += f; total += f;
                    }
                }
            }
            if (total > 0f) { for (int i = 0; i < w.Length; i++) w[i] /= total; return; }
            w[NearestLayer(p)] = 1f;
        }

        int NearestLayer(Vector2 p)
        {
            float best = float.MaxValue; int layer = 0;
            foreach (var c in all) { float d = (c.p - p).sqrMagnitude; if (d < best) { best = d; layer = c.layer; } }
            return layer;
        }
    }
}
