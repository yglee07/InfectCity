Shader "Custom/CountryFill_Coverage"
{
    Properties
    {
        _MaskTex   ("Mask Texture", 2D) = "black" {}
        _FillColor ("Fill Color", Color) = (0.3, 0.8, 0.3, 1)
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry+1"
        }

        Pass
        {
             ZWrite Off
            ZTest Always
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            float4 _FillColor;
            float4 _BaseColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 🔥 브러쉬로 칠한 마스크 값 (0~1)
                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv).r;

                // 마스크 값 그대로 색에 반영
                float3 col = lerp(_BaseColor.rgb, _FillColor.rgb, mask);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
