// #2877: the ground of a plowed field or a drained swamp (SiteGroundLayer): one patch draped on the terrain over the baked
// ground, in parcels. The mesh gives the site's own axes in metres (uv: x along the furrows, y across), the look (color.r: 0
// field, 1 drained swamp) and the mask (color.a: 0 at the hex's edge, on water and on a town). Each parcel shows only where the
// mask passes its own hashed threshold, so the patch ends along parcels, not along a hexagon. The furrows and the tracks are
// box-filtered to the pixel (Route.shader's PulseBox), so they fade to their mean instead of shimmering as the camera pulls
// back. Lit like the ground beside it, as Route.shader: the sun with its shadows plus the ambient probe, then the hex grid and
// the land haze of StatecraftMap.hlsl. Drawn before the roads and the railways (the Transparent queue less ten), so they cross
// it on top. All colours and sizes are design values: no field layout or crop is sourced.
Shader "Statecraft/SiteGround"
{
    Properties
    {
        _Opacity ("Opacity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SiteGround"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -4
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // The main light's shadows, as the terrain and Route.shader have them.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Opacity;
            CBUFFER_END

            // The rows' width and the parcels' mean length along a row (m): about 20 by 40 px at the gameplay zoom.
            static const float RowM = 900.0, ParcelM = 1700.0;
            static const float FurrowM = 130.0;         // a furrow pair: three pixels at the gameplay zoom, sharp up close
            static const float TrackDuty = 0.05;        // the share of a row or parcel the pale track between them takes
            static const float MaskSoft = 0.06;
            static const float3 PloughedSrgb = float3(0.46, 0.33, 0.22), PloughedDarkSrgb = float3(0.33, 0.23, 0.15);
            static const float3 YoungSrgb = float3(0.42, 0.53, 0.22), YoungDarkSrgb = float3(0.33, 0.45, 0.17);
            static const float3 StubbleSrgb = float3(0.78, 0.65, 0.34), StubbleDarkSrgb = float3(0.69, 0.56, 0.27);
            static const float3 PeatSrgb = float3(0.25, 0.19, 0.14), PeatDarkSrgb = float3(0.18, 0.13, 0.10);
            static const float3 TrackSrgb = float3(0.72, 0.63, 0.47);
            static const float3 CanalSrgb = float3(0.16, 0.24, 0.26);
            static const float CanalM = 2600.0, CanalDuty = 0.05;   // the drained swamp's straight canals, about 6 px wide at the gameplay zoom

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float2 look : TEXCOORD3; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                o.look = float2(input.color.r, input.color.a);
                return o;
            }

            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            // Integral of a pulse train (1 for the first `duty` of each `period`, 0 after), for its box filter (Route.shader).
            float PulseIntegral(float x, float period, float duty)
            {
                float k = floor(x / period);
                return k * duty * period + min(x - k * period, duty * period);
            }

            // Share of a pixel `w` wide (same units) centred at x covered by the pulse train.
            float PulseBox(float x, float w, float period, float duty)
            {
                w = max(w, 1e-3);
                return (PulseIntegral(x + 0.5 * w, period, duty) - PulseIntegral(x - 0.5 * w, period, duty)) / w;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float wx = max(fwidth(uv.x), 1e-3), wy = max(fwidth(uv.y), 1e-3);
                float2 mapM = input.positionWS.xz - _ScMapOrigin.xz;
                float grid = ScGridAmount(mapM, input.positionWS);   // uniform control flow (it takes derivatives)

                float row = floor(uv.y / RowM);
                float x = uv.x + Hash(float2(row, 3.0)) * ParcelM;   // each row's parcels break at their own places
                float cell = floor(x / ParcelM);
                float id = Hash(float2(cell, row));
                float show = Hash(float2(row, cell) + 41.0);
                float furrows = PulseBox(uv.y, wy, FurrowM, 0.5);

                bool drained = input.look.x > 0.5;
                float3 lightC, darkC;
                if (drained)
                {
                    lightC = SRGBToLinear(id < 0.72 ? PeatSrgb : YoungSrgb);
                    darkC = SRGBToLinear(id < 0.72 ? PeatDarkSrgb : YoungDarkSrgb);
                }
                else
                {
                    lightC = SRGBToLinear(id < 0.6 ? PloughedSrgb : id < 0.82 ? YoungSrgb : StubbleSrgb);
                    darkC = SRGBToLinear(id < 0.6 ? PloughedDarkSrgb : id < 0.82 ? YoungDarkSrgb : StubbleDarkSrgb);
                }
                float3 albedo = lerp(darkC, lightC, furrows);
                // The tracks: the row's and the parcel's first share. A pale line between the parcels.
                float track = saturate(PulseBox(uv.y, wy, RowM, TrackDuty) + PulseBox(x, wx, ParcelM, TrackDuty * 0.6));
                albedo = lerp(albedo, SRGBToLinear(TrackSrgb), track);
                if (drained)
                {
                    float canal = saturate(PulseBox(uv.x, wx, CanalM, CanalDuty));
                    albedo = lerp(albedo, SRGBToLinear(CanalSrgb), canal);
                }
                albedo = lerp(albedo, SRGBToLinear(ScGridLandSrgb), grid * _ScGrid.x);

                float threshold = 0.15 + 0.7 * show;
                float alpha = smoothstep(threshold - MaskSoft, threshold + MaskSoft, input.look.y);

                float3 n = normalize(input.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lit = albedo * (sun.color * saturate(dot(n, sun.direction)) * sun.shadowAttenuation * sun.distanceAttenuation + SampleSH(n));
                float h = _ScHaze.a * ScHazeAmount(input.positionWS);
                return half4(lerp(lit, _ScHaze.rgb, h), alpha * _Opacity);
            }
            ENDHLSL
        }
    }
}
