#region

using ConcreteEngine.Common;

#endregion

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
        var resourceKind = TId.Kind;
        var fStore = _resources.GfxStoreHub.GetStore<TId>();
        var gfxHandle = fStore.GetHandle(id);

        var bStore = _resources.BackendStoreHub.Get(resourceKind);
        var native = bStore.GetNative(in gfxHandle);

        var cmd = new DeleteCmd(gfxHandle, native, id.Value, 0, replace);
        _disposeQueue.Enqueue(cmd);
    }


    private sealed class ResourceDisposeQueue
    {
        private readonly Queue<DeleteCmd> _disposeQueue = new(8);
        private readonly HashSet<int> _disposeSet = new(8);

        public int PendingCount => _disposeQueue.Count;

        private bool _isDisposing = false;

        private int _ticks;

        public void Enqueue(in DeleteCmd cmd)
        {
            // Kept for now to catch bugs
            InvalidOpThrower.ThrowIf(_isDisposing);
            InvalidOpThrower.ThrowIfNot(_disposeSet.Add(cmd.IdValue));

            _disposeQueue.Enqueue(cmd);
        }


        public bool TryGetNext(int delayTicks, out DeleteCmd cmd)
        {
            cmd = default;

            if (_disposeQueue.Count == 0)
            {
                _ticks = 0;
                return false;
            }

            if (++_ticks < delayTicks)
                return false;

            _isDisposing = true;

            cmd = _disposeQueue.Dequeue();

            if (_disposeQueue.Count == 0)
            {
                _isDisposing = false;
                _disposeSet.Clear();
            }

            _ticks = 0;
            return true;
        }
    }
}