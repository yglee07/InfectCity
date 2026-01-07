Shader "Hidden/CountryPaint_Coverage"
{
    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;   // 기존 coverage
            sampler2D _BrushTex;  // 브러시

            float2 _BrushUV;
            float _BrushSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float current = tex2D(_MainTex, i.uv).r;

                float2 d = i.uv - _BrushUV;
                float2 brushUV = d / _BrushSize + 0.5;

                float brush = tex2D(_BrushTex, brushUV).r;

                // 🔥 핵심: 누적 coverage
                float result = max(current, brush);

                return fixed4(result, result, result, 1);
            }
            ENDHLSL
        }
    }
}
