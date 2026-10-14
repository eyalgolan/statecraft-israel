// The diorama's frame round the map (issue #36, promo 02; MapBuilder.BuildFrame): the board's cut earth, the walnut frame
// and the dark table it stands on. Vertex colour is the sRGB albedo and its alpha the surface (MapBuilder.FrameKind: 0 earth,
// 0.5 frame, 1 table); uv0 is (along the side, out from the frame's outer edge) in metres. The grain is procedural and fades
// to its mean once it is under about a pixel, so the overview shows no shimmer. Lit like the kits and the terrain (the sun
// with its shadows, the trilight ambient through SH) and hazed with the terrain's own ScHazeAmount, so the frame sits in the
// same air as the map. Casts no shadow (MapScene turns it off), like the border skirt it replaces.
Shader "Statecraft/MapFrame"
{
    Properties
    {
        _Grain ("Grain strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half _Grain;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "MapFrame"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2; float2 uv : TEXCOORD3;
            };

            // The frame's contact shadow on the table and the table's light falloff (m out from the frame: uv.y).
            static const float ContactM = 8000.0, FalloffM = 160000.0;
            // The table's planks (m wide, running north-south) and the grain's streaks (m across, m along).
            static const float PlankM = 9000.0;
            static const float TableHaze = 0.4;

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color; o.uv = v.uv;
                return o;
            }

            // Streaky noise along `along`, fine across `across` (both in cells), faded to its mean (0.5) as a cell shrinks
            // under about a pixel (fw: the pixel's footprint in cells across).
            float Streaks(float across, float along, float fw)
            {
                return lerp(0.5, ScNoise(float2(across, along)), saturate(1.5 - fw));
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = SafeNormalize(i.normalWS);
                half3 albedo = SRGBToLinear(i.color.rgb);
                float kind = i.color.a;
                float2 mapM = i.positionWS.xz - _ScMapOrigin.xz;
                // Every derivative in uniform control flow.
                float fwX = fwidth(mapM.x), fwUv = fwidth(i.uv.y), fwY = fwidth(i.positionWS.y);
                float shade = 1.0;
                if (kind > 0.75)
                {
                    // The table: planks running north-south with a faint seam, each a shade of its own, and a grain along them.
                    float plank = floor(mapM.x / PlankM);
                    shade *= 1.0 + 0.16 * (ScHash(float2(plank, 7.3)) - 0.5);
                    shade *= 1.0 + 0.30 * _Grain * (Streaks(mapM.x / 450.0, mapM.y / 14000.0 + plank * 3.1, fwX / 450.0) - 0.5);
                    float seam = frac(mapM.x / PlankM);
                    shade *= 1.0 - 0.35 * saturate(1.5 - fwX / 120.0) * (1.0 - smoothstep(0.0, 0.012, min(seam, 1.0 - seam)));
                    // The frame's contact shadow, and the light falling off away from the board, as under a lamp over it.
                    shade *= lerp(0.45, 1.0, smoothstep(0.0, 1.0, saturate(i.uv.y / ContactM)));
                    shade *= lerp(1.0, 0.55, saturate(i.uv.y / FalloffM));
                }
                else if (kind > 0.25)
                {
                    // The frame: grain along the side, fine across it.
                    shade *= 1.0 + 0.28 * _Grain * (Streaks(i.uv.y / 160.0, i.uv.x / 9000.0, fwUv / 160.0) - 0.5);
                }
                else
                {
                    // The board's cut earth: layers by height, wavering a little along the edge.
                    float layer = i.positionWS.y / 260.0 + 0.8 * ScNoise(float2(i.uv.x / 6000.0, 1.7));
                    shade *= 1.0 + 0.22 * _Grain * (Streaks(layer, 0.37, fwY / 260.0) - 0.5);
                }
                albedo *= shade;
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl = saturate(dot(n, sun.direction));
                half3 lit = albedo * (SampleSH(n) + sun.color * ndl * sun.shadowAttenuation);
                // The haze is the land's aerial haze, read at sea level or higher: the table lies at world y 0 (the Dead Sea's
                // floor) and the frame's outer face runs down to it, and ScHazeAmount's rift boost (up to 1.6x below the
                // rift's top) belongs to the Jordan rift, not to furniture past the map's edge (review S1). The table then
                // takes TableHaze of that: it lies under the board's lamp, dark as in promo 02.
                float3 hazeAt = float3(i.positionWS.x, max(i.positionWS.y, _ScMap.z), i.positionWS.z);
                float h = _ScHaze.a * ScHazeAmount(hazeAt) * (kind > 0.75 ? TableHaze : 1.0);
                return half4(lerp(lit, _ScHaze.rgb, h), 1.0);
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
            struct Attributes { float4 positionOS : POSITION; };
            float4 Vert(Attributes v) : SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
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
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0.0); }
            ENDHLSL
        }
    }
}
