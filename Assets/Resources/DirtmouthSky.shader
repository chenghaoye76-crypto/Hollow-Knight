Shader "Dirtmouth/NightSky"
{
    Properties { [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float2 world : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.world = mul(unity_ObjectToWorld, v.vertex).xy; return o; }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            fixed4 frag(v2f i) : SV_Target
            {
                float haze = exp(-pow((i.world.y - 1.8) * 0.073, 2));
                float3 colour = lerp(float3(0.002,0.004,0.012), float3(0.028,0.046,0.095), haze);
                float2 grid = i.world * 1.1;
                float2 cell = floor(grid);
                float2 starPoint = float2(hash(cell), hash(cell+31.7)) * 0.7 + 0.15;
                float star = (1-smoothstep(0.006,0.025,length(frac(grid)-starPoint))) * step(0.91,hash(cell+79.3));
                colour += star * float3(0.22,0.27,0.36) * smoothstep(2,9,i.world.y);
                return float4(colour,1);
            }
            ENDHLSL
        }
    }
}
