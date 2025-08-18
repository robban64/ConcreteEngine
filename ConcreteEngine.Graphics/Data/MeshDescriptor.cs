#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Data;

public record MeshDescriptor<TVertex, TIndex> where TVertex : unmanaged where TIndex : unmanaged
{
    public required VertexAttributeDescriptor[] VertexPointers { get; init; }
    public required MeshDataBufferDescriptor<TVertex> VertexBuffer { get; init; }
    public MeshDataBufferDescriptor<TIndex>? IndexBuffer { get; init; }
    public uint? DrawCount { get; set; } = null;
    public DrawPrimitive Primitive { get; set; } = DrawPrimitive.Triangles;
}

public record MeshDataBufferDescriptor<T>(
    BufferUsage Usage,
    T[]? Data
) where T : unmanaged;

public readonly record struct VertexAttributeDescriptor(
    uint StrideBytes, // total size of one vertex in bytes
    uint OffsetBytes, // offset of this attribute in the vertex struct
    VertexElementFormat Format = VertexElementFormat.Float2,
    bool Normalized = false
)
{
    public static VertexAttributeDescriptor Make<TStruct>(
        string fieldName,
        VertexElementFormat format = VertexElementFormat.Float2,
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