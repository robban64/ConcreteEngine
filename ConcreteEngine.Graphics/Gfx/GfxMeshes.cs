using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxMeshes
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;

    private readonly IGraphicsDriver _driver;
    
    private readonly GfxBuffers _buffers;

    internal GfxMeshes(GfxContext context, GfxBuffers buffers)
    {
        _driver = context.Driver;
        _buffers = buffers;
        _resources = context.Stores;
        _repository = context.Repositories;
    }

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
            var vbo = _resources.VboStore.GetHandle(vboId);
            driverMesh.AttachVertexBuffer(in meshRef.Handle, in vbo, i, 0, desc.VertexSize);
        }

        if (payload is MeshPayloadIndexed payloadIndexed)
        {
            var iboPayload = payloadIndexed.IndexBuffer;
            var (data, desc) = (iboPayload.Data, iboPayload.Descriptor);
            var iboId = _buffers.CreateIndexBuffer(data.Span, desc.Storage, desc.Access);
            var ibo = _resources.IboStore.GetHandle(iboId);
            driverMesh.AttachIndexBuffer(in meshRef.Handle, in ibo);
        }
        
        SetVertexAttributes(meshRef, payload.Attributes);

        return meshId;
    }

    public MeshId CreateEmptyMesh()
    {
        var vaoRef = _driver.Meshes.CreateVertexArray();
        return _resources.MeshStore.Add(default, vaoRef);
    }

    private void SetVertexAttributes(GfxRefToken<MeshId> meshRef, IReadOnlyList<VertexAttributeDesc> attributes)
    {
        for (int i = 0; i < attributes.Count; i++)
        {
            var attrib = attributes[i];
            _driver.Meshes.SetVertexAttribute(meshRef, i, in attrib);
        }
    }


    internal sealed class GfxMeshesBackend(IGraphicsDriver _driver)
    {
    }
}