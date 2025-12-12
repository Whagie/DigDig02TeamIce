using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthOnlyRenderFeature : ScriptableRendererFeature
{
    class DepthOnlyPass : ScriptableRenderPass
    {
        private ShaderTagId shaderTag = new("UniversalForward");
        private FilteringSettings filteringSettings;
        private Material depthMat;
        private string profilerTag = "DepthOnlyPass";

        public DepthOnlyPass(Material mat, LayerMask layerMask)
        {
            depthMat = mat;
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (depthMat == null)
                return;

            var cmd = CommandBufferPool.Get(profilerTag);
            var drawSettings = CreateDrawingSettings(shaderTag, ref renderingData, SortingCriteria.CommonOpaque);
            drawSettings.overrideMaterial = depthMat;

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [System.Serializable]
    public class Settings
    {
        public Material depthMaterial;
        public LayerMask layerMask = -1;
    }

    public Settings settings = new();
    DepthOnlyPass pass;

    public override void Create()
    {
        if (settings.depthMaterial != null)
            pass = new DepthOnlyPass(settings.depthMaterial, settings.layerMask);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass != null)
            renderer.EnqueuePass(pass);
    }
}
