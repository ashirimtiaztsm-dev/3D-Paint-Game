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

        [Header(Organic Edge Noise)]
        _BrushNoiseTex ("Brush Edge Noise", 2D) = "white" {}
        _BrushNoiseScale ("Brush Edge Noise Scale", Float) = 8
        _BrushNoiseStrength ("Brush Edge Noise Strength", Range(0, 1)) = 0

        [Header(Jelly Thickness Deposit)]
        _JellyDomePower ("Jelly Dome Power", Range(0.3, 1.5)) = 0.7
        _BlobMergeSoftness ("Blob Merge Softness", Range(0.001, 0.5)) = 0.12
        _ThicknessBuildRate ("Thickness Build Rate", Range(0, 1)) = 0.35
        _MaximumThickness ("Maximum Thickness", Range(0.5, 2)) = 1.0
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
            TEXTURE2D(_BrushNoiseTex);
            SAMPLER(sampler_BrushNoiseTex);

            float4 _BrushUV;
            float _BrushRadius;
            float _BrushHardness;
            half4 _BrushColor;
            float _BrushOpacity;
            float _BrushNoiseScale;
            float _BrushNoiseStrength;
            float _JellyDomePower;
            float _BlobMergeSoftness;
            float _ThicknessBuildRate;
            float _MaximumThickness;

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

            // Classic polynomial smooth-min (IQ), then smooth-max via sign flip. No loops, two cheap
            // scalar ops — safe for a per-pixel fragment shader with no derivative/branch cost.
            float SmoothMin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / max(k, 1e-5));
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            float SmoothMax(float a, float b, float k)
            {
                return -SmoothMin(-a, -b, k);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 previous = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float distanceFromBrush = distance(input.uv, _BrushUV.xy);

                // Organic outer edge: perturb the effective radius using a fixed noise texture in
                // surface UV space (same point on the surface always gets the same offset — no
                // per-frame shimmer), concentrated near the edge only.
                float edgeNoise = SAMPLE_TEXTURE2D(_BrushNoiseTex, sampler_BrushNoiseTex, input.uv * _BrushNoiseScale).r - 0.5;
                float edgeInfluence = smoothstep(_BrushRadius * 0.5, _BrushRadius, distanceFromBrush);
                float effectiveRadius = max(_BrushRadius * 0.05, _BrushRadius + edgeNoise * edgeInfluence * _BrushNoiseStrength * _BrushRadius);

                // Domed liquid deposit: peaks at 1.0 exactly at the stamp centre (always solid there)
                // and falls off smoothly with no flat plateau and no hard vertical edge.
                float distance01 = saturate(distanceFromBrush / effectiveRadius);
                float dome = pow(saturate(1.0 - distance01 * distance01), _JellyDomePower);

                // Per-pixel target clipping: a stamp can straddle more than one region, so this must be
                // sampled at every pixel, never just checked once at the brush centre. This is the final,
                // authoritative gate — wrong-colour / out-of-region paint is rejected here regardless of
                // dome shape or noise.
                float allowed = SAMPLE_TEXTURE2D(_AllowedMask, sampler_AllowedMask, input.uv).r;

                float incomingThickness = saturate(dome * _BrushOpacity) * allowed;

                // Smooth-union the incoming dome with whatever thickness is already there, plus a small
                // extra build-up term where both overlap, so repeated/overlapping spray reads as
                // thickening liquid rather than a flat re-stamped circle. Clamped to a safe maximum.
                float merged = SmoothMax(previous.a, incomingThickness, _BlobMergeSoftness);
                float build = previous.a * incomingThickness * _ThicknessBuildRate;
                half blendedAlpha = (half)saturate(min(merged + build, _MaximumThickness));

                half3 blendedColor = lerp(previous.rgb, _BrushColor.rgb, saturate(incomingThickness));

                return half4(blendedColor, blendedAlpha);
            }
            ENDHLSL
        }
    }
}
