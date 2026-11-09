Shader "Custom/UltrakillDither"
{
    Properties
    {
        _PixelHeight    ("Vertical Res", Float) = 240
        _ColorLevels    ("Color Levels ", Range(2, 32)) = 6
        _DitherStrength ("Dither Strength", Range(0, 1)) = 1
        _Spread         ("Palette DitherSpread", Range(0, 1)) = 0.25
        _Contrast       ("Contrast", Range(0.5, 2)) = 1.1
        _Saturation     ("Saturation", Range(0, 2)) = 1
        _ShimmerSpeed   ("Shimmer Speed ", Range(0, 30)) = 0
        
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Dithering"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _USE_PALETTE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelHeight, _ColorLevels, _DitherStrength, _Spread;
            float _Contrast, _Saturation, _ShimmerSpeed;

            
            #define PALETTE_SIZE 8
            static const float3 PALETTE[PALETTE_SIZE] =
            {
                float3(0.05, 0.03, 0.06),
                float3(0.25, 0.08, 0.10),
                float3(0.60, 0.10, 0.08),
                float3(0.95, 0.35, 0.10),
                float3(0.98, 0.80, 0.35),
                float3(0.35, 0.45, 0.55),
                float3(0.70, 0.78, 0.85),
                float3(1.00, 0.97, 0.90)
            };

            float Bayer8(uint2 p)
            {
                p &= 7u;
                uint a = p.x ^ p.y;
                uint y = p.y;
                uint v = ((a & 1u) << 5) | ((y & 1u) << 4)
                       | (((a >> 1) & 1u) << 3) | (((y >> 1) & 1u) << 2)
                       | (((a >> 2) & 1u) << 1) | ((y >> 2) & 1u);
                return (v + 0.5) / 64.0;
            }

            float3 NearestPalette(float3 c)
            {
                float3 best = PALETTE[0];
                float bestD = 1e9;
                [unroll] for (int i = 0; i < PALETTE_SIZE; i++)
                {
                    float3 d = c - PALETTE[i];
                    float dd = dot(d, d);
                    if (dd < bestD) { bestD = dd; best = PALETTE[i]; }
                }
                return best;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 res   = float2(_PixelHeight * aspect, _PixelHeight);
                float2 pix   = floor(input.texcoord * res);
                float2 uv    = (pix + 0.5) / res;

                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;

                col = saturate((col - 0.5) * _Contrast + 0.5);
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                col = saturate(lerp(lum.xxx, col, _Saturation));

                float threshold = Bayer8((uint2)pix);

              
                if (_ShimmerSpeed > 0)
                {
                    float tick = floor(_Time.y * _ShimmerSpeed);
                    threshold = frac(threshold + frac(tick * 0.618034));
                }

                threshold = lerp(0.5, threshold, _DitherStrength);

            #ifdef _USE_PALETTE
                col = NearestPalette(col + (threshold - 0.5) * _Spread);
            #else
                float steps = _ColorLevels - 1.0;
                col = floor(col * steps + threshold) / steps;
            #endif

                return float4(saturate(col), 1);
            }
            ENDHLSL
        }
    }
}