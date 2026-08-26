Shader "EndlessGuard/Raid/CrackScar"
{
    Properties
    {
        _CrackMask("Crack Mask", 2D) = "white" {}
        _StoneDetail("Stone Detail", 2D) = "bump" {}
        _GrooveColor("Groove Color", Color) = (0.004, 0.002, 0.010, 1)
        _RimColor("Broken Rim Color", Color) = (0.11, 0.10, 0.18, 1)
        [HDR]_GlowColor("Glow Color", Color) = (1.15, 0.10, 2.65, 1)
        _GrooveAlpha("Groove Alpha", Range(0, 1)) = 1
        _GlowStrength("Glow Strength", Range(0, 1)) = 0.16
        _RevealProgress("Reveal Progress", Range(0, 1)) = 0
        _RevealSoftness("Reveal Softness", Range(0.002, 0.12)) = 0.028
        _DetailStrength("Detail Strength", Range(0, 0.5)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+25" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "CrackScar"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -2, -2

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CrackMask);
            SAMPLER(sampler_CrackMask);
            TEXTURE2D(_StoneDetail);
            SAMPLER(sampler_StoneDetail);

            CBUFFER_START(UnityPerMaterial)
                float4 _CrackMask_ST;
                float4 _StoneDetail_ST;
                float4 _GrooveColor;
                float4 _RimColor;
                float4 _GlowColor;
                float _GrooveAlpha;
                float _GlowStrength;
                float _RevealProgress;
                float _RevealSoftness;
                float _DetailStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 detailUV : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 detailUV : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.detailUV = input.detailUV;
                output.color = input.color;
                return output;
            }

            float Reveal(float progress)
            {
                float softness = max(0.002, _RevealSoftness);
                return 1.0 - smoothstep(_RevealProgress, _RevealProgress + softness, progress);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 maskUV = saturate(input.detailUV);
                float mask = SAMPLE_TEXTURE2D(_CrackMask, sampler_CrackMask, maskUV).r;
                float3 stone = SAMPLE_TEXTURE2D(_StoneDetail, sampler_StoneDetail, input.detailUV * float2(2.2, 3.7)).rgb;
                float stoneNoise = saturate(abs(stone.r - 0.5) + abs(stone.g - 0.5));
                mask = saturate(mask * lerp(1.0, 0.78 + stoneNoise * 0.5, _DetailStrength));

                float groove = smoothstep(0.035, 0.20, mask);
                float core = smoothstep(0.60, 0.92, mask);
                float rimOuter = smoothstep(0.02, 0.10, mask);
                float rimInner = smoothstep(0.18, 0.34, mask);
                float rim = saturate(rimOuter - rimInner);

                float reveal = Reveal(input.uv.y);
                float softness = max(0.002, _RevealSoftness);
                float headDistance = abs(input.uv.y - _RevealProgress);
                float head = saturate(1.0 - headDistance / (softness * 2.6 + 0.018));
                head *= reveal;

                float glow = saturate(core * (_GlowStrength * 2.4) + head * (_GlowStrength * 1.8));
                float3 color = _GrooveColor.rgb;
                color = lerp(color, _RimColor.rgb, rim * 0.42);
                color += _GlowColor.rgb * glow;

                float alpha = reveal * input.color.a;
                alpha *= saturate(groove * _GrooveAlpha + rim * 0.24 + core * _GlowStrength * 0.35 + head * _GlowStrength * 0.18);

                clip(alpha - 0.018);
                return half4(color, alpha * _GrooveColor.a);
            }
            ENDHLSL
        }
    }
}
