Shader "Shadow Receiver URP/ShadowReceiverTransparent"
{
	Properties
	{
		Color_72bc1e57291a4e8dadc03f5e475acd62 ("Shadow Color", Color) = (0, 0, 0, 0)
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
			"Queue" = "Transparent+50"
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
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half4 Color_72bc1e57291a4e8dadc03f5e475acd62;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positionInputs.positionCS;
				output.positionWS = positionInputs.positionWS;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				Light mainLight = GetMainLight(shadowCoord);
				half shadowAlpha = (1.0h - saturate(mainLight.shadowAttenuation)) * Color_72bc1e57291a4e8dadc03f5e475acd62.a;
				return half4(Color_72bc1e57291a4e8dadc03f5e475acd62.rgb, shadowAlpha);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
