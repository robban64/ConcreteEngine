using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateContext(
    EventManager eventManager,
    SelectionManager selection,
    PanelState panelState)
{
    public readonly PanelState Panels = panelState;
    public readonly SelectionManager Selection = selection;

    public bool IsMetricMode => Panels.RightPanelId == PanelId.MetricsRight;

    public void EmitTransition(TransitionMessage msg) => Panels.EmitTransition(msg);

    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventManager.Enqueue(evt);
}