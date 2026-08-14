Shader "BackpackHero/Graphics/Laser Beam Neon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Laser Pattern", 2D) = "white" {}
        [HDR] _OutlineColor ("Faction Glow Color", Color) = (1, 1, 1, 1)
        _OutlineIntensity ("Glow Intensity", Range(0.0, 8.0)) = 8.0
        _OutlineWidth ("Glow Width (Pixels)", Range(0.0, 6.0)) = 1.5
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _CoreIntensity ("Core Intensity", Range(0.0, 1.0)) = 1.0
        _CoreThreshold ("White Core Threshold", Range(0.0, 1.0)) = 0.75
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "LaserBeamNeon"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _OutlineColor;
                float _OutlineIntensity;
                float _OutlineWidth;
                float4 _CoreColor;
                float _CoreIntensity;
                float _CoreThreshold;
                float4 _Color;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);

            float4 _RendererColor;
            float _EnableExternalAlpha;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            float4 SampleLaser(float2 uv)
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                #if ETC1_EXTERNAL_ALPHA
                    color.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                #endif
                return color;
            }

            float BrightnessMask(float2 uv)
            {
                float3 color = SampleLaser(uv).rgb;
                return saturate(dot(color, float3(0.2126, 0.7152, 0.0722)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // The source image has no useful alpha: its luminance is the beam mask.
                float mask = BrightnessMask(input.uv);
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                float neighbourMask = 0.0;
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2( offset.x,  0.0)));
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2(-offset.x,  0.0)));
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2( 0.0,  offset.y)));
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2( 0.0, -offset.y)));
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2( offset.x,  offset.y)));
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2(-offset.x,  offset.y)));
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2( offset.x, -offset.y)));
                neighbourMask = max(neighbourMask, BrightnessMask(input.uv + float2(-offset.x, -offset.y)));

                float glowMask = max(mask, saturate(neighbourMask - mask));
                float coreMask = smoothstep(_CoreThreshold, 1.0, mask);
                float alpha = max(glowMask, coreMask) * input.color.a;
                float3 glow = _OutlineColor.rgb * _OutlineIntensity * input.color.rgb;
                float3 core = _CoreColor.rgb * _CoreIntensity;
                float3 color = lerp(glow, core, coreMask);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
