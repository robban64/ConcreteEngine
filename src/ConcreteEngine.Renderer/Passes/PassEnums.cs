namespace ConcreteEngine.Renderer.Passes;

public enum PassStateMode : byte
{
    Main,
    Depth,
    Post
}

public enum PassOp : byte
{
    Draw = 0,
    DrawEffect = 1,
    Resolve = 2,
    Fsq = 3,
    Screen = 4
}

internal enum NextPassAction : byte
{
    Run, Skip
}