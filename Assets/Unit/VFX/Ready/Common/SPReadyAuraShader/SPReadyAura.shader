Shader "EndlessGuard/SP Ready Aura"
{
    Properties
    {
        _AuraColor ("Aura Color", Color) = (1.0, 0.72, 0.05, 1.0)
        _CoreColor ("Core Color", Color) = (1.0, 1.0, 0.72, 1.0)
        _Intensity ("Intensity", Range(0.1, 5.0)) = 1.75
        _EdgeWidth ("Core Edge Width", Range(0.005, 0.15)) = 0.035
        _GlowWidth ("Glow Width", Range(0.02, 0.35)) = 0.13
        _SpikeAmount ("Flame Spike Amount", Range(0.0, 0.45)) = 0.18
        _FlickerSpeed ("Flicker Speed", Range(0.0, 8.0)) = 2.2
        [HideInInspector] _AlphaScale ("Alpha Scale", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _PhaseOffset ("Phase Offset", Float) = 0.0
    }

    // Universal Render Pipeline
    SubShader
    {
        Tags
        {
            "Queue"="Transparent+20"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha One

        Pass
        {
            Name "SPReadyAuraURP"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AuraColor;
                float4 _CoreColor;
                float _Intensity;
                float _EdgeWidth;
                float _GlowWidth;
                float _SpikeAmount;
                float _FlickerSpeed;
                float _AlphaScale;
                float _PhaseOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 DrawAura(float2 uv)
            {
                float2 p = (uv - 0.5) * 2.0;

                // 세로로 긴 실루엣. 캐릭터 몸 전체를 감싸되 중앙은 거의 투명하게 둡니다.
                float2 q = float2(p.x * 1.18, p.y * 0.82);
                float radius = length(q);
                float angle = atan2(q.y, q.x);
                float t = _Time.y * _FlickerSpeed + _PhaseOffset;

                // 파티클이 이동하는 대신 외곽선 자체가 제자리에서 일렁입니다.
                float wave1 = sin(angle * 7.0  + t * 1.31);
                float wave2 = sin(angle * 13.0 - t * 1.73);
                float wave3 = sin(angle * 23.0 + t * 0.77);
                float rough = wave1 * 0.026 + wave2 * 0.017 + wave3 * 0.010;

                // 위쪽/옆쪽에 짧은 불꽃 봉우리를 만들어 '기'가 솟는 인상을 줍니다.
                float peakWave = 0.5 + 0.5 * sin(angle * 9.0 + t * 1.17);
                float peaks = pow(saturate(peakWave), 9.0);
                float upperWeight = lerp(0.48, 1.18, smoothstep(-0.45, 0.95, p.y));
                float sideWeight = lerp(0.72, 1.06, saturate(abs(p.x)));
                float boundary = 0.765 + rough + peaks * _SpikeAmount * upperWeight * sideWeight;

                float distanceToEdge = abs(radius - boundary);
                float core = 1.0 - smoothstep(_EdgeWidth, _EdgeWidth * 2.25, distanceToEdge);
                float glow = 1.0 - smoothstep(_GlowWidth, _GlowWidth * 2.15, distanceToEdge);

                // 외곽의 일부가 빠르게 밝아졌다 어두워지는 전기성 반짝임.
                float sparkWave = 0.5 + 0.5 * sin(angle * 19.0 - t * 3.1 + wave2);
                float sparks = pow(saturate(sparkWave), 16.0) * core;

                // 화면 가장자리 잘림을 자연스럽게 처리합니다.
                float edgeFadeX = 1.0 - smoothstep(0.88, 1.0, abs(p.x));
                float edgeFadeY = 1.0 - smoothstep(0.91, 1.0, abs(p.y));
                float boundsFade = edgeFadeX * edgeFadeY;

                float pulse = 0.90 + 0.10 * sin(t * 2.55 + angle * 1.6);
                float alpha = saturate((core * 0.90 + glow * 0.42 + sparks * 0.40) * pulse * _AlphaScale) * boundsFade;

                float3 color = _AuraColor.rgb * (glow * 0.72 + core * 0.48);
                color += _CoreColor.rgb * (core * 0.72 + sparks * 1.25);
                color *= _Intensity * pulse;

                clip(alpha - 0.003);
                return float4(color, alpha * _AuraColor.a);
            }

            half4 frag(Varyings input) : SV_Target
            {
                return (half4)DrawAura(input.uv);
            }
            ENDHLSL
        }
    }

    // Built-in Render Pipeline fallback. 같은 모양을 유지합니다.
    SubShader
    {
        Tags
        {
            "Queue"="Transparent+20"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha One

        Pass
        {
            Name "SPReadyAuraBuiltin"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _AuraColor;
            float4 _CoreColor;
            float _Intensity;
            float _EdgeWidth;
            float _GlowWidth;
            float _SpikeAmount;
            float _FlickerSpeed;
            float _AlphaScale;
            float _PhaseOffset;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float2 q = float2(p.x * 1.18, p.y * 0.82);
                float radius = length(q);
                float angle = atan2(q.y, q.x);
                float t = _Time.y * _FlickerSpeed + _PhaseOffset;

                float wave1 = sin(angle * 7.0  + t * 1.31);
                float wave2 = sin(angle * 13.0 - t * 1.73);
                float wave3 = sin(angle * 23.0 + t * 0.77);
                float rough = wave1 * 0.026 + wave2 * 0.017 + wave3 * 0.010;

                float peakWave = 0.5 + 0.5 * sin(angle * 9.0 + t * 1.17);
                float peaks = pow(saturate(peakWave), 9.0);
                float upperWeight = lerp(0.48, 1.18, smoothstep(-0.45, 0.95, p.y));
                float sideWeight = lerp(0.72, 1.06, saturate(abs(p.x)));
                float boundary = 0.765 + rough + peaks * _SpikeAmount * upperWeight * sideWeight;

                float distanceToEdge = abs(radius - boundary);
                float core = 1.0 - smoothstep(_EdgeWidth, _EdgeWidth * 2.25, distanceToEdge);
                float glow = 1.0 - smoothstep(_GlowWidth, _GlowWidth * 2.15, distanceToEdge);

                float sparkWave = 0.5 + 0.5 * sin(angle * 19.0 - t * 3.1 + wave2);
                float sparks = pow(saturate(sparkWave), 16.0) * core;

                float edgeFadeX = 1.0 - smoothstep(0.88, 1.0, abs(p.x));
                float edgeFadeY = 1.0 - smoothstep(0.91, 1.0, abs(p.y));
                float boundsFade = edgeFadeX * edgeFadeY;

                float pulse = 0.90 + 0.10 * sin(t * 2.55 + angle * 1.6);
                float alpha = saturate((core * 0.90 + glow * 0.42 + sparks * 0.40) * pulse * _AlphaScale) * boundsFade;

                float3 color = _AuraColor.rgb * (glow * 0.72 + core * 0.48);
                color += _CoreColor.rgb * (core * 0.72 + sparks * 1.25);
                color *= _Intensity * pulse;

                clip(alpha - 0.003);
                return fixed4(color, alpha * _AuraColor.a);
            }
            ENDCG
        }
    }

    FallBack Off
}
