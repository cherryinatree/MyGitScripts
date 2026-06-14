using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

[Serializable]
public class GhostWindowNoShaderPass : CustomPass
{
    [Header("Layers")]
    public LayerMask windowMaskLayer;  // e.g. GhostWindowMask
    public LayerMask ghostLayer;       // e.g. Ghost

    [Header("Stencil bit (HDRP safe bits)")]
    public UserStencilUsage userBit = UserStencilUsage.UserBit0; // 64 (UserBit1 = 128)

    [Header("Ghost visibility")]
    [Tooltip("If true, ghosts draw on top of walls (only inside the window). If false, normal depth occlusion applies.")]
    public bool xrayThroughWalls = true;

    [Header("Optional: Override Ghost Material")]
    public Material overrideGhostMaterial;

    ShaderTagId[] _shaderTags;

    protected override bool executeInSceneView => false;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        _shaderTags = new[]
        {
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("Forward"),
            new ShaderTagId("SRPDefaultUnlit"),
        };
    }

    // Ensures we can render Ghost layer even if the main camera culling mask excludes it.
    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
    {
        cullingParameters.cullingMask |= (uint)ghostLayer.value;
        cullingParameters.cullingMask |= (uint)windowMaskLayer.value;
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // We render into the normal camera buffers.
        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None);

        byte bit = (byte)userBit; // UserBit0=64, UserBit1=128

        // PASS A: Write stencil where the window is, WITHOUT writing color.
        DrawWindowMaskToStencil(ctx, bit);

        // PASS B: Draw ghosts only where stencil == bit.
        DrawGhostsThroughStencil(ctx, bit);
    }

    void DrawWindowMaskToStencil(CustomPassContext ctx, byte bit)
    {
        // Disable ALL color writes via BlendState writeMask.
        var rt0 = new RenderTargetBlendState();
        rt0.writeMask = (ColorWriteMask)0; // <- FIX: no "None" enum in Unity

        var blend = new BlendState();
        blend.blendState0 = rt0;

        var stencilWrite = new StencilState(
            enabled: true,
            readMask: 255,
            writeMask: bit,
            compareFunction: CompareFunction.Always,
            passOperation: StencilOp.Replace,
            failOperation: StencilOp.Keep,
            zFailOperation: StencilOp.Keep
        );

        var stateBlock = new RenderStateBlock(RenderStateMask.Blend | RenderStateMask.Depth | RenderStateMask.Stencil)
        {
            blendState = blend,
            depthState = new DepthState(writeEnabled: false, compareFunction: CompareFunction.LessEqual),
            stencilState = stencilWrite,
            stencilReference = bit
        };

        var desc = new RendererListDesc(_shaderTags, ctx.cullingResults, ctx.hdCamera.camera)
        {
            layerMask = windowMaskLayer,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.None,
            stateBlock = stateBlock
        };

        var list = ctx.renderContext.CreateRendererList(desc);
        CoreUtils.DrawRendererList(ctx.cmd, list);
    }
    void DrawGhostsThroughStencil(CustomPassContext ctx, byte bit)
    {
        var stencilTest = new StencilState(
            enabled: true,
            readMask: bit,
            writeMask: 0,
            compareFunction: CompareFunction.Equal,
            passOperation: StencilOp.Keep,
            failOperation: StencilOp.Keep,
            zFailOperation: StencilOp.Keep
        );

        var depthCompare = xrayThroughWalls ? CompareFunction.Always : CompareFunction.LessEqual;

        var stateBlock = new RenderStateBlock(RenderStateMask.Depth | RenderStateMask.Stencil)
        {
            depthState = new DepthState(writeEnabled: false, compareFunction: depthCompare),
            stencilState = stencilTest,
            stencilReference = bit
        };

        int passIndex = 0;
        if (overrideGhostMaterial != null)
        {
            int p = overrideGhostMaterial.FindPass("ForwardOnly");
            passIndex = p >= 0 ? p : 0;
        }

        var desc = new RendererListDesc(_shaderTags, ctx.cullingResults, ctx.hdCamera.camera)
        {
            layerMask = ghostLayer,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonTransparent,
            stateBlock = stateBlock,
            overrideMaterial = overrideGhostMaterial,
            overrideMaterialPassIndex = passIndex
        };

        var list = ctx.renderContext.CreateRendererList(desc);
        CoreUtils.DrawRendererList(ctx.cmd, list);
    }

    protected override void Cleanup() { }
}