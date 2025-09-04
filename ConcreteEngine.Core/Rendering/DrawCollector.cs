#region

#endregion

namespace ConcreteEngine.Core.Rendering;


internal sealed class DrawCollector
{
    private readonly Dictionary<Type, IDrawCommandProducer> _producers = new(8);

    private List<IDrawCommandProducer> _producerList = null!;
    
    public int Count => _producerList.Count;

    internal DrawCollector()
    {
        
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink
    {
        if(_producers.TryGetValue(typeof(TSink), out var producer))
            return (TSink)producer;
        
        throw new InvalidOperationException($"{typeof(TSink).Name} is not registered");
    }
    
    public void RegisterProducer<TSink>(TSink producer) where TSink: IDrawSink
    {
        ArgumentNullException.ThrowIfNull(producer, nameof(producer));
        if(!_producers.TryAdd(typeof(TSink), (IDrawCommandProducer)producer))
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


    public void BeginTick(in UpdateMetaInfo updateMeta)
    {
        foreach (var producer in _producerList)
            producer.BeginTick(in updateMeta);

    }
    
    public void EndTick()
    {
        foreach (var producer in _producerList)
            producer.EndTick();
    }


    public void Collect(float alpha, RenderPipeline submitter)
    {
        foreach (var producer in _producerList)
            producer.EmitFrame(alpha, submitter);
    }
}