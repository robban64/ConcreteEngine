#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public record GlMeshFactoryResult(GlVertexBuffer VertexBuffer, GlIndexBuffer IndexBuffer);

public sealed class GlMesh : OpenGLResource, IMesh
{
    // public bool IsStaticMesh { get; private set; }
    public int VertexBufferId { get; private set; }

    public int IndexBufferId { get; private set; }
    public ICollection<VertexAttributeDescriptor> VertexAttributes { get; }
    public uint DrawCount { get; set; }

    public bool HasIndices => IndexBufferId > 0;

    internal GlMesh(
        uint handle,
        int vertexBufferId,
        int indexBufferId,
        VertexAttributeDescriptor[] vertexAttributes,
        uint drawCount
    ) : base(handle)
    {
        VertexBufferId = vertexBufferId;
        IndexBufferId = indexBufferId;
        VertexAttributes = vertexAttributes;
        DrawCount = drawCount;
    }
}