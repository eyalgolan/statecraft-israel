#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>How a region is outlined: the promo's blue band with a white inner stripe (Primary), its red-orange band with
    /// a cream inner stripe (Secondary), or not at all (None: a region that only stops its neighbours' bands from running on,
    /// e.g. the land past a border that is not drawn from its side).</summary>
    public enum BorderStyle : byte { None = 0, Primary = 1, Secondary = 2 }

    /// <summary>Which region each hex belongs to, and how each region is outlined: what the border renderer draws. A hex not
    /// listed belongs to no region, and no band is drawn against it (the coast, the edge of the data). Region ids run from 1
    /// to <see cref="MaxId"/>. The renderer draws only what a layer says, so a layer must come from real, sourced data (owner
    /// ruling, 2026-10-11: no invented political boundaries): the 1945-48 districts and sub-districts, the 1949 armistice
    /// lines when dated, fronts. Play draws the 1948 sub-districts (<see cref="Subdistricts1948"/>); the rest are research
    /// items (docs/visual-v1.md, "Region borders"); <see cref="BorderProof"/> is a synthetic pattern for tests only.</summary>
    public sealed class RegionLayer
    {
        public const int MaxId = 65535;   // two texel bytes

        public readonly string Name;
        /// <summary>A label shown on screen while the layer is drawn (null: none). The synthetic proof sets one, so no
        /// screenshot of it can pass for real boundaries.</summary>
        public readonly string? Banner;
        readonly Dictionary<(int q, int r), int> regionOf;
        readonly Dictionary<int, BorderStyle> styleOf;

        /// <summary>Throws ArgumentException naming the layer for an id outside 1..MaxId, a hex's region with no style, or an
        /// undefined style.</summary>
        public RegionLayer(string name, IReadOnlyDictionary<(int q, int r), int> hexRegions, IReadOnlyDictionary<int, BorderStyle> styles,
            string? banner = null)
        {
            Banner = banner;
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("a region layer needs a name", nameof(name)) : name;
            if (hexRegions == null) throw new ArgumentNullException(nameof(hexRegions));
            if (styles == null) throw new ArgumentNullException(nameof(styles));
            foreach (var kv in styles)
            {
                if (kv.Key < 1 || kv.Key > MaxId) throw new ArgumentException($"{name}: region id {kv.Key} outside 1..{MaxId}");
                if (!Enum.IsDefined(typeof(BorderStyle), kv.Value)) throw new ArgumentException($"{name}: region {kv.Key} has an unknown style {(int)kv.Value}");
            }
            foreach (var kv in hexRegions)
                if (!styles.ContainsKey(kv.Value)) throw new ArgumentException($"{name}: hex ({kv.Key.q}, {kv.Key.r}) is in region {kv.Value}, which has no style");
            regionOf = new Dictionary<(int q, int r), int>(hexRegions);
            styleOf = new Dictionary<int, BorderStyle>(styles);
        }

        /// <summary>The hex's region, or 0 (none).</summary>
        public int RegionOf(int q, int r) => regionOf.TryGetValue((q, r), out var id) ? id : 0;
        public BorderStyle StyleOf(int id) => styleOf.TryGetValue(id, out var s) ? s : BorderStyle.None;
        public IEnumerable<(int q, int r)> Hexes => regionOf.Keys;
        public int Count => regionOf.Count;
    }

    /// <summary>The region borders (promo-borders): the promo 03 look of coloured, two-tone bands along hex edges, drawn by
    /// the terrain shader (StatecraftBorders.hlsl) from one texel per hex. <see cref="Apply"/> runs on every map build
    /// (MapBuilder.Build, after MapShaderGlobals.Apply): it allocates the hex texture, draws nothing, and registers the "borders" toggle;
    /// <see cref="Use"/> gives the built map a layer. Visual only: it reads no sim state and changes none.</summary>
    public static class MapBorders
    {
        public const string ToggleName = "borders";

        static readonly int RegionTex = Shader.PropertyToID("_ScRegionTex"), RegionInfo = Shader.PropertyToID("_ScRegionInfo"),
            Borders = Shader.PropertyToID("_ScBorders"),
            Colour1 = Shader.PropertyToID("_ScBorderColour1"), Light1 = Shader.PropertyToID("_ScBorderLight1"), Shape1 = Shader.PropertyToID("_ScBorderShape1"),
            Colour2 = Shader.PropertyToID("_ScBorderColour2"), Light2 = Shader.PropertyToID("_ScBorderLight2"), Shape2 = Shader.PropertyToID("_ScBorderShape2");

        /// <summary>One style's look. Colours are sRGB (above 1: brighter than white before the grade), measured on
        /// promo/final/03-gameplay-1936.png (2752 px wide) and set so the graded game matches them (docs/visual-v1.md, "Region borders"). BandM is the coloured plus light stripe in metres,
        /// so the band grows towards the camera as the promo's does; ColourShare the coloured stripe's part of it; Wash the
        /// wash's width as a multiple of the band, WashStrength its opacity at the band.</summary>
        public readonly struct Style
        {
            public readonly Color Colour, Light; public readonly float BandM, ColourShare, Wash, WashStrength;
            public Style(Color colour, Color light, float bandM, float colourShare, float wash, float washStrength)
            { Colour = colour; Light = light; BandM = bandM; ColourShare = colourShare; Wash = wash; WashStrength = washStrength; }
        }

        /// <summary>The promo's blue line (6 + 6 px of blue and white with a 10 px wash at 2752 px; 125,140,182 and
        /// 251,251,248): the heavier of the two.</summary>
        public static readonly Style Primary = new Style(new Color(0.40f, 0.50f, 0.80f), new Color(1.08f, 1.08f, 1.06f), 230f, 0.5f, 0.9f, 0.40f);
        /// <summary>The promo's red-orange line (4 + 3 px with an 8 px wash; 176,93,61 and 246,188,124), a little more
        /// saturated than the promo's so it keeps a contrast against both olive and dry-grass ground for colour-blind players
        /// (GradeTests).</summary>
        public static readonly Style Secondary = new Style(new Color(0.75f, 0.31f, 0.13f), new Color(1.05f, 0.76f, 0.48f), 200f, 0.6f, 1.0f, 0.18f);
        /// <summary>The band draws at least MinBandPx wide (the overview), unless that would make it, with its wash, wider than
        /// MaxShareOfInradius of a hex's inradius: that cap wins (below about 8 px of inradius on screen the band goes under
        /// 2 px), so a far hex is never filled by its outline.</summary>
        public const float MinBandPx = 2f, MaxShareOfInradius = 0.5f;

        static Texture2D? texture;
        static MapRuntimeAssets? owner;
        static GameObject? banner;
        static bool hasLayer, toggleOn = true;
        static bool noEarlyOut;

        /// <summary>Test seam (_ScBorders.z): true draws every pixel the long way, with no early-out, so a test can prove the
        /// early-out exact (the two renders must match pixel for pixel).</summary>
        internal static bool NoEarlyOut { get => noEarlyOut; set { noEarlyOut = value; Push(); } }
        static int qMin, rMin;
        static readonly HashSet<(int q, int r)> mapHexes = new HashSet<(int q, int r)>();

        /// <summary>The layer the map draws now (null: none).</summary>
        public static RegionLayer? Current { get; private set; }

        /// <summary>Called by MapBuilder.Build (after MapShaderGlobals.Apply) on every build: a cleared hex texture over hexes.json's (q, r) range (owned
        /// by the map), the styles' globals, no layer, and the "borders" toggle (on), all undone by the map's teardown.</summary>
        public static void Apply(HexFile hexes, MapRuntimeAssets assets)
        {
            if (hexes == null) throw new ArgumentNullException(nameof(hexes));
            if (assets == null) throw new ArgumentNullException(nameof(assets));
            qMin = hexes.hexes.Min(h => h.q); rMin = hexes.hexes.Min(h => h.r);
            int w = hexes.hexes.Max(h => h.q) - qMin + 1, h = hexes.hexes.Max(x => x.r) - rMin + 1;
            mapHexes.Clear();
            foreach (var x in hexes.hexes) mapHexes.Add((x.q, x.r));
            var tex = assets.Own(new Texture2D(w, h, TextureFormat.RGBA32, false, true)
            {
                name = "RegionHexes", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            });
            tex.SetPixels32(new Color32[w * h]); tex.Apply(false, false);
            texture = tex; owner = assets; banner = null;
            Current = null; hasLayer = false; toggleOn = true; noEarlyOut = false;
            Shader.SetGlobalTexture(RegionTex, tex);
            Shader.SetGlobalVector(RegionInfo, new Vector4(qMin, rMin, w, h));
            SetStyle(Colour1, Light1, Shape1, Primary);
            SetStyle(Colour2, Light2, Shape2, Secondary);
            Push();
            assets.OnTeardown(VisualToggles.Register(ToggleName, on => { toggleOn = on; Push(); }));
            assets.OnTeardown(() => { texture = null; owner = null; banner = null; Current = null; hasLayer = false; noEarlyOut = false; Push(); });
        }

        /// <summary>Draws <paramref name="layer"/> on the built map (null: none). Throws ArgumentException for a hex that is
        /// not on the map, InvalidOperationException before a map is built.</summary>
        public static void Use(RegionLayer? layer)
        {
            if (texture == null) throw new InvalidOperationException("MapBorders.Use needs a built map");
            int w = texture.width, h = texture.height;
            var px = new Color32[w * h];
            if (layer != null)
                foreach (var (q, r) in layer.Hexes)
                {
                    if (!mapHexes.Contains((q, r))) throw new ArgumentException($"{layer.Name}: hex ({q}, {r}) is not on the map");
                    px[(r - rMin) * w + (q - qMin)] = Encode(layer.RegionOf(q, r), layer.StyleOf(layer.RegionOf(q, r)));
                }
            texture.SetPixels32(px); texture.Apply(false, false);
            Current = layer; hasLayer = layer != null && layer.Count > 0;
            if (banner != null) { DestroyObject(banner); banner = null; }
            if (layer?.Banner != null && owner != null) banner = Banner.Create(layer.Banner, owner);
            Push();
        }

        static void DestroyObject(UnityEngine.Object o) { if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o); }

        /// <summary>The texel of a hex: r + 256 g the region id, b the style (StatecraftBorders.hlsl, ScRegionAt).</summary>
        public static Color32 Encode(int id, BorderStyle style) => new Color32((byte)(id & 0xFF), (byte)(id >> 8), (byte)style, 255);

        static void SetStyle(int colour, int light, int shape, Style s)
        {
            var c = s.Colour.linear; var l = s.Light.linear;
            Shader.SetGlobalVector(colour, new Vector4(c.r, c.g, c.b, 1f));
            Shader.SetGlobalVector(light, new Vector4(l.r, l.g, l.b, 1f));
            Shader.SetGlobalVector(shape, new Vector4(s.BandM, s.ColourShare, s.Wash, s.WashStrength));
        }

        static void Push()
        {
            bool drawn = toggleOn && hasLayer && texture != null;
            Shader.SetGlobalVector(Borders, new Vector4(MinBandPx, MaxShareOfInradius, noEarlyOut ? 1f : 0f, drawn ? 1f : 0f));
            if (banner != null) banner.SetActive(drawn);
        }

        /// <summary>The banner that labels a layer on screen: white text with a dark drop copy, a child of the main camera
        /// at a fixed place in view (left, 35% down: inside the tilt-shift's sharp band, so it stays legible), drawn by the camera itself so editor
        /// renders, tests and players all carry it. Owned by the map (destroyed with it).</summary>
        static class Banner
        {
            public static GameObject? Create(string text, MapRuntimeAssets assets)
            {
                var cam = Camera.main;
                if (cam == null) return null;
                var root = assets.Own(new GameObject("BorderBanner"));
                root.transform.SetParent(cam.transform, false);
                root.transform.localRotation = Quaternion.identity;
                root.AddComponent<BannerPlacement>().Place(cam);
                float d = BannerPlacement.Depth(cam), halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                float height = 0.065f * halfH;   // about 3.3% of the screen's height per line
                Line(root.transform, text, font, height, new Color(0.08f, 0.06f, 0.05f), new Vector3(height * 0.06f, -height * 0.06f, height * 0.01f));
                Line(root.transform, text, font, height, new Color(1f, 0.98f, 0.9f), Vector3.zero);
                return root;
            }

            /// <summary>Keeps the banner at the top left of whatever its camera draws into: placed from the camera's aspect
            /// just before each of its renders (URP's beginCameraRendering), so a 4:3 window or a 1920x1080 capture both show
            /// it whole.</summary>
            [ExecuteAlways]
            sealed class BannerPlacement : MonoBehaviour
            {
                public static float Depth(Camera cam) => cam.nearClipPlane * 4f;

                public void Place(Camera cam)
                {
                    float d = Depth(cam), halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), halfW = halfH * cam.aspect;
                    transform.localPosition = new Vector3(-0.94f * halfW, 0.30f * halfH, d);   // 35% down: inside the tilt-shift's sharp band
                }

                void OnEnable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += Before;
                void OnDisable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= Before;
                void Before(UnityEngine.Rendering.ScriptableRenderContext _, Camera cam)
                {
                    if (this != null && transform.parent == cam.transform) Place(cam);
                }
            }

            static void Line(Transform parent, string text, Font font, float height, Color colour, Vector3 offset)
            {
                var go = new GameObject("Line");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = offset;
                var tm = go.AddComponent<TextMesh>();
                tm.text = text; tm.font = font; tm.fontSize = 64; tm.characterSize = height / 6.4f; tm.anchor = TextAnchor.UpperLeft;
                tm.fontStyle = FontStyle.Bold;
                tm.color = colour;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = font.material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            }
        }
    }

    /// <summary>A synthetic region pattern that shows the border renderer at work, for tests and captures only: a checker of
    /// <see cref="BlockSize"/> x <see cref="BlockSize"/> axial hex blocks (parallelograms, a regular lattice over the whole
    /// map, unrelated to any town or line), alternately Primary and Secondary; sea and lake hexes in no region. It carries
    /// the on-screen label <see cref="Label"/> while drawn, and only the editor and development builds honour its flag
    /// ("borderproof" in the WebGL page URL, -borderproof on desktop and in the editor's promo renders): a release
    /// (shareable) build never shows it (review I3).</summary>
    public static class BorderProof
    {
        public const string Flag = "borderproof";
        public const string Label = "SYNTHETIC TEST PATTERN \u2014 not real boundaries";
        public const int BlockSize = 4;

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer,
                Application.isEditor || Debug.isDebugBuild);

        /// <summary>Whether the flag is given and honoured: never in a release build (<paramref name="devBuild"/> false).</summary>
        public static bool Requested(string? url, string[] args, bool webgl, bool devBuild) =>
            devBuild && (webgl ? UrlFlags.Has(url, Flag) : args.Contains("-" + Flag));

        /// <summary>The checker on <paramref name="hexes"/>' land hexes.</summary>
        public static RegionLayer Build(HexFile hexes)
        {
            var regions = new Dictionary<(int q, int r), int>(); var styles = new Dictionary<int, BorderStyle>();
            foreach (var h in hexes.hexes)
            {
                if (!h.land) continue;
                var (bq, br) = Block(h.q, h.r);
                int id = 1 + (bq + 64) + 128 * br;
                regions[(h.q, h.r)] = id;
                styles[id] = ((bq + br) & 1) == 0 ? BorderStyle.Primary : BorderStyle.Secondary;
            }
            return new RegionLayer("border proof (synthetic checker)", regions, styles, Label);
        }

        /// <summary>The checker block of axial hex (q, r).</summary>
        public static (int bq, int br) Block(int q, int r) => (FloorDiv(q, BlockSize), FloorDiv(r, BlockSize));

        static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

        /// <summary>The pattern on the built map, drawn at once, with its label.</summary>
        public static RegionLayer Show(MapScene map)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) throw new InvalidOperationException("BorderProof needs a built map");
            var layer = Build(map.Hexes);
            MapBorders.Use(layer);
            return layer;
        }

        /// <summary>The axial hex under a lon/lat point of the built map.</summary>
        public static (int q, int r) HexAt(MapScene map, double lon, double lat)
        {
            float size = map.Hexes.hex_size_km * 1000f;
            var p = HexGrid.FromWorld(map.Frame.ToWorld(lon, lat, 0), map.Terrain.transform.position, map.Terrain.terrainData.size.z);
            var c = HexGrid.NearestCentre(p, size);
            int r = Mathf.RoundToInt(c.y / (1.5f * size));
            return (Mathf.RoundToInt((c.x / size - 0.8660254f * r) / 1.7320508f), r);
        }

        public static int Distance((int q, int r) a, (int q, int r) b)
        {
            int dq = a.q - b.q, dr = a.r - b.r;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }
    }
}
