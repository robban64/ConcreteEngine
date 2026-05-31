using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

//TODO REmove?
public sealed class MaterialStore
{
    private const int DefaultCapacity = 128;

    private MaterialId NextId() => new(++Count);

    private AssetId[] _byMaterialId = new AssetId[DefaultCapacity];
    private readonly Stack<int> _free = [];

    private readonly AssetStore _assetStore;

    public static Material FallbackMaterial { get; private set; } = null!;

    public int Count { get; private set; }
    public int ActiveCount => Count - _free.Count;
    public int FreeSlots => _free.Count;

    internal MaterialStore(AssetStore assetStore)
    {
        _assetStore = assetStore;
    }

    internal void InitializeStore()
    {
        FallbackMaterial.BoundShader = _assetStore.GetByName<Shader>("Model");
        foreach (var it in _assetStore.GetAssetEnumerator<Material>())
            RegisterMaterial(it);
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
        var assetId = _assetStore.RegisterPlainAsset(gid, AssetKind.Material, newName, AssetStorageKind.InMemory);
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
        _byMaterialId[id.Index()] = material.Id;
        return material;
    }

    public bool TryRemove(MaterialId materialId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(materialId.Id, 0, nameof(materialId));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(materialId.Id, Count);

        var idx = materialId.Index();
        var assetId = _byMaterialId[idx];
        if (!assetId.IsValid()) return false;

        _byMaterialId[idx] = AssetId.Empty;

        if (idx == Count - 1) Count--;
        else _free.Push(idx);

        if (ActiveCount == 0 && Count > 0)
        {
            _free.Clear();
            Count = 0;
        }

        return true;
    }

    private MaterialId NextIdAndEnsureCapacity()
    {
        var len = _byMaterialId.Length;
        if (Count >= len)
        {
            var newCap = CapacityUtils.CapacityGrowthToFit(len, len * 2);

            if (newCap > RenderLimits.MaxMaterialCount)
                throw new InvalidOperationException("Material limit exceeded");

            //Logger.LogString(LogScope.Assets, $"Material store resized {newCap}", LogLevel.Warn);
            Array.Resize(ref _byMaterialId, newCap);
        }

        return NextId();
    }
}