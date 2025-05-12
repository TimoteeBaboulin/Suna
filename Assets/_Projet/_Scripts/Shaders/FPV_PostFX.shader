Shader "Unlit/FPV_PostFX"
{
    Properties
    {
        _FPV_RT("First Person View Render Texture", 2D) = "white"
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
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_FPV_RT);

            half4 Frag (Varyings IN) : SV_Target
            {
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float4 fpvColor = SAMPLE_TEXTURE2D(_FPV_RT, sampler_LinearClamp, IN.texcoord);

                return sceneColor * (1 - fpvColor.a) + fpvColor;
            }
            ENDHLSL
        }
    }
}
