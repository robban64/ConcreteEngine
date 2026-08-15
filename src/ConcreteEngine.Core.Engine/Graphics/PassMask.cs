namespace ConcreteEngine.Core.Engine.Graphics;

[Flags]
public enum PassMask : byte
{
    None = 0,
    Depth = 1 << 0,
    Main = 1 << 1,
    Effect = 1 << 2,
    /*ShadowDir = 1 << 2,
    ShadowSpot = 1 << 3,
    ShadowPoint = 1 << 4,
    Ui = 1 << 5,
    Post = 1 << 6,*/

    Default = Depth | Main
}