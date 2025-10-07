Shader "CustomRenderTexture/LightCookieShader"
{
    Properties
    {
        _Color1 ("Color1", Color) = (1,1,1,1)
        _Tex1("Tex1", 2D) = "white" {}
        _Color2 ("Color2", Color) = (1,1,1,1)
        _Tex2("Tex2", 2D) = "white" {}
     }

     SubShader
     {
        Blend One Zero

        Pass
        {
            Name "LightCookieShader"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4      _Color1;
            float4      _Color2;

            sampler2D   _Tex1;
            sampler2D   _Tex2;

            float4      _Tex1_ST;
            float4      _Tex2_ST;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 uv1 = IN.globalTexcoord.xy * _Tex1_ST.xy + _Tex1_ST.zw;
                float2 uv2 = IN.globalTexcoord.xy * _Tex2_ST.xy + _Tex2_ST.zw;
                float4 color = (tex2D(_Tex1, uv1) * _Color1) + (tex2D(_Tex2, uv2) * _Color2);

                // // TODO: Replace this by actual code!
                // uint2 p = uv.xy * 256;
                // return countbits(~(p.x & p.y) + 1) % 2 * float4(uv, 1, 1) * color;
                return color;
            }
            ENDCG
        }
    }
}
