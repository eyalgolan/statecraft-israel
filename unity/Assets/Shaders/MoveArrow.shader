// The move arrow (#2134, MapScene.ShowMoveArrow): an animated arrow from a selected Israeli unit's hex through every hex
// of the sim's path to the hovered one. The mesh is the path's centre line, two vertices a cross-section (u 0 and 1, v
// metres along) carrying the section's sideways direction (a mitre at a bend) and the path's length; the vertex shader
// widens it in proportion to the camera's distance (_WidthPerDistance), so the arrow keeps the same size on screen at
// every zoom. The fragment shader draws a dark-edged shaft whose gold chevrons flow towards the target with _Time (no
// script runs per frame) and a solid arrowhead on the target hex's centre. Unlit and drawn over the ground, the tokens and
// the town models (ZTest Always): an order preview must never hide behind a ridge or a house.
Shader "Statecraft/MoveArrow"
{
    Properties
    {
        _WidthPerDistance ("Width (m) per metre of camera distance", Float) = 0.016
        _Speed ("Chevrons per second", Float) = 1.2
        [MainColor] _BaseColor ("Fill", Color) = (1, 0.82, 0.25, 1)
        _InkColor ("Ink", Color) = (0.06, 0.08, 0.14, 0.9)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
            CBUFFER_START(UnityPerMaterial)
                float _WidthPerDistance;
                float _Speed;
                half4 _BaseColor;
                half4 _InkColor;
            CBUFFER_END

            // Shares of the arrow's full width (the arrowhead's base): the shaft's half-width, the ink edge; the head's
            // length; a chevron's period along the shaft and how far its arms lean back.
            static const float ShaftHalf = 0.17, Ink = 0.07, HeadLength = 1.25, Period = 1.4, Lean = 0.9;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 side : TEXCOORD1; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float across : TEXCOORD0;   // -0.5 .. 0.5 of the width
                float along : TEXCOORD1;    // metres from the arrow's start
                float width : TEXCOORD2;    // metres
                float total : TEXCOORD3;    // the path's length, metres
            };

            Varyings Vert(Attributes input)
            {
                float3 centre = TransformObjectToWorld(input.positionOS.xyz);
                float width = _WidthPerDistance * distance(GetCameraPositionWS(), centre);
                float across = input.uv.x - 0.5;
                float3 side = TransformObjectToWorldDir(float3(input.side.x, 0, input.side.y), false);
                Varyings o;
                o.positionCS = TransformWorldToHClip(centre + side * (across * width));
                o.across = across; o.along = input.uv.y; o.width = width; o.total = input.side.z;
                return o;
            }

            // Coverage of a signed distance (negative inside), box-filtered to the pixel.
            float Cover(float d) { return saturate(0.5 - d / max(fwidth(d), 1e-5)); }

            half4 Frag(Varyings input) : SV_Target
            {
                float a = abs(input.across);
                float w = max(input.width, 1e-3);
                float toTip = (input.total - input.along) / w;   // in widths
                // The arrowhead: a triangle from the full width at HeadLength before the tip to a point at the tip. The shaft:
                // ShaftHalf either side of the line, up to the head's base. Distances in widths, negative inside.
                float head = max(a - 0.5 * toTip / HeadLength, toTip - HeadLength);
                float shaft = max(a - ShaftHalf, HeadLength - toTip);
                float d = min(head, shaft);
                float shape = Cover(d - Ink);   // the whole arrow, ink edge included
                float fill = Cover(d);
                // The chevrons: bands whose arms trail behind their point on the line, flowing towards the tip; on the shaft only, the head solid.
                float phase = frac((input.along / w + a * Lean) / Period - _Time.y * _Speed);
                float chevron = Cover(abs(phase - 0.25) - 0.25) * step(HeadLength, toTip);
                float bright = max(fill * chevron, fill * Cover(head));
                half3 colour = lerp(_InkColor.rgb, _BaseColor.rgb, bright);
                // The start fades in over one width, so the arrow leaves the unit's token softly.
                float alpha = shape * lerp(_InkColor.a, _BaseColor.a, bright) * saturate(input.along / w);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
