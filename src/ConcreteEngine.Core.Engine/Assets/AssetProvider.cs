using ConcreteEngine.Core.Engine.Assets.Data;

namespace ConcreteEngine.Core.Engine.Assets;

public abstract class AssetProvider
{
    public abstract AssetObject Get(AssetId id);
    public abstract T Get<T>(AssetId id) where T : AssetObject;
    public abstract T Get<T>(string name) where T : AssetObject;

    public abstract bool TryGet<T>(AssetId id, out T asset) where T : AssetObject;
    public abstract bool TryGet<T>(string name, out T asset) where T : AssetObject;
    public abstract bool TryGet<T>(Guid gid, out T asset) where T : AssetObject;
    public abstract bool TryGetByRootFile(AssetFileId id, out AssetObject asset);

    public abstract bool IsUnboundFile(AssetFileId fileId);
    public abstract bool IsRootFile(AssetFileId fileId);

    public abstract AssetFileSpec GetFile(AssetFileId id);
    public abstract AssetFileSpec GetAssetRootFile(AssetId id);
    public abstract ReadOnlySpan<AssetFileId> GetAssetFileBindings(AssetId id);
    public abstract ReadOnlySpan<AssetFileId> GetUnboundFileIds();

    public abstract ReadOnlySpan<AssetObject> GetAllAssets();
    public abstract ReadOnlySpan<AssetId> GetAssetIdsByKind(AssetKind kind);

    public abstract AssetEnumerator AssetEnumerator(AssetKind kind);
    public AssetFilesEnumerator AssetBindingsEnumerator(AssetId assetId) => new(assetId, this);
}