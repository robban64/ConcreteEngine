using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Data;

internal enum PanelPlacement : byte
{
    Left, Right
}

internal enum TransitionAction : byte
{
    Push, Pop, Replace
}

internal struct TransitionMessage
{
    public PanelId Panel;
    public TransitionAction Action;
    public PanelPlacement Placement;
    public bool Clear;

    public static TransitionMessage PushLeft(PanelId panel)
        => new() { Panel = panel, Action = TransitionAction.Push, Placement = PanelPlacement.Left };

    public static TransitionMessage PushRight(PanelId panel)
        => new() { Panel = panel, Action = TransitionAction.Push, Placement = PanelPlacement.Right };

    public static TransitionMessage PopLeft(PanelId panel)
        => new() { Panel = panel, Action = TransitionAction.Pop, Placement = PanelPlacement.Left };

    public static TransitionMessage PopRight()
        => new() { Action = TransitionAction.Pop, Placement = PanelPlacement.Right };
}