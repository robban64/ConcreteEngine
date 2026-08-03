using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
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

    private readonly AssetTypeStore _materialStore = AssetStore.GetTypeStore(AssetKind.Material);

    private MaterialMeta[] _metas = new MaterialMeta[MaterialBufferCapacity];

    private NativeArray<TextureBinding> _textureSlots =
        NativeArray.Allocate<TextureBinding>(TextureSlotCapacity);

    private NativeArray<MaterialUniform> _buffer =
        NativeArray.Allocate<MaterialUniform>(MaterialBufferCapacity, false);


    internal void Commit()
    {
        if (_materialStore.DirtyCount == 0) return;
        Submit();
        _materialStore.ClearDirty();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeView<TextureBinding> GetMetaAndSlots(Id16<Material> materialId, out MaterialMeta meta)
    {
        meta = _metas[materialId.Index()];
        return _textureSlots.Slice(meta.BindingRange);
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
            var textureId = source.FallbackTexture;
            var profile = SamplerProfile.LinearWrap;
            if (source.OverrideTexture > 0) textureId = source.OverrideTexture;
            else if (source.AssetTexture.Id > 0)
            {
                var texture = AssetManager.Assets.Get<Texture>(source.AssetTexture);
                textureId = texture.GfxId;
                profile = texture.Profile;
            }

            _textureSlots[range.Offset + i] = new TextureBinding(textureId, source.Usage, (byte)i, profile);
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