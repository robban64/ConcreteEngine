#region

using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Rendering.Batchers;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class CommandProducerContext
{
    public required IGraphicsDevice Graphics { get; init; }
    public required BatcherRegistry DrawBatchers { get; init; }
}

public interface IDrawCommandProducer
{
    Type EntityType { get; }
    void AttachContext(CommandProducerContext ctx);
    void Produce(float alpha, DrawCommandSubmitter submitter);

    void RegisterFeature(IDrawableFeature feature);
}

public abstract class DrawCommandProducer<TDrawData> : IDrawCommandProducer where TDrawData : class
{
    public Type EntityType => typeof(TDrawData);

    private readonly List<IDrawableFeature<TDrawData>> _features = new(8);
    
    protected CommandProducerContext Context { get; private set; } = null!;

    public virtual void OnInitialize()
    {
    }

    protected abstract void EmitCommands(
        float alpha,
        TDrawData data,
        DrawCommandSubmitter submitter);

    public void AttachContext(CommandProducerContext ctx)
    {
        Context = ctx;
        OnInitialize();
    }

    public void Produce(float alpha, DrawCommandSubmitter submitter)
    {
        if (_features.Count == 0)
        {
            return;
        }

        foreach (var feature in _features)
        {
            EmitCommands(alpha, feature.GetDrawables(),  submitter);
        }
    }


    public void RegisterFeature(IDrawableFeature feature)
    {
        if (feature is not IDrawableFeature<TDrawData> featureEntity)
            throw new ArgumentException($"Feature type {feature.GetType()} is not supported");

        _features.Add(featureEntity);
    }

    internal void RegisterFeature<TFeature>( IDrawableFeature<TDrawData> feature)
        where TFeature : IDrawableFeature<TDrawData>

    {
        _features.Add(feature);
    }
}