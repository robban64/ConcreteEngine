#region

using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Producers;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawCommandPipeline
{
    private DrawCommandCollector _commandCollector = null!;
    private SceneDrawProducer _sceneDrawProducer = null!;

    private DrawCommandBuffer _cmdBuffer = null!;
    private MaterialDrawBuffer _materialBuffer;

    private DrawCommandProcessor _cmdDraw = null!;
    private DrawBuffers _drawBuffers = null!;

    internal MaterialDrawBuffer MaterialBuffer => _materialBuffer;

    internal DrawCommandPipeline()
    {
    }

    internal void BeginTick(in UpdateTickInfo tick) => _commandCollector.BeginTick(tick);
    internal void EndTick() => _commandCollector.EndTick();
    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();
    
    public void Initialize(GfxContext gfx, BatcherRegistry batches, DrawCommandProcessor cmdDraw, DrawBuffers drawBuffers)
    {
        _cmdDraw = cmdDraw;
        _drawBuffers = drawBuffers;
        
        _commandCollector = new DrawCommandCollector();
        _cmdBuffer = new DrawCommandBuffer(cmdDraw, drawBuffers);
        _materialBuffer = new MaterialDrawBuffer();

        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);

        var cmdProducerCtx = new CommandProducerContext { Gfx = gfx, DrawBatchers = batches };
        _commandCollector.AttachContext(cmdProducerCtx);
        _cmdBuffer.Initialize();
        _commandCollector.InitializeProducers();
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