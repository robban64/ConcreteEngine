namespace ConcreteEngine.Editor.Data;

internal enum PanelPlacement : byte
{
    Left, Right
}

internal enum TransitionAction : byte
{
    Push, Pop, Replace
}

internal ref struct TransitionMessage
{
    public Type ComponentType;
    public TransitionAction Action;
    public PanelPlacement Placement;
    public bool Clear;

    public static TransitionMessage PushLeft(Type t)
        => new() { ComponentType = t, Action = TransitionAction.Push, Placement = PanelPlacement.Left };

    public static TransitionMessage PushRight(Type t)
        => new() { ComponentType = t, Action = TransitionAction.Push, Placement = PanelPlacement.Right };

    public static TransitionMessage PopLeft(Type t)
        => new() { ComponentType = t, Action = TransitionAction.Pop, Placement = PanelPlacement.Left };

    public static TransitionMessage PopRight(Type t)
        => new() { ComponentType = t, Action = TransitionAction.Pop, Placement = PanelPlacement.Right };
}