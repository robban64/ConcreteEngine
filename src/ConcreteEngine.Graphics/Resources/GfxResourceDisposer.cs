using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceDisposer
{
    int PendingCount { get; }
    void EnqueueRemoval<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta;
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

    public void DrainDisposeQueue(GlBackendDriver driver)
    {
        int drainCount = 0;
        while (drainCount < DrainPerFrame && _disposeQueue.TryGetNext(DrainDelayTicks, out var cmd))
        {
            driver.Disposer.DeleteGlResource(cmd);
            _backendStoreHub.GetStore(cmd.Handle.Kind).Remove(cmd.Handle);
            if (!cmd.Replace)
            {
                _gfxStoreHub.RemoveResource(cmd.GfxId, cmd.Handle.Kind);
            }

            drainCount++;
        }
    }

    public void EnqueueRemoval<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        ArgumentOutOfRangeException.ThrowIfZero(id.Id);
        var resourceKind = TMeta.ResourceKind;
        var fStore = _gfxStoreHub.GetStore<TMeta>();
        var gfxHandle = fStore.GetHandle(id);

        var bStore = _backendStoreHub.GetStore(resourceKind);
        var native = bStore.GetNativeHandle(gfxHandle);

        var cmd = DeleteResourceCommand.MakeDelete(gfxHandle, native, id);
        _disposeQueue.Enqueue(cmd);

        GfxLog.LogBackend(native, gfxHandle, resourceKind.ToLogTopic(), LogAction.Evict);
    }

    public void EnqueueReplace(GfxHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(handle.IsValid, false);

        var bkStore = _backendStoreHub.GetStore(handle.Kind);
        var bkHandle = bkStore.GetNativeHandle(handle);
        var cmd = DeleteResourceCommand.MakeReplace(handle, bkHandle);
        _disposeQueue.Enqueue(cmd);

        GfxLog.LogBackend(bkHandle, handle, handle.Kind.ToLogTopic(), LogAction.Evict);
    }


    private sealed class ResourceDisposeQueue
    {
        private readonly Queue<DeleteResourceCommand> _disposeQueue = new(8);
        private readonly HashSet<int> _disposeSet = new(8);
        public int PendingCount => _disposeQueue.Count;

        private bool _isDisposing;

        private int _ticks;

        public void Enqueue(DeleteResourceCommand cmd)
        {
            InvalidOpThrower.ThrowIf(_isDisposing);
            InvalidOpThrower.ThrowIfNot(_disposeSet.Add(cmd.GetHashCode()));

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