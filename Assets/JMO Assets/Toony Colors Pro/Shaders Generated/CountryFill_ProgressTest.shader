Shader "Custom/CountryFill_ProgressTest"
{
    Properties
    {
        _Progress ("Progress", Range(0,1)) = 0
        _FillColor ("Fill Color", Color) = (0.2, 0.8, 0.3, 1)
        _BaseColor ("Base Color", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry+1"
            "RenderType"="Transparent"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Progress;
            float4 _FillColor;
            float4 _BaseColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 좌 → 우 진행
                float visible = step(i.uv.x, _Progress);

                // 보이는 부분만 FillColor
                return lerp(_BaseColor, _FillColor, visible);
            }
            ENDHLSL
        }
    }
}
