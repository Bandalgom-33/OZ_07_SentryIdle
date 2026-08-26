Shader "EndlessGuard/Raid/UI/BuffStackFrame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (0.8,0.4,1,1)
        _Stack ("Stack", Float) = 0
        _MaxStack ("Max Stack", Float) = 10
        _HighStackThreshold ("High Stack Threshold", Float) = 5
        _Intensity ("Intensity", Range(0,3)) = 0.95
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
            Name "BuffStackFrame"
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
            fixed4 _TextureSampleAdd;
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

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = saturate(IN.texcoord);
                half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * IN.color;
                float highActive = step(_HighStackThreshold - 0.01, _Stack);
                float maxActive = step(_MaxStack - 0.01, _Stack);
                float highRange = max(1.0, _MaxStack - _HighStackThreshold);
                float highRatio = saturate((_Stack - _HighStackThreshold) / highRange);
                float edgeDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float edge = 1.0 - smoothstep(0.012, 0.040, edgeDistance);
                float halo = 1.0 - smoothstep(0.035, 0.095, edgeDistance);
                float slowPulse = 0.96 + 0.04 * sin(_Time.y * 2.4);
                float persistent = highActive * slowPulse * (edge * (0.022 + 0.018 * highRatio + 0.030 * maxActive) + halo * (0.010 + 0.012 * maxActive));
                float ignition = _Ignition * highActive * (edge * (0.36 + 0.18 * maxActive) + halo * (0.12 + 0.10 * maxActive));
                float glow = (persistent + ignition) * _Intensity;
                color.rgb += _GlowColor.rgb * glow;
                color.a = max(color.a, saturate(glow * _GlowColor.a * 0.30));

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
