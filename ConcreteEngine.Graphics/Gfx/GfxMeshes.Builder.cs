#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;

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
    void SetAttributeSpan(ReadOnlySpan<VertexAttribute> attributes);
}

internal sealed class GfxMeshBuilder : IGfxMeshBuilder
{
    private readonly MeshBuildState _state = new();

    private GfxMeshes _gfxMeshes = null!;
    private GfxBuffers _gfxBuffers = null!;
    private Phase _phase;

    private int _vboIdx = 0;

    internal GfxMeshBuilder(GfxMeshes gfxMeshes, GfxBuffers gfxBuffers, in MeshDrawProperties props)
    {
        _gfxMeshes = gfxMeshes;
        _gfxBuffers = gfxBuffers;

        //_state.ResetState();
        _state.MeshId = _gfxMeshes.CreateEmptyMesh(in props);
        _state.DrawProperties = props;

        _phase = Phase.Started;
    }

    public MeshLayout Finish()
    {
        InvalidOpThrower.ThrowIfNot(_state.MeshId.IsValid());
        EnsureStarted();

        _state.ValidateState();
        var result = _state.Compile();
        _gfxMeshes.SetVertexAttributes(result.MeshId, result.Attributes);

        _phase = Phase.Closed;

        _gfxMeshes = null!;
        _gfxBuffers = null!;

        return result;
    }


    public void UploadVertices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIf(_vboIdx >= GfxLimits.MaxVboBindings, nameof(_vboIdx));
        var vboId = _gfxBuffers.CreateVertexBuffer(data, 0, 0, storage, access);

        _state.VboIds[_vboIdx] = vboId;
        _gfxMeshes.AttachVertexBuffer(_state.MeshId, vboId, _vboIdx);
        _vboIdx++;
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
        InvalidOpThrower.ThrowIfNot(_state.AttribCount == 0, nameof(attributes));
        InvalidOpThrower.ThrowIf(attributes.Count >= GfxLimits.MaxVertexAttribs, nameof(_vboIdx));

        _state.Attributes = attributes.ToArray();
        _state.AttribCount = attributes.Count;
        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public void SetAttributeSpan(ReadOnlySpan<VertexAttribute> attributes)
    {
        EnsureStarted();
        ArgumentOutOfRangeException.ThrowIfEqual(attributes.Length, 0, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.AttribCount == 0, nameof(attributes));
        InvalidOpThrower.ThrowIf(attributes.Length >= GfxLimits.MaxVertexAttribs, nameof(_vboIdx));

        _state.Attributes = attributes.ToArray();
        _state.AttribCount = attributes.Length;
        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public void AddAttribute(in VertexAttribute attribute)
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIf(_state.AttribCount + 1 >= GfxLimits.MaxVertexAttribs, nameof(_vboIdx));
        if (_state.Attributes.Length == 0 && _state.AttribCount == 0)
            _state.Attributes = new VertexAttribute[4];

        var stateAttributes = _state.Attributes;
        stateAttributes[_state.AttribCount++] = attribute;

        if (_state.AttribCount >= _state.Attributes.Length)
            Array.Resize(ref stateAttributes, GfxLimits.MaxVertexAttribs);

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
}

internal sealed class MeshBuildState
{
    public int AttribCount { get; set; }
    public int VboCount { get; set; }
    public MeshId MeshId { get; set; }
    public IndexBufferId IboId { get; set; }
    public VertexBufferId[] VboIds { get; set; } = new VertexBufferId[4];
    public VertexAttribute[] Attributes { get; set; } = Array.Empty<VertexAttribute>();
    public MeshDrawProperties DrawProperties { get; set; } = MeshDrawProperties.MakeDefault();

    public MeshLayout Compile()
    {
        var vbo = VboCount == VboIds.Length ? VboIds : VboIds.AsSpan(0, VboCount).ToArray();
        var attribs = AttribCount == Attributes.Length ? Attributes : Attributes.AsSpan(0, AttribCount).ToArray();
        return new MeshLayout(MeshId, IboId, vbo, attribs);
    }

    public void ValidateState()
    {
        InvalidOpThrower.ThrowIfNot(MeshId.IsValid(), nameof(MeshId));
        InvalidOpThrower.ThrowIfNullOrEmptyCollection(VboIds, nameof(VboIds));
        InvalidOpThrower.ThrowIfNullOrEmptyCollection(Attributes, nameof(Attributes));

        InvalidOpThrower.ThrowIfNot(VboIds[0].IsValid(), nameof(VboIds));

        if (IboId.IsValid())
        {
            InvalidOpThrower.ThrowIf(DrawProperties.ElementSize == DrawElementSize.Invalid);
            InvalidOpThrower.ThrowIf(DrawProperties.Kind != DrawMeshKind.Elements);
        }
    }
}