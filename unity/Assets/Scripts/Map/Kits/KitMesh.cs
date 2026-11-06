#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Facade styles, carried in the vertex colour's alpha and read by Statecraft/Kit (Kit.shader): which window
    /// pattern a wall gets. Roofs, domes and trees are Plain.</summary>
    public enum Facade : byte { Plain = 0, Stone = 64, Village = 128, WhiteCity = 255 }

    /// <summary>Grows one mesh from procedural parts (boxes, roofs, domes, towers, crowns). Vertex layout: position, normal,
    /// colour (sRGB albedo, alpha = <see cref="Facade"/>), uv0 = a wall's (along, up) position in real metres before the
    /// symbolic scale, so the shader draws storeys and windows at their real proportions on symbolic-size buildings.</summary>
    public sealed class KitMesh
    {
        readonly List<Vector3> pos = new List<Vector3>();
        readonly List<Vector3> nrm = new List<Vector3>();
        readonly List<Color32> col = new List<Color32>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> tri = new List<int>();

        public int VertexCount => pos.Count;
        /// <summary>The vertex colours and uvs so far (test aids: ToMesh's upload drops the CPU copy).</summary>
        internal IReadOnlyList<Color32> Colours => col;
        internal IReadOnlyList<Vector2> Uvs => uv;
        // ---- Issue #659: the close mesh (owner ruling on #700: "Models up close only") --------------------------------
        // Up close the towns' buildings are Tal Heuberger's models (TownModelLayer); far off they are this mesh's kit buildings, as
        // before. The close mesh is this mesh without its buildings (their vertex and index spans, CutStart/CutEnd), plus
        // the stone plinths under the models (Plinths).

        readonly List<(int v0, int v1, int i0, int i1)> cuts = new List<(int, int, int, int)>();
        KitMesh? plinths;
        /// <summary>The plinths under this mesh's building models, drawn only with the models (and, issue #1032, the
        /// landmarks' close copies, CloseCopy).</summary>
        internal KitMesh Plinths => plinths ??= new KitMesh();

        /// <summary>Marks the start of a building's parts (CutEnd marks its end): the close mesh leaves them out.</summary>
        internal (int v, int i) CutStart() => (pos.Count, tri.Count);
        internal void CutEnd((int v, int i) start) { if (pos.Count > start.v) cuts.Add((start.v, pos.Count, start.i, tri.Count)); }

        /// <summary>Issue #1032: the parts since <paramref name="start"/> (a landmark at a kit's symbolic scale) draw far off
        /// only (cut, as a building's), and up close a copy of them draws instead (with the plinths): shrunk about
        /// <paramref name="anchor"/>, on the ground by <paramref name="along"/> along the yaw <paramref name="yawDeg"/> and
        /// <paramref name="across"/> across it, and what stands above the anchor's level by <paramref name="up"/>; what is
        /// below keeps its depth, so the foundations stay in the slope. The facade uv stays in real metres, so the windows
        /// shrink with the walls. Returns the copy's bounds.</summary>
        internal Bounds CloseCopy((int v, int i) start, Vector3 anchor, float yawDeg, float along, float across, float up)
        {
            CutEnd(start);
            return Copy(start, (pos.Count, tri.Count), Plinths, anchor, yawDeg, along, across, up, new Bounds(anchor, Vector3.zero));
        }

        // ---- #2266 (owner, 2 November 2026: "tal's high quality models replace the built ones everytwhere") ----------------
        // A landmark's replaceable part (a minaret, a belfry, a clock tower) moves to a second submesh: of this mesh at the kit's
        // scale (FarParts), and of the close mesh as the close copy (CloseParts). Its renderer draws that submesh, the code-built
        // fallback, until the map gives it one material only, once Tal Heuberger's model stands there (HexPropModels).

        readonly List<(int v0, int v1, int i0, int i1)> detached = new List<(int, int, int, int)>();
        KitMesh? farParts, closeParts;
        internal KitMesh FarParts => farParts ??= new KitMesh();
        internal KitMesh CloseParts => closeParts ??= new KitMesh();
        internal bool HasParts => farParts != null;
        /// <summary>The replaceable parts detached from this mesh: their kind ("minaret", "belfry", "clock_tower"), owner, yaw
        /// and boxes far off and up close, in placement order.</summary>
        internal readonly List<(string kind, string owner, float yaw, Bounds far, Bounds close)> Parts = new List<(string, string, float, Bounds, Bounds)>();

        /// <summary>As CloseCopy for a landmark from <paramref name="start"/>, whose parts from <paramref name="part"/> on are a
        /// replaceable <paramref name="kind"/>: those leave this mesh for FarParts and their close copy goes to CloseParts, with
        /// the same transform as the rest. Returns the whole landmark's close box, as CloseCopy's.</summary>
        internal Bounds CloseCopySplit((int v, int i) start, (int v, int i) part, string kind, string owner, float partYaw,
            Vector3 anchor, float yawDeg, float along, float across, float up)
        {
            CutEnd(start);
            var end = (pos.Count, tri.Count);
            var b = Copy(start, part, Plinths, anchor, yawDeg, along, across, up, new Bounds(anchor, Vector3.zero));
            var close = Copy(part, end, CloseParts, anchor, yawDeg, along, across, up, new Bounds(anchor, Vector3.zero), fresh: true);
            b.Encapsulate(close);
            var far = Copy(part, end, FarParts, Vector3.zero, 0f, 1f, 1f, 1f, default, fresh: true);
            if (end.Item1 > part.v) detached.Add((part.v, end.Item1, part.i, end.Item2));
            Parts.Add((kind, owner, partYaw, far, close));
            return b;
        }

        /// <summary>Appends a copy of vertices and triangles [<paramref name="from"/>, <paramref name="to"/>) to
        /// <paramref name="into"/>, shrunk about <paramref name="anchor"/> as CloseCopy describes; returns <paramref name="b"/>
        /// grown by the copy (with <paramref name="fresh"/>, the copy's own box).</summary>
        Bounds Copy((int v, int i) from, (int v, int i) to, KitMesh into, Vector3 anchor, float yawDeg, float along, float across, float up,
            Bounds b, bool fresh = false)
        {
            int off = into.pos.Count;
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            bool first = fresh;
            for (int v = from.v; v < to.v; v++)
            {
                Vector3 p = pos[v] - anchor, n = nrm[v];
                // Into the yaw's frame (u along, w across), scaled, and back; a normal by the inverse scale.
                float u = (p.x * ca + p.z * sa) * along, w = (-p.x * sa + p.z * ca) * across;
                float nu = (n.x * ca + n.z * sa) / along, nw = (-n.x * sa + n.z * ca) / across;
                var q = anchor + new Vector3(u * ca - w * sa, p.y > 0f ? p.y * up : p.y, u * sa + w * ca);
                into.pos.Add(q); if (first) { b = new Bounds(q, Vector3.zero); first = false; } else b.Encapsulate(q);
                into.nrm.Add(new Vector3(nu * ca - nw * sa, p.y > 0f ? n.y / up : n.y, nu * sa + nw * ca).normalized);
                into.col.Add(col[v]); into.uv.Add(uv[v]);
            }
            for (int i = from.i; i < to.i; i++) into.tri.Add(tri[i] - from.v + off);
            return b;
        }
        /// <summary>Buildings marked (a test aid).</summary>
        internal int Cuts => cuts.Count;

        /// <summary>This mesh without its marked buildings, plus its plinths (and the landmarks' close copies, CloseCopy), or null
        /// when that is empty. A building's
        /// triangles only use its own vertices (every part appends its vertices and indexes only those), so the kept
        /// triangles are re-indexed and nothing else changes.</summary>
        internal Mesh? ToCloseMesh(string name)
        {
            var map = new int[pos.Count];
            var p = new List<Vector3>(); var n = new List<Vector3>(); var c = new List<Color32>(); var u = new List<Vector2>(); var t = new List<int>();
            int k = 0;
            for (int v = 0; v < pos.Count; v++)
            {
                while (k < cuts.Count && v >= cuts[k].v1) k++;
                if (k < cuts.Count && v >= cuts[k].v0) { map[v] = -1; continue; }
                map[v] = p.Count; p.Add(pos[v]); n.Add(nrm[v]); c.Add(col[v]); u.Add(uv[v]);
            }
            k = 0;
            for (int i = 0; i < tri.Count; i++)
            {
                while (k < cuts.Count && i >= cuts[k].i1) k++;
                if (k < cuts.Count && i >= cuts[k].i0) continue;
                int at = map[tri[i]];
                if (at < 0) throw new System.InvalidOperationException("a kept triangle uses a cut building's vertex");
                t.Add(at);
            }
            if (plinths != null)
            {
                int off = p.Count;
                p.AddRange(plinths.pos); n.AddRange(plinths.nrm); c.AddRange(plinths.col); u.AddRange(plinths.uv);
                foreach (var i in plinths.tri) t.Add(i + off);
            }
            if (t.Count == 0 && (closeParts == null || closeParts.tri.Count == 0)) return null;
            return Upload(name, p, n, c, u, t, closeParts);
        }
        public int TriangleCount => tri.Count / 3;
        /// <summary>Vertices whose normal is not unit length (a degenerate face): the shader would light them as NaN on WebGL 2.</summary>
        public int BadNormals { get { int n = 0; foreach (var v in nrm) if (Mathf.Abs(v.sqrMagnitude - 1f) > 1e-3f) n++; return n; } }

        public Mesh ToMesh(string name)
        {
            if (detached.Count > 0) return Without(name, detached, farParts);
            var m = new Mesh { name = name, indexFormat = pos.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(pos); m.SetNormals(nrm); m.SetColors(col); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            m.UploadMeshData(true);   // no CPU copy: nothing reads the vertices back
            return m;
        }

        /// <summary>This mesh without the vertex and index spans <paramref name="ranges"/> (in order), as ToMesh uploads it.</summary>
        Mesh Without(string name, List<(int v0, int v1, int i0, int i1)> ranges, KitMesh? second)
        {
            var map = new int[pos.Count];
            var p = new List<Vector3>(); var n = new List<Vector3>(); var c = new List<Color32>(); var u = new List<Vector2>(); var t = new List<int>();
            int k = 0;
            for (int v = 0; v < pos.Count; v++)
            {
                while (k < ranges.Count && v >= ranges[k].v1) k++;
                if (k < ranges.Count && v >= ranges[k].v0) { map[v] = -1; continue; }
                map[v] = p.Count; p.Add(pos[v]); n.Add(nrm[v]); c.Add(col[v]); u.Add(uv[v]);
            }
            k = 0;
            for (int i = 0; i < tri.Count; i++)
            {
                while (k < ranges.Count && i >= ranges[k].i1) k++;
                if (k < ranges.Count && i >= ranges[k].i0) continue;
                int at = map[tri[i]];
                if (at < 0) throw new System.InvalidOperationException("a kept triangle uses a detached part's vertex");
                t.Add(at);
            }
            return Upload(name, p, n, c, u, t, second);
        }

        /// <summary>The mesh of these lists, with <paramref name="second"/>'s parts (if any) as submesh 1.</summary>
        static Mesh Upload(string name, List<Vector3> p, List<Vector3> n, List<Color32> c, List<Vector2> u, List<int> t, KitMesh? second)
        {
            var t1 = new List<int>();
            if (second != null && second.tri.Count > 0)
            {
                int off = p.Count;
                p.AddRange(second.pos); n.AddRange(second.nrm); c.AddRange(second.col); u.AddRange(second.uv);
                foreach (var i in second.tri) t1.Add(i + off);
            }
            var m = new Mesh { name = name, indexFormat = p.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(p); m.SetNormals(n); m.SetColors(c); m.SetUVs(0, u);
            m.subMeshCount = t1.Count > 0 ? 2 : 1;
            m.SetTriangles(t, 0);
            if (t1.Count > 0) m.SetTriangles(t1, 1);
            m.RecalculateBounds();
            m.UploadMeshData(true);
            return m;
        }

        /// <summary>The grid a kit's columns are counted on (Columns).</summary>
        public const int ColumnGrid = 4;

        /// <summary>Appends the positions of vertices [<paramref name="from"/>, <paramref name="to"/>) to <paramref name="into"/>
        /// (one settlement's part of a shared mesh). Read before ToMesh's upload drops the CPU copy.</summary>
        public void Positions(int from, int to, List<Vector3> into) { for (int i = from; i < to; i++) into.Add(pos[i]); }

        /// <summary>This mesh's vertices as columns (see the static Columns); empty for an empty mesh. Read before
        /// ToMesh's upload drops the CPU copy.</summary>
        public Bounds[] Columns() => pos.Count == 0 ? new Bounds[0] : Columns(pos);

        /// <summary>The occupied columns of a point set: for each cell of a <paramref name="grid"/>-square grid over the points'
        /// footprint that holds any, a box over its points from the lowest point of the set to the cell's highest (the map
        /// labels keep clear of these, issue #214; MapScene.TryKitColumns).</summary>
        public static Bounds[] Columns(IReadOnlyList<Vector3> points, int grid = ColumnGrid)
        {
            var min = points[0]; var max = points[0];
            foreach (var p in points) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            var lo = new Vector3[grid * grid]; var hi = new Vector3[grid * grid]; var used = new bool[grid * grid];
            float sx = Mathf.Max(max.x - min.x, 1e-3f) / grid, sz = Mathf.Max(max.z - min.z, 1e-3f) / grid;
            foreach (var p in points)
            {
                int i = Mathf.Min(grid - 1, (int)((p.x - min.x) / sx)) + grid * Mathf.Min(grid - 1, (int)((p.z - min.z) / sz));
                if (!used[i]) { used[i] = true; lo[i] = p; hi[i] = p; }
                else { lo[i] = Vector3.Min(lo[i], p); hi[i] = Vector3.Max(hi[i], p); }
            }
            var result = new List<Bounds>();
            for (int i = 0; i < used.Length; i++)
                if (used[i])
                {
                    var b = new Bounds(); b.SetMinMax(new Vector3(lo[i].x, min.y, lo[i].z), hi[i]);
                    result.Add(b);
                }
            return result.ToArray();
        }

        int V(Vector3 p, Vector3 n, Color32 c, Vector2 t) { pos.Add(p); nrm.Add(n); col.Add(c); uv.Add(t); return pos.Count - 1; }

        void Quad(int a, int b, int c, int d) { tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(a); tri.Add(c); tri.Add(d); }

        /// <summary>A flat-shaded quad a-b-c-d, clockwise seen from its front (Unity's front-face winding), with a per-corner uv.</summary>
        public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 colour, Vector2 ta, Vector2 tb, Vector2 tc, Vector2 td)
        {
            // From the diagonals: robust when one edge collapses to a point (a frond's tip), where an edge cross product
            // is zero and its normal would be NaN on WebGL 2 (normalize of zero).
            var n = Vector3.Cross(c - a, d - b).normalized;
            int i = V(a, n, colour, ta); V(b, n, colour, tb); V(c, n, colour, tc); V(d, n, colour, td);
            Quad(i, i + 1, i + 2, i + 3);
        }

        /// <summary>A flat-shaded triangle, clockwise seen from its front.</summary>
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color32 colour) => Triangle(a, b, c, colour, Vector3.Cross(b - a, c - a));

        /// <summary>A triangle, clockwise seen from its front, lit as if it faced <paramref name="normal"/> (normalised here):
        /// a boat's sail is lit as cloth that catches the sky, not as a flat plate turned from the sun (SeaCraft).</summary>
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color32 colour, Vector3 normal)
        {
            var n = normal.normalized;
            int i = V(a, n, colour, Vector2.zero); V(b, n, colour, Vector2.zero); V(c, n, colour, Vector2.zero);
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
        }

        /// <summary>The walls (and, unless <paramref name="roofed"/>, the flat roof) of a box: footprint centred on
        /// (<paramref name="x"/>, <paramref name="z"/>), <paramref name="w"/> wide along its own x after a yaw of
        /// <paramref name="yawDeg"/>, <paramref name="d"/> deep; walls from <paramref name="bottom"/> (sunk into the slope) to
        /// <paramref name="ground"/> + <paramref name="h"/>. <paramref name="metresPerReal"/> turns world metres into real
        /// metres for the facade uv (the symbolic scale). With <paramref name="parapet"/> above 0 and a flat roof, a plastered
        /// rim stands round the roof's edge (Parapet).</summary>
        public void Box(float x, float z, float yawDeg, float w, float d, float bottom, float ground, float h, Color32 wall, Color32 roof,
            Facade facade, float metresPerReal, bool roofed = false, float parapet = 0f, float parapetThickness = 0f)
        {
            var c = Corners(x, z, yawDeg, w, d);
            float top = ground + h;
            var wallC = wall; wallC.a = (byte)facade;
            float vb = (bottom - ground) / metresPerReal, vt = h / metresPerReal;
            for (int i = 0; i < 4; i++)
            {
                var a = c[i]; var b = c[(i + 1) % 4];
                float len = (b - a).magnitude / metresPerReal;
                Face(new Vector3(a.x, bottom, a.y), new Vector3(a.x, top, a.y), new Vector3(b.x, top, b.y), new Vector3(b.x, bottom, b.y), wallC,
                    new Vector2(0, vb), new Vector2(0, vt), new Vector2(len, vt), new Vector2(len, vb));
            }
            if (!roofed)
            {
                var r = roof; r.a = 0;
                Face(new Vector3(c[0].x, top, c[0].y), new Vector3(c[3].x, top, c[3].y), new Vector3(c[2].x, top, c[2].y), new Vector3(c[1].x, top, c[1].y), r,
                    Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                if (parapet > 0f && 2f * parapetThickness < Mathf.Min(w, d)) Parapet(c, Corners(x, z, yawDeg, w - 2f * parapetThickness, d - 2f * parapetThickness), top, parapet, wall);
            }
        }

        /// <summary>A flat roof's parapet: a rim <paramref name="height"/> high on the roof
        /// at <paramref name="roofY"/>, between the footprint's corners <paramref name="outer"/> and the inner corners
        /// <paramref name="inner"/> (both as Corners gives them). Its outer face continues the wall upwards but is Plain (no
        /// window rows), so it reads as a light band at the roof's edge; its inner face looks onto the roof; its top is one
        /// ring of 8 vertices. 40 vertices in all.</summary>
        void Parapet(Vector2[] outer, Vector2[] inner, float roofY, float height, Color32 colour)
        {
            colour.a = (byte)Facade.Plain;
            float y = roofY + height;
            Vector3 P(Vector2 q, float h) => new Vector3(q.x, h, q.y);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                // Outside (as Box's walls), then inside: the same edge reversed, so it faces the roof.
                Face(P(outer[i], roofY), P(outer[i], y), P(outer[j], y), P(outer[j], roofY), colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                Face(P(inner[j], roofY), P(inner[j], y), P(inner[i], y), P(inner[i], roofY), colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            }
            int o = pos.Count;
            for (int i = 0; i < 4; i++) V(P(outer[i], y), Vector3.up, colour, Vector2.zero);
            for (int i = 0; i < 4; i++) V(P(inner[i], y), Vector3.up, colour, Vector2.zero);
            // Clockwise seen from above, as the roof (outer i, inner i, inner i+1, outer i+1).
            for (int i = 0; i < 4; i++) { int j = (i + 1) % 4; Quad(o + i, o + 4 + i, o + 4 + j, o + j); }
        }

        /// <summary>A gable roof on a w x d box whose walls end at <paramref name="eaves"/>: the ridge runs along the longer
        /// side, <paramref name="rise"/> above the eaves, with a small overhang.</summary>
        public void Gable(float x, float z, float yawDeg, float w, float d, float eaves, float rise, Color32 roof, Color32 gableWall, float metresPerReal = 1f)
        {
            if (d > w) { yawDeg += 90f; (w, d) = (d, w); }
            float ow = w * 1.06f, od = d * 1.12f;
            var c = Corners(x, z, yawDeg, ow, od);           // 0 (-w,-d), 1 (+w,-d), 2 (+w,+d), 3 (-w,+d) in local axes
            var r0 = (c[0] + c[3]) * 0.5f; var r1 = (c[1] + c[2]) * 0.5f;   // ridge ends
            var g = gableWall; g.a = 0; var rf = roof; rf.a = 0;
            Vector3 P(Vector2 p, float y) => new Vector3(p.x, y, p.y);
            Face(P(c[0], eaves), P(r0, eaves + rise), P(r1, eaves + rise), P(c[1], eaves), rf, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Face(P(c[2], eaves), P(r1, eaves + rise), P(r0, eaves + rise), P(c[3], eaves), rf, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Triangle(P(c[1], eaves), P(r1, eaves + rise), P(c[2], eaves), g);
            Triangle(P(c[3], eaves), P(r0, eaves + rise), P(c[0], eaves), g);
        }

        /// <summary>A four-sided pyramid cap (a clock tower's or a tiled hip roof).</summary>
        public void Pyramid(float x, float z, float yawDeg, float w, float d, float y, float rise, Color32 colour)
        {
            var c = Corners(x, z, yawDeg, w, d); var apex = new Vector3(x, y + rise, z); colour.a = 0;
            for (int i = 0; i < 4; i++)
            {
                var a = c[i]; var b = c[(i + 1) % 4];
                Triangle(new Vector3(a.x, y, a.y), apex, new Vector3(b.x, y, b.y), colour);
            }
        }

        /// <summary>A smooth dome (a mosque's or a house's) of radius r standing on y: a hemisphere, or, with
        /// <paramref name="rise"/> under 1, a lower cap rising rise x r (a village house's shallow vault).</summary>
        public void Dome(float x, float z, float y, float r, Color32 colour, int segments = 10, int rings = 4, float rise = 1f)
        {
            colour.a = 0;
            int first = pos.Count;
            for (int j = 0; j <= rings; j++)
            {
                float phi = Mathf.PI * 0.5f * j / rings;
                for (int i = 0; i <= segments; i++)
                {
                    float th = 2f * Mathf.PI * i / segments;
                    var n = new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th));
                    // An ellipsoid's normal: the sphere's with its height scaled by 1 / rise (unchanged for a hemisphere).
                    var nn = rise == 1f ? n : new Vector3(n.x * rise, n.y, n.z * rise).normalized;
                    V(new Vector3(x, y, z) + new Vector3(n.x * r, n.y * r * rise, n.z * r), nn, colour, Vector2.zero);
                }
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < segments; i++)
                {
                    int a = first + j * (segments + 1) + i, b = a + 1, c = a + segments + 1, d = c + 1;
                    tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d);
                }
        }

        /// <summary>An upright prism of <paramref name="sides"/> sides (a minaret's shaft, a palm's trunk, a water tower's
        /// legs), radius r0 at y0 tapering to r1 at y1, smooth-shaded, open at both ends.</summary>
        public void Shaft(Vector3 b0, Vector3 b1, float r0, float r1, Color32 colour, int sides, Facade facade = Facade.Plain, float metresPerReal = 1f)
        {
            colour.a = (byte)facade;
            int first = pos.Count;
            for (int i = 0; i <= sides; i++)
            {
                float th = 2f * Mathf.PI * i / sides;
                var n = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th));
                float u = th * r0 / metresPerReal;
                V(b0 + n * r0, n, colour, new Vector2(u, 0f));
                V(b1 + n * r1, n, colour, new Vector2(u, (b1.y - b0.y) / metresPerReal));
            }
            for (int i = 0; i < sides; i++)
            {
                int a = first + 2 * i; Quad(a, a + 1, a + 3, a + 2);
            }
        }

        public void Cone(Vector3 b, float r, float h, Color32 colour, int sides)
        {
            colour.a = 0;
            var apex = b + Vector3.up * h;
            for (int i = 0; i < sides; i++)
            {
                float t0 = 2f * Mathf.PI * i / sides, t1 = 2f * Mathf.PI * (i + 1) / sides;
                Triangle(b + new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)) * r, apex, b + new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1)) * r, colour);
            }
        }

        /// <summary>A soft, lumpy crown: a sphere of radius (rx, ry, rz) around <paramref name="centre"/> whose vertices are
        /// pushed in and out by <paramref name="lumps"/> (seeded), with spherical normals so the light rolls over it like
        /// foliage, not facets. <paramref name="speck"/> is the share of faces tinted <paramref name="speckColour"/> (citrus
        /// fruit). With <paramref name="topFrom"/> under 1, the crown's top pales towards <paramref name="topColour"/>: from none
        /// where the normal's up component is <paramref name="topFrom"/> to all of it at the crown's peak (the sunlit top;
        /// up, not towards the sun, so a tree's random yaw cannot turn it away; the shader's sun then lights its sunward side).</summary>
        public void Crown(Vector3 centre, Vector3 radius, Color32 colour, ref KitRandom rnd, float lumps = 0.12f, int segments = 9, int rings = 6,
            float speck = 0f, Color32 speckColour = default, float flatBottom = 0.35f, Color32 topColour = default, float topFrom = 1f)
        {
            colour.a = 0; speckColour.a = 0; topColour.a = 0;
            int first = pos.Count;
            var cols = new Color32[(rings + 1) * (segments + 1)];
            for (int j = 0; j <= rings; j++)
            {
                float phi = Mathf.PI * j / rings - Mathf.PI * 0.5f, k0 = 0f;
                for (int i = 0; i <= segments; i++)
                {
                    float th = 2f * Mathf.PI * (i % segments) / segments;
                    var n = new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th));
                    // The last column repeats the first one's angle, so it reuses the first one's lump; otherwise the crown has a
                    // slit (#2025). The draw still happens, so every other vertex keeps its seeded shape.
                    float lump = j == 0 || j == rings ? 0f : rnd.Range(-lumps, lumps);
                    if (i == 0) k0 = lump;
                    float k = 1f + (i == segments ? k0 : lump);
                    var p = Vector3.Scale(n, radius) * k;
                    if (p.y < -radius.y * flatBottom) p.y = -radius.y * flatBottom;   // a flatter underside, as clipped trees have
                    var c = speck > 0f && rnd.Chance(speck) ? speckColour : colour;
                    if (topFrom < 1f) c = Color32.Lerp(c, topColour, Mathf.SmoothStep(0f, 1f, (n.y - topFrom) / (1f - topFrom)));
                    // Light the underside less: foliage self-shadows.
                    float shade = Mathf.Lerp(0.72f, 1.06f, (n.y + 1f) * 0.5f);
                    c = new Color32((byte)Mathf.Min(255, c.r * shade), (byte)Mathf.Min(255, c.g * shade), (byte)Mathf.Min(255, c.b * shade), 0);
                    V(centre + p, n, c, Vector2.zero);
                }
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < segments; i++)
                {
                    int a = first + j * (segments + 1) + i, b = a + 1, c = a + segments + 1, d = c + 1;
                    tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d);
                }
        }

        /// <summary>A palm frond: a drooping, tapering two-sided blade from the crown <paramref name="top"/> outwards along
        /// the horizontal <paramref name="dir"/>.</summary>
        public void Frond(Vector3 top, Vector3 dir, float length, float width, float droop, Color32 colour)
        {
            colour.a = 0;
            var side = Vector3.Cross(Vector3.up, dir).normalized * width * 0.5f;
            const int n = 4;
            var spine = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                spine[i] = top + dir * (length * t) + Vector3.up * (length * (0.25f * t - droop * t * t));
            }
            for (int i = 0; i < n; i++)
            {
                float w0 = Mathf.Sin(Mathf.PI * Mathf.Clamp((float)i / n, 0.15f, 0.92f)), w1 = Mathf.Sin(Mathf.PI * Mathf.Clamp((float)(i + 1) / n, 0.15f, 0.92f));
                var a = spine[i] - side * w0; var b = spine[i] + side * w0; var c = spine[i + 1] + side * w1; var d = spine[i + 1] - side * w1;
                Face(a, b, c, d, colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                Face(d, c, b, a, colour, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            }
        }

        static Vector2[] Corners(float x, float z, float yawDeg, float w, float d)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector2 R(float u, float v) => new Vector2(x + u * ca - v * sa, z + u * sa + v * ca);
            float hw = w * 0.5f, hd = d * 0.5f;
            // Anticlockwise seen from above (x east, z north): with (bottom, top) pairs this makes every wall face outward.
            return new[] { R(-hw, -hd), R(hw, -hd), R(hw, hd), R(-hw, hd) };
        }
    }
}
