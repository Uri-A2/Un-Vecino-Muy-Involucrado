Shader "Custom/RetroLit"
{
    Properties
    {
        _BaseMap   ("Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _SnapGrid  ("Vertex Snap Grid", Float) = 160
        _Ambient   ("Ambient", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_point_repeat);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _SnapGrid;
                float _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                noperspective float2 uv : TEXCOORD0; // affine warping
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 pos = TransformObjectToHClip(IN.positionOS.xyz);

                // Snap to a coarse screen grid for the PS1 wobble
                float2 ndc = pos.xy / pos.w;
                ndc = floor(ndc * _SnapGrid + 0.5) / _SnapGrid;
                pos.xy = ndc * pos.w;

                OUT.positionCS = pos;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_point_repeat, IN.uv) * _BaseColor;
                Light l = GetMainLight();
                float ndl = saturate(dot(normalize(IN.normalWS), l.direction));
                float3 lit = tex.rgb * (_Ambient + ndl * l.color);
                return float4(lit, 1);
            }
            ENDHLSL
        }
    }
}