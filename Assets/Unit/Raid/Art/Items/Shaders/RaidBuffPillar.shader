Shader "EndlessGuard/Raid/Buff Pillar"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (0.5, 0.2, 2.0, 1)
        _EdgeColor("Edge Color", Color) = (0.2, 0.05, 1.0, 1)
        _Intensity("Intensity", Range(0, 4)) = 1.0
        _Opacity("Opacity", Range(0, 1)) = 0.30
        _FlowSpeed("Flow Speed", Range(0, 6)) = 1.0
        _Style("Style", Range(0, 1)) = 0
        [HideInInspector] _BuffStartTime("Buff Start Time", Float) = 0
        [HideInInspector] _BuffEndTime("Buff End Time", Float) = 0
        [HideInInspector] _StackNormalized("Stack Normalized", Range(0, 1)) = 0
        [HideInInspector] _StackCount("Stack Count", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+21" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "RaidBuffPillar"
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
                float _BuffStartTime;
                float _BuffEndTime;
                half _StackNormalized;
                half _StackCount;
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
                half2 uv = input.uv;
                half x = uv.x * 2.0h - 1.0h;
                half y = saturate(uv.y);
                half time = (half)_Time.y;

                half timed = _BuffEndTime > _BuffStartTime + 0.001f ? 1.0h : 0.0h;
                half duration = max(0.001h, (half)(_BuffEndTime - _BuffStartTime));
                half elapsed = max(0.0h, time - (half)_BuffStartTime);
                half remaining = max(0.0h, (half)_BuffEndTime - time);
                half fadeIn = smoothstep(0.0h, min(0.18h, duration * 0.18h), elapsed);
                half fadeOut = smoothstep(0.0h, min(0.30h, duration * 0.18h), remaining);
                half life = lerp(1.0h, min(fadeIn, fadeOut), timed);

                half speedWeight = step(0.5h, _Style);
                half stack = saturate(_StackNormalized);
                half width = lerp(0.70h, 0.52h, y) + 0.045h * sin(y * 9.0h + time * lerp(1.4h, 3.1h, speedWeight));
                half ax = abs(x);
                half body = 1.0h - smoothstep(width * 0.72h, width, ax);
                half edge = (1.0h - smoothstep(0.025h, 0.085h, abs(ax - width * 0.86h))) * body;
                half verticalFade = smoothstep(0.0h, 0.13h, y) * (1.0h - smoothstep(0.76h, 1.0h, y));

                half flowSpeed = _FlowSpeed * lerp(0.75h, 1.75h, speedWeight);
                half flowA = frac(y * lerp(2.3h, 4.2h, speedWeight) - time * flowSpeed + x * 0.37h);
                half flowB = frac(y * lerp(3.1h, 5.8h, speedWeight) - time * flowSpeed * 1.31h - x * 0.51h + 0.37h);
                half streakA = pow(saturate(1.0h - abs(flowA - 0.5h) * 2.0h), lerp(9.0h, 18.0h, speedWeight));
                half streakB = pow(saturate(1.0h - abs(flowB - 0.5h) * 2.0h), lerp(12.0h, 22.0h, speedWeight));
                half xBands = pow(saturate(abs(sin((x * lerp(5.0h, 9.0h, speedWeight) + y * 1.7h + time * flowSpeed * 0.45h) * 3.14159265h))), lerp(10.0h, 18.0h, speedWeight));
                half streams = (streakA * 0.58h + streakB * 0.42h) * (0.36h + 0.64h * xBands) * body;

                half baseGlow = pow(saturate(1.0h - ax / max(0.001h, width)), 2.2h) * (1.0h - y) * 0.22h;
                half topWisps = streams * smoothstep(0.18h, 0.52h, y) * (1.0h - smoothstep(0.82h, 1.0h, y));
                half stackPulse = 1.0h + stack * (0.18h + 0.07h * sin(time * 4.2h));
                half energy = (edge * lerp(0.58h, 0.42h, speedWeight) + topWisps * lerp(0.72h, 1.05h, speedWeight) + baseGlow) * verticalFade * stackPulse;

                half alpha = saturate(energy) * _Opacity * life;
                half hot = saturate(topWisps * 0.85h + edge * 0.42h);
                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, hot);
                color *= max(0.0h, _Intensity) * (0.55h + energy) * life;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
