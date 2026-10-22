Shader "Statecraft/ConstructionStone"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half3 normalWS:TEXCOORD0; half4 color:COLOR; };
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.color=v.color; return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                Light sun=GetMainLight();
                half diffuse=saturate(dot(normalize(i.normalWS),sun.direction));
                return half4(i.color.rgb * (half3(0.36,0.34,0.30) + sun.color * diffuse * 0.72),1);
            }
            ENDHLSL
        }
    }
}
