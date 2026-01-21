using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Editor.Core;

public interface IEngineProxy
{
}

internal abstract class SelectionEntry
{
    public IEngineProxy? Proxy { get; private set; }
    public Action<IEngineProxy>? Changed;
}

internal sealed class SelectionManager
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

        AssetProxy = EngineController.AssetController.GetAssetProxy(id);
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

        SceneProxy = proxy;
    }
}