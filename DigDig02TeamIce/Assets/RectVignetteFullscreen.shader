Shader "Hidden/RectVignetteFullscreenCorners"
{
    Properties
    {
        _Color ("Vignette Color", Color) = (0,0,0,1)
        _Intensity ("Intensity", Range(0,1)) = 0.5
        _EdgeWidth ("Edge Width", Range(0,1)) = 0.2
        _FadeAmount ("Edge Fade Amount", Range(0,1)) = 0.25
        _CornerFade ("Corner Fade Distance", Range(0,1)) = 0.05
        _Falloff ("Fade Falloff", Range(0.1,5)) = 1.0
        _WidthRatio ("Width Ratio", Range(0.1,2)) = 1.0
        _HeightRatio ("Height Ratio", Range(0.1,2)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;
            float _Intensity;
            float _EdgeWidth;
            float _FadeAmount;
            float _CornerFade;
            float _Falloff;
            float _WidthRatio;
            float _HeightRatio;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // center UVs to -1..1
                float2 uv = i.uv * 2.0 - 1.0;

                // scale UVs by aspect ratio
                uv.x /= _WidthRatio;
                uv.y /= _HeightRatio;

                // ----- rectangular edge fade -----
                float distX = 1.0 - abs(uv.x);
                float distY = 1.0 - abs(uv.y);

                float fadeStart = _EdgeWidth - _FadeAmount;

                float tX = saturate((distX - fadeStart) / (_FadeAmount + 1e-5));
                float tY = saturate((distY - fadeStart) / (_FadeAmount + 1e-5));

                float vignetteRect = 1.0 - pow(min(tX, tY), _Falloff);

                // ----- corner fade (independent) -----
                float dx = max(0.0, abs(uv.x) - _EdgeWidth);
                float dy = max(0.0, abs(uv.y) - _EdgeWidth);
                float cornerDist = length(float2(dx, dy));
                float cornerFade = saturate(cornerDist / (_CornerFade + 1e-5));
                cornerFade = pow(cornerFade, _Falloff);

                // combine rectangular and corner
                float finalVignette = max(vignetteRect, cornerFade);

                return _Color * finalVignette * _Intensity;
            }
            ENDHLSL
        }
    }
}
