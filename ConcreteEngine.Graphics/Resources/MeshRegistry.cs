using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

public interface IMeshRegistry
{
    IMeshLayout Get(MeshId meshId);
}


// Todo animation skeleton data
public interface IMeshLayout
{
    MeshId MeshId { get;  }
    IndexBufferId IndexBufferId { get;  }
    ReadOnlySpan<VertexBufferId> GetVertexBufferIds();
    ReadOnlySpan<VertexAttributeDescriptor> GetAttributes();

}

internal sealed class MeshRegistry: IMeshRegistry
{
    private readonly Dictionary<MeshId, MeshLayout> _registry = new(16);

    public IMeshLayout Get(MeshId meshId)
    {
        return _registry[meshId];
    }
    
    public void RegisterMesh(MeshLayout record)
    {
        _registry.Add(record.MeshId, record);
    }
    
    public void RegisterEmptyMesh(MeshId meshId)
    {
        _registry.Add(meshId, new MeshLayout
        {
            MeshId = meshId
        });
    }
    public MeshLayout GetInternal(MeshId meshId)
    {
        return _registry[meshId];
    }

    public void UpdateIboId(MeshId meshId, IndexBufferId iboId)
        => GetInternal(meshId).IndexBufferId = iboId;
    
    public void UpdateVboIds(MeshId meshId, IReadOnlyList<VertexBufferId> vboIds)
        => GetInternal(meshId).VertexBufferIds  = vboIds.ToArray();

    public void UpdateAttributes(MeshId meshId, ReadOnlySpan<VertexAttributeDescriptor> attr)
        => GetInternal(meshId).Attributes = attr.ToArray();

    internal sealed class MeshLayout : IMeshLayout
    {
        public MeshId MeshId { get; set; } = default;
        public IndexBufferId IndexBufferId { get; set; } = default;
        public VertexBufferId[] VertexBufferIds { get; set; } = [];
        public VertexAttributeDescriptor[] Attributes { get; set; } = [];

        public ReadOnlySpan<VertexBufferId> GetVertexBufferIds() =>  VertexBufferIds;
        public ReadOnlySpan<VertexAttributeDescriptor> GetAttributes() => Attributes;
    }

}
