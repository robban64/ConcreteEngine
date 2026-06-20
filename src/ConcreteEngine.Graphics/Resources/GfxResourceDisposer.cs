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

    private readonly ResourceDisposeQueue _disposeQueue;

    public int PendingCount => _disposeQueue.PendingCount;

    internal GfxResourceDisposer()
    {
        _disposeQueue = new ResourceDisposeQueue();
    }

    public void DrainDisposeQueue(GlBackendDriver driver)
    {
        int drainCount = 0;
        while (drainCount < DrainPerFrame && _disposeQueue.TryGetNext(DrainDelayTicks, out var cmd))
        {
            driver.Disposer.DeleteGlResource(cmd);
            GfxRegistry.GetBackendStore(cmd.Handle.Kind).Remove(cmd.Handle);
            if (!cmd.Replace)
            {
                GfxRegistry.GetGfxStore(cmd.Handle.Kind).Remove(new GfxId(cmd.GfxId, cmd.Handle.Kind));
            }

            drainCount++;
        }
    }

    public void EnqueueRemoval<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        ArgumentOutOfRangeException.ThrowIfZero(id.Id);
        var resourceKind = TMeta.ResourceKind;
        var fStore = GfxRegistry.GetGfxStore<TMeta>();
        var gfxHandle = fStore.GetHandle(id);

        var bkHandle = GfxRegistry.GetBackendStore<TMeta>().GetSafe(gfxHandle);

        var cmd = DeleteResourceCommand.MakeDelete(gfxHandle, bkHandle, id);
        _disposeQueue.Enqueue(cmd);

        GfxLog.LogBackend(bkHandle, gfxHandle, resourceKind.ToLogTopic(), LogAction.Evict);
    }

    public void EnqueueReplace(GfxHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(handle.IsValid, false);

        var bkHandle = GfxRegistry.GetBackendStore(handle.Kind).GetSafe(handle);
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
            if(_isDisposing) 
                Throwers.InvalidOperation("Disposer is active");
            if(!_disposeSet.Add(cmd.GetHashCode())) 
                Throwers.InvalidArgument(nameof(cmd), "GfxResource already enqueued");

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