#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public interface IGfxMeshBuilder
{
    MeshId Finish();

    void UploadVertices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged;

    void UploadIndices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged;

    void AddAttribute(in VertexAttributeDesc attribute);
    void SetAttributeRange(IReadOnlyList<VertexAttributeDesc> attributes);
}

internal sealed class GfxMeshBuilder : IGfxMeshBuilder
{
    private readonly State _state = new();

    private GfxMeshes _gfxMeshes = null!;
    private GfxBuffers _gfxBuffers = null!;
    private Phase _phase = Phase.Idle;

    internal GfxMeshBuilder Init(GfxMeshes gfxMeshes, GfxBuffers gfxBuffers, in MeshDrawProperties props)
    {
        _gfxMeshes = gfxMeshes;
        _gfxBuffers = gfxBuffers;

        _state.ResetState();
        _state.MeshId = _gfxMeshes.CreateEmptyMesh();
        _state.DrawProperties = props;

        _phase = Phase.Started;
        return this;
    }

    public MeshId Finish()
    {
        InvalidOpThrower.ThrowIfNot(_state.MeshId.IsValid());
        EnsureStarted();

        _gfxMeshes.SetVertexAttributes(_state.MeshId, _state.Attributes);
        var id = _gfxMeshes.FinishUploadCommit(_state);

        _state.ResetState();
        _phase = Phase.Idle;
        _gfxMeshes = null!;
        _gfxBuffers = null!;
        return id;
    }

    public void UploadVertices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged
    {
        EnsureStarted();
        var binding = _state.VboIds.Count;
        var vboId = _gfxBuffers.CreateVertexBuffer(data, binding, storage, access);

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
                ElementSize = elementSize, DrawKind = MeshDrawKind.Elements
            };
        }
        else
        {
            InvalidOpThrower.ThrowIfNot(_state.DrawProperties.ElementSize == elementSize, nameof(elementSize));
            InvalidOpThrower.ThrowIfNot(_state.DrawProperties.DrawKind == MeshDrawKind.Elements);
        }

        _state.IboId = _gfxBuffers.CreateIndexBuffer(data, storage, access);
        _gfxMeshes.AttachIndexBuffer(_state.MeshId, _state.IboId);
    }

    public void SetAttributeRange(IReadOnlyList<VertexAttributeDesc> attributes)
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIfNullOrEmptyCollection(attributes, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.Attributes.Count == 0, nameof(attributes));

        _state.Attributes.AddRange(attributes);

        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public void SetAttributeSpan(ReadOnlySpan<VertexAttributeDesc> attributes)
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIf(attributes.Length == 0, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.Attributes.Count == 0, nameof(attributes));

        _state.Attributes.AddRange(attributes);

        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public void AddAttribute(in VertexAttributeDesc attribute)
    {
        EnsureStarted();

        _state.Attributes.Add(attribute);

        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }


    private void EnsureStarted() => InvalidOpThrower.ThrowIfNot(_phase >= Phase.Started, "Builder not started.");

    private enum Phase : byte
    {
        Idle = 0,
        Started,
        BuffersUploading,
        AttributesSet
    }

    internal sealed class State
    {
        public MeshId MeshId { get; set; }
        public IndexBufferId IboId { get; set; }
        public List<VertexBufferId> VboIds { get; set; } = new();
        public List<VertexAttributeDesc> Attributes { get; set; } = new();
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
                InvalidOpThrower.ThrowIf(DrawProperties.DrawKind != MeshDrawKind.Elements);
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