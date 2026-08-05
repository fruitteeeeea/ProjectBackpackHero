Shader "Backpack/UI/Item Cooldown"
{
    Properties
    {
        [PerRendererData] _MainTex (
            "Sprite Texture",
            2D
        ) = "white" {}

        _Color ("Tint", Color) = (1, 1, 1, 1)

        _ShadowColor (
            "Cooldown Shadow",
            Color
        ) = (0, 0, 0, 0.55)

        _CooldownProgress (
            "Cooldown Progress",
            Range(0, 1)
        ) = 1

        _FlashAmount (
            "Flash Amount",
            Range(0, 1)
        ) = 0

        _AircraftPatternTex (
            "Aircraft Pattern",
            2D
        ) = "white" {}

        _EquipmentPatternTex (
            "Equipment Pattern",
            2D
        ) = "white" {}

        _ItemVisualStyle (
            "Item Visual Style (0 None, 1 Aircraft, 2 Equipment)",
            Range(0, 2)
        ) = 0

        _GrayOverlayStrength (
            "Gray Overlay Strength",
            Range(0, 1)
        ) = 0.45

        _PatternOverlayStrength (
            "Pattern Overlay Strength",
            Range(0, 1)
        ) = 0.25

        _PatternTiling (
            "Pattern Tiling",
            Vector
        ) = (1, 1, 0, 0)

        _StencilComp (
            "Stencil Comparison",
            Float
        ) = 8

        _Stencil (
            "Stencil ID",
            Float
        ) = 0

        _StencilOp (
            "Stencil Operation",
            Float
        ) = 0

        _StencilWriteMask (
            "Stencil Write Mask",
            Float
        ) = 255

        _StencilReadMask (
            "Stencil Read Mask",
            Float
        ) = 255

        _ColorMask (
            "Color Mask",
            Float
        ) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip (
            "Use Alpha Clip",
            Float
        ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "Default"

            CGPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _AircraftPatternTex;
            sampler2D _EquipmentPatternTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            fixed4 _ShadowColor;

            float _CooldownProgress;
            float _FlashAmount;
            float _ItemVisualStyle;
            float _GrayOverlayStrength;
            float _PatternOverlayStrength;
            float4 _PatternTiling;
            float4 _ClipRect;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.worldPosition =
                    input.positionOS;

                output.positionCS =
                    UnityObjectToClipPos(
                        input.positionOS);

                output.uv = input.uv;
                output.color = input.color * _Color;

                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed4 spriteColor =
                    (tex2D(_MainTex, input.uv) +
                     _TextureSampleAdd) *
                    input.color;

                float progress =
                    saturate(_CooldownProgress);

                // UV.y 为 1 的位置是图片顶部。
                // progress 从 0 增加到 1 时，
                // 阴影的底边从顶部移动到底部。
                float shadowBoundary =
                    1.0 - progress;

                float shadowMask =
                    step(
                        input.uv.y,
                        shadowBoundary);

                float shadowStrength =
                    shadowMask *
                    _ShadowColor.a;

                spriteColor.rgb =
                    lerp(
                        spriteColor.rgb,
                        _ShadowColor.rgb,
                        shadowStrength);

                // 物品分类视觉只应用在背景层：
                // 飞机从底部向顶部渐隐，装备从顶部向底部渐隐。
                if (_ItemVisualStyle > 0.5)
                {
                    float isEquipment =
                        step(1.5, _ItemVisualStyle);

                    float gradientMask =
                        lerp(
                            1.0 - input.uv.y,
                            input.uv.y,
                            isEquipment);

                    fixed3 patternColor =
                        lerp(
                            tex2D(
                                _AircraftPatternTex,
                                input.uv *
                                _PatternTiling.xy).rgb,
                            tex2D(
                                _EquipmentPatternTex,
                                input.uv *
                                _PatternTiling.xy).rgb,
                            isEquipment);

                    spriteColor.rgb =
                        lerp(
                            spriteColor.rgb,
                            fixed3(0.5, 0.5, 0.5),
                            gradientMask *
                            saturate(_GrayOverlayStrength));

                    spriteColor.rgb =
                        lerp(
                            spriteColor.rgb,
                            patternColor,
                            gradientMask *
                            saturate(_PatternOverlayStrength));
                }

                spriteColor.rgb =
                    lerp(
                        spriteColor.rgb,
                        fixed3(1.0, 1.0, 1.0),
                        saturate(_FlashAmount));

                #ifdef UNITY_UI_CLIP_RECT
                spriteColor.a *=
                    UnityGet2DClipping(
                        input.worldPosition.xy,
                        _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(spriteColor.a - 0.001);
                #endif

                return spriteColor;
            }

            ENDCG
        }
    }
}
