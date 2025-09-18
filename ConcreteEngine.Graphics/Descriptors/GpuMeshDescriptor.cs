#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;


public readonly ref struct GpuMeshDescriptor
{
    public required uint DrawCount { get; init; }
    public required ReadOnlySpan<VertexAttributeDescriptor> Attributes { get; init; }
    public required DrawPrimitive Primitive { get; init; }
    public required MeshDrawKind DrawKind { get; init; }
    public required DrawElementSize ElementSize { get; init; } 

    public static GpuMeshDescriptor MakeArray(ReadOnlySpan<VertexAttributeDescriptor> atr,
        DrawPrimitive primitive, uint drawCount)
    {
        return new GpuMeshDescriptor
        {
            Attributes = atr,
            Primitive = primitive,
            DrawCount = drawCount,
            DrawKind = MeshDrawKind.Arrays,
            ElementSize = DrawElementSize.Invalid
        };
    }
    public static GpuMeshDescriptor MakeElemental(ReadOnlySpan<VertexAttributeDescriptor> atr,
        DrawElementSize elementSize, DrawPrimitive primitive, uint drawCount)
    {
        return new GpuMeshDescriptor
        {
            Attributes = atr,
            Primitive = primitive,
            DrawCount = drawCount,
            DrawKind = MeshDrawKind.Elements,
            ElementSize = elementSize
        };
    }
}


public readonly ref struct GpuMeshVertexPayload<T, I> where T : unmanaged where I : unmanaged
{
    public GpuMeshVertexPayload(ReadOnlySpan<T> vertices, ReadOnlySpan<I> indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public ReadOnlySpan<T> Vertices { get; }
    public ReadOnlySpan<I> Indices { get; }
    
    public BufferUsage VboUsage { get; init; } = BufferUsage.StaticDraw;
    public BufferUsage IboUsage { get; init; } = BufferUsage.StaticDraw;
}

public readonly ref struct GpuMeshData<T, I> where T : unmanaged where I : unmanaged
{
    public GpuMeshData(ReadOnlySpan<T> vertices)
    {
        Vertices = vertices;
        Indices = ReadOnlySpan<I>.Empty;
    }

    public GpuMeshData(ReadOnlySpan<T> vertices, ReadOnlySpan<I> indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public ReadOnlySpan<T> Vertices { get; }
    public ReadOnlySpan<I> Indices { get; }
    
    public BufferUsage VboUsage { get; init; } = BufferUsage.StaticDraw;
    public BufferUsage IboUsage { get; init; } = BufferUsage.StaticDraw;

    public bool Elemental => Indices.Length > 0;
}
//New structure

internal readonly ref struct GpuAddMeshData<THandle, TMeta, TVboHandle, TVboMeta, IIboHandle, IIboMeta>
{
    public readonly THandle Handle;
    public readonly TMeta Meta;
    
    public readonly IIboHandle IboHandle;
    public readonly IIboMeta IboMeta;

    public readonly ReadOnlySpan<TVboHandle> VboHandles;
    public readonly ReadOnlySpan<TVboMeta> VboMetas;
}

public readonly struct GpuNewMeshDescriptor()
{
    public required MeshDrawKind DrawKind { get; init; }
    public required DrawPrimitive Primitive { get; init; }
    public uint DrawCount { get; init; } = 0;
}

public readonly ref struct GpuVboDescriptor<V> where V : unmanaged
{
    public ReadOnlySpan<V> Data { get; init; }
    public BufferUsage Usage { get; init; } = BufferUsage.StaticDraw;
    public uint BindingIndex { get; init; } = 0;

    public GpuVboDescriptor()
    {
        
    }
    public GpuVboDescriptor(ReadOnlySpan<V> data, BufferUsage usage, uint bindingIndex = 0)
    {
        Data = data;
        Usage = usage;
        BindingIndex = bindingIndex;
    }
    
    public static GpuVboDescriptor<V> Empty => new()
    {
        BindingIndex = 0,
        Data = ReadOnlySpan<V>.Empty,
        Usage = default,
    };
}
public readonly ref struct GpuIboDescriptor<I> where I : unmanaged
{
    public ReadOnlySpan<I> Data { get; init; }
    public BufferUsage Usage { get; init; } = BufferUsage.StaticDraw;
    public GpuIboDescriptor()
    {
        
    }
    public GpuIboDescriptor(ReadOnlySpan<I> data, BufferUsage usage)
    {
        Data = data;
        Usage = usage;
    }
}
