Shader "Dirtmouth/DistanceBlur"
{
    Properties { [PerRendererData] _MainTex("Sprite",2D)="white" {} _Radius("Blur in texels",Float)=5 _EnvironmentTint("Environment tint",Color)=(0.48,0.54,0.76,1) _ShadowDepth("Shadow depth",Range(0,1))=0.14 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _Radius; float4 _UvRect; float4 _EnvironmentTint; float _ShadowDepth;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float4 samplePremult(float2 uv){float inside=step(_UvRect.x,uv.x)*step(_UvRect.y,uv.y)*step(uv.x,_UvRect.z)*step(uv.y,_UvRect.w);float4 c=tex2D(_MainTex,clamp(uv,_UvRect.xy+_MainTex_TexelSize.xy*0.5,_UvRect.zw-_MainTex_TexelSize.xy*0.5));c.rgb*=c.a;return c*inside;}
            float4 frag(v2f i):SV_Target
            {
                float2 d=_MainTex_TexelSize.xy*_Radius;
                float4 c=samplePremult(i.uv)*0.20;
                c+=(samplePremult(i.uv+float2(d.x,0))+samplePremult(i.uv-float2(d.x,0))+samplePremult(i.uv+float2(0,d.y))+samplePremult(i.uv-float2(0,d.y)))*0.12;
                c+=(samplePremult(i.uv+d)+samplePremult(i.uv-d)+samplePremult(i.uv+float2(d.x,-d.y))+samplePremult(i.uv+float2(-d.x,d.y)))*0.08;
                // Grade straight RGB, then premultiply again to keep blurred transparent edges clean.
                float3 rgb=c.rgb/max(c.a,0.0001);
                rgb=pow(max(rgb,0),1+_ShadowDepth);
                c.rgb=rgb*_EnvironmentTint.rgb*c.a;
                c.rgb*=i.color.rgb*i.color.a;c.a*=i.color.a;return c;
            }
            ENDHLSL
        }
    }
}
