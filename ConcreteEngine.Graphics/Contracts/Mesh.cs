using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Contracts;

public interface IMeshPayload
{
    public MeshDrawProperties DrawProperties { get; init; }
    public IReadOnlyList<VertexAttributeDesc> Attributes { get; init; }
    public IReadOnlyList<VertexBufferPayload> VertexBuffers { get; init; }
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
    int DrawCount
)
{
    public static MeshDrawProperties FromMeta(in MeshMeta meta) =>
        new(meta.Primitive, meta.DrawKind, meta.ElementSize, meta.DrawCount);

    public static MeshMeta ToMeta(in MeshDrawProperties props, int attributeLength) =>
        new(props.Primitive, props.DrawKind, props.ElementSize, attributeLength, props.DrawCount);

    public static MeshDrawProperties MakeDefault() =>
        new(DrawPrimitive.Triangles, MeshDrawKind.Invalid, DrawElementSize.Invalid, 0);

    public static MeshDrawProperties MakeTriElemental(MeshDrawKind kind = MeshDrawKind.Elements,
        DrawElementSize size = DrawElementSize.UnsignedInt, int drawCount = 0)
    => new (DrawPrimitive.Triangles, kind, size, drawCount);
}

public readonly record struct VertexAttributeDesc(
    int VboBinding,
    int Stride, // vertex in bytes
    int Offset, // attribute in the vertex struct
    VertexElementFormat Format,
    int DivisorIndex = 0,
    int Divisor = 0,
    bool Normalized = false
)
{
    public static VertexAttributeDesc Make<TElement>(
        string fieldName,
        VertexElementFormat format,
        int vboBinding = 0,
        int divisorIndex = 0,
        int divisor = 0,
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

        var offsetBytes = offsetPtr.ToInt32();
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)offsetBytes, structSize, nameof(offsetBytes));

        return new VertexAttributeDesc(
            VboBinding: vboBinding,
            Stride: Unsafe.SizeOf<TElement>(),
            Offset: Marshal.OffsetOf<TElement>(fieldName)!.ToInt32(),
            Format: format,
            DivisorIndex: divisorIndex,
            Divisor: divisor,
            Normalized: normalized
        );
    }

    public static VertexAttributeDesc Make<TPrimitive>(
        int strideCount,
        int offsetCount,
        int vboBinding = 0,
        int divisorIndex = 0,
        int divisor = 0,
        VertexElementFormat format = VertexElementFormat.Float2,
        bool normalized = false)
        where TPrimitive : unmanaged
    {
        var size = Unsafe.SizeOf<TPrimitive>();

        if (size <= 0)
            throw new GraphicsException($"Size of {typeof(TPrimitive).Name} returned invalid {size}.");

        var strideBytes = strideCount * size;
        var offsetBytes = offsetCount * size;

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