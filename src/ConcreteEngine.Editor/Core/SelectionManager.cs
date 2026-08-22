using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Assets;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Scene;
using ConcreteEngine.Editor.Logging;

namespace ConcreteEngine.Editor.Core;

internal sealed class SelectionManager
{
    public static SelectionManager Instance { get; private set; } = null!;

    public SceneObject? SelectedSceneObject { get; private set; }
    public AssetObject? SelectedAsset { get; private set; }

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
        foreach (var it in inspectSceneObj.GetInstances())
            it.ToggleDebugBounds(enabled);
    }


    private void SelectAsset(AssetId id)
    {
        if (id == SelectedAsset?.Id) return;
        if (!id.IsValid())
        {
            LogService.PushMessage($"(SelectAsset) - Invalid AssetId: {id}");
            return;
        }

        SelectedAsset = AssetManager.Assets.Get<AssetObject>(id);
        AssetObjectInspector.Instance.AttachTarget(SelectedAsset);
    }

    private void DeselectAsset()
    {
        var id = SelectedAsset?.Id ?? AssetId.Empty;
        if (!id.IsValid()) return;

        SelectedAsset = null;
        AssetObjectInspector.Instance.DetachTarget();
    }

    private void SelectSceneObject(SceneObjectId id, bool showDebugBounds)
    {
        if (id == SelectedSceneObject?.Id) return;
        if (!id.IsValid())
        {
            LogService.PushMessage($"(SelectSceneObject) - Invalid SceneObjectId: {id}");
            return;
        }

        if (SelectedSceneObject?.Id.IsValid() ?? false)
            DeselectSceneObject();

        var sceneObject = SceneManager.SceneStore.Get(id);
        foreach (var it in sceneObject.GetInstances())
        {
            it.ToggleSelection(true);
            if (it is ParticleInstance particle) ParticleInspector.Instance.AttachTarget(particle.Emitter);
        }

        if (showDebugBounds)
            ToggleDrawBounds(true);

        SelectedSceneObject = sceneObject;
        SceneObjectInspector.Instance.AttachTarget(sceneObject);
    }

    private void DeselectSceneObject()
    {
        if (SelectedSceneObject is not { } selected || !selected.Id.IsValid()) return;
        foreach (var it in selected.GetInstances())
        {
            it.ToggleSelection(false);
            it.ToggleDebugBounds(false);
            if (it is ParticleInstance) ParticleInspector.Instance.DetachTarget();
        }
        
        SelectedSceneObject = null;
        SceneObjectInspector.Instance.DetachTarget();
    }
}