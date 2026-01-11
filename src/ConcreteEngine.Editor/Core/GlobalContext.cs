using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class GlobalContext
{
    public readonly StateManager EditorState;

    public EngineWorldController WorldController =>  EngineController.WorldController;
    public EngineInteractionController InteractionController =>  EngineController.InteractionController;
    public EngineSceneController SceneController =>  EngineController.SceneController;
    public EngineAssetController AssetController =>  EngineController.AssetController;

    private readonly ComponentHub _stateHub;

    public SceneObjectProxy? SelectedProxy;
    public SceneObjectId SelectedId => SelectedProxy?.Id ?? SceneObjectId.Empty;

    public GlobalContext(StateManager editorState, ComponentHub stateHub)
    {
        EditorState = editorState;
        _stateHub = stateHub;
    }
    
    public void TriggerStateEvent<TState, TEvent>(EventKey eventKey, TEvent evt) where TState : class 
        => _stateHub.TriggerEvent<TState, TEvent>(eventKey, evt);

    public void SelectSceneObject(SceneObjectId id)
    {
        if (id == SelectedId) return;
        if (!id.IsValid())
        {
            ConsoleGateway.LogPlain($"(SelectSceneObject) - Invalid SceneObjectId: {id}");
            return;
        }

        if (SelectedId.IsValid())
            SceneController.Deselect(SelectedId);

        SceneController.Select(id);
        SelectedProxy = SceneController.GetProxy(id);
    }

    public void DeSelectSceneObject()
    {
        var id = SelectedId;
        if (!id.IsValid()) return;

        SceneController.Deselect(id);
        SelectedProxy = null;
    }
}