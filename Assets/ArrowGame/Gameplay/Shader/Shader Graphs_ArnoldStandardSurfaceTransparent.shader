Shader "Shader Graphs/ArnoldStandardSurfaceTransparent"
{
    Properties
    {
        _BASE_COLOR ("BaseColor", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _BASE_COLOR_MAP ("BaseColorMap", 2D) = "white" {}
        _METALNESS ("Metalness", Float) = 0
        [NoScaleOffset] _METALNESS_MAP ("MetalnessMap", 2D) = "white" {}
        _SPECULAR_COLOR ("SpecularColor", Color) = (1, 1, 1, 0)
        [NoScaleOffset] _SPECULAR_COLOR_MAP ("SpecularColorMap", 2D) = "white" {}
        _SPECULAR_ROUGHNESS ("SpecularRoughness", Float) = 0
        [NoScaleOffset] _SPECULAR_ROUGHNESS_MAP ("SpecularRoughnessMap", 2D) = "white" {}
        _SPECULAR_IOR ("SpecularIOR", Float) = 1.5
        [NoScaleOffset] _SPECULAR_IOR_MAP ("SpecularIORMap", 2D) = "white" {}
        _EMISSION_COLOR ("EmissionColor", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _EMISSION_COLOR_MAP ("EmissionColorMap", 2D) = "white" {}
        [NoScaleOffset] [Normal] _NORMAL_MAP ("NormalMap", 2D) = "bump" {}
        _OPACITY ("Opacity", Range(0, 1)) = 1
        [NoScaleOffset] _OPACITY_MAP ("OpacityMap", 2D) = "white" {}
        [HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl ("_QueueControl", Float) = -1
        [HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #define _SPECULAR_SETUP 1
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BASE_COLOR_MAP);
            SAMPLER(sampler_BASE_COLOR_MAP);
            TEXTURE2D(_METALNESS_MAP);
            SAMPLER(sampler_METALNESS_MAP);
            TEXTURE2D(_SPECULAR_COLOR_MAP);
            SAMPLER(sampler_SPECULAR_COLOR_MAP);
            TEXTURE2D(_SPECULAR_ROUGHNESS_MAP);
            SAMPLER(sampler_SPECULAR_ROUGHNESS_MAP);
            TEXTURE2D(_SPECULAR_IOR_MAP);
            SAMPLER(sampler_SPECULAR_IOR_MAP);
            TEXTURE2D(_EMISSION_COLOR_MAP);
            SAMPLER(sampler_EMISSION_COLOR_MAP);
            TEXTURE2D(_NORMAL_MAP);
            SAMPLER(sampler_NORMAL_MAP);
            TEXTURE2D(_OPACITY_MAP);
            SAMPLER(sampler_OPACITY_MAP);

            CBUFFER_START(UnityPerMaterial)
                half4 _BASE_COLOR;
                half _METALNESS;
                half4 _SPECULAR_COLOR;
                half _SPECULAR_ROUGHNESS;
                half _SPECULAR_IOR;
                half4 _EMISSION_COLOR;
                half _OPACITY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half3 BuildNormalWS(Varyings input, half3 normalTS)
            {
                half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                return NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 baseMap = SAMPLE_TEXTURE2D(_BASE_COLOR_MAP, sampler_BASE_COLOR_MAP, input.uv).rgb;
                half3 baseColor = baseMap * _BASE_COLOR.rgb;

                half3 metalnessMap = SAMPLE_TEXTURE2D(_METALNESS_MAP, sampler_METALNESS_MAP, input.uv).rgb;
                half3 metalness = saturate(metalnessMap * _METALNESS);

                half3 specularMap = SAMPLE_TEXTURE2D(_SPECULAR_COLOR_MAP, sampler_SPECULAR_COLOR_MAP, input.uv).rgb;
                half3 specularColor = specularMap * _SPECULAR_COLOR.rgb;

                half iorMap = SAMPLE_TEXTURE2D(_SPECULAR_IOR_MAP, sampler_SPECULAR_IOR_MAP, input.uv).r;
                half ior = max(_SPECULAR_IOR * iorMap, 1.0001h);
                half iorRatio = (ior - 1.0h) / (ior + 1.0h);
                half3 dielectricF0 = specularColor * (iorRatio * iorRatio);
                half3 specularF0 = lerp(dielectricF0, baseColor, metalness);

                half roughness = SAMPLE_TEXTURE2D(_SPECULAR_ROUGHNESS_MAP, sampler_SPECULAR_ROUGHNESS_MAP, input.uv).r;
                half smoothness = saturate(1.0h - roughness * _SPECULAR_ROUGHNESS);

                half opacityMap = SAMPLE_TEXTURE2D(_OPACITY_MAP, sampler_OPACITY_MAP, input.uv).r;
                half alpha = saturate(opacityMap * _OPACITY);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NORMAL_MAP, sampler_NORMAL_MAP, input.uv));
                half3 emission = SAMPLE_TEXTURE2D(_EMISSION_COLOR_MAP, sampler_EMISSION_COLOR_MAP, input.uv).rgb * _EMISSION_COLOR.rgb;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor;
                surfaceData.specular = specularF0;
                surfaceData.metallic = 1.0h;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission;
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = alpha;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = BuildNormalWS(input, normalTS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = alpha;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
