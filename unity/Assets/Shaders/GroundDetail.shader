// Ground detail between the towns (#2878): a multiply pass drawn straight after the terrain, under everything else, so the
// land stops reading as smooth colour at the gameplay zooms. It is a pass of its own, not more code in
// StatecraftTerrain.hlsl, because that function sits at the frame-time cliff docs/visual-v1.md records (#391): a few lines
// added there cost 4-8 ms at gameplay03. The mesh (GroundDetail.BuildMesh) is a coarse copy of the relief. The scrub grows by
// real data: StatecraftGroundDetail.hlsl, which GroundScrub.shader's standing bushes share. What it draws comes from one tiling texture
// (GroundDetail.BuildTexture): r soil grain, g bushes as a height field (a lower threshold shows more of them), b their
// shadows, a pale stones. Blend DstColor SrcColor doubles the source, so 0.5 leaves the ground as it is, and every term
// fades to 0.5 by the pixel's footprint, before a tile could show or a dot could shimmer.
// Furrows (#2917, SC_FURROWS, the "furrows" toggle): fine lines along the land bake's own rows, so they lie in its parcels.
// The bake's parcels hang off a 64-bit hash no shader can repeat, so tools/land_bake.py writes each farm block's angle and
// strip offset (fieldblocks.png) and each texel's furrow strength (furrows.png); ScFieldRows rebuilds the rest exactly.
Shader "Statecraft/GroundDetail"
{
    Properties
    {
        _GrainGain ("Soil grain strength", Range(0, 1)) = 0.32
        _BushLight ("A bush's light parts, times the ground", Color) = (0.80, 0.80, 0.68, 1)
        _BushDark ("A bush's dark parts, times the ground", Color) = (0.56, 0.58, 0.47, 1)
        _ShadowDark ("Bush shadow, times the ground", Range(0, 1)) = 0.84
        _StoneGain ("Stones, brighter than the ground", Range(0, 1)) = 0.11
        _FurrowGain ("Furrows, darker and lighter than the ground", Range(0, 1)) = 0.30
        _FurrowM ("Furrow spacing (m)", Float) = 240
        _FurrowTex ("Furrow strength (land_bake.py)", 2D) = "black" {}
        _FieldBlockTex ("Farm blocks: angle, strip offset (land_bake.py)", 2D) = "black" {}
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
            #pragma multi_compile_local_fragment _ SC_FURROWS   // both variants ship: the toggle prices the furrows, not a zero gain
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"   // the map's globals, ScHexAxial and ScRegionAt; only what is called is compiled
            #include "Assets/Shaders/StatecraftGroundDetail.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _GrainGain, _ShadowDark, _StoneGain, _FurrowGain, _FurrowM;
                half4 _BushLight, _BushDark;
                float4 _FurrowTex_ST, _FieldBlockTex_ST, _FieldBlockTex_TexelSize;
            CBUFFER_END
            TEXTURE2D(_FurrowTex); SAMPLER(sampler_FurrowTex);
            TEXTURE2D(_FieldBlockTex);

            // tools/land_bake.py's farm blocks: BLOCK_M, STRIP_M, BLOCK_PAD and block_angle's range (ANGLE_MIN, ANGLE_SPAN).
            static const float ScBlockM = 5200.0, ScStripM = 1000.0, ScAngleMin = -0.35, ScAngleSpan = 0.7 + 1.5707963;
            static const int ScBlockPad = 1;
            // How far in from a strip's or a block's edge (m) the furrows start and are full: past the bake's grassy rims,
            // hedges and farm tracks (smoothstep(30, 100) and 70-75 m either side of an edge).
            static const float2 ScFurrowOffEdge = float2(90.0, 150.0);

            // A map point's place in the bake's parcels (land_bake.py field_layout): x its distance across the strips from
            // its block's centre (m: the bake's lx, along which its rows repeat), y its distance to the nearest strip or
            // block edge (m). The wobble is land_bake.py's block_wobble; the block's angle and strip offset are its texel.
            float2 ScFieldRows(float2 p)
            {
                float2 bp = p / ScBlockM + 0.275 * float2(sin(p.y / 4100.0 + sin(p.x / 6700.0)), sin(p.x / 3900.0 + sin(p.y / 5300.0)));
                float2 bi = floor(bp), bf = bp - bi;
                int2 texel = clamp(int2(bi) + ScBlockPad, int2(0, 0), int2(_FieldBlockTex_TexelSize.zw) - 1);
                float4 b = round(255.0 * LOAD_TEXTURE2D(_FieldBlockTex, texel));
                float ang = ScAngleMin + (b.r * 256.0 + b.g) / 65535.0 * ScAngleSpan;
                float2 q = (bf - 0.5) * ScBlockM;
                float lx = cos(ang) * q.x - sin(ang) * q.y;
                float s = frac(lx / ScStripM + (b.b * 256.0 + b.a) / 65536.0);
                float2 e = min(bf, 1.0 - bf);
                return float2(lx, min(min(s, 1.0 - s) * ScStripM, min(e.x, e.y) * ScBlockM));
            }

            // One set of furrows `spacing` apart: -1 in the furrow, +1 on the ridge, mean 0 (an odd curve of a triangle
            // wave), so the land keeps its brightness. Softer as the lines near the pixel, gone under 3 pixels apart.
            half ScFurrowLines(float lx, float spacing, float px)
            {
                float t = abs(frac(lx / spacing) - 0.5) * 4.0 - 1.0;
                float k = max(0.35, 4.0 * px / spacing);
                return (2.0 * smoothstep(-k, k, t) - 1.0) * (1.0 - smoothstep(spacing / 5.0, spacing / 3.0, px));
            }

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
            #ifdef SC_FURROWS
                // Before the clip, which would leave fwidth undefined. px: metres a pixel across the furrows; it leaps where
                // a block edge cuts lx, and the furrows have faded out there already.
                float2 rows = ScFieldRows(i.mapM);
                float px = fwidth(rows.x);
            #endif
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
            #ifdef SC_FURROWS
                // Two sets: one 6 pixels apart at the gameplay pose, a finer at a third of that, which fades in closer; on the
                // crop's strength (the bake's).
                half furrow = SAMPLE_TEXTURE2D(_FurrowTex, sampler_FurrowTex, uv).r * smoothstep(ScFurrowOffEdge.x, ScFurrowOffEdge.y, rows.y);
                c *= 1.0 + _FurrowGain * furrow * (ScFurrowLines(rows.x, _FurrowM, px) + 0.6 * ScFurrowLines(rows.x, _FurrowM / 3.0, px));
            #endif
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
