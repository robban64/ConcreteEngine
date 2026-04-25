using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Lib;

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
    private static SceneController SceneController => EngineObjectStore.SceneController;
    private static AssetProvider AssetProvider => EngineObjectStore.AssetProvider;

    public InspectSceneObject? SelectedSceneObject { get; private set; }
    public InspectAsset? SelectedAsset { get; private set; }

    public bool IsEmpty => SelectedAsset is null && SelectedSceneObject is null;
    public bool IsMixed => SelectedAsset is not null && SelectedSceneObject is not null;


    public void SelectionContextChange(SelectionContext ctx)
    {
        if (SelectedSceneObject is not null && SelectedSceneObject.Id != ctx.SelectedSceneId)
            DeSelectSceneObject();

        if (SelectedSceneObject is null && ctx.HasSceneObject)
            SelectSceneObject(ctx.SelectedSceneId);
        
        if (SelectedAsset is not null && SelectedAsset.Id != ctx.SelectedAssetId)
            DeselectAsset();

        if (SelectedAsset is null && ctx.HasAsset)
            SelectAsset(ctx.SelectedAssetId);
    }


    public void ToggleDrawBounds(bool enabled)
    {
        if (SelectedSceneObject is not { } inspectSceneObj || inspectSceneObj.ShowDebugBounds == enabled) return;
        SceneController.ToggleDrawBounds(inspectSceneObj.Id, enabled);
        inspectSceneObj.ShowDebugBounds = enabled;
    }


    public void SelectAsset(AssetId id)
    {
        if (id == SelectedAsset?.Id) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectAsset) - Invalid AssetId: {id}");
            return;
        }

        var asset = AssetProvider.Get(id);
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
        var id = SelectedAsset?.Id ?? AssetId.Empty;
        if (!id.IsValid()) return;

        SelectedAsset = null;
        InspectorFieldProvider.Instance.TextureFields.Unbind();
        InspectorFieldProvider.Instance.MaterialFields.Unbind();
    }

    public void SelectSceneObject(SceneObjectId id)
    {
        if (id == SelectedSceneObject?.Id) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectSceneObject) - Invalid SceneObjectId: {id}");
            return;
        }

        if (SelectedSceneObject?.Id.IsValid() ?? false)
            SceneController.Deselect(SelectedSceneObject.Id);

        var inspector = SceneController.Select(id);
        SelectedSceneObject = inspector;
    }

    public void DeSelectSceneObject()
    {
        var id = SelectedSceneObject?.Id ?? SceneObjectId.Empty;
        if (!id.IsValid()) return;

        SceneController.Deselect(id);
        SelectedSceneObject = null;

        InspectorFieldProvider.Instance.SceneFields.Unbind();
        InspectorFieldProvider.Instance.ModelInstanceFields.Unbind();
        InspectorFieldProvider.Instance.ParticleInstanceFields.Unbind();
    }
}