using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Assets;

public sealed class MaterialStore
{
    private const int DefaultCapacity = 128;

    private MaterialId NextId() => new(++Count);

    private AssetId[] _materials = new AssetId[DefaultCapacity];
    private readonly Stack<int> _free = [];

    private readonly AssetStore _assetStore;
    private readonly AssetCollection<Material> _materialCollection;
    
    public Material FallbackMaterial { get; private set; } = null!;

    public int Count { get; private set; }

    public int FreeSlots => _free.Count;
    public bool HasDirtyMaterials => _materialCollection.DirtyIds.Count > 0;

    internal MaterialStore(AssetStore assetStore)
    {
        _assetStore = assetStore;
        _materialCollection = _assetStore.GetAssetList<Material>();
    }

    public ReadOnlySpan<Material> GetMaterials() => _materialCollection.GetAssetSpan();

    public Material Get(MaterialId materialId) => _assetStore.Get<Material>(_materials[materialId.Index()]);
    public Material Get(string name) => _assetStore.GetByName<Material>(name);

    internal void InitializeStore()
    {
        FallbackMaterial.AssetShader = _assetStore.GetByName<Shader>("Model").Id;
        _assetStore.Process<Material>(Action);
        return;
        void Action(Material it) => RegisterMaterial(it);
    }

    internal void AddFallbackMaterial(Material material)
    {
        FallbackMaterial = material;
    }


    public Material CreateMaterial(string materialName, string newName)
    {
        ArgumentException.ThrowIfNullOrEmpty(materialName);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var originalMaterial = _assetStore.GetByName<Material>(materialName);

        var gid = Guid.NewGuid();
        var assetId = _assetStore.RegisterScannedAsset(gid, 0);

        var material = originalMaterial.MakeNewAsTemplate(assetId, gid, newName);
        _assetStore.AddAsset(material);
        return RegisterMaterial(material);
    }

    private Material RegisterMaterial(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(material.Name);

        var id = _free.Count > 0 ? new MaterialId(_free.Pop() + 1) : NextIdAndEnsureCapacity();
        InvalidOpThrower.ThrowIf(id == default);

        material.MaterialId = id;
        _materials[id.Index()] = material.Id;
        return material;
    }

    public bool TryRemove(MaterialId materialId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(materialId.Id, 0, nameof(materialId));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(materialId.Id, Count);

        var idx = materialId.Index();
        var assetId = _materials[idx];
        if (!assetId.IsValid()) return false;

        _materials[idx] = AssetId.Empty;
        _free.Push(idx);
        return true;
    }

    internal void ClearDirtyMaterials()
    {
        _materialCollection.DirtyIds.Clear();
    }

    internal int GetMaterialUploadData(Material material, Span<TextureBinding> slots, out RenderMaterialPayload data)
    {
        var shader = _assetStore.Get<Shader>(material.AssetShader).GfxId;

        material.FillParams(out var param);

        data = new RenderMaterialPayload(material.MaterialId, shader, in param,
            material.GetProperties(), material.Pipeline);

        var textureSlots = material.GetTextureSources();
        for (var i = 0; i < textureSlots.Length; i++)
        {
            var slot = textureSlots[i];
            var textureId = ResolveTextureId(slot);
            slots[i] = new TextureBinding(textureId, slot.Usage, slot.TextureKind);
        }

        return textureSlots.Length;
    }

    private TextureId ResolveTextureId(TextureSource source)
    {
        if (source.IsFallback)
        {
            var texId = GfxTextures.Fallback.AlbedoId;
            if (source.Usage == TextureUsage.Normal) texId = GfxTextures.Fallback.NormalId;
            return texId;
        }


        if (source.Usage == TextureUsage.Shadowmap) return default;

        if (!source.Texture.IsValid())
        {
            switch (source.Usage)
            {
                case TextureUsage.Albedo: return GfxTextures.Fallback.AlbedoId;
                case TextureUsage.Normal: return GfxTextures.Fallback.NormalId;
                case TextureUsage.Mask: return GfxTextures.Fallback.AlphaMaskId;
            }
        }

        return _assetStore.Get<Texture>(source.Texture).GfxId;
    }

    private MaterialId NextIdAndEnsureCapacity()
    {
        var len = _materials.Length;
        if (Count >= len)
        {
            var newCap = Arrays.CapacityGrowthLinear(len, len * 2, step: 32);

            if (newCap > RenderLimits.MaxMaterialCount)
                throw new InvalidOperationException("Material limit exceeded");

            Logger.LogString(LogScope.Assets, $"Material store resized {newCap}", LogLevel.Warn);
            Array.Resize(ref _materials, newCap);
        }

        return NextId();
    }
}