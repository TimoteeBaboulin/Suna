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
        _SandRoughness("Sand Roughness", 2D) = "white"

        _Noises("Noise Texture", 2D) = "white"

        _HeightFactor("Sand Height Scale", float) = 1
        _BlendScale("Blend Scale", float) = 0.05

        _BigNoiseTiling("Tiling Factor", float) = 4
        _Sharpness("Sharpness", float) = 1
        _TextureScale("Material Scale", float) = 1

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
        ZWrite On

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
            #pragma shader_feature _ LIGHTMAP_ON
            #pragma shader_feature_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma shader_feature_fragment _ _ADDITIONAL_LIGHTS
            #pragma shader_feature_fragment _ADDITIONAL_LIGHT_SHADOWS
            #pragma shader_feature_fragment _ _SHADOWS_SOFT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_FloorAlbedo); SAMPLER(sampler_FloorAlbedo);
            TEXTURE2D(_FloorRoughness);
            TEXTURE2D(_FloorNormal); SAMPLER(sampler_FloorNormal);
            TEXTURE2D(_FloorHeight); SAMPLER(sampler_FloorHeight);

            TEXTURE2D(_SandTexture); SAMPLER(sampler_SandTexture);
            TEXTURE2D(_SandNormal); SAMPLER(sampler_SandNormal);
            TEXTURE2D(_SandRoughness);

            TEXTURE2D(_Noises); SAMPLER(sampler_Noises);

            CBUFFER_START(UnityPerMaterial)
                float _HeightFactor;
                float _BlendScale;

                float _BigNoiseTiling;

                float _BigNoiseScale;
                float _SmallNoiseScale;
                float _NegativeNoiseScale;

                float _Sharpness;
                float _TextureScale;
            CBUFFER_END

            struct VertexInput
            {
                float4 vertex : POSITION; 
	            float4 tangent : TANGENT;
	            float3 normal : NORMAL;
	            float2 textureUV : TEXCOORD0;  
	            float2 lightmapUV : TEXCOORD1; 
	            float4 color : COLOR;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
           	    float2 uv : TEXCOORD0;

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

                vo.pos = vpi.positionCS;
                vo.normScreen = normalize(mul(UNITY_MATRIX_IT_MV, float4(input.normal, 1)).xyz);
                vo.uv = input.textureUV;
                vo.worldPos = float4(vpi.positionWS, 0);
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

            float4 SurfaceFragment(VertexOutput input) : SV_Target
            {
                float3x3 TBN = float3x3(input.worldTangent, input.worldBitangent, input.worldNormal);

                float2 uvFrontGround = input.worldPos.xy * _TextureScale;
                float2 uvSideGround = input.worldPos.zy * _TextureScale;
                float2 uvTopGround = input.worldPos.xz * _TextureScale;

                float2 uvFrontSand = input.worldPos.xy * _TextureScale;
                float2 uvSideSand = input.worldPos.zy * _TextureScale;
                float2 uvTopSand = input.worldPos.xz * _TextureScale;

                float4 colorFrontGround = SAMPLE_TEXTURE2D(_FloorAlbedo, sampler_FloorAlbedo, uvFrontGround);
                float4 colorSideGround = SAMPLE_TEXTURE2D(_FloorAlbedo, sampler_FloorAlbedo, uvSideGround);
                float4 colorTopGround = SAMPLE_TEXTURE2D(_FloorAlbedo, sampler_FloorAlbedo, uvTopGround);

                float3 normalFrontGround = SAMPLE_TEXTURE2D(_FloorNormal, sampler_FloorNormal, uvFrontGround).xyz;
                float3 normalSideGround = SAMPLE_TEXTURE2D(_FloorNormal, sampler_FloorNormal, uvSideGround).xyz;
                float3 normalTopGround = SAMPLE_TEXTURE2D(_FloorNormal, sampler_FloorNormal, uvTopGround).xyz;

                float heightFront = SAMPLE_TEXTURE2D(_FloorHeight, sampler_FloorHeight, uvFrontGround).x;
                float heightSide = SAMPLE_TEXTURE2D(_FloorHeight, sampler_FloorHeight, uvSideGround).x;
                float heightTop = SAMPLE_TEXTURE2D(_FloorHeight, sampler_FloorHeight, uvTopGround).x;

                float roughnessFrontGround = SAMPLE_TEXTURE2D(_FloorRoughness, sampler_FloorHeight, uvFrontGround).x;
                float roughnessSideGround = SAMPLE_TEXTURE2D(_FloorRoughness, sampler_FloorHeight, uvSideGround).x;
                float roughnessTopGround = SAMPLE_TEXTURE2D(_FloorRoughness, sampler_FloorHeight, uvTopGround).x;

                float4 colorFrontSand = SAMPLE_TEXTURE2D(_SandTexture, sampler_SandTexture, uvFrontSand);
                float4 colorSideSand = SAMPLE_TEXTURE2D(_SandTexture, sampler_SandTexture, uvSideSand);
                float4 colorTopSand = SAMPLE_TEXTURE2D(_SandTexture, sampler_SandTexture, uvTopSand);

                float3 normalFrontSand = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandNormal, uvFrontSand).xyz;
                float3 normalSideSand = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandNormal, uvSideSand).xyz;
                float3 normalTopSand = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandNormal, uvTopSand).xyz;

                float roughnessFrontSand = SAMPLE_TEXTURE2D(_SandRoughness, sampler_FloorHeight, uvFrontSand).x;
                float roughnessSideSand = SAMPLE_TEXTURE2D(_SandRoughness, sampler_FloorHeight, uvSideSand).x;
                float roughnessTopSand = SAMPLE_TEXTURE2D(_SandRoughness, sampler_FloorHeight, uvTopSand).x;

                float3 weights = input.worldNormal;
                weights = abs(weights);
                weights = pow(weights, _Sharpness);
                weights = weights / (weights.x + weights.y + weights.z);

                colorFrontGround *= weights.z;
                colorSideGround *= weights.x;
                colorTopGround *= weights.y;
                colorFrontSand *= weights.z;
                colorSideSand *= weights.x;
                colorTopSand *= weights.y;
                normalFrontGround *= weights.z;
                normalSideGround *= weights.x;
                normalTopGround *= weights.y;
                normalFrontSand *= weights.z;
                normalSideSand *= weights.x;
                normalTopSand *= weights.y;
                heightFront *= weights.z;
                heightSide *= weights.x;
                heightTop *= weights.y;
                roughnessFrontGround *= weights.z;
                roughnessSideGround *= weights.x;
                roughnessTopGround *= weights.y;
                roughnessFrontSand *= weights.z;
                roughnessSideSand *= weights.x;
                roughnessTopSand *= weights.y;

                float4 groundColor = colorFrontGround + colorSideGround + colorTopGround;
                float4 sandColor = colorFrontSand + colorSideSand + colorTopSand;
                float3 groundNormal = mul(TBN, (normalFrontGround + normalSideGround + normalTopGround).xyz);
                float3 sandNormal = mul(TBN, (normalFrontSand + normalSideSand + normalTopSand).xyz);
                float floorHeight = heightFront + heightSide + heightTop;
                float groundRoughness = roughnessFrontGround + roughnessSideGround + roughnessTopGround;
                float sandRoughness = roughnessFrontSand + roughnessSideSand + roughnessTopSand;

                InputData lighting = (InputData) 0;
                lighting.positionWS = input.worldPos.xyz;
                lighting.positionCS = input.pos;
                lighting.normalWS = normalize(input.worldNormal);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.worldPos);
                lighting.fogCoord = input.fogFactorAndVertexLight.x;
                lighting.vertexLighting = input.fogFactorAndVertexLight.yzw;
                lighting.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, lighting.normalWS);
                // lighting.bakedGI = SampleSH(lighting.normalWS);
                
                // #if defined(LIGHTMAP_ON)
                //     lighting.bakedGI += SampleLightmap(input.lightmapUV, lighting.normalWS);
                // #endif

                //SAMPLE_GI(input.lightmapUV, input.vertexSH, lighting.normalWS);
                
                 #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                     lighting.shadowCoord = input.shadowCoord;
                 #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                     lighting.shadowCoord = TransformWorldToShadowCoord(lighting.positionWS);
                 #else
                     lighting.shadowCoord = float4(0, 0, 0, 0);
                 #endif

                SurfaceData surface = (SurfaceData) 0;

                float2 bigNoiseMapping = frac(input.worldPos.xz * _BigNoiseTiling);

                float bigNoise = SAMPLE_TEXTURE2D(_Noises, sampler_Noises, bigNoiseMapping).r;
                float2 noiseData = SAMPLE_TEXTURE2D(_Noises, sampler_Noises, input.uv).gb;

                float sandHeight = pow(bigNoise, _BigNoiseScale)
                                +  pow(noiseData.r, _SmallNoiseScale)
                                -  pow(noiseData.g, _NegativeNoiseScale);

                float lerpFactor = saturate((floorHeight - (sandHeight * _HeightFactor)) / _BlendScale);
                surface.albedo = lerp(sandColor, groundColor, lerpFactor);
                surface.normalTS = lerp(sandNormal, groundNormal, lerpFactor);
                surface.alpha = 1;     
                surface.occlusion = 1;
                surface.smoothness = lerp(sandRoughness, groundRoughness, lerpFactor);

                if(dot(input.worldNormal, float3(0, 1, 0)) < 0.1)
                {
                    surface.albedo = groundColor;
                    surface.normalTS = groundNormal;
                    surface.smoothness = groundRoughness;
                }

                float4 PBR = UniversalFragmentPBR(lighting, surface);
                float4 finalColor = float4(MixFog(PBR.rgb, lighting.fogCoord), 1);
                return finalColor;
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
