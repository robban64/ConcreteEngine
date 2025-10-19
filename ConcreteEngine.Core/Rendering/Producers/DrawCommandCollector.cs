#region

#endregion

#region

using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.State;

#endregion

namespace ConcreteEngine.Core.Rendering.Producers;

public interface IDrawCommandCollector
{
    void RegisterProducerSink<TSink>(TSink producer) where TSink : IDrawSink;
    void RegisterProducer<TProducer>(TProducer producer) where TProducer : IDrawCommandProducer;

}
public sealed class DrawCommandCollector : IDrawCommandCollector
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
    
    public TProducer GetProducer<TProducer>() where TProducer : IDrawCommandProducer
    {
        if (_producers.TryGetValue(typeof(TProducer), out var producer))
            return (TProducer)producer;

        throw new InvalidOperationException($"{typeof(TProducer).Name} is not registered");
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


    public void BeginTick(in UpdateTickInfo tick)
    {
        foreach (var producer in _producerList)
            producer.BeginTick(in tick);
    }

    public void EndTick()
    {
        foreach (var producer in _producerList)
            producer.EndTick();
    }

    public void CollectTo(float alpha, RenderSceneState snapshot, DrawCommandBuffer submitter)
    {
        foreach (var producer in _producerList)
            producer.EmitFrame(alpha, in snapshot, submitter);
    }
}