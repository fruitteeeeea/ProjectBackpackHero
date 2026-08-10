Shader "BackpackHero/Graphics/Battle Curve Pulse"
{
    Properties
    {
        _MainTex ("Pulse Pattern", 2D) = "white" {}
        [HDR] _PulseColor ("Pulse Color", Color) = (0.35, 0.8, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0.0, 16.0)) = 6.0
        _Opacity ("Opacity", Range(0.0, 1.0)) = 1.0
        _TextureTiling ("Texture Tiling", Range(0.1, 20.0)) = 3.0
        _TextureScrollSpeed ("Texture Scroll Speed", Range(-10.0, 10.0)) = 2.0
        _PatternContrast ("Pattern Contrast", Range(0.1, 8.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "BattleCurvePulse"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

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
                float4 _PulseColor;
                float _GlowIntensity;
                float _Opacity;
                float _TextureTiling;
                float _TextureScrollSpeed;
                float _PatternContrast;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 patternUv = float2(
                    input.uv.x * _TextureTiling +
                    _Time.y * _TextureScrollSpeed,
                    input.uv.y);
                float3 pattern = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    patternUv).rgb;
                float brightness = dot(
                    pattern,
                    float3(0.2126, 0.7152, 0.0722));
                float mask = pow(
                    saturate(brightness),
                    _PatternContrast);
                float alpha = mask * _Opacity * input.color.a;
                float3 color =
                    _PulseColor.rgb * _GlowIntensity * input.color.rgb;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
