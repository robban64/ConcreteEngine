#region

#endregion

using ConcreteEngine.Core.Rendering.Data;

namespace ConcreteEngine.Core.Rendering.Commands;

internal sealed class DrawCommandCollector
{
    private readonly Dictionary<Type, IDrawCommandProducer> _producers = new(8);

    private List<IDrawCommandProducer> _producerList = null!;

    public int Count => _producerList.Count;

    internal DrawCommandCollector()
    {
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink
    {
        if (typeof(TSink).IsClass)
            throw new InvalidOperationException("TSink is not an interface of IDrawSink");

        if (_producers.TryGetValue(typeof(TSink), out var producer))
            return (TSink)producer;

        throw new InvalidOperationException($"{typeof(TSink).Name} is not registered");
    }

    public void RegisterProducerSink<TSink>(TSink producer) where TSink : IDrawSink
    {
        ArgumentNullException.ThrowIfNull(producer, nameof(producer));
        if (!_producers.TryAdd(typeof(TSink), (IDrawCommandProducer)producer))
            throw new InvalidOperationException($"{producer.GetType().Name} is already registered");
    }

    public void RegisterProducer<TProducer>(TProducer producer) where TProducer : IDrawCommandProducer
    {
        ArgumentNullException.ThrowIfNull(producer, nameof(producer));
        if (!_producers.TryAdd(typeof(TProducer), producer))
            throw new InvalidOperationException($"{producer.GetType().Name} is already registered");
    }


    public void AttachContext(CommandProducerContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        _producerList = _producers.Values.ToList();

        foreach (var producer in _producerList)
        {
            producer.AttachContext(context);
        }
    }

    public void InitializeProducers()
    {
        foreach (var producer in _producerList)
            producer.Initialize();
    }


    public void BeginTick(in UpdateInfo update)
    {
        foreach (var producer in _producerList)
            producer.BeginTick(in update);
    }

    public void EndTick()
    {
        foreach (var producer in _producerList)
            producer.EndTick();
    }

    public void Collect(float alpha, in RenderGlobalSnapshot snapshot, DrawCommandPipeline submitter)
    {
        foreach (var producer in _producerList)
            producer.EmitFrame(alpha, in snapshot, submitter);
    }
}