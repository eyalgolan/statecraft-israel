#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>How a region is outlined: the promo's deeper blue band with a white inner stripe (Primary: the Israeli side),
    /// its red-orange band with a cream inner stripe (Secondary: the other side), a grey band (Neutral: contested, no-man's
    /// land, a demilitarized zone, or the 1947 sub-districts before any side is sourced; owner ruling 2026-10-14), or not at
    /// all (None: a region that only stops its neighbours' bands from running on, e.g. the land past a border that is not
    /// drawn from its side).</summary>
    public enum BorderStyle : byte { None = 0, Primary = 1, Secondary = 2, Neutral = 3 }

    /// <summary>The Civ-like border prototype's variants for the owner to choose from (#196, owner 2026-10-26: "make the border
    /// lines more visible and civ-like", "Try dark outline, Try stronger blue"). A2 is the default (owner ruling 2026-10-27:
    /// "A2: stronger bands (Recommended)"; MapBorders.DefaultVariant). Legacy: main's earlier borders exactly, still
    /// selectable (review C320). A: bands that hold their weight at every
    /// zoom, an equal wash on both sides reaching a quarter of the hex, pixel floors and a territory tint that grows as the
    /// camera pulls out. B: A with a thin dark outer rim. C: A with the Primary blue pushed toward Okabe-Ito's
    /// colour-blind-safe blue (#0072B2). A2, B2, C2: the same on the stronger tier for gameplay03, the view players live in
    /// (coordinator, 2026-10-26: A read almost like main there): a 6 px colour stripe floor, a 0.45 wash that holds most
    /// of its strength until near its reach, and a 0.12 tint up close (MapBorders.Strong*).</summary>
    public enum BorderVariant : byte { Legacy = 0, A = 1, B = 2, C = 3, A2 = 4, B2 = 5, C2 = 6 }

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
            Borders = Shader.PropertyToID("_ScBorders"), Min = Shader.PropertyToID("_ScBorderMin"), Tint = Shader.PropertyToID("_ScBorderTint"),
            Rim = Shader.PropertyToID("_ScBorderRim"), Dash = Shader.PropertyToID("_ScBorderDash"),
            Colour1 = Shader.PropertyToID("_ScBorderColour1"), Light1 = Shader.PropertyToID("_ScBorderLight1"), Shape1 = Shader.PropertyToID("_ScBorderShape1"),
            Colour2 = Shader.PropertyToID("_ScBorderColour2"), Light2 = Shader.PropertyToID("_ScBorderLight2"), Shape2 = Shader.PropertyToID("_ScBorderShape2"),
            Colour3 = Shader.PropertyToID("_ScBorderColour3"), Light3 = Shader.PropertyToID("_ScBorderLight3"), Shape3 = Shader.PropertyToID("_ScBorderShape3");

        /// <summary>One style's look. Colours are sRGB (above 1: brighter than white before the grade), measured on
        /// promo/final/03-gameplay-1936.png (2752 px wide) and set so the graded game matches them (docs/visual-v1.md, "Region borders"). BandM is the coloured plus light stripe in metres,
        /// so the band grows towards the camera as the promo's does; ColourShare the coloured stripe's part of it; WashReach
        /// how far the wash runs from the edge as a share of the hex's inradius, WashStrength its opacity at the band.</summary>
        public readonly struct Style
        {
            public readonly Color Colour, Light; public readonly float BandM, ColourShare, WashReach, WashStrength;
            public Style(Color colour, Color light, float bandM, float colourShare, float washReach, float washStrength)
            { Colour = colour; Light = light; BandM = bandM; ColourShare = colourShare; WashReach = washReach; WashStrength = washStrength; }
        }

        /// <summary>How far every style's wash runs from the edge: a quarter of the hex's inradius (about 1,080 m), the
        /// reach the Civ VI research puts on each side of its border ribbon (prototype #196; was 0.9-1.0 of the band, about
        /// 10% of the inradius with the band).</summary>
        public const float WashReachShare = 0.25f;
        /// <summary>Both sides' wash strength, equal so neither side's territory reads as the junior one (prototype #196;
        /// the Secondary was 0.18 against the Primary's 0.40).</summary>
        public const float SideWashStrength = 0.40f;
        /// <summary>The stronger tier (A2, B2, C2): the colour stripe's floor, both sides' wash strength, the tint up close,
        /// and a wash that holds (falls as 1 - x squared over its reach instead of a smoothstep), so it reads well into the
        /// hex at gameplay03.</summary>
        public const float StrongMinColourPx = 6f, StrongWashStrength = 0.45f, StrongTintNear = 0.12f;
        public static bool Strong(BorderVariant v) => v >= BorderVariant.A2;

        /// <summary>Legacy: main's styles, exactly (shape: band m, colour share, wash width times the band, wash strength),
        /// with main's 2 px floor for the whole band and its far fade (StatecraftBorders.hlsl, ScBorderLegacy).</summary>
        static readonly Vector4 LegacyShape1 = new Vector4(230f, 0.5f, 0.9f, 0.40f), LegacyShape2 = new Vector4(200f, 0.6f, 1.0f, 0.18f),
            LegacyShape3 = new Vector4(200f, 0.6f, 0.8f, 0.14f);
        public const float LegacyMinBandPx = 2f;
        public static bool HasRim(BorderVariant v) => v == BorderVariant.B || v == BorderVariant.B2;
        public static bool StrongBlue(BorderVariant v) => v == BorderVariant.C || v == BorderVariant.C2;

        /// <summary>The promo's blue line (6 + 6 px of blue and white with a 10 px wash at 2752 px; white 251,251,248), the
        /// heavier of the two, in the deeper blue of the owner's ruling (2026-10-14): about 89,119,156 on screen after the
        /// grade (promo 03's Israeli side), against main's lighter 120,142,192.</summary>
        public static readonly Style Primary = new Style(new Color(0.29f, 0.40f, 0.56f), new Color(1.08f, 1.08f, 1.06f), 230f, 0.5f, WashReachShare, SideWashStrength);
        /// <summary>Variant C's Primary: the same band in Okabe-Ito's colour-blind-safe blue #0072B2 (sRGB 0, 114, 178),
        /// stronger and more saturated than the owner's 2026-10-14 deeper blue; a proposal, not a ruling.</summary>
        public static readonly Style PrimaryStrongBlue = new Style(new Color(0f, 0.447f, 0.698f), new Color(1.08f, 1.08f, 1.06f), 230f, 0.5f, WashReachShare, SideWashStrength);
        /// <summary>The promo's red-orange line (4 + 3 px with an 8 px wash; 176,93,61 and 246,188,124), a little more
        /// saturated than the promo's so it keeps a contrast against both olive and dry-grass ground for colour-blind players
        /// (GradeTests).</summary>
        public static readonly Style Secondary = new Style(new Color(0.75f, 0.31f, 0.13f), new Color(1.05f, 0.76f, 0.48f), 200f, 0.6f, WashReachShare, SideWashStrength);
        /// <summary>A grey line with a pale inner stripe and a faint wash, the width of the red-orange: neither side's colour
        /// (contested, no-man's land, a demilitarized zone, the 1947 sub-districts before any side is sourced). Its lightness sits
        /// between the two sides' so colour-blind players still tell it from both (GradeTests).</summary>
        public static readonly Style Neutral = new Style(new Color(0.46f, 0.46f, 0.44f), new Color(0.98f, 0.97f, 0.93f), 200f, 0.6f, WashReachShare, 0.14f);
        /// <summary>The least on-screen widths, measured across the edge (metres per pixel along the band's normal, so they
        /// hold under the camera's tilt): the colour stripe, the light stripe and the wash. They hold at every zoom (no far
        /// fade), unless band plus wash would be wider than MaxShareOfInradius of a hex's inradius: that cap wins, the wash
        /// giving way first, so a far hex is never filled by its outline (BandWidths).</summary>
        public const float MinColourPx = 3f, MinLightPx = 1f, MinWashPx = 12f, MaxShareOfInradius = 0.5f;
        /// <summary>The territory tint: TintNear of the region's colour over the whole region while a hex's inradius is
        /// TintNearInradiusPx or more on screen, rising to TintFar at TintFarInradiusPx and below (the overview), so far off
        /// the map reads as coloured territory with a firm edge (Civ V's 25% owned-hex fill; Civ VI's minimap).</summary>
        public const float TintNear = 0.08f, TintFar = 0.25f, TintNearInradiusPx = 120f, TintFarInradiusPx = 40f;
        /// <summary>The neutral style's dashes (a cue that is not colour, review S9 of #555): NeutralDashM metres a period,
        /// the first NeutralDashOn of it drawn, the period doubling as the camera pulls out so a drawn dash is never under
        /// NeutralDashMinPx.</summary>
        public const float NeutralDashM = 2000f, NeutralDashOn = 0.65f, NeutralDashMinPx = 8f;
        /// <summary>Variant B's dark outer rim along the edge, outside the colour stripe: RimM wide, never under RimMinPx,
        /// drawn at RimOpacity in the band's colour at RimValue of its brightness. Not on a shared edge (one band, no seam).</summary>
        public const float RimM = 50f, RimMinPx = 1f, RimOpacity = 0.6f, RimValue = 0.3f;

        /// <summary>A band's on-screen widths (px) at <paramref name="metresPerPx"/> across its edge: the mirror of
        /// StatecraftBorders.hlsl's ScBandWidths, for the tests. The wash runs to WashReach of the inradius from the edge.</summary>
        public static (float rim, float colour, float light, float wash) BandWidths(Style s, float metresPerPx, float inradiusM, bool rim,
            float minColourPx = MinColourPx)
        {
            float widest = MaxShareOfInradius * inradiusM / metresPerPx;
            float rimPx = rim ? Mathf.Min(Mathf.Max(RimM / metresPerPx, RimMinPx), widest) : 0f;
            float room = widest - rimPx;
            float colour = Mathf.Max(s.BandM * s.ColourShare / metresPerPx, minColourPx);
            float light = Mathf.Max(s.BandM * (1f - s.ColourShare) / metresPerPx, MinLightPx);
            float wash = Mathf.Max(s.WashReach * inradiusM / metresPerPx - colour - light, MinWashPx);
            wash = Mathf.Min(wash, Mathf.Max(room - colour - light, 0f));
            float k = Mathf.Min(1f, room / Mathf.Max(colour + light, 1e-4f));
            return (rimPx, colour * k, light * k, wash);
        }

        /// <summary>The territory tint's opacity when a hex's inradius is <paramref name="inradiusPx"/> on screen.</summary>
        public static float TintAlpha(float inradiusPx, float near = TintNear) =>
            Mathf.Lerp(near, TintFar, Mathf.Clamp01((TintNearInradiusPx - inradiusPx) / (TintNearInradiusPx - TintFarInradiusPx)));

        /// <summary>The neutral dashes' period (m) at <paramref name="metresPerPx"/>: the mirror of the shader's.</summary>
        public static float NeutralDashPeriodM(float metresPerPx) =>
            NeutralDashM * Mathf.Pow(2f, Mathf.Max(0f, Mathf.Ceil(Mathf.Log(NeutralDashMinPx / NeutralDashOn * metresPerPx / NeutralDashM, 2f))));

        /// <summary>The variant drawn when none is asked for: A2 (owner ruling 2026-10-27: "A2: stronger bands
        /// (Recommended)").</summary>
        public const BorderVariant DefaultVariant = BorderVariant.A2;

        /// <summary>The variant drawn: DefaultVariant unless the "bordervariant=legacy|a|b|c|a2|b2|c2" URL parameter (WebGL)
        /// or -bordervariant ... (desktop, editor) names one; anything else is DefaultVariant. Setting it redraws at once; it
        /// outlives map builds.</summary>
        public static BorderVariant Variant
        {
            get => variant ??= VariantRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);
            set { variant = value; if (texture != null) { SetStyles(); Push(); } }
        }
        static BorderVariant? variant;

        public static BorderVariant VariantRequested(string? url, string[] args, bool webgl)
        {
            string? v = null;
            if (webgl)
            {
                var m = System.Text.RegularExpressions.Regex.Match(url ?? "", @"[?&]bordervariant=((?i:legacy|[abc]2?))(?=[&#]|$)");
                if (m.Success) v = m.Groups[1].Value;
            }
            else
            {
                int at = Array.IndexOf(args, "-bordervariant");
                if (at >= 0 && at + 1 < args.Length) v = args[at + 1];
            }
            return v?.ToLowerInvariant() switch
            {
                "b" => BorderVariant.B, "c" => BorderVariant.C, "a2" => BorderVariant.A2, "b2" => BorderVariant.B2, "c2" => BorderVariant.C2,
                "a" => BorderVariant.A, "legacy" => BorderVariant.Legacy, _ => DefaultVariant,
            };
        }

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
            SetStyles();
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
                    int id = layer.RegionOf(q, r), edges = 0, shared = 0;
                    var style = layer.StyleOf(id);
                    // Edge i borders another region when that neighbour is in one (an unlisted hex, on the map or off it,
                    // is 0: the coast and the data's edge get no band) and it is not this one. It is shared when the neighbour
                    // draws its band there in the same style: two sub-districts side by side. Each side then draws part of the
                    // band, so the boundary reads as one band of the promo's width, not two (the Mandate frontier, against
                    // the "outside" region of style none, and a primary-secondary edge keep the full band each side).
                    for (int i = 0; i < NeighbourSteps.Length; i++)
                    {
                        int other = layer.RegionOf(q + NeighbourSteps[i].dq, r + NeighbourSteps[i].dr);
                        if (other <= 0 || other == id) continue;
                        edges |= 1 << i;
                        if (style != BorderStyle.None && layer.StyleOf(other) == style) shared |= 1 << i;
                    }
                    px[(r - rMin) * w + (q - qMin)] = Encode(id, style, edges, shared);
                }
            texture.SetPixels32(px); texture.Apply(false, false);
            Current = layer; hasLayer = layer != null && layer.Count > 0;
            if (banner != null) { DestroyObject(banner); banner = null; }
            if (layer?.Banner != null && owner != null) banner = Banner.Create(layer.Banner, owner);
            Push();
        }

        static void DestroyObject(UnityEngine.Object o) { if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o); }

        /// <summary>The texel of a hex: r + 256 g the region id; b the style in its low two bits and, above them, a bit per
        /// shared edge (one whose neighbour draws a band in the same style, so each side draws part of it: StatecraftBorders.hlsl, ScSharedBand); a 192 plus a bit per edge
        /// that borders another region; edge bits in <see cref="NeighbourSteps"/> order (StatecraftBorders.hlsl, ScRegionAt).
        /// Baked here so the terrain's shader reads one texel, not seven (issue #195). The default, every edge a border and
        /// none shared, is a = 255.</summary>
        public static Color32 Encode(int id, BorderStyle style, int borderEdges = 0x3F, int sharedEdges = 0) =>
            new Color32((byte)(id & 0xFF), (byte)(id >> 8), (byte)((int)style | (sharedEdges & borderEdges & 0x3F) << 2),
                (byte)(0xC0 | (borderEdges & 0x3F)));

        /// <summary>Axial steps to a hex's six neighbours, in tools/hexgrid.py's NEIGHBOURS order: the order of
        /// StatecraftBorders.hlsl's ScHexNormals, so bit i of the edge mask is the edge shared with neighbour i.</summary>
        public static readonly (int dq, int dr)[] NeighbourSteps = { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };

        static void SetStyles()
        {
            var v = Variant; bool strong = Strong(v);
            if (v == BorderVariant.Legacy)
            {
                SetStyle(Colour1, Light1, Shape1, Primary); Shader.SetGlobalVector(Shape1, LegacyShape1);
                SetStyle(Colour2, Light2, Shape2, Secondary); Shader.SetGlobalVector(Shape2, LegacyShape2);
                SetStyle(Colour3, Light3, Shape3, Neutral); Shader.SetGlobalVector(Shape3, LegacyShape3);
                Shader.SetGlobalVector(Min, new Vector4(0f, 0f, 0f, 1f));   // w 1: main's drawing (ScBorderLegacy)
                Shader.SetGlobalVector(Rim, Vector4.zero);
                return;
            }
            Style Side(Style s) => strong ? new Style(s.Colour, s.Light, s.BandM, s.ColourShare, s.WashReach, StrongWashStrength) : s;
            SetStyle(Colour1, Light1, Shape1, Side(StrongBlue(v) ? PrimaryStrongBlue : Primary));
            SetStyle(Colour2, Light2, Shape2, Side(Secondary));
            SetStyle(Colour3, Light3, Shape3, Neutral);
            Shader.SetGlobalVector(Min, new Vector4(MinLightPx, MinWashPx, strong ? 1f : 0f, 0f));   // w 0: the prototype's drawing
            Shader.SetGlobalVector(Tint, new Vector4(strong ? StrongTintNear : TintNear, TintFar, TintNearInradiusPx, TintFarInradiusPx));
            Shader.SetGlobalVector(Rim, new Vector4(RimM, RimMinPx, HasRim(v) ? RimOpacity : 0f, RimValue));
            Shader.SetGlobalVector(Dash, new Vector4(NeutralDashM, NeutralDashOn, NeutralDashMinPx, 0f));
        }

        static void SetStyle(int colour, int light, int shape, Style s)
        {
            var c = s.Colour.linear; var l = s.Light.linear;
            Shader.SetGlobalVector(colour, new Vector4(c.r, c.g, c.b, 1f));
            Shader.SetGlobalVector(light, new Vector4(l.r, l.g, l.b, 1f));
            Shader.SetGlobalVector(shape, new Vector4(s.BandM, s.ColourShare, s.WashReach, s.WashStrength));
        }

        static void Push()
        {
            bool drawn = toggleOn && hasLayer && texture != null;
            Shader.SetGlobalVector(Borders, new Vector4(Variant == BorderVariant.Legacy ? LegacyMinBandPx : Strong(Variant) ? StrongMinColourPx : MinColourPx, MaxShareOfInradius, noEarlyOut ? 1f : 0f, drawn ? 1f : 0f));
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
