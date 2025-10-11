#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly record struct GfxPassState(
    bool? DepthTest = null,
    bool? DepthWrite = null,
    bool? Cull = null,
    bool? Blend = null,
    bool? Scissor = null,
    bool? FramebufferSrgb = null,
    bool? ColorMask = null,
    bool? PolygonOffset = null
)
{
    public static GfxPassState MakeScene() => new(true, true, true, false, false, true, true, false);
    public static GfxPassState MakeShadow() => new(true, true, true, false, false, true, false, true);
    public static GfxPassState MakeLighting() => new(true, true, true, true, false, true, false, true);

    public static GfxPassState MakePostProcess(bool blend = false) =>
        new(false, false, false, blend, false, true, true, false);

    public static GfxPassState MakeScreen() => new(false, false, false, false, false, false, true, false);
    public static GfxPassState MakeOff() => new(false, false, false, false, false, false, true, false);
}

public readonly record struct GfxPassStateFunc(
    BlendMode Blend = BlendMode.Unset,
    CullMode Cull = CullMode.Unset,
    DepthMode Depth = DepthMode.Unset,
    PolygonOffsetLevel PolygonOffset = PolygonOffsetLevel.Unset)
{
    public static GfxPassStateFunc MakeDefault() => new(BlendMode.Alpha, CullMode.BackCcw, DepthMode.Lequal, PolygonOffsetLevel.None);
    public static GfxPassStateFunc MakeDepth() => new(BlendMode.Unset, CullMode.FrontCcw, DepthMode.Lequal, PolygonOffsetLevel.Medium);
}


public readonly record struct GfxPassClear(Color4 ClearColor, ClearBufferFlag ClearBuffer)
{
    public static GfxPassClear MakeColorClear(Color4 clearColor) => new(clearColor, ClearBufferFlag.Color);
    public static GfxPassClear MakeColorDepthClear(Color4 clearColor) => new(clearColor, ClearBufferFlag.ColorAndDepth);
    public static GfxPassClear MakeDepthClear() => new(Color4.Black, ClearBufferFlag.Depth);
    public static GfxPassClear MakeNoClear() => new(Color4.Black, ClearBufferFlag.None);
}
