// The 1948 railways and roads (issue #35, RouteLayer): ribbons draped on the terrain at the promo's symbolic width. One
// shader for both looks, chosen by _Kind (a material value, so a uniform branch): 0 a pale dirt road with faint wheel ruts and patchy colour,
// 1 a railway (a ballast bed, sleepers and two rails). The mesh gives u across the ribbon (0..1) and v along it (metres),
// so the sleepers repeat at a fixed spacing; every pattern is box-filtered to the pixel (fwidth), so it fades to its mean
// instead of shimmering as the camera pulls back. Lit like the ground beside it: the sun with its shadows plus the ambient
// probe, then the hex grid and the land haze of StatecraftMap.hlsl, so the ribbons carry the terrain's relief shading and
// fade with it. Transparent with soft edges; a stencil bit lets each pixel take one ribbon core only, so crossings and
// joins never double-blend (the rail material draws first, so a railway stays on top of a road it crosses).
Shader "Statecraft/Route"
{
    Properties
    {
        _Kind ("Kind (0 road, 1 rail)", Float) = 0
        _Opacity ("Opacity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Shaders/StatecraftMap.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _Kind;
            float _Opacity;
        CBUFFER_END

        // Colours (sRGB) after the promo's gameplay frame (promo/final/03): a wide, pale packed-dirt track with soft,
        // slightly darker worn verges, as the promo's tracks read; a cool grey stone ballast bed with dark timber sleepers and
        // bright steel rails (promo video 0:21).
        // The road: pale packed dirt (promo 03's tracks render about (0.97, 0.84, 0.74) after its grade), varied between a
        // cream and a warmer, darker dust in patches hundreds of metres long (RoadPatchM: a few pixels at the gameplay zoom,
        // so the variation never shimmers), so a long road does not read as one flat painted stripe.
        static const float3 RoadSrgb = float3(0.98, 0.92, 0.80);
        static const float3 RoadDustSrgb = float3(0.92, 0.83, 0.68);
        static const float RoadPatchM = 900.0;
        static const float3 RutSrgb = float3(0.82, 0.73, 0.59);
        static const float3 VergeSrgb = float3(0.46, 0.39, 0.29);
        static const float3 BallastSrgb = float3(0.56, 0.54, 0.50);
        static const float3 BankSrgb = float3(0.44, 0.42, 0.39);     // the embankment's shoulders, darker
        static const float3 SleeperSrgb = float3(0.30, 0.22, 0.15);
        // Far off (a pixel over FarFromM, full by FarToM: the country-wide pose), pale dirt on pale ground would vanish,
        // so a road darkens to a grey-brown line, as the promo's diorama draws its roads (a symbolic far look).
        static const float3 RoadFarSrgb = float3(0.50, 0.44, 0.36);
        static const float FarFromM = 60.0, FarToM = 150.0;
        static const float3 RailSrgb = float3(0.90, 0.89, 0.86);     // bright worn steel, as the promo video's close-up
        // The railway's pattern, in metres along (v) and as a share of the ribbon across (u). Sleepers every 100 m (about
        // 3 px at the gameplay zoom: a fine texture, as the promo's, not a coarse ladder).
        static const float SleeperSpacingM = 100.0, SleeperDuty = 0.35, SleeperHalfSpan = 0.29;
        static const float RailOffset = 0.12, RailHalfWidth = 0.018;
        // A rail is drawn at least RailMinPx wide, so at the gameplay zoom (its true 0.036 of the bed is under a pixel) it
        // still reads as a bright line; the rails fade out where the two would merge (under RailFadeToPx apart).
        static const float RailMinPx = 0.8, RailFadeFromPx = 1.5, RailFadeToPx = 3.0;
        static const float RutOffset = 0.12, RutHalfWidth = 0.035, RutStrength = 0.4;
        // The verges: a narrow, softer darker worn strip either side of the dirt, where the soft edge (RoadEdge) begins, so
        // it outlines the wide pale core on pale ground without reading as a dark line of its own.
        static const float VergeOffset = 0.33, VergeHalfWidth = 0.07, VergeStrength = 0.6, RoadEdge = 0.22;

        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };

        Varyings Vert(Attributes input)
        {
            Varyings o;
            VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
            o.positionCS = p.positionCS; o.positionWS = p.positionWS;
            o.normalWS = TransformObjectToWorldNormal(input.normalOS);
            o.uv = input.uv;
            return o;
        }

        // Integral of a pulse train (1 for the first `duty` of each `period`, 0 after), for its box filter.
        float PulseIntegral(float x, float period, float duty)
        {
            float k = floor(x / period);
            return k * duty * period + min(x - k * period, duty * period);
        }

        // Share of a pixel `w` wide (same units) centred at x covered by the pulse train.
        float PulseBox(float x, float w, float period, float duty)
        {
            w = max(w, 1e-4);
            return (PulseIntegral(x + 0.5 * w, period, duty) - PulseIntegral(x - 0.5 * w, period, duty)) / w;
        }

        // Share of a pixel `w` wide covered by a band of half-width h around c (all in the same units), thinned when the
        // band is narrower than the pixel, so a line far away fades rather than staying a full pixel wide.
        float Band(float x, float c, float h, float w)
        {
            w = max(w, 1e-4);
            return saturate((h - abs(x - c)) / w + 0.5) * saturate(2.0 * h / w);
        }

        // The ribbon's colour and its coverage (before the material's opacity).
        half4 Shade(Varyings input)
        {
            float u = input.uv.x, v = input.uv.y;
            float du = max(fwidth(u), 1e-5), dv = max(fwidth(v), 1e-3);
            float2 mapM = input.positionWS.xz - _ScMapOrigin.xz;
            float grid = ScGridAmount(mapM, input.positionWS);   // uniform control flow (it takes derivatives)
            float3 albedo;
            float alpha;
            [branch] if (_Kind > 0.5)
            {
                albedo = lerp(SRGBToLinear(BankSrgb), SRGBToLinear(BallastSrgb), smoothstep(0.12, 0.3, 0.5 - abs(u - 0.5)));
                float across = Band(u, 0.5, SleeperHalfSpan, du);
                float sleeper = across * PulseBox(v, dv, SleeperSpacingM, SleeperDuty);
                albedo = lerp(albedo, SRGBToLinear(SleeperSrgb), sleeper);
                float railH = max(RailHalfWidth, 0.5 * RailMinPx * du);
                float rails = saturate(Band(u, 0.5 - RailOffset, railH, du) + Band(u, 0.5 + RailOffset, railH, du));
                rails *= smoothstep(RailFadeFromPx, RailFadeToPx, 2.0 * RailOffset / du);   // the two rails' spacing in pixels
                albedo = lerp(albedo, SRGBToLinear(RailSrgb), rails);
                float e = max(0.16, du);                  // the soft edge, at least a pixel wide (review M5)
                alpha = smoothstep(0.0, e, u) * smoothstep(1.0, 1.0 - e, u);
            }
            else
            {
                // Two octaves of value noise on the map position: low frequency, so its box filter is not needed.
                float patch = 0.65 * ScNoise(mapM / RoadPatchM) + 0.35 * ScNoise(mapM / (0.37 * RoadPatchM) + 17.0);
                albedo = lerp(SRGBToLinear(RoadSrgb), SRGBToLinear(RoadDustSrgb), smoothstep(0.3, 0.75, patch));
                float ruts = saturate(Band(u, 0.5 - RutOffset, RutHalfWidth, du) + Band(u, 0.5 + RutOffset, RutHalfWidth, du));
                albedo = lerp(albedo, SRGBToLinear(RutSrgb), RutStrength * ruts);
                float verges = saturate(Band(u, 0.5 - VergeOffset, VergeHalfWidth, du) + Band(u, 0.5 + VergeOffset, VergeHalfWidth, du));
                albedo = lerp(albedo, SRGBToLinear(VergeSrgb), VergeStrength * verges);
                albedo = lerp(albedo, SRGBToLinear(RoadFarSrgb), smoothstep(FarFromM, FarToM, dv));
                float e = max(RoadEdge, du);
                alpha = smoothstep(0.0, e, u) * smoothstep(1.0, 1.0 - e, u);
            }
            albedo = lerp(albedo, SRGBToLinear(ScGridLandSrgb), grid * _ScGrid.x);
            float3 n = normalize(input.normalWS);
            Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            float3 lit = albedo * (sun.color * saturate(dot(n, sun.direction)) * sun.shadowAttenuation * sun.distanceAttenuation + SampleSH(n));
            // A ribbon narrower than a pixel (the country-wide pose) covers only part of it: fade it by its coverage, so it
            // thins out instead of crawling (review M5).
            alpha *= saturate(1.0 / du);
            float h = _ScHaze.a * ScHazeAmount(input.positionWS);
            return half4(lerp(lit, _ScHaze.rgb, h), alpha);
        }
        ENDHLSL

        // Two passes split each ribbon at half coverage (review M3). The core (coverage 0.5 and up) writes the stencil bit,
        // so no later ribbon draws over it; the soft rim only tests the bit and never writes it, so a ribbon's faint edge
        // cannot cut a notch out of the next ribbon's core where roads join or cross the railway. URP draws both: the core
        // as UniversalForward, the rim as SRPDefaultUnlit.
        Pass
        {
            Name "RouteCore"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -4
            Stencil { Ref 32 ReadMask 32 WriteMask 32 Comp NotEqual Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // The main light's shadows, as the terrain and Sea.shader have them.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 Frag(Varyings input) : SV_Target
            {
                half4 c = Shade(input);
                clip(c.a - 0.5);
                return half4(c.rgb, c.a * _Opacity);
            }
            ENDHLSL
        }
        Pass
        {
            Name "RouteRim"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -4
            Stencil { Ref 32 ReadMask 32 Comp NotEqual }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 Frag(Varyings input) : SV_Target
            {
                half4 c = Shade(input);
                clip(0.5 - c.a);
                return half4(c.rgb, c.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
