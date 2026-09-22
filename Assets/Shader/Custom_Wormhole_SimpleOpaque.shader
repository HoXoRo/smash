Shader "Custom/Wormhole_SimpleOpaque"
{
    Properties
    {
        [NoScaleOffset] _Mask ("Mask", 2D) = "white" {}
        _TwirlStrength ("Twirl Strength", Float) = 10
        _Scale ("Scale", Float) = 2.5
        _TimeSpeed ("Time Speed", Float) = 0.5
        [HDR] _Color ("Color", Vector) = (1,1,1,1)
        _Dissolve ("Dissolve", Float) = 0
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
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            CBUFFER_START(UnityPerMaterial)
                float _TwirlStrength;
                float _Scale;
                float _TimeSpeed;
                float4 _Color;
                float _Dissolve;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float2 HashCell(float2 cell)
            {
                float2 dots = float2(
                    dot(cell, float2(127.1, 311.7)),
                    dot(cell, float2(269.5, 183.3))
                );
                return frac(sin(dots) * 43758.5469);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv.yx - 0.5;
                float angle = length(centered) * _TwirlStrength;
                float s = sin(angle);
                float c = cos(angle);

                float2 rotated;
                rotated.x = c * centered.y - centered.x * s;
                rotated.y = c * centered.x + centered.y * s;

                float2 noiseUv = rotated + (_Time.y * _TimeSpeed);
                noiseUv = (noiseUv + 0.5) * _Scale;

                float2 baseCell = floor(noiseUv);
                float2 localUv = frac(noiseUv);
                float minDistance = 8.0;

                UNITY_LOOP
                for (int y = -1; y <= 1; y++)
                {
                    UNITY_LOOP
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 cellOffset = float2(x, y);
                        float2 feature = HashCell(baseCell + cellOffset) + cellOffset - localUv;
                        minDistance = min(minDistance, length(feature));
                    }
                }

                float dissolve = pow(max(minDistance, 0.0001), _Dissolve);
                float3 mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv).rgb;
                float3 color = dissolve * mask * _Color.rgb;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
