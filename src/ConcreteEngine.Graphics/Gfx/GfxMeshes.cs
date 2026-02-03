using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxMeshes
{
    private readonly GlBackendDriver _driver;

    private readonly GfxBuffers _buffers;

    private readonly MeshStore _meshStore;
    private readonly VboStore _vboStore;
    private readonly IboStore _iboStore;

    private readonly Dictionary<MeshId, MeshLayout> _meshAttributes = new(64);

    internal GfxMeshes(GfxContextInternal context, GfxBuffers buffers)
    {
        _driver = context.Driver;
        _buffers = buffers;
        _meshStore = context.Resources.GfxStoreHub.MeshStore;
        _vboStore = context.Resources.GfxStoreHub.VboStore;
        _iboStore = context.Resources.GfxStoreHub.IboStore;
    }
    
    public MeshLayout GetMeshDetails(MeshId meshId, out MeshMeta meta)
    {
        meta = _meshStore.GetMeta(meshId);
        return _meshAttributes[meshId];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureMeshCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _meshStore.EnsureCapacity(count);
        _vboStore.EnsureCapacity(count);
        _iboStore.EnsureCapacity(count);
        _meshAttributes.EnsureCapacity(count);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public MeshId CreateEmptyMesh(in MeshDrawProperties props, int vboCount, ReadOnlySpan<VertexAttribute> attrib)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vboCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vboCount, GfxLimits.MaxVboBindings);
        ArgumentOutOfRangeException.ThrowIfZero(attrib.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(attrib.Length, GfxLimits.MaxVertexAttribs);

        var meshRef = _driver.Meshes.CreateVertexArray();
        _driver.Meshes.AddVertexAttributeFromSpan(meshRef, attrib);

        var meta = new MeshMeta
        {
            VboCount = (byte)vboCount,
            AttributeCount = attrib.Length,
            Kind = props.Kind,
            ElementSize = props.ElementSize,
            Primitive = props.Primitive,
            DrawCount = props.DrawCount,
            InstanceCount = props.InstanceCount
        };

        var meshId = _meshStore.Add(in meta, meshRef);
        _meshAttributes.Add(meshId, new MeshLayout(meshId, vboCount, attrib.ToArray()));
        return meshId;
    }

    public void CreateAttachVertexBuffer<T>(MeshId meshId, ReadOnlySpan<T> data, CreateVboArgs args)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshId.Value);
        var offset = (uint)args.Offset;
        var vbo = _buffers.CreateVertexBuffer(data, args.Divisor, offset, args.Storage, args.Access, args.Length);
        AttachVertexBuffer(meshId, vbo, args.Binding);
    }

    public void CreateAttachIndexBuffer<T>(MeshId meshId, ReadOnlySpan<T> data, CreateIboArgs args)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(meshId.Value);
        var ibo = _buffers.CreateIndexBuffer(data, args.Storage, args.Access, args.Length);
        AttachIndexBuffer(meshId, ibo);
    }


    public void AttachVertexBuffer(MeshId meshId, VertexBufferId vboId, int binding)
    {
        var meshView = _meshStore.GetHandleAndMeta(meshId, out var meta);
        var vboRef = _vboStore.GetHandleAndMeta(vboId, out var vboMeta);
        _driver.Meshes.AttachVertexBuffer(meshView, binding, vboRef, in vboMeta);

        var newMeta = meta with { VboCount = (byte)(meta.VboCount + 1) };
        _meshStore.ReplaceMeta(meshId, in newMeta, out _);
        _meshAttributes[meshId].VboIds[binding] = vboId;
    }

    public void AttachIndexBuffer(MeshId meshId, IndexBufferId iboId)
    {
        var meshRef = _meshStore.GetHandleAndMeta(meshId, out var meta);
        var iboRef = _iboStore.GetHandleAndMeta(iboId, out var iboMeta);
        _driver.Meshes.AttachIndexBuffer(meshRef, iboRef);

        var elementSize = GfxUtilsEnum.ToDrawElementSize(iboMeta.Stride);
        _meshStore.ReplaceMeta(meshId, meta with { ElementSize = elementSize }, out _);

        _meshAttributes[meshId].IboId = iboId;
    }
}
