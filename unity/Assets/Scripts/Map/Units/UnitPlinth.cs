#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Demo2 T29's token (refs #197): the figures of an army token stand shoulder to shoulder on one round earth
    /// base that hugs them, a bright white ring lies on the ground just outside its edge, and the side's flag flies on a pole
    /// at the base's back edge (<see cref="UnitFlags"/>). The base is draped on the terrain (round 3 of #559): its top
    /// follows the ground under it at <see cref="LipShare"/> of its radius, an edge lip of the same height closes it to the
    /// ground, and the ring is draped too, so on a hill it neither stands up as a mesa nor leaves a gap downhill. Its earth
    /// is mottled by the palette's shade rows (no texture). Low-poly meshes built per token (they follow its ground); the
    /// base in the units' palette material, the ring in the selection ring's unlit material made white (no new shader,
    /// texture or per-pixel cost; docs/visual-v1.md, #391).</summary>
    public static class UnitPlinth
    {
        public const string PlinthName = "Plinth", LipName = "PlinthLip", RimName = "PlinthRim", FlagName = "Flag";
        /// <summary>The ring's outer edge over the trio's width (the outer figures' outer edges), T29's measure: 1.41, 1.62
        /// and 1.71 on three of its tokens (4x crops of the 1:12 frame).</summary>
        public const float BaseToTrio = 1.45f;
        /// <summary>Half a figure's width (m, real size: the Ready pose's rifle across the body is the widest).</summary>
        public const float FigureHalfWidthM = 0.35f;
        /// <summary>The base's top over the ground under it, as a share of its radius (about 100 m): T29's base is a slab
        /// about a tenth of its radius thick seen from above; draped, a twentieth reads as its edge without hiding the ground's
        /// shape.</summary>
        public const float LipShare = 0.05f;
        /// <summary>How far the lip reaches into the ground below the base's edge (m), so no seam shows where the terrain's
        /// rendered surface falls between its samples.</summary>
        public const float LipSinkM = 40f;
        /// <summary>The ring: a band on the ground from <see cref="RimInner"/> to <see cref="RimOuter"/> of the radius, just
        /// outside the base's edge, as T29's (about 8% of the radius wide), <see cref="RimLiftM"/> over the ground.</summary>
        public const float RimInner = 1.02f, RimOuter = 1.10f, RimLiftM = 20f;
        /// <summary>The ring's vertices round it: each takes the highest ground across the band and halfway to its
        /// neighbours (MapBuilder.DrapedRing's rule), so the ground never rises through it between them.</summary>
        public const int RimSegments = 192;
        /// <summary>The ring's colour: unlit white (the selection ring's ivory is 1, 0.95, 0.8).</summary>
        public static readonly Color RimColour = new Color(1f, 1f, 1f);
        /// <summary>The draped grid: rings out to the edge and segments round it. At a 2 km radius a ring every 63 m and a
        /// segment every 130 m at the edge, finer than the heightmap's 79 x 210 m spacing where it matters (12 x 48 left the
        /// top 74-81 m off the ground's shape on Harel's and the 7th Brigade's slopes, 24 x 72 still 58 m on Harel's).</summary>
        public const int Rings = 32, Segments = 96;
        /// <summary>The mottling: each vertex of the top takes a shade row in this range (the palette's dark wash below 0.45,
        /// its base colour above) from smooth value noise over the world (cells of <see cref="MottleCellM"/>), so the earth
        /// shows soft patches, the same every time and continuous across facets.</summary>
        public const float MottleLow = 0.15f, MottleHigh = 0.75f, MottleCellM = 450f;
        /// <summary>Where the flag's pole stands: on the base's back edge (north, away from the camera), at this share of
        /// the radius from the middle, east of the middle by <see cref="FlagEastShare"/> of the radius but never nearer it
        /// than half the badge's width and <see cref="FlagClearOfBadgeM"/>, so the flag flies clear of the badge.</summary>
        public const float FlagEdgeShare = 0.9f, FlagEastShare = 0.3f, FlagClearOfBadgeM = 150f;

        /// <summary>The trio's width (m) for figures at <paramref name="offsets"/> (in hex circumradii times
        /// <paramref name="spread"/>).</summary>
        public static float TrioWidth(IReadOnlyList<Vector2> offsets, float spread)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var o in offsets) { lo = Mathf.Min(lo, o.x * spread); hi = Mathf.Max(hi, o.x * spread); }
            return hi - lo + 2f * FigureHalfWidthM * UnitShowcase.FigureScale;
        }

        /// <summary>The base's radius (m) for a trio of <paramref name="trioWidth"/>: the ring's outer edge at
        /// <see cref="BaseToTrio"/> times the trio's width.</summary>
        public static float Radius(float trioWidth) => BaseToTrio * trioWidth / 2f / RimOuter;

        static float Hash(int i, int j)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(j * 19349663) ^ 0x9E3779B9u;
            h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            return (h & 0xFFFF) / 65535f;
        }

        /// <summary>A shade row in [<see cref="MottleLow"/>, <see cref="MottleHigh"/>] at world (x, z): smooth value noise.</summary>
        public static float Mottle(float x, float z)
        {
            float fx = x / MottleCellM, fz = z / MottleCellM;
            int i = Mathf.FloorToInt(fx), j = Mathf.FloorToInt(fz);
            float u = Mathf.SmoothStep(0f, 1f, fx - i), v = Mathf.SmoothStep(0f, 1f, fz - j);
            float n = Mathf.Lerp(Mathf.Lerp(Hash(i, j), Hash(i + 1, j), u), Mathf.Lerp(Hash(i, j + 1), Hash(i + 1, j + 1), u), v);
            return Mathf.Lerp(MottleLow, MottleHigh, n);
        }

        static Vector2 Polar(int ring, int seg) =>
            new Vector2(Mathf.Cos(2f * Mathf.PI * seg / Segments), Mathf.Sin(2f * Mathf.PI * seg / Segments)) * ((float)ring / Rings);

        /// <summary>The base's top, draped: a polar grid over the unit disc (x, z scaled by the radius on the transform)
        /// whose every vertex stands <see cref="LipShare"/> of the radius over the ground at its world point, with smooth
        /// normals (the light follows the ground's shape) and mottled shade rows.</summary>
        public static Mesh TopMesh(string name, Func<float, float, float> groundAt, Vector2 centre, float radius)
        {
            float lip = LipShare * radius, u = UnitPalette.U(UnitPalette.Earth);
            var verts = new List<Vector3>(1 + Rings * Segments); var uv = new List<Vector2>(verts.Capacity); var tris = new List<int>(6 * Rings * Segments);
            void Add(Vector2 p)
            {
                float x = centre.x + p.x * radius, z = centre.y + p.y * radius;
                verts.Add(new Vector3(p.x, groundAt(x, z) + lip, p.y)); uv.Add(new Vector2(u, Mottle(x, z)));
            }
            Add(Vector2.zero);
            for (int ring = 1; ring <= Rings; ring++) for (int seg = 0; seg < Segments; seg++) Add(Polar(ring, seg));
            int V(int ring, int seg) => ring == 0 ? 0 : 1 + (ring - 1) * Segments + ((seg % Segments) + Segments) % Segments;
            for (int seg = 0; seg < Segments; seg++) tris.AddRange(new[] { 0, V(1, seg + 1), V(1, seg) });
            for (int ring = 1; ring < Rings; ring++)
                for (int seg = 0; seg < Segments; seg++)
                    tris.AddRange(new[] { V(ring, seg), V(ring, seg + 1), V(ring + 1, seg), V(ring + 1, seg), V(ring, seg + 1), V(ring + 1, seg + 1) });
            var mesh = new Mesh { name = name };
            if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The edge lip: the band from the top's edge down to <see cref="LipSinkM"/> below the ground, following it.</summary>
        public static Mesh LipMesh(string name, Func<float, float, float> groundAt, Vector2 centre, float radius)
        {
            var m = new MiniatureMesh();
            float lip = LipShare * radius;
            for (int seg = 0; seg < Segments; seg++)
            {
                var a = Polar(Rings, seg); var b = Polar(Rings, seg + 1);
                float ga = groundAt(centre.x + a.x * radius, centre.y + a.y * radius), gb = groundAt(centre.x + b.x * radius, centre.y + b.y * radius);
                m.Facet(new List<Vector3> { new Vector3(a.x, ga + lip, a.y), new Vector3(a.x, ga - LipSinkM, a.y), new Vector3(b.x, gb - LipSinkM, b.y), new Vector3(b.x, gb + lip, b.y) },
                    UnitPalette.Earth, 0.1f, outward: true);
            }
            return m.Build(name);
        }

        /// <summary>The ring: a flat band on the ground from <see cref="RimInner"/> to <see cref="RimOuter"/> of the radius,
        /// <see cref="RimLiftM"/> over it (the ring shader draws both faces, with the selection ring's depth offset).</summary>
        public static Mesh RimMesh(string name, Func<float, float, float> groundAt, Vector2 centre, float radius)
        {
            const int n = RimSegments;
            var verts = new List<Vector3>(2 * n); var tris = new List<int>(6 * n);
            float Across(float step)
            {
                float a = 2f * Mathf.PI * step / n, top = float.MinValue;
                foreach (var r in new[] { RimInner, (RimInner + RimOuter) / 2f, RimOuter })
                    top = Mathf.Max(top, groundAt(centre.x + Mathf.Cos(a) * r * radius, centre.y + Mathf.Sin(a) * r * radius));
                return top;
            }
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n, y = Mathf.Max(Across(i), Mathf.Max(Across(i - 0.5f), Across(i + 0.5f))) + RimLiftM;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                verts.Add(new Vector3(d.x * RimOuter, y, d.y * RimOuter)); verts.Add(new Vector3(d.x * RimInner, y, d.y * RimInner));
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

        /// <summary>The highest ground under a figure's boots (0.2 m from its centre at real size).</summary>
        public static float BootsTop(Func<float, float, float> heightAt, float x, float z)
        {
            float r = 0.2f * UnitShowcase.FigureScale, top = heightAt(x, z);
            for (int k = 0; k < 16; k++) { float a = Mathf.PI / 8f * k; top = Mathf.Max(top, heightAt(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r)); }
            return top;
        }

        public static Func<float, float, float> HeightAt(MapScene map) =>
            (x, z) => map.Terrain.SampleHeight(new Vector3(x, 0, z)) + map.Terrain.transform.position.y;

        /// <summary>The height a figure standing at (x, z) on a base of <paramref name="radius"/> stands at: on the base's top
        /// over the highest ground under its boots.</summary>
        public static float FigureY(MapScene map, float radius, float x, float z) => BootsTop(HeightAt(map), x, z) + LipShare * radius;

        /// <summary>Builds the draped base, its lip and its ring, of radius <paramref name="radius"/> at
        /// <paramref name="centre"/> under <paramref name="token"/>, and <paramref name="side"/>'s flag when it has one.
        /// <paramref name="own"/> takes ownership of each per-token mesh; <paramref name="shared"/> returns the shared flag
        /// mesh for a key. Returns the base's top at its centre.</summary>
        public static float Build(MapScene map, Transform token, Vector3 centre, float radius, string side, Func<Mesh, Mesh> own,
            Func<string, Func<Mesh>, Mesh> shared, Material paint, Material rim, Material flagWhite, Material flagBlue)
        {
            var ground = HeightAt(map); var c = new Vector2(centre.x, centre.z);
            var at = new Vector3(centre.x, 0f, centre.z); var scale = new Vector3(radius, 1f, radius);
            UnitShowcase.Piece(token, PlinthName, own(TopMesh("UnitPlinth", ground, c, radius)), paint, at, Quaternion.identity, 1f).transform.localScale = scale;
            UnitShowcase.Piece(token, LipName, own(LipMesh("UnitPlinthLip", ground, c, radius)), paint, at, Quaternion.identity, 1f).transform.localScale = scale;
            var ring = UnitShowcase.Piece(token, RimName, own(RimMesh("UnitPlinthRim", ground, c, radius)), rim, at, Quaternion.identity, 1f);
            ring.transform.localScale = scale;
            var r = ring.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            if (UnitFlags.Has(side))
            {
                float edge = FlagEdgeShare * radius;
                float east = Mathf.Min(edge * 0.95f, Mathf.Max(FlagEastShare * radius, UnitBadge.SizeM / 2f + FlagClearOfBadgeM));
                float north = Mathf.Sqrt(edge * edge - east * east);
                var foot = new Vector3(centre.x + east, 0f, centre.z + north);
                foot.y = FigureY(map, radius, foot.x, foot.z);
                var flag = UnitShowcase.Piece(token, FlagName, shared("unit-flag-" + side, () => UnitFlags.Build(side)!), paint, foot, Quaternion.identity,
                    UnitBadge.FigureHeightM * UnitShowcase.FigureScale);
                flag.GetComponent<MeshRenderer>().sharedMaterials = new[] { paint, flagWhite, flagBlue };
            }
            return ground(centre.x, centre.z) + LipShare * radius;
        }
    }
}
