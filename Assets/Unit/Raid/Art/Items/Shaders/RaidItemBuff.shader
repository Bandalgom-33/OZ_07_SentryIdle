Shader "EndlessGuard/Raid/Item Buff"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (0.4, 0.8, 2.0, 1)
        _EdgeColor("Edge Color", Color) = (0.5, 0.1, 1.2, 1)
        _Intensity("Intensity", Range(0, 4)) = 1.2
        _Opacity("Opacity", Range(0, 1)) = 0.65
        _SpinSpeed("Spin Speed", Range(-2, 2)) = 0.35
        _PulseSpeed("Pulse Speed", Range(0, 8)) = 2.0
        _Style("Style", Range(0, 1)) = 0
        [HideInInspector] _BuffStartTime("Buff Start Time", Float) = 0
        [HideInInspector] _BuffEndTime("Buff End Time", Float) = 0
        [HideInInspector] _StackNormalized("Stack Normalized", Range(0, 1)) = 0
        [HideInInspector] _StackCount("Stack Count", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "RaidItemBuff"
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
                half _SpinSpeed;
                half _PulseSpeed;
                half _Style;
                float _BuffStartTime;
                float _BuffEndTime;
                half _StackNormalized;
                half _StackCount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half Band(half value, half center, half width, half softness)
            {
                return 1.0h - smoothstep(width, width + softness, abs(value - center));
            }

            half SoftSegment(half phase, half center, half width)
            {
                half d = abs(frac(phase - center + 0.5h) - 0.5h);
                return 1.0h - smoothstep(width, width + 0.035h, d);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                const half InvTwoPi = 0.15915494h;
                half2 p = input.uv * 2.0h - 1.0h;
                half radius = length(p);
                half angle = atan2(p.y, p.x);
                half angle01 = frac(angle * InvTwoPi + 0.5h);
                half time = (half)_Time.y;

                half timed = _BuffEndTime > _BuffStartTime + 0.001f ? 1.0h : 0.0h;
                half duration = max(0.001h, (half)(_BuffEndTime - _BuffStartTime));
                half elapsed = max(0.0h, time - (half)_BuffStartTime);
                half remaining = max(0.0h, (half)_BuffEndTime - time);
                half normalizedRemaining = saturate(remaining / duration);
                half fadeIn = smoothstep(0.0h, min(0.18h, duration * 0.18h), elapsed);
                half fadeOut = smoothstep(0.0h, min(0.32h, duration * 0.20h), remaining);
                half life = lerp(1.0h, min(fadeIn, fadeOut), timed);

                half styleSpeed = lerp(0.72h, 1.55h, saturate(_Style));
                half spin = time * _SpinSpeed * styleSpeed;
                half stackBoost = 1.0h + saturate(_StackNormalized) * 0.24h;
                half pulse = 0.92h + (0.08h + saturate(_StackNormalized) * 0.035h) * sin(time * _PulseSpeed * 6.2831853h);

                half attackWeight = 1.0h - step(0.5h, _Style);
                half speedWeight = step(0.5h, _Style);

                half attackOuter = Band(radius, 0.79h, 0.024h, 0.038h);
                half attackInner = Band(radius, 0.57h, 0.018h, 0.030h);
                half attackPhase = frac((angle01 + spin * 0.115h) * 4.0h);
                half attackBlade = SoftSegment(attackPhase, 0.5h, 0.075h) * smoothstep(0.57h, 0.66h, radius) * (1.0h - smoothstep(0.92h, 1.0h, radius));
                half attackVortex = (0.5h + 0.5h * sin(angle * 5.0h - spin * 7.0h + radius * 13.0h)) * Band(radius, 0.68h, 0.17h, 0.09h);
                half attackRune = pow(saturate(abs(cos(angle * 3.0h + spin * 2.4h))), 10.0h) * Band(radius, 0.46h, 0.045h, 0.045h);
                half attackSigilRadius = 0.35h + 0.070h * cos(angle * 5.0h - spin * 1.15h);
                half attackSigil = Band(radius, attackSigilRadius, 0.010h, 0.024h) * (1.0h - smoothstep(0.46h, 0.54h, radius));
                half attackEnergy = attackOuter * 0.65h + attackInner * 0.42h + attackBlade * 1.05h + attackVortex * 0.22h + attackRune * 0.34h + attackSigil * 0.58h;

                half speedOuter = Band(radius, 0.84h, 0.022h, 0.032h);
                half speedInner = Band(radius, 0.65h, 0.017h, 0.028h);
                half speedPhaseA = frac((angle01 + spin * 0.30h) * 12.0h);
                half speedPhaseB = frac((angle01 - spin * 0.24h + 0.13h) * 9.0h);
                half speedArcA = SoftSegment(speedPhaseA, 0.5h, 0.19h) * speedOuter;
                half speedArcB = SoftSegment(speedPhaseB, 0.5h, 0.22h) * speedInner;
                half speedStreak = pow(saturate(abs(cos(angle * 7.0h - spin * 9.0h + radius * 6.0h))), 13.0h) * smoothstep(0.48h, 0.58h, radius) * (1.0h - smoothstep(0.93h, 1.0h, radius));
                half speedEnergy = speedArcA * 0.98h + speedArcB * 0.72h + speedStreak * 0.30h;

                half stackRune = pow(saturate(abs(cos(angle * (5.0h + min(3.0h, _StackCount)) + spin * 2.0h))), 14.0h) * Band(radius, 0.88h, 0.016h, 0.024h) * saturate(_StackNormalized);
                half baseEnergy = (attackEnergy * attackWeight + speedEnergy * speedWeight) * stackBoost + stackRune * 0.34h;

                half timerAngle = frac(angle01 + 0.25h - spin * 0.04h);
                half timerVisible = 1.0h - smoothstep(normalizedRemaining, normalizedRemaining + 0.018h, timerAngle);
                half timerRing = Band(radius, 0.965h, 0.010h, 0.018h) * lerp(1.0h, timerVisible, timed);

                half centerHalo = pow(saturate(1.0h - radius / 0.72h), 3.0h) * 0.08h;
                half energy = baseEnergy * pulse + timerRing * 0.62h + centerHalo;
                half alpha = saturate(energy) * _Opacity * life;
                half hot = saturate(timerRing + attackBlade * attackWeight + speedArcA * speedWeight);
                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(hot * 0.72h + speedWeight * speedArcB * 0.25h));
                color *= max(0.0h, _Intensity) * (0.44h + energy * 0.92h) * life;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
