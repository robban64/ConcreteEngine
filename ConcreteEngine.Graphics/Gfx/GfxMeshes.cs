#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxMeshes
{
    private readonly IGraphicsDriver _driver;

    private readonly GfxBuffers _buffers;

    private readonly MeshStore _meshStore;
    private readonly VboStore _vboStore;
    private readonly IboStore _iboStore;

    private GfxMeshBuilder? _meshBuilder;

    internal GfxMeshes(GfxContextInternal context, GfxBuffers buffers)
    {
        _driver = context.Driver;
        _buffers = buffers;
        _meshStore = context.Resources.GfxStoreHub.MeshStore;
        _vboStore = context.Resources.GfxStoreHub.VboStore;
        _iboStore = context.Resources.GfxStoreHub.IboStore;
    }

    public IGfxMeshBuilder StartUploadBuilder(in MeshDrawProperties props)
    {
        InvalidOpThrower.ThrowIfNotNull(_meshBuilder, nameof(_meshBuilder), "MeshBuilder is active");
        return _meshBuilder = new GfxMeshBuilder(this, _buffers, in props);
    }
    
    public void CloseBuilder() => _meshBuilder = null;

    internal MeshId FinishUploadCommit(GfxMeshBuilder.State state, out MeshMeta meta)
    {
        state.ValidateState();

        var meshId = state.MeshId;
        // InvalidOpThrower.ThrowIf(_repository.MeshRepository.HasRecord(meshId), nameof(meshId));

        var props = state.DrawProperties;
        if (!state.IboId.IsValid())
        {
            props = props with { Kind = DrawMeshKind.Arrays, ElementSize = DrawElementSize.Invalid };
        }

        var attach = new VboAttachment
        {
            V0 = state.VboIds[0],
            V1 = state.VboIds.Count > 1 ? state.VboIds[1] : default,
            V2 = state.VboIds.Count > 2 ? state.VboIds[2] : default,
            V3 = state.VboIds.Count > 3 ? state.VboIds[3] : default
        };
         meta = MeshDrawProperties.ToMeta(in props, state.IboId, (byte)state.VboIds.Count, in attach);
        _meshStore.ReplaceMeta(meshId, in meta, out var _);
        return meshId;
    }


    public MeshId CreateEmptyMesh()
    {
        var vaoRef = _driver.Meshes.CreateVertexArray();
        return _meshStore.Add(default, vaoRef);
    }

    public void AttachVertexBuffer(MeshId meshId, VertexBufferId vboId, int binding)
    {
        var meshRef = _meshStore.GetRefHandle(meshId);
        var vboRef = _vboStore.GetRefAndMeta(vboId, out var vboMeta);
        _driver.Meshes.AttachVertexBuffer(meshRef, binding, vboRef, in vboMeta);
    }

    public void AttachIndexBuffer(MeshId meshId, IndexBufferId iboId)
    {
        var meshRef = _meshStore.GetRefHandle(meshId);
        var iboRef = _iboStore.GetRefHandle(iboId);
        _driver.Meshes.AttachIndexBuffer(meshRef, iboRef);
    }

    public void SetVertexAttributes(MeshId meshId, IReadOnlyList<VertexAttribute> attributes)
    {
        var meshRef = _meshStore.GetRefHandle(meshId);
        _driver.Meshes.AddVertexAttributeRange(meshRef, attributes);
    }

    public void SetVertexAttributesFromSpan(MeshId meshId, ReadOnlySpan<VertexAttribute> attributes)
    {
        var meshRef = _meshStore.GetRefHandle(meshId);
        _driver.Meshes.AddVertexAttributeFromSpan(meshRef, attributes);
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