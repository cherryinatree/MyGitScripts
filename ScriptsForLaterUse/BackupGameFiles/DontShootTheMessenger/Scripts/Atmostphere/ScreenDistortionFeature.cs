using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class ScreenDistortionFeature : ScriptableRendererFeature
{
    // Nested pass
    class DistortionPass : ScriptableRenderPass
    {
        private Material distortionMaterial;
        private RTHandle temporaryColorTexture;

        public DistortionPass(Material mat)
        {
            this.distortionMaterial = mat;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Create a temporary RT
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref temporaryColorTexture, descriptor, name: "_TemporaryColorTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (distortionMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("DistortionPass");

            // Copy camera color target into temporary RT
            Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, temporaryColorTexture);

            // Apply distortion and blit back
            Blitter.BlitCameraTexture(cmd, temporaryColorTexture, renderingData.cameraData.renderer.cameraColorTargetHandle, distortionMaterial, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Cleanup handled by RTHandle system
        }
    }

    [System.Serializable]
    public class DistortionSettings
    {
        public Material distortionMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public DistortionSettings settings = new DistortionSettings();

    DistortionPass distortionPass;

    public override void Create()
    {
        if (settings.distortionMaterial != null)
        {
            distortionPass = new DistortionPass(settings.distortionMaterial)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.distortionMaterial != null)
            renderer.EnqueuePass(distortionPass);
    }
}