using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraBlendFeature : ScriptableRendererFeature
{
    class CameraBlendPass : ScriptableRenderPass
    {
        private Material blendMaterial;
        private RenderTexture cameraA;
        private RenderTexture cameraB;
        private RenderTexture playerDepthRenderTexture;
        private RenderTexture sceneDepthRenderTexture;
        private Texture mask;
        private Mesh fullscreenQuad;
        private float nearDepth;
        private float farDepth;

        public CameraBlendPass(Material mat, RenderTexture camA, RenderTexture camB, RenderTexture playerDepth, RenderTexture sceneDepth, Texture maskTex, float nDepth, float fDepth)
        {
            blendMaterial = mat;
            cameraA = camA;
            cameraB = camB;
            playerDepthRenderTexture = playerDepth;
            sceneDepthRenderTexture = sceneDepth;
            mask = maskTex;
            nearDepth = nDepth;
            farDepth = fDepth;

            fullscreenQuad = CreateFullscreenQuad();
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        private Mesh CreateFullscreenQuad()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-1,-1,0),
                new Vector3(1,-1,0),
                new Vector3(-1,1,0),
                new Vector3(1,1,0)
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0,1), // top-left
                new Vector2(1,1), // top-right
                new Vector2(0,0), // bottom-left
                new Vector2(1,0)  // bottom-right
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            return mesh;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blendMaterial == null || cameraA == null || cameraB == null)
                return;

            var cmd = CommandBufferPool.Get("CameraBlendPass");

            // Assign textures to shader
            blendMaterial.SetTexture("_TexA", cameraA);
            blendMaterial.SetTexture("_TexB", cameraB);
            blendMaterial.SetTexture("_Mask", mask);
            blendMaterial.SetTexture("_PlayerDepth", playerDepthRenderTexture);
            blendMaterial.SetTexture("_SceneDepth", sceneDepthRenderTexture);
            blendMaterial.SetFloat("_NearDepth", nearDepth);
            blendMaterial.SetFloat("_FarDepth", farDepth);

            // Override camera matrices so quad fills the screen
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);

            cmd.DrawMesh(fullscreenQuad, Matrix4x4.identity, blendMaterial);

            // Restore camera matrices
            cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix,
                                          renderingData.cameraData.camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [System.Serializable]
    public class Settings
    {
        public Material blendMaterial;
        public Texture maskTexture;
        public RenderTexture cameraATexture;
        public RenderTexture cameraBTexture;
        public RenderTexture playerDepthTexture;
        public RenderTexture sceneDepthTexture;
        public float camNearDepth;
        public float camFarDepth;
    }

    public Settings settings = new Settings();

    private CameraBlendPass pass;

    public override void Create()
    {
        if (settings.blendMaterial != null)
        {
            pass = new CameraBlendPass(
                settings.blendMaterial,
                settings.cameraATexture,
                settings.cameraBTexture,
                settings.playerDepthTexture,
                settings.sceneDepthTexture,
                settings.maskTexture,
                settings.camNearDepth,
                settings.camFarDepth
            );
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass != null)
            renderer.EnqueuePass(pass);
    }
}
