using ConcreteEngine.Graphics.Contracts;

namespace ConcreteEngine.Graphics.Resources;

// Todo animation skeleton data
public interface IMeshLayout
{
    MeshId MeshId { get; }
    IndexBufferId IndexBufferId { get; }
    MeshDrawProperties Properties { get;  }
    ReadOnlySpan<VertexBufferId> GetVertexBufferIds();
    ReadOnlySpan<VertexAttributeDesc> GetAttributes();
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


    internal void AddRecord(MeshId meshId, in MeshMeta meta, IndexBufferId iboId, IReadOnlyList<VertexBufferId> vboIds,
        IReadOnlyList<VertexAttributeDesc> attr)
    {
        _registry.Add(meshId, new MeshLayout(meshId, in meta, iboId, vboIds, attr));
    }

    internal void AddRecord(MeshId meshId, MeshLayout layout)
    {
        _registry.Add(meshId, layout);
    }

    internal bool HasRecord(MeshId meshId) => _registry.ContainsKey(meshId);

    internal void RemoveRecord(MeshId meshId)
    {
        _registry.Remove(meshId);
    }

    internal void UpdateDrawCount(MeshId meshId, int drawCount)
    {
        var layout = GetInternal(meshId);
        layout.Properties = layout.Properties with { DrawCount = drawCount };
    }

    private MeshLayout GetInternal(MeshId meshId)
    {
        return _registry[meshId];
    }


    internal sealed class MeshLayout : IMeshLayout
    {
        internal MeshLayout(MeshId meshId)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(meshId.Value, 0);
            MeshId = meshId;
        }

        internal MeshLayout(MeshId meshId, in MeshMeta meta, IndexBufferId iboId, IReadOnlyList<VertexBufferId> vboIds,
            IReadOnlyList<VertexAttributeDesc> attr)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(meshId.Value, 0);
            MeshId = meshId;
            IndexBufferId = iboId;
            VertexBufferIds = vboIds.ToArray();
            Attributes = attr.ToArray();
            Properties = MeshDrawProperties.FromMeta(in meta);
        }

        public MeshId MeshId { get; init; }
        public IndexBufferId IndexBufferId { get; init; } = default;
        public VertexBufferId[] VertexBufferIds { get; init; }
        public VertexAttributeDesc[] Attributes { get; init; }
        public MeshDrawProperties Properties { get; internal set; }

        public ReadOnlySpan<VertexBufferId> GetVertexBufferIds() => VertexBufferIds;
        public ReadOnlySpan<VertexAttributeDesc> GetAttributes() => Attributes;
    }
}