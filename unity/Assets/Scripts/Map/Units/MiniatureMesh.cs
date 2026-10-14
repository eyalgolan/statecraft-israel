#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Builds one painted-miniature mesh from code: lathed solids (limbs, torsos, hulls, fuselages), ellipsoids,
    /// boxes, flat plates and thin wings, each placed by a matrix and painted with one <see cref="UnitPalette"/> slot. The
    /// paint is a palette texture: u picks the slot's column, v the shading row (<see cref="Build"/>), so one URP Lit
    /// material draws every unit and no texture file ships. Pure geometry: the same calls give the same mesh, vertex for
    /// vertex (no randomness, no time).</summary>
    public sealed class MiniatureMesh
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<int> slots = new List<int>();
        readonly List<float> shadeOverride = new List<float>();   // NaN: shade by height
        readonly List<int> tris = new List<int>();

        // The normal matrix of the part being built: every builder places all its vertices with one matrix, so it is
        // inverted once per part, not once per vertex.
        Matrix4x4 normalOf = Matrix4x4.identity, normalMatrix = Matrix4x4.identity;

        Vector3 N(Matrix4x4 m, Vector3 n)
        {
            if (m != normalOf) { normalOf = m; normalMatrix = m.inverse.transpose; }
            return normalMatrix.MultiplyVector(n).normalized;
        }

        int Add(Matrix4x4 m, Vector3 p, Vector3 n, int slot, float shade)
        {
            verts.Add(m.MultiplyPoint3x4(p)); normals.Add(N(m, n)); slots.Add(slot); shadeOverride.Add(shade);
            return verts.Count - 1;
        }

        /// <summary>A solid of revolution about the local +Y axis: <paramref name="profile"/> lists (y, radius) from bottom to
        /// top; an end with a radius above zero is closed with a flat cap, unless <paramref name="caps"/> is false (a ring,
        /// such as a plinth's rim). <paramref name="segments"/> around.</summary>
        public MiniatureMesh Lathe(Matrix4x4 m, IReadOnlyList<Vector2> profile, int slot, int segments = 14, float shade = float.NaN, bool caps = true)
        {
            if (profile.Count < 2) throw new ArgumentException("a lathe needs two profile points", nameof(profile));
            int rings = profile.Count, start = verts.Count;
            for (int i = 0; i < rings; i++)
            {
                // The profile's slope gives the normal's vertical part (central differences).
                var a = profile[Math.Max(0, i - 1)]; var b = profile[Math.Min(rings - 1, i + 1)];
                float dy = b.x - a.x, dr = b.y - a.y;
                var side = new Vector2(dy, -dr).normalized;   // (radial, vertical)
                for (int s = 0; s <= segments; s++)
                {
                    float ang = 2f * Mathf.PI * s / segments;
                    float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                    Add(m, new Vector3(c * profile[i].y, profile[i].x, sn * profile[i].y), new Vector3(c * side.x, side.y, sn * side.x), slot, shade);
                }
            }
            int stride = segments + 1;
            for (int i = 0; i < rings - 1; i++)
                for (int s = 0; s < segments; s++)
                {
                    int a = start + i * stride + s, b = a + 1, c = a + stride, d = c + 1;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            if (caps && profile[0].y > 0f) Cap(m, profile[0].x, profile[0].y, segments, slot, down: true, shade);
            if (caps && profile[rings - 1].y > 0f) Cap(m, profile[rings - 1].x, profile[rings - 1].y, segments, slot, down: false, shade);
            return this;
        }

        void Cap(Matrix4x4 m, float y, float r, int segments, int slot, bool down, float shade)
        {
            var n = down ? Vector3.down : Vector3.up;
            int centre = Add(m, new Vector3(0, y, 0), n, slot, shade);
            int first = verts.Count;
            for (int s = 0; s <= segments; s++)
            {
                float ang = 2f * Mathf.PI * s / segments;
                Add(m, new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r), n, slot, shade);
            }
            for (int s = 0; s < segments; s++)
            {
                if (down) tris.AddRange(new[] { centre, first + s, first + s + 1 });
                else tris.AddRange(new[] { centre, first + s + 1, first + s });
            }
        }

        /// <summary>A tapered rod from <paramref name="a"/> to <paramref name="b"/> (limbs, barrels, masts), radius
        /// <paramref name="ra"/> at a and <paramref name="rb"/> at b, rounded at both ends by half-spheres when
        /// <paramref name="rounded"/>.</summary>
        public MiniatureMesh Rod(Vector3 a, Vector3 b, float ra, float rb, int slot, int segments = 12, bool rounded = true, float shade = float.NaN)
        {
            var axis = b - a; float len = axis.magnitude;
            if (len <= 0f) throw new ArgumentException("a rod needs two distinct ends");
            var m = Matrix4x4.TRS(a, Quaternion.FromToRotation(Vector3.up, axis / len), Vector3.one);
            var profile = new List<Vector2>();
            if (rounded)
            {
                for (int k = 0; k <= 3; k++) { float t = -Mathf.PI / 2 + k * Mathf.PI / 6; profile.Add(new Vector2(ra * Mathf.Sin(t), ra * Mathf.Cos(t))); }
                for (int k = 0; k <= 3; k++) { float t = k * Mathf.PI / 6; profile.Add(new Vector2(len + rb * Mathf.Sin(t), rb * Mathf.Cos(t))); }
                profile[0] = new Vector2(profile[0].x, 0f); profile[profile.Count - 1] = new Vector2(profile[profile.Count - 1].x, 0f);
            }
            else { profile.Add(new Vector2(0f, ra)); profile.Add(new Vector2(len, rb)); }
            return Lathe(m, profile, slot, segments, shade);
        }

        /// <summary>A unit sphere placed by <paramref name="m"/> (its scale gives the three radii).</summary>
        public MiniatureMesh Ellipsoid(Matrix4x4 m, int slot, int segments = 14, int rings = 8, float shade = float.NaN)
        {
            var profile = new List<Vector2>(rings + 1);
            for (int i = 0; i <= rings; i++) { float t = -Mathf.PI / 2 + Mathf.PI * i / rings; profile.Add(new Vector2(Mathf.Sin(t), Mathf.Cos(t))); }
            profile[0] = new Vector2(-1f, 0f); profile[rings] = new Vector2(1f, 0f);
            return Lathe(m, profile, slot, segments, shade);
        }

        /// <summary>A unit cube centred on the origin, placed by <paramref name="m"/>, flat-shaded.</summary>
        public MiniatureMesh Box(Matrix4x4 m, int slot, float shade = float.NaN)
        {
            for (int f = 0; f < 6; f++)
            {
                var n = f switch { 0 => Vector3.right, 1 => Vector3.left, 2 => Vector3.up, 3 => Vector3.down, 4 => Vector3.forward, _ => Vector3.back };
                var u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(n, u);
                int b0 = verts.Count;
                foreach (var (su, sv) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                    Add(m, 0.5f * (n + su * u + sv * v), n, slot, shade);
                tris.AddRange(new[] { b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3 });
            }
            return this;
        }

        /// <summary>A flat two-sided polygon (a roundel, a star) in the local XZ plane, placed by <paramref name="m"/>;
        /// <paramref name="outline"/> is convex or a fan from its first point.</summary>
        public MiniatureMesh Plate(Matrix4x4 m, IReadOnlyList<Vector2> outline, int slot, float shade = float.NaN)
        {
            foreach (var side in new[] { 1, -1 })
            {
                int b0 = verts.Count;
                foreach (var p in outline) Add(m, new Vector3(p.x, 0f, p.y), Vector3.up * side, slot, shade);
                for (int i = 1; i < outline.Count - 1; i++)
                    tris.AddRange(side > 0 ? new[] { b0, b0 + i + 1, b0 + i } : new[] { b0, b0 + i, b0 + i + 1 });
            }
            return this;
        }

        /// <summary>A thin lifting surface along local +X (span) with chord along Z: the leading edge at z = +lead(s), the
        /// trailing edge at z = -trail(s) for s in [0, 1] from root to tip, thickness <paramref name="thickness"/> times the
        /// local chord. <paramref name="topSlot"/> and <paramref name="bottomSlot"/> paint each upper and lower panel by its
        /// span and chord index. Flat-shaded panels, so the camouflage reads as painted patches.</summary>
        public MiniatureMesh Wing(Matrix4x4 m, Func<float, float> lead, Func<float, float> trail, float span, float thickness,
            Func<int, int, int> topSlot, Func<int, int, int> bottomSlot, int spanSteps = 8, int chordSteps = 3)
        {
            Vector3 P(int i, int j, float side)
            {
                float s = (float)i / spanSteps, x = s * span;
                float le = lead(s), te = -trail(s), t = (float)j / chordSteps;
                float z = Mathf.Lerp(le, te, t), chord = le - te;
                float camber = Mathf.Sin(Mathf.PI * t) * thickness * chord;
                return new Vector3(x, side * camber, z);
            }
            for (int i = 0; i < spanSteps; i++)
                for (int j = 0; j < chordSteps; j++)
                {
                    Quad(m, P(i, j, 1), P(i + 1, j, 1), P(i + 1, j + 1, 1), P(i, j + 1, 1), topSlot(i, j), up: true);
                    Quad(m, P(i, j, -1), P(i, j + 1, -1), P(i + 1, j + 1, -1), P(i + 1, j, -1), bottomSlot(i, j), up: false);
                }
            return this;
        }

        void Quad(Matrix4x4 m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int slot, bool up)
        {
            var n = Vector3.Cross(c - a, b - a).normalized;
            if (n.sqrMagnitude < 0.5f) n = up ? Vector3.up : Vector3.down;
            if (up && n.y < 0f || !up && n.y > 0f) n = -n;
            int b0 = verts.Count;
            Add(m, a, n, slot, float.NaN); Add(m, b, n, slot, float.NaN); Add(m, c, n, slot, float.NaN); Add(m, d, n, slot, float.NaN);
            tris.AddRange(new[] { b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3 });   // Build orients it
        }

        /// <summary>The mesh. Each vertex's v is its shade row: by default its height within the whole model (0 at the lowest
        /// vertex, 1 at the highest), so the palette darkens feet and lifts shoulders like a painted miniature's wash and
        /// highlight; a part built with an explicit shade keeps it. Every triangle is wound here to face along its vertices'
        /// normals (Unity's front face: Cross(p1 - p0, p2 - p0) points out), so the builders above need not track winding
        /// through mirrored or rotated matrices.</summary>
        public Mesh Build(string name) => Build(name, float.NaN);

        /// <summary>As <see cref="Build(string)"/>, but every part built without an explicit shade takes
        /// <paramref name="shade"/> instead of its height: for models whose height says nothing about wear (an aircraft's
        /// wings sit lowest in its airframe and would otherwise take the feet's dark wash).</summary>
        public Mesh Build(string name, float shade)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in verts) { lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y); }
            float range = Mathf.Max(hi - lo, 1e-6f);
            var uv = new List<Vector2>(verts.Count);
            for (int i = 0; i < verts.Count; i++)
            {
                float v = !float.IsNaN(shadeOverride[i]) ? shadeOverride[i] : !float.IsNaN(shade) ? shade : (verts[i].y - lo) / range;
                uv.Add(new Vector2(UnitPalette.U(slots[i]), Mathf.Clamp01(v)));
            }
            var flipped = new int[tris.Count];
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                var face = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                bool keep = Vector3.Dot(face, normals[a] + normals[b] + normals[c]) >= 0f;
                flipped[i] = a; flipped[i + 1] = keep ? b : c; flipped[i + 2] = keep ? c : b;
            }
            var mesh = new Mesh { name = name };
            if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(flipped, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
