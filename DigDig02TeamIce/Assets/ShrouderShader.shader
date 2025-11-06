Shader "Custom/ShrouderShader"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _Alpha("Alpha", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "MainPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _Color;
            float _Alpha;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 posOS = float4(IN.positionOS, 1.0);
                OUT.positionHCS = TransformObjectToHClip(posOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_Color.rgb, _Color.a * _Alpha);
            }
            ENDHLSL
        }

        // === Depth-only pass (writes depth even when transparent) ===
        Pass
        {
            Name "DepthWrite"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 posOS = float4(IN.positionOS, 1.0);
                OUT.positionHCS = TransformObjectToHClip(posOS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Write nothing, just depth
                return 0;
            }
            ENDHLSL
        }
    }
}
