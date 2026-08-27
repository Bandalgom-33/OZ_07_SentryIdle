Shader "EndlessGuard/Raid/UI/BuffStackRune"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (0.8,0.4,1,1)
        _Stack ("Stack", Float) = 0
        _MaxStack ("Max Stack", Float) = 10
        _HighStackThreshold ("High Stack Threshold", Float) = 5
        _Intensity ("Intensity", Range(0,3)) = 1
        _Ignition ("Ignition", Range(0,1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "BuffStackRune"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _GlowColor;
            float _Stack;
            float _MaxStack;
            float _HighStackThreshold;
            float _Intensity;
            float _Ignition;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float DiamondDistance(float2 uv, float2 center)
            {
                float2 p = uv - center;
                return abs(p.x) / 0.082 + abs(p.y) / 0.34;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = saturate(IN.texcoord);
                float highActive = step(_HighStackThreshold - 0.01, _Stack);
                float maxActive = step(_MaxStack - 0.01, _Stack);
                float activeCount = clamp(floor(_Stack - _HighStackThreshold + 1.001), 0.0, 5.0) * highActive;
                float newestIndex = clamp(floor(_Stack - _HighStackThreshold + 0.001), 0.0, 4.0);
                float slowPulse = 0.94 + 0.06 * sin(_Time.y * 2.1);
                float maxPulse = 0.92 + 0.08 * sin(_Time.y * 3.0);
                float outline = 0.0;
                float fill = 0.0;
                float core = 0.0;
                float ignition = 0.0;

                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    float index = (float)i;
                    float2 center = float2(0.10 + index * 0.20, 0.50);
                    float d = DiamondDistance(uv, center);
                    float outer = 1.0 - smoothstep(0.86, 1.05, d);
                    float innerHole = 1.0 - smoothstep(0.54, 0.72, d);
                    float border = saturate(outer - innerHole);
                    float solid = 1.0 - smoothstep(0.60, 0.88, d);
                    float centerCore = 1.0 - smoothstep(0.10, 0.34, d);
                    float active = step(index + 0.5, activeCount);
                    float newest = 1.0 - step(0.45, abs(index - newestIndex));
                    float ignitionHalo = 1.0 - smoothstep(0.82, 1.55, d);
                    outline += border * highActive * (0.16 + active * 0.18);
                    fill += solid * active * (0.52 + 0.12 * slowPulse + 0.10 * maxActive * maxPulse);
                    core += centerCore * active * (0.48 + 0.28 * maxActive);
                    ignition += ignitionHalo * newest * _Ignition * highActive;
                }

                float alpha = saturate(outline + fill + core + ignition * 0.72);
                float3 rgb = _GlowColor.rgb * (0.40 * outline + 0.88 * fill + 1.28 * core + 1.30 * ignition) * _Intensity;
                rgb += _GlowColor.rgb * maxActive * fill * 0.30 * maxPulse;
                fixed4 color = fixed4(rgb, alpha * IN.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
