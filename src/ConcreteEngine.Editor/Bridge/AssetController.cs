using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Bridge;

public abstract class AssetController
{
    public abstract ReadOnlySpan<IAsset> GetAssetSpan(AssetKind kind);
    public abstract AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);

    public abstract AssetObjectProxy GetAssetProxy(AssetId assetId);
}