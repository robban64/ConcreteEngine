using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

public class ResourceDisposeQueue
{
    private const int DrainPerFrame = 4;
    private const int DrainDelayTicks = 16;

    private readonly Queue<DeleteCmd> _disposeQueue = new(8);

    private int _ticks;

    public void Enqueue(
        ResourceKind kind,
        Action disposeAction,
        int priority = 0)
    {
        _disposeQueue.Enqueue(new DeleteCmd(kind, disposeAction, priority));
    }

    public void Drain(bool drainAll = false)
    {
        if (_disposeQueue.Count == 0) return;
        if (++_ticks < DrainDelayTicks && !drainAll) return;

        int n = 0;
        while (_disposeQueue.Count > 0 && (drainAll || n < DrainPerFrame))
        {
            _disposeQueue.Dequeue().DisposeAction();
            n++;
        }

        _ticks = 0;
    }

    private readonly record struct DeleteCmd(
        ResourceKind Kind,
        Action DisposeAction,
        int Priority = 0
    );
}