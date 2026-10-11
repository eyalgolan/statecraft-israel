// The border skirt's sea past the map's edge (plan Task 7): the same water as the terrain's own sea, from the same function
// (StatecraftMap.hlsl ScWaterShade) and coast texture (clamped at the edge), with the same water haze, so no seam shows. Unlit:
// the function lights itself, as the terrain graph's water (emission only) does. DepthOnly and DepthNormals passes keep it
// in the depth prepass on tiers that make one.
Shader "Statecraft/Sea"
{
    Properties
    {
        [NoScaleOffset] _ScCoastTex ("Coast distance (R)", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float _ScSeaUnused;   // the SRP Batcher needs a UnityPerMaterial buffer; the sea has no per-material numbers
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; };
        ENDHLSL

        Pass
        {
            Name "Sea"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // The main light's shadows, as the terrain graph's forward pass has them (ScWaterShade samples them).
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"
            TEXTURE2D(_ScCoastTex); SAMPLER(sampler_ScCoastTex);
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 mapM = input.positionWS.xz - _ScMapOrigin.xz;
                float coast = SAMPLE_TEXTURE2D(_ScCoastTex, sampler_ScCoastTex, saturate(mapM / _ScMap.xy)).r;
                float footprintM = max(length(ddx(mapM)), length(ddy(mapM)));   // as the terrain's water takes it
                float3 c = ScHazeWater(ScWaterShade(coast, 0.0, input.positionWS, mapM, footprintM, ScBreakers(coast, mapM)), input.positionWS);
                // The hex lattice runs on past the map's edge, exactly as on the terrain's water (review I3).
                c = lerp(c, SRGBToLinear(ScGridWaterSrgb), ScGridAmount(mapM, input.positionWS) * _ScGrid.y);
                return half4(c, 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Vert(Attributes input) : SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Vert(Attributes input) : SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half4 Frag() : SV_Target { return half4(0.0, 1.0, 0.0, 0.0); }   // the sea faces up
            ENDHLSL
        }
    }
}
