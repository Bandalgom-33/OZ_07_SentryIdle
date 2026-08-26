Shader "Unit/Raid/BackgroundSky"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _TopTint("Top Tint", Color) = (1,1,1,1)
        _BottomTint("Bottom Tint", Color) = (1,1,1,1)
        _Exposure("Exposure", Range(-2,2)) = 0
        _Saturation("Saturation", Range(0,2)) = 1
        _Contrast("Contrast", Range(0.5,1.5)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            Name "RaidBackgroundSky"
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; half4 _TopTint; half4 _BottomTint;
                half _Exposure; half _Saturation; half _Contrast;
            CBUFFER_END
            Varyings Vert(Attributes input){ Varyings o; o.positionHCS=TransformObjectToHClip(input.positionOS.xyz); o.uv=input.uv; return o; }
            half3 Grade(half3 c){ half l=dot(c,half3(0.2126h,0.7152h,0.0722h)); c=lerp(l.xxx,c,_Saturation); c=(c-0.5h)*_Contrast+0.5h; return max(c*exp2(_Exposure),0); }
            half4 Frag(Varyings input):SV_Target{ half3 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).rgb*_Tint.rgb; half3 v=lerp(_BottomTint.rgb,_TopTint.rgb,saturate(input.uv.y)); c=Grade(c*v); return half4(c,1); }
            ENDHLSL
        }
    }
}
