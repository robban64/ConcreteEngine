using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Editor.Core;

internal sealed class SelectionManager(AssetController assetController, SceneController sceneController)
{
    public InspectSceneObject? SelectedSceneObject { get; private set; }
    public SceneObjectId SelectedSceneId { get; private set; }

    public InspectAsset? SelectedAsset { get; private set; }
    public AssetId SelectedAssetId { get; private set; }

    public void ToggleDrawBounds(bool enabled)
    {
        if(SelectedSceneObject is not {} inspectSceneObj || inspectSceneObj.ShowDebugBounds == enabled) return;
        sceneController.ToggleDrawBounds(SelectedSceneId, enabled);
        inspectSceneObj.ShowDebugBounds = enabled;
    }

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
            Shader shader => new InspectShader(shader, fileSpecs),
            Texture texture => new InspectTexture(texture, fileSpecs),
            Model model => new InspectModel(model, fileSpecs),
            Material material => new InspectMaterial(material, fileSpecs),
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

        var inspector = sceneController.Select(id);
        SelectedSceneId = inspector.Id;
        SelectedSceneObject = inspector;
    }

    public void DeSelectSceneObject()
    {
        var id = SelectedSceneId;
        if (!id.IsValid()) return;

        sceneController.Deselect(id);
        SelectedSceneId = SceneObjectId.Empty;
        SelectedSceneObject = null;
    }

}