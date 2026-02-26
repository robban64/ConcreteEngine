using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Bridge;

public abstract class AssetController
{
    public abstract AssetObject GetAsset(AssetId id);
    public abstract T GetAsset<T>(AssetId id) where T : AssetObject;
    public abstract bool TryGetAsset(AssetId id, out AssetObject asset);
    public abstract bool TryGetAsset<T>(AssetId id, out T asset) where T : AssetObject;

    public abstract AssetFileSpec[] GetAssetFileSpecs(AssetId assetId);

    public abstract ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind);
    public abstract ReadOnlySpan<T> GetAssetSpan<T>() where T : AssetObject;

}