Shader "BackpackHero/Graphics/Battle Curve Neon Flow"
{
    Properties
    {
        _MainTex ("Pattern Texture", 2D) = "white" {}
        [HDR] _GlowColor ("Glow Color", Color) = (0.451, 0.851, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0.0, 8.0)) = 8.0
        [HideInInspector] _FlowOffset ("Flow Offset", Float) = 0.0
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "BattleCurveNeonFlow"

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
                float4 _MainTex_ST;
                float4 _GlowColor;
                float _GlowIntensity;
                float _FlowOffset;
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
                // LineRenderer generates U from the player endpoint towards the enemy.
                // Map that axis to the pattern's vertical chevrons, then scroll it forward.
                float2 patternUv = float2(input.uv.y, input.uv.x - _FlowOffset);
                patternUv = patternUv * _MainTex_ST.xy + _MainTex_ST.zw;
                float3 pattern = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    patternUv).rgb;
                float mask = max(pattern.r, max(pattern.g, pattern.b));
                float alpha = mask * input.color.a;
                float3 color = _GlowColor.rgb * _GlowIntensity * input.color.rgb;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
