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
    DepthTest = 1 << 0,
    DepthWrite = 1 << 1,
    Cull = 1 << 2,
    Blend = 1 << 3,
    PolygonOffset = 1 << 4,
    SampleAlphaCoverage = 1 << 5,
    Scissor = 1 << 6,
    FramebufferSrgb = 1 << 7,
    ColorMask = 1 << 8,
}