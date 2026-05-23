using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Types;

public readonly struct GfxPassClear
{
    public readonly ColorRgba ClearColor;
    public readonly ClearBufferFlag ClearBuffer;

    public GfxPassClear(in Color4 clearColor, ClearBufferFlag clearBuffer)
    {
        ClearColor = (ColorRgba)clearColor;
        ClearBuffer = clearBuffer;
    }

    public GfxPassClear(ColorRgba clearColor, ClearBufferFlag clearBuffer)
    {
        ClearColor = clearColor;
        ClearBuffer = clearBuffer;
    }


    public static GfxPassClear MakeColorClear(Color4 clearColor) => new(in clearColor, ClearBufferFlag.Color);

    public static GfxPassClear MakeColorDepthClear(Color4 clearColor) =>
        new(in clearColor, ClearBufferFlag.ColorAndDepth);

    public static GfxPassClear MakeDepthClear() => new(ColorRgba.Black, ClearBufferFlag.Depth);

    public static GfxPassClear MakeNoClear() => new(ColorRgba.Black, ClearBufferFlag.None);
}