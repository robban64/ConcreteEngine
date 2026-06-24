using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed unsafe class UniformUploader
{
    private readonly RenderUbo _drawUbo;
    private readonly RenderUbo _materialUbo;
    private readonly RenderUbo _animationUbo;

    private readonly GfxBuffers _gfxBuffers;
    private readonly MaterialBuffer _materialBuffer;
    private readonly SkinningBuffer _skinningBuffer;
    private readonly EffectBuffer _effectBuffer;

    public Id16<MaterialSlot> PrevMaterial { get; private set; } = new(-1);


    internal UniformUploader(GfxContext gfx, RenderRegistry renderRegistry, RenderUploadBuffers buffers)
    {
        _materialBuffer = buffers.Materials;
        _skinningBuffer = buffers.Skinning;
        _effectBuffer = buffers.Effects;

        _gfxBuffers = gfx.Buffers;

        var registry = renderRegistry.UboRegistry;

        _drawUbo = registry.GetRenderUbo<DrawObjectUniform>();
        _materialUbo = registry.GetRenderUbo<MaterialUniform>();
        _animationUbo = registry.GetRenderUbo<DrawAnimationUniform>();

        UploadLight(); // set the buffer
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Prepare()
    {
        _drawUbo.ResetCursor();
        _materialUbo.ResetCursor();
        _animationUbo.ResetCursor();

        PrevMaterial = default;
    }

    internal void EnsureUboSizes(int drawCount, int materialCount)
    {
        if (drawCount * _drawUbo.Stride > _drawUbo.Capacity)
        {
            var capacity = _drawUbo.GetCapacityFor(drawCount);
            _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, capacity);
        }

        if (materialCount * _materialUbo.Stride > _materialUbo.Capacity)
        {
            var capacity = _materialUbo.GetCapacityFor(materialCount);
            _gfxBuffers.SetUniformBufferCapacity(_materialUbo.Id, capacity);
        }
    }

    internal ReadOnlySpan<TextureBinding> ResolveMaterial(Id16<MaterialSlot> materialId,
        out RenderMaterialMeta materialMeta)
    {
        if (PrevMaterial != materialId)
        {
            PrevMaterial = materialId;
            BindMaterialObject(materialId);
            return _materialBuffer.GetMetaAndSlots(materialId, out materialMeta);
        }

        materialMeta = default;
        return ReadOnlySpan<TextureBinding>.Empty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindMaterialObject(Id16<MaterialSlot> matId)
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
        var range = _skinningBuffer.GetSlotRange(slot);
        _gfxBuffers.BindUniformBufferRange(_animationUbo.Id, range.Offset * 64, range.Length * 64);
    }

    internal void UploadMaterial(NativeView<MaterialUniform> data) =>
        _gfxBuffers.UploadUniform(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));

    internal void UploadDrawObjects(NativeView<DrawObjectUniform> data) =>
        _gfxBuffers.UploadUniform(_drawUbo.Id, data, _drawUbo.SetUploadCursor(0));


    internal void UploadAnimationData(NativeView<Matrix4x4> boneData)
    {
        var uploadSize = boneData.Length * 64;
        if (uploadSize > _animationUbo.Capacity)
            _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, uploadSize);

        _gfxBuffers.UploadUniform(_animationUbo.Id, boneData, 0);
    }

    // Globals //
    internal void UploadEditorEffectUniform(byte slot, bool isAnimated)
    {
        ref readonly var effect = ref _effectBuffer.Get(slot);
        var data = new EditorEffectsUniform(isAnimated, effect.Color);
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<EditorEffectsUniform>(), &data, 0);
    }

    internal void UploadViewUniforms()
    {
        var ctx = GetUploadContext();
        var callbacks = RenderContext.Instance.UniformCallbacks;
        if (RenderContext.Instance.IsDepth)
        {
            callbacks.UploadShadow(in ctx);
            callbacks.UploadLightView(in ctx);
            return;
        }

        callbacks.UploadMainView(in ctx);
    }

    public void UploadLight()
    {
        LightUniform data = default;
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<LightUniform>(), &data, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniformUploadContext GetUploadContext() => new(_gfxBuffers);
}

public readonly ref struct UniformUploadContext(GfxBuffers gfxBuffers)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void UploadUniform<T>(T* data) where T : unmanaged, IUniform
    {
        gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<T>(), data, 0);
    }
}