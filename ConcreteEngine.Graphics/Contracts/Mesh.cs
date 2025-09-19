using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Contracts;

public interface IMeshPayload
{
    public MeshDrawProperties DrawProperties { get; init; }
    public IReadOnlyList<VertexAttributeDesc> Attributes { get; init;}
    public IReadOnlyList<VertexBufferPayload> VertexBuffers { get; init;}
}

public sealed class MeshPayloadBasic : IMeshPayload
{
    public required MeshDrawProperties DrawProperties { get; init; }
    public required IReadOnlyList<VertexAttributeDesc> Attributes { get; init; }
    public required IReadOnlyList<VertexBufferPayload> VertexBuffers { get; init; }
}

public sealed class MeshPayloadIndexed : IMeshPayload
{
    public required MeshDrawProperties DrawProperties { get; init; }
    public required IReadOnlyList<VertexAttributeDesc> Attributes { get; init; }
    public required IReadOnlyList<VertexBufferPayload> VertexBuffers { get; init; }
    public required IndexBufferPayload IndexBuffer { get; init; }
}


public readonly record struct MeshDrawProperties(
    DrawPrimitive Primitive,
    MeshDrawKind DrawKind,
    DrawElementSize ElementSize,
    uint DrawCount
)
{
    public static MeshDrawProperties MakeDefault() =>
        new(DrawPrimitive.Triangles, MeshDrawKind.Invalid, DrawElementSize.Invalid, 0);
}

public readonly record struct VertexAttributeDesc(
    uint VboBinding,
    uint Stride, // vertex in bytes
    uint Offset, // attribute in the vertex struct
    VertexElementFormat Format,
    uint DivisorIndex = 0,
    uint Divisor = 0,
    bool Normalized = false
)
{
    public static VertexAttributeDesc Make<TElement>(
        string fieldName,
        VertexElementFormat format,
        uint vboBinding = 0,
        uint divisorIndex = 0,
        uint divisor = 0,
        bool normalized = false)
        where TElement : unmanaged
    {
        var structSize = Unsafe.SizeOf<TElement>();
        if (structSize <= 0)
            throw new GraphicsException($"Size of {typeof(TElement).Name} returned invalid {structSize}.");

        IntPtr offsetPtr;
        try
        {
            offsetPtr = Marshal.OffsetOf<TElement>(fieldName);
        }
        catch (Exception)
        {
            throw new Exception($"Field '{fieldName}' not found in struct '{typeof(TElement).Name}'.");
        }

        var offsetBytes = (uint)offsetPtr.ToInt32();
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)offsetBytes, structSize, nameof(offsetBytes));

        return new VertexAttributeDesc(
            VboBinding: vboBinding,
            Stride: (uint)Unsafe.SizeOf<TElement>(),
            Offset: (uint)Marshal.OffsetOf<TElement>(fieldName)!.ToInt32(),
            Format: format,
            DivisorIndex: divisorIndex,
            Divisor: divisor,
            Normalized: normalized
        );
    }

    public static VertexAttributeDesc Make<TPrimitive>(
        uint strideCount,
        uint offsetCount,
        uint vboBinding = 0,
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

        return new VertexAttributeDesc(
            VboBinding: vboBinding,
            Stride: strideBytes,
            Offset: offsetBytes,
            Format: format,
            DivisorIndex: divisorIndex,
            Divisor: divisor,
            Normalized: normalized
        );
    }
}