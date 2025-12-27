using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Bridge;

public interface IEngineAssetController
{
    List<EditorAssetResource> LoadAssetList();
    EditorFileAssetModel[] GetAssetFiles(EditorId editorId);

    List<EditorAnimationResource> GetAnimationResources();
}