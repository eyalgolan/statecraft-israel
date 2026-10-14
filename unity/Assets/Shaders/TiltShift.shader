// Two-band tilt-shift (plan Task 3). Pass 0 blurs horizontally from the camera colour into a half-resolution target,
// pass 1 blurs that vertically, pass 2 composites it over the camera colour with the band weight as alpha. The radius at a
// pixel is TiltWeight(y) * _ScTiltShift.w of the picture's height, so the focus band stays sharp and the blur grows
// smoothly towards the top and bottom. The radius is a share of the picture's height (UV units). No depth texture: it runs on
// the web tier. The far (top) band is scaled by _ScTiltShiftFar in both its radius and its blend, so at the gameplay zoom
// the far towns stay readable (issue #390, the owner's ruling) while the overview keeps the far band in full. The near
// (bottom) band is scaled by _ScTiltShiftNear the same way: gentle at the gameplay zoom, about none at the overview (the
// owner's ruling of 2026-10-14, the near band as on promo 03).
Shader "Statecraft/TiltShift"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _ScTiltShift;   // x focus centre (viewport y, 0 = bottom), y sharp half-width, z feather, w max radius (share of height)
        float _ScTiltShiftFar; // the far (top) band's strength, 0..1: scales its radius and its blend (TiltShift.FarStrength)
        float _ScTiltShiftNear; // the near (bottom) band's strength, 0..1: scales its radius and its blend (TiltShift.NearStrength)

        float TiltWeight(float y)
        {
            return smoothstep(_ScTiltShift.y, _ScTiltShift.y + _ScTiltShift.z, abs(y - _ScTiltShift.x));
        }

        // The band's own strength: _ScTiltShiftNear below the focus (the near band), _ScTiltShiftFar above it. Continuous:
        // TiltWeight is 0 within the sharp half-width either side of the focus.
        float BandStrength(float y) { return y > _ScTiltShift.x ? _ScTiltShiftFar : _ScTiltShiftNear; }

        half3 Sample(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb; }

        // 9-tap Gaussian (sigma = 2 taps) whose tap spacing is a quarter of the local radius.
        half3 Blur(float2 uv, float2 dir)
        {
            float r = TiltWeight(uv.y) * BandStrength(uv.y) * _ScTiltShift.w;
            // r is a share of the picture's height; the horizontal step converts it with the aspect ratio, which the render
            // scale does not change (review I4).
            float2 s = dir * (r * 0.25) * float2(_ScreenParams.y / _ScreenParams.x, 1.0);
            half3 c = Sample(uv) * 0.2270270;
            c += (Sample(uv + s) + Sample(uv - s)) * 0.1945946;
            c += (Sample(uv + 2.0 * s) + Sample(uv - 2.0 * s)) * 0.1216216;
            c += (Sample(uv + 3.0 * s) + Sample(uv - 3.0 * s)) * 0.0540541;
            c += (Sample(uv + 4.0 * s) + Sample(uv - 4.0 * s)) * 0.0162162;
            return c;
        }
        ENDHLSL

        Pass
        {
            Name "TiltShiftHorizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return half4(Blur(i.texcoord, float2(1, 0)), 1); }
            ENDHLSL
        }
        Pass
        {
            Name "TiltShiftVertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return half4(Blur(i.texcoord, float2(0, 1)), 1); }
            ENDHLSL
        }
        Pass
        {
            Name "TiltShiftComposite"
            Blend SrcAlpha OneMinusSrcAlpha, Zero One   // colour by the band weight; the target's alpha is kept
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return half4(Sample(i.texcoord), TiltWeight(i.texcoord.y) * BandStrength(i.texcoord.y)); }
            ENDHLSL
        }
    }
}
