namespace ConcreteEngine.Core.Rendering.Passes;

[Flags]
public enum PassMask : ushort
{
    None = 0,
    DepthPre = 1 << 0,
    Main = 1 << 1,
    /*ShadowDir = 1 << 2,
    ShadowSpot = 1 << 3,
    ShadowPoint = 1 << 4,
    Ui = 1 << 5,
    Post = 1 << 6,*/

    Default = DepthPre | Main //| ShadowDir | ShadowSpot | ShadowPoint
}