#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public readonly struct GpuMeshDescriptor()
{
    public required VertexAttributeDescriptor[] VertexPointers { get; init; }
    public required MeshDrawKind DrawKind { get; init; }
    public uint? DrawCount { get; init; } = null;
    public DrawPrimitive? Primitive { get; init; } = null;
    public BufferUsage VboUsage { get; init; } = BufferUsage.StaticDraw;
    public BufferUsage IboUsage { get; init; } = BufferUsage.StaticDraw;
    
}

public readonly ref struct GpuMeshData<T, I> where T : unmanaged where I : unmanaged
{
    public readonly ReadOnlySpan<T> Vertices;
    public readonly ReadOnlySpan<I> Indices;
    public bool Elemental { get; }

    public GpuMeshData(ReadOnlySpan<T> vertices)
    {
        Vertices = vertices;
        Indices = ReadOnlySpan<I>.Empty;
        Elemental = false;
    }

    public GpuMeshData(ReadOnlySpan<T> vertices, ReadOnlySpan<I> indices)
    {
        Vertices = vertices;
        Indices = indices;
        Elemental = true;
    }
}

public readonly record struct VertexAttributeDescriptor(
    uint StrideBytes, // total size of one vertex in bytes
    uint OffsetBytes, // offset of this attribute in the vertex struct
    VertexElementFormat Format,
    bool Normalized = false
)
{
    public static VertexAttributeDescriptor Make<TStruct>(
        string fieldName,
        VertexElementFormat format,
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
            StrideBytes: (uint)Unsafe.SizeOf<TStruct>(),
            OffsetBytes: (uint)Marshal.OffsetOf<TStruct>(fieldName)!.ToInt32(),
            Format: format,
            Normalized: normalized
        );
    }

    public static VertexAttributeDescriptor Make<TPrimitive>(
        uint strideCount,
        uint offsetCount,
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
            StrideBytes: strideBytes,
            OffsetBytes: offsetBytes,
            Format: format,
            Normalized: normalized
        );
    }
}