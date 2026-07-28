Shader "Hidden/ColorGame/PaintBrush"
{
    Properties
    {
        _MainTex ("Previous Paint Texture", 2D) = "black" {}
        _BrushUV ("Brush UV", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius", Float) = 0.05
        _BrushHardness ("Brush Hardness", Range(0, 1)) = 0.8
        _BrushColor ("Brush Color", Color) = (1, 1, 1, 1)
        _BrushOpacity ("Brush Opacity", Range(0, 1)) = 1
        _AllowedMask ("Allowed Paint Mask", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AllowedMask);
            SAMPLER(sampler_AllowedMask);

            float4 _BrushUV;
            float _BrushRadius;
            float _BrushHardness;
            half4 _BrushColor;
            float _BrushOpacity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 previous = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float distanceFromBrush = distance(input.uv, _BrushUV.xy);
                float innerRadius = _BrushRadius * saturate(_BrushHardness);
                float mask = 1.0 - smoothstep(
                    innerRadius,
                    max(innerRadius + 0.00001, _BrushRadius),
                    distanceFromBrush);

                // Per-pixel target clipping: a stamp can straddle more than one region, so this must be
                // sampled at every pixel, never just checked once at the brush centre.
                float allowed = SAMPLE_TEXTURE2D(_AllowedMask, sampler_AllowedMask, input.uv).r;

                float stampAlpha = saturate(mask * _BrushOpacity * allowed);

                half3 blendedColor =
                    lerp(previous.rgb, _BrushColor.rgb, stampAlpha);

                half blendedAlpha =
                    saturate(previous.a + stampAlpha * (1.0 - previous.a));

                return half4(blendedColor, blendedAlpha);
            }
            ENDHLSL
        }
    }
}
