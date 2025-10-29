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

    public MeshId FinishUploadBuilder(out MeshMeta meta)
    {
        InvalidOpThrower.ThrowIfNull(_meshBuilder, nameof(_meshBuilder), "MeshBuilder is not active");
        var state = _meshBuilder!.Finish();
        var meshId = state.MeshId;
        var props = state.DrawProperties;
        if (!state.IboId.IsValid())
            props = props with { Kind = DrawMeshKind.Arrays, ElementSize = DrawElementSize.Invalid };

        ref readonly var prevMeta = ref _meshStore.GetMeta(meshId);
        meta = prevMeta with
        {
            Primitive = props.Primitive, Kind = props.Kind, ElementSize = props.ElementSize,
            DrawCount = props.DrawCount
        };
        _meshStore.ReplaceMeta(meshId, in meta, out _);
        _meshBuilder = null;
        return meshId;
    }

    public MeshId CreateEmptyMesh()
    {
        var vaoRef = _driver.Meshes.CreateVertexArray();
        return _meshStore.Add(default, vaoRef);
    }

    public void AttachVertexBuffer(MeshId meshId, VertexBufferId vboId, int binding)
    {
        var meshRef = _meshStore.GetRefAndMeta(meshId, out var meta);
        var vboRef = _vboStore.GetRefAndMeta(vboId, out var vboMeta);
        _driver.Meshes.AttachVertexBuffer(meshRef, binding, vboRef, in vboMeta);

        var attachments = meta.VboAttachment;
        attachments = binding switch
        {
            0 => attachments with { V0 = vboId },
            1 => attachments with { V1 = vboId },
            2 => attachments with { V2 = vboId },
            3 => attachments with { V3 = vboId },
            _ => attachments
        };
        var newMeta = meta with
        {
            VboCount = (byte)(meta.VboCount + 1),
            VboAttachment = attachments
        };
        _meshStore.ReplaceMeta(meshId, in newMeta, out _);
    }

    public void AttachIndexBuffer(MeshId meshId, IndexBufferId iboId)
    {
        var meshRef = _meshStore.GetRefAndMeta(meshId, out var meta);
        var iboRef = _iboStore.GetRefHandle(iboId);
        _driver.Meshes.AttachIndexBuffer(meshRef, iboRef);
        
        _meshStore.ReplaceMeta(meshId, meta with{IndexBufferId = iboId}, out _);
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