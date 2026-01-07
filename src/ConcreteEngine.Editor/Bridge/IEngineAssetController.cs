using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineAssetController
{
    ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind);
    AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);
}