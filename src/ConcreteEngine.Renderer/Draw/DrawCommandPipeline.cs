using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawCommandPipeline
{
    internal DrawCommandBuffer CommandBuffer { get; }
    internal MaterialBuffer MaterialBuffer { get; }

    private DrawCommandProcessor _drawCmdProc = null!;
    private UniformUploader _uniformUploader = null!;
    private DrawStateOps _drawStateOps = null!;

    internal DrawStateOps DrawStateOps => _drawStateOps;

    internal DrawCommandPipeline()
    {
        CommandBuffer = new DrawCommandBuffer();
        MaterialBuffer = new MaterialBuffer();
    }

    public void Initialize(RenderProgramContext ctx)
    {
        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload { Gfx = ctx.Gfx, Registry = ctx.Registry, };

        //
        _uniformUploader = new UniformUploader(drawCtx, drawCtxPayload);
        _drawCmdProc = new DrawCommandProcessor(drawCtx, drawCtxPayload, _uniformUploader);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _uniformUploader);

        //

        //
        _drawCmdProc.Initialize();
        _uniformUploader.Initialize(MaterialBuffer);
    }

    internal void Prepare()
    {
        CommandBuffer.Reset();
        MaterialBuffer.Reset();

        _drawCmdProc.Prepare();
        _uniformUploader.ResetCursor();
    }

    internal void PrepareDrawBuffers()
    {
        // Sort command buffer and prepare passes
        CommandBuffer.ReadyDrawCommands();
        
        // Fill Material buffer
        // Happens in engine atm
        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(CommandBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniformRecord>(MaterialBuffer.Count + 4);

        _uniformUploader.EnsureDrawBuffers(drawCap, matCap);
    }


    internal void UploadUniformGlobals()
    {
        _uniformUploader.UploadGlobalUniforms();
        _uniformUploader.UploadCameraView();
    }

    internal void UploadDrawUniformData()
    {
        var materialPayload = MaterialBuffer.DrainDrawMaterialData();
        if (materialPayload.Length > 0)
            _uniformUploader.UploadMaterial(materialPayload);

        var transformPayload = CommandBuffer.DrainTransformBuffer();
        if (transformPayload.Length > 0)
            _uniformUploader.UploadDrawObjects(transformPayload);

        var animationPayload = CommandBuffer.DrainBoneTransformBuffer();
        if (animationPayload.Length > 0)
            _uniformUploader.UploadAnimationData(animationPayload);
    }

    internal void ExecuteDrawPass(PassId passId, bool defaultDraw)
    {
        _uniformUploader.ResetCursor();
        _drawCmdProc.PrepareDrawPass();
        
        if(defaultDraw)
            CommandBuffer.DispatchDrawPass(_drawCmdProc, passId);
        else 
            CommandBuffer.DispatchResolveDrawPass(_drawCmdProc, passId);

    }
}