using ConcreteEngine.Core.Engine.Assets.Data;

namespace ConcreteEngine.Core.Engine.Assets;

public abstract class AssetProvider
{
    public abstract AssetObject GetAsset(AssetId id);
    public abstract T GetAsset<T>(AssetId id) where T : AssetObject;

    public abstract bool TryGetAsset<T>(AssetId id, out T asset) where T : AssetObject;
    public abstract bool TryGetAssetByName<T>(string name, out T asset) where T : AssetObject;
    public abstract bool TryGetByGuid<T>(Guid gid, out T asset) where T : AssetObject;
    public abstract bool TryGetByRootFile(AssetFileId id, out AssetObject asset);

    public abstract bool IsRootFile(AssetFileId fileId);
    public abstract AssetFileSpec GetFileSpec(AssetFileId id);
    public abstract AssetFileSpec GetAssetRootFile(AssetId id);
    public abstract ReadOnlySpan<AssetFileId> GetAssetFileBindings(AssetId id);
    public abstract ReadOnlySpan<AssetFileId> GetUnboundFileIds();

    public abstract ReadOnlySpan<AssetObject> GetAllAssets();

    public abstract AssetEnumerator AssetEnumerator(AssetKind kind);
    public AssetFilesEnumerator AssetBindingsEnumerator(AssetId assetId) => new(assetId, this);
}