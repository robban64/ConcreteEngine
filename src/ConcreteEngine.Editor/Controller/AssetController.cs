using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller.Proxy;

namespace ConcreteEngine.Editor.Controller;

public abstract class AssetController
{
    public abstract ReadOnlySpan<IAsset> GetAssetSpan(AssetKind kind);
    public abstract AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);
    public abstract AssetObjectProxy GetAssetProxy(AssetId assetId);
}