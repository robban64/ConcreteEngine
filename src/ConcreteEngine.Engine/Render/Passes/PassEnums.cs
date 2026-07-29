namespace ConcreteEngine.Engine.Render.Passes;

public enum PassStateMode : byte
{
    Main,
    Depth,
    Post
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