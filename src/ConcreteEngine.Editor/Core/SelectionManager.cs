using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Controller.Proxy;

namespace ConcreteEngine.Editor.Core;

internal sealed class SelectionManager(AssetController assetController, SceneController sceneController)
{
    public SceneObjectProxy? SceneProxy { get; private set; }
    public SceneObjectId SelectedSceneId => SceneProxy?.Id ?? SceneObjectId.Empty;

    public EditorAsset? SelectedAsset { get; private set; }
    public AssetId SelectedAssetId { get; private set; }


    public bool HasSelection() => SelectedSceneId != SceneObjectId.Empty || SelectedAssetId != AssetId.Empty;


    public void SelectAsset(AssetId id)
    {
        if (id == SelectedAssetId) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectAsset) - Invalid AssetId: {id}");
            return;
        }

        var asset = assetController.GetAsset(id);
        var fileSpecs = assetController.GetAssetFileSpecs(id);
        SelectedAssetId = id;
        SelectedAsset = asset switch
        {
            Shader shader => new EditorShader(shader, fileSpecs),
            Texture texture => new EditorTexture(texture, fileSpecs),
            Model model => new EditorModel(model, fileSpecs),
            Material material => new EditorMaterial(material, fileSpecs),
            _ => throw new ArgumentOutOfRangeException()
        };
        
    }

    public void DeselectAsset()
    {
        SelectedAssetId = AssetId.Empty;
        SelectedAsset = null;
    }

    public void SelectSceneObject(SceneObjectId id)
    {
        if (id == SelectedSceneId) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectSceneObject) - Invalid SceneObjectId: {id}");
            return;
        }

        if (SelectedSceneId.IsValid())
            sceneController.Deselect(SelectedSceneId);

        sceneController.Select(id);
        SetSceneProxy(sceneController.GetProxy(id));
    }

    public void DeSelectSceneObject()
    {
        var id = SelectedSceneId;
        if (!id.IsValid()) return;

        sceneController.Deselect(id);
        SetSceneProxy(null);
    }

    private void SetSceneProxy(SceneObjectProxy? proxy)
    {
        var id = proxy?.Id ?? SceneObjectId.Empty;
        if (SelectedSceneId == id) return;

        SceneProxy = proxy;
    }
}