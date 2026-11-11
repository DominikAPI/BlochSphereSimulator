Shader "Custom/ProgressBar"
{
    Properties
    {
        _Progress("_Progress", Range(0, 1)) = 0
        _ColorStart("Color Start", Color) = (0.2, 1.0, 0.2, 1)
        _ColorFinal("Color Final", Color) = (1.0, 0.1, 0.1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque+1" "RenderPipeline" = "UniversalPipeline" "Queue" = "Overlay+1"}

        Pass
        {
           Blend SrcAlpha OneMinusSrcAlpha
           ZWrite Off
           ZTest Always
           Cull Off

           HLSLPROGRAM

           #pragma vertex vert
           #pragma fragment frag

           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

           struct Attributes
           {
               float4 positionOS : POSITION;
               float2 uv : TEXCOORD0;
           };

           struct Varyings
           {
               float4 positionHCS : SV_POSITION;
               float2 uv : TEXCOORD0;
           };

           TEXTURE2D(_BaseMap);
           SAMPLER(sampler_BaseMap);

           CBUFFER_START(UnityPerMaterial)
               float _Progress;
               float4 _ColorStart;
               float4 _ColorFinal;
           CBUFFER_END

           Varyings vert(Attributes IN)
           {
               Varyings OUT;
               OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
               OUT.uv = IN.uv;
               return OUT;
           }

           half4 frag(Varyings IN) : SV_Target
           {
               float fill = step(IN.uv.y, _Progress);
               float3 color = lerp(_ColorStart.rgb, _ColorFinal.rgb, _Progress);

               half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
               float3 finalColor = fill * color * tex.rgb;

               return half4(finalColor, tex.a);
           }
           ENDHLSL
        }
    }
}
