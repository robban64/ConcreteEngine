using ConcreteEngine.Graphics.Utility;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

internal sealed class DrawCommandPipeline(RenderUploadBuffers buffers)
{
    public UniformUploader UniformUploader { get; private set; } = null!;
    public DrawStateContext DrawContext { get; private set; } = null!;

    private DrawCommandProcessor _drawCmd = null!;

    public void Initialize(RenderProgramContext ctx)
    {
        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload { Gfx = ctx.Gfx, Registry = ctx.Registry, };

        //
        DrawContext = drawCtx;
        UniformUploader = new UniformUploader(drawCtx, drawCtxPayload, buffers);
        _drawCmd = new DrawCommandProcessor(drawCtx, drawCtxPayload, UniformUploader);

        //
        _drawCmd.Initialize();
    }

    internal void Prepare()
    {
        buffers.Reset();
        _drawCmd.Prepare();
        UniformUploader.ResetCursor();
    }

    internal void PrepareDrawBuffers()
    {
        // Sort command buffer and prepare passes
        buffers.Commands.ReadyDrawCommands();

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(buffers.Commands.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniform>(buffers.Materials.Count + 4);

        UniformUploader.EnsureDrawBuffers(drawCap, matCap);
    }

    internal void UploadUniforms()
    {
        UniformUploader.UploadViewUniforms();

        var materialPayload = buffers.Materials.DrainBuffer();
        if (materialPayload.Length > 0)
            UniformUploader.UploadMaterial(materialPayload);

        var transformPayload = buffers.Commands.DrainTransformBuffer();
        if (transformPayload.Length > 0)
            UniformUploader.UploadDrawObjects(transformPayload);

        var animationPayload = buffers.Skinning.DrainBuffer();
        if (animationPayload.Length > 0)
            UniformUploader.UploadAnimationData(animationPayload);
    }

    internal void ExecuteDrawPass(PassId passId, bool defaultDraw)
    {
        UniformUploader.ResetCursor();
        _drawCmd.PrepareDrawPass();

        if (defaultDraw)
            buffers.Commands.DispatchDrawPass(_drawCmd, passId);
        else
            buffers.Commands.DispatchResolveDrawPass(_drawCmd, passId);
    }
}