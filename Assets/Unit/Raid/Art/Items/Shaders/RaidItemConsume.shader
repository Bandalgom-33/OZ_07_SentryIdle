Shader "EndlessGuard/Raid/Item Consume"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (0.4, 0.8, 2.0, 1)
        _EdgeColor("Edge Color", Color) = (0.5, 0.1, 1.2, 1)
        _Intensity("Intensity", Range(0, 5)) = 1.8
        _Opacity("Opacity", Range(0, 1)) = 0.9
        _SpinSpeed("Spin Speed", Range(-3, 3)) = 0.8
        _Style("Style", Range(0, 2)) = 0
        [HideInInspector] _VFXStartTime("VFX Start Time", Float) = 0
        [HideInInspector] _VFXDuration("VFX Duration", Float) = 0.65
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+24" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "RaidItemConsume"
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
                half _Style;
                float _VFXStartTime;
                float _VFXDuration;
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

            half4 Frag(Varyings input) : SV_Target
            {
                const half InvTwoPi = 0.15915494h;
                half2 p = input.uv * 2.0h - 1.0h;
                half radius = length(p);
                half angle = atan2(p.y, p.x);
                half angle01 = frac(angle * InvTwoPi + 0.5h);
                half duration = max(0.05h, (half)_VFXDuration);
                half progress = saturate(((half)_Time.y - (half)_VFXStartTime) / duration);
                half fade = smoothstep(0.0h, 0.08h, progress) * (1.0h - smoothstep(0.78h, 1.0h, progress));
                half style = round(_Style);
                half styleSpeed = lerp(0.8h, 1.55h, step(0.5h, style));
                styleSpeed = lerp(styleSpeed, 0.62h, step(1.5h, style));
                half spin = progress * _SpinSpeed * styleSpeed;

                half shrinkRadius = lerp(0.92h, 0.16h, smoothstep(0.0h, 1.0h, progress));
                half shrinkRing = Band(radius, shrinkRadius, 0.030h, 0.050h);
                half echoRadius = lerp(0.28h, 1.06h, progress);
                half echo = Band(radius, echoRadius, 0.018h, 0.055h) * (1.0h - progress);

                half shardCount = lerp(5.0h, 10.0h, step(0.5h, style));
                shardCount = lerp(shardCount, 6.0h, step(1.5h, style));
                half shardPhase = frac((angle01 + spin) * shardCount);
                half shardAngular = 1.0h - smoothstep(0.06h, 0.18h, abs(shardPhase - 0.5h));
                half shardRadius = lerp(0.82h, 0.12h, progress);
                half shards = shardAngular * Band(radius, shardRadius, 0.035h, 0.055h);

                half spiral = (0.5h + 0.5h * sin(angle * (4.0h + style * 2.0h) - progress * 20.0h + radius * 12.0h)) * Band(radius, lerp(0.64h, 0.24h, progress), 0.12h, 0.08h);
                half flashEnvelope = smoothstep(0.45h, 0.72h, progress) * (1.0h - smoothstep(0.82h, 1.0h, progress));
                half core = pow(saturate(1.0h - radius / 0.38h), 2.2h) * flashEnvelope;

                half healWeight = step(1.5h, style);
                half healPetal = pow(saturate(abs(cos(angle * 4.0h + progress * 2.0h))), 8.0h) * Band(radius, lerp(0.62h, 0.20h, progress), 0.055h, 0.07h) * healWeight;

                half energy = shrinkRing * 0.88h + echo * 0.42h + shards * 0.74h + spiral * 0.22h + core * 1.35h + healPetal * 0.58h;
                half alpha = saturate(energy) * _Opacity * fade;
                half hot = saturate(core + shrinkRing * 0.55h + healPetal * 0.38h);
                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, hot);
                color *= max(0.0h, _Intensity) * (0.45h + energy * 0.92h) * fade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
