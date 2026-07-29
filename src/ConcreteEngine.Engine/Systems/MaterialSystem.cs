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
    private const int DefaultTextureSlotCapacity = DefaultMaterialBufferCapacity * 4;

    public int Count { get; private set; }
    private int _slotCount;

    private readonly AssetTypeStore _materialStore = AssetStore.GetTypeStore(AssetKind.Material);

    private RangeU16[] _slotRanges = new RangeU16[DefaultMaterialBufferCapacity];
    private MaterialMeta[] _metas = new MaterialMeta[DefaultMaterialBufferCapacity];

    private NativeArray<TextureBinding> _textureSlots =
        NativeArray.Allocate<TextureBinding>(DefaultTextureSlotCapacity);

    private NativeArray<MaterialUniform> _buffer =
        NativeArray.Allocate<MaterialUniform>(DefaultMaterialBufferCapacity);

    
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
        return _textureSlots.Slice(_slotRanges[materialId.Index()]);
    }
    
    internal NativeView<MaterialUniform> GetBufferView()
    {
        Debug.Assert(_metas.Length == _buffer.Length);
        if (Count == 0) return NativeView<MaterialUniform>.MakeNull();
        return _buffer.Slice(0, Count);
    }

    private void Submit()
    {
        Shader lastShader = null!;
        var lastProfile = MaterialProfileId.None;
        foreach (var id in _materialStore.GetDirtySpan())
        {
            var material = AssetManager.Assets.GetUnsafe<Material>(id);
            var flag = material.Commit();
            if ((flag & AssetDirtyFlag.State) == 0 && (flag & AssetDirtyFlag.Structure) == 0) continue;

            if (lastShader == null! || material.ProfileId != lastProfile)
            {
                lastProfile = material.ProfileId;
                lastShader = material.BoundShader;
            }

            FillSamplers(material);
            SubmitUniform(material.State, lastShader);
        }
    }

    private void SubmitUniform(MaterialState state, Shader shader)
    {
        ref var uniform = ref Submit(
            state.MaterialId,
            shader.GfxId,
            state.DrawState,
            state.DrawFunctions,
            state.ReceiveShadows ? shader.DefaultBindings.ShadowMapBinding : (sbyte)-1
        );

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
    

    private void FillSamplers(Material material)
    {
        var textureSources = material.GetSourceSpan();
        Span<TextureBinding> slots = stackalloc TextureBinding[textureSources.Length];

        for (var i = 0; i < textureSources.Length; i++)
        {
            var source = textureSources[i];
            var textureId = source.FallbackTexture;
            if (source.OverrideTexture > 0) textureId = source.OverrideTexture;
            else if (source.AssetTexture.Id > 0)
                textureId = AssetManager.Assets.Get<Texture>(source.AssetTexture).GfxId;

            slots[i] = new TextureBinding(textureId, source.Usage, (byte)i);
        }

        SubmitBindings(material.MaterialId, slots);
    }
    
    private void SubmitBindings(Id16<Material> id, Span<TextureBinding> slots)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slots.Length, TextureSlots);
        EnsureTextureSlotCapacity(slots.Length);

        var slotIdx = _slotCount;
        for (var i = 0; i < slots.Length; i++, slotIdx++)
            _textureSlots[slotIdx] = slots[i];
        
        _slotRanges[id.Index()] = new RangeU16(_slotCount, slots.Length);
        _slotCount = slotIdx;
    }

    private ref MaterialUniform Submit(
        Id16<Material> id,
        ShaderId shaderId,
        GfxDrawState drawState,
        GfxDrawFunctions drawFunctions,
        sbyte shadowMapBinding)
    {
         EnsureCapacity(id.Value);

        _metas[id.Index()] = new MaterialMeta(shaderId, drawState, drawFunctions, shadowMapBinding);

        Count = int.Max(Count, id.Index());
        return ref _buffer[id.Index()];
    }


    
    private void EnsureCapacity(int amount)
    {
        if (_metas.Length > amount) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_metas.Length, amount);

        if (newCap > MaxMaterialBufferCapacity)
            Throwers.BufferOverflow(nameof(MaterialSystem), newCap, MaxMaterialBufferCapacity);

        Console.WriteLine($"{nameof(MaterialSystem)} TextureSlots resize");
        Array.Resize(ref _metas, newCap);
        Array.Resize(ref _slotRanges, newCap);
        _buffer.Resize(newCap, true);
    }

    private void EnsureTextureSlotCapacity(int amount)
    {
        if (_textureSlots.Length > _slotCount + amount) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_textureSlots.Length, amount);
        if (newCap > MaxTextureSlotBuffCapacity)
            Throwers.BufferOverflow(nameof(MaterialSystem), newCap, MaxMaterialBufferCapacity);

        Console.WriteLine($"{nameof(MaterialSystem)} TextureSlots resize");
        _textureSlots.Resize(newCap, true);
    }

    public void Dispose()
    {
        _buffer.Dispose();
        _textureSlots.Dispose();
    }
}