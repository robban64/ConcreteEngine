using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateContext(
    StateManager stateManager,
    ComponentHub stateHub,
    SelectionManager selection)
{
    public readonly StateManager StateManager = stateManager;
    public readonly SelectionManager Selection = selection;

    public void TriggerStateEvent<TEvent>(TEvent evt) where TEvent : ComponentEvent => stateHub.TriggerEvent(evt);
}