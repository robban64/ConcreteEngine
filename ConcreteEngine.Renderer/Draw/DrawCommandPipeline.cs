#region

using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

#endregion

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawCommandPipeline
{
    private DrawCommandBuffer _commandBuffer = null!;
    private MaterialDrawBuffer _materialBuffer = null!;

    private DrawCommandProcessor _drawCmdProc = null!;
    private DrawBuffers _drawBuffers = null!;
    private DrawStateOps _drawStateOps = null!;

    private RenderStateContext _stateContext = null!;

    internal DrawStateOps DrawStateOps => _drawStateOps;
    internal DrawBuffers DrawBuffer => _drawBuffers;
    internal DrawCommandProcessor DrawCmdProcessor => _drawCmdProc;
    internal DrawCommandBuffer CommandBuffer => _commandBuffer;

    internal DrawCommandPipeline()
    {
    }

    public void Initialize(RenderEngineContext ctx, RenderStateContext stateContext)
    {
        _stateContext = stateContext;

        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload
        {
            Gfx = ctx.Gfx, Registry = ctx.Registry, RenderView = _stateContext.View, Snapshot = _stateContext.Snapshot
        };

        //
        _drawBuffers = new DrawBuffers(drawCtx, drawCtxPayload);
        _drawCmdProc = new DrawCommandProcessor(drawCtx, drawCtxPayload, _drawBuffers);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawBuffers);

        //
        _commandBuffer = new DrawCommandBuffer(_drawCmdProc, _drawBuffers);
        _materialBuffer = new MaterialDrawBuffer();

        //
        _commandBuffer.Initialize();
        _drawCmdProc.Initialize();
        _drawBuffers.AttachMaterialBuffer(_materialBuffer);
    }

    internal void SubmitMaterialDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
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
        _drawBuffers.UploadGlobalUniforms(in _stateContext.CurrentFrameInfo, in _stateContext.CurrentRuntimeParams);
        _drawBuffers.UploadCameraView(_stateContext.View);
    }

    internal void UploadDrawUniformData()
    {
        _drawBuffers.UploadMaterial(_materialBuffer.DrainDrawMaterialData());
        _drawBuffers.UploadDrawObjects(_commandBuffer.DrainTransformQueue());
    }

    internal void ExecuteDrawPass(PassId passId)
    {
        _drawBuffers.ResetCursor();
        _drawCmdProc.PrepareDrawPass();
        _commandBuffer.DispatchDrawPass(passId, _drawCmdProc);
    }
}