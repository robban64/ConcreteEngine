using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Bridge;

public abstract class AssetController
{
    public abstract AssetObject GetAsset(AssetId id);
    public abstract bool TryGetAssetByName(string name, out AssetObject asset);

    public abstract T GetAsset<T>(AssetId id) where T : AssetObject;
    public abstract bool TryGetAsset(AssetId id, out AssetObject asset);
    public abstract bool TryGetAsset<T>(AssetId id, out T asset) where T : AssetObject;
    public abstract bool TryGetByGuid<T>(Guid gid, out T asset) where T : AssetObject;
    public abstract bool TryGetByRootFile(AssetFileId id, out AssetObject asset);

    public abstract AssetFileSpec[] GetAssetFileSpecs(AssetId assetId);
    
    public abstract bool IsRootFile(AssetFileId fileId);
    public abstract ReadOnlySpan<AssetFileId> GetUnboundFileIds();
    public abstract AssetFileSpec GetFileSpec(AssetFileId id);
    public abstract AssetFileSpec GetAssetRootFile(AssetId id);
    public abstract ReadOnlySpan<AssetFileSpec> GetFileSpecs(AssetKind kind);

    public abstract ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind);
    public abstract ReadOnlySpan<T> GetAssetSpan<T>() where T : AssetObject;
}