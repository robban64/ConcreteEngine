using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Graphics.Gfx;

public readonly record struct GfxPassState(
    bool? DepthTest = null,
    bool? DepthWrite = null,
    bool? Cull = null,
    bool? Blend = null,
    bool? Scissor = null,
    bool? FramebufferSrgb = null,
    bool? ColorMask = null
)
{
    public static GfxPassState MakeScene() => new(true, true, true, false, false, true);
    public static GfxPassState MakeScreen() => new(false, false, false, false, false, false);
    public static GfxPassState MakeShadow() => new(true, true, true, false, false, false, false);
    public static GfxPassState MakeLighting() => new(true, true, true, true, false, true);
    public static GfxPassState MakePostProcess(bool blend = false) => new(false, false, false, blend, false, true);
    public static GfxPassState MakeOff() => new(false, false, false, false, false, false, false);
}

public readonly record struct GfxPassClear(Color4? ClearColor, ClearBufferFlag ClearBuffer)
{
    public static GfxPassClear MakeColorClear(Color4 clearColor) => new(clearColor, ClearBufferFlag.Color);
    public static GfxPassClear MakeColorDepthClear(Color4 clearColor) => new(clearColor, ClearBufferFlag.ColorAndDepth);
    public static GfxPassClear MakeDepthClear() => new(null, ClearBufferFlag.Depth);
    public static GfxPassClear MakeNoClear() => new(null, ClearBufferFlag.None);
}