#region

using ConcreteEngine.Core.Rendering.Batchers.Sprite;
using ConcreteEngine.Core.Rendering.Batchers.Tilemap;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;

public sealed class DrawEmitterContext
{
    public float Alpha;
    public required IGraphicsDevice Graphics { get; init; }
    public required SpriteBatcher SpriteBatch { get; init; }
    public required TilemapBatcher TilemapBatch { get; init; }
}

public interface IDrawCommandEmitter
{
    int Order { get; }
    Type EntityType { get; }
    void Initialize(int order);
    void Emit(DrawEmitterContext context, DrawCommandSubmitter submitter);

    void RegisterFeature(int order, IDrawableFeature feature);
}

public abstract class DrawCommandEmitter<TEntity> : IDrawCommandEmitter
    where TEntity : struct
{
    public int Order { get; private set; }
    public Type EntityType => typeof(TEntity);

    private readonly SortedList<int, IDrawableFeature<TEntity>> _features = new(8);

    public void Initialize(int order)
    {
        Order = order;
    }

    public void Emit(DrawEmitterContext ctx, DrawCommandSubmitter submitter)
    {
        if (_features.Count == 0)
        {
            EmitBatch(ReadOnlySpan<TEntity>.Empty, in ctx, submitter, 0);
            return;
        }

        foreach (var (order, feature) in _features)
        {
            EmitBatch(feature.GetDrawables(), in ctx, submitter, order);
        }
    }

    protected abstract void EmitBatch(
        ReadOnlySpan<TEntity> entities,
        in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter,
        int order);


    public void RegisterFeature(int order, IDrawableFeature feature)
    {
        if (feature is not IDrawableFeature<TEntity> featureEntity)
            throw new ArgumentException($"Feature type {feature.GetType()} is not supported");

        _features.Add(order, featureEntity);
    }

    internal void RegisterFeature<TFeature>(int order, IDrawableFeature<TEntity> feature)
        where TFeature : IDrawableFeature<TEntity>

    {
        _features.Add(order, feature);
    }
}