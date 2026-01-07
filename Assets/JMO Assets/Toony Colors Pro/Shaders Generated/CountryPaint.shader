Shader "Hidden/CountryPaint"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;   // 기존 마스크
            sampler2D _BrushTex;  // 브러시
            float2 _BrushUV;
            float _BrushSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float baseMask = tex2D(_MainTex, i.uv).r;

                float2 d = i.uv - _BrushUV;
                float brush = tex2D(_BrushTex, d / _BrushSize + 0.5).r;

                float result = max(baseMask, brush);
                return result;
            }
            ENDHLSL
        }
    }
}
