#region

#endregion

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawCommandCollector
{
    private readonly SortedList<int, IDrawCommandProducer> _producers = new(8);

    public int Count => _producers.Count;

    public DrawCommandProducer<TDrawData> GetProducer<TProducer, TDrawData>()
        where TProducer : DrawCommandProducer<TDrawData>
        where TDrawData : class
    {
        foreach (var producer in _producers.Values)
        {
            if (producer is TProducer tProducer) return tProducer;
        }

        throw new InvalidOperationException($"DrawCommandProducer {typeof(TProducer).Name} not registered");
    }

    public IDrawCommandProducer GetProducer(Type producerType)
    {
        foreach (var producer in _producers.Values)
        {
            if (producer.GetType() == producerType) return producer;
        }

        throw new InvalidOperationException($"DrawCommandProducer {producerType.Name} not registered");
    }

    public void AddProducer(int order, IDrawCommandProducer producer)
    {
        ArgumentNullException.ThrowIfNull(producer, nameof(producer));
        if (_producers.ContainsValue(producer))
            throw new InvalidOperationException($"DrawCommandProducer {producer.GetType().Name} is already registered");

        _producers.Add(order, producer);
    }

    public void Initialize()
    {
        foreach (var (order, producer) in _producers)
        {
            producer.Initialize(order);
        }
    }

    public void Collect(CommandProducerContext context, DrawCommandSubmitter submitter)
    {
        var producers = _producers.Values;
        foreach (var producer in producers)
        {
            producer.Produce(context, submitter);
        }
    }
}