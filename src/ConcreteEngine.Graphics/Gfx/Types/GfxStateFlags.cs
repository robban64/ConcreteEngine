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
    SampleAlphaCoverage = 1 << 5,
}

[Flags]
public enum GfxStateFlags : ushort
{
    None = 0,
    ColorMask = 1 << 0,
    DepthWrite = 1 << 1,
    Scissor = 1 << 2,
    DepthTest = 1 << 3,
    Cull = 1 << 4,
    Blend = 1 << 5,
    PolygonOffset = 1 << 6,
    SampleAlphaCoverage = 1 << 7,
    FramebufferSrgb = 1 << 8,
}