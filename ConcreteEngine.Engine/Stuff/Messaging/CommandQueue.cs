namespace ConcreteEngine.Engine.Stuff.Messaging;

internal sealed class CommandQueue
{
    private readonly List<IGameCommand> _queue = new(64);

    public int Count => _queue.Count;

    public void Enqueue(IGameCommand cmd)
    {
        _queue.Add(cmd);
    }

    public void DequeueAll(List<IGameCommand> outBatch)
    {
        outBatch.Clear();
        outBatch.AddRange(_queue);
        _queue.Clear();
    }
}