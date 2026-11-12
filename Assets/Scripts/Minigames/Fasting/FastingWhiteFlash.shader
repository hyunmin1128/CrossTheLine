Shader "CrossTheLine/FastingWhiteFlash"
{
    Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} _Flash ("White flash", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _Flash;
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes a){Varyings o;o.positionCS=TransformObjectToHClip(a.positionOS);o.uv=a.uv;o.color=a.color;return o;}
            half4 Frag(Varyings i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;c.rgb=lerp(c.rgb,half3(1,1,1),_Flash);return c;}
            ENDHLSL
        }
    }
}
