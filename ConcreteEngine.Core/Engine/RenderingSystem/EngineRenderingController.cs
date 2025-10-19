using ConcreteEngine.Core.Engine.RenderingSystem.Batching;
using ConcreteEngine.Core.Engine.RenderingSystem.Producers;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.RenderingSystem;

public sealed class EngineRenderingController
{
    public BatcherRegistry Batchers { get; }
    public DrawCommandCollector CommandCollector { get; }

    private readonly GfxContext _gfx;
    private SceneDrawProducer _sceneDrawProducer = null!;

    internal EngineRenderingController(GfxContext gfx)
    {
        _gfx = gfx;
        Batchers = new BatcherRegistry();
        CommandCollector = new DrawCommandCollector();
    }

    internal void Initialize(Action<GfxContext, BatcherRegistry> batcherSetup, Action<IDrawCommandCollector> collectorSetup)
    {
        var cmdProducerCtx = new CommandProducerContext { Gfx = _gfx, DrawBatchers = Batchers };

        batcherSetup(_gfx, Batchers);
        
        collectorSetup(CommandCollector);
        _sceneDrawProducer = CommandCollector.GetProducer<SceneDrawProducer>();
        CommandCollector.AttachContext(cmdProducerCtx);
        CommandCollector.InitializeProducers();
    }
}