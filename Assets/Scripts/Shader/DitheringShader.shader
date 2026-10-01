Shader "Hidden/DitherEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Dither"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _DitherScale;   // size of the bayer cells on screen (pixels)
            float _ColorLevels;   // quantization steps per channel
            float _DitherStrength;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);
                return OUT;
            }

            // 8x8 Bayer matrix, normalized 0..1
            static const float bayer8x8[64] = {
                 0, 32,  8, 40,  2, 34, 10, 42,
                48, 16, 56, 24, 50, 18, 58, 26,
                12, 44,  4, 36, 14, 46,  6, 38,
                60, 28, 52, 20, 62, 30, 54, 22,
                 3, 35, 11, 43,  1, 33,  9, 41,
                51, 19, 59, 27, 49, 17, 57, 25,
                15, 47,  7, 39, 13, 45,  5, 37,
                63, 31, 55, 23, 61, 29, 53, 21
            };

            float GetBayerValue(float2 screenPos)
            {
                uint2 pixel = uint2(fmod(screenPos, 8.0));
                return bayer8x8[pixel.y * 8 + pixel.x] / 64.0;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                float2 screenPos = IN.uv * _MainTex_TexelSize.zw / max(_DitherScale, 0.001);
                float threshold = GetBayerValue(screenPos) - 0.5;

                float levels = max(_ColorLevels, 2.0);
                float3 ditheredColor = col.rgb + threshold * _DitherStrength / levels;
                ditheredColor = floor(ditheredColor * levels + 0.5) / levels;
                ditheredColor = saturate(ditheredColor);

                return float4(ditheredColor, col.a);
            }
            ENDHLSL
        }
    }
}