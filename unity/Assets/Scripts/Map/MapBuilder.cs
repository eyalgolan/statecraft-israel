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

        /// <summary>The muted land tone of neighbouring land past the map's edge (BorderLand.mat).</summary>
        public static readonly Color BorderLandColour = new Color(0.60f, 0.56f, 0.46f);

        /// <summary>Biome colours of the terrain layers (the minimap uses the same ones).</summary>
        public static IReadOnlyDictionary<string, Color> BiomeColours => Biome;

        static readonly Dictionary<string, Color> Biome = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.16f, 0.42f, 0.55f), ["lake"] = new Color(0.20f, 0.50f, 0.62f),
            ["coastal_plain"] = new Color(0.55f, 0.62f, 0.36f), ["hills"] = new Color(0.62f, 0.58f, 0.40f),
            ["mountains"] = new Color(0.58f, 0.52f, 0.42f), ["valley"] = new Color(0.50f, 0.56f, 0.38f),
            ["desert"] = new Color(0.80f, 0.68f, 0.48f),
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
            Action<Color32[], int, GeoFrame> paintGround)
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
            MapShaderGlobals.Apply(terrain, meta, hexFile.hex_size_km * 1000f, BakeBiomeTexture(frame, hexFile, assets, paintGround), assets);
            Boot.DiagLog.BootPhase("biome texture baked");
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
        /// terrain shader samples as _ScBiomeTex. Owned by the map.</summary>
        static Texture2D BakeBiomeTexture(GeoFrame frame, HexFile hexes, MapRuntimeAssets assets, Action<Color32[], int, GeoFrame> paint)
        {
            var names = new List<string>(Biome.Keys);
            var colours = new Color[names.Count];
            for (int i = 0; i < names.Count; i++) colours[i] = MapPalette.Ground[names[i]];
            var centres = new List<Vector2>(hexes.hexes.Length); var layerOf = new List<int>(hexes.hexes.Length);
            foreach (var h in hexes.hexes)
            {
                var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add(new Vector2(w.x, w.z)); layerOf.Add(names.IndexOf(h.biome));
            }
            var blender = new BiomeBlender(centres, layerOf, hexes.hex_size_km * 1000f, names.Count);
            int res = BiomeTextureResolution;
            var tex = assets.Own(new Texture2D(res, res, TextureFormat.RGBA32, true, false)
                { name = "BiomeColour", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear });
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
            paint?.Invoke(px, res, frame);
            tex.SetPixels32(px); tex.Apply(true, true);   // no CPU copy: nothing reads the pixels back (about 5.6 MB on the heap)
            return tex;
        }

        /// <summary>Distances (m) of the border skirt's rings from the map's edge: land holds the edge's height across the
        /// first rings and reaches sea level only at the last, beyond anything the camera can see (SkirtFall).</summary>
        public static readonly float[] SkirtRings = { 0f, 4000f, 20000f, 60000f, 600000f };
        /// <summary>Share of the fall to sea level reached at each ring (land samples only). Visual v1 Task 7: the edge's height
        /// is held for 20 km (the Jordanian plateau, about 900 m, continues past the edge instead of sagging at once) and
        /// eases to sea level by the last ring, beyond anything the camera sees.</summary>
        public static readonly float[] SkirtFall = { 0f, 0.05f, 0.15f, 0.45f, 1f };
        /// <summary>Half-width (m) of the along-edge average the land skirt holds past its inner ring (Task 7): holding each
        /// edge vertex's own height for 20 km drew its ridges and gullies out as long stripes across the skirt, so the rings
        /// past the edge hold the edge's broad height (the plateau) and only ring 0 follows the terrain's edge exactly.</summary>
        public const float SkirtHoldSmoothingM = 8000f;
        /// <summary>A heightmap sample this close to 0 m is sea (tools/dem_to_heightmap.py writes open sea as exactly 0 m).</summary>
        public const float SeaToleranceM = 0.5f;

        /// <summary>The land and sea past the map's edge (fix round 2, R2-1 and N1). Each edge sample continues outward: a sea
        /// sample stays flat at sea level (submesh 0, Sea.mat: the unlit Statecraft/Sea, shaded by the same ScWaterShade as the
        /// terrain's own water, so no seam shows); a land sample meets the terrain's edge at ring 0, then holds the along-edge
        /// mean of the edge's height (SkirtHoldSmoothingM) and eases to sea level by the last ring (SkirtFall; submesh 1, a
        /// muted land tone), so Sinai, Jordan and Lebanon read as land. A segment with a land end is land. Corner fans close the four corners. Every triangle lies outside the
        /// terrain's footprint, so nothing covers the map (the old full sea plane hid all land below about -10 m).</summary>
        public static Mesh BuildBorderSkirt(TerrainData data, float elevMinM, MapRuntimeAssets assets)
        {
            var size = data.size;
            float seaY = (0f - elevMinM) * VerticalExaggeration;
            // One spoke per terrain vertex along each edge (the heightmap's own resolution), so the skirt's inner edge follows
            // the terrain's edge exactly; sampling every second vertex left slivers up to 36 m (fix round 3, m-4).
            int samples = data.heightmapResolution - 1;
            var verts = new List<Vector3>(); var sea = new List<int>(); var land = new List<int>();
            bool IsLand(float h) => Mathf.Abs(h / VerticalExaggeration + elevMinM) > SeaToleranceM;
            // The edge's heights and their running mean over +-SkirtHoldSmoothingM of the edge's own samples (prefix sums; the
            // window is cut short at the edge's ends). Every sample counts, so no ridge between taps aliases (fix round 1, M4).
            float[] HeldAlong(Vector2 a, Vector2 b, float[] h)
            {
                for (int i = 0; i <= samples; i++) h[i] = Height(Vector2.Lerp(a, b, (float)i / samples));
                var sum = new double[samples + 2];
                for (int i = 0; i <= samples; i++) sum[i + 1] = sum[i] + h[i];
                int w = Mathf.Max(1, Mathf.RoundToInt(SkirtHoldSmoothingM / ((b - a).magnitude / samples)));
                var held = new float[samples + 1];
                for (int i = 0; i <= samples; i++)
                {
                    int lo = Mathf.Max(0, i - w), hi = Mathf.Min(samples, i + w);
                    held[i] = (float)((sum[hi + 1] - sum[lo]) / (hi - lo + 1));
                }
                return held;
            }
            // One ring of vertices outward from an edge point (h its height, held the height the rings past it hold); returns
            // the index of its first vertex.
            int Spoke(Vector2 p, float h, float held, Vector2 outward, bool isLand, Vector2? outward2 = null)
            {
                int first = verts.Count;
                for (int k = 0; k < SkirtRings.Length; k++)
                {
                    var q = p + (outward + (outward2 ?? Vector2.zero)) * SkirtRings[k];
                    float y = isLand ? Mathf.Lerp(held, seaY, SkirtFall[k]) : seaY;
                    if (k == 0) y = h;                                     // meets the terrain's edge exactly
                    verts.Add(new Vector3(q.x, y, q.y));
                }
                return first;
            }
            void Quad(List<int> into, int a0, int b0)                   // the band between two spokes
            {
                for (int k = 0; k + 1 < SkirtRings.Length; k++)
                {
                    int a = a0 + k, a1 = a0 + k + 1, b = b0 + k, b1 = b0 + k + 1;
                    Tri(into, a, b, b1); Tri(into, a, b1, a1);
                }
            }
            void Tri(List<int> into, int i, int j, int l)               // wound to face up
            {
                var n = Vector3.Cross(verts[j] - verts[i], verts[l] - verts[i]);
                if (n.y < 0) { var t = j; j = l; l = t; }
                if (Mathf.Abs(n.y) > 1e-3f) { into.Add(i); into.Add(j); into.Add(l); }   // drops the degenerate corner centre
            }
            float Height(Vector2 p) => data.GetInterpolatedHeight(Mathf.Clamp01(p.x / size.x), Mathf.Clamp01(p.y / size.z));
            // The four edges, walked along: (start, end, outward).
            var edges = new (Vector2 a, Vector2 b, Vector2 n)[]
            {
                (new Vector2(0, 0), new Vector2(size.x, 0), Vector2.down),                 // south
                (new Vector2(size.x, 0), new Vector2(size.x, size.z), Vector2.right),       // east
                (new Vector2(size.x, size.z), new Vector2(0, size.z), Vector2.up),          // north
                (new Vector2(0, size.z), new Vector2(0, 0), Vector2.left),                  // west
            };
            var heights = new float[edges.Length][]; var helds = new float[edges.Length][];
            for (int e = 0; e < edges.Length; e++) { heights[e] = new float[samples + 1]; helds[e] = HeldAlong(edges[e].a, edges[e].b, heights[e]); }
            for (int e = 0; e < edges.Length; e++)
            {
                var (a, b, n) = edges[e];
                int prev = -1; bool prevLand = false;
                for (int i = 0; i <= samples; i++)
                {
                    var p = Vector2.Lerp(a, b, (float)i / samples);
                    bool isLand = IsLand(heights[e][i]);
                    int spoke = Spoke(p, heights[e][i], helds[e][i], n, isLand);
                    if (prev >= 0) Quad(prevLand || isLand ? land : sea, prev, spoke);
                    prev = spoke; prevLand = isLand;
                }
            }
            // Corners: a fan between the two edges' outward spokes and the diagonal, in the corner sample's kind.
            for (int e = 0; e < edges.Length; e++)
            {
                var corner = edges[e].b; var n1 = edges[e].n; var n2 = edges[(e + 1) % edges.Length].n;
                int e2 = (e + 1) % edges.Length;
                float h = heights[e][samples], held1 = helds[e][samples], held2 = helds[e2][0];   // the corner ends edge e, starts e2
                bool isLand = IsLand(h);
                int s1 = Spoke(corner, h, held1, n1, isLand), sd = Spoke(corner, h, 0.5f * (held1 + held2), n1, isLand, n2), s2 = Spoke(corner, h, held2, n2, isLand);
                Quad(isLand ? land : sea, s1, sd); Quad(isLand ? land : sea, sd, s2);
            }
            var mesh = assets.Own(new Mesh { name = "BorderSkirt", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 });
            mesh.SetVertices(verts);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(sea, 0); mesh.SetTriangles(land, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
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
