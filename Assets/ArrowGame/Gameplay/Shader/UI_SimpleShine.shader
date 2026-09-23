Shader "UI/SimpleShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _ShineColor ("Shine Color", Color) = (1, 1, 1, 1)
        _ShineIntensity ("Shine Intensity", Range(0, 2)) = 0.35
        _ShineWidth ("Shine Width", Range(0.001, 0.5)) = 0.12
        _ShineDuration ("Shine Duration", Float) = 0.8
        _WaitDuration ("Wait Duration", Float) = 2
        _ShineAngle ("Shine Angle Radians", Float) = 0.5

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }
        LOD 100

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            Name "SimpleShine"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            ZTest [unity_GUIZTestMode]
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _ShineColor;
                float _ShineIntensity;
                float _ShineWidth;
                float _ShineDuration;
                float _WaitDuration;
                float _ShineAngle;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                float shineWidth = max(_ShineWidth, 0.001);
                float shineDuration = max(_ShineDuration, 0.001);
                float cycleDuration = max(_WaitDuration + _ShineDuration, 0.001);
                float elapsed = frac(_Time.y / cycleDuration) * cycleDuration;

                // The band travels from just outside the left edge to just outside the right edge.
                float shineProgress = saturate(elapsed / shineDuration);
                float shineCenter = shineProgress * (1.0 + shineWidth * 2.0) - shineWidth;

                float2 centeredUV = input.uv - 0.5;
                float2 shineDirection = float2(cos(_ShineAngle), sin(_ShineAngle));
                float projectedPosition = dot(centeredUV, shineDirection) + 0.5;
                float shineMask = saturate(1.0 - abs(projectedPosition - shineCenter) / shineWidth);
                shineMask = shineMask * shineMask * (3.0 - 2.0 * shineMask);

                // During the wait portion the band is fully disabled.
                shineMask *= step(elapsed, _ShineDuration);
                baseColor.rgb += _ShineColor.rgb * (_ShineIntensity * shineMask * baseColor.a);
                return baseColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
