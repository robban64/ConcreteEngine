namespace ConcreteEngine.Core.Engine.Graphics;
/*
public sealed class MaterialManager
{
    private const int DefaultCapacity = 128;

    public static Material FallbackMaterial { get; private set; } = null!;

    private AssetId[] _byMaterialId = new AssetId[DefaultCapacity];

    private readonly AssetStore _assetStore;

    public MaterialId MaxId { get; private set; }

    internal MaterialManager(AssetStore assetStore)
    {
        _assetStore = assetStore;
    }

    internal void InitializeStore()
    {
        FallbackMaterial.ShaderId = _assetStore.GetByName<Shader>("Model").Id;
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
        ArgumentException.ThrowIfNullOrEmpty(material.Name, nameof(material.Name));
        ArgumentOutOfRangeException.ThrowIfZero(material.MaterialId.Value, nameof(material.MaterialId));

        var index = material.MaterialId.Index();
        if(index >= _byMaterialId.Length)
            EnsureCapacity(index + 1);

        if (material.MaterialId > MaxId) MaxId = material.MaterialId;

        _byMaterialId[index] = material.Id;
        return material;
    }

    public bool Remove(Id16<Material> materialId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(materialId.Value, nameof(materialId));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(materialId.Value, MaxId.Value);

        var index = materialId.Index();
        var assetId = _byMaterialId[index];
        if (!assetId.IsValid()) return false;

        _byMaterialId[index] = AssetId.Empty;
        return true;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        var len = _byMaterialId.Length;
        if (requiredCapacity < len) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(len, requiredCapacity);

        if (newCap > RenderLimits.MaxMaterialCount)
            throw new InvalidOperationException("Material limit exceeded");

        Logger.LogString(LogScope.Engine, $"Material store resized {newCap}", LogLevel.Warn);
        Array.Resize(ref _byMaterialId, newCap);
    }
}*/