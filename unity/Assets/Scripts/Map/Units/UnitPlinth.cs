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
        /// <summary>The outline's sides (the base's edge, the clip of its top and the line its lip follows), and the lip's
        /// step along it (m): the lip samples the ground every 40 m, so its top meets the draped top's edge.</summary>
        public const int OutlineSides = 96;
        public const float LipStepM = 40f;
        /// <summary>The mottling: each vertex of the top takes a shade row in this range (the palette's dark wash below 0.45,
        /// its base colour above) from smooth value noise over the world (cells of <see cref="MottleCellM"/>), so the earth
        /// shows soft patches, the same every time and continuous across facets.</summary>
        public const float MottleLow = 0.15f, MottleHigh = 0.75f, MottleCellM = 450f;
        /// <summary>Where the flag's pole stands: on the base's back edge (north, away from the camera), at this share of
        /// the radius from the middle, east of the middle by <see cref="FlagEastShare"/> of the radius but never nearer it
        /// than half the badge's width and <see cref="FlagClearOfBadgeM"/>, so the flag flies clear of the badge.</summary>
        public const float FlagEdgeShare = 0.9f, FlagEastShare = 0.3f, FlagClearOfBadgeM = 700f;   // 150 and 400 m left the pole's finial touching a badge near the frame's edge at gameplay03

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

        /// <summary>The terrain's own sample spacing (m): the finer of its heightmap's steps east-west and north-south.</summary>
        public static float TerrainStepM(MapScene map)
        {
            var d = map.Terrain.terrainData; int n = d.heightmapResolution - 1;
            return Mathf.Min(d.size.x / n, d.size.z / n);
        }

        /// <summary>The base's outline: a <see cref="OutlineSides"/>-gon of radius r round c (world x, z), counter-clockwise.</summary>
        static Vector2[] Outline(Vector2 c, float r)
        {
            var o = new Vector2[OutlineSides];
            for (int k = 0; k < OutlineSides; k++) { float a = 2f * Mathf.PI * k / OutlineSides; o[k] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            return o;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>The base's top, draped on the terrain's own triangles (round 4 of #559): every heightmap cell under the
        /// disc is split along the diagonal the terrain itself uses there (found from its height at the cell's centre) into the
        /// terrain's two triangles, each raised by the lip and clipped to the outline, a clipped corner taking its height from
        /// its triangle's own plane. So the top follows the ground exactly, wherever the token stands, with no grid to tune; a
        /// base about 2 km across covers some 760 cells. x and z are in the unit disc (the transform scales them by the
        /// radius), y in world metres; smooth normals across the cells' shared corners; mottled shade rows.</summary>
        public static Mesh TopMesh(string name, MapScene map, Vector2 centre, float radius)
        {
            var td = map.Terrain.terrainData; var tp = map.Terrain.transform.position;
            int n = td.heightmapResolution - 1; float dx = td.size.x / n, dz = td.size.z / n, lip = LipShare * radius, u = UnitPalette.U(UnitPalette.Earth);
            var ground = HeightAt(map); var outline = Outline(centre, radius);
            var verts = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>(); var grid = new Dictionary<long, int>();
            int Add(float x, float y, float z) { verts.Add(new Vector3((x - centre.x) / radius, y + lip, (z - centre.y) / radius)); uv.Add(new Vector2(u, Mottle(x, z))); return verts.Count - 1; }
            int G(int i, int j)
            {
                long key = (long)i * 1_000_003L + j;
                if (!grid.TryGetValue(key, out int k)) { float x = tp.x + i * dx, z = tp.z + j * dz; grid[key] = k = Add(x, ground(x, z), z); }
                return k;
            }
            bool Inside(Vector2 q) { for (int k = 0; k < OutlineSides; k++) if (Cross(outline[(k + 1) % OutlineSides] - outline[k], q - outline[k]) < 0f) return false; return true; }
            void Tri(int a, int b, int c)
            {
                var n3 = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                if (n3.y >= 0f) tris.AddRange(new[] { a, b, c }); else tris.AddRange(new[] { a, c, b });
            }
            int i0 = Mathf.Max(0, Mathf.FloorToInt((centre.x - radius - tp.x) / dx)), i1 = Mathf.Min(n - 1, Mathf.CeilToInt((centre.x + radius - tp.x) / dx));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((centre.y - radius - tp.z) / dz)), j1 = Mathf.Min(n - 1, Mathf.CeilToInt((centre.y + radius - tp.z) / dz));
            for (int i = i0; i <= i1; i++)
                for (int j = j0; j <= j1; j++)
                {
                    float x0 = tp.x + i * dx, z0 = tp.z + j * dz;
                    var near = new Vector2(Mathf.Clamp(centre.x, x0, x0 + dx), Mathf.Clamp(centre.y, z0, z0 + dz));
                    if ((near - centre).magnitude > radius) continue;   // the cell lies outside the disc
                    var c00 = new Vector3(x0, ground(x0, z0), z0); var c10 = new Vector3(x0 + dx, ground(x0 + dx, z0), z0);
                    var c01 = new Vector3(x0, ground(x0, z0 + dz), z0 + dz); var c11 = new Vector3(x0 + dx, ground(x0 + dx, z0 + dz), z0 + dz);
                    float mid = ground(x0 + dx / 2f, z0 + dz / 2f);
                    bool d1 = Mathf.Abs(mid - (c00.y + c11.y) / 2f) <= Mathf.Abs(mid - (c10.y + c01.y) / 2f);   // the terrain's own diagonal here
                    var cells = d1 ? new[] { (c00, c10, c11, (0, 0), (1, 0), (1, 1)), (c00, c11, c01, (0, 0), (1, 1), (0, 1)) }
                                   : new[] { (c00, c10, c01, (0, 0), (1, 0), (0, 1)), (c10, c11, c01, (1, 0), (1, 1), (0, 1)) };
                    foreach (var (pa, pb, pc, ia, ib, ic) in cells)
                    {
                        Vector2 A = new Vector2(pa.x, pa.z), B = new Vector2(pb.x, pb.z), Cc = new Vector2(pc.x, pc.z);
                        if (Inside(A) && Inside(B) && Inside(Cc)) { Tri(G(i + ia.Item1, j + ia.Item2), G(i + ib.Item1, j + ib.Item2), G(i + ic.Item1, j + ic.Item2)); continue; }
                        // Clip the triangle to the outline (Sutherland-Hodgman); heights from the triangle's plane.
                        var poly = new List<Vector2> { A, B, Cc };
                        for (int k = 0; k < OutlineSides && poly.Count > 0; k++)
                        {
                            Vector2 e0 = outline[k], e = outline[(k + 1) % OutlineSides] - e0; var outp = new List<Vector2>();
                            for (int m = 0; m < poly.Count; m++)
                            {
                                Vector2 p = poly[m], q = poly[(m + 1) % poly.Count];
                                float sp = Cross(e, p - e0), sq = Cross(e, q - e0);
                                if (sp >= 0f) outp.Add(p);
                                if ((sp >= 0f) != (sq >= 0f)) outp.Add(p + (q - p) * (sp / (sp - sq)));
                            }
                            poly = outp;
                        }
                        if (poly.Count < 3) continue;
                        float det = (B.y - Cc.y) * (A.x - Cc.x) + (Cc.x - B.x) * (A.y - Cc.y);
                        float H(Vector2 q)
                        {
                            float wa = ((B.y - Cc.y) * (q.x - Cc.x) + (Cc.x - B.x) * (q.y - Cc.y)) / det;
                            float wb = ((Cc.y - A.y) * (q.x - Cc.x) + (A.x - Cc.x) * (q.y - Cc.y)) / det;
                            return wa * pa.y + wb * pb.y + (1f - wa - wb) * pc.y;
                        }
                        int f0 = Add(poly[0].x, H(poly[0]), poly[0].y);
                        for (int m = 1; m + 1 < poly.Count; m++) Tri(f0, Add(poly[m].x, H(poly[m]), poly[m].y), Add(poly[m + 1].x, H(poly[m + 1]), poly[m + 1].y));
                    }
                }
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
            float lip = LipShare * radius; var outline = Outline(centre, radius);
            var pts = new List<Vector2>();
            for (int k = 0; k < OutlineSides; k++)
            {
                Vector2 a = outline[k], b = outline[(k + 1) % OutlineSides];
                int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / LipStepM));
                for (int s = 0; s < steps; s++) pts.Add(Vector2.Lerp(a, b, (float)s / steps));
            }
            Vector3 L(Vector2 p, float dy) => new Vector3((p.x - centre.x) / radius, groundAt(p.x, p.y) + dy, (p.y - centre.y) / radius);
            for (int k = 0; k < pts.Count; k++)
            {
                Vector2 a = pts[k], b = pts[(k + 1) % pts.Count];
                m.Facet(new List<Vector3> { L(a, lip), L(a, -LipSinkM), L(b, -LipSinkM), L(b, lip) }, UnitPalette.Earth, 0.1f, outward: true);
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

        /// <summary>The height a figure standing at (x, z) on a base of <paramref name="radius"/> stands at: on the draped top
        /// at its own point (round 4 of #559; an uphill boot sets into the base, which hides it).
        /// UnitLayer.FaceCamera calls it as the camera turns (issue #644), so it samples the ground itself: HeightAt builds a
        /// new closure on every call.</summary>
        public static float FigureY(MapScene map, float radius, float x, float z) =>
            map.Terrain.SampleHeight(new Vector3(x, 0, z)) + map.Terrain.transform.position.y + LipShare * radius;

        /// <summary>Tests read the built meshes back: true keeps their CPU copy (players free it after the upload).</summary>
        internal static bool KeepMeshesReadable;

        /// <summary>Frees a built mesh's CPU copy after its upload, unless a test reads it back.</summary>
        public static Mesh Upload(Mesh m) { if (!KeepMeshesReadable) m.UploadMeshData(true); return m; }

        /// <summary>Builds the draped base, its lip and its ring, of radius <paramref name="radius"/> at
        /// <paramref name="centre"/> under <paramref name="token"/>, and <paramref name="side"/>'s flag when it has one (the neutral
        /// pennant when the map draws the side without one, <see cref="UnitFlags.HasPennant"/>).
        /// <paramref name="own"/> takes ownership of each per-token mesh; <paramref name="shared"/> returns the shared flag
        /// mesh for a key. Returns the base's top at its centre.</summary>
        public static float Build(MapScene map, Transform token, Vector3 centre, float radius, string side, Func<Mesh, Mesh> own,
            Func<string, Func<Mesh>, Mesh> shared, Material paint, Material rim, Material flagWhite, Material flagBlue)
        {
            var ground = HeightAt(map); var c = new Vector2(centre.x, centre.z);
            var at = new Vector3(centre.x, 0f, centre.z); var scale = new Vector3(radius, 1f, radius);
            UnitShowcase.Piece(token, PlinthName, own(TopMesh("UnitPlinth", map, c, radius)), paint, at, Quaternion.identity, 1f).transform.localScale = scale;
            UnitShowcase.Piece(token, LipName, own(LipMesh("UnitPlinthLip", ground, c, radius)), paint, at, Quaternion.identity, 1f).transform.localScale = scale;
            var ring = UnitShowcase.Piece(token, RimName, own(RimMesh("UnitPlinthRim", ground, c, radius)), rim, at, Quaternion.identity, 1f);
            ring.transform.localScale = scale;
            var r = ring.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            bool pennant = UnitFlags.HasPennant(side);
            if (UnitFlags.Has(side) || pennant)
            {
                float edge = FlagEdgeShare * radius;
                float east = Mathf.Min(edge * 0.95f, Mathf.Max(FlagEastShare * radius, UnitBadge.SizeM / 2f + FlagClearOfBadgeM));
                float north = Mathf.Sqrt(edge * edge - east * east);
                var foot = new Vector3(centre.x + east, 0f, centre.z + north);
                foot.y = FigureY(map, radius, foot.x, foot.z);
                var flag = pennant
                    ? UnitShowcase.Piece(token, FlagName, shared("unit-pennant", UnitFlags.BuildPennant), paint, foot, Quaternion.identity, UnitBadge.FigureHeightM * UnitShowcase.FigureScale)
                    : UnitShowcase.Piece(token, FlagName, shared("unit-flag-" + side, () => UnitFlags.Build(side)!), paint, foot, Quaternion.identity,
                        UnitBadge.FigureHeightM * UnitShowcase.FigureScale);
                if (!pennant) flag.GetComponent<MeshRenderer>().sharedMaterials = new[] { paint, flagWhite, flagBlue };
                flag.AddComponent<UnitFlagFacing>();   // owner ruling, 2026-10-15: the cloth turns about the pole to face the camera
                var cam = Camera.main; if (cam != null) UnitFlagFacing.Face(flag.transform, cam.transform.position);
            }
            return ground(centre.x, centre.z) + LipShare * radius;
        }
    }
}
