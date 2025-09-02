#region

#endregion

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawCommandCollector
{
    private readonly List<IDrawCommandProducer> _producers = new(8);
    public int Count => _producers.Count;

    public TProducer GetProducer<TProducer>() where TProducer : IDrawCommandProducer
    {
        foreach (var producer in _producers)
        {
            if (producer is TProducer tProducer) return tProducer;
        }

        throw new InvalidOperationException($"DrawCommandProducer {typeof(TProducer).Name} not registered");
    }

    public IDrawCommandProducer GetProducer(Type producerType)
    {
        foreach (var producer in _producers)
        {
            if (producer.GetType() == producerType) return producer;
        }

        throw new InvalidOperationException($"DrawCommandProducer {producerType.Name} not registered");
    }

    public void AddProducer(IDrawCommandProducer producer)
    {
        ArgumentNullException.ThrowIfNull(producer, nameof(producer));
        if (_producers.Contains(producer))
            throw new InvalidOperationException($"DrawCommandProducer {producer.GetType().Name} is already registered");

        _producers.Add(producer);
    }

    public void AttachContext(CommandProducerContext context)
    {
        foreach (var producer in _producers)
        {
            producer.AttachContext(context);
        }
    }

    public void Collect(float alpha, DrawCommandSubmitter submitter)
    {
        foreach (var producer in _producers)
        {
            producer.Produce(alpha, submitter);
        }
    }
}