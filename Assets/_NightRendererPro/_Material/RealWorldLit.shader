Shader "AR/RealWorldLit"
{
   Properties
    {
        _BaseColor   ("Base Color", Color) = (1,1,1,1)
        _Metallic    ("Metallic", Range(0,1)) = 0.0
        _Smoothness  ("Smoothness", Range(0,1)) = 0.5

        // --- Light array limits ---
        [IntRange] _VirtualLightCount ("Virtual Light Count", Range(0,32)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            // -------------------------------------------------
            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float2 uv       : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 worldNormal: TEXCOORD1;
                float2 uv         : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                SHADOW_COORDS(4)
            };
            // -------------------------------------------------

            fixed4 _BaseColor;
            float _Metallic;
            float _Smoothness;
            int _VirtualLightCount;

            // Max 32 lights (mobile safe)
            #define MAX_VIRTUAL_LIGHTS 32
            float4 _VirtualLightPos[MAX_VIRTUAL_LIGHTS];      // .w = 1 for point, 0 for disabled
            fixed4 _VirtualLightColor[MAX_VIRTUAL_LIGHTS];
            float  _VirtualLightIntensity[MAX_VIRTUAL_LIGHTS];

            v2f vert (appdata v)
            {
                v2f o;
                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv          = v.uv;

                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_SHADOW(o);
                return o;
            }

            // Simple PBR helper
            float3 PBR(float3 N, float3 V, float3 L, float3 lightCol, float roughness, float metallic, float3 albedo)
            {
                float3 H = normalize(V + L);
                float NdotL = max(0, dot(N, L));
                float NdotV = max(0, dot(N, V));
                float NdotH = max(0, dot(N, H));
                float VdotH = max(0, dot(V, H));

                float3 F0 = lerp(0.04, albedo, metallic);
                float3 F  = F0 + (1 - F0) * pow(1 - VdotH, 5);
                float3 diffuse = albedo * (1 - metallic) * (1 - F);

                float spec = pow(NdotH, (1-roughness)*128 + 1);

                return diffuse * NdotL + spec * F;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                // ---- Unity directional light (real-world) ----
                float3 L_dir = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightCol = _LightColor0.rgb;
                float atten = LIGHT_ATTENUATION(i);
                float3 dirContrib = PBR(N, V, L_dir, lightCol, 1-_Smoothness, _Metallic, _BaseColor.rgb) * atten;

                // ---- Virtual point lights ----
                float3 virtContrib = 0;
                for (int idx = 0; idx < _VirtualLightCount && idx < MAX_VIRTUAL_LIGHTS; ++idx)
                {
                    if (_VirtualLightPos[idx].w <= 0) continue; // disabled

                    float3 L = _VirtualLightPos[idx].xyz - i.worldPos;
                    float dist = length(L);
                    L = normalize(L);

                    // Quadratic falloff
                    float falloff = 1.0 / (1.0 + 0.25 * dist * dist);
                    float intensity = _VirtualLightIntensity[idx];

                    float3 contrib = PBR(N, V, L, _VirtualLightColor[idx].rgb, 1-_Smoothness, _Metallic, _BaseColor.rgb)
                                   * intensity * falloff;

                    virtContrib += contrib;
                }

                float3 final = dirContrib + virtContrib;
                UNITY_APPLY_FOG(i.fogCoord, final);
                return fixed4(final, 1);
            }
            ENDCG
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #include "UnityCG.cginc"

            struct v2fShadow { V2F_SHADOW_CASTER; };
            v2fShadow vertShadow(appdata_base v)
            {
                v2fShadow o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 fragShadow(v2fShadow i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}