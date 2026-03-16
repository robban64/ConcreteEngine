using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawCommandPipeline
{
    private readonly DrawCommandBuffer _commandBuffer;
    private readonly MaterialDrawBuffer _materialBuffer;

    private DrawCommandProcessor _drawCmdProc = null!;
    private DrawBuffers _drawBuffers = null!;
    private DrawStateOps _drawStateOps = null!;

    internal DrawStateOps DrawStateOps => _drawStateOps;
    internal DrawCommandBuffer CommandBuffer => _commandBuffer;

    internal DrawCommandPipeline()
    {
        _commandBuffer = new DrawCommandBuffer();
        _materialBuffer = new MaterialDrawBuffer();
    }

    public void Initialize(RenderProgramContext ctx)
    {
        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload { Gfx = ctx.Gfx, Registry = ctx.Registry, };

        //
        _drawBuffers = new DrawBuffers(drawCtx, drawCtxPayload);
        _drawCmdProc = new DrawCommandProcessor(drawCtx, drawCtxPayload, _drawBuffers);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawBuffers);

        //

        //
        _commandBuffer.Initialize(_drawCmdProc);
        _drawCmdProc.Initialize();
        _drawBuffers.Initialize(_materialBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SubmitMaterialDrawData(in RenderMaterialPayload payload, ReadOnlySpan<TextureBinding> slots) =>
        _materialBuffer.SubmitDrawData(in payload, slots);


    internal void Prepare()
    {
        _commandBuffer.Reset();
        _materialBuffer.Reset();

        _drawCmdProc.Prepare();
        _drawBuffers.ResetCursor();
    }

    internal void PrepareDrawBuffers()
    {
        // Sort command buffer and prepare passes
        _commandBuffer.ReadyDrawCommands();

        // Fill Material buffer
        // Happens in engine atm
        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_commandBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniformRecord>(_materialBuffer.Count + 4);

        _drawBuffers.EnsureDrawBuffers(drawCap, matCap);
    }


    internal void UploadUniformGlobals()
    {
        _drawBuffers.UploadGlobalUniforms();
        _drawBuffers.UploadCameraView();
    }

    internal void UploadDrawUniformData()
    {
        var materialPayload = _materialBuffer.DrainDrawMaterialData();
        if (materialPayload.Length > 0)
            _drawBuffers.UploadMaterial(materialPayload);

        var transformPayload = _commandBuffer.DrainTransformBuffer();
        if (transformPayload.Length > 0)
            _drawBuffers.UploadDrawObjects(transformPayload);

        var animationPayload = _commandBuffer.DrainBoneTransformBuffer();
        if (animationPayload.Length > 0)
            _drawBuffers.UploadAnimationData(animationPayload);
    }

    internal void ExecuteDrawPass(PassId passId, bool defaultDraw)
    {
        _drawBuffers.ResetCursor();
        _drawCmdProc.PrepareDrawPass();
        _commandBuffer.DispatchDrawPass(passId, defaultDraw);
    }
}