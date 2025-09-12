#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public readonly ref struct GpuMeshDescriptor
{
    public required ReadOnlySpan<VertexAttributeDescriptor> Attributes { get; init; }
    public required DrawPrimitive Primitive { get; init; }
    public required MeshDrawKind DrawKind { get; init; }
    public required DrawElementType ElementType { get; init; } 
    public required uint DrawCount { get; init; }

    public static GpuMeshDescriptor MakeArray(ReadOnlySpan<VertexAttributeDescriptor> atr,
        DrawPrimitive primitive, uint drawCount)
    {
        return new GpuMeshDescriptor
        {
            Attributes = atr,
            Primitive = primitive,
            DrawCount = drawCount,
            DrawKind = MeshDrawKind.Arrays,
            ElementType = DrawElementType.Invalid
        };
    }
    public static GpuMeshDescriptor MakeElemental(ReadOnlySpan<VertexAttributeDescriptor> atr,
        DrawElementType elementType, DrawPrimitive primitive, uint drawCount)
    {
        return new GpuMeshDescriptor
        {
            Attributes = atr,
            Primitive = primitive,
            DrawCount = drawCount,
            DrawKind = MeshDrawKind.Elements,
            ElementType = elementType
        };
    }
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



public readonly record struct VertexAttributeDescriptor(
    uint VboIndex,
    uint StrideBytes, // total size of one vertex in bytes
    uint OffsetBytes, // offset of this attribute in the vertex struct
    VertexElementFormat Format,
    uint DivisorIndex = 0,
    uint Divisor = 0,
    bool Normalized = false
)
{
    public static VertexAttributeDescriptor Make<TStruct>(
        string fieldName,
        VertexElementFormat format,
        uint vboIndex = 0,
        uint divisorIndex = 0,
        uint divisor = 0,
        bool normalized = false)
        where TStruct : struct
    {
        var structSize = Unsafe.SizeOf<TStruct>();


        if (structSize <= 0)
            throw new GraphicsException($"Size of {typeof(TStruct).Name} returned invalid {structSize}.");

        IntPtr offsetPtr;
        try
        {
            offsetPtr = Marshal.OffsetOf<TStruct>(fieldName);
        }
        catch (Exception)
        {
            throw new Exception($"Field '{fieldName}' not found in struct '{typeof(TStruct).Name}'.");
        }

        var offsetBytes = (uint)offsetPtr.ToInt32();
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)offsetBytes, structSize, nameof(offsetBytes));

        return new VertexAttributeDescriptor(
            VboIndex: vboIndex,
            StrideBytes: (uint)Unsafe.SizeOf<TStruct>(),
            OffsetBytes: (uint)Marshal.OffsetOf<TStruct>(fieldName)!.ToInt32(),
            Format: format,
            DivisorIndex: divisorIndex,
            Divisor: divisor,
            Normalized: normalized
        );
    }

    public static VertexAttributeDescriptor Make<TPrimitive>(
        uint strideCount,
        uint offsetCount,
        uint vboIndex = 0,
        uint divisorIndex = 0,
        uint divisor = 0,
        VertexElementFormat format = VertexElementFormat.Float2,
        bool normalized = false)
        where TPrimitive : unmanaged
    {
        var size = Unsafe.SizeOf<TPrimitive>();

        if (size <= 0)
            throw new GraphicsException($"Size of {typeof(TPrimitive).Name} returned invalid {size}.");

        var strideBytes = strideCount * (uint)size;
        var offsetBytes = offsetCount * (uint)size;

        if (offsetBytes >= strideBytes)
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offsetBytes, strideBytes, nameof(offsetBytes));

        return new VertexAttributeDescriptor(
            VboIndex: vboIndex,
            StrideBytes: strideBytes,
            OffsetBytes: offsetBytes,
            Format: format,
            DivisorIndex: divisorIndex,
            Divisor: divisor,
            Normalized: normalized
        );
    }
}