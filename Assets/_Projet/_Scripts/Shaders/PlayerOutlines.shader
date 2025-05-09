Shader "Unlit/PlayerOutlines"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.3, 0.3, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZTest Always Cull Off ZWrite Off Fog { Mode off }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 normScreen : TEXCOORD2;
            };

            float4 _OutlineColor;

            v2f Vert(appdata input)
            {
                VertexPositionInputs vpi = GetVertexPositionInputs(input.vertex.xyz);
                v2f output;

                output.pos = vpi.positionCS;
                output.uv = input.uv;
                output.normScreen = normalize(mul(UNITY_MATRIX_IT_MV, float4(input.normal, 1)).xyz);

                return output;
            }

            half4 Frag (v2f i) : SV_Target
            {
                return _OutlineColor;
                //return lerp(_OutlineColor * 0.5, _OutlineColor, pow(abs(dot(normalize(i.normScreen), float3(0, 0, 1))), 2));
            }
            
            ENDHLSL
        }
    }
}
