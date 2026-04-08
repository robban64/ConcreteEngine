using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;


/*
    public readonly delegate*<in MeshMeta, uint, void>[] DrawTable =
    [
        &DrawInvalid,
        &DrawArrays,
        &DrawElements,
        &DrawInstanced,
    ];
*/
//private DrawMeshKind _prevKind;
// private delegate*<in MeshMeta, uint, uint> _cache;
internal unsafe class GlDraw
{
    private static readonly GL Gl = GlBackendDriver.Gl;

    private readonly delegate*<DrawPrimitive, DrawElementsType, uint, uint, void>* _table;
    private readonly DrawElementsType* _elements;

    private RenderFrameMeta _frameMeta;

    public GlDraw()
    {
        var fkSize = sizeof(nint) * 3;
        var elementSize = sizeof(int) * 4;

        var memory = NativeArray.Allocate<byte>(fkSize + elementSize);

        _table = (delegate*<DrawPrimitive, DrawElementsType, uint, uint, void>*)memory.Ptr;
        _table[0] = &DrawArrays;
        _table[1] = &DrawElements;
        _table[2] = &DrawInstanced;

        var elements = (int*)(memory.Ptr + fkSize);
        elements[0] = 0;
        elements[1] = (int)DrawElementsType.UnsignedByte;
        elements[2] = (int)DrawElementsType.UnsignedShort;
        elements[3] = (int)DrawElementsType.UnsignedInt;
        _elements = (DrawElementsType*)elements;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawMesh(scoped ref readonly MeshMeta meta, uint instanceCount = 0)
    {
        var index = (byte)meta.Kind - 1;
        var element = _elements[(byte)meta.ElementSize];
        _table[index](meta.Primitive, element, meta.DrawCount, instanceCount);
        _frameMeta.AddDrawCall(meta.DrawCount, instanceCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawInvalid(DrawPrimitive primitive, uint count, uint instances) =>
        throw new NotSupportedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawArrays(DrawPrimitive primitive, DrawElementsType _, uint count, uint instances)
    {
        Gl.DrawArrays(primitive.ToGlEnum(), 0, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawElements(DrawPrimitive primitive, DrawElementsType elementSize, uint count, uint instances)
    {
        Gl.DrawElements(primitive.ToGlEnum(), count, elementSize, (void*)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawInstanced(DrawPrimitive primitive, DrawElementsType _, uint count, uint instances)
    {
        Gl.DrawArraysInstanced(primitive.ToGlEnum(), 0, count, instances);
    }
}