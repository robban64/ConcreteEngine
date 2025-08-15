#region

using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public interface IMesh : IGraphicsResource
{
    public ushort VertexBufferId { get; }
    public ushort IndexBufferId { get; }
    public ICollection<VertexAttributeDescriptor> VertexAttributes { get; }
    public bool HasIndices { get; }
    public uint DrawCount { get; set; }
}