using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal class ResourceDisposeQueue
{
    private const int DrainPerFrame = 4;
    private const int DrainDelayTicks = 16;

    private readonly Queue<DeleteCmd> _disposeQueue = new(8);
    private readonly HashSet<DeleteCmd> _disposeSet = new(8);

    private bool _isDisposing = false;

    private int _ticks;

    public void Enqueue(in GfxHandle handle, int priority = 0)
    {
        if (_isDisposing)
            throw GraphicsException.InvalidState("Illegal state: Enqueue of removal during active dispose");
        
        var cmd = new DeleteCmd(in handle, priority);
        if(!_disposeSet.Add(cmd))
            throw  GraphicsException.DuplicatedResource<ResourceDisposeQueue>("");
        
        _disposeQueue.Enqueue(cmd);
    }

    public void Drain(Action<GfxHandle> disposeFunc, bool drainAll = false)
    {
        if (_disposeQueue.Count == 0) return;
        if (++_ticks < DrainDelayTicks && !drainAll) return;

        _isDisposing = true;
        
        int n = 0;
        while (_disposeQueue.Count > 0 && (drainAll || n < DrainPerFrame))
        {
            var curr = _disposeQueue.Dequeue();
            disposeFunc(curr.Handle);
            n++;
        }

        if (_disposeQueue.Count == 0)
        {
            _isDisposing = false;
            _disposeSet.Clear();
        }

        _ticks = 0;
    }

    private readonly record struct DeleteCmd(
        in GfxHandle Handle,
        int Priority = 0
    );
}