Shader "ColorGame/PaintableSurface"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _PaintTex ("Paint Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PaintTex);
            SAMPLER(sampler_PaintTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 baseUV : TEXCOORD0;
                float2 paintUV : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.baseUV = TRANSFORM_TEX(input.uv, _BaseMap);
                output.paintUV = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseColor =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV) * _BaseColor;

                half4 paint =
                    SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV);

                half3 finalColor = lerp(baseColor.rgb, paint.rgb, paint.a);
                return half4(finalColor, 1.0h);
            }
            ENDHLSL
        }
    }
}
