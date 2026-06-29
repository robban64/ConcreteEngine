using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Inspector;

namespace ConcreteEngine.Editor.Core;

internal sealed class SelectionManager
{
    public static SelectionManager Instance { get; private set; } = null!;

    public InspectSceneObject? SelectedSceneObject { get; private set; }
    public InspectAsset? SelectedAsset { get; private set; }

    public bool HasSceneObject => SelectedSceneObject is not null;
    public bool IsEmpty => SelectedAsset is null && SelectedSceneObject is null;
    public bool IsMixed => SelectedAsset is not null && SelectedSceneObject is not null;

    public SelectionManager(StateManager stateManager)
    {
        if (Instance != null) throw new InvalidOperationException();

        stateManager.ContextChanged += OnContextChanged;
        Instance = this;
    }

    private void OnContextChanged(EditorContext prev, EditorContext next)
    {
        if (prev.Selection != next.Selection)
            SelectionContextChange(next.Selection, next.Tool);

        if (prev.Tool != next.Tool)
            ToggleDrawBounds(next.Tool.ShowDebugBounds);
    }

    private void SelectionContextChange(SelectionContext selection, ToolContext tool)
    {
        if (SelectedSceneObject is not null && SelectedSceneObject.Id != selection.SelectedSceneId)
            DeselectSceneObject();

        if (SelectedSceneObject is null && selection.HasSceneObject)
            SelectSceneObject(selection.SelectedSceneId, tool.ShowDebugBounds);

        if (SelectedAsset is not null && SelectedAsset.Id != selection.SelectedAssetId)
            DeselectAsset();

        if (SelectedAsset is null && selection.HasAsset)
            SelectAsset(selection.SelectedAssetId);
    }


    private void ToggleDrawBounds(bool enabled)
    {
        if (SelectedSceneObject is not { } inspectSceneObj) return;
        foreach (var it in inspectSceneObj.SceneObject.GetInstances())
            it.ToggleDebugBounds(enabled);
    }


    private void SelectAsset(AssetId id)
    {
        if (id == SelectedAsset?.Id) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectAsset) - Invalid AssetId: {id}");
            return;
        }

        var asset = AssetManager.Assets.Get<AssetObject>(id);
        SelectedAsset = asset switch
        {
            Shader shader => new InspectShader(shader),
            Texture texture => new InspectTexture(texture),
            Model model => new InspectModel(model),
            Material material => new InspectMaterial(material),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void DeselectAsset()
    {
        var id = SelectedAsset?.Id ?? AssetId.Empty;
        if (!id.IsValid()) return;

        SelectedAsset = null;
        InspectorFieldProvider.Instance.TextureFields.Unbind();
        InspectorFieldProvider.Instance.MaterialFields.Unbind();
    }

    private void SelectSceneObject(SceneObjectId id, bool showDebugBounds)
    {
        if (id == SelectedSceneObject?.Id) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectSceneObject) - Invalid SceneObjectId: {id}");
            return;
        }

        if (SelectedSceneObject?.Id.IsValid() ?? false)
            DeselectSceneObject();

        var sceneObject = SceneManager.SceneStore.Get(id);
        foreach (var it in sceneObject.GetInstances())
            it.ToggleSelection(true);

        if (showDebugBounds)
            ToggleDrawBounds(true);

        SelectedSceneObject = new InspectSceneObject(sceneObject);
    }

    private void DeselectSceneObject()
    {
        if (SelectedSceneObject is not { } selected || !selected.Id.IsValid()) return;
        foreach (var it in selected.SceneObject.GetInstances())
        {
            it.ToggleSelection(false);
            it.ToggleDebugBounds(false);
        }

        SelectedSceneObject = null;

        InspectorFieldProvider.Instance.SceneFields.Unbind();
        //InspectorFieldProvider.Instance.ModelInstanceFields.Unbind();
        InspectorFieldProvider.Instance.ParticleInstanceFields.Unbind();
    }
}