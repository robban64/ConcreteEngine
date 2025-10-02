using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawCommandPipeline
{
    private DrawCommandCollector _commandCollector = null!;

    private SceneDrawProducer _sceneDrawProducer = null!;

    private DrawCommandBuffer _cmdBuffer = null!;

    public DrawCommandPipeline()
    {
    }

    public void Initialize(GfxContext gfx, BatcherRegistry batches, DrawProcessor drawProcessor)
    {
        _commandCollector = new DrawCommandCollector();
        _cmdBuffer = new DrawCommandBuffer(drawProcessor);

        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);

        var cmdProducerCtx = new CommandProducerContext { Gfx = gfx, DrawBatchers = batches };
        _commandCollector.AttachContext(cmdProducerCtx);
        _cmdBuffer.Initialize();
        _commandCollector.InitializeProducers();
    }

    internal void BeginTick(in UpdateInfo update) => _commandCollector.BeginTick(update);
    internal void EndTick() => _commandCollector.EndTick();
    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();


    internal nint Prepare(float alpha, in RenderGlobalSnapshot snapshot)
    {
        _cmdBuffer.Reset();

        _sceneDrawProducer.SetSceneGlobals(in snapshot);

        // Fill buffer
        _commandCollector.CollectTo(alpha, in snapshot, _cmdBuffer);

        _cmdBuffer.ReadyDrawCommands();

        //
        return UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_cmdBuffer.Count + 32);
    }

    internal void ExecuteTransforms() => _cmdBuffer.DrainTransformQueue();

    internal void ExecuteDrawPass(RenderTargetId targetId) => _cmdBuffer.DrainCommandQueue(targetId);
}