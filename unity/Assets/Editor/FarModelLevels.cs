#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;

namespace Statecraft.Game.EditorTools
{
    /// <summary>The far-off copies of Tal Heuberger's town building models (#2378; owner, 3 November 2026: "Ask Tal; allow
    /// them far off only", then "I have Tal's OK; go ahead", "it must be high quality"), made in the editor from each model's
    /// streamed prefab. Tal's FBX, colour map and material are never written. Each model gets five files beside its own, in
    /// Assets/Art/Settlements/Prototype/Models/&lt;id&gt;/: &lt;id&gt;-far.asset (the mesh), &lt;id&gt;-far.png (its colour map), &lt;id&gt;-far.mat
    /// (Tal's material with that map), &lt;id&gt;-far.prefab (what the far bundle streams, CollectionCatalog.FarCopy) and
    /// &lt;id&gt;-far.json (the sources' hashes, the settings and the counts). TownModelLayer draws them beyond its close switch
    /// (40 km) out to its far band (100 km), where a building is 3 to 36 px tall; the originals keep every view below 40 km
    /// (#1273).
    ///
    /// Why a bake: Tripo cut every model into thousands of UV charts (b01: 2,325 charts on 5,999 triangles, 82% of its
    /// positions on a seam), so a simplifier that keeps Tal's colour map valid (HexHouseLevels, Unity's import-time mesh
    /// LOD) cannot get near <see cref="FarTriangles"/>. So: (1) a quadric-error edge collapse on the welded positions with no
    /// seam constraint, every kept vertex one of Tal's own positions (a collapse moves a vertex onto its neighbour, never to a
    /// new point), the open boundary weighted so the base keeps its outline, no face flipped, down to FarTriangles; (2) a new
    /// colour map: each far triangle gets a <see cref="CellPx"/> square cell of a square atlas; every texel of the cell maps
    /// to a point of the far triangle (texels outside it clamp onto it, so the cell is the triangle dilated), the nearest
    /// point of Tal's original surface to that point is found, and Tal's colour map is sampled there (a box-prefiltered
    /// <see cref="SamplePx"/> copy, bilinear, <see cref="Supersample"/> squared samples per texel). Cells follow the Morton
    /// order of the triangle centroids, so each mip level blends neighbouring surface rather than unrelated faces. Normals
    /// are flat per face.</summary>
    public static class FarModelLevels
    {
        public const string Suffix = TownModels.FarSuffix;
        /// <summary>The most triangles a far copy keeps (TownModels.FarTriangles, the issue's 250-500 band): a 204 m house is
        /// 9 px at 40 km and a 840 m Tel Aviv block 36 px, where silhouettes, not facets, are what shows.</summary>
        public const int FarTriangles = TownModels.FarTriangles;
        /// <summary>The atlas cell of one far triangle (texels, square; the triangle's corners sit 1.5 texels in), the
        /// supersampling grid per texel, and the width of the box-prefiltered copy of Tal's colour map that is sampled.</summary>
        public const int CellPx = 8, Supersample = 3, SamplePx = 512;

        public static string MeshPath(string id) => TownModels.FarStem(id) + ".asset";
        public static string ColourMapPath(string id) => TownModels.FarStem(id) + ".png";
        public static string MaterialPath(string id) => TownModels.FarStem(id) + ".mat";
        /// <summary>The prefab the far bundle streams (CollectionCatalog.PrefabPath of the far copy's id).</summary>
        public static string PrefabPath(string id) => CollectionCatalog.PrefabPath(CollectionCatalog.FarCopy(id), CollectionDetail.Far);
        public static string RecordPath(string id) => TownModels.FarStem(id) + ".json";

