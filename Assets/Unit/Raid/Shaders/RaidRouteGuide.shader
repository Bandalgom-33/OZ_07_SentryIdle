Shader "EndlessGuard/Raid/Route Guide"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.10, 0.18, 0.85, 1)
        _PulseColor("Pulse Color", Color) = (0.48, 0.82, 1.65, 1)
        _LayerColor("Layer Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Range(0, 3)) = 1
        _LayerIntensity("Layer Intensity", Range(0, 3)) = 1
        _LayerAlpha("Layer Alpha", Range(0, 1)) = 1
        _Visibility("Visibility", Range(0, 1)) = 1
        _FlowSpeed("Flow Speed", Range(0, 4)) = 1.15
        _PulseSpacing("Pulse Spacing", Range(0.25, 8)) = 2.2
        _PulseWidth("Pulse Width", Range(0.04, 0.8)) = 0.24
        _PulseBoost("Pulse Boost", Range(0, 3)) = 1
        _BaseGlow("Base Glow", Range(0, 1)) = 0.16
        _EdgeSoftness("Edge Softness", Range(0.01, 0.5)) = 0.22
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+25" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "RaidRouteGuide"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _PulseColor;
                float4 _LayerColor;
                float _Intensity;
                float _LayerIntensity;
                float _LayerAlpha;
                float _Visibility;
                float _FlowSpeed;
                float _PulseSpacing;
                float _PulseWidth;
                float _PulseBoost;
                float _BaseGlow;
                float _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float center = 1.0 - abs(input.uv.y * 2.0 - 1.0);
                float edge = smoothstep(0.0, max(0.01, _EdgeSoftness), center);
                float spacing = max(0.25, _PulseSpacing);
                float phase = frac(input.uv.x / spacing - _Time.y * _FlowSpeed);
                float pulseWidth = max(0.04, _PulseWidth);
                float pulseHead = 1.0 - smoothstep(0.0, pulseWidth, phase);
                float pulseTail = 1.0 - smoothstep(pulseWidth, min(0.98, pulseWidth * 3.2), phase);
                float pulse = saturate(pulseHead * 0.78 + pulseTail * 0.36);
                float secondaryPhase = frac(input.uv.x / (spacing * 1.85) - _Time.y * (_FlowSpeed * 0.58) + 0.47);
                float secondary = 1.0 - smoothstep(0.0, pulseWidth * 1.45, secondaryPhase);
                float shimmer = 0.92 + 0.08 * sin(_Time.y * 5.4 + input.uv.x * 1.7);
                float energy = (_BaseGlow + pulse * _PulseBoost + secondary * 0.25) * shimmer;
                float visibility = saturate(_Visibility) * input.color.a;
                float alpha = saturate(edge * energy * _LayerAlpha * visibility);
                float coreBlend = saturate(center * 0.72 + pulse * 0.48);
                float3 baseColor = _BaseColor.rgb * _LayerColor.rgb;
                float3 pulseColor = _PulseColor.rgb * _LayerColor.rgb;
                float3 color = lerp(baseColor, pulseColor, coreBlend);
                color *= max(0.0, _Intensity) * max(0.0, _LayerIntensity) * (0.54 + energy * 0.82) * visibility;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
