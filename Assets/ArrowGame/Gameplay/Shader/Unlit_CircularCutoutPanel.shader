Shader "Unlit/CircularCutoutPanel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _HolePositionX ("Hole Position X", Range(0, 1)) = 0.5
        _HolePositionY ("Hole Position Y", Range(0, 1)) = 0.5
        _HoleInnerWidth ("Hole Inner Width", Range(0, 1)) = 0.4
        _HoleOuterWidth ("Hole Outer Width", Range(0, 1)) = 0.5
        _PanelColor ("Panel Color", Color) = (0, 0, 0, 0.8)

        // UI masking properties. They are harmless for regular MeshRenderers.
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
            Name "CircularCutout"

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

            CBUFFER_START(UnityPerMaterial)
                float _HolePositionX;
                float _HolePositionY;
                float _HoleInnerWidth;
                float _HoleOuterWidth;
                half4 _PanelColor;
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
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 holePosition = float2(_HolePositionX, _HolePositionY);
                float distanceFromHole = distance(input.uv, holePosition);

                // Sort the radii so runtime-authored values cannot invert the fade.
                float innerRadius = min(_HoleInnerWidth, _HoleOuterWidth);
                float outerRadius = max(_HoleInnerWidth, _HoleOuterWidth);
                float radiusRange = max(outerRadius - innerRadius, 1e-5);
                float fade = saturate((distanceFromHole - innerRadius) / radiusRange);
                fade = fade * fade * (3.0 - 2.0 * fade);

                half4 color = _PanelColor * input.color;
                color.a *= fade;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
