Shader "Custom/LeafShader"
{
    Properties
    {
        _MainTex ("Leaf Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
        _WindStrength ("Wind Strength", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }

        CGPROGRAM
        #pragma surface surf Standard alpha:clip

        sampler2D _MainTex;
        float _Cutoff;
        float _WindStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

            o.Albedo = c.rgb;
            o.Alpha = c.a;

            clip(o.Alpha - _Cutoff);
        }
        ENDCG
    }
}