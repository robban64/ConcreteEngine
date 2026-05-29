using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics.Gfx;

public readonly struct GfxPassState
{
    public readonly ColorRgba ClearColor;
    public readonly ClearBufferFlag ClearBuffer;
    public readonly GfxStateFlags StateFlags;

    public GfxPassState(in Color4 clearColor, ClearBufferFlag clearBuffer, GfxStateFlags stateFlags)
    {
        ClearColor = (ColorRgba)clearColor;
        ClearBuffer = clearBuffer;
        StateFlags = stateFlags;
    }

    public GfxPassState(ColorRgba clearColor, ClearBufferFlag clearBuffer, GfxStateFlags stateFlags)
    {
        ClearColor = clearColor;
        ClearBuffer = clearBuffer;
        StateFlags = stateFlags;
    }

    public static GfxPassState MakeColorClear(Color4 clearColor,GfxStateFlags flags) => new(in clearColor, ClearBufferFlag.Color,flags);
    public static GfxPassState MakeColorDepthClear(Color4 clearColor,GfxStateFlags flags) => new(in clearColor, ClearBufferFlag.ColorAndDepth,flags);
    public static GfxPassState MakeDepthClear(GfxStateFlags flags) => new(ColorRgba.Black, ClearBufferFlag.Depth,flags);
    public static GfxPassState MakeNoClear(GfxStateFlags flags) => new(ColorRgba.Black, ClearBufferFlag.None, flags);
}