namespace ConcreteEngine.Renderer.Definitions;

public enum PassOpKind : byte
{
    Draw = 0,
    DrawEffect = 1,
    Resolve = 2,
    Fsq = 3,
    Screen = 4
}

internal enum PreparePassActionKind : byte
{
    Run,
    Skip
}