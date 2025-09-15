using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceDisposer
{
    public int PendingCount { get; }
    public void EnqueueRemoval<TId>(TId id, bool replace) where TId : unmanaged, IResourceId;
    public void EnqueueRemovalReplace<TId>(TId id, uint newHandle) where TId : unmanaged, IResourceId;
    void DrainDisposeQueue();
}



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
        _disposeQueue.Drain(OnDeleteGfxResource, DrainPerFrame, DrainDelayTicks);
    }
    
   private void OnDeleteGfxResource(DeleteCmd cmd)
    {
        /*
        if (cmd.Replace)
        {
            _resources.DispatchReplace(cmd);
        }
*/
        _backend.DeleteGfxResource(in cmd);
    }

    public void EnqueueRemoval<TId>(TId id, bool replace) where TId : unmanaged, IResourceId
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Id, 0);
        var resourceKind = ResourceTypeConverter.FromId<TId>();
        var fs = _resources.FrontendStoreHub.GetStore<TId>(resourceKind);
        var gfxHandle = fs.GetHandle(id);

        var bs = _resources.BackendStoreHub.GetStore(resourceKind);
        var rawHandle = bs.GetRawHandle(in gfxHandle);
        
        var cmd = new DeleteCmd(gfxHandle, id.Id, rawHandle, 0, replace);
        _disposeQueue.Enqueue(cmd);

    }
    
    public void EnqueueRemovalReplace<TId>(TId id, uint newHandle) where TId : unmanaged, IResourceId
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Id, 0);
        var resourceKind = ResourceTypeConverter.FromId<TId>();
        var fs = _resources.FrontendStoreHub.GetStore<TId>(resourceKind);
        var handle = fs.GetHandle(id);
        var cmd = new DeleteCmd(handle, id.Id, newHandle, 0, true);
        _disposeQueue.Enqueue(cmd);
    }

}