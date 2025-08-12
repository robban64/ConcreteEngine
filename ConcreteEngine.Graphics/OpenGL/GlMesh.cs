#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public record GlMeshFactoryResult(GlVertexBuffer VertexBuffer, GlIndexBuffer IndexBuffer);

public sealed class GlMesh :  IMesh
{
    public uint Handle { get; }
    public bool IsDisposed { get; set; } = false;
    public ushort VertexBufferId { get; private set; }

    public ushort IndexBufferId { get; private set; }
    public ICollection<VertexAttributeDescriptor> VertexAttributes { get; }
    public uint DrawCount { get; set; }

    public bool HasIndices => IndexBufferId > 0;

    internal GlMesh(
        uint handle,
        ushort vertexBufferId,
        ushort indexBufferId,
        VertexAttributeDescriptor[] vertexAttributes,
        uint drawCount
    )
    {
        Handle = handle;
        VertexBufferId = vertexBufferId;
        IndexBufferId = indexBufferId;
        VertexAttributes = vertexAttributes;
        DrawCount = drawCount;
    }
}