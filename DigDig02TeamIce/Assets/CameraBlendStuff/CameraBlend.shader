Shader "Custom/CameraBlendPlayerDepthMask"
{
    Properties
    {
        _TexA ("Camera A", 2D) = "white" {}
        _TexB ("Camera B", 2D) = "black" {}
        _Mask ("Mask", 2D) = "gray" {}
        _PlayerDepth ("Player Depth", 2D) = "black" {}
        _SceneDepth ("Scene Depth", 2D) = "black" {}
        _NearDepth ("Near Depth", Float) = 20
        _FarDepth  ("Far Depth", Float) = 1000
        _MaskCenter ("Mask Center (0-1)", Vector) = (0.5,0.5,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float4 _MaskCenter;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = float4(IN.positionOS.xy, 0, 1);
                OUT.uv = IN.uv;
                return OUT;
            }

            TEXTURE2D(_TexA); SAMPLER(sampler_TexA);
            TEXTURE2D(_TexB); SAMPLER(sampler_TexB);
            TEXTURE2D(_Mask); SAMPLER(sampler_Mask);
            TEXTURE2D(_PlayerDepth); SAMPLER(sampler_PlayerDepth);
            TEXTURE2D(_SceneDepth); SAMPLER(sampler_SceneDepth);
            float _NearDepth;
            float _FarDepth;

            float4 frag(Varyings IN) : SV_Target
            {
                float4 colA = SAMPLE_TEXTURE2D(_TexA, sampler_TexA, IN.uv);
                float4 colB = SAMPLE_TEXTURE2D(_TexB, sampler_TexB, IN.uv);

                // Player-centered mask UV
                float2 uvMask = IN.uv - _MaskCenter.xy;
                uvMask = saturate(uvMask + 0.5);
                float maskVal = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uvMask).r;

                // Sample depth textures
                float playerDepthRaw = SAMPLE_TEXTURE2D(_PlayerDepth, sampler_PlayerDepth, IN.uv).r;
                float sceneDepthRaw  = SAMPLE_TEXTURE2D(_SceneDepth, sampler_SceneDepth, IN.uv).r;

                // Remap depth for orthographic camera
                float playerDepth = saturate((playerDepthRaw - _NearDepth) / (_FarDepth - _NearDepth));
                float sceneDepth  = saturate((sceneDepthRaw  - _NearDepth) / (_FarDepth - _NearDepth));

                // 1 = visible, 0 = occluded by scene
                float v = saturate(sceneDepth * 2.5);   // scale up small depth range
                float v2 = saturate(playerDepth * 2.5);   // scale up small depth range
                v = saturate((v - 0.5) * 2.0 + 0.5);        // apply contrast
                v2 = saturate((v2 - 0.5) * 2.0 + 0.5);        // apply contrast

                float depthMask = step(v, v2);

                // Combine depth with mask
                //float finalMask = min(depthMask, maskVal);
                //float finalMask = depthMask;

                float finalMask = maskVal * (1 - depthMask) + depthMask;

                // Output as grayscale
                //return float4(finalMask, finalMask, finalMask, 1.0);
                return lerp(colA, colB, finalMask);
            }
            ENDHLSL
        }
    }
}
