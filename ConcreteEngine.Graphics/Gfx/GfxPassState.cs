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
);

public readonly record struct GfxPassStateFlags(
    BlendMode Blend = BlendMode.Unset,
    DepthMode Depth = DepthMode.Unset,
    CullMode Cull = CullMode.Unset
);

public readonly record struct GfxPassClear(Color4? ClearColor, ClearBufferFlag ClearBuffer)
{
    public static GfxPassClear MakeColorClear(Color4 clearColor) => new(clearColor, ClearBufferFlag.Color);
    public static GfxPassClear MakeColorDepthClear(Color4 clearColor) => new(clearColor, ClearBufferFlag.ColorAndDepth);
    public static GfxPassClear MakeDepthClear() => new(null, ClearBufferFlag.Depth);
    public static GfxPassClear MakeNoClear() => new(null, ClearBufferFlag.None);
}