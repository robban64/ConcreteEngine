using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
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
    private NativeArray<MaterialUniform> _uniforms;

    public MaterialSystem()
    {
        _metas = new MaterialMeta[MaterialBufferCapacity];
        _materialStore = AssetStore.GetTypeStore(AssetKind.Material);
        _textureSlots = NativeArray.Allocate<TextureBinding>(TextureSlotCapacity);
        _uniforms = NativeArray.Allocate<MaterialUniform>(MaterialBufferCapacity, false);
    }


    internal void Commit()
    {
        if (_materialStore.DirtyCount == 0) return;
        Submit();
        _materialStore.ClearDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<TextureBinding> GetMetaAndSlots(Id16<Material> materialId, out MaterialMeta meta)
    {
        ref var m = ref _metas[materialId.Index];
        meta = m;
        return _textureSlots.Slice(m.BindingRange).AsReadOnlySpan();
    }

    internal NativeView<MaterialUniform> GetUniforms()
    {
        Debug.Assert(_metas.Length == _uniforms.Length);
        if (Count == 0) return NativeView<MaterialUniform>.MakeNull();
        return _uniforms.Slice(0, Count);
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
            WriteUniform(material.State);
        }
    }

    private void SubmitMaterial(Material material, ShaderId shaderId)
    {
        var index = material.MaterialId.Index;
        EnsureCapacity(index);

        var sources = material.GetSourceSpan();

        ref var meta = ref _metas[index];

        var start = meta.BindingRange.Offset;
        var capacity = int.Max(sources.Length, 6);

        if (capacity > meta.BindingCapacity)
        {
            EnsureTextureSlotCapacity(capacity);
            start = _slotCount;
            _slotCount += capacity;
        }

        meta = new MaterialMeta(shaderId, new RangeU16(start, sources.Length), (byte)capacity,
            material.State.DrawState, material.State.DrawFunctions);

        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            var textureId = source.GetTextureOrFallback();
            _textureSlots[start + i] = new TextureBinding(textureId, source.Slot, source.Profile);
        }

        Count = int.Max(Count, index);
    }

    private void WriteUniform(MaterialState state)
    {
        ref var uniform = ref _uniforms[state.MaterialId.Index];
        uniform.Color = state.Color;
        uniform.SpecularColor = state.SpecularColor;
        uniform.UvTransform = new Vector4(state.UvOffset.X, state.UvOffset.Y, state.UvRepeat.X, state.UvRepeat.Y);

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
        _uniforms.ReAlloc(newCap, true);
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
        _uniforms.Dispose();
        _textureSlots.Dispose();
    }
}