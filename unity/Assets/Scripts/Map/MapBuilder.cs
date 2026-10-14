using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    [Serializable] public sealed class HexJson { public int q, r; public double lon, lat; public float elev_m; public bool land; public string biome; }
    [Serializable] public sealed class HexBounds { public double lon0, lon1, lat0, lat1; }
    [Serializable] public sealed class HexFile { public HexBounds bounds; public float hex_size_km; public string layout; public HexJson[] hexes; }

    public static class MapBuilder
    {
        public const float VerticalExaggeration = 3f; // diorama look; real relief would read flat at 400 km scale
        public const int BiomeTextureResolution = 1024;   // ~158 m (E-W) x ~421 m (N-S) per texel
        /// <summary>Terrain.basemapDistance: farther than the camera ever is, so the terrain is always drawn by the Statecraft
        /// graph, never by its low-resolution baked basemap (Unity's default switches at 1 km).</summary>
        public const float NoBasemapDistanceM = 1e7f;
        public const float BiomeLayerTileSizeM = 1000f; // layers are flat colours, so the tiling only needs to be finite
        public const int OverlaySegmentsPerEdge = 10;  // ~500 m sub-segments on a 5 km hex edge, each draped on the terrain
        // ~31 m steps, finer than the heightmap spacing on both axes: 78.9 m E-W x 210.6 m N-S
        // (heightmap.json: 2049 samples, so 2048 intervals, over width_km 161.53 x length_km 431.22).
        public const int OverlaySamplesPerSegment = 16;
        /// <summary>Terrain LOD screen-space error in pixels. Unity's default (5) gives distant cameras (the top-down
        /// capture, the 90 km flythrough) a coarse surface that rises above the draped hex outlines in valleys (K2).</summary>
        public const float TerrainPixelError = 1f;


        /// <summary>Biome colours of the terrain layers (the minimap uses the same ones).</summary>
        public static IReadOnlyDictionary<string, Color> BiomeColours => Biome;

        static readonly Dictionary<string, Color> Biome = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.16f, 0.42f, 0.55f), ["lake"] = new Color(0.20f, 0.50f, 0.62f),
            ["coastal_plain"] = new Color(0.55f, 0.62f, 0.36f), ["hills"] = new Color(0.62f, 0.58f, 0.40f),
            ["mountains"] = new Color(0.58f, 0.52f, 0.42f), ["valley"] = new Color(0.50f, 0.56f, 0.38f),
            ["lowland"] = new Color(0.53f, 0.59f, 0.37f), ["desert"] = new Color(0.80f, 0.68f, 0.48f),
        };

        /// <summary>Builds the terrain from the heightmap in <paramref name="mapDir"/> and the
        /// hexes in <paramref name="hexesJson"/>, both read with System.IO. The material is an editor-made asset (Assets/Materials/Terrain.mat) passed in by
        /// the caller: Shader.Find returns null in players (M0 correction 1).
        /// Runtime-created objects are owned by a <see cref="MapRuntimeAssets"/> on the returned GameObject and destroyed with it.</summary>
        public static GameObject Build(string mapDir, string hexesJson, Material terrainMaterial)
        {
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            var (meta, heights) = HeightmapLoader.Load(mapDir);
            return Build(meta, heights, LoadHexes(hexesJson), terrainMaterial);
        }

        /// <summary>Builds the map from already-read inputs (HeightmapLoader.Load and LoadHexes over text and bytes), so a
        /// WebGL build needs no System.IO. Throws InvalidDataException, before creating any object, when the hexes' bounds
        /// differ from the heightmap's by more than <see cref="BoundsTolerance"/>.</summary>
        public static GameObject Build(HeightmapMeta meta, float[,] heights, HexFile hexFile, Material terrainMaterial) =>
            Build(meta, heights, hexFile, terrainMaterial, null);

        /// <summary>As Build, with <paramref name="paintGround"/> given the baked biome colour texels (row 0 = south,
        /// <see cref="BiomeTextureResolution"/> square over the map) before they are uploaded, so the settlement kits can tint
        /// the ground of their footprints (art spike) at no cost per frame.</summary>
        public static GameObject Build(HeightmapMeta meta, float[,] heights, HexFile hexFile, Material terrainMaterial,
            Action<Color32[], int, GeoFrame> paintGround) => Build(meta, heights, hexFile, terrainMaterial, paintGround, false);

        /// <summary>As Build; with <paramref name="bakedBiome"/> the biome colours are read from their bake
        /// (BiomeColourFile, issue #321: boot time) when it was made from these inputs, else blended here.</summary>
        public static GameObject Build(HeightmapMeta meta, float[,] heights, HexFile hexFile, Material terrainMaterial,
            Action<Color32[], int, GeoFrame> paintGround, bool bakedBiome)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (heights == null) throw new ArgumentNullException(nameof(heights));
            if (hexFile == null) throw new ArgumentNullException(nameof(hexFile));
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            CheckSameArea(meta, hexFile.bounds);
            float widthM = meta.width_km * 1000f, lengthM = meta.length_km * 1000f;
            float reliefM = (meta.elev_max_m - meta.elev_min_m) * VerticalExaggeration;
            var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, widthM, lengthM);

            var data = new TerrainData { heightmapResolution = meta.width };
            data.size = new Vector3(widthM, reliefM, lengthM);
            data.SetHeights(0, 0, heights);
            Boot.DiagLog.BootPhase("terrain heights set");

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            var assets = terrainGo.AddComponent<MapRuntimeAssets>();
            assets.Own(data);
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMaterial;
            terrain.heightmapPixelError = TerrainPixelError;
            terrain.basemapDistance = NoBasemapDistanceM;
            data.terrainLayers = new[] { NeutralLayer(assets) };   // one layer, one pass: the graph paints from the biome texture
            data.alphamapResolution = 16;
            Boot.DiagLog.BootPhase("terrain object created");   // issue #321: splits the terrain object from the biome colours
            MapShaderGlobals.Apply(terrain, meta, hexFile.hex_size_km * 1000f, BakeBiomeTexture(frame, hexFile, assets, paintGround, bakedBiome), assets);
            Boot.DiagLog.BootPhase("biome texture baked");
            MapBorders.Apply(hexFile, assets);   // region borders: none drawn until a layer is given (MapBorders.Use)
            return terrainGo;
        }

        /// <summary>Reads and validates hexes.json (non-empty, hex_size_km > 0, pointy layout, known biomes); problems throw
        /// InvalidDataException naming the file. Build calls this before creating any object.</summary>
        public static HexFile LoadHexes(string path) => LoadHexes(File.ReadAllText(path), path);

        /// <summary>Parses already-read hexes.json text; errors name <paramref name="path"/>, as the path overload does.</summary>
        public static HexFile LoadHexes(string text, string path)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException($"{path}: empty file");
            HexFile f;
            try { f = JsonUtility.FromJson<HexFile>(text); }
            catch (ArgumentException e) { throw new InvalidDataException($"{path}: malformed JSON ({e.Message})", e); }
            if (f == null) throw new InvalidDataException($"{path}: empty file");
            if (f.hexes == null || f.hexes.Length == 0) throw new InvalidDataException($"{path}: no hexes");
            if (!(f.hex_size_km > 0)) throw new InvalidDataException($"{path}: hex_size_km {f.hex_size_km} must be > 0");
            if (f.layout != "pointy") throw new InvalidDataException($"{path}: layout '{f.layout}' unsupported (expected 'pointy')");
            foreach (var h in f.hexes)
                if (h == null || h.biome == null || !Biome.ContainsKey(h.biome))
                    throw new InvalidDataException($"{path}: unknown biome '{h?.biome}' at q={h?.q} r={h?.r}");
            return f;
        }

        public const double BoundsTolerance = 1e-9; // degrees

        /// <summary>hexes.json and the heightmap must describe the same lon/lat rectangle, or hexes and settlements land
        /// in the wrong place on the terrain.</summary>
        static void CheckSameArea(HeightmapMeta meta, HexBounds b)
        {
            if (b == null || Math.Abs(b.lon0 - meta.lon0) > BoundsTolerance || Math.Abs(b.lon1 - meta.lon1) > BoundsTolerance
                || Math.Abs(b.lat0 - meta.lat0) > BoundsTolerance || Math.Abs(b.lat1 - meta.lat1) > BoundsTolerance)
                throw new InvalidDataException(FormattableString.Invariant(
                    $"hexes.json bounds (lon {b?.lon0}..{b?.lon1}, lat {b?.lat0}..{b?.lat1}) differ from heightmap.json (lon {meta.lon0}..{meta.lon1}, lat {meta.lat0}..{meta.lat1}) by more than {BoundsTolerance}"));
        }

        /// <summary>The single terrain layer the Terrain Lit target needs; the graph ignores its colour.</summary>
        static TerrainLayer NeutralLayer(MapRuntimeAssets assets)
        {
            var tex = assets.Own(new Texture2D(1, 1, TextureFormat.RGB24, false) { name = "TerrainLayerNeutral" }); // no alpha: no smoothness
            tex.SetPixel(0, 0, Color.grey); tex.Apply();
            return assets.Own(new TerrainLayer
            {
                name = "neutral", diffuseTexture = tex, tileSize = new Vector2(BiomeLayerTileSizeM, BiomeLayerTileSizeM),
                smoothness = 0f, metallic = 0f,
            });
        }

        /// <summary>The ground colour of every point, blended from the biomes of nearby hex centres (BiomeBlender) in the
        /// MapPalette.Ground colours, baked once into an sRGB texture over the whole map (texel row 0 = south) that the
        /// terrain shader samples as _ScBiomeTex. Owned by the map. With <paramref name="baked"/> the colours come from
        /// BiomeColourFile when it matches these inputs.</summary>
        static Texture2D BakeBiomeTexture(GeoFrame frame, HexFile hexes, MapRuntimeAssets assets, Action<Color32[], int, GeoFrame> paint, bool baked)
        {
            int res = BiomeTextureResolution;
            var tex = assets.Own(new Texture2D(res, res, TextureFormat.RGBA32, true, false)
                { name = "BiomeColour", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear });
            var px = baked ? BiomeColourFile.Load(frame, hexes) : null;
            px ??= BiomePixels(frame, hexes);
            paint?.Invoke(px, res, frame);
            tex.SetPixels32(px); tex.Apply(true, true);   // no CPU copy: nothing reads the pixels back (about 5.6 MB on the heap)
            return tex;
        }

        /// <summary>The biome colour texels (<see cref="BiomeTextureResolution"/> square, row 0 = south) before the ground
        /// tint: every texel centre's BiomeBlender weights over the MapPalette.Ground colours. Editor BiomeColourBake bakes
        /// them (BiomeColourFile).</summary>
        public static Color32[] BiomePixels(GeoFrame frame, HexFile hexes)
        {
            var names = new List<string>(Biome.Keys);
            var colours = BiomeLayerColours();
            var centres = new List<Vector2>(hexes.hexes.Length); var layerOf = new List<int>(hexes.hexes.Length);
            foreach (var h in hexes.hexes)
            {
                var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add(new Vector2(w.x, w.z)); layerOf.Add(names.IndexOf(h.biome));
            }
            var blender = new BiomeBlender(centres, layerOf, hexes.hex_size_km * 1000f, names.Count);
            int res = BiomeTextureResolution;
            var px = new Color32[res * res]; var wts = new float[names.Count];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    blender.Weights(new Vector2((x + 0.5f) / res * frame.WidthM, (y + 0.5f) / res * frame.LengthM), wts);
                    var c = Color.clear;
                    for (int l = 0; l < wts.Length; l++) c += colours[l] * wts[l];
                    c.a = 1f;
                    px[y * res + x] = c;
                }
            return px;
        }

        /// <summary>The ground colour of each biome layer, in the order of <see cref="BiomeColours"/>' keys (the blend's
        /// layer indices).</summary>
        public static Color[] BiomeLayerColours()
        {
            var names = new List<string>(Biome.Keys);
            var colours = new Color[names.Count];
            for (int i = 0; i < names.Count; i++) colours[i] = MapPalette.Ground[names[i]];
            return colours;
        }

        /// <summary>A heightmap sample this close to 0 m is sea (tools/dem_to_heightmap.py writes open sea as exactly 0 m).</summary>
        public const float SeaToleranceM = 0.5f;

        /// <summary>The diorama's frame (issue #36, promo 02): the map is a relief board in a wooden picture frame on a dark
        /// table. Horizontal run (m) of the bevel from the terrain's edge to the frame's top: where the edge stands higher than
        /// the frame (the Jordanian plateau, the Lebanon hills) it is the board's cut earth falling to the frame; where it is
        /// lower (the sea, the Sinai coast) it is the frame's inner lip rising from it.</summary>
        public const float FrameBevelM = 1500f;
        /// <summary>Width (m) of the frame's top, from the bevel's outer end to the frame's outer face.</summary>
        public const float FrameWidthM = 6000f;
        /// <summary>Height (world m, exaggerated like the relief) of the frame's top above sea level, and of its rounded
        /// crown above that.</summary>
        public const float FrameRiseM = 1500f, FrameCrownM = 300f;
        /// <summary>How far (m) the table runs past the frame: beyond anything the camera sees at its farthest zoom.</summary>
        public const float TableReachM = 600000f;
        /// <summary>sRGB albedos: the frame's walnut, the table's darker wood, and the board's cut earth.</summary>
        public static readonly Color FrameWood = new Color(0.44f, 0.26f, 0.15f), TableWood = new Color(0.21f, 0.14f, 0.10f),
            CutEarth = new Color(0.64f, 0.49f, 0.33f);
        /// <summary>Which surface a frame vertex belongs to, carried in its colour's alpha (MapFrame.shader picks the grain).</summary>
        public static class FrameKind { public const float Earth = 0f, Wood = 0.5f, Table = 1f; }

        /// <summary>The frame round the map (issue #36; it replaces the border skirt, whose flat land slab and sea ran 600 km
        /// past the edge). Three parts, all outside the terrain's footprint and all at or below the terrain's highest point
        /// and at or above its lowest (world y 0), so the camera's fit (CameraController.ViewFootprint, issue #174) keeps
        /// every one of them off the screen wherever the view fits the map; they show only at the overview, where the view
        /// is wider than the map:
        /// - the bevel: one spoke per terrain vertex along each edge, from the edge's own height (ring 0 meets the terrain
        ///   exactly) to the frame's top FrameBevelM out (cut earth above the frame's top, the frame's inner lip below it);
        /// - the frame: a mitred walnut moulding FrameWidthM wide with a rounded crown, and its outer face down to the table;
        /// - the table: dark wood at world y 0 out to TableReachM (the shader darkens it next to the frame and away from it).
        /// One submesh, one material (MapFrame.mat): vertex colour = sRGB albedo, alpha = FrameKind, uv0 = (along the side,
        /// out from the frame's outer edge) in metres. The table and frame faces carry their own vertices, so their creases stay
        /// sharp; the bevel is smooth along each edge.</summary>
        public static Mesh BuildFrame(TerrainData data, float elevMinM, MapRuntimeAssets assets)
        {
            var size = data.size;
            float seaY = (0f - elevMinM) * VerticalExaggeration, topY = seaY + FrameRiseM, tableY = 0f;
            int samples = data.heightmapResolution - 1;
            var verts = new List<Vector3>(); var colours = new List<Color>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            float Height(Vector2 p) => data.GetInterpolatedHeight(Mathf.Clamp01(p.x / size.x), Mathf.Clamp01(p.y / size.z));
            Color Of(Color albedo, float kind) => new Color(albedo.r, albedo.g, albedo.b, kind);
            // The four edges, walked along: (start, end, outward). Each corner's mitre runs along the sum of its two normals.
            var edges = new (Vector2 a, Vector2 b, Vector2 n)[]
            {
                (new Vector2(0, 0), new Vector2(size.x, 0), Vector2.down),                 // south
                (new Vector2(size.x, 0), new Vector2(size.x, size.z), Vector2.right),       // east
                (new Vector2(size.x, size.z), new Vector2(0, size.z), Vector2.up),          // north
                (new Vector2(0, size.z), new Vector2(0, 0), Vector2.left),                  // west
            };
            // A quad (a, b, c, d in order round it), wound to face up, or outward where it is vertical.
            void Quad(int a, int b, int c, int d, Vector2 outward)
            {
                var n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                bool flip = Mathf.Abs(n.y) > 1e-3f * n.magnitude ? n.y < 0 : n.x * outward.x + n.z * outward.y < 0;
                if (flip) tris.AddRange(new[] { a, c, b, a, d, c });
                else tris.AddRange(new[] { a, b, c, a, c, d });
            }
            int Add(Vector2 p, float y, Color colour, Vector2 uv) { verts.Add(new Vector3(p.x, y, p.y)); colours.Add(colour); uvs.Add(uv); return verts.Count - 1; }
            // The frame's profile past the bevel: (offset from the map's edge, height). The crown sits a little nearer the map,
            // as on a picture frame's moulding; the last ring drops the outer face to the table.
            var profile = new (float o, float y)[]
            {
                (FrameBevelM, topY), (FrameBevelM + 0.3f * FrameWidthM, topY + FrameCrownM),
                (FrameBevelM + 0.7f * FrameWidthM, topY + FrameCrownM), (FrameBevelM + FrameWidthM, topY), (FrameBevelM + FrameWidthM, tableY),
            };
            float outer = FrameBevelM + FrameWidthM;
            for (int e = 0; e < edges.Length; e++)
            {
                var (a, b, n) = edges[e];
                Vector2 ma = n + edges[(e + edges.Length - 1) % edges.Length].n, mb = n + edges[(e + 1) % edges.Length].n;
                var dir = (b - a).normalized;
                float Along(Vector2 p) => Vector2.Dot(p - a, dir);
                // The bevel: inner vertex on the terrain's edge, outer on the frame's top inner edge (mitred at the corners).
                int prev = -1;
                for (int i = 0; i <= samples; i++)
                {
                    float t = (float)i / samples;
                    Vector2 p = Vector2.Lerp(a, b, t), q = Vector2.Lerp(a + ma * FrameBevelM, b + mb * FrameBevelM, t);
                    float h = Height(p);
                    var colour = h > topY ? Of(CutEarth, FrameKind.Earth) : Of(FrameWood, FrameKind.Wood);
                    int inner = Add(p, h, colour, new Vector2(Along(p), -outer));
                    Add(q, topY, colour, new Vector2(Along(q), FrameBevelM - outer));
                    if (prev >= 0) Quad(prev, inner, inner + 1, prev + 1, n);
                    prev = inner;
                }
                // The frame's faces, each with its own four vertices (sharp creases).
                for (int k = 0; k + 1 < profile.Length; k++)
                {
                    var (o0, y0) = profile[k]; var (o1, y1) = profile[k + 1];
                    var colour = Of(FrameWood, FrameKind.Wood);
                    Vector2 a0 = a + ma * o0, b0 = b + mb * o0, a1 = a + ma * o1, b1 = b + mb * o1;
                    int v0 = Add(a0, y0, colour, new Vector2(Along(a0), o0 - outer)), v1 = Add(b0, y0, colour, new Vector2(Along(b0), o0 - outer));
                    int v2 = Add(b1, y1, colour, new Vector2(Along(b1), o1 - outer)), v3 = Add(a1, y1, colour, new Vector2(Along(a1), o1 - outer));
                    Quad(v0, v1, v2, v3, n);
                }
                // The table, out to the reach (uv.y = metres out from the frame).
                {
                    var colour = Of(TableWood, FrameKind.Table);
                    float far = outer + TableReachM;
                    Vector2 a0 = a + ma * outer, b0 = b + mb * outer, a1 = a + ma * far, b1 = b + mb * far;
                    int v0 = Add(a0, tableY, colour, new Vector2(Along(a0), 0f)), v1 = Add(b0, tableY, colour, new Vector2(Along(b0), 0f));
                    int v2 = Add(b1, tableY, colour, new Vector2(Along(b1), TableReachM)), v3 = Add(a1, tableY, colour, new Vector2(Along(a1), TableReachM));
                    Quad(v0, v1, v2, v3, n);
                }
            }
            // About 16.5k vertices at the 2049 heightmap: 16-bit indices, unless a finer heightmap needs more.
            var mesh = assets.Own(new Mesh { name = "MapFrame", indexFormat = verts.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 });
            mesh.SetVertices(verts); mesh.SetColors(colours); mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Lakes: flat water (a 3x3 height range within LakeFlatM) below LakeBelowM, tools/terrain_bake.py's rule.</summary>
        public const float LakeBelowM = -150f, LakeFlatM = 0.25f;
        /// <summary>The water mesh reaches this many heightmap samples past the last wet one: the shore's coverage ramp (about
        /// 70 m inland) and the coast texture's own resolution stay inside it.</summary>
        public const int WaterMeshReach = 2;
        /// <summary>The largest flat block the water mesh draws as one fan (heightmap cells a side).</summary>
        public const int WaterMeshBlock = 32;

        /// <summary>The surface the water inside the map is drawn on (issue #195; MapScene.TerrainWaterName): the terrain's own
        /// heightmap surface wherever the water's coverage can be above 0, so Sea.shader's terrain-water mode can add the water
        /// on top of the terrain instead of the terrain's own shader carrying it. Wet samples are the sea (within SeaToleranceM
        /// of 0 m) and flat rift lakes (LakeBelowM, LakeFlatM); every cell within WaterMeshReach samples of one is drawn at
        /// the heightmap's full resolution, except square blocks whose samples all share one height (the open sea, the lakes'
        /// flat middles), drawn as fans from the centre to every sample on their edge, so no edge has a T-junction.
        /// <paramref name="heights"/> are the ones the terrain was given (Unity's [y, x], 0..1): the terrain stores them at
        /// 16 bits, a few centimetres off, well inside the water's depth pull (Sea.shader).</summary>
        public static Mesh BuildWaterMesh(TerrainData data, float[,] heights, float elevMinM, MapRuntimeAssets assets)
        {
            var (grid, indices) = WaterMeshGrid(data.heightmapResolution, data.size, heights, elevMinM);
            return assets.Own(WaterMesh(data.heightmapResolution, data.size, heights, grid, indices));
        }

        /// <summary>The terrain's size (m: width, relief, length) for a heightmap, as Build gives it.</summary>
        public static Vector3 TerrainSize(HeightmapMeta meta) =>
            new Vector3(meta.width_km * 1000f, (meta.elev_max_m - meta.elev_min_m) * VerticalExaggeration, meta.length_km * 1000f);

        /// <summary>BuildWaterMesh's geometry as heightmap samples: each vertex is sample (x, y) packed as x | y << 16, and
        /// the triangles index them (three a triangle, facing up). Baked into terrain_water.bytes (WaterMeshBytes) so a
        /// player only places the vertices (issue #195: boot time). The bake has no version: a change to this method (or to
        /// TerrainSize, WaterMeshBytes) needs a re-bake (TerrainWaterBake.Bake) and a re-pin of PINNED_CODE in
        /// tools/tests/test_terrain_water_bake.py, which fails until then (issue #535).</summary>
        public static (int[] grid, int[] indices) WaterMeshGrid(int res, Vector3 size, float[,] heights, float elevMinM)
        {
            int cells = res - 1;
            // Thresholds on the 0..1 heights (one sample is h * size.y / VerticalExaggeration + elevMinM metres): the scan
            // below runs over every sample (4.2 million), so it stays a compare (boot time, issue #195).
            float perMetre = VerticalExaggeration / size.y, zero = -elevMinM * perMetre;
            float seaLo = zero - SeaToleranceM * perMetre, seaHi = zero + SeaToleranceM * perMetre;
            float lakeBelow = (LakeBelowM - elevMinM) * perMetre, lakeFlat = LakeFlatM * perMetre;
            // A sample is wet when it is sea, or the flat floor of a rift lake (its 3x3 neighbourhood within LakeFlatM).
            bool Wet(int x, int y)
            {
                float h = heights[y, x];
                if (h > seaLo && h < seaHi) return true;
                if (h >= lakeBelow) return false;
                float lo = h, hi = h;
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        float n = heights[Mathf.Clamp(y + oy, 0, cells), Mathf.Clamp(x + ox, 0, cells)];
                        lo = Mathf.Min(lo, n); hi = Mathf.Max(hi, n);
                    }
                return hi - lo <= lakeFlat;
            }
            // One pass over every sample, with no per-sample array (on WebGL a 4 MB allocation at boot costs more than the
            // scan): which WaterMeshBlock-square blocks hold a wet sample. Only blocks with a wet sample in or beside them
            // are looked at again (a cell is drawn when a wet sample lies within WaterMeshReach of one of its corners, and
            // the reach is under a block), so the land away from the water costs a compare or two a sample (boot time,
            // issue #195).
            const int B = WaterMeshBlock, R = WaterMeshReach;
            int blocks = (cells + B - 1) / B;
            var blockWet = new bool[blocks * blocks];
            // A row at a time (WebGL: the browser optimises a function after its first calls, not inside one long loop).
            void ScanRow(int y)
            {
                int by = Mathf.Min(y / B, blocks - 1) * blocks;
                for (int x = 0; x < res; x++)
                {
                    float h = heights[y, x];
                    if ((h > seaLo && h < seaHi) || (h < lakeBelow && Wet(x, y))) blockWet[by + Mathf.Min(x / B, blocks - 1)] = true;
                }
            }
            for (int y = 0; y < res; y++) ScanRow(y);
            bool Relevant(int bx, int by)
            {
                for (int j = Mathf.Max(0, by - 1); j <= Mathf.Min(blocks - 1, by + 1); j++)
                    for (int i = Mathf.Max(0, bx - 1); i <= Mathf.Min(blocks - 1, bx + 1); i++)
                        if (blockWet[j * blocks + i]) return true;
                return false;
            }

            var verts = new List<int>(1 << 17); var tris = new List<int>(3 << 18);
            // Each sample's vertex is made once: samples on the blocks' grid lines are indexed globally, a block's inner
            // samples in a block-local table (no dictionary: boot time).
            var onColumn = new int[(blocks + 1) * res]; var onRow = new int[(blocks + 1) * res];
            var inner = new int[(B - 1) * (B - 1)];
            for (int i = 0; i < onColumn.Length; i++) { onColumn[i] = -1; onRow[i] = -1; }
            int bx0 = 0, by0 = 0;
            int Vertex(int x, int y)
            {
                int[] table; int at;
                if (x % B == 0) { table = onColumn; at = x / B * res + y; }
                else if (y % B == 0) { table = onRow; at = y / B * res + x; }
                else { table = inner; at = (y - by0 - 1) * (B - 1) + (x - bx0 - 1); }
                if (table[at] < 0) { table[at] = verts.Count; verts.Add(x | y << 16); }
                return table[at];
            }
            // Triangles face up: every cell and fan below is listed anticlockwise seen from above (x east, z north), which
            // Unity's clockwise front faces take in the reverse order.
            void Tri(int i, int j, int l) { tris.Add(i); tris.Add(l); tris.Add(j); }
            // Whether block (x0, y0, s) is all wet and all its samples share one height (bitwise: the sea's 0 m, a lake's
            // quantised level).
            bool Flat(int x0, int y0, int s)
            {
                float h = heights[y0, x0];
                for (int y = y0; y <= y0 + s; y++)
                    for (int x = x0; x <= x0 + s; x++)
                    {
                        float v = heights[y, x];
                        if (v != h || !((v > seaLo && v < seaHi) || (v < lakeBelow && Wet(x, y)))) return false;
                    }
                return true;
            }
            // Within the top-level block being drawn: near[] says, per sample of the block (B + 1 a side), whether a wet
            // sample lies within R: the wetness of the block plus R around it, then a separable dilation (counts of wet
            // samples in a sliding window of 2 R + 1, along x, then along y).
            int side = B + 1, span = side + 2 * R;
            var near = new bool[side * side]; var localWet = new byte[span * span]; var rowNear = new byte[span * side];
            int nx0 = 0, ny0 = 0;
            void Dilate(int x0, int y0)
            {
                nx0 = x0; ny0 = y0;
                for (int j = 0; j < span; j++)
                    for (int i = 0; i < span; i++)
                    {
                        int x = x0 - R + i, y = y0 - R + j;
                        bool w = false;
                        if (x >= 0 && y >= 0 && x < res && y < res)
                        {
                            float h = heights[y, x];                           // the sea test inline: a call costs (boot time)
                            w = (h > seaLo && h < seaHi) || (h < lakeBelow && Wet(x, y));
                        }
                        localWet[j * span + i] = (byte)(w ? 1 : 0);
                    }
                for (int j = 0; j < span; j++)                              // along x: sample i of the block is at i + R
                {
                    int o = j * span, count = 0;
                    for (int k = 0; k < 2 * R; k++) count += localWet[o + k];
                    for (int i = 0; i < side; i++)
                    {
                        count += localWet[o + i + 2 * R];
                        rowNear[j * side + i] = (byte)(count > 0 ? 1 : 0);
                        count -= localWet[o + i];
                    }
                }
                for (int i = 0; i < side; i++)                              // then along y
                {
                    int count = 0;
                    for (int k = 0; k < 2 * R; k++) count += rowNear[k * side + i];
                    for (int j = 0; j < side; j++)
                    {
                        count += rowNear[(j + 2 * R) * side + i];
                        near[j * side + i] = count > 0;
                        count -= rowNear[j * side + i];
                    }
                }
            }
            bool Near(int x, int y) => near[(y - ny0) * side + (x - nx0)];
            bool Drawn(int cx, int cy) => Near(cx, cy) || Near(cx + 1, cy) || Near(cx, cy + 1) || Near(cx + 1, cy + 1);
            bool AnyDrawn(int x0, int y0, int s)
            {
                for (int y = y0; y < y0 + s; y++)
                    for (int x = x0; x < x0 + s; x++)
                        if (Drawn(x, y)) return true;
                return false;
            }
            void Fan(int x0, int y0, int s)                             // from the centre to every sample on the edge: 4 s triangles
            {
                int c = verts.Count;
                verts.Add((x0 + s / 2) | (y0 + s / 2) << 16);              // a sample: s is even, the block level
                var ring = new List<int>(4 * s);
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0 + i, y0));
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0 + s, y0 + i));
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0 + s - i, y0 + s));
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0, y0 + s - i));
                for (int i = 0; i < ring.Count; i++) Tri(c, ring[i], ring[(i + 1) % ring.Count]);
            }
            void Block(int x0, int y0, int s)                           // inside a dilated top-level block
            {
                if (x0 >= cells || y0 >= cells || !AnyDrawn(x0, y0, Mathf.Min(s, Mathf.Min(cells - x0, cells - y0)))) return;
                if (x0 + s <= cells && y0 + s <= cells && s > 1 && Flat(x0, y0, s)) { Fan(x0, y0, s); return; }
                if (s > 1)
                {
                    int h = s / 2;
                    Block(x0, y0, h); Block(x0 + h, y0, h); Block(x0, y0 + h, h); Block(x0 + h, y0 + h, h);
                    return;
                }
                if (!Drawn(x0, y0)) return;
                int a = Vertex(x0, y0), b = Vertex(x0 + 1, y0), d = Vertex(x0, y0 + 1), e = Vertex(x0 + 1, y0 + 1);
                Tri(a, b, e); Tri(a, e, d);
            }
            for (int by = 0; by < blocks; by++)
                for (int bx = 0; bx < blocks; bx++)
                {
                    if (!Relevant(bx, by)) continue;
                    int x0 = bx * B, y0 = by * B;
                    bx0 = x0; by0 = y0;
                    for (int i = 0; i < inner.Length; i++) inner[i] = -1;
                    if (x0 + B <= cells && y0 + B <= cells && Flat(x0, y0, B)) { Fan(x0, y0, B); continue; }
                    Dilate(x0, y0);
                    Block(x0, y0, B);
                }
            return (verts.ToArray(), tris.ToArray());
        }

        /// <summary>The water mesh from its samples (WaterMeshGrid): every vertex on the terrain's own heightmap sample.</summary>
        public static Mesh WaterMesh(int res, Vector3 size, float[,] heights, int[] grid, int[] indices)
        {
            int cells = res - 1;
            float stepX = size.x / cells, stepZ = size.z / cells;
            var verts = new Vector3[grid.Length];
            for (int i = 0; i < grid.Length; i++)
            {
                int x = grid[i] & 0xFFFF, y = grid[i] >> 16;
                verts[i] = new Vector3(x * stepX, heights[y, x] * size.y, y * stepZ);
            }
            var mesh = new Mesh { name = "TerrainWater", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            mesh.bounds = new Bounds(size * 0.5f, size);
            if (!Application.isEditor) mesh.UploadMeshData(true);   // players keep only the GPU copy (about 4.5 MB less heap)
            return mesh;
        }

        /// <summary>terrain_water.bytes (MapTexturePaths.WaterMesh): "SCW1", the heightmap's resolution, a check of its
        /// heights (HeightsCheck), the vertex and index counts, then WaterMeshGrid's two arrays, all little-endian int32.</summary>
        const int WaterMagic = 0x31574353;
        public static byte[] WaterMeshBytes(int res, float[,] heights, int[] grid, int[] indices)
        {
            var bytes = new byte[20 + 4 * (grid.Length + indices.Length)];
            int[] head = { WaterMagic, res, HeightsCheck(heights), grid.Length, indices.Length };
            Buffer.BlockCopy(head, 0, bytes, 0, 20);
            Buffer.BlockCopy(grid, 0, bytes, 20, 4 * grid.Length);
            Buffer.BlockCopy(indices, 0, bytes, 20 + 4 * grid.Length, 4 * indices.Length);
            return bytes;
        }

        /// <summary>Reads terrain_water.bytes; false (and the reason) when it is not for this heightmap.</summary>
        public static bool TryReadWaterMesh(byte[] bytes, int res, float[,] heights, out int[] grid, out int[] indices, out string reason)
        {
            grid = indices = Array.Empty<int>();
            if (bytes == null || bytes.Length < 20) { reason = "missing or short"; return false; }
            var head = new int[5];
            Buffer.BlockCopy(bytes, 0, head, 0, 20);
            if (head[0] != WaterMagic) { reason = "not a water mesh"; return false; }
            if (head[1] != res) { reason = $"made for a {head[1]} heightmap, this one is {res}"; return false; }
            if (head[2] != HeightsCheck(heights)) { reason = "made for other heights (re-bake: TerrainWaterBake.Bake)"; return false; }
            if (bytes.Length != 20 + 4L * (head[3] + head[4])) { reason = "truncated"; return false; }
            grid = new int[head[3]]; indices = new int[head[4]];
            Buffer.BlockCopy(bytes, 20, grid, 0, 4 * grid.Length);
            Buffer.BlockCopy(bytes, 20 + 4 * grid.Length, indices, 0, 4 * indices.Length);
            reason = "";
            return true;
        }

        /// <summary>A cheap check that baked water data still matches the heights: FNV-1a over every 997th sample's bits.</summary>
        public static int HeightsCheck(float[,] heights)
        {
            int res = heights.GetLength(1), n = heights.Length;
            uint h = 2166136261;
            for (int i = 0; i < n; i += 997)
            {
                uint bits = (uint)BitConverter.SingleToInt32Bits(heights[i / res, i % res]);
                for (int k = 0; k < 4; k++) { h ^= (bits >> (8 * k)) & 0xFF; h *= 16777619; }
            }
            return (int)h;
        }

        /// <summary>Vertices of one draped hex ring (<see cref="DrapedRing"/>).</summary>
        public const int RingVertices = 6 * OverlaySegmentsPerEdge;

        /// <summary>Appends the <see cref="RingVertices"/> vertices of a hex outline of circumradius <paramref name="size"/>
        /// around <paramref name="centre"/> (world x, z), corners from HexMath. Each edge is split into OverlaySegmentsPerEdge
        /// sub-segments, clamped to the terrain bounds; each vertex sits <paramref name="clearance"/> above the highest
        /// terrain sampled along its two adjacent sub-segments, so the straight line between two vertices clears any ridge.
        /// The selection outline uses it (the hex grid itself is drawn by the shaders since visual v1 Task 8).</summary>
        public static void DrapedRing(GeoFrame frame, TerrainData data, Vector2 centre, float size, float clearance, List<Vector3> verts)
        {
            int n = RingVertices;
            var corners = new Vector2[6]; var ground = new Vector2[n]; var segMax = new float[n];
            for (int i = 0; i < 6; i++) corners[i] = HexMath.Corner(centre, size, i);
            for (int i = 0; i < 6; i++)
                for (int s = 0; s < OverlaySegmentsPerEdge; s++)
                {
                    var k = Vector2.Lerp(corners[i], corners[(i + 1) % 6], (float)s / OverlaySegmentsPerEdge);
                    k.x = Mathf.Clamp(k.x, 0f, frame.WidthM); k.y = Mathf.Clamp(k.y, 0f, frame.LengthM);
                    ground[i * OverlaySegmentsPerEdge + s] = k;
                }
            for (int j = 0; j < n; j++)
            {
                float m = float.MinValue;
                for (int t = 0; t <= OverlaySamplesPerSegment; t++)
                {
                    var p = Vector2.Lerp(ground[j], ground[(j + 1) % n], (float)t / OverlaySamplesPerSegment);
                    m = Mathf.Max(m, data.GetInterpolatedHeight(p.x / frame.WidthM, p.y / frame.LengthM));
                }
                segMax[j] = m;
            }
            for (int j = 0; j < n; j++)
                verts.Add(new Vector3(ground[j].x, Mathf.Max(segMax[(j + n - 1) % n], segMax[j]) + clearance, ground[j].y));
        }
    }
}
