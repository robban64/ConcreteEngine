using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

internal sealed class DrawCommandPipeline(RenderUploadBuffers buffers)
{
    private readonly DrawCommandBuffer _commandBuffer = buffers.CommandBuffer;
    private readonly MaterialBuffer _materialBuffer = buffers.MaterialBuffer;
    private readonly SkinningBuffer _skinningBuffer = buffers.SkinningBuffer;

    internal DrawStateOps DrawStateOps { get; private set; } = null!;

    private DrawCommandProcessor _drawCmd = null!;
    private UniformUploader _uniformUploader = null!;

    public void Initialize(RenderProgramContext ctx)
    {
        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload { Gfx = ctx.Gfx, Registry = ctx.Registry, };

        //
        _uniformUploader = new UniformUploader(drawCtx, drawCtxPayload);
        _drawCmd = new DrawCommandProcessor(drawCtx, drawCtxPayload, _uniformUploader);
        DrawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _uniformUploader);

        //
        _drawCmd.Initialize();
        _uniformUploader.Initialize(_materialBuffer);
    }

    internal void Prepare()
    {
        _commandBuffer.Reset();
        _materialBuffer.Reset();
        _skinningBuffer.Reset();

        _drawCmd.Prepare();
        _uniformUploader.ResetCursor();
    }

    internal void PrepareDrawBuffers()
    {
        // Sort command buffer and prepare passes
        _commandBuffer.ReadyDrawCommands();

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_commandBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniform>(_materialBuffer.Count + 4);

        _uniformUploader.EnsureDrawBuffers(drawCap, matCap);
    }


    internal void UploadUniformGlobals()
    {
        _uniformUploader.UploadGlobalUniforms();
        _uniformUploader.UploadCameraView();
    }

    internal void UploadDrawUniformData()
    {
        var materialPayload = _materialBuffer.DrainBuffer();
        if (materialPayload.Length > 0)
            _uniformUploader.UploadMaterial(materialPayload);

        var transformPayload = _commandBuffer.DrainTransformBuffer();
        if (transformPayload.Length > 0)
            _uniformUploader.UploadDrawObjects(transformPayload);

        var animationPayload = _skinningBuffer.DrainBuffer();
        if (animationPayload.Length > 0)
            _uniformUploader.UploadAnimationData(animationPayload);
    }

    internal void ExecuteDrawPass(PassId passId, bool defaultDraw)
    {
        _uniformUploader.ResetCursor();
        _drawCmd.PrepareDrawPass();

        if (defaultDraw)
            _commandBuffer.DispatchDrawPass(_drawCmd, passId);
        else
            _commandBuffer.DispatchResolveDrawPass(_drawCmd, passId);
    }
}