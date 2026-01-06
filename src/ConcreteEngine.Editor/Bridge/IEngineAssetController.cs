using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineAssetController
{
    EditorAssetResource[] FetchAssets(AssetKind kind);
    EditorFileAssetModel[] FetchAssetFileSpecs(EditorId editorId);

    List<EditorAnimationResource> GetAnimationResources();
}