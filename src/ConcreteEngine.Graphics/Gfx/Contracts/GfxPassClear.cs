
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly struct GfxPassClear
{
    public readonly Color ClearColor;
    public readonly ClearBufferFlag ClearBuffer;

    public GfxPassClear(in Color4 clearColor, ClearBufferFlag clearBuffer)
    {
        ClearColor = (Color)clearColor;
        ClearBuffer = clearBuffer;
    }

    public GfxPassClear(Color clearColor, ClearBufferFlag clearBuffer)
    {
        ClearColor = clearColor;
        ClearBuffer = clearBuffer;
    }


    public static GfxPassClear MakeColorClear(Color4 clearColor) => new(in clearColor, ClearBufferFlag.Color);

    public static GfxPassClear MakeColorDepthClear(Color4 clearColor) =>
        new(in clearColor, ClearBufferFlag.ColorAndDepth);

    public static GfxPassClear MakeDepthClear() => new(Color.Black, ClearBufferFlag.Depth);

    public static GfxPassClear MakeNoClear() => new(Color.Black, ClearBufferFlag.None);
}