Shader "Dirtmouth/Vignette"
{
    Properties { _MainTex("Texture",2D)="white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 frag(v2f_img i) : SV_Target
            {
                float d = length((i.uv-float2(0.5,0.51))*float2(1.06,0.95));
                return fixed4(0.002,0.004,0.012,smoothstep(0.31,0.72,d)*0.72);
            }
            ENDCG
        }
    }
}
