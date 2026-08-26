Shader "EndlessGuard/Raid/Item Field"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (0.4, 0.8, 2.0, 1)
        _EdgeColor("Edge Color", Color) = (0.5, 0.1, 1.2, 1)
        _Intensity("Intensity", Range(0, 4)) = 1.5
        _Opacity("Opacity", Range(0, 1)) = 0.9
        _PulseSpeed("Pulse Speed", Range(0, 6)) = 2.2
        _SpinSpeed("Spin Speed", Range(-1.5, 1.5)) = 0.35
        _Segments("Segments", Range(4, 16)) = 8
        _GlyphPoints("Glyph Points", Range(4, 12)) = 6
        _Scale("Rune Scale", Range(0.65, 1.15)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+18" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "RaidItemField"
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
                half _PulseSpeed;
                half _SpinSpeed;
                half _Segments;
                half _GlyphPoints;
                half _Scale;
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
                half2 p = (input.uv * 2.0h - 1.0h) / max(0.65h, _Scale);
                half radius = length(p);
                half angle = atan2(p.y, p.x);
                half angle01 = frac(angle * InvTwoPi + 0.5h);
                half time = (half)_Time.y;
                half pulse = 0.88h + 0.12h * sin(time * _PulseSpeed * 6.2831853h);

                half segmentCount = max(4.0h, round(_Segments));
                half phase = frac(angle01 * segmentCount + time * _SpinSpeed);
                half brokenArc = 1.0h - smoothstep(0.27h, 0.46h, abs(phase - 0.5h));
                half outerRing = Band(radius, 0.78h, 0.026h, 0.038h) * (0.18h + brokenArc * 0.82h);

                half innerSegments = max(4.0h, segmentCount - 2.0h);
                half innerPhase = frac(angle01 * innerSegments - time * _SpinSpeed * 0.72h + 0.21h);
                half innerArc = 1.0h - smoothstep(0.25h, 0.46h, abs(innerPhase - 0.5h));
                half innerRing = Band(radius, 0.57h, 0.018h, 0.032h) * (0.24h + innerArc * 0.76h);

                half glyphCount = max(4.0h, round(_GlyphPoints));
                half glyphRadius = 0.35h + 0.052h * cos(angle * glyphCount + time * _SpinSpeed * 0.35h);
                half glyph = Band(radius, glyphRadius, 0.009h, 0.024h) * (1.0h - smoothstep(0.48h, 0.55h, radius));

                half rayWave = pow(saturate(abs(cos(angle * glyphCount + time * _SpinSpeed * 0.20h))), 9.0h);
                half rays = rayWave * Band(radius, 0.48h, 0.030h, 0.050h);

                half core = pow(saturate(1.0h - radius / 0.34h), 2.2h);
                half halo = Band(radius, 0.22h, 0.070h, 0.100h);
                half glow = pow(saturate(1.0h - radius / 1.06h), 2.2h);
                half microPulse = 0.92h + 0.08h * sin(angle * 3.0h + time * 3.2h);

                half energy = glow * 0.10h + outerRing * 0.94h + innerRing * 0.56h + glyph * 0.60h + rays * 0.26h + halo * 0.24h + core * 0.54h;
                half alpha = saturate(energy * pulse * microPulse) * _Opacity;
                half coreMix = saturate(core * 1.08h + glyph * 0.42h + innerRing * 0.22h);
                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, coreMix);
                color *= max(0.0h, _Intensity) * (0.48h + energy * 0.88h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
