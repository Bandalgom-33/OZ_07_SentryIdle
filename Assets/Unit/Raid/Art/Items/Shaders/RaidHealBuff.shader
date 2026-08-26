Shader "EndlessGuard/Raid/Heal Buff"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (0.25, 2.4, 0.85, 1)
        _EdgeColor("Edge Color", Color) = (0.04, 0.7, 0.28, 1)
        _Intensity("Intensity", Range(0, 4)) = 1.15
        _Opacity("Opacity", Range(0, 1)) = 0.56
        _SpinSpeed("Spin Speed", Range(-2, 2)) = 0.24
        _PulseSpeed("Pulse Speed", Range(0, 8)) = 1.8
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
            Name "RaidHealBuff"
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

            half Band(half value, half center, half width, half softness)
            {
                return 1.0h - smoothstep(width, width + softness, abs(value - center));
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

                half spin = time * _SpinSpeed;
                half stack = saturate(_StackNormalized);
                half pulse = 0.92h + (0.08h + stack * 0.035h) * sin(time * _PulseSpeed * 6.2831853h);

                half outer = Band(radius, 0.82h, 0.022h, 0.036h);
                half inner = Band(radius, 0.61h, 0.017h, 0.030h);
                half petalWave = abs(cos((angle + spin) * 3.0h));
                half petalRadius = 0.36h + 0.10h * pow(petalWave, 2.0h);
                half petals = Band(radius, petalRadius, 0.015h, 0.028h) * (1.0h - smoothstep(0.54h, 0.62h, radius));
                half rune = pow(saturate(abs(cos(angle * 6.0h - spin * 2.1h))), 15.0h) * Band(radius, 0.72h, 0.015h, 0.026h);
                half motes = pow(saturate(0.5h + 0.5h * sin((angle01 * 12.0h + radius * 4.0h - time * 0.42h) * 6.2831853h)), 10.0h) * Band(radius, 0.50h, 0.24h, 0.10h);
                half centerHalo = pow(saturate(1.0h - radius / 0.58h), 3.2h) * 0.12h;
                half stackRune = pow(saturate(abs(cos(angle * (5.0h + min(4.0h, _StackCount)) - spin))), 16.0h) * Band(radius, 0.91h, 0.013h, 0.024h) * stack;

                half timerAngle = frac(angle01 + 0.25h - spin * 0.03h);
                half timerVisible = 1.0h - smoothstep(normalizedRemaining, normalizedRemaining + 0.018h, timerAngle);
                half timerRing = Band(radius, 0.965h, 0.010h, 0.018h) * lerp(1.0h, timerVisible, timed);

                half energy = (outer * 0.56h + inner * 0.42h + petals * 0.80h + rune * 0.34h + motes * 0.20h + stackRune * 0.30h) * pulse + timerRing * 0.62h + centerHalo;
                half alpha = saturate(energy) * _Opacity * life;
                half hot = saturate(petals * 0.85h + timerRing + rune * 0.25h);
                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, hot);
                color *= max(0.0h, _Intensity) * (0.46h + energy * 0.90h) * life;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
