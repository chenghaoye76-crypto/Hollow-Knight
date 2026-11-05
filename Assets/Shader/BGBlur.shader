// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/UI_BGBlur"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0.0, 5.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _BlurSize;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Gaussian blur calculation
                float2 uv = i.uv;
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float2 offset1 = texelSize * _BlurSize;
                float2 offset2 = texelSize * _BlurSize * 2.0;

                half4 col = tex2D(_MainTex, uv) * 0.2270270270;
                col += tex2D(_MainTex, uv + offset1) * 0.1945945946;
                col += tex2D(_MainTex, uv - offset1) * 0.1945945946;
                col += tex2D(_MainTex, uv + offset2) * 0.1216216216;
                col += tex2D(_MainTex, uv - offset2) * 0.1216216216;

                return col;
            }
            ENDCG
        }
    }
}
