Shader "Dirtmouth/AnimatedMist"
{
    Properties { _Mode("Mode", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float2 world:TEXCOORD1; };
            float _Mode, _FlowTime;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; o.world=mul(unity_ObjectToWorld,v.vertex).xy; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p) { float2 a=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y); }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float a;
                if(_Mode>0.5)
                {
                    // World-space advection makes the strands visibly travel across the street.
                    // A separate CPU clock also animates Scene view and deterministic preview renders.
                    float2 q=float2((i.world.x-_FlowTime*0.65)*0.32,i.world.y*1.4);
                    float warp=noise(q*0.55+float2(0,_FlowTime*0.065));
                    q.y+=(warp-0.5)*0.9;
                    float n=noise(q)*0.78+noise(q*2.03+float2(-_FlowTime*0.08,1.7))*0.22;
                    float strands=0.18+0.65*n;
                    float edge=pow(saturate(1-p.x*p.x),2)*pow(saturate(1-p.y*p.y),2);
                    a=edge*strands;
                }
                else a=exp(-dot(p,p)*7)*(1-smoothstep(0.7,1,length(p)));
                return float4(i.color.rgb,i.color.a*a);
            }
            ENDHLSL
        }
    }
}
