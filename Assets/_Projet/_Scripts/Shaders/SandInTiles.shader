Shader "Custom/SandInTiles"
{
    Properties
    {
        // [Header("Floor Textures")]
        _FloorAlbedo("Floor Albedo", 2D) = "white"
        _FloorNormal("Floor Normal", 2D) = "white"
        _FloorRoughness("Floor Roughness", 2D) = "white"
        _FloorHeight("Floor Height", 2D) = "white"

        _SandTexture("Sand Texture", 2D) = "white"
        _SandNormal("Sand Normal", 2D) = "white"

        _Noises("Noise Texture", 2D) = "white"

        _HeightFactor("Sand Height Scale", float) = 1
        _BlendScale("Blend Scale", float) = 0.05

        _BigNoiseTiling("Tiling Factor", float) = 4

        _BigNoiseScale("Big Noise Scale", float) = 2
        _SmallNoiseScale("Small Noise Scale", float) = 1
        _NegativeNoiseScale("Negative Noise Scale", float) = 2

    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        LOD 200

        Pass
        {
            Name "ForwardPass"
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #define _SPECULAR_COLOR_SPECULAR_COLOR
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment
            #pragma multi_compile_fog
            #pragma shader_feature _ _FORWARD_PLUS
            #pragma shader_feature_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma shader_feature_fragment _ADDITIONAL_LIGHT_SHADOWS
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_FloorAlbedo); SAMPLER(sampler_FloorAlbedo);
            TEXTURE2D(_FloorRoughness);
            TEXTURE2D(_FloorNormal); SAMPLER(sampler_FloorNormal);
            TEXTURE2D(_FloorHeight);

            TEXTURE2D(_SandTexture); SAMPLER(sampler_SandTexture);
            TEXTURE2D(_SandNormal); SAMPLER(sampler_SandNormal);

            TEXTURE2D(_Noises); SAMPLER(sampler_Noises);

            float _HeightFactor;
            float _BlendScale;

            float _BigNoiseTiling;

            float _BigNoiseScale;
            float _SmallNoiseScale;
            float _NegativeNoiseScale;

            struct VertexInput
            {
                float4 vertex : POSITION; 
	            float4 tangent : TANGENT;
	            float3 normal : NORMAL;
	            float4 textureUV : TEXCOORD0;  
	            float4 lightmapUV : TEXCOORD1; 
	            float4 color : COLOR;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
           	    float4 uv : TEXCOORD0;

                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

			    float3 normScreen : TEXCOORD2;

			    float4 worldPos : TEXCOORD3;
			    float4 screenPos : TEXCOORD4;

			    float3 worldNormal : TEXCOORD5;
                float3 worldTangent : TEXCOORD6;
                float3 worldBitangent : TEXCOORD7;

                float4 fogFactorAndVertexLight : TEXCOORD8;
                float4 shadowCoord : TEXCOORD9;
            };

            struct TBN
            {
                float3 normalWorldSpace;
                float3 tangentWorldSpace;
                float3 bitangentWorldSpace;
            };

            VertexOutput SurfaceVertex(VertexInput input)
            {
                VertexOutput vo;

                VertexPositionInputs vpi = GetVertexPositionInputs(input.vertex.xyz);

                vo.pos = TransformObjectToHClip(input.vertex.xyz);
                vo.normScreen = normalize(mul(UNITY_MATRIX_IT_MV, float4(input.normal, 1)).xyz);
                vo.uv = input.textureUV;
                vo.worldPos = mul(unity_ObjectToWorld, input.vertex);
                vo.screenPos = ComputeScreenPos(vo.pos);

                VertexNormalInputs tbn = GetVertexNormalInputs(input.normal, input.tangent);
                vo.worldNormal = tbn.normalWS;
                vo.worldTangent = tbn.tangentWS;
                vo.worldBitangent = tbn.bitangentWS;

                float3 vertexLight = VertexLighting(vo.worldPos.xyz, vo.worldNormal);
                float fogFactor = ComputeFogFactor(vo.pos.z);
                vo.fogFactorAndVertexLight = float4(fogFactor, vertexLight);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, vo.lightmapUV);
                OUTPUT_SH(vo.worldNormal.xyz, vo.vertexSH);

                vo.shadowCoord = GetShadowCoord(vpi);

                return vo;
            }

            float4 SurfaceFragment (VertexOutput input) : SV_Target
            {
                float3x3 TBN = float3x3(input.worldTangent, input.worldBitangent, input.worldNormal);

                InputData lighting = (InputData) 0;
                lighting.positionWS = input.worldPos.xyz;
                lighting.normalWS = normalize(input.worldNormal);
                lighting.viewDirectionWS = GetWorldSpaceViewDir(input.worldPos).xyz;
                lighting.fogCoord = input.fogFactorAndVertexLight.x;
                lighting.vertexLighting = input.fogFactorAndVertexLight.yzw;
                lighting.shadowCoord = input.shadowCoord;
                lighting.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, lighting.normalWS);
                
                // #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                //     lighting.shadowCoord = input.shadowCoord;
                // #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                //     lighting.shadowCoord = TransformWorldToShadowCoord(lighting.worldPos);
                // #else
                //     lighting.shadowCoord = float4(0, 0, 0, 0);
                // #endif

                SurfaceData surface = (SurfaceData) 0;

                float2 bigNoiseMapping = frac(input.worldPos.xz * _BigNoiseTiling);

                float bigNoise = SAMPLE_TEXTURE2D(_Noises, sampler_Noises, bigNoiseMapping).r;
                float2 noiseData = SAMPLE_TEXTURE2D(_Noises, sampler_Noises, input.uv).gb;

                float sandHeight = pow(bigNoise, _BigNoiseScale)
                                +  pow(noiseData.r, _SmallNoiseScale)
                                -  pow(noiseData.g, _NegativeNoiseScale);

                float floorHeight = SAMPLE_TEXTURE2D(_FloorHeight, sampler_FloorAlbedo, input.uv).r;

                float3 floorColor = SAMPLE_TEXTURE2D(_FloorAlbedo, sampler_FloorAlbedo, input.uv).rgb;
                float3 sandColor = SAMPLE_TEXTURE2D(_SandTexture, sampler_SandTexture, input.uv).rgb;

                float3 floorNormal = mul(TBN, SAMPLE_TEXTURE2D(_FloorNormal, sampler_FloorNormal, input.uv).xyz);
                float3 sandNormal = mul(TBN, SAMPLE_TEXTURE2D(_SandNormal, sampler_SandNormal, input.uv).xyz);

                surface.albedo = lerp(sandColor, floorColor, saturate((floorHeight - (sandHeight * _HeightFactor)) / _BlendScale));
                surface.normalTS = lerp(sandNormal, floorNormal, saturate((floorHeight - (sandHeight * _HeightFactor)) / _BlendScale));
                surface.alpha = 1;     
                surface.occlusion = 1;
                surface.smoothness = SAMPLE_TEXTURE2D(_FloorRoughness, sampler_FloorAlbedo, input.uv).r;

                return UniversalFragmentPBR(lighting, surface);
            }

            ENDHLSL
        } 
    }
    FallBack "Diffuse"
}