        /// <summary>One model's record, written beside its far copy (ASSETS.yaml has the matching row).</summary>
        [Serializable]
        public sealed class Record
        {
            public string tool = nameof(FarModelLevels), source = "", source_sha256 = "", colour_map = "", colour_map_sha256 = "";
            /// <summary>atlas_px is the baked map's width (256); its height is what the cells' rows need (128 for 400 faces).</summary>
            public int source_triangles, triangles, target_triangles = FarTriangles, cell_px = CellPx, supersample = Supersample,
                sample_px = SamplePx, atlas_px;
            public string provenance = Provenance;
        }

        public const string Provenance = "Derived from Tal Heuberger's original (the source FBX and colour map above) by "
            + "FarModelLevels: a subset of the model's own vertices (exact positions, none added or moved) and a colour map "
            + "baked from the original's. Same licence, CC BY-NC-ND 4.0, credit Tal Heuberger; lighter far-off derived copy; "
            + "owner confirms Tal's permission, 3 Nov 2026 (#2378).";

        /// <summary>Builds every town model's far copy, then refreshes the editor list (TownModelAssets.Create). Run headless:
        /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod
        /// Statecraft.Game.EditorTools.FarModelLevels.Build</summary>
        [MenuItem("Statecraft/Town models/Build the far-off copies")]
        public static void Build()
        {
            foreach (var id in TownModels.Ids) BuildOne(id);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TownModelAssets.Create();
        }

        public static void BuildOne(string id)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
            var filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
            var material = prefab != null ? prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial : null;
            if (prefab == null || filter == null || filter.sharedMesh == null || material == null || material.mainTexture == null)
                throw new InvalidOperationException("missing source model " + id);
            var source = filter.sharedMesh;
            string fbx = AssetDatabase.GetAssetPath(source), jpg = AssetDatabase.GetAssetPath(material.mainTexture);
            if (!jpg.EndsWith("_basecolor.jpg", StringComparison.Ordinal)) throw new InvalidOperationException(id + ": its colour map is not the delivered JPG: " + jpg);

            // Tal's colour map as delivered, read from the file (the imported texture is GPU-only and platform-capped).
            var delivered = new Texture2D(2, 2, TextureFormat.RGB24, false);
            var atlas = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Mesh far;
            try
            {
                if (!ImageConversion.LoadImage(delivered, File.ReadAllBytes(jpg), false)) throw new InvalidOperationException(id + ": cannot read " + jpg);
                far = Derive(source, delivered, atlas);
            }
            finally { UnityEngine.Object.DestroyImmediate(delivered); }
            far.name = id + Suffix;

            // Copied into the existing assets on a rebuild, so the prefab's and material's references (their GUIDs) hold.
            File.WriteAllBytes(ColourMapPath(id), atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(ColourMapPath(id), ImportAssetOptions.ForceUpdate);
            ImportColourMap(ColourMapPath(id));
            var map = AssetDatabase.LoadAssetAtPath<Texture2D>(ColourMapPath(id)) ?? throw new InvalidOperationException(id + ": the baked map did not import");

            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath(id));
            if (existingMesh == null) AssetDatabase.CreateAsset(far, MeshPath(id));
            else { EditorUtility.CopySerialized(far, existingMesh); UnityEngine.Object.DestroyImmediate(far); far = existingMesh; }

            var mat = new Material(material) { name = id + Suffix, mainTexture = map, enableInstancing = true };
            mat.SetTexture("_MainTex", map);   // the copied legacy slot too: the far bundle must not depend on Tal's JPG
            var existingMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(id));
            if (existingMat == null) AssetDatabase.CreateAsset(mat, MaterialPath(id));
            else { EditorUtility.CopySerialized(mat, existingMat); UnityEngine.Object.DestroyImmediate(mat); mat = existingMat; }

