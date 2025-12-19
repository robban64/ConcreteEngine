using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Assets.Materials;

public interface IMaterialStore
{
    int Count { get; }
    int FreeSlots { get; }

    Material Get(MaterialId materialId);
    Material Get(string name);
    Material CreateMaterial(string templateName, string name);
}

public sealed class MaterialStore : IMaterialStore
{
    private const int DefaultCapacity = 128;

    private static int _idx = 0;
    private static MaterialId NextId() => new(++_idx);

    private readonly AssetStore _assetStore;

    private Material?[] _materials = new Material[DefaultCapacity];
    private readonly Dictionary<string, MaterialId> _materialDict = new(DefaultCapacity);
    private readonly Stack<MaterialId> _free = [];

    public int Count => _idx;
    public int FreeSlots => _free.Count;
    public bool HasDirtyMaterials => MaterialState.DirtyState.DirtyIds.Count > 0;


    internal MaterialStore(AssetStore assetStore)
    {
        _assetStore = assetStore;
    }

    public ReadOnlySpan<Material?> MaterialSpan => _materials.AsSpan(0, _idx);

    public Material Get(MaterialId materialId) => _materials[materialId - 1]!;
    public Material Get(string name) => _materials[_materialDict[name] - 1]!;


    internal void InitializeStore()
    {
        _assetStore.Process<MaterialTemplate>(Action);
        return;
        void Action(MaterialTemplate it) => RegisterMaterial(it, it.Name);
    }


    public Material CreateMaterial(string templateName, string name)
    {
        ArgumentNullException.ThrowIfNull(templateName, nameof(templateName));

        var template = _assetStore.GetByName<MaterialTemplate>(templateName);
        return RegisterMaterial(template, name);
    }

    private Material RegisterMaterial(MaterialTemplate template, string name)
    {
        ArgumentNullException.ThrowIfNull(template, nameof(template));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        var id = _free.Count > 0 ? _free.Pop() : NextIdAndEnsureCapacity();
        InvalidOpThrower.ThrowIf(id == default);

        var material = new Material(id, template, name);
        _materials[id - 1] = material;
        _materialDict.Add(name, id);

        FillTextureInfo(material);
        return material;
    }

    public bool TryRemove(MaterialId materialId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(materialId.Id, 0, nameof(materialId));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(materialId.Id, _idx);

        var idx = materialId - 1;
        var material = _materials[idx];
        if (material is null) return false;
        _free.Push(materialId);
        _materialDict.Remove(material.Name);
        _materials[idx] = null;
        return true;
    }

    internal void GetMaterialUploadData(Material material, out RenderMaterialPayload data)
    {
        var shader = ResolveShader(material);
        var pipeline = material.State.Pipeline;

        material.FillSnapshot(out var snapshot);
        data = new RenderMaterialPayload(
            new RenderMaterialMeta(material.Id, shader, pipeline.PassState, pipeline.PassFunctions), in snapshot);
    }

    internal ShaderId ResolveShader(Material material) => _assetStore.GetByRef(material.ShaderRef).ResourceId;

    internal void ClearDirtyMaterials()
    {
        MaterialState.DirtyState.DirtyIds.Clear();
    }
    
    private void FillTextureInfo(Material material)
    {
        var textureSlots = material.TextureSlots.AssetSlots;
        var result = new TextureSlotInfo[textureSlots.Length];
        for (var i = 0; i < textureSlots.Length; i++)
        {
            var slot = textureSlots[i];
            var textureId = ResolveTextureId(slot);
            result[i] = new TextureSlotInfo(textureId, slot.SlotKind, slot.TextureKind);
        }

        material.TextureSlots.CacheSlots = result;
    }


    private TextureId ResolveTextureId(AssetTextureSlot assetSlot)
    {
        if (assetSlot.IsFallback)
        {
            var texId = GfxTextures.FallbackTextures.AlbedoId;
            if (assetSlot.SlotKind == TextureSlotKind.Normal) texId = GfxTextures.FallbackTextures.NormalId;
            return texId;
        }


        if (assetSlot.SlotKind == TextureSlotKind.Shadowmap) return default;

        if (!assetSlot.Asset.IsValid)
        {
            switch (assetSlot.SlotKind)
            {
                case TextureSlotKind.Albedo: return GfxTextures.FallbackTextures.AlbedoId;
                case TextureSlotKind.Normal: return GfxTextures.FallbackTextures.NormalId;
                case TextureSlotKind.Mask: return GfxTextures.FallbackTextures.AlphaMaskId;
            }
        }

        if (assetSlot.TextureKind == TextureKind.Texture2D)
            return _assetStore.GetByRef(new AssetRef<Texture2D>(assetSlot.Asset)).ResourceId;

        if (assetSlot.TextureKind == TextureKind.CubeMap)
            return _assetStore.GetByRef(new AssetRef<CubeMap>(assetSlot.Asset)).ResourceId;

        throw new InvalidOperationException(nameof(assetSlot));
    }

    private MaterialId NextIdAndEnsureCapacity()
    {
        var len = _materials.Length;
        if (_idx >= len)
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