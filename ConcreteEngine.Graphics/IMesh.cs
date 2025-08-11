#region

using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Graphics;

public interface IMesh : IGraphicsResource
{
    public IGraphicsBuffer VertexBuffer { get; }
    public IGraphicsBuffer IndexBuffer { get; }
    public ICollection<VertexAttributeDescriptor> VertexAttributes { get; }
    public bool HasIndices { get; }
    public bool IsStaticMesh { get; }
    public uint DrawCount { get; set; }
}