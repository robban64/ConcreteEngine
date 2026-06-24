using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Graphics.Utility;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

internal sealed class DrawCommandPipeline(RenderUploadBuffers buffers)
{
    public UniformUploader UniformUploader { get; private set; } = null!;
    private DrawCommandProcessor _drawCmd = null!;

    public void Initialize(RenderProgramContext ctx)
    {
        //
        UniformUploader = new UniformUploader(ctx.Gfx, ctx.Registry, buffers);
        _drawCmd = new DrawCommandProcessor(ctx.Gfx, ctx.Registry, UniformUploader);
    }

    internal void Prepare()
    {
        buffers.Reset();
        _drawCmd.Prepare();
        UniformUploader.Prepare();
    }

    internal void PrepareDrawBuffers()
    {
        // Sort command buffer and prepare passes
        avg.BeginSample();
        buffers.Commands.ReadyDrawCommands();
        if (avg.EndSample() >= 144) avg.ResetAndPrint();

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(buffers.Commands.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniform>(buffers.Materials.Count + 4);

        UniformUploader.EnsureUboSizes(buffers.Commands.Count + 32, buffers.Materials.Count + 4);
    }

    private AvgFrameTimer avg;

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
        UniformUploader.Prepare();
        _drawCmd.PrepareDrawPass();

        if (defaultDraw)
            buffers.Commands.DispatchDrawPass(_drawCmd, passId);
        else
            buffers.Commands.DispatchResolveDrawPass(_drawCmd, passId);
    }
}