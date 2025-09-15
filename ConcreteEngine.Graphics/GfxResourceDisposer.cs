using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceDisposer
{
    public int PendingCount { get; }
    void EnqueueRemoval<TId>(TId id, bool replace) where TId : unmanaged, IResourceId;
    void DrainDisposeQueue();
}

internal readonly record struct DeleteCmd(
    in GfxHandle Handle,
    int IdValue,
    uint NewHandle,
    ushort Priority,
    bool Replace
);

internal sealed class GfxResourceDisposer : IGfxResourceDisposer
{
    private const int DrainPerFrame = 4;
    private const int DrainDelayTicks = 16;

    private readonly GfxResourceManager _resources;
    private readonly GfxResourceRegistry _registry;
    private readonly IDeleteResourceBackend _backend;

    private readonly ResourceDisposeQueue _disposeQueue;
    public int PendingCount => _disposeQueue.PendingCount;

    internal GfxResourceDisposer(GfxResourceManager resources, GfxResourceRegistry registry,
        IDeleteResourceBackend backend)
    {
        _resources = resources;
        _registry = registry;
        _backend = backend;
        _disposeQueue = new ResourceDisposeQueue();
    }

    public void DrainDisposeQueue()
    {
        _disposeQueue.Drain(_backend.DeleteGfxResource, DrainPerFrame, DrainDelayTicks);
    }

    public static ResourceKind FromId<TId>()
        where TId : struct, IResourceId =>
        typeof(TId) switch
        {
            var t when t == typeof(TextureId) => ResourceKind.Texture,
            var t when t == typeof(ShaderId) => ResourceKind.Shader,
            var t when t == typeof(MeshId) => ResourceKind.Mesh,
            var t when t == typeof(VertexBufferId) => ResourceKind.VertexBuffer,
            var t when t == typeof(IndexBufferId) => ResourceKind.IndexBuffer,
            var t when t == typeof(FrameBufferId) => ResourceKind.FrameBuffer,
            var t when t == typeof(RenderBufferId) => ResourceKind.RenderBuffer,
            var t when t == typeof(UniformBufferId) => ResourceKind.UniformBuffer,
            _ => ResourceKind.Invalid
        };


    public void EnqueueRemoval<TId>(TId id) where TId : unmanaged, IResourceId
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Id, 0);
        var resourceKind = FromId<TId>();
        var fs = _resources.FrontendStoreHub.GetStore<TId>(resourceKind);
        var handle = fs.GetHandle(id);
        var cmd = new DeleteCmd(handle, id.Id, 0, 0, false);
        _disposeQueue.Enqueue(cmd);
    }
    
    public void EnqueueRemovalReplace<TId, TMeta>(TId id, in TMeta meta, uint newHandle)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Id, 0);
        var resourceKind = FromId<TId>();
        var fs = _resources.FrontendStoreHub.GetStore<TId, TMeta>(resourceKind);
        var handle = fs.GetHandle(id);
        var cmd = new DeleteCmd(handle, id.Id, newHandle, 0, false);
        _disposeQueue.Enqueue(cmd);
    }

    public void EnqueueRemoval<TId>(TId id, bool replace) where TId : unmanaged, IResourceId
    {
        switch (id)
        {
            case MeshId meshId:
                var meshLayout = _registry.MeshRepository.Get(meshId);
                if (meshLayout.IndexBufferId.IsValid())
                    EnqueueRemoval(meshLayout.IndexBufferId, replace);

                var vboIds = meshLayout.GetVertexBufferIds();
                foreach (var vboId in vboIds)
                    EnqueueRemoval(vboId, replace);

                break;
            case FrameBufferId fboId:
                var fboLayout = _registry.FboRepository.Get(fboId);
                var fboRes = fboLayout.AttachedFboResources;
                if (fboRes.RboDepthId.IsValid()) EnqueueRemoval(fboRes.RboDepthId, replace);
                if (fboRes.RboTexId.IsValid()) EnqueueRemoval(fboRes.RboTexId, replace);
                if (fboRes.FboTexId.IsValid()) EnqueueRemoval(fboRes.FboTexId, replace);
                break;
        }

        // 
        switch (id)
        {
            case TextureId textureId:
                _disposeQueue.Enqueue(handle, id.Id, replace);
                if (!replace) _resources.TextureStore.Remove(textureId, out _);
                break;
/*
            case TextureId textureId:
                ref readonly var handleTex = ref _resources.TextureStore.GetHandle(textureId);
                _disposeQueue.Enqueue(handleTex, textureId.Id, replace);
                if (!replace) _resources.TextureStore.Remove(textureId, out _);
                break;*/
            case ShaderId shaderId:
                ref readonly var handleShader = ref _resources.ShaderStore.GetHandle(shaderId);
                _disposeQueue.Enqueue(handleShader, shaderId.Id, replace);
                if (!replace) _resources.ShaderStore.Remove(shaderId, out _);
                break;
            case MeshId meshId:
                ref readonly var handleMesh = ref _resources.MeshStore.GetHandle(meshId);
                _disposeQueue.Enqueue(handleMesh, meshId.Id, replace);
                if (!replace) _resources.MeshStore.Remove(meshId, out _);
                break;
            case VertexBufferId vboId:
                ref readonly var handleVbo = ref _resources.VboStore.GetHandle(vboId);
                _disposeQueue.Enqueue(handleVbo, vboId.Id, replace);
                if (!replace) _resources.VboStore.Remove(vboId, out _);
                break;
            case IndexBufferId iboId:
                ref readonly var handleIbo = ref _resources.IboStore.GetHandle(iboId);
                _disposeQueue.Enqueue(handleIbo, iboId.Id, replace);
                if (!replace) _resources.IboStore.Remove(iboId, out _);
                break;
            case FrameBufferId fboId:
                ref readonly var fboHandle = ref _resources.FboStore.GetHandle(fboId);
                _disposeQueue.Enqueue(fboHandle, 0, replace);
                if (!replace) _resources.FboStore.Remove(fboId, out _);
                break;
            case RenderBufferId rboId:
                ref readonly var rboHandle = ref _resources.RboStore.GetHandle(rboId);
                _disposeQueue.Enqueue(rboHandle, rboId.Id, replace);
                if (!replace) _resources.RboStore.Remove(rboId, out _);
                break;
            default:
                throw new GraphicsException($"Unknown resource type {typeof(TId).Name}");
        }
    }
}