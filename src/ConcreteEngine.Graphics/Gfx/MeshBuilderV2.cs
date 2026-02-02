using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Internal;

namespace ConcreteEngine.Graphics.Gfx;


public ref struct MeshBuilderV2(GfxMeshes gfxMeshes, GfxBuffers gfxBuffers, in MeshDrawProperties drawProps)
{
    private int _vboCount = 0;
    private MeshId _meshId;
    private MeshDrawProperties _drawProperties = drawProps;
    private bool _hasIbo = false;


    public void Start()
    {
        InvalidOpThrower.ThrowIf(_meshId.IsValid(), nameof(_meshId));
        _meshId = gfxMeshes.CreateEmptyMesh(in _drawProperties);
        _vboCount = 0;
        _hasIbo = false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public readonly MeshId End(out MeshMeta meta)
    {
        InvalidOpThrower.ThrowIfNot(_meshId.IsValid(), nameof(_meshId));
        if (_hasIbo)
        {
            InvalidOpThrower.ThrowIf(_drawProperties.ElementSize == DrawElementSize.Invalid);
            InvalidOpThrower.ThrowIf(_drawProperties.Kind != DrawMeshKind.Elements);
        }

        var meshId = gfxMeshes.FinishUploadBuilder(out meta);
        InvalidOpThrower.ThrowIfNot(_meshId == meshId);
        InvalidOpThrower.ThrowIf(meta.VboCount == 0 || meta.AttributeCount == 0);
        return meshId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UploadVertices<T>(ReadOnlySpan<T> data, BufferStorage storage, BufferAccess access, byte divisor = 0,
        int length = 0)
        where T : unmanaged
    {
        InvalidOpThrower.ThrowIfNot(_meshId.IsValid(), nameof(_meshId));
        if (_vboCount >= GfxLimits.MaxVboBindings)
            throw GraphicsException.LimitExceeded(nameof(GfxLimits.MaxVboBindings), GfxLimits.MaxVboBindings);

        var vboId = gfxBuffers.CreateVertexBuffer(data, divisor, 0, storage, access, length);
        gfxMeshes.AttachVertexBuffer(_meshId, vboId, _vboCount++);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UploadIndices<T>(ReadOnlySpan<T> data, BufferStorage storage, BufferAccess access, int length = 0)
        where T : unmanaged
    {
        InvalidOpThrower.ThrowIfNot(_meshId.IsValid(), nameof(_meshId));
        InvalidOpThrower.ThrowIf(_hasIbo, nameof(_hasIbo));

        var elementSize = GfxUtilsEnum.ToDrawElementSize<T>();
        if (_drawProperties.ElementSize == DrawElementSize.Invalid)
        {
            _drawProperties = _drawProperties with { ElementSize = elementSize, Kind = DrawMeshKind.Elements };
        }
        else
        {
            InvalidOpThrower.ThrowIfNot(_drawProperties.ElementSize == elementSize, nameof(elementSize));
            InvalidOpThrower.ThrowIfNot(_drawProperties.Kind == DrawMeshKind.Elements);
        }

        var iboId = gfxBuffers.CreateIndexBuffer(data, storage, access, length);
        gfxMeshes.AttachIndexBuffer(_meshId, iboId);
        _hasIbo = true;
    }

    public readonly void SetAttributeSpan(ReadOnlySpan<VertexAttribute> attributes)
    {
        ValidateSetAttribute(attributes.Length);
        gfxMeshes.SetVertexAttributes(_meshId, attributes.ToArray());
    }

    public readonly void SetAttributeSpan(VertexAttribute[] attributes)
    {
        ValidateSetAttribute(attributes.Length);
        gfxMeshes.SetVertexAttributes(_meshId, attributes.ToArray());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private readonly void ValidateSetAttribute(int attributes)
    {
        InvalidOpThrower.ThrowIfNot(_meshId.IsValid(), nameof(_meshId));
        ArgumentOutOfRangeException.ThrowIfZero(attributes);
        if (attributes >= GfxLimits.MaxVertexAttribs)
            throw GraphicsException.LimitExceeded(nameof(GfxLimits.MaxVertexAttribs), GfxLimits.MaxVertexAttribs);
    }
}