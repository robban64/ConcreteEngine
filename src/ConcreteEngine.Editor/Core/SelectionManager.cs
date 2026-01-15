using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Editor.Core;

internal sealed class SelectionManager
{
    public SceneObjectId SelectedSceneId { get; private set; } = SceneObjectId.Empty;
    public SceneObjectProxy? SceneProxy { get; private set; }

    public AssetId SelectedAssetId { get; private set; } = AssetId.Empty;
    public AssetProxy? AssetProxy { get; private set; }
    
    public Action<SceneObjectId>? Changed;

    public void SelectAsset(AssetId id, AssetKind kind)
    {
        if (id == SelectedAssetId) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectAsset) - Invalid AssetId: {id}");
            return;
        }

        SelectedAssetId = id;
        AssetProxy = EngineController.AssetController.GetAssetProxy(id, kind);
    }

    public void DeselectAsset()
    {
        SelectedAssetId = AssetId.Empty;
        AssetProxy = null;
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
            EngineController.SceneController.Deselect(SelectedSceneId);

        EngineController.SceneController.Select(id);
        SetSceneProxy(EngineController.SceneController.GetProxy(id));
    }

    public void DeSelectSceneObject()
    {
        var id = SelectedSceneId;
        if (!id.IsValid()) return;

        EngineController.SceneController.Deselect(id);
        SetSceneProxy(null);
    }
    
    private void SetSceneProxy(SceneObjectProxy? proxy)
    {
        var id = proxy?.Id ?? SceneObjectId.Empty;
        if (SelectedSceneId == id) return;

        SelectedSceneId = id;
        SceneProxy = proxy;
        Changed?.Invoke(id);
    }

}