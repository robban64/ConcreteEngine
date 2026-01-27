using ConcreteEngine.Core.Common;
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

    private MeshBuilder? _meshBuilder;

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
        var meshRef = _meshStore.GetRefAndMeta(meshId, out meta);
        return _meshAttributes[meshId];
    }

    public GfxMeshBuilder StartUploadBuilder(in MeshDrawProperties props)
    {
        InvalidOpThrower.ThrowIfNotNull(_meshBuilder, nameof(_meshBuilder), "MeshBuilder is active");
        return _meshBuilder = new MeshBuilder(this, _buffers, in props);
    }

    public MeshId FinishUploadBuilder(out MeshMeta meta)
    {
        InvalidOpThrower.ThrowIfNull(_meshBuilder, nameof(_meshBuilder), "MeshBuilder is not active");
        var result = _meshBuilder!.Finish();
        var meshId = result.MeshId;

        meta = _meshStore.GetMeta(meshId);
        _meshAttributes.Add(meshId, result);
        _meshBuilder = null;
        return meshId;
    }

    public MeshId CreateEmptyMesh(in MeshDrawProperties props)
    {
        var vaoRef = _driver.Meshes.CreateVertexArray();
        var meta = new MeshMeta
        {
            Kind = props.Kind,
            ElementSize = props.ElementSize,
            Primitive = props.Primitive,
            DrawCount = props.DrawCount,
            InstanceCount = props.InstanceCount
        };
        return _meshStore.Add(in meta, vaoRef);
    }

    public void AttachVertexBuffer(MeshId meshId, VertexBufferId vboId, int binding)
    {
        var meshView = _meshStore.GetRefAndMeta(meshId, out var meta);
        var vboRef = _vboStore.GetRefAndMeta(vboId, out var vboMeta);
        _driver.Meshes.AttachVertexBuffer(meshView, binding, vboRef, in vboMeta);
        var newMeta = meta with { VboCount = (byte)(meta.VboCount + 1) };
        _meshStore.ReplaceMeta(meshId, in newMeta, out _);
    }

    public void AttachIndexBuffer(MeshId meshId, IndexBufferId iboId)
    {
        var meshRef = _meshStore.GetRefAndMeta(meshId, out var meta);
        var iboRef = _iboStore.GetRefAndMeta(iboId, out var iboMeta);
        _driver.Meshes.AttachIndexBuffer(meshRef, iboRef);

        var elementSize = GfxUtilsEnum.ToDrawElementSize(iboMeta.Stride);
        _meshStore.ReplaceMeta(meshId, meta with { ElementSize = elementSize }, out _);
    }

    public void SetVertexAttributes(MeshId meshId, IReadOnlyList<VertexAttribute> attributes)
    {
        var meshRef = _meshStore.GetRefAndMeta(meshId, out var meta);
        _driver.Meshes.AddVertexAttributeRange(meshRef, attributes);
        _meshStore.ReplaceMeta(meshId, meta with { AttributeCount = attributes.Count }, out _);
    }

    public void SetVertexAttributesFromSpan(MeshId meshId, ReadOnlySpan<VertexAttribute> attributes)
    {
        var meshRef = _meshStore.GetRefAndMeta(meshId, out var meta);
        _driver.Meshes.AddVertexAttributeFromSpan(meshRef, attributes);
        _meshStore.ReplaceMeta(meshId, meta with { AttributeCount = attributes.Length }, out _);
    }
}

/*
  public MeshId CreateMesh(IMeshPayload payload)
     {
         var driverMesh = _driver.Meshes;
         var drawProp = payload.DrawProperties;
         var meta = new MeshMeta(drawProp.Primitive, drawProp.DrawKind,
             drawProp.ElementSize, payload.Attributes.Count, drawProp.DrawCount);

         var meshRef = driverMesh.CreateVertexArray();
         var meshId = _meshStore.Add(in meta, meshRef);

         var vboIds = new VertexBufferId[payload.VertexBuffers.Count];
         for (int i = 0; i < payload.VertexBuffers.Count; i++)
         {
             var vboPayload = payload.VertexBuffers[i];
             var (data, desc) = (vboPayload.Data, vboPayload.Descriptor);
             var vboId = vboIds[i] = _buffers.CreateVertexBuffer(data.Span, i, desc.Storage, desc.Access);
             var vbo = _vboStore.GetRef(vboId);
             driverMesh.AttachVertexBuffer(in meshRef, in vbo, i, 0, desc.VertexSize);
         }

         if (payload is MeshPayloadIndexed payloadIndexed)
         {
             var iboPayload = payloadIndexed.IndexBuffer;
             var (data, desc) = (iboPayload.Data, iboPayload.Descriptor);
             var iboId = _buffers.CreateIndexBuffer(data.Span, desc.Storage, desc.Access);
             var ibo = _iboStore.GetRef(iboId);
             driverMesh.AttachIndexBuffer(in meshRef, in ibo);
         }

         SetVertexAttributes(meshId, payload.Attributes);

         return meshId;
     }

     public MeshId CreateExistingMesh(IMeshPayload payload)
     {
         var driverMesh = _driver.Meshes;
         var drawProp = payload.DrawProperties;
         var meta = new MeshMeta(drawProp.Primitive, drawProp.DrawKind,
             drawProp.ElementSize, payload.Attributes.Count, drawProp.DrawCount);

         var meshRef = driverMesh.CreateVertexArray();
         var meshId = _meshStore.Add(in meta, meshRef);

         var vboIds = new VertexBufferId[payload.VertexBuffers.Count];
         for (int i = 0; i < payload.VertexBuffers.Count; i++)
         {
             var vboPayload = payload.VertexBuffers[i];
             var (data, desc) = (vboPayload.Data, vboPayload.Descriptor);
             var vboId = vboIds[i] = _buffers.CreateVertexBuffer(data.Span, i, desc.Storage, desc.Access);
             var vbo = _vboStore.GetRef(vboId);
             driverMesh.AttachVertexBuffer(in meshRef, in vbo, i, 0, desc.VertexSize);
         }

         if (payload is MeshPayloadIndexed payloadIndexed)
         {
             var iboPayload = payloadIndexed.IndexBuffer;
             var (data, desc) = (iboPayload.Data, iboPayload.Descriptor);
             var iboId = _buffers.CreateIndexBuffer(data.Span, desc.Storage, desc.Access);
             var ibo = _iboStore.GetRef(iboId);
             driverMesh.AttachIndexBuffer(in meshRef, in ibo);
         }

         SetVertexAttributes(meshId, payload.Attributes);

         return meshId;
     }
     */