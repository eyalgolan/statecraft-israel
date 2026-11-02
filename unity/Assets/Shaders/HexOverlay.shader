// The lit hexes' fill and the hover marker (#2201, MapScene.ShowReachable and ShowHover). Unlit; each vertex carries its
// colour (the fill's translucent ivory, the hover band's gold core and navy edges), so one material draws both. Drawn over
// the ground, the tokens and the town and tree models (ZTest Always), as the move arrow is (#2134): a lit hex in a dense
// town or a wood was a thin outline the models stood over. Queued just under the arrow's Transparent+50, so the arrow stays
// on top of both. No motion. A vertex with uv.x 1 (the fill) fades to CloseShare of its alpha as the camera comes within
// CloseM of it: at the closest zoom one hex fills the screen and the full tint washed the whole view. uv.x 0 (the hover
// band) keeps its strength at every zoom.
Shader "Statecraft/HexOverlay"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+40" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            static const float CloseM = 8000.0, FarM = 40000.0, CloseShare = 0.3;
            struct Attributes { float4 positionOS : POSITION; half4 colour : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 colour : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                float far = saturate((distance(GetCameraPositionWS(), world) - CloseM) / (FarM - CloseM));
                o.colour = input.colour;
                o.colour.a *= lerp(1.0, lerp(CloseShare, 1.0, far), input.uv.x);
                return o;
            }
            half4 Frag(Varyings input) : SV_Target { return input.colour; }
            ENDHLSL
        }
    }
}
