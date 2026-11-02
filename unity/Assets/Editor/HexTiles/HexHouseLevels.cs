#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>A lighter level of Tal Heuberger's generated town houses for the hex-tile boards (#1901), made in the editor
    /// from their delivered FBX, which is never written. Each house gets three new files beside theirs, in
    /// Assets/Art/Settlements/Prototype/Models/&lt;id&gt;/: &lt;id&gt;-light.asset (the mesh), &lt;id&gt;-light.prefab (their prefab with that
    /// mesh, their material) and &lt;id&gt;-light.json (the source FBX's hash and the settings). HexModels.HouseLevels says which
    /// hex-tile ids draw one; HexHouseLevelTests holds the promises.
    ///
    /// How: quadric-error half-edge collapse (Garland and Heckbert 1997), so every vertex left is one of Tal's own, with its
    /// exact position, UV and normal; nothing is moved or interpolated. Tripo's UVs are cut into hundreds of charts (house
    /// 04: 915 charts, 80% of its positions on a seam), so locking every seam vertex removes only 17% of house 04 and its
    /// 16 Tiberias copies would still miss the 75,000 budget (measured, 29 October 2026). A seam vertex may therefore go,
    /// but only along its seam, on both sides at once, and only where the seam runs straight through it within
    /// <see cref="UvToleranceTexels"/> in each chart and <see cref="EdgeTolerance"/> in 3D: the seam stays closed and the
    /// texture border stays within a texel of Tal's (checked against every seam vertex removed so far, not only the last).
    /// Never touched: vertices on open or non-manifold edges (the open base, the
    /// source's own cracks), vertices where three or more charts meet, and vertices shared by two submeshes.</summary>
    public static class HexHouseLevels
    {
        public const string Folder = "Assets/Art/Settlements/Prototype/Models/";
        public static readonly string[] Houses =
        {
            "generated-1948-01-netanya-tiled-house-original", "generated-1948-02-netanya-two-storey-balcony-house",
            "generated-1948-03-netanya-flat-roof-porch-house", "generated-1948-04-dark-basalt-house",
            "generated-1948-05-plastered-courtyard-house-original",
        };

        /// <summary>How far a removed seam vertex may sit off the straight seam left behind, in texels of the house's colour
        /// map, and in 3D as a share of the model's diagonal.</summary>
        public const float UvToleranceTexels = 1f, EdgeTolerance = 0.002f;
        /// <summary>The largest quadric error a collapse may add, as a share of the model's diagonal. Measured on 29 October
        /// 2026: house 04 goes from 5,624 to 3,448 triangles; the seam tolerances, not this, are what stop it (at half a
        /// texel it keeps a few hundred more, which with the local palms, olives and boat would miss Tiberias's budget).</summary>
        public const float MaxError = 0.005f;

        public static string Stem(string id) => Folder + id + "/" + id;
        public static string LightStem(string id) => Stem(id) + "-light";

        [Serializable]
        sealed class Record
        {
            public string tool = nameof(HexHouseLevels), source = "", source_sha256 = "";
            public int source_triangles, triangles;
            public float uv_tolerance_texels = UvToleranceTexels, edge_tolerance = EdgeTolerance, max_error = MaxError;
            public string provenance = Provenance;
        }

        /// <summary>The derived meshes' provenance line, written into every -light.json (ASSETS.yaml has the same row).</summary>
        const string Provenance = "Derived from Tal Heuberger's original (the source FBX above) by HexHouseLevels. Same licence, "
            + "CC BY-NC-ND 4.0, credit Tal Heuberger, with Tal's permission for these lighter derived meshes (30 October 2026, "
            + "recorded by the owner in PR #1994).";

        [MenuItem("Statecraft/Hex Tiles/Build the generated houses' lighter levels")]
        public static void Build()
        {
            foreach (var id in Houses)
            {
                // Their streamed prefab (#2046): their FBX's mesh with their material, as the review prefab was.
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stem(id) + "-stream.prefab");
                var filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
                var material = prefab != null ? prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial : null;
                if (prefab == null || filter == null || filter.sharedMesh == null || material == null || material.mainTexture == null)
                    throw new InvalidOperationException("missing source house " + id);
                var source = filter.sharedMesh;
                var light = Simplify(source, UvToleranceTexels / material.mainTexture.width, EdgeTolerance, MaxError);
                light.name = id + "-light";

                // Copied into the existing asset on a rebuild, so the prefab's reference (the asset's GUID) holds.
                string meshPath = LightStem(id) + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null) AssetDatabase.CreateAsset(light, meshPath);
                else { EditorUtility.CopySerialized(light, existing); UnityEngine.Object.DestroyImmediate(light); light = existing; }

                var go = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    go.name = id + "-light";
                    // The lighter level is hex-tile art, not a streamed original: no residency distance switch.
                    foreach (var d in go.GetComponentsInChildren<Statecraft.Game.Map.Collection.OriginalModelDistance>(true))
                        UnityEngine.Object.DestroyImmediate(d);
                    go.GetComponentInChildren<MeshFilter>().sharedMesh = light;
                    PrefabUtility.SaveAsPrefabAsset(go, LightStem(id) + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }

                string fbx = AssetDatabase.GetAssetPath(source);
                var record = new Record
                {
                    source = fbx, source_sha256 = Sha256(fbx),
                    source_triangles = (int)(source.GetIndexCount(0) / 3), triangles = light.triangles.Length / 3,
                };
                File.WriteAllText(LightStem(id) + ".json", JsonUtility.ToJson(record, true) + "\n");
                Debug.Log($"HEX HOUSE LEVEL {id}: {record.source_triangles} -> {record.triangles} triangles");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static string Sha256(string assetPath)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(assetPath))).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>The lighter copy of <paramref name="source"/> (see the class summary). <paramref name="uvTolerance"/> is
        /// in UV units; <paramref name="edgeTolerance"/> and <paramref name="maxError"/> are shares of the diagonal.</summary>
        public static Mesh Simplify(Mesh source, float uvTolerance, float edgeTolerance, float maxError)
        {
            if (source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent) || source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)
                || source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1))
                throw new NotSupportedException(source.name + ": only positions, normals and one UV set are carried over");
            var p = source.vertices; var uv = source.uv; var normals = source.normals;
            int n = p.Length;

            // Corners (index triangles) and their submesh.
            List<int[]> tris = new List<int[]>(); var sub = new List<int>();
            for (int s = 0; s < source.subMeshCount; s++)
            {
                var t = source.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3) { tris.Add(new[] { t[i], t[i + 1], t[i + 2] }); sub.Add(s); }
            }
            var alive = Enumerable.Repeat(true, tris.Count).ToArray();
            var around = new HashSet<int>[n];
            for (int i = 0; i < n; i++) around[i] = new HashSet<int>();
            for (int i = 0; i < tris.Count; i++) foreach (var x in tris[i]) around[x].Add(i);

            // Positions: a vertex's copies are the vertices at its exact position (one per UV chart it is in).
            var posOf = new int[n]; var index = new Dictionary<Vector3, int>(); var copies = new List<List<int>>();
            for (int i = 0; i < n; i++)
            {
                if (!index.TryGetValue(p[i], out var k)) { index[p[i]] = k = copies.Count; copies.Add(new List<int>()); }
                posOf[i] = k; copies[k].Add(i);
            }
            int positions = copies.Count;

            // Locked: on an open or non-manifold edge, where three or more charts meet, or shared by two submeshes.
            var locked = new bool[positions];
            var edgeFaces = new Dictionary<(int, int), int>();
            for (int i = 0; i < tris.Count; i++)
                for (int c = 0; c < 3; c++)
                {
                    int a = posOf[tris[i][c]], b = posOf[tris[i][(c + 1) % 3]];
                    var e = a < b ? (a, b) : (b, a);
                    edgeFaces[e] = edgeFaces.TryGetValue(e, out var f) ? f + 1 : 1;
                }
            foreach (var (e, f) in edgeFaces) if (f != 2) locked[e.Item1] = locked[e.Item2] = true;
            for (int k = 0; k < positions; k++)
                if (copies[k].Count > 2 || copies[k].SelectMany(i => around[i]).Select(t => sub[t]).Distinct().Count() > 1) locked[k] = true;

            var b0 = new Bounds(p[0], Vector3.zero); foreach (var v in p) b0.Encapsulate(v);
            double diag = b0.size.magnitude, tol3 = edgeTolerance * diag, limit = maxError * diag * maxError * diag;

            // Each position's quadric: the planes of its triangles (a symmetric 4x4, as its 10 upper entries).
            var q = new double[positions][];
            for (int k = 0; k < positions; k++) q[k] = new double[10];
            foreach (var t in tris)
            {
                Vector3 a = p[t[0]], nrm = Vector3.Cross(p[t[1]] - a, p[t[2]] - a);
                double len = nrm.magnitude;
                if (len < 1e-12) continue;
                double nx = nrm.x / len, ny = nrm.y / len, nz = nrm.z / len, d = -(nx * a.x + ny * a.y + nz * a.z);
                var plane = new[] { nx * nx, nx * ny, nx * nz, nx * d, ny * ny, ny * nz, ny * d, nz * nz, nz * d, d * d };
                foreach (var x in t) { var qk = q[posOf[x]]; for (int j = 0; j < 10; j++) qk[j] += plane[j]; }
            }
            double Cost(int from, int to)
            {
                var a = q[from]; var b = q[to]; var v = p[copies[to][0]];
                double x = v.x, y = v.y, z = v.z;
                double Q(int j) => a[j] + b[j];
                return Q(0) * x * x + 2 * Q(1) * x * y + 2 * Q(2) * x * z + 2 * Q(3) * x + Q(4) * y * y + 2 * Q(5) * y * z + 2 * Q(6) * y
                    + Q(7) * z * z + 2 * Q(8) * z + Q(9);
            }

            IEnumerable<int> Ring(int i) => around[i].SelectMany(t => tris[t]).Where(x => x != i).Distinct();
            // A vertex's neighbours along its chart's border (each met by one of its triangles only).
            List<int> Border(int i)
            {
                var count = new Dictionary<int, int>();
                foreach (var t in around[i])
                {
                    int c = Array.IndexOf(tris[t], i);
                    foreach (var x in new[] { tris[t][(c + 1) % 3], tris[t][(c + 2) % 3] }) count[x] = count.TryGetValue(x, out var m) ? m + 1 : 1;
                }
                return count.Where(kv => kv.Value == 1).Select(kv => kv.Key).ToList();
            }
            // Distance from a point to a segment (not the infinite line, so a seam that doubles back does not pass).
            static double OffSegment(Vector3 x, Vector3 a, Vector3 b)
            {
                var d = b - a; var w = x - a; float l2 = d.sqrMagnitude;
                return l2 < 1e-20f ? w.magnitude : (w - d * Mathf.Clamp01(Vector3.Dot(w, d) / l2)).magnitude;
            }
            static (int, int) Key(int a, int b) => a < b ? (a, b) : (b, a);
            // The seam vertices removed so far that each kept border segment of a chart stands in for: every one must stay
            // within the tolerances of the segment, so the bound holds against Tal's border, not only against the last step.
            var riders = new Dictionary<(int, int), List<int>>();
            List<int> Riders(int a, int b) => riders.TryGetValue(Key(a, b), out var r) ? r : new List<int>();

            // Which copy of position "to" each copy of position "from" moves onto, or null when the collapse would cut a
            // seam, bend a chart's border by more than the tolerance, or touch a locked vertex.
            Dictionary<int, int>? Plan(int from, int to, List<((int, int) key, List<int> riders)> borders)
            {
                if (locked[from]) return null;
                var plan = new Dictionary<int, int>();
                foreach (var i in copies[from])
                {
                    var onto = Ring(i).Where(x => posOf[x] == to).ToList();
                    if (onto.Count != 1) return null;
                    plan[i] = onto[0];
                    // Any vertex on a chart's border (a seam, or the tip of a cut that ends inside a chart) keeps to it.
                    var border = Border(i);
                    if (border.Count == 0) continue;
                    if (border.Count != 2 || !border.Contains(onto[0])) return null;
                    int w = border[0] == onto[0] ? border[1] : border[0];
                    var ride = Riders(onto[0], i).Concat(Riders(i, w)).Append(i).ToList();
                    foreach (var r in ride)
                        if (OffSegment(uv[r], uv[onto[0]], uv[w]) > uvTolerance || OffSegment(p[r], p[onto[0]], p[w]) > tol3) return null;
                    borders.Add((Key(onto[0], w), ride));
                }
                return plan;
            }
            static float Area2(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

            var queue = new SortedSet<(double cost, int from, int to)>();
            void Offer(int k)
            {
                if (locked[k]) return;
                foreach (var i in copies[k]) foreach (var x in Ring(i)) queue.Add((Cost(k, posOf[x]), k, posOf[x]));
            }
            for (int k = 0; k < positions; k++) Offer(k);
            var gone = new bool[positions];

            while (queue.Count > 0)
            {
                var (cost, from, to) = queue.Min; queue.Remove(queue.Min);
                if (cost > limit) break;
                if (gone[from] || gone[to] || cost != Cost(from, to)) continue;   // stale: a fresher entry was offered
                var borders = new List<((int, int) key, List<int> riders)>();
                var plan = Plan(from, to, borders);
                if (plan == null) continue;
                // The link condition, on positions: the two only share the neighbours of the faces on their edge.
                var shared = copies[from].SelectMany(i => around[i]).Where(t => tris[t].Any(x => posOf[x] == to)).ToHashSet();
                var ringFrom = copies[from].SelectMany(Ring).Select(x => posOf[x]).Where(x => x != to).ToHashSet();
                var ringTo = copies[to].SelectMany(Ring).Select(x => posOf[x]).Where(x => x != from).ToHashSet();
                var apex = shared.SelectMany(t => tris[t]).Select(x => posOf[x]).Where(x => x != from && x != to).ToHashSet();
                if (!ringFrom.Intersect(ringTo).ToHashSet().SetEquals(apex)) continue;
                // No face may flip or fold, in 3D or in its chart.
                bool ok = true;
                foreach (var (i, onto) in plan)
                {
                    // A copy whose faces all die would take its chart (a sliver of texture) with it, and its border
                    // with no segment left to measure against: keep it.
                    if (around[i].All(shared.Contains)) { ok = false; break; }
                    foreach (var t in around[i])
                    {
                        if (shared.Contains(t)) continue;
                        var before = tris[t]; var after = before.Select(x => x == i ? onto : x).ToArray();
                        var n0 = Vector3.Cross(p[before[1]] - p[before[0]], p[before[2]] - p[before[0]]);
                        var n1 = Vector3.Cross(p[after[1]] - p[after[0]], p[after[2]] - p[after[0]]);
                        float a0 = Area2(uv[before[0]], uv[before[1]], uv[before[2]]), a1 = Area2(uv[after[0]], uv[after[1]], uv[after[2]]);
                        // In doubles: Vector3.normalized returns zero below 1e-5, which these small faces' cross products are.
                        double l0 = n0.magnitude, l1 = n1.magnitude;
                        if (l0 < 1e-12 || l1 < 1e-12 || Vector3.Dot(n0, n1) / (l0 * l1) < 0.2 || a0 * a1 <= 0f) { ok = false; break; }
                    }
                    if (!ok) break;
                }
                if (!ok) continue;
                foreach (var (i, onto) in plan)
                    foreach (var x in Ring(i).ToList()) riders.Remove(Key(i, x));
                foreach (var (key, ride) in borders) riders[key] = ride;
                foreach (var (i, onto) in plan)
                {
                    foreach (var t in around[i].ToList())
                    {
                        if (shared.Contains(t)) { alive[t] = false; foreach (var x in tris[t]) around[x].Remove(t); }
                        else { for (int c = 0; c < 3; c++) if (tris[t][c] == i) tris[t][c] = onto; around[onto].Add(t); }
                    }
                    around[i].Clear();
                }
                gone[from] = true;
                for (int j = 0; j < 10; j++) q[to][j] += q[from][j];
                var touched = new HashSet<int> { to };
                foreach (var i in copies[to]) foreach (var x in Ring(i)) touched.Add(posOf[x]);
                foreach (var k in touched) Offer(k);
            }

            // The kept vertices, in their source order, and the kept triangles per submesh.
            var keep = new int[n]; int kept = 0;
            for (int i = 0; i < n; i++) keep[i] = around[i].Count > 0 ? kept++ : -1;
            var mesh = new Mesh { indexFormat = kept > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(Enumerable.Range(0, n).Where(i => keep[i] >= 0).Select(i => p[i]).ToList());
            mesh.SetNormals(Enumerable.Range(0, n).Where(i => keep[i] >= 0).Select(i => normals[i]).ToList());
            mesh.SetUVs(0, Enumerable.Range(0, n).Where(i => keep[i] >= 0).Select(i => uv[i]).ToList());
            mesh.subMeshCount = source.subMeshCount;
            for (int s = 0; s < source.subMeshCount; s++)
                mesh.SetTriangles(Enumerable.Range(0, tris.Count).Where(t => alive[t] && sub[t] == s).SelectMany(t => tris[t]).Select(x => keep[x]).ToArray(), s);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
