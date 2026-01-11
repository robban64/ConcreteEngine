using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Editor.Core;

internal sealed class GlobalContext
{
    public readonly StateManager EditorState;

    public readonly EngineWorldController WorldController;
    public readonly EngineInteractionController InteractionController;
    public readonly EngineSceneController SceneController;
    public readonly EngineAssetController AssetController;

    public SceneObjectProxy? SelectedProxy;
    public SceneObjectId SelectedId => SelectedProxy?.Id ?? SceneObjectId.Empty;

    public GlobalContext(StateManager editorState)
    {
        EditorState = editorState;
        WorldController = EngineController.WorldController;
        InteractionController = EngineController.InteractionController;
        SceneController = EngineController.SceneController;
        AssetController = EngineController.AssetController;

        if (WorldController == null! || InteractionController == null! || SceneController == null! ||
            AssetController == null!)
        {
            throw new InvalidOperationException();
        }
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