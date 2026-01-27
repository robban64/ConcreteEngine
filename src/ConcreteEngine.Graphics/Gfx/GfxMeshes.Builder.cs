using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Internal;

namespace ConcreteEngine.Graphics.Gfx;

public abstract class GfxMeshBuilder
{
    public abstract void UploadVerticesEmpty<T>(int componentCapacity, BufferUsage usage, BufferStorage storage,
        BufferAccess access,
        byte divisor = 0) where T : unmanaged;

    public abstract void UploadVertices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access, byte divisor = 0) where T : unmanaged;

    public abstract void UploadIndices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access) where T : unmanaged;

    public abstract void AddAttribute(in VertexAttribute attribute);
    public abstract void SetAttributeRange(IReadOnlyList<VertexAttribute> attributes);
    public abstract void SetAttributeSpan(ReadOnlySpan<VertexAttribute> attributes);
}

internal sealed class MeshBuilder : GfxMeshBuilder
{
    private readonly MeshBuildState _state = new();

    private GfxMeshes _gfxMeshes;
    private GfxBuffers _gfxBuffers;
    private Phase _phase;


    internal MeshBuilder(GfxMeshes gfxMeshes, GfxBuffers gfxBuffers, in MeshDrawProperties props)
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


    public override void UploadVerticesEmpty<T>(int componentCapacity, BufferUsage usage, BufferStorage storage,
        BufferAccess access, byte divisor = 0)
    {
        EnsureStarted();
        if (_state.VboCount >= GfxLimits.MaxVboBindings)
            throw GraphicsException.LimitExceeded(nameof(GfxLimits.MaxVboBindings), GfxLimits.MaxVboBindings);

        var vboId = _gfxBuffers.CreateVertexBuffer(ReadOnlySpan<T>.Empty, divisor, 0, storage, access,
            componentCapacity);
        AttachVboInternal(vboId);
    }

    public override void UploadVertices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access, byte divisor = 0)
    {
        EnsureStarted();
        if (_state.VboCount >= GfxLimits.MaxVboBindings)
            throw GraphicsException.LimitExceeded(nameof(GfxLimits.MaxVboBindings), GfxLimits.MaxVboBindings);

        var vboId = _gfxBuffers.CreateVertexBuffer(data, divisor, 0, storage, access);
        AttachVboInternal(vboId);
    }

    private void AttachVboInternal(VertexBufferId vboId)
    {
        _state.VboIds[_state.VboCount] = vboId;
        _gfxMeshes.AttachVertexBuffer(_state.MeshId, vboId, _state.VboCount);
        _state.VboCount++;
        if (_phase < Phase.BuffersUploading) _phase = Phase.BuffersUploading;
    }


    public override void UploadIndices<T>(ReadOnlySpan<T> data, BufferUsage usage,
        BufferStorage storage, BufferAccess access)
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

    public override void SetAttributeRange(IReadOnlyList<VertexAttribute> attributes)
    {
        EnsureStarted();
        InvalidOpThrower.ThrowIfNullOrEmptyCollection(attributes, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.AttribCount == 0, nameof(attributes));
        if (attributes.Count >= GfxLimits.MaxVertexAttribs)
            throw GraphicsException.LimitExceeded(nameof(GfxLimits.MaxVertexAttribs), GfxLimits.MaxVertexAttribs);

        _state.Attributes = attributes.ToArray();
        _state.AttribCount = attributes.Count;
        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public override void SetAttributeSpan(ReadOnlySpan<VertexAttribute> attributes)
    {
        EnsureStarted();
        ArgumentOutOfRangeException.ThrowIfEqual(attributes.Length, 0, nameof(attributes));
        InvalidOpThrower.ThrowIfNot(_state.AttribCount == 0, nameof(attributes));
        if (attributes.Length >= GfxLimits.MaxVertexAttribs)
            throw GraphicsException.LimitExceeded(nameof(GfxLimits.MaxVertexAttribs), GfxLimits.MaxVertexAttribs);

        _state.Attributes = attributes.ToArray();
        _state.AttribCount = attributes.Length;
        if (_phase < Phase.AttributesSet) _phase = Phase.AttributesSet;
    }

    public override void AddAttribute(in VertexAttribute attribute)
    {
        EnsureStarted();
        if (_state.AttribCount + 1 >= GfxLimits.MaxVertexAttribs)
            throw GraphicsException.LimitExceeded(nameof(GfxLimits.MaxVertexAttribs), GfxLimits.MaxVertexAttribs);

        _state.EnsureAttributes(_state.AttribCount);

        var stateAttributes = _state.Attributes;
        stateAttributes[_state.AttribCount++] = attribute;

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
    private VertexAttribute[] _attributes = [];

    public int AttribCount { get; set; }
    public int VboCount { get; set; }
    public MeshId MeshId { get; set; }
    public IndexBufferId IboId { get; set; }

    public VertexBufferId[] VboIds { get; set; } = new VertexBufferId[4];

    public VertexAttribute[] Attributes
    {
        get => _attributes;
        set => _attributes = value;
    }

    public MeshDrawProperties DrawProperties { get; set; } = MeshDrawProperties.MakeArray();

    public void EnsureAttributes(int count)
    {
        if (_attributes.Length == 0) _attributes = new VertexAttribute[4];
        if (count == 4) Array.Resize(ref _attributes, 8);
        if (count == 8) Array.Resize(ref _attributes, 16);
    }

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