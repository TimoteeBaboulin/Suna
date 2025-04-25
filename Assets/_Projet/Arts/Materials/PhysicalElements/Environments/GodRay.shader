Shader "Unlit/GodRay"
{
    Properties
    {
        _RayColor("Ray Color", Color) = (1, 1, 1, 1)

        _FirstEndSize("First End Size", float) = 0.25
        _SecondEndSize("Second End Size", float) = 0.75

        _BaseOpacity("Base Opacity", float) = 0.01
        _SmokeOpacity("Smoke Opacity", float) = 4

        _FogDensity("Fog Density", float) = 1
        _LightScattering("Light Scattering", float) = 1

        _DustAngle("Dust Angle", float) = 0

        _FadeScale("Fade Scale", float) = 0.01

        _SmokeNoise("Smoke Noise", 2D) = "white"
        _SmokeNoiseSpeed("Smoke Speed", float) = 1

        _RayNoise("Ray Noise", 2D) = "white"
        _RayNoiseStartPoint("Ray Noise Start Point", float) = 0.7
        _RayNoiseIntensity("Ray Noise Intensity", float) = 0.1

        _DustParticles("Dust Particles Noise", 2D) = "white"

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

            float _DustAngle;

            float _FadeScale;

            float _BaseOpacity;
            float _SmokeOpacity;

            float _FogDensity;
            float _LightScattering;

            TEXTURE2D(_SmokeNoise); SAMPLER(sampler_SmokeNoise);
            float _SmokeNoiseSpeed;

            TEXTURE2D(_RayNoise);
            float _RayNoiseStartPoint;
            float _RayNoiseIntensity;

            TEXTURE2D(_DustParticles); SAMPLER(sampler_DustParticles);

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

            float XMask(float sizeAt, float2 uv)
            {
                float size = 1 - sizeAt;
                return saturate(smoothstep(0, _FadeScale, uv.x)) * saturate(smoothstep(1, 1 - _FadeScale, uv.x));
            }

            float YMask(float sizeAt, float2 uv)
            {
                float size = 1 - sizeAt;

                float startMask = saturate(smoothstep(size * 0.5, _FadeScale * 10 + size * 0.5, uv.y) * lerp(1, 0.1, saturate(uv.x - 0.7) / 0.3));
                float endMask = saturate(smoothstep(1 - (size * 0.5), 1 - (size * 0.5 + _FadeScale), uv.y));

                return saturate(startMask * endMask * 100);
            }

            float4 FragmentMain (VertexOutput input) : SV_Target
            {
                float size = lerp(_FirstEndSize, _SecondEndSize, input.uv.x);
                float msize = 1 - size;
                float maskX = XMask(size, input.uv);
                float maskY = YMask(size, input.uv);

                float mask = maskX * maskY;

                //return float4(mask, mask, mask, 1);

                if(mask > 0)
                {
                    float4 color = _RayColor;

                    float2 smokeSampleAt1 = float2(input.uv.x, input.uv.y) + float2(-_Time.y * _SmokeNoiseSpeed * cos(_DustAngle), _Time.y * _SmokeNoiseSpeed * sin(_DustAngle));
                    float2 smokeSampleAt2 = float2(input.uv.x * .88, input.uv.y * .94) + float2(-_Time.y * _SmokeNoiseSpeed * 0.9 * cos(_DustAngle + .3), _Time.y * _SmokeNoiseSpeed * sin(_DustAngle + .3));
                    float2 smokeSampleAt3 = float2(input.uv.x * 1.1, input.uv.y * .8) + float2(-_Time.y * _SmokeNoiseSpeed * 1.1 * cos(_DustAngle - .2), _Time.y * _SmokeNoiseSpeed * sin(_DustAngle - .2));

                    float smokeAt1 = SAMPLE_TEXTURE2D(_SmokeNoise, sampler_SmokeNoise, smokeSampleAt1).r;
                    float smokeAt2 = SAMPLE_TEXTURE2D(_SmokeNoise, sampler_SmokeNoise, smokeSampleAt2).r;
                    float smokeAt3 = SAMPLE_TEXTURE2D(_SmokeNoise, sampler_SmokeNoise, smokeSampleAt3).r;

                    float smokeAlpha = saturate(
                        pow(smokeAt1, _SmokeOpacity) * 
                        pow(smokeAt2, _SmokeOpacity * .9) *
                        pow(smokeAt3, _SmokeOpacity * 1.1)
                    );

                    // float dustAt = SAMPLE_TEXTURE2D(_DustParticles, sampler_DustParticles, smokeSampleAt1).r;
                    // float dustAt2 = SAMPLE_TEXTURE2D(_DustParticles, sampler_DustParticles, smokeSampleAt3 * 1.5).r;

                    color.a = saturate(_BaseOpacity + smokeAlpha);
                    // float voro = sin(input.uv.x + cos(_Time.y - input.uv.y) * sin(_Time.y)) * pow(dustAt, 4) * 350;
                    // float voro2 = cos(input.uv.x - sin(_Time.y * 4 + input.uv.y) * sin(_Time.y * 0.5)) * pow(dustAt2, 4);
                    // color.a += max(saturate(voro) * smoothstep(.7, .9, 1 - smokeAt2), saturate(voro2) * smoothstep(.7, .9, 1 - smokeAt1)) * 10;
                    //return float4(voro, voro, voro, 1);
                    color.a *= mask;
                    color.a *= saturate(1 - pow(SAMPLE_TEXTURE2D(_RayNoise, sampler_SmokeNoise, float2(input.uv.x, input.uv.y / size)).r, _RayNoiseIntensity) * smoothstep(_RayNoiseStartPoint, 1, input.uv.x));
                    color.a *= saturate(exp2(log2(saturate((pow(dot(input.normScreen, float3(0, 0, 1)), 2) * 1.5 - .3) / .8) * _FogDensity) * _LightScattering));
                    //color.a *= saturate(smoothstep(0, 1, input.screenPos.z));

                    return color;
                }
                
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
