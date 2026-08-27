Shader "EndlessGuard/Raid/Collapse Aurora"
{
    Properties
    {
        _OuterColor("Outer Color", Color) = (0.08, 0.01, 0.22, 1)
        _CoreColor("Core Color", Color) = (0.72, 0.24, 1.3, 1)
        _Intensity("Intensity", Range(0, 2)) = 0
        _Flicker("Flicker", Range(0, 1)) = 0.34
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "RaidCollapseAurora"
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
                float _Flicker;
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
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float vertical = saturate(input.uv.y);
                float bottomFade = smoothstep(0.0, 0.035, vertical);
                float topFade = 1.0 - smoothstep(0.48, 1.0, vertical);
                float heightFade = bottomFade * topFade;

                float along = input.uv.x;
                float worldPhase = input.positionWS.x * 0.42 + input.positionWS.z * 0.58;
                float broadWave = 0.5 + 0.5 * sin(along * 1.35 + worldPhase + _Time.y * 0.72);
                float secondaryWave = 0.5 + 0.5 * sin(along * 2.55 - worldPhase * 0.63 - _Time.y * 1.08 + 1.4);
                float upwardFlow = 0.5 + 0.5 * sin(vertical * 8.6 - _Time.y * 3.35 + along * 0.92 + worldPhase * 0.31);
                float slowDrift = 0.5 + 0.5 * sin(vertical * 3.1 + along * 0.58 + _Time.y * 0.94 - worldPhase * 0.22);

                float veil = saturate(broadWave * 0.46 + secondaryWave * 0.28 + slowDrift * 0.26);
                float strand = smoothstep(0.4, 0.88, veil * 0.72 + upwardFlow * 0.28);
                float gaps = smoothstep(0.12, 0.72, broadWave * 0.58 + secondaryWave * 0.42);
                float baseGlow = (1.0 - smoothstep(0.02, 0.28, vertical)) * 0.72;

                float shimmer = 0.9 + sin(_Time.y * 9.7 + worldPhase * 0.7) * 0.055 + sin(_Time.y * 15.3 + along * 0.83) * 0.035;
                float flicker = lerp(1.0, shimmer, saturate(_Flicker));
                float intensity = max(0.0, _Intensity) * input.color.a * flicker;

                float softEnergy = 0.11 + veil * 0.2 + strand * 0.28 + upwardFlow * 0.11 + baseGlow * 0.2;
                float alpha = saturate(heightFade * softEnergy * lerp(0.42, 1.0, gaps) * intensity);
                float core = saturate(strand * 0.58 + upwardFlow * 0.2 + baseGlow * 0.34);
                float3 color = lerp(_OuterColor.rgb, _CoreColor.rgb, core);
                color *= (0.52 + veil * 0.22 + core * 0.38) * intensity;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
