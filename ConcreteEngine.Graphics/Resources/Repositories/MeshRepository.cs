using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Graphics.Resources;



// Todo animation skeleton data
public interface IMeshLayout
{
    MeshId MeshId { get; }
    IndexBufferId IndexBufferId { get; }
    ReadOnlySpan<VertexBufferId> GetVertexBufferIds();
    ReadOnlySpan<VertexAttributeDesc> GetAttributes();
    MeshDrawKind DrawKind { get; }
    bool IsElemental { get; }
    uint DrawCount { get; }
}


public interface IMeshRepository
{
    IMeshLayout Get(MeshId meshId);
}
internal sealed class MeshRepository : IMeshRepository
{
    private readonly Dictionary<MeshId, MeshLayout> _registry = new(16);

    public IMeshLayout Get(MeshId meshId)
    {
        return _registry[meshId];
    }


    internal void AddRecord(MeshId meshId, in MeshMeta meta, IndexBufferId iboId, IReadOnlyList<VertexBufferId> vboIds, IReadOnlyList<VertexAttributeDesc> attr)
    {
        _registry.Add(meshId, new MeshLayout(meshId, in meta, iboId, vboIds, attr));
    }
    

    internal void RemoveRecord(MeshId meshId)
    {
        _registry.Remove(meshId);
    }
    
    internal void UpdateDrawCount(MeshId meshId,uint drawCount) => GetInternal(meshId).DrawCount = drawCount;

    private MeshLayout GetInternal(MeshId meshId)
    {
        return _registry[meshId];
    }


    internal sealed class MeshLayout : IMeshLayout
    {

        internal MeshLayout(MeshId meshId, in MeshMeta meta, IndexBufferId iboId, IReadOnlyList<VertexBufferId> vboIds,
            IReadOnlyList<VertexAttributeDesc> attr)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(meshId.Value,0);
            MeshId = meshId;
            DrawCount = meta.DrawCount;
            IsElemental = meta.ElementSize != DrawElementSize.Invalid && iboId.IsValid();
            DrawKind = meta.DrawKind;

            IndexBufferId = iboId;
            VertexBufferIds = vboIds.ToArray();
            Attributes = attr.ToArray();
        }

        public MeshId MeshId { get; init; } = default;
        public IndexBufferId IndexBufferId { get; init; } = default;
        public VertexBufferId[] VertexBufferIds { get; init; }
        public VertexAttributeDesc[] Attributes { get; init; }

        public MeshDrawKind DrawKind { get; init; }
        public bool IsElemental { get; init; }
        public uint DrawCount { get; internal set; }

        public ReadOnlySpan<VertexBufferId> GetVertexBufferIds() => VertexBufferIds;
        public ReadOnlySpan<VertexAttributeDesc> GetAttributes() => Attributes;
    }
}