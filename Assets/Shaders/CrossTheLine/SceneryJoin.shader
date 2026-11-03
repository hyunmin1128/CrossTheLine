Shader "CrossTheLine/SceneryJoin"
{
 Properties { [PerRendererData] _MainTex("Main",2D)="white"{} _LeftTex("Left landscape",2D)="white"{} _RightTex("Right landscape",2D)="white"{} _LeftBounds("Left bounds",Vector)=(0,0,1,1) _RightBounds("Right bounds",Vector)=(0,0,1,1) _LeftUV("Left UV",Vector)=(0,0,1,1) _RightUV("Right UV",Vector)=(0,0,1,1) _Join("Join",Vector)=(0,10,0,0) _JoinFlips("Flips",Vector)=(0,0,0,0) }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True"}
  Pass
  {
   Tags { "LightMode"="Universal2D" }
   ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   Texture2D _MainTex : register(t0); SamplerState sampler_MainTex : register(s0);
   Texture2D _LeftTex : register(t1); SamplerState sampler_LeftTex : register(s1);
   Texture2D _RightTex : register(t2); SamplerState sampler_RightTex : register(s2);
   CBUFFER_START(UnityPerMaterial)
   float4 _LeftBounds,_RightBounds,_LeftUV,_RightUV,_Join,_JoinFlips;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float2 world:TEXCOORD0;};
   V vert(A v){V o;float3 p=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(p);o.world=p.xy;return o;}
   float2 uvFor(float2 p,float4 b,float4 rect,float flip){float2 uv=saturate((p-b.xy)/max(b.zw,float2(.001,.001)));if(flip>.5)uv.x=1-uv.x;return rect.xy+uv*rect.zw;}
   half4 frag(V i):SV_Target
   {
    float2 p=floor(i.world*32)/32;
    float t=smoothstep(_Join.x-_Join.y*.5,_Join.x+_Join.y*.5,p.x);
    // Sample on the shared world pixel grid; palette steps avoid subpixel filtering.
    float2 cell=floor(p*16);float noise=frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);
    t=saturate(t);
    half4 left=SAMPLE_TEXTURE2D(_LeftTex,sampler_LeftTex,uvFor(p,_LeftBounds,_LeftUV,_JoinFlips.x));
    half4 right=SAMPLE_TEXTURE2D(_RightTex,sampler_RightTex,uvFor(p,_RightBounds,_RightUV,_JoinFlips.y));
    half4 c=lerp(left,right,t);
    c.rgb=floor(c.rgb*63+.5)/63;
    float rim=saturate((_Join.y*.5-abs(p.x-_Join.x))/.5);
    c.a*=rim*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,float2(.5,.5)).a;return c;
   }
   ENDHLSL
  }
 }
}






