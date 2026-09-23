Shader "Shader Graphs/Glow"
{
    Properties
    {
        _GlowStrength ("GlowStrength", Float) = 1
        _Color ("Color", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _Texture2D ("Texture2D", 2D) = "white" {}
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
            "PreviewType" = "Plane"
        }
        LOD 100

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Texture2D);
            SAMPLER(sampler_Texture2D);

            CBUFFER_START(UnityPerMaterial)
                half _GlowStrength;
                half4 _Color;
            CBUFFER_END

            // ParticleSystemRenderer supplies this value at draw time.
            half4 _RendererColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _RendererColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half3 textureColor = SAMPLE_TEXTURE2D(_Texture2D, sampler_Texture2D, input.uv).rgb;

                // The original graph promotes its RGB expression to RGBA using B as alpha.
                half4 glowColor = half4(textureColor, textureColor.b) + _Color.xyzz;
                glowColor *= _GlowStrength;
                glowColor *= input.color;

                clip(glowColor.a - 1e-6h);
                return glowColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
