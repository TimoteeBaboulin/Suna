Shader "Kevin Iglesias/Unlit Basic Motions Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorTint ("Tint Color", Color) = (1, 1, 1, 1)
        _OcclusionTex ("Occlusion Texture", 2D) = "white" {}
        _OcclusionIntensity ("Occlusion Intensity", Range(0, 1)) = 1.0
        _MetallicMap ("Metallic Map", 2D) = "white" {}
        _MetallicTransparency ("Metallic Transparency", Range(0, 1)) = 1.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalIntensity ("Normal Intensity", Range(-1, 1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Name "MAIN"
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _OcclusionTex;
            sampler2D _MetallicMap;
            sampler2D _NormalMap;
            float4 _MainTex_ST;
            float4 _OcclusionTex_ST;
            float4 _MetallicMap_ST;
            float4 _NormalMap_ST;
            fixed4 _ColorTint;
            float _OcclusionIntensity;
            float _MetallicTransparency;
            float _NormalIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                col *= _ColorTint;
                
                fixed4 occlusion = tex2D(_OcclusionTex, i.uv);
                col.rgb *= lerp(1.0, occlusion.rgb, _OcclusionIntensity);
                
                fixed4 metallic = tex2D(_MetallicMap, i.uv);
                metallic.rgb = 1.0 - metallic.rgb;
                col.rgb = lerp(col.rgb, metallic.rgb * col.rgb, _MetallicTransparency);

                float3 normalTex = tex2D(_NormalMap, i.uv).rgb;
                normalTex.g = 1.0 - normalTex.g;
                normalTex = normalize(normalTex * 2.0 - 1.0);

                float3 lightDir = float3(0.0, 0.0, 1.0);
                float bumpEffect = dot(normalTex, lightDir) * _NormalIntensity;
                col.rgb *= 1.0 + bumpEffect;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
