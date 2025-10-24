#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Diagnostic;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

public interface IGfxResourceDisposer
{
    int PendingCount { get; }
    void EnqueueRemoval<TId>(TId id) where TId : unmanaged, IResourceId;
}

internal sealed class GfxResourceDisposer : IGfxResourceDisposer
{
    private const int DrainPerFrame = 6;
    private const int DrainDelayTicks = 2;

    private readonly BackendStoreHub _backendStoreHub;
    private readonly GfxStoreHub _gfxStoreHub;


    private readonly ResourceDisposeQueue _disposeQueue;
    public int PendingCount => _disposeQueue.PendingCount;

    internal GfxResourceDisposer(GfxResourceManager resources)
    {
        _backendStoreHub = resources.BackendStoreHub;
        _gfxStoreHub = resources.GfxStoreHub;
        _disposeQueue = new ResourceDisposeQueue();
    }

    public void DrainDisposeQueue(IGraphicsDriver driver)
    {
        int drainCount = 0;
        while (drainCount < DrainPerFrame && _disposeQueue.TryGetNext(DrainDelayTicks, out var cmd))
        {
            driver.Disposer.DeleteGlResource(in cmd);
            _backendStoreHub.GetStore(cmd.Handle.Kind).Remove(cmd.Handle);
            if (!cmd.Replace)
            {
                _gfxStoreHub.RemoveResource(cmd.GfxId, cmd.Handle.Kind);
            }

            drainCount++;
        }
    }

    public void EnqueueRemoval<TId>(TId id) where TId : unmanaged, IResourceId
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0);
        var resourceKind = TId.Kind;
        var fStore = _gfxStoreHub.GetStore<TId>();
        var gfxHandle = fStore.GetHandleUntyped(id);

        var bStore = _backendStoreHub.GetStore(resourceKind);
        var native = bStore.GetNativeHandle(in gfxHandle);

        var cmd = DeleteResourceCommand.MakeDelete(gfxHandle, native, id.Value);
        _disposeQueue.Enqueue(cmd);

        GfxDebugMetrics.Log(DebugLog.MakeEnqueueDispose(id.Value, gfxHandle));
    }

    public void EnqueueReplace<TId>(GfxRefToken<TId> refToken)
        where TId : unmanaged, IResourceId
    {
        ArgumentOutOfRangeException.ThrowIfEqual(refToken.Handle.IsValid, false);
        var fkStore = _gfxStoreHub.GetStore<TId>();

        var bkStore = _backendStoreHub.GetStore(TId.Kind);
        var handle = bkStore.GetNativeHandle(refToken);
        var cmd = DeleteResourceCommand.MakeReplace(refToken, handle);
        _disposeQueue.Enqueue(cmd);

        GfxDebugMetrics.Log(DebugLog.MakeEnqueueDispose((int)handle.Value, refToken));
    }


    private sealed class ResourceDisposeQueue
    {
        private readonly Queue<DeleteResourceCommand> _disposeQueue = new(8);

        private readonly HashSet<GfxHandle> _disposeSet = new(8);

        public int PendingCount => _disposeQueue.Count;

        private bool _isDisposing = false;

        private int _ticks;

        public void Enqueue(in DeleteResourceCommand cmd)
        {
            // Kept for now to catch bugs
            InvalidOpThrower.ThrowIf(_isDisposing);
            InvalidOpThrower.ThrowIfNot(_disposeSet.Add(cmd.Handle));

            _disposeQueue.Enqueue(cmd);
        }


        public bool TryGetNext(int delayTicks, out DeleteResourceCommand cmd)
        {
            cmd = default;

            if (_disposeQueue.Count == 0)
            {
                _ticks = 0;
                _isDisposing = false;
                return false;
            }

            if (++_ticks < delayTicks)
            {
                _isDisposing = false;
                return false;
            }

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