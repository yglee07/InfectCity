Shader "Custom/CountryMaskFill"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ConquerColor ("Conquer Color", Color) = (0.3,0.8,0.3,1)
        _Fill ("Fill Amount", Range(0,1)) = 0

        // Country Bounds (월드 기준)
        _WorldMinX ("World Min X", Float) = 0
        _WorldMaxX ("World Max X", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float worldX : TEXCOORD0;
            };

            fixed4 _BaseColor;
            fixed4 _ConquerColor;
            float _Fill;
            float _WorldMinX;
            float _WorldMaxX;

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldX = worldPos.x;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = saturate(
                    (i.worldX - _WorldMinX) /
                    max(0.0001, (_WorldMaxX - _WorldMinX))
                );

                float mask = step(t, _Fill);
                return lerp(_BaseColor, _ConquerColor, mask);
            }
            ENDCG
        }
    }
}
