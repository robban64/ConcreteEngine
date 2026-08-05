using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Engine.Render.RenderLimits;

namespace ConcreteEngine.Engine.Systems;

internal sealed class MaterialSystem : IDisposable
{
    public const int MaterialBufferCapacity = 512;
    private const int TextureSlotCapacity = MaterialBufferCapacity * 4;

    public int Count { get; private set; }
    private int _slotCount;

    private readonly AssetTypeStore _materialStore;

    private MaterialMeta[] _metas;

    private NativeArray<TextureBinding> _textureSlots;

    private NativeArray<MaterialUniform> _buffer;

    public MaterialSystem()
    {
        _metas = new MaterialMeta[MaterialBufferCapacity];
        _materialStore = AssetStore.GetTypeStore(AssetKind.Material);
        _textureSlots = NativeArray.Allocate<TextureBinding>(TextureSlotCapacity);
        _buffer = NativeArray.Allocate<MaterialUniform>(MaterialBufferCapacity, false);
    }


    internal void Commit()
    {
        if (_materialStore.DirtyCount == 0) return;
        Submit();
        _materialStore.ClearDirty();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeView<TextureBinding> GetMetaAndSlots(Id16<Material> materialId, out MaterialMeta meta)
    {
        ref var m = ref _metas[materialId.Index()];
        meta = m;
        return _textureSlots.Slice(m.BindingRange);
    }

    internal NativeView<MaterialUniform> GetUniforms()
    {
        Debug.Assert(_metas.Length == _buffer.Length);
        if (Count == 0) return NativeView<MaterialUniform>.MakeNull();
        return _buffer.Slice(0, Count);
    }

    private void Submit()
    {
        ShaderId lastShader = default;
        var lastProfile = MaterialProfileId.None;
        foreach (var id in _materialStore.GetDirtySpan())
        {
            var material = (Material)AssetManager.Assets.GetUnsafe(id);
            var flag = material.Commit();
            if ((flag & AssetDirtyFlag.State) == 0 && (flag & AssetDirtyFlag.Structure) == 0) continue;

            if (lastShader == default || material.ProfileId != lastProfile)
            {
                lastProfile = material.ProfileId;
                lastShader = material.BoundShader.GfxId;
            }

            SubmitMaterial(material, lastShader);
            SubmitUniform(material.State);
        }
    }

    private void SubmitMaterial(Material material, ShaderId shaderId)
    {
        var id = material.MaterialId;
        var textureSources = material.GetSourceSpan();

        EnsureCapacity(id.Value);
        EnsureTextureSlotCapacity(textureSources.Length);

        var range = new RangeU16(_slotCount, textureSources.Length);
        _metas[id.Index()] = new MaterialMeta(shaderId, range, material.State.DrawState, material.State.DrawFunctions);

        for (var i = 0; i < textureSources.Length; i++)
        {
            var source = textureSources[i];
            var textureId = source.GetTextureOrFallback();
            _textureSlots[range.Offset + i] = new TextureBinding(textureId, (SamplerSlot)i, source.Profile);
        }

        _slotCount += range.Length;
        Count = int.Max(Count, id.Index());
    }

    private void SubmitUniform(MaterialState state)
    {
        ref var uniform = ref _buffer[state.MaterialId.Index()];
        uniform.Color = state.Color;
        uniform.SpecularColor = state.SpecularColor;
        uniform.UvTransform = state.UvTransform;

        uniform.Shininess = state.Shininess;
        uniform.Roughness = state.Roughness;
        uniform.Metallic = state.Metallic;
        uniform.AlphaCutoff = state.IsTransparent ? (state.HasAlphaMask ? 0.5f : 0.1f) : 0f;

        uniform.AlphaMaskToggle = state.HasAlphaMask ? 1 : 0;
        uniform.ShadowToggle = state.ReceiveShadows ? 1 : 0;
    }


    private void EnsureCapacity(int amount)
    {
        if (_metas.Length > amount) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_metas.Length, amount);

        if (newCap > MaxMaterialBufferCapacity)
            Throwers.BufferOverflow(nameof(MaterialSystem), newCap, MaxMaterialBufferCapacity);

        Console.WriteLine($"{nameof(MaterialSystem)} TextureSlots resize");
        Array.Resize(ref _metas, newCap);
        _buffer.ReAlloc(newCap, true);
    }

    private void EnsureTextureSlotCapacity(int amount)
    {
        if (_textureSlots.Length > _slotCount + amount) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_textureSlots.Length, amount);
        if (newCap > MaxTextureSlotBuffCapacity)
            Throwers.BufferOverflow(nameof(MaterialSystem), newCap, MaxMaterialBufferCapacity);

        Console.WriteLine($"{nameof(MaterialSystem)} TextureSlots resize");
        _textureSlots.ReAlloc(newCap, true);
    }

    public void Dispose()
    {
        _buffer.Dispose();
        _textureSlots.Dispose();
    }
}