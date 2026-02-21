using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateContext(
    EventManager eventManager,
    SelectionManager selection,
    PanelState panelState)
{
    public readonly PanelState State = panelState;
    public readonly SelectionManager Selection = selection;

    public bool IsMetricMode() => State.RightPanelId == PanelId.MetricsRight;

    public void EmitTransition(TransitionMessage msg) => State.EmitTransition(msg);

    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventManager.Enqueue(evt);
}