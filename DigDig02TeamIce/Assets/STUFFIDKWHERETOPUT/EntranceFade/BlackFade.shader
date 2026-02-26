Shader "Unlit/BlackFadeLocal"
{
    Properties
    {
        _FadeColor("Fade Color", Color) = (0,0,0,1)
        _FadeStart("Fade Start", Float) = 0.0
        _FadeEnd("Fade End", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Forward"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionLS : TEXCOORD0; // store local position
            };

            float4 _FadeColor;
            float _FadeStart;
            float _FadeEnd;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionLS = input.positionOS.xyz; // keep local position for fade
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Use local Z axis as fade direction
                float fadeDir = input.positionLS.z;

                // Remap fade between start/end
                float fade = saturate((fadeDir - _FadeStart) / max(_FadeEnd - _FadeStart, 0.0001));

                // Output fade color with alpha
                return half4(_FadeColor.rgb, fade * _FadeColor.a);
            }
            ENDHLSL
        }
    }
}
