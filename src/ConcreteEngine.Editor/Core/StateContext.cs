using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateContext(
    ComponentHub stateHub,
    SelectionManager selection,
    EditorState editorState)
{
    public readonly EditorState State = editorState;
    public readonly SelectionManager Selection = selection;

    public bool IsActiveRight<T>() => State.Right != null && typeof(T) == State.Right.ComponentType;

    public void EmitTransition(TransitionMessage msg)  => State.EmitTransition(msg);

    public void TriggerEvent<TEvent>(TEvent evt) where TEvent : ComponentEvent => stateHub.TriggerEvent(evt);
}