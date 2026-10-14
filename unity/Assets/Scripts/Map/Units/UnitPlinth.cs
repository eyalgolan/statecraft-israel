#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Demo2 T29's token (refs #197): the figures of an army token stand together on one round earth plinth with a
    /// white rim, and the side's flag flies on a pole at its back (<see cref="UnitFlags"/>). Low-poly meshes in the units'
    /// palette material, one shared mesh per kind (no new shader, texture or per-pixel cost; docs/visual-v1.md, #391).
    /// On a slope the plinth's top is level at the highest ground under it, and an earth skirt reaches down to the lowest,
    /// as a painted miniature's base does, so no boot sinks and nothing floats.</summary>
    public static class UnitPlinth
    {
        public const string PlinthName = "Plinth", SkirtName = "PlinthSkirt", FlagName = "Flag";
        /// <summary>The plinth's radius as a share of the hex circumradius (a compact token's is halved with its trio): it
        /// holds the trio's boots (<see cref="UnitShowcase.FigureOffsets"/> at <see cref="UnitShowcase.TrioSpread"/>) and
        /// stays inside the selection ring (<see cref="UnitShowcase.RingRadius"/>).</summary>
        public const float RadiusShare = 0.52f;
        /// <summary>The earth disc's thickness and the rim's height, as shares of the radius; the rim's inner and outer
        /// radius.</summary>
        public const float ThicknessShare = 0.06f, RimHeightShare = 0.022f, RimInner = 0.915f, RimOuter = 0.985f;
        /// <summary>Where the flag's pole stands, as shares of the radius: back (north, away from the camera) and to the
        /// east, so the flag flies clear of the badge over the trio's middle.</summary>
        public static readonly Vector2 FlagAt = new Vector2(0.45f, 0.55f);
        public const int RimSlot = UnitPalette.RoundelWhite;
        const int Segments = 28;

        /// <summary>The plinth: an earth disc of radius 1, its top at y = 0, with a raised white rim near its edge.</summary>
        public static Mesh PlinthMesh(string name)
        {
            var m = new MiniatureMesh();
            m.Lathe(Matrix4x4.identity, new List<Vector2> { new Vector2(-ThicknessShare, 1f), new Vector2(-0.012f, 1f), new Vector2(0f, RimOuter) },
                UnitPalette.Earth, Segments, shade: 0.8f);
            m.Lathe(Matrix4x4.identity, new List<Vector2>
                {
                    new Vector2(0f, RimOuter), new Vector2(RimHeightShare, RimOuter), new Vector2(RimHeightShare, RimInner), new Vector2(0f, RimInner),
                }, RimSlot, Segments, shade: 0.95f, caps: false);
            return m.Build(name);
        }

        /// <summary>The skirt under a plinth on a slope: an earth cylinder of radius 1 from y = -1 to 0, scaled per token.</summary>
        public static Mesh SkirtMesh(string name) =>
            new MiniatureMesh().Lathe(Matrix4x4.identity, new List<Vector2> { new Vector2(-1f, 0.995f), new Vector2(0f, 0.995f) }, UnitPalette.Earth, Segments, shade: 0.55f).Build(name);

        /// <summary>The highest and lowest ground under a disc of radius <paramref name="r"/> at (x, z): its centre and six
        /// rings of 36 (the outer one round the disc's edge), so a ridge under the plinth is caught.</summary>
        public static (float top, float low) GroundRange(Func<float, float, float> heightAt, float x, float z, float r)
        {
            float top = heightAt(x, z), low = top;
            foreach (var ring in new[] { 0.2f * r, 0.4f * r, 0.6f * r, 0.8f * r, 0.95f * r, r / Mathf.Cos(Mathf.PI / 36f) })
                for (int k = 0; k < 36; k++)
                {
                    float a = Mathf.PI / 18f * k, h = heightAt(x + Mathf.Cos(a) * ring, z + Mathf.Sin(a) * ring);
                    top = Mathf.Max(top, h); low = Mathf.Min(low, h);
                }
            return (top, low);
        }

        /// <summary>Builds the plinth (and its skirt on a slope) of radius <paramref name="radius"/> at
        /// <paramref name="centre"/> under <paramref name="token"/>, and <paramref name="side"/>'s flag when it has one.
        /// <paramref name="mesh"/> returns the shared mesh for a key. Returns the height the figures stand at.</summary>
        public static float Build(MapScene map, Transform token, Vector3 centre, float radius, string side,
            Func<string, Func<Mesh>, Mesh> mesh, Material paint)
        {
            var (top, low) = GroundRange((x, z) => map.Terrain.SampleHeight(new Vector3(x, 0, z)) + map.Terrain.transform.position.y, centre.x, centre.z, radius);
            var at = new Vector3(centre.x, top, centre.z);
            UnitShowcase.Piece(token, PlinthName, mesh("unit-plinth", () => PlinthMesh("UnitPlinth")), paint, at, Quaternion.identity, radius);
            float drop = top - low;
            if (drop > ThicknessShare * radius)
            {
                var skirt = UnitShowcase.Piece(token, SkirtName, mesh("unit-plinth-skirt", () => SkirtMesh("UnitPlinthSkirt")), paint, at, Quaternion.identity, 1f);
                skirt.transform.localScale = new Vector3(radius, drop + 30f, radius);   // 30 m into the ground: the lowest sample is not the lowest point
            }
            if (UnitFlags.Has(side))
            {
                var foot = at + new Vector3(FlagAt.x, 0f, FlagAt.y) * radius;
                UnitShowcase.Piece(token, FlagName, mesh("unit-flag-" + side, () => UnitFlags.Build(side)!), paint, foot, Quaternion.identity,
                    UnitBadge.FigureHeightM * UnitShowcase.FigureScale);
            }
            return top;
        }
    }
}
