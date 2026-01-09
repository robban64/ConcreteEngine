using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Bridge;

public abstract class EngineAssetController
{
    public abstract ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind);
    public abstract AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);
}