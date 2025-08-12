#region

using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Graphics;

public interface IMesh : IGraphicsResource
{
    public int VertexBufferId { get; }
    public int IndexBufferId { get; }
    public ICollection<VertexAttributeDescriptor> VertexAttributes { get; }
    public bool HasIndices { get; }
    public uint DrawCount { get; set; }
}