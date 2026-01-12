using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class GlobalContext(
    StateManager editorState,
    ComponentHub stateHub,
    SelectionManager selection)
{
    public readonly StateManager EditorState = editorState;
    public readonly SelectionManager Selection = selection;

    public void TriggerStateEvent<TState, TEvent>(EventKey eventKey, TEvent evt) where TState : class =>
        stateHub.TriggerEvent<TState, TEvent>(eventKey, evt);
}