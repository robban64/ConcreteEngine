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

    private readonly ResourceBackendDispatcher _dispatcher;
    internal GfxResourceDisposer(ResourceBackendDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _disposeQueue = new ResourceDisposeQueue();
    }

    public void DrainDisposeQueue()
    {
        int drainCount = 0;
        while (drainCount < DrainPerFrame && _disposeQueue.TryGetNext(DrainDelayTicks, out var cmd))
        {
            GlDisposer.DeleteGlResource(_dispatcher, cmd);
            drainCount++;
        }
    }

    public void EnqueueRemoval<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        ArgumentOutOfRangeException.ThrowIfZero(id.Id);
        var handle = GfxRegistry.GetStore<TMeta>().GetHandle(id);
        var cmd = new DeleteResourceCommand(handle, id, TMeta.ResourceKind, false);
        _disposeQueue.Enqueue(cmd);
        GfxLog.LogBackend(handle, id, TMeta.ResourceKind.ToLogTopic(), LogAction.Evict);
    }

    public void EnqueueReplace<TMeta>(GfxId<TMeta> id, NativeHandle handle) where TMeta : unmanaged, IResourceMeta
    {
        ArgumentOutOfRangeException.ThrowIfEqual(handle.IsValid(), false);
        var cmd = new DeleteResourceCommand(handle, id, TMeta.ResourceKind, true);
        _disposeQueue.Enqueue(cmd);
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
            if (_isDisposing)
                Throwers.InvalidOperation("Disposer is active");
            if (!_disposeSet.Add(cmd.GetHashCode()))
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