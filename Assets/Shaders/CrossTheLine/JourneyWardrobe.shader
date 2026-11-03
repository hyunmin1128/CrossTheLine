Shader "CrossTheLine/JourneyWardrobe"
{
 Properties { _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _ClothTint("Clothing",Color)=(.5,.5,.4,1) _SpriteRect("Atlas",Vector)=(0,0,1,1) _WalkTexel("Native movement",Vector)=(0,0,0,0) _PreserveClothing("Original colors",Float)=0 }
 SubShader
 {
  Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex WardrobeVertex
   #pragma fragment WardrobeFragment
   #pragma multi_compile_instancing
   #pragma multi_compile _ SKINNED_SPRITE
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
   struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS };
   struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; };
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _Color;half4 _ClothTint;float4 _SpriteRect;float4 _WalkTexel;float _PreserveClothing;
   CBUFFER_END
   Varyings WardrobeVertex(Attributes input)
   {
    UNITY_SKINNED_VERTEX_COMPUTE(input);SetUpSpriteInstanceProperties();
    input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
    Varyings o=CommonUnlitVertex(input);o.color=input.color*_Color*unity_SpriteColor;return o;
   }
   half4 WardrobeFragment(Varyings input):SV_Target
   {
    float2 local=(input.uv-_SpriteRect.xy)/_SpriteRect.zw;
    float2 uv=input.uv;
    if(local.y<.29&&local.y>.06)uv.y-=max(0,(local.x<.5?1:-1)*_WalkTexel.z)*_WalkTexel.y;
    if(local.y>.34&&local.y<.53&&(local.x<.39||local.x>.61))uv.x+=round(_WalkTexel.z)*(local.x<.5?1:-1)*_WalkTexel.x;
    half4 t=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
    float3 g=LinearToSRGB(saturate(t.rgb));
    bool skin=g.r>.55&&g.g>.32&&g.r>g.g*1.09&&g.g>g.b*1.18;
    // Retain native faces, hands, shoes, silhouettes and the original three-level shading.
    if(_PreserveClothing<.5&&local.y>.12&&local.y<.58&&!skin&&max(g.r,max(g.g,g.b))>.28)
    {
     float value=max(g.r,max(g.g,g.b));float shade=value>.72?1.13:value>.46?.94:.67;
     t.rgb=SRGBToLinear(saturate(_ClothTint.rgb*shade));
    }
    return t*input.color;
   }
   ENDHLSL
  }
 }
}
