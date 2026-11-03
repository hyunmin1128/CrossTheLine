Shader "CrossTheLine/SubmergedSprite"
{
 Properties {
  [PerRendererData] _MainTex("Sprite",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _WaterColor("Water tint",Color)=(0.25,0.49,0.55,0.6)
  _WaterBounds("Water X min/max",Vector)=(-34,-14,0,0)
  _SurfaceY("Surface Y",Float)=-3.2
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   Tags {"LightMode"="Universal2D"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float4 color:COLOR; };
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _Color, _WaterColor, _WaterBounds; float _SurfaceY;
   CBUFFER_END
   Varyings vert(Attributes i) { UNITY_SETUP_INSTANCE_ID(i); SetUpSpriteInstanceProperties(); Varyings o; i.positionOS.xyz=UnityFlipSprite(i.positionOS.xyz,unity_SpriteProps.xy); o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.uv=i.uv; o.color=i.color*_Color*unity_SpriteColor; return o; }
   half4 frag(Varyings i):SV_Target {
    half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
    float wet=step(_WaterBounds.x,i.world.x)*step(i.world.x,_WaterBounds.y)*step(i.world.y,_SurfaceY);
    c.rgb=lerp(c.rgb,c.rgb*_WaterColor.rgb,wet*_WaterColor.a);
    return c;
   }
   ENDHLSL
  }
 }
}
