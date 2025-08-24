namespace ConcreteEngine.Common.Collections;

public class PendingList<T>(Action<T> handler)
{
    private readonly List<T> _pending = new(8);
    public void Enqueue(T item) => _pending.Add(item);

    public void Flush()
    {
        if (_pending.Count > 0)
        {
            foreach (var item in _pending)
            {
                handler(item);
            }
        }
    }
}

public class PendingListDouble<T>(Action<T> handler1, Action<T> handler2)
{
    private readonly PendingList<T> _pendingFirst = new(handler1);
    private readonly PendingList<T> _pendingSecond = new(handler2);

    public void EnqueueFirst(T item) => _pendingFirst.Enqueue(item);
    public void EnqueueSecond(T item) => _pendingSecond.Enqueue(item);

    public void Flush()
    {
        _pendingSecond.Flush();
        _pendingFirst.Flush();
    }
}
/*
public class PendingListDouble<T>
{
    private readonly List<T> _pendingAdd = new (32);
    private readonly List<T> _pendingRemove = new (32);

    public void EnqueueAdd(T item) =>  _pendingAdd.Add(item);
    public void EnqueueRemove(T item) =>  _pendingRemove.Add(item);

    public void Flush(ICollection<T> collection)
    {
        if (_pendingRemove.Count > 0)
        {
            foreach (var item in _pendingRemove)
            {
                collection.Remove(item);
            }
            _pendingRemove.Clear();
        }

        if (_pendingAdd.Count > 0)
        {
            foreach (var item in _pendingAdd)
            {
                collection.Add(item);
            }
            _pendingAdd.Clear();
        }
    }
}*/