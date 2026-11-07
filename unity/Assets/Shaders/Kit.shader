// Settlement kits and trees (art spike, issues #31 #32 #34 #37; atlas, #2428). Vertex colour is the sRGB albedo; its alpha
// picks the surface (KitMesh.Facade): a layer of the one shared atlas (_KitAtlas, Art/Kits/kit_atlas.png as a 2D array), which
// multiplies the vertex colour, at uv0 (cells; a facade's v is storeys). So storeys, tile rows and courses keep real
// proportions on symbolic-size buildings, and the atlas's mipmaps fade them to their mean instead of shimmering when a
// storey is a pixel or two. The atlas is albedo only: this shader does all the light. Lit like the terrain (the sun with its shadows, the trilight ambient through SH; in shade
// the ambient also takes a sun-warm bounce, KitBounce, so a face keeps its own colour) and
// hazed with the terrain's own ScHazeAmount, so kits sit in the same air as the ground. GPU instancing for the trees, with a
// per-instance tint hashed from the instance's origin (no per-instance buffer: WebGL 2 has instanced arrays only).
// Shadows use the sun's own URP bias, unchanged (ApplyShadowBias): KitTests.AKitShadowStartsAtItsWalls finds no gap at
// the walls with it and the hero renders show no acne (#37, review I3), so the kits carry no bias of their own.
Shader "Statecraft/Kit"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Jitter ("Per-instance tint jitter", Range(0, 0.5)) = 0
        [NoScaleOffset] _KitAtlas ("Town kit atlas (one Texture2DArray)", 2DArray) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Jitter;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Kit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2; float2 uv : TEXCOORD3; half tint : TEXCOORD4;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color; o.uv = v.uv;
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                o.tint = ScHash(origin.xz * 0.0137 + 3.1);
                return o;
            }

            // How much of the ambient in full shade is the sun's bounce (kit shadow colour, refs #32).
            static const half KitBounce = 0.6;

            TEXTURE2D_ARRAY(_KitAtlas); SAMPLER(sampler_KitAtlas);

            // The surfaces up to this one are facades: one ground storey under three that repeat (make_kit_atlas.py).
            static const float KitFacades = 5.0, KitFacadeRows = 4.0;

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = SafeNormalize(i.normalWS);   // never NaN, even for a degenerate face
                half3 albedo = SRGBToLinear(i.color.rgb) * _BaseColor.rgb;
                // Per-instance (tree) tint: brighter or darker, a touch warmer or cooler.
                float t = i.tint - 0.5;
                albedo *= 1.0 + _Jitter * t * 2.0;
                albedo.r *= 1.0 + _Jitter * (frac(i.tint * 7.31) - 0.5);
                float surface = round(i.color.a * 15.0);   // KitMesh.Facade: the atlas layer + 1, or 0 for no cell
                // Outside the branch: derivatives need uniform control flow.
                float2 dx = ddx(i.uv), dy = ddy(i.uv);
                if (surface > 0.5)
                {
                    float2 uv = i.uv;
                    if (surface < KitFacades + 0.5)
                    {
                        // Storeys up the wall to rows of the cell: the ground storey once, then the upper three over and over; a
                        // wall's buried part stays on the plinth band at the cell's foot.
                        float storeys = uv.y;
                        float row = storeys < 0.0 ? 0.01 : storeys < 1.0 ? storeys : 1.0 + fmod(storeys - 1.0, KitFacadeRows - 1.0);
                        uv.y = row / KitFacadeRows;
                        dx.y /= KitFacadeRows; dy.y /= KitFacadeRows;
                        // Contact darkening in the bottom real 3 m of a wall.
                        albedo *= lerp(0.72, 1.0, saturate(storeys * 3.3 / 3.0));
                    }
                    albedo *= SAMPLE_TEXTURE2D_ARRAY_GRAD(_KitAtlas, sampler_KitAtlas, uv, surface - 1.0, dx, dy).rgb;
                }
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl = saturate(dot(n, sun.direction));
                // The ambient takes the sun's warmth where the sun does not reach (the bounce off the sunlit ground; same
                // brightness, the trilight's luminance): with the trilight's own colour, near grey at a wall and sky blue on
                // a roof, a pale face in shade had almost no colour left, and the grade's teal shadows turned it blue-grey.
                float sunlit = ndl * sun.shadowAttenuation;
                half3 sh = SampleSH(n);
                half3 bounce = Luminance(sh) * sun.color / max(Luminance(sun.color), 1e-4);
                half3 lit = albedo * (lerp(sh, bounce, KitBounce * (1.0 - sunlit)) + sun.color * sunlit);
                float h = _ScHaze.a * ScHazeAmount(i.positionWS);
                return half4(lerp(lit, _ScHaze.rgb, h), 1.0);
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 Vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection)));
            }
            half4 Frag() : SV_Target { return 0; }
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
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 Vert(Attributes v) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(v); return TransformObjectToHClip(v.positionOS.xyz); }
            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
