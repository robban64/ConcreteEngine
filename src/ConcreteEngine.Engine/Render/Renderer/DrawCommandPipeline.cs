using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Engine.Render.Buffers;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Render.Renderer;

internal sealed class DrawCommandPipeline
{
    private readonly DrawCommandProcessor _drawCmd;
    private readonly RenderUploadBuffers _buffers;
    private readonly GfxBuffers _gfxBuffers;
    
    public DrawCommandPipeline(GfxContext gfx, RenderUploadBuffers buffers) {
        _buffers = buffers;
        _gfxBuffers = gfx.Buffers;
        _drawCmd = new DrawCommandProcessor(gfx, buffers);
    }
    
    internal void Prepare()
    {
        _buffers.Reset();
        _drawCmd.Prepare();
    }

    internal void PrepareDrawBuffers()
    {
        // Sort command buffer and prepare passes
        _buffers.Commands.ReadyDrawCommands();

        var drawCount = _buffers.Commands.Count + 32;
        var materialCount = _buffers.Materials.Count + 4;
        _ = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(drawCount);
        _ = UniformBufferUtils.GetCapacityForEntities<MaterialUniform>(materialCount);

        if (!GfxRegistry.GetMeta(DrawObjectUniform.UboId).HasCapacity(drawCount))
            _gfxBuffers.SetUniformBufferCount(DrawObjectUniform.UboId, drawCount);

        if (!GfxRegistry.GetMeta(MaterialUniform.UboId).HasCapacity(materialCount))
            _gfxBuffers.SetUniformBufferCount(MaterialUniform.UboId, materialCount);

    }

    internal void UploadUniforms()
    {
        VisualUniformProcessor.Instance.UploadMainView();

        var materialPayload = _buffers.Materials.DrainBuffer();
        if (materialPayload.Length > 0)
            UploadMaterials(materialPayload);

        var transformPayload = _buffers.Commands.DrainTransformBuffer();
        if (transformPayload.Length > 0)
            UploadDrawTransforms(transformPayload);

        var animationPayload = _buffers.Skinning.DrainBuffer();
        if (animationPayload.Length > 0)
            UploadBones(animationPayload);
    }

    private AvgFrameTimer avg;

    internal void ExecuteDrawPass(PassId passId, bool defaultDraw)
    {
        _drawCmd.PrepareDrawPass();
        avg.BeginSample();
        if (defaultDraw)
            _buffers.Commands.DispatchDrawPass(_drawCmd, passId);
        else
            _buffers.Commands.DispatchResolveDrawPass(_drawCmd, passId);
        
        if(avg.EndSample() > 144 * 4) avg.ResetAndPrint();

    }

    private void UploadMaterials(NativeView<MaterialUniform> data) => _gfxBuffers.UploadUniform(data, 0);
    private void UploadDrawTransforms(NativeView<DrawObjectUniform> data) => _gfxBuffers.UploadUniform(data, 0);
    private unsafe void UploadBones(NativeView<Matrix4x4> boneData)
    {
        if (!GfxRegistry.GetMeta(DrawAnimationUniform.UboId).HasCapacity(boneData.Length))
            _gfxBuffers.SetUniformBufferCount(DrawAnimationUniform.UboId, boneData.Length);

        var view = new NativeView<DrawAnimationUniform>((DrawAnimationUniform*)boneData.Ptr, boneData.Length);
        _gfxBuffers.UploadUniform(view, 0);
    }
    
}