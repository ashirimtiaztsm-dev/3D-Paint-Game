Shader "ColorGame/PaintableSurface"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _PaintTex ("Paint Texture", 2D) = "black" {}

        [Header(Paint Coverage)]
        _PaintCoverageStart ("Paint Coverage Start", Range(0, 1)) = 0.04
        _PaintCoverageFull ("Paint Coverage Full", Range(0, 1)) = 0.20
        _BaseSmoothness ("Base Smoothness", Range(0, 1)) = 0.15

        [Header(Jelly Height and Normal)]
        _JellyHeight ("Jelly Height", Range(0, 2)) = 0.9
        _JellyNormalStrength ("Jelly Normal Strength", Range(0, 10)) = 6
        _JellySmoothingRadius ("Jelly Smoothing Radius (texels)", Range(1, 6)) = 3

        [Header(Meniscus)]
        _MeniscusWidth ("Meniscus Width", Range(0.01, 0.3)) = 0.12
        _MeniscusStrength ("Meniscus Strength", Range(0, 2)) = 0.75
        _MeniscusSmoothness ("Meniscus Smoothness", Range(0, 1)) = 0.9
        _MeniscusTint ("Meniscus Tint", Color) = (1, 1, 1, 1)

        [Header(Jelly Lighting)]
        _JellySmoothness ("Jelly Smoothness", Range(0, 1)) = 0.92
        _JellySpecularStrength ("Jelly Specular Strength", Range(0, 2)) = 0.85
        _JellyBroadSpecularPower ("Jelly Broad Specular Power", Range(4, 64)) = 28
        _JellySharpSpecularPower ("Jelly Sharp Specular Power", Range(32, 256)) = 110
        _JellyFresnelStrength ("Jelly Fresnel Strength", Range(0, 1)) = 0.3
        _JellyFresnelPower ("Jelly Fresnel Power", Range(1, 8)) = 4
        _JellyDepthDarkening ("Jelly Depth Darkening", Range(0, 0.5)) = 0.12
        _JellyInternalGlow ("Jelly Internal Glow", Range(0, 0.5)) = 0.1

        [Header(Internal Moving Pattern)]
        _JellyNoiseScaleA ("Jelly Noise Scale A", Float) = 5
        _JellyNoiseScaleB ("Jelly Noise Scale B", Float) = 9
        _JellyNoiseSpeedA ("Jelly Noise Speed A", Float) = 0.03
        _JellyNoiseSpeedB ("Jelly Noise Speed B", Float) = 0.02
        _JellyNoiseStrength ("Jelly Noise Strength", Range(0, 1)) = 0.1
        _JellyHighlightVariation ("Jelly Highlight Variation", Range(0, 1)) = 0.12

        [Header(Liquid Surface Noise Texture)]
        _LiquidNoiseTex ("Liquid Noise", 2D) = "gray" {}

        [Header(Impact Ripple)]
        _ImpactUV ("Impact UV", Vector) = (-10, -10, 0, 0)
        _ImpactStartTime ("Impact Start Time", Float) = -1000
        _ImpactStrength ("Impact Strength", Range(0, 2)) = 0
        _ImpactRippleFrequency ("Impact Ripple Frequency", Float) = 90
        _ImpactRippleSpeed ("Impact Ripple Speed", Float) = 10
        _ImpactRippleDecay ("Impact Ripple Decay", Float) = 3
        _ImpactRippleRadius ("Impact Ripple Radius", Range(0.02, 0.5)) = 0.18

        [Header(Target Guide)]
        _TargetGuideTex ("Target Guide", 2D) = "black" {}
        _HasTargetGuide ("Has Target Guide", Range(0, 1)) = 0
        _GuideOpacity ("Guide Opacity", Range(0, 1)) = 1
        _GuideOutlineStrength ("Guide Outline Strength", Range(0, 2)) = 0.6
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PaintTex);
            SAMPLER(sampler_PaintTex);
            TEXTURE2D(_LiquidNoiseTex);
            SAMPLER(sampler_LiquidNoiseTex);
            TEXTURE2D(_TargetGuideTex);
            SAMPLER(sampler_TargetGuideTex);

            float4 _PaintTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _PaintCoverageStart;
                half _PaintCoverageFull;
                half _BaseSmoothness;
                half _JellyHeight;
                half _JellyNormalStrength;
                half _JellySmoothingRadius;
                half _MeniscusWidth;
                half _MeniscusStrength;
                half _MeniscusSmoothness;
                half4 _MeniscusTint;
                half _JellySmoothness;
                half _JellySpecularStrength;
                half _JellyBroadSpecularPower;
                half _JellySharpSpecularPower;
                half _JellyFresnelStrength;
                half _JellyFresnelPower;
                half _JellyDepthDarkening;
                half _JellyInternalGlow;
                half _JellyNoiseScaleA;
                half _JellyNoiseScaleB;
                half _JellyNoiseSpeedA;
                half _JellyNoiseSpeedB;
                half _JellyNoiseStrength;
                half _JellyHighlightVariation;
                float4 _ImpactUV;
                float _ImpactStartTime;
                half _ImpactStrength;
                half _ImpactRippleFrequency;
                half _ImpactRippleSpeed;
                half _ImpactRippleDecay;
                half _ImpactRippleRadius;
                half _HasTargetGuide;
                half _GuideOpacity;
                half _GuideOutlineStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 baseUV : TEXCOORD0;
                float2 paintUV : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.baseUV = TRANSFORM_TEX(input.uv, _BaseMap);
                output.paintUV = input.uv;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseColor =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV) * _BaseColor;

                // Paint alpha is stored as jelly thickness (0.._MaximumThickness from the brush shader),
                // not directly as visible opacity — coverage below is the separate visual-opacity curve.
                half4 paint = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV);
                float thickness = paint.a;
                float coverage = smoothstep(_PaintCoverageStart, _PaintCoverageFull, thickness);

                // ---- Height sampling: a locally-tight tap set (fine meniscus edge) plus a much wider
                // tap set (broad puddle mound), both fixed-count, no loops, no extra render targets.
                float2 texel = _PaintTex_TexelSize.xy;

                float aRight = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV + float2(texel.x, 0)).a;
                float aLeft = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV - float2(texel.x, 0)).a;
                float aUp = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV + float2(0, texel.y)).a;
                float aDown = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV - float2(0, texel.y)).a;

                float localGradX = (aRight - aLeft) * 0.5;
                float localGradY = (aUp - aDown) * 0.5;
                float localGradMagnitude = length(float2(localGradX, localGradY));

                float2 wideOffset = texel * _JellySmoothingRadius;
                float hRight = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV + float2(wideOffset.x, 0)).a;
                float hLeft = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV - float2(wideOffset.x, 0)).a;
                float hUp = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV + float2(0, wideOffset.y)).a;
                float hDown = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV - float2(0, wideOffset.y)).a;

                float wideGradX = (hRight - hLeft) * 0.5;
                float wideGradY = (hUp - hDown) * 0.5;

                // Broad mound shape dominates, local gradient adds the fine meniscus edge detail.
                float combinedGradX = wideGradX + localGradX * 0.5;
                float combinedGradY = wideGradY + localGradY * 0.5;

                // ---- Jelly fake normal ----
                float3 worldNormal = normalize(input.normalWS);
                float3 upHelper = (abs(worldNormal.y) < 0.99) ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 worldTangent = normalize(cross(upHelper, worldNormal));
                float3 worldBitangent = cross(worldNormal, worldTangent);

                float3 jellyNormal = normalize(worldNormal -
                    (combinedGradX * worldTangent + combinedGradY * worldBitangent) * _JellyNormalStrength * _JellyHeight);

                // ---- Internal moving liquid pattern (two independently scaled/scrolled noise taps) ----
                float2 dirA = normalize(float2(1.0, 0.35));
                float2 dirB = normalize(float2(-0.4, 1.0));
                float2 noiseUVA = input.paintUV * _JellyNoiseScaleA + dirA * _Time.y * _JellyNoiseSpeedA;
                float2 noiseUVB = input.paintUV * _JellyNoiseScaleB + dirB * _Time.y * _JellyNoiseSpeedB;

                float noiseA = SAMPLE_TEXTURE2D(_LiquidNoiseTex, sampler_LiquidNoiseTex, noiseUVA).r;
                float noiseB = SAMPLE_TEXTURE2D(_LiquidNoiseTex, sampler_LiquidNoiseTex, noiseUVB).r;
                float combinedNoise = (noiseA + noiseB) * 0.5;
                float internalPattern = abs(noiseA - noiseB);

                float noiseNormalPerturb = (combinedNoise - 0.5) * _JellyNoiseStrength;
                jellyNormal = normalize(jellyNormal
                    + worldTangent * noiseNormalPerturb * coverage * 0.5
                    + worldBitangent * noiseNormalPerturb * coverage * 0.3);

                float highlightVariation = 1.0 + (combinedNoise - 0.5) * _JellyHighlightVariation;

                // ---- Impact ripple: localized, time-decaying, never touches the paint mask itself ----
                float impactAge = max(_Time.y - _ImpactStartTime, 0.0);
                float distanceFromImpact = distance(input.paintUV, _ImpactUV.xy);
                float radialFalloff = saturate(1.0 - distanceFromImpact / max(_ImpactRippleRadius, 1e-4));
                float temporalDecay = exp(-impactAge * _ImpactRippleDecay);
                float ripple = sin(distanceFromImpact * _ImpactRippleFrequency - impactAge * _ImpactRippleSpeed)
                    * radialFalloff * temporalDecay * coverage * _ImpactStrength;

                jellyNormal = normalize(jellyNormal
                    + worldTangent * ripple * _JellyNormalStrength * 0.15
                    + worldBitangent * ripple * _JellyNormalStrength * 0.1);

                // ---- Lighting ----
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(lightDir + viewDir);

                float NdotL = saturate(dot(jellyNormal, lightDir));
                float NdotH = saturate(dot(jellyNormal, halfDir));
                float NdotV = saturate(dot(jellyNormal, viewDir));

                float broadSpec = pow(NdotH, _JellyBroadSpecularPower) * NdotL;
                float sharpSpec = pow(NdotH, _JellySharpSpecularPower) * NdotL;
                float specTotal = (broadSpec * 0.6 + sharpSpec * 0.4) * _JellySpecularStrength * coverage * highlightVariation;
                specTotal *= (1.0 + ripple * 0.5);

                float fresnel = pow(1.0 - NdotV, _JellyFresnelPower) * _JellyFresnelStrength * coverage;

                float depthDarken = coverage * saturate(thickness) * _JellyDepthDarkening;
                depthDarken *= (1.0 - ripple * 0.2);

                // Final visible opacity uses COVERAGE (a shaped curve), never raw thickness directly —
                // this is what keeps thin edges rounded/translucent while thick centres read as solid.
                half3 finalColor = lerp(baseColor.rgb, paint.rgb, coverage);
                finalColor *= (1.0 - saturate(depthDarken));
                finalColor += (specTotal + fresnel) * mainLight.color;
                finalColor += internalPattern * _JellyInternalGlow * coverage;

                // ---- Rounded meniscus: brightens exactly where local paint alpha changes sharply (the
                // stamped blob's own boundary), gated by coverage so it can never show outside paint.
                float meniscusRaw = smoothstep(0.0, max(_MeniscusWidth, 1e-4), localGradMagnitude);
                float meniscusShaped = pow(meniscusRaw, lerp(4.0, 1.0, saturate(_MeniscusSmoothness)));
                float meniscus = meniscusShaped * _MeniscusStrength * coverage;
                finalColor += meniscus * _MeniscusTint.rgb;

                // ---- Target guide overlay: gated by an explicit toggle (never texture-default alpha),
                // fades out under paint via coverage instead of raw thickness.
                half4 guideSample = SAMPLE_TEXTURE2D(_TargetGuideTex, sampler_TargetGuideTex, input.paintUV);
                float guideVisibility = guideSample.a * _GuideOpacity * (1.0h - coverage) * _HasTargetGuide;
                float3 guideColor = guideSample.rgb * (1.0h + _GuideOutlineStrength * guideSample.a);
                finalColor = lerp(finalColor, guideColor, saturate(guideVisibility));

                return half4(saturate(finalColor), 1.0h);
            }
            ENDHLSL
        }
    }
}
