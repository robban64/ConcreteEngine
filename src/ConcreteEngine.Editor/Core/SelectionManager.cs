using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;

namespace ConcreteEngine.Editor.Core;

internal abstract class InspectSelection<T> 
{
    public T? Selected { get; private set; }
    public Action<T> OnSelect;
    public Action<T> OnDeslect;
    public bool HasSelection => Selected is not null;
}

internal sealed class SelectionManager
{
    public InspectSceneObject? SelectedSceneObject { get; private set; }
    public SceneObjectId SelectedSceneId { get; private set; }

    public InspectAsset? SelectedAsset { get; private set; }
    public AssetId SelectedAssetId { get; private set; }


    private static SceneController SceneController => EngineObjectStore.SceneController;
    private static AssetProvider AssetProvider => EngineObjectStore.AssetProvider;

    public void ToggleDrawBounds(bool enabled)
    {
        if (SelectedSceneObject is not { } inspectSceneObj || inspectSceneObj.ShowDebugBounds == enabled) return;
        SceneController.ToggleDrawBounds(SelectedSceneId, enabled);
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

        var asset = AssetProvider.GetAsset(id);
        SelectedAssetId = id;
        SelectedAsset = asset switch
        {
            Shader shader => new InspectShader(shader),
            Texture texture => new InspectTexture(texture),
            Model model => new InspectModel(model),
            Material material => new InspectMaterial(material),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void DeselectAsset()
    {
        SelectedAssetId = AssetId.Empty;
        SelectedAsset = null;
        InspectorFieldProvider.Instance.TextureFields.Unbind();
        InspectorFieldProvider.Instance.MaterialFields.Unbind();

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
            SceneController.Deselect(SelectedSceneId);

        var inspector = SceneController.Select(id);
        SelectedSceneId = inspector.Id;
        SelectedSceneObject = inspector;
    }

    public void DeSelectSceneObject()
    {
        var id = SelectedSceneId;
        if (!id.IsValid()) return;

        SceneController.Deselect(id);
        SelectedSceneId = SceneObjectId.Empty;
        SelectedSceneObject = null;

        InspectorFieldProvider.Instance.SceneFields.Unbind();
        InspectorFieldProvider.Instance.ModelInstanceFields.Unbind();
        InspectorFieldProvider.Instance.ParticleInstanceFields.Unbind();

    }
}