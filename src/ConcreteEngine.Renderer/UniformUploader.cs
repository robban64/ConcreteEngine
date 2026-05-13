using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed unsafe class UniformUploader
{
    private readonly RenderUbo _drawUbo;
    private readonly RenderUbo _materialUbo;
    private readonly RenderUbo _animationUbo;

    private readonly DrawStateContext _ctx;
    private readonly GfxBuffers _gfxBuffers;
    private readonly MaterialBuffer _materialBuffer;
    private readonly EffectBuffer _effectBuffer;

    public readonly UniformUploadContext UploadCtx;
    
    private static VisualRenderContext RenderContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => VisualRenderContext.Instance;
    }

    internal UniformUploader(DrawStateContext ctx, DrawStateContextPayload ctxPayload, RenderUploadBuffers buffers)
    {
        _ctx = ctx;
        _materialBuffer = buffers.Materials;
        _effectBuffer = buffers.Effects;

        _gfxBuffers = ctxPayload.Gfx.Buffers;
        UploadCtx = new UniformUploadContext(_gfxBuffers);
        
        var registry = ctxPayload.Registry.UboRegistry;

        _drawUbo = registry.GetRenderUbo<DrawObjectUniform>();
        _materialUbo = registry.GetRenderUbo<MaterialUniform>();
        _animationUbo = registry.GetRenderUbo<DrawAnimationUniform>();

        _animationUbo.SetCapacity(_animationUbo.Stride * 64);
        _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, _animationUbo.Capacity);
        
        UploadLight(); // set the buffer
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ResetCursor()
    {
        _drawUbo.ResetCursor();
        _materialUbo.ResetCursor();
        _animationUbo.ResetCursor();
    }

    internal void EnsureDrawBuffers(uint drawCapacity, uint materialCapacity)
    {
        if (drawCapacity > _drawUbo.Capacity)
        {
            _drawUbo.SetCapacity(drawCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, drawCapacity);
        }

        if (materialCapacity > _materialUbo.Capacity)
        {
            _materialUbo.SetCapacity(materialCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_materialUbo.Id, drawCapacity);
        }
    }

    internal ReadOnlySpan<TextureBinding> ResolveMaterial(MaterialId materialId, out RenderMaterialMeta materialMeta)
    {
        if (_ctx.ResolveMaterialBind(materialId))
        {
            BindMaterialObject(materialId);
            return _materialBuffer.GetMetaAndSlots(materialId, out materialMeta);
        }

        materialMeta = default;
        return ReadOnlySpan<TextureBinding>.Empty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindMaterialObject(MaterialId matId)
    {
        var cursor = _materialUbo.SetDrawCursor(matId.Index());
        _gfxBuffers.BindUniformBufferRange(_materialUbo.Id, _materialUbo.Slot, cursor, _materialUbo.Stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindDrawObject(int submitIndex)
    {
        var cursor = _drawUbo.SetDrawCursor(submitIndex);
        _gfxBuffers.BindUniformBufferRange(_drawUbo.Id, _drawUbo.Slot, cursor, _drawUbo.Stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindAnimation(int slot)
    {
        var cursor = _animationUbo.SetDrawCursor(slot);
        _gfxBuffers.BindUniformBufferRange(_animationUbo.Id, cursor, _animationUbo.Stride);
    }

    internal void UploadMaterial(NativeView<MaterialUniform> data) =>
        _gfxBuffers.UploadUniform(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));

    internal void UploadDrawObjects(NativeView<DrawObjectUniform> data) =>
        _gfxBuffers.UploadUniform(_drawUbo.Id, data, _drawUbo.SetUploadCursor(0));


    internal void UploadAnimationData(NativeView<Matrix4x4> boneData)
    {
        var uploadSize = _animationUbo.GetCapacityFor(boneData.Length);
        if (uploadSize > _animationUbo.Capacity)
        {
            _animationUbo.SetCapacity(uploadSize);
            _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, uploadSize);
        }

        _gfxBuffers.UploadUniform(_animationUbo.Id, boneData, 0);
        //_gfxBuffers.UploadUniformBytes(_animationUbo.Id, boneData.Reinterpret<byte>(), boneData.Length, stride, 0);
    }

    // Globals //
    internal void UploadEditorEffectUniform(byte slot, bool isAnimated)
    {
        ref readonly var effect = ref _effectBuffer.GetResolveEffect(slot);
        var data = new EditorEffectsUniform(isAnimated, effect.Color);
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<EditorEffectsUniform>(), &data, 0);
    }

    internal void UploadViewUniforms()
    {
        if (_ctx.IsDepth)
        {
            RenderContext.UploadShadow(UploadCtx);
            RenderContext.UploadLightView(UploadCtx);
            return;
        }
        
        RenderContext.UploadMainView(UploadCtx);    
    }

    public void UploadLight()
    {
        LightUniform data = default;
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<LightUniform>(), &data, 0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniform<T>(T* data) where T : unmanaged, IUniform =>
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<T>(), data, 0);
}

public sealed unsafe class UniformUploadContext(GfxBuffers gfxBuffers)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniform<T>(T* data) where T : unmanaged, IUniform =>
        gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<T>(), data, 0);
}