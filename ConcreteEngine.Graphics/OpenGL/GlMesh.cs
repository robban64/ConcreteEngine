#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public record GlMeshFactoryResult(GlVertexBuffer VertexBuffer, GlIndexBuffer IndexBuffer);

public sealed class GlMesh : OpenGLResource, IMesh
{
    public bool IsStaticMesh { get; private set; }
    public uint DrawCount { get; set; }
    public ICollection<VertexAttributeDescriptor> VertexAttributes { get; }
    public IGraphicsBuffer IndexBuffer { get; private set; }
    public IGraphicsBuffer VertexBuffer { get; private set; }

    public bool HasIndices => IndexBuffer is not null;

    internal GlMesh(
        uint handle,
        GlVertexBuffer vertexBuffer,
        GlIndexBuffer indexBuffer,
        VertexAttributeDescriptor[] vertexAttributes
    ) : base(handle)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        VertexAttributes = vertexAttributes;

        if (IndexBuffer is not null) DrawCount = (uint)IndexBuffer.ElementCount;
        else DrawCount = (uint)VertexBuffer.ElementCount;

        IsStaticMesh = VertexBuffer.Usage == BufferUsage.StaticDraw;
    }
}