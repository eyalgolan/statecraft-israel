#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map
{
    /// <summary>The land between the towns (#2878), two layers on one rule for where scrub grows (StatecraftGroundDetail.hlsl).
    /// GroundDetail.shader paints grain, scrub and stones over the terrain, a pass of its own and one draw call
    /// (its header says why). GroundScrub.shader stands low 3D bushes on it: one mesh of a 15 km tile, drawn for the tiles in
    /// view by a few tile instances a frame, not one instance a bush ("the WebGL cost is per instance"), each bush placed on the
    /// ground by the vertex shader. Everything is built at boot, so neither adds a byte to the web download. Neither is drawn
    /// where it could not read: the paint past <see cref="FarM"/>, the bushes past <see cref="ScrubGoneM"/>.</summary>
    [ExecuteAlways]
    public sealed class GroundDetail : MonoBehaviour
    {
        public const string Name = "GroundDetail", MaterialResource = "Map/GroundDetail", ScrubMaterialResource = "Map/GroundScrub",
            Toggle = "grounddetail", ScrubToggle = "groundscrub";
        /// <summary>The queue right after the terrain (Terrain.mat 1998), over its lit colour and under everything else.</summary>
        public const int Queue = (int)RenderQueue.Geometry - 1;
        public const int TextureSize = 1024;
        /// <summary>Design values: the texture's tile on the ground (12 m a texel); the mesh's spacing, fine enough to follow the
        /// hills at these zooms, as this pass needs no more than the terrain's rough height.</summary>
        public const float TileM = 12288f, MeshStepM = 1500f;
        /// <summary>Design values: the detail is full up to a pixel footprint of FadeStartM (m) and gone at FadeEndM, past the
        /// gameplay zoom; the camera distance beyond which the pass is not drawn at all.</summary>
        public const float FadeStartM = 60f, FadeEndM = 130f, FarM = 150000f;

        /// <summary>Design values: the scrub tile's side, its bushes (about 5 a km2 where the density is full), a bush's reach and
        /// height. BushTopM stays under the lip of the smallest unit base (UnitPlinth.LipShare of a compact ring's radius, 72 m),
        /// so a bush never stands through a token that moved on top of it; the camera distance over which the bushes shrink away, past the gameplay zoom.</summary>
        public const float ScrubTileM = 15000f, ScrubCellM = 450f, BushMinM = 70f, BushMaxM = 170f, BushHeightShare = 0.62f, BushTopM = 68f, ScrubFadeM = 70000f, ScrubGoneM = 85000f;
        public const int KeepOutWidth = 1024, KeepOutHeight = 2048, HeightStepX = 4, HeightStepY = 2;

        static readonly int TileId = Shader.PropertyToID("_ScDetailTileM"), FadeId = Shader.PropertyToID("_ScDetailFade"),
            DetailId = Shader.PropertyToID("_ScDetailTex"), ClassId = Shader.PropertyToID("_ScDetailClassTex"),
            KeepOutId = Shader.PropertyToID("_ScDetailKeepOutTex"), LandId = Shader.PropertyToID("_ScDetailLandTex"), CoastId = Shader.PropertyToID("_ScDetailCoastTex"),
            BakeId = Shader.PropertyToID("_ScDetailBakeTex"), HeightId = Shader.PropertyToID("_ScDetailHeightTex"), ScrubId = Shader.PropertyToID("_ScDetailScrub");

        /// <summary>One biome's class at a mesh vertex: scrub on land the land use weighs high (the wet unfarmed hills: its weight
        /// falls with rainfall), scrub where it weighs low (the plains' ground between the fields), the soil grain, the stones.
        /// Design values, by biome, tuned on renders.</summary>
        public readonly struct Class
        {
            public readonly float WetScrub, OpenScrub, Grain, Stones;
            public Class(float wetScrub, float openScrub, float grain, float stones) { WetScrub = wetScrub; OpenScrub = openScrub; Grain = grain; Stones = stones; }
        }

        public static Class ClassOf(string biome) => biome switch
        {
            "hills" => new Class(1f, 0.25f, 1f, 0.7f),
            "mountains" => new Class(0.9f, 0.1f, 1f, 1f),
            "lowland" => new Class(0.45f, 0.45f, 0.9f, 0.3f),
            "coastal_plain" => new Class(0.15f, 0.40f, 0.8f, 0f),
            "valley" => new Class(0.2f, 0.35f, 0.8f, 0.2f),
            "desert" => new Class(0f, 0f, 1f, 0.8f),
            _ => new Class(0f, 0f, 0f, 0f),   // sea, lake
        };

        MeshRenderer? shown;
        Terrain? ground;
        Mesh? scrubMesh; Material? scrubMaterial;
        bool on = true, scrubOn = true;
        readonly List<Matrix4x4> visible = new List<Matrix4x4>();
        Rect mapRect;
        float reliefM, baseY;
        /// <summary>Scrub tiles submitted for the last camera (a test and profiling aid).</summary>
        public int LastTiles { get; private set; }

        /// <summary>Adds both layers to the map. <paramref name="keepOff"/> are the discs (towns, the units' clearings) and
        /// <paramref name="corridors"/> the routes where no scrub grows, painted or standing. With <paramref name="bushes"/> false
        /// (a map built without its kits and trees) the 3D scrub is left out too: as they do, it casts the map's shadows.</summary>
        public static GroundDetail? Build(Transform parent, Terrain terrain, GeoFrame frame, HexFile hexes, float[,] heights,
            IEnumerable<(Vector2 centre, float radius)> keepOff, IEnumerable<(List<Vector2> line, float halfWidth)> corridors,
            Material terrainMaterial, MapRuntimeAssets assets, bool bushes = true)
        {
            var template = Resources.Load<Material>(MaterialResource); var scrubTemplate = Resources.Load<Material>(ScrubMaterialResource);
            if (template == null || scrubTemplate == null) { Debug.LogWarning($"{MaterialResource} or {ScrubMaterialResource} is missing: no ground detail"); return null; }
            var material = assets.Own(new Material(template) { name = Name, renderQueue = Queue });
            var size = terrain.terrainData.size; var o = terrain.transform.position;
            Shader.SetGlobalTexture(DetailId, assets.Own(BuildTexture()));
            var classes = FillClasses(frame, hexes, out int nx, out int nz);
            Shader.SetGlobalTexture(ClassId, assets.Own(BuildClassTexture(classes, nx, nz)));
            Shader.SetGlobalTexture(KeepOutId, assets.Own(BuildKeepOutTexture(frame, keepOff, corridors)));
            Shader.SetGlobalTexture(HeightId, assets.Own(BuildHeightTexture(heights)));
            Shader.SetGlobalTexture(LandId, terrainMaterial.GetTexture("_ScLandTex"));
            Shader.SetGlobalTexture(CoastId, terrainMaterial.GetTexture("_ScCoastTex"));
            Shader.SetGlobalTexture(BakeId, terrainMaterial.GetTexture("_ScBakeTex"));
            Shader.SetGlobalFloat(TileId, TileM);
            Shader.SetGlobalVector(FadeId, new Vector4(FadeStartM, FadeEndM, 0f, 0f));
            Shader.SetGlobalVector(ScrubId, new Vector4(size.y, o.y, ScrubFadeM, ScrubGoneM));
            var go = new GameObject(Name);
            go.transform.SetParent(parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = assets.Own(BuildMesh(terrain, frame, classes, nx, nz));
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var layer = go.AddComponent<GroundDetail>();
            layer.shown = renderer; layer.ground = terrain;
            if (bushes)
            {
                layer.scrubMesh = assets.Own(BuildScrubMesh());
                layer.scrubMaterial = assets.Own(new Material(scrubTemplate) { name = "GroundScrub", enableInstancing = true });
            }
            layer.mapRect = new Rect(o.x, o.z, size.x, size.z); layer.reliefM = size.y; layer.baseY = o.y;
            assets.OnTeardown(VisualToggles.Register(Toggle, set => layer.on = set));
            assets.OnTeardown(VisualToggles.Register(ScrubToggle, set => layer.scrubOn = set));
            return layer;
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Near;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Near;

        /// <summary>Far out the paint has faded: no draw, no fill. Near enough, the scrub's tiles in view go in one instanced
        /// draw; the shadow pass sees casters outside the view too, so the test widens each tile as VegetationLayer's does.</summary>
        void Near(ScriptableRenderContext context, Camera cam)
        {
            if (shown == null || cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            float height = cam.transform.position.y;
            bool terrainOn = ground != null && ground.enabled;
            shown.enabled = on && terrainOn && height < FarM;
            LastTiles = 0;
            // The opt-in hex-tile board (HexTilesLook) stands on this ground with its own props.
            if (!scrubOn || !terrainOn || scrubMesh == null || scrubMaterial == null || height > ScrubGoneM || transform.parent.Find(HexTiles.HexTilesLook.RootName) != null) return;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            visible.Clear();
            int nx = Mathf.CeilToInt(mapRect.width / ScrubTileM), nz = Mathf.CeilToInt(mapRect.height / ScrubTileM);
            var eye = cam.transform.position;
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    var origin = new Vector3(mapRect.x + x * ScrubTileM, baseY, mapRect.y + z * ScrubTileM);
                    var tile = new Bounds(origin + new Vector3(0.5f * ScrubTileM, 0.5f * reliefM, 0.5f * ScrubTileM), new Vector3(ScrubTileM + 3000f, reliefM + 400f, ScrubTileM + 3000f));
                    if (tile.SqrDistance(eye) > ScrubGoneM * ScrubGoneM || !GeometryUtility.TestPlanesAABB(planes, tile)) continue;
                    visible.Add(Matrix4x4.Translate(origin));
                }
            if (visible.Count == 0) return;
            var rp = new RenderParams(scrubMaterial)
            {
                worldBounds = new Bounds(new Vector3(mapRect.center.x, baseY, mapRect.center.y), new Vector3(mapRect.width + 6000f, 2f * reliefM + 800f, mapRect.height + 6000f)),
                camera = cam, shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, layer = gameObject.layer,
            };
            Graphics.RenderMeshInstanced(rp, scrubMesh, 0, visible);
            LastTiles = visible.Count;
        }

        /// <summary>The land as a coarse grid, every <see cref="MeshStepM"/>, at the terrain's height: a pixel's paint lands
        /// within the terrain's rough relief of where it lies, which grain and bushes cannot show. Quads with no land are left
        /// out (by the hexes' biome class).</summary>
        public static Mesh BuildMesh(Terrain terrain, GeoFrame frame, Color32[] classes, int nx, int nz)
        {
            var data = terrain.terrainData; var o = terrain.transform.position;
            float stepX = frame.WidthM / (nx - 1), stepZ = frame.LengthM / (nz - 1);
            var verts = new Vector3[nx * nz];
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                    verts[z * nx + x] = new Vector3(x * stepX + o.x, o.y + data.GetInterpolatedHeight((float)x / (nx - 1), (float)z / (nz - 1)), z * stepZ + o.z);
            var tris = new List<int>((nx - 1) * (nz - 1) * 6);
            bool Land(int i) => classes[i].b > 0;
            for (int z = 0; z + 1 < nz; z++)
                for (int x = 0; x + 1 < nx; x++)
                {
                    int a = z * nx + x, b = a + 1, c = a + nx, d = c + 1;
                    if (!Land(a) && !Land(b) && !Land(c) && !Land(d)) continue;
                    tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
                }
            var mesh = new Mesh { name = Name };
            mesh.vertices = verts; mesh.SetTriangles(tris, 0, true);
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>The blended class of every mesh step (row 0 = south): r, g, b, a as <see cref="Class"/>'s fields.</summary>
        public static Color32[] FillClasses(GeoFrame frame, HexFile hexes, out int nx, out int nz)
        {
            nx = Mathf.CeilToInt(frame.WidthM / MeshStepM) + 1; nz = Mathf.CeilToInt(frame.LengthM / MeshStepM) + 1;
            float stepX = frame.WidthM / (nx - 1), stepZ = frame.LengthM / (nz - 1);
            var names = new List<string>(MapBuilder.BiomeColours.Keys);
            var classes = new Class[names.Count];
            for (int i = 0; i < classes.Length; i++) classes[i] = ClassOf(names[i]);
            var centres = new List<Vector2>(hexes.hexes.Length); var layers = new List<int>(hexes.hexes.Length);
            foreach (var h in hexes.hexes) { var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add(new Vector2(w.x, w.z)); layers.Add(names.IndexOf(h.biome)); }
            var blender = new BiomeBlender(centres, layers, hexes.hex_size_km * 1000f, names.Count);
            var px = new Color32[nx * nz]; var wts = new float[names.Count];
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    blender.Weights(new Vector2(x * stepX, z * stepZ), wts);
                    float wet = 0f, dry = 0f, grain = 0f, stones = 0f;
                    for (int l = 0; l < wts.Length; l++)
                    {
                        wet += wts[l] * classes[l].WetScrub; dry += wts[l] * classes[l].OpenScrub;
                        grain += wts[l] * classes[l].Grain; stones += wts[l] * classes[l].Stones;
                    }
                    px[z * nx + x] = new Color32((byte)(255f * wet), (byte)(255f * dry), (byte)(255f * grain), (byte)(255f * stones));
                }
            return px;
        }

        static Texture2D BuildClassTexture(Color32[] px, int nx, int nz)
        {
            var tex = new Texture2D(nx, nz, TextureFormat.RGBA32, false, true) { name = "GroundDetailClass", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px); tex.Apply(false, true);
            return tex;
        }

        /// <summary>Where no scrub grows, one byte a texel (row 0 = south): the discs, and the routes, each corridor's line drawn
        /// as a chain of its half-width's discs. Only the texels they touch are written.</summary>
        public static byte[] FillKeepOut(GeoFrame frame, IEnumerable<(Vector2 centre, float radius)> keepOff, IEnumerable<(List<Vector2> line, float halfWidth)> corridors)
        {
            int w = KeepOutWidth, h = KeepOutHeight;
            var px = new byte[w * h];
            float tx = frame.WidthM / w, tz = frame.LengthM / h;
            void Disc(Vector2 c, float r)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - r) / tx)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt((c.x + r) / tx));
                int z0 = Mathf.Max(0, Mathf.FloorToInt((c.y - r) / tz)), z1 = Mathf.Min(h - 1, Mathf.CeilToInt((c.y + r) / tz));
                for (int z = z0; z <= z1; z++)
                {
                    float dz = (z + 0.5f) * tz - c.y;
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = (x + 0.5f) * tx - c.x;
                        if (dx * dx + dz * dz <= r * r) px[z * w + x] = 255;
                    }
                }
            }
            foreach (var (c, r) in keepOff) Disc(c, r);
            foreach (var (line, halfWidth) in corridors)
                for (int i = 0; i + 1 < line.Count; i++)
                {
                    Vector2 a = line[i], b = line[i + 1];
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / Mathf.Max(tx, halfWidth)));
                    for (int s = 0; s <= steps; s++) Disc(Vector2.Lerp(a, b, (float)s / steps), halfWidth);
                }
            return px;
        }

        static Texture2D BuildKeepOutTexture(GeoFrame frame, IEnumerable<(Vector2 centre, float radius)> keepOff, IEnumerable<(List<Vector2> line, float halfWidth)> corridors)
        {
            var tex = new Texture2D(KeepOutWidth, KeepOutHeight, TextureFormat.R8, false, true) { name = "GroundDetailKeepOut", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixelData(FillKeepOut(frame, keepOff, corridors), 0); tex.Apply(false, true);
            return tex;
        }

        /// <summary>The terrain's 0..1 heights for the bushes' vertex shader (Unity's [y, x] as MapBuilder gives them, row 0 =
        /// south, every HeightStep sample): half floats, which WebGL 2 filters, where the terrain's own 16-bit texture may not.</summary>
        static Texture2D BuildHeightTexture(float[,] heights)
        {
            int ny = (heights.GetLength(0) - 1) / HeightStepY + 1, nx = (heights.GetLength(1) - 1) / HeightStepX + 1;
            var px = new ushort[nx * ny];
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                    px[y * nx + x] = Mathf.FloatToHalf(heights[y * HeightStepY, x * HeightStepX]);
            var tex = new Texture2D(nx, ny, TextureFormat.RHalf, false, true) { name = "GroundDetailHeight", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixelData(px, 0); tex.Apply(false, true);
            return tex;
        }

        // A bush's dome from the ground up (the first ring sunk, so it sits in a slope): share of its reach and of its height,
        // the normals' up share, and the shading painted in, darker under the crown.
        static readonly float[] TierRadius = { 0.80f, 1f, 0.55f }, TierHeight = { -0.3f, 0.4f, 0.85f }, TierUp = { 0.1f, 0.55f, 1.6f }, TierShade = { 0.7f, 0.9f, 1.05f };

        /// <summary>One scrub tile: bushes in jittered <see cref="ScrubCellM"/> cells, each a low dome of 3 rings of 6 and an apex
        /// around its pivot (uv0, in the tile), its rank in uv1.x (GroundScrub.shader), its colour a dull olive to grey-brown (design
        /// values, near the hill olives' and the land bake's scrub colours). No geography here: the shader decides which grow.</summary>
        public static Mesh BuildScrubMesh()
        {
            int cells = Mathf.RoundToInt(ScrubTileM / ScrubCellM), n = cells * cells;
            const int Ring = 6, Tiers = 3, Verts = Tiers * Ring + 1;
            var pos = new Vector3[n * Verts]; var nrm = new Vector3[n * Verts]; var col = new Color32[n * Verts];
            var pivot = new Vector2[n * Verts]; var rank = new Vector2[n * Verts];
            var tris = new int[n * 3 * Ring * (2 * (Tiers - 1) + 1)];
            int t = 0;
            for (int cz = 0; cz < cells; cz++)
                for (int cx = 0; cx < cells; cx++)
                {
                    int v0 = (cz * cells + cx) * Verts, apex = v0 + Tiers * Ring;
                    var p = new Vector2((cx + 0.05f + 0.9f * Hash(cx, cz, 21)) * ScrubCellM, (cz + 0.05f + 0.9f * Hash(cx, cz, 22)) * ScrubCellM);
                    float size = Hash(cx, cz, 23), r = Mathf.Lerp(BushMinM, BushMaxM, size * size), yaw = Hash(cx, cz, 24) * Mathf.PI * 2f;
                    float top = Mathf.Min(r * BushHeightShare, BushTopM), grow = Hash(cx, cz, 26);
                    Color c = Color32.Lerp(new Color32(62, 62, 40, 255), new Color32(92, 80, 58, 255), Hash(cx, cz, 25));
                    for (int i = 0; i < Ring; i++)
                    {
                        float a = yaw + i * Mathf.PI * 2f / Ring, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                        float lobe = r * (0.75f + 0.5f * Hash(i, cx + cz * cells, 27));
                        for (int tier = 0; tier < Tiers; tier++)
                        {
                            float w = TierRadius[tier] * lobe;
                            int at = v0 + tier * Ring + i;
                            pos[at] = new Vector3(ca * w, TierHeight[tier] * top, sa * w);
                            nrm[at] = new Vector3(ca, TierUp[tier], sa).normalized;
                            col[at] = c * TierShade[tier];
                        }
                        int j = (i + 1) % Ring;
                        for (int tier = 0; tier + 1 < Tiers; tier++)
                        {
                            int a0 = v0 + tier * Ring + i, a1 = v0 + tier * Ring + j, b0 = a0 + Ring, b1 = a1 + Ring;
                            tris[t++] = a0; tris[t++] = b0; tris[t++] = b1;
                            tris[t++] = a0; tris[t++] = b1; tris[t++] = a1;
                        }
                        tris[t++] = v0 + (Tiers - 1) * Ring + i; tris[t++] = apex; tris[t++] = v0 + (Tiers - 1) * Ring + j;
                    }
                    pos[apex] = new Vector3(0f, top, 0f); nrm[apex] = Vector3.up; col[apex] = c * 1.15f;
                    for (int i = 0; i < Verts; i++) { pivot[v0 + i] = p; rank[v0 + i] = new Vector2(grow, 0f); }
                }
            var mesh = new Mesh { name = "GroundScrub" };
            mesh.vertices = pos; mesh.normals = nrm; mesh.colors32 = col; mesh.SetUVs(0, pivot); mesh.SetUVs(1, rank);
            mesh.triangles = tris;
            mesh.bounds = new Bounds(new Vector3(0.5f * ScrubTileM, 0f, 0.5f * ScrubTileM), new Vector3(ScrubTileM, 2000f, ScrubTileM));
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>The detail tile (<see cref="TextureSize"/> square, seamless, seeded, data: GroundDetail.shader says its
        /// channels). r is value noise over 3 octaves and a speckle; g and b are dots, one jittered dot per cell, each with its own peak so
        /// a lower threshold shows more of them, its shadow offset away from the sun; a is sparse small dots.</summary>
        public static Texture2D BuildTexture()
        {
            int n = TextureSize;
            var px = FillTexels();
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = Name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            tex.SetPixelData(px, 0); tex.Apply(true, true);
            return tex;
        }

        /// <summary>A stable hash in 0..1 of a cell (no Unity random stream moves).</summary>
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)x) * 16777619u; h = (h ^ (uint)y) * 16777619u; h = (h ^ (uint)seed) * 16777619u;
                h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        public static byte[] FillTexels()
        {
            int n = TextureSize;
            var px = new byte[n * n * 4];
            FillGrain(px, n);
            // g and b: the bushes, in cells of 16 texels; a clump noise biases their peaks, so they gather as the promo's do.
            const int Cell = 12; int cells = n / Cell;
            for (int cy = 0; cy < cells; cy++)
                for (int cx = 0; cx < cells; cx++)
                {
                    float x = (cx + 0.1f + 0.8f * Hash(cx, cy, 1)) * Cell, y = (cy + 0.1f + 0.8f * Hash(cx, cy, 2)) * Cell;
                    float size = Hash(cx, cy, 3), r = 0.9f + 3.6f * size * size;   // many small bushes, a few big ones
                    float clump = Hash(cx / 6, cy / 6, 4) * 0.6f + Hash(cx / 3, cy / 3, 5) * 0.4f;
                    float peak = Mathf.Clamp01(0.25f + 0.40f * Hash(cx, cy, 6) + 0.45f * clump);
                    int lobes = 2 + (int)(3f * Hash(cx, cy, 11));          // a bush is a few lobes, never one round dot
                    for (int l = 0; l < lobes; l++)
                    {
                        float bx = x + (Hash(l, cx + cy * cells, 12) - 0.5f) * 2.4f * r, by = y + (Hash(l, cx + cy * cells, 13) - 0.5f) * 2.4f * r;
                        float br = r * (0.6f + 0.6f * Hash(l, cx + cy * cells, 14));
                        Stamp(px, n, bx, by, br, peak, 1);
                        Stamp(px, n, bx + 0.9f * br, by - 0.28f * br, br * 1.05f, peak, 2);   // east-south-east: MapLighting.SunAzimuth 285 degrees
                    }
                }
            // a: small pale stones, in cells of 9 texels.
            const int StoneCell = 9; int stoneCells = n / StoneCell;
            for (int cy = 0; cy < stoneCells; cy++)
                for (int cx = 0; cx < stoneCells; cx++)
                {
                    if (Hash(cx, cy, 7) > 0.4f) continue;
                    Stamp(px, n, (cx + Hash(cx, cy, 8)) * StoneCell, (cy + Hash(cx, cy, 9)) * StoneCell, 1.2f + 1.6f * Hash(cx, cy, 10), 1f, 3);
                }
            return px;
        }

        /// <summary>The soil's grain into r: 3 octaves of periodic value noise on a quarter-size tile, spread over the full one
        /// (boot time: the full tile's octaves cost about 140 ms in the editor), plus a fine per-texel speckle.</summary>
        static void FillGrain(byte[] px, int n)
        {
            const int Small = 256;
            var small = new float[Small * Small];
            var periods = new[] { 4, 16, 64 }; var gains = new[] { 0.32f, 0.32f, 0.26f };
            var t = new float[Small]; var cell = new int[Small];
            for (int o = 0; o < periods.Length; o++)
            {
                int p = periods[o]; float k = (float)p / Small;
                var lattice = new float[p * p];
                for (int i = 0; i < lattice.Length; i++) lattice[i] = Hash(i % p, i / p, p);
                for (int i = 0; i < Small; i++) { float f = i * k; cell[i] = (int)f; float u = f - cell[i]; t[i] = u * u * (3f - 2f * u); }
                for (int y = 0; y < Small; y++)
                {
                    int y0 = cell[y] % p * p, y1 = (cell[y] + 1) % p * p; float ty = t[y];
                    for (int x = 0; x < Small; x++)
                    {
                        int x0 = cell[x] % p, x1 = (cell[x] + 1) % p; float tx = t[x];
                        float v0 = lattice[y0 + x0] + (lattice[y0 + x1] - lattice[y0 + x0]) * tx, v1 = lattice[y1 + x0] + (lattice[y1 + x1] - lattice[y1 + x0]) * tx;
                        small[y * Small + x] += (v0 + (v1 - v0) * ty) * gains[o];
                    }
                }
            }
            int step = n / Small; float per = 1f / step;
            uint noise = 2463534242u;
            for (int y = 0; y < n; y++)
            {
                int sy = y / step; float ty = (y % step) * per;
                int row0 = sy * Small, row1 = (sy + 1) % Small * Small;
                for (int x = 0; x < n; x++)
                {
                    int sx = x / step, sx1 = (sx + 1) % Small; float tx = (x % step) * per;
                    float a = small[row0 + sx], b = small[row0 + sx1], c = small[row1 + sx], d = small[row1 + sx1];
                    float v = a + (b - a) * tx; v += (c + (d - c) * tx - v) * ty;
                    noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                    px[(y * n + x) * 4] = (byte)(255f * (v + 0.1f * (noise >> 24) / 255f));
                }
            }
        }

        /// <summary>A soft dot into one channel, wrapping at the tile's edges, keeping the higher value.</summary>
        static void Stamp(byte[] px, int n, float x, float y, float r, float peak, int channel)
        {
            int x0 = Mathf.FloorToInt(x - r), x1 = Mathf.CeilToInt(x + r), y0 = Mathf.FloorToInt(y - r), y1 = Mathf.CeilToInt(y + r);
            for (int v = y0; v <= y1; v++)
                for (int u = x0; u <= x1; u++)
                {
                    float dx = (u - x) / r, dy = (v - y) / r, d = dx * dx + dy * dy;
                    if (d >= 1f) continue;
                    int at = (((v % n + n) % n) * n + (u % n + n) % n) * 4 + channel;
                    byte value = (byte)(255f * peak * (1f - d * d));
                    if (value > px[at]) px[at] = value;
                }
        }
    }
}
