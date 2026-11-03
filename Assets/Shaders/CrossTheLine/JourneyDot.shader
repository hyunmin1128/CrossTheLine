Shader "CrossTheLine/JourneyDot"
{
 Properties
 {
  _MainTex ("Sprite",2D)="white"{}
  _Color ("Tint",Color)=(1,1,1,1)
  _SpriteRect ("Atlas rectangle",Vector)=(0,0,1,1)
  _PixelGrid ("Dot grid",Vector)=(64,64,0,0)
  _DotPalette ("Source palette lookup",2D)="white"{}
  _JesusPalette ("Original Jesus palette",2D)="white"{}
  _IsJesus ("Original Jesus colors",Float)=0
  _SourceJesus ("Original Jesus pixels",2D)="white"{}
  _HeadRect ("Original head placement",Vector)=(0,0,0,0)
  _HeadLeft ("Head faces left",Float)=0
  _WorldArt ("Background color range",Float)=0
  _HeadTears ("Small grief tear",Float)=0
  _HeadKind ("Head and raised arm mask",Float)=0
 }
 SubShader
 {
  Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  Cull Off
  ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex DotVertex
   #pragma fragment DotFragment
   #pragma multi_compile_instancing
   #pragma multi_compile _ SKINNED_SPRITE
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
   struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS };
   struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; };
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
   TEXTURE2D(_DotPalette);SAMPLER(sampler_DotPalette);
   TEXTURE2D(_JesusPalette);SAMPLER(sampler_JesusPalette);
   TEXTURE2D(_SourceJesus);SAMPLER(sampler_SourceJesus);
   CBUFFER_START(UnityPerMaterial)
    half4 _Color;float4 _SpriteRect;float4 _PixelGrid;float4 _HeadRect;float _HeadLeft;float _WorldArt;float _HeadTears;float _IsJesus;float _HeadKind;
   CBUFFER_END
   Varyings DotVertex(Attributes input)
   {
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    SetUpSpriteInstanceProperties();
    input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
    Varyings o=CommonUnlitVertex(input);o.color=input.color*_Color*unity_SpriteColor;return o;
   }
   half4 DotFragment(Varyings input):SV_Target
   {
    float2 local=(input.uv-_SpriteRect.xy)/_SpriteRect.zw;
    float2 grid=max(_PixelGrid.xy,1);
    float2 pixelUV=(min(floor(saturate(local)*grid),grid-1)+.5)/grid;
    float2 uv=_SpriteRect.xy+pixelUV*_SpriteRect.zw;
    float2 quarter=_SpriteRect.zw/grid*.22;
    half4 tex=(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+quarter)+SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv-quarter)+SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(quarter.x,-quarter.y))+SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(-quarter.x,quarter.y)))*.25;
    tex.a=step(.5,tex.a);
    float3 sourceGamma=LinearToSRGB(saturate(tex.rgb));
    float3 worldColor=SRGBToLinear(floor(LinearToSRGB(saturate(tex.rgb))*15+.5)/15);
    float3 key=floor(LinearToSRGB(saturate(tex.rgb))*31+.5);
    float index=key.r+key.g*32+key.b*1024;
    tex.rgb=SAMPLE_TEXTURE2D(_DotPalette,sampler_DotPalette,float2((fmod(index,1024)+.5)/1024,(floor(index/1024)+.5)/32)).rgb;
    if(_IsJesus>.5)tex.rgb=SAMPLE_TEXTURE2D(_JesusPalette,sampler_JesusPalette,float2((fmod(index,1024)+.5)/1024,(floor(index/1024)+.5)/32)).rgb;
    if(_IsJesus>.5)
    {
     float3 gamma=sourceGamma;
     float brightest=max(gamma.r,max(gamma.g,gamma.b)),darkest=min(gamma.r,min(gamma.g,gamma.b));
     if(brightest>.55&&darkest>.35&&brightest-darkest<.43)
     {
      float luminance=dot(gamma,float3(.25,.5,.25));
      float3 robe=luminance>.9?float3(217,213,208)/255:luminance>.72?float3(207,197,187)/255:float3(185,172,160)/255;
      tex.rgb=SRGBToLinear(robe);
     }
    }
    if(_WorldArt>.5)tex.rgb=worldColor;
    if(_HeadRect.z>0)
    {
     float2 head=(pixelUV-_HeadRect.xy)/_HeadRect.zw;
     // Cut only the face area; source sleeves and raised hands remain visible.
     // The exact native head is a separate full-rectangle sprite, so atlas meshes never crop it.
     bool face=head.x>.18&&head.x<.88&&head.y>.20&&head.y<=1.1;
     bool hair=head.x>-.12&&head.x<1.12&&head.y>.42&&head.y<=1.15;
     bool sleep=head.x>-.12&&head.x<1.12&&head.y>-.05&&head.y<=1.15;
     if(face||(_HeadKind<.5&&hair)||(_HeadKind>1.5&&sleep))tex.a=0;
    }
    return tex*input.color;
   }
   ENDHLSL
  }
 }
}
