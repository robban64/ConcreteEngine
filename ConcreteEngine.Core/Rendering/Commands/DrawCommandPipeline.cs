#region

using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Producers;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using MaterialStore = ConcreteEngine.Core.Assets.Materials.MaterialStore;

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

internal sealed class DrawCommandPipeline
{
    private DrawCommandCollector _commandCollector = null!;

    private SceneDrawProducer _sceneDrawProducer = null!;

    private DrawCommandBuffer _cmdBuffer = null!;
    private DrawMaterialBuffer _materialBuffer;
    private RenderMaterialRegistry _materialRegistry;

    public DrawCommandPipeline()
    {
    }

    public void Initialize(GfxContext gfx, BatcherRegistry batches, DrawProcessor drawProcessor,
        RenderMaterialRegistry materialRegistry)
    {
        _materialRegistry = materialRegistry;

        _commandCollector = new DrawCommandCollector();
        _cmdBuffer = new DrawCommandBuffer(drawProcessor);
        _materialBuffer = new DrawMaterialBuffer(drawProcessor);

        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);

        var cmdProducerCtx = new CommandProducerContext { Gfx = gfx, DrawBatchers = batches };
        _commandCollector.AttachContext(cmdProducerCtx);
        _cmdBuffer.Initialize();
        _commandCollector.InitializeProducers();
    }

    internal void BeginTick(in UpdateTickInfo tick) => _commandCollector.BeginTick(tick);
    internal void EndTick() => _commandCollector.EndTick();
    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();


    private void PrepareMaterials()
    {
        Span<MaterialParams> dataBuffer = stackalloc MaterialParams[_materialRegistry.Count];
        Span<DrawMaterialCommand> cmdBuffer = stackalloc DrawMaterialCommand[_materialRegistry.Count];

         _materialRegistry.DrainDrawData(cmdBuffer,dataBuffer);
/*
        for (int i = 0; i < dataBuffer.Length; i++)
        {
            ref readonly MaterialParams param = ref dataBuffer[i];
            ref readonly DrawMaterialCommand cmd = ref cmdBuffer[i];

            cmdBuffer[i] = new DrawMaterialCommand(m.Id);
            dataBuffer[i] = new MaterialParams(m.Color, m.SpecularStrength, m.Shininess, 
                m.UvRepeat, m.HasNormalMap ? 1f : 0f);
        }
    */
        _materialBuffer.SubmitMaterials(cmdBuffer, dataBuffer);
    }

    internal (nint, nint) Prepare(float alpha, in RenderSceneState snapshot)
    {
        _cmdBuffer.Reset();
        _materialBuffer.Reset();

        _sceneDrawProducer.SetSceneGlobals(in snapshot);
        
        // Fill command buffer
        _commandCollector.CollectTo(alpha, in snapshot, _cmdBuffer);

        // Sort command buffer and prepare passes
        _cmdBuffer.ReadyDrawCommands();

        // Fill materials
        PrepareMaterials();

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_cmdBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniformRecord>(_materialBuffer.Count + 4);
        return (drawCap, matCap);
    }

    internal void ExecuteMaterials() => _materialBuffer.DispatchMaterials();
    
    internal void ExecuteTransforms() => _cmdBuffer.DrainTransformQueue();

    internal void ExecuteDrawPass(PassId passId) => _cmdBuffer.DispatchDrawPass(passId);
}