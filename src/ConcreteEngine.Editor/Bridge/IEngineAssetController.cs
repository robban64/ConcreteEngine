using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineAssetController
{
    ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind);
    AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId);
}