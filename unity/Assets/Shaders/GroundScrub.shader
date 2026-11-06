// The low 3D scrub between the towns (#2878): one small mesh of bushes around the 15 km tile (GroundDetail.BuildScrubMesh),
// drawn at every tile in view, a few tile instances a frame however many bushes they hold, so the WebGL cost stays that of
// a handful of trees. The vertex shader stands each bush on the ground and decides whether it grows, from the same data as
// the painted scrub (StatecraftGroundDetail.hlsl): a bush with a rank (uv1.x) over the density shrinks to a point. The bush's
// pivot in its tile is uv0; the mesh's positions are offsets from that pivot in metres. Lit as Kit.shader lights the trees.
Shader "Statecraft/GroundScrub"
{
    Properties { _Jitter ("Tint jitter", Range(0, 0.5)) = 0.12 }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Shaders/StatecraftMap.hlsl"
        #include "Assets/Shaders/StatecraftGroundDetail.hlsl"
        TEXTURE2D(_ScDetailHeightTex); SAMPLER(sampler_ScDetailHeightTex);   // the terrain's 0..1 height, GroundDetail.BuildHeightTexture
        float4 _ScDetailScrub;   // x the terrain's height range (m), y terrain origin y, z, w the camera distance (m) where it shrinks away
        static const float ScScrubBorderFootprintM = 60.0;   // keeps a bush's reach, not a pixel, off the border's band
        CBUFFER_START(UnityPerMaterial)
            half _Jitter;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 pivot : TEXCOORD0; float2 rank : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // The bush's world position: on the ground at its pivot, scaled by its density; a point where it does not grow.
        float3 ScrubPosition(Attributes v)
        {
            float3 tile = TransformObjectToWorld(float3(0, 0, 0));
            float2 mapM = tile.xz + v.pivot - _ScMapOrigin.xz, uv = mapM / _ScMap.xy;
            float scale = 0.0;
            if (uv.x > 0.0 && uv.y > 0.0 && uv.x < 1.0 && uv.y < 1.0)
            {
                float4 land = SAMPLE_TEXTURE2D_LOD(_ScDetailLandTex, sampler_ScDetailLandTex, uv, 0.0);
                float coast = SAMPLE_TEXTURE2D_LOD(_ScDetailCoastTex, sampler_ScDetailCoastTex, uv, 0.0).r;
                half4 cls = SAMPLE_TEXTURE2D_LOD(_ScDetailClassTex, sampler_ScDetailClassTex, uv, 0.0);
                float density = ScScrubDensity(uv, mapM, cls, land, coast);
                density *= ScOffTheBeach(coast, mapM) * ScClearOfBorder(mapM, ScScrubBorderFootprintM);
                scale = step(v.rank.x * v.rank.x, density) * (0.65 + 0.35 * density);   // the rank squared: a thin scrub still shows
            }
            float height = SAMPLE_TEXTURE2D_LOD(_ScDetailHeightTex, sampler_ScDetailHeightTex, uv, 0.0).r * _ScDetailScrub.x + _ScDetailScrub.y;
            float3 pivot = float3(tile.x + v.pivot.x, height, tile.z + v.pivot.y);
            scale *= 1.0 - smoothstep(_ScDetailScrub.z, _ScDetailScrub.w, length(_WorldSpaceCameraPos - pivot));
            return pivot + v.positionOS.xyz * scale;
        }
        ENDHLSL

        Pass
        {
            Name "Scrub"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half3 color : TEXCOORD2; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionWS = ScrubPosition(v);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = v.normalOS;
                o.color = v.color.rgb;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = SafeNormalize(i.normalWS);
                half3 albedo = SRGBToLinear(i.color);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float sunlit = saturate(dot(n, sun.direction)) * sun.shadowAttenuation;
                half3 sh = SampleSH(n);
                half3 bounce = Luminance(sh) * sun.color / max(Luminance(sun.color), 1e-4);
                half3 lit = albedo * (lerp(sh, bounce, 0.6 * (1.0 - sunlit)) + sun.color * sunlit);
                return half4(lerp(lit, _ScHaze.rgb, _ScHaze.a * ScHazeAmount(i.positionWS)), 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            float3 _LightDirection;
            float4 Vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(ScrubPosition(v), v.normalOS, _LightDirection)));
            }
            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
