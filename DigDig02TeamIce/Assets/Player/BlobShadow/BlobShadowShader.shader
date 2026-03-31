Shader "Custom/BlobShadow"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,0.5)
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float _Softness;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Convert UV (0–1) -> centered (-1..1)
                float2 centeredUV = i.uv * 2.0 - 1.0;

                // Radial distance
                float dist = length(centeredUV);

                // Soft circular falloff
                float alpha = smoothstep(1.0, 1.0 - _Softness, dist);

                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
}
