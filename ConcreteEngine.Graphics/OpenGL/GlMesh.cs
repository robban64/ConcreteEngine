#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public record GlMeshFactoryResult(GlVertexBuffer VertexBuffer, GlIndexBuffer IndexBuffer);

public sealed class GlMesh : IMesh
{
    public uint Handle { get; }
    public bool IsDisposed { get; set; } = false;
    public ushort VertexBufferId { get; }
    public ushort IndexBufferId { get; }
    public ICollection<VertexAttributeDescriptor> VertexAttributes { get; }
    public uint DrawCount { get; set; }
    public DrawElementsType ElementType { get; }

    public bool HasIndices => IndexBufferId > 0;

    internal GlMesh(
        uint handle,
        ushort vertexBufferId,
        ushort indexBufferId,
        VertexAttributeDescriptor[] vertexAttributes,
        uint drawCount,
        DrawElementsType elementType
    )
    {
        Handle = handle;
        VertexBufferId = vertexBufferId;
        IndexBufferId = indexBufferId;
        VertexAttributes = vertexAttributes;
        DrawCount = drawCount;
        ElementType = elementType;
    }
}