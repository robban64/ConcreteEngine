using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Editor.Core;

internal sealed class SelectionManager
{
    public SceneObjectId SelectedId { get; private set; } = SceneObjectId.Empty;
    public SceneObjectProxy? Proxy { get; private set; }

    public Action<SceneObjectId>? Changed;

    private void Set(SceneObjectProxy? proxy)
    {
        var id = proxy?.Id ?? SceneObjectId.Empty;
        if (SelectedId == id) return;

        SelectedId = id;
        Proxy = proxy;
        Changed?.Invoke(id);
    }

    public void SelectSceneObject(SceneObjectId id)
    {
        if (id == SelectedId) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectSceneObject) - Invalid SceneObjectId: {id}");
            return;
        }

        if (SelectedId.IsValid())
            EngineController.SceneController.Deselect(SelectedId);

        EngineController.SceneController.Select(id);
        Set(EngineController.SceneController.GetProxy(id));
    }

    public void DeSelectSceneObject()
    {
        var id = SelectedId;
        if (!id.IsValid()) return;

        EngineController.SceneController.Deselect(id);
        Set(null);
    }
}