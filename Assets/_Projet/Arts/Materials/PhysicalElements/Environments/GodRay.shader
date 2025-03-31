Shader "Unlit/GodRay"
{
    Properties
    {
        _RayColor("Ray Color", Color) = (1, 1, 1, 1)

        _FirstEndSize("First End Size", float) = 0.25
        _SecondEndSize("Second End Size", float) = 0.75

        _BaseOpacity("Base Opacity", float) = 0.01
        _SmokeOpacity("Smoke Opacity", float) = 4

        _FadeScale("Fade Scale", float) = 0.01

        _SmokeNoise("Smoke Noise", 2D) = "white"
        _SmokeNoiseSpeed("Smoke Speed", float) = 1

        _RayNoise("Ray Noise", 2D) = "white"
        _RayNoiseStartPoint("Ray Noise Start Point", float) = 0.7
        _RayNoiseIntensity("Ray Noise Intensity", float) = 0.1

    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexMain
            #pragma fragment FragmentMain
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _RayColor;

            float _FirstEndSize;
            float _SecondEndSize;
            float _FadeScale;

            float _BaseOpacity;
            float _SmokeOpacity;

            TEXTURE2D(_SmokeNoise); SAMPLER(sampler_SmokeNoise);
            float _SmokeNoiseSpeed;

            TEXTURE2D(_RayNoise); SAMPLER(sampler_RayNoise);
            float _RayNoiseStartPoint;
            float _RayNoiseIntensity;

            struct VertexInput
            {
                float4 vertex : POSITION; 
                float4 uv : TEXCOORD0;
	            float3 normal : NORMAL;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
           	    float4 uv : TEXCOORD0;

			    float3 normScreen : TEXCOORD1;

                float4 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            VertexOutput VertexMain (VertexInput input)
            {
                VertexOutput vo;

                vo.pos = TransformObjectToHClip(input.vertex.xyz);
                vo.normScreen = normalize(mul(UNITY_MATRIX_IT_MV, float4(input.normal, 1)).xyz);
                vo.uv = input.uv;

                vo.worldPos = mul(unity_ObjectToWorld, input.vertex);
                vo.screenPos = ComputeScreenPos(vo.pos);

                return vo;
            }

            float4 FragmentMain (VertexOutput input) : SV_Target
            {
                float size = lerp(_FirstEndSize, _SecondEndSize, input.uv.x);
                float msize = 1 - size;
                float maskX = smoothstep(0, _FadeScale, input.uv.x) * smoothstep(1, 1 - _FadeScale, input.uv.x);
                float maskY = smoothstep(msize * 0.5, _FadeScale + msize * 0.5, input.uv.y) * smoothstep(1 - msize * 0.5, 1 - msize * 0.5 - _FadeScale, input.uv.y);

                float mask = maskX * maskY;

                if(mask > 0)
                {
                    float4 color = _RayColor;
                    float2 smokeUV = float2(input.uv.x * 0.3, input.uv.y) + float2(-_Time.y * pow(_SmokeNoiseSpeed, 1), 0);
                    color.a = saturate(_BaseOpacity + pow(SAMPLE_TEXTURE2D(_SmokeNoise, sampler_SmokeNoise, smokeUV).r, _SmokeOpacity));
                    color.a *= mask;
                    color.a *= saturate(1 - pow(SAMPLE_TEXTURE2D(_RayNoise, sampler_SmokeNoise, float2(input.uv.x, input.uv.y / size)).r, _RayNoiseIntensity) * smoothstep(_RayNoiseStartPoint, 1, input.uv.x));
                    color.a *= pow(dot(input.normScreen, float3(0, 0, 1)), 2);

                    return color;
                }
                
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
