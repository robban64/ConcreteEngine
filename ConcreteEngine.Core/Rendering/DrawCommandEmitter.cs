using ConcreteEngine.Core.Rendering.SpriteBatching;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering;

public interface IDrawCommandEmitter
{
    int Order { get; set; }
    void Initialize(IFeatureRegistry registry);
    void Emit(DrawEmitterContext context, DrawCommandSubmitter submitter);
}

public sealed class DrawEmitterContext
{
    public IGraphicsDevice Graphics { get; init; }
    public SpriteBatcher SpriteBatch { get; init; }
    public TilemapBatcher TilemapBatch { get; init; }
}