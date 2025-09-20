using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxMeshes
{
    private readonly GfxStoreHub _resources;
    private readonly GfxResourceRepository _repository;

    private readonly IGraphicsDriver _driver;

    private readonly GfxBuffers _buffers;

    private readonly GfxMeshBuilder _meshBuilder;

    internal GfxMeshes(GfxContextInternal context, GfxBuffers buffers)
    {
        _driver = context.Driver;
        _buffers = buffers;
        _resources = context.Stores;
        _repository = context.Repositories;
        _meshBuilder = new GfxMeshBuilder();
    }

    public IGfxMeshBuilder StartUploadBuilder(in MeshDrawProperties props) => _meshBuilder.Init(this, _buffers, props);


    internal MeshId FinishUploadCommit(GfxMeshBuilder.State state)
    {
        state.ValidateState();

        var meshId = state.MeshId;
        InvalidOpThrower.ThrowIf(_repository.MeshRepository.HasRecord(meshId), nameof(meshId));

        var props = state.DrawProperties;
        if (!state.IboId.IsValid())
        {
            props = props with { DrawKind = MeshDrawKind.Arrays, ElementSize = DrawElementSize.Invalid };
        }

        var record = new MeshRepository.MeshLayout(meshId)
        {
            IndexBufferId = state.IboId,
            VertexBufferIds = state.VboIds.ToArray(),
            Attributes = state.Attributes.ToArray(),
            Properties = props,
        };
        _repository.MeshRepository.AddRecord(meshId, record);
        return meshId;
    }


    public MeshId CreateEmptyMesh()
    {
        var vaoRef = _driver.Meshes.CreateVertexArray();
        return _resources.MeshStore.Add(default, vaoRef);
    }

    public void AttachVertexBuffer(MeshId meshId, VertexBufferId vboId, int bindingIdx)
    {
        var meshRef = _resources.MeshStore.GetRef(meshId);
        var vboRef = _resources.VboStore.GetRefAndMeta(vboId, out var vboMeta);
        _driver.Meshes.AttachVertexBuffer(in meshRef, in vboRef, bindingIdx, 0, vboMeta.Stride);
    }

    public void AttachIndexBuffer(MeshId meshId, IndexBufferId iboId)
    {
        var meshRef = _resources.MeshStore.GetRef(meshId);
        var iboRef = _resources.IboStore.GetRef(iboId);
        _driver.Meshes.AttachIndexBuffer(in meshRef, in iboRef);
    }

    public void SetVertexAttributes(MeshId meshId, IReadOnlyList<VertexAttributeDesc> attributes)
    {
        var meshRef = _resources.MeshStore.GetRef(meshId);
        _driver.Meshes.AddVertexAttributeRange(meshRef, attributes);
    }

    public void SetVertexAttributesFromSpan(MeshId meshId, ReadOnlySpan<VertexAttributeDesc> attributes)
    {
        var meshRef = _resources.MeshStore.GetRef(meshId);
        _driver.Meshes.AddVertexAttributeRange(meshRef, attributes);
    }

    internal sealed class GfxMeshesBackend(IGraphicsDriver _driver)
    {
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
         var meshId = _resources.MeshStore.Add(in meta, meshRef);

         var vboIds = new VertexBufferId[payload.VertexBuffers.Count];
         for (int i = 0; i < payload.VertexBuffers.Count; i++)
         {
             var vboPayload = payload.VertexBuffers[i];
             var (data, desc) = (vboPayload.Data, vboPayload.Descriptor);
             var vboId = vboIds[i] = _buffers.CreateVertexBuffer(data.Span, i, desc.Storage, desc.Access);
             var vbo = _resources.VboStore.GetRef(vboId);
             driverMesh.AttachVertexBuffer(in meshRef, in vbo, i, 0, desc.VertexSize);
         }

         if (payload is MeshPayloadIndexed payloadIndexed)
         {
             var iboPayload = payloadIndexed.IndexBuffer;
             var (data, desc) = (iboPayload.Data, iboPayload.Descriptor);
             var iboId = _buffers.CreateIndexBuffer(data.Span, desc.Storage, desc.Access);
             var ibo = _resources.IboStore.GetRef(iboId);
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
         var meshId = _resources.MeshStore.Add(in meta, meshRef);

         var vboIds = new VertexBufferId[payload.VertexBuffers.Count];
         for (int i = 0; i < payload.VertexBuffers.Count; i++)
         {
             var vboPayload = payload.VertexBuffers[i];
             var (data, desc) = (vboPayload.Data, vboPayload.Descriptor);
             var vboId = vboIds[i] = _buffers.CreateVertexBuffer(data.Span, i, desc.Storage, desc.Access);
             var vbo = _resources.VboStore.GetRef(vboId);
             driverMesh.AttachVertexBuffer(in meshRef, in vbo, i, 0, desc.VertexSize);
         }

         if (payload is MeshPayloadIndexed payloadIndexed)
         {
             var iboPayload = payloadIndexed.IndexBuffer;
             var (data, desc) = (iboPayload.Data, iboPayload.Descriptor);
             var iboId = _buffers.CreateIndexBuffer(data.Span, desc.Storage, desc.Access);
             var ibo = _resources.IboStore.GetRef(iboId);
             driverMesh.AttachIndexBuffer(in meshRef, in ibo);
         }

         SetVertexAttributes(meshId, payload.Attributes);

         return meshId;
     }
     */