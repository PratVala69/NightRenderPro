Shader "Custom/URP/BlackToTransparent_Color"
{
    Properties
    {
        _MainTex ("Grayscale Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)

        _Softness ("Soft / Hard Transition", Range(0.01, 1)) = 0.2
        _AlphaPower ("Alpha Power", Range(0.5, 5)) = 1
        _AlphaMultiplier ("Alpha Multiplier", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float _Softness;
            float _AlphaPower;
            float _AlphaMultiplier;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Яскравість (0 = чорний, 1 = білий)
                half luminance = dot(tex.rgb, half3(0.299, 0.587, 0.114));

                // М’який / різкий перехід
                half alpha = smoothstep(0.0, _Softness, luminance);

                // Керування різкістю
                alpha = pow(alpha, _AlphaPower);

                // Загальна прозорість
                alpha *= _AlphaMultiplier;

                // Колір береться з Color Tint
                half3 finalColor = _Color.rgb * luminance;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
