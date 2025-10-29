#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public interface IGfxMeshBuilder
{
    void UploadVertices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged;

    void UploadIndices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged;

    void AddAttribute(in VertexAttribute attribute);
    void SetAttributeRange(IReadOnlyList<VertexAttribute> attributes);
    void SetAttributeRange(ReadOnlySpan<VertexAttribute> attributes);
}

internal sealed class GfxMeshBuilder : IGfxMeshBuilder
{
    private readonly State _state = new();

    private GfxMeshes _gfxMeshes = null!;
    private GfxBuffers _gfxBuffers = null!;
    private Phase _phase;

    internal GfxMeshBuilder(GfxMeshes gfxMeshes, GfxBuffers gfxBuffers, in MeshDrawProperties props)
    {
        _gfxMeshes = gfxMeshes;
        _gfxBuffers = gfxBuffers;

        _state.ResetState();
        _state.MeshId = _gfxMeshes.CreateEmptyMesh();
        _state.DrawProperties = props;

        _phase = Phase.Started;
    }

    public State Finish()
    {
        InvalidOpThrower.ThrowIfNot(_state.MeshId.IsValid());
        EnsureStarted();

        _state.ValidateState();
        
        _gfxMeshes.SetVertexAttributes(_state.MeshId, _state.Attributes);

        _phase = Phase.Closed;

        _gfxMeshes = null!;
        _gfxBuffers = null!;
        
        return _state;
    }


    public void UploadVertices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged
    {
        EnsureStarted();
        var binding = _state.VboIds.Count;
        var vboId = _gfxBuffers.CreateVertexBuffer(data,0, 0, storage, access);

        _state.VboIds.Add(vboId);
        _gfxMeshes.AttachVertexBuffer(_state.MeshId, vboId, binding);

        if (_phase < Phase.BuffersUploading) _phase = Phase.BuffersUploading;
    }


    public void UploadIndices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIf(_state.IboId.IsValid(), nameof(_state.IboId));

        var elementSize = GfxUtilsEnum.ToDrawElementSize<T>();
        if (_state.DrawProperties.ElementSize == DrawElementSize.Invalid)
        {
            _state.DrawProperties = _state.DrawProperties with
            {
                ElementSize = elementSize, Kind = DrawMeshKind.Elements
            };
        }
        else
        {
            InvalidOpThrower.ThrowIfNot(_state.DrawProperties.ElementSize == elementSize, nameof(elementSize));
            InvalidOpThrower.ThrowIfNot(_state.DrawProperties.Kind == DrawMeshKind.Elements);
        }

        _state.IboId = _gfxBuffers.CreateIndexBuffer(data, storage, access);
        _gfxMeshes.AttachIndexBuffer(_state.MeshId, _state.IboId);
    }

    public void SetAttributeRange(IReadOnlyList<VertexAttribute> attributes)
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIfNullOrEmptyCollection(attributes, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.Attributes.Count == 0, nameof(attributes));

        _state.Attributes.AddRange(attributes);

        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public void SetAttributeRange(ReadOnlySpan<VertexAttribute> attributes)
    {
        EnsureStarted();
        ArgumentOutOfRangeException.ThrowIfEqual(attributes.Length, 0, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.Attributes.Count == 0, nameof(attributes));

        _state.Attributes.AddRange(attributes);

        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public void SetAttributeSpan(ReadOnlySpan<VertexAttribute> attributes)
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIf(attributes.Length == 0, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.Attributes.Count == 0, nameof(attributes));

        _state.Attributes.AddRange(attributes);

        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public void AddAttribute(in VertexAttribute attribute)
    {
        EnsureStarted();

        _state.Attributes.Add(attribute);

        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }


    private void EnsureStarted() => InvalidOpThrower.ThrowIfNot(_phase >= Phase.Started, "Builder not started.");

    private enum Phase : byte
    {
        Closed = 0,
        Started = 1,
        BuffersUploading = 2,
        AttributesSet = 3
    }

    internal sealed class State
    {
        public MeshId MeshId { get; set; }
        public IndexBufferId IboId { get; set; }
        public List<VertexBufferId> VboIds { get; set; } = new(2);
        public List<VertexAttribute> Attributes { get; set; } = new(4);
        public MeshDrawProperties DrawProperties { get; set; } = MeshDrawProperties.MakeDefault();

        public void ValidateState()
        {
            InvalidOpThrower.ThrowIfNot(MeshId.IsValid(), nameof(MeshId));
            InvalidOpThrower.ThrowIfNullOrEmptyCollection(VboIds, nameof(VboIds));
            InvalidOpThrower.ThrowIfNullOrEmptyCollection(Attributes, nameof(Attributes));

            foreach (var vboId in VboIds)
                InvalidOpThrower.ThrowIfNot(vboId.IsValid(), nameof(vboId));

            if (IboId.IsValid())
            {
                InvalidOpThrower.ThrowIf(DrawProperties.ElementSize == DrawElementSize.Invalid);
                InvalidOpThrower.ThrowIf(DrawProperties.Kind != DrawMeshKind.Elements);
            }
        }

        public void ResetState()
        {
            MeshId = default;
            IboId = default;
            VboIds.Clear();
            Attributes.Clear();
            DrawProperties = MeshDrawProperties.MakeDefault();
        }
    }
}