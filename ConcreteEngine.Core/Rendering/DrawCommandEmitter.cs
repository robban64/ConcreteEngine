#region

using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IDrawCommandEmitter
{
    int Order { get; set; }
    void Initialize(IFeatureRegistry registry);
    void Emit(DrawEmitterContext context, DrawCommandSubmitter submitter);
}

public sealed class DrawEmitterContext
{
    public float Alpha;
    public required IGraphicsDevice Graphics { get; init; }
    public required SpriteBatcher SpriteBatch { get; init; }
    public required TilemapBatcher TilemapBatch { get; init; }
}