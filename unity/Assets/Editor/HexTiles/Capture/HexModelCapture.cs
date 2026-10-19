#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles.Capture
{
    /// <summary>Whether a building's mesh is whole, measured in the placement's unit frame (HexModels.Parts: y up whatever
    /// the source mesh's). The test is what the player sees: the buildings' materials cull back faces and Tripo's
    /// buildings are open underneath, so a view ray that meets the model but no front face of it (in through a missing
    /// wall face, out past the culled inside of the far wall) shows the ground through the building (SeeThrough, of the
    /// rays that meet the model at all, from eight sides at the board camera's elevations). Rays whose first face is a
    /// back face (BackFirst) are reported but do not decide: one-sided railings and awnings seen from behind count there,
    /// and the wall behind them still shows (measured on b02b, 19 October 2026). Open (boundary) edges, after welding
    /// vertices by position (UV and normal seams split them), are reported too: the shipped meshes have thousands
    /// (T-junctions and hairline cracks) that never show. Also how a sandbox prefab and the town model drawn for it line
    /// up (Facing).</summary>
    public static class HexMeshCheck
    {
        public readonly struct Result
        {
            public readonly int Triangles, Open, OpenOffBase, NonManifold, Hits, BackFirst, SeeThrough;
            public Result(int triangles, int open, int offBase, int nonManifold, int hits, int backFirst, int seeThrough)
            {
                Triangles = triangles; Open = open; OpenOffBase = offBase; NonManifold = nonManifold; Hits = hits; BackFirst = backFirst; SeeThrough = seeThrough;
            }
            /// <summary>The share of view rays on the model that see the ground through it.</summary>
            public float SeeThroughShare => Hits == 0 ? 0f : SeeThrough / (float)Hits;
            /// <summary>The share of view rays on the model whose first face is a back face.</summary>
            public float BackFirstShare => Hits == 0 ? 0f : BackFirst / (float)Hits;
            public override string ToString() =>
                $"{Triangles} tris, {SeeThroughShare:P2} see-through ({SeeThrough} of {Hits} rays), {BackFirstShare:P2} back face first, {Open} open edges ({OpenOffBase} off the base), {NonManifold} non-manifold";
        }

        /// <summary>The view elevations (degrees down) and sides the rays come from, and the ray grid's side per view.</summary>
        static readonly float[] Elevations = { 20f, 35f, 50f };
        const int Sides = 8, Grid = 96;

        /// <summary>A prefab's static meshes as the hex tiles draw them (HexModels.Parts, Fit.Width).</summary>
        public static Result Of(GameObject prefab, float weld = 1e-5f) =>
            Of(HexModels.Parts(prefab, Fit.Width).GroupBy(p => (p.Mesh, p.Local)).Select(g => (g.Key.Mesh, g.Key.Local)), weld);

        /// <summary>Meshes with their matrices into a y-up frame; welded together, so a model split over several meshes or
        /// submeshes counts as one surface.</summary>
        public static Result Of(IEnumerable<(Mesh mesh, Matrix4x4 local)> parts, float weld = 1e-5f)
        {
            var pts = new List<Vector3>(); var tris = new List<int>();
            foreach (var (mesh, local) in parts)
            {
                int start = pts.Count;
                pts.AddRange(mesh.vertices.Select(v => local.MultiplyPoint3x4(v)));
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                    tris.AddRange(mesh.GetTriangles(s).Select(i => i + start));
                }
            }
            if (pts.Count == 0) return new Result(0, 0, 0, 0, 0, 0, 0);
            var b = new Bounds(pts[0], Vector3.zero); foreach (var p in pts) b.Encapsulate(p);
            var (open, offBase, nonManifold) = Edges(pts, tris, b, weld);
            var (hits, backFirst, through) = Rays(pts, tris, b);
            return new Result(tris.Count / 3, open, offBase, nonManifold, hits, backFirst, through);
        }

        /// <summary>Open edges (one face), those off the base plane (min y + 0.5% of the height) and non-manifold edges
        /// (three or more faces), once vertices within <paramref name="weld"/> of the model's size are one.</summary>
        static (int open, int offBase, int nonManifold) Edges(List<Vector3> pts, List<int> tris, Bounds b, float weld)
        {
            float grid = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) * weld, baseY = b.min.y + 0.005f * b.size.y;
            var cells = new Dictionary<(long, long, long), int>(); var id = new int[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                var k = ((long)Mathf.Round(pts[i].x / grid), (long)Mathf.Round(pts[i].y / grid), (long)Mathf.Round(pts[i].z / grid));
                if (!cells.TryGetValue(k, out var w)) cells[k] = w = i;
                id[i] = w;
            }
            var edges = new Dictionary<(int, int), int>();
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                int a = id[tris[t]], c = id[tris[t + 1]], d = id[tris[t + 2]];
                if (a == c || c == d || d == a) continue;   // degenerate once welded
                foreach (var (u, v) in new[] { (a, c), (c, d), (d, a) })
                {
                    var e = u < v ? (u, v) : (v, u);
                    edges[e] = edges.TryGetValue(e, out var n) ? n + 1 : 1;
                }
            }
            int open = 0, offBase = 0, nonManifold = 0;
            foreach (var (e, n) in edges)
            {
                if (n > 2) nonManifold++;
                if (n != 1) continue;
                open++;
                if (pts[e.Item1].y > baseY || pts[e.Item2].y > baseY) offBase++;
            }
            return (open, offBase, nonManifold);
        }

        /// <summary>Parallel view rays from <see cref="Sides"/> sides at each of <see cref="Elevations"/>, on a
        /// <see cref="Grid"/>-square grid over the model: how many meet a triangle, how many of those meet a back face
        /// first, and how many meet no front face at all (Unity's front face winds clockwise to the viewer, so
        /// Cross(b - a, c - a) points at it). A software
        /// orthographic raster: each view bins the projected triangles by grid cell.</summary>
        static (int hits, int backFirst, int through) Rays(List<Vector3> pts, List<int> tris, Bounds b)
        {
            int hits = 0, backFirst = 0, through = 0, n = tris.Count / 3;
            float radius = b.extents.magnitude, cell = 2f * radius / Grid;
            var bins = new List<int>[Grid * Grid];
            for (int i = 0; i < bins.Length; i++) bins[i] = new List<int>();
            var uv = new Vector3[pts.Count];
            int Cell(float v) => Mathf.Clamp(Mathf.FloorToInt((v + radius) / cell), 0, Grid - 1);
            foreach (var elevation in Elevations)
                for (int side = 0; side < Sides; side++)
                {
                    // Looking down and in; each elevation's sides offset a little, so the views do not repeat.
                    var dir = Quaternion.Euler(elevation, 360f / Sides * side + 7.5f * System.Array.IndexOf(Elevations, elevation), 0f) * Vector3.forward;
                    var right = Vector3.Cross(Vector3.up, dir).normalized; var up = Vector3.Cross(dir, right);
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var p = pts[i] - b.center;
                        uv[i] = new Vector3(Vector3.Dot(p, right), Vector3.Dot(p, up), Vector3.Dot(p, dir));
                    }
                    foreach (var bin in bins) bin.Clear();
                    for (int t = 0; t < n; t++)
                    {
                        Vector3 p0 = uv[tris[3 * t]], p1 = uv[tris[3 * t + 1]], p2 = uv[tris[3 * t + 2]];
                        int x0 = Cell(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x))), x1 = Cell(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)));
                        int y0 = Cell(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y))), y1 = Cell(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)));
                        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) bins[y * Grid + x].Add(t);
                    }
                    for (int c = 0; c < Grid * Grid; c++)
                    {
                        // The ray through the cell's centre: its nearest triangle, and which side of it faces the ray.
                        float rx = -radius + (c % Grid + 0.5f) * cell, ry = -radius + (c / Grid + 0.5f) * cell, near = float.MaxValue;
                        bool back = false, front = false;
                        foreach (var t in bins[c])
                        {
                            Vector3 p0 = uv[tris[3 * t]], p1 = uv[tris[3 * t + 1]], p2 = uv[tris[3 * t + 2]];
                            float area = (p1.x - p0.x) * (p2.y - p0.y) - (p2.x - p0.x) * (p1.y - p0.y);
                            if (Mathf.Abs(area) < 1e-12f) continue;
                            float w1 = ((rx - p0.x) * (p2.y - p0.y) - (p2.x - p0.x) * (ry - p0.y)) / area;
                            float w2 = ((p1.x - p0.x) * (ry - p0.y) - (rx - p0.x) * (p1.y - p0.y)) / area;
                            if (w1 < 0f || w2 < 0f || w1 + w2 > 1f) continue;
                            float z = p0.z + w1 * (p1.z - p0.z) + w2 * (p2.z - p0.z);
                            var a = pts[tris[3 * t]];
                            bool isBack = Vector3.Dot(Vector3.Cross(pts[tris[3 * t + 1]] - a, pts[tris[3 * t + 2]] - a), dir) > 0f;
                            front |= !isBack;
                            if (z >= near) continue;
                            near = z; back = isBack;
                        }
                        if (near == float.MaxValue) continue;
                        hits++;
                        if (back) backFirst++;
                        if (!front) through++;
                    }
                }
            return (hits, backFirst, through);
        }

        /// <summary>The names of <see cref="Facing"/>'s scores: quarter turns about y, and the same mirrored in x.</summary>
        public static readonly string[] FacingNames = { "0", "90", "180", "270", "m0", "m90", "m180", "m270" };

        /// <summary>For each of the eight turns and mirrors of <paramref name="from"/>'s vertices, their mean distance to the
        /// nearest vertex of <paramref name="to"/>, both in the unit frame (Fit.Width): index 0 (as they are) is least when
        /// the two face the same way.</summary>
        public static float[] Facing(GameObject from, GameObject to)
        {
            static List<Vector3> Points(GameObject go, int most)
            {
                var all = HexModels.Parts(go, Fit.Width).GroupBy(p => (p.Mesh, p.Local))
                    .SelectMany(g => g.Key.Mesh.vertices.Select(v => g.Key.Local.MultiplyPoint3x4(v))).ToList();
                int step = Mathf.Max(1, all.Count / most);
                return all.Where((_, i) => i % step == 0).ToList();
            }
            var a = Points(from, 600); var b = Points(to, 6000);
            var scores = new float[8];
            for (int k = 0; k < 8; k++)
            {
                var turn = Quaternion.Euler(0f, 90f * (k % 4), 0f);
                double sum = 0;
                foreach (var p0 in a)
                {
                    var p = turn * (k >= 4 ? new Vector3(-p0.x, p0.y, p0.z) : p0);
                    float best = float.MaxValue;
                    foreach (var q in b) { float d = (p - q).sqrMagnitude; if (d < best) best = d; }
                    sum += Mathf.Sqrt(best);
                }
                scores[k] = (float)(sum / a.Count);
            }
            return scores;
        }
    }

    /// <summary>Close shots of every building model the hex tiles draw, alone on a plain ground, from four sides (one 2x2
    /// sheet per model at -captureSize, default 3840x2160): before (the sandbox prefab, when present on this machine) and
    /// after (what the id resolves to now, HexModels.Prefab). Uses the hex captures' stage (HexBoardCapture.Stage, Shoot).
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity
    ///   -executeMethod Statecraft.HexTiles.Capture.HexModelCapture.Run [-captureDir asset-staging/ui-local/hex-tiles/round2/buildings/models]</summary>
    public static class HexModelCapture
    {
        /// <summary>How large a model stands in the shot (m across), and how far the camera stands from it.</summary>
        const float WidthM = 1000f, DistanceM = 2600f;

        public static void Run()
        {
            try
            {
                var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                var dir = Path.GetFullPath(Path.Combine(repo, HexBoardCapture.Arg("-captureDir") ?? "asset-staging/ui-local/hex-tiles/round2/buildings/models"));
                var size = (HexBoardCapture.Arg("-captureSize") ?? "3840x2160").Split('x');
                Capture(dir, int.Parse(size[0]), int.Parse(size[1]));
            }
            catch (Exception e)
            {
                Debug.LogError("HEX MODEL CAPTURE FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>The building ids the hex tiles may draw: every sandbox id with a town model; with -allTown also every
        /// town model at its full and mid level.</summary>
        static IEnumerable<string> Ids() => HexModels.TownAliases.Select(a => a.id).Distinct().OrderBy(i => i, StringComparer.Ordinal)
            .Concat(Environment.GetCommandLineArgs().Contains("-allTown")
                ? Statecraft.Game.Map.Kits.TownModels.Ids.SelectMany(t => new[] { HexModels.TownPrefix + t, HexModels.TownPrefix + t + HexModels.MidSuffix })
                : Enumerable.Empty<string>());

        static void Capture(string dir, int width, int height)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = HexBoardCapture.Stage();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 600f;
            var gm = new Material(Shader.Find("Universal Render Pipeline/Lit")); gm.SetColor("_BaseColor", new Color(0.55f, 0.62f, 0.38f));
            ground.GetComponent<Renderer>().sharedMaterial = gm;
            Directory.CreateDirectory(dir);
            var lines = new List<string>();
            foreach (var id in Ids())
            {
                var name = id.Replace("sandbox:", "").Replace(HexModels.TownPrefix, "town-").Replace(HexModels.MidSuffix, "-mid");
                var before = id.StartsWith(HexModels.TownPrefix, StringComparison.Ordinal) ? null : Resources.Load<GameObject>(HexModels.PathOf(id));
                var after = HexModels.Prefab(id);
                if (before != null) Sheet(cam, before, Path.Combine(dir, $"{name}-before.png"), width, height);
                if (after != null) Sheet(cam, after, Path.Combine(dir, $"{name}-after.png"), width, height);
                lines.Add($"{id}: before {(before != null ? before.name : "-")}, after {(after != null ? after.name : "-")}");
            }
            File.WriteAllLines(Path.Combine(dir, "models.txt"), lines);
            Debug.Log("HEX MODEL CAPTURE " + string.Join(" | ", lines) + " -> " + dir);
        }

        /// <summary>Four shots of the model (from its front, then turned a quarter each time), tiled 2x2 into one image.</summary>
        static void Sheet(Camera cam, GameObject prefab, string path, int width, int height)
        {
            int w = width / 2, h = height / 2;
            var sheet = new Texture2D(width, height, TextureFormat.RGB24, false);
            var holder = new GameObject("model");
            try
            {
                foreach (var part in HexModels.Parts(prefab, Fit.Width))
                {
                    var go = new GameObject(part.Mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(holder.transform, false);
                    var m = Matrix4x4.Scale(Vector3.one * WidthM) * part.Local;
                    go.transform.localPosition = m.GetColumn(3); go.transform.localRotation = m.rotation; go.transform.localScale = m.lossyScale;
                    go.GetComponent<MeshFilter>().sharedMesh = part.Mesh;
                    var mats = new Material[part.Mesh.subMeshCount];
                    for (int i = 0; i < mats.Length; i++) mats[i] = part.Material;
                    go.GetComponent<MeshRenderer>().sharedMaterials = mats;
                }
                for (int k = 0; k < 4; k++)
                {
                    // The camera looks north (HexBoardCapture.Shoot): turning the model shows each side in turn; 0 shows its -z side.
                    holder.transform.rotation = Quaternion.Euler(0f, 90f * k, 0f);
                    var tmp = Path.Combine(Path.GetDirectoryName(path)!, "_tile.png");
                    HexBoardCapture.Shoot(cam, new Vector3(0f, WidthM * 0.3f, 0f), DistanceM, tmp, w, h);
                    var tile = new Texture2D(2, 2); tile.LoadImage(File.ReadAllBytes(tmp)); File.Delete(tmp);
                    sheet.SetPixels((k % 2) * w, (1 - k / 2) * h, w, h, tile.GetPixels());
                    Object.DestroyImmediate(tile);
                }
                sheet.Apply();
                File.WriteAllBytes(path, sheet.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(holder); Object.DestroyImmediate(sheet); }
        }
    }
}
