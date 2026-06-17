namespace ConcreteEngine.Graphics.Gfx;

[Flags]
public enum GfxDrawFlags : byte
{
    None = 0,
    DepthTest = 1 << 0,
    DepthWrite = 1 << 1,
    Cull = 1 << 2,
    Blend = 1 << 3,
    PolygonOffset = 1 << 4,
    Ac2 = 1 << 5,
    
    All = DepthTest | DepthWrite | Cull | Blend | PolygonOffset | Ac2
}

[Flags]
public enum GfxStateFlags : ushort
{
    None = 0,
    DepthTest = 1 << 0,
    DepthWrite = 1 << 1,
    Cull = 1 << 2,
    Blend = 1 << 3,
    PolygonOffset = 1 << 4,
    Ac2 = 1 << 5,
    Srgb = 1 << 6,
    ColorMask = 1 << 7,
    Scissor = 1 << 8,
}