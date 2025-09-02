using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Rendering.Batchers;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;


internal sealed class RenderPipeline2D
{
    private readonly IGraphicsDevice _graphics;
    private readonly Camera2D _camera2D;
    private readonly MaterialStore _materialStore;

    private readonly List<ICommandRenderer> _renderers = new();

    private readonly RenderTargetRegistry _renderTargetRegistry;
    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly CommandProducerContext _commandProducerContext;
    
    private readonly SpriteBatcher _spriteBatcher;
    private readonly TilemapBatcher _tilemapBatcher;
    
    internal RenderPipeline2D(IGraphicsDevice graphics, Camera2D camera2D, MaterialStore materialStore, IGameFeatureManager _features)
    {
        _graphics = graphics;
        _camera2D = camera2D;
        _materialStore = materialStore;
        /*
        // Draw Features
        var tilemapProducer = new TilemapDrawProducer();
        tilemapProducer.RegisterFeature(_features.Get<TilemapFeature>());
        
        var spriteProducer = new SpriteDrawProducer();
        spriteProducer.RegisterFeature(_features.Get<SpriteFeature>());

        var lightProducer = new LightProducer();
        lightProducer.RegisterFeature(_features.Get<LightFeature>());

        _spriteBatcher = new SpriteBatcher(_graphics);
        _tilemapBatcher = new TilemapBatcher(graphics, 64, 32);
        
        // Collector
        _commandCollector  = new DrawCommandCollector();
        _commandCollector.AddProducer(tilemapProducer);
        _commandCollector.AddProducer(spriteProducer);
        _commandCollector.AddProducer(lightProducer);
        _commandCollector.AttachContext(_commandProducerContext);
        
        _renderers.Add(new SpriteRenderer(_graphics, _camera2D, _materialStore));
        _renderers.Add(new LightRenderer(_graphics, _camera2D, _materialStore));

        _commandSubmitter = new DrawCommandSubmitter(_renderers);
        _commandSubmitter.Register<DrawCommandMesh, SpriteRenderer>(DrawCommandTag.Mesh2D, DrawCommandId.Tilemap, DrawCommandId.Sprite);
        _commandSubmitter.Register<DrawCommandLight, LightRenderer>(DrawCommandTag.Effect2D, DrawCommandId.Light);*/
    }
}