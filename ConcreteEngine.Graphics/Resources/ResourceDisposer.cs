using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class ResourceDisposer
{
    private readonly IResourceManager _resources;
    private readonly IResourceRegistry _registry;
    private readonly ResourceDisposeQueue _disposeQueue;

    internal ResourceDisposer(IResourceManager resources, IResourceRegistry registry)
    {
        _resources = resources;
        _registry = registry;
        _disposeQueue = new ResourceDisposeQueue();
    }

    void EnqueueRemoveResource<TId>(TId id, bool replace = false) where TId : unmanaged, IResourceId
    {
        switch (id)
        {
            case MeshId meshId:
                var meshLayout = _registry.MeshRegistry.GetInternal(meshId);
                if (meshLayout.IndexBufferId.IsValid())
                    EnqueueRemoveResource(meshLayout.IndexBufferId, replace);

                var vboIds = meshLayout.GetVertexBufferIds();
                foreach (var vboId in vboIds)
                    EnqueueRemoveResource(vboId, replace);

                break;
            case FrameBufferId fboId:
                ref readonly var fbo = ref _resources.FboStore.GetMeta(fboId);
                var fboLayout = _registry.FboRegistry.Get(fboId);
                if (fboLayout.RboDepthId.Id > 0) EnqueueRemoveResource(fboLayout.RboDepthId, replace);
                if (fboLayout.RboTexId.Id > 0) EnqueueRemoveResource(fboLayout.RboTexId, replace);
                if (fboLayout.TexColorId.Id > 0) EnqueueRemoveResource(fboLayout.TexColorId, replace);
                break;
            default:
                throw new GraphicsException($"Unknown resource type {typeof(TId).Name}");
        }


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