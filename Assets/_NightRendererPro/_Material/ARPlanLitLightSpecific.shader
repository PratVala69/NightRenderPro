Shader "Custom/AR Targeted Light Receiver (Additive)"
{
    SubShader
    {
        Tags { 
            "RenderPipeline"="UniversalPipeline" 
            "RenderType"="Transparent" 
            "Queue"="Transparent+1" 
        }

        Pass
        {
            Name "TargetedAdditive"
            Tags { "LightMode" = "UniversalForward" }
            
            // CRITICAL: Additive Blending (Final Color = SrcColor + DstColor)
            Blend One One 
            Cull Back
            ZWrite Off 
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // --- CBUFFER: Custom Light Data (Set by C# Script) ---
            // These properties receive the data from TargetedLightController.cs
            CBUFFER_START(CustomLightData)
                half4 _TargetLightColor;     
                half4 _TargetLightPosition;
                half4 _TargetLightDirection; 
                float _TargetLightAttenuation; // Distance falloff factor
            CBUFFER_END
            
            // --- STRUCTS (Same as before) ---
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            // --- VERTEX SHADER (Standard) ---
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                return output;
            }

            // --- FRAGMENT SHADER ---
            half4 frag(Varyings input) : SV_Target
            {
                half3 finalIllumination = 0;
                input.normalWS = normalize(input.normalWS);

                // 1. Calculate light direction (from surface to light)
                half3 lightDirection = normalize(_TargetLightPosition.xyz - input.positionWS);

                // 2. Calculate the diffuse light contribution (Lambertian)
                // saturate(dot(normal, lightDirection)) gives the intensity based on angle
                half3 lightContribution = _TargetLightColor.rgb * saturate(dot(input.normalWS, lightDirection));

                // 3. Apply the Attenuation Factor (distance falloff calculated in C#)
                finalIllumination += lightContribution * _TargetLightAttenuation;
                
                // FINAL OUTPUT (Additive)
                return half4(finalIllumination, 1.0);
            }
            ENDHLSL
        }
    }
}