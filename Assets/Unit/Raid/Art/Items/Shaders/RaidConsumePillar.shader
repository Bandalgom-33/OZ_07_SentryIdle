Shader "EndlessGuard/Raid/Consume Pillar"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (0.5, 0.2, 2.0, 1)
        _EdgeColor("Edge Color", Color) = (0.2, 0.05, 1.0, 1)
        _Intensity("Intensity", Range(0, 5)) = 1.8
        _Opacity("Opacity", Range(0, 1)) = 0.55
        _FlowSpeed("Flow Speed", Range(0, 8)) = 2.0
        _Style("Style", Range(0, 2)) = 0
        [HideInInspector] _VFXStartTime("VFX Start Time", Float) = 0
        [HideInInspector] _VFXDuration("VFX Duration", Float) = 0.65
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+25" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "RaidConsumePillar"
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
                half4 _CoreColor;
                half4 _EdgeColor;
                half _Intensity;
                half _Opacity;
                half _FlowSpeed;
                half _Style;
                float _VFXStartTime;
                float _VFXDuration;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half x = input.uv.x * 2.0h - 1.0h;
                half y = saturate(input.uv.y);
                half duration = max(0.05h, (half)_VFXDuration);
                half progress = saturate(((half)_Time.y - (half)_VFXStartTime) / duration);
                half fade = smoothstep(0.0h, 0.07h, progress) * (1.0h - smoothstep(0.72h, 1.0h, progress));
                half speedWeight = step(0.5h, _Style);
                half healWeight = step(1.5h, _Style);

                half rise = smoothstep(progress - 0.26h, progress + 0.08h, y);
                half reveal = 1.0h - rise;
                half topFade = 1.0h - smoothstep(0.72h, 1.0h, y);
                half width = lerp(0.68h, 0.42h, y) * lerp(1.0h, 0.82h, progress);
                half body = 1.0h - smoothstep(width * 0.68h, width, abs(x));
                half edge = 1.0h - smoothstep(0.025h, 0.085h, abs(abs(x) - width * 0.84h));

                half flow = frac(y * lerp(3.0h, 5.2h, speedWeight) - progress * _FlowSpeed * lerp(2.2h, 4.1h, speedWeight) + x * 0.43h);
                half streak = pow(saturate(1.0h - abs(flow - 0.5h) * 2.0h), lerp(9.0h, 17.0h, speedWeight));
                half side = pow(saturate(abs(sin((x * lerp(6.0h, 10.0h, speedWeight) + progress * 4.0h) * 3.14159265h))), lerp(8.0h, 16.0h, speedWeight));
                half healingCore = pow(saturate(1.0h - abs(x) / max(0.001h, width)), 3.0h) * healWeight * 0.42h;
                half energy = (edge * 0.58h + streak * side * 0.95h + healingCore) * body * reveal * topFade;
                half flash = smoothstep(0.42h, 0.64h, progress) * (1.0h - smoothstep(0.72h, 0.92h, progress));
                energy += flash * pow(saturate(1.0h - abs(x) / 0.42h), 4.0h) * (1.0h - y) * 0.55h;

                half alpha = saturate(energy) * _Opacity * fade;
                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(streak + flash * 0.7h));
                color *= max(0.0h, _Intensity) * (0.55h + energy) * fade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
