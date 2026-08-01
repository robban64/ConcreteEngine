using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxMeshes
{
    public static MeshId FsqQuad { get; private set; }
    public static MeshId SkyboxCube { get; private set; }
    public static MeshId Cube { get; private set; }
    public static MeshId Sphere { get; private set; }

    //
    private readonly GfxBuffers _buffers;
    
    private GfxMeshEntry[] _meshAttributes;

    internal GfxMeshes(GfxBuffers buffers)
    {
        _buffers = buffers;
        _meshAttributes = new GfxMeshEntry[int.Max(64, GfxRegistry.MeshStore.Capacity)];
        CreatePrimitives(this);
    }

    public GfxMeshEntry GetMeshDetails(MeshId meshId, out MeshMeta meta)
    {
        meta = GfxRegistry.MeshStore.GetMeta(meshId);
        return _meshAttributes[meshId.Index()];
    }
    
    public MeshId CreateEmptyMesh(in MeshDrawProperties props, int vboCount, VertexAttributeDef[] attrib)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vboCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vboCount, GfxLimits.MaxVboBindings);
        ArgumentOutOfRangeException.ThrowIfZero(attrib.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(attrib.Length, GfxLimits.MaxVertexAttribs);

        var meshRef = GlMeshes.CreateVertexArray();
        GlMeshes.AddVertexAttributes(meshRef, attrib);

        var meta = new MeshMeta
        {
            Kind = props.Kind,
            ElementSize = props.ElementSize,
            Primitive = props.Primitive,
            DrawCount = props.DrawCount,
            InstanceCount = props.InstanceCount
        };

        var meshId = GfxRegistry.MeshStore.Add(in meta, meshRef);
        if(GfxRegistry.MeshStore.Capacity != _meshAttributes.Length) 
            Array.Resize(ref _meshAttributes, GfxRegistry.MeshStore.Capacity);
        
        _meshAttributes[meshId.Index()] = new GfxMeshEntry(vboCount, attrib);
        return meshId;
    }

    public VertexBufferId CreateAttachVertexBuffer<T>(MeshId meshId, NativeView<T> data, CreateVboArgs args)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Id);
        var offset = (uint)args.Offset;
        var vbo = _buffers.CreateVertexBuffer(data, args.Divisor, offset, args.Storage, args.Access, args.Length);
        AttachVertexBuffer(meshId, vbo, args.Binding);
        return vbo;
    }

    public IndexBufferId CreateAttachIndexBuffer<T>(MeshId meshId, NativeView<T> data, CreateIboArgs args)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Id);
        var ibo = _buffers.CreateIndexBuffer(data, args.Storage, args.Access, args.Length);
        AttachIndexBuffer(meshId, ibo);
        return ibo;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AttachVertexBuffer(MeshId meshId, VertexBufferId vboId, int binding)
    {
        var meshView = GfxRegistry.MeshStore.GetHandleAndMeta(meshId, out var meta);
        var vboRef = GfxRegistry.VboStore.GetHandleAndMeta(vboId, out var vboMeta);
        GlMeshes.AttachVertexBuffer(meshView, binding, vboRef, in vboMeta);
        _meshAttributes[meshId.Index()].VboIds[binding] = vboId;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AttachIndexBuffer(MeshId meshId, IndexBufferId iboId)
    {
        var meshRef = GfxRegistry.MeshStore.GetHandleAndMeta(meshId, out var meta);
        var iboRef = GfxRegistry.IboStore.GetHandleAndMeta(iboId, out var iboMeta);
        GlMeshes.AttachIndexBuffer(meshRef, iboRef);

        var elementSize = GfxEnumUtils.ToDrawElementSize(iboMeta.Stride);
        GfxRegistry.MeshStore.ReplaceMeta(meshId, meta with { ElementSize = elementSize }, out _);
        _meshAttributes[meshId.Index()].IboId = iboId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreatePrimitives(GfxMeshes meshes)
    {
        FsqQuad = PrimitiveMeshBuilder.GenerateFsqQuad(meshes);
        SkyboxCube = PrimitiveMeshBuilder.GenerateSkyboxCube(meshes);
        Cube = PrimitiveMeshBuilder.GenerateCube(meshes);
        Sphere = PrimitiveMeshBuilder.GenerateSphere(meshes, 1f, 18, 36);
    }
}