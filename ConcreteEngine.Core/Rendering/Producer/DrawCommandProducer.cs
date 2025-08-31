#region

using ConcreteEngine.Core.Features;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class CommandProducerContext
{
    public float Alpha;
    public required IGraphicsDevice Graphics { get; init; }
    public required SpriteBatcher SpriteBatch { get; init; }
    public required TilemapBatcher TilemapBatch { get; init; }
}

public interface IDrawCommandProducer
{
    int Order { get; }
    Type EntityType { get; }
    void Initialize(CommandProducerContext ctx, int order);
    void Produce(CommandProducerContext context, DrawCommandSubmitter submitter);

    void RegisterFeature(int order, IDrawableFeature feature);
}

public abstract class DrawCommandProducer<TDrawData> : IDrawCommandProducer where TDrawData : class
{
    public int Order { get; private set; }
    public Type EntityType => typeof(TDrawData);

    private readonly SortedList<int, IDrawableFeature<TDrawData>> _features = new(8);
    
    
    public virtual void OnInitialize(CommandProducerContext ctx)
    {
        
    }
    
    protected abstract void EmitCommands(
        TDrawData data,
         CommandProducerContext ctx,
        DrawCommandSubmitter submitter,
        int order);

    public void Initialize(CommandProducerContext ctx, int order)
    {
        Order = order;
        OnInitialize(ctx);
    }

    public void Produce(CommandProducerContext ctx, DrawCommandSubmitter submitter)
    {
        if (_features.Count == 0)
        {
            EmitCommands(null!,  ctx, submitter, 0);
            return;
        }

        foreach (var (order, feature) in _features)
        {
            EmitCommands(feature.GetDrawables(),  ctx, submitter, order);
        }
    }


    public void RegisterFeature(int order, IDrawableFeature feature)
    {
        if (feature is not IDrawableFeature<TDrawData> featureEntity)
            throw new ArgumentException($"Feature type {feature.GetType()} is not supported");

        _features.Add(order, featureEntity);
    }

    internal void RegisterFeature<TFeature>(int order, IDrawableFeature<TDrawData> feature)
        where TFeature : IDrawableFeature<TDrawData>

    {
        _features.Add(order, feature);
    }
}