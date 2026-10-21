Shader "Custom/AR Light Receiver (Additive)"
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
            Name "ForwardLitAdditive"
            Tags { "LightMode" = "UniversalForward" }
            
            // CRITICAL: Additive Blending (Final Color = SrcColor + DstColor)
            // SrcColor is our calculated light color. DstColor is the camera feed.
            Blend One One 
            Cull Back
            ZWrite On // Do not write to depth
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag
            // Only need additional lights defined, as that's where Point/Spot lights are
            #pragma multi_compile _ _ADDITIONAL_LIGHTS 

            // Include URP's core libraries for lighting
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // --- STRUCTS (Same as Shadow Catcher) ---
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

            // --- VERTEX SHADER ---
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
                // A. CALCULATE LIGHTING CONTRIBUTION (Excluding Shadows)
                half3 finalIllumination = 0;
                input.normalWS = normalize(input.normalWS);

                // --- 1. Calculate lighting from Additional Lights (Spot/Point) ---
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, input.positionWS);
                    
                    // LightingLambert calculates the light contribution (diffuse)
                    half3 lightContribution = LightingLambert(additionalLight.color, additionalLight.direction, input.normalWS);
                    
                    // CRITICAL: We only use the light contribution multiplied by distance attenuation.
                    // We EXCLUDE additionalLight.shadowAttenuation here.
                    finalIllumination += lightContribution * additionalLight.distanceAttenuation;
                }
                
                // --- 2. Calculate lighting from Main Light (Optional, but good practice) ---
                // We typically exclude the Main Light (Sun/Ambient) from additive receivers
                // unless we want it to also brighten the floor, which can look unnatural.
                // If you want it, uncomment and use LightingLambert(MainLight.color, ...):
                /*
                Light mainLight = GetMainLight(input.positionWS);
                half3 mainLightContribution = LightingLambert(mainLight.color, mainLight.direction, input.normalWS);
                finalIllumination += mainLightContribution;
                */

                // B. FINAL OUTPUT (Additive)
                // The final output color is the pure calculated illumination.
                // The Blend One One mode will ADD this color to the camera feed.
                return half4(finalIllumination, 1.0);
            }
            ENDHLSL
        }
    }
}