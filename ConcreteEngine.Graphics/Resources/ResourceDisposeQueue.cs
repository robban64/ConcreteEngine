using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal class ResourceDisposeQueue
{

    private readonly Queue<DeleteCmd> _disposeQueue = new(8);
    private readonly HashSet<int> _disposeSet = new(8);
    
    public int PendingCount => _disposeQueue.Count;

    private bool _isDisposing = false;

    private int _ticks;

    public void Enqueue(in DeleteCmd cmd)
    {
        if (_isDisposing)
            throw GraphicsException.InvalidState("Illegal state: Enqueue of removal during active dispose");
        
        if(!_disposeSet.Add(cmd.IdValue))
            throw GraphicsException.DuplicatedResource<ResourceDisposeQueue>("");
        
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