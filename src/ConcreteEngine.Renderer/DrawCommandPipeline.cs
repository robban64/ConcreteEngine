using ConcreteEngine.Graphics.Utility;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

internal sealed class DrawCommandPipeline(RenderUploadBuffers buffers)
{
    internal DrawStateOps DrawStateOps { get; private set; } = null!;

    private DrawCommandProcessor _drawCmd = null!;
    private UniformUploader _uniformUploader = null!;

    public void Initialize(RenderProgramContext ctx)
    {
        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload { Gfx = ctx.Gfx, Registry = ctx.Registry, };

        //
        _uniformUploader = new UniformUploader(drawCtx, drawCtxPayload, buffers);
        _drawCmd = new DrawCommandProcessor(drawCtx, drawCtxPayload, _uniformUploader);
        DrawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _uniformUploader);

        //
        _drawCmd.Initialize();
    }

    internal void Prepare()
    {
        buffers.Reset();
        _drawCmd.Prepare();
        _uniformUploader.ResetCursor();
    }

    internal void PrepareDrawBuffers()
    {
        // Sort command buffer and prepare passes
        buffers.Commands.ReadyDrawCommands();

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(buffers.Commands.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniform>(buffers.Materials.Count + 4);

        _uniformUploader.EnsureDrawBuffers(drawCap, matCap);
    }


    internal void UploadUniformGlobals(in RenderFrameArgs frameArgs)
    {
        _uniformUploader.UploadEngineUniformRecord(in frameArgs);

        if (VisualRenderContext.Instance.Environment.WasDirty)
        {
            _uniformUploader.UploadFrameUniformRecord();
            _uniformUploader.UploadDirLight();
            _uniformUploader.UploadPost();
        }

        _uniformUploader.UploadCameraView();
    }

    internal void UploadDrawUniformData()
    {
        var materialPayload = buffers.Materials.DrainBuffer();
        if (materialPayload.Length > 0)
            _uniformUploader.UploadMaterial(materialPayload);

        var transformPayload = buffers.Commands.DrainTransformBuffer();
        if (transformPayload.Length > 0)
            _uniformUploader.UploadDrawObjects(transformPayload);

        var animationPayload = buffers.Skinning.DrainBuffer();
        if (animationPayload.Length > 0)
            _uniformUploader.UploadAnimationData(animationPayload);
    }

    internal void ExecuteDrawPass(PassId passId, bool defaultDraw)
    {
        _uniformUploader.ResetCursor();
        _drawCmd.PrepareDrawPass();

        if (defaultDraw)
            buffers.Commands.DispatchDrawPass(_drawCmd, passId);
        else
            buffers.Commands.DispatchResolveDrawPass(_drawCmd, passId);
    }
}