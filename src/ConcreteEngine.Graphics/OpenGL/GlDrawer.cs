using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal static unsafe class GlDraw
{
    private static readonly GL Gl = GlBackendDriver.Gl;

    public static void DrawInvalid(DrawPrimitive primitive, DrawElementSize size, uint count, uint instances) =>
        throw new NotSupportedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawArrays(DrawPrimitive primitive, DrawElementSize size, uint count, uint instances)
    {
        Gl.DrawArrays(primitive.ToGlEnum(), 0, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawElements(DrawPrimitive primitive, DrawElementSize elementSize, uint count, uint instances)
    {
        Gl.DrawElements(primitive.ToGlEnum(), count, elementSize.ToGlEnum(), (void*)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawInstanced(DrawPrimitive primitive, DrawElementSize size, uint count, uint instances)
    {
        Gl.DrawArraysInstanced(primitive.ToGlEnum(), 0, count, instances);
    }
}