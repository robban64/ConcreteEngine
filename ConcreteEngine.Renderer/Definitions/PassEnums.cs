namespace ConcreteEngine.Renderer.Definitions;

[Flags]
public enum PassMask : ushort
{
    None = 0,
    DepthPre = 1 << 0,
    Main = 1 << 1,
    Effect = 1 << 2,
    /*ShadowDir = 1 << 2,
    ShadowSpot = 1 << 3,
    ShadowPoint = 1 << 4,
    Ui = 1 << 5,
    Post = 1 << 6,*/

    Default = DepthPre | Main //| ShadowDir | ShadowSpot | ShadowPoint
}

public enum PassOpKind : byte
{
    Draw = 0,
    Resolve = 1,
    Fsq = 2,
    Screen = 3
}

internal enum PreparePassActionKind : byte
{
    Run,
    Skip
}