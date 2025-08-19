#region

using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IDrawCommandEmitter
{
    int Order { get;  }
    Type EntityType { get; }
    void Initialize();
    void Emit(DrawEmitterContext context, DrawCommandSubmitter submitter);
}

public abstract class DrawCommandEmitter<TEntity> : IDrawCommandEmitter
    where TEntity : struct
{
    public int Order { get; internal set; }
    public Type EntityType => typeof(TEntity);

    private readonly SortedList<int, IDrawableFeature<TEntity>> _features = new(8);

    internal void RegisterFeature<TFeature>(int order, TFeature feature)
        where TFeature : IDrawableFeature<TEntity>

    {
        _features.Add(order, feature);
    }
    
    protected abstract void EmitBatch(
        ReadOnlySpan<TEntity> entities,
        in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter,
        int order);

    public void Emit(DrawEmitterContext ctx, DrawCommandSubmitter submitter)
    {
        foreach (var (order, feature) in _features)
            EmitBatch(feature.GetDrawables(), in ctx, submitter, order);
    }
    
    public void Initialize()
    {
    }
}

public sealed class DrawEmitterContext
{
    public float Alpha;
    public required IGraphicsDevice Graphics { get; init; }
    public required SpriteBatcher SpriteBatch { get; init; }
    public required TilemapBatcher TilemapBatch { get; init; }
}