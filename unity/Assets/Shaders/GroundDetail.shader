// Ground detail between the towns (#2878): a multiply pass drawn straight after the terrain, under everything else, so the
// land stops reading as smooth colour at the gameplay zooms. It is a pass of its own, not more code in
// StatecraftTerrain.hlsl, because that function sits at the frame-time cliff docs/visual-v1.md records (#391): a few lines
// added there cost 4-8 ms at gameplay03. The mesh (GroundDetail.BuildMesh) is a coarse copy of the relief. The scrub grows by
// real data: StatecraftGroundDetail.hlsl, which GroundScrub.shader's standing bushes share. What it draws comes from one tiling texture
// (GroundDetail.BuildTexture): r soil grain, g bushes as a height field (a lower threshold shows more of them), b their
// shadows, a pale stones. Blend DstColor SrcColor doubles the source, so 0.5 leaves the ground as it is, and every term
// fades to 0.5 by the pixel's footprint, before a tile could show or a dot could shimmer.
Shader "Statecraft/GroundDetail"
{
    Properties
    {
        _GrainGain ("Soil grain strength", Range(0, 1)) = 0.32
        _BushLight ("A bush's light parts, times the ground", Color) = (0.80, 0.80, 0.68, 1)
        _BushDark ("A bush's dark parts, times the ground", Color) = (0.56, 0.58, 0.47, 1)
        _ShadowDark ("Bush shadow, times the ground", Range(0, 1)) = 0.84
        _StoneGain ("Stones, brighter than the ground", Range(0, 1)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-1" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "GroundDetail"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest Always
            Cull Back
            Blend DstColor SrcColor
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"   // the map's globals, ScHexAxial and ScRegionAt; only what is called is compiled
            #include "Assets/Shaders/StatecraftGroundDetail.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _GrainGain, _ShadowDark, _StoneGain;
                half4 _BushLight, _BushDark;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 mapM : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(w);
                o.mapM = w.xz - _ScMapOrigin.xz;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float footprint = length(fwidth(i.mapM));
                float fade = 1.0 - smoothstep(_ScDetailFade.x, _ScDetailFade.y, footprint);
                float2 uv = i.mapM / _ScMap.xy;
                float coast = SAMPLE_TEXTURE2D(_ScDetailCoastTex, sampler_ScDetailCoastTex, uv).r;
                fade *= ScOffTheBeach(coast, i.mapM);
                fade *= ScClearOfBorder(i.mapM, footprint);
                clip(fade - 0.001);                                       // not even a rounding step on the sand and the stripes
                float4 land = SAMPLE_TEXTURE2D(_ScDetailLandTex, sampler_ScDetailLandTex, uv);
                half4 cls = SAMPLE_TEXTURE2D(_ScDetailClassTex, sampler_ScDetailClassTex, uv);
                // Two layers of one texture, turned and scaled apart, so no 12 km tile repeats on the screen.
                half4 a = SAMPLE_TEXTURE2D(_ScDetailTex, sampler_ScDetailTex, i.mapM / _ScDetailTileM);
                float2 turned = float2(0.8 * i.mapM.x - 0.6 * i.mapM.y, 0.6 * i.mapM.x + 0.8 * i.mapM.y);
                half4 b = SAMPLE_TEXTURE2D(_ScDetailTex, sampler_ScDetailTex, turned / (0.73 * _ScDetailTileM));
                half scrub = ScScrubDensity(uv, i.mapM, cls, land, coast);
                half threshold = lerp(1.05, 0.38, scrub);
                half ragged = 0.30 * (a.r - b.r);                        // the grain frays the bushes' round edges
                half height = max(a.g, b.g) + ragged;
                half bush = smoothstep(threshold, threshold + 0.10, height);
                half tone = saturate((height - threshold) / 0.45);       // each bush's own peak: tall ones darker, their edges lighter
                half shade = smoothstep(threshold, threshold + 0.16, max(a.b, b.b) + ragged) * (1.0 - bush);
                half stone = smoothstep(0.72, 0.88, a.a) * cls.a;
                half3 c = 1.0 + _GrainGain * cls.b * ((a.r + b.r) - 1.0);
                c *= 1.0 + _StoneGain * stone;
                c *= lerp(1.0, _ShadowDark, shade);
                c *= lerp(half3(1, 1, 1), lerp(_BushLight.rgb, _BushDark.rgb, tone), bush);
                return half4(lerp(half3(0.5, 0.5, 0.5), 0.5 * c, fade), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
