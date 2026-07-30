Shader "ColorGame/PaintableSurface"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _PaintTex ("Paint Texture", 2D) = "black" {}

        [Header(Liquid Paint Lighting)]
        _BaseSmoothness ("Base Smoothness", Range(0, 1)) = 0.15
        _PaintSmoothness ("Paint Smoothness", Range(0, 1)) = 0.75
        _PaintSpecularStrength ("Paint Specular Strength", Range(0, 4)) = 1.2
        _PaintNormalStrength ("Paint Fake-Normal Strength", Range(0, 2)) = 0.6

        [Header(Wet Edge Highlight)]
        _PaintEdgeHighlightStrength ("Paint Edge Highlight Strength", Range(0, 4)) = 1.5
        _PaintEdgeWidth ("Paint Edge Width", Range(0.001, 0.2)) = 0.02

        [Header(Liquid Surface Noise)]
        _LiquidNoiseTex ("Liquid Noise", 2D) = "gray" {}
        _LiquidNoiseScale ("Liquid Noise Scale", Float) = 6
        _LiquidNoiseStrength ("Liquid Noise Strength", Range(0, 1)) = 0.12
        _LiquidNoiseSpeed ("Liquid Noise Speed", Float) = 0.05

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
                half _BaseSmoothness;
                half _PaintSmoothness;
                half _PaintSpecularStrength;
                half _PaintNormalStrength;
                half _PaintEdgeHighlightStrength;
                half _PaintEdgeWidth;
                half _LiquidNoiseScale;
                half _LiquidNoiseStrength;
                half _LiquidNoiseSpeed;
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

                half4 paint =
                    SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV);

                half3 finalColor = lerp(baseColor.rgb, paint.rgb, paint.a);

                // 5-tap gradient of paint alpha in UV space: gives a cheap raised/wet-edge "fake
                // normal" and a meniscus highlight without needing an authored normal map.
                float2 texel = _PaintTex_TexelSize.xy;
                float aRight = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV + float2(texel.x, 0)).a;
                float aLeft = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV - float2(texel.x, 0)).a;
                float aUp = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV + float2(0, texel.y)).a;
                float aDown = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, input.paintUV - float2(0, texel.y)).a;

                float gradX = (aRight - aLeft) * 0.5;
                float gradY = (aUp - aDown) * 0.5;
                float gradMagnitude = length(float2(gradX, gradY));

                float3 worldNormal = normalize(input.normalWS);
                float3 upHelper = (abs(worldNormal.y) < 0.99) ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 worldTangent = normalize(cross(upHelper, worldNormal));
                float3 worldBitangent = cross(worldNormal, worldTangent);

                float3 bumpNormal = normalize(
                    worldNormal - (gradX * worldTangent + gradY * worldBitangent) * _PaintNormalStrength);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(lightDir + viewDir);

                float NdotL = saturate(dot(bumpNormal, lightDir));
                float NdotH = saturate(dot(bumpNormal, halfDir));

                half smoothness = lerp(_BaseSmoothness, _PaintSmoothness, paint.a);
                float specPower = exp2(smoothness * 10.0h + 1.0h);
                float specStrength = _PaintSpecularStrength * paint.a;
                float spec = pow(NdotH, specPower) * specStrength * NdotL;

                finalColor += spec * mainLight.color;

                // Wet meniscus rim: brightens exactly where the paint alpha changes sharply (the
                // edge of a stamped blob), independent of the specular highlight above.
                float rimMask = smoothstep(0.0, max(_PaintEdgeWidth, 1e-4), gradMagnitude);
                float rim = rimMask * _PaintEdgeHighlightStrength * paint.a;
                finalColor += rim * mainLight.color;

                // Subtle scrolling liquid noise, painted areas only. Neutral-gray default texture
                // makes this a no-op if no noise texture is assigned.
                float2 noiseUV = input.paintUV * _LiquidNoiseScale + _Time.y * _LiquidNoiseSpeed;
                float noiseSample = SAMPLE_TEXTURE2D(_LiquidNoiseTex, sampler_LiquidNoiseTex, noiseUV).r;
                finalColor += (noiseSample - 0.5h) * _LiquidNoiseStrength * paint.a;

                // Target guide overlay: only applied when a real guide texture is bound
                // (_HasTargetGuide gate), and fades out under paint via guideSample.a * (1 - paint.a).
                half4 guideSample = SAMPLE_TEXTURE2D(_TargetGuideTex, sampler_TargetGuideTex, input.paintUV);
                float guideVisibility = guideSample.a * _GuideOpacity * (1.0h - paint.a) * _HasTargetGuide;
                float3 guideColor = guideSample.rgb * (1.0h + _GuideOutlineStrength * guideSample.a);
                finalColor = lerp(finalColor, guideColor, saturate(guideVisibility));

                return half4(saturate(finalColor), 1.0h);
            }
            ENDHLSL
        }
    }
}
