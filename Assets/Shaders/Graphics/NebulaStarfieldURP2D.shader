Shader "BackpackHero/Graphics/Nebula Starfield URP 2D"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _ColorA("Deep Blue", Color) = (0.008, 0.016, 0.075, 1)
        [HDR] _ColorB("Nebula Violet", Color) = (0.075, 0.025, 0.18, 1)
        [HDR] _ColorC("Cool Highlight", Color) = (0.08, 0.24, 0.38, 1)
        _Speed("Animation Speed", Range(0, 2)) = 0.35
        _NoiseScale("Noise Scale", Range(0.2, 4)) = 1.25
        _FlowStrength("Flow Strength", Range(0, 2)) = 0.65
        _TileCount("Tile Count", Range(0.5, 4)) = 1
        _StarDensity("Star Density", Range(0, 2)) = 0.55
        [HDR] _StarColor("Star Color", Color) = (0.4, 0.7, 1.2, 1)
        _StarIntensity("Star Intensity", Range(0, 2)) = 0.45
        _Brightness("Background Brightness", Range(0, 1)) = 0.58
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        Pass
        {
            Name "NebulaStarfield"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _ColorC;
                float4 _StarColor;
                float _Speed;
                float _NoiseScale;
                float _FlowStrength;
                float _TileCount;
                float _StarDensity;
                float _StarIntensity;
                float _Brightness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float Hash41(float4 p)
            {
                return frac(sin(dot(p, float4(1234.0, 2345.0, 3456.0, 4567.0))) * 5678.0);
            }

            // One 4D value-noise sample needs 16 inexpensive scalar hash lookups.
            float SmoothNoise4D(float4 p)
            {
                float4 cell = floor(p);
                float4 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n0000 = lerp(Hash41(cell), Hash41(cell + float4(1, 0, 0, 0)), f.x);
                float n1000 = lerp(Hash41(cell + float4(0, 1, 0, 0)), Hash41(cell + float4(1, 1, 0, 0)), f.x);
                float n0100 = lerp(Hash41(cell + float4(0, 0, 1, 0)), Hash41(cell + float4(1, 0, 1, 0)), f.x);
                float n1100 = lerp(Hash41(cell + float4(0, 1, 1, 0)), Hash41(cell + float4(1, 1, 1, 0)), f.x);
                float n0001 = lerp(Hash41(cell + float4(0, 0, 0, 1)), Hash41(cell + float4(1, 0, 0, 1)), f.x);
                float n1001 = lerp(Hash41(cell + float4(0, 1, 0, 1)), Hash41(cell + float4(1, 1, 0, 1)), f.x);
                float n0101 = lerp(Hash41(cell + float4(0, 0, 1, 1)), Hash41(cell + float4(1, 0, 1, 1)), f.x);
                float n1101 = lerp(Hash41(cell + float4(0, 1, 1, 1)), Hash41(cell + float4(1, 1, 1, 1)), f.x);

                return lerp(
                    lerp(lerp(n0000, n1000, f.y), lerp(n0100, n1100, f.y), f.z),
                    lerp(lerp(n0001, n1001, f.y), lerp(n0101, n1101, f.y), f.z), f.w);
            }

            // Four fixed octaves are intentionally used for predictable mobile performance.
            float Fbm4(float3 p, float timeSlice)
            {
                float value = 0.0;
                float amplitude = 0.5;
                value += amplitude * SmoothNoise4D(float4(p, timeSlice));
                p = p * 2.0 + 1.0; amplitude *= 0.5;
                value += amplitude * SmoothNoise4D(float4(p, timeSlice));
                p = p * 2.0 + 1.0; amplitude *= 0.5;
                value += amplitude * SmoothNoise4D(float4(p, timeSlice));
                p = p * 2.0 + 1.0; amplitude *= 0.5;
                value += amplitude * SmoothNoise4D(float4(p, timeSlice));
                return value;
            }

            // Maps both edges of a tile to the same 3D coordinates. Unlike the
            // original equirectangular planet map, this is seamless vertically too.
            float3 TiledDirection(float2 uv)
            {
                float theta = uv.x * TWO_PI;
                float phi = uv.y * TWO_PI;
                return float3(cos(theta), sin(theta), sin(phi) + cos(phi) * 0.5);
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float StarField(float2 uv, float time)
            {
                float cells = lerp(34.0, 74.0, saturate(_StarDensity * 0.5));
                float2 gridUv = uv * cells;
                float2 cell = floor(gridUv);
                float2 local = frac(gridUv) - 0.5;
                float random = Hash21(cell);
                float enabled = step(1.0 - 0.045 * _StarDensity, random);
                float2 offset = float2(Hash21(cell + 8.13), Hash21(cell + 19.37)) - 0.5;
                float distanceToStar = length(local - offset * 0.55);
                float radius = lerp(0.025, 0.075, Hash21(cell + 3.17));
                float core = 1.0 - smoothstep(radius, radius * 2.2, distanceToStar);
                float twinkle = 0.72 + 0.28 * sin(time * (1.2 + random * 1.6) + random * TWO_PI);
                return enabled * core * twinkle;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                #if UNITY_UV_STARTS_AT_TOP
                    if (_ProjectionParams.x < 0.0) uv.y = 1.0 - uv.y;
                #endif

                float time = _Time.y * _Speed;
                // Keep every repeated unit square in screen space. This prevents
                // portrait displays from vertically stretching the nebula and stars.
                float aspect = _ScaledScreenParams.x / max(_ScaledScreenParams.y, 1.0);
                float2 tiledUv = frac(uv * float2(_TileCount, _TileCount / max(aspect, 0.001)));
                float3 direction = TiledDirection(tiledUv);
                float slice = cos(time * 0.18) * 32.0;
                float3 p = direction * _NoiseScale;
                p += float3(sin(time * 0.24), cos(time * 0.20), sin(time * 0.16)) * 0.35;

                float baseNoise = Fbm4(p, slice);
                float3 warp = float3(baseNoise, sin(baseNoise * TWO_PI), cos(baseNoise * TWO_PI));
                float turbulence = Fbm4(p + warp * (2.6 * _FlowStrength), slice + time * 0.12);
                float nebula = saturate(turbulence * 1.9 - 0.38);
                float accent = smoothstep(0.57, 0.86, turbulence);
                float3 color = lerp(_ColorA.rgb, _ColorB.rgb, nebula);
                color = lerp(color, _ColorC.rgb, accent * 0.48);

                // Compress high values before Bloom so the background cannot dominate foreground UI.
                color *= lerp(0.45, 0.85, nebula) * _Brightness;
                float stars = StarField(tiledUv, time);
                color += _StarColor.rgb * stars * _StarIntensity;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
