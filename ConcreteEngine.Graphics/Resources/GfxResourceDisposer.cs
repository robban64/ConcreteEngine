namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceDisposer
{
    public int PendingCount { get; }
    public void EnqueueRemoval<TId>(TId id, bool replace) where TId : unmanaged, IResourceId;
}



internal sealed class GfxResourceDisposer : IGfxResourceDisposer
{
    private const int DrainPerFrame = 4;
    private const int DrainDelayTicks = 16;

    private readonly GfxResourceManager _resources;
    private readonly GfxResourceRepository _repository;

    private readonly ResourceDisposeQueue _disposeQueue;
    public int PendingCount => _disposeQueue.PendingCount;

    internal GfxResourceDisposer(GfxResourceManager resources, GfxResourceRepository repository)
    {
        _resources = resources;
        _repository = repository;
        _disposeQueue = new ResourceDisposeQueue();
    }

    public void DrainDisposeQueue(IGraphicsDriver driver)
    {
        int drainCount = 0;
        while (drainCount < DrainPerFrame && _disposeQueue.TryGetNext(DrainDelayTicks, out var cmd))
        {
            driver.Disposer.DeleteGfxResource(in cmd);
            drainCount++;
        }
    }

    public void EnqueueRemoval<TId>(TId id, bool replace) where TId : unmanaged, IResourceId
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0);
        var resourceKind = HandleUtils.ToResourceKind<TId>();
        var fStore = _resources.FrontendStoreHub.GetStore<TId>(resourceKind);
        var gfxHandle = fStore.GetHandle(id);

        var bStore = _resources.BackendStoreHub.Get(resourceKind);
        var native = bStore.GetNative(in gfxHandle);
        
        var cmd = new DeleteCmd(gfxHandle, native, id.Value, 0, replace);
        _disposeQueue.Enqueue(cmd);
    }
    
}