#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles
{
    /// <summary>Per tile: what was built and whether it kept to its budget.</summary>
    public sealed class HexTileReport
    {
        public string Prototype = "", Title = "", Models = ""; public int Q, R, Pieces, StandIns, Triangles, Budget;
        public bool OverBudget => Triangles > Budget;
        public override string ToString() =>
            $"{Prototype} ({Q},{R}) \"{Title}\": {Pieces} pieces ({StandIns} stand-ins), {Triangles} tris{(Budget == int.MaxValue ? "" : $" of {Budget} " + (OverBudget ? "OVER BUDGET" : "ok"))}\n      models: {Models}";
    }

    /// <summary>Builds a board in the open scene under one root: a Unity Terrain shaped and painted from the hexes
    /// (HexBoardSurface, HexGround), a sea plane, every tile's prototype, and the faint tile edges. Everything it makes is
    /// owned by the root (HexBoardAssets) and destroyed with it. Textures come from <paramref name="textures"/> (null = flat
    /// colours), so the core needs no editor API.</summary>
    public static class HexBoardBuilder
    {
        public const string RootName = "HexBoard", MountainPrototype = "mountain_peaks";

        /// <summary>How the board sits in its scene: alone (its own sea, ground everywhere), or on the real map (no sea of
        /// its own, terrain holes outside its land hexes so the real map shows around and under the sea).</summary>
        public sealed class Options { public bool OwnSea = true; public bool HolesOutsideLand; public Transform? Parent; public Vector3 Origin; }

        public static (GameObject root, List<HexTileReport> reports) Build(HexBoard board, Func<string, (Texture2D? albedo, Texture2D? normal)>? textures, int seed = 1948, string? only = null, Options? options = null)
        {
            options ??= new Options();
            var root = new GameObject(RootName);
            // Everything below is built in board space (HexGeometry, origin at the hex grid's north-west corner): the root
            // carries the board into the scene, so the real map only needs Origin = (0, sea level, map length).
            if (options.Parent != null) root.transform.SetParent(options.Parent, false);
            root.transform.position = options.Origin;
            var assets = root.AddComponent<HexBoardAssets>();
            var surface = new HexBoardSurface(board);
            var reports = new List<HexTileReport>();
            var placements = new List<HexPlacement>();
            var paints = new List<(HexPaint paint, int q, int r)>();
            foreach (var h in board.Hexes.Values.OrderBy(h => h.q).ThenBy(h => h.r))
            {
                string id = h.prototype != "" ? h.prototype : HexBoard.TerrainOf(h) == HexTerrain.Mountain ? MountainPrototype : "";
                if (id == "" || (only != null && id != only && id != MountainPrototype)) continue;
                var proto = HexPrototypes.Create(id);
                var ctx = new HexTileContext(h.q, h.r, surface, seed);
                proto.Build(ctx);
                placements.AddRange(ctx.Placements);
                paints.AddRange(ctx.Paints.Select(p => (p, h.q, h.r)));
                reports.Add(new HexTileReport
                {
                    Prototype = id, Title = proto.Title, Q = h.q, R = h.r, Pieces = ctx.Placements.Count, Budget = proto.TriangleBudget,
                    StandIns = ctx.Placements.Count(p => p.StandIn == StandIn.Flat || HexModels.Prefab(HexModels.Resolve(p.Model, p.Position)) == null),
                    Triangles = ctx.Placements.Sum(p => p.StandIn == StandIn.Flat ? 12 : HexModels.Triangles(p.Model, p.StandIn, p.Position)),
                    // Which models drew and which fell back to stand-ins ("~" marks a missing model).
                    Models = string.Join(", ", ctx.Placements.GroupBy(p => p.StandIn == StandIn.Flat ? "flat" : (HexModels.Prefab(HexModels.Resolve(p.Model, p.Position)) == null ? "~" : "") + p.Model)
                        .OrderBy(g => g.Key).Select(g => $"{g.Key} x{g.Count()}")),
                });
            }
            // The environment ring: continuous landscape round the playable hexes (no tiles, no hex lines).
            placements.AddRange(HexEnvironment.Dress(board, surface, seed));
            // Ground second: the tiles' paints (fields, sand, tracks) and the board's roads go into the terrain's splat.
            BuildTerrain(root.transform, board, surface, textures, assets, paints, options.HolesOutsideLand);
            if (options.OwnSea) BuildSea(root.transform, board, assets);
            HexBoardRenderer.Emit(root.transform, placements, assets);
            // Units: their own layer above the tiles, standing at idle on their hex's centre.
            var units = new GameObject("HexBoardUnits").transform; units.SetParent(root.transform, false);
            foreach (var h in board.Hexes.Values.Where(h => h.unit != "").OrderBy(h => h.q).ThenBy(h => h.r))
            {
                if (only != null && h.prototype != only) continue;
                var type = HexUnitTypes.Create(h.unit);
                var unit = type.Create();
                var motion = new HexUnitMotion(unit, new[] { (h.q, h.r) }, surface, units, HexScale.HexM, h.facing);
                motion.Pose(0f, false);
                reports.Add(new HexTileReport
                {
                    Prototype = "unit:" + h.unit, Title = type.Title, Q = h.q, R = h.r, Pieces = unit.Members.Count, Budget = int.MaxValue,
                    StandIns = unit.Members.Count(m => HexModels.Prefab(m.Model) == null),
                    Triangles = unit.Members.Sum(m => HexModels.Triangles(m.Model, m.StandIn, Vector3.zero)),
                    Models = string.Join(", ", unit.Members.GroupBy(m => (HexModels.Prefab(m.Model) == null ? "~" : "") + m.Model).Select(g => $"{g.Key} x{g.Count()}")),
                });
            }
            HexBoardEdges.Build(root.transform, board, surface, assets);
            return (root, reports);
        }

        /// <summary>The board's world bounds: every hex plus one hex of sea around it.</summary>
        public static Rect Bounds(HexBoard board, float sizeM = HexScale.HexM)
        {
            var cs = board.Hexes.Values.Select(h => HexGeometry.Centre(h.q, h.r, sizeM)).ToList();
            float m = sizeM * 2.2f;
            float x0 = cs.Min(c => c.x) - m, x1 = cs.Max(c => c.x) + m, z0 = cs.Min(c => c.z) - m, z1 = cs.Max(c => c.z) + m;
            return new Rect(x0, z0, x1 - x0, z1 - z0);
        }

        const int HeightRes = 1025, AlphaRes = 2048;
        const float MinY = -600f, MaxY = 2400f;

        /// <summary>Road width on the ground, metres (a Civ road reads as a thin track from far away).</summary>
        public const float RoadM = 170f;

        static void BuildTerrain(Transform root, HexBoard board, HexBoardSurface surface, Func<string, (Texture2D?, Texture2D?)>? textures, HexBoardAssets assets,
            List<(HexPaint paint, int q, int r)> paints, bool holesOutsideLand)
        {
            var rect = Bounds(board);
            var data = assets.Own(new TerrainData { heightmapResolution = HeightRes });
            data.size = new Vector3(rect.width, MaxY - MinY, rect.height);
            var heights = new float[HeightRes, HeightRes];
            for (int z = 0; z < HeightRes; z++)
                for (int x = 0; x < HeightRes; x++)
                {
                    var p = new Vector3(rect.x + rect.width * x / (HeightRes - 1f), 0f, rect.y + rect.height * z / (HeightRes - 1f));
                    heights[z, x] = Mathf.InverseLerp(MinY, MaxY, surface.Height(p));
                }
            data.SetHeights(0, 0, heights);
            var ids = HexGround.Ids;
            data.terrainLayers = HexGround.Layers.Select(l =>
            {
                var (albedo, normal) = textures?.Invoke(l.texture) ?? (null, null);
                var layer = assets.Own(new TerrainLayer
                {
                    name = "hex-" + l.id, diffuseTexture = albedo != null ? albedo : assets.Own(Solid(l.fallback)), normalMapTexture = normal,
                    tileSize = new Vector2(l.tileM, l.tileM), normalScale = 0.6f, smoothness = 0f, metallic = 0f,
                    diffuseRemapMin = Vector4.zero, diffuseRemapMax = albedo != null ? new Vector4(l.tint.r, l.tint.g, l.tint.b, 1f) : Vector4.one,
                });
                return layer;
            }).ToArray();
            data.alphamapResolution = AlphaRes;
            var alpha = new float[AlphaRes, AlphaRes, ids.Count];
            var w = new float[ids.Count];
            for (int z = 0; z < AlphaRes; z++)
                for (int x = 0; x < AlphaRes; x++)
                {
                    surface.GroundWeights(new Vector3(rect.x + rect.width * x / (AlphaRes - 1f), 0f, rect.y + rect.height * z / (AlphaRes - 1f)), ids, w);
                    for (int l = 0; l < ids.Count; l++) alpha[z, x, l] = w[l];
                }
            // Paints, clipped to their own hex (a tile owns its ground), then roads, which run across hexes.
            foreach (var (paint, q, r) in paints) Paint(alpha, rect, ids, paint, HexGeometry.Centre(q, r, HexScale.HexM), surface);
            foreach (var line in board.RoadLines())
                for (int i = 0; i + 1 < line.Length; i++)
                    Paint(alpha, rect, ids, new HexPaint("dirt", line[i], line[i + 1], RoadM, 0f, 0f, 0.5f, 1f, true), null, surface);
            data.SetAlphamaps(0, 0, alpha);
            if (holesOutsideLand)
            {
                // Holes everywhere but the board's land (and a sliver of shore), so the real map shows around it.
                int hr = data.holesResolution;
                var solid = new bool[hr, hr];
                for (int z = 0; z < hr; z++)
                    for (int x = 0; x < hr; x++)
                    {
                        var p = new Vector3(rect.x + rect.width * (x + 0.5f) / hr, 0f, rect.y + rect.height * (z + 0.5f) / hr);
                        var (q, r) = HexGeometry.Axial(p, HexScale.HexM);
                        solid[z, x] = board.Hexes.TryGetValue((q, r), out var h) && HexBoard.TerrainOf(h) != HexTerrain.Water && surface.Height(p) > -2f;
                    }
                data.SetHoles(0, 0, solid);
            }
            var go = Terrain.CreateTerrainGameObject(data);
            var t = go.GetComponent<Terrain>();
            // An overlay overlaps the real map; it must not join its grid of adjacent terrain tiles.
            if (holesOutsideLand) t.allowAutoConnect = false;
            go.name = "HexBoardTerrain";
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(rect.x, MinY, rect.y);
            var m = assets.Own(HexMaterials.New(holesOutsideLand ? HexMaterials.TerrainHoles : HexMaterials.Terrain));
            t.materialTemplate = m; t.heightmapPixelError = 2f; t.basemapDistance = 1e7f; t.drawTreesAndFoliage = false;
        }

        /// <summary>Blends one paint into the splat: weight 1 inside, easing to 0 over its soft edge; never on water; when
        /// <paramref name="clipCentre"/> is given, only inside that hex.</summary>
        static void Paint(float[,,] alpha, Rect rect, IReadOnlyList<string> ids, HexPaint p, Vector3? clipCentre, HexBoardSurface surface)
        {
            int layer = -1; for (int i = 0; i < ids.Count; i++) if (ids[i] == p.Layer) layer = i;
            if (layer < 0) return;
            int n = alpha.GetLength(0), layers = alpha.GetLength(2);
            float reach = p.Line ? p.Width : 0.75f * Mathf.Max(p.Width, p.Depth);
            float x0 = Mathf.Min(p.A.x, p.B.x) - reach, x1 = Mathf.Max(p.A.x, p.B.x) + reach, z0 = Mathf.Min(p.A.z, p.B.z) - reach, z1 = Mathf.Max(p.A.z, p.B.z) + reach;
            int i0 = Mathf.Max(0, Mathf.FloorToInt((x0 - rect.x) / rect.width * (n - 1))), i1 = Mathf.Min(n - 1, Mathf.CeilToInt((x1 - rect.x) / rect.width * (n - 1)));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((z0 - rect.y) / rect.height * (n - 1))), j1 = Mathf.Min(n - 1, Mathf.CeilToInt((z1 - rect.y) / rect.height * (n - 1)));
            var inv = Quaternion.Euler(0f, -p.Yaw, 0f);
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    var w = new Vector3(rect.x + rect.width * i / (n - 1f), 0f, rect.y + rect.height * j / (n - 1f));
                    if (clipCentre is Vector3 c && !HexGeometry.InHex(w, c, HexScale.HexM)) continue;
                    if (surface.IsWater(w)) continue;
                    float k;
                    if (p.Line)
                    {
                        float d = HexBoardSurface.DistanceToLine(w, new[] { p.A, p.B });
                        k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(p.Width * 0.5f, p.Width * 0.5f * (1f - p.Soft), d));
                    }
                    else
                    {
                        var l = inv * (w - p.A);
                        float soft = Mathf.Max(1f, p.Soft * Mathf.Min(p.Width, p.Depth));
                        float kx = Mathf.InverseLerp(p.Width * 0.5f, p.Width * 0.5f - soft, Mathf.Abs(l.x));
                        float kz = Mathf.InverseLerp(p.Depth * 0.5f, p.Depth * 0.5f - soft, Mathf.Abs(l.z));
                        k = Mathf.SmoothStep(0f, 1f, Mathf.Min(kx, kz));
                    }
                    k *= p.Strength;
                    if (k <= 0f) continue;
                    for (int l2 = 0; l2 < layers; l2++) alpha[j, i, l2] = alpha[j, i, l2] * (1f - k) + (l2 == layer ? k : 0f);
                }
        }

        static void BuildSea(Transform root, HexBoard board, HexBoardAssets assets)
        {
            var rect = Bounds(board);
            var mesh = assets.Own(new Mesh { name = "hex-sea" });
            mesh.SetVertices(new[] { new Vector3(rect.xMin, 0, rect.yMin), new Vector3(rect.xMax, 0, rect.yMin), new Vector3(rect.xMin, 0, rect.yMax), new Vector3(rect.xMax, 0, rect.yMax) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var m = assets.Own(Transparent(new Color(0.06f, 0.42f, 0.58f, 0.78f), lit: true));   // Civ turquoise
            m.SetFloat("_Smoothness", 0.85f);
            var go = new GameObject("HexBoardSea", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
        }

        public static Material Transparent(Color c, bool lit)
        {
            var m = HexMaterials.New(lit ? HexMaterials.Sea : HexMaterials.Edges);
            m.SetColor("_BaseColor", c);
            return m;
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(4, 4, TextureFormat.RGB24, true) { name = "hex-solid" };
            t.SetPixels(Enumerable.Repeat(c, 16).ToArray()); t.Apply();
            return t;
        }
    }

    /// <summary>Lives on the board root and destroys what the board made (meshes, materials, textures, terrain data).</summary>
    [ExecuteAlways]
    public sealed class HexBoardAssets : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        readonly List<Action> teardown = new List<Action>();
        public T Own<T>(T o) where T : Object { owned.Add(o); return o; }
        /// <summary>Runs when the board is destroyed, last registered first (e.g. restoring the real map's heights).</summary>
        public void OnTeardown(Action undo) => teardown.Add(undo);
        void OnDestroy() => Teardown();

        /// <summary>Restores everything the board changed, now and once (Destroy is deferred to the frame's end, so a
        /// board rebuilt in the same frame must not find the old one's changes still in place).</summary>
        public void Teardown()
        {
            for (int i = teardown.Count - 1; i >= 0; i--) teardown[i]();
            teardown.Clear();
            foreach (var o in owned) if (o != null) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
            owned.Clear();
        }
    }

    /// <summary>Draws placements: static models and stand-ins combined into one renderer per material, skinned models
    /// (figures, animals) instantiated and posed at their idle's first frame.</summary>
    public static class HexBoardRenderer
    {
        public static void Emit(Transform root, IEnumerable<HexPlacement> placements, HexBoardAssets assets)
        {
            var parent = new GameObject("HexBoardPieces").transform; parent.SetParent(root, false);
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var parts = new Dictionary<(string, Fit), HexModels.Part[]>();
            var tints = new Dictionary<Color, Material>();
            var pieces = new Dictionary<(Mesh, Matrix4x4), MeshRenderer>();
            Material Tinted(Color c)
            {
                if (tints.TryGetValue(c, out var m)) return m;
                m = assets.Own(HexMaterials.New(HexMaterials.Lit)); m.name = "hex-" + ColorUtility.ToHtmlStringRGB(c);
                m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.05f);
                return tints[c] = m;
            }
            void Add(Material m, Mesh mesh, int sub, Matrix4x4 world)
            {
                // A player cannot combine meshes imported without Read/Write (the Tripo and kit models): draw those as
                // their own renderers sharing the mesh (the SRP batcher keeps the cost down); combine the rest.
                if (!mesh.isReadable)
                {
                    // One renderer per placed mesh; each of its submeshes gets its own material as the parts arrive.
                    var key = (mesh, world);
                    if (!pieces.TryGetValue(key, out var piece))
                    {
                        var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                        go.transform.SetParent(parent, false);
                        go.transform.localPosition = world.GetColumn(3);
                        go.transform.localRotation = world.rotation;
                        go.transform.localScale = world.lossyScale;
                        go.GetComponent<MeshFilter>().sharedMesh = mesh;
                        piece = go.GetComponent<MeshRenderer>();
                        piece.sharedMaterials = Enumerable.Repeat(m, mesh.subMeshCount).ToArray();
                        pieces[key] = piece;
                    }
                    var mats = piece.sharedMaterials; mats[sub] = m; piece.sharedMaterials = mats;
                    return;
                }
                if (!groups.TryGetValue(m, out var list)) groups[m] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = world });   // board space = parent's local space
            }
            foreach (var p in placements)
            {
                var id = HexModels.Resolve(p.Model, p.Position);
                var prefab = p.StandIn == StandIn.Flat ? null : HexModels.Prefab(id);
                if (prefab != null && HexModels.Skinned(prefab))
                {
                    var go = Object.Instantiate(prefab, parent);
                    var b = HexModels.Bounds(go);
                    float k = HexModels.Normaliser(b.size, p.Fit).x;
                    go.transform.localPosition = p.Position; go.transform.localRotation = p.Matrix.rotation;
                    go.transform.localScale = Vector3.one * k * p.Matrix.lossyScale.x;
                    foreach (var a in go.GetComponentsInChildren<Animator>(true)) HexStandIns.SampleIdle(a);
                    continue;
                }
                if (prefab != null)
                {
                    if (!parts.TryGetValue((id, p.Fit), out var ps)) parts[(id, p.Fit)] = ps = HexModels.Parts(prefab, p.Fit);
                    foreach (var part in ps) Add(part.Material, part.Mesh, part.SubMesh, p.Matrix * part.Local);
                    continue;
                }
                foreach (var (mesh, colour, local) in HexModels.StandInParts(p.StandIn))
                {
                    var c = p.StandIn == StandIn.Flat || (p.Tint != Color.white && colour != "bark") ? p.Tint : HexStandIns.Palette[colour];
                    Add(Tinted(c), mesh, 0, p.Matrix * local);
                }
            }
            foreach (var (m, list) in groups)
            {
                var mesh = assets.Own(new Mesh { name = "hex-" + m.name, indexFormat = IndexFormat.UInt32 });
                mesh.CombineMeshes(list.ToArray(), true, true);
                var go = new GameObject("hex-" + m.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
        }
    }

    /// <summary>Faint tile edges on land, Civ-style: a pale transparent ribbon along every land hex edge, draped on the ground.</summary>
    public static class HexBoardEdges
    {
        const int Segments = 12; const float LiftM = 14f, WidthM = 60f;

        public static void Build(Transform root, HexBoard board, HexBoardSurface surface, HexBoardAssets assets, float sizeM = HexScale.HexM)
        {
            var verts = new List<Vector3>(); var tris = new List<int>();
            foreach (var h in board.Hexes.Values)
            {
                if (HexBoard.TerrainOf(h) == HexTerrain.Water || h.env) continue;   // the environment ring has no hex lines
                var corners = HexGeometry.Corners(HexGeometry.Centre(h.q, h.r, sizeM), sizeM);
                for (int e = 0; e < 6; e++)
                {
                    // Each shared land edge once: draw it from the hex whose neighbour across it sorts later or is not land.
                    var n = HexGeometry.Neighbours[(7 - e) % 6];   // corner e..e+1 faces this neighbour (north-east first)
                    bool other = board.Hexes.TryGetValue((h.q + n.q, h.r + n.r), out var nb) && HexBoard.TerrainOf(nb) != HexTerrain.Water && !nb.env;
                    if (other && (h.q, h.r).CompareTo((nb!.q, nb.r)) > 0) continue;
                    Vector3 a = corners[e], b = corners[(e + 1) % 6];
                    var side = Vector3.Cross(Vector3.up, (b - a).normalized) * (WidthM * 0.5f);
                    for (int s = 0; s < Segments; s++)
                    {
                        Vector3 p0 = Vector3.Lerp(a, b, s / (float)Segments), p1 = Vector3.Lerp(a, b, (s + 1) / (float)Segments);
                        if (surface.IsWater(p0) && surface.IsWater(p1)) continue;
                        p0.y = Mathf.Max(surface.Height(p0), 0f) + LiftM; p1.y = Mathf.Max(surface.Height(p1), 0f) + LiftM;
                        int i = verts.Count;
                        verts.Add(p0 - side); verts.Add(p0 + side); verts.Add(p1 - side); verts.Add(p1 + side);
                        tris.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
                    }
                }
            }
            var mesh = assets.Own(new Mesh { name = "hex-edges", indexFormat = IndexFormat.UInt32 });
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            var m = assets.Own(HexBoardBuilder.Transparent(new Color(1f, 0.97f, 0.88f, 0.28f), lit: false));
            m.SetFloat("_Cull", (float)CullMode.Off);
            var go = new GameObject("HexBoardEdges", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
