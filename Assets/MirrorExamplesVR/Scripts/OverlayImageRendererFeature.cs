using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OverlayImageRendererFeature : ScriptableRendererFeature
{
    public Material overlayMaterial;
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;

    private OverlayImageRenderPass renderPass;

    public override void Create()
    {
        renderPass = new OverlayImageRenderPass(overlayMaterial)
        {
            renderPassEvent = injectionPoint
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (overlayMaterial == null)
        {
            Debug.LogWarning("OverlayImageRendererFeature: No material assigned.");
            return;
        }

        renderer.EnqueuePass(renderPass);
    }

    private class OverlayImageRenderPass : ScriptableRenderPass
    {
        private Material material;
        private RTHandle source;

        public OverlayImageRenderPass(Material material)
        {
            this.material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("OverlayImagePass");

            if (source != null)
            {
                Blit(cmd, source, source, material);
            }
            else
            {
                Debug.LogWarning("OverlayImageRenderPass: Source is null.");
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
