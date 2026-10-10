namespace ConcreteEngine.Core.Engine.Graphics;

[Flags]
public enum PassMask : byte
{
    None = 0,
    Depth = 1 << 0,
    Scene = 1 << 1,
    Effect = 1 << 2,

    Default = Depth | Scene
}