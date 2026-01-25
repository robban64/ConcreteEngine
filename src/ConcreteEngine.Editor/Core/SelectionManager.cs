using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Proxy;

namespace ConcreteEngine.Editor.Core;

public interface IEngineProxy
{
}

internal abstract class SelectionEntry
{
    public IEngineProxy? Proxy { get; private set; }
    public Action<IEngineProxy>? Changed;
}

internal sealed class SelectionManager(AssetController assetController, SceneController sceneController)
{
    public SceneObjectProxy? SceneProxy { get; private set; }
    public SceneObjectId SelectedSceneId => SceneProxy?.Id ?? SceneObjectId.Empty;

    public AssetObjectProxy? AssetProxy { get; private set; }
    public AssetId SelectedAssetId => AssetProxy?.Asset.Id ?? AssetId.Empty;

    public bool HasSelection() => SelectedSceneId != SceneObjectId.Empty || SelectedAssetId != AssetId.Empty;


    public void SelectAsset(AssetId id)
    {
        if (id == SelectedAssetId) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectAsset) - Invalid AssetId: {id}");
            return;
        }

        AssetProxy = assetController.GetAssetProxy(id);
    }

    public void DeselectAsset()
    {
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