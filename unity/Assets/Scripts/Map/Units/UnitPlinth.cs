#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Demo2 T29's token (refs #197): the figures of an army token stand shoulder to shoulder on one round earth
    /// base that hugs them, ringed by a bright white rim, and the side's flag flies on a pole at the base's back edge
    /// (<see cref="UnitFlags"/>). Low-poly meshes, one shared mesh per kind: the base in the units' palette material, the
    /// rim in the selection ring's unlit material made white (no new shader, texture or per-pixel cost; docs/visual-v1.md,
    /// #391). On a slope the base sits at the median ground under it, so it never stands up as a mesa on a hill; an earth
    /// skirt of at most <see cref="SkirtCapM"/> hides the seam where the ground falls away, and ground higher than the base
    /// shows through it, as a miniature's base set into a diorama.</summary>
    public static class UnitPlinth
    {
        public const string PlinthName = "Plinth", RimName = "PlinthRim", SkirtName = "PlinthSkirt", FlagName = "Flag";
        /// <summary>The base's width over the trio's (the outer figures' outer edges), T29's measure: 1.41, 1.62 and 1.71 on
        /// three of its tokens (the rim's outer edge over the figures' outer edges, 4x crops of the 1:12 frame).</summary>
        public const float BaseToTrio = 1.45f;
        /// <summary>Half a figure's width (m, real size: the Ready pose's rifle across the body is the widest).</summary>
        public const float FigureHalfWidthM = 0.35f;
        /// <summary>The base's thickness and the rim's band (inner and outer edge), as shares of the radius. T29's ring is
        /// about 8% of the base's radius wide and wraps its edge; its base is about a tenth of its radius thick.</summary>
        public const float ThicknessShare = 0.10f, RimInner = 0.95f, RimOuter = 1.04f, RimLiftShare = 0.006f;
        /// <summary>The rim's colour: unlit white (the selection ring's ivory is 1, 0.95, 0.8).</summary>
        public static readonly Color RimColour = new Color(1f, 0.99f, 0.96f);
        /// <summary>The most an earth skirt reaches below the base (m): on the coastal plain and the Shephelah the ground under
        /// a base falls less than this below its median, so the skirt closes the seam there; on the steep hills a short gap
        /// shows under the downhill edge rather than a kilometre-tall pedestal. About a sixteenth of a figure's height.</summary>
        public const float SkirtCapM = 150f;
        /// <summary>Where the flag's pole stands: on the base's back edge (north, away from the camera), at this share of
        /// the radius from the middle, east of the middle by <see cref="FlagEastShare"/> of the radius but never nearer it
        /// than half the badge's width and <see cref="FlagClearOfBadgeM"/>, so the flag flies clear of the badge.</summary>
        public const float FlagEdgeShare = 0.9f, FlagEastShare = 0.3f, FlagClearOfBadgeM = 150f;
        public const int Segments = 28;

        /// <summary>The trio's width (m) for figures at <paramref name="offsets"/> (in hex circumradii times
        /// <paramref name="spread"/>).</summary>
        public static float TrioWidth(IReadOnlyList<Vector2> offsets, float spread)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var o in offsets) { lo = Mathf.Min(lo, o.x * spread); hi = Mathf.Max(hi, o.x * spread); }
            return hi - lo + 2f * FigureHalfWidthM * UnitShowcase.FigureScale;
        }

        /// <summary>The base's radius (m) for a trio of <paramref name="trioWidth"/>: the rim's outer edge at
        /// <see cref="BaseToTrio"/> times the trio's width.</summary>
        public static float Radius(float trioWidth) => BaseToTrio * trioWidth / 2f / RimOuter;

        /// <summary>The base: an earth disc of radius 1, its top at y = 0, <see cref="ThicknessShare"/> thick, with a
        /// slightly rounded top edge.</summary>
        public static Mesh PlinthMesh(string name) =>
            new MiniatureMesh().Lathe(Matrix4x4.identity, new List<Vector2>
                { new Vector2(-ThicknessShare, 1f), new Vector2(-0.02f, 1f), new Vector2(0f, 0.985f) }, UnitPalette.Earth, Segments, shade: 0.8f).Build(name);

        /// <summary>The rim: a flat white band from <see cref="RimInner"/> to <see cref="RimOuter"/> of the radius, a hair
        /// above the base's top, round its edge (drawn with the ring's unlit material, which draws both faces).</summary>
        public static Mesh RimMesh(string name)
        {
            const int n = 48;
            var verts = new List<Vector3>(2 * n); var tris = new List<int>(6 * n);
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts.Add(d * RimOuter + Vector3.up * RimLiftShare); verts.Add(d * RimInner + Vector3.up * RimLiftShare);
            }
            for (int i = 0; i < n; i++)
            {
                int a = 2 * i, b = 2 * ((i + 1) % n);
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The skirt under a base on a slope: an earth cylinder of radius 1 from y = -1 to 0, scaled per token.</summary>
        public static Mesh SkirtMesh(string name) =>
            new MiniatureMesh().Lathe(Matrix4x4.identity, new List<Vector2> { new Vector2(-1f, 0.995f), new Vector2(0f, 0.995f) }, UnitPalette.Earth, Segments, shade: 0.55f).Build(name);

        /// <summary>The ground under a disc of radius <paramref name="r"/> at (x, z): its centre and six rings of 36 (the
        /// outer one round the disc's edge). Returns the lowest, the median and the highest sample.</summary>
        public static (float low, float median, float high) GroundUnder(Func<float, float, float> heightAt, float x, float z, float r)
        {
            var h = new List<float>(217) { heightAt(x, z) };
            foreach (var ring in new[] { 0.2f * r, 0.4f * r, 0.6f * r, 0.8f * r, 0.95f * r, r / Mathf.Cos(Mathf.PI / 36f) })
                for (int k = 0; k < 36; k++)
                {
                    float a = Mathf.PI / 18f * k;
                    h.Add(heightAt(x + Mathf.Cos(a) * ring, z + Mathf.Sin(a) * ring));
                }
            h.Sort();
            return (h[0], h[h.Count / 2], h[h.Count - 1]);
        }

        /// <summary>Where the base sits: its bottom at the median ground under it, its top <see cref="ThicknessShare"/> of the
        /// radius higher, and its skirt's bottom no more than <see cref="SkirtCapM"/> below the bottom (and not below the
        /// lowest ground).</summary>
        public static (float top, float bottom, float skirtBottom) Seat(Func<float, float, float> heightAt, float x, float z, float r)
        {
            var (low, median, _) = GroundUnder(heightAt, x, z, r);
            return (median + ThicknessShare * r, median, Mathf.Max(low - 30f, median - SkirtCapM));
        }

        /// <summary>The highest ground under a figure's boots (0.2 m from its centre at real size): a figure stands on the
        /// base's top, or on the ground where the ground under its boots rises above it.</summary>
        public static float BootsTop(Func<float, float, float> heightAt, float x, float z)
        {
            float r = 0.2f * UnitShowcase.FigureScale, top = heightAt(x, z);
            for (int k = 0; k < 16; k++) { float a = Mathf.PI / 8f * k; top = Mathf.Max(top, heightAt(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r)); }
            return top;
        }

        static Func<float, float, float> HeightAt(MapScene map) =>
            (x, z) => map.Terrain.SampleHeight(new Vector3(x, 0, z)) + map.Terrain.transform.position.y;

        /// <summary>The height a figure standing at (x, z) on a base whose top is <paramref name="top"/> stands at.</summary>
        public static float FigureY(MapScene map, float top, float x, float z) => Mathf.Max(top, BootsTop(HeightAt(map), x, z));

        /// <summary>Builds the base (with its rim, and its skirt on a slope) of radius <paramref name="radius"/> at
        /// <paramref name="centre"/> under <paramref name="token"/>, and <paramref name="side"/>'s flag when it has one.
        /// <paramref name="mesh"/> returns the shared mesh for a key. Returns the base's top.</summary>
        public static float Build(MapScene map, Transform token, Vector3 centre, float radius, string side,
            Func<string, Func<Mesh>, Mesh> mesh, Material paint, Material rim)
        {
            var (top, bottom, skirtBottom) = Seat(HeightAt(map), centre.x, centre.z, radius);
            var at = new Vector3(centre.x, top, centre.z);
            UnitShowcase.Piece(token, PlinthName, mesh("unit-plinth", () => PlinthMesh("UnitPlinth")), paint, at, Quaternion.identity, radius);
            var ring = UnitShowcase.Piece(token, RimName, mesh("unit-plinth-rim", () => RimMesh("UnitPlinthRim")), rim, at, Quaternion.identity, radius);
            ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float plinthBottom = top - ThicknessShare * radius;
            if (skirtBottom < plinthBottom - 1f)
            {
                var skirt = UnitShowcase.Piece(token, SkirtName, mesh("unit-plinth-skirt", () => SkirtMesh("UnitPlinthSkirt")), paint,
                    new Vector3(centre.x, plinthBottom + 1f, centre.z), Quaternion.identity, 1f);
                skirt.transform.localScale = new Vector3(radius, plinthBottom + 1f - skirtBottom, radius);
            }
            if (UnitFlags.Has(side))
            {
                float edge = FlagEdgeShare * radius;
                float east = Mathf.Min(edge * 0.95f, Mathf.Max(FlagEastShare * radius, UnitBadge.SizeM / 2f + FlagClearOfBadgeM));
                float north = Mathf.Sqrt(edge * edge - east * east);
                var foot = new Vector3(centre.x + east, 0f, centre.z + north);
                foot.y = FigureY(map, top, foot.x, foot.z);
                UnitShowcase.Piece(token, FlagName, mesh("unit-flag-" + side, () => UnitFlags.Build(side)!), paint, foot, Quaternion.identity,
                    UnitBadge.FigureHeightM * UnitShowcase.FigureScale);
            }
            return top;
        }
    }
}
