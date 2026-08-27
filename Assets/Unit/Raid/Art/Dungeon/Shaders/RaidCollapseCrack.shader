Shader "EndlessGuard/Raid/Collapse Crack"
{
    Properties
    {
        _OuterColor("Outer Color", Color) = (0.15, 0.018, 0.34, 1)
        _CoreColor("Core Color", Color) = (0.82, 0.36, 1.24, 1)
        _Intensity("Intensity", Range(0, 2)) = 0
        _RevealProgress("Reveal Progress", Range(0, 1)) = 0
        _RevealSoftness("Reveal Softness", Range(0.002, 0.12)) = 0.035
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+5" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "RaidCollapseCrack"
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
                float4 _OuterColor;
                float4 _CoreColor;
                float _Intensity;
                float _RevealProgress;
                float _RevealSoftness;
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
                float center = saturate(1.0 - abs(input.uv.x * 2.0 - 1.0));
                float core = smoothstep(0.50, 0.95, center);
                float edge = smoothstep(0.04, 0.62, center);

                float softness = max(0.002, _RevealSoftness);
                float reveal = 1.0 - smoothstep(_RevealProgress, _RevealProgress + softness, input.uv.y);
                float headDistance = abs(input.uv.y - _RevealProgress);
                float head = saturate(1.0 - headDistance / (softness * 3.2 + 0.018));
                head *= reveal;

                float intensity = max(0.0, _Intensity) * input.color.a * reveal;
                float alpha = saturate((edge * 0.30 + core * 0.74) * intensity);
                float3 color = lerp(_OuterColor.rgb, _CoreColor.rgb, core) * intensity;
                color += _CoreColor.rgb * head * max(0.0, _Intensity) * input.color.a * 0.48;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
