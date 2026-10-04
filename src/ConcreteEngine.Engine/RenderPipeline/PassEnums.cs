namespace ConcreteEngine.Engine.RenderPipeline;

public enum RenderTargetKind : byte
{
    Scene = 0,
    Shadow = 1,
    Light = 2,
    Screen = 2
}

public enum PassOp : byte
{
    Draw = 0,
    Resolve = 1,
    Fsq = 2,
    Screen = 3,
    Continue = 4
}

internal enum NextPassAction : byte
{
    Run, Skip
}