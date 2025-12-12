using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraBlendPass : ScriptableRenderPass
{
    private Material blendMaterial;
    private RTHandle cameraA;
    private RTHandle cameraB;
    private Texture mask;

    private RTHandle destination;

    public CameraBlendPass(Material blendMat, RTHandle camA, RTHandle camB, Texture maskTex)
    {
        blendMaterial = blendMat;
        cameraA = camA;
        cameraB = camB;
        mask = maskTex;
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents; // adjust as needed
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (blendMaterial == null || cameraA == null || cameraB == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get("CameraBlend");

        blendMaterial.SetTexture("_TexA", cameraA);
        blendMaterial.SetTexture("_TexB", cameraB);
        blendMaterial.SetTexture("_Mask", mask);

        // Blit using URP-safe API
        Blitter.BlitCameraTexture(cmd, cameraA, renderingData.cameraData.renderer.cameraColorTargetHandle, blendMaterial, 0);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
