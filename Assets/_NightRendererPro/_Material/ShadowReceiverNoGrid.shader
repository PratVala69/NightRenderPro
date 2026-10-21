Shader "Custom/ARShadowReceiver_NoGrid"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "ShadowPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _ShadowColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample main light shadow
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = input.positionWS;
                float4 shadowCoord = GetShadowCoord(vertexInput);
                half shadowAtten = MainLightRealtimeShadow(shadowCoord);

                // Dither to reduce grid (simple blue noise sim)
                float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                float dither = frac(sin(dot(screenUV, float2(12.9898, 78.233))) * 43758.5453) * 0.01;
                shadowAtten = lerp(shadowAtten, shadowAtten + dither, 0.5);  // Subtle noise

                half4 color = _ShadowColor;
                color.rgb *= (1.0 - shadowAtten);  // Darken where shadowed
                color.a = _ShadowColor.a * (1.0 - shadowAtten);
                return color;
            }
            ENDHLSL
        }
    }
}