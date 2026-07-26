using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed unsafe class UniformUploader
{
    private readonly GfxBuffers _gfxBuffers;
    private readonly MaterialBuffer _materialBuffer;
    private readonly SkinningBuffer _skinningBuffer;
    private readonly EffectBuffer _effectBuffer;

    public Id16<MaterialSlot> PrevMaterial { get; private set; } = new(-1);


    internal UniformUploader(GfxContext gfx, RenderUploadBuffers buffers)
    {
        _materialBuffer = buffers.Materials;
        _skinningBuffer = buffers.Skinning;
        _effectBuffer = buffers.Effects;

        _gfxBuffers = gfx.Buffers;

        UploadLight(); // set the buffer
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Prepare()
    {
        PrevMaterial = default;
    }

    internal void EnsureUboSizes(int drawCount, int materialCount)
    {
        if (!GfxResourceApi.GetMeta(DrawObjectUniform.UboId).HasCapacity(drawCount))
            _gfxBuffers.SetUniformBufferCount(DrawObjectUniform.UboId, drawCount);

        if (!GfxResourceApi.GetMeta(MaterialUniform.UboId).HasCapacity(drawCount))
            _gfxBuffers.SetUniformBufferCount(MaterialUniform.UboId, materialCount);
    }

    internal ReadOnlySpan<TextureBinding> BindResolveMaterial(Id16<MaterialSlot> materialId,
        out RenderMaterialMeta materialMeta)
    {
        if (PrevMaterial != materialId)
        {
            PrevMaterial = materialId;
            _gfxBuffers.BindUniformBufferRange<MaterialUniform>(materialId.Index(), 1);
            return _materialBuffer.GetMetaAndSlots(materialId, out materialMeta);
        }

        materialMeta = default;
        return ReadOnlySpan<TextureBinding>.Empty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindDrawObject(int submitIndex)
    {
        _gfxBuffers.BindUniformBufferRange<DrawObjectUniform>(submitIndex , 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindAnimation(int slot)
    {
        var range = _skinningBuffer.GetSlotRange(slot);
        _gfxBuffers.BindUniformBufferRange<DrawAnimationUniform>(range.Offset , range.Length );
    }

    internal void UploadMaterial(NativeView<MaterialUniform> data) => _gfxBuffers.UploadUniform(data, 0);
    internal void UploadDrawObjects(NativeView<DrawObjectUniform> data) => _gfxBuffers.UploadUniform(data, 0);

    internal void UploadAnimationData(NativeView<Matrix4x4> boneData)
    {
        if (!GfxResourceApi.GetMeta(DrawAnimationUniform.UboId).HasCapacity(boneData.Length))
            _gfxBuffers.SetUniformBufferCount(DrawAnimationUniform.UboId, boneData.Length);

        var view = new NativeView<DrawAnimationUniform>((DrawAnimationUniform*)boneData.Ptr, boneData.Length);
        _gfxBuffers.UploadUniform(view, 0);
    }

    // Globals //
    internal void UploadEditorEffectUniform(byte slot, bool isAnimated)
    {
        ref readonly var effect = ref _effectBuffer.Get(slot);
        var data = new EditorEffectsUniform(isAnimated, effect.Color);
        _gfxBuffers.UploadSingleUniform(&data, 0);
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
        _gfxBuffers.UploadSingleUniform(&data, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniformUploadContext GetUploadContext() => new(_gfxBuffers);
}

public readonly ref struct UniformUploadContext(GfxBuffers gfxBuffers)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void UploadUniform<T>(T* data) where T : unmanaged, IUniform
    {
        gfxBuffers.UploadSingleUniform(data, 0);
    }
}