            // The prefab the far bundle streams: the mesh with its material, stood up from Z-up as the FBX import stands.
            var go = new GameObject(id + Suffix, typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                go.GetComponent<MeshFilter>().sharedMesh = far;
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(id));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            var record = new Record
            {
                source = fbx, source_sha256 = CollectionCatalog.Sha256(fbx), colour_map = jpg, colour_map_sha256 = CollectionCatalog.Sha256(jpg),
                source_triangles = Triangles(source), triangles = Triangles(far), atlas_px = map.width,
            };
            File.WriteAllText(RecordPath(id), JsonUtility.ToJson(record, true) + "\n");
            Debug.Log($"FAR MODEL {id}: {record.source_triangles} -> {record.triangles} triangles, {record.atlas_px} px map");
        }

        /// <summary>The baked map's import: sRGB, mips (trilinear, so the 3 to 36 px buildings never pop between levels),
        /// clamped, compressed at its own size on every target.</summary>
        static void ImportColourMap(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new FileNotFoundException("no baked map", path);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Trilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.isReadable = false;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.crunchedCompression = false;
            foreach (var target in new[] { "Standalone", "WebGL" })
            {
                var s = imp.GetPlatformTextureSettings(target);
                s.overridden = true; s.maxTextureSize = 512; s.format = TextureImporterFormat.Automatic;
                s.textureCompression = TextureImporterCompression.Compressed; s.crunchedCompression = false;
                imp.SetPlatformTextureSettings(s);
            }
            imp.SaveAndReimport();
        }

        public static int Triangles(Mesh mesh) => Enumerable.Range(0, mesh.subMeshCount).Sum(s => (int)(mesh.GetIndexCount(s) / 3));

        /// <summary>The far copy of <paramref name="source"/> (see the class summary), writing no asset: the mesh, and its
        /// colour map into <paramref name="atlas"/> (resized). <paramref name="delivered"/> is Tal's colour map, readable.</summary>
        public static Mesh Derive(Mesh source, Texture2D delivered, Texture2D atlas)
        {
            var (positions, faces) = Decimate(source, FarTriangles);
            var sample = Prefilter(delivered, SamplePx);
            var uv = Bake(source, sample, positions, faces, atlas);
            // Three vertices per face (each face has its own cell of the map), flat normals.
            int n = faces.Count * 3;
            var v = new Vector3[n]; var nrm = new Vector3[n]; var tri = new int[n];
            for (int f = 0; f < faces.Count; f++)
            {
                var (a, b, c) = faces[f];
                Vector3 pa = positions[a], pb = positions[b], pc = positions[c];
                var normal = Vector3.Cross(pb - pa, pc - pa).normalized;
                v[3 * f] = pa; v[3 * f + 1] = pb; v[3 * f + 2] = pc;
                nrm[3 * f] = nrm[3 * f + 1] = nrm[3 * f + 2] = normal;
                tri[3 * f] = 3 * f; tri[3 * f + 1] = 3 * f + 1; tri[3 * f + 2] = 3 * f + 2;
            }
            var mesh = new Mesh { indexFormat = n > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(v); mesh.SetNormals(nrm); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Quadric-error edge collapse (Garland and Heckbert 1997) on the positions of <paramref name="source"/>
        /// (its vertices welded by exact position), down to <paramref name="target"/> faces or until no collapse is valid. A
        /// collapse moves one position onto a neighbouring one (whichever end costs less), so every position kept is the
        /// source's own. Boundary edges add a perpendicular plane weighted by their length, so the open base keeps its
        /// outline; a collapse that would flip a face (its normal turning past 78 degrees) or pinch the surface (the link
        /// condition) is skipped. Returns the kept positions and the faces over them, in a deterministic order.</summary>
        public static (List<Vector3> positions, List<(int a, int b, int c)> faces) Decimate(Mesh source, int target)
        {
            var sv = source.vertices;
            var posOf = new int[sv.Length]; var index = new Dictionary<Vector3, int>(); var p = new List<Vector3>();
            for (int i = 0; i < sv.Length; i++)
            {
                if (!index.TryGetValue(sv[i], out int k)) { index[sv[i]] = k = p.Count; p.Add(sv[i]); }
                posOf[i] = k;
            }
            int n = p.Count;
            var tris = new List<int[]>();
            for (int s = 0; s < source.subMeshCount; s++)
            {
                var t = source.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3)
                {
                    int a = posOf[t[i]], b = posOf[t[i + 1]], c = posOf[t[i + 2]];
                    if (a != b && b != c && a != c) tris.Add(new[] { a, b, c });
                }
            }
            var alive = Enumerable.Repeat(true, tris.Count).ToArray(); int liveFaces = tris.Count;
            var around = new HashSet<int>[n];
            for (int i = 0; i < n; i++) around[i] = new HashSet<int>();
            for (int i = 0; i < tris.Count; i++) foreach (var x in tris[i]) around[x].Add(i);

            var b0 = new Bounds(p[0], Vector3.zero); foreach (var v in p) b0.Encapsulate(v);
            double diag = Math.Max(b0.size.magnitude, 1e-9);
            // Quadrics as the 10 upper entries of the symmetric 4x4, scaled by the diagonal so the costs are dimensionless.
            var q = new double[n][];
            for (int k = 0; k < n; k++) q[k] = new double[10];
            void AddPlane(int k, double nx, double ny, double nz, double d, double w)
            {
                var qk = q[k];
                qk[0] += w * nx * nx; qk[1] += w * nx * ny; qk[2] += w * nx * nz; qk[3] += w * nx * d; qk[4] += w * ny * ny;
                qk[5] += w * ny * nz; qk[6] += w * ny * d; qk[7] += w * nz * nz; qk[8] += w * nz * d; qk[9] += w * d * d;
            }
            var edgeFaces = new Dictionary<(int, int), int>();
            static (int, int) Key(int a, int b) => a < b ? (a, b) : (b, a);
            foreach (var t in tris)
            {
                Vector3 a = p[t[0]], nrm = Vector3.Cross(p[t[1]] - a, p[t[2]] - a);
                double len = nrm.magnitude;
                for (int c = 0; c < 3; c++) { var e = Key(t[c], t[(c + 1) % 3]); edgeFaces[e] = edgeFaces.TryGetValue(e, out var f) ? f + 1 : 1; }
                if (len < 1e-18) continue;
                double nx = nrm.x / len, ny = nrm.y / len, nz = nrm.z / len, d = -(nx * a.x + ny * a.y + nz * a.z) / diag;
                double area = 0.5 * len / (diag * diag);   // area-weighted: the small triangles of a noisy surface count less
                foreach (var x in t) AddPlane(x, nx, ny, nz, d, area);
            }
            // Boundary edges: a plane through the edge, perpendicular to the face, weighted by the edge's length (squared,
            // like an area) so the open base and any crack keep their outline.
            for (int i = 0; i < tris.Count; i++)
                for (int c = 0; c < 3; c++)
                {
                    int ia = tris[i][c], ib = tris[i][(c + 1) % 3];
                    if (edgeFaces[Key(ia, ib)] != 1) continue;
                    Vector3 a = p[ia], e = p[ib] - a, faceN = Vector3.Cross(p[tris[i][1]] - p[tris[i][0]], p[tris[i][2]] - p[tris[i][0]]);
                    var nrm = Vector3.Cross(e, faceN); double len = nrm.magnitude;
                    if (len < 1e-18) continue;
                    double nx = nrm.x / len, ny = nrm.y / len, nz = nrm.z / len, d = -(nx * a.x + ny * a.y + nz * a.z) / diag;
                    double w = (double)e.sqrMagnitude / (diag * diag);
                    AddPlane(ia, nx, ny, nz, d, w); AddPlane(ib, nx, ny, nz, d, w);
                }
            double Cost(int from, int to)
            {
                var a = q[from]; var b = q[to]; var v = p[to];
                double x = v.x / diag, y = v.y / diag, z = v.z / diag;
                double Q(int j) => a[j] + b[j];
                return Q(0) * x * x + 2 * Q(1) * x * y + 2 * Q(2) * x * z + 2 * Q(3) * x + Q(4) * y * y + 2 * Q(5) * y * z + 2 * Q(6) * y
                    + Q(7) * z * z + 2 * Q(8) * z + Q(9);
            }
            IEnumerable<int> Ring(int i) => around[i].SelectMany(t => tris[t]).Where(x => x != i).Distinct();
            var queue = new SortedSet<(double cost, int from, int to)>();
            void Offer(int k) { foreach (var x in Ring(k).ToList()) { queue.Add((Cost(k, x), k, x)); queue.Add((Cost(x, k), x, k)); } }
            var gone = new bool[n];
            // Three passes, each from the cheapest collapse left: with the link condition (a clean surface), without it
            // (Tripo's cracks and T-junctions leave corners no manifold collapse can take), and last allowing a face to turn
            // up to a right angle (never past it: no inverted face). Most models finish in the first.
            for (int pass = 0; pass < 3 && liveFaces > target; pass++)
            {
                bool link = pass == 0; double flipLimit = pass < 2 ? 0.2 : 0.0;
                queue.Clear();
                for (int k = 0; k < n; k++) if (!gone[k]) foreach (var x in Ring(k).ToList()) queue.Add((Cost(k, x), k, x));
                while (liveFaces > target && queue.Count > 0)
                {
                    var (cost, from, to) = queue.Min; queue.Remove(queue.Min);
                    if (gone[from] || gone[to] || cost != Cost(from, to) || !around[from].Any(t => tris[t].Contains(to))) continue;
                    var shared = around[from].Where(t => tris[t].Contains(to)).ToHashSet();
                    if (link)
                    {
                        // The link condition: the two share only the third corners of the faces on their edge.
                        var apex = shared.SelectMany(t => tris[t]).Where(x => x != from && x != to).ToHashSet();
                        var common = Ring(from).Where(x => x != to).ToHashSet(); common.IntersectWith(Ring(to).Where(x => x != from));
                        if (!common.SetEquals(apex)) continue;
                    }
                    // No face may flip.
                    bool ok = true;
                    foreach (var t in around[from])
                    {
                        if (shared.Contains(t)) continue;
                        var before = tris[t]; var after = before.Select(x => x == from ? to : x).ToArray();
                        if (after[0] == after[1] || after[1] == after[2] || after[0] == after[2]) { ok = false; break; }
                        var n0 = Vector3.Cross(p[before[1]] - p[before[0]], p[before[2]] - p[before[0]]);
                        var n1 = Vector3.Cross(p[after[1]] - p[after[0]], p[after[2]] - p[after[0]]);
                        double l0 = n0.magnitude, l1 = n1.magnitude;
                        if (l0 < 1e-18 || l1 < 1e-18 || Vector3.Dot(n0, n1) / (l0 * l1) < flipLimit) { ok = false; break; }
                    }
                    if (!ok) continue;
                    foreach (var t in around[from].ToList())
                    {
                        if (shared.Contains(t)) { alive[t] = false; liveFaces--; foreach (var x in tris[t]) around[x].Remove(t); }
                        else { for (int c = 0; c < 3; c++) if (tris[t][c] == from) tris[t][c] = to; around[to].Add(t); }
                    }
                    around[from].Clear(); gone[from] = true;
                    for (int j = 0; j < 10; j++) q[to][j] += q[from][j];
                    Offer(to); foreach (var x in Ring(to).ToList()) Offer(x);
                }
            }
            var keep = new int[n]; int kept = 0;
            var positions = new List<Vector3>();
            for (int i = 0; i < n; i++) { keep[i] = around[i].Count > 0 ? kept++ : -1; if (keep[i] >= 0) positions.Add(p[i]); }
            var faces = new List<(int, int, int)>();
            for (int t = 0; t < tris.Count; t++) if (alive[t]) faces.Add((keep[tris[t][0]], keep[tris[t][1]], keep[tris[t][2]]));
            return (positions, faces);
        }

        /// <summary>A box-prefiltered copy of <paramref name="source"/>, <paramref name="width"/> texels wide (the source as it
        /// is when it is no wider), so that sampling it bilinearly averages the source's texels under each far texel.</summary>
        static (Color[] pixels, int w, int h) Prefilter(Texture2D source, int width)
        {
            var src = source.GetPixels32();
            int sw = source.width, sh = source.height, f = Math.Max(1, sw / width), w = sw / f, h = sh / f;
            var dst = new Color[w * h];
            float inv = 1f / (255f * f * f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (int j = 0; j < f; j++) for (int i = 0; i < f; i++) { var c = src[(y * f + j) * sw + x * f + i]; r += c.r; g += c.g; b += c.b; }
                    dst[y * w + x] = new Color(r * inv, g * inv, b * inv, 1f);
                }
            return (dst, w, h);
        }

        static Color Bilinear((Color[] pixels, int w, int h) map, float u, float v)
        {
            u -= Mathf.Floor(u); v -= Mathf.Floor(v);
            float x = u * map.w - 0.5f, y = v * map.h - 0.5f;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y); float fx = x - x0, fy = y - y0;
            Color At(int px, int py) => map.pixels[Mathf.Clamp(py, 0, map.h - 1) * map.w + Mathf.Clamp(px, 0, map.w - 1)];
            return Color.Lerp(Color.Lerp(At(x0, y0), At(x0 + 1, y0), fx), Color.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), fx), fy);
        }

        /// <summary>The cell of each far face in the atlas (Morton order of the centroids), the corner UVs (three per face,
        /// in face order), and the atlas itself, baked from the original surface (see the class summary).</summary>
        static List<Vector2> Bake(Mesh source, (Color[] pixels, int w, int h) map, List<Vector3> positions, List<(int a, int b, int c)> faces, Texture2D atlas)
        {
            var surface = new Surface(source);
            // 32 cells across (256 texels) and as many rows as the faces need, to a power of two: 400 faces fill 13 of 16 rows
            // of a 256 x 128 map. The texels no cell uses take the mean colour, so the smallest mip levels stay the building's.
            int cells = Mathf.Max(32, Mathf.CeilToInt(Mathf.Sqrt(faces.Count)));
            int width = Mathf.NextPowerOfTwo(cells * CellPx); cells = width / CellPx;
            int rows = (faces.Count + cells - 1) / cells, height = Mathf.NextPowerOfTwo(rows * CellPx);
            var order = Enumerable.Range(0, faces.Count).OrderBy(f => Morton(positions, faces[f], surface.Bounds)).ThenBy(f => f).ToList();
            var pixels = new Color[width * height];
            var uv = new Vector2[faces.Count * 3];
            float inset = 1.5f;
            for (int slot = 0; slot < order.Count; slot++)
            {
                int f = order[slot]; var (a, b, c) = faces[f];
                int cx = (slot % cells) * CellPx, cy = (slot / cells) * CellPx;
                // The corners in texels: the right-angled corner at the cell's origin, the legs along its two sides.
                Vector2 t0 = new Vector2(cx + inset, cy + inset), t1 = new Vector2(cx + CellPx - inset, cy + inset), t2 = new Vector2(cx + inset, cy + CellPx - inset);
                var scale = new Vector2(1f / width, 1f / height);
                uv[3 * f] = t0 * scale; uv[3 * f + 1] = t1 * scale; uv[3 * f + 2] = t2 * scale;
                Vector3 pa = positions[a], pb = positions[b], pc = positions[c];
                float legs = CellPx - 2f * inset;
                for (int y = 0; y < CellPx; y++)
                    for (int x = 0; x < CellPx; x++)
                    {
                        float r = 0, g = 0, bl = 0;
                        for (int sy = 0; sy < Supersample; sy++)
                            for (int sx = 0; sx < Supersample; sx++)
                            {
                                // The sample's barycentric coordinates in the cell triangle, clamped onto it (the dilation).
                                float px = x + (sx + 0.5f) / Supersample, py = y + (sy + 0.5f) / Supersample;
                                float wb = (px - inset) / legs, wc = (py - inset) / legs;
                                wb = Mathf.Clamp01(wb); wc = Mathf.Clamp01(wc);
                                if (wb + wc > 1f) { float s = wb + wc; wb /= s; wc /= s; }
                                var point = pa + (pb - pa) * wb + (pc - pa) * wc;
                                surface.Nearest(point, out var st);
                                var col = Bilinear(map, st.x, st.y);
                                r += col.r; g += col.g; bl += col.b;
                            }
                        float inv = 1f / (Supersample * Supersample);
                        pixels[(cy + y) * width + cx + x] = new Color(r * inv, g * inv, bl * inv, 1f);
                    }
            }
            int used = faces.Count * CellPx * CellPx; var mean = Color.black;
            for (int i = 0; i < used; i++) mean += pixels[i] / used;
            for (int i = used; i < pixels.Length; i++) pixels[i] = mean;
            atlas.Reinitialize(width, height, TextureFormat.RGB24, false);
            atlas.SetPixels(pixels); atlas.Apply(false, false);
            return uv.ToList();
        }

        static ulong Morton(List<Vector3> positions, (int a, int b, int c) face, Bounds bounds)
        {
            var centre = (positions[face.a] + positions[face.b] + positions[face.c]) / 3f;
            ulong code = 0;
            for (int axis = 0; axis < 3; axis++)
            {
                float t = bounds.size[axis] > 1e-9f ? (centre[axis] - bounds.min[axis]) / bounds.size[axis] : 0f;
                ulong v = (ulong)Mathf.Clamp(Mathf.RoundToInt(t * 1023f), 0, 1023);
                for (int bit = 0; bit < 10; bit++) code |= ((v >> bit) & 1UL) << (3 * bit + axis);
            }
            return code;
        }

        /// <summary>Tal's original surface: its faces with their own corner UVs, binned on a uniform grid for nearest-point
        /// queries.</summary>
        sealed class Surface
        {
            public readonly Bounds Bounds;
            readonly Vector3[] v; readonly Vector2[] uv; readonly int[] tri; readonly int faces;
            const int G = 24;
            readonly List<int>[] grid = new List<int>[G * G * G];
            readonly Vector3 cell;
            public Surface(Mesh mesh)
            {
                v = mesh.vertices; uv = mesh.uv;
                tri = Enumerable.Range(0, mesh.subMeshCount).SelectMany(s => mesh.GetTriangles(s)).ToArray(); faces = tri.Length / 3;
                Bounds = new Bounds(v[0], Vector3.zero); foreach (var x in v) Bounds.Encapsulate(x);
                Bounds.Expand(Bounds.size.magnitude * 1e-3f + 1e-6f);
                cell = Bounds.size / G;
                for (int i = 0; i < grid.Length; i++) grid[i] = new List<int>();
                for (int f = 0; f < faces; f++)
                {
                    var (lo, hi) = (Cell(Vector3.Min(Vector3.Min(v[tri[3 * f]], v[tri[3 * f + 1]]), v[tri[3 * f + 2]])),
                        Cell(Vector3.Max(Vector3.Max(v[tri[3 * f]], v[tri[3 * f + 1]]), v[tri[3 * f + 2]])));
                    for (int z = lo.z; z <= hi.z; z++) for (int y = lo.y; y <= hi.y; y++) for (int x = lo.x; x <= hi.x; x++) grid[(z * G + y) * G + x].Add(f);
                }
            }
            Vector3Int Cell(Vector3 p) => new Vector3Int(
                Mathf.Clamp(Mathf.FloorToInt((p.x - Bounds.min.x) / cell.x), 0, G - 1),
                Mathf.Clamp(Mathf.FloorToInt((p.y - Bounds.min.y) / cell.y), 0, G - 1),
                Mathf.Clamp(Mathf.FloorToInt((p.z - Bounds.min.z) / cell.z), 0, G - 1));

            /// <summary>The nearest face's index; <paramref name="st"/> is the UV of the nearest point on it.</summary>
            public int Nearest(Vector3 p, out Vector2 st)
            {
                var c = Cell(p); int best = -1; float bestD = float.PositiveInfinity; Vector3 bestBary = default;
                float minCell = Mathf.Min(cell.x, Mathf.Min(cell.y, cell.z));
                for (int r = 0; r < G; r++)
                {
                    // Once the explored cube's inner radius exceeds the best distance, nothing outside it can be nearer.
                    if (best >= 0 && (r - 1) * minCell > bestD) break;
                    for (int z = Math.Max(0, c.z - r); z <= Math.Min(G - 1, c.z + r); z++)
                        for (int y = Math.Max(0, c.y - r); y <= Math.Min(G - 1, c.y + r); y++)
                            for (int x = Math.Max(0, c.x - r); x <= Math.Min(G - 1, c.x + r); x++)
                            {
                                if (Math.Max(Math.Abs(x - c.x), Math.Max(Math.Abs(y - c.y), Math.Abs(z - c.z))) != r) continue;
                                foreach (int f in grid[(z * G + y) * G + x])
                                {
                                    var bary = ClosestPoint(p, v[tri[3 * f]], v[tri[3 * f + 1]], v[tri[3 * f + 2]], out float d);
                                    if (d < bestD || (d == bestD && f < best)) { bestD = d; best = f; bestBary = bary; }
                                }
                            }
                }
                if (best < 0) throw new InvalidOperationException("no face near " + p);
                st = uv[tri[3 * best]] * bestBary.x + uv[tri[3 * best + 1]] * bestBary.y + uv[tri[3 * best + 2]] * bestBary.z;
                return best;
            }

            /// <summary>The closest point of triangle abc to p, as barycentric coordinates (Ericson, Real-Time Collision
            /// Detection, 5.1.5); <paramref name="distance"/> is its distance to p.</summary>
            static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out float distance)
            {
                Vector3 ab = b - a, ac = c - a, ap = p - a;
                float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
                Vector3 bary;
                if (d1 <= 0f && d2 <= 0f) bary = new Vector3(1f, 0f, 0f);
                else
                {
                    Vector3 bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
                    if (d3 >= 0f && d4 <= d3) bary = new Vector3(0f, 1f, 0f);
                    else
                    {
                        float vc = d1 * d4 - d3 * d2;
                        if (vc <= 0f && d1 >= 0f && d3 <= 0f) { float t = d1 / (d1 - d3); bary = new Vector3(1f - t, t, 0f); }
                        else
                        {
                            Vector3 cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
                            if (d6 >= 0f && d5 <= d6) bary = new Vector3(0f, 0f, 1f);
                            else
                            {
                                float vb = d5 * d2 - d1 * d6;
                                if (vb <= 0f && d2 >= 0f && d6 <= 0f) { float t = d2 / (d2 - d6); bary = new Vector3(1f - t, 0f, t); }
                                else
                                {
                                    float va = d3 * d6 - d5 * d4;
                                    if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f) { float t = (d4 - d3) / ((d4 - d3) + (d5 - d6)); bary = new Vector3(0f, 1f - t, t); }
                                    else
                                    {
                                        float denom = 1f / (va + vb + vc); float vv = vb * denom, ww = vc * denom;
                                        bary = new Vector3(1f - vv - ww, vv, ww);
                                    }
                                }
                            }
                        }
                    }
                }
                distance = (a * bary.x + b * bary.y + c * bary.z - p).magnitude;
                return bary;
            }
        }
    }
}
