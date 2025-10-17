#region

using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Producers;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawCommandPipeline
{
    private readonly DrawCommandCollector _commandCollector;
    private SceneDrawProducer _sceneDrawProducer = null!;

    private readonly DrawCommandBuffer _cmdBuffer;
    private readonly MaterialDrawBuffer _materialBuffer;

    private readonly DrawCommandProcessor _cmdDraw;
    private readonly DrawBuffers _drawBuffers;
    private readonly DrawStateOps _drawStateOps;


    public DrawCommandProcessor DrawCmdProcessor => _cmdDraw;
    public DrawBuffers Buffers => _drawBuffers;
    public DrawStateOps DrawStateOps => _drawStateOps;
    internal DrawCommandCollector Collector => _commandCollector;
    
    internal DrawCommandPipeline(GfxContext gfx, BatcherRegistry batches, RenderRegistry renderRegistry, RenderView view, RenderSceneState snapshot)
    {
        var drawCtx = new DrawStateContext(renderRegistry);
        var drawCtxPayload = new DrawStateContextPayload
        {
            Gfx = gfx, Registry = renderRegistry, RenderView = view, Snapshot = snapshot
        };

        // Draw / Invokers
        _drawBuffers = new DrawBuffers(drawCtx, drawCtxPayload);
        _cmdDraw = new DrawCommandProcessor(drawCtx, drawCtxPayload, _drawBuffers);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawBuffers);

        _cmdBuffer = new DrawCommandBuffer(_cmdDraw, _drawBuffers);
        _materialBuffer = new MaterialDrawBuffer();

        _commandCollector = new DrawCommandCollector();
        var cmdProducerCtx = new CommandProducerContext { Gfx = gfx, DrawBatchers = batches };

        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _commandCollector.RegisterProducer<SceneDrawProducer>(new SceneDrawProducer());
        _sceneDrawProducer = _commandCollector.GetProducer<SceneDrawProducer>();

        _commandCollector.AttachContext(cmdProducerCtx);
        _commandCollector.InitializeProducers();
    }

    internal void BeginTick(in UpdateTickInfo tick) => _commandCollector.BeginTick(tick);
    internal void EndTick() => _commandCollector.EndTick();
    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();
    
    public void Initialize()
    {
        _cmdBuffer.Initialize();
        _cmdDraw.Initialize();
        _drawBuffers.AttachMaterialBuffer(_materialBuffer);

    }

    internal void SubmitMaterialDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _materialBuffer.SubmitDrawData(in payload, slots);

    internal (nint, nint) Prepare(float alpha, RenderSceneState snapshot)
    {
        _cmdBuffer.Reset();
        _materialBuffer.Reset();

        _sceneDrawProducer.SetSceneGlobals(snapshot);

        // Fill command buffer
        _commandCollector.CollectTo(alpha, snapshot, _cmdBuffer);

        // Sort command buffer and prepare passes
        _cmdBuffer.ReadyDrawCommands();

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_cmdBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniformRecord>(_materialBuffer.Count + 4);
        return (drawCap, matCap);
    }

    internal void ExecuteMaterials()
    {
        _drawBuffers.UploadMaterial(_materialBuffer.DrainDrawMaterialData());
    }

    internal void ExecuteTransforms() => _cmdBuffer.DrainTransformQueue();

    internal void ExecuteDrawPass(PassId passId) => _cmdBuffer.DispatchDrawPass(passId);
}