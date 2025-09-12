using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;


public interface IGfxResourceDisposer
{
    public int PendingCount { get; }
    void EnqueueRemoval<TId>(TId id, bool replace = false) where TId : unmanaged, IResourceId;
    void DrainDisposeQueue();

}

internal sealed class GfxResourceDisposer : IGfxResourceDisposer
{
    private const int DrainPerFrame = 4;
    private const int DrainDelayTicks = 16;
    
    private readonly GfxResourceManager _resources;
    private readonly GfxResourceRegistry _registry;
    private readonly IDisposableBackend _backend;
    
    private readonly ResourceDisposeQueue _disposeQueue;
    public int PendingCount => _disposeQueue.PendingCount;

    internal GfxResourceDisposer(GfxResourceManager resources, GfxResourceRegistry registry, IDisposableBackend backend)
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


    public void EnqueueRemoval<TId>(TId id, bool replace = false) where TId : unmanaged, IResourceId
    {
        // Enqueue dependent resources
        switch (id)
        {
            case MeshId meshId:
                var meshLayout = _registry.MeshRegistry.GetInternal(meshId);
                if (meshLayout.IndexBufferId.IsValid())
                    EnqueueRemoval(meshLayout.IndexBufferId, replace);

                var vboIds = meshLayout.GetVertexBufferIds();
                foreach (var vboId in vboIds)
                    EnqueueRemoval(vboId, replace);

                break;
            case FrameBufferId fboId:
                var fboLayout = _registry.FboRegistry.Get(fboId);
                if (fboLayout.RboDepthId.IsValid()) EnqueueRemoval(fboLayout.RboDepthId, replace);
                if (fboLayout.RboTexId.IsValid()) EnqueueRemoval(fboLayout.RboTexId, replace);
                if (fboLayout.FboTexId.IsValid()) EnqueueRemoval(fboLayout.FboTexId, replace);
                break;
            default:
                throw new GraphicsException($"Unknown resource type {typeof(TId).Name}");
        }
        
        // 
        switch (id)
        {
            case TextureId textureId:
                var handleTex = _resources.TextureStore.GetHandle(textureId);
                _disposeQueue.Enqueue(handleTex);
                if (!replace) _resources.TextureStore.Remove(textureId, out _);
                break;
            case ShaderId shaderId:
                var handleShader = _resources.ShaderStore.GetHandle(shaderId);
                _disposeQueue.Enqueue(handleShader);
                if (!replace) _resources.ShaderStore.Remove(shaderId, out _);
                break;
            case MeshId meshId:
                var handleMesh = _resources.MeshStore.GetHandle(meshId);
                _disposeQueue.Enqueue(handleMesh);
                if (!replace) _resources.MeshStore.Remove(meshId, out _);
                break;
            case VertexBufferId vboId:
                var handleVbo = _resources.VboStore.GetHandle(vboId);
                _disposeQueue.Enqueue(handleVbo);
                if (!replace) _resources.VboStore.Remove(vboId, out _);
                break;
            case IndexBufferId iboId:
                var handleIbo = _resources.IboStore.GetHandle(iboId);
                _disposeQueue.Enqueue(handleIbo);
                if (!replace) _resources.IboStore.Remove(iboId, out _);
                break;
            case FrameBufferId fboId:
                ref readonly var fbo = ref _resources.FboStore.GetMeta(fboId);
                var fboHandle = _resources.FboStore.GetHandle(fboId);
                _disposeQueue.Enqueue(fboHandle);
                if (!replace) _resources.FboStore.Remove(fboId, out _);
                break;
            case RenderBufferId rboId:
                var rboHandle = _resources.RboStore.GetHandle(rboId);
                _disposeQueue.Enqueue(rboHandle);
                if (!replace) _resources.RboStore.Remove(rboId, out _);
                break;
            default:
                throw new GraphicsException($"Unknown resource type {typeof(TId).Name}");
        }
    }
}