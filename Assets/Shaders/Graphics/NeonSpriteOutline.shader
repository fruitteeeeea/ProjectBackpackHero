Shader "BackpackHero/Graphics/Neon Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _OutlineColor ("Outline Color", Color) = (0.32, 0.78, 1.0, 1.0)
        _OutlineIntensity ("Outline Intensity", Range(0.0, 8.0)) = 3.0
        _OutlineWidth ("Outline Width (Pixels)", Range(0.0, 6.0)) = 1.5
        _CoreColor ("Core Color", Color) = (0.72, 0.78, 0.86, 1.0)
        _CoreIntensity ("Core Intensity", Range(0.0, 1.0)) = 0.35
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
            Name "NeonSpriteOutline"

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

            float4 SampleSprite(float2 uv)
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                #if ETC1_EXTERNAL_ALPHA
                    color.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                #endif
                return color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 source = SampleSprite(input.uv);
                float sourceAlpha = source.a;

                // Eight alpha taps create a rounded, texture-pixel-accurate dilation.
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                float neighbourAlpha = 0.0;
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2( offset.x,  0.0)).a);
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2(-offset.x,  0.0)).a);
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2( 0.0,  offset.y)).a);
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2( 0.0, -offset.y)).a);
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2( offset.x,  offset.y)).a);
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2(-offset.x,  offset.y)).a);
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2( offset.x, -offset.y)).a);
                neighbourAlpha = max(neighbourAlpha, SampleSprite(input.uv + float2(-offset.x, -offset.y)).a);

                float outlineMask = saturate(neighbourAlpha - sourceAlpha);
                float alpha = max(sourceAlpha, outlineMask) * input.color.a;

                float3 core = source.rgb * _CoreColor.rgb * _CoreIntensity;
                float3 outline = _OutlineColor.rgb * _OutlineIntensity;
                float3 color = lerp(core, outline, outlineMask) * input.color.rgb;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